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

## Indexes

| Index | Columns | Kind |
|---|---|---|
| `ix_users_google_subject` | `users(google_subject)` | Unique |

## Migrations

- Migrations live in `src/LudumEligo.Api/Infrastructure/Persistence/Migrations`.
- Add one with:

  ```sh
  dotnet tool restore
  dotnet ef migrations add <Name> --project src/LudumEligo.Api --output-dir Infrastructure/Persistence/Migrations
  ```

- The API applies pending migrations at startup when `Database:MigrateOnStartup` is `true` (set in `appsettings.Development.json` and by the integration tests). In production, migrations run as a separate step.
