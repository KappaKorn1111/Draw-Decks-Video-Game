# Draw Decks — Implementation Status

**Current phase:** Phase 3 — Playable Unity Client & Local Match System  
**Working branch:** dev/foundation  
**Pull request:** #1 (open; not merged)

## Phase 2 — Shared card system and rules engine

- Versioned catalog with 52 standard cards and all 13 requested action, attack, defense, and special definitions.
- Unique physical card instances, rarity, availability, values, suits, ranks, and per-definition copy limits.
- Deck validation, deterministic seeded shuffle, deal, draw, hand limits, play, discard, reverse direction, skip scheduling, and suit selection.
- Domain resolution for Draw 2, Swap Hands, Steal, Random Discard, Block Draw, Shield, Reflect, Negate, Wild, Deck Reset, and Double Score.
- Typed command failures, elimination/target-score victory, sequenced transition events, accepted-command records, and deterministic replay/equivalence checks.

## Phase 3 — Playable local Unity client

- Runtime-generated main menu with Play, Solo Practice, Local Multiplayer, Online Multiplayer, Deck Builder, Collection, Statistics, Settings, Support, and Exit.
- Match setup for 2–4 seats, player names, Human/CPU selection, Classic/Quick/Marathon score targets, Confluence/Classic deck presets, and turn timer selection.
- Unity consumes the shared domain source through a local UPM package; domain source uses C# 9-compatible syntax for Unity 6.3.
- Input System UI event module, local CPU opponent policy, human card selection, draw/play/pass, target/suit selection, attack responses, and defense cards.
- Hot-seat privacy screen between human seats, turn timer, scoreboard, opponent hand counts, deck/discard counts, card entrance animation, match notifications/history, pause, rematch, results, and local statistics.
- Editor bootstrap creates a Main scene and registers it in Build Settings.
- Automated .NET seat-composition acceptance test covers 1 human vs 1–3 CPUs, 2 humans vs 2 CPUs, 3 humans vs 1 CPU, and 4 humans.

## Explicit limitations

- Unity Editor import, Play Mode, and standalone player build have not yet been executed in this environment.
- Online multiplayer, LAN, authentication, lobbies, chat, authoritative network hosting, and persistent match storage remain later phases.
- Effect stacking remains disabled by the shared ruleset.
- The current rules engine is score-based and does not expose vitality/health; the client displays points, not artificial health indicators.
- CPU behavior is a straightforward score-oriented policy, not a difficulty system.
- Deck Builder currently offers two supported presets. Full user-defined deck construction, collection unlocks, monetization, and a payment endpoint are not part of this milestone.

## Acceptance state

**Implementation status:** IMPLEMENTED — local client source, shared Unity package integration, and seat-composition tests are committed.  
**CI status:** PASS on commit a57cd8b1099ad9e021de85cb64fded3489e3c80d — Release build succeeded; 27 tests passed, 0 failed, 0 skipped.  
**Unity verification:** Pending. Unity Editor import, Play Mode end-to-end match, and desktop player build must still be executed before Prompt 03 is fully accepted.  
