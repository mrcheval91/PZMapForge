Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$pass  = 0
$fail  = 0
$total = 0

function Assert-True {
    param([bool]$Condition, [string]$Label)
    $script:total++
    if ($Condition) {
        Write-Host "  PASS  $Label"
        $script:pass++
    } else {
        Write-Host "  FAIL  $Label"
        $script:fail++
    }
}

$scriptsDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$inspector  = Join-Path $scriptsDir 'inspect-build42-igmb-first-non-ff-transition.ps1'
$outDir     = Join-Path $scriptsDir '.local\test-map8u-inspector-output'
if (-not (Test-Path $outDir)) { New-Item -ItemType Directory -Force -Path $outDir | Out-Null }

$syntheticDir = Join-Path $outDir 'synthetic-files'
if (-not (Test-Path $syntheticDir)) { New-Item -ItemType Directory -Force -Path $syntheticDir | Out-Null }

# Synthetic file A: 300 bytes, StringPoolEndOffset=42, 59 FF bytes then 0x42 at byte 101
$fileA = Join-Path $syntheticDir 'synthetic-a.bin'
$bytesA = [byte[]]::new(300)
$bytesA[0] = 0x49; $bytesA[1] = 0x47; $bytesA[2] = 0x4D; $bytesA[3] = 0x42  # IGMB
$bytesA[4] = 0x02; $bytesA[5] = 0x00; $bytesA[6] = 0x00; $bytesA[7] = 0x00  # version 2
# bytes 8-19: 0x00 (already zeroed)
$bytesA[20] = 0x02; $bytesA[21] = 0x00; $bytesA[22] = 0x00; $bytesA[23] = 0x00  # string_pool_count=2
# LP "Polygon" at 24: len=7 (U16LE), then "Polygon" (7 bytes) = 9 total
$bytesA[24] = 0x07; $bytesA[25] = 0x00
$bytesA[26] = 0x50; $bytesA[27] = 0x6F; $bytesA[28] = 0x6C; $bytesA[29] = 0x79
$bytesA[30] = 0x67; $bytesA[31] = 0x6F; $bytesA[32] = 0x6E
# LP "highway" at 33: len=7 (U16LE), then "highway" (7 bytes) = 9 total
$bytesA[33] = 0x07; $bytesA[34] = 0x00
$bytesA[35] = 0x68; $bytesA[36] = 0x69; $bytesA[37] = 0x67; $bytesA[38] = 0x68
$bytesA[39] = 0x77; $bytesA[40] = 0x61; $bytesA[41] = 0x79
# bytes 42-100: 0xFF (59 bytes)
for ($i = 42; $i -le 100; $i++) { $bytesA[$i] = 0xFF }
# byte 101: 0x42 (non-FF)
$bytesA[101] = 0x42
# bytes 102-299: 0x41
for ($i = 102; $i -le 299; $i++) { $bytesA[$i] = 0x41 }
[System.IO.File]::WriteAllBytes($fileA, $bytesA)

# Synthetic file B: 200 bytes, all FF after offset 42
$fileB = Join-Path $syntheticDir 'synthetic-b.bin'
$bytesB = [byte[]]::new(200)
$bytesB[0] = 0x49; $bytesB[1] = 0x47; $bytesB[2] = 0x4D; $bytesB[3] = 0x42
$bytesB[4] = 0x02; $bytesB[5] = 0x00; $bytesB[6] = 0x00; $bytesB[7] = 0x00
$bytesB[20] = 0x02; $bytesB[21] = 0x00; $bytesB[22] = 0x00; $bytesB[23] = 0x00
$bytesB[24] = 0x07; $bytesB[25] = 0x00
$bytesB[26] = 0x50; $bytesB[27] = 0x6F; $bytesB[28] = 0x6C; $bytesB[29] = 0x79
$bytesB[30] = 0x67; $bytesB[31] = 0x6F; $bytesB[32] = 0x6E
$bytesB[33] = 0x07; $bytesB[34] = 0x00
$bytesB[35] = 0x68; $bytesB[36] = 0x69; $bytesB[37] = 0x67; $bytesB[38] = 0x68
$bytesB[39] = 0x77; $bytesB[40] = 0x61; $bytesB[41] = 0x79
for ($i = 42; $i -le 199; $i++) { $bytesB[$i] = 0xFF }
[System.IO.File]::WriteAllBytes($fileB, $bytesB)

$outA = Join-Path $outDir 'run-a'
$outB = Join-Path $outDir 'run-b'

