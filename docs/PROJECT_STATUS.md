# Draw Decks — Implementation Status

**Phase:** Prompt 1 of 12 — Repository foundation and architecture  
**Canonical branch:** main  
**Working branch:** dev/foundation

## Product target
A multiplayer strategy card game for 1–4 total seats, including human and CPU players.

## Target architecture
- Unity + C# client for Windows, macOS, and Linux.
- Shared deterministic C# rules engine, independent of Unity.
- ASP.NET Core Web API and authoritative match service.
- PostgreSQL persistence; Redis transient state and distributed coordination.
- SignalR real-time transport.
- Hot-seat, LAN, and online multiplayer.
- Android and iOS as additional client targets after desktop validation.
- Automated tests, CI, release automation, security controls, and deployment documentation.

## Checklist
- [x] Inspect repository and preserve original README.
- [x] Create isolated development branch.
- [x] Define initial repository architecture.
- [x] Establish initial deterministic domain engine and tests.
- [ ] Unity client and input integration.
- [ ] Backend API, authentication, persistence, and migrations.
- [ ] Authoritative online match process and SignalR.
- [ ] LAN transport and reconnect/recovery.
- [ ] CPU opponent strategy and difficulty levels.
- [ ] Full UI, accessibility, settings, and localization.
- [ ] CI, security scanning, release pipelines, and deployment.
- [ ] Platform builds and smoke tests for each supported OS.

## Default ruleset v0.1 — High Card
The initial playable ruleset is intentionally small, deterministic, and configurable:
1. A match has 1–4 seats. A solo seat plays against a built-in neutral CPU seat only when configured; a match must contain at least one human and at least one opponent.
2. Each seat receives five cards from a standard 52-card deck.
3. The match lasts five rounds. Each active seat, in turn order, plays exactly one card from their hand.
4. The highest rank in a round wins that round. Rank order is 2–10, Jack=11, Queen=12, King=13, Ace=14. Equal ranks share the round win.
5. A seat earns one point per round won. Highest total score wins; tied highest scores produce a draw.
6. Played cards are removed from the hand and are not recycled. The initial deal and turn order are derived from the match seed.
7. A player may only play a card actually present in their hand, only on their turn, and only while the match is active.
8. Ruleset values are data/configuration, not client-only behavior. Future rulesets can replace the scoring and round policy without changing transports.

This is the initial documented baseline, not a claim that all future game modes are complete. Balance and UX playtesting may revise it.

## Acceptance policy
A platform is not complete until its target build is produced, automated checks pass, and a smoke test is documented on that platform. Android/iOS additionally require their SDKs, signing configuration, and device validation.

## Environment prerequisites
.NET 10 SDK, Unity Editor with platform modules, Docker Engine/Desktop, and Git are required for the full local workflow. Xcode is required for iOS builds and macOS; Android SDK/NDK/JDK are required for Android builds.
