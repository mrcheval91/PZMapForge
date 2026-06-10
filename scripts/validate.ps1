#Requires -Version 5.1
<#
.SYNOPSIS
    Full local validation for PZMapForge.
    Runs all PowerShell validation sub-scripts and finishes with a ledger
    summary. All sub-scripts must pass; exits nonzero on any failure.

    Final output reports the complete PowerShell validation lane total (1824)
    and the .NET lane total (556) as separate evidence lanes.
    Counts are sourced from proof-packet v0.76 / docs/VALIDATION_LEDGER.md.
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
Write-Output "--- MAP-9A Muldraugh bootstrap canary overlay packet ---"
$map9aRuntimeDoc   = Join-Path $repoRoot 'docs\MAP_8Z_RUNTIME_FALLBACK_RESULT.md'
$map9aDoc          = Join-Path $repoRoot 'docs\MAP_9A_MULDRAUGH_BOOTSTRAP_CANARY_OVERLAY.md'
$map9aPacketScript = Join-Path $repoRoot 'scripts\prepare-build42-map9a-bootstrap-canary-packet.ps1'
$map9aPacketTests  = Join-Path $repoRoot 'scripts\test-build42-map9a-bootstrap-canary-packet.ps1'
if (-not (Test-Path -LiteralPath $map9aRuntimeDoc))   { throw "MAP-8Z runtime fallback result doc missing" }
Write-Output "OK: docs\MAP_8Z_RUNTIME_FALLBACK_RESULT.md"
if (-not (Test-Path -LiteralPath $map9aDoc))          { throw "MAP-9A doc missing" }
Write-Output "OK: docs\MAP_9A_MULDRAUGH_BOOTSTRAP_CANARY_OVERLAY.md"
if (-not (Test-Path -LiteralPath $map9aPacketScript)) { throw "MAP-9A packet script missing" }
Write-Output "OK: scripts\prepare-build42-map9a-bootstrap-canary-packet.ps1"
if (-not (Test-Path -LiteralPath $map9aPacketTests))  { throw "MAP-9A packet tests missing" }
Write-Output "OK: scripts\test-build42-map9a-bootstrap-canary-packet.ps1"
$map9aRuntimeDocContent = Get-Content -LiteralPath $map9aRuntimeDoc -Raw
if ($map9aRuntimeDocContent -notmatch 'MAP8Z_RUNTIME_FALLBACK_MULDRAUGH_CONFIRMED') { throw "MAP-8Z runtime fallback doc missing MAP8Z_RUNTIME_FALLBACK_MULDRAUGH_CONFIRMED" }
Write-Output "OK: doc contains MAP8Z_RUNTIME_FALLBACK_MULDRAUGH_CONFIRMED"
if ($map9aRuntimeDocContent -notmatch 'NO_MULDRAUGH_STRATEGY_REJECTED') { throw "MAP-8Z runtime fallback doc missing NO_MULDRAUGH_STRATEGY_REJECTED" }
Write-Output "OK: doc contains NO_MULDRAUGH_STRATEGY_REJECTED"
if ($map9aRuntimeDocContent -notmatch 'PUBLIC_PLAYABLE_CLAIM_ALLOWED=false') { throw "MAP-8Z runtime fallback doc missing PUBLIC_PLAYABLE_CLAIM_ALLOWED=false" }
Write-Output "OK: doc contains PUBLIC_PLAYABLE_CLAIM_ALLOWED=false"
$map9aDocContent = Get-Content -LiteralPath $map9aDoc -Raw
if ($map9aDocContent -notmatch 'MAP9A_MULDRAUGH_BOOTSTRAP_CANARY_OVERLAY_DEFINED') { throw "MAP-9A doc missing MAP9A_MULDRAUGH_BOOTSTRAP_CANARY_OVERLAY_DEFINED" }
Write-Output "OK: doc contains MAP9A_MULDRAUGH_BOOTSTRAP_CANARY_OVERLAY_DEFINED"
if ($map9aDocContent -notmatch 'canary_writer_blocked') { throw "MAP-9A doc missing canary_writer_blocked" }
Write-Output "OK: doc contains canary_writer_blocked"
if ($map9aDocContent -notmatch 'PUBLIC_PLAYABLE_CLAIM_ALLOWED=false') { throw "MAP-9A doc missing PUBLIC_PLAYABLE_CLAIM_ALLOWED=false" }
Write-Output "OK: MAP-9A doc contains PUBLIC_PLAYABLE_CLAIM_ALLOWED=false"
$map9aPacketContent = Get-Content -LiteralPath $map9aPacketScript -Raw
if ($map9aPacketContent -notmatch '\.local') { throw "MAP-9A packet script missing .local refusal" }
Write-Output "OK: MAP-9A packet script contains .local refusal language"

Write-Output ""
Write-Output "--- MAP-9A bootstrap canary packet tests ---"
& powershell -ExecutionPolicy Bypass -File $map9aPacketTests
if ($LASTEXITCODE -ne 0) { throw "MAP-9A bootstrap canary packet tests failed." }

Write-Output ""
Write-Output "--- MAP-8Z Controlled IGMB install packet ---"
$map8zDoc          = Join-Path $repoRoot 'docs\MAP_8Z_CONTROLLED_IGMB_INSTALL_PACKET.md'
$map8zPacketScript = Join-Path $repoRoot 'scripts\prepare-build42-map8z-controlled-igmb-install-packet.ps1'
$map8zPacketTests  = Join-Path $repoRoot 'scripts\test-build42-map8z-controlled-igmb-install-packet.ps1'
if (-not (Test-Path -LiteralPath $map8zDoc))          { throw "MAP-8Z doc missing" }
Write-Output "OK: docs\MAP_8Z_CONTROLLED_IGMB_INSTALL_PACKET.md"
if (-not (Test-Path -LiteralPath $map8zPacketScript)) { throw "MAP-8Z packet script missing" }
Write-Output "OK: scripts\prepare-build42-map8z-controlled-igmb-install-packet.ps1"
if (-not (Test-Path -LiteralPath $map8zPacketTests))  { throw "MAP-8Z packet tests missing" }
Write-Output "OK: scripts\test-build42-map8z-controlled-igmb-install-packet.ps1"
$map8zDocContent = Get-Content -LiteralPath $map8zDoc -Raw
if ($map8zDocContent -notmatch 'MAP8Z_CONTROLLED_IGMB_INSTALL_PACKET_DEFINED') { throw "MAP-8Z doc missing MAP8Z_CONTROLLED_IGMB_INSTALL_PACKET_DEFINED" }
Write-Output "OK: doc contains MAP8Z_CONTROLLED_IGMB_INSTALL_PACKET_DEFINED"
if ($map8zDocContent -notmatch 'HUMAN_MANUAL_COPY_REQUIRED=true') { throw "MAP-8Z doc missing HUMAN_MANUAL_COPY_REQUIRED=true" }
Write-Output "OK: doc contains HUMAN_MANUAL_COPY_REQUIRED=true"
if ($map8zDocContent -notmatch 'PUBLIC_PLAYABLE_CLAIM_ALLOWED=false') { throw "MAP-8Z doc missing PUBLIC_PLAYABLE_CLAIM_ALLOWED=false" }
Write-Output "OK: doc contains PUBLIC_PLAYABLE_CLAIM_ALLOWED=false"
$map8zPacketContent = Get-Content -LiteralPath $map8zPacketScript -Raw
if ($map8zPacketContent -notmatch '\.local') { throw "MAP-8Z packet script missing .local refusal" }
Write-Output "OK: packet script contains .local refusal language"

Write-Output ""
Write-Output "--- MAP-8Z controlled IGMB install packet tests ---"
& powershell -ExecutionPolicy Bypass -File $map8zPacketTests
if ($LASTEXITCODE -ne 0) { throw "MAP-8Z controlled IGMB install packet tests failed." }

Write-Output ""
Write-Output "--- MAP-8Y Experimental IGMB writer skeleton ---"
$map8yDoc          = Join-Path $repoRoot 'docs\MAP_8Y_EXPERIMENTAL_IGMB_WRITER_SKELETON.md'
$map8yWriter       = Join-Path $repoRoot 'scripts\write-build42-experimental-igmb-worldmap.ps1'
$map8yWriterTests  = Join-Path $repoRoot 'scripts\test-build42-experimental-igmb-worldmap-writer.ps1'
$map8yPacketScript = Join-Path $repoRoot 'scripts\prepare-build42-map8y-experimental-igmb-writer-packet.ps1'
$map8yPacketTests  = Join-Path $repoRoot 'scripts\test-build42-map8y-experimental-igmb-writer-packet.ps1'
if (-not (Test-Path -LiteralPath $map8yDoc))          { throw "MAP-8Y doc missing" }
Write-Output "OK: docs\MAP_8Y_EXPERIMENTAL_IGMB_WRITER_SKELETON.md"
if (-not (Test-Path -LiteralPath $map8yWriter))       { throw "MAP-8Y writer missing" }
Write-Output "OK: scripts\write-build42-experimental-igmb-worldmap.ps1"
if (-not (Test-Path -LiteralPath $map8yWriterTests))  { throw "MAP-8Y writer tests missing" }
Write-Output "OK: scripts\test-build42-experimental-igmb-worldmap-writer.ps1"
if (-not (Test-Path -LiteralPath $map8yPacketScript)) { throw "MAP-8Y packet script missing" }
Write-Output "OK: scripts\prepare-build42-map8y-experimental-igmb-writer-packet.ps1"
if (-not (Test-Path -LiteralPath $map8yPacketTests))  { throw "MAP-8Y packet tests missing" }
Write-Output "OK: scripts\test-build42-map8y-experimental-igmb-writer-packet.ps1"
$map8yDocContent = Get-Content -LiteralPath $map8yDoc -Raw
if ($map8yDocContent -notmatch 'MAP8Y_EXPERIMENTAL_IGMB_WRITER_SKELETON_ADDED') { throw "MAP-8Y doc missing MAP8Y_EXPERIMENTAL_IGMB_WRITER_SKELETON_ADDED" }
Write-Output "OK: doc contains MAP8Y_EXPERIMENTAL_IGMB_WRITER_SKELETON_ADDED"
if ($map8yDocContent -notmatch 'EXPERIMENTAL_WRITER_LOCAL_ONLY') { throw "MAP-8Y doc missing EXPERIMENTAL_WRITER_LOCAL_ONLY" }
Write-Output "OK: doc contains EXPERIMENTAL_WRITER_LOCAL_ONLY"
if ($map8yDocContent -notmatch 'PUBLIC_PLAYABLE_CLAIM_ALLOWED=false') { throw "MAP-8Y doc missing PUBLIC_PLAYABLE_CLAIM_ALLOWED=false" }
Write-Output "OK: doc contains PUBLIC_PLAYABLE_CLAIM_ALLOWED=false"
$map8yWriterContent = Get-Content -LiteralPath $map8yWriter -Raw
if ($map8yWriterContent -notmatch '\.local') { throw "MAP-8Y writer missing .local refusal" }
Write-Output "OK: writer contains .local refusal language"

Write-Output ""
Write-Output "--- MAP-8Y experimental IGMB writer tests ---"
& powershell -ExecutionPolicy Bypass -File $map8yWriterTests
if ($LASTEXITCODE -ne 0) { throw "MAP-8Y experimental IGMB writer tests failed." }

Write-Output ""
Write-Output "--- MAP-8Y experimental IGMB writer packet tests ---"
& powershell -ExecutionPolicy Bypass -File $map8yPacketTests
if ($LASTEXITCODE -ne 0) { throw "MAP-8Y experimental IGMB writer packet tests failed." }

Write-Output ""
Write-Output "--- MAP-8X Real transition structure result ---"
$map8xDoc          = Join-Path $repoRoot 'docs\MAP_8X_REAL_TRANSITION_STRUCTURE_RESULT.md'
$map8xPacketScript = Join-Path $repoRoot 'scripts\prepare-build42-map8x-real-transition-structure-result-packet.ps1'
$map8xResultTests  = Join-Path $repoRoot 'scripts\test-build42-map8x-real-transition-structure-result.ps1'
if (-not (Test-Path -LiteralPath $map8xDoc))          { throw "MAP-8X doc missing" }
Write-Output "OK: docs\MAP_8X_REAL_TRANSITION_STRUCTURE_RESULT.md"
if (-not (Test-Path -LiteralPath $map8xPacketScript)) { throw "MAP-8X packet script missing" }
Write-Output "OK: scripts\prepare-build42-map8x-real-transition-structure-result-packet.ps1"
if (-not (Test-Path -LiteralPath $map8xResultTests))  { throw "MAP-8X result tests missing" }
Write-Output "OK: scripts\test-build42-map8x-real-transition-structure-result.ps1"
$map8xDocContent = Get-Content -LiteralPath $map8xDoc -Raw
if ($map8xDocContent -notmatch 'MAP8X_REAL_TRANSITION_STRUCTURE_RESULT_RECORDED') { throw "MAP-8X doc missing MAP8X_REAL_TRANSITION_STRUCTURE_RESULT_RECORDED" }
Write-Output "OK: doc contains MAP8X_REAL_TRANSITION_STRUCTURE_RESULT_RECORDED"
if ($map8xDocContent -notmatch 'BINARY_WRITER_GATE_STILL_CLOSED') { throw "MAP-8X doc missing BINARY_WRITER_GATE_STILL_CLOSED" }
Write-Output "OK: doc contains BINARY_WRITER_GATE_STILL_CLOSED"
if ($map8xDocContent -notmatch 'PUBLIC_PLAYABLE_CLAIM_ALLOWED=false') { throw "MAP-8X doc missing PUBLIC_PLAYABLE_CLAIM_ALLOWED=false" }
Write-Output "OK: doc contains PUBLIC_PLAYABLE_CLAIM_ALLOWED=false"
$map8xPacketContent = Get-Content -LiteralPath $map8xPacketScript -Raw
if ($map8xPacketContent -notmatch '\.local') { throw "MAP-8X packet script missing .local refusal" }
Write-Output "OK: packet script contains .local refusal language"

Write-Output ""
Write-Output "--- MAP-8X real transition structure result tests ---"
& powershell -ExecutionPolicy Bypass -File $map8xResultTests
if ($LASTEXITCODE -ne 0) { throw "MAP-8X real transition structure result tests failed." }

