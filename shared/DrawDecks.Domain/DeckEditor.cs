#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;

namespace DrawDecks.Domain
{
    public sealed class CardCatalogFilter
    {
        public string SearchText { get; }
        public CardKind? Kind { get; }
        public CardRarity? Rarity { get; }
        public CardEffect? Effect { get; }
        public bool IncludeUnavailable { get; }

        public CardCatalogFilter(string? searchText = null, CardKind? kind = null,
            CardRarity? rarity = null, CardEffect? effect = null, bool includeUnavailable = false)
        {
            SearchText = searchText?.Trim() ?? string.Empty;
            Kind = kind;
            Rarity = rarity;
            Effect = effect;
            IncludeUnavailable = includeUnavailable;
        }
    }

    public static class CardCatalogSearch
    {
        public static IReadOnlyList<CardDefinition> Search(CardCatalog catalog, CardCatalogFilter? filter = null)
        {
            if (catalog == null) throw new ArgumentNullException(nameof(catalog));
            filter = filter ?? new CardCatalogFilter();
            IEnumerable<CardDefinition> cards = catalog.Definitions;
            if (!filter.IncludeUnavailable) cards = cards.Where(card => card.IsAvailable);
            if (filter.Kind.HasValue) cards = cards.Where(card => card.Kind == filter.Kind.Value);
            if (filter.Rarity.HasValue) cards = cards.Where(card => card.Rarity == filter.Rarity.Value);
            if (filter.Effect.HasValue) cards = cards.Where(card => card.Effect == filter.Effect.Value);
            if (filter.SearchText.Length > 0)
                cards = cards.Where(card => card.Id.IndexOf(filter.SearchText, StringComparison.OrdinalIgnoreCase) >= 0 ||
                    card.Name.IndexOf(filter.SearchText, StringComparison.OrdinalIgnoreCase) >= 0);
            return cards.OrderBy(card => card.Name, StringComparer.Ordinal).ToArray();
        }
    }

    /// <summary>Mutable deck editing session; Build produces an immutable, versioned profile.</summary>
    public sealed class DeckBuilderSession
    {
        private readonly CardCatalog catalog;
        private readonly RulesetProfile ruleset;
        private readonly List<string> cardIds;
        public string DeckId { get; }
        public string DeckName { get; private set; }
        public IReadOnlyList<string> CardDefinitionIds => cardIds.AsReadOnly();

        public DeckBuilderSession(CardCatalog catalog, RulesetProfile ruleset,
            string deckId, string deckName, IEnumerable<string>? initialCardIds = null)
        {
            this.catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
            this.ruleset = ruleset ?? throw new ArgumentNullException(nameof(ruleset));
            if (string.IsNullOrWhiteSpace(deckId)) throw new ArgumentException("Deck ID is required.", nameof(deckId));
            if (string.IsNullOrWhiteSpace(deckName)) throw new ArgumentException("Deck name is required.", nameof(deckName));
            DeckId = deckId.Trim();
            DeckName = deckName.Trim();
            cardIds = (initialCardIds ?? Array.Empty<string>()).ToList();
        }

        public void Rename(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Deck name is required.", nameof(name));
            DeckName = name.Trim();
        }

        public bool TryAdd(string definitionId, out string error)
        {
            error = string.Empty;
            if (!catalog.TryGet(definitionId, out var definition) || definition == null)
            {
                error = "Unknown card definition.";
                return false;
            }
            if (!definition.IsAvailable)
            {
                error = "Card is not available.";
                return false;
            }
            if (ruleset.BannedCardIds.Contains(definitionId, StringComparer.Ordinal))
            {
                error = "Card is banned by this ruleset.";
                return false;
            }
            if (ruleset.EnabledCardIds.Count > 0 && !ruleset.EnabledCardIds.Contains(definitionId, StringComparer.Ordinal))
            {
                error = "Card is not enabled by this ruleset.";
                return false;
            }
            if (!ruleset.SpecialCardsEnabled && definition.Kind == CardKind.Special)
            {
                error = "Special cards are disabled by this ruleset.";
                return false;
            }
            if (ruleset.RestrictedEffects.Contains(definition.Effect.ToString(), StringComparer.Ordinal))
            {
                error = "This card effect is restricted by the ruleset.";
                return false;
            }
            int copies = cardIds.Count(id => StringComparer.Ordinal.Equals(id, definitionId));
            if (copies >= Math.Min(ruleset.DuplicateCardLimit, definition.MaxCopies))
            {
                error = "Duplicate card limit reached.";
                return false;
            }
            if (cardIds.Count >= ruleset.DeckMaximumSize)
            {
                error = "Deck maximum size reached.";
                return false;
            }
            cardIds.Add(definitionId);
            return true;
        }

        public bool Remove(string definitionId)
        {
            int index = cardIds.FindIndex(id => StringComparer.Ordinal.Equals(id, definitionId));
            if (index < 0) return false;
            cardIds.RemoveAt(index);
            return true;
        }

        public void Clear() => cardIds.Clear();

