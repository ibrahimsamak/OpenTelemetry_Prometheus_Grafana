# ASPCore_Log_project

An ASP.NET Core Razor Pages CRUD application instrumented with OpenTelemetry and wired to a
full local observability stack (OpenTelemetry Collector → Prometheus, Tempo, Loki → Grafana).

The app manages **Orders** and runs a small **order-fulfillment saga** (reserve inventory →
charge payment → ship, with compensation on failure) so there are realistic traces, metrics
and logs to explore.

## Stack

| Concern | Component |
|---|---|
| Web app | ASP.NET Core Razor Pages (.NET 9) |
| Persistence | EF Core + SQLite |
| Instrumentation | OpenTelemetry (traces, metrics, logs) via OTLP |
| Collector | OpenTelemetry Collector (contrib) |
| Metrics | Prometheus |
| Traces | Tempo |
| Logs | Loki |
| Dashboards | Grafana |

## Project layout

```
CrudApp/                     ASP.NET Core application
  Models/                    Order entity + status enum
  Data/                      EF Core DbContext and registration
  Telemetry/                 Custom ActivitySource, Meter and instruments
  Services/                  Order saga + load simulator
  Pages/Orders/              CRUD Razor Pages
observability/               Collector, Prometheus, Tempo, Loki, Grafana config
docker-compose.yml           Local stack
Dockerfile                   Container image for the app
```

## Run the observability stack

```bash
docker compose up -d --build
```

Services:

| Service | URL |
|---|---|
| App | http://localhost:8080 |
| Grafana | http://localhost:3000 |
| Prometheus | http://localhost:9090 |
| Tempo | http://localhost:3200 |
| Loki | http://localhost:3100 |

Grafana has anonymous admin access enabled for local use and ships a provisioned
dashboard, **CrudApp - Saga & HTTP Overview**, under the *CrudApp* folder.

## Run the app locally

```bash
cd CrudApp
dotnet run
```

The `http` launch profile points OTLP at `http://localhost:4317`, so start the stack first
if you want telemetry to be collected.

## Generate load

Create orders and run sagas to produce telemetry:

```bash
curl -X POST "http://localhost:8080/api/simulate?count=200"
```

…or use the **Simulate orders + sagas** button on the Orders page.

## Example queries

**PromQL (Prometheus)**

```promql
sum by (saga_outcome) (increase(saga_outcomes_total{job="crud-app"}[1h]))
histogram_quantile(0.95, sum by (le, saga_outcome) (rate(saga_duration_seconds_bucket[5m])))
```

**LogQL (Loki)**

```logql
{service_name="crud-app"} | severity_number >= 13
{service_name="crud-app"} |= "manual intervention"
```

**TraceQL (Tempo)**

```traceql
{ resource.service.name = "crud-app" && name = "saga order-fulfillment" && span.saga.outcome != "completed" }
```

## Configuration

Saga failure rates and telemetry endpoints are configurable:

| Setting | Where | Default |
|---|---|---|
| `Saga:StepFailureRate` | `appsettings.json` | `0.15` |
| `Saga:CompensationFailureRate` | `appsettings.json` | `0.10` |
| `OTEL_EXPORTER_OTLP_ENDPOINT` | environment | `http://localhost:4317` |
| `OTEL_METRIC_EXPORT_INTERVAL` | environment | `10000` (ms) |