Write-Output ""
Write-Output "--- MAP-8W IGMB transition structure analysis ---"
$map8wDoc          = Join-Path $repoRoot 'docs\MAP_8W_IGMB_TRANSITION_STRUCTURE_ANALYSIS.md'
$map8wInspector    = Join-Path $repoRoot 'scripts\inspect-build42-igmb-transition-structure.ps1'
$map8wInspTests    = Join-Path $repoRoot 'scripts\test-build42-igmb-transition-structure.ps1'
$map8wPacketScript = Join-Path $repoRoot 'scripts\prepare-build42-map8w-transition-structure-result-packet.ps1'
$map8wResultTests  = Join-Path $repoRoot 'scripts\test-build42-map8w-transition-structure-result.ps1'
if (-not (Test-Path -LiteralPath $map8wDoc))          { throw "MAP-8W doc missing" }
Write-Output "OK: docs\MAP_8W_IGMB_TRANSITION_STRUCTURE_ANALYSIS.md"
if (-not (Test-Path -LiteralPath $map8wInspector))    { throw "MAP-8W inspector missing" }
Write-Output "OK: scripts\inspect-build42-igmb-transition-structure.ps1"
if (-not (Test-Path -LiteralPath $map8wInspTests))    { throw "MAP-8W inspector tests missing" }
Write-Output "OK: scripts\test-build42-igmb-transition-structure.ps1"
if (-not (Test-Path -LiteralPath $map8wPacketScript)) { throw "MAP-8W packet script missing" }
Write-Output "OK: scripts\prepare-build42-map8w-transition-structure-result-packet.ps1"
if (-not (Test-Path -LiteralPath $map8wResultTests))  { throw "MAP-8W result tests missing" }
Write-Output "OK: scripts\test-build42-map8w-transition-structure-result.ps1"
$map8wDocContent = Get-Content -LiteralPath $map8wDoc -Raw
if ($map8wDocContent -notmatch 'MAP8W_IGMB_TRANSITION_STRUCTURE_ANALYSIS_APPROVED') { throw "MAP-8W doc missing MAP8W_IGMB_TRANSITION_STRUCTURE_ANALYSIS_APPROVED" }
Write-Output "OK: doc contains MAP8W_IGMB_TRANSITION_STRUCTURE_ANALYSIS_APPROVED"
if ($map8wDocContent -notmatch 'BINARY_WRITER_GATE_STILL_CLOSED') { throw "MAP-8W doc missing BINARY_WRITER_GATE_STILL_CLOSED" }
Write-Output "OK: doc contains BINARY_WRITER_GATE_STILL_CLOSED"
if ($map8wDocContent -notmatch 'PUBLIC_PLAYABLE_CLAIM_ALLOWED=false') { throw "MAP-8W doc missing PUBLIC_PLAYABLE_CLAIM_ALLOWED=false" }
Write-Output "OK: doc contains PUBLIC_PLAYABLE_CLAIM_ALLOWED=false"
$map8wInspContent = Get-Content -LiteralPath $map8wInspector -Raw
if ($map8wInspContent -notmatch '\.local') { throw "MAP-8W inspector missing .local refusal" }
Write-Output "OK: inspector contains .local refusal language"

Write-Output ""
Write-Output "--- MAP-8W IGMB transition structure inspector tests ---"
& powershell -ExecutionPolicy Bypass -File $map8wInspTests
if ($LASTEXITCODE -ne 0) { throw "MAP-8W IGMB transition structure inspector tests failed." }

Write-Output ""
Write-Output "--- MAP-8W IGMB transition structure result tests ---"
& powershell -ExecutionPolicy Bypass -File $map8wResultTests
if ($LASTEXITCODE -ne 0) { throw "MAP-8W IGMB transition structure result tests failed." }

Write-Output ""
Write-Output "--- MAP-8V Real first non-FF transition result ---"
$map8vDoc          = Join-Path $repoRoot 'docs\MAP_8V_REAL_FIRST_NON_FF_TRANSITION_RESULT.md'
$map8vPacketScript = Join-Path $repoRoot 'scripts\prepare-build42-map8v-real-first-non-ff-transition-result-packet.ps1'
$map8vResultTests  = Join-Path $repoRoot 'scripts\test-build42-map8v-real-first-non-ff-transition-result.ps1'
if (-not (Test-Path -LiteralPath $map8vDoc))          { throw "MAP-8V doc missing" }
Write-Output "OK: docs\MAP_8V_REAL_FIRST_NON_FF_TRANSITION_RESULT.md"
if (-not (Test-Path -LiteralPath $map8vPacketScript)) { throw "MAP-8V packet script missing" }
Write-Output "OK: scripts\prepare-build42-map8v-real-first-non-ff-transition-result-packet.ps1"
if (-not (Test-Path -LiteralPath $map8vResultTests))  { throw "MAP-8V result tests missing" }
Write-Output "OK: scripts\test-build42-map8v-real-first-non-ff-transition-result.ps1"
$map8vDocContent = Get-Content -LiteralPath $map8vDoc -Raw
if ($map8vDocContent -notmatch 'MAP8V_REAL_FIRST_NON_FF_TRANSITION_RESULT_RECORDED') { throw "MAP-8V doc missing MAP8V_REAL_FIRST_NON_FF_TRANSITION_RESULT_RECORDED" }
Write-Output "OK: doc contains MAP8V_REAL_FIRST_NON_FF_TRANSITION_RESULT_RECORDED"
if ($map8vDocContent -notmatch 'BINARY_WRITER_GATE_STILL_CLOSED') { throw "MAP-8V doc missing BINARY_WRITER_GATE_STILL_CLOSED" }
Write-Output "OK: doc contains BINARY_WRITER_GATE_STILL_CLOSED"
if ($map8vDocContent -notmatch 'PUBLIC_PLAYABLE_CLAIM_ALLOWED=false') { throw "MAP-8V doc missing PUBLIC_PLAYABLE_CLAIM_ALLOWED=false" }
Write-Output "OK: doc contains PUBLIC_PLAYABLE_CLAIM_ALLOWED=false"

Write-Output ""
Write-Output "--- MAP-8V real first non-FF transition result tests ---"
& powershell -ExecutionPolicy Bypass -File $map8vResultTests
if ($LASTEXITCODE -ne 0) { throw "MAP-8V real first non-FF transition result tests failed." }

Write-Output ""
Write-Output "--- MAP-8U First non-FF transition scan ---"
$map8uDoc          = Join-Path $repoRoot 'docs\MAP_8U_FIRST_NON_FF_TRANSITION_SCAN.md'
$map8uInspector    = Join-Path $repoRoot 'scripts\inspect-build42-igmb-first-non-ff-transition.ps1'
$map8uInspectorTests = Join-Path $repoRoot 'scripts\test-build42-igmb-first-non-ff-transition.ps1'
$map8uPacketScript = Join-Path $repoRoot 'scripts\prepare-build42-map8u-first-non-ff-transition-result-packet.ps1'
$map8uResultTests  = Join-Path $repoRoot 'scripts\test-build42-map8u-first-non-ff-transition-result.ps1'
if (-not (Test-Path -LiteralPath $map8uDoc))          { throw "MAP-8U doc missing" }
Write-Output "OK: docs\MAP_8U_FIRST_NON_FF_TRANSITION_SCAN.md"
if (-not (Test-Path -LiteralPath $map8uInspector))    { throw "MAP-8U inspector missing" }
Write-Output "OK: scripts\inspect-build42-igmb-first-non-ff-transition.ps1"
if (-not (Test-Path -LiteralPath $map8uInspectorTests)) { throw "MAP-8U inspector tests missing" }
Write-Output "OK: scripts\test-build42-igmb-first-non-ff-transition.ps1"
if (-not (Test-Path -LiteralPath $map8uPacketScript)) { throw "MAP-8U packet script missing" }
Write-Output "OK: scripts\prepare-build42-map8u-first-non-ff-transition-result-packet.ps1"
if (-not (Test-Path -LiteralPath $map8uResultTests))  { throw "MAP-8U result tests missing" }
Write-Output "OK: scripts\test-build42-map8u-first-non-ff-transition-result.ps1"
$map8uDocContent = Get-Content -LiteralPath $map8uDoc -Raw
if ($map8uDocContent -notmatch 'MAP8U_FIRST_NON_FF_TRANSITION_SCAN_APPROVED') { throw "MAP-8U doc missing MAP8U_FIRST_NON_FF_TRANSITION_SCAN_APPROVED" }
Write-Output "OK: doc contains MAP8U_FIRST_NON_FF_TRANSITION_SCAN_APPROVED"
if ($map8uDocContent -notmatch 'BINARY_WRITER_GATE_STILL_CLOSED') { throw "MAP-8U doc missing BINARY_WRITER_GATE_STILL_CLOSED" }
Write-Output "OK: doc contains BINARY_WRITER_GATE_STILL_CLOSED"
if ($map8uDocContent -notmatch 'PUBLIC_PLAYABLE_CLAIM_ALLOWED=false') { throw "MAP-8U doc missing PUBLIC_PLAYABLE_CLAIM_ALLOWED=false" }
Write-Output "OK: doc contains PUBLIC_PLAYABLE_CLAIM_ALLOWED=false"

Write-Output ""
Write-Output "--- MAP-8U first non-FF transition inspector tests ---"
& powershell -ExecutionPolicy Bypass -File $map8uInspectorTests
if ($LASTEXITCODE -ne 0) { throw "MAP-8U first non-FF transition inspector tests failed." }

Write-Output ""
Write-Output "--- MAP-8U first non-FF transition result tests ---"
& powershell -ExecutionPolicy Bypass -File $map8uResultTests
if ($LASTEXITCODE -ne 0) { throw "MAP-8U first non-FF transition result tests failed." }

Write-Output ""
Write-Output "--- MAP-8T Real cell boundary FF sentinel result ---"
$map8tDoc          = Join-Path $repoRoot 'docs\MAP_8T_REAL_CELL_BOUNDARY_FF_SENTINEL_RESULT.md'
$map8tPacketScript = Join-Path $repoRoot 'scripts\prepare-build42-map8t-real-cell-boundary-result-packet.ps1'
$map8tResultTests  = Join-Path $repoRoot 'scripts\test-build42-map8t-real-cell-boundary-result.ps1'
if (-not (Test-Path -LiteralPath $map8tDoc))          { throw "MAP-8T doc missing" }
Write-Output "OK: docs\MAP_8T_REAL_CELL_BOUNDARY_FF_SENTINEL_RESULT.md"
if (-not (Test-Path -LiteralPath $map8tPacketScript)) { throw "MAP-8T packet script missing" }
Write-Output "OK: scripts\prepare-build42-map8t-real-cell-boundary-result-packet.ps1"
if (-not (Test-Path -LiteralPath $map8tResultTests))  { throw "MAP-8T result tests missing" }
Write-Output "OK: scripts\test-build42-map8t-real-cell-boundary-result.ps1"
$map8tDocContent = Get-Content -LiteralPath $map8tDoc -Raw
if ($map8tDocContent -notmatch 'MAP8T_REAL_CELL_BOUNDARY_FF_SENTINEL_RESULT_RECORDED') { throw "MAP-8T doc missing MAP8T_REAL_CELL_BOUNDARY_FF_SENTINEL_RESULT_RECORDED" }
Write-Output "OK: doc contains MAP8T_REAL_CELL_BOUNDARY_FF_SENTINEL_RESULT_RECORDED"
if ($map8tDocContent -notmatch 'BINARY_WRITER_GATE_STILL_CLOSED') { throw "MAP-8T doc missing BINARY_WRITER_GATE_STILL_CLOSED" }
Write-Output "OK: doc contains BINARY_WRITER_GATE_STILL_CLOSED"
if ($map8tDocContent -notmatch 'PUBLIC_PLAYABLE_CLAIM_ALLOWED=false') { throw "MAP-8T doc missing PUBLIC_PLAYABLE_CLAIM_ALLOWED=false" }
Write-Output "OK: doc contains PUBLIC_PLAYABLE_CLAIM_ALLOWED=false"

Write-Output ""
Write-Output "--- MAP-8T real cell boundary result tests ---"
& powershell -ExecutionPolicy Bypass -File $map8tResultTests
if ($LASTEXITCODE -ne 0) { throw "MAP-8T real cell boundary result tests failed." }

Write-Output ""
Write-Output "--- MAP-8S IGMB cell boundary research ---"
$map8sDoc          = Join-Path $repoRoot 'docs\MAP_8S_IGMB_CELL_INDEX_BOUNDARY_RESEARCH.md'
$map8sInspector    = Join-Path $repoRoot 'scripts\inspect-build42-igmb-cell-boundary.ps1'
$map8sPacketScript = Join-Path $repoRoot 'scripts\prepare-build42-map8s-cell-boundary-result-packet.ps1'
$map8sInspTests    = Join-Path $repoRoot 'scripts\test-build42-igmb-cell-boundary.ps1'
$map8sResultTests  = Join-Path $repoRoot 'scripts\test-build42-map8s-cell-boundary-result.ps1'
if (-not (Test-Path -LiteralPath $map8sDoc))          { throw "MAP-8S doc missing" }
Write-Output "OK: docs\MAP_8S_IGMB_CELL_INDEX_BOUNDARY_RESEARCH.md"
if (-not (Test-Path -LiteralPath $map8sInspector))    { throw "MAP-8S inspector missing" }
Write-Output "OK: scripts\inspect-build42-igmb-cell-boundary.ps1"
if (-not (Test-Path -LiteralPath $map8sPacketScript)) { throw "MAP-8S packet script missing" }
Write-Output "OK: scripts\prepare-build42-map8s-cell-boundary-result-packet.ps1"
if (-not (Test-Path -LiteralPath $map8sInspTests))    { throw "MAP-8S inspector tests missing" }
Write-Output "OK: scripts\test-build42-igmb-cell-boundary.ps1"
if (-not (Test-Path -LiteralPath $map8sResultTests))  { throw "MAP-8S result tests missing" }
Write-Output "OK: scripts\test-build42-map8s-cell-boundary-result.ps1"
$map8sDocContent = Get-Content -LiteralPath $map8sDoc -Raw
if ($map8sDocContent -notmatch 'MAP8S_IGMB_CELL_BOUNDARY_RESEARCH_DEFINED') { throw "MAP-8S doc missing MAP8S_IGMB_CELL_BOUNDARY_RESEARCH_DEFINED" }
Write-Output "OK: doc contains MAP8S_IGMB_CELL_BOUNDARY_RESEARCH_DEFINED"
if ($map8sDocContent -notmatch 'BINARY_WRITER_GATE_STILL_CLOSED') { throw "MAP-8S doc missing BINARY_WRITER_GATE_STILL_CLOSED" }
Write-Output "OK: doc contains BINARY_WRITER_GATE_STILL_CLOSED"
if ($map8sDocContent -notmatch 'PUBLIC_PLAYABLE_CLAIM_ALLOWED=false') { throw "MAP-8S doc missing PUBLIC_PLAYABLE_CLAIM_ALLOWED=false" }
Write-Output "OK: doc contains PUBLIC_PLAYABLE_CLAIM_ALLOWED=false"
$map8sInspContent = Get-Content -LiteralPath $map8sInspector -Raw
if ($map8sInspContent -notmatch '\.local') { throw "MAP-8S inspector missing .local refusal" }
Write-Output "OK: inspector contains .local refusal language"

Write-Output ""
Write-Output "--- MAP-8S IGMB cell boundary inspector tests ---"
& powershell -ExecutionPolicy Bypass -File $map8sInspTests
if ($LASTEXITCODE -ne 0) { throw "MAP-8S IGMB cell boundary inspector tests failed." }

Write-Output ""
Write-Output "--- MAP-8S cell boundary result tests ---"
& powershell -ExecutionPolicy Bypass -File $map8sResultTests
if ($LASTEXITCODE -ne 0) { throw "MAP-8S cell boundary result tests failed." }

