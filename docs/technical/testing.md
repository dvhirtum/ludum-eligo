# Testing

## Test-driven development

Every slice, shared component, and Angular service or component is built test-first: write a failing test, make it pass with the simplest code, then refactor. Spec scenarios are the source of test cases.

## Test projects

| Project | Covers | Needs |
|---|---|---|
| `tests/LudumEligo.Api.UnitTests` | Validators, `Common/` building blocks, BGG parsing, rate limiting, pure logic | Nothing |
| `tests/LudumEligo.Api.IntegrationTests` | Handlers and endpoints against a real database | Docker |
| `src/ludum-eligo-web` (`*.spec.ts`) | Angular components, services, guards, interceptors (Vitest) | Nothing |

Integration tests:

- run against PostgreSQL in a Testcontainer (`Infrastructure/ApiFactory.cs`), started once and shared by every test in the `api` collection;
- host the API with `WebApplicationFactory`, which applies the migrations at startup;
- use unique ids from `TestData` so tests sharing the database never collide.

Both .NET test projects mirror the API's folders, for example `Features/Collection/AddGameTests.cs`.

## Naming

Backend test methods use Snake_Case with the pattern `Should_<outcome>_When_<situation>`:

```csharp
public async Task Should_store_unknown_weight_as_null_When_saving_a_game_without_weight()
```

## Running the tests

From the repository root:

```sh
# Both .NET test projects (Docker must be running)
dotnet test src/LudumEligo.slnx

# Only unit tests
dotnet test tests/LudumEligo.Api.UnitTests

# Angular
cd src/ludum-eligo-web
npm ci
npm test
```

`dotnet test` and the integration tests build the API. Stop a running AppHost first; its API process locks the API assembly.
