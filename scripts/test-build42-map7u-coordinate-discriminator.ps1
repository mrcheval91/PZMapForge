#Requires -Version 5.1
<#
.SYNOPSIS
    Tests for MAP-7U: inspect-build42-workshop-cell-coordinate-contract.ps1 and
    prepare-build42-map7u-coordinate-discriminator-packet.ps1.

    Expected assertion count: 20
#>

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$scriptDir       = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot        = Split-Path -Parent $scriptDir
$inspectorScript = Join-Path $repoRoot 'scripts\inspect-build42-workshop-cell-coordinate-contract.ps1'
$packetScript    = Join-Path $repoRoot 'scripts\prepare-build42-map7u-coordinate-discriminator-packet.ps1'
$tempRoot        = [System.IO.Path]::GetTempPath()
$candidateMapId  = 'pzmapforge_build42_candidate_v4_001'
$referenceMapId  = 'Dru_map'

$pass = 0
$fail = 0

function Assert-True {
    param([bool]$Condition, [string]$Label)
    if ($Condition) { Write-Output "  PASS  $Label"; $script:pass++ }
    else            { Write-Output "  FAIL  $Label"; $script:fail++ }
}

Write-Output 'test-build42-map7u-coordinate-discriminator.ps1'
Write-Output ''

$testBase = Join-Path $tempRoot ('pzmf-t7u-' + [System.IO.Path]::GetRandomFileName())
$badPath  = Join-Path $tempRoot 'pzmf-t7u-bad-no-local'
New-Item -ItemType Directory -Force -Path $badPath | Out-Null

# ---------------------------------------------------------------------------
# Build synthetic fixture roots for inspector tests
# ---------------------------------------------------------------------------

# Candidate: single cell 0_0 with zoomX=0, worldX/Y=0
$candRoot    = Join-Path $testBase 'candidate-modroot'
$candMapsDir = Join-Path $candRoot "common\media\maps\$candidateMapId"
New-Item -ItemType Directory -Force -Path (Join-Path $candRoot '42') | Out-Null
New-Item -ItemType Directory -Force -Path $candMapsDir | Out-Null

Set-Content -Path (Join-Path $candRoot 'mod.info')    -Value "id=$candidateMapId`n" -Encoding ASCII
Set-Content -Path (Join-Path $candRoot '42\mod.info') -Value "id=$candidateMapId`n" -Encoding ASCII
Set-Content -Path (Join-Path $candMapsDir 'map.info') `
    -Value "title=Test`nlots=NONE`nzoomX=0`nzoomY=0`nzoomS=1`n" -Encoding ASCII
Set-Content -Path (Join-Path $candMapsDir 'spawnpoints.lua') `
    -Value "-- spawn`nlocal sp = { Profession_Unemployed = { { worldX = 0, worldY = 0, posX = 150, posY = 150, posZ = 0 } } }`nreturn sp`n" -Encoding ASCII
[System.IO.File]::WriteAllBytes((Join-Path $candMapsDir '0_0.lotheader'),    ([System.Byte[]]::new(64)))
[System.IO.File]::WriteAllBytes((Join-Path $candMapsDir 'world_0_0.lotpack'), ([System.Byte[]]::new(64)))
[System.IO.File]::WriteAllBytes((Join-Path $candMapsDir 'chunkdata_0_0.bin'), ([System.Byte[]]::new(64)))

# Reference: 4 cells (35_27, 36_27, 35_28, 36_28) with Dru_map-style coords
$refRoot    = Join-Path $testBase 'reference-modroot'
$refMapsDir = Join-Path $refRoot "common\media\maps\$referenceMapId"
New-Item -ItemType Directory -Force -Path (Join-Path $refRoot '42') | Out-Null
New-Item -ItemType Directory -Force -Path $refMapsDir | Out-Null

Set-Content -Path (Join-Path $refRoot 'mod.info')    -Value "id=$referenceMapId`n" -Encoding ASCII
Set-Content -Path (Join-Path $refRoot '42\mod.info') -Value "id=$referenceMapId`n" -Encoding ASCII
Set-Content -Path (Join-Path $refMapsDir 'map.info') `
    -Value "title=Ref`nlots=NONE`nzoomX=10505`nzoomY=12220`nzoomS=14.5`n" -Encoding ASCII
