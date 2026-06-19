#Requires -Version 5.1
<#
.SYNOPSIS
MAP-28B: Run the DeadMTL residential building footprint planner for map_00_component_0001.

Produces 10 output files under:
  .local\deadmtl-authoring\worldbuilder-residential-building-footprint-plan\map_00\

This is a sandbox-only planning artifact. NOT a playable Project Zomboid export.
NOT .lotpack / .lotheader / .lua / .bin. No runtime files written.
#>
Set-StrictMode -Version Latest

$ScriptDir   = $PSScriptRoot
$RepoRoot    = (Resolve-Path (Join-Path $ScriptDir "..\..\..\")).Path
$CliProject  = Join-Path $RepoRoot "src\PZMapForge.Cli\PZMapForge.Cli.csproj"
$OutputRoot  = Join-Path $RepoRoot ".local\deadmtl-authoring\worldbuilder-residential-building-footprint-plan\map_00"

New-Item -ItemType Directory -Path $OutputRoot -Force | Out-Null

$OutputJson      = Join-Path $OutputRoot "map_00.residential_building_footprint_plan.json"
$FootprintsCsv   = Join-Path $OutputRoot "map_00.residential_building_footprint_plan_footprints.csv"
$ChecksCsv       = Join-Path $OutputRoot "map_00.residential_building_footprint_plan_checks.csv"
$Summary         = Join-Path $OutputRoot "map_00.residential_building_footprint_plan.summary.txt"
$Readme          = Join-Path $OutputRoot "README_MAP28B_RESIDENTIAL_BUILDING_FOOTPRINT_PLAN.md"
$CleanPng        = Join-Path $OutputRoot "map_00_residential_building_footprints_clean_native_256.png"
$DebugPng        = Join-Path $OutputRoot "map_00_residential_building_footprints_debug_native_256.png"
$OverlayPng      = Join-Path $OutputRoot "map_00_residential_building_footprints_overlay_native_256.png"
$Html            = Join-Path $OutputRoot "map_00_residential_building_footprints_viewer.html"
$ParentManifest  = Join-Path $OutputRoot "map_00.residential_building_footprint_plan_parent_manifest.json"

$RawSourcePng    = "E:\Omni\Zomboid\assets\raw\map_00.png"

Write-Host "MAP-28B: Running residential building footprint planner..."
Write-Host "  Output root: $OutputRoot"

$extraArgs = @()
if (Test-Path $RawSourcePng) {
    $extraArgs = @("--raw-source-png", $RawSourcePng)
    Write-Host "  Raw source PNG: $RawSourcePng (found)"
} else {
    Write-Host "  Raw source PNG: not found -- overlay will use dark background"
}

dotnet run --project $CliProject -- `
    "deadmtl-build-worldbuilder-residential-building-footprint-plan" `
    "--output-root"            $OutputRoot `
    "--output-json"            $OutputJson `
    "--output-footprints-csv"  $FootprintsCsv `
    "--output-checks-csv"      $ChecksCsv `
    "--summary"                $Summary `
    "--output-readme"          $Readme `
    "--output-clean-png"       $CleanPng `
    "--output-debug-png"       $DebugPng `
    "--output-overlay-png"     $OverlayPng `
    "--output-html"            $Html `
    "--output-parent-manifest" $ParentManifest `
    @extraArgs

if ($LASTEXITCODE -eq 0) {
    Write-Host ""
    Write-Host "Output files:"
    $files = @($OutputJson, $FootprintsCsv, $ChecksCsv, $Summary, $Readme,
               $CleanPng, $DebugPng, $OverlayPng, $Html, $ParentManifest)
    foreach ($f in $files) {
        if (Test-Path $f) {
            $size = (Get-Item $f).Length
            Write-Host "  OK  $(Split-Path $f -Leaf)  ($size bytes)"
        } else {
            Write-Host "  MISSING  $(Split-Path $f -Leaf)"
        }
    }
    Write-Host ""
    Write-Host "MAP-28B: PASS"
} else {
    Write-Host "MAP-28B: FAIL (exit code $LASTEXITCODE)"
}

exit $LASTEXITCODE
