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
$packetScript = Join-Path $scriptsDir 'prepare-build42-map8u-first-non-ff-transition-result-packet.ps1'
$outDir = Join-Path $scriptsDir '.local\test-map8u-result-output'
if (-not (Test-Path $outDir)) { New-Item -ItemType Directory -Force -Path $outDir | Out-Null }

# === Assertion 1: .local guard exits nonzero ===
$eap = $ErrorActionPreference; $ErrorActionPreference = 'SilentlyContinue'
$null = & powershell.exe -NonInteractive -NoProfile -File $packetScript `
    -Output 'C:\temp\no-local-in-path' 2>&1
$guardExit = $LASTEXITCODE
$ErrorActionPreference = $eap
Assert-True ($guardExit -ne 0) '.local guard exits nonzero'

# === Run packet script ===
$eap = $ErrorActionPreference; $ErrorActionPreference = 'SilentlyContinue'
$null = & powershell.exe -NonInteractive -NoProfile -File $packetScript `
    -Output $outDir 2>&1
$exitCode = $LASTEXITCODE
$ErrorActionPreference = $eap

# === Assertion 2: exits 0 ===
Assert-True ($exitCode -eq 0) 'packet script exits 0'

$jsonPath   = Join-Path $outDir 'map8u-first-non-ff-transition-result.json'
$mdPath     = Join-Path $outDir 'map8u-first-non-ff-transition-result.md'
$packetPath = Join-Path $outDir 'MAP_8U_FIRST_NON_FF_TRANSITION_RESULT_PACKET.md'

# === Assertion 3: JSON exists ===
Assert-True (Test-Path $jsonPath) 'JSON output exists'

# === Assertion 4: MD exists ===
Assert-True (Test-Path $mdPath) 'MD output exists'

# === Assertion 5: packet MD exists ===
Assert-True (Test-Path $packetPath) 'packet MD exists'

$p = $null
if (Test-Path $jsonPath) {
    $p = Get-Content -Raw $jsonPath | ConvertFrom-Json
}

# === Assertion 6: schema ===
Assert-True ($null -ne $p -and $p.schema -eq 'pzmapforge.map8u-result.v0.1') "schema == 'pzmapforge.map8u-result.v0.1'"

# === Assertion 7: operator_approved_first_non_ff_scan == true ===
Assert-True ($null -ne $p -and $p.operator_approved_first_non_ff_scan -eq $true) 'operator_approved_first_non_ff_scan == true'

# === Assertion 8: max_bytes_allowed == 65536 ===
Assert-True ($null -ne $p -and [int]$p.max_bytes_allowed -eq 65536) 'max_bytes_allowed == 65536'

# === Assertion 9: string_pool_end_offset == 133 ===
Assert-True ($null -ne $p -and [int]$p.string_pool_end_offset -eq 133) 'string_pool_end_offset == 133'

# === Assertion 10: full_format_understood == false ===
Assert-True ($null -ne $p -and $p.full_format_understood -eq $false) 'full_format_understood == false'

# === Assertion 11: cell_index_understood == false ===
Assert-True ($null -ne $p -and $p.cell_index_understood -eq $false) 'cell_index_understood == false'

# === Assertion 12: geometry_payload_understood == false ===
Assert-True ($null -ne $p -and $p.geometry_payload_understood -eq $false) 'geometry_payload_understood == false'

# === Assertion 13: writer_implementation_allowed == false ===
Assert-True ($null -ne $p -and $p.writer_implementation_allowed -eq $false) 'writer_implementation_allowed == false'

# === Assertion 14: binary_writer_gate_closed == true ===
Assert-True ($null -ne $p -and $p.binary_writer_gate_closed -eq $true) 'binary_writer_gate_closed == true'

# === Assertion 15: playable_claim_allowed == false ===
Assert-True ($null -ne $p -and $p.playable_claim_allowed -eq $false) 'playable_claim_allowed == false'

# === Assertion 16: third_party_files_copied == false ===
Assert-True ($null -ne $p -and $p.third_party_files_copied -eq $false) 'third_party_files_copied == false'

# === Assertion 17: no_pz_run_by_claude == true ===
Assert-True ($null -ne $p -and $p.no_pz_run_by_claude -eq $true) 'no_pz_run_by_claude == true'

# === Assertion 18: no_workshop_upload_by_claude == true ===
Assert-True ($null -ne $p -and $p.no_workshop_upload_by_claude -eq $true) 'no_workshop_upload_by_claude == true'

# === Assertion 19: next_branch ===
Assert-True ($null -ne $p -and $p.next_branch -eq 'igmb_transition_structure_analysis_pending_operator_approval_if_non_ff_found') 'next_branch == igmb_transition_structure_analysis_pending_operator_approval_if_non_ff_found'

# === Assertion 20: packet doc contains MAP8U_FIRST_NON_FF_TRANSITION_SCAN_APPROVED ===
$packetContent = ''
if (Test-Path $packetPath) { $packetContent = Get-Content -Raw $packetPath }
Assert-True ($packetContent.Contains('MAP8U_FIRST_NON_FF_TRANSITION_SCAN_APPROVED')) 'packet doc contains MAP8U_FIRST_NON_FF_TRANSITION_SCAN_APPROVED sentinel'

Write-Host ""
Write-Host "Results: $pass passed, $fail failed, $total total"

if ($fail -gt 0) { exit 1 }
exit 0
