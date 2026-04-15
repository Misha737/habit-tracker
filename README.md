# Practice 7 - Habit Tracker: Workflow / Saga + Kubernetes Deployment

**Stack:** .NET 8 · EF Core 8 · PostgreSQL · YARP · Kubernetes · Docker Compose

---

## What changed vs Practice 6

| Area | Practice 6 | Practice 7 |
|------|-----------|------------|
| `WorkflowService/` | — | New Saga/Process Manager service |
| `workflow_instances` table | — | State persistence for workflows |
| `habit_joinings` table | — | Stores user-habit joinings |
| Compensation path | — | Rollback on failure |
| `k8s/` folder | — | Kubernetes manifests |
| Gateway routes | 2 clusters | 3 clusters (+ workflow) |
| docker-compose.yml | 7 services | 8 services (+ workflow) |

---

## Architecture

```
Client
  |
  ▼ :8080
Gateway (YARP)
  |
  ├── /core/**  ──▶  core-api ──▶ core-db
  │                      │
  │                      │ publish (async)
  │                      ▼
  │                  RabbitMQ
  │                      │
  │                      ▼ consume
  │              notification-svc ──▶ notification-db
  │
  ├── /users/** ──▶ users-service ──▶ users-db
  │
  └── /workflows/** ──▶ workflow-service ──▶ workflow-db
                              │
                              ├── calls users-service
                              ├── calls core-api
                              └── calls notification-svc
```

---

## Workflow: Join Habit

Workflow type: `JoinHabit`

### Steps

1. **Started** - Workflow instance created
2. **UserValidated** - Verify user exists (UsersService)
3. **HabitValidated** - Verify habit exists (Core API)
4. **JoiningCreated** - Create joining record
5. **NotificationSent** - Send notification
6. **Completed** - Success

### Compensation Path

If any step fails after JoiningCreated:
- Transition to `Compensating` state
- Cancel the joining record
- Transition to `Compensated` state
- Store error in `last_error`

### State Machine

```
Started → UserValidated → HabitValidated → JoiningCreated → NotificationSent → Completed
                                              │
                                              ▼ (on failure)
                                        Compensating → Compensated
```

---

## Workflow Service API

### POST /workflows/join-habit

Start a new workflow to join a habit.

**Request:**
```json
{
  "userId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "habitId": "a1b2c3d4-e5f6-7890-abcd-ef1234567890"
}
```

**Response (201 Created - Success):**
```json
{
  "workflowId": "b2c3d4e5-f6a7-8901-bcde-f23456789012",
  "state": "Completed",
  "joiningId": "c3d4e5f6-a7b8-9012-cdef-345678901234"
}
```

**Response (200 OK - Compensated):**
```json
{
  "workflowId": "b2c3d4e5-f6a7-8901-bcde-f23456789012",
  "state": "Compensated",
  "joiningId": null
}
```

### GET /workflows/{workflowId}

Get workflow status.

**Response:**
```json
{
  "workflowId": "b2c3d4e5-f6a7-8901-bcde-f23456789012",
  "type": "JoinHabit",
  "state": "Completed",
  "userId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "habitId": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
  "joiningId": "c3d4e5f6-a7b8-9012-cdef-345678901234",
  "createdAt": "2024-04-07T10:00:00Z",
  "updatedAt": "2024-04-07T10:00:05Z",
  "lastError": null
}
```

---

## How to Run (Docker Compose)

```bash
docker compose up --build
```

Start order: databases → rabbitmq → users-service → core-api → notification-svc → workflow-service → gateway

---

## Kubernetes Deployment

### Prerequisites

- Kubernetes cluster (minikube, kind, or cloud)
- kubectl configured
- Docker images built and available

### Build Images

```bash
# Build all images
docker build -t habit-tracker/core-api:latest -f src/Api/Dockerfile .
docker build -t habit-tracker/users-service:latest -f src/UsersService/Dockerfile .
docker build -t habit-tracker/notification-service:latest -f src/NotificationService/Dockerfile .
docker build -t habit-tracker/workflow-service:latest -f src/WorkflowService/Dockerfile .
docker build -t habit-tracker/gateway:latest -f src/Gateway/Dockerfile .
```

### Apply Manifests

```bash
# Apply all manifests
kubectl apply -f k8s/

# Or apply individually
kubectl apply -f k8s/namespace.yaml
kubectl apply -f k8s/configmap.yaml
kubectl apply -f k8s/secret.yaml
kubectl apply -f k8s/databases.yaml
kubectl apply -f k8s/rabbitmq.yaml
kubectl apply -f k8s/users-service.yaml
kubectl apply -f k8s/core-api.yaml
kubectl apply -f k8s/notification-service.yaml
kubectl apply -f k8s/workflow-service.yaml
kubectl apply -f k8s/gateway.yaml
kubectl apply -f k8s/ingress.yaml
```

### Verify Deployment

