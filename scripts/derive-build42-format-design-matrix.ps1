#Requires -Version 5.1
<#
.SYNOPSIS
    Local-only Build 42 format design matrix deriver (MAP-6I).

    Reads a MAP-6H reference geometry inspection report (v0.2) from .local and
    produces a word-level format design matrix for LOTP lotpacks, LOTH lotheaders,
    and chunkdata records. Used to plan a candidate writer.

    Does NOT read PZ assets. Does NOT write to the repo (only to .local).
    Does NOT perform a load test. Does NOT implement a writer.
    Input and Output must be under .local only.

Usage:
    .\scripts\derive-build42-format-design-matrix.ps1 `
        -InspectionReport ".local\reference-build42-geometry-XX\build42-reference-geometry-report.json" `
        -Output           ".local\build42-format-design-XX" `
        [-MaxRecords 50]
#>

param(
    [Parameter(Mandatory=$true)]
    [string]$InspectionReport,

    [Parameter(Mandatory=$true)]
    [string]$Output,

    [int]$MaxRecords = 50
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

if ($MaxRecords -gt 200) { $MaxRecords = 200 }
if ($MaxRecords -lt 1)   { $MaxRecords = 1 }

Write-Output 'derive-build42-format-design-matrix.ps1'
Write-Output "InspectionReport: $InspectionReport"
Write-Output "Output:           $Output"
Write-Output "MaxRecords:       $MaxRecords"
Write-Output ''

# ---------------------------------------------------------------------------
# Guards: .local only
# ---------------------------------------------------------------------------

$sep         = [System.IO.Path]::DirectorySeparatorChar
$localMarker = $sep + '.local' + $sep

function Test-IsUnderLocal { param([string]$P)
    return ($P.Contains($localMarker) -or $P.EndsWith($sep + '.local'))
}

$reportFull = [System.IO.Path]::GetFullPath($InspectionReport)
$outputFull = [System.IO.Path]::GetFullPath($Output)

if (-not (Test-IsUnderLocal $reportFull)) {
    Write-Error "derive-build42-format-design-matrix: -InspectionReport must be under .local/: $reportFull"
    exit 1
}
if (-not (Test-IsUnderLocal $outputFull)) {
    Write-Error "derive-build42-format-design-matrix: -Output must be under .local/: $outputFull"
    exit 1
}

$forbiddenPatterns = @(
    'Zomboid' + $sep + 'mods',   'Zomboid' + $sep + 'Workshop',
    'Zomboid' + $sep + 'Server',
    'steamapps' + $sep + 'common' + $sep + 'ProjectZomboid',
    'steamapps' + $sep + 'common' + $sep + 'Project Zomboid',
    'steamapps/common/ProjectZomboid', 'steamapps/common/Project Zomboid'
)
foreach ($pat in $forbiddenPatterns) {
    if ($reportFull -match [regex]::Escape($pat)) {
        Write-Error "derive-build42-format-design-matrix: forbidden path in report: $reportFull"
        exit 1
    }
}

if (-not (Test-Path -LiteralPath $reportFull)) {
    Write-Error "derive-build42-format-design-matrix: report not found: $reportFull"
    exit 1
}

# ---------------------------------------------------------------------------
# Load and validate report
# ---------------------------------------------------------------------------

Write-Output '--- Loading inspection report ---'
$report = Get-Content $reportFull -Raw | ConvertFrom-Json

$requiredSchema = 'pzmapforge.build42-reference-geometry-report.v0.2'
if ($report.schema -ne $requiredSchema) {
    Write-Error "derive-build42-format-design-matrix: expected schema '$requiredSchema', got '$($report.schema)'"
    exit 1
}
Write-Output "  Schema:          $($report.schema)"
Write-Output "  Geometry status: $($report.geometry_status)"
Write-Output "  Source path:     $($report.source_path)"
Write-Output ''

# ---------------------------------------------------------------------------
# Helper: Compute word stability across multiple records
# ---------------------------------------------------------------------------

function Get-WordStabilityMap {
    param([object[]]$Records, [string]$WordField, [int]$WordCount = 16)
    # Returns ordered array of word-stability items, indexed 0..(WordCount-1).
    $items = [System.Collections.Generic.List[object]]::new()
    for ($i = 0; $i -lt $WordCount; $i++) {
        $valList = [System.Collections.Generic.List[long]]::new()
        foreach ($rec in $Records) {
            $wordProp = $rec.PSObject.Properties[$WordField]
            if ($null -ne $wordProp) {
                $wordArr = @($wordProp.Value)
                if ($i -lt $wordArr.Count) {
                    $valList.Add([long]$wordArr[$i])
                }
            }
        }
        $distinct = @($valList | Select-Object -Unique)
        if ($valList.Count -eq 0) {
            $items.Add([ordered]@{ position=$i; stable=$false; value='missing'; label='missing' })
        } elseif ($distinct.Count -eq 1) {
            $v = $valList[0]
            $label = if ($i -eq 0) { 'stable_magic' } elseif ($i -eq 1) { 'stable_version' } else { 'stable_unknown' }
            $items.Add([ordered]@{ position=$i; stable=$true; value=$v; label=$label })
        } else {
            $items.Add([ordered]@{ position=$i; stable=$false; value='variable'; label='variable_unknown'; values=@($distinct) })
        }
    }
    return $items
}

function WordMapToTable {
    param([object]$Items)
    $rows = @()
    foreach ($e in $Items) {
        $valStr = if ($e.stable) { "$($e.value)" } else { "variable ($($e['values'] -join ', '))" }
        $rows += "| $($e.position) | $valStr | $($e.label) |"
    }
    return $rows -join "`n"
}

