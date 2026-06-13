# run-spawn-centered-proof-build.ps1
# End-to-end spawn-centered proof build:
#   1. Generate spawn-centered proof pack
#   2. Run deadmtl-authoring-build
#   3. Print Lua preview and expected runtime markers
#   4. Optionally install Lua to game folder (-Install)
#
# IMPORTANT: By default this script does NOT write to any Project Zomboid game folders.
# Pass -Install to perform the optional install step.
#
# Spawn reference: world tile 10650, 8250
# Road crossing:   world x=10618..10682, y=8248..8252 (visible at spawn)
#
# Usage (no install):
#   powershell -ExecutionPolicy Bypass -File scripts\run-spawn-centered-proof-build.ps1
#
# Usage (with install — installs Lua, clears save folder):
#   powershell -ExecutionPolicy Bypass -File scripts\run-spawn-centered-proof-build.ps1 -Install

param(
    [switch]$Install
)

$ErrorActionPreference = "Stop"

$repoRoot  = $PSScriptRoot | Split-Path -Parent | Split-Path -Parent | Split-Path -Parent
$packDir   = Join-Path $repoRoot ".local\deadmtl-authoring\spawn-centered-proof-pack"
$outputDir = Join-Path $repoRoot ".local\deadmtl-authoring\spawn-centered-proof"
$luaPath   = Join-Path $outputDir "WorldGenOverride.lua"

$MAP_ID = "deadmtl_spawn_centered_proof_v1"

# ---------------------------------------------------------------------------
# Step 1: Generate pack
# ---------------------------------------------------------------------------
Write-Host "=== Step 1: Generate spawn-centered proof pack ==="
Write-Host ""

$generateScript = Join-Path $PSScriptRoot "generate-spawn-centered-proof-pack.ps1"
& powershell -ExecutionPolicy Bypass -File $generateScript -OutputDir $packDir
if ($LASTEXITCODE -ne 0) {
    Write-Error "generate-spawn-centered-proof-pack.ps1 failed (exit $LASTEXITCODE)"
    exit 1
}

# ---------------------------------------------------------------------------
# Step 2: Run deadmtl-authoring-build
# ---------------------------------------------------------------------------
Write-Host ""
Write-Host "=== Step 2: Run deadmtl-authoring-build ==="
Write-Host ""

