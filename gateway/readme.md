# API Gateway

Traefik is the active API gateway for this project.

## What it does

- Routes public `/api/*` requests to the correct backend service.
- Keeps the frontend talking to one entry point instead of many service ports.
- Preserves long-lived HTTP connections so SSE endpoints work through the proxy.
- Exposes `/health` for a simple gateway-level health check.

## Routing Table

| External Route | Service | Internal Path |
|---|---|---|
| `/api/players/*` | Player Service | `/players` |
| `/api/queue/*` | Queue Process Service | `/queue` |
| `/api/matches/*` and `/api/match/*` | Match Service | `/matches` or `/match` |
| `/api/mm/*` | Matchmaking Process Service | `/mm` |
| `/api/ratings/*` and `/api/rating/*` | Rating Service | `/ratings` or `/rating` |

## SSE

Queue status streaming works through Traefik at `GET /api/queue/{playerId}/stream`.
The upstream queue service returns `text/event-stream`, and Traefik forwards the stream without buffering.

## Health Check

`GET /health` returns `{"status":"ok"}`.

## Running

```bash
docker compose up -d gateway
```

## Notes

- Traefik config lives in `gateway/traefik/` — all routing rules defined in YAML
- Static config: `traefik.yml` (entrypoint, file provider)
- Dynamic config: `dynamic.yml` (routers, middlewares, backend services)
- No application code needed — Traefik handles all routing and proxying
