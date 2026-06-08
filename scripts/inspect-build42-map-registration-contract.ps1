#Requires -Version 5.1
<#
.SYNOPSIS
    MAP-7W: Compares the runtime map-registration contract between a candidate
    mod root and a known-working reference mod root.

    Reads only the explicit roots provided. Does NOT crawl arbitrary directories.
    Output under .local/ only.

    Writes:
      <Output>/map-registration-contract.json
      <Output>/map-registration-contract.md

.PARAMETER CandidateModRoot
    Path to the PZMapForge candidate mod root.

.PARAMETER ReferenceModRoot
    Path to the reference (Dru_map) mod root.

.PARAMETER CandidateMapId
    Map ID within the candidate root.

.PARAMETER ReferenceMapId
    Map ID within the reference root.

.PARAMETER Output
    Must be under .local/. Receives JSON and MD reports.

.PARAMETER CandidateLogsRoot
    Optional. If supplied, log files under this path are parsed.

.PARAMETER ReferenceLogsRoot
    Optional. If supplied, log files under this path are parsed.
#>

param(
    [Parameter(Mandatory=$true)][string]$CandidateModRoot,
    [Parameter(Mandatory=$true)][string]$ReferenceModRoot,
    [Parameter(Mandatory=$true)][string]$CandidateMapId,
    [Parameter(Mandatory=$true)][string]$ReferenceMapId,
    [Parameter(Mandatory=$true)][string]$Output,
    [string]$CandidateLogsRoot = '',
    [string]$ReferenceLogsRoot = ''
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

Write-Output "Map registration contract inspection"
Write-Output "Candidate: $CandidateModRoot"
Write-Output "Reference: $ReferenceModRoot"

# ---------------------------------------------------------------------------
# Helpers
# ---------------------------------------------------------------------------

function Test-HasBom {
    param([string]$Path)
    if (-not (Test-Path -LiteralPath $Path)) { return $false }
    $b = [System.IO.File]::ReadAllBytes($Path)
    return ($b.Length -ge 3 -and $b[0] -eq 0xEF -and $b[1] -eq 0xBB -and $b[2] -eq 0xBF)
}

function Parse-IniLike {
    param([string]$Path)
    $result = [ordered]@{}
    if (-not (Test-Path -LiteralPath $Path)) { return $result }
    $content = [System.IO.File]::ReadAllText($Path)
    foreach ($line in ($content -split '\r?\n')) {
        $line = $line.Trim()
        if ($line -match '^([^#=]+)=(.*)$') {
            $k = $Matches[1].Trim()
            $v = $Matches[2].Trim()
            $result[$k] = $v
        }
    }
    return $result
}

function Get-MapFolderDir {
    param([string]$ModRoot, [string]$MapId)
    $candidates = @(
        (Join-Path $ModRoot "common\media\maps\$MapId"),
        (Join-Path $ModRoot "media\maps\$MapId"),
        (Join-Path $ModRoot "42\media\maps\$MapId"),
        (Join-Path $ModRoot "mods\$MapId\media\maps\$MapId"),
        (Join-Path $ModRoot "Contents\mods\$MapId\media\maps\$MapId")
    )
    foreach ($d in $candidates) {
        if (Test-Path -LiteralPath $d) { return $d }
    }
    return ''
}

function Get-FileSetInfo {
    param([string]$MapDir, [string]$MapId)
    $result = [ordered]@{
        dir_found            = ($MapDir -ne '')
        files                = [string[]]@()
        subdirs              = [string[]]@()
        has_map_bin          = $false
        has_worldmap_xml     = $false
        has_worldmap_forest_xml = $false
        has_objects_lua      = $false
        has_spawnpoints_lua  = $false
        has_thumb_png        = $false
        has_lotheader_files  = $false
        has_lotpack_files    = $false
        has_chunkdata_files  = $false
        has_biomemap_folder  = $false
        has_biomemap_png     = $false
        lotheader_count      = 0
        lotpack_count        = 0
        chunkdata_count      = 0
        file_count           = 0
    }
    if ($MapDir -eq '' -or -not (Test-Path -LiteralPath $MapDir)) { return $result }

    $allItems   = @(Get-ChildItem -LiteralPath $MapDir -Recurse -ErrorAction SilentlyContinue)
    $topFiles   = @(Get-ChildItem -LiteralPath $MapDir -File -ErrorAction SilentlyContinue)
    $topDirs    = @(Get-ChildItem -LiteralPath $MapDir -Directory -ErrorAction SilentlyContinue)
    $lhFiles    = @(Get-ChildItem -LiteralPath $MapDir -Filter '*.lotheader'     -ErrorAction SilentlyContinue)
    $lpFiles    = @(Get-ChildItem -LiteralPath $MapDir -Filter '*.lotpack'       -ErrorAction SilentlyContinue)
    $cdFiles    = @(Get-ChildItem -LiteralPath $MapDir -Filter 'chunkdata_*.bin' -ErrorAction SilentlyContinue)

    $result.files           = [string[]]@($topFiles  | ForEach-Object { $_.Name })
    $result.subdirs         = [string[]]@($topDirs   | ForEach-Object { $_.Name })
    $result.file_count      = $topFiles.Count

    $result.has_map_bin          = (@($topFiles | Where-Object { $_.Name -eq 'map.bin' }).Count -gt 0)
    $result.has_worldmap_xml     = (Test-Path -LiteralPath (Join-Path $MapDir 'worldmap.xml'))
    $result.has_worldmap_forest_xml = (Test-Path -LiteralPath (Join-Path $MapDir 'worldmap-forest.xml'))
    $result.has_objects_lua      = (Test-Path -LiteralPath (Join-Path $MapDir 'objects.lua'))
    $result.has_spawnpoints_lua  = (Test-Path -LiteralPath (Join-Path $MapDir 'spawnpoints.lua'))
    $result.has_thumb_png        = (Test-Path -LiteralPath (Join-Path $MapDir 'thumb.png'))
    $result.has_lotheader_files  = ($lhFiles.Count -gt 0)
    $result.has_lotpack_files    = ($lpFiles.Count -gt 0)
    $result.has_chunkdata_files  = ($cdFiles.Count -gt 0)
    $result.lotheader_count      = $lhFiles.Count
    $result.lotpack_count        = $lpFiles.Count
    $result.chunkdata_count      = $cdFiles.Count

    $mapsSubdir   = Join-Path $MapDir 'maps'
    $biomemapPath = Join-Path $mapsSubdir 'biomemap_0_0.png'
    $result.has_biomemap_folder  = (Test-Path -LiteralPath $mapsSubdir)
    $result.has_biomemap_png     = (Test-Path -LiteralPath $biomemapPath)

    return $result
}

function Parse-Spawnpoints {
    param([string]$MapDir)
    $result = [ordered]@{
        present             = $false
        is_function_style   = $false
        is_return_style     = $false
        spawn_pairs         = [string[]]@()
        first_world_x       = $null
        first_world_y       = $null
    }
    $spPath = Join-Path $MapDir 'spawnpoints.lua'
    if (-not (Test-Path -LiteralPath $spPath)) { return $result }
    $result.present = $true
    $content = [System.IO.File]::ReadAllText($spPath)
    $result.is_function_style = ($content -match 'function\s+SpawnPoints')
    $result.is_return_style   = ($content -match '\breturn\b' -and -not $result.is_function_style)
    $pairList = [System.Collections.Generic.List[string]]::new()
    foreach ($line in ($content -split '\r?\n')) {
        if ($line -match 'worldX\s*=\s*(\d+)' -and $line -match 'worldY\s*=\s*(\d+)') {
            $wx = $null; $wy = $null; $px = $null; $py = $null
            if ($line -match 'worldX\s*=\s*(\d+)') { $wx = $Matches[1] }
            if ($line -match 'worldY\s*=\s*(\d+)') { $wy = $Matches[1] }
            if ($line -match 'posX\s*=\s*(\d+)')   { $px = $Matches[1] }
            if ($line -match 'posY\s*=\s*(\d+)')   { $py = $Matches[1] }
            $pairList.Add("worldX=$wx worldY=$wy posX=$px posY=$py")
        }
    }
    $result.spawn_pairs = [string[]]@($pairList.ToArray())
    if ($pairList.Count -gt 0) {
        $first = $pairList[0]
        if ($first -match 'worldX=(\d+)') { $result.first_world_x = $Matches[1] }
        if ($first -match 'worldY=(\d+)') { $result.first_world_y = $Matches[1] }
    }
    return $result
}

function Parse-LogEvidence {
    param([string]$LogsRoot, [string]$ModId)
    $result = [ordered]@{
        logs_root_provided     = ($LogsRoot -ne '')
        logs_root_exists       = $false
        log_files_found        = 0
        workshop_ready_seen    = $false
        mod_loaded_seen        = $false
        map_folder_scan_found  = $false
        map_folder_scan_empty  = $null
        map_folders_listed     = [string[]]@()
        lotheader_evidence     = $false
        sanity_check_fail      = $false
        eof_exception          = $false
        isometagrid_begin      = $false
        isometagrid_finish     = $false
        relevant_lines         = [string[]]@()
    }
    if ($LogsRoot -eq '') { return $result }
    $result.logs_root_exists = (Test-Path -LiteralPath $LogsRoot)
    if (-not $result.logs_root_exists) { return $result }

    $logFiles = @(Get-ChildItem -LiteralPath $LogsRoot -Filter '*.txt' -Recurse -ErrorAction SilentlyContinue)
    $result.log_files_found = $logFiles.Count
    if ($logFiles.Count -eq 0) { return $result }

    $relevant = [System.Collections.Generic.List[string]]::new()
    $folders  = [System.Collections.Generic.List[string]]::new()
    $inFolderList = $false
    $scanEndIdx   = -1

    foreach ($lf in $logFiles) {
        $content = [System.IO.File]::ReadAllText($lf.FullName)
        if ($content -match 'Workshop.*Ready|Ready.*Workshop') { $result.workshop_ready_seen = $true }
        if ($content -match ('loading\s+' + [regex]::Escape($ModId))) { $result.mod_loaded_seen = $true }
        if ($content -match '\.lotheader') { $result.lotheader_evidence = $true }
        if ($content -match 'SANITY CHECK FAIL') { $result.sanity_check_fail = $true }
        if ($content -match 'EOFException|java\.io\.EOF') { $result.eof_exception = $true }
        if ($content -match 'IsoMetaGrid.*Create|IsoMetaGrid.*begin') { $result.isometagrid_begin = $true }
        if ($content -match 'IsoMetaGrid.*finish') { $result.isometagrid_finish = $true }

        $lines = $content -split '\r?\n'
        for ($i = 0; $i -lt $lines.Count; $i++) {
            $raw = $lines[$i]
            $sem = ($raw -replace '^\[.*?\]\s+\w+\s*:\s+\w+\s+f:\d+[^>]*>\s*', '').TrimEnd('.')
            if ($sem -match 'Looking in these map folders') {
                $result.map_folder_scan_found = $true
                $inFolderList = $true
                $relevant.Add("[map-folder-scan-start] $sem")
                continue
            }
            if ($inFolderList -and $sem -match '^<End of map-folders list>') {
                $inFolderList = $false
                $result.map_folder_scan_empty = ($folders.Count -eq 0)
                $relevant.Add("[map-folder-scan-end] $sem")
                continue
            }
            if ($inFolderList) {
                $s = $sem.Trim()
                if ($s.Length -gt 0) { $folders.Add($s); $relevant.Add("[map-folder] $s") }
            }
            if ($sem -match 'SANITY CHECK FAIL|EOFException|IsoMetaGrid|lotheader|loadCell|initSpawnBuildings|no room or building') {
                $relevant.Add($sem)
            }
        }
    }

    $result.map_folders_listed = [string[]]@($folders.ToArray())
    $result.relevant_lines     = [string[]]@($relevant | Select-Object -First 100)
    return $result
}

# ---------------------------------------------------------------------------
# Run inspections
# ---------------------------------------------------------------------------

$candExists = Test-Path -LiteralPath $CandidateModRoot
$refExists  = Test-Path -LiteralPath $ReferenceModRoot
Write-Output "Candidate root exists: $candExists"
Write-Output "Reference root exists: $refExists"

$candMapDir = if ($candExists) { Get-MapFolderDir -ModRoot $CandidateModRoot -MapId $CandidateMapId } else { '' }
$refMapDir  = if ($refExists)  { Get-MapFolderDir -ModRoot $ReferenceModRoot -MapId $ReferenceMapId  } else { '' }
Write-Output "Candidate map folder: $(if ($candMapDir) { $candMapDir } else { '(not found)' })"
Write-Output "Reference map folder: $(if ($refMapDir)  { $refMapDir  } else { '(not found)' })"

# mod.info parsing
$candRootModInfo = if ($candExists) { Parse-IniLike (Join-Path $CandidateModRoot 'mod.info') } else { [ordered]@{} }
$ref42ModInfo    = if ($refExists)  { Parse-IniLike (Join-Path $ReferenceModRoot '42\mod.info') } else { [ordered]@{} }
$cand42ModInfo   = if ($candExists) { Parse-IniLike (Join-Path $CandidateModRoot '42\mod.info') } else { [ordered]@{} }
$refRootModInfo  = if ($refExists)  { Parse-IniLike (Join-Path $ReferenceModRoot 'mod.info') } else { [ordered]@{} }

# map.info parsing
$candMapInfo = if ($candMapDir -ne '') { Parse-IniLike (Join-Path $candMapDir 'map.info') } else { [ordered]@{} }
$refMapInfo  = if ($refMapDir -ne '')  { Parse-IniLike (Join-Path $refMapDir  'map.info') } else { [ordered]@{} }

# Compute map.info value differences
$mapInfoDiffs  = [System.Collections.Generic.List[string]]::new()
$mapInfoRefNotCand = [System.Collections.Generic.List[string]]::new()
$mapInfoCandNotRef = [System.Collections.Generic.List[string]]::new()
foreach ($k in $refMapInfo.Keys) {
    if (-not $candMapInfo.Contains($k)) { $mapInfoRefNotCand.Add($k) }
    elseif ($candMapInfo[$k] -ne $refMapInfo[$k]) { $mapInfoDiffs.Add("$k : ref='$($refMapInfo[$k])' cand='$($candMapInfo[$k])'") }
}
foreach ($k in $candMapInfo.Keys) { if (-not $refMapInfo.Contains($k)) { $mapInfoCandNotRef.Add($k) } }

# Compute mod.info value differences (root mod.info)
$modInfoDiffs = [System.Collections.Generic.List[string]]::new()
foreach ($k in $refRootModInfo.Keys) {
    if ($candRootModInfo.Contains($k) -and $candRootModInfo[$k] -ne $refRootModInfo[$k]) {
        $modInfoDiffs.Add("$k : ref='$($refRootModInfo[$k])' cand='$($candRootModInfo[$k])'")
    }
}

# File set info
$candFileSet = Get-FileSetInfo -MapDir $candMapDir -MapId $CandidateMapId
$refFileSet  = Get-FileSetInfo -MapDir $refMapDir  -MapId $ReferenceMapId

# File set comparison
$refFilesNotInCand  = [System.Collections.Generic.List[string]]::new()
$candFilesNotInRef  = [System.Collections.Generic.List[string]]::new()
$candFileNames = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::OrdinalIgnoreCase)
$refFileNames  = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::OrdinalIgnoreCase)
foreach ($f in $candFileSet.files) { [void]$candFileNames.Add($f) }
foreach ($f in $refFileSet.files)  { [void]$refFileNames.Add($f) }
foreach ($f in $refFileSet.files)  { if (-not $candFileNames.Contains($f)) { $refFilesNotInCand.Add($f) } }
foreach ($f in $candFileSet.files) { if (-not $refFileNames.Contains($f))  { $candFilesNotInRef.Add($f) } }
$exactFileSetMatch = ($refFilesNotInCand.Count -eq 0 -and $candFilesNotInRef.Count -eq 0)

