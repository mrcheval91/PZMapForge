#Requires -Version 5.1
<#
.SYNOPSIS
    Tests for MAP-7L: prepare-build42-map7l-common-layout-experiment-packet.ps1.

    Expected assertion count: 15
#>

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$scriptDir    = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot     = Split-Path -Parent $scriptDir
$packetScript = Join-Path $repoRoot 'scripts\prepare-build42-map7l-common-layout-experiment-packet.ps1'
$tempRoot     = [System.IO.Path]::GetTempPath()
$mapId        = 'pzmapforge_build42_candidate_v4_001'

$pass = 0
$fail = 0

function Assert-True {
    param([bool]$Condition, [string]$Label)
    if ($Condition) { Write-Output "  PASS  $Label"; $script:pass++ }
    else            { Write-Output "  FAIL  $Label"; $script:fail++ }
}

Write-Output 'test-build42-map7l-common-layout-experiment.ps1'
Write-Output ''

$testBase = Join-Path $tempRoot ('pzmf-t7l-' + [System.IO.Path]::GetRandomFileName())
$badPath  = Join-Path $tempRoot 'pzmf-t7l-bad-no-local'
New-Item -ItemType Directory -Force -Path $badPath | Out-Null

function Invoke-Packet {
    param([string]$OutDir)
    & powershell -ExecutionPolicy Bypass -File $packetScript -Output $OutDir | Out-Null
    return [int]$LASTEXITCODE
}

# ---------------------------------------------------------------------------
# Test 1: Packet refuses output outside .local
# ---------------------------------------------------------------------------

Write-Output '--- Test 1: Packet refuses output outside .local ---'
$t1exit = Invoke-Packet -OutDir $badPath
Assert-True ($t1exit -ne 0) 'Test1: packet output outside .local exits nonzero'

# ---------------------------------------------------------------------------
# Tests 2-15: Run packet and verify output
# ---------------------------------------------------------------------------

Write-Output ''
Write-Output '--- Running packet (Tests 2-15) ---'
$packetOut = Join-Path $testBase '.local\packet'
$t2exit    = Invoke-Packet -OutDir $packetOut

# Test 2: Packet writes all required files
$reqFiles = @(
    'MAP_7L_COMMON_LAYOUT_PACKET.md',
    'MAP_7L_VARIANT_G_RESULT_SUMMARY.md',
    'MAP_7L_OPERATOR_STRUCTURE_EVIDENCE.md',
    'MAP_7L_EXPERIMENT_H_MANUAL_INSTALL_COMMANDS.md',
    'MAP_7L_EXPERIMENT_H_LOG_CAPTURE_COMMANDS.md',
    'MAP_7L_EXPERIMENT_H_MANUAL_RESULT.local-template.md',
    'map7l-common-layout-preflight.json',
    'map7l-common-layout-preflight.md',
    'map-discovery-path-report.json',
    'map-discovery-path-report.md',
    'map-metadata-contract-report.json',
    'map-metadata-contract-report.md'
)
$allPresent = $true
foreach ($f in $reqFiles) {
    if (-not (Test-Path (Join-Path $packetOut $f))) { $allPresent = $false }
}
Assert-True ($t2exit -eq 0 -and $allPresent) 'Test2: packet exits 0 and all 12 required files present'

# Experiment-H paths
$expHBase      = Join-Path $packetOut "experiment-h-candidate\$mapId"
$expHCommonDir = Join-Path $expHBase 'common'
$expHCommonMaps = Join-Path $expHCommonDir "media\maps\$mapId"

# Test 3: Experiment-H has common/mod.info
Write-Output ''
Write-Output '--- Test 3: common/mod.info exists ---'
Assert-True (Test-Path -LiteralPath (Join-Path $expHCommonDir 'mod.info')) `
    'Test3: experiment-H has common/mod.info'

# Test 4: Experiment-H has common/media/maps/<MapId>/map.info
Write-Output ''
Write-Output '--- Test 4: common/media/maps/map.info exists ---'
Assert-True (Test-Path -LiteralPath (Join-Path $expHCommonMaps 'map.info')) `
    'Test4: experiment-H has common/media/maps/map.info'

# Test 5: common/media/maps/<MapId>/spawnpoints.lua exists
Write-Output ''
Write-Output '--- Test 5: common/media/maps/spawnpoints.lua exists ---'
Assert-True (Test-Path -LiteralPath (Join-Path $expHCommonMaps 'spawnpoints.lua')) `
    'Test5: experiment-H has common/media/maps/spawnpoints.lua'

# Test 6: common/media/maps/<MapId>/objects.lua exists
Write-Output ''
Write-Output '--- Test 6: common/media/maps/objects.lua exists ---'
Assert-True (Test-Path -LiteralPath (Join-Path $expHCommonMaps 'objects.lua')) `
    'Test6: experiment-H has common/media/maps/objects.lua'

# Test 7: common/media/maps/<MapId>/0_0.lotheader exists
Write-Output ''
Write-Output '--- Test 7: common/media/maps/0_0.lotheader exists ---'
Assert-True (Test-Path -LiteralPath (Join-Path $expHCommonMaps '0_0.lotheader')) `
    'Test7: experiment-H has common/media/maps/0_0.lotheader'

# Test 8: common/media/maps/<MapId>/chunkdata_0_0.bin exists
Write-Output ''
Write-Output '--- Test 8: common/media/maps/chunkdata_0_0.bin exists ---'
Assert-True (Test-Path -LiteralPath (Join-Path $expHCommonMaps 'chunkdata_0_0.bin')) `
    'Test8: experiment-H has common/media/maps/chunkdata_0_0.bin'

