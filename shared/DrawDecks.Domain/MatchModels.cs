using System;
using System.Collections.Generic;
using System.Linq;

namespace DrawDecks.Domain;

public enum SeatKind { Human, Cpu }
public enum MatchStatus { Active, Completed }
public enum ActionError { None, MatchNotActive, NotPlayersTurn, CardNotInHand, InvalidPlayer }

public sealed class Ruleset
{
    public int StartingHandSize { get; }
    public int Rounds { get; }

    public Ruleset(int startingHandSize = 5, int rounds = 5)
    {
        if (startingHandSize < 1 || startingHandSize > 10) throw new ArgumentOutOfRangeException(nameof(startingHandSize));
        if (rounds < 1 || rounds > 10) throw new ArgumentOutOfRangeException(nameof(rounds));
        StartingHandSize = startingHandSize;
        Rounds = rounds;
    }

    public static Ruleset Default => new Ruleset();
}

public sealed class PlayerState
{
    private readonly List<Card> _hand;
    public string Id { get; }
    public string DisplayName { get; }
    public SeatKind Kind { get; }
    public IReadOnlyList<Card> Hand => _hand.AsReadOnly();
    public int Score { get; internal set; }

    internal PlayerState(string id, string displayName, SeatKind kind, IEnumerable<Card> hand)
    { Id = id; DisplayName = displayName; Kind = kind; _hand = new List<Card>(hand); }

    internal bool RemoveCard(Card card) => _hand.Remove(card);
}

public sealed class PlayedCard
{
    public string PlayerId { get; }
    public Card Card { get; }
    public int Round { get; }
    internal PlayedCard(string playerId, Card card, int round)
    { PlayerId = playerId; Card = card; Round = round; }
}

public sealed class MatchState
{
    private readonly List<PlayerState> _players;
    private readonly List<PlayedCard> _plays = new List<PlayedCard>();
    public string MatchId { get; }
    public ulong Seed { get; }
    public Ruleset Rules { get; }
    public IReadOnlyList<PlayerState> Players => _players.AsReadOnly();
    public IReadOnlyList<PlayedCard> Plays => _plays.AsReadOnly();
    public MatchStatus Status { get; internal set; }
    public int CurrentRound { get; internal set; }
    public int CurrentPlayerIndex { get; internal set; }
    public string? WinnerPlayerId { get; internal set; }
    public bool IsDraw { get; internal set; }

    internal MatchState(string matchId, ulong seed, Ruleset rules, IEnumerable<PlayerState> players)
    { MatchId = matchId; Seed = seed; Rules = rules; _players = players.ToList(); Status = MatchStatus.Active; CurrentRound = 1; }

    internal void Record(PlayedCard play) => _plays.Add(play);
}
