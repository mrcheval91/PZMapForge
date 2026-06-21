# MAP-36F EXP_004: Bucket-to-tilesheet cross-reference read-only probe
# Read-only. Does not write any game files or binary cells.

$RepoRoot = Resolve-Path "$PSScriptRoot\..\..\.."
$CliProject = "$RepoRoot\src\PZMapForge.Cli\PZMapForge.Cli.csproj"

$EmitterJson = "$RepoRoot\.local\deadmtl-authoring\worldbuilder-minimal-concrete-geometry-sandbox-writer-locked-replay-backend-dry-run-emitter\map_00\map_00.sandbox_writer_locked_replay_backend_dry_run_operations.json"
$PaletteGuide = "$RepoRoot\examples\deadmtl-layer-pack\palettes\worldgen-png-palette.layer-guide.txt"
$PaletteSwatches = "$RepoRoot\examples\deadmtl-layer-pack\palettes\worldgen-png-palette.swatches.txt"
$OutputRoot = "$RepoRoot\.local\deadmtl-authoring\map36f-bucket-to-tilesheet-cross-reference"

# Auto-detect PZ install from common Steam paths
$PzInstallRoot = $null
$PzCandidates = @(
    "C:\Program Files (x86)\Steam\steamapps\common\ProjectZomboid",
    "C:\Program Files\Steam\steamapps\common\ProjectZomboid",
    "D:\Steam\steamapps\common\ProjectZomboid",
    "E:\Steam\steamapps\common\ProjectZomboid",
    "C:\Games\Steam\steamapps\common\ProjectZomboid",
    "D:\Games\Steam\steamapps\common\ProjectZomboid"
)
foreach ($p in $PzCandidates) {
    if (Test-Path $p) { $PzInstallRoot = $p; break }
}

$args_list = @(
    "deadmtl-build-worldbuilder-bucket-to-tilesheet-cross-reference"
    "--output-root", $OutputRoot
)

if (Test-Path $EmitterJson) {
    $args_list += "--emitter-json", $EmitterJson
} else {
    Write-Warning "Emitter JSON not found (optional): $EmitterJson"
}

if (Test-Path $PaletteGuide) {
    $args_list += "--palette-guide", $PaletteGuide
} else {
    Write-Warning "Palette guide not found (optional): $PaletteGuide"
}

if (Test-Path $PaletteSwatches) {
    $args_list += "--palette-swatches", $PaletteSwatches
} else {
    Write-Warning "Palette swatches not found (optional): $PaletteSwatches"
}

if ($PzInstallRoot) {
    $args_list += "--pz-install-root", $PzInstallRoot
    Write-Host "  pz-install: $PzInstallRoot"
} else {
    Write-Warning "PZ install root not found - running without local tile scan. Pass --pz-install-root to override."
}

Write-Host "MAP-36F: Running bucket-to-tilesheet cross-reference probe..."
Write-Host "  output: $OutputRoot"

dotnet run --project "$CliProject" -- @args_list

if ($LASTEXITCODE -ne 0) {
    Write-Error "MAP-36F bucket-to-tilesheet cross-reference probe failed (exit $LASTEXITCODE)"
    exit $LASTEXITCODE
}

Write-Host ""
Write-Host "MAP-36F complete. Artifacts in: $OutputRoot"
