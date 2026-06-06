#Requires -Version 5.1
<#
.SYNOPSIS
    MAP-6U: Inspects the full body of Build 42 LOTH lotheader files from
    reference files copied under .local/. Reads the complete file to find
    where the newline-delimited ASCII string table ends, and records any
    trailing binary content after the last entry.

    Determines whether Build 42 LOTH files require a trailing binary section
    after the ASCII string table (LOTH_REQUIRES_TRAILING_BINARY_BODY).

    All inputs/outputs must be under .local/.
    Does NOT read PZ install folders. Does NOT write outside .local/.

    Writes:
      <Output>/build42-loth-full-body-report.json
      <Output>/build42-loth-full-body-report.md

.PARAMETER ReferenceRoot
    Path under .local/ containing reference Build 42 *.lotheader files.

.PARAMETER Output
    Path under .local/ for report output.

.PARAMETER MaxFiles
    Maximum number of reference files to inspect. Default: 20.

.EXAMPLE
    powershell -ExecutionPolicy Bypass -File scripts\inspect-build42-loth-full-body.ps1 `
        -ReferenceRoot .local\reference-build42-map\Dru_map `
        -Output .local\map6u-loth-full-body
#>

param(
    [Parameter(Mandatory=$true)][string]$ReferenceRoot,
    [Parameter(Mandatory=$true)][string]$Output,
    [int]$MaxFiles = 20
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

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
# Helper: hex encoding
# ---------------------------------------------------------------------------

function bytes-hex ([byte[]]$b, [int]$start, [int]$len) {
    $end = [Math]::Min($start + $len, $b.Length) - 1
    if ($end -lt $start) { return '' }
    ($b[$start..$end] | ForEach-Object { $_.ToString('x2') }) -join ''
}

function words-u32le ([byte[]]$b, [int]$startOffset, [int]$maxWords) {
    $out = [System.Collections.Generic.List[object]]::new()
    for ($i = $startOffset; $i -lt $startOffset + $maxWords * 4; $i += 4) {
        if ($i + 3 -lt $b.Length) {
            $out.Add([long][BitConverter]::ToUInt32($b, $i)) | Out-Null
        } else {
            $out.Add($null) | Out-Null
        }
    }
    [object[]]$out.ToArray()
}

# ---------------------------------------------------------------------------
# Inspect each reference file
# ---------------------------------------------------------------------------

$refFiles = @(Get-ChildItem -LiteralPath $ReferenceRoot -Recurse -Filter '*.lotheader' -File -ErrorAction SilentlyContinue |
    Sort-Object Length -Descending |
    Select-Object -First $MaxFiles)

Write-Output "Found $($refFiles.Count) reference lotheader files. Inspecting full bodies..."

$records = [System.Collections.Generic.List[object]]::new()

foreach ($f in $refFiles) {
    $allBytes = [System.IO.File]::ReadAllBytes($f.FullName)
    $size     = $allBytes.Length

    $magic = if ($size -ge 4) { [System.Text.Encoding]::ASCII.GetString($allBytes[0..3]) -replace '[^\x20-\x7E]','?' } else { '????' }
    $ver   = if ($size -ge 8)  { [BitConverter]::ToUInt32($allBytes, 4) } else { $null }
    $f8    = if ($size -ge 12) { [BitConverter]::ToUInt32($allBytes, 8) } else { $null }

    # Parse ASCII entries from offset 12 onward
    # Stop as soon as we hit a byte that is neither 0x0A (newline) nor a printable ASCII (0x20-0x7E)
    $asciiStart  = 12
    $asciiEnd    = $asciiStart
    $entryCount  = 0
    $currentLen  = 0

    for ($i = $asciiStart; $i -lt $allBytes.Length; $i++) {
        $b = $allBytes[$i]
        if ($b -eq 0x0A) {
            # End of an entry
            if ($currentLen -gt 0) {
                $entryCount++
                $currentLen = 0
            }
            $asciiEnd = $i + 1
        } elseif ($b -ge 0x20 -and $b -le 0x7E) {
            $currentLen++
            $asciiEnd = $i + 1
        } else {
            # Non-printable, non-newline byte: end of ASCII region
            break
        }
    }

    $trailingStart  = $asciiEnd
    $trailingCount  = $size - $trailingStart
    $trailingHex    = if ($trailingCount -gt 0) { bytes-hex $allBytes $trailingStart 64 } else { '' }
    $trailingWords  = if ($trailingCount -ge 4) { @(words-u32le $allBytes $trailingStart 16) } else { @() }

    $f8MatchParsed  = $null -ne $f8 -and ($f8 -eq $entryCount -or $f8 -eq ($entryCount - 1) -or $f8 -eq ($entryCount + 1))
    $hasTrailing    = $trailingCount -gt 0

    $rec = [ordered]@{
        file                         = $f.Name
        size_bytes                   = $size
        magic_ascii                  = $magic
        version_u32le                = $ver
        field8_u32le                 = $f8
        ascii_region_start_offset    = $asciiStart
        ascii_region_end_offset      = $asciiEnd
        ascii_entry_count            = $entryCount
        trailing_bytes_count         = $trailingCount
        trailing_body_exists         = $hasTrailing
        first_64_trailing_bytes_hex  = $trailingHex
        trailing_u32le_words_first_64 = $trailingWords
        field8_matches_ascii_count   = $f8MatchParsed
    }
    $records.Add($rec) | Out-Null

    Write-Output "  $($f.Name): $size bytes | magic=$magic | field8=$f8 | asciiEntries=$entryCount | trailing=$trailingCount"
}

# ---------------------------------------------------------------------------
# Cross-file summary
# ---------------------------------------------------------------------------

$allSizes      = @($records | ForEach-Object { $_.size_bytes })
$allF8         = @($records | ForEach-Object { if ($null -ne $_.field8_u32le) { [int]$_.field8_u32le } else { 0 } })
$allAsciiCnt   = @($records | ForEach-Object { $_.ascii_entry_count })
$allTrailing   = @($records | ForEach-Object { $_.trailing_bytes_count })

$minSize       = if ($allSizes.Count    -gt 0) { ($allSizes    | Measure-Object -Minimum).Minimum } else { 0 }
$maxSize       = if ($allSizes.Count    -gt 0) { ($allSizes    | Measure-Object -Maximum).Maximum } else { 0 }
$minF8         = if ($allF8.Count       -gt 0) { ($allF8       | Measure-Object -Minimum).Minimum } else { 0 }
$maxF8         = if ($allF8.Count       -gt 0) { ($allF8       | Measure-Object -Maximum).Maximum } else { 0 }
$minAscii      = if ($allAsciiCnt.Count -gt 0) { ($allAsciiCnt | Measure-Object -Minimum).Minimum } else { 0 }
$maxAscii      = if ($allAsciiCnt.Count -gt 0) { ($allAsciiCnt | Measure-Object -Maximum).Maximum } else { 0 }
$minTrailing   = if ($allTrailing.Count -gt 0) { ($allTrailing | Measure-Object -Minimum).Minimum } else { 0 }
$maxTrailing   = if ($allTrailing.Count -gt 0) { ($allTrailing | Measure-Object -Maximum).Maximum } else { 0 }

$countTrailing = (@($records | Where-Object { $_.trailing_body_exists -eq $true })).Count
$countF8Match  = (@($records | Where-Object { $_.field8_matches_ascii_count -eq $true })).Count

# Hypothesis
$requiresTrailing = $countTrailing -gt 0 -and $countTrailing -eq $records.Count
$hypothesis = if ($requiresTrailing) {
    'LOTH_REQUIRES_TRAILING_BINARY_BODY'
} elseif ($countTrailing -gt 0) {
    'LOTH_TRAILING_BODY_PRESENT_IN_SOME_FILES'
} else {
    'LOTH_NO_TRAILING_BODY_DETECTED'
}

# Stable trailing word positions (first 16 words among files that have trailing bytes)
$trailingWordSummary = [System.Collections.Generic.List[object]]::new()
$trailingRecs = @($records | Where-Object { $_.trailing_bytes_count -ge 4 })
for ($wi = 0; $wi -lt 16; $wi++) {
    $vals = @($trailingRecs | ForEach-Object {
        $w = @($_.trailing_u32le_words_first_64)
        if ($wi -lt $w.Count) { $w[$wi] } else { $null }
    } | Where-Object { $null -ne $_ } | Select-Object -Unique)
    $trailingWordSummary.Add([ordered]@{
        word_index    = $wi
        byte_offset_relative = $wi * 4
        unique_values = [object[]]$vals
        stable        = $vals.Count -le 1
    }) | Out-Null
}

$report = [ordered]@{
    schema                   = 'pzmapforge.build42-loth-full-body-report.v0.1'
    reference_root           = $ReferenceRoot
    reference_count          = $records.Count
    min_size_bytes           = $minSize
    max_size_bytes           = $maxSize
    min_field8               = $minF8
    max_field8               = $maxF8
    min_ascii_entry_count    = $minAscii
    max_ascii_entry_count    = $maxAscii
    min_trailing_bytes       = $minTrailing
    max_trailing_bytes       = $maxTrailing
    count_with_trailing_body = $countTrailing
    count_field8_matches_ascii = $countF8Match
    hypothesis               = $hypothesis
    trailing_word_summary    = [object[]]$trailingWordSummary.ToArray()
    status_labels            = [string[]]@(
        $hypothesis,
        'WRITER_NOT_CHANGED',
        'LOAD_TEST_NOT_PERFORMED',
        'PLAYABLE_EXPORT_CLAIM_ALLOWED=false'
    )
    records                  = [object[]]($records | ForEach-Object {
        [ordered]@{
            file                     = $_.file
            size_bytes               = $_.size_bytes
            magic_ascii              = $_.magic_ascii
            field8_u32le             = $_.field8_u32le
            ascii_entry_count        = $_.ascii_entry_count
            ascii_region_end_offset  = $_.ascii_region_end_offset
            trailing_bytes_count     = $_.trailing_bytes_count
            trailing_body_exists     = $_.trailing_body_exists
            field8_matches_ascii_count = $_.field8_matches_ascii_count
        }
    })
}

$jsonPath = Join-Path $Output 'build42-loth-full-body-report.json'
$mdPath   = Join-Path $Output 'build42-loth-full-body-report.md'

$report | ConvertTo-Json -Depth 6 | Set-Content -Path $jsonPath -Encoding UTF8
Write-Output "Report JSON: $jsonPath"

# ---------------------------------------------------------------------------
# Markdown report
# ---------------------------------------------------------------------------

$fence = '```'

$refTable = ($records | ForEach-Object {
    $tb = if ($_.trailing_body_exists) { 'yes' } else { 'no' }
    $fm = if ($_.field8_matches_ascii_count) { 'yes' } else { 'no' }
    "| $($_.file) | $($_.size_bytes) | $($_.field8_u32le) | $($_.ascii_entry_count) | $($_.trailing_bytes_count) | $tb | $fm |"
}) -join "`n"

$md = @"
# MAP-6U Build 42 LOTH Full Body Report

${fence}text
$hypothesis
WRITER_NOT_CHANGED
LOAD_TEST_NOT_PERFORMED
PLAYABLE_EXPORT_CLAIM_ALLOWED=false
${fence}

## Summary

| Field | Value |
|---|---|
| Reference count | $($records.Count) |
| Min size | $minSize bytes |
| Max size | $maxSize bytes |
| Min field8 | $minF8 |
| Max field8 | $maxF8 |
| Min ASCII entry count | $minAscii |
| Max ASCII entry count | $maxAscii |
| Min trailing bytes | $minTrailing |
| Max trailing bytes | $maxTrailing |
| Files with trailing body | $countTrailing / $($records.Count) |
| Files where field8 matches ASCII count | $countF8Match / $($records.Count) |

## Hypothesis

$hypothesis

## Per-file records

| File | Size | Field8 | AsciiEntries | TrailingBytes | HasTrailing | F8Match |
|---|---|---|---|---|---|---|
$refTable

## Non-claims

- WRITER_NOT_CHANGED: full body inspection only.
- LOAD_TEST_NOT_PERFORMED: no PZ session.
- PLAYABLE_EXPORT_CLAIM_ALLOWED=false: binding.
"@

Set-Content -Path $mdPath -Value $md -Encoding ASCII
Write-Output "Report MD:   $mdPath"
Write-Output ""
Write-Output "hypothesis:                      $hypothesis"
Write-Output "count_with_trailing_body:        $countTrailing / $($records.Count)"
Write-Output "min_trailing_bytes:              $minTrailing"
Write-Output "max_trailing_bytes:              $maxTrailing"
Write-Output "WRITER_NOT_CHANGED"
Write-Output "LOAD_TEST_NOT_PERFORMED"
Write-Output "PLAYABLE_EXPORT_CLAIM_ALLOWED=false"
Write-Output "Done."
