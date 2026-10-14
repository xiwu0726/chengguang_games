# Kubernetes Deployment Guide

This document explains the Kubernetes deployment configuration for the Game Server.

## Overview

The Kubernetes configuration includes:
- **Deployment**: Manages pod replicas
- **Service**: Exposes the application
- **ConfigMap**: Stores non-sensitive configuration
- **Secret**: Stores sensitive configuration
- **Helm Chart**: For easy deployment and management

## Architecture

```
┌─────────────────┐
│  Ingress/Nginx  │
└────────┬────────┘
         │
         ▼
┌─────────────────┐
│  Service        │
│  (ClusterIP)    │
└────────┬────────┘
         │
         ▼
┌─────────────────┐
│  Deployment     │
│  (3 Replicas)   │
└────────┬────────┘
         │
         ▼
┌─────────────────┐
│  Pods           │
│  (game-server)  │
└─────────────────┘
```

## Kubernetes Resources

### Deployment (`game-server-deployment.yaml`)

Manages 3 replicas of the game-server pod:

```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: game-server
spec:
  replicas: 3
  selector:
    matchLabels:
      app: game-server
```

**Key Features**:
- 3 replicas for high availability
- Auto-scaling support
- Health checks (liveness/readiness)
- Resource limits and requests
- Volume mounts for logs and config

### Service (`game-server-service.yaml`)

Exposes the deployment as a ClusterIP service:

```yaml
apiVersion: v1
kind: Service
metadata:
  name: game-server-service
spec:
  type: ClusterIP
  ports:
    - port: 80
      targetPort: 3000
  selector:
    app: game-server
```

### ConfigMap (`game-server-configmap.yaml`)

Stores non-sensitive configuration:

```yaml
apiVersion: v1
kind: ConfigMap
metadata:
  name: game-server-config
data:
  log.level: "info"
  app-config.json: |
    {
      "server": {
        "port": 3000
      },
      "retry": {
        "maxRetries": 3
      }
    }
```

### Secret (`game-server-secret.yaml`)

Stores sensitive configuration (base64 encoded):

```yaml
apiVersion: v1
kind: Secret
metadata:
  name: game-server-secret
type: Opaque
data:
  JWT_SECRET: base64_encoded_value
  DB_PASSWORD: base64_encoded_value
```

## Helm Chart

The Helm chart provides a standardized way to deploy and manage the application.

### Chart Structure

```
game-server/
├── Chart.yaml          # Chart metadata
├── values.yaml         # Default values
└── templates/
    ├── deployment.yaml # Deployment template
    ├── service.yaml    # Service template
    ├── ingress.yaml    # Ingress template
    └── _helpers.tpl    # Helper functions
```

### Installation

#### Install with Helm

```bash
# Navigate to helm chart directory
cd k8s/helm/game-server

# Install in production
helm install game-server . \
  --namespace production \
  --create-namespace \
  --set image.tag=1.0.0 \
  --set env.NODE_ENV=production
```

#### Upgrade

```bash
helm upgrade game-server . \
  --namespace production \
  --set image.tag=2.0.0
```

#### Uninstall

```bash
helm uninstall game-server -n production
```

### Custom Values

Edit `values.yaml` to customize:

```yaml
replicaCount: 5

image:
  repository: your-registry/game-server
  tag: "1.0.0"

resources:
  limits:
    cpu: 1
    memory: 1Gi
  requests:
    cpu: 500m
    memory: 512Mi

autoscaling:
  enabled: true
  minReplicas: 3
  maxReplicas: 10
```

## Deployment Commands

### Create Namespace

```bash
kubectl create namespace production
kubectl create namespace staging
```

### Apply Manifests

```bash
# Apply all resources
kubectl apply -f k8s/game-server-configmap.yaml -n production
kubectl apply -f k8s/game-server-secret.yaml -n production
kubectl apply -f k8s/game-server-deployment.yaml -n production
kubectl apply -f k8s/game-server-service.yaml -n production
```

### Check Status

```bash
# Check pods
kubectl get pods -n production -l app=game-server

# Check services
kubectl get svc -n production -l app=game-server

# Check deployments
kubectl get deployment -n production -l app=game-server
```

### View Logs

```bash
# Follow logs for all pods
kubectl logs -f -l app=game-server -n production

# View logs for specific pod
kubectl logs -f <pod-name> -n production
```

### Port Forward

```bash
# Forward service port to local
kubectl port-forward svc/game-server-service 3000:80 -n production
```

## Rolling Updates

### Update Deployment

```bash
kubectl set image deployment/game-server \
  game-server=game-server:v2.0.0 \
  -n production
```

### Check Rollout Status

```bash
kubectl rollout status deployment/game-server -n production
```

### View Rollout History

```bash
kubectl rollout history deployment/game-server -n production
```

### Rollback

```bash
kubectl rollout undo deployment/game-server -n production
```

## Scaling

### Scale Replicas

```bash
# Scale to 5 replicas
kubectl scale deployment/game-server --replicas=5 -n production
```

### Horizontal Pod Autoscaler

Enable HPA in `values.yaml`:

```yaml
autoscaling:
  enabled: true
  minReplicas: 3
  maxReplicas: 10
  targetCPUUtilizationPercentage: 80
  targetMemoryUtilizationPercentage: 80
```

Apply HPA:

```bash
kubectl autoscale deployment game-server \
  --min=3 --max=10 \
  --cpu-percent=80 \
  -n production
```

## Ingress Configuration

### Enable Ingress

Edit `values.yaml`:

```yaml
ingress:
  enabled: true
  className: nginx
  annotations:
    cert-manager.io/cluster-issuer: letsencrypt-prod
  hosts:
    - host: game-server.example.com
      paths:
        - path: /
          pathType: Prefix
  tls:
    - secretName: game-server-tls
      hosts:
        - game-server.example.com
```

### Apply Ingress

```bash
kubectl apply -f k8s/game-server-service.yaml -n production
```

### Get Ingress

```bash
kubectl get ingress game-server -n production
```

## Health Checks

### Liveness Probe

Checks if the container is running:

```yaml
livenessProbe:
  httpGet:
    path: /health
    port: 3000
  initialDelaySeconds: 30
  periodSeconds: 10
```

### Readiness Probe

Checks if the container is ready to serve traffic:

```yaml
readinessProbe:
  httpGet:
    path: /health
    port: 3000
  initialDelaySeconds: 5
  periodSeconds: 5
```

## Resource Management

### Resource Requests

```yaml
resources:
  requests:
    cpu: 250m
    memory: 256Mi
```

### Resource Limits

```yaml
resources:
  limits:
    cpu: 500m
    memory: 512Mi
```

## Monitoring

### Prometheus Integration

Add metrics endpoint (requires server modification):

```bash
# Install Prometheus Operator
kubectl apply -f https://raw.githubusercontent.com/prometheus-operator/prometheus-operator/master/bundle.yaml

# Create ServiceMonitor
kubectl apply -f k8s/game-server-servicemonitor.yaml
```

### Logging

Configure logging to stdout for Kubernetes:

```yaml
volumeMounts:
  - name: logs
    mountPath: /app/logs

volumes:
  - name: logs
    emptyDir: {}
```

## Troubleshooting

### Pods Not Running

```bash
kubectl describe pod <pod-name> -n production
kubectl logs <pod-name> -n production
```

### Image Pull Errors

```bash
# Check image name and tag
kubectl get deployment game-server -n production -o jsonpath='{.spec.template.spec.containers[0].image}'

# Verify image exists in registry
# Check imagePullPolicy
kubectl get deployment game-server -n production -o jsonpath='{.spec.template.spec.containers[0].imagePullPolicy}'
```

### Service Not Accessible

```bash
# Check service endpoints
kubectl get endpoints game-server-service -n production

# Check service selector
kubectl get svc game-server-service -n production -o jsonpath='{.spec.selector}'
```

### Resource Issues

```bash
# Check resource usage
kubectl top pods -n production

# Check events
kubectl get events -n production --sort-by='.lastTimestamp'
```

## Security

### Pod Security Standards

```yaml
podSecurityContext:
  runAsNonRoot: true
  runAsUser: 1000
  fsGroup: 1000

securityContext:
  allowPrivilegeEscalation: false
  readOnlyRootFilesystem: true
  capabilities:
    drop:
    - ALL
```

### RBAC

Create ServiceAccount and RBAC rules:

```yaml
apiVersion: v1
kind: ServiceAccount
metadata:
  name: game-server
  namespace: production
---
apiVersion: rbac.authorization.k8s.io/v1
kind: Role
metadata:
  name: game-server
  namespace: production
rules:
- apiGroups: [""]
  resources: ["pods"]
  verbs: ["get", "list", "watch"]
---
apiVersion: rbac.authorization.k8s.io/v1
kind: RoleBinding
metadata:
  name: game-server
  namespace: production
subjects:
- kind: ServiceAccount
  name: game-server
  namespace: production
roleRef:
  kind: Role
  name: game-server
  apiGroup: rbac.authorization.k8s.io
```

## Backup and Restore

### Backup ConfigMaps and Secrets

```bash
# Backup ConfigMap
kubectl get configmap game-server-config -n production -o yaml > configmap-backup.yaml

# Backup Secret
kubectl get secret game-server-secret -n production -o yaml > secret-backup.yaml
```

### Restore ConfigMaps and Secrets

```bash
kubectl apply -f configmap-backup.yaml -n production
kubectl apply -f secret-backup.yaml -n production
```

## Best Practices

1. **Use Helm for deployments**: Easier management and versioning
2. **Enable auto-scaling**: Use HPA for production workloads
3. **Implement health checks**: Liveness and readiness probes
4. **Set resource limits**: Prevent resource exhaustion
5. **Use secrets management**: Don't commit secrets to Git
6. **Monitor logs**: Use centralized logging (ELK, Loki)
7. **Regular backups**: Backup configurations and data
8. **Security hardening**: Follow security best practices
9. **Version control**: Track all configuration changes
10. **Documentation**: Keep deployment documentation updated
