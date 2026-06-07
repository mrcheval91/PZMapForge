#Requires -Version 5.1
<#
.SYNOPSIS
    MAP-7K: Generates an experiment-G candidate (dual layout + map= field in
    mod.info) and produces a diagnostic packet.

    All output under .local/ only.
    Does NOT copy files to PZ folders.
    Does NOT write outside .local/.
    Does NOT run PZ.
    Does NOT change LOTH/LOTP/chunkdata binary behavior.

.PARAMETER Output
    Required. Path under .local/ for packet output.

.PARAMETER MapId
    Map ID. Default: pzmapforge_build42_candidate_v4_001

.PARAMETER ModFolderName
    Mod folder name for reference. Default: pzmapforge_build42_candidate_v4_001

.PARAMETER ServerName
    Suggested server preset name. Default: PZMF_B42_METADATA_V4_VARIANT_G_001

.EXAMPLE
    powershell -ExecutionPolicy Bypass -File .\scripts\prepare-build42-map7k-modinfo-map-field-experiment-packet.ps1 `
        -Output .\.local\map7k-packet
#>

param(
    [Parameter(Mandatory=$true)][string]$Output,
    [string]$MapId         = 'pzmapforge_build42_candidate_v4_001',
    [string]$ModFolderName = 'pzmapforge_build42_candidate_v4_001',
    [string]$ServerName    = 'PZMF_B42_METADATA_V4_VARIANT_G_001'
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
# Step 2: Build experiment-G candidate (dual layout + map= field)
# ---------------------------------------------------------------------------

Write-Output "Building experiment-G candidate (dual layout + map= field)..."

$expGBase     = Join-Path $Output ('experiment-g-candidate\' + $MapId)
$expG42Dir    = Join-Path $expGBase '42'
$expG42Maps   = Join-Path $expG42Dir "media\maps\$MapId"
$expGRootMaps = Join-Path $expGBase "media\maps\$MapId"

New-Item -ItemType Directory -Force -Path $expG42Maps   | Out-Null
New-Item -ItemType Directory -Force -Path $expGRootMaps | Out-Null

# Copy versioned 42/ files (byte-exact, preserves no-BOM)
Copy-Item -LiteralPath (Join-Path $src42Dir 'mod.info') `
          -Destination (Join-Path $expG42Dir 'mod.info') -Force

$mapDataFiles = @('map.info', 'spawnpoints.lua', 'objects.lua',
                  '0_0.lotheader', 'world_0_0.lotpack', 'chunkdata_0_0.bin')
foreach ($f in $mapDataFiles) {
    $src = Join-Path $srcMapData $f
    if (Test-Path -LiteralPath $src) {
        Copy-Item -LiteralPath $src -Destination (Join-Path $expG42Maps $f) -Force
    }
}

# Create root copies (byte-exact)
Copy-Item -LiteralPath (Join-Path $expG42Dir 'mod.info') `
          -Destination (Join-Path $expGBase 'mod.info') -Force

foreach ($f in $mapDataFiles) {
    $src = Join-Path $expG42Maps $f
    if (Test-Path -LiteralPath $src) {
        Copy-Item -LiteralPath $src -Destination (Join-Path $expGRootMaps $f) -Force
    }
}

# Add map= field to both mod.info files (no-BOM UTF-8)
function Add-MapField {
    param([string]$ModInfoPath, [string]$MapValue)
    if (-not (Test-Path -LiteralPath $ModInfoPath)) { return }
    $content = [System.IO.File]::ReadAllText($ModInfoPath, [System.Text.UTF8Encoding]::new($false))
    if ($content -notmatch '(?m)^map\s*=') {
        $content = $content.TrimEnd() + "`nmap=$MapValue`n"
        [System.IO.File]::WriteAllText($ModInfoPath, $content, [System.Text.UTF8Encoding]::new($false))
        Write-Output "  Added map=$MapValue to $ModInfoPath"
    } else {
        Write-Output "  map= field already present in $ModInfoPath"
    }
}

Add-MapField -ModInfoPath (Join-Path $expG42Dir 'mod.info')  -MapValue $MapId
Add-MapField -ModInfoPath (Join-Path $expGBase 'mod.info')   -MapValue $MapId

Write-Output "Experiment-G candidate at: $expGBase"

# ---------------------------------------------------------------------------
# Step 3: Verify no-BOM on modified mod.info files
# ---------------------------------------------------------------------------

function Test-HasBom {
    param([string]$Path)
    if (-not (Test-Path -LiteralPath $Path)) { return $false }
    $b = [System.IO.File]::ReadAllBytes($Path)
    return ($b.Length -ge 3 -and $b[0] -eq 0xEF -and $b[1] -eq 0xBB -and $b[2] -eq 0xBF)
}

$v42ModNoBom   = -not (Test-HasBom (Join-Path $expG42Dir 'mod.info'))
$rootModNoBom  = -not (Test-HasBom (Join-Path $expGBase 'mod.info'))
Write-Output "  42/mod.info no-BOM: $v42ModNoBom"
Write-Output "  root/mod.info no-BOM: $rootModNoBom"

# ---------------------------------------------------------------------------
# Step 4: Run discovery path inspector
# ---------------------------------------------------------------------------

$discoveryScript = Join-Path $repoRoot 'scripts\inspect-build42-map-discovery-path.ps1'
Write-Output "Running discovery path inspector..."

& powershell -ExecutionPolicy Bypass -File $discoveryScript `
    -CandidateRoot $expGBase `
    -Output $Output `
    -MapId $MapId

