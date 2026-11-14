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

Health/damage, Team Battle scoring and targeting, and Survival wave progression are now implemented in the shared runtime. See [Mode Runtime](MODE_RUNTIME.md). Objective event tracking, randomized turn order, turn timer enforcement inside the authoritative engine, and arbitrary custom effect parameter execution remain outstanding.

The Unity client now exposes the five-mode selector and displays health, team scores, and Survival progression. The Deck Builder UI remains preset-oriented, and the domain deck editor/store are not yet wired into Unity. Durable deck/ruleset profile persistence, community sharing endpoints, canonical serialization, signatures/version migration, and server-side persistence are not implemented.

## Acceptance status

**NEEDS REVISION.** Classic, Elimination, Team Battle, and Survival have their principal match mechanics; Custom currently uses the score-based engine with configured deck/rules validation. Full Prompt 05 acceptance still requires Unity deck/rules editor UI, durable saved deck/ruleset profiles, and community/server sharing. Unity Editor import and Play Mode verification are also outstanding.
