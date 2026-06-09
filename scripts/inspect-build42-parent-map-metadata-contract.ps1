#Requires -Version 5.1
<#
.SYNOPSIS
    MAP-8K: Compares PZMapForge parent map folder metadata against a
    known-working Project Russia parent map folder.

    Reads map.info key/value fields, file presence, binary file counts,
    and text-file summaries from both roots. Produces comparison JSON + MD.

    -Output must be under .local/. Script exits nonzero otherwise.
    Does NOT copy any files from -ReferenceParentRoot.
    Does NOT read binary file contents (*.lotheader, *.lotpack, chunkdata_*.bin,
    *.bin, *.png, *.bik, *.pack).
    Does NOT write to Steam/PZ folders. Does NOT run PZ.

.PARAMETER CandidateParentRoot
    Required. Path to the PZMapForge parent map folder.

.PARAMETER ReferenceParentRoot
    Required. Path to the known-working reference parent map folder (read-only).

.PARAMETER Output
    Required. Output path (must be under .local/).

.PARAMETER CandidateParentMapId
    Optional. Label for the candidate. Default: PZMapForge.

.PARAMETER ReferenceParentMapId
    Optional. Label for the reference. Default: Project Russia.
#>

param(
    [Parameter(Mandatory=$true)][string]$CandidateParentRoot,
    [Parameter(Mandatory=$true)][string]$ReferenceParentRoot,
    [Parameter(Mandatory=$true)][string]$Output,
    [string]$CandidateParentMapId = 'PZMapForge',
    [string]$ReferenceParentMapId = 'Project Russia'
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

Write-Output "MAP-8K: Parent Map Metadata Contract Comparator"
Write-Output "CandidateParentRoot: $CandidateParentRoot"
Write-Output "ReferenceParentRoot: $ReferenceParentRoot"
Write-Output "Output:              $Output"
Write-Output ""

# ---------------------------------------------------------------------------
# Helpers
# ---------------------------------------------------------------------------

function Read-MapInfo {
    param([string]$Root)
    $result = [ordered]@{}
    $path = Join-Path $Root 'map.info'
    if (-not (Test-Path -LiteralPath $path)) { return $result }
    foreach ($line in (Get-Content -LiteralPath $path)) {
        $line = $line.Trim()
        if ($line -match '^([^=]+)=(.*)$') {
            $result[$Matches[1].Trim()] = $Matches[2].Trim()
        }
    }
    return $result
}

function Get-TextFileSummary {
    param([string]$Root, [string]$FileName)
    $path = Join-Path $Root $FileName
    if (-not (Test-Path -LiteralPath $path)) {
        return [ordered]@{
            exists                = $false
            size_bytes            = 0
            line_count            = 0
            skeletal_or_substantial = 'absent'
        }
    }
    $bytes = (Get-Item -LiteralPath $path).Length
    $lines = @(Get-Content -LiteralPath $path).Count
    $class = if ($bytes -lt 500) { 'skeletal' } else { 'substantial' }
    return [ordered]@{
        exists                = $true
        size_bytes            = [int]$bytes
        line_count            = $lines
        skeletal_or_substantial = $class
    }
}

function Get-CellBinaryCounts {
    param([string]$Root)
    $loth = @(Get-ChildItem -LiteralPath $Root -Filter '*.lotheader'  -ErrorAction SilentlyContinue).Count
    $lotp = @(Get-ChildItem -LiteralPath $Root -Filter '*.lotpack'    -ErrorAction SilentlyContinue).Count
    $chkd = @(Get-ChildItem -LiteralPath $Root -Filter 'chunkdata_*.bin' -ErrorAction SilentlyContinue).Count
    return [ordered]@{
        lotheader_count = $loth
        lotpack_count   = $lotp
        chunkdata_count = $chkd
    }
}

# ---------------------------------------------------------------------------
# Read candidate
# ---------------------------------------------------------------------------

Write-Output "Reading candidate ($CandidateParentMapId)..."
$candMapInfo = Read-MapInfo $CandidateParentRoot
$candCellCounts   = Get-CellBinaryCounts $CandidateParentRoot
$candWorldmapXml  = Get-TextFileSummary $CandidateParentRoot 'worldmap.xml'
$candObjectsLua   = Get-TextFileSummary $CandidateParentRoot 'objects.lua'
$candSpawnptsLua  = Get-TextFileSummary $CandidateParentRoot 'spawnpoints.lua'

$candHasLots        = $candMapInfo.Contains('lots')
$candFixed2x        = if ($candMapInfo.Contains('fixed2x'))     { $candMapInfo['fixed2x']     } else { '(absent)' }
$candDescription    = if ($candMapInfo.Contains('description')) { $candMapInfo['description'] } else { '(absent)' }
$candDemoVideo      = $candMapInfo.Contains('demoVideo')
$candWorldmapBin    = Test-Path -LiteralPath (Join-Path $CandidateParentRoot 'worldmap.xml.bin')
$candStreetsXmlBin  = Test-Path -LiteralPath (Join-Path $CandidateParentRoot 'streets.xml.bin')
$candObjectsSize    = if ($candObjectsLua.exists)  { $candObjectsLua.size_bytes }  else { 0 }
$candSpawnptsSize   = if ($candSpawnptsLua.exists) { $candSpawnptsLua.size_bytes } else { 0 }

# ---------------------------------------------------------------------------
# Read reference
# ---------------------------------------------------------------------------

Write-Output "Reading reference ($ReferenceParentMapId)..."
$refMapInfo = Read-MapInfo $ReferenceParentRoot
$refCellCounts   = Get-CellBinaryCounts $ReferenceParentRoot
$refWorldmapXml  = Get-TextFileSummary $ReferenceParentRoot 'worldmap.xml'
$refObjectsLua   = Get-TextFileSummary $ReferenceParentRoot 'objects.lua'
$refSpawnptsLua  = Get-TextFileSummary $ReferenceParentRoot 'spawnpoints.lua'

$refHasLots        = $refMapInfo.Contains('lots')
$refFixed2x        = if ($refMapInfo.Contains('fixed2x'))     { $refMapInfo['fixed2x']     } else { '(absent)' }
$refDescription    = if ($refMapInfo.Contains('description')) { $refMapInfo['description'] } else { '(absent)' }
$refDemoVideo      = $refMapInfo.Contains('demoVideo')
$refWorldmapBin    = Test-Path -LiteralPath (Join-Path $ReferenceParentRoot 'worldmap.xml.bin')
$refStreetsXmlBin  = Test-Path -LiteralPath (Join-Path $ReferenceParentRoot 'streets.xml.bin')
$refObjectsSize    = if ($refObjectsLua.exists)  { $refObjectsLua.size_bytes }  else { 0 }
$refSpawnptsSize   = if ($refSpawnptsLua.exists) { $refSpawnptsLua.size_bytes } else { 0 }

# ---------------------------------------------------------------------------
# Compute map.info field differences
# ---------------------------------------------------------------------------

$mapInfoDiffs = [System.Collections.Generic.List[object]]::new()
$allKeys = [System.Collections.Generic.HashSet[string]]::new()
foreach ($k in $candMapInfo.Keys) { [void]$allKeys.Add($k) }
foreach ($k in $refMapInfo.Keys)  { [void]$allKeys.Add($k) }
foreach ($k in $allKeys) {
    $cv = if ($candMapInfo.Contains($k)) { $candMapInfo[$k] } else { '(absent)' }
    $rv = if ($refMapInfo.Contains($k))  { $refMapInfo[$k]  } else { '(absent)' }
    if ($cv -ne $rv) {
        $mapInfoDiffs.Add([ordered]@{ key = $k; candidate = $cv; reference = $rv })
    }
}

# ---------------------------------------------------------------------------
# Build output report
# ---------------------------------------------------------------------------

$report = [ordered]@{
    schema                          = 'pzmapforge.map8k-parent-metadata-contract.v0.1'
    candidate_parent_map_id         = $CandidateParentMapId
    reference_parent_map_id         = $ReferenceParentMapId
    candidate_layout_hint           = "common\media\maps\$CandidateParentMapId"
    reference_layout_hint           = "common\media\maps\$ReferenceParentMapId"
    candidate_map_info_fields       = $candMapInfo
    reference_map_info_fields       = $refMapInfo
    map_info_field_differences      = [object[]]@($mapInfoDiffs.ToArray())
    candidate_has_lots_field        = $candHasLots
    reference_has_lots_field        = $refHasLots
    candidate_fixed2x               = $candFixed2x
    reference_fixed2x               = $refFixed2x
    candidate_description           = $candDescription
    reference_description           = $refDescription
    candidate_demoVideo_present     = $candDemoVideo
    reference_demoVideo_present     = $refDemoVideo
    candidate_cell_binary_counts    = $candCellCounts
    reference_cell_binary_counts    = $refCellCounts
    candidate_worldmap_xml          = $candWorldmapXml
    reference_worldmap_xml          = $refWorldmapXml
    candidate_worldmap_xml_bin_present = $candWorldmapBin
    reference_worldmap_xml_bin_present = $refWorldmapBin
    candidate_streets_xml_bin_present  = $candStreetsXmlBin
    reference_streets_xml_bin_present  = $refStreetsXmlBin
    candidate_objects_lua_size      = $candObjectsSize
    reference_objects_lua_size      = $refObjectsSize
    candidate_spawnpoints_lua_size  = $candSpawnptsSize
    reference_spawnpoints_lua_size  = $refSpawnptsSize
    binary_contents_read            = $false
    third_party_files_copied        = $false
    no_project_russia_files_copied  = $true
    playable_claim_allowed          = $false
    binary_writer_gate_closed       = $true
}

$jsonPath = Join-Path $Output 'build42-parent-map-metadata-contract.json'
$report | ConvertTo-Json -Depth 5 | Set-Content -Path $jsonPath -Encoding ASCII
Write-Output "Wrote: build42-parent-map-metadata-contract.json"

# ---------------------------------------------------------------------------
# Write MD
# ---------------------------------------------------------------------------

$mdLines = [System.Collections.Generic.List[string]]::new()
$mdLines.Add("# MAP-8K: Parent Map Metadata Contract Comparator")
$mdLines.Add("")
$mdLines.Add("``````text")
$mdLines.Add("MAP8K_PARENT_METADATA_CONTRACT_COMPARATOR_DEFINED")
$mdLines.Add("binary_contents_read: false")
$mdLines.Add("third_party_files_copied: false")
$mdLines.Add("playable_claim_allowed: false")
$mdLines.Add("binary_writer_gate_closed: true")
$mdLines.Add("``````")
$mdLines.Add("")
$mdLines.Add("## map.info field differences")
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
$mdLines.Add("## Cell binary counts")
$mdLines.Add("")
$mdLines.Add("| Type | Candidate | Reference |")
$mdLines.Add("|---|---|---|")
$mdLines.Add("| .lotheader | $($candCellCounts.lotheader_count) | $($refCellCounts.lotheader_count) |")
$mdLines.Add("| .lotpack | $($candCellCounts.lotpack_count) | $($refCellCounts.lotpack_count) |")
$mdLines.Add("| chunkdata_*.bin | $($candCellCounts.chunkdata_count) | $($refCellCounts.chunkdata_count) |")
$mdLines.Add("")
$mdLines.Add("## Key field comparison")
$mdLines.Add("")
$mdLines.Add("``````text")
$mdLines.Add("candidate_has_lots_field:           $($candHasLots.ToString().ToLower())")
$mdLines.Add("reference_has_lots_field:           $($refHasLots.ToString().ToLower())")
$mdLines.Add("candidate_fixed2x:                  $candFixed2x")
$mdLines.Add("reference_fixed2x:                  $refFixed2x")
$mdLines.Add("candidate_demoVideo_present:        $($candDemoVideo.ToString().ToLower())")
$mdLines.Add("reference_demoVideo_present:        $($refDemoVideo.ToString().ToLower())")
$mdLines.Add("candidate_worldmap_xml_bin_present: $($candWorldmapBin.ToString().ToLower())")
$mdLines.Add("reference_worldmap_xml_bin_present: $($refWorldmapBin.ToString().ToLower())")
$mdLines.Add("candidate_streets_xml_bin_present:  $($candStreetsXmlBin.ToString().ToLower())")
$mdLines.Add("reference_streets_xml_bin_present:  $($refStreetsXmlBin.ToString().ToLower())")
$mdLines.Add("``````")
$mdLines.Add("")
$mdLines.Add("## worldmap.xml summary")
$mdLines.Add("")
$mdLines.Add("| Field | Candidate | Reference |")
$mdLines.Add("|---|---|---|")
$mdLines.Add("| exists | $($candWorldmapXml.exists) | $($refWorldmapXml.exists) |")
$mdLines.Add("| size_bytes | $($candWorldmapXml.size_bytes) | $($refWorldmapXml.size_bytes) |")
$mdLines.Add("| line_count | $($candWorldmapXml.line_count) | $($refWorldmapXml.line_count) |")
$mdLines.Add("| skeletal_or_substantial | $($candWorldmapXml.skeletal_or_substantial) | $($refWorldmapXml.skeletal_or_substantial) |")
$mdLines.Add("")
$mdLines.Add("## Lua file sizes")
$mdLines.Add("")
$mdLines.Add("| File | Candidate bytes | Reference bytes |")
$mdLines.Add("|---|---|---|")
$mdLines.Add("| objects.lua | $candObjectsSize | $refObjectsSize |")
$mdLines.Add("| spawnpoints.lua | $candSpawnptsSize | $refSpawnptsSize |")
$mdLines.Add("")
$mdLines.Add("## map.info field diff count: $($mapInfoDiffs.Count)")

$mdPath = Join-Path $Output 'build42-parent-map-metadata-contract.md'
Set-Content -Path $mdPath -Value ($mdLines -join "`n") -Encoding ASCII
Write-Output "Wrote: build42-parent-map-metadata-contract.md"

Write-Output ""
Write-Output "MAP-8K comparator complete."
Write-Output "map_info_field_differences: $($mapInfoDiffs.Count)"
Write-Output "candidate_has_lots_field:   $candHasLots"
Write-Output "reference_has_lots_field:   $refHasLots"
Write-Output "candidate_fixed2x:          $candFixed2x"
Write-Output "reference_fixed2x:          $refFixed2x"
Write-Output "binary_contents_read:       false"
Write-Output "third_party_files_copied:   false"
Write-Output "PUBLIC_PLAYABLE_CLAIM_ALLOWED=false"
Write-Output "Done."
