# Tasks

Every backend task below is done test-first: write the named failing tests, make them pass, then refactor. Backend test methods use the `Should_<outcome>_When_<situation>` pattern. Angular tasks follow the same red-green-refactor cycle with component and service tests.

## 1. Filtering feature (`game-filtering`)

- [ ] 1.1 `PlayerCount` and `AvailableTime`: write failing unit tests (`Should_reject_the_player_count_When_it_is_below_1`, `Should_reject_the_player_count_When_it_is_above_12`, `Should_treat_12_as_twelve_or_more_When_creating_a_player_count`, `Should_accept_no_limit_When_creating_an_available_time`), then implement the value types in `Features/Filtering` until they pass
- [ ] 1.2 `GameFilter.Apply`: write failing table-driven unit tests for every `game-filtering` scenario (`Should_keep_the_game_When_it_supports_the_player_count`, `Should_remove_the_game_When_it_does_not_support_the_player_count`, `Should_keep_the_game_When_its_player_count_is_unknown`, `Should_keep_the_game_When_its_max_is_12_or_more_and_12_plus_players_play`, `Should_keep_the_game_When_its_minimum_time_fits`, `Should_use_the_maximum_time_When_the_minimum_is_unknown`, `Should_remove_the_game_When_its_minimum_time_is_too_long`, `Should_keep_every_game_When_there_is_no_time_limit`), then implement until they pass
- [ ] 1.3 `SelectableGame` and `LoadSelectableGames`: write a failing integration test (`Should_load_only_the_current_users_games_When_two_users_have_collections`), then implement the record and the `AsNoTracking` projection until it passes
- [ ] 1.4 Write the filtering section of `docs/technical/selection.md` (player and time rules, unknown values, `LoadSelectableGames`), and add the Filtering feature to `docs/technical/architecture.md`; verify it matches the `game-filtering` rules as implemented

## 2. Quiz engine (`game-quiz`, pure)

- [ ] 2.1 `QuizClassification`: write failing unit tests (`Should_be_cooperative_When_mechanics_include_cooperative_game`, `Should_be_light_When_weight_is_below_2`, `Should_be_medium_When_weight_is_exactly_2`, `Should_be_heavy_When_weight_is_exactly_3`, `Should_have_no_band_When_weight_is_unknown`, `Should_offer_the_4_most_common_categories_When_more_exist`, `Should_break_category_ties_alphabetically_When_counts_are_equal`), then implement until they pass
- [ ] 2.2 `SeededShuffle`: write failing unit tests (`Should_return_the_same_order_When_the_seed_is_the_same`, `Should_change_the_order_When_the_seed_differs`), then implement until they pass
- [ ] 2.3 `QuizEngine` flow: write failing table-driven unit tests (`Should_ask_players_then_time_When_the_quiz_starts`, `Should_return_empty_collection_When_the_user_has_no_games`, `Should_return_no_match_When_no_game_passes_the_filters`, `Should_return_results_immediately_When_one_game_remains`, `Should_ask_preferences_in_fixed_order_When_all_are_askable`), then implement until they pass
- [ ] 2.4 `QuizEngine` scoring, skipping and early stop: write failing unit tests (`Should_add_a_point_When_a_game_matches_the_preference`, `Should_add_no_point_When_the_answer_is_no_preference`, `Should_skip_the_coop_question_When_all_remaining_games_are_competitive`, `Should_skip_the_weight_question_When_all_games_share_a_band_or_are_unknown`, `Should_stop_early_When_the_lead_exceeds_the_remaining_questions`, `Should_keep_asking_When_the_lead_equals_the_remaining_questions`, `Should_return_up_to_3_picks_When_the_quiz_ends`, `Should_order_ties_by_seed_When_scores_are_equal`), then implement until they pass
- [ ] 2.5 Add the quiz engine section to `docs/technical/selection.md` (question order, scoring, skipping, early stop, seeded tie-break, classification rules), and add the Quiz feature to `docs/technical/architecture.md`; verify it matches the implemented engine

