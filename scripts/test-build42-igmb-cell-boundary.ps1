[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$pass = 0; $fail = 0
function Assert-True([bool]$cond, [string]$label) {
    if ($cond) { Write-Host "  PASS: $label"; $script:pass++ }
    else        { Write-Host "  FAIL: $label"; $script:fail++ }
}

$scriptPath = Join-Path $PSScriptRoot 'inspect-build42-igmb-cell-boundary.ps1'
$tmpOut     = Join-Path $PSScriptRoot '.local\map8s-cell-boundary-test-tmp'
$tmpBin     = Join-Path $PSScriptRoot '.local\map8s-cell-boundary-test-synthetic.bin'

Write-Host "MAP-8S IGMB Cell Boundary Inspector Tests"
Write-Host "=========================================="

# Build synthetic 300-byte IGMB file
# Header: IGMB + version 2 + unknown_a/b/c + string_pool_count=2 (bytes 0-23)
# String 1: len=7 "Polygon" at bytes 24-32
# String 2: len=7 "highway" at bytes 33-41
# Post-pool starts at 42:
#   U32LE=3 at 42, U32LE=200 at 46, 8 zero bytes at 50-57, U32LE=5 at 58
#   Fill 0x41 at 62-299
$synth = [byte[]]::new(300)
# IGMB magic
$synth[0] = 0x49; $synth[1] = 0x47; $synth[2] = 0x4D; $synth[3] = 0x42
# version LE U32 = 2
$synth[4] = 0x02
# unknown_a = 256
$synth[9] = 0x01
# unknown_b = 59
$synth[12] = 0x3B
# unknown_c = 68
$synth[16] = 0x44
# string_pool_count = 2
$synth[20] = 0x02
# LP string 1: len=7, "Polygon"
$synth[24] = 0x07; $synth[25] = 0x00
$synth[26] = 0x50; $synth[27] = 0x6F; $synth[28] = 0x6C; $synth[29] = 0x79
$synth[30] = 0x67; $synth[31] = 0x6F; $synth[32] = 0x6E
# LP string 2: len=7, "highway"
$synth[33] = 0x07; $synth[34] = 0x00
$synth[35] = 0x68; $synth[36] = 0x69; $synth[37] = 0x67; $synth[38] = 0x68
$synth[39] = 0x77; $synth[40] = 0x61; $synth[41] = 0x79
# Post-pool: U32LE=3 at offset 42
$synth[42] = 0x03
# U32LE=200 at offset 46
$synth[46] = 0xC8
# zeros at 50-57 already zero by default
# U32LE=5 at offset 58
$synth[58] = 0x05
# fill 62-299 with 0x41
for ($i = 62; $i -lt 300; $i++) { $synth[$i] = 0x41 }

$tmpBinDir = Split-Path $tmpBin -Parent
if (-not (Test-Path $tmpBinDir)) { New-Item -ItemType Directory -Force $tmpBinDir | Out-Null }
[System.IO.File]::WriteAllBytes($tmpBin, $synth)

# Test 1: .local guard exits nonzero
Write-Host "`n[1] .local guard on bad path"
$savedPref = $ErrorActionPreference
$ErrorActionPreference = 'SilentlyContinue'
$null = & powershell -ExecutionPolicy Bypass -NonInteractive -Command `
    "& '$scriptPath' -ReferenceWorldmapBinPath '$tmpBin' -Output 'C:\tmp\bad-path'" 2>&1
$guardExit = $LASTEXITCODE
$ErrorActionPreference = $savedPref
Assert-True ($guardExit -ne 0) ".local guard exits nonzero for path without .local"

# Test 2: exits 0 with valid .local path and -StringPoolEndOffset 42
Write-Host "`n[2] exits 0 with valid .local path"
if (Test-Path $tmpOut) { Remove-Item -Recurse -Force $tmpOut }
$savedPref2 = $ErrorActionPreference
$ErrorActionPreference = 'SilentlyContinue'
$null = & powershell -ExecutionPolicy Bypass -NonInteractive -Command `
    "& '$scriptPath' -ReferenceWorldmapBinPath '$tmpBin' -Output '$tmpOut' -StringPoolEndOffset 42" 2>&1
$validExit = $LASTEXITCODE
$ErrorActionPreference = $savedPref2
Assert-True ($validExit -eq 0) "exits 0 with valid .local path"

$jsonPath = Join-Path $tmpOut 'igmb-cell-boundary-inspection.json'
$mdPath   = Join-Path $tmpOut 'igmb-cell-boundary-inspection.md'

# Test 3: JSON file present
Write-Host "`n[3] JSON file present"
Assert-True (Test-Path $jsonPath) "igmb-cell-boundary-inspection.json exists"

# Test 4: MD file present
Write-Host "`n[4] MD file present"
Assert-True (Test-Path $mdPath) "igmb-cell-boundary-inspection.md exists"

$p = Get-Content $jsonPath -Raw | ConvertFrom-Json

# Test 5: schema
Write-Host "`n[5] schema"
Assert-True ($p.schema -eq 'pzmapforge.map8s-igmb-cell-boundary-inspection.v0.1') `
    "schema == pzmapforge.map8s-igmb-cell-boundary-inspection.v0.1"

# Test 6: reference_present == true
Write-Host "`n[6] reference_present"
Assert-True ($p.reference_present -eq $true) "reference_present == true"

# Test 7: bytes_read_count > 0
Write-Host "`n[7] bytes_read_count > 0"
Assert-True ([int]$p.bytes_read_count -gt 0) "bytes_read_count > 0"

# Test 8: full_file_read == false
Write-Host "`n[8] full_file_read == false"
Assert-True ($p.full_file_read -eq $false) "full_file_read == false"

# Test 9: max_bytes_allowed == 4096
Write-Host "`n[9] max_bytes_allowed == 4096"
Assert-True ([int]$p.max_bytes_allowed -eq 4096) "max_bytes_allowed == 4096"

# Test 10: string_pool_end_offset == 42
Write-Host "`n[10] string_pool_end_offset == 42"
Assert-True ([int]$p.string_pool_end_offset -eq 42) "string_pool_end_offset == 42"

# Test 11: post_string_pool_window_start == 42
Write-Host "`n[11] post_string_pool_window_start == 42"
Assert-True ([int]$p.post_string_pool_window_start -eq 42) "post_string_pool_window_start == 42"

# Test 12: post_string_pool_window_bytes_available > 0
Write-Host "`n[12] post_string_pool_window_bytes_available > 0"
Assert-True ([int]$p.post_string_pool_window_bytes_available -gt 0) "post_string_pool_window_bytes_available > 0"

# Test 13: u32le_values_after_string_pool_first_128 has entries
Write-Host "`n[13] u32le_values_after_string_pool_first_128 not empty"
$u32arr = @($p.u32le_values_after_string_pool_first_128)
Assert-True ($u32arr.Count -gt 0) "u32le_values_after_string_pool_first_128.Count > 0"

# Test 14: u32le_aligned_boundary_hypothesis has entries
# postStart=42, nextAligned=44, so this should be populated
Write-Host "`n[14] u32le_aligned_boundary_hypothesis not empty"
$u32algn = @($p.u32le_aligned_boundary_hypothesis)
Assert-True ($u32algn.Count -gt 0) "u32le_aligned_boundary_hypothesis.Count > 0 (nextAligned=44 ne 42)"

# Test 15: u16le_values_after_string_pool_first_128 has entries
Write-Host "`n[15] u16le_values_after_string_pool_first_128 not empty"
$u16arr = @($p.u16le_values_after_string_pool_first_128)
Assert-True ($u16arr.Count -gt 0) "u16le_values_after_string_pool_first_128.Count > 0"

# Test 16: float32le_values_after_string_pool_first_128 has entries
Write-Host "`n[16] float32le_values_after_string_pool_first_128 not empty"
$f32arr = @($p.float32le_values_after_string_pool_first_128)
Assert-True ($f32arr.Count -gt 0) "float32le_values_after_string_pool_first_128.Count > 0"

# Test 17: zero_run_candidates not empty (bytes 50-57 are zeros in synthetic file)
Write-Host "`n[17] zero_run_candidates not empty"
$zeroRuns = @($p.zero_run_candidates)
Assert-True ($zeroRuns.Count -gt 0) "zero_run_candidates.Count > 0 (8 zero bytes at 50-57)"

# Test 18: confidence_level == 'low'
Write-Host "`n[18] confidence_level == low"
Assert-True ($p.confidence_level -eq 'low') "confidence_level == low"

# Test 19: binary_writer_gate_closed == true
Write-Host "`n[19] binary_writer_gate_closed == true"
Assert-True ($p.binary_writer_gate_closed -eq $true) "binary_writer_gate_closed == true"

# Test 20: playable_claim_allowed == false
Write-Host "`n[20] playable_claim_allowed == false"
Assert-True ($p.playable_claim_allowed -eq $false) "playable_claim_allowed == false"

# Cleanup
if (Test-Path $tmpOut)  { Remove-Item -Recurse -Force $tmpOut }
if (Test-Path $tmpBin)  { Remove-Item -Force $tmpBin }

Write-Host "`n=========================================="
Write-Host "PASS: $pass   FAIL: $fail   TOTAL: $($pass + $fail)"

if ($fail -gt 0) { exit 1 } else { exit 0 }
