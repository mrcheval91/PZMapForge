#Requires -Version 5.1
<#
.SYNOPSIS
    Full local validation for PZMapForge.
    Runs all PowerShell validation sub-scripts and finishes with a ledger
    summary. All sub-scripts must pass; exits nonzero on any failure.

    Final output reports the complete PowerShell validation lane total (958)
    and the .NET lane total (556) as separate evidence lanes.
    Counts are sourced from proof-packet v0.43 / docs/VALIDATION_LEDGER.md.
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
Write-Output "--- MAP-7N reference map id comparator ---"
$map7nDoc    = Join-Path $repoRoot 'docs\MAP_7N_REFERENCE_MAP_ID_COMPARATOR.md'
$map7nTests  = Join-Path $repoRoot 'scripts\test-build42-map7n-reference-map-id.ps1'
if (-not (Test-Path -LiteralPath $map7nDoc))   { throw "MAP-7N doc missing" }
Write-Output "OK: docs\MAP_7N_REFERENCE_MAP_ID_COMPARATOR.md"
if (-not (Test-Path -LiteralPath $map7nTests)) { throw "MAP-7N tests missing" }
Write-Output "OK: scripts\test-build42-map7n-reference-map-id.ps1"
$map7nDocContent = Get-Content -LiteralPath $map7nDoc -Raw
if ($map7nDocContent -notmatch 'REFERENCE_MAP_ID_SUPPORT_ADDED') { throw "MAP-7N doc missing REFERENCE_MAP_ID_SUPPORT_ADDED" }
Write-Output "OK: doc contains REFERENCE_MAP_ID_SUPPORT_ADDED"
if ($map7nDocContent -notmatch 'PUBLIC_PLAYABLE_CLAIM_ALLOWED=false') { throw "MAP-7N doc missing PUBLIC_PLAYABLE_CLAIM_ALLOWED=false" }
Write-Output "OK: doc contains PUBLIC_PLAYABLE_CLAIM_ALLOWED=false"
$map7nCompContent = Get-Content -LiteralPath (Join-Path $repoRoot 'scripts\inspect-build42-known-working-map-contract.ps1') -Raw
if ($map7nCompContent -notmatch 'ReferenceMapId') { throw "MAP-7N comparator missing ReferenceMapId parameter" }
Write-Output "OK: comparator contains ReferenceMapId parameter"
if ($map7nCompContent -notmatch 'reference_map_id') { throw "MAP-7N comparator missing reference_map_id field" }
Write-Output "OK: comparator contains reference_map_id field"

Write-Output ""
Write-Output "--- MAP-7N reference map id tests ---"
& powershell -ExecutionPolicy Bypass -File $map7nTests
if ($LASTEXITCODE -ne 0) { throw "MAP-7N reference map id tests failed." }

Write-Output ""
Write-Output "--- MAP-7M Variant H and known-working contract ---"
$map7mDoc          = Join-Path $repoRoot 'docs\MAP_7M_VARIANT_H_AND_WORKING_MAP_CONTRACT.md'
$map7mComparator   = Join-Path $repoRoot 'scripts\inspect-build42-known-working-map-contract.ps1'
$map7mPacketScript = Join-Path $repoRoot 'scripts\prepare-build42-map7m-known-working-contract-packet.ps1'
$map7mTests        = Join-Path $repoRoot 'scripts\test-build42-map7m-known-working-contract.ps1'
if (-not (Test-Path -LiteralPath $map7mDoc))          { throw "MAP-7M doc missing" }
Write-Output "OK: docs\MAP_7M_VARIANT_H_AND_WORKING_MAP_CONTRACT.md"
if (-not (Test-Path -LiteralPath $map7mComparator))   { throw "MAP-7M comparator missing" }
Write-Output "OK: scripts\inspect-build42-known-working-map-contract.ps1"
if (-not (Test-Path -LiteralPath $map7mPacketScript)) { throw "MAP-7M packet script missing" }
Write-Output "OK: scripts\prepare-build42-map7m-known-working-contract-packet.ps1"
if (-not (Test-Path -LiteralPath $map7mTests))        { throw "MAP-7M tests missing" }
Write-Output "OK: scripts\test-build42-map7m-known-working-contract.ps1"
$map7mDocContent = Get-Content -LiteralPath $map7mDoc -Raw
if ($map7mDocContent -notmatch 'MAP7F_VARIANT_H_MAP_FOLDER_SCAN_EMPTY') { throw "MAP-7M doc missing MAP7F_VARIANT_H_MAP_FOLDER_SCAN_EMPTY" }
Write-Output "OK: doc contains MAP7F_VARIANT_H_MAP_FOLDER_SCAN_EMPTY"
if ($map7mDocContent -notmatch 'VARIANTS_ABCDEFGH_EXHAUSTED') { throw "MAP-7M doc missing VARIANTS_ABCDEFGH_EXHAUSTED" }
Write-Output "OK: doc contains VARIANTS_ABCDEFGH_EXHAUSTED"
if ($map7mDocContent -notmatch 'KNOWN_WORKING_MAP_COMPARATOR_REQUIRED') { throw "MAP-7M doc missing KNOWN_WORKING_MAP_COMPARATOR_REQUIRED" }
Write-Output "OK: doc contains KNOWN_WORKING_MAP_COMPARATOR_REQUIRED"
if ($map7mDocContent -notmatch 'PUBLIC_PLAYABLE_CLAIM_ALLOWED=false') { throw "MAP-7M doc missing PUBLIC_PLAYABLE_CLAIM_ALLOWED=false" }
Write-Output "OK: doc contains PUBLIC_PLAYABLE_CLAIM_ALLOWED=false"
if ($map7mDocContent -notmatch 'LOAD_TEST_NOT_PERFORMED') { throw "MAP-7M doc missing LOAD_TEST_NOT_PERFORMED" }
Write-Output "OK: doc contains LOAD_TEST_NOT_PERFORMED"
$map7mCompContent = Get-Content -LiteralPath $map7mComparator -Raw
if ($map7mCompContent -notmatch '\.local') { throw "MAP-7M comparator missing .local refusal" }
Write-Output "OK: comparator contains .local refusal language"
if ($map7mCompContent -notmatch 'mod_info_fields_in_reference_not_candidate') { throw "MAP-7M comparator missing field gap detection" }
Write-Output "OK: comparator contains mod_info_fields_in_reference_not_candidate"
$map7mPacketContent = Get-Content -LiteralPath $map7mPacketScript -Raw
if ($map7mPacketContent -notmatch '\.local') { throw "MAP-7M packet script missing .local refusal" }
Write-Output "OK: packet script contains .local refusal language"

Write-Output ""
Write-Output "--- MAP-7M known-working contract tests ---"
& powershell -ExecutionPolicy Bypass -File $map7mTests
if ($LASTEXITCODE -ne 0) { throw "MAP-7M known-working contract tests failed." }

Write-Output ""
Write-Output "--- MAP-7L Variant G and common layout pivot ---"
$map7lDoc          = Join-Path $repoRoot 'docs\MAP_7L_VARIANT_G_AND_COMMON_LAYOUT_PIVOT.md'
$map7lPacketScript = Join-Path $repoRoot 'scripts\prepare-build42-map7l-common-layout-experiment-packet.ps1'
$map7lTests        = Join-Path $repoRoot 'scripts\test-build42-map7l-common-layout-experiment.ps1'
if (-not (Test-Path -LiteralPath $map7lDoc))          { throw "MAP-7L doc missing" }
Write-Output "OK: docs\MAP_7L_VARIANT_G_AND_COMMON_LAYOUT_PIVOT.md"
if (-not (Test-Path -LiteralPath $map7lPacketScript)) { throw "MAP-7L packet script missing" }
Write-Output "OK: scripts\prepare-build42-map7l-common-layout-experiment-packet.ps1"
if (-not (Test-Path -LiteralPath $map7lTests))        { throw "MAP-7L tests missing" }
Write-Output "OK: scripts\test-build42-map7l-common-layout-experiment.ps1"
$map7lDocContent = Get-Content -LiteralPath $map7lDoc -Raw
if ($map7lDocContent -notmatch 'MAP7F_VARIANT_G_MAP_FOLDER_SCAN_EMPTY') { throw "MAP-7L doc missing MAP7F_VARIANT_G_MAP_FOLDER_SCAN_EMPTY" }
Write-Output "OK: doc contains MAP7F_VARIANT_G_MAP_FOLDER_SCAN_EMPTY"
if ($map7lDocContent -notmatch 'VARIANTS_ABCDEFG_EXHAUSTED') { throw "MAP-7L doc missing VARIANTS_ABCDEFG_EXHAUSTED" }
Write-Output "OK: doc contains VARIANTS_ABCDEFG_EXHAUSTED"
if ($map7lDocContent -notmatch 'COMMON_LAYOUT_PIVOT') { throw "MAP-7L doc missing COMMON_LAYOUT_PIVOT" }
Write-Output "OK: doc contains COMMON_LAYOUT_PIVOT"
if ($map7lDocContent -notmatch 'PUBLIC_PLAYABLE_CLAIM_ALLOWED=false') { throw "MAP-7L doc missing PUBLIC_PLAYABLE_CLAIM_ALLOWED=false" }
Write-Output "OK: doc contains PUBLIC_PLAYABLE_CLAIM_ALLOWED=false"
if ($map7lDocContent -notmatch 'LOAD_TEST_NOT_PERFORMED') { throw "MAP-7L doc missing LOAD_TEST_NOT_PERFORMED" }
Write-Output "OK: doc contains LOAD_TEST_NOT_PERFORMED"
$map7lPacketContent = Get-Content -LiteralPath $map7lPacketScript -Raw
if ($map7lPacketContent -notmatch '\.local') { throw "MAP-7L packet script missing .local refusal" }
Write-Output "OK: packet script contains .local refusal language"
$map7lDiscContent = Get-Content -LiteralPath (Join-Path $repoRoot 'scripts\inspect-build42-map-discovery-path.ps1') -Raw
if ($map7lDiscContent -notmatch 'has_common_media_maps') { throw "MAP-7L inspector missing has_common_media_maps" }
Write-Output "OK: discovery inspector contains has_common_media_maps"
if ($map7lDiscContent -notmatch 'variant_g_result') { throw "MAP-7L inspector missing variant_g_result" }
Write-Output "OK: discovery inspector contains variant_g_result"
if ($map7lDiscContent -notmatch 'variants_abcdefg_exhausted') { throw "MAP-7L inspector missing variants_abcdefg_exhausted" }
Write-Output "OK: discovery inspector contains variants_abcdefg_exhausted"

Write-Output ""
Write-Output "--- MAP-7L common layout experiment tests ---"
& powershell -ExecutionPolicy Bypass -File $map7lTests
if ($LASTEXITCODE -ne 0) { throw "MAP-7L common layout experiment tests failed." }

