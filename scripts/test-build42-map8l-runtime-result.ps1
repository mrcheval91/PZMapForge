[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$pass = 0; $fail = 0
function Assert-True([bool]$cond, [string]$label) {
    if ($cond) { Write-Host "  PASS: $label"; $script:pass++ }
    else        { Write-Host "  FAIL: $label"; $script:fail++ }
}

$scriptPath = Join-Path $PSScriptRoot 'prepare-build42-map8l-runtime-result-packet.ps1'
$tmpOut = Join-Path $PSScriptRoot '.local\map8l-result-test-tmp'

Write-Host "MAP-8L Runtime Result Packet Tests"
Write-Host "==================================="

# Test 1: .local guard exits nonzero when Output lacks .local
Write-Host "`n[1] .local guard on bad path"
$savedPref = $ErrorActionPreference
$ErrorActionPreference = 'SilentlyContinue'
$null = & powershell -ExecutionPolicy Bypass -NonInteractive -Command `
    "& '$scriptPath' -Output 'C:\tmp\bad-path'" 2>&1
$guardExit = $LASTEXITCODE
$ErrorActionPreference = $savedPref
Assert-True ($guardExit -ne 0) ".local guard exits nonzero for path without .local"

# Test 2: exits 0 with valid .local path
Write-Host "`n[2] exits 0 with valid path"
if (Test-Path $tmpOut) { Remove-Item -Recurse -Force $tmpOut }
$savedPref2 = $ErrorActionPreference
$ErrorActionPreference = 'SilentlyContinue'
$null = & powershell -ExecutionPolicy Bypass -NonInteractive -Command `
    "& '$scriptPath' -Output '$tmpOut'" 2>&1
$validExit = $LASTEXITCODE
$ErrorActionPreference = $savedPref2
Assert-True ($validExit -eq 0) "exits 0 with valid .local path"

$jsonPath   = Join-Path $tmpOut 'map8l-result.json'
$mdPath     = Join-Path $tmpOut 'map8l-result.md'
$packetPath = Join-Path $tmpOut 'MAP_8L_WORLDMAP_XML_RUNTIME_RESULT_PACKET.md'

# Tests 3-5: output files exist
Write-Host "`n[3-5] output files exist"
Assert-True (Test-Path $packetPath)  "MAP_8L_WORLDMAP_XML_RUNTIME_RESULT_PACKET.md exists"
Assert-True (Test-Path $jsonPath)    "map8l-result.json exists"
Assert-True (Test-Path $mdPath)      "map8l-result.md exists"

$p = Get-Content $jsonPath -Raw | ConvertFrom-Json

# Test 6: schema
Write-Host "`n[6] schema"
Assert-True ($p.schema -eq 'pzmapforge.map8l-result.v0.1') "schema == pzmapforge.map8l-result.v0.1"

# Test 7: map8l_worldmap_xml_deployed
Write-Host "`n[7] map8l_worldmap_xml_deployed"
Assert-True ($p.map8l_worldmap_xml_deployed -eq $true) "map8l_worldmap_xml_deployed == true"

# Test 8: downloaded_workshop_worldmap_xml_was_map8l
Write-Host "`n[8] downloaded_workshop_worldmap_xml_was_map8l"
Assert-True ($p.downloaded_workshop_worldmap_xml_was_map8l -eq $true) "downloaded_workshop_worldmap_xml_was_map8l == true"

# Test 9: player_spawn_coordinate
Write-Host "`n[9] player_spawn_coordinate"
Assert-True ($p.player_spawn_coordinate -eq '10746,8288,0') "player_spawn_coordinate == 10746,8288,0"

# Test 10: player_disconnect_coordinate
Write-Host "`n[10] player_disconnect_coordinate"
Assert-True ($p.player_disconnect_coordinate -eq '10777,8287,0') "player_disconnect_coordinate == 10777,8287,0"

# Test 11: spawn_coordinate_matches_35_27
Write-Host "`n[11] spawn_coordinate_matches_35_27"
Assert-True ($p.spawn_coordinate_matches_35_27 -eq $true) "spawn_coordinate_matches_35_27 == true"

# Test 12: iso_meta_grid_map_folder_list_empty
Write-Host "`n[12] iso_meta_grid_map_folder_list_empty"
Assert-True ($p.iso_meta_grid_map_folder_list_empty -eq $true) "iso_meta_grid_map_folder_list_empty == true"

# Test 13: parent_folder_listed_by_isometagrid
Write-Host "`n[13] parent_folder_listed_by_isometagrid"
Assert-True ($p.parent_folder_listed_by_isometagrid -eq $false) "parent_folder_listed_by_isometagrid == false"

# Test 14: worldmap_xml_asset_failed_to_load
Write-Host "`n[14] worldmap_xml_asset_failed_to_load"
Assert-True ($p.worldmap_xml_asset_failed_to_load -eq $true) "worldmap_xml_asset_failed_to_load == true"

# Test 15: worldmap_xml_bin_present
Write-Host "`n[15] worldmap_xml_bin_present"
Assert-True ($p.worldmap_xml_bin_present -eq $false) "worldmap_xml_bin_present == false"

# Test 16: invalid_magic_logged
Write-Host "`n[16] invalid_magic_logged"
Assert-True ($p.invalid_magic_logged -eq $false) "invalid_magic_logged == false"

# Test 17: lotheader_parse_attempt_logged
Write-Host "`n[17] lotheader_parse_attempt_logged"
Assert-True ($p.lotheader_parse_attempt_logged -eq $false) "lotheader_parse_attempt_logged == false"

# Test 18: generated_cell_content_mounted
Write-Host "`n[18] generated_cell_content_mounted"
Assert-True ($p.generated_cell_content_mounted -eq $false) "generated_cell_content_mounted == false"

# Test 19: playable_claim_allowed
Write-Host "`n[19] playable_claim_allowed"
Assert-True ($p.playable_claim_allowed -eq $false) "playable_claim_allowed == false"

# Test 20: next_branch contains worldmap_xml_bin_binary_format_investigation
Write-Host "`n[20] next_branch"
Assert-True ($p.next_branch -like '*worldmap_xml_bin_binary_format_investigation*') "next_branch contains worldmap_xml_bin_binary_format_investigation"

if (Test-Path $tmpOut) { Remove-Item -Recurse -Force $tmpOut }

Write-Host "`n==================================="
Write-Host "PASS: $pass   FAIL: $fail   TOTAL: $($pass + $fail)"

if ($fail -gt 0) { exit 1 } else { exit 0 }
