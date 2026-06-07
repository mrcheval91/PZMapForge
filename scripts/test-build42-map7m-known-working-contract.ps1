#Requires -Version 5.1
<#
.SYNOPSIS
    Tests for MAP-7M: inspect-build42-known-working-map-contract.ps1 and
    prepare-build42-map7m-known-working-contract-packet.ps1.

    Uses synthetic .local fixtures.
    Does NOT depend on user-local logs or PZ install.
    Expected assertion count: 12
#>

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$scriptDir        = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot         = Split-Path -Parent $scriptDir
$comparatorScript = Join-Path $repoRoot 'scripts\inspect-build42-known-working-map-contract.ps1'
$packetScript     = Join-Path $repoRoot 'scripts\prepare-build42-map7m-known-working-contract-packet.ps1'
$tempRoot         = [System.IO.Path]::GetTempPath()
$mapId            = 'pzmapforge_build42_candidate_v4_001'

$pass = 0
$fail = 0

function Assert-True {
    param([bool]$Condition, [string]$Label)
    if ($Condition) { Write-Output "  PASS  $Label"; $script:pass++ }
    else            { Write-Output "  FAIL  $Label"; $script:fail++ }
}

Write-Output 'test-build42-map7m-known-working-contract.ps1'
Write-Output ''

# ---------------------------------------------------------------------------
# Setup: synthetic fixtures
# ---------------------------------------------------------------------------

$testBase = Join-Path $tempRoot ('pzmf-t7m-' + [System.IO.Path]::GetRandomFileName())
$badPath  = Join-Path $tempRoot 'pzmf-t7m-bad-no-local'
New-Item -ItemType Directory -Force -Path $badPath | Out-Null

# Fixture A (candidate-like): 42/ layout, minimal mod.info fields
$fixA = Join-Path $testBase 'fixture-a'
$fixA42 = Join-Path $fixA '42'
$fixA42Maps = Join-Path $fixA42 "media\maps\$mapId"
New-Item -ItemType Directory -Force -Path $fixA42Maps | Out-Null
[System.IO.File]::WriteAllText((Join-Path $fixA42 'mod.info'),
    "id=$mapId`nname=Test Mod`ndescription=Test`n",
    [System.Text.UTF8Encoding]::new($false))
[System.IO.File]::WriteAllText((Join-Path $fixA42Maps 'map.info'),
    "id=$mapId`ntitle=Test Map`n",
    [System.Text.UTF8Encoding]::new($false))

# Fixture B (reference-like): 42.0/ layout, richer mod.info/map.info fields
$fixB = Join-Path $testBase 'fixture-b'
$fixB420 = Join-Path $fixB '42.0'
$fixBCommon = Join-Path $fixB 'common'
$fixBCommonMaps = Join-Path $fixBCommon "media\maps\$mapId"
New-Item -ItemType Directory -Force -Path (Join-Path $fixB420 'dummy') | Out-Null  # create 42.0 folder
Remove-Item -LiteralPath (Join-Path $fixB420 'dummy')
New-Item -ItemType Directory -Force -Path $fixBCommonMaps | Out-Null
[System.IO.File]::WriteAllText((Join-Path $fixB420 'mod.info'),
    "id=$mapId`nname=Reference Mod`ncategory=map`npzversion=42.0`nversionMin=42.0`nmap=$mapId`n",
    [System.Text.UTF8Encoding]::new($false))
[System.IO.File]::WriteAllText((Join-Path $fixBCommonMaps 'map.info'),
    "id=$mapId`ntitle=Reference Map`nlots=1`ndescription=A map`nfixed2x=true`n",
    [System.Text.UTF8Encoding]::new($false))