if ($LASTEXITCODE -ne 0) {
    Write-Error "Discovery path inspection failed (exit $LASTEXITCODE)"
    exit 1
}

# ---------------------------------------------------------------------------
# Step 5: Run metadata contract inspector
# ---------------------------------------------------------------------------

$metaScript = Join-Path $repoRoot 'scripts\inspect-build42-map-metadata-contract.ps1'
Write-Output "Running metadata contract inspector..."

& powershell -ExecutionPolicy Bypass -File $metaScript `
    -CandidateRoot $expGBase `
    -Output $Output `
    -MapId $MapId `
    -ModId $MapId

if ($LASTEXITCODE -ne 0) {
    Write-Error "Metadata contract inspection failed (exit $LASTEXITCODE)"
    exit 1
}

# Load inspector results
$discoveryJson = Join-Path $Output 'map-discovery-path-report.json'
$metaJson      = Join-Path $Output 'map-metadata-contract-report.json'
$disc = if (Test-Path $discoveryJson) { Get-Content $discoveryJson -Raw | ConvertFrom-Json } else { $null }
$meta = if (Test-Path $metaJson)      { Get-Content $metaJson      -Raw | ConvertFrom-Json } else { $null }

$riskLabel       = if ($null -ne $disc) { [string]$disc.map_folder_discovery_risk        } else { 'UNKNOWN' }
$hasMapField     = if ($null -ne $meta) { [bool]$meta.mod_info_has_map_field              } else { $false }
$mapFieldValue   = if ($null -ne $meta) { [string]$meta.v42_mod_info_map_value            } else { '' }
$mapFieldMatches = if ($null -ne $meta) { [bool]$meta.mod_info_map_value_matches_expected } else { $false }

# LOTH/LOTP/chunkdata sizes (binary files unchanged from v4 writer)
$lothSize      = if ($null -ne $disc) { [long]$disc.versioned_42_files.lotheader.size } else { 0 }
$lotpSize      = if ($null -ne $disc) { [long]$disc.versioned_42_files.lotpack.size   } else { 0 }
$chunkdataSize = if ($null -ne $disc) { [long]$disc.versioned_42_files.chunkdata.size } else { 0 }

# ---------------------------------------------------------------------------
# Packet files
# ---------------------------------------------------------------------------

# MAP_7K_MODINFO_MAP_FIELD_PACKET.md
$packetMd = Join-Path $Output 'MAP_7K_MODINFO_MAP_FIELD_PACKET.md'
Set-Content -Path $packetMd -Encoding ASCII -Value @"
# MAP-7K: mod.info map= Field Experiment Packet

${fence}text
MAP7F_VARIANT_F_MAP_FOLDER_SCAN_EMPTY
H5_FOLDER_ID_ALIGNMENT_RULED_OUT
H8_MOD_INFO_MAP_FIELD_RECOMMENDED
VARIANTS_ABCDEF_EXHAUSTED
mod_info_has_map_field=$($hasMapField.ToString().ToLower())
v42_mod_info_map_value=$mapFieldValue
mod_info_map_value_matches_expected=$($mapFieldMatches.ToString().ToLower())
map_folder_discovery_risk=$riskLabel
LOAD_TEST_NOT_PERFORMED_BY_THIS_SCRIPT
PUBLIC_PLAYABLE_CLAIM_ALLOWED=false
${fence}

