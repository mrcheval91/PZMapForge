#Requires -Version 5.1
<#
.SYNOPSIS
    Tests for MAP-8B: prepare-build42-map8b-runtime-result-packet.ps1.

    Expected assertion count: 20
#>

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$scriptDir    = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot     = Split-Path -Parent $scriptDir
$packetScript = Join-Path $repoRoot 'scripts\prepare-build42-map8b-runtime-result-packet.ps1'
$tempRoot     = [System.IO.Path]::GetTempPath()

$pass = 0
$fail = 0

function Assert-True {
    param([bool]$Condition, [string]$Label)
    if ($Condition) { Write-Output "  PASS  $Label"; $script:pass++ }
    else            { Write-Output "  FAIL  $Label"; $script:fail++ }
}

Write-Output 'test-build42-map8b-runtime-result.ps1'
Write-Output ''

$testBase = Join-Path $tempRoot ('pzmf-t8b-' + [System.IO.Path]::GetRandomFileName())
$badPath  = Join-Path $tempRoot 'pzmf-t8b-bad-no-local'
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
$packetOut = Join-Path $testBase '.local\map8b-packet'
$t2exit    = Invoke-Packet -OutDir $packetOut

# Test 2: Exits 0
Assert-True ($t2exit -eq 0) 'Test2: packet exits 0 with valid output path'

# Tests 3-5: Required files exist
Write-Output ''
Write-Output '--- Tests 3-5: Required files ---'
Assert-True (Test-Path (Join-Path $packetOut 'MAP_8B_VERSION_MEDIA_RUNTIME_RESULT_PACKET.md')) `
    'Test3: MAP_8B_VERSION_MEDIA_RUNTIME_RESULT_PACKET.md exists'

Assert-True (Test-Path (Join-Path $packetOut 'map8b-result-packet.json')) `
    'Test4: map8b-result-packet.json exists'

Assert-True (Test-Path (Join-Path $packetOut 'map8b-result-packet.md')) `
    'Test5: map8b-result-packet.md exists'

# Parse result JSON
$rfl = if (Test-Path (Join-Path $packetOut 'map8b-result-packet.json')) {
    Get-Content (Join-Path $packetOut 'map8b-result-packet.json') -Raw | ConvertFrom-Json
} else { $null }

Write-Output ''
Write-Output '--- Tests 6-17: Result JSON fields ---'

# Test 6: Schema correct
Assert-True ($null -ne $rfl -and [string]$rfl.schema -eq 'pzmapforge.map8b-result-packet.v0.1') `
    "Test6: schema correct (got '$($rfl.schema)')"

# Test 7: workshop_ready=true
Assert-True ($null -ne $rfl -and [bool]$rfl.workshop_ready -eq $true) `
    'Test7: workshop_ready=true'

# Test 8: mod_loaded=true
Assert-True ($null -ne $rfl -and [bool]$rfl.mod_loaded -eq $true) `
    'Test8: mod_loaded=true'

# Test 9: visual_custom_city_selector_visible=true
Assert-True ($null -ne $rfl -and [bool]$rfl.visual_custom_city_selector_visible -eq $true) `
    'Test9: visual_custom_city_selector_visible=true'

# Test 10: player_fully_connected=true
Assert-True ($null -ne $rfl -and [bool]$rfl.player_fully_connected -eq $true) `
    'Test10: player_fully_connected=true'

# Test 11: player_spawn_coordinate correct
Assert-True ($null -ne $rfl -and [string]$rfl.player_spawn_coordinate -eq '10878,10028,0') `
    "Test11: player_spawn_coordinate=10878,10028,0 (got '$($rfl.player_spawn_coordinate)')"

# Test 12: iso_meta_grid_map_folder_list_empty=true
Assert-True ($null -ne $rfl -and [bool]$rfl.iso_meta_grid_map_folder_list_empty -eq $true) `
    'Test12: iso_meta_grid_map_folder_list_empty=true'

# Test 13: version_scoped_media_path_visible_to_worldmap_loader=true
Assert-True ($null -ne $rfl -and
             [bool]$rfl.version_scoped_media_path_visible_to_worldmap_loader -eq $true) `
    'Test13: version_scoped_media_path_visible_to_worldmap_loader=true'

# Test 14: worldmap_bin_invalid_magic=true
Assert-True ($null -ne $rfl -and [bool]$rfl.worldmap_bin_invalid_magic -eq $true) `
    'Test14: worldmap_bin_invalid_magic=true'

# Test 15: playable_claim_allowed=false
Assert-True ($null -ne $rfl -and [bool]$rfl.playable_claim_allowed -eq $false) `
    'Test15: playable_claim_allowed=false'

# Test 16: binary_writer_gate_closed=true
Assert-True ($null -ne $rfl -and [bool]$rfl.binary_writer_gate_closed -eq $true) `
    'Test16: binary_writer_gate_closed=true'

# Test 17: next_branch_candidates has at least 2 entries
$branches = if ($null -ne $rfl -and $null -ne $rfl.next_branch_candidates) {
    @($rfl.next_branch_candidates)
} else { @() }
Assert-True ($branches.Count -ge 2) `
    "Test17: next_branch_candidates count >= 2 (got $($branches.Count))"

# Tests 18-20: Doc content
Write-Output ''
Write-Output '--- Tests 18-20: Doc content ---'

$packetContent = if (Test-Path (Join-Path $packetOut 'MAP_8B_VERSION_MEDIA_RUNTIME_RESULT_PACKET.md')) {
    Get-Content (Join-Path $packetOut 'MAP_8B_VERSION_MEDIA_RUNTIME_RESULT_PACKET.md') -Raw
} else { '' }
$resultMdContent = if (Test-Path (Join-Path $packetOut 'map8b-result-packet.md')) {
    Get-Content (Join-Path $packetOut 'map8b-result-packet.md') -Raw
} else { '' }
$allContent = $packetContent + $resultMdContent

Assert-True ($allContent -match 'MAP8B_PARTIAL_REGISTRATION_BREAKTHROUGH') `
    'Test18: docs contain MAP8B_PARTIAL_REGISTRATION_BREAKTHROUGH'

Assert-True ($allContent -match 'PUBLIC_PLAYABLE_CLAIM_ALLOWED=false') `
    'Test19: docs contain PUBLIC_PLAYABLE_CLAIM_ALLOWED=false'

Assert-True ($allContent -match 'WORLDMAP_BIN_INVALID_MAGIC|worldmap_bin_invalid_magic') `
    'Test20: docs contain WORLDMAP_BIN_INVALID_MAGIC sentinel'

Write-Output ''
Write-Output '----------------------------------------'
Write-Output "Results: $pass passed, $fail failed"
Write-Output '----------------------------------------'

if ($fail -gt 0) { exit 1 }
exit 0
