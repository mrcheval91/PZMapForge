[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$CandidateParentRoot,
    [Parameter(Mandatory)][string]$ReferenceParentRoot,
    [Parameter(Mandatory)][string]$Output,
    [string]$CandidateParentMapId = 'PZMapForge',
    [string]$ReferenceParentMapId = 'Project_Russia'
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

if (-not $Output.Contains('.local')) {
    Write-Error "-Output must be a path under .local/ (got: $Output)"
    exit 1
}

$outDir = $Output
if (-not (Test-Path $outDir)) {
    New-Item -ItemType Directory -Force -Path $outDir | Out-Null
}

function Get-FileInfo([string]$root, [string]$name) {
    $p = Join-Path $root $name
    if (Test-Path $p) {
        $info = Get-Item $p
        return [ordered]@{ present = $true; size_bytes = $info.Length }
    }
    return [ordered]@{ present = $false; size_bytes = 0 }
}

function Get-FileCount([string]$root, [string]$pattern) {
    if (-not (Test-Path $root)) { return 0 }
    return @(Get-ChildItem -Path $root -Filter $pattern -File -Recurse -ErrorAction SilentlyContinue).Count
}

$schema = 'pzmapforge.map8m-worldmap-bin-presence.v0.1'

$cand = [ordered]@{}
$ref  = [ordered]@{}

foreach ($fname in @('worldmap.xml','worldmap.xml.bin','worldmap-forest.xml','worldmap-forest.xml.bin','streets.xml.bin','objects.lua','spawnpoints.lua')) {
    $cand[$fname -replace '[.\-]','_'] = Get-FileInfo $CandidateParentRoot $fname
    $ref[$fname -replace '[.\-]','_']  = Get-FileInfo $ReferenceParentRoot $fname
}

$cand['lotheader_count'] = Get-FileCount $CandidateParentRoot '*.lotheader'
$cand['lotpack_count']   = Get-FileCount $CandidateParentRoot '*.lotpack'
$cand['chunkdata_count'] = Get-FileCount $CandidateParentRoot 'chunkdata_*.bin'

$ref['lotheader_count']  = Get-FileCount $ReferenceParentRoot '*.lotheader'
$ref['lotpack_count']    = Get-FileCount $ReferenceParentRoot '*.lotpack'
$ref['chunkdata_count']  = Get-FileCount $ReferenceParentRoot 'chunkdata_*.bin'

$result = [ordered]@{
    schema                              = $schema
    candidate_parent_map_id             = $CandidateParentMapId
    reference_parent_map_id             = $ReferenceParentMapId
    candidate                           = $cand
    reference                           = $ref
    candidate_worldmap_xml_present      = $cand['worldmap_xml']['present']
    candidate_worldmap_xml_bin_present  = $cand['worldmap_xml_bin']['present']
    reference_worldmap_xml_present      = $ref['worldmap_xml']['present']
    reference_worldmap_xml_bin_present  = $ref['worldmap_xml_bin']['present']
    binary_contents_read                = $false
    no_project_russia_files_copied      = $true
    playable_claim_allowed              = $false
    binary_writer_gate_closed           = $true
}

$jsonPath = Join-Path $outDir 'worldmap-bin-presence.json'
$mdPath   = Join-Path $outDir 'worldmap-bin-presence.md'

$result | ConvertTo-Json -Depth 6 | Set-Content -Encoding UTF8 -Path $jsonPath

$mdLines = @(
    '# MAP-8M: Worldmap Bin Presence Inspection'
    ''
    "Schema: ``$schema``"
    ''
    '| Key | Value |'
    '|-----|-------|'
    "| candidate_parent_map_id | $CandidateParentMapId |"
    "| reference_parent_map_id | $ReferenceParentMapId |"
    "| candidate_worldmap_xml_present | $($result.candidate_worldmap_xml_present) |"
    "| candidate_worldmap_xml_bin_present | $($result.candidate_worldmap_xml_bin_present) |"
    "| reference_worldmap_xml_present | $($result.reference_worldmap_xml_present) |"
    "| reference_worldmap_xml_bin_present | $($result.reference_worldmap_xml_bin_present) |"
    "| binary_contents_read | false |"
    "| no_project_russia_files_copied | true |"
    "| playable_claim_allowed | false |"
    "| binary_writer_gate_closed | true |"
)
$mdLines | Set-Content -Encoding UTF8 -Path $mdPath

Write-Host "worldmap-bin-presence.json -> $jsonPath"
Write-Host "worldmap-bin-presence.md   -> $mdPath"
exit 0
