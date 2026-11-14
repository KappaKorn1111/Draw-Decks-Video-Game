# Draw Decks

A strategy card game for 2–4 total seats, supporting human and CPU players.

## Current milestone

Phase 4 — CPU Opponents & Intelligent Game AI, followed by a stabilization pass. The shared C# domain includes four CPU difficulty levels, legal-action candidate generation, hidden-information-safe observations, seeded decision-making, turn-order/skip/replay safeguards, deck-exhaustion and maximum-turn termination, and automated match simulations.

See [CPU AI](docs/CPU_AI.md), [Stabilization Audit](docs/STABILIZATION_AUDIT.md), [Local Client](docs/LOCAL_CLIENT.md), [Project Status](docs/PROJECT_STATUS.md), the [target rulebook](docs/RULEBOOK.md), and [implementation checklist](docs/IMPLEMENTATION_CHECKLIST.md).

## Architecture

- Unity 6.3 + C# client; Windows, macOS, and Linux are the initial desktop targets.
- Shared deterministic C# domain package imported by Unity and used by the server.
- Shared CPU decision engine; no client-side duplicate rules.
- ASP.NET Core 10 API foundation.
- PostgreSQL schema baseline and Redis local infrastructure.
- Planned authoritative online match processes and SignalR real-time transport.

## Repository map

- client/ — Unity project, runtime UI, and editor setup.
- shared/DrawDecks.Domain/ — shared card rules, match state, legal-action validation, and CPU AI.
- server/ — ASP.NET Core solution and API foundation.
- database/migrations/ — PostgreSQL schema baseline.
- infra/ — local dependency orchestration.
- tests/ — automated domain, local-match, and CPU AI tests.
- docs/ — product, architecture, rules, setup, status, and implementation notes.

## Quick start

See [Development Setup](docs/DEVELOPMENT_SETUP.md) for backend tooling. To launch the local playable game, open client/ in Unity 6000.3.x, allow package resolution, open Assets/Scenes/Main.unity, and press Play. The editor bootstrap creates the launch scene and adds it to Build Settings when the project is first opened.

The shared local Unity package is resolved from shared/DrawDecks.Domain via file:../../shared/DrawDecks.Domain in client/Packages/manifest.json. Use the same repository folder layout; do not move client/ independently from shared/.

    dotnet restore server/DrawDecks.sln
    dotnet build server/DrawDecks.sln -c Release
    dotnet test tests/DrawDecks.Domain.Tests/DrawDecks.Domain.Tests.csproj -c Release
    docker compose -f infra/docker-compose.yml up -d

The .NET CI workflow validates the shared domain, CPU AI, and automated match simulations. Unity Editor import, Play Mode, and player builds require a Unity Editor installation and are reported separately from .NET CI.

## Known scope

Online multiplayer, LAN discovery, authentication, chat, persistent match hosting, and real payment/support integrations are not active in the local playable milestone. Effect stacking is disabled by the current ruleset. The Confluence rulebook is a target specification; its full vitality, combat, combo, and scoring mechanics are not yet implemented.

## Security

Never commit secrets, signing keys, certificates, or production connection strings. The included Compose password is local-development-only.

## License

This repository's original software code is licensed under the [Apache License, Version 2.0](LICENSE). Third-party assets and dependencies may carry separate licenses; review their notices before redistribution.

See [Prompt 05 customization status](docs/CUSTOMIZATION_SYSTEM.md) for the current deck builder and ruleset implementation scope.
