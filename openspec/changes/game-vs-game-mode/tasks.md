# Tasks

## 1. Playable games API

- [ ] 1.1 Implement `GET /api/collection/playable?players=&time=` (reusing the `PlayerCount`/`AvailableTime` validation and `GameFilter.Apply`, returning games, noMatch, or emptyCollection) and verify integration tests for the filtered result, only the caller's games, invalid values returning `400`, an empty collection, and no match

## 2. Knockout module (`game-vs-game`)

- [ ] 2.1 Implement the pure TypeScript knockout module (`start`, `choose`, `isFinished`, `progress`) with an injected `rng`, Fisher–Yates pairing, and the free-pass rule, and verify unit tests with a seeded PRNG for: n − 1 choices for n from 1 to 33, even and odd rounds, no repeated free pass when another game is available, and the 3-game edge case
- [ ] 2.2 Verify with unit tests that `progress` matches the spec example (9 games, 2 choices made: round 1, 7 games still in, 6 choices to go) and that different seeds produce different first-round pairings

## 3. Shared filtering UI

- [ ] 3.1 Extract the player-count and time inputs from the quiz page into `FilteringQuestionsComponent`, emitting `{ players, time }`, use it in the quiz, and verify the existing quiz component tests still pass unchanged

## 4. Game vs game UI

- [ ] 4.1 Implement `VersusStore` (phases, game lookup, knockout state, undo stack, and the actions `begin`/`choose`/`undo`/`restart`/`confirm`) and verify unit tests for: undo within a round, undo across a round boundary restoring the same matchup, restart returning to filtering, a single game going straight to the winner, and confirm posting once with mode `"game-vs-game"`
- [ ] 4.2 Build the `/versus` page: filtering questions, two side-by-side clickable game cards (stacked on narrow screens), the progress bar, Undo and Restart, and the noMatch and emptyCollection states; verify with component tests that choosing a card advances and Undo is disabled at the first matchup
- [ ] 4.3 Build the winner view ("We'll play this" posts to history and shows the confirmation with "Pick again" and "History" links) and verify with a component test that restarting without confirming does not post
- [ ] 4.4 Add "Game vs game" to the navigation, guarded for signed-in users, and verify the route-guard test covers `/versus`

## 5. Integration check

- [ ] 5.1 Run `dotnet test` and the Angular test suite and verify both pass, including the quiz regression tests
- [ ] 5.2 With `aspire run` and at least 10 games: filter to an odd number of games, play the knockout, undo once within a round and once across a round boundary, confirm the winner, and verify it appears at the top of the history with mode "game-vs-game"

## Workflow follow-up

- Archive the change after review, once `collection-and-auth` and `quiz-mode` have been archived.
