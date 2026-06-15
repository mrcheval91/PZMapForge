#Requires -Version 5.1
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

# MAP-24A: DeadMTL Vanilla Building Source Discovery
# Discovery only. No building extraction claimed. No editable vanilla catalogue claimed.
# No runtime proof. Not writer-ready.

$RepoRoot = $PSScriptRoot | Split-Path -Parent | Split-Path -Parent | Split-Path -Parent

$OutDir     = Join-Path $RepoRoot ".local\deadmtl-authoring\vanilla-building-source-discovery"
$OutJson    = Join-Path $OutDir "vanilla_building_source_discovery.json"
$OutMd      = Join-Path $OutDir "vanilla_building_source_discovery.md"
$OutCsv     = Join-Path $OutDir "vanilla_building_source_discovery.csv"
$OutSummary = Join-Path $OutDir "vanilla_building_source_discovery.summary.txt"

$CliProject = Join-Path $RepoRoot "src\PZMapForge.Cli\PZMapForge.Cli.csproj"

if (-not (Test-Path $CliProject)) {
    Write-Error "CLI project not found: $CliProject"
    exit 1
}

Write-Host "MAP-24A: Discovering vanilla building sources..."
Write-Host "  output-json: $OutJson"

dotnet run --project $CliProject --configuration Release --no-build -- `
    deadmtl-discover-vanilla-building-sources `
    --output-json $OutJson `
    --output-md   $OutMd `
    --output-csv  $OutCsv `
    --summary     $OutSummary

if ($LASTEXITCODE -ne 0) {
    Write-Error "deadmtl-discover-vanilla-building-sources failed (exit $LASTEXITCODE)"
    exit $LASTEXITCODE
}

Write-Host ""
Write-Host "Output files:"
Write-Host "  JSON:    $OutJson"
Write-Host "  MD:      $OutMd"
Write-Host "  CSV:     $OutCsv"
Write-Host "  Summary: $OutSummary"
Write-Host ""

if (Test-Path $OutSummary) {
    Get-Content $OutSummary | Write-Host
}

# VERDICT: MAP24A_VANILLA_BUILDING_SOURCE_DISCOVERY_COMPLETE