        public DeckValidationResult Validate()
        {
            var errors = new List<string>();
            if (cardIds.Count < ruleset.DeckMinimumSize || cardIds.Count > ruleset.DeckMaximumSize)
                errors.Add("Deck size is outside the selected ruleset bounds.");
            foreach (var group in cardIds.GroupBy(id => id, StringComparer.Ordinal))
            {
                if (!catalog.TryGet(group.Key, out var definition) || definition == null)
                {
                    errors.Add("Unknown card definition: " + group.Key);
                    continue;
                }
                if (group.Count() > Math.Min(ruleset.DuplicateCardLimit, definition.MaxCopies))
                    errors.Add("Copy limit exceeded: " + group.Key);
                if (ruleset.BannedCardIds.Contains(group.Key, StringComparer.Ordinal))
                    errors.Add("Banned card included: " + group.Key);
                if (ruleset.EnabledCardIds.Count > 0 && !ruleset.EnabledCardIds.Contains(group.Key, StringComparer.Ordinal))
                    errors.Add("Card is not enabled: " + group.Key);
                if (!ruleset.SpecialCardsEnabled && definition.Kind == CardKind.Special)
                    errors.Add("Special cards are disabled: " + group.Key);
                if (ruleset.RestrictedEffects.Contains(definition.Effect.ToString(), StringComparer.Ordinal))
                    errors.Add("Restricted effect included: " + group.Key);
            }
            return new DeckValidationResult(errors);
        }

        public DeckProfile Build(bool starterDeck = false)
        {
            var result = Validate();
            if (!result.IsValid) throw new InvalidOperationException(string.Join(" ", result.Errors));
            return new DeckProfile(DeckId, DeckName, catalog.Version, cardIds, starterDeck);
        }
    }

    public sealed class ConfiguredMatchResult
    {
        public MatchState? Match { get; }
        public IReadOnlyList<string> Errors { get; }
        public bool Success => Match != null && Errors.Count == 0;

        internal ConfiguredMatchResult(MatchState? match, IEnumerable<string> errors)
        {
            Match = match;
            Errors = errors.ToArray();
        }
    }

    public static class ConfiguredMatchFactory
    {
        /// <summary>
        /// Validates a saved deck and ruleset against the authoritative catalog before
        /// creating a deterministic match. Mode-specific engines are intentionally gated
        /// until their state/effect implementations are available.
        /// </summary>
        public static ConfiguredMatchResult Create(string matchId, ulong seed,
            IReadOnlyList<SeatDefinition> seats, CardCatalog catalog,
            DeckProfile deck, RulesetProfile profile)
        {
            if (seats == null) throw new ArgumentNullException(nameof(seats));
            if (catalog == null) throw new ArgumentNullException(nameof(catalog));
            if (deck == null) throw new ArgumentNullException(nameof(deck));
            if (profile == null) throw new ArgumentNullException(nameof(profile));

            var errors = new List<string>(profile.Validate(catalog, seats.Count, seats.Count(seat => seat.Kind == SeatKind.Cpu)));
            if (!StringComparer.Ordinal.Equals(deck.CatalogVersion, catalog.Version))
                errors.Add("Deck catalog version does not match the active catalog.");
            var deckValidation = deck.Validate(catalog, profile.DeckMinimumSize, profile.DeckMaximumSize);
            errors.AddRange(deckValidation.Errors);
            if (deck.CardDefinitionIds.Count < seats.Count * profile.StartingHandSize)
                errors.Add("Deck does not contain enough cards to deal every starting hand.");
            foreach (var group in deck.CardDefinitionIds.GroupBy(id => id, StringComparer.Ordinal))
            {
                if (!catalog.TryGet(group.Key, out var definition) || definition == null) continue;
                if (group.Count() > Math.Min(profile.DuplicateCardLimit, definition.MaxCopies))
                    errors.Add("Deck exceeds duplicate card limit: " + group.Key);
                if (profile.BannedCardIds.Contains(group.Key, StringComparer.Ordinal))
                    errors.Add("Deck contains banned card: " + group.Key);
                if (profile.EnabledCardIds.Count > 0 && !profile.EnabledCardIds.Contains(group.Key, StringComparer.Ordinal))
                    errors.Add("Deck contains a card not enabled by the ruleset: " + group.Key);
                if (!profile.SpecialCardsEnabled && definition.Kind == CardKind.Special)
                    errors.Add("Deck contains a special card while special cards are disabled: " + group.Key);
                if (profile.RestrictedEffects.Contains(definition.Effect.ToString(), StringComparer.Ordinal))
                    errors.Add("Deck contains a restricted effect: " + group.Key);
            }
            if (errors.Count > 0) return new ConfiguredMatchResult(null, errors.Distinct(StringComparer.Ordinal));

            if (profile.Mode != GameMode.Classic && profile.Mode != GameMode.Custom)
                return new ConfiguredMatchResult(null, new[]
                {
                    profile.Mode + " is configured and validated, but its dedicated runtime rules are not implemented yet."
                });

            try
            {
                var match = MatchFactory.Create(matchId, seed, seats, catalog,
                    profile.ToEngineRuleset(), deck.CardDefinitionIds);
                return new ConfiguredMatchResult(match, Array.Empty<string>());
            }
            catch (ArgumentException exception)
            {
                return new ConfiguredMatchResult(null, new[] { exception.Message });
            }
        }
    }
}
