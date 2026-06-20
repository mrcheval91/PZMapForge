#Requires -Version 5.1
<#
.SYNOPSIS
MAP-30A: Generate parcel building footprint candidates from MAP-29C lot-fill output.
Reads the lot-fill JSON and a building footprint policy JSON, computes setback-aware
footprint rectangles inside each lot, and produces JSON/CSV/PNG/HTML proof artifacts.

Outputs under:
  .local\deadmtl-authoring\worldbuilder-parcel-building-footprint-candidates\map_00\

This is a sandbox-only planning artifact. NOT a playable Project Zomboid export.
NOT .lotpack / .lotheader / .lua / .bin. Source PNG is never mutated.
#>
Set-StrictMode -Version Latest

$ScriptDir       = $PSScriptRoot
$RepoRoot        = (Resolve-Path (Join-Path $ScriptDir "..\..\..\")).Path
$CliProject      = Join-Path $RepoRoot "src\PZMapForge.Cli\PZMapForge.Cli.csproj"
$OutputRoot      = Join-Path $RepoRoot ".local\deadmtl-authoring\worldbuilder-parcel-building-footprint-candidates\map_00"

$LotFillJson     = Join-Path $RepoRoot ".local\deadmtl-authoring\worldbuilder-residential-blue-quadrilateral-lot-fill\map_00\map_00.blue_lot_fill.json"
$FootprintPolicy = Join-Path $RepoRoot "examples\deadmtl-layer-pack\worldbuilder\parcel-building-footprint-policies.json"

$OutputJson      = Join-Path $OutputRoot "deadmtl-worldbuilder-parcel-building-footprint-candidates.json"
$OutputCsv       = Join-Path $OutputRoot "deadmtl-worldbuilder-parcel-building-footprint-candidates.csv"
$OutputChkCsv    = Join-Path $OutputRoot "deadmtl-worldbuilder-parcel-building-footprint-candidates-checks.csv"
$OutputPng       = Join-Path $OutputRoot "deadmtl-worldbuilder-parcel-building-footprint-candidates.png"
$OutputHtml      = Join-Path $OutputRoot "deadmtl-worldbuilder-parcel-building-footprint-candidates.html"
$Summary         = Join-Path $OutputRoot "deadmtl-worldbuilder-parcel-building-footprint-candidates-summary.txt"

if (-not (Test-Path $LotFillJson)) {
    Write-Error ("MAP-29C lot-fill JSON not found at:`n  $LotFillJson`n" +
                 "Run run-deadmtl-worldbuilder-residential-blue-quadrilateral-lot-fill.ps1 first.")
}

New-Item -ItemType Directory -Path $OutputRoot -Force | Out-Null

dotnet run --project $CliProject -- `
    deadmtl-build-worldbuilder-parcel-building-footprint-candidates `
    --lot-fill-json             $LotFillJson `
    --building-footprint-policy $FootprintPolicy `
    --output-root               $OutputRoot `
    --output-json               $OutputJson `
    --output-csv                $OutputCsv `
    --output-checks-csv         $OutputChkCsv `
    --output-png                $OutputPng `
    --output-html               $OutputHtml `
    --summary                   $Summary

if ($LASTEXITCODE -ne 0) {
    Write-Error "deadmtl-build-worldbuilder-parcel-building-footprint-candidates failed (exit $LASTEXITCODE)"
}

Write-Host ""
Write-Host "Outputs written to: $OutputRoot"
