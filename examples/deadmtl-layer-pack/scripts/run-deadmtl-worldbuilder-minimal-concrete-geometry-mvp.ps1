Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$repoRoot = $PSScriptRoot | Split-Path -Parent | Split-Path -Parent | Split-Path -Parent

$pngPath               = "E:\Omni\Zomboid\assets\raw\map_00.png"
$connectedComponents   = Join-Path $repoRoot ".local\deadmtl-authoring\worldbuilder-connected-component-extraction\map_00\map_00.connected_component_extraction.json"
$accessProfile         = Join-Path $repoRoot ".local\deadmtl-authoring\worldbuilder-component-access-profile\map_00\map_00.component_access_profile.json"
$targetComponentOrder  = 1

$outDir = Join-Path $repoRoot ".local\deadmtl-authoring\worldbuilder-minimal-concrete-geometry-mvp\map_00"
New-Item -ItemType Directory -Force -Path $outDir | Out-Null

$outJson    = Join-Path $outDir "map_00.minimal_concrete_geometry_mvp.json"
$outMd      = Join-Path $outDir "map_00.minimal_concrete_geometry_mvp.md"
$outCsv     = Join-Path $outDir "map_00.minimal_concrete_geometry_mvp.csv"
$outSummary = Join-Path $outDir "map_00.minimal_concrete_geometry_mvp.summary.txt"

$cliProject = Join-Path $repoRoot "src\PZMapForge.Cli\PZMapForge.Cli.csproj"

dotnet run --project $cliProject --configuration Release --no-build -- `
    deadmtl-build-worldbuilder-minimal-concrete-geometry-mvp `
    --png                    $pngPath `
    --connected-components   $connectedComponents `
    --access-profile         $accessProfile `
    --target-component-order $targetComponentOrder `
    --output-json            $outJson `
    --output-md              $outMd `
    --output-csv             $outCsv `
    --summary                $outSummary

if ($LASTEXITCODE -ne 0) {
    Write-Error "deadmtl-build-worldbuilder-minimal-concrete-geometry-mvp exited $LASTEXITCODE"
    exit $LASTEXITCODE
}

Write-Host "Output written to: $outDir"
