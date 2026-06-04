#Requires -Version 5.1
<#
.SYNOPSIS
    Local-only chunkdata binary pattern evidence probe (MAP-4G).

    Reads bounded byte prefixes from chunkdata_*.bin files and records
    cautious byte-pattern evidence. Writes a local-only JSON and Markdown report.

    Reads ONLY chunkdata_*.bin files (bounded prefixes, not full files).
    Does NOT read .lotheader files.
    Does NOT read .lotpack files.
    Does NOT copy input files.
    Does NOT write .bin/.lotheader/.lotpack files.
    Does NOT touch repo media/maps.
    Does NOT claim playable export.
    Does NOT implement any compiled writer.
    Output must be under .local only.

Usage:
    .\scripts\inspect-chunkdata-binary-patterns.ps1 `
        -Path            <local path to mod/map root> `
        -Output          <output directory (must be under .local)> `
        -MaxFiles        <int, default 10, max 50> `
        -MaxBytesPerFile <int, default 65536, max 1048576>

Example:
    .\scripts\inspect-chunkdata-binary-patterns.ps1 `
        -Path   "C:\path\to\mod-root" `
        -Output ".local\evidence\chunkdata-patterns-01"
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

Write-Output 'inspect-chunkdata-binary-patterns.ps1'
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
    Write-Error "inspect-chunkdata-binary-patterns: refusing to write outside a .local/ directory: $outputFull"
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
# SHA-256 helper (full file, provenance only)
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
# Scan for chunkdata_*.bin files only
# ---------------------------------------------------------------------------

Write-Output '--- Scanning for chunkdata_*.bin files ---'

$chunkdataFiles = @(Get-ChildItem -LiteralPath $inputFull -Recurse -File -ErrorAction SilentlyContinue |
    Where-Object { $_.Name -like 'chunkdata_*' -and $_.Extension.ToLowerInvariant() -eq '.bin' } |
    Sort-Object FullName |
    Select-Object -First $MaxFiles)

Write-Output "  Found: $($chunkdataFiles.Count) chunkdata_*.bin files (sampling up to $MaxFiles)"
Write-Output ''

# ---------------------------------------------------------------------------
# Process each file
# ---------------------------------------------------------------------------

Write-Output "--- Analysing byte patterns (MaxBytesPerFile=$MaxBytesPerFile) ---"

$sampledRecords  = [System.Collections.Generic.List[object]]::new()
$generatedAt     = (Get-Date).ToUniversalTime().ToString('yyyy-MM-ddTHH:mm:ssZ')

