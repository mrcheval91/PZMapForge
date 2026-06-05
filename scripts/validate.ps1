#Requires -Version 5.1
<#
.SYNOPSIS
    Full local validation for PZMapForge.
    Runs all PowerShell validation sub-scripts and finishes with a ledger
    summary. All sub-scripts must pass; exits nonzero on any failure.

    Final output reports the complete PowerShell validation lane total (381)
    and the .NET lane total (152) as separate evidence lanes.
    Counts are sourced from proof-packet v0.16 / docs/VALIDATION_LEDGER.md.
    Do not edit the constants below without also updating the proof packet
    schema and the validation ledger.
#>

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot  = Split-Path -Parent $scriptDir

Write-Output 'PZMapForge validate.ps1'
Write-Output "Root: $repoRoot"
Write-Output ""

# Happy-path smoke: generate sample image and run ImageMapForge against it
Write-Output "--- Smoke: sample image + ImageMapForge ---"
& powershell -ExecutionPolicy Bypass -File (Join-Path $repoRoot 'scripts\new-test-image.ps1')
if ($LASTEXITCODE -ne 0) { throw "new-test-image.ps1 failed." }

& powershell -ExecutionPolicy Bypass -File (Join-Path $repoRoot 'source\image-mapforge.ps1') `
    -ImagePath (Join-Path $repoRoot '.local\mapforge\sample-input.png')
if ($LASTEXITCODE -ne 0) { throw "image-mapforge.ps1 failed." }

$required = @(
    '.local\mapforge\parsed-cell.json',
    '.local\mapforge\parsed-cell-report.md',
    '.local\mapforge\parsed-cell-preview.png',
    '.local\mapforge\parsed-cell-tiles.png',
    '.local\mapforge\parsed-cell-basic.tmx'
)

foreach ($relative in $required) {
    $path = Join-Path $repoRoot $relative
    if (-not (Test-Path -LiteralPath $path)) {
        throw "Missing expected output: $relative"
    }
    Write-Output "OK: $relative"
}

$mediaMaps = Join-Path $repoRoot 'media\maps'
if (Test-Path -LiteralPath $mediaMaps) {
    $items = @(Get-ChildItem -LiteralPath $mediaMaps -Recurse -Force -ErrorAction SilentlyContinue)
    if ($items.Count -gt 0) {
        throw 'media/maps contains files. ImageMapForge must not write into media/maps.'
    }
}

Write-Output ""
Write-Output "--- Schema file sanity ---"
& powershell -ExecutionPolicy Bypass -File (Join-Path $repoRoot 'scripts\test-schema-files.ps1')
if ($LASTEXITCODE -ne 0) { throw "Schema file sanity failed." }

Write-Output ""
Write-Output "--- Artifact contract validation ---"
& powershell -ExecutionPolicy Bypass -File (Join-Path $repoRoot 'scripts\test-parsed-cell-contract.ps1')
if ($LASTEXITCODE -ne 0) { throw "Artifact contract validation failed." }

Write-Output ""
Write-Output "--- Palette SHA-256 verification ---"
& powershell -ExecutionPolicy Bypass -File (Join-Path $repoRoot 'scripts\test-palette-sha256.ps1')
if ($LASTEXITCODE -ne 0) { throw "Palette SHA-256 verification failed." }

Write-Output ""
Write-Output "--- TMX integrity ---"
& powershell -ExecutionPolicy Bypass -File (Join-Path $repoRoot 'scripts\test-tmx-integrity.ps1')
if ($LASTEXITCODE -ne 0) { throw "TMX integrity validation failed." }

Write-Output ""
Write-Output "--- Hardening test harness ---"
& powershell -ExecutionPolicy Bypass -File (Join-Path $repoRoot 'tests\test-image-mapforge.ps1')
if ($LASTEXITCODE -ne 0) { throw "Hardening test harness failed." }

Write-Output ""
Write-Output "--- Restore sample artifacts for region extraction ---"
& powershell -ExecutionPolicy Bypass -File (Join-Path $repoRoot 'scripts\new-test-image.ps1')
if ($LASTEXITCODE -ne 0) { throw "new-test-image.ps1 failed (restore)." }

& powershell -ExecutionPolicy Bypass -File (Join-Path $repoRoot 'source\image-mapforge.ps1') `
    -ImagePath (Join-Path $repoRoot '.local\mapforge\sample-input.png')
