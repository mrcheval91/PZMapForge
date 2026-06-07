#Requires -Version 5.1
<#
.SYNOPSIS
    MAP-7U: Inspects and compares cell coordinates, spawn coordinates, and
    map.info zoom fields between two Workshop mod roots.

    Reads only the two explicit roots provided by the operator.
    Does NOT crawl arbitrary directories.
    Output is under .local/ only.

    Writes:
      <Output>/workshop-cell-coordinate-contract.json
      <Output>/workshop-cell-coordinate-contract.md

.PARAMETER CandidateModRoot
    Path to the PZMapForge candidate mod root.

.PARAMETER ReferenceModRoot
    Path to the reference (Dru_map) mod root.

.PARAMETER CandidateMapId
    Map ID within the candidate root.

.PARAMETER ReferenceMapId
    Map ID within the reference root.

.PARAMETER Output
    Must be under .local/. Receives JSON and MD reports.
#>

param(
    [Parameter(Mandatory=$true)][string]$CandidateModRoot,
    [Parameter(Mandatory=$true)][string]$ReferenceModRoot,
    [Parameter(Mandatory=$true)][string]$CandidateMapId,
    [Parameter(Mandatory=$true)][string]$ReferenceMapId,
    [Parameter(Mandatory=$true)][string]$Output
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Assert-LocalPath {
    param([string]$Path, [string]$Label)
    $norm = $Path.Replace('/', '\')
    if ($norm -notmatch '\\\.local(\\|$)') {
        Write-Error "$Label must be under .local/. Got: $Path"
        exit 1
    }
}

Assert-LocalPath $Output '-Output'

New-Item -ItemType Directory -Force -Path $Output | Out-Null

Write-Output "Cell coordinate contract inspection"
Write-Output "Candidate: $CandidateModRoot"
Write-Output "Reference: $ReferenceModRoot"
Write-Output ""

# ---------------------------------------------------------------------------
# Helper: find map data directory inside a mod root
# ---------------------------------------------------------------------------

function Find-MapDataDir {
    param([string]$ModRoot, [string]$MapId)
    $candidates = @(
        (Join-Path $ModRoot "common\media\maps\$MapId"),
        (Join-Path $ModRoot "media\maps\$MapId"),
        (Join-Path $ModRoot "42\media\maps\$MapId"),
        (Join-Path $ModRoot "mods\$MapId\media\maps\$MapId"),
        (Join-Path $ModRoot "Contents\mods\$MapId\media\maps\$MapId")
    )
    foreach ($d in $candidates) {
        if (Test-Path -LiteralPath $d) { return $d }
    }
    return ''
}

# ---------------------------------------------------------------------------
# Helper: extract cell coordinates from lotheader filenames
# ---------------------------------------------------------------------------

function Get-CellCoordinates {
    param([string]$MapDir)
    if ($MapDir -eq '' -or -not (Test-Path -LiteralPath $MapDir)) {
        return [ordered]@{
            lotheader_count    = 0
            lotpack_count      = 0
            chunkdata_count    = 0
            cells              = [string[]]@()
            min_cell_x         = $null
            max_cell_x         = $null
            min_cell_y         = $null
            max_cell_y         = $null
            first_40_cells     = [string[]]@()
        }
    }

    $lhFiles = @(Get-ChildItem -LiteralPath $MapDir -Filter '*.lotheader' -ErrorAction SilentlyContinue)
    $lpFiles = @(Get-ChildItem -LiteralPath $MapDir -Filter '*.lotpack'   -ErrorAction SilentlyContinue)
    $cdFiles = @(Get-ChildItem -LiteralPath $MapDir -Filter 'chunkdata_*.bin' -ErrorAction SilentlyContinue)

    $cellList = [System.Collections.Generic.List[string]]::new()
    $xs       = [System.Collections.Generic.List[int]]::new()
    $ys       = [System.Collections.Generic.List[int]]::new()

    foreach ($f in $lhFiles) {
        if ($f.Name -match '^(\d+)_(\d+)\.lotheader$') {
            $cx = [int]$Matches[1]
            $cy = [int]$Matches[2]
            $cellList.Add("$cx,$cy")
            $xs.Add($cx)
            $ys.Add($cy)
        }
    }

    $sorted     = [string[]]@($cellList | Sort-Object)
    $first40    = [string[]]@($sorted | Select-Object -First 40)
    $minX = if ($xs.Count -gt 0) { ($xs | Measure-Object -Minimum).Minimum } else { $null }
    $maxX = if ($xs.Count -gt 0) { ($xs | Measure-Object -Maximum).Maximum } else { $null }
    $minY = if ($ys.Count -gt 0) { ($ys | Measure-Object -Minimum).Minimum } else { $null }
    $maxY = if ($ys.Count -gt 0) { ($ys | Measure-Object -Maximum).Maximum } else { $null }

    return [ordered]@{
        lotheader_count    = $lhFiles.Count
        lotpack_count      = $lpFiles.Count
        chunkdata_count    = $cdFiles.Count
        cells              = $sorted
        min_cell_x         = $minX
        max_cell_x         = $maxX
        min_cell_y         = $minY
        max_cell_y         = $maxY
        first_40_cells     = $first40
    }
}

# ---------------------------------------------------------------------------
# Helper: parse map.info zoom fields
# ---------------------------------------------------------------------------

function Get-MapInfoZoom {
    param([string]$MapDir)
    $result = [ordered]@{
        zoom_x = $null
        zoom_y = $null
        zoom_s = $null
    }
    if ($MapDir -eq '') { return $result }
    $miPath = Join-Path $MapDir 'map.info'
    if (-not (Test-Path -LiteralPath $miPath)) { return $result }

    $content = [System.IO.File]::ReadAllText($miPath)
    if ($content -match '(?m)^zoomX\s*=\s*(.+)$') { $result.zoom_x = $Matches[1].Trim() }
    if ($content -match '(?m)^zoomY\s*=\s*(.+)$') { $result.zoom_y = $Matches[1].Trim() }
    if ($content -match '(?m)^zoomS\s*=\s*(.+)$') { $result.zoom_s = $Matches[1].Trim() }
    return $result
}

# ---------------------------------------------------------------------------
# Helper: parse spawnpoints.lua spawn coordinate pairs
# ---------------------------------------------------------------------------

function Get-SpawnCoordinates {
    param([string]$MapDir)
    $result = [ordered]@{
        spawnpoints_present     = $false
        spawn_pairs             = [string[]]@()
        first_world_x           = $null
        first_world_y           = $null
        first_pos_x             = $null
        first_pos_y             = $null
    }
    if ($MapDir -eq '') { return $result }
    $spPath = Join-Path $MapDir 'spawnpoints.lua'
    if (-not (Test-Path -LiteralPath $spPath)) { return $result }

    $result.spawnpoints_present = $true
    $content = [System.IO.File]::ReadAllText($spPath)

    $pairList = [System.Collections.Generic.List[string]]::new()
    $lines = $content -split '\r?\n'
    foreach ($line in $lines) {
        if ($line -match 'worldX\s*=\s*(\d+)' -and $line -match 'worldY\s*=\s*(\d+)') {
            $wx = $Matches[1]
            $wy = $null
            $px = $null
            $py = $null
            if ($line -match 'worldY\s*=\s*(\d+)') { $wy = $Matches[1] }
            if ($line -match 'posX\s*=\s*(\d+)')   { $px = $Matches[1] }
            if ($line -match 'posY\s*=\s*(\d+)')   { $py = $Matches[1] }
            $pairList.Add("worldX=$wx worldY=$wy posX=$px posY=$py")
        }
    }

    $result.spawn_pairs = [string[]]@($pairList.ToArray())
    if ($pairList.Count -gt 0) {
        $first = $pairList[0]
        if ($first -match 'worldX=(\d+)') { $result.first_world_x = $Matches[1] }
        if ($first -match 'worldY=(\d+)') { $result.first_world_y = $Matches[1] }
        if ($first -match 'posX=(\d+)')   { $result.first_pos_x   = $Matches[1] }
        if ($first -match 'posY=(\d+)')   { $result.first_pos_y   = $Matches[1] }
    }
    return $result
}

# ---------------------------------------------------------------------------
# Inspect both roots
# ---------------------------------------------------------------------------

$candMapDir = Find-MapDataDir -ModRoot $CandidateModRoot -MapId $CandidateMapId
$refMapDir  = Find-MapDataDir -ModRoot $ReferenceModRoot -MapId $ReferenceMapId

Write-Output "Candidate map data dir: $(if ($candMapDir) { $candMapDir } else { '(not found)' })"
Write-Output "Reference map data dir: $(if ($refMapDir)  { $refMapDir  } else { '(not found)' })"

$candCells  = Get-CellCoordinates -MapDir $candMapDir
$refCells   = Get-CellCoordinates -MapDir $refMapDir
$candZoom   = Get-MapInfoZoom    -MapDir $candMapDir
$refZoom    = Get-MapInfoZoom    -MapDir $refMapDir
$candSpawn  = Get-SpawnCoordinates -MapDir $candMapDir
$refSpawn   = Get-SpawnCoordinates -MapDir $refMapDir

# Check if candidate spawn target cell exists as a lotheader file
$candSpawnCellExists = $false
if ($null -ne $candSpawn.first_world_x -and $null -ne $candSpawn.first_world_y) {
    $spawnCell = "$($candSpawn.first_world_x)_$($candSpawn.first_world_y).lotheader"
    $spawnCellPath = if ($candMapDir -ne '') { Join-Path $candMapDir $spawnCell } else { '' }
    $candSpawnCellExists = ($spawnCellPath -ne '' -and (Test-Path -LiteralPath $spawnCellPath))
}

# ---------------------------------------------------------------------------
# Build report
# ---------------------------------------------------------------------------

$report = [ordered]@{
    schema                          = 'pzmapforge.workshop-cell-coordinate-contract.v0.1'
    candidate_mod_root              = $CandidateModRoot
    reference_mod_root              = $ReferenceModRoot
    candidate_map_id                = $CandidateMapId
    reference_map_id                = $ReferenceMapId
    candidate_map_data_dir          = $candMapDir
    reference_map_data_dir          = $refMapDir
    candidate_cells                 = $candCells
    reference_cells                 = $refCells
    candidate_zoom                  = $candZoom
    reference_zoom                  = $refZoom
    candidate_spawn                 = $candSpawn
    reference_spawn                 = $refSpawn
    candidate_spawn_cell_exists     = $candSpawnCellExists
    public_playable_claim_allowed   = $false
    load_test_performed_by_script   = $false
    binary_writer_changed           = $false
}

$jsonPath = Join-Path $Output 'workshop-cell-coordinate-contract.json'
$mdPath   = Join-Path $Output 'workshop-cell-coordinate-contract.md'

$report | ConvertTo-Json -Depth 6 | Set-Content -Path $jsonPath -Encoding UTF8
Write-Output "JSON: $jsonPath"

# ---------------------------------------------------------------------------
# Markdown
# ---------------------------------------------------------------------------

$fence = '```'
$md = @"
# Workshop Cell Coordinate Contract

## Roots

| | Path |
|---|---|
| Candidate | $CandidateModRoot |
| Reference | $ReferenceModRoot |

## Cell counts

| | Candidate ($CandidateMapId) | Reference ($ReferenceMapId) |
|---|---|---|
| lotheader count | $($candCells.lotheader_count) | $($refCells.lotheader_count) |
| lotpack count | $($candCells.lotpack_count) | $($refCells.lotpack_count) |
| chunkdata count | $($candCells.chunkdata_count) | $($refCells.chunkdata_count) |

## Cell coordinate range

| | Candidate | Reference |
|---|---|---|
| min X | $($candCells.min_cell_x) | $($refCells.min_cell_x) |
| max X | $($candCells.max_cell_x) | $($refCells.max_cell_x) |
| min Y | $($candCells.min_cell_y) | $($refCells.min_cell_y) |
| max Y | $($candCells.max_cell_y) | $($refCells.max_cell_y) |

## map.info zoom

| | Candidate | Reference |
|---|---|---|
| zoomX | $($candZoom.zoom_x) | $($refZoom.zoom_x) |
| zoomY | $($candZoom.zoom_y) | $($refZoom.zoom_y) |
| zoomS | $($candZoom.zoom_s) | $($refZoom.zoom_s) |

## Spawn coordinates

| | Candidate | Reference |
|---|---|---|
| first worldX | $($candSpawn.first_world_x) | $($refSpawn.first_world_x) |
| first worldY | $($candSpawn.first_world_y) | $($refSpawn.first_world_y) |
| first posX | $($candSpawn.first_pos_x) | $($refSpawn.first_pos_x) |
| first posY | $($candSpawn.first_pos_y) | $($refSpawn.first_pos_y) |
| spawn cell exists | $candSpawnCellExists | N/A |

## Non-claims

${fence}text
public_playable_claim_allowed=false
load_test_performed_by_script=false
binary_writer_changed=false
${fence}
"@

Set-Content -Path $mdPath -Value $md -Encoding ASCII
Write-Output "MD:   $mdPath"
Write-Output ""
Write-Output "candidate_lotheader_count:   $($candCells.lotheader_count)"
Write-Output "reference_lotheader_count:   $($refCells.lotheader_count)"
Write-Output "candidate_zoom_x:            $($candZoom.zoom_x)"
Write-Output "candidate_spawn_worldX:      $($candSpawn.first_world_x)"
Write-Output "candidate_spawn_cell_exists: $candSpawnCellExists"
Write-Output "public_playable_claim_allowed=false"
Write-Output "Done."