# Test 9: Binary files preserve sizes (LOTH/LOTP/chunkdata unchanged)
Write-Output ''
Write-Output '--- Test 9: Binary sizes unchanged from empty_grass_v4 ---'
$lothSize  = if (Test-Path (Join-Path $expHCommonMaps '0_0.lotheader'))    { (Get-Item (Join-Path $expHCommonMaps '0_0.lotheader')).Length    } else { 0 }
$lotpSize  = if (Test-Path (Join-Path $expHCommonMaps 'world_0_0.lotpack')) { (Get-Item (Join-Path $expHCommonMaps 'world_0_0.lotpack')).Length } else { 0 }
$chunkSize = if (Test-Path (Join-Path $expHCommonMaps 'chunkdata_0_0.bin')) { (Get-Item (Join-Path $expHCommonMaps 'chunkdata_0_0.bin')).Length } else { 0 }
$sizesOk   = ($lothSize -eq 29646) -and ($lotpSize -eq 1056780) -and ($chunkSize -eq 1026)
Assert-True $sizesOk `
    "Test9: LOTH=29646($lothSize), LOTP=1056780($lotpSize), chunkdata=1026($chunkSize)"

# Test 10: Text files are no-BOM
Write-Output ''
Write-Output '--- Test 10: Text files no-BOM ---'
function Test-HasBom {
    param([string]$Path)
    if (-not (Test-Path -LiteralPath $Path)) { return $false }
    $b = [System.IO.File]::ReadAllBytes($Path)
    return ($b.Length -ge 3 -and $b[0] -eq 0xEF -and $b[1] -eq 0xBB -and $b[2] -eq 0xBF)
}
$modNoBom   = -not (Test-HasBom (Join-Path $expHCommonDir 'mod.info'))
$mapNoBom   = -not (Test-HasBom (Join-Path $expHCommonMaps 'map.info'))
$spawnNoBom = -not (Test-HasBom (Join-Path $expHCommonMaps 'spawnpoints.lua'))
$objNoBom   = -not (Test-HasBom (Join-Path $expHCommonMaps 'objects.lua'))
Assert-True ($modNoBom -and $mapNoBom -and $spawnNoBom -and $objNoBom) `
    "Test10: common text files no-BOM: mod=$modNoBom, map=$mapNoBom, spawn=$spawnNoBom, obj=$objNoBom"

# Preflight JSON checks
$pfl = if (Test-Path (Join-Path $packetOut 'map7l-common-layout-preflight.json')) {
    Get-Content (Join-Path $packetOut 'map7l-common-layout-preflight.json') -Raw | ConvertFrom-Json
} else { $null }

# Test 11: Preflight variant_g_result
Write-Output ''
Write-Output '--- Test 11: Preflight variant_g_result ---'
$vgResult = if ($null -ne $pfl) { [string]$pfl.variant_g_result } else { '' }
Assert-True ($vgResult -eq 'MAP7F_VARIANT_G_MAP_FOLDER_SCAN_EMPTY') `
    "Test11: variant_g_result == MAP7F_VARIANT_G_MAP_FOLDER_SCAN_EMPTY (got '$vgResult')"

# Test 12: Preflight variants_abcdefg_exhausted=true
Write-Output ''
Write-Output '--- Test 12: Preflight variants_abcdefg_exhausted ---'
$abcdefg = if ($null -ne $pfl) { [bool]$pfl.variants_abcdefg_exhausted } else { $false }
Assert-True ($abcdefg -eq $true) 'Test12: preflight variants_abcdefg_exhausted == true'

# Test 13: Preflight common_media_maps_layout=true
Write-Output ''
Write-Output '--- Test 13: Preflight common_media_maps_layout ---'
$cmlFlag = if ($null -ne $pfl) { [bool]$pfl.common_media_maps_layout } else { $false }
Assert-True ($cmlFlag -eq $true) 'Test13: preflight common_media_maps_layout == true'

# Test 14: Packet contains HUMAN-ONLY install language
Write-Output ''
Write-Output '--- Test 14: Packet contains HUMAN-ONLY language ---'
$installContent = if (Test-Path (Join-Path $packetOut 'MAP_7L_EXPERIMENT_H_MANUAL_INSTALL_COMMANDS.md')) {
    Get-Content (Join-Path $packetOut 'MAP_7L_EXPERIMENT_H_MANUAL_INSTALL_COMMANDS.md') -Raw
} else { '' }
Assert-True ($installContent -match 'HUMAN-ONLY') 'Test14: install commands contain HUMAN-ONLY'

# Test 15: Packet public_playable_claim_allowed=false
Write-Output ''
Write-Output '--- Test 15: Packet public_playable_claim_allowed=false ---'
Assert-True ($null -ne $pfl -and [bool]$pfl.public_playable_claim_allowed -eq $false) `
    'Test15: preflight public_playable_claim_allowed == false'

# ---------------------------------------------------------------------------
# Summary
# ---------------------------------------------------------------------------

Write-Output ''
Write-Output '----------------------------------------'
Write-Output "Results: $pass passed, $fail failed"
Write-Output '----------------------------------------'

if ($fail -gt 0) { exit 1 }
exit 0
