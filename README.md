# Practice 6 — Habit Tracker: Event-Driven Communication

**Stack:** .NET 8 · MassTransit 8 · RabbitMQ 3 · EF Core 8 · PostgreSQL · YARP · Docker Compose

---

## What changed vs Practice 5

| Area                                             | Practice 5              | Practice 6                                            |
| ------------------------------------------------ | ----------------------- | ----------------------------------------------------- |
| `Shared/`                                        | empty                   | `CoreItemCreatedEvent` contract                       |
| `Modules.Core.Application`                       | `IUserValidationClient` | + `IEventPublisher` port                              |
| `Modules.Core.Application/Services/HabitService` | creates habit           | + publishes `CoreItemCreatedEvent`                    |
| `Modules.Core.Infrastructure`                    | EF + HttpClient         | + MassTransit publisher adapter                       |
| New service                                      | —                       | `NotificationService` (consumer + DB)                 |
| `docker-compose.yml`                             | 5 containers            | + `rabbitmq` + `notification-db` + `notification-svc` |

---

## Architecture

```
Client
  │
  ▼ :8080
Gateway (YARP)
  │
  ├── /core/**  ──▶  core-api ──▶ core-db
  │                      │
  │                      │ publish (async)
  │                      ▼
  │                  RabbitMQ
  │                      │
  │                      ▼ consume
  │              notification-svc ──▶ notification-db
  │
  └── /users/** ──▶ users-service ──▶ users-db
```

---

## Event Contract

**Event name:** `CoreItemCreatedEvent`
**Exchange:** `habit-tracker:notification-service` (MassTransit default fanout topology)
**Queue:** `notification-service`
**Routing key:** MassTransit default

### Payload

```json
{
  "eventId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "occurredAt": "2024-04-01T10:00:00Z",
  "correlationId": "7f3a1c2e-...",
  "coreItemId": "a1b2c3d4-...",
  "ownerUserId": "b2c3d4e5-...",
  "summary": "Habit 'Morning Run' created"
}
```

---

## Idempotency

`event_id` is the **Primary Key** in the `notifications` table — unique constraint at DB level.

Two-layer protection:

1. **Application check** — `ExistsByEventIdAsync()` before insert (fast path)
2. **DB constraint** — PK violation caught and logged (race condition safety net)

If the same event arrives twice (retry, restart, duplicate delivery) the second one is silently skipped.

---

## How to Run

```bash
docker compose up --build
```

Start order: databases → rabbitmq → users-service → core-api → notification-svc → gateway

---

## Verification Steps

### 1. Check all services are healthy

```bash
curl http://localhost:8080/health
# gateway healthy

docker compose ps
# all containers: healthy
```

### 2. Create a user

```bash
curl -X POST http://localhost:8080/users \
  -H "Content-Type: application/json" \
  -d '{"displayName":"Ivan","email":"ivan@test.com"}'
# → {"id":"<USER_ID>", ...}
```

### 3. Create a habit — triggers the event

```bash
curl -X POST http://localhost:8080/core/core-items \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Morning Run",
    "description": "5km before work",
    "frequencyPerWeek": 5,
    "ownerUserId": "<USER_ID>"
  }'
# → 201 Created
```

### 4. Verify notification was stored

```bash
curl http://localhost:8080/core/notifications
# This hits notification-svc directly via... wait, it's not routed through gateway.
# Call it directly (only during local dev, not exposed in prod):
docker compose exec notification-svc curl -s http://localhost:8080/notifications
# OR add a route in Gateway for /notifications/** → notification-svc

# → [{
#     "eventId": "...",
#     "correlationId": "...",
#     "coreItemId": "...",
#     "ownerUserId": "...",
#     "summary": "Habit 'Morning Run' created",
#     "createdAt": "2024-04-01T10:00:05Z"
#   }]
```

### 5. Open RabbitMQ Management UI

```
http://localhost:15672
username: guest
password: guest
```

In the UI verify:

- **Exchanges** tab: find `NotificationService:CoreItemCreatedEvent` (or similar MassTransit name)
- **Queues** tab: find `notification-service` queue
- **Message rates**: spike when you POST a new habit

### 6. Verify idempotency — send same event twice

MassTransit handles this automatically on retry, but to test manually:

```bash
# Create the same habit name again for a different user:
curl -X POST http://localhost:8080/core/core-items \
  -d '{"name":"Morning Run","frequencyPerWeek":5,"ownerUserId":"<USER_ID_2>"}'

# Check notification-svc logs for "Duplicate event" warnings:
docker compose logs notification-svc | grep -i duplicate
```

---

## Troubleshooting

| Problem                             | Cause                    | Fix                                                                 |
| ----------------------------------- | ------------------------ | ------------------------------------------------------------------- |
| `core-api` fails to start           | RabbitMQ not ready yet   | Increase `retries` in healthcheck or add `restart: on-failure`      |
| No messages in queue                | Exchange/queue not bound | Check RabbitMQ UI → Bindings tab                                    |
| `notification-svc` skips all events | Duplicate EventId        | Check logs for "already processed" — restart clears in-memory state |
| `Connection refused` to RabbitMQ    | Wrong host name          | Ensure `RabbitMQ__Host=rabbitmq` (service name, not localhost)      |

---

## Exchange / Queue Naming

MassTransit uses **message type full name** for exchange names by default:

| Type     | Name in RabbitMQ                        |
| -------- | --------------------------------------- |
| Exchange | `Shared.Contracts:CoreItemCreatedEvent` |
| Queue    | `notification-service`                  |
| Binding  | exchange → queue (automatic)            |
