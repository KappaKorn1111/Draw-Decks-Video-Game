# Draw Decks

A cross-platform multiplayer strategy card game for 1–4 total seats (human and CPU).

## Project status
Active development. See [docs/PROJECT_STATUS.md](docs/PROJECT_STATUS.md) for the implementation checklist, architecture, and current default ruleset.

## Architecture
- **Client:** Unity + C# (Windows, macOS, Linux first; Android/iOS architecture-compatible).
- **Shared rules:** deterministic C# domain engine.
- **Backend:** ASP.NET Core Web API, PostgreSQL, Redis, SignalR.
- **Online matches:** server-authoritative match processes.
- **Modes:** hot-seat, LAN, and online multiplayer.

## Repository layout
- `client/` — Unity project (introduced in client milestone)
- `server/` — ASP.NET Core services
- `shared/` — engine-independent C# domain and rules
- `tests/` — automated test suites
- `infra/` — Docker and deployment configuration
- `docs/` — architecture and operations

## Build and test the domain engine
```bash
dotnet test tests/DrawDecks.Domain.Tests/DrawDecks.Domain.Tests.csproj
```

## Prerequisites
.NET 10 SDK, Unity Editor with required platform modules, Docker Engine/Desktop, and Git.

## Security
Never commit credentials, signing keys, production connection strings, or local environment files. Use environment variables or a secrets manager.

## License
License selection is pending project-owner approval.
