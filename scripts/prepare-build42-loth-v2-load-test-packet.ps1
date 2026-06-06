#Requires -Version 5.1
<#
.SYNOPSIS
    MAP-6T: Generates the empty_grass_v1 candidate and produces a human-ready
    load-test packet for the Build 42 LOTH v2 candidate.

    -Output must be under .local/.
    Does NOT copy any files to PZ folders.
    Does NOT write outside .local/.

    Steps:
    1. Generates empty_grass_v1 candidate under Output/candidate/.
    2. Runs 20-point preflight on the generated files.
    3. Writes MAP_6T_LOAD_TEST_PACKET.md, RECORD template, wiring commands,
       map6t-preflight.json, map6t-preflight.md.

.PARAMETER Output
    Path under .local/ for packet output.

.PARAMETER MapId
    Map ID for the candidate. Default: pzmapforge_build42_candidate_v1_001

.PARAMETER ModFolderName
    Destination mod folder name for human install step.
    Default: pzmapforge_build42_candidate_v1_001_test

.PARAMETER ServerName
    PZ server preset name.
    Default: PZMF_B42_LOTH_V2_TEST_001

.EXAMPLE
    powershell -ExecutionPolicy Bypass -File scripts\prepare-build42-loth-v2-load-test-packet.ps1 `
        -Output .local\map6t-packet
#>

param(
    [Parameter(Mandatory=$true)][string]$Output,
    [string]$MapId         = 'pzmapforge_build42_candidate_v1_001',
    [string]$ModFolderName = 'pzmapforge_build42_candidate_v1_001_test',
    [string]$ServerName    = 'PZMF_B42_LOTH_V2_TEST_001'
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot  = Split-Path -Parent $scriptDir

# ---------------------------------------------------------------------------
# Path guard
# ---------------------------------------------------------------------------

function Assert-LocalPath {
    param([string]$Path, [string]$Label)
    $norm = $Path.Replace('/', '\')
    if ($norm -notmatch '\\\.local(\\|$)') {
        Write-Error "$Label must be under .local/. Got: $Path"
        exit 1
    }
}

Assert-LocalPath $Output '-Output'

New-Item -ItemType Directory -Force -Path $Output | Out-Null

# ---------------------------------------------------------------------------
# Step 1: Generate empty_grass_v1 candidate
# ---------------------------------------------------------------------------

$candidateOut = Join-Path $Output 'candidate'
Write-Output "Generating empty_grass_v1 candidate..."

& dotnet run --project (Join-Path $repoRoot 'src\PZMapForge.Cli') `
    --configuration Release --no-build `
    -- map-export-experimental `
    --map-id $MapId `
    --output $candidateOut `
    --build42-candidate-writer `
    --build42-candidate-profile empty_grass_v1

if ($LASTEXITCODE -ne 0) {
    Write-Error "CLI candidate generation failed (exit $LASTEXITCODE)"
    exit 1
}

# ---------------------------------------------------------------------------
# Paths to generated files
# ---------------------------------------------------------------------------

$candDir    = Join-Path $candidateOut ($MapId + '_build42_candidate')
$v42Dir     = Join-Path $candDir '42'
$mapDataDir = Join-Path $v42Dir "media\maps\$MapId"
$reportJson = Join-Path $v42Dir 'experimental-map-export-report.json'

$modInfoPath    = Join-Path $v42Dir     'mod.info'
$mapInfoPath    = Join-Path $mapDataDir 'map.info'
$spawnPtsPath   = Join-Path $mapDataDir 'spawnpoints.lua'
$objectsPath    = Join-Path $mapDataDir 'objects.lua'
$lotheaderPath  = Join-Path $mapDataDir '0_0.lotheader'
$lotpackPath    = Join-Path $mapDataDir 'world_0_0.lotpack'
$chunkdataPath  = Join-Path $mapDataDir 'chunkdata_0_0.bin'

# ---------------------------------------------------------------------------
# Step 2: Preflight checks
# ---------------------------------------------------------------------------

Write-Output "Running preflight checks..."

$checks  = [System.Collections.Generic.List[object]]::new()
$allPass = $true

function Add-Check {
    param([string]$Name, [bool]$Pass, [string]$Detail)
    $r = if ($Pass) { 'PASS' } else { 'FAIL' }
    $checks.Add([ordered]@{ name = $Name; result = $r; detail = $Detail }) | Out-Null
    if (-not $Pass) { $script:allPass = $false }
    Write-Output "  [$r] $Name -- $Detail"
}

# File existence checks
Add-Check 'mod_info_exists'         (Test-Path $modInfoPath)    $modInfoPath
Add-Check 'map_info_exists'         (Test-Path $mapInfoPath)    $mapInfoPath
Add-Check 'spawnpoints_lua_exists'  (Test-Path $spawnPtsPath)   $spawnPtsPath
Add-Check 'objects_lua_exists'      (Test-Path $objectsPath)    $objectsPath
Add-Check 'lotheader_exists'        (Test-Path $lotheaderPath)  $lotheaderPath
Add-Check 'lotpack_exists'          (Test-Path $lotpackPath)    $lotpackPath
Add-Check 'chunkdata_exists'        (Test-Path $chunkdataPath)  $chunkdataPath

# LOTH binary checks
if (Test-Path $lotheaderPath) {
    $loth = [System.IO.File]::ReadAllBytes($lotheaderPath)

    $magic    = if ($loth.Length -ge 4) { [System.Text.Encoding]::ASCII.GetString($loth[0..3]) } else { '' }
    $ver      = if ($loth.Length -ge 8)  { [BitConverter]::ToUInt32($loth, 4) } else { 0 }
    $cnt      = if ($loth.Length -ge 12) { [BitConverter]::ToUInt32($loth, 8) } else { 0 }
    $lothSize = $loth.Length

    Add-Check 'loth_magic_is_LOTH'    ($magic -eq 'LOTH')  "magic=$magic"
    Add-Check 'loth_version_is_1'     ($ver   -eq 1)       "version=$ver"
    Add-Check 'loth_entry_count_1024' ($cnt   -eq 1024)    "entry_count=$cnt"

    # First and last entry
    $text    = [System.Text.Encoding]::ASCII.GetString($loth, 12, $loth.Length - 12)
    $entries = @($text -split "`n" | Where-Object { $_.Length -gt 0 })
    $first   = if ($entries.Count -gt 0)    { $entries[0]           } else { '' }
    $last    = if ($entries.Count -gt 0)    { $entries[$entries.Count-1] } else { '' }
    Add-Check 'loth_first_entry_correct' ($first -eq 'blends_grassoverlays_01_0')    "first=$first"
    Add-Check 'loth_last_entry_correct'  ($last  -eq 'blends_grassoverlays_01_1023') "last=$last"
    Add-Check 'loth_size_28598'          ($lothSize -eq 28598)  "size=$lothSize"
} else {
    foreach ($n in @('loth_magic_is_LOTH','loth_version_is_1','loth_entry_count_1024',
                     'loth_first_entry_correct','loth_last_entry_correct','loth_size_28598')) {
        Add-Check $n $false 'lotheader file missing'
    }
}

