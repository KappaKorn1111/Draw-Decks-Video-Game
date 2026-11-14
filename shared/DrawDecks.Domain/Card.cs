using System;

namespace DrawDecks.Domain;

public enum Suit { Clubs = 0, Diamonds = 1, Hearts = 2, Spades = 3 }

/// <summary>A standard playing card. Rank is 2–14, with Ace high.</summary>
public readonly struct Card : IEquatable<Card>
{
    public Suit Suit { get; }
    public int Rank { get; }

    public Card(Suit suit, int rank)
    {
        if (!Enum.IsDefined(typeof(Suit), suit)) throw new ArgumentOutOfRangeException(nameof(suit));
        if (rank < 2 || rank > 14) throw new ArgumentOutOfRangeException(nameof(rank));
        Suit = suit;
        Rank = rank;
    }

    public bool Equals(Card other) => Suit == other.Suit && Rank == other.Rank;
    public override bool Equals(object? obj) => obj is Card other && Equals(other);
    public override int GetHashCode() => ((int)Suit * 397) ^ Rank;
    public static bool operator ==(Card left, Card right) => left.Equals(right);
    public static bool operator !=(Card left, Card right) => !left.Equals(right);
    public override string ToString() => $"{Rank} of {Suit}";
}
