#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;

namespace DrawDecks.Domain
{
    public enum GameMode
    {
        Classic = 0,
        Elimination = 1,
        TeamBattle = 2,
        Survival = 3,
        Custom = 4
    }

    public enum TurnOrderPolicy
    {
        Clockwise = 0,
        CounterClockwise = 1,
        Randomized = 2
    }

    public enum CustomVictoryCondition
    {
        TargetScore = 0,
        LastPlayerStanding = 1,
        TeamScore = 2,
        SurvivalRounds = 3,
        Objective = 4
    }

    /// <summary>
    /// A portable, versioned deck definition. It contains card definition IDs only;
    /// runtime card instances are created by DeckBuilder for each match.
    /// </summary>
    public sealed class DeckProfile
    {
        private readonly string[] cardDefinitionIds;
        public string Id { get; }
        public string Name { get; }
        public string CatalogVersion { get; }
        public IReadOnlyList<string> CardDefinitionIds => Array.AsReadOnly(cardDefinitionIds);
        public bool IsStarterDeck { get; }
        public DateTimeOffset UpdatedAtUtc { get; }

        public DeckProfile(string id, string name, string catalogVersion,
            IEnumerable<string> cardDefinitionIds, bool isStarterDeck = false,
            DateTimeOffset? updatedAtUtc = null)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("Deck ID is required.", nameof(id));
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Deck name is required.", nameof(name));
            if (string.IsNullOrWhiteSpace(catalogVersion)) throw new ArgumentException("Catalog version is required.", nameof(catalogVersion));
            if (cardDefinitionIds == null) throw new ArgumentNullException(nameof(cardDefinitionIds));
            Id = id.Trim();
            Name = name.Trim();
            CatalogVersion = catalogVersion.Trim();
            this.cardDefinitionIds = cardDefinitionIds.ToArray();
            IsStarterDeck = isStarterDeck;
            UpdatedAtUtc = updatedAtUtc ?? DateTimeOffset.UtcNow;
        }

        public DeckValidationResult Validate(CardCatalog catalog, int minimumSize = 1, int maximumSize = 108)
        {
            if (catalog == null) throw new ArgumentNullException(nameof(catalog));
            var errors = new List<string>();
            if (!StringComparer.Ordinal.Equals(CatalogVersion, catalog.Version))
                errors.Add("Deck catalog version does not match the active catalog.");
            var baseResult = DeckBuilder.Validate(catalog, cardDefinitionIds, minimumSize, maximumSize);
            errors.AddRange(baseResult.Errors);
            return new DeckValidationResult(errors);
        }

