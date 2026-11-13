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

**Non-Goals:**
- Running more than one API instance. The background import worker assumes a single instance.
- Production deployment and hosting choice (see Open Questions).
- Offline use, or showing data from BGG beyond the fields listed in the `game-catalog` spec.

## Decisions

### 1. Solution layout

```
src/
  LudumEligo.AppHost/          Aspire orchestration
  LudumEligo.ServiceDefaults/  OpenTelemetry, health checks, resilience defaults
  LudumEligo.Api/              ASP.NET Core minimal APIs, EF Core, BGG client, import worker
  ludum-eligo-web/             Angular SPA (standalone components, signals)
tests/
  LudumEligo.Api.Tests/        Unit and integration tests
```

The AppHost declares a PostgreSQL resource with a database, the API project (referencing the database), and the Angular app as a JavaScript/npm app resource referencing the API.

- **Why:** this is the standard Aspire shape, and it keeps the backend as one deployable unit.
- **Alternative considered:** splitting the import worker into its own Worker Service project. Rejected for now because one process is enough and simpler. The worker is isolated behind a class, so it can be moved out later.

### 2. Same origin: dev proxy in development, API serves the SPA in production

- **Development:** the Angular dev server proxies `/api/**` to the API. Its proxy config reads the API URL from the environment variables Aspire injects (`services__api__https__0`).
- **Production:** the API serves the built SPA from `wwwroot`, with a fallback to `index.html` for client-side routes.

Either way the browser only ever talks to one origin, so no CORS configuration is needed and the auth cookie stays first-party.

- **Alternative considered:** a YARP gateway resource in front of both apps. Rejected because it adds a moving part that buys nothing over the dev proxy for a single API.

### 3. Authentication: cookie plus server-side Google OAuth

ASP.NET Core cookie authentication is the default scheme, and the Google handler is the challenge scheme.

**Endpoints**

| Endpoint | Behaviour |
|---|---|
| `GET /api/auth/login?returnUrl=...` | Starts the challenge. `returnUrl` is validated as a local path. |
| `POST /api/auth/logout` | Ends the session. |
| `GET /api/auth/me` | Returns the display name, or `401`. |

- The Google callback path is set to `/api/auth/signin-google`, so it passes through the dev proxy.
- The API trusts forwarded headers in development, so the redirect URI is built with the SPA's origin.

**On a successful Google sign-in** (`OnTicketReceived`):
- The API upserts `User` by the Google `sub` claim.
- It puts the internal user id into the cookie principal.

**Cookie settings:** `HttpOnly`, `Secure`, `SameSite=Lax`. For `/api` requests, the cookie handler's redirect-to-login is replaced with a `401` response.

**CSRF:** ASP.NET Core antiforgery runs in Angular's built-in XSRF mode. The API issues an `XSRF-TOKEN` cookie readable by scripts, and Angular's `HttpClient` echoes it in `X-XSRF-TOKEN`. Every non-GET `/api` endpoint validates it.

**Secrets:** the Google client id and secret are Aspire parameters, with the secret marked secret and supplied via user secrets in development.

- **Why:** it meets the "no tokens in the browser" requirement with the least code, and the session cookie stays out of reach of scripts.
- **Alternative considered:** OIDC in the SPA with bearer JWTs. Rejected because it needs token storage and refresh logic in Angular and a larger attack surface.

### 4. Data model (EF Core + Npgsql)

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
- Migrations are applied at API startup in development. In production they run as a separate step.

### 5. BGG client

A typed `HttpClient` (`BggClient`) is the only code that calls BGG. It sends the bearer token from an Aspire secret parameter and parses XML with `System.Xml.Linq`. It exposes:

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

**Catalog service:**
- `CatalogService.EnsureGamesAsync(ids)` sits on top of the client. It loads existing `Game` rows and fetches only ids that are missing or older than 30 days.
- If a refresh fails and a cached row exists, it keeps the cached row.
- Both the add-game and import flows go through it, which implements the shared-cache and refresh requirements.

