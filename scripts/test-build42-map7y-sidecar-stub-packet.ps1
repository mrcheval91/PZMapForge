#Requires -Version 5.1
<#
.SYNOPSIS
    Tests for MAP-7Y: prepare-build42-map7y-sidecar-stub-packet.ps1.

    Expected assertion count: 24
#>

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$scriptDir    = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot     = Split-Path -Parent $scriptDir
$packetScript = Join-Path $repoRoot 'scripts\prepare-build42-map7y-sidecar-stub-packet.ps1'
$tempRoot     = [System.IO.Path]::GetTempPath()

$pass = 0
$fail = 0

function Assert-True {
    param([bool]$Condition, [string]$Label)
    if ($Condition) { Write-Output "  PASS  $Label"; $script:pass++ }
    else            { Write-Output "  FAIL  $Label"; $script:fail++ }
}

Write-Output 'test-build42-map7y-sidecar-stub-packet.ps1'
Write-Output ''

$testBase = Join-Path $tempRoot ('pzmf-t7y-' + [System.IO.Path]::GetRandomFileName())
$badPath  = Join-Path $tempRoot 'pzmf-t7y-bad-no-local'
New-Item -ItemType Directory -Force -Path $badPath | Out-Null

function Invoke-Packet {
    param([string]$OutDir)
    & powershell -ExecutionPolicy Bypass -File $packetScript -Output $OutDir | Out-Null
    return [int]$LASTEXITCODE
}

# Test 1: Packet refuses output outside .local
Write-Output '--- Test 1: Packet refuses output outside .local ---'
$t1exit = Invoke-Packet -OutDir $badPath
Assert-True ($t1exit -ne 0) 'Test1: packet output outside .local exits nonzero'

# Run packet
Write-Output ''
Write-Output '--- Running packet (Tests 2-24) ---'
$packetOut = Join-Path $testBase '.local\packet'
$t2exit    = Invoke-Packet -OutDir $packetOut

# Test 2: Packet exits 0
Assert-True ($t2exit -eq 0) 'Test2: packet exits 0 with valid output path'

# Tests 3-7: Required doc files
Write-Output ''
Write-Output '--- Tests 3-7: Required docs ---'
$reqFiles = @(
    'MAP_7Y_MINIMAL_SIDECAR_STUB_PACKET.md',
    'MAP_7Y_STAGED_PACKAGE_MANIFEST.md',
    'MAP_7Y_HUMAN_WORKSHOP_UPDATE_CHECKLIST.md',
    'MAP_7Y_LOG_CAPTURE_AFTER_UPLOAD.md',
    'MAP_7Y_SUCCESS_FAILURE_CRITERIA.md'
)
foreach ($f in $reqFiles) {
    Assert-True (Test-Path (Join-Path $packetOut $f)) "Test: $f exists"
}

# Test 8: Preflight JSON and MD exist
Assert-True ((Test-Path (Join-Path $packetOut 'map7y-preflight.json')) -and
             (Test-Path (Join-Path $packetOut 'map7y-preflight.md'))) `
    'Test8: map7y-preflight.json and map7y-preflight.md exist'

# Parse preflight JSON
$pfl = if (Test-Path (Join-Path $packetOut 'map7y-preflight.json')) {
    Get-Content (Join-Path $packetOut 'map7y-preflight.json') -Raw | ConvertFrom-Json
} else { $null }

# Test 9: Schema correct
Write-Output ''
Write-Output '--- Tests 9-19: Preflight JSON ---'
Assert-True ($null -ne $pfl -and [string]$pfl.schema -eq 'pzmapforge.map7y-preflight.v0.1') `
    "Test9: schema correct (got '$($pfl.schema)')"

# Test 10: source_map7x_commit
Assert-True ($null -ne $pfl -and [string]$pfl.source_map7x_commit -eq '8c45c0a') `
    'Test10: source_map7x_commit=8c45c0a'

# Test 11: map_bin_discriminator=false
Assert-True ($null -ne $pfl -and [bool]$pfl.map_bin_discriminator -eq $false) `
    'Test11: map_bin_discriminator=false'

# Test 12: sidecar_probe_created=true + bak_sidecars_created=false
Assert-True ($null -ne $pfl -and [bool]$pfl.sidecar_probe_created -eq $true -and
             [bool]$pfl.bak_sidecars_created -eq $false) `
    'Test12: sidecar_probe_created=true and bak_sidecars_created=false'

# Test 13: generated_sidecars includes streets.xml.bin + worldmap.xml.bin
$sidecars = if ($null -ne $pfl -and $null -ne $pfl.generated_sidecars) {
    @($pfl.generated_sidecars)
} else { @() }
Assert-True ($sidecars -contains 'streets.xml.bin' -and $sidecars -contains 'worldmap.xml.bin') `
    'Test13: generated_sidecars includes streets.xml.bin and worldmap.xml.bin'

