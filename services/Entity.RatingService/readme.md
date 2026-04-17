# Rating Service

## Overview

The Rating Service owns player rating data and exposes APIs to read, update, and calculate ratings.

## API Endpoints

| Method | Endpoint | Description |
|---|---|---|
| GET | `/health` | Health check returning `{"status":"ok"}` |
| GET | `/rating/{id}` | Get rating by player id |
| POST | `/rating/{id}` | Update rating by player id |
| POST | `/glicko2` | Calculate updated rating from match result |

## Environment Variables

| Variable | Description | Default |
|---|---|---|
| `RATING_DB_HOST` | Rating database host | `rating-db` |
| `RATING_DB_PORT` | Host port exposed by Docker Compose | `5435` |
| `RATING_DB_INTERNAL_PORT` | MySQL container port used by service | `3306` |
| `RATING_DB_NAME` | Database name | `ratingdb` |
| `RATING_DB_USER` | Database user | `rating` |
| `RATING_DB_PASSWORD` | Database password | `ratingpass` |
| `RATING_DB_ROOT_PASSWORD` | Root password for DB container | `rootpass` |
| `RATING_SERVICE_PORT` | Host port exposed by Docker Compose | `5005` |

## Running Locally

```bash
docker compose up rating-db rating-service --build
```
