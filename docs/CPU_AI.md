# Draw Decks — CPU Opponents & Game AI

## Overview

The CPU decision engine lives in the shared DrawDecks.Domain assembly. Unity does not contain a separate rules implementation. AI candidate generation calls the same GameEngine.Validate* methods used by the command handlers, and the chosen action is executed through GameEngine.

## Difficulty profiles

| Difficulty | Policy |
|---|---|
| Easy | Seeded random selection from legal candidates, including draw/pass choices. |
| Normal | Tactical scoring, card-value prioritization, score pressure, basic defense, and opponent score/hand-count awareness. |
| Hard | Stronger threat weighting, resource preservation, wild-suit selection, draw-pile composition estimates, and target prioritization. |
| Expert | Hard policy plus public action-history modeling, risk assessment, and bounded look-ahead over the acting player's remaining hand. |

Easy is intentionally stochastic. The other levels are deterministic for a fixed state, difficulty, and configuration.

## Information boundaries

CpuAiObservation is a restricted snapshot:
- Full card identities are included only for the acting seat's own hand.
- Opponents expose seat ID/name/index, score, hand count, elimination state, and a public-history aggression metric.
- Discarded cards are public.
- Draw-pile composition is an estimate derived from the original deck list, the acting player's hand, the public discard pile, and the count of cards still in the draw pile.
- The AI does not receive opponent hand contents or draw-pile order.

The estimated deck distribution is probabilistic. Hidden opponent-held cards remain in the unseen pool and are not inferred individually.

## Decision and execution flow

1. Build the current actor's CpuAiObservation.
2. Generate only candidates accepted by GameEngine.ValidateDraw, ValidatePlayCard, ValidateRespondToAttack, or ValidateEndTurn.
3. Apply the selected difficulty policy and bounded evaluation budget.
4. If Draw is selected, execute one draw and reevaluate with HasDrawnThisTurn=true.
5. Execute the final command through the existing GameEngine handler.
6. If an unexpected validation disagreement occurs, the Unity controller logs the event and uses a safe pass/accept-attack fallback rather than repeatedly attempting the invalid action.

## Configuration and performance controls

Each CPU seat has an independent difficulty selector:
- Easy
- Normal
- Hard
- Expert

Local match setup also exposes:
- Thinking delay: Fast (0.15s), Standard (0.65s), Slow (1.15s).
- Evaluation budget: Fast (48 candidates, depth 1), Balanced (96, depth 2), Deep (192, depth 4).

The domain options clamp evaluation budget to 8–256 candidates and planning depth to 1–4. The ruleset also has a maximum-turn safety limit (default 500) so non-scoring/deck-recycle loops cannot leave a match active forever; when reached, the engine applies deterministic score/hand sudden-score resolution.

## Determinism

A CPU decision's random seed is derived from the configured seed (or match seed), seat ID, turn number, and whether the draw action has already been used. The same match state and options therefore produce the same Easy choice. Strategic difficulties do not use random tie-breaking.

## Validation coverage

The test suite covers:
- Repeatability of Easy decisions for a fixed seed/state.
- Different Easy choices across seed values and score-first Normal selection.
- Hard-versus-Normal target selection under conflicting hand-size and score threats.
- Expert targeting based on observed public opponent actions.
- The absence of opponent card identities from the opponent observation API.
- 120 seeded match simulations across all four difficulties and two, three, and four total seats (up to three CPU seats).
- Bounded candidate-evaluation and planning-depth settings.
- Attack response, skip queue, deck-exhaustion, max-turn, replay, and no-op defensive response regressions.

## Boundaries

Expert planning is bounded heuristic look-ahead over the acting player's known hand, not full expectimax or hidden-state search. Deck composition awareness is an estimate, not access to the shuffled deck order. CPU difficulty does not change game rules or grant additional information. The max-turn sudden-score fallback is a termination safeguard, not a substitute for the full Confluence Sudden Score final-turn sequence specified by the product rulebook.
