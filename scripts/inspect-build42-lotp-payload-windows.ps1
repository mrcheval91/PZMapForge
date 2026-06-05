#Requires -Version 5.1
<#
.SYNOPSIS
    Local-only Build 42 LOTP payload / LOTH entry inspector (MAP-6K).

    Reads LOTP lotpack offset tables and chunk payload windows, LOTH lotheader
    tileset entry lists, and chunkdata body patterns from a Build 42 reference
    mod under .local/. Produces a payload-window research report.

    Does NOT copy any files into the repo.
    Does NOT read PZ assets outside .local.
    Does NOT perform a load test.
    Does NOT implement a compiled writer.
    Output must be under .local only.

    Operator must manually place the reference map under .local/ first.

Usage:
    .\scripts\inspect-build42-lotp-payload-windows.ps1 `
        -Source  ".local\reference-build42-map\<mod_folder>" `
        -Output  ".local\build42-lotp-payload-<date>" `
        [-MaxCells 3] [-MaxChunksPerCell 16] [-WindowBytes 64]
#>

param(
    [Parameter(Mandatory=$true)]  [string]$Source,
    [Parameter(Mandatory=$true)]  [string]$Output,
    [int]$MaxCells           = 3,
    [int]$MaxChunksPerCell   = 16,
    [int]$WindowBytes        = 64
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

if ($MaxCells         -gt 20)  { $MaxCells = 20 }
if ($MaxCells         -lt 1)   { $MaxCells = 1 }
if ($MaxChunksPerCell -gt 64)  { $MaxChunksPerCell = 64 }
if ($MaxChunksPerCell -lt 1)   { $MaxChunksPerCell = 1 }
if ($WindowBytes      -gt 256) { $WindowBytes = 256 }
if ($WindowBytes      -lt 8)   { $WindowBytes = 8 }

Write-Output 'inspect-build42-lotp-payload-windows.ps1'
Write-Output "Source:           $Source"
Write-Output "Output:           $Output"
Write-Output "MaxCells:         $MaxCells"
Write-Output "MaxChunksPerCell: $MaxChunksPerCell"
Write-Output "WindowBytes:      $WindowBytes"
Write-Output ''

# ---------------------------------------------------------------------------
# Guards: .local only
# ---------------------------------------------------------------------------

$sep         = [System.IO.Path]::DirectorySeparatorChar
$localMarker = $sep + '.local' + $sep

function Test-IsUnderLocal { param([string]$P)
    return ($P.Contains($localMarker) -or $P.EndsWith($sep + '.local'))
}

$sourceFull = [System.IO.Path]::GetFullPath($Source)
$outputFull = [System.IO.Path]::GetFullPath($Output)

if (-not (Test-IsUnderLocal $sourceFull)) {
    Write-Error "inspect-build42-lotp-payload-windows: -Source must be under .local/: $sourceFull"
    exit 1
}
if (-not (Test-IsUnderLocal $outputFull)) {
    Write-Error "inspect-build42-lotp-payload-windows: -Output must be under .local/: $outputFull"
    exit 1
}

$forbidden = @('Zomboid'+$sep+'mods','Zomboid'+$sep+'Workshop','Zomboid'+$sep+'Server',
    'steamapps'+$sep+'common'+$sep+'ProjectZomboid','steamapps'+$sep+'common'+$sep+'Project Zomboid',
    'steamapps/common/ProjectZomboid','steamapps/common/Project Zomboid')
foreach ($pat in $forbidden) {
    if ($sourceFull -match [regex]::Escape($pat)) {
        Write-Error "inspect-build42-lotp-payload-windows: forbidden path: $sourceFull"
        exit 1
    }
}
if (-not (Test-Path -LiteralPath $sourceFull)) {
    Write-Error "inspect-build42-lotp-payload-windows: Source not found: $sourceFull"
    exit 1
}

# ---------------------------------------------------------------------------
# Helpers
# ---------------------------------------------------------------------------

function Read-Bytes {
    param([string]$Path, [long]$Offset, [int]$Count)
    $stream = [System.IO.File]::OpenRead($Path)
    try {
        if ($Offset -gt 0) { [void]$stream.Seek($Offset, [System.IO.SeekOrigin]::Begin) }
        $buf  = [byte[]]::new($Count)
        $read = $stream.Read($buf, 0, $Count)
        if ($read -lt $Count) { $buf = $buf[0..($read-1)] }
        return $buf
    }
    finally { $stream.Dispose() }
}

function Bytes-ToHex { param([byte[]]$B)
    return ($B | ForEach-Object { $_.ToString('x2') }) -join ''
}

function Get-Sha256Hex { param([byte[]]$B)
    $sha = [System.Security.Cryptography.SHA256]::Create()
    try { return (($sha.ComputeHash($B) | ForEach-Object { $_.ToString('x2') }) -join '') }
    finally { $sha.Dispose() }
}

# ---------------------------------------------------------------------------
# Scan source
# ---------------------------------------------------------------------------

Write-Output '--- Scanning source tree ---'
$allFiles = @(Get-ChildItem -LiteralPath $sourceFull -Recurse -File -ErrorAction SilentlyContinue | Sort-Object FullName)
$lotpackFiles  = @($allFiles | Where-Object { $_.Name -match '^world_\d+_\d+\.lotpack$'  } | Select-Object -First $MaxCells)
$lotheaderFiles= @($allFiles | Where-Object { $_.Extension.ToLower() -eq '.lotheader'    } | Select-Object -First $MaxCells)
$chunkdataFiles= @($allFiles | Where-Object { $_.Name -match '^chunkdata_\d+_\d+\.bin$' } | Select-Object -First $MaxCells)
Write-Output "  LOTP lotpacks: $($lotpackFiles.Count)"
Write-Output "  LOTH lotheaders: $($lotheaderFiles.Count)"
Write-Output "  chunkdata .bin: $($chunkdataFiles.Count)"
Write-Output ''

# ---------------------------------------------------------------------------
# Analyse LOTP lotpacks
# ---------------------------------------------------------------------------

Write-Output '--- Analysing LOTP lotpacks ---'
$lotpRecords = [System.Collections.Generic.List[object]]::new()

foreach ($f in $lotpackFiles) {
    $rel  = $f.FullName.Substring($sourceFull.Length).TrimStart($sep)
    $size = $f.Length
    Write-Output "  $rel  ($size bytes)"

    # Read header: 12 bytes
    $hdr = Read-Bytes $f.FullName 0 12
    if ($hdr.Length -lt 12) { Write-Output "    SKIP: too small for header"; continue }

    $magic = Bytes-ToHex $hdr[0..3]
    $isLotp = ($hdr[0] -eq 0x4C -and $hdr[1] -eq 0x4F -and $hdr[2] -eq 0x54 -and $hdr[3] -eq 0x50)
    if (-not $isLotp) { Write-Output "    SKIP: not LOTP magic ($magic)"; continue }

    $version    = [System.BitConverter]::ToUInt32($hdr, 4)
    $chunkCount = [System.BitConverter]::ToUInt32($hdr, 8)
    Write-Output "    magic=LOTP version=$version chunk_count=$chunkCount"

    # Read full offset table: 1024 × 8 bytes
    $tableBytes  = [int]$chunkCount * 8
    $tableBuffer = Read-Bytes $f.FullName 12 $tableBytes
    if ($tableBuffer.Length -lt $tableBytes) {
        Write-Output "    WARN: offset table truncated ($($tableBuffer.Length) < $tableBytes bytes)"
    }

    # Parse all offsets as U64 LE (low U32 only since hi=0 in observed data)
    $offsets = [System.Collections.Generic.List[long]]::new()
    $parseable = [Math]::Floor($tableBuffer.Length / 8)
    for ($i = 0; $i -lt $parseable; $i++) {
        $pos = $i * 8
        $lo  = [long][System.BitConverter]::ToUInt32($tableBuffer, $pos)
        $hi  = [long][System.BitConverter]::ToUInt32($tableBuffer, $pos + 4)
        $offsets.Add($lo + ($hi -shl 32))
    }

    # Compute payload sizes between consecutive offsets
    $payloadSizes = [System.Collections.Generic.List[long]]::new()
    for ($i = 1; $i -lt $offsets.Count; $i++) {
        $payloadSizes.Add($offsets[$i] - $offsets[$i-1])
    }
    # Last chunk payload size = file_size - last_offset
    $tailSize = if ($offsets.Count -gt 0) { $size - $offsets[$offsets.Count - 1] } else { 0 }

    $firstOffset  = if ($offsets.Count -gt 0) { $offsets[0] } else { -1 }
    $lastOffset   = if ($offsets.Count -gt 0) { $offsets[$offsets.Count - 1] } else { -1 }

    # Is monotonically increasing?
    $monotonic = $true
    for ($i = 1; $i -lt $offsets.Count; $i++) {
        if ($offsets[$i] -le $offsets[$i-1]) { $monotonic = $false; break }
    }

    # Most common payload size
    $sizeGroups = @($payloadSizes | Group-Object | Sort-Object Count -Descending)
    $mostCommonSize = if ($sizeGroups.Count -gt 0) { [long]$sizeGroups[0].Name } else { -1 }
    $uniqueSizes = @($sizeGroups | ForEach-Object { [long]$_.Name })

    Write-Output "    first_offset=$firstOffset  last_offset=$lastOffset  monotonic=$monotonic"
    Write-Output "    most_common_payload_size=$mostCommonSize  unique_sizes=$($uniqueSizes.Count)  tail_bytes=$tailSize"

    # Sample chunk windows
    $sampleIndices = @(0)
    if ($MaxChunksPerCell -gt 1 -and $offsets.Count -gt 1) {
        $step = [Math]::Max(1, [Math]::Floor(($offsets.Count - 1) / ($MaxChunksPerCell - 1)))
        for ($i = $step; $i -lt $offsets.Count -and $sampleIndices.Count -lt $MaxChunksPerCell; $i += $step) {
            $sampleIndices += $i
        }
    }

    $windowRecords = [System.Collections.Generic.List[object]]::new()
    $windowShas    = [System.Collections.Generic.List[string]]::new()
    foreach ($idx in $sampleIndices) {
        $off = $offsets[$idx]
        try {
            $window = Read-Bytes $f.FullName $off $WindowBytes
            $sha    = Get-Sha256Hex $window
            $hex    = Bytes-ToHex $window
            $windowShas.Add($sha)
            $windowRecords.Add([ordered]@{
                chunk_index    = $idx
                offset         = $off
                window_bytes   = $window.Length
                sha256         = $sha
                first_hex      = $hex
            })
            Write-Output "    chunk[$idx] off=$off sha256=$($sha.Substring(0,16))..."
        } catch {
            Write-Output "    chunk[$idx] FAIL: $_"
        }
    }

    $distinctShas = @($windowShas | Select-Object -Unique)
    $windowsIdentical = ($distinctShas.Count -le 1 -and $windowShas.Count -gt 0)

    $lotpRecords.Add([ordered]@{
        file                    = $rel
        size_bytes              = $size
        magic                   = 'LOTP'
        version_u32le           = [int]$version
        chunk_count             = [int]$chunkCount
        first_offset            = $firstOffset
        last_offset             = $lastOffset
        monotonic_offsets       = $monotonic
        most_common_payload_size = $mostCommonSize
        unique_payload_size_count = $uniqueSizes.Count
        unique_payload_sizes    = @($uniqueSizes | Select-Object -First 10)
        tail_bytes              = $tailSize
        first_5_offsets         = @($offsets | Select-Object -First 5)
        sampled_windows         = @($windowRecords)
        windows_identical       = $windowsIdentical
        distinct_window_sha_count = $distinctShas.Count
    })
}

# ---------------------------------------------------------------------------
# Analyse LOTH lotheaders
# ---------------------------------------------------------------------------

Write-Output ''
Write-Output '--- Analysing LOTH lotheaders ---'
$lothRecords = [System.Collections.Generic.List[object]]::new()

foreach ($f in $lotheaderFiles) {
    $rel  = $f.FullName.Substring($sourceFull.Length).TrimStart($sep)
    $size = $f.Length
    Write-Output "  $rel  ($size bytes)"

    $bytes = [System.IO.File]::ReadAllBytes($f.FullName)
    if ($bytes.Length -lt 12) { Write-Output "    SKIP: too small"; continue }

    $isLoth = ($bytes[0] -eq 0x4C -and $bytes[1] -eq 0x4F -and $bytes[2] -eq 0x54 -and $bytes[3] -eq 0x48)
    if (-not $isLoth) { Write-Output "    SKIP: not LOTH"; continue }

    $version       = [System.BitConverter]::ToUInt32($bytes, 4)
    $declaredCount = [System.BitConverter]::ToUInt32($bytes, 8)

    # Parse string table at bytes 12+
    $strBytes = $bytes[12..($bytes.Length-1)]
    $text     = [System.Text.Encoding]::ASCII.GetString($strBytes)
    $entries  = @($text.Split("`n") | Where-Object { $_.Length -gt 0 })

    Write-Output "    version=$version  declared_count=$declaredCount  parsed_count=$($entries.Count)"

    $lothRecords.Add([ordered]@{
        file                 = $rel
        size_bytes           = $size
        magic                = 'LOTH'
        version_u32le        = [int]$version
        declared_entry_count = [int]$declaredCount
        parsed_entry_count   = $entries.Count
        count_matches        = ($entries.Count -eq [int]$declaredCount)
        first_20_entries     = @($entries | Select-Object -First 20)
        all_entries_count    = $entries.Count
    })
}

