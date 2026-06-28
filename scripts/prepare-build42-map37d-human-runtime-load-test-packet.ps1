#Requires -Version 5.1
<#
.SYNOPSIS
    MAP-37D: Prepare the MAP-37C human runtime load-test packet.

    Invokes the MAP-37C prepare script to generate a fresh staged candidate,
    captures binary evidence from the staged chunkdata_35_27.bin, then writes
    the full human-installable runtime test packet under .local/ only.

    Does NOT write to Steam, Workshop, Project Zomboid install folders, or live server folders.
    All install/copy commands in the produced docs are marked HUMAN-ONLY.

Claim boundary:
    PLAYABLE_EXPORT_CLAIM_ALLOWED=false
    HUMAN_ONLY_INSTALL_REQUIRED=true
    CLAUDE_RAN_PZ=false
    CLAUDE_WROTE_STEAM=false
    CLAUDE_WROTE_WORKSHOP=false
    staged_output_local_only=true
    MAP37D_HUMAN_RUNTIME_LOAD_TEST_PACKET_DEFINED
#>
param(
    [string]$OutputRoot = ''
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot  = Split-Path -Parent $scriptDir

if ($OutputRoot -eq '') {
    $OutputRoot = Join-Path $repoRoot '.local\deadmtl-authoring\map37d-human-runtime-load-test-packet'
}

function Assert-LocalPath([string]$p) {
    $localRoot = Join-Path $repoRoot '.local'
    $resolved  = [System.IO.Path]::GetFullPath($p)
    if (-not $resolved.StartsWith([System.IO.Path]::GetFullPath($localRoot), [System.StringComparison]::OrdinalIgnoreCase)) {
        Write-Error "MAP-37D: Output path must be under .local\: $p"
        exit 1
    }
}

Assert-LocalPath $OutputRoot

$mapId    = 'pzmapforge_map37c'
$cellX    = 35
$cellY    = 27
$posX     = 150
$posY     = 150
$posZ     = 0
$pzWorldX = $cellX * 300 + $posX   # 10650
$pzWorldY = $cellY * 300 + $posY   # 8250

New-Item -ItemType Directory -Path $OutputRoot -Force | Out-Null

# Step 1: Run MAP-37C prepare to produce a fresh staged candidate
$map37cOut        = Join-Path $OutputRoot 'map37c-staged'
$map37cPrepScript = Join-Path $scriptDir 'prepare-build42-map37c-chunkdata-staged-packet.ps1'

Write-Output "MAP-37D: Generating MAP-37C staged candidate..."
Remove-Item -Recurse -Force $map37cOut -ErrorAction SilentlyContinue
& powershell -ExecutionPolicy Bypass -File $map37cPrepScript -OutputRoot $map37cOut
if ($LASTEXITCODE -ne 0) { throw "MAP-37D: MAP-37C prepare script failed (exit $LASTEXITCODE)" }

# Step 2: Verify candidate files and capture binary evidence
$chunkRel     = "${mapId}_build42_candidate\42\media\maps\${mapId}\chunkdata_35_27.bin"
$chunkPath    = Join-Path $map37cOut "run1\$chunkRel"
$map37cJson   = Join-Path $map37cOut 'map37c-chunkdata-staged-packet.json'

if (-not (Test-Path $chunkPath))  { throw "MAP-37D: chunkdata_35_27.bin not found at $chunkPath" }
if (-not (Test-Path $map37cJson)) { throw "MAP-37D: MAP-37C preflight JSON not found at $map37cJson" }

$bytes      = [System.IO.File]::ReadAllBytes($chunkPath)
$sha        = [System.Security.Cryptography.SHA256]::Create()
$chunkHash  = [System.BitConverter]::ToString($sha.ComputeHash($bytes)).Replace('-', '').ToLower()
$sha.Dispose()

$map37cEvidence = Get-Content $map37cJson -Raw | ConvertFrom-Json
$generatedAt    = (Get-Date -Format 'yyyy-MM-ddTHH:mm:ssZ')

# Step 3: Write preflight JSON
$jsonPath = Join-Path $OutputRoot 'map37d-human-runtime-load-test-packet.json'
$preflight = [ordered]@{
    schema                            = 'pzmapforge.map37d-human-runtime-load-test-packet.v0.1'
    generated_at_utc                  = $generatedAt
    map_id                            = $mapId
    cell_x                            = $cellX
    cell_y                            = $cellY
    chunkdata_file                    = 'chunkdata_35_27.bin'
    chunkdata_sha256                  = $chunkHash
    chunkdata_file_size               = $bytes.Length
    map37c_binary_evidence_referenced = $true
    map37c_chunkdata_deterministic    = $map37cEvidence.chunkdata_deterministic
    map37c_chunkdata_exact_fit        = $map37cEvidence.chunkdata_exact_fit
    map37c_chunkdata_sha256_run1      = $map37cEvidence.chunkdata_sha256_run1
    staged_candidate_subpath          = "map37c-staged\run1\${mapId}_build42_candidate"
    mod_id                            = $mapId
    expected_mods_line                = "Mods=$mapId"
    expected_map_line                 = "Map=${mapId};Muldraugh, KY"
    expected_spawn_world_x            = $cellX
    expected_spawn_world_y            = $cellY
    expected_spawn_pos_x              = $posX
    expected_spawn_pos_y              = $posY
    expected_spawn_pos_z              = $posZ
    expected_pz_world_x               = $pzWorldX
    expected_pz_world_y               = $pzWorldY
    map_folder                        = "media\maps\$mapId"
    cell_identity                     = "${cellX}_${cellY}"
    binary_files_expected             = @('chunkdata_35_27.bin', '35_27.lotheader', 'world_35_27.lotpack')
    PLAYABLE_EXPORT_CLAIM_ALLOWED     = $false
    HUMAN_ONLY_INSTALL_REQUIRED       = $true
    CLAUDE_RAN_PZ                     = $false
    CLAUDE_WROTE_STEAM                = $false
    CLAUDE_WROTE_WORKSHOP             = $false
    staged_output_local_only          = $true
}
$preflight | ConvertTo-Json -Depth 4 | Out-File $jsonPath -Encoding utf8

# Step 4: Write preflight MD (uses variables — no code fences, safe for @"..."@)
$mdPath = Join-Path $OutputRoot 'map37d-human-runtime-load-test-packet.md'
@"
# MAP-37D Human Runtime Load-Test Packet

Generated: $generatedAt
Schema: pzmapforge.map37d-human-runtime-load-test-packet.v0.1

## Staged candidate

| Field | Value |
|---|---|
| map_id | $mapId |
| cell_identity | ${cellX}_${cellY} |
| chunkdata_file | chunkdata_35_27.bin |
| chunkdata_sha256 | $chunkHash |
| chunkdata_file_size | $($bytes.Length) |
| map37c_binary_evidence_referenced | true |

## Expected server wiring

| Field | Value |
|---|---|
| Mods= | Mods=$mapId |
| Map= | Map=${mapId};Muldraugh, KY |
| spawn worldX | $cellX |
| spawn worldY | $cellY |
| spawn posX | $posX |
| spawn posY | $posY |
| PZ world X | $pzWorldX |
| PZ world Y | $pzWorldY |

## Claim boundary

MAP37D_HUMAN_RUNTIME_LOAD_TEST_PACKET_DEFINED
PLAYABLE_EXPORT_CLAIM_ALLOWED=false
HUMAN_ONLY_INSTALL_REQUIRED=true
CLAUDE_RAN_PZ=false
CLAUDE_WROTE_STEAM=false
CLAUDE_WROTE_WORKSHOP=false
staged_output_local_only=true
"@ | Out-File $mdPath -Encoding utf8

# Step 5: Write operator packet doc (no variables needed — use @'...'@ to avoid backtick issues)
$packetDocPath = Join-Path $OutputRoot 'MAP_37D_HUMAN_RUNTIME_LOAD_TEST_PACKET.md'
@'
# MAP-37D Human Runtime Load-Test Packet

MAP37D_HUMAN_RUNTIME_LOAD_TEST_PACKET_DEFINED

This packet enables a controlled human runtime test of the MAP-37C chunkdata
candidate (chunkdata_35_27.bin, 1026 bytes, 00 01 header + 128 zero records).

Claim boundary: PLAYABLE_EXPORT_CLAIM_ALLOWED=false. This is a test packet.
No terrain mount claim is made until the operator records a positive runtime result.

## What this tests

Whether Project Zomboid Build 42 can:
1. Load the pzmapforge_map37c mod (mod_id=pzmapforge_map37c).
2. Register the map folder (media/maps/pzmapforge_map37c) in IsoMetaGrid.
3. Parse chunkdata_35_27.bin (MAP-37B/C shape: 1026 bytes) without fatal error.
4. Spawn the player near the expected coordinate (world 10650, 8250, 0).
5. Mount non-empty terrain at that coordinate.

## Binary evidence from MAP-37C

| Field | Value |
|---|---|
| chunkdata_file | chunkdata_35_27.bin |
| file_size | 1026 bytes |
| header | 0x00 0x01 |
| record_width | 8 |
| record_count | 128 |
| exact_fit | true (2 + 128 * 8 = 1026) |
| deterministic | true (SHA-256 run1 == run2) |

## Docs in this packet

- MAP_37D_HUMAN_INSTALL_STEPS.md       -- install checklist (HUMAN-ONLY)
- MAP_37D_SERVER_WIRING.md             -- Mods= and Map= config lines
- MAP_37D_LOG_CAPTURE_COMMANDS.md      -- log capture commands
- MAP_37D_SUCCESS_FAILURE_CRITERIA.md  -- 7 outcome criteria
- MAP_37D_RUNTIME_RESULT_RECORD.md     -- result recording template

HUMAN_ONLY_INSTALL_REQUIRED=true
CLAUDE_RAN_PZ=false
CLAUDE_WROTE_STEAM=false
CLAUDE_WROTE_WORKSHOP=false
'@ | Out-File $packetDocPath -Encoding utf8

# Step 6: Write human install steps (HUMAN-ONLY — no variables, use @'...'@)
$installDocPath = Join-Path $OutputRoot 'MAP_37D_HUMAN_INSTALL_STEPS.md'
@'
# MAP-37D Human Install Steps

HUMAN-ONLY -- Claude does NOT execute these steps.
All copy operations must be performed manually by the operator.

## Staged candidate location

    .local\deadmtl-authoring\map37d-human-runtime-load-test-packet\map37c-staged\run1\pzmapforge_map37c_build42_candidate\

This folder contains the 42\ mod structure.

## Key binary files

Inside 42\media\maps\pzmapforge_map37c\:
- chunkdata_35_27.bin  (1026 bytes, MAP-37B/C shape)
- 35_27.lotheader
- world_35_27.lotpack

## Install steps (HUMAN-ONLY)

1. HUMAN-ONLY: Locate the staged candidate:
   .local\deadmtl-authoring\map37d-human-runtime-load-test-packet\map37c-staged\run1\pzmapforge_map37c_build42_candidate\

2. HUMAN-ONLY: Copy the entire pzmapforge_map37c_build42_candidate\ folder to your PZ mods directory:
   %USERPROFILE%\Zomboid\mods\pzmapforge_map37c\
   Result: %USERPROFILE%\Zomboid\mods\pzmapforge_map37c\42\mod.info

3. HUMAN-ONLY: Verify chunkdata_35_27.bin is present at:
   %USERPROFILE%\Zomboid\mods\pzmapforge_map37c\42\media\maps\pzmapforge_map37c\chunkdata_35_27.bin

4. Apply the server wiring from MAP_37D_SERVER_WIRING.md.

5. Start a FRESH world (do not reuse any existing world that loaded a different map configuration).

6. Connect to the server and observe the spawn location and terrain.

7. Capture logs per MAP_37D_LOG_CAPTURE_COMMANDS.md.

8. Record the result in MAP_37D_RUNTIME_RESULT_RECORD.md.

## Safety gates

- Do NOT copy files to Steam\steamapps paths.
- Do NOT upload to Workshop.
- Do NOT overwrite vanilla PZ install files.
- Only the %USERPROFILE%\Zomboid\mods\ path is the intended target.

HUMAN_ONLY_INSTALL_REQUIRED=true
CLAUDE_WROTE_STEAM=false
CLAUDE_WROTE_WORKSHOP=false
'@ | Out-File $installDocPath -Encoding utf8

# Step 7: Write server wiring doc (no variables needed — use @'...'@ to avoid backtick issues)
$wiringDocPath = Join-Path $OutputRoot 'MAP_37D_SERVER_WIRING.md'
@'
# MAP-37D Server Wiring

## Mod config lines

Add to your server servertest.ini (or equivalent server config file):

    Mods=pzmapforge_map37c
    Map=pzmapforge_map37c;Muldraugh, KY

- Mods=pzmapforge_map37c loads the staged candidate mod
- Map=pzmapforge_map37c;Muldraugh, KY registers the map + Muldraugh bootstrap

Muldraugh, KY MUST remain as the bootstrap tail (NO_MULDRAUGH_STRATEGY_REJECTED doctrine).

## Expected spawn coordinate

| Field | Value |
|---|---|
| worldX | 35 |
| worldY | 27 |
| posX | 150 |
| posY | 150 |
| posZ | 0 |
| PZ world X | 10650 |
| PZ world Y | 8250 |

Derived: worldX * 300 + posX = 35 * 300 + 150 = 10650
Derived: worldY * 300 + posY = 27 * 300 + 150 = 8250

## Expected map folder

42\media\maps\pzmapforge_map37c\ (inside the mod)

## Expected cell identity

35_27 -- covers chunkdata_35_27.bin, 35_27.lotheader, world_35_27.lotpack

## mod.info fields

    id=pzmapforge_map37c
    name=PZMapForge Build42 Candidate - pzmapforge_map37c

## map.info fields

    lots=pzmapforge_map37c
    fixed2x=true
'@ | Out-File $wiringDocPath -Encoding utf8

# Step 8: Write log capture commands (no variables — use @'...'@)
$logDocPath = Join-Path $OutputRoot 'MAP_37D_LOG_CAPTURE_COMMANDS.md'
@'
# MAP-37D Log Capture Commands

Capture these logs after the runtime test. All commands are HUMAN-ONLY.

## Server log

    %USERPROFILE%\Zomboid\server\Logs\

Look for entries containing:
- pzmapforge_map37c -- mod load confirmation
- IsoMetaGrid -- map folder registration
- chunkdata -- parse attempt (success or failure)
- lotheader -- lotheader parse attempt
- lotpack -- lotpack parse attempt
- Error or Exception -- any parse failure

## Client log

    %USERPROFILE%\Zomboid\Logs\

Look for entries containing:
- pzmapforge_map37c
- WorldX=35 or WorldY=27 -- spawn coordinate confirmation
- 10650 or 8250 -- absolute PZ world coordinate

## Spawn coordinate verification

After spawn, check your in-game coordinates (F11 or debug console):
- Expected: approx world (10650, 8250, 0)
- If you see (10746, 8288) or other Muldraugh coords: MULDRAUGH_FALLBACK

## Key log patterns

| Signal | Meaning |
|---|---|
| pzmapforge_map37c in mod list | MOD_LOADED |
| IsoMetaGrid lists pzmapforge_map37c | MAP_FOLDER_REGISTERED |
| Error on chunkdata/lotheader/lotpack | PARSE_FAIL |
| Spawn at approx (10650, 8250) | SPAWN_METADATA_OK |
| Visible terrain at spawn | TERRAIN_MOUNT_SUCCESS candidate |
| pzmapforge_map37c absent from IsoMetaGrid | MAP_FOLDER_REGISTRATION_FAIL |
'@ | Out-File $logDocPath -Encoding utf8

# Step 9: Write success/failure criteria (no variables — use @'...'@)
$criteriaDocPath = Join-Path $OutputRoot 'MAP_37D_SUCCESS_FAILURE_CRITERIA.md'
@'
# MAP-37D Success/Failure Criteria

Seven outcomes must be distinguished. Record which outcome applies
in MAP_37D_RUNTIME_RESULT_RECORD.md.

## Outcome 1: MOD_NOT_LOADED

Signal: pzmapforge_map37c does NOT appear in the server/client mod list.
Logs: No pzmapforge_map37c in mod load sequence.
Action: Check mod.info path, Mods= line, and local mods folder.
Do NOT claim terrain mount evidence.

## Outcome 2: SPAWN_METADATA_FAIL

Signal: Mod loads, but player spawns at an unexpected coordinate
(not near worldX=35, worldY=27 / PZ world approx 10650, 8250).
Logs: No WorldX=35 or WorldY=27 in client log; spawn coord mismatch.
Action: Inspect spawnpoints.lua content; verify worldX=35 worldY=27 posX=150 posY=150.
Do NOT claim terrain mount evidence.

## Outcome 3: MAP_FOLDER_REGISTRATION_FAIL

Signal: Mod loads and spawn coord is correct, but IsoMetaGrid does NOT list
pzmapforge_map37c in its map folder list.
Logs: IsoMetaGrid map folder list visible in log, pzmapforge_map37c absent.
Action: Layout variant mismatch. Cross-reference MAP-9C variant hypotheses (H1-H7).
Do NOT claim terrain mount evidence.

## Outcome 4: CHUNKDATA_PARSE_FAIL

Signal: Mod loads, map folder registers, but log shows parse error on
chunkdata_35_27.bin, 35_27.lotheader, or world_35_27.lotpack.
Logs: Exception or error message referencing chunkdata / lotheader / lotpack.
Action: MAP-37B/C binary shape (1026 bytes, 00 01 header, 128 zero records) is
the hypothesis. If parse fails, record exact error and binary offset if present.
Do NOT claim terrain mount evidence.

## Outcome 5: MULDRAUGH_FALLBACK

Signal: All above pass, but the visible world is vanilla Muldraugh/Knox County.
No pzmapforge_map37c terrain visible. Player may spawn at Muldraugh coords.
Logs: Spawn near known Muldraugh coordinate (not 10650, 8250).
Action: Map folder registration succeeded but terrain not mounted.
This is the most likely failure mode per MAP-9A/MAP-9C history.
Do NOT claim terrain mount evidence.

## Outcome 6: EMPTY_WORLD

Signal: Player spawns near correct coordinate (10650, 8250) but the world
is empty (water, void, or fully black/missing tiles).
Logs: Spawn coord correct; no chunkdata parse error.
Action: Chunkdata parsed but zero-body records produce no visible terrain.
Record as EMPTY_WORLD; this is a distinct finding from MULDRAUGH_FALLBACK.
Do NOT claim terrain mount evidence.

## Outcome 7: TERRAIN_MOUNT_SUCCESS

Signal: Player spawns near correct coordinate (10650, 8250) AND non-empty
terrain is visible (grass, ground tiles, or any generated terrain tiles).
Logs: Spawn coord correct; no parse errors; visible world at target cell.
Action: Record exact spawn coord, screenshot if possible, log extract.
This is the first positive terrain mount evidence for PZMapForge Build 42.
PLAYABLE_EXPORT_CLAIM_ALLOWED remains false until operator ratifies.
'@ | Out-File $criteriaDocPath -Encoding utf8

# Step 10: Write runtime result record template (no variables — use @'...'@)
$resultDocPath = Join-Path $OutputRoot 'MAP_37D_RUNTIME_RESULT_RECORD.md'
@'
# MAP-37D Runtime Result Record

Complete this document after the human runtime test.

## Test metadata

| Field | Value |
|---|---|
| Test date | YYYY-MM-DD |
| Operator | (operator handle) |
| PZ Build | 42.x.x |
| Server type | Dedicated / Solo |
| World state | Fresh (confirm: yes / no) |
| Mod source | Local mods / Workshop |

## Outcome

Circle one:
- [ ] MOD_NOT_LOADED
- [ ] SPAWN_METADATA_FAIL
- [ ] MAP_FOLDER_REGISTRATION_FAIL
- [ ] CHUNKDATA_PARSE_FAIL
- [ ] MULDRAUGH_FALLBACK
- [ ] EMPTY_WORLD
- [ ] TERRAIN_MOUNT_SUCCESS

## Spawn coordinate observed

| Field | Value |
|---|---|
| Observed world X | |
| Observed world Y | |
| Expected PZ world X | 10650 |
| Expected PZ world Y | 8250 |
| Coord match | yes / no |

## IsoMetaGrid map folder list

pzmapforge_map37c present: yes / no
List contents (copy from log if available):

(paste here)

## Log extract

(paste relevant log lines here)

## Terrain observation

Visible terrain at spawn: yes / no
Description:

## Claim boundary

PLAYABLE_EXPORT_CLAIM_ALLOWED=false (until operator ratifies TERRAIN_MOUNT_SUCCESS)
CLAUDE_RAN_PZ=false
CLAUDE_WROTE_STEAM=false
CLAUDE_WROTE_WORKSHOP=false
'@ | Out-File $resultDocPath -Encoding utf8

Write-Output ""
Write-Output "MAP-37D human runtime load-test packet complete."
Write-Output "  Output: $OutputRoot"
Write-Output "  map37c_binary_evidence_referenced=true"
Write-Output "  chunkdata_sha256: $chunkHash"
Write-Output "  expected_mods_line: Mods=$mapId"
Write-Output "  expected_map_line: Map=${mapId};Muldraugh, KY"
Write-Output "  expected_pz_world: ($pzWorldX, $pzWorldY, $posZ)"
Write-Output "  PLAYABLE_EXPORT_CLAIM_ALLOWED=false"
Write-Output "  HUMAN_ONLY_INSTALL_REQUIRED=true"
Write-Output "  CLAUDE_RAN_PZ=false"
Write-Output "  CLAUDE_WROTE_STEAM=false"
Write-Output "  CLAUDE_WROTE_WORKSHOP=false"
