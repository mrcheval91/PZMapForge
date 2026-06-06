#Requires -Version 5.1
<#
.SYNOPSIS
    MAP-7E: Produces a diagnostic packet for the next controlled Build 42 retest.

    Records MAP-7D partial load result context.
    Generates a v4 candidate for local inspection.
    Verifies no-BOM on all text files.
    Writes observation checklists and no-BOM server wiring template.

    Does NOT copy files to PZ folders.
    Does NOT write outside .local/.
    Does NOT perform a PZ load test.

.PARAMETER Output
    Path under .local/ for packet output.

.PARAMETER MapId
    Default: pzmapforge_build42_candidate_v4_001

.PARAMETER ModFolderName
    Default: pzmapforge_build42_candidate_v4_001_test

.PARAMETER ServerName
    Default: PZMF_B42_METADATA_V4_TEST_001

.EXAMPLE
    powershell -ExecutionPolicy Bypass -File scripts\prepare-build42-map7e-diagnostics-packet.ps1 `
        -Output .local\map7e-packet
#>

param(
    [Parameter(Mandatory=$true)][string]$Output,
    [string]$MapId         = 'pzmapforge_build42_candidate_v4_001',
    [string]$ModFolderName = 'pzmapforge_build42_candidate_v4_001_test',
    [string]$ServerName    = 'PZMF_B42_METADATA_V4_TEST_001'
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot  = Split-Path -Parent $scriptDir

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
# Step 1: Generate v4 candidate for local inspection
# ---------------------------------------------------------------------------

$candidateOut = Join-Path $Output 'candidate'
Write-Output "Generating empty_grass_v4 candidate for local inspection..."

& dotnet run --project (Join-Path $repoRoot 'src\PZMapForge.Cli') `
    --configuration Release --no-build `
    -- map-export-experimental `
    --map-id $MapId `
    --output $candidateOut `
    --build42-candidate-writer `
    --build42-candidate-profile empty_grass_v4

if ($LASTEXITCODE -ne 0) {
    Write-Error "CLI candidate generation failed (exit $LASTEXITCODE)"
    exit 1
}

$candDir    = Join-Path $candidateOut ($MapId + '_build42_candidate')
$v42Dir     = Join-Path $candDir '42'
$mapDataDir = Join-Path $v42Dir "media\maps\$MapId"

$modInfoPath   = Join-Path $v42Dir     'mod.info'
$mapInfoPath   = Join-Path $mapDataDir 'map.info'
$spawnPtsPath  = Join-Path $mapDataDir 'spawnpoints.lua'
$objectsPath   = Join-Path $mapDataDir 'objects.lua'
$lotheaderPath = Join-Path $mapDataDir '0_0.lotheader'
$lotpackPath   = Join-Path $mapDataDir 'world_0_0.lotpack'
$chunkdataPath = Join-Path $mapDataDir 'chunkdata_0_0.bin'

function Has-Bom ([string]$path) {
    if (-not (Test-Path $path)) { return $false }
    $b = [System.IO.File]::ReadAllBytes($path)
    return ($b.Length -ge 3 -and $b[0] -eq 0xEF -and $b[1] -eq 0xBB -and $b[2] -eq 0xBF)
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

# ---------------------------------------------------------------------------
# Step 2: Preflight verification
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

Add-Check 'objects_lua_no_bom'     (-not (Has-Bom $objectsPath))   "has_bom=$(Has-Bom $objectsPath)"
Add-Check 'spawnpoints_lua_no_bom' (-not (Has-Bom $spawnPtsPath))  "has_bom=$(Has-Bom $spawnPtsPath)"
Add-Check 'mod_info_no_bom'        (-not (Has-Bom $modInfoPath))   "has_bom=$(Has-Bom $modInfoPath)"
Add-Check 'map_info_no_bom'        (-not (Has-Bom $mapInfoPath))   "has_bom=$(Has-Bom $mapInfoPath)"

if (Test-Path $lotheaderPath) {
    $loth = [System.IO.File]::ReadAllBytes($lotheaderPath)
    $trailerStart = Find-TrailerStart $loth
    Add-Check 'loth_total_size_29646' ($loth.Length -eq 29646) "size=$($loth.Length)"
    Add-Check 'loth_trailer_size_1048' (($loth.Length - $trailerStart) -eq 1048) "trailer=$($loth.Length - $trailerStart)"
} else {
    Add-Check 'loth_total_size_29646'  $false 'lotheader missing'
    Add-Check 'loth_trailer_size_1048' $false 'lotheader missing'
}

if (Test-Path $lotpackPath) {
    Add-Check 'lotp_size_1056780' ((Get-Item $lotpackPath).Length -eq 1056780) "size=$((Get-Item $lotpackPath).Length)"
} else { Add-Check 'lotp_size_1056780' $false 'lotpack missing' }

if (Test-Path $chunkdataPath) {
    Add-Check 'chunkdata_size_1026' ((Get-Item $chunkdataPath).Length -eq 1026) "size=$((Get-Item $chunkdataPath).Length)"
} else { Add-Check 'chunkdata_size_1026' $false 'chunkdata missing' }

$passCount = (@($checks | Where-Object { $_.result -eq 'PASS' })).Count
$failCount = (@($checks | Where-Object { $_.result -eq 'FAIL' })).Count
Write-Output ""
Write-Output "Preflight: $passCount PASS / $failCount FAIL"

if (-not $allPass) {
    Write-Error "Preflight failed ($failCount checks). Packet not written."
    exit 1
}

# ---------------------------------------------------------------------------
# Write preflight JSON / MD
# ---------------------------------------------------------------------------

$preflight = [ordered]@{
    schema                         = 'pzmapforge.map7e-diagnostic-preflight.v0.1'
    map_id                         = $MapId
    mod_folder_name                = $ModFolderName
    server_name                    = $ServerName
    candidate_profile              = 'empty_grass_v4'
    all_pass                       = $allPass
    no_bom_objects_lua             = $true
    no_bom_spawnpoints_lua         = $true
    no_bom_mod_info                = $true
    no_bom_map_info                = $true
    loth_size                      = 29646
    loth_trailer_size              = 1048
    lotp_size                      = 1056780
    chunkdata_size                 = 1026
    checks                         = [object[]]$checks.ToArray()
    public_playable_claim_allowed  = $false
    load_test_not_performed        = $true
    map7d_result_basis             = 'MAP7D_LOAD_TEST_PARTIAL_PASS_IN_GAME_EMPTY_WORLD'
    next_diagnostic_goal           = 'map_folder_registration_and_spawn_location'
}

$fence = '```'
$preflightJsonPath = Join-Path $Output 'map7e-diagnostic-preflight.json'
$preflight | ConvertTo-Json -Depth 4 | Set-Content -Path $preflightJsonPath -Encoding UTF8
Write-Output "Preflight JSON: $preflightJsonPath"

