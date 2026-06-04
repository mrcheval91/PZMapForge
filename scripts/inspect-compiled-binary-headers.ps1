#Requires -Version 5.1
<#
.SYNOPSIS
    Local-only compiled binary header evidence probe (MAP-4D).

    Reads bounded byte prefixes from compiled map files (.lotheader, .lotpack,
    .bin) and writes a local-only hex evidence JSON and Markdown report.

    Reads ONLY the first MaxBytes (default 64, max 256) of each sampled file.
    Does NOT read full binary file contents.
    Does NOT copy input files.
    Does NOT write binary samples — hex strings only.
    Does NOT touch repo media/maps.
    Does NOT claim playable export.
    Does NOT implement any compiled writer.
    Output must be under .local only.

Usage:
    .\scripts\inspect-compiled-binary-headers.ps1 `
        -Path                 <local path to mod/map root> `
        -Output               <output directory (must be under .local)> `
        -MaxBytes             <int, default 64, max 256> `
        -MaxFilesPerExtension <int, default 5, max 20>

Example:
    .\scripts\inspect-compiled-binary-headers.ps1 `
        -Path   "C:\path\to\mod-root" `
        -Output ".local\evidence\binary-header-01"
#>

param(
    [Parameter(Mandatory=$true)]
    [string]$Path,

    [Parameter(Mandatory=$true)]
    [string]$Output,

    [int]$MaxBytes             = 64,
    [int]$MaxFilesPerExtension = 5
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

# ---------------------------------------------------------------------------
# Guard: clamp MaxBytes to [1, 256]
# ---------------------------------------------------------------------------

if ($MaxBytes -gt 256) { $MaxBytes = 256 }
if ($MaxBytes -lt 1)   { $MaxBytes = 1 }
if ($MaxFilesPerExtension -gt 20) { $MaxFilesPerExtension = 20 }
if ($MaxFilesPerExtension -lt 1)  { $MaxFilesPerExtension = 1 }

Write-Output 'inspect-compiled-binary-headers.ps1'
Write-Output "Path:                  $Path"
Write-Output "Output:                $Output"
Write-Output "MaxBytes:              $MaxBytes"
Write-Output "MaxFilesPerExtension:  $MaxFilesPerExtension"
Write-Output ''

# ---------------------------------------------------------------------------
# Guard: output must be under .local
# ---------------------------------------------------------------------------

$outputFull  = [System.IO.Path]::GetFullPath($Output)
$sep         = [System.IO.Path]::DirectorySeparatorChar
$localMarker = $sep + '.local' + $sep
$endsLocal   = $outputFull.EndsWith($sep + '.local')

if (-not ($outputFull.Contains($localMarker) -or $endsLocal)) {
    Write-Error "inspect-compiled-binary-headers: refusing to write outside a .local/ directory: $outputFull"
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
# SHA-256 helper (full file hash for provenance; does not store content)
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
# Bounded prefix reader — reads at most $MaxBytes bytes, returns byte array
# ---------------------------------------------------------------------------

function Read-BoundedPrefix {
    param([string]$FilePath, [int]$MaxRead)
    $stream = [System.IO.File]::OpenRead($FilePath)
    try {
        $buf  = [byte[]]::new($MaxRead)
        $read = $stream.Read($buf, 0, $MaxRead)
        if ($read -le 0) { return [byte[]]@() }
        $prefix = [byte[]]::new($read)
        [System.Array]::Copy($buf, $prefix, $read)
        return $prefix
    }
    finally { $stream.Dispose() }
}

# ---------------------------------------------------------------------------
# Scan for target-extension files
# ---------------------------------------------------------------------------

Write-Output '--- Scanning for binary files ---'

$targetExtensions = @('.lotheader', '.lotpack', '.bin')
$allBinaryFiles   = @(Get-ChildItem -LiteralPath $inputFull -Recurse -File -ErrorAction SilentlyContinue |
    Where-Object { $targetExtensions -contains $_.Extension.ToLowerInvariant() } |
    Sort-Object FullName)

$extCountsFound = [ordered]@{}
foreach ($ext in $targetExtensions) {
    $c = @($allBinaryFiles | Where-Object { $_.Extension.ToLowerInvariant() -eq $ext }).Count
    $extCountsFound[$ext] = $c
    Write-Output ("  {0,-14} {1,4} files found" -f $ext, $c)
}
Write-Output ''

# ---------------------------------------------------------------------------
# Sample up to MaxFilesPerExtension per extension
# ---------------------------------------------------------------------------

$sampledFileInfos = [System.Collections.Generic.List[object]]::new()
foreach ($ext in $targetExtensions) {
    $group = @($allBinaryFiles | Where-Object { $_.Extension.ToLowerInvariant() -eq $ext } | Select-Object -First $MaxFilesPerExtension)
    foreach ($f in $group) { $sampledFileInfos.Add($f) }
}

Write-Output "--- Reading bounded prefixes ($MaxBytes bytes max per file) ---"

$sampledRecords  = [System.Collections.Generic.List[object]]::new()
$groupAccumulator = [ordered]@{}   # prefix-16-key -> list of relative paths
$extCountsSampled = [ordered]@{}

$generatedAt = (Get-Date).ToUniversalTime().ToString('yyyy-MM-ddTHH:mm:ssZ')

foreach ($f in $sampledFileInfos) {
    $rel = $f.FullName.Substring($inputFull.Length).TrimStart($sep)
    $ext = $f.Extension.ToLowerInvariant()

    # Accumulate sampled count per extension
    if (-not $extCountsSampled.Contains($ext)) { $extCountsSampled[$ext] = 0 }
    $extCountsSampled[$ext]++

    # SHA-256 of full file (provenance only; does not store content)
    $sha  = Get-FileSha256 $f.FullName
    $size = $f.Length

    # Read bounded prefix
    $prefix = Read-BoundedPrefix $f.FullName $MaxBytes
    $bytesRead = $prefix.Count

    # Hex of full prefix
    $prefixHex = if ($bytesRead -gt 0) {
        ($prefix | ForEach-Object { $_.ToString('x2') }) -join ''
    } else { '' }

    # Safe ASCII preview: printable [32,126] -> char, else '.'
    $asciiPreview = if ($bytesRead -gt 0) {
        ($prefix | ForEach-Object { if ($_ -ge 32 -and $_ -le 126) { [char]$_ } else { '.' } }) -join ''
    } else { '' }

    # First 4 and 8 bytes
    $first4 = if ($bytesRead -ge 4) {
        ($prefix[0..3] | ForEach-Object { $_.ToString('x2') }) -join ''
    } elseif ($bytesRead -gt 0) {
        ($prefix | ForEach-Object { $_.ToString('x2') }) -join ''
    } else { '' }

    $first8 = if ($bytesRead -ge 8) {
        ($prefix[0..7] | ForEach-Object { $_.ToString('x2') }) -join ''
    } elseif ($bytesRead -gt 0) {
        ($prefix | ForEach-Object { $_.ToString('x2') }) -join ''
    } else { '' }

    # All-zero check
    $allZero = ($bytesRead -eq 0) -or ($null -eq ($prefix | Where-Object { $_ -ne 0 } | Select-Object -First 1))

    # Group key: first 16 bytes hex (or less if file is smaller)
    $groupEnd = [Math]::Min(15, $bytesRead - 1)
    $groupKey = if ($bytesRead -gt 0) {
        ($prefix[0..$groupEnd] | ForEach-Object { $_.ToString('x2') }) -join ''
    } else { 'empty' }

    if (-not $groupAccumulator.Contains($groupKey)) {
        $groupAccumulator[$groupKey] = [System.Collections.Generic.List[string]]::new()
    }
    $groupAccumulator[$groupKey].Add(($rel -replace '\\', '/'))

    $sampledRecords.Add([ordered]@{
        relative_path        = ($rel -replace '\\', '/')
        extension            = $ext
        size_bytes           = $size
        sha256               = $sha
        prefix_bytes_read    = $bytesRead
        prefix_hex           = $prefixHex
        prefix_ascii_preview = $asciiPreview
        first_4_bytes_hex    = $first4
        first_8_bytes_hex    = $first8
        all_zero_prefix      = $allZero
        prefix_group_key     = $groupKey
    })

    Write-Output ("  {0,-52} {1,8} bytes  first4={2}" -f ($rel -replace '\\', '/'), $size, $first4)
}

Write-Output ''

# Build repeated prefix groups (only groups with >1 member)
$repeatedPrefixGroups = [ordered]@{}
foreach ($key in $groupAccumulator.Keys) {
    $members = $groupAccumulator[$key].ToArray()
    $repeatedPrefixGroups[$key] = $members
}

# ---------------------------------------------------------------------------
# Write JSON output
# ---------------------------------------------------------------------------

New-Item -ItemType Directory -Force -Path $outputFull | Out-Null

$evidence = [ordered]@{
    schema                       = 'pzmapforge.compiled-binary-header-evidence.v0.1'
    claim_boundary               = 'evidence_inventory_only_not_compiled_not_pz_load_tested'
    generated_at_utc             = $generatedAt
    input_path                   = ($inputFull -replace '\\', '/')
    max_bytes_requested          = $MaxBytes
    max_files_per_extension      = $MaxFilesPerExtension
    copied_input_files           = $false
    pz_assets_copied             = $false
    media_maps_touched           = $false
    playable_export_claimed      = $false
    compiled_writer_implemented  = $false
    binary_prefixes_read         = $true
    full_binary_files_read       = $false
    extension_counts_found       = $extCountsFound
    extension_counts_sampled     = $extCountsSampled
    sampled_files                = $sampledRecords.ToArray()
    repeated_prefix_groups       = $repeatedPrefixGroups
    notes                        = @(
        'Prefix evidence only. Full binary files were not read.',
        'No binary files copied into the repo.',
        'No compiled writer implemented.',
        'No playable export claimed.',
        'No repo media/maps writes.',
        'Output is under .local only.'
    )
}

$jsonPath = Join-Path $outputFull 'compiled-binary-header-evidence.json'
$evidence | ConvertTo-Json -Depth 4 | Set-Content -Path $jsonPath -Encoding UTF8

# ---------------------------------------------------------------------------
# Write Markdown output
# ---------------------------------------------------------------------------

$sampledRows = ($sampledRecords | ForEach-Object {
    $shortSha = $_.sha256.Substring(0, 12) + '...'
    "| ``$($_.relative_path)`` | ``$($_.extension)`` | $($_.size_bytes) | $($_.prefix_bytes_read) | ``$($_.first_4_bytes_hex)`` | ``$($_.first_8_bytes_hex)`` | $($_.all_zero_prefix) |"
}) -join "`n"

$groupSection = ''
foreach ($key in $repeatedPrefixGroups.Keys) {
    $members  = $repeatedPrefixGroups[$key]
    $count    = $members.Count
    $keyShort = if ($key.Length -gt 32) { $key.Substring(0, 32) + '...' } else { $key }
    $groupSection += "`n### Group ``$keyShort`` ($count file(s))`n`n"
    foreach ($m in $members) { $groupSection += "- ``$m```n" }
}
if ($groupSection -eq '') { $groupSection = "`n(no groups)`n" }

$md = @"
# Compiled Binary Header Evidence

Schema:               pzmapforge.compiled-binary-header-evidence.v0.1
Claim boundary:       evidence_inventory_only_not_compiled_not_pz_load_tested
Generated:            $generatedAt
Input path:           $($inputFull -replace '\\', '/')
Max bytes requested:  $MaxBytes
Files per extension:  $MaxFilesPerExtension

## Sampled files

| Relative path | Extension | Bytes | Prefix read | First 4 (hex) | First 8 (hex) | All zero |
|---|---|---:|---:|---|---|---|
$sampledRows

## Prefix groups (by first 16 bytes)
$groupSection
## Non-claims

- Prefix evidence only. Full binary files were not read.
- No binary files copied into the repo.
- No compiled writer implemented.
- No playable export claimed.
- No repo media/maps writes.
- Output is under .local only.
"@

$mdPath = Join-Path $outputFull 'compiled-binary-header-evidence.md'
Set-Content -Path $mdPath -Value $md -Encoding UTF8

# ---------------------------------------------------------------------------
# Summary
# ---------------------------------------------------------------------------

Write-Output "Evidence JSON:               $jsonPath"
Write-Output "Evidence MD:                 $mdPath"
Write-Output "Sampled files:               $($sampledRecords.Count)"
Write-Output "Prefix groups:               $($repeatedPrefixGroups.Count)"
Write-Output 'binary_prefixes_read:        true'
Write-Output 'full_binary_files_read:      false'
Write-Output 'copied_input_files:          false'
Write-Output 'pz_assets_copied:            false'
Write-Output 'media_maps_touched:          false'
Write-Output 'playable_export_claimed:     false'
Write-Output 'compiled_writer_implemented: false'
Write-Output 'Status:                      OK'
