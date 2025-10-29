# Cosmos DB Emulator Helper Script for Windows/PowerShell
# Usage:
#   .\cosmos-emulator.ps1 start    - Start Cosmos DB Emulator
#   .\cosmos-emulator.ps1 stop     - Stop Cosmos DB Emulator
#   .\cosmos-emulator.ps1 status   - Check emulator status
#   .\cosmos-emulator.ps1 cert     - Install emulator certificate (requires admin)

param(
    [Parameter(Mandatory=$true)]
    [ValidateSet('start', 'stop', 'status', 'cert')]
    [string]$Action
)

$containerName = "biotrackr-cosmos-emulator"
$composeFile = "docker-compose.cosmos.yml"

function Start-CosmosEmulator {
    Write-Host "Starting Cosmos DB Emulator..." -ForegroundColor Cyan
    Write-Host "⚠️  First startup may take 2-3 minutes while emulator initializes" -ForegroundColor Yellow
    
    docker-compose -f $composeFile up -d
    
    Write-Host "`n✅ Cosmos DB Emulator starting in background" -ForegroundColor Green
    Write-Host "📍 Endpoint: https://localhost:8081" -ForegroundColor White
    Write-Host "🔑 Key: C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==" -ForegroundColor White
    Write-Host "`n📊 Check status with: .\cosmos-emulator.ps1 status" -ForegroundColor Cyan
    Write-Host "🔐 Install certificate with: .\cosmos-emulator.ps1 cert (requires admin)" -ForegroundColor Cyan
}

function Stop-CosmosEmulator {
    Write-Host "Stopping Cosmos DB Emulator..." -ForegroundColor Cyan
    docker-compose -f $composeFile down
    Write-Host "✅ Cosmos DB Emulator stopped" -ForegroundColor Green
}

function Get-CosmosEmulatorStatus {
    Write-Host "Checking Cosmos DB Emulator status..." -ForegroundColor Cyan
    
    $container = docker ps --filter "name=$containerName" --format "{{.Status}}"
    
    if ($container) {
        Write-Host "✅ Cosmos DB Emulator is running" -ForegroundColor Green
        Write-Host "   Status: $container" -ForegroundColor White
        
        # Test endpoint connectivity
        Write-Host "`nTesting endpoint connectivity..." -ForegroundColor Cyan
        try {
            $response = Invoke-WebRequest -Uri "https://localhost:8081/_explorer/emulator.pem" -SkipCertificateCheck -ErrorAction Stop
            Write-Host "✅ Endpoint is accessible" -ForegroundColor Green
        } catch {
            Write-Host "⚠️  Endpoint not yet ready (may still be initializing)" -ForegroundColor Yellow
        }
    } else {
        Write-Host "❌ Cosmos DB Emulator is not running" -ForegroundColor Red
        Write-Host "   Start with: .\cosmos-emulator.ps1 start" -ForegroundColor Cyan
    }
}

function Install-CosmosEmulatorCertificate {
    # Check if running as administrator
    $isAdmin = ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
    
    if (-not $isAdmin) {
        Write-Host "❌ This command requires administrator privileges" -ForegroundColor Red
        Write-Host "   Right-click PowerShell and select 'Run as Administrator', then try again" -ForegroundColor Yellow
        return
    }
    
    Write-Host "Installing Cosmos DB Emulator certificate..." -ForegroundColor Cyan
    
    # Check if emulator is running
    $container = docker ps --filter "name=$containerName" --format "{{.ID}}"
    if (-not $container) {
        Write-Host "❌ Cosmos DB Emulator is not running. Start it first with: .\cosmos-emulator.ps1 start" -ForegroundColor Red
        return
    }
    
    try {
        # Download certificate
        Write-Host "Downloading certificate..." -ForegroundColor Cyan
        $certPath = "$env:TEMP\cosmos-emulator.crt"
        Invoke-WebRequest -Uri "https://localhost:8081/_explorer/emulator.pem" -OutFile $certPath -SkipCertificateCheck
        
        # Import certificate to Trusted Root
        Write-Host "Installing certificate to Trusted Root Certification Authorities..." -ForegroundColor Cyan
        Import-Certificate -FilePath $certPath -CertStoreLocation Cert:\LocalMachine\Root
        
        # Clean up
        Remove-Item $certPath -ErrorAction SilentlyContinue
        
        Write-Host "✅ Certificate installed successfully" -ForegroundColor Green
        Write-Host "   You can now run E2E tests without SSL errors" -ForegroundColor White
    } catch {
        Write-Host "❌ Failed to install certificate: $_" -ForegroundColor Red
    }
}

# Execute requested action
switch ($Action) {
    'start'  { Start-CosmosEmulator }
    'stop'   { Stop-CosmosEmulator }
    'status' { Get-CosmosEmulatorStatus }
    'cert'   { Install-CosmosEmulatorCertificate }
}