$preflightMdPath = Join-Path $Output 'map7e-diagnostic-preflight.md'
$checkTable = ($checks | ForEach-Object { "| $($_.name) | $($_.result) | $($_.detail) |" }) -join "`n"
$preflightMd = @"
# MAP-7E Diagnostic Preflight Report

${fence}text
candidate_profile: empty_grass_v4
no_bom_objects_lua: true
no_bom_spawnpoints_lua: true
loth_size: 29646
public_playable_claim_allowed: false
LOAD_TEST_NOT_PERFORMED
${fence}

## Checks ($passCount PASS / $failCount FAIL)

| Check | Result | Detail |
|---|---|---|
$checkTable
"@
Set-Content -Path $preflightMdPath -Value $preflightMd -Encoding ASCII

# ---------------------------------------------------------------------------
# Paths for docs (human-only references)
# ---------------------------------------------------------------------------

$serverDir  = 'C:\Users\Palmacede\Zomboid\Server'
$iniPath    = "$serverDir\$ServerName.ini"
$srPath     = "$serverDir\${ServerName}_spawnregions.lua"
$hostIni    = 'C:\Users\Palmacede\Zomboid\Lua\host.ini'

# ---------------------------------------------------------------------------
# Write MAP_7E_DIAGNOSTIC_PACKET.md
# ---------------------------------------------------------------------------

$packetMdPath = Join-Path $Output 'MAP_7E_DIAGNOSTIC_PACKET.md'
$packetMd = @"
# MAP-7E Build 42 Empty World / Map Registration Diagnostic Packet

${fence}text
Profile:       empty_grass_v4
MapId:         $MapId
ModFolder:     $ModFolderName
ServerName:    $ServerName
BASIS:         MAP7D_LOAD_TEST_PARTIAL_PASS_IN_GAME_EMPTY_WORLD
GOAL:          Diagnose map folder registration gap
HUMAN_ONLY_COPY_REQUIRED
LOAD_TEST_NOT_PERFORMED
PUBLIC_PLAYABLE_CLAIM_ALLOWED=false
${fence}

## MAP-7D result basis

From MAP-7D successful no-BOM retest:
- Game loaded into IngameState in 32 seconds.
- No LexState/BOM errors.
- No player-data timeout.
- BUT: map folders list was EMPTY and no room/building at spawn 150,150,0.

## Diagnostic goals for next retest

1. Is the map folder registered? Check if candidate name appears between:
   "Looking in these map folders:" and "<End of map-folders list>".
2. What are the player world coordinates at spawn? (F3 or /coords command if available)
3. Is there a city choice screen? Or did PZ auto-select spawn?
4. Does the loaded world look like the candidate region or the base Muldraugh world?
5. Are there any roads, buildings, or visible map grid at the spawn location?
6. Does any console output mention pzmapforge after IsoMetaGrid?

## v4 candidate files (unchanged from MAP-7D)

- Profile: empty_grass_v4 (no-BOM text encoding)
- LOTH size: 29646 bytes
- LOTP size: 1056780 bytes
- chunkdata size: 1026 bytes
- objects.lua: comment-only, no BOM
- spawnpoints.lua: unemployed key, pos 150,150,0, no BOM

## Steps for next retest (HUMAN ONLY)

See MAP_7E_INSTALL_AND_SERVER_WIRING_COMMANDS.md (reuse MAP-7D wiring).
No changes to candidate or server wiring are needed for this diagnostic retest.

## Preflight status

All $passCount preflight checks PASS (no-BOM, binary sizes unchanged).

---

## Safety

- HUMAN_ONLY_COPY_REQUIRED
- LOAD_TEST_NOT_PERFORMED
- PUBLIC_PLAYABLE_CLAIM_ALLOWED=false
"@
Set-Content -Path $packetMdPath -Value $packetMd -Encoding ASCII
Write-Output "Packet MD: $packetMdPath"

# ---------------------------------------------------------------------------
# Write MAP_7E_MANUAL_RETEST_RECORD.local-template.md
# ---------------------------------------------------------------------------

$recordMdPath = Join-Path $Output 'MAP_7E_MANUAL_RETEST_RECORD.local-template.md'
$recordMd = @"
# MAP-7E Manual Retest Record

${fence}text
Profile:       empty_grass_v4
MapId:         $MapId
tested_at:     FILL_IN
basis:         MAP7D_LOAD_TEST_PARTIAL_PASS_IN_GAME_EMPTY_WORLD
${fence}

## Observations