# map.bin discriminator
$refHasMapBin   = [bool]$refFileSet.has_map_bin
$candHasMapBin  = [bool]$candFileSet.has_map_bin
$mapBinDiscriminator = ($refHasMapBin -and -not $candHasMapBin)

# Spawnpoints
$candSpawn = if ($candMapDir -ne '') { Parse-Spawnpoints -MapDir $candMapDir } else { [ordered]@{ present=$false } }
$refSpawn  = if ($refMapDir -ne '')  { Parse-Spawnpoints -MapDir $refMapDir  } else { [ordered]@{ present=$false } }

# BOM checks
$bomCheck = [ordered]@{}
foreach ($label in @('cand_root_mod_info','ref_root_mod_info','cand_42_mod_info','ref_42_mod_info')) {
    $p = switch ($label) {
        'cand_root_mod_info' { if ($candExists) { Join-Path $CandidateModRoot 'mod.info' } else { '' } }
        'ref_root_mod_info'  { if ($refExists)  { Join-Path $ReferenceModRoot 'mod.info' } else { '' } }
        'cand_42_mod_info'   { if ($candExists) { Join-Path $CandidateModRoot '42\mod.info' } else { '' } }
        'ref_42_mod_info'    { if ($refExists)  { Join-Path $ReferenceModRoot '42\mod.info' } else { '' } }
    }
    $bomCheck[$label] = if ($p -ne '' -and (Test-Path -LiteralPath $p)) { Test-HasBom $p } else { $null }
}
foreach ($label in @('cand_map_info','ref_map_info','cand_sp','ref_sp')) {
    $p = switch ($label) {
        'cand_map_info' { if ($candMapDir -ne '') { Join-Path $candMapDir 'map.info' } else { '' } }
        'ref_map_info'  { if ($refMapDir -ne '')  { Join-Path $refMapDir  'map.info' } else { '' } }
        'cand_sp'       { if ($candMapDir -ne '') { Join-Path $candMapDir 'spawnpoints.lua' } else { '' } }
        'ref_sp'        { if ($refMapDir -ne '')  { Join-Path $refMapDir  'spawnpoints.lua' } else { '' } }
    }
    $bomCheck[$label] = if ($p -ne '' -and (Test-Path -LiteralPath $p)) { Test-HasBom $p } else { $null }
}