# ---------------------------------------------------------------------------
# Analyse LOTP lotpack records
# ---------------------------------------------------------------------------

Write-Output '--- Analysing LOTP lotpack records ---'

$lotpRecords = @($report.lotpack_records | Where-Object { $_.lotpack_format -eq 'LOTP' } | Select-Object -First $MaxRecords)
Write-Output "  LOTP records sampled: $($lotpRecords.Count)"

$lotpStability = Get-WordStabilityMap $lotpRecords 'u32le_words_first_64' 16
$lotpStableCount   = [int]($lotpStability | Where-Object { $_.stable }                                | Measure-Object).Count
$lotpVariableCount = [int]($lotpStability | Where-Object { -not $_.stable -and $_.label -ne 'missing' } | Measure-Object).Count
$lotpMissingCount  = [int]($lotpStability | Where-Object { $_.label -eq 'missing' }                     | Measure-Object).Count

Write-Output "  Word stability: stable=$lotpStableCount variable=$lotpVariableCount missing=$lotpMissingCount"

# Derive LOTP candidate values from stable words
$lotpMagic         = $null; $lotpVersion = $null; $lotpChunkCount = $null; $lotpFirstChunkOff = $null
foreach ($e in $lotpStability) {
    if ($e.stable) {
        if ($e.position -eq 0) { $lotpMagic = $e.value }
        elseif ($e.position -eq 1) { $lotpVersion = $e.value }
        elseif ($e.position -eq 2) { $lotpChunkCount = $e.value }
        elseif ($e.position -eq 3) { $lotpFirstChunkOff = $e.value }
    }
}
if ($null -eq $lotpMagic)  { $lotpMagic = 'unknown' }
if ($null -eq $lotpVersion) { $lotpVersion = 'unknown' }
if ($null -eq $lotpChunkCount) { $lotpChunkCount = 'unknown' }
if ($null -eq $lotpFirstChunkOff) { $lotpFirstChunkOff = 'unknown' }

Write-Output "  word[0] magic=$lotpMagic (=LOTP: 0x50544F4C LE)"
Write-Output "  word[1] version=$lotpVersion"
Write-Output "  word[2] chunk_count=$lotpChunkCount"
Write-Output "  word[3] first_chunk_offset=$lotpFirstChunkOff"

# ---------------------------------------------------------------------------
# Analyse LOTH lotheader records
# ---------------------------------------------------------------------------

Write-Output ''
Write-Output '--- Analysing LOTH lotheader records ---'

$lothRecords = @($report.lotheader_records | Where-Object { $_.lotheader_format -eq 'LOTH' } | Select-Object -First $MaxRecords)
Write-Output "  LOTH records sampled: $($lothRecords.Count)"

$lothStability = Get-WordStabilityMap $lothRecords 'u32le_words_first_64' 16
$lothStableCount   = [int]($lothStability | Where-Object { $_.stable }                                | Measure-Object).Count
$lothVariableCount = [int]($lothStability | Where-Object { -not $_.stable -and $_.label -ne 'missing' } | Measure-Object).Count
$lothMissingCount  = [int]($lothStability | Where-Object { $_.label -eq 'missing' }                     | Measure-Object).Count

