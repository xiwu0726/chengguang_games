# Game Server Deployment Guide

This document provides comprehensive instructions for deploying the Game Server in various environments using Docker, Docker Compose, and Kubernetes.

## Table of Contents

1. [Prerequisites](#prerequisites)
2. [Local Development](#local-development)
3. [Docker Deployment](#docker-deployment)
4. [Docker Compose Deployment](#docker-compose-deployment)
5. [Kubernetes Deployment](#kubernetes-deployment)
6. [CI/CD Pipeline](#cicd-pipeline)
7. [Monitoring and Logging](#monitoring-and-logging)
8. [Troubleshooting](#troubleshooting)

## Prerequisites

### For Docker & Docker Compose
- Docker Desktop or Docker Engine installed
- Docker Compose plugin installed
- Minimum 2GB RAM available

### For Kubernetes
- Kubernetes cluster (Minikube, Kind, or cloud provider)
- kubectl configured
- Helm 3.x installed

### For CI/CD
- GitHub repository
- AWS account (for ECR and EKS)
- GitHub Secrets configured

## Local Development

### Setup

1. Navigate to the game-server directory:
```bash
cd game-server
```

2. Install dependencies:
```bash
npm install
```

3. Create environment file:
```bash
cp .env.example .env.development
```

4. Start the server:
```bash
npm start
```

The server will start on `http://localhost:3000`

### Development with Docker Compose

1. Build and start with Docker Compose:
```bash
cd ..
./scripts/deploy.sh docker-compose development
```

2. Access the server at `http://localhost:3000`

3. View logs:
```bash
docker-compose logs -f
```

## Docker Deployment

### Build and Run

1. Navigate to game-server directory:
```bash
cd game-server
```

2. Load environment variables:
```bash
export $(cat .env.production | grep -v '^#' | xargs)
```

3. Build Docker image:
```bash
docker build -t game-server:latest .
```

4. Run container:
```bash
docker run -d \
  --name game-server \
  --restart unless-stopped \
  -p 3000:3000 \
  --env-file .env.production \
  game-server:latest
```

### Health Check

```bash
curl http://localhost:3000/health
```

### Stop and Remove

```bash
docker stop game-server
docker rm game-server
```

## Docker Compose Deployment

### Development Environment

```bash
cd game-server
docker-compose up -d
```

### Production Environment

```bash
cd ..
./scripts/deploy.sh docker-compose production
```

### View Logs

```bash
docker-compose logs -f game-server
```

### Stop Services

```bash
docker-compose down
```

## Kubernetes Deployment

### Prerequisites

1. Install Helm:
```bash
helm install --help
```

2. Create Kubernetes namespace:
```bash
kubectl create namespace production
kubectl create namespace staging
```

3. Load Docker images to your cluster (if using Minikube/Kind):
```bash
minikube image load game-server:latest
```

### Deploy with Helm

1. Navigate to the helm chart directory:
```bash
cd k8s/helm/game-server
```

2. Update values for your environment:
```bash
helm edit release game-server
```

3. Install the chart:
```bash
helm install game-server . \
  --namespace production \
  --create-namespace \
  --set image.tag=latest \
  --set env.NODE_ENV=production
```

### Deploy with kubectl

1. Apply ConfigMap:
```bash
kubectl apply -f k8s/game-server-configmap.yaml -n production
```

2. Apply Secret:
```bash
kubectl apply -f k8s/game-server-secret.yaml -n production
```

3. Apply Deployment:
```bash
kubectl apply -f k8s/game-server-deployment.yaml -n production
```

4. Apply Service:
```bash
kubectl apply -f k8s/game-server-service.yaml -n production
```

### Verify Deployment

```bash
# Check pods
kubectl get pods -n production

# Check services
kubectl get svc -n production

# View logs
kubectl logs -f deployment/game-server -n production

# Port forward for testing
kubectl port-forward svc/game-server-service 3000:80 -n production
```

### Update Deployment

```bash
# Update image tag
kubectl set image deployment/game-server game-server=game-server:v2.0.0 -n production

# Rollback if needed
kubectl rollout undo deployment/game-server -n production
```

### Scale Deployment

```bash
# Scale to 5 replicas
kubectl scale deployment/game-server --replicas=5 -n production
```

### Delete Deployment

```bash
helm uninstall game-server -n production
```

## CI/CD Pipeline

### GitHub Actions Workflow

The CI/CD pipeline is defined in `.github/workflows/game-server-ci-cd.yml`

### Pipeline Stages

1. **Test**: Run unit tests and integration tests
2. **Build**: Build Docker images and push to ECR
3. **Deploy to Staging**: Deploy to staging environment on push to `develop` branch
4. **Deploy to Production**: Deploy to production on release

### Secrets Required

- `AWS_ACCESS_KEY_ID`: AWS access key for ECR
- `AWS_SECRET_ACCESS_KEY`: AWS secret key for ECR
- `ECR_REPOSITORY`: ECR repository name

### Triggering Deployments

- **Staging**: Push to `develop` branch
- **Production**: Create a GitHub release

### Manual Deployment

```bash
# Deploy to staging manually
kubectl set image deployment/game-server game-server=game-server:latest -n staging

# Deploy to production manually
kubectl set image deployment/game-server game-server=game-server:latest -n production
```

## Monitoring and Logging

### Health Checks

The server provides a health check endpoint:

```bash
curl http://localhost:3000/health
```

Response:
```json
{
  "status": "healthy",
  "uptime": 123.45,
  "timestamp": "2024-01-01T00:00:00.000Z"
}
```

### Logs

Logs are written to:
- `logs/error.log`: Error-level logs
- `logs/combined.log`: All logs

### Kubernetes Logs

```bash
# Follow logs
kubectl logs -f deployment/game-server -n production

# View logs from all pods
kubectl logs -l app=game-server -n production --all-containers=true
```

### Metrics

For production environments, consider adding:
- Prometheus metrics
- Grafana dashboards
- Logging aggregation (ELK stack)

## Troubleshooting

### Docker Issues

**Problem**: Container won't start
```bash
docker logs game-server
```

**Problem**: Port already in use
```bash
# Find process using port 3000
lsof -i :3000
# Kill the process
kill -9 <PID>
```

### Kubernetes Issues

**Problem**: Pods not running
```bash
kubectl describe pod <pod-name> -n production
kubectl logs <pod-name> -n production
```

**Problem**: Rolling update failed
```bash
kubectl rollout history deployment/game-server -n production
kubectl rollout undo deployment/game-server -n production
```

**Problem**: Service not accessible
```bash
kubectl get svc game-server-service -n production
kubectl describe svc game-server-service -n production
```

### Application Issues

**Problem**: Service returns 500 error
```bash
# Check logs
tail -f game-server/logs/error.log

# Check health endpoint
curl http://localhost:3000/health
```

**Problem**: Environment variables not loading
```bash
# Verify environment file
cat .env.production

# Check container environment
docker exec game-server env | grep NODE_ENV
```

## Best Practices

1. **Always test in staging before production**
2. **Use environment-specific configuration**
3. **Monitor logs and health checks**
4. **Implement proper error handling**
5. **Use secrets management for sensitive data**
6. **Keep dependencies updated**
7. **Regular security audits**
8. **Backup configurations and data**

## Support

For issues or questions:
- Check the logs
- Review this documentation
- Contact the DevOps team