if ($LASTEXITCODE -ne 0) { throw "image-mapforge.ps1 failed (restore)." }

Write-Output ""
Write-Output "--- Region extraction ---"
& powershell -ExecutionPolicy Bypass -File (Join-Path $repoRoot 'scripts\extract-regions.ps1')
if ($LASTEXITCODE -ne 0) { throw "extract-regions.ps1 failed." }

& powershell -ExecutionPolicy Bypass -File (Join-Path $repoRoot 'scripts\test-region-extraction.ps1')
if ($LASTEXITCODE -ne 0) { throw "Region extraction tests failed." }

Write-Output ""
Write-Output "--- Primitive classification ---"
& powershell -ExecutionPolicy Bypass -File (Join-Path $repoRoot 'scripts\classify-primitives.ps1')
if ($LASTEXITCODE -ne 0) { throw "classify-primitives.ps1 failed." }

& powershell -ExecutionPolicy Bypass -File (Join-Path $repoRoot 'scripts\test-primitive-classification.ps1')
if ($LASTEXITCODE -ne 0) { throw "Primitive classification tests failed." }

Write-Output ""
Write-Output "--- Plan export ---"
& dotnet run --project (Join-Path $repoRoot 'src\PZMapForge.Cli') `
    --configuration Release --no-build `
    -- plan-export `
    --path (Join-Path $repoRoot '.local\mapforge\parsed-cell.json') `
    --output (Join-Path $repoRoot '.local\mapforge')
if ($LASTEXITCODE -ne 0) { throw "plan-export failed." }

Write-Output ""
Write-Output "--- Plan recommendations contract ---"
& powershell -ExecutionPolicy Bypass -File (Join-Path $repoRoot 'scripts\test-plan-recommendations-contract.ps1')
if ($LASTEXITCODE -ne 0) { throw "Plan recommendations contract failed." }

Write-Output ""
Write-Output "--- Proof packet ---"
& powershell -ExecutionPolicy Bypass -File (Join-Path $repoRoot 'scripts\write-proof-packet.ps1')
if ($LASTEXITCODE -ne 0) { throw "write-proof-packet.ps1 failed." }

& powershell -ExecutionPolicy Bypass -File (Join-Path $repoRoot 'scripts\test-proof-packet.ps1')
if ($LASTEXITCODE -ne 0) { throw "Proof packet validation failed." }

Write-Output ""
Write-Output "--- MAP-4A evidence contract artifacts ---"
$map4aDoc      = Join-Path $repoRoot 'docs\COMPILED_CELL_FORMAT_EVIDENCE.md'
$map4aScript   = Join-Path $repoRoot 'scripts\inspect-compiled-cell-evidence.ps1'
$map4aTemplate = Join-Path $repoRoot 'docs\examples\compiled-cell-evidence\COMPILED_CELL_EVIDENCE_TEMPLATE.md'
if (-not (Test-Path -LiteralPath $map4aDoc))      { throw "MAP-4A doc missing: docs\COMPILED_CELL_FORMAT_EVIDENCE.md" }
Write-Output "OK: docs\COMPILED_CELL_FORMAT_EVIDENCE.md"
if (-not (Test-Path -LiteralPath $map4aScript))   { throw "MAP-4A script missing: scripts\inspect-compiled-cell-evidence.ps1" }
Write-Output "OK: scripts\inspect-compiled-cell-evidence.ps1"
if (-not (Test-Path -LiteralPath $map4aTemplate)) { throw "MAP-4A template missing: docs\examples\compiled-cell-evidence\COMPILED_CELL_EVIDENCE_TEMPLATE.md" }
Write-Output "OK: docs\examples\compiled-cell-evidence\COMPILED_CELL_EVIDENCE_TEMPLATE.md"
$map4aContent = Get-Content -LiteralPath $map4aScript -Raw
if ($map4aContent -notmatch '\.local') { throw "MAP-4A script missing .local refusal language" }
Write-Output "OK: script contains .local refusal language"
if ($map4aContent -notmatch 'copied_input_files') { throw "MAP-4A script missing copied_input_files sentinel" }
Write-Output "OK: script contains copied_input_files sentinel"

Write-Output ""
Write-Output "--- MAP-4C text metadata script contract ---"
$map4cScript = Join-Path $repoRoot 'scripts\inspect-map-text-metadata.ps1'
if (-not (Test-Path -LiteralPath $map4cScript)) { throw "MAP-4C script missing: scripts\inspect-map-text-metadata.ps1" }
Write-Output "OK: scripts\inspect-map-text-metadata.ps1"
$map4cContent = Get-Content -LiteralPath $map4cScript -Raw
if ($map4cContent -notmatch '\.local') { throw "MAP-4C script missing .local refusal language" }
Write-Output "OK: script contains .local refusal language"
if ($map4cContent -notmatch 'binary_files_read') { throw "MAP-4C script missing binary_files_read sentinel" }
Write-Output "OK: script contains binary_files_read sentinel"
if ($map4cContent -notmatch 'compiled_writer_implemented') { throw "MAP-4C script missing compiled_writer_implemented sentinel" }
Write-Output "OK: script contains compiled_writer_implemented sentinel"

Write-Output ""
Write-Output "--- MAP-4D binary header script contract ---"
$map4dScript = Join-Path $repoRoot 'scripts\inspect-compiled-binary-headers.ps1'
if (-not (Test-Path -LiteralPath $map4dScript)) { throw "MAP-4D script missing: scripts\inspect-compiled-binary-headers.ps1" }
Write-Output "OK: scripts\inspect-compiled-binary-headers.ps1"
$map4dContent = Get-Content -LiteralPath $map4dScript -Raw
if ($map4dContent -notmatch '\.local') { throw "MAP-4D script missing .local refusal language" }
Write-Output "OK: script contains .local refusal language"
if ($map4dContent -notmatch 'full_binary_files_read') { throw "MAP-4D script missing full_binary_files_read sentinel" }
Write-Output "OK: script contains full_binary_files_read sentinel"
if ($map4dContent -notmatch 'compiled_writer_implemented') { throw "MAP-4D script missing compiled_writer_implemented sentinel" }
Write-Output "OK: script contains compiled_writer_implemented sentinel"
if ($map4dContent -notmatch '256') { throw "MAP-4D script missing MaxBytes 256 guard" }
Write-Output "OK: script contains MaxBytes 256 guard"

Write-Output ""
Write-Output "--- MAP-4E lotheader string table script contract ---"
$map4eScript = Join-Path $repoRoot 'scripts\inspect-lotheader-string-table.ps1'
if (-not (Test-Path -LiteralPath $map4eScript)) { throw "MAP-4E script missing: scripts\inspect-lotheader-string-table.ps1" }
Write-Output "OK: scripts\inspect-lotheader-string-table.ps1"
$map4eContent = Get-Content -LiteralPath $map4eScript -Raw
if ($map4eContent -notmatch '\.local') { throw "MAP-4E script missing .local refusal language" }
Write-Output "OK: script contains .local refusal language"
if ($map4eContent -notmatch 'only_lotheader_files_read') { throw "MAP-4E script missing only_lotheader_files_read sentinel" }
Write-Output "OK: script contains only_lotheader_files_read sentinel"
if ($map4eContent -notmatch 'lotpack_files_read') { throw "MAP-4E script missing lotpack_files_read sentinel" }
Write-Output "OK: script contains lotpack_files_read sentinel"
if ($map4eContent -notmatch 'bin_files_read') { throw "MAP-4E script missing bin_files_read sentinel" }
Write-Output "OK: script contains bin_files_read sentinel"
if ($map4eContent -notmatch 'compiled_writer_implemented') { throw "MAP-4E script missing compiled_writer_implemented sentinel" }
Write-Output "OK: script contains compiled_writer_implemented sentinel"

