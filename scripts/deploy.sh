#!/bin/bash

# Game Server Deployment Script
# Supports Docker, Docker Compose, and Kubernetes deployments

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
MODE=${1:-docker-compose}
ENVIRONMENT=${2:-development}

# Validate environment
if [[ "$ENVIRONMENT" != "development" && "$ENVIRONMENT" != "production" ]]; then
    print_error "Invalid environment. Use 'development' or 'production'"
    exit 1
fi

print_info "Starting deployment in $ENVIRONMENT mode using $MODE"

# Docker Compose Deployment
if [ "$MODE" == "docker-compose" ]; then
    print_info "Deploying with Docker Compose..."

    cd game-server

    # Load environment variables
    if [ -f ".env.$ENVIRONMENT" ]; then
        print_info "Loading environment variables from .env.$ENVIRONMENT"
        export $(cat .env.$ENVIRONMENT | grep -v '^#' | xargs)
    fi

    # Build and start containers
    print_info "Building Docker images..."
    docker-compose build

    print_info "Starting containers..."
    docker-compose up -d

    # Wait for service to be ready
    print_info "Waiting for service to be ready..."
    sleep 5

    # Check health
    print_info "Checking service health..."
    curl -f http://localhost:3000/health || {
        print_error "Service health check failed"
        exit 1
    }

    print_info "Docker Compose deployment completed successfully"

# Kubernetes Deployment
elif [ "$MODE" == "kubernetes" ]; then
    print_info "Deploying with Kubernetes..."

    # Check if kubectl is available
    if ! command -v kubectl &> /dev/null; then
        print_error "kubectl is not installed"
        exit 1
    fi

    # Set namespace
    NAMESPACE="staging"
    if [ "$ENVIRONMENT" == "production" ]; then
        NAMESPACE="production"
    fi

    print_info "Using namespace: $NAMESPACE"

    # Load Docker images to kind/minikube if running locally
    if command -v minikube &> /dev/null; then
        print_info "Loading images to Minikube..."
        minikube image load game-server:latest || true
    fi

    # Apply Kubernetes resources
    print_info "Applying Kubernetes manifests..."
    kubectl apply -f k8s/game-server-configmap.yaml -n "$NAMESPACE"
    kubectl apply -f k8s/game-server-secret.yaml -n "$NAMESPACE"
    kubectl apply -f k8s/game-server-deployment.yaml -n "$NAMESPACE"
    kubectl apply -f k8s/game-server-service.yaml -n "$NAMESPACE"

    # Wait for deployment
    print_info "Waiting for deployment to be ready..."
    kubectl rollout status deployment/game-server -n "$NAMESPACE" --timeout=5m

    # Show service endpoint
    print_info "Service endpoint:"
    kubectl get svc game-server-service -n "$NAMESPACE" -o wide

    print_info "Kubernetes deployment completed successfully"

# Docker Deployment
elif [ "$MODE" == "docker" ]; then
    print_info "Deploying with Docker..."

    cd game-server

    # Load environment variables
    if [ -f ".env.$ENVIRONMENT" ]; then
        print_info "Loading environment variables from .env.$ENVIRONMENT"
        export $(cat .env.$ENVIRONMENT | grep -v '^#' | xargs)
    fi

    # Build Docker image
    print_info "Building Docker image..."
    docker build -t game-server:latest .

    # Stop existing container
    print_info "Stopping existing container..."
    docker stop game-server || true
    docker rm game-server || true

    # Run container
    print_info "Starting container..."
    docker run -d \
        --name game-server \
        --restart unless-stopped \
        -p 3000:3000 \
        --env-file .env.$ENVIRONMENT \
        game-server:latest

    # Wait for service to be ready
    print_info "Waiting for service to be ready..."
    sleep 5

    # Check health
    print_info "Checking service health..."
    curl -f http://localhost:3000/health || {
        print_error "Service health check failed"
        exit 1
    }

    print_info "Docker deployment completed successfully"

else
    print_error "Invalid deployment mode. Use 'docker-compose', 'kubernetes', or 'docker'"
    exit 1
fi

print_info "Deployment completed successfully!"