## Variants A through F exhausted

VARIANTS_ABCDEF_EXHAUSTED: all six layout and alignment experiments
produced empty IsoMetaGrid map folder scan. H5 (folder/id alignment) ruled out.

| Variant | Change | Result |
|---|---|---|
| A | Map= candidate;Muldraugh | SCAN_EMPTY |
| B | Map= candidate only | SCAN_EMPTY |
| C | Map= Muldraugh;candidate | SCAN_EMPTY |
| D | + root media/maps/ | SCAN_EMPTY |
| E | + root mod.info | SCAN_EMPTY |
| F | folder name == mod.info id | SCAN_EMPTY |

## Experiment G: mod.info map= field

The experiment-G candidate adds map=$MapId to both mod.info files.

${fence}text
experiment-g-candidate/$MapId/
  mod.info                  (map=$MapId added)
  42/mod.info               (map=$MapId added)
  42/media/maps/$MapId/     (binary files unchanged)
  media/maps/$MapId/
${fence}

mod_info_has_map_field: $hasMapField
v42_mod_info_map_value: $mapFieldValue
LOTH size: $lothSize bytes (unchanged from empty_grass_v4)
LOTP size: $lotpSize bytes (unchanged)
Chunkdata size: $chunkdataSize bytes (unchanged)

## Success condition

If Experiment G succeeds:
${fence}text
Looking in these map folders:
  $MapId
<End of map-folders list>
${fence}

classification target: MAP7K_VARIANT_G_MAP_FOLDER_SCAN_FOUND (hypothetical)

## Failure condition

If Experiment G fails:
classification: MAP7F_VARIANT_G_MAP_FOLDER_SCAN_EMPTY

## Next human decision

Install experiment-G candidate per MAP_7K_EXPERIMENT_G_MANUAL_INSTALL_COMMANDS.md.
All PZ folder writes are HUMAN-ONLY.

LOAD_TEST_NOT_PERFORMED_BY_THIS_SCRIPT
PUBLIC_PLAYABLE_CLAIM_ALLOWED=false: binding.
"@

# MAP_7K_VARIANT_F_RESULT_SUMMARY.md
$variantFMd = Join-Path $Output 'MAP_7K_VARIANT_F_RESULT_SUMMARY.md'
Set-Content -Path $variantFMd -Encoding ASCII -Value @"
# MAP-7K: Variant F Result Summary

MAP7F_VARIANT_F_MAP_FOLDER_SCAN_EMPTY
H5_FOLDER_ID_ALIGNMENT_RULED_OUT

Variant F:
  Hypothesis: H5 -- folder name must match mod.info id
  Installed folder: $MapId
  mod.info id: $MapId
  Result: IsoMetaGrid map-folder scan still empty
  candidate_loaded: true
  player_data_received: true
  map_folders_list_empty: true
  spawn_building_warning: true

Conclusion: exact folder/id alignment alone does not fix the registration.
Next: Experiment G tests H8 -- mod.info map= field.

PUBLIC_PLAYABLE_CLAIM_ALLOWED=false
"@

# MAP_7K_EXPERIMENT_G_MANUAL_INSTALL_COMMANDS.md
$installMd = Join-Path $Output 'MAP_7K_EXPERIMENT_G_MANUAL_INSTALL_COMMANDS.md'
Set-Content -Path $installMd -Encoding ASCII -Value @"
# MAP-7K: Experiment G Manual Install Commands (HUMAN-ONLY)

These commands show how to copy the experiment-G candidate to the PZ mods
folder. The packet script does NOT do this. All steps are HUMAN-ONLY.

## Candidate source location

${fence}text
$expGBase
${fence}

Structure:
${fence}text
$MapId/
  mod.info             (map=$MapId added -- no-BOM)
  42/mod.info          (map=$MapId added -- no-BOM)
  42/media/maps/$MapId/ (binary files unchanged)
  media/maps/$MapId/
${fence}

## Step 1: Delete old install (HUMAN-ONLY)

Remove any existing $ModFolderName folder from your PZ mods directory.

## Step 2: Copy experiment-G candidate (HUMAN-ONLY)

