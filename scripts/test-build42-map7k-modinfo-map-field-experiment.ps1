#Requires -Version 5.1
<#
.SYNOPSIS
    Tests for MAP-7K: prepare-build42-map7k-modinfo-map-field-experiment-packet.ps1.

    Uses .local fixtures via the packet script (dotnet CLI required).
    Does NOT depend on user-local logs.
    Expected assertion count: 11
#>

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$scriptDir    = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot     = Split-Path -Parent $scriptDir
$packetScript = Join-Path $repoRoot 'scripts\prepare-build42-map7k-modinfo-map-field-experiment-packet.ps1'
$tempRoot     = [System.IO.Path]::GetTempPath()
$mapId        = 'pzmapforge_build42_candidate_v4_001'

$pass = 0
$fail = 0

function Assert-True {
    param([bool]$Condition, [string]$Label)
    if ($Condition) { Write-Output "  PASS  $Label"; $script:pass++ }
    else            { Write-Output "  FAIL  $Label"; $script:fail++ }
}

Write-Output 'test-build42-map7k-modinfo-map-field-experiment.ps1'
Write-Output ''

# ---------------------------------------------------------------------------
# Setup
# ---------------------------------------------------------------------------

$testBase = Join-Path $tempRoot ('pzmf-t7k-' + [System.IO.Path]::GetRandomFileName())
$badPath  = Join-Path $tempRoot 'pzmf-t7k-bad-no-local'
New-Item -ItemType Directory -Force -Path $badPath | Out-Null

function Invoke-Packet {
    param([string]$OutDir)
    & powershell -ExecutionPolicy Bypass -File $packetScript -Output $OutDir | Out-Null
    return [int]$LASTEXITCODE
}

# ===========================================================================
# Test 1: Packet refuses output outside .local
# ===========================================================================

Write-Output '--- Test 1: Packet refuses output outside .local ---'
$t1exit = Invoke-Packet -OutDir $badPath
Assert-True ($t1exit -ne 0) 'Test1: packet output outside .local exits nonzero'

# ===========================================================================
# Tests 2-11: Run packet and verify output
# ===========================================================================

Write-Output ''
Write-Output '--- Running packet (Tests 2-11) ---'
$packetOut = Join-Path $testBase '.local\packet'
$t2exit    = Invoke-Packet -OutDir $packetOut

# Test 2: Packet writes all required files
$reqFiles = @(
    'MAP_7K_MODINFO_MAP_FIELD_PACKET.md',
    'MAP_7K_VARIANT_F_RESULT_SUMMARY.md',
    'MAP_7K_EXPERIMENT_G_MANUAL_INSTALL_COMMANDS.md',
    'MAP_7K_EXPERIMENT_G_LOG_CAPTURE_COMMANDS.md',
    'MAP_7K_EXPERIMENT_G_MANUAL_RESULT.local-template.md',
    'map7k-modinfo-map-field-preflight.json',
    'map7k-modinfo-map-field-preflight.md',
    'map-discovery-path-report.json',
    'map-discovery-path-report.md',
    'map-metadata-contract-report.json',
    'map-metadata-contract-report.md'
)
$allPresent = $true
foreach ($f in $reqFiles) {
    if (-not (Test-Path (Join-Path $packetOut $f))) { $allPresent = $false }
}
Assert-True ($t2exit -eq 0 -and $allPresent) 'Test2: packet exits 0 and all 11 required files present'

# Experiment-G candidate paths
$expGBase   = Join-Path $packetOut "experiment-g-candidate\$mapId"
$expG42Dir  = Join-Path $expGBase '42'
$expGRoot42ModInfo  = Join-Path $expG42Dir 'mod.info'
$expGRootModInfo    = Join-Path $expGBase 'mod.info'

# Test 3: Experiment-G root mod.info contains map=<MapId>
Write-Output ''
Write-Output '--- Test 3: Root mod.info contains map= field ---'
$rootModContent = if (Test-Path $expGRootModInfo) {
    Get-Content $expGRootModInfo -Raw
} else { '' }
Assert-True ($rootModContent -match "map=$mapId") `
    "Test3: root mod.info contains map=$mapId"

# Test 4: Experiment-G 42/mod.info contains map=<MapId>
Write-Output ''
Write-Output '--- Test 4: 42/mod.info contains map= field ---'
$v42ModContent = if (Test-Path $expGRoot42ModInfo) {
    Get-Content $expGRoot42ModInfo -Raw
} else { '' }
Assert-True ($v42ModContent -match "map=$mapId") `
    "Test4: 42/mod.info contains map=$mapId"