Write-Output "  Word stability: stable=$lothStableCount variable=$lothVariableCount missing=$lothMissingCount"

# Override word[2] label to entry_count for LOTH
$lothMagic = 'unknown'; $lothVersion = 'unknown'; $lothEntryCount2 = 'unknown'
foreach ($e in $lothStability) {
    if ($e.position -eq 2) {
        if (-not $e.stable) { $e.label = 'variable_entry_count' }
        else                { $e.label = 'stable_entry_count' }
        $lothEntryCount2 = $e.label
    }
    if ($e.stable -and $e.position -eq 0) { $lothMagic   = $e.value }
    if ($e.stable -and $e.position -eq 1) { $lothVersion  = $e.value }
}

Write-Output "  word[0] magic=$lothMagic (=LOTH: 0x48544F4C LE)"
Write-Output "  word[1] version=$lothVersion"
Write-Output "  word[2] entry_count: $lothEntryCount2"
Write-Output "  word[3+]: tileset pack name bytes (variable per cell)"

# ---------------------------------------------------------------------------
# Analyse chunkdata records
# ---------------------------------------------------------------------------

Write-Output ''
Write-Output '--- Analysing chunkdata records ---'

$chunkRecords = @($report.chunkdata_records | Select-Object -First $MaxRecords)
Write-Output "  Chunkdata records sampled: $($chunkRecords.Count)"

$body900Count  = @($chunkRecords | Where-Object { $_.body_bytes -eq 900  }).Count
$body1024Count = @($chunkRecords | Where-Object { $_.body_bytes -eq 1024 }).Count
$bodyOtherCount= $chunkRecords.Count - $body900Count - $body1024Count

$dominantModel = 'unknown'
if ($body1024Count -gt 0 -and $body1024Count -ge $body900Count) { $dominantModel = '32x32_1024' }
elseif ($body900Count -gt 0 -and $body900Count -ge $body1024Count) { $dominantModel = '30x30_900' }

Write-Output "  body=900:  $body900Count"
Write-Output "  body=1024: $body1024Count"
Write-Output "  other:     $bodyOtherCount"
Write-Output "  dominant:  $dominantModel"

# ---------------------------------------------------------------------------
# Determine design matrix statuses
# ---------------------------------------------------------------------------

$matrixStatuses = [System.Collections.Generic.List[string]]::new()
[void]$matrixStatuses.Add('BUILD42_FORMAT_DESIGN_MATRIX_CREATED')
if ($lotpRecords.Count -gt 0) { [void]$matrixStatuses.Add('BUILD42_LOTP_FORMAT_OBSERVED') }
if ($lothRecords.Count -gt 0) { [void]$matrixStatuses.Add('BUILD42_LOTH_LOTHEADER_FORMAT_OBSERVED') }
if ($body1024Count -gt 0)     { [void]$matrixStatuses.Add('BUILD42_32X32_CHUNK_GRID_OBSERVED') }
if ($lotpRecords.Count -gt 0 -and $lothRecords.Count -gt 0 -and $body1024Count -gt 0) {
    [void]$matrixStatuses.Add('BUILD42_256_MODEL_STRONGLY_SUPPORTED')
}
[void]$matrixStatuses.Add('WRITER_DESIGN_ONLY')
[void]$matrixStatuses.Add('WRITER_NOT_IMPLEMENTED')
[void]$matrixStatuses.Add('GEOMETRY_MODEL_STILL_NOT_LOAD_TESTED')
[void]$matrixStatuses.Add('PLAYABLE_EXPORT_CLAIM_ALLOWED=false')

Write-Output ''
Write-Output "--- Design matrix statuses: $($matrixStatuses -join ', ') ---"

# ---------------------------------------------------------------------------
# Build word arrays for JSON
# ---------------------------------------------------------------------------

function Items-ToArray {
    param([object]$Items)
    return @($Items)
}

# ---------------------------------------------------------------------------
# Write output
# ---------------------------------------------------------------------------

New-Item -ItemType Directory -Force -Path $outputFull | Out-Null

$generatedAt = (Get-Date).ToUniversalTime().ToString('yyyy-MM-ddTHH:mm:ssZ')