Write-Output ""
Write-Output "--- MAP-8R Real IGMB structure result ---"
$map8rDoc          = Join-Path $repoRoot 'docs\MAP_8R_REAL_IGMB_STRUCTURE_RESULT.md'
$map8rPacketScript = Join-Path $repoRoot 'scripts\prepare-build42-map8r-real-igmb-structure-result-packet.ps1'
$map8rTests        = Join-Path $repoRoot 'scripts\test-build42-map8r-real-igmb-structure-result.ps1'
if (-not (Test-Path -LiteralPath $map8rDoc))          { throw "MAP-8R doc missing" }
Write-Output "OK: docs\MAP_8R_REAL_IGMB_STRUCTURE_RESULT.md"
if (-not (Test-Path -LiteralPath $map8rPacketScript)) { throw "MAP-8R packet script missing" }
Write-Output "OK: scripts\prepare-build42-map8r-real-igmb-structure-result-packet.ps1"
if (-not (Test-Path -LiteralPath $map8rTests))        { throw "MAP-8R tests missing" }
Write-Output "OK: scripts\test-build42-map8r-real-igmb-structure-result.ps1"
$map8rDocContent = Get-Content -LiteralPath $map8rDoc -Raw
if ($map8rDocContent -notmatch 'MAP8R_REAL_IGMB_STRUCTURE_RESULT_RECORDED') { throw "MAP-8R doc missing MAP8R_REAL_IGMB_STRUCTURE_RESULT_RECORDED" }
Write-Output "OK: doc contains MAP8R_REAL_IGMB_STRUCTURE_RESULT_RECORDED"
if ($map8rDocContent -notmatch 'BINARY_WRITER_GATE_STILL_CLOSED') { throw "MAP-8R doc missing BINARY_WRITER_GATE_STILL_CLOSED" }
Write-Output "OK: doc contains BINARY_WRITER_GATE_STILL_CLOSED"
if ($map8rDocContent -notmatch 'PUBLIC_PLAYABLE_CLAIM_ALLOWED=false') { throw "MAP-8R doc missing PUBLIC_PLAYABLE_CLAIM_ALLOWED=false" }
Write-Output "OK: doc contains PUBLIC_PLAYABLE_CLAIM_ALLOWED=false"
$map8rPacketContent = Get-Content -LiteralPath $map8rPacketScript -Raw
if ($map8rPacketContent -notmatch '\.local') { throw "MAP-8R packet script missing .local refusal" }
Write-Output "OK: packet script contains .local refusal language"

Write-Output ""
Write-Output "--- MAP-8R real IGMB structure result tests ---"
& powershell -ExecutionPolicy Bypass -File $map8rTests
if ($LASTEXITCODE -ne 0) { throw "MAP-8R real IGMB structure result tests failed." }

Write-Output ""
Write-Output "--- MAP-8Q IGMB structure research ---"
$map8qDoc          = Join-Path $repoRoot 'docs\MAP_8Q_IGMB_STRUCTURE_RESEARCH.md'
$map8qInspector    = Join-Path $repoRoot 'scripts\inspect-build42-igmb-structure.ps1'
$map8qPacketScript = Join-Path $repoRoot 'scripts\prepare-build42-map8q-igmb-structure-result-packet.ps1'
$map8qTests        = Join-Path $repoRoot 'scripts\test-build42-igmb-structure.ps1'
$map8qResultTests  = Join-Path $repoRoot 'scripts\test-build42-map8q-igmb-structure-result.ps1'
if (-not (Test-Path -LiteralPath $map8qDoc))          { throw "MAP-8Q doc missing" }
Write-Output "OK: docs\MAP_8Q_IGMB_STRUCTURE_RESEARCH.md"
if (-not (Test-Path -LiteralPath $map8qInspector))    { throw "MAP-8Q inspector missing" }
Write-Output "OK: scripts\inspect-build42-igmb-structure.ps1"
if (-not (Test-Path -LiteralPath $map8qPacketScript)) { throw "MAP-8Q packet script missing" }
Write-Output "OK: scripts\prepare-build42-map8q-igmb-structure-result-packet.ps1"
if (-not (Test-Path -LiteralPath $map8qTests))        { throw "MAP-8Q inspector tests missing" }
Write-Output "OK: scripts\test-build42-igmb-structure.ps1"
if (-not (Test-Path -LiteralPath $map8qResultTests))  { throw "MAP-8Q result tests missing" }
Write-Output "OK: scripts\test-build42-map8q-igmb-structure-result.ps1"
$map8qDocContent = Get-Content -LiteralPath $map8qDoc -Raw
if ($map8qDocContent -notmatch 'MAP8Q_IGMB_STRUCTURE_RESEARCH_DEFINED') { throw "MAP-8Q doc missing MAP8Q_IGMB_STRUCTURE_RESEARCH_DEFINED" }
Write-Output "OK: doc contains MAP8Q_IGMB_STRUCTURE_RESEARCH_DEFINED"
if ($map8qDocContent -notmatch 'BINARY_WRITER_GATE_STILL_CLOSED') { throw "MAP-8Q doc missing BINARY_WRITER_GATE_STILL_CLOSED" }
Write-Output "OK: doc contains BINARY_WRITER_GATE_STILL_CLOSED"
if ($map8qDocContent -notmatch 'PUBLIC_PLAYABLE_CLAIM_ALLOWED=false') { throw "MAP-8Q doc missing PUBLIC_PLAYABLE_CLAIM_ALLOWED=false" }
Write-Output "OK: doc contains PUBLIC_PLAYABLE_CLAIM_ALLOWED=false"
$map8qInspContent = Get-Content -LiteralPath $map8qInspector -Raw
if ($map8qInspContent -notmatch '\.local') { throw "MAP-8Q inspector missing .local refusal" }
Write-Output "OK: inspector contains .local refusal language"

Write-Output ""
Write-Output "--- MAP-8Q IGMB structure inspector tests ---"
& powershell -ExecutionPolicy Bypass -File $map8qTests
if ($LASTEXITCODE -ne 0) { throw "MAP-8Q IGMB structure inspector tests failed." }

Write-Output ""
Write-Output "--- MAP-8Q IGMB structure result tests ---"
& powershell -ExecutionPolicy Bypass -File $map8qResultTests
if ($LASTEXITCODE -ne 0) { throw "MAP-8Q IGMB structure result tests failed." }

Write-Output ""
Write-Output "--- MAP-8P IGMB worldmap bin header result ---"
$map8pDoc          = Join-Path $repoRoot 'docs\MAP_8P_IGMB_WORLDMAP_BIN_HEADER_RESULT.md'
$map8pPacketScript = Join-Path $repoRoot 'scripts\prepare-build42-map8p-igmb-header-result-packet.ps1'
$map8pTests        = Join-Path $repoRoot 'scripts\test-build42-map8p-igmb-header-result.ps1'
if (-not (Test-Path -LiteralPath $map8pDoc))          { throw "MAP-8P doc missing" }
Write-Output "OK: docs\MAP_8P_IGMB_WORLDMAP_BIN_HEADER_RESULT.md"
if (-not (Test-Path -LiteralPath $map8pPacketScript)) { throw "MAP-8P packet script missing" }
Write-Output "OK: scripts\prepare-build42-map8p-igmb-header-result-packet.ps1"
if (-not (Test-Path -LiteralPath $map8pTests))        { throw "MAP-8P tests missing" }
Write-Output "OK: scripts\test-build42-map8p-igmb-header-result.ps1"
$map8pDocContent = Get-Content -LiteralPath $map8pDoc -Raw
if ($map8pDocContent -notmatch 'MAP8P_IGMB_WORLDMAP_BIN_HEADER_RESULT_RECORDED') { throw "MAP-8P doc missing MAP8P_IGMB_WORLDMAP_BIN_HEADER_RESULT_RECORDED" }
Write-Output "OK: doc contains MAP8P_IGMB_WORLDMAP_BIN_HEADER_RESULT_RECORDED"
if ($map8pDocContent -notmatch 'BINARY_WRITER_GATE_STILL_CLOSED') { throw "MAP-8P doc missing BINARY_WRITER_GATE_STILL_CLOSED" }
Write-Output "OK: doc contains BINARY_WRITER_GATE_STILL_CLOSED"
if ($map8pDocContent -notmatch 'PUBLIC_PLAYABLE_CLAIM_ALLOWED=false') { throw "MAP-8P doc missing PUBLIC_PLAYABLE_CLAIM_ALLOWED=false" }
Write-Output "OK: doc contains PUBLIC_PLAYABLE_CLAIM_ALLOWED=false"
$map8pPacketContent = Get-Content -LiteralPath $map8pPacketScript -Raw
if ($map8pPacketContent -notmatch '\.local') { throw "MAP-8P packet script missing .local refusal" }
Write-Output "OK: packet script contains .local refusal language"

Write-Output ""
Write-Output "--- MAP-8P IGMB header result tests ---"
& powershell -ExecutionPolicy Bypass -File $map8pTests
if ($LASTEXITCODE -ne 0) { throw "MAP-8P IGMB header result tests failed." }

Write-Output ""
Write-Output "--- MAP-8O Worldmap bin header inspection ---"
$map8oDoc          = Join-Path $repoRoot 'docs\MAP_8O_WORLDMAP_BIN_HEADER_INSPECTION.md'
$map8oInspector    = Join-Path $repoRoot 'scripts\inspect-build42-worldmap-bin-header.ps1'
$map8oPacketScript = Join-Path $repoRoot 'scripts\prepare-build42-map8o-header-result-packet.ps1'
$map8oTests        = Join-Path $repoRoot 'scripts\test-build42-worldmap-bin-header.ps1'
$map8oResultTests  = Join-Path $repoRoot 'scripts\test-build42-map8o-header-result.ps1'
if (-not (Test-Path -LiteralPath $map8oDoc))          { throw "MAP-8O doc missing" }
Write-Output "OK: docs\MAP_8O_WORLDMAP_BIN_HEADER_INSPECTION.md"
if (-not (Test-Path -LiteralPath $map8oInspector))    { throw "MAP-8O inspector missing" }
Write-Output "OK: scripts\inspect-build42-worldmap-bin-header.ps1"
if (-not (Test-Path -LiteralPath $map8oPacketScript)) { throw "MAP-8O packet script missing" }
Write-Output "OK: scripts\prepare-build42-map8o-header-result-packet.ps1"
if (-not (Test-Path -LiteralPath $map8oTests))        { throw "MAP-8O inspector tests missing" }
Write-Output "OK: scripts\test-build42-worldmap-bin-header.ps1"
if (-not (Test-Path -LiteralPath $map8oResultTests))  { throw "MAP-8O result tests missing" }
Write-Output "OK: scripts\test-build42-map8o-header-result.ps1"
$map8oDocContent = Get-Content -LiteralPath $map8oDoc -Raw
if ($map8oDocContent -notmatch 'MAP8O_WORLDMAP_XML_BIN_HEADER_INSPECTION_DEFINED') { throw "MAP-8O doc missing MAP8O_WORLDMAP_XML_BIN_HEADER_INSPECTION_DEFINED" }
Write-Output "OK: doc contains MAP8O_WORLDMAP_XML_BIN_HEADER_INSPECTION_DEFINED"
if ($map8oDocContent -notmatch 'BINARY_WRITER_GATE_STILL_CLOSED') { throw "MAP-8O doc missing BINARY_WRITER_GATE_STILL_CLOSED" }
Write-Output "OK: doc contains BINARY_WRITER_GATE_STILL_CLOSED"
if ($map8oDocContent -notmatch 'PUBLIC_PLAYABLE_CLAIM_ALLOWED=false') { throw "MAP-8O doc missing PUBLIC_PLAYABLE_CLAIM_ALLOWED=false" }
Write-Output "OK: doc contains PUBLIC_PLAYABLE_CLAIM_ALLOWED=false"
$map8oInspContent = Get-Content -LiteralPath $map8oInspector -Raw
if ($map8oInspContent -notmatch '\.local') { throw "MAP-8O inspector missing .local refusal" }
Write-Output "OK: inspector contains .local refusal language"

Write-Output ""
Write-Output "--- MAP-8O worldmap bin header inspector tests ---"
& powershell -ExecutionPolicy Bypass -File $map8oTests
if ($LASTEXITCODE -ne 0) { throw "MAP-8O worldmap bin header inspector tests failed." }

Write-Output ""
Write-Output "--- MAP-8O worldmap bin header result tests ---"
& powershell -ExecutionPolicy Bypass -File $map8oResultTests
if ($LASTEXITCODE -ne 0) { throw "MAP-8O worldmap bin header result tests failed." }

Write-Output ""
Write-Output "--- MAP-8N Worldmap bin presence discriminator result ---"
$map8nDoc          = Join-Path $repoRoot 'docs\MAP_8N_WORLDMAP_BIN_PRESENCE_RESULT.md'
$map8nPacketScript = Join-Path $repoRoot 'scripts\prepare-build42-map8n-presence-result-packet.ps1'
$map8nTests        = Join-Path $repoRoot 'scripts\test-build42-map8n-presence-result.ps1'
if (-not (Test-Path -LiteralPath $map8nDoc))          { throw "MAP-8N doc missing" }
Write-Output "OK: docs\MAP_8N_WORLDMAP_BIN_PRESENCE_RESULT.md"
if (-not (Test-Path -LiteralPath $map8nPacketScript)) { throw "MAP-8N packet script missing" }
Write-Output "OK: scripts\prepare-build42-map8n-presence-result-packet.ps1"
if (-not (Test-Path -LiteralPath $map8nTests))        { throw "MAP-8N tests missing" }
Write-Output "OK: scripts\test-build42-map8n-presence-result.ps1"
$map8nDocContent = Get-Content -LiteralPath $map8nDoc -Raw
if ($map8nDocContent -notmatch 'MAP8N_WORLDMAP_XML_BIN_PRESENCE_DISCRIMINATOR_CONFIRMED') { throw "MAP-8N doc missing MAP8N_WORLDMAP_XML_BIN_PRESENCE_DISCRIMINATOR_CONFIRMED" }
Write-Output "OK: doc contains MAP8N_WORLDMAP_XML_BIN_PRESENCE_DISCRIMINATOR_CONFIRMED"
if ($map8nDocContent -notmatch 'BINARY_WRITER_GATE_STILL_CLOSED') { throw "MAP-8N doc missing BINARY_WRITER_GATE_STILL_CLOSED" }
Write-Output "OK: doc contains BINARY_WRITER_GATE_STILL_CLOSED"
if ($map8nDocContent -notmatch 'PUBLIC_PLAYABLE_CLAIM_ALLOWED=false') { throw "MAP-8N doc missing PUBLIC_PLAYABLE_CLAIM_ALLOWED=false" }
Write-Output "OK: doc contains PUBLIC_PLAYABLE_CLAIM_ALLOWED=false"
$map8nPacketContent = Get-Content -LiteralPath $map8nPacketScript -Raw
if ($map8nPacketContent -notmatch '\.local') { throw "MAP-8N packet script missing .local refusal" }
Write-Output "OK: packet script contains .local refusal language"

