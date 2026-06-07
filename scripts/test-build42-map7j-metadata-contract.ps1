#Requires -Version 5.1
<#
.SYNOPSIS
    Tests for MAP-7J: inspect-build42-map-metadata-contract.ps1 and
    prepare-build42-map7j-metadata-contract-packet.ps1.

    Uses synthetic/local .local fixtures.
    Does NOT depend on user-local logs or PZ install assets.
    Expected assertion count: 17
#>

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$scriptDir       = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot        = Split-Path -Parent $scriptDir
$inspectorScript = Join-Path $repoRoot 'scripts\inspect-build42-map-metadata-contract.ps1'
$packetScript    = Join-Path $repoRoot 'scripts\prepare-build42-map7j-metadata-contract-packet.ps1'
$tempRoot        = [System.IO.Path]::GetTempPath()
$mapId           = 'pzmapforge_build42_candidate_v4_001'

$pass = 0
$fail = 0

function Assert-True {
    param([bool]$Condition, [string]$Label)
    if ($Condition) { Write-Output "  PASS  $Label"; $script:pass++ }
    else            { Write-Output "  FAIL  $Label"; $script:fail++ }
}

Write-Output 'test-build42-map7j-metadata-contract.ps1'
Write-Output ''

# ---------------------------------------------------------------------------
# Setup: synthetic fixtures
# ---------------------------------------------------------------------------

$testBase = Join-Path $tempRoot ('pzmf-t7j-' + [System.IO.Path]::GetRandomFileName())
$badPath  = Join-Path $tempRoot 'pzmf-t7j-bad-no-local'
New-Item -ItemType Directory -Force -Path $badPath | Out-Null

# Build a dual-layout synthetic candidate
$fixture = Join-Path $testBase 'fixture'
$fix42ModDir  = Join-Path $fixture '42'
$fix42MapDir  = Join-Path $fixture "42\media\maps\$mapId"
$fixRootMapDir = Join-Path $fixture "media\maps\$mapId"
New-Item -ItemType Directory -Force -Path $fix42MapDir  | Out-Null
New-Item -ItemType Directory -Force -Path $fixRootMapDir | Out-Null

$modInfoContent = "id=$mapId`nname=Test Map Mod`ndescription=Test`nposter=poster.png`n"
$mapInfoContent = "id=$mapId`ntitle=Test Map`ndescription=A test map`n"

# 42/mod.info (no-BOM)
[System.IO.File]::WriteAllText((Join-Path $fix42ModDir 'mod.info'),
    $modInfoContent, [System.Text.UTF8Encoding]::new($false))
# root/mod.info (byte-identical copy)
[System.IO.File]::WriteAllText((Join-Path $fixture 'mod.info'),
    $modInfoContent, [System.Text.UTF8Encoding]::new($false))
# 42/media/maps/map.info (no-BOM)
[System.IO.File]::WriteAllText((Join-Path $fix42MapDir 'map.info'),
    $mapInfoContent, [System.Text.UTF8Encoding]::new($false))
# root/media/maps/map.info (byte-identical copy)
[System.IO.File]::WriteAllText((Join-Path $fixRootMapDir 'map.info'),
    $mapInfoContent, [System.Text.UTF8Encoding]::new($false))

# Fixture with wrong ModId (to test mismatch detection)
$fixtureMismatch = Join-Path $testBase 'fixture-mismatch'
$fixMM42ModDir   = Join-Path $fixtureMismatch '42'
$fixMM42MapDir   = Join-Path $fixtureMismatch "42\media\maps\$mapId"
New-Item -ItemType Directory -Force -Path $fixMM42MapDir | Out-Null
[System.IO.File]::WriteAllText((Join-Path $fixMM42ModDir 'mod.info'),
    "id=wrong_mod_id`nname=Wrong`n", [System.Text.UTF8Encoding]::new($false))
[System.IO.File]::WriteAllText((Join-Path $fixMM42MapDir 'map.info'),
    "id=wrong_map_id`ntitle=Wrong Map`n", [System.Text.UTF8Encoding]::new($false))

