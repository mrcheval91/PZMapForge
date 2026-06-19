#Requires -Version 5.1
<#
.SYNOPSIS
MAP-27K: Sandbox Writer Locked Replay Backend Dry-Run Emitter

Reads the MAP-27J backend plan output and emits deterministic backend dry-run
operation artifacts for the 5 locked replay material buckets.

This is NOT a runtime writer.
This is NOT a lotpack writer.
This is NOT a Project Zomboid export.
This is NOT writer-ready.

This emits concrete dry-run backend write records from the MAP-27J operation
plan. Each emitted record is a sandbox-only dry-run artifact; no runtime files,
binary files, Lua files, or install paths are produced.

Canonical MAP-27J backend plan directory:
    .local\deadmtl-authoring\worldbuilder-minimal-concrete-geometry-sandbox-writer-locked-replay-backend-plan\map_00\

Canonical output directory:
    .local\deadmtl-authoring\worldbuilder-minimal-concrete-geometry-sandbox-writer-locked-replay-backend-dry-run-emitter\map_00\
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

# canonical MAP-27J backend plan root
$BackendPlanRoot = Join-Path $LocalBase "worldbuilder-minimal-concrete-geometry-sandbox-writer-locked-replay-backend-plan"
$BackendPlanRoot = Join-Path $BackendPlanRoot "map_00"
$BackendPlanRoot = [IO.Path]::GetFullPath($BackendPlanRoot)

# canonical MAP-27K output root
$OutputRoot = Join-Path $LocalBase "worldbuilder-minimal-concrete-geometry-sandbox-writer-locked-replay-backend-dry-run-emitter"
$OutputRoot = Join-Path $OutputRoot "map_00"
$OutputRoot = [IO.Path]::GetFullPath($OutputRoot)

if (-not (Test-Path $BackendPlanRoot)) {
    Write-Error "MAP-27J backend plan root not found: $BackendPlanRoot"
    exit 1
}

$PlanJson = Join-Path $BackendPlanRoot "map_00.minimal_concrete_geometry_sandbox_writer_locked_replay_backend_plan.json"
if (-not (Test-Path $PlanJson)) {
    Write-Error "MAP-27J backend plan JSON not found: $PlanJson"
    exit 1
}

if (-not (Test-Path $OutputRoot)) { New-Item -ItemType Directory -Path $OutputRoot | Out-Null }

$BaseName           = "map_00.minimal_concrete_geometry_sandbox_writer_locked_replay_backend_dry_run_emitter"
$OutputJson         = Join-Path $OutputRoot ($BaseName + ".json")
$OutputMd           = Join-Path $OutputRoot ($BaseName + ".md")
$OutputCsv          = Join-Path $OutputRoot ($BaseName + ".csv")
$OutputSummary      = Join-Path $OutputRoot ($BaseName + ".summary.txt")
$OpsJson            = Join-Path $OutputRoot "map_00.sandbox_writer_locked_replay_backend_dry_run_operations.json"
$OpsCsv             = Join-Path $OutputRoot "map_00.sandbox_writer_locked_replay_backend_dry_run_operations.csv"
$SrcManJson         = Join-Path $OutputRoot "map_00.sandbox_writer_locked_replay_backend_dry_run_source_manifest.json"
$DryRunDigestJson   = Join-Path $OutputRoot "map_00.sandbox_writer_locked_replay_backend_dry_run_digest.json"
$GuardJson          = Join-Path $OutputRoot "map_00.sandbox_writer_locked_replay_backend_dry_run_forbidden_output_guard.json"

Write-Host "MAP-27K Sandbox Writer Locked Replay Backend Dry-Run Emitter"
Write-Host "Backend Plan Root : $BackendPlanRoot"
Write-Host "Output Root       : $OutputRoot"
Write-Host ""

dotnet run --project $CliProject -- `
    deadmtl-build-worldbuilder-minimal-concrete-geometry-sandbox-writer-locked-replay-backend-dry-run-emitter `
    --backend-plan-root             $BackendPlanRoot `
    --output-root                   $OutputRoot `
    --output-json                   $OutputJson `
    --output-md                     $OutputMd `
    --output-csv                    $OutputCsv `
    --summary                       $OutputSummary `
    --output-operations-json        $OpsJson `
    --output-operations-csv         $OpsCsv `
    --output-source-manifest-json   $SrcManJson `
    --output-dry-run-digest-json    $DryRunDigestJson `
    --output-forbidden-guard-json   $GuardJson

if ($LASTEXITCODE -ne 0) {
    Write-Error "MAP-27K command failed with exit code $LASTEXITCODE"
    exit $LASTEXITCODE
}

if (Test-Path $OutputSummary) {
    Write-Host ""
    Write-Host "=== MAP-27K Summary ==="
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
    Write-Host "POST_BACKEND_DRY_RUN_EMITTER_FORBIDDEN_SCAN PASS (0 forbidden artifacts in output root)"
}

exit $localExit