# LOTP size check
if (Test-Path $lotpackPath) {
    $lotpSize = (Get-Item $lotpackPath).Length
    Add-Check 'lotp_size_1056780' ($lotpSize -eq 1056780) "size=$lotpSize"
} else {
    Add-Check 'lotp_size_1056780' $false 'lotpack file missing'
}

# chunkdata size check
if (Test-Path $chunkdataPath) {
    $cdSize = (Get-Item $chunkdataPath).Length
    Add-Check 'chunkdata_size_1026' ($cdSize -eq 1026) "size=$cdSize"
} else {
    Add-Check 'chunkdata_size_1026' $false 'chunkdata file missing'
}

# Report safety flags
if (Test-Path $reportJson) {
    $rep = Get-Content $reportJson -Raw | ConvertFrom-Json
    Add-Check 'report_load_tested_false'             ($rep.load_tested           -eq $false) 'load_tested=false'
    Add-Check 'report_playable_export_generated_false' ($rep.playable_export_generated -eq $false) 'playable_export_generated=false'
    Add-Check 'report_playable_export_claimed_false' ($rep.playable_export_claimed   -eq $false) 'playable_export_claimed=false'
    Add-Check 'report_pz_assets_copied_false'        ($rep.pz_assets_copied          -eq $false) 'pz_assets_copied=false'
    Add-Check 'report_pz_assets_read_false'          ($rep.pz_assets_read            -eq $false) 'pz_assets_read=false'
} else {
    foreach ($n in @('report_load_tested_false','report_playable_export_generated_false',
                     'report_playable_export_claimed_false','report_pz_assets_copied_false',
                     'report_pz_assets_read_false')) {
        Add-Check $n $false 'report.json missing'
    }
}

