#Requires -Version 5.1
<#
.SYNOPSIS
    Writes a deterministic local proof packet (v0.16) covering ImageMapForge,
    palette SHA-256 verification, TMX integrity, region extraction, primitive classification,
    planning recommendation artifacts, plan-recommendations contract (incl. thresholds_used),
    and a separate dotnet_validation_summary section tracking .NET xUnit test counts.
    Hardening harness covers -Resize (36 assertions).

    Reads parsed-cell.json, regions.json, primitives.json and companion files,
    computes SHA-256 hashes, captures git state, and writes:
      .local/mapforge/proof-packet.json
      .local/mapforge/proof-packet.md

    If parsed-cell.json is missing, runs validate.ps1 first.
    If regions.json is missing, runs extract-regions.ps1.
    If primitives.json is missing, runs classify-primitives.ps1.
    All outputs are local-only. Does not commit. Does not touch media/maps.
    Does not claim playable Project Zomboid export.
#>

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$scriptDir          = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot           = Split-Path -Parent $scriptDir
$outputDir          = Join-Path $repoRoot '.local\mapforge'
$jsonPath           = Join-Path $outputDir 'parsed-cell.json'
$reportPath         = Join-Path $outputDir 'parsed-cell-report.md'
$previewPath        = Join-Path $outputDir 'parsed-cell-preview.png'
$tilesPath          = Join-Path $outputDir 'parsed-cell-tiles.png'
$tmxPath            = Join-Path $outputDir 'parsed-cell-basic.tmx'
$regionsJsonPath    = Join-Path $outputDir 'regions.json'
$regionsMdPath      = Join-Path $outputDir 'regions-report.md'
$primitivesJsonPath = Join-Path $outputDir 'primitives.json'
$primitivesMdPath   = Join-Path $outputDir 'primitives-report.md'
$planJsonPath       = Join-Path $outputDir 'plan-recommendations.json'
$planMdPath         = Join-Path $outputDir 'plan-report.md'
$packetJson         = Join-Path $outputDir 'proof-packet.json'
$packetMd           = Join-Path $outputDir 'proof-packet.md'

# ---------------------------------------------------------------------------
# Ensure artifacts exist
# ---------------------------------------------------------------------------

if (-not (Test-Path $jsonPath -PathType Leaf)) {
    Write-Output "parsed-cell.json not found. Running validate.ps1 first..."
    & powershell -ExecutionPolicy Bypass -File (Join-Path $repoRoot 'scripts\validate.ps1')
    if ($LASTEXITCODE -ne 0) { Write-Error "validate.ps1 failed."; exit 1 }
}

foreach ($p in @($jsonPath, $reportPath, $previewPath, $tilesPath, $tmxPath)) {
    if (-not (Test-Path $p -PathType Leaf)) {
        Write-Error "Required ImageMapForge artifact missing: $p"; exit 1
    }
}

if (-not (Test-Path $regionsJsonPath -PathType Leaf)) {
    Write-Output "regions.json not found. Running extract-regions.ps1..."
    & powershell -ExecutionPolicy Bypass -File (Join-Path $repoRoot 'scripts\extract-regions.ps1')
    if ($LASTEXITCODE -ne 0) { Write-Error "extract-regions.ps1 failed."; exit 1 }
}

foreach ($p in @($regionsJsonPath, $regionsMdPath)) {
    if (-not (Test-Path $p -PathType Leaf)) {
        Write-Error "Required region artifact missing: $p"; exit 1
    }
}

if (-not (Test-Path $primitivesJsonPath -PathType Leaf)) {
    Write-Output "primitives.json not found. Running classify-primitives.ps1..."
    & powershell -ExecutionPolicy Bypass -File (Join-Path $repoRoot 'scripts\classify-primitives.ps1')
    if ($LASTEXITCODE -ne 0) { Write-Error "classify-primitives.ps1 failed."; exit 1 }
}

foreach ($p in @($primitivesJsonPath, $primitivesMdPath)) {
    if (-not (Test-Path $p -PathType Leaf)) {
        Write-Error "Required primitive artifact missing: $p"; exit 1
    }
}

