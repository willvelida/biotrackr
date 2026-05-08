#!/bin/bash
# Start all backend APIs + reverse proxy + UI for local development
# Usage: bash scripts/start-local.sh

set -e

COSMOS_ENDPOINT="https://cosmos-emulator:8081"
COSMOS_KEY="C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw=="
PROXY_PORT=9000

# Shared env vars for all APIs
export cosmosdbendpoint="$COSMOS_ENDPOINT"
export cosmosdbaccountkey="$COSMOS_KEY"
export Biotrackr__CosmosDb__AccountKey="$COSMOS_KEY"
export Biotrackr__DatabaseName="BiotrackrDB"
export Biotrackr__ContainerName="records"
export Biotrackr__ConversationsContainerName="conversations"
export azureappconfigendpoint=""
export managedidentityclientid=""
export applicationinsightsconnectionstring="InstrumentationKey=00000000-0000-0000-0000-000000000000"
export ASPNETCORE_ENVIRONMENT="Development"

PIDS=()

cleanup() {
    echo ""
    echo "Shutting down services..."
    for pid in "${PIDS[@]}"; do
        kill "$pid" 2>/dev/null || true
    done
    kill "$(cat /tmp/caddy.pid 2>/dev/null)" 2>/dev/null || true
    echo "All services stopped."
}
trap cleanup EXIT INT TERM

echo "=== Biotrackr Local Dev Stack ==="
echo ""

# Start Caddy reverse proxy
echo "Starting API gateway (Caddy) on port $PROXY_PORT..."
caddy start --config .devcontainer/Caddyfile --pidfile /tmp/caddy.pid 2>/dev/null
echo "  Gateway ready at http://localhost:$PROXY_PORT"

# Start domain APIs
start_api() {
    local name="$1" dir="$2" port="$3"
    echo "Starting $name on port $port..."
    cd "/workspaces/biotrackr/$dir/$name"
    ASPNETCORE_URLS="http://+:$port" dotnet run --no-build --no-launch-profile > "/tmp/$name.log" 2>&1 &
    PIDS+=($!)
    cd /workspaces/biotrackr
}

start_api "Biotrackr.Activity.Api" "src/Biotrackr.Activity.Api" 5272
start_api "Biotrackr.Food.Api" "src/Biotrackr.Food.Api" 5006
start_api "Biotrackr.Sleep.Api" "src/Biotrackr.Sleep.Api" 5004
start_api "Biotrackr.Vitals.Api" "src/Biotrackr.Vitals.Api" 5062

echo ""
echo "Waiting for APIs to start..."
sleep 5

# Start UI
echo "Starting UI on port 5239..."
export biotrackrapiendpoint="http://localhost:$PROXY_PORT"
cd /workspaces/biotrackr/src/Biotrackr.UI/Biotrackr.UI
ASPNETCORE_URLS="http://+:5239" dotnet run --no-build --no-launch-profile > /tmp/Biotrackr.UI.log 2>&1 &
PIDS+=($!)
cd /workspaces/biotrackr

echo ""
echo "=== All services starting ==="
echo "  API Gateway:   http://localhost:$PROXY_PORT"
echo "  Activity API:  http://localhost:5272"
echo "  Food API:      http://localhost:5006"
echo "  Sleep API:     http://localhost:5004"
echo "  Vitals API:    http://localhost:5062"
echo "  UI:            http://localhost:5239"
echo ""
echo "Logs: /tmp/Biotrackr.*.log"
echo "Press Ctrl+C to stop all services."
echo ""

# Wait for any child to exit
wait