# Test 5: mod.info files remain no-BOM
Write-Output ''
Write-Output '--- Test 5: mod.info files remain no-BOM ---'
function Test-HasBom {
    param([string]$Path)
    if (-not (Test-Path -LiteralPath $Path)) { return $false }
    $b = [System.IO.File]::ReadAllBytes($Path)
    return ($b.Length -ge 3 -and $b[0] -eq 0xEF -and $b[1] -eq 0xBB -and $b[2] -eq 0xBF)
}
$rootNoBom = -not (Test-HasBom $expGRootModInfo)
$v42NoBom  = -not (Test-HasBom $expGRoot42ModInfo)
Assert-True ($rootNoBom -and $v42NoBom) `
    "Test5: root mod.info no-BOM=$rootNoBom, 42/mod.info no-BOM=$v42NoBom"

# Test 6: LOTH/LOTP/chunkdata sizes unchanged from empty_grass_v4
Write-Output ''
Write-Output '--- Test 6: LOTH/LOTP/chunkdata sizes unchanged ---'
$discJson = if (Test-Path (Join-Path $packetOut 'map-discovery-path-report.json')) {
    Get-Content (Join-Path $packetOut 'map-discovery-path-report.json') -Raw | ConvertFrom-Json
} else { $null }
$lothSize      = if ($null -ne $discJson) { [long]$discJson.versioned_42_files.lotheader.size } else { 0 }
$lotpSize      = if ($null -ne $discJson) { [long]$discJson.versioned_42_files.lotpack.size   } else { 0 }
$chunkSize     = if ($null -ne $discJson) { [long]$discJson.versioned_42_files.chunkdata.size } else { 0 }
# Binary files unchanged: all > 0 and match known empty_grass_v4 sizes
$binarySizesOk = ($lothSize -eq 29646) -and ($lotpSize -eq 1056780) -and ($chunkSize -eq 1026)
Assert-True $binarySizesOk `
    "Test6: LOTH=29646($lothSize), LOTP=1056780($lotpSize), chunkdata=1026($chunkSize)"

# Preflight JSON checks
$pfl = if (Test-Path (Join-Path $packetOut 'map7k-modinfo-map-field-preflight.json')) {
    Get-Content (Join-Path $packetOut 'map7k-modinfo-map-field-preflight.json') -Raw | ConvertFrom-Json
} else { $null }

# Test 7: Preflight records variant_f_result
Write-Output ''
Write-Output '--- Test 7: Preflight variant_f_result ---'
$vfResult = if ($null -ne $pfl) { [string]$pfl.variant_f_result } else { '' }
Assert-True ($vfResult -eq 'MAP7F_VARIANT_F_MAP_FOLDER_SCAN_EMPTY') `
    "Test7: preflight variant_f_result == MAP7F_VARIANT_F_MAP_FOLDER_SCAN_EMPTY (got '$vfResult')"

# Test 8: Preflight records h5_folder_id_alignment_ruled_out=true
Write-Output ''
Write-Output '--- Test 8: Preflight h5_folder_id_alignment_ruled_out ---'
$h5Ruled = if ($null -ne $pfl) { [bool]$pfl.h5_folder_id_alignment_ruled_out } else { $false }
Assert-True ($h5Ruled -eq $true) 'Test8: preflight h5_folder_id_alignment_ruled_out == true'

# Test 9: Preflight records h8_mod_info_map_field=true
Write-Output ''
Write-Output '--- Test 9: Preflight h8_mod_info_map_field ---'
$h8Flag = if ($null -ne $pfl) { [bool]$pfl.h8_mod_info_map_field } else { $false }
Assert-True ($h8Flag -eq $true) 'Test9: preflight h8_mod_info_map_field == true'

# Test 10: Packet contains HUMAN-ONLY install language
Write-Output ''
Write-Output '--- Test 10: Packet contains HUMAN-ONLY install language ---'
$installContent = if (Test-Path (Join-Path $packetOut 'MAP_7K_EXPERIMENT_G_MANUAL_INSTALL_COMMANDS.md')) {
    Get-Content (Join-Path $packetOut 'MAP_7K_EXPERIMENT_G_MANUAL_INSTALL_COMMANDS.md') -Raw
} else { '' }
Assert-True ($installContent -match 'HUMAN-ONLY') `
    'Test10: install commands contain HUMAN-ONLY'

# Test 11: Packet contains public_playable_claim_allowed=false
Write-Output ''
Write-Output '--- Test 11: Packet public_playable_claim_allowed=false ---'
Assert-True ($null -ne $pfl -and [bool]$pfl.public_playable_claim_allowed -eq $false) `
    'Test11: preflight public_playable_claim_allowed == false'

# ---------------------------------------------------------------------------
# Summary
# ---------------------------------------------------------------------------

Write-Output ''
Write-Output '----------------------------------------'
Write-Output "Results: $pass passed, $fail failed"
Write-Output '----------------------------------------'

if ($fail -gt 0) { exit 1 }
exit 0
