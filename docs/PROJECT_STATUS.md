# Draw Decks — Implementation Status

**Current phase:** Phase 2 — Complete Card System & Game Rules Engine  
**Working branch:** dev/foundation  
**Pull request:** #1 (draft)

## Phase 2 implemented
- Versioned catalog with 52 standard cards and all 13 requested action, attack, defense, and special definitions.
- Unique physical card instances, rarity, availability, values, suits, ranks, and per-definition copy limits.
- Deck validation, deterministic seeded shuffle, deal, draw, hand limits, play, discard, reverse direction, skip scheduling, and suit selection.
- Domain resolution for Draw 2, Swap Hands, Steal, Random Discard, Block Draw, Shield, Reflect, Negate, Wild, Deck Reset, and Double Score.
- Typed command failures, elimination/target-score victory, sequenced transition events, accepted-command records, and deterministic replay/equivalence checks.
- Automated coverage for catalog definitions, deck constraints, seeded shuffle, draw/discard, hand limits, empty deck, invalid commands, attack/defense, card effects, elimination, victory, and replay.

## Remaining work
- Full stacking behavior is intentionally disabled; the ruleset rejects stacking as a default capability and the composition policy remains future work.
- Persistent event storage, tamper-evident audit signatures, online authoritative command hosting, authentication, SignalR, matchmaking, and LAN remain future phases.
- Unity Editor import/build and Docker service startup have not been verified.

## Acceptance
The card engine runs independently of Unity. Phase 2 implementation is committed on the development branch; final acceptance is pending the latest CI run and review of replay/effect edge cases.
