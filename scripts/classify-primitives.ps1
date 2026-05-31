#Requires -Version 5.1
<#
.SYNOPSIS
    Classifies semantic regions into higher-level map primitives.

    Reads .local/mapforge/regions.json and maps each region to a planning
    primitive type based on its semantic kind. Writes:
      .local/mapforge/primitives.json
      .local/mapforge/primitives-report.md

    If regions.json is missing, runs scripts/extract-regions.ps1 first.
    Outputs are local-only. Does not touch media/maps.
    Does not copy or reference Project Zomboid assets.
    Does not claim playable Project Zomboid export.
#>

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$scriptDir      = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot       = Split-Path -Parent $scriptDir
$outputDir      = Join-Path $repoRoot '.local\mapforge'
$regionsPath    = Join-Path $outputDir 'regions.json'
$primitivesJson = Join-Path $outputDir 'primitives.json'
$primitivesMd   = Join-Path $outputDir 'primitives-report.md'

# ---------------------------------------------------------------------------
# Ensure regions artifact exists
# ---------------------------------------------------------------------------

if (-not (Test-Path $regionsPath -PathType Leaf)) {
    Write-Output "regions.json not found. Running extract-regions.ps1..."
    & powershell -ExecutionPolicy Bypass -File (Join-Path $repoRoot 'scripts\extract-regions.ps1')
    if ($LASTEXITCODE -ne 0) { Write-Error "extract-regions.ps1 failed."; exit 1 }
}

if (-not (Test-Path $regionsPath -PathType Leaf)) {
    Write-Error "regions.json still missing after extraction attempt."; exit 1
}

Write-Output "Classifying primitives from: $regionsPath"

# ---------------------------------------------------------------------------
# Load regions
# ---------------------------------------------------------------------------

$r       = Get-Content $regionsPath -Raw | ConvertFrom-Json
$W       = [int]$r.width
$H       = [int]$r.height
$regions = @($r.regions)

# ---------------------------------------------------------------------------
# Kind -> primitive type and planning role mapping
# ---------------------------------------------------------------------------

$kindMap = @{
    'grass'           = [pscustomobject]@{ type = 'ground_region';     role = 'open ground or background area' }
    'road'            = [pscustomobject]@{ type = 'road_region';        role = 'driveable surface' }
    'sidewalk'        = [pscustomobject]@{ type = 'sidewalk_region';    role = 'pedestrian path' }
    'row_house'       = [pscustomobject]@{ type = 'building_footprint'; role = 'structure footprint' }
    'depanneur'       = [pscustomobject]@{ type = 'building_footprint'; role = 'structure footprint' }
    'garage'          = [pscustomobject]@{ type = 'building_footprint'; role = 'structure footprint' }
    'industrial_yard' = [pscustomobject]@{ type = 'yard_region';        role = 'open industrial or service yard' }
    'landmark'        = [pscustomobject]@{ type = 'landmark_marker';    role = 'navigation reference point' }
    'spawn'           = [pscustomobject]@{ type = 'spawn_marker';       role = 'player spawn location' }
}

# ---------------------------------------------------------------------------
# Build unsorted primitives (preserve sort fields as flat properties)
# ---------------------------------------------------------------------------

$unsorted = @()
foreach ($reg in $regions) {
    $kind = [string]$reg.kind
    if (-not $kindMap.ContainsKey($kind)) {
        Write-Error "Unknown kind '$kind' in regions.json. No primitive mapping defined."
        exit 1
    }
    $pm = $kindMap[$kind]
    $unsorted += [pscustomobject]@{
        s_type    = $pm.type
        s_px      = [int]$reg.pixel_count
        s_by      = [int]$reg.bounds.y
        s_bx      = [int]$reg.bounds.x
        s_src     = [int]$reg.region_id
        kind      = $kind
        code      = [string]$reg.code
        px        = [int]$reg.pixel_count
        bx        = [int]$reg.bounds.x
        by        = [int]$reg.bounds.y
        bw        = [int]$reg.bounds.width
        bh        = [int]$reg.bounds.height
        cx        = $reg.centroid.x
        cy        = $reg.centroid.y
        role      = $pm.role
    }
}

# ---------------------------------------------------------------------------
# Sort: primitive_type ASC, pixel_count DESC, bounds.y ASC, bounds.x ASC,
#       source_region_id ASC
# ---------------------------------------------------------------------------

