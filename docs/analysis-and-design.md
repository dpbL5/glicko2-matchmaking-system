# Analysis and Design — Business Process Automation Solution

> **Goal**: Analyze a specific business process and design a service-oriented automation solution (SOA/Microservices).
> Scope: 4–6 week assignment — focus on **one business process**, not an entire system.

**References:**
1. *Service-Oriented Architecture: Analysis and Design for Services and Microservices* — Thomas Erl (2nd Edition)
2. *Microservices Patterns: With Examples in Java* — Chris Richardson
3. *Bài tập — Phát triển phần mềm hướng dịch vụ* — Hung Dang (available in Vietnamese)

---

## Part 1 — Analysis Preparation

### 1.1 Business Process Definition

Describe or diagram the high-level Business Process to be automated.

- **Domain**: Matchmaking & Rating System
- **Business Process**: Once a player queues up for a match, the matchmaking system begins searching for suitable opponents. It uses the player's skill rating as the primary criterion, looking for other players with similar ratings. When a suitable group of players is found, the system creates a match. It then assigns each player to a team, trying to balance the overall skill level of both teams. After the match, recalculate rating base on the match result.
- **Actors**: Player, Game Server
- **Scope**: From the moment a player enters the queue to to all players' SR updated. Excludes in-game play.

**Process Diagram:**

```mermaid
flowchart TD
    X[Start] --> A

    A[Player enqueue] --> B[Add player to matchmaking pool]

    B --> C{Find suitable opponents?}

    C -- No --> D["Expand search range<br/>(wider SR range, wait more time)"]
    D --> C

    C -- Yes --> E[Select group of players]

    E --> F[Assign players to teams]

    F --> H1[Dequeue player from pool]

    H1 --> G[Create match]
    
    G --> I["Start match<br/>(out of scope)"]

    I --> J[Match ends]

    J --> K[Collect match result]

    K --> L["Recalculate player ratings<br/>(SR)"]

    L --> M[Update player SR]

    M --> N[End process]
```

### 1.2 Existing Automation Systems

List existing systems, databases, or legacy logic related to this process.

| System Name | Type | Current Role | Interaction Method |
|-------------|------|--------------|-------------------|
| Valve's MMR System |External Rating Service / Matchmaking Backend|Calculates player skill rating (MMR), supports matchmaking decisions, updates ratings after matches| RPC |
|Riot's MMR System| External Rating & Matchmaking Service | Determines hidden MMR, supports matchmaking, adjusts rating based on performance and match outcome| RPC |

> If none exist, state: *"None — the process is currently performed manually."*

### 1.3 Non-Functional Requirements

Non-functional requirements serve as input for identifying Utility Service and Microservice Candidates in step 2.7.

| Requirement    | Description |
|----------------|-------------|
| Performance    |             |
| Security       |             |
| Scalability    |             |
| Availability   |             |

---

## Part 2 — REST/Microservices Modeling

### 2.1 Decompose Business Process & 2.2 Filter Unsuitable Actions

Decompose the process from 1.1 into granular actions. Mark actions unsuitable for service encapsulation.

| # | Action | Actor | Description | Suitable? |
|---|--------|-------|-------------|-----------|
|1|Player queues for match|Player|Player sends request to join matchmaking queue|✅|
|2|Add player to matchmaking pool|Game Server|Store player as waiting with current SR and queue timestamp|✅|
|3|Find opponents in SR range|Game Server|Search waiting players with similar SR|✅|
|4|Expand search range over time|Game Server|Increase acceptable SR gap as wait time grows|✅|
|5|Initalize a match|Game Server|Lock a valid group of players for one match|✅|
|6|Create match record|Game Server|Generate match id and participant list|✅|
|7|Assign teams to balance SR|Game Server|Split players into teams with minimal SR difference|✅|
|8|Start in-game match session|Game Server|Launch actual gameplay session (out of assignment scope)|❌|
|9|Receive final match result|Game Server|Get winner/loser and per-player outcomes from game server|✅|
|10|Recalculate player ratings|Game Server|Apply rating algorithm (Glicko-2) after match result|✅|
|11|Update persisted player SR|Game Server|Save new ratings to rating datastore|✅|

