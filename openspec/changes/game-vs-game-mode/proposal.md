# Proposal

## Why

Sometimes a group doesn't know what it wants until it sees two options side by side. A head-to-head knockout lets the user compare games directly instead of answering abstract questions, and it is a playful alternative to the quiz. Asking for player count and time first keeps the bracket short and guarantees the winner can actually be played.

## What Changes

- Add the game-vs-game mode.
  - It starts with the player-count and time questions from the `game-filtering` capability.
  - It then runs a single-elimination knockout over the games that remain. Each round pairs games randomly, and with an odd number one game gets a free pass to the next round.
  - The user picks the preferred game of each pair until one winner is left.
- Show progress through the knockout, and let the user undo their last choice or restart.
- Let the user confirm the winner, which records it in pick history with mode "game-vs-game".
- Add an API endpoint that returns the playable games from the user's collection for a given player count and time, reusing the existing filter.
- Keep knockout progress in the browser only, as in the quiz.

Out of scope: sampling a subset of large pools (a later "sample 16" option), seeding brackets by preference or history, and multi-person voting on each matchup.

## Capabilities

### New Capabilities
- `game-vs-game`: The knockout flow, including the filtering step, random pairing and free passes, choosing between two games, progress, undo and restart, the winner, and confirming it.

### Modified Capabilities
<!-- None. game-filtering and pick-history (from quiz-mode) already cover the filtering rules and the "game-vs-game" mode; this change uses them without changing their requirements. -->

## Impact

- Depends on `collection-and-auth` (collection, catalog) and `quiz-mode` (`GameFilter`, pick history accepting mode "game-vs-game").
- API: new read-only endpoint for playable games.
- SPA: new game-vs-game page and a navigation entry. No database changes.
- Documentation: new `functional/versus.md`; README, `technical/selection.md` and `architecture.md` extended.
