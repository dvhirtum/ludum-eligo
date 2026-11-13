# BGG integration

Ludum Eligo reads board game data from the [BoardGameGeek XML API2](https://boardgamegeek.com/wiki/page/BGG_XML_API2). It never writes to BGG.

## `BggClient`

`IBggClient` (`Infrastructure/Bgg/BggClient.cs`) is the only code that calls BGG. It is a typed `HttpClient` and parses XML with `System.Xml.Linq`.

| Method | BGG request | Returns |
|---|---|---|
| `SearchAsync(query)` | `search?query=…&type=boardgame` | Id, name and year of each match, each id once |
| `GetThingsAsync(ids)` | `thing?id=a,b,…&stats=1`, in batches of 20 ids | Metadata per game; ids BGG does not know are left out |
| `UserExistsAsync(username)` | `user?name=…` | `false` for `404` or an empty `<user id="">` |
| `GetOwnedCollectionAsync(username)` | `collection?username=…&own=1&subtype=boardgame&excludesubtype=boardgameexpansion&brief=1` | Ids of the owned base games |

Mapping from a `thing`:

| Field | BGG element | Unknown when |
|---|---|---|
| `Name` | primary `name` | — |
| `YearPublished` | `yearpublished` | `0` |
| `ImageUrl`, `ThumbnailUrl` | `image`, `thumbnail` | missing or empty |
| `MinPlayers`, `MaxPlayers` | `minplayers`, `maxplayers` | `0` |
| `MinPlaytime`, `MaxPlaytime` | `minplaytime`, `maxplaytime` | `0` |
| `Weight` | `statistics/ratings/averageweight` | `0` (unrated) |
| `Mechanics` | `link type="boardgamemechanic"` | empty list |
| `Categories` | `link type="boardgamecategory"` | empty list |

Unknown values are `null`, never `0`.

## Errors

Every failure throws `BggUnavailableException`:

- BGG cannot be reached, or the request times out;
- BGG answers anything other than `200` (except `202` for collections, and `404` for users);
- the response is not valid XML;
- a collection request returns an `<errors>` document. The message reads "BGG did not return the collection (…). It may be private or unavailable."

## Rate limiting

- `BggRateLimiter` (singleton) lets one request through per `Bgg:RequestInterval` (default 2 seconds). Waiting callers are served in arrival order.
- `RateLimitingHandler` puts every `BggClient` request through it, so searches and imports share one budget.
- The standard resilience handler from ServiceDefaults wraps the rate limiter. It retries `408`, `429` and `5xx` with exponential backoff, and each retry waits for a slot too.

## `202 Accepted`

BGG answers a collection request with `202` while it prepares the data. `GetOwnedCollectionAsync` asks again after 2, 4, 8, … seconds (at most 1 minute between attempts). If BGG still answers `202` after `Bgg:CollectionTimeout` (default 10 minutes), it throws `BggUnavailableException`.

## Game catalog

`IGameCatalog` (`Features/Catalog/GameCatalog.cs`) keeps the shared `games` table filled from BGG. Slices that need a game's metadata call `EnsureGamesAsync(ids)`:

| Game in `games` | Action |
|---|---|
| Missing | Fetched from BGG |
| Refreshed 30 days ago or less | Used as is; BGG is not called |
| Refreshed more than 30 days ago (stale) | Fetched again and updated |

- Missing and stale games are fetched together, through `GetThingsAsync`.
- Fetched games are written with `INSERT … ON CONFLICT (bgg_id) DO UPDATE`, which sets `refreshed_at` to now.
- If BGG fails and every requested game is cached, the cached metadata is kept (stale included) and a warning is logged.
- If BGG fails and a requested game is not cached, the result is `BggUnavailable`.
- Ids BGG does not know are left out of the result.
- The 30-day rule uses `TimeProvider` (`GameCatalog.IsStale`).

## Search endpoint

`GET /api/games/search?q=` (`Features/Catalog/SearchGames.cs`, signed in):

| Case | Response |
|---|---|
| Term (trimmed) shorter than 3 characters, or missing | `400`, error on `q`; BGG is not called |
| Matches | `200 [{ bggId, name, yearPublished }]` |
| No matches | `200 []` |
| BGG unavailable | `503`, `code: bgg_unavailable` |

Search results are not stored in the catalog.

## Configuration

| Setting | Default | Set by |
|---|---|---|
| `Bgg:Token` | — | AppHost parameter `bgg-token` (secret), sent as `Authorization: Bearer <token>` |
| `Bgg:BaseAddress` | `https://boardgamegeek.com/xmlapi2/` | |
| `Bgg:RequestInterval` | `00:00:02` | |
| `Bgg:CollectionTimeout` | `00:10:00` | |

Request a token by registering an application with BGG, as described in [Using the XML API](https://boardgamegeek.com/using_the_xml_api). Without a token BGG rejects requests, and the app reports BGG as unavailable.

## Tests

- Unit tests run `BggClient` against XML fixtures in `tests/LudumEligo.Api.UnitTests/Infrastructure/Bgg/Fixtures` through a fake `HttpMessageHandler`.
- `InstantTimeProvider` fires timers at once and records each wait, so rate limiting and `202` retries are tested without waiting.
