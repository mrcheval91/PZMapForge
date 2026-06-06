#Requires -Version 5.1
<#
.SYNOPSIS
    MAP-7A: Generates the empty_grass_v2 candidate and produces a human-ready
    load-test packet for the Build 42 LOTH v3 candidate.

    -Output must be under .local/.
    Does NOT copy any files to PZ folders.
    Does NOT write outside .local/.

    Steps:
    1. Generates empty_grass_v2 candidate under Output/candidate/.
    2. Runs 24-point preflight on the generated files.
    3. Writes MAP_7A_LOAD_TEST_PACKET.md, RECORD template, wiring commands,
       map7a-preflight.json, map7a-preflight.md.

.PARAMETER Output
    Path under .local/ for packet output.

.PARAMETER MapId
    Map ID for the candidate. Default: pzmapforge_build42_candidate_v2_001

.PARAMETER ModFolderName
    Destination mod folder name for human install step.
    Default: pzmapforge_build42_candidate_v2_001_test

.PARAMETER ServerName
    PZ server preset name.
    Default: PZMF_B42_LOTH_V3_TEST_001

.EXAMPLE
    powershell -ExecutionPolicy Bypass -File scripts\prepare-build42-loth-v3-load-test-packet.ps1 `
        -Output .local\map7a-packet
#>

param(
    [Parameter(Mandatory=$true)][string]$Output,
    [string]$MapId         = 'pzmapforge_build42_candidate_v2_001',
    [string]$ModFolderName = 'pzmapforge_build42_candidate_v2_001_test',
    [string]$ServerName    = 'PZMF_B42_LOTH_V3_TEST_001'
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
# Step 1: Generate empty_grass_v2 candidate
# ---------------------------------------------------------------------------

$candidateOut = Join-Path $Output 'candidate'
Write-Output "Generating empty_grass_v2 candidate..."

& dotnet run --project (Join-Path $repoRoot 'src\PZMapForge.Cli') `
    --configuration Release --no-build `
    -- map-export-experimental `
    --map-id $MapId `
    --output $candidateOut `
    --build42-candidate-writer `
    --build42-candidate-profile empty_grass_v2

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

function Find-TrailerStart ([byte[]]$bytes) {
    $i = 12
    while ($i -lt $bytes.Length) {
        $b = $bytes[$i]
        if ($b -eq 0x0A -or ($b -ge 0x20 -and $b -le 0x7E)) { $i++ }
        else { break }
    }
    return $i
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

    $magic    = if ($loth.Length -ge 4)  { [System.Text.Encoding]::ASCII.GetString($loth[0..3]) } else { '' }
    $ver      = if ($loth.Length -ge 8)  { [BitConverter]::ToUInt32($loth, 4) }  else { 0 }
    $cnt      = if ($loth.Length -ge 12) { [BitConverter]::ToUInt32($loth, 8) }  else { 0 }

    Add-Check 'loth_magic_is_LOTH'    ($magic -eq 'LOTH')  "magic=$magic"
    Add-Check 'loth_version_is_1'     ($ver   -eq 1)       "version=$ver"
    Add-Check 'loth_entry_count_1024' ($cnt   -eq 1024)    "entry_count=$cnt"

    # Parse ASCII region only (v3 has binary trailer after ASCII)
    $trailerStart = Find-TrailerStart $loth
    if ($trailerStart -gt 12) {
        $asciiText = [System.Text.Encoding]::ASCII.GetString($loth, 12, $trailerStart - 12)
        $entries   = @($asciiText -split "`n" | Where-Object { $_.Length -gt 0 })
        $first     = if ($entries.Count -gt 0) { $entries[0]                     } else { '' }
        $last      = if ($entries.Count -gt 0) { $entries[$entries.Count - 1]    } else { '' }
    } else {
        $entries = @(); $first = ''; $last = ''
    }

    Add-Check 'loth_first_entry_correct' ($first -eq 'blends_grassoverlays_01_0')    "first=$first"
    Add-Check 'loth_last_entry_correct'  ($last  -eq 'blends_grassoverlays_01_1023') "last=$last"

    # Trailer checks
    $trailerSize = $loth.Length - $trailerStart
    Add-Check 'loth_trailer_size_1048' ($trailerSize -eq 1048) "trailer_size=$trailerSize"

    $canonSha = '93a8f3ccf2cafdc2fb7cd4f3836c29d87076f244f5ba685f92659fbdaf778ec7'
    $trailerSha256 = ''
    if ($trailerSize -ge 1048) {
        $sha256obj   = [System.Security.Cryptography.SHA256]::Create()
        $trailerBytes = [byte[]]($loth[$trailerStart..($trailerStart + 1047)])
        $hashBytes   = $sha256obj.ComputeHash($trailerBytes)
        $sha256obj.Dispose()
        $trailerSha256 = ($hashBytes | ForEach-Object { $_.ToString('x2') }) -join ''
        Add-Check 'loth_trailer_sha256_canonical' ($trailerSha256 -eq $canonSha) "sha256=$trailerSha256"
    } else {
        Add-Check 'loth_trailer_sha256_canonical' $false "trailer too small ($trailerSize bytes, expected 1048)"
    }

    Add-Check 'loth_total_size_29646' ($loth.Length -eq 29646) "size=$($loth.Length)"
} else {
    foreach ($n in @('loth_magic_is_LOTH','loth_version_is_1','loth_entry_count_1024',
                     'loth_first_entry_correct','loth_last_entry_correct',
                     'loth_trailer_size_1048','loth_trailer_sha256_canonical',
                     'loth_total_size_29646')) {
        Add-Check $n $false 'lotheader file missing'
    }
    $trailerSha256 = ''
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
    Add-Check 'report_profile_empty_grass_v2'        ($rep.build42_candidate_profile -eq 'empty_grass_v2')             'profile=empty_grass_v2'
    Add-Check 'report_trailer_strategy_map6y_stable' ($rep.loth_trailer_strategy -eq 'map6y_stable_literal_1048_block') 'trailer_strategy=map6y_stable_literal_1048_block'
    Add-Check 'report_load_tested_false'             ($rep.load_tested             -eq $false)                          'load_tested=false'
    Add-Check 'report_playable_export_generated_false' ($rep.playable_export_generated -eq $false)                      'playable_export_generated=false'
    Add-Check 'report_playable_export_claimed_false' ($rep.playable_export_claimed   -eq $false)                        'playable_export_claimed=false'
    Add-Check 'report_pz_assets_copied_false'        ($rep.pz_assets_copied          -eq $false)                        'pz_assets_copied=false'
    Add-Check 'report_pz_assets_read_false'          ($rep.pz_assets_read            -eq $false)                        'pz_assets_read=false'
} else {
    foreach ($n in @('report_profile_empty_grass_v2','report_trailer_strategy_map6y_stable',
                     'report_load_tested_false','report_playable_export_generated_false',
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
    schema              = 'pzmapforge.map7a-preflight.v0.1'
    map_id              = $MapId
    mod_folder_name     = $ModFolderName
    server_name         = $ServerName
    candidate_profile   = 'empty_grass_v2'
    all_pass            = $allPass
    entry_count         = 1024
    loth_size           = 29646
    loth_trailer_size   = 1048
    loth_trailer_sha256 = $trailerSha256
    lotp_size           = 1056780
    chunkdata_size      = 1026
    checks              = [object[]]$checks.ToArray()
    load_test_not_performed  = $true
    pz_assets_copied         = $false
    playable_export_claimed  = $false
    writer_not_changed       = $true
}

$preflightJsonPath = Join-Path $Output 'map7a-preflight.json'
$preflight | ConvertTo-Json -Depth 4 | Set-Content -Path $preflightJsonPath -Encoding UTF8
Write-Output "Preflight JSON: $preflightJsonPath"

# ---------------------------------------------------------------------------
# Write preflight MD
# ---------------------------------------------------------------------------

$fence = '```'
$preflightMdPath = Join-Path $Output 'map7a-preflight.md'
$checkTable = ($checks | ForEach-Object { "| $($_.name) | $($_.result) | $($_.detail) |" }) -join "`n"
$preflightMd = @"
# MAP-7A Preflight Report

${fence}text
candidate_profile: empty_grass_v2
map_id: $MapId
all_pass: $($allPass.ToString().ToLower())
entry_count: 1024
loth_size: 29646
loth_trailer_size: 1048
loth_trailer_sha256: $trailerSha256
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
# Destination paths (documented for human use only)
# ---------------------------------------------------------------------------

$destBase  = "C:\Users\Palmacede\Zomboid\mods\$ModFolderName\42"
$serverDir = 'C:\Users\Palmacede\Zomboid\Server'
$iniPath   = "$serverDir\$ServerName.ini"
$srPath    = "$serverDir\${ServerName}_spawnregions.lua"
$hostIni   = 'C:\Users\Palmacede\Zomboid\Lua\host.ini'
$consoleTxt = 'C:\Users\Palmacede\Zomboid\console.txt'

# ---------------------------------------------------------------------------
# Write MAP_7A_LOAD_TEST_PACKET.md
# ---------------------------------------------------------------------------

$packetMdPath = Join-Path $Output 'MAP_7A_LOAD_TEST_PACKET.md'
$packetMd = @"
# MAP-7A Build 42 LOTH v3 Load Test Packet

${fence}text
Profile:       empty_grass_v2
MapId:         $MapId
ModFolder:     $ModFolderName
ServerName:    $ServerName
HUMAN_ONLY_COPY_REQUIRED
LOAD_TEST_NOT_PERFORMED
WRITER_NOT_CHANGED
PLAYABLE_EXPORT_CLAIM_ALLOWED=false
${fence}

## Preflight status

All $passCount preflight checks PASS. Candidate is ready for manual testing.

- Profile:        empty_grass_v2 (MAP-6Z)
- LOTH size:      29646 bytes (header=12 + ASCII=28586 + trailer=1048)
- entry_count:    1024
- first entry:    blends_grassoverlays_01_0
- last entry:     blends_grassoverlays_01_1023
- trailer size:   1048 bytes (MAP-6Y canonical stable block)
- trailer SHA256: $trailerSha256
- LOTP size:      1056780 bytes (unchanged from MAP-6S/MAP-6Z)
- chunkdata size: 1026 bytes (unchanged)

---

## Diagnostic value table

| Scenario | Classification | Next task |
|---|---|---|
| 0_0.lotheader fails again (EOF/readInt) | LOAD_TEST_FAIL_LOTH | MAP-7B: deepen LOTH format or try different entry set |
| World loads, 0_0.lotheader passes, lotpack fails | LOAD_TEST_FAIL_LOTP | MAP-7B: LOTP payload format research |
| LOTP passes, chunkdata fails | LOAD_TEST_FAIL_CHUNKDATA | MAP-7B: chunkdata format research |
| chunkdata passes, objects.lua fails | LOAD_TEST_FAIL_OBJECTS_LUA | MAP-7B: objects.lua fix |
| World enters successfully | LOAD_TEST_PASS | Record carefully; still no public claim until human review |
| Test inconclusive (crash/no log) | LOAD_TEST_INCONCLUSIVE | Repeat with clean environment |

---

## Step 1: Remove previous test mod folders (HUMAN ONLY)

${fence}
HUMAN-ONLY: Remove-Item -Recurse -Force 'C:\Users\Palmacede\Zomboid\mods\pzmapforge_build42_candidate_001_test_clean' -ErrorAction SilentlyContinue
HUMAN-ONLY: Remove-Item -Recurse -Force 'C:\Users\Palmacede\Zomboid\mods\pzmapforge_manual_b42_001_maptest_a' -ErrorAction SilentlyContinue
HUMAN-ONLY: Remove-Item -Recurse -Force 'C:\Users\Palmacede\Zomboid\mods\$ModFolderName' -ErrorAction SilentlyContinue
${fence}

---

## Step 2: Copy v2 candidate to PZ mods (HUMAN ONLY)

SOURCE (under .local -- do not modify):
  $v42Dir

DESTINATION (parent dir):
  $($destBase | Split-Path -Parent)

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

See MAP_7A_INSTALL_AND_SERVER_WIRING_COMMANDS.md for full wiring details.

Required in server ini ($iniPath):
${fence}
Mods=$MapId
Map=$MapId;Muldraugh, KY
WorkshopItems=
${fence}

Required server spawnregions file ($srPath):
${fence}lua
function SpawnRegions()
    return {
        {name="PZMapForge v2 Candidate Cell", file="media/maps/$MapId/spawnpoints.lua"},
    }
end
${fence}

---

## Step 4: Patch host.ini (HUMAN ONLY)

${fence}
HUMAN-ONLY: Set in ${hostIni}: servername=$ServerName
${fence}

---

## Step 5: Delete stale console.txt (HUMAN ONLY)

${fence}
HUMAN-ONLY: Remove-Item -Force '$consoleTxt' -ErrorAction SilentlyContinue
${fence}

---

## Step 6: Run the test (HUMAN ONLY)

1. Launch Project Zomboid Build 42.
2. Enable ONLY the $MapId mod.
3. Navigate to Host and select $ServerName.
4. Record observations in MAP_7A_LOAD_TEST_RECORD.local-template.md.

Key questions to answer:
- Does mod selection crash?          -> mod_selection_crash
- Is the v2 candidate spawn visible? -> candidate_spawn_region_visible
- Does world loading start?          -> world_load_started
- Is there a lotheader exception?    -> loth_error_found
- Is there a LOTP exception?         -> lotp_error_found
- Is there a chunkdata exception?    -> chunkdata_error_found
- Is there an objects.lua exception? -> objects_lua_error_found

---

## Step 7: Capture fresh log (HUMAN ONLY)

Immediately after the test:
${fence}
HUMAN-ONLY: New-Item -ItemType Directory -Force -Path '$Output\logs'
HUMAN-ONLY: Copy-Item '$consoleTxt' '$Output\logs\console-map7a-TIMESTAMP.txt'
${fence}
Replace TIMESTAMP with actual time (e.g. 20260605-143022).

---

## Step 8: Run log triage (PZMapForge tool -- safe, not a load test)

${fence}powershell
powershell -ExecutionPolicy Bypass -File scripts\extract-map6n-current-candidate-log-evidence.ps1 ``
    -InputLogFolder '$Output\logs' ``
    -Output '$Output\triage'
${fence}

---

## Safety

- HUMAN_ONLY_COPY_REQUIRED: no automatic copy to PZ folders by this script.
- LOAD_TEST_NOT_PERFORMED: this packet does not execute the PZ test.
- WRITER_NOT_CHANGED: no writer change in MAP-7A.
- PLAYABLE_EXPORT_CLAIM_ALLOWED=false: binding.
"@
Set-Content -Path $packetMdPath -Value $packetMd -Encoding ASCII
Write-Output "Packet MD: $packetMdPath"

# ---------------------------------------------------------------------------
# Write MAP_7A_LOAD_TEST_RECORD.local-template.md
# ---------------------------------------------------------------------------

$recordMdPath = Join-Path $Output 'MAP_7A_LOAD_TEST_RECORD.local-template.md'
$recordMd = @"
# MAP-7A Load Test Record

${fence}text
Profile:       empty_grass_v2
MapId:         $MapId
ModFolder:     $ModFolderName
ServerName:    $ServerName
tested_at:     FILL_IN
tester:        FILL_IN
${fence}

## Observations

| Item | Value |
|---|---|
| mod_selection_crash | yes/no |
| spawn_screen_reached | yes/no |
| candidate_spawn_region_visible | yes/no |
| world_load_started | yes/no |
| entered_world | yes/no |
| returned_to_menu | yes/no |
| crash_to_desktop | yes/no |
| candidate_specific_exception_found | yes/no |
| loth_error_found | yes/no |
| lotp_error_found | yes/no |
| chunkdata_error_found | yes/no |
| objects_lua_error_found | yes/no |

## First error message

${fence}
FILL_IN (paste first exception or error line from console.txt)
${fence}

## Result

Circle one:

- LOAD_TEST_PASS
- LOAD_TEST_FAIL_LOTH
- LOAD_TEST_FAIL_LOTP
- LOAD_TEST_FAIL_CHUNKDATA
- LOAD_TEST_FAIL_OBJECTS_LUA
- LOAD_TEST_INCONCLUSIVE

## Log evidence

Log file:
  FILL_IN (e.g. $Output\logs\console-map7a-TIMESTAMP.txt)

Triage output:
  FILL_IN (e.g. $Output\triage\map6n-log-triage-report.json)

## Non-claims

- No playable export claim is made regardless of result.
- If LOAD_TEST_PASS: requires human review before any public statement.
- PLAYABLE_EXPORT_CLAIM_ALLOWED=false: binding.
"@
Set-Content -Path $recordMdPath -Value $recordMd -Encoding ASCII
Write-Output "Record template: $recordMdPath"

# ---------------------------------------------------------------------------
# Write MAP_7A_INSTALL_AND_SERVER_WIRING_COMMANDS.md
# ---------------------------------------------------------------------------

$wiringMdPath = Join-Path $Output 'MAP_7A_INSTALL_AND_SERVER_WIRING_COMMANDS.md'
$wiringMd = @"
# MAP-7A Install and Server Wiring Commands

All commands below are HUMAN-ONLY. This file is informational only.
The packet script does NOT execute any of these commands.

${fence}text
HUMAN_ONLY_COPY_REQUIRED
LOAD_TEST_NOT_PERFORMED
PLAYABLE_EXPORT_CLAIM_ALLOWED=false
${fence}

---

## 1. Delete previous test mods (PowerShell, HUMAN ONLY)

${fence}powershell
# HUMAN-ONLY
Remove-Item -Recurse -Force 'C:\Users\Palmacede\Zomboid\mods\pzmapforge_build42_candidate_001_test_clean' -ErrorAction SilentlyContinue
Remove-Item -Recurse -Force 'C:\Users\Palmacede\Zomboid\mods\pzmapforge_manual_b42_001_maptest_a' -ErrorAction SilentlyContinue
Remove-Item -Recurse -Force 'C:\Users\Palmacede\Zomboid\mods\$ModFolderName' -ErrorAction SilentlyContinue
${fence}

---

## 2. Copy v2 candidate 42/ folder (PowerShell, HUMAN ONLY)

${fence}powershell
# HUMAN-ONLY
New-Item -ItemType Directory -Force -Path '$($destBase | Split-Path -Parent)'
Copy-Item -Recurse -Force '$v42Dir' '$($destBase | Split-Path -Parent)'
${fence}

Verify after copy:
${fence}powershell
# HUMAN-ONLY
Test-Path '$destBase\mod.info'
Test-Path '$destBase\media\maps\$MapId\0_0.lotheader'
Test-Path '$destBase\media\maps\$MapId\world_0_0.lotpack'
Test-Path '$destBase\media\maps\$MapId\chunkdata_0_0.bin'
${fence}

---

## 3. Create server INI (HUMAN ONLY)

File: $iniPath

Required lines:
${fence}
Mods=$MapId
Map=$MapId;Muldraugh, KY
WorkshopItems=
${fence}

---

## 4. Create _spawnregions.lua (HUMAN ONLY)

File: $srPath

Content:
${fence}lua
function SpawnRegions()
    return {
        {name="PZMapForge v2 Candidate Cell", file="media/maps/$MapId/spawnpoints.lua"},
    }
end
${fence}

---

## 5. Patch host.ini (HUMAN ONLY)

File: $hostIni

Set:
${fence}
servername=$ServerName
${fence}

---

## 6. Delete stale console.txt (HUMAN ONLY)

${fence}powershell
# HUMAN-ONLY
Remove-Item -Force '$consoleTxt' -ErrorAction SilentlyContinue
${fence}

---

## 7. Run PZ test (HUMAN ONLY)

1. Launch Project Zomboid Build 42.
2. Enable only mod: $MapId.
3. Host > select $ServerName.
4. Observe and record in MAP_7A_LOAD_TEST_RECORD.local-template.md.

---

## 8. Capture fresh console.txt (HUMAN ONLY)

Immediately after test exits:
${fence}powershell
# HUMAN-ONLY
New-Item -ItemType Directory -Force -Path '$Output\logs'
Copy-Item '$consoleTxt' '$Output\logs\console-map7a-TIMESTAMP.txt'
${fence}

---

## 9. Run log triage (PZMapForge tool -- safe)

${fence}powershell
powershell -ExecutionPolicy Bypass -File scripts\extract-map6n-current-candidate-log-evidence.ps1 ``
    -InputLogFolder '$Output\logs' ``
    -Output '$Output\triage'
${fence}
"@
Set-Content -Path $wiringMdPath -Value $wiringMd -Encoding ASCII
Write-Output "Wiring MD: $wiringMdPath"

# ---------------------------------------------------------------------------
# Summary
# ---------------------------------------------------------------------------

Write-Output ""
Write-Output "MAP_7A_LOTH_V3_LOAD_TEST_PACKET_CREATED"
Write-Output "EMPTY_GRASS_V2_CANDIDATE_GENERATED"
Write-Output "HUMAN_ONLY_COPY_REQUIRED"
Write-Output "LOAD_TEST_NOT_PERFORMED"
Write-Output "WRITER_NOT_CHANGED"
Write-Output "PLAYABLE_EXPORT_CLAIM_ALLOWED=false"
Write-Output ""
Write-Output "Outputs:"
Write-Output "  $packetMdPath"
Write-Output "  $recordMdPath"
Write-Output "  $wiringMdPath"
Write-Output "  $preflightJsonPath"
Write-Output "  $preflightMdPath"
Write-Output "Done."
