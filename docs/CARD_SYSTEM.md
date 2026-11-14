# Draw Decks — Card System Contract

## Catalog and identity
The versioned catalog defines card kind, effect, suit, rank, value, rarity, and maximum copies. Physical card instances have unique IDs independent of definition IDs. Each match stores the catalog version. The default catalog contains 52 standard cards plus the 13 requested action, attack, defense, and special effects.

## Deck construction
DeckBuilder validates deck size, known definitions, and per-definition copy caps. It creates uniquely identified instances and applies seeded Fisher–Yates shuffle. The seed is for gameplay reproducibility, never security.

## Engine commands and zones
The shared domain engine owns draw pile, hands, discard pile, current player, direction, pending attack, score modifiers, match status, and sequenced events. Commands are Draw, PlayCard, Discard, EndTurn, and RespondToAttack. Invalid commands return typed errors without changing gameplay state. Unity is a presentation/input layer and does not implement card effects.

## Effect semantics
- Draw 2: draws up to two, bounded by hand limit and deck size.
- Skip Turn: marks a selected opponent to be skipped by turn scheduling.
- Reverse Order: reverses turn direction.
- Swap Hands: exchanges the acting player's hand with a selected opponent.
- Steal Card: pauses for target response; an unblocked attack transfers a target card.
- Discard Random Card: pauses for response; seeded PRNG selects the target card.
- Block Draw: pending attack; successful resolution prevents the target's next draw.
- Shield and Negate: cancel a pending attack.
- Reflect: redirects the pending effect to its original attacker.
- Wild: clears the active suit marker.
- Deck Reset: shuffles the discard pile back into the draw pile.
- Double Score: doubles the next standard-card score earned by that player.

## Defense and stacking
A pending attack transfers response control to its target. Shield and Negate cancel; Reflect redirects. Stacking is disabled by default. Ruleset.AllowStacking is an extension point; stack composition and ordering still require implementation before enabling it in production.

## Auditability
Accepted transitions append monotonically sequenced GameEvent records with actor, card instance, target, and effect data. Rejected commands do not append events. Match seed and catalog version are retained. Event serialization and replay verification are not implemented; the event list is an in-memory audit trail, not a replay engine.

## Remaining scope
The engine is Unity-independent and implements catalog, deck validation, seeded shuffle, draw/play/discard, hand limits, turn direction, core effects, attack/defense response, score modifiers, and event records. Full vitality/elimination, target-score and tie-break lifecycle, advanced stack semantics, and exhaustive effect interaction coverage remain open acceptance items.
