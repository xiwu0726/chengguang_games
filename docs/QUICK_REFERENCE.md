# DevOps Quick Reference

## Deployment Commands

### Docker
```bash
cd game-server
docker build -t game-server:latest .
docker run -d -p 3000:3000 --env-file .env.production game-server:latest
```

### Docker Compose
```bash
cd game-server
docker-compose up -d
```

### Kubernetes
```bash
kubectl apply -f k8s/game-server-configmap.yaml -n production
kubectl apply -f k8s/game-server-secret.yaml -n production
kubectl apply -f k8s/game-server-deployment.yaml -n production
kubectl apply -f k8s/game-server-service.yaml -n production
```

### Helm
```bash
helm install game-server k8s/helm/game-server/ -n production --create-namespace
```

## Health Checks

```bash
curl http://localhost:3000/health
```

## Logs

```bash
# Docker
docker logs -f game-server

# Docker Compose
docker-compose logs -f

# Kubernetes
kubectl logs -f deployment/game-server -n production
```

## Rollback

```bash
kubectl rollout undo deployment/game-server -n production
```

## Scale

```bash
kubectl scale deployment/game-server --replicas=5 -n production
```

## Environment Variables

- `NODE_ENV`: development | production
- `PORT`: 3000
- `LOG_LEVEL`: debug | info | warn | error

## Endpoints

- `GET /health`: Health check
- `GET /simulate-error`: Simulate errors
- `GET /api/game-status`: Game status with retry logic

## CI/CD Triggers

- **Staging**: Push to `develop` branch
- **Production**: Create GitHub release

## Files

### Docker
- `game-server/Dockerfile`
- `game-server/docker-compose.yml`
- `game-server/.dockerignore`

### Kubernetes
- `k8s/game-server-deployment.yaml`
- `k8s/game-server-service.yaml`
- `k8s/game-server-configmap.yaml`
- `k8s/game-server-secret.yaml`
- `k8s/helm/game-server/`

### Scripts
- `scripts/deploy.sh`
- `scripts/rollback.sh`
- `scripts/health-check.sh`

### Documentation
- `docs/DEPLOYMENT.md`
- `docs/DOCKER.md`
- `docs/KUBERNETES.md`
- `docs/README.md`

## Common Issues

| Issue | Solution |
|-------|----------|
| Port in use | `lsof -i :3000` then kill the process |
| Container won't start | Check logs: `docker logs game-server` |
| Pod not running | Check status: `kubectl describe pod <pod>` |
| Service not accessible | Check endpoints: `kubectl get endpoints` |
| Image pull error | Check image name and tag |