> Actions marked ❌: manual-only, require human judgment, or cannot be encapsulated as a service.

### 2.3 Entity Service Candidates

Identify business entities and group reusable (agnostic) actions into Entity Service Candidates.

| Entity | Service Candidate | Agnostic Actions |
|--------|-------------------|------------------|
|Player|PlayerService|Get all player profiles, get player by id|
|Match|MatchService|Create match record, get match by id, update match status and result|
|Rating|RatingService|Get player rating, create player rating, update player rating|

### 2.4 Task Service Candidate

Group process-specific (non-agnostic) actions into a Task Service Candidate.

| Non-agnostic Action | Task Service Candidate |
|---------------------|------------------------|
|Initialize a match and start Matchmaking workflow|MatchmakingProcessService|


### 2.5 Identify Resources

Map entities/processes to REST URI Resources.

| Entity / Process | Resource URI |
|------------------|--------------|
|Player|/players|
|Match|/matches|
|Rating|/ratings|
|MatchmakingProcess|/mm|
|QueueProcess|/queue|

### 2.6 Associate Capabilities with Resources and Methods

| Service Candidate | Capability | Resource | HTTP Method |
|-------------------|------------|----------|-------------|
|PlayerService|List players       |/players|GET|
|PlayerService|Get player by id   |/players/{id}|GET|
|MatchService |Create match record|/matches|POST|
|MatchService |Get match by id|/matches/{id}|GET|
|MatchService |Submit match result / update status|/matches/{id}|PATCH|
|RatingService|Get player rating  |/ratings/{id}|GET|
|RatingService|Update player rating|/ratings/{id}/recalculate|PUT|
|QueueProcessService|Enqueue player|/queue|POST|
|QueueProcessService|Player and start searching opponents|/queue/{playerId}/stream|GET|
|QueueProcessService|Dequeue player|/queue|DELETE|
|MatchmakingProcessService|Initialize a match and start Matchmaking workflow|/mm|POST|



### 2.7 Utility Service & Microservice Candidates

Based on Non-Functional Requirements (1.3) and Processing Requirements, identify cross-cutting utility logic or logic requiring high autonomy/performance.

| Candidate | Type (Utility / Microservice) | Justification |
|-----------|-------------------------------|---------------|
|QueueProcessService|Microservice|Manages a high-churn waiting queue, requires atomic candidate selection, and benefits from independent scaling and low-latency access|

### 2.8 Service Composition Candidates

Interaction diagram showing how Service Candidates collaborate to fulfill the business process.

```mermaid
sequenceDiagram
    participant Client
    participant Gateway
    participant Broker
    participant QueueProcessService
    participant PlayerService
    participant RatingService
    participant MatchmakingProcessService
    participant MatchService
    participant GameServer as GameService  

    Client->>Gateway: GET /queue/{playerId}/stream
    QueueProcessService->>PlayerService: GET /player/{id}
    PlayerService-->>QueueProcessService: Return

    QueueProcessService->>RatingService: GET /rating/{id}
    RatingService-->>QueueProcessService: Return
    Gateway->>QueueProcessService: Open SSE stream
    QueueProcessService-->>Gateway: SSE stream established
    Gateway-->>Client: SSE connection established
    
    Gateway->>QueueProcessService: POST /queue
    QueueProcessService->>QueueProcessService: Store ticket as Waiting
    QueueProcessService-->>Gateway: 201 Created
    Gateway-->>Client: Queue ticket 


    loop While queue ticket is active
        QueueProcessService->>QueueProcessService: Search tickets within SR range
        QueueProcessService-->>Gateway: SSE event
        Gateway-->>Client: SSE data (Waiting / Removed)
    end

    alt valid party found
        QueueProcessService->>MatchmakingProcessService: POST /mm
        MatchmakingProcessService->>MatchService: POST /matches
        MatchService-->>MatchmakingProcessService: Return

        MatchmakingProcessService->>QueueProcessService: DELETE /queue
        MatchmakingProcessService->>Broker: MatchReady
        Broker-->>QueueProcessService: MatchReady
        QueueProcessService-->>MatchmakingProcessService: Return
        QueueProcessService-->>Gateway: Return
        Gateway-->>Client: SSE data (Matched + matchId)
        QueueProcessService-->>Gateway: SSE stream closed
        Gateway-->>Client: SSE connection closed
    else not enough players
        QueueProcessService->>QueueProcessService: Keep tickets waiting
    end

    Note over GameServer,MatchService: In-game session is out of scope and runs externally
    GameServer->>Broker: MatchEnded
    Broker-->>RatingService: MatchEnded
    RatingService->>RatingService: Recalculate rating and stored
    RatingService-->>Broker: RatingUpdated
    Broker-->>MatchmakingProcessService: RatingUpdated
    Broker-->>MatchService: MatchEnded
    MatchService->>MatchService: Update Match result
    MatchService-->>Broker: MatchUpdated
    Broker-->>MatchmakingProcessService: MatchUpdated
   
    Client->>Gateway: GET /matches/{matchId}
    Gateway->>MatchmakingProcessService: Open SSE stream
    loop wait for result
        MatchmakingProcessService-->>Gateway: SSE stream established
        Gateway-->>Client: Match result
    end
```

---

## Part 3 — Service-Oriented Design

> Part 3 is the **convergence point** — the service contracts and service logic below follow the resources and capabilities identified in Part 2.
<!-- > Internal saga commands used for coordination are part of the implementation detail, not the public REST contract. -->

### 3.1 Uniform Contract Design

Service contract specification for each service. The tables below reflect the public process-facing capabilities identified in Part 2; internal search, lock, confirm, and release mechanics remain implementation details.

Full OpenAPI specs:
- [docs/api-specs/player-service.yaml](docs/api-specs/player-service.yaml)
- [docs/api-specs/match-service.yaml](docs/api-specs/match-service.yaml)
- [docs/api-specs/queue-process-service.yaml](docs/api-specs/queue-process-service.yaml)
- [docs/api-specs/matchmaking-process-service.yaml](docs/api-specs/matchmaking-process-service.yaml)
- [docs/api-specs/rating-service.yaml](docs/api-specs/rating-service.yaml)

**PlayerService:**

| Endpoint | Method | Description | Request Body | Response Codes |
|----------|--------|-------------|--------------|----------------|
|/health|GET|Health check|None|200|
|/players|GET|List player profiles|None|200|
|/players/{id}|GET|Get player by id|None|200, 404|

**MatchService:**

| Endpoint | Method | Description | Request Body | Response Codes |
|----------|--------|-------------|--------------|----------------|
|/health|GET|Health check|None|200|
|/matches|POST|Create match record for a locked player group|Match create request|201, 400|
|/matches/{id}|GET|Get match by id|None|200, 404|
|/matches/{id}|PATCH|Submit match result and update status|Match result request|200, 400|

**QueueProcessService:**

| Endpoint | Method | Description | Request Body | Response Codes |
|----------|--------|-------------|--------------|----------------|
|/health|GET|Health check|None|200|
|/queue|POST|Enqueue player to matchmaking pool|QueueEnqueueRequest|201, 400, 404|
|/queue/{playerId}/stream|GET|Subscribe queue status via SSE|None|200, 404|
|/queue|DELETE|Dequeue player from pool|QueueDequeueRequest|204, 404|

The queue service also performs the search and lock/release loop that leads to match formation, but those steps are orchestrated internally rather than exposed as separate public REST endpoints.

**MatchmakingProcessService:**

