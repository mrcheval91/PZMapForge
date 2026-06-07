#Requires -Version 5.1
<#
.SYNOPSIS
    MAP-7J: Inspects mod.info and map.info metadata from a Build 42 candidate
    mod to diagnose map metadata contract mismatches.

    Parses key/value fields and raw lines.
    Checks no-BOM status, ASCII status, byte-identity between root/42 copies.
    Records whether mod id and map id match expected values.
    Does NOT make validity claims beyond what is directly observed.

    Does NOT read PZ install assets.
    Does NOT write outside .local/.
    Does NOT run PZ.

.PARAMETER CandidateRoot
    Path to the candidate mod root (the folder that contains 42/mod.info
    and/or mod.info directly).

.PARAMETER Output
    Path under .local/ for report output.

.PARAMETER MapId
    Expected map folder name / map ID. Default: pzmapforge_build42_candidate_v4_001

.PARAMETER ModId
    Expected mod.info id field value. Default: pzmapforge_build42_candidate_v4_001

.EXAMPLE
    powershell -ExecutionPolicy Bypass -File .\scripts\inspect-build42-map-metadata-contract.ps1 `
        -CandidateRoot .\.local\map7j-packet\experiment-f-candidate\pzmapforge_build42_candidate_v4_001 `
        -Output .\.local\map7j-packet `
        -MapId pzmapforge_build42_candidate_v4_001 `
        -ModId pzmapforge_build42_candidate_v4_001
#>