Write-Output ""
Write-Output "--- MAP-8N worldmap bin presence result tests ---"
& powershell -ExecutionPolicy Bypass -File $map8nTests
if ($LASTEXITCODE -ne 0) { throw "MAP-8N worldmap bin presence result tests failed." }

Write-Output ""
Write-Output "--- MAP-8M Worldmap bin presence investigation plan ---"
$map8mDoc         = Join-Path $repoRoot 'docs\MAP_8M_WORLDMAP_BIN_INVESTIGATION_PLAN.md'
$map8mInspector   = Join-Path $repoRoot 'scripts\inspect-build42-worldmap-bin-presence.ps1'
$map8mTests       = Join-Path $repoRoot 'scripts\test-build42-worldmap-bin-presence.ps1'
if (-not (Test-Path -LiteralPath $map8mDoc))       { throw "MAP-8M doc missing" }
Write-Output "OK: docs\MAP_8M_WORLDMAP_BIN_INVESTIGATION_PLAN.md"
if (-not (Test-Path -LiteralPath $map8mInspector)) { throw "MAP-8M inspector missing" }
Write-Output "OK: scripts\inspect-build42-worldmap-bin-presence.ps1"
if (-not (Test-Path -LiteralPath $map8mTests))     { throw "MAP-8M tests missing" }
Write-Output "OK: scripts\test-build42-worldmap-bin-presence.ps1"
$map8mDocContent = Get-Content -LiteralPath $map8mDoc -Raw
if ($map8mDocContent -notmatch 'MAP8M_WORLDMAP_BIN_INVESTIGATION_PLAN_DEFINED') { throw "MAP-8M doc missing MAP8M_WORLDMAP_BIN_INVESTIGATION_PLAN_DEFINED" }
Write-Output "OK: doc contains MAP8M_WORLDMAP_BIN_INVESTIGATION_PLAN_DEFINED"
if ($map8mDocContent -notmatch 'BINARY_WRITER_GATE_STILL_CLOSED') { throw "MAP-8M doc missing BINARY_WRITER_GATE_STILL_CLOSED" }
Write-Output "OK: doc contains BINARY_WRITER_GATE_STILL_CLOSED"
if ($map8mDocContent -notmatch 'PUBLIC_PLAYABLE_CLAIM_ALLOWED=false') { throw "MAP-8M doc missing PUBLIC_PLAYABLE_CLAIM_ALLOWED=false" }
Write-Output "OK: doc contains PUBLIC_PLAYABLE_CLAIM_ALLOWED=false"
$map8mInspContent = Get-Content -LiteralPath $map8mInspector -Raw
if ($map8mInspContent -notmatch '\.local') { throw "MAP-8M inspector missing .local refusal" }
Write-Output "OK: inspector contains .local refusal language"

Write-Output ""
Write-Output "--- MAP-8M worldmap bin presence tests ---"
& powershell -ExecutionPolicy Bypass -File $map8mTests
if ($LASTEXITCODE -ne 0) { throw "MAP-8M worldmap bin presence tests failed." }

Write-Output ""
Write-Output "--- MAP-8L Worldmap XML runtime result ---"
$map8lRtDoc          = Join-Path $repoRoot 'docs\MAP_8L_RUNTIME_RESULT.md'
$map8lRtPacketScript = Join-Path $repoRoot 'scripts\prepare-build42-map8l-runtime-result-packet.ps1'
$map8lRtTests        = Join-Path $repoRoot 'scripts\test-build42-map8l-runtime-result.ps1'
if (-not (Test-Path -LiteralPath $map8lRtDoc))          { throw "MAP-8L runtime result doc missing" }
Write-Output "OK: docs\MAP_8L_RUNTIME_RESULT.md"
if (-not (Test-Path -LiteralPath $map8lRtPacketScript)) { throw "MAP-8L runtime result packet script missing" }
Write-Output "OK: scripts\prepare-build42-map8l-runtime-result-packet.ps1"
if (-not (Test-Path -LiteralPath $map8lRtTests))        { throw "MAP-8L runtime result tests missing" }
Write-Output "OK: scripts\test-build42-map8l-runtime-result.ps1"
$map8lRtDocContent = Get-Content -LiteralPath $map8lRtDoc -Raw
if ($map8lRtDocContent -notmatch 'MAP8L_WORLDMAP_XML_FAILED_TO_MOUNT') { throw "MAP-8L runtime result doc missing MAP8L_WORLDMAP_XML_FAILED_TO_MOUNT" }
Write-Output "OK: doc contains MAP8L_WORLDMAP_XML_FAILED_TO_MOUNT"
if ($map8lRtDocContent -notmatch 'BINARY_WRITER_GATE_STILL_CLOSED') { throw "MAP-8L runtime result doc missing BINARY_WRITER_GATE_STILL_CLOSED" }
Write-Output "OK: doc contains BINARY_WRITER_GATE_STILL_CLOSED"
if ($map8lRtDocContent -notmatch 'PUBLIC_PLAYABLE_CLAIM_ALLOWED=false') { throw "MAP-8L runtime result doc missing PUBLIC_PLAYABLE_CLAIM_ALLOWED=false" }
Write-Output "OK: doc contains PUBLIC_PLAYABLE_CLAIM_ALLOWED=false"
$map8lRtPacketContent = Get-Content -LiteralPath $map8lRtPacketScript -Raw
if ($map8lRtPacketContent -notmatch '\.local') { throw "MAP-8L runtime result packet script missing .local refusal" }
Write-Output "OK: packet script contains .local refusal language"

Write-Output ""
Write-Output "--- MAP-8L worldmap xml runtime result tests ---"
& powershell -ExecutionPolicy Bypass -File $map8lRtTests
if ($LASTEXITCODE -ne 0) { throw "MAP-8L worldmap xml runtime result tests failed." }

Write-Output ""
Write-Output "--- MAP-8L Worldmap XML substantial candidate ---"
$map8lDoc    = Join-Path $repoRoot 'docs\MAP_8L_WORLDMAP_XML_SUBSTANTIAL_CANDIDATE.md'
$map8lScript = Join-Path $repoRoot 'scripts\prepare-build42-map8l-worldmap-xml-candidate.ps1'
$map8lTests  = Join-Path $repoRoot 'scripts\test-build42-map8l-worldmap-xml-candidate.ps1'
if (-not (Test-Path -LiteralPath $map8lDoc))    { throw "MAP-8L doc missing" }
Write-Output "OK: docs\MAP_8L_WORLDMAP_XML_SUBSTANTIAL_CANDIDATE.md"
if (-not (Test-Path -LiteralPath $map8lScript)) { throw "MAP-8L script missing" }
Write-Output "OK: scripts\prepare-build42-map8l-worldmap-xml-candidate.ps1"
if (-not (Test-Path -LiteralPath $map8lTests))  { throw "MAP-8L tests missing" }
Write-Output "OK: scripts\test-build42-map8l-worldmap-xml-candidate.ps1"
$map8lDocContent = Get-Content -LiteralPath $map8lDoc -Raw
if ($map8lDocContent -notmatch 'MAP8L_WORLDMAP_XML_SUBSTANTIAL_CANDIDATE_STAGED') { throw "MAP-8L doc missing MAP8L_WORLDMAP_XML_SUBSTANTIAL_CANDIDATE_STAGED" }
Write-Output "OK: doc contains MAP8L_WORLDMAP_XML_SUBSTANTIAL_CANDIDATE_STAGED"
if ($map8lDocContent -notmatch 'BINARY_WRITER_GATE_STILL_CLOSED') { throw "MAP-8L doc missing BINARY_WRITER_GATE_STILL_CLOSED" }
Write-Output "OK: doc contains BINARY_WRITER_GATE_STILL_CLOSED"
if ($map8lDocContent -notmatch 'PUBLIC_PLAYABLE_CLAIM_ALLOWED=false') { throw "MAP-8L doc missing PUBLIC_PLAYABLE_CLAIM_ALLOWED=false" }
Write-Output "OK: doc contains PUBLIC_PLAYABLE_CLAIM_ALLOWED=false"
$map8lScriptContent = Get-Content -LiteralPath $map8lScript -Raw
if ($map8lScriptContent -notmatch '\.local') { throw "MAP-8L script missing .local refusal" }
Write-Output "OK: script contains .local refusal language"

Write-Output ""
Write-Output "--- MAP-8L worldmap xml substantial candidate tests ---"
& powershell -ExecutionPolicy Bypass -File $map8lTests
if ($LASTEXITCODE -ne 0) { throw "MAP-8L worldmap xml substantial candidate tests failed." }

Write-Output ""
Write-Output "--- MAP-8K Parent map metadata contract comparator ---"
$map8kDoc         = Join-Path $repoRoot 'docs\MAP_8K_PARENT_METADATA_CONTRACT_COMPARATOR.md'
$map8kComparator  = Join-Path $repoRoot 'scripts\inspect-build42-parent-map-metadata-contract.ps1'
$map8kTests       = Join-Path $repoRoot 'scripts\test-build42-parent-map-metadata-contract.ps1'
if (-not (Test-Path -LiteralPath $map8kDoc))        { throw "MAP-8K doc missing" }
Write-Output "OK: docs\MAP_8K_PARENT_METADATA_CONTRACT_COMPARATOR.md"
if (-not (Test-Path -LiteralPath $map8kComparator)) { throw "MAP-8K comparator missing" }
Write-Output "OK: scripts\inspect-build42-parent-map-metadata-contract.ps1"
if (-not (Test-Path -LiteralPath $map8kTests))      { throw "MAP-8K tests missing" }
Write-Output "OK: scripts\test-build42-parent-map-metadata-contract.ps1"
$map8kDocContent = Get-Content -LiteralPath $map8kDoc -Raw
if ($map8kDocContent -notmatch 'MAP8K_PARENT_METADATA_CONTRACT_COMPARATOR_DEFINED') { throw "MAP-8K doc missing MAP8K_PARENT_METADATA_CONTRACT_COMPARATOR_DEFINED" }
Write-Output "OK: doc contains MAP8K_PARENT_METADATA_CONTRACT_COMPARATOR_DEFINED"
if ($map8kDocContent -notmatch 'BINARY_WRITER_GATE_STILL_CLOSED') { throw "MAP-8K doc missing BINARY_WRITER_GATE_STILL_CLOSED" }
Write-Output "OK: doc contains BINARY_WRITER_GATE_STILL_CLOSED"
if ($map8kDocContent -notmatch 'PUBLIC_PLAYABLE_CLAIM_ALLOWED=false') { throw "MAP-8K doc missing PUBLIC_PLAYABLE_CLAIM_ALLOWED=false" }
Write-Output "OK: doc contains PUBLIC_PLAYABLE_CLAIM_ALLOWED=false"
if ($map8kDocContent -notmatch 'NO_BINARY_CONTENTS_READ') { throw "MAP-8K doc missing NO_BINARY_CONTENTS_READ" }
Write-Output "OK: doc contains NO_BINARY_CONTENTS_READ"
$map8kCompContent = Get-Content -LiteralPath $map8kComparator -Raw
if ($map8kCompContent -notmatch '\.local') { throw "MAP-8K comparator missing .local refusal" }
Write-Output "OK: comparator contains .local refusal language"

Write-Output ""
Write-Output "--- MAP-8K parent metadata contract comparator tests ---"
& powershell -ExecutionPolicy Bypass -File $map8kTests
if ($LASTEXITCODE -ne 0) { throw "MAP-8K parent metadata contract comparator tests failed." }

Write-Output ""
Write-Output "--- MAP-8I Dual spawnpoint keys runtime result ---"
$map8iDoc          = Join-Path $repoRoot 'docs\MAP_8I_DUAL_SPAWNPOINT_RUNTIME_RESULT.md'
$map8iPacketScript = Join-Path $repoRoot 'scripts\prepare-build42-map8i-runtime-result-packet.ps1'
$map8iTests        = Join-Path $repoRoot 'scripts\test-build42-map8i-runtime-result.ps1'
if (-not (Test-Path -LiteralPath $map8iDoc))          { throw "MAP-8I doc missing" }
Write-Output "OK: docs\MAP_8I_DUAL_SPAWNPOINT_RUNTIME_RESULT.md"
if (-not (Test-Path -LiteralPath $map8iPacketScript)) { throw "MAP-8I packet script missing" }
Write-Output "OK: scripts\prepare-build42-map8i-runtime-result-packet.ps1"
if (-not (Test-Path -LiteralPath $map8iTests))        { throw "MAP-8I tests missing" }
Write-Output "OK: scripts\test-build42-map8i-runtime-result.ps1"
$map8iDocContent = Get-Content -LiteralPath $map8iDoc -Raw
if ($map8iDocContent -notmatch 'MAP8I_SPAWNPOINT_FIXED_BUT_ISOMETAGRID_NOT_MOUNTED') { throw "MAP-8I doc missing MAP8I_SPAWNPOINT_FIXED_BUT_ISOMETAGRID_NOT_MOUNTED" }
Write-Output "OK: doc contains MAP8I_SPAWNPOINT_FIXED_BUT_ISOMETAGRID_NOT_MOUNTED"
if ($map8iDocContent -notmatch 'BINARY_WRITER_GATE_STILL_CLOSED') { throw "MAP-8I doc missing BINARY_WRITER_GATE_STILL_CLOSED" }
Write-Output "OK: doc contains BINARY_WRITER_GATE_STILL_CLOSED"
if ($map8iDocContent -notmatch 'PUBLIC_PLAYABLE_CLAIM_ALLOWED=false') { throw "MAP-8I doc missing PUBLIC_PLAYABLE_CLAIM_ALLOWED=false" }
Write-Output "OK: doc contains PUBLIC_PLAYABLE_CLAIM_ALLOWED=false"
if ($map8iDocContent -notmatch 'SPAWN_COORDINATE_MATCHES_35_27=true') { throw "MAP-8I doc missing SPAWN_COORDINATE_MATCHES_35_27=true" }
Write-Output "OK: doc contains SPAWN_COORDINATE_MATCHES_35_27=true"
$map8iPacketContent = Get-Content -LiteralPath $map8iPacketScript -Raw
if ($map8iPacketContent -notmatch '\.local') { throw "MAP-8I packet script missing .local refusal" }
Write-Output "OK: packet script contains .local refusal language"

Write-Output ""
Write-Output "--- MAP-8I dual spawnpoint runtime result tests ---"
& powershell -ExecutionPolicy Bypass -File $map8iTests
if ($LASTEXITCODE -ne 0) { throw "MAP-8I dual spawnpoint runtime result tests failed." }

