# Design

## Context

This change builds on `collection-and-auth`. It relies on:
- the vertical-slice layout of the API (`Features/`, `Infrastructure/`, `Domain/`, `Common/`), including the `ICommandHandler<,>`/`IQueryHandler<,>` abstractions, `Result<T>`, the validation endpoint filter, and `ICurrentUser`;
- the per-user `CollectionItem` rows and the shared `Game` table, including nullable `MinPlayers`/`MaxPlayers`, `MinPlaytime`/`MaxPlaytime`, `Weight`, and the `Mechanics`/`Categories` `text[]` columns;
- cookie auth with antiforgery on non-GET requests;
- the two test projects (`LudumEligo.Api.UnitTests`, `LudumEligo.Api.IntegrationTests`) with the test authentication handler;
- the Angular shell, with a guard, an interceptor, and Material.

For motivation see proposal.md. The behaviour is defined in `specs/game-filtering`, `specs/game-quiz`, and `specs/pick-history`.

A user's collection is small enough to process in memory: realistically up to a few hundred games, and rarely more than about 1,000.

## Goals / Non-Goals

**Goals:**
- One implementation of the filter and quiz logic, written as pure functions that are easy to test exhaustively.
- Keep the server stateless for quiz progress, as the spec requires (progress lives in the browser).
- A filtering component that `game-vs-game-mode` can reuse without changes and without depending on the quiz feature.

**Non-Goals:**
- Adaptive question ordering based on information gain. The spec fixes the order.
- Caching quiz computations. Recomputing on each step is cheap at these sizes.
- Letting history influence picks.

## Decisions

### 1. Where the code lives

```
src/LudumEligo.Api/Features/
  Filtering/   PlayerCount, AvailableTime   value types with validation
               SelectableGame               record: BggId, name, image, players, time, weight,
                                            mechanics, categories
               GameFilter                   pure; shared by Quiz now and Versus later
  Quiz/        QuizEngine                   pure
               QuizClassification           pure: IsCooperative, WeightBand, category options
               SeededShuffle                pure
               TakeQuizStep.cs              query slice: GET /api/quiz/step
  History/     RecordPick.cs                command slice: POST /api/history
               GetPickHistory.cs            query slice: GET /api/history
src/LudumEligo.Api/Domain/
  PickHistory                               entity (+ configuration in Infrastructure/Persistence)
```

**`Filtering` is its own feature.**
- It is owned by the `game-filtering` capability, and the quiz and versus slices both depend on it. That way they never depend on each other.
- It holds only pure types and functions, so it adds no shared service layer.
- It also provides one query helper, `LoadSelectableGames(userId)`. This is an `IQueryable` projection to `SelectableGame` with `AsNoTracking()`, used by the query handlers of both modes.

**The engine is pure.** `GameFilter`, `QuizEngine`, `QuizClassification` and `SeededShuffle` have no database access, no dependency injection, and no clock. They take data in and return results, which keeps them in the unit-test project and follows single responsibility. Each one has one reason to change.

**Slice files** follow the `collection-and-auth` convention: one file holds the `Query` or `Command` record, its `Validator`, its `Handler`, and a `Map…` endpoint extension.

### 2. The engine runs on the server, behind a stateless query

```
 Browser (QuizStore)                          API (Quiz/TakeQuizStep, query)
 -------------------                          ------------------------------
 seed + answers --GET /api/quiz/step?...-->   validator (PlayerCount, AvailableTime, options)
                                              LoadSelectableGames(currentUser)
                                              GameFilter.Apply(players, time)
                                              QuizEngine.Next(remaining, answers, seed)
                <-- question | results | noMatch | emptyCollection
```

The client sends the full set of answers on every step, and the server derives everything from it.
- Going back means dropping the last answer and requesting again.
- A reload clears the client state, which matches the spec.

**Why GET:** `TakeQuizStep` changes nothing, so under CQRS it is a query.
- A GET makes that explicit and needs no antiforgery token.
- The answers are a few short values, so they fit comfortably in a query string.

