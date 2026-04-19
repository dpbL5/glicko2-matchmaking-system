# Frontend — Glicko2 Matchmaking UI

Vue.js 3 + TypeScript + Pinia frontend for the Glicko2 Matchmaking System. Demonstrates the complete matchmaking workflow with integration to all backend services.

## Architecture

### Service Integration

The frontend communicates with all backend services through the **API Gateway** (Traefik) at port 8080:

| Service | API Route | Purpose |
|---------|-----------|---------|
| **Player Service** | `/api/players/*` | List players, get player details, ratings |
| **Queue Service** | `/api/queue/*` | Enqueue player, check queue status, SSE updates |
| **Matchmaking Process Service** | `/api/mm` | Initialize matchmaking workflow for selected players |
| **Match Service** | `/api/matches/*` | Create matches, submit results |
| **Rating Service** | `/api/ratings/*` | Get and update player ratings |

### Project Structure

```
src/
├── services/           # API client layer
│   ├── playerService.ts    # Player Service API
│   ├── queueService.ts     # Queue Service API + SSE
│   ├── matchmakingProcessService.ts # Matchmaking Process Service API
│   ├── matchService.ts     # Match Service API
│   ├── ratingService.ts    # Rating Service API
│   └── index.ts            # Export all services
├── stores/
│   └── matchmaking.ts      # Pinia store (state + actions)
├── views/
│   └── HomeView.vue        # Main UI dashboard
├── types/
│   └── index.ts            # TypeScript interfaces
├── App.vue                 # Root component
└── main.ts                 # Vue setup
```

## Key Features

### 1. Service Health Monitoring
Displays real-time health status of all backend services:
- Player Service
- Queue Service
- Matchmaking Process Service
- Match Service
- Rating Service

### 2. Player Management
- Browse and select from available players
- Automatically fetch player rating after selection
- View player details (ID, name)

### 3. Queue Management
- Enqueue selected player to Queue Process Service
- Use SSE (`/queue/{playerId}/stream`) for queue status updates
- Select nearest-rated opponents and enqueue them
- Trigger Matchmaking Process Service (`POST /api/mm`) to initialize match
- Ability to leave queue

### 4. Match Workflow
- View matched opponents returned by matchmaking flow
- Submit one of three outcomes: **Win**, **Draw**, **Loss**
- Update match result via Match Service (`POST /matches/{id}/result`)
- Recalculate selected player rating via Rating Service (`POST /ratings/{id}/recalculate`)

### 5. System Logs
- Real-time activity feed
- Shows all API calls and service events
- Helps debug workflow issues

## Installation

```bash
cd frontend
npm install
```

## Development

### Local Development

```bash
npm run dev
```

Visit `http://localhost:5173` (Vite default port)

### Vite Configuration

The frontend uses environment variables in `.env`:

```env
VITE_API_GATEWAY=http://localhost:8080/api
VITE_PLAYER_SERVICE_URL=http://localhost:5001
VITE_QUEUE_SERVICE_URL=http://localhost:5003
VITE_MATCHMAKING_SERVICE_URL=http://localhost:5004
VITE_MATCH_SERVICE_URL=http://localhost:5002
VITE_RATING_SERVICE_URL=http://localhost:5005
```

### Build

```bash
npm run build
```

Outputs to `dist/` for production deployment.

## Docker

### Build & Run

```bash
# Using docker compose (recommended)
docker compose up --build frontend

# Or manual build
docker build -t glicko2-frontend:latest .
docker run -p 3000:80 glicko2-frontend:latest
```

The Docker image uses a multi-stage build:
1. **Build stage**: Node.js builds the Vue app
2. **Production stage**: Nginx serves the static files on port 80

Visit `http://localhost:3000`

## Workflow Demo

1. **Check Health**: All services should show green status on startup.
2. **Select Player**: Choose a player; rating is fetched automatically.
3. **Find Opponents**: Click "Find Opponents" to start Queue + Matchmaking flow.
4. **Wait for Match**: Observe queue SSE logs and matchmaking completion.
5. **Review Opponents**: Opponent names and ratings appear once match is created.
6. **Submit Outcome**: Click one of **Win**, **Draw**, or **Loss**.
7. **Verify New Rating**: Selected player's rating is recalculated and displayed.

## API Service Layer

### playerService.ts
```typescript
// Get all players
const players = await playerService.listPlayers();

// Get specific player
const player = await playerService.getPlayer(playerId);

// Health check
const health = await playerService.getHealth();
```

### queueService.ts
```typescript
// Enqueue player
const ticket = await queueService.enqueue({ playerId, sr: 1500 });

// Get queue status
const status = await queueService.getQueueStatus(playerId);

// SSE stream for live updates
const source = queueService.streamQueueUpdates(playerId, (data) => {
  console.log('Queue status:', data.status);
});

// Remove from queue
await queueService.removeFromQueue(playerId);
```

### matchService.ts
```typescript
// Create match
const match = await matchService.createMatch({ playerIds, queueId });

// Get match details
const match = await matchService.getMatch(matchId);

// Submit result
const result = await matchService.submitMatchResult(matchId, { winner, result });
```

### ratingService.ts
```typescript
// Get player rating
const rating = await ratingService.getPlayerRating(playerId);

// Update rating
const updated = await ratingService.updatePlayerRating(playerId, {
  rating: 1600,
  rd: 30,
  volatility: 0.06
});
```

## State Management (Pinia Store)

The `matchmaking` store manages all state:

```typescript
// Access store
const store = useMatchmakingStore();

// State
store.players            // All players
store.selectedPlayer     // Currently selected player
store.queueTicket        // Active queue ticket
store.queueStatus        // 'idle' | 'queuing' | 'matched' | 'error'
store.currentMatch       // Match data when matched
store.selectedPlayerRating // Player's Glicko-2 rating
store.logs              // System event logs

// Actions
await store.fetchPlayers();              // Load all players
await store.selectPlayer(playerId);      // Select and fetch player details
await store.joinQueue(playerId, sr);     // Enqueue
await store.leaveQueue(playerId);        // Dequeue
await store.submitMatchResult(...);      // Submit game result
await store.updatePlayerRating(...);     // Update rating
```

## Troubleshooting

### API Gateway Not Responding

Check that Traefik gateway is running:
```bash
docker compose ps | grep gateway
docker compose logs gateway --tail 20
```

### SSE Stream Not Updating

Ensure queue service is running and `/api/queue/{playerId}/stream` returns SSE:
```bash
curl -N http://localhost:8080/api/queue/{playerId}/stream
```

### Players List Empty

Verify Player Service is running and has seeded data:
```bash
curl http://localhost:8080/api/players
```

## References

- [Vue 3 Documentation](https://vuejs.org/)
- [TypeScript](https://www.typescriptlang.org/)
- [Pinia State Management](https://pinia.vuejs.org/)
- [Axios HTTP Client](https://axios-http.com/)
- [OpenAPI Specs](../docs/api-specs/)
- [System Architecture](../docs/architecture.md)

## Environment

- **Node.js**: 20+
- **npm**: 10+
- **Vue**: 3.4+
- **TypeScript**: 5.0+
- **Pinia**: 2.1+

