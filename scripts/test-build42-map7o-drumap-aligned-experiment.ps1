#Requires -Version 5.1
<#
.SYNOPSIS
    Tests for MAP-7O: prepare-build42-map7o-drumap-aligned-experiment-packet.ps1.

    Uses .local fixtures via the packet script (dotnet CLI required).
    Expected assertion count: 19
#>

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$scriptDir    = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot     = Split-Path -Parent $scriptDir
$packetScript = Join-Path $repoRoot 'scripts\prepare-build42-map7o-drumap-aligned-experiment-packet.ps1'
$tempRoot     = [System.IO.Path]::GetTempPath()
$mapId        = 'pzmapforge_build42_candidate_v4_001'

$pass = 0
$fail = 0

function Assert-True {
    param([bool]$Condition, [string]$Label)
    if ($Condition) { Write-Output "  PASS  $Label"; $script:pass++ }
    else            { Write-Output "  FAIL  $Label"; $script:fail++ }
}

function Test-HasBom {
    param([string]$Path)
    if (-not (Test-Path -LiteralPath $Path)) { return $false }
    $b = [System.IO.File]::ReadAllBytes($Path)
    return ($b.Length -ge 3 -and $b[0] -eq 0xEF -and $b[1] -eq 0xBB -and $b[2] -eq 0xBF)
}

Write-Output 'test-build42-map7o-drumap-aligned-experiment.ps1'
Write-Output ''

$testBase = Join-Path $tempRoot ('pzmf-t7o-' + [System.IO.Path]::GetRandomFileName())
$badPath  = Join-Path $tempRoot 'pzmf-t7o-bad-no-local'
New-Item -ItemType Directory -Force -Path $badPath | Out-Null

function Invoke-Packet {
    param([string]$OutDir)
    & powershell -ExecutionPolicy Bypass -File $packetScript -Output $OutDir | Out-Null
    return [int]$LASTEXITCODE
}

# Test 1: Packet refuses output outside .local
Write-Output '--- Test 1: Packet refuses output outside .local ---'
$t1exit = Invoke-Packet -OutDir $badPath
Assert-True ($t1exit -ne 0) 'Test1: packet output outside .local exits nonzero'

# Run packet
Write-Output ''
Write-Output '--- Running packet (Tests 2-19) ---'
$packetOut = Join-Path $testBase '.local\packet'
$t2exit    = Invoke-Packet -OutDir $packetOut