# === Assertion 1: .local guard exits nonzero ===
$eap = $ErrorActionPreference; $ErrorActionPreference = 'SilentlyContinue'
$null = & powershell.exe -NonInteractive -NoProfile -File $inspector `
    -ReferenceWorldmapBinPath $fileA -Output "$env:TEMP\pzmf-map8u-guard-test" 2>&1
$guardExit = $LASTEXITCODE
$ErrorActionPreference = $eap
Assert-True ($guardExit -ne 0) '.local guard exits nonzero'

# === Run file A with MaxBytes=150 ===
$eap = $ErrorActionPreference; $ErrorActionPreference = 'SilentlyContinue'
$null = & powershell.exe -NonInteractive -NoProfile -File $inspector `
    -ReferenceWorldmapBinPath $fileA `
    -Output $outA `
    -StringPoolEndOffset 42 `
    -MaxBytes 150 2>&1
$exitA = $LASTEXITCODE
$ErrorActionPreference = $eap

# === Assertion 2: exits 0 (file A) ===
Assert-True ($exitA -eq 0) 'inspector exits 0 (file A)'

$jsonPathA = Join-Path $outA 'igmb-first-non-ff-transition-inspection.json'
$mdPathA   = Join-Path $outA 'igmb-first-non-ff-transition-inspection.md'

# === Assertion 3: JSON exists ===
Assert-True (Test-Path $jsonPathA) 'JSON output exists (file A)'

# === Assertion 4: MD exists ===
Assert-True (Test-Path $mdPathA) 'MD output exists (file A)'

$pA = $null
if (Test-Path $jsonPathA) {
    $pA = Get-Content -Raw $jsonPathA | ConvertFrom-Json
}

# === Assertion 5: schema ===
Assert-True ($null -ne $pA -and $pA.schema -eq 'pzmapforge.map8u-igmb-first-non-ff-transition-inspection.v0.1') 'schema == pzmapforge.map8u-igmb-first-non-ff-transition-inspection.v0.1'

# === Assertion 6: reference_present ===
Assert-True ($null -ne $pA -and $pA.reference_present -eq $true) 'reference_present == true'

# === Assertion 7: bytes_read_count == 150 ===
Assert-True ($null -ne $pA -and [int]$pA.bytes_read_count -eq 150) 'bytes_read_count == 150'

# === Assertion 8: max_bytes_allowed == 65536 ===
Assert-True ($null -ne $pA -and [int]$pA.max_bytes_allowed -eq 65536) 'max_bytes_allowed == 65536'

# === Assertion 9: full_file_read == false ===
Assert-True ($null -ne $pA -and $pA.full_file_read -eq $false) 'full_file_read == false'

# === Assertion 10: scan_start_offset == 42 ===
Assert-True ($null -ne $pA -and [int]$pA.scan_start_offset -eq 42) 'scan_start_offset == 42'

# === Assertion 11: first_non_ff_found == true ===
Assert-True ($null -ne $pA -and $pA.first_non_ff_found -eq $true) 'first_non_ff_found == true'

# === Assertion 12: first_non_ff_offset == 101 ===
Assert-True ($null -ne $pA -and [int]$pA.first_non_ff_offset -eq 101) 'first_non_ff_offset == 101'

# === Assertion 13: first_non_ff_relative_offset_after_string_pool == 59 ===
Assert-True ($null -ne $pA -and [int]$pA.first_non_ff_relative_offset_after_string_pool -eq 59) 'first_non_ff_relative_offset_after_string_pool == 59'

# === Assertion 14: ff_run_start_offset == 42 ===
Assert-True ($null -ne $pA -and [int]$pA.ff_run_start_offset -eq 42) 'ff_run_start_offset == 42'

# === Assertion 15: ff_run_length_until_first_non_ff == 59 ===
Assert-True ($null -ne $pA -and [int]$pA.ff_run_length_until_first_non_ff -eq 59) 'ff_run_length_until_first_non_ff == 59'

# === Assertion 16: transition_offset_is_4_byte_aligned == false (101 % 4 == 1) ===
Assert-True ($null -ne $pA -and $pA.transition_offset_is_4_byte_aligned -eq $false) 'transition_offset_is_4_byte_aligned == false'

# === Assertion 17: confidence_level == low ===
Assert-True ($null -ne $pA -and $pA.confidence_level -eq 'low') "confidence_level == 'low'"

# === Assertion 18: binary_writer_gate_closed == true ===
Assert-True ($null -ne $pA -and $pA.binary_writer_gate_closed -eq $true) 'binary_writer_gate_closed == true'

# === Assertion 19: playable_claim_allowed == false ===
Assert-True ($null -ne $pA -and $pA.playable_claim_allowed -eq $false) 'playable_claim_allowed == false'

# === Assertion 20: third_party_files_copied == false ===
Assert-True ($null -ne $pA -and $pA.third_party_files_copied -eq $false) 'third_party_files_copied == false'

# === Run file B with MaxBytes=70 (not-found case) ===
$eap = $ErrorActionPreference; $ErrorActionPreference = 'SilentlyContinue'
$null = & powershell.exe -NonInteractive -NoProfile -File $inspector `
    -ReferenceWorldmapBinPath $fileB `
    -Output $outB `
    -StringPoolEndOffset 42 `
    -MaxBytes 70 2>&1
$exitB = $LASTEXITCODE
$ErrorActionPreference = $eap

# === Assertion 21: not-found case exits 0 ===
Assert-True ($exitB -eq 0) 'not-found case exits 0 (file B)'

$jsonPathB = Join-Path $outB 'igmb-first-non-ff-transition-inspection.json'
$pB = $null
if (Test-Path $jsonPathB) {
    $pB = Get-Content -Raw $jsonPathB | ConvertFrom-Json
}

# === Assertion 22: not-found: first_non_ff_found == false ===
Assert-True ($null -ne $pB -and $pB.first_non_ff_found -eq $false) 'not-found: first_non_ff_found == false'

# === Assertion 23: not-found: interpretation == ff_region_continues_beyond_bounded_scan ===
Assert-True ($null -ne $pB -and $pB.interpretation -eq 'ff_region_continues_beyond_bounded_scan') "not-found: interpretation == 'ff_region_continues_beyond_bounded_scan'"

Write-Host ""
Write-Host "Results: $pass passed, $fail failed, $total total"

if ($fail -gt 0) { exit 1 }
exit 0