- **Why one choke point:** it is the simplest way to guarantee the usage limits across the interactive and background paths.

### 6. Import worker

- `POST /api/import` inserts an `ImportJob` with status `Queued` and returns `202`. If the partial unique index rejects the insert, it returns the active job instead.
- An `ImportWorker` (a `BackgroundService`) polls the database every few seconds for queued jobs and processes them one at a time:
  1. Set the job to `Running`.
  2. Fetch the owned collection.
  3. Call `EnsureGamesAsync` on the ids.
  4. Insert the missing `CollectionItem` rows with `ON CONFLICT DO NOTHING`.
  5. Record the added and already-present counts.
  6. Set the job to `Succeeded`, or to `Failed` with a reason.
- On startup the worker marks any job left `Running` as `Failed` ("interrupted"), so a crash never leaves a user blocked.
- `GET /api/import/latest` returns the most recent job, and the SPA polls it every 3 seconds while the job is active.

- **Why a database-backed queue:** it survives restarts and gives the status endpoint its data for free.
- **Alternative considered:** Hangfire or Quartz. Rejected as unnecessary for one job type on a single instance.

### 7. API surface

All `/api` routes except `/api/auth/*` require authentication. Collection routes always scope queries by the signed-in user's id, and an item owned by someone else returns `404`.

| Method | Route | Purpose |
|---|---|---|
| GET | `/api/games/search?q=` | BGG search (min 3 chars) |
| GET | `/api/collection` | List own collection |
| POST | `/api/collection` `{ bggId }` | Add game (`200` if added, `200` with `alreadyPresent` flag if duplicate) |
| DELETE | `/api/collection/{bggId}` | Remove game |
| GET/PUT | `/api/profile` | Read, set, or clear the BGG username (validated against BGG) |
| POST | `/api/import` | Start import |
| GET | `/api/import/latest` | Latest import status |

### 8. Angular SPA

- **Pages:** standalone components, signals for state, and lazy routes:
  - `/` shows a landing page with sign-in when anonymous, and redirects to the collection when signed in.
  - `/collection` shows the list with the name filter (client-side).
  - `/collection/add` is BGG search with debounced input.
  - `/settings` holds the BGG username and the import button with status.
- **Auth state:** an `AuthService` loads `/api/auth/me` at startup. A functional route guard protects the signed-in routes, and an HTTP interceptor maps `401` to the signed-out state.
- **Styling:** Angular Material for components, to keep UI work small in this first change.

### 9. Testing

- **API integration tests:** `WebApplicationFactory` with a PostgreSQL Testcontainer. A test authentication handler replaces Google, so tests can act as specific users.
- **BGG client and import tests:** run against recorded XML fixtures through a fake `HttpMessageHandler`, including the `202`-then-`200` sequence and error bodies.
- **Angular:** component and service tests for the guard, the interceptor, and the import status polling.

## Risks / Trade-offs

- [BGG application token approval can take time] → Request the token at the start of implementation. Until it arrives, develop against the recorded fixtures.
- [BGG XML format or limits change] → All parsing lives in `BggClient` behind fixture-based tests, and the rate limit interval is configurable.
- [The shared rate limiter makes searches wait while a large import runs] → Accepted for v1. If it becomes noticeable, give interactive search priority through a separate small budget.
- [The Google redirect URI must be exact, and Aspire assigns dynamic ports] → Pin the Angular dev server port in the AppHost and register `https://localhost:<port>/api/auth/signin-google` in the Google console. Document this in the README.
- [The in-process worker does not scale out] → A single instance is a stated non-goal. Moving the worker to a separate project, with row locking (`FOR UPDATE SKIP LOCKED`), is the upgrade path.
- [Re-importing re-adds games the user removed locally] → This is intended behaviour per the spec. Revisit with an "ignored" list if users find it annoying.

## Migration Plan

This is a greenfield change with no existing data or deployment, so nothing needs migrating. Rollback means reverting the change.

## Open Questions

- Production hosting target (for example Azure Container Apps through `aspire deploy`, or a self-hosted container). This does not affect the specs or the tasks in this change.
