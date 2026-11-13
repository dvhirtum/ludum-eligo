# Design

## Context

The repository is empty apart from a README, so this change establishes the solution structure that later changes (`quiz-mode`, `game-vs-game-mode`) build on. Motivation and scope: see proposal.md. Behaviour: see the four spec files under `specs/`.

Fixed constraints:
- Angular SPA, .NET 11 API, orchestrated by .NET Aspire.
- Only Google sign-in. The browser never holds tokens.
- BGG XML API2 needs a registered application token (sent as a bearer token) and is rate-limited. Collection requests first answer `202 Accepted` while BGG prepares the data, and must be retried until they return `200`.

## Goals / Non-Goals

**Goals:**
- One `aspire run` starts the database, the API, and the Angular dev server, with the SPA and API reachable on one origin.
- A data model whose `Game` metadata can be queried directly by the later filtering engine (players, time, weight, mechanics).
- A BGG client that every caller goes through, so rate limiting and token handling live in one place.
- A backend layout in which each feature can be added, tested, and changed without touching unrelated features. Later changes add new slices rather than editing shared layers.

**Non-Goals:**
- Running more than one API instance. The background import worker assumes a single instance.
- Production deployment and hosting choice (see Open Questions).
- Offline use, or showing data from BGG beyond the fields listed in the `game-catalog` spec.
- Separate read and write data stores. CQRS here means separate code paths over one database.

## Decisions

### 1. Solution layout

```
src/
  LudumEligo.AppHost/                 Aspire orchestration
  LudumEligo.ServiceDefaults/         OpenTelemetry, health checks, resilience defaults
  LudumEligo.Api/                     ASP.NET Core minimal APIs, organised as vertical slices (see 2)
  ludum-eligo-web/                    Angular SPA (standalone components, signals)
tests/
  LudumEligo.Api.UnitTests/           Validators, BGG parsing, rate limiting, pure logic; no Docker
  LudumEligo.Api.IntegrationTests/    WebApplicationFactory + PostgreSQL Testcontainer
```

The AppHost declares a PostgreSQL resource with a database, the API project (referencing the database), and the Angular app as a JavaScript/npm app resource referencing the API.

- **Why:** this is the standard Aspire shape, and it keeps the backend as one deployable unit.
- **Why two test projects:** unit tests run fast and without Docker, so they suit the TDD loop for logic with no database. Integration tests cover handlers and endpoints against a real database.
- **Alternative considered:** splitting the import worker into its own Worker Service project. Rejected for now because one process is enough and simpler. The worker only dispatches a command (see 7), so it can be moved out later.

### 2. Backend architecture: vertical slices + CQRS

```
src/LudumEligo.Api/
  Features/
    Auth/        SignInWithGoogle (command), SignOut (command), GetCurrentUser (query)
    Catalog/     SearchGames (query), GameCatalog (shared component: ensure/refresh games)
    Collection/  GetCollection (query), AddGame (command), RemoveGame (command)
    Profile/     GetProfile (query), SetBggUsername (command)
    Import/      StartImport (command), GetLatestImport (query), ProcessImport (command), ImportWorker
  Infrastructure/
    Persistence/ AppDbContext, entity configurations, migrations
    Bgg/         BggClient (IBggClient), rate-limiting handler
  Domain/        User, Game, CollectionItem, ImportJob entities
  Common/        ICommandHandler<TCommand,TResult>, IQueryHandler<TQuery,TResult>,
                 ICurrentUser, Result<T>, validation endpoint filter, handler registration
```

**One file per slice.** For example, `Features/Collection/AddGame.cs` contains:
- the `Command` record (or the `Query` record, for a query slice);
- its `Validator`;
- its `Handler`, which implements `ICommandHandler<Command, Result<AddGameResponse>>`;
- a `MapAddGame(this IEndpointRouteBuilder)` extension that registers the route.

`Program.cs` only calls the `Map…` extensions.

**Commands and queries:**
- **Commands** change state through EF Core entities and return a `Result<T>`, which carries either the value or a typed error such as `NotFound`, `AlreadyPresent`, `BggUnavailable`, or `Validation`. The endpoint maps that result to an HTTP response.
- **Queries** read with `AsNoTracking()` and map straight into response records. They never change state.

