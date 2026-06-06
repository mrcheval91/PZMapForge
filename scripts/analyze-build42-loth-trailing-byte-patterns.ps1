#Requires -Version 5.1
<#
.SYNOPSIS
    MAP-6W: Byte-level analysis of the LOTH trailing body section.

    MAP-6V showed the trailing section is NOT u32-aligned in 17/20 reference
    files (HYPOTHESIS_TRAILER_UNKNOWN). MAP-6W deepens the analysis to U16/byte
    level to determine structure type.

    All inputs/outputs must be under .local/.
    Does NOT read PZ install folders. Does NOT write outside .local/.

    Writes:
      <Output>/build42-loth-trailing-byte-patterns.json
      <Output>/build42-loth-trailing-byte-patterns.md

.PARAMETER ReferenceRoot
    Path under .local/ containing reference Build 42 *.lotheader files.

.PARAMETER Output
    Path under .local/ for pattern analysis output.

.PARAMETER MaxFiles
    Max reference files to sample. Default: 20.

.PARAMETER FocusSmallestCount
    Number of smallest files to analyse in depth. Default: 5.

.PARAMETER MaxBytesPerFile
    Safety cap on bytes read per file. Default: 131072.

.EXAMPLE
    powershell -ExecutionPolicy Bypass -File scripts\analyze-build42-loth-trailing-byte-patterns.ps1 `
        -ReferenceRoot .local\reference-build42-map\Dru_map `
        -Output .local\map6w-loth-byte-patterns
#>

