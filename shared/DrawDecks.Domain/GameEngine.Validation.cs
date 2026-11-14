#nullable enable
using System;
using System.Linq;

namespace DrawDecks.Domain
{
    /// <summary>
    /// Pure command validation shared by human input, CPU candidate generation,
    /// automated simulations, and authoritative command execution.
    /// </summary>
    public static partial class GameEngine
    {
        public static ActionError ValidateDraw(MatchState state, string playerId, int count = 1)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));
            if (count < 1) throw new ArgumentOutOfRangeException(nameof(count));
            var error = ValidateTurn(state, playerId);
            if (error != ActionError.None) return error;
            if (state.BlockedDrawPlayerId == playerId) return ActionError.None;
            if (state.CurrentPlayer.Hand.Count + count > state.Rules.MaximumHandSize)
                return ActionError.HandLimitReached;
            int availableCards = state.DrawPileCount +
                (state.Rules.Mode == GameMode.Survival ? state.DiscardPile.Count : 0);
            if (availableCards < count) return ActionError.DeckEmpty;
            return ActionError.None;
        }

        public static ActionError ValidatePlayCard(
            MatchState state,
            string playerId,
            string cardInstanceId,
            string? targetPlayerId = null,
            Suit chosenSuit = Suit.None)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));
            if (string.IsNullOrWhiteSpace(cardInstanceId)) return ActionError.CardNotInHand;

            var error = ValidateTurn(state, playerId);
            if (error != ActionError.None) return error;

            var player = state.CurrentPlayer;
            var card = player.Hand.FirstOrDefault(c => c.InstanceId == cardInstanceId);
            if (card == null) return ActionError.CardNotInHand;
            if (card.Definition.Kind == CardKind.Defense) return ActionError.InvalidPhase;
            if (card.Definition.Effect == CardEffect.Damage &&
                state.Rules.Mode != GameMode.Elimination && state.Rules.Mode != GameMode.TeamBattle &&
                state.Rules.Mode != GameMode.Survival)
                return ActionError.InvalidCardType;
            if (state.ActiveSuit != Suit.None &&
                card.Definition.Kind == CardKind.Standard &&
                card.Definition.Suit != state.ActiveSuit)
                return ActionError.InvalidCardType;

            if (card.Definition.Effect == CardEffect.Wild &&
                (chosenSuit == Suit.None || !Enum.IsDefined(typeof(Suit), chosenSuit)))
                return ActionError.InvalidCardType;

            var target = targetPlayerId == null
                ? null
                : state.Players.FirstOrDefault(p => p.Id == targetPlayerId && !p.IsEliminated);
            if (targetPlayerId != null && (target == null || target.Id == playerId))
                return ActionError.InvalidTarget;
            if (target != null && state.Rules.Mode == GameMode.TeamBattle &&
                player.TeamId != null && StringComparer.Ordinal.Equals(player.TeamId, target.TeamId))
                return ActionError.InvalidTarget;

            if (RequiresTarget(card.Definition.Effect) && target == null)
                return ActionError.InvalidTarget;

            return ActionError.None;
        }

        public static ActionError ValidateRespondToAttack(
            MatchState state,
            string playerId,
            string? defenseCardInstanceId)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));
            if (state.Status != MatchStatus.Active) return ActionError.MatchNotActive;

            var attack = state.PendingAttack;
            if (attack == null || attack.TargetId != playerId || state.CurrentPlayer.Id != playerId)
                return ActionError.InvalidPhase;

            if (defenseCardInstanceId == null) return ActionError.None;

            var defense = state.CurrentPlayer.Hand.FirstOrDefault(c => c.InstanceId == defenseCardInstanceId);
            if (defense == null) return ActionError.CardNotInHand;
            if (defense.Definition.Kind != CardKind.Defense) return ActionError.InvalidDefense;
            if (defense.Definition.Effect != CardEffect.Shield &&
                defense.Definition.Effect != CardEffect.Negate &&
                defense.Definition.Effect != CardEffect.Reflect)
                return ActionError.InvalidDefense;

            return ActionError.None;
        }

        public static ActionError ValidateEndTurn(MatchState state, string playerId)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));
            var error = ValidateTurn(state, playerId);
            if (error != ActionError.None) return error;
            return state.PendingAttack == null ? ActionError.None : ActionError.InvalidPhase;
        }

        private static bool RequiresTarget(CardEffect effect)
        {
            return effect == CardEffect.StealCard ||
                   effect == CardEffect.DiscardRandomCard ||
                   effect == CardEffect.BlockDraw ||
                   effect == CardEffect.Damage ||
                   effect == CardEffect.SkipTurn ||
                   effect == CardEffect.SwapHands;
        }
    }
}