Write-Output ""
Write-Output "--- MAP-7K Variant F folder/id failure ---"
$map7kDoc          = Join-Path $repoRoot 'docs\MAP_7K_VARIANT_F_FOLDER_ID_FAILURE.md'
$map7kPacketScript = Join-Path $repoRoot 'scripts\prepare-build42-map7k-modinfo-map-field-experiment-packet.ps1'
$map7kTests        = Join-Path $repoRoot 'scripts\test-build42-map7k-modinfo-map-field-experiment.ps1'
if (-not (Test-Path -LiteralPath $map7kDoc))          { throw "MAP-7K doc missing" }
Write-Output "OK: docs\MAP_7K_VARIANT_F_FOLDER_ID_FAILURE.md"
if (-not (Test-Path -LiteralPath $map7kPacketScript)) { throw "MAP-7K packet script missing" }
Write-Output "OK: scripts\prepare-build42-map7k-modinfo-map-field-experiment-packet.ps1"
if (-not (Test-Path -LiteralPath $map7kTests))        { throw "MAP-7K tests missing" }
Write-Output "OK: scripts\test-build42-map7k-modinfo-map-field-experiment.ps1"
$map7kDocContent = Get-Content -LiteralPath $map7kDoc -Raw
if ($map7kDocContent -notmatch 'MAP7F_VARIANT_F_MAP_FOLDER_SCAN_EMPTY') { throw "MAP-7K doc missing MAP7F_VARIANT_F_MAP_FOLDER_SCAN_EMPTY" }
Write-Output "OK: doc contains MAP7F_VARIANT_F_MAP_FOLDER_SCAN_EMPTY"
if ($map7kDocContent -notmatch 'H5_FOLDER_ID_ALIGNMENT_RULED_OUT') { throw "MAP-7K doc missing H5_FOLDER_ID_ALIGNMENT_RULED_OUT" }
Write-Output "OK: doc contains H5_FOLDER_ID_ALIGNMENT_RULED_OUT"
if ($map7kDocContent -notmatch 'H8_MOD_INFO_MAP_FIELD_RECOMMENDED') { throw "MAP-7K doc missing H8_MOD_INFO_MAP_FIELD_RECOMMENDED" }
Write-Output "OK: doc contains H8_MOD_INFO_MAP_FIELD_RECOMMENDED"
if ($map7kDocContent -notmatch 'PUBLIC_PLAYABLE_CLAIM_ALLOWED=false') { throw "MAP-7K doc missing PUBLIC_PLAYABLE_CLAIM_ALLOWED=false" }
Write-Output "OK: doc contains PUBLIC_PLAYABLE_CLAIM_ALLOWED=false"
if ($map7kDocContent -notmatch 'LOAD_TEST_NOT_PERFORMED') { throw "MAP-7K doc missing LOAD_TEST_NOT_PERFORMED" }
Write-Output "OK: doc contains LOAD_TEST_NOT_PERFORMED"
$map7kPacketContent = Get-Content -LiteralPath $map7kPacketScript -Raw
if ($map7kPacketContent -notmatch '\.local') { throw "MAP-7K packet script missing .local refusal" }
Write-Output "OK: packet script contains .local refusal language"
$map7kInspContent = Get-Content -LiteralPath (Join-Path $repoRoot 'scripts\inspect-build42-map-metadata-contract.ps1') -Raw
if ($map7kInspContent -notmatch 'mod_info_has_map_field') { throw "MAP-7K inspector missing mod_info_has_map_field" }
Write-Output "OK: inspector contains mod_info_has_map_field"
if ($map7kInspContent -notmatch 'h8_mod_info_map_field_recommended') { throw "MAP-7K inspector missing h8_mod_info_map_field_recommended" }
Write-Output "OK: inspector contains h8_mod_info_map_field_recommended"

Write-Output ""
Write-Output "--- MAP-7K modinfo map field experiment tests ---"
& powershell -ExecutionPolicy Bypass -File $map7kTests
if ($LASTEXITCODE -ne 0) { throw "MAP-7K modinfo map field experiment tests failed." }

Write-Output ""
Write-Output "--- MAP-7J Variant E metadata contract failure ---"
$map7jDoc           = Join-Path $repoRoot 'docs\MAP_7J_VARIANT_E_METADATA_CONTRACT_FAILURE.md'
$map7jInspector     = Join-Path $repoRoot 'scripts\inspect-build42-map-metadata-contract.ps1'
$map7jPacketScript  = Join-Path $repoRoot 'scripts\prepare-build42-map7j-metadata-contract-packet.ps1'
$map7jTests         = Join-Path $repoRoot 'scripts\test-build42-map7j-metadata-contract.ps1'
if (-not (Test-Path -LiteralPath $map7jDoc))          { throw "MAP-7J doc missing" }
Write-Output "OK: docs\MAP_7J_VARIANT_E_METADATA_CONTRACT_FAILURE.md"
if (-not (Test-Path -LiteralPath $map7jInspector))    { throw "MAP-7J inspector missing" }
Write-Output "OK: scripts\inspect-build42-map-metadata-contract.ps1"
if (-not (Test-Path -LiteralPath $map7jPacketScript)) { throw "MAP-7J packet script missing" }
Write-Output "OK: scripts\prepare-build42-map7j-metadata-contract-packet.ps1"
if (-not (Test-Path -LiteralPath $map7jTests))        { throw "MAP-7J tests missing" }
Write-Output "OK: scripts\test-build42-map7j-metadata-contract.ps1"
$map7jDocContent = Get-Content -LiteralPath $map7jDoc -Raw
if ($map7jDocContent -notmatch 'MAP7F_VARIANT_E_MAP_FOLDER_SCAN_EMPTY') { throw "MAP-7J doc missing MAP7F_VARIANT_E_MAP_FOLDER_SCAN_EMPTY" }
Write-Output "OK: doc contains MAP7F_VARIANT_E_MAP_FOLDER_SCAN_EMPTY"
if ($map7jDocContent -notmatch 'VARIANTS_ABCDE_EXHAUSTED') { throw "MAP-7J doc missing VARIANTS_ABCDE_EXHAUSTED" }
Write-Output "OK: doc contains VARIANTS_ABCDE_EXHAUSTED"
if ($map7jDocContent -notmatch 'METADATA_CONTRACT_FOCUS') { throw "MAP-7J doc missing METADATA_CONTRACT_FOCUS" }
Write-Output "OK: doc contains METADATA_CONTRACT_FOCUS"
if ($map7jDocContent -notmatch 'PUBLIC_PLAYABLE_CLAIM_ALLOWED=false') { throw "MAP-7J doc missing PUBLIC_PLAYABLE_CLAIM_ALLOWED=false" }
Write-Output "OK: doc contains PUBLIC_PLAYABLE_CLAIM_ALLOWED=false"
if ($map7jDocContent -notmatch 'LOAD_TEST_NOT_PERFORMED') { throw "MAP-7J doc missing LOAD_TEST_NOT_PERFORMED" }
Write-Output "OK: doc contains LOAD_TEST_NOT_PERFORMED"
$map7jInspContent = Get-Content -LiteralPath $map7jInspector -Raw
if ($map7jInspContent -notmatch '\.local') { throw "MAP-7J inspector missing .local refusal" }
Write-Output "OK: inspector contains .local refusal language"
if ($map7jInspContent -notmatch 'metadata_contract_focus') { throw "MAP-7J inspector missing metadata_contract_focus field" }
Write-Output "OK: inspector contains metadata_contract_focus field"
$map7jPacketContent = Get-Content -LiteralPath $map7jPacketScript -Raw
if ($map7jPacketContent -notmatch '\.local') { throw "MAP-7J packet script missing .local refusal" }
Write-Output "OK: packet script contains .local refusal language"
$map7jAnalyzerContent = Get-Content -LiteralPath (Join-Path $repoRoot 'scripts\inspect-build42-map7d-load-result.ps1') -Raw
if ($map7jAnalyzerContent -notmatch 'Failed to find any') { throw "MAP-7J analyzer missing lotheader discovery failure detection" }
Write-Output "OK: analyzer contains lotheader discovery failure detection"
if ($map7jAnalyzerContent -notmatch 'MAP_FOLDER_SCAN_FOUND_BUT_LOTHEADER_FILES_MISSING') { throw "MAP-7J analyzer missing MAP_FOLDER_SCAN_FOUND_BUT_LOTHEADER_FILES_MISSING classification" }
Write-Output "OK: analyzer contains MAP_FOLDER_SCAN_FOUND_BUT_LOTHEADER_FILES_MISSING"

Write-Output ""
Write-Output "--- MAP-7J metadata contract tests ---"
& powershell -ExecutionPolicy Bypass -File $map7jTests
if ($LASTEXITCODE -ne 0) { throw "MAP-7J metadata contract tests failed." }

Write-Output ""
Write-Output "--- MAP-7I Variant D root media failure ---"
$map7iDoc          = Join-Path $repoRoot 'docs\MAP_7I_VARIANT_D_ROOT_MEDIA_FAILURE.md'
$map7iPacketScript = Join-Path $repoRoot 'scripts\prepare-build42-map7i-root-modinfo-experiment-packet.ps1'
$map7iTests        = Join-Path $repoRoot 'scripts\test-build42-map7i-root-modinfo-experiment.ps1'
if (-not (Test-Path -LiteralPath $map7iDoc))          { throw "MAP-7I doc missing" }
Write-Output "OK: docs\MAP_7I_VARIANT_D_ROOT_MEDIA_FAILURE.md"
if (-not (Test-Path -LiteralPath $map7iPacketScript)) { throw "MAP-7I packet script missing" }
Write-Output "OK: scripts\prepare-build42-map7i-root-modinfo-experiment-packet.ps1"
if (-not (Test-Path -LiteralPath $map7iTests))        { throw "MAP-7I tests missing" }
Write-Output "OK: scripts\test-build42-map7i-root-modinfo-experiment.ps1"
$map7iDocContent = Get-Content -LiteralPath $map7iDoc -Raw
if ($map7iDocContent -notmatch 'MAP7F_VARIANT_D_MAP_FOLDER_SCAN_EMPTY') { throw "MAP-7I doc missing MAP7F_VARIANT_D_MAP_FOLDER_SCAN_EMPTY" }
Write-Output "OK: doc contains MAP7F_VARIANT_D_MAP_FOLDER_SCAN_EMPTY"
if ($map7iDocContent -notmatch 'ROOT_MEDIA_MAPS_ALONE_INSUFFICIENT') { throw "MAP-7I doc missing ROOT_MEDIA_MAPS_ALONE_INSUFFICIENT" }
Write-Output "OK: doc contains ROOT_MEDIA_MAPS_ALONE_INSUFFICIENT"
if ($map7iDocContent -notmatch 'EXPERIMENT_E_ROOT_MOD_INFO_RECOMMENDED') { throw "MAP-7I doc missing EXPERIMENT_E_ROOT_MOD_INFO_RECOMMENDED" }
Write-Output "OK: doc contains EXPERIMENT_E_ROOT_MOD_INFO_RECOMMENDED"
if ($map7iDocContent -notmatch 'PUBLIC_PLAYABLE_CLAIM_ALLOWED=false') { throw "MAP-7I doc missing PUBLIC_PLAYABLE_CLAIM_ALLOWED=false" }
Write-Output "OK: doc contains PUBLIC_PLAYABLE_CLAIM_ALLOWED=false"
if ($map7iDocContent -notmatch 'LOAD_TEST_NOT_PERFORMED') { throw "MAP-7I doc missing LOAD_TEST_NOT_PERFORMED" }
Write-Output "OK: doc contains LOAD_TEST_NOT_PERFORMED"
$map7iPacketContent = Get-Content -LiteralPath $map7iPacketScript -Raw
if ($map7iPacketContent -notmatch '\.local') { throw "MAP-7I packet script missing .local refusal" }
Write-Output "OK: packet script contains .local refusal language"
$map7iInspContent = Get-Content -LiteralPath (Join-Path $repoRoot 'scripts\inspect-build42-map-discovery-path.ps1') -Raw
if ($map7iInspContent -notmatch 'has_dual_mod_info_layout') { throw "MAP-7I inspector missing has_dual_mod_info_layout field" }
Write-Output "OK: inspector contains has_dual_mod_info_layout"
if ($map7iInspContent -notmatch 'experiment_e_root_mod_info_recommended') { throw "MAP-7I inspector missing experiment_e_root_mod_info_recommended" }
Write-Output "OK: inspector contains experiment_e_root_mod_info_recommended"

