#Requires -Version 5.1
<#
.SYNOPSIS
    Tests for the MAP-8Y experimental IGMB writer packet script.
    20 assertions. All output under .local/.
#>

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$scriptDir    = Split-Path -Parent $MyInvocation.MyCommand.Path
$packetScript = Join-Path $scriptDir 'prepare-build42-map8y-experimental-igmb-writer-packet.ps1'
$testOutputDir = Join-Path $scriptDir '.local\test-map8y-packet-output'

$pass = 0
$fail = 0

function Assert-True {
    param([bool]$cond, [string]$msg)
    if ($cond) { Write-Output "PASS: $msg"; $script:pass++ }
    else        { Write-Output "FAIL: $msg"; $script:fail++ }
}

# 1. Guard: no .local exits nonzero
$savedEAP = $ErrorActionPreference
$ErrorActionPreference = 'Continue'
& powershell -ExecutionPolicy Bypass -File $packetScript -Output "$env:TEMP\pzmf-map8y-packet-guard-no-local" 2>$null
$guardExit = $LASTEXITCODE
$ErrorActionPreference = $savedEAP
Assert-True ($guardExit -ne 0) '.local guard exits nonzero'

# Run packet script
if (Test-Path -LiteralPath $testOutputDir) {
    Remove-Item -LiteralPath $testOutputDir -Recurse -Force
}
$savedEAP2 = $ErrorActionPreference
$ErrorActionPreference = 'Continue'
& powershell -ExecutionPolicy Bypass -File $packetScript -Output $testOutputDir
$packetExit = $LASTEXITCODE
$ErrorActionPreference = $savedEAP2

# 2. Script exits 0
Assert-True ($packetExit -eq 0) 'packet script exits 0'

$jsonPath      = Join-Path $testOutputDir 'map8y-experimental-igmb-writer-packet.json'
$mdPath        = Join-Path $testOutputDir 'map8y-experimental-igmb-writer-packet.md'
$packetDocPath = Join-Path $testOutputDir 'MAP_8Y_EXPERIMENTAL_IGMB_WRITER_SKELETON_PACKET.md'

# 3. JSON exists
Assert-True (Test-Path -LiteralPath $jsonPath) 'map8y-experimental-igmb-writer-packet.json exists'

# 4. MD exists
Assert-True (Test-Path -LiteralPath $mdPath) 'map8y-experimental-igmb-writer-packet.md exists'

# 5. Packet doc exists
Assert-True (Test-Path -LiteralPath $packetDocPath) 'MAP_8Y_EXPERIMENTAL_IGMB_WRITER_SKELETON_PACKET.md exists'

$packet = Get-Content -LiteralPath $jsonPath -Raw | ConvertFrom-Json

# 6. Schema correct
$schema = $packet.schema
Assert-True ($schema -eq 'pzmapforge.map8y-experimental-igmb-writer-packet.v0.1') "schema correct (got '$schema')"

# 7. experimental_writer_added == true
Assert-True ($packet.experimental_writer_added -eq $true) 'experimental_writer_added == true'

# 8. writes_worldmap_xml_bin == true
Assert-True ($packet.writes_worldmap_xml_bin -eq $true) 'writes_worldmap_xml_bin == true'

# 9. output_scope == '.local_only'
Assert-True ($packet.output_scope -eq '.local_only') "output_scope == '.local_only' (got '$($packet.output_scope)')"

# 10. generated_from_scratch == true
Assert-True ($packet.generated_from_scratch -eq $true) 'generated_from_scratch == true'

# 11. third_party_bytes_copied == false
Assert-True ($packet.third_party_bytes_copied -eq $false) 'third_party_bytes_copied == false'

# 12. project_russia_file_read == false
Assert-True ($packet.project_russia_file_read -eq $false) 'project_russia_file_read == false'

# 13. pz_run_performed == false
Assert-True ($packet.pz_run_performed -eq $false) 'pz_run_performed == false'

# 14. workshop_upload_performed == false
Assert-True ($packet.workshop_upload_performed -eq $false) 'workshop_upload_performed == false'

# 15. playable_claim_allowed == false
Assert-True ($packet.playable_claim_allowed -eq $false) 'playable_claim_allowed == false'

# 16. writer_status == 'experimental_skeleton_not_load_proven'
$ws = $packet.writer_status
Assert-True ($ws -eq 'experimental_skeleton_not_load_proven') "writer_status correct (got '$ws')"

# 17. full_igmb_format_understood == false
Assert-True ($packet.full_igmb_format_understood -eq $false) 'full_igmb_format_understood == false'

# 18. cell_index_understood == false
Assert-True ($packet.cell_index_understood -eq $false) 'cell_index_understood == false'

# 19. geometry_payload_understood == false
Assert-True ($packet.geometry_payload_understood -eq $false) 'geometry_payload_understood == false'

# 20. Packet doc contains MAP8Y_EXPERIMENTAL_IGMB_WRITER_SKELETON_ADDED
$packetDocContent = Get-Content -LiteralPath $packetDocPath -Raw
Assert-True ($packetDocContent -match 'MAP8Y_EXPERIMENTAL_IGMB_WRITER_SKELETON_ADDED') 'packet doc contains MAP8Y_EXPERIMENTAL_IGMB_WRITER_SKELETON_ADDED'

# Report
Write-Output ""
Write-Output "Results: $pass passed, $fail failed"
if ($fail -gt 0) { exit 1 }
exit 0