        public DeckStatistics GetStatistics(CardCatalog catalog)
        {
            if (catalog == null) throw new ArgumentNullException(nameof(catalog));
            var known = cardDefinitionIds.Where(id => catalog.TryGet(id, out _))
                .Select(catalog.Get).ToArray();
            var rarityCounts = known.GroupBy(card => card.Rarity.ToString(), StringComparer.Ordinal)
                .ToDictionary(group => group.Key, group => group.Count(), StringComparer.Ordinal);
            var effectCounts = known.GroupBy(card => card.Effect.ToString(), StringComparer.Ordinal)
                .ToDictionary(group => group.Key, group => group.Count(), StringComparer.Ordinal);
            return new DeckStatistics(cardDefinitionIds.Length, known.Length,
                cardDefinitionIds.Length - known.Length, rarityCounts, effectCounts);
        }
    }

    public sealed class DeckStatistics
    {
        public int TotalCards { get; }
        public int RecognizedCards { get; }
        public int InvalidCards { get; }
        public IReadOnlyDictionary<string, int> RarityCounts { get; }
        public IReadOnlyDictionary<string, int> EffectCounts { get; }

        internal DeckStatistics(int total, int recognized, int invalid,
            IDictionary<string, int> rarities, IDictionary<string, int> effects)
        {
            TotalCards = total;
            RecognizedCards = recognized;
            InvalidCards = invalid;
            RarityCounts = new Dictionary<string, int>(rarities, StringComparer.Ordinal);
            EffectCounts = new Dictionary<string, int>(effects, StringComparer.Ordinal);
        }
    }

    /// <summary>Validated, immutable ruleset data suitable for versioned API transport.</summary>
    public sealed class RulesetProfile
    {
        private readonly string[] enabledCardIds;
        private readonly string[] bannedCardIds;
        private readonly string[] restrictedEffects;
        private readonly string[] objectives;

        public string Id { get; }
        public string Name { get; }
        public int SchemaVersion { get; }
        public GameMode Mode { get; }
        public int MinimumPlayers { get; }
        public int MaximumPlayers { get; }
        public int DeckMinimumSize { get; }
        public int DeckMaximumSize { get; }
        public string StarterDeckId { get; }
        public int DuplicateCardLimit { get; }
        public bool SpecialCardsEnabled { get; }
        public IReadOnlyList<string> EnabledCardIds => Array.AsReadOnly(enabledCardIds);
        public IReadOnlyList<string> BannedCardIds => Array.AsReadOnly(bannedCardIds);
        public IReadOnlyList<string> RestrictedEffects => Array.AsReadOnly(restrictedEffects);
        public int StartingHandSize { get; }
        public int DrawAmount { get; }
        public int MaximumHandSize { get; }
        public int TurnTimerSeconds { get; }
        public TurnOrderPolicy TurnOrder { get; }
        public int TargetScore { get; }
        public int StartingHealth { get; }
        public bool EliminationEnabled { get; }
        public int MaximumRounds { get; }
        public CustomVictoryCondition VictoryCondition { get; }
        public IReadOnlyList<string> Objectives => Array.AsReadOnly(objectives);
        public int CpuOpponentCount { get; }
        public CpuDifficulty CpuDifficulty { get; }
        public int CpuThinkingDelayMilliseconds { get; }
        public DateTimeOffset UpdatedAtUtc { get; }

        public RulesetProfile(string id, string name, GameMode mode,
            IEnumerable<string>? enabledCardIds = null, IEnumerable<string>? bannedCardIds = null,
            IEnumerable<string>? restrictedEffects = null, IEnumerable<string>? objectives = null,
            int schemaVersion = 1, int minimumPlayers = 2, int maximumPlayers = 4,
            int deckMinimumSize = 20, int deckMaximumSize = 108, string starterDeckId = "standard",
            int duplicateCardLimit = 4, bool specialCardsEnabled = true,
            int startingHandSize = 5, int drawAmount = 1, int maximumHandSize = 7,
            int turnTimerSeconds = 0, TurnOrderPolicy turnOrder = TurnOrderPolicy.Clockwise,
            int targetScore = 100, int startingHealth = 20, bool eliminationEnabled = false,
            int maximumRounds = 0, CustomVictoryCondition victoryCondition = CustomVictoryCondition.TargetScore,
            int cpuOpponentCount = 1, CpuDifficulty cpuDifficulty = CpuDifficulty.Normal,
            int cpuThinkingDelayMilliseconds = 700, DateTimeOffset? updatedAtUtc = null)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("Ruleset ID is required.", nameof(id));
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Ruleset name is required.", nameof(name));
            if (!Enum.IsDefined(typeof(GameMode), mode)) throw new ArgumentOutOfRangeException(nameof(mode));
            if (!Enum.IsDefined(typeof(TurnOrderPolicy), turnOrder)) throw new ArgumentOutOfRangeException(nameof(turnOrder));
            if (!Enum.IsDefined(typeof(CustomVictoryCondition), victoryCondition)) throw new ArgumentOutOfRangeException(nameof(victoryCondition));
            if (!Enum.IsDefined(typeof(CpuDifficulty), cpuDifficulty)) throw new ArgumentOutOfRangeException(nameof(cpuDifficulty));
            Id = id.Trim();
            Name = name.Trim();
            Mode = mode;
            SchemaVersion = schemaVersion;
            MinimumPlayers = minimumPlayers;
            MaximumPlayers = maximumPlayers;
            DeckMinimumSize = deckMinimumSize;
            DeckMaximumSize = deckMaximumSize;
            StarterDeckId = starterDeckId ?? string.Empty;
            DuplicateCardLimit = duplicateCardLimit;
            SpecialCardsEnabled = specialCardsEnabled;
            this.enabledCardIds = (enabledCardIds ?? Array.Empty<string>()).Distinct(StringComparer.Ordinal).ToArray();
            this.bannedCardIds = (bannedCardIds ?? Array.Empty<string>()).Distinct(StringComparer.Ordinal).ToArray();
            this.restrictedEffects = (restrictedEffects ?? Array.Empty<string>()).Distinct(StringComparer.Ordinal).ToArray();
            StartingHandSize = startingHandSize;
            DrawAmount = drawAmount;
            MaximumHandSize = maximumHandSize;
            TurnTimerSeconds = turnTimerSeconds;
            TurnOrder = turnOrder;
            TargetScore = targetScore;
            StartingHealth = startingHealth;
            EliminationEnabled = eliminationEnabled;
            MaximumRounds = maximumRounds;
            VictoryCondition = victoryCondition;
            this.objectives = (objectives ?? Array.Empty<string>()).Where(value => !string.IsNullOrWhiteSpace(value)).ToArray();
            CpuOpponentCount = cpuOpponentCount;
            CpuDifficulty = cpuDifficulty;
            CpuThinkingDelayMilliseconds = cpuThinkingDelayMilliseconds;
            UpdatedAtUtc = updatedAtUtc ?? DateTimeOffset.UtcNow;
        }

        public IReadOnlyList<string> Validate(CardCatalog catalog, int seatCount, int cpuSeatCount)
        {
            if (catalog == null) throw new ArgumentNullException(nameof(catalog));
            var errors = new List<string>();
            if (SchemaVersion != 1) errors.Add("Unsupported ruleset schema version.");
            if (MinimumPlayers < 2 || MaximumPlayers > 4 || MaximumPlayers < MinimumPlayers)
                errors.Add("Player bounds must be between two and four, with maximum >= minimum.");
            if (seatCount < MinimumPlayers || seatCount > MaximumPlayers)
                errors.Add("Seat count is outside the ruleset player bounds.");
            if (cpuSeatCount < 0 || cpuSeatCount > seatCount - 1)
                errors.Add("CPU seat count is invalid; at least one human seat is required.");
            if (cpuSeatCount != CpuOpponentCount)
                errors.Add("CPU seat count does not match the configured CPU opponent count.");
            if (cpuSeatCount != CpuOpponentCount)
                errors.Add("CPU seat count does not match the configured CPU opponent count.");
            if (CpuOpponentCount < 0 || CpuOpponentCount > MaximumPlayers - 1)
                errors.Add("Configured CPU opponent count is invalid.");
            if (DeckMinimumSize < 1 || DeckMaximumSize < DeckMinimumSize || DeckMaximumSize > 108)
                errors.Add("Deck size bounds must be between 1 and 108.");
            if (DuplicateCardLimit < 1 || StartingHandSize < 1 || MaximumHandSize < StartingHandSize)
                errors.Add("Duplicate limit and hand-size settings are invalid.");
            if (DrawAmount < 0 || TurnTimerSeconds < 0 || TargetScore < 1 || StartingHealth < 1 ||
                MaximumRounds < 0 || CpuThinkingDelayMilliseconds < 0)
                errors.Add("Draw, timer, victory, health, round, or CPU delay settings are invalid.");
            if (Mode == GameMode.TeamBattle && (seatCount != 4 || seatCount % 2 != 0))
                errors.Add("Team Battle requires exactly four seats (2v2).");
            if (Mode == GameMode.Elimination && !EliminationEnabled)
                errors.Add("Elimination mode requires elimination to be enabled.");
            if (Mode == GameMode.Survival && cpuSeatCount < 1)
                errors.Add("Survival mode requires at least one CPU opponent.");
            if (VictoryCondition == CustomVictoryCondition.Objective && objectives.Length == 0)
                errors.Add("Objective victory requires at least one objective.");
            if (TurnOrder == TurnOrderPolicy.Randomized && Mode == GameMode.TeamBattle)
                errors.Add("Team Battle currently requires a stable clockwise turn order.");
            foreach (var id in enabledCardIds.Concat(bannedCardIds))
                if (!catalog.TryGet(id, out _)) errors.Add("Unknown card definition: " + id);
            var overlap = enabledCardIds.Intersect(bannedCardIds, StringComparer.Ordinal).ToArray();
            if (overlap.Length > 0) errors.Add("A card cannot be both enabled and banned: " + string.Join(", ", overlap));
            return errors.AsReadOnly();
        }

        public Ruleset ToEngineRuleset()
        {
            return new Ruleset(StartingHandSize, MaximumHandSize, MaximumPlayers,
                false, TargetScore, MaximumRounds > 0 ? MaximumRounds : 500);
        }
    }

    public static class RulesetPresets
    {
        public static IReadOnlyList<RulesetProfile> All => new[]
        {
            new RulesetProfile("classic", "Classic Match", GameMode.Classic,
                targetScore: 100, victoryCondition: CustomVictoryCondition.TargetScore),
            new RulesetProfile("elimination", "Elimination", GameMode.Elimination,
                targetScore: 100, startingHealth: 20, eliminationEnabled: true,
                victoryCondition: CustomVictoryCondition.LastPlayerStanding),
            new RulesetProfile("team-battle", "Team Battle 2v2", GameMode.TeamBattle,
                minimumPlayers: 4, maximumPlayers: 4, targetScore: 150,
                victoryCondition: CustomVictoryCondition.TeamScore),
            new RulesetProfile("survival", "Survival", GameMode.Survival,
                minimumPlayers: 2, maximumPlayers: 4, cpuOpponentCount: 3,
                cpuDifficulty: CpuDifficulty.Normal, victoryCondition: CustomVictoryCondition.SurvivalRounds),
            new RulesetProfile("custom", "Custom Rules", GameMode.Custom,
                victoryCondition: CustomVictoryCondition.TargetScore)
        };
    }

    public interface ICustomizationStore
    {
        void SaveDeck(DeckProfile deck);
        DeckProfile? LoadDeck(string id);
        IReadOnlyList<DeckProfile> ListDecks();
        void SaveRuleset(RulesetProfile ruleset);
        RulesetProfile? LoadRuleset(string id);
        IReadOnlyList<RulesetProfile> ListRulesets();
        bool DeleteDeck(string id);
        bool DeleteRuleset(string id);
    }

    /// <summary>
    /// Storage contract for local profile persistence. Platform clients should provide
    /// a durable implementation (Unity persistentDataPath, browser IndexedDB, or server).
    /// </summary>
    public sealed class InMemoryCustomizationStore : ICustomizationStore
    {
        private readonly Dictionary<string, DeckProfile> decks = new Dictionary<string, DeckProfile>(StringComparer.Ordinal);
        private readonly Dictionary<string, RulesetProfile> rulesets = new Dictionary<string, RulesetProfile>(StringComparer.Ordinal);

        public void SaveDeck(DeckProfile deck)
        {
            if (deck == null) throw new ArgumentNullException(nameof(deck));
            decks[deck.Id] = deck;
        }
        public DeckProfile? LoadDeck(string id) => id != null && decks.TryGetValue(id, out var value) ? value : null;
        public IReadOnlyList<DeckProfile> ListDecks() => decks.Values.OrderBy(value => value.Name, StringComparer.Ordinal).ToArray();
        public void SaveRuleset(RulesetProfile ruleset)
        {
            if (ruleset == null) throw new ArgumentNullException(nameof(ruleset));
            rulesets[ruleset.Id] = ruleset;
        }
        public RulesetProfile? LoadRuleset(string id) => id != null && rulesets.TryGetValue(id, out var value) ? value : null;
        public IReadOnlyList<RulesetProfile> ListRulesets() => rulesets.Values.OrderBy(value => value.Name, StringComparer.Ordinal).ToArray();
        public bool DeleteDeck(string id) => id != null && decks.Remove(id);
        public bool DeleteRuleset(string id) => id != null && rulesets.Remove(id);
    }
}
