# Architecture

## Dependency direction
Unity presentation and platform adapters depend on the shared domain. Backend application services depend on the same domain contracts. The domain has no Unity, ASP.NET, database, Redis, or transport dependency.

## Components
- `shared/DrawDecks.Domain`: deterministic rules, match state, validation, scoring.
- `client/`: Unity UI, input, presentation, local adapters, client networking adapters.
- `server/`: ASP.NET Core API, identity/session orchestration, SignalR gateways, authoritative match hosting.
- `infra/`: PostgreSQL, Redis, container and deployment definitions.
- `tests/`: domain, API, persistence, networking, and end-to-end test suites.

## Authority and security
Online clients submit intent, never authoritative state. The server validates every action against the shared rules engine and publishes resulting state. Clients must not decide scores, turn ownership, winners, or random outcomes. Match seeds are reproducibility metadata, not secrets. Authentication and authorization are enforced server-side.

## Persistence boundaries
PostgreSQL stores durable accounts, profiles, deck definitions, match records, and results. Redis stores ephemeral presence, matchmaking coordination, and short-lived session state. Durable records must not rely on Redis as their only copy.

## Transport boundaries
Hot-seat, LAN, and online play call the same domain action interface. Transport adapters serialize commands and events; they do not reimplement game rules.

## Determinism
Shuffle randomness is explicitly seeded and implemented in the domain layer. The generator is for reproducible gameplay only and must never be used for credentials, tokens, or cryptographic material.
