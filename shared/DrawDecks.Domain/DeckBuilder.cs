#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;

namespace DrawDecks.Domain
{
    public sealed class DeckValidationResult
    {
        public IReadOnlyList<string> Errors { get; }
        public bool IsValid => Errors.Count == 0;

        internal DeckValidationResult(IEnumerable<string> errors)
        {
            Errors = errors.ToArray();
        }
    }

    public static class DeckBuilder
    {
        public static DeckValidationResult Validate(
            CardCatalog catalog,
            IEnumerable<string> ids,
            int minimum = 1,
            int maximum = 108)
        {
            if (catalog == null) throw new ArgumentNullException(nameof(catalog));
            if (ids == null) throw new ArgumentNullException(nameof(ids));
            if (minimum < 0) throw new ArgumentOutOfRangeException(nameof(minimum));
            if (maximum < minimum) throw new ArgumentOutOfRangeException(nameof(maximum));

            var items = ids.ToArray();
            var errors = new List<string>();

            if (items.Length < minimum || items.Length > maximum)
                errors.Add("Deck size outside permitted range.");

            foreach (var group in items.GroupBy(id => id, StringComparer.Ordinal))
            {
                if (string.IsNullOrWhiteSpace(group.Key))
                {
                    errors.Add("Card definition ID cannot be empty.");
                    continue;
                }

                if (!catalog.TryGet(group.Key, out var definition))
                    errors.Add("Unknown card definition: " + group.Key);
                else if (!definition!.IsAvailable)
                    errors.Add("Card is not available: " + group.Key);
                else if (group.Count() > definition.MaxCopies)
                    errors.Add("Copy limit exceeded: " + group.Key);
            }

            return new DeckValidationResult(errors);
        }

        public static List<CardInstance> Create(CardCatalog catalog, IEnumerable<string> ids, ulong seed)
        {
            if (catalog == null) throw new ArgumentNullException(nameof(catalog));
            if (ids == null) throw new ArgumentNullException(nameof(ids));

            var items = ids.ToArray();
            var validation = Validate(catalog, items);
            if (!validation.IsValid)
                throw new ArgumentException(string.Join(" ", validation.Errors), nameof(ids));

            var deck = items
                .Select((id, index) => new CardInstance(
                    "card-" + seed.ToString("x") + "-" + index,
                    catalog.Get(id)))
                .ToList();

            Shuffle(deck, seed);
            return deck;
        }

        public static void Shuffle<T>(IList<T> cards, ulong seed)
        {
            if (cards == null) throw new ArgumentNullException(nameof(cards));

            var random = new DeterministicRandom(seed);
            for (int index = cards.Count - 1; index > 0; index--)
            {
                int swapIndex = random.Next(index + 1);
                T temporary = cards[index];
                cards[index] = cards[swapIndex];
                cards[swapIndex] = temporary;
            }
        }
    }
}
