# Prompt 05 Runtime — Health, Team Battle, and Survival

## Health and damage

- Every player state has health initialized from `Ruleset.StartingHealth`.
- The catalog includes a Strike attack card. It enters a pending-attack phase and applies `Ruleset.DamagePerHit` when accepted.
- Shield and Negate cancel the incoming damage; Reflect applies the incoming effect back to the attacker.
- Health reaching zero marks the target eliminated, removes queued skip effects, emits damage/elimination events, and invokes mode-specific victory evaluation.
- Damage cards are legal only in Elimination, Team Battle, and Survival.
- Elimination mode ignores Classic target-score victories: the last living player wins. At the engine's turn/deck safety boundary, a health/score/hand tiebreak resolves the match deterministically.

## Team Battle

- Match creation assigns seats 1 and 3 to `team-a`, seats 2 and 4 to `team-b` unless explicit team IDs are provided.
- Match creation rejects any layout other than four seats split into two teams of two.
- Standard-card points are credited to both the individual player score and their team's shared score.
- A team wins by reaching the configured target score or by eliminating every member of the opposing team.
- Team Battle sudden-score fallback compares team score, living team health, and remaining hand counts; exact ties draw.
- Targeted card interactions and direct elimination reject friendly targets. This covers Strike, Skip, Swap Hands, Steal, and other target-based effects.
- Match state exposes immutable team-score snapshots and each player's team ID.

## Survival

- Survival requires exactly one human survivor and one or more CPU opponents.
- Each CPU has health. Defeating all CPUs clears the current wave, awards survival points, and respawns the CPU seats.
- CPU health increases by two for each subsequent wave.
- The local Unity client raises CPU difficulty every two waves and shortens thinking delay as the run progresses.
- A configured maximum wave count completes the run; zero means an endless run until the human survivor is defeated.
- Survival score includes points from scoring cards and the configured reward for each CPU defeated.
- `SurvivalLeaderboardEntry` is a stable, privacy-safe result projection (match ID, player ID, score, waves, seed, catalog/rules version). `SurvivalLeaderboard.Rank` provides deterministic score ordering.
- The Unity client stores a local personal-best survival score in PlayerPrefs.

## Unity client integration

- Match setup now includes a mode selector for Classic, Elimination, Team Battle, Survival, and Custom.
- Team Battle selects four seats and configures two human teammates against two CPU teammates by default.
- Survival defaults to one human and three CPU opponents.
- Match HUD displays health in health-based modes, team identity and shared scores in Team Battle, and wave/score progression in Survival.
- Target selection excludes teammates in Team Battle.
- Results display team winners, team totals, survival score, waves cleared, and local personal best.

## Verification boundaries

The shared .NET domain is covered by automated tests and GitHub Actions. The repository's hosted workflow does not install Unity Editor, so Unity script compilation, Play Mode, mobile input behavior, and desktop/mobile player builds still require a licensed Unity workstation or runner. Online matchmaking and remote leaderboard submission are not implemented; the survival result contract is ready for a future server endpoint.
