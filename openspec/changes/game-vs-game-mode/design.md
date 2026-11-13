# Design

## Context

This change builds on two earlier changes:
- **`collection-and-auth`**:
  - the vertical-slice layout (`Features/`, `Common/` with `IQueryHandler<,>`, `Result<T>`, the validation endpoint filter, and `ICurrentUser`);
  - the collection and the catalog;
  - cookie auth with antiforgery on non-GET requests;
  - the two test projects;
  - the Angular shell.
- **`quiz-mode`**:
  - `Features/Filtering`: the `PlayerCount`/`AvailableTime` value types, `SelectableGame`, the pure `GameFilter`, and the `LoadSelectableGames(userId)` projection;
  - the `History/RecordPick` command (`POST /api/history`), which already accepts mode `"game-vs-game"`;
  - the quiz page's filtering-question UI.

For motivation see proposal.md. The behaviour is defined in `specs/game-vs-game`, together with the existing `game-filtering` and `pick-history` specs.

Pools stay small after filtering, typically 5–40 games, so a knockout needs at most a few dozen choices.

## Goals / Non-Goals

**Goals:**
- Reuse `Features/Filtering` without changing it, so both modes keep identical filtering rules. Versus depends on Filtering only, never on Quiz.
- Keep the knockout logic as a pure, deterministic-under-test TypeScript module.
- Make undo exact, so it restores the same matchup, including across round boundaries.

**Non-Goals:**
- Server-side knockout state or validation of each choice.
- Bracket visualisation, beyond the progress figures the spec requires.
- Sampling a subset of large pools.

## Decisions

### 1. The server filters and the browser runs the knockout

```
 Browser                                        API (Features/Versus/GetPlayableGames, query)
 -------                                        ---------------------------------------------
 players, time --GET /api/versus/games?players=4&time=60-->
                                                validator (PlayerCount, AvailableTime)
                                                LoadSelectableGames(currentUser)
                                                GameFilter.Apply(players, time)
              <-- { kind: "games", games: [...] } | noMatch | emptyCollection
 KnockoutState (pure TS, all choices local)
 winner confirmed --POST /api/history { bggId, mode: "game-vs-game" }-->  History/RecordPick (command)
```

**Backend slice:** the whole backend part of this change is one query slice, `Features/Versus/GetPlayableGames.cs`. Like the other slices it holds the `Query` record, the `Validator`, the `Handler`, and a `MapGetPlayableGames` endpoint extension.
- The validator reuses the `PlayerCount`/`AvailableTime` rules, with `players=1..12` and `time=30|60|90|120|none` exactly as in the quiz. Invalid values return `400`.
- The handler is `LoadSelectableGames(currentUser)` followed by `GameFilter.Apply`.
- It returns each game's `bggId`, name, image, and player and time ranges.
- It is a GET because it changes nothing, so it needs no antiforgery token.

**Route:** `/api/versus/games` follows the convention that routes sit under the feature that owns them.

**Why the knockout runs in the browser:**
- The whole game list is needed up front anyway, and every choice would otherwise cost a round trip.
- Progress is browser-only by spec.
- The server checks the one thing that matters, the final pick, through the ownership check in `RecordPick`.

**Alternatives considered:**
- A stateless server step query like the quiz's. Rejected, because the client would have to resend up to n − 1 choices per step for no added trust. The server cannot stop a user from choosing whichever game they like anyway.
- Reusing `GET /api/quiz/step` to get the filtered games. Rejected, because that query returns quiz questions and results, not the playable list. It would also make Versus depend on Quiz.
- Placing the endpoint at `/api/collection/playable`. Rejected, because the slice lives in `Features/Versus`, and routes follow the owning feature.

### 2. Knockout state model

```
KnockoutState {
  round: number                 // 1-based
  pending: Matchup[]            // remaining matchups in this round, in order
  advanced: GameId[]            // winners (and the bye) already through to next round
  byeLastRound: GameId | null
  eliminated: number
  total: number
}
Matchup = [GameId, GameId]
```

**Pure functions:**
- `start(games, rng)` builds round 1.
- `choose(state, winnerId, rng)` removes the current matchup, adds the winner to `advanced`, and when `pending` is empty, builds the next round from `advanced`.
- `isFinished(state)` is true when one game is left.
- `progress(state)` returns the round, the games still in (`total − eliminated`), and the choices left (`still in − 1`).

Each function has a single responsibility and no side effects.