# Log evidence
Write-Output "Parsing candidate logs: $(if ($CandidateLogsRoot) { $CandidateLogsRoot } else { '(not supplied)' })"
Write-Output "Parsing reference logs:  $(if ($ReferenceLogsRoot) { $ReferenceLogsRoot } else { '(not supplied)' })"
$candLog = Parse-LogEvidence -LogsRoot $CandidateLogsRoot -ModId $CandidateMapId
$refLog  = Parse-LogEvidence -LogsRoot $ReferenceLogsRoot -ModId $ReferenceMapId

# Runtime mount discriminator found?
$runtimeMountDiscriminatorFound = (
    $mapBinDiscriminator -or
    ($refFilesNotInCand.Count -gt 0) -or
    ($mapInfoDiffs.Count -gt 0) -or
    ($modInfoDiffs.Count -gt 0)
)

# ---------------------------------------------------------------------------
# Build report
# ---------------------------------------------------------------------------

$report = [ordered]@{
    schema                                   = 'pzmapforge.map-registration-contract.v0.1'
    candidate_mod_root                       = $CandidateModRoot
    reference_mod_root                       = $ReferenceModRoot
    candidate_map_id                         = $CandidateMapId
    reference_map_id                         = $ReferenceMapId
    candidate_root_exists                    = $candExists
    reference_root_exists                    = $refExists
    layout_match_known_from_map7u            = $true
    exact_file_set_match                     = $exactFileSetMatch
    reference_files_missing_in_candidate_count = $refFilesNotInCand.Count
    candidate_files_missing_in_reference_count = $candFilesNotInRef.Count
    reference_files_not_in_candidate         = [string[]]@($refFilesNotInCand.ToArray())
    candidate_files_not_in_reference         = [string[]]@($candFilesNotInRef.ToArray())
    reference_has_map_bin                    = $refHasMapBin
    candidate_has_map_bin                    = $candHasMapBin
    map_bin_discriminator                    = $mapBinDiscriminator
    map_info_value_differences_count         = $mapInfoDiffs.Count
    map_info_value_differences               = [string[]]@($mapInfoDiffs.ToArray())
    map_info_fields_in_ref_not_cand          = [string[]]@($mapInfoRefNotCand.ToArray())
    map_info_fields_in_cand_not_ref          = [string[]]@($mapInfoCandNotRef.ToArray())
    mod_info_value_differences_count         = $modInfoDiffs.Count
    mod_info_value_differences               = [string[]]@($modInfoDiffs.ToArray())
    candidate_map_folder                     = $candFileSet
    reference_map_folder                     = $refFileSet
    candidate_spawnpoints                    = $candSpawn
    reference_spawnpoints                    = $refSpawn
    candidate_map_info_keys                  = [string[]]@($candMapInfo.Keys)
    reference_map_info_keys                  = [string[]]@($refMapInfo.Keys)
    bom_checks                               = $bomCheck
    candidate_log                            = $candLog
    reference_log                            = $refLog
    candidate_log_map_folder_scan_empty      = $candLog.map_folder_scan_empty
    reference_log_map_folder_scan_empty      = $refLog.map_folder_scan_empty
    runtime_mount_discriminator_found        = $runtimeMountDiscriminatorFound
    candidate_map_folder_has_binary_files    = [bool]$candFileSet.has_lotheader_files
    reference_map_folder_has_binary_files    = [bool]$refFileSet.has_lotheader_files
    binary_writer_gate_closed                = $true
    binary_format_investigation_paused       = $true
    public_playable_claim_allowed            = $false
    load_test_performed_by_script            = $false
    automatic_workshop_upload_performed      = $false
    binary_writer_changed                    = $false
}

