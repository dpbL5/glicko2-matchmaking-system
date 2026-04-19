# Player Service

## Overview

The Player Service owns player profile data and provides read-only player lookup endpoints.

## Tech Stack

| Component | Choice |
|---|---|
| Language | C# |
| Framework | ASP.NET Core (.NET 9) |
| Database | MySQL 8 |
| ORM | Entity Framework Core + Pomelo |

## API Endpoints

| Method | Endpoint | Description |
|---|---|---|
| GET | `/health` | Health check returning `{"status":"ok"}` |
| GET | `/players` | List all players |
| GET | `/players/{id}` | Get player by id |

Compatibility routes are also exposed at `/player` and `/player/{id}` for existing callers.

## Environment Variables

The service reads DB settings from the following variables (as required by architecture and compose setup):

| Variable | Description | Example |
|---|---|---|
| `DB_HOST` | Player database hostname on Docker network | `player-db` |
| `DB_PORT` | Player database port used by the service | `3306` |
| `DB_NAME` | Player database name | `playerdb` |
| `DB_USER` | Database username | `player` |
| `DB_PASSWORD` | Database password | `playerpass` |

Docker Compose maps these values from `.env` so secrets/config are externalized.

## Setup

1. Ensure `.env` exists (copy from `.env.example` if needed).
2. Start the service and database:

```bash
docker compose up --build player-db player-service
```

3. Verify health:

```bash
curl http://localhost:5001/health
```

Expected response:

```json
{"status":"ok"}
```

## Gateway Access

- Direct service: `http://localhost:5001/players`
- Through gateway: `http://localhost:8080/api/player` and `http://localhost:8080/api/player/{id}`

## Notes

- The service listens on port `5001` inside the container.
- Database schema and seed records are created automatically on first startup.