# MAP-36D EXP_002: Chunkdata hex-disassembly read-only probe
# Read-only. Does not write chunkdata, lotheader, lotpack, or any live game file.

$RepoRoot = Resolve-Path "$PSScriptRoot\..\..\.."
$CliProject = "$RepoRoot\src\PZMapForge.Cli\PZMapForge.Cli.csproj"

$MinimalChunkdata = "$RepoRoot\.local\map7y-packet\staged-workshop-sidecar-stubs\pzmapforge_build42_candidate_v4_001\common\media\maps\pzmapforge_build42_candidate_v4_001\chunkdata_35_27.bin"
$VisibleChunkdata = "C:\Users\Palmacede\Zomboid\mods\pzmapforge_build42_candidate_v4_001\common\media\maps\pzmapforge_build42_candidate_v4_001\chunkdata_35_27.bin"
$OutputRoot = "$RepoRoot\.local\deadmtl-authoring\map36d-chunkdata-hex-disassembly"

$args_list = @(
    "deadmtl-build-worldbuilder-chunkdata-hex-disassembly"
    "--output-root", $OutputRoot
)

if (Test-Path $MinimalChunkdata) {
    $args_list += "--minimal-chunkdata", $MinimalChunkdata
} else {
    Write-Warning "Minimal chunkdata not found: $MinimalChunkdata"
}

if (Test-Path $VisibleChunkdata) {
    $args_list += "--visible-chunkdata", $VisibleChunkdata
} else {
    Write-Warning "Visible chunkdata not found: $VisibleChunkdata"
}

Write-Host "MAP-36D: Running chunkdata hex-disassembly probe..."
Write-Host "  minimal: $MinimalChunkdata"
Write-Host "  visible: $VisibleChunkdata"
Write-Host "  output:  $OutputRoot"

dotnet run --project "$CliProject" -- @args_list

if ($LASTEXITCODE -ne 0) {
    Write-Error "MAP-36D chunkdata hex-disassembly probe failed (exit $LASTEXITCODE)"
    exit $LASTEXITCODE
}

Write-Host ""
Write-Host "MAP-36D complete. Artifacts in: $OutputRoot"