Write-Output ""
Write-Output "--- MAP-7I root modinfo experiment tests ---"
& powershell -ExecutionPolicy Bypass -File $map7iTests
if ($LASTEXITCODE -ne 0) { throw "MAP-7I root modinfo experiment tests failed." }

Write-Output ""
Write-Output "--- MAP-7H Variant B/C and discovery path ---"
$map7hDoc           = Join-Path $repoRoot 'docs\MAP_7H_VARIANT_BC_AND_DISCOVERY_PATH.md'
$map7hInspector     = Join-Path $repoRoot 'scripts\inspect-build42-map-discovery-path.ps1'
$map7hPacketScript  = Join-Path $repoRoot 'scripts\prepare-build42-map7h-discovery-path-packet.ps1'
$map7hTests         = Join-Path $repoRoot 'scripts\test-build42-map7h-discovery-path.ps1'
if (-not (Test-Path -LiteralPath $map7hDoc))          { throw "MAP-7H doc missing" }
Write-Output "OK: docs\MAP_7H_VARIANT_BC_AND_DISCOVERY_PATH.md"
if (-not (Test-Path -LiteralPath $map7hInspector))    { throw "MAP-7H inspector script missing" }
Write-Output "OK: scripts\inspect-build42-map-discovery-path.ps1"
if (-not (Test-Path -LiteralPath $map7hPacketScript)) { throw "MAP-7H packet script missing" }
Write-Output "OK: scripts\prepare-build42-map7h-discovery-path-packet.ps1"
if (-not (Test-Path -LiteralPath $map7hTests))        { throw "MAP-7H tests missing" }
Write-Output "OK: scripts\test-build42-map7h-discovery-path.ps1"
$map7hDocContent = Get-Content -LiteralPath $map7hDoc -Raw
if ($map7hDocContent -notmatch 'MAP7F_VARIANT_B_MAP_FOLDER_SCAN_EMPTY') { throw "MAP-7H doc missing MAP7F_VARIANT_B_MAP_FOLDER_SCAN_EMPTY" }
Write-Output "OK: doc contains MAP7F_VARIANT_B_MAP_FOLDER_SCAN_EMPTY"
if ($map7hDocContent -notmatch 'MAP7F_VARIANT_C_MAP_FOLDER_SCAN_EMPTY') { throw "MAP-7H doc missing MAP7F_VARIANT_C_MAP_FOLDER_SCAN_EMPTY" }
Write-Output "OK: doc contains MAP7F_VARIANT_C_MAP_FOLDER_SCAN_EMPTY"
if ($map7hDocContent -notmatch 'MAP_LINE_VARIANTS_EXHAUSTED') { throw "MAP-7H doc missing MAP_LINE_VARIANTS_EXHAUSTED" }
Write-Output "OK: doc contains MAP_LINE_VARIANTS_EXHAUSTED"
if ($map7hDocContent -notmatch 'PUBLIC_PLAYABLE_CLAIM_ALLOWED=false') { throw "MAP-7H doc missing PUBLIC_PLAYABLE_CLAIM_ALLOWED=false" }
Write-Output "OK: doc contains PUBLIC_PLAYABLE_CLAIM_ALLOWED=false"
if ($map7hDocContent -notmatch 'LOAD_TEST_NOT_PERFORMED') { throw "MAP-7H doc missing LOAD_TEST_NOT_PERFORMED" }
Write-Output "OK: doc contains LOAD_TEST_NOT_PERFORMED"
$map7hInspContent = Get-Content -LiteralPath $map7hInspector -Raw
if ($map7hInspContent -notmatch '\.local') { throw "MAP-7H inspector missing .local refusal" }
Write-Output "OK: inspector contains .local refusal language"
if ($map7hInspContent -notmatch 'has_versioned_42_media_maps') { throw "MAP-7H inspector missing has_versioned_42_media_maps field" }
Write-Output "OK: inspector contains has_versioned_42_media_maps"
$map7hPacketContent = Get-Content -LiteralPath $map7hPacketScript -Raw
if ($map7hPacketContent -notmatch '\.local') { throw "MAP-7H packet script missing .local refusal" }
Write-Output "OK: packet script contains .local refusal language"

Write-Output ""
Write-Output "--- MAP-7H discovery path tests ---"
& powershell -ExecutionPolicy Bypass -File $map7hTests
if ($LASTEXITCODE -ne 0) { throw "MAP-7H discovery path tests failed." }

Write-Output ""
Write-Output "--- MAP-7G Variant A registration failure ---"
$map7gDoc    = Join-Path $repoRoot 'docs\MAP_7G_VARIANT_A_REGISTRATION_FAILURE.md'
$map7gTests  = Join-Path $repoRoot 'scripts\test-build42-map7g-variant-a-failure.ps1'
if (-not (Test-Path -LiteralPath $map7gDoc))   { throw "MAP-7G doc missing" }
Write-Output "OK: docs\MAP_7G_VARIANT_A_REGISTRATION_FAILURE.md"
if (-not (Test-Path -LiteralPath $map7gTests)) { throw "MAP-7G tests missing" }
Write-Output "OK: scripts\test-build42-map7g-variant-a-failure.ps1"
$map7gDocContent = Get-Content -LiteralPath $map7gDoc -Raw
if ($map7gDocContent -notmatch 'MAP7F_VARIANT_A_MAP_FOLDER_SCAN_EMPTY') { throw "MAP-7G doc missing MAP7F_VARIANT_A_MAP_FOLDER_SCAN_EMPTY" }
Write-Output "OK: doc contains MAP7F_VARIANT_A_MAP_FOLDER_SCAN_EMPTY"
if ($map7gDocContent -notmatch 'CANDIDATE_NOT_IN_MAP_FOLDER_LIST') { throw "MAP-7G doc missing CANDIDATE_NOT_IN_MAP_FOLDER_LIST" }
Write-Output "OK: doc contains CANDIDATE_NOT_IN_MAP_FOLDER_LIST"
if ($map7gDocContent -notmatch 'PUBLIC_PLAYABLE_CLAIM_ALLOWED=false') { throw "MAP-7G doc missing PUBLIC_PLAYABLE_CLAIM_ALLOWED=false" }
Write-Output "OK: doc contains PUBLIC_PLAYABLE_CLAIM_ALLOWED=false"
if ($map7gDocContent -notmatch 'LOAD_TEST_NOT_PERFORMED') { throw "MAP-7G doc missing LOAD_TEST_NOT_PERFORMED" }
Write-Output "OK: doc contains LOAD_TEST_NOT_PERFORMED"
$map7gAnalyzerContent = Get-Content -LiteralPath (Join-Path $repoRoot 'scripts\inspect-build42-map7d-load-result.ps1') -Raw
if ($map7gAnalyzerContent -notmatch 'ExpectedMapId') { throw "MAP-7G analyzer missing ExpectedMapId param" }
Write-Output "OK: analyzer contains ExpectedMapId parameter"
if ($map7gAnalyzerContent -notmatch 'VariantLabel') { throw "MAP-7G analyzer missing VariantLabel param" }
Write-Output "OK: analyzer contains VariantLabel parameter"

Write-Output ""
Write-Output "--- MAP-7G variant A failure tests ---"
& powershell -ExecutionPolicy Bypass -File $map7gTests
if ($LASTEXITCODE -ne 0) { throw "MAP-7G variant A failure tests failed." }

Write-Output ""
Write-Output "--- MAP-7F map folder registration diagnostic ---"
$map7fDoc          = Join-Path $repoRoot 'docs\MAP_7F_MAP_FOLDER_REGISTRATION_DIAGNOSTIC.md'
$map7fPacketScript = Join-Path $repoRoot 'scripts\prepare-build42-map7f-registration-diagnostic-packet.ps1'
$map7fTests        = Join-Path $repoRoot 'scripts\test-build42-map7f-registration-diagnostic.ps1'
if (-not (Test-Path -LiteralPath $map7fDoc))          { throw "MAP-7F doc missing" }
Write-Output "OK: docs\MAP_7F_MAP_FOLDER_REGISTRATION_DIAGNOSTIC.md"
if (-not (Test-Path -LiteralPath $map7fPacketScript)) { throw "MAP-7F packet script missing" }
Write-Output "OK: scripts\prepare-build42-map7f-registration-diagnostic-packet.ps1"
if (-not (Test-Path -LiteralPath $map7fTests))        { throw "MAP-7F tests missing" }
Write-Output "OK: scripts\test-build42-map7f-registration-diagnostic.ps1"
$map7fDocContent = Get-Content -LiteralPath $map7fDoc -Raw
if ($map7fDocContent -notmatch 'MAP_FOLDER_SCAN_EMPTY_CONFIRMED') { throw "MAP-7F doc missing MAP_FOLDER_SCAN_EMPTY_CONFIRMED" }
Write-Output "OK: doc contains MAP_FOLDER_SCAN_EMPTY_CONFIRMED"
if ($map7fDocContent -notmatch 'ANALYZER_TIMESTAMPED_LOG_BUG_FIXED') { throw "MAP-7F doc missing ANALYZER_TIMESTAMPED_LOG_BUG_FIXED" }
Write-Output "OK: doc contains ANALYZER_TIMESTAMPED_LOG_BUG_FIXED"
if ($map7fDocContent -notmatch 'PUBLIC_PLAYABLE_CLAIM_ALLOWED=false') { throw "MAP-7F doc missing PUBLIC_PLAYABLE_CLAIM_ALLOWED=false" }
Write-Output "OK: doc contains PUBLIC_PLAYABLE_CLAIM_ALLOWED=false"
if ($map7fDocContent -notmatch 'LOAD_TEST_NOT_PERFORMED') { throw "MAP-7F doc missing LOAD_TEST_NOT_PERFORMED" }
Write-Output "OK: doc contains LOAD_TEST_NOT_PERFORMED"
$map7fPacketContent = Get-Content -LiteralPath $map7fPacketScript -Raw
if ($map7fPacketContent -notmatch '\.local') { throw "MAP-7F packet script missing .local refusal" }
Write-Output "OK: packet script contains .local refusal language"

