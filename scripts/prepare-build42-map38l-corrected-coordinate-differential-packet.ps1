#Requires -Version 5.1
<#
.SYNOPSIS
    MAP-38L: Differential control test packet for renderable_v1, using the
    MAP-38J coordinate fix (Build 42's real 256-tile cell grid, not Build
    41's 300-tile grid).

    Supersedes MAP-38B's packet, which used the wrong 300-based coordinate
    math and returned an uninterpreted (not disproven) FALLBACK_INDISTINGUISHABLE
    result (MAP-38C). This packet targets cell 34_26 -- the cell MAP-38K
    already confirmed rendering on -- at the CORRECT coordinate
    (cellX*256+128, cellY*256+128).

    Stages one renderable_v1 candidate (generated live via the CLI's
    map-export-experimental --build42-candidate-writer --build42-candidate-profile
    renderable_v1 command, which already includes the MAP-38D/G/H/J fixes)
    twice under .local/:
      Run A (files-present) -- 34_26.lotheader, world_34_26.lotpack,
        chunkdata_34_26.bin present, unmodified.
      Run B (files-removed) -- the same three candidate binary files deleted.

    Does NOT change the renderable_v1 writer. Does NOT write to Steam,
    Workshop, Project Zomboid install folders, or live server folders. All
    install/copy commands in the produced docs are marked HUMAN-ONLY.

Claim boundary:
    PLAYABLE_EXPORT_CLAIM_ALLOWED=false
    HUMAN_ONLY_INSTALL_REQUIRED=true
    CLAUDE_RAN_PZ=false
    CLAUDE_WROTE_STEAM=false
    CLAUDE_WROTE_WORKSHOP=false
    staged_output_local_only=true
    MAP38L_CORRECTED_COORDINATE_DIFFERENTIAL_PACKET_DEFINED
#>
param(
    [string]$OutputRoot = ''
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot  = Split-Path -Parent $scriptDir

if ($OutputRoot -eq '') {
    $OutputRoot = Join-Path $repoRoot '.local\deadmtl-authoring\map38l-corrected-coordinate-differential-packet'
}

function Assert-LocalPath([string]$p) {
    $localRoot = Join-Path $repoRoot '.local'
    $resolved  = [System.IO.Path]::GetFullPath($p)
    if (-not $resolved.StartsWith([System.IO.Path]::GetFullPath($localRoot), [System.StringComparison]::OrdinalIgnoreCase)) {
        Write-Error "MAP-38L: Output path must be under .local\: $p"
        exit 1
    }
}

Assert-LocalPath $OutputRoot

$mapId    = 'pzmapforge_map38l'
$cellX    = 34
$cellY    = 26
# MAP-38J math: Build 42's real 256-tile cell grid, not Build 41's 300-tile grid.
$pzWorldX = $cellX * 256 + 128
$pzWorldY = $cellY * 256 + 128
$pzWorldZ = 0
$spawnPointIni = "SpawnPoint=$pzWorldX,$pzWorldY,$pzWorldZ"

New-Item -ItemType Directory -Path $OutputRoot -Force | Out-Null

# Step 1: Generate a fresh renderable_v1 candidate via the actual CLI writer
# (includes the MAP-38D/G/H/J fixes -- real tile, real spawn object, real
# chunk encoding, corrected coordinate math).
$genOut     = Join-Path $OutputRoot 'renderable-v1-generated'
$cliProject = Join-Path $repoRoot 'src\PZMapForge.Cli'

Write-Output "MAP-38L: Generating renderable_v1 candidate via CLI writer (corrected coordinate)..."
Remove-Item -Recurse -Force $genOut -ErrorAction SilentlyContinue
& dotnet run --project $cliProject -- map-export-experimental `
    --map-id $mapId --output $genOut `
    --build42-candidate-writer --build42-candidate-profile renderable_v1 `
    --cell-x $cellX --cell-y $cellY
if ($LASTEXITCODE -ne 0) { throw "MAP-38L: renderable_v1 CLI generation failed (exit $LASTEXITCODE)" }

$candidateRelRoot = "${mapId}_build42_candidate"
$candidateSrc     = Join-Path $genOut $candidateRelRoot
if (-not (Test-Path $candidateSrc)) { throw "MAP-38L: generated candidate not found at $candidateSrc" }

$mapFolderRel   = "common\media\maps\$mapId"
$candidateFiles = @("${cellX}_${cellY}.lotheader", "world_${cellX}_${cellY}.lotpack", "chunkdata_${cellX}_${cellY}.bin")

# Sanity check: confirm the generated spawnpoints.lua/objects.lua actually carry the
# corrected coordinate before staging anything -- fail loudly if MAP-38J regresses.
$objectsLuaPath = Join-Path $candidateSrc "$mapFolderRel\objects.lua"
$objectsLuaText = Get-Content -LiteralPath $objectsLuaPath -Raw
if ($objectsLuaText -notmatch "x = $pzWorldX, y = $pzWorldY") {
    throw "MAP-38L: generated objects.lua does not contain the expected corrected coordinate (x = $pzWorldX, y = $pzWorldY). MAP-38J's coordinate fix may have regressed."
}

# Step 2: Stage Run A (files-present) -- verbatim copy of the generated candidate
$runARoot = Join-Path $OutputRoot 'run-a-files-present'
Remove-Item -Recurse -Force $runARoot -ErrorAction SilentlyContinue
Copy-Item -Recurse -Force $candidateSrc (Join-Path $runARoot $candidateRelRoot)

$runAMapFolder = Join-Path $runARoot "$candidateRelRoot\$mapFolderRel"
$sha = [System.Security.Cryptography.SHA256]::Create()
$runAEvidence = [ordered]@{}
foreach ($f in $candidateFiles) {
    $fp = Join-Path $runAMapFolder $f
    if (-not (Test-Path $fp)) { throw "MAP-38L: Run A missing expected file $fp" }
    $bytes = [System.IO.File]::ReadAllBytes($fp)
    $hash  = [System.BitConverter]::ToString($sha.ComputeHash($bytes)).Replace('-', '').ToLower()
    $key   = ($f -replace '[^a-zA-Z0-9]', '_')
    $runAEvidence["${key}_sha256"] = $hash
    $runAEvidence["${key}_size"]   = $bytes.Length
}

# Step 3: Stage Run B (files-removed) -- identical copy, minus the three candidate files
$runBRoot = Join-Path $OutputRoot 'run-b-files-removed'
Remove-Item -Recurse -Force $runBRoot -ErrorAction SilentlyContinue
Copy-Item -Recurse -Force $candidateSrc (Join-Path $runBRoot $candidateRelRoot)

$runBMapFolder = Join-Path $runBRoot "$candidateRelRoot\$mapFolderRel"
foreach ($f in $candidateFiles) {
    $fp = Join-Path $runBMapFolder $f
    Remove-Item -Force $fp -ErrorAction SilentlyContinue
    if (Test-Path $fp) { throw "MAP-38L: Run B failed to remove $fp" }
}
$sha.Dispose()

$lotheaderKey = "${cellX}_${cellY}_lotheader"
$lotpackKey   = "world_${cellX}_${cellY}_lotpack"
$chunkKey     = "chunkdata_${cellX}_${cellY}_bin"

$generatedAt = (Get-Date -Format 'yyyy-MM-ddTHH:mm:ssZ')

# Step 4: Write preflight JSON
$jsonPath = Join-Path $OutputRoot 'map38l-corrected-coordinate-differential-packet.json'
$preflight = [ordered]@{
    schema                          = 'pzmapforge.map38l-corrected-coordinate-differential-packet.v0.1'
    generated_at_utc                = $generatedAt
    map_id                          = $mapId
    cell_x                          = $cellX
    cell_y                          = $cellY
    cell_identity                   = "${cellX}_${cellY}"
    profile                         = 'renderable_v1'
    map_folder                      = "common\media\maps\$mapId"
    coordinate_math                 = 'build42_256_tile_cell_grid_map38j_fix'
    coordinate_math_superseded      = 'build41_300_tile_cell_grid_used_by_map38b_and_every_prior_test'
    run_a_label                     = 'files-present'
    run_b_label                     = 'files-removed'
    run_a_subpath                   = "run-a-files-present\$candidateRelRoot"
    run_b_subpath                   = "run-b-files-removed\$candidateRelRoot"
    run_a_lotheader_sha256          = $runAEvidence["${lotheaderKey}_sha256"]
    run_a_lotheader_size            = $runAEvidence["${lotheaderKey}_size"]
    run_a_lotpack_sha256            = $runAEvidence["${lotpackKey}_sha256"]
    run_a_lotpack_size              = $runAEvidence["${lotpackKey}_size"]
    run_a_chunkdata_sha256          = $runAEvidence["${chunkKey}_sha256"]
    run_a_chunkdata_size            = $runAEvidence["${chunkKey}_size"]
    run_b_lotheader_present         = $false
    run_b_lotpack_present            = $false
    run_b_chunkdata_present          = $false
    expected_spawn_point_ini        = $spawnPointIni
    expected_pz_world_x             = $pzWorldX
    expected_pz_world_y             = $pzWorldY
    expected_pz_world_z             = $pzWorldZ
    distinctive_marker_tile         = 'floors_rugs_01_0'
    distinctive_marker_tile_index   = 4
    expected_visual_signal          = 'endless_repeating_street_or_indoor_floor_tile_pattern'
    prior_confirmation              = 'pzmapforge_map38j at this same cell and coordinate, human-confirmed 2026-07-10 (MAP-38K)'
    differential_control_test       = $true
    outcomes_defined                = 8
    PLAYABLE_EXPORT_CLAIM_ALLOWED   = $false
    HUMAN_ONLY_INSTALL_REQUIRED     = $true
    CLAUDE_RAN_PZ                   = $false
    CLAUDE_WROTE_STEAM              = $false
    CLAUDE_WROTE_WORKSHOP           = $false
    staged_output_local_only        = $true
}
$preflight | ConvertTo-Json -Depth 4 | Out-File $jsonPath -Encoding utf8

# Step 5: Write operator packet doc (no variables -- use @'...'@)
$packetDocPath = Join-Path $OutputRoot 'MAP_38L_CORRECTED_COORDINATE_DIFFERENTIAL_PACKET.md'
@'
# MAP-38L Corrected-Coordinate Differential Control Test

MAP38L_CORRECTED_COORDINATE_DIFFERENTIAL_PACKET_DEFINED

## Why this packet exists

MAP-38B's differential packet used cellX*300+offset coordinate math --
Build 41's cell width, not Build 42's. MAP-38J found and fixed this (Build 42
cells are 256x256 tiles, confirmed via pzwiki.net/wiki/Mapping). MAP-38K
confirmed renderable_v1 actually renders at the corrected coordinate: cell
34_26, candidate pzmapforge_map38j, operator observed "endless repeating
street/indoor tile" -- the real floors_rugs_01_0 marker tile.

That confirmation was ONE human test, not yet a differential control test.
This packet reruns the MAP-37E/MAP-38B differential pattern (Run A vs Run B)
at the SAME cell and corrected coordinate, to convert MAP-38K's single
confirmation into repeatable evidence before promoting renderable_v1 to
Ratified.

## Expected result if MAP-38K's finding is real

- Run A (files-present): endless repeating street/indoor floor tile pattern
  at spawn -- unmistakably artificial, not procedural forest.
- Run B (files-removed): procedural fallback (forest, scattered
  shrubs/trees, or the real underlying vanilla content for that coordinate
  if it happens to be populated) -- NOT the repeating tile pattern.

If Run A shows the repeating pattern and Run B does not, that is
RENDERABLE_MOUNT_SUCCESS, properly differential this time. If both runs look
the same, MAP-38K's confirmation does not reproduce and needs its own
investigation.

## Docs in this packet

- MAP_38L_HUMAN_INSTALL_STEPS.md       -- Run A / Run B install checklist (HUMAN-ONLY)
- MAP_38L_RUNTIME_RESULT_RECORD.md     -- two-run result recording template

HUMAN_ONLY_INSTALL_REQUIRED=true
CLAUDE_RAN_PZ=false
CLAUDE_WROTE_STEAM=false
CLAUDE_WROTE_WORKSHOP=false
PLAYABLE_EXPORT_CLAIM_ALLOWED=false
'@ | Out-File $packetDocPath -Encoding utf8

# Step 6: Write human install steps (HUMAN-ONLY -- no variables)
$installDocPath = Join-Path $OutputRoot 'MAP_38L_HUMAN_INSTALL_STEPS.md'
@'
# MAP-38L Human Install Steps

HUMAN-ONLY -- Claude does NOT execute these steps.
Run A and Run B must be tested as two SEPARATE fresh Solo games. Use the
SAME mod folder name for both (do not run them side by side under different
names -- that risks a duplicate-mod-ID conflict).

## Staged variant locations

    .local\deadmtl-authoring\map38l-corrected-coordinate-differential-packet\run-a-files-present\pzmapforge_map38l_build42_candidate\
    .local\deadmtl-authoring\map38l-corrected-coordinate-differential-packet\run-b-files-removed\pzmapforge_map38l_build42_candidate\

## Run A -- files-present

1. HUMAN-ONLY: Copy run-a-files-present\pzmapforge_map38l_build42_candidate\
   to %USERPROFILE%\Zomboid\mods\pzmapforge_map38l\

2. Full restart of the game (not just a new Solo game from the same running
   session -- PZ can cache mod file contents across in-process New Game
   attempts).

3. Fresh Solo game, select pzmapforge_map38l as the starting location.

4. Check your on-screen coordinate (F11 or debug console). Expected: close
   to the value in MAP_38L_HUMAN_INSTALL_STEPS's companion JSON
   (expected_pz_world_x / expected_pz_world_y).

5. Look at the ground: endless repeating street/indoor floor tile pattern
   expected, per MAP-38K.

6. Record the Run A result in MAP_38L_RUNTIME_RESULT_RECORD.md.

## Run B -- files-removed

7. HUMAN-ONLY: Remove the Run A install:
   %USERPROFILE%\Zomboid\mods\pzmapforge_map38l\

8. HUMAN-ONLY: Copy run-b-files-removed\pzmapforge_map38l_build42_candidate\
   to the same path.

9. Full restart again.

10. Fresh Solo game (a SECOND new game, not a continuation of Run A's),
    select pzmapforge_map38l again.

11. Check the same coordinate. Look at the ground -- should NOT show the
    repeating tile pattern this time.

12. Record the Run B result and compare.

## Safety gates

- Do NOT copy files to Steam\steamapps paths.
- Do NOT upload to Workshop.
- Only the %USERPROFILE%\Zomboid\mods\ path is the intended target.

HUMAN_ONLY_INSTALL_REQUIRED=true
CLAUDE_WROTE_STEAM=false
CLAUDE_WROTE_WORKSHOP=false
'@ | Out-File $installDocPath -Encoding utf8

# Step 7: Write runtime result record template (no variables)
$resultDocPath = Join-Path $OutputRoot 'MAP_38L_RUNTIME_RESULT_RECORD.md'
@'
# MAP-38L Runtime Result Record

## Run A -- files-present

| Field | Value |
|---|---|
| World state | Fresh (confirm: yes / no) |
| Observed spawn coordinate | |
| Coordinate matches expected | yes / no |
| Repeating street/indoor tile pattern observed | yes / no |
| Terrain description | |

## Run B -- files-removed

| Field | Value |
|---|---|
| World state | Fresh, separate from Run A (confirm: yes / no) |
| Observed spawn coordinate | |
| Repeating tile pattern observed | yes / no |
| Terrain description | |

## Outcome

- [ ] RENDERABLE_MOUNT_SUCCESS (Run A shows pattern, Run B does not)
- [ ] FALLBACK_INDISTINGUISHABLE (both runs look the same)

## Claim boundary

PLAYABLE_EXPORT_CLAIM_ALLOWED=false (until operator ratifies)
CLAUDE_RAN_PZ=false
'@ | Out-File $resultDocPath -Encoding utf8

Write-Output ""
Write-Output "MAP-38L corrected-coordinate differential packet complete."
Write-Output "  Output: $OutputRoot"
Write-Output "  expected_spawn_point_ini: $spawnPointIni"
Write-Output "  run_a_chunkdata_sha256: $($runAEvidence["${chunkKey}_sha256"])"
Write-Output "  run_b_chunkdata_present: false"
Write-Output "  PLAYABLE_EXPORT_CLAIM_ALLOWED=false"
Write-Output "  HUMAN_ONLY_INSTALL_REQUIRED=true"