$passCount = (@($checks | Where-Object { $_.result -eq 'PASS' })).Count
$failCount = (@($checks | Where-Object { $_.result -eq 'FAIL' })).Count
Write-Output ""
Write-Output "Preflight: $passCount PASS / $failCount FAIL"

if (-not $allPass) {
    Write-Error "Preflight failed ($failCount checks). Packet not written."
    exit 1
}

# ---------------------------------------------------------------------------
# Write preflight JSON
# ---------------------------------------------------------------------------

$preflight = [ordered]@{
    schema            = 'pzmapforge.map6t-preflight.v0.1'
    map_id            = $MapId
    mod_folder_name   = $ModFolderName
    server_name       = $ServerName
    candidate_profile = 'empty_grass_v1'
    all_pass          = $allPass
    entry_count       = 1024
    loth_size         = 28598
    lotp_size         = 1056780
    chunkdata_size    = 1026
    checks            = [object[]]$checks.ToArray()
    load_test_not_performed  = $true
    pz_assets_copied         = $false
    playable_export_claimed  = $false
    writer_not_changed       = $true
}

$preflightJsonPath = Join-Path $Output 'map6t-preflight.json'
$preflight | ConvertTo-Json -Depth 4 | Set-Content -Path $preflightJsonPath -Encoding UTF8
Write-Output "Preflight JSON: $preflightJsonPath"

# ---------------------------------------------------------------------------
# Fence for markdown here-strings
# ---------------------------------------------------------------------------
$fence = '```'

# ---------------------------------------------------------------------------
# Write preflight MD
# ---------------------------------------------------------------------------

$preflightMdPath = Join-Path $Output 'map6t-preflight.md'
$checkTable = ($checks | ForEach-Object { "| $($_.name) | $($_.result) | $($_.detail) |" }) -join "`n"
$preflightMd = @"
# MAP-6T Preflight Report

${fence}text
candidate_profile: empty_grass_v1
map_id: $MapId
all_pass: $($allPass.ToString().ToLower())
entry_count: 1024
loth_size: 28598
lotp_size: 1056780
chunkdata_size: 1026
LOAD_TEST_NOT_PERFORMED
PLAYABLE_EXPORT_CLAIM_ALLOWED=false
${fence}

## Checks ($passCount PASS / $failCount FAIL)

| Check | Result | Detail |
|---|---|---|
$checkTable
"@
Set-Content -Path $preflightMdPath -Value $preflightMd -Encoding ASCII
Write-Output "Preflight MD:   $preflightMdPath"

# ---------------------------------------------------------------------------
# Dest paths (for docs -- human action only)
# ---------------------------------------------------------------------------

$destBase   = "C:\Users\Palmacede\Zomboid\mods\$ModFolderName\42"
$serverDir  = 'C:\Users\Palmacede\Zomboid\Server'
$iniPath    = "$serverDir\$ServerName.ini"
$srPath     = "$serverDir\${ServerName}_spawnregions.lua"

# ---------------------------------------------------------------------------
# Write MAP_6T_LOAD_TEST_PACKET.md
# ---------------------------------------------------------------------------

$packetMdPath = Join-Path $Output 'MAP_6T_LOAD_TEST_PACKET.md'
$packetMd = @"
# MAP-6T Build 42 LOTH v2 Load Test Packet

${fence}text
Profile:       empty_grass_v1
MapId:         $MapId
ModFolder:     $ModFolderName
ServerName:    $ServerName
HUMAN_ONLY_COPY_REQUIRED
LOAD_TEST_NOT_PERFORMED
PLAYABLE_EXPORT_CLAIM_ALLOWED=false
${fence}

## Preflight status

All 20 preflight checks PASS. Candidate is ready for manual testing.

- LOTH size:      28598 bytes (vs v0: 38 bytes; reference minimum: 34920 bytes)
- entry_count:    1024
- first entry:    blends_grassoverlays_01_0
- last entry:     blends_grassoverlays_01_1023
- LOTP size:      1056780 bytes
- chunkdata size: 1026 bytes

---

## Step 1: Remove previous test mod folders (HUMAN ONLY)

${fence}
HUMAN-ONLY: Remove-Item -Recurse -Force 'C:\Users\Palmacede\Zomboid\mods\pzmapforge_build42_candidate_001_test_clean' -ErrorAction SilentlyContinue
HUMAN-ONLY: Remove-Item -Recurse -Force 'C:\Users\Palmacede\Zomboid\mods\pzmapforge_manual_b42_001_maptest_a' -ErrorAction SilentlyContinue
HUMAN-ONLY: Remove-Item -Recurse -Force 'C:\Users\Palmacede\Zomboid\mods\$ModFolderName' -ErrorAction SilentlyContinue
${fence}

---

## Step 2: Copy v1 candidate to PZ mods (HUMAN ONLY)

SOURCE (under .local -- do not modify):
  $v42Dir

DESTINATION:
  $destBase

${fence}
HUMAN-ONLY: Copy-Item -Recurse -Force '$v42Dir' '$($destBase | Split-Path -Parent)'
${fence}

After copy, verify (HUMAN-ONLY):
${fence}powershell
Test-Path '$destBase\mod.info'
Test-Path '$destBase\media\maps\$MapId\0_0.lotheader'
Test-Path '$destBase\media\maps\$MapId\world_0_0.lotpack'
Test-Path '$destBase\media\maps\$MapId\chunkdata_0_0.bin'
${fence}
All must return True.

---

## Step 3: Create or update server preset (HUMAN ONLY)

See MAP_6T_INSTALL_AND_SERVER_WIRING_COMMANDS.md for full wiring details.

Required in server ini ($iniPath):
${fence}
Mods=$MapId
Map=$MapId;Muldraugh, KY
${fence}

Required server spawnregions file ($srPath):
${fence}lua
function SpawnRegions()
    return {
        {name="PZMapForge v1 Candidate Cell", file="media/maps/$MapId/spawnpoints.lua"},
    }
end
${fence}

---

## Step 4: Delete stale console.txt (HUMAN ONLY)

${fence}
HUMAN-ONLY: Remove-Item -Force 'C:\Users\Palmacede\Zomboid\console.txt' -ErrorAction SilentlyContinue
${fence}

---

## Step 5: Run the test (HUMAN ONLY)

1. Launch Project Zomboid Build 42.
2. Enable only the $MapId mod.
3. Navigate to Host and select $ServerName.
4. Record observations in MAP_6T_LOAD_TEST_RECORD.local-template.md.

Key questions to answer:
- Does mod selection crash? -> MOD_SELECTION_CRASH
- Is the v1 candidate spawn region visible? -> CANDIDATE_SPAWN_REGION_VISIBLE
- Does world loading start? -> WORLD_LOAD_STARTED
- Is there an IsoLot/lotheader exception? -> LOTH_ERROR
- Is there a LOTP/lotpack exception? -> LOTP_ERROR

---

## Step 6: Capture fresh log (HUMAN ONLY)

