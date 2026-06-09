[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$ReferenceWorldmapBinPath,
    [string]$CandidateWorldmapBinPath = '',
    [Parameter(Mandatory)][string]$Output,
    [int]$MaxBytes = 4096,
    [int]$StringPoolEndOffset = 133
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

if ($MaxBytes -gt 4096) { $MaxBytes = 4096 }

if (-not $Output.Contains('.local')) {
    Write-Error "-Output must be a path under .local/ (got: $Output)"
    exit 1
}

$outDir = $Output
if (-not (Test-Path $outDir)) {
    New-Item -ItemType Directory -Force -Path $outDir | Out-Null
}

$schema = 'pzmapforge.map8s-igmb-cell-boundary-inspection.v0.1'

function Read-BoundedBytes([string]$filePath, [int]$maxRead) {
    $r = [ordered]@{
        present          = $false
        size_bytes       = [long]0
        bytes_read_count = 0
        buf              = [byte[]]@()
    }
    if (-not (Test-Path -LiteralPath $filePath)) { return $r }
    $r.present = $true
    $info = Get-Item -LiteralPath $filePath
    $r.size_bytes = [long]$info.Length
    $readCount = [int][Math]::Min($maxRead, $r.size_bytes)
    if ($readCount -eq 0) { return $r }
    $buf = [byte[]]::new($readCount)
    $fs = [System.IO.FileStream]::new($filePath, [System.IO.FileMode]::Open, [System.IO.FileAccess]::Read, [System.IO.FileShare]::Read)
    try {
        $actual = $fs.Read($buf, 0, $readCount)
        if ($actual -lt $readCount) { $buf = $buf[0..($actual - 1)] }
        $r.bytes_read_count = $actual
        $r.buf = $buf
    } finally {
        $fs.Close()
    }
    return $r
}

$ref = Read-BoundedBytes $ReferenceWorldmapBinPath $MaxBytes
$buf = $ref.buf

# Version from bytes 4-7
$versionLeU32 = if ($buf.Count -ge 8) {
    [int]($buf[4]) -bor ([int]($buf[5]) -shl 8) -bor ([int]($buf[6]) -shl 16) -bor ([int]($buf[7]) -shl 24)
} else { -1 }

# Post-string-pool window
$postStart = $StringPoolEndOffset
$postAvailable = [int][Math]::Max(0, $buf.Count - $postStart)

# First 128 bytes after string pool as hex string
$hexLimit128 = [int][Math]::Min(128, $postAvailable)
$sbHex128 = [System.Text.StringBuilder]::new()
for ($i = 0; $i -lt $hexLimit128; $i++) {
    if ($i -gt 0) { [void]$sbHex128.Append(' ') }
    [void]$sbHex128.Append($buf[$postStart + $i].ToString('X2'))
}
$first128Hex = $sbHex128.ToString()

# First 256 bytes after string pool as hex string
$hexLimit256 = [int][Math]::Min(256, $postAvailable)
$sbHex256 = [System.Text.StringBuilder]::new()
for ($i = 0; $i -lt $hexLimit256; $i++) {
    if ($i -gt 0) { [void]$sbHex256.Append(' ') }
    [void]$sbHex256.Append($buf[$postStart + $i].ToString('X2'))
}
$first256Hex = $sbHex256.ToString()

# U32LE from postStart in 4-byte steps (first 128 bytes window = 32 values max)
$u32PostList = [System.Collections.ArrayList]::new()
$u32PostLimit = [int][Math]::Min(32, [int][Math]::Floor($postAvailable / 4))
for ($i = 0; $i -lt $u32PostLimit; $i++) {
    $off = $postStart + ($i * 4)
    $v = [int]($buf[$off]) -bor ([int]($buf[$off+1]) -shl 8) -bor ([int]($buf[$off+2]) -shl 16) -bor ([int]($buf[$off+3]) -shl 24)
    [void]$u32PostList.Add([ordered]@{ offset = $off; value = $v })
}

# Next 4-byte aligned boundary after postStart, separate alignment hypothesis
$nextAligned = if ($postStart % 4 -eq 0) { $postStart } else { $postStart + (4 - ($postStart % 4)) }
$u32AlignedList = [System.Collections.ArrayList]::new()
if ($nextAligned -ne $postStart) {
    $alignedAvail = [int][Math]::Max(0, $buf.Count - $nextAligned)
    $u32AlignedLimit = [int][Math]::Min(32, [int][Math]::Floor($alignedAvail / 4))
    for ($i = 0; $i -lt $u32AlignedLimit; $i++) {
        $off = $nextAligned + ($i * 4)
        $v = [int]($buf[$off]) -bor ([int]($buf[$off+1]) -shl 8) -bor ([int]($buf[$off+2]) -shl 16) -bor ([int]($buf[$off+3]) -shl 24)
        [void]$u32AlignedList.Add([ordered]@{ offset = $off; value = $v })
    }
}

# U16LE from postStart in 2-byte steps (first 128 bytes window = 64 values max)
$u16PostList = [System.Collections.ArrayList]::new()
$u16PostLimit = [int][Math]::Min(64, [int][Math]::Floor($postAvailable / 2))
for ($i = 0; $i -lt $u16PostLimit; $i++) {
    $off = $postStart + ($i * 2)
    $v = [int]($buf[$off]) -bor ([int]($buf[$off+1]) -shl 8)
    [void]$u16PostList.Add([ordered]@{ offset = $off; value = $v })
}

# float32LE heuristic (first 128 bytes window = 32 values max)
$f32PostList = [System.Collections.ArrayList]::new()
$f32PostLimit = [int][Math]::Min(32, [int][Math]::Floor($postAvailable / 4))
for ($i = 0; $i -lt $f32PostLimit; $i++) {
    $off = $postStart + ($i * 4)
    $bytes4 = [byte[]]@($buf[$off], $buf[$off+1], $buf[$off+2], $buf[$off+3])
    $fv = [System.BitConverter]::ToSingle($bytes4, 0)
    [void]$f32PostList.Add([ordered]@{
        offset          = $off
        value_heuristic = [string]$fv
        note            = 'float32le_heuristic_only'
    })
}

# Plausible count fields: U32LE values in range 1..65535
$countCandidates = [System.Collections.ArrayList]::new()
foreach ($entry in $u32PostList) {
    $v = [int]$entry.value
    if ($v -ge 1 -and $v -le 65535) {
        [void]$countCandidates.Add([ordered]@{
            offset = $entry.offset
            value  = $v
            note   = 'plausible_count_u32le'
        })
    }
}

# Plausible offset candidates: U32LE in range 1..(file_size-1)
$fileSize = if ($ref.size_bytes -gt 0) { [long]$ref.size_bytes } else { [long]283881 }
$offsetCandidates = [System.Collections.ArrayList]::new()
foreach ($entry in $u32PostList) {
    $v = [long]$entry.value
    if ($v -gt 0 -and $v -lt $fileSize) {
        [void]$offsetCandidates.Add([ordered]@{
            offset = $entry.offset
            value  = [int]$entry.value
            note   = 'plausible_file_offset_u32le'
        })
    }
}

# Plausible cell coordinate candidates: U32LE values in range 1..500 (heuristic only)
$coordCandidates = [System.Collections.ArrayList]::new()
foreach ($entry in $u32PostList) {
    $v = [int]$entry.value
    if ($v -ge 1 -and $v -le 500) {
        [void]$coordCandidates.Add([ordered]@{
            offset = $entry.offset
            value  = $v
            note   = 'plausible_cell_coordinate_heuristic_only'
        })
    }
}

# Zero run candidates: runs of >= 4 zero bytes
$zeroRuns = [System.Collections.ArrayList]::new()
$zRunStart = -1
$zRunLen   = 0
for ($i = 0; $i -lt $postAvailable; $i++) {
    if ($buf[$postStart + $i] -eq 0) {
        if ($zRunStart -lt 0) { $zRunStart = $postStart + $i }
        $zRunLen++
    } else {
        if ($zRunLen -ge 4) {
            [void]$zeroRuns.Add([ordered]@{ offset = $zRunStart; length = $zRunLen })
        }
        $zRunStart = -1
        $zRunLen   = 0
    }
}
if ($zRunLen -ge 4) {
    [void]$zeroRuns.Add([ordered]@{ offset = $zRunStart; length = $zRunLen })
}

# Repeated pattern candidates: consecutive identical U32LE pairs
$repeatedPatterns = [System.Collections.ArrayList]::new()
$pairLimit = [int][Math]::Floor($postAvailable / 4) - 1
for ($i = 0; $i -lt $pairLimit; $i++) {
    $off1 = $postStart + ($i * 4)
    $off2 = $off1 + 4
    if ($buf[$off1] -eq $buf[$off2] -and $buf[$off1+1] -eq $buf[$off2+1] -and
        $buf[$off1+2] -eq $buf[$off2+2] -and $buf[$off1+3] -eq $buf[$off2+3]) {
        $v = [int]($buf[$off1]) -bor ([int]($buf[$off1+1]) -shl 8) -bor ([int]($buf[$off1+2]) -shl 16) -bor ([int]($buf[$off1+3]) -shl 24)
        [void]$repeatedPatterns.Add([ordered]@{
            offset1 = $off1; offset2 = $off2; value = $v
            note    = 'consecutive_u32le_repeat'
        })
    }
}

# Section boundary hypotheses
$hypotheses = [System.Collections.ArrayList]::new()
[void]$hypotheses.Add('section_after_string_pool_identity_unknown')
[void]$hypotheses.Add('may_be_cell_index_table')
[void]$hypotheses.Add('may_be_offset_table_into_geometry_section')
[void]$hypotheses.Add('may_be_feature_layer_table')
[void]$hypotheses.Add('may_be_section_header_with_count_and_data')
[void]$hypotheses.Add('full_format_not_determined_from_4096_bytes')

$result = [ordered]@{
    schema                                    = $schema
    reference_present                         = $ref.present
    reference_size_bytes                      = $ref.size_bytes
    bytes_read_count                          = $ref.bytes_read_count
    max_bytes_allowed                         = 4096
    full_file_read                            = $false
    magic                                     = 'IGMB'
    version_le_u32                            = $versionLeU32
    string_pool_end_offset                    = $StringPoolEndOffset
    post_string_pool_window_start             = $postStart
    post_string_pool_window_bytes_available   = $postAvailable
    next_aligned_offset_after_string_pool     = $nextAligned
    first_128_bytes_after_string_pool_hex     = $first128Hex
    first_256_bytes_after_string_pool_hex     = $first256Hex
    u32le_values_after_string_pool_first_128  = $u32PostList
    u32le_aligned_boundary_hypothesis         = $u32AlignedList
    u16le_values_after_string_pool_first_128  = $u16PostList
    float32le_values_after_string_pool_first_128 = $f32PostList
    plausible_count_fields_after_string_pool  = $countCandidates
    plausible_offset_table_candidates         = $offsetCandidates
    plausible_cell_coordinate_candidates      = $coordCandidates
    zero_run_candidates                       = $zeroRuns
    repeated_pattern_candidates               = $repeatedPatterns
    section_boundary_hypotheses               = $hypotheses
    confidence_level                          = 'low'
    full_format_understood                    = $false
    cell_index_understood                     = $false
    geometry_payload_understood               = $false
    writer_implementation_allowed             = $false
    binary_writer_gate_closed                 = $true
    playable_claim_allowed                    = $false
    third_party_files_copied                  = $false
    next_branch                               = 'igmb_cell_index_model_research_pending_operator_approval_if_boundary_evidence_sufficient'
}

$jsonPath = Join-Path $outDir 'igmb-cell-boundary-inspection.json'
$mdPath   = Join-Path $outDir 'igmb-cell-boundary-inspection.md'

$result | ConvertTo-Json -Depth 6 | Set-Content -Encoding UTF8 -Path $jsonPath

$mdLines = [System.Collections.ArrayList]::new()
[void]$mdLines.Add('# MAP-8S: IGMB Cell Boundary Inspection')
[void]$mdLines.Add('')
[void]$mdLines.Add("Schema: ``$schema``")
[void]$mdLines.Add('')
[void]$mdLines.Add("Reference present: $($ref.present)")
[void]$mdLines.Add("Reference size bytes: $($ref.size_bytes)")
[void]$mdLines.Add("Bytes read: $($ref.bytes_read_count) (max_bytes_allowed=4096)")
[void]$mdLines.Add("Version U32LE: $versionLeU32")
[void]$mdLines.Add('')
[void]$mdLines.Add("string_pool_end_offset: $StringPoolEndOffset")
[void]$mdLines.Add("post_string_pool_window_start: $postStart")
[void]$mdLines.Add("post_string_pool_window_bytes_available: $postAvailable")
[void]$mdLines.Add("next_aligned_offset_after_string_pool: $nextAligned")
[void]$mdLines.Add('')
[void]$mdLines.Add('## First 128 bytes after string pool (hex)')
[void]$mdLines.Add('')
[void]$mdLines.Add("  $first128Hex")
[void]$mdLines.Add('')
[void]$mdLines.Add('## U32LE values after string pool (from postStart, 4-byte steps)')
[void]$mdLines.Add('')
foreach ($e in $u32PostList) {
    [void]$mdLines.Add("  offset $($e.offset): $($e.value) (0x$(([int]$e.value).ToString('X8')))")
}
[void]$mdLines.Add('')
[void]$mdLines.Add('## U32LE aligned boundary hypothesis (from next 4-byte aligned offset)')
[void]$mdLines.Add('')
foreach ($e in $u32AlignedList) {
    [void]$mdLines.Add("  offset $($e.offset): $($e.value) (0x$(([int]$e.value).ToString('X8')))")
}
[void]$mdLines.Add('')
[void]$mdLines.Add('## Plausible count fields (U32LE 1..65535)')
[void]$mdLines.Add('')
foreach ($e in $countCandidates) {
    [void]$mdLines.Add("  offset $($e.offset): value=$($e.value)")
}
[void]$mdLines.Add('')
[void]$mdLines.Add('## Plausible offset candidates (U32LE 1..<file_size)')
[void]$mdLines.Add('')
foreach ($e in $offsetCandidates) {
    [void]$mdLines.Add("  offset $($e.offset): value=$($e.value)")
}
[void]$mdLines.Add('')
[void]$mdLines.Add('## Zero run candidates (>= 4 bytes)')
[void]$mdLines.Add('')
foreach ($e in $zeroRuns) {
    [void]$mdLines.Add("  offset $($e.offset): length=$($e.length)")
}
[void]$mdLines.Add('')
[void]$mdLines.Add('## Section boundary hypotheses')
[void]$mdLines.Add('')
foreach ($h in $hypotheses) {
    [void]$mdLines.Add("  - $h")
}
[void]$mdLines.Add('')
[void]$mdLines.Add('## Safety')
[void]$mdLines.Add('')
[void]$mdLines.Add('- binary_writer_gate_closed=true')
[void]$mdLines.Add('- playable_claim_allowed=false')
[void]$mdLines.Add('- third_party_files_copied=false')
[void]$mdLines.Add('- full_file_read=false')
[void]$mdLines.Add('- max_bytes_allowed=4096')
[void]$mdLines.Add('- cell_index_understood=false')
[void]$mdLines.Add('- confidence_level=low')

$mdLines | Set-Content -Encoding UTF8 -Path $mdPath

Write-Host "igmb-cell-boundary-inspection.json -> $jsonPath"
Write-Host "igmb-cell-boundary-inspection.md   -> $mdPath"
exit 0
