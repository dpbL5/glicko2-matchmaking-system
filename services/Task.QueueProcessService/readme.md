# Queue Process Service

## Overview

The Queue Process Service owns the waiting queue lifecycle, validates players before queueing, snapshots SR from the Rating Service, and finds matched candidates inside the configured SR range.

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
| POST | `/queue` | Enqueue player to pool |
| GET | `/queue/{playerId}` | Get queue ticket by player id |
| GET | `/queue/{playerId}/stream` | Subscribe to queue signals via SSE |
| DELETE | `/queue/{playerId}` | Dequeue player from pool |

## Environment Variables

| Variable | Description | Example |
|---|---|---|
| `DB_HOST` | Queue database host | `queue-db` |
| `DB_PORT` | Queue database port used by service | `3306` |
| `DB_NAME` | Queue database name | `queuedb` |
| `DB_USER` | Queue database user | `queue` |
| `DB_PASSWORD` | Queue database password | `queuepass` |
| `PLAYER_SERVICE_BASE_URL` | Internal Player Service base URL | `http://player-service:5001` |
| `RATING_SERVICE_BASE_URL` | Internal Rating Service base URL | `http://rating-service:5005` |

Docker Compose maps these from `.env` queue-specific values (`QUEUE_DB_*`) into `DB_*` for this service.

## Running Locally

1. Ensure `.env` exists (copy from `.env.example` if needed).
2. Start queue database and service:

```bash
docker compose up --build queue-db queue-service
```

3. Verify health:

```bash
curl http://localhost:5003/health
```

The service listens on port `5003` inside the container and is exposed through the API gateway at `/api/queue/*`.

## Project Structure

```
Task.QueueProcessService/
├── Dockerfile
├── Task.QueueProcessService.csproj
├── Program.cs
├── appsettings.json
├── readme.md
└── src/
    ├── Api/
    ├── Application/
    ├── Domain/
    └── Infrastructure/
```

## Notes

- The service uses environment variables bound into configuration and never hardcodes connection details.
- Queue entries are persisted with an authoritative SR snapshot fetched from the Rating Service.
- Frontend clients should detect queue lifecycle from SSE `queue-signal` events (`QUEUED`, `MATCH_FOUND`, `DEQUEUED`). `MATCH_FOUND` now includes `matchId` and `playerIds` in camelCase.