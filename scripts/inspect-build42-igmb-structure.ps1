[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$ReferenceWorldmapBinPath,
    [string]$CandidateWorldmapBinPath = '',
    [Parameter(Mandatory)][string]$Output,
    [int]$MaxBytes = 4096
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

$schema = 'pzmapforge.map8q-igmb-structure-inspection.v0.1'

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

function IsPrintable([byte]$b) { return $b -ge 0x20 -and $b -le 0x7E }

$ref = Read-BoundedBytes $ReferenceWorldmapBinPath $MaxBytes
$buf = $ref.buf

# U32LE values from first 64 bytes (up to 16 values)
$u32List = [System.Collections.ArrayList]::new()
$u32Limit = [int][Math]::Min(16, [int][Math]::Floor($buf.Count / 4))
for ($i = 0; $i -lt $u32Limit; $i++) {
    $off = $i * 4
    $v = [int]($buf[$off]) -bor ([int]($buf[$off+1]) -shl 8) -bor ([int]($buf[$off+2]) -shl 16) -bor ([int]($buf[$off+3]) -shl 24)
    [void]$u32List.Add($v)
}

# U16LE values from first 64 bytes (up to 32 values)
$u16List = [System.Collections.ArrayList]::new()
$u16Limit = [int][Math]::Min(32, [int][Math]::Floor($buf.Count / 2))
for ($i = 0; $i -lt $u16Limit; $i++) {
    $off = $i * 2
    $v = [int]($buf[$off]) -bor ([int]($buf[$off+1]) -shl 8)
    [void]$u16List.Add($v)
}

# Version field: bytes 4-7 as U32LE
$versionLeU32 = if ($buf.Count -ge 8) {
    [int]($buf[4]) -bor ([int]($buf[5]) -shl 8) -bor ([int]($buf[6]) -shl 16) -bor ([int]($buf[7]) -shl 24)
} else { -1 }

# Printable ASCII runs >= 3 in full read window
$asciiRuns = [System.Collections.ArrayList]::new()
$runStart = -1
$runChars = [System.Text.StringBuilder]::new()
for ($i = 0; $i -lt $buf.Count; $i++) {
    if (IsPrintable $buf[$i]) {
        if ($runStart -lt 0) { $runStart = $i }
        [void]$runChars.Append([char]$buf[$i])
    } else {
        if ($runChars.Length -ge 3) {
            [void]$asciiRuns.Add([ordered]@{
                offset = $runStart
                length = $runChars.Length
                value  = $runChars.ToString()
            })
        }
        $runStart = -1
        [void]$runChars.Clear()
    }
}
if ($runChars.Length -ge 3) {
    [void]$asciiRuns.Add([ordered]@{
        offset = $runStart
        length = $runChars.Length
        value  = $runChars.ToString()
    })
}

# U16LE length-prefixed string scan across full read window
$lpStrings = [System.Collections.ArrayList]::new()
for ($i = 0; $i -le ($buf.Count - 3); $i++) {
    $len = [int]($buf[$i]) -bor ([int]($buf[$i+1]) -shl 8)
    if ($len -ge 3 -and $len -le 128 -and ($i + 2 + $len) -le $buf.Count) {
        $allPrint = $true
        $sb = [System.Text.StringBuilder]::new()
        for ($j = 0; $j -lt $len; $j++) {
            if (-not (IsPrintable $buf[$i+2+$j])) { $allPrint = $false; break }
            [void]$sb.Append([char]$buf[$i+2+$j])
        }
        if ($allPrint) {
            [void]$lpStrings.Add([ordered]@{
                offset    = $i
                len_field = $len
                value     = $sb.ToString()
            })
        }
    }
}

# Possible string pool offset candidates
$spOffsets = [System.Collections.ArrayList]::new()
foreach ($entry in $lpStrings) {
    if (-not $spOffsets.Contains($entry.offset)) {
        [void]$spOffsets.Add($entry.offset)
    }
}

# Possible header fields (bytes before string pool)
$headerFields = [System.Collections.ArrayList]::new()
if ($buf.Count -ge 8)  { [void]$headerFields.Add("bytes_4_7_u32le=$versionLeU32 (possible_version)") }
if ($buf.Count -ge 12) {
    $f = [int]($buf[8]) -bor ([int]($buf[9]) -shl 8) -bor ([int]($buf[10]) -shl 16) -bor ([int]($buf[11]) -shl 24)
    [void]$headerFields.Add("bytes_8_11_u32le=$f (possible_count_or_size)")
}
if ($buf.Count -ge 16) {
    $f = [int]($buf[12]) -bor ([int]($buf[13]) -shl 8) -bor ([int]($buf[14]) -shl 16) -bor ([int]($buf[15]) -shl 24)
    [void]$headerFields.Add("bytes_12_15_u32le=$f (possible_count_or_offset)")
}
if ($buf.Count -ge 20) {
    $f = [int]($buf[16]) -bor ([int]($buf[17]) -shl 8) -bor ([int]($buf[18]) -shl 16) -bor ([int]($buf[19]) -shl 24)
    [void]$headerFields.Add("bytes_16_19_u32le=$f (possible_offset_or_size)")
}
if ($buf.Count -ge 24) {
    $f = [int]($buf[20]) -bor ([int]($buf[21]) -shl 8) -bor ([int]($buf[22]) -shl 16) -bor ([int]($buf[23]) -shl 24)
    [void]$headerFields.Add("bytes_20_23_u32le=$f (possible_count_or_size)")
}

# Unverified hypotheses
$hypotheses = [System.Collections.ArrayList]::new()
[void]$hypotheses.Add('IGMB_is_custom_pz_worldmap_binary_format')
[void]$hypotheses.Add('string_pool_may_start_near_offset_24')
[void]$hypotheses.Add('u16le_length_prefixed_strings_observed')
[void]$hypotheses.Add('header_contains_multiple_u32le_fields_before_string_pool')
[void]$hypotheses.Add('format_likely_little_endian_throughout')
[void]$hypotheses.Add('full_format_not_understood_from_4096_bytes_alone')

$result = [ordered]@{
    schema                                  = $schema
    reference_present                       = $ref.present
    reference_size_bytes                    = $ref.size_bytes
    bytes_read_count                        = $ref.bytes_read_count
    max_bytes_allowed                       = 4096
    full_file_read                          = $false
    magic                                   = 'IGMB'
    version_le_u32                          = $versionLeU32
    candidate_u32_values_first_64_le        = $u32List
    candidate_u16_values_first_64_le        = $u16List
    printable_ascii_runs_min_length_3       = $asciiRuns
    possible_length_prefixed_strings        = $lpStrings
    possible_string_pool_offset_candidates  = $spOffsets
    possible_string_pool_count_candidates   = [int]$lpStrings.Count
    possible_header_fields_observed_only    = $headerFields
    unverified_format_hypotheses            = $hypotheses
    confidence_level                        = 'low_to_medium'
    binary_writer_gate_closed               = $true
    playable_claim_allowed                  = $false
    third_party_files_copied                = $false
    next_branch                             = 'igmb_minimal_encoder_design_pending_operator_approval_if_structure_sufficient'
}

$jsonPath = Join-Path $outDir 'igmb-structure-inspection.json'
$mdPath   = Join-Path $outDir 'igmb-structure-inspection.md'

$result | ConvertTo-Json -Depth 6 | Set-Content -Encoding UTF8 -Path $jsonPath

$mdLines = [System.Collections.ArrayList]::new()
[void]$mdLines.Add('# MAP-8Q: IGMB Structure Inspection')
[void]$mdLines.Add('')
[void]$mdLines.Add("Schema: ``$schema``")
[void]$mdLines.Add('')
[void]$mdLines.Add("Reference present: $($ref.present)")
[void]$mdLines.Add("Reference size bytes: $($ref.size_bytes)")
[void]$mdLines.Add("Bytes read: $($ref.bytes_read_count) (max_bytes_allowed=4096)")
[void]$mdLines.Add("Version U32LE: $versionLeU32")
[void]$mdLines.Add('')
[void]$mdLines.Add('## U32LE values from first 64 bytes')
[void]$mdLines.Add('')
for ($i = 0; $i -lt $u32List.Count; $i++) {
    [void]$mdLines.Add("  offset $($i*4): $($u32List[$i])")
}
[void]$mdLines.Add('')
[void]$mdLines.Add('## Possible U16LE length-prefixed strings')
[void]$mdLines.Add('')
foreach ($s in $lpStrings) {
    [void]$mdLines.Add("  offset $($s.offset): len=$($s.len_field) value=`"$($s.value)`"")
}
[void]$mdLines.Add('')
[void]$mdLines.Add('## Printable ASCII runs (length >= 3)')
[void]$mdLines.Add('')
foreach ($run in $asciiRuns) {
    [void]$mdLines.Add("  offset $($run.offset): len=$($run.length) value=`"$($run.value)`"")
}
[void]$mdLines.Add('')
[void]$mdLines.Add('## Possible header fields (observed, not confirmed)')
[void]$mdLines.Add('')
foreach ($hf in $headerFields) {
    [void]$mdLines.Add("  $hf")
}
[void]$mdLines.Add('')
[void]$mdLines.Add('## Unverified hypotheses')
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

$mdLines | Set-Content -Encoding UTF8 -Path $mdPath

Write-Host "igmb-structure-inspection.json -> $jsonPath"
Write-Host "igmb-structure-inspection.md   -> $mdPath"
exit 0
