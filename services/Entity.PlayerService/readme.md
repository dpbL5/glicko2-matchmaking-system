# Player Service

## Overview

The Player Service owns player profile data and exposes read-only REST endpoints for player lookup.

## Tech Stack

| Component | Choice |
|---|---|
| Language | C# |
| Framework | .NET 9 / ASP.NET Core Minimal API |
| Database | MySQL 8.4 |
| ORM | Entity Framework Core (Pomelo MySQL provider) |

## API Endpoints

| Method | Endpoint | Description |
|---|---|---|
| GET | `/health` | Health check returning `{"status":"ok"}` |
| GET | `/player` | List all players |
| GET | `/player/{id}` | Get a player by id |

## Environment Variables

| Variable | Description | Default |
|---|---|---|
| `PLAYER_DB_HOST` | Player database host | `player-db` |
| `PLAYER_DB_PORT` | Host port exposed by Docker Compose | `5432` |
| `PLAYER_DB_INTERNAL_PORT` | MySQL container port used by the service | `3306` |
| `PLAYER_DB_NAME` | Database name | `playerdb` |
| `PLAYER_DB_USER` | Database user | `player` |
| `PLAYER_DB_PASSWORD` | Database password | `playerpass` |
| `PLAYER_DB_ROOT_PASSWORD` | Root password for the DB container | `rootpass` |
| `PLAYER_SERVICE_PORT` | Host port exposed by Docker Compose | `5001` |

## Running Locally

```bash
docker compose up player-db player-service --build
```

The service listens on port `5001` inside the container and is exposed through the API gateway at `/api/player/*`.

## Project Structure

```
Entity.PlayerService/
├── Dockerfile
├── Entity.PlayerService.csproj
├── Program.cs
├── appsettings.json
├── readme.md
└── src/
    ├── Domain/
    │   └── PlayerRecord.cs
    └── Infrastructure/
        ├── EfPlayerRepository.cs
        ├── IPlayerRepository.cs
        ├── PlayerDbContext.cs
        ├── PlayerEntity.cs
        ├── PlayerDatabaseInitializer.cs
        └── PlayerDatabaseOptions.cs
```

## Notes

- The service uses environment variables bound into configuration and never hardcodes connection details.
- Seed data is inserted automatically on first start if the `players` table is empty.