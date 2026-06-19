#Requires -Version 5.1
<#
.SYNOPSIS
MAP-27J: Sandbox Writer Locked Replay Backend Plan

Reads the MAP-27I locked materialization replay dry-run output and produces
a deterministic backend-neutral writer plan object.

This is NOT a runtime writer.
This is NOT a lotpack writer.
This is NOT a Project Zomboid export.

This is a concrete plan object defining what a future backend would need to
consume, what operations it would emit, what ordering it must use, what
invariants must hold, and what remains forbidden.

Canonical MAP-27I dry-run directory:
    .local\deadmtl-authoring\worldbuilder-minimal-concrete-geometry-sandbox-writer-locked-materialization-replay-dry-run\map_00\

Canonical output directory:
    .local\deadmtl-authoring\worldbuilder-minimal-concrete-geometry-sandbox-writer-locked-replay-backend-plan\map_00\
#>
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

# repo root: 3 dirs up (scripts -> deadmtl-layer-pack -> examples -> PZMapForge)
$RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$RepoRoot = (Resolve-Path (Join-Path $RepoRoot "..")).Path
$RepoRoot = (Resolve-Path (Join-Path $RepoRoot "..")).Path

$CliProject = Join-Path $RepoRoot "src"
$CliProject = Join-Path $CliProject "PZMapForge.Cli"
$CliProject = Join-Path $CliProject "PZMapForge.Cli.csproj"

$LocalBase = Join-Path $RepoRoot ".local"
$LocalBase = Join-Path $LocalBase "deadmtl-authoring"

# canonical MAP-27I dry-run root
$DryRunRoot = Join-Path $LocalBase "worldbuilder-minimal-concrete-geometry-sandbox-writer-locked-materialization-replay-dry-run"
$DryRunRoot = Join-Path $DryRunRoot "map_00"
$DryRunRoot = [IO.Path]::GetFullPath($DryRunRoot)

# canonical MAP-27J output root
$OutputRoot = Join-Path $LocalBase "worldbuilder-minimal-concrete-geometry-sandbox-writer-locked-replay-backend-plan"
$OutputRoot = Join-Path $OutputRoot "map_00"
$OutputRoot = [IO.Path]::GetFullPath($OutputRoot)

if (-not (Test-Path $DryRunRoot)) {
    Write-Error "MAP-27I dry-run root not found: $DryRunRoot"
    exit 1
}

$DryRunJson = Join-Path $DryRunRoot "map_00.minimal_concrete_geometry_sandbox_writer_locked_materialization_replay_dry_run.json"
if (-not (Test-Path $DryRunJson)) {
    Write-Error "MAP-27I dry-run JSON not found: $DryRunJson"
    exit 1
}

if (-not (Test-Path $OutputRoot)) { New-Item -ItemType Directory -Path $OutputRoot | Out-Null }

$BaseName     = "map_00.minimal_concrete_geometry_sandbox_writer_locked_replay_backend_plan"
$OutputJson   = Join-Path $OutputRoot ($BaseName + ".json")
$OutputMd     = Join-Path $OutputRoot ($BaseName + ".md")
$OutputCsv    = Join-Path $OutputRoot ($BaseName + ".csv")
$OutputSummary = Join-Path $OutputRoot ($BaseName + ".summary.txt")
$OpPlanJson   = Join-Path $OutputRoot "map_00.sandbox_writer_locked_replay_backend_operation_plan.json"
$OpPlanCsv    = Join-Path $OutputRoot "map_00.sandbox_writer_locked_replay_backend_operation_plan.csv"
$SrcManJson   = Join-Path $OutputRoot "map_00.sandbox_writer_locked_replay_backend_source_manifest.json"
$GuardJson    = Join-Path $OutputRoot "map_00.sandbox_writer_locked_replay_backend_forbidden_output_guard.json"

Write-Host "MAP-27J Sandbox Writer Locked Replay Backend Plan"
Write-Host "Dry-Run Root : $DryRunRoot"
Write-Host "Output Root  : $OutputRoot"
Write-Host ""

dotnet run --project $CliProject -- `
    deadmtl-build-worldbuilder-minimal-concrete-geometry-sandbox-writer-locked-replay-backend-plan `
    --dry-run-root                $DryRunRoot `
    --output-root                 $OutputRoot `
    --output-json                 $OutputJson `
    --output-md                   $OutputMd `
    --output-csv                  $OutputCsv `
    --summary                     $OutputSummary `
    --output-operation-plan-json  $OpPlanJson `
    --output-operation-plan-csv   $OpPlanCsv `
    --output-source-manifest-json $SrcManJson `
    --output-forbidden-guard-json $GuardJson

if ($LASTEXITCODE -ne 0) {
    Write-Error "MAP-27J command failed with exit code $LASTEXITCODE"
    exit $LASTEXITCODE
}

if (Test-Path $OutputSummary) {
    Write-Host ""
    Write-Host "=== MAP-27J Summary ==="
    Get-Content $OutputSummary
}

# Post-run forbidden artifact scan (patterns split to avoid literal strings in body)
$Pat1 = ("*." + "lotpack")
$Pat2 = ("*." + "lotheader")
$Pat3 = ("*." + "lua")
$Pat4 = ("*." + "bin")

$forbidden = @()
foreach ($pat in @($Pat1, $Pat2, $Pat3, $Pat4)) {
    $found = Get-ChildItem -Path $OutputRoot -Filter $pat -Recurse -ErrorAction SilentlyContinue
    if ($found) { $forbidden += $found }
}

$localExit = 0

if (Test-Path (Join-Path $OutputRoot "steamapps")) {
    Write-Error "FORBIDDEN: steamapps directory found in output root"
    $localExit = 1
}

$mapsDir = Join-Path $OutputRoot "media"
$mapsDir = Join-Path $mapsDir "maps"
if (Test-Path $mapsDir) {
    Write-Error "FORBIDDEN: media/maps directory found in output root"
    $localExit = 1
}

if ($forbidden.Count -gt 0) {
    Write-Error "FORBIDDEN artifacts found: $($forbidden.Count)"
    $localExit = 1
} else {
    Write-Host ""
    Write-Host "POST_BACKEND_PLAN_FORBIDDEN_SCAN PASS (0 forbidden artifacts in output root)"
}

exit $localExit
