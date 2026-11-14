# Confluence — Draw Decks Default Rulebook

**Version:** 0.1.0

## Objective
Be the last player with vitality remaining. If the draw pile runs out and the table stalls, finish with the highest prestige.

## Seats and setup
2–4 total seats, at least one human. One human may play against one to three CPUs. Shuffle a 52-card deck with the match seed; deal five cards per seat. Each player starts at 20 vitality, zero prestige, and a hand limit of seven. Select first player from the seed; play clockwise.

## Card taxonomy and deck
Four suits, ranks 2–Ace. Rank bands define categories: 2–5 Strike, 6–8 Guard, 9–10 Forge, Jack/Queen/King Tactic, Ace Crest. All base-deck cards are Common. Rarity metadata (Common, Uncommon, Rare, Epic, Legendary) is content metadata, never a hidden power multiplier.

## Turn sequence
1. Draw one card if available and below hand limit.
2. Play up to two cards, or pass. A Forge pair counts as one play; King can raise the limit to three.
3. Resolve effects and reactions.
4. Discard down to seven, then pass clockwise.

## Card effects
- Strike (2–5): choose an opponent. Power = rank − 1. Target may react with one Guard. Remaining damage reduces vitality.
- Guard (6–8): reaction only; block = rank − 5 damage. Discard after reaction.
- Forge (9–10): combine two same-suit Forge cards as one play; gain prestige equal to the sum of their ranks; discard both.
- Jack: draw one extra card, subject to hand limit.
- Queen: exchange one card in hand for the top draw-pile card.
- King: gain one additional play this turn, maximum three.
- Crest Ace: gain prestige equal to five plus the number of living opponents.

## Elimination and scoring
At zero vitality, a player is eliminated and cannot take turns. The player causing elimination gains three prestige. One remaining player wins immediately. Simultaneous elimination of all players is a draw.

## Deck exhaustion
When the draw pile is empty, track consecutive turns without a Strike, Forge, or Crest. When every living player completes two such turns, each living player receives one final turn. Highest prestige wins; ties resolve by vitality, then hand size, then shared victory.

## Legal actions
Cards must be held, played in the correct phase, within the play limit, and target valid opponents. Guards only react to Strikes; Forge requires two same-suit Forge cards. Invalid commands cause no state mutation. Online server is authoritative.

## Configurable rules
Seat bounds, starting hand, hand limit, vitality, draw count, plays per turn, catalog version, effect parameters, elimination, deck exhaustion, scoring, timeouts, and tie-break order are versioned ruleset data pinned to each match by hash.

This is the approved default product baseline. The earlier High Card implementation is only a temporary scaffold and must not be mistaken for complete Confluence rules.
