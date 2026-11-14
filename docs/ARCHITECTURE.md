# Draw Decks — System Architecture

Unity client → transport adapter → ASP.NET Core API/SignalR gateway → authoritative match application → shared domain engine. PostgreSQL stores durable records; Redis handles presence, matchmaking coordination, rate limits, and ephemeral leases.

## Boundaries
- Unity presentation: scenes, UI, animation, audio, accessibility, localization.
- Unity application: use cases, view models, input actions, navigation.
- Client adapters: hot-seat, LAN, online command/event transports.
- Shared domain: cards, rulesets, match aggregate, validation, deterministic transitions, scoring.
- Backend API: identity integration, profiles, decks, lobby, matchmaking, chat, administration.
- Match host: authoritative command sequencing, domain execution, event publication, snapshots, reconnect recovery.
- PostgreSQL repositories/migrations; Redis ephemeral coordination only.
- Content management: versioned card catalog/ruleset manifests, validation, publication, rollback, match-pinned hashes.

## Communication
Clients submit versioned commands with match ID, seat credential, command ID, expected state version, and payload. Server validates identity, seat ownership, turn, state version, rate limits, and domain invariants. SignalR carries sequenced real-time events; REST handles account/profile/catalog/lobby queries. LAN uses the same command/event contract over local host transport.

## Persistence
PostgreSQL stores users, identities, profiles, decks, card/ruleset versions, lobbies, matches, seat membership, event log, snapshots, results, reports, and audit records. Use UUIDs, UTC timestamps, foreign keys, unique constraints, explicit migrations. Redis is never the sole store for durable data.

## Identity and security
Use OIDC/OAuth-compatible providers. Validate issuer, audience, signature, expiry, and nonce. Map principals to account/seat permissions. Seat credentials are short-lived, scoped, revocable, and never logged. LAN guest access is explicit and host-controlled. Inject secrets from environment/secret stores.

## Deployment
Local: Docker Compose PostgreSQL/Redis, .NET API, Unity Editor. Production: containerized API and match workers behind TLS ingress; managed PostgreSQL/Redis; object storage/CDN for versioned content; centralized logs, metrics, traces, backups, alerts. Signed client binaries distributed per platform. Licensed/self-hosted Unity runners produce platform builds.

## Testing and recovery
Domain unit/property tests; API integration tests with disposable services; transport contract tests; deterministic replay; Unity EditMode/PlayMode; multiplayer load/soak; security; platform smoke tests. Reconnect resumes from authorized snapshot plus event sequence. Idempotent command IDs prevent duplicate plays. Server leases prevent split-brain. PostgreSQL transactions persist events/state atomically; Redis loss cannot erase durable results.