$sorted = @($unsorted | Sort-Object `
    -Property s_type,
              @{ Expression = 's_px'; Descending = $true },
              s_by, s_bx, s_src)

# ---------------------------------------------------------------------------
# Build final primitives array
# Use @() += [pscustomobject] to avoid ConvertTo-Json {value:[...]} wrapping
# ---------------------------------------------------------------------------

$primitives = @()
for ($i = 0; $i -lt $sorted.Count; $i++) {
    $s = $sorted[$i]
    $primitives += [pscustomobject][ordered]@{
        primitive_id     = $i + 1
        primitive_type   = $s.s_type
        source_region_id = $s.s_src
        kind             = $s.kind
        code             = $s.code
        pixel_count      = $s.px
        bounds           = [pscustomobject][ordered]@{ x = $s.bx; y = $s.by; width = $s.bw; height = $s.bh }
        centroid         = [pscustomobject][ordered]@{ x = $s.cx; y = $s.cy }
        planning_role    = $s.role
    }
}

# ---------------------------------------------------------------------------
# Build summary_by_primitive_type
# ---------------------------------------------------------------------------

$typeAccum = @{}
foreach ($p in $primitives) {
    $t = [string]$p.primitive_type
    if (-not $typeAccum.ContainsKey($t)) {
        $typeAccum[$t] = [pscustomobject]@{
            primitive_type        = $t
            region_count          = 0
            total_pixels          = 0
            largest_region_pixels = 0
        }
    }
    $acc = $typeAccum[$t]
    $acc.region_count++
    $acc.total_pixels += [int]$p.pixel_count
    if ([int]$p.pixel_count -gt [int]$acc.largest_region_pixels) {
        $acc.largest_region_pixels = [int]$p.pixel_count
    }
}

$summaryList = @($typeAccum.Values | Sort-Object primitive_type)

# ---------------------------------------------------------------------------
# Write primitives.json
# ---------------------------------------------------------------------------

New-Item -ItemType Directory -Force -Path $outputDir | Out-Null

$output = [pscustomobject][ordered]@{
    schema                  = 'pzmapforge.primitives.v0.1'
    claim_boundary          = 'planning_artifact_only_not_pz_load_tested'
    source                  = '.local/mapforge/regions.json'
    width                   = $W
    height                  = $H
    primitive_count         = [int]$primitives.Count
    primitives              = $primitives
    summary_by_primitive_type = $summaryList
}

$output | ConvertTo-Json -Depth 10 | Set-Content -Path $primitivesJson -Encoding UTF8
Write-Output "Primitives JSON: $primitivesJson"

# ---------------------------------------------------------------------------
# Write primitives-report.md
# ---------------------------------------------------------------------------

$summaryLines = foreach ($s in $summaryList) {
    "| $($s.primitive_type) | $($s.region_count) | $($s.total_pixels) | $($s.largest_region_pixels) |"
}

$top20 = $primitives | Select-Object -First 20
$primLines = foreach ($p in $top20) {
    $b = $p.bounds; $c = $p.centroid
    "| $($p.primitive_id) | $($p.primitive_type) | $($p.kind) | $($p.pixel_count) | ($($b.x),$($b.y)) $($b.width)x$($b.height) |"
}

$md = @"
# Primitive Classification Report

Source: .local/mapforge/regions.json
Dimensions: ${W}x${H}
Total primitives: $($primitives.Count)

## Claim boundary

planning_artifact_only_not_pz_load_tested

## Summary by primitive type

| Primitive type | Count | Total pixels | Largest |
|---|---:|---:|---:|
$($summaryLines -join "`n")

## Top 20 primitives by sort order (type, pixel_count desc, y, x)

| ID | Primitive type | Kind | Pixels | Bounds (x,y WxH) |
|---|---|---|---:|---|
$($primLines -join "`n")

## Kind-to-primitive mapping

| Kind | Primitive type | Planning role |
|---|---|---|
| grass | ground_region | open ground or background area |
| road | road_region | driveable surface |
| sidewalk | sidewalk_region | pedestrian path |
| row_house | building_footprint | structure footprint |
| depanneur | building_footprint | structure footprint |
| garage | building_footprint | structure footprint |
| industrial_yard | yard_region | open industrial or service yard |
| landmark | landmark_marker | navigation reference point |
| spawn | spawn_marker | player spawn location |
"@

Set-Content -Path $primitivesMd -Value $md -Encoding UTF8
Write-Output "Primitives MD:   $primitivesMd"
Write-Output "Done. Planning artifact only; no PZ export claim."