Write-Output ""
Write-Output "--- MAP-7F registration diagnostic tests ---"
& powershell -ExecutionPolicy Bypass -File $map7fTests
if ($LASTEXITCODE -ne 0) { throw "MAP-7F registration diagnostic tests failed." }

Write-Output ""
Write-Output "--- MAP-7E empty world diagnostics ---"
$map7eDoc          = Join-Path $repoRoot 'docs\MAP_7E_EMPTY_WORLD_MAP_REGISTRATION_DIAGNOSTICS.md'
$map7eAnalyzer     = Join-Path $repoRoot 'scripts\inspect-build42-map7d-load-result.ps1'
$map7ePacketScript = Join-Path $repoRoot 'scripts\prepare-build42-map7e-diagnostics-packet.ps1'
$map7eTests        = Join-Path $repoRoot 'scripts\test-build42-map7e-diagnostics.ps1'
if (-not (Test-Path -LiteralPath $map7eDoc))          { throw "MAP-7E doc missing" }
Write-Output "OK: docs\MAP_7E_EMPTY_WORLD_MAP_REGISTRATION_DIAGNOSTICS.md"
if (-not (Test-Path -LiteralPath $map7eAnalyzer))     { throw "MAP-7E analyzer script missing" }
Write-Output "OK: scripts\inspect-build42-map7d-load-result.ps1"
if (-not (Test-Path -LiteralPath $map7ePacketScript)) { throw "MAP-7E packet script missing" }
Write-Output "OK: scripts\prepare-build42-map7e-diagnostics-packet.ps1"
if (-not (Test-Path -LiteralPath $map7eTests))        { throw "MAP-7E tests missing" }
Write-Output "OK: scripts\test-build42-map7e-diagnostics.ps1"
$map7eDocContent = Get-Content -LiteralPath $map7eDoc -Raw
if ($map7eDocContent -notmatch 'MAP7D_LOAD_TEST_PARTIAL_PASS_IN_GAME_EMPTY_WORLD') { throw "MAP-7E doc missing MAP7D_LOAD_TEST_PARTIAL_PASS_IN_GAME_EMPTY_WORLD" }
Write-Output "OK: doc contains MAP7D_LOAD_TEST_PARTIAL_PASS_IN_GAME_EMPTY_WORLD"
if ($map7eDocContent -notmatch 'LOAD_TEST_NOT_PERFORMED') { throw "MAP-7E doc missing LOAD_TEST_NOT_PERFORMED" }
Write-Output "OK: doc contains LOAD_TEST_NOT_PERFORMED"
if ($map7eDocContent -notmatch 'PUBLIC_PLAYABLE_CLAIM_ALLOWED=false') { throw "MAP-7E doc missing PUBLIC_PLAYABLE_CLAIM_ALLOWED=false" }
Write-Output "OK: doc contains PUBLIC_PLAYABLE_CLAIM_ALLOWED=false"
$map7eAnalyzerContent = Get-Content -LiteralPath $map7eAnalyzer -Raw
if ($map7eAnalyzerContent -notmatch '\.local') { throw "MAP-7E analyzer missing .local refusal" }
Write-Output "OK: analyzer script contains .local refusal language"

Write-Output ""
Write-Output "--- MAP-7E diagnostics tests ---"
& powershell -ExecutionPolicy Bypass -File $map7eTests
if ($LASTEXITCODE -ne 0) { throw "MAP-7E diagnostics tests failed." }

Write-Output ""
Write-Output "--- MAP-7D timeout and Lua encoding fix ---"
$map7dDoc          = Join-Path $repoRoot 'docs\MAP_7D_TIMEOUT_AND_LUA_ENCODING_FIX.md'
$map7dPacketScript = Join-Path $repoRoot 'scripts\prepare-build42-metadata-v4-load-test-packet.ps1'
$map7dPacketTests  = Join-Path $repoRoot 'scripts\test-build42-metadata-v4-load-test-packet.ps1'
if (-not (Test-Path -LiteralPath $map7dDoc))          { throw "MAP-7D doc missing" }
Write-Output "OK: docs\MAP_7D_TIMEOUT_AND_LUA_ENCODING_FIX.md"
if (-not (Test-Path -LiteralPath $map7dPacketScript)) { throw "MAP-7D packet script missing" }
Write-Output "OK: scripts\prepare-build42-metadata-v4-load-test-packet.ps1"
if (-not (Test-Path -LiteralPath $map7dPacketTests))  { throw "MAP-7D packet tests missing" }
Write-Output "OK: scripts\test-build42-metadata-v4-load-test-packet.ps1"
$map7dDocContent = Get-Content -LiteralPath $map7dDoc -Raw
if ($map7dDocContent -notmatch 'LOAD_TEST_FAIL_TIMEOUT_PLAYER_DATA') { throw "MAP-7D doc missing LOAD_TEST_FAIL_TIMEOUT_PLAYER_DATA" }
Write-Output "OK: doc contains LOAD_TEST_FAIL_TIMEOUT_PLAYER_DATA"
if ($map7dDocContent -notmatch 'OBJECTS_LUA_NO_BOM_FIX_APPLIED') { throw "MAP-7D doc missing OBJECTS_LUA_NO_BOM_FIX_APPLIED" }
Write-Output "OK: doc contains OBJECTS_LUA_NO_BOM_FIX_APPLIED"
if ($map7dDocContent -notmatch 'PLAYABLE_EXPORT_CLAIM_ALLOWED=false') { throw "MAP-7D doc missing PLAYABLE_EXPORT_CLAIM_ALLOWED=false" }
Write-Output "OK: doc contains PLAYABLE_EXPORT_CLAIM_ALLOWED=false"
$map7dPacketScriptContent = Get-Content -LiteralPath $map7dPacketScript -Raw
if ($map7dPacketScriptContent -notmatch '\.local') { throw "MAP-7D packet script missing .local refusal" }
Write-Output "OK: packet script contains .local refusal language"

Write-Output ""
Write-Output "--- MAP-7D metadata v4 packet tests ---"
& powershell -ExecutionPolicy Bypass -File $map7dPacketTests
if ($LASTEXITCODE -ne 0) { throw "MAP-7D metadata v4 packet tests failed." }

Write-Output ""
Write-Output "--- MAP-7C candidate Lua metadata fix ---"
$map7cDoc          = Join-Path $repoRoot 'docs\MAP_7C_OBJECTS_LUA_SPAWN_METADATA_FIX.md'
$map7cPacketScript = Join-Path $repoRoot 'scripts\prepare-build42-metadata-v3-load-test-packet.ps1'
$map7cPacketTests  = Join-Path $repoRoot 'scripts\test-build42-metadata-v3-load-test-packet.ps1'
if (-not (Test-Path -LiteralPath $map7cDoc))          { throw "MAP-7C doc missing" }
Write-Output "OK: docs\MAP_7C_OBJECTS_LUA_SPAWN_METADATA_FIX.md"
if (-not (Test-Path -LiteralPath $map7cPacketScript)) { throw "MAP-7C packet script missing" }
Write-Output "OK: scripts\prepare-build42-metadata-v3-load-test-packet.ps1"
if (-not (Test-Path -LiteralPath $map7cPacketTests))  { throw "MAP-7C packet tests missing" }
Write-Output "OK: scripts\test-build42-metadata-v3-load-test-packet.ps1"
$map7cDocContent = Get-Content -LiteralPath $map7cDoc -Raw
if ($map7cDocContent -notmatch 'OBJECTS_LUA_FIXED_COMMENT_ONLY') { throw "MAP-7C doc missing OBJECTS_LUA_FIXED_COMMENT_ONLY" }
Write-Output "OK: doc contains OBJECTS_LUA_FIXED_COMMENT_ONLY"
if ($map7cDocContent -notmatch 'LOAD_TEST_NOT_PERFORMED') { throw "MAP-7C doc missing LOAD_TEST_NOT_PERFORMED" }
Write-Output "OK: doc contains LOAD_TEST_NOT_PERFORMED"
if ($map7cDocContent -notmatch 'PLAYABLE_EXPORT_CLAIM_ALLOWED=false') { throw "MAP-7C doc missing PLAYABLE_EXPORT_CLAIM_ALLOWED=false" }
Write-Output "OK: doc contains PLAYABLE_EXPORT_CLAIM_ALLOWED=false"
$map7cPacketScriptContent = Get-Content -LiteralPath $map7cPacketScript -Raw
if ($map7cPacketScriptContent -notmatch '\.local') { throw "MAP-7C packet script missing .local refusal" }
Write-Output "OK: packet script contains .local refusal language"

Write-Output ""
Write-Output "--- MAP-7C metadata v3 packet tests ---"
& powershell -ExecutionPolicy Bypass -File $map7cPacketTests
if ($LASTEXITCODE -ne 0) { throw "MAP-7C metadata v3 packet tests failed." }