**No mediator library.** Endpoints receive their handler through dependency injection, for example `(AddGame.Command cmd, AddGame.Handler handler, CancellationToken ct) => …`.
- Handlers are registered by scanning the assembly for the two handler interfaces.
- Cross-cutting concerns run as endpoint filters: validation runs the slice's validator, and logging and timing come from ServiceDefaults and OpenTelemetry.
- **Why:** the dependency chain stays explicit and easy to follow, with no extra framework or licence.
- **Alternatives considered:** MediatR, which now needs a commercial licence, and Wolverine, which is more framework than this application needs.

**SOLID in practice:**
- **Single responsibility:** each handler does one use case.
- **Dependency inversion:** handlers depend on interfaces (`IGameCatalog`, `IBggClient`, `ICurrentUser`, `TimeProvider`), so unit tests can substitute fakes.
- **Open for extension:** new behaviour arrives as new slices rather than edits to shared services.

**Sharing between slices:**
- Logic needed by several slices lives in one well-named component inside the feature that owns it. For example, `IGameCatalog` lives in `Features/Catalog` and is used by `AddGame` and `ProcessImport`.
- Handlers never call other handlers.
- Domain entities and the `AppDbContext` are shared.

### 3. Same origin: dev proxy in development, API serves the SPA in production

- **Development:** the Angular dev server proxies `/api/**` to the API. Its proxy config reads the API URL from the environment variables Aspire injects (`services__api__https__0`).
- **Production:** the API serves the built SPA from `wwwroot`, with a fallback to `index.html` for client-side routes.

Either way the browser only ever talks to one origin, so no CORS configuration is needed and the auth cookie stays first-party.

- **Alternative considered:** a YARP gateway resource in front of both apps. Rejected because it adds a moving part that buys nothing over the dev proxy for a single API.

### 4. Authentication: cookie plus server-side Google OAuth

ASP.NET Core cookie authentication is the default scheme, and the Google handler is the challenge scheme.

**Endpoints** (in `Features/Auth`)

| Endpoint | Slice | Behaviour |
|---|---|---|
| `GET /api/auth/login?returnUrl=...` | — (challenge only) | Starts the challenge. `returnUrl` is validated as a local path. |
| `POST /api/auth/logout` | `SignOut` | Ends the session. |
| `GET /api/auth/me` | `GetCurrentUser` | Returns the display name, or `401`. |

- The Google callback path is set to `/api/auth/signin-google`, so it passes through the dev proxy.
- The API trusts forwarded headers in development, so the redirect URI is built with the SPA's origin.

**On a successful Google sign-in** (`OnTicketReceived`):
- The `SignInWithGoogle` command handler upserts `User` by the Google `sub` claim.
- It returns the internal user id, which is put into the cookie principal.

**`ICurrentUser`** reads that id from the principal for every other slice.

**Cookie settings:** `HttpOnly`, `Secure`, `SameSite=Lax`. For `/api` requests, the cookie handler's redirect-to-login is replaced with a `401` response.

**CSRF:** ASP.NET Core antiforgery runs in Angular's built-in XSRF mode. The API issues an `XSRF-TOKEN` cookie readable by scripts, and Angular's `HttpClient` echoes it in `X-XSRF-TOKEN`. Every non-GET `/api` endpoint validates it.

**Secrets:** the Google client id and secret are Aspire parameters, with the secret marked secret and supplied via user secrets in development.

- **Why:** it meets the "no tokens in the browser" requirement with the least code, and the session cookie stays out of reach of scripts.
- **Alternative considered:** OIDC in the SPA with bearer JWTs. Rejected because it needs token storage and refresh logic in Angular and a larger attack surface.

### 5. Data model (EF Core + Npgsql)

```
User                         Game (shared catalog)          CollectionItem
----                         ---------------------          --------------
Id (uuid, PK)                BggId (int, PK)                UserId  (FK) --+ unique
GoogleSubject (unique)       Name                           BggId   (FK) --+ (UserId, BggId)
DisplayName                  YearPublished?                 AddedAt
Email                        ImageUrl?, ThumbnailUrl?       Source (Search | Import)
BggUsername?                 MinPlayers?, MaxPlayers?
CreatedAt                    MinPlaytime?, MaxPlaytime?     ImportJob
                             Weight? (decimal)              ---------
                             Mechanics (text[])             Id, UserId (FK), Status,
                             Categories (text[])            CreatedAt, StartedAt?, FinishedAt?,
                             RefreshedAt                    Added?, AlreadyPresent?, Error?
```

