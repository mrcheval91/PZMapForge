#Requires -Version 5.1
<#
.SYNOPSIS
    Tests for MAP-7H: inspect-build42-map-discovery-path.ps1 and
    prepare-build42-map7h-discovery-path-packet.ps1.

    Uses synthetic fixtures; does NOT depend on user-local logs or PZ install.
    Expected assertion count: 12
#>

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$scriptDir       = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot        = Split-Path -Parent $scriptDir
$inspectorScript = Join-Path $repoRoot 'scripts\inspect-build42-map-discovery-path.ps1'
$packetScript    = Join-Path $repoRoot 'scripts\prepare-build42-map7h-discovery-path-packet.ps1'
$tempRoot        = [System.IO.Path]::GetTempPath()

$pass = 0
$fail = 0

function Assert-True {
    param([bool]$Condition, [string]$Label)
    if ($Condition) { Write-Output "  PASS  $Label"; $script:pass++ }
    else            { Write-Output "  FAIL  $Label"; $script:fail++ }
}

Write-Output 'test-build42-map7h-discovery-path.ps1'
Write-Output ''

# ---------------------------------------------------------------------------
# Setup
# ---------------------------------------------------------------------------

$testBase   = Join-Path $tempRoot ('pzmf-t7h-' + [System.IO.Path]::GetRandomFileName())
$badPath    = Join-Path $tempRoot 'pzmf-t7h-bad-no-local'
$mapId      = 'pzmapforge_build42_candidate_v4_001'

New-Item -ItemType Directory -Force -Path $badPath | Out-Null

# Fixture A: versioned 42/ layout only (no root media/maps)
$fixtureA = Join-Path $testBase 'fixture-a'
$fixtureA42MediaMaps = Join-Path $fixtureA "42\media\maps\$mapId"
New-Item -ItemType Directory -Force -Path $fixtureA42MediaMaps | Out-Null
New-Item -ItemType Directory -Force -Path (Join-Path $fixtureA '42') | Out-Null

# mod.info (no-BOM)
[System.IO.File]::WriteAllText((Join-Path $fixtureA '42\mod.info'),
    "id=$mapId`nname=Test Mod`n", [System.Text.UTF8Encoding]::new($false))
# map.info (no-BOM)
[System.IO.File]::WriteAllText((Join-Path $fixtureA42MediaMaps 'map.info'),
    "id=$mapId`ntitle=Test Map`n", [System.Text.UTF8Encoding]::new($false))
# spawnpoints.lua (no-BOM)
[System.IO.File]::WriteAllText((Join-Path $fixtureA42MediaMaps 'spawnpoints.lua'),
    "-- spawnpoints`n", [System.Text.UTF8Encoding]::new($false))
# objects.lua (no-BOM)
[System.IO.File]::WriteAllText((Join-Path $fixtureA42MediaMaps 'objects.lua'),
    "-- objects`n", [System.Text.UTF8Encoding]::new($false))
# Binary stubs
[System.IO.File]::WriteAllBytes((Join-Path $fixtureA42MediaMaps '0_0.lotheader'), [byte[]]@(0x4C,0x54,0x5A,0x48))
[System.IO.File]::WriteAllBytes((Join-Path $fixtureA42MediaMaps 'world_0_0.lotpack'), [byte[]]@(0x4C,0x4F,0x54,0x50))
[System.IO.File]::WriteAllBytes((Join-Path $fixtureA42MediaMaps 'chunkdata_0_0.bin'), [byte[]]@(0x00,0x01))

# Fixture B: versioned 42/ layout + root media/maps (to test H1 hypothesis check)
$fixtureB = Join-Path $testBase 'fixture-b'
$fixtureB42MediaMaps   = Join-Path $fixtureB "42\media\maps\$mapId"
$fixtureBRootMediaMaps = Join-Path $fixtureB "media\maps\$mapId"
New-Item -ItemType Directory -Force -Path $fixtureB42MediaMaps   | Out-Null
New-Item -ItemType Directory -Force -Path $fixtureBRootMediaMaps | Out-Null
New-Item -ItemType Directory -Force -Path (Join-Path $fixtureB '42') | Out-Null
[System.IO.File]::WriteAllText((Join-Path $fixtureB '42\mod.info'),
    "id=$mapId`n", [System.Text.UTF8Encoding]::new($false))