# Test 2: Packet writes all required files
$reqFiles = @(
    'MAP_7O_DRUMAP_ALIGNED_PACKET.md',
    'MAP_7O_EXPERIMENT_I_MANUAL_INSTALL_COMMANDS.md',
    'MAP_7O_EXPERIMENT_I_LOG_CAPTURE_COMMANDS.md',
    'MAP_7O_EXPERIMENT_I_MANUAL_RESULT.local-template.md',
    'MAP_7O_DRUMAP_COMPARISON_SUMMARY.md',
    'map7o-drumap-aligned-preflight.json',
    'map7o-drumap-aligned-preflight.md',
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

$expIBase   = Join-Path $packetOut "experiment-i-candidate\$mapId"
$expI42Dir  = Join-Path $expIBase '42'
$expICommon = Join-Path $expIBase 'common'
$expICommonMaps = Join-Path $expICommon "media\maps\$mapId"
$mapInfoPath = Join-Path $expICommonMaps 'map.info'

# Test 3: Root mod.info exists
Write-Output ''
Write-Output '--- Test 3: Root mod.info exists ---'
Assert-True (Test-Path -LiteralPath (Join-Path $expIBase 'mod.info')) `
    'Test3: experiment-I has root mod.info'

# Test 4: 42/mod.info exists
Write-Output ''
Write-Output '--- Test 4: 42/mod.info exists ---'
Assert-True (Test-Path -LiteralPath (Join-Path $expI42Dir 'mod.info')) `
    'Test4: experiment-I has 42/mod.info'

# Test 5: NO common/mod.info
Write-Output ''
Write-Output '--- Test 5: NO common/mod.info ---'
Assert-True (-not (Test-Path -LiteralPath (Join-Path $expICommon 'mod.info'))) `
    'Test5: experiment-I has NO common/mod.info'

# Test 6: common/media/maps/<MapId>/map.info exists
Write-Output ''
Write-Output '--- Test 6: common/media/maps/map.info exists ---'
Assert-True (Test-Path -LiteralPath $mapInfoPath) `
    'Test6: experiment-I has common/media/maps/map.info'

# Test 7: map.info has lots=NONE
Write-Output ''
Write-Output '--- Test 7: map.info lots=NONE ---'
$mapContent = if (Test-Path -LiteralPath $mapInfoPath) {
    Get-Content $mapInfoPath -Raw
} else { '' }
Assert-True ($mapContent -match '(?m)^lots=NONE') `
    'Test7: map.info contains lots=NONE'

# Test 8: map.info has zoomX
Write-Output ''
Write-Output '--- Test 8: map.info has zoomX ---'
Assert-True ($mapContent -match '(?m)^zoomX=') `
    'Test8: map.info contains zoomX='

# Test 9: map.info has zoomY
Write-Output ''
Write-Output '--- Test 9: map.info has zoomY ---'
Assert-True ($mapContent -match '(?m)^zoomY=') `
    'Test9: map.info contains zoomY='

# Test 10: map.info has zoomS
Write-Output ''
Write-Output '--- Test 10: map.info has zoomS ---'
Assert-True ($mapContent -match '(?m)^zoomS=') `
    'Test10: map.info contains zoomS='

# Test 11: root mod.info no-BOM
Write-Output ''
Write-Output '--- Test 11: root mod.info no-BOM ---'
Assert-True (-not (Test-HasBom (Join-Path $expIBase 'mod.info'))) `
    'Test11: root mod.info no-BOM'

# Test 12: 42/mod.info no-BOM
Write-Output ''
Write-Output '--- Test 12: 42/mod.info no-BOM ---'
Assert-True (-not (Test-HasBom (Join-Path $expI42Dir 'mod.info'))) `
    'Test12: 42/mod.info no-BOM'

# Test 13: map.info, spawnpoints, objects no-BOM
Write-Output ''
Write-Output '--- Test 13: common text files no-BOM ---'
$mapNoBom   = -not (Test-HasBom $mapInfoPath)
$spawnNoBom = -not (Test-HasBom (Join-Path $expICommonMaps 'spawnpoints.lua'))
$objNoBom   = -not (Test-HasBom (Join-Path $expICommonMaps 'objects.lua'))
Assert-True ($mapNoBom -and $spawnNoBom -and $objNoBom) `
    "Test13: map.info=$mapNoBom spawnpoints=$spawnNoBom objects=$objNoBom all no-BOM"

# Test 14: LOTH/LOTP/chunkdata sizes unchanged
Write-Output ''
Write-Output '--- Test 14: Binary sizes unchanged ---'
$loth  = if (Test-Path (Join-Path $expICommonMaps '0_0.lotheader'))    { (Get-Item (Join-Path $expICommonMaps '0_0.lotheader')).Length    } else { 0 }
$lotp  = if (Test-Path (Join-Path $expICommonMaps 'world_0_0.lotpack')) { (Get-Item (Join-Path $expICommonMaps 'world_0_0.lotpack')).Length } else { 0 }
$chunk = if (Test-Path (Join-Path $expICommonMaps 'chunkdata_0_0.bin')) { (Get-Item (Join-Path $expICommonMaps 'chunkdata_0_0.bin')).Length } else { 0 }
Assert-True ($loth -eq 29646 -and $lotp -eq 1056780 -and $chunk -eq 1026) `
    "Test14: LOTH=29646($loth) LOTP=1056780($lotp) chunk=1026($chunk)"

# Test 15: Preflight records drumap_aligned_layout=true
Write-Output ''
Write-Output '--- Test 15: Preflight drumap_aligned_layout ---'
$pfl = if (Test-Path (Join-Path $packetOut 'map7o-drumap-aligned-preflight.json')) {
    Get-Content (Join-Path $packetOut 'map7o-drumap-aligned-preflight.json') -Raw | ConvertFrom-Json
} else { $null }
$druAligned = if ($null -ne $pfl) { [bool]$pfl.drumap_aligned_layout } else { $false }
Assert-True ($druAligned -eq $true) 'Test15: preflight drumap_aligned_layout == true'

# Test 16: Preflight public_playable_claim_allowed=false
Write-Output ''
Write-Output '--- Test 16: Preflight public_playable_claim_allowed=false ---'
Assert-True ($null -ne $pfl -and [bool]$pfl.public_playable_claim_allowed -eq $false) `
    'Test16: preflight public_playable_claim_allowed == false'

# Test 17: Docs contain HUMAN-ONLY
Write-Output ''
Write-Output '--- Test 17: Docs contain HUMAN-ONLY ---'
$installContent = if (Test-Path (Join-Path $packetOut 'MAP_7O_EXPERIMENT_I_MANUAL_INSTALL_COMMANDS.md')) {
    Get-Content (Join-Path $packetOut 'MAP_7O_EXPERIMENT_I_MANUAL_INSTALL_COMMANDS.md') -Raw
} else { '' }
Assert-True ($installContent -match 'HUMAN-ONLY') 'Test17: install commands contain HUMAN-ONLY'

# Test 18: Docs contain MAP7F_VARIANT_I_MAP_FOLDER_SCAN_EMPTY
Write-Output ''
Write-Output '--- Test 18: Docs contain MAP7F_VARIANT_I_MAP_FOLDER_SCAN_EMPTY ---'
$packetContent = if (Test-Path (Join-Path $packetOut 'MAP_7O_DRUMAP_ALIGNED_PACKET.md')) {
    Get-Content (Join-Path $packetOut 'MAP_7O_DRUMAP_ALIGNED_PACKET.md') -Raw
} else { '' }
Assert-True ($packetContent -match 'MAP7F_VARIANT_I_MAP_FOLDER_SCAN_EMPTY') `
    'Test18: packet contains MAP7F_VARIANT_I_MAP_FOLDER_SCAN_EMPTY'

# Test 19: Docs contain MAP_FOLDER_SCAN_FOUND_BUT_LOTHEADER_FILES_MISSING
Write-Output ''
Write-Output '--- Test 19: Docs contain MAP_FOLDER_SCAN_FOUND_BUT_LOTHEADER ---'
Assert-True ($packetContent -match 'MAP_FOLDER_SCAN_FOUND_BUT_LOTHEADER_FILES_MISSING') `
    'Test19: packet contains MAP_FOLDER_SCAN_FOUND_BUT_LOTHEADER_FILES_MISSING'

Write-Output ''
Write-Output '----------------------------------------'
Write-Output "Results: $pass passed, $fail failed"
Write-Output '----------------------------------------'

if ($fail -gt 0) { exit 1 }
exit 0
