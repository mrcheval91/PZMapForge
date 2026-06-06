#Requires -Version 5.1
<#
.SYNOPSIS
    MAP-6Y: Inspects the fixed 1048-byte LOTH trailing block for simple cells.

    MAP-6X found that all 40 smallest Dru_map cells have EXACTLY 1048 trailing
    bytes (U32-aligned, 262 words, first 32 bytes stable). MAP-6Y determines
    whether the full 1048-byte block is constant, partially stable, or variable
    across reference cells.

    All inputs/outputs must be under .local/.
    Does NOT read PZ install folders. Does NOT write outside .local/.

    Writes:
      <Output>/build42-loth-fixed-1048-block.json
      <Output>/build42-loth-fixed-1048-block.md

.PARAMETER ReferenceRoot
    Path under .local/ containing reference Build 42 *.lotheader files.

.PARAMETER Output
    Path under .local/ for report output.

.PARAMETER MaxFiles
    Max reference files to inspect. Default: 80.

.PARAMETER OnlyTrailingSize
    Only analyse files whose trailing body is exactly this many bytes. Default: 1048.

.PARAMETER MaxBytesPerFile
    Safety cap per file. Default: 131072.

.EXAMPLE
    powershell -ExecutionPolicy Bypass -File .\scripts\analyze-build42-loth-fixed-1048-block.ps1 `
        -ReferenceRoot .\.local\reference-build42-map\Dru_map `
        -Output .\.local\map6y-loth-fixed-1048 `
        -MaxFiles 80 `
        -OnlyTrailingSize 1048
#>

param(
    [Parameter(Mandatory=$true)][string]$ReferenceRoot,
    [Parameter(Mandatory=$true)][string]$Output,
    [int]$MaxFiles         = 80,
    [int]$OnlyTrailingSize = 1048,
    [int]$MaxBytesPerFile  = 131072
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
    Write-Error "ReferenceRoot not found: $ReferenceRoot"
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
    return -1, -1
}

function compute-sha256 ([byte[]]$bytes) {
    $sha = [System.Security.Cryptography.SHA256]::Create()
    try {
        $hash = $sha.ComputeHash($bytes)
        return ($hash | ForEach-Object { $_.ToString('x2') }) -join ''
    } finally { $sha.Dispose() }
}

function compute-runs ([bool[]]$flags, [bool]$targetValue) {
    $runs  = [System.Collections.Generic.List[object]]::new()
    $start = -1
    for ($i = 0; $i -le $flags.Length; $i++) {
        $isTarget = ($i -lt $flags.Length -and $flags[$i] -eq $targetValue)
        if ($isTarget -and $start -lt 0) {
            $start = $i
        } elseif (-not $isTarget -and $start -ge 0) {
            $runs.Add([ordered]@{ start = $start; end = $i-1; length = ($i - $start) }) | Out-Null
            $start = -1
        }
    }
    [object[]]$runs.ToArray()
}

# ---------------------------------------------------------------------------
# Collect files
# ---------------------------------------------------------------------------

$allFiles = @(Get-ChildItem -LiteralPath $ReferenceRoot -Recurse -Filter '*.lotheader' -File -ErrorAction SilentlyContinue |
    Sort-Object Length |
    Select-Object -First $MaxFiles)

Write-Output "Found $($allFiles.Count) LOTH files. Analysing fixed $OnlyTrailingSize-byte block..."

$allRecords  = [System.Collections.Generic.List[object]]::new()
$allTrailers = [System.Collections.Generic.List[object]]::new()

foreach ($f in $allFiles) {
    $readLimit = [Math]::Min([long]$f.Length, $MaxBytesPerFile)
    $allBytes  = [byte[]]::new($readLimit)
    $fs        = [System.IO.File]::OpenRead($f.FullName)
    try { [void]$fs.Read($allBytes, 0, $readLimit) } finally { $fs.Dispose() }

    $cellX, $cellY = infer-cell $f.Name
    $ver  = if ($readLimit -ge 8)  { [long][BitConverter]::ToUInt32($allBytes, 4) } else { $null }
    $f8   = if ($readLimit -ge 12) { [long][BitConverter]::ToUInt32($allBytes, 8) } else { $null }

    # Parse ASCII string table from offset 12
    $asciiEnd = 12; $entryCount = 0; $curLen = 0
    for ($i = 12; $i -lt $allBytes.Length; $i++) {
        $b = $allBytes[$i]
        if ($b -eq 0x0A)                       { if ($curLen -gt 0) { $entryCount++; $curLen = 0 }; $asciiEnd = $i+1 }
        elseif ($b -ge 0x20 -and $b -le 0x7E) { $curLen++; $asciiEnd = $i+1 }
        else                                    { break }
    }

    $tStart      = $asciiEnd
    $tBytesTotal = [long]$f.Length - $tStart
    $tBytesRead  = [Math]::Max(0, $readLimit - $tStart)
    $hasFullTrailer = ($tBytesRead -ge $OnlyTrailingSize -and $tBytesTotal -eq $OnlyTrailingSize)

    $trailerRaw      = $null
    $sha256Trailer   = ''
    $zeroByteCount   = 0
    $nonzeroByteCount = 0
    $u32First32      = [long[]]@()
    $u32Last32       = [long[]]@()

    if ($hasFullTrailer -and $tStart + $OnlyTrailingSize -le $allBytes.Length) {
        $trailerRaw = $allBytes[$tStart..($tStart + $OnlyTrailingSize - 1)]
        $sha256Trailer = compute-sha256 $trailerRaw
        foreach ($bv in $trailerRaw) {
            if ($bv -eq 0) { $zeroByteCount++ } else { $nonzeroByteCount++ }
        }
        $lst32 = [System.Collections.Generic.List[long]]::new()
        for ($w = 0; $w -lt 32; $w++) {
            $p = $w * 4
            if ($p + 3 -lt $trailerRaw.Length) { $lst32.Add([long][BitConverter]::ToUInt32($trailerRaw, $p)) | Out-Null }
        }
        $u32First32 = [long[]]$lst32.ToArray()
        $wTotal = [int]($OnlyTrailingSize / 4)
        $llast32 = [System.Collections.Generic.List[long]]::new()
        for ($w = [Math]::Max(0, $wTotal - 32); $w -lt $wTotal; $w++) {
            $p = $w * 4
            if ($p + 3 -lt $trailerRaw.Length) { $llast32.Add([long][BitConverter]::ToUInt32($trailerRaw, $p)) | Out-Null }
        }
        $u32Last32 = [long[]]$llast32.ToArray()
    }

    $tFirst64 = bytes-hex $allBytes $tStart 64
    $lastOff  = [Math]::Max($tStart, [Math]::Min($allBytes.Length, $tStart + $OnlyTrailingSize) - 64)
    $tLast64  = bytes-hex $allBytes $lastOff 64

    $rec = [ordered]@{
        file                  = $f.Name
        cell_x                = $cellX
        cell_y                = $cellY
        field8                = $f8
        ascii_entry_count     = $entryCount
        trailing_start_offset = $tStart
        trailing_bytes_count  = $tBytesTotal
        has_full_trailer      = $hasFullTrailer
        sha256_trailer        = $sha256Trailer
        zero_byte_count       = $zeroByteCount
        nonzero_byte_count    = $nonzeroByteCount
        u32_word_count        = [int]([Math]::Floor($tBytesTotal / 4))
        first_64_trailer_hex  = $tFirst64
        last_64_trailer_hex   = $tLast64
        first_32_u32le_words  = $u32First32
        last_32_u32le_words   = $u32Last32
    }
    $allRecords.Add($rec)   | Out-Null
    $allTrailers.Add($trailerRaw) | Out-Null

    Write-Output "  $($f.Name): trailing=$tBytesTotal hasFullTrailer=$hasFullTrailer"
}

# ---------------------------------------------------------------------------
# Select files with exactly OnlyTrailingSize trailing bytes
# ---------------------------------------------------------------------------

$selectedIndices = [System.Collections.Generic.List[int]]::new()
for ($i = 0; $i -lt $allRecords.Count; $i++) {
    $rec = $allRecords[$i]
    if ($rec.trailing_bytes_count -eq $OnlyTrailingSize -and $rec.has_full_trailer -eq $true) {
        $selectedIndices.Add($i) | Out-Null
    }
}

$selectedFileCount = $selectedIndices.Count
Write-Output "Selected $selectedFileCount files with exactly $OnlyTrailingSize trailing bytes."

# ---------------------------------------------------------------------------
# Cross-file stability analysis
# ---------------------------------------------------------------------------

$uniqueSha256s      = [System.Collections.Generic.HashSet[string]]::new()
$canonicalTrailer   = $null
$stableFlags        = [bool[]]::new($OnlyTrailingSize)
$allZeroPositions      = 0
$allNonzeroStablePos   = 0

foreach ($idx in $selectedIndices) {
    $sha = [string]$allRecords[$idx].sha256_trailer
    if ($sha) { [void]$uniqueSha256s.Add($sha) }
}

if ($selectedFileCount -ge 1) {
    $canonicalTrailer = [byte[]]$allTrailers[$selectedIndices[0]]

    for ($pos = 0; $pos -lt $OnlyTrailingSize; $pos++) {
        $vals = [System.Collections.Generic.HashSet[int]]::new()
        foreach ($idx in $selectedIndices) {
            $t = $allTrailers[$idx]
            if ($null -ne $t) {
                $tb = [byte[]]$t
                if ($pos -lt $tb.Length) { [void]$vals.Add([int]$tb[$pos]) }
            }
        }
        $stableFlags[$pos] = ($vals.Count -le 1)
    }

    if ($null -ne $canonicalTrailer) {
        for ($pos = 0; $pos -lt $canonicalTrailer.Length; $pos++) {
            if ($stableFlags[$pos]) {
                if ($canonicalTrailer[$pos] -eq 0) { $allZeroPositions++ }
                else { $allNonzeroStablePos++ }
            }
        }
    }
} else {
    for ($pos = 0; $pos -lt $OnlyTrailingSize; $pos++) { $stableFlags[$pos] = $false }
}

$stableByteCount   = 0; $variableByteCount = 0
foreach ($f in $stableFlags) { if ($f) { $stableByteCount++ } else { $variableByteCount++ } }

$_stableRaw   = compute-runs $stableFlags $true
$_variableRaw = compute-runs $stableFlags $false
[object[]]$stableRanges   = if ($null -eq $_stableRaw)   { [object[]]@() } else { [object[]]@($_stableRaw) }
[object[]]$variableRanges = if ($null -eq $_variableRaw) { [object[]]@() } else { [object[]]@($_variableRaw) }

$stablePrefixLength = 0
if ($stableRanges.Count -gt 0 -and ([int]$stableRanges[0]['start']) -eq 0) {
    $stablePrefixLength = [int]$stableRanges[0]['length']
}

$stableSuffixLength = 0
if ($stableRanges.Count -gt 0 -and ([int]$stableRanges[$stableRanges.Count - 1]['end']) -eq ($OnlyTrailingSize - 1)) {
    $stableSuffixLength = [int]$stableRanges[$stableRanges.Count - 1]['length']
}

$firstVariableOffset = -1
$lastVariableOffset  = -1
for ($pos = 0; $pos -lt $OnlyTrailingSize; $pos++) {
    if (-not $stableFlags[$pos]) {
        if ($firstVariableOffset -lt 0) { $firstVariableOffset = $pos }
        $lastVariableOffset = $pos
    }
}

# Top-10 variable position summary
$varTopSummary = [System.Collections.Generic.List[object]]::new()
$varSeen = 0
for ($pos = 0; $pos -lt $OnlyTrailingSize -and $varSeen -lt 10; $pos++) {
    if (-not $stableFlags[$pos]) {
        $uvals = [System.Collections.Generic.HashSet[int]]::new()
        foreach ($idx in $selectedIndices) {
            $t = $allTrailers[$idx]
            if ($null -ne $t) {
                $tb = [byte[]]$t
                if ($pos -lt $tb.Length) { [void]$uvals.Add([int]$tb[$pos]) }
            }
        }
        $varTopSummary.Add([ordered]@{ pos = $pos; unique_values = [int[]]($uvals | Sort-Object) }) | Out-Null
        $varSeen++
    }
}

# U32 word stability
$wordTotal = [int]($OnlyTrailingSize / 4)
$wordStableFlags = [bool[]]::new($wordTotal)
$stableWordCount = 0; $variableWordCount = 0
for ($w = 0; $w -lt $wordTotal; $w++) {
    $wStable = $true
    for ($b = 0; $b -lt 4; $b++) {
        if (-not $stableFlags[$w*4 + $b]) { $wStable = $false; break }
    }
    $wordStableFlags[$w] = $wStable
    if ($wStable) { $stableWordCount++ } else { $variableWordCount++ }
}
$_swRaw  = compute-runs $wordStableFlags $true
$_vwRaw  = compute-runs $wordStableFlags $false
[object[]]$stableWordRanges   = if ($null -eq $_swRaw) { [object[]]@() } else { [object[]]@($_swRaw) }
[object[]]$variableWordRanges = if ($null -eq $_vwRaw) { [object[]]@() } else { [object[]]@($_vwRaw) }

# Coordinate correlation
$coordCorrelationFound = $false
foreach ($idx in $selectedIndices) {
    $rec = $allRecords[$idx]
    $cx  = [int]$rec.cell_x
    $cy  = [int]$rec.cell_y
    if ($cx -lt 0 -and $cy -lt 0) { continue }
    $t = $allTrailers[$idx]
    if ($null -eq $t) { continue }
    $tb = [byte[]]$t
    for ($pos = 0; $pos -lt $OnlyTrailingSize; $pos++) {
        if (-not $stableFlags[$pos] -and $pos -lt $tb.Length) {
            $bv = [int]$tb[$pos]
            if ($bv -eq $cx -or $bv -eq $cy) { $coordCorrelationFound = $true; break }
        }
    }
    if ($coordCorrelationFound) { break }
}

# ---------------------------------------------------------------------------
# Hypotheses
# ---------------------------------------------------------------------------

$allIdentical  = ($uniqueSha256s.Count -le 1 -and $selectedFileCount -ge 2)
$isEntirelyZero = ($selectedFileCount -ge 1 -and $allZeroPositions -eq $OnlyTrailingSize -and $allNonzeroStablePos -eq 0 -and $variableByteCount -eq 0)

$hyps = [System.Collections.Generic.List[string]]::new()

if ($selectedFileCount -lt 2) {
    $hyps.Add('HYPOTHESIS_1048_BLOCK_NOT_ENOUGH_REFERENCE_FILES') | Out-Null
} elseif ($allIdentical) {
    $hyps.Add('HYPOTHESIS_1048_BLOCK_FULLY_CONSTANT') | Out-Null
    if ($isEntirelyZero) {
        $hyps.Add('HYPOTHESIS_1048_BLOCK_STABLE_HEADER_ZERO_BODY') | Out-Null
    }
} else {
    if ($stablePrefixLength -ge 32) {
        $hyps.Add('HYPOTHESIS_1048_BLOCK_STABLE_PREFIX_VARIABLE_BODY') | Out-Null
    }
    if ($allZeroPositions -gt 0 -and $stablePrefixLength -ge 8 -and $allZeroPositions -ge ($OnlyTrailingSize - $stablePrefixLength - $variableByteCount)) {
        $hyps.Add('HYPOTHESIS_1048_BLOCK_STABLE_HEADER_ZERO_BODY') | Out-Null
    }
    if ($coordCorrelationFound) {
        $hyps.Add('HYPOTHESIS_1048_BLOCK_CELL_COORDINATE_FIELDS') | Out-Null
    }
    if ($hyps.Count -eq 0) {
        $hyps.Add('HYPOTHESIS_1048_BLOCK_VARIABLE_UNKNOWN') | Out-Null
    }
}

# ---------------------------------------------------------------------------
# Writer readiness
# ---------------------------------------------------------------------------

$writerReadiness = 'WRITER_NOT_DEFENSIBLE'
$recommendedNext = 'MAP-6Z_DEEPEN_FIXED_BLOCK_FIELDS'

if ($hyps.Contains('HYPOTHESIS_1048_BLOCK_FULLY_CONSTANT')) {
    if ($isEntirelyZero) {
        $writerReadiness = 'WRITER_MAYBE_DEFENSIBLE_WITH_ZERO_1048_BLOCK'
        $recommendedNext = 'MAP-6Z_LOTH_V3_MINIMAL_1048_ZERO_BLOCK'
    } else {
        $writerReadiness = 'WRITER_MAYBE_DEFENSIBLE_WITH_STABLE_LITERAL_1048_BLOCK'
        $recommendedNext = 'MAP-6Z_LOTH_V3_STABLE_LITERAL_BLOCK'
    }
} elseif ($hyps.Contains('HYPOTHESIS_1048_BLOCK_STABLE_HEADER_ZERO_BODY') -and $variableByteCount -le 4) {
    $writerReadiness = 'WRITER_MAYBE_DEFENSIBLE_WITH_STABLE_PREFIX_ZERO_REMAINDER'
    $recommendedNext = 'MAP-6Z_LOTH_V3_STABLE_PREFIX_ZERO_REMAINDER'
}

$canonicalSha256 = if ($null -ne $canonicalTrailer) { compute-sha256 $canonicalTrailer } else { '' }

$statusLabels = [string[]]@(
    'BUILD42_LOTH_FIXED_1048_BLOCK_ANALYSED',
    'LOTH_TRAILING_BODY_FIXED_SIZE_FOR_SIMPLE_CELLS',
    'WRITER_RESEARCH_ONLY',
    'WRITER_NOT_CHANGED',
    'LOAD_TEST_NOT_PERFORMED',
    'PLAYABLE_EXPORT_CLAIM_ALLOWED=false'
)

# ---------------------------------------------------------------------------
# Build report
# ---------------------------------------------------------------------------

$report = [ordered]@{
    schema                             = 'pzmapforge.build42-loth-fixed-1048-block.v0.1'
    reference_root                     = $ReferenceRoot
    reference_file_count               = $allRecords.Count
    selected_file_count                = $selectedFileCount
    only_trailing_size                 = $OnlyTrailingSize
    unique_trailer_sha256_count        = $uniqueSha256s.Count
    all_1048_blocks_identical          = $allIdentical
    canonical_trailer_sha256           = $canonicalSha256
    stable_byte_count                  = $stableByteCount
    variable_byte_count                = $variableByteCount
    stable_byte_ranges                 = [object[]]$stableRanges
    variable_byte_ranges               = [object[]]$variableRanges
    first_variable_offset              = $firstVariableOffset
    last_variable_offset               = $lastVariableOffset
    stable_prefix_length               = $stablePrefixLength
    stable_suffix_length               = $stableSuffixLength
    stable_u32_word_count              = $stableWordCount
    variable_u32_word_count            = $variableWordCount
    stable_u32_word_ranges             = [object[]]$stableWordRanges
    variable_u32_word_ranges           = [object[]]$variableWordRanges
    all_zero_positions_count           = $allZeroPositions
    all_nonzero_stable_positions_count = $allNonzeroStablePos
    variable_positions_top_summary     = [object[]]$varTopSummary.ToArray()
    block_entirely_zero                = $isEntirelyZero
    variable_bytes_confined_to_small_range = ($variableByteCount -gt 0 -and $variableRanges.Count -le 3 -and ($lastVariableOffset - $firstVariableOffset) -le 32)
    coordinate_correlation_found       = $coordCorrelationFound
    hypotheses                         = [string[]]$hyps.ToArray()
    writer_readiness                   = $writerReadiness
    recommended_next_step              = $recommendedNext
    status_labels                      = $statusLabels
    file_records                       = [object[]]($allRecords.ToArray())
}

$jsonPath = Join-Path $Output 'build42-loth-fixed-1048-block.json'
$mdPath   = Join-Path $Output 'build42-loth-fixed-1048-block.md'

$report | ConvertTo-Json -Depth 8 | Set-Content -Path $jsonPath -Encoding UTF8
Write-Output "JSON: $jsonPath"

# ---------------------------------------------------------------------------
# Markdown
# ---------------------------------------------------------------------------

$fence   = '```'
$hypsStr = ($hyps -join ', ')

# Compute range strings directly from $stableFlags (avoids PS array-unrolling edge cases)
$mdStableParts = [System.Collections.Generic.List[string]]::new()
$mdVarParts    = [System.Collections.Generic.List[string]]::new()
$mdRs = -1
for ($pos = 0; $pos -le $OnlyTrailingSize; $pos++) {
    $isSt = ($pos -lt $OnlyTrailingSize -and $stableFlags[$pos] -eq $true)
    if ($isSt -and $mdRs -lt 0) { $mdRs = $pos }
    elseif (-not $isSt -and $mdRs -ge 0) {
        $mdStableParts.Add("[$mdRs-$($pos-1) len=$($pos-$mdRs)]") | Out-Null
        $mdRs = -1
    }
}
$mdRs = -1
for ($pos = 0; $pos -le $OnlyTrailingSize; $pos++) {
    $isVr = ($pos -lt $OnlyTrailingSize -and $stableFlags[$pos] -eq $false)
    if ($isVr -and $mdRs -lt 0) { $mdRs = $pos }
    elseif (-not $isVr -and $mdRs -ge 0) {
        $mdVarParts.Add("[$mdRs-$($pos-1) len=$($pos-$mdRs)]") | Out-Null
        $mdRs = -1
    }
}
$stableRangesStr = if ($mdStableParts.Count -gt 0) { $mdStableParts -join ', ' } else { 'none' }
$varRangesStr    = if ($mdVarParts.Count -gt 0)    { $mdVarParts -join ', '    } else { 'none' }

$md = @"
# MAP-6Y Build 42 LOTH Fixed 1048-Byte Block Analysis

${fence}text
BUILD42_LOTH_FIXED_1048_BLOCK_ANALYSED
LOTH_TRAILING_BODY_FIXED_SIZE_FOR_SIMPLE_CELLS
WRITER_RESEARCH_ONLY
WRITER_NOT_CHANGED
LOAD_TEST_NOT_PERFORMED
PLAYABLE_EXPORT_CLAIM_ALLOWED=false
${fence}

## Summary

| Field | Value |
|---|---|
| Reference files inspected | $($allRecords.Count) |
| Selected files (trailing=$OnlyTrailingSize bytes) | $selectedFileCount |
| Unique trailer SHA-256 hashes | $($uniqueSha256s.Count) |
| All $OnlyTrailingSize-byte blocks identical | $allIdentical |
| Stable byte count | $stableByteCount / $OnlyTrailingSize |
| Variable byte count | $variableByteCount / $OnlyTrailingSize |
| Stable prefix length | $stablePrefixLength bytes |
| Stable suffix length | $stableSuffixLength bytes |
| First variable offset | $firstVariableOffset |
| Last variable offset | $lastVariableOffset |
| Stable U32 word count | $stableWordCount / $wordTotal |
| Variable U32 word count | $variableWordCount / $wordTotal |
| All-zero stable positions | $allZeroPositions |
| All-nonzero stable positions | $allNonzeroStablePos |
| Block entirely zero | $isEntirelyZero |
| Coordinate correlation found | $coordCorrelationFound |

## Stable byte ranges

$stableRangesStr

## Variable byte ranges

$varRangesStr

## Hypotheses

$hypsStr

## Writer readiness

$writerReadiness

## Recommended next step

$recommendedNext

## Non-claims

- WRITER_NOT_CHANGED: no writer modification in MAP-6Y.
- LOAD_TEST_NOT_PERFORMED: no PZ session.
- PLAYABLE_EXPORT_CLAIM_ALLOWED=false: binding.
"@

Set-Content -Path $mdPath -Value $md -Encoding ASCII
Write-Output "MD:   $mdPath"
Write-Output ""
Write-Output "selected_file_count:         $selectedFileCount"
Write-Output "unique_trailer_sha256_count:  $($uniqueSha256s.Count)"
Write-Output "all_1048_blocks_identical:   $allIdentical"
Write-Output "stable_byte_count:           $stableByteCount"
Write-Output "variable_byte_count:         $variableByteCount"
Write-Output "stable_prefix_length:        $stablePrefixLength"
Write-Output "stable_suffix_length:        $stableSuffixLength"
Write-Output "writer_readiness:            $writerReadiness"
Write-Output "hypotheses:                  $hypsStr"
Write-Output "BUILD42_LOTH_FIXED_1048_BLOCK_ANALYSED"
Write-Output "WRITER_NOT_CHANGED"
Write-Output "LOAD_TEST_NOT_PERFORMED"
Write-Output "PLAYABLE_EXPORT_CLAIM_ALLOWED=false"
Write-Output "Done."