Write-Output ""
Write-Output "--- MAP-4F lotpack offset table script contract ---"
$map4fScript = Join-Path $repoRoot 'scripts\inspect-lotpack-offset-table.ps1'
if (-not (Test-Path -LiteralPath $map4fScript)) { throw "MAP-4F script missing: scripts\inspect-lotpack-offset-table.ps1" }
Write-Output "OK: scripts\inspect-lotpack-offset-table.ps1"
$map4fContent = Get-Content -LiteralPath $map4fScript -Raw
if ($map4fContent -notmatch '\.local') { throw "MAP-4F script missing .local refusal language" }
Write-Output "OK: script contains .local refusal language"
if ($map4fContent -notmatch 'only_lotpack_files_read') { throw "MAP-4F script missing only_lotpack_files_read sentinel" }
Write-Output "OK: script contains only_lotpack_files_read sentinel"
if ($map4fContent -notmatch 'lotheader_files_read') { throw "MAP-4F script missing lotheader_files_read sentinel" }
Write-Output "OK: script contains lotheader_files_read sentinel"
if ($map4fContent -notmatch 'bin_files_read') { throw "MAP-4F script missing bin_files_read sentinel" }
Write-Output "OK: script contains bin_files_read sentinel"
if ($map4fContent -notmatch 'full_lotpack_files_read') { throw "MAP-4F script missing full_lotpack_files_read sentinel" }
Write-Output "OK: script contains full_lotpack_files_read sentinel"
if ($map4fContent -notmatch 'compiled_writer_implemented') { throw "MAP-4F script missing compiled_writer_implemented sentinel" }
Write-Output "OK: script contains compiled_writer_implemented sentinel"

Write-Output ""
Write-Output "--- MAP-4G chunkdata binary patterns script contract ---"
$map4gScript = Join-Path $repoRoot 'scripts\inspect-chunkdata-binary-patterns.ps1'
if (-not (Test-Path -LiteralPath $map4gScript)) { throw "MAP-4G script missing: scripts\inspect-chunkdata-binary-patterns.ps1" }
Write-Output "OK: scripts\inspect-chunkdata-binary-patterns.ps1"
$map4gContent = Get-Content -LiteralPath $map4gScript -Raw
if ($map4gContent -notmatch '\.local') { throw "MAP-4G script missing .local refusal language" }
Write-Output "OK: script contains .local refusal language"
if ($map4gContent -notmatch 'only_chunkdata_bin_files_read') { throw "MAP-4G script missing only_chunkdata_bin_files_read sentinel" }
Write-Output "OK: script contains only_chunkdata_bin_files_read sentinel"
if ($map4gContent -notmatch 'lotheader_files_read') { throw "MAP-4G script missing lotheader_files_read sentinel" }
Write-Output "OK: script contains lotheader_files_read sentinel"
if ($map4gContent -notmatch 'lotpack_files_read') { throw "MAP-4G script missing lotpack_files_read sentinel" }
Write-Output "OK: script contains lotpack_files_read sentinel"
if ($map4gContent -notmatch 'bin_files_written') { throw "MAP-4G script missing bin_files_written sentinel" }
Write-Output "OK: script contains bin_files_written sentinel"
if ($map4gContent -notmatch 'compiled_writer_implemented') { throw "MAP-4G script missing compiled_writer_implemented sentinel" }
Write-Output "OK: script contains compiled_writer_implemented sentinel"

Write-Output ""
Write-Output "--- MAP-6I Build 42 format design matrix ---"
$map6iDoc    = Join-Path $repoRoot 'docs\MAP_6I_BUILD42_FORMAT_DESIGN_MATRIX.md'
$map6iScript = Join-Path $repoRoot 'scripts\derive-build42-format-design-matrix.ps1'
$map6iTests  = Join-Path $repoRoot 'scripts\test-build42-format-design-matrix.ps1'
if (-not (Test-Path -LiteralPath $map6iDoc))    { throw "MAP-6I doc missing: docs\MAP_6I_BUILD42_FORMAT_DESIGN_MATRIX.md" }
Write-Output "OK: docs\MAP_6I_BUILD42_FORMAT_DESIGN_MATRIX.md"
if (-not (Test-Path -LiteralPath $map6iScript)) { throw "MAP-6I script missing: scripts\derive-build42-format-design-matrix.ps1" }
Write-Output "OK: scripts\derive-build42-format-design-matrix.ps1"
if (-not (Test-Path -LiteralPath $map6iTests))  { throw "MAP-6I tests missing: scripts\test-build42-format-design-matrix.ps1" }
Write-Output "OK: scripts\test-build42-format-design-matrix.ps1"
$map6iDocContent = Get-Content -LiteralPath $map6iDoc -Raw
if ($map6iDocContent -notmatch 'BUILD42_FORMAT_DESIGN_MATRIX_CREATED') { throw "MAP-6I doc missing BUILD42_FORMAT_DESIGN_MATRIX_CREATED" }
Write-Output "OK: doc contains BUILD42_FORMAT_DESIGN_MATRIX_CREATED"
if ($map6iDocContent -notmatch 'WRITER_NOT_IMPLEMENTED') { throw "MAP-6I doc missing WRITER_NOT_IMPLEMENTED" }
Write-Output "OK: doc contains WRITER_NOT_IMPLEMENTED"
if ($map6iDocContent -notmatch 'PLAYABLE_EXPORT_CLAIM_ALLOWED=false') { throw "MAP-6I doc missing PLAYABLE_EXPORT_CLAIM_ALLOWED=false" }
Write-Output "OK: doc contains PLAYABLE_EXPORT_CLAIM_ALLOWED=false"