Write-Output ""
Write-Output "--- MAP-8H Parent/child map contract probe ---"
$map8hDoc          = Join-Path $repoRoot 'docs\MAP_8H_PARENT_CHILD_CONTRACT_PROBE.md'
$map8hPacketScript = Join-Path $repoRoot 'scripts\prepare-build42-map8h-parent-child-contract-packet.ps1'
$map8hTests        = Join-Path $repoRoot 'scripts\test-build42-map8h-parent-child-contract.ps1'
if (-not (Test-Path -LiteralPath $map8hDoc))          { throw "MAP-8H doc missing" }
Write-Output "OK: docs\MAP_8H_PARENT_CHILD_CONTRACT_PROBE.md"
if (-not (Test-Path -LiteralPath $map8hPacketScript)) { throw "MAP-8H packet script missing" }
Write-Output "OK: scripts\prepare-build42-map8h-parent-child-contract-packet.ps1"
if (-not (Test-Path -LiteralPath $map8hTests))        { throw "MAP-8H tests missing" }
Write-Output "OK: scripts\test-build42-map8h-parent-child-contract.ps1"
$map8hDocContent = Get-Content -LiteralPath $map8hDoc -Raw
if ($map8hDocContent -notmatch 'MAP8H_PARENT_CHILD_CONTRACT_PROBE_STAGED') { throw "MAP-8H doc missing MAP8H_PARENT_CHILD_CONTRACT_PROBE_STAGED" }
Write-Output "OK: doc contains MAP8H_PARENT_CHILD_CONTRACT_PROBE_STAGED"
if ($map8hDocContent -notmatch 'BINARY_WRITER_GATE_STILL_CLOSED') { throw "MAP-8H doc missing BINARY_WRITER_GATE_STILL_CLOSED" }
Write-Output "OK: doc contains BINARY_WRITER_GATE_STILL_CLOSED"
if ($map8hDocContent -notmatch 'PUBLIC_PLAYABLE_CLAIM_ALLOWED=false') { throw "MAP-8H doc missing PUBLIC_PLAYABLE_CLAIM_ALLOWED=false" }
Write-Output "OK: doc contains PUBLIC_PLAYABLE_CLAIM_ALLOWED=false"
if ($map8hDocContent -notmatch 'NO_PROJECT_RUSSIA_FILES_COPIED') { throw "MAP-8H doc missing NO_PROJECT_RUSSIA_FILES_COPIED" }
Write-Output "OK: doc contains NO_PROJECT_RUSSIA_FILES_COPIED"
$map8hPacketContent = Get-Content -LiteralPath $map8hPacketScript -Raw
if ($map8hPacketContent -notmatch '\.local') { throw "MAP-8H packet script missing .local refusal" }
Write-Output "OK: packet script contains .local refusal language"

Write-Output ""
Write-Output "--- MAP-8H parent/child contract probe tests ---"
& powershell -ExecutionPolicy Bypass -File $map8hTests
if ($LASTEXITCODE -ne 0) { throw "MAP-8H parent/child contract probe tests failed." }

Write-Output ""
Write-Output "--- MAP-8G Known-working map contract comparator v2 ---"
$map8gDoc       = Join-Path $repoRoot 'docs\MAP_8G_KNOWN_WORKING_CONTRACT_COMPARATOR.md'
$map8gInspector = Join-Path $repoRoot 'scripts\inspect-build42-known-working-map-contract-v2.ps1'
if (-not (Test-Path -LiteralPath $map8gDoc))       { throw "MAP-8G doc missing" }
Write-Output "OK: docs\MAP_8G_KNOWN_WORKING_CONTRACT_COMPARATOR.md"
if (-not (Test-Path -LiteralPath $map8gInspector)) { throw "MAP-8G inspector missing" }
Write-Output "OK: scripts\inspect-build42-known-working-map-contract-v2.ps1"
$map8gDocContent  = Get-Content -LiteralPath $map8gDoc -Raw
if ($map8gDocContent -notmatch 'MAP8G_KNOWN_WORKING_CONTRACT_COMPARATOR_DEFINED') { throw "MAP-8G doc missing MAP8G_KNOWN_WORKING_CONTRACT_COMPARATOR_DEFINED" }
Write-Output "OK: doc contains MAP8G_KNOWN_WORKING_CONTRACT_COMPARATOR_DEFINED"
if ($map8gDocContent -notmatch 'BINARY_WRITER_GATE_STILL_CLOSED') { throw "MAP-8G doc missing BINARY_WRITER_GATE_STILL_CLOSED" }
Write-Output "OK: doc contains BINARY_WRITER_GATE_STILL_CLOSED"
if ($map8gDocContent -notmatch 'PUBLIC_PLAYABLE_CLAIM_ALLOWED=false') { throw "MAP-8G doc missing PUBLIC_PLAYABLE_CLAIM_ALLOWED=false" }
Write-Output "OK: doc contains PUBLIC_PLAYABLE_CLAIM_ALLOWED=false"
$map8gInspContent = Get-Content -LiteralPath $map8gInspector -Raw
if ($map8gInspContent -notmatch '\.local') { throw "MAP-8G inspector missing .local refusal" }
Write-Output "OK: inspector contains .local refusal language"
if ($map8gInspContent -notmatch 'build42-known-working-map-contract-v2') { throw "MAP-8G inspector missing output filename" }
Write-Output "OK: inspector contains output filename"

Write-Output ""
Write-Output "--- MAP-8F lots=self runtime result ---"
$map8fDoc          = Join-Path $repoRoot 'docs\MAP_8F_LOTS_SELF_RUNTIME_RESULT.md'
$map8fPacketScript = Join-Path $repoRoot 'scripts\prepare-build42-map8f-runtime-result-packet.ps1'
$map8fTests        = Join-Path $repoRoot 'scripts\test-build42-map8f-runtime-result.ps1'
if (-not (Test-Path -LiteralPath $map8fDoc))          { throw "MAP-8F doc missing" }
Write-Output "OK: docs\MAP_8F_LOTS_SELF_RUNTIME_RESULT.md"
if (-not (Test-Path -LiteralPath $map8fPacketScript)) { throw "MAP-8F packet script missing" }
Write-Output "OK: scripts\prepare-build42-map8f-runtime-result-packet.ps1"
if (-not (Test-Path -LiteralPath $map8fTests))        { throw "MAP-8F tests missing" }
Write-Output "OK: scripts\test-build42-map8f-runtime-result.ps1"
$map8fDocContent = Get-Content -LiteralPath $map8fDoc -Raw
if ($map8fDocContent -notmatch 'MAP8F_LOTS_SELF_VISIBLE_BUT_NOT_MOUNTED') { throw "MAP-8F doc missing MAP8F_LOTS_SELF_VISIBLE_BUT_NOT_MOUNTED" }
Write-Output "OK: doc contains MAP8F_LOTS_SELF_VISIBLE_BUT_NOT_MOUNTED"
if ($map8fDocContent -notmatch 'MAP8F_ISO_META_GRID_MAP_FOLDER_LIST_EMPTY') { throw "MAP-8F doc missing MAP8F_ISO_META_GRID_MAP_FOLDER_LIST_EMPTY" }
Write-Output "OK: doc contains MAP8F_ISO_META_GRID_MAP_FOLDER_LIST_EMPTY"
if ($map8fDocContent -notmatch 'PUBLIC_PLAYABLE_CLAIM_ALLOWED=false') { throw "MAP-8F doc missing PUBLIC_PLAYABLE_CLAIM_ALLOWED=false" }
Write-Output "OK: doc contains PUBLIC_PLAYABLE_CLAIM_ALLOWED=false"
if ($map8fDocContent -notmatch 'BINARY_WRITER_GATE_STILL_CLOSED') { throw "MAP-8F doc missing BINARY_WRITER_GATE_STILL_CLOSED" }
Write-Output "OK: doc contains BINARY_WRITER_GATE_STILL_CLOSED"
if ($map8fDocContent -notmatch 'known_working_build42_map_contract_comparator') { throw "MAP-8F doc missing next_branch sentinel" }
Write-Output "OK: doc contains next_branch sentinel"
$map8fPacketContent = Get-Content -LiteralPath $map8fPacketScript -Raw
if ($map8fPacketContent -notmatch '\.local') { throw "MAP-8F packet script missing .local refusal" }
Write-Output "OK: packet script contains .local refusal language"

Write-Output ""
Write-Output "--- MAP-8F lots=self runtime result tests ---"
& powershell -ExecutionPolicy Bypass -File $map8fTests
if ($LASTEXITCODE -ne 0) { throw "MAP-8F lots=self runtime result tests failed." }

Write-Output ""
Write-Output "--- MAP-8D No invalid worldmap bin stubs probe ---"
$map8dDoc          = Join-Path $repoRoot 'docs\MAP_8D_NO_INVALID_WORLDMAP_BIN_STUBS_PACKET.md'
$map8dPacketScript = Join-Path $repoRoot 'scripts\prepare-build42-map8d-no-invalid-worldmap-bin-packet.ps1'
$map8dTests        = Join-Path $repoRoot 'scripts\test-build42-map8d-no-invalid-worldmap-bin.ps1'
if (-not (Test-Path -LiteralPath $map8dDoc))          { throw "MAP-8D doc missing" }
Write-Output "OK: docs\MAP_8D_NO_INVALID_WORLDMAP_BIN_STUBS_PACKET.md"
if (-not (Test-Path -LiteralPath $map8dPacketScript)) { throw "MAP-8D packet script missing" }
Write-Output "OK: scripts\prepare-build42-map8d-no-invalid-worldmap-bin-packet.ps1"
if (-not (Test-Path -LiteralPath $map8dTests))        { throw "MAP-8D tests missing" }
Write-Output "OK: scripts\test-build42-map8d-no-invalid-worldmap-bin.ps1"
$map8dDocContent = Get-Content -LiteralPath $map8dDoc -Raw
if ($map8dDocContent -notmatch 'MAP8D_NO_INVALID_WORLDMAP_BIN_STUBS_PROBE_STAGED') { throw "MAP-8D doc missing MAP8D_NO_INVALID_WORLDMAP_BIN_STUBS_PROBE_STAGED" }
Write-Output "OK: doc contains MAP8D_NO_INVALID_WORLDMAP_BIN_STUBS_PROBE_STAGED"
if ($map8dDocContent -notmatch 'INVALID_WORLDMAP_BIN_STUBS_REMOVED') { throw "MAP-8D doc missing INVALID_WORLDMAP_BIN_STUBS_REMOVED" }
Write-Output "OK: doc contains INVALID_WORLDMAP_BIN_STUBS_REMOVED"
if ($map8dDocContent -notmatch 'BINARY_WRITER_GATE_STILL_CLOSED') { throw "MAP-8D doc missing BINARY_WRITER_GATE_STILL_CLOSED" }
Write-Output "OK: doc contains BINARY_WRITER_GATE_STILL_CLOSED"
if ($map8dDocContent -notmatch 'PUBLIC_PLAYABLE_CLAIM_ALLOWED=false') { throw "MAP-8D doc missing PUBLIC_PLAYABLE_CLAIM_ALLOWED=false" }
Write-Output "OK: doc contains PUBLIC_PLAYABLE_CLAIM_ALLOWED=false"
if ($map8dDocContent -notmatch 'STREETS_XML_BIN_REMOVED') { throw "MAP-8D doc missing STREETS_XML_BIN_REMOVED" }
Write-Output "OK: doc contains STREETS_XML_BIN_REMOVED"
$map8dPacketContent = Get-Content -LiteralPath $map8dPacketScript -Raw
if ($map8dPacketContent -notmatch '\.local') { throw "MAP-8D packet script missing .local refusal" }
Write-Output "OK: packet script contains .local refusal language"

Write-Output ""
Write-Output "--- MAP-8D no invalid worldmap bin probe tests ---"
& powershell -ExecutionPolicy Bypass -File $map8dTests
if ($LASTEXITCODE -ne 0) { throw "MAP-8D no invalid worldmap bin probe tests failed." }

Write-Output ""
Write-Output "--- MAP-8B Version-scoped media path runtime result ---"
$map8bDoc          = Join-Path $repoRoot 'docs\MAP_8B_VERSION_MEDIA_RUNTIME_RESULT.md'
$map8bPacketScript = Join-Path $repoRoot 'scripts\prepare-build42-map8b-runtime-result-packet.ps1'
$map8bTests        = Join-Path $repoRoot 'scripts\test-build42-map8b-runtime-result.ps1'
if (-not (Test-Path -LiteralPath $map8bDoc))          { throw "MAP-8B doc missing" }
Write-Output "OK: docs\MAP_8B_VERSION_MEDIA_RUNTIME_RESULT.md"
if (-not (Test-Path -LiteralPath $map8bPacketScript)) { throw "MAP-8B packet script missing" }
Write-Output "OK: scripts\prepare-build42-map8b-runtime-result-packet.ps1"
if (-not (Test-Path -LiteralPath $map8bTests))        { throw "MAP-8B tests missing" }
Write-Output "OK: scripts\test-build42-map8b-runtime-result.ps1"
$map8bDocContent = Get-Content -LiteralPath $map8bDoc -Raw
if ($map8bDocContent -notmatch 'MAP8B_PARTIAL_REGISTRATION_BREAKTHROUGH') { throw "MAP-8B doc missing MAP8B_PARTIAL_REGISTRATION_BREAKTHROUGH" }
Write-Output "OK: doc contains MAP8B_PARTIAL_REGISTRATION_BREAKTHROUGH"
if ($map8bDocContent -notmatch 'WORLDMAP_BIN_INVALID_MAGIC') { throw "MAP-8B doc missing WORLDMAP_BIN_INVALID_MAGIC" }
Write-Output "OK: doc contains WORLDMAP_BIN_INVALID_MAGIC"
if ($map8bDocContent -notmatch 'ISO_META_GRID_MAP_FOLDER_LIST_EMPTY') { throw "MAP-8B doc missing ISO_META_GRID_MAP_FOLDER_LIST_EMPTY" }
Write-Output "OK: doc contains ISO_META_GRID_MAP_FOLDER_LIST_EMPTY"
if ($map8bDocContent -notmatch 'PUBLIC_PLAYABLE_CLAIM_ALLOWED=false') { throw "MAP-8B doc missing PUBLIC_PLAYABLE_CLAIM_ALLOWED=false" }
Write-Output "OK: doc contains PUBLIC_PLAYABLE_CLAIM_ALLOWED=false"
if ($map8bDocContent -notmatch 'BINARY_WRITER_GATE_STILL_CLOSED') { throw "MAP-8B doc missing BINARY_WRITER_GATE_STILL_CLOSED" }
Write-Output "OK: doc contains BINARY_WRITER_GATE_STILL_CLOSED"
$map8bPacketContent = Get-Content -LiteralPath $map8bPacketScript -Raw
if ($map8bPacketContent -notmatch '\.local') { throw "MAP-8B packet script missing .local refusal" }
Write-Output "OK: packet script contains .local refusal language"

Write-Output ""
Write-Output "--- MAP-8B version media runtime result tests ---"
& powershell -ExecutionPolicy Bypass -File $map8bTests
if ($LASTEXITCODE -ne 0) { throw "MAP-8B version media runtime result tests failed." }

