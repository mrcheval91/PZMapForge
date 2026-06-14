param(
    [int]$OriginX = 10580,
    [int]$OriginY = 8200
)

$ErrorActionPreference = "Stop"

$repoRoot     = $PSScriptRoot | Split-Path -Parent | Split-Path -Parent | Split-Path -Parent
$SampleDir    = Join-Path $repoRoot ".local\deadmtl-authoring\system2-static-road-sample"
$ExtractDir   = Join-Path $repoRoot ".local\deadmtl-authoring\system2-static-road-sample-extract"
$CliProject   = Join-Path $repoRoot "src\PZMapForge.Cli\PZMapForge.Cli.csproj"
$SampleScript = Join-Path $PSScriptRoot "generate-system2-static-road-sample.ps1"

$Contract = Join-Path $SampleDir "system2-static-road-overlay-contract.json"
$Palette  = Join-Path $SampleDir "palettes\system2-static-road-intent-palette.json"
$PackRoot = $SampleDir

$OutputJson = Join-Path $ExtractDir "system2_static_road_sample_extract.json"
$SummaryTxt = Join-Path $ExtractDir "system2_static_road_sample_extract.summary.txt"

Write-Host "Generating System 2 road sample layers..."
& powershell -ExecutionPolicy Bypass -File $SampleScript -OutputDir $SampleDir
if ($LASTEXITCODE -ne 0) {
    Write-Error "Sample generator failed with exit code $LASTEXITCODE"
    exit 1
}

New-Item -ItemType Directory -Force -Path $ExtractDir | Out-Null

Write-Host "Building CLI..."
& dotnet build $CliProject --configuration Release -q
if ($LASTEXITCODE -ne 0) {
    Write-Error "Build failed."
    exit 1
}

Write-Host "Running system2-extract-static-roads..."
& dotnet run --project $CliProject --configuration Release --no-build -- `
    system2-extract-static-roads `
    --input    $Contract `
    --palette  $Palette `
    --root     $PackRoot `
    --output   $OutputJson `
    --summary  $SummaryTxt `
    --origin-x $OriginX `
    --origin-y $OriginY

if ($LASTEXITCODE -ne 0) {
    Write-Error "system2-extract-static-roads failed with exit code $LASTEXITCODE"
    exit 1
}

Write-Host ""
Write-Host "Output:  $OutputJson"
Write-Host "Summary: $SummaryTxt"
Write-Host "VERDICT: MAP22F_SYSTEM2_STATIC_ROAD_SAMPLE_EXTRACT_COMPLETE"
