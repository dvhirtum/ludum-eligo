# Proposal

## Why

Picking a game from a large shelf is slow and often ends in "whatever we played last time". The quiz narrows a user's collection with a few quick questions so the group reaches a choice quickly, and the player-count and time filtering it introduces is reused by the game-vs-game mode. Recording the final pick lets users see what they chose before.

## What Changes

- Add a filtering step shared by both picking modes. It asks how many players there are and how much time is available, then removes games from the user's collection that cannot be played under those conditions.
- Add the quiz.
  - After the filtering step, it asks preference questions (cooperative or competitive, complexity, theme) that re-rank the remaining games rather than removing them.
  - It skips questions that would not tell the remaining games apart, and stops early once the top game can no longer be overtaken.
  - It shows a top pick plus up to two runners-up, and the user confirms which game they will play.
- Keep quiz progress in the browser. Only the confirmed pick is stored on the server.
- Add pick history: every confirmed pick (game, mode, time) is stored per user and shown on a "previous picks" page. In v1 the history is display only and does not influence the quiz.

Out of scope: history influencing picks, deleting history entries, user-defined questions, and the game-vs-game mode (covered by `game-vs-game-mode`).

## Capabilities

### New Capabilities
- `game-filtering`: Player-count and available-time questions, and the rules for which games in a user's collection remain playable under those answers. Shared by the quiz and the game-vs-game mode.
- `game-quiz`: The quiz flow, including the preference questions, scoring and ranking, question skipping, early stopping, results, and confirming a pick.
- `pick-history`: Recording a user's confirmed picks from any picking mode, and listing them.

### Modified Capabilities
<!-- None. This change builds on the game-catalog and game-collection capabilities from collection-and-auth without changing their requirements. -->

## Impact

- Depends on `collection-and-auth`. It uses the catalog metadata (players, play time, weight, mechanics, categories) and the per-user collection.
- API: new quiz endpoint (stateless), pick-history endpoints.
- Database: new `PickHistory` table.
- SPA: new quiz page and history page, and navigation entries for both.
- Documentation: new `functional/quiz.md`, `functional/history.md`, `technical/selection.md`; README, `architecture.md` and `data-model.md` extended.
- `game-vs-game-mode` will reuse the `game-filtering` behaviour and write to `pick-history`.