function Invoke-Inspector {
    param([string]$Root, [string]$OutDir, [string]$MId = $mapId, [string]$ModId = $mapId)
    & powershell -ExecutionPolicy Bypass -File $inspectorScript `
        -CandidateRoot $Root -Output $OutDir -MapId $MId -ModId $ModId | Out-Null
    return [int]$LASTEXITCODE
}

function Invoke-Packet {
    param([string]$OutDir)
    & powershell -ExecutionPolicy Bypass -File $packetScript -Output $OutDir | Out-Null
    return [int]$LASTEXITCODE
}

# ===========================================================================
# Inspector tests (1-10)
# ===========================================================================

# Test 1: Inspector refuses output outside .local
Write-Output '--- Test 1: Inspector refuses output outside .local ---'
$t1exit = Invoke-Inspector -Root $fixture -OutDir $badPath
Assert-True ($t1exit -ne 0) 'Test1: inspector output outside .local exits nonzero'

# Run inspector on fixture
$outInsp = Join-Path $testBase '.local\inspector'
Invoke-Inspector -Root $fixture -OutDir $outInsp | Out-Null
$dInsp = if (Test-Path (Join-Path $outInsp 'map-metadata-contract-report.json')) {
    Get-Content (Join-Path $outInsp 'map-metadata-contract-report.json') -Raw | ConvertFrom-Json
} else { $null }

# Test 2: Parses root mod.info id field
Write-Output ''
Write-Output '--- Test 2: Parses root mod.info id field ---'
$rootModId = if ($null -ne $dInsp) { [string]$dInsp.root_mod_info_id } else { '' }
Assert-True ($rootModId -eq $mapId) `
    "Test2: root_mod_info_id == $mapId (got '$rootModId')"

# Test 3: Parses 42/mod.info id field
Write-Output ''
Write-Output '--- Test 3: Parses 42/mod.info id field ---'
$v42ModId2 = if ($null -ne $dInsp) { [string]$dInsp.v42_mod_info_id } else { '' }
Assert-True ($v42ModId2 -eq $mapId) `
    "Test3: v42_mod_info_id == $mapId (got '$v42ModId2')"

# Test 4: Parses root map.info id field
Write-Output ''
Write-Output '--- Test 4: Parses root map.info id field ---'
$rootMapId = if ($null -ne $dInsp) { [string]$dInsp.root_map_info_id } else { '' }
Assert-True ($rootMapId -eq $mapId) `
    "Test4: root_map_info_id == $mapId (got '$rootMapId')"

# Test 5: Parses 42 map.info id field
Write-Output ''
Write-Output '--- Test 5: Parses 42/map.info id field ---'
$v42MapId2 = if ($null -ne $dInsp) { [string]$dInsp.v42_map_info_id } else { '' }
Assert-True ($v42MapId2 -eq $mapId) `
    "Test5: v42_map_info_id == $mapId (got '$v42MapId2')"

# Test 6: Detects no-BOM (fixture files are no-BOM)
Write-Output ''
Write-Output '--- Test 6: Detects no-BOM on 42/mod.info ---'
$v42ModNoBom = if ($null -ne $dInsp) { [bool]$dInsp.v42_mod_info.no_bom } else { $false }
Assert-True ($v42ModNoBom -eq $true) `
    "Test6: v42_mod_info.no_bom == true (got $v42ModNoBom)"

# Test 7: Detects byte-identical root/42 files
Write-Output ''
Write-Output '--- Test 7: Detects byte-identical root/42 mod.info ---'
$modIdentical = if ($null -ne $dInsp) { [bool]$dInsp.mod_info_bytes_identical } else { $false }
Assert-True ($modIdentical -eq $true) `
    "Test7: mod_info_bytes_identical == true (got $modIdentical)"

# Test 8: Records ModId match (fixture has matching id)
Write-Output ''
Write-Output '--- Test 8: Records ModId match correctly ---'
$modIdMatch = if ($null -ne $dInsp) { [bool]$dInsp.v42_mod_info_id_matches_expected } else { $false }
Assert-True ($modIdMatch -eq $true) `
    "Test8: v42_mod_info_id_matches_expected == true"

# Test 9: Inspector writes JSON and MD
Write-Output ''
Write-Output '--- Test 9: Inspector writes JSON and MD ---'
$jsonExists = Test-Path (Join-Path $outInsp 'map-metadata-contract-report.json')
$mdExists   = Test-Path (Join-Path $outInsp 'map-metadata-contract-report.md')
Assert-True ($jsonExists -and $mdExists) 'Test9: map-metadata-contract-report.json and .md both exist'

# Test 10: JSON includes public_playable_claim_allowed=false
Write-Output ''
Write-Output '--- Test 10: JSON includes public_playable_claim_allowed=false ---'
Assert-True ($null -ne $dInsp -and [bool]$dInsp.public_playable_claim_allowed -eq $false) `
    'Test10: public_playable_claim_allowed == false'

# ===========================================================================
# Packet tests (11-17)
# ===========================================================================

# Test 11: Packet refuses output outside .local
Write-Output ''
Write-Output '--- Test 11: Packet refuses output outside .local ---'
$t11exit = Invoke-Packet -OutDir $badPath
Assert-True ($t11exit -ne 0) 'Test11: packet output outside .local exits nonzero'

# Run packet
Write-Output ''
Write-Output '--- Running packet (Tests 12-17) ---'
$packetOut = Join-Path $testBase '.local\packet'
$t12exit   = Invoke-Packet -OutDir $packetOut

# Test 12: Packet writes all required files
$reqFiles = @(
    'MAP_7J_METADATA_CONTRACT_PACKET.md',
    'MAP_7J_VARIANT_E_RESULT_SUMMARY.md',
    'MAP_7J_METADATA_CONTRACT_REPORT.md',
    'MAP_7J_NEXT_HYPOTHESES.md',
    'MAP_7J_EXPERIMENT_F_MANUAL_RESULT.local-template.md',
    'map7j-metadata-contract-preflight.json',
    'map7j-metadata-contract-preflight.md',
    'map-discovery-path-report.json',
    'map-discovery-path-report.md',
    'map-metadata-contract-report.json',
    'map-metadata-contract-report.md'
)
$allPresent = $true
foreach ($f in $reqFiles) {
    if (-not (Test-Path (Join-Path $packetOut $f))) { $allPresent = $false }
}
Assert-True ($t12exit -eq 0 -and $allPresent) 'Test12: packet exits 0 and all 11 required files present'

$pfl = if (Test-Path (Join-Path $packetOut 'map7j-metadata-contract-preflight.json')) {
    Get-Content (Join-Path $packetOut 'map7j-metadata-contract-preflight.json') -Raw | ConvertFrom-Json
} else { $null }

# Test 13: Preflight records variant_e_result
Write-Output ''
Write-Output '--- Test 13: Preflight variant_e_result ---'
$veResult = if ($null -ne $pfl) { [string]$pfl.variant_e_result } else { '' }
Assert-True ($veResult -eq 'MAP7F_VARIANT_E_MAP_FOLDER_SCAN_EMPTY') `
    "Test13: preflight variant_e_result == MAP7F_VARIANT_E_MAP_FOLDER_SCAN_EMPTY (got '$veResult')"

# Test 14: Preflight records metadata_contract_focus=true
Write-Output ''
Write-Output '--- Test 14: Preflight metadata_contract_focus ---'
$mcFocus = if ($null -ne $pfl) { [bool]$pfl.metadata_contract_focus } else { $false }
Assert-True ($mcFocus -eq $true) 'Test14: preflight metadata_contract_focus == true'

# Test 15: Packet main MD says A/B/C/D/E exhausted
Write-Output ''
Write-Output '--- Test 15: Packet says variants A-E exhausted ---'
$packetContent = if (Test-Path (Join-Path $packetOut 'MAP_7J_METADATA_CONTRACT_PACKET.md')) {
    Get-Content (Join-Path $packetOut 'MAP_7J_METADATA_CONTRACT_PACKET.md') -Raw
} else { '' }
Assert-True ($packetContent -match 'VARIANTS_ABCDE_EXHAUSTED') `
    'Test15: packet contains VARIANTS_ABCDE_EXHAUSTED'

# Test 16: Packet says no load test performed
Write-Output ''
Write-Output '--- Test 16: Packet says no load test ---'
Assert-True ($packetContent -match 'LOAD_TEST_NOT_PERFORMED') `
    'Test16: packet contains LOAD_TEST_NOT_PERFORMED'

# Test 17: Packet says no public playable claim
Write-Output ''
Write-Output '--- Test 17: Packet public_playable_claim_allowed=false ---'
Assert-True ($null -ne $pfl -and [bool]$pfl.public_playable_claim_allowed -eq $false) `
    'Test17: preflight public_playable_claim_allowed == false'

# ---------------------------------------------------------------------------
# Summary
# ---------------------------------------------------------------------------

Write-Output ''
Write-Output '----------------------------------------'
Write-Output "Results: $pass passed, $fail failed"
Write-Output '----------------------------------------'

if ($fail -gt 0) { exit 1 }
exit 0
