# Draw Decks — Card System Contract

## Catalog and identity
The versioned catalog defines card kind, effect, suit, rank, value, rarity, availability, and maximum copies. Physical instances have unique IDs independent of definition IDs. Every match pins its catalog version. The default catalog contains 52 standard cards and all 13 requested action, attack, defense, and special definitions.

## Deck construction
DeckBuilder validates deck size, known definitions, availability, and per-definition copy caps. It creates uniquely identified instances and applies seeded Fisher–Yates shuffle. The seed is for gameplay reproducibility, never security.

## Engine commands and zones
The shared domain engine owns draw pile, hands, discard pile, current player, direction, pending attack, score modifiers, match status, and sequenced events. Commands are Draw, PlayCard, Discard, EndTurn, RespondToAttack, and EliminatePlayer. Invalid commands return typed errors without changing gameplay state. Unity is a presentation/input layer and does not implement card effects.

## Effect semantics
- Draw 2: draws up to two, bounded by hand limit and deck size.
- Skip Turn: marks a selected opponent to be skipped by turn scheduling.
- Reverse Order: reverses turn direction.
- Swap Hands: exchanges the acting player's hand with a selected opponent.
- Steal Card: pauses for target response; an unblocked attack transfers one target card.
- Discard Random Card: pauses for response; seeded PRNG selects the target card.
- Block Draw: pending attack; successful resolution prevents the target's next draw.
- Shield and Negate: cancel a pending attack.
- Reflect: redirects the pending effect to its original attacker.
- Wild: selects the active suit; subsequent standard cards must match it.
- Deck Reset: shuffles the discard pile back into the draw pile.
- Double Score: doubles the next standard-card score earned by that player.

## Match lifecycle and stacking
The default ruleset disables effect stacking. An attack pauses the turn and transfers response control to its target. Shield/Negate cancel; Reflect redirects. The ruleset exposes a stacking flag, but enabling stacked resolution is not supported yet. The match has a configurable target score; a player reaching it wins. The authoritative engine can also eliminate a player (for forfeits or server adjudication); the last active player wins, and zero active players is a draw.

## Audit and replay
Each accepted command is recorded with actor, command type, card/target identifiers, and parameters. State transitions append monotonically sequenced events. Match seed, seat definitions, deck definition IDs, ruleset, and catalog version are retained. MatchReplay rebuilds the initial match and re-applies the accepted command stream; equivalence checks compare event sequence, state, card zones, scores, turn, random state, and pending attack. Replay requires the same catalog version.

## Remaining scope
The engine is Unity-independent and implements catalog, deck validation, seeded shuffle, draw/play/discard, hand limits, turn scheduling, all requested core effects, attack/defense response, score modifiers, elimination, target-score victory, and deterministic replay. Production persistence of the event log, cryptographic tamper evidence, stack composition when enabled, network-authoritative command hosting, and UI are later phases.