Immediately after the test:
${fence}
HUMAN-ONLY: Copy-Item 'C:\Users\Palmacede\Zomboid\console.txt' '$Output\logs\console-map6t-<TIMESTAMP>.txt'
${fence}

---

## Step 7: Run triage (PZMapForge tool -- safe)

${fence}powershell
powershell -ExecutionPolicy Bypass -File scripts\extract-map6n-current-candidate-log-evidence.ps1 ``
    -InputLogFolder '$Output\logs' ``
    -Output '$Output\triage'
${fence}

---

## Safety

- HUMAN_ONLY_COPY_REQUIRED: no automatic copy to PZ folders.
- LOAD_TEST_NOT_PERFORMED: this packet does not execute the test.
- PLAYABLE_EXPORT_CLAIM_ALLOWED=false: binding.
- WRITER_NOT_CHANGED: no writer change in MAP-6T.
"@
Set-Content -Path $packetMdPath -Value $packetMd -Encoding ASCII
Write-Output "Packet MD: $packetMdPath"

# ---------------------------------------------------------------------------
# Write MAP_6T_LOAD_TEST_RECORD.local-template.md
# ---------------------------------------------------------------------------

$recordMdPath = Join-Path $Output 'MAP_6T_LOAD_TEST_RECORD.local-template.md'
$recordMd = @"
# MAP-6T Load Test Record

${fence}text
Profile:       empty_grass_v1
MapId:         $MapId
ModFolder:     $ModFolderName
ServerName:    $ServerName
Date:          [FILL IN]
PZ version:    [FILL IN e.g. Build 42.0.4]
Operator:      [FILL IN]
LOAD_TEST_INCONCLUSIVE -- default status until filled in
PLAYABLE_EXPORT_CLAIM_ALLOWED=false
${fence}

## Observations

- mod_selection_crash:              [ yes / no ]
- spawn_screen_reached:             [ yes / no ]
- candidate_spawn_region_visible:   [ yes / no ]
- world_load_started:               [ yes / no ]
- entered_world:                    [ yes / no ]
- returned_to_menu:                 [ yes / no ]
- crash_to_desktop:                 [ yes / no ]
- first_error_message:              [FILL IN or none]

## Triage results

- candidate_specific_exception_found: [ yes / no ]
- loth_error_found:                   [ yes / no ]
  - (IsoLot.readInt or lotheader exception in log)
- lotp_error_found:                   [ yes / no ]
  - (lotpack or IsoMetaGrid LOTP exception in log)
- chunkdata_error_found:              [ yes / no ]
  - (chunkdata or chunk exception in log)

## Final result

- result: [ LOAD_TEST_PASS / LOAD_TEST_FAIL_LOTH / LOAD_TEST_FAIL_LOTP / LOAD_TEST_FAIL_CHUNKDATA / LOAD_TEST_INCONCLUSIVE ]

Notes:
[FILL IN]

---

LOAD_TEST_INCONCLUSIVE -- status until this template is completed with evidence.
PLAYABLE_EXPORT_CLAIM_ALLOWED=false
"@
Set-Content -Path $recordMdPath -Value $recordMd -Encoding ASCII
Write-Output "Record template: $recordMdPath"

# ---------------------------------------------------------------------------
# Write MAP_6T_INSTALL_AND_SERVER_WIRING_COMMANDS.md
# ---------------------------------------------------------------------------

$wiringMdPath = Join-Path $Output 'MAP_6T_INSTALL_AND_SERVER_WIRING_COMMANDS.md'
$wiringMd = @"
# MAP-6T Install and Server Wiring Commands

All commands marked HUMAN-ONLY must be run manually.
Do not automate these steps.

${fence}text
MapId:         $MapId
ModFolder:     $ModFolderName
ServerName:    $ServerName
HUMAN_ONLY_COPY_REQUIRED
LOAD_TEST_NOT_PERFORMED
PLAYABLE_EXPORT_CLAIM_ALLOWED=false
${fence}

---

## 1. Remove old test mod folders (HUMAN ONLY)

