# Design

## Context

This change builds on two earlier changes:
- **`collection-and-auth`**: the collection, the catalog, cookie auth with antiforgery, and the Angular shell.
- **`quiz-mode`**:
  - `GameFilter.Apply` (pure, server-side) with the `PlayerCount`/`AvailableTime` value types;
  - `POST /api/history`, which already accepts mode `"game-vs-game"`;
  - the quiz page's filtering-question UI.

For motivation see proposal.md. The behaviour is defined in `specs/game-vs-game`, together with the existing `game-filtering` and `pick-history` specs.

Pools stay small after filtering, typically 5–40 games, so a knockout needs at most a few dozen choices.

## Goals / Non-Goals

**Goals:**
- Reuse `GameFilter` without changing it, so both modes keep identical filtering rules.
- Keep the knockout logic as a pure, deterministic-under-test TypeScript module.
- Make undo exact, so it restores the same matchup, including across round boundaries.

**Non-Goals:**
- Server-side knockout state or validation of each choice.
- Bracket visualisation, beyond the progress figures the spec requires.
- Sampling a subset of large pools.

## Decisions

### 1. The server filters and the browser runs the knockout

```
 Browser                                        API
 -------                                        ---
 players, time --GET /api/collection/playable?players=4&time=60-->
                                                load user's games, GameFilter.Apply
              <-- { kind: "games", games: [...] } | noMatch | emptyCollection
 KnockoutState (pure TS, all choices local)
 winner confirmed --POST /api/history { bggId, mode: "game-vs-game" }-->
```

- `GET /api/collection/playable` takes the same answer values and validation as the quiz (`400` when out of range). It returns each game's `bggId`, name, image, and player and time ranges.
- It is a GET because it changes nothing, so it needs no antiforgery token.

**Why the knockout runs in the browser:**
- The whole game list is needed up front anyway, and every choice would otherwise cost a round trip.
- Progress is browser-only by spec.
- The server checks the one thing that matters, the final pick, through the existing ownership check on `POST /api/history`.

**Alternative considered:** a stateless server step endpoint like the quiz's. Rejected, because the client would have to resend up to n − 1 choices per step for no added trust. The server cannot stop a user from choosing whichever game they like anyway.

**Alternative considered:** reusing `POST /api/quiz/step` to get the filtered games. Rejected, because that endpoint returns quiz questions and results, not the playable list.

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

The player-count picker and the time options on the quiz page are extracted into a `FilteringQuestionsComponent`. It emits `{ players, time }` and is used by both the quiz and game vs game.

This is a refactor of `quiz-mode`'s UI only. The quiz behaviour does not change, and existing quiz component tests must still pass.

### 4. Angular pages

- **`VersusStore`** (signals) holds the phase (`filtering | loading | knockout | winner | noMatch | emptyCollection`), the game lookup by id, the current `KnockoutState`, and the undo stack. It exposes the actions `begin(players, time)`, `choose(id)`, `undo()`, `restart()`, and `confirm()`.
- **`/versus` page:**
  - It shows the filtering questions, then a matchup view.
  - The matchup view has two large cards side by side (stacked on narrow screens), showing the image, name, player range, and play time. The whole card is clickable.
  - A progress bar shows "Round 2 · 5 games left · 4 choices to go", with Undo and Restart buttons.
  - The winner view has a "We'll play this" button, which posts to history and shows a confirmation with links to "Pick again" and "History".
- **Single game after filtering:** the store goes straight to the winner phase.
- **Navigation:** gains a "Game vs game" entry, guarded for signed-in users.

### 5. Testing

- **API integration tests:** `/api/collection/playable` returns only the caller's games that pass the filter, rejects invalid values, and handles the empty-collection and no-match cases.
- **Knockout module unit tests** (seeded RNG):
  - the number of choices is n − 1 for n from 1 to 33;
  - even and odd rounds, with no repeated free pass when avoidable;
  - progress figures, including the spec's 9-game example;
  - different seeds give different first rounds.
- **`VersusStore` tests:** undo within a round, undo across a round boundary, restart, and confirm (posts once, with mode `"game-vs-game"`).
- **Quiz regression:** quiz component tests still pass after the filtering component is extracted.

## Risks / Trade-offs

- [Large filtered pools mean many choices (100 games is 99 choices)] → Accepted for v1. The progress display makes the length visible, and a "sample 16" option is the planned follow-up.
- [Two games with the same free-pass history in a 3-game round] → The rule only prevents a repeated free pass "when another game is available". Tests pin this edge case.
- [Extracting the filtering component could regress the quiz] → The quiz component tests run as part of this change's tasks.

## Migration Plan

There are no database changes. Deploy with the API and SPA. Rollback is reverting the change.