Write-Output ""
Write-Output "--- MAP-7Y Minimal sidecar stub probe ---"
$map7yDoc          = Join-Path $repoRoot 'docs\MAP_7Y_MINIMAL_SIDECAR_STUB_PROBE.md'
$map7yPacketScript = Join-Path $repoRoot 'scripts\prepare-build42-map7y-sidecar-stub-packet.ps1'
$map7yTests        = Join-Path $repoRoot 'scripts\test-build42-map7y-sidecar-stub-packet.ps1'
if (-not (Test-Path -LiteralPath $map7yDoc))          { throw "MAP-7Y doc missing" }
Write-Output "OK: docs\MAP_7Y_MINIMAL_SIDECAR_STUB_PROBE.md"
if (-not (Test-Path -LiteralPath $map7yPacketScript)) { throw "MAP-7Y packet script missing" }
Write-Output "OK: scripts\prepare-build42-map7y-sidecar-stub-packet.ps1"
if (-not (Test-Path -LiteralPath $map7yTests))        { throw "MAP-7Y tests missing" }
Write-Output "OK: scripts\test-build42-map7y-sidecar-stub-packet.ps1"
$map7yDocContent = Get-Content -LiteralPath $map7yDoc -Raw
if ($map7yDocContent -notmatch 'MAP7Y_SIDECAR_STUB_PROBE_STAGED') { throw "MAP-7Y doc missing MAP7Y_SIDECAR_STUB_PROBE_STAGED" }
Write-Output "OK: doc contains MAP7Y_SIDECAR_STUB_PROBE_STAGED"
if ($map7yDocContent -notmatch 'MAP_BIN_DISCRIMINATOR_FALSE') { throw "MAP-7Y doc missing MAP_BIN_DISCRIMINATOR_FALSE" }
Write-Output "OK: doc contains MAP_BIN_DISCRIMINATOR_FALSE"
if ($map7yDocContent -notmatch 'SIDECAR_STUBS_GENERATED_FROM_SCRATCH') { throw "MAP-7Y doc missing SIDECAR_STUBS_GENERATED_FROM_SCRATCH" }
Write-Output "OK: doc contains SIDECAR_STUBS_GENERATED_FROM_SCRATCH"
if ($map7yDocContent -notmatch 'NO_THIRD_PARTY_FILES_COPIED') { throw "MAP-7Y doc missing NO_THIRD_PARTY_FILES_COPIED" }
Write-Output "OK: doc contains NO_THIRD_PARTY_FILES_COPIED"
if ($map7yDocContent -notmatch 'PUBLIC_PLAYABLE_CLAIM_ALLOWED=false') { throw "MAP-7Y doc missing PUBLIC_PLAYABLE_CLAIM_ALLOWED=false" }
Write-Output "OK: doc contains PUBLIC_PLAYABLE_CLAIM_ALLOWED=false"
if ($map7yDocContent -notmatch 'LOAD_TEST_NOT_PERFORMED') { throw "MAP-7Y doc missing LOAD_TEST_NOT_PERFORMED" }
Write-Output "OK: doc contains LOAD_TEST_NOT_PERFORMED"
$map7yPacketContent = Get-Content -LiteralPath $map7yPacketScript -Raw
if ($map7yPacketContent -notmatch '\.local') { throw "MAP-7Y packet script missing .local refusal" }
Write-Output "OK: packet script contains .local refusal language"
if ($map7yPacketContent -notmatch 'PZMF_MAP7Y_STUB') { throw "MAP-7Y packet script missing PZMF_MAP7Y_STUB marker" }
Write-Output "OK: packet script contains PZMF_MAP7Y_STUB stub marker"

Write-Output ""
Write-Output "--- MAP-7Y sidecar stub probe tests ---"
& powershell -ExecutionPolicy Bypass -File $map7yTests
if ($LASTEXITCODE -ne 0) { throw "MAP-7Y sidecar stub probe tests failed." }

Write-Output ""
Write-Output "--- MAP-7X Actual registration contract result ---"
$map7xDoc          = Join-Path $repoRoot 'docs\MAP_7X_ACTUAL_REGISTRATION_CONTRACT_RESULT.md'
$map7xPacketScript = Join-Path $repoRoot 'scripts\prepare-build42-map7x-actual-contract-result-packet.ps1'
$map7xTests        = Join-Path $repoRoot 'scripts\test-build42-map7x-actual-contract-result.ps1'
if (-not (Test-Path -LiteralPath $map7xDoc))          { throw "MAP-7X doc missing" }
Write-Output "OK: docs\MAP_7X_ACTUAL_REGISTRATION_CONTRACT_RESULT.md"
if (-not (Test-Path -LiteralPath $map7xPacketScript)) { throw "MAP-7X packet script missing" }
Write-Output "OK: scripts\prepare-build42-map7x-actual-contract-result-packet.ps1"
if (-not (Test-Path -LiteralPath $map7xTests))        { throw "MAP-7X tests missing" }
Write-Output "OK: scripts\test-build42-map7x-actual-contract-result.ps1"
$map7xDocContent = Get-Content -LiteralPath $map7xDoc -Raw
if ($map7xDocContent -notmatch 'MAP7X_ACTUAL_CONTRACT_RESULT_RECORDED') { throw "MAP-7X doc missing MAP7X_ACTUAL_CONTRACT_RESULT_RECORDED" }
Write-Output "OK: doc contains MAP7X_ACTUAL_CONTRACT_RESULT_RECORDED"
if ($map7xDocContent -notmatch 'MAP_BIN_DISCRIMINATOR_FALSE') { throw "MAP-7X doc missing MAP_BIN_DISCRIMINATOR_FALSE" }
Write-Output "OK: doc contains MAP_BIN_DISCRIMINATOR_FALSE"
if ($map7xDocContent -notmatch 'NON_CELL_SIDECAR_GAP_IDENTIFIED') { throw "MAP-7X doc missing NON_CELL_SIDECAR_GAP_IDENTIFIED" }
Write-Output "OK: doc contains NON_CELL_SIDECAR_GAP_IDENTIFIED"
if ($map7xDocContent -notmatch 'PUBLIC_PLAYABLE_CLAIM_ALLOWED=false') { throw "MAP-7X doc missing PUBLIC_PLAYABLE_CLAIM_ALLOWED=false" }
Write-Output "OK: doc contains PUBLIC_PLAYABLE_CLAIM_ALLOWED=false"
if ($map7xDocContent -notmatch 'LOAD_TEST_NOT_PERFORMED') { throw "MAP-7X doc missing LOAD_TEST_NOT_PERFORMED" }
Write-Output "OK: doc contains LOAD_TEST_NOT_PERFORMED"
if ($map7xDocContent -notmatch 'NO_THIRD_PARTY_FILES_COPIED') { throw "MAP-7X doc missing NO_THIRD_PARTY_FILES_COPIED" }
Write-Output "OK: doc contains NO_THIRD_PARTY_FILES_COPIED"
$map7xPacketContent = Get-Content -LiteralPath $map7xPacketScript -Raw
if ($map7xPacketContent -notmatch '\.local') { throw "MAP-7X packet script missing .local refusal" }
Write-Output "OK: packet script contains .local refusal language"

Write-Output ""
Write-Output "--- MAP-7X actual contract result tests ---"
& powershell -ExecutionPolicy Bypass -File $map7xTests
if ($LASTEXITCODE -ne 0) { throw "MAP-7X actual contract result tests failed." }

Write-Output ""
Write-Output "--- MAP-7W Runtime map registration inspector ---"
$map7wDoc          = Join-Path $repoRoot 'docs\MAP_7W_RUNTIME_MAP_REGISTRATION_MOUNTING_CONTRACT.md'
$map7wInspector    = Join-Path $repoRoot 'scripts\inspect-build42-map-registration-contract.ps1'
$map7wPacketScript = Join-Path $repoRoot 'scripts\prepare-build42-map7w-runtime-registration-packet.ps1'
$map7wTests        = Join-Path $repoRoot 'scripts\test-build42-map7w-runtime-registration.ps1'
if (-not (Test-Path -LiteralPath $map7wDoc))          { throw "MAP-7W doc missing" }
Write-Output "OK: docs\MAP_7W_RUNTIME_MAP_REGISTRATION_MOUNTING_CONTRACT.md"
if (-not (Test-Path -LiteralPath $map7wInspector))    { throw "MAP-7W inspector missing" }
Write-Output "OK: scripts\inspect-build42-map-registration-contract.ps1"
if (-not (Test-Path -LiteralPath $map7wPacketScript)) { throw "MAP-7W packet script missing" }
Write-Output "OK: scripts\prepare-build42-map7w-runtime-registration-packet.ps1"
if (-not (Test-Path -LiteralPath $map7wTests))        { throw "MAP-7W tests missing" }
Write-Output "OK: scripts\test-build42-map7w-runtime-registration.ps1"
$map7wDocContent = Get-Content -LiteralPath $map7wDoc -Raw
if ($map7wDocContent -notmatch 'MAP7W_RUNTIME_MAP_REGISTRATION_INSPECTOR_ADDED') { throw "MAP-7W doc missing MAP7W_RUNTIME_MAP_REGISTRATION_INSPECTOR_ADDED" }
Write-Output "OK: doc contains MAP7W_RUNTIME_MAP_REGISTRATION_INSPECTOR_ADDED"
if ($map7wDocContent -notmatch 'BINARY_FORMAT_INVESTIGATION_PAUSED') { throw "MAP-7W doc missing BINARY_FORMAT_INVESTIGATION_PAUSED" }
Write-Output "OK: doc contains BINARY_FORMAT_INVESTIGATION_PAUSED"
if ($map7wDocContent -notmatch 'BINARY_WRITER_GATE_STILL_CLOSED') { throw "MAP-7W doc missing BINARY_WRITER_GATE_STILL_CLOSED" }
Write-Output "OK: doc contains BINARY_WRITER_GATE_STILL_CLOSED"
if ($map7wDocContent -notmatch 'PUBLIC_PLAYABLE_CLAIM_ALLOWED=false') { throw "MAP-7W doc missing PUBLIC_PLAYABLE_CLAIM_ALLOWED=false" }
Write-Output "OK: doc contains PUBLIC_PLAYABLE_CLAIM_ALLOWED=false"
if ($map7wDocContent -notmatch 'LOAD_TEST_NOT_PERFORMED') { throw "MAP-7W doc missing LOAD_TEST_NOT_PERFORMED" }
Write-Output "OK: doc contains LOAD_TEST_NOT_PERFORMED"
$map7wInspContent = Get-Content -LiteralPath $map7wInspector -Raw
if ($map7wInspContent -notmatch '\.local') { throw "MAP-7W inspector missing .local refusal" }
Write-Output "OK: inspector contains .local refusal language"
if ($map7wInspContent -notmatch 'map-registration-contract') { throw "MAP-7W inspector missing output filename" }
Write-Output "OK: inspector contains map-registration-contract output filename"
$map7wPacketContent = Get-Content -LiteralPath $map7wPacketScript -Raw
if ($map7wPacketContent -notmatch '\.local') { throw "MAP-7W packet script missing .local refusal" }
Write-Output "OK: packet script contains .local refusal language"

Write-Output ""
Write-Output "--- MAP-7W runtime registration tests ---"
& powershell -ExecutionPolicy Bypass -File $map7wTests
if ($LASTEXITCODE -ne 0) { throw "MAP-7W runtime registration tests failed." }

Write-Output ""
Write-Output "--- MAP-7V K004/K006 control results ---"
$map7vDoc          = Join-Path $repoRoot 'docs\MAP_7V_K004_K006_CONTROL_RESULTS.md'
$map7vPacketScript = Join-Path $repoRoot 'scripts\prepare-build42-map7v-control-results-packet.ps1'
$map7vTests        = Join-Path $repoRoot 'scripts\test-build42-map7v-control-results.ps1'
if (-not (Test-Path -LiteralPath $map7vDoc))          { throw "MAP-7V doc missing" }
Write-Output "OK: docs\MAP_7V_K004_K006_CONTROL_RESULTS.md"
if (-not (Test-Path -LiteralPath $map7vPacketScript)) { throw "MAP-7V packet script missing" }
Write-Output "OK: scripts\prepare-build42-map7v-control-results-packet.ps1"
if (-not (Test-Path -LiteralPath $map7vTests))        { throw "MAP-7V tests missing" }
Write-Output "OK: scripts\test-build42-map7v-control-results.ps1"
$map7vDocContent = Get-Content -LiteralPath $map7vDoc -Raw
if ($map7vDocContent -notmatch 'MAP7V_K004_COORDINATE_ALIGNED_RESULT_RECORDED') { throw "MAP-7V doc missing MAP7V_K004_COORDINATE_ALIGNED_RESULT_RECORDED" }
Write-Output "OK: doc contains MAP7V_K004_COORDINATE_ALIGNED_RESULT_RECORDED"
if ($map7vDocContent -notmatch 'MAP7V_K006_ZERO_BINARY_CONTROL_RECORDED') { throw "MAP-7V doc missing MAP7V_K006_ZERO_BINARY_CONTROL_RECORDED" }
Write-Output "OK: doc contains MAP7V_K006_ZERO_BINARY_CONTROL_RECORDED"
if ($map7vDocContent -notmatch 'BINARY_FORMAT_INVESTIGATION_PAUSED') { throw "MAP-7V doc missing BINARY_FORMAT_INVESTIGATION_PAUSED" }
Write-Output "OK: doc contains BINARY_FORMAT_INVESTIGATION_PAUSED"
if ($map7vDocContent -notmatch 'PUBLIC_PLAYABLE_CLAIM_ALLOWED=false') { throw "MAP-7V doc missing PUBLIC_PLAYABLE_CLAIM_ALLOWED=false" }
Write-Output "OK: doc contains PUBLIC_PLAYABLE_CLAIM_ALLOWED=false"
if ($map7vDocContent -notmatch 'LOAD_TEST_NOT_PERFORMED') { throw "MAP-7V doc missing LOAD_TEST_NOT_PERFORMED" }
Write-Output "OK: doc contains LOAD_TEST_NOT_PERFORMED"
$map7vPacketContent = Get-Content -LiteralPath $map7vPacketScript -Raw
if ($map7vPacketContent -notmatch '\.local') { throw "MAP-7V packet script missing .local refusal" }
Write-Output "OK: packet script contains .local refusal language"

Write-Output ""
Write-Output "--- MAP-7V control results tests ---"
& powershell -ExecutionPolicy Bypass -File $map7vTests
if ($LASTEXITCODE -ne 0) { throw "MAP-7V control results tests failed." }

