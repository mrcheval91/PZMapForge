#Requires -Version 5.1
<#
.SYNOPSIS
    MAP-7M: Compares the PZMapForge candidate mod structure against a
    human-provided known-working Build 42 map mod under .local/.

    Reads ONLY from .local/ paths.
    Does NOT read Workshop, Zomboid mods, Server, or PZ install paths.
    Does NOT write outside .local/.
    Does NOT run PZ.
    Does NOT copy binary contents into reports.

    Records file names, sizes, SHA-256 hashes, field names, and layout
    differences only.

.PARAMETER CandidateRoot
    Path under .local/ for the candidate mod root.

.PARAMETER ReferenceRoot
    Path under .local/ for the known-working reference mod root.

.PARAMETER Output
    Path under .local/ for report output.

.PARAMETER MapId
    Map ID for the candidate mod. Default: pzmapforge_build42_candidate_v4_001

.PARAMETER ReferenceMapId
    Map ID for the reference mod. Defaults to the value of -MapId when omitted.
    Use when the reference mod has a different map folder name than the candidate.

.EXAMPLE
    powershell -ExecutionPolicy Bypass -File .\scripts\inspect-build42-known-working-map-contract.ps1 `
        -CandidateRoot .\.local\map7m-packet\experiment-h-candidate\pzmapforge_build42_candidate_v4_001 `
        -ReferenceRoot .\.local\map7m-packet\reference-known-working-map\Dru_map `
        -Output .\.local\map7m-packet\comparison-dru-map `
        -MapId pzmapforge_build42_candidate_v4_001 `
        -ReferenceMapId Dru_map
#>

param(
    [Parameter(Mandatory=$true)][string]$CandidateRoot,
    [Parameter(Mandatory=$true)][string]$ReferenceRoot,
    [Parameter(Mandatory=$true)][string]$Output,
    [string]$MapId          = 'pzmapforge_build42_candidate_v4_001',
    [string]$ReferenceMapId = ''
)

if ($ReferenceMapId -eq '') { $ReferenceMapId = $MapId }

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

Assert-LocalPath $CandidateRoot '-CandidateRoot'
Assert-LocalPath $ReferenceRoot '-ReferenceRoot'
Assert-LocalPath $Output        '-Output'

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
    foreach ($byte in $b) {
        if ($byte -gt 0x7E -and $byte -ne 0x0A -and $byte -ne 0x0D) { return $false }
    }
    return $true
}

function Get-FileSha256 {
    param([string]$Path)
    if (-not (Test-Path -LiteralPath $Path)) { return '' }
    $sha = [System.Security.Cryptography.SHA256]::Create()
    $st  = [System.IO.File]::OpenRead($Path)
    try { $h = $sha.ComputeHash($st); return (($h | ForEach-Object { $_.ToString('x2') }) -join '') }
    finally { $st.Dispose(); $sha.Dispose() }
}

function Parse-IniLike {
    param([string]$Path)
    $result = [ordered]@{}
    if (-not (Test-Path -LiteralPath $Path)) { return $result }
    Get-Content -LiteralPath $Path | ForEach-Object {
        if ($_ -match '^(\w[\w\s]*?)\s*=\s*(.*)$') {
            $result[$Matches[1].Trim()] = $Matches[2].Trim()
        }
    }
    return $result
}

# ---------------------------------------------------------------------------
# Layout scan for a given root
# ---------------------------------------------------------------------------

function Get-ModLayout {
    param([string]$Root, [string]$MapId2)
    $layout = [ordered]@{
        root                  = $Root
        has_42_folder         = Test-Path -LiteralPath (Join-Path $Root '42')
        has_42_0_folder       = Test-Path -LiteralPath (Join-Path $Root '42.0')
        has_common_folder     = Test-Path -LiteralPath (Join-Path $Root 'common')
        has_root_mod_info     = Test-Path -LiteralPath (Join-Path $Root 'mod.info')
        has_42_mod_info       = Test-Path -LiteralPath (Join-Path $Root '42\mod.info')
        has_42_0_mod_info     = Test-Path -LiteralPath (Join-Path $Root '42.0\mod.info')
        has_common_mod_info   = Test-Path -LiteralPath (Join-Path $Root 'common\mod.info')
        has_common_media_maps = Test-Path -LiteralPath (Join-Path $Root "common\media\maps\$MapId2")
        has_42_media_maps     = Test-Path -LiteralPath (Join-Path $Root "42\media\maps\$MapId2")
        has_42_0_media_maps   = Test-Path -LiteralPath (Join-Path $Root "42.0\media\maps\$MapId2")
        has_root_media_maps   = Test-Path -LiteralPath (Join-Path $Root "media\maps\$MapId2")
    }

    # Locate primary mod.info (search order: common, 42, 42.0, root)
    $modInfoPath = ''
    foreach ($rel in @('common\mod.info', '42\mod.info', '42.0\mod.info', 'mod.info')) {
        $p = Join-Path $Root $rel
        if (Test-Path -LiteralPath $p) { $modInfoPath = $p; break }
    }

    # Locate primary map.info
    $mapInfoPath = ''
    foreach ($rel in @("common\media\maps\$MapId2\map.info",
                        "42\media\maps\$MapId2\map.info",
                        "42.0\media\maps\$MapId2\map.info",
                        "media\maps\$MapId2\map.info")) {
        $p = Join-Path $Root $rel
        if (Test-Path -LiteralPath $p) { $mapInfoPath = $p; break }
    }

    # Locate primary map data directory
    $mapDataDir = ''
    foreach ($rel in @("common\media\maps\$MapId2",
                        "42\media\maps\$MapId2",
                        "42.0\media\maps\$MapId2",
                        "media\maps\$MapId2")) {
        $p = Join-Path $Root $rel
        if (Test-Path -LiteralPath $p) { $mapDataDir = $p; break }
    }

    $layout['primary_mod_info_path']  = $modInfoPath
    $layout['primary_map_info_path']  = $mapInfoPath
    $layout['primary_map_data_dir']   = $mapDataDir
    $layout['mod_info_fields']        = Parse-IniLike $modInfoPath
    $layout['map_info_fields']        = Parse-IniLike $mapInfoPath
    $layout['mod_info_no_bom']        = if ($modInfoPath -ne '') { -not (Test-HasBom $modInfoPath) } else { $false }
    $layout['mod_info_ascii']         = if ($modInfoPath -ne '') { Test-IsAscii $modInfoPath } else { $false }
    $layout['map_info_no_bom']        = if ($mapInfoPath -ne '') { -not (Test-HasBom $mapInfoPath) } else { $false }
    $layout['map_info_ascii']         = if ($mapInfoPath -ne '') { Test-IsAscii $mapInfoPath } else { $false }

    # Map data file inventory
    $mapFiles = [ordered]@{}
    if ($mapDataDir -ne '') {
        foreach ($f in @('map.info', 'spawnpoints.lua', 'objects.lua',
                          'mod.info', 'thumb.png', 'worldmap.xml', 'worldmap-forest.xml',
                          '0_0.lotheader', 'world_0_0.lotpack', '0_0.lotpack', 'chunkdata_0_0.bin')) {
            $fp = Join-Path $mapDataDir $f
            $mapFiles[$f] = [ordered]@{
                exists = Test-Path -LiteralPath $fp
                size   = if (Test-Path -LiteralPath $fp) { (Get-Item -LiteralPath $fp).Length } else { 0 }
                sha256 = if (Test-Path -LiteralPath $fp) { Get-FileSha256 $fp } else { '' }
            }
        }
        # Check maps/ subfolder
        $mapsSubdir = Join-Path $mapDataDir 'maps'
        $mapFiles['maps_subfolder'] = [ordered]@{
            exists = Test-Path -LiteralPath $mapsSubdir
            size   = 0
            sha256 = ''
        }
    }
    $layout['map_data_files'] = $mapFiles

    # Lotpack naming convention
    $layout['uses_world_xy_lotpack'] = ($mapDataDir -ne '') -and (Test-Path -LiteralPath (Join-Path $mapDataDir 'world_0_0.lotpack'))
    $layout['uses_plain_xy_lotpack'] = ($mapDataDir -ne '') -and (Test-Path -LiteralPath (Join-Path $mapDataDir '0_0.lotpack'))

    return $layout
}

# ---------------------------------------------------------------------------
# Scan both roots
# ---------------------------------------------------------------------------

Write-Output "Scanning candidate: $CandidateRoot (MapId=$MapId)"
$cand = Get-ModLayout -Root $CandidateRoot -MapId2 $MapId
Write-Output "Scanning reference: $ReferenceRoot (ReferenceMapId=$ReferenceMapId)"
$ref  = Get-ModLayout -Root $ReferenceRoot -MapId2 $ReferenceMapId

# ---------------------------------------------------------------------------
# Compare mod.info fields
# ---------------------------------------------------------------------------

$candModFields = [string[]]@($cand.mod_info_fields.Keys)
$refModFields  = [string[]]@($ref.mod_info_fields.Keys)

$modFieldsInRefNotCand  = [string[]]@($refModFields  | Where-Object { $candModFields -notcontains $_ })
$modFieldsInCandNotRef  = [string[]]@($candModFields | Where-Object { $refModFields  -notcontains $_ })
$modFieldsInBoth        = [string[]]@($candModFields | Where-Object { $refModFields  -contains $_ })

$modValueDifferences = [ordered]@{}
foreach ($k in $modFieldsInBoth) {
    $cv = [string]$cand.mod_info_fields[$k]
    $rv = [string]$ref.mod_info_fields[$k]
    if ($cv -ne $rv) {
        $modValueDifferences[$k] = [ordered]@{ candidate = $cv; reference = $rv }
    }
}

# ---------------------------------------------------------------------------
# Compare map.info fields
# ---------------------------------------------------------------------------

$candMapFields = [string[]]@($cand.map_info_fields.Keys)
$refMapFields  = [string[]]@($ref.map_info_fields.Keys)

$mapFieldsInRefNotCand  = [string[]]@($refMapFields  | Where-Object { $candMapFields -notcontains $_ })
$mapFieldsInCandNotRef  = [string[]]@($candMapFields | Where-Object { $refMapFields  -notcontains $_ })
$mapFieldsInBoth        = [string[]]@($candMapFields | Where-Object { $refMapFields  -contains $_ })

$mapValueDifferences = [ordered]@{}
foreach ($k in $mapFieldsInBoth) {
    $cv = [string]$cand.map_info_fields[$k]
    $rv = [string]$ref.map_info_fields[$k]
    if ($cv -ne $rv) {
        $mapValueDifferences[$k] = [ordered]@{ candidate = $cv; reference = $rv }
    }
}

# ---------------------------------------------------------------------------
# Layout structural comparison
# ---------------------------------------------------------------------------

$version_folder_differs = ($cand.has_42_folder -ne $ref.has_42_folder) -or
                          ($cand.has_42_0_folder -ne $ref.has_42_0_folder)
$common_folder_differs  = ($cand.has_common_folder -ne $ref.has_common_folder)

# Decision tree signals
$decision_signals = [string[]]@()
if ($modFieldsInRefNotCand.Count -gt 0)   { $decision_signals += 'mod_info_fields_in_reference_not_candidate' }
if ($mapFieldsInRefNotCand.Count -gt 0)   { $decision_signals += 'map_info_fields_in_reference_not_candidate' }
if ($ref.has_42_0_folder -and -not $cand.has_42_0_folder) { $decision_signals += 'reference_uses_42_0_candidate_uses_42' }
if ($version_folder_differs)              { $decision_signals += 'version_folder_name_differs' }
if ($common_folder_differs)               { $decision_signals += 'common_folder_presence_differs' }
if ($decision_signals.Count -eq 0)        { $decision_signals += 'no_structural_differences_found' }

# ---------------------------------------------------------------------------
# Build report
# ---------------------------------------------------------------------------

$report = [ordered]@{
    schema                                = 'pzmapforge.build42-known-working-contract.v0.2'
    candidate_root                        = $CandidateRoot
    reference_root                        = $ReferenceRoot
    candidate_map_id                      = $MapId
    reference_map_id                      = $ReferenceMapId
    map_id                                = $MapId
    candidate_layout                      = $cand
    reference_layout                      = $ref
    mod_info_fields_candidate             = $candModFields
    mod_info_fields_reference             = $refModFields
    mod_info_fields_in_reference_not_candidate = $modFieldsInRefNotCand
    mod_info_fields_in_candidate_not_reference = $modFieldsInCandNotRef
    mod_info_value_differences            = $modValueDifferences
    map_info_fields_candidate             = $candMapFields
    map_info_fields_reference             = $refMapFields
    map_info_fields_in_reference_not_candidate = $mapFieldsInRefNotCand
    map_info_fields_in_candidate_not_reference = $mapFieldsInCandNotRef
    map_info_value_differences            = $mapValueDifferences
    version_folder_differs                = $version_folder_differs
    common_folder_differs                 = $common_folder_differs
    decision_signals                      = $decision_signals
    variant_h_result                      = 'MAP7F_VARIANT_H_MAP_FOLDER_SCAN_EMPTY'
    variants_abcdefgh_exhausted           = $true
    public_playable_claim_allowed         = $false
    load_test_not_performed               = $true
    pz_assets_read                        = $false
    no_automatic_pz_read_or_write         = $true
}

$jsonPath = Join-Path $Output 'map-known-working-contract-report.json'
$mdPath   = Join-Path $Output 'map-known-working-contract-report.md'

$report | ConvertTo-Json -Depth 8 | Set-Content -Path $jsonPath -Encoding UTF8
Write-Output "JSON: $jsonPath"

$fence = '```'
$md = @"
# MAP-7M Known-Working Map Contract Comparison

