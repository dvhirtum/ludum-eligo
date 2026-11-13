# Your collection

Your collection is the list of board games you own. Only you can see and change it. All games come from [BoardGameGeek](https://boardgamegeek.com) (BGG).

## Viewing

**Your collection** (`/collection`) lists your games sorted by name. Each game shows its picture, year, player count, and play time, when BGG provides them.

If your collection is empty, the page offers two ways to fill it:

- **Search BGG** opens the add page.
- **Import from BGG** opens the settings page (see [Importing from BGG](bgg-import.md)).

## Filtering

Type in **Filter by name** to show only games whose name contains the text. Case does not matter. If nothing matches, the page says so.

## Adding a game

1. Choose **Add game** on your collection (or **Search BGG** when it is empty).
2. Type at least 3 characters of the game's name. The search starts when you stop typing.
3. The results show each game's name and year. Choose **Add** next to the game you own.

The page then says one of:

| Message | Meaning |
|---|---|
| "*Game* was added to your collection." | Added |
| "*Game* is already in your collection." | Nothing changed; each game is in your collection at most once |
| "BoardGameGeek is unavailable. Try again later." | BGG could not be reached; nothing was added |

If no game matches the search, the page shows "No games found."

## Removing a game

Choose **Remove** next to a game in your collection. Removing a game only affects your own collection.

## API

| Method | Route | Response |
|---|---|---|
| GET | `/api/collection` | `200` with your games, sorted by name |
| GET | `/api/games/search?q=` | `200` with matches, `400` for fewer than 3 characters, `503` when BGG is unavailable |
| POST | `/api/collection` `{ bggId }` | `200 { bggId, name, alreadyPresent }`, `400` for an invalid id, `404` when BGG does not know the game, `503` when BGG is unavailable |
| DELETE | `/api/collection/{bggId}` | `204`, or `404` when the game is not in your collection |

All routes need a signed-in user. POST and DELETE need the XSRF token (see [Authentication](../technical/authentication.md#antiforgery)).