# Aggregate LOTH analysis
$allEntries = [System.Collections.Generic.List[string]]::new()
foreach ($r in $lothRecords) {
    foreach ($e in $r.first_20_entries) { if (-not $allEntries.Contains([string]$e)) { $allEntries.Add([string]$e) } }
}
$smallestRecordCount = if ($lothRecords.Count -gt 0) {
    [int]($lothRecords | ForEach-Object { $_.declared_entry_count } | Measure-Object -Minimum).Minimum
} else { -1 }

# ---------------------------------------------------------------------------
# Analyse chunkdata
# ---------------------------------------------------------------------------

Write-Output ''
Write-Output '--- Analysing chunkdata ---'
$chunkdataRecords = [System.Collections.Generic.List[object]]::new()

foreach ($f in $chunkdataFiles) {
    $rel      = $f.FullName.Substring($sourceFull.Length).TrimStart($sep)
    $size     = [int]$f.Length
    $bodyBytes = $size - 2
    Write-Output "  $rel  ($size bytes, body=$bodyBytes)"

    $prefix = Read-Bytes $f.FullName 0 32
    $first32 = Bytes-ToHex $prefix

    # Check all-zero body (read up to 1024 bytes of body)
    $bodyCheck = Read-Bytes $f.FullName 2 ([Math]::Min(1024, $bodyBytes))
    $allZero   = $true
    foreach ($b in $bodyCheck) { if ($b -ne 0) { $allZero = $false; break } }

    Write-Output "    all_zero_body=$allZero"

    $chunkdataRecords.Add([ordered]@{
        file          = $rel
        size_bytes    = $size
        body_bytes    = $bodyBytes
        first_32_hex  = $first32
        all_zero_body = $allZero
    })
}

