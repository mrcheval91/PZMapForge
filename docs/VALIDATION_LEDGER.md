# Validation Ledger

PZMapForge maintains two parallel, intentionally separate validation lanes.
This document is the operator-readable reference for both.

Baseline commit: cac517c

---

## Claim boundary

planning_artifact_only_not_pz_load_tested

All artifacts produced by this tool are local planning artifacts only.
See docs/CLAIM_BOUNDARY.md for the full boundary statement.

---

## Lane 1: PowerShell artifact validation

Covers: image parsing, artifact structure, palette verification, TMX integrity,
region extraction, primitive classification, plan recommendations, proof packet.

### Commands

    powershell -ExecutionPolicy Bypass -File ".\scripts\test-schema-files.ps1"
    powershell -ExecutionPolicy Bypass -File ".\scripts\write-proof-packet.ps1"
    powershell -ExecutionPolicy Bypass -File ".\scripts\test-proof-packet.ps1"
    powershell -ExecutionPolicy Bypass -File ".\scripts\validate.ps1"

validate.ps1 runs all sub-scripts in sequence and exits nonzero if any fail.

### Expected counts

| Check | Script | Expected assertions |
|---|---|---:|
| Schema file sanity | test-schema-files.ps1 | 136 |
| Artifact contract | test-parsed-cell-contract.ps1 | 40 |
| Palette SHA-256 verification | test-palette-sha256.ps1 | 5 |
| TMX integrity | test-tmx-integrity.ps1 | 21 |
| Hardening harness | tests/test-image-mapforge.ps1 | 36 |
| Region extraction | test-region-extraction.ps1 | 24 |
| Primitive classification | test-primitive-classification.ps1 | 22 |
| Plan recommendations contract | test-plan-recommendations-contract.ps1 | 28 |
| Proof packet | test-proof-packet.ps1 | 69 |
| Total | | 381 |

These 381 assertions are the canonical PowerShell validation total.
They are recorded in proof-packet.json as validation_summary.total_expected_assertions.

### What this lane covers

- JSON schema file structure (5 schemas)
- parsed-cell.json artifact structure and field contracts
- palette_sha256 cross-check (parsed-cell vs source/image-palette.json)
- TMX base64/gzip/GID structural validity
- ImageMapForge full 11-test hardening harness including -Resize
- BFS region extraction correctness
- Primitive classification correctness
- Plan recommendations artifact contract including thresholds_used
- Proof packet completeness and field contracts

### What this lane does NOT cover

- .NET typed engine correctness (that is Lane 2)
- PlayabIe Project Zomboid map load testing (not claimed)

---

## Lane 2: .NET / xUnit typed engine validation

Covers: .NET core library correctness, CLI process behavior, full-pipeline
artifact completeness, and markdown report content contracts.

### Commands

    dotnet build PZMapForge.slnx
    dotnet test PZMapForge.slnx

### Expected counts

| Check | Expected |
|---|---:|
| dotnet build | 0 errors |
| dotnet test (total) | 152 |
| PZMapForge.Core.Tests | 123 |
| PZMapForge.Cli.Tests | 29 |
| Process CLI tests present | true |
| Full-pipeline contract tests present | true |
| Full-pipeline artifact count | 7 |

These counts are recorded in proof-packet.json as dotnet_validation_summary.
They are intentionally not added to validation_summary.total_expected_assertions.
The two lanes measure different things and must remain separate.

### Test breakdown: PZMapForge.Core.Tests (123)

- PaletteLoader: 8 tests
- ImageMapForgeParser: 10 tests
- ImageMapForgeParser cross-verification: 6 tests
- ImageMapForgeArtifactWriter: 9 tests
- ParsedCellLoader: 11 tests
- RegionExtractor: 8 tests
- RegionExtractor cross-verification: 3 tests
- RegionArtifactWriter: 8 tests (5 JSON + 3 markdown)
- PrimitiveClassifier: 16 tests (8 behavioral + 8 kind mappings)
- PrimitiveArtifactWriter: 8 tests (5 JSON + 3 markdown)
- PlanningRuleEngine: 15 tests
- PlanningRuleOptions: 8 tests
- PlanningArtifactWriter: 10 tests
- PlanningArtifactCrossVerification: 3 tests

### Test breakdown: PZMapForge.Cli.Tests (29)

- CliSmokeTests: 13 tests
- CliProcessTests: 10 process-level integration tests
- FullPipelineContractTests: 6 content contract tests (via IClassFixture)

### What this lane covers

- All .NET core types: loaders, parsers, extractors, classifiers, writers
- CLI commands: image-check, image-export, full-pipeline, plan-check,
  plan-export, palette-check, parsed-cell-check, region-check, primitive-check
- Process-level CLI integration (exit codes, artifact presence)
- Full-pipeline content contracts (claim boundary in all 3 markdown artifacts,
  summary tables present)

### Why .NET counts are not in total_expected_assertions

total_expected_assertions counts PowerShell assertion invocations.
xUnit tests are a different evidence kind. Adding them would mix two
incomparable units and make the total meaningless as a PS-validation baseline.
The proof packet records both separately and labels each clearly.

---

## Lane 3: Proof packet

The proof packet records the state of both validation lanes plus artifact
SHA-256 hashes and git state.

Schema: pzmapforge.proof-packet.v0.10

### Commands

    powershell -ExecutionPolicy Bypass -File ".\scripts\write-proof-packet.ps1"
    powershell -ExecutionPolicy Bypass -File ".\scripts\test-proof-packet.ps1"

### What the proof packet records

- Schema version (pzmapforge.proof-packet.v0.10)
- git branch and commit
- SHA-256 hashes for all 11 artifacts
- validation_summary: PowerShell lane assertion counts (381 total)
- dotnet_validation_summary: .NET test counts (152 total), artifact list
- claim_boundary: planning_artifact_only_not_pz_load_tested
- safety flags: local_only_outputs, media_maps_touched, pz_assets_copied,
  playable_export_claimed

### Proof packet test assertions: 69

    2  output files present
   28  required top-level fields
    2  sentinels (schema version, claim_boundary)
   11  SHA-256 format checks
    9  PowerShell validation_summary counts
   13  dotnet_validation_summary (test_total, core/cli counts, booleans, 7 artifacts)
    4  safety flags
   --
   69  total

---

## Full-pipeline artifact surface

The .NET full-pipeline command emits 7 artifacts to .local/mapforge/:

    parsed-cell.json
    regions.json
    regions-report.md
    primitives.json
    primitives-report.md
    plan-recommendations.json
    plan-report.md

All 7 are verified by FullPipelineContractTests (IClassFixture, single pipeline run).
Content contracts checked:
- regions-report.md contains planning_artifact_only_not_pz_load_tested
- regions-report.md contains Summary by kind
- primitives-report.md contains planning_artifact_only_not_pz_load_tested
- primitives-report.md contains Summary by primitive type
- plan-report.md contains planning_artifact_only_not_pz_load_tested

---

## Full validation run (all commands in order)

    dotnet build PZMapForge.slnx
    dotnet test PZMapForge.slnx
    powershell -ExecutionPolicy Bypass -File ".\scripts\test-schema-files.ps1"
    powershell -ExecutionPolicy Bypass -File ".\scripts\write-proof-packet.ps1"
    powershell -ExecutionPolicy Bypass -File ".\scripts\test-proof-packet.ps1"
    powershell -ExecutionPolicy Bypass -File ".\scripts\validate.ps1"

Expected outcome: 0 errors, 152 tests, 136 schema assertions, 69 proof packet
assertions, validate.ps1 exits 0 and prints "Validation passed."

---

## Explicit non-claims

The following are not claimed and must not be added without documented evidence:

- Playable Project Zomboid map export
- lotpack, lotheader, or bin file generation
- WorldEd replacement or compatibility
- Official Project Zomboid tool status
- Build 42 tested compatibility
- Steam Workshop readiness
- Copying or redistributing Project Zomboid game assets
- Writing to media/maps

The claim boundary does not advance without a real local load test and
a documented decision record.
