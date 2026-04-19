# Matchmaking Process Service

## Overview

The Matchmaking Process Service orchestrates match initialization after a valid player group is selected. It creates a match through Match Service, then dequeues players from Queue Process Service as the process step of the matchmaking workflow.

## Tech Stack

| Component | Choice |
|---|---|
| Language | C# |
| Framework | .NET 9 / ASP.NET Core Controllers |
| Messaging | MassTransit + RabbitMQ |

## API Endpoints

| Method | Endpoint | Description |
|---|---|---|
| GET | `/health` | Health check returning `{"status":"ok"}` |
| POST | `/mm` | Initialize matchmaking workflow for a locked player group |

## Environment Variables

| Variable | Description | Example |
|---|---|---|
| `MATCH_SERVICE_BASE_URL` | Internal Match Service base URL | `http://match-service:5002` |
| `QUEUE_SERVICE_BASE_URL` | Internal Queue Service base URL | `http://queue-service:5003` |
| `RABBITMQ_HOST` | RabbitMQ host | `rabbitmq` |
| `RABBITMQ_PORT` | RabbitMQ AMQP port | `5672` |
| `RABBITMQ_USER` | RabbitMQ username | `guest` |
| `RABBITMQ_PASSWORD` | RabbitMQ password | `guest` |

## Running Locally

1. Ensure `.env` exists (copy from `.env.example` if needed).
2. Start dependent services, broker, and matchmaking process service:

```bash
docker compose up --build rabbitmq match-service queue-service matchmaking-process-service
```

3. Verify health:

```bash
curl http://localhost:5004/health
```

The service listens on port `5004` inside the container and is exposed through the API gateway at `/api/mm`.

## Project Structure

```
Task.MatchmakingProcessService/
├── Dockerfile
├── Task.MatchmakingProcessService.csproj
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

- Request validation rejects empty IDs, duplicate player IDs, and insufficient player count.
- Upstream failures are returned as structured Problem Details responses.
- `appsettings.json` provides Docker-friendly defaults for Match and Queue upstream URLs.
- The endpoint follows saga-style orchestration: create match first, then dequeue participants.
- On successful orchestration, the service publishes `MatchReady` event to RabbitMQ.
- The service consumes `RatingUpdated` and `MatchUpdated` events through MassTransit consumers.
