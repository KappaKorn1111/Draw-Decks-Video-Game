using System;
using System.Linq;
using System.Reflection;
using DrawDecks.Domain;
using Xunit;

namespace DrawDecks.Domain.Tests;

public sealed class CpuAiTests
{
    [Fact]
    public void EasyDecisionsAreReproducibleForTheSameSeedAndState()
    {
        var catalog = CardCatalog.CreateDefault();
        var match = CreateStandardOnlyMatch(catalog, 424242UL, 2);
        var options = new CpuAiOptions(CpuDifficulty.Easy, seed: 998877UL, hasDrawnThisTurn: true);

        var first = CpuAi.Decide(match, match.CurrentPlayer.Id, catalog, options);
        var second = CpuAi.Decide(match, match.CurrentPlayer.Id, catalog, options);

        Assert.Equal(first.Action.Kind, second.Action.Kind);
        Assert.Equal(first.Action.CardInstanceId, second.Action.CardInstanceId);
        Assert.Equal(first.Action.TargetPlayerId, second.Action.TargetPlayerId);
        Assert.Equal(first.Action.ChosenSuit, second.Action.ChosenSuit);
    }

    [Fact]
    public void EasySelectsRandomLegalOptionsWhileNormalPrioritizesHighValueCards()
    {
        var catalog = CardCatalog.CreateDefault();
        var match = CreateStandardOnlyMatch(catalog, 771122UL, 2);
        var cpuId = match.CurrentPlayer.Id;
        var normal = CpuAi.Decide(match, cpuId, catalog,
            new CpuAiOptions(CpuDifficulty.Normal, seed: 12UL, hasDrawnThisTurn: true));

        Assert.Equal(CpuActionKind.PlayCard, normal.Action.Kind);
        var selectedNormalCard = match.CurrentPlayer.Hand.Single(c => c.InstanceId == normal.Action.CardInstanceId);
        int highestAvailableValue = match.CurrentPlayer.Hand.Max(c => c.Definition.Value);
        Assert.Equal(highestAvailableValue, selectedNormalCard.Definition.Value);

        var easySelections = Enumerable.Range(1, 80)
            .Select(seed => CpuAi.Decide(match, cpuId, catalog,
                new CpuAiOptions(CpuDifficulty.Easy, seed: (ulong)seed, hasDrawnThisTurn: true)))
            .Select(decision => decision.Action.Kind + "|" + decision.Action.CardInstanceId)
            .Distinct()
            .Count();

        Assert.True(easySelections >= 3,
            "Easy should sample multiple legal actions rather than always choose a fixed card.");
    }

    [Fact]
    public void HardWeightsScoreThreatMoreStronglyThanNormal()
    {
        var catalog = CardCatalog.CreateDefault();
        var seats = new[]
        {
            new SeatDefinition("cpu", "CPU", SeatKind.Cpu),
            new SeatDefinition("high-score", "High Score", SeatKind.Human),
            new SeatDefinition("large-hand", "Large Hand", SeatKind.Human)
        };
        var match = MatchFactory.Create("hard-threat", 707070UL, seats, catalog,
            new Ruleset(startingHandSize: 1, maximumHandSize: 7, maximumPlayers: 3, targetScore: 100));

        match.CurrentPlayer.MutableHand.Clear();
        match.CurrentPlayer.MutableHand.Add(new CardInstance("cpu-steal-target-card", catalog.Get("attack-steal")));

        match.MutablePlayers[1].Score = 50;
        match.MutablePlayers[1].MutableHand.Clear();
        match.MutablePlayers[1].MutableHand.Add(new CardInstance("high-score-held", catalog.Get("standard-hearts-2")));

        match.MutablePlayers[2].Score = 0;
        match.MutablePlayers[2].MutableHand.Clear();
        for (int index = 0; index < 7; index++)
            match.MutablePlayers[2].MutableHand.Add(new CardInstance(
                "large-hand-held-" + index, catalog.Get("standard-clubs-2")));

        var normal = CpuAi.Decide(match, "cpu", catalog,
            new CpuAiOptions(CpuDifficulty.Normal, hasDrawnThisTurn: true));
        var hard = CpuAi.Decide(match, "cpu", catalog,
            new CpuAiOptions(CpuDifficulty.Hard, hasDrawnThisTurn: true));

        Assert.Equal("large-hand", normal.Action.TargetPlayerId);
        Assert.Equal("high-score", hard.Action.TargetPlayerId);
    }

    [Fact]
    public void ExpertUsesPublicOpponentBehaviorWithoutReadingOpponentHands()
    {
        var catalog = CardCatalog.CreateDefault();
        var seats = new[]
        {
            new SeatDefinition("cpu", "CPU", SeatKind.Cpu),
            new SeatDefinition("target-a", "Target A", SeatKind.Human),
            new SeatDefinition("target-b", "Target B", SeatKind.Cpu)
        };
        var match = MatchFactory.Create("expert-threat", 808080UL, seats, catalog,
            new Ruleset(startingHandSize: 1, maximumHandSize: 7, maximumPlayers: 3, targetScore: 100));

        match.CurrentPlayer.MutableHand.Clear();
        match.CurrentPlayer.MutableHand.Add(new CardInstance("cpu-steal-card", catalog.Get("attack-steal")));
        match.MutablePlayers[1].MutableHand.Clear();
        match.MutablePlayers[1].MutableHand.Add(new CardInstance("target-a-known", catalog.Get("standard-hearts-2")));
        match.MutablePlayers[2].MutableHand.Clear();
        match.MutablePlayers[2].MutableHand.Add(new CardInstance("target-b-known", catalog.Get("standard-clubs-2")));
        match.MutableEvents.Add(new GameEvent(10, "CardPlayed", "target-b", "public-card-1", null, CardEffect.StealCard.ToString()));
        match.MutableEvents.Add(new GameEvent(11, "CardPlayed", "target-b", "public-card-2", null, CardEffect.BlockDraw.ToString()));

        var observation = CpuAi.Observe(match, "cpu", catalog);
        var opponentViewProperties = typeof(CpuOpponentObservation)
            .GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Select(property => property.Name)
            .ToArray();

        Assert.Contains("HandCount", opponentViewProperties);
        Assert.DoesNotContain("Hand", opponentViewProperties);
        Assert.DoesNotContain("Cards", opponentViewProperties);
        Assert.Equal(1, observation.Opponents.Single(p => p.Id == "target-a").HandCount);
        Assert.Equal(1, observation.Opponents.Single(p => p.Id == "target-b").HandCount);

        var normal = CpuAi.Decide(match, "cpu", catalog,
            new CpuAiOptions(CpuDifficulty.Normal, hasDrawnThisTurn: true, useOpponentModeling: true));
        var expert = CpuAi.Decide(match, "cpu", catalog,
            new CpuAiOptions(CpuDifficulty.Expert, hasDrawnThisTurn: true, useOpponentModeling: true));

        Assert.Equal(CpuActionKind.PlayCard, normal.Action.Kind);
        Assert.Equal("target-a", normal.Action.TargetPlayerId);
        Assert.Equal(CpuActionKind.PlayCard, expert.Action.Kind);
        Assert.Equal("target-b", expert.Action.TargetPlayerId);
    }

    [Fact]
    public void CpuSimulationCompletesMatchesForEveryDifficultyAndUpToThreeOpponents()
    {
        var difficulties = new[]
        {
            CpuDifficulty.Easy,
            CpuDifficulty.Normal,
            CpuDifficulty.Hard,
            CpuDifficulty.Expert
        };

        foreach (var difficulty in difficulties)
        {
            for (int playerCount = 2; playerCount <= 4; playerCount++)
            {
              for (int seedOffset = 0; seedOffset < 10; seedOffset++)
              {
                var catalog = CardCatalog.CreateDefault();
                var seats = Enumerable.Range(0, playerCount)
                    .Select(index => new SeatDefinition(
                        "seat-" + index,
                        index == 0 ? "Human Driver" : "CPU " + index,
                        index == 0 ? SeatKind.Human : SeatKind.Cpu))
                    .ToArray();

                var rules = new Ruleset(
                    startingHandSize: 5,
                    maximumHandSize: 7,
                    maximumPlayers: playerCount,
                    allowStacking: false,
                    targetScore: 45);
                var seed = 900000UL + (ulong)playerCount * 1000UL +
                           (ulong)difficulty * 10UL + (ulong)seedOffset;
                var match = MatchFactory.Create(
                    "cpu-sim-" + difficulty + "-" + playerCount + "-" + seedOffset,
                    seed,
                    seats,
                    catalog,
                    rules);

                int commands = 0;
                while (match.Status == MatchStatus.Active && commands < 2500)
                {
                    var current = match.CurrentPlayer;
                    var cpuDifficulty = current.Kind == SeatKind.Cpu ? difficulty : CpuDifficulty.Hard;
                    var options = new CpuAiOptions(
                        cpuDifficulty,
                        seed: seed,
                        hasDrawnThisTurn: false,
                        maximumEvaluations: 192,
                        planningDepth: 4,
                        useDeckCompositionEstimates: cpuDifficulty >= CpuDifficulty.Hard,
                        useOpponentModeling: cpuDifficulty == CpuDifficulty.Expert);

                    var decision = CpuAi.Decide(match, current.Id, catalog, options);
                    if (decision.Action.Kind == CpuActionKind.Draw)
                    {
                        var draw = GameEngine.Draw(match, current.Id);
                        Assert.True(draw.Success, "CPU draw was rejected: " + draw.Error);
                        commands++;

                        if (match.Status != MatchStatus.Active || match.CurrentPlayer.Id != current.Id)
                            continue;

                        decision = CpuAi.Decide(match, current.Id, catalog,
                            new CpuAiOptions(
                                cpuDifficulty,
                                seed: seed,
                                hasDrawnThisTurn: true,
                                maximumEvaluations: 192,
                                planningDepth: 4,
                                useDeckCompositionEstimates: cpuDifficulty >= CpuDifficulty.Hard,
                                useOpponentModeling: cpuDifficulty == CpuDifficulty.Expert));
                        Assert.NotEqual(CpuActionKind.Draw, decision.Action.Kind);
                    }

                    var result = Apply(match, current.Id, decision.Action);
                    Assert.True(result.Success,
                        "CPU simulation command was rejected. Difficulty=" + difficulty +
                        ", players=" + playerCount + ", action=" + decision.Action.Kind +
                        ", error=" + result.Error);
                    commands++;
                }

                Assert.True(match.Status != MatchStatus.Active,
                    "Match failed to complete. Difficulty=" + difficulty + ", players=" + playerCount +
                    ", seed=" + seed + ", turn=" + match.TurnNumber + ", deck=" + match.DrawPileCount +
                    ", streaks=" + string.Join(",", match.ConsecutiveNonScoringTurns.Select(pair =>
                        pair.Key + ":" + pair.Value)));
                Assert.InRange(commands, 1, 2500);
                if (match.Status == MatchStatus.Completed)
                    Assert.Contains(match.Players, player => player.Id == match.WinnerPlayerId);
              }
            }
        }
    }

    [Fact]
    public void CpuOptionsBoundComputeAndPlanningBudgets()
    {
        var options = new CpuAiOptions(
            CpuDifficulty.Expert,
            maximumEvaluations: 10000,
            planningDepth: 0);

        Assert.Equal(256, options.MaximumEvaluations);
        Assert.Equal(1, options.PlanningDepth);
    }

    private static MatchState CreateStandardOnlyMatch(CardCatalog catalog, ulong seed, int playerCount)
    {
        var seats = new[]
        {
            new SeatDefinition("cpu", "CPU", SeatKind.Cpu),
            new SeatDefinition("human", "Human", SeatKind.Human)
        };
        var standardIds = catalog.Definitions
            .Where(definition => definition.Kind == CardKind.Standard)
            .Select(definition => definition.Id)
            .ToArray();
        return MatchFactory.Create(
            "cpu-ai-" + seed,
            seed,
            seats.Take(playerCount).ToArray(),
            catalog,
            new Ruleset(startingHandSize: 5, maximumHandSize: 7, maximumPlayers: playerCount, targetScore: 100),
            standardIds);
    }

    private static ActionResult Apply(MatchState match, string playerId, CpuAiAction action)
    {
        switch (action.Kind)
        {
            case CpuActionKind.Draw:
                return GameEngine.Draw(match, playerId);
            case CpuActionKind.PlayCard:
                return GameEngine.PlayCard(match, playerId, action.CardInstanceId!,
                    action.TargetPlayerId, action.ChosenSuit);
            case CpuActionKind.PlayDefense:
                return GameEngine.RespondToAttack(match, playerId, action.CardInstanceId);
            case CpuActionKind.AcceptAttack:
                return GameEngine.RespondToAttack(match, playerId, null);
            case CpuActionKind.EndTurn:
                return GameEngine.EndTurn(match, playerId);
            default:
                throw new InvalidOperationException("Unhandled CPU action: " + action.Kind);
        }
    }
}
