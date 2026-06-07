#Requires -Version 5.1
<#
.SYNOPSIS
    Tests for MAP-7N: -ReferenceMapId support in
    inspect-build42-known-working-map-contract.ps1.

    Uses synthetic .local fixtures with different candidate/reference map IDs.
    Expected assertion count: 9
#>

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$scriptDir        = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot         = Split-Path -Parent $scriptDir
$comparatorScript = Join-Path $repoRoot 'scripts\inspect-build42-known-working-map-contract.ps1'
$packetScript     = Join-Path $repoRoot 'scripts\prepare-build42-map7m-known-working-contract-packet.ps1'
$tempRoot         = [System.IO.Path]::GetTempPath()

$candMapId = 'pzmapforge_build42_candidate_v4_001'
$refMapId  = 'Dru_map_test'

$pass = 0
$fail = 0

function Assert-True {
    param([bool]$Condition, [string]$Label)
    if ($Condition) { Write-Output "  PASS  $Label"; $script:pass++ }
    else            { Write-Output "  FAIL  $Label"; $script:fail++ }
}

Write-Output 'test-build42-map7n-reference-map-id.ps1'
Write-Output ''

# ---------------------------------------------------------------------------
# Setup: synthetic fixtures under .local
# ---------------------------------------------------------------------------

$testBase = Join-Path $tempRoot ('pzmf-t7n-' + [System.IO.Path]::GetRandomFileName())
$badPath  = Join-Path $tempRoot 'pzmf-t7n-bad-no-local'
New-Item -ItemType Directory -Force -Path $badPath | Out-Null

# Fixture: candidate (common/ layout, candMapId)
$localCand = Join-Path $testBase ".local\cand"
$localCandCommonMaps = Join-Path $localCand "common\media\maps\$candMapId"
$localCandCommon     = Join-Path $localCand 'common'
$localCand42         = Join-Path $localCand '42'
New-Item -ItemType Directory -Force -Path $localCandCommonMaps | Out-Null
New-Item -ItemType Directory -Force -Path $localCand42         | Out-Null
[System.IO.File]::WriteAllText((Join-Path $localCand42 'mod.info'),
    "id=$candMapId`nname=Candidate Mod`ndescription=test`n",
    [System.Text.UTF8Encoding]::new($false))
[System.IO.File]::WriteAllText((Join-Path $localCand 'mod.info'),
    "id=$candMapId`nname=Candidate Mod`n",
    [System.Text.UTF8Encoding]::new($false))
[System.IO.File]::WriteAllText((Join-Path $localCandCommonMaps 'map.info'),
    "id=$candMapId`ntitle=Candidate Map`nlots=1`n",
    [System.Text.UTF8Encoding]::new($false))
[System.IO.File]::WriteAllText((Join-Path $localCandCommon 'mod.info'),
    "id=$candMapId`nname=Candidate Mod`n",
    [System.Text.UTF8Encoding]::new($false))

# Fixture: reference (common/ layout, refMapId -- DIFFERENT from candMapId)
$localRef = Join-Path $testBase ".local\ref"
$localRefCommonMaps = Join-Path $localRef "common\media\maps\$refMapId"
$localRef42         = Join-Path $localRef '42'
New-Item -ItemType Directory -Force -Path $localRefCommonMaps | Out-Null
New-Item -ItemType Directory -Force -Path $localRef42         | Out-Null
[System.IO.File]::WriteAllText((Join-Path $localRef42 'mod.info'),
    "id=$refMapId`nname=Reference Mod`ncategory=map`npzversion=42.0`nversionMin=42.0`n",
    [System.Text.UTF8Encoding]::new($false))
[System.IO.File]::WriteAllText((Join-Path $localRef 'mod.info'),
    "id=$refMapId`nname=Reference Mod`n",
    [System.Text.UTF8Encoding]::new($false))
[System.IO.File]::WriteAllText((Join-Path $localRefCommonMaps 'map.info'),
    "id=$refMapId`ntitle=Reference Map`nlots=5`ndescription=A working map`nfixed2x=true`n",
    [System.Text.UTF8Encoding]::new($false))