```bash
# Check namespace
kubectl get namespace habit-tracker

# Check pods
kubectl get pods -n habit-tracker

# Check services
kubectl get svc -n habit-tracker

# Check persistent volumes
kubectl get pvc -n habit-tracker

# View logs
kubectl logs -n habit-tracker deployment/workflow-service

# Port forward gateway
kubectl port-forward -n habit-tracker svc/gateway 8080:8080
```

---

## Verification Steps

### 1. Check all services are healthy

```bash
# Docker Compose
curl http://localhost:8080/health

# Kubernetes (with port-forward)
kubectl port-forward -n habit-tracker svc/gateway 8080:8080 &
curl http://localhost:8080/health
```

### 2. Create a user

```bash
curl -X POST http://localhost:8080/users \
  -H "Content-Type: application/json" \
  -d '{"displayName":"Ivan","email":"ivan@test.com"}'
# → {"id":"<USER_ID>", ...}
```

### 3. Create a habit

```bash
curl -X POST http://localhost:8080/core/core-items \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Morning Run",
    "description": "5km before work",
    "frequencyPerWeek": 5,
    "ownerUserId": "<USER_ID>"
  }'
# → 201 Created, note the habit ID
```

### 4. Test Workflow Success Path

```bash
# Start workflow
curl -X POST http://localhost:8080/workflows/join-habit \
  -H "Content-Type: application/json" \
  -d '{
    "userId": "<USER_ID>",
    "habitId": "<HABIT_ID>"
  }'
# → 201 Created with workflowId and joiningId

# Check workflow status
curl http://localhost:8080/workflows/<WORKFLOW_ID>
# → state: "Completed"

# Verify joining was created
curl http://localhost:8080/joinings/<JOINING_ID>
# → status: "Active"
```

### 5. Test Compensation Path

```bash
# Try to join with non-existent user (triggers compensation)
curl -X POST http://localhost:8080/workflows/join-habit \
  -H "Content-Type: application/json" \
  -d '{
    "userId": "00000000-0000-0000-0000-000000000000",
    "habitId": "<HABIT_ID>"
  }'
# → 200 OK (workflow started)

# Check workflow status
curl http://localhost:8080/workflows/<WORKFLOW_ID>
# → state: "Compensated", lastError: "Failed at Step1"
```

### 6. Test with non-existent habit

```bash
# Try to join with non-existent habit
curl -X POST http://localhost:8080/workflows/join-habit \
  -H "Content-Type: application/json" \
  -d '{
    "userId": "<USER_ID>",
    "habitId": "00000000-0000-0000-0000-000000000000"
  }'

# Check workflow status
curl http://localhost:8080/workflows/<WORKFLOW_ID>
# → state: "Compensated", lastError: "Failed at Step2"
```

---

## Kubernetes Manifests Structure

```
k8s/
├── namespace.yaml          # habit-tracker namespace
├── configmap.yaml          # Non-sensitive configuration
├── secret.yaml             # Sensitive data (passwords)
├── databases.yaml          # 4 PostgreSQL StatefulSets
├── rabbitmq.yaml           # RabbitMQ Deployment
├── users-service.yaml      # Users Service Deployment
├── core-api.yaml           # Core API Deployment
├── notification-service.yaml # Notification Service Deployment
├── workflow-service.yaml   # Workflow Service Deployment
├── gateway.yaml            # Gateway Deployment + NodePort
└── ingress.yaml            # Ingress (optional)
```

### Resource Limits

Each service has:
- **Requests:** 128Mi memory, 100m CPU
- **Limits:** 512Mi memory, 500m CPU

### Probes

All services include:
- **Readiness probe:** HTTP GET /health, initialDelay: 5s
- **Liveness probe:** HTTP GET /health, initialDelay: 10s

Databases include:
- **Readiness probe:** pg_isready
- **Liveness probe:** pg_isready

---

## Troubleshooting

| Problem | Cause | Fix |
|---------|-------|-----|
| `ImagePullBackOff` | Image not found | Build and tag images correctly |
| `CrashLoopBackOff` | App crashes on start | Check logs: `kubectl logs <pod>` |
| `Pending` pods | Insufficient resources | Increase node resources or reduce limits |
| Workflow stuck | Service unreachable | Verify service names in ConfigMap |
| PVC pending | No storage class | Check `kubectl get storageclass` |

---

## Workflow State Persistence

The `workflow_instances` table stores:

| Column | Type | Description |
|--------|------|-------------|
| workflow_id | UUID | Primary key |
| type | VARCHAR(50) | Workflow type (JoinHabit) |
| state | VARCHAR(50) | Current state |
| created_at | TIMESTAMP | Creation time |
| updated_at | TIMESTAMP | Last update time |
| last_error | TEXT | Error message if failed |
| user_id | UUID | Target user |
| habit_id | UUID | Target habit |
| joining_id | UUID | Created joining (if any) |

---

## Cleanup

```bash
# Docker Compose
docker compose down -v

# Kubernetes
kubectl delete namespace habit-tracker
```
