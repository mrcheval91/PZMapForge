#Requires -Version 5.1
<#
.SYNOPSIS
    MAP-8G: Compares a PZMapForge candidate map folder against a known-working
    Build 42 map folder in the version-scoped 42\media\maps\<MapId>\ layout.

    Reads map.info, mod.info, worldmap file presence, and binary file presence
    from both roots. Produces a comparison JSON + MD under -Output.

    Both -CandidateRoot and -ReferenceRoot must be under .local/.
    -Output must be under .local/.
    Does NOT copy any files. Does NOT read binary file contents.
    Does NOT write to Steam/PZ folders. Does NOT run PZ.

.PARAMETER CandidateRoot
    Required. Path under .local/ to the candidate map folder
    (the directory containing map.info, spawnpoints.lua, lotheader files, etc.)

.PARAMETER ReferenceRoot
    Required. Path under .local/ to the reference map folder
    (known-working Build 42 map folder placed by operator).

.PARAMETER Output
    Required. Path under .local/ for output JSON and MD.

.PARAMETER CandidateMapId
    Optional. Map ID used for labelling. Defaults to pzmapforge_build42_candidate_v4_001.

.PARAMETER ReferenceMapId
    Optional. Map ID of reference. Defaults to 'reference'.
#>

param(
    [Parameter(Mandatory=$true)][string]$CandidateRoot,
    [Parameter(Mandatory=$true)][string]$ReferenceRoot,
    [Parameter(Mandatory=$true)][string]$Output,
    [string]$CandidateMapId = 'pzmapforge_build42_candidate_v4_001',
    [string]$ReferenceMapId = 'reference'
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

Assert-LocalPath $CandidateRoot '-CandidateRoot'
Assert-LocalPath $ReferenceRoot '-ReferenceRoot'
Assert-LocalPath $Output '-Output'

New-Item -ItemType Directory -Force -Path $Output | Out-Null

Write-Output "MAP-8G: Known-Working Map Contract Comparator v2"
Write-Output "CandidateRoot: $CandidateRoot"
Write-Output "ReferenceRoot: $ReferenceRoot"
Write-Output "Output:        $Output"
Write-Output ""

# ---------------------------------------------------------------------------
# Helper: parse key=value text file
# ---------------------------------------------------------------------------

function Read-KVFile {
    param([string]$Path)
    $result = [ordered]@{}
    if (-not (Test-Path -LiteralPath $Path)) { return $result }
    foreach ($line in (Get-Content -LiteralPath $Path)) {
        $line = $line.Trim()
        if ($line -match '^([^=]+)=(.*)$') {
            $result[$Matches[1].Trim()] = $Matches[2].Trim()
        }
    }
    return $result
}

# ---------------------------------------------------------------------------
# Helper: check file existence (returns bool)
# ---------------------------------------------------------------------------

function Test-FilePresent {
    param([string]$Dir, [string]$File)
    return (Test-Path -LiteralPath (Join-Path $Dir $File))
}

# ---------------------------------------------------------------------------
# Read candidate fields
# ---------------------------------------------------------------------------

Write-Output "Reading candidate..."
$candMapInfo = Read-KVFile (Join-Path $CandidateRoot 'map.info')

$candFiles = [ordered]@{
    'map.info'               = Test-FilePresent $CandidateRoot 'map.info'
    'spawnpoints.lua'        = Test-FilePresent $CandidateRoot 'spawnpoints.lua'
    'objects.lua'            = Test-FilePresent $CandidateRoot 'objects.lua'
    'spawnregions.lua'       = Test-FilePresent $CandidateRoot 'spawnregions.lua'
    'thumb.png'              = Test-FilePresent $CandidateRoot 'thumb.png'
    'worldmap.xml'           = Test-FilePresent $CandidateRoot 'worldmap.xml'
    'worldmap-forest.xml'    = Test-FilePresent $CandidateRoot 'worldmap-forest.xml'
    'worldmap.xml.bin'       = Test-FilePresent $CandidateRoot 'worldmap.xml.bin'
    'worldmap-forest.xml.bin' = Test-FilePresent $CandidateRoot 'worldmap-forest.xml.bin'
    'worldmap.png'           = Test-FilePresent $CandidateRoot 'worldmap.png'
    'streets.xml.bin'        = Test-FilePresent $CandidateRoot 'streets.xml.bin'
}

$candLothCount = @(Get-ChildItem -LiteralPath $CandidateRoot -Filter '*.lotheader' -ErrorAction SilentlyContinue).Count
$candLotpCount = @(Get-ChildItem -LiteralPath $CandidateRoot -Filter '*.lotpack'   -ErrorAction SilentlyContinue).Count
$candBinCount  = @(Get-ChildItem -LiteralPath $CandidateRoot -Filter '*.bin'       -ErrorAction SilentlyContinue).Count

# ---------------------------------------------------------------------------
# Read reference fields
# ---------------------------------------------------------------------------

Write-Output "Reading reference..."
$refMapInfo = Read-KVFile (Join-Path $ReferenceRoot 'map.info')

$refFiles = [ordered]@{
    'map.info'               = Test-FilePresent $ReferenceRoot 'map.info'
    'spawnpoints.lua'        = Test-FilePresent $ReferenceRoot 'spawnpoints.lua'
    'objects.lua'            = Test-FilePresent $ReferenceRoot 'objects.lua'
    'spawnregions.lua'       = Test-FilePresent $ReferenceRoot 'spawnregions.lua'
    'thumb.png'              = Test-FilePresent $ReferenceRoot 'thumb.png'
    'worldmap.xml'           = Test-FilePresent $ReferenceRoot 'worldmap.xml'
    'worldmap-forest.xml'    = Test-FilePresent $ReferenceRoot 'worldmap-forest.xml'
    'worldmap.xml.bin'       = Test-FilePresent $ReferenceRoot 'worldmap.xml.bin'
    'worldmap-forest.xml.bin' = Test-FilePresent $ReferenceRoot 'worldmap-forest.xml.bin'
    'worldmap.png'           = Test-FilePresent $ReferenceRoot 'worldmap.png'
    'streets.xml.bin'        = Test-FilePresent $ReferenceRoot 'streets.xml.bin'
}

$refLothCount = @(Get-ChildItem -LiteralPath $ReferenceRoot -Filter '*.lotheader' -ErrorAction SilentlyContinue).Count
$refLotpCount = @(Get-ChildItem -LiteralPath $ReferenceRoot -Filter '*.lotpack'   -ErrorAction SilentlyContinue).Count
$refBinCount  = @(Get-ChildItem -LiteralPath $ReferenceRoot -Filter '*.bin'       -ErrorAction SilentlyContinue).Count

# ---------------------------------------------------------------------------
# Compute diffs
# ---------------------------------------------------------------------------

$mapInfoDiffs = [System.Collections.Generic.List[object]]::new()
$allKeys = [System.Collections.Generic.HashSet[string]]::new()
foreach ($k in $candMapInfo.Keys) { [void]$allKeys.Add($k) }
foreach ($k in $refMapInfo.Keys)  { [void]$allKeys.Add($k) }
foreach ($k in $allKeys) {
    $cv = if ($candMapInfo.Contains($k)) { $candMapInfo[$k] } else { '(absent)' }
    $rv = if ($refMapInfo.Contains($k))  { $refMapInfo[$k] }  else { '(absent)' }
    if ($cv -ne $rv) {
        $mapInfoDiffs.Add([ordered]@{ key = $k; candidate = $cv; reference = $rv })
    }
}

$fileSetDiffs = [System.Collections.Generic.List[object]]::new()
foreach ($f in $refFiles.Keys) {
    if ([bool]$refFiles[$f] -and -not [bool]$candFiles[$f]) {
        $fileSetDiffs.Add([ordered]@{ file = $f; in_reference = $true; in_candidate = $false })
    }
}
$candidateExtra = [System.Collections.Generic.List[object]]::new()
foreach ($f in $candFiles.Keys) {
    if ([bool]$candFiles[$f] -and -not [bool]$refFiles[$f]) {
        $candidateExtra.Add([ordered]@{ file = $f; in_candidate = $true; in_reference = $false })
    }
}

$worldmapBinDiffers = ([bool]$refFiles['worldmap.xml.bin']) -ne ([bool]$candFiles['worldmap.xml.bin'])
$spawnregionsDiffers = ([bool]$refFiles['spawnregions.lua']) -ne ([bool]$candFiles['spawnregions.lua'])

# ---------------------------------------------------------------------------
# Build report
# ---------------------------------------------------------------------------

$report = [ordered]@{
    schema                     = 'pzmapforge.map8g-comparator-v2.v0.1'
    candidate_map_id           = $CandidateMapId
    reference_map_id           = $ReferenceMapId
    candidate_root             = $CandidateRoot
    reference_root             = $ReferenceRoot
    candidate_file_set         = $candFiles
    reference_file_set         = $refFiles
    candidate_lotheader_count  = $candLothCount
    reference_lotheader_count  = $refLothCount
    candidate_lotpack_count    = $candLotpCount
    reference_lotpack_count    = $refLotpCount
    candidate_bin_count        = $candBinCount
    reference_bin_count        = $refBinCount
    candidate_map_info         = $candMapInfo
    reference_map_info         = $refMapInfo
    map_info_field_differences = [object[]]@($mapInfoDiffs.ToArray())
    file_set_differences       = [object[]]@($fileSetDiffs.ToArray())
    candidate_extra_files      = [object[]]@($candidateExtra.ToArray())
    worldmap_bin_differs       = $worldmapBinDiffers
    spawnregions_lua_differs   = $spawnregionsDiffers
    no_files_copied            = $true
    no_binary_contents_read    = $true
    no_pz_run_by_script        = $true
    public_playable_claim_allowed = $false
}

$jsonPath = Join-Path $Output 'build42-known-working-map-contract-v2.json'
$report | ConvertTo-Json -Depth 6 | Set-Content -Path $jsonPath -Encoding ASCII
Write-Output "Wrote: build42-known-working-map-contract-v2.json"

# ---------------------------------------------------------------------------
# Write MD
# ---------------------------------------------------------------------------

$mdLines = [System.Collections.Generic.List[string]]::new()
$mdLines.Add("# MAP-8G: Known-Working Map Contract Comparator v2")
$mdLines.Add("")
$mdLines.Add("``````text")
$mdLines.Add("MAP8G_KNOWN_WORKING_CONTRACT_COMPARATOR_DEFINED")
$mdLines.Add("no_files_copied: true")
$mdLines.Add("public_playable_claim_allowed: false")
$mdLines.Add("``````")
$mdLines.Add("")
$mdLines.Add("## map.info differences")
$mdLines.Add("")
if ($mapInfoDiffs.Count -eq 0) {
    $mdLines.Add("No map.info field differences found.")
} else {
    $mdLines.Add("| Key | Candidate | Reference |")
    $mdLines.Add("|---|---|---|")
    foreach ($d in $mapInfoDiffs) {
        $mdLines.Add("| $($d.key) | $($d.candidate) | $($d.reference) |")
    }
}
$mdLines.Add("")
$mdLines.Add("## File set differences (in reference, absent from candidate)")
$mdLines.Add("")
if ($fileSetDiffs.Count -eq 0) {
    $mdLines.Add("No files present in reference but absent from candidate.")
} else {
    foreach ($d in $fileSetDiffs) {
        $mdLines.Add("  MISSING_FROM_CANDIDATE  $($d.file)")
    }
}
$mdLines.Add("")
$mdLines.Add("## Candidate extra files (in candidate, absent from reference)")
$mdLines.Add("")
if ($candidateExtra.Count -eq 0) {
    $mdLines.Add("No candidate-only extra files.")
} else {
    foreach ($d in $candidateExtra) {
        $mdLines.Add("  CANDIDATE_ONLY  $($d.file)")
    }
}
$mdLines.Add("")
$mdLines.Add("## Binary file counts")
$mdLines.Add("")
$mdLines.Add("| Type | Candidate | Reference |")
$mdLines.Add("|---|---|---|")
$mdLines.Add("| .lotheader | $candLothCount | $refLothCount |")
$mdLines.Add("| .lotpack | $candLotpCount | $refLotpCount |")
$mdLines.Add("| .bin | $candBinCount | $refBinCount |")
$mdLines.Add("")
$mdLines.Add("## Key discriminators")
$mdLines.Add("")
$mdLines.Add("``````text")
$mdLines.Add("worldmap_bin_differs:    $($worldmapBinDiffers.ToString().ToLower())")
$mdLines.Add("spawnregions_differs:    $($spawnregionsDiffers.ToString().ToLower())")
$mdLines.Add("map_info_diff_count:     $($mapInfoDiffs.Count)")
$mdLines.Add("missing_from_candidate:  $($fileSetDiffs.Count) files")
$mdLines.Add("``````")

$mdPath = Join-Path $Output 'build42-known-working-map-contract-v2.md'
Set-Content -Path $mdPath -Value ($mdLines -join "`n") -Encoding ASCII
Write-Output "Wrote: build42-known-working-map-contract-v2.md"

Write-Output ""
Write-Output "MAP-8G comparator complete."
Write-Output "map_info_field_differences: $($mapInfoDiffs.Count)"
Write-Output "file_set_differences: $($fileSetDiffs.Count)"
Write-Output "worldmap_bin_differs: $worldmapBinDiffers"
Write-Output "spawnregions_lua_differs: $spawnregionsDiffers"
Write-Output "no_files_copied: true"
Write-Output "public_playable_claim_allowed: false"
