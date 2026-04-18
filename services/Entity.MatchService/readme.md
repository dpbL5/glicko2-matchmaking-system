# Match Service

## Overview

The Match Service owns match records and exposes REST endpoints for match creation, lookup, and result updates.

## Tech Stack

| Component | Choice |
|---|---|
| Language | C# |
| Framework | .NET 9 / ASP.NET Core Controllers |
| Database | MySQL 8.4 |
| ORM | Entity Framework Core (Pomelo MySQL provider) |

## API Endpoints

| Method | Endpoint | Description |
|---|---|---|
| GET | `/health` | Health check returning `{"status":"ok"}` |
| POST | `/match` | Create match record |
| GET | `/match/{id}` | Get match by id |
| POST | `/match/{id}/result` | Update match status and result |

## Environment Variables

| Variable | Description | Default |
|---|---|---|
| `MATCH_DB_HOST` | Match database host | `match-db` |
| `MATCH_DB_PORT` | Host port exposed by Docker Compose | `5433` |
| `MATCH_DB_INTERNAL_PORT` | MySQL container port used by the service | `3306` |
| `MATCH_DB_NAME` | Database name | `matchdb` |
| `MATCH_DB_USER` | Database user | `match` |
| `MATCH_DB_PASSWORD` | Database password | `matchpass` |
| `MATCH_DB_ROOT_PASSWORD` | Root password for the DB container | `rootpass` |
| `MATCH_SERVICE_PORT` | Host port exposed by Docker Compose | `5002` |

## Running Locally

```bash
docker compose up match-db match-service --build
```

The service listens on port `5002` inside the container and is exposed through the API gateway at `/api/match/*`.

## Project Structure

```
Entity.MatchService/
├── Dockerfile
├── Entity.MatchService.csproj
├── Program.cs
├── appsettings.json
├── readme.md
└── src/
    ├── Api/
    │   ├── HealthController.cs
    │   ├── MatchController.cs
    │   ├── MatchCreateRequestDto.cs
    │   ├── MatchDto.cs
    │   ├── MatchResultRequestDto.cs
    │   └── MatchResultResponseDto.cs
    ├── Application/
    │   └── IMatchRepository.cs
    ├── Domain/
    │   ├── Match.cs
    │   └── MatchStatus.cs
    └── Infrastructure/
        ├── MatchDatabaseInitializer.cs
        ├── MatchDatabaseOptions.cs
        ├── MatchDbContext.cs
        └── MatchRepository.cs
```

## Notes

- The service uses environment variables bound into configuration and never hardcodes connection details.
- Match records are persisted automatically and can be updated later with the final match result.