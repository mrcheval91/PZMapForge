#Requires -Version 5.1
<#
.SYNOPSIS
    MAP-6V: Decodes the trailing binary section of Build 42 LOTH lotheader files.

    MAP-6U confirmed LOTH files have a substantial binary section after the ASCII
    string table (7018-33558 bytes in Dru_map references). This script reads the
    full file, parses the ASCII region, then analyses the trailing bytes to identify
    their structure (u32 alignment, index references, coordinate patterns).

    All inputs/outputs must be under .local/.
    Does NOT read PZ install folders. Does NOT write outside .local/.

    Writes:
      <Output>/build42-loth-trailing-body-decode.json
      <Output>/build42-loth-trailing-body-decode.md

.PARAMETER ReferenceRoot
    Path under .local/ containing reference Build 42 *.lotheader files.

.PARAMETER Output
    Path under .local/ for decode report output.

.PARAMETER MaxFiles
    Maximum number of reference files to inspect. Default: 20.

.PARAMETER MaxBytesPerFile
    Safety cap on bytes read per file. Default: 65536 (covers all expected LOTH sizes).

.EXAMPLE
    powershell -ExecutionPolicy Bypass -File scripts\decode-build42-loth-trailing-body.ps1 `
        -ReferenceRoot .local\reference-build42-map\Dru_map `
        -Output .local\map6v-loth-trailing-decode
#>