Write-Output ""
Write-Output "--- MAP-7B retest result and Lua metadata inspector ---"
$map7bDoc    = Join-Path $repoRoot 'docs\MAP_7B_LOTH_V3_RETEST_OBJECTS_LUA_FAILURE.md'
$map7bScript = Join-Path $repoRoot 'scripts\inspect-build42-candidate-lua-metadata.ps1'
$map7bTests  = Join-Path $repoRoot 'scripts\test-build42-candidate-lua-metadata.ps1'
if (-not (Test-Path -LiteralPath $map7bDoc))    { throw "MAP-7B doc missing" }
Write-Output "OK: docs\MAP_7B_LOTH_V3_RETEST_OBJECTS_LUA_FAILURE.md"
if (-not (Test-Path -LiteralPath $map7bScript)) { throw "MAP-7B script missing" }
Write-Output "OK: scripts\inspect-build42-candidate-lua-metadata.ps1"
if (-not (Test-Path -LiteralPath $map7bTests))  { throw "MAP-7B tests missing" }
Write-Output "OK: scripts\test-build42-candidate-lua-metadata.ps1"
$map7bDocContent = Get-Content -LiteralPath $map7bDoc -Raw
if ($map7bDocContent -notmatch 'MAP7A_CLEAN_RETEST_RECORDED') { throw "MAP-7B doc missing MAP7A_CLEAN_RETEST_RECORDED" }
Write-Output "OK: doc contains MAP7A_CLEAN_RETEST_RECORDED"
if ($map7bDocContent -notmatch 'OBJECTS_LUA_PRIMARY_BLOCKER') { throw "MAP-7B doc missing OBJECTS_LUA_PRIMARY_BLOCKER" }
Write-Output "OK: doc contains OBJECTS_LUA_PRIMARY_BLOCKER"
if ($map7bDocContent -notmatch 'PLAYABLE_EXPORT_CLAIM_ALLOWED=false') { throw "MAP-7B doc missing PLAYABLE_EXPORT_CLAIM_ALLOWED=false" }
Write-Output "OK: doc contains PLAYABLE_EXPORT_CLAIM_ALLOWED=false"
$map7bScriptContent = Get-Content -LiteralPath $map7bScript -Raw
if ($map7bScriptContent -notmatch '\.local') { throw "MAP-7B script missing .local refusal" }
Write-Output "OK: script contains .local refusal language"
if ($map7bScriptContent -notmatch 'candidate_files_read') { throw "MAP-7B script missing candidate_files_read sentinel" }
Write-Output "OK: script contains candidate_files_read sentinel"

Write-Output ""
Write-Output "--- MAP-7B Lua metadata tests ---"
& powershell -ExecutionPolicy Bypass -File $map7bTests
if ($LASTEXITCODE -ne 0) { throw "MAP-7B Lua metadata tests failed." }

Write-Output ""
Write-Output "--- MAP-7A LOTH v3 load test packet ---"
$map7aDoc    = Join-Path $repoRoot 'docs\MAP_7A_LOTH_V3_LOAD_TEST_PACKET.md'
$map7aScript = Join-Path $repoRoot 'scripts\prepare-build42-loth-v3-load-test-packet.ps1'
$map7aTests  = Join-Path $repoRoot 'scripts\test-build42-loth-v3-load-test-packet.ps1'
if (-not (Test-Path -LiteralPath $map7aDoc))    { throw "MAP-7A doc missing" }
Write-Output "OK: docs\MAP_7A_LOTH_V3_LOAD_TEST_PACKET.md"
if (-not (Test-Path -LiteralPath $map7aScript)) { throw "MAP-7A script missing" }
Write-Output "OK: scripts\prepare-build42-loth-v3-load-test-packet.ps1"
if (-not (Test-Path -LiteralPath $map7aTests))  { throw "MAP-7A tests missing" }
Write-Output "OK: scripts\test-build42-loth-v3-load-test-packet.ps1"
$map7aDocContent = Get-Content -LiteralPath $map7aDoc -Raw
if ($map7aDocContent -notmatch 'MAP7A_LOTH_V3_LOAD_TEST_PACKET_CREATED') { throw "MAP-7A doc missing MAP7A_LOTH_V3_LOAD_TEST_PACKET_CREATED" }
Write-Output "OK: doc contains MAP7A_LOTH_V3_LOAD_TEST_PACKET_CREATED"
if ($map7aDocContent -notmatch 'HUMAN_ONLY_COPY_REQUIRED') { throw "MAP-7A doc missing HUMAN_ONLY_COPY_REQUIRED" }
Write-Output "OK: doc contains HUMAN_ONLY_COPY_REQUIRED"
if ($map7aDocContent -notmatch 'PLAYABLE_EXPORT_CLAIM_ALLOWED=false') { throw "MAP-7A doc missing PLAYABLE_EXPORT_CLAIM_ALLOWED=false" }
Write-Output "OK: doc contains PLAYABLE_EXPORT_CLAIM_ALLOWED=false"
$map7aScriptContent = Get-Content -LiteralPath $map7aScript -Raw
if ($map7aScriptContent -notmatch '\.local') { throw "MAP-7A script missing .local refusal" }
Write-Output "OK: script contains .local refusal language"

Write-Output ""
Write-Output "--- MAP-7A load test packet tests ---"
& powershell -ExecutionPolicy Bypass -File $map7aTests
if ($LASTEXITCODE -ne 0) { throw "MAP-7A load test packet tests failed." }

Write-Output ""
Write-Output "--- MAP-6Y LOTH fixed 1048-byte block research ---"
$map6yDoc    = Join-Path $repoRoot 'docs\MAP_6Y_LOTH_FIXED_1048_BLOCK_RESEARCH.md'
$map6yScript = Join-Path $repoRoot 'scripts\analyze-build42-loth-fixed-1048-block.ps1'
$map6yTests  = Join-Path $repoRoot 'scripts\test-build42-loth-fixed-1048-block.ps1'
if (-not (Test-Path -LiteralPath $map6yDoc))    { throw "MAP-6Y doc missing" }
Write-Output "OK: docs\MAP_6Y_LOTH_FIXED_1048_BLOCK_RESEARCH.md"
if (-not (Test-Path -LiteralPath $map6yScript)) { throw "MAP-6Y script missing" }
Write-Output "OK: scripts\analyze-build42-loth-fixed-1048-block.ps1"
if (-not (Test-Path -LiteralPath $map6yTests))  { throw "MAP-6Y tests missing" }
Write-Output "OK: scripts\test-build42-loth-fixed-1048-block.ps1"
$map6yDocContent = Get-Content -LiteralPath $map6yDoc -Raw
if ($map6yDocContent -notmatch 'BUILD42_LOTH_FIXED_1048_BLOCK_ANALYSED') { throw "MAP-6Y doc missing BUILD42_LOTH_FIXED_1048_BLOCK_ANALYSED" }
Write-Output "OK: doc contains BUILD42_LOTH_FIXED_1048_BLOCK_ANALYSED"
if ($map6yDocContent -notmatch 'LOTH_TRAILING_BODY_FIXED_SIZE_FOR_SIMPLE_CELLS') { throw "MAP-6Y doc missing LOTH_TRAILING_BODY_FIXED_SIZE_FOR_SIMPLE_CELLS" }
Write-Output "OK: doc contains LOTH_TRAILING_BODY_FIXED_SIZE_FOR_SIMPLE_CELLS"
if ($map6yDocContent -notmatch 'PLAYABLE_EXPORT_CLAIM_ALLOWED=false') { throw "MAP-6Y doc missing PLAYABLE_EXPORT_CLAIM_ALLOWED=false" }
Write-Output "OK: doc contains PLAYABLE_EXPORT_CLAIM_ALLOWED=false"
$map6yScriptContent = Get-Content -LiteralPath $map6yScript -Raw
if ($map6yScriptContent -notmatch '\.local') { throw "MAP-6Y script missing .local refusal" }
Write-Output "OK: script contains .local refusal language"

Write-Output ""
Write-Output "--- MAP-6Y fixed 1048-byte block tests ---"
& powershell -ExecutionPolicy Bypass -File $map6yTests
if ($LASTEXITCODE -ne 0) { throw "MAP-6Y fixed 1048-byte block tests failed." }

Write-Output ""
Write-Output "--- MAP-6X LOTH per-entry record model research ---"
$map6xDoc    = Join-Path $repoRoot 'docs\MAP_6X_LOTH_PER_ENTRY_RECORD_MODEL_RESEARCH.md'
$map6xScript = Join-Path $repoRoot 'scripts\analyze-build42-loth-per-entry-record-model.ps1'
$map6xTests  = Join-Path $repoRoot 'scripts\test-build42-loth-per-entry-record-model.ps1'
if (-not (Test-Path -LiteralPath $map6xDoc))    { throw "MAP-6X doc missing" }
Write-Output "OK: docs\MAP_6X_LOTH_PER_ENTRY_RECORD_MODEL_RESEARCH.md"
if (-not (Test-Path -LiteralPath $map6xScript)) { throw "MAP-6X script missing" }
Write-Output "OK: scripts\analyze-build42-loth-per-entry-record-model.ps1"
if (-not (Test-Path -LiteralPath $map6xTests))  { throw "MAP-6X tests missing" }
Write-Output "OK: scripts\test-build42-loth-per-entry-record-model.ps1"
$map6xDocContent = Get-Content -LiteralPath $map6xDoc -Raw
if ($map6xDocContent -notmatch 'BUILD42_LOTH_PER_ENTRY_RECORD_MODEL_ANALYSED') { throw "MAP-6X doc missing BUILD42_LOTH_PER_ENTRY_RECORD_MODEL_ANALYSED" }
Write-Output "OK: doc contains BUILD42_LOTH_PER_ENTRY_RECORD_MODEL_ANALYSED"
if ($map6xDocContent -notmatch 'LOTH_TRAILING_BODY_FIXED_SIZE_FOR_SIMPLE_CELLS') { throw "MAP-6X doc missing LOTH_TRAILING_BODY_FIXED_SIZE_FOR_SIMPLE_CELLS" }
Write-Output "OK: doc contains LOTH_TRAILING_BODY_FIXED_SIZE_FOR_SIMPLE_CELLS"
if ($map6xDocContent -notmatch 'PLAYABLE_EXPORT_CLAIM_ALLOWED=false') { throw "MAP-6X doc missing PLAYABLE_EXPORT_CLAIM_ALLOWED=false" }
Write-Output "OK: doc contains PLAYABLE_EXPORT_CLAIM_ALLOWED=false"
$map6xScriptContent = Get-Content -LiteralPath $map6xScript -Raw
if ($map6xScriptContent -notmatch '\.local') { throw "MAP-6X script missing .local refusal" }
Write-Output "OK: script contains .local refusal language"

Write-Output ""
Write-Output "--- MAP-6X per-entry record model tests ---"
& powershell -ExecutionPolicy Bypass -File $map6xTests
if ($LASTEXITCODE -ne 0) { throw "MAP-6X per-entry record model tests failed." }

