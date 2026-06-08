#Requires -Version 5.1
<#
.SYNOPSIS
    Tests for MAP-7X: prepare-build42-map7x-actual-contract-result-packet.ps1.

    Expected assertion count: 20
#>

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$scriptDir    = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot     = Split-Path -Parent $scriptDir
$packetScript = Join-Path $repoRoot 'scripts\prepare-build42-map7x-actual-contract-result-packet.ps1'
$tempRoot     = [System.IO.Path]::GetTempPath()

$pass = 0
$fail = 0

function Assert-True {
    param([bool]$Condition, [string]$Label)
    if ($Condition) { Write-Output "  PASS  $Label"; $script:pass++ }
    else            { Write-Output "  FAIL  $Label"; $script:fail++ }
}

Write-Output 'test-build42-map7x-actual-contract-result.ps1'
Write-Output ''

$testBase = Join-Path $tempRoot ('pzmf-t7x-' + [System.IO.Path]::GetRandomFileName())
$badPath  = Join-Path $tempRoot 'pzmf-t7x-bad-no-local'
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
Write-Output '--- Running packet (Tests 2-20) ---'
$packetOut = Join-Path $testBase '.local\packet'
$t2exit    = Invoke-Packet -OutDir $packetOut

# Test 2: Packet exits 0
Assert-True ($t2exit -eq 0) 'Test2: packet exits 0 with valid output path'

# Tests 3-7: Required files
Write-Output ''
Write-Output '--- Tests 3-7: Required files ---'
$reqFiles = @(
    'MAP_7X_ACTUAL_CONTRACT_RESULT.md',
    'MAP_7X_NON_CELL_SIDECAR_DISCRIMINATORS.md',
    'MAP_7X_NEXT_DECISION_TREE.md',
    'map7x-preflight.json',
    'map7x-preflight.md'
)
foreach ($f in $reqFiles) {
    Assert-True (Test-Path (Join-Path $packetOut $f)) "Test: $f exists"
}

# Parse preflight JSON
$pfl = if (Test-Path (Join-Path $packetOut 'map7x-preflight.json')) {
    Get-Content (Join-Path $packetOut 'map7x-preflight.json') -Raw | ConvertFrom-Json
} else { $null }

# Test 8: Schema correct
Write-Output ''
Write-Output '--- Tests 8-16: Preflight JSON fields ---'
Assert-True ($null -ne $pfl -and [string]$pfl.schema -eq 'pzmapforge.map7x-preflight.v0.1') `
    "Test8: schema correct (got '$($pfl.schema)')"

# Test 9: map_bin_discriminator=false
Assert-True ($null -ne $pfl -and [bool]$pfl.map_bin_discriminator -eq $false) `
    'Test9: map_bin_discriminator=false'

# Test 10: reference_has_map_bin=false
Assert-True ($null -ne $pfl -and [bool]$pfl.reference_has_map_bin -eq $false) `
    'Test10: reference_has_map_bin=false'

# Test 11: candidate_has_map_bin=false
Assert-True ($null -ne $pfl -and [bool]$pfl.candidate_has_map_bin -eq $false) `
    'Test11: candidate_has_map_bin=false'

# Test 12: missing_non_cell_sidecars includes streets.xml.bin
$sidecars = if ($null -ne $pfl -and $null -ne $pfl.missing_non_cell_sidecars) {
    @($pfl.missing_non_cell_sidecars)
} else { @() }
Assert-True ($sidecars -contains 'streets.xml.bin') `
    'Test12: missing_non_cell_sidecars contains streets.xml.bin'

# Test 13: missing_non_cell_sidecars includes worldmap.xml.bin
Assert-True ($sidecars -contains 'worldmap.xml.bin') `
    'Test13: missing_non_cell_sidecars contains worldmap.xml.bin'

# Test 14: binary_writer_gate_closed=true
Assert-True ($null -ne $pfl -and [bool]$pfl.binary_writer_gate_closed -eq $true) `
    'Test14: binary_writer_gate_closed=true'

# Test 15: public_playable_claim_allowed=false
Assert-True ($null -ne $pfl -and [bool]$pfl.public_playable_claim_allowed -eq $false) `
    'Test15: public_playable_claim_allowed=false'

# Test 16: third_party_reference_files_copied=false
Assert-True ($null -ne $pfl -and [bool]$pfl.third_party_reference_files_copied -eq $false) `
    'Test16: third_party_reference_files_copied=false'

# Tests 17-20: Doc content
Write-Output ''
Write-Output '--- Tests 17-20: Doc content ---'

$resultContent = if (Test-Path (Join-Path $packetOut 'MAP_7X_ACTUAL_CONTRACT_RESULT.md')) {
    Get-Content (Join-Path $packetOut 'MAP_7X_ACTUAL_CONTRACT_RESULT.md') -Raw
} else { '' }

Assert-True ($resultContent -match 'map\.bin.*ruled out|RULED OUT|map_bin_discriminator.*false') `
    'Test17: result doc mentions map.bin ruled out'

Assert-True ($resultContent -match '12390|12,390|4130.*cell|cell.*4130|dominated by.*cell|cell.*dominated') `
    'Test18: result doc explains large count is dominated by expected cell files'

$sidecarContent = if (Test-Path (Join-Path $packetOut 'MAP_7X_NON_CELL_SIDECAR_DISCRIMINATORS.md')) {
    Get-Content (Join-Path $packetOut 'MAP_7X_NON_CELL_SIDECAR_DISCRIMINATORS.md') -Raw
} else { '' }

Assert-True ($sidecarContent -match 'Do NOT copy|do not copy|NOT.*copy.*Dru_map|copy.*Dru_map.*forbidden') `
    'Test19: sidecar doc mentions do not copy Dru_map files'

$treeContent = if (Test-Path (Join-Path $packetOut 'MAP_7X_NEXT_DECISION_TREE.md')) {
    Get-Content (Join-Path $packetOut 'MAP_7X_NEXT_DECISION_TREE.md') -Raw
} else { '' }
$allContent = $resultContent + $sidecarContent + $treeContent

Assert-True ($allContent -match 'No PZ run|LOAD_TEST_NOT_PERFORMED_BY_SCRIPT|NO_AUTOMATIC_WORKSHOP_UPLOAD|PUBLIC_PLAYABLE_CLAIM_ALLOWED=false') `
    'Test20: docs mention no PZ run / no Workshop upload / no playable claim'

Write-Output ''
Write-Output '----------------------------------------'
Write-Output "Results: $pass passed, $fail failed"
Write-Output '----------------------------------------'

if ($fail -gt 0) { exit 1 }
exit 0