# Test 14: generated_sidecars includes worldmap-forest.xml.bin + worldmap.png
Assert-True ($sidecars -contains 'worldmap-forest.xml.bin' -and $sidecars -contains 'worldmap.png') `
    'Test14: generated_sidecars includes worldmap-forest.xml.bin and worldmap.png'

# Test 15: third_party_reference_files_copied=false
Assert-True ($null -ne $pfl -and [bool]$pfl.third_party_reference_files_copied -eq $false) `
    'Test15: third_party_reference_files_copied=false'

# Test 16: coordinate_aligned_binaries_present=true
Assert-True ($null -ne $pfl -and [bool]$pfl.coordinate_aligned_binaries_present -eq $true) `
    'Test16: coordinate_aligned_binaries_present=true'

# Test 17: binary_writer_gate_closed=true + public_playable_claim_allowed=false
Assert-True ($null -ne $pfl -and
             [bool]$pfl.binary_writer_gate_closed -eq $true -and
             [bool]$pfl.public_playable_claim_allowed -eq $false) `
    'Test17: binary_writer_gate_closed=true and public_playable_claim_allowed=false'

# Staged package paths
$candidateMapId = 'pzmapforge_build42_candidate_v4_001'
$stagedMaps     = Join-Path $packetOut "staged-workshop-sidecar-stubs\$candidateMapId\common\media\maps\$candidateMapId"

# Tests 18-20: Binary files staged
Write-Output ''
Write-Output '--- Tests 18-21: Staged binary files ---'
Assert-True (Test-Path (Join-Path $stagedMaps '35_27.lotheader')) `
    'Test18: staged 35_27.lotheader exists'

Assert-True (Test-Path (Join-Path $stagedMaps 'streets.xml.bin')) `
    'Test19: staged streets.xml.bin exists'

# Test 20: streets.xml.bin has PZMF marker
$streetsContent = if (Test-Path (Join-Path $stagedMaps 'streets.xml.bin')) {
    [System.IO.File]::ReadAllText((Join-Path $stagedMaps 'streets.xml.bin'))
} else { '' }
Assert-True ($streetsContent -match 'PZMF_MAP7Y_STUB_streets_xml_bin') `
    'Test20: streets.xml.bin contains PZMF_MAP7Y_STUB marker'

# Test 21: worldmap.xml.bin + worldmap-forest.xml.bin exist
Assert-True ((Test-Path (Join-Path $stagedMaps 'worldmap.xml.bin')) -and
             (Test-Path (Join-Path $stagedMaps 'worldmap-forest.xml.bin'))) `
    'Test21: worldmap.xml.bin and worldmap-forest.xml.bin exist'

# Test 22: worldmap.png exists
Assert-True (Test-Path (Join-Path $stagedMaps 'worldmap.png')) `
    'Test22: worldmap.png exists'

# Test 23: spawnpoints.lua contains function SpawnPoints()
Write-Output ''
Write-Output '--- Tests 23-24: Doc content ---'
$spawnContent = if (Test-Path (Join-Path $stagedMaps 'spawnpoints.lua')) {
    Get-Content (Join-Path $stagedMaps 'spawnpoints.lua') -Raw
} else { '' }
Assert-True ($spawnContent -match 'function SpawnPoints') `
    'Test23: spawnpoints.lua contains function SpawnPoints()'

# Test 24: Docs mention no PZ run + no Upload + no third-party + no playable claim
$packetContent = if (Test-Path (Join-Path $packetOut 'MAP_7Y_MINIMAL_SIDECAR_STUB_PACKET.md')) {
    Get-Content (Join-Path $packetOut 'MAP_7Y_MINIMAL_SIDECAR_STUB_PACKET.md') -Raw
} else { '' }
$checklistContent = if (Test-Path (Join-Path $packetOut 'MAP_7Y_HUMAN_WORKSHOP_UPDATE_CHECKLIST.md')) {
    Get-Content (Join-Path $packetOut 'MAP_7Y_HUMAN_WORKSHOP_UPDATE_CHECKLIST.md') -Raw
} else { '' }
$allContent = $packetContent + $checklistContent

$t24cond = (($allContent -match 'NO_PZ_RUN_BY_SCRIPT|No PZ run|does NOT|LOAD_TEST_NOT_PERFORMED') -and
            ($allContent -match 'NO_AUTOMATIC_WORKSHOP_UPLOAD|does NOT upload|no.*upload') -and
            ($allContent -match 'NO_THIRD_PARTY_FILES_COPIED|NOT.*copied.*Dru_map|NOT COPIED FROM DRU_MAP') -and
            ($allContent -match 'PUBLIC_PLAYABLE_CLAIM_ALLOWED=false'))
Assert-True $t24cond 'Test24: docs mention no PZ run, no upload, no third-party, no playable claim'

Write-Output ''
Write-Output '----------------------------------------'
Write-Output "Results: $pass passed, $fail failed"
Write-Output '----------------------------------------'

if ($fail -gt 0) { exit 1 }
exit 0
