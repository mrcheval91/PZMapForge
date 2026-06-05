#Requires -Version 5.1
<#
.SYNOPSIS
    MAP-6P: Generates spawn activation diagnostic commands and a fillable
    record template for the Build 42 candidate (pzmapforge_build42_candidate_001).

    The candidate mod loaded without a crash (MAP-6P/MAP-6O) but the candidate
    spawn region was not visible on the spawn selection screen. This script
    emits human-run inspection commands targeting the likely causes:
      - Server preset Map= line missing candidate map ID
      - spawnregions.lua absent or incorrect in the mod
      - Server _spawnregions.lua not referencing the candidate

    -Output must be under .local/.
    Does NOT read from PZ folders. Does NOT copy files to PZ.

.PARAMETER Output
    Path under .local/ for diagnostic output.

.PARAMETER MapId
    The candidate map ID. Default: pzmapforge_build42_candidate_001

.PARAMETER ModFolderName
    The mod folder name used during install.
    Default: pzmapforge_build42_candidate_001_test_clean

.PARAMETER ServerName
    The PZ server preset name used during the retest.
    Default: PZMF_B42_CANDIDATE_CLEAN_001

.EXAMPLE
    powershell -ExecutionPolicy Bypass -File scripts\prepare-map6p-spawn-activation-diagnostic.ps1 `
        -Output .local\map6p-diagnostic
#>

param(
    [Parameter(Mandatory=$true)][string]$Output,
    [string]$MapId         = 'pzmapforge_build42_candidate_001',
    [string]$ModFolderName = 'pzmapforge_build42_candidate_001_test_clean',
    [string]$ServerName    = 'PZMF_B42_CANDIDATE_CLEAN_001'
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

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

$modBase    = "C:\Users\Palmacede\Zomboid\mods\$ModFolderName"
$mapDir     = "$modBase\42\media\maps\$MapId"
$serverDir  = 'C:\Users\Palmacede\Zomboid\Server'
$iniPath    = "$serverDir\$ServerName.ini"
$srPath     = "$serverDir\${ServerName}_spawnregions.lua"

# Fence variable avoids backtick escape issues in double-quoted here-strings.
$fence = '```'

# ---------------------------------------------------------------------------
# MAP_6P_SPAWN_ACTIVATION_DIAGNOSTIC_COMMANDS.md
# ---------------------------------------------------------------------------

$cmdPath = Join-Path $Output 'MAP_6P_SPAWN_ACTIVATION_DIAGNOSTIC_COMMANDS.md'

$cmds = @"
# MAP-6P Spawn Activation Diagnostic Commands

All commands are read-only. Run each manually in PowerShell.
Do not copy results to any PZ folder.

${fence}text
MapId:         $MapId
ModFolderName: $ModFolderName
ServerName:    $ServerName
HUMAN_ONLY_INSPECTION_REQUIRED
LOAD_TEST_NOT_PERFORMED
PLAYABLE_EXPORT_CLAIM_ALLOWED=false
${fence}

---

## 1. Verify mod folder layout

${fence}powershell
# Check mod folder exists with 42/ layout
Test-Path '$modBase\42\mod.info'
Get-Content '$modBase\42\mod.info' | Select-String 'id='
${fence}

Expected: id=$MapId

${fence}powershell
# Check required map files
Test-Path '$mapDir\map.info'
Test-Path '$mapDir\spawnpoints.lua'
Test-Path '$mapDir\spawnregions.lua'
Test-Path '$mapDir\objects.lua'
Test-Path '$mapDir\0_0.lotheader'
Test-Path '$mapDir\world_0_0.lotpack'
Test-Path '$mapDir\chunkdata_0_0.bin'
${fence}

Record which files return True. spawnregions.lua is critical for spawn activation.

---

## 2. Inspect spawnregions.lua content

If spawnregions.lua exists, inspect it:

${fence}powershell
Get-Content '$mapDir\spawnregions.lua'
${fence}

Expected format:
${fence}lua
function SpawnRegions()
    return {
        {name="PZMapForge Candidate Cell", file="media/maps/$MapId/spawnpoints.lua"},
    }
end
${fence}

If the file is absent, copy pzmapforge_candidate_spawnregions.lua from the MAP-6M
packet and rename it to spawnregions.lua at the path above (HUMAN-ONLY action).

---

## 3. Verify server preset files

${fence}powershell
# Check server preset files exist
Test-Path '$iniPath'
Test-Path '$srPath'
${fence}

If the ini does not exist, the server preset was not created. Launch PZ, go to
Host, create a preset named $ServerName, and exit without starting.

---

## 4. Inspect server ini Mods, Map, WorkshopItems lines

${fence}powershell
Get-Content '$iniPath' | Select-String -Pattern '^Mods=|^Map=|^WorkshopItems='
${fence}

Record the exact Mods=, Map=, and WorkshopItems= values.

Required for spawn activation:
- Mods= must include $MapId
- Map= must include $MapId

Example of correct Map= line:
${fence}text
Map=$MapId;Muldraugh, KY
${fence}

If Map= does not include $MapId, the spawn region will not appear even if all
binary files are valid. Edit the ini to add $MapId to the Map= line
(HUMAN-ONLY action).

---

## 5. Inspect server spawnregions file

${fence}powershell
if (Test-Path '$srPath') { Get-Content '$srPath' } else { 'File not found' }
${fence}

The server spawnregions file should reference:
  media/maps/$MapId/spawnpoints.lua

---

## 6. Record findings

Fill in the diagnostic record template (MAP_6P_SPAWN_ACTIVATION_RECORD.local-template.md)
with values from each inspection command above.

LOAD_TEST_NOT_PERFORMED
PLAYABLE_EXPORT_CLAIM_ALLOWED=false
"@

Set-Content -Path $cmdPath -Value $cmds -Encoding ASCII
Write-Output "Diagnostic commands: $cmdPath"

# ---------------------------------------------------------------------------
# MAP_6P_SPAWN_ACTIVATION_RECORD.local-template.md
# ---------------------------------------------------------------------------

$recordPath = Join-Path $Output 'MAP_6P_SPAWN_ACTIVATION_RECORD.local-template.md'

$record = @"
# MAP-6P Spawn Activation Diagnostic Record

${fence}text
MapId:         $MapId
ModFolderName: $ModFolderName
ServerName:    $ServerName
Date:          [FILL IN]
Operator:      [FILL IN]
CANDIDATE_SPAWN_REGION_NOT_VISIBLE
LOAD_TEST_INCONCLUSIVE
PLAYABLE_EXPORT_CLAIM_ALLOWED=false
${fence}

## Mod Folder Inspection

- mod_folder_exists:              [ yes / no ]
- mod_info_id_matches:            [ yes / no ]
- map_info_exists:                [ yes / no ]
- spawnpoints_exists:             [ yes / no ]
- spawnregions_in_mod_exists:     [ yes / no ]

## Server Preset Inspection

- server_ini_exists:                      [ yes / no ]
- mods_line_contains_candidate:           [ yes / no ]
- map_line_contains_candidate:            [ yes / no ]
- server_spawnregions_exists:             [ yes / no ]
- server_spawnregions_references_candidate: [ yes / no ]

## Observed result

- candidate_spawn_region_visible: no (MAP-6P observation)
- first_gap_identified:           [FILL IN]

## Fix applied

- [FILL IN what was changed to address the gap]

## Next step

Re-run MAP-6O clean retest protocol after applying fix. Record new result.

---

LOAD_TEST_INCONCLUSIVE -- status until spawn activation gap is resolved.
PLAYABLE_EXPORT_CLAIM_ALLOWED=false
"@

Set-Content -Path $recordPath -Value $record -Encoding ASCII
Write-Output "Record template: $recordPath"

Write-Output ""
Write-Output "MAP-6P spawn activation diagnostic written to: $Output"
Write-Output "HUMAN_ONLY_INSPECTION_REQUIRED"
Write-Output "LOAD_TEST_NOT_PERFORMED"
Write-Output "PLAYABLE_EXPORT_CLAIM_ALLOWED=false"
Write-Output "Done."
