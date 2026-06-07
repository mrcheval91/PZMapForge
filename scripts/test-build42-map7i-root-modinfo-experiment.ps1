#Requires -Version 5.1
<#
.SYNOPSIS
    Tests for MAP-7I: prepare-build42-map7i-root-modinfo-experiment-packet.ps1
    and the updated inspect-build42-map-discovery-path.ps1.

    Uses synthetic/local .local fixtures.
    Does NOT depend on user-local logs.
    Expected assertion count: 12
#>

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$scriptDir      = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot       = Split-Path -Parent $scriptDir
$packetScript   = Join-Path $repoRoot 'scripts\prepare-build42-map7i-root-modinfo-experiment-packet.ps1'
$inspectorScript = Join-Path $repoRoot 'scripts\inspect-build42-map-discovery-path.ps1'
$tempRoot       = [System.IO.Path]::GetTempPath()
$mapId          = 'pzmapforge_build42_candidate_v4_001'

$pass = 0
$fail = 0

function Assert-True {
    param([bool]$Condition, [string]$Label)
    if ($Condition) { Write-Output "  PASS  $Label"; $script:pass++ }
    else            { Write-Output "  FAIL  $Label"; $script:fail++ }
}

Write-Output 'test-build42-map7i-root-modinfo-experiment.ps1'
Write-Output ''

# ---------------------------------------------------------------------------
# Setup
# ---------------------------------------------------------------------------

$testBase = Join-Path $tempRoot ('pzmf-t7i-' + [System.IO.Path]::GetRandomFileName())
$badPath  = Join-Path $tempRoot 'pzmf-t7i-bad-no-local'
New-Item -ItemType Directory -Force -Path $badPath | Out-Null

function Invoke-Packet {
    param([string]$OutDir)
    & powershell -ExecutionPolicy Bypass -File $packetScript -Output $OutDir | Out-Null
    return [int]$LASTEXITCODE
}

# ===========================================================================
# Test 1: Packet refuses output outside .local
# ===========================================================================

Write-Output '--- Test 1: Packet refuses output outside .local ---'
$t1exit = Invoke-Packet -OutDir $badPath
Assert-True ($t1exit -ne 0) 'Test1: packet output outside .local exits nonzero'

# ===========================================================================
# Tests 2-12: Run packet to .local output and verify
# ===========================================================================

Write-Output ''
Write-Output '--- Running packet (Tests 2-12) ---'
$packetOut = Join-Path $testBase '.local\packet'
$t2exit    = Invoke-Packet -OutDir $packetOut

# Test 2: Packet exits 0
Assert-True ($t2exit -eq 0) 'Test2: packet exits 0'

# ---------------------------------------------------------------------------
# Experiment-E candidate structure checks
# ---------------------------------------------------------------------------

$expEBase     = Join-Path $packetOut "experiment-e-candidate\$mapId"
$expE42Maps   = Join-Path $expEBase "42\media\maps\$mapId"
$expERootMaps = Join-Path $expEBase "media\maps\$mapId"

# Test 3: Root mod.info exists
Write-Output ''
Write-Output '--- Test 3: Root mod.info exists ---'
$rootModInfo = Join-Path $expEBase 'mod.info'
Assert-True (Test-Path -LiteralPath $rootModInfo) `
    'Test3: experiment-E candidate has root mod.info'

# Test 4: Root media/maps/<MapId>/map.info exists
Write-Output ''
Write-Output '--- Test 4: Root media/maps/map.info exists ---'
$rootMapInfo = Join-Path $expERootMaps 'map.info'
Assert-True (Test-Path -LiteralPath $rootMapInfo) `
    'Test4: experiment-E candidate has root media/maps/map.info'

# Test 5: 42/mod.info preserved
Write-Output ''
Write-Output '--- Test 5: 42/mod.info preserved ---'
$v42ModInfo = Join-Path $expEBase '42\mod.info'
Assert-True (Test-Path -LiteralPath $v42ModInfo) `
    'Test5: experiment-E candidate preserves 42/mod.info'

# Test 6: 42/media/maps/<MapId>/map.info preserved
Write-Output ''
Write-Output '--- Test 6: 42/media/maps/map.info preserved ---'
$v42MapInfo = Join-Path $expE42Maps 'map.info'
Assert-True (Test-Path -LiteralPath $v42MapInfo) `
    'Test6: experiment-E candidate preserves 42/media/maps/map.info'

