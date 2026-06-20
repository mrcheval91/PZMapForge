#Requires -Version 5.1
<#
.SYNOPSIS
MAP-29A: Detect residential blue quadrilateral shapes in map_00.png and replace
with calculated beige lot fills. Produces JSON/CSV proof and a replacement PNG.

Outputs under:
  .local\deadmtl-authoring\worldbuilder-residential-blue-quadrilateral-lot-fill\map_00\

This is a sandbox-only planning artifact. NOT a playable Project Zomboid export.
NOT .lotpack / .lotheader / .lua / .bin. Source PNG is never mutated.
#>
Set-StrictMode -Version Latest

$ScriptDir  = $PSScriptRoot
$RepoRoot   = (Resolve-Path (Join-Path $ScriptDir "..\..\..\")).Path
$CliProject = Join-Path $RepoRoot "src\PZMapForge.Cli\PZMapForge.Cli.csproj"
$OutputRoot = Join-Path $RepoRoot ".local\deadmtl-authoring\worldbuilder-residential-blue-quadrilateral-lot-fill\map_00"

New-Item -ItemType Directory -Path $OutputRoot -Force | Out-Null

$SourcePng  = "E:\Omni\Zomboid\assets\raw\map_00.png"
$OutputJson = Join-Path $OutputRoot "map_00.blue_lot_fill.json"
$LotsCsv    = Join-Path $OutputRoot "map_00.blue_lot_fill_lots.csv"
$FacCsv     = Join-Path $OutputRoot "map_00.blue_lot_fill_facades.csv"
$ChkCsv     = Join-Path $OutputRoot "map_00.blue_lot_fill_checks.csv"
$OutputPng  = Join-Path $OutputRoot "map_00_residential_blue_lot_fill_output_native_256.png"
$Html       = Join-Path $OutputRoot "map_00_blue_lot_fill_viewer.html"
$Summary    = Join-Path $OutputRoot "map_00.blue_lot_fill.summary.txt"

if (-not (Test-Path $SourcePng)) {
    Write-Error "Source PNG not found: $SourcePng"
    exit 1
}

Write-Host "MAP-29A: Running residential blue quadrilateral lot fill..."
Write-Host "  Source PNG : $SourcePng"
Write-Host "  Output root: $OutputRoot"

dotnet run --project $CliProject -- `
    "deadmtl-build-worldbuilder-residential-blue-quadrilateral-lot-fill" `
    "--source-png"          $SourcePng `
    "--output-root"         $OutputRoot `
    "--output-json"         $OutputJson `
    "--output-lots-csv"     $LotsCsv `
    "--output-facades-csv"  $FacCsv `
    "--output-checks-csv"   $ChkCsv `
    "--output-png"          $OutputPng `
    "--output-html"         $Html `
    "--summary"             $Summary

if ($LASTEXITCODE -eq 0) {
    Write-Host ""
    Write-Host "Output files:"
    $files = @($OutputJson, $LotsCsv, $FacCsv, $ChkCsv, $OutputPng, $Html, $Summary)
    foreach ($f in $files) {
        if (Test-Path $f) {
            $size = (Get-Item $f).Length
            Write-Host "  OK  $(Split-Path $f -Leaf)  ($size bytes)"
        } else {
            Write-Host "  MISSING  $(Split-Path $f -Leaf)"
        }
    }
    Write-Host ""
    Write-Host "MAP-29A: PASS"
} else {
    Write-Host "MAP-29A: FAIL (exit code $LASTEXITCODE)"
}

exit $LASTEXITCODE
