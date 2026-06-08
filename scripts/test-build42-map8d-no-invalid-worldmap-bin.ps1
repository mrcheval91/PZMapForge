#Requires -Version 5.1
<#
.SYNOPSIS
    Tests for MAP-8D: prepare-build42-map8d-no-invalid-worldmap-bin-packet.ps1.

    Expected assertion count: 20
#>

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$scriptDir    = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot     = Split-Path -Parent $scriptDir
$packetScript = Join-Path $repoRoot 'scripts\prepare-build42-map8d-no-invalid-worldmap-bin-packet.ps1'
$tempRoot     = [System.IO.Path]::GetTempPath()

$pass = 0
$fail = 0

function Assert-True {
    param([bool]$Condition, [string]$Label)
    if ($Condition) { Write-Output "  PASS  $Label"; $script:pass++ }
    else            { Write-Output "  FAIL  $Label"; $script:fail++ }
}

Write-Output 'test-build42-map8d-no-invalid-worldmap-bin.ps1'
Write-Output ''

$testBase = Join-Path $tempRoot ('pzmf-t8d-' + [System.IO.Path]::GetRandomFileName())
$badPath  = Join-Path $tempRoot 'pzmf-t8d-bad-no-local'
New-Item -ItemType Directory -Force -Path $badPath | Out-Null

function Invoke-Packet {
    param([string]$OutDir)
    & powershell -ExecutionPolicy Bypass -File $packetScript -Output $OutDir | Out-Null
    return [int]$LASTEXITCODE
}

# Test 1: Script refuses output outside .local
Write-Output '--- Test 1: Refuses output outside .local ---'
$t1exit = Invoke-Packet -OutDir $badPath
Assert-True ($t1exit -ne 0) 'Test1: output outside .local exits nonzero'

# Run packet
Write-Output ''
Write-Output '--- Running packet (Tests 2-20) ---'
$packetOut = Join-Path $testBase '.local\map8d-packet'
$t2exit    = Invoke-Packet -OutDir $packetOut

# Test 2: Exits 0
Assert-True ($t2exit -eq 0) 'Test2: packet exits 0 with valid output path'

# Tests 3-5: Required packet files exist
Write-Output ''
Write-Output '--- Tests 3-5: Required packet files ---'
Assert-True (Test-Path (Join-Path $packetOut 'MAP_8D_NO_INVALID_WORLDMAP_BIN_PACKET.md')) `
    'Test3: MAP_8D_NO_INVALID_WORLDMAP_BIN_PACKET.md exists'

Assert-True (Test-Path (Join-Path $packetOut 'map8d-preflight.json')) `
    'Test4: map8d-preflight.json exists'

Assert-True (Test-Path (Join-Path $packetOut 'map8d-preflight.md')) `
    'Test5: map8d-preflight.md exists'

# Parse preflight JSON
$pfl = if (Test-Path (Join-Path $packetOut 'map8d-preflight.json')) {
    Get-Content (Join-Path $packetOut 'map8d-preflight.json') -Raw | ConvertFrom-Json
} else { $null }

Write-Output ''
Write-Output '--- Tests 6-16: Preflight JSON fields ---'

# Test 6: Schema correct
Assert-True ($null -ne $pfl -and [string]$pfl.schema -eq 'pzmapforge.map8d-preflight.v0.1') `
    "Test6: schema correct (got '$($pfl.schema)')"

# Test 7: source_basis = MAP-8B
Assert-True ($null -ne $pfl -and [string]$pfl.source_basis -eq 'MAP-8B') `
    "Test7: source_basis=MAP-8B (got '$($pfl.source_basis)')"

# Test 8: version_scoped_media_path = true
Assert-True ($null -ne $pfl -and [bool]$pfl.version_scoped_media_path -eq $true) `
    'Test8: version_scoped_media_path=true'

# Test 9: invalid_worldmap_bin_stubs_removed = true
Assert-True ($null -ne $pfl -and [bool]$pfl.invalid_worldmap_bin_stubs_removed -eq $true) `
    'Test9: invalid_worldmap_bin_stubs_removed=true'

# Test 10: worldmap_xml_retained = true
Assert-True ($null -ne $pfl -and [bool]$pfl.worldmap_xml_retained -eq $true) `
    'Test10: worldmap_xml_retained=true'

# Test 11: worldmap_png_retained = true
Assert-True ($null -ne $pfl -and [bool]$pfl.worldmap_png_retained -eq $true) `
    'Test11: worldmap_png_retained=true'

# Test 12: streets_xml_bin_removed = true
Assert-True ($null -ne $pfl -and [bool]$pfl.streets_xml_bin_removed -eq $true) `
    'Test12: streets_xml_bin_removed=true'

# Test 13: binary_writer_gate_closed = true
Assert-True ($null -ne $pfl -and [bool]$pfl.binary_writer_gate_closed -eq $true) `
    'Test13: binary_writer_gate_closed=true'

# Test 14: playable_claim_allowed = false
Assert-True ($null -ne $pfl -and [bool]$pfl.playable_claim_allowed -eq $false) `
    'Test14: playable_claim_allowed=false'

# Test 15: no_pz_run_by_script = true
Assert-True ($null -ne $pfl -and [bool]$pfl.no_pz_run_by_script -eq $true) `
    'Test15: no_pz_run_by_script=true'

# Test 16: no_workshop_upload_by_script = true
Assert-True ($null -ne $pfl -and [bool]$pfl.no_workshop_upload_by_script -eq $true) `
    'Test16: no_workshop_upload_by_script=true'

# Staged package paths
$candidateMapId = 'pzmapforge_build42_candidate_v4_001'
$stagedMapsDir  = Join-Path $packetOut "staged-workshop-no-worldmap-bin\$candidateMapId\42\media\maps\$candidateMapId"

Write-Output ''
Write-Output '--- Tests 17-20: Staged package contents ---'

# Test 17: Staged map dir exists (version-scoped 42\media path)
Assert-True (Test-Path $stagedMapsDir) `
    'Test17: staged 42\media\maps\<MapId>\ directory exists'

# Test 18: worldmap.xml.bin is NOT present (removed)
Assert-True (-not (Test-Path (Join-Path $stagedMapsDir 'worldmap.xml.bin'))) `
    'Test18: worldmap.xml.bin is ABSENT from staged package'

# Test 19: worldmap.xml (uncompiled) IS present
Assert-True (Test-Path (Join-Path $stagedMapsDir 'worldmap.xml')) `
    'Test19: worldmap.xml (uncompiled) exists in staged package'

# Test 20: MAP8D_NO_WORLDMAP_BIN_STUBS.txt canary present
Assert-True (Test-Path (Join-Path $stagedMapsDir 'MAP8D_NO_WORLDMAP_BIN_STUBS.txt')) `
    'Test20: MAP8D_NO_WORLDMAP_BIN_STUBS.txt canary exists'

Write-Output ''
Write-Output '----------------------------------------'
Write-Output "Results: $pass passed, $fail failed"
Write-Output '----------------------------------------'

if ($fail -gt 0) { exit 1 }
exit 0
