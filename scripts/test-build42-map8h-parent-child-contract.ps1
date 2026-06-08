#Requires -Version 5.1
<#
.SYNOPSIS
    Tests for MAP-8H: prepare-build42-map8h-parent-child-contract-packet.ps1.

    Expected assertion count: 20
#>

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$scriptDir    = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot     = Split-Path -Parent $scriptDir
$packetScript = Join-Path $repoRoot 'scripts\prepare-build42-map8h-parent-child-contract-packet.ps1'
$tempRoot     = [System.IO.Path]::GetTempPath()

$pass = 0
$fail = 0

function Assert-True {
    param([bool]$Condition, [string]$Label)
    if ($Condition) { Write-Output "  PASS  $Label"; $script:pass++ }
    else            { Write-Output "  FAIL  $Label"; $script:fail++ }
}

Write-Output 'test-build42-map8h-parent-child-contract.ps1'
Write-Output ''

$testBase = Join-Path $tempRoot ('pzmf-t8h-' + [System.IO.Path]::GetRandomFileName())
$badPath  = Join-Path $tempRoot 'pzmf-t8h-bad-no-local'
New-Item -ItemType Directory -Force -Path $badPath | Out-Null

function Invoke-Packet {
    param([string]$OutDir)
    & powershell -ExecutionPolicy Bypass -File $packetScript -Output $OutDir | Out-Null
    return [int]$LASTEXITCODE
}

# Test 1: Script refuses output outside .local
Write-Output '--- Test 1: Refuses output outside .local ---'
$t1exit = Invoke-Packet -OutDir $badPath
Assert-True ($t1exit -ne 0) 'Test1: output outside .local exits nonzero'

# Run packet
Write-Output ''
Write-Output '--- Running packet (Tests 2-20) ---'
$packetOut = Join-Path $testBase '.local\map8h-packet'
$t2exit    = Invoke-Packet -OutDir $packetOut

# Test 2: Exits 0
Assert-True ($t2exit -eq 0) 'Test2: packet exits 0 with valid output path'

# Tests 3-5: Required packet files
Write-Output ''
Write-Output '--- Tests 3-5: Required packet files ---'
Assert-True (Test-Path (Join-Path $packetOut 'MAP_8H_PARENT_CHILD_CONTRACT_PROBE_PACKET.md')) `
    'Test3: MAP_8H_PARENT_CHILD_CONTRACT_PROBE_PACKET.md exists'

Assert-True (Test-Path (Join-Path $packetOut 'map8h-preflight.json')) `
    'Test4: map8h-preflight.json exists'

Assert-True (Test-Path (Join-Path $packetOut 'map8h-preflight.md')) `
    'Test5: map8h-preflight.md exists'

# Parse preflight JSON
$pfl = if (Test-Path (Join-Path $packetOut 'map8h-preflight.json')) {
    Get-Content (Join-Path $packetOut 'map8h-preflight.json') -Raw | ConvertFrom-Json
} else { $null }

Write-Output ''
Write-Output '--- Tests 6-17: Preflight JSON fields ---'

# Test 6: Schema correct
Assert-True ($null -ne $pfl -and [string]$pfl.schema -eq 'pzmapforge.map8h-preflight.v0.1') `
    "Test6: schema correct (got '$($pfl.schema)')"

# Test 7: parent_map_id = PZMapForge
Assert-True ($null -ne $pfl -and [string]$pfl.parent_map_id -eq 'PZMapForge') `
    "Test7: parent_map_id=PZMapForge (got '$($pfl.parent_map_id)')"

# Test 8: child_map_id correct
Assert-True ($null -ne $pfl -and [string]$pfl.child_map_id -eq 'pzmapforge_build42_candidate_v4_001') `
    'Test8: child_map_id correct'

# Test 9: layout = common_media_maps_parent_child
Assert-True ($null -ne $pfl -and [string]$pfl.layout -eq 'common_media_maps_parent_child') `
    "Test9: layout=common_media_maps_parent_child (got '$($pfl.layout)')"

# Test 10: parent_contains_generated_cell_binaries = true
Assert-True ($null -ne $pfl -and [bool]$pfl.parent_contains_generated_cell_binaries -eq $true) `
    'Test10: parent_contains_generated_cell_binaries=true'

# Test 11: child_contains_spawn_selector_metadata = true
Assert-True ($null -ne $pfl -and [bool]$pfl.child_contains_spawn_selector_metadata -eq $true) `
    'Test11: child_contains_spawn_selector_metadata=true'

# Test 12: no_third_party_files_copied = true
Assert-True ($null -ne $pfl -and [bool]$pfl.no_third_party_files_copied -eq $true) `
    'Test12: no_third_party_files_copied=true'

# Test 13: no_project_russia_files_copied = true
Assert-True ($null -ne $pfl -and [bool]$pfl.no_project_russia_files_copied -eq $true) `
    'Test13: no_project_russia_files_copied=true'

# Test 14: no_pz_run_by_script = true
Assert-True ($null -ne $pfl -and [bool]$pfl.no_pz_run_by_script -eq $true) `
    'Test14: no_pz_run_by_script=true'

# Test 15: binary_writer_gate_closed = true
Assert-True ($null -ne $pfl -and [bool]$pfl.binary_writer_gate_closed -eq $true) `
    'Test15: binary_writer_gate_closed=true'

# Test 16: playable_claim_allowed = false
Assert-True ($null -ne $pfl -and [bool]$pfl.playable_claim_allowed -eq $false) `
    'Test16: playable_claim_allowed=false'

# Test 17: server_map_line contains both map IDs
$sml = if ($null -ne $pfl) { [string]$pfl.server_map_line } else { '' }
Assert-True ($sml -match 'PZMapForge' -and $sml -match 'pzmapforge_build42_candidate_v4_001') `
    "Test17: server_map_line contains both map IDs (got '$sml')"

# Staged package paths
$parentMapsDir = Join-Path $packetOut 'staged-workshop-parent-child\pzmapforge_build42_candidate_v4_001\common\media\maps\PZMapForge'
$childMapsDir  = Join-Path $packetOut 'staged-workshop-parent-child\pzmapforge_build42_candidate_v4_001\common\media\maps\pzmapforge_build42_candidate_v4_001'

Write-Output ''
Write-Output '--- Tests 18-20: Staged package structure ---'

# Test 18: Parent map folder exists with cell binary
Assert-True ((Test-Path $parentMapsDir) -and
             (Test-Path (Join-Path $parentMapsDir '35_27.lotheader'))) `
    'Test18: parent map folder exists with 35_27.lotheader'

# Test 19: Child map folder exists with no cell binaries
$childHasLotheader = Test-Path (Join-Path $childMapsDir '35_27.lotheader')
Assert-True ((Test-Path $childMapsDir) -and -not $childHasLotheader) `
    'Test19: child map folder exists and has no cell binaries'

# Test 20: Child map.info has lots=PZMapForge
$childMapInfoContent = if (Test-Path (Join-Path $childMapsDir 'map.info')) {
    Get-Content (Join-Path $childMapsDir 'map.info') -Raw
} else { '' }
Assert-True ($childMapInfoContent -match 'lots=PZMapForge') `
    'Test20: child map.info has lots=PZMapForge'

Write-Output ''
Write-Output '----------------------------------------'
Write-Output "Results: $pass passed, $fail failed"
Write-Output '----------------------------------------'

if ($fail -gt 0) { exit 1 }
exit 0
