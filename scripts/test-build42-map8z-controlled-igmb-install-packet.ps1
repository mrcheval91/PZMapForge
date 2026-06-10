#Requires -Version 5.1
<#
.SYNOPSIS
    Tests for the MAP-8Z controlled install packet script.
    24 assertions. All output under .local/.
#>

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$scriptDir     = Split-Path -Parent $MyInvocation.MyCommand.Path
$packetScript  = Join-Path $scriptDir 'prepare-build42-map8z-controlled-igmb-install-packet.ps1'
$testOutputDir = Join-Path $scriptDir '.local\test-map8z-output'
$testSrcDir    = Join-Path $scriptDir '.local\test-map8z-source'
$testSrcFile   = Join-Path $testSrcDir 'worldmap.xml.bin'

$pass = 0
$fail = 0

function Assert-True {
    param([bool]$cond, [string]$msg)
    if ($cond) { Write-Output "PASS: $msg"; $script:pass++ }
    else        { Write-Output "FAIL: $msg"; $script:fail++ }
}

# Create synthetic 65536-byte source file (IGMB magic, rest 0xFF)
if (-not (Test-Path -LiteralPath $testSrcDir)) {
    New-Item -ItemType Directory -Force -Path $testSrcDir | Out-Null
}
$synBuf = [byte[]]::new(65536)
$synBuf[0] = 0x49; $synBuf[1] = 0x47; $synBuf[2] = 0x4D; $synBuf[3] = 0x42
for ($i = 4; $i -lt 65536; $i++) { $synBuf[$i] = 0xFF }
[System.IO.File]::WriteAllBytes($testSrcFile, $synBuf)

# Create a temp file outside .local for guard test 3
$tmpSrcFile = Join-Path $env:TEMP 'pzmf-map8z-test-src.bin'
[System.IO.File]::WriteAllBytes($tmpSrcFile, $synBuf)

# ---- Guard tests ----

# 1. Output guard: non-.local path exits nonzero
$savedEAP = $ErrorActionPreference
$ErrorActionPreference = 'Continue'
& powershell -ExecutionPolicy Bypass -File $packetScript `
    -Output "$env:TEMP\pzmf-map8z-no-local" `
    -GeneratedWorldmapBinPath $testSrcFile 2>$null
$ec1 = $LASTEXITCODE
$ErrorActionPreference = $savedEAP
Assert-True ($ec1 -ne 0) '.local guard on -Output exits nonzero'

# 2. Missing source file exits nonzero
$savedEAP = $ErrorActionPreference
$ErrorActionPreference = 'Continue'
& powershell -ExecutionPolicy Bypass -File $packetScript `
    -Output $testOutputDir `
    -GeneratedWorldmapBinPath "$scriptDir\.local\nonexistent\worldmap.xml.bin" 2>$null
$ec2 = $LASTEXITCODE
$ErrorActionPreference = $savedEAP
Assert-True ($ec2 -ne 0) 'missing GeneratedWorldmapBinPath exits nonzero'

# 3. Source outside .local exits nonzero
$savedEAP = $ErrorActionPreference
$ErrorActionPreference = 'Continue'
& powershell -ExecutionPolicy Bypass -File $packetScript `
    -Output $testOutputDir `
    -GeneratedWorldmapBinPath $tmpSrcFile 2>$null
$ec3 = $LASTEXITCODE
$ErrorActionPreference = $savedEAP
Assert-True ($ec3 -ne 0) 'source outside .local exits nonzero'

# ---- Valid run ----

# Clean prior output
if (Test-Path -LiteralPath $testOutputDir) {
    Remove-Item -LiteralPath $testOutputDir -Recurse -Force
}

$savedEAP = $ErrorActionPreference
$ErrorActionPreference = 'Continue'
& powershell -ExecutionPolicy Bypass -File $packetScript `
    -Output $testOutputDir `
    -GeneratedWorldmapBinPath $testSrcFile
$validExit = $LASTEXITCODE
$ErrorActionPreference = $savedEAP