function Invoke-Comparator {
    param([string]$Cand, [string]$Ref, [string]$CMapId, [string]$RMapId, [string]$OutDir)
    & powershell -ExecutionPolicy Bypass -File $comparatorScript `
        -CandidateRoot $Cand -ReferenceRoot $Ref `
        -MapId $CMapId -ReferenceMapId $RMapId `
        -Output $OutDir | Out-Null
    return [int]$LASTEXITCODE
}

# ---------------------------------------------------------------------------
# Test 1: Comparator still refuses non-.local CandidateRoot
# ---------------------------------------------------------------------------

Write-Output '--- Test 1: Comparator still refuses non-.local CandidateRoot ---'
$outT1 = Join-Path $testBase '.local\t1'
New-Item -ItemType Directory -Force -Path $outT1 | Out-Null
$t1exit = Invoke-Comparator -Cand $badPath -Ref $localRef -CMapId $candMapId -RMapId $refMapId -OutDir $outT1
Assert-True ($t1exit -ne 0) 'Test1: comparator refuses non-.local CandidateRoot'

# ---------------------------------------------------------------------------
# Test 2: Comparator still refuses non-.local ReferenceRoot
# ---------------------------------------------------------------------------

Write-Output ''
Write-Output '--- Test 2: Comparator still refuses non-.local ReferenceRoot ---'
$outT2 = Join-Path $testBase '.local\t2'
New-Item -ItemType Directory -Force -Path $outT2 | Out-Null
$t2exit = Invoke-Comparator -Cand $localCand -Ref $badPath -CMapId $candMapId -RMapId $refMapId -OutDir $outT2
Assert-True ($t2exit -ne 0) 'Test2: comparator refuses non-.local ReferenceRoot'

# ---------------------------------------------------------------------------
# Test 3: Comparator exits 0 with separate -MapId and -ReferenceMapId
# ---------------------------------------------------------------------------

Write-Output ''
Write-Output '--- Test 3: Comparator accepts separate MapId and ReferenceMapId ---'
$outT3 = Join-Path $testBase '.local\t3'
$t3exit = Invoke-Comparator -Cand $localCand -Ref $localRef `
    -CMapId $candMapId -RMapId $refMapId -OutDir $outT3
Assert-True ($t3exit -eq 0) 'Test3: comparator exits 0 with separate map IDs'

# Load report
$dT3 = if (Test-Path (Join-Path $outT3 'map-known-working-contract-report.json')) {
    Get-Content (Join-Path $outT3 'map-known-working-contract-report.json') -Raw | ConvertFrom-Json
} else { $null }

# ---------------------------------------------------------------------------
# Test 4: Comparator finds reference common/media/maps/<ReferenceMapId>
# ---------------------------------------------------------------------------

Write-Output ''
Write-Output '--- Test 4: Reference common/media/maps/<ReferenceMapId> found ---'
$refHasCommon = if ($null -ne $dT3) { [bool]$dT3.reference_layout.has_common_media_maps } else { $false }
Assert-True ($refHasCommon -eq $true) `
    "Test4: reference_layout.has_common_media_maps == true (using ReferenceMapId=$refMapId)"

# ---------------------------------------------------------------------------
# Test 5: Comparator reports candidate_map_id
# ---------------------------------------------------------------------------

Write-Output ''
Write-Output '--- Test 5: candidate_map_id reported ---'
$cMapId = if ($null -ne $dT3) { [string]$dT3.candidate_map_id } else { '' }
Assert-True ($cMapId -eq $candMapId) `
    "Test5: candidate_map_id == $candMapId (got '$cMapId')"

# ---------------------------------------------------------------------------
# Test 6: Comparator reports reference_map_id
# ---------------------------------------------------------------------------

Write-Output ''
Write-Output '--- Test 6: reference_map_id reported ---'
$rMapId = if ($null -ne $dT3) { [string]$dT3.reference_map_id } else { '' }
Assert-True ($rMapId -eq $refMapId) `
    "Test6: reference_map_id == $refMapId (got '$rMapId')"

# ---------------------------------------------------------------------------
# Test 7: Candidate layout scanned with candidate MapId
# ---------------------------------------------------------------------------

Write-Output ''
Write-Output '--- Test 7: Candidate layout uses candidate MapId ---'
$candHasCommon = if ($null -ne $dT3) { [bool]$dT3.candidate_layout.has_common_media_maps } else { $false }
Assert-True ($candHasCommon -eq $true) `
    "Test7: candidate_layout.has_common_media_maps == true (using MapId=$candMapId)"

# ---------------------------------------------------------------------------
# Test 8: Packet command examples contain -ReferenceMapId
# ---------------------------------------------------------------------------

Write-Output ''
Write-Output '--- Test 8: Packet commands include -ReferenceMapId ---'
$packetContent = if (Test-Path $packetScript) { Get-Content $packetScript -Raw } else { '' }
Assert-True ($packetContent -match 'ReferenceMapId') `
    'Test8: packet script contains -ReferenceMapId in command examples'

# ---------------------------------------------------------------------------
# Test 9: public_playable_claim_allowed=false remains
# ---------------------------------------------------------------------------

Write-Output ''
Write-Output '--- Test 9: public_playable_claim_allowed=false ---'
Assert-True ($null -ne $dT3 -and [bool]$dT3.public_playable_claim_allowed -eq $false) `
    'Test9: report public_playable_claim_allowed == false'

# ---------------------------------------------------------------------------
# Summary
# ---------------------------------------------------------------------------

Write-Output ''
Write-Output '----------------------------------------'
Write-Output "Results: $pass passed, $fail failed"
Write-Output '----------------------------------------'

if ($fail -gt 0) { exit 1 }
exit 0
