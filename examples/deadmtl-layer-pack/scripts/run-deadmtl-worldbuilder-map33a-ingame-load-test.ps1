#Requires -Version 5.1
<#
.SYNOPSIS
MAP-34B: Controlled in-game load test installer and log classifier for the MAP-33A binary-seeded runtime candidate.

Parameters:
  -InstallOnly  (default) Install candidate into local Zomboid mods folder and print manual test steps.
  -CollectLogs  Collect and analyze PZ console/debug logs after a manual test run. Does NOT reinstall.
  -OperatorObservation  Free-text description of what the operator observed in-game.

Installs to:
  C:\Users\Palmacede\Zomboid\mods\deadmtl_map33a_candidate\

Collect-logs mode:
  Does NOT reinstall or overwrite the installed mod folder.
  If installed folder is missing, fails with MAP34B_COLLECT_REJECTED_NOT_INSTALLED.

Claim boundary:
  binary_cell_materialized=true (from MAP-33A)
  geometry_from_map31b_materialized=false (MAP-7Y sidecar seed, not MAP-31B geometry)
  runtime_proof_claimed=false unless logs prove PZ loaded the candidate
  playable_export_claimed=false unless spawn/playable terrain is proven
  No Workshop upload. No Steam install write. No public release.
#>
param(
    [switch]$InstallOnly,
    [switch]$CollectLogs,
    [string]$OperatorObservation = ""
)

Set-StrictMode -Version Latest