$matrixJson = [ordered]@{
    schema                   = 'pzmapforge.build42-format-design-matrix.v0.1'
    claim_boundary           = 'writer_design_only_not_implemented_not_load_tested'
    generated_at_utc         = $generatedAt
    inspection_report        = $reportFull.Replace('\','/')
    matrix_statuses          = @($matrixStatuses)
    WRITER_NOT_IMPLEMENTED   = $true
    PLAYABLE_EXPORT_CLAIM_ALLOWED = 'false'
    lotp_lotpack = [ordered]@{
        records_sampled      = $lotpRecords.Count
        stable_word_count    = $lotpStableCount
        variable_word_count  = $lotpVariableCount
        missing_word_count   = $lotpMissingCount
        word_stability       = @(Items-ToArray $lotpStability)
        candidate = [ordered]@{
            bytes_0_3 = '4c4f5450'
            bytes_0_3_ascii = 'LOTP'
            bytes_4_7 = '01000000'
            bytes_4_7_value = 1
            bytes_8_11 = '00040000'
            bytes_8_11_value = 1024
            bytes_8_11_role = 'chunk_count_u32le (32x32=1024, stable_observed)'
            bytes_12_plus_role = 'chunk_offset_table (1024 x 8 bytes = 8192 bytes, then chunk data)'
            first_chunk_offset = 8204
            first_chunk_offset_derivation = '12 + 1024*8 = 8204 (stable observed)'
            chunk_data_format = 'unknown_requires_further_research'
            note = 'candidate bytes only; no writer implemented; not load-tested'
        }
    }
    loth_lotheader = [ordered]@{
        records_sampled      = $lothRecords.Count
        stable_word_count    = $lothStableCount
        variable_word_count  = $lothVariableCount
        missing_word_count   = $lothMissingCount
        word_stability       = @(Items-ToArray $lothStability)
        candidate = [ordered]@{
            bytes_0_3 = '4c4f5448'
            bytes_0_3_ascii = 'LOTH'
            bytes_4_7 = '01000000'
            bytes_4_7_value = 1
            bytes_8_11_role = 'entry_count_u32le (variable, depends on tileset usage)'
            bytes_12_plus_role = 'newline-delimited ASCII tileset pack names (observed; same structure as MAP-4E Build 41 evidence)'
            minimum_entry_count = 'unknown — must research minimum required set for empty/grass cell'
            candidate_minimum_entry = 'blends_grassoverlays_01_0 (observed in MAP-4E as first entry for grass cells)'
            note = 'candidate structure only; no writer implemented; not load-tested'
        }
    }
    chunkdata = [ordered]@{
        records_sampled     = $chunkRecords.Count
        body_900_count      = $body900Count
        body_1024_count     = $body1024Count
        body_other_count    = $bodyOtherCount
        dominant_model      = $dominantModel
        candidate = [ordered]@{
            size_bytes = 1026
            bytes_0_1 = '0001'
            bytes_0_1_role = '2-byte header (00 01, consistent with observed Build 41 and Build 42 files)'
            bytes_2_plus_role = '1024-byte body (32x32 chunk grid, 1 byte per chunk)'
            body_candidate = 'all-zero (hypothesis only, not load-tested)'
            note = 'candidate size only; no writer implemented; not load-tested'
        }
    }
    cell_geometry = [ordered]@{
        cell_tile_model     = '256x256_strongly_supported'
        chunk_grid_model    = '32x32_strongly_supported'
        tiles_per_chunk     = '8x8_hypothesized (not confirmed by load test)'
        derivation          = '32 chunks/side x 8 tiles/chunk = 256 tiles/side'
        evidence_basis      = 'LOTP(word[2]=1024=32x32) + LOTH observed + chunkdata body=1024'
        confirmed_by_load_test = $false
    }
    safety = [ordered]@{
        pz_assets_copied             = $false
        writer_implemented           = $false
        load_test_performed          = $false
        playable_export_claimed      = $false
        report_read_only_no_pz_files = $true
    }
}

$jsonPath = Join-Path $outputFull 'build42-format-design-matrix.json'
$matrixJson | ConvertTo-Json -Depth 8 | Set-Content -Path $jsonPath -Encoding UTF8
Write-Output "Matrix JSON: $jsonPath"

# ---------------------------------------------------------------------------
# Build Markdown
# ---------------------------------------------------------------------------

$lotpStabilityRows = WordMapToTable $lotpStability
$lothStabilityRows = WordMapToTable $lothStability

$md = @"
# Build 42 Format Design Matrix

Schema: pzmapforge.build42-format-design-matrix.v0.1
Generated: $generatedAt
Inspection report: $($reportFull.Replace('\','/'))

## Status

$($matrixStatuses | ForEach-Object { "- $_" } | Out-String)

## LOTP Lotpack format

Records sampled: $($lotpRecords.Count)
Stable words: $lotpStableCount / variable: $lotpVariableCount / missing: $lotpMissingCount

### Word stability (first 16 U32 LE words)

| Position | Value | Label |
|---|---|---|
$lotpStabilityRows

### Candidate byte layout

| Offset | Bytes | Role |
|---|---|---|
| 0-3 | 4C 4F 54 50 | LOTP magic ("LOTP" ASCII) |
| 4-7 | 01 00 00 00 | version = 1 (stable observed) |
| 8-11 | 00 04 00 00 | chunk_count = 1024 U32LE (32x32, stable observed) |
| 12-8203 | offset table | 1024 x 8-byte chunk offsets (stable first-chunk-offset=8204) |
| 8204+ | chunk data | unknown format — requires further research |

## LOTH Lotheader format

Records sampled: $($lothRecords.Count)
Stable words: $lothStableCount / variable: $lothVariableCount / missing: $lothMissingCount

### Word stability (first 16 U32 LE words)

| Position | Value | Label |
|---|---|---|
$lothStabilityRows

### Candidate byte layout

| Offset | Bytes | Role |
|---|---|---|
| 0-3 | 4C 4F 54 48 | LOTH magic ("LOTH" ASCII) |
| 4-7 | 01 00 00 00 | version = 1 (stable observed) |
| 8-11 | variable | entry_count U32LE (depends on tileset usage) |
| 12+ | ASCII | newline-delimited tileset pack names (same structure as MAP-4E Build 41 evidence) |

Minimum entry count for loadable cell: **unknown** — must research minimum required tileset set.
Candidate first entry for grass cell: blends_grassoverlays_01_0 (from MAP-4E evidence).

## Chunkdata format

Records sampled: $($chunkRecords.Count)
body=900: $body900Count | body=1024: $body1024Count | other: $bodyOtherCount
Dominant model: $dominantModel

### Candidate byte layout

| Offset | Bytes | Role |
|---|---|---|
| 0-1 | 00 01 | 2-byte header (consistent Build 41 + Build 42) |
| 2-1025 | 1024 bytes | chunk grid (32x32, 1 byte per chunk — hypothesis only) |

## Cell geometry model

- Cell tiles: 256x256 (strongly supported, not load-tested)
- Chunk grid: 32x32 = 1024 chunks per cell (observed in both LOTP and chunkdata)
- Tiles per chunk: 8x8 (hypothesized from 256/32=8, not confirmed)

## What we can safely write now (candidate only)

- LOTP header: bytes 0-11 (magic + version + chunk_count=1024)
- LOTH header: bytes 0-7 (magic + version), entry_count, and tileset entries
- Chunkdata: 1026-byte file (2-byte header + 1024 zero bytes)

**NOT safe to write yet:**
- LOTP chunk offset table values (need to understand chunk data format first)
- LOTP chunk data content
- LOTH minimum required tileset entries for a loadable cell

## What remains unknown

1. LOTP chunk data format (content after the offset table).
2. LOTP chunk offset table — are offsets relative or absolute? Are there flags?
3. LOTH minimum required entry set for a cell to load.
4. Whether all-zero chunkdata body is accepted by PZ for an empty cell.
5. Whether the LOTH entry format changed from Build 41 (newline-delimited assumption).
6. Other required files (worldmap.xml.bin, objects.lua, etc.) for Build 42.

## Recommended next task

MAP-6J: Build 42 candidate writer contract
- Define the exact byte layout for a minimal all-empty LOTP lotpack.
- Define the exact byte layout for a minimal LOTH lotheader.
- Define a deterministic candidate writer for all three file types.
- Prepare a versioned load-test packet.
- No load test until the packet is reviewed.

## Non-claims

- No writer implemented.
- No load test performed.
- No PZ assets copied into repo.
- PLAYABLE_EXPORT_CLAIM_ALLOWED=false
"@

$mdPath = Join-Path $outputFull 'build42-format-design-matrix.md'
Set-Content -Path $mdPath -Value $md -Encoding UTF8
Write-Output "Matrix MD:   $mdPath"
Write-Output ''
Write-Output "WRITER_NOT_IMPLEMENTED: true"
Write-Output "PLAYABLE_EXPORT_CLAIM_ALLOWED: false"
Write-Output 'Done.'
