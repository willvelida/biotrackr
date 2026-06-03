#!/bin/bash
# Wait for the Cosmos DB Emulator to be ready before running E2E tests.
# Usage: bash scripts/wait-for-cosmos-emulator.sh [endpoint]
#
# The web/cert endpoint (/_explorer/emulator.pem) becomes available before the
# gateway can serve account reads, so the first data-plane request can fail with
# a 503 ServiceUnavailable. This script first waits for the web endpoint, then
# polls the data-plane account endpoint until the gateway is actually serving
# requests (an authenticated endpoint returns 401 once ready, versus 503/000
# while still initialising).

set -e

ENDPOINT="${1:-https://localhost:8081}"
MAX_ATTEMPTS=30
SLEEP_SECONDS=10

echo "Waiting for Cosmos DB Emulator web endpoint to be ready..."
cert_ready=false
for i in $(seq 1 "$MAX_ATTEMPTS"); do
  if curl -k "${ENDPOINT}/_explorer/emulator.pem" > /dev/null 2>&1; then
    echo "Cosmos DB Emulator web endpoint is ready!"
    cert_ready=true
    break
  fi
  echo "Waiting for web endpoint... (attempt $i/$MAX_ATTEMPTS)"
  sleep "$SLEEP_SECONDS"
done

if [ "$cert_ready" != "true" ]; then
  echo "::error::Cosmos DB Emulator web endpoint did not become ready in time"
  exit 1
fi

echo "Waiting for Cosmos DB Emulator data plane to be ready..."
data_ready=false
for i in $(seq 1 "$MAX_ATTEMPTS"); do
  status=$(curl -k -s -o /dev/null -w "%{http_code}" "${ENDPOINT}/" || echo "000")
  if [ "$status" = "401" ] || [ "$status" = "200" ]; then
    echo "Cosmos DB Emulator data plane is ready! (HTTP $status)"
    data_ready=true
    break
  fi
  echo "Waiting for data plane... (attempt $i/$MAX_ATTEMPTS, HTTP $status)"
  sleep "$SLEEP_SECONDS"
done

if [ "$data_ready" != "true" ]; then
  echo "::error::Cosmos DB Emulator data plane did not become ready in time"
  exit 1
fi
