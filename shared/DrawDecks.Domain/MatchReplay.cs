#nullable enable
using System;
using System.Linq;

namespace DrawDecks.Domain
{

public static class MatchReplay
{
    public static MatchState Replay(MatchState source, CardCatalog catalog)
    {
        if (source == null) throw new ArgumentNullException(nameof(source));
        if (catalog == null) throw new ArgumentNullException(nameof(catalog));
        if (!StringComparer.Ordinal.Equals(source.CatalogVersion, catalog.Version))
            throw new InvalidOperationException("Replay catalog version does not match the original match.");

        var replay = MatchFactory.Create(source.MatchId, source.Seed, source.Seats, catalog, source.Rules, source.DeckDefinitionIds);
        foreach (var command in source.Events.Where(e => e.Type == "CommandAccepted"))
        {
            var parts = command.Data.Split('|');
            ActionResult result;
            switch (parts[0])
            {
                case "Draw":
                    result = GameEngine.Draw(replay, command.PlayerId, int.Parse(parts[1], System.Globalization.CultureInfo.InvariantCulture));
                    break;
                case "PlayCard":
                    result = GameEngine.PlayCard(replay, command.PlayerId, command.CardInstanceId!,
                        command.TargetPlayerId, parts.Length > 1 ? Enum.Parse<Suit>(parts[1]) : Suit.None);
                    break;
                case "RespondToAttack":
                    result = GameEngine.RespondToAttack(replay, command.PlayerId, parts.Length > 1 && parts[1] != "-" ? parts[1] : null);
                    break;
                case "Discard":
                    result = GameEngine.Discard(replay, command.PlayerId, command.CardInstanceId!);
                    break;
                case "EndTurn":
                    result = GameEngine.EndTurn(replay, command.PlayerId);
                    break;
                case "EliminatePlayer":
                    result = GameEngine.EliminatePlayer(replay, command.PlayerId, command.TargetPlayerId!);
                    break;
                default:
                    throw new InvalidOperationException("Unknown recorded command: " + parts[0]);
            }
            if (!result.Success) throw new InvalidOperationException("Recorded command could not be replayed: " + command.Data + " (" + result.Error + ").");
        }
        return replay;
    }

    private static bool NonScoringCountsEqual(MatchState left, MatchState right)
    {
        if (left.ConsecutiveNonScoringTurns.Count != right.ConsecutiveNonScoringTurns.Count)
            return false;
        return left.ConsecutiveNonScoringTurns.All(pair =>
            right.ConsecutiveNonScoringTurns.TryGetValue(pair.Key, out var count) && count == pair.Value);
    }

    private static bool RulesEqual(Ruleset left, Ruleset right)
    {
        return left.StartingHandSize == right.StartingHandSize &&
               left.MaximumHandSize == right.MaximumHandSize &&
               left.MaximumPlayers == right.MaximumPlayers &&
               left.AllowStacking == right.AllowStacking &&
               left.TargetScore == right.TargetScore &&
               left.MaximumTurns == right.MaximumTurns &&
               left.Mode == right.Mode &&
               left.StartingHealth == right.StartingHealth &&
               left.DamagePerHit == right.DamagePerHit &&
               left.SurvivalMaximumWaves == right.SurvivalMaximumWaves &&
               left.SurvivalScorePerCpu == right.SurvivalScorePerCpu;
    }

    public static bool AreEquivalent(MatchState left, MatchState right)
    {
        if (!RulesEqual(left.Rules,right.Rules) || left.MatchId != right.MatchId || left.Seed != right.Seed || left.CatalogVersion != right.CatalogVersion || left.Status != right.Status || left.WinnerPlayerId != right.WinnerPlayerId || left.WinnerTeamId != right.WinnerTeamId || left.SurvivalWave != right.SurvivalWave || left.SurvivalScore != right.SurvivalScore || left.CpuDefeatedThisWave != right.CpuDefeatedThisWave ||
            left.CurrentPlayer.Id != right.CurrentPlayer.Id || left.TurnNumber != right.TurnNumber ||
            left.DrawPileCount != right.DrawPileCount || left.ActiveSuit != right.ActiveSuit || left.RandomState != right.RandomState || left.Direction != right.Direction || left.DoubleScoreActive != right.DoubleScoreActive || left.BlockedDrawPlayerId != right.BlockedDrawPlayerId || !left.PendingSkipPlayerIds.SetEquals(right.PendingSkipPlayerIds) || left.Events.Count != right.Events.Count || left.TurnHadScoringAction != right.TurnHadScoringAction || !NonScoringCountsEqual(left,right))
            return false;
        for (var i = 0; i < left.Events.Count; i++)
        {
            var a = left.Events[i]; var b = right.Events[i];
            if (a.Sequence != b.Sequence || a.Type != b.Type || a.PlayerId != b.PlayerId ||
                a.CardInstanceId != b.CardInstanceId || a.TargetPlayerId != b.TargetPlayerId || a.Data != b.Data)
                return false;
        }
        for (var i = 0; i < left.Players.Count; i++)
        {
            var a = left.Players[i]; var b = right.Players[i];
            if (a.Id != b.Id || a.Score != b.Score || a.Health != b.Health || a.TeamId != b.TeamId || a.IsEliminated != b.IsEliminated ||
                !a.Hand.Select(c => c.InstanceId).SequenceEqual(b.Hand.Select(c => c.InstanceId)))
                return false;
        }
        if (left.TeamScores.Count != right.TeamScores.Count ||
            left.TeamScores.Any(pair => !right.TeamScores.TryGetValue(pair.Key, out var score) || score != pair.Value))
            return false;
        return left.DiscardPile.Select(c => c.InstanceId).SequenceEqual(right.DiscardPile.Select(c => c.InstanceId)) && left.DrawPile.Select(c => c.InstanceId).SequenceEqual(right.DrawPile.Select(c => c.InstanceId)) && ((left.PendingAttack == null && right.PendingAttack == null) || (left.PendingAttack != null && right.PendingAttack != null && left.PendingAttack.AttackerId == right.PendingAttack.AttackerId && left.PendingAttack.TargetId == right.PendingAttack.TargetId && left.PendingAttack.Effect == right.PendingAttack.Effect));
    }
}
}
