# Data model

PostgreSQL through EF Core (Npgsql). Table and column names use snake_case.

## Entities

### `User` (`users`)

| Column | Type | Notes |
|---|---|---|
| `id` | `uuid` | Primary key |
| `google_subject` | `varchar(255)` | Google `sub` claim; unique |
| `display_name` | `varchar(200)` | |
| `email` | `varchar(320)` | |
| `bgg_username` | `varchar(100)?` | Linked BGG username; `NULL` when none |
| `created_at` | `timestamptz` | |

### `Game` (`games`)

The shared catalog, one row per BGG game. Metadata BGG does not provide is `NULL`, never `0`.

| Column | Type | Notes |
|---|---|---|
| `bgg_id` | `integer` | Primary key; the BGG id |
| `name` | `varchar(500)` | |
| `year_published` | `integer?` | |
| `image_url`, `thumbnail_url` | `text?` | |
| `min_players`, `max_players` | `integer?` | |
| `min_playtime`, `max_playtime` | `integer?` | Minutes |
| `weight` | `numeric(5,4)?` | BGG average weight (1–5) |
| `mechanics` | `text[]` | |
| `categories` | `text[]` | |
| `refreshed_at` | `timestamptz` | Last fetch from BGG |

Rows are written by `GameCatalog` (see [BGG integration](bgg-integration.md#game-catalog)).

### `CollectionItem` (`collection_items`)

A game in a user's collection.

| Column | Type | Notes |
|---|---|---|
| `user_id` | `uuid` | Primary key (with `bgg_id`); foreign key to `users`, cascade delete |
| `bgg_id` | `integer` | Primary key (with `user_id`); foreign key to `games`, restrict delete |
| `added_at` | `timestamptz` | |
| `source` | `varchar(20)` | `Search` or `Import` |

### `ImportJob` (`import_jobs`)

One import of a user's BGG collection. Also the queue that `ImportWorker` reads.

| Column | Type | Notes |
|---|---|---|
| `id` | `uuid` | Primary key |
| `user_id` | `uuid` | Foreign key to `users`, cascade delete |
| `status` | `varchar(20)` | `Queued`, `Running`, `Succeeded` or `Failed` |
| `created_at` | `timestamptz` | |
| `started_at`, `finished_at` | `timestamptz?` | |
| `added` | `integer?` | Games added; set on success |
| `already_present` | `integer?` | Owned games already in the collection; set on success |
| `error` | `varchar(1000)?` | Failure reason |

## Indexes

| Index | Columns | Kind |
|---|---|---|
| `ix_users_google_subject` | `users(google_subject)` | Unique |
| `pk_collection_items` | `collection_items(user_id, bgg_id)` | Primary key: a user has each game at most once |
| `ix_collection_items_bgg_id` | `collection_items(bgg_id)` | Foreign key index |
| `ix_import_jobs_active_user` | `import_jobs(user_id) WHERE status IN ('Queued', 'Running')` | Partial unique: at most one active import per user |
| `ix_import_jobs_user_id_created_at` | `import_jobs(user_id, created_at)` | Latest import per user |
| `ix_import_jobs_status_created_at` | `import_jobs(status, created_at)` | Worker queue |

## Migrations

- Migrations live in `src/LudumEligo.Api/Infrastructure/Persistence/Migrations`.
- Add one with:

  ```sh
  dotnet tool restore
  dotnet ef migrations add <Name> --project src/LudumEligo.Api --output-dir Infrastructure/Persistence/Migrations
  ```

- The API applies pending migrations at startup when `Database:MigrateOnStartup` is `true` (set in `appsettings.Development.json` and by the integration tests). In production, migrations run as a separate step.
