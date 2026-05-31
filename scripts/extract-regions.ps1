#Requires -Version 5.1
<#
.SYNOPSIS
    Semantic region extraction for PZMapForge.

    Reads .local/mapforge/parsed-cell.json, runs a BFS flood-fill with
    4-neighbor connectivity to identify contiguous regions by semantic kind,
    and writes:
      .local/mapforge/regions.json
      .local/mapforge/regions-report.md

    If parsed-cell.json is missing, runs scripts/validate.ps1 first.
    Outputs are local-only. Does not touch media/maps.
    Does not claim playable Project Zomboid export.
#>

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$scriptDir   = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot    = Split-Path -Parent $scriptDir
$outputDir   = Join-Path $repoRoot '.local\mapforge'
$jsonPath    = Join-Path $outputDir 'parsed-cell.json'
$regionsJson = Join-Path $outputDir 'regions.json'
$regionsMd   = Join-Path $outputDir 'regions-report.md'

# ---------------------------------------------------------------------------
# Ensure source artifact exists
# ---------------------------------------------------------------------------

if (-not (Test-Path $jsonPath -PathType Leaf)) {
    Write-Output "parsed-cell.json not found. Running validate.ps1 first..."
    & powershell -ExecutionPolicy Bypass -File (Join-Path $repoRoot 'scripts\validate.ps1')
    if ($LASTEXITCODE -ne 0) { Write-Error "validate.ps1 failed."; exit 1 }
}

Write-Output "Region extraction from: $jsonPath"

$a    = Get-Content $jsonPath -Raw | ConvertFrom-Json
$W    = [int]$a.width
$H    = [int]$a.height
$rows = @($a.rows)

# Build code-to-kind map from legend
$codeToKind = @{}
foreach ($entry in $a.legend) {
    $codeToKind[[string]$entry.code] = [string]$entry.kind
}

# ---------------------------------------------------------------------------
# Build flat code grid from rows
# ---------------------------------------------------------------------------

$codeGrid = New-Object char[] ($W * $H)
for ($gy = 0; $gy -lt $H; $gy++) {
    $row = [string]$rows[$gy]
    for ($gx = 0; $gx -lt $W; $gx++) {
        $codeGrid[$gy * $W + $gx] = $row[$gx]
    }
}

# ---------------------------------------------------------------------------
# BFS flood-fill — 4-neighbor connectivity
#
# NOTE: [int]($cur / $W) is WRONG in PS5.1 because / produces a Double and
# [int] rounds (not truncates). Use modulo decomposition instead:
#   $cx = $cur % $W        (always exact)
#   $cy = ($cur - $cx) / $W  (exact because $cur-$cx is divisible by $W)
# ---------------------------------------------------------------------------

$visited  = New-Object bool[] ($W * $H)
$bfsQ     = New-Object int[]  ($W * $H)   # pre-allocated queue
$dys      = @(-1, 1, 0, 0)
$dxs      = @(0, 0, -1, 1)

$unsorted = [System.Collections.ArrayList]::new()
$tempId   = 0

for ($sy = 0; $sy -lt $H; $sy++) {
    for ($sx = 0; $sx -lt $W; $sx++) {
        $si = $sy * $W + $sx
        if ($visited[$si]) { continue }

        $code = $codeGrid[$si]
        $visited[$si] = $true
        $tempId++

        $qHead = 0; $qTail = 0
        $bfsQ[$qTail] = $si; $qTail++

        $count = 0
        $minX = $W; $maxX = -1; $minY = $H; $maxY = -1
        $sumX = [long]0; $sumY = [long]0

        while ($qHead -lt $qTail) {
            $cur = $bfsQ[$qHead]; $qHead++

            # Modulo decomposition — avoids PS5.1 [int] rounding on float division
            $cx = $cur % $W
            $cy = ($cur - $cx) / $W

            $count++
            if ($cx -lt $minX) { $minX = $cx }
            if ($cx -gt $maxX) { $maxX = $cx }
            if ($cy -lt $minY) { $minY = $cy }
            if ($cy -gt $maxY) { $maxY = $cy }
            $sumX += $cx
            $sumY += $cy

            for ($d = 0; $d -lt 4; $d++) {
                $ny = $cy + $dys[$d]
                $nx = $cx + $dxs[$d]
                if ($ny -lt 0 -or $ny -ge $H -or $nx -lt 0 -or $nx -ge $W) { continue }
                $ni = $ny * $W + $nx
                if ($visited[$ni] -or $codeGrid[$ni] -ne $code) { continue }
                $visited[$ni] = $true
                $bfsQ[$qTail] = $ni; $qTail++
            }
        }

        $centX = [Math]::Round([double]$sumX / $count, 2)
        $centY = [Math]::Round([double]$sumY / $count, 2)

        $null = $unsorted.Add([pscustomobject]@{
            temp_id     = $tempId
            kind        = $codeToKind[[string]$code]
            code        = [string]$code
            pixel_count = $count
            bx          = $minX
            by          = $minY
            bw          = $maxX - $minX + 1
            bh          = $maxY - $minY + 1
            cx          = $centX
            cy          = $centY
        })
    }
}

Write-Output "  Found $($unsorted.Count) regions."

# ---------------------------------------------------------------------------
# Sort: kind ASC, pixel_count DESC, bounds.y ASC, bounds.x ASC, temp_id ASC
# ---------------------------------------------------------------------------

$sorted = @($unsorted | Sort-Object `
    -Property kind,
              @{ Expression = 'pixel_count'; Descending = $true },
              by,
              bx,
              temp_id)

# ---------------------------------------------------------------------------
# Assign sequential region_ids and build final output objects
# ---------------------------------------------------------------------------

$finalRegions = @()
for ($i = 0; $i -lt $sorted.Count; $i++) {
    $sr = $sorted[$i]
    $finalRegions += [pscustomobject][ordered]@{
        region_id   = $i + 1
        kind        = $sr.kind
        code        = $sr.code
        pixel_count = [int]$sr.pixel_count
        bounds      = [pscustomobject][ordered]@{ x = [int]$sr.bx; y = [int]$sr.by; width = [int]$sr.bw; height = [int]$sr.bh }
        centroid    = [pscustomobject][ordered]@{ x = $sr.cx; y = $sr.cy }
    }
}

# ---------------------------------------------------------------------------
# Build summary_by_kind
# ---------------------------------------------------------------------------

$kindMap = @{}
foreach ($r in $finalRegions) {
    $k = [string]$r.kind
    if (-not $kindMap.ContainsKey($k)) {
        $kindMap[$k] = [ordered]@{
            kind                  = $k
            code                  = [string]$r.code
            region_count          = 0
            total_pixels          = 0
            largest_region_pixels = 0
        }
    }
    $km = $kindMap[$k]
    $km.region_count++
    $km.total_pixels += [int]$r.pixel_count
    if ([int]$r.pixel_count -gt [int]$km.largest_region_pixels) {
        $km.largest_region_pixels = [int]$r.pixel_count
    }
}

$summaryList = @($kindMap.Values | Sort-Object kind)

# ---------------------------------------------------------------------------
# Write regions.json
# ---------------------------------------------------------------------------

New-Item -ItemType Directory -Force -Path $outputDir | Out-Null

$output = [ordered]@{
    schema          = 'pzmapforge.regions.v0.1'
    claim_boundary  = 'planning_artifact_only_not_pz_load_tested'
    source          = '.local/mapforge/parsed-cell.json'
    width           = $W
    height          = $H
    total_regions   = [int]$finalRegions.Count
    regions         = $finalRegions
    summary_by_kind = $summaryList
}

$output | ConvertTo-Json -Depth 10 | Set-Content -Path $regionsJson -Encoding UTF8
Write-Output "Regions JSON: $regionsJson"

# ---------------------------------------------------------------------------
# Write regions-report.md
# ---------------------------------------------------------------------------

$summaryLines = foreach ($s in $summaryList) {
    "| $($s.kind) | $($s.code) | $($s.region_count) | $($s.total_pixels) | $($s.largest_region_pixels) |"
}

$top20 = $finalRegions | Select-Object -First 20
$regionLines = foreach ($r in $top20) {
    $b = $r.bounds; $c = $r.centroid
    "| $($r.region_id) | $($r.kind) | $($r.code) | $($r.pixel_count) | ($($b.x),$($b.y)) $($b.width)x$($b.height) | ($($c.x),$($c.y)) |"
}

$md = @"
# Region Extraction Report

Source: .local/mapforge/parsed-cell.json
Dimensions: ${W}x${H}
Total regions: $($finalRegions.Count)

## Claim boundary

planning_artifact_only_not_pz_load_tested

## Summary by kind

| Kind | Code | Regions | Total pixels | Largest region |
|---|---:|---:|---:|---:|
$($summaryLines -join "`n")

## Top 20 regions by sort order (kind, pixel_count desc, y, x)

| ID | Kind | Code | Pixels | Bounds (x,y WxH) | Centroid |
|---|---|---:|---:|---|---|
$($regionLines -join "`n")
"@

Set-Content -Path $regionsMd -Value $md -Encoding UTF8
Write-Output "Regions MD:   $regionsMd"
Write-Output "Done. Planning artifact only; no PZ export claim."
