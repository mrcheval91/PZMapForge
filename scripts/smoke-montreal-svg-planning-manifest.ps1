#Requires -Version 5.1
<#
.SYNOPSIS
    Local-only smoke script for the Montreal SVG planning manifest chain.

    Runs two app-export passes:
      1. Source run: SVG structure analysis, candidate inventory, selection template.
      2. Review run: SVG selection review + planning manifest.

    All output is written under .local/ (gitignored).
    Requires machine-local files that are NOT committed to the repo:
      - E:\Omni\Zomboid\assets\arrondissements-quartiers-montreal-200802.svg
      - E:\Omni\Zomboid\scratch\worlded-canal-garage-cell\source\canal-garage-cell-analysis-clean-v3.png

    Claim boundary: planning_artifact_only_not_pz_load_tested
    No SVG geometry converted. No coordinates extracted. No PZ assets. No media/maps writes.
#>

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$scriptDir   = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot    = Split-Path -Parent $scriptDir
$palettePath = Join-Path $repoRoot 'source\image-palette.json'

$svgPath     = 'E:\Omni\Zomboid\assets\arrondissements-quartiers-montreal-200802.svg'
$analysisPng = 'E:\Omni\Zomboid\scratch\worlded-canal-garage-cell\source\canal-garage-cell-analysis-clean-v3.png'

$sourceRunName  = 'mtl-svg-selection-source-smoke'
$reviewRunName  = 'mtl-svg-planning-manifest-smoke'
$sourceRunDir   = Join-Path $repoRoot ".local\app\$sourceRunName"
$reviewRunDir   = Join-Path $repoRoot ".local\app\$reviewRunName"
$artifactsDir   = Join-Path $sourceRunDir 'artifacts'
$selectionPath  = Join-Path $artifactsDir 'svg-layer-selection.smoke.json'

Write-Output 'smoke-montreal-svg-planning-manifest.ps1'
Write-Output ''

# ---------------------------------------------------------------------------
# Step 1: verify required files
# ---------------------------------------------------------------------------

Write-Output '--- Required files ---'

if (-not (Test-Path -LiteralPath $svgPath)) {
    Write-Error "Required Montreal SVG not found: $svgPath"
    exit 1
}
Write-Output "OK: SVG       $svgPath"

if (-not (Test-Path -LiteralPath $analysisPng)) {
    Write-Error "Required analysis PNG not found: $analysisPng"
    exit 1
}
Write-Output "OK: Analysis  $analysisPng"

if (-not (Test-Path -LiteralPath $palettePath)) {
    Write-Error "Palette not found: $palettePath"
    exit 1
}
Write-Output "OK: Palette   $palettePath"

# ---------------------------------------------------------------------------
# Step 2: source run (SVG structure + candidates + selection template)
# ---------------------------------------------------------------------------

Write-Output ''
Write-Output '--- Source run: SVG structure + candidates ---'
Write-Output "    Output: $sourceRunDir"

& dotnet run --project (Join-Path $repoRoot 'src\PZMapForge.Cli') `
    --configuration Release --no-build `
    -- app-export `
    --path       $analysisPng `
    --palette    $palettePath `
    --annotation $svgPath `
    --output     (Join-Path $repoRoot '.local\app') `
    --run-name   $sourceRunName

if ($LASTEXITCODE -ne 0) { throw 'Source run (app-export) failed.' }

if (-not (Test-Path -LiteralPath (Join-Path $sourceRunDir 'index.html'))) {
    throw 'Source run did not write index.html'
}
Write-Output 'OK: source run index.html written'

# ---------------------------------------------------------------------------
# Step 3: write selection JSON (9 operator-selected items)
# ---------------------------------------------------------------------------

Write-Output ''
Write-Output '--- Selection JSON: 9 items ---'

$selectionJson = @'
{
  "schema": "pzmapforge.svg-layer-selection-template.v0.1",
  "claim_boundary": "planning_artifact_only_not_pz_load_tested",
  "source_file_name": "arrondissements-quartiers-montreal-200802.svg",
  "selection_status": "operator_review_required",
  "generated_from": "svg-layer-candidates.json",
  "candidate_generation_method": "metadata_name_pattern_only",
  "parsed_as_geometry": false,
  "converted_to_map_geometry": false,
  "pz_assets_copied": false,
  "media_maps_touched": false,
  "playable_export_claimed": false,
  "water_candidates": [
    { "value": "Eaux",              "selected": true, "intended_use": "water_reference",        "operator_note": "St. Lawrence / river outline" }
  ],
  "outline_candidates": [
    { "value": "Outline_MTL",       "selected": true, "intended_use": "city_boundary_reference", "operator_note": "outer city limit" }
  ],
  "borough_or_district_candidates": [
    { "value": "SudOuest",          "selected": true, "intended_use": "borough_boundary",        "operator_note": "" },
    { "value": "VilleMarie",        "selected": true, "intended_use": "borough_boundary",        "operator_note": "" },
    { "value": "Plateau",           "selected": true, "intended_use": "borough_boundary",        "operator_note": "" },
    { "value": "NDG_CDN",           "selected": true, "intended_use": "borough_boundary",        "operator_note": "" }
  ],
  "transit_or_station_candidates": [
    { "value": "ANGRIGNON",         "selected": true, "intended_use": "transit_landmark",        "operator_note": "metro station" }
  ],
  "park_or_green_space_candidates": [
    { "value": "Pte-Angus",         "selected": true, "intended_use": "park_reference",          "operator_note": "Pointe-Saint-Charles greenway" },
    { "value": "Cap-Saint-Jacques", "selected": true, "intended_use": "park_reference",          "operator_note": "regional park" }
  ]
}
'@

New-Item -ItemType Directory -Force -Path $artifactsDir | Out-Null
Set-Content -LiteralPath $selectionPath -Value $selectionJson -Encoding UTF8
Write-Output "OK: selection JSON written: $selectionPath"

# ---------------------------------------------------------------------------
# Step 4: review run (selection review + planning manifest)
# ---------------------------------------------------------------------------

Write-Output ''
Write-Output '--- Review run: planning manifest ---'
Write-Output "    Output: $reviewRunDir"

& dotnet run --project (Join-Path $repoRoot 'src\PZMapForge.Cli') `
    --configuration Release --no-build `
    -- app-export `
    --path          $analysisPng `
    --palette       $palettePath `
    --svg-selection $selectionPath `
    --output        (Join-Path $repoRoot '.local\app') `
    --run-name      $reviewRunName

