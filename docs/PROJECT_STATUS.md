# Draw Decks — Implementation Status

**Current phase:** Phase 1 — Core Game Foundation  
**Working branch:** `dev/foundation`  
**Pull request:** #1 (draft)

## Product
A turn-based strategy card game for 2–4 seats in the default competitive ruleset, with human and CPU opponents. One human versus CPU supports solo play.

## Architecture baseline
Unity/C# client; portable shared C# domain; ASP.NET Core API; PostgreSQL durable persistence; Redis transient coordination; SignalR planned for real-time communication; server-authoritative online match execution.

## Committed foundation
- Repository conventions, .NET SDK pin, CI workflow.
- Shared deterministic domain scaffold and xUnit tests.
- ASP.NET Core API and solution scaffold.
- Unity project skeleton and Input System package declaration.
- PostgreSQL schema baseline and PostgreSQL/Redis Compose.
- Product, architecture, rulebook, setup, and checklist documentation.

## Explicitly incomplete
- Full Confluence rules are specified but not yet implemented in the domain engine.
- Authentication, database runtime integration/migration runner, SignalR, matchmaking, LAN, and authoritative match workers remain future implementation.
- Unity project has not been imported or built by a licensed Unity Editor.
- No desktop/mobile release binaries exist yet.

## Acceptance gate
Phase 1 is not complete until .NET solution build and domain tests pass in CI, PostgreSQL/Redis startup is verified, and Unity Editor imports the project successfully. Platform acceptance requires actual per-platform builds and smoke tests.
