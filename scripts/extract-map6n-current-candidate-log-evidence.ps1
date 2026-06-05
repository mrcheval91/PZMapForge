#Requires -Version 5.1
<#
.SYNOPSIS
    MAP-6N log triage tool. Reads log files from a provided .local input folder
    and separates current candidate evidence from stale maptest_a evidence.

    Searches for lines referencing the current Build 42 candidate
    (pzmapforge_build42_candidate_001) and related technical patterns.
    Separately counts stale pzmapforge_manual_b42_001_maptest_a lines so they
    cannot be misattributed as current candidate failures.

    Both -InputLogFolder and -Output must be under .local/.
    Refuses to read from or write to any path outside .local/.

    Writes:
      <Output>/map6n-log-triage-report.json
      <Output>/map6n-log-triage-report.md

.PARAMETER InputLogFolder
    Path under .local/ containing copied log files (e.g. console.txt).

.PARAMETER Output
    Path under .local/ for triage report output.

.EXAMPLE
    powershell -ExecutionPolicy Bypass -File scripts\extract-map6n-current-candidate-log-evidence.ps1 `
        -InputLogFolder .local\map6n-logs `
        -Output .local\map6n-triage
#>

param(
    [Parameter(Mandatory=$true)][string]$InputLogFolder,
    [Parameter(Mandatory=$true)][string]$Output
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

# ---------------------------------------------------------------------------
# Path guards — both paths must contain .local as a directory component
# ---------------------------------------------------------------------------

function Assert-LocalPath {
    param([string]$Path, [string]$Label)
    $norm = $Path.Replace('/', '\')
    if ($norm -notmatch '\\\.local(\\|$)') {
        Write-Error "$Label must be under .local/. Got: $Path"
        exit 1
    }
}

Assert-LocalPath $InputLogFolder '-InputLogFolder'
Assert-LocalPath $Output '-Output'

if (-not (Test-Path -LiteralPath $InputLogFolder -PathType Container)) {
    Write-Error "-InputLogFolder does not exist or is not a directory: $InputLogFolder"
    exit 1
}

New-Item -ItemType Directory -Force -Path $Output | Out-Null

# ---------------------------------------------------------------------------
# Search patterns
# ---------------------------------------------------------------------------

$currentCandidateIds = @(
    'pzmapforge_build42_candidate_001',
    'pzmapforge_build42_candidate_001_test'
)

$technicalTerms = @(
    'LOTP', 'LOTH', 'lotheader', 'lotpack', 'chunkdata',
    'IsoLot', 'CellLoader', 'IsoCell', 'Exception', 'ERROR'
)

$stalePattern = 'pzmapforge_manual_b42_001_maptest_a'

# ---------------------------------------------------------------------------
# Scan log files
# ---------------------------------------------------------------------------

$logFiles = @(Get-ChildItem -LiteralPath $InputLogFolder -Recurse -File -ErrorAction SilentlyContinue)

$currentCandidateMatchLines = [System.Collections.Generic.List[object]]::new()
$staleMatchLines            = [System.Collections.Generic.List[object]]::new()
$candidateSpecificExceptionFound = $false

foreach ($file in $logFiles) {
    $lines = @(Get-Content -LiteralPath $file.FullName -ErrorAction SilentlyContinue)
    foreach ($line in $lines) {
        # Stale check first — stale lines are excluded from current candidate analysis
        if ($line -match [regex]::Escape($stalePattern)) {
            $staleMatchLines.Add([PSCustomObject]@{ file = $file.Name; line = $line.Trim() }) | Out-Null
            continue
        }

        # Current candidate check
        $isCandidateLine = $false
        foreach ($id in $currentCandidateIds) {
            if ($line -match [regex]::Escape($id)) {
                $isCandidateLine = $true
                break
            }
        }

        if ($isCandidateLine) {
            $currentCandidateMatchLines.Add([PSCustomObject]@{ file = $file.Name; line = $line.Trim() }) | Out-Null
            # Check for technical exception/error indicators on the same line
            foreach ($term in @('IsoLot', 'CellLoader', 'IsoCell', 'Exception', 'ERROR')) {
                if ($line -match $term) {
                    $candidateSpecificExceptionFound = $true
                    break
                }
            }
        }
    }
}

# ---------------------------------------------------------------------------
# Recommendation
# ---------------------------------------------------------------------------

$resultRecommendation = if ($candidateSpecificExceptionFound) {
    'CURRENT_CANDIDATE_EXCEPTION_FOUND'
} else {
    'LOAD_TEST_INCONCLUSIVE'
}

# ---------------------------------------------------------------------------
# Build report
# ---------------------------------------------------------------------------

$report = [ordered]@{
    schema                           = 'pzmapforge.map6n-log-triage.v0.1'
    candidate                        = 'pzmapforge_build42_candidate_001'
    candidate_source                 = 'MAP-6L/MAP-6M'
    stale_maptest_a_pattern          = $stalePattern
    current_candidate_matches        = $currentCandidateMatchLines.Count
    stale_maptest_a_matches          = $staleMatchLines.Count
    candidate_specific_exception_found = $candidateSpecificExceptionFound
    result_recommendation            = $resultRecommendation
    input_log_folder                 = $InputLogFolder
    input_files_scanned              = $logFiles.Count
    load_test_performed              = $false
    pz_assets_copied                 = $false
    playable_export_claimed          = $false
}

$jsonPath = Join-Path $Output 'map6n-log-triage-report.json'
$mdPath   = Join-Path $Output 'map6n-log-triage-report.md'

$report | ConvertTo-Json -Depth 4 | Set-Content -Path $jsonPath -Encoding UTF8
Write-Output "Triage JSON: $jsonPath"

# ---------------------------------------------------------------------------
# Markdown report
# ---------------------------------------------------------------------------

$currentSection = if ($currentCandidateMatchLines.Count -gt 0) {
    ($currentCandidateMatchLines | ForEach-Object { "  $($_.file): $($_.line)" }) -join "`n"
} else {
    '  (none found)'
}

$staleSection = if ($staleMatchLines.Count -gt 0) {
    ($staleMatchLines | Select-Object -First 10 | ForEach-Object { "  $($_.file): $($_.line)" }) -join "`n"
} else {
    '  (none found)'
}

$md = @"
# MAP-6N Log Triage Report

candidate: $($report.candidate)
candidate_source: $($report.candidate_source)
result_recommendation: $resultRecommendation

## Current candidate matches (count: $($report.current_candidate_matches))

$currentSection

## Stale maptest_a matches excluded (count: $($report.stale_maptest_a_matches))

$staleSection

## Exception detection

candidate_specific_exception_found: $($candidateSpecificExceptionFound.ToString().ToLower())

## Non-claims

- load_test_performed: false
- pz_assets_copied: false
- playable_export_claimed: false
- STALE_MAPTEST_A_LOGS_EXCLUDED: stale maptest_a matches do not count as current candidate failure
- PLAYABLE_EXPORT_CLAIM_ALLOWED=false
"@

Set-Content -Path $mdPath -Value $md -Encoding UTF8
Write-Output "Triage MD:   $mdPath"
Write-Output "result_recommendation: $resultRecommendation"
Write-Output "Done."
