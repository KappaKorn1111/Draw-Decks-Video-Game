using DrawDecks.Domain;

namespace DrawDecks.Domain.Tests;

public sealed class GameEngineTests
{
    private static SeatDefinition[] TwoSeats() =>
    [
        new SeatDefinition("p1", "Player One", SeatKind.Human),
        new SeatDefinition("p2", "Player Two", SeatKind.Cpu)
    ];

    [Fact]
    public void SameSeedProducesSameInitialHands()
    {
        var a = MatchFactory.Create("a", 12345, TwoSeats());
        var b = MatchFactory.Create("b", 12345, TwoSeats());
        Assert.Equal(a.Players[0].Hand, b.Players[0].Hand);
        Assert.Equal(a.Players[1].Hand, b.Players[1].Hand);
    }

    [Fact]
    public void RejectsMoreThanFourSeats()
    {
        var seats = Enumerable.Range(0, 5).Select(i =>
            new SeatDefinition($"p{i}", $"Player {i}", SeatKind.Human)).ToArray();
        Assert.Throws<ArgumentOutOfRangeException>(() => MatchFactory.Create("m", 1, seats));
    }

    [Fact]
    public void RejectsPlayingOutOfTurn()
    {
        var state = MatchFactory.Create("m", 7, TwoSeats());
        var result = GameEngine.PlayCard(state, "p2", state.Players[1].Hand[0]);
        Assert.False(result.Success);
        Assert.Equal(ActionError.NotPlayersTurn, result.Error);
        Assert.Equal(5, state.Players[1].Hand.Count);
    }

    [Fact]
    public void RejectsCardNotInPlayersHand()
    {
        var state = MatchFactory.Create("m", 9, TwoSeats());
        var foreignCard = new Card(Suit.Spades, 14);
        if (state.Players[0].Hand.Contains(foreignCard)) foreignCard = new Card(Suit.Spades, 13);
        var result = GameEngine.PlayCard(state, "p1", foreignCard);
        Assert.False(result.Success);
        Assert.Equal(ActionError.CardNotInHand, result.Error);
    }

    [Fact]
    public void CompletingAllRoundsCompletesMatchAndAwardsWinnerOrDraw()
    {
        var state = MatchFactory.Create("m", 11, TwoSeats());
        for (var round = 0; round < 5; round++)
            foreach (var player in state.Players.ToArray())
            {
                var current = state.Players.First(p => p.Id == player.Id);
                Assert.True(GameEngine.PlayCard(state, current.Id, current.Hand[0]).Success);
            }

        Assert.Equal(MatchStatus.Completed, state.Status);
        Assert.InRange(state.Players.Sum(p => p.Score), 5, 10);
        Assert.True(state.IsDraw || state.WinnerPlayerId != null);
    }
}
