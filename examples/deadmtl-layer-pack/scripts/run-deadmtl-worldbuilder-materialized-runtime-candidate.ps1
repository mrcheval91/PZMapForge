#Requires -Version 5.1
<#
.SYNOPSIS
MAP-32A: Stage the first DeadMTL materialized sandbox runtime candidate from MAP-31B data.

Consumes:
  MAP-29C lot-fill JSON (run its helper first if missing)
  MAP-31B footprint candidate JSON (run its helper first if missing)

Produces under:
  .local\deadmtl-authoring\map32a-materialized-runtime-candidate\deadmtl_map32a_candidate\

  mod.info
  media\maps\DeadMTL_MAP32A\map.info
  media\maps\DeadMTL_MAP32A\spawnpoints.lua
  media\maps\DeadMTL_MAP32A\objects.lua
  manifest JSON, checks CSV, summary TXT

Claim boundary:
  sandbox_only=true
  binary_cell_materialized=false (Path B - no binary writer available, gap recorded)
  runtime_proof_claimed=false
  playable_export_claimed=false
  NOT a playable Project Zomboid export until a real in-game load test is performed.
#>
Set-StrictMode -Version Latest

$ScriptDir     = $PSScriptRoot
$RepoRoot      = (Resolve-Path (Join-Path $ScriptDir "..\..\..\")).Path
$CliProject    = Join-Path $RepoRoot "src\PZMapForge.Cli\PZMapForge.Cli.csproj"

$LotFillJson   = Join-Path $RepoRoot ".local\deadmtl-authoring\worldbuilder-residential-blue-quadrilateral-lot-fill\map_00\map_00.blue_lot_fill.json"
$FootprintJson = Join-Path $RepoRoot ".local\deadmtl-authoring\worldbuilder-parcel-building-footprint-candidates\map_00\deadmtl-worldbuilder-parcel-building-footprint-candidates.json"
$OutputRoot    = Join-Path $RepoRoot ".local\deadmtl-authoring\map32a-materialized-runtime-candidate\deadmtl_map32a_candidate"

$OutputManifest  = Join-Path $OutputRoot "deadmtl-worldbuilder-materialized-runtime-candidate-manifest.json"
$OutputChecksCsv = Join-Path $OutputRoot "deadmtl-worldbuilder-materialized-runtime-candidate-checks.csv"
$Summary         = Join-Path $OutputRoot "deadmtl-worldbuilder-materialized-runtime-candidate-summary.txt"

# Guard: lot-fill JSON must exist (run MAP-29C helper first if missing)
if (-not (Test-Path $LotFillJson)) {
    Write-Error ("MAP-29C lot-fill JSON not found at:`n  $LotFillJson`n" +
                 "Run run-deadmtl-worldbuilder-residential-blue-quadrilateral-lot-fill.ps1 first.")
    exit 1
}

# Guard: footprint JSON must exist (run MAP-31B helper first if missing)
if (-not (Test-Path $FootprintJson)) {
    Write-Error ("MAP-31B footprint JSON not found at:`n  $FootprintJson`n" +
                 "Run run-deadmtl-worldbuilder-parcel-building-footprint-candidates.ps1 first.")
    exit 1
}

New-Item -ItemType Directory -Path $OutputRoot -Force | Out-Null

dotnet run --project $CliProject -- `
    deadmtl-build-worldbuilder-materialized-runtime-candidate `
    --lot-fill-json     $LotFillJson `
    --footprint-json    $FootprintJson `
    --output-root       $OutputRoot `
    --output-manifest   $OutputManifest `
    --output-checks-csv $OutputChecksCsv `
    --summary           $Summary

if ($LASTEXITCODE -ne 0) {
    Write-Error "deadmtl-build-worldbuilder-materialized-runtime-candidate failed (exit $LASTEXITCODE)"
    exit 1
}

Write-Host ""
Write-Host "=== MAP-32A Staged Runtime Candidate ==="
Write-Host "Output root  : $OutputRoot"
Write-Host "Manifest     : $OutputManifest"

# Print materialization status from manifest
if (Test-Path $OutputManifest) {
    $manifest = Get-Content $OutputManifest -Raw | ConvertFrom-Json
    Write-Host "Lot count                : $($manifest.lot_count)"
    Write-Host "Footprint count          : $($manifest.footprint_count)"
    Write-Host "Skipped lot count        : $($manifest.skipped_lot_count)"
    Write-Host "Binary cell materialized : $($manifest.binary_cell_materialized)"
    if ($manifest.binary_cell_materialized -eq $false) {
        Write-Host "Binary gap               : $($manifest.binary_materialization_gap)"
    }
    Write-Host "Checks                   : $($manifest.passed_check_count)/$($manifest.check_count) PASS"
    Write-Host "Verdict                  : $($manifest.verdict)"
}

Write-Host ""
Write-Host "=== Runtime Status ==="
Write-Host "Runtime proof   : FALSE - no PZ in-game load test has been performed"
Write-Host "Playable export : NOT CLAIMED - sandbox staging only"
Write-Host "Public package  : NOT CLAIMED"
Write-Host "Workshop write  : NOT PERFORMED"
Write-Host "PZ install write: NOT PERFORMED"
