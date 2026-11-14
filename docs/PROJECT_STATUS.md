# Draw Decks — Implementation Status

**Current phase:** Phase 5 — Game Modes & Customization Runtime  
**Working branch:** feature/prompt-05-mode-runtime  
**Pull request:** #3 (open; not merged)

## Phase 2 — Shared card system and rules engine

- Versioned catalog with 52 standard cards and 14 action, attack, defense, and special definitions, including Strike damage.
- Unique physical card instances, rarity, availability, values, suits, ranks, and per-definition copy limits.
- Deck validation, deterministic seeded shuffle, deal, draw, hand limits, play, discard, reverse direction, skip scheduling, and suit selection.
- Domain resolution for Draw 2, Swap Hands, Steal, Random Discard, Block Draw, Shield, Reflect, Negate, Wild, Deck Reset, and Double Score.
- Typed command failures, target-score victory, sequenced transition events, accepted-command records, and deterministic replay/equivalence checks.

## Phase 3 — Playable local Unity client

- Runtime-generated main menu with Play, Solo Practice, Local Multiplayer, Online Multiplayer, Deck Builder, Collection, Statistics, Settings, Support, and Exit.
- Match setup for 2–4 seats, player names, Human/CPU selection, Classic/Quick/Marathon score targets, Confluence/Classic deck presets, and turn timer selection.
- Unity consumes the shared domain source through a local UPM package; domain source uses C# 9-compatible syntax for Unity 6.3.
- Input System UI event module, human card selection, draw/play/pass, target/suit selection, attack responses, and defense cards.
- Hot-seat privacy handoff, CPU-thinking screen, turn timer, scoreboard, opponent hand counts, deck/discard counts, card entrance animation, match notifications/history, pause, rematch, results, and local statistics.
- Editor bootstrap creates a Main scene and registers it in Build Settings.

## Phase 4 — CPU opponents and intelligent game AI

- Shared-domain CPU decision engine with Easy, Normal, Hard, and Expert difficulty policies.
- Legal candidate generation calls the same GameEngine validation methods as human and CPU command execution.
- Easy uses seeded random legal-action selection. Normal prioritizes tactical value, scores, and basic defense.
- Hard adds threat weighting, resource management, estimated unseen-deck composition, and stronger target selection.
- Expert adds bounded look-ahead over the CPU's own remaining hand, risk weighting, and opponent behavior modeling from public action history.
- Restricted CpuAiObservation exposes the acting player's own hand, public discard history, opponent scores/hand counts, and probabilistic deck estimates—not opponent hand identities or draw-pile order.
- Per-seat CPU difficulty controls in Unity setup; configurable thinking delays and Fast/Balanced/Deep evaluation budgets.
- CPU turns draw at most once, reevaluate, then play, defend, accept an attack, or end the turn through the shared engine.

## Stabilization — correctness and deadlock fixes

- Attack responses restore the attacker's seat as the turn origin before advancing, fixing wrong next-player selection in matches with three or four seats.
- Skip effects are stored as a set of pending targets rather than a single overwriteable target. Multiple skips resolve in order, and eliminated targets are removed.
- Victory resolution is idempotent after a terminal result, preventing duplicate MatchWon/MatchDraw events.
- Draw-pile exhaustion plus two consecutive non-scoring turns by every living player resolves through deterministic sudden score.
- Rulesets now have a configurable MaximumTurns safety bound (default 500) to stop endless deck-reset/no-scoring loops. At the bound, the engine resolves using score and then hand count; exact ties are draws.
- Replay equivalence includes queued skips, non-scoring streak state, scoring-in-progress state, and all ruleset settings including MaximumTurns.
- Deck/catalog validation rejects null/blank IDs, invalid size bounds, null definitions, and null lookup inputs deterministically.
- Competitive MatchFactory creation enforces 2–4 total seats.
- CPU defense evaluation avoids spending defenses on attacks with no effect; Unity CPU coroutine errors have a safe fallback and stale routines are invalidated across menu/rematch changes.
- Unity action legality indicators now call the shared domain validators. The CPU-thinking screen hides the previous human's hand during the CPU turn.

## Verification

GitHub Actions passed on commit 8ccf9137c297d59d3f35b18fae603891bdafe205:
- Backend/shared-domain Release build succeeded with 0 warnings and 0 errors.
- **45 tests passed, 0 failed, 0 skipped.**
- CPU simulation suite covers **120 deterministic matches**: Easy, Normal, Hard, and Expert across 2-, 3-, and 4-seat matches, with ten seeds for each combination.
- Regression tests cover non-next-seat attack responses, multiple queued skips, elimination of skipped players, replay of queued skips, ineffective CPU defense, deck validation, deck-exhaustion sudden score, maximum-turn stalemate protection, single terminal-event emission, maximum-turn sudden-score replay, and target-score terminal-event idempotence.

