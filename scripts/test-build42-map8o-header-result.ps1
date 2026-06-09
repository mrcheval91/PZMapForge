[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$pass = 0; $fail = 0
function Assert-True([bool]$cond, [string]$label) {
    if ($cond) { Write-Host "  PASS: $label"; $script:pass++ }
    else        { Write-Host "  FAIL: $label"; $script:fail++ }
}

$scriptPath = Join-Path $PSScriptRoot 'prepare-build42-map8o-header-result-packet.ps1'
$tmpOut = Join-Path $PSScriptRoot '.local\map8o-result-test-tmp'

Write-Host "MAP-8O Worldmap Bin Header Result Tests"
Write-Host "======================================="

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

$jsonPath   = Join-Path $tmpOut 'map8o-header-result.json'
$mdPath     = Join-Path $tmpOut 'map8o-header-result.md'
$packetPath = Join-Path $tmpOut 'MAP_8O_WORLDMAP_BIN_HEADER_RESULT_PACKET.md'

# Tests 3-5: output files exist
Write-Host "`n[3-5] output files exist"
Assert-True (Test-Path $packetPath) "MAP_8O_WORLDMAP_BIN_HEADER_RESULT_PACKET.md exists"
Assert-True (Test-Path $jsonPath)   "map8o-header-result.json exists"
Assert-True (Test-Path $mdPath)     "map8o-header-result.md exists"

$p = Get-Content $jsonPath -Raw | ConvertFrom-Json

# Test 6: schema
Write-Host "`n[6] schema"
Assert-True ($p.schema -eq 'pzmapforge.map8o-result.v0.1') "schema == pzmapforge.map8o-result.v0.1"

# Test 7: operator_approved_header_only_inspection == true
Write-Host "`n[7] operator_approved_header_only_inspection"
Assert-True ($p.operator_approved_header_only_inspection -eq $true) "operator_approved_header_only_inspection == true"

# Test 8: max_bytes_allowed == 64
Write-Host "`n[8] max_bytes_allowed == 64"
Assert-True ([int]$p.max_bytes_allowed -eq 64) "max_bytes_allowed == 64"

# Test 9: binary_contents_read_scope
Write-Host "`n[9] binary_contents_read_scope"
Assert-True ($p.binary_contents_read_scope -eq 'first_64_bytes_only') "binary_contents_read_scope == first_64_bytes_only"

# Test 10: binary_contents_full_read == false
Write-Host "`n[10] binary_contents_full_read == false"
Assert-True ($p.binary_contents_full_read -eq $false) "binary_contents_full_read == false"

# Test 11: no_project_russia_files_copied == true
Write-Host "`n[11] no_project_russia_files_copied"
Assert-True ($p.no_project_russia_files_copied -eq $true) "no_project_russia_files_copied == true"

# Test 12: playable_claim_allowed == false
Write-Host "`n[12] playable_claim_allowed"
Assert-True ($p.playable_claim_allowed -eq $false) "playable_claim_allowed == false"

# Test 13: binary_writer_gate_closed == true
Write-Host "`n[13] binary_writer_gate_closed"
Assert-True ($p.binary_writer_gate_closed -eq $true) "binary_writer_gate_closed == true"

# Test 14: worldmap_xml_bin_primary_discriminator == true
Write-Host "`n[14] worldmap_xml_bin_primary_discriminator"
Assert-True ($p.worldmap_xml_bin_primary_discriminator -eq $true) "worldmap_xml_bin_primary_discriminator == true"

# Test 15: next_branch contains 'run_header_inspector'
Write-Host "`n[15] next_branch contains run_header_inspector"
Assert-True ($p.next_branch -like '*run_header_inspector*') "next_branch contains run_header_inspector"

# Test 16: next_branch not empty
Write-Host "`n[16] next_branch not empty"
Assert-True (-not [string]::IsNullOrEmpty($p.next_branch)) "next_branch is not empty"

# Tests 17-20: packet doc content
Write-Host "`n[17-20] packet doc content"
$packetContent = Get-Content $packetPath -Raw
Assert-True ($packetContent -match 'MAP8O_WORLDMAP_XML_BIN_HEADER_INSPECTION_DEFINED') "packet contains MAP8O label"
Assert-True ($packetContent -match 'BINARY_WRITER_GATE_STILL_CLOSED') "packet contains BINARY_WRITER_GATE_STILL_CLOSED"
Assert-True ($packetContent -match 'PUBLIC_PLAYABLE_CLAIM_ALLOWED=false') "packet contains PUBLIC_PLAYABLE_CLAIM_ALLOWED=false"
Assert-True ($packetContent -match 'operator_approved') "packet contains operator_approved"

# Cleanup
if (Test-Path $tmpOut) { Remove-Item -Recurse -Force $tmpOut }

Write-Host "`n======================================="
Write-Host "PASS: $pass   FAIL: $fail   TOTAL: $($pass + $fail)"

if ($fail -gt 0) { exit 1 } else { exit 0 }