Set-Content -Path (Join-Path $refMapsDir 'spawnpoints.lua') `
    -Value "-- spawn`nlocal sp = { Profession_Unemployed = { { worldX = 35, worldY = 27, posX = 246, posY = 188, posZ = 0 } } }`nreturn sp`n" -Encoding ASCII
foreach ($cell in @('35_27','36_27','35_28','36_28')) {
    [System.IO.File]::WriteAllBytes((Join-Path $refMapsDir "$cell.lotheader"), ([System.Byte[]]::new(64)))
}

function Invoke-Inspector {
    param([string]$CandRoot, [string]$RefRoot, [string]$OutDir)
    New-Item -ItemType Directory -Force -Path $OutDir | Out-Null
    & powershell -ExecutionPolicy Bypass -File $inspectorScript `
        -CandidateModRoot $CandRoot `
        -ReferenceModRoot $RefRoot `
        -CandidateMapId $candidateMapId `
        -ReferenceMapId $referenceMapId `
        -Output $OutDir | Out-Null
    return [int]$LASTEXITCODE
}

function Invoke-Packet {
    param([string]$OutDir)
    & powershell -ExecutionPolicy Bypass -File $packetScript -Output $OutDir | Out-Null
    return [int]$LASTEXITCODE
}

# ---------------------------------------------------------------------------
# Test 1: Inspector refuses output outside .local
# ---------------------------------------------------------------------------
Write-Output '--- Test 1: Inspector refuses output outside .local ---'
$t1exit = Invoke-Inspector -CandRoot $candRoot -RefRoot $refRoot -OutDir $badPath
Assert-True ($t1exit -ne 0) 'Test1: inspector output outside .local exits nonzero'

# Run inspector
Write-Output ''
Write-Output '--- Running inspector (Tests 2-8) ---'
$inspOut = Join-Path $testBase '.local\inspection'
$t2exit  = Invoke-Inspector -CandRoot $candRoot -RefRoot $refRoot -OutDir $inspOut

# Test 2: Inspector exits 0 + JSON exists
Assert-True ($t2exit -eq 0 -and (Test-Path (Join-Path $inspOut 'workshop-cell-coordinate-contract.json'))) `
    'Test2: inspector exits 0 and JSON exists'

# Parse JSON
$inspJson = if (Test-Path (Join-Path $inspOut 'workshop-cell-coordinate-contract.json')) {
    Get-Content (Join-Path $inspOut 'workshop-cell-coordinate-contract.json') -Raw | ConvertFrom-Json
} else { $null }

# Test 3: Candidate lotheader count = 1
Assert-True ($null -ne $inspJson -and [int]$inspJson.candidate_cells.lotheader_count -eq 1) `
    'Test3: candidate_lotheader_count=1 (single 0_0 cell)'

# Test 4: Reference has more lotheaders than candidate
$refCount  = if ($null -ne $inspJson) { [int]$inspJson.reference_cells.lotheader_count } else { 0 }
$candCount = if ($null -ne $inspJson) { [int]$inspJson.candidate_cells.lotheader_count } else { 0 }
Assert-True ($refCount -gt $candCount) `
    "Test4: reference_lotheader_count($refCount) > candidate($candCount)"

# Test 5: Inspector extracts candidate zoomX=0
Assert-True ($null -ne $inspJson -and [string]$inspJson.candidate_zoom.zoom_x -eq '0') `
    "Test5: candidate zoomX=0 (got '$($inspJson.candidate_zoom.zoom_x)')"

# Test 6: Inspector extracts reference zoomX=10505
Assert-True ($null -ne $inspJson -and [string]$inspJson.reference_zoom.zoom_x -eq '10505') `
    "Test6: reference zoomX=10505 (got '$($inspJson.reference_zoom.zoom_x)')"

# Test 7: Inspector extracts candidate spawn worldX=0 and worldY=0
Assert-True ($null -ne $inspJson -and [string]$inspJson.candidate_spawn.first_world_x -eq '0' -and
             [string]$inspJson.candidate_spawn.first_world_y -eq '0') `
    "Test7: candidate spawn worldX=0 worldY=0"

# Test 8: Inspector detects candidate spawn cell exists (0_0.lotheader present)
Assert-True ($null -ne $inspJson -and [bool]$inspJson.candidate_spawn_cell_exists -eq $true) `
    'Test8: candidate_spawn_cell_exists=true (0_0.lotheader matches spawn target)'

# ---------------------------------------------------------------------------
# Packet tests
# ---------------------------------------------------------------------------

# Test 9: Packet refuses output outside .local
Write-Output ''
Write-Output '--- Test 9: Packet refuses output outside .local ---'
$t9exit = Invoke-Packet -OutDir $badPath
Assert-True ($t9exit -ne 0) 'Test9: packet outside .local exits nonzero'

# Test 10: Packet exits 0
Write-Output ''
Write-Output '--- Tests 10-20: Packet outputs ---'
$packetOut = Join-Path $testBase '.local\packet'
$t10exit   = Invoke-Packet -OutDir $packetOut
Assert-True ($t10exit -eq 0) 'Test10: packet exits 0'

# Tests 11-18: Required docs
$reqFiles = @(
    'MAP_7U_COORDINATE_DISCRIMINATOR_PACKET.md',
    'MAP_7U_MODROOT_LAYOUT_MATCH_SUMMARY.md',
    'MAP_7U_COORDINATE_ALIGNED_STAGING_MANIFEST.md',
    'MAP_7U_HUMAN_WORKSHOP_UPDATE_CHECKLIST.md',
    'MAP_7U_SERVER_WIRING_AFTER_UPDATE_TEMPLATE.md',
    'MAP_7U_SUCCESS_FAILURE_CRITERIA.md',
    'map7u-preflight.json',
    'map7u-preflight.md'
)
Write-Output '--- Tests 11-18: Required docs ---'
foreach ($f in $reqFiles) {
    Assert-True (Test-Path (Join-Path $packetOut $f)) "Test: $f exists"
}

# Test 19: Preflight has modroot_layout_match=true and coordinate_aligned_target_cell=35_27
$pfl = if (Test-Path (Join-Path $packetOut 'map7u-preflight.json')) {
    Get-Content (Join-Path $packetOut 'map7u-preflight.json') -Raw | ConvertFrom-Json
} else { $null }
Assert-True ($null -ne $pfl -and
             [bool]$pfl.modroot_layout_match -eq $true -and
             [string]$pfl.coordinate_aligned_target_cell -eq '35_27') `
    "Test19: preflight modroot_layout_match=true and coordinate_aligned_target_cell=35_27"

# Test 20: Staged package contains 35_27.lotheader, spawnpoints worldX=35, map.info zoomX=10505
$stagedMaps = Join-Path $packetOut "staged-workshop-coordinate-aligned\$candidateMapId\common\media\maps\$candidateMapId"
$loth35     = Test-Path (Join-Path $stagedMaps '35_27.lotheader')
$spawnContent = if (Test-Path (Join-Path $stagedMaps 'spawnpoints.lua')) {
    Get-Content (Join-Path $stagedMaps 'spawnpoints.lua') -Raw
} else { '' }
$mapContent = if (Test-Path (Join-Path $stagedMaps 'map.info')) {
    Get-Content (Join-Path $stagedMaps 'map.info') -Raw
} else { '' }

Assert-True ($loth35 -and
             ($spawnContent -match 'worldX\s*=\s*35') -and
             ($mapContent   -match '(?m)^zoomX=10505')) `
    "Test20: staged 35_27.lotheader exists, spawnpoints worldX=35, map.info zoomX=10505"

Write-Output ''
Write-Output '----------------------------------------'
Write-Output "Results: $pass passed, $fail failed"
Write-Output '----------------------------------------'

if ($fail -gt 0) { exit 1 }
exit 0
