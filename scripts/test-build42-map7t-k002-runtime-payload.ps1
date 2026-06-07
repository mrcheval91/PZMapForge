#Requires -Version 5.1
<#
.SYNOPSIS
    Tests for MAP-7T: inspect-build42-workshop-runtime-payload.ps1 and
    prepare-build42-map7t-k002-record-packet.ps1.

    Expected assertion count: 20
#>

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$scriptDir       = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot        = Split-Path -Parent $scriptDir
$inspectorScript = Join-Path $repoRoot 'scripts\inspect-build42-workshop-runtime-payload.ps1'
$packetScript    = Join-Path $repoRoot 'scripts\prepare-build42-map7t-k002-record-packet.ps1'
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

Write-Output 'test-build42-map7t-k002-runtime-payload.ps1'
Write-Output ''

$testBase = Join-Path $tempRoot ('pzmf-t7t-' + [System.IO.Path]::GetRandomFileName())
$badPath  = Join-Path $tempRoot 'pzmf-t7t-bad-no-local'
New-Item -ItemType Directory -Force -Path $badPath | Out-Null

function Invoke-Inspector {
    param([string]$CandRoot, [string]$RefRoot, [string]$OutDir)
    New-Item -ItemType Directory -Force -Path $OutDir | Out-Null
    & powershell -ExecutionPolicy Bypass -File $inspectorScript `
        -CandidateWorkshopRoot $CandRoot `
        -ReferenceWorkshopRoot $RefRoot `
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
# Build synthetic fixture roots
# ---------------------------------------------------------------------------

# Candidate: Dru_map-aligned layout with binary files
$candRoot     = Join-Path $testBase 'candidate-root'
$cand42Dir    = Join-Path $candRoot '42'
$candMapsDir  = Join-Path $candRoot "common\media\maps\$candidateMapId"
$candMapsSubdir = Join-Path $candMapsDir 'maps'
$candModsDir  = Join-Path $candRoot "mods\$candidateMapId"
New-Item -ItemType Directory -Force -Path $cand42Dir      | Out-Null
New-Item -ItemType Directory -Force -Path $candMapsDir    | Out-Null
New-Item -ItemType Directory -Force -Path $candMapsSubdir | Out-Null
New-Item -ItemType Directory -Force -Path $candModsDir    | Out-Null

Set-Content -Path (Join-Path $candRoot 'mod.info')      -Value "id=$candidateMapId`n" -Encoding ASCII
Set-Content -Path (Join-Path $cand42Dir 'mod.info')     -Value "id=$candidateMapId`n" -Encoding ASCII
# NO common/mod.info
Set-Content -Path (Join-Path $candMapsDir 'map.info')   -Value "title=Test`nlots=NONE`nzoomX=0`nzoomY=0`n" -Encoding ASCII
Set-Content -Path (Join-Path $candMapsDir 'spawnpoints.lua') -Value "-- spawn`n" -Encoding ASCII
Set-Content -Path (Join-Path $candMapsDir 'objects.lua')     -Value "-- objects`n" -Encoding ASCII
[System.IO.File]::WriteAllBytes((Join-Path $candMapsDir '0_0.lotheader'),    ([byte[]]@(0x4C,0x4F,0x54,0x48,0x01,0x00,0x00,0x00) + [System.Byte[]]::new(29638)))
[System.IO.File]::WriteAllBytes((Join-Path $candMapsDir 'world_0_0.lotpack'), [System.Byte[]]::new(64))
[System.IO.File]::WriteAllBytes((Join-Path $candMapsDir 'chunkdata_0_0.bin'), [System.Byte[]]::new(902))

# Reference: Dru_map-style layout (WITH common/mod.info to test detection)
$refRoot     = Join-Path $testBase 'reference-root'
$ref42Dir    = Join-Path $refRoot '42'
$refMapsDir  = Join-Path $refRoot "common\media\maps\$referenceMapId"
New-Item -ItemType Directory -Force -Path $ref42Dir      | Out-Null
New-Item -ItemType Directory -Force -Path (Join-Path $refRoot 'common') | Out-Null
New-Item -ItemType Directory -Force -Path $refMapsDir    | Out-Null

Set-Content -Path (Join-Path $refRoot 'mod.info')     -Value "id=$referenceMapId`n"  -Encoding ASCII
Set-Content -Path (Join-Path $ref42Dir 'mod.info')    -Value "id=$referenceMapId`n"  -Encoding ASCII
Set-Content -Path (Join-Path $refRoot 'common\mod.info') -Value "id=$referenceMapId`n" -Encoding ASCII
Set-Content -Path (Join-Path $refMapsDir 'map.info')  -Value "title=Ref`nlots=NONE`nzoomX=0`nzoomY=0`n" -Encoding ASCII
[System.IO.File]::WriteAllBytes((Join-Path $refMapsDir '0_0.lotheader'), ([byte[]]@(0x4C,0x4F,0x54,0x48) + [System.Byte[]]::new(100)))

# Contents/mods fixture (test that layout)
$contentsRoot    = Join-Path $testBase 'contents-root'
$contentsMod     = Join-Path $contentsRoot "Contents\mods\$candidateMapId"
New-Item -ItemType Directory -Force -Path $contentsMod | Out-Null
Set-Content -Path (Join-Path $contentsMod 'mod.info') -Value "id=$candidateMapId`n" -Encoding ASCII

# ---------------------------------------------------------------------------
# Test 1: Inspector refuses output outside .local
# ---------------------------------------------------------------------------
Write-Output '--- Test 1: Inspector refuses output outside .local ---'
$t1exit = Invoke-Inspector -CandRoot $candRoot -RefRoot $refRoot -OutDir $badPath
Assert-True ($t1exit -ne 0) 'Test1: inspector output outside .local exits nonzero'

# Run inspector with valid synthetic roots
Write-Output ''
Write-Output '--- Running inspector (Tests 2-13) ---'
$inspOut = Join-Path $testBase '.local\inspection'
$t2exit  = Invoke-Inspector -CandRoot $candRoot -RefRoot $refRoot -OutDir $inspOut

# Test 2: Inspector exits 0
Assert-True ($t2exit -eq 0) 'Test2: inspector exits 0 with valid roots'

# Test 3: JSON output exists
Assert-True (Test-Path (Join-Path $inspOut 'workshop-runtime-payload-comparison.json')) `
    'Test3: workshop-runtime-payload-comparison.json exists'

# Test 4: MD output exists
Assert-True (Test-Path (Join-Path $inspOut 'workshop-runtime-payload-comparison.md')) `
    'Test4: workshop-runtime-payload-comparison.md exists'

# Parse JSON
$inspJson = if (Test-Path (Join-Path $inspOut 'workshop-runtime-payload-comparison.json')) {
    Get-Content (Join-Path $inspOut 'workshop-runtime-payload-comparison.json') -Raw | ConvertFrom-Json
} else { $null }

# Test 5: Candidate root_mod_info_present=true
Assert-True ($null -ne $inspJson -and [bool]$inspJson.candidate.root_mod_info_present -eq $true) `
    'Test5: candidate root_mod_info_present=true'

# Test 6: Candidate versioned_42_mod_info_present=true
Assert-True ($null -ne $inspJson -and [bool]$inspJson.candidate.versioned_42_mod_info_present -eq $true) `
    'Test6: candidate versioned_42_mod_info_present=true'

# Test 7: Candidate common_mod_info_present=false
Assert-True ($null -ne $inspJson -and [bool]$inspJson.candidate.common_mod_info_present -eq $false) `
    'Test7: candidate common_mod_info_present=false (no common/mod.info in candidate)'

# Test 8: Candidate common_media_maps_present=true
Assert-True ($null -ne $inspJson -and [bool]$inspJson.candidate.common_media_maps_present -eq $true) `
    'Test8: candidate common_media_maps_present=true'

# Test 9: Candidate lotheader_files not empty
$candLothCount = if ($null -ne $inspJson) { @($inspJson.candidate.lotheader_files).Count } else { 0 }
Assert-True ($candLothCount -gt 0) `
    "Test9: candidate lotheader_files not empty (count=$candLothCount)"

# Test 10: Reference root_mod_info_present=true
Assert-True ($null -ne $inspJson -and [bool]$inspJson.reference.root_mod_info_present -eq $true) `
    'Test10: reference root_mod_info_present=true'

# Test 11: Reference common_mod_info_present=true (we set this in the reference fixture)
Assert-True ($null -ne $inspJson -and [bool]$inspJson.reference.common_mod_info_present -eq $true) `
    'Test11: reference common_mod_info_present=true'

# Test 12: Contents/mods layout detected in separate inspector run
$contentsOut = Join-Path $testBase '.local\inspection-contents'
$t12exit = Invoke-Inspector -CandRoot $contentsRoot -RefRoot $refRoot -OutDir $contentsOut
$contentsJson = if (Test-Path (Join-Path $contentsOut 'workshop-runtime-payload-comparison.json')) {
    Get-Content (Join-Path $contentsOut 'workshop-runtime-payload-comparison.json') -Raw | ConvertFrom-Json
} else { $null }
Assert-True ($null -ne $contentsJson -and [bool]$contentsJson.candidate.contents_mods_subdir_present -eq $true) `
    'Test12: Contents/mods/<id> layout detected (contents_mods_subdir_present=true)'

# Test 13: JSON has public_playable_claim_allowed=false
Assert-True ($null -ne $inspJson -and [bool]$inspJson.public_playable_claim_allowed -eq $false) `
    'Test13: inspection JSON public_playable_claim_allowed=false'

# ---------------------------------------------------------------------------
# Packet tests
# ---------------------------------------------------------------------------

# Test 14: Packet refuses output outside .local
Write-Output ''
Write-Output '--- Test 14: Packet refuses output outside .local ---'
$t14exit = Invoke-Packet -OutDir $badPath
Assert-True ($t14exit -ne 0) 'Test14: packet outside .local exits nonzero'

# Test 15-20: Packet with valid path
Write-Output ''
Write-Output '--- Tests 15-20: Packet outputs ---'
$packetOut = Join-Path $testBase '.local\packet'
$t15exit   = Invoke-Packet -OutDir $packetOut

Assert-True ($t15exit -eq 0 -and (Test-Path (Join-Path $packetOut 'MAP_7T_K002_RESULT_SUMMARY.md'))) `
    'Test15: packet exits 0 and MAP_7T_K002_RESULT_SUMMARY.md exists'

Assert-True (Test-Path (Join-Path $packetOut 'MAP_7T_NEXT_DECISION_TREE.md')) `
    'Test16: MAP_7T_NEXT_DECISION_TREE.md exists'

$preflightPath = Join-Path $packetOut 'map7t-preflight.json'
$pfl = if (Test-Path $preflightPath) { Get-Content $preflightPath -Raw | ConvertFrom-Json } else { $null }
Assert-True ($null -ne $pfl -and [bool]$pfl.binary_writer_gate_closed -eq $true) `
    'Test17: preflight binary_writer_gate_closed=true'

$summaryContent = if (Test-Path (Join-Path $packetOut 'MAP_7T_K002_RESULT_SUMMARY.md')) {
    Get-Content (Join-Path $packetOut 'MAP_7T_K002_RESULT_SUMMARY.md') -Raw
} else { '' }

Assert-True ($summaryContent -match '3740642200') `
    'Test18: K002 result summary contains 3740642200 (Workshop ID)'

Assert-True ($summaryContent -match 'ABSENT|absent|no expected.map') `
    'Test19: K002 result summary mentions absent expected-map evidence'

Assert-True ($summaryContent -match 'PUBLIC_PLAYABLE_CLAIM_ALLOWED=false') `
    'Test20: K002 result summary contains PUBLIC_PLAYABLE_CLAIM_ALLOWED=false'

Write-Output ''
Write-Output '----------------------------------------'
Write-Output "Results: $pass passed, $fail failed"
Write-Output '----------------------------------------'

if ($fail -gt 0) { exit 1 }
exit 0