**Building a round:**
1. Shuffle the remaining games with Fisher–Yates.
2. If the count is odd, give the free pass to the first game in shuffled order that is not `byeLastRound`, and put it straight into `advanced`.
3. Pair the rest in order.

**Randomness:** an injected `rng` function, `Math.random` in the app and a seeded PRNG in tests. This makes the random-pairing scenarios testable.

**Undo:** the store keeps a stack of previous `KnockoutState` snapshots, which are immutable objects. `undo()` pops the stack and restores the previous state.
- Undoing the first choice of a new round restores the state before the round was built, so the previous round's last matchup reappears exactly. This is the "undo across a round boundary" scenario.
- If the user then chooses again, the next round is shuffled afresh. That is acceptable, since pairing is random by spec.

Snapshots are small (arrays of ids), and there are at most n − 1 of them.

**Alternative considered:** recording choices and replaying them from a fixed seed. It is more fragile, because a replay would need the RNG sequence to stay identical. Snapshots are simpler.

### 3. Shared filtering UI

The player-count picker and the time options on the quiz page are extracted into a `FilteringQuestionsComponent`, in a shared `features/filtering` folder that mirrors the backend. It emits `{ players, time }` and is used by both the quiz and game vs game.

The extraction is done test-first:
1. Write the new component's own tests (it emits the chosen values and limits the player count to 1–12+).
2. Move the markup until those tests pass.
3. Switch the quiz to use it. The existing quiz component tests act as the regression check and must pass unchanged.

### 4. Angular pages

- **`VersusStore`** (signals) holds the phase (`filtering | loading | knockout | winner | noMatch | emptyCollection`), the game lookup by id, the current `KnockoutState`, and the undo stack. It exposes the actions `begin(players, time)` (calls `GET /api/versus/games`), `choose(id)`, `undo()`, `restart()`, and `confirm()`.
- **`/versus` page** (`features/versus`):
  - It shows the filtering questions, then a matchup view.
  - The matchup view has two large cards side by side (stacked on narrow screens), showing the image, name, player range, and play time. The whole card is clickable.
  - A progress bar shows "Round 2 · 5 games left · 4 choices to go", with Undo and Restart buttons.
  - The winner view has a "We'll play this" button, which posts to history and shows a confirmation with links to "Pick again" and "History".
- **Single game after filtering:** the store goes straight to the winner phase.
- **Navigation:** gains a "Game vs game" entry, guarded for signed-in users.

### 5. Testing: TDD and naming

**TDD:** every part is built test-first, with spec scenarios as the source of test cases. Write a failing test, make it pass, then refactor.

**Backend integration tests** (`LudumEligo.Api.IntegrationTests/Features/Versus`) cover `GetPlayableGames` against PostgreSQL. Test names use `Should_<outcome>_When_<situation>`, for example:
- `Should_return_only_playable_own_games_When_filters_are_given`
- `Should_return_no_match_When_no_game_fits`
- `Should_return_empty_collection_When_the_user_has_no_games`
- `Should_return_400_When_players_is_out_of_range`

No new backend unit tests are needed, because the filtering rules are already covered by `quiz-mode`'s `GameFilter` tests.

**Frontend tests** are written first too. The Should_When naming applies to backend tests only, so the TypeScript tests use plain `describe`/`it` sentences.
- **Knockout module** (seeded RNG):
  - the number of choices is n − 1 for n from 1 to 33;
  - even and odd rounds, with no repeated free pass when avoidable, including the 3-game edge case;
  - progress figures, including the spec's 9-game example;
  - different seeds give different first rounds.
- **`FilteringQuestionsComponent`:** its own tests, plus the unchanged quiz component tests as a regression check.
- **`VersusStore`:** undo within a round, undo across a round boundary, restart, a single game going straight to the winner, and confirm (posts once, with mode `"game-vs-game"`).
- **`/versus` page and winner view:** component tests.

## Risks / Trade-offs

- [Large filtered pools mean many choices (100 games is 99 choices)] → Accepted for v1. The progress display makes the length visible, and a "sample 16" option is the planned follow-up.
- [Two games with the same free-pass history in a 3-game round] → The rule only prevents a repeated free pass "when another game is available". Tests pin this edge case.
- [Extracting the filtering component could regress the quiz] → The new component is test-first, and the unchanged quiz component tests must pass as part of this change.
- [Filtering is now used by two features and could grow mode-specific branches] → Keep `Features/Filtering` limited to the `game-filtering` spec. Mode-specific logic stays in `Quiz` or `Versus`.

## Migration Plan

There are no database changes. Deploy with the API and SPA. Rollback is reverting the change.
