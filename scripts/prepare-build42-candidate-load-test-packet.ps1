#Requires -Version 5.1
<#
.SYNOPSIS
    Prepares a local-only Build 42 candidate load-test packet from MAP-6L output (MAP-6M).

    Validates the MAP-6L candidate source, runs a binary preflight, and writes:
      BUILD42_CANDIDATE_LOAD_TEST_PACKET.md
      BUILD42_CANDIDATE_LOAD_TEST_RECORD.local-template.md
      BUILD42_CANDIDATE_PREFLIGHT.json
      pzmapforge_candidate_spawnregions.lua
      INSTALL_COPY_COMMANDS_README.txt

    Does NOT copy mod files to any PZ mods or Workshop folder.
    Does NOT touch the PZ install directory.
    Does NOT touch repo media/maps.
    Does NOT copy PZ assets.
    Does NOT perform a load test.
    Does NOT claim playable export.
    Both -Source and -Output must be under .local only.

Usage:
    .\scripts\prepare-build42-candidate-load-test-packet.ps1 `
        -Source  ".local\...\<map_id>_build42_candidate" `
        -Output  ".local\...\<packet_dir>" `
        [-ServerName PZMF_B42_CANDIDATE_TEST_001] `
        [-ModFolderName <name>]
#>

param(
    [Parameter(Mandatory=$true)]  [string]$Source,
    [Parameter(Mandatory=$true)]  [string]$Output,
    [string]$ServerName    = 'PZMF_B42_CANDIDATE_TEST_001',
    [string]$ModFolderName = ''
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

Write-Output 'prepare-build42-candidate-load-test-packet.ps1'
Write-Output "Source:      $Source"
Write-Output "Output:      $Output"
Write-Output "ServerName:  $ServerName"
Write-Output ''

# ---------------------------------------------------------------------------
# Guards: .local only
# ---------------------------------------------------------------------------

$sep         = [System.IO.Path]::DirectorySeparatorChar
$localMarker = $sep + '.local' + $sep

function Test-IsUnderLocal { param([string]$P)
    return ($P.Contains($localMarker) -or $P.EndsWith($sep + '.local'))
}

$sourceFull = [System.IO.Path]::GetFullPath($Source)
$outputFull = [System.IO.Path]::GetFullPath($Output)

if (-not (Test-IsUnderLocal $sourceFull)) {
    Write-Error "prepare-build42-candidate-load-test-packet: -Source must be under .local/: $sourceFull"
    exit 1
}
if (-not (Test-IsUnderLocal $outputFull)) {
    Write-Error "prepare-build42-candidate-load-test-packet: -Output must be under .local/: $outputFull"
    exit 1
}

$forbiddenPatterns = @(
    'Zomboid'+$sep+'mods','Zomboid'+$sep+'Workshop','Zomboid'+$sep+'Server',
    'steamapps'+$sep+'common'+$sep+'ProjectZomboid','steamapps'+$sep+'common'+$sep+'Project Zomboid'
)
foreach ($pat in $forbiddenPatterns) {
    if ($sourceFull -match [regex]::Escape($pat)) {
        Write-Error "prepare-build42-candidate-load-test-packet: forbidden path: $sourceFull"; exit 1
    }
}

if (-not (Test-Path -LiteralPath $sourceFull)) {
    Write-Error "Source not found: $sourceFull"; exit 1
}

# ---------------------------------------------------------------------------
# Read MAP-6L report JSON
# ---------------------------------------------------------------------------

Write-Output '--- Reading MAP-6L candidate report ---'

$reportPath = Join-Path $sourceFull ('42' + $sep + 'experimental-map-export-report.json')
if (-not (Test-Path -LiteralPath $reportPath)) {
    Write-Error "MAP-6L report not found: $reportPath"; exit 1
}

$report  = Get-Content $reportPath -Raw | ConvertFrom-Json
$mapId   = [string]$report.map_id
$profile = [string]$report.build42_candidate_profile

if ([string]::IsNullOrEmpty($ModFolderName)) {
    $ModFolderName = $mapId + '_test'
}

Write-Output "  map_id:   $mapId"
Write-Output "  profile:  $profile"

$v42Dir     = Join-Path $sourceFull '42'
$mapDataDir = Join-Path $v42Dir ('media' + $sep + 'maps' + $sep + $mapId)
$lothPath   = Join-Path $mapDataDir '0_0.lotheader'
$lotpPath   = Join-Path $mapDataDir 'world_0_0.lotpack'
$cdataPath  = Join-Path $mapDataDir 'chunkdata_0_0.bin'
$destPath   = "C:\Users\Palmacede\Zomboid\mods\$ModFolderName\42\"

# ---------------------------------------------------------------------------
# Preflight checks
# ---------------------------------------------------------------------------

Write-Output ''
Write-Output '--- Running preflight checks ---'

$checks  = [System.Collections.Generic.List[object]]::new()
$allPass = $true

function Add-Check {
    param([string]$Id, [bool]$Pass, [string]$Detail)
    $script:checks.Add([ordered]@{ id=$Id; pass=$Pass; detail=$Detail })
    if (-not $Pass) { $script:allPass = $false }
    $status = if ($Pass) { 'PASS' } else { 'FAIL' }
    Write-Output "  $status  $Id  $Detail"
}

Add-Check 'report_schema_valid'            ($report.schema -eq 'pzmapforge.build42-candidate-report.v0.1') "schema=$($report.schema)"
Add-Check 'build42_candidate_writer_true'  ($report.build42_candidate_writer -eq $true)    "value=$($report.build42_candidate_writer)"
Add-Check 'profile_empty_grass_v0'         ($report.build42_candidate_profile -eq 'empty_grass_v0') "profile=$($report.build42_candidate_profile)"
Add-Check 'writer_implemented_true'        ($report.writer_implemented -eq $true)          "value=$($report.writer_implemented)"
Add-Check 'writer_scope_candidate_only'    ($report.writer_scope -eq 'candidate_only_not_load_tested') "scope=$($report.writer_scope)"
Add-Check 'load_tested_false'              ($report.load_tested -eq $false)                "value=$($report.load_tested)"
Add-Check 'playable_export_generated_false'($report.playable_export_generated -eq $false)  "value=$($report.playable_export_generated)"
Add-Check 'playable_export_claimed_false'  ($report.playable_export_claimed -eq $false)    "value=$($report.playable_export_claimed)"
Add-Check 'pz_assets_copied_false'         ($report.pz_assets_copied -eq $false)           "value=$($report.pz_assets_copied)"
Add-Check 'pz_assets_read_false'           ($report.pz_assets_read -eq $false)             "value=$($report.pz_assets_read)"
Add-Check 'media_maps_repo_clean'          ($report.media_maps_touched_in_repo -eq $false) "value=$($report.media_maps_touched_in_repo)"

# Required files
foreach ($rel in @(
    '42/mod.info',
    '42/experimental-map-export-report.json',
    "42/media/maps/$mapId/map.info",
    "42/media/maps/$mapId/spawnpoints.lua",
    "42/media/maps/$mapId/objects.lua",
    "42/media/maps/$mapId/0_0.lotheader",
    "42/media/maps/$mapId/world_0_0.lotpack",
    "42/media/maps/$mapId/chunkdata_0_0.bin"
)) {
    $fp = Join-Path $sourceFull ($rel.Replace('/', $sep))
    $cid = 'file_exists_' + $rel.Replace('/','_').Replace('.','_')
    Add-Check $cid (Test-Path -LiteralPath $fp) $rel
}

# LOTH binary signature
if (Test-Path -LiteralPath $lothPath) {
    $lothBytes = [System.IO.File]::ReadAllBytes($lothPath)
    Add-Check 'loth_magic_correct'    ($lothBytes.Length -ge 4 -and $lothBytes[0] -eq 0x4C -and $lothBytes[1] -eq 0x4F -and $lothBytes[2] -eq 0x54 -and $lothBytes[3] -eq 0x48) '4C4F5448=LOTH'
    $lothVer = if ($lothBytes.Length -ge 8) { [System.BitConverter]::ToUInt32($lothBytes, 4) } else { 0 }
    Add-Check 'loth_version_1'        ($lothVer -eq 1) "version=$lothVer"
    $lothCount = if ($lothBytes.Length -ge 12) { [System.BitConverter]::ToUInt32($lothBytes, 8) } else { 0 }
    Add-Check 'loth_entry_count_gte1' ($lothCount -ge 1) "entry_count=$lothCount"
} else {
    Add-Check 'loth_magic_correct' $false 'file missing'
    Add-Check 'loth_version_1'     $false 'file missing'
    Add-Check 'loth_entry_count_gte1' $false 'file missing'
}

# LOTP binary signature
if (Test-Path -LiteralPath $lotpPath) {
    $lotpInfo  = Get-Item -LiteralPath $lotpPath
    $lotpBytes = [System.IO.File]::ReadAllBytes($lotpPath)
    Add-Check 'lotp_magic_correct'     ($lotpBytes.Length -ge 4 -and $lotpBytes[0] -eq 0x4C -and $lotpBytes[1] -eq 0x4F -and $lotpBytes[2] -eq 0x54 -and $lotpBytes[3] -eq 0x50) '4C4F5450=LOTP'
    $lotpVer   = if ($lotpBytes.Length -ge 8)  { [System.BitConverter]::ToUInt32($lotpBytes, 4) } else { 0 }
    $lotpCount = if ($lotpBytes.Length -ge 12) { [System.BitConverter]::ToUInt32($lotpBytes, 8) } else { 0 }
    $lotpOff1  = if ($lotpBytes.Length -ge 20) { [System.BitConverter]::ToUInt64($lotpBytes, 12) } else { [uint64]0 }
    $lotpOff2  = if ($lotpBytes.Length -ge 28) { [System.BitConverter]::ToUInt64($lotpBytes, 20) } else { [uint64]0 }
    Add-Check 'lotp_version_1'         ($lotpVer -eq 1)      "version=$lotpVer"
    Add-Check 'lotp_chunk_count_1024'  ($lotpCount -eq 1024) "chunk_count=$lotpCount"
    Add-Check 'lotp_first_offset_8204' ($lotpOff1 -eq [uint64]8204) "first_offset=$lotpOff1"
    Add-Check 'lotp_second_offset_9228'($lotpOff2 -eq [uint64]9228) "second_offset=$lotpOff2"
    Add-Check 'lotp_size_1056780'      ($lotpInfo.Length -eq 1056780) "size=$($lotpInfo.Length)"
} else {
    foreach ($c in @('lotp_magic_correct','lotp_version_1','lotp_chunk_count_1024','lotp_first_offset_8204','lotp_second_offset_9228','lotp_size_1056780')) {
        Add-Check $c $false 'file missing'
    }
}

# Chunkdata
if (Test-Path -LiteralPath $cdataPath) {
    $cdataInfo  = Get-Item -LiteralPath $cdataPath
    $cdataBytes = [System.IO.File]::ReadAllBytes($cdataPath)
    Add-Check 'chunkdata_size_1026'   ($cdataInfo.Length -eq 1026) "size=$($cdataInfo.Length)"
    Add-Check 'chunkdata_header_0001' ($cdataBytes.Length -ge 2 -and $cdataBytes[0] -eq 0x00 -and $cdataBytes[1] -eq 0x01) "bytes01=$($cdataBytes[0].ToString('x2'))$($cdataBytes[1].ToString('x2'))"
    $bodyAllZero = $true
    for ($i = 2; $i -lt [Math]::Min($cdataBytes.Length, 1026); $i++) {
        if ($cdataBytes[$i] -ne 0) { $bodyAllZero = $false; break }
    }
    Add-Check 'chunkdata_body_all_zero' $bodyAllZero "all_zero=$bodyAllZero"
} else {
    foreach ($c in @('chunkdata_size_1026','chunkdata_header_0001','chunkdata_body_all_zero')) {
        Add-Check $c $false 'file missing'
    }
}

$passCount = [int]($checks | Where-Object { $_.pass } | Measure-Object).Count
$failCount = [int]($checks | Where-Object { -not $_.pass } | Measure-Object).Count
Write-Output ''
Write-Output "Preflight: $passCount passed, $failCount failed"

if (-not $allPass) {
    Write-Error "prepare-build42-candidate-load-test-packet: preflight FAILED - $failCount check(s) did not pass."
    exit 1
}
Write-Output 'Preflight: ALL PASS'

# ---------------------------------------------------------------------------
# Write output files
# ---------------------------------------------------------------------------

New-Item -ItemType Directory -Force -Path $outputFull | Out-Null
$generatedAt = (Get-Date).ToUniversalTime().ToString('yyyy-MM-ddTHH:mm:ssZ')

# ---- BUILD42_CANDIDATE_PREFLIGHT.json ----
$preflight = [ordered]@{
    schema                       = 'pzmapforge.build42-candidate-preflight.v0.1'
    generated_at_utc             = $generatedAt
    source_path                  = $sourceFull.Replace('\','/')
    map_id                       = $mapId
    profile                      = $profile
    build42_candidate_writer     = [bool]$report.build42_candidate_writer
    load_tested                  = [bool]$report.load_tested
    playable_export_generated    = [bool]$report.playable_export_generated
    playable_export_claimed      = [bool]$report.playable_export_claimed
    all_checks_pass              = $allPass
    pass_count                   = $passCount
    fail_count                   = $failCount
    checks                       = @($checks)
    CANDIDATE_PREFLIGHT_VERIFIED  = $allPass
    MANUAL_LOAD_TEST_REQUIRED     = $true
    LOAD_TEST_NOT_PERFORMED       = $true
    PLAYABLE_EXPORT_CLAIM_ALLOWED = 'false'
}

$preflightPath = Join-Path $outputFull 'BUILD42_CANDIDATE_PREFLIGHT.json'
$preflight | ConvertTo-Json -Depth 6 | Set-Content -Path $preflightPath -Encoding UTF8
Write-Output "Preflight JSON: $preflightPath"

# ---- pzmapforge_candidate_spawnregions.lua ----
$spawnLines = @(
    '-- PZMapForge Build42 Candidate Spawn Regions (MAP-6M)',
    '-- NOT load-tested. NOT a playable export. Candidate only.',
    '-- Operator: place this as server spawnregions.lua if needed.',
    'function SpawnRegions()',
    '    return {',
    "        { name = `"PZMapForge Candidate Cell`", file = `"media/maps/$mapId/spawnpoints.lua`" },",
    '    }',
    'end'
)
$spawnRegionsPath = Join-Path $outputFull 'pzmapforge_candidate_spawnregions.lua'
Set-Content -Path $spawnRegionsPath -Value ($spawnLines -join "`n") -Encoding UTF8
Write-Output "Spawnregions: $spawnRegionsPath"

# ---- BUILD42_CANDIDATE_LOAD_TEST_PACKET.md ----
$packetLines = @(
    '# Build 42 Candidate Load Test Packet',
    '',
    '**CANDIDATE ONLY -- NOT VALIDATED -- NOT A PLAYABLE EXPORT**',
    '',
    "Generated: $generatedAt",
    "Source:    $($sourceFull.Replace('\','/'))",
    "Map ID:    $mapId",
    "Profile:   $profile",
    "Server:    $ServerName",
    '',
    '## Preflight result',
    '',
    "ALL $passCount checks PASSED. See BUILD42_CANDIDATE_PREFLIGHT.json for details.",
    '',
    '## Expected destination (operator manual action only)',
    '',
    '```',
    $destPath,
    '```',
    '',
    'This script did NOT copy any files.',
    'The operator must perform the copy manually.',
    '',
    '## Manual copy (operator only - NOT automated)',
    '',
    'Step 1. Open a command prompt or Explorer.',
    '',
    'Step 2. Create the mod folder:',
    "    mkdir `"$destPath`"",
    '',
    'Step 3. Copy the 42/ versioned layout:',
    "    xcopy /E /I `"$($v42Dir.Replace('\','/').Replace('/','\'))`" `"$destPath`"",
    '',
    'Step 4. Copy pzmapforge_candidate_spawnregions.lua to the server Lua directory if needed.',
    '',
    '## Required server configuration',
    '',
    "1. Launch PZ server or SP with server name: $ServerName",
    '2. In server options, set:',
    "   Mods=$mapId",
    '3. Add spawnregions.lua if PZMapForge Candidate Cell does not appear.',
    '4. Use a CLEAN test save.',
    '',
    '## Test observation checklist',
    '',
    '- [ ] Mod visible in Mods screen',
    '- [ ] Mod can be enabled without error',
    '- [ ] PZ reaches spawn/character screen',
    '- [ ] PZMapForge Candidate Cell visible in spawn list',
    '- [ ] Selection starts loading without immediate crash',
    '- [ ] Player entered the world',
    '- [ ] Player returned to main menu (clean exit)',
    '- [ ] Unexpected error screen appeared',
    '- [ ] Crash to desktop occurred',
    '- [ ] PZ log contains LOTP/LOTH/chunkdata/IsoLot/CellLoader/IsoCell errors',
    '',
    '## Non-claims',
    '',
    '- This packet does not perform a load test.',
    '- This script did NOT copy any files to PZ folders.',
    '- The candidate remains unproven until a successful load test is recorded.',
    '- No playable export claim is made.',
    '- PLAYABLE_EXPORT_CLAIM_ALLOWED=false'
)

$packetPath = Join-Path $outputFull 'BUILD42_CANDIDATE_LOAD_TEST_PACKET.md'
Set-Content -Path $packetPath -Value ($packetLines -join "`n") -Encoding UTF8
Write-Output "Packet MD:  $packetPath"

# ---- BUILD42_CANDIDATE_LOAD_TEST_RECORD.local-template.md ----
$recordLines = @(
    '# Build 42 Candidate Load Test Record (Local Template)',
    '',
    '**Fill in this template after performing the manual load test.**',
    '**This file must remain under .local/ and must not be committed.**',
    '',
    '## Test metadata',
    '',
    '| Field | Value |',
    '|---|---|',
    '| Date | [ fill in ] |',
    '| PZ build | Build 42 |',
    "| Map ID | $mapId |",
    "| Profile | $profile |",
    "| Server | $ServerName |",
    '| Tester | [ fill in ] |',
    '| Notes | [ fill in ] |',
    '',
    '## Observation checklist',
    '',
    '| Observation | Result (YES / NO / N/A) |',
    '|---|---|',
    '| Mod visible in Mods screen | [ ] |',
    '| Mod can be enabled | [ ] |',
    '| PZ reaches spawn screen | [ ] |',
    '| PZMapForge Candidate Cell visible | [ ] |',
    '| Selection starts loading | [ ] |',
    '| Player entered world | [ ] |',
    '| Clean return to menu | [ ] |',
    '| Unexpected error screen | [ ] |',
    '| Crash to desktop | [ ] |',
    '| LOTP/LOTH/IsoLot/CellLoader error in log | [ ] |',
    '',
    '## PZ log evidence',
    '',
    '```',
    '[ paste log lines ]',
    '```',
    '',
    '## Result',
    '',
    'Choose one:',
    '',
    '- [ ] DISCOVERY_FAIL',
    '- [ ] DISCOVERY_PASS',
    '- [ ] SPAWN_REGION_NOT_VISIBLE',
    '- [ ] SPAWN_REGION_VISIBLE',
    '- [ ] LOAD_TEST_PASS',
    '- [ ] LOAD_TEST_FAIL',
    '- [ ] LOAD_TEST_INCONCLUSIVE',
    '',
    '## Narrative',
    '',
    '[ Describe what happened in detail. ]',
    '',
    '## Non-claims',
    '',
    '- This record does not constitute a playable export claim.',
    '- PLAYABLE_EXPORT_CLAIM_ALLOWED=false until reviewed.'
)

$recordPath = Join-Path $outputFull 'BUILD42_CANDIDATE_LOAD_TEST_RECORD.local-template.md'
Set-Content -Path $recordPath -Value ($recordLines -join "`n") -Encoding UTF8
Write-Output "Record tmpl: $recordPath"

# ---- INSTALL_COPY_COMMANDS_README.txt ----
$readmeLines = @(
    'Build 42 Candidate Install Reference',
    '=====================================',
    "Generated: $generatedAt",
    "Map ID:    $mapId",
    "Profile:   $profile",
    '',
    'IMPORTANT: This file is for HUMAN reference only.',
    'The prepare script did NOT copy any files.',
    '',
    "Expected destination: $destPath",
    '',
    'Copy commands:',
    "  mkdir `"$destPath`"",
    "  xcopy /E /I `"$($v42Dir.Replace('\','/').Replace('/','\'))`" `"$destPath`"",
    '',
    'Verify copy:',
    "  dir `"$destPath`"",
    "  dir `"${destPath}media\maps\$mapId`"",
    '',
    'No playable claim. PLAYABLE_EXPORT_CLAIM_ALLOWED=false'
)

$readmePath = Join-Path $outputFull 'INSTALL_COPY_COMMANDS_README.txt'
Set-Content -Path $readmePath -Value ($readmeLines -join "`n") -Encoding UTF8
Write-Output "Install ref: $readmePath"

Write-Output ''
Write-Output "Output: $outputFull"
Write-Output "CANDIDATE_PREFLIGHT_VERIFIED: $allPass"
Write-Output "MANUAL_LOAD_TEST_REQUIRED: true"
Write-Output "LOAD_TEST_NOT_PERFORMED: true"
Write-Output "PLAYABLE_EXPORT_CLAIM_ALLOWED: false"
Write-Output 'Done.'