if ($LASTEXITCODE -ne 0) { throw 'Review run (app-export) failed.' }

# ---------------------------------------------------------------------------
# Step 5: verify outputs
# ---------------------------------------------------------------------------

Write-Output ''
Write-Output '--- Verification ---'

$filesToCheck = @(
    @{ Path = Join-Path $reviewRunDir 'index.html';                                Label = 'index.html' },
    @{ Path = Join-Path $reviewRunDir 'artifacts\svg-layer-selection-review.json'; Label = 'svg-layer-selection-review.json' },
    @{ Path = Join-Path $reviewRunDir 'artifacts\svg-planning-manifest.json';      Label = 'svg-planning-manifest.json' },
    @{ Path = Join-Path $reviewRunDir 'artifacts\svg-planning-manifest.md';        Label = 'svg-planning-manifest.md' }
)

foreach ($c in $filesToCheck) {
    if (-not (Test-Path -LiteralPath $c.Path)) {
        throw "Missing expected output: $($c.Label)"
    }
    Write-Output "OK: $($c.Label)"
}

$manifestJson = Get-Content -LiteralPath (Join-Path $reviewRunDir 'artifacts\svg-planning-manifest.json') -Raw |
    ConvertFrom-Json

if ([int]$manifestJson.selected_count -ne 9) {
    throw "selected_count mismatch: expected 9, got $($manifestJson.selected_count)"
}
Write-Output "OK: selected_count = $($manifestJson.selected_count)"

if ($manifestJson.planning_status -ne 'operator_selected_metadata_only') {
    throw "planning_status mismatch: got $($manifestJson.planning_status)"
}
Write-Output "OK: planning_status = $($manifestJson.planning_status)"

if ($manifestJson.exported_to_project_zomboid -ne $false) {
    throw 'exported_to_project_zomboid must be false'
}
Write-Output 'OK: exported_to_project_zomboid = false'

if ($manifestJson.converted_to_map_geometry -ne $false) {
    throw 'converted_to_map_geometry must be false'
}
Write-Output 'OK: converted_to_map_geometry = false'

$manifestMd = Get-Content -LiteralPath (Join-Path $reviewRunDir 'artifacts\svg-planning-manifest.md') -Raw
if ($manifestMd -notmatch 'No SVG geometry converted') {
    throw "svg-planning-manifest.md does not contain 'No SVG geometry converted'"
}
Write-Output "OK: manifest markdown contains 'No SVG geometry converted'"

$html = Get-Content -LiteralPath (Join-Path $reviewRunDir 'index.html') -Raw
if ($html -notmatch 'SVG Planning Manifest') {
    throw "index.html does not contain 'SVG Planning Manifest'"
}
Write-Output "OK: HTML contains 'SVG Planning Manifest'"

# APP-8: cockpit strings in source run HTML (SVG annotation path)
$sourceHtml = Get-Content -LiteralPath (Join-Path $sourceRunDir 'index.html') -Raw

$sourceCockpitChecks = @(
    'Run Summary',
    'SVG annotation: present',
    'SVG parse: parsed',
    'SVG candidates: present',
    'playable export generated: false',
    'PZ assets copied/read: false',
    'media/maps touched: false',
    'claim_boundary: intact'
)

foreach ($needle in $sourceCockpitChecks) {
    if ($sourceHtml -notlike "*$needle*") {
        throw "source run index.html does not contain cockpit string: $needle"
    }
    Write-Output "OK (source): $needle"
}

# APP-8: cockpit strings in review run HTML (selection/manifest path)
$reviewCockpitChecks = @(
    'Run Summary',
    'SVG review: present',
    'Planning manifest: present',
    'playable export generated: false',
    'PZ assets copied/read: false',
    'media/maps touched: false',
    'claim_boundary: intact'
)

foreach ($needle in $reviewCockpitChecks) {
    if ($html -notlike "*$needle*") {
        throw "review run index.html does not contain cockpit string: $needle"
    }
    Write-Output "OK (review): $needle"
}

# ---------------------------------------------------------------------------
# PASS
# ---------------------------------------------------------------------------

Write-Output ''
Write-Output '========================================'
Write-Output 'PASS: Montreal SVG planning manifest smoke'
Write-Output '========================================'
Write-Output ''
Write-Output "  Source run : $sourceRunDir"
Write-Output "  Review run : $reviewRunDir"
Write-Output "  Selected   : $($manifestJson.selected_count) items"
Write-Output "  Status     : $($manifestJson.planning_status)"
Write-Output ''
Write-Output '  Claim boundary: planning_artifact_only_not_pz_load_tested'
Write-Output '  No SVG geometry converted.'
Write-Output '  No coordinates extracted.'
Write-Output '  No PZ assets copied or read.'
Write-Output '  No media/maps writes.'