if (-not (Test-Path $planJsonPath -PathType Leaf)) {
    Write-Output "plan-recommendations.json not found. Running plan-export via dotnet..."
    & dotnet run --project (Join-Path $repoRoot 'src\PZMapForge.Cli') `
        --configuration Release --no-build `
        -- plan-export `
        --path (Join-Path $repoRoot '.local\mapforge\parsed-cell.json') `
        --output (Join-Path $repoRoot '.local\mapforge')
    if ($LASTEXITCODE -ne 0) { Write-Error "plan-export failed."; exit 1 }
}

foreach ($p in @($planJsonPath, $planMdPath)) {
    if (-not (Test-Path $p -PathType Leaf)) {
        Write-Error "Required plan artifact missing: $p"; exit 1
    }
}

# ---------------------------------------------------------------------------
# SHA-256 helper
# ---------------------------------------------------------------------------

function Get-FileSha256 {
    param([string]$Path)
    $sha    = [System.Security.Cryptography.SHA256]::Create()
    $stream = [System.IO.File]::OpenRead($Path)
    try {
        $hash = $sha.ComputeHash($stream)
        return (($hash | ForEach-Object { $_.ToString('x2') }) -join '')
    }
    finally { $stream.Dispose(); $sha.Dispose() }
}

# ---------------------------------------------------------------------------
# Git state (best-effort; 'unknown' if git unavailable)
# ---------------------------------------------------------------------------

$savedPref = $ErrorActionPreference
$ErrorActionPreference = 'SilentlyContinue'
$gitBranch = (@(git -C $repoRoot rev-parse --abbrev-ref HEAD) -join '').Trim()
if (-not $gitBranch) { $gitBranch = 'unknown' }
$gitCommit = (@(git -C $repoRoot rev-parse HEAD) -join '').Trim()
if (-not $gitCommit) { $gitCommit = 'unknown' }
$gitStatusLines = @(git -C $repoRoot status --porcelain)
$gitStatus = if ($gitStatusLines.Count -gt 0) { $gitStatusLines -join "`n" } else { '' }
$ErrorActionPreference = $savedPref

# ---------------------------------------------------------------------------
# Compute hashes
# ---------------------------------------------------------------------------

$generatedAt = (Get-Date).ToUniversalTime().ToString('yyyy-MM-ddTHH:mm:ssZ')

$parsedCellSha      = Get-FileSha256 $jsonPath
$reportSha          = Get-FileSha256 $reportPath
$previewSha         = Get-FileSha256 $previewPath
$tilesSha           = Get-FileSha256 $tilesPath
$tmxSha             = Get-FileSha256 $tmxPath
$regionsJsonSha     = Get-FileSha256 $regionsJsonPath
$regionsMdSha       = Get-FileSha256 $regionsMdPath
$primitivesJsonSha  = Get-FileSha256 $primitivesJsonPath
$primitivesMdSha    = Get-FileSha256 $primitivesMdPath
$planJsonSha        = Get-FileSha256 $planJsonPath
$planMdSha          = Get-FileSha256 $planMdPath

# ---------------------------------------------------------------------------
# Build proof packet
# ---------------------------------------------------------------------------

