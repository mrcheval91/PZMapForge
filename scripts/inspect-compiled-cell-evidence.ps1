#Requires -Version 5.1
<#
.SYNOPSIS
    Local-only compiled cell evidence inspector (MAP-4A).

    Enumerates files at a given local path, computes SHA-256 hashes,
    and writes a local-only evidence JSON and Markdown report.

    Does NOT copy, read, or parse file contents beyond SHA-256 hashing.
    Does NOT touch repo media/maps.
    Does NOT claim playable export.
    Does NOT implement any compiled writer.
    Output must be under .local only.

Usage:
    .\scripts\inspect-compiled-cell-evidence.ps1 `
        -Path   <local path to compiled mod or WorldEd export directory> `
        -Output <output directory (must be under .local)>

Example:
    .\scripts\inspect-compiled-cell-evidence.ps1 `
        -Path   "C:\path\to\worlded-export" `
        -Output ".local\evidence\worlded-export-01"
#>

param(
    [Parameter(Mandatory=$true)]
    [string]$Path,

    [Parameter(Mandatory=$true)]
    [string]$Output
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

Write-Output 'inspect-compiled-cell-evidence.ps1'
Write-Output "Path:   $Path"
Write-Output "Output: $Output"
Write-Output ''

# ---------------------------------------------------------------------------
# Guard: output must be under .local
# ---------------------------------------------------------------------------

$outputFull   = [System.IO.Path]::GetFullPath($Output)
$sep          = [System.IO.Path]::DirectorySeparatorChar
$localMarker  = $sep + '.local' + $sep
$endsLocal    = $outputFull.EndsWith($sep + '.local')

if (-not ($outputFull.Contains($localMarker) -or $endsLocal)) {
    Write-Error "inspect-compiled-cell-evidence: refusing to write outside a .local/ directory: $outputFull"
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

# ---------------------------------------------------------------------------
# SHA-256 helper (reads bytes only for hashing; does not parse content)
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
# Enumerate files
# ---------------------------------------------------------------------------

Write-Output '--- Enumerating files ---'

$inputFull = [System.IO.Path]::GetFullPath($Path)
$allFiles  = @(Get-ChildItem -LiteralPath $inputFull -Recurse -File -ErrorAction SilentlyContinue)

Write-Output "  Files found: $($allFiles.Count)"
Write-Output ''

$generatedAt = (Get-Date).ToUniversalTime().ToString('yyyy-MM-ddTHH:mm:ssZ')
$fileRecords = [System.Collections.Generic.List[object]]::new()

foreach ($f in $allFiles) {
    $rel  = $f.FullName.Substring($inputFull.Length).TrimStart($sep)
    $sha  = Get-FileSha256 $f.FullName
    $fileRecords.Add([ordered]@{
        relative_path = $rel -replace '\\', '/'
        extension     = $f.Extension.ToLowerInvariant()
        size_bytes    = $f.Length
        sha256        = $sha
    })
    Write-Output ("  {0,-60} {1,10} bytes" -f ($rel -replace '\\', '/'), $f.Length)
}

# ---------------------------------------------------------------------------
# Extension summary
# ---------------------------------------------------------------------------

$extCounts = [ordered]@{}
foreach ($r in $fileRecords) {
    $ext = if ($r.extension) { $r.extension } else { '(none)' }
    if (-not $extCounts.Contains($ext)) { $extCounts[$ext] = 0 }
    $extCounts[$ext]++
}

# ---------------------------------------------------------------------------
# Write JSON output
# ---------------------------------------------------------------------------

New-Item -ItemType Directory -Force -Path $outputFull | Out-Null

$evidence = [ordered]@{
    schema                      = 'pzmapforge.compiled-cell-evidence.v0.1'
    claim_boundary              = 'evidence_inventory_only_not_compiled_not_pz_load_tested'
    generated_at_utc            = $generatedAt
    input_path                  = $inputFull -replace '\\', '/'
    file_count                  = $fileRecords.Count
    extension_counts            = $extCounts
    files                       = $fileRecords.ToArray()
    copied_input_files          = $false
    pz_assets_copied            = $false
    media_maps_touched          = $false
    playable_export_claimed     = $false
    compiled_writer_implemented = $false
    notes                       = @(
        'Evidence inventory only. No compiled writer implemented.',
        'File contents are not parsed beyond SHA-256 hashing.',
        'No input files copied into the repo.',
        'No PZ assets copied or committed.',
        'Output is under .local only.',
        'Fill COMPILED_CELL_EVIDENCE_TEMPLATE.md with observations.'
    )
}

$jsonPath = Join-Path $outputFull 'compiled-cell-evidence.json'
$evidence | ConvertTo-Json -Depth 5 | Set-Content -Path $jsonPath -Encoding UTF8

# ---------------------------------------------------------------------------
# Write Markdown output
# ---------------------------------------------------------------------------

$extRows  = ($extCounts.GetEnumerator() | ForEach-Object {
    "| ``$($_.Key)`` | $($_.Value) |"
}) -join "`n"

$fileRows = ($fileRecords | ForEach-Object {
    $shortSha = if ($_.sha256.Length -ge 12) { $_.sha256.Substring(0,12) + '...' } else { $_.sha256 }
    "| ``$($_.relative_path)`` | ``$($_.extension)`` | $($_.size_bytes) | ``$shortSha`` |"
}) -join "`n"

$md = @"
# Compiled Cell Evidence Inventory

Schema:         pzmapforge.compiled-cell-evidence.v0.1
Claim boundary: evidence_inventory_only_not_compiled_not_pz_load_tested
Generated:      $generatedAt
Input path:     $($inputFull -replace '\\', '/')
File count:     $($fileRecords.Count)

## Extension summary

| Extension | Count |
|---|---:|
$extRows

## File inventory

| Relative path | Extension | Bytes | SHA-256 (first 12) |
|---|---|---:|---|
$fileRows

## Safety

| Property | Value |
|---|---|
| copied_input_files | false |
| pz_assets_copied | false |
| media_maps_touched | false |
| playable_export_claimed | false |
| compiled_writer_implemented | false |

## Notes

- Evidence inventory only. No compiled writer implemented.
- File contents are not parsed beyond SHA-256 hashing.
- No input files copied into the repo.
- No PZ assets copied or committed.
- Output is under .local only.
- Fill docs/examples/compiled-cell-evidence/COMPILED_CELL_EVIDENCE_TEMPLATE.md with observations.
"@

$mdPath = Join-Path $outputFull 'compiled-cell-evidence.md'
Set-Content -Path $mdPath -Value $md -Encoding UTF8

# ---------------------------------------------------------------------------
# Summary
# ---------------------------------------------------------------------------

Write-Output ''
Write-Output "Evidence JSON:               $jsonPath"
Write-Output "Evidence MD:                 $mdPath"
Write-Output "Files enumerated:            $($fileRecords.Count)"
Write-Output 'copied_input_files:          false'
Write-Output 'pz_assets_copied:            false'
Write-Output 'media_maps_touched:          false'
Write-Output 'playable_export_claimed:     false'
Write-Output 'compiled_writer_implemented: false'
Write-Output 'Status:                      OK'