${fence}
HUMAN-ONLY: Remove-Item -Recurse -Force 'C:\Users\Palmacede\Zomboid\mods\pzmapforge_build42_candidate_001_test_clean' -ErrorAction SilentlyContinue
HUMAN-ONLY: Remove-Item -Recurse -Force 'C:\Users\Palmacede\Zomboid\mods\pzmapforge_manual_b42_001_maptest_a' -ErrorAction SilentlyContinue
HUMAN-ONLY: Remove-Item -Recurse -Force 'C:\Users\Palmacede\Zomboid\mods\$ModFolderName' -ErrorAction SilentlyContinue
${fence}

---

## 2. Copy candidate to PZ mods (HUMAN ONLY)

${fence}
HUMAN-ONLY: Copy-Item -Recurse -Force '$v42Dir' '$($destBase | Split-Path -Parent)'
${fence}

Verify install:
${fence}powershell
Test-Path '$destBase\mod.info'
Test-Path '$destBase\media\maps\$MapId\0_0.lotheader'
Test-Path '$destBase\media\maps\$MapId\world_0_0.lotpack'
Test-Path '$destBase\media\maps\$MapId\chunkdata_0_0.bin'
${fence}

---

## 3. Create server preset ini (HUMAN ONLY)

Check if $iniPath exists:
${fence}powershell
Test-Path '$iniPath'
${fence}

If missing: launch PZ, go to Host, create server preset named $ServerName, exit.

Then inspect and update Mods= and Map= lines:
${fence}powershell
Get-Content '$iniPath' | Select-String '^Mods=|^Map=|^WorkshopItems='
${fence}

Required values:
${fence}text
Mods=$MapId
Map=$MapId;Muldraugh, KY
${fence}

Edit the ini file manually to set these lines (HUMAN ONLY).

---

## 4. Create server spawnregions file (HUMAN ONLY)

${fence}
HUMAN-ONLY: Set-Content -Path '$srPath' -Value @'
function SpawnRegions()
    return {
        {name="PZMapForge v1 Candidate Cell", file="media/maps/$MapId/spawnpoints.lua"},
    }
end
'@
${fence}

Verify:
${fence}powershell
Get-Content '$srPath'
${fence}

---

## 5. Delete stale console.txt before test (HUMAN ONLY)

${fence}
HUMAN-ONLY: Remove-Item -Force 'C:\Users\Palmacede\Zomboid\console.txt' -ErrorAction SilentlyContinue
${fence}

---

## 6. Run log triage after test (safe -- reads .local only)

${fence}powershell
powershell -ExecutionPolicy Bypass -File scripts\extract-map6n-current-candidate-log-evidence.ps1 ``
    -InputLogFolder '$Output\logs' ``
    -Output '$Output\triage'
${fence}
"@
Set-Content -Path $wiringMdPath -Value $wiringMd -Encoding ASCII
Write-Output "Wiring commands: $wiringMdPath"

# ---------------------------------------------------------------------------
# Summary
# ---------------------------------------------------------------------------

Write-Output ""
Write-Output "MAP-6T packet written to: $Output"
Write-Output "Files:"
Write-Output "  MAP_6T_LOAD_TEST_PACKET.md"
Write-Output "  MAP_6T_LOAD_TEST_RECORD.local-template.md"
Write-Output "  MAP_6T_INSTALL_AND_SERVER_WIRING_COMMANDS.md"
Write-Output "  map6t-preflight.json"
Write-Output "  map6t-preflight.md"
Write-Output "  candidate/ (generated v1 candidate)"
Write-Output ""
Write-Output "MAP6T_LOTH_V2_LOAD_TEST_PACKET_CREATED"
Write-Output "EMPTY_GRASS_V1_CANDIDATE_GENERATED"
Write-Output "HUMAN_ONLY_COPY_REQUIRED"
Write-Output "LOAD_TEST_NOT_PERFORMED"
Write-Output "WRITER_NOT_CHANGED"
Write-Output "PLAYABLE_EXPORT_CLAIM_ALLOWED=false"
Write-Output "Done."