| Item | Value |
|---|---|
| candidate_loaded | yes/no |
| player_data_received | yes/no |
| entered_ingame_state | yes/no |
| city_choice_appeared | yes/no |
| map_folders_list_empty | yes/no |
| candidate_name_in_map_folders | yes/no |
| spawn_building_warning_found | yes/no |
| player_world_x | FILL_IN (or unknown) |
| player_world_y | FILL_IN (or unknown) |
| world_appearance | empty/roads/buildings/fog/grass/procedural/black_void |
| lexstate_error_found | yes/no |
| timeout_error_found | yes/no |

## Map folder log evidence

Paste the exact content from log between:
"Looking in these map folders:" and "<End of map-folders list>":

${fence}
FILL_IN
${fence}

## First error message (if any)

${fence}
FILL_IN
${fence}

## Result

- LOAD_TEST_PASS
- LOAD_TEST_PARTIAL_MAP_REGISTERED
- MAP7D_LOAD_TEST_PARTIAL_PASS_IN_GAME_EMPTY_WORLD
- LOAD_TEST_INCONCLUSIVE

## Non-claims

- PUBLIC_PLAYABLE_CLAIM_ALLOWED=false: binding.
- No compatibility claim regardless of result.
"@
Set-Content -Path $recordMdPath -Value $recordMd -Encoding ASCII
Write-Output "Record template: $recordMdPath"

# ---------------------------------------------------------------------------
# Write MAP_7E_EMPTY_WORLD_OBSERVATION_CHECKLIST.md
# ---------------------------------------------------------------------------

$checklistMdPath = Join-Path $Output 'MAP_7E_EMPTY_WORLD_OBSERVATION_CHECKLIST.md'
$checklistMd = @"
# MAP-7E Empty World Observation Checklist

Complete this during the next retest session.

## Log checks (search console.txt after test)

- [ ] Does "pzmapforge_build42_candidate_v4_001" appear after "Looking in these map folders:"?
- [ ] Is there any folder listed between the map-folders markers?
- [ ] Does "no room or building at" appear? At what coords?
- [ ] Does "IsoMetaGrid" mention pzmapforge anywhere?
- [ ] Does "loadForCell" appear for cell 0,0 or any cell?
- [ ] Is there a "reading cell" or "loading cell" message for the candidate?

## In-game checks (during session)

- [ ] Did a city choice / spawn selection screen appear?
- [ ] What spawn option was shown (or was spawn auto-selected)?
- [ ] Press F3 or type /coords if available — note worldX, worldY.
- [ ] Is the world empty (no roads, buildings, grass tiles)?
- [ ] Is the world black/void (cell not loaded)?
- [ ] Is the world procedurally generated (roads visible that aren't ours)?
- [ ] Is the world the base Muldraugh/vanilla map?
- [ ] Can the player move?

## Classification guide

| Observation | Likely next task |
|---|---|
| Map folder appears in log scan | MAP-7F: cell visibility / content diagnostic |
| Map folder still missing from log | MAP-7F: mod.info lots= / Map= wiring investigation |
| Cell 0,0 loads visibly | MAP-7F: confirm cell content, spawn point |
| Black void at spawn | MAP-7F: chunkdata/lotpack payload format |
| Vanilla world (roads etc.) | MAP-7F: confirm candidate region vs base world |

## Non-claims

PUBLIC_PLAYABLE_CLAIM_ALLOWED=false: binding.
"@
Set-Content -Path $checklistMdPath -Value $checklistMd -Encoding ASCII
Write-Output "Checklist MD: $checklistMdPath"

# ---------------------------------------------------------------------------
# Write MAP_7E_SERVER_WIRING_NO_BOM_TEMPLATE.md
# ---------------------------------------------------------------------------

$wiringMdPath = Join-Path $Output 'MAP_7E_SERVER_WIRING_NO_BOM_TEMPLATE.md'
$wiringMd = @"
# MAP-7E Server Wiring No-BOM Template

All commands in this file are HUMAN-ONLY.
This file is informational only. This script does NOT execute these commands.
All Zomboid Server writes must be performed by the human operator.

${fence}text
HUMAN_ONLY_COPY_REQUIRED
NO_AUTOMATIC_PZ_WRITES_PERFORMED_BY_THIS_SCRIPT
PUBLIC_PLAYABLE_CLAIM_ALLOWED=false
${fence}

## Server INI (HUMAN-ONLY, UTF-8 no-BOM)

File: $iniPath

${fence}powershell
# HUMAN-ONLY: Write server INI with no-BOM UTF-8
# HUMAN-ONLY: `$iniContent = "Mods=$MapId``nMap=$MapId;Muldraugh, KY``nWorkshopItems=``nPublic=false"
# HUMAN-ONLY: [System.IO.File]::WriteAllText('$iniPath', `$iniContent, [System.Text.UTF8Encoding]::new(`$false))
${fence}

## _spawnregions.lua (HUMAN-ONLY, UTF-8 no-BOM)

File: $srPath

${fence}powershell
# HUMAN-ONLY: Write spawnregions with no-BOM UTF-8
# HUMAN-ONLY: `$srContent = "function SpawnRegions()`n    return {`n        {name=`"PZMapForge v4`", file=`"media/maps/$MapId/spawnpoints.lua`"},`n    }`nend`n"
# HUMAN-ONLY: [System.IO.File]::WriteAllText('$srPath', `$srContent, [System.Text.UTF8Encoding]::new(`$false))
${fence}

## host.ini (HUMAN-ONLY, UTF-8 no-BOM)

File: $hostIni

${fence}powershell
# HUMAN-ONLY: Update host.ini servername entry with no-BOM UTF-8
# HUMAN-ONLY: Read, replace servername line, write back with no-BOM:
# HUMAN-ONLY: `$hi = [System.IO.File]::ReadAllText('$hostIni')
# HUMAN-ONLY: `$hi = `$hi -replace 'servername=.*', 'servername=$ServerName'
# HUMAN-ONLY: [System.IO.File]::WriteAllText('$hostIni', `$hi, [System.Text.UTF8Encoding]::new(`$false))
${fence}

## Verify (HUMAN-ONLY)

After writing all files, verify first 3 bytes are NOT EF BB BF (UTF-8 BOM):

${fence}powershell
# HUMAN-ONLY
@('$iniPath', '$srPath', '$hostIni') | ForEach-Object {
    `$b = [System.IO.File]::ReadAllBytes(`$_)
    `$hasBom = (`$b.Length -ge 3 -and `$b[0] -eq 0xEF -and `$b[1] -eq 0xBB -and `$b[2] -eq 0xBF)
    Write-Output "`$_ : has_bom=`$hasBom"
}
# All should output has_bom=False
${fence}
"@
Set-Content -Path $wiringMdPath -Value $wiringMd -Encoding ASCII
Write-Output "Wiring template: $wiringMdPath"

# ---------------------------------------------------------------------------
# Summary
# ---------------------------------------------------------------------------

Write-Output ""
Write-Output "MAP7E_DIAGNOSTIC_PACKET_CREATED"
Write-Output "EMPTY_GRASS_V4_CANDIDATE_VERIFIED"
Write-Output "NO_BOM_CONFIRMED_ON_ALL_TEXT_FILES"
Write-Output "HUMAN_ONLY_COPY_REQUIRED"
Write-Output "LOAD_TEST_NOT_PERFORMED"
Write-Output "NO_AUTOMATIC_PZ_WRITES_PERFORMED_BY_THIS_SCRIPT"
Write-Output "PUBLIC_PLAYABLE_CLAIM_ALLOWED=false"
Write-Output ""
Write-Output "Outputs:"
Write-Output "  $packetMdPath"
Write-Output "  $recordMdPath"
Write-Output "  $checklistMdPath"
Write-Output "  $wiringMdPath"
Write-Output "  $preflightJsonPath"
Write-Output "  $preflightMdPath"
Write-Output "Done."
