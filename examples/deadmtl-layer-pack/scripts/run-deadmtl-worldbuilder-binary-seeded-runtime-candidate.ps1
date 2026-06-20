#Requires -Version 5.1
<#
.SYNOPSIS
MAP-33A: Seed the first DeadMTL binary-materialized sandbox runtime candidate
from repo-owned MAP-7Y sidecar stub binary cell files.

Consumes:
  MAP-32A manifest JSON (runs MAP-32A helper first if missing)
  Binary seed root: MAP-7Y sidecar stub (pzmapforge_build42_candidate_v4_001)

Produces under:
  .local\deadmtl-authoring\map33a-binary-seeded-runtime-candidate\deadmtl_map33a_candidate\

  mod.info
  media\maps\DeadMTL_MAP33A\map.info
  media\maps\DeadMTL_MAP33A\spawnpoints.lua
  media\maps\DeadMTL_MAP33A\objects.lua
  media\maps\DeadMTL_MAP33A\35_27.lotheader          (binary seed)
  media\maps\DeadMTL_MAP33A\world_35_27.lotpack      (binary seed)
  media\maps\DeadMTL_MAP33A\chunkdata_35_27.bin      (binary seed)
  media\maps\DeadMTL_MAP33A\streets.xml.bin          (optional sidecar)
  media\maps\DeadMTL_MAP33A\worldmap.xml.bin         (optional sidecar)
  media\maps\DeadMTL_MAP33A\worldmap-forest.xml.bin  (optional sidecar)
  manifest JSON, checks CSV, summary TXT

Claim boundary:
  sandbox_only=true
  binary_cell_materialized=true (repo-owned MAP-7Y sidecar seed files copied)
  geometry_from_map31b_materialized=false (seed is MAP-7Y sidecar, not MAP-31B encoded geometry)
  runtime_proof_claimed=false
  playable_export_claimed=false
  NOT a playable Project Zomboid export until a real in-game load test is performed.
#>
Set-StrictMode -Version Latest

$ScriptDir  = $PSScriptRoot
$RepoRoot   = (Resolve-Path (Join-Path $ScriptDir "..\..\..\")).Path
$CliProject = Join-Path $RepoRoot "src\PZMapForge.Cli\PZMapForge.Cli.csproj"

$Map32AManifest = Join-Path $RepoRoot ".local\deadmtl-authoring\map32a-materialized-runtime-candidate\deadmtl_map32a_candidate\deadmtl-worldbuilder-materialized-runtime-candidate-manifest.json"
$BinarySeedRoot = Join-Path $RepoRoot ".local\map7y-packet\staged-workshop-sidecar-stubs\pzmapforge_build42_candidate_v4_001\common\media\maps\pzmapforge_build42_candidate_v4_001"
$OutputRoot     = Join-Path $RepoRoot ".local\deadmtl-authoring\map33a-binary-seeded-runtime-candidate\deadmtl_map33a_candidate"

$OutputManifest  = Join-Path $OutputRoot "deadmtl-worldbuilder-binary-seeded-runtime-candidate-manifest.json"
$OutputChecksCsv = Join-Path $OutputRoot "deadmtl-worldbuilder-binary-seeded-runtime-candidate-checks.csv"
$Summary         = Join-Path $OutputRoot "deadmtl-worldbuilder-binary-seeded-runtime-candidate-summary.txt"

# Guard: run MAP-32A helper first if manifest is missing
if (-not (Test-Path $Map32AManifest)) {
    Write-Host "MAP-32A manifest not found. Running MAP-32A helper first..."
    $map32AHelper = Join-Path $ScriptDir "run-deadmtl-worldbuilder-materialized-runtime-candidate.ps1"
    powershell -ExecutionPolicy Bypass -File $map32AHelper
    if ($LASTEXITCODE -ne 0) {
        Write-Error "MAP-32A helper failed (exit $LASTEXITCODE). Cannot continue MAP-33A."
        exit 1
    }
}

if (-not (Test-Path $Map32AManifest)) {
    Write-Error "MAP-32A manifest still missing after running helper: $Map32AManifest"
    exit 1
}

# Guard: binary seed root must exist
if (-not (Test-Path $BinarySeedRoot)) {
    Write-Error "Binary seed root not found: $BinarySeedRoot"
    Write-Error "Expected: repo-owned MAP-7Y sidecar stub pzmapforge_build42_candidate_v4_001 under .local\map7y-packet\"
    exit 1
}

New-Item -ItemType Directory -Path $OutputRoot -Force | Out-Null

dotnet run --project $CliProject -- `
    deadmtl-build-worldbuilder-binary-seeded-runtime-candidate `
    --map32a-manifest    $Map32AManifest `
    --binary-seed-root   $BinarySeedRoot `
    --output-root        $OutputRoot `
    --output-manifest    $OutputManifest `
    --output-checks-csv  $OutputChecksCsv `
    --summary            $Summary

if ($LASTEXITCODE -ne 0) {
    Write-Error "deadmtl-build-worldbuilder-binary-seeded-runtime-candidate failed (exit $LASTEXITCODE)"
    exit 1
}

Write-Host ""
Write-Host "=== MAP-33A Binary-Seeded Runtime Candidate ==="
Write-Host "Output root      : $OutputRoot"
Write-Host "Binary seed root : $BinarySeedRoot"

if (Test-Path $OutputManifest) {
    $manifest = Get-Content $OutputManifest -Raw | ConvertFrom-Json
    Write-Host "Lot count                        : $($manifest.lot_count)"
    Write-Host "Footprint count                  : $($manifest.footprint_count)"
    Write-Host "Skipped lot count                : $($manifest.skipped_lot_count)"
    Write-Host "Binary seed files written        : $($manifest.binary_seed_files_written.Count)"
    foreach ($path in $manifest.binary_seed_files_written) {
        $name = Split-Path $path -Leaf
        $sha  = if ($manifest.binary_seed_file_sha256.$name) { $manifest.binary_seed_file_sha256.$name.Substring(0, 16) + "..." } else { "?" }
        Write-Host "  $name  sha256=$sha"
    }
    Write-Host "Binary cell materialized         : $($manifest.binary_cell_materialized)"
    Write-Host "Geometry from MAP-31B            : $($manifest.geometry_from_map31b_materialized)"
    Write-Host "Checks                           : $($manifest.passed_check_count)/$($manifest.check_count) PASS"
    Write-Host "Verdict                          : $($manifest.verdict)"
}

Write-Host ""
Write-Host "=== Runtime Status ==="
Write-Host "Runtime proof                    : FALSE - no PZ in-game load test has been performed"
Write-Host "Playable export                  : NOT CLAIMED - sandbox staging only"
Write-Host "Public package                   : NOT CLAIMED"
Write-Host "Live Workshop write              : NOT PERFORMED"
Write-Host "Project Zomboid install write    : NOT PERFORMED"
