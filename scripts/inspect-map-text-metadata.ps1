#Requires -Version 5.1
<#
.SYNOPSIS
    Local-only map text metadata evidence reader (MAP-4C).

    Reads safe text files (mod.info, map.info, spawnpoints.lua, objects.lua)
    from an operator-provided map/mod path and writes a local-only evidence
    JSON and Markdown report.

    Does NOT read .lotheader, .lotpack, .bin, .png, or any binary file.
    Does NOT copy input files.
    Does NOT touch repo media/maps.
    Does NOT claim playable export.
    Does NOT implement any compiled writer.
    Output must be under .local only.

Usage:
    .\scripts\inspect-map-text-metadata.ps1 `
        -Path   <local path to mod/map root> `
        -Output <output directory (must be under .local)>

Example:
    .\scripts\inspect-map-text-metadata.ps1 `
        -Path   "C:\path\to\mod-root" `
        -Output ".local\evidence\mod-text-01"
#>

param(
    [Parameter(Mandatory=$true)]
    [string]$Path,

    [Parameter(Mandatory=$true)]
    [string]$Output
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

Write-Output 'inspect-map-text-metadata.ps1'
Write-Output "Path:   $Path"
Write-Output "Output: $Output"
Write-Output ''

# ---------------------------------------------------------------------------
# Guard: output must be under .local
# ---------------------------------------------------------------------------

$outputFull  = [System.IO.Path]::GetFullPath($Output)
$sep         = [System.IO.Path]::DirectorySeparatorChar
$localMarker = $sep + '.local' + $sep
$endsLocal   = $outputFull.EndsWith($sep + '.local')

if (-not ($outputFull.Contains($localMarker) -or $endsLocal)) {
    Write-Error "inspect-map-text-metadata: refusing to write outside a .local/ directory: $outputFull"
    Write-Error "  Pass -Output to an explicit .local/ path."
    exit 1
}

# ---------------------------------------------------------------------------
# Guard: input path must exist
# ---------------------------------------------------------------------------

if (-not (Test-Path -LiteralPath $Path)) {
    Write-Error "Input path not found: $Path"
    exit 1
}

$inputFull = [System.IO.Path]::GetFullPath($Path)

# ---------------------------------------------------------------------------
# SHA-256 helper (reads bytes only for hashing)
# ---------------------------------------------------------------------------

function Get-FileSha256 {
    param([string]$FilePath)
    $sha    = [System.Security.Cryptography.SHA256]::Create()
    $stream = [System.IO.File]::OpenRead($FilePath)
    try {
        $hash = $sha.ComputeHash($stream)
        return (($hash | ForEach-Object { $_.ToString('x2') }) -join '')
    }
    finally { $stream.Dispose(); $sha.Dispose() }
}

# ---------------------------------------------------------------------------
# Key=value parser for .info files
# ---------------------------------------------------------------------------

function Parse-InfoKeyValues {
    param([string[]]$Lines)
    $kv = [ordered]@{}
    foreach ($line in $Lines) {
        $t  = $line.Trim()
        if ($t -eq '' -or $t.StartsWith('#')) { continue }
        $eq = $t.IndexOf('=')
        if ($eq -gt 0) {
            $key = $t.Substring(0, $eq).Trim()
            $val = $t.Substring($eq + 1).Trim()
            if ($key -ne '') { $kv[$key] = $val }
        }
    }
    return $kv
}

# ---------------------------------------------------------------------------
# Detect map folders under media/maps/
# ---------------------------------------------------------------------------

Write-Output '--- Detecting map folders ---'

$mediaMapsPath = Join-Path $inputFull 'media\maps'
$mapFolders    = @()

if (Test-Path -LiteralPath $mediaMapsPath) {
    $mapFolders = @(Get-ChildItem -LiteralPath $mediaMapsPath -Directory -ErrorAction SilentlyContinue)
}

$detectedMapFolderNames = @($mapFolders | ForEach-Object { $_.Name })

if ($detectedMapFolderNames.Count -gt 0) {
    $detectedMapFolderNames | ForEach-Object { Write-Output "  Map folder: $_" }
} else {
    Write-Output '  No media/maps/<folder> detected.'
}

Write-Output ''

# ---------------------------------------------------------------------------
# Collect candidate text files by name only — no wildcard binary scan
# ---------------------------------------------------------------------------

$textFileCandidates = [System.Collections.Generic.List[object]]::new()

$modInfoFull = Join-Path $inputFull 'mod.info'
if (Test-Path -LiteralPath $modInfoFull) {
    $textFileCandidates.Add([ordered]@{
        RelPath  = 'mod.info'
        FullPath = $modInfoFull
        Role     = 'mod_info'
        MapId    = $null
    })
}

foreach ($mf in $mapFolders) {
    foreach ($name in @('map.info', 'spawnpoints.lua', 'objects.lua')) {
        $fp = Join-Path $mf.FullName $name
        if (Test-Path -LiteralPath $fp) {
            $textFileCandidates.Add([ordered]@{
                RelPath  = ('media/maps/' + $mf.Name + '/' + $name)
                FullPath = $fp
                Role     = ($name -replace '\.', '_')
                MapId    = $mf.Name
            })
        }
    }
}

# ---------------------------------------------------------------------------
# Read and summarize each text file
# ---------------------------------------------------------------------------

Write-Output '--- Reading text files ---'

$fileRecords     = [System.Collections.Generic.List[object]]::new()
$mapInfoKvs      = [ordered]@{}
$spawnSummaries  = [ordered]@{}
$objectSummaries = [ordered]@{}

$generatedAt = (Get-Date).ToUniversalTime().ToString('yyyy-MM-ddTHH:mm:ssZ')

foreach ($candidate in $textFileCandidates) {
    $fp  = $candidate.FullPath
    $rel = $candidate.RelPath
    $ext = [System.IO.Path]::GetExtension($fp).ToLowerInvariant()

    $lines    = @(Get-Content -LiteralPath $fp -Encoding UTF8 -ErrorAction SilentlyContinue)
    if ($null -eq $lines) { $lines = @() }

    $nonEmpty = @($lines | Where-Object { $_.Trim() -ne '' } | ForEach-Object { [string]$_ } | Select-Object -First 20)
    $sha      = Get-FileSha256 $fp
    $size     = (Get-Item -LiteralPath $fp).Length

    $fileRecords.Add([ordered]@{
        relative_path         = $rel
        extension             = $ext
        size_bytes            = $size
        sha256                = $sha
        line_count            = $lines.Count
        first_non_empty_lines = $nonEmpty
    })

    Write-Output ("  {0,-52} {1,8} bytes  {2} lines" -f $rel, $size, $lines.Count)

    # key=value for .info files
    if ($ext -eq '.info') {
        $kv = Parse-InfoKeyValues $lines
        $mapInfoKvs[$rel] = $kv
    }

    # Lua analysis
    if ($ext -eq '.lua' -and $candidate.MapId) {
        $raw     = $lines -join "`n"
        $numHits = [regex]::Matches($raw, '\b\d+\.?\d*\b')

        if ($candidate.Role -eq 'spawnpoints_lua') {
            $spawnSummaries[$candidate.MapId] = [ordered]@{
                file_present                    = $true
                line_count                      = $lines.Count
                contains_return_statement       = ($raw -imatch '\breturn\b')
                contains_profession_name_tokens = ($raw -imatch 'profession')
                coordinate_number_count         = $numHits.Count
            }
        }

        if ($candidate.Role -eq 'objects_lua') {
            $objectSummaries[$candidate.MapId] = [ordered]@{
                file_present            = $true
                line_count              = $lines.Count
                coordinate_number_count = $numHits.Count
            }
        }
    }
}

# Fill absent summaries for map folders whose Lua files were not found
foreach ($mf in $mapFolders) {
    if (-not $spawnSummaries.Contains($mf.Name)) {
        $spawnSummaries[$mf.Name] = [ordered]@{
            file_present                    = $false
            line_count                      = 0
            contains_return_statement       = $false
            contains_profession_name_tokens = $false
            coordinate_number_count         = 0
        }
    }
    if (-not $objectSummaries.Contains($mf.Name)) {
        $objectSummaries[$mf.Name] = [ordered]@{
            file_present            = $false
            line_count              = 0
            coordinate_number_count = 0
        }
    }
}

Write-Output ''

# ---------------------------------------------------------------------------
# Write JSON output
# ---------------------------------------------------------------------------

New-Item -ItemType Directory -Force -Path $outputFull | Out-Null

$evidence = [ordered]@{
    schema                      = 'pzmapforge.map-text-metadata-evidence.v0.1'
    claim_boundary              = 'evidence_inventory_only_not_compiled_not_pz_load_tested'
    generated_at_utc            = $generatedAt
    input_path                  = ($inputFull -replace '\\', '/')
    copied_input_files          = $false
    pz_assets_copied            = $false
    media_maps_touched          = $false
    playable_export_claimed     = $false
    compiled_writer_implemented = $false
    binary_files_read           = $false
    text_files_read             = $true
    detected_map_folders        = $detectedMapFolderNames
    files                       = $fileRecords.ToArray()
    map_info_key_values         = $mapInfoKvs
    spawnpoints_summary         = $spawnSummaries
    objects_summary             = $objectSummaries
}

$jsonPath = Join-Path $outputFull 'map-text-metadata-evidence.json'
$evidence | ConvertTo-Json -Depth 6 | Set-Content -Path $jsonPath -Encoding UTF8

# ---------------------------------------------------------------------------
# Write Markdown output
# ---------------------------------------------------------------------------

$mapFolderList = if ($detectedMapFolderNames.Count -gt 0) {
    ($detectedMapFolderNames | ForEach-Object { "- ``$_``" }) -join "`n"
} else {
    '- (none detected)'
}

$filesReadRows = ($fileRecords | ForEach-Object {
    "| ``$($_.relative_path)`` | ``$($_.extension)`` | $($_.size_bytes) | $($_.line_count) |"
}) -join "`n"

$mapInfoSection = ''
foreach ($key in $mapInfoKvs.Keys) {
    $mapInfoSection += "`n### $key`n`n"
    foreach ($k in $mapInfoKvs[$key].Keys) {
        $mapInfoSection += "- ``$k`` = $($mapInfoKvs[$key][$k])`n"
    }
}
if ($mapInfoSection -eq '') { $mapInfoSection = "`n(no .info key=value files found)`n" }

$spawnSection = ''
foreach ($mapId in $spawnSummaries.Keys) {
    $s = $spawnSummaries[$mapId]
    $spawnSection += "`n### $mapId`n`n| Field | Value |`n|---|---|`n"
    $spawnSection += "| file_present | $($s.file_present) |`n"
    $spawnSection += "| line_count | $($s.line_count) |`n"
    $spawnSection += "| contains_return_statement | $($s.contains_return_statement) |`n"
    $spawnSection += "| contains_profession_name_tokens | $($s.contains_profession_name_tokens) |`n"
    $spawnSection += "| coordinate_number_count | $($s.coordinate_number_count) |`n"
}
if ($spawnSection -eq '') { $spawnSection = "`n(no map folders detected)`n" }

$objectsSection = ''
foreach ($mapId in $objectSummaries.Keys) {
    $o = $objectSummaries[$mapId]
    $objectsSection += "`n### $mapId`n`n| Field | Value |`n|---|---|`n"
    $objectsSection += "| file_present | $($o.file_present) |`n"
    $objectsSection += "| line_count | $($o.line_count) |`n"
    $objectsSection += "| coordinate_number_count | $($o.coordinate_number_count) |`n"
}
if ($objectsSection -eq '') { $objectsSection = "`n(no map folders detected)`n" }

$md = @"
# Map Text Metadata Evidence

Schema:         pzmapforge.map-text-metadata-evidence.v0.1
Claim boundary: evidence_inventory_only_not_compiled_not_pz_load_tested
Generated:      $generatedAt
Input path:     $($inputFull -replace '\\', '/')

## Detected map folders

$mapFolderList

## Files read

| Relative path | Extension | Bytes | Lines |
|---|---|---:|---:|
$filesReadRows

## map.info / mod.info key values
$mapInfoSection
## spawnpoints.lua summary
$spawnSection
## objects.lua summary
$objectsSection
## Non-claims

- No binary files read (.lotheader, .lotpack, .bin, .png).
- No compiled writer implemented.
- No PZ assets copied or committed.
- No playable export claimed.
- No repo media/maps writes.
- Output is under .local only.
"@

$mdPath = Join-Path $outputFull 'map-text-metadata-evidence.md'
Set-Content -Path $mdPath -Value $md -Encoding UTF8

# ---------------------------------------------------------------------------
# Summary
# ---------------------------------------------------------------------------

Write-Output "Evidence JSON:               $jsonPath"
Write-Output "Evidence MD:                 $mdPath"
Write-Output "Text files read:             $($fileRecords.Count)"
Write-Output "Map folders detected:        $($detectedMapFolderNames.Count)"
Write-Output 'binary_files_read:           false'
Write-Output 'copied_input_files:          false'
Write-Output 'pz_assets_copied:            false'
Write-Output 'media_maps_touched:          false'
Write-Output 'playable_export_claimed:     false'
Write-Output 'compiled_writer_implemented: false'
Write-Output 'Status:                      OK'