& dotnet run `
    --project (Join-Path $repoRoot "src\PZMapForge.Cli") `
    --configuration Release `
    --no-build `
    -- `
    deadmtl-authoring-build `
    --input $packDir `
    --output $outputDir

if ($LASTEXITCODE -ne 0) {
    Write-Error "deadmtl-authoring-build failed (exit $LASTEXITCODE)"
    exit 1
}

# ---------------------------------------------------------------------------
# Step 3: Lua preview
# ---------------------------------------------------------------------------
Write-Host ""
Write-Host "=== Step 3: Lua preview (first 30 lines) ==="
Write-Host ""

if (Test-Path $luaPath) {
    Get-Content $luaPath -TotalCount 30 | ForEach-Object { Write-Host "  $_" }
} else {
    Write-Error "Lua file not found at: $luaPath"
    exit 1
}

# ---------------------------------------------------------------------------
# Step 4: Expected runtime markers
# ---------------------------------------------------------------------------
Write-Host ""
Write-Host "=== Step 4: Expected runtime markers ==="
Write-Host ""

$luaContent = Get-Content $luaPath -Raw

$mapIdMarker    = "PZMAPFORGE_WORLDGENOVERRIDE_MAP_ID=$MAP_ID"
$loadedMarker   = "PZMAPFORGE_WORLDGENOVERRIDE_LOADED"

# Count static_modules entries for module count marker
$moduleCount = ([regex]::Matches($luaContent, "position\s*=\s*\{")).Count
$countMarker = "PZMAPFORGE_WORLDGENOVERRIDE_MODULE_COUNT=$moduleCount"

Write-Host "  $mapIdMarker"
Write-Host "  $loadedMarker"
Write-Host "  $countMarker"
Write-Host ""
Write-Host "Spawn reference:  world tile 10650, 8250"
Write-Host "Road crossing:    world y=8248..8252 (should cross immediately under player)"
Write-Host "Shore patch:      world x=10632..10668, y=8238..8265"
Write-Host "Forest patch:     world x=10608..10630, y=8238..8272"
Write-Host "Water base:       entire canvas (visible at x=10670..10690, y=8238..8265)"

# ---------------------------------------------------------------------------
# Step 5: Install (only if -Install passed)
# ---------------------------------------------------------------------------
if (-not $Install) {
    Write-Host ""
    Write-Host "=== Install step: SKIPPED (pass -Install to install) ==="
    Write-Host ""
    Write-Host "To install manually, copy:"
    Write-Host "  Source: $luaPath"
    Write-Host "  Dest:   D:\Program Files (x86)\Steam\steamapps\common\ProjectZomboid\media\maps\pzmapforge_build42_candidate_v4_001\WorldGenOverride.lua"
    Write-Host ""
    Write-Host "Then clear save folder:"
    Write-Host "  C:\Users\Palmacede\Zomboid\Saves\Multiplayer\PZMF_B42_MAP9F_NO_MULDRAUGH_001"
    exit 0
}

# ---------------------------------------------------------------------------
# Install block — only runs when -Install is explicitly passed
# ---------------------------------------------------------------------------
Write-Host ""
Write-Host "=== Step 5: Install (-Install mode) ==="
Write-Host ""

$gameMapDir = "D:\Program Files (x86)\Steam\steamapps\common\ProjectZomboid\media\maps\pzmapforge_build42_candidate_v4_001"
$destLua    = Join-Path $gameMapDir "WorldGenOverride.lua"
$saveFolder = "C:\Users\Palmacede\Zomboid\Saves\Multiplayer\PZMF_B42_MAP9F_NO_MULDRAUGH_001"

# Verify game map dir exists
if (-not (Test-Path $gameMapDir)) {
    Write-Error "Game map dir not found: $gameMapDir"
    exit 1
}

# Backup existing Lua if present
if (Test-Path $destLua) {
    $timestamp = (Get-Date -Format "yyyyMMdd-HHmmss")
    $backupPath = "$destLua.$timestamp.bak"
    Copy-Item -Path $destLua -Destination $backupPath
    Write-Host "  Backed up: $backupPath"
}

# Install new Lua
Copy-Item -Path $luaPath -Destination $destLua -Force
Write-Host "  Installed: $destLua"

# Byte-check installed file
$installedBytes = [System.IO.File]::ReadAllBytes($destLua)
$hasBom      = ($installedBytes.Length -ge 3) -and ($installedBytes[0] -eq 0xEF) -and ($installedBytes[1] -eq 0xBB) -and ($installedBytes[2] -eq 0xBF)
$hasNonAscii = ($installedBytes | Where-Object { $_ -gt 127 }).Count -gt 0

if ($hasBom) {
    Write-Error "VERDICT: FAIL — installed Lua has BOM"
    exit 1
}
if ($hasNonAscii) {
    Write-Error "VERDICT: FAIL — installed Lua has non-ASCII bytes"
    exit 1
}

Write-Host "  Byte check: BOM=false, Non-ASCII=false"

# Clear save folder
if (Test-Path $saveFolder) {
    Remove-Item -Recurse -Force $saveFolder
    Write-Host "  Cleared save folder: $saveFolder"
} else {
    Write-Host "  Save folder not found (already clear): $saveFolder"
}

Write-Host ""
Write-Host "VERDICT: PASS"
Write-Host "  Map ID:       $MAP_ID"
Write-Host "  Module count: $moduleCount"
Write-Host "  BOM:          false"
Write-Host "  Non-ASCII:    false"
Write-Host ""
Write-Host "Launch PZ, host PZMF_B42_MAP9F_NO_MULDRAUGH_001, spawn at 10650,8250."
Write-Host "Confirm road crossing visible at world y=8248..8252."
