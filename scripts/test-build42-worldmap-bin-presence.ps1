[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$pass = 0; $fail = 0
function Assert-True([bool]$cond, [string]$label) {
    if ($cond) { Write-Host "  PASS: $label"; $script:pass++ }
    else        { Write-Host "  FAIL: $label"; $script:fail++ }
}

$scriptPath = Join-Path $PSScriptRoot 'inspect-build42-worldmap-bin-presence.ps1'
$tmpOut   = Join-Path $PSScriptRoot '.local\map8m-presence-test-tmp'
$tmpCand  = Join-Path $PSScriptRoot '.local\map8m-tmp-candidate'
$tmpRef   = Join-Path $PSScriptRoot '.local\map8m-tmp-reference'

Write-Host "MAP-8M Worldmap Bin Presence Tests"
Write-Host "==================================="

# Test 1: .local guard exits nonzero on bad path
Write-Host "`n[1] .local guard on bad path"
$savedPref = $ErrorActionPreference
$ErrorActionPreference = 'SilentlyContinue'
$null = & powershell -ExecutionPolicy Bypass -NonInteractive -Command `
    "& '$scriptPath' -CandidateParentRoot 'x:\dummy' -ReferenceParentRoot 'x:\dummy' -Output 'C:\tmp\bad'" 2>&1
$guardExit = $LASTEXITCODE
$ErrorActionPreference = $savedPref
Assert-True ($guardExit -ne 0) ".local guard exits nonzero for path without .local"

# Set up temp dirs
foreach ($d in @($tmpOut, $tmpCand, $tmpRef)) {
    if (Test-Path $d) { Remove-Item -Recurse -Force $d }
    New-Item -ItemType Directory -Force -Path $d | Out-Null
}

# candidate: worldmap.xml present, no .bin, one dummy .lotpack
Set-Content -Encoding UTF8 -Path (Join-Path $tmpCand 'worldmap.xml') -Value '<worldmap/>'
[System.IO.File]::WriteAllBytes((Join-Path $tmpCand 'world_35_27.lotpack'), [byte[]]@(0x00, 0x01))
# reference: worldmap.xml + worldmap.xml.bin (dummy)
Set-Content -Encoding UTF8 -Path (Join-Path $tmpRef 'worldmap.xml')     -Value '<worldmap/>'
[System.IO.File]::WriteAllBytes((Join-Path $tmpRef 'worldmap.xml.bin'), [byte[]]@(0xAB, 0xCD, 0xEF))

# Test 2: exits 0 with valid temp dirs
Write-Host "`n[2] exits 0 with valid paths"
$savedPref2 = $ErrorActionPreference
$ErrorActionPreference = 'SilentlyContinue'
$null = & powershell -ExecutionPolicy Bypass -NonInteractive -Command `
    "& '$scriptPath' -CandidateParentRoot '$tmpCand' -ReferenceParentRoot '$tmpRef' -Output '$tmpOut' -CandidateParentMapId 'PZMapForge' -ReferenceParentMapId 'Project_Russia'" 2>&1
$validExit = $LASTEXITCODE
$ErrorActionPreference = $savedPref2
Assert-True ($validExit -eq 0) "exits 0 with valid .local paths"

$jsonPath = Join-Path $tmpOut 'worldmap-bin-presence.json'
$mdPath   = Join-Path $tmpOut 'worldmap-bin-presence.md'

# Tests 3-4: output files exist
Write-Host "`n[3-4] output files exist"
Assert-True (Test-Path $jsonPath) "worldmap-bin-presence.json exists"
Assert-True (Test-Path $mdPath)   "worldmap-bin-presence.md exists"

$p = Get-Content $jsonPath -Raw | ConvertFrom-Json

# Test 5: schema
Write-Host "`n[5] schema"
Assert-True ($p.schema -eq 'pzmapforge.map8m-worldmap-bin-presence.v0.1') "schema == pzmapforge.map8m-worldmap-bin-presence.v0.1"

# Test 6: candidate_parent_map_id
Write-Host "`n[6] candidate_parent_map_id"
Assert-True ($p.candidate_parent_map_id -eq 'PZMapForge') "candidate_parent_map_id == PZMapForge"

# Test 7: reference_parent_map_id
Write-Host "`n[7] reference_parent_map_id"
Assert-True ($p.reference_parent_map_id -eq 'Project_Russia') "reference_parent_map_id == Project_Russia"

# Test 8: candidate_worldmap_xml_bin_present == false (no .bin in tmp candidate)
Write-Host "`n[8] candidate_worldmap_xml_bin_present"
Assert-True ($p.candidate_worldmap_xml_bin_present -eq $false) "candidate_worldmap_xml_bin_present == false"

# Test 9: reference_worldmap_xml_bin_present == true (dummy .bin in tmp reference)
Write-Host "`n[9] reference_worldmap_xml_bin_present"
Assert-True ($p.reference_worldmap_xml_bin_present -eq $true) "reference_worldmap_xml_bin_present == true"

# Test 10: binary_contents_read == false
Write-Host "`n[10] binary_contents_read"
Assert-True ($p.binary_contents_read -eq $false) "binary_contents_read == false"

# Test 11: no_project_russia_files_copied == true
Write-Host "`n[11] no_project_russia_files_copied"
Assert-True ($p.no_project_russia_files_copied -eq $true) "no_project_russia_files_copied == true"

# Test 12: playable_claim_allowed == false
Write-Host "`n[12] playable_claim_allowed"
Assert-True ($p.playable_claim_allowed -eq $false) "playable_claim_allowed == false"

# Test 13: binary_writer_gate_closed == true
Write-Host "`n[13] binary_writer_gate_closed"
Assert-True ($p.binary_writer_gate_closed -eq $true) "binary_writer_gate_closed == true"

# Test 14: candidate_worldmap_xml_present == true
Write-Host "`n[14] candidate_worldmap_xml_present"
Assert-True ($p.candidate_worldmap_xml_present -eq $true) "candidate_worldmap_xml_present == true"

# Test 15: reference_worldmap_xml_present == true
Write-Host "`n[15] reference_worldmap_xml_present"
Assert-True ($p.reference_worldmap_xml_present -eq $true) "reference_worldmap_xml_present == true"

# Test 16: candidate lotpack_count == 1 (dummy world_35_27.lotpack in temp candidate)
Write-Host "`n[16] candidate lotpack_count"
Assert-True ([int]$p.candidate.lotpack_count -eq 1) "candidate.lotpack_count == 1 (*.lotpack pattern fixed)"

foreach ($d in @($tmpOut, $tmpCand, $tmpRef)) {
    if (Test-Path $d) { Remove-Item -Recurse -Force $d }
}

Write-Host "`n==================================="
Write-Host "PASS: $pass   FAIL: $fail   TOTAL: $($pass + $fail)"

if ($fail -gt 0) { exit 1 } else { exit 0 }