- Nullable metadata columns implement "unknown rather than zero" from the `game-catalog` spec.
- `Mechanics` and `Categories` are PostgreSQL `text[]` columns rather than join tables.
  - **Why:** `text[]` keeps the model small and still supports the later quiz filters with `ANY`/`&&` and a GIN index.
  - **Alternative considered:** normalized `Mechanic` and `GameMechanic` tables. They would only pay off if mechanics needed their own attributes.
- The unique index on `(UserId, BggId)` enforces "exactly once" at the database level for both manual adds and imports.
- A partial unique index on `ImportJob(UserId) WHERE Status IN ('Queued','Running')` enforces one active import per user.
- Entities and their configurations arrive with the first slice that needs them, each with its own migration, so the schema grows slice by slice.
- Migrations are applied at API startup in development. In production they run as a separate step.

### 6. BGG client and game catalog

`BggClient` (behind `IBggClient`, in `Infrastructure/Bgg`) is a typed `HttpClient` and the only code that calls BGG. It sends the bearer token from an Aspire secret parameter and parses XML with `System.Xml.Linq`. It exposes:

| Method | BGG call |
|---|---|
| `SearchAsync(query)` | `/search?query=...&type=boardgame` |
| `GetThingsAsync(ids)` | `/thing?id=a,b,...&stats=1`, in batches of 20 ids |
| `UserExistsAsync(username)` | `/user?name=...` |
| `GetOwnedCollectionAsync(username)` | `/collection?username=...&own=1&subtype=boardgame&excludesubtype=boardgameexpansion&brief=1` |

**Rate limiting:**
- All calls pass through one shared `RateLimiter` (a token bucket allowing one request every 2 seconds, configurable). It is registered as a singleton DelegatingHandler, so interactive searches and background imports share one budget.
- `429` and `5xx` responses are retried with exponential backoff using the standard resilience handler.
- `202` is not treated as an error. `GetOwnedCollectionAsync` retries it itself, with increasing delays, until it gets `200` or 10 minutes have passed.

**Game catalog:**
- `IGameCatalog.EnsureGamesAsync(ids)` (in `Features/Catalog`) loads existing `Game` rows and fetches only ids that are missing or older than 30 days.
- If a refresh fails and a cached row exists, it keeps the cached row.
- It uses `TimeProvider`, so tests can control the 30-day rule.
- The `AddGame` and `ProcessImport` handlers both depend on it, which implements the shared-cache and refresh requirements.

- **Why one choke point:** it is the simplest way to guarantee the usage limits across the interactive and background paths.

### 7. Import

- **`StartImport`** (command) inserts an `ImportJob` with status `Queued` and returns `202`. If the partial unique index rejects the insert, it returns the active job instead.
- **`ImportWorker`** is a thin `BackgroundService`. It polls the database every few seconds for queued jobs and, for each one, creates a DI scope and runs the `ProcessImport` command handler. All the import logic lives in that handler, so it can be tested directly without running the hosted worker:
  1. Set the job to `Running`.
  2. Fetch the owned collection through `IBggClient`.
  3. Call `IGameCatalog.EnsureGamesAsync` on the ids.
  4. Insert the missing `CollectionItem` rows with `ON CONFLICT DO NOTHING`.
  5. Record the added and already-present counts.
  6. Set the job to `Succeeded`, or to `Failed` with a reason.
- On startup the worker marks any job left `Running` as `Failed` ("interrupted"), so a crash never leaves a user blocked.
- **`GetLatestImport`** (query) returns the most recent job, and the SPA polls it every 3 seconds while the job is active.

- **Why a database-backed queue:** it survives restarts and gives the status query its data for free.
- **Alternative considered:** Hangfire or Quartz. Rejected as unnecessary for one job type on a single instance.

### 8. API surface

All `/api` routes except `/api/auth/*` require authentication. Every handler scopes data by `ICurrentUser`, and an item owned by someone else returns `404`.