**Why the server:**
- The collection endpoint does not return mechanics, categories, or weight.
- `game-vs-game-mode` will reuse the same `GameFilter`.
- C# unit tests can cover the rules thoroughly.

**Alternatives considered:**
- Running the engine in Angular on a richer collection payload. It gives instant steps, but would duplicate the filter for the game-vs-game API side and move business rules into the client.
- A POST step endpoint. It would mislabel a read as a write.

### 3. Engine structure

- `GameFilter.Apply(games, players, time)` implements the `game-filtering` rules: unknown values never remove a game, and "12 or more" means `MaxPlayers >= 12`.
- `QuizEngine.Next(remaining, answers, seed)` runs these steps:
  1. While the filtering answers are incomplete, return the next filtering question.
  2. Handle the early outcomes: no games left returns `noMatch`, and exactly one game left returns `results` straight away.
  3. Compute the **askable** preference questions once from the remaining set. A question is askable when at least one of its answer options would score the remaining games unequally. Because preferences never remove games, the remaining set, and therefore the askable list, does not change after filtering.
  4. Score the games with the answered preferences, at one point per match.
  5. Check the early stop: if `lead > number of askable questions not yet answered`, return `results`.
  6. Otherwise return the next unanswered askable question, or `results` when none remain.
- `QuizClassification`:
  - `IsCooperative`: mechanics contains `"Cooperative Game"`.
  - `WeightBand`: light below 2.0, medium below 3.0, heavy otherwise, and null when weight is unknown.
  - The category options are the top 4 categories by frequency in the remaining set, with ties broken alphabetically so the options are stable.
- The outcome is a closed set of result types (question, results, noMatch, emptyCollection). The endpoint maps each to the response shape below.

### 4. Seeded tie-breaking

The client generates a random `seed` when a quiz starts and sends it with every step. `SeededShuffle` orders equal scores with a PRNG built from that seed.

- The order stays stable while the user moves back and forth within one quiz.
- It varies between quizzes with the same answers, as the "ties ordered randomly" scenario requires.

Tests can pass fixed seeds.

### 5. API contract

`GET /api/quiz/step`

```
query:    seed=int (required)
          players=1..12        time=30|60|90|120|none
          coop=coop|competitive|any
          weight=light|medium|heavy|any
          category=<category name>|any
          (all answers optional; absent = not answered yet)
response: { kind: "question", question: { id, text, options: [{ value, label }] },
            step: int, remainingGames: int }
        | { kind: "results", picks: [{ bggId, name, imageUrl, score }] }   // 1..3 items
        | { kind: "noMatch" }
        | { kind: "emptyCollection" }
```

- An invalid value (players outside 1–12, an unknown option, or a missing seed) returns `400` from the validation filter.
- Answers are keyed parameters, not an ordered list, because the question order is fixed by the engine.

### 6. Pick history

```
PickHistory
-----------
Id (uuid, PK)
UserId (FK User)
BggId  (FK Game)          -- Game, not CollectionItem: survives removal from the collection
Mode   (text: "quiz" | "game-vs-game")
PickedAt (timestamptz)
INDEX (UserId, PickedAt DESC)
```

| Method | Route | Slice | Kind | Behaviour |
|---|---|---|---|---|
| POST | `/api/history` `{ bggId, mode }` | `History/RecordPick` | command | The validator checks the mode. The handler checks that the game is in the `ICurrentUser`'s collection and inserts the pick with `TimeProvider` time. It returns `Result<T>` with a `Validation` or `NotOwned` error, both mapped to `400`. |
| GET | `/api/history?page=1` | `History/GetPickHistory` | query | Returns 50 entries per page, newest first, projected with `AsNoTracking()` and joined to `Game` for the name and image, with a `hasMore` flag. |

Offset paging is enough at these volumes.

`mode` is stored as text so the next change can use `"game-vs-game"` without a migration.

### 7. Angular