$ScriptDir  = $PSScriptRoot
$RepoRoot   = (Resolve-Path (Join-Path $ScriptDir "..\..\..\")).Path
$CliProject = Join-Path $RepoRoot "src\PZMapForge.Cli\PZMapForge.Cli.csproj"

$Map33AManifest      = Join-Path $RepoRoot ".local\deadmtl-authoring\map33a-binary-seeded-runtime-candidate\deadmtl_map33a_candidate\deadmtl-worldbuilder-binary-seeded-runtime-candidate-manifest.json"
$SourceCandidateRoot = Join-Path $RepoRoot ".local\deadmtl-authoring\map33a-binary-seeded-runtime-candidate\deadmtl_map33a_candidate"
$LocalModsRoot       = "C:\Users\Palmacede\Zomboid\mods"
$ZomboidUserRoot     = "C:\Users\Palmacede\Zomboid"

$OutputRoot   = Join-Path $RepoRoot ".local\deadmtl-authoring\map34a-ingame-load-test"
$OutputResult = Join-Path $OutputRoot "deadmtl-worldbuilder-map33a-ingame-load-test-result.json"
$ChecksCsv    = Join-Path $OutputRoot "deadmtl-worldbuilder-map33a-ingame-load-test-checks.csv"
$Summary      = Join-Path $OutputRoot "deadmtl-worldbuilder-map33a-ingame-load-test-summary.txt"

# Guard: MAP-33A helper must have run first
if (-not (Test-Path $Map33AManifest)) {
    Write-Host "MAP-33A manifest not found. Running MAP-33A helper first..."
    $map33AHelper = Join-Path $ScriptDir "run-deadmtl-worldbuilder-binary-seeded-runtime-candidate.ps1"
    powershell -ExecutionPolicy Bypass -File $map33AHelper
    if ($LASTEXITCODE -ne 0) {
        Write-Error "MAP-33A helper failed (exit $LASTEXITCODE). Cannot continue MAP-34B."
        exit 1
    }
}

if (-not (Test-Path $Map33AManifest)) {
    Write-Error "MAP-33A manifest still missing: $Map33AManifest"
    exit 1
}

# Guard: source candidate root must exist
if (-not (Test-Path $SourceCandidateRoot)) {
    Write-Error "MAP-33A source candidate root not found: $SourceCandidateRoot"
    exit 1
}

# Guard: local mods root must exist
if (-not (Test-Path $LocalModsRoot)) {
    Write-Error "Local Zomboid mods folder not found: $LocalModsRoot"
    exit 1
}

New-Item -ItemType Directory -Path $OutputRoot -Force | Out-Null

# Determine mode
$isCollectLogs = $CollectLogs.IsPresent

$cliArgs = @(
    "--map33a-manifest",       $Map33AManifest,
    "--source-candidate-root", $SourceCandidateRoot,
    "--local-mods-root",       $LocalModsRoot,
    "--output-root",           $OutputRoot,
    "--output-result",         $OutputResult,
    "--output-checks-csv",     $ChecksCsv,
    "--summary",               $Summary,
    "--zomboid-user-root",     $ZomboidUserRoot
)
if ($isCollectLogs) { $cliArgs += "--collect-logs" }
if ($OperatorObservation) {
    $cliArgs += "--operator-observation"
    $cliArgs += $OperatorObservation
}

dotnet run --project $CliProject -- deadmtl-build-worldbuilder-map33a-ingame-load-test @cliArgs

if ($LASTEXITCODE -ne 0) {
    Write-Error "deadmtl-build-worldbuilder-map33a-ingame-load-test failed (exit $LASTEXITCODE)"
    exit 1
}

Write-Host ""
Write-Host "=== MAP-34B In-Game Load Test ==="

if (Test-Path $OutputResult) {
    $result = Get-Content $OutputResult -Raw | ConvertFrom-Json
    Write-Host "Installed candidate root : $($result.installed_candidate_root)"
    Write-Host "Install performed        : $($result.install_performed)"
    Write-Host "Install marker written   : $($result.install_marker_written)"
    Write-Host "Binary cell materialized : $($result.binary_cell_materialized)"
    Write-Host "Geometry from MAP-31B    : $($result.geometry_from_map31b_materialized)"
    Write-Host "Log collection attempted : $($result.runtime_log_collection_attempted)"
    Write-Host "Runtime classification   : $($result.runtime_classification)"
    Write-Host "Checks                   : $($result.passed_check_count)/$($result.check_count) PASS"
    Write-Host "Verdict                  : $($result.verdict)"
}

Write-Host ""
Write-Host "=== Claim Boundary ==="
Write-Host "Runtime proof claimed    : FALSE - no PZ in-game test has proven load"
Write-Host "Playable export claimed  : FALSE - gated until in-game spawn/terrain proven"
Write-Host "Geometry from MAP-31B    : FALSE - binary seed is MAP-7Y sidecar"
Write-Host "Live Workshop write      : NOT PERFORMED"
Write-Host "Steam install write      : NOT PERFORMED"

if ($isCollectLogs -and (Test-Path $OutputResult)) {
    $result = Get-Content $OutputResult -Raw | ConvertFrom-Json
    Write-Host ""
    Write-Host "=== MAP-34B Log Classification ==="
    Write-Host "Logs found               : $($result.runtime_logs_found)"
    Write-Host "Candidate mod loaded     : $($result.candidate_mod_loaded)"
    Write-Host "Binary files mounted     : $($result.candidate_binary_files_mounted)"
    Write-Host "MapGroup registered      : $($result.candidate_mapgroup_registered)"
    Write-Host "Spawn blocker absent     : $($result.candidate_spawn_blocker_absent)"
    Write-Host "Binary chunk attempted   : $($result.candidate_binary_chunk_load_attempted)"
    Write-Host "Empty/fallback terrain   : $($result.fallback_empty_terrain_detected)"
    Write-Host "Unrelated errors found   : $($result.unrelated_errors_found)"
    if ($result.operator_observation) {
        Write-Host "Operator observation     : $($result.operator_observation)"
    }
    Write-Host "Classification           : $($result.runtime_classification)"
    if ($result.runtime_classification -eq "MAP34B_RUNTIME_PARTIAL_PASS_EMPTY_FALLBACK_TERRAIN") {
        Write-Host "RESULT: Partial pass - mod mounted, binary files loaded, player entered world (empty/fallback terrain)."
        Write-Host "NOTE: runtime_proof_claimed remains FALSE - partial pass, not full playable proof."
        Write-Host "NOTE: geometry_from_map31b_materialized=false - DeadMTL geometry not yet rendered."
    } elseif ($result.runtime_classification -eq "MAP34B_RUNTIME_SPAWN_BLOCKED") {
        Write-Host "RESULT: Spawn blocker detected - spawn table or -1 square error present."
    } elseif ($result.runtime_classification -eq "MAP34B_RUNTIME_MOD_NOT_LOADED") {
        Write-Host "RESULT: Mod not loaded - no 'loading DeadMTL_MAP33A' found in logs."
    } elseif ($result.runtime_classification -eq "MAP34B_RUNTIME_MAPGROUP_MISSING") {
        Write-Host "RESULT: MapGroup not registered in logs."
    } elseif ($result.runtime_classification -eq "MAP34B_RUNTIME_CANDIDATE_SPECIFIC_FAIL") {
        Write-Host "RESULT: Candidate-specific fatal error detected in logs."
    } else {
        Write-Host "RESULT: Evidence insufficient - logs do not conclusively classify the run."
    }
} elseif (-not $isCollectLogs) {
    Write-Host ""
    Write-Host "=== Manual PZ Test Steps ==="
    Write-Host "1. Launch Project Zomboid Build 42."
    Write-Host "2. Go to Mods menu and enable: DeadMTL MAP33A Binary-Seeded Runtime Candidate"
    Write-Host "   (mod ID: DeadMTL_MAP33A)"
    Write-Host "3. Start a new sandbox game."
    Write-Host "4. Check if DeadMTL_MAP33A appears as a map/spawn option."
    Write-Host "5. Try spawning at the Unemployed spawn point (worldX=35, worldY=27)."
    Write-Host "6. Observe whether terrain loads or errors appear."
    Write-Host "7. Exit Project Zomboid."
    Write-Host "8. Re-run this helper with -CollectLogs and -OperatorObservation:"
    Write-Host "   powershell -ExecutionPolicy Bypass -File run-deadmtl-worldbuilder-map33a-ingame-load-test.ps1 -CollectLogs -OperatorObservation ""describe what you saw"""
}
