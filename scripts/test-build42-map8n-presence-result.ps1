[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$pass = 0; $fail = 0
function Assert-True([bool]$cond, [string]$label) {
    if ($cond) { Write-Host "  PASS: $label"; $script:pass++ }
    else        { Write-Host "  FAIL: $label"; $script:fail++ }
}

$scriptPath = Join-Path $PSScriptRoot 'prepare-build42-map8n-presence-result-packet.ps1'
$tmpOut = Join-Path $PSScriptRoot '.local\map8n-result-test-tmp'

Write-Host "MAP-8N Worldmap Bin Presence Result Tests"
Write-Host "========================================="

# Test 1: .local guard exits nonzero
Write-Host "`n[1] .local guard on bad path"
$savedPref = $ErrorActionPreference
$ErrorActionPreference = 'SilentlyContinue'
$null = & powershell -ExecutionPolicy Bypass -NonInteractive -Command `
    "& '$scriptPath' -Output 'C:\tmp\bad-path'" 2>&1
$guardExit = $LASTEXITCODE
$ErrorActionPreference = $savedPref
Assert-True ($guardExit -ne 0) ".local guard exits nonzero for path without .local"

# Test 2: exits 0 with valid path
Write-Host "`n[2] exits 0 with valid path"
if (Test-Path $tmpOut) { Remove-Item -Recurse -Force $tmpOut }
$savedPref2 = $ErrorActionPreference
$ErrorActionPreference = 'SilentlyContinue'
$null = & powershell -ExecutionPolicy Bypass -NonInteractive -Command `
    "& '$scriptPath' -Output '$tmpOut'" 2>&1
$validExit = $LASTEXITCODE
$ErrorActionPreference = $savedPref2
Assert-True ($validExit -eq 0) "exits 0 with valid .local path"

$jsonPath   = Join-Path $tmpOut 'map8n-result.json'
$mdPath     = Join-Path $tmpOut 'map8n-result.md'
$packetPath = Join-Path $tmpOut 'MAP_8N_WORLDMAP_BIN_PRESENCE_RESULT_PACKET.md'

# Tests 3-5: output files exist
Write-Host "`n[3-5] output files exist"
Assert-True (Test-Path $packetPath) "MAP_8N_WORLDMAP_BIN_PRESENCE_RESULT_PACKET.md exists"
Assert-True (Test-Path $jsonPath)   "map8n-result.json exists"
Assert-True (Test-Path $mdPath)     "map8n-result.md exists"

$p = Get-Content $jsonPath -Raw | ConvertFrom-Json

# Test 6: schema
Write-Host "`n[6] schema"
Assert-True ($p.schema -eq 'pzmapforge.map8n-result.v0.1') "schema == pzmapforge.map8n-result.v0.1"

# Test 7: candidate_worldmap_xml_present
Write-Host "`n[7] candidate_worldmap_xml_present"
Assert-True ($p.candidate_worldmap_xml_present -eq $true) "candidate_worldmap_xml_present == true"

# Test 8: candidate_worldmap_xml_bin_present
Write-Host "`n[8] candidate_worldmap_xml_bin_present"
Assert-True ($p.candidate_worldmap_xml_bin_present -eq $false) "candidate_worldmap_xml_bin_present == false"

# Test 9: reference_worldmap_xml_bin_present
Write-Host "`n[9] reference_worldmap_xml_bin_present"
Assert-True ($p.reference_worldmap_xml_bin_present -eq $true) "reference_worldmap_xml_bin_present == true"

# Test 10: reference_worldmap_xml_bin_size_bytes
Write-Host "`n[10] reference_worldmap_xml_bin_size_bytes"
Assert-True ([int64]$p.reference_worldmap_xml_bin_size_bytes -eq 283881) "reference_worldmap_xml_bin_size_bytes == 283881"

# Test 11: candidate_streets_xml_bin_present
Write-Host "`n[11] candidate_streets_xml_bin_present"
Assert-True ($p.candidate_streets_xml_bin_present -eq $false) "candidate_streets_xml_bin_present == false"

# Test 12: reference_streets_xml_bin_present
Write-Host "`n[12] reference_streets_xml_bin_present"
Assert-True ($p.reference_streets_xml_bin_present -eq $false) "reference_streets_xml_bin_present == false"

# Test 13: streets_xml_bin_primary_blocker_likely
Write-Host "`n[13] streets_xml_bin_primary_blocker_likely"
Assert-True ($p.streets_xml_bin_primary_blocker_likely -eq $false) "streets_xml_bin_primary_blocker_likely == false"

# Test 14: worldmap_xml_text_primary_blocker_likely
Write-Host "`n[14] worldmap_xml_text_primary_blocker_likely"
Assert-True ($p.worldmap_xml_text_primary_blocker_likely -eq $false) "worldmap_xml_text_primary_blocker_likely == false"

# Test 15: worldmap_xml_bin_primary_discriminator
Write-Host "`n[15] worldmap_xml_bin_primary_discriminator"
Assert-True ($p.worldmap_xml_bin_primary_discriminator -eq $true) "worldmap_xml_bin_primary_discriminator == true"

# Test 16: lotpack_count_pattern_fixed
Write-Host "`n[16] lotpack_count_pattern_fixed"
Assert-True ($p.lotpack_count_pattern_fixed -eq $true) "lotpack_count_pattern_fixed == true"

# Test 17: binary_contents_read
Write-Host "`n[17] binary_contents_read"
Assert-True ($p.binary_contents_read -eq $false) "binary_contents_read == false"

# Test 18: no_project_russia_files_copied
Write-Host "`n[18] no_project_russia_files_copied"
Assert-True ($p.no_project_russia_files_copied -eq $true) "no_project_russia_files_copied == true"

# Test 19: playable_claim_allowed
Write-Host "`n[19] playable_claim_allowed"
Assert-True ($p.playable_claim_allowed -eq $false) "playable_claim_allowed == false"

# Test 20: next_branch
Write-Host "`n[20] next_branch"
Assert-True ($p.next_branch -like '*worldmap_xml_bin_header_format_investigation*') "next_branch contains worldmap_xml_bin_header_format_investigation"

if (Test-Path $tmpOut) { Remove-Item -Recurse -Force $tmpOut }

Write-Host "`n========================================="
Write-Host "PASS: $pass   FAIL: $fail   TOTAL: $($pass + $fail)"

if ($fail -gt 0) { exit 1 } else { exit 0 }
