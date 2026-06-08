#Requires -Version 5.1
<#
.SYNOPSIS
    Tests for MAP-7V: prepare-build42-map7v-control-results-packet.ps1.

    Expected assertion count: 20
#>

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$scriptDir    = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot     = Split-Path -Parent $scriptDir
$packetScript = Join-Path $repoRoot 'scripts\prepare-build42-map7v-control-results-packet.ps1'
$tempRoot     = [System.IO.Path]::GetTempPath()

$pass = 0
$fail = 0

function Assert-True {
    param([bool]$Condition, [string]$Label)
    if ($Condition) { Write-Output "  PASS  $Label"; $script:pass++ }
    else            { Write-Output "  FAIL  $Label"; $script:fail++ }
}

Write-Output 'test-build42-map7v-control-results.ps1'
Write-Output ''

$testBase = Join-Path $tempRoot ('pzmf-t7v-' + [System.IO.Path]::GetRandomFileName())
$badPath  = Join-Path $tempRoot 'pzmf-t7v-bad-no-local'
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

# Tests 3-7: Required files exist
Write-Output ''
Write-Output '--- Tests 3-7: Required files ---'
$reqFiles = @(
    'MAP_7V_K004_K006_CONTROL_RESULTS.md',
    'MAP_7V_BINARY_GATE_DECISION.md',
    'MAP_7V_NEXT_RUNTIME_MOUNTING_BRANCH.md',
    'map7v-preflight.json',
    'map7v-preflight.md'
)
foreach ($f in $reqFiles) {
    Assert-True (Test-Path (Join-Path $packetOut $f)) "Test: $f exists"
}

# Parse preflight JSON
$preflightPath = Join-Path $packetOut 'map7v-preflight.json'
$pfl = if (Test-Path $preflightPath) { Get-Content $preflightPath -Raw | ConvertFrom-Json } else { $null }

# Test 8: Schema is correct
Write-Output ''
Write-Output '--- Tests 8-17: Preflight JSON fields ---'
Assert-True ($null -ne $pfl -and [string]$pfl.schema -eq 'pzmapforge.map7v-preflight.v0.1') `
    "Test8: preflight schema correct (got '$($pfl.schema)')"

# Test 9: K004 fields present
Assert-True ($null -ne $pfl -and [bool]$pfl.k004_workshop_ready -eq $true -and
             [bool]$pfl.k004_mod_loaded -eq $true) `
    'Test9: k004_workshop_ready=true and k004_mod_loaded=true'

# Test 10: K004 expected lotheader evidence absent
Assert-True ($null -ne $pfl -and [bool]$pfl.k004_expected_candidate_lotheader_evidence -eq $false) `
    'Test10: k004_expected_candidate_lotheader_evidence=false'

# Test 11: K006 lotheader count=0
Assert-True ($null -ne $pfl -and [int]$pfl.k006_candidate_lotheader_count -eq 0) `
    'Test11: k006_candidate_lotheader_count=0'

# Test 12: K006 lotpack count=0
Assert-True ($null -ne $pfl -and [int]$pfl.k006_candidate_lotpack_count -eq 0) `
    'Test12: k006_candidate_lotpack_count=0'

# Test 13: K006 chunkdata count=0
Assert-True ($null -ne $pfl -and [int]$pfl.k006_candidate_chunkdata_count -eq 0) `
    'Test13: k006_candidate_chunkdata_count=0'

# Test 14: binary_writer_gate_closed=true
Assert-True ($null -ne $pfl -and [bool]$pfl.binary_writer_gate_closed -eq $true) `
    'Test14: binary_writer_gate_closed=true'

# Test 15: binary_format_investigation_paused=true
Assert-True ($null -ne $pfl -and [bool]$pfl.binary_format_investigation_paused -eq $true) `
    'Test15: binary_format_investigation_paused=true'

# Test 16: next_branch=runtime_map_registration_and_mounting
Assert-True ($null -ne $pfl -and [string]$pfl.next_branch -eq 'runtime_map_registration_and_mounting') `
    "Test16: next_branch=runtime_map_registration_and_mounting (got '$($pfl.next_branch)')"

# Test 17: public_playable_claim_allowed=false
Assert-True ($null -ne $pfl -and [bool]$pfl.public_playable_claim_allowed -eq $false) `
    'Test17: public_playable_claim_allowed=false'

# Tests 18-20: Doc content checks
Write-Output ''
Write-Output '--- Tests 18-20: Doc content ---'

$resultsContent = if (Test-Path (Join-Path $packetOut 'MAP_7V_K004_K006_CONTROL_RESULTS.md')) {
    Get-Content (Join-Path $packetOut 'MAP_7V_K004_K006_CONTROL_RESULTS.md') -Raw
} else { '' }

Assert-True ($resultsContent -match 'lotheader count: 0|lotheader_count.*0|count: 0') `
    'Test18: K006 results doc mentions zero candidate binaries'

$gateContent = if (Test-Path (Join-Path $packetOut 'MAP_7V_BINARY_GATE_DECISION.md')) {
    Get-Content (Join-Path $packetOut 'MAP_7V_BINARY_GATE_DECISION.md') -Raw
} else { '' }

Assert-True ($gateContent -match 'SANITY CHECK FAIL|SANITY.*CHECK.*FAIL') `
    'Test19: binary gate doc mentions SANITY CHECK FAIL'

$allContent = $resultsContent + $gateContent + $(if (Test-Path (Join-Path $packetOut 'MAP_7V_NEXT_RUNTIME_MOUNTING_BRANCH.md')) {
    Get-Content (Join-Path $packetOut 'MAP_7V_NEXT_RUNTIME_MOUNTING_BRANCH.md') -Raw
} else { '' })

Assert-True ($allContent -match 'NO_PZ_RUN_BY_SCRIPT|LOAD_TEST_NOT_PERFORMED_BY_SCRIPT|does not upload|does NOT upload|NOT.*run.*PZ|do not run PZ') `
    'Test20: docs mention no PZ run and no Workshop upload by script'

Write-Output ''
Write-Output '----------------------------------------'
Write-Output "Results: $pass passed, $fail failed"
Write-Output '----------------------------------------'

if ($fail -gt 0) { exit 1 }
exit 0
