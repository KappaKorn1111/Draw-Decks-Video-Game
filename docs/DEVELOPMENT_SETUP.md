# Development Setup

## Requirements
.NET 10 SDK, Unity Editor 6000.3 LTS with desktop build modules, Docker Engine/Desktop, and Git. Add Android/iOS modules when enabling those targets.

## Clone and build backend
```bash
git clone https://github.com/KappaKorn1111/Draw-Decks-Video-Game.git
cd Draw-Decks-Video-Game
dotnet restore server/DrawDecks.sln
dotnet build server/DrawDecks.sln -c Release
dotnet test tests/DrawDecks.Domain.Tests/DrawDecks.Domain.Tests.csproj -c Release
dotnet run --project server/DrawDecks.Api/DrawDecks.Api.csproj
```
Set `ASPNETCORE_URLS=http://localhost:5080`; health: `/health/live`.

## Start local services
```bash
cp .env.example .env
docker compose -f infra/docker-compose.yml up -d
```
Sample password is local-only. PostgreSQL init scripts run only on first initialization of an empty data volume.

## Open Unity
Open `client/` in Unity Hub using the version in `client/ProjectSettings/ProjectVersion.txt`. Allow package resolution to finish. Hosted .NET CI has no Unity Editor/license; editor import and player builds require a licensed Unity workstation/runner.

## Secrets and workflow
Never commit .env, tokens, signing keys, certificates, or production credentials. Inject production secrets through a deployment secret manager. Use feature branches; CI must pass before review/merge.