## 3. Quiz query slice

- [ ] 3.1 `TakeQuizStep` (`GET /api/quiz/step`): write failing integration tests (`Should_return_the_first_question_When_no_answers_are_given`, `Should_return_results_When_all_askable_questions_are_answered`, `Should_return_the_previous_question_When_the_last_answer_is_removed`, `Should_return_empty_collection_When_the_user_has_no_games`, `Should_return_400_When_players_is_out_of_range`, `Should_return_400_When_the_seed_is_missing`, `Should_only_consider_own_games_When_two_users_have_different_collections`), then implement the query, validator, handler, and endpoint until they pass
- [ ] 3.2 Add the `GET /api/quiz/step` contract (parameters, response kinds, `400` cases) to `docs/technical/selection.md`, and verify it matches the endpoint

## 4. History slices (`pick-history`)

- [ ] 4.1 `RecordPick` command (`POST /api/history`): add the `PickHistory` entity, its `(UserId, PickedAt DESC)` index, and a migration. Write failing integration tests (`Should_record_the_pick_When_the_game_is_in_the_collection`, `Should_reject_the_pick_When_the_game_is_not_in_the_collection`, `Should_reject_the_pick_When_the_mode_is_unknown`, `Should_use_the_server_time_When_recording_a_pick`), then implement until they pass
- [ ] 4.2 `GetPickHistory` query (`GET /api/history`): write failing integration tests (`Should_list_picks_newest_first_When_history_is_requested`, `Should_return_50_and_has_more_When_there_are_60_picks`, `Should_return_the_remaining_10_When_requesting_page_2`, `Should_list_only_own_picks_When_two_users_have_history`, `Should_still_show_the_pick_When_the_game_was_removed_from_the_collection`), then implement until they pass
- [ ] 4.3 Add `PickHistory` and its index to `docs/technical/data-model.md`, and the History feature to `docs/technical/architecture.md`; verify both history endpoints are described

## 5. Quiz UI

- [ ] 5.1 `QuizStore`: write failing unit tests (seed created on start, each answer triggers a GET with the answers as query parameters, `back` drops the last answer, `restart` creates a new seed, `confirm` posts once), then implement until they pass
- [ ] 5.2 `/quiz` page: write failing component tests (question card with option buttons, player-count picker from 1 to 12+, remaining-games hint, back and restart, the noMatch state linking to change answers, the emptyCollection state linking to add games), then build the page until they pass
- [ ] 5.3 Results view: write failing component tests (1, 2 and 3 picks, "We'll play this" posting to history with the confirmation shown, restarting without confirming does not post), then build it until they pass
- [ ] 5.4 Write `docs/functional/quiz.md` (the questions, "no preference", results, confirming a pick, back and restart), and verify it matches the quiz page

## 6. History UI

- [ ] 6.1 `/history` page: write failing component tests (name, image, mode and date shown, "Load more" fetching the next page, empty state linking to the quiz), then build the page until they pass
- [ ] 6.2 Navigation: extend the route-guard tests with `/quiz` and `/history` (failing first), then add the "Quiz" and "History" entries until they pass
- [ ] 6.3 Write `docs/functional/history.md` (what is recorded, ordering, paging, display only), and add Quiz and History to the README feature list; verify both match the running app

## 7. Integration check

- [ ] 7.1 Run `dotnet test` (both test projects) and the Angular test suite, and verify all pass
- [ ] 7.2 With `aspire run` and a collection of at least 10 games: take a quiz through to results, go back and change an answer, confirm a runner-up, see it at the top of the history, remove that game from the collection, and verify the history still shows it
- [ ] 7.3 Review the README and `docs/` against the running app: verify the quiz and history pages and endpoints are covered, nothing describes behaviour that does not exist, and the text stays short and limited to what, not why

## Workflow follow-up

- Archive the change after review, then implement `game-vs-game-mode`, which reuses `Features/Filtering` and writes picks with mode `"game-vs-game"`.