| Method | Route | Slice | Kind | Purpose |
|---|---|---|---|---|
| GET | `/api/games/search?q=` | `Catalog/SearchGames` | query | BGG search (min 3 chars) |
| GET | `/api/collection` | `Collection/GetCollection` | query | List own collection |
| POST | `/api/collection` `{ bggId }` | `Collection/AddGame` | command | Add game (`200`, with an `alreadyPresent` flag on duplicates) |
| DELETE | `/api/collection/{bggId}` | `Collection/RemoveGame` | command | Remove game |
| GET | `/api/profile` | `Profile/GetProfile` | query | Read the BGG username |
| PUT | `/api/profile` | `Profile/SetBggUsername` | command | Set or clear the BGG username (validated against BGG) |
| POST | `/api/import` | `Import/StartImport` | command | Start import |
| GET | `/api/import/latest` | `Import/GetLatestImport` | query | Latest import status |

### 9. Angular SPA

- **Pages:** standalone components, signals for state, and lazy routes:
  - `/` shows a landing page with sign-in when anonymous, and redirects to the collection when signed in.
  - `/collection` shows the list with the name filter (client-side).
  - `/collection/add` is BGG search with debounced input.
  - `/settings` holds the BGG username and the import button with status.
- **Folder structure:** the frontend mirrors the backend features (`features/collection`, `features/settings`, …), with a `core/` folder for auth and HTTP plumbing.
- **Auth state:** an `AuthService` loads `/api/auth/me` at startup. A functional route guard protects the signed-in routes, and an HTTP interceptor maps `401` to the signed-out state.
- **Styling:** Angular Material for components, to keep UI work small in this first change.

### 10. Testing: TDD and naming

**TDD:** every slice, shared component, and Angular service or component is built test-first. Write a failing test for the next scenario, make it pass with the simplest code, then refactor. Spec scenarios are the source of test cases.

**Unit tests** (`LudumEligo.Api.UnitTests`) cover code that runs without a database:
- validators;
- `BggClient` parsing and the `202` handling, against recorded XML fixtures through a fake `HttpMessageHandler`;
- the rate-limiting handler;
- pure decision logic such as `GameCatalog`'s staleness rule, with a fake `TimeProvider`.

**Integration tests** (`LudumEligo.Api.IntegrationTests`) cover handlers and endpoints that touch the database:
- against a PostgreSQL Testcontainer, never the EF in-memory provider, because `ON CONFLICT`, `text[]`, and partial indexes must behave as in production;
- `GameCatalog` and `ProcessImport` are exercised directly from a DI scope;
- endpoints are exercised through `WebApplicationFactory`, with a test authentication handler replacing Google so tests act as specific users;
- `IBggClient` is replaced by a fixture-backed fake.

**Layout:** both projects mirror `Features/`, for example `Features/Collection/AddGameTests.cs`.

**Naming:** backend test methods use Snake_Case with the Should_When pattern, `Should_<outcome>_When_<situation>`. For example:
- `Should_return_already_present_When_adding_a_game_twice`
- `Should_keep_cached_metadata_When_the_refresh_fails`

**Angular:** component and service tests (guard, interceptor, pages, import status polling) are also written first.

## Risks / Trade-offs

- [BGG application token approval can take time] → Request the token at the start of implementation. Until it arrives, develop against the recorded fixtures.
- [BGG XML format or limits change] → All parsing lives in `BggClient` behind fixture-based tests, and the rate limit interval is configurable.
- [The shared rate limiter makes searches wait while a large import runs] → Accepted for v1. If it becomes noticeable, give interactive search priority through a separate small budget.
- [The Google redirect URI must be exact, and Aspire assigns dynamic ports] → Pin the Angular dev server port in the AppHost and register `https://localhost:<port>/api/auth/signin-google` in the Google console. Document this in the README.
- [The in-process worker does not scale out] → A single instance is a stated non-goal. Moving the worker to a separate project, with row locking (`FOR UPDATE SKIP LOCKED`), is the upgrade path, and `ProcessImport` moves unchanged.
- [Re-importing re-adds games the user removed locally] → This is intended behaviour per the spec. Revisit with an "ignored" list if users find it annoying.
- [Shared components such as `GameCatalog` can grow into a hidden service layer] → Keep each shared component to one responsibility, owned by one feature. Extract a new one rather than adding unrelated methods.
- [Slices duplicate small bits of mapping or query code] → Accepted as the price of independent slices. Extract only when the same logic appears in three or more slices.

## Migration Plan

This is a greenfield change with no existing data or deployment, so nothing needs migrating. Rollback means reverting the change.

## Open Questions

- Production hosting target (for example Azure Container Apps through `aspire deploy`, or a self-hosted container). This does not affect the specs or the tasks in this change.
