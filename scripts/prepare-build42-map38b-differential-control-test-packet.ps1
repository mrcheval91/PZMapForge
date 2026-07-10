#Requires -Version 5.1
<#
.SYNOPSIS
    MAP-38B: Prepare a differential control test packet for the MAP-38A
    renderable_v1 Build 42 candidate profile.

    Stages one renderable_v1 candidate (generated live via the CLI's
    map-export-experimental --build42-candidate-writer --build42-candidate-profile
    renderable_v1 command) twice under .local/:
      Run A (files-present) -- 35_27.lotheader, world_35_27.lotpack,
        chunkdata_35_27.bin are present, unmodified, under
        common/media/maps/<mapId>/ (the MAP-38A folder-structure fix).
      Run B (files-removed) -- the same three candidate binary files are
        deleted from an otherwise identical copy.

    This reworks the MAP-37E differential-control-test pattern for the new
    renderable_v1 format (MAP-38A). MAP-37E tested the old MAP-37B/C
    zero-body chunkdata shape and returned FALLBACK_INDISTINGUISHABLE.
    renderable_v1 fixed three structural bugs (folder layout, map.info lots=,
    real lotpack chunk payload) plus one content-choice bug (the
    unofficial_fork_map_0 distinctive marker tile, baked into this profile's
    default lotheader/lotpack output) and was confirmed rendering a visibly
    artificial, grid-aligned pattern in ONE unrepeatable human runtime test.
    This packet exists to make that confirmation repeatable and differential,
    per this repo's own evidence-discipline rule (a passing test suite does
    not promote provisional behavior to canon).

    Does NOT change the renderable_v1 writer or chunkdata/lotheader/lotpack
    binary generation. Does NOT write to Steam, Workshop, Project Zomboid
    install folders, or live server folders. All install/copy commands in
    the produced docs are marked HUMAN-ONLY.

Claim boundary:
    PLAYABLE_EXPORT_CLAIM_ALLOWED=false
    HUMAN_ONLY_INSTALL_REQUIRED=true
    CLAUDE_RAN_PZ=false
    CLAUDE_WROTE_STEAM=false
    CLAUDE_WROTE_WORKSHOP=false
    staged_output_local_only=true
    MAP38B_DIFFERENTIAL_CONTROL_TEST_PACKET_DEFINED
#>
param(
    [string]$OutputRoot = ''
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot  = Split-Path -Parent $scriptDir

if ($OutputRoot -eq '') {
    $OutputRoot = Join-Path $repoRoot '.local\deadmtl-authoring\map38b-differential-control-test-packet'
}

function Assert-LocalPath([string]$p) {
    $localRoot = Join-Path $repoRoot '.local'
    $resolved  = [System.IO.Path]::GetFullPath($p)
    if (-not $resolved.StartsWith([System.IO.Path]::GetFullPath($localRoot), [System.StringComparison]::OrdinalIgnoreCase)) {
        Write-Error "MAP-38B: Output path must be under .local\: $p"
        exit 1
    }
}

Assert-LocalPath $OutputRoot

$mapId       = 'pzmapforge_map38b'
$cellX       = 35
$cellY       = 27
$pzWorldX    = 10746
$pzWorldY    = 8288
$pzWorldZ    = 0
$spawnPointIni = "SpawnPoint=$pzWorldX,$pzWorldY,$pzWorldZ"

New-Item -ItemType Directory -Path $OutputRoot -Force | Out-Null

# Step 1: Generate a fresh renderable_v1 candidate via the actual CLI writer
# (the same command path MAP-38A's C# port uses -- no hand-crafted scaffolding).
$genOut      = Join-Path $OutputRoot 'renderable-v1-generated'
$cliProject  = Join-Path $repoRoot 'src\PZMapForge.Cli'

Write-Output "MAP-38B: Generating renderable_v1 candidate via CLI writer..."
Remove-Item -Recurse -Force $genOut -ErrorAction SilentlyContinue
& dotnet run --project $cliProject -- map-export-experimental `
    --map-id $mapId --output $genOut `
    --build42-candidate-writer --build42-candidate-profile renderable_v1 `
    --cell-x $cellX --cell-y $cellY
if ($LASTEXITCODE -ne 0) { throw "MAP-38B: renderable_v1 CLI generation failed (exit $LASTEXITCODE)" }

$candidateRelRoot = "${mapId}_build42_candidate"
$candidateSrc     = Join-Path $genOut $candidateRelRoot
if (-not (Test-Path $candidateSrc)) { throw "MAP-38B: generated candidate not found at $candidateSrc" }

$mapFolderRel = "common\media\maps\$mapId"
$candidateFiles = @("${cellX}_${cellY}.lotheader", "world_${cellX}_${cellY}.lotpack", "chunkdata_${cellX}_${cellY}.bin")

# Step 2: Stage Run A (files-present) — verbatim copy of the generated candidate
$runARoot = Join-Path $OutputRoot 'run-a-files-present'
Remove-Item -Recurse -Force $runARoot -ErrorAction SilentlyContinue
Copy-Item -Recurse -Force $candidateSrc (Join-Path $runARoot $candidateRelRoot)

$runAMapFolder = Join-Path $runARoot "$candidateRelRoot\$mapFolderRel"
$sha = [System.Security.Cryptography.SHA256]::Create()
$runAEvidence = [ordered]@{}
foreach ($f in $candidateFiles) {
    $fp = Join-Path $runAMapFolder $f
    if (-not (Test-Path $fp)) { throw "MAP-38B: Run A missing expected file $fp" }
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
    if (Test-Path $fp) { throw "MAP-38B: Run B failed to remove $fp" }
}
$sha.Dispose()

$lotheaderKey = "${cellX}_${cellY}_lotheader"
$lotpackKey   = "world_${cellX}_${cellY}_lotpack"
$chunkKey     = "chunkdata_${cellX}_${cellY}_bin"

$generatedAt = (Get-Date -Format 'yyyy-MM-ddTHH:mm:ssZ')

# Step 4: Write preflight JSON
$jsonPath = Join-Path $OutputRoot 'map38b-differential-control-test-packet.json'
$preflight = [ordered]@{
    schema                             = 'pzmapforge.map38b-differential-control-test-packet.v0.1'
    generated_at_utc                   = $generatedAt
    map_id                             = $mapId
    cell_x                             = $cellX
    cell_y                             = $cellY
    cell_identity                      = "${cellX}_${cellY}"
    profile                            = 'renderable_v1'
    map_folder                         = "common\media\maps\$mapId"
    map_folder_structure_fixed         = $true
    map_token_matches_staged_folder    = $true
    run_a_label                        = 'files-present'
    run_b_label                        = 'files-removed'
    run_a_subpath                      = "run-a-files-present\$candidateRelRoot"
    run_b_subpath                      = "run-b-files-removed\$candidateRelRoot"
    run_a_lotheader_sha256             = $runAEvidence["${lotheaderKey}_sha256"]
    run_a_lotheader_size               = $runAEvidence["${lotheaderKey}_size"]
    run_a_lotpack_sha256               = $runAEvidence["${lotpackKey}_sha256"]
    run_a_lotpack_size                 = $runAEvidence["${lotpackKey}_size"]
    run_a_chunkdata_sha256             = $runAEvidence["${chunkKey}_sha256"]
    run_a_chunkdata_size               = $runAEvidence["${chunkKey}_size"]
    run_b_lotheader_present            = $false
    run_b_lotpack_present               = $false
    run_b_chunkdata_present             = $false
    expected_mods_line                 = "Mods=$mapId"
    expected_map_line                  = "Map=$mapId"
    muldraugh_chain_present            = $false
    map_info_lots_value                = 'Muldraugh, KY'
    distinctive_marker_tile            = 'unofficial_fork_map_0'
    distinctive_marker_tile_index      = 4
    expected_visual_signal             = 'square_grid_aligned_vegetation_patches_not_generic_wilderness'
    expected_spawn_point_ini           = $spawnPointIni
    expected_pz_world_x                = $pzWorldX
    expected_pz_world_y                = $pzWorldY
    expected_pz_world_z                = $pzWorldZ
    differential_control_test          = $true
    outcomes_defined                   = 8
    FALLBACK_INDISTINGUISHABLE_defined = $true
    RENDERABLE_MOUNT_SUCCESS_narrowed   = $true
    supersedes_criterion               = 'MAP-38A single unrepeatable human runtime test'
    prior_related_result               = 'MAP-37E (old MAP-37B/C zero-body format) returned FALLBACK_INDISTINGUISHABLE'
    PLAYABLE_EXPORT_CLAIM_ALLOWED      = $false
    HUMAN_ONLY_INSTALL_REQUIRED        = $true
    CLAUDE_RAN_PZ                      = $false
    CLAUDE_WROTE_STEAM                 = $false
    CLAUDE_WROTE_WORKSHOP              = $false
    staged_output_local_only           = $true
}
$preflight | ConvertTo-Json -Depth 4 | Out-File $jsonPath -Encoding utf8

# Step 5: Write preflight MD (uses variables)
$mdPath = Join-Path $OutputRoot 'map38b-differential-control-test-packet.md'
@"
# MAP-38B Differential Control Test Packet

Generated: $generatedAt
Schema: pzmapforge.map38b-differential-control-test-packet.v0.1

## Staged variants

| Field | Value |
|---|---|
| map_id | $mapId |
| profile | renderable_v1 |
| cell_identity | ${cellX}_${cellY} |
| run_a_label | files-present |
| run_b_label | files-removed |
| run_a_chunkdata_sha256 | $($runAEvidence["${chunkKey}_sha256"]) |
| run_a_chunkdata_size | $($runAEvidence["${chunkKey}_size"]) |
| run_b_chunkdata_present | false |

## Expected server wiring

| Field | Value |
|---|---|
| Mods= | Mods=$mapId |
| Map= | Map=$mapId |
| Muldraugh chain present | false |
| map.info lots= | Muldraugh, KY |
| SpawnPoint= | $spawnPointIni |
| PZ world | ($pzWorldX, $pzWorldY, $pzWorldZ) |

## Expected visual signal (MAP-38A lesson)

square_grid_aligned_vegetation_patches_not_generic_wilderness -- do NOT accept
"terrain looks different" alone; natural/blend content is indistinguishable
from PZ's own procedural fallback. Look specifically for the artificial,
grid-regular pattern produced by the unofficial_fork_map_0 marker tile
(lotheader index 4).

## Claim boundary

MAP38B_DIFFERENTIAL_CONTROL_TEST_PACKET_DEFINED
PLAYABLE_EXPORT_CLAIM_ALLOWED=false
HUMAN_ONLY_INSTALL_REQUIRED=true
CLAUDE_RAN_PZ=false
CLAUDE_WROTE_STEAM=false
CLAUDE_WROTE_WORKSHOP=false
staged_output_local_only=true
"@ | Out-File $mdPath -Encoding utf8

# Step 6: Write operator packet doc (no variables — use @'...'@)
$packetDocPath = Join-Path $OutputRoot 'MAP_38B_DIFFERENTIAL_CONTROL_TEST.md'
@'
# MAP-38B Differential Control Test

MAP38B_DIFFERENTIAL_CONTROL_TEST_PACKET_DEFINED

## Why this test exists

MAP-38A found and fixed the reason no PZMapForge Build 42 candidate had ever
rendered distinctly: wrong folder structure, wrong map.info lots= value, a
flat zero-payload lotpack, and (the costliest mistake) test content that was
visually indistinguishable from Build 42's own procedural wilderness
fallback. The fix was confirmed in ONE unrepeatable human runtime test.

MAP-37E already proved, for the OLD chunkdata format, that a single-run
"spawn + visible terrain" test cannot distinguish authored content from
fallback wilderness (that test returned FALLBACK_INDISTINGUISHABLE). This
repo's own evidence-discipline rule -- a passing test suite does not promote
provisional behavior to canon -- applies just as much to a single passing
human test. MAP-38B reworks the same differential pattern for renderable_v1:
run the identical wiring twice, once with the candidate binary files present
and once with them removed, and compare.

## What this tests

Whether the visibly artificial, grid-aligned vegetation pattern observed in
the MAP-38A test can be attributed to the renderable_v1-generated
`35_27.lotheader` / `world_35_27.lotpack` / `chunkdata_35_27.bin`, using:

- Run A (files-present): a freshly-generated renderable_v1 candidate,
  unmodified.
- Run B (files-removed): an identical install with those three files
  deleted.

Both runs use the fixed folder structure (`common/media/maps/<mapId>/`) and
identical mod metadata, map token, and server wiring.

## Expected visual signal -- do not settle for "looks different"

MAP-38A's own record identifies its costliest mistake: natural/blend ground
tiles are format-correct AND likely render, but look like generic wilderness
either way. The distinguishing signal is specifically the artificial,
grid-regular vegetation pattern produced by the `unofficial_fork_map_0`
marker tile (lotheader entry index 4, baked into this profile's default
output). "Run A terrain differs from Run B" is necessary but not sufficient
-- confirm the grid-aligned pattern specifically, not just any difference.

## Server wiring (unchanged mount shape, fixed map.info)

    Mods=pzmapforge_map38b
    Map=pzmapforge_map38b
    SpawnPoint=10746,8288,0

No `;Muldraugh, KY` chain on the `Map=` line (MAP-9K/9L/9Q proven mount
shape, reused unchanged by MAP-37E and this packet). Separately, inside the
mod's own `map.info`, `lots=Muldraugh, KY` -- this is the MAP-38A fix and is
not the same field as the server's `Map=` token.

## Docs in this packet

- MAP_38B_HUMAN_INSTALL_STEPS.md       -- Run A / Run B install checklist (HUMAN-ONLY)
- MAP_38B_SERVER_WIRING.md             -- Mods=, Map=, map.info lots=, SpawnPoint= lines
- MAP_38B_LOG_CAPTURE_COMMANDS.md      -- log capture commands
- MAP_38B_SUCCESS_FAILURE_CRITERIA.md  -- 8 outcome criteria (incl. FALLBACK_INDISTINGUISHABLE)
- MAP_38B_RUNTIME_RESULT_RECORD.md     -- two-run result recording template

HUMAN_ONLY_INSTALL_REQUIRED=true
CLAUDE_RAN_PZ=false
CLAUDE_WROTE_STEAM=false
CLAUDE_WROTE_WORKSHOP=false
PLAYABLE_EXPORT_CLAIM_ALLOWED=false
'@ | Out-File $packetDocPath -Encoding utf8

# Step 7: Write human install steps (HUMAN-ONLY — no variables)
$installDocPath = Join-Path $OutputRoot 'MAP_38B_HUMAN_INSTALL_STEPS.md'
@'
# MAP-38B Human Install Steps

HUMAN-ONLY -- Claude does NOT execute these steps.
All copy operations must be performed manually by the operator.
Run A and Run B must be tested as two SEPARATE fresh installs / fresh worlds.
Ideally use a DIFFERENT populated cell than 35_27 for at least one of the two
independent confirmations this packet is meant to support (MAP-38A's own
recommendation), by re-running the prepare script with edited cellX/cellY
and re-verifying the new cell is populated before testing.

## Staged variant locations

    .local\deadmtl-authoring\map38b-differential-control-test-packet\run-a-files-present\pzmapforge_map38b_build42_candidate\
    .local\deadmtl-authoring\map38b-differential-control-test-packet\run-b-files-removed\pzmapforge_map38b_build42_candidate\

Both contain the same common/ + 42/ mod structure (MAP-38A folder-structure
fix). Run B's map folder is missing 35_27.lotheader, world_35_27.lotpack,
and chunkdata_35_27.bin.

## Run A -- files-present

1. HUMAN-ONLY: Copy run-a-files-present\pzmapforge_map38b_build42_candidate\
   to your PZ mods directory:
   %USERPROFILE%\Zomboid\mods\pzmapforge_map38b\

2. HUMAN-ONLY: Verify all three files are present at:
   %USERPROFILE%\Zomboid\mods\pzmapforge_map38b\common\media\maps\pzmapforge_map38b\
   - chunkdata_35_27.bin
   - 35_27.lotheader
   - world_35_27.lotpack

3. Apply the server wiring from MAP_38B_SERVER_WIRING.md.

4. Start a FRESH world (do not reuse any existing world).

5. Connect, observe spawn location and terrain, capture logs per
   MAP_38B_LOG_CAPTURE_COMMANDS.md. Look specifically for the grid-aligned
   vegetation pattern described in MAP_38B_SUCCESS_FAILURE_CRITERIA.md, not
   just "some terrain."

6. Record the Run A result in MAP_38B_RUNTIME_RESULT_RECORD.md.

## Run B -- files-removed

7. HUMAN-ONLY: Remove the Run A install:
   %USERPROFILE%\Zomboid\mods\pzmapforge_map38b\

8. HUMAN-ONLY: Copy run-b-files-removed\pzmapforge_map38b_build42_candidate\
   to the same PZ mods directory:
   %USERPROFILE%\Zomboid\mods\pzmapforge_map38b\

9. HUMAN-ONLY: Verify all three files are ABSENT at:
   %USERPROFILE%\Zomboid\mods\pzmapforge_map38b\common\media\maps\pzmapforge_map38b\

10. Apply the SAME server wiring from MAP_38B_SERVER_WIRING.md (identical
    Mods=, Map=, SpawnPoint= lines as Run A).

11. Start a SECOND fresh world (do not reuse Run A's world).

12. Connect, observe spawn location and terrain, capture logs per
    MAP_38B_LOG_CAPTURE_COMMANDS.md.

13. Record the Run B result in MAP_38B_RUNTIME_RESULT_RECORD.md.

## Comparison

14. Compare the Run A and Run B terrain observations. Confirm specifically
    whether the grid-aligned vegetation pattern from Run A is ABSENT in
    Run B (expected, since Run B has no lotheader/lotpack/chunkdata at all).
    If Run A and Run B look visually similar -- both generic wilderness, no
    grid-aligned pattern in either -- record FALLBACK_INDISTINGUISHABLE per
    MAP_38B_SUCCESS_FAILURE_CRITERIA.md. Do NOT record RENDERABLE_MOUNT_SUCCESS
    unless the specific grid-aligned pattern is observed in Run A and absent
    in Run B.

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
$wiringDocPath = Join-Path $OutputRoot 'MAP_38B_SERVER_WIRING.md'
@'
# MAP-38B Server Wiring

Reuses the MAP-9K/9L/9Q/MAP-37E proven mount shape for the server ini
Mods=/Map= tokens, plus the MAP-38A map.info fix. Identical wiring is used
for both Run A (files-present) and Run B (files-removed) -- only the map
folder contents differ.

## Mod config lines (server ini)

Add to your server servertest.ini (or equivalent server config file):

    Mods=pzmapforge_map38b
    Map=pzmapforge_map38b
    SpawnPoint=10746,8288,0

- Mods=pzmapforge_map38b loads the staged candidate mod.
- Map=pzmapforge_map38b -- single self-referential token. NO ;Muldraugh, KY chain.
- SpawnPoint=10746,8288,0 -- direct INI spawn coordinate (not spawnpoints.lua metadata).

MAP-9D proved that Map=<token>;Muldraugh, KY produces an empty IsoMetaGrid
map-folder list across five folder-layout variants. MAP-9K/9L/9Q proved that
dropping the Muldraugh chain and using a direct SpawnPoint= line reaches a
mounted, rendering state. This field is unchanged by MAP-38A.

## map.info lots= (MAP-38A fix -- a DIFFERENT field from Map=)

Inside the mod's own map folder (`common\media\maps\pzmapforge_map38b\map.info`):

    lots=Muldraugh, KY
    fixed2x=true

Every prior profile (empty_grass_v0-v5) used a self-referential
lots=pzmapforge_<...>, which real Build 42 sub-town map.info files never do.
Real cells inside the vanilla coordinate space need lots=Muldraugh, KY --
confirmed against real vanilla Rosewood/West Point/Riverside map.info files
and a working community sample mod's own map.info. Do not confuse this
field with the server ini Map= token above; they serve different purposes.

## Expected spawn coordinate

| Field | Value |
|---|---|
| cellX | 35 |
| cellY | 27 |
| PZ world X | 10746 |
| PZ world Y | 8288 |
| PZ world Z | 0 |

10746 and 8288 fall inside cell 35_27 (world range 10500-10799, 8100-8399),
the same cell used in the confirmed MAP-38A human runtime test (Riverside Rd).

## Expected map folder (MAP-38A folder-structure fix)

common\media\maps\pzmapforge_map38b\ (inside the mod) -- cell data lives
under common/, NOT under 42/media/maps/ like every prior profile. The 42\
version folder holds only mod.info + poster.png (duplicated into common\
as well).

## Expected cell identity

35_27 -- covers chunkdata_35_27.bin, 35_27.lotheader, world_35_27.lotpack in
Run A only. Absent in Run B.

## mod.info fields

    id=pzmapforge_map38b
    name=PZMapForge Build42 Candidate - pzmapforge_map38b
'@ | Out-File $wiringDocPath -Encoding utf8

# Step 9: Write log capture commands (no variables)
$logDocPath = Join-Path $OutputRoot 'MAP_38B_LOG_CAPTURE_COMMANDS.md'
@'
# MAP-38B Log Capture Commands

Capture these logs after EACH runtime test (Run A and Run B separately).
All commands are HUMAN-ONLY.

## Server log

    %USERPROFILE%\Zomboid\server\Logs\

Look for entries containing:
- pzmapforge_map38b -- mod load confirmation
- IsoMetaGrid -- map folder registration
- chunkdata -- parse attempt (Run A only; Run B has no chunkdata to parse)
- lotheader -- lotheader parse attempt (Run A only)
- lotpack -- lotpack parse attempt (Run A only)
- Error or Exception -- any parse failure

## Client log

    %USERPROFILE%\Zomboid\Logs\

Look for entries containing:
- pzmapforge_map38b
- 10746 or 8288 -- absolute PZ world coordinate

## Spawn coordinate verification

After spawn, check your in-game coordinates (F11 or debug console):
- Expected: approx world (10746, 8288, 0) for BOTH Run A and Run B.
- If you see a different (Muldraugh) coordinate: MULDRAUGH_FALLBACK.

## Visual comparison (the decisive step)

Take a screenshot or written description of the terrain at spawn for both
runs. The decisive signal is NOT "the terrain differs" in general -- it is
whether a visibly artificial, grid-aligned patch pattern (square, regular
patches of vegetation, not procedural/organic placement) is present in Run A
and absent in Run B. Natural/blend ground tiles alone look like generic
wilderness in either run per MAP-38A's own recorded lesson.

## Key log patterns

| Signal | Meaning |
|---|---|
| pzmapforge_map38b in mod list | MOD_LOADED |
| IsoMetaGrid lists pzmapforge_map38b | MAP_FOLDER_REGISTERED |
| Error on chunkdata/lotheader/lotpack (Run A) | CHUNKDATA_PARSE_FAIL |
| Spawn at approx (10746, 8288) in both runs | SPAWN_METADATA_OK |
| Grid-aligned vegetation pattern in Run A, absent in Run B | candidate for RENDERABLE_MOUNT_SUCCESS |
| Run A and Run B terrain look the same (no grid pattern in either) | FALLBACK_INDISTINGUISHABLE |
| pzmapforge_map38b absent from IsoMetaGrid | MAP_FOLDER_REGISTRATION_FAIL |
'@ | Out-File $logDocPath -Encoding utf8

# Step 10: Write success/failure criteria (no variables)
$criteriaDocPath = Join-Path $OutputRoot 'MAP_38B_SUCCESS_FAILURE_CRITERIA.md'
@'
# MAP-38B Success/Failure Criteria

Eight outcomes must be distinguished. Record which outcome applies in
MAP_38B_RUNTIME_RESULT_RECORD.md. Outcomes 1-6 are evaluated per run
(Run A and Run B separately, using identical wiring). Outcomes 7 and 8
are evaluated only after BOTH runs are complete, by comparing them.

## Outcome 1: MOD_NOT_LOADED

Signal: pzmapforge_map38b does NOT appear in the server/client mod list.
Applies to: either run, independently.
Do NOT claim terrain mount evidence.

## Outcome 2: SPAWN_METADATA_FAIL

Signal: Mod loads, but player spawns at an unexpected coordinate
(not near PZ world approx 10746, 8288).
Applies to: either run, independently.
Do NOT claim terrain mount evidence.

## Outcome 3: MAP_FOLDER_REGISTRATION_FAIL

Signal: Mod loads and spawn coord is correct, but IsoMetaGrid does NOT list
pzmapforge_map38b in its map folder list.
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

## Outcome 7: RENDERABLE_MOUNT_SUCCESS (narrowed -- requires both runs AND the specific visual signal)

Signal: Run A spawns near (10746, 8288) and shows the SPECIFIC visibly
artificial, grid-aligned vegetation patch pattern (not generic/organic
wilderness) that the unofficial_fork_map_0 marker tile produced in the
original MAP-38A test, AND Run B, under IDENTICAL wiring, does not show
that same grid-aligned pattern (Run B may be empty/void/errored, or may
show only generic fallback wilderness).

This outcome requires BOTH: (a) Run A and Run B are distinguishable, AND
(b) the distinguishing feature in Run A is specifically the grid-regular
patch pattern, not merely "different-looking terrain." MAP-38A's own record
identifies "terrain looks different" as an insufficient and previously
misleading signal -- natural/blend tiles are format-correct and may render,
but are visually indistinguishable from procedural fallback. If Run A shows
only generic wilderness (even if different from Run B in some minor way),
use Outcome 8 instead.

This is the renderable_v1 analogue of MAP-37E's TERRAIN_MOUNT_SUCCESS
outcome, narrowed further per the MAP-38A content-choice lesson.

PLAYABLE_EXPORT_CLAIM_ALLOWED remains false even when this outcome applies,
until operator ratifies with a repeatable/second-cell confirmation.

## Outcome 8: FALLBACK_INDISTINGUISHABLE

Signal: Run A (files-present) and Run B (files-removed) both spawn
successfully at approximately (10746, 8288) and show visually similar
wilderness/terrain, OR Run A shows terrain that differs from Run B but
without the specific grid-aligned marker-tile pattern -- no clear
grid-regular vegetation signal in either run.

Meaning: the MAP-38A finding does not reproduce under this packet's staged
candidate. Visible terrain in Run A cannot be attributed to the authored
renderable_v1 binary files.

Action: Do NOT record RENDERABLE_MOUNT_SUCCESS. Re-check the staged files
against the confirmed MAP-38A byte-level format spec
(docs/MAP_38A_RENDERABLE_V1_FORMAT_DISCOVERY.md) before assuming the format
itself regressed -- also check PZ Build version drift since the original
2026-07-08 test.

PLAYABLE_EXPORT_CLAIM_ALLOWED remains false.
'@ | Out-File $criteriaDocPath -Encoding utf8

# Step 11: Write runtime result record template (no variables)
$resultDocPath = Join-Path $OutputRoot 'MAP_38B_RUNTIME_RESULT_RECORD.md'
@'
# MAP-38B Runtime Result Record

Complete this document after BOTH human runtime tests (Run A and Run B).

## Test metadata

| Field | Value |
|---|---|
| Test date | YYYY-MM-DD |
| Operator | (operator handle) |
| PZ Build | 42.x.x |
| Server type | Dedicated / Solo |
| Mod source | Local mods |
| Cell tested | 35_27 (default) or (record actual cellX_cellY if changed) |

## Run A -- files-present

| Field | Value |
|---|---|
| World state | Fresh (confirm: yes / no) |
| Mod loaded | yes / no |
| IsoMetaGrid lists pzmapforge_map38b | yes / no |
| Observed spawn coordinate | |
| Coord matches (10746, 8288) | yes / no |
| Chunkdata/lotheader/lotpack parse error observed | yes / no |
| Terrain visible at spawn | yes / no |
| Grid-aligned vegetation patch pattern observed (specific signal) | yes / no |
| Terrain description | |

## Run B -- files-removed

| Field | Value |
|---|---|
| World state | Fresh, separate from Run A (confirm: yes / no) |
| Mod loaded | yes / no |
| IsoMetaGrid lists pzmapforge_map38b | yes / no |
| Observed spawn coordinate | |
| Coord matches (10746, 8288) | yes / no |
| Terrain visible at spawn | yes / no |
| Grid-aligned vegetation patch pattern observed | yes / no |
| Terrain description | |

## Comparison

Run A shows grid-aligned pattern AND Run B does not: yes / no

If NO: outcome is FALLBACK_INDISTINGUISHABLE. Do not record RENDERABLE_MOUNT_SUCCESS.
If YES: describe the difference, then evaluate against Outcome 7 (RENDERABLE_MOUNT_SUCCESS).

## Outcome

Circle one:
- [ ] MOD_NOT_LOADED
- [ ] SPAWN_METADATA_FAIL
- [ ] MAP_FOLDER_REGISTRATION_FAIL
- [ ] CHUNKDATA_PARSE_FAIL
- [ ] MULDRAUGH_FALLBACK
- [ ] EMPTY_WORLD
- [ ] RENDERABLE_MOUNT_SUCCESS (only if grid-aligned pattern present in A, absent in B)
- [ ] FALLBACK_INDISTINGUISHABLE

## Log extract

(paste relevant log lines here, both runs)

## Claim boundary

PLAYABLE_EXPORT_CLAIM_ALLOWED=false (until operator ratifies RENDERABLE_MOUNT_SUCCESS with a second independent confirmation)
CLAUDE_RAN_PZ=false
CLAUDE_WROTE_STEAM=false
CLAUDE_WROTE_WORKSHOP=false
'@ | Out-File $resultDocPath -Encoding utf8

Write-Output ""
Write-Output "MAP-38B differential control test packet complete."
Write-Output "  Output: $OutputRoot"
Write-Output "  run_a_chunkdata_sha256: $($runAEvidence["${chunkKey}_sha256"])"
Write-Output "  run_b_chunkdata_present: false"
Write-Output "  expected_mods_line: Mods=$mapId"
Write-Output "  expected_map_line: Map=$mapId (no Muldraugh chain)"
Write-Output "  map_info_lots_value: Muldraugh, KY"
Write-Output "  expected_spawn_point_ini: $spawnPointIni"
Write-Output "  PLAYABLE_EXPORT_CLAIM_ALLOWED=false"
Write-Output "  HUMAN_ONLY_INSTALL_REQUIRED=true"
Write-Output "  CLAUDE_RAN_PZ=false"
Write-Output "  CLAUDE_WROTE_STEAM=false"
Write-Output "  CLAUDE_WROTE_WORKSHOP=false"
