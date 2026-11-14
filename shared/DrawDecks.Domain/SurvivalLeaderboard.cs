#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;

namespace DrawDecks.Domain
{
    /// <summary>
    /// Stable result projection for local or server-side survival leaderboards.
    /// It intentionally excludes private hands and mutable match internals.
    /// </summary>
    public sealed class SurvivalLeaderboardEntry
    {
        public string MatchId { get; }
        public string PlayerId { get; }
        public int Score { get; }
        public int WavesCompleted { get; }
        public ulong Seed { get; }
        public string CatalogVersion { get; }

        private SurvivalLeaderboardEntry(string matchId, string playerId, int score,
            int wavesCompleted, ulong seed, string catalogVersion)
        {
            MatchId = matchId; PlayerId = playerId; Score = score;
            WavesCompleted = wavesCompleted; Seed = seed; CatalogVersion = catalogVersion;
        }

        public static SurvivalLeaderboardEntry FromCompletedMatch(MatchState match, string playerId)
        {
            if (match == null) throw new ArgumentNullException(nameof(match));
            if (match.Rules.Mode != GameMode.Survival)
                throw new ArgumentException("Only Survival matches produce survival leaderboard entries.", nameof(match));
            if (match.Status != MatchStatus.Completed && match.Status != MatchStatus.Draw)
                throw new InvalidOperationException("A running Survival match cannot be submitted.");
            var player = match.Players.FirstOrDefault(item => item.Id == playerId && item.Kind == SeatKind.Human);
            if (player == null) throw new ArgumentException("Player is not a human participant in this Survival match.", nameof(playerId));
            return new SurvivalLeaderboardEntry(match.MatchId, player.Id, match.SurvivalScore,
                match.SurvivalWave, match.Seed, match.CatalogVersion);
        }
    }

    public static class SurvivalLeaderboard
    {
        public static IReadOnlyList<SurvivalLeaderboardEntry> Rank(IEnumerable<SurvivalLeaderboardEntry> entries)
        {
            if (entries == null) throw new ArgumentNullException(nameof(entries));
            return entries.OrderByDescending(entry => entry.Score)
                .ThenByDescending(entry => entry.WavesCompleted)
                .ThenBy(entry => entry.MatchId, StringComparer.Ordinal)
                .ToArray();
        }
    }
}
