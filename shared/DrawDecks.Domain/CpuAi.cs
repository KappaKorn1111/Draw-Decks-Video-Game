#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;

namespace DrawDecks.Domain
{
    public enum CpuDifficulty
    {
        Easy = 0,
        Normal = 1,
        Hard = 2,
        Expert = 3
    }

    public enum CpuActionKind
    {
        Draw,
        PlayCard,
        AcceptAttack,
        PlayDefense,
        EndTurn
    }

    /// <summary>Bounded controls for reproducible CPU decisions and per-turn compute cost.</summary>
    public sealed class CpuAiOptions
    {
        public CpuDifficulty Difficulty { get; }
        public ulong Seed { get; }
        public bool HasDrawnThisTurn { get; }
        public int MaximumEvaluations { get; }
        public int PlanningDepth { get; }
        public bool UseDeckCompositionEstimates { get; }
        public bool UseOpponentModeling { get; }

        public CpuAiOptions(
            CpuDifficulty difficulty = CpuDifficulty.Normal,
            ulong seed = 0,
            bool hasDrawnThisTurn = false,
            int maximumEvaluations = 96,
            int planningDepth = 2,
            bool useDeckCompositionEstimates = true,
            bool useOpponentModeling = true)
        {
            if (!Enum.IsDefined(typeof(CpuDifficulty), difficulty))
                throw new ArgumentOutOfRangeException(nameof(difficulty));
            Difficulty = difficulty;
            Seed = seed;
            HasDrawnThisTurn = hasDrawnThisTurn;
            MaximumEvaluations = Math.Max(8, Math.Min(256, maximumEvaluations));
            PlanningDepth = Math.Max(1, Math.Min(4, planningDepth));
            UseDeckCompositionEstimates = useDeckCompositionEstimates;
            UseOpponentModeling = useOpponentModeling;
        }
    }

    public sealed class CpuAiAction
    {
        public CpuActionKind Kind { get; }
        public string? CardInstanceId { get; }
        public string? TargetPlayerId { get; }
        public Suit ChosenSuit { get; }

        internal CpuAiAction(
            CpuActionKind kind,
            string? cardInstanceId = null,
            string? targetPlayerId = null,
            Suit chosenSuit = Suit.None)
        {
            Kind = kind;
            CardInstanceId = cardInstanceId;
            TargetPlayerId = targetPlayerId;
            ChosenSuit = chosenSuit;
        }

        internal string StableKey =>
            ((int)Kind).ToString() + "|" + (CardInstanceId ?? "-") + "|" +
            (TargetPlayerId ?? "-") + "|" + ((int)ChosenSuit).ToString();
    }

    /// <summary>
    /// Public opponent information only. It deliberately has no CardInstance or hand
    /// collection property so a policy cannot inspect another seat's hidden cards.
    /// </summary>
    public sealed class CpuOpponentObservation
    {
        public string Id { get; }
        public string DisplayName { get; }
        public int SeatIndex { get; }
        public int Score { get; }
        public int HandCount { get; }
        public bool IsEliminated { get; }
        public float RecentAggression { get; }

        internal CpuOpponentObservation(
            string id,
            string displayName,
            int seatIndex,
            int score,
            int handCount,
            bool isEliminated,
            float recentAggression)
        {
            Id = id;
            DisplayName = displayName;
            SeatIndex = seatIndex;
            Score = score;
            HandCount = handCount;
            IsEliminated = isEliminated;
            RecentAggression = recentAggression;
        }
    }

    /// <summary>
    /// A bounded information view for the acting seat. Own hand, public discard pile,
    /// scores, opponent hand counts, turn state, and a probabilistic unseen-card
    /// estimate are exposed. Opponent hand identities and draw-pile order are not.
    /// </summary>
    public sealed class CpuAiObservation
    {
        private readonly Dictionary<string, double> estimatedDrawCopies;

