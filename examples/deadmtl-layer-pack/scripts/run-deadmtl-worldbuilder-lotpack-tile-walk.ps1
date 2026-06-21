# MAP-36E EXP_003: Lotpack tile-walk read-only probe
# Read-only. Does not write lotpack, lotheader, chunkdata, or any live game file.

$RepoRoot = Resolve-Path "$PSScriptRoot\..\..\.."
$CliProject = "$RepoRoot\src\PZMapForge.Cli\PZMapForge.Cli.csproj"

$MinimalLotpack = "$RepoRoot\.local\map7y-packet\staged-workshop-sidecar-stubs\pzmapforge_build42_candidate_v4_001\common\media\maps\pzmapforge_build42_candidate_v4_001\world_35_27.lotpack"
$VisibleLotpack = "C:\Users\Palmacede\Zomboid\mods\pzmapforge_build42_candidate_v4_001\common\media\maps\pzmapforge_build42_candidate_v4_001\world_35_27.lotpack"
$OutputRoot = "$RepoRoot\.local\deadmtl-authoring\map36e-lotpack-tile-walk"

$args_list = @(
    "deadmtl-build-worldbuilder-lotpack-tile-walk"
    "--output-root", $OutputRoot
)

if (Test-Path $MinimalLotpack) {
    $args_list += "--minimal-lotpack", $MinimalLotpack
} else {
    Write-Warning "Minimal lotpack not found: $MinimalLotpack"
}

if (Test-Path $VisibleLotpack) {
    $args_list += "--visible-lotpack", $VisibleLotpack
} else {
    Write-Warning "Visible lotpack not found: $VisibleLotpack"
}

Write-Host "MAP-36E: Running lotpack tile-walk probe..."
Write-Host "  minimal: $MinimalLotpack"
Write-Host "  visible: $VisibleLotpack"
Write-Host "  output:  $OutputRoot"

dotnet run --project "$CliProject" -- @args_list

if ($LASTEXITCODE -ne 0) {
    Write-Error "MAP-36E lotpack tile-walk probe failed (exit $LASTEXITCODE)"
    exit $LASTEXITCODE
}

Write-Host ""
Write-Host "MAP-36E complete. Artifacts in: $OutputRoot"
