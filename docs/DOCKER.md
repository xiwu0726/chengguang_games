# Docker Configuration Guide

This document explains the Docker configuration for the Game Server.

## Dockerfile

The `Dockerfile` creates an optimized Node.js 18 Alpine image:

```dockerfile
FROM node:18-alpine
WORKDIR /app
COPY package*.json ./
RUN npm ci --only=production
COPY . .
RUN mkdir -p logs
EXPOSE 3000
HEALTHCHECK --interval=30s --timeout=10s --start-period=5s --retries=3 \
  CMD node -e "require('http').get('http://localhost:3000/health', (r) => {process.exit(r.statusCode === 200 ? 0 : 1)})"
CMD ["node", "server.js"]
```

### Key Features

- **Multi-stage optimization**: Uses Alpine Linux for smaller image size
- **Layer caching**: Package files copied first for better caching
- **Production-only dependencies**: Reduces image size
- **Health check**: Built-in health check for monitoring
- **Graceful shutdown**: Proper signal handling

## Docker Compose

The `docker-compose.yml` file provides local development environment:

```yaml
version: '3.8'
services:
  game-server:
    build:
      context: .
      dockerfile: Dockerfile
    ports:
      - "3000:3000"
    environment:
      - NODE_ENV=development
    volumes:
      - ./server.js:/app/server.js
      - ./logs:/app/logs
    restart: unless-stopped
```

### Features

- **Hot reload**: Source code mounted for development
- **Persistent logs**: Logs directory mounted
- **Auto-restart**: Container restarts on failure

## Docker Ignore

The `.dockerignore` file excludes unnecessary files:

- `node_modules`
- `logs`
- `.env` files
- `.git` directory
- Test files

## Building and Running

### Build Image

```bash
cd game-server
docker build -t game-server:latest .
```

### Run Container

```bash
docker run -d \
  --name game-server \
  -p 3000:3000 \
  --env-file .env.production \
  game-server:latest
```

### Using Docker Compose

```bash
cd game-server
docker-compose up -d
```

## Environment Variables

The server uses environment variables for configuration:

- `NODE_ENV`: Environment (development/production)
- `PORT`: Server port (default: 3000)
- `LOG_LEVEL`: Logging level (debug/info/warn/error)
- Database and Redis configuration
- API keys and secrets

### Environment Files

- `.env.example`: Template for all environment variables
- `.env.development`: Development environment configuration
- `.env.production`: Production environment configuration

## Health Check

The Docker container includes a health check:

```bash
docker ps --filter "name=game-server" --format "table {{.Names}}\t{{.Status}}"
```

### Manual Health Check

```bash
curl http://localhost:3000/health
```

## Logs

### View Logs

```bash
# All logs
docker logs game-server

# Follow logs
docker logs -f game-server

# Last 100 lines
docker logs --tail 100 game-server
```

### Access Logs from Container

```bash
docker exec game-server tail -f /app/logs/combined.log
```

## Docker Networking

The Docker Compose setup creates a bridge network:

```bash
docker network ls | grep game-network
```

### Inspect Network

```bash
docker network inspect game-network
```

## Resource Limits

Add resource limits to Docker Compose:

```yaml
services:
  game-server:
    deploy:
      resources:
        limits:
          cpus: '0.5'
          memory: 512M
        reservations:
          cpus: '0.25'
          memory: 256M
```

## Security Best Practices

1. **Use non-root user**: Create a dedicated user in Dockerfile
2. **Minimize image size**: Use Alpine Linux
3. **Don't hardcode secrets**: Use environment variables
4. **Scan images**: Use Docker Scout or Trivy
5. **Keep images updated**: Regularly rebuild with latest base image

## Troubleshooting

### Container won't start

```bash
# Check logs
docker logs game-server

# Check exit code
docker inspect game-server --format='{{.State.ExitCode}}'
```

### Port already in use

```bash
# Find process
lsof -i :3000

# Kill process
kill -9 <PID>
```

### Volume mount issues

```bash
# Check volume permissions
docker exec game-server ls -la /app/logs
```

### Image build fails

```bash
# Clean build cache
docker builder prune

# Rebuild without cache
docker build --no-cache -t game-server:latest .
```

## Production Considerations

For production deployments:

1. **Use specific image tags**: Don't use `:latest` in production
2. **Enable secrets management**: Use Docker secrets or external secret stores
3. **Configure resource limits**: Prevent resource exhaustion
4. **Set up monitoring**: Integrate with logging and metrics systems
5. **Implement backup strategy**: Regular backups of configuration and data
