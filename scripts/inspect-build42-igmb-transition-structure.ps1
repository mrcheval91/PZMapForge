[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$ReferenceWorldmapBinPath,
    [Parameter(Mandatory)][string]$Output,
    [int]$TransitionOffset  = 6389,
    [int]$MaxBytes          = 65536,
    [int]$WindowBeforeBytes = 64,
    [int]$WindowAfterBytes  = 512
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

if (-not $Output.Contains('.local')) {
    Write-Error "-Output must be a path under .local/ (got: $Output)"
    exit 1
}

$schema        = 'pzmapforge.map8w-igmb-transition-structure-inspection.v0.1'
$hardCap       = 65536
$winBeforeCap  = 256
$winAfterCap   = 2048

$effectiveMax  = [Math]::Min($MaxBytes, $hardCap)
$effectiveWinB = [Math]::Min($WindowBeforeBytes, $winBeforeCap)
$effectiveWinA = [Math]::Min($WindowAfterBytes, $winAfterCap)

$outDir = $Output
if (-not (Test-Path $outDir)) {
    New-Item -ItemType Directory -Force -Path $outDir | Out-Null
}

$referencePresent   = Test-Path -LiteralPath $ReferenceWorldmapBinPath
$referenceSizeBytes = $null
$bytesReadCount     = 0
$fullFileRead       = $false
$buf                = $null
$magic              = $null
$versionU32         = $null

if ($referencePresent) {
    $referenceSizeBytes = (Get-Item -LiteralPath $ReferenceWorldmapBinPath).Length
    $toRead = [int][Math]::Min([long]$referenceSizeBytes, [long]$effectiveMax)
    $stream = [System.IO.FileStream]::new(
        $ReferenceWorldmapBinPath,
        [System.IO.FileMode]::Open,
        [System.IO.FileAccess]::Read,
        [System.IO.FileShare]::Read
    )
    try {
        $buf            = [byte[]]::new($toRead)
        $bytesReadCount = $stream.Read($buf, 0, $toRead)
    } finally {
        $stream.Dispose()
    }
    $fullFileRead = ([long]$bytesReadCount -eq [long]$referenceSizeBytes)
    if ($bytesReadCount -ge 4) {
        $magic = [System.Text.Encoding]::ASCII.GetString($buf, 0, 4)
    }
    if ($bytesReadCount -ge 8) {
        $versionU32 = [BitConverter]::ToUInt32($buf, 4)
    }
}

$transitionInRange     = ($referencePresent -and $TransitionOffset -ge 0 -and $TransitionOffset -lt $bytesReadCount)
$transitionAligned4    = ($TransitionOffset % 4 -eq 0)
$transitionAligned2    = ($TransitionOffset % 2 -eq 0)

$transWindowBefore  = $null
$transWindowAfter   = $null
$exact128Limit      = 0
$exactLen           = 0

if ($transitionInRange) {
    # Window before
    $wbStart = [Math]::Max(0, $TransitionOffset - $effectiveWinB)
    $wbLen   = $TransitionOffset - $wbStart
    if ($wbLen -gt 0) {
        $transWindowBefore = (($buf[$wbStart..($TransitionOffset - 1)] | ForEach-Object { $_.ToString('x2') }) -join ' ')
    }

    # Window after
    $waEnd = [Math]::Min($bytesReadCount, $TransitionOffset + $effectiveWinA)
    $waLen = $waEnd - $TransitionOffset
    if ($waLen -gt 0) {
        $transWindowAfter = (($buf[$TransitionOffset..($waEnd - 1)] | ForEach-Object { $_.ToString('x2') }) -join ' ')
    }

    $exact128Limit = [Math]::Min($bytesReadCount, $TransitionOffset + 128)
    $exactLen      = $exact128Limit - $TransitionOffset
}

# Exact U32LE from transition
$exactU32s = [System.Collections.ArrayList]::new()
if ($transitionInRange) {
    $u32End = [Math]::Min($bytesReadCount - 4, $TransitionOffset + 124)
    for ($j = $TransitionOffset; $j -le $u32End; $j += 4) {
        $vu = [BitConverter]::ToUInt32($buf, $j)
        $vi = [BitConverter]::ToInt32($buf, $j)
        [void]$exactU32s.Add([ordered]@{
            offset    = $j
            relative  = ($j - $TransitionOffset)
            value_u32 = $vu
            value_i32 = $vi
        })
    }
}

# Exact U16LE from transition
$exactU16s = [System.Collections.ArrayList]::new()
if ($transitionInRange) {
    $u16End = [Math]::Min($bytesReadCount - 2, $TransitionOffset + 126)
    for ($j = $TransitionOffset; $j -le $u16End; $j += 2) {
        $vu16 = [int][BitConverter]::ToUInt16($buf, $j)
        [void]$exactU16s.Add([ordered]@{
            offset   = $j
            relative = ($j - $TransitionOffset)
            value    = $vu16
        })
    }
}

# Exact I16LE from transition (signed)
$exactI16s = [System.Collections.ArrayList]::new()
if ($transitionInRange) {
    $i16End = [Math]::Min($bytesReadCount - 2, $TransitionOffset + 126)
    for ($j = $TransitionOffset; $j -le $i16End; $j += 2) {
        $vi16 = [int][BitConverter]::ToInt16($buf, $j)
        [void]$exactI16s.Add([ordered]@{
            offset   = $j
            relative = ($j - $TransitionOffset)
            value    = $vi16
        })
    }
}

# Exact byte values from transition
$exactBytes = [System.Collections.ArrayList]::new()
if ($transitionInRange) {
    $byteEnd = [Math]::Min($bytesReadCount - 1, $TransitionOffset + 127)
    for ($j = $TransitionOffset; $j -le $byteEnd; $j++) {
        [void]$exactBytes.Add([ordered]@{
            offset   = $j
            relative = ($j - $TransitionOffset)
            value    = [int]$buf[$j]
        })
    }
}

# Candidate header U32 triplet
$candidateHeaderTriplet = $null
$tripletFirst  = $null
$tripletSecond = $null
$tripletThird  = $null
if ($transitionInRange -and ($TransitionOffset + 12) -le $bytesReadCount) {
    $tripletFirst  = [int][BitConverter]::ToUInt32($buf, $TransitionOffset)
    $tripletSecond = [int][BitConverter]::ToUInt32($buf, $TransitionOffset + 4)
    $tripletThird  = [int][BitConverter]::ToUInt32($buf, $TransitionOffset + 8)
    $candidateHeaderTriplet = [ordered]@{
        first          = $tripletFirst
        second         = $tripletSecond
        third          = $tripletThird
        interpretation = 'observed_only_unconfirmed'
    }
}

# Candidate count fields (U32LE in 1..65535)
$candidateCounts = [System.Collections.ArrayList]::new()
foreach ($entry in $exactU32s) {
    if ([long]$entry.value_u32 -ge 1 -and [long]$entry.value_u32 -le 65535) {
        [void]$candidateCounts.Add($entry)
    }
}

# Candidate offset fields (U32LE in 1..(reference_size-1))
$candidateOffsets = [System.Collections.ArrayList]::new()
if ($null -ne $referenceSizeBytes) {
    foreach ($entry in $exactU32s) {
        if ([long]$entry.value_u32 -ge 1 -and [long]$entry.value_u32 -lt [long]$referenceSizeBytes) {
            [void]$candidateOffsets.Add($entry)
        }
    }
}

# Candidate coordinate fields (U16LE in 1..1000)
$candidateCoords = [System.Collections.ArrayList]::new()
foreach ($entry in $exactU16s) {
    if ($entry.value -ge 1 -and $entry.value -le 1000) {
        [void]$candidateCoords.Add($entry)
    }
}

# Candidate signed coordinate fields (I16LE in -5000..5000, non-zero)
$candidateSignedCoords = [System.Collections.ArrayList]::new()
foreach ($entry in $exactI16s) {
    if ($entry.value -ne 0 -and $entry.value -ge -5000 -and $entry.value -le 5000) {
        [void]$candidateSignedCoords.Add($entry)
    }
}

# Candidate run-length patterns (same U16LE repeated consecutively, length >= 2)
$candidateRunLengths = [System.Collections.ArrayList]::new()
$u16Arr = @($exactU16s)
if ($u16Arr.Count -ge 2) {
    $runStart = 0
    for ($k = 1; $k -le $u16Arr.Count; $k++) {
        $atEnd    = ($k -eq $u16Arr.Count)
        $differs  = (-not $atEnd) -and ($u16Arr[$k].value -ne $u16Arr[$runStart].value)
        if ($atEnd -or $differs) {
            $runLen = $k - $runStart
            if ($runLen -ge 2) {
                [void]$candidateRunLengths.Add([ordered]@{
                    start_offset = $u16Arr[$runStart].offset
                    value        = $u16Arr[$runStart].value
                    run_length   = $runLen
                })
            }
            $runStart = $k
        }
    }
}

# Candidate repeated pairs (same U32LE value at 2+ offsets)
$candidateRepeatedPairs = [System.Collections.ArrayList]::new()
$seenU32 = @{}
foreach ($entry in $exactU32s) {
    $key = "$($entry.value_u32)"
    if (-not $seenU32.ContainsKey($key)) { $seenU32[$key] = [System.Collections.ArrayList]::new() }
    [void]$seenU32[$key].Add($entry.offset)
}
foreach ($kv in $seenU32.GetEnumerator()) {
    if ($kv.Value.Count -ge 2) {
        [void]$candidateRepeatedPairs.Add([ordered]@{
            value   = [int]$kv.Key
            count   = $kv.Value.Count
            offsets = @($kv.Value)
        })
    }
}

# Candidate small-value clusters (U16LE < 512, consecutive run of >= 4)
$candidateSmallClusters = [System.Collections.ArrayList]::new()
$clusterStart = -1
$clusterLen   = 0
for ($k = 0; $k -lt $u16Arr.Count; $k++) {
    if ($u16Arr[$k].value -lt 512) {
        if ($clusterStart -lt 0) { $clusterStart = $k; $clusterLen = 1 } else { $clusterLen++ }
    } else {
        if ($clusterLen -ge 4) {
            [void]$candidateSmallClusters.Add([ordered]@{
                start_offset = $u16Arr[$clusterStart].offset
                entry_count  = $clusterLen
            })
        }
        $clusterStart = -1; $clusterLen = 0
    }
}
if ($clusterLen -ge 4) {
    [void]$candidateSmallClusters.Add([ordered]@{
        start_offset = $u16Arr[$clusterStart].offset
        entry_count  = $clusterLen
    })
}

# Candidate FF or null sentinels (U16LE == 0x0000 or 0xFFFF)
$candidateSentinels = [System.Collections.ArrayList]::new()
if ($transitionInRange) {
    $sentEnd = [Math]::Min($bytesReadCount - 2, $TransitionOffset + 126)
    for ($j = $TransitionOffset; $j -le $sentEnd; $j += 2) {
        $vs = [int][BitConverter]::ToUInt16($buf, $j)
        if ($vs -eq 0x0000 -or $vs -eq 0xFFFF) {
            [void]$candidateSentinels.Add([ordered]@{
                offset   = $j
                relative = ($j - $TransitionOffset)
                value    = $vs
            })
        }
    }
}

# Candidate monotonic sequences (U16LE strictly increasing or decreasing, run >= 3)
$candidateMonotonic = [System.Collections.ArrayList]::new()
if ($u16Arr.Count -ge 3) {
    $monoStart = 0
    $dir       = 0
    for ($k = 1; $k -le $u16Arr.Count; $k++) {
        $seqBreak = ($k -eq $u16Arr.Count)
        if (-not $seqBreak) {
            $diff = $u16Arr[$k].value - $u16Arr[$k-1].value
            if ($diff -gt 0) {
                if ($dir -eq 0) { $dir = 1 } elseif ($dir -ne 1) { $seqBreak = $true }
            } elseif ($diff -lt 0) {
                if ($dir -eq 0) { $dir = -1 } elseif ($dir -ne -1) { $seqBreak = $true }
            } else { $seqBreak = $true }
        }
        if ($seqBreak) {
            $seqLen = $k - $monoStart
            if ($seqLen -ge 3) {
                [void]$candidateMonotonic.Add([ordered]@{
                    start_offset = $u16Arr[$monoStart].offset
                    length       = $seqLen
                    direction    = if ($dir -eq 1) { 'increasing' } else { 'decreasing' }
                })
            }
            $monoStart = $k; $dir = 0
        }
    }
}

# Printable ASCII runs near transition (length >= 3)
$asciiRuns = [System.Collections.ArrayList]::new()
if ($transitionInRange -and $exactLen -gt 0) {
    $asciiEnd = $exact128Limit
    $runSb    = [System.Text.StringBuilder]::new()
    $runOff   = $TransitionOffset
    for ($j = $TransitionOffset; $j -lt $asciiEnd; $j++) {
        $b = $buf[$j]
        if ($b -ge 0x20 -and $b -le 0x7E) {
            if ($runSb.Length -eq 0) { $runOff = $j }
            [void]$runSb.Append([char]$b)
        } else {
            if ($runSb.Length -ge 3) {
                [void]$asciiRuns.Add([ordered]@{ offset=$runOff; length=$runSb.Length; text=$runSb.ToString() })
            }
            $runSb.Clear() | Out-Null
        }
    }
    if ($runSb.Length -ge 3) {
        [void]$asciiRuns.Add([ordered]@{ offset=$runOff; length=$runSb.Length; text=$runSb.ToString() })
    }
}

# Entropy estimate (Shannon entropy of first 128 bytes from transition)
$entropyEstimate = $null
if ($transitionInRange -and $exactLen -gt 0) {
    $byteCounts = @{}
    for ($j = $TransitionOffset; $j -lt $exact128Limit; $j++) {
        $bv = $buf[$j]
        if (-not $byteCounts.ContainsKey($bv)) { $byteCounts[$bv] = 0 }
        $byteCounts[$bv]++
    }
    $entropy = 0.0
    foreach ($c in $byteCounts.Values) {
        $p = [double]$c / [double]$exactLen
        if ($p -gt 0) { $entropy -= $p * [Math]::Log($p, 2) }
    }
    $entropyEstimate = [Math]::Round($entropy, 4)
}

# Structure hypotheses (observed-only)
$hypotheses = [System.Collections.ArrayList]::new()
[void]$hypotheses.Add('hypothesis_a_count_table_header: first_3_u32le_may_be_counts_or_dimensions_observed_only')
[void]$hypotheses.Add('hypothesis_b_u16le_pairs: bytes_after_first_12_may_be_packed_u16le_coordinate_or_delta_pairs')
[void]$hypotheses.Add('hypothesis_c_variable_length_section: transition_may_mark_start_of_variable_length_geometry_payload')
[void]$hypotheses.Add('hypothesis_d_big_endian_possibility: if_java_big_endian_u16_pairs_after_byte_12_have_different_interpretation')
if ($candidateMonotonic.Count -gt 0) {
    [void]$hypotheses.Add('hypothesis_e_coordinate_sequence: monotonic_u16le_sequences_suggest_possible_coordinate_run')
}
if ($candidateRunLengths.Count -gt 0) {
    [void]$hypotheses.Add('hypothesis_f_run_length_or_repetition: repeated_u16le_values_suggest_rle_or_repeated_structure')
}
if ($candidateRepeatedPairs.Count -gt 0) {
    [void]$hypotheses.Add('hypothesis_g_repeated_u32_fields: same_u32le_value_at_multiple_offsets_suggests_struct_reuse_or_sentinel')
}

$strongestHypothesis = 'transition_immediately_follows_ff_padding_first_12_bytes_are_3_u32le_fields_bytes_after_resemble_packed_u16le_pairs_no_hypothesis_confirmed'

$interpretation = if ($transitionInRange) {
    'transition_structure_window_analyzed_observe_only'
} else {
    'transition_offset_outside_read_range_cannot_analyze'
}

$result = [ordered]@{
    schema                                            = $schema
    reference_present                                 = $referencePresent
    reference_size_bytes                              = $referenceSizeBytes
    bytes_read_count                                  = $bytesReadCount
    max_bytes_allowed                                 = $hardCap
    full_file_read                                    = $fullFileRead
    magic                                             = $magic
    version_le_u32                                    = $versionU32
    transition_offset                                 = $TransitionOffset
    transition_offset_is_4_byte_aligned               = $transitionAligned4
    transition_offset_is_2_byte_aligned               = $transitionAligned2
    transition_offset_in_range                        = $transitionInRange
    transition_window_before_hex                      = $transWindowBefore
    transition_window_after_hex                       = $transWindowAfter
    exact_u32le_values_from_transition_first_128      = $exactU32s
    exact_u16le_values_from_transition_first_128      = $exactU16s
    exact_i16le_values_from_transition_first_128      = $exactI16s
    exact_byte_values_from_transition_first_128       = $exactBytes
    candidate_header_u32_triplet                      = $candidateHeaderTriplet
    candidate_header_triplet_confidence               = 'low'
    candidate_count_fields                            = $candidateCounts
    candidate_offset_fields                           = $candidateOffsets
    candidate_coordinate_fields                       = $candidateCoords
    candidate_signed_coordinate_fields                = $candidateSignedCoords
    candidate_run_length_patterns                     = $candidateRunLengths
    candidate_repeated_pairs                          = $candidateRepeatedPairs
    candidate_small_value_clusters                    = $candidateSmallClusters
    candidate_ff_or_null_sentinels_after_transition   = $candidateSentinels
    candidate_monotonic_sequences                     = $candidateMonotonic
    printable_ascii_runs_near_transition              = $asciiRuns
    entropy_estimate_transition_window                = $entropyEstimate
    structure_hypotheses_observed_only                = $hypotheses
    strongest_current_hypothesis                      = $strongestHypothesis
    interpretation                                    = $interpretation
    confidence_level                                  = 'low'
    transition_structure_understood                   = $false
    full_format_understood                            = $false
    cell_index_understood                             = $false
    geometry_payload_understood                       = $false
    writer_implementation_allowed                     = $false
    binary_writer_gate_closed                         = $true
    playable_claim_allowed                            = $false
    third_party_files_copied                          = $false
    next_branch                                       = 'igmb_transition_model_record_pending_operator_review'
}

$jsonPath = Join-Path $outDir 'igmb-transition-structure-inspection.json'
$mdPath   = Join-Path $outDir 'igmb-transition-structure-inspection.md'

$result | ConvertTo-Json -Depth 5 | Set-Content -Encoding UTF8 -Path $jsonPath

$mdLines = [System.Collections.ArrayList]::new()
[void]$mdLines.Add('# IGMB Transition Structure Inspection')
[void]$mdLines.Add('')
[void]$mdLines.Add("Schema: ``$schema``")
[void]$mdLines.Add('')
[void]$mdLines.Add("reference_present: $referencePresent")
[void]$mdLines.Add("reference_size_bytes: $referenceSizeBytes")
[void]$mdLines.Add("bytes_read_count: $bytesReadCount")
[void]$mdLines.Add("max_bytes_allowed: $hardCap")
[void]$mdLines.Add("full_file_read: $fullFileRead")
[void]$mdLines.Add("magic: $magic")
[void]$mdLines.Add("version_le_u32: $versionU32")
[void]$mdLines.Add('')
[void]$mdLines.Add("transition_offset: $TransitionOffset")
[void]$mdLines.Add("transition_offset_is_4_byte_aligned: $transitionAligned4")
[void]$mdLines.Add("transition_offset_is_2_byte_aligned: $transitionAligned2")
[void]$mdLines.Add("transition_offset_in_range: $transitionInRange")
[void]$mdLines.Add('')
if ($null -ne $candidateHeaderTriplet) {
    [void]$mdLines.Add("candidate_header_u32_triplet: first=$tripletFirst second=$tripletSecond third=$tripletThird (observed_only_unconfirmed)")
}
[void]$mdLines.Add("candidate_header_triplet_confidence: low")
[void]$mdLines.Add('')
[void]$mdLines.Add("entropy_estimate_transition_window: $entropyEstimate")
[void]$mdLines.Add('')
[void]$mdLines.Add("interpretation: $interpretation")
[void]$mdLines.Add("confidence_level: low")
[void]$mdLines.Add('')
[void]$mdLines.Add("strongest_current_hypothesis: $strongestHypothesis")
[void]$mdLines.Add('')
[void]$mdLines.Add("transition_structure_understood: False")
[void]$mdLines.Add("full_format_understood: False")
[void]$mdLines.Add("cell_index_understood: False")
[void]$mdLines.Add("binary_writer_gate_closed: True")
[void]$mdLines.Add("playable_claim_allowed: False")
[void]$mdLines.Add("third_party_files_copied: False")
[void]$mdLines.Add('')
[void]$mdLines.Add("next_branch: igmb_transition_model_record_pending_operator_review")

$mdLines | Set-Content -Encoding UTF8 -Path $mdPath

Write-Host "igmb-transition-structure-inspection.json -> $jsonPath"
Write-Host "igmb-transition-structure-inspection.md   -> $mdPath"
exit 0
