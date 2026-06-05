#Requires -Version 5.1
<#
.SYNOPSIS
    Local-only Build 42 reference geometry inspector (MAP-6F/MAP-6H).

    Inspects a known-good Build 42 map mod or export that the operator has
    manually placed under .local/. Reads only bounded byte prefixes and file
    sizes. Detects LOTP lotpack magic (Build 42), LOTH lotheader magic (Build 42),
    and chunkdata body sizes. Reports a geometry status array.

    Reads .lotheader, world_*.lotpack, chunkdata_*.bin, map.info, mod.info,
    spawnpoints.lua from the provided source path.
    Does NOT copy any files into the repo.
    Does NOT read PZ assets.
    Does NOT perform a load test.
    Does NOT claim playable export.
    Does NOT implement any compiled writer.
    Output must be under .local only.

    The operator must manually place the reference map under .local/ before
    running this script. This script does not automate that copy.

Usage:
    .\scripts\inspect-build42-reference-geometry.ps1 `
        -Source  ".local\reference-build42-map\<mod_folder>" `
        -Output  ".local\reference-build42-geometry-<date>" `
        [-MaxFiles 20]

Example:
    .\scripts\inspect-build42-reference-geometry.ps1 `
        -Source  ".local\reference-build42-map\SomeMap42" `
        -Output  ".local\reference-build42-geometry-01"
#>

param(
    [Parameter(Mandatory=$true)]
    [string]$Source,

    [Parameter(Mandatory=$true)]
    [string]$Output,

    [int]$MaxFiles = 20
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

# ---------------------------------------------------------------------------
# Clamp MaxFiles
# ---------------------------------------------------------------------------

if ($MaxFiles -gt 100) { $MaxFiles = 100 }
if ($MaxFiles -lt 1)   { $MaxFiles = 1 }

Write-Output 'inspect-build42-reference-geometry.ps1'
Write-Output "Source:   $Source"
Write-Output "Output:   $Output"
Write-Output "MaxFiles: $MaxFiles"
Write-Output ''

# ---------------------------------------------------------------------------
# Guards: .local only
# ---------------------------------------------------------------------------

$sep         = [System.IO.Path]::DirectorySeparatorChar
$localMarker = $sep + '.local' + $sep

function Test-IsUnderLocal {
    param([string]$FullPath)
    $endsLocal = $FullPath.EndsWith($sep + '.local')
    return ($FullPath.Contains($localMarker) -or $endsLocal)
}

$sourceFull = [System.IO.Path]::GetFullPath($Source)
$outputFull = [System.IO.Path]::GetFullPath($Output)

if (-not (Test-IsUnderLocal $sourceFull)) {
    Write-Error "inspect-build42-reference-geometry: -Source must be under a .local/ directory: $sourceFull"
    exit 1
}

if (-not (Test-IsUnderLocal $outputFull)) {
    Write-Error "inspect-build42-reference-geometry: -Output must be under a .local/ directory: $outputFull"
    exit 1
}

# ---------------------------------------------------------------------------
# Guards: refuse Zomboid user data, Workshop, Server, PZ install paths
# ---------------------------------------------------------------------------

$forbiddenPatterns = @(
    'Zomboid' + $sep + 'mods',
    'Zomboid' + $sep + 'Workshop',
    'Zomboid' + $sep + 'Server',
    'steamapps' + $sep + 'common' + $sep + 'ProjectZomboid',
    'steamapps' + $sep + 'common' + $sep + 'Project Zomboid',
    'steamapps/common/ProjectZomboid',
    'steamapps/common/Project Zomboid'
)
foreach ($pat in $forbiddenPatterns) {
    if ($sourceFull -match [regex]::Escape($pat)) {
        Write-Error "inspect-build42-reference-geometry: -Source path contains forbidden location ($pat): $sourceFull"
        exit 1
    }
}

# ---------------------------------------------------------------------------
# Guard: source must exist
# ---------------------------------------------------------------------------

if (-not (Test-Path -LiteralPath $sourceFull)) {
    Write-Error "inspect-build42-reference-geometry: -Source path not found: $sourceFull"
    exit 1
}

# ---------------------------------------------------------------------------
# Helper: read bounded binary prefix (clamped to 1–256 bytes)
# ---------------------------------------------------------------------------

function Read-BoundedPrefix {
    param([string]$FilePath, [int]$MaxBytes = 64)
    if ($MaxBytes -gt 256) { $MaxBytes = 256 }
    if ($MaxBytes -lt 1)   { $MaxBytes = 1 }
    $stream = [System.IO.File]::OpenRead($FilePath)
    try {
        $buf  = [byte[]]::new($MaxBytes)
        $read = $stream.Read($buf, 0, $MaxBytes)
        $out  = [byte[]]::new($read)
        if ($read -gt 0) { [System.Array]::Copy($buf, $out, $read) }
        return $out
    }
    finally { $stream.Dispose() }
}

function Bytes-ToHex {
    param([byte[]]$Bytes)
    return ($Bytes | ForEach-Object { $_.ToString('x2') }) -join ''
}

# Read a bounded slice of a byte array as a hex string
function Slice-ToHex {
    param([byte[]]$Bytes, [int]$Len)
    $take = [Math]::Min($Len, $Bytes.Length)
    if ($take -le 0) { return '' }
    return ($Bytes[0..($take-1)] | ForEach-Object { $_.ToString('x2') }) -join ''
}

# Extract U32 LE words from a byte array, up to $MaxWords
function Get-U32Words {
    param([byte[]]$Bytes, [int]$MaxWords = 16)
    $words = [System.Collections.Generic.List[long]]::new()
    $pos   = 0
    while ($pos + 3 -lt $Bytes.Length -and $words.Count -lt $MaxWords) {
        $words.Add([long][System.BitConverter]::ToUInt32($Bytes, $pos))
        $pos += 4
    }
    return @($words)
}

# ---------------------------------------------------------------------------
# Scan source tree
# ---------------------------------------------------------------------------

Write-Output '--- Scanning source tree ---'

$allFiles = @(Get-ChildItem -LiteralPath $sourceFull -Recurse -File -ErrorAction SilentlyContinue |
    Sort-Object FullName)

$lotpackFiles    = @($allFiles | Where-Object { $_.Name -match '^world_\d+_\d+\.lotpack$'   } | Select-Object -First $MaxFiles)
$chunkdataFiles  = @($allFiles | Where-Object { $_.Name -match '^chunkdata_\d+_\d+\.bin$'   } | Select-Object -First $MaxFiles)
$lotheaderFiles  = @($allFiles | Where-Object { $_.Extension.ToLower() -eq '.lotheader'     } | Select-Object -First $MaxFiles)
$mapInfoFiles    = @($allFiles | Where-Object { $_.Name -eq 'map.info'                       } | Select-Object -First $MaxFiles)
$modInfoFiles    = @($allFiles | Where-Object { $_.Name -eq 'mod.info'                       } | Select-Object -First $MaxFiles)
$spawnFiles      = @($allFiles | Where-Object { $_.Name -eq 'spawnpoints.lua'                } | Select-Object -First $MaxFiles)

Write-Output "  .lotheader files:   $($lotheaderFiles.Count)"
Write-Output "  .lotpack files:     $($lotpackFiles.Count)"
Write-Output "  chunkdata .bin:     $($chunkdataFiles.Count)"
Write-Output "  map.info:           $($mapInfoFiles.Count)"
Write-Output "  mod.info:           $($modInfoFiles.Count)"
Write-Output "  spawnpoints.lua:    $($spawnFiles.Count)"
Write-Output ''

# ---------------------------------------------------------------------------
# Analyse .lotpack files
# ---------------------------------------------------------------------------

Write-Output '--- Analysing .lotpack files ---'

$lotpackRecords     = [System.Collections.Generic.List[object]]::new()
$hdrA_900_count     = 0
$hdrB_7204_count    = 0
$lotpack_lotp_count = 0

foreach ($f in $lotpackFiles) {
    $rel    = $f.FullName.Substring($sourceFull.Length).TrimStart($sep)
    $size   = $f.Length
    $prefix = Read-BoundedPrefix $f.FullName 64

    # Detect LOTP magic header (Build 42 format: 4C 4F 54 50 = "LOTP")
    $isLotp = ($prefix.Length -ge 4 -and
               $prefix[0] -eq 0x4C -and $prefix[1] -eq 0x4F -and
               $prefix[2] -eq 0x54 -and $prefix[3] -eq 0x50)

    if ($isLotp) {
        $lotpVersion = if ($prefix.Length -ge 8) { [System.BitConverter]::ToUInt32($prefix, 4) } else { [uint32]0 }
        $lotpack_lotp_count++
        $note = 'build42_lotp_format_detected'

        $first16hex = Slice-ToHex $prefix 16
        $first32hex = Slice-ToHex $prefix 32
        $first64hex = Bytes-ToHex $prefix
        $u32words   = Get-U32Words $prefix 16

        Write-Output "  $rel  size=$size  format=LOTP  version_field=$lotpVersion"
        $lotpackRecords.Add([ordered]@{
            file                    = $rel
            size_bytes              = $size
            lotpack_format          = 'LOTP'
            lotpack_magic           = 'LOTP'
            lotpack_version_field_u32le = [int]$lotpVersion
            first_16_bytes_hex      = $first16hex
            first_32_bytes_hex      = $first32hex
            first_64_bytes_hex      = $first64hex
            u32le_words_first_64    = $u32words
            note                    = $note
        })
    } else {
        # Legacy format: interpret bytes 0-3 as hdrA and 4-7 as hdrB.
        # Use Int64 for derived values to prevent overflow.
        $hdrA = if ($prefix.Length -ge 4) { [System.BitConverter]::ToUInt32($prefix, 0) } else { [uint32]0 }
        $hdrB = if ($prefix.Length -ge 8) { [System.BitConverter]::ToUInt32($prefix, 4) } else { [uint32]0 }

        [int64]$inferredTableBytes = [int64]$hdrA * 8
        [int64]$inferredTableEnd   = 8 + $inferredTableBytes

        if ($hdrA -eq 900)  { $hdrA_900_count++ }
        if ($hdrB -eq 7204) { $hdrB_7204_count++ }

        $note = if ($hdrA -eq 900 -and $hdrB -eq 7204) { 'matches_legacy_30x30_model' }
                elseif ($hdrA -eq 1024)                  { 'candidate_32x32_1024' }
                elseif ($hdrA -eq 256)                   { 'candidate_16x16_256' }
                else                                     { "hdrA=$hdrA hdrB=$hdrB (unknown_model)" }

        $first32hex = Slice-ToHex $prefix 32
        Write-Output "  $rel  size=$size  format=legacy  hdrA=$hdrA hdrB=$hdrB"
        $lotpackRecords.Add([ordered]@{
            file                     = $rel
            size_bytes               = $size
            lotpack_format           = 'legacy'
            first_32_bytes_hex       = $first32hex
            hdrA_u32le               = [int]$hdrA
            hdrB_u32le               = [int]$hdrB
            inferred_table_entries   = [int64]$hdrA
            inferred_table_bytes     = $inferredTableBytes
            inferred_table_end_byte  = $inferredTableEnd
            note                     = $note
        })
    }
}

# ---------------------------------------------------------------------------
# Analyse chunkdata_*.bin files
# ---------------------------------------------------------------------------

Write-Output ''
Write-Output '--- Analysing chunkdata_*.bin files ---'

$chunkdataRecords    = [System.Collections.Generic.List[object]]::new()
$body_900_count      = 0
$body_1024_count     = 0
$body_256_count      = 0

foreach ($f in $chunkdataFiles) {
    $rel        = $f.FullName.Substring($sourceFull.Length).TrimStart($sep)
    $size       = [int]$f.Length
    $bodyBytes  = $size - 2   # subtract 2-byte header
    $prefix     = Read-BoundedPrefix $f.FullName 32

    $headerByte0 = if ($prefix.Length -ge 1) { $prefix[0] } else { [byte]0 }
    $headerByte1 = if ($prefix.Length -ge 2) { $prefix[1] } else { [byte]0 }

    $first32hex = Bytes-ToHex $prefix
    $u32words   = Get-U32Words $prefix 8

    $gridCandidate = if ($bodyBytes -eq 900)  { $body_900_count++;  '30x30_900'  }
                     elseif ($bodyBytes -eq 1024) { $body_1024_count++; '32x32_1024' }
                     elseif ($bodyBytes -eq 256)  { $body_256_count++;  '16x16_256'  }
                     else                         { "unknown_body_$bodyBytes" }

    Write-Output "  $rel  size=$size  body_bytes=$bodyBytes  grid_candidate=$gridCandidate"

    $chunkdataRecords.Add([ordered]@{
        file                  = $rel
        size_bytes            = $size
        header_byte_0         = '0x{0:x2}' -f $headerByte0
        header_byte_1         = '0x{0:x2}' -f $headerByte1
        first_32_bytes_hex    = $first32hex
        u32le_words_first_32  = $u32words
        body_bytes            = $bodyBytes
        chunk_grid_candidate  = $gridCandidate
    })
}

# ---------------------------------------------------------------------------
# Analyse .lotheader files — detect LOTH magic (Build 42 format)
# ---------------------------------------------------------------------------

Write-Output ''
Write-Output '--- Analysing .lotheader files ---'

$lotheaderRecords    = [System.Collections.Generic.List[object]]::new()
$lotheader_loth_count = 0

foreach ($f in $lotheaderFiles) {
    $rel    = $f.FullName.Substring($sourceFull.Length).TrimStart($sep)
    $size   = $f.Length
    $prefix = Read-BoundedPrefix $f.FullName 64

    # Detect LOTH magic header (Build 42: 4C 4F 54 48 = "LOTH")
    # field0 as LE U32 = 0x48544F4C = 1213484876
    $isLoth = ($prefix.Length -ge 4 -and
               $prefix[0] -eq 0x4C -and $prefix[1] -eq 0x4F -and
               $prefix[2] -eq 0x54 -and $prefix[3] -eq 0x48)

    if ($isLoth) {
        $lothVersion = if ($prefix.Length -ge 8) { [System.BitConverter]::ToUInt32($prefix, 4) } else { [uint32]0 }
        $lotheader_loth_count++
        $note = 'build42_loth_format_detected'

        $first16hex = Slice-ToHex $prefix 16
        $first32hex = Slice-ToHex $prefix 32
        $first64hex = Bytes-ToHex $prefix
        $u32words   = Get-U32Words $prefix 16

        Write-Output "  $rel  size=$size  format=LOTH  version_field=$lothVersion"
        $lotheaderRecords.Add([ordered]@{
            file                          = $rel
            size_bytes                    = $size
            lotheader_format              = 'LOTH'
            lotheader_magic               = 'LOTH'
            lotheader_version_field_u32le = [int]$lothVersion
            first_16_bytes_hex            = $first16hex
            first_32_bytes_hex            = $first32hex
            first_64_bytes_hex            = $first64hex
            u32le_words_first_64          = $u32words
            note                          = $note
        })
    } else {
        # Legacy format (Build 41 style)
        $field0 = if ($prefix.Length -ge 4) { [System.BitConverter]::ToUInt32($prefix, 0) } else { [uint32]0 }
        $field1 = if ($prefix.Length -ge 8) { [System.BitConverter]::ToUInt32($prefix, 4) } else { [uint32]0 }
        $first32hex = Slice-ToHex $prefix 32
        $first64hex = Bytes-ToHex $prefix

        Write-Output "  $rel  size=$size  format=legacy  field0=$field0 field1=$field1"
        $lotheaderRecords.Add([ordered]@{
            file               = $rel
            size_bytes         = $size
            lotheader_format   = 'legacy'
            first_32_bytes_hex = $first32hex
            first_64_bytes_hex = $first64hex
            first_u32le        = [int]$field0
            second_u32le       = [int]$field1
        })
    }
}

# ---------------------------------------------------------------------------
# Read text file names only (no binary parsing)
# ---------------------------------------------------------------------------

$textFiles = [System.Collections.Generic.List[string]]::new()
foreach ($f in ($mapInfoFiles + $modInfoFiles + $spawnFiles)) {
    $rel = $f.FullName.Substring($sourceFull.Length).TrimStart($sep)
    $textFiles.Add($rel)
}

# ---------------------------------------------------------------------------
# Determine geometry statuses (array) and primary geometry_status string
# ---------------------------------------------------------------------------

$totalLotpack             = $lotpackRecords.Count
$totalChunkdata           = $chunkdataRecords.Count
$lotpack_legacy_900_count = $hdrA_900_count

$geometryStatuses = [System.Collections.Generic.List[string]]::new()

if ($lotpack_lotp_count -gt 0) {
    [void]$geometryStatuses.Add('BUILD42_LOTP_FORMAT_OBSERVED')
}
if ($lotheader_loth_count -gt 0) {
    [void]$geometryStatuses.Add('BUILD42_LOTH_LOTHEADER_FORMAT_OBSERVED')
}
if ($body_1024_count -gt 0) {
    [void]$geometryStatuses.Add('BUILD42_32X32_CHUNK_GRID_OBSERVED')
}
if ($body_900_count -gt 0) {
    [void]$geometryStatuses.Add('BUILD41_30X30_CHUNK_GRID_OBSERVED')
}
if ($lotpack_lotp_count -gt 0 -and $lotheader_loth_count -gt 0 -and $body_1024_count -gt 0) {
    [void]$geometryStatuses.Add('BUILD42_256_MODEL_STRONGLY_SUPPORTED')
}
[void]$geometryStatuses.Add('GEOMETRY_MODEL_STILL_NOT_LOAD_TESTED')
[void]$geometryStatuses.Add('PLAYABLE_EXPORT_CLAIM_ALLOWED=false')

# Primary geometry status (highest-priority single string)
$geometryStatus = 'BUILD42_GEOMETRY_STILL_UNKNOWN'
if ($geometryStatuses -contains 'BUILD42_256_MODEL_STRONGLY_SUPPORTED') {
    $geometryStatus = 'BUILD42_256_MODEL_STRONGLY_SUPPORTED'
} elseif ($lotpack_lotp_count -gt 0 -and $lotheader_loth_count -gt 0) {
    $geometryStatus = 'BUILD42_LOTP_LOTH_FORMAT_OBSERVED'
} elseif ($lotpack_lotp_count -gt 0) {
    $geometryStatus = 'BUILD42_LOTP_FORMAT_OBSERVED'
} elseif ($hdrA_900_count -gt 0 -and $body_900_count -gt 0 -and $body_1024_count -eq 0 -and $body_256_count -eq 0) {
    $geometryStatus = 'BUILD42_300_MODEL_SUPPORTED'
} elseif ($body_1024_count -gt 0 -or $body_256_count -gt 0) {
    $geometryStatus = 'BUILD42_256_MODEL_SUPPORTED'
} elseif ($hdrA_900_count -gt 0 -or $body_900_count -gt 0) {
    $geometryStatus = 'BUILD42_300_MODEL_PARTIALLY_SUPPORTED'
}

Write-Output ''
Write-Output "--- Geometry status: $geometryStatus ---"
Write-Output "  geometry_statuses: $($geometryStatuses -join ', ')"
Write-Output "  lotpack_lotp_count:        $lotpack_lotp_count"
Write-Output "  lotpack_legacy_900_count:  $lotpack_legacy_900_count"
Write-Output "  lotheader_loth_count:       $lotheader_loth_count"

# ---------------------------------------------------------------------------
# Write output
# ---------------------------------------------------------------------------

New-Item -ItemType Directory -Force -Path $outputFull | Out-Null

$generatedAt = (Get-Date).ToUniversalTime().ToString('yyyy-MM-ddTHH:mm:ssZ')

$reportJson = [ordered]@{
    schema                      = 'pzmapforge.build42-reference-geometry-report.v0.2'
    claim_boundary              = 'evidence_record_only_not_load_tested_not_playable'
    generated_at_utc            = $generatedAt
    source_path                 = $sourceFull.Replace('\','/')
    geometry_status             = $geometryStatus
    geometry_statuses           = @($geometryStatuses)
    REFERENCE_GEOMETRY_OBSERVED = $true
    PLAYABLE_EXPORT_CLAIM_ALLOWED = 'false'
    lotpack_count               = $totalLotpack
    lotpack_lotp_count          = $lotpack_lotp_count
    lotpack_legacy_900_count    = $lotpack_legacy_900_count
    lotpack_hdrA_900_count      = $hdrA_900_count
    lotpack_hdrB_7204_count     = $hdrB_7204_count
    chunkdata_count             = $totalChunkdata
    chunkdata_body_900_count    = $body_900_count
    chunkdata_body_1024_count   = $body_1024_count
    chunkdata_body_256_count    = $body_256_count
    lotheader_count             = $lotheaderRecords.Count
    lotheader_loth_count         = $lotheader_loth_count
    text_files_found            = @($textFiles)
    lotpack_records             = @($lotpackRecords)
    chunkdata_records           = @($chunkdataRecords)
    lotheader_records           = @($lotheaderRecords)
    safety = [ordered]@{
        reference_files_copied          = $false
        pz_assets_copied                = $false
        media_maps_touched_in_repo      = $false
        playable_export_claimed         = $false
        compiled_writer_implemented     = $false
        load_test_performed             = $false
        only_prefix_bytes_read          = $true
    }
}

$jsonPath = Join-Path $outputFull 'build42-reference-geometry-report.json'
$reportJson | ConvertTo-Json -Depth 8 | Set-Content -Path $jsonPath -Encoding UTF8
Write-Output "Report JSON: $jsonPath"

$md = @"
# Build 42 Reference Geometry Report

Schema: pzmapforge.build42-reference-geometry-report.v0.2
Generated: $generatedAt
Source: $($sourceFull.Replace('\','/'))

## Geometry status

**$geometryStatus**

Statuses: $($geometryStatuses -join ' | ')

## Observed counts

| Type | Found | Detail |
|---|---:|---|
| .lotpack files | $totalLotpack | LOTP: $lotpack_lotp_count, legacy-900: $lotpack_legacy_900_count |
| chunkdata .bin | $totalChunkdata | body=900: $body_900_count, body=1024: $body_1024_count, body=256: $body_256_count |
| .lotheader | $($lotheaderRecords.Count) | LOTH: $lotheader_loth_count |
| Text files | $($textFiles.Count) | — |

## .lotpack records
$(foreach ($r in $lotpackRecords) {
    if ($r['lotpack_format'] -eq 'LOTP') { "- $($r['file']) size=$($r['size_bytes']) format=LOTP version=$($r['lotpack_version_field_u32le'])" }
    else { "- $($r['file']) size=$($r['size_bytes']) format=legacy hdrA=$($r['hdrA_u32le']) hdrB=$($r['hdrB_u32le'])" }
})

## chunkdata records
$(foreach ($r in $chunkdataRecords) { "- $($r['file']) size=$($r['size_bytes']) body=$($r['body_bytes']) grid=$($r['chunk_grid_candidate'])" })

## .lotheader records
$(foreach ($r in $lotheaderRecords) {
    if ($r['lotheader_format'] -eq 'LOTH') { "- $($r['file']) size=$($r['size_bytes']) format=LOTH version=$($r['lotheader_version_field_u32le'])" }
    else { "- $($r['file']) size=$($r['size_bytes']) format=legacy field0=$($r['first_u32le'])" }
})

## Safety

| Property | Value |
|---|---|
| Reference files copied | false |
| PZ assets copied | false |
| media/maps touched in repo | false |
| Playable export claimed | false |
| Compiled writer implemented | false |
| Load test performed | false |
| Only prefix bytes read | true |

## Non-claims

- Not a load test.
- Not a playable export.
- No PZ assets copied or read into repo.
- No files modified in source.
- PLAYABLE_EXPORT_CLAIM_ALLOWED=false
"@

$mdPath = Join-Path $outputFull 'build42-reference-geometry-report.md'
Set-Content -Path $mdPath -Value $md -Encoding UTF8
Write-Output "Report MD:   $mdPath"
Write-Output ''
Write-Output "Geometry status:  $geometryStatus"
Write-Output "reference_files_copied: false"
Write-Output "pz_assets_copied:       false"
Write-Output "playable_export_claimed: false"
Write-Output 'Done.'
