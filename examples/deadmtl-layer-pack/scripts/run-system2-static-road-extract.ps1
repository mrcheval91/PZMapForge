param(
    [int]$OriginX = 10580,
    [int]$OriginY = 8200
)

$ErrorActionPreference = "Stop"

$repoRoot        = $PSScriptRoot | Split-Path -Parent | Split-Path -Parent | Split-Path -Parent
$PlaceholderDir  = Join-Path $repoRoot ".local\deadmtl-authoring\system2-static-road-placeholders"
$ExtractDir      = Join-Path $repoRoot ".local\deadmtl-authoring\system2-static-road-extract"
$CliProject      = Join-Path $repoRoot "src\PZMapForge.Cli\PZMapForge.Cli.csproj"
$PlaceholderScript = Join-Path $PSScriptRoot "generate-system2-static-road-placeholders.ps1"

$Contract = Join-Path $PlaceholderDir "system2-static-road-overlay-contract.json"
$Palette  = Join-Path $PlaceholderDir "palettes\system2-static-road-intent-palette.json"
$PackRoot = $PlaceholderDir

$OutputJson  = Join-Path $ExtractDir "system2_static_road_extract.json"
$SummaryTxt  = Join-Path $ExtractDir "system2_static_road_extract.summary.txt"

Write-Host "Generating System 2 placeholder layers..."
& powershell -ExecutionPolicy Bypass -File $PlaceholderScript -OutputDir $PlaceholderDir
if ($LASTEXITCODE -ne 0) {
    Write-Error "Placeholder generator failed with exit code $LASTEXITCODE"
    exit 1
}

New-Item -ItemType Directory -Force -Path $ExtractDir | Out-Null

Write-Host "Building CLI..."
$buildAttempt = 0
$buildOk = $false
while (-not $buildOk -and $buildAttempt -lt 3) {
    $buildAttempt++
    & dotnet build $CliProject --configuration Release -q /nodeReuse:false
    if ($LASTEXITCODE -eq 0) {
        $buildOk = $true
    } elseif ($buildAttempt -lt 3) {
        # MSB3492 AssemblyInfoInputs.cache read race: a prior `dotnet build`
        # invocation's background compiler-server process can still hold the
        # file handle for a brief window after that process returns. This is
        # a known transient MSBuild race, not a real compile failure - retry.
        Start-Sleep -Milliseconds 500
    }
}
if (-not $buildOk) {
    Write-Error "Build failed after $buildAttempt attempts."
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
Write-Host "VERDICT: MAP22E_SYSTEM2_STATIC_ROAD_INTENT_EXTRACT_COMPLETE"
