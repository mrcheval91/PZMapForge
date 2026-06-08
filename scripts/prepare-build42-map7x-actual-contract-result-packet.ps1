#Requires -Version 5.1
<#
.SYNOPSIS
    MAP-7X: Records the actual MAP-7W inspector result and prepares the
    non-cell sidecar discriminator analysis.

    Does NOT run Project Zomboid.
    Does NOT upload to Steam Workshop.
    Does NOT copy reference files into the candidate.
    Does NOT write outside .local/.

.PARAMETER Output
    Required. Path under .local/.

.PARAMETER ActualContractJson
    Optional. Path to the MAP-7W actual contract JSON if available.

.PARAMETER ActualContractMd
    Optional. Path to the MAP-7W actual contract MD if available.
#>

param(
    [Parameter(Mandatory=$true)][string]$Output,
    [string]$ActualContractJson = '.\.local\map7w-actual-contract\map-registration-contract.json',
    [string]$ActualContractMd  = '.\.local\map7w-actual-contract\map-registration-contract.md'
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

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

Write-Output "MAP-7X: Actual Registration Contract Result Packet"
Write-Output "Output: $Output"
Write-Output ""

# ---------------------------------------------------------------------------
# Hardcoded MAP-7W actual results (from the real inspector run)
# ---------------------------------------------------------------------------

$map7wCommit                       = 'd57069a'
$exactFileSetMatch                 = $false
$referenceFilesMissingCount        = 12398
$candidateFilesMissingCount        = 1
$referenceHasMapBin                = $false
$candidateHasMapBin                = $false
$mapBinDiscriminator               = $false
$mapInfoValueDifferencesCount      = 2
$modInfoValueDifferencesCount      = 4
$runtimeMountDiscriminatorFound    = $true
$missingLotheaderCount             = 4130
$missingLotpackCount               = 4130
$missingChunkdataCount             = 4130
$nonCellSidecars                   = [string[]]@(
    'streets.xml.bin',
    'worldmap-forest.xml.bak',
    'worldmap-forest.xml.bin',
    'worldmap-forest.xml.bin.bak',
    'worldmap.png',
    'worldmap.xml.bak',
    'worldmap.xml.bin',
    'worldmap.xml.bin.bak'
)

$actualContractLoaded = $false
if ($ActualContractJson -ne '' -and (Test-Path -LiteralPath $ActualContractJson)) {
    Write-Output "Actual contract JSON found: $ActualContractJson"
    $actualContractLoaded = $true
} else {
    Write-Output "Actual contract JSON not found; using hardcoded MAP-7W results."
}

# ---------------------------------------------------------------------------
# MAP_7X_ACTUAL_CONTRACT_RESULT.md
# ---------------------------------------------------------------------------

$resultPath = Join-Path $Output 'MAP_7X_ACTUAL_CONTRACT_RESULT.md'
Set-Content -Path $resultPath -Value @"
# MAP-7X: Actual Registration Contract Result

``````text
MAP7X_ACTUAL_CONTRACT_RESULT_RECORDED
MAP_BIN_DISCRIMINATOR_FALSE
NON_CELL_SIDECAR_GAP_IDENTIFIED
RUNTIME_MOUNT_DISCRIMINATOR_FOUND
BINARY_WRITER_GATE_STILL_CLOSED
PUBLIC_PLAYABLE_CLAIM_ALLOWED=false
``````

## MAP-7W actual run results

``````text
exact_file_set_match:                    false
reference_files_missing_in_candidate:   $referenceFilesMissingCount
candidate_files_missing_in_reference:   $candidateFilesMissingCount
reference_has_map_bin:                   false
candidate_has_map_bin:                   false
map_bin_discriminator:                   false
map_info_value_differences_count:        $mapInfoValueDifferencesCount
mod_info_value_differences_count:        $modInfoValueDifferencesCount
runtime_mount_discriminator_found:       true
``````

## map.bin ruled out

Neither the Dru_map reference nor the PZMapForge candidate has map.bin.
Hypothesis H1 (map.bin missing) is RULED OUT.

## Why $referenceFilesMissingCount missing files is expected under K006

The large missing count is dominated by Dru_map cell files absent from K006 no-binary:
  missing lotheader: $missingLotheaderCount
  missing lotpack:   $missingLotpackCount
  missing chunkdata: $missingChunkdataCount
  subtotal cells:    12390

These are NOT actionable. Do not copy Dru_map binary cells into PZMapForge.
Copying third-party mod cell files is explicitly forbidden.

## Non-cell sidecar gap

8 non-cell reference files not present in the candidate:
  streets.xml.bin
  worldmap-forest.xml.bak
  worldmap-forest.xml.bin
  worldmap-forest.xml.bin.bak
  worldmap.png
  worldmap.xml.bak
  worldmap.xml.bin
  worldmap.xml.bin.bak

These are compiled worldmap and streets sidecars. PZMapForge has the uncompiled
worldmap.xml and worldmap-forest.xml stubs but not the compiled .bin versions.
Whether these are required for IsoMetaGrid mounting is unproven.

## Non-claims

``````text
map.bin: RULED OUT (neither side has it)
Do not copy Dru_map files into PZMapForge.
No playable export claimed.
PUBLIC_PLAYABLE_CLAIM_ALLOWED=false
``````
"@ -Encoding ASCII
Write-Output "Wrote: MAP_7X_ACTUAL_CONTRACT_RESULT.md"

# ---------------------------------------------------------------------------
# MAP_7X_NON_CELL_SIDECAR_DISCRIMINATORS.md
# ---------------------------------------------------------------------------

$sidecarPath = Join-Path $Output 'MAP_7X_NON_CELL_SIDECAR_DISCRIMINATORS.md'
Set-Content -Path $sidecarPath -Value @"
# MAP-7X: Non-Cell Sidecar Discriminators

``````text
NON_CELL_SIDECAR_GAP_IDENTIFIED
MAP_BIN_DISCRIMINATOR_FALSE
BINARY_WRITER_GATE_STILL_CLOSED
PUBLIC_PLAYABLE_CLAIM_ALLOWED=false
``````

## Files in reference not in candidate (non-cell)

| File | Type | Notes |
|---|---|---|
| streets.xml.bin | compiled streets index | Generated by WorldEd/PZ authoring pipeline |
| worldmap.xml.bin | compiled worldmap | Binary form of worldmap.xml |
| worldmap.xml.bak | worldmap backup | May be unused at runtime |
| worldmap.xml.bin.bak | compiled worldmap backup | May be unused at runtime |
| worldmap-forest.xml.bin | compiled forest worldmap | Binary form of worldmap-forest.xml |
| worldmap-forest.xml.bak | forest worldmap backup | May be unused at runtime |
| worldmap-forest.xml.bin.bak | compiled forest backup | May be unused at runtime |
| worldmap.png | worldmap image | In-game map overlay |

## Hypotheses for these files

### H_SIDECAR_1: streets.xml.bin is required for map-folder mounting

streets.xml.bin is a compiled streets/roads index.
PZ may load this file as part of map-folder discovery.
If absent, IsoMetaGrid may skip the map folder.
Status: UNCONFIRMED. Test by generating a minimal stub.

### H_SIDECAR_2: worldmap.xml.bin is required for map-folder mounting

PZ may require the compiled .bin form of worldmap.xml.
If absent, map folder may not be mounted even if map.xml and cells exist.
Status: UNCONFIRMED. Test by generating a minimal stub.

### H_SIDECAR_3: .bak files are not required

Backup files (.bak) are likely artifacts of the authoring pipeline.
They are unlikely to be required for runtime mounting.
Status: Likely not required. Low priority to generate.

### H_SIDECAR_4: worldmap.png is for in-game map rendering only

worldmap.png provides the UI map overlay for the player's in-game map.
It is unlikely to be required for IsoMetaGrid map-folder registration.
Status: Likely not a registration discriminator.

## Investigation approach

Priority order:
1. Determine if streets.xml.bin or worldmap.xml.bin are read by IsoMetaGrid
   during the map-folder scan (review PZ source/decompiled behavior if available).
2. Generate minimal stub stubs for streets.xml.bin and worldmap.xml.bin
   under .local/ only, upload to Workshop, and test.
3. Capture server-side IsoMetaGrid log to see the actual scan attempt.

## Prohibition

Do NOT copy streets.xml.bin or any other Dru_map sidecar file into PZMapForge.
If stub generation is needed, generate minimal stubs from scratch under .local/.
Do NOT redistribute third-party map data.

## Non-claims

``````text
streets.xml.bin is NOT proven to be the cause.
worldmap.xml.bin is NOT proven to be the cause.
No binary writer changes.
PUBLIC_PLAYABLE_CLAIM_ALLOWED=false
``````
"@ -Encoding ASCII
Write-Output "Wrote: MAP_7X_NON_CELL_SIDECAR_DISCRIMINATORS.md"

# ---------------------------------------------------------------------------
# MAP_7X_NEXT_DECISION_TREE.md
# ---------------------------------------------------------------------------

$nextTreePath = Join-Path $Output 'MAP_7X_NEXT_DECISION_TREE.md'
Set-Content -Path $nextTreePath -Value @"
# MAP-7X: Next Decision Tree

## Current state

``````text
MAP7X_ACTUAL_CONTRACT_RESULT_RECORDED
MAP_BIN_DISCRIMINATOR_FALSE
NON_CELL_SIDECAR_GAP_IDENTIFIED
RUNTIME_MOUNT_DISCRIMINATOR_FOUND
BINARY_WRITER_GATE_STILL_CLOSED
PUBLIC_PLAYABLE_CLAIM_ALLOWED=false
``````

## Decision tree

### Branch 1: Sidecar stub test (generate minimal streets.xml.bin / worldmap.xml.bin)

Action:
  Generate minimal stub files for streets.xml.bin and worldmap.xml.bin
  under .local/ only. Do NOT copy Dru_map sidecar content.
  Update Workshop item. Test.

Success condition:
  IsoMetaGrid mounts the candidate map folder.
  expected_map_lotheader_meta_evidence_found=true.
  OR: explicit lotheader parse error (binary writer gate opens).

Failure condition:
  Fallback forest persists. Still no map mount evidence.

### Branch 2: Server-side IsoMetaGrid log capture

Action:
  Capture the server-side IsoMetaGrid log (not client log) from
  K004/K006 tests. Inspect whether the server attempted to scan
  the candidate map folder.

If server log shows no IsoMetaGrid attempt:
  Map folder is not being discovered at all. Registration gap persists.
  Investigate server Map= line, spawnregions.lua, Workshop discovery path.

If server log shows IsoMetaGrid attempt but failure:
  The attempt is happening. Identify the exact failure point.
  If lotheader parse error: binary writer gate opens.
  If sidecar file read error: sidecar stub is the next fix.

### Branch 3: mod.info icon difference investigation

Action:
  Candidate uses poster.png, reference uses icon.png.
  Test if PZ uses the icon= field for map folder registration (unlikely but unproven).
  Change candidate icon= to icon.png and update Workshop item.

### Branch 4: Binary writer gate condition

``````text
BINARY_WRITER_GATE_STILL_CLOSED

Opens when:
  expected_map_lotheader_meta_evidence_found=true
  OR: explicit EOFException on candidate lotheader in log

Do not mutate LOTH/LOTP/chunkdata until gate opens.
``````

## Non-claims

``````text
PUBLIC_PLAYABLE_CLAIM_ALLOWED=false
LOAD_TEST_NOT_PERFORMED_BY_SCRIPT
NO_AUTOMATIC_WORKSHOP_UPLOAD
NO_BINARY_WRITER_CHANGES
NO_THIRD_PARTY_FILES_COPIED
No PZ run performed by this script.
``````
"@ -Encoding ASCII
Write-Output "Wrote: MAP_7X_NEXT_DECISION_TREE.md"

# ---------------------------------------------------------------------------
# Preflight JSON
# ---------------------------------------------------------------------------

$preflight = [ordered]@{
    schema                               = 'pzmapforge.map7x-preflight.v0.1'
    source_map7w_commit                  = $map7wCommit
    actual_contract_loaded               = $actualContractLoaded
    map_bin_discriminator                = $mapBinDiscriminator
    reference_has_map_bin                = $referenceHasMapBin
    candidate_has_map_bin                = $candidateHasMapBin
    reference_files_missing_in_candidate_count = $referenceFilesMissingCount
    missing_lotheader_count              = $missingLotheaderCount
    missing_lotpack_count                = $missingLotpackCount
    missing_chunkdata_count              = $missingChunkdataCount
    missing_non_cell_sidecar_count       = $nonCellSidecars.Count
    missing_non_cell_sidecars            = $nonCellSidecars
    runtime_mount_discriminator_found    = $runtimeMountDiscriminatorFound
    binary_writer_gate_closed            = $true
    binary_format_investigation_paused   = $true
    next_branch                          = 'non_cell_sidecar_and_runtime_registration_probe'
    public_playable_claim_allowed        = $false
    load_test_performed_by_script        = $false
    automatic_workshop_upload_performed  = $false
    binary_writer_changed                = $false
    third_party_reference_files_copied   = $false
}

$preflightJsonPath = Join-Path $Output 'map7x-preflight.json'
$preflight | ConvertTo-Json -Depth 4 | Set-Content -Path $preflightJsonPath -Encoding ASCII
Write-Output "Wrote: map7x-preflight.json"

$preflightMdPath = Join-Path $Output 'map7x-preflight.md'
Set-Content -Path $preflightMdPath -Value @"
# MAP-7X Preflight

``````text
MAP7X_ACTUAL_CONTRACT_RESULT_RECORDED
MAP_BIN_DISCRIMINATOR_FALSE
NON_CELL_SIDECAR_GAP_IDENTIFIED
BINARY_WRITER_GATE_STILL_CLOSED
PUBLIC_PLAYABLE_CLAIM_ALLOWED=false
``````

``````text
map_bin_discriminator:                   false
reference_has_map_bin:                   false
candidate_has_map_bin:                   false
reference_files_missing_in_candidate:   $referenceFilesMissingCount
missing_lotheader_count:                 $missingLotheaderCount
missing_lotpack_count:                   $missingLotpackCount
missing_chunkdata_count:                 $missingChunkdataCount
missing_non_cell_sidecar_count:          $($nonCellSidecars.Count)
runtime_mount_discriminator_found:       true
binary_writer_gate_closed:               true
next_branch:                             non_cell_sidecar_and_runtime_registration_probe
third_party_reference_files_copied:      false
public_playable_claim_allowed:           false
``````
"@ -Encoding ASCII
Write-Output "Wrote: map7x-preflight.md"

Write-Output ""
Write-Output "MAP-7X packet complete."
Write-Output "map_bin_discriminator=false"
Write-Output "missing_non_cell_sidecars=$($nonCellSidecars.Count)"
Write-Output "next_branch=non_cell_sidecar_and_runtime_registration_probe"
Write-Output "binary_writer_gate_closed=true"
Write-Output "third_party_reference_files_copied=false"
Write-Output "PUBLIC_PLAYABLE_CLAIM_ALLOWED=false"
Write-Output "Done."
