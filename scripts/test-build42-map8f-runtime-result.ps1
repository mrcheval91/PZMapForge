#Requires -Version 5.1
<#
.SYNOPSIS
    Tests for MAP-8F: prepare-build42-map8f-runtime-result-packet.ps1.

    Expected assertion count: 20
#>

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$scriptDir    = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot     = Split-Path -Parent $scriptDir
$packetScript = Join-Path $repoRoot 'scripts\prepare-build42-map8f-runtime-result-packet.ps1'
$tempRoot     = [System.IO.Path]::GetTempPath()

$pass = 0
$fail = 0

function Assert-True {
    param([bool]$Condition, [string]$Label)
    if ($Condition) { Write-Output "  PASS  $Label"; $script:pass++ }
    else            { Write-Output "  FAIL  $Label"; $script:fail++ }
}

Write-Output 'test-build42-map8f-runtime-result.ps1'
Write-Output ''

$testBase = Join-Path $tempRoot ('pzmf-t8f-' + [System.IO.Path]::GetRandomFileName())
$badPath  = Join-Path $tempRoot 'pzmf-t8f-bad-no-local'
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
$packetOut = Join-Path $testBase '.local\map8f-packet'
$t2exit    = Invoke-Packet -OutDir $packetOut

# Test 2: Exits 0
Assert-True ($t2exit -eq 0) 'Test2: packet exits 0 with valid output path'

# Tests 3-5: Required files exist
Write-Output ''
Write-Output '--- Tests 3-5: Required files ---'
Assert-True (Test-Path (Join-Path $packetOut 'MAP_8F_LOTS_SELF_RUNTIME_RESULT_PACKET.md')) `
    'Test3: MAP_8F_LOTS_SELF_RUNTIME_RESULT_PACKET.md exists'

Assert-True (Test-Path (Join-Path $packetOut 'map8f-result-packet.json')) `
    'Test4: map8f-result-packet.json exists'

Assert-True (Test-Path (Join-Path $packetOut 'map8f-result-packet.md')) `
    'Test5: map8f-result-packet.md exists'

# Parse result JSON
$rfl = if (Test-Path (Join-Path $packetOut 'map8f-result-packet.json')) {
    Get-Content (Join-Path $packetOut 'map8f-result-packet.json') -Raw | ConvertFrom-Json
} else { $null }

Write-Output ''
Write-Output '--- Tests 6-18: Result JSON fields ---'

# Test 6: Schema correct
Assert-True ($null -ne $rfl -and [string]$rfl.schema -eq 'pzmapforge.map8f-result-packet.v0.1') `
    "Test6: schema correct (got '$($rfl.schema)')"

# Test 7: workshop_ready=true
Assert-True ($null -ne $rfl -and [bool]$rfl.workshop_ready -eq $true) `
    'Test7: workshop_ready=true'

# Test 8: lots_self=true
Assert-True ($null -ne $rfl -and [bool]$rfl.lots_self -eq $true) `
    'Test8: lots_self=true'

# Test 9: city_selector_visible=true
Assert-True ($null -ne $rfl -and [bool]$rfl.city_selector_visible -eq $true) `
    'Test9: city_selector_visible=true'

# Test 10: invalid_bin_stubs_absent=true
Assert-True ($null -ne $rfl -and [bool]$rfl.invalid_bin_stubs_absent -eq $true) `
    'Test10: invalid_bin_stubs_absent=true'

# Test 11: worldmap_xml_failed_to_load=true
Assert-True ($null -ne $rfl -and [bool]$rfl.worldmap_xml_failed_to_load -eq $true) `
    'Test11: worldmap_xml_failed_to_load=true'

# Test 12: player_fully_connected=true
Assert-True ($null -ne $rfl -and [bool]$rfl.player_fully_connected -eq $true) `
    'Test12: player_fully_connected=true'

# Test 13: player_spawn_coordinate correct
Assert-True ($null -ne $rfl -and [string]$rfl.player_spawn_coordinate -eq '10851,9846,0') `
    "Test13: player_spawn_coordinate=10851,9846,0 (got '$($rfl.player_spawn_coordinate)')"

# Test 14: spawned_in_muldraugh_or_fallback=true
Assert-True ($null -ne $rfl -and [bool]$rfl.spawned_in_muldraugh_or_fallback -eq $true) `
    'Test14: spawned_in_muldraugh_or_fallback=true'

# Test 15: iso_meta_grid_map_folder_list_empty=true
Assert-True ($null -ne $rfl -and [bool]$rfl.iso_meta_grid_map_folder_list_empty -eq $true) `
    'Test15: iso_meta_grid_map_folder_list_empty=true'

# Test 16: playable_claim_allowed=false
Assert-True ($null -ne $rfl -and [bool]$rfl.playable_claim_allowed -eq $false) `
    'Test16: playable_claim_allowed=false'

# Test 17: binary_writer_gate_closed=true
Assert-True ($null -ne $rfl -and [bool]$rfl.binary_writer_gate_closed -eq $true) `
    'Test17: binary_writer_gate_closed=true'

# Test 18: next_branch correct
Assert-True ($null -ne $rfl -and [string]$rfl.next_branch -eq 'known_working_build42_map_contract_comparator') `
    "Test18: next_branch=known_working_build42_map_contract_comparator (got '$($rfl.next_branch)')"

# Tests 19-20: Doc content
Write-Output ''
Write-Output '--- Tests 19-20: Doc content ---'

$packetContent = if (Test-Path (Join-Path $packetOut 'MAP_8F_LOTS_SELF_RUNTIME_RESULT_PACKET.md')) {
    Get-Content (Join-Path $packetOut 'MAP_8F_LOTS_SELF_RUNTIME_RESULT_PACKET.md') -Raw
} else { '' }

Assert-True ($packetContent -match 'MAP8F_LOTS_SELF_VISIBLE_BUT_NOT_MOUNTED') `
    'Test19: packet doc contains MAP8F_LOTS_SELF_VISIBLE_BUT_NOT_MOUNTED'

Assert-True ($packetContent -match 'PUBLIC_PLAYABLE_CLAIM_ALLOWED=false') `
    'Test20: packet doc contains PUBLIC_PLAYABLE_CLAIM_ALLOWED=false'

Write-Output ''
Write-Output '----------------------------------------'
Write-Output "Results: $pass passed, $fail failed"
Write-Output '----------------------------------------'

if ($fail -gt 0) { exit 1 }
exit 0
