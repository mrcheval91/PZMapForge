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

$scriptsDir   = Split-Path -Parent $MyInvocation.MyCommand.Path
$packetScript = Join-Path $scriptsDir 'prepare-build42-map8w-transition-structure-result-packet.ps1'
$outDir       = Join-Path $scriptsDir '.local\test-map8w-result-output'
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

$jsonPath   = Join-Path $outDir 'map8w-transition-structure-result.json'
$mdPath     = Join-Path $outDir 'map8w-transition-structure-result.md'
$packetPath = Join-Path $outDir 'MAP_8W_IGMB_TRANSITION_STRUCTURE_ANALYSIS_PACKET.md'

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
Assert-True ($null -ne $p -and $p.schema -eq 'pzmapforge.map8w-result.v0.1') "schema == 'pzmapforge.map8w-result.v0.1'"

# === Assertion 7: operator_approved_transition_structure_analysis == true ===
Assert-True ($null -ne $p -and $p.operator_approved_transition_structure_analysis -eq $true) 'operator_approved_transition_structure_analysis == true'

# === Assertion 8: transition_offset == 6389 ===
Assert-True ($null -ne $p -and [int]$p.transition_offset -eq 6389) 'transition_offset == 6389'

# === Assertion 9: max_bytes_allowed == 65536 ===
Assert-True ($null -ne $p -and [int]$p.max_bytes_allowed -eq 65536) 'max_bytes_allowed == 65536'

# === Assertion 10: full_file_read == false ===
Assert-True ($null -ne $p -and $p.full_file_read -eq $false) 'full_file_read == false'

# === Assertion 11: transition_structure_understood == false ===
Assert-True ($null -ne $p -and $p.transition_structure_understood -eq $false) 'transition_structure_understood == false'

# === Assertion 12: full_format_understood == false ===
Assert-True ($null -ne $p -and $p.full_format_understood -eq $false) 'full_format_understood == false'

# === Assertion 13: cell_index_understood == false ===
Assert-True ($null -ne $p -and $p.cell_index_understood -eq $false) 'cell_index_understood == false'

# === Assertion 14: geometry_payload_understood == false ===
Assert-True ($null -ne $p -and $p.geometry_payload_understood -eq $false) 'geometry_payload_understood == false'

# === Assertion 15: writer_implementation_allowed == false ===
Assert-True ($null -ne $p -and $p.writer_implementation_allowed -eq $false) 'writer_implementation_allowed == false'

# === Assertion 16: binary_writer_gate_closed == true ===
Assert-True ($null -ne $p -and $p.binary_writer_gate_closed -eq $true) 'binary_writer_gate_closed == true'

# === Assertion 17: playable_claim_allowed == false ===
Assert-True ($null -ne $p -and $p.playable_claim_allowed -eq $false) 'playable_claim_allowed == false'

# === Assertion 18: third_party_files_copied == false ===
Assert-True ($null -ne $p -and $p.third_party_files_copied -eq $false) 'third_party_files_copied == false'

# === Assertion 19: no_pz_run_by_claude == true ===
Assert-True ($null -ne $p -and $p.no_pz_run_by_claude -eq $true) 'no_pz_run_by_claude == true'

# === Assertion 20: packet doc contains MAP8W_IGMB_TRANSITION_STRUCTURE_ANALYSIS_APPROVED ===
$packetContent = ''
if (Test-Path $packetPath) { $packetContent = Get-Content -Raw $packetPath }
Assert-True ($packetContent.Contains('MAP8W_IGMB_TRANSITION_STRUCTURE_ANALYSIS_APPROVED')) 'packet doc contains MAP8W_IGMB_TRANSITION_STRUCTURE_ANALYSIS_APPROVED sentinel'

Write-Host ""
Write-Host "Results: $pass passed, $fail failed, $total total"

if ($fail -gt 0) { exit 1 }
exit 0
