param(
    [int]$OriginX = 10580,
    [int]$OriginY = 8200
)

$ErrorActionPreference = "Stop"

$repoRoot          = $PSScriptRoot | Split-Path -Parent | Split-Path -Parent | Split-Path -Parent
$TileFamilyScript  = Join-Path $PSScriptRoot "run-system2-static-road-tile-family-plan.ps1"
$CliProject        = Join-Path $repoRoot "src\PZMapForge.Cli\PZMapForge.Cli.csproj"

$TileFamilyJson = Join-Path $repoRoot ".local\deadmtl-authoring\system2-static-road-tile-family-plan\system2_static_road_tile_family_plan.json"
$SurveyDir      = Join-Path $repoRoot ".local\deadmtl-authoring\system2-static-road-tile-family-survey"
$SurveyJson     = Join-Path $SurveyDir "system2_static_road_tile_family_survey.json"
$SurveySummary  = Join-Path $SurveyDir "system2_static_road_tile_family_survey.summary.txt"

Write-Host "Running tile-family plan to produce input..."
& powershell -ExecutionPolicy Bypass -File $TileFamilyScript
if ($LASTEXITCODE -ne 0) {
    Write-Error "Tile family plan failed with exit code $LASTEXITCODE"
    exit 1
}

New-Item -ItemType Directory -Force -Path $SurveyDir | Out-Null

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

Write-Host "Running system2-build-static-road-tile-family-survey..."
& dotnet run --project $CliProject --configuration Release --no-build -- `
    system2-build-static-road-tile-family-survey `
    --input   $TileFamilyJson `
    --output  $SurveyJson `
    --summary $SurveySummary

if ($LASTEXITCODE -ne 0) {
    Write-Error "system2-build-static-road-tile-family-survey failed with exit code $LASTEXITCODE"
    exit 1
}

Write-Host ""
Write-Host "Survey:  $SurveyJson"
Write-Host "Summary: $SurveySummary"
Write-Host "VERDICT: MAP22I_SYSTEM2_STATIC_ROAD_TILE_FAMILY_SURVEY_COMPLETE"
