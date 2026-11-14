# Draw Decks — System Architecture

## Runtime topology
Unity client → transport adapter → ASP.NET Core API/SignalR gateway → authoritative match application → shared domain engine. PostgreSQL stores durable records; Redis handles presence, matchmaking coordination, rate limits, and ephemeral leases.

## Architecture boundaries
- Unity presentation: scenes, UI, animation, audio, accessibility, localization.
- Unity application: use cases, view models, input actions, navigation.
- Client adapters: hot-seat, LAN, and online command/event transports.
- Shared domain: cards, rulesets, match aggregate, validation, deterministic transitions, scoring.
- Backend API: identity integration, profiles, decks, lobby, matchmaking, chat, administration.
- Match host: authoritative command sequencing, domain execution, event publication, snapshots, reconnect recovery.
- Persistence: PostgreSQL repositories/migrations; Redis ephemeral coordination only.
- Content management: versioned card catalog/ruleset manifests, validation, publication, rollback, match-pinned hashes.

## Communication contracts
Clients submit versioned commands with match ID, seat credential, command ID, expected state version, and payload. Server validates identity, seat ownership, turn, state version, rate limits, and domain invariants. SignalR carries sequenced real-time events; REST handles account/profile/catalog/lobby queries. LAN uses the same command/event contract over a local host transport.

## Database architecture
PostgreSQL stores users, identities, profiles, decks, card/ruleset versions, lobbies, matches, seat membership, event log, snapshots, results, reports, and audit records. Use UUIDs, UTC timestamps, foreign keys, unique constraints, and explicit migrations. Redis is never the sole store for durable data.

## Authentication and authorization
Use OIDC/OAuth-compatible identity providers. Validate issuer, audience, signature, expiry, and nonce. Map principals to account and seat permissions. Seat credentials are short-lived, scoped, revocable, and never logged. LAN guest access is explicit and host-controlled. Secrets are injected from environment/secret stores.

## Deployment
Local: Docker Compose PostgreSQL/Redis, .NET API, Unity Editor. Production: containerized API and match workers behind TLS ingress; managed PostgreSQL/Redis; object storage/CDN for versioned content; centralized logs, metrics, traces, backups, and alerts. Signed client binaries distributed per platform. Licensed/self-hosted Unity runners produce platform builds.

## Testing
Domain unit/property tests; API integration tests with disposable services; transport contract tests; deterministic replay tests; Unity EditMode/PlayMode tests; multiplayer soak/load tests; security tests; platform smoke tests.

## Failure and recovery
Reconnect resumes from an authorized snapshot plus event sequence. Idempotent command IDs prevent duplicate plays. Server leases prevent split-brain match ownership. PostgreSQL transactions persist event/state atomically. Redis loss may degrade presence/matchmaking but cannot erase durable results.