| Endpoint | Method | Description | Request Body | Response Codes |
|----------|--------|-------------|--------------|----------------|
|/health|GET|Health check|None|200|
|/mm|POST|Initialize matchmaking from a locked player group|MatchInitRequest|202, 400, 409|

**RatingService:**

| Endpoint | Method | Description | Request Body | Response Codes |
|----------|--------|-------------|--------------|----------------|
|/health|GET|Health check|None|200|
|/ratings/{id}|GET|Get player rating|None|200, 404|
|/ratings/{id}/recalculate|PUT|Update player rating after match result|RatingUpdateRequest|200, 404|

### 3.2 Service Logic Design

Internal processing flow for each service, based on the current implementation.

**PlayerService:**

```mermaid
flowchart TD
    A["Receive GET /players or /players/{id}"] --> B{Request type?}
    B -->|GET /players| C[Read player profiles from store]
    C --> D[Return player list]
    B -->|"GET /players/{id}"| E[Validate identifier]
    E --> F[Read player profile from store]
    F --> G{Player found?}
    G -->|No| H[Return 404]
    G -->|Yes| I[Return player profile]
```

**MatchService:**

```mermaid
flowchart TD
    A[Receive POST /matches] --> B[Validate match create request]
    B --> C{Valid?}
    C -->|No| D[Return 400]
    C -->|Yes| E[Persist pending match]
    E --> F[Return 201 Created]
    G["Receive GET /matches/{id}"] --> H[Validate identifier]
    H --> I[Load match from store]
    I --> J{Match found?}
    J -->|No| K[Return 404]
    J -->|Yes| L[Return match record]
    M["Receive PATCH /matches/{id}"] --> N[Validate match result callback]
    N --> O{Valid?}
    O -->|No| P[Return 400]
    O -->|Yes| Q[Update result and status]
    Q --> R{Match found?}
    R -->|No| K
    R -->|Yes| S[Return updated match result]
```

**QueueProcessService:**

```mermaid
flowchart TD
    A[Receive POST /queue] --> B[Validate enqueue request]
    B --> C[Check player exists via PlayerService]
    C --> D[Check current rating via RatingService]
    D --> E[Upsert waiting ticket with SR snapshot]
    E --> F[Return 201 Created]
    G["Receive GET /queue/{playerId}/stream"] --> H[Open SSE stream]
    H --> I[Emit current queue state]
    J["Receive DELETE /queue"] --> K[Validate dequeue request]
    K --> L[Mark ticket removed]
    L --> M[Return 204 No Content]
    N[Background worker pass] --> O[Scan waiting tickets every interval]
    O --> P[Group by SR delta and queue time]
    P --> Q{Match group found?}
    Q -->|No| N
    Q -->|Yes| R[Request match initialization via POST /mm]
    R --> S{Workflow accepted?}
    S -->|No| N
    S -->|Yes| T[Receive queue status change]
    T --> U[Push SSE event: Matched + matchId]
    U --> V[Close SSE stream]
```

**MatchmakingProcessService:**

```mermaid
flowchart TD
    A[Receive POST /mm] --> B[Validate locked player group]
    B --> C[Request match creation from MatchService]
    C --> D{Match created?}
    D -->|No| E[Return 409 or 400]
    D -->|Yes| F[Request dequeue from QueueProcessService]
    F --> G{Queue updated?}
    G -->|No| E
    G -->|Yes| H[Publish MatchReady event]
    H --> I[Return 202 Accepted]
```

**RatingService:**

```mermaid
flowchart TD
    A["Receive GET /ratings/{id}"] --> B[Load rating from store]
    B --> C{Rating found?}
    C -->|No| D[Return 404]
    C -->|Yes| E[Return rating record]
    F["Receive PUT /ratings/{id}"] --> G[Validate rating update request]
    G --> H[Load current rating]
    H --> I[Apply Glicko-2 recalculation from match result]
    I --> J[Persist updated rating]
    J --> K[Return updated rating]
```
