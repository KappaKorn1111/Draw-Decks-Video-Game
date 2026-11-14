# Draw Decks — Stabilization Audit

**Scope:** Shared C# domain, CPU decision engine, Unity local-match orchestration source, and automated regression coverage.  
**Branch:** dev/foundation  
**Status:** .NET domain stabilization verified; Unity runtime acceptance remains pending.

## Verified corrections

| Area | Failure mode | Stabilization |
|---|---|---|
| Attack turn order | A non-adjacent target's response advanced from the defender's seat. | Restore attacker as the turn origin before advancing. |
| Skip effects | A single stored target allowed one skip to overwrite another. | Store queued targets in a set; resolve consecutive skips and remove eliminated targets. |
| Terminal results | Follow-on command flow could re-run victory evaluation after a result. | Make victory evaluation idempotent after Completed/Draw. |
| Empty-deck stall | All seats could repeatedly pass with no playable scoring card. | Count consecutive non-scoring turns and resolve deterministic sudden score after deck exhaustion and two quiet turns per living seat. |
| Endless recycle loops | Deck Reset can recycle the discard pile and prevent literal deck exhaustion. | Rulesets now have a maximum-turn safety bound (default 500); exceeding it triggers deterministic score/hand resolution. |
| Replay integrity | Replay comparison omitted skip/stagnation/ruleset state. | Compare skip queues, non-scoring streaks, in-progress scoring state, and ruleset settings including MaximumTurns. |
| Deck/catalog inputs | Null IDs, malformed limits, null definitions, and null lookup keys were not handled consistently. | Validate arguments explicitly and return validation errors for malformed deck IDs. |
| Match setup | Factory allowed a single-seat competitive match. | Require 2–4 configured seats and at least one human. |
| CPU defense | CPU could spend a defense on an attack that had no effect. | Score defense against actual incoming/reflected impact. |
| Unity CPU flow | Coroutine errors or stale routines could leave a CPU turn stuck or mutate a new match. | Exception recovery, validated fallback, match-generation guard, and pause-aware execution. |
| Hot-seat privacy | Previous human's hand remained visible during CPU thinking. | Show a separate CPU-thinking screen and use shared validators for card legality indicators. |

## Automated evidence

The successful GitHub Actions run on code commit 214e0a908a670750cfd365ebed2a5a65efb05f00 reported:
- Backend/shared-domain Release build succeeded with 0 warnings and 0 errors.
- 43 tests passed, 0 failed, 0 skipped.
- 120 deterministic CPU match simulations: four difficulty levels × three player counts (2, 3, 4 seats) × ten seeds.
- Regression coverage for attack turn ordering, multiple skips, skip-target elimination, skip replay, ineffective CPU defense, invalid deck inputs, sudden score, maximum-turn termination, and single terminal-result emission.

Run: https://github.com/KappaKorn1111/Draw-Decks-Video-Game/actions/runs/43568682436

## Not verified in this environment

- Unity Editor package import, C# compilation against the actual Unity assemblies, Play Mode, and desktop player builds.
- Full UI-driven CPU match walkthroughs, including hot-seat and pause/resume timing.
- Actual desktop input, display sizing, animation, and audio behavior.

The GitHub workflow compiles the .NET shared domain and API. It does not compile Unity-specific MonoBehaviours or validate the Unity scene lifecycle.

## Product rulebook parity gap

The current engine is not yet a complete implementation of the Confluence target rulebook. The target specification describes:
- 20 vitality per player and elimination at zero.
- Rank-mapped Strike, Guard, Forge, Tactic, and Crest card semantics.
- Guard reactions that reduce Strike damage.
- Same-suit Forge pairs and two/three-card play limits.
- Jack draw, Queen exchange, King extra play, and Ace Crest prestige.
- A final Sudden Score turn for each living player with vitality/hand tie-breaks.
- Seeded random first-player selection.

The current engine instead uses a score-based card-action model with 52 standard cards plus 13 effect definitions, one automatic card-play advance, no vitality field, and a score/hand sudden-score fallback. It also starts with the first configured seat. The maximum-turn fallback is a safety mechanism, not the full final-turn rulebook procedure.

These are outstanding product implementation requirements, not hidden defects that the stabilization tests can prove as complete.

## Merge recommendation

Keep PR #1 open until:
1. The latest branch-head CI run is green.
2. The Unity project imports and compiles in the targeted Unity Editor version.
3. Play Mode matches complete with CPU difficulties Easy/Normal/Hard/Expert and 1–3 CPU seats.
4. The product owner decides whether to implement full Confluence rulebook parity before merge or accept it as a separately tracked milestone.