[System.IO.File]::WriteAllText((Join-Path $fixtureB42MediaMaps 'map.info'),
    "id=$mapId`n", [System.Text.UTF8Encoding]::new($false))
[System.IO.File]::WriteAllText((Join-Path $fixtureBRootMediaMaps 'map.info'),
    "id=$mapId`n", [System.Text.UTF8Encoding]::new($false))

# Fixture C: versioned 42/ layout + map.info with BOM (to test BOM detection)
$fixtureC = Join-Path $testBase 'fixture-c'
$fixtureC42MediaMaps = Join-Path $fixtureC "42\media\maps\$mapId"
New-Item -ItemType Directory -Force -Path $fixtureC42MediaMaps | Out-Null
New-Item -ItemType Directory -Force -Path (Join-Path $fixtureC '42') | Out-Null
[System.IO.File]::WriteAllText((Join-Path $fixtureC '42\mod.info'),
    "id=$mapId`n", [System.Text.UTF8Encoding]::new($false))
# map.info WITH BOM
$bomBytes = [byte[]]@(0xEF, 0xBB, 0xBF) + [System.Text.Encoding]::UTF8.GetBytes("id=$mapId`n")
[System.IO.File]::WriteAllBytes((Join-Path $fixtureC42MediaMaps 'map.info'), $bomBytes)

