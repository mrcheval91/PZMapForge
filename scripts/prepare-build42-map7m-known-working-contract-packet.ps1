#Requires -Version 5.1
<#
.SYNOPSIS
    MAP-7M: Generates the current candidate (experiment-H layout), creates a
    placeholder reference folder, and produces a diagnostic packet.

    The comparator (inspect-build42-known-working-map-contract.ps1) is NOT
    run automatically because no known-working reference mod is available yet.
    The human must first copy or export a known-working Build 42 map mod
    into the reference folder, then run the comparator manually.

    All output under .local/ only.
    Does NOT copy files to PZ folders.
    Does NOT write outside .local/.
    Does NOT run PZ.

.PARAMETER Output
    Required. Path under .local/ for packet output.

.PARAMETER MapId
    Map ID. Default: pzmapforge_build42_candidate_v4_001

.PARAMETER ModFolderName
    Mod folder name for reference. Default: pzmapforge_build42_candidate_v4_001

.EXAMPLE
    powershell -ExecutionPolicy Bypass -File .\scripts\prepare-build42-map7m-known-working-contract-packet.ps1 `
        -Output .\.local\map7m-packet
#>

param(
    [Parameter(Mandatory=$true)][string]$Output,
    [string]$MapId         = 'pzmapforge_build42_candidate_v4_001',
    [string]$ModFolderName = 'pzmapforge_build42_candidate_v4_001'
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

$fence = '```'

# ---------------------------------------------------------------------------
# Step 1: Generate empty_grass_v4 candidate (experiment-H layout: common/)
# ---------------------------------------------------------------------------

$candidateOut = Join-Path $Output 'candidate'
Write-Output "Generating empty_grass_v4 candidate..."

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
$src42Dir   = Join-Path $candDir '42'
$srcMapData = Join-Path $src42Dir "media\maps\$MapId"

# Build experiment-H candidate (common/ layout, same as MAP-7L)
$expHBase         = Join-Path $Output ('experiment-h-candidate\' + $MapId)
$expH42Dir        = Join-Path $expHBase '42'
$expHCommonDir    = Join-Path $expHBase 'common'
$expHCommonMaps   = Join-Path $expHCommonDir "media\maps\$MapId"
$expHMapsSubdir   = Join-Path $expHCommonMaps 'maps'

New-Item -ItemType Directory -Force -Path $expH42Dir      | Out-Null
New-Item -ItemType Directory -Force -Path $expHCommonMaps | Out-Null
New-Item -ItemType Directory -Force -Path $expHMapsSubdir | Out-Null

Copy-Item -LiteralPath (Join-Path $src42Dir 'mod.info') `
          -Destination (Join-Path $expH42Dir 'mod.info') -Force
Copy-Item -LiteralPath (Join-Path $src42Dir 'mod.info') `
          -Destination (Join-Path $expHCommonDir 'mod.info') -Force

$mapDataFiles = @('map.info', 'spawnpoints.lua', 'objects.lua',
                  '0_0.lotheader', 'world_0_0.lotpack', 'chunkdata_0_0.bin')
foreach ($f in $mapDataFiles) {
    $src = Join-Path $srcMapData $f
    if (Test-Path -LiteralPath $src) {
        Copy-Item -LiteralPath $src -Destination (Join-Path $expHCommonMaps $f) -Force
    }
}

# Minimal placeholder files
$thumbPath = Join-Path $expHCommonMaps 'thumb.png'
try {
    Add-Type -AssemblyName System.Drawing -ErrorAction Stop
    $bmp = New-Object System.Drawing.Bitmap(1, 1)
    $bmp.SetPixel(0, 0, [System.Drawing.Color]::White)
    $bmp.Save($thumbPath, [System.Drawing.Imaging.ImageFormat]::Png)
    $bmp.Dispose()
} catch { [System.IO.File]::WriteAllBytes($thumbPath, [byte[]]@()) }

[System.IO.File]::WriteAllText((Join-Path $expHCommonMaps 'worldmap.xml'),
    "<?xml version=""1.0"" encoding=""UTF-8""?>`n<worldmap />`n",
    [System.Text.UTF8Encoding]::new($false))
[System.IO.File]::WriteAllText((Join-Path $expHCommonMaps 'worldmap-forest.xml'),
    "<?xml version=""1.0"" encoding=""UTF-8""?>`n<worldmap />`n",
    [System.Text.UTF8Encoding]::new($false))

$biomemapPath = Join-Path $expHMapsSubdir 'biomemap_0_0.png'
try {
    Add-Type -AssemblyName System.Drawing -ErrorAction Stop
    $bmp2 = New-Object System.Drawing.Bitmap(1, 1)
    $bmp2.SetPixel(0, 0, [System.Drawing.Color]::White)
    $bmp2.Save($biomemapPath, [System.Drawing.Imaging.ImageFormat]::Png)
    $bmp2.Dispose()
} catch { [System.IO.File]::WriteAllBytes($biomemapPath, [byte[]]@()) }

Write-Output "Experiment-H candidate: $expHBase"

# ---------------------------------------------------------------------------
# Step 2: Create reference placeholder folder
# ---------------------------------------------------------------------------

$refBase = Join-Path $Output 'reference-known-working-map'
New-Item -ItemType Directory -Force -Path $refBase | Out-Null

$refReadme = Join-Path $refBase 'README-place-reference-here.md'
Set-Content -Path $refReadme -Encoding ASCII -Value @"
# MAP-7M Reference Placeholder

Place the known-working Build 42 map mod folder here.

Instructions:
1. Obtain a Build 42 map mod that is confirmed to appear in IsoMetaGrid
   map-folder scan (i.e., map_folders_list_empty=false observed in a retest).
2. Copy the mod's root folder (the folder that contains 42/ or common/) into:
   $refBase
3. After copying, run the comparator:

${fence}powershell
powershell -ExecutionPolicy Bypass ``
  -File .\scripts\inspect-build42-known-working-map-contract.ps1 ``
  -CandidateRoot "$expHBase" ``
  -ReferenceRoot "<reference mod root under .local>" ``
  -Output "$Output\comparison" ``
  -MapId $MapId ``
  -ReferenceMapId "<map folder name inside the reference mod>"
${fence}

Use -ReferenceMapId when the reference map folder name differs from $MapId.

Do NOT copy the mod from your PZ Workshop folder directly by path reference.
The comparator ONLY reads from .local/ paths.
PUBLIC_PLAYABLE_CLAIM_ALLOWED=false
"@
Write-Output "Reference placeholder: $refBase"

# ---------------------------------------------------------------------------
# Packet files
# ---------------------------------------------------------------------------

# MAP_7M_KNOWN_WORKING_CONTRACT_PACKET.md
$packetMd = Join-Path $Output 'MAP_7M_KNOWN_WORKING_CONTRACT_PACKET.md'
Set-Content -Path $packetMd -Encoding ASCII -Value @"
# MAP-7M: Known-Working Map Contract Packet

${fence}text
MAP7F_VARIANT_H_MAP_FOLDER_SCAN_EMPTY
COMMON_LAYOUT_ALONE_INSUFFICIENT
VARIANTS_ABCDEFGH_EXHAUSTED
MAP_FOLDER_DISCOVERY_CONTRACT_UNKNOWN
KNOWN_WORKING_MAP_COMPARATOR_REQUIRED
LOAD_TEST_NOT_PERFORMED_BY_THIS_SCRIPT
PUBLIC_PLAYABLE_CLAIM_ALLOWED=false
${fence}

## Variants A through H exhausted

VARIANTS_ABCDEFGH_EXHAUSTED: all eight structural experiments produced
empty IsoMetaGrid map folder scan. common/media/maps layout alone is
not sufficient.

| Variant | Change | Result |
|---|---|---|
| A | Map= candidate;Muldraugh | SCAN_EMPTY |
| B | Map= candidate only | SCAN_EMPTY |
| C | Map= Muldraugh;candidate | SCAN_EMPTY |
| D | + root media/maps/ | SCAN_EMPTY |
| E | + root mod.info | SCAN_EMPTY |
| F | folder name == mod.info id | SCAN_EMPTY |
| G | mod.info map= field | SCAN_EMPTY |
| H | common/media/maps layout | SCAN_EMPTY |

## Key diagnostic signals

- map_folders_list_empty=true is the DECISIVE signal.
- No city choice is NOT decisive for server retests.
- Forest/fallback world = game loaded, NOT custom map registered.
- Player death/respawn = world runs, NOT map registered.
- Do NOT investigate LOTH/LOTP/chunkdata until map folder is registered.

## Comparator not yet run

The known-working map comparator has NOT been run automatically because
no known-working reference mod is available.

Candidate (experiment-H layout): $expHBase
Reference placeholder:           $refBase

See MAP_7M_REFERENCE_CAPTURE_INSTRUCTIONS.md for how to provide the reference.

## Run comparator (after reference is placed)

${fence}powershell
powershell -ExecutionPolicy Bypass ``
  -File .\scripts\inspect-build42-known-working-map-contract.ps1 ``
  -CandidateRoot "$expHBase" ``
  -ReferenceRoot "$refBase\<working-map-mod>" ``
  -Output "$Output\comparison" ``
  -MapId $MapId ``
  -ReferenceMapId "<reference map id here>"
${fence}

Use -ReferenceMapId when the reference mod has a different map folder name
than the candidate. Example: -MapId pzmapforge_build42_candidate_v4_001
-ReferenceMapId Dru_map.
Both CandidateRoot and ReferenceRoot must be under .local/.
The comparator does NOT read PZ install or Workshop paths.

LOAD_TEST_NOT_PERFORMED_BY_THIS_SCRIPT
PUBLIC_PLAYABLE_CLAIM_ALLOWED=false: binding.
"@

# MAP_7M_VARIANT_H_RESULT_SUMMARY.md
$variantHMd = Join-Path $Output 'MAP_7M_VARIANT_H_RESULT_SUMMARY.md'
Set-Content -Path $variantHMd -Encoding ASCII -Value @"
# MAP-7M: Variant H Result Summary

MAP7F_VARIANT_H_MAP_FOLDER_SCAN_EMPTY
COMMON_LAYOUT_ALONE_INSUFFICIENT
VARIANTS_ABCDEFGH_EXHAUSTED

Variant H:
  Layout: common/media/maps/$MapId/ (documented Build 42 structure)
  Includes: thumb.png, worldmap.xml, worldmap-forest.xml, maps/biomemap_0_0.png
  Result: IsoMetaGrid map-folder scan still empty
  candidate_loaded: true
  player_data_received: true
  map_folders_list_empty: true
  spawn_building_warning: true
  Human obs: no city choice, forest/fallback world, player died (respawn offered)

DECISIVE SIGNAL: map_folders_list_empty=true.
NOT decisive: no city choice, forest world, player death.
These confirm the world/server runs; they do NOT confirm map registration.

Do NOT investigate binary format (LOTH/LOTP/chunkdata) yet.
IsoMetaGrid never reaches the candidate folder.

Next: known-working map comparator.
PUBLIC_PLAYABLE_CLAIM_ALLOWED=false
"@

# MAP_7M_REFERENCE_CAPTURE_INSTRUCTIONS.md
$refInstructMd = Join-Path $Output 'MAP_7M_REFERENCE_CAPTURE_INSTRUCTIONS.md'
Set-Content -Path $refInstructMd -Encoding ASCII -Value @"
# MAP-7M: Reference Capture Instructions

To use the known-working map comparator, a human must provide a reference
Build 42 map mod that is confirmed to appear in IsoMetaGrid's map-folder
scan list.

## What you need

A Build 42 map mod folder where you have personally observed:
${fence}text
Looking in these map folders:
  <map name here>
<End of map-folders list>
${fence}
(map_folders_list_empty=false in the analyzer output)

## Where to place the reference

Place the mod folder contents under:
${fence}text
$refBase
${fence}

Do NOT reference Workshop or Zomboid mods paths directly.
The comparator reads ONLY from .local/ paths.

## Example copy command (HUMAN-ONLY)

${fence}powershell
Copy-Item -Recurse -Force `
  "C:\Users\YourUser\Zomboid\mods\<working-map-mod>" `
  "$refBase\<working-map-mod>"
${fence}

Replace YourUser and working-map-mod with your actual paths.

## Run comparator after copying

${fence}powershell
powershell -ExecutionPolicy Bypass ``
  -File .\scripts\inspect-build42-known-working-map-contract.ps1 ``
  -CandidateRoot "$expHBase" ``
  -ReferenceRoot "$refBase\<working-map-mod>" ``
  -Output "$Output\comparison" ``
  -MapId $MapId
${fence}

PUBLIC_PLAYABLE_CLAIM_ALLOWED=false
"@

# MAP_7M_NEXT_DECISION_TREE.md
$decisionMd = Join-Path $Output 'MAP_7M_NEXT_DECISION_TREE.md'
Set-Content -Path $decisionMd -Encoding ASCII -Value @"
# MAP-7M: Next Decision Tree

After running the known-working map comparator, apply this decision tree.

## Based on comparator output

| Comparator finding | Next task |
|---|---|
| mod.info fields in reference not in candidate | Metadata contract alignment -- add missing fields |
| map.info fields in reference not in candidate | map.info contract alignment -- add missing fields |
| Reference uses 42.0/ but candidate uses 42/ | Exact version-folder experiment (Variant I: use 42.0/) |
| Reference has Workshop-only metadata | Local-mod vs Workshop registration investigation |
| Candidate lacks files that reference has | Add missing non-binary placeholders |
| Contract matches and no structural differences | Isolated clean-client/server contamination test |

## What NEVER to do

- Do not modify LOTH/LOTP/chunkdata binary files before map folder is registered.
- Do not claim playable export.
- Do not run PZ automatically.
- Do not read from PZ Workshop or mods paths directly.

PUBLIC_PLAYABLE_CLAIM_ALLOWED=false
LOAD_TEST_NOT_PERFORMED
"@

# ---------------------------------------------------------------------------
# map7m-preflight.json
# ---------------------------------------------------------------------------

$preflightJson = Join-Path $Output 'map7m-preflight.json'
$preflight = [ordered]@{
    schema                              = 'pzmapforge.map7m-preflight.v0.1'
    map_id                              = $MapId
    mod_folder_name                     = $ModFolderName
    variant_h_result                    = 'MAP7F_VARIANT_H_MAP_FOLDER_SCAN_EMPTY'
    common_layout_alone_insufficient    = $true
    variants_abcdefgh_exhausted         = $true
    map_folder_discovery_contract_unknown = $true
    known_working_comparator_required   = $true
    candidate_path                      = $expHBase
    reference_placeholder_path          = $refBase
    comparator_run                      = $false
    no_automatic_pz_read_or_write       = $true
    all_pz_folder_writes_human_only     = $true
    load_test_not_performed             = $true
    public_playable_claim_allowed       = $false
    binary_writer_not_changed           = $true
}
$preflight | ConvertTo-Json -Depth 4 | Set-Content -Path $preflightJson -Encoding UTF8
Write-Output "JSON: $preflightJson"

# map7m-preflight.md
$preflightMd = Join-Path $Output 'map7m-preflight.md'
Set-Content -Path $preflightMd -Encoding ASCII -Value @"
# MAP-7M Preflight

${fence}text
map_id=$MapId
variant_h_result=MAP7F_VARIANT_H_MAP_FOLDER_SCAN_EMPTY
common_layout_alone_insufficient=true
variants_abcdefgh_exhausted=true
map_folder_discovery_contract_unknown=true
known_working_comparator_required=true
comparator_run=false
no_automatic_pz_read_or_write=true
all_pz_folder_writes_human_only=true
load_test_not_performed=true
public_playable_claim_allowed=false
${fence}

LOAD_TEST_NOT_PERFORMED_BY_THIS_SCRIPT
PUBLIC_PLAYABLE_CLAIM_ALLOWED=false: binding.
"@
Write-Output "MD: $preflightMd"

# ---------------------------------------------------------------------------
# Summary
# ---------------------------------------------------------------------------

Write-Output ""
Write-Output "MAP_7M_KNOWN_WORKING_CONTRACT_PACKET.md: $(Test-Path $packetMd)"
Write-Output "MAP_7M_VARIANT_H_RESULT_SUMMARY.md:      $(Test-Path $variantHMd)"
Write-Output "MAP_7M_REFERENCE_CAPTURE_INSTRUCTIONS.md: $(Test-Path $refInstructMd)"
Write-Output "MAP_7M_NEXT_DECISION_TREE.md:             $(Test-Path $decisionMd)"
Write-Output "map7m-preflight.json:                    $(Test-Path $preflightJson)"
Write-Output "map7m-preflight.md:                      $(Test-Path $preflightMd)"
Write-Output ""
Write-Output "MAP7F_VARIANT_H_MAP_FOLDER_SCAN_EMPTY"
Write-Output "VARIANTS_ABCDEFGH_EXHAUSTED"
Write-Output "KNOWN_WORKING_MAP_COMPARATOR_REQUIRED"
Write-Output "LOAD_TEST_NOT_PERFORMED_BY_THIS_SCRIPT"
Write-Output "PUBLIC_PLAYABLE_CLAIM_ALLOWED=false"
Write-Output "Done."