# ---------------------------------------------------------------------------
# Build status labels
# ---------------------------------------------------------------------------

$statuses = [System.Collections.Generic.List[string]]::new()
if ($lotpRecords.Count -gt 0)     { [void]$statuses.Add('BUILD42_LOTP_PAYLOAD_WINDOWS_INSPECTED') }
if ($lothRecords.Count -gt 0)     { [void]$statuses.Add('BUILD42_LOTH_ENTRIES_EXTRACTED') }
if ($chunkdataRecords.Count -gt 0){ [void]$statuses.Add('BUILD42_CHUNKDATA_BODY_INSPECTED') }
[void]$statuses.Add('WRITER_RESEARCH_ONLY')
[void]$statuses.Add('WRITER_NOT_IMPLEMENTED')
[void]$statuses.Add('LOAD_TEST_NOT_PERFORMED')
[void]$statuses.Add('PLAYABLE_EXPORT_CLAIM_ALLOWED=false')

Write-Output ''
Write-Output "--- Statuses: $($statuses -join ', ') ---"

# ---------------------------------------------------------------------------
# Write output
# ---------------------------------------------------------------------------

New-Item -ItemType Directory -Force -Path $outputFull | Out-Null
$generatedAt = (Get-Date).ToUniversalTime().ToString('yyyy-MM-ddTHH:mm:ssZ')

