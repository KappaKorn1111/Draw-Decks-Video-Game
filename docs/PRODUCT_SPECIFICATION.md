# Draw Decks — Product & System Specification

**Version:** 0.1.0  
**Status:** Product target specification — not a complete description of the currently implemented runtime.  
**Seats:** 1–4 total, humans and CPU opponents combined

> **Implementation notice:** This document describes the intended Confluence game and online product scope. The current shared engine implements a score-based card-action ruleset and local CPU matches, not the complete vitality/combat/Forge/two-card-play Confluence rulebook. LAN/internet lobbies, authentication, chat, reconnect, and authoritative online hosting are also not implemented yet. See [Project Status](PROJECT_STATUS.md) and [Stabilization Audit](STABILIZATION_AUDIT.md) for verified implementation and remaining parity work.

## Product scope
Draw Decks is a turn-based multiplayer strategy card game. The initial release targets Windows, macOS, and Linux desktop. The client architecture must remain portable to Android and iOS. Supported play modes are hot-seat, LAN-hosted, and internet multiplayer with lobbies, chat, reconnect, and authoritative match hosting.

## Seat and opponent policy
- A match contains 2–4 seats for the default competitive ruleset.
- A seat is exactly one human or one CPU controller.
- At least one human seat is required.
- Solo play means one human versus at least one CPU.
- Mixed matches can contain any valid combination, e.g. 1H+1C, 1H+3C, 2H+2C, 3H+1C, or 4H.
- A seat has a stable match-scoped ID, display name, controller type, connection/presence state, hand, score, and status.
- Disconnecting a human does not transfer ownership; reconnection restores the same seat. A configurable timeout policy may offer CPU takeover in a later milestone.

## Default ruleset: Confluence
Confluence is the original default ruleset. It uses a 52-card standard deck plus a ruleset-defined supply of shared tactical tokens. The rules are designed to make drawing, playing, combining, defending, and scoring explicit.

### Card taxonomy
Every card definition has a stable ID, name, category, rank/power, rarity, rules text, targeting schema, and tags.
- **Strike:** offensive card; contests a target's guard.
- **Guard:** defensive card; reduces or cancels incoming strike power.
- **Forge:** combination card; combines with one compatible card to create a higher-value play.
- **Tactic:** utility card; changes draw, hand, turn, or scoring conditions.
- **Crest:** scoring card; earns prestige when played under its condition.

Rarity is content metadata (Common, Uncommon, Rare, Epic, Legendary). Rarity does not randomly alter a card's rules or make a match pay-to-win. The starter ruleset uses a deterministic 52-card playing deck mapped into the five categories by rank bands; later curated card sets may add bespoke definitions.

### Starter deck composition
A standard 52-card deck contains four suits and ranks 2–14. Each rank maps to a category:
- 2–5: Strike
- 6–8: Guard
- 9–10: Forge
- Jack–King (11–13): Tactic
- Ace (14): Crest

All cards are Common in the default starter deck. Suit is a tag used for Forge compatibility and tie-breaks only where explicitly stated. Rank is the card's base power.

### Match setup
1. Validate 2–4 seats and at least one human.
2. Create a match seed and shuffle the 52-card deck with the domain PRNG.
3. Deal five cards to each seat.
4. Place the remaining cards in the draw pile.
5. Set each seat to 20 vitality and zero prestige.
6. Randomly select first player using the match seed. Turn order proceeds clockwise.
7. Each player draws one card at the start of their turn, up to a hand limit of seven. If the draw pile is empty, skip the draw.

### Turn sequence
A turn has four ordered steps:
1. **Draw:** draw one card, if available and below hand limit.
2. **Play:** play up to two cards. A Forge combination consumes two compatible cards as one play. A player may pass the play step.
3. **Resolve:** resolve effects in order; validate targets and costs before changing state.
4. **Cleanup:** discard down to seven cards, then pass priority clockwise.

A player may not play cards they do not hold. A card can be consumed only once. Invalid actions do not mutate match state.

### Core effects
- **Strike:** choose one opponent; strike power is the printed rank minus one. The target may reveal/play one Guard from hand as a reaction. Guard reduces strike power by its printed rank minus five. Remaining power reduces target vitality. A player at zero vitality is eliminated.
- **Guard:** may be played only as a reaction to a Strike. It is discarded after resolution. Guard cannot increase vitality.
- **Forge:** two Forge cards of the same suit combine into a single Forge Burst. Burst power is the sum of both ranks. Deal that much prestige to the player, then discard both cards. This is the primary combination mechanic.
- **Tactic:** rank 11 draws one additional card (respecting hand limit); rank 12 lets the player exchange one card in hand with the top card of the draw pile; rank 13 grants one additional play this turn (maximum three total plays). Tactic cards are discarded after resolution.
- **Crest:** score prestige equal to five plus the number of living opponents. A Crest may be played only during the active player's Play step.

### Scoring and end conditions
- Forge Burst prestige and Crest prestige are added to a player's score.
- Eliminating an opponent grants the eliminating player three prestige.
- A match ends immediately when one player remains alive; that player wins.
- If the draw pile is exhausted and every living player completes two consecutive turns without a Strike, Forge, or Crest being played, the match enters Sudden Score. Players take one final turn in seat order; the player with the highest prestige wins.
- Tied prestige in Sudden Score is broken by higher remaining vitality, then more cards in hand, then shared victory. No arbitrary hidden tie-break.
- If all players are eliminated by simultaneous effects, the match is a draw.

### State machine
Created → Lobby → ReadyCheck → Dealing → ActiveTurn → Resolving → ActiveTurn; terminal branches are Completed, Draw, Cancelled, or Abandoned. Reconnect is an orthogonal connection state and never rewinds game state.

### Configurable ruleset fields
Ruleset definitions are versioned data: seat minimum/maximum, starting hand, hand limit, starting vitality, draw count, plays per turn, card catalog version, target rules, effect parameters, elimination policy, deck exhaustion policy, scoring, turn timeout, and tie-break order. Server validates the ruleset schema and pins its version/hash to each match.

## Non-functional requirements
- Online gameplay is server-authoritative; clients send commands, not trusted state.
- Domain actions are deterministic given state, command, ruleset, and seed.
- Match events are append-only and replayable for diagnostics.
- Accessibility: scalable UI, keyboard/controller navigation, color-independent suit/category symbols, reduced-motion option, screen-reader labels where platform support permits.
- No real-money card packs or gambling mechanics in the foundation release.