${fence}powershell
Copy-Item -Recurse -Force `
  "$expGBase" `
  "C:\Users\YourUser\Zomboid\mods\$ModFolderName"
${fence}

Replace YourUser with your Windows username.
Verify mod.info contains map=$MapId after copying.

## Step 3: Server configuration (HUMAN-ONLY -- no-BOM UTF-8)

${fence}ini
Map=$MapId;Muldraugh, KY
Mods=$MapId
${fence}

Server name: $ServerName

## Step 4: Launch and capture log

Run PZ server and client. After loading, copy DebugLog to .local:
${fence}text
C:\Users\YourUser\Zomboid\Logs\*_DebugLog.txt -> .local\map7k-logs\DebugLog-variant-G.txt
${fence}

## Step 5: Analyze log

${fence}powershell
powershell -ExecutionPolicy Bypass ``
  -File .\scripts\inspect-build42-map7d-load-result.ps1 ``
  -LogPath .\.local\map7k-logs\DebugLog-variant-G.txt ``
  -Output .\.local\map7k-analysis\variant-G ``
  -ExpectedMapId $MapId ``
  -VariantLabel VariantG
${fence}

## Success target

${fence}text
Looking in these map folders:
  $MapId
<End of map-folders list>
${fence}

classification target: map_folders_list_empty=false

PUBLIC_PLAYABLE_CLAIM_ALLOWED=false
"@

# MAP_7K_EXPERIMENT_G_LOG_CAPTURE_COMMANDS.md
$logMd = Join-Path $Output 'MAP_7K_EXPERIMENT_G_LOG_CAPTURE_COMMANDS.md'
Set-Content -Path $logMd -Encoding ASCII -Value @"
# MAP-7K: Experiment G Log Capture Commands

${fence}powershell
New-Item -ItemType Directory -Force -Path .\.local\map7k-logs | Out-Null
Copy-Item "C:\Users\YourUser\Zomboid\Logs\*_DebugLog.txt" ``
  .\.local\map7k-logs\DebugLog-variant-G.txt
${fence}

## Run analyzer

${fence}powershell
powershell -ExecutionPolicy Bypass ``
  -File .\scripts\inspect-build42-map7d-load-result.ps1 ``
  -LogPath .\.local\map7k-logs\DebugLog-variant-G.txt ``
  -Output .\.local\map7k-analysis\variant-G ``
  -ExpectedMapId $MapId ``
  -VariantLabel VariantG
${fence}

## Map folder scan section to capture

${fence}text
IsoMetaGrid.Create: begin scanning directories
Looking in these map folders:
  (expect: $MapId appears here for success)
<End of map-folders list>
${fence}

PUBLIC_PLAYABLE_CLAIM_ALLOWED=false
LOAD_TEST_NOT_PERFORMED_BY_THIS_SCRIPT
"@

# MAP_7K_EXPERIMENT_G_MANUAL_RESULT.local-template.md
$resultMd = Join-Path $Output 'MAP_7K_EXPERIMENT_G_MANUAL_RESULT.local-template.md'
Set-Content -Path $resultMd -Encoding ASCII -Value @"
# MAP-7K: Experiment G Manual Result (local template)

Fill in after Experiment G manual retest. Do not commit.

## Test metadata

Date:
PZ version:
Server: $ServerName
Map= line: Map=$MapId;Muldraugh, KY
Change applied: map=$MapId added to mod.info

## Pre-test checks (HUMAN-ONLY)

- [ ] Both mod.info files contain map=$MapId (verify by opening the file)
- [ ] All text files no-BOM UTF-8
- [ ] Server preset INI updated (no-BOM)
- [ ] Old log files deleted
- [ ] Only candidate mod enabled

## Map folder scan section

${fence}text
(paste IsoMetaGrid scan section here)
${fence}

- Did $MapId appear in map folder scan? YES / NO
- map_folders_list_empty:

## Analyzer output

classification:
map_folders_list_empty:
map_folders_list_count:

## Result

(PASS -- map folder registered) / (FAIL -- still empty) / (INCONCLUSIVE)

PUBLIC_PLAYABLE_CLAIM_ALLOWED=false
"@

# ---------------------------------------------------------------------------
# map7k-modinfo-map-field-preflight.json
# ---------------------------------------------------------------------------