# 4. Valid run exits 0
Assert-True ($validExit -eq 0) 'valid run exits 0'

$jsonPath  = Join-Path $testOutputDir 'map8z-controlled-igmb-install-packet.json'
$mdPath    = Join-Path $testOutputDir 'map8z-controlled-igmb-install-packet.md'
$stepsPath = Join-Path $testOutputDir 'MAP_8Z_HUMAN_INSTALL_STEPS.md'
$stagedBin = Join-Path $testOutputDir 'staged\common\media\maps\PZMapForge\worldmap.xml.bin'

# 5. JSON exists
Assert-True (Test-Path -LiteralPath $jsonPath) 'map8z-controlled-igmb-install-packet.json exists'
# 6. MD exists
Assert-True (Test-Path -LiteralPath $mdPath) 'map8z-controlled-igmb-install-packet.md exists'
# 7. Human install steps MD exists
Assert-True (Test-Path -LiteralPath $stepsPath) 'MAP_8Z_HUMAN_INSTALL_STEPS.md exists'
# 8. Staged worldmap.xml.bin exists
Assert-True (Test-Path -LiteralPath $stagedBin) 'staged worldmap.xml.bin exists'

$p = Get-Content -LiteralPath $jsonPath -Raw | ConvertFrom-Json

# 9. source_size_bytes == 65536
Assert-True ([int]$p.source_size_bytes -eq 65536) "source_size_bytes == 65536 (got $($p.source_size_bytes))"
# 10. staged_size_bytes == 65536
Assert-True ([int]$p.staged_size_bytes -eq 65536) "staged_size_bytes == 65536 (got $($p.staged_size_bytes))"
# 11. sha256_match == true
Assert-True ($p.sha256_match -eq $true) 'sha256_match == true'
# 12. schema correct
$schema = $p.schema
Assert-True ($schema -eq 'pzmapforge.map8z-controlled-igmb-install-packet.v0.1') "schema correct (got '$schema')"
# 13. controlled_install_packet_created == true
Assert-True ($p.controlled_install_packet_created -eq $true) 'controlled_install_packet_created == true'
# 14. human_manual_copy_required == true
Assert-True ($p.human_manual_copy_required -eq $true) 'human_manual_copy_required == true'
# 15. claude_copied_to_workshop == false
Assert-True ($p.claude_copied_to_workshop -eq $false) 'claude_copied_to_workshop == false'
# 16. claude_modified_steam_files == false
Assert-True ($p.claude_modified_steam_files -eq $false) 'claude_modified_steam_files == false'
# 17. claude_ran_pz == false
Assert-True ($p.claude_ran_pz -eq $false) 'claude_ran_pz == false'
# 18. claude_uploaded_workshop == false
Assert-True ($p.claude_uploaded_workshop -eq $false) 'claude_uploaded_workshop == false'
# 19. third_party_bytes_copied == false
Assert-True ($p.third_party_bytes_copied -eq $false) 'third_party_bytes_copied == false'
# 20. generated_file_only == true
Assert-True ($p.generated_file_only -eq $true) 'generated_file_only == true'
# 21. playable_claim_allowed == false
Assert-True ($p.playable_claim_allowed -eq $false) 'playable_claim_allowed == false'
# 22. load_test_performed == false
Assert-True ($p.load_test_performed -eq $false) 'load_test_performed == false'
# 23. target_relative_path correct
$trp = $p.target_relative_path
Assert-True ($trp -eq 'common/media/maps/PZMapForge/worldmap.xml.bin') "target_relative_path correct (got '$trp')"
# 24. next_branch correct
$nb = $p.next_branch
Assert-True ($nb -eq 'human_runtime_test_pending') "next_branch == 'human_runtime_test_pending' (got '$nb')"

# Cleanup temp file
if (Test-Path -LiteralPath $tmpSrcFile) {
    Remove-Item -LiteralPath $tmpSrcFile -Force -ErrorAction SilentlyContinue
}

# Report
Write-Output ""
Write-Output "Results: $pass passed, $fail failed"
if ($fail -gt 0) { exit 1 }
exit 0