function Invoke-Comparator {
    param([string]$Cand, [string]$Ref, [string]$OutDir)
    & powershell -ExecutionPolicy Bypass -File $comparatorScript `
        -CandidateRoot $Cand -ReferenceRoot $Ref -Output $OutDir -MapId $mapId | Out-Null
    return [int]$LASTEXITCODE
}

function Invoke-Packet {
    param([string]$OutDir)
    & powershell -ExecutionPolicy Bypass -File $packetScript -Output $OutDir | Out-Null
    return [int]$LASTEXITCODE
}

# ===========================================================================
# Comparator tests (1-8)
# ===========================================================================

# Test 1: Comparator refuses non-.local CandidateRoot
Write-Output '--- Test 1: Comparator refuses non-.local CandidateRoot ---'
$outT1 = Join-Path $testBase '.local\t1'
New-Item -ItemType Directory -Force -Path $outT1 | Out-Null
$t1exit = Invoke-Comparator -Cand $badPath -Ref $fixA -OutDir $outT1
Assert-True ($t1exit -ne 0) 'Test1: comparator refuses non-.local CandidateRoot'

# Test 2: Comparator refuses non-.local ReferenceRoot
Write-Output ''
Write-Output '--- Test 2: Comparator refuses non-.local ReferenceRoot ---'
$outT2 = Join-Path $testBase '.local\t2'
New-Item -ItemType Directory -Force -Path $outT2 | Out-Null
$t2exit = Invoke-Comparator -Cand $fixA -Ref $badPath -OutDir $outT2
Assert-True ($t2exit -ne 0) 'Test2: comparator refuses non-.local ReferenceRoot'

# --- BUT fixture paths ARE under .local? No -- fixtures are under temp.
# The fixtures must be under .local for the comparator to accept them.
# Re-create fixtures under .local
$localFixA = Join-Path $testBase '.local\fixture-a'
$localFixA42 = Join-Path $localFixA '42'
$localFixA42Maps = Join-Path $localFixA42 "media\maps\$mapId"
New-Item -ItemType Directory -Force -Path $localFixA42Maps | Out-Null
[System.IO.File]::WriteAllText((Join-Path $localFixA42 'mod.info'),
    "id=$mapId`nname=Test Mod`ndescription=Test`n",
    [System.Text.UTF8Encoding]::new($false))
[System.IO.File]::WriteAllText((Join-Path $localFixA42Maps 'map.info'),
    "id=$mapId`ntitle=Test Map`n",
    [System.Text.UTF8Encoding]::new($false))

$localFixB = Join-Path $testBase '.local\fixture-b'
$localFixB420 = Join-Path $localFixB '42.0'
$localFixBCommon = Join-Path $localFixB 'common'
$localFixBCommonMaps = Join-Path $localFixBCommon "media\maps\$mapId"
New-Item -ItemType Directory -Force -Path $localFixB420      | Out-Null
New-Item -ItemType Directory -Force -Path $localFixBCommonMaps | Out-Null
[System.IO.File]::WriteAllText((Join-Path $localFixB420 'mod.info'),
    "id=$mapId`nname=Reference Mod`ncategory=map`npzversion=42.0`nversionMin=42.0`nmap=$mapId`n",
    [System.Text.UTF8Encoding]::new($false))
[System.IO.File]::WriteAllText((Join-Path $localFixBCommonMaps 'map.info'),
    "id=$mapId`ntitle=Reference Map`nlots=1`ndescription=A map`nfixed2x=true`n",
    [System.Text.UTF8Encoding]::new($false))

# Test 3: Comparator produces report for two .local fixtures
Write-Output ''
Write-Output '--- Test 3: Comparator produces report ---'
$outT3 = Join-Path $testBase '.local\t3'
New-Item -ItemType Directory -Force -Path $outT3 | Out-Null
$t3exit = Invoke-Comparator -Cand $localFixA -Ref $localFixB -OutDir $outT3
$dT3 = if (Test-Path (Join-Path $outT3 'map-known-working-contract-report.json')) {
    Get-Content (Join-Path $outT3 'map-known-working-contract-report.json') -Raw | ConvertFrom-Json
} else { $null }
Assert-True ($t3exit -eq 0 -and $null -ne $dT3) 'Test3: comparator exits 0 and produces JSON report'

# Test 4: Detects differing mod.info fields (ref has more fields)
Write-Output ''
Write-Output '--- Test 4: Detects differing mod.info fields ---'
$modGap = if ($null -ne $dT3) { @($dT3.mod_info_fields_in_reference_not_candidate) } else { @() }
$hasModGap = ($modGap.Count -gt 0)
Assert-True $hasModGap `
    "Test4: mod_info_fields_in_reference_not_candidate has entries (got $($modGap.Count))"

# Test 5: Detects differing map.info fields (ref has lots, description, fixed2x)
Write-Output ''
Write-Output '--- Test 5: Detects differing map.info fields ---'
$mapGap = if ($null -ne $dT3) { @($dT3.map_info_fields_in_reference_not_candidate) } else { @() }
$hasMapGap = ($mapGap.Count -gt 0)
Assert-True $hasMapGap `
    "Test5: map_info_fields_in_reference_not_candidate has entries (got $($mapGap.Count))"