Write-Output ""
Write-Output "--- MAP-6W LOTH trailing byte pattern research ---"
$map6wDoc    = Join-Path $repoRoot 'docs\MAP_6W_LOTH_TRAILING_BYTE_PATTERN_RESEARCH.md'
$map6wScript = Join-Path $repoRoot 'scripts\analyze-build42-loth-trailing-byte-patterns.ps1'
$map6wTests  = Join-Path $repoRoot 'scripts\test-build42-loth-trailing-byte-patterns.ps1'
if (-not (Test-Path -LiteralPath $map6wDoc))    { throw "MAP-6W doc missing" }
Write-Output "OK: docs\MAP_6W_LOTH_TRAILING_BYTE_PATTERN_RESEARCH.md"
if (-not (Test-Path -LiteralPath $map6wScript)) { throw "MAP-6W script missing" }
Write-Output "OK: scripts\analyze-build42-loth-trailing-byte-patterns.ps1"
if (-not (Test-Path -LiteralPath $map6wTests))  { throw "MAP-6W tests missing" }
Write-Output "OK: scripts\test-build42-loth-trailing-byte-patterns.ps1"
$map6wDocContent = Get-Content -LiteralPath $map6wDoc -Raw
if ($map6wDocContent -notmatch 'BUILD42_LOTH_TRAILING_BYTE_PATTERNS_ANALYSED') { throw "MAP-6W doc missing BUILD42_LOTH_TRAILING_BYTE_PATTERNS_ANALYSED" }
Write-Output "OK: doc contains BUILD42_LOTH_TRAILING_BYTE_PATTERNS_ANALYSED"
if ($map6wDocContent -notmatch 'WRITER_NOT_DEFENSIBLE') { throw "MAP-6W doc missing WRITER_NOT_DEFENSIBLE" }
Write-Output "OK: doc contains WRITER_NOT_DEFENSIBLE"
if ($map6wDocContent -notmatch 'PLAYABLE_EXPORT_CLAIM_ALLOWED=false') { throw "MAP-6W doc missing PLAYABLE_EXPORT_CLAIM_ALLOWED=false" }
Write-Output "OK: doc contains PLAYABLE_EXPORT_CLAIM_ALLOWED=false"
$map6wScriptContent = Get-Content -LiteralPath $map6wScript -Raw
if ($map6wScriptContent -notmatch '\.local') { throw "MAP-6W script missing .local refusal" }
Write-Output "OK: script contains .local refusal language"

Write-Output ""
Write-Output "--- MAP-6W byte pattern tests ---"
& powershell -ExecutionPolicy Bypass -File $map6wTests
if ($LASTEXITCODE -ne 0) { throw "MAP-6W byte pattern tests failed." }

Write-Output ""
Write-Output "--- MAP-6V LOTH trailing body decode research ---"
$map6vDoc    = Join-Path $repoRoot 'docs\MAP_6V_LOTH_TRAILING_BODY_DECODE_RESEARCH.md'
$map6vScript = Join-Path $repoRoot 'scripts\decode-build42-loth-trailing-body.ps1'
$map6vTests  = Join-Path $repoRoot 'scripts\test-build42-loth-trailing-body-decode.ps1'
if (-not (Test-Path -LiteralPath $map6vDoc))    { throw "MAP-6V doc missing" }
Write-Output "OK: docs\MAP_6V_LOTH_TRAILING_BODY_DECODE_RESEARCH.md"
if (-not (Test-Path -LiteralPath $map6vScript)) { throw "MAP-6V script missing" }
Write-Output "OK: scripts\decode-build42-loth-trailing-body.ps1"
if (-not (Test-Path -LiteralPath $map6vTests))  { throw "MAP-6V tests missing" }
Write-Output "OK: scripts\test-build42-loth-trailing-body-decode.ps1"
$map6vDocContent = Get-Content -LiteralPath $map6vDoc -Raw
if ($map6vDocContent -notmatch 'BUILD42_LOTH_TRAILING_BODY_DECODED') { throw "MAP-6V doc missing BUILD42_LOTH_TRAILING_BODY_DECODED" }
Write-Output "OK: doc contains BUILD42_LOTH_TRAILING_BODY_DECODED"
if ($map6vDocContent -notmatch 'HYPOTHESIS_TRAILER_UNKNOWN') { throw "MAP-6V doc missing HYPOTHESIS_TRAILER_UNKNOWN" }
Write-Output "OK: doc contains HYPOTHESIS_TRAILER_UNKNOWN"
if ($map6vDocContent -notmatch 'PLAYABLE_EXPORT_CLAIM_ALLOWED=false') { throw "MAP-6V doc missing PLAYABLE_EXPORT_CLAIM_ALLOWED=false" }
Write-Output "OK: doc contains PLAYABLE_EXPORT_CLAIM_ALLOWED=false"
$map6vScriptContent = Get-Content -LiteralPath $map6vScript -Raw
if ($map6vScriptContent -notmatch '\.local') { throw "MAP-6V script missing .local refusal" }
Write-Output "OK: script contains .local refusal language"

Write-Output ""
Write-Output "--- MAP-6V trailing body decode tests ---"
& powershell -ExecutionPolicy Bypass -File $map6vTests
if ($LASTEXITCODE -ne 0) { throw "MAP-6V trailing body decode tests failed." }

Write-Output ""
Write-Output "--- MAP-6U LOTH v2 failure record and full body research ---"
$map6uDoc    = Join-Path $repoRoot 'docs\MAP_6U_LOTH_V2_FAILURE_AND_FULL_BODY_RESEARCH.md'
$map6uScript = Join-Path $repoRoot 'scripts\inspect-build42-loth-full-body.ps1'
$map6uTests  = Join-Path $repoRoot 'scripts\test-build42-loth-full-body.ps1'
if (-not (Test-Path -LiteralPath $map6uDoc))    { throw "MAP-6U doc missing" }
Write-Output "OK: docs\MAP_6U_LOTH_V2_FAILURE_AND_FULL_BODY_RESEARCH.md"
if (-not (Test-Path -LiteralPath $map6uScript)) { throw "MAP-6U script missing" }
Write-Output "OK: scripts\inspect-build42-loth-full-body.ps1"
if (-not (Test-Path -LiteralPath $map6uTests))  { throw "MAP-6U tests missing" }
Write-Output "OK: scripts\test-build42-loth-full-body.ps1"
$map6uDocContent = Get-Content -LiteralPath $map6uDoc -Raw
if ($map6uDocContent -notmatch 'LOAD_TEST_FAIL_LOTH') { throw "MAP-6U doc missing LOAD_TEST_FAIL_LOTH" }
Write-Output "OK: doc contains LOAD_TEST_FAIL_LOTH"
if ($map6uDocContent -notmatch 'EMPTY_GRASS_V1_LOTHEADER_REJECTED') { throw "MAP-6U doc missing EMPTY_GRASS_V1_LOTHEADER_REJECTED" }
Write-Output "OK: doc contains EMPTY_GRASS_V1_LOTHEADER_REJECTED"
if ($map6uDocContent -notmatch 'PLAYABLE_EXPORT_CLAIM_ALLOWED=false') { throw "MAP-6U doc missing PLAYABLE_EXPORT_CLAIM_ALLOWED=false" }
Write-Output "OK: doc contains PLAYABLE_EXPORT_CLAIM_ALLOWED=false"
$map6uScriptContent = Get-Content -LiteralPath $map6uScript -Raw
if ($map6uScriptContent -notmatch '\.local') { throw "MAP-6U script missing .local refusal" }
Write-Output "OK: script contains .local refusal language"

Write-Output ""
Write-Output "--- MAP-6U full body tests ---"
& powershell -ExecutionPolicy Bypass -File $map6uTests
if ($LASTEXITCODE -ne 0) { throw "MAP-6U full body tests failed." }

Write-Output ""
Write-Output "--- MAP-6T Build 42 LOTH v2 load test packet ---"
$map6tDoc    = Join-Path $repoRoot 'docs\MAP_6T_LOTH_V2_LOAD_TEST_PACKET.md'
$map6tScript = Join-Path $repoRoot 'scripts\prepare-build42-loth-v2-load-test-packet.ps1'
$map6tTests  = Join-Path $repoRoot 'scripts\test-build42-loth-v2-load-test-packet.ps1'
if (-not (Test-Path -LiteralPath $map6tDoc))    { throw "MAP-6T doc missing" }
Write-Output "OK: docs\MAP_6T_LOTH_V2_LOAD_TEST_PACKET.md"
if (-not (Test-Path -LiteralPath $map6tScript)) { throw "MAP-6T script missing" }
Write-Output "OK: scripts\prepare-build42-loth-v2-load-test-packet.ps1"
if (-not (Test-Path -LiteralPath $map6tTests))  { throw "MAP-6T tests missing" }
Write-Output "OK: scripts\test-build42-loth-v2-load-test-packet.ps1"
$map6tDocContent = Get-Content -LiteralPath $map6tDoc -Raw
if ($map6tDocContent -notmatch 'MAP6T_LOTH_V2_LOAD_TEST_PACKET_CREATED') { throw "MAP-6T doc missing MAP6T_LOTH_V2_LOAD_TEST_PACKET_CREATED" }
Write-Output "OK: doc contains MAP6T_LOTH_V2_LOAD_TEST_PACKET_CREATED"
if ($map6tDocContent -notmatch 'HUMAN_ONLY_COPY_REQUIRED') { throw "MAP-6T doc missing HUMAN_ONLY_COPY_REQUIRED" }
Write-Output "OK: doc contains HUMAN_ONLY_COPY_REQUIRED"
if ($map6tDocContent -notmatch 'PLAYABLE_EXPORT_CLAIM_ALLOWED=false') { throw "MAP-6T doc missing PLAYABLE_EXPORT_CLAIM_ALLOWED=false" }
Write-Output "OK: doc contains PLAYABLE_EXPORT_CLAIM_ALLOWED=false"
$map6tScriptContent = Get-Content -LiteralPath $map6tScript -Raw
if ($map6tScriptContent -notmatch '\.local') { throw "MAP-6T script missing .local refusal" }
Write-Output "OK: script contains .local refusal language"

Write-Output ""
Write-Output "--- MAP-6T load test packet tests ---"
& powershell -ExecutionPolicy Bypass -File $map6tTests
if ($LASTEXITCODE -ne 0) { throw "MAP-6T load test packet tests failed." }

Write-Output ""
Write-Output "--- MAP-6R Build 42 LOTH structure research ---"
$map6rDoc    = Join-Path $repoRoot 'docs\MAP_6R_BUILD42_LOTH_STRUCTURE_RESEARCH.md'
$map6rScript = Join-Path $repoRoot 'scripts\inspect-build42-loth-structure.ps1'
$map6rTests  = Join-Path $repoRoot 'scripts\test-build42-loth-structure.ps1'
if (-not (Test-Path -LiteralPath $map6rDoc))    { throw "MAP-6R doc missing" }
Write-Output "OK: docs\MAP_6R_BUILD42_LOTH_STRUCTURE_RESEARCH.md"
if (-not (Test-Path -LiteralPath $map6rScript)) { throw "MAP-6R script missing" }
Write-Output "OK: scripts\inspect-build42-loth-structure.ps1"
if (-not (Test-Path -LiteralPath $map6rTests))  { throw "MAP-6R tests missing" }
Write-Output "OK: scripts\test-build42-loth-structure.ps1"
$map6rDocContent = Get-Content -LiteralPath $map6rDoc -Raw
if ($map6rDocContent -notmatch 'BUILD42_LOTH_STRUCTURE_INSPECTED') { throw "MAP-6R doc missing BUILD42_LOTH_STRUCTURE_INSPECTED" }
Write-Output "OK: doc contains BUILD42_LOTH_STRUCTURE_INSPECTED"
if ($map6rDocContent -notmatch 'CANDIDATE_LOTHEADER_TOO_SHORT_CONFIRMED') { throw "MAP-6R doc missing CANDIDATE_LOTHEADER_TOO_SHORT_CONFIRMED" }
Write-Output "OK: doc contains CANDIDATE_LOTHEADER_TOO_SHORT_CONFIRMED"
if ($map6rDocContent -notmatch 'PLAYABLE_EXPORT_CLAIM_ALLOWED=false') { throw "MAP-6R doc missing PLAYABLE_EXPORT_CLAIM_ALLOWED=false" }
Write-Output "OK: doc contains PLAYABLE_EXPORT_CLAIM_ALLOWED=false"
$map6rScriptContent = Get-Content -LiteralPath $map6rScript -Raw
if ($map6rScriptContent -notmatch '\.local') { throw "MAP-6R script missing .local refusal" }
Write-Output "OK: script contains .local refusal language"

