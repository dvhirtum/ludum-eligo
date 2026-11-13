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

- Technical
  - [Architecture](docs/technical/architecture.md)
  - [Data model](docs/technical/data-model.md)
  - [Testing](docs/technical/testing.md)