param(
    [Parameter(Mandatory=$true)][string]$ReferenceRoot,
    [Parameter(Mandatory=$true)][string]$Output,
    [int]$MaxFiles          = 20,
    [int]$FocusSmallestCount = 5,
    [int]$MaxBytesPerFile    = 131072
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

function infer-cell ([string]$n) {
    if ($n -match '^(\d+)_(\d+)\.lotheader$') { return [int]$Matches[1], [int]$Matches[2] }
    return $null, $null
}

function entropy-est ([byte[]]$b) {
    if ($b.Length -eq 0) { return 0.0 }
    $freq = @{}
    foreach ($byte in $b) {
        $k = [string]$byte
        if ($freq.ContainsKey($k)) { $freq[$k]++ } else { $freq[$k] = 1 }
    }
    $e = 0.0
    foreach ($v in $freq.Values) {
        $p = $v / $b.Length
        if ($p -gt 0) { $e -= $p * [Math]::Log($p, 2) }
    }
    [Math]::Round($e, 3)
}

function top-bytes ([byte[]]$b, [int]$n) {
    $freq = @{}
    foreach ($byte in $b) {
        $k = [int]$byte
        if ($freq.ContainsKey($k)) { $freq[$k]++ } else { $freq[$k] = 1 }
    }
    @($freq.GetEnumerator() | Sort-Object Value -Descending | Select-Object -First $n |
        ForEach-Object { [ordered]@{ byte = $_.Key; count = $_.Value } })
}

function top-u16 ([byte[]]$b, [int]$startOff, [int]$maxWords, [int]$topN) {
    $freq = @{}
    for ($i = $startOff; $i -lt $startOff + $maxWords * 2 -and $i + 1 -lt $b.Length; $i += 2) {
        $v = [BitConverter]::ToUInt16($b, $i)
        $k = [string]$v
        if ($freq.ContainsKey($k)) { $freq[$k]++ } else { $freq[$k] = 1 }
    }
    @($freq.GetEnumerator() | Sort-Object Value -Descending | Select-Object -First $topN |
        ForEach-Object { [ordered]@{ value = [int]$_.Key; count = $_.Value } })
}

function top-u32 ([byte[]]$b, [int]$startOff, [int]$maxWords, [int]$topN) {
    $freq = @{}
    for ($i = $startOff; $i -lt $startOff + $maxWords * 4 -and $i + 3 -lt $b.Length; $i += 4) {
        $v = [BitConverter]::ToUInt32($b, $i)
        $k = [string]$v
        if ($freq.ContainsKey($k)) { $freq[$k]++ } else { $freq[$k] = 1 }
    }
    @($freq.GetEnumerator() | Sort-Object Value -Descending | Select-Object -First $topN |
        ForEach-Object { [ordered]@{ value = [long]$_.Key; count = $_.Value } })
}

function count-length-prefixed ([byte[]]$b, [int]$startOff, [int]$maxCount, [bool]$useU16) {
    $count = 0; $i = $startOff
    $step  = if ($useU16) { 2 } else { 1 }
    while ($i -lt $b.Length -and $count -lt $maxCount) {
        $len = if ($useU16) {
            if ($i + 1 -lt $b.Length) { [int][BitConverter]::ToUInt16($b, $i) } else { break }
        } else { [int]$b[$i] }
        $i += $step
        if ($len -le 0 -or $len -gt 512) { break }
        $end = $i + $len
        if ($end -gt $b.Length) { break }
        $allPrint = $true
        for ($j = $i; $j -lt $end; $j++) {
            if ($b[$j] -lt 0x20 -or $b[$j] -gt 0x7E) { $allPrint = $false; break }
        }
        if ($allPrint) { $count++ }
        $i = $end
    }
    $count
}

# ---------------------------------------------------------------------------
# Collect and sort reference files
# ---------------------------------------------------------------------------

$allFiles = @(Get-ChildItem -LiteralPath $ReferenceRoot -Recurse -Filter '*.lotheader' -File -ErrorAction SilentlyContinue |
    Sort-Object Length -Descending |
    Select-Object -First $MaxFiles)

Write-Output "Found $($allFiles.Count) reference files. Analysing trailing byte patterns..."

$records = [System.Collections.Generic.List[object]]::new()

foreach ($f in $allFiles) {
    $readLimit = [Math]::Min($f.Length, $MaxBytesPerFile)
    $allBytes  = [byte[]]::new($readLimit)
    $fs        = [System.IO.File]::OpenRead($f.FullName)
    try { [void]$fs.Read($allBytes, 0, $readLimit) } finally { $fs.Dispose() }

    $cellX, $cellY = infer-cell $f.Name
    $magic = if ($readLimit -ge 4) { [System.Text.Encoding]::ASCII.GetString($allBytes[0..3]) -replace '[^\x20-\x7E]','?' } else { '????' }
    $ver   = if ($readLimit -ge 8)  { [long][BitConverter]::ToUInt32($allBytes, 4) } else { $null }
    $f8    = if ($readLimit -ge 12) { [long][BitConverter]::ToUInt32($allBytes, 8) } else { $null }

    # Parse ASCII region
    $asciiEnd   = 12; $entryCount = 0; $curLen = 0
    for ($i = 12; $i -lt $allBytes.Length; $i++) {
        $b = $allBytes[$i]
        if ($b -eq 0x0A)                          { if ($curLen -gt 0) { $entryCount++; $curLen = 0 }; $asciiEnd = $i+1 }
        elseif ($b -ge 0x20 -and $b -le 0x7E)    { $curLen++; $asciiEnd = $i+1 }
        else                                       { break }
    }

    $tStart  = $asciiEnd
    $tBytes  = $f.Length - $tStart
    $tMod2   = $tBytes % 2
    $tMod4   = $tBytes % 4
    $tMod8   = $tBytes % 8

    # Extract trailing slice from buffer
    $tBufStart = $tStart
    $tBufEnd   = [Math]::Min($tStart + 512, $allBytes.Length)
    $tSlice    = if ($tBufEnd -gt $tBufStart) { $allBytes[$tBufStart..($tBufEnd-1)] } else { [byte[]]::new(0) }

    # Last 256 bytes (from end of actual file via buffer tail)
    $lastStart = [Math]::Max($tBufStart, $allBytes.Length - 256)
    $lastSlice = if ($lastStart -lt $allBytes.Length) { $allBytes[$lastStart..($allBytes.Length-1)] } else { [byte[]]::new(0) }

    $firstHex  = bytes-hex $allBytes $tStart 256
    $lastHex   = bytes-hex $allBytes ($allBytes.Length - [Math]::Min(256, $tSlice.Length)) 256

    # Byte-level statistics
    $zeroCnt      = (@($tSlice | Where-Object { $_ -eq 0 })).Count
    $printCnt     = (@($tSlice | Where-Object { $_ -ge 0x20 -and $_ -le 0x7E })).Count
    $highBitCnt   = (@($tSlice | Where-Object { $_ -ge 0x80 })).Count
    $entrop       = entropy-est $tSlice
    $byteHist     = @(top-bytes $tSlice 16)
    $firstNonzero = -1
    for ($i = 0; $i -lt $tSlice.Length; $i++) { if ($tSlice[$i] -ne 0) { $firstNonzero = $i; break } }
    $lastNonzero  = -1
    for ($i = $tSlice.Length-1; $i -ge 0; $i--) { if ($tSlice[$i] -ne 0) { $lastNonzero = $i; break } }

    # U16 analysis
    $u16arr    = [System.Collections.Generic.List[object]]::new()
    for ($i = 0; $i -lt [Math]::Min($tSlice.Length, 128); $i += 2) {
        if ($i + 1 -lt $tSlice.Length) { $u16arr.Add([int][BitConverter]::ToUInt16($tSlice, $i)) | Out-Null }
    }
    $u16Words  = [object[]]$u16arr.ToArray()
    $u16Top    = @(top-u16 $tSlice 0 64 16)
    $f8Val     = if ($null -ne $f8) { [int]$f8 } else { 0 }
    $u16LtF8   = (@($u16Words | Where-Object { $null -ne $_ -and $_ -lt $f8Val -and $_ -gt 0 })).Count
    $u16RatioF8 = if ($u16Words.Count -gt 0) { [Math]::Round($u16LtF8 / $u16Words.Count, 3) } else { 0.0 }

    # U32 analysis
    $u32arr    = [System.Collections.Generic.List[object]]::new()
    for ($i = 0; $i -lt [Math]::Min($tSlice.Length, 256); $i += 4) {
        if ($i + 3 -lt $tSlice.Length) { $u32arr.Add([long][BitConverter]::ToUInt32($tSlice, $i)) | Out-Null }
    }
    $u32Words  = [object[]]$u32arr.ToArray()
    $u32Top    = @(top-u32 $tSlice 0 64 16)
    $u32LtF8   = (@($u32Words | Where-Object { $null -ne $_ -and $_ -lt $f8Val -and $_ -gt 0 })).Count
    $u32RatioF8 = if ($u32Words.Count -gt 0) { [Math]::Round($u32LtF8 / $u32Words.Count, 3) } else { 0.0 }

    # Length-prefixed string scan
    $lpU8Cnt  = count-length-prefixed $tSlice 0 20 $false
    $lpU16Cnt = count-length-prefixed $tSlice 0 20 $true

    # Compression probes
    $compCandidates = [System.Collections.Generic.List[object]]::new()
    for ($i = 0; $i -lt $tSlice.Length - 1; $i++) {
        $b0 = $tSlice[$i]; $b1 = $tSlice[$i+1]
        if (($b0 -eq 0x78 -and ($b1 -eq 0x01 -or $b1 -eq 0x9C -or $b1 -eq 0xDA)) -or
            ($b0 -eq 0x1F -and $b1 -eq 0x8B)) {
            $tag = if ($b0 -eq 0x1F) { 'gzip' } else { "zlib_$($b1.ToString('x2'))" }
            $compCandidates.Add([ordered]@{ offset = $i; header = "$($b0.ToString('x2'))$($b1.ToString('x2'))"; type = $tag }) | Out-Null
        }
    }

    # Structural markers
    $nlCount  = (@($tSlice | Where-Object { $_ -eq 0x0A })).Count
    $crlf     = 0
    for ($i = 0; $i -lt $tSlice.Length - 1; $i++) { if ($tSlice[$i] -eq 0x0D -and $tSlice[$i+1] -eq 0x0A) { $crlf++ } }

    $rec = [ordered]@{
        file                          = $f.Name
        cell_x                        = $cellX
        cell_y                        = $cellY
        size_bytes                    = $f.Length
        field8_u32le                  = $f8
        ascii_entry_count             = $entryCount
        ascii_region_end_offset       = $asciiEnd
        trailing_start_offset         = $tStart
        trailing_bytes_count          = $tBytes
        trailing_bytes_mod2           = $tMod2
        trailing_bytes_mod4           = $tMod4
        trailing_bytes_mod8           = $tMod8
        first_256_trailing_hex        = $firstHex
        last_256_trailing_hex         = $lastHex
        byte_histogram_top_16         = $byteHist
        zero_byte_count               = $zeroCnt
        printable_byte_count          = $printCnt
        high_bit_byte_count           = $highBitCnt
        entropy_estimate              = $entrop
        first_nonzero_offset          = $firstNonzero
        last_nonzero_offset_in_buffer = $lastNonzero
        u16le_first_64                = $u16Words
        u16le_top_values              = $u16Top
        u32le_first_64                = $u32Words
        u32le_top_values              = $u32Top
        values_less_than_field8_u16   = $u16LtF8
        values_less_than_field8_u32   = $u32LtF8
        plausible_string_index_ratio_u16 = $u16RatioF8
        plausible_string_index_ratio_u32 = $u32RatioF8
        length_prefixed_u8_segments   = $lpU8Cnt
        length_prefixed_u16_segments  = $lpU16Cnt
        compression_candidates        = [object[]]$compCandidates.ToArray()
        newline_count_in_trailer      = $nlCount
        crlf_count_in_trailer         = $crlf
    }
    $records.Add($rec) | Out-Null

    Write-Output "  $($f.Name): trail=$tBytes mod4=$tMod4 entrpy=$($entrop) u16ratF8=$($u16RatioF8) lp_u16=$lpU16Cnt"
}

# ---------------------------------------------------------------------------
# Focus on smallest files
# ---------------------------------------------------------------------------

$sorted       = @($records | Sort-Object { $_.trailing_bytes_count })
$focusRecords = @($sorted | Select-Object -First $FocusSmallestCount)

# Common first bytes across focus files
$commonFirstBytes = [System.Collections.Generic.List[object]]::new()
for ($pos = 0; $pos -lt 32; $pos++) {
    $vals = @($focusRecords | ForEach-Object {
        $hex = $_.first_256_trailing_hex
        if ($hex.Length -ge ($pos+1)*2) { [System.Convert]::ToInt32($hex.Substring($pos*2, 2), 16) } else { $null }
    } | Where-Object { $null -ne $_ } | Select-Object -Unique)
    $commonFirstBytes.Add([ordered]@{ pos = $pos; unique_values = [object[]]$vals; stable = ($vals.Count -le 1) }) | Out-Null
}

# ---------------------------------------------------------------------------
# Cross-file summary
# ---------------------------------------------------------------------------

$allSizes    = @($records | ForEach-Object { $_.size_bytes })
$allTrail    = @($records | ForEach-Object { $_.trailing_bytes_count })
$minTrail    = if ($allTrail.Count -gt 0) { ($allTrail | Measure-Object -Minimum).Minimum } else { 0 }
$maxTrail    = if ($allTrail.Count -gt 0) { ($allTrail | Measure-Object -Maximum).Maximum } else { 0 }

$cntMod2     = (@($records | Where-Object { $_.trailing_bytes_mod2 -eq 0 })).Count
$cntMod4     = (@($records | Where-Object { $_.trailing_bytes_mod4 -eq 0 })).Count
$cntMod8     = (@($records | Where-Object { $_.trailing_bytes_mod8 -eq 0 })).Count
$cntHasTrail = (@($records | Where-Object { $_.trailing_bytes_count -gt 0 })).Count

$avgEntropy  = if ($records.Count -gt 0) {
    [Math]::Round(($records | ForEach-Object { $_.entropy_estimate } | Measure-Object -Average).Average, 3)
} else { 0.0 }

$avgU16Ratio = if ($records.Count -gt 0) {
    [Math]::Round(($records | ForEach-Object { $_.plausible_string_index_ratio_u16 } | Measure-Object -Average).Average, 3)
} else { 0.0 }

$cntLpU16    = (@($records | Where-Object { $_.length_prefixed_u16_segments -gt 0 })).Count
$cntComp     = (@($records | Where-Object { $_.compression_candidates.Count -gt 0 })).Count

# Hypotheses
$hypotheses = [System.Collections.Generic.List[string]]::new()
$hypotheses.Add('LOTH_REQUIRES_TRAILING_BINARY_BODY') | Out-Null
if ($cntMod2 -gt ($records.Count * 0.5))   { $hypotheses.Add('HYPOTHESIS_TRAILER_U16_OR_BYTE_RECORDS')    | Out-Null }
if ($cntLpU16 -gt ($records.Count * 0.3))  { $hypotheses.Add('HYPOTHESIS_TRAILER_LENGTH_PREFIXED_STRINGS') | Out-Null }
if ($cntComp  -gt ($records.Count * 0.3))  { $hypotheses.Add('HYPOTHESIS_TRAILER_COMPRESSED_BLOCK_CANDIDATE') | Out-Null }
if ($avgU16Ratio -gt 0.3)                   { $hypotheses.Add('HYPOTHESIS_TRAILER_STRING_TABLE_REFERENCES')  | Out-Null }
if ($cntMod4 -lt ($records.Count / 2) -and $cntMod2 -lt ($records.Count / 2)) {
    $hypotheses.Add('HYPOTHESIS_TRAILER_MIXED_BINARY_RECORDS') | Out-Null
}
if ($hypotheses.Count -le 1)               { $hypotheses.Add('HYPOTHESIS_TRAILER_UNKNOWN')                  | Out-Null }

# Writer readiness
$writerReadiness = if ($hypotheses -contains 'HYPOTHESIS_TRAILER_LENGTH_PREFIXED_STRINGS' -or
                       $hypotheses -contains 'HYPOTHESIS_TRAILER_STRING_TABLE_REFERENCES') {
    'WRITER_MAYBE_DEFENSIBLE_AFTER_MINIMAL_BODY_MODEL'
} else {
    'WRITER_NOT_DEFENSIBLE'
}

$nextStep = if ($writerReadiness -eq 'WRITER_NOT_DEFENSIBLE') {
    'MAP-6X_DEEPEN_ANALYSIS'
} elseif ($writerReadiness -eq 'WRITER_MAYBE_DEFENSIBLE_AFTER_MINIMAL_BODY_MODEL') {
    'MAP-6X_MINIMAL_TRAILER_WRITER'
} else {
    'MAP-6X_REFERENCE_MODEL_COMPARISON'
}

$report = [ordered]@{
    schema                          = 'pzmapforge.build42-loth-trailing-byte-patterns.v0.1'
    reference_root                  = $ReferenceRoot
    reference_count                 = $records.Count
    focus_smallest_count            = $focusRecords.Count
    min_trailing_bytes              = $minTrail
    max_trailing_bytes              = $maxTrail
    count_with_trailing_body        = $cntHasTrail
    count_mod2_aligned              = $cntMod2
    count_mod4_aligned              = $cntMod4
    count_mod8_aligned              = $cntMod8
    avg_entropy                     = $avgEntropy
    avg_u16_string_index_ratio      = $avgU16Ratio
    count_with_lp_u16_segments      = $cntLpU16
    count_with_compression_candidate = $cntComp
    focus_file_first_byte_stability = [object[]]$commonFirstBytes.ToArray()
    likely_structure_hypotheses     = [string[]]$hypotheses.ToArray()
    writer_readiness                = $writerReadiness
    recommended_next_step           = $nextStep
    status_labels                   = [string[]]@(
        'BUILD42_LOTH_TRAILING_BYTE_PATTERNS_ANALYSED',
        'LOTH_REQUIRES_TRAILING_BINARY_BODY',
        'WRITER_RESEARCH_ONLY',
        'WRITER_NOT_CHANGED',
        'LOAD_TEST_NOT_PERFORMED',
        'PLAYABLE_EXPORT_CLAIM_ALLOWED=false'
    )
    focus_records                   = [object[]]($focusRecords | ForEach-Object {
        [ordered]@{
            file                     = $_.file
            trailing_bytes_count     = $_.trailing_bytes_count
            trailing_bytes_mod2      = $_.trailing_bytes_mod2
            trailing_bytes_mod4      = $_.trailing_bytes_mod4
            trailing_bytes_mod8      = $_.trailing_bytes_mod8
            entropy_estimate         = $_.entropy_estimate
            plausible_string_index_ratio_u16 = $_.plausible_string_index_ratio_u16
            length_prefixed_u16_segments = $_.length_prefixed_u16_segments
            compression_candidates   = $_.compression_candidates
            u16le_first_64           = $_.u16le_first_64
        }
    })
    all_records                     = [object[]]($records | ForEach-Object {
        [ordered]@{
            file                     = $_.file
            trailing_bytes_count     = $_.trailing_bytes_count
            trailing_bytes_mod2      = $_.trailing_bytes_mod2
            trailing_bytes_mod4      = $_.trailing_bytes_mod4
            entropy_estimate         = $_.entropy_estimate
            zero_byte_count          = $_.zero_byte_count
            plausible_string_index_ratio_u16 = $_.plausible_string_index_ratio_u16
            length_prefixed_u16_segments = $_.length_prefixed_u16_segments
        }
    })
}

$jsonPath = Join-Path $Output 'build42-loth-trailing-byte-patterns.json'
$mdPath   = Join-Path $Output 'build42-loth-trailing-byte-patterns.md'

$report | ConvertTo-Json -Depth 8 | Set-Content -Path $jsonPath -Encoding UTF8
Write-Output "Patterns JSON: $jsonPath"

# ---------------------------------------------------------------------------
# Markdown report
# ---------------------------------------------------------------------------

$fence = '```'
$hypsStr = $hypotheses -join ', '

$focusTable = ($focusRecords | ForEach-Object {
    "| $($_.file) | $($_.trailing_bytes_count) | $($_.trailing_bytes_mod4) | $($_.entropy_estimate) | $($_.plausible_string_index_ratio_u16) | $($_.length_prefixed_u16_segments) |"
}) -join "`n"

$allTable = ($records | ForEach-Object {
    "| $($_.file) | $($_.trailing_bytes_count) | $($_.trailing_bytes_mod2) | $($_.trailing_bytes_mod4) | $($_.entropy_estimate) |"
}) -join "`n"

$md = @"
# MAP-6W Build 42 LOTH Trailing Byte Patterns

${fence}text
BUILD42_LOTH_TRAILING_BYTE_PATTERNS_ANALYSED
LOTH_REQUIRES_TRAILING_BINARY_BODY
WRITER_RESEARCH_ONLY
WRITER_NOT_CHANGED
LOAD_TEST_NOT_PERFORMED
PLAYABLE_EXPORT_CLAIM_ALLOWED=false
${fence}

## Overall hypotheses

$hypsStr

## Writer readiness

$writerReadiness

## Recommended next step

$nextStep

## Summary

| Field | Value |
|---|---|
| Reference count | $($records.Count) |
| Min trailing bytes | $minTrail |
| Max trailing bytes | $maxTrail |
| Mod2-aligned | $cntMod2 / $($records.Count) |
| Mod4-aligned | $cntMod4 / $($records.Count) |
| Mod8-aligned | $cntMod8 / $($records.Count) |
| Avg entropy | $avgEntropy bits |
| Avg U16 string-index ratio | $avgU16Ratio |
| Files with LP-U16 segments | $cntLpU16 / $($records.Count) |
| Files with compression candidates | $cntComp / $($records.Count) |

## Focus files (smallest trailing)

| File | TrailingBytes | Mod4 | Entropy | U16IdxRatio | LP-U16 |
|---|---|---|---|---|---|
$focusTable

## All files

| File | TrailingBytes | Mod2 | Mod4 | Entropy |
|---|---|---|---|---|
$allTable

## Non-claims

- WRITER_NOT_CHANGED: byte-pattern analysis only.
- LOAD_TEST_NOT_PERFORMED: no PZ session.
- PLAYABLE_EXPORT_CLAIM_ALLOWED=false: binding.
"@

Set-Content -Path $mdPath -Value $md -Encoding ASCII
Write-Output "Patterns MD:   $mdPath"
Write-Output ""
Write-Output "likely_structure_hypotheses:     $hypsStr"
Write-Output "writer_readiness:                $writerReadiness"
Write-Output "recommended_next_step:           $nextStep"
Write-Output "count_mod4_aligned:              $cntMod4 / $($records.Count)"
Write-Output "count_mod2_aligned:              $cntMod2 / $($records.Count)"
Write-Output "avg_entropy:                     $avgEntropy"
Write-Output "avg_u16_string_index_ratio:      $avgU16Ratio"
Write-Output "BUILD42_LOTH_TRAILING_BYTE_PATTERNS_ANALYSED"
Write-Output "WRITER_NOT_CHANGED"
Write-Output "LOAD_TEST_NOT_PERFORMED"
Write-Output "PLAYABLE_EXPORT_CLAIM_ALLOWED=false"
Write-Output "Done."
