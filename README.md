# Ludum Eligo

Ludum Eligo helps you pick the next board game to play for your game night.

## Prerequisites

- [.NET 11 SDK](https://dotnet.microsoft.com/download)
- [Node.js 24](https://nodejs.org/) with npm
- [Docker](https://www.docker.com/) (runs PostgreSQL and the integration tests)
- [Aspire CLI](https://aspire.dev/get-started/install-cli/)
- A trusted ASP.NET Core HTTPS development certificate:

  ```sh
  dotnet dev-certs https --trust
  ```

## Secrets

Set these once per machine, from the repository root. They are stored as user secrets of the AppHost.

### Google sign-in

1. Create an OAuth client as described in [Authentication](docs/technical/authentication.md#google-console-setup).
2. Store its id and secret:

   ```sh
   aspire secret set Parameters:google-client-id <client-id>
   aspire secret set Parameters:google-client-secret <client-secret>
   ```

### BoardGameGeek

1. Register an application with BGG to get an XML API2 token (see [BGG integration](docs/technical/bgg-integration.md#configuration)).
2. Store the token:

   ```sh
   aspire secret set Parameters:bgg-token <token>
   ```

If a parameter is missing, the Aspire dashboard asks for it at startup.

## Run

From the repository root:

```sh
aspire run
```

Aspire starts PostgreSQL, the API, and the Angular dev server, and prints the dashboard URL. The app runs at <https://localhost:4200>.

## Test

```sh
dotnet test src/LudumEligo.slnx
cd src/ludum-eligo-web && npm ci && npm test
```

## Documentation

- Functional
  - [Signing in and out](docs/functional/sign-in.md)
  - [Your collection](docs/functional/collection.md)
  - [Importing from BGG](docs/functional/bgg-import.md)
- Technical
  - [Architecture](docs/technical/architecture.md)
  - [Authentication](docs/technical/authentication.md)
  - [BGG integration](docs/technical/bgg-integration.md)
  - [Data model](docs/technical/data-model.md)
  - [Testing](docs/technical/testing.md)