function Invoke-Inspector {
    param([string]$Root, [string]$OutDir)
    & powershell -ExecutionPolicy Bypass -File $inspectorScript `
        -CandidateRoot $Root -Output $OutDir -MapId $mapId | Out-Null
    return [int]$LASTEXITCODE
}

function Invoke-Packet {
    param([string]$OutDir)
    & powershell -ExecutionPolicy Bypass -File $packetScript -Output $OutDir | Out-Null
    return [int]$LASTEXITCODE
}

# ===========================================================================
# Inspector tests
# ===========================================================================

# Test 1: Inspector refuses output outside .local
Write-Output '--- Test 1: Inspector refuses output outside .local ---'
$t1exit = Invoke-Inspector -Root $fixtureA -OutDir $badPath
Assert-True ($t1exit -ne 0) 'Test1: inspector output outside .local exits nonzero'

# Test 2: Versioned 42/media/maps detected
Write-Output ''
Write-Output '--- Test 2: Versioned 42/media/maps detected ---'
$outA = Join-Path $testBase '.local\inspector-a'
Invoke-Inspector -Root $fixtureA -OutDir $outA | Out-Null
$dA = if (Test-Path (Join-Path $outA 'map-discovery-path-report.json')) {
    Get-Content (Join-Path $outA 'map-discovery-path-report.json') -Raw | ConvertFrom-Json
} else { $null }
Assert-True ([bool]$dA.has_versioned_42_media_maps -eq $true) `
    "Test2: fixture-a -> has_versioned_42_media_maps == true"

# Test 3: Missing root media/maps detected
Write-Output ''
Write-Output '--- Test 3: Missing root media/maps detected ---'
Assert-True ([bool]$dA.root_media_maps_missing -eq $true) `
    "Test3: fixture-a -> root_media_maps_missing == true"

# Test 4: Root media/maps present when fixture includes it
Write-Output ''
Write-Output '--- Test 4: Root media/maps present in fixture-b ---'
$outB = Join-Path $testBase '.local\inspector-b'
Invoke-Inspector -Root $fixtureB -OutDir $outB | Out-Null
$dB = if (Test-Path (Join-Path $outB 'map-discovery-path-report.json')) {
    Get-Content (Join-Path $outB 'map-discovery-path-report.json') -Raw | ConvertFrom-Json
} else { $null }
Assert-True ([bool]$dB.has_root_media_maps -eq $true) `
    "Test4: fixture-b -> has_root_media_maps == true"

# Test 5: No-BOM correctly detected on no-BOM map.info
Write-Output ''
Write-Output '--- Test 5: No-BOM detected on no-BOM map.info ---'
$noBoMap = $dA.versioned_42_files.map_info
Assert-True ([bool]$noBoMap.no_bom -eq $true) `
    "Test5: fixture-a map.info -> no_bom == true"

# Test 6: BOM correctly detected on BOM map.info
Write-Output ''
Write-Output '--- Test 6: BOM detected on BOM map.info ---'
$outC = Join-Path $testBase '.local\inspector-c'
Invoke-Inspector -Root $fixtureC -OutDir $outC | Out-Null
$dC = if (Test-Path (Join-Path $outC 'map-discovery-path-report.json')) {
    Get-Content (Join-Path $outC 'map-discovery-path-report.json') -Raw | ConvertFrom-Json
} else { $null }
$bomMapInfo = $dC.versioned_42_files.map_info
Assert-True ([bool]$bomMapInfo.no_bom -eq $false) `
    "Test6: fixture-c map.info (with BOM) -> no_bom == false"

# ===========================================================================
# Packet tests
# ===========================================================================

# Test 7: Packet refuses output outside .local
Write-Output ''
Write-Output '--- Test 7: Packet refuses output outside .local ---'
$t7exit = Invoke-Packet -OutDir $badPath
Assert-True ($t7exit -ne 0) 'Test7: packet output outside .local exits nonzero'

# Tests 8-12: Valid packet run
Write-Output ''
Write-Output '--- Tests 8-12: packet run ---'
$packetOut = Join-Path $testBase '.local\packet'
$t8exit = Invoke-Packet -OutDir $packetOut

# Test 8: Packet exits 0
Assert-True ($t8exit -eq 0) 'Test8: packet exits 0'

# Test 9: All required packet files present
$reqFiles = @(
    'MAP_7H_DISCOVERY_PATH_PACKET.md',
    'MAP_7H_DISCOVERY_PATH_HYPOTHESES.md',
    'MAP_7H_VARIANT_RESULTS_SUMMARY.md',
    'MAP_7H_NEXT_MANUAL_EXPERIMENTS.local-template.md',
    'map7h-discovery-preflight.json',
    'map7h-discovery-preflight.md',
    'map-discovery-path-report.json',
    'map-discovery-path-report.md'
)
$allPresent = $true
foreach ($f in $reqFiles) {
    if (-not (Test-Path (Join-Path $packetOut $f))) { $allPresent = $false }
}
Assert-True $allPresent 'Test9: all 8 required packet files present'

# Test 10: Preflight JSON has variants_abc_exhausted=true and public_playable=false
$pfl = if (Test-Path (Join-Path $packetOut 'map7h-discovery-preflight.json')) {
    Get-Content (Join-Path $packetOut 'map7h-discovery-preflight.json') -Raw | ConvertFrom-Json
} else { $null }
Assert-True ($null -ne $pfl -and [bool]$pfl.variants_abc_exhausted -eq $true -and
             [bool]$pfl.public_playable_claim_allowed -eq $false) `
    'Test10: preflight variants_abc_exhausted==true and public_playable_claim_allowed==false'

# Test 11: Packet contains LOAD_TEST_NOT_PERFORMED sentinel
$packetContent = if (Test-Path (Join-Path $packetOut 'MAP_7H_DISCOVERY_PATH_PACKET.md')) {
    Get-Content (Join-Path $packetOut 'MAP_7H_DISCOVERY_PATH_PACKET.md') -Raw
} else { '' }
Assert-True ($packetContent -match 'LOAD_TEST_NOT_PERFORMED') `
    'Test11: packet contains LOAD_TEST_NOT_PERFORMED'

# Test 12: Packet hypotheses contain root media/maps experiment reference
$hypContent = if (Test-Path (Join-Path $packetOut 'MAP_7H_DISCOVERY_PATH_HYPOTHESES.md')) {
    Get-Content (Join-Path $packetOut 'MAP_7H_DISCOVERY_PATH_HYPOTHESES.md') -Raw
} else { '' }
Assert-True ($hypContent -match 'root.*media.*maps' -or $hypContent -match 'Experiment D') `
    'Test12: hypotheses doc references root media/maps experiment'

# ---------------------------------------------------------------------------
# Summary
# ---------------------------------------------------------------------------

Write-Output ''
Write-Output '----------------------------------------'
Write-Output "Results: $pass passed, $fail failed"
Write-Output '----------------------------------------'

if ($fail -gt 0) { exit 1 }
exit 0
