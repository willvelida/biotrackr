#!/bin/bash
set -e

echo "Running onCreateCommand..."

# Install .NET global tools
dotnet tool install -g dotnet-reportgenerator-globaltool || true

# Ensure jq and xxd are available (needed by cosmos-init.sh)
sudo apt-get update -qq && sudo apt-get install -y -qq jq xxd > /dev/null 2>&1 || true

# Install Caddy reverse proxy (for local API gateway)
echo "Installing Caddy..."
sudo apt-get install -y -qq debian-keyring debian-archive-keyring apt-transport-https curl > /dev/null 2>&1 || true
curl -1sLf 'https://dl.cloudsmith.io/public/caddy/stable/gpg.key' | sudo gpg --dearmor -o /usr/share/keyrings/caddy-stable-archive-keyring.gpg 2>/dev/null || true
curl -1sLf 'https://dl.cloudsmith.io/public/caddy/stable/debian.deb.txt' | sudo tee /etc/apt/sources.list.d/caddy-stable.list > /dev/null 2>&1 || true
sudo apt-get update -qq && sudo apt-get install -y -qq caddy > /dev/null 2>&1 || true

# Trust .NET dev certificates
dotnet dev-certs https --trust 2>/dev/null || true

echo "onCreateCommand complete."