# ---------------------------------------------------------------------------
# No-BOM checks (via inspector JSON output)
# ---------------------------------------------------------------------------

$discoveryJson = Join-Path $packetOut 'map-discovery-path-report.json'
$disc = if (Test-Path $discoveryJson) {
    Get-Content $discoveryJson -Raw | ConvertFrom-Json
} else { $null }

# Test 7: Root mod.info no-BOM
Write-Output ''
Write-Output '--- Test 7: Root mod.info no-BOM ---'
$rootModNoBom = if ($null -ne $disc) { [bool]$disc.root_files.mod_info.no_bom } else { $false }
Assert-True ($rootModNoBom -eq $true) `
    "Test7: inspector root_files.mod_info.no_bom == true (got $rootModNoBom)"

# Test 8: Root map.info no-BOM
Write-Output ''
Write-Output '--- Test 8: Root map.info no-BOM ---'
$rootMapNoBom = if ($null -ne $disc) { [bool]$disc.root_files.map_info.no_bom } else { $false }
Assert-True ($rootMapNoBom -eq $true) `
    "Test8: inspector root_files.map_info.no_bom == true (got $rootMapNoBom)"

# ---------------------------------------------------------------------------
# Preflight JSON checks
# ---------------------------------------------------------------------------

$preflightJson = Join-Path $packetOut 'map7i-root-modinfo-preflight.json'
$pfl = if (Test-Path $preflightJson) {
    Get-Content $preflightJson -Raw | ConvertFrom-Json
} else { $null }

# Test 9: Preflight records variant_d_result
Write-Output ''
Write-Output '--- Test 9: Preflight variant_d_result ---'
$vdResult = if ($null -ne $pfl) { [string]$pfl.variant_d_result } else { '' }
Assert-True ($vdResult -eq 'MAP7F_VARIANT_D_MAP_FOLDER_SCAN_EMPTY') `
    "Test9: preflight variant_d_result == MAP7F_VARIANT_D_MAP_FOLDER_SCAN_EMPTY (got '$vdResult')"

# Test 10: Preflight records experiment_e_root_mod_info=true
Write-Output ''
Write-Output '--- Test 10: Preflight experiment_e_root_mod_info ---'
$expEFlag = if ($null -ne $pfl) { [bool]$pfl.experiment_e_root_mod_info } else { $false }
Assert-True ($expEFlag -eq $true) `
    'Test10: preflight experiment_e_root_mod_info == true'

# Test 11: Packet contains HUMAN-ONLY language
Write-Output ''
Write-Output '--- Test 11: Packet contains HUMAN-ONLY language ---'
$installContent = if (Test-Path (Join-Path $packetOut 'MAP_7I_EXPERIMENT_E_MANUAL_INSTALL_COMMANDS.md')) {
    Get-Content (Join-Path $packetOut 'MAP_7I_EXPERIMENT_E_MANUAL_INSTALL_COMMANDS.md') -Raw
} else { '' }
Assert-True ($installContent -match 'HUMAN-ONLY') `
    'Test11: install commands contain HUMAN-ONLY marker'

# Test 12: Packet contains public_playable_claim_allowed=false
Write-Output ''
Write-Output '--- Test 12: Packet public_playable_claim_allowed=false ---'
Assert-True ($null -ne $pfl -and [bool]$pfl.public_playable_claim_allowed -eq $false) `
    'Test12: preflight public_playable_claim_allowed == false'

# ---------------------------------------------------------------------------
# Summary
# ---------------------------------------------------------------------------

Write-Output ''
Write-Output '----------------------------------------'
Write-Output "Results: $pass passed, $fail failed"
Write-Output '----------------------------------------'

if ($fail -gt 0) { exit 1 }
exit 0
