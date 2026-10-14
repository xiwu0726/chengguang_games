# Prometheus and Grafana Monitoring Setup

This directory contains Prometheus and Grafana configuration for monitoring the game server.

## Files

### Prometheus Configuration
- `prometheus.yml` - Main Prometheus configuration file
- `prometheus/alerts/game-server-alerts.yml` - Alert rules for game server

### Grafana Configuration
- `grafana/dashboards/game-server-dashboard.json` - Monitoring dashboard
- `grafana/datasources/prometheus-datasource.yml` - Prometheus data source configuration
- `grafana/alerting/alert-rules.yml` - Grafana alert rules

## Setup

### 1. Prometheus Setup

#### Using Docker
```bash
docker run -d \
  --name prometheus \
  -p 9090:9090 \
  -v $(pwd)/prometheus.yml:/etc/prometheus/prometheus.yml \
  -v $(pwd)/prometheus/alerts:/etc/prometheus/alerts \
  prom/prometheus
```

#### Using Docker Compose
```bash
docker-compose -f docker-compose.monitoring.yml up -d
```

### 2. Grafana Setup

#### Using Docker
```bash
docker run -d \
  --name grafana \
  -p 3000:3000 \
  -v $(pwd)/grafana/dashboards:/var/lib/grafana/dashboards \
  -v $(pwd)/grafana/datasources:/etc/grafana/provisioning/datasources \
  -v $(pwd)/grafana/alerting:/etc/grafana/provisioning/alerting \
  grafana/grafana
```

#### Using Docker Compose
```bash
docker-compose -f docker-compose.monitoring.yml up -d grafana
```

### 3. Access Grafana

1. Open http://localhost:3000 in your browser
2. Default login:
   - Username: admin
   - Password: admin
3. Change the password on first login

### 4. Import Dashboard

1. Go to Dashboards → Import
2. Upload `game-server-dashboard.json`
3. Select "Prometheus" as datasource
4. Click "Import"

## Monitoring Metrics

The game server exposes the following metrics:

### Request Metrics
- `http_requests_total` - Total requests
- `http_request_duration_seconds` - Request duration
- `http_retry_total` - Total retries

### Circuit Breaker Metrics
- `circuit_breaker_state` - Circuit breaker state (CLOSED/OPEN/HALF_OPEN)
- `circuit_breaker_failure_count` - Failure count

### Degraded Mode Metrics
- `degraded_mode_enabled` - Whether degraded mode is enabled
- `degraded_mode_failure_rate` - Failure rate

## Alert Rules

The system monitors for:
- High error rate (> 5%)
- Circuit breaker open state
- High latency (p95 > 1s)
- Very high latency (p95 > 5s)
- Server down status
- Degraded mode enabled
- High memory usage (> 90%)
- High CPU usage (> 80%)
- Too many retries (> 10 retries/s)

## Health Check

The game server provides a health check endpoint:
- URL: `GET /health`
- Response: JSON with server status, uptime, circuit breakers, degraded mode, etc.

Example response:
```json
{
  "status": "healthy",
  "timestamp": "2024-01-01T00:00:00.000Z",
  "uptime": 123.45,
  "version": "1.0.0",
  "requestId": "abc-123-def",
  "circuitBreakers": {
    "gameService": {
      "state": "CLOSED",
      "failureCount": 0
    }
  },
  "degradedMode": {
    "enabled": false,
    "failureRate": 0,
    "failedRequests": 0,
    "totalRequests": 100
  }
}
```

## Integration with Kubernetes

The Prometheus configuration includes Kubernetes service discovery for:
- Nodes
- Pods with annotations

### Enable Pod Metrics
Add the following annotation to your game-server deployment:
```yaml
annotations:
  prometheus.io/scrape: "true"
  prometheus.io/path: "/metrics"
  prometheus.io/port: "3000"
```

## Troubleshooting

### Prometheus not scraping metrics
- Check if the game server is running: `curl http://localhost:3000/metrics`
- Check Prometheus targets: http://localhost:9090/targets
- Verify the scrape interval and port configuration

### Grafana not showing data
- Verify the Prometheus datasource is configured correctly
- Check the dashboard query syntax
- Ensure Prometheus is collecting metrics

### Alerts not firing
- Check alert rules in Prometheus UI
- Verify the alert duration threshold
- Check alertmanager configuration

## License

MIT