        public string PlayerId { get; }
        public string DisplayName { get; }
        public int OwnSeatIndex { get; }
        public int SeatCount { get; }
        public int OwnScore { get; }
        public IReadOnlyList<CardInstance> OwnHand { get; }
        public IReadOnlyList<CpuOpponentObservation> Opponents { get; }
        public IReadOnlyList<CardInstance> PublicDiscardPile { get; }
        public int DrawPileCount { get; }
        public int MaximumHandSize { get; }
        public int TargetScore { get; }
        public int TurnNumber { get; }
        public int TurnDirection { get; }
        public Suit ActiveSuit { get; }
        public bool HasPendingAttack { get; }
        public string? AttackerId { get; }
        public CardEffect PendingAttackEffect { get; }
        public string CatalogVersion { get; }
        public double EstimatedUnknownCardCount { get; }

        internal CpuAiObservation(
            string playerId,
            string displayName,
            int ownSeatIndex,
            int seatCount,
            int ownScore,
            IReadOnlyList<CardInstance> ownHand,
            IReadOnlyList<CpuOpponentObservation> opponents,
            IReadOnlyList<CardInstance> discardPile,
            int drawPileCount,
            int maximumHandSize,
            int targetScore,
            int turnNumber,
            int turnDirection,
            Suit activeSuit,
            PendingAttack? pendingAttack,
            string catalogVersion,
            Dictionary<string, double> drawEstimates,
            double unknownCount)
        {
            PlayerId = playerId;
            DisplayName = displayName;
            OwnSeatIndex = ownSeatIndex;
            SeatCount = seatCount;
            OwnScore = ownScore;
            OwnHand = ownHand;
            Opponents = opponents;
            PublicDiscardPile = discardPile;
            DrawPileCount = drawPileCount;
            MaximumHandSize = maximumHandSize;
            TargetScore = targetScore;
            TurnNumber = turnNumber;
            TurnDirection = turnDirection;
            ActiveSuit = activeSuit;
            HasPendingAttack = pendingAttack != null;
            AttackerId = pendingAttack?.AttackerId;
            PendingAttackEffect = pendingAttack?.Effect ?? CardEffect.None;
            CatalogVersion = catalogVersion;
            estimatedDrawCopies = drawEstimates;
            EstimatedUnknownCardCount = unknownCount;
        }

        /// <summary>
        /// Expected count in the draw pile, not an exact count. Opponent hands are
        /// hidden and included in the unseen pool, so no private card identities
        /// are inferred or revealed.
        /// </summary>
        public double EstimateDrawCopies(string definitionId)
        {
            return estimatedDrawCopies.TryGetValue(definitionId, out var value) ? value : 0d;
        }

        public float OwnScoreProgress => Math.Min(1f, (float)OwnScore / Math.Max(1, TargetScore));
        public int FreeHandSlots => Math.Max(0, MaximumHandSize - OwnHand.Count);
    }

    public sealed class CpuAiDecision
    {
        public CpuAiAction Action { get; }
        public float Utility { get; }
        public string Reason { get; }
        public int CandidatesConsidered { get; }
        public CpuDifficulty Difficulty { get; }

        internal CpuAiDecision(
            CpuAiAction action,
            float utility,
            string reason,
            int candidatesConsidered,
            CpuDifficulty difficulty)
        {
            Action = action;
            Utility = utility;
            Reason = reason;
            CandidatesConsidered = candidatesConsidered;
            Difficulty = difficulty;
        }
    }