$packet = [ordered]@{
    schema                  = 'pzmapforge.proof-packet.v0.16'
    generated_at_utc        = $generatedAt
    repo_root               = $repoRoot
    git_branch              = $gitBranch
    git_commit              = $gitCommit
    git_status_short        = $gitStatus
    parsed_cell_path        = '.local/mapforge/parsed-cell.json'
    parsed_cell_sha256      = $parsedCellSha
    report_sha256           = $reportSha
    preview_sha256          = $previewSha
    tiles_sha256            = $tilesSha
    tmx_sha256              = $tmxSha
    regions_json_path       = '.local/mapforge/regions.json'
    regions_report_path     = '.local/mapforge/regions-report.md'
    regions_json_sha256     = $regionsJsonSha
    regions_report_sha256   = $regionsMdSha
    primitives_json_path    = '.local/mapforge/primitives.json'
    primitives_report_path  = '.local/mapforge/primitives-report.md'
    primitives_json_sha256  = $primitivesJsonSha
    primitives_report_sha256 = $primitivesMdSha
    plan_recommendations_path = '.local/mapforge/plan-recommendations.json'
    plan_report_path          = '.local/mapforge/plan-report.md'
    plan_recommendations_sha256 = $planJsonSha
    plan_report_sha256          = $planMdSha
    claim_boundary          = 'planning_artifact_only_not_pz_load_tested'
    validation_summary      = [ordered]@{
        schema_file_sanity          = 214
        artifact_contract           = 40
        palette_sha256_verification = 5
        tmx_integrity               = 21
        hardening_harness           = 36
        region_extraction           = 24
        primitive_classification    = 22
        plan_recommendations_contract = 28
        proof_packet                = 102
        total_expected_assertions   = 492
    }
    dotnet_validation_summary = [ordered]@{
        test_total                          = 438
        core_tests = 190
        cli_tests                           = 248
        process_cli_tests_present           = $true
        full_pipeline_contract_tests_present = $true
        full_pipeline_artifact_count        = 7
        full_pipeline_artifacts             = [string[]]@(
            'parsed-cell.json',
            'regions.json',
            'regions-report.md',
            'primitives.json',
            'primitives-report.md',
            'plan-recommendations.json',
            'plan-report.md'
        )
        layer_pipeline_present              = $true
        layer_pipeline_artifact_count       = 8
        layer_pipeline_artifacts            = [string[]]@(
            'parsed-cell.json',
            'layer-merge-report.md',
            'regions.json',
            'regions-report.md',
            'primitives.json',
            'primitives-report.md',
            'plan-recommendations.json',
            'plan-report.md'
        )
        layer_validate_present          = $true
        layer_validate_writes_artifacts = $false
	local_pz_config_loader_present = $true
	local_pz_config_loader_requires_real_install = $false
	local_pz_config_loader_inspects_assets = $false
	local_pz_config_loader_copies_assets = $false
	local_pz_install_validator_present = $true
	local_pz_install_validator_requires_real_install = $false
	local_pz_install_validator_reads_asset_contents = $false
	local_pz_install_validator_copies_assets = $false
	local_pz_install_validator_touches_media_maps = $false

        local_tile_reference_survey_writer_present = $true
        local_tile_reference_survey_writer_requires_real_install = $false
        local_tile_reference_survey_writer_reads_asset_contents = $false
        local_tile_reference_survey_writer_copies_assets = $false
        local_tile_reference_survey_writer_touches_media_maps = $false
        local_tile_reference_survey_writer_outputs_local_only = $true
        local_tile_survey_cli_present                       = $true
        local_tile_survey_cli_requires_real_install_for_tests = $false
        local_tile_survey_cli_outputs_local_only            = $true
        local_tile_survey_cli_copies_assets                 = $false
        local_tile_survey_cli_reads_asset_contents          = $false
        local_tile_survey_cli_touches_media_maps            = $false
        note = 'Dotnet validation is tracked separately from the PowerShell artifact validation pipeline.'
    }
    safety = [ordered]@{
        local_only_outputs      = $true
        media_maps_touched      = $false
        pz_assets_copied        = $false
        playable_export_claimed = $false
    }
}

New-Item -ItemType Directory -Force -Path $outputDir | Out-Null

$packet | ConvertTo-Json -Depth 5 | Set-Content -Path $packetJson -Encoding UTF8
Write-Output "Proof packet JSON: $packetJson"

# ---------------------------------------------------------------------------
# Markdown report
# ---------------------------------------------------------------------------

$statusDisplay = if ($gitStatus) { $gitStatus } else { '(clean)' }
$shortCommit   = if ($gitCommit.Length -ge 8) { $gitCommit.Substring(0, 8) } else { $gitCommit }

$md = @"
# PZMapForge Proof Packet