Run: https://github.com/KappaKorn1111/Draw-Decks-Video-Game/actions/runs/43569298132

## Remaining gaps — do not treat as verified

- Unity Editor import, Play Mode walkthroughs, and standalone player builds have not been executed in this environment. The .NET workflow does not compile or exercise the Unity UI scripts.
- The existing score-based engine is not yet a complete implementation of the Confluence target rulebook. Vitality/health, Strike/Guard damage reactions, Forge pair scoring, two/three-card plays, the full Jack/Queen/King/Ace behavior, and the specified final Sudden Score turn sequence are not implemented by the current engine.
- The current sudden-score safety resolver ranks prestige/score, then hand count. It cannot apply the rulebook's vitality tie-break because the engine has no vitality state.
- The current match factory still starts with the first configured seat; random first-player selection from the product rulebook is not implemented.
- Expert look-ahead is bounded heuristic planning over known hand contents, not full expectimax or hidden-state search. Deck composition is probabilistic.
- Effect stacking remains disabled.
- Online multiplayer, LAN discovery, authentication, lobbies, chat, authoritative network hosting, and persistent match storage remain later phases.
- Deck Builder currently offers two presets. Free-form deck construction, persistent collection progression, monetization, and support payment integration remain future work.

## Acceptance state

**Stabilization status:** PASS for the shared C#/.NET domain and CPU simulation scope described above.  
**CI:** PASS — 45/45 tests on commit 8ccf9137c297d59d3f35b18fae603891bdafe205.  
**Unity runtime acceptance:** PENDING — Editor import, Play Mode CPU matches, UI/hot-seat walkthroughs, and desktop player builds remain required.  
**Confluence rulebook parity:** NEEDS IMPLEMENTATION — the listed ruleset gaps must be resolved before claiming full product-rulebook compliance.


## Prompt 05 — customization foundation

- Added immutable, versioned deck and ruleset profile models.
- Added deck editing sessions, card catalog search/filtering, deck rarity/effect statistics, and duplicate/banned/allowlist/special/effect validation.
- Added five named mode presets and configured Classic/Custom match creation validation.
- Added a storage interface and in-memory implementation for tests and session workflows.
- Added eight customization regression tests.
- GitHub Actions run 43572435711 passed: Release build succeeded with 0 warnings and 0 errors; **53 tests passed, 0 failed, 0 skipped**.
- Elimination, Team Battle, and Survival runtime mechanics; durable local persistence; Unity editor integration; and community sharing/server serialization remain unimplemented.

Prompt 05 acceptance remains **NEEDS REVISION**. See [Customization System](CUSTOMIZATION_SYSTEM.md).


## Phase 5 — Game modes and customization runtime

- Added immutable, versioned deck and ruleset profiles, named presets for Classic, Elimination, Team Battle, Survival, and Custom, plus configured-match validation.
- Added deck editing sessions, card catalog search/filtering, rarity/effect statistics, and duplicate/banned/allowlist/special/effect restrictions.
- Added player health and Strike damage attacks with Shield, Negate, and Reflect responses. Zero health eliminates a player and triggers mode-specific victory resolution.
- Elimination uses last-player-standing victory and health-based deterministic safety resolution.
- Team Battle assigns four seats to two teams, tracks shared team scores, prevents friendly targeted interactions, and supports score- or elimination-based team victory.
- Survival supports one human versus CPU opponents, wave clearing, CPU respawn, increasing CPU health, survival scoring, deterministic discard recycling, progressive CPU difficulty, and leaderboard-ready result records.
- Unity setup now selects all five modes. The HUD displays health, team totals, and Survival wave/score state; local Survival best score persists in PlayerPrefs.
- Added deterministic mode tests for damage, elimination, team assignment/scoring/friendly fire, Survival progression/replay, leaderboard projection, and configured match creation.
- GitHub Actions run #239 passed on commit 66ae2420bc57180bbcdebd70fcb4f72f420c9a65: Release build had 0 warnings and 0 errors; **63 tests passed, 0 failed, 0 skipped**.

## Remaining acceptance gates

- Unity Editor import, Play Mode, and desktop/mobile player builds still require a licensed Unity workstation or runner; hosted CI only compiles the .NET shared domain/backend.
- Unity's deck builder remains preset-only; the new domain editor is not yet connected to full deck creation/editing screens.
- Deck/ruleset persistence is currently represented by the in-memory domain store; durable Unity profile persistence and community/server sharing endpoints are not implemented.
- Objective-based victory, arbitrary custom card effect parameters, and online leaderboard submission remain future work.

Prompt 05 mode-runtime mechanics are implemented and .NET verified. Overall Prompt 05 acceptance remains **NEEDS REVISION** until the remaining client persistence/editor and Unity verification gates pass.