Write-Output ""
Write-Output "--- MAP-6R LOTH structure tests ---"
& powershell -ExecutionPolicy Bypass -File $map6rTests
if ($LASTEXITCODE -ne 0) { throw "MAP-6R LOTH structure tests failed." }

Write-Output ""
Write-Output "--- MAP-6Q spawn activation fixed; lotheader EOF failure ---"
$map6qDoc    = Join-Path $repoRoot 'docs\MAP_6Q_SPAWN_FIXED_LOTHEADER_EOF_FAILURE_RECORD.md'
$map6qScript = Join-Path $repoRoot 'scripts\compare-build42-lotheader-candidate.ps1'
$map6qTests  = Join-Path $repoRoot 'scripts\test-build42-lotheader-candidate-comparison.ps1'
if (-not (Test-Path -LiteralPath $map6qDoc))    { throw "MAP-6Q doc missing" }
Write-Output "OK: docs\MAP_6Q_SPAWN_FIXED_LOTHEADER_EOF_FAILURE_RECORD.md"
if (-not (Test-Path -LiteralPath $map6qScript)) { throw "MAP-6Q script missing" }
Write-Output "OK: scripts\compare-build42-lotheader-candidate.ps1"
if (-not (Test-Path -LiteralPath $map6qTests))  { throw "MAP-6Q tests missing" }
Write-Output "OK: scripts\test-build42-lotheader-candidate-comparison.ps1"
$map6qDocContent = Get-Content -LiteralPath $map6qDoc -Raw
if ($map6qDocContent -notmatch 'CURRENT_CANDIDATE_LOTHEADER_EOF') { throw "MAP-6Q doc missing CURRENT_CANDIDATE_LOTHEADER_EOF" }
Write-Output "OK: doc contains CURRENT_CANDIDATE_LOTHEADER_EOF"
if ($map6qDocContent -notmatch 'LOTHEADER_STRUCTURE_REJECTED') { throw "MAP-6Q doc missing LOTHEADER_STRUCTURE_REJECTED" }
Write-Output "OK: doc contains LOTHEADER_STRUCTURE_REJECTED"
if ($map6qDocContent -notmatch 'PLAYABLE_EXPORT_CLAIM_ALLOWED=false') { throw "MAP-6Q doc missing PLAYABLE_EXPORT_CLAIM_ALLOWED=false" }
Write-Output "OK: doc contains PLAYABLE_EXPORT_CLAIM_ALLOWED=false"
$map6qScriptContent = Get-Content -LiteralPath $map6qScript -Raw
if ($map6qScriptContent -notmatch '\.local') { throw "MAP-6Q script missing .local refusal" }
Write-Output "OK: script contains .local refusal language"

Write-Output ""
Write-Output "--- MAP-6Q lotheader comparison tests ---"
& powershell -ExecutionPolicy Bypass -File $map6qTests
if ($LASTEXITCODE -ne 0) { throw "MAP-6Q lotheader comparison tests failed." }

Write-Output ""
Write-Output "--- MAP-6P clean retest and spawn activation diagnostic ---"
$map6pDoc    = Join-Path $repoRoot 'docs\MAP_6P_CLEAN_RETEST_SPAWN_ACTIVATION_RECORD.md'
$map6pProto  = Join-Path $repoRoot 'docs\MAP_6P_SPAWN_ACTIVATION_DIAGNOSTIC_PROTOCOL.md'
$map6pScript = Join-Path $repoRoot 'scripts\prepare-map6p-spawn-activation-diagnostic.ps1'
$map6pTests  = Join-Path $repoRoot 'scripts\test-map6p-spawn-activation-diagnostic.ps1'
if (-not (Test-Path -LiteralPath $map6pDoc))    { throw "MAP-6P record doc missing" }
Write-Output "OK: docs\MAP_6P_CLEAN_RETEST_SPAWN_ACTIVATION_RECORD.md"
if (-not (Test-Path -LiteralPath $map6pProto))  { throw "MAP-6P protocol doc missing" }
Write-Output "OK: docs\MAP_6P_SPAWN_ACTIVATION_DIAGNOSTIC_PROTOCOL.md"
if (-not (Test-Path -LiteralPath $map6pScript)) { throw "MAP-6P script missing" }
Write-Output "OK: scripts\prepare-map6p-spawn-activation-diagnostic.ps1"
if (-not (Test-Path -LiteralPath $map6pTests))  { throw "MAP-6P tests missing" }
Write-Output "OK: scripts\test-map6p-spawn-activation-diagnostic.ps1"
$map6pDocContent = Get-Content -LiteralPath $map6pDoc -Raw
if ($map6pDocContent -notmatch 'BUILD42_CANDIDATE_MOD_LOAD_CONFIRMED') { throw "MAP-6P doc missing BUILD42_CANDIDATE_MOD_LOAD_CONFIRMED" }
Write-Output "OK: doc contains BUILD42_CANDIDATE_MOD_LOAD_CONFIRMED"
if ($map6pDocContent -notmatch 'CANDIDATE_SPAWN_REGION_NOT_VISIBLE') { throw "MAP-6P doc missing CANDIDATE_SPAWN_REGION_NOT_VISIBLE" }
Write-Output "OK: doc contains CANDIDATE_SPAWN_REGION_NOT_VISIBLE"
if ($map6pDocContent -notmatch 'PLAYABLE_EXPORT_CLAIM_ALLOWED=false') { throw "MAP-6P doc missing PLAYABLE_EXPORT_CLAIM_ALLOWED=false" }
Write-Output "OK: doc contains PLAYABLE_EXPORT_CLAIM_ALLOWED=false"
$map6pScriptContent = Get-Content -LiteralPath $map6pScript -Raw
if ($map6pScriptContent -notmatch '\.local') { throw "MAP-6P script missing .local refusal" }
Write-Output "OK: script contains .local refusal language"

Write-Output ""
Write-Output "--- MAP-6P spawn activation diagnostic tests ---"
& powershell -ExecutionPolicy Bypass -File $map6pTests
if ($LASTEXITCODE -ne 0) { throw "MAP-6P spawn activation diagnostic tests failed." }

Write-Output ""
Write-Output "--- MAP-6O clean isolated candidate retest protocol ---"
$map6oDoc    = Join-Path $repoRoot 'docs\MAP_6O_CLEAN_ISOLATED_CANDIDATE_RETEST_PROTOCOL.md'
$map6oScript = Join-Path $repoRoot 'scripts\prepare-map6o-clean-retest-checklist.ps1'
$map6oTests  = Join-Path $repoRoot 'scripts\test-map6o-clean-retest-checklist.ps1'
if (-not (Test-Path -LiteralPath $map6oDoc))    { throw "MAP-6O doc missing: docs\MAP_6O_CLEAN_ISOLATED_CANDIDATE_RETEST_PROTOCOL.md" }
Write-Output "OK: docs\MAP_6O_CLEAN_ISOLATED_CANDIDATE_RETEST_PROTOCOL.md"
if (-not (Test-Path -LiteralPath $map6oScript)) { throw "MAP-6O script missing: scripts\prepare-map6o-clean-retest-checklist.ps1" }
Write-Output "OK: scripts\prepare-map6o-clean-retest-checklist.ps1"
if (-not (Test-Path -LiteralPath $map6oTests))  { throw "MAP-6O tests missing: scripts\test-map6o-clean-retest-checklist.ps1" }
Write-Output "OK: scripts\test-map6o-clean-retest-checklist.ps1"
$map6oDocContent = Get-Content -LiteralPath $map6oDoc -Raw
if ($map6oDocContent -notmatch 'CLEAN_ISOLATED_RETEST_PROTOCOL_CREATED') { throw "MAP-6O doc missing CLEAN_ISOLATED_RETEST_PROTOCOL_CREATED sentinel" }
Write-Output "OK: doc contains CLEAN_ISOLATED_RETEST_PROTOCOL_CREATED"
if ($map6oDocContent -notmatch 'HUMAN_ONLY_COPY_REQUIRED') { throw "MAP-6O doc missing HUMAN_ONLY_COPY_REQUIRED sentinel" }
Write-Output "OK: doc contains HUMAN_ONLY_COPY_REQUIRED"
if ($map6oDocContent -notmatch 'PLAYABLE_EXPORT_CLAIM_ALLOWED=false') { throw "MAP-6O doc missing PLAYABLE_EXPORT_CLAIM_ALLOWED=false sentinel" }
Write-Output "OK: doc contains PLAYABLE_EXPORT_CLAIM_ALLOWED=false"
$map6oScriptContent = Get-Content -LiteralPath $map6oScript -Raw
if ($map6oScriptContent -notmatch '\.local') { throw "MAP-6O script missing .local refusal language" }
Write-Output "OK: script contains .local refusal language"
if ($map6oScriptContent -notmatch 'HUMAN_ONLY_COPY_REQUIRED') { throw "MAP-6O script missing HUMAN_ONLY_COPY_REQUIRED sentinel" }
Write-Output "OK: script contains HUMAN_ONLY_COPY_REQUIRED sentinel"

Write-Output ""
Write-Output "--- MAP-6O retest checklist tests ---"
& powershell -ExecutionPolicy Bypass -File $map6oTests
if ($LASTEXITCODE -ne 0) { throw "MAP-6O retest checklist tests failed." }

