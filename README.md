# Practice 8 - Habit Tracker: Production Hardening, Scaling & Observability

**Stack:** .NET 8 · EF Core 8 · PostgreSQL · YARP · Kubernetes · Docker Compose · Polly

---

## What changed vs Practice 7

| Area              | Practice 7    | Practice 8                                             |
| ----------------- | ------------- | ------------------------------------------------------ |
| Correlation ID    | —             | End-to-end propagation (Gateway → Services → RabbitMQ) |
| Resilience        | Basic timeout | Polly retry (2x) + timeout policies                    |
| Core API replicas | 1             | 3 (with HPA 3-6)                                       |
| Gateway replicas  | 1             | 2 (with HPA 2-4)                                       |
| RollingUpdate     | —             | Configured for zero-downtime deployments               |
| Resource limits   | 512Mi/500m    | Tuned per service (256Mi/300m app, 512Mi/500m DB)      |
| Observability     | Basic logs    | Structured logs with CorrelationId scope               |

---

## Architecture

```
Client
  |
  ▼ :8080
Gateway (YARP) [2 replicas]
  |
  ├── /core/**  ──▶  core-api [3 replicas] ──▶ core-db
  │                      │
  │                      │ publish (async) with correlation_id header
  │                      ▼
  │                  RabbitMQ
  │                      │
  │                      ▼ consume
  │              notification-svc [2 replicas] ──▶ notification-db
  │
  ├── /users/** ──▶ users-service [2 replicas] ──▶ users-db
  │
  └── /workflows/** ──▶ workflow-service [2 replicas] ──▶ workflow-db
                              │
                              ├── calls users-service (with Polly retry)
                              ├── calls core-api (with Polly retry)
                              └── calls notification-svc (with Polly retry)
```

---

## Correlation ID Flow

```
┌─────────┐     ┌──────────┐     ┌──────────┐     ┌─────────┐
│  Client │────▶│  Gateway │────▶│ Core API │────▶│ RabbitMQ│
│         │     │(generate │     │(forward) │     │(header) │
│         │     │ or use)  │     │          │     │         │
└─────────┘     └──────────┘     └──────────┘     └─────────┘
                                         │               │
                                         ▼               ▼
                                   ┌──────────┐     ┌─────────────┐
                                   │Users Svc │     │Notification │
                                   │(forward) │     │   Service   │
                                   └──────────┘     └─────────────┘
```

Every request:

- If `X-Correlation-Id` header exists → use it
- If not → generate new UUID
- Forward to downstream services via HTTP headers
- Include in RabbitMQ message headers
- Return in response header
- Include in all log entries via `ILogger.BeginScope`

---

## Resilience with Polly

### Core API → Users Service

```csharp
AddPolicyHandler(GetRetryPolicy())   // 2 retries with exponential backoff
AddPolicyHandler(GetTimeoutPolicy()) // 5 second timeout

static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy() =>
    HttpPolicyExtensions
        .HandleTransientHttpError()
        .WaitAndRetryAsync(2, retryAttempt =>
            TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));

static IAsyncPolicy<HttpResponseMessage> GetTimeoutPolicy() =>
    Policy.TimeoutAsync<HttpResponseMessage>(TimeSpan.FromSeconds(5));
```

### Workflow Service → All Dependencies

Same pattern applied to:

- Users Service calls
- Core API calls
- Notification Service calls

Timeout: 30 seconds (longer for saga operations)

### HTTP Status Codes

| Code | Meaning             | Trigger                                       |
| ---- | ------------------- | --------------------------------------------- |
| 503  | Service Unavailable | Dependency unreachable (HttpRequestException) |
| 504  | Gateway Timeout     | Dependency timeout (TaskCanceledException)    |
| 502  | Bad Gateway         | Dependency returns error status               |

---

## Kubernetes Scaling

### Core API Deployment

```yaml
spec:
  replicas: 3
  strategy:
    type: RollingUpdate
    rollingUpdate:
      maxSurge: 1
      maxUnavailable: 0
```

### Horizontal Pod Autoscaler (HPA)

```yaml
spec:
  minReplicas: 3
  maxReplicas: 6
  metrics:
    - type: Resource
      resource:
        name: cpu
        target:
          averageUtilization: 70
    - type: Resource
      resource:
        name: memory
        target:
          averageUtilization: 80
```

### Resource Limits

| Service          | Requests     | Limits       |
| ---------------- | ------------ | ------------ |
| core-api         | 128Mi / 100m | 256Mi / 300m |
| gateway          | 128Mi / 100m | 256Mi / 300m |
| users-service    | 128Mi / 100m | 256Mi / 300m |
| notification-svc | 128Mi / 100m | 256Mi / 300m |
| workflow-service | 128Mi / 100m | 256Mi / 300m |
| \*-db            | 128Mi / 100m | 512Mi / 500m |
| rabbitmq         | 256Mi / 100m | 512Mi / 500m |

### Probes

All services:

- **Readiness**: HTTP GET /health, initialDelay: 5s, period: 5s
- **Liveness**: HTTP GET /health, initialDelay: 10s, period: 10s
- **FailureThreshold**: 3

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
docker build -t habit-tracker/core-api:latest -f src/Api/Dockerfile .
docker build -t habit-tracker/users-service:latest -f src/UsersService/Dockerfile .
docker build -t habit-tracker/notification-service:latest -f src/NotificationService/Dockerfile .
docker build -t habit-tracker/workflow-service:latest -f src/WorkflowService/Dockerfile .
docker build -t habit-tracker/gateway:latest -f src/Gateway/Dockerfile .
```

### Apply Manifests

```bash
kubectl apply -f k8s/
```

### Verify Deployment

```bash
# Check namespace
kubectl get namespace habit-tracker