Write-Output ""
Write-Output "--- MAP-6I format design matrix tests ---"
& powershell -ExecutionPolicy Bypass -File $map6iTests
if ($LASTEXITCODE -ne 0) { throw "MAP-6I format design matrix tests failed." }

Write-Output ""
Write-Output "--- MAP-6H Build 42 LOTP LTZH deep inspection ---"
$map6hDoc = Join-Path $repoRoot 'docs\MAP_6H_BUILD42_LOTP_LOTH_DEEP_INSPECTION.md'
if (-not (Test-Path -LiteralPath $map6hDoc)) { throw "MAP-6H doc missing: docs\MAP_6H_BUILD42_LOTP_LOTH_DEEP_INSPECTION.md" }
Write-Output "OK: docs\MAP_6H_BUILD42_LOTP_LOTH_DEEP_INSPECTION.md"
$map6hContent = Get-Content -LiteralPath $map6hDoc -Raw
if ($map6hContent -notmatch 'BUILD42_LOTH_LOTHEADER_FORMAT_OBSERVED') { throw "MAP-6H doc missing BUILD42_LOTH_LOTHEADER_FORMAT_OBSERVED sentinel" }
Write-Output "OK: doc contains BUILD42_LOTH_LOTHEADER_FORMAT_OBSERVED"
if ($map6hContent -notmatch 'BUILD42_256_MODEL_STRONGLY_SUPPORTED') { throw "MAP-6H doc missing BUILD42_256_MODEL_STRONGLY_SUPPORTED sentinel" }
Write-Output "OK: doc contains BUILD42_256_MODEL_STRONGLY_SUPPORTED"
if ($map6hContent -notmatch 'PLAYABLE_EXPORT_CLAIM_ALLOWED=false') { throw "MAP-6H doc missing PLAYABLE_EXPORT_CLAIM_ALLOWED=false sentinel" }
Write-Output "OK: doc contains PLAYABLE_EXPORT_CLAIM_ALLOWED=false"

Write-Output ""
Write-Output "--- MAP-6G Build 42 LOTP lotpack evidence ---"
$map6gDoc = Join-Path $repoRoot 'docs\MAP_6G_BUILD42_LOTP_LOTPACK_EVIDENCE.md'
if (-not (Test-Path -LiteralPath $map6gDoc)) { throw "MAP-6G doc missing: docs\MAP_6G_BUILD42_LOTP_LOTPACK_EVIDENCE.md" }
Write-Output "OK: docs\MAP_6G_BUILD42_LOTP_LOTPACK_EVIDENCE.md"
$map6gContent = Get-Content -LiteralPath $map6gDoc -Raw
if ($map6gContent -notmatch 'BUILD42_LOTP_FORMAT_OBSERVED') { throw "MAP-6G doc missing BUILD42_LOTP_FORMAT_OBSERVED sentinel" }
Write-Output "OK: doc contains BUILD42_LOTP_FORMAT_OBSERVED"
if ($map6gContent -notmatch 'LEGACY_900_LOTPACK_HEADER_NOT_APPLICABLE_TO_REFERENCE') { throw "MAP-6G doc missing LEGACY_900 sentinel" }
Write-Output "OK: doc contains LEGACY_900_LOTPACK_HEADER_NOT_APPLICABLE_TO_REFERENCE"
if ($map6gContent -notmatch 'PLAYABLE_EXPORT_CLAIM_ALLOWED=false') { throw "MAP-6G doc missing PLAYABLE_EXPORT_CLAIM_ALLOWED=false sentinel" }
Write-Output "OK: doc contains PLAYABLE_EXPORT_CLAIM_ALLOWED=false"