$preflightJson = Join-Path $Output 'map7k-modinfo-map-field-preflight.json'
$preflight = [ordered]@{
    schema                              = 'pzmapforge.map7k-modinfo-map-field-preflight.v0.1'
    map_id                              = $MapId
    mod_folder_name                     = $ModFolderName
    server_name                         = $ServerName
    variant_f_result                    = 'MAP7F_VARIANT_F_MAP_FOLDER_SCAN_EMPTY'
    h5_folder_id_alignment_ruled_out    = $true
    variants_abcdef_exhausted           = $true
    h8_mod_info_map_field               = $true
    mod_info_has_map_field              = $hasMapField
    v42_mod_info_map_value              = $mapFieldValue
    mod_info_map_value_matches_expected = $mapFieldMatches
    loth_size_bytes                     = $lothSize
    lotp_size_bytes                     = $lotpSize
    chunkdata_size_bytes                = $chunkdataSize
    map_folder_discovery_risk           = $riskLabel
    suggested_map_line                  = "Map=$MapId;Muldraugh, KY"
    suggested_analyzer_expected_map_id  = $MapId
    suggested_analyzer_variant_label    = 'VariantG'
    all_pz_folder_writes_human_only     = $true
    load_test_not_performed             = $true
    public_playable_claim_allowed       = $false
    binary_writer_not_changed           = $true
}
$preflight | ConvertTo-Json -Depth 4 | Set-Content -Path $preflightJson -Encoding UTF8
Write-Output "JSON: $preflightJson"

# map7k-modinfo-map-field-preflight.md
$preflightMd = Join-Path $Output 'map7k-modinfo-map-field-preflight.md'
Set-Content -Path $preflightMd -Encoding ASCII -Value @"
# MAP-7K mod.info map= Field Preflight

${fence}text
map_id=$MapId
variant_f_result=MAP7F_VARIANT_F_MAP_FOLDER_SCAN_EMPTY
h5_folder_id_alignment_ruled_out=true
variants_abcdef_exhausted=true
h8_mod_info_map_field=true
mod_info_has_map_field=$($hasMapField.ToString().ToLower())
v42_mod_info_map_value=$mapFieldValue
mod_info_map_value_matches_expected=$($mapFieldMatches.ToString().ToLower())
loth_size_bytes=$lothSize
lotp_size_bytes=$lotpSize
chunkdata_size_bytes=$chunkdataSize
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
Write-Output "MAP_7K_MODINFO_MAP_FIELD_PACKET.md:             $(Test-Path $packetMd)"
Write-Output "MAP_7K_VARIANT_F_RESULT_SUMMARY.md:             $(Test-Path $variantFMd)"
Write-Output "MAP_7K_EXPERIMENT_G_MANUAL_INSTALL_COMMANDS.md: $(Test-Path $installMd)"
Write-Output "MAP_7K_EXPERIMENT_G_LOG_CAPTURE_COMMANDS.md:    $(Test-Path $logMd)"
Write-Output "MAP_7K_EXPERIMENT_G_MANUAL_RESULT.local-template: $(Test-Path $resultMd)"
Write-Output "map7k-modinfo-map-field-preflight.json:         $(Test-Path $preflightJson)"
Write-Output "map7k-modinfo-map-field-preflight.md:           $(Test-Path $preflightMd)"
Write-Output "map-discovery-path-report.json:                 $(Test-Path $discoveryJson)"
Write-Output "map-discovery-path-report.md:                   $(Test-Path (Join-Path $Output 'map-discovery-path-report.md'))"
Write-Output "map-metadata-contract-report.json:              $(Test-Path $metaJson)"
Write-Output "map-metadata-contract-report.md:                $(Test-Path (Join-Path $Output 'map-metadata-contract-report.md'))"
Write-Output ""
Write-Output "MAP7F_VARIANT_F_MAP_FOLDER_SCAN_EMPTY"
Write-Output "H5_FOLDER_ID_ALIGNMENT_RULED_OUT"
Write-Output "H8_MOD_INFO_MAP_FIELD_RECOMMENDED"
Write-Output "VARIANTS_ABCDEF_EXHAUSTED"
Write-Output "LOAD_TEST_NOT_PERFORMED_BY_THIS_SCRIPT"
Write-Output "PUBLIC_PLAYABLE_CLAIM_ALLOWED=false"
Write-Output "Done."
