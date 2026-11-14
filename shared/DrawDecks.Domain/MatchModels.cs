#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;

namespace DrawDecks.Domain
{
    public enum SeatKind { Human, Cpu }
    public enum MatchStatus { Active, Completed, Draw }
    public enum ActionError { None, MatchNotActive, NotPlayersTurn, CardNotInHand, InvalidPlayer, InvalidTarget, InvalidPhase, InvalidCardType, HandLimitReached, DeckEmpty, InvalidDefense }

    public sealed class Ruleset
    {
        public int StartingHandSize { get; }
        public int MaximumHandSize { get; }
        public int MaximumPlayers { get; }
        public bool AllowStacking { get; }
        public int TargetScore { get; }
        public int MaximumTurns { get; }
        public GameMode Mode { get; }
        public int StartingHealth { get; }
        public int DamagePerHit { get; }
        public int SurvivalMaximumWaves { get; }
        public int SurvivalScorePerCpu { get; }

        public Ruleset(int startingHandSize = 5, int maximumHandSize = 7, int maximumPlayers = 4,
            bool allowStacking = false, int targetScore = 100, int maximumTurns = 500,
            GameMode mode = GameMode.Classic, int startingHealth = 20, int damagePerHit = 5,
            int survivalMaximumWaves = 0, int survivalScorePerCpu = 10)
        {
            if (startingHandSize < 1 || maximumHandSize < startingHandSize ||
                maximumPlayers < 2 || maximumPlayers > 4 || targetScore < 1 || maximumTurns < 1 ||
                startingHealth < 1 || damagePerHit < 1 || survivalMaximumWaves < 0 || survivalScorePerCpu < 0)
                throw new ArgumentOutOfRangeException(nameof(startingHandSize));
            if (!Enum.IsDefined(typeof(GameMode), mode)) throw new ArgumentOutOfRangeException(nameof(mode));
            if (allowStacking) throw new NotSupportedException("Effect stacking is disabled until its resolution policy is implemented.");
            Mode = mode; StartingHandSize = startingHandSize; MaximumHandSize = maximumHandSize;
            MaximumPlayers = maximumPlayers; AllowStacking = allowStacking; TargetScore = targetScore;
            MaximumTurns = maximumTurns; StartingHealth = startingHealth; DamagePerHit = damagePerHit;
            SurvivalMaximumWaves = survivalMaximumWaves; SurvivalScorePerCpu = survivalScorePerCpu;
            if (mode == GameMode.TeamBattle && maximumPlayers != 4)
                throw new ArgumentException("Team Battle requires exactly four seats.", nameof(maximumPlayers));
        }

        public static Ruleset Default => new();
    }

    public sealed class SeatDefinition
    {
        public string Id { get; }
        public string DisplayName { get; }
        public SeatKind Kind { get; }
        public string? TeamId { get; }
        public SeatDefinition(string id, string name, SeatKind kind, string? teamId = null)
        {
            if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Seat id and name required.");
            if (!Enum.IsDefined(typeof(SeatKind), kind)) throw new ArgumentOutOfRangeException(nameof(kind));
            Id = id; DisplayName = name; Kind = kind;
            TeamId = string.IsNullOrWhiteSpace(teamId) ? null : teamId.Trim();
        }
    }

    public sealed class PlayerState
    {
        internal readonly List<CardInstance> MutableHand = new();
        public string Id { get; }
        public string DisplayName { get; }
        public SeatKind Kind { get; }
        public string? TeamId { get; }
        public IReadOnlyList<CardInstance> Hand => MutableHand.AsReadOnly();
        public int Score { get; internal set; }
        public int Health { get; internal set; }
        public bool IsEliminated { get; internal set; }
        internal PlayerState(SeatDefinition seat, int startingHealth)
        {
            Id = seat.Id; DisplayName = seat.DisplayName; Kind = seat.Kind;
            TeamId = seat.TeamId; Health = startingHealth;
        }
    }

