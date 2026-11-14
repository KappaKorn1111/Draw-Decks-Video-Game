using System.Linq;
using DrawDecks.Domain;
using Xunit;

namespace DrawDecks.Domain.Tests;

public sealed class ModeRuntimeTests
{
    private static CardCatalog Catalog() => CardCatalog.CreateDefault();

    private static MatchState CreateWithStrikeInHumanHand(GameMode mode, int startingHealth = 5,
        int survivalMaximumWaves = 0, int targetScore = 100)
    {
        var catalog = Catalog();
        var seats = mode == GameMode.TeamBattle
            ? new[]
            {
                new SeatDefinition("human-a", "Human A", SeatKind.Human),
                new SeatDefinition("cpu-b1", "CPU B1", SeatKind.Cpu),
                new SeatDefinition("human-a2", "Human A2", SeatKind.Human),
                new SeatDefinition("cpu-b2", "CPU B2", SeatKind.Cpu)
            }
            : new[]
            {
                new SeatDefinition("human", "Human", SeatKind.Human),
                new SeatDefinition("cpu", "CPU", SeatKind.Cpu)
            };
        var rules = new Ruleset(startingHandSize: 5, maximumHandSize: 9,
            maximumPlayers: mode == GameMode.TeamBattle ? 4 : 2,
            targetScore: targetScore, mode: mode, startingHealth: startingHealth,
            damagePerHit: 5, survivalMaximumWaves: survivalMaximumWaves,
            survivalScorePerCpu: 10);
        var deckIds = catalog.Definitions.Select(card => card.Id)
            .Where(id => id != "attack-damage").ToList();
        deckIds.AddRange(Enumerable.Repeat("attack-damage", 4));

        for (ulong seed = 1; seed < 10000; seed++)
        {
            var match = MatchFactory.Create("mode-test", seed, seats, catalog, rules, deckIds);
            if (match.Players[0].Hand.Any(card => card.DefinitionId == "attack-damage"))
                return match;
        }
        throw new Xunit.Sdk.XunitException("Could not find deterministic seed with Strike in the human opening hand.");
    }

    [Fact]
    public void EliminationStrikeDamagesAndEliminatesPlayerAtZeroHealth()
    {
        var match = CreateWithStrikeInHumanHand(GameMode.Elimination);
        var strike = match.Players[0].Hand.Single(card => card.DefinitionId == "attack-damage");

        Assert.True(GameEngine.PlayCard(match, "human", strike.InstanceId, "cpu").Success);
        Assert.NotNull(match.PendingAttack);
        Assert.True(GameEngine.RespondToAttack(match, "cpu", null).Success);

        Assert.Equal(0, match.Players[1].Health);
        Assert.True(match.Players[1].IsEliminated);
        Assert.Equal(MatchStatus.Completed, match.Status);
        Assert.Equal("human", match.WinnerPlayerId);
        Assert.Contains(match.Events, item => item.Type == "PlayerDamaged");
    }

    [Fact]
    public void TeamBattleAssignsTwoTeamsAndScoresToTheWholeTeam()
    {
        var catalog = Catalog();
        var seats = new[]
        {
            new SeatDefinition("h1", "Human 1", SeatKind.Human),
            new SeatDefinition("c1", "CPU 1", SeatKind.Cpu),
            new SeatDefinition("h2", "Human 2", SeatKind.Human),
            new SeatDefinition("c2", "CPU 2", SeatKind.Cpu)
        };
        var rules = new Ruleset(maximumPlayers: 4, targetScore: 1, mode: GameMode.TeamBattle);
        var deckIds = catalog.Definitions.Select(card => card.Id)
            .Where(id => id != "attack-damage").ToList();
        deckIds.AddRange(Enumerable.Repeat("attack-damage", 4));
        var match = MatchFactory.Create("team-score", 991, seats, catalog, rules, deckIds);
        var scoringCard = match.Players[0].Hand.First(card => card.Definition.Kind == CardKind.Standard);

        Assert.Equal("team-a", match.Players[0].TeamId);
        Assert.Equal("team-b", match.Players[1].TeamId);
        Assert.Equal("team-a", match.Players[2].TeamId);
        Assert.True(GameEngine.PlayCard(match, "h1", scoringCard.InstanceId).Success);

        Assert.True(match.TeamScores["team-a"] >= scoringCard.Definition.Value);
        Assert.Equal("team-a", match.WinnerTeamId);
        Assert.Equal(MatchStatus.Completed, match.Status);
    }

    [Fact]
    public void TeamBattleRejectsFriendlyTargetedDamage()
    {
        var match = CreateWithStrikeInHumanHand(GameMode.TeamBattle);
        var strike = match.Players[0].Hand.First(card => card.DefinitionId == "attack-damage");

        Assert.Equal(ActionError.InvalidTarget,
            GameEngine.ValidatePlayCard(match, "human-a", strike.InstanceId, "human-a2"));
        Assert.Equal(ActionError.None,
            GameEngine.ValidatePlayCard(match, "human-a", strike.InstanceId, "cpu-b1"));
    }

    [Fact]
    public void SurvivalClearsWaveAwardsScoreAndCompletesAtConfiguredWaveLimit()
    {
        var match = CreateWithStrikeInHumanHand(GameMode.Survival, startingHealth: 5, survivalMaximumWaves: 1);
        var strike = match.Players[0].Hand.Single(card => card.DefinitionId == "attack-damage");

        Assert.True(GameEngine.PlayCard(match, "human", strike.InstanceId, "cpu").Success);
        Assert.True(GameEngine.RespondToAttack(match, "cpu", null).Success);

        Assert.Equal(10, match.SurvivalScore);
        Assert.Equal(1, match.SurvivalWave);
        Assert.Equal(MatchStatus.Completed, match.Status);
        Assert.Equal("human", match.WinnerPlayerId);
        Assert.Contains(match.Events, item => item.Type == "SurvivalWaveCleared");
        Assert.Contains(match.Events, item => item.Type == "SurvivalCompleted");
    }

    [Fact]
    public void SurvivalAdvancesWaveAndScalesCpuHealthWhenUnlimitedWavesConfigured()
    {
        var match = CreateWithStrikeInHumanHand(GameMode.Survival, startingHealth: 5, survivalMaximumWaves: 0);
        var strike = match.Players[0].Hand.Single(card => card.DefinitionId == "attack-damage");

        Assert.True(GameEngine.PlayCard(match, "human", strike.InstanceId, "cpu").Success);
        Assert.True(GameEngine.RespondToAttack(match, "cpu", null).Success);

        Assert.Equal(2, match.SurvivalWave);
        Assert.Equal(10, match.SurvivalScore);
        Assert.False(match.Players[1].IsEliminated);
        Assert.Equal(7, match.Players[1].Health);
        Assert.Equal(MatchStatus.Active, match.Status);
        Assert.Contains(match.Events, item => item.Type == "SurvivalWaveStarted" && item.Data.Contains("wave=2"));
    }

    [Fact]
    public void DamageAndTeamStateAreDeterministicallyReplayable()
    {
        var match = CreateWithStrikeInHumanHand(GameMode.Elimination);
        var strike = match.Players[0].Hand.Single(card => card.DefinitionId == "attack-damage");
        Assert.True(GameEngine.PlayCard(match, "human", strike.InstanceId, "cpu").Success);
        Assert.True(GameEngine.RespondToAttack(match, "cpu", null).Success);

        var replay = MatchReplay.Replay(match, Catalog());

        Assert.True(MatchReplay.AreEquivalent(match, replay));
    }
}
