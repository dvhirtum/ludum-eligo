# Design

## Context

This change builds on `collection-and-auth`. It relies on:
- the per-user `CollectionItem` rows and the shared `Game` table, including nullable `MinPlayers`/`MaxPlayers`, `MinPlaytime`/`MaxPlaytime`, `Weight`, and the `Mechanics`/`Categories` `text[]` columns;
- cookie auth with antiforgery on non-GET requests;
- the Angular shell, with a guard, an interceptor, and Material.

For motivation see proposal.md. The behaviour is defined in `specs/game-filtering`, `specs/game-quiz`, and `specs/pick-history`.

A user's collection is small enough to process in memory: realistically up to a few hundred games, and rarely more than about 1,000.

## Goals / Non-Goals

**Goals:**
- One implementation of the filter and quiz logic, written as pure functions that are easy to test exhaustively.
- Keep the server stateless for quiz progress, as the spec requires (progress lives in the browser).
- A filtering component that `game-vs-game-mode` can reuse without changes.

**Non-Goals:**
- Adaptive question ordering based on information gain. The spec fixes the order.
- Caching quiz computations. Recomputing on each step is cheap at these sizes.
- Letting history influence picks.

## Decisions

### 1. The engine runs on the server, behind a stateless step endpoint

```
 Browser (QuizStore)                       API
 -------------------                       ---
 seed, answers[] --POST /api/quiz/step-->  load user's games (one query)
                                           GameFilter.Apply(players, time)
                                           QuizEngine.Next(remaining, answers, seed)
                 <-- question | results | noMatch | emptyCollection
```

The client sends the full answer list on every step, and the server derives everything from it.
- Going back means dropping the last answer and posting again.
- A reload clears the client state, which matches the spec.

**Why the server:**
- The collection endpoint does not return mechanics, categories, or weight.
- `game-vs-game-mode` will reuse the same `GameFilter`.
- C# unit tests can cover the rules thoroughly.

**Alternative considered:** running the engine in Angular on a richer collection payload. It gives instant steps, but would duplicate the filter for the game-vs-game API side and move business rules into the client.

### 2. Engine structure (pure, no I/O)

- `GameFilter.Apply(games, players, time)` implements the `game-filtering` rules: unknown values never remove a game, and "12 or more" means `MaxPlayers >= 12`.
- `QuizEngine.Next(remaining, answers, seed)` runs these steps:
  1. While the filtering answers are incomplete, return the next filtering question.
  2. Handle the early outcomes: no games left returns `noMatch`, and exactly one game left returns `results` straight away.
  3. Compute the **askable** preference questions once from the remaining set. A question is askable when at least one of its answer options would score the remaining games unequally. Because preferences never remove games, the remaining set, and therefore the askable list, does not change after filtering.
  4. Score the games with the answered preferences, at one point per match.
  5. Check the early stop: if `lead > number of askable questions not yet answered`, return `results`.
  6. Otherwise return the next unanswered askable question, or `results` when none remain.
- Classification helpers:
  - `IsCooperative`: mechanics contains `"Cooperative Game"`.
  - `WeightBand`: light below 2.0, medium below 3.0, heavy otherwise, and null when weight is unknown.
  - The category options are the top 4 categories by frequency in the remaining set, with ties broken alphabetically so the options are stable.

### 3. Seeded tie-breaking

The client generates a random `seed` when a quiz starts and sends it with every step. Equal scores are ordered by a seeded shuffle.

- The order stays stable while the user moves back and forth within one quiz.
- It varies between quizzes with the same answers, as the "ties ordered randomly" scenario requires.

Tests can pass fixed seeds.

### 4. API contract

`POST /api/quiz/step`

```
request:  { seed: int, answers: { players?: int, time?: 30|60|90|120|null(no limit),
                                  coop?: "coop"|"competitive"|"any",
                                  weight?: "light"|"medium"|"heavy"|"any",
                                  category?: string|"any" } }
response: { kind: "question", question: { id, text, options: [{ value, label }] },
            step: int, remainingGames: int }
        | { kind: "results", picks: [{ bggId, name, imageUrl, score }] }   // 1..3 items
        | { kind: "noMatch" }
        | { kind: "emptyCollection" }
```

- It is a POST, so it carries the antiforgery header, even though nothing changes on the server.
- Answers are a keyed object, not an ordered list, because the question order is fixed by the engine.
- An invalid value (players outside 1–12, or an unknown option) returns `400`.

### 5. Pick history storage

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

| Method | Route | Behaviour |
|---|---|---|
| POST | `/api/history` `{ bggId, mode }` | Checks that the game is in the caller's collection and that the mode is known (otherwise `400`), then inserts the pick with the server time. |
| GET | `/api/history?page=1` | Returns 50 entries per page, newest first, joined to `Game` for the name and image, with a `hasMore` flag. |

Offset paging is enough at these volumes.

`mode` is stored as text so the next change can use `"game-vs-game"` without a migration.

### 6. Angular

- **`QuizStore`** (signals) holds the seed, the answers, and the last response. It exposes the actions `answer(id, value)`, `back()`, `restart()`, and `confirm(bggId)`.
- **`/quiz` page** renders a single question card with large option buttons and a progress hint ("12 games left"). It then shows a results view with the top pick emphasised, the runners-up, and a "We'll play this" button on each.
  - Confirming posts to `/api/history` and shows a short confirmation with links to "Pick again" and "History".
  - The player count uses a stepper or chip set from 1 to 12+.
- **`/history` page** is a list with a "Load more" button, plus an empty state that links to the quiz.
- Navigation gains "Quiz" and "History" entries. The landing redirect for signed-in users stays on the collection.

### 7. Testing

- **`GameFilter` unit tests:** every scenario in `game-filtering`, including the unknown values and the 12+ case.
- **`QuizEngine` table-driven tests:** question order, skipping, early stop (including the "lead equals the remaining questions, so keep asking" boundary), results of 1, 2, and 3 picks, `noMatch`, and the fixed-seed tie order.
- **API integration tests:** `/api/quiz/step` and `/api/history`, covering validation, ownership, paging, and history surviving the removal of a game.
- **Angular tests:** `QuizStore` (back, restart, and the confirm flow) and the history page's paging.

## Risks / Trade-offs

- [BGG renames the "Cooperative Game" mechanic] → The name is one constant in `QuizEngine`. Matching by BGG mechanic id would need the catalog to store ids, which is a possible follow-up.
- [Every step reloads the user's games from the database] → It is one indexed query over at most about 1,000 rows, which is fine. Add a short per-request cache only if profiling shows a need.
- [Category options can be generic, such as "Card Game" or "Dice"] → This is accepted for v1, because these are BGG categories. Curating or grouping them is a later improvement.
- [The client could post a pick for a game the quiz never offered] → History only checks ownership. Recording a false pick only affects the user's own display-only history, so this is acceptable.

## Migration Plan

Add a migration for `PickHistory`. No existing data is affected. Rollback is reverting the change and dropping the table.