    public sealed class GameEvent
    {
        public long Sequence { get; }
        public string Type { get; }
        public string PlayerId { get; }
        public string? CardInstanceId { get; }
        public string? TargetPlayerId { get; }
        public string Data { get; }
        public GameEvent(long seq, string type, string player, string? card, string? target, string data)
        { Sequence = seq; Type = type; PlayerId = player; CardInstanceId = card; TargetPlayerId = target; Data = data; }
    }

    public sealed class PendingAttack
    {
        public string AttackerId { get; }
        public string TargetId { get; }
        public CardEffect Effect { get; }
        public string CardInstanceId { get; }
        internal PendingAttack(string attacker, string target, CardEffect effect, string card)
        { AttackerId = attacker; TargetId = target; Effect = effect; CardInstanceId = card; }
    }

    public sealed class MatchState
    {
        internal readonly List<PlayerState> MutablePlayers;
        internal readonly List<CardInstance> DrawPile;
        internal readonly List<CardInstance> MutableDiscardPile = new();
        internal readonly List<GameEvent> MutableEvents = new();
        internal readonly Dictionary<string, int> MutableTeamScores = new(StringComparer.Ordinal);
        internal ulong RandomState;
        internal int Direction = 1;
        internal bool DoubleScoreActive;
        internal string? BlockedDrawPlayerId;
        internal readonly HashSet<string> PendingSkipPlayerIds = new(StringComparer.Ordinal);
        internal readonly Dictionary<string, int> ConsecutiveNonScoringTurns = new(StringComparer.Ordinal);
        internal bool TurnHadScoringAction;
        internal long Sequence;

        public string MatchId { get; }
        public ulong Seed { get; }
        public string CatalogVersion { get; }
        public Ruleset Rules { get; }
        public IReadOnlyList<PlayerState> Players => MutablePlayers.AsReadOnly();
        public IReadOnlyList<CardInstance> DiscardPile => MutableDiscardPile.AsReadOnly();
        public int DrawPileCount => DrawPile.Count;
        public IReadOnlyList<GameEvent> Events => MutableEvents.AsReadOnly();
        public IReadOnlyDictionary<string, int> TeamScores => new Dictionary<string, int>(MutableTeamScores, StringComparer.Ordinal);
        public MatchStatus Status { get; internal set; } = MatchStatus.Active;
        public int CurrentPlayerIndex { get; internal set; }
        public int TurnNumber { get; internal set; } = 1;
        public string? WinnerPlayerId { get; internal set; }
        public string? WinnerTeamId { get; internal set; }
        public PendingAttack? PendingAttack { get; internal set; }
        public Suit ActiveSuit { get; internal set; } = Suit.None;
        public IReadOnlyList<SeatDefinition> Seats { get; }
        public IReadOnlyList<string> DeckDefinitionIds { get; }
        public int TurnDirection => Direction;
        public int SurvivalWave { get; internal set; } = 1;
        public int SurvivalScore { get; internal set; }
        public int CpuDefeatedThisWave { get; internal set; }

        internal MatchState(string id, ulong seed, string version, Ruleset rules,
            List<PlayerState> players, List<CardInstance> deck, List<SeatDefinition> seats, List<string> deckIds)
        {
            MatchId = id; Seed = seed; CatalogVersion = version; Rules = rules;
            MutablePlayers = players; DrawPile = deck; Seats = seats.AsReadOnly();
            DeckDefinitionIds = deckIds.AsReadOnly();
            RandomState = seed == 0 ? 0x9E3779B97F4A7C15UL : seed;
            foreach (var player in players)
            {
                ConsecutiveNonScoringTurns[player.Id] = 0;
                if (player.TeamId != null && !MutableTeamScores.ContainsKey(player.TeamId))
                    MutableTeamScores[player.TeamId] = 0;
            }
        }

        internal void Emit(string type, string player, string? card = null, string? target = null, string data = "") =>
            MutableEvents.Add(new GameEvent(++Sequence, type, player, card, target, data));
        public PlayerState CurrentPlayer => MutablePlayers[CurrentPlayerIndex];
    }
}