Write-Output ""
Write-Output "--- MAP-7U Coordinate-aligned diagnostic ---"
$map7uDoc          = Join-Path $repoRoot 'docs\MAP_7U_MODROOT_LAYOUT_MATCH_AND_COORDINATE_DISCRIMINATOR.md'
$map7uInspector    = Join-Path $repoRoot 'scripts\inspect-build42-workshop-cell-coordinate-contract.ps1'
$map7uPacketScript = Join-Path $repoRoot 'scripts\prepare-build42-map7u-coordinate-discriminator-packet.ps1'
$map7uTests        = Join-Path $repoRoot 'scripts\test-build42-map7u-coordinate-discriminator.ps1'
if (-not (Test-Path -LiteralPath $map7uDoc))          { throw "MAP-7U doc missing" }
Write-Output "OK: docs\MAP_7U_MODROOT_LAYOUT_MATCH_AND_COORDINATE_DISCRIMINATOR.md"
if (-not (Test-Path -LiteralPath $map7uInspector))    { throw "MAP-7U inspector missing" }
Write-Output "OK: scripts\inspect-build42-workshop-cell-coordinate-contract.ps1"
if (-not (Test-Path -LiteralPath $map7uPacketScript)) { throw "MAP-7U packet script missing" }
Write-Output "OK: scripts\prepare-build42-map7u-coordinate-discriminator-packet.ps1"
if (-not (Test-Path -LiteralPath $map7uTests))        { throw "MAP-7U tests missing" }
Write-Output "OK: scripts\test-build42-map7u-coordinate-discriminator.ps1"
$map7uDocContent = Get-Content -LiteralPath $map7uDoc -Raw
if ($map7uDocContent -notmatch 'MAP7U_MODROOT_LAYOUT_MATCH_CONFIRMED') { throw "MAP-7U doc missing MAP7U_MODROOT_LAYOUT_MATCH_CONFIRMED" }
Write-Output "OK: doc contains MAP7U_MODROOT_LAYOUT_MATCH_CONFIRMED"
if ($map7uDocContent -notmatch 'COORDINATE_DISCRIMINATOR_IDENTIFIED') { throw "MAP-7U doc missing COORDINATE_DISCRIMINATOR_IDENTIFIED" }
Write-Output "OK: doc contains COORDINATE_DISCRIMINATOR_IDENTIFIED"
if ($map7uDocContent -notmatch 'BINARY_WRITER_GATE_STILL_CLOSED') { throw "MAP-7U doc missing BINARY_WRITER_GATE_STILL_CLOSED" }
Write-Output "OK: doc contains BINARY_WRITER_GATE_STILL_CLOSED"
if ($map7uDocContent -notmatch 'PUBLIC_PLAYABLE_CLAIM_ALLOWED=false') { throw "MAP-7U doc missing PUBLIC_PLAYABLE_CLAIM_ALLOWED=false" }
Write-Output "OK: doc contains PUBLIC_PLAYABLE_CLAIM_ALLOWED=false"
$map7uInspContent = Get-Content -LiteralPath $map7uInspector -Raw
if ($map7uInspContent -notmatch '\.local') { throw "MAP-7U inspector missing .local refusal" }
Write-Output "OK: inspector contains .local refusal language"
if ($map7uInspContent -notmatch 'workshop-cell-coordinate-contract') { throw "MAP-7U inspector missing output filename" }
Write-Output "OK: inspector contains workshop-cell-coordinate-contract output"
$map7uPacketContent = Get-Content -LiteralPath $map7uPacketScript -Raw
if ($map7uPacketContent -notmatch '\.local') { throw "MAP-7U packet script missing .local refusal" }
Write-Output "OK: packet script contains .local refusal language"

Write-Output ""
Write-Output "--- MAP-7U coordinate-aligned diagnostic tests ---"
& powershell -ExecutionPolicy Bypass -File $map7uTests
if ($LASTEXITCODE -ne 0) { throw "MAP-7U coordinate-aligned diagnostic tests failed." }

Write-Output ""
Write-Output "--- MAP-7T Workshop K002 runtime payload comparison ---"
$map7tDoc          = Join-Path $repoRoot 'docs\MAP_7T_WORKSHOP_K002_RUNTIME_PAYLOAD_COMPARISON.md'
$map7tInspector    = Join-Path $repoRoot 'scripts\inspect-build42-workshop-runtime-payload.ps1'
$map7tPacketScript = Join-Path $repoRoot 'scripts\prepare-build42-map7t-k002-record-packet.ps1'
$map7tTests        = Join-Path $repoRoot 'scripts\test-build42-map7t-k002-runtime-payload.ps1'
if (-not (Test-Path -LiteralPath $map7tDoc))          { throw "MAP-7T doc missing" }
Write-Output "OK: docs\MAP_7T_WORKSHOP_K002_RUNTIME_PAYLOAD_COMPARISON.md"
if (-not (Test-Path -LiteralPath $map7tInspector))    { throw "MAP-7T inspector missing" }
Write-Output "OK: scripts\inspect-build42-workshop-runtime-payload.ps1"
if (-not (Test-Path -LiteralPath $map7tPacketScript)) { throw "MAP-7T packet script missing" }
Write-Output "OK: scripts\prepare-build42-map7t-k002-record-packet.ps1"
if (-not (Test-Path -LiteralPath $map7tTests))        { throw "MAP-7T tests missing" }
Write-Output "OK: scripts\test-build42-map7t-k002-runtime-payload.ps1"
$map7tDocContent = Get-Content -LiteralPath $map7tDoc -Raw
if ($map7tDocContent -notmatch 'MAP7T_K002_WORKSHOP_ACTIVATION_RECORDED') { throw "MAP-7T doc missing MAP7T_K002_WORKSHOP_ACTIVATION_RECORDED" }
Write-Output "OK: doc contains MAP7T_K002_WORKSHOP_ACTIVATION_RECORDED"
if ($map7tDocContent -notmatch 'BINARY_WRITER_GATE_STILL_CLOSED') { throw "MAP-7T doc missing BINARY_WRITER_GATE_STILL_CLOSED" }
Write-Output "OK: doc contains BINARY_WRITER_GATE_STILL_CLOSED"
if ($map7tDocContent -notmatch 'PUBLIC_PLAYABLE_CLAIM_ALLOWED=false') { throw "MAP-7T doc missing PUBLIC_PLAYABLE_CLAIM_ALLOWED=false" }
Write-Output "OK: doc contains PUBLIC_PLAYABLE_CLAIM_ALLOWED=false"
if ($map7tDocContent -notmatch 'LOAD_TEST_NOT_PERFORMED') { throw "MAP-7T doc missing LOAD_TEST_NOT_PERFORMED" }
Write-Output "OK: doc contains LOAD_TEST_NOT_PERFORMED"
$map7tInspContent = Get-Content -LiteralPath $map7tInspector -Raw
if ($map7tInspContent -notmatch '\.local') { throw "MAP-7T inspector missing .local refusal" }
Write-Output "OK: inspector contains .local refusal language"
if ($map7tInspContent -notmatch 'workshop-runtime-payload-comparison') { throw "MAP-7T inspector missing output filename" }
Write-Output "OK: inspector contains workshop-runtime-payload-comparison output"
$map7tPacketContent = Get-Content -LiteralPath $map7tPacketScript -Raw
if ($map7tPacketContent -notmatch '\.local') { throw "MAP-7T packet script missing .local refusal" }
Write-Output "OK: packet script contains .local refusal language"

Write-Output ""
Write-Output "--- MAP-7T k002 runtime payload tests ---"
& powershell -ExecutionPolicy Bypass -File $map7tTests
if ($LASTEXITCODE -ne 0) { throw "MAP-7T k002 runtime payload tests failed." }

Write-Output ""
Write-Output "--- MAP-7S Private Workshop staging packet ---"
$map7sDoc          = Join-Path $repoRoot 'docs\MAP_7S_PRIVATE_WORKSHOP_STAGING_PACKET.md'
$map7sPacketScript = Join-Path $repoRoot 'scripts\prepare-build42-map7s-private-workshop-staging-packet.ps1'
$map7sTests        = Join-Path $repoRoot 'scripts\test-build42-map7s-private-workshop-staging.ps1'
if (-not (Test-Path -LiteralPath $map7sDoc))          { throw "MAP-7S doc missing" }
Write-Output "OK: docs\MAP_7S_PRIVATE_WORKSHOP_STAGING_PACKET.md"
if (-not (Test-Path -LiteralPath $map7sPacketScript)) { throw "MAP-7S packet script missing" }
Write-Output "OK: scripts\prepare-build42-map7s-private-workshop-staging-packet.ps1"
if (-not (Test-Path -LiteralPath $map7sTests))        { throw "MAP-7S tests missing" }
Write-Output "OK: scripts\test-build42-map7s-private-workshop-staging.ps1"
$map7sDocContent = Get-Content -LiteralPath $map7sDoc -Raw
if ($map7sDocContent -notmatch 'MAP7S_WORKSHOP_STAGING_PACKET_CREATED') { throw "MAP-7S doc missing MAP7S_WORKSHOP_STAGING_PACKET_CREATED" }
Write-Output "OK: doc contains MAP7S_WORKSHOP_STAGING_PACKET_CREATED"
if ($map7sDocContent -notmatch 'NO_AUTOMATIC_WORKSHOP_UPLOAD') { throw "MAP-7S doc missing NO_AUTOMATIC_WORKSHOP_UPLOAD" }
Write-Output "OK: doc contains NO_AUTOMATIC_WORKSHOP_UPLOAD"
if ($map7sDocContent -notmatch 'PUBLIC_PLAYABLE_CLAIM_ALLOWED=false') { throw "MAP-7S doc missing PUBLIC_PLAYABLE_CLAIM_ALLOWED=false" }
Write-Output "OK: doc contains PUBLIC_PLAYABLE_CLAIM_ALLOWED=false"
if ($map7sDocContent -notmatch 'LOAD_TEST_NOT_PERFORMED') { throw "MAP-7S doc missing LOAD_TEST_NOT_PERFORMED" }
Write-Output "OK: doc contains LOAD_TEST_NOT_PERFORMED"
$map7sPacketContent = Get-Content -LiteralPath $map7sPacketScript -Raw
if ($map7sPacketContent -notmatch '\.local') { throw "MAP-7S packet script missing .local refusal" }
Write-Output "OK: packet script contains .local refusal language"
if ($map7sPacketContent -notmatch 'dotnet run') { throw "MAP-7S packet script missing dotnet run call" }
Write-Output "OK: packet script contains dotnet run for candidate generation"

Write-Output ""
Write-Output "--- MAP-7S private Workshop staging tests ---"
& powershell -ExecutionPolicy Bypass -File $map7sTests
if ($LASTEXITCODE -ne 0) { throw "MAP-7S private Workshop staging tests failed." }

Write-Output ""
Write-Output "--- MAP-7R Variant J Workshop trigger failure ---"
$map7rDoc          = Join-Path $repoRoot 'docs\MAP_7R_VARIANT_J_WORKSHOP_TRIGGER_FAILURE.md'
$map7rPacketScript = Join-Path $repoRoot 'scripts\prepare-build42-map7r-workshop-activation-decision-packet.ps1'
$map7rTests        = Join-Path $repoRoot 'scripts\test-build42-map7r-workshop-trigger-failure.ps1'
if (-not (Test-Path -LiteralPath $map7rDoc))          { throw "MAP-7R doc missing" }
Write-Output "OK: docs\MAP_7R_VARIANT_J_WORKSHOP_TRIGGER_FAILURE.md"
if (-not (Test-Path -LiteralPath $map7rPacketScript)) { throw "MAP-7R packet script missing" }
Write-Output "OK: scripts\prepare-build42-map7r-workshop-activation-decision-packet.ps1"
if (-not (Test-Path -LiteralPath $map7rTests))        { throw "MAP-7R tests missing" }
Write-Output "OK: scripts\test-build42-map7r-workshop-trigger-failure.ps1"
$map7rDocContent = Get-Content -LiteralPath $map7rDoc -Raw
if ($map7rDocContent -notmatch 'MAP7R_VARIANT_J_WORKSHOP_TRIGGER_INSUFFICIENT') { throw "MAP-7R doc missing MAP7R_VARIANT_J_WORKSHOP_TRIGGER_INSUFFICIENT" }
Write-Output "OK: doc contains MAP7R_VARIANT_J_WORKSHOP_TRIGGER_INSUFFICIENT"
if ($map7rDocContent -notmatch 'BORROWED_WORKSHOPITEMS_TRIGGER_EXHAUSTED') { throw "MAP-7R doc missing BORROWED_WORKSHOPITEMS_TRIGGER_EXHAUSTED" }
Write-Output "OK: doc contains BORROWED_WORKSHOPITEMS_TRIGGER_EXHAUSTED"
if ($map7rDocContent -notmatch 'NO_MORE_STATIC_LAYOUT_TESTS') { throw "MAP-7R doc missing NO_MORE_STATIC_LAYOUT_TESTS" }
Write-Output "OK: doc contains NO_MORE_STATIC_LAYOUT_TESTS"
if ($map7rDocContent -notmatch 'PUBLIC_PLAYABLE_CLAIM_ALLOWED=false') { throw "MAP-7R doc missing PUBLIC_PLAYABLE_CLAIM_ALLOWED=false" }
Write-Output "OK: doc contains PUBLIC_PLAYABLE_CLAIM_ALLOWED=false"
if ($map7rDocContent -notmatch 'LOAD_TEST_NOT_PERFORMED') { throw "MAP-7R doc missing LOAD_TEST_NOT_PERFORMED" }
Write-Output "OK: doc contains LOAD_TEST_NOT_PERFORMED"
$map7rPacketContent = Get-Content -LiteralPath $map7rPacketScript -Raw
if ($map7rPacketContent -notmatch '\.local') { throw "MAP-7R packet script missing .local refusal" }
Write-Output "OK: packet script contains .local refusal language"
$map7rAnalyzerContent = Get-Content -LiteralPath (Join-Path $repoRoot 'scripts\inspect-build42-map7d-load-result.ps1') -Raw
if ($map7rAnalyzerContent -notmatch 'MAP7R_VARIANT_J_WORKSHOP_TRIGGER_INSUFFICIENT') { throw "MAP-7R analyzer missing MAP7R_VARIANT_J_WORKSHOP_TRIGGER_INSUFFICIENT" }
Write-Output "OK: analyzer contains MAP7R_VARIANT_J_WORKSHOP_TRIGGER_INSUFFICIENT"
if ($map7rAnalyzerContent -notmatch 'expectedMapLotheaderMetaFound') { throw "MAP-7R analyzer missing expectedMapLotheaderMetaFound" }
Write-Output "OK: analyzer contains expectedMapLotheaderMetaFound"

Write-Output ""
Write-Output "--- MAP-7R Workshop trigger failure tests ---"
& powershell -ExecutionPolicy Bypass -File $map7rTests
if ($LASTEXITCODE -ne 0) { throw "MAP-7R Workshop trigger failure tests failed." }