param(
    [Parameter(Mandatory=$true)][string]$CandidateRoot,
    [Parameter(Mandatory=$true)][string]$Output,
    [string]$MapId = 'pzmapforge_build42_candidate_v4_001',
    [string]$ModId = 'pzmapforge_build42_candidate_v4_001'
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

$forbidden = @('C:\Users\Palmacede\Zomboid', 'C:\Users\Palmacede\Zomboid\mods',
               'C:\Users\Palmacede\Zomboid\Server')
foreach ($f in $forbidden) {
    if ($CandidateRoot -match [regex]::Escape($f)) {
        Write-Error "CandidateRoot must not reference PZ user paths: $CandidateRoot"
        exit 1
    }
}

New-Item -ItemType Directory -Force -Path $Output | Out-Null

# ---------------------------------------------------------------------------
# Helpers
# ---------------------------------------------------------------------------

function Test-HasBom {
    param([string]$Path)
    if (-not (Test-Path -LiteralPath $Path)) { return $false }
    $b = [System.IO.File]::ReadAllBytes($Path)
    return ($b.Length -ge 3 -and $b[0] -eq 0xEF -and $b[1] -eq 0xBB -and $b[2] -eq 0xBF)
}

function Test-IsAscii {
    param([string]$Path)
    if (-not (Test-Path -LiteralPath $Path)) { return $false }
    $b = [System.IO.File]::ReadAllBytes($Path)
    foreach ($byte in $b) { if ($byte -gt 0x7E -and $byte -ne 0x0A -and $byte -ne 0x0D) { return $false } }
    return $true
}

function Get-FileSha256 {
    param([string]$Path)
    if (-not (Test-Path -LiteralPath $Path)) { return '' }
    $sha    = [System.Security.Cryptography.SHA256]::Create()
    $stream = [System.IO.File]::OpenRead($Path)
    try {
        $hash = $sha.ComputeHash($stream)
        return (($hash | ForEach-Object { $_.ToString('x2') }) -join '')
    } finally { $stream.Dispose(); $sha.Dispose() }
}

function Read-FileRecord {
    param([string]$Path, [string]$Label)
    $exists = Test-Path -LiteralPath $Path
    $rec = [ordered]@{
        label       = $Label
        path        = $Path
        exists      = $exists
        size        = if ($exists) { (Get-Item -LiteralPath $Path).Length } else { 0 }
        sha256      = Get-FileSha256 $Path
        has_bom     = if ($exists) { Test-HasBom $Path } else { $false }
        no_bom      = if ($exists) { -not (Test-HasBom $Path) } else { $false }
        is_ascii    = if ($exists) { Test-IsAscii $Path } else { $false }
        raw_lines   = [string[]]@()
        fields      = [ordered]@{}
    }
    if ($exists) {
        $lines = Get-Content -LiteralPath $Path
        $rec['raw_lines'] = [string[]]$lines
        $fields = [ordered]@{}
        foreach ($line in $lines) {
            if ($line -match '^(\w[\w\s]*?)\s*=\s*(.*)$') {
                $fields[$Matches[1].Trim()] = $Matches[2].Trim()
            }
        }
        $rec['fields'] = $fields
    }
    return $rec
}

# ---------------------------------------------------------------------------
# File paths
# ---------------------------------------------------------------------------

$v42ModInfoPath      = Join-Path $CandidateRoot '42\mod.info'
$rootModInfoPath     = Join-Path $CandidateRoot 'mod.info'
$v42MapInfoPath      = Join-Path $CandidateRoot "42\media\maps\$MapId\map.info"
$rootMapInfoPath     = Join-Path $CandidateRoot "media\maps\$MapId\map.info"
$commonMapInfoPath   = Join-Path $CandidateRoot "common\media\maps\$MapId\map.info"
$commonModInfoPath   = Join-Path $CandidateRoot 'common\mod.info'

# ---------------------------------------------------------------------------
# Read records
# ---------------------------------------------------------------------------

Write-Output "Inspecting metadata: $CandidateRoot"

$v42ModInfo   = Read-FileRecord $v42ModInfoPath    '42/mod.info'
$rootModInfo  = Read-FileRecord $rootModInfoPath   'root/mod.info'
$commonModInfo = Read-FileRecord $commonModInfoPath 'common/mod.info'
$v42MapInfo   = Read-FileRecord $v42MapInfoPath    '42/media/maps/map.info'
$rootMapInfo  = Read-FileRecord $rootMapInfoPath   'root/media/maps/map.info'
$commonMapInfo = Read-FileRecord $commonMapInfoPath 'common/media/maps/map.info'

# ---------------------------------------------------------------------------
# ID match analysis
# ---------------------------------------------------------------------------

function Get-FieldValue {
    param($Fields, [string]$Key)
    if ($null -eq $Fields -or $Fields -isnot [System.Collections.IDictionary]) { return '' }
    if ($Fields.Contains($Key)) { return [string]$Fields[$Key] }
    foreach ($k in $Fields.Keys) { if ($k -eq $Key) { return [string]$Fields[$k] } }
    return ''
}

$v42ModInfoId   = Get-FieldValue $v42ModInfo.fields   'id'
$rootModInfoId  = Get-FieldValue $rootModInfo.fields  'id'
$v42MapInfoId   = Get-FieldValue $v42MapInfo.fields   'id'
$rootMapInfoId  = Get-FieldValue $rootMapInfo.fields  'id'

$v42ModInfoIdMatchesExpected   = ($v42ModInfoId   -eq $ModId) -and ($ModId -ne '')
$rootModInfoIdMatchesExpected  = ($rootModInfoId  -eq $ModId) -and ($ModId -ne '')
$v42MapInfoIdMatchesExpected   = ($v42MapInfoId   -eq $MapId) -and ($MapId -ne '')
$rootMapInfoIdMatchesExpected  = ($rootMapInfoId  -eq $MapId) -and ($MapId -ne '')

# Check whether MapId appears anywhere in raw map.info text
$v42MapInfoContainsMapId   = ($v42MapInfo.exists  -and (($v42MapInfo.raw_lines  -join "`n") -match [regex]::Escape($MapId)))
$rootMapInfoContainsMapId  = ($rootMapInfo.exists -and (($rootMapInfo.raw_lines -join "`n") -match [regex]::Escape($MapId)))

# Byte-identity checks (root vs 42 copies)
$modInfoBytesIdentical = ($v42ModInfo.sha256  -ne '' -and $v42ModInfo.sha256  -eq $rootModInfo.sha256)
$mapInfoBytesIdentical = ($v42MapInfo.sha256  -ne '' -and $v42MapInfo.sha256  -eq $rootMapInfo.sha256)

# H8: mod.info map= field analysis
# Check whether mod.info contains a map= key that registers the map/media path.
$v42ModInfoMapValue   = Get-FieldValue $v42ModInfo.fields  'map'
$rootModInfoMapValue  = Get-FieldValue $rootModInfo.fields 'map'
$modInfoHasMapField   = ($v42ModInfoMapValue -ne '') -or ($rootModInfoMapValue -ne '')
$modInfoMapValueMatchesExpected = ($MapId -ne '') -and (
    ($v42ModInfoMapValue -eq $MapId) -or ($rootModInfoMapValue -eq $MapId))
# Variant F confirmed folder/id alignment does not fix registration.
# H8 is recommended when map= field is absent from mod.info.
$h5FolderIdAlignmentResult    = 'MAP7F_VARIANT_F_MAP_FOLDER_SCAN_EMPTY'
$h8ModInfoMapFieldRecommended = -not $modInfoHasMapField

# ---------------------------------------------------------------------------
# Hypothesis notes (no validity claims -- observations only)
# ---------------------------------------------------------------------------

$hypothesisNotes = [string[]]@(
    "H4: map.info field contract -- observed fields recorded above; completeness unverified without reference mod",
    "H5: mod.info id/folder match -- v42_mod_info_id_matches_expected=$($v42ModInfoIdMatchesExpected.ToString().ToLower()); root_mod_info_id_matches=$($rootModInfoIdMatchesExpected.ToString().ToLower())",
    "H6: map.info id match -- v42_map_info_id_matches_expected=$($v42MapInfoIdMatchesExpected.ToString().ToLower()); root_map_info_id_matches=$($rootMapInfoIdMatchesExpected.ToString().ToLower())",
    "H8: mod.info map= field -- check whether a map= field is present in mod.info to register media path (see fields above)"
)

# ---------------------------------------------------------------------------
# Build report
# ---------------------------------------------------------------------------

$report = [ordered]@{
    schema                               = 'pzmapforge.build42-map-metadata-contract.v0.3'
    candidate_root                       = $CandidateRoot
    expected_map_id                      = $MapId
    expected_mod_id                      = $ModId
    v42_mod_info                         = $v42ModInfo
    root_mod_info                        = $rootModInfo
    v42_map_info                         = $v42MapInfo
    root_map_info                        = $rootMapInfo
    v42_mod_info_id                      = $v42ModInfoId
    root_mod_info_id                     = $rootModInfoId
    v42_map_info_id                      = $v42MapInfoId
    root_map_info_id                     = $rootMapInfoId
    v42_mod_info_id_matches_expected     = $v42ModInfoIdMatchesExpected
    root_mod_info_id_matches_expected    = $rootModInfoIdMatchesExpected
    v42_map_info_id_matches_expected     = $v42MapInfoIdMatchesExpected
    root_map_info_id_matches_expected    = $rootMapInfoIdMatchesExpected
    v42_map_info_contains_map_id         = $v42MapInfoContainsMapId
    root_map_info_contains_map_id        = $rootMapInfoContainsMapId
    mod_info_bytes_identical             = $modInfoBytesIdentical
    map_info_bytes_identical             = $mapInfoBytesIdentical
    mod_info_has_map_field               = $modInfoHasMapField
    v42_mod_info_map_value               = $v42ModInfoMapValue
    root_mod_info_map_value              = $rootModInfoMapValue
    mod_info_map_value_matches_expected  = $modInfoMapValueMatchesExpected
    h5_folder_id_alignment_result        = $h5FolderIdAlignmentResult
    h8_mod_info_map_field_recommended    = $h8ModInfoMapFieldRecommended
    map_info_lots_is_none                = (Get-FieldValue $v42MapInfo.fields     'lots') -eq 'NONE' -or
                                           (Get-FieldValue $rootMapInfo.fields    'lots') -eq 'NONE' -or
                                           (Get-FieldValue $commonMapInfo.fields  'lots') -eq 'NONE'
    map_info_has_zoomX                   = (Get-FieldValue $v42MapInfo.fields     'zoomX') -ne '' -or
                                           (Get-FieldValue $rootMapInfo.fields    'zoomX') -ne '' -or
                                           (Get-FieldValue $commonMapInfo.fields  'zoomX') -ne ''
    map_info_has_zoomY                   = (Get-FieldValue $v42MapInfo.fields     'zoomY') -ne '' -or
                                           (Get-FieldValue $rootMapInfo.fields    'zoomY') -ne '' -or
                                           (Get-FieldValue $commonMapInfo.fields  'zoomY') -ne ''
    map_info_has_zoomS                   = (Get-FieldValue $v42MapInfo.fields     'zoomS') -ne '' -or
                                           (Get-FieldValue $rootMapInfo.fields    'zoomS') -ne '' -or
                                           (Get-FieldValue $commonMapInfo.fields  'zoomS') -ne ''
    hypothesis_notes                     = $hypothesisNotes
    variant_e_result                     = 'MAP7F_VARIANT_E_MAP_FOLDER_SCAN_EMPTY'
    map_line_variants_exhausted          = $true
    root_media_maps_experiment_failed    = $true
    root_mod_info_experiment_failed      = $true
    metadata_contract_focus              = $true
    public_playable_claim_allowed        = $false
    load_test_not_performed              = $true
    pz_assets_read                       = $false
}

$jsonPath = Join-Path $Output 'map-metadata-contract-report.json'
$mdPath   = Join-Path $Output 'map-metadata-contract-report.md'

$report | ConvertTo-Json -Depth 8 | Set-Content -Path $jsonPath -Encoding UTF8
Write-Output "JSON: $jsonPath"

# ---------------------------------------------------------------------------
# Markdown
# ---------------------------------------------------------------------------

$fence = '```'
$md = @"
# MAP-7J Build 42 Map Metadata Contract Inspection

${fence}text
expected_map_id=$MapId
expected_mod_id=$ModId
v42_mod_info_id=$v42ModInfoId
root_mod_info_id=$rootModInfoId
v42_map_info_id=$v42MapInfoId
root_map_info_id=$rootMapInfoId
v42_mod_info_id_matches_expected=$($v42ModInfoIdMatchesExpected.ToString().ToLower())
root_mod_info_id_matches_expected=$($rootModInfoIdMatchesExpected.ToString().ToLower())
v42_map_info_id_matches_expected=$($v42MapInfoIdMatchesExpected.ToString().ToLower())
root_map_info_id_matches_expected=$($rootMapInfoIdMatchesExpected.ToString().ToLower())
mod_info_bytes_identical=$($modInfoBytesIdentical.ToString().ToLower())
map_info_bytes_identical=$($mapInfoBytesIdentical.ToString().ToLower())
mod_info_has_map_field=$($modInfoHasMapField.ToString().ToLower())
v42_mod_info_map_value=$v42ModInfoMapValue
root_mod_info_map_value=$rootModInfoMapValue
mod_info_map_value_matches_expected=$($modInfoMapValueMatchesExpected.ToString().ToLower())
h5_folder_id_alignment_result=$h5FolderIdAlignmentResult
h8_mod_info_map_field_recommended=$($h8ModInfoMapFieldRecommended.ToString().ToLower())
variant_e_result=MAP7F_VARIANT_E_MAP_FOLDER_SCAN_EMPTY
metadata_contract_focus=true
public_playable_claim_allowed=false
${fence}

## 42/ mod.info

Exists: $($v42ModInfo.exists) | Size: $($v42ModInfo.size) | No-BOM: $($v42ModInfo.no_bom) | ASCII: $($v42ModInfo.is_ascii)

Fields: $($v42ModInfo.fields.Keys -join ', ')

## root mod.info

Exists: $($rootModInfo.exists) | Size: $($rootModInfo.size) | No-BOM: $($rootModInfo.no_bom) | ASCII: $($rootModInfo.is_ascii)

Fields: $($rootModInfo.fields.Keys -join ', ')

## 42/ map.info

Exists: $($v42MapInfo.exists) | Size: $($v42MapInfo.size) | No-BOM: $($v42MapInfo.no_bom) | ASCII: $($v42MapInfo.is_ascii)

Fields: $($v42MapInfo.fields.Keys -join ', ')

## root media/maps map.info

Exists: $($rootMapInfo.exists) | Size: $($rootMapInfo.size) | No-BOM: $($rootMapInfo.no_bom) | ASCII: $($rootMapInfo.is_ascii)

Fields: $($rootMapInfo.fields.Keys -join ', ')

## Byte-identity

mod.info root == 42/: $modInfoBytesIdentical
map.info root == 42/: $mapInfoBytesIdentical

## Hypotheses (observations only, no validity claims)

$(foreach ($n in $hypothesisNotes) { "- $n`n" })
## Non-claims

- No load test was performed by this script.
- public_playable_claim_allowed=false: binding.
"@

Set-Content -Path $mdPath -Value $md -Encoding ASCII
Write-Output "MD: $mdPath"
Write-Output ""
Write-Output "v42_mod_info_id:              $v42ModInfoId"
Write-Output "root_mod_info_id:             $rootModInfoId"
Write-Output "v42_map_info_id:              $v42MapInfoId"
Write-Output "root_map_info_id:             $rootMapInfoId"
Write-Output "mod_info_bytes_identical:     $modInfoBytesIdentical"
Write-Output "metadata_contract_focus:      true"
Write-Output "public_playable_claim_allowed=false"
Write-Output "Done."
