Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$repoRoot = $PSScriptRoot | Split-Path -Parent | Split-Path -Parent | Split-Path -Parent

$pngPath        = "E:\Omni\Zomboid\assets\raw\map_00.png"
$geometryMvp    = Join-Path $repoRoot ".local\deadmtl-authoring\worldbuilder-minimal-concrete-geometry-mvp\map_00\map_00.minimal_concrete_geometry_mvp.json"

$outDir = Join-Path $repoRoot ".local\deadmtl-authoring\worldbuilder-minimal-concrete-geometry-qa-overlay\map_00"
New-Item -ItemType Directory -Force -Path $outDir | Out-Null

$outPng     = Join-Path $outDir "map_00.minimal_concrete_geometry_qa_overlay.png"
$outJson    = Join-Path $outDir "map_00.minimal_concrete_geometry_qa_overlay.json"
$outMd      = Join-Path $outDir "map_00.minimal_concrete_geometry_qa_overlay.md"
$outCsv     = Join-Path $outDir "map_00.minimal_concrete_geometry_qa_overlay.csv"
$outSummary = Join-Path $outDir "map_00.minimal_concrete_geometry_qa_overlay.summary.txt"

$cliProject = Join-Path $repoRoot "src\PZMapForge.Cli\PZMapForge.Cli.csproj"

dotnet run --project $cliProject -- `
    deadmtl-build-worldbuilder-minimal-concrete-geometry-qa-overlay `
    --png            $pngPath `
    --geometry-mvp   $geometryMvp `
    --output-png     $outPng `
    --output-json    $outJson `
    --output-md      $outMd `
    --output-csv     $outCsv `
    --summary        $outSummary

if ($LASTEXITCODE -ne 0) {
    Write-Error "deadmtl-build-worldbuilder-minimal-concrete-geometry-qa-overlay exited $LASTEXITCODE"
    exit $LASTEXITCODE
}

Write-Host "Output written to: $outDir"

foreach ($f in @($outPng, $outJson, $outMd, $outCsv, $outSummary)) {
    if (Test-Path $f) {
        Write-Host "EXISTS: $f"
    } else {
        Write-Error "MISSING: $f"
        exit 1
    }
}

Get-Content $outSummary