$jsonPath = Join-Path $Output 'map-registration-contract.json'
$mdPath   = Join-Path $Output 'map-registration-contract.md'

$report | ConvertTo-Json -Depth 7 | Set-Content -Path $jsonPath -Encoding UTF8
Write-Output "JSON: $jsonPath"

# ---------------------------------------------------------------------------
# Markdown
# ---------------------------------------------------------------------------

$fence = '```'
function Yn { param([object]$v) if ($null -eq $v) { return 'N/A' } if ([bool]$v) { return 'YES' } else { return 'no' } }

$md = @"
# Map Registration Contract

## Roots

| | Path |
|---|---|
| Candidate | $CandidateModRoot |
| Reference | $ReferenceModRoot |

## File set match

| Field | Value |
|---|---|
| exact_file_set_match | $exactFileSetMatch |
| ref files missing in candidate | $($refFilesNotInCand.Count) |
| cand files missing in reference | $($candFilesNotInRef.Count) |
| reference has map.bin | $(Yn $refHasMapBin) |
| candidate has map.bin | $(Yn $candHasMapBin) |
| map.bin discriminator | $mapBinDiscriminator |

## map.info value differences: $($mapInfoDiffs.Count)

$(if ($mapInfoDiffs.Count -eq 0) { '(none)' } else { ($mapInfoDiffs | ForEach-Object { "- $_ " }) -join "`n" })

## mod.info value differences: $($modInfoDiffs.Count)

$(if ($modInfoDiffs.Count -eq 0) { '(none)' } else { ($modInfoDiffs | ForEach-Object { "- $_ " }) -join "`n" })

## Non-claims

${fence}text
public_playable_claim_allowed=false
binary_writer_gate_closed=true
binary_format_investigation_paused=true
${fence}
"@

Set-Content -Path $mdPath -Value $md -Encoding ASCII
Write-Output "MD:   $mdPath"
Write-Output ""
Write-Output "exact_file_set_match:     $exactFileSetMatch"
Write-Output "map_bin_discriminator:    $mapBinDiscriminator"
Write-Output "ref_files_missing_in_cand: $($refFilesNotInCand.Count)"
Write-Output "map_info_diffs:           $($mapInfoDiffs.Count)"
Write-Output "runtime_mount_discriminator_found: $runtimeMountDiscriminatorFound"
Write-Output "Done."
