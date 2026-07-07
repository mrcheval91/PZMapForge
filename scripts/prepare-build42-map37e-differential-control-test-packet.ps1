#Requires -Version 5.1
<#
.SYNOPSIS
    MAP-37E: Prepare a differential control test packet for the MAP-37C chunkdata
    candidate.

    Stages the same MAP-37C candidate twice under .local/:
      Run A (files-present) -- 35_27.lotheader, world_35_27.lotpack, chunkdata_35_27.bin
        are present, unmodified.
      Run B (files-removed) -- the same three candidate binary files are deleted from
        an otherwise identical copy.

    The operator installs and runs both variants with identical mod metadata, map
    token, and spawn coordinate. If both variants render visually similar terrain,
    that is FALLBACK_INDISTINGUISHABLE evidence: visible terrain in Run A cannot be
    attributed to the authored PZMapForge binary files (MAP-9N/MAP-9Q precedent).

    Server wiring uses the MAP-9K/9L/9Q proven mount shape: a single self-referential
    Map= token (no ;Muldraugh, KY chain) plus a direct SpawnPoint= INI coordinate,
    NOT the MAP-9D/MAP-37D shape (Map=<token>;Muldraugh, KY + spawnpoints.lua metadata)
    that MAP-9D showed produces an empty IsoMetaGrid map-folder list.

    Does NOT change chunkdata/lotheader/lotpack binary generation.
    Does NOT write to Steam, Workshop, Project Zomboid install folders, or live server folders.
    All install/copy commands in the produced docs are marked HUMAN-ONLY.

Claim boundary:
    PLAYABLE_EXPORT_CLAIM_ALLOWED=false
    HUMAN_ONLY_INSTALL_REQUIRED=true
    CLAUDE_RAN_PZ=false
    CLAUDE_WROTE_STEAM=false
    CLAUDE_WROTE_WORKSHOP=false
    staged_output_local_only=true
    MAP37E_DIFFERENTIAL_CONTROL_TEST_PACKET_DEFINED
#>
param(
    [string]$OutputRoot = ''
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot  = Split-Path -Parent $scriptDir

if ($OutputRoot -eq '') {
    $OutputRoot = Join-Path $repoRoot '.local\deadmtl-authoring\map37e-differential-control-test-packet'
}

function Assert-LocalPath([string]$p) {
    $localRoot = Join-Path $repoRoot '.local'
    $resolved  = [System.IO.Path]::GetFullPath($p)
    if (-not $resolved.StartsWith([System.IO.Path]::GetFullPath($localRoot), [System.StringComparison]::OrdinalIgnoreCase)) {
        Write-Error "MAP-37E: Output path must be under .local\: $p"
        exit 1
    }
}

Assert-LocalPath $OutputRoot

$mapId       = 'pzmapforge_map37c'
$cellX       = 35
$cellY       = 27
$pzWorldX    = 10746
$pzWorldY    = 8288
$pzWorldZ    = 0
$spawnPointIni = "SpawnPoint=$pzWorldX,$pzWorldY,$pzWorldZ"

New-Item -ItemType Directory -Path $OutputRoot -Force | Out-Null

# Step 1: Run MAP-37C prepare to produce a fresh staged candidate (no binary format changes)
$map37cOut        = Join-Path $OutputRoot 'map37c-staged'
$map37cPrepScript = Join-Path $scriptDir 'prepare-build42-map37c-chunkdata-staged-packet.ps1'

Write-Output "MAP-37E: Generating MAP-37C staged candidate..."
Remove-Item -Recurse -Force $map37cOut -ErrorAction SilentlyContinue
& powershell -ExecutionPolicy Bypass -File $map37cPrepScript -OutputRoot $map37cOut
if ($LASTEXITCODE -ne 0) { throw "MAP-37E: MAP-37C prepare script failed (exit $LASTEXITCODE)" }

$candidateRelRoot = "${mapId}_build42_candidate"
$candidateSrc     = Join-Path $map37cOut "run1\$candidateRelRoot"
if (-not (Test-Path $candidateSrc)) { throw "MAP-37E: staged candidate not found at $candidateSrc" }

$mapFolderRel = "42\media\maps\$mapId"
$candidateFiles = @('35_27.lotheader', 'world_35_27.lotpack', 'chunkdata_35_27.bin')

# Step 2: Stage Run A (files-present) — verbatim copy of the staged candidate
$runARoot = Join-Path $OutputRoot 'run-a-files-present'
Remove-Item -Recurse -Force $runARoot -ErrorAction SilentlyContinue
Copy-Item -Recurse -Force $candidateSrc (Join-Path $runARoot $candidateRelRoot)

$runAMapFolder = Join-Path $runARoot "$candidateRelRoot\$mapFolderRel"
$sha = [System.Security.Cryptography.SHA256]::Create()
$runAEvidence = [ordered]@{}
foreach ($f in $candidateFiles) {
    $fp = Join-Path $runAMapFolder $f
    if (-not (Test-Path $fp)) { throw "MAP-37E: Run A missing expected file $fp" }
    $bytes = [System.IO.File]::ReadAllBytes($fp)
    $hash  = [System.BitConverter]::ToString($sha.ComputeHash($bytes)).Replace('-', '').ToLower()
    $key   = ($f -replace '[^a-zA-Z0-9]', '_')
    $runAEvidence["${key}_sha256"] = $hash
    $runAEvidence["${key}_size"]   = $bytes.Length
}

# Step 3: Stage Run B (files-removed) — identical copy, minus the three candidate files
$runBRoot = Join-Path $OutputRoot 'run-b-files-removed'
Remove-Item -Recurse -Force $runBRoot -ErrorAction SilentlyContinue
Copy-Item -Recurse -Force $candidateSrc (Join-Path $runBRoot $candidateRelRoot)

$runBMapFolder = Join-Path $runBRoot "$candidateRelRoot\$mapFolderRel"
foreach ($f in $candidateFiles) {
    $fp = Join-Path $runBMapFolder $f
    Remove-Item -Force $fp -ErrorAction SilentlyContinue
    if (Test-Path $fp) { throw "MAP-37E: Run B failed to remove $fp" }
}
$sha.Dispose()

$generatedAt = (Get-Date -Format 'yyyy-MM-ddTHH:mm:ssZ')

# Step 4: Write preflight JSON
$jsonPath = Join-Path $OutputRoot 'map37e-differential-control-test-packet.json'
$preflight = [ordered]@{
    schema                            = 'pzmapforge.map37e-differential-control-test-packet.v0.1'
    generated_at_utc                  = $generatedAt
    map_id                            = $mapId
    cell_x                            = $cellX
    cell_y                            = $cellY
    cell_identity                     = "${cellX}_${cellY}"
    map_folder                        = "media\maps\$mapId"
    map_token_matches_staged_folder   = $true
    run_a_label                       = 'files-present'
    run_b_label                       = 'files-removed'
    run_a_subpath                     = "run-a-files-present\$candidateRelRoot"
    run_b_subpath                     = "run-b-files-removed\$candidateRelRoot"
    run_a_lotheader_sha256            = $runAEvidence['35_27_lotheader_sha256']
    run_a_lotheader_size              = $runAEvidence['35_27_lotheader_size']
    run_a_lotpack_sha256              = $runAEvidence['world_35_27_lotpack_sha256']
    run_a_lotpack_size                = $runAEvidence['world_35_27_lotpack_size']
    run_a_chunkdata_sha256            = $runAEvidence['chunkdata_35_27_bin_sha256']
    run_a_chunkdata_size              = $runAEvidence['chunkdata_35_27_bin_size']
    run_b_lotheader_present           = $false
    run_b_lotpack_present              = $false
    run_b_chunkdata_present            = $false
    expected_mods_line                = "Mods=$mapId"
    expected_map_line                 = "Map=$mapId"
    muldraugh_chain_present           = $false
    expected_spawn_point_ini          = $spawnPointIni
    expected_pz_world_x               = $pzWorldX
    expected_pz_world_y               = $pzWorldY
    expected_pz_world_z               = $pzWorldZ
    differential_control_test         = $true
    outcomes_defined                  = 8
    FALLBACK_INDISTINGUISHABLE_defined = $true
    TERRAIN_MOUNT_SUCCESS_narrowed     = $true
    supersedes_criterion               = 'MAP-37D outcome 7 (spawn + visible terrain alone)'
    PLAYABLE_EXPORT_CLAIM_ALLOWED     = $false
    HUMAN_ONLY_INSTALL_REQUIRED       = $true
    CLAUDE_RAN_PZ                     = $false
    CLAUDE_WROTE_STEAM                = $false
    CLAUDE_WROTE_WORKSHOP             = $false
    staged_output_local_only          = $true
}
$preflight | ConvertTo-Json -Depth 4 | Out-File $jsonPath -Encoding utf8

# Step 5: Write preflight MD (uses variables)
$mdPath = Join-Path $OutputRoot 'map37e-differential-control-test-packet.md'
@"
# MAP-37E Differential Control Test Packet

Generated: $generatedAt
Schema: pzmapforge.map37e-differential-control-test-packet.v0.1

## Staged variants

| Field | Value |
|---|---|
| map_id | $mapId |
| cell_identity | ${cellX}_${cellY} |
| run_a_label | files-present |
| run_b_label | files-removed |
| run_a_chunkdata_sha256 | $($runAEvidence['chunkdata_35_27_bin_sha256']) |
| run_a_chunkdata_size | $($runAEvidence['chunkdata_35_27_bin_size']) |
| run_b_chunkdata_present | false |

## Expected server wiring (MAP-9K/9L/9Q proven mount shape)

| Field | Value |
|---|---|
| Mods= | Mods=$mapId |
| Map= | Map=$mapId |
| Muldraugh chain present | false |
| SpawnPoint= | $spawnPointIni |
| PZ world | ($pzWorldX, $pzWorldY, $pzWorldZ) |

## Claim boundary

MAP37E_DIFFERENTIAL_CONTROL_TEST_PACKET_DEFINED
PLAYABLE_EXPORT_CLAIM_ALLOWED=false
HUMAN_ONLY_INSTALL_REQUIRED=true
CLAUDE_RAN_PZ=false
CLAUDE_WROTE_STEAM=false
CLAUDE_WROTE_WORKSHOP=false
staged_output_local_only=true
"@ | Out-File $mdPath -Encoding utf8

# Step 6: Write operator packet doc (no variables — use @'...'@)
$packetDocPath = Join-Path $OutputRoot 'MAP_37E_DIFFERENTIAL_CONTROL_TEST.md'
@'
# MAP-37E Differential Control Test

MAP37E_DIFFERENTIAL_CONTROL_TEST_PACKET_DEFINED

## Why this test exists

MAP-37D's TERRAIN_MOUNT_SUCCESS criterion was "player spawns at the expected
coordinate and terrain is visible." MAP-9N and MAP-9Q already showed that
criterion is insufficient: Project Zomboid Build 42 can render generic
procedural/fallback wilderness even when the generated candidate cell files
are removed entirely. Visible terrain alone does not prove authored content
was loaded.

MAP-37E replaces the single-run test with a two-run differential control test.
The same mod metadata, map token, and spawn coordinate are used for both runs.
Only the presence of the three candidate binary files differs.

## What this tests

Whether visible terrain at the target coordinate can be attributed to the
PZMapForge-generated chunkdata_35_27.bin / 35_27.lotheader / world_35_27.lotpack,
by comparing:

- Run A (files-present): the MAP-37C staged candidate, unmodified.
- Run B (files-removed): an identical install with those three files deleted.

If Run A and Run B look the same, the result is FALLBACK_INDISTINGUISHABLE,
not TERRAIN_MOUNT_SUCCESS.

## Server wiring (proven mount shape)

This packet uses the MAP-9K/9L/9Q wiring shown to actually mount and render:

    Mods=pzmapforge_map37c
    Map=pzmapforge_map37c
    SpawnPoint=10746,8288,0

No ;Muldraugh, KY chain. MAP-9D proved that shape (self-token + Muldraugh chain)
produces an empty IsoMetaGrid map-folder list across five folder layouts.

## Docs in this packet

- MAP_37E_HUMAN_INSTALL_STEPS.md       -- Run A / Run B install checklist (HUMAN-ONLY)
- MAP_37E_SERVER_WIRING.md             -- Mods=, Map=, SpawnPoint= lines
- MAP_37E_LOG_CAPTURE_COMMANDS.md      -- log capture commands
- MAP_37E_SUCCESS_FAILURE_CRITERIA.md  -- 8 outcome criteria (incl. FALLBACK_INDISTINGUISHABLE)
- MAP_37E_RUNTIME_RESULT_RECORD.md     -- two-run result recording template

HUMAN_ONLY_INSTALL_REQUIRED=true
CLAUDE_RAN_PZ=false
CLAUDE_WROTE_STEAM=false
CLAUDE_WROTE_WORKSHOP=false
PLAYABLE_EXPORT_CLAIM_ALLOWED=false
'@ | Out-File $packetDocPath -Encoding utf8

# Step 7: Write human install steps (HUMAN-ONLY — no variables)
$installDocPath = Join-Path $OutputRoot 'MAP_37E_HUMAN_INSTALL_STEPS.md'
@'
# MAP-37E Human Install Steps

HUMAN-ONLY -- Claude does NOT execute these steps.
All copy operations must be performed manually by the operator.
Run A and Run B must be tested as two SEPARATE fresh installs / fresh worlds.

## Staged variant locations

    .local\deadmtl-authoring\map37e-differential-control-test-packet\run-a-files-present\pzmapforge_map37c_build42_candidate\
    .local\deadmtl-authoring\map37e-differential-control-test-packet\run-b-files-removed\pzmapforge_map37c_build42_candidate\

Both contain the same 42\ mod structure. Run B's map folder is missing
35_27.lotheader, world_35_27.lotpack, and chunkdata_35_27.bin.

## Run A -- files-present

1. HUMAN-ONLY: Copy run-a-files-present\pzmapforge_map37c_build42_candidate\
   to your PZ mods directory:
   %USERPROFILE%\Zomboid\mods\pzmapforge_map37c\

2. HUMAN-ONLY: Verify all three files are present at:
   %USERPROFILE%\Zomboid\mods\pzmapforge_map37c\42\media\maps\pzmapforge_map37c\
   - chunkdata_35_27.bin
   - 35_27.lotheader
   - world_35_27.lotpack

3. Apply the server wiring from MAP_37E_SERVER_WIRING.md.

4. Start a FRESH world (do not reuse any existing world).

5. Connect, observe spawn location and terrain, capture logs per
   MAP_37E_LOG_CAPTURE_COMMANDS.md.

6. Record the Run A result in MAP_37E_RUNTIME_RESULT_RECORD.md.

## Run B -- files-removed

7. HUMAN-ONLY: Remove the Run A install:
   %USERPROFILE%\Zomboid\mods\pzmapforge_map37c\

8. HUMAN-ONLY: Copy run-b-files-removed\pzmapforge_map37c_build42_candidate\
   to the same PZ mods directory:
   %USERPROFILE%\Zomboid\mods\pzmapforge_map37c\

9. HUMAN-ONLY: Verify all three files are ABSENT at:
   %USERPROFILE%\Zomboid\mods\pzmapforge_map37c\42\media\maps\pzmapforge_map37c\

10. Apply the SAME server wiring from MAP_37E_SERVER_WIRING.md (identical
    Mods=, Map=, SpawnPoint= lines as Run A).

11. Start a SECOND fresh world (do not reuse Run A's world).

12. Connect, observe spawn location and terrain, capture logs per
    MAP_37E_LOG_CAPTURE_COMMANDS.md.

13. Record the Run B result in MAP_37E_RUNTIME_RESULT_RECORD.md.

## Comparison

14. Compare the Run A and Run B terrain observations. If they look visually
    similar (same generic wilderness, no distinguishing feature), record
    FALLBACK_INDISTINGUISHABLE per MAP_37E_SUCCESS_FAILURE_CRITERIA.md.
    Do NOT record TERRAIN_MOUNT_SUCCESS unless Run A and Run B are
    distinguishable.

## Safety gates

- Do NOT copy files to Steam\steamapps paths.
- Do NOT upload to Workshop.
- Do NOT overwrite vanilla PZ install files.
- Only the %USERPROFILE%\Zomboid\mods\ path is the intended target.

HUMAN_ONLY_INSTALL_REQUIRED=true
CLAUDE_WROTE_STEAM=false
CLAUDE_WROTE_WORKSHOP=false
'@ | Out-File $installDocPath -Encoding utf8

# Step 8: Write server wiring doc (no variables)
$wiringDocPath = Join-Path $OutputRoot 'MAP_37E_SERVER_WIRING.md'
@'
# MAP-37E Server Wiring

Proven mount shape per MAP-9K/9L/9Q. Identical wiring is used for both
Run A (files-present) and Run B (files-removed) -- only the map folder
contents differ.

## Mod config lines

Add to your server servertest.ini (or equivalent server config file):

    Mods=pzmapforge_map37c
    Map=pzmapforge_map37c
    SpawnPoint=10746,8288,0

- Mods=pzmapforge_map37c loads the staged candidate mod.
- Map=pzmapforge_map37c -- single self-referential token. NO ;Muldraugh, KY chain.
- SpawnPoint=10746,8288,0 -- direct INI spawn coordinate (not spawnpoints.lua metadata).

MAP-9D proved that Map=<token>;Muldraugh, KY produces an empty IsoMetaGrid
map-folder list across five folder-layout variants. MAP-9K/9L/9Q proved that
dropping the Muldraugh chain and using a direct SpawnPoint= line reaches a
mounted, rendering state. This packet uses the MAP-9K/9L/9Q shape, not the
MAP-9D/MAP-37D shape.

## Expected spawn coordinate

| Field | Value |
|---|---|
| cellX | 35 |
| cellY | 27 |
| PZ world X | 10746 |
| PZ world Y | 8288 |
| PZ world Z | 0 |

10746 and 8288 fall inside cell 35_27 (world range 10500-10799, 8100-8399),
matching the MAP-9K/9L/9Q coordinate used when terrain last rendered under
this map identity.

## Expected map folder

42\media\maps\pzmapforge_map37c\ (inside the mod) -- token matches the
folder name exactly.

## Expected cell identity

35_27 -- covers chunkdata_35_27.bin, 35_27.lotheader, world_35_27.lotpack in
Run A only. Absent in Run B.

## mod.info fields

    id=pzmapforge_map37c
    name=PZMapForge Build42 Candidate - pzmapforge_map37c

## map.info fields

    lots=pzmapforge_map37c
    fixed2x=true
'@ | Out-File $wiringDocPath -Encoding utf8

# Step 9: Write log capture commands (no variables)
$logDocPath = Join-Path $OutputRoot 'MAP_37E_LOG_CAPTURE_COMMANDS.md'
@'
# MAP-37E Log Capture Commands

Capture these logs after EACH runtime test (Run A and Run B separately).
All commands are HUMAN-ONLY.

## Server log

    %USERPROFILE%\Zomboid\server\Logs\

Look for entries containing:
- pzmapforge_map37c -- mod load confirmation
- IsoMetaGrid -- map folder registration
- chunkdata -- parse attempt (Run A only; Run B has no chunkdata to parse)
- lotheader -- lotheader parse attempt (Run A only)
- lotpack -- lotpack parse attempt (Run A only)
- Error or Exception -- any parse failure

## Client log

    %USERPROFILE%\Zomboid\Logs\

Look for entries containing:
- pzmapforge_map37c
- 10746 or 8288 -- absolute PZ world coordinate

## Spawn coordinate verification

After spawn, check your in-game coordinates (F11 or debug console):
- Expected: approx world (10746, 8288, 0) for BOTH Run A and Run B.
- If you see a different (Muldraugh) coordinate: MULDRAUGH_FALLBACK.

## Visual comparison (the decisive step)

Take a screenshot or written description of the terrain at spawn for both
runs. Place them side by side. This comparison, not either log alone,
determines TERRAIN_MOUNT_SUCCESS vs. FALLBACK_INDISTINGUISHABLE.

## Key log patterns

| Signal | Meaning |
|---|---|
| pzmapforge_map37c in mod list | MOD_LOADED |
| IsoMetaGrid lists pzmapforge_map37c | MAP_FOLDER_REGISTERED |
| Error on chunkdata/lotheader/lotpack (Run A) | CHUNKDATA_PARSE_FAIL |
| Spawn at approx (10746, 8288) in both runs | SPAWN_METADATA_OK |
| Run A and Run B terrain look the same | FALLBACK_INDISTINGUISHABLE |
| Run A and Run B terrain are visibly different | candidate for TERRAIN_MOUNT_SUCCESS |
| pzmapforge_map37c absent from IsoMetaGrid | MAP_FOLDER_REGISTRATION_FAIL |
'@ | Out-File $logDocPath -Encoding utf8

# Step 10: Write success/failure criteria (no variables)
$criteriaDocPath = Join-Path $OutputRoot 'MAP_37E_SUCCESS_FAILURE_CRITERIA.md'
@'
# MAP-37E Success/Failure Criteria

Eight outcomes must be distinguished. Record which outcome applies in
MAP_37E_RUNTIME_RESULT_RECORD.md. Outcomes 1-6 are evaluated per run
(Run A and Run B separately, using identical wiring). Outcomes 7 and 8
are evaluated only after BOTH runs are complete, by comparing them.

## Outcome 1: MOD_NOT_LOADED

Signal: pzmapforge_map37c does NOT appear in the server/client mod list.
Applies to: either run, independently.
Do NOT claim terrain mount evidence.

## Outcome 2: SPAWN_METADATA_FAIL

Signal: Mod loads, but player spawns at an unexpected coordinate
(not near PZ world approx 10746, 8288).
Applies to: either run, independently.
Do NOT claim terrain mount evidence.

## Outcome 3: MAP_FOLDER_REGISTRATION_FAIL

Signal: Mod loads and spawn coord is correct, but IsoMetaGrid does NOT list
pzmapforge_map37c in its map folder list.
Applies to: either run, independently.
Do NOT claim terrain mount evidence.

## Outcome 4: CHUNKDATA_PARSE_FAIL

Signal: Run A only -- mod loads, map folder registers, but log shows parse
error on chunkdata_35_27.bin, 35_27.lotheader, or world_35_27.lotpack.
Not applicable to Run B (those files do not exist in Run B).
Do NOT claim terrain mount evidence.

## Outcome 5: MULDRAUGH_FALLBACK

Signal: All above pass, but the visible world is vanilla Muldraugh/Knox
County, not the target cell. Player may spawn at a Muldraugh coordinate
instead of (10746, 8288).
Applies to: either run, independently.
Do NOT claim terrain mount evidence.

## Outcome 6: EMPTY_WORLD

Signal: Player spawns near correct coordinate (10746, 8288) but the world
is empty (water, void, or fully black/missing tiles).
Applies to: either run, independently.
Do NOT claim terrain mount evidence.

## Outcome 7: TERRAIN_MOUNT_SUCCESS (narrowed -- requires both runs)

Signal: Run A spawns near (10746, 8288) with non-empty visible terrain AND
Run B, under IDENTICAL wiring, does not show the same terrain -- Run B is
empty/void, crashes, errors, or is CLEARLY visually distinguishable from
Run A.

This outcome may be recorded ONLY when Run A and Run B are distinguishable.
"Run A spawned and showed visible terrain" is NOT sufficient by itself.
MAP-9L recorded that exact signal once; MAP-9N/MAP-9Q subsequently showed
generic procedural wilderness renders even when the candidate files are
removed, so single-run "spawn + visible terrain" is not evidence of
authored content. If Run A and Run B look the same, use Outcome 8 instead.

PLAYABLE_EXPORT_CLAIM_ALLOWED remains false even when this outcome applies,
until operator ratifies.

## Outcome 8: FALLBACK_INDISTINGUISHABLE

Signal: Run A (files-present) and Run B (files-removed) both spawn
successfully at approximately (10746, 8288) and show visually similar
wilderness/terrain -- no distinguishing feature between them.

Meaning: visible terrain in Run A cannot be attributed to the authored
PZMapForge binary files. This is the expected result per MAP-9N/MAP-9Q
prior evidence (generic wilderness/forest renders even when generated
35_27 files are removed entirely).

Action: Do NOT record TERRAIN_MOUNT_SUCCESS. The next work is to find a
byte/field that produces a visually distinct result before any further
chunkdata-format claims are meaningful.

PLAYABLE_EXPORT_CLAIM_ALLOWED remains false.
'@ | Out-File $criteriaDocPath -Encoding utf8

# Step 11: Write runtime result record template (no variables)
$resultDocPath = Join-Path $OutputRoot 'MAP_37E_RUNTIME_RESULT_RECORD.md'
@'
# MAP-37E Runtime Result Record

Complete this document after BOTH human runtime tests (Run A and Run B).

## Test metadata

| Field | Value |
|---|---|
| Test date | YYYY-MM-DD |
| Operator | (operator handle) |
| PZ Build | 42.x.x |
| Server type | Dedicated / Solo |
| Mod source | Local mods |

## Run A -- files-present

| Field | Value |
|---|---|
| World state | Fresh (confirm: yes / no) |
| Mod loaded | yes / no |
| IsoMetaGrid lists pzmapforge_map37c | yes / no |
| Observed spawn coordinate | |
| Coord matches (10746, 8288) | yes / no |
| Chunkdata/lotheader/lotpack parse error observed | yes / no |
| Terrain visible at spawn | yes / no |
| Terrain description | |

## Run B -- files-removed

| Field | Value |
|---|---|
| World state | Fresh, separate from Run A (confirm: yes / no) |
| Mod loaded | yes / no |
| IsoMetaGrid lists pzmapforge_map37c | yes / no |
| Observed spawn coordinate | |
| Coord matches (10746, 8288) | yes / no |
| Terrain visible at spawn | yes / no |
| Terrain description | |

## Comparison

Run A and Run B visually distinguishable: yes / no

If NO: outcome is FALLBACK_INDISTINGUISHABLE. Do not record TERRAIN_MOUNT_SUCCESS.
If YES: describe the difference, then evaluate against Outcome 7 (TERRAIN_MOUNT_SUCCESS).

## Outcome

Circle one:
- [ ] MOD_NOT_LOADED
- [ ] SPAWN_METADATA_FAIL
- [ ] MAP_FOLDER_REGISTRATION_FAIL
- [ ] CHUNKDATA_PARSE_FAIL
- [ ] MULDRAUGH_FALLBACK
- [ ] EMPTY_WORLD
- [ ] TERRAIN_MOUNT_SUCCESS (only if Run A/Run B distinguishable)
- [ ] FALLBACK_INDISTINGUISHABLE (expected result per MAP-9N/MAP-9Q precedent)

## Log extract

(paste relevant log lines here, both runs)

## Claim boundary

PLAYABLE_EXPORT_CLAIM_ALLOWED=false (until operator ratifies TERRAIN_MOUNT_SUCCESS)
CLAUDE_RAN_PZ=false
CLAUDE_WROTE_STEAM=false
CLAUDE_WROTE_WORKSHOP=false
'@ | Out-File $resultDocPath -Encoding utf8

Write-Output ""
Write-Output "MAP-37E differential control test packet complete."
Write-Output "  Output: $OutputRoot"
Write-Output "  run_a_chunkdata_sha256: $($runAEvidence['chunkdata_35_27_bin_sha256'])"
Write-Output "  run_b_chunkdata_present: false"
Write-Output "  expected_mods_line: Mods=$mapId"
Write-Output "  expected_map_line: Map=$mapId (no Muldraugh chain)"
Write-Output "  expected_spawn_point_ini: $spawnPointIni"
Write-Output "  PLAYABLE_EXPORT_CLAIM_ALLOWED=false"
Write-Output "  HUMAN_ONLY_INSTALL_REQUIRED=true"
Write-Output "  CLAUDE_RAN_PZ=false"
Write-Output "  CLAUDE_WROTE_STEAM=false"
Write-Output "  CLAUDE_WROTE_WORKSHOP=false"
