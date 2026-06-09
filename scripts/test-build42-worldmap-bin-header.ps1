[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$pass = 0; $fail = 0
function Assert-True([bool]$cond, [string]$label) {
    if ($cond) { Write-Host "  PASS: $label"; $script:pass++ }
    else        { Write-Host "  FAIL: $label"; $script:fail++ }
}

$scriptPath = Join-Path $PSScriptRoot 'inspect-build42-worldmap-bin-header.ps1'
$tmpOut     = Join-Path $PSScriptRoot '.local\map8o-header-test-tmp'
$tmpLocalDir = Join-Path $PSScriptRoot '.local'
$tmpRef     = Join-Path $tmpLocalDir 'map8o-dummy-ref.bin'

Write-Host "MAP-8O Worldmap Bin Header Inspector Tests"
Write-Host "=========================================="

# Test 1: .local guard exits nonzero
Write-Host "`n[1] .local guard on bad path"
$savedPref = $ErrorActionPreference
$ErrorActionPreference = 'SilentlyContinue'
$null = & powershell -ExecutionPolicy Bypass -NonInteractive -Command `
    "& '$scriptPath' -CandidateWorldmapBinPath 'C:\nonexistent.bin' -ReferenceWorldmapBinPath 'C:\nonexistent.bin' -Output 'C:\tmp\bad-path'" 2>&1
$guardExit = $LASTEXITCODE
$ErrorActionPreference = $savedPref
Assert-True ($guardExit -ne 0) ".local guard exits nonzero for path without .local"

# Create dummy reference: 128 bytes, gzip signature 1F 8B at bytes 0-1
if (-not (Test-Path $tmpLocalDir)) { New-Item -ItemType Directory -Force -Path $tmpLocalDir | Out-Null }
$dummyBytes = [byte[]]::new(128)
$dummyBytes[0] = 0x1F
$dummyBytes[1] = 0x8B
for ($i = 2; $i -lt 128; $i++) { $dummyBytes[$i] = [byte]($i -band 0xFF) }
[System.IO.File]::WriteAllBytes($tmpRef, $dummyBytes)

# Test 2: exits 0 with valid path, absent candidate, present reference
Write-Host "`n[2] exits 0 with valid path"
if (Test-Path $tmpOut) { Remove-Item -Recurse -Force $tmpOut }
$savedPref2 = $ErrorActionPreference
$ErrorActionPreference = 'SilentlyContinue'
$null = & powershell -ExecutionPolicy Bypass -NonInteractive -Command `
    "& '$scriptPath' -CandidateWorldmapBinPath 'C:\nonexistent_candidate_map8o.bin' -ReferenceWorldmapBinPath '$tmpRef' -Output '$tmpOut'" 2>&1
$validExit = $LASTEXITCODE
$ErrorActionPreference = $savedPref2
Assert-True ($validExit -eq 0) "exits 0 with valid .local path"

$jsonPath = Join-Path $tmpOut 'worldmap-bin-header-inspection.json'
$mdPath   = Join-Path $tmpOut 'worldmap-bin-header-inspection.md'

# Tests 3-4: output files exist
Write-Host "`n[3-4] output files exist"
Assert-True (Test-Path $jsonPath) "worldmap-bin-header-inspection.json exists"
Assert-True (Test-Path $mdPath)   "worldmap-bin-header-inspection.md exists"

$p = Get-Content $jsonPath -Raw | ConvertFrom-Json

# Test 5: schema
Write-Host "`n[5] schema"
Assert-True ($p.schema -eq 'pzmapforge.map8o-worldmap-bin-header-inspection.v0.1') "schema == pzmapforge.map8o-worldmap-bin-header-inspection.v0.1"

# Test 6: candidate_present == false (absent file)
Write-Host "`n[6] candidate_present == false"
Assert-True ($p.candidate_present -eq $false) "candidate_present == false"

# Test 7: candidate_bytes_read_count == 0
Write-Host "`n[7] candidate_bytes_read_count == 0"
Assert-True ([int]$p.candidate_bytes_read_count -eq 0) "candidate_bytes_read_count == 0"

# Test 8: reference_present == true
Write-Host "`n[8] reference_present == true"
Assert-True ($p.reference_present -eq $true) "reference_present == true"

# Test 9: reference_size_bytes == 128
Write-Host "`n[9] reference_size_bytes == 128"
Assert-True ([int64]$p.reference_size_bytes -eq 128) "reference_size_bytes == 128"

# Test 10: reference_bytes_read_count == 64 (only 64 of 128 bytes read)
Write-Host "`n[10] reference_bytes_read_count == 64"
Assert-True ([int]$p.reference_bytes_read_count -eq 64) "reference_bytes_read_count == 64 (not full 128 bytes)"

# Test 11: reference_first_16_bytes_hex not empty
Write-Host "`n[11] reference_first_16_bytes_hex not empty"
Assert-True (-not [string]::IsNullOrEmpty($p.reference_first_16_bytes_hex)) "reference_first_16_bytes_hex is not empty"

# Test 12: reference_first_64_bytes_hex not empty
Write-Host "`n[12] reference_first_64_bytes_hex not empty"
Assert-True (-not [string]::IsNullOrEmpty($p.reference_first_64_bytes_hex)) "reference_first_64_bytes_hex is not empty"

# Test 13: reference_detected_signature == 'gzip' (dummy starts with 1F 8B)
Write-Host "`n[13] reference_detected_signature == gzip"
Assert-True ($p.reference_detected_signature -eq 'gzip') "reference_detected_signature == gzip (1F 8B signature)"

# Test 14: reference_ascii_preview is a string
Write-Host "`n[14] reference_ascii_preview is a string"
Assert-True ($p.reference_ascii_preview -is [string]) "reference_ascii_preview is a string"

# Test 15: max_bytes_allowed == 64
Write-Host "`n[15] max_bytes_allowed == 64"
Assert-True ([int]$p.max_bytes_allowed -eq 64) "max_bytes_allowed == 64"

# Test 16: binary_contents_read_scope == 'first_64_bytes_only'
Write-Host "`n[16] binary_contents_read_scope"
Assert-True ($p.binary_contents_read_scope -eq 'first_64_bytes_only') "binary_contents_read_scope == first_64_bytes_only"

# Test 17: binary_contents_full_read == false
Write-Host "`n[17] binary_contents_full_read == false"
Assert-True ($p.binary_contents_full_read -eq $false) "binary_contents_full_read == false"

# Test 18: third_party_files_copied == false
Write-Host "`n[18] third_party_files_copied == false"
Assert-True ($p.third_party_files_copied -eq $false) "third_party_files_copied == false"

# Test 19: playable_claim_allowed == false
Write-Host "`n[19] playable_claim_allowed == false"
Assert-True ($p.playable_claim_allowed -eq $false) "playable_claim_allowed == false"

# Test 20: binary_writer_gate_closed == true
Write-Host "`n[20] binary_writer_gate_closed == true"
Assert-True ($p.binary_writer_gate_closed -eq $true) "binary_writer_gate_closed == true"

# Cleanup
if (Test-Path $tmpOut) { Remove-Item -Recurse -Force $tmpOut }
if (Test-Path $tmpRef) { Remove-Item -Force $tmpRef }

Write-Host "`n=========================================="
Write-Host "PASS: $pass   FAIL: $fail   TOTAL: $($pass + $fail)"

if ($fail -gt 0) { exit 1 } else { exit 0 }
