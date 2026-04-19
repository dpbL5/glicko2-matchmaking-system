# Rating Service

## Overview

The Rating Service owns player rating data and exposes APIs to read, update, and recalculate ratings after match results.

## API Endpoints

| Method | Endpoint | Description |
|---|---|---|
| GET | `/health` | Health check returning `{"status":"ok"}` |
| GET | `/ratings/{id}` | Get rating by player id |
| POST | `/ratings/{id}` | Update rating by player id |
| POST | `/ratings/{id}/recalculate` | Recalculate rating from match result |

Compatibility routes are also exposed at `/rating/{id}` and `/rating/{id}/recalculate` for existing callers.

## Environment Variables

The service reads DB settings from the following variables (as required by architecture and compose setup):

| Variable | Description | Example |
|---|---|---|
| `DB_HOST` | Rating database hostname on Docker network | `rating-db` |
| `DB_PORT` | Rating database port used by the service | `3306` |
| `DB_NAME` | Rating database name | `ratingdb` |
| `DB_USER` | Database username | `rating` |
| `DB_PASSWORD` | Database password | `ratingpass` |

Docker Compose maps these values from `.env` so secrets/config are externalized.

## Setup

1. Ensure `.env` exists (copy from `.env.example` if needed).
2. Start the service and database:

```bash
docker compose up --build rating-db rating-service
```

3. Verify health:

```bash
curl http://localhost:5005/health
```

Expected response:

```json
{"status":"ok"}
```

## Gateway Access

- Direct service: `http://localhost:5005/ratings/{id}`
- Through gateway: `http://localhost:8080/api/rating/{id}`

## Notes

- The service listens on port `5005` inside the container.
- Database schema and seed rating records are created automatically on first startup.
