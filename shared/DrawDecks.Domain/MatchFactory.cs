using System;
using System.Collections.Generic;
using System.Linq;

namespace DrawDecks.Domain;

public sealed class SeatDefinition
{
    public string Id { get; }
    public string DisplayName { get; }
    public SeatKind Kind { get; }

    public SeatDefinition(string id, string displayName, SeatKind kind)
    {
        if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("Seat id is required.", nameof(id));
        if (string.IsNullOrWhiteSpace(displayName)) throw new ArgumentException("Display name is required.", nameof(displayName));
        Id = id; DisplayName = displayName; Kind = kind;
    }
}

public static class MatchFactory
{
    public static MatchState Create(string matchId, ulong seed, IReadOnlyList<SeatDefinition> seats, Ruleset? rules = null)
    {
        if (string.IsNullOrWhiteSpace(matchId)) throw new ArgumentException("Match id is required.", nameof(matchId));
        if (seats == null) throw new ArgumentNullException(nameof(seats));
        if (seats.Count < 1 || seats.Count > 4) throw new ArgumentOutOfRangeException(nameof(seats), "A match supports 1–4 seats.");
        if (!seats.Any(s => s.Kind == SeatKind.Human)) throw new ArgumentException("At least one human seat is required.", nameof(seats));
        if (seats.Select(s => s.Id).Distinct(StringComparer.Ordinal).Count() != seats.Count)
            throw new ArgumentException("Seat ids must be unique.", nameof(seats));

        rules ??= Ruleset.Default;
        if (seats.Count * rules.StartingHandSize > 52)
            throw new ArgumentException("The selected rules require more cards than the deck contains.", nameof(seats));

        var deck = new List<Card>(52);
        foreach (Suit suit in Enum.GetValues(typeof(Suit)))
            for (var rank = 2; rank <= 14; rank++) deck.Add(new Card(suit, rank));

        var random = new DeterministicRandom(seed);
        for (var i = deck.Count - 1; i > 0; i--)
        {
            var j = random.Next(i + 1);
            (deck[i], deck[j]) = (deck[j], deck[i]);
        }

        var players = new List<PlayerState>();
        var cursor = 0;
        foreach (var seat in seats)
        {
            var hand = deck.Skip(cursor).Take(rules.StartingHandSize).ToArray();
            cursor += rules.StartingHandSize;
            players.Add(new PlayerState(seat.Id, seat.DisplayName, seat.Kind, hand));
        }
        return new MatchState(matchId, seed, rules, players);
    }
}