Write-Output ""
Write-Output "--- MAP-7Q Dru_map runtime baseline success ---"
$map7qDoc          = Join-Path $repoRoot 'docs\MAP_7Q_DRUMAP_RUNTIME_BASELINE_SUCCESS.md'
$map7qPacketScript = Join-Path $repoRoot 'scripts\prepare-build42-map7q-runtime-activation-next-packet.ps1'
$map7qTests        = Join-Path $repoRoot 'scripts\test-build42-map7q-runtime-baseline-success.ps1'
if (-not (Test-Path -LiteralPath $map7qDoc))          { throw "MAP-7Q doc missing" }
Write-Output "OK: docs\MAP_7Q_DRUMAP_RUNTIME_BASELINE_SUCCESS.md"
if (-not (Test-Path -LiteralPath $map7qPacketScript)) { throw "MAP-7Q packet script missing" }
Write-Output "OK: scripts\prepare-build42-map7q-runtime-activation-next-packet.ps1"
if (-not (Test-Path -LiteralPath $map7qTests))        { throw "MAP-7Q tests missing" }
Write-Output "OK: scripts\test-build42-map7q-runtime-baseline-success.ps1"
$map7qDocContent = Get-Content -LiteralPath $map7qDoc -Raw
if ($map7qDocContent -notmatch 'MAP7Q_DRUMAP_BASELINE_RUNTIME_SUCCESS') { throw "MAP-7Q doc missing MAP7Q_DRUMAP_BASELINE_RUNTIME_SUCCESS" }
Write-Output "OK: doc contains MAP7Q_DRUMAP_BASELINE_RUNTIME_SUCCESS"
if ($map7qDocContent -notmatch 'EMPTY_CLIENT_SCAN_NOT_DECISIVE') { throw "MAP-7Q doc missing EMPTY_CLIENT_SCAN_NOT_DECISIVE" }
Write-Output "OK: doc contains EMPTY_CLIENT_SCAN_NOT_DECISIVE"
if ($map7qDocContent -notmatch 'DRUMAP_BASELINE_RUNTIME_SUCCESSFUL') { throw "MAP-7Q doc missing DRUMAP_BASELINE_RUNTIME_SUCCESSFUL" }
Write-Output "OK: doc contains DRUMAP_BASELINE_RUNTIME_SUCCESSFUL"
if ($map7qDocContent -notmatch 'PUBLIC_PLAYABLE_CLAIM_ALLOWED=false') { throw "MAP-7Q doc missing PUBLIC_PLAYABLE_CLAIM_ALLOWED=false" }
Write-Output "OK: doc contains PUBLIC_PLAYABLE_CLAIM_ALLOWED=false"
if ($map7qDocContent -notmatch 'LOAD_TEST_NOT_PERFORMED') { throw "MAP-7Q doc missing LOAD_TEST_NOT_PERFORMED" }
Write-Output "OK: doc contains LOAD_TEST_NOT_PERFORMED"
$map7qPacketContent = Get-Content -LiteralPath $map7qPacketScript -Raw
if ($map7qPacketContent -notmatch '\.local') { throw "MAP-7Q packet script missing .local refusal" }
Write-Output "OK: packet script contains .local refusal language"
$map7qAnalyzerContent = Get-Content -LiteralPath (Join-Path $repoRoot 'scripts\inspect-build42-map7d-load-result.ps1') -Raw
if ($map7qAnalyzerContent -notmatch 'MAP7Q_DRUMAP_BASELINE_RUNTIME_SUCCESS') { throw "MAP-7Q analyzer missing MAP7Q_DRUMAP_BASELINE_RUNTIME_SUCCESS" }
Write-Output "OK: analyzer contains MAP7Q_DRUMAP_BASELINE_RUNTIME_SUCCESS"
if ($map7qAnalyzerContent -notmatch 'runtimeSuccessEvidenceFound') { throw "MAP-7Q analyzer missing runtimeSuccessEvidenceFound" }
Write-Output "OK: analyzer contains runtimeSuccessEvidenceFound"

Write-Output ""
Write-Output "--- MAP-7Q runtime baseline success tests ---"
& powershell -ExecutionPolicy Bypass -File $map7qTests
if ($LASTEXITCODE -ne 0) { throw "MAP-7Q runtime baseline success tests failed." }

Write-Output ""
Write-Output "--- MAP-7P Variant I and runtime baseline ---"
$map7pDoc          = Join-Path $repoRoot 'docs\MAP_7P_VARIANT_I_AND_RUNTIME_BASELINE.md'
$map7pPacketScript = Join-Path $repoRoot 'scripts\prepare-build42-map7p-known-working-runtime-baseline-packet.ps1'
$map7pTests        = Join-Path $repoRoot 'scripts\test-build42-map7p-known-working-runtime-baseline.ps1'
if (-not (Test-Path -LiteralPath $map7pDoc))          { throw "MAP-7P doc missing" }
Write-Output "OK: docs\MAP_7P_VARIANT_I_AND_RUNTIME_BASELINE.md"
if (-not (Test-Path -LiteralPath $map7pPacketScript)) { throw "MAP-7P packet script missing" }
Write-Output "OK: scripts\prepare-build42-map7p-known-working-runtime-baseline-packet.ps1"
if (-not (Test-Path -LiteralPath $map7pTests))        { throw "MAP-7P tests missing" }
Write-Output "OK: scripts\test-build42-map7p-known-working-runtime-baseline.ps1"
$map7pDocContent = Get-Content -LiteralPath $map7pDoc -Raw
if ($map7pDocContent -notmatch 'MAP7F_VARIANT_I_MAP_FOLDER_SCAN_EMPTY') { throw "MAP-7P doc missing MAP7F_VARIANT_I_MAP_FOLDER_SCAN_EMPTY" }
Write-Output "OK: doc contains MAP7F_VARIANT_I_MAP_FOLDER_SCAN_EMPTY"
if ($map7pDocContent -notmatch 'VARIANTS_ABCDEFGHI_EXHAUSTED') { throw "MAP-7P doc missing VARIANTS_ABCDEFGHI_EXHAUSTED" }
Write-Output "OK: doc contains VARIANTS_ABCDEFGHI_EXHAUSTED"
if ($map7pDocContent -notmatch 'DRUMAP_BASELINE_DIAGNOSTIC_REQUIRED') { throw "MAP-7P doc missing DRUMAP_BASELINE_DIAGNOSTIC_REQUIRED" }
Write-Output "OK: doc contains DRUMAP_BASELINE_DIAGNOSTIC_REQUIRED"
if ($map7pDocContent -notmatch 'PUBLIC_PLAYABLE_CLAIM_ALLOWED=false') { throw "MAP-7P doc missing PUBLIC_PLAYABLE_CLAIM_ALLOWED=false" }
Write-Output "OK: doc contains PUBLIC_PLAYABLE_CLAIM_ALLOWED=false"
if ($map7pDocContent -notmatch 'LOAD_TEST_NOT_PERFORMED') { throw "MAP-7P doc missing LOAD_TEST_NOT_PERFORMED" }
Write-Output "OK: doc contains LOAD_TEST_NOT_PERFORMED"
$map7pPacketContent = Get-Content -LiteralPath $map7pPacketScript -Raw
if ($map7pPacketContent -notmatch '\.local') { throw "MAP-7P packet script missing .local refusal" }
Write-Output "OK: packet script contains .local refusal language"
$map7pAnalyzerContent = Get-Content -LiteralPath (Join-Path $repoRoot 'scripts\inspect-build42-map7d-load-result.ps1') -Raw
if ($map7pAnalyzerContent -notmatch 'MAP7P_DRUMAP_BASELINE_MAP_FOLDER_SCAN_FOUND') { throw "MAP-7P analyzer missing MAP7P_DRUMAP_BASELINE_MAP_FOLDER_SCAN_FOUND" }
Write-Output "OK: analyzer contains MAP7P_DRUMAP_BASELINE_MAP_FOLDER_SCAN_FOUND"
if ($map7pAnalyzerContent -notmatch 'MAP7P_DRUMAP_BASELINE_MAP_FOLDER_SCAN_EMPTY') { throw "MAP-7P analyzer missing MAP7P_DRUMAP_BASELINE_MAP_FOLDER_SCAN_EMPTY" }
Write-Output "OK: analyzer contains MAP7P_DRUMAP_BASELINE_MAP_FOLDER_SCAN_EMPTY"

Write-Output ""
Write-Output "--- MAP-7P runtime baseline tests ---"
& powershell -ExecutionPolicy Bypass -File $map7pTests
if ($LASTEXITCODE -ne 0) { throw "MAP-7P runtime baseline tests failed." }

Write-Output ""
Write-Output "--- MAP-7O Dru_map-aligned experiment I ---"
$map7oDoc          = Join-Path $repoRoot 'docs\MAP_7O_DRUMAP_ALIGNED_EXPERIMENT_I.md'
$map7oPacketScript = Join-Path $repoRoot 'scripts\prepare-build42-map7o-drumap-aligned-experiment-packet.ps1'
$map7oTests        = Join-Path $repoRoot 'scripts\test-build42-map7o-drumap-aligned-experiment.ps1'
if (-not (Test-Path -LiteralPath $map7oDoc))          { throw "MAP-7O doc missing" }
Write-Output "OK: docs\MAP_7O_DRUMAP_ALIGNED_EXPERIMENT_I.md"
if (-not (Test-Path -LiteralPath $map7oPacketScript)) { throw "MAP-7O packet script missing" }
Write-Output "OK: scripts\prepare-build42-map7o-drumap-aligned-experiment-packet.ps1"
if (-not (Test-Path -LiteralPath $map7oTests))        { throw "MAP-7O tests missing" }
Write-Output "OK: scripts\test-build42-map7o-drumap-aligned-experiment.ps1"
$map7oDocContent = Get-Content -LiteralPath $map7oDoc -Raw
if ($map7oDocContent -notmatch 'DRUMAP_ALIGNED_EXPERIMENT_I_PREPARED') { throw "MAP-7O doc missing DRUMAP_ALIGNED_EXPERIMENT_I_PREPARED" }
Write-Output "OK: doc contains DRUMAP_ALIGNED_EXPERIMENT_I_PREPARED"
if ($map7oDocContent -notmatch 'MAP7F_VARIANT_I_MAP_FOLDER_SCAN_EMPTY') { throw "MAP-7O doc missing MAP7F_VARIANT_I_MAP_FOLDER_SCAN_EMPTY" }
Write-Output "OK: doc contains MAP7F_VARIANT_I_MAP_FOLDER_SCAN_EMPTY"
if ($map7oDocContent -notmatch 'PUBLIC_PLAYABLE_CLAIM_ALLOWED=false') { throw "MAP-7O doc missing PUBLIC_PLAYABLE_CLAIM_ALLOWED=false" }
Write-Output "OK: doc contains PUBLIC_PLAYABLE_CLAIM_ALLOWED=false"
$map7oPacketContent = Get-Content -LiteralPath $map7oPacketScript -Raw
if ($map7oPacketContent -notmatch '\.local') { throw "MAP-7O packet script missing .local refusal" }
Write-Output "OK: packet script contains .local refusal language"
$map7oDiscContent = Get-Content -LiteralPath (Join-Path $repoRoot 'scripts\inspect-build42-map-discovery-path.ps1') -Raw
if ($map7oDiscContent -notmatch 'has_drumap_aligned_layout') { throw "MAP-7O discovery inspector missing has_drumap_aligned_layout" }
Write-Output "OK: discovery inspector contains has_drumap_aligned_layout"
if ($map7oDiscContent -notmatch 'common_mod_info_absent') { throw "MAP-7O discovery inspector missing common_mod_info_absent" }
Write-Output "OK: discovery inspector contains common_mod_info_absent"
$map7oMetaContent = Get-Content -LiteralPath (Join-Path $repoRoot 'scripts\inspect-build42-map-metadata-contract.ps1') -Raw
if ($map7oMetaContent -notmatch 'map_info_lots_is_none') { throw "MAP-7O metadata inspector missing map_info_lots_is_none" }
Write-Output "OK: metadata inspector contains map_info_lots_is_none"
if ($map7oMetaContent -notmatch 'map_info_has_zoomX') { throw "MAP-7O metadata inspector missing map_info_has_zoomX" }
Write-Output "OK: metadata inspector contains map_info_has_zoomX"

Write-Output ""
Write-Output "--- MAP-7O Dru_map-aligned experiment tests ---"
& powershell -ExecutionPolicy Bypass -File $map7oTests
if ($LASTEXITCODE -ne 0) { throw "MAP-7O Dru_map-aligned experiment tests failed." }

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
    'Proof packet'                         = 136
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
    'MAP-9A bootstrap canary packet tests'                         = 26
    'MAP-8Z controlled IGMB install packet tests'                 = 24
    'MAP-8Y experimental IGMB writer tests'                       = 30
    'MAP-8Y experimental IGMB writer packet tests'                = 20
    'MAP-8X real transition structure result tests'               = 20
    'MAP-8W IGMB transition structure inspector tests'            = 24
    'MAP-8W IGMB transition structure result tests'               = 20
    'MAP-8V real first non-FF transition result tests'             = 20
    'MAP-8U first non-FF transition inspector tests'               = 27
    'MAP-8U first non-FF transition result tests'                  = 20
    'MAP-8T real cell boundary result tests'                       = 20
    'MAP-8S IGMB cell boundary inspector tests'                    = 20
    'MAP-8S cell boundary result tests'                            = 20
    'MAP-8R real IGMB structure result tests'                      = 20
    'MAP-8Q IGMB structure inspector tests'                        = 24
    'MAP-8Q IGMB structure result tests'                           = 20
    'MAP-8P IGMB header result tests'                              = 20
    'MAP-8O worldmap bin header inspector tests'        = 22
    'MAP-8O worldmap bin header result tests'          = 20
    'MAP-8N worldmap bin presence result tests'        = 20
    'MAP-8M worldmap bin presence tests'               = 16
    'MAP-8L worldmap xml runtime result tests'         = 20
    'MAP-8L worldmap xml substantial candidate tests'  = 20
    'MAP-8K parent metadata contract comparator tests' = 20
    'MAP-8I dual spawnpoint runtime result tests'  = 20
    'MAP-8H parent/child contract probe tests'     = 20
    'MAP-8F lots=self runtime result tests'        = 20
    'MAP-8D no invalid worldmap bin probe tests'  = 20
    'MAP-8B version media runtime result tests'   = 20
    'MAP-7Y sidecar stub probe tests'             = 24
    'MAP-7X actual contract result tests'         = 20
    'MAP-7W runtime registration tests'           = 20
    'MAP-7V control results tests'                = 20
    'MAP-7U coordinate-aligned diagnostic tests'  = 20
    'MAP-7T k002 runtime payload tests'           = 20
    'MAP-7S private Workshop staging tests'       = 20
    'MAP-7R Workshop trigger failure tests'       = 20
    'MAP-7Q runtime baseline success tests'      = 20
    'MAP-7P runtime baseline tests'              = 20
    'MAP-7O Dru_map-aligned experiment tests' = 19
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
$psTotal = 1824  # = validation_summary.total_expected_assertions in proof-packet v0.76

$dnCoreTests = 190   # PZMapForge.Core.Tests
$dnCliTests  = 366   # PZMapForge.Cli.Tests (MAP-7D: +18 Build42 LOTH v4 no-BOM tests)
$dnTotal     = 556   # = dotnet_validation_summary.test_total in proof-packet v0.35

Write-Output ""
Write-Output "  PowerShell lane  (validation_summary in proof-packet v0.76):"
foreach ($kv in $psChecks.GetEnumerator()) {
    Write-Output ("    {0,-34} {1,4}" -f "$($kv.Key):", $kv.Value)
}
Write-Output "    -------------------------------------- ----"
Write-Output ("    {0,-34} {1,4}" -f "Total:", $psTotal)

Write-Output ""
Write-Output "  .NET lane  (dotnet_validation_summary in proof-packet v0.76 -- tracked separately):"
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
