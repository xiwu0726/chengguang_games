#!/bin/bash

# Game Server Rollback Script

set -e

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Functions
print_info() {
    echo -e "${GREEN}[INFO]${NC} $1"
}

print_warning() {
    echo -e "${YELLOW}[WARN]${NC} $1"
}

print_error() {
    echo -e "${RED}[ERROR]${NC} $1"
}

# Parse command line arguments
MODE=${1:-kubernetes}
ENVIRONMENT=${2:-production}

# Validate environment
if [[ "$ENVIRONMENT" != "development" && "$ENVIRONMENT" != "production" ]]; then
    print_error "Invalid environment. Use 'development' or 'production'"
    exit 1
fi

print_info "Starting rollback in $ENVIRONMENT mode using $MODE"

# Kubernetes Rollback
if [ "$MODE" == "kubernetes" ]; then
    print_info "Rolling back Kubernetes deployment..."

    # Set namespace
    NAMESPACE="production"
    if [ "$ENVIRONMENT" == "development" ]; then
        NAMESPACE="staging"
    fi

    print_info "Using namespace: $NAMESPACE"

    # Rollback deployment
    print_info "Rolling back deployment..."
    kubectl rollout undo deployment/game-server -n "$NAMESPACE"

    # Wait for rollback to complete
    print_info "Waiting for rollback to complete..."
    kubectl rollout status deployment/game-server -n "$NAMESPACE" --timeout=5m

    print_info "Kubernetes rollback completed successfully"

# Docker Compose Rollback
elif [ "$MODE" == "docker-compose" ]; then
    print_info "Rolling back Docker Compose deployment..."

    cd game-server

    # Stop containers
    print_info "Stopping containers..."
    docker-compose down

    # Restart from previous commit
    print_info "Restarting from previous commit..."
    git log --oneline -1
    git checkout HEAD~1

    # Rebuild and start
    print_info "Rebuilding and starting..."
    docker-compose build
    docker-compose up -d

    print_info "Docker Compose rollback completed successfully"

# Docker Rollback
elif [ "$MODE" == "docker" ]; then
    print_info "Rolling back Docker deployment..."

    cd game-server

    # Stop and remove container
    print_info "Stopping container..."
    docker stop game-server
    docker rm game-server

    # Revert image tag
    print_info "Reverting to previous image..."
    docker tag game-server:latest game-server:previous
    docker tag game-server:previous game-server:latest

    # Restart
    print_info "Restarting container..."
    docker run -d \
        --name game-server \
        --restart unless-stopped \
        -p 3000:3000 \
        game-server:latest

    print_info "Docker rollback completed successfully"

else
    print_error "Invalid rollback mode. Use 'kubernetes', 'docker-compose', or 'docker'"
    exit 1
fi

print_info "Rollback completed successfully!"
