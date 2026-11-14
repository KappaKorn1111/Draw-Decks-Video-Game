# Draw Decks — Local Playable Client

## Launch

1. Open the `client/` folder in Unity 6.3 (6000.3.x).
2. Allow the editor to resolve the local `com.drawdecks.domain` package at `../shared/DrawDecks.Domain`.
3. Allow the editor bootstrap to create `Assets/Scenes/Main.unity` and add it to Build Settings.
4. Open the Main scene and press Play. The runtime constructs its Canvas and EventSystem; no scene prefab setup is required.

The shared C# domain package is imported from the repository's `shared/DrawDecks.Domain` folder. The Unity client does not maintain a fork of the card rules.

## Implemented local experience

- Main menu navigation for Play, Solo Practice, Local Multiplayer, Online Multiplayer, Deck Builder, Collection, Statistics, Settings, Support, and Exit.
- Two-to-four-seat match setup, editable player names, per-seat Human/CPU selection, three target-score presets, two deck presets, and turn timer presets.
- Human versus CPU, human-only, and mixed local seat combinations.
- Automated CPU turns with a deterministic rules-engine command path and simple score-oriented card selection.
- Hot-seat privacy handoff between human players. A player's hand is not rendered until that seat confirms readiness.
- Current hand, opponent hand counts, turn indicator, draw pile, discard pile, scoreboard, active suit, selected target/suit, turn timer, action feedback, and recent event history.
- Draw, play, attack response, defense, pass/end-turn, pause, rematch, match results, and local statistics.
- Runtime button/card selection feedback, page fade transitions, and generated UI sound effects.

## Current rules and deliberate boundaries

- A legal card play advances the turn in the current shared engine. A player may draw once and then play one card or pass; a player may also play immediately.
- The current domain ruleset is score-based. It does not expose a vitality/health mechanic, so the client displays scores rather than inventing health bars.
- Effect stacking is rejected by the current ruleset and is not offered as an enabled option.
- CPU behavior is a basic local opponent policy, not a difficulty-scaled strategy model.
- Online multiplayer is a navigation placeholder. Authentication, internet lobbies, LAN discovery, network-authoritative matches, and chat remain later implementation phases.
- Support is informational only; no payment or donation endpoint is configured.
- Collection and deck selection expose the catalog and the two supported presets. Persistent deck composition editing is not part of this milestone.

## Verification

The .NET CI workflow validates the shared domain and automated local-match seat-composition tests. Unity Editor import, Play Mode, and player builds still require a Unity Editor installation and have to be verified in Unity.
