using System;
using System.Linq;
using DrawDecks.Domain;
using Xunit;

namespace DrawDecks.Domain.Tests;

public sealed class StabilityRegressionTests
{
    private static readonly CardCatalog Catalog = CardCatalog.CreateDefault();

    private static SeatDefinition[] FourHumanSeats()
    {
        return Enumerable.Range(0, 4)
            .Select(index => new SeatDefinition("p" + index, "Player " + index, SeatKind.Human))
            .ToArray();
    }

    [Fact]
    public void AttackReactionReturnsTurnOrderToTheAttackerThenAdvances()
    {
        var match = MatchFactory.Create("attack-turn-order", 1001UL, FourHumanSeats(), Catalog);
        var attacker = match.Players[0];
        var nonNextTarget = match.Players[2];

        attacker.MutableHand.Add(new CardInstance("attack-card", Catalog.Get("attack-steal")));

        var play = GameEngine.PlayCard(match, attacker.Id, "attack-card", nonNextTarget.Id);
        Assert.True(play.Success);
        Assert.Equal(nonNextTarget.Id, match.CurrentPlayer.Id);
        Assert.NotNull(match.PendingAttack);

        var response = GameEngine.RespondToAttack(match, nonNextTarget.Id, null);

        Assert.True(response.Success);
        Assert.Null(match.PendingAttack);
        Assert.Equal("p1", match.CurrentPlayer.Id);
    }

    [Fact]
    public void MultipleQueuedSkipEffectsCannotOverwriteOneAnother()
    {
        var match = MatchFactory.Create("multiple-skips", 1002UL, FourHumanSeats(), Catalog);

        match.Players[0].MutableHand.Add(new CardInstance("skip-p3", Catalog.Get("action-skip")));
        var first = GameEngine.PlayCard(match, "p0", "skip-p3", "p3");
        Assert.True(first.Success);
        Assert.Equal("p1", match.CurrentPlayer.Id);

        match.Players[1].MutableHand.Add(new CardInstance("skip-p2", Catalog.Get("action-skip")));
        var second = GameEngine.PlayCard(match, "p1", "skip-p2", "p2");

        Assert.True(second.Success);
        Assert.Equal("p0", match.CurrentPlayer.Id);
        Assert.Equal(2, match.Events.Count(e => e.Type == "TurnSkipped"));
        Assert.Empty(match.PendingSkipPlayerIds);
    }

    [Fact]
    public void EliminatingAQueuedSkipTargetRemovesTheStaleSkip()
    {
        var seats = new[]
        {
            new SeatDefinition("p0", "Player 0", SeatKind.Human),
            new SeatDefinition("p1", "Player 1", SeatKind.Human),
            new SeatDefinition("p2", "Player 2", SeatKind.Human)
        };
        var match = MatchFactory.Create("skip-elimination", 1003UL, seats, Catalog);
        match.Players[0].MutableHand.Add(new CardInstance("skip-p2", Catalog.Get("action-skip")));

        Assert.True(GameEngine.PlayCard(match, "p0", "skip-p2", "p2").Success);
        Assert.Contains("p2", match.PendingSkipPlayerIds);

        Assert.True(GameEngine.EliminatePlayer(match, "p1", "p2").Success);
        Assert.DoesNotContain("p2", match.PendingSkipPlayerIds);
    }

    [Fact]
    public void MatchReplayPreservesQueuedSkipTargets()
    {
        var match = MatchFactory.Create("skip-replay", 1004UL, FourHumanSeats(), Catalog);
        match.Players[0].MutableHand.Add(new CardInstance("skip-p3-replay", Catalog.Get("action-skip")));

        Assert.True(GameEngine.PlayCard(match, "p0", "skip-p3-replay", "p3").Success);
        Assert.Contains("p3", match.PendingSkipPlayerIds);

        var replay = MatchReplay.Replay(match, Catalog);

        Assert.True(MatchReplay.AreEquivalent(match, replay));
    }

    [Fact]
    public void MatchFactoryRejectsSingleSeatMatches()
    {
        var onlySeat = new[] { new SeatDefinition("solo", "Solo", SeatKind.Human) };

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            MatchFactory.Create("one-seat", 1005UL, onlySeat, Catalog));
    }

    [Fact]
    public void DeckValidationReportsNullIdsAndRejectsInvalidBounds()
    {
        var result = DeckBuilder.Validate(Catalog, new string[] { null!, "standard-hearts-2" });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.Contains("cannot be empty", StringComparison.Ordinal));

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            DeckBuilder.Validate(Catalog, Array.Empty<string>(), minimum: 3, maximum: 2));
        Assert.Throws<ArgumentNullException>(() =>
            DeckBuilder.Validate(Catalog, null!));
    }

    [Fact]
    public void CpuDoesNotWasteADefenseAgainstBlockedDrawWhenAlreadyAtHandLimit()
    {
        var seats = new[]
        {
            new SeatDefinition("attacker", "Attacker", SeatKind.Human),
            new SeatDefinition("cpu", "CPU", SeatKind.Cpu)
        };
        var match = MatchFactory.Create("no-op-defense", 1006UL, seats, Catalog);
        var defender = match.Players[1];

        defender.MutableHand.Clear();
        for (int index = 0; index < 6; index++)
        {
            var definition = Catalog.Definitions
                .First(card => card.Kind == CardKind.Standard && card.Id.EndsWith("-" + (index + 2), StringComparison.Ordinal));
            defender.MutableHand.Add(new CardInstance("cpu-hand-" + index, definition));
        }
        defender.MutableHand.Add(new CardInstance("cpu-shield", Catalog.Get("defense-shield")));
        match.Players[0].MutableHand.Add(new CardInstance("block-draw", Catalog.Get("attack-block-draw")));

        Assert.True(GameEngine.PlayCard(match, "attacker", "block-draw", "cpu").Success);
        Assert.NotNull(match.PendingAttack);

        var decision = CpuAi.Decide(match, "cpu", Catalog,
            new CpuAiOptions(CpuDifficulty.Normal, hasDrawnThisTurn: false));

        Assert.Equal(CpuActionKind.AcceptAttack, decision.Action.Kind);
    }
}