Write-Output ""
Write-Output "--- MAP-6N preliminary candidate load test record ---"
$map6nDoc    = Join-Path $repoRoot 'docs\MAP_6N_PRELIMINARY_CANDIDATE_LOAD_TEST_RECORD.md'
$map6nScript = Join-Path $repoRoot 'scripts\extract-map6n-current-candidate-log-evidence.ps1'
$map6nTests  = Join-Path $repoRoot 'scripts\test-extract-map6n-log-evidence.ps1'
if (-not (Test-Path -LiteralPath $map6nDoc))    { throw "MAP-6N doc missing: docs\MAP_6N_PRELIMINARY_CANDIDATE_LOAD_TEST_RECORD.md" }
Write-Output "OK: docs\MAP_6N_PRELIMINARY_CANDIDATE_LOAD_TEST_RECORD.md"
if (-not (Test-Path -LiteralPath $map6nScript)) { throw "MAP-6N script missing: scripts\extract-map6n-current-candidate-log-evidence.ps1" }
Write-Output "OK: scripts\extract-map6n-current-candidate-log-evidence.ps1"
if (-not (Test-Path -LiteralPath $map6nTests))  { throw "MAP-6N tests missing: scripts\test-extract-map6n-log-evidence.ps1" }
Write-Output "OK: scripts\test-extract-map6n-log-evidence.ps1"
$map6nDocContent = Get-Content -LiteralPath $map6nDoc -Raw
if ($map6nDocContent -notmatch 'LOAD_TEST_INCONCLUSIVE') { throw "MAP-6N doc missing LOAD_TEST_INCONCLUSIVE sentinel" }
Write-Output "OK: doc contains LOAD_TEST_INCONCLUSIVE"
if ($map6nDocContent -notmatch 'STALE_MAPTEST_A_LOGS_EXCLUDED') { throw "MAP-6N doc missing STALE_MAPTEST_A_LOGS_EXCLUDED sentinel" }
Write-Output "OK: doc contains STALE_MAPTEST_A_LOGS_EXCLUDED"
if ($map6nDocContent -notmatch 'PLAYABLE_EXPORT_CLAIM_ALLOWED=false') { throw "MAP-6N doc missing PLAYABLE_EXPORT_CLAIM_ALLOWED=false sentinel" }
Write-Output "OK: doc contains PLAYABLE_EXPORT_CLAIM_ALLOWED=false"
$map6nScriptContent = Get-Content -LiteralPath $map6nScript -Raw
if ($map6nScriptContent -notmatch '\.local') { throw "MAP-6N script missing .local refusal language" }
Write-Output "OK: script contains .local refusal language"
if ($map6nScriptContent -notmatch 'stale_maptest_a_matches') { throw "MAP-6N script missing stale_maptest_a_matches sentinel" }
Write-Output "OK: script contains stale_maptest_a_matches sentinel"

Write-Output ""
Write-Output "--- MAP-6N log triage tests ---"
& powershell -ExecutionPolicy Bypass -File $map6nTests
if ($LASTEXITCODE -ne 0) { throw "MAP-6N log triage tests failed." }

Write-Output ""
Write-Output "--- MAP-6M Build 42 candidate load test packet ---"
$map6mDoc    = Join-Path $repoRoot 'docs\MAP_6M_BUILD42_CANDIDATE_LOAD_TEST_PACKET.md'
$map6mScript = Join-Path $repoRoot 'scripts\prepare-build42-candidate-load-test-packet.ps1'
$map6mTests  = Join-Path $repoRoot 'scripts\test-build42-candidate-load-test-packet.ps1'
if (-not (Test-Path -LiteralPath $map6mDoc))    { throw "MAP-6M doc missing" }
Write-Output "OK: docs\MAP_6M_BUILD42_CANDIDATE_LOAD_TEST_PACKET.md"
if (-not (Test-Path -LiteralPath $map6mScript)) { throw "MAP-6M script missing" }
Write-Output "OK: scripts\prepare-build42-candidate-load-test-packet.ps1"
if (-not (Test-Path -LiteralPath $map6mTests))  { throw "MAP-6M tests missing" }
Write-Output "OK: scripts\test-build42-candidate-load-test-packet.ps1"
$map6mDocContent = Get-Content -LiteralPath $map6mDoc -Raw
if ($map6mDocContent -notmatch 'BUILD42_CANDIDATE_LOAD_TEST_PACKET_CREATED') { throw "MAP-6M doc missing BUILD42_CANDIDATE_LOAD_TEST_PACKET_CREATED" }
Write-Output "OK: doc contains BUILD42_CANDIDATE_LOAD_TEST_PACKET_CREATED"
if ($map6mDocContent -notmatch 'LOAD_TEST_NOT_PERFORMED') { throw "MAP-6M doc missing LOAD_TEST_NOT_PERFORMED" }
Write-Output "OK: doc contains LOAD_TEST_NOT_PERFORMED"
if ($map6mDocContent -notmatch 'PLAYABLE_EXPORT_CLAIM_ALLOWED=false') { throw "MAP-6M doc missing PLAYABLE_EXPORT_CLAIM_ALLOWED=false" }
Write-Output "OK: doc contains PLAYABLE_EXPORT_CLAIM_ALLOWED=false"

Write-Output ""
Write-Output "--- MAP-6M packet tests ---"
& powershell -ExecutionPolicy Bypass -File $map6mTests
if ($LASTEXITCODE -ne 0) { throw "MAP-6M packet tests failed." }

Write-Output ""
Write-Output "--- MAP-6K LOTP payload / LOTH entry research ---"
$map6kDoc    = Join-Path $repoRoot 'docs\MAP_6K_LOTP_PAYLOAD_AND_LOTH_ENTRY_RESEARCH.md'
$map6kScript = Join-Path $repoRoot 'scripts\inspect-build42-lotp-payload-windows.ps1'
$map6kTests  = Join-Path $repoRoot 'scripts\test-build42-lotp-payload-windows.ps1'
if (-not (Test-Path -LiteralPath $map6kDoc))    { throw "MAP-6K doc missing" }
Write-Output "OK: docs\MAP_6K_LOTP_PAYLOAD_AND_LOTH_ENTRY_RESEARCH.md"
if (-not (Test-Path -LiteralPath $map6kScript)) { throw "MAP-6K script missing" }
Write-Output "OK: scripts\inspect-build42-lotp-payload-windows.ps1"
if (-not (Test-Path -LiteralPath $map6kTests))  { throw "MAP-6K tests missing" }
Write-Output "OK: scripts\test-build42-lotp-payload-windows.ps1"
$map6kDocContent = Get-Content -LiteralPath $map6kDoc -Raw
if ($map6kDocContent -notmatch 'BUILD42_LOTP_PAYLOAD_WINDOWS_INSPECTED') { throw "MAP-6K doc missing BUILD42_LOTP_PAYLOAD_WINDOWS_INSPECTED" }
Write-Output "OK: doc contains BUILD42_LOTP_PAYLOAD_WINDOWS_INSPECTED"
if ($map6kDocContent -notmatch 'WRITER_NOT_IMPLEMENTED') { throw "MAP-6K doc missing WRITER_NOT_IMPLEMENTED" }
Write-Output "OK: doc contains WRITER_NOT_IMPLEMENTED"
if ($map6kDocContent -notmatch 'PLAYABLE_EXPORT_CLAIM_ALLOWED=false') { throw "MAP-6K doc missing PLAYABLE_EXPORT_CLAIM_ALLOWED=false" }
Write-Output "OK: doc contains PLAYABLE_EXPORT_CLAIM_ALLOWED=false"

Write-Output ""
Write-Output "--- MAP-6K payload window tests ---"
& powershell -ExecutionPolicy Bypass -File $map6kTests
if ($LASTEXITCODE -ne 0) { throw "MAP-6K payload window tests failed." }

Write-Output ""
Write-Output "--- MAP-6J Build 42 writer contract ---"
& powershell -ExecutionPolicy Bypass -File (Join-Path $repoRoot 'scripts\test-build42-writer-contract.ps1')
if ($LASTEXITCODE -ne 0) { throw "MAP-6J writer contract tests failed." }

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
# Ledger constants - sourced from proof-packet v0.27 / docs/VALIDATION_LEDGER.md.
# Update here when counts change; update the proof packet schema and ledger too.
# ---------------------------------------------------------------------------

$psChecks = [ordered]@{
    'Schema file sanity'                   = 214
    'Artifact contract'                    = 40
    'Palette SHA-256 verification'         = 5
    'TMX integrity'                        = 21
    'Hardening harness'                    = 36
    'Region extraction'                    = 24
    'Primitive classification'             = 22
    'Plan recommendations contract'        = 28
    'Proof packet'                         = 102
    'Build42 geometry inspector tests'     = 23
    'Build42 format design matrix tests'   = 13
    'Build42 writer contract tests'        = 20
    'Build42 LOTP payload window tests'    = 20
    'Build42 candidate packet tests'       = 20
    'MAP-6N log triage tests'             = 12
    'MAP-6O retest checklist tests'        = 15
    'MAP-6P spawn activation tests'        = 12
    'MAP-6Q lotheader comparison tests'    = 13
    'MAP-6R LOTH structure tests'          = 14
    'MAP-6T load test packet tests'        = 18
    'MAP-6U full body tests'               = 14
    'MAP-6V trailing body decode tests'    = 17
    'MAP-6W byte pattern tests'            = 20
    'MAP-6X per-entry record model tests'  = 20
    'MAP-6Y fixed 1048 block tests'        = 20
    'MAP-7A load test packet tests'        = 23
    'MAP-7B Lua metadata tests'            = 21
    'MAP-7C metadata v3 packet tests'     = 18
    'MAP-7D metadata v4 packet tests'     = 15
    'MAP-7N reference map id tests'          = 9
    'MAP-7M known-working contract tests'    = 12
    'MAP-7L common layout experiment tests'  = 15
    'MAP-7K modinfo map field tests'         = 11
    'MAP-7J metadata contract tests'        = 17
    'MAP-7I root modinfo experiment tests'  = 12
    'MAP-7H discovery path tests'          = 12
    'MAP-7G variant A failure tests'       = 8
    'MAP-7F registration diagnostic tests' = 11
    'MAP-7E diagnostics tests'            = 11
}
$psTotal = 958   # = validation_summary.total_expected_assertions in proof-packet v0.43

$dnCoreTests = 190   # PZMapForge.Core.Tests
$dnCliTests  = 366   # PZMapForge.Cli.Tests (MAP-7D: +18 Build42 LOTH v4 no-BOM tests)
$dnTotal     = 556   # = dotnet_validation_summary.test_total in proof-packet v0.35

Write-Output ""
Write-Output "  PowerShell lane  (validation_summary in proof-packet v0.43):"
foreach ($kv in $psChecks.GetEnumerator()) {
    Write-Output ("    {0,-34} {1,4}" -f "$($kv.Key):", $kv.Value)
}
Write-Output "    -------------------------------------- ----"
Write-Output ("    {0,-34} {1,4}" -f "Total:", $psTotal)

Write-Output ""
Write-Output "  .NET lane  (dotnet_validation_summary in proof-packet v0.43 -- tracked separately):"
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
