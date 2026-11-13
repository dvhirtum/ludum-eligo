# Architecture

## Solution layout

```
src/
  LudumEligo.slnx                     Solution file
  LudumEligo.AppHost/                 Aspire orchestration
  LudumEligo.ServiceDefaults/         OpenTelemetry, health checks, resilience defaults
  LudumEligo.Api/                     ASP.NET Core minimal API
    Features/                         Vertical slices, one folder per feature
    Infrastructure/                   Persistence and external clients
    Domain/                           Entities
    Common/                           Shared building blocks
  ludum-eligo-web/                    Angular SPA
    src/app/core/                     Auth and HTTP plumbing
    src/app/features/                 One folder per feature, mirroring the API
tests/
  LudumEligo.Api.UnitTests/           Tests without a database
  LudumEligo.Api.IntegrationTests/    Tests against PostgreSQL in a Testcontainer
docs/
  functional/                         What users can do
  technical/                          How the system is built
```

## Runtime resources

The AppHost starts:

| Resource | What it is |
|---|---|
| `postgres` | PostgreSQL container with a data volume |
| `ludumeligo` | The application database |
| `api` | `LudumEligo.Api`, health check at `/api/health` |
| `web` | Angular dev server on `https://localhost:4200`, using the ASP.NET Core dev certificate |

## Same origin

The browser only talks to one origin:

- **Development:** the Angular dev server proxies `/api/**` to the API (`proxy.conf.mjs`). The API URL comes from the `services__api__https__0` variable that Aspire injects.
- **Production:** `dotnet publish` builds the Angular app and copies it to the API's `wwwroot`. The API serves it, with a fallback to `index.html` for client routes. Unknown `/api` routes return `404`.

## Vertical slices

Each use case is one slice: one file in `Features/<Feature>/`, for example `Features/Collection/AddGame.cs`. A slice file contains:

- the `Command` or `Query` record;
- its `Validator` (FluentValidation), when the input needs validation;
- its `Handler`;
- a `Map<SliceName>(this IEndpointRouteBuilder)` extension that registers the endpoint.

`Program.cs` only calls the `Map…` extensions.

Rules:

- Handlers never call other handlers.
- Logic used by several slices lives in one component owned by one feature, for example `IGameCatalog` in `Features/Catalog`.
- Entities (`Domain/`) and `AppDbContext` are shared by all slices.

## Commands and queries

| Kind | Interface | Does |
|---|---|---|
| Command | `ICommandHandler<TCommand, TResult>` | Changes state through EF Core entities |
| Query | `IQueryHandler<TQuery, TResult>` | Reads with `AsNoTracking()` and maps to response records; never changes state |

Both live in `Common/Handlers.cs`. Both use one database.

## Results and errors

Handlers return `Result<T>` (`Common/Result.cs`), a C# union of a value and an `Error`. Code reads it with pattern matching (`result is Error error`). The endpoint converts it with `ToHttpResult`:

| `ErrorType` | HTTP status | Body |
|---|---|---|
| `Validation` | `400` | Validation problem details |
| `NotFound` | `404` | Problem details with `code` |
| `Conflict` | `409` | Problem details with `code` |
| `BggUnavailable` | `503` | Problem details with `code: bgg_unavailable` |

## Validation

`.WithValidation<T>()` on an endpoint adds `ValidationFilter<T>`. The filter runs the registered `IValidator<T>` on the `T` argument and returns `400` with the errors when validation fails. Validators are registered by scanning the API assembly.

## Handler registration

`AddHandlersFromAssembly` (`Common/HandlerRegistration.cs`) scans the API assembly for classes implementing `ICommandHandler<,>` or `IQueryHandler<,>`. Each one is registered as scoped, both as its concrete type and as its handler interface. Endpoints receive their handler through dependency injection:

```csharp
(AddGame.Command command, AddGame.Handler handler, CancellationToken ct) => ...
```

There is no mediator library.

## Background import

`ImportWorker` (`Features/Import/ImportWorker.cs`) is a `BackgroundService` in the API process. It assumes a single API instance.

1. On start, it marks every `Running` import as `Failed` ("The import was interrupted. Start a new import.").
2. Every `Import:PollInterval` (default 3 seconds), it takes the queued imports, oldest first. For each one it creates a DI scope and runs the `ProcessImport` command handler.

`ProcessImport` does all the work:

1. Sets the job to `Running`.
2. Reads the user's owned base games from BGG (`IBggClient.GetOwnedCollectionAsync`).
3. Makes sure the games are in the catalog (`IGameCatalog.EnsureGamesAsync`).
4. Inserts the missing `collection_items` rows with `ON CONFLICT DO NOTHING`, with source `Import`.
5. Stores the added and already-present counts and sets the job to `Succeeded`. Any failure sets it to `Failed` with a reason.

The integration test host does not run the worker. Tests call `ProcessImport` directly or start their own worker.
