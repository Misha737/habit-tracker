# Practice 5 — Habit Tracker

**Stack:** .NET 8 · ASP.NET Core Minimal API · EF Core 8 · YARP · PostgreSQL · Docker Compose

---

## What changed vs Practice 4

| File / Area                | Practice 4        | Practice 5                                   |
| -------------------------- | ----------------- | -------------------------------------------- |
| `Habit.OwnerId`            | `string OwnerId`  | `Guid OwnerUserId`                           |
| Owner validation           | none              | HTTP call to UsersService                    |
| `InfrastructureExtensions` | registers DB only | also registers `HttpClient` for UsersService |
| Databases                  | 1 shared Postgres | `core-db` + `users-db` (separate)            |
| New projects               | —                 | `UsersService`, `Gateway`                    |
| New migration              | —                 | `20240402_AddOwnerUserId`                    |

All existing `Modules.Core.*` namespaces and project names are **unchanged**.

---

## Architecture

```
Client
  │
  ▼ :8080
┌──────────────────────────────┐
│  Gateway  (YARP)             │
│  + generates X-Correlation-Id│
└────────────┬─────────────────┘
             │
     ┌───────┴────────┐
     │                │
     ▼                ▼
┌──────────┐    ┌──────────────┐
│ core-api │    │users-service │
│(src/Api) │    │(src/Users    │
│          │───▶│  Service)    │
│ Modules  │ GET /users/{id}   │
│ .Core.*  │    │              │
└────┬─────┘    └──────┬───────┘
     │                 │
  core-db          users-db
```

**Gateway routes:**

| Client calls    | Forwarded to            |
| --------------- | ----------------------- |
| `GET /core/**`  | `core-api:8080/**`      |
| `GET /users/**` | `users-service:8080/**` |

**Cross-service call (Core → Users):**
When `POST /core-items` is received, Core calls `GET http://users-service:8080/users/{ownerUserId}`:

- **404** → `400 Bad Request` to client
- **Unreachable / timeout** → `503 Service Unavailable` to client

---

## How to Run (Docker Compose)

```bash
docker compose up --build
# Gateway available at http://localhost:8080
# Migrations run automatically on startup
```

Start order enforced by healthchecks:
`core-db` + `users-db` → `users-service` → `core-api` → `gateway`

---

## How to Run Locally (without Docker)

```bash
cd src/UsersService
export ConnectionStrings__UsersDb="Host=localhost;Database=usersdb;Username=postgres;Password=postgres"
dotnet run --urls http://localhost:5001

cd src/Api
export ConnectionStrings__CoreDb="Host=localhost;Database=coredb;Username=postgres;Password=postgres"
export UsersService__BaseUrl="http://localhost:5001"
dotnet run --urls http://localhost:5000

cd src/Gateway
dotnet run --urls http://localhost:8080
# (Edit appsettings.json clusters to point at localhost:5000 / localhost:5001)
```

---

## API Examples (curl via Gateway on :8080)

### Health

```bash
curl http://localhost:8080/health
# {"service":"gateway","status":"healthy"}
```

### Create a User

```bash
curl -X POST http://localhost:8080/users \
  -H "Content-Type: application/json" \
  -d '{"displayName":"Ivan Petrenko","email":"ivan@example.com"}'
# 201 Created → {"id":"<USER_ID>","displayName":"Ivan Petrenko",...}
```

### Get a User

```bash
curl http://localhost:8080/users/<USER_ID>
# 200 OK  |  404 Not Found
```

### Create a Habit (valid owner)

```bash
curl -X POST http://localhost:8080/core/core-items \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Morning Run",
    "description": "5km before work",
    "frequencyPerWeek": 5,
    "ownerUserId": "<USER_ID>"
  }'
# 201 Created
```

### Create a Habit (owner does not exist → 400)

```bash
curl -X POST http://localhost:8080/core/core-items \
  -H "Content-Type: application/json" \
  -d '{"name":"Run","frequencyPerWeek":3,"ownerUserId":"00000000-0000-0000-0000-000000000000"}'
# 400 Bad Request → {"error":"User '00000000-...' not found."}
```

### Change Habit Status

```bash
curl -X PATCH http://localhost:8080/core/core-items/<HABIT_ID>/status \
  -H "Content-Type: application/json" \
  -d '{"status":"Paused"}'
```

### Correlation ID

```bash
curl -v http://localhost:8080/health 2>&1 | grep X-Correlation-Id

curl -H "X-Correlation-Id: my-trace-42" http://localhost:8080/users/<ID>
```

---

## When UsersService Is Down

```bash
docker compose stop users-service

curl -X POST http://localhost:8080/core/core-items \
  -H "Content-Type: application/json" \
  -d '{"name":"Run","frequencyPerWeek":3,"ownerUserId":"<any-uuid>"}'
```

---

## Run Tests

```bash
dotnet test
```

---

## Data Ownership

| Service         | Database   | Table    | Must NOT touch |
| --------------- | ---------- | -------- | -------------- |
| `core-api`      | `core-db`  | `habits` | `usersdb`      |
| `users-service` | `users-db` | `users`  | `coredb`       |
| `gateway`       | —          | —        | both           |

Communication between services: **HTTP only**, never shared DB.
