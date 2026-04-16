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
- **Actors**: Player
- **Scope**: From the moment a player enters the queue to to all players' SR updated. Excludes in-game play.

**Process Diagram:**

```mermaid
flowchart TD
    X[Start] --> A

    A[Player enters queue] --> B[Add player to matchmaking pool]

    B --> C{Find suitable opponents?}

    C -- No --> D["Expand search range<br/>(wider SR range, wait more time)"]
    D --> C

    C -- Yes --> E[Select group of players]

    E --> F[Assign players to teams]

    F --> H{Teams balanced?}

    H -- No --> G2[Reassign teams to balance SR]
    G2 --> H

    H -- Yes --> G[Create match]
    
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
| Valves MMR System |External Rating Service / Matchmaking Backend|Calculates player skill rating (MMR), supports matchmaking decisions, updates ratings after matches|API calls (internal service endpoints), event-driven updates after match results|
|Riot's MMR System| External Rating & Matchmaking Service | Determines hidden MMR, supports matchmaking, adjusts rating based on performance and match outcome| API calls|

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
|Player|PlayerService|Get player profiles, get player by id, get player by status, update player status by id|
|Match|MatchService|Create match record, get match by id, update match status and result|
|Rating|RatingService|Get player rating, update player rating|

### 2.4 Task Service Candidate

Group process-specific (non-agnostic) actions into a Task Service Candidate.

| Non-agnostic Action | Task Service Candidate |
|---------------------|------------------------|
|Initalize a match|MatchmakingProcess|


### 2.5 Identify Resources

Map entities/processes to REST URI Resources.

| Entity / Process | Resource URI |
|------------------|--------------|
|Player|/player|
|Match|/match|
|Rating|/rating|
|Queue|/queue|
|MatchmakingProcess|/mm|
|Glicko2Rating|/glicko2|

### 2.6 Associate Capabilities with Resources and Methods

| Service Candidate | Capability | Resource | HTTP Method |
|-------------------|------------|----------|-------------|




### 2.7 Utility Service & Microservice Candidates

Based on Non-Functional Requirements (1.3) and Processing Requirements, identify cross-cutting utility logic or logic requiring high autonomy/performance.

| Candidate | Type (Utility / Microservice) | Justification |
|-----------|-------------------------------|---------------|
|Glicko2RatingService|Microservice|Calculate player rating using Glicko2 model|

### 2.8 Service Composition Candidates

Interaction diagram showing how Service Candidates collaborate to fulfill the business process.

```mermaid
sequenceDiagram
    participant Client
    participant TaskService
    participant EntityServiceA
    participant EntityServiceB
    participant UtilityService

    Client->>TaskService: (fill in)
    TaskService->>EntityServiceA: (fill in)
    EntityServiceA-->>TaskService: (fill in)
    TaskService->>EntityServiceB: (fill in)
    EntityServiceB-->>TaskService: (fill in)
    TaskService-->>Client: (fill in)
```

---

## Part 3 — Service-Oriented Design

> Part 3 is the **convergence point** — regardless of whether you used Step-by-Step Action or DDD in Part 2, the outputs here are the same: service contracts and service logic.

### 3.1 Uniform Contract Design

Service Contract specification for each service. Full OpenAPI specs:
- [`docs/api-specs/service-a.yaml`](api-specs/service-a.yaml)
- [`docs/api-specs/service-b.yaml`](api-specs/service-b.yaml)

> 💡 **Derive from Part 2:** Each service capability from 2.6 maps to one API endpoint. Update the OpenAPI spec files to match.

**Service A — *(service name)*:**

| Endpoint | Method | Description | Request Body | Response Codes |
|----------|--------|-------------|--------------|----------------|
|          |        |             |              |                |

**Service B — *(service name)*:**

| Endpoint | Method | Description | Request Body | Response Codes |
|----------|--------|-------------|--------------|----------------|
|          |        |             |              |                |

### 3.2 Service Logic Design

Internal processing flow for each service.

**Service A:**

```mermaid
flowchart TD
    A[Receive Request] --> B{Validate?}
    B -->|Valid| C[(Process / DB)]
    B -->|Invalid| D[Return 4xx Error]
    C --> E[Return Response]
```

**Service B:**

```mermaid
flowchart TD
    A[Receive Request] --> B{Validate?}
    B -->|Valid| C[(Process / DB)]
    B -->|Invalid| D[Return 4xx Error]
    C --> E[Return Response]
```