$report = [ordered]@{
    schema           = 'pzmapforge.build42-lotp-payload-window-report.v0.1'
    claim_boundary   = 'writer_research_only_not_implemented_not_load_tested'
    generated_at_utc = $generatedAt
    source_path      = $sourceFull.Replace('\','/')
    statuses         = @($statuses)
    WRITER_NOT_IMPLEMENTED       = $true
    LOAD_TEST_NOT_PERFORMED      = $true
    PLAYABLE_EXPORT_CLAIM_ALLOWED = 'false'
    lotp_analysis = [ordered]@{
        cells_sampled          = $lotpRecords.Count
        smallest_observed_entry = $smallestRecordCount
        records                = @($lotpRecords)
    }
    loth_analysis = [ordered]@{
        cells_sampled           = $lothRecords.Count
        smallest_entry_count    = $smallestRecordCount
        unique_entries_seen     = $allEntries.Count
        records                 = @($lothRecords)
    }
    chunkdata_analysis = [ordered]@{
        cells_sampled = $chunkdataRecords.Count
        records       = @($chunkdataRecords)
    }
    safety = [ordered]@{
        reference_files_copied    = $false
        pz_assets_copied          = $false
        media_maps_touched_in_repo = $false
        playable_export_claimed   = $false
        compiled_writer_implemented = $false
        load_test_performed       = $false
        only_prefix_bytes_read    = $true
    }
}

$jsonPath = Join-Path $outputFull 'build42-lotp-payload-window-report.json'
$report | ConvertTo-Json -Depth 8 | Set-Content -Path $jsonPath -Encoding UTF8
Write-Output "Report JSON: $jsonPath"

$md = @"
# Build 42 LOTP Payload Window Report

Schema: pzmapforge.build42-lotp-payload-window-report.v0.1
Generated: $generatedAt
Source: $($sourceFull.Replace('\','/'))

## Statuses

$($statuses | ForEach-Object { "- $_" } | Out-String)

## LOTP lotpack payload analysis

Cells sampled: $($lotpRecords.Count)

$(foreach ($r in $lotpRecords) {
@"
### $($r['file'])

| Field | Value |
|---|---|
| size_bytes | $($r['size_bytes']) |
| chunk_count | $($r['chunk_count']) |
| first_offset | $($r['first_offset']) |
| last_offset | $($r['last_offset']) |
| monotonic_offsets | $($r['monotonic_offsets']) |
| most_common_payload_size | $($r['most_common_payload_size']) |
| unique_payload_size_count | $($r['unique_payload_size_count']) |
| tail_bytes | $($r['tail_bytes']) |
| windows_identical | $($r['windows_identical']) |
| distinct_window_sha_count | $($r['distinct_window_sha_count']) |

"@
})

## LOTH lotheader entry analysis

Cells sampled: $($lothRecords.Count)
Smallest entry count: $smallestRecordCount

$(foreach ($r in $lothRecords) {
@"
### $($r['file'])

declared=$($r['declared_entry_count'])  parsed=$($r['parsed_entry_count'])  match=$($r['count_matches'])

First entries: $($r['first_20_entries'] | Select-Object -First 5 | Out-String)

"@
})

## Chunkdata analysis

Cells sampled: $($chunkdataRecords.Count)

$(foreach ($r in $chunkdataRecords) { "- $($r['file']): $($r['size_bytes']) bytes, body=$($r['body_bytes']), all_zero=$($r['all_zero_body'])" })

## Safety

reference_files_copied: false
pz_assets_copied: false
playable_export_claimed: false
WRITER_NOT_IMPLEMENTED: true
LOAD_TEST_NOT_PERFORMED: true
PLAYABLE_EXPORT_CLAIM_ALLOWED=false
"@

$mdPath = Join-Path $outputFull 'build42-lotp-payload-window-report.md'
Set-Content -Path $mdPath -Value $md -Encoding UTF8
Write-Output "Report MD:   $mdPath"
Write-Output ''
Write-Output "WRITER_NOT_IMPLEMENTED: true"
Write-Output "LOAD_TEST_NOT_PERFORMED: true"
Write-Output "PLAYABLE_EXPORT_CLAIM_ALLOWED: false"
Write-Output 'Done.'
