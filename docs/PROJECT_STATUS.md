# Draw Decks — Implementation Status

**Current phase:** Phase 4 — CPU Opponents & Intelligent Game AI  
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
- Input System UI event module, human card selection, draw/play/pass, target/suit selection, attack responses, and defense cards.
- Hot-seat privacy screen between human seats, turn timer, scoreboard, opponent hand counts, deck/discard counts, card entrance animation, match notifications/history, pause, rematch, results, and local statistics.
- Editor bootstrap creates a Main scene and registers it in Build Settings.

## Phase 4 — CPU opponents and intelligent game AI

- Shared-domain CPU decision engine with Easy, Normal, Hard, and Expert difficulty policies.
- Legal candidate generation calls the same GameEngine validation methods as human and CPU command execution.
- Easy uses seeded random legal-action selection. Normal prioritizes tactical value, scores, and basic defense.
- Hard adds threat weighting, resource management, estimated unseen-deck composition, and stronger target selection.
- Expert adds bounded look-ahead over the CPU's own remaining hand, risk weighting, and opponent behavior modeling from public action history.
- Restricted CpuAiObservation exposes the acting player's own hand, public discard history, opponent scores/hand counts, and probabilistic deck estimates—not opponent hand identities or draw-pile order.
- Per-seat CPU difficulty controls in Unity setup; configurable thinking delays and Fast/Balanced/Deep evaluation budgets.
- CPU turns can draw once, reevaluate, then play, defend, accept an attack, or end their turn through the shared engine.
- Safe fallback handling is included for unexpected command-validation disagreement.

## Verification

CI passed on commit 815ba24ebbf8f1ddb3f7719ffed03e6e61218acd:
- Backend/shared-domain Release build succeeded.
- 32 tests passed, 0 failed, 0 skipped.
- CPU simulations covered Easy, Normal, Hard, and Expert for 2-, 3-, and 4-seat matches (up to three CPU opponents).
- Seeded decision repeatability, Easy randomization, Normal card prioritization, Expert public-opponent modeling, hidden-information boundary, and bounded performance controls are covered.

Run: https://github.com/KappaKorn1111/Draw-Decks-Video-Game/actions/runs/43562746785

## Explicit limitations

- Unity Editor import, Play Mode, and standalone player build have not yet been executed in this environment.
- Expert look-ahead is bounded heuristic planning over known hand contents, not full expectimax or hidden-state search.
- Deck composition is estimated from visible information and unseen-card counts, not the true shuffled draw-pile order.
- CPU profiles do not change game rules or grant hidden information.
- Online multiplayer, LAN, authentication, lobbies, chat, authoritative network hosting, and persistent match storage remain later phases.
- Effect stacking remains disabled by the shared ruleset.
- The current rules engine is score-based and does not expose vitality/health; the client displays points, not artificial health indicators.
- Deck Builder currently offers two supported presets. Full user-defined deck construction, collection unlocks, monetization, and a payment endpoint are not part of this milestone.

## Acceptance state

**Prompt 04 implementation:** BUILT — shared AI engine, Unity integration, difficulty/performance controls, and tests are committed.  
**CI status:** PASS — 32/32 tests on commit 815ba24ebbf8f1ddb3f7719ffed03e6e61218acd.  
**Unity verification:** Pending. Unity Editor import and Play Mode CPU-turn walkthroughs must still be completed before full Unity runtime acceptance.
