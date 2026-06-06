#Requires -Version 5.1
<#
.SYNOPSIS
    MAP-6X: Tests per-entry record hypotheses for the LOTH trailing body.

    MAP-6W showed avg entropy 2.657 and mod2=10/20 -- consistent with packed
    small-integer records. MAP-6X tries fixed record sizes (4-16 bytes) to see
    which best explains the trailing body as:
      header + N*record_size + footer
    ...with the smallest overhead/remainder.

    All inputs/outputs must be under .local/.
    Does NOT read PZ install folders. Does NOT write outside .local/.

    Writes:
      <Output>/build42-loth-per-entry-record-model.json
      <Output>/build42-loth-per-entry-record-model.md

.PARAMETER ReferenceRoot
    Path under .local/ containing reference Build 42 *.lotheader files.

.PARAMETER Output
    Path under .local/ for report output.

.PARAMETER MaxFiles
    Max reference files to inspect. Default: 40.

.PARAMETER FocusSmallestCount
    Number of smallest trailing-body files to analyse in depth. Default: 8.

.PARAMETER RecordSizes
    Comma-separated list of candidate record sizes to test. Default: "4,5,6,7,8,9,10,12,16".

.PARAMETER MaxBytesPerFile
    Safety cap. Default: 131072.

.EXAMPLE
    powershell -ExecutionPolicy Bypass -File scripts\analyze-build42-loth-per-entry-record-model.ps1 `
        -ReferenceRoot .local\reference-build42-map\Dru_map `
        -Output .local\map6x-loth-per-entry-model
#>

param(
    [Parameter(Mandatory=$true)][string]$ReferenceRoot,
    [Parameter(Mandatory=$true)][string]$Output,
    [int]$MaxFiles          = 40,
    [int]$FocusSmallestCount = 8,
    [string]$RecordSizes    = '4,5,6,7,8,9,10,12,16',
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
    Write-Error "ReferenceRoot not found: $ReferenceRoot"
    exit 1
}

New-Item -ItemType Directory -Force -Path $Output | Out-Null

$candidateRecordSizes = @($RecordSizes -split ',' | ForEach-Object { [int]$_.Trim() })

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

function record-samples ([byte[]]$bytes, [int]$startOff, [int]$recordSize, [int]$maxRecs) {
    $samples = [System.Collections.Generic.List[object]]::new()
    for ($i = 0; $i -lt $maxRecs; $i++) {
        $pos = $startOff + $i * $recordSize
        if ($pos + $recordSize - 1 -ge $bytes.Length) { break }
        $hex    = bytes-hex $bytes $pos $recordSize
        $allZero = $true
        for ($j = $pos; $j -lt $pos + $recordSize; $j++) { if ($bytes[$j] -ne 0) { $allZero = $false; break } }
        $samples.Add([ordered]@{ index = $i; offset = $pos; hex = $hex; all_zero = $allZero }) | Out-Null
    }
    [object[]]$samples.ToArray()
}

# ---------------------------------------------------------------------------
# Collect files
# ---------------------------------------------------------------------------

$allFiles = @(Get-ChildItem -LiteralPath $ReferenceRoot -Recurse -Filter '*.lotheader' -File -ErrorAction SilentlyContinue |
    Sort-Object Length |
    Select-Object -First $MaxFiles)

Write-Output "Found $($allFiles.Count) LOTH files. Testing per-entry record models..."

$records = [System.Collections.Generic.List[object]]::new()

foreach ($f in $allFiles) {
    $readLimit = [Math]::Min($f.Length, $MaxBytesPerFile)
    $allBytes  = [byte[]]::new($readLimit)
    $fs        = [System.IO.File]::OpenRead($f.FullName)
    try { [void]$fs.Read($allBytes, 0, $readLimit) } finally { $fs.Dispose() }

    $cellX, $cellY = infer-cell $f.Name
    $ver   = if ($readLimit -ge 8)  { [long][BitConverter]::ToUInt32($allBytes, 4) } else { $null }
    $f8    = if ($readLimit -ge 12) { [long][BitConverter]::ToUInt32($allBytes, 8) } else { $null }

    # Parse ASCII region
    $asciiEnd   = 12; $entryCount = 0; $curLen = 0
    for ($i = 12; $i -lt $allBytes.Length; $i++) {
        $b = $allBytes[$i]
        if ($b -eq 0x0A)                       { if ($curLen -gt 0) { $entryCount++; $curLen = 0 }; $asciiEnd = $i+1 }
        elseif ($b -ge 0x20 -and $b -le 0x7E) { $curLen++; $asciiEnd = $i+1 }
        else                                    { break }
    }

    $tStart = $asciiEnd
    $tBytes = $f.Length - $tStart
    $tMod2  = $tBytes % 2
    $tMod4  = $tBytes % 4
    $tMod8  = $tBytes % 8

    $n      = if ($entryCount -gt 0) { $entryCount } else { 1 }
    $bpe    = if ($entryCount -gt 0) { [Math]::Round($tBytes / $entryCount, 3) } else { 0.0 }

    # Trailer bytes available
    $tFirst32  = bytes-hex $allBytes $tStart 32
    $tFirst64  = bytes-hex $allBytes $tStart 64
    $tFirst128 = bytes-hex $allBytes $tStart 128
    $tLast32   = bytes-hex $allBytes ([Math]::Max($tStart, $allBytes.Length-32)) 32
    $tLast64   = bytes-hex $allBytes ([Math]::Max($tStart, $allBytes.Length-64)) 64

    # Per-record size scoring
    $modelsBySize = [System.Collections.Generic.List[object]]::new()
    $bestAbsRem   = [int]::MaxValue
    $bestAbsSize  = -1
    $bestOvrRatio = 1.0
    $bestOvrSize  = -1

    foreach ($rs in $candidateRecordSizes) {
        $totalRec = $n * $rs
        $rem      = $tBytes - $totalRec
        $absRem   = [Math]::Abs($rem)
        $feasible = $totalRec -le $tBytes
        $ovrRatio = if ($tBytes -gt 0) { [Math]::Round([Math]::Abs($rem) / $tBytes, 4) } else { 1.0 }

        $model = if ($rem -eq 0)               { 'exact_match' }
                 elseif ($rem -gt 0 -and $rem -le 256) { 'records_plus_footer' }
                 elseif ($rem -lt 0 -and [Math]::Abs($rem) -le 256) { 'header_plus_records' }
                 elseif ($rem -gt 0)            { 'header_plus_records_plus_footer' }
                 else                           { 'too_large' }

        if ($feasible -and $absRem -lt $bestAbsRem) { $bestAbsRem = $absRem; $bestAbsSize = $rs }
        if ($feasible -and $ovrRatio -lt $bestOvrRatio) { $bestOvrRatio = $ovrRatio; $bestOvrSize = $rs }

        $modelsBySize.Add([ordered]@{
            record_size          = $rs
            total_record_bytes   = $totalRec
            remainder            = $rem
            abs_remainder        = $absRem
            feasible             = $feasible
            overhead_ratio       = $ovrRatio
            candidate_model      = $model
        }) | Out-Null
    }

    # Per-record sampling for 5,6,7,8
    $sampledRecs = [ordered]@{}
    foreach ($rs in @(5,6,7,8)) {
        if (($n * $rs) -le $tBytes) {
            $samps = @(record-samples $allBytes $tStart $rs 8)
            $sampledRecs["rs$rs"] = $samps
        }
    }

    $rs6 = $modelsBySize | Where-Object { $_.record_size -eq 6 } | Select-Object -First 1

    $rec = [ordered]@{
        file                          = $f.Name
        cell_x                        = $cellX
        cell_y                        = $cellY
        size_bytes                    = $f.Length
        field8_u32le                  = $f8
        ascii_entry_count             = $entryCount
        trailing_start_offset         = $tStart
        trailing_bytes_count          = $tBytes
        trailing_bytes_mod2           = $tMod2
        trailing_bytes_mod4           = $tMod4
        trailing_bytes_mod8           = $tMod8
        bytes_per_entry               = $bpe
        best_record_size_by_abs_rem   = $bestAbsSize
        best_record_size_by_ovr_ratio = $bestOvrSize
        record_size_6_feasible        = ($null -ne $rs6 -and $rs6.feasible -eq $true)
        record_size_6_remainder       = if ($null -ne $rs6) { $rs6.remainder } else { $null }
        record_size_6_overhead_ratio  = if ($null -ne $rs6) { $rs6.overhead_ratio } else { $null }
        first_32_trailer_hex          = $tFirst32
        first_64_trailer_hex          = $tFirst64
        first_128_trailer_hex         = $tFirst128
        last_32_trailer_hex           = $tLast32
        last_64_trailer_hex           = $tLast64
        record_model_scores           = [object[]]$modelsBySize.ToArray()
        sampled_records               = $sampledRecs
    }
    $records.Add($rec) | Out-Null

    $bStr = if ($bestAbsSize -ge 0) { "$bestAbsSize" } else { '?' }
    Write-Output "  $($f.Name): trail=$tBytes n=$entryCount bpe=$bpe bestAbs=$bStr rs6rem=$($rs6.remainder)"
}

# ---------------------------------------------------------------------------
# Focus on smallest files
# ---------------------------------------------------------------------------

$sorted      = @($records | Sort-Object { $_.trailing_bytes_count })
$focus       = @($sorted | Select-Object -First $FocusSmallestCount)

# Stable prefix byte positions across focus files
$stablePrefix = [System.Collections.Generic.List[object]]::new()
for ($pos = 0; $pos -lt 32; $pos++) {
    $vals = @($focus | ForEach-Object {
        $hex = $_.first_64_trailer_hex
        if ($hex.Length -ge ($pos+1)*2) { [System.Convert]::ToInt32($hex.Substring($pos*2, 2), 16) } else { $null }
    } | Where-Object { $null -ne $_ } | Select-Object -Unique)
    $stablePrefix.Add([ordered]@{ pos = $pos; unique_values = [object[]]$vals; stable = ($vals.Count -le 1) }) | Out-Null
}

# Record-size scoreboard
$scoreboard = [System.Collections.Generic.List[object]]::new()
foreach ($rs in $candidateRecordSizes) {
    $feasibleRecs = @($records | Where-Object {
        $m = @($_.record_model_scores | Where-Object { $_.record_size -eq $rs })
        $m.Count -gt 0 -and $m[0].feasible -eq $true
    })
    $absRems = @($feasibleRecs | ForEach-Object {
        $m = @($_.record_model_scores | Where-Object { $_.record_size -eq $rs })
        if ($m.Count -gt 0) { [int]$m[0].abs_remainder } else { $null }
    } | Where-Object { $null -ne $_ })
    $avgAbs = if ($absRems.Count -gt 0) { [Math]::Round(($absRems | Measure-Object -Average).Average, 1) } else { $null }
    $medAbs = if ($absRems.Count -gt 0) {
        $sorted2 = @($absRems | Sort-Object)
        $sorted2[[int]($sorted2.Count/2)]
    } else { $null }
    $ovrRatios = @($feasibleRecs | ForEach-Object {
        $m = @($_.record_model_scores | Where-Object { $_.record_size -eq $rs })
        if ($m.Count -gt 0) { [double]$m[0].overhead_ratio } else { $null }
    } | Where-Object { $null -ne $_ })
    $avgOvr = if ($ovrRatios.Count -gt 0) { [Math]::Round(($ovrRatios | Measure-Object -Average).Average, 4) } else { $null }

    $scoreboard.Add([ordered]@{
        record_size         = $rs
        feasible_count      = $feasibleRecs.Count
        avg_abs_remainder   = $avgAbs
        median_abs_remainder = $medAbs
        avg_overhead_ratio  = $avgOvr
    }) | Out-Null
}

# Most plausible: lowest avg_overhead_ratio among feasible
$plausible = @($scoreboard | Where-Object { $null -ne $_.avg_overhead_ratio } |
    Sort-Object avg_overhead_ratio | Select-Object -First 3 |
    ForEach-Object { $_.record_size })

# Hypotheses
$hyps = [System.Collections.Generic.List[string]]::new()
$hyps.Add('LOTH_REQUIRES_TRAILING_BINARY_BODY') | Out-Null

foreach ($rs in $plausible) {
    $tag = "HYPOTHESIS_PER_ENTRY_RECORDS_${rs}_BYTES"
    $hyps.Add($tag) | Out-Null
}

$focusMod4  = (@($focus | Where-Object { $_.trailing_bytes_mod4 -eq 0 })).Count
$focusMod2  = (@($focus | Where-Object { $_.trailing_bytes_mod2 -eq 0 })).Count
$stablePos  = (@($stablePrefix | Where-Object { $_.stable -eq $true })).Count

if ($stablePos -ge 4) { $hyps.Add('HYPOTHESIS_FIXED_HEADER_PLUS_RECORDS') | Out-Null }

# Check if records+footer or header+records+footer models dominate
$footerCount  = (@($records | Where-Object {
    $best = $_.best_record_size_by_abs_rem
    if ($null -eq $best -or $best -lt 0) { return $false }
    $m = @($_.record_model_scores | Where-Object { $_.record_size -eq $best })
    $m.Count -gt 0 -and ($m[0].candidate_model -eq 'records_plus_footer' -or $m[0].candidate_model -eq 'header_plus_records_plus_footer')
})).Count
if ($footerCount -gt ($records.Count * 0.5)) { $hyps.Add('HYPOTHESIS_RECORDS_PLUS_FOOTER') | Out-Null }

if ($hyps.Count -le 1) { $hyps.Add('HYPOTHESIS_VARIABLE_PACKED_RECORDS') | Out-Null }

# Writer readiness
$topRs = if ($plausible.Count -gt 0) { $plausible[0] } else { -1 }
$topScore = @($scoreboard | Where-Object { $_.record_size -eq $topRs } | Select-Object -First 1)
$topOvr   = if ($topScore.Count -gt 0 -and $null -ne $topScore[0].avg_overhead_ratio) { [double]$topScore[0].avg_overhead_ratio } else { 1.0 }

$writerReadiness = if ($topOvr -lt 0.05) {
    'WRITER_MAYBE_DEFENSIBLE_AFTER_MODEL_CONFIRMATION'
} else {
    'WRITER_NOT_DEFENSIBLE'
}

$nextStep = if ($writerReadiness -eq 'WRITER_NOT_DEFENSIBLE') {
    'MAP-6Y_DEEPEN_PER_RECORD_DECODING'
} elseif ($writerReadiness -eq 'WRITER_MAYBE_DEFENSIBLE_AFTER_MODEL_CONFIRMATION') {
    'MAP-6Y_MINIMAL_TRAILER_MODEL'
} else {
    'MAP-6Y_LOTH_V3_WRITER'
}

$smallestRec = $focus | Select-Object -First 1

$report = [ordered]@{
    schema                        = 'pzmapforge.build42-loth-per-entry-record-model.v0.1'
    reference_root                = $ReferenceRoot
    reference_count               = $records.Count
    focus_file_count              = $focus.Count
    candidate_record_sizes        = [int[]]$candidateRecordSizes
    smallest_file                 = if ($null -ne $smallestRec) { $smallestRec.file } else { '' }
    smallest_field8               = if ($null -ne $smallestRec) { $smallestRec.field8_u32le } else { $null }
    smallest_trailing_bytes       = if ($null -ne $smallestRec) { $smallestRec.trailing_bytes_count } else { 0 }
    smallest_bytes_per_entry      = if ($null -ne $smallestRec) { $smallestRec.bytes_per_entry } else { 0.0 }
    most_plausible_record_sizes   = [int[]]$plausible
    count_where_rs6_feasible      = (@($records | Where-Object { $_.record_size_6_feasible -eq $true })).Count
    avg_rs6_overhead_ratio        = if ($records.Count -gt 0) {
        $vals = @($records | Where-Object { $null -ne $_.record_size_6_overhead_ratio } |
            ForEach-Object { [double]$_.record_size_6_overhead_ratio })
        if ($vals.Count -gt 0) { [Math]::Round(($vals | Measure-Object -Average).Average, 4) } else { $null }
    } else { $null }
    stable_prefix_byte_count      = $stablePos
    record_size_scoreboard        = [object[]]$scoreboard.ToArray()
    likely_model_hypotheses       = [string[]]$hyps.ToArray()
    writer_readiness              = $writerReadiness
    recommended_next_step         = $nextStep
    stable_prefix_positions       = [object[]]$stablePrefix.ToArray()
    status_labels                 = [string[]]@(
        'BUILD42_LOTH_PER_ENTRY_RECORD_MODEL_ANALYSED',
        'LOTH_REQUIRES_TRAILING_BINARY_BODY',
        'WRITER_RESEARCH_ONLY',
        'WRITER_NOT_CHANGED',
        'LOAD_TEST_NOT_PERFORMED',
        'PLAYABLE_EXPORT_CLAIM_ALLOWED=false'
    )
    focus_records                 = [object[]]($focus | ForEach-Object {
        [ordered]@{
            file                     = $_.file
            field8_u32le             = $_.field8_u32le
            ascii_entry_count        = $_.ascii_entry_count
            trailing_bytes_count     = $_.trailing_bytes_count
            trailing_bytes_mod2      = $_.trailing_bytes_mod2
            trailing_bytes_mod4      = $_.trailing_bytes_mod4
            bytes_per_entry          = $_.bytes_per_entry
            best_record_size_by_abs_rem = $_.best_record_size_by_abs_rem
            record_size_6_feasible   = $_.record_size_6_feasible
            record_size_6_remainder  = $_.record_size_6_remainder
            record_size_6_overhead_ratio = $_.record_size_6_overhead_ratio
            first_64_trailer_hex     = $_.first_64_trailer_hex
            sampled_records          = $_.sampled_records
        }
    })
    all_records_summary           = [object[]]($records | ForEach-Object {
        [ordered]@{
            file                     = $_.file
            trailing_bytes_count     = $_.trailing_bytes_count
            bytes_per_entry          = $_.bytes_per_entry
            best_record_size_by_abs_rem = $_.best_record_size_by_abs_rem
            record_size_6_overhead_ratio = $_.record_size_6_overhead_ratio
        }
    })
}

$jsonPath = Join-Path $Output 'build42-loth-per-entry-record-model.json'
$mdPath   = Join-Path $Output 'build42-loth-per-entry-record-model.md'

$report | ConvertTo-Json -Depth 9 | Set-Content -Path $jsonPath -Encoding UTF8
Write-Output "Model JSON: $jsonPath"

# ---------------------------------------------------------------------------
# Markdown
# ---------------------------------------------------------------------------

$fence   = '```'
$hypsStr = $hyps -join ', '
$plStr   = $plausible -join ', '

$scoreTable = ($scoreboard | ForEach-Object {
    "| $($_.record_size) | $($_.feasible_count)/$($records.Count) | $($_.avg_abs_remainder) | $($_.median_abs_remainder) | $($_.avg_overhead_ratio) |"
}) -join "`n"

$focusTable = ($focus | ForEach-Object {
    "| $($_.file) | $($_.trailing_bytes_count) | $($_.ascii_entry_count) | $($_.bytes_per_entry) | $($_.best_record_size_by_abs_rem) | $($_.record_size_6_remainder) |"
}) -join "`n"

$md = @"
# MAP-6X Build 42 LOTH Per-Entry Record Model

${fence}text
BUILD42_LOTH_PER_ENTRY_RECORD_MODEL_ANALYSED
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
| Focus files | $($focus.Count) |
| Most plausible record sizes | $plStr |
| Stable prefix byte positions | $stablePos / 32 |
| Files where rs6 feasible | $($report.count_where_rs6_feasible) / $($records.Count) |
| Avg rs6 overhead ratio | $($report.avg_rs6_overhead_ratio) |
| Smallest file | $($report.smallest_file) |
| Smallest bytes per entry | $($report.smallest_bytes_per_entry) |

## Record size scoreboard

| Record size | Feasible | Avg abs rem | Median abs rem | Avg overhead |
|---|---|---|---|---|
$scoreTable

## Focus files (smallest trailing)

| File | TrailingBytes | Entries | BPE | BestRS | RS6Rem |
|---|---|---|---|---|---|
$focusTable

## Non-claims

- WRITER_NOT_CHANGED: model analysis only.
- LOAD_TEST_NOT_PERFORMED: no PZ session.
- PLAYABLE_EXPORT_CLAIM_ALLOWED=false: binding.
"@

Set-Content -Path $mdPath -Value $md -Encoding ASCII
Write-Output "Model MD:   $mdPath"
Write-Output ""
Write-Output "likely_model_hypotheses:         $hypsStr"
Write-Output "writer_readiness:                $writerReadiness"
Write-Output "recommended_next_step:           $nextStep"
Write-Output "most_plausible_record_sizes:     $plStr"
Write-Output "stable_prefix_byte_count:        $stablePos"
Write-Output "BUILD42_LOTH_PER_ENTRY_RECORD_MODEL_ANALYSED"
Write-Output "WRITER_NOT_CHANGED"
Write-Output "LOAD_TEST_NOT_PERFORMED"
Write-Output "PLAYABLE_EXPORT_CLAIM_ALLOWED=false"
Write-Output "Done."
