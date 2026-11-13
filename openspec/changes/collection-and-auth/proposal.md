# Proposal

## Why

Ludum Eligo helps a group pick which board game to play, but every picking mode depends on knowing which games a user owns and having consistent metadata about them (player count, play time, weight, mechanics). This change lays that foundation: the application skeleton, user sign-in, and a per-user collection backed by BoardGameGeek (BGG) data, so the quiz and game-vs-game modes can be built on top of it.

## What Changes

- Create the application: an Angular SPA and a .NET 11 API orchestrated by .NET Aspire, with PostgreSQL as the database. The SPA and API are served from the same origin.
- Add sign-in with Google as the only identity provider. The API handles the OAuth flow and keeps the session in an HttpOnly cookie; the SPA never handles tokens. Users are created on first sign-in.
- Add a shared game catalog whose entries are sourced from and cached from the BGG XML API2 (players, play time, weight, mechanics, categories, image).
- Let users search BGG and add games to, or remove games from, their own collection. Games can only be added from BGG; manual (non-BGG) games are out of scope.
- Let users store an optional BGG username and import their BGG collection. The import runs as a background job, only adds games that are missing, never removes games, and never writes back to BGG.

Out of scope: other identity providers, manual games, overriding BGG metadata, the quiz and game-vs-game modes, and pick history (covered by later changes).

## Capabilities

### New Capabilities
- `user-auth`: Signing in and out with a Google account, user creation on first sign-in, and protecting the API so only signed-in users can reach their data.
- `game-catalog`: The shared catalog of board games sourced from BGG, including searching BGG and fetching, caching, and refreshing game metadata.
- `game-collection`: A user's own collection of games: viewing, adding from the catalog, and removing games.
- `bgg-collection-import`: Linking a BGG username to a user and importing that user's BGG collection into their game collection as a merge that only adds games.

### Modified Capabilities
<!-- None: there are no existing specs. -->

## Impact

- New solution: Aspire AppHost and ServiceDefaults, a .NET 11 API project, an Angular SPA, and a PostgreSQL resource.
- External dependencies: Google OAuth client (client ID and secret), a registered BGG application token for the BGG XML API2.
- New database schema: users, games (shared catalog), collection items, and import jobs.
- New documentation: README and `docs/functional`, `docs/technical`.
- Later changes (`quiz-mode`, `game-vs-game-mode`) depend on the catalog metadata and collection defined here.