# Test 6: Detects 42 vs 42.0 folder difference
Write-Output ''
Write-Output '--- Test 6: Detects 42 vs 42.0 folder difference ---'
$candHas42   = if ($null -ne $dT3) { [bool]$dT3.candidate_layout.has_42_folder    } else { $false }
$refHas420   = if ($null -ne $dT3) { [bool]$dT3.reference_layout.has_42_0_folder  } else { $false }
$refNotHas42 = if ($null -ne $dT3) { -not [bool]$dT3.reference_layout.has_42_folder } else { $true }
Assert-True ($candHas42 -and $refHas420 -and $refNotHas42) `
    "Test6: candidate has 42/, reference has 42.0/ (not 42/): cand42=$candHas42, ref420=$refHas420"

# Test 7: Detects common/media/maps presence in reference
Write-Output ''
Write-Output '--- Test 7: Detects common/media/maps in reference ---'
$refHasCommon = if ($null -ne $dT3) { [bool]$dT3.reference_layout.has_common_media_maps } else { $false }
Assert-True ($refHasCommon -eq $true) 'Test7: reference has_common_media_maps == true'

# Test 8: Detects no-BOM/ASCII for text files
Write-Output ''
Write-Output '--- Test 8: Detects no-BOM status for text files ---'
$candModNoBom = if ($null -ne $dT3) { [bool]$dT3.candidate_layout.mod_info_no_bom } else { $false }
Assert-True ($candModNoBom -eq $true) 'Test8: candidate mod_info_no_bom == true'

# ===========================================================================
# Packet tests (9-12)
# ===========================================================================

# Test 9: Packet creates all expected docs
Write-Output ''
Write-Output '--- Running packet (Tests 9-12) ---'
$packetOut = Join-Path $testBase '.local\packet'
$t9exit    = Invoke-Packet -OutDir $packetOut

$reqFiles = @(
    'MAP_7M_KNOWN_WORKING_CONTRACT_PACKET.md',
    'MAP_7M_VARIANT_H_RESULT_SUMMARY.md',
    'MAP_7M_REFERENCE_CAPTURE_INSTRUCTIONS.md',
    'MAP_7M_NEXT_DECISION_TREE.md',
    'map7m-preflight.json',
    'map7m-preflight.md'
)
$allPresent = $true
foreach ($f in $reqFiles) {
    if (-not (Test-Path (Join-Path $packetOut $f))) { $allPresent = $false }
}
Assert-True ($t9exit -eq 0 -and $allPresent) 'Test9: packet exits 0 and all 6 required docs present'

$pfl = if (Test-Path (Join-Path $packetOut 'map7m-preflight.json')) {
    Get-Content (Join-Path $packetOut 'map7m-preflight.json') -Raw | ConvertFrom-Json
} else { $null }

# Test 10: Packet contains VARIANTS_ABCDEFGH_EXHAUSTED
Write-Output ''
Write-Output '--- Test 10: Packet contains VARIANTS_ABCDEFGH_EXHAUSTED ---'
$packetContent = if (Test-Path (Join-Path $packetOut 'MAP_7M_KNOWN_WORKING_CONTRACT_PACKET.md')) {
    Get-Content (Join-Path $packetOut 'MAP_7M_KNOWN_WORKING_CONTRACT_PACKET.md') -Raw
} else { '' }
Assert-True ($packetContent -match 'VARIANTS_ABCDEFGH_EXHAUSTED') `
    'Test10: packet contains VARIANTS_ABCDEFGH_EXHAUSTED'

# Test 11: Packet contains PUBLIC_PLAYABLE_CLAIM_ALLOWED=false
Write-Output ''
Write-Output '--- Test 11: Packet public_playable_claim_allowed=false ---'
Assert-True ($null -ne $pfl -and [bool]$pfl.public_playable_claim_allowed -eq $false) `
    'Test11: preflight public_playable_claim_allowed == false'

# Test 12: Packet states no automatic PZ read/write
Write-Output ''
Write-Output '--- Test 12: Packet no automatic PZ read/write ---'
Assert-True ($null -ne $pfl -and [bool]$pfl.no_automatic_pz_read_or_write -eq $true) `
    'Test12: preflight no_automatic_pz_read_or_write == true'

# ---------------------------------------------------------------------------
# Summary
# ---------------------------------------------------------------------------

Write-Output ''
Write-Output '----------------------------------------'
Write-Output "Results: $pass passed, $fail failed"
Write-Output '----------------------------------------'

if ($fail -gt 0) { exit 1 }
exit 0
