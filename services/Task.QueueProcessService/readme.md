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
| GET | `/queue/{playerId}` | Get queue status by player id |
| POST | `/queue/search` | Find opponents in SR range |
| DELETE | `/queue/{playerId}` | Dequeue player from pool |

## Environment Variables

| Variable | Description | Default |
|---|---|---|
| `QUEUE_DB_HOST` | Queue database host | `queue-db` |
| `QUEUE_DB_PORT` | Host port exposed by Docker Compose | `5434` |
| `QUEUE_DB_INTERNAL_PORT` | MySQL container port used by the service | `3306` |
| `QUEUE_DB_NAME` | Database name | `queuedb` |
| `QUEUE_DB_USER` | Database user | `queue` |
| `QUEUE_DB_PASSWORD` | Database password | `queuepass` |
| `QUEUE_DB_ROOT_PASSWORD` | Root password for the DB container | `rootpass` |
| `QUEUE_SERVICE_PORT` | Host port exposed by Docker Compose | `5003` |
| `PLAYER_SERVICE_BASE_URL` | Internal Player Service base URL | `http://player-service:5001` |
| `RATING_SERVICE_BASE_URL` | Internal Rating Service base URL | `http://rating-service:5005` |

## Running Locally

```bash
docker compose up queue-db queue-service --build
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
- Matching is atomic inside the queue database transaction so selected players are removed from the waiting set together.