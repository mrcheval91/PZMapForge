#Requires -Version 5.1
<#
.SYNOPSIS
MAP-35A: Stage, install, or collect logs for the DeadMTL visible-cell runtime candidate.

Parameters:
  -StageOnly      (default) Stage binary files from repo-owned Build 42 candidate source
                  into the .local staging area. Does not install.
  -InstallOnly    Stage then install candidate into local Zomboid mods folder.
  -CollectLogs    Collect and analyze PZ console/debug logs after a manual test run.
                  Does NOT reinstall.
  -OperatorObservation  Free-text description of what the operator observed in-game.

Source (priority order):
  1. C:\Users\Palmacede\Zomboid\mods\pzmapforge_build42_candidate_v4_001\common\media\maps\pzmapforge_build42_candidate_v4_001
  2. .local\map7y-packet\... (fallback)

Installs to:
  C:\Users\Palmacede\Zomboid\mods\deadmtl_map35a_visible_cell_candidate\

Collect-logs mode:
  Does NOT reinstall or overwrite the installed mod folder.
  If installed folder is missing, fails with MAP35A_COLLECT_REJECTED_NOT_INSTALLED.

Claim boundary:
  binary_cell_materialized=true
  visible_cell_candidate=true
  geometry_from_map31b_materialized=false
  runtime_proof_claimed=false unless logs prove PZ loaded with visible terrain
  playable_export_claimed=false unless spawn/visible terrain proven
  No Workshop upload. No Steam install write. No public release.

Source rejection policy:
  Any source path containing "Dru", "Dru_map", "workshop donor", or "third-party" is rejected.
  Only REPO_OWNED_LOCAL_GENERATED_PZMAPFORGE_BUILD42_CANDIDATE sources are accepted.
#>
param(
    [switch]$StageOnly,
    [switch]$InstallOnly,
    [switch]$CollectLogs,
    [string]$OperatorObservation = ""
)

Set-StrictMode -Version Latest

$ScriptDir  = $PSScriptRoot
$RepoRoot   = (Resolve-Path (Join-Path $ScriptDir "..\..\..\")).Path
$CliProject = Join-Path $RepoRoot "src\PZMapForge.Cli\PZMapForge.Cli.csproj"

$PrimarySourceRoot  = "C:\Users\Palmacede\Zomboid\mods\pzmapforge_build42_candidate_v4_001\common\media\maps\pzmapforge_build42_candidate_v4_001"
$FallbackSourceRoot = Join-Path $RepoRoot ".local\map7y-packet\common\media\maps\pzmapforge_map7y_cell"
$LocalModsRoot      = "C:\Users\Palmacede\Zomboid\mods"
$ZomboidUserRoot    = "C:\Users\Palmacede\Zomboid"

$OutputRoot   = Join-Path $RepoRoot ".local\deadmtl-authoring\map35a-visible-cell-runtime-candidate"
$OutputResult = Join-Path $OutputRoot "deadmtl-worldbuilder-visible-cell-runtime-candidate-result.json"
$ChecksCsv    = Join-Path $OutputRoot "deadmtl-worldbuilder-visible-cell-runtime-candidate-checks.csv"
$Summary      = Join-Path $OutputRoot "deadmtl-worldbuilder-visible-cell-runtime-candidate-summary.txt"

# Guard: local mods root must exist
if (-not (Test-Path $LocalModsRoot)) {
    Write-Error "Local Zomboid mods folder not found: $LocalModsRoot"
    exit 1
}

New-Item -ItemType Directory -Path $OutputRoot -Force | Out-Null

# Determine mode
$isInstallOnly = $InstallOnly.IsPresent
$isCollectLogs = $CollectLogs.IsPresent

$cliArgs = @(
    "--primary-source-root",   $PrimarySourceRoot,
    "--fallback-source-root",  $FallbackSourceRoot,
    "--local-mods-root",       $LocalModsRoot,
    "--output-root",           $OutputRoot,
    "--output-result",         $OutputResult,
    "--output-checks-csv",     $ChecksCsv,
    "--summary",               $Summary,
    "--zomboid-user-root",     $ZomboidUserRoot
)
if ($isInstallOnly) { $cliArgs += "--install-only" }
if ($isCollectLogs) { $cliArgs += "--collect-logs" }
if ($OperatorObservation) {
    $cliArgs += "--operator-observation"
    $cliArgs += $OperatorObservation
}

dotnet run --project $CliProject -- deadmtl-build-worldbuilder-visible-cell-runtime-candidate @cliArgs

if ($LASTEXITCODE -ne 0) {
    Write-Error "deadmtl-build-worldbuilder-visible-cell-runtime-candidate failed (exit $LASTEXITCODE)"
    exit 1
}

Write-Host ""
Write-Host "=== MAP-35B Visible Cell Runtime Candidate ==="

if (Test-Path $OutputResult) {
    $result = Get-Content $OutputResult -Raw | ConvertFrom-Json
    Write-Host "Selected source root          : $($result.selected_source_root)"
    Write-Host "Source classification         : $($result.selected_source_classification)"
    Write-Host "Source size advantage         : $($result.source_size_advantage_over_map33a)"
    Write-Host "B42 layout written            : $($result.b42_layout_written)"
    Write-Host "Staged candidate root         : $($result.staged_candidate_root)"
    Write-Host "Installed candidate root      : $($result.installed_candidate_root)"
    Write-Host "Stage performed               : $($result.stage_performed)"
    Write-Host "Install performed             : $($result.install_performed)"
    Write-Host "Install marker written        : $($result.install_marker_written)"
    Write-Host "Binary cell materialized      : $($result.binary_cell_materialized)"
    Write-Host "Visible cell candidate        : $($result.visible_cell_candidate)"
    Write-Host "Geometry from MAP-31B         : $($result.geometry_from_map31b_materialized)"
    Write-Host "Log collection attempted      : $($result.runtime_log_collection_attempted)"
    if ($result.runtime_log_collection_attempted) {
        Write-Host "Installed candidate present   : $($result.installed_candidate_present)"
        Write-Host "Installed binary files present: $($result.installed_binary_files_present)"
        Write-Host "Installed marker present      : $($result.installed_marker_present)"
        Write-Host "Visible-cell proof observed   : $($result.runtime_visible_cell_proof_observed)"
        if ($result.runtime_visible_cell_proof_source) {
            Write-Host "Visible-cell proof source     : $($result.runtime_visible_cell_proof_source)"
        }
    }
    Write-Host "Runtime classification        : $($result.runtime_classification)"
    Write-Host "Checks                        : $($result.passed_check_count)/$($result.check_count) PASS"
    Write-Host "Verdict                       : $($result.verdict)"
}

Write-Host ""
Write-Host "=== Claim Boundary ==="
if (Test-Path $OutputResult) {
    $result = Get-Content $OutputResult -Raw | ConvertFrom-Json
    if ($result.runtime_visible_cell_proof_observed) {
        Write-Host "Visible-cell runtime proof observed : TRUE"
        Write-Host "Runtime proof claimed               : FALSE - not promoted to playable/final claim"
    } else {
        Write-Host "Visible-cell runtime proof observed : FALSE - in-game visible terrain not yet confirmed"
        Write-Host "Runtime proof claimed               : FALSE"
    }
} else {
    Write-Host "Visible-cell runtime proof observed : FALSE - result not available"
    Write-Host "Runtime proof claimed               : FALSE"
}
Write-Host "Playable export claimed  : FALSE - gated until in-game visible terrain proven"
Write-Host "Geometry from MAP-31B    : FALSE - source is PZMapForge Build 42 candidate"
Write-Host "Live Workshop write      : NOT PERFORMED"
Write-Host "Steam install write      : NOT PERFORMED"

if ($isCollectLogs -and (Test-Path $OutputResult)) {
    $result = Get-Content $OutputResult -Raw | ConvertFrom-Json
    Write-Host ""
    Write-Host "=== MAP-35B Log Classification ==="
    Write-Host "Collect mode (no stage/install) : $($result.collect_logs_mode_does_not_stage_or_install)"
    Write-Host "Installed candidate present     : $($result.installed_candidate_present)"
    Write-Host "Installed binary files present  : $($result.installed_binary_files_present)"
    Write-Host "Installed marker present        : $($result.installed_marker_present)"
    Write-Host "Binary cell materialized        : $($result.binary_cell_materialized)"
    Write-Host "Logs found               : $($result.runtime_logs_found)"
    Write-Host "Candidate mod loaded     : $($result.candidate_mod_loaded)"
    Write-Host "Binary files mounted     : $($result.candidate_binary_files_mounted)"
    Write-Host "MapGroup registered      : $($result.candidate_mapgroup_registered)"
    Write-Host "Spawn blocker absent     : $($result.candidate_spawn_blocker_absent)"
    Write-Host "Binary chunk attempted   : $($result.candidate_binary_chunk_load_attempted)"
    Write-Host "Visible terrain detected : $($result.visible_terrain_detected)"
    Write-Host "Empty/fallback terrain   : $($result.fallback_empty_terrain_detected)"
    Write-Host "Unrelated errors found   : $($result.unrelated_errors_found)"
    if ($result.operator_observation) {
        Write-Host "Operator observation     : $($result.operator_observation)"
    }
    Write-Host "Visible-cell proof observed : $($result.runtime_visible_cell_proof_observed)"
    Write-Host "Classification           : $($result.runtime_classification)"
    if ($result.runtime_classification -eq "MAP35A_RUNTIME_VISIBLE_CELL_PASS") {
        Write-Host "RESULT: VISIBLE CELL PASS - terrain loaded and visible in-game."
        Write-Host "NOTE: Visible-cell runtime proof observed = TRUE."
        Write-Host "NOTE: runtime_proof_claimed remains FALSE - not promoted to playable/final claim."
        Write-Host "NOTE: geometry_from_map31b_materialized=false - source is PZMapForge Build 42 candidate."
    } elseif ($result.runtime_classification -eq "MAP35A_RUNTIME_PARTIAL_PASS_EMPTY_FALLBACK_TERRAIN") {
        Write-Host "RESULT: Partial pass - mod mounted, binary files loaded, player entered world (empty/fallback terrain)."
        Write-Host "NOTE: Larger chunkdata may not be sufficient alone - map may require additional cell data."
    } elseif ($result.runtime_classification -eq "MAP35A_RUNTIME_SPAWN_BLOCKED") {
        Write-Host "RESULT: Spawn blocker detected - spawn table or -1 square error present."
    } elseif ($result.runtime_classification -eq "MAP35A_RUNTIME_MOD_NOT_LOADED") {
        Write-Host "RESULT: Mod not loaded - no 'loading DeadMTL_MAP35A' found in logs."
    } elseif ($result.runtime_classification -eq "MAP35A_RUNTIME_MAPGROUP_MISSING") {
        Write-Host "RESULT: MapGroup not registered in logs."
    } elseif ($result.runtime_classification -eq "MAP35A_RUNTIME_CANDIDATE_SPECIFIC_FAIL") {
        Write-Host "RESULT: Candidate-specific fatal error detected in logs."
    } else {
        Write-Host "RESULT: Evidence insufficient - logs do not conclusively classify the run."
    }
} elseif (-not $isCollectLogs) {
    Write-Host ""
    Write-Host "=== Manual PZ Test Steps ==="
    Write-Host "1. Launch Project Zomboid Build 42."
    Write-Host "2. Go to Mods menu and enable: DeadMTL MAP35A Visible Cell Runtime Candidate"
    Write-Host "   (mod ID: DeadMTL_MAP35A)"
    Write-Host "3. Start a new sandbox game."
    Write-Host "4. Check if DeadMTL_MAP35A appears as a map/spawn option."
    Write-Host "5. Try spawning at the Unemployed spawn point (worldX=35, worldY=27)."
    Write-Host "6. Observe whether visible terrain loads (roads, buildings, vegetation)"
    Write-Host "   or whether terrain is empty/field/fallback."
    Write-Host "7. Exit Project Zomboid."
    Write-Host "8. Re-run this helper with -CollectLogs and -OperatorObservation:"
    Write-Host "   powershell -ExecutionPolicy Bypass -File run-deadmtl-worldbuilder-visible-cell-runtime-candidate.ps1 -CollectLogs -OperatorObservation ""describe what you saw"""
}
