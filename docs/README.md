# DevOps Deployment Documentation

This directory contains comprehensive DevOps documentation for the Game Server deployment.

## Quick Start

### 1. Local Development

```bash
cd game-server
npm install
npm start
```

### 2. Docker Deployment

```bash
cd game-server
docker build -t game-server:latest .
docker run -d -p 3000:3000 --env-file .env.production game-server:latest
```

### 3. Kubernetes Deployment

```bash
kubectl apply -f k8s/game-server-configmap.yaml -n production
kubectl apply -f k8s/game-server-secret.yaml -n production
kubectl apply -f k8s/game-server-deployment.yaml -n production
kubectl apply -f k8s/game-server-service.yaml -n production
```

## Documentation Files

- **[DEPLOYMENT.md](DEPLOYMENT.md)**: Complete deployment guide for all environments
- **[DOCKER.md](DOCKER.md)**: Docker configuration and best practices
- **[KUBERNETES.md](KUBERNETES.md)**: Kubernetes deployment and management

## Deployment Scripts

Located in `scripts/`:

- **deploy.sh**: Main deployment script supporting Docker, Docker Compose, and Kubernetes
- **rollback.sh**: Rollback script for all deployment modes
- **health-check.sh**: Health check script for monitoring

Usage:
```bash
./scripts/deploy.sh [mode] [environment]
./scripts/rollback.sh [mode] [environment]
./scripts/health-check.sh [url] [environment]
```

## Configuration Files

### Docker
- `game-server/Dockerfile`: Container configuration
- `game-server/docker-compose.yml`: Local development setup
- `game-server/.dockerignore`: Files to exclude from Docker build

### Kubernetes
- `k8s/game-server-deployment.yaml`: Deployment manifest
- `k8s/game-server-service.yaml`: Service manifest
- `k8s/game-server-configmap.yaml`: Non-sensitive configuration
- `k8s/game-server-secret.yaml`: Sensitive configuration
- `k8s/helm/game-server/`: Helm chart for Kubernetes

### Environment
- `game-server/.env.example`: Environment variable template
- `game-server/.env.development`: Development environment
- `game-server/.env.production`: Production environment

## CI/CD

The CI/CD pipeline is configured in `.github/workflows/game-server-ci-cd.yml`:

- **Test**: Runs unit and integration tests
- **Build**: Builds and pushes Docker images to ECR
- **Deploy**: Deploys to staging or production based on branch/release

## Health Check Endpoint

```bash
curl http://localhost:3000/health
```

Returns:
```json
{
  "status": "healthy",
  "uptime": 123.45,
  "timestamp": "2024-01-01T00:00:00.000Z"
}
```

## Common Commands

### Docker

```bash
# Build image
docker build -t game-server:latest .

# Run container
docker run -d -p 3000:3000 --name game-server game-server:latest

# View logs
docker logs -f game-server

# Stop container
docker stop game-server
docker rm game-server
```

### Docker Compose

```bash
# Start services
docker-compose up -d

# View logs
docker-compose logs -f

# Stop services
docker-compose down

# Rebuild
docker-compose up -d --build
```

### Kubernetes

```bash
# Apply manifests
kubectl apply -f k8s/

# Check status
kubectl get all -n production

# View logs
kubectl logs -f deployment/game-server -n production

# Scale deployment
kubectl scale deployment/game-server --replicas=5 -n production

# Rollback
kubectl rollout undo deployment/game-server -n production

# Port forward
kubectl port-forward svc/game-server-service 3000:80 -n production
```

### Helm

```bash
# Install
helm install game-server k8s/helm/game-server/ -n production --create-namespace

# Upgrade
helm upgrade game-server k8s/helm/game-server/ -n production

# Uninstall
helm uninstall game-server -n production
```

## Troubleshooting

See [DEPLOYMENT.md](DEPLOYMENT.md#troubleshooting) for common issues and solutions.

## Monitoring

The server includes:
- Health check endpoint (`/health`)
- Winston logging (error.log, combined.log)
- Kubernetes health probes
- Docker health checks

## Security

- Secrets stored in Kubernetes Secrets
- Environment variables for sensitive data
- Non-root user in Docker
- Image scanning recommended
- RBAC configured for Kubernetes

## Support

For issues:
1. Check logs
2. Review documentation
3. Run health check script
4. Contact DevOps team

## Version History

- **v1.0.0** (2024): Initial DevOps configuration
  - Docker support
  - Kubernetes manifests
  - Helm chart
  - CI/CD pipeline
  - Deployment scripts
  - Comprehensive documentation
