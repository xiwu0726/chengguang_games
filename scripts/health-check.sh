#!/bin/bash

# Game Server Health Check Script

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
URL=${1:-http://localhost:3000}
ENVIRONMENT=${2:-development}

print_info "Running health check on $URL (Environment: $ENVIRONMENT)"

# Check if URL is reachable
print_info "Checking if URL is reachable..."
if ! curl -f -s -o /dev/null "$URL"; then
    print_error "URL is not reachable"
    exit 1
fi

print_info "URL is reachable"

# Health check endpoint
print_info "Checking health endpoint..."
HEALTH_RESPONSE=$(curl -s "$URL/health")
echo "Health response: $HEALTH_RESPONSE"

# Parse health response
STATUS=$(echo "$HEALTH_RESPONSE" | grep -o '"status":"[^"]*"' | cut -d'"' -f4)
UPTIME=$(echo "$HEALTH_RESPONSE" | grep -o '"uptime":[^,}]*' | cut -d':' -f2)

print_info "Service status: $STATUS"
print_info "Uptime: ${UPTIME}s"

# API endpoint check
print_info "Checking API endpoint..."
API_RESPONSE=$(curl -s "$URL/api/game-status")
echo "API response: $API_RESPONSE"

# Check for errors
if echo "$API_RESPONSE" | grep -q '"errorType"'; then
    print_warning "API returned error: $(echo "$API_RESPONSE" | grep -o '"errorType":[^,}]*' | cut -d':' -f2)"
fi

# Check logs if available
if [ -d "game-server/logs" ]; then
    print_info "Checking recent logs..."
    echo "Recent error logs:"
    tail -20 game-server/logs/error.log || true
    echo "Recent combined logs:"
    tail -20 game-server/logs/combined.log || true
fi

# Overall status
if [ "$STATUS" == "healthy" ]; then
    print_info "Health check PASSED"
    exit 0
else
    print_error "Health check FAILED"
    exit 1
fi