# Check pods
kubectl get pods -n habit-tracker

# Check services
kubectl get svc -n habit-tracker

# Check HPA
kubectl get hpa -n habit-tracker

# View logs with correlation ID
kubectl logs -n habit-tracker -l app=core-api --tail=50

# Port forward gateway
kubectl port-forward -n habit-tracker svc/gateway 8080:8080
```

---

## Scaling Operations

### Manual Scale

```bash
# Scale core-api to 5 replicas
kubectl scale deployment core-api --replicas=5 -n habit-tracker

# Verify
kubectl get pods -n habit-tracker -l app=core-api
```

### Rolling Update

```bash
# Update image
kubectl set image deployment/core-api core-api=habit-tracker/core-api:v2 -n habit-tracker

# Watch rollout
kubectl rollout status deployment/core-api -n habit-tracker

# Verify new pods
kubectl get pods -n habit-tracker -l app=core-api
```

### Rollback

```bash
# Undo last rollout
kubectl rollout undo deployment/core-api -n habit-tracker

# Rollback to specific revision
kubectl rollout undo deployment/core-api --to-revision=2 -n habit-tracker

# View rollout history
kubectl rollout history deployment/core-api -n habit-tracker
```

---

## Verification Steps

### 1. Check all services are healthy

```bash
kubectl port-forward -n habit-tracker svc/gateway 8080:8080 &
curl http://localhost:8080/health
```

### 2. Verify Correlation ID propagation

```bash
# Create request with custom correlation ID
curl -X POST http://localhost:8080/core/core-items \
  -H "Content-Type: application/json" \
  -H "X-Correlation-Id: test-correlation-123" \
  -d '{
    "name": "Test Habit",
    "description": "Test",
    "frequencyPerWeek": 3,
    "ownerUserId": "3fa85f64-5717-4562-b3fc-2c963f66afa6"
  }' -v

# Check response header X-Correlation-Id: test-correlation-123
```

### 3. Check logs for CorrelationId

```bash
# View logs from multiple pods
kubectl logs -n habit-tracker -l app=core-api --tail=20

# Should see: [CorrelationId:test-correlation-123] Habit ... created
```

### 4. Test retry behavior

```bash
# Scale down users-service to simulate failure
kubectl scale deployment users-service --replicas=0 -n habit-tracker

# Make request (should retry and eventually fail with 503)
curl -X POST http://localhost:8080/core/core-items \
  -H "Content-Type: application/json" \
  -d '{...}' -w "\nHTTP Status: %{http_code}\n"

# Scale back up
kubectl scale deployment users-service --replicas=2 -n habit-tracker
```

### 5. Verify multiple replicas

```bash
# Check multiple pods are running
kubectl get pods -n habit-tracker -l app=core-api

# NAME                        READY   STATUS
core-api-7d9f4b8c5a-abc12   1/1     Running
core-api-7d9f4b8c5a-def34   1/1     Running
core-api-7d9f4b8c5a-ghi56   1/1     Running
```

### 6. Test HPA (if metrics server available)

```bash
# Generate load
for i in {1..100}; do
  curl -s http://localhost:8080/health > /dev/null &
done

# Watch HPA scale up
kubectl get hpa core-api-hpa -n habit-tracker -w
```

---

## Kubernetes Manifests Structure

```
k8s/
├── namespace.yaml              # habit-tracker namespace
├── configmap.yaml              # Non-sensitive configuration
├── secret.yaml                 # Sensitive data (passwords)
├── databases.yaml              # 4 PostgreSQL StatefulSets
├── rabbitmq.yaml               # RabbitMQ Deployment
├── users-service.yaml          # Users Service Deployment + HPA
├── core-api.yaml               # Core API Deployment + HPA
├── notification-service.yaml   # Notification Service Deployment + HPA
├── workflow-service.yaml       # Workflow Service Deployment + HPA
├── gateway.yaml                # Gateway Deployment + HPA
└── ingress.yaml                # Ingress (optional)
```

---

## Troubleshooting

| Problem                | Cause                     | Fix                                                      |
| ---------------------- | ------------------------- | -------------------------------------------------------- |
| `ImagePullBackOff`     | Image not found           | Build and tag images correctly                           |
| `CrashLoopBackOff`     | App crashes on start      | Check logs: `kubectl logs <pod>`                         |
| `Pending` pods         | Insufficient resources    | Increase node resources or reduce limits                 |
| HPA not scaling        | Metrics server missing    | Install metrics-server: `minikube addons enable metrics` |
| Correlation ID missing | Middleware not registered | Check `app.UseCorrelationId()` in Program.cs             |
| Retry not working      | Polly not configured      | Verify `AddPolicyHandler` calls                          |

---

## Architecture Progression

| Practice       | Focus                                                          |
| -------------- | -------------------------------------------------------------- |
| Practice 4     | Modular Monolith                                               |
| Practice 5     | Microservice Extraction                                        |
| Practice 6     | Event-Driven Communication                                     |
| Practice 7     | Workflow + Kubernetes                                          |
| **Practice 8** | **Production Hardening (Correlation ID, Resilience, Scaling)** |

Your system is now production-ready with:

- End-to-end observability via Correlation IDs
- Resilient inter-service communication
- Horizontal scaling with HPA
- Zero-downtime deployments
- Proper resource management

---

## Cleanup

```bash
# Docker Compose
docker compose down -v

# Kubernetes
kubectl delete namespace habit-tracker
```