    /// <summary>
    /// CPU decision engine. Candidate generation uses GameEngine's public command
    /// validators; chosen commands are subsequently executed by GameEngine itself.
    /// </summary>
    public static class CpuAi
    {
        public static CpuAiObservation Observe(MatchState state, string playerId, CardCatalog catalog)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));
            if (catalog == null) throw new ArgumentNullException(nameof(catalog));
            if (state.Status != MatchStatus.Active) throw new InvalidOperationException("CPU observation requires an active match.");
            if (state.CurrentPlayer.Id != playerId) throw new InvalidOperationException("Only the current player may make a CPU decision.");
            if (!StringComparer.Ordinal.Equals(state.CatalogVersion, catalog.Version))
                throw new InvalidOperationException("CPU catalog version does not match the active match.");

            var own = state.CurrentPlayer;
            var publicDiscard = state.DiscardPile.ToArray();
            var ownHand = own.Hand.ToArray();
            var recentEvents = state.Events
                .Skip(Math.Max(0, state.Events.Count - 40))
                .Where(e => e.Type == "CardPlayed")
                .ToArray();

            var opponents = new List<CpuOpponentObservation>();
            for (int index = 0; index < state.Players.Count; index++)
            {
                var player = state.Players[index];
                if (player.Id == playerId) continue;

                var events = recentEvents.Where(e => e.PlayerId == player.Id).ToArray();
                int aggressive = events.Count(e =>
                    e.Data == CardEffect.StealCard.ToString() ||
                    e.Data == CardEffect.DiscardRandomCard.ToString() ||
                    e.Data == CardEffect.BlockDraw.ToString() ||
                    e.Data == CardEffect.SkipTurn.ToString());
                float aggression = events.Length == 0 ? 0f : (float)aggressive / events.Length;

                opponents.Add(new CpuOpponentObservation(
                    player.Id,
                    player.DisplayName,
                    index,
                    player.Score,
                    player.Hand.Count,
                    player.IsEliminated,
                    aggression));
            }

            var originalCounts = state.DeckDefinitionIds
                .GroupBy(id => id, StringComparer.Ordinal)
                .ToDictionary(group => group.Key, group => (double)group.Count(), StringComparer.Ordinal);

            foreach (var card in ownHand.Concat(publicDiscard))
            {
                if (originalCounts.TryGetValue(card.DefinitionId, out var count))
                    originalCounts[card.DefinitionId] = Math.Max(0d, count - 1d);
            }

            double unknownCount = originalCounts.Values.Sum();
            var drawEstimates = new Dictionary<string, double>(StringComparer.Ordinal);
            foreach (var item in originalCounts)
            {
                double expected = unknownCount <= 0d
                    ? 0d
                    : item.Value * state.DrawPileCount / unknownCount;
                drawEstimates[item.Key] = expected;
            }

            return new CpuAiObservation(
                own.Id,
                own.DisplayName,
                state.CurrentPlayerIndex,
                state.Players.Count,
                own.Score,
                ownHand,
                opponents.AsReadOnly(),
                publicDiscard,
                state.DrawPileCount,
                state.Rules.MaximumHandSize,
                state.Rules.TargetScore,
                state.TurnNumber,
                state.TurnDirection,
                state.ActiveSuit,
                state.PendingAttack,
                catalog.Version,
                drawEstimates,
                unknownCount);
        }

        public static CpuAiDecision Decide(
            MatchState state,
            string playerId,
            CardCatalog catalog,
            CpuAiOptions? options = null)
        {
            options = options ?? new CpuAiOptions();
            var observation = Observe(state, playerId, catalog);
            var candidates = GenerateLegalCandidates(state, playerId, observation, catalog, options);

            if (candidates.Count == 0)
                throw new InvalidOperationException("No legal CPU action was found. The current match state cannot advance.");

            if (options.Difficulty == CpuDifficulty.Easy)
            {
                var random = new DeterministicRandom(DecisionSeed(state, playerId, options));
                var selected = candidates[random.Next(candidates.Count)];
                return new CpuAiDecision(
                    selected,
                    0f,
                    "Easy policy selected a legal action at random.",
                    candidates.Count,
                    options.Difficulty);
            }

            int evaluationCount = Math.Min(candidates.Count, options.MaximumEvaluations);
            CpuAiAction best = candidates[0];
            float bestUtility = float.NegativeInfinity;

            for (int index = 0; index < evaluationCount; index++)
            {
                var candidate = candidates[index];
                float score = Evaluate(observation, candidate, catalog, options);
                if (score > bestUtility)
                {
                    bestUtility = score;
                    best = candidate;
                }
            }

            return new CpuAiDecision(
                best,
                bestUtility,
                Explain(observation, best, bestUtility),
                evaluationCount,
                options.Difficulty);
        }

        private static List<CpuAiAction> GenerateLegalCandidates(
            MatchState state,
            string playerId,
            CpuAiObservation observation,
            CardCatalog catalog,
            CpuAiOptions options)
        {
            var candidates = new List<CpuAiAction>();

            if (observation.HasPendingAttack)
            {
                if (GameEngine.ValidateRespondToAttack(state, playerId, null) == ActionError.None)
                    candidates.Add(new CpuAiAction(CpuActionKind.AcceptAttack));

                foreach (var card in observation.OwnHand)
                {
                    if (card.Definition.Kind != CardKind.Defense) continue;
                    if (GameEngine.ValidateRespondToAttack(state, playerId, card.InstanceId) == ActionError.None)
                        candidates.Add(new CpuAiAction(CpuActionKind.PlayDefense, card.InstanceId));
                }

                return candidates;
            }

            var livingOpponents = observation.Opponents.Where(p => !p.IsEliminated).ToArray();
            foreach (var card in observation.OwnHand)
            {
                if (card.Definition.Kind == CardKind.Defense) continue;

                if (card.Definition.Effect == CardEffect.Wild)
                {
                    foreach (Suit suit in new[] { Suit.Clubs, Suit.Diamonds, Suit.Hearts, Suit.Spades })
                        AddPlayCandidate(candidates, state, playerId, card, null, suit);
                    continue;
                }

                if (RequiresTarget(card.Definition.Effect))
                {
                    foreach (var opponent in livingOpponents)
                        AddPlayCandidate(candidates, state, playerId, card, opponent.Id, Suit.None);
                }
                else
                {
                    AddPlayCandidate(candidates, state, playerId, card, null, Suit.None);
                }
            }

            if (!options.HasDrawnThisTurn &&
                GameEngine.ValidateDraw(state, playerId, 1) == ActionError.None)
                candidates.Add(new CpuAiAction(CpuActionKind.Draw));

            if (GameEngine.ValidateEndTurn(state, playerId) == ActionError.None)
                candidates.Add(new CpuAiAction(CpuActionKind.EndTurn));

            return candidates;
        }

        private static void AddPlayCandidate(
            List<CpuAiAction> candidates,
            MatchState state,
            string playerId,
            CardInstance card,
            string? targetPlayerId,
            Suit chosenSuit)
        {
            if (GameEngine.ValidatePlayCard(state, playerId, card.InstanceId, targetPlayerId, chosenSuit) == ActionError.None)
                candidates.Add(new CpuAiAction(CpuActionKind.PlayCard, card.InstanceId, targetPlayerId, chosenSuit));
        }

        private static float Evaluate(
            CpuAiObservation observation,
            CpuAiAction action,
            CardCatalog catalog,
            CpuAiOptions options)
        {
            if (action.Kind == CpuActionKind.EndTurn) return -1f;
            if (action.Kind == CpuActionKind.AcceptAttack) return -2f;

            if (action.Kind == CpuActionKind.PlayDefense)
            {
                var defense = observation.OwnHand.First(c => c.InstanceId == action.CardInstanceId);
                var attacker = observation.Opponents.FirstOrDefault(p => p.Id == observation.AttackerId);
                float threat = attacker == null ? 0f : Threat(attacker, observation.TargetScore, options);
                if (defense.Definition.Effect == CardEffect.Reflect)
                    return 8.5f + threat * 0.45f;
                if (defense.Definition.Effect == CardEffect.Negate)
                    return 7.5f + threat * 0.25f;
                return 7f + threat * 0.2f;
            }

            if (action.Kind == CpuActionKind.Draw)
            {
                bool hasPlayable = observation.OwnHand.Any(card =>
                    IsPlayableWithoutTarget(card, observation.ActiveSuit));
                float value = hasPlayable ? 0.5f : 8.5f;
                value += Math.Min(2.5f, observation.FreeHandSlots * 0.3f);
                if (options.Difficulty >= CpuDifficulty.Hard && options.UseDeckCompositionEstimates)
                    value += AverageExpectedDrawValue(observation, catalog) * 0.08f;
                return value;
            }

            var played = observation.OwnHand.First(c => c.InstanceId == action.CardInstanceId);
            var target = action.TargetPlayerId == null
                ? null
                : observation.Opponents.FirstOrDefault(p => p.Id == action.TargetPlayerId);
            float score;

            switch (played.Definition.Effect)
            {
                case CardEffect.None:
                    score = played.Definition.Value * 1.4f;
                    if (observation.OwnScore + played.Definition.Value >= observation.TargetScore)
                        score += 100f;
                    break;
                case CardEffect.DrawTwo:
                    score = 7f + Math.Min(3.5f, observation.FreeHandSlots * 0.7f);
                    break;
                case CardEffect.SkipTurn:
                    score = 5.5f + (target == null ? 0f : Threat(target, observation.TargetScore, options));
                    if (target != null && IsNextSeat(observation, target.SeatIndex))
                        score += 2.5f;
                    break;
                case CardEffect.ReverseOrder:
                    score = 3.5f + Threat(NextSeatAfterReverse(observation), observation.TargetScore, options) * 0.45f;
                    break;
                case CardEffect.SwapHands:
                    score = 4f + (target == null ? 0f : (observation.OwnHand.Count - target.HandCount) * 0.75f);
                    if (target != null)
                        score += Math.Max(0, target.Score - observation.OwnScore) * 0.05f;
                    break;
                case CardEffect.StealCard:
                    score = 5.5f + (target == null ? 0f : target.HandCount * 0.35f + Threat(target, observation.TargetScore, options) * 0.7f);
                    break;
                case CardEffect.DiscardRandomCard:
                    score = 5f + (target == null ? 0f : target.HandCount * 0.45f + Threat(target, observation.TargetScore, options) * 0.65f);
                    break;
                case CardEffect.BlockDraw:
                    score = 4.5f + (target == null ? 0f : Threat(target, observation.TargetScore, options) * 0.5f);
                    if (target != null && IsNextSeat(observation, target.SeatIndex))
                        score += target.HandCount < observation.MaximumHandSize ? 2f : 0.25f;
                    break;
                case CardEffect.Wild:
                    score = 3.5f + SuitValue(observation, catalog, action.ChosenSuit, options) * 0.35f;
                    break;
                case CardEffect.DeckReset:
                    score = observation.DrawPileCount < 5 ? 10f :
                        observation.DrawPileCount < 12 ? 6f : -2.5f;
                    break;
                case CardEffect.DoubleScore:
                    score = -3.5f;
                    var nextOpponent = NextSeatInDirection(observation, observation.TurnDirection);
                    if (nextOpponent != null)
                        score -= nextOpponent.Score >= observation.TargetScore * 0.75f ? 5f : 1.5f;
                    break;
                default:
                    score = 0f;
                    break;
            }

            if (options.Difficulty >= CpuDifficulty.Hard)
            {
                if (target != null)
                    score += Threat(target, observation.TargetScore, options) * 0.55f;

                if (options.UseDeckCompositionEstimates && played.Definition.Effect == CardEffect.Wild)
                    score += SuitValue(observation, catalog, action.ChosenSuit, options) * 0.35f;

                if (played.Definition.Kind == CardKind.Standard && observation.OwnScoreProgress > 0.65f)
                    score += played.Definition.Value * observation.OwnScoreProgress * 0.25f;
            }

            if (options.Difficulty == CpuDifficulty.Expert)
            {
                score += FutureHandPotential(observation, played, action.ChosenSuit, catalog) * 0.55f;
                if (options.PlanningDepth >= 2)
                    score += ContinuationPlan(observation, played, action.ChosenSuit, catalog) *
                             (0.18f * Math.Min(3, options.PlanningDepth));
                if (target != null)
                {
                    float winRisk = (float)target.Score / Math.Max(1, observation.TargetScore);
                    score += winRisk * 2f + target.RecentAggression * 1.5f;
                }
                score -= RiskOfHelpingNextOpponent(observation, played);
            }

            return score;
        }

        private static bool IsPlayableWithoutTarget(CardInstance card, Suit activeSuit)
        {
            if (card.Definition.Kind == CardKind.Defense) return false;
            if (card.Definition.Kind == CardKind.Standard &&
                activeSuit != Suit.None && card.Definition.Suit != activeSuit) return false;
            return card.Definition.Effect != CardEffect.SkipTurn &&
                   card.Definition.Effect != CardEffect.SwapHands &&
                   card.Definition.Effect != CardEffect.StealCard &&
                   card.Definition.Effect != CardEffect.DiscardRandomCard &&
                   card.Definition.Effect != CardEffect.BlockDraw;
        }

        private static float Threat(CpuOpponentObservation opponent, int targetScore, CpuAiOptions options)
        {
            float scoreThreat = (float)opponent.Score / Math.Max(1, targetScore) * 10f;
            float handThreat = Math.Min(3f, opponent.HandCount * 0.35f);
            float aggression = options.Difficulty == CpuDifficulty.Expert && options.UseOpponentModeling
                ? opponent.RecentAggression * 3f
                : 0f;
            return scoreThreat + handThreat + aggression;
        }

        private static CpuOpponentObservation? NextSeatInDirection(CpuAiObservation observation, int direction)
        {
            for (int step = 1; step <= observation.SeatCount; step++)
            {
                int index = (observation.OwnSeatIndex + direction * step + observation.SeatCount * 2) %
                            observation.SeatCount;
                var opponent = observation.Opponents.FirstOrDefault(p => p.SeatIndex == index && !p.IsEliminated);
                if (opponent != null) return opponent;
            }
            return null;
        }

        private static CpuOpponentObservation NextSeatAfterReverse(CpuAiObservation observation)
        {
            return NextSeatInDirection(observation, -observation.TurnDirection) ??
                   new CpuOpponentObservation("none", "None", observation.OwnSeatIndex, 0, 0, true, 0f);
        }

        private static bool IsNextSeat(CpuAiObservation observation, int seatIndex)
        {
            var next = NextSeatInDirection(observation, observation.TurnDirection);
            return next != null && next.SeatIndex == seatIndex;
        }

        private static float SuitValue(
            CpuAiObservation observation,
            CardCatalog catalog,
            Suit suit,
            CpuAiOptions options)
        {
            float ownCount = observation.OwnHand.Count(c =>
                c.Definition.Kind == CardKind.Standard && c.Definition.Suit == suit);
            double expected = 0d;
            if (options.UseDeckCompositionEstimates)
            {
                foreach (var definition in catalog.Definitions.Where(d =>
                    d.Kind == CardKind.Standard && d.Suit == suit))
                    expected += observation.EstimateDrawCopies(definition.Id);
            }
            return ownCount * 1.5f + (float)Math.Min(4d, expected);
        }

        private static float AverageExpectedDrawValue(CpuAiObservation observation, CardCatalog catalog)
        {
            double value = 0d;
            double count = 0d;
            foreach (var definition in catalog.Definitions.Where(d => d.Kind == CardKind.Standard))
            {
                double expected = observation.EstimateDrawCopies(definition.Id);
                value += expected * definition.Value;
                count += expected;
            }
            return count <= 0d ? 0f : (float)(value / count);
        }

        private static float FutureHandPotential(
            CpuAiObservation observation,
            CardInstance played,
            Suit chosenSuit,
            CardCatalog catalog)
        {
            Suit nextSuit = played.Definition.Effect == CardEffect.Wild ? chosenSuit : observation.ActiveSuit;
            int standardOptions = observation.OwnHand.Count(card =>
                card.InstanceId != played.InstanceId &&
                card.Definition.Kind == CardKind.Standard &&
                (nextSuit == Suit.None || card.Definition.Suit == nextSuit));
            int tacticalOptions = observation.OwnHand.Count(card =>
                card.InstanceId != played.InstanceId &&
                card.Definition.Kind != CardKind.Standard &&
                card.Definition.Kind != CardKind.Defense &&
                card.Definition.Effect != CardEffect.None);
            return standardOptions * 1.25f + tacticalOptions * 0.45f;
        }

        private static float ContinuationPlan(
            CpuAiObservation observation,
            CardInstance played,
            Suit chosenSuit,
            CardCatalog catalog)
        {
            // Shallow private-hand lookahead: estimate the best follow-up card value
            // after this card leaves the hand, without simulating opponents' hidden hands.
            Suit suit = played.Definition.Effect == CardEffect.Wild ? chosenSuit : observation.ActiveSuit;
            float best = 0f;
            foreach (var card in observation.OwnHand)
            {
                if (card.InstanceId == played.InstanceId || card.Definition.Kind == CardKind.Defense) continue;
                if (card.Definition.Kind == CardKind.Standard &&
                    suit != Suit.None && card.Definition.Suit != suit) continue;

                float value = card.Definition.Kind == CardKind.Standard
                    ? card.Definition.Value * 0.35f
                    : card.Definition.Effect == CardEffect.DoubleScore ? -1.5f
                    : card.Definition.Effect == CardEffect.DeckReset && observation.DrawPileCount < 8 ? 4f
                    : card.Definition.Effect == CardEffect.DrawTwo ? 3f
                    : 1.5f;
                if (value > best) best = value;
            }
            return best;
        }

        private static float RiskOfHelpingNextOpponent(CpuAiObservation observation, CardInstance played)
        {
            if (played.Definition.Effect != CardEffect.DoubleScore) return 0f;
            var next = NextSeatInDirection(observation, observation.TurnDirection);
            if (next == null) return 0f;
            return 3f + ((float)next.Score / Math.Max(1, observation.TargetScore)) * 5f;
        }

        private static ulong DecisionSeed(MatchState state, string playerId, CpuAiOptions options)
        {
            ulong seed = options.Seed == 0 ? state.Seed : options.Seed;
            seed ^= StableHash(playerId);
            seed ^= unchecked((ulong)state.TurnNumber * 0x9E3779B97F4A7C15UL);
            if (options.HasDrawnThisTurn) seed ^= 0xD1B54A32D192ED03UL;
            return seed == 0 ? 0x9E3779B97F4A7C15UL : seed;
        }

        private static ulong StableHash(string value)
        {
            const ulong offset = 14695981039346656037UL;
            const ulong prime = 1099511628211UL;
            ulong hash = offset;
            foreach (char character in value)
            {
                hash ^= character;
                hash *= prime;
            }
            return hash;
        }

        private static bool RequiresTarget(CardEffect effect)
        {
            return effect == CardEffect.StealCard ||
                   effect == CardEffect.DiscardRandomCard ||
                   effect == CardEffect.BlockDraw ||
                   effect == CardEffect.SkipTurn ||
                   effect == CardEffect.SwapHands;
        }

        private static string Explain(CpuAiObservation observation, CpuAiAction action, float utility)
        {
            if (action.Kind == CpuActionKind.Draw) return "No current play outranked drawing a card.";
            if (action.Kind == CpuActionKind.EndTurn) return "No higher-value legal action was available.";
            if (action.Kind == CpuActionKind.AcceptAttack) return "Accepting the attack preserves defensive resources.";
            if (action.Kind == CpuActionKind.PlayDefense) return "Defensive response selected using attack impact.";
            if (action.Kind == CpuActionKind.PlayCard)
            {
                var card = observation.OwnHand.First(c => c.InstanceId == action.CardInstanceId);
                return "Selected " + card.Definition.Name + " using " + utility.ToString("0.00") + " utility.";
            }
            return "Selected highest evaluated legal action.";
        }
    }
}
