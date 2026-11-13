# Tasks

## 1. Filtering engine (`game-filtering`)

- [ ] 1.1 Add the `PlayerCount` (1–12, 12 meaning "12 or more") and `AvailableTime` (30/60/90/120/no limit) value types with validation, and verify unit tests reject out-of-range values
- [ ] 1.2 Implement `GameFilter.Apply` (player range with unknowns kept, 12+ rule, minimum play time falling back to maximum, no-limit) and verify table-driven unit tests cover every `game-filtering` scenario

## 2. Quiz engine (`game-quiz`)

- [ ] 2.1 Implement the classification helpers (`IsCooperative`, `WeightBand` with the 2.0/3.0 boundaries, top-4 category options with alphabetical tie-break) and verify unit tests, including unknown weight and the exact boundary values
- [ ] 2.2 Implement `QuizEngine.Next`: filtering questions first, `emptyCollection`/`noMatch`/single-game short-circuits, the askable-question computation, scoring, early stop, and the next question or results; verify table-driven tests for question order, skipping, the early-stop boundary (lead equal to the remaining questions keeps asking), and results with 1, 2, and 3 picks
- [ ] 2.3 Implement the seeded tie-break shuffle and verify that a fixed seed gives a stable order and that different seeds can change the top pick among tied games

## 3. Quiz API

- [ ] 3.1 Implement `POST /api/quiz/step` (load the user's games with metadata in one query, run the engine, return the question, results, noMatch, or emptyCollection response) with `400` for invalid answers, and verify integration tests for a full quiz run, going back by resubmitting fewer answers, an empty collection, and invalid input
- [ ] 3.2 Verify with an integration test that the quiz only considers the signed-in user's games when two users have different collections

## 4. Pick history (`pick-history`)

- [ ] 4.1 Add the `PickHistory` entity, its `(UserId, PickedAt DESC)` index, and a migration, and verify the migration applies in the integration test database
- [ ] 4.2 Implement `POST /api/history` (ownership check, mode validation, server timestamp) and verify integration tests for accepted, not-owned, and unknown-mode requests
- [ ] 4.3 Implement `GET /api/history?page=` (50 per page, newest first, joined to `Game`, `hasMore`) and verify integration tests for ordering, the 60-pick paging case, privacy between users, and a pick whose game was removed from the collection

## 5. Quiz UI

- [ ] 5.1 Implement `QuizStore` (seed on start, `answer`, `back`, `restart`, `confirm`) and verify unit tests that `back` drops the last answer and `restart` creates a new seed
- [ ] 5.2 Build the `/quiz` page: a question card with option buttons, a player-count picker from 1 to 12+, a remaining-games hint, back and restart controls, and the noMatch and emptyCollection states with links to change answers or add games; verify with component tests
- [ ] 5.3 Build the results view (top pick emphasised, up to two runners-up, "We'll play this" on each, which posts to history and shows a confirmation) and verify component tests for 1-, 2-, and 3-pick results and that restarting without confirming does not post

## 6. History UI

- [ ] 6.1 Build the `/history` page (name, image, mode, date, "Load more", and an empty state linking to the quiz) and verify component tests for paging and the empty state
- [ ] 6.2 Add "Quiz" and "History" to the navigation, guarded for signed-in users, and verify that the route-guard test covers both routes

## 7. Integration check

- [ ] 7.1 Run `dotnet test` and the Angular test suite, and verify both pass
- [ ] 7.2 With `aspire run` and a collection of at least 10 games: take a quiz through to results, go back and change an answer, confirm a runner-up, see it at the top of the history, remove that game from the collection, and verify the history still shows it

## Workflow follow-up

- Archive the change after review, then propose `game-vs-game-mode`, which reuses `GameFilter` and writes picks with mode `"game-vs-game"`.