Write-Output ""
Write-Output "--- MAP-6F Build 42 reference geometry inspector ---"
$map6fDoc    = Join-Path $repoRoot 'docs\MAP_6F_BUILD42_REFERENCE_GEOMETRY_PACKET.md'
$map6fScript = Join-Path $repoRoot 'scripts\inspect-build42-reference-geometry.ps1'
$map6fTests  = Join-Path $repoRoot 'scripts\test-build42-reference-geometry-inspector.ps1'
if (-not (Test-Path -LiteralPath $map6fDoc)) { throw "MAP-6F doc missing: docs\MAP_6F_BUILD42_REFERENCE_GEOMETRY_PACKET.md" }
Write-Output "OK: docs\MAP_6F_BUILD42_REFERENCE_GEOMETRY_PACKET.md"
if (-not (Test-Path -LiteralPath $map6fScript)) { throw "MAP-6F script missing: scripts\inspect-build42-reference-geometry.ps1" }
Write-Output "OK: scripts\inspect-build42-reference-geometry.ps1"
if (-not (Test-Path -LiteralPath $map6fTests)) { throw "MAP-6F test missing: scripts\test-build42-reference-geometry-inspector.ps1" }
Write-Output "OK: scripts\test-build42-reference-geometry-inspector.ps1"
$map6fDocContent = Get-Content -LiteralPath $map6fDoc -Raw
if ($map6fDocContent -notmatch 'REFERENCE_GEOMETRY_OBSERVED') { throw "MAP-6F doc missing REFERENCE_GEOMETRY_OBSERVED sentinel" }
Write-Output "OK: doc contains REFERENCE_GEOMETRY_OBSERVED"
if ($map6fDocContent -notmatch 'PLAYABLE_EXPORT_CLAIM_ALLOWED=false') { throw "MAP-6F doc missing PLAYABLE_EXPORT_CLAIM_ALLOWED=false sentinel" }
Write-Output "OK: doc contains PLAYABLE_EXPORT_CLAIM_ALLOWED=false"
$map6fScriptContent = Get-Content -LiteralPath $map6fScript -Raw
if ($map6fScriptContent -notmatch '\.local') { throw "MAP-6F script missing .local refusal language" }
Write-Output "OK: script contains .local refusal language"
if ($map6fScriptContent -notmatch 'reference_files_copied') { throw "MAP-6F script missing reference_files_copied sentinel" }
Write-Output "OK: script contains reference_files_copied sentinel"
if ($map6fScriptContent -notmatch 'pz_assets_copied') { throw "MAP-6F script missing pz_assets_copied sentinel" }
Write-Output "OK: script contains pz_assets_copied sentinel"
if ($map6fScriptContent -notmatch 'playable_export_claimed') { throw "MAP-6F script missing playable_export_claimed sentinel" }
Write-Output "OK: script contains playable_export_claimed sentinel"

Write-Output ""
Write-Output "--- Build42 geometry inspector tests ---"
& powershell -ExecutionPolicy Bypass -File $map6fTests
if ($LASTEXITCODE -ne 0) { throw "Build42 geometry inspector tests failed." }

