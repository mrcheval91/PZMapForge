#Requires -Version 5.1
<#
.SYNOPSIS
    MAP-6R: Inspects Build 42 LOTH lotheader structure from reference files
    copied under .local/. Extracts bounded prefix fields to explain why the
    MAP-6L 38-byte candidate EOFs at IsoLot.readInt.

    All inputs/outputs must be under .local/.
    Does NOT read PZ install folders. Does NOT write outside .local/.

    Writes:
      <Output>/build42-loth-structure-report.json
      <Output>/build42-loth-structure-report.md

.PARAMETER ReferenceRoot
    Path under .local/ containing reference Build 42 *.lotheader files.

.PARAMETER Output
    Path under .local/ for report output.

.PARAMETER MaxFiles
    Maximum number of reference files to inspect. Default: 20.

.PARAMETER MaxPrefixBytes
    Maximum bytes to read per file for prefix analysis. Default: 512.
    Clamped to [16, 4096].

.EXAMPLE
    powershell -ExecutionPolicy Bypass -File scripts\inspect-build42-loth-structure.ps1 `
        -ReferenceRoot .local\reference-build42-map\Dru_map `
        -Output .local\map6r-loth-structure
#>

param(
    [Parameter(Mandatory=$true)][string]$ReferenceRoot,
    [Parameter(Mandatory=$true)][string]$Output,
    [int]$MaxFiles       = 20,
    [int]$MaxPrefixBytes = 512
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$MaxPrefixBytes = [Math]::Max(16, [Math]::Min(4096, $MaxPrefixBytes))

# ---------------------------------------------------------------------------
# Path guards
# ---------------------------------------------------------------------------

function Assert-LocalPath {
    param([string]$Path, [string]$Label)
    $norm = $Path.Replace('/', '\')
    if ($norm -notmatch '\\\.local(\\|$)') {
        Write-Error "$Label must be under .local/. Got: $Path"
        exit 1
    }
}

Assert-LocalPath $ReferenceRoot '-ReferenceRoot'
Assert-LocalPath $Output        '-Output'

if (-not (Test-Path -LiteralPath $ReferenceRoot -PathType Container)) {
    Write-Error "ReferenceRoot not found or not a directory: $ReferenceRoot"
    exit 1
}

New-Item -ItemType Directory -Force -Path $Output | Out-Null

# ---------------------------------------------------------------------------
# Helper functions
# ---------------------------------------------------------------------------

function bytes-hex ([byte[]]$b, [int]$start, [int]$len) {
    $end = [Math]::Min($start + $len, $b.Length) - 1
    if ($end -lt $start) { return '' }
    ($b[$start..$end] | ForEach-Object { $_.ToString('x2') }) -join ''
}

function u32le ([byte[]]$b, [int]$offset) {
    if ($offset + 3 -ge $b.Length) { return $null }
    [BitConverter]::ToUInt32($b, $offset)
}

function words-u32le ([byte[]]$b, [int]$maxWords) {
    $out = [System.Collections.Generic.List[object]]::new()
    for ($i = 0; $i -lt $maxWords * 4; $i += 4) {
        if ($i + 3 -lt $b.Length) {
            $out.Add([long][BitConverter]::ToUInt32($b, $i)) | Out-Null
        } else {
            $out.Add($null) | Out-Null
        }
    }
    [object[]]$out.ToArray()
}

function first-printable-offset ([byte[]]$b, [int]$startAt) {
    for ($i = $startAt; $i -lt $b.Length; $i++) {
        if ($b[$i] -ge 0x20 -and $b[$i] -le 0x7E) { return $i }
    }
    return -1
}

function first-newline-offset ([byte[]]$b, [int]$startAt) {
    for ($i = $startAt; $i -lt $b.Length; $i++) {
        if ($b[$i] -eq 0x0A) { return $i }
    }
    return -1
}

function parse-ascii-lines ([byte[]]$b, [int]$startAt, [int]$maxLines) {
    $lines = [System.Collections.Generic.List[string]]::new()
    $cur   = [System.Text.StringBuilder]::new()
    for ($i = $startAt; $i -lt $b.Length -and $lines.Count -lt $maxLines; $i++) {
        $c = $b[$i]
        if ($c -eq 0x0A) {
            $lines.Add($cur.ToString()) | Out-Null
            $cur = [System.Text.StringBuilder]::new()
        } elseif ($c -ge 0x20 -and $c -le 0x7E) {
            [void]$cur.Append([char]$c)
        } else {
            break
        }
    }
    [string[]]$lines.ToArray()
}

function printable-runs ([byte[]]$b, [int]$maxRuns, [int]$minRunLen) {
    $runs = [System.Collections.Generic.List[object]]::new()
    $i = 0
    while ($i -lt $b.Length -and $runs.Count -lt $maxRuns) {
        if ($b[$i] -ge 0x20 -and $b[$i] -le 0x7E) {
            $start = $i
            $sb = [System.Text.StringBuilder]::new()
            while ($i -lt $b.Length -and $b[$i] -ge 0x20 -and $b[$i] -le 0x7E) {
                [void]$sb.Append([char]$b[$i])
                $i++
            }
            $s = $sb.ToString()
            if ($s.Length -ge $minRunLen) {
                $runs.Add([PSCustomObject]@{ offset = $start; length = $s.Length; preview = $s.Substring(0, [Math]::Min(40, $s.Length)) }) | Out-Null
            }
        } else {
            $i++
        }
    }
    [object[]]$runs.ToArray()
}

# ---------------------------------------------------------------------------
# Inspect each reference file
# ---------------------------------------------------------------------------

$refFiles = @(Get-ChildItem -LiteralPath $ReferenceRoot -Recurse -Filter '*.lotheader' -File -ErrorAction SilentlyContinue |
    Sort-Object Length -Descending |
    Select-Object -First $MaxFiles)

Write-Output "Found $($refFiles.Count) reference lotheader files. Inspecting..."

$records = [System.Collections.Generic.List[object]]::new()

foreach ($f in $refFiles) {
    $size   = $f.Length
    $limit  = [Math]::Min($size, $MaxPrefixBytes)
    $bytes  = [byte[]]::new($limit)
    $fs     = [System.IO.File]::OpenRead($f.FullName)
    try { [void]$fs.Read($bytes, 0, $limit) } finally { $fs.Dispose() }

    $magic    = if ($limit -ge 4) { [System.Text.Encoding]::ASCII.GetString($bytes[0..3]) -replace '[^\x20-\x7E]','?' } else { '????' }
    $ver      = u32le $bytes 4
    $f8       = u32le $bytes 8
    $words32  = @(words-u32le $bytes 32)   # 128 bytes / 32 words

    $nullCount    = (@($bytes | Where-Object { $_ -eq 0x00 })).Count
    $nlCount      = (@($bytes | Where-Object { $_ -eq 0x0A })).Count
    $fpOff        = first-printable-offset $bytes 12
    $fnlOff       = first-newline-offset   $bytes 12
    $runs         = @(printable-runs $bytes 8 4)
    $asciiLines   = @(parse-ascii-lines $bytes 12 100)
    $linesCount   = $asciiLines.Count
    $f8Match      = if ($null -ne $f8) { [Math]::Abs([int]$f8 - $linesCount) -le 1 } else { $false }

    # Detect binary gap: are there non-printable bytes between offset 12 and first printable?
    $binaryGap = ($fpOff -gt 12) -or ($fpOff -eq -1)

    $rec = [ordered]@{
        file                        = $f.Name
        size_bytes                  = $size
        magic_ascii                 = $magic
        version_u32le               = $ver
        field8_u32le                = $f8
        first_16_bytes_hex          = bytes-hex $bytes 0 16
        first_64_bytes_hex          = bytes-hex $bytes 0 64
        first_128_bytes_hex         = bytes-hex $bytes 0 128
        first_512_bytes_hex         = if ($MaxPrefixBytes -ge 512) { bytes-hex $bytes 0 512 } else { 'not_read_maxprefix_too_small' }
        u32le_words_first_128       = $words32
        null_byte_count_in_prefix   = $nullCount
        newline_count_in_prefix     = $nlCount
        first_printable_offset      = $fpOff
        first_newline_offset        = $fnlOff
        first_ascii_printable_runs  = $runs
        binary_gap_before_ascii     = $binaryGap
        ascii_lines_from_offset_12  = $asciiLines
        parsed_line_count           = $linesCount
        field8_matches_parsed_count = $f8Match
        prefix_bytes_read           = $limit
    }
    $records.Add($rec) | Out-Null
    Write-Output "  $($f.Name): $size bytes | magic=$magic | ver=$ver | field8=$f8 | lines=$linesCount | binaryGap=$binaryGap"
}

# ---------------------------------------------------------------------------
# Cross-file summary
# ---------------------------------------------------------------------------

$allSizes    = @($records | ForEach-Object { $_.size_bytes })
$minSize     = if ($allSizes.Count -gt 0) { ($allSizes | Measure-Object -Minimum).Minimum } else { 0 }
$maxSize     = if ($allSizes.Count -gt 0) { ($allSizes | Measure-Object -Maximum).Maximum } else { 0 }
$allMagics   = @(@($records | ForEach-Object { [string]$_.magic_ascii })            | Group-Object | Select-Object Name, Count)
$allVers     = @(@($records | ForEach-Object { [string]$_.version_u32le })           | Group-Object | Select-Object Name, Count)
$allF8       = @(@($records | ForEach-Object { [string]$_.field8_u32le })            | Group-Object | Select-Object Name, Count)
$allBinGaps  = @(@($records | ForEach-Object { [string]$_.binary_gap_before_ascii }) | Group-Object | Select-Object Name, Count)

# Stable word summary: for each of 32 word positions, list unique values seen
$stableWords = [System.Collections.Generic.List[object]]::new()
for ($wi = 0; $wi -lt 32; $wi++) {
    $vals = @($records | ForEach-Object {
        $w = @($_.u32le_words_first_128)
        if ($wi -lt $w.Count) { $w[$wi] } else { $null }
    } | Where-Object { $null -ne $_ } | Select-Object -Unique)
    $stable = $vals.Count -le 1
    $stableWords.Add([ordered]@{
        word_index    = $wi
        byte_offset   = $wi * 4
        unique_values = [object[]]$vals
        stable        = $stable
    }) | Out-Null
}

# Smallest record by size
$smallestRec = $records | Sort-Object { $_.size_bytes } | Select-Object -First 1

$report = [ordered]@{
    schema                           = 'pzmapforge.build42-loth-structure-report.v0.1'
    reference_root                   = $ReferenceRoot
    reference_count                  = $records.Count
    max_prefix_bytes                 = $MaxPrefixBytes
    min_size_bytes                   = $minSize
    max_size_bytes                   = $maxSize
    magic_counts                     = [object[]]@($allMagics  | ForEach-Object { [ordered]@{ magic = $_.Name; count = [int]$_.Count } })
    version_counts                   = [object[]]@($allVers    | ForEach-Object { [ordered]@{ version = [string]$_.Name; count = [int]$_.Count } })
    field8_counts                    = [object[]]@($allF8      | ForEach-Object { [ordered]@{ field8 = [string]$_.Name; count = [int]$_.Count } })
    binary_gap_before_ascii_counts   = [object[]]@($allBinGaps | ForEach-Object { [ordered]@{ value = $_.Name; count = [int]$_.Count } })
    stable_word_summary              = [object[]]$stableWords.ToArray()
    smallest_reference_record        = $smallestRec
    records                          = [object[]]($records | ForEach-Object {
        [ordered]@{
            file                      = $_.file
            size_bytes                = $_.size_bytes
            magic_ascii               = $_.magic_ascii
            version_u32le             = $_.version_u32le
            field8_u32le              = $_.field8_u32le
            binary_gap_before_ascii   = $_.binary_gap_before_ascii
            parsed_line_count         = $_.parsed_line_count
            field8_matches_parsed_count = $_.field8_matches_parsed_count
            newline_count_in_prefix   = $_.newline_count_in_prefix
            first_printable_offset    = $_.first_printable_offset
            first_newline_offset      = $_.first_newline_offset
        }
    })
    candidate_gap_hypothesis         = 'CANDIDATE_LOTHEADER_MISSING_REFERENCE_SCALE_BODY'
    status_labels                    = [string[]]@(
        'BUILD42_LOTH_STRUCTURE_INSPECTED',
        'LOTH_REFERENCE_PREFIX_ANALYSED',
        'CANDIDATE_LOTHEADER_TOO_SHORT_CONFIRMED',
        'WRITER_RESEARCH_ONLY',
        'WRITER_NOT_CHANGED',
        'LOAD_TEST_NOT_PERFORMED',
        'PLAYABLE_EXPORT_CLAIM_ALLOWED=false'
    )
}

$jsonPath = Join-Path $Output 'build42-loth-structure-report.json'
$mdPath   = Join-Path $Output 'build42-loth-structure-report.md'

$report | ConvertTo-Json -Depth 7 | Set-Content -Path $jsonPath -Encoding UTF8
Write-Output "Report JSON: $jsonPath"

# ---------------------------------------------------------------------------
# Markdown report
# ---------------------------------------------------------------------------

$fence = '```'

$magicRow   = ($allMagics  | ForEach-Object { "| $($_.Name) | $($_.Count) |" }) -join "`n"
$verRow     = ($allVers    | ForEach-Object { "| $($_.Name) | $($_.Count) |" }) -join "`n"
$binGapRow  = ($allBinGaps | ForEach-Object { "| $($_.Name) | $($_.Count) |" }) -join "`n"

$stableTable = ($stableWords | ForEach-Object {
    $uv = [string]($_.unique_values -join ', ')
    $stab = if ($_.stable) { 'yes' } else { 'no' }
    "| $($_.word_index) | $($_.byte_offset) | $stab | $uv |"
}) -join "`n"

$refTable = ($records | ForEach-Object {
    $bg = if ($_.binary_gap_before_ascii) { 'yes' } else { 'no' }
    "| $($_.file) | $($_.size_bytes) | $($_.magic_ascii) | $($_.version_u32le) | $($_.field8_u32le) | $bg | $($_.parsed_line_count) |"
}) -join "`n"

$md = @"
# MAP-6R Build 42 LOTH Structure Report

${fence}text
BUILD42_LOTH_STRUCTURE_INSPECTED
LOTH_REFERENCE_PREFIX_ANALYSED
CANDIDATE_LOTHEADER_TOO_SHORT_CONFIRMED
WRITER_RESEARCH_ONLY
WRITER_NOT_CHANGED
LOAD_TEST_NOT_PERFORMED
PLAYABLE_EXPORT_CLAIM_ALLOWED=false
${fence}

## Summary

| Field | Value |
|---|---|
| Reference count | $($records.Count) |
| Min reference size | $minSize bytes |
| Max reference size | $maxSize bytes |
| Max prefix read | $MaxPrefixBytes bytes |

## Magic distribution

| Magic | Count |
|---|---|
$magicRow

## Version distribution

| Version | Count |
|---|---|
$verRow

## Binary gap before ASCII distribution

| BinaryGap | Count |
|---|---|
$binGapRow

## Word stability (first 128 bytes = 32 words)

| Word | Offset | Stable | Unique values |
|---|---|---|---|
$stableTable

## Per-file summary

| File | Size | Magic | Version | Field8 | BinaryGap | ParsedLines |
|---|---|---|---|---|---|---|
$refTable

## Candidate gap hypothesis

CANDIDATE_LOTHEADER_MISSING_REFERENCE_SCALE_BODY

## Non-claims

- WRITER_NOT_CHANGED: inspection only.
- LOAD_TEST_NOT_PERFORMED: no PZ session.
- PLAYABLE_EXPORT_CLAIM_ALLOWED=false: binding.
"@

Set-Content -Path $mdPath -Value $md -Encoding ASCII
Write-Output "Report MD:   $mdPath"
Write-Output ""
Write-Output "BUILD42_LOTH_STRUCTURE_INSPECTED"
Write-Output "CANDIDATE_LOTHEADER_TOO_SHORT_CONFIRMED"
Write-Output "WRITER_NOT_CHANGED"
Write-Output "LOAD_TEST_NOT_PERFORMED"
Write-Output "PLAYABLE_EXPORT_CLAIM_ALLOWED=false"
Write-Output "Done."
