$repoRoot = $PSScriptRoot | Split-Path -Parent | Split-Path -Parent | Split-Path -Parent
$outDir = Join-Path $repoRoot ".local\deadmtl-authoring\worldbuilder-component-access-profile\map_00"
if (-not (Test-Path $outDir)) { New-Item -ItemType Directory -Force -Path $outDir | Out-Null }

dotnet run --project (Join-Path $repoRoot "src\PZMapForge.Cli\PZMapForge.Cli.csproj") `
    --configuration Release --no-build -- `
    deadmtl-build-worldbuilder-component-access-profile `
    --planning-candidates (Join-Path $repoRoot ".local\deadmtl-authoring\worldbuilder-adjacency-planning-candidate-extraction\map_00\map_00.adjacency_planning_candidate_extraction.json") `
    --component-intents   (Join-Path $repoRoot ".local\deadmtl-authoring\worldbuilder-component-intent-classification\map_00\map_00.component_intent_classification.json") `
    --output-json  (Join-Path $outDir "map_00.component_access_profile.json") `
    --output-md    (Join-Path $outDir "map_00.component_access_profile.md") `
    --output-csv   (Join-Path $outDir "map_00.component_access_profile.csv") `
    --summary      (Join-Path $outDir "map_00.component_access_profile.summary.txt")