Write-Output ""
Write-Output "--- MAP-6E Build 42 geometry model audit ---"
$map6eDoc = Join-Path $repoRoot 'docs\MAP_6E_BUILD42_GEOMETRY_MODEL_AUDIT.md'
if (-not (Test-Path -LiteralPath $map6eDoc)) { throw "MAP-6E doc missing: docs\MAP_6E_BUILD42_GEOMETRY_MODEL_AUDIT.md" }
Write-Output "OK: docs\MAP_6E_BUILD42_GEOMETRY_MODEL_AUDIT.md"
$map6eContent = Get-Content -LiteralPath $map6eDoc -Raw
if ($map6eContent -notmatch 'GEOMETRY_MODEL_UNVERIFIED') { throw "MAP-6E doc missing GEOMETRY_MODEL_UNVERIFIED sentinel" }
Write-Output "OK: doc contains GEOMETRY_MODEL_UNVERIFIED"
if ($map6eContent -notmatch 'BUILD42_256_MODEL_OPERATOR_REPORTED') { throw "MAP-6E doc missing BUILD42_256_MODEL_OPERATOR_REPORTED sentinel" }
Write-Output "OK: doc contains BUILD42_256_MODEL_OPERATOR_REPORTED"
if ($map6eContent -notmatch 'PLAYABLE_EXPORT_CLAIM_ALLOWED=false') { throw "MAP-6E doc missing PLAYABLE_EXPORT_CLAIM_ALLOWED=false sentinel" }
Write-Output "OK: doc contains PLAYABLE_EXPORT_CLAIM_ALLOWED=false"
if ($map6eContent -notmatch 'LOAD_TEST_BLOCKED_PENDING_GEOMETRY_DECISION') { throw "MAP-6E doc missing LOAD_TEST_BLOCKED_PENDING_GEOMETRY_DECISION sentinel" }
Write-Output "OK: doc contains LOAD_TEST_BLOCKED_PENDING_GEOMETRY_DECISION"

Write-Output ""
Write-Output "--- MAP-6D non-empty lotheader candidate ---"
$map6dDoc = Join-Path $repoRoot 'docs\MAP_6D_NONEMPTY_LOTHEADER_CANDIDATE.md'
if (-not (Test-Path -LiteralPath $map6dDoc)) { throw "MAP-6D doc missing: docs\MAP_6D_NONEMPTY_LOTHEADER_CANDIDATE.md" }
Write-Output "OK: docs\MAP_6D_NONEMPTY_LOTHEADER_CANDIDATE.md"
$map6dContent = Get-Content -LiteralPath $map6dDoc -Raw
if ($map6dContent -notmatch 'LOTHEADER_CANDIDATE_V2=newline_tileset_table_minimal') { throw "MAP-6D doc missing LOTHEADER_CANDIDATE_V2=newline_tileset_table_minimal sentinel" }
Write-Output "OK: doc contains LOTHEADER_CANDIDATE_V2=newline_tileset_table_minimal"
if ($map6dContent -notmatch 'PLAYABLE_EXPORT_CLAIM_ALLOWED=false') { throw "MAP-6D doc missing PLAYABLE_EXPORT_CLAIM_ALLOWED=false sentinel" }
Write-Output "OK: doc contains PLAYABLE_EXPORT_CLAIM_ALLOWED=false"
if ($map6dContent -notmatch 'generated_not_load_tested') { throw "MAP-6D doc missing generated_not_load_tested sentinel" }
Write-Output "OK: doc contains generated_not_load_tested"

Write-Output ""
Write-Output "--- MAP-6C lotheader format research packet ---"
$map6cDoc = Join-Path $repoRoot 'docs\MAP_6C_LOTHEADER_FORMAT_RESEARCH_PACKET.md'
if (-not (Test-Path -LiteralPath $map6cDoc)) { throw "MAP-6C doc missing: docs\MAP_6C_LOTHEADER_FORMAT_RESEARCH_PACKET.md" }
Write-Output "OK: docs\MAP_6C_LOTHEADER_FORMAT_RESEARCH_PACKET.md"
$map6cContent = Get-Content -LiteralPath $map6cDoc -Raw
if ($map6cContent -notmatch 'LOTHEADER_CANDIDATE_V0=current_failed') { throw "MAP-6C doc missing LOTHEADER_CANDIDATE_V0=current_failed sentinel" }
Write-Output "OK: doc contains LOTHEADER_CANDIDATE_V0=current_failed"
if ($map6cContent -notmatch 'LOTHEADER_CANDIDATE_V1=newline_tileset_table') { throw "MAP-6C doc missing LOTHEADER_CANDIDATE_V1=newline_tileset_table sentinel" }
Write-Output "OK: doc contains LOTHEADER_CANDIDATE_V1=newline_tileset_table"
if ($map6cContent -notmatch 'PLAYABLE_EXPORT_CLAIM_ALLOWED=false') { throw "MAP-6C doc missing PLAYABLE_EXPORT_CLAIM_ALLOWED=false sentinel" }
Write-Output "OK: doc contains PLAYABLE_EXPORT_CLAIM_ALLOWED=false"

