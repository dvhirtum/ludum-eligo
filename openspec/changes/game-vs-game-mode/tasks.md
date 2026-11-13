# Tasks

Every task below is done test-first: write the named failing tests, make them pass, then refactor. Backend test methods use the `Should_<outcome>_When_<situation>` pattern. Angular tests use plain `describe`/`it` sentences and follow the same red-green-refactor cycle.

## 1. Versus query slice

- [ ] 1.1 `GetPlayableGames` (`GET /api/versus/games`): write failing integration tests (`Should_return_only_playable_own_games_When_filters_are_given`, `Should_not_return_other_users_games_When_two_users_have_collections`, `Should_return_no_match_When_no_game_fits`, `Should_return_empty_collection_When_the_user_has_no_games`, `Should_return_400_When_players_is_out_of_range`, `Should_return_400_When_time_is_not_an_allowed_value`), then implement the query, validator (reusing `PlayerCount`/`AvailableTime`), handler (`LoadSelectableGames` + `GameFilter.Apply`), and endpoint in `Features/Versus` until they pass

## 2. Knockout module (`game-vs-game`, pure TypeScript)

- [ ] 2.1 Write failing tests with a seeded PRNG for `start` and `choose`: n − 1 choices for every n from 1 to 33, an even round with no free pass, an odd round with exactly one free pass, no repeated free pass when another game is available, and the 3-game edge case; then implement the module (`start`, `choose`, `isFinished`, Fisher–Yates pairing, the free-pass rule, injected `rng`) until they pass
- [ ] 2.2 Write failing tests for `progress` (9 games with 2 choices made gives round 1, 7 games still in, 6 choices to go) and for different seeds producing different first-round pairings, then implement until they pass

## 3. Shared filtering UI

- [ ] 3.1 Write failing component tests for `FilteringQuestionsComponent` in `features/filtering` (emits `{ players, time }`, offers 1 to 12+ players and the five time options), then extract the inputs from the quiz page until they pass
- [ ] 3.2 Switch the quiz page to `FilteringQuestionsComponent` and verify the existing quiz component tests pass unchanged

## 4. Game vs game UI

- [ ] 4.1 Write failing `VersusStore` tests (`begin` calls `GET /api/versus/games` and enters the knockout, noMatch and emptyCollection phases, a single game going straight to the winner, undo within a round, undo across a round boundary restoring the same matchup, restart returning to filtering, confirm posting once with mode `"game-vs-game"`), then implement the store until they pass
- [ ] 4.2 Write failing component tests for the `/versus` page (filtering questions shown first, choosing a card advances, Undo disabled at the first matchup, progress text, noMatch and emptyCollection states), then build the page with side-by-side cards stacked on narrow screens until they pass
- [ ] 4.3 Write failing component tests for the winner view ("We'll play this" posts to history and shows the confirmation with "Pick again" and "History" links, restarting without confirming does not post), then build it until they pass
- [ ] 4.4 Extend the route-guard tests with `/versus` (failing first), then add "Game vs game" to the navigation until they pass

## 5. Integration check

- [ ] 5.1 Run `dotnet test` (both test projects) and the Angular test suite, and verify all pass, including the unchanged quiz tests
- [ ] 5.2 With `aspire run` and at least 10 games: filter to an odd number of games, play the knockout, undo once within a round and once across a round boundary, confirm the winner, and verify it appears at the top of the history with mode "game-vs-game"

## Workflow follow-up

- Archive the change after review, once `collection-and-auth` and `quiz-mode` have been archived.