Generated: $generatedAt
Schema: pzmapforge.proof-packet.v0.16

## Claim boundary

planning_artifact_only_not_pz_load_tested

## Git state

| Field | Value |
|---|---|
| Branch | $gitBranch |
| Commit | $shortCommit |
| Status | $statusDisplay |

## ImageMapForge artifact hashes (SHA-256)

| Artifact | SHA-256 |
|---|---|
| parsed-cell.json | $parsedCellSha |
| parsed-cell-report.md | $reportSha |
| parsed-cell-preview.png | $previewSha |
| parsed-cell-tiles.png | $tilesSha |
| parsed-cell-basic.tmx | $tmxSha |

## Region extraction artifact hashes (SHA-256)

| Artifact | SHA-256 |
|---|---|
| regions.json | $regionsJsonSha |
| regions-report.md | $regionsMdSha |

## Primitive classification artifact hashes (SHA-256)

| Artifact | SHA-256 |
|---|---|
| primitives.json | $primitivesJsonSha |
| primitives-report.md | $primitivesMdSha |

## Planning recommendation artifact hashes (SHA-256)

| Artifact | SHA-256 |
|---|---|
| plan-recommendations.json | $planJsonSha |
| plan-report.md | $planMdSha |

## Validation summary (PowerShell lane)

| Check | Expected assertions |
|---|---:|
| Schema file sanity | 214 |
| Artifact contract | 40 |
| Palette SHA-256 verification | 5 |
| TMX integrity | 21 |
| Hardening harness | 36 |
| Region extraction | 24 |
| Primitive classification | 22 |
| Plan recommendations contract | 28 |
| Proof packet | 102 |
| Total | 492 |

## .NET validation summary (separate lane)

| Field | Value |
|---|---|
| test_total | 438 |
| core_tests | 190 |
| cli_tests | 248 |
| process_cli_tests_present | true |
| full_pipeline_contract_tests_present | true |
| full_pipeline_artifact_count | 7 |
| full_pipeline_artifacts | parsed-cell.json, regions.json, regions-report.md, primitives.json, primitives-report.md, plan-recommendations.json, plan-report.md |
| layer_pipeline_present | true |
| layer_pipeline_artifact_count | 8 |
| layer_pipeline_artifacts | parsed-cell.json, layer-merge-report.md, regions.json, regions-report.md, primitives.json, primitives-report.md, plan-recommendations.json, plan-report.md |
| layer_validate_present | true |
| layer_validate_writes_artifacts | false |
| local_pz_config_loader_present | true |
| local_pz_config_loader_requires_real_install | false |
| local_pz_config_loader_inspects_assets | false |
| local_pz_config_loader_copies_assets | false |
| local_pz_install_validator_present | true |
| local_pz_install_validator_requires_real_install | false |
| local_pz_install_validator_reads_asset_contents | false |
| local_pz_install_validator_copies_assets | false |
| local_pz_install_validator_touches_media_maps | false |
| local_tile_reference_survey_writer_present | true |
| local_tile_reference_survey_writer_requires_real_install | false |
| local_tile_reference_survey_writer_reads_asset_contents | false |
| local_tile_reference_survey_writer_copies_assets | false |
| local_tile_reference_survey_writer_touches_media_maps | false |
| local_tile_reference_survey_writer_outputs_local_only | true |
| local_tile_survey_cli_present | true |
| local_tile_survey_cli_requires_real_install_for_tests | false |
| local_tile_survey_cli_outputs_local_only | true |
| local_tile_survey_cli_copies_assets | false |
| local_tile_survey_cli_reads_asset_contents | false |
| local_tile_survey_cli_touches_media_maps | false |
Note: .NET test counts are tracked separately and are not included in total_expected_assertions.

## Safety

| Property | Value |
|---|---|
| Local-only outputs | true |
| media/maps touched | false |
| PZ assets copied | false |
| Playable export claimed | false |
"@

Set-Content -Path $packetMd -Value $md -Encoding UTF8
Write-Output "Proof packet MD:   $packetMd"
Write-Output "Done."