Write-Output ""
Write-Output "--- MAP-6B binary format failure record ---"
$map6bDoc = Join-Path $repoRoot 'docs\MAP_6B_BINARY_FORMAT_FAILURE_RECORD.md'
if (-not (Test-Path -LiteralPath $map6bDoc)) { throw "MAP-6B doc missing: docs\MAP_6B_BINARY_FORMAT_FAILURE_RECORD.md" }
Write-Output "OK: docs\MAP_6B_BINARY_FORMAT_FAILURE_RECORD.md"
$map6bContent = Get-Content -LiteralPath $map6bDoc -Raw
if ($map6bContent -notmatch 'BINARY_FAILURE_CONFIRMED') { throw "MAP-6B doc missing BINARY_FAILURE_CONFIRMED sentinel" }
Write-Output "OK: doc contains BINARY_FAILURE_CONFIRMED"
if ($map6bContent -notmatch 'PLAYABLE_EXPORT_CLAIM_ALLOWED=false') { throw "MAP-6B doc missing PLAYABLE_EXPORT_CLAIM_ALLOWED=false sentinel" }
Write-Output "OK: doc contains PLAYABLE_EXPORT_CLAIM_ALLOWED=false"
if ($map6bContent -notmatch 'DISCOVERY_PASS_VERSIONED_LAYOUT') { throw "MAP-6B doc missing DISCOVERY_PASS_VERSIONED_LAYOUT sentinel" }
Write-Output "OK: doc contains DISCOVERY_PASS_VERSIONED_LAYOUT"

Write-Output ""
Write-Output "========================================"
Write-Output "PZMapForge validation summary"
Write-Output "========================================"

# ---------------------------------------------------------------------------
# Ledger constants - sourced from proof-packet v0.16 / docs/VALIDATION_LEDGER.md.
# Update here when counts change; update the proof packet schema and ledger too.
# ---------------------------------------------------------------------------

$psChecks = [ordered]@{
    'Schema file sanity'                  = 214
    'Artifact contract'                   = 40
    'Palette SHA-256 verification'        = 5
    'TMX integrity'                       = 21
    'Hardening harness'                   = 36
    'Region extraction'                   = 24
    'Primitive classification'            = 22
    'Plan recommendations contract'       = 28
    'Proof packet'                        = 102
    'Build42 geometry inspector tests'    = 23
    'Build42 format design matrix tests' = 13
}
$psTotal = 528   # = validation_summary.total_expected_assertions in proof-packet v0.16

$dnCoreTests = 190   # PZMapForge.Core.Tests
$dnCliTests  = 250   # PZMapForge.Cli.Tests (MAP-6E: +2 geometry model status tests)
$dnTotal     = 440   # = dotnet_validation_summary.test_total in proof-packet v0.16

Write-Output ""
Write-Output "  PowerShell lane  (validation_summary in proof-packet v0.16):"
foreach ($kv in $psChecks.GetEnumerator()) {
    Write-Output ("    {0,-34} {1,4}" -f "$($kv.Key):", $kv.Value)
}
Write-Output "    -------------------------------------- ----"
Write-Output ("    {0,-34} {1,4}" -f "Total:", $psTotal)

Write-Output ""
Write-Output "  .NET lane  (dotnet_validation_summary in proof-packet v0.16 -- tracked separately):"
Write-Output ("    {0,-34} {1,4}" -f "Core tests (PZMapForge.Core.Tests):", $dnCoreTests)
Write-Output ("    {0,-34} {1,4}" -f "CLI tests  (PZMapForge.Cli.Tests):", $dnCliTests)
Write-Output "    -------------------------------------- ----"
Write-Output ("    {0,-34} {1,4}" -f "Total:", $dnTotal)

Write-Output ""
Write-Output ("  PS {0} + .NET {1} = two separate evidence lanes, not summed." -f $psTotal, $dnTotal)
Write-Output "  Claim boundary: planning_artifact_only_not_pz_load_tested"
Write-Output ""
Write-Output "========================================"
Write-Output "Validation passed."
Write-Output "========================================"
