#Requires -Version 5.1
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

# MAP-25A: DeadMTL WorldBuilder Neighborhood Profile Contract Validation
# Contract only. No terrain generation. No building placement.
# No lotpack writing. No worldgen override file. No runtime proof.

$RepoRoot = $PSScriptRoot | Split-Path -Parent | Split-Path -Parent | Split-Path -Parent

$ProfilePath = Join-Path $RepoRoot "examples\deadmtl-layer-pack\worldbuilder\neighborhoods\deadmtl_baseline_neighborhood_profile.json"

$OutDir     = Join-Path $RepoRoot ".local\deadmtl-authoring\worldbuilder-neighborhood-profile\deadmtl_baseline"
$OutJson    = Join-Path $OutDir "deadmtl_baseline.neighborhood_profile.validation.json"
$OutMd      = Join-Path $OutDir "deadmtl_baseline.neighborhood_profile.validation.md"
$OutCsv     = Join-Path $OutDir "deadmtl_baseline.neighborhood_profile.validation.csv"
$OutSummary = Join-Path $OutDir "deadmtl_baseline.neighborhood_profile.validation.summary.txt"

$CliProject = Join-Path $RepoRoot "src\PZMapForge.Cli\PZMapForge.Cli.csproj"

if (-not (Test-Path $CliProject)) {
    Write-Error "CLI project not found: $CliProject"
    exit 1
}

Write-Host "MAP-25A: Validating WorldBuilder neighborhood profile..."
Write-Host "  profile:     $ProfilePath"
Write-Host "  output-json: $OutJson"

dotnet run --project $CliProject --configuration Release --no-build -- `
    deadmtl-validate-worldbuilder-neighborhood-profile `
    --profile     $ProfilePath `
    --output-json $OutJson `
    --output-md   $OutMd `
    --output-csv  $OutCsv `
    --summary     $OutSummary

if ($LASTEXITCODE -ne 0) {
    Write-Error "deadmtl-validate-worldbuilder-neighborhood-profile failed (exit $LASTEXITCODE)"
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

# VERDICT: MAP25A_WORLDBUILDER_NEIGHBORHOOD_PROFILE_CONTRACT_COMPLETE
