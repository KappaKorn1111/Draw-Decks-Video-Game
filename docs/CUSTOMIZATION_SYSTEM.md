# Prompt 05 — Game Modes, Deck Builder & Custom Rules Engine

## Implemented in the shared domain

- Versioned `DeckProfile` stores stable card-definition IDs, catalog version, starter-deck metadata, and update timestamp.
- `DeckBuilderSession` supports create/edit/rename/remove/clear, catalog validation, duplicate limits, banned cards, enabled-card allowlists, special-card restrictions, and effect restrictions.
- `CardCatalogSearch` filters by text, card kind, rarity, effect, and availability.
- `DeckStatistics` reports card count, recognized/invalid cards, rarity distribution, and effect distribution.
- `RulesetProfile` models the requested configuration surface: mode, seat bounds, deck bounds, starter deck ID, duplicate limit, card allowlists/bans/restricted effects, special-card policy, starting hand, draw amount, maximum hand, turn timer, turn order, target score, health, elimination, maximum rounds, objective names, CPU count/difficulty, and CPU thinking delay.
- `RulesetPresets` provides named definitions for Classic, Elimination, Team Battle 2v2, Survival, and Custom.
- `ICustomizationStore` defines save/load/list/delete operations for decks and rulesets. `InMemoryCustomizationStore` provides a testable session-scoped implementation.
- `ConfiguredMatchFactory` validates a saved deck and ruleset against the active catalog before creating a deterministic match. Classic and Custom currently map to the existing score-based engine.

## Runtime gaps — not accepted as complete

The existing match state and engine still do not implement health/damage, team identity/shared team scoring, team-aware card targeting, survival wave progression/score, objective event tracking, turn timers, randomized turn order, or custom effect parameter execution. Elimination, Team Battle, and Survival profiles are described and validated but deliberately rejected by `ConfiguredMatchFactory` rather than silently launching as Classic.

The current Unity Deck Builder UI remains preset-oriented. The new domain session is not yet wired into Unity screens. The in-memory store does not survive process restart; Unity persistentDataPath / browser IndexedDB / server-backed durable storage still needs an implementation. Community sharing endpoints, canonical serialization, signatures/version migration, and server-side persistence are not implemented.

## Acceptance status

**NEEDS REVISION.** Domain-level customization and validation foundations are committed on this branch, but the requested end-to-end acceptance criteria are not yet met. In particular, all five modes must run with their actual mechanics, and deck/ruleset profiles must be durably saved and loaded through the client UI before Prompt 05 can pass.