foreach ($f in $chunkdataFiles) {
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

    # Header bytes as hex
    $first2Hex = if ($bytesRead -ge 2) { ($bytes[0..1] | ForEach-Object { $_.ToString('x2') }) -join '' } else { '' }
    $first4Hex = if ($bytesRead -ge 4) { ($bytes[0..3] | ForEach-Object { $_.ToString('x2') }) -join '' } else { '' }
    $first8Hex = if ($bytesRead -ge 8) { ($bytes[0..7] | ForEach-Object { $_.ToString('x2') }) -join '' } else { '' }

    # Candidate integer values
    $u16First = if ($bytesRead -ge 2) { [int][System.BitConverter]::ToUInt16($bytes, 0) } else { 0 }
    $u32First = if ($bytesRead -ge 4) { [long][System.BitConverter]::ToUInt32($bytes, 0) } else { 0 }

    # Byte distribution analysis
    $zeroCnt    = 0
    $nonzeroCnt = 0
    $byteBag    = [System.Collections.Generic.Dictionary[byte, int]]::new()
    foreach ($b in $bytes) {
        if ($b -eq 0) { $zeroCnt++ } else { $nonzeroCnt++ }
        if ($byteBag.ContainsKey($b)) { $byteBag[$b]++ } else { $byteBag[$b] = 1 }
    }
    $distinctCount = $byteBag.Count

    # Top 16 most common bytes
    $topBytes = @($byteBag.GetEnumerator() |
        Sort-Object Value -Descending |
        Select-Object -First 16 |
        ForEach-Object { [ordered]@{ byte_hex = $_.Key.ToString('x2'); count = $_.Value } })

    # First 32 nonzero byte offsets
    $nonzeroOffsets = [System.Collections.Generic.List[int]]::new()
    for ($i = 0; $i -lt $bytes.Length -and $nonzeroOffsets.Count -lt 32; $i++) {
        if ($bytes[$i] -ne 0) { $nonzeroOffsets.Add($i) }
    }

    # 16-byte prefix group key
    $groupEnd = [Math]::Min(15, $bytesRead - 1)
    $prefixGroupKey = if ($bytesRead -gt 0) {
        ($bytes[0..$groupEnd] | ForEach-Object { $_.ToString('x2') }) -join ''
    } else { 'empty' }

    $parseStatus = if ($bytesRead -lt 2) { 'too_short' } else { 'ok' }

    $record = [ordered]@{
        relative_path                  = ($rel -replace '\\', '/')
        size_bytes                     = $size
        bytes_read                     = $bytesRead
        sha256                         = $sha
        first_2_bytes_hex              = $first2Hex
        first_4_bytes_hex              = $first4Hex
        first_8_bytes_hex              = $first8Hex
        u16_first_value_le             = $u16First
        u32_first_value_le             = $u32First
        zero_byte_count_sampled        = $zeroCnt
        nonzero_byte_count_sampled     = $nonzeroCnt
        distinct_byte_count_sampled    = $distinctCount
        most_common_bytes              = $topBytes
        first_nonzero_offsets          = $nonzeroOffsets.ToArray()
        repeated_16_byte_prefix_group  = $prefixGroupKey
        parse_status                   = $parseStatus
    }
    $sampledRecords.Add($record)

    Write-Output ("  {0,-52} {1,8}b  first2={2} nonzero={3} distinct={4}" -f `
        ($rel -replace '\\', '/'), $size, $first2Hex, $nonzeroCnt, $distinctCount)
}

Write-Output ''

# ---------------------------------------------------------------------------
# Build aggregate
# ---------------------------------------------------------------------------

$allFirst2Same  = $true
$allFirst4Same  = $true
$first2Set      = [System.Collections.Generic.HashSet[string]]::new()
$first4Set      = [System.Collections.Generic.HashSet[string]]::new()
$minSz  = [long]::MaxValue
$maxSz  = [long]0
$minNz  = [int]::MaxValue
$maxNz  = [int]0

foreach ($r in $sampledRecords) {
    $first2Set.Add($r.first_2_bytes_hex) | Out-Null
    $first4Set.Add($r.first_4_bytes_hex) | Out-Null
    if ($r.size_bytes -lt $minSz) { $minSz = $r.size_bytes }
    if ($r.size_bytes -gt $maxSz) { $maxSz = $r.size_bytes }
    if ($r.nonzero_byte_count_sampled -lt $minNz) { $minNz = $r.nonzero_byte_count_sampled }
    if ($r.nonzero_byte_count_sampled -gt $maxNz) { $maxNz = $r.nonzero_byte_count_sampled }
}
$allFirst2Same = ($first2Set.Count -le 1)
$allFirst4Same = ($first4Set.Count -le 1)
if ($sampledRecords.Count -eq 0) { $minSz = 0; $minNz = 0 }

$commonFirst2 = if ($first2Set.Count -eq 1) { @($first2Set)[0] } else { 'varies' }
$commonFirst4 = if ($first4Set.Count -eq 1) { @($first4Set)[0] } else { 'varies' }

# Build interpretation notes
$notes = [System.Collections.Generic.List[string]]::new()
$notes.Add('Observations are candidate patterns only. chunkdata_*.bin format is not confirmed.')
if ($allFirst2Same -and $sampledRecords.Count -gt 1) {
    $notes.Add("First 2 bytes appear identical across all $($sampledRecords.Count) sampled files: $commonFirst2. U16 LE = $($sampledRecords[0].u16_first_value_le). Candidate format version or file type marker.")
}
if ($allFirst4Same -and $sampledRecords.Count -gt 1) {
    $notes.Add("First 4 bytes appear identical across all $($sampledRecords.Count) sampled files: $commonFirst4. Candidate fixed header.")
}
$highZeroPct = $sampledRecords | Where-Object { ($_.zero_byte_count_sampled -gt 0) -and ($_.bytes_read -gt 0) -and (($_.zero_byte_count_sampled / $_.bytes_read) -gt 0.7) }
if ($highZeroPct.Count -gt 0) {
    $notes.Add("$($highZeroPct.Count) of $($sampledRecords.Count) sampled files have >70% zero bytes in the sampled prefix. Sparse data structure or occupancy/presence bitmap likely.")
}
if ($minSz -lt $maxSz) {
    $notes.Add("File sizes vary: $minSz to $maxSz bytes. Larger files may correspond to more complex cells (more occupied chunks or additional metadata).")
}

$openQuestions = @(
    'Whether the first 2 bytes (U16 LE = 1 if consistent) encode a format version or chunk presence type.',
    'Whether the remaining bytes form a 900-slot (30x30) occupancy or metadata structure pairing with the lotpack offset table.',
    'Whether each chunkdata_*.bin file pairs with its corresponding lotpack cell by coordinate naming (chunkdata_X_Y.bin ↔ world_X_Y.lotpack).',
    'Whether chunkdata stores high-level chunk metadata (building flags, zone presence, pathfinding) rather than raw tile rendering data.',
    'Whether an empty or minimal chunkdata_*.bin file would be valid for a cell with no buildings.',
    'Whether all chunkdata_*.bin files are required or whether some may be absent for empty cells.'
)

$aggregate = [ordered]@{
    sampled_count                    = $sampledRecords.Count
    all_first_2_bytes_identical      = $allFirst2Same
    distinct_first_2_bytes           = @($first2Set)
    all_first_4_bytes_identical      = $allFirst4Same
    distinct_first_4_bytes           = @($first4Set)
    min_size_bytes                   = $minSz
    max_size_bytes                   = $maxSz
    min_nonzero_byte_count_sampled   = $minNz
    max_nonzero_byte_count_sampled   = $maxNz
    likely_interpretation_notes      = $notes.ToArray()
    open_questions                   = $openQuestions
}

# ---------------------------------------------------------------------------
# Write JSON output
# ---------------------------------------------------------------------------

New-Item -ItemType Directory -Force -Path $outputFull | Out-Null

$evidence = [ordered]@{
    schema                         = 'pzmapforge.chunkdata-binary-pattern-evidence.v0.1'
    claim_boundary                 = 'evidence_inventory_only_not_compiled_not_pz_load_tested'
    generated_at_utc               = $generatedAt
    input_path                     = ($inputFull -replace '\\', '/')
    max_files                      = $MaxFiles
    max_bytes_per_file             = $MaxBytesPerFile
    copied_input_files             = $false
    pz_assets_copied               = $false
    media_maps_touched             = $false
    playable_export_claimed        = $false
    compiled_writer_implemented    = $false
    only_chunkdata_bin_files_read  = $true
    lotheader_files_read           = $false
    lotpack_files_read             = $false
    bin_files_written              = $false
    sampled_files                  = $sampledRecords.ToArray()
    aggregate                      = $aggregate
}

$jsonPath = Join-Path $outputFull 'chunkdata-binary-pattern-evidence.json'
$evidence | ConvertTo-Json -Depth 5 | Set-Content -Path $jsonPath -Encoding UTF8

# ---------------------------------------------------------------------------
# Write Markdown output
# ---------------------------------------------------------------------------

$sampledRows = ($sampledRecords | ForEach-Object {
    "| ``$($_.relative_path.Split('/')[-1])`` | $($_.size_bytes) | $($_.first_2_bytes_hex) | $($_.first_4_bytes_hex) | $($_.nonzero_byte_count_sampled) | $($_.distinct_byte_count_sampled) | $($_.parse_status) |"
}) -join "`n"

$notesList  = ($aggregate.likely_interpretation_notes | ForEach-Object { "- $_" }) -join "`n"
$qList      = ($aggregate.open_questions | ForEach-Object { "- $_" }) -join "`n"

$nonzeroSection = ''
foreach ($r in $sampledRecords) {
    $name = $r.relative_path.Split('/')[-1]
    $offsets = if ($r.first_nonzero_offsets.Count -gt 0) {
        ($r.first_nonzero_offsets | Select-Object -First 16 | ForEach-Object { "$_" }) -join ', '
    } else { '(all zero in sample)' }
    $nonzeroSection += "`n**$name** ($($r.size_bytes)b): first_nonzero_offsets = $offsets`n"
}
if ($nonzeroSection -eq '') { $nonzeroSection = "`n(no files sampled)`n" }

$md = @"
# Chunkdata Binary Pattern Evidence

Schema:             pzmapforge.chunkdata-binary-pattern-evidence.v0.1
Claim boundary:     evidence_inventory_only_not_compiled_not_pz_load_tested
Generated:          $generatedAt
Input path:         $($inputFull -replace '\\', '/')
Max files:          $MaxFiles
Max bytes/file:     $MaxBytesPerFile

## Sampled files

| File | Bytes | First 2 | First 4 | Nonzero bytes | Distinct bytes | Status |
|---|---:|---|---|---:|---:|---|
$sampledRows

## Header consistency

| Field | Value |
|---|---|
| All first 2 bytes identical | $($aggregate.all_first_2_bytes_identical) |
| All first 4 bytes identical | $($aggregate.all_first_4_bytes_identical) |
| First 2 bytes common value | $commonFirst2 |
| First 4 bytes common value | $commonFirst4 |
| Size range | $minSz to $maxSz bytes |
| Nonzero byte count range | $minNz to $maxNz bytes |

## Candidate interpretation notes

$notesList

## First nonzero byte offsets (per file)
$nonzeroSection

## Open questions

$qList

## Non-claims

- Only chunkdata_*.bin prefixes read (bounded, not full files).
- No .lotheader files read.
- No .lotpack files read.
- No binary files copied.
- No binary files written.
- No compiled writer implemented.
- No playable export claimed.
- No repo media/maps writes.
- Output is under .local only.
"@

$mdPath = Join-Path $outputFull 'chunkdata-binary-pattern-evidence.md'
Set-Content -Path $mdPath -Value $md -Encoding UTF8

# ---------------------------------------------------------------------------
# Summary
# ---------------------------------------------------------------------------

Write-Output "Evidence JSON:               $jsonPath"
Write-Output "Evidence MD:                 $mdPath"
Write-Output "Sampled files:               $($sampledRecords.Count)"
Write-Output "All first-2-bytes identical: $allFirst2Same"
Write-Output "All first-4-bytes identical: $allFirst4Same"
Write-Output 'only_chunkdata_bin_files_read: true'
Write-Output 'lotheader_files_read:        false'
Write-Output 'lotpack_files_read:          false'
Write-Output 'bin_files_written:           false'
Write-Output 'copied_input_files:          false'
Write-Output 'pz_assets_copied:            false'
Write-Output 'compiled_writer_implemented: false'
Write-Output 'Status:                      OK'
