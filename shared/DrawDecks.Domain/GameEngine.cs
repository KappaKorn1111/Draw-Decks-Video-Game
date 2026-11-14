using System;
using System.Linq;

namespace DrawDecks.Domain;

public sealed class ActionResult
{
    public bool Success { get; }
    public ActionError Error { get; }
    public MatchState State { get; }
    internal ActionResult(bool success, ActionError error, MatchState state)
    { Success = success; Error = error; State = state; }
}

public static class GameEngine
{
    public static ActionResult PlayCard(MatchState state, string playerId, Card card)
    {
        if (state == null) throw new ArgumentNullException(nameof(state));
        if (state.Status != MatchStatus.Active) return new ActionResult(false, ActionError.MatchNotActive, state);
        var player = state.Players.FirstOrDefault(p => p.Id == playerId);
        if (player == null) return new ActionResult(false, ActionError.InvalidPlayer, state);
        if (state.Players[state.CurrentPlayerIndex].Id != playerId)
            return new ActionResult(false, ActionError.NotPlayersTurn, state);
        if (!player.Hand.Contains(card)) return new ActionResult(false, ActionError.CardNotInHand, state);

        player.RemoveCard(card);
        state.Record(new PlayedCard(playerId, card, state.CurrentRound));
        var roundPlays = state.Plays.Where(p => p.Round == state.CurrentRound).ToArray();

        if (roundPlays.Length == state.Players.Count)
        {
            var highRank = roundPlays.Max(p => p.Card.Rank);
            foreach (var winner in roundPlays.Where(p => p.Card.Rank == highRank))
                state.Players.First(p => p.Id == winner.PlayerId).Score++;

            if (state.CurrentRound >= state.Rules.Rounds)
            {
                state.Status = MatchStatus.Completed;
                var highScore = state.Players.Max(p => p.Score);
                var leaders = state.Players.Where(p => p.Score == highScore).ToArray();
                state.IsDraw = leaders.Length != 1;
                state.WinnerPlayerId = state.IsDraw ? null : leaders[0].Id;
            }
            else
            {
                state.CurrentRound++;
                state.CurrentPlayerIndex = 0;
            }
        }
        else state.CurrentPlayerIndex = (state.CurrentPlayerIndex + 1) % state.Players.Count;

        return new ActionResult(true, ActionError.None, state);
    }
}
