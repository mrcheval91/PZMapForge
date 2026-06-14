# run-worldgen-prefab-dump.ps1
# Run/install helper for the MAP-22B WorldGen prefab discovery probe.
#
#   1. Generate the prefab dump Lua probe
#   2. Preview and byte-check
#   3. Optionally install to game folder (-Install)
#
# IMPORTANT: By default this script does NOT write to any PZ game folders.
# Pass -Install to perform the optional install step.
#
# After install, launch PZ and host PZMF_B42_MAP9F_NO_MULDRAUGH_001.
# Then run harvest-worldgen-prefab-dump-logs.ps1 to capture results.
#
# Usage (no install):
#   powershell -ExecutionPolicy Bypass -File scripts\run-worldgen-prefab-dump.ps1
#
# Usage (with install):
#   powershell -ExecutionPolicy Bypass -File scripts\run-worldgen-prefab-dump.ps1 -Install

param(
    [switch]$Install
)

$ErrorActionPreference = "Stop"

$repoRoot  = $PSScriptRoot | Split-Path -Parent | Split-Path -Parent | Split-Path -Parent
$outputDir = Join-Path $repoRoot ".local\deadmtl-authoring\worldgen-prefab-dump"
$luaPath   = Join-Path $outputDir "WorldGenOverride.lua"

# ---------------------------------------------------------------------------
# Step 1: Generate probe Lua
# ---------------------------------------------------------------------------
Write-Host "=== Step 1: Generate WorldGen prefab dump probe ==="
Write-Host ""

$generateScript = Join-Path $PSScriptRoot "generate-worldgen-prefab-dump-lua.ps1"
& powershell -ExecutionPolicy Bypass -File $generateScript -OutputDir $outputDir
if ($LASTEXITCODE -ne 0) {
    Write-Error "generate-worldgen-prefab-dump-lua.ps1 failed (exit $LASTEXITCODE)"
    exit 1
}

# ---------------------------------------------------------------------------
# Step 2: Byte-check confirmation
# ---------------------------------------------------------------------------
Write-Host ""
Write-Host "=== Step 2: Byte check ==="
Write-Host ""

$bytes       = [System.IO.File]::ReadAllBytes($luaPath)
$hasBom      = ($bytes.Length -ge 3) -and ($bytes[0] -eq 0xEF) -and ($bytes[1] -eq 0xBB) -and ($bytes[2] -eq 0xBF)
$hasNonAscii = ($bytes | Where-Object { $_ -gt 127 }).Count -gt 0

Write-Host "  Lua path:   $luaPath"
Write-Host "  Size:       $($bytes.Length) bytes"
Write-Host "  BOM:        $hasBom"
Write-Host "  Non-ASCII:  $hasNonAscii"

if ($hasBom -or $hasNonAscii) {
    Write-Error "Byte check FAILED"
    exit 1
}
Write-Host "  PASS: ASCII, no BOM"

# ---------------------------------------------------------------------------
# No-install exit
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
    Write-Host ""
    Write-Host "After PZ run, harvest results with:"
    Write-Host "  powershell -ExecutionPolicy Bypass -File scripts\harvest-worldgen-prefab-dump-logs.ps1"
    exit 0
}

# ---------------------------------------------------------------------------
# Install block -- only runs when -Install is explicitly passed
# ---------------------------------------------------------------------------
Write-Host ""
Write-Host "=== Install (-Install mode) ==="
Write-Host ""

$gameMapDir = "D:\Program Files (x86)\Steam\steamapps\common\ProjectZomboid\media\maps\pzmapforge_build42_candidate_v4_001"
$destLua    = Join-Path $gameMapDir "WorldGenOverride.lua"
$saveFolder = "C:\Users\Palmacede\Zomboid\Saves\Multiplayer\PZMF_B42_MAP9F_NO_MULDRAUGH_001"

if (-not (Test-Path $gameMapDir)) {
    Write-Error "Game map dir not found: $gameMapDir"
    exit 1
}

# Backup existing Lua
if (Test-Path $destLua) {
    $ts         = (Get-Date -Format "yyyyMMdd-HHmmss")
    $backupPath = "$destLua.$ts.bak"
    Copy-Item -Path $destLua -Destination $backupPath
    Write-Host "  Backed up: $backupPath"
}

# Install probe Lua
Copy-Item -Path $luaPath -Destination $destLua -Force
Write-Host "  Installed: $destLua"

# Byte-check installed file
$installedBytes = [System.IO.File]::ReadAllBytes($destLua)
$instBom        = ($installedBytes.Length -ge 3) -and ($installedBytes[0] -eq 0xEF) -and ($installedBytes[1] -eq 0xBB) -and ($installedBytes[2] -eq 0xBF)
$instNonAscii   = ($installedBytes | Where-Object { $_ -gt 127 }).Count -gt 0

if ($instBom)      { Write-Error "VERDICT: FAIL -- installed Lua has BOM"; exit 1 }
if ($instNonAscii) { Write-Error "VERDICT: FAIL -- installed Lua has non-ASCII bytes"; exit 1 }
Write-Host "  Byte check: BOM=false, Non-ASCII=false"

# Clear save folder
if (Test-Path $saveFolder) {
    Remove-Item -Recurse -Force $saveFolder
    Write-Host "  Cleared save folder: $saveFolder"
} else {
    Write-Host "  Save folder not found (already clear): $saveFolder"
}

Write-Host ""
Write-Host "VERDICT: MAP22B_WORLDGEN_PREFAB_DUMP_INSTALLED_RESTART_REQUIRED"
Write-Host ""
Write-Host "Next steps:"
Write-Host "  1. Launch PZ, host PZMF_B42_MAP9F_NO_MULDRAUGH_001, spawn at 10650,8250."
Write-Host "  2. Wait a few seconds for WorldGen Lua to execute."
Write-Host "  3. Exit PZ."
Write-Host "  4. Run: powershell -ExecutionPolicy Bypass -File scripts\harvest-worldgen-prefab-dump-logs.ps1"
