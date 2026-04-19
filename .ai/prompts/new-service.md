# Prompt: Scaffold a New Microservice

Use this prompt to have an AI tool generate a complete microservice scaffold.

---

## Prompt


---

## Example Usage

Replace placeholders:
- `[SERVICE_NAME]` → `user-service`
- `[LANGUAGE]` → `Python`
- `[FRAMEWORK]` → `FastAPI`
- `[ENTITY_NAME]` → `User`
- `[ENTITY_PLURAL]` → `users`


```
Create a new microservice called PlayerService using C# and .NET 9.

Location: services/G2mm.Entity.PlayerService/

Requirements:
1. Project structure following .NET best practices
2. Dockerfile with multi-stage build
3. GET /health endpoint returning {"status": "ok"}
4. Basic endpoints for Player:
   - GET /player — list all
   - GET /player/{id} — get by ID
5. Database connection using environment variables (look up in architechture doc)
6. Input validation and error handling
7. OpenAPI spec in docs/api-specs/player-service.yaml
8. Update docker-compose.yml to include this service
9. Service readme.md with setup instructions

The service should:
- Listen on port that match architecture docs inside the container
- Use environment variables from .env
- Be accessible via the API gateway
- Follow RESTful conventions
```

Update: 

```
Update microservice called Task.QueueProcessService using .NET 9.

Location: services/Task.QueueProcessService/

Requirements:
1. Project structure following existed structure
2. Dockerfile with multi-stage build
3. GET /health endpoint returning {"status": "ok"}
4. Database connection using environment variables in docs/architecture.md (DB_HOST, DB_PORT, DB_NAME, DB_USER, DB_PASSWORD)
5. Logic of workflow follows docs/analysis-and-design.md
6. Input validation and error handling
7. OpenAPI spec in docs/api-specs/player-service.yaml
8. Update docker-compose.yml to include this service
9. Service readme.md with setup instructions
10. Clean any residuals, unused code

The service should:
- Listen on port included in docs/architecture.md inside the container
- Use environment variables from .env
- Be accessible via the API gateway
- Follow RESTful conventions
- Code clean, easy to understand
```