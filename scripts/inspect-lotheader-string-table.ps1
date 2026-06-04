#Requires -Version 5.1
<#
.SYNOPSIS
    Local-only lotheader string table evidence probe (MAP-4E).

    Reads .lotheader files from an operator-provided map/mod path, extracts
    the candidate tileset string table (bytes 8+), and writes a local-only
    evidence JSON and Markdown report.

    Reads ONLY .lotheader files.
    Does NOT read .lotpack files.
    Does NOT read .bin files.
    Does NOT copy input files.
    Does NOT write .lotheader/.lotpack/.bin files.
    Does NOT touch repo media/maps.
    Does NOT claim playable export.
    Does NOT implement any compiled writer.
    Output must be under .local only.

Usage:
    .\scripts\inspect-lotheader-string-table.ps1 `
        -Path           <local path to mod/map root> `
        -Output         <output directory (must be under .local)> `
        -MaxFiles       <int, default 10, max 50> `
        -MaxBytesPerFile <int, default 131072, max 1048576>

Example:
    .\scripts\inspect-lotheader-string-table.ps1 `
        -Path   "C:\path\to\mod-root" `
        -Output ".local\evidence\lotheader-strings-01"
#>

param(
    [Parameter(Mandatory=$true)]
    [string]$Path,

    [Parameter(Mandatory=$true)]
    [string]$Output,

    [int]$MaxFiles        = 10,
    [int]$MaxBytesPerFile = 131072
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

# ---------------------------------------------------------------------------
# Clamp parameters
# ---------------------------------------------------------------------------

if ($MaxFiles -gt 50)          { $MaxFiles = 50 }
if ($MaxFiles -lt 1)           { $MaxFiles = 1 }
if ($MaxBytesPerFile -gt 1048576) { $MaxBytesPerFile = 1048576 }
if ($MaxBytesPerFile -lt 8)    { $MaxBytesPerFile = 8 }

Write-Output 'inspect-lotheader-string-table.ps1'
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
    Write-Error "inspect-lotheader-string-table: refusing to write outside a .local/ directory: $outputFull"
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
# SHA-256 helper (full file hash for provenance)
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
# Scan for .lotheader files only
# ---------------------------------------------------------------------------

Write-Output '--- Scanning for .lotheader files ---'

$lotheaderFiles = @(Get-ChildItem -LiteralPath $inputFull -Recurse -File -ErrorAction SilentlyContinue |
    Where-Object { $_.Extension.ToLowerInvariant() -eq '.lotheader' } |
    Sort-Object FullName |
    Select-Object -First $MaxFiles)

Write-Output "  Found: $($lotheaderFiles.Count) .lotheader files (sampling up to $MaxFiles)"
Write-Output ''

# ---------------------------------------------------------------------------
# Process each file
# ---------------------------------------------------------------------------

Write-Output "--- Parsing string tables (MaxBytesPerFile=$MaxBytesPerFile) ---"

$sampledRecords     = [System.Collections.Generic.List[object]]::new()
$distinctAllEntries = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::OrdinalIgnoreCase)
$generatedAt        = (Get-Date).ToUniversalTime().ToString('yyyy-MM-ddTHH:mm:ssZ')

foreach ($f in $lotheaderFiles) {
    $rel  = $f.FullName.Substring($inputFull.Length).TrimStart($sep)
    $size = $f.Length
    $sha  = Get-FileSha256 $f.FullName

    # Read at most MaxBytesPerFile bytes
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

    # bytes 0-3: header zero field
    $headerZeroHex = if ($bytesRead -ge 4) {
        ($bytes[0..3] | ForEach-Object { $_.ToString('x2') }) -join ''
    } else { '' }

    # bytes 4-7: candidate entry count (32-bit LE UInt32)
    $candidateCount = [uint32]0
    if ($bytesRead -ge 8) {
        $candidateCount = [System.BitConverter]::ToUInt32($bytes, 4)
    }

    # bytes 8+: candidate string table
    $tableLen = $bytesRead - 8
    [byte[]]$tableBytes = [byte[]]::new(0)
    if ($tableLen -gt 0) {
        $tableBytes = [byte[]]::new($tableLen)
        [System.Array]::Copy($bytes, 8, $tableBytes, 0, $tableLen)
    }

    # Count non-printable bytes in table (excluding structural LF/CR/NUL)
    $nonPrint = 0
    foreach ($b in $tableBytes) {
        if ($b -ne 0x0A -and $b -ne 0x0D -and $b -ne 0x00 -and ($b -lt 0x20 -or $b -gt 0x7E)) {
            $nonPrint++
        }
    }

    # Split on LF (0x0A), extract printable ASCII entries
    $entries  = [System.Collections.Generic.List[string]]::new()
    $segStart = 0
    for ($i = 0; $i -lt $tableBytes.Length; $i++) {
        if ($tableBytes[$i] -eq 0x0A) {
            $segLen = $i - $segStart
            if ($segLen -gt 0) {
                $seg     = [byte[]]::new($segLen)
                [System.Array]::Copy($tableBytes, $segStart, $seg, 0, $segLen)
                $trimList = [System.Collections.Generic.List[byte]]::new()
                foreach ($b in $seg) {
                    if ($b -ne 0x0D -and $b -ne 0x00) { $trimList.Add($b) }
                }
                if ($trimList.Count -gt 0) {
                    $hasBad = $false
                    foreach ($b in $trimList) {
                        if ($b -lt 0x20 -or $b -gt 0x7E) { $hasBad = $true; break }
                    }
                    if (-not $hasBad) {
                        $str = [System.Text.Encoding]::ASCII.GetString($trimList.ToArray())
                        if ($str.Length -gt 0) { $entries.Add($str) }
                    }
                }
            }
            $segStart = $i + 1
        }
    }
    # Handle remaining bytes after last LF
    $remLen = $tableBytes.Length - $segStart
    if ($remLen -gt 0) {
        $seg     = [byte[]]::new($remLen)
        [System.Array]::Copy($tableBytes, $segStart, $seg, 0, $remLen)
        $trimList = [System.Collections.Generic.List[byte]]::new()
        foreach ($b in $seg) {
            if ($b -ne 0x0D -and $b -ne 0x00) { $trimList.Add($b) }
        }
        if ($trimList.Count -gt 0) {
            $hasBad = $false
            foreach ($b in $trimList) {
                if ($b -lt 0x20 -or $b -gt 0x7E) { $hasBad = $true; break }
            }
            if (-not $hasBad) {
                $str = [System.Text.Encoding]::ASCII.GetString($trimList.ToArray())
                if ($str.Length -gt 0) { $entries.Add($str) }
            }
        }
    }

    $extractedCount = $entries.Count
    $countMatches   = ($extractedCount -eq [int]$candidateCount)

    # Unique and duplicate counts
    $uniqueSet   = @($entries | Sort-Object -Unique)
    $uniqueCount = $uniqueSet.Count
    $dupCount    = $extractedCount - $uniqueCount

    # parse_status
    $parseStatus = if ($tableBytes.Length -eq 0) {
        'no_table_bytes'
    } elseif ($extractedCount -eq 0 -and $nonPrint -gt 10) {
        'unparseable'
    } elseif (-not $countMatches) {
        'count_mismatch'
    } else {
        'ok'
    }

    # First 25 entries
    $firstEntries = @($entries | Select-Object -First 25 | ForEach-Object { [string]$_ })

    # Accumulate distinct entries for aggregate (max 50)
    foreach ($e in $firstEntries) {
        if ($distinctAllEntries.Count -lt 50) { $distinctAllEntries.Add($e) | Out-Null }
    }

    $sampledRecords.Add([ordered]@{
        relative_path                      = ($rel -replace '\\', '/')
        size_bytes                         = $size
        bytes_read                         = $bytesRead
        sha256                             = $sha
        header_zero_hex                    = $headerZeroHex
        candidate_entry_count              = [int]$candidateCount
        extracted_entry_count              = $extractedCount
        count_matches_extracted_entries    = $countMatches
        first_entries                      = $firstEntries
        duplicate_entry_count              = $dupCount
        unique_entry_count                 = $uniqueCount
        non_printable_byte_count_after_header = $nonPrint
        parse_status                       = $parseStatus
    })

    Write-Output ("  {0,-50} {1,7}b  entries: cand={2} extr={3} match={4} status={5}" -f `
        ($rel -replace '\\', '/'), $size, [int]$candidateCount, $extractedCount, $countMatches, $parseStatus)
}

Write-Output ''

# ---------------------------------------------------------------------------
# Build aggregate
# ---------------------------------------------------------------------------

$allHeaderZeroConsistent = $true
foreach ($r in $sampledRecords) {
    if ($r.header_zero_hex -ne '00000000') { $allHeaderZeroConsistent = $false; break }
}

$countMatchCount    = 0
$countMismatchCount = 0
$minCandCount = [int]::MaxValue
$maxCandCount = 0
foreach ($r in $sampledRecords) {
    if ($r.count_matches_extracted_entries -eq $true)  { $countMatchCount++ }
    if ($r.count_matches_extracted_entries -eq $false) { $countMismatchCount++ }
    if ($r.candidate_entry_count -lt $minCandCount) { $minCandCount = $r.candidate_entry_count }
    if ($r.candidate_entry_count -gt $maxCandCount) { $maxCandCount = $r.candidate_entry_count }
}
if ($sampledRecords.Count -eq 0) { $minCandCount = 0 }

$distinctFirst50 = @($distinctAllEntries | Sort-Object | Select-Object -First 50 | ForEach-Object { [string]$_ })

$aggregate = [ordered]@{
    sampled_count                    = $sampledRecords.Count
    all_header_zero_bytes_consistent = $allHeaderZeroConsistent
    count_match_count                = $countMatchCount
    count_mismatch_count             = $countMismatchCount
    min_candidate_entry_count        = $minCandCount
    max_candidate_entry_count        = $maxCandCount
    distinct_first_entries           = $distinctFirst50
}

# ---------------------------------------------------------------------------
# Write JSON output
# ---------------------------------------------------------------------------

New-Item -ItemType Directory -Force -Path $outputFull | Out-Null

$evidence = [ordered]@{
    schema                       = 'pzmapforge.lotheader-string-table-evidence.v0.1'
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
    only_lotheader_files_read    = $true
    lotpack_files_read           = $false
    bin_files_read               = $false
    sampled_files                = $sampledRecords.ToArray()
    aggregate                    = $aggregate
}

$jsonPath = Join-Path $outputFull 'lotheader-string-table-evidence.json'
$evidence | ConvertTo-Json -Depth 5 | Set-Content -Path $jsonPath -Encoding UTF8

# ---------------------------------------------------------------------------
# Write Markdown output
# ---------------------------------------------------------------------------

$sampledRows = ($sampledRecords | ForEach-Object {
    "| ``$($_.relative_path.Split('/')[-1])`` | $($_.size_bytes) | $($_.candidate_entry_count) | $($_.extracted_entry_count) | $($_.count_matches_extracted_entries) | $($_.parse_status) |"
}) -join "`n"

$firstEntriesSection = ''
foreach ($r in $sampledRecords) {
    $name = $r.relative_path.Split('/')[-1]
    $firstEntriesSection += "`n### $name (cand=$($r.candidate_entry_count), extr=$($r.extracted_entry_count))`n`n"
    foreach ($e in $r.first_entries) {
        $firstEntriesSection += "- ``$e```n"
    }
}
if ($firstEntriesSection -eq '') { $firstEntriesSection = "`n(no entries extracted)`n" }

$distinctSection = if ($distinctFirst50.Count -gt 0) {
    ($distinctFirst50 | ForEach-Object { "- ``$_``" }) -join "`n"
} else { '- (none)' }

$md = @"
# Lotheader String Table Evidence

Schema:             pzmapforge.lotheader-string-table-evidence.v0.1
Claim boundary:     evidence_inventory_only_not_compiled_not_pz_load_tested
Generated:          $generatedAt
Input path:         $($inputFull -replace '\\', '/')
Max files:          $MaxFiles
Max bytes/file:     $MaxBytesPerFile

## Sampled files

| File | Bytes | Cand count | Extr count | Count match | Status |
|---|---:|---:|---:|---|---|
$sampledRows

## Aggregate

| Field | Value |
|---|---|
| Sampled count | $($aggregate.sampled_count) |
| All header-zero consistent | $($aggregate.all_header_zero_bytes_consistent) |
| Count match count | $($aggregate.count_match_count) |
| Count mismatch count | $($aggregate.count_mismatch_count) |
| Min candidate entry count | $($aggregate.min_candidate_entry_count) |
| Max candidate entry count | $($aggregate.max_candidate_entry_count) |

## First entries per file
$firstEntriesSection
## Distinct first entries (cross-file, max 50)

$distinctSection

## Non-claims

- Only .lotheader files read.
- No .lotpack files read.
- No .bin files read.
- No binary files copied.
- No compiled writer implemented.
- No playable export claimed.
- No repo media/maps writes.
- Output is under .local only.
"@

$mdPath = Join-Path $outputFull 'lotheader-string-table-evidence.md'
Set-Content -Path $mdPath -Value $md -Encoding UTF8

# ---------------------------------------------------------------------------
# Summary
# ---------------------------------------------------------------------------

Write-Output "Evidence JSON:               $jsonPath"
Write-Output "Evidence MD:                 $mdPath"
Write-Output "Sampled files:               $($sampledRecords.Count)"
Write-Output "Count matches:               $countMatchCount"
Write-Output "Count mismatches:            $countMismatchCount"
Write-Output "Distinct entries (first 50): $($distinctFirst50.Count)"
Write-Output 'only_lotheader_files_read:   true'
Write-Output 'lotpack_files_read:          false'
Write-Output 'bin_files_read:              false'
Write-Output 'copied_input_files:          false'
Write-Output 'pz_assets_copied:            false'
Write-Output 'compiled_writer_implemented: false'
Write-Output 'Status:                      OK'