${fence}text
candidate_root=$CandidateRoot
reference_root=$ReferenceRoot
candidate_map_id=$MapId
reference_map_id=$ReferenceMapId
variant_h_result=MAP7F_VARIANT_H_MAP_FOLDER_SCAN_EMPTY
variants_abcdefgh_exhausted=true
version_folder_differs=$version_folder_differs
common_folder_differs=$common_folder_differs
mod_info_fields_in_reference_not_candidate=$($modFieldsInRefNotCand -join ', ')
map_info_fields_in_reference_not_candidate=$($mapFieldsInRefNotCand -join ', ')
decision_signals=$($decision_signals -join ', ')
public_playable_claim_allowed=false
${fence}

## Candidate layout

has_42_folder: $($cand.has_42_folder) | has_42_0_folder: $($cand.has_42_0_folder) | has_common_folder: $($cand.has_common_folder)
primary_mod_info: $($cand.primary_mod_info_path)
primary_map_info: $($cand.primary_map_info_path)
mod_info_fields: $($candModFields -join ', ')
map_info_fields: $($candMapFields -join ', ')
uses_world_xy_lotpack: $($cand.uses_world_xy_lotpack)

## Reference layout

has_42_folder: $($ref.has_42_folder) | has_42_0_folder: $($ref.has_42_0_folder) | has_common_folder: $($ref.has_common_folder)
primary_mod_info: $($ref.primary_mod_info_path)
primary_map_info: $($ref.primary_map_info_path)
mod_info_fields: $($refModFields -join ', ')
map_info_fields: $($refMapFields -join ', ')
uses_world_xy_lotpack: $($ref.uses_world_xy_lotpack)

## Field gaps (in reference, not in candidate)

mod.info: $($modFieldsInRefNotCand -join ', ')
map.info: $($mapFieldsInRefNotCand -join ', ')

## Decision signals

$($decision_signals | ForEach-Object { "- $_`n" })
## Non-claims

- No load test was performed by this script.
- No binary contents copied into report.
- public_playable_claim_allowed=false: binding.
"@

Set-Content -Path $mdPath -Value $md -Encoding ASCII
Write-Output "MD: $mdPath"
Write-Output ""
Write-Output "mod_info_fields_in_ref_not_cand: $($modFieldsInRefNotCand -join ', ')"
Write-Output "map_info_fields_in_ref_not_cand: $($mapFieldsInRefNotCand -join ', ')"
Write-Output "decision_signals: $($decision_signals -join ', ')"
Write-Output "public_playable_claim_allowed=false"
Write-Output "Done."
