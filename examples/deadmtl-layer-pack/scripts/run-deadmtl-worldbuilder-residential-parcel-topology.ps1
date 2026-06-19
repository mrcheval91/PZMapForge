#Requires -Version 5.1
<#
.SYNOPSIS
MAP-28A: Run the DeadMTL residential parcel topology builder for map_00_component_0001.

Produces 11 output files under:
  .local\deadmtl-authoring\worldbuilder-residential-parcel-topology\map_00\

This is a sandbox-only planning artifact. NOT a playable Project Zomboid export.
NOT .lotpack / .lotheader / .lua / .bin. No runtime files written.
#>
Set-StrictMode -Version Latest

$ScriptDir   = $PSScriptRoot
$RepoRoot    = (Resolve-Path (Join-Path $ScriptDir "..\..\..\")).Path
$CliProject  = Join-Path $RepoRoot "src\PZMapForge.Cli\PZMapForge.Cli.csproj"
$OutputRoot  = Join-Path $RepoRoot ".local\deadmtl-authoring\worldbuilder-residential-parcel-topology\map_00"

New-Item -ItemType Directory -Path $OutputRoot -Force | Out-Null

$OutputJson      = Join-Path $OutputRoot "map_00.residential_parcel_topology.json"
$ParcelsCsv      = Join-Path $OutputRoot "map_00.residential_parcel_topology_parcels.csv"
$EdgesCsv        = Join-Path $OutputRoot "map_00.residential_parcel_topology_frontage_edges.csv"
$StripsCsv       = Join-Path $OutputRoot "map_00.residential_parcel_topology_sidewalk_strips.csv"
$ChecksCsv       = Join-Path $OutputRoot "map_00.residential_parcel_topology_checks.csv"
$Summary         = Join-Path $OutputRoot "map_00.residential_parcel_topology.summary.txt"
$Readme          = Join-Path $OutputRoot "README_MAP28A_RESIDENTIAL_PARCEL_TOPOLOGY.md"
$CleanPng        = Join-Path $OutputRoot "map_00_residential_parcels_topology_clean_native_256.png"
$DebugPng        = Join-Path $OutputRoot "map_00_residential_parcels_topology_debug_native_256.png"
$OverlayPng      = Join-Path $OutputRoot "map_00_residential_parcels_topology_overlay_native_256.png"
$Html            = Join-Path $OutputRoot "map_00_residential_parcels_topology_viewer.html"

$RawSourcePng    = "E:\Omni\Zomboid\assets\raw\map_00.png"

Write-Host "MAP-28A: Running residential parcel topology builder..."
Write-Host "  Output root: $OutputRoot"

$extraArgs = @()
if (Test-Path $RawSourcePng) {
    $extraArgs = @("--raw-source-png", $RawSourcePng)
    Write-Host "  Raw source PNG: $RawSourcePng (found)"
} else {
    Write-Host "  Raw source PNG: not found -- overlay will use dark background"
}

dotnet run --project $CliProject -- `
    "deadmtl-build-worldbuilder-residential-parcel-topology" `
    "--output-root"               $OutputRoot `
    "--output-json"               $OutputJson `
    "--output-parcels-csv"        $ParcelsCsv `
    "--output-frontage-edges-csv" $EdgesCsv `
    "--output-sidewalk-strips-csv" $StripsCsv `
    "--output-checks-csv"         $ChecksCsv `
    "--summary"                   $Summary `
    "--output-readme"             $Readme `
    "--output-clean-png"          $CleanPng `
    "--output-debug-png"          $DebugPng `
    "--output-overlay-png"        $OverlayPng `
    "--output-html"               $Html `
    @extraArgs

if ($LASTEXITCODE -eq 0) {
    Write-Host ""
    Write-Host "Output files:"
    $files = @($OutputJson, $ParcelsCsv, $EdgesCsv, $StripsCsv, $ChecksCsv,
               $Summary, $Readme, $CleanPng, $DebugPng, $OverlayPng, $Html)
    foreach ($f in $files) {
        if (Test-Path $f) {
            $size = (Get-Item $f).Length
            Write-Host "  OK  $(Split-Path $f -Leaf)  ($size bytes)"
        } else {
            Write-Host "  MISSING  $(Split-Path $f -Leaf)"
        }
    }
    Write-Host ""
    Write-Host "MAP-28A: PASS"
} else {
    Write-Host "MAP-28A: FAIL (exit code $LASTEXITCODE)"
}

exit $LASTEXITCODE
