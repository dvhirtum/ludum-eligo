# Tasks

## 1. Solution scaffolding

- [ ] 1.1 Create `LudumEligo.AppHost`, `LudumEligo.ServiceDefaults`, and `LudumEligo.Api` (.NET 11) plus a solution file under `src/`, and verify `dotnet build` succeeds
- [ ] 1.2 Create the Angular app `src/ludum-eligo-web` (standalone, routing, Angular Material) and verify `npm run build` succeeds
- [ ] 1.3 Add `tests/LudumEligo.Api.Tests` (xUnit, `WebApplicationFactory`, Testcontainers PostgreSQL) with one smoke test hitting the health endpoint, and verify `dotnet test` passes
- [ ] 1.4 Wire the AppHost: PostgreSQL resource and database, the API referencing it, and the Angular app as an npm resource referencing the API with a pinned port; verify `aspire run` shows all resources healthy in the dashboard
- [ ] 1.5 Add the Angular dev proxy for `/api/**` using the Aspire-injected API URL, and verify a request from the SPA to `/api/health` succeeds through the proxy
- [ ] 1.6 Configure the API to serve the built SPA from `wwwroot` with an `index.html` fallback for production builds, and verify a deep link such as `/collection` returns the SPA from a published build
- [ ] 1.7 Document prerequisites and `aspire run` in README.md, and verify the steps work on a clean clone

## 2. Data model

- [ ] 2.1 Add EF Core with Npgsql and the `User`, `Game`, `CollectionItem`, and `ImportJob` entities as in design.md, including nullable metadata, `text[]` columns, the unique `(UserId, BggId)` index, and the partial unique active-import index
- [ ] 2.2 Create the initial migration, apply it at startup in development, and verify with an integration test that the schema is created and a duplicate `(UserId, BggId)` insert is rejected

## 3. Authentication (`user-auth`)

- [ ] 3.1 Add Aspire parameters for the Google client id and secret (secret via user secrets), and document Google console setup, including the redirect URI `https://localhost:<port>/api/auth/signin-google`, in README.md
- [ ] 3.2 Configure cookie authentication (HttpOnly, Secure, SameSite=Lax, `401` instead of redirect for `/api`) with Google as the challenge scheme and callback path `/api/auth/signin-google`, plus forwarded headers in development
- [ ] 3.3 Implement user upsert by Google `sub` on ticket received (create on first sign-in, update the display name on later sign-ins), and verify with tests for first, returning, and renamed sign-ins
- [ ] 3.4 Implement `GET /api/auth/login` (local `returnUrl` only), `POST /api/auth/logout`, and `GET /api/auth/me`, and verify with integration tests using a test auth handler, including rejection of a non-local `returnUrl`
- [ ] 3.5 Add antiforgery in Angular XSRF mode, validated on all non-GET `/api` endpoints, and verify a POST without the `X-XSRF-TOKEN` header is rejected
- [ ] 3.6 Require authentication on all non-auth `/api` routes, and verify with a test that anonymous requests get `401`
- [ ] 3.7 Build the Angular `AuthService` (load `/api/auth/me`), route guard, `401` interceptor, landing page with "Sign in with Google", and sign-out in the toolbar, and verify with unit tests for the guard and interceptor
- [ ] 3.8 Verify manually with `aspire run` that Google sign-in, page reload, and sign-out work end to end, and that the session cookie is HttpOnly in browser devtools

## 4. BGG client and catalog (`game-catalog`)

- [ ] 4.1 Add the BGG token Aspire parameter and the typed `BggClient` with the bearer token, shared token-bucket rate limiter (configurable interval), and resilience retries for `429`/`5xx`
- [ ] 4.2 Implement `SearchAsync` and `GetThingsAsync` (batches of 20, `stats=1`, unknown values mapped to null), and verify with fixture-based tests, including a game without weight
- [ ] 4.3 Implement `UserExistsAsync` and `GetOwnedCollectionAsync` (owned, expansions excluded, `202` retried with increasing delays up to 10 minutes), and verify with fixture tests for `202`-then-`200`, timeout, and private-collection error responses
- [ ] 4.4 Implement `CatalogService.EnsureGamesAsync` (reuse cached rows, fetch missing ones, refresh rows older than 30 days, keep cached rows when a refresh fails), and verify with tests for each case
- [ ] 4.5 Implement `GET /api/games/search?q=` (minimum 3 characters, BGG-unavailable error), and verify with integration tests
- [ ] 4.6 Add a test that the rate limiter spaces consecutive BGG requests by the configured interval

## 5. Collection management (`game-collection`)

- [ ] 5.1 Implement `GET /api/collection` (own items, sorted by name, with name, image, players, and play time), and verify with an integration test that two users see only their own games
- [ ] 5.2 Implement `POST /api/collection` (via `EnsureGamesAsync`, `alreadyPresent` flag on duplicates, error when BGG is unavailable for an uncached game), and verify with integration tests for each scenario
- [ ] 5.3 Implement `DELETE /api/collection/{bggId}` (scoped to the user, `404` for others' items, catalog untouched), and verify with integration tests, including that another user's copy remains
- [ ] 5.4 Build the `/collection` page (list, client-side name filter ignoring case, empty state with search and import actions, remove), and verify with component tests for filtering and the empty state
- [ ] 5.5 Build the `/collection/add` page (debounced BGG search, results with year, add button, "already in collection" and "BGG unavailable" messages), and verify with component tests

## 6. BGG import (`bgg-collection-import`)

- [ ] 6.1 Implement `GET/PUT /api/profile` (set, change, or clear the BGG username, validated with `UserExistsAsync`), and verify with integration tests for valid, unknown, and cleared usernames
- [ ] 6.2 Implement `POST /api/import` (rejected without a username, returns the active job if one exists) and `GET /api/import/latest`, and verify with integration tests
- [ ] 6.3 Implement `ImportWorker` (pick queued jobs, fetch the owned collection, ensure catalog games, insert missing items with `ON CONFLICT DO NOTHING`, record counts, failure reasons, and mark interrupted `Running` jobs as failed on startup), and verify with tests for: new games added, existing games untouched, local-only games kept, removed game re-added, and failure handling
- [ ] 6.4 Build the `/settings` page (BGG username form, import button, status with added and already-present counts, polling every 3 seconds while active), and verify with component tests for the polling and failed state
- [ ] 6.5 Verify manually with `aspire run` and a real BGG account that an import completes and the collection shows the owned base games

## 7. Integration check

- [ ] 7.1 Run `dotnet test` and the Angular test suite and verify both pass
- [ ] 7.2 Run the full flow with `aspire run`: sign in, search and add a game, link a BGG username, import, filter, remove a game, sign out; verify each step and that the Aspire dashboard shows no errors

## Workflow follow-up

- Archive the change after review, then start the `quiz-mode` change.
