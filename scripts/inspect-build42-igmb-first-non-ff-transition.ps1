[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$ReferenceWorldmapBinPath,
    [Parameter(Mandatory)][string]$Output,
    [int]$StringPoolEndOffset = 133,
    [int]$MaxBytes = 65536,
    [int]$WindowBytes = 64
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

$hardCap       = 65536
$windowHardCap = 256
$effectiveMax    = [Math]::Min($MaxBytes, $hardCap)
$effectiveWindow = [Math]::Min($WindowBytes, $windowHardCap)

$schema = 'pzmapforge.map8u-igmb-first-non-ff-transition-inspection.v0.1'

$referencePresent  = Test-Path -LiteralPath $ReferenceWorldmapBinPath
$referenceSizeBytes = [long]0
$bytesReadCount    = 0
$fullFileRead      = $false
$magic             = 'unknown'
$versionU32        = 0
$scanEndExclusive  = 0

$firstNonFfFound   = $false
$firstNonFfOffset  = $null
$firstNonFfRelOffset = $null
$ffRunLength       = $null
$alignedBy4        = $null
$alignedBy2        = $null
$hexBefore         = $null
$hexAfter          = $null

$u32Around       = [System.Collections.ArrayList]::new()
$u16Around       = [System.Collections.ArrayList]::new()
$asciiRuns       = [System.Collections.ArrayList]::new()
$plausibleCounts  = [System.Collections.ArrayList]::new()
$plausibleOffsets = [System.Collections.ArrayList]::new()
$plausibleCoords  = [System.Collections.ArrayList]::new()

if ($referencePresent) {
    $referenceSizeBytes = (Get-Item -LiteralPath $ReferenceWorldmapBinPath).Length
    $toRead = [int][Math]::Min([long]$referenceSizeBytes, [long]$effectiveMax)

    $buf = [byte[]]::new($toRead)
    $fs = [System.IO.FileStream]::new(
        $ReferenceWorldmapBinPath,
        [System.IO.FileMode]::Open,
        [System.IO.FileAccess]::Read,
        [System.IO.FileShare]::Read
    )
    try { $null = $fs.Read($buf, 0, $toRead) }
    finally { $fs.Dispose() }

    $bytesReadCount = $toRead
    $fullFileRead   = ([long]$bytesReadCount -eq [long]$referenceSizeBytes)

    if ($buf.Length -ge 4) {
        $magic = [System.Text.Encoding]::ASCII.GetString($buf, 0, 4)
    }
    if ($buf.Length -ge 8) {
        $versionU32 = [int][BitConverter]::ToUInt32($buf, 4)
    }

    $scanStart        = $StringPoolEndOffset
    $scanEndExclusive = $bytesReadCount

    if ($scanStart -lt $bytesReadCount) {
        for ($i = $scanStart; $i -lt $scanEndExclusive; $i++) {
            if ($buf[$i] -ne 0xFF) {
                $firstNonFfFound    = $true
                $firstNonFfOffset   = $i
                $firstNonFfRelOffset = $i - $scanStart
                $ffRunLength        = $i - $scanStart
                break
            }
        }
    }

    if ($firstNonFfFound) {
        $alignedBy4 = (($firstNonFfOffset % 4) -eq 0)
        $alignedBy2 = (($firstNonFfOffset % 2) -eq 0)

        $beforeStart = [Math]::Max(0, $firstNonFfOffset - $effectiveWindow)
        $beforeLen   = $firstNonFfOffset - $beforeStart
        if ($beforeLen -gt 0) {
            $hexBefore = (($buf[$beforeStart..($firstNonFfOffset - 1)] | ForEach-Object { $_.ToString('x2') }) -join ' ')
        } else {
            $hexBefore = ''
        }

        $afterEnd = [Math]::Min($bytesReadCount, $firstNonFfOffset + $effectiveWindow)
        $afterLen = $afterEnd - $firstNonFfOffset
        if ($afterLen -gt 0) {
            $hexAfter = (($buf[$firstNonFfOffset..($afterEnd - 1)] | ForEach-Object { $_.ToString('x2') }) -join ' ')
        } else {
            $hexAfter = ''
        }

        if ($bytesReadCount -ge 4) {
            $u32ScanStart = [Math]::Max(0, $firstNonFfOffset - 16)
            $rem = $u32ScanStart % 4
            if ($rem -ne 0) { $u32ScanStart += (4 - $rem) }
            $u32ScanEnd = [Math]::Min($bytesReadCount - 4, $firstNonFfOffset + 16)
            for ($j = $u32ScanStart; $j -le $u32ScanEnd; $j += 4) {
                $vi32 = [BitConverter]::ToInt32($buf, $j)
                $vu32 = [BitConverter]::ToUInt32($buf, $j)
                [void]$u32Around.Add([ordered]@{
                    offset          = $j
                    value_i32       = $vi32
                    value_u32_str   = "$vu32"
                })
            }
        }

        if ($bytesReadCount -ge 2) {
            $u16ScanStart = [Math]::Max(0, $firstNonFfOffset - 8)
            $rem16 = $u16ScanStart % 2
            if ($rem16 -ne 0) { $u16ScanStart++ }
            $u16ScanEnd = [Math]::Min($bytesReadCount - 2, $firstNonFfOffset + 8)
            for ($j = $u16ScanStart; $j -le $u16ScanEnd; $j += 2) {
                $vu16 = [int][BitConverter]::ToUInt16($buf, $j)
                [void]$u16Around.Add([ordered]@{ offset=$j; value=$vu16 })
            }
        }

        $asciiStart = [Math]::Max(0, $firstNonFfOffset - $effectiveWindow)
        $asciiEnd   = [Math]::Min($bytesReadCount, $firstNonFfOffset + $effectiveWindow)
        $runSb      = [System.Text.StringBuilder]::new()
        $runOff     = -1
        for ($j = $asciiStart; $j -lt $asciiEnd; $j++) {
            if ($buf[$j] -ge 0x20 -and $buf[$j] -le 0x7E) {
                if ($runOff -lt 0) { $runOff = $j }
                [void]$runSb.Append([char]$buf[$j])
            } else {
                if ($runSb.Length -ge 3) {
                    [void]$asciiRuns.Add([ordered]@{
                        offset = $runOff
                        length = $runSb.Length
                        text   = $runSb.ToString()
                    })
                }
                [void]$runSb.Clear()
                $runOff = -1
            }
        }
        if ($runSb.Length -ge 3) {
            [void]$asciiRuns.Add([ordered]@{
                offset = $runOff
                length = $runSb.Length
                text   = $runSb.ToString()
            })
        }

        if ($bytesReadCount -ge 4) {
            $hStart = $firstNonFfOffset
            $hRem = $hStart % 4
            if ($hRem -ne 0) { $hStart += (4 - $hRem) }
            $hEnd = [Math]::Min($bytesReadCount - 4, $firstNonFfOffset + 48)
            for ($j = $hStart; $j -le $hEnd; $j += 4) {
                $vu32h = [BitConverter]::ToUInt32($buf, $j)
                if ($vu32h -ge 1 -and $vu32h -le 65535) {
                    [void]$plausibleCounts.Add([ordered]@{ offset=$j; value=$vu32h })
                }
                if ($vu32h -ge 1 -and [long]$vu32h -lt [long]$referenceSizeBytes) {
                    [void]$plausibleOffsets.Add([ordered]@{ offset=$j; value=$vu32h })
                }
                if ($vu32h -ge 1 -and $vu32h -le 500) {
                    [void]$plausibleCoords.Add([ordered]@{ offset=$j; value=$vu32h })
                }
            }
        }
    }
}

$interpretation = if ($firstNonFfFound) {
    'first_non_ff_byte_found_transition_located'
} else {
    'ff_region_continues_beyond_bounded_scan'
}

$nextBranch = if ($firstNonFfFound) {
    'igmb_transition_structure_analysis_pending_operator_approval_if_non_ff_found'
} else {
    'larger_bounded_transition_scan_pending_operator_approval'
}

$result = [ordered]@{
    schema                                         = $schema
    reference_present                              = $referencePresent
    reference_size_bytes                           = $referenceSizeBytes
    bytes_read_count                               = $bytesReadCount
    max_bytes_allowed                              = $hardCap
    full_file_read                                 = $fullFileRead
    magic                                          = $magic
    version_le_u32                                 = $versionU32
    string_pool_end_offset                         = $StringPoolEndOffset
    scan_start_offset                              = $StringPoolEndOffset
    scan_end_offset_exclusive                      = $scanEndExclusive
    first_non_ff_found                             = $firstNonFfFound
    first_non_ff_offset                            = $firstNonFfOffset
    first_non_ff_relative_offset_after_string_pool = $firstNonFfRelOffset
    ff_run_start_offset                            = $StringPoolEndOffset
    ff_run_length_until_first_non_ff               = $ffRunLength
    transition_offset_is_4_byte_aligned            = $alignedBy4
    transition_offset_is_2_byte_aligned            = $alignedBy2
    hex_window_before_transition                   = $hexBefore
    hex_window_after_transition                    = $hexAfter
    u32le_values_around_transition                 = $u32Around
    u16le_values_around_transition                 = $u16Around
    printable_ascii_runs_near_transition           = $asciiRuns
    plausible_count_fields_near_transition         = $plausibleCounts
    plausible_offset_candidates_near_transition    = $plausibleOffsets
    plausible_cell_coordinate_candidates_near_transition = $plausibleCoords
    interpretation                                 = $interpretation
    confidence_level                               = 'low'
    full_format_understood                         = $false
    cell_index_understood                          = $false
    geometry_payload_understood                    = $false
    writer_implementation_allowed                  = $false
    binary_writer_gate_closed                      = $true
    playable_claim_allowed                         = $false
    third_party_files_copied                       = $false
    next_branch                                    = $nextBranch
}

$jsonPath = Join-Path $outDir 'igmb-first-non-ff-transition-inspection.json'
$mdPath   = Join-Path $outDir 'igmb-first-non-ff-transition-inspection.md'

$result | ConvertTo-Json -Depth 5 | Set-Content -Encoding UTF8 -Path $jsonPath

$mdLines = [System.Collections.ArrayList]::new()
[void]$mdLines.Add('# IGMB First Non-FF Transition Inspection')
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
[void]$mdLines.Add("string_pool_end_offset: $StringPoolEndOffset")
[void]$mdLines.Add("scan_start_offset: $StringPoolEndOffset")
[void]$mdLines.Add("scan_end_offset_exclusive: $scanEndExclusive")
[void]$mdLines.Add('')
[void]$mdLines.Add("first_non_ff_found: $firstNonFfFound")
[void]$mdLines.Add("first_non_ff_offset: $firstNonFfOffset")
[void]$mdLines.Add("first_non_ff_relative_offset_after_string_pool: $firstNonFfRelOffset")
[void]$mdLines.Add("ff_run_start_offset: $StringPoolEndOffset")
[void]$mdLines.Add("ff_run_length_until_first_non_ff: $ffRunLength")
[void]$mdLines.Add("transition_offset_is_4_byte_aligned: $alignedBy4")
[void]$mdLines.Add("transition_offset_is_2_byte_aligned: $alignedBy2")
[void]$mdLines.Add('')
[void]$mdLines.Add("interpretation: $interpretation")
[void]$mdLines.Add("confidence_level: low")
[void]$mdLines.Add('')
[void]$mdLines.Add("binary_writer_gate_closed: True")
[void]$mdLines.Add("playable_claim_allowed: False")
[void]$mdLines.Add("third_party_files_copied: False")
[void]$mdLines.Add('')
[void]$mdLines.Add("next_branch: $nextBranch")

$mdLines | Set-Content -Encoding UTF8 -Path $mdPath

Write-Host "igmb-first-non-ff-transition-inspection.json -> $jsonPath"
Write-Host "igmb-first-non-ff-transition-inspection.md   -> $mdPath"
exit 0
