# Draw Decks

Cross-platform multiplayer strategy card game for 2–4 total seats in the default Confluence ruleset (human and CPU combinations supported).

## Current milestone
Phase 1 — Core Game Foundation. See [implementation checklist](docs/IMPLEMENTATION_CHECKLIST.md), [product specification](docs/PRODUCT_SPECIFICATION.md), and [default rulebook](docs/RULEBOOK.md).

## Architecture
- Unity + C# client; Windows, macOS, Linux first, Android/iOS planned.
- Shared deterministic C# domain library.
- ASP.NET Core 10 API foundation.
- PostgreSQL schema baseline and Redis local infrastructure.
- Planned authoritative match processes and SignalR real-time transport.

## Repository map
- `client/` — Unity project foundation
- `shared/DrawDecks.Domain/` — transport-independent rules/domain
- `server/` — ASP.NET Core solution and API
- `database/migrations/` — PostgreSQL schema
- `infra/` — local dependency orchestration
- `tests/` — automated tests
- `docs/` — product, architecture, rules, setup, and status

## Quick start
See [Development Setup](docs/DEVELOPMENT_SETUP.md).

```bash
dotnet restore server/DrawDecks.sln
dotnet build server/DrawDecks.sln -c Release
dotnet test tests/DrawDecks.Domain.Tests/DrawDecks.Domain.Tests.csproj -c Release
docker compose -f infra/docker-compose.yml up -d
```

The Unity Editor is required to import and build the client. Unity builds are not claimed as verified by the .NET CI workflow.

## Security
Never commit secrets, signing keys, certificates, or production connection strings. The included Compose password is local-development-only.

## License
Pending project-owner decision.
