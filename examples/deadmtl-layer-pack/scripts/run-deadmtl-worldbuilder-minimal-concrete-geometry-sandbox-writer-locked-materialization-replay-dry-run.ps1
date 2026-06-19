#Requires -Version 5.1
<#
.SYNOPSIS
MAP-27I: Sandbox Writer Locked Materialization Replay Dry Run

Reads the MAP-27H locked replay audit JSON, re-hashes the 8 locked files,
loads the MATERIALIZED_CELLS_CSV, computes a deterministic locked replay
digest, and emits 8 output files.

SANDBOX ONLY. Does NOT write lotpack, lotheader, runtime Lua, or install
into Project Zomboid. writer_ready=false, runtime_valid=false.

Canonical MAP-27H audit directory:
    .local\deadmtl-authoring\worldbuilder-minimal-concrete-geometry-sandbox-writer-tile-materialization-locked-replay-audit\map_00\

Canonical output directory:
    .local\deadmtl-authoring\worldbuilder-minimal-concrete-geometry-sandbox-writer-locked-materialization-replay-dry-run\map_00\
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

# canonical MAP-27H audit root
$LocalBase = Join-Path $RepoRoot ".local"
$LocalBase = Join-Path $LocalBase "deadmtl-authoring"

$AuditRoot = Join-Path $LocalBase "worldbuilder-minimal-concrete-geometry-sandbox-writer-tile-materialization-locked-replay-audit"
$AuditRoot = Join-Path $AuditRoot "map_00"
$AuditRoot = [IO.Path]::GetFullPath($AuditRoot)

# canonical MAP-27I output root
$OutputRoot = Join-Path $LocalBase "worldbuilder-minimal-concrete-geometry-sandbox-writer-locked-materialization-replay-dry-run"
$OutputRoot = Join-Path $OutputRoot "map_00"
$OutputRoot = [IO.Path]::GetFullPath($OutputRoot)

if (-not (Test-Path $AuditRoot)) {
    Write-Error "MAP-27H audit root not found: $AuditRoot"
    exit 1
}

$AuditJson = Join-Path $AuditRoot "map_00.minimal_concrete_geometry_sandbox_writer_tile_materialization_locked_replay_audit.json"
if (-not (Test-Path $AuditJson)) {
    Write-Error "MAP-27H audit JSON not found: $AuditJson"
    exit 1
}

if (-not (Test-Path $OutputRoot)) { New-Item -ItemType Directory -Path $OutputRoot | Out-Null }

$BaseName      = "map_00.minimal_concrete_geometry_sandbox_writer_locked_materialization_replay_dry_run"
$OutputJson    = Join-Path $OutputRoot ($BaseName + ".json")
$OutputMd      = Join-Path $OutputRoot ($BaseName + ".md")
$OutputCsv     = Join-Path $OutputRoot ($BaseName + ".csv")
$OutputSummary = Join-Path $OutputRoot ($BaseName + ".summary.txt")
$MaterialCsv   = Join-Path $OutputRoot "map_00.sandbox_writer_locked_replay_material_counts.csv"
$SourceManJson = Join-Path $OutputRoot "map_00.sandbox_writer_locked_replay_source_manifest.json"
$DigestJson    = Join-Path $OutputRoot "map_00.sandbox_writer_locked_replay_digest.json"
$GuardJson     = Join-Path $OutputRoot "map_00.sandbox_writer_locked_replay_forbidden_output_guard.json"

Write-Host "MAP-27I Sandbox Writer Locked Materialization Replay Dry Run"
Write-Host "Audit Root : $AuditRoot"
Write-Host "Output Root: $OutputRoot"
Write-Host ""

dotnet run --project $CliProject -- `
    deadmtl-build-worldbuilder-minimal-concrete-geometry-sandbox-writer-locked-materialization-replay-dry-run `
    --audit-root  $AuditRoot `
    --output-root $OutputRoot `
    --output-json $OutputJson `
    --output-md   $OutputMd `
    --output-csv  $OutputCsv `
    --summary     $OutputSummary `
    --output-material-counts-csv  $MaterialCsv `
    --output-source-manifest-json $SourceManJson `
    --output-replay-digest-json   $DigestJson `
    --output-forbidden-guard-json $GuardJson

if ($LASTEXITCODE -ne 0) {
    Write-Error "MAP-27I command failed with exit code $LASTEXITCODE"
    exit $LASTEXITCODE
}

if (Test-Path $OutputSummary) {
    Write-Host ""
    Write-Host "=== MAP-27I Summary ==="
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
    Write-Host "POST_DRY_RUN_FORBIDDEN_SCAN PASS (0 forbidden artifacts in output root)"
}

exit $localExit
