# Draw Decks

A strategy card game for 2–4 total seats, supporting human and CPU players.

## Current milestone

Phase 3 — Playable Unity Client & Local Match System. The project now includes a runtime-built Unity menu, local match setup, hot-seat handoffs, CPU turns, card actions, and match results. See [Local Client](docs/LOCAL_CLIENT.md), [Project Status](docs/PROJECT_STATUS.md), the [rulebook](docs/RULEBOOK.md), and [implementation checklist](docs/IMPLEMENTATION_CHECKLIST.md).

## Architecture

- Unity 6.3 + C# client; Windows, macOS, and Linux are the initial desktop targets.
- Shared deterministic C# domain package imported by Unity and used by the server.
- ASP.NET Core 10 API foundation.
- PostgreSQL schema baseline and Redis local infrastructure.
- Planned authoritative online match processes and SignalR real-time transport.

## Repository map

- client/ — Unity project, runtime UI, and editor setup.
- shared/DrawDecks.Domain/ — shared card rules, match state, and domain package metadata.
- server/ — ASP.NET Core solution and API foundation.
- database/migrations/ — PostgreSQL schema baseline.
- infra/ — local dependency orchestration.
- tests/ — automated domain and local-match acceptance tests.
- docs/ — product, architecture, rules, setup, status, and client notes.

## Quick start

See [Development Setup](docs/DEVELOPMENT_SETUP.md) for backend tooling. To launch the local playable game, open client/ in Unity 6000.3.x, allow package resolution, open Assets/Scenes/Main.unity, and press Play. The editor bootstrap creates the launch scene and adds it to Build Settings when the project is first opened.

    dotnet restore server/DrawDecks.sln
    dotnet build server/DrawDecks.sln -c Release
    dotnet test tests/DrawDecks.Domain.Tests/DrawDecks.Domain.Tests.csproj -c Release
    docker compose -f infra/docker-compose.yml up -d

The .NET CI workflow validates the shared domain. Unity Editor import, Play Mode, and player builds require a Unity Editor installation and are reported separately from .NET CI.

## Known scope

Online multiplayer, LAN discovery, authentication, chat, persistent match hosting, and real payment/support integrations are not active in the local playable milestone. Effect stacking is disabled by the current ruleset.

## Security

Never commit secrets, signing keys, certificates, or production connection strings. The included Compose password is local-development-only.

## License

Pending project-owner decision.
