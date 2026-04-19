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
| POST | `/matches` | Create match record |
| GET | `/matches/{id}` | Get match by id |
| POST | `/matches/{id}/result` | Update match status and result |

Compatibility routes are also exposed at `/match`, `/match/{id}`, and `/match/{id}/result` for existing callers.

## Environment Variables

The service reads DB settings from the following variables (as required by architecture and compose setup):

| Variable | Description | Example |
|---|---|---|
| `DB_HOST` | Match database hostname on Docker network | `match-db` |
| `DB_PORT` | Match database port used by the service | `3306` |
| `DB_NAME` | Match database name | `matchdb` |
| `DB_USER` | Database username | `match` |
| `DB_PASSWORD` | Database password | `matchpass` |

Docker Compose maps these values from `.env` so secrets/config are externalized.

## Setup

1. Ensure `.env` exists (copy from `.env.example` if needed).
2. Start the service and database:

```bash
docker compose up --build match-db match-service
```

3. Verify health:

```bash
curl http://localhost:5002/health
```

Expected response:

```json
{"status":"ok"}
```

## Gateway Access

- Direct service: `http://localhost:5002/matches`
- Through gateway: `http://localhost:8080/api/match` and `http://localhost:8080/api/matches`

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

- The service listens on port `5002` inside the container.
- Match records are persisted in the Match database and can be updated after results are submitted.