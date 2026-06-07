#Requires -Version 5.1
<#
.SYNOPSIS
    MAP-7J: Generates an experiment-F dual-layout candidate (same as MAP-7I
    experiment-E), runs metadata contract inspection, and produces a diagnostic
    packet.

    All output under .local/ only.
    Does NOT copy files to PZ folders.
    Does NOT write outside .local/.
    Does NOT run PZ.

.PARAMETER Output
    Required. Path under .local/ for packet output.

.PARAMETER MapId
    Map ID. Default: pzmapforge_build42_candidate_v4_001

.PARAMETER ModFolderName
    Mod folder name for reference. Default: pzmapforge_build42_candidate_v4_001_test

.PARAMETER ServerName
    Suggested server preset name. Default: PZMF_B42_METADATA_V4_VARIANT_F_001

.EXAMPLE
    powershell -ExecutionPolicy Bypass -File .\scripts\prepare-build42-map7j-metadata-contract-packet.ps1 `
        -Output .\.local\map7j-packet
#>

param(
    [Parameter(Mandatory=$true)][string]$Output,
    [string]$MapId         = 'pzmapforge_build42_candidate_v4_001',
    [string]$ModFolderName = 'pzmapforge_build42_candidate_v4_001_test',
    [string]$ServerName    = 'PZMF_B42_METADATA_V4_VARIANT_F_001'
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
# Step 1: Generate empty_grass_v4 candidate
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

# ---------------------------------------------------------------------------
# Step 2: Build experiment-F candidate (dual layout, same as experiment-E)
# ---------------------------------------------------------------------------

Write-Output "Building experiment-F candidate (dual layout)..."

$expFBase     = Join-Path $Output ('experiment-f-candidate\' + $MapId)
$expF42Dir    = Join-Path $expFBase '42'
$expF42Maps   = Join-Path $expF42Dir "media\maps\$MapId"
$expFRootMaps = Join-Path $expFBase "media\maps\$MapId"

New-Item -ItemType Directory -Force -Path $expF42Maps   | Out-Null
New-Item -ItemType Directory -Force -Path $expFRootMaps | Out-Null

# Copy versioned 42/ files
Copy-Item -LiteralPath (Join-Path $src42Dir 'mod.info') `
          -Destination (Join-Path $expF42Dir 'mod.info') -Force

$mapDataFiles = @('map.info', 'spawnpoints.lua', 'objects.lua',
                  '0_0.lotheader', 'world_0_0.lotpack', 'chunkdata_0_0.bin')
foreach ($f in $mapDataFiles) {
    $src = Join-Path $srcMapData $f
    if (Test-Path -LiteralPath $src) {
        Copy-Item -LiteralPath $src -Destination (Join-Path $expF42Maps $f) -Force
    }
}

# Create root copies (byte-exact, preserves no-BOM)
Copy-Item -LiteralPath (Join-Path $expF42Dir 'mod.info') `
          -Destination (Join-Path $expFBase 'mod.info') -Force

foreach ($f in $mapDataFiles) {
    $src = Join-Path $expF42Maps $f
    if (Test-Path -LiteralPath $src) {
        Copy-Item -LiteralPath $src -Destination (Join-Path $expFRootMaps $f) -Force
    }
}

Write-Output "Experiment-F candidate at: $expFBase"

# ---------------------------------------------------------------------------
# Step 3: Run discovery path inspector
# ---------------------------------------------------------------------------

$discoveryScript = Join-Path $repoRoot 'scripts\inspect-build42-map-discovery-path.ps1'
Write-Output "Running discovery path inspector..."

& powershell -ExecutionPolicy Bypass -File $discoveryScript `
    -CandidateRoot $expFBase `
    -Output $Output `
    -MapId $MapId

if ($LASTEXITCODE -ne 0) {
    Write-Error "Discovery path inspection failed (exit $LASTEXITCODE)"
    exit 1
}

# ---------------------------------------------------------------------------
# Step 4: Run metadata contract inspector
# ---------------------------------------------------------------------------

$metaScript = Join-Path $repoRoot 'scripts\inspect-build42-map-metadata-contract.ps1'
Write-Output "Running metadata contract inspector..."

& powershell -ExecutionPolicy Bypass -File $metaScript `
    -CandidateRoot $expFBase `
    -Output $Output `
    -MapId $MapId `
    -ModId $MapId

if ($LASTEXITCODE -ne 0) {
    Write-Error "Metadata contract inspection failed (exit $LASTEXITCODE)"
    exit 1
}

# Load results
$discoveryJson = Join-Path $Output 'map-discovery-path-report.json'
$metaJson      = Join-Path $Output 'map-metadata-contract-report.json'
$disc = if (Test-Path $discoveryJson) { Get-Content $discoveryJson -Raw | ConvertFrom-Json } else { $null }
$meta = if (Test-Path $metaJson)      { Get-Content $metaJson      -Raw | ConvertFrom-Json } else { $null }

$riskLabel    = if ($null -ne $disc) { [string]$disc.map_folder_discovery_risk } else { 'UNKNOWN' }
$hasDual      = if ($null -ne $disc) { [bool]$disc.has_dual_mod_info_layout    } else { $false }
$v42ModId     = if ($null -ne $meta) { [string]$meta.v42_mod_info_id            } else { '' }
$v42MapId     = if ($null -ne $meta) { [string]$meta.v42_map_info_id            } else { '' }
$idMatch      = if ($null -ne $meta) { [bool]$meta.v42_mod_info_id_matches_expected } else { $false }
$mapIdMatch   = if ($null -ne $meta) { [bool]$meta.v42_map_info_id_matches_expected } else { $false }

# ---------------------------------------------------------------------------
# Packet files
# ---------------------------------------------------------------------------

# MAP_7J_METADATA_CONTRACT_PACKET.md
$packetMd = Join-Path $Output 'MAP_7J_METADATA_CONTRACT_PACKET.md'
Set-Content -Path $packetMd -Encoding ASCII -Value @"
# MAP-7J: Metadata Contract Diagnostic Packet

${fence}text
MAP7F_VARIANT_E_MAP_FOLDER_SCAN_EMPTY
VARIANTS_ABCDE_EXHAUSTED
ROOT_MOD_INFO_EXPERIMENT_FAILED
METADATA_CONTRACT_FOCUS
map_folder_discovery_risk=$riskLabel
has_dual_mod_info_layout=$($hasDual.ToString().ToLower())
v42_mod_info_id=$v42ModId
v42_map_info_id=$v42MapId
v42_mod_info_id_matches_expected=$($idMatch.ToString().ToLower())
v42_map_info_id_matches_expected=$($mapIdMatch.ToString().ToLower())
LOAD_TEST_NOT_PERFORMED_BY_THIS_SCRIPT
PUBLIC_PLAYABLE_CLAIM_ALLOWED=false
${fence}

## Variants A through E exhausted

VARIANTS_ABCDE_EXHAUSTED: all five layout and Map= order experiments produced
empty IsoMetaGrid map folder scan. Map= ordering, root media/maps, and root
mod.info are all ruled out as the sole cause.

| Variant | Layout | Map= | Result |
|---|---|---|---|
| A | 42/ only | candidate;Muldraugh | SCAN_EMPTY |
| B | 42/ only | candidate only | SCAN_EMPTY |
| C | 42/ only | Muldraugh;candidate | SCAN_EMPTY |
| D | 42/ + root media/maps | candidate;Muldraugh | SCAN_EMPTY |
| E | 42/ + root media/maps + root mod.info | candidate;Muldraugh | SCAN_EMPTY |

## Experiment-F candidate (local only)

A dual-layout experiment-F candidate has been generated under .local/
(same structure as experiment-E, for metadata inspection).

${fence}text
experiment-f-candidate/$MapId/
  mod.info           (root)
  42/mod.info
  42/media/maps/$MapId/
  media/maps/$MapId/
${fence}

## Metadata contract inspection results

v42_mod_info_id:           $v42ModId
v42_map_info_id:           $v42MapId
id_fields_match_expected:  v42_mod_info=$($idMatch.ToString().ToLower()), v42_map=$($mapIdMatch.ToString().ToLower())

See map-metadata-contract-report.json for full field inspection.

## Next human decision

Before Variant F:
1. Review mod.info and map.info fields in map-metadata-contract-report.md.
2. Compare against a known-working Build 42 custom map mod (operator-provided).
3. Identify any field differences that may explain the registration failure.
4. Only proceed to Variant F after a specific field hypothesis is formulated.

Do NOT run Variant F until there is a specific metadata change to test.
Do NOT read any real third-party mod files automatically.

## Diagnostic distinction

MAP_FOLDER_SCAN_EMPTY = discovery failure (our current blocker, all A-E).
MAP_FOLDER_SCAN_FOUND_BUT_LOTHEADER_FILES_MISSING = later-stage failure
  (map folder found by IsoMetaGrid but no .lotheader files in it).
These are DISTINCT. Fixing binary writer format is premature until the
map folder registration is resolved.

## Non-claims

LOAD_TEST_NOT_PERFORMED_BY_THIS_SCRIPT
PUBLIC_PLAYABLE_CLAIM_ALLOWED=false: binding.
"@

# MAP_7J_VARIANT_E_RESULT_SUMMARY.md
$variantEMd = Join-Path $Output 'MAP_7J_VARIANT_E_RESULT_SUMMARY.md'
Set-Content -Path $variantEMd -Encoding ASCII -Value @"
# MAP-7J: Variant E Result Summary

MAP7F_VARIANT_E_MAP_FOLDER_SCAN_EMPTY
VARIANTS_ABCDE_EXHAUSTED

Variant E:
  Layout: root mod.info + 42/mod.info + root media/maps/$MapId/ + 42/media/maps/$MapId/
  Map= line: Map=$MapId;Muldraugh, KY
  Mods= line: Mods=$MapId
  Result: IsoMetaGrid map-folder scan still empty
  candidate_loaded: true
  player_data_received: true
  map_folders_list_empty: true
  spawn_building_warning: true
  Visual: forest/fallback world, no blocked square, no city choice

Conclusion: root mod.info alone is not sufficient.
Next focus: metadata contract and mod metadata shape.
See MAP_7J_NEXT_HYPOTHESES.md for hypotheses H4-H8.

PUBLIC_PLAYABLE_CLAIM_ALLOWED=false
"@

# MAP_7J_METADATA_CONTRACT_REPORT.md (packet-level summary, distinct from inspector raw output)
$contractReportMd = Join-Path $Output 'MAP_7J_METADATA_CONTRACT_REPORT.md'
Set-Content -Path $contractReportMd -Encoding ASCII -Value @"
# MAP-7J: Metadata Contract Report Summary

This document summarizes the metadata contract inspection findings for
the experiment-F candidate. See map-metadata-contract-report.json for
the full raw output.

## mod.info inspection summary

v42_mod_info_id: $v42ModId
root_mod_info_id: $(if ($null -ne $meta) { [string]$meta.root_mod_info_id } else { '' })
Expected ModId: $MapId
id_matches: v42=$($idMatch.ToString().ToLower())

## map.info inspection summary

v42_map_info_id: $v42MapId
root_map_info_id: $(if ($null -ne $meta) { [string]$meta.root_map_info_id } else { '' })
Expected MapId: $MapId
id_matches: v42=$($mapIdMatch.ToString().ToLower())

## Byte-identity

mod_info_bytes_identical: $(if ($null -ne $meta) { $meta.mod_info_bytes_identical } else { '' })
map_info_bytes_identical: $(if ($null -ne $meta) { $meta.map_info_bytes_identical } else { '' })

## Hypothesis assessment (observations only)

H5 (mod.info id/folder match): observed v42_mod_info_id=$v42ModId vs expected=$MapId
H6 (map.info id match): observed v42_map_info_id=$v42MapId vs expected=$MapId

No validity claims are made without reference mod comparison.
Next step: operator compares these fields against a known-working mod.

PUBLIC_PLAYABLE_CLAIM_ALLOWED=false
LOAD_TEST_NOT_PERFORMED_BY_THIS_SCRIPT
"@

# MAP_7J_NEXT_HYPOTHESES.md
$hypothesesMd = Join-Path $Output 'MAP_7J_NEXT_HYPOTHESES.md'
Set-Content -Path $hypothesesMd -Encoding ASCII -Value @"
# MAP-7J: Next Hypotheses for Map Registration

VARIANTS_ABCDE_EXHAUSTED
METADATA_CONTRACT_FOCUS

## Summary of experiments to date

D/E proved: root media/maps + root mod.info is not enough.
The registration failure is before IsoMetaGrid scans any path.
The metadata shape of the candidate is the next focus.

## Hypotheses

### H4: map.info field contract (MEDIUM RISK)

The candidate map.info has: id, title, description (minimal).
A working Build 42 mod may require additional fields such as:
  lots=<count>    -- number of lots in the map
  fixed=<value>   -- unknown; seen in some examples
  poster=<file>   -- map poster image
  or other Build 42-specific fields

Without a reference mod comparison this is hypothetical.

### H5: mod.info id field must match folder name exactly (MEDIUM RISK)

The mod.info id= field must equal the mod folder name in the PZ mods directory.
If the installed folder name differs from mod.info id=, PZ may load the mod
but not associate its media path.

### H6: map.info must have id= matching map folder name (MEDIUM RISK)

The map.info id= must equal the media/maps/<id> folder name and the Map= entry.
Our candidate uses the same value throughout, but this should be verified.

### H7: Server manifest/config difference (LOW RISK)

A dedicated-server-specific config file may be required for custom map
registration. Not yet tested.

### H8: mod.info map= field for media path registration (MEDIUM RISK)

Some PZ mods include a map= field in mod.info to register the media/maps path.
Our candidate mod.info does not include this. Adding map=$MapId to mod.info
is a candidate for the next experiment (Variant F).

## Proposed Variant F

If H8 or H4 is confirmed by operator comparison with a reference mod:
Add the identified missing fields to mod.info and/or map.info.
Test in a fresh server preset.

Variant F must not be run until a specific field hypothesis is confirmed.
Do not auto-generate Variant F. This is a human decision gate.

All future PZ folder writes are HUMAN-ONLY.
PUBLIC_PLAYABLE_CLAIM_ALLOWED=false
"@

# MAP_7J_EXPERIMENT_F_MANUAL_RESULT.local-template.md
$resultMd = Join-Path $Output 'MAP_7J_EXPERIMENT_F_MANUAL_RESULT.local-template.md'
Set-Content -Path $resultMd -Encoding ASCII -Value @"
# MAP-7J: Experiment F Manual Result (local template)

Fill in after Experiment F manual retest. Do not commit.
Only run Experiment F after a specific field hypothesis is confirmed.

## Hypothesis tested

(Describe the specific mod.info or map.info change being tested)

## Test metadata

Date:
PZ version:
Server: $ServerName
Map= line: Map=$MapId;Muldraugh, KY
Changes applied vs experiment-E:

## Pre-test checks (HUMAN-ONLY)

- [ ] All text files saved with no-BOM UTF-8
- [ ] Server preset INI updated (no-BOM)
- [ ] Old log files deleted
- [ ] Only candidate mod enabled

## Map folder scan section

${fence}text
(paste IsoMetaGrid scan section from DebugLog here)
${fence}

- Did $MapId appear in map folder scan? YES / NO
- map_folders_list_empty:

## Analyzer output

${fence}powershell
powershell -ExecutionPolicy Bypass -File .\scripts\inspect-build42-map7d-load-result.ps1 ``
  -LogPath .\.local\map7j-logs\DebugLog-variant-F.txt ``
  -Output .\.local\map7j-analysis\variant-F ``
  -ExpectedMapId $MapId ``
  -VariantLabel VariantF
${fence}

classification:
map_folders_list_empty:

## Result

(PASS -- map folder registered) / (FAIL -- still empty) / (INCONCLUSIVE)

PUBLIC_PLAYABLE_CLAIM_ALLOWED=false
"@

# ---------------------------------------------------------------------------
# map7j-metadata-contract-preflight.json
# ---------------------------------------------------------------------------

$preflightJson = Join-Path $Output 'map7j-metadata-contract-preflight.json'
$preflight = [ordered]@{
    schema                              = 'pzmapforge.map7j-metadata-contract-preflight.v0.1'
    map_id                              = $MapId
    mod_folder_name                     = $ModFolderName
    server_name                         = $ServerName
    variant_e_result                    = 'MAP7F_VARIANT_E_MAP_FOLDER_SCAN_EMPTY'
    variants_abcde_exhausted            = $true
    root_media_maps_experiment_failed   = $true
    root_mod_info_experiment_failed     = $true
    metadata_contract_focus             = $true
    v42_mod_info_id                     = $v42ModId
    v42_map_info_id                     = $v42MapId
    v42_mod_info_id_matches_expected    = $idMatch
    v42_map_info_id_matches_expected    = $mapIdMatch
    map_folder_discovery_risk           = $riskLabel
    has_dual_mod_info_layout            = $hasDual
    next_experiment_f_requires_human_decision = $true
    load_test_not_performed             = $true
    public_playable_claim_allowed       = $false
    binary_writer_not_changed           = $true
    pz_assets_read                      = $false
}
$preflight | ConvertTo-Json -Depth 4 | Set-Content -Path $preflightJson -Encoding UTF8
Write-Output "JSON: $preflightJson"

# map7j-metadata-contract-preflight.md
$preflightMd = Join-Path $Output 'map7j-metadata-contract-preflight.md'
Set-Content -Path $preflightMd -Encoding ASCII -Value @"
# MAP-7J Metadata Contract Preflight

${fence}text
map_id=$MapId
variant_e_result=MAP7F_VARIANT_E_MAP_FOLDER_SCAN_EMPTY
variants_abcde_exhausted=true
root_media_maps_experiment_failed=true
root_mod_info_experiment_failed=true
metadata_contract_focus=true
v42_mod_info_id=$v42ModId
v42_map_info_id=$v42MapId
v42_mod_info_id_matches_expected=$($idMatch.ToString().ToLower())
v42_map_info_id_matches_expected=$($mapIdMatch.ToString().ToLower())
next_experiment_f_requires_human_decision=true
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
Write-Output "MAP_7J_METADATA_CONTRACT_PACKET.md:              $(Test-Path $packetMd)"
Write-Output "MAP_7J_VARIANT_E_RESULT_SUMMARY.md:              $(Test-Path $variantEMd)"
Write-Output "MAP_7J_METADATA_CONTRACT_REPORT.md:              $(Test-Path $contractReportMd)"
Write-Output "MAP_7J_NEXT_HYPOTHESES.md:                       $(Test-Path $hypothesesMd)"
Write-Output "MAP_7J_EXPERIMENT_F_MANUAL_RESULT.local-template: $(Test-Path $resultMd)"
Write-Output "map7j-metadata-contract-preflight.json:          $(Test-Path $preflightJson)"
Write-Output "map7j-metadata-contract-preflight.md:            $(Test-Path $preflightMd)"
Write-Output "map-discovery-path-report.json:                  $(Test-Path $discoveryJson)"
Write-Output "map-discovery-path-report.md:                    $(Test-Path (Join-Path $Output 'map-discovery-path-report.md'))"
Write-Output "map-metadata-contract-report.json:               $(Test-Path $metaJson)"
Write-Output "map-metadata-contract-report.md:                 $(Test-Path (Join-Path $Output 'map-metadata-contract-report.md'))"
Write-Output ""
Write-Output "MAP7F_VARIANT_E_MAP_FOLDER_SCAN_EMPTY"
Write-Output "VARIANTS_ABCDE_EXHAUSTED"
Write-Output "METADATA_CONTRACT_FOCUS"
Write-Output "LOAD_TEST_NOT_PERFORMED_BY_THIS_SCRIPT"
Write-Output "PUBLIC_PLAYABLE_CLAIM_ALLOWED=false"
Write-Output "Done."
