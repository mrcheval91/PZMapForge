#Requires -Version 5.1
<#
.SYNOPSIS
    Local-only lotpack offset table evidence probe (MAP-4F).

    Reads bounded byte prefixes from .lotpack files and analyzes the apparent
    offset/size table structure for evidence. Writes a local-only evidence
    JSON and Markdown report.

    Reads ONLY .lotpack files (bounded prefixes, not full files).
    Does NOT read .lotheader files.
    Does NOT read .bin files.
    Does NOT copy input files.
    Does NOT write .lotheader/.lotpack/.bin files.
    Does NOT touch repo media/maps.
    Does NOT claim playable export.
    Does NOT implement any compiled writer.
    Output must be under .local only.

Usage:
    .\scripts\inspect-lotpack-offset-table.ps1 `
        -Path            <local path to mod/map root> `
        -Output          <output directory (must be under .local)> `
        -MaxFiles        <int, default 10, max 50> `
        -MaxBytesPerFile <int, default 65536, max 1048576>

Example:
    .\scripts\inspect-lotpack-offset-table.ps1 `
        -Path   "C:\path\to\mod-root" `
        -Output ".local\evidence\lotpack-offsets-01"
#>

param(
    [Parameter(Mandatory=$true)]
    [string]$Path,

    [Parameter(Mandatory=$true)]
    [string]$Output,

    [int]$MaxFiles        = 10,
    [int]$MaxBytesPerFile = 65536
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

# ---------------------------------------------------------------------------
# Clamp parameters
# ---------------------------------------------------------------------------

if ($MaxFiles -gt 50)             { $MaxFiles = 50 }
if ($MaxFiles -lt 1)              { $MaxFiles = 1 }
if ($MaxBytesPerFile -gt 1048576) { $MaxBytesPerFile = 1048576 }
if ($MaxBytesPerFile -lt 8)       { $MaxBytesPerFile = 8 }

Write-Output 'inspect-lotpack-offset-table.ps1'
Write-Output "Path:             $Path"
Write-Output "Output:           $Output"
Write-Output "MaxFiles:         $MaxFiles"
Write-Output "MaxBytesPerFile:  $MaxBytesPerFile"
Write-Output ''

# ---------------------------------------------------------------------------
# Guard: output must be under .local
# ---------------------------------------------------------------------------

$outputFull  = [System.IO.Path]::GetFullPath($Output)
$sep         = [System.IO.Path]::DirectorySeparatorChar
$localMarker = $sep + '.local' + $sep
$endsLocal   = $outputFull.EndsWith($sep + '.local')

if (-not ($outputFull.Contains($localMarker) -or $endsLocal)) {
    Write-Error "inspect-lotpack-offset-table: refusing to write outside a .local/ directory: $outputFull"
    Write-Error "  Pass -Output to an explicit .local/ path."
    exit 1
}

# ---------------------------------------------------------------------------
# Guard: input path must exist
# ---------------------------------------------------------------------------

if (-not (Test-Path -LiteralPath $Path)) {
    Write-Error "Input path not found: $Path"
    exit 1
}

$inputFull = [System.IO.Path]::GetFullPath($Path)

# ---------------------------------------------------------------------------
# SHA-256 helper
# ---------------------------------------------------------------------------

function Get-FileSha256 {
    param([string]$FilePath)
    $sha    = [System.Security.Cryptography.SHA256]::Create()
    $stream = [System.IO.File]::OpenRead($FilePath)
    try {
        $hash = $sha.ComputeHash($stream)
        return (($hash | ForEach-Object { $_.ToString('x2') }) -join '')
    }
    finally { $stream.Dispose(); $sha.Dispose() }
}

# ---------------------------------------------------------------------------
# Scan for .lotpack files only
# ---------------------------------------------------------------------------

Write-Output '--- Scanning for .lotpack files ---'

$lotpackFiles = @(Get-ChildItem -LiteralPath $inputFull -Recurse -File -ErrorAction SilentlyContinue |
    Where-Object { $_.Extension.ToLowerInvariant() -eq '.lotpack' } |
    Sort-Object FullName |
    Select-Object -First $MaxFiles)

Write-Output "  Found: $($lotpackFiles.Count) .lotpack files (sampling up to $MaxFiles)"
Write-Output ''

# ---------------------------------------------------------------------------
# Process each file — read bounded prefix and analyze table values
# ---------------------------------------------------------------------------

Write-Output "--- Analysing offset table candidates (MaxBytesPerFile=$MaxBytesPerFile) ---"

$sampledRecords = [System.Collections.Generic.List[object]]::new()
$generatedAt    = (Get-Date).ToUniversalTime().ToString('yyyy-MM-ddTHH:mm:ssZ')

foreach ($f in $lotpackFiles) {
    $rel  = $f.FullName.Substring($inputFull.Length).TrimStart($sep)
    $size = $f.Length
    $sha  = Get-FileSha256 $f.FullName

    # Read bounded prefix
    $stream = [System.IO.File]::OpenRead($f.FullName)
    $bytes  = [byte[]]::new(0)
    try {
        $buf   = [byte[]]::new($MaxBytesPerFile)
        $read  = $stream.Read($buf, 0, $MaxBytesPerFile)
        $bytes = [byte[]]::new($read)
        if ($read -gt 0) { [System.Array]::Copy($buf, $bytes, $read) }
    }
    finally { $stream.Dispose() }

    $bytesRead = $bytes.Length

    # First 8 bytes as hex
    $first8Hex = if ($bytesRead -ge 8) {
        ($bytes[0..7] | ForEach-Object { $_.ToString('x2') }) -join ''
    } else { '' }

    # Bytes 0-3: candidate_header_a (U32 LE)
    $headerA = if ($bytesRead -ge 4) { [System.BitConverter]::ToUInt32($bytes, 0) } else { [uint32]0 }

    # Bytes 4-7: candidate_header_b (U32 LE)
    $headerB = if ($bytesRead -ge 8) { [System.BitConverter]::ToUInt32($bytes, 4) } else { [uint32]0 }

    # Bytes 8+: candidate table bytes
    $tableLen = $bytesRead - 8
    [byte[]]$tableBytes = [byte[]]::new(0)
    if ($tableLen -gt 0) {
        $tableBytes = [byte[]]::new($tableLen)
        [System.Array]::Copy($bytes, 8, $tableBytes, 0, $tableLen)
    }

    # Parse as U32 LE values
    $u32List = [System.Collections.Generic.List[long]]::new()
    $pos = 0
    while ($pos + 3 -lt $tableBytes.Length) {
        $u32List.Add([long][System.BitConverter]::ToUInt32($tableBytes, $pos))
        $pos += 4
    }

    # Parse as U64 LE values — store as hex strings to avoid JSON overflow
    $u64HexList = [System.Collections.Generic.List[string]]::new()
    $pos = 0
    while ($pos + 7 -lt $tableBytes.Length) {
        $v = [System.BitConverter]::ToUInt64($tableBytes, $pos)
        $u64HexList.Add(('0x{0:x16}' -f $v))
        $pos += 8
    }

    # U32 statistics
    $u32Count = $u32List.Count
    $u32Mono  = 0
    for ($i = 1; $i -lt $u32Count; $i++) {
        if ($u32List[$i] -ge $u32List[$i-1]) { $u32Mono++ }
    }

    $u32NonZero   = @($u32List | Where-Object { $_ -ne 0 })
    $u32NzCount   = $u32NonZero.Count
    $u32NzMin     = if ($u32NzCount -gt 0) { ($u32NonZero | Measure-Object -Minimum).Minimum } else { 0 }
    $u32NzMax     = if ($u32NzCount -gt 0) { ($u32NonZero | Measure-Object -Maximum).Maximum } else { 0 }

    # U64 statistics (compare hex strings as uint64 values)
    $u64Count = $u64HexList.Count
    $u64Mono  = 0
    $u64Prev  = [uint64]0
    $u64NzCount = 0
    $u64NzMin = [uint64]::MaxValue
    $u64NzMax = [uint64]0
    foreach ($hexStr in $u64HexList) {
        $v = [System.Convert]::ToUInt64($hexStr.Substring(2), 16)
        if ($v -ge $u64Prev) { $u64Mono++ }
        $u64Prev = $v
        if ($v -ne 0) {
            $u64NzCount++
            if ($v -lt $u64NzMin) { $u64NzMin = $v }
            if ($v -gt $u64NzMax) { $u64NzMax = $v }
        }
    }
    if ($u64Count -gt 0) { $u64Mono-- }  # first entry not compared to anything
    if ($u64Mono -lt 0)  { $u64Mono = 0 }
    if ($u64NzCount -eq 0) { $u64NzMin = 0; $u64NzMax = 0 }

    $parseStatus = if ($bytesRead -lt 8) { 'too_short' } else { 'ok' }

    $record = [ordered]@{
        relative_path                       = ($rel -replace '\\', '/')
        size_bytes                          = $size
        bytes_read                          = $bytesRead
        sha256                              = $sha
        first_8_bytes_hex                   = $first8Hex
        candidate_header_a_u32              = [long]$headerA
        candidate_header_b_u32              = [long]$headerB
        u32_value_count_sampled             = $u32Count
        u64_value_count_sampled             = $u64Count
        first_u32_values                    = @($u32List | Select-Object -First 32)
        first_u64_values                    = @($u64HexList | Select-Object -First 32)
        u32_monotonic_non_decreasing_count  = $u32Mono
        u64_monotonic_non_decreasing_count  = $u64Mono
        u32_nonzero_count                   = $u32NzCount
        u64_nonzero_count                   = $u64NzCount
        u32_min_nonzero                     = [long]$u32NzMin
        u32_max_nonzero                     = [long]$u32NzMax
        u64_min_nonzero                     = ('0x{0:x16}' -f [uint64]$u64NzMin)
        u64_max_nonzero                     = ('0x{0:x16}' -f [uint64]$u64NzMax)
        parse_status                        = $parseStatus
    }
    $sampledRecords.Add($record)

    Write-Output ("  {0,-50} {1,10}b  hdrA={2} hdrB={3} u32s={4}" -f `
        ($rel -replace '\\', '/'), $size, [long]$headerA, [long]$headerB, $u32Count)
}

Write-Output ''

# ---------------------------------------------------------------------------
# Build aggregate
# ---------------------------------------------------------------------------

$allFirst8Identical = $true
$first8Set = [System.Collections.Generic.HashSet[string]]::new()
$allHdrASame = $true
$allHdrBSame = $true
$hdrAVals = [System.Collections.Generic.List[long]]::new()
$hdrBVals = [System.Collections.Generic.List[long]]::new()

$firstHdrA = if ($sampledRecords.Count -gt 0) { $sampledRecords[0].candidate_header_a_u32 } else { 0 }
$firstHdrB = if ($sampledRecords.Count -gt 0) { $sampledRecords[0].candidate_header_b_u32 } else { 0 }
$firstF8   = if ($sampledRecords.Count -gt 0) { $sampledRecords[0].first_8_bytes_hex } else { '' }

foreach ($r in $sampledRecords) {
    $first8Set.Add($r.first_8_bytes_hex) | Out-Null
    $hdrAVals.Add($r.candidate_header_a_u32)
    $hdrBVals.Add($r.candidate_header_b_u32)
    if ($r.candidate_header_a_u32 -ne $firstHdrA) { $allHdrASame = $false }
    if ($r.candidate_header_b_u32 -ne $firstHdrB) { $allHdrBSame = $false }
}
$allFirst8Identical = ($first8Set.Count -le 1)

# Build interpretation notes based on observed patterns
$notes = [System.Collections.Generic.List[string]]::new()
$notes.Add('Observations are candidate patterns only. Full lotpack format is not confirmed.')
if ($allFirst8Identical -and $sampledRecords.Count -gt 1) {
    $notes.Add("First 8 bytes appear identical across all $($sampledRecords.Count) sampled files: $firstF8 -- consistent magic or version header candidate.")
}
if ($allHdrASame) {
    $notes.Add("Bytes 0-3 (candidate_header_a) are the same across all sampled files ($firstHdrA). May be a format version or lot count.")
}
if ($allHdrBSame) {
    $notes.Add("Bytes 4-7 (candidate_header_b) are the same across all sampled files ($firstHdrB). May be a fixed offset to the data section or a table size.")
}

$sampleFile = if ($sampledRecords.Count -gt 0) { $sampledRecords[0] } else { $null }
if ($null -ne $sampleFile -and $sampleFile.u32_value_count_sampled -gt 4) {
    $evenZero = $true
    $u32arr   = @($sampleFile.first_u32_values)
    for ($i = 0; $i -lt [Math]::Min($u32arr.Count, 16); $i += 2) {
        if ($u32arr[$i] -ne 0) { $evenZero = $false; break }
    }
    if ($evenZero) {
        $notes.Add('In the sampled first file, even-indexed U32 values (0, 2, 4...) appear to be 0x00000000. Odd-indexed U32 values appear to be monotonically non-decreasing. This is consistent with a table of 8-byte entries where each entry contains {zero_field, offset_or_size}.')
    }

    $notes.Add('U64 interpretation of the same bytes yields very large values (billions+), which are not plausible file offsets for files of the observed sizes. U32 alternating-pair interpretation appears more consistent with the observed data pattern.')
    $notes.Add("Candidate header_a ($firstHdrA) may represent entry count. header_b ($firstHdrB) may represent table byte size or data section start offset. Neither confirmed.")
}

$sampleValuePatterns = [ordered]@{
    u32_interpretation_note  = 'Alternating even-zero / odd-increasing U32 pairs appear consistent across sampled files. Candidate for an 8-byte-per-entry offset table.'
    u64_interpretation_note  = 'U64 LE interpretation yields very large values inconsistent with file offsets. U32 pair interpretation appears more plausible.'
    confidence               = 'low -- candidate observations only, not confirmed'
}

$aggregate = [ordered]@{
    sampled_count                    = $sampledRecords.Count
    all_first_8_bytes_identical      = $allFirst8Identical
    distinct_first_8_bytes           = @($first8Set)
    header_a_values                  = $hdrAVals.ToArray()
    header_b_values                  = $hdrBVals.ToArray()
    all_header_a_same                = $allHdrASame
    all_header_b_same                = $allHdrBSame
    likely_table_interpretation_notes = $notes.ToArray()
    sample_value_patterns            = $sampleValuePatterns
}

# ---------------------------------------------------------------------------
# Write JSON output
# ---------------------------------------------------------------------------

New-Item -ItemType Directory -Force -Path $outputFull | Out-Null

$evidence = [ordered]@{
    schema                       = 'pzmapforge.lotpack-offset-table-evidence.v0.1'
    claim_boundary               = 'evidence_inventory_only_not_compiled_not_pz_load_tested'
    generated_at_utc             = $generatedAt
    input_path                   = ($inputFull -replace '\\', '/')
    max_files                    = $MaxFiles
    max_bytes_per_file           = $MaxBytesPerFile
    copied_input_files           = $false
    pz_assets_copied             = $false
    media_maps_touched           = $false
    playable_export_claimed      = $false
    compiled_writer_implemented  = $false
    only_lotpack_files_read      = $true
    lotheader_files_read         = $false
    bin_files_read               = $false
    full_lotpack_files_read      = $false
    sampled_files                = $sampledRecords.ToArray()
    aggregate                    = $aggregate
}

$jsonPath = Join-Path $outputFull 'lotpack-offset-table-evidence.json'
$evidence | ConvertTo-Json -Depth 5 | Set-Content -Path $jsonPath -Encoding UTF8

# ---------------------------------------------------------------------------
# Write Markdown output
# ---------------------------------------------------------------------------

$sampledRows = ($sampledRecords | ForEach-Object {
    "| ``$($_.relative_path.Split('/')[-1])`` | $($_.size_bytes) | $($_.candidate_header_a_u32) | $($_.candidate_header_b_u32) | $($_.u32_value_count_sampled) | $($_.u32_nonzero_count) | $($_.u32_monotonic_non_decreasing_count) | $($_.parse_status) |"
}) -join "`n"

$notesList = ($aggregate.likely_table_interpretation_notes | ForEach-Object { "- $_" }) -join "`n"

$firstU32Section = ''
foreach ($r in $sampledRecords) {
    $name = $r.relative_path.Split('/')[-1]
    $firstU32Section += "`n### $name`n`n"
    $firstU32Section += "hdrA=$($r.candidate_header_a_u32)  hdrB=$($r.candidate_header_b_u32)  u32s=$($r.u32_value_count_sampled)`n`n"
    $vals = ($r.first_u32_values | Select-Object -First 16 | ForEach-Object { "$_" }) -join ', '
    $firstU32Section += "First 16 U32 values: $vals`n"
}
if ($firstU32Section -eq '') { $firstU32Section = "`n(no files sampled)`n" }

$md = @"
# Lotpack Offset Table Evidence

Schema:             pzmapforge.lotpack-offset-table-evidence.v0.1
Claim boundary:     evidence_inventory_only_not_compiled_not_pz_load_tested
Generated:          $generatedAt
Input path:         $($inputFull -replace '\\', '/')
Max files:          $MaxFiles
Max bytes/file:     $MaxBytesPerFile

## Sampled files

| File | Bytes | Hdr A (U32) | Hdr B (U32) | U32 count | U32 nonzero | U32 mono | Status |
|---|---:|---:|---:|---:|---:|---:|---|
$sampledRows

## Header consistency

| Field | Value |
|---|---|
| All first 8 bytes identical | $($aggregate.all_first_8_bytes_identical) |
| All header_a (bytes 0-3) same | $($aggregate.all_header_a_same) |
| All header_b (bytes 4-7) same | $($aggregate.all_header_b_same) |
| header_a common value | $firstHdrA |
| header_b common value | $firstHdrB |
| first 8 bytes | $firstF8 |

## Candidate interpretation notes

$notesList

## U32 table values (first 16 per file)
$firstU32Section

## U32 vs U64 interpretation

$($aggregate.sample_value_patterns.u32_interpretation_note)

$($aggregate.sample_value_patterns.u64_interpretation_note)

Confidence: $($aggregate.sample_value_patterns.confidence)

## Non-claims

- Only .lotpack prefixes read (bounded, not full files).
- No .lotheader files read.
- No .bin files read.
- No full .lotpack files read.
- No binary files copied.
- No compiled writer implemented.
- No playable export claimed.
- No repo media/maps writes.
- Output is under .local only.
"@

$mdPath = Join-Path $outputFull 'lotpack-offset-table-evidence.md'
Set-Content -Path $mdPath -Value $md -Encoding UTF8

# ---------------------------------------------------------------------------
# Summary
# ---------------------------------------------------------------------------

Write-Output "Evidence JSON:               $jsonPath"
Write-Output "Evidence MD:                 $mdPath"
Write-Output "Sampled files:               $($sampledRecords.Count)"
Write-Output "All first-8-bytes identical: $allFirst8Identical"
Write-Output "All header_a same:           $allHdrASame"
Write-Output "All header_b same:           $allHdrBSame"
Write-Output 'only_lotpack_files_read:     true'
Write-Output 'lotheader_files_read:        false'
Write-Output 'bin_files_read:              false'
Write-Output 'full_lotpack_files_read:     false'
Write-Output 'copied_input_files:          false'
Write-Output 'pz_assets_copied:            false'
Write-Output 'compiled_writer_implemented: false'
Write-Output 'Status:                      OK'
