<#
.SYNOPSIS
    Scans public markdown files for accidental Tier 3 confidential detail leakage.

.DESCRIPTION
    Checks markdown files at the repo root and in docs/ for patterns that suggest
    rate limit values, blocklist patterns, system prompt references, or internal
    API endpoint details that should not appear in public documentation.

    Excludes .copilot-tracking/ (planning files may reference patterns legitimately).

.EXAMPLE
    pwsh scripts/check-ai-bom-safety.ps1
#>

[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'

$patterns = @(
    @{ Pattern = '\b\d+\s*(req|requests?)\s*[/\\]\s*(min|minute)'; Description = 'Rate limit value (requests per minute)' }
    @{ Pattern = 'MaxToolCallsPerSession\s*=\s*\d+'; Description = 'Tool call limit configuration' }
    @{ Pattern = 'MaxConcurrentJobs\s*=\s*\d+'; Description = 'Concurrent job limit configuration' }
    @{ Pattern = 'MaxHydrationMessageCount\s*=\s*\d+'; Description = 'Conversation hydration limit' }
    @{ Pattern = 'MaxMessageContentLength\s*=\s*\d+'; Description = 'Message content length limit' }
    @{ Pattern = 'MaxMessagesPerConversation\s*=\s*\d+'; Description = 'Messages per conversation limit' }
    @{ Pattern = 'FixedWindowRateLimiter'; Description = 'Rate limiter implementation detail' }
    @{ Pattern = 'InjectionPatterns'; Description = 'Prompt injection blocklist reference' }
    @{ Pattern = 'DangerousCodePatterns'; Description = 'Code validation blocklist reference' }
    @{ Pattern = 'ChatSystemPrompt'; Description = 'System prompt configuration reference' }
    @{ Pattern = 'ReviewerSystemPrompt'; Description = 'Reviewer prompt configuration reference' }
    @{ Pattern = 'ReportGeneratorSystemPrompt'; Description = 'Report generator prompt configuration reference' }
    @{ Pattern = 'X-Api-Key\s*[:=]'; Description = 'API key header with value' }
    @{ Pattern = 'Ocp-Apim-Subscription-Key\s*[:=]'; Description = 'APIM subscription key with value' }
    @{ Pattern = '\.azurecr\.io'; Description = 'Azure Container Registry endpoint' }
    @{ Pattern = '\.azure-api\.net'; Description = 'Azure API Management endpoint' }
    @{ Pattern = 'MaxArtifactSizeBytes\s*='; Description = 'Artifact size limit configuration' }
    @{ Pattern = 'ReportGenerationTimeoutMinutes\s*='; Description = 'Report timeout configuration' }
)

# Only scan public-facing markdown files.
# Exclude internal directories that legitimately discuss implementation details.
$excludeDirs = @(
    '.copilot-tracking'
    'node_modules'
    '.git'
    'sbom-output'
)

# Internal docs directories where implementation details are expected
$excludeDocDirs = @(
    'blog-post-ideas'
    'decision-records'
    'plans'
    'presentation-notes'
    'reports'
    'scratch'
    'standards'
    'templates'
)

$files = Get-ChildItem -Path . -Filter '*.md' -Recurse | Where-Object {
    $rel = $_.FullName.Replace((Get-Location).Path + [IO.Path]::DirectorySeparatorChar, '')

    # Exclude top-level hidden/internal dirs
    $excluded = $false
    foreach ($dir in $excludeDirs) {
        if ($rel -like "$dir*") { $excluded = $true; break }
    }

    # Exclude docs subdirectories (internal documentation)
    if (-not $excluded) {
        $sep = [IO.Path]::DirectorySeparatorChar
        foreach ($docDir in $excludeDocDirs) {
            if ($rel.StartsWith("docs$sep$docDir")) { $excluded = $true; break }
        }
    }

    # Exclude src/ directory (service READMEs with implementation details)
    if (-not $excluded -and $rel.StartsWith("src$([IO.Path]::DirectorySeparatorChar)")) { $excluded = $true }

    -not $excluded
}

Write-Host "Scanning $($files.Count) markdown files for Tier 3 detail leakage..."

$violations = @()

foreach ($file in $files) {
    $relativePath = $file.FullName.Replace((Get-Location).Path + [IO.Path]::DirectorySeparatorChar, '')
    $lines = Get-Content $file.FullName

    for ($i = 0; $i -lt $lines.Count; $i++) {
        foreach ($check in $patterns) {
            if ($lines[$i] -match $check.Pattern) {
                $violations += [PSCustomObject]@{
                    File        = $relativePath
                    Line        = $i + 1
                    Pattern     = $check.Description
                    Content     = $lines[$i].Trim().Substring(0, [Math]::Min(80, $lines[$i].Trim().Length))
                }
            }
        }
    }
}

if ($violations.Count -gt 0) {
    Write-Host ""
    Write-Host "AI-BOM safety check FAILED - $($violations.Count) potential Tier 3 detail(s) found:" -ForegroundColor Red
    Write-Host ""
    $violations | Format-Table -AutoSize -Property File, Line, Pattern, Content
    exit 1
}

Write-Host "AI-BOM safety check PASSED - no Tier 3 details found in $($files.Count) markdown files." -ForegroundColor Green
exit 0
