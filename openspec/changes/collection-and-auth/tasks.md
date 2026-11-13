# Tasks

Every backend task below is done test-first: write the named failing tests, make them pass, then refactor. Backend test methods use the `Should_<outcome>_When_<situation>` pattern. Angular tasks follow the same red-green-refactor cycle with component and service tests.

## 1. Solution scaffolding

- [x] 1.1 Create `LudumEligo.AppHost`, `LudumEligo.ServiceDefaults`, and `LudumEligo.Api` (.NET 11, C# 15) plus a solution file under `src/`, with empty `Features/`, `Infrastructure/`, `Domain/`, and `Common/` folders, and verify `dotnet build` succeeds
- [x] 1.2 Create `tests/LudumEligo.Api.UnitTests` (xUnit) and `tests/LudumEligo.Api.IntegrationTests` (xUnit, `WebApplicationFactory`, PostgreSQL Testcontainer fixture), each with a `Features/` folder, and verify `dotnet test` runs both projects (with no tests yet)
- [x] 1.3 Create the Angular app `src/ludum-eligo-web` (standalone, routing, Angular Material, `core/` and `features/` folders), and verify `npm run build` and `npm test` succeed
- [x] 1.4 Wire the AppHost (PostgreSQL resource and database, the API referencing it, and the Angular app as an npm resource referencing the API with a pinned port) and verify `aspire run` shows all resources healthy in the dashboard
- [x] 1.5 Add the Angular dev proxy for `/api/**` using the Aspire-injected API URL, and verify a request from the SPA to `/api/health` succeeds through the proxy
- [x] 1.6 Write a failing integration test `Should_serve_the_spa_When_requesting_a_client_route`, then configure the API to serve the built SPA from `wwwroot` with an `index.html` fallback until it passes
- [x] 1.7 Document prerequisites and `aspire run` in README.md (with links into `docs/`), create `docs/functional/` and `docs/technical/`, and write `docs/technical/architecture.md` with the solution layout; verify the README steps work on a clean clone

## 2. Shared building blocks and persistence

- [x] 2.1 Write failing unit tests for `Result<T>` (success, typed errors) and the validation endpoint filter (`Should_return_400_with_errors_When_the_validator_fails`), then implement `Common/` (`ICommandHandler<,>`, `IQueryHandler<,>`, `Result<T>`, the validation filter, and assembly-scanning handler registration) until they pass
- [x] 2.2 Write a failing integration test `Should_resolve_every_handler_When_the_application_starts`, then wire handler registration in `Program.cs` until it passes
- [x] 2.3 Add `AppDbContext` in `Infrastructure/Persistence` with the `User` and `Game` entities (nullable metadata, `text[]` columns) and the initial migration applied at startup in development, and verify with an integration test `Should_store_unknown_weight_as_null_When_saving_a_game_without_weight`
- [x] 2.4 Document the slice conventions, CQRS, and handler registration in `docs/technical/architecture.md`. Create `docs/technical/data-model.md` (with `User` and `Game`) and `docs/technical/testing.md` (TDD, the two test projects, Should_When naming, how to run the tests). Verify that `dotnet test` runs as `testing.md` describes

## 3. Auth feature (`user-auth`)

- [x] 3.1 Add Aspire parameters for the Google client id and secret (secret via user secrets), and document Google console setup, including the redirect URI `https://localhost:<port>/api/auth/signin-google`, in README.md and `docs/technical/authentication.md`
- [x] 3.2 Configure cookie authentication (HttpOnly, Secure, SameSite=Lax, `401` instead of redirect for `/api`), the Google challenge scheme with callback path `/api/auth/signin-google`, forwarded headers in development, and a test authentication handler in the integration test host; verify with `Should_return_401_When_an_anonymous_user_calls_a_protected_endpoint`
- [x] 3.3 `SignInWithGoogle` command: write failing integration tests (`Should_create_a_user_When_signing_in_for_the_first_time`, `Should_reuse_the_user_When_signing_in_again`, `Should_update_the_display_name_When_it_changed_at_google`), then implement the command, its handler, and the `OnTicketReceived` hook putting the user id into the principal until they pass
- [x] 3.4 Write failing unit tests for `ICurrentUser` (`Should_return_the_user_id_When_the_principal_has_one`), then implement it until they pass
- [x] 3.5 `GetCurrentUser` query and `GET /api/auth/me`: write failing integration tests (`Should_return_the_display_name_When_signed_in`, `Should_return_401_When_not_signed_in`), then implement until they pass
- [x] 3.6 Login and `SignOut`: write failing integration tests (`Should_reject_the_return_url_When_it_is_not_local`, `Should_end_the_session_When_signing_out`), then implement `GET /api/auth/login` and the `SignOut` command with `POST /api/auth/logout` until they pass
- [x] 3.7 Write a failing integration test `Should_reject_the_request_When_a_post_has_no_xsrf_token`, then add antiforgery in Angular XSRF mode, validated on all non-GET `/api` endpoints, until it passes
- [x] 3.8 Angular auth: write failing tests for the route guard, the `401` interceptor, and `AuthService`, then build them, the landing page with "Sign in with Google", and sign-out in the toolbar until they pass
- [x] 3.9 Verify manually with `aspire run` that Google sign-in, page reload, and sign-out work end to end, and that the session cookie is HttpOnly in browser devtools
- [x] 3.10 Complete `docs/technical/authentication.md` (cookie and Google flow, antiforgery, `ICurrentUser`), and write `docs/functional/sign-in.md`; verify that every auth endpoint and the sign-in and sign-out behaviour are described

## 4. BGG client (`Infrastructure/Bgg`)

- [ ] 4.1 Write failing unit tests for the rate-limiting handler (`Should_space_requests_by_the_configured_interval_When_sending_several_requests`), then implement it, plus the BGG token Aspire parameter and the typed `BggClient` registration with the bearer token and resilience retries for `429`/`5xx`, until they pass
- [ ] 4.2 Write failing fixture-based unit tests for `SearchAsync` and `GetThingsAsync` (`Should_map_missing_weight_to_null_When_the_game_is_unrated`, `Should_request_in_batches_of_20_When_fetching_many_games`), then implement them until they pass
- [ ] 4.3 Write failing fixture-based unit tests for `UserExistsAsync` and `GetOwnedCollectionAsync` (`Should_retry_When_bgg_answers_202`, `Should_fail_When_the_collection_is_not_ready_within_10_minutes`, `Should_report_private_collection_When_bgg_returns_an_error`), then implement them until they pass
- [ ] 4.4 Write `docs/technical/bgg-integration.md` (`BggClient`, rate limiting, `202` handling), and add the BGG token to the README's secrets section; verify the README secret setup works on a clean clone

## 5. Catalog feature (`game-catalog`)

- [ ] 5.1 `GameCatalog`: write a failing unit test for the staleness rule (`Should_be_stale_When_refreshed_more_than_30_days_ago`) and failing integration tests (`Should_use_the_cached_game_When_it_is_fresh`, `Should_fetch_the_game_When_it_is_not_cached`, `Should_refresh_the_game_When_it_is_stale`, `Should_keep_cached_metadata_When_the_refresh_fails`), then implement `IGameCatalog` until they pass
- [ ] 5.2 `SearchGames` query and `GET /api/games/search`: write failing tests (`Should_return_matches_When_searching_by_name`, `Should_return_400_When_the_term_is_shorter_than_3_characters`, `Should_return_bgg_unavailable_When_bgg_cannot_be_reached`), then implement the query, validator, handler, and endpoint until they pass
- [ ] 5.3 Add `GameCatalog` caching and the 30-day refresh to `docs/technical/bgg-integration.md`, and verify it matches the implemented behaviour

## 6. Collection feature (`game-collection`)

- [ ] 6.1 Add the `CollectionItem` entity, the unique `(UserId, BggId)` index, and a migration, and verify with `Should_reject_a_duplicate_When_inserting_the_same_game_twice_for_a_user`
- [ ] 6.2 `GetCollection` query: write failing integration tests (`Should_list_only_own_games_sorted_by_name_When_two_users_have_collections`, `Should_return_an_empty_list_When_the_collection_is_empty`), then implement until they pass
- [ ] 6.3 `AddGame` command: write failing integration tests (`Should_add_the_game_When_it_is_not_in_the_collection`, `Should_return_already_present_When_adding_a_game_twice`, `Should_fail_with_bgg_unavailable_When_an_uncached_game_cannot_be_fetched`), then implement the command, validator, handler, and endpoint until they pass
- [ ] 6.4 `RemoveGame` command: write failing integration tests (`Should_remove_the_game_When_it_is_in_the_collection`, `Should_return_404_When_the_game_belongs_to_another_user`, `Should_keep_another_users_copy_When_removing_a_shared_game`), then implement until they pass
- [ ] 6.5 Angular `/collection` page: write failing component tests (filtering ignoring case, empty state with search and import actions, remove), then build the page until they pass
- [ ] 6.6 Angular `/collection/add` page: write failing component tests (debounced search, results with year, "already in collection" and "BGG unavailable" messages), then build the page until they pass
- [ ] 6.7 Write `docs/functional/collection.md` (viewing, filtering, adding through BGG search, removing), and add `CollectionItem` to `docs/technical/data-model.md`; verify that every collection page and endpoint is described

## 7. Profile and import features (`bgg-collection-import`)

- [ ] 7.1 Add `BggUsername` to `User` with a migration. `GetProfile` query and `SetBggUsername` command: write failing integration tests (`Should_save_the_username_When_bgg_knows_the_user`, `Should_reject_the_username_When_bgg_does_not_know_the_user`, `Should_clear_the_username_When_an_empty_value_is_saved`), then implement both slices and `GET/PUT /api/profile` until they pass
- [ ] 7.2 Add the `ImportJob` entity, the partial unique active-import index, and a migration. `StartImport` command and `GetLatestImport` query: write failing integration tests (`Should_queue_an_import_When_a_username_is_linked`, `Should_reject_the_import_When_no_username_is_linked`, `Should_return_the_active_import_When_one_is_already_running`, `Should_return_the_latest_status_When_requested`), then implement until they pass
- [ ] 7.3 `ProcessImport` command: write failing integration tests (`Should_add_missing_games_When_importing`, `Should_leave_existing_games_untouched_When_importing`, `Should_keep_local_only_games_When_importing`, `Should_add_a_removed_game_again_When_it_is_still_owned_on_bgg`, `Should_mark_the_import_failed_When_bgg_does_not_return_the_collection`, `Should_record_added_and_already_present_counts_When_the_import_succeeds`), then implement the handler until they pass
- [ ] 7.4 `ImportWorker`: write failing integration tests (`Should_process_a_queued_import_When_the_worker_runs`, `Should_mark_running_imports_failed_When_the_worker_starts`), then implement the thin `BackgroundService` dispatching `ProcessImport` until they pass
- [ ] 7.5 Angular `/settings` page: write failing component tests (username form, import button, polling every 3 seconds while active, failed state), then build the page until they pass
- [ ] 7.6 Verify manually with `aspire run` and a real BGG account that an import completes and the collection shows the owned base games
- [ ] 7.7 Write `docs/functional/bgg-import.md` (linking a username, starting an import, statuses and counts), and add `BggUsername`, `ImportJob`, and the active-import index to `docs/technical/data-model.md`; verify that every profile and import endpoint is described

## 8. Integration check

- [ ] 8.1 Run `dotnet test` (both test projects) and the Angular test suite, and verify all pass
- [ ] 8.2 Run the full flow with `aspire run`: sign in, search and add a game, link a BGG username, import, filter, remove a game, sign out; verify each step and that the Aspire dashboard shows no errors
- [ ] 8.3 Review the README and `docs/` against the running app: verify every page and endpoint is covered, nothing describes behaviour that does not exist, and the text stays short and limited to what, not why

## Workflow follow-up

- Archive the change after review, then start the `quiz-mode` change.