- **Folders:** `features/quiz` and `features/history` mirror the backend.
- **`QuizStore`** (signals) holds the seed, the answers, and the last response. It exposes the actions `answer(id, value)`, `back()`, `restart()`, and `confirm(bggId)`. Each step is a `GET /api/quiz/step` with the answers as query parameters.
- **`/quiz` page** renders a single question card with large option buttons and a progress hint ("12 games left"). It then shows a results view with the top pick emphasised, the runners-up, and a "We'll play this" button on each.
  - Confirming posts to `/api/history` and shows a short confirmation with links to "Pick again" and "History".
  - The player count uses a stepper or chip set from 1 to 12+.
- **`/history` page** is a list with a "Load more" button, plus an empty state that links to the quiz.
- Navigation gains "Quiz" and "History" entries. The landing redirect for signed-in users stays on the collection.

### 8. Testing: TDD and naming

**TDD:** every component is built test-first, with spec scenarios as the source of test cases. Write a failing test, make it pass, then refactor.

**Unit tests** (`LudumEligo.Api.UnitTests/Features/Filtering`, `…/Features/Quiz`) cover:
- `PlayerCount` and `AvailableTime`;
- `GameFilter`, against every `game-filtering` scenario;
- `QuizClassification`;
- `SeededShuffle`;
- `QuizEngine`, with table-driven cases for question order, skipping, the early-stop boundary, results of 1, 2 and 3 picks, and `noMatch`.

**Integration tests** (`LudumEligo.Api.IntegrationTests/Features/Quiz`, `…/Features/History`) cover `TakeQuizStep`, `RecordPick` and `GetPickHistory` against PostgreSQL. They include validation, the separation between users, paging, and history surviving the removal of a game.

**Naming:** backend test names use `Should_<outcome>_When_<situation>`, for example:
- `Should_keep_the_game_When_its_player_count_is_unknown`
- `Should_skip_the_coop_question_When_all_remaining_games_are_competitive`
- `Should_keep_asking_When_the_lead_equals_the_remaining_questions`
- `Should_reject_the_pick_When_the_game_is_not_in_the_collection`

**Angular:** the `QuizStore` and page tests are also written first.

### 9. Documentation

This change follows the documentation layout from `collection-and-auth`: short, and limited to what things are and do. Each task group updates the files its own work touches.

| File | New or extended | Content |
|---|---|---|
| `README.md` | extended | The feature list gains Quiz and History. |
| `docs/functional/quiz.md` | **new** | The questions, what "no preference" does, results, confirming a pick, back and restart. |
| `docs/functional/history.md` | **new** | What is recorded, ordering, paging, and that it is display only. |
| `docs/technical/selection.md` | **new** | `Features/Filtering` (filter rules, `LoadSelectableGames`); `QuizEngine` (scoring, skipping, early stop, seeded tie-break); the `GET /api/quiz/step` contract. |
| `docs/technical/architecture.md` | extended | The Filtering, Quiz and History features, and the rule that Quiz and Versus depend on Filtering and never on each other. |
| `docs/technical/data-model.md` | extended | `PickHistory` and its index. |

- **Why one `selection.md`:** all the picking logic is documented in one place. `game-vs-game-mode` adds its knockout section to the same file.
- **Alternative considered:** separate `filtering.md` and `quiz-engine.md` files. They would split closely related rules across files.

## Risks / Trade-offs

- [BGG renames the "Cooperative Game" mechanic] → The name is one constant in `QuizClassification`. Matching by BGG mechanic id would need the catalog to store ids, which is a possible follow-up.
- [Every step reloads the user's games from the database] → `LoadSelectableGames` is one indexed projection over at most about 1,000 rows, which is fine. Add a short per-request cache only if profiling shows a need.
- [Category options can be generic, such as "Card Game" or "Dice"] → This is accepted for v1, because these are BGG categories. Curating or grouping them is a later improvement.
- [The client could post a pick for a game the quiz never offered] → `RecordPick` only checks ownership. Recording a false pick only affects the user's own display-only history, so this is acceptable.
- [Quiz state in the URL query string appears in server logs] → The answers are not sensitive (counts, moods, category names), so this is acceptable.

## Migration Plan

Add a migration for `PickHistory`. No existing data is affected. Rollback is reverting the change and dropping the table.