param(
    [Parameter(Mandatory=$true)][string]$ReferenceRoot,
    [Parameter(Mandatory=$true)][string]$Output,
    [int]$MaxFiles       = 20,
    [int]$MaxBytesPerFile = 65536
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
# Helpers
# ---------------------------------------------------------------------------

function bytes-hex ([byte[]]$b, [int]$start, [int]$len) {
    $end = [Math]::Min($start + $len, $b.Length) - 1
    if ($end -lt $start) { return '' }
    ($b[$start..$end] | ForEach-Object { $_.ToString('x2') }) -join ''
}

function read-u32le ([byte[]]$b, [int]$offset) {
    if ($offset + 3 -ge $b.Length) { return $null }
    [long][BitConverter]::ToUInt32($b, $offset)
}

function read-u32le-array ([byte[]]$b, [int]$startOffset, [int]$maxCount) {
    $out = [System.Collections.Generic.List[object]]::new()
    for ($i = $startOffset; $i -lt $startOffset + $maxCount * 4; $i += 4) {
        if ($i + 3 -lt $b.Length) {
            $out.Add([long][BitConverter]::ToUInt32($b, $i)) | Out-Null
        }
    }
    [object[]]$out.ToArray()
}

function infer-cell-coords ([string]$filename) {
    # Pattern: <cx>_<cy>.lotheader
    if ($filename -match '^(\d+)_(\d+)\.lotheader$') {
        return [int]$Matches[1], [int]$Matches[2]
    }
    return $null, $null
}

# ---------------------------------------------------------------------------
# Inspect each file
# ---------------------------------------------------------------------------

$refFiles = @(Get-ChildItem -LiteralPath $ReferenceRoot -Recurse -Filter '*.lotheader' -File -ErrorAction SilentlyContinue |
    Sort-Object Length -Descending |
    Select-Object -First $MaxFiles)

Write-Output "Found $($refFiles.Count) reference LOTH files. Decoding trailing bodies..."

$records = [System.Collections.Generic.List[object]]::new()

foreach ($f in $refFiles) {
    $readLimit = [Math]::Min($f.Length, $MaxBytesPerFile)
    $allBytes  = [byte[]]::new($readLimit)
    $fs        = [System.IO.File]::OpenRead($f.FullName)
    try { [void]$fs.Read($allBytes, 0, $readLimit) } finally { $fs.Dispose() }

    $cellX, $cellY = infer-cell-coords $f.Name

    $magic = if ($readLimit -ge 4) { [System.Text.Encoding]::ASCII.GetString($allBytes[0..3]) -replace '[^\x20-\x7E]','?' } else { '????' }
    $ver   = if ($readLimit -ge 8)  { read-u32le $allBytes 4 } else { $null }
    $f8    = if ($readLimit -ge 12) { read-u32le $allBytes 8 } else { $null }

    # Parse ASCII region
    $asciiEnd   = 12
    $entryCount = 0
    $curLen     = 0
    for ($i = 12; $i -lt $allBytes.Length; $i++) {
        $b = $allBytes[$i]
        if ($b -eq 0x0A) {
            if ($curLen -gt 0) { $entryCount++; $curLen = 0 }
            $asciiEnd = $i + 1
        } elseif ($b -ge 0x20 -and $b -le 0x7E) {
            $curLen++; $asciiEnd = $i + 1
        } else { break }
    }

    $trailStart  = $asciiEnd
    $trailBytes  = $f.Length - $trailStart
    $trailMod4   = $trailBytes % 4
    $trailU32Cnt = [Math]::Floor($trailBytes / 4)

    # Read first 16 and last 16 u32 words from trailing section
    $maxTrailRead   = [Math]::Min($trailStart + 256, $allBytes.Length)
    $firstU32       = @(read-u32le-array $allBytes $trailStart 16)
    # Last 16: read from end of file if we have it in our buffer
    $lastU32Start   = [Math]::Max($trailStart, $f.Length - 64)
    $lastU32        = @()
    if ($lastU32Start -lt $allBytes.Length) {
        $lastU32 = @(read-u32le-array $allBytes $lastU32Start 16)
    }

    # Statistical analysis of first-16 words
    $firstHex      = bytes-hex $allBytes $trailStart 128
    $allFirstU32   = @($firstU32 | Where-Object { $null -ne $_ })
    $zeroCount     = (@($allFirstU32 | Where-Object { $_ -eq 0 })).Count
    $nonzeroCount  = $allFirstU32.Count - $zeroCount
    $minU32        = if ($allFirstU32.Count -gt 0) { ($allFirstU32 | Measure-Object -Minimum).Minimum } else { $null }
    $maxU32        = if ($allFirstU32.Count -gt 0) { ($allFirstU32 | Measure-Object -Maximum).Maximum } else { $null }
    $distinctU32   = (@($allFirstU32 | Select-Object -Unique)).Count
    $firstNonzero  = -1
    for ($wi = 0; $wi -lt $allFirstU32.Count; $wi++) {
        if ($allFirstU32[$wi] -ne 0) { $firstNonzero = $wi; break }
    }

    # Coordinate candidate analysis
    $f8Val = if ($null -ne $f8) { [int]$f8 } else { 0 }
    $wordsLtF8     = (@($allFirstU32 | Where-Object { $null -ne $_ -and $_ -lt $f8Val })).Count
    $wordsEqF8     = (@($allFirstU32 | Where-Object { $null -ne $_ -and $_ -eq $f8Val })).Count
    $wordsIn300    = (@($allFirstU32 | Where-Object { $null -ne $_ -and $_ -le 300 -and $_ -gt 0 })).Count
    $wordsIn100    = (@($allFirstU32 | Where-Object { $null -ne $_ -and $_ -le 100 -and $_ -gt 0 })).Count

    # Hypotheses for this file
    $hyps = [System.Collections.Generic.List[string]]::new()
    if ($trailMod4 -eq 0)    { $hyps.Add('HYPOTHESIS_TRAILER_U32_RECORDS')                | Out-Null }
    else                     { $hyps.Add('HYPOTHESIS_TRAILER_HAS_NON_U32_PADDING')         | Out-Null }
    if ($allFirstU32.Count -gt 0 -and $wordsLtF8 -gt ($allFirstU32.Count / 2)) {
        $hyps.Add('HYPOTHESIS_TRAILER_REFERENCES_STRING_TABLE') | Out-Null
    }
    if ($allFirstU32.Count -gt 0 -and $wordsIn300 -gt ($allFirstU32.Count / 3)) {
        $hyps.Add('HYPOTHESIS_TRAILER_COORDINATE_OR_GRID_DATA') | Out-Null
    }
    if ($hyps.Count -le 1)   { $hyps.Add('HYPOTHESIS_TRAILER_UNKNOWN')                    | Out-Null }

    $rec = [ordered]@{
        file                         = $f.Name
        cell_x                       = $cellX
        cell_y                       = $cellY
        size_bytes                   = $f.Length
        magic_ascii                  = $magic
        version_u32le                = $ver
        field8_u32le                 = $f8
        ascii_entry_count            = $entryCount
        ascii_region_end_offset      = $asciiEnd
        trailing_start_offset        = $trailStart
        trailing_bytes_count         = $trailBytes
        trailing_bytes_mod4          = $trailMod4
        trailing_u32_count           = $trailU32Cnt
        first_128_trailing_bytes_hex = $firstHex
        first_64_trailing_u32le      = $firstU32
        last_64_trailing_u32le       = $lastU32
        zero_word_count              = $zeroCount
        nonzero_word_count           = $nonzeroCount
        min_u32                      = $minU32
        max_u32                      = $maxU32
        distinct_u32_count           = $distinctU32
        first_nonzero_word_index     = $firstNonzero
        words_less_than_field8       = $wordsLtF8
        words_equal_field8           = $wordsEqF8
        words_in_0_300               = $wordsIn300
        words_in_0_100               = $wordsIn100
        hypotheses                   = [string[]]$hyps.ToArray()
    }
    $records.Add($rec) | Out-Null

    $hyp1 = $hyps[0]
    Write-Output "  $($f.Name): trail=$trailBytes bytes mod4=$trailMod4 firstHyp=$hyp1 wordsLtF8=$wordsLtF8/$($allFirstU32.Count)"
}

# ---------------------------------------------------------------------------
# Cross-file stable-word analysis
# ---------------------------------------------------------------------------

$maxW = 16
$stablePrefix = [System.Collections.Generic.List[object]]::new()
for ($wi = 0; $wi -lt $maxW; $wi++) {
    $vals = @($records | ForEach-Object {
        $w = @($_.first_64_trailing_u32le)
        if ($wi -lt $w.Count) { $w[$wi] } else { $null }
    } | Where-Object { $null -ne $_ } | Select-Object -Unique)
    $stablePrefix.Add([ordered]@{
        word_index = $wi; byte_offset_in_trailer = $wi*4
        unique_values = [object[]]$vals; stable = ($vals.Count -le 1)
    }) | Out-Null
}

# ---------------------------------------------------------------------------
# Overall hypothesis
# ---------------------------------------------------------------------------

$allMod4     = (@($records | Where-Object { $_.trailing_bytes_mod4 -eq 0 })).Count
$allHasTrail = (@($records | Where-Object { $_.trailing_bytes_count -gt 0 })).Count
$anyRefStr   = (@($records | Where-Object { $_.hypotheses -contains 'HYPOTHESIS_TRAILER_REFERENCES_STRING_TABLE' })).Count
$anyCoord    = (@($records | Where-Object { $_.hypotheses -contains 'HYPOTHESIS_TRAILER_COORDINATE_OR_GRID_DATA' })).Count

$overallHypotheses = [System.Collections.Generic.List[string]]::new()
$overallHypotheses.Add('LOTH_REQUIRES_TRAILING_BINARY_BODY') | Out-Null
if ($allMod4 -eq $records.Count -and $records.Count -gt 0) {
    $overallHypotheses.Add('HYPOTHESIS_TRAILER_U32_RECORDS') | Out-Null
}
if ($anyRefStr -gt ($records.Count / 2)) {
    $overallHypotheses.Add('HYPOTHESIS_TRAILER_REFERENCES_STRING_TABLE') | Out-Null
}
if ($anyCoord -gt ($records.Count / 2)) {
    $overallHypotheses.Add('HYPOTHESIS_TRAILER_COORDINATE_OR_GRID_DATA') | Out-Null
}
if ($overallHypotheses.Count -le 1) {
    $overallHypotheses.Add('HYPOTHESIS_TRAILER_UNKNOWN') | Out-Null
}

$allSizes    = @($records | ForEach-Object { $_.size_bytes })
$allTrail    = @($records | ForEach-Object { $_.trailing_bytes_count })
$minSize     = if ($allSizes.Count  -gt 0) { ($allSizes  | Measure-Object -Minimum).Minimum } else { 0 }
$maxSize     = if ($allSizes.Count  -gt 0) { ($allSizes  | Measure-Object -Maximum).Maximum } else { 0 }
$minTrail    = if ($allTrail.Count  -gt 0) { ($allTrail  | Measure-Object -Minimum).Minimum } else { 0 }
$maxTrail    = if ($allTrail.Count  -gt 0) { ($allTrail  | Measure-Object -Maximum).Maximum } else { 0 }

$report = [ordered]@{
    schema                     = 'pzmapforge.build42-loth-trailing-body-decode.v0.1'
    reference_root             = $ReferenceRoot
    reference_count            = $records.Count
    min_size_bytes             = $minSize
    max_size_bytes             = $maxSize
    min_trailing_bytes         = $minTrail
    max_trailing_bytes         = $maxTrail
    count_u32_aligned          = $allMod4
    count_with_trailing_body   = $allHasTrail
    count_references_string_table = $anyRefStr
    count_coordinate_pattern   = $anyCoord
    overall_hypotheses         = [string[]]$overallHypotheses.ToArray()
    stable_prefix_word_summary = [object[]]$stablePrefix.ToArray()
    status_labels              = [string[]]@(
        'BUILD42_LOTH_TRAILING_BODY_DECODED',
        'LOTH_REQUIRES_TRAILING_BINARY_BODY',
        'WRITER_RESEARCH_ONLY',
        'WRITER_NOT_CHANGED',
        'LOAD_TEST_NOT_PERFORMED',
        'PLAYABLE_EXPORT_CLAIM_ALLOWED=false'
    )
    records                    = [object[]]($records | ForEach-Object {
        [ordered]@{
            file                     = $_.file
            size_bytes               = $_.size_bytes
            field8_u32le             = $_.field8_u32le
            ascii_entry_count        = $_.ascii_entry_count
            trailing_bytes_count     = $_.trailing_bytes_count
            trailing_bytes_mod4      = $_.trailing_bytes_mod4
            trailing_u32_count       = $_.trailing_u32_count
            zero_word_count          = $_.zero_word_count
            nonzero_word_count       = $_.nonzero_word_count
            words_less_than_field8   = $_.words_less_than_field8
            words_in_0_300           = $_.words_in_0_300
            first_64_trailing_u32le  = $_.first_64_trailing_u32le
            hypotheses               = $_.hypotheses
        }
    })
}

$jsonPath = Join-Path $Output 'build42-loth-trailing-body-decode.json'
$mdPath   = Join-Path $Output 'build42-loth-trailing-body-decode.md'

$report | ConvertTo-Json -Depth 7 | Set-Content -Path $jsonPath -Encoding UTF8
Write-Output "Decode JSON: $jsonPath"

# ---------------------------------------------------------------------------
# Markdown
# ---------------------------------------------------------------------------

$fence = '```'
$hypsStr = $overallHypotheses -join ', '

$refTable = ($records | ForEach-Object {
    $hyp1 = if ($_.hypotheses.Count -gt 0) { $_.hypotheses[0] } else { '-' }
    "| $($_.file) | $($_.trailing_bytes_count) | $($_.trailing_bytes_mod4) | $($_.words_less_than_field8) | $hyp1 |"
}) -join "`n"

$stableTable = ($stablePrefix | ForEach-Object {
    $stab = if ($_.stable) { 'yes' } else { 'no' }
    $uv   = [string]($_.unique_values -join ', ')
    "| $($_.word_index) | $($_.byte_offset_in_trailer) | $stab | $uv |"
}) -join "`n"

$md = @"
# MAP-6V Build 42 LOTH Trailing Body Decode

${fence}text
BUILD42_LOTH_TRAILING_BODY_DECODED
LOTH_REQUIRES_TRAILING_BINARY_BODY
WRITER_RESEARCH_ONLY
WRITER_NOT_CHANGED
LOAD_TEST_NOT_PERFORMED
PLAYABLE_EXPORT_CLAIM_ALLOWED=false
${fence}

## Overall hypotheses

$hypsStr

## Summary

| Field | Value |
|---|---|
| Reference count | $($records.Count) |
| Min size | $minSize bytes |
| Max size | $maxSize bytes |
| Min trailing bytes | $minTrail |
| Max trailing bytes | $maxTrail |
| U32-aligned count | $allMod4 / $($records.Count) |
| References string table | $anyRefStr / $($records.Count) |
| Coordinate pattern | $anyCoord / $($records.Count) |

## Per-file records

| File | TrailingBytes | Mod4 | WordsLtF8 | Hypothesis1 |
|---|---|---|---|---|
$refTable

## Stable prefix word analysis (first 16 u32 words of trailer)

| Word | TrailerOffset | Stable | Unique values |
|---|---|---|---|
$stableTable

## Non-claims

- WRITER_NOT_CHANGED: decode is research only.
- LOAD_TEST_NOT_PERFORMED: no PZ session.
- PLAYABLE_EXPORT_CLAIM_ALLOWED=false: binding.
"@

Set-Content -Path $mdPath -Value $md -Encoding ASCII
Write-Output "Decode MD:   $mdPath"
Write-Output ""
Write-Output "overall_hypotheses:            $hypsStr"
Write-Output "count_u32_aligned:             $allMod4 / $($records.Count)"
Write-Output "count_references_string_table: $anyRefStr / $($records.Count)"
Write-Output "BUILD42_LOTH_TRAILING_BODY_DECODED"
Write-Output "WRITER_NOT_CHANGED"
Write-Output "LOAD_TEST_NOT_PERFORMED"
Write-Output "PLAYABLE_EXPORT_CLAIM_ALLOWED=false"
Write-Output "Done."
