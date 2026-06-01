# Changelog

All notable changes to PZMapForge will be documented here.

Format: Keep a Changelog.

---

## [Unreleased]

### Added
- docs/VALIDATION_LEDGER.md: operator-readable ledger for both validation lanes.
  Documents PowerShell lane (381 assertions, 9 scripts), .NET lane (152 tests,
  breakdown by project), proof packet lane (69 assertions), full-pipeline artifact
  surface (7 artifacts with content contracts), full validation command sequence,
  and explicit non-claims.

### Changed
- README.md: link to VALIDATION_LEDGER.md added; Quickstart assertion count
  updated from 285 to 381; full-pipeline artifact list updated from 5 to 7.
- docs/IMPLEMENTATION.md: validation ledger row added.
- CHANGELOG.md: this entry.

dotnet build: 0 errors
dotnet test:  152/152
validate.ps1: Validation passed (no count changes)

---

## Previous

### Added
- schemas/pzmapforge.proof-packet.v0.10.schema.json: proof packet schema bumped to v0.10.
  Adds dotnet_validation_summary section (test_total, core_tests, cli_tests, process/contract
  booleans, artifact_count, artifact list, note). PS validation_summary counts updated:
  schema_file_sanity=136, proof_packet=69, total_expected_assertions=381.

### Changed
- scripts/write-proof-packet.ps1: schema → v0.10; dotnet_validation_summary block added;
  validation_summary counts updated (136/69/381); markdown report updated.
- scripts/test-proof-packet.ps1: schema sentinel → v0.10; dotnet_validation_summary field
  added to required check; 13 new dotnet section assertions (69 total, was 55).
- scripts/test-schema-files.ps1: proof-packet check updated from v0.9 to v0.10;
  dotnet_validation_summary added to CheckRequired (136 total, was 134).
- docs/IMPLEMENTATION.md: proof packet row updated to v0.10.

dotnet build: 0 errors
dotnet test:  152/152 (123 Core + 29 Cli)
scripts/test-schema-files.ps1: 136 assertions pass
scripts/test-proof-packet.ps1: 69 assertions pass (6 new dotnet lane checks)
scripts/validate.ps1: 69 assertions pass, Validation passed

---

## Previous

### Added
- tests/PZMapForge.Cli.Tests/FullPipelineContractTests.cs: FullPipelineContractFixture
  (IClassFixture) runs full-pipeline once against a temp 300x300 grass image.
  6 contract tests: exit code, regions-report.md claim boundary + summary-by-kind,
  primitives-report.md claim boundary + summary-by-primitive-type,
  plan-report.md claim boundary.

### Changed
- CHANGELOG.md: contract test entry added.

dotnet build: 0 errors, 2 pre-existing warnings
dotnet test:  152/152 (123 Core + 29 Cli)
scripts/validate.ps1: 55 PS assertions pass.

---

## Previous

### Added
- src/PZMapForge.Core/Regions/RegionArtifactWriter.cs: Write() now returns
  (string JsonPath, string MdPath) and also writes regions-report.md with
  claim boundary, summary-by-kind table, top-20-regions table.
- src/PZMapForge.Core/Primitives/PrimitiveArtifactWriter.cs: Write() now returns
  (string JsonPath, string MdPath) and also writes primitives-report.md with
  claim boundary, summary-by-primitive-type table, top-20-primitives table.
- tests/PZMapForge.Core.Tests/Regions/RegionArtifactWriterTests.cs: 8 tests
  (5 JSON + 3 markdown: file exists, claim boundary, summary-by-kind).
- tests/PZMapForge.Core.Tests/Primitives/PrimitiveArtifactWriterTests.cs: 8 tests
  (5 JSON + 3 markdown: file exists, claim boundary, summary-by-primitive-type).

### Changed
- src/PZMapForge.Cli/Program.cs: full-pipeline now prints regions-report.md and
  primitives-report.md paths. 7 artifacts emitted total.
- tests/PZMapForge.Cli.Tests/CliProcessTests.cs: Test 7 now verifies 7 artifacts
  including regions-report.md and primitives-report.md.
- docs/IMPLEMENTATION.md: artifact writer rows updated to 8 tests each.
- docs/IMAGE_MAPFORGE.md: full-pipeline artifact list updated.

dotnet build: 0 errors, 0 warnings
dotnet test:  146/146 (123 Core + 23 Cli)
scripts/validate.ps1: 55 PS assertions pass.

---

## [Unreleased - prev32]

### Added
- tests/PZMapForge.Cli.Tests/CliProcessTests.cs: 10 process-level integration
  tests invoking the CLI via dotnet run and verifying exit codes and artifacts.
  1. image-check 300x300 -> exit 0, Status OK
  2. image-check 150x150 without --resize -> exit 1
  3. image-check 150x150 with --resize -> exit 0
  4. image-export writes parsed-cell.json
  5. plan-check on valid fixture -> exit 0
  6. plan-export writes plan-recommendations.json + plan-report.md
  7. full-pipeline writes all 3 artifacts
  8. full-pipeline refuses non-.local output -> exit 1
  9. plan-check --tiny-threshold abc -> exit 1
  10. plan-check --tiny-threshold -1 -> exit 1

### Changed
- tests/PZMapForge.Cli.Tests/PZMapForge.Cli.Tests.csproj: System.Drawing.Common
  10.0.8 added for programmatic test image creation.

dotnet build: 0 errors, 0 warnings
dotnet test:  130/130 (107 Core + 23 Cli)
scripts/validate.ps1: 365 PS assertions unchanged.

---

## [Unreleased - prev31]

### Added
- src/PZMapForge.Cli/Program.cs: full-pipeline --path --palette [--output]
  [--resize] [--tiny-threshold] [--large-threshold]. Chains:
  ImageMapForgeParser -> ImageMapForgeArtifactWriter (parsed-cell.json) ->
  ParsedCellLoader -> RegionExtractor -> PrimitiveClassifier ->
  PlanningRuleEngine -> PlanningArtifactWriter (plan-recommendations.json,
  plan-report.md). Default output .local/mapforge. Refuses non-.local output.
  Prints: parsed-cell path, plan paths, dims, resized, regions, primitives,
  recommendations, warnings, thresholds, status.
- tests/PZMapForge.Cli.Tests/CliSmokeTests.cs: 3 new full-pipeline coverage
  tests (ArtifactWriter accessible, PlanningArtifactWriter accessible, default
  output path under .local). 13 total CLI tests (was 10).

dotnet build: 0 errors, 0 warnings
dotnet test:  120/120 (107 Core + 13 Cli)
scripts/validate.ps1: 365 PS assertions unchanged.

---

## [Unreleased - prev30]

### Added
- src/PZMapForge.Core/ImageParsing/ImageMapForgeArtifactWriter.cs: writes
  parsed-cell.json from ImageMapForgeResult + PaletteDocument. Builds a full
  ParsedCellDocument (schema, tool, claim_boundary, source/palette paths and
  SHA-256, width/height/resized, matching, legend, counts, nearest_drift, rows,
  outputs). Output is loadable by ParsedCellLoader and compatible with the full
  .NET downstream pipeline.
- src/PZMapForge.Cli/Program.cs: image-export --path --palette [--output]
  [--resize] command. Defaults to .local/mapforge. Refuses output outside
  .local/. Calls ImageMapForgeArtifactWriter.Write().
- tests/PZMapForge.Core.Tests/ImageParsing/ImageMapForgeArtifactWriterTests.cs:
  9 xUnit tests (file created, schema, claim_boundary, dims, rows, counts,
  resized flag, determinism, loadable by ParsedCellLoader).

dotnet build: 0 errors, 0 warnings
dotnet test:  117/117 (107 Core + 10 Cli)
scripts/validate.ps1: 365 PS assertions unchanged.

---

## [Unreleased - prev29]

### Added
- src/PZMapForge.Cli/Program.cs: image-check --path --palette [--resize] command.
  Calls ImageMapForgeParser.Parse(). Prints image/palette paths, dimensions,
  resized flag, row count, kind count, exact/nearest/unmapped pixels, palette
  SHA-256, status. Exits 0 on success, 1 on error. Does not write artifacts.
- tests/PZMapForge.Cli.Tests/CliSmokeTests.cs: 3 new ImageMapForge smoke tests
  (DefaultResize=false, ResizeTrue construction, Parser accessible). 10 total
  CLI tests (was 7).

### Changed
- src/PZMapForge.Cli/PZMapForge.Cli.csproj: <NoWarn>CA1416</NoWarn> added
  (Windows-only CLI; System.Drawing.Common calls are intentional).

dotnet build: 0 errors, 0 warnings
dotnet test:  108/108 (98 Core + 10 Cli)
scripts/validate.ps1: 365 PS assertions unchanged.

---

## [Unreleased - prev28]

### Added
- tests/PZMapForge.Core.Tests/ImageParsing/ImageMapForgeParserCrossVerificationTests.cs:
  6 cross-verification tests comparing ImageMapForgeParser output against
  tests/fixtures/parsed-cell/valid.json:
  1. HeaderFields: width==300, height==300, resized==false, claim_boundary correct
  2. PaletteSha256: parser result matches actual source/image-palette.json hash
     (fixture has placeholder zeros; test documents this explicitly)
  3. AllRows: all 300 rows identical to fixture
  4. Counts: all 9 kinds with correct pixel counts match fixture
  5. MatchingStats: exact==90000, nearest==0, unique==9, unmapped==0
  6. Resize: 150x150 all-grass -> 300x300 with all 90000 grass pixels

dotnet test: 105/105 (98 Core + 7 Cli)
scripts/validate.ps1: 365 PS assertions unchanged.

---

## [Unreleased - prev27]

### Added
- src/PZMapForge.Core/ImageParsing/ImageMapForgeOptions.cs: Resize flag
  (default false).
- src/PZMapForge.Core/ImageParsing/ImageMapForgeResult.cs: Width, Height,
  Resized, PaletteSha256, Rows, Counts, Matching, NearestDrift, BuildGrid().
- src/PZMapForge.Core/ImageParsing/ImageMapForgeParser.cs: [SupportedOSPlatform
  windows] static Parse(imagePath, palettePath, options). Loads palette via
  PaletteLoader, computes palette SHA-256 from file bytes, loads image with
  System.Drawing.Common (GDI+), resizes with NearestNeighbor if Resize=true,
  scans pixels with exact-then-nearest-colour matching (drift cache per unique
  unmapped colour), returns ImageMapForgeResult.
- tests/PZMapForge.Core.Tests/ImageParsing/ImageMapForgeParserTests.cs:
  10 xUnit tests using programmatically created temp images. Covers 300x300,
  150x150 without/with Resize, dimension/row/count validation, resized flag,
  palette SHA-256, invalid path, unsupported extension, determinism.
- System.Drawing.Common 10.0.8 added to PZMapForge.Core and PZMapForge.Core.Tests.

dotnet build: 0 errors, 0 warnings
dotnet test:  99/99 pass (92 Core + 7 Cli)
scripts/validate.ps1: 365 PS assertions unchanged.

---

## [Unreleased - prev26]

### Added
- schemas/pzmapforge.proof-packet.v0.9.schema.json: plan_recommendations_contract
  const=28, proof_packet const=55, total_expected_assertions const=365.

### Changed
- scripts/test-plan-recommendations-contract.ps1: thresholds_used section
  added (+7 assertions): field exists, tiny/large sub-fields exist,
  both >= 0, tiny==9, large==50000. 28 total (was 21).
- scripts/write-proof-packet.ps1: bumped to v0.9; plan_recommendations_contract=28,
  total=365.
- scripts/test-proof-packet.ps1: v0.9; plan_recommendations_contract==28;
  total==365. 55 assertions (unchanged count).
- scripts/test-schema-files.ps1: proof-packet -> v0.9; total 134 unchanged.
- docs/IMPLEMENTATION.md: contract and proof packet rows updated.

Full PowerShell pipeline: 134+40+5+21+36+24+22+28+55 = 365 assertions. All pass.
dotnet test: 89/89 unchanged.

---

## [Unreleased - prev25]

### Added
- plan-recommendations.json now includes thresholds_used object
  (tiny_building_pixel_threshold, large_ground_pixel_threshold) recording
  the PlanningRuleOptions values active during export.

### Changed
- PlanningArtifactWriter.Write: added PlanningRuleOptions? options = null
  parameter (before overrideGeneratedAt). Defaults to PlanningRuleOptions.Default
  when null. Backward-compatible via default and named arguments.
- PlanExportCommand: passes parsed opts to PlanningArtifactWriter.Write.
- schemas/pzmapforge.plan-recommendations.v0.1.schema.json: thresholds_used
  added to required and properties.
- tests/fixtures/plan-recommendations/valid.json: regenerated with
  thresholds_used: {tiny_building_pixel_threshold: 9, large_ground_pixel_threshold: 50000}.
- PlanningArtifactWriterTests: 2 new tests (default/custom thresholds recorded).
  10 total (was 8).
- PlanningArtifactCrossVerificationTests: thresholds_used fields added to
  CrossVerify_HeaderFieldsMatch.

dotnet test: 89/89 (82 Core + 7 Cli)
scripts/validate.ps1: 358 PS assertions unchanged.

---

## [Unreleased - prev24]

### Added
- src/PZMapForge.Cli/Program.cs: --tiny-threshold and --large-threshold optional
  flags for plan-check and plan-export. ParsePlanningOptions() helper parses
  both flags, returns (PlanningRuleOptions?, errorCode). Non-integer value or
  negative value prints a clear error and exits 1. Both commands print
  "Tiny threshold: N" and "Large threshold: N" in their output.
- tests/PZMapForge.Cli.Tests/CliSmokeTests.cs: 5 new threshold tests (default
  values, custom values accepted, zero tiny valid, negative tiny throws,
  negative large throws). 7 total Cli tests.

dotnet build: 0 errors
dotnet test:  87/87 pass (80 Core + 7 Cli)

---

## [Unreleased - prev23]

### Added
- src/PZMapForge.Core/Planning/PlanningRuleOptions.cs: configurable thresholds.
  TinyBuildingPixelThreshold (default 9) and LargeGroundPixelThreshold
  (default 50000). Both must be >= 0; ArgumentOutOfRangeException otherwise.
  PlanningRuleOptions.Default provides the original hardcoded values.
- PlanningRuleEngine.Evaluate(primitives, options) overload; existing no-options
  overload delegates to it via PlanningRuleOptions.Default.
- tests/PZMapForge.Core.Tests/Planning/PlanningRuleEngineOptionsTests.cs:
  8 xUnit tests: default preserves output, zero-threshold suppresses tiny
  warnings (1px buildings not <= 0), lower threshold = fewer warnings,
  threshold equals pixel_count triggers, higher large-ground suppresses note,
  lower large-ground triggers, negative tiny throws, negative large throws.

### Changed
- src/PZMapForge.Core/Planning/PlanningRuleEngine.cs: removed private const
  thresholds; now reads from PlanningRuleOptions.
- docs/PLANNING_RULES.md: threshold table updated to reflect PlanningRuleOptions
  field names; configurable usage example added.

dotnet build: 0 errors
dotnet test:  82/82 pass (80 Core + 2 Cli)
scripts/validate.ps1: 358 PS assertions unchanged.

---

## [Unreleased - prev22]

### Added
- tests/fixtures/plan-recommendations/valid.json: committed reference fixture
  generated from tests/fixtures/parsed-cell/valid.json via PlanningArtifactWriter.
  9 primitives, 13 recommendations (3 warnings/tiny_building + 10 info),
  warning_count=3, total_pixels=90000.
- tests/PZMapForge.Core.Tests/Planning/PlanningArtifactCrossVerificationTests.cs:
  3 [Fact] cross-verification tests comparing .NET pipeline output against the
  committed fixture (header fields, all 13 recommendation details incl. bounds,
  summary counts_by_type/counts_by_severity). EnsureFixture() generates the
  fixture if missing (developer commits on first run).

dotnet test: 74/74 (72 Core + 2 Cli)
scripts/validate.ps1: 358 PS assertions unchanged.

---

## [Unreleased - prev21]

### Added
- scripts/test-plan-recommendations-contract.ps1: 21-assertion contract
  validator for .local/mapforge/plan-recommendations.json. Checks: 2 output
  files, schema/claim_boundary/width/height sentinels, 4 count integrity
  checks, recommendations array exists/count-matches/5-field presence,
  summary exists/matches/counts_by_severity sum. Generates artifact via
  dotnet plan-export if missing (no validate.ps1 recursion).
- schemas/pzmapforge.proof-packet.v0.8.schema.json: adds
  plan_recommendations_contract (const: 21) to validation_summary; updates
  proof_packet (55) and total_expected_assertions (358).

### Changed
- scripts/validate.ps1: plan-recommendations contract step added between
  plan-export and proof packet.
- scripts/write-proof-packet.ps1: bumped to v0.8; adds
  plan_recommendations_contract=21, proof_packet=55, total=358.
- scripts/test-proof-packet.ps1: v0.8; adds plan_recommendations_contract==21;
  total==358. 55 assertions (was 54).
- scripts/test-schema-files.ps1: proof-packet -> v0.8; total unchanged at 134.
- docs/IMPLEMENTATION.md: plan contract ratified; proof packet v0.8.

Full PowerShell pipeline: 134+40+5+21+36+24+22+21+55 = 358 assertions. All pass.

---

## [Unreleased - prev20]

### Added
- schemas/pzmapforge.proof-packet.v0.7.schema.json: adds plan_recommendations_
  sha256/plan_report_sha256 to required; updates validation_summary consts
  (schema_file_sanity=134, proof_packet=54, total=336).

### Changed
- scripts/test-schema-files.ps1: proof-packet -> v0.7 (16 CheckRequired, +2);
  plan-recommendations schema section added (10 CheckRequired, 26 assertions).
  134 total assertions (was 104).
- scripts/write-proof-packet.ps1: bumped to v0.7; adds plan_recommendations_
  path/report_path/sha256 fields; runs plan-export if artifacts missing;
  schema_file_sanity=134, proof_packet=54, total=336.
- scripts/test-proof-packet.ps1: v0.7; +4 required field checks, +2 SHA-256
  checks; schema_file_sanity==134; total==336. 54 assertions (was 48).
- scripts/validate.ps1: plan-export step (dotnet run --no-build) inserted
  before proof packet step.
- docs/IMPLEMENTATION.md: plan-recs schema sanity ratified; proof packet v0.7.

Full PowerShell pipeline: 134+40+5+21+36+24+22+54 = 336 assertions. All pass.

---

## [Unreleased - prev19]

### Added
- src/PZMapForge.Core/Planning/PlanningArtifactWriter.cs: writes
  plan-recommendations.json (schema v0.1) and plan-report.md from a
  PlanningRuleResult. Accepts DateTimeOffset? overrideGeneratedAt for
  deterministic testing. File stream disposed with using block.
- schemas/pzmapforge.plan-recommendations.v0.1.schema.json: JSON Schema.
- docs/PLAN_EXPORT.md: artifact structure, determinism, output path safety.
- src/PZMapForge.Cli/Program.cs: plan-export --path --output command.
  Refuses output outside .local/. Prints JSON path, markdown path,
  primitive count, recommendation count, warning count, status.
- tests/PZMapForge.Core.Tests/Planning/PlanningArtifactWriterTests.cs:
  8 xUnit tests: JSON/md files created, schema sentinel, claim boundary,
  recommendation_count, warning_count, markdown contains claim boundary,
  determinism with fixed timestamp.

dotnet build: 0 errors
dotnet test:  71/71 pass (69 Core + 2 Cli)
plan-export: plan-recommendations.json + plan-report.md written, Status OK

---

## [Unreleased - prev18]

### Added
- src/PZMapForge.Core/Planning/: planning rule engine.
  - PlanningSeverity enum (Warning, Info).
  - PlanningRecommendationType enum (10 values) + ToTypeString() extension.
  - PlanningRecommendation: source primitive id, type, severity, role, pixel
    count, bounds; id=0 for global recommendations.
  - PlanningSummary: total pixels, primitive count, recommendation count,
    warning count, counts by type and severity.
  - PlanningRuleResult: ClaimBoundary, Recommendations, Summary.
  - PlanningRuleEngine.Evaluate(PrimitiveClassificationResult):
    per-primitive rules for all 7 types; tiny_building_candidate warning
    (pixel_count <= 9); large_open_ground_area info (pixel_count > 50000);
    missing_spawn_marker global warning; deterministic sort (severity ASC,
    type ASC, pixel_count DESC, y ASC, x ASC, primitive_id ASC).
- tests/PZMapForge.Core.Tests/Planning/PlanningRuleEngineTests.cs:
  15 xUnit tests covering non-empty output, claim boundary, count consistency,
  all 7 primitive-type mappings ([Theory]), missing spawn warning, determinism,
  counts_by_type stability, counts_by_severity stability, source primitive id.
- docs/PLANNING_RULES.md: rules table, thresholds, sort order, output model.
- src/PZMapForge.Cli/Program.cs: plan-check --path command.

dotnet build: 0 errors
dotnet test:  63/63 pass (61 Core + 2 Cli)
plan-check output: 300x300, 20 primitives, 20 recommendations, 0 warnings, OK

---

## [Unreleased - prev17]

### Added
- tests/test-image-mapforge.ps1 Test 11: -Resize coverage (8 assertions).
  Creates a 150x150 all-grass image, runs image-mapforge.ps1 with -Resize,
  asserts exit 0, parsed-cell.json written, width==300, height==300,
  rows.Count==300, all row lengths==300, counts sum==90000, resized==true.
  Closes IMPLEMENTATION.md gap 1. Hardening harness total: 36 (was 28).
- schemas/pzmapforge.proof-packet.v0.6.schema.json: hardening_harness const=36,
  total_expected_assertions const=300.

### Changed
- scripts/write-proof-packet.ps1: bumped to v0.6; hardening_harness=36, total=300.
- scripts/test-proof-packet.ps1: expects v0.6; hardening_harness==36; total==300.
  48 assertions unchanged.
- scripts/test-schema-files.ps1: proof-packet section updated to v0.6.
  Total schema assertions unchanged at 104.
- docs/IMPLEMENTATION.md: -Resize ratified; gap 1 closed; proof packet v0.6.

### Note
Full PowerShell pipeline: 104+40+5+21+36+24+22+48 = 300 assertions. All pass.
All three original known gaps now closed:
  Gap 1: -Resize flag (closed this slice)
  Gap 2: TMX structural validation (closed by test-tmx-integrity.ps1)
  Gap 3: palette_sha256 verification (closed by test-palette-sha256.ps1)

---

## [Unreleased - prev16]

### Added
- scripts/test-palette-sha256.ps1: 5-assertion palette hash verifier. Checks
  parsed-cell.json exists, source/image-palette.json exists, palette_sha256
  field is present and 64-char hex, and matches the computed SHA-256 of
  source/image-palette.json. Closes IMPLEMENTATION.md gap 3.
- schemas/pzmapforge.proof-packet.v0.5.schema.json: adds
  palette_sha256_verification (const: 5) to validation_summary; updates
  proof_packet (48) and total_expected_assertions (292).

### Changed
- scripts/validate.ps1: palette SHA-256 verification step inserted after
  artifact contract and before TMX integrity.
- scripts/write-proof-packet.ps1: bumped to v0.5; adds
  palette_sha256_verification=5, proof_packet=48, total=292.
- scripts/test-proof-packet.ps1: expects v0.5; adds
  palette_sha256_verification==5; total==292. 48 assertions (was 47).
- scripts/test-schema-files.ps1: proof-packet section updated to v0.5.
  Total schema assertions unchanged at 104.
- docs/IMPLEMENTATION.md: palette SHA-256 ratified; gap 3 closed.

### Note
Full PowerShell pipeline: 104+40+5+21+28+24+22+48 = 292 assertions, all pass.

---

## [Unreleased - prev15]

### Added
- schemas/pzmapforge.proof-packet.v0.4.schema.json: proof packet schema v0.4.
  Adds tmx_integrity (const: 21) to validation_summary; updates proof_packet
  (47) and total_expected_assertions (286).

### Changed
- scripts/write-proof-packet.ps1: bumped to v0.4; added tmx_integrity=21,
  updated proof_packet=47, total=286.
- scripts/test-proof-packet.ps1: expects v0.4; adds tmx_integrity==21 check;
  total_expected_assertions==286. 47 assertions (was 46).
- scripts/test-schema-files.ps1: proof-packet section now validates v0.4.
  Total schema assertions unchanged at 104.
- docs/IMPLEMENTATION.md: proof packet row updated to v0.4/47 assertions.

### Note
Full PowerShell pipeline: schema (104) + contract (40) + TMX integrity (21) +
hardening (28) + regions (24) + primitives (22) + proof packet (47) = 286
assertions. All pass.

---

## [Unreleased - prev14]

### Added
- src/PZMapForge.Cli/Program.cs: primitive-check --path <path> command.
  Loads parsed-cell, extracts regions, classifies primitives, prints
  dimensions/regions/primitives/primitive-types/pixels/status. Exits 0
  on success; exits 1 if parsed-cell is invalid or classification fails
  (e.g. unmapped kind).
- tests/PZMapForge.Cli.Tests/CliSmokeTests.cs: PrimitiveClassifier_IsAccessible
  test confirming PrimitiveClassifier.IsKnownKind and all 7 PlanningPrimitiveType
  enum values are accessible from the CLI test project.

---

## [Unreleased - prev13]

### Added
- src/PZMapForge.Core/Primitives/: typed primitive classifier.
  - PlanningPrimitiveType enum (7 values mirroring PS kindMap output).
  - PlanningPrimitive, PrimitiveKindSummary, PrimitiveClassificationResult.
  - PrimitiveClassifier.Classify(RegionExtractionResult): ports PS classify-
    primitives.ps1 exactly -- same 9->7 kind mapping, same sort order
    (primitive_type ASC, pixel_count DESC, y ASC, x ASC, source_region_id ASC),
    same sequential primitive_id, same summary_by_primitive_type aggregation.
    Throws ArgumentException for unmapped kinds.
- tests/PZMapForge.Core.Tests/Primitives/PrimitiveClassifierTests.cs:
  16 xUnit tests: 8 [Theory] kind-mapping assertions + Classify_ValidFixture
  (count, coverage), Classify_IsDeterministic, Classify_NoUnclassifiedRegions,
  Classify_UnknownKind_Throws, Classify_BuildingFootprintAggregates3Kinds,
  Classify_MatchesPsReferenceFixture (full cross-verification).
- tests/fixtures/primitives/valid.json: PS-generated reference fixture (9
  primitives, 90000 px, 7 primitive types; generated by running classify-
  primitives.ps1 against tests/fixtures/regions/valid.json).
- src/PZMapForge.Core/Regions/: public CreateForTesting factory methods added
  to SemanticRegion, RegionKindSummary, and RegionExtractionResult to support
  isolated unit tests without going through ParsedCellLoader/RegionExtractor.

---

## [Unreleased - prev12]

### Added
- tests/PZMapForge.Core.Tests/Regions/RegionCrossVerificationTests.cs:
  3 cross-verification [Fact] tests asserting the .NET RegionExtractor
  matches the PowerShell extract-regions.ps1 reference for the same input:
  1. TotalsMatch: total region count and total pixel count
  2. SummaryByKindMatches: region_count, total_pixels, largest_region_pixels
     per kind; no extra kinds in either direction
  3. RegionDetailsMatch: kind, code, pixel_count, bounds (x,y,w,h), centroid
     (x,y to 2 decimal places) for all 9 regions (< 20 cap)
- tests/fixtures/regions/valid.json: PS-generated reference fixture from
  tests/fixtures/parsed-cell/valid.json; 9 regions, 90000 pixels,
  grass centroid (149.49, 149.51).

---

## [Unreleased - prev11]

### Added
- src/PZMapForge.Core/Regions/: typed .NET region extractor.
  - RegionBounds, RegionCentroid, SemanticRegion, RegionKindSummary,
    RegionExtractionResult: typed region models.
  - RegionExtractor.Extract(SemanticGrid, IReadOnlyDictionary<char,string>):
    BFS flood-fill with 4-neighbor connectivity, deterministic sort
    (kind ASC, pixel_count DESC, y ASC, x ASC, discovery_id ASC),
    sequential region_id, summary_by_kind. Uses integer modulo decomposition
    (cx = idx % w; cy = idx / w) to avoid floating-point rounding issues.
- SemanticGrid.CreateForTesting(w, h, rows): public factory for test fixtures
  that bypass ParsedCellLoader's 300x300 constraint.
- src/PZMapForge.Cli/Program.cs: region-check --path <path> command.
  Loads parsed-cell, extracts regions, prints dims/regions/kinds/pixels/status.
- tests/PZMapForge.Core.Tests/Regions/RegionExtractorTests.cs:
  8 xUnit tests: all-grass=1 region, valid fixture has 9 kinds, pixel sum 90000,
  all regions positive, bounds in grid, centroid in bounds, deterministic,
  diagonal cells are separate regions (4-neighbor proof).

---

## [Unreleased - prev10]

### Added
- src/PZMapForge.Core/ParsedCell/: typed parsed-cell artifact reader.
  - ParsedCellDocument, ParsedCellMatching, ParsedCellLegendEntry,
    ParsedCellCount, ParsedCellDrift, ParsedCellOutputs: JSON-mapped models.
  - SemanticGrid: InBounds(x,y), GetCode(x,y) throws on OOB, CountCode(code).
  - ParsedCellLoadResult: IsValid, Document, Grid, Errors.
  - ParsedCellLoader.Load(path): validates schema, claim_boundary, width==300,
    height==300, rows count/length, counts sum==90000, all 9 required kinds.
- src/PZMapForge.Cli/Program.cs: parsed-cell-check --path <path> command.
- tests/PZMapForge.Core.Tests/ParsedCell/ParsedCellLoaderTests.cs:
  11 xUnit tests (valid fixture, missing file, wrong schema, wrong claim
  boundary, wrong width, bad row count, bad row length, counts sum mismatch,
  missing required kind, GetCode works, GetCode out-of-bounds throws).
- tests/fixtures/parsed-cell/valid.json: checked-in minimal 300x300 fixture
  (292 grass + 1 each of all 8 non-grass kinds per row 0; rows 1-299 all grass;
  counts sum == 90000; all 9 required kinds present).
- docs/IMPLEMENTATION.md: parsed-cell reader and CLI command ratified.

---

## [Unreleased - prev9]

### Added
- PZMapForge.slnx: .NET 10 solution (.slnx format, dotnet 10 SDK default).
- src/PZMapForge.Core: class library with typed palette models
  (PaletteDocument, PaletteKind, PaletteValidationResult) and PaletteLoader
  which reads source/image-palette.json, validates schema/dims/kinds/GIDs/
  codes/RGB, and returns structured errors.
- src/PZMapForge.Cli: console app with palette-check command. Exits 0 on
  valid palette; prints schema, dimensions, kind count, GID range, status.
  Usage: dotnet run --project src/PZMapForge.Cli -- palette-check --palette
  source/image-palette.json
- tests/PZMapForge.Core.Tests: 8 xUnit tests covering valid canonical palette,
  missing file, duplicate GID, missing required kind, invalid RGB, duplicate
  code, wrong schema, wrong cell_width.
- tests/PZMapForge.Cli.Tests: 1 smoke test confirming PZMapForge.Core loads.
- .gitignore: bin/, obj/, .vs/ added for .NET build artifacts.

### Notes
PowerShell scripts are unchanged. The .NET engine is additive only.
All existing PowerShell validation (285 assertions) continues to pass.

---

## [Unreleased - prev8]

### Added
- scripts/test-tmx-integrity.ps1: 21-assertion TMX structural validator.
  Checks map/tileset/layer XML attributes, decodes base64+gzip payload,
  verifies decompressed length == 360000, GID count == 90000, all GIDs
  in range 1..9. Closes IMPLEMENTATION.md gap 2.
- docs/TMX_INTEGRITY.md: validator design, payload encoding, assertions table,
  claim boundary.
- scripts/validate.ps1: TMX integrity step added after artifact contract
  and before hardening harness.
- docs/IMPLEMENTATION.md: TMX structural integrity ratified; gap 2 closed;
  TileZed provisional note updated.

### Note
Proof packet stays at v0.3. The running pipeline total is now 285 assertions
(104+40+21+28+24+22+46). Proof packet v0.4 will correct the stale counts
(schema_file_sanity and total_expected_assertions).

---

## [Unreleased - prev7]

### Added
- schemas/pzmapforge.proof-packet.v0.3.schema.json: proof packet schema v0.3
  covering all three artifact groups (ImageMapForge, region, primitive) and
  updated validation_summary consts (schema_file_sanity=104, primitive_
  classification=22, proof_packet=46, total=264).

### Changed
- scripts/write-proof-packet.ps1: bumped to v0.3. Hashes primitives.json and
  primitives-report.md; runs classify-primitives.ps1 if missing; updated
  all validation_summary counts.
- scripts/test-proof-packet.ps1: 46 assertions (was 39). 4 new required
  fields, 2 new SHA-256 checks, primitive_classification=22, total=264.
- scripts/test-schema-files.ps1: proof-packet section updated to v0.3
  (14 checked fields, was 12) -- 104 total schema assertions (was 100).
- docs/IMPLEMENTATION.md: proof packet and schema sanity rows updated.

---

## [Unreleased - prev6]

### Added
- scripts/classify-primitives.ps1: maps 9 semantic kinds to 7 planning
  primitive types (road_region, sidewalk_region, building_footprint,
  yard_region, landmark_marker, spawn_marker, ground_region). Reads
  regions.json, sorts primitives deterministically (primitive_type ASC,
  pixel_count DESC, y ASC, x ASC, source_region_id ASC), writes
  primitives.json and primitives-report.md.
- scripts/test-primitive-classification.ps1: 22-assertion harness (output
  files, sentinels, dimensions, structure, bounds, centroids, all 7 types
  present, pixel sum 90000, determinism, gitignore proof).
- schemas/pzmapforge.primitives.v0.1.schema.json: JSON Schema for primitives.
- docs/PRIMITIVE_CLASSIFICATION.md: kind-to-primitive mapping table, output
  fields, deterministic sort order, claim boundary.
- scripts/test-schema-files.ps1: primitives schema added -- 100 total
  assertions (was 78 for 3 schemas).
- scripts/validate.ps1: primitive classification step added before proof packet.
- docs/IMPLEMENTATION.md: primitive classification ratified.

### Note
Proof packet remains at v0.2 (schema_file_sanity hardcoded at 78, total at 209).
These counts are now stale. Proof packet v0.3 will cover primitive artifacts
and correct the counts.

---

## [Unreleased - prev5]

### Added
- schemas/pzmapforge.proof-packet.v0.2.schema.json: proof packet schema v0.2
  covering region artifact fields (regions_json_path, regions_report_path,
  regions_json_sha256, regions_report_sha256) and updated validation_summary
  (schema_file_sanity=78, region_extraction=24, proof_packet=39, total=209).

### Changed
- scripts/write-proof-packet.ps1: bumped to v0.2. Now hashes regions.json and
  regions-report.md; runs extract-regions.ps1 if regions.json is missing;
  updated validation_summary counts.
- scripts/test-proof-packet.ps1: updated for v0.2 contract — 39 assertions
  (was 32): 4 new required fields, 2 new SHA-256 checks, region_extraction=24,
  total=209.
- scripts/test-schema-files.ps1: proof-packet section now validates v0.2
  (12 checked fields, was 10) — 78 total schema assertions (unchanged count
  since proof-packet gained 4 but the total was already recomputed correctly).
- docs/IMPLEMENTATION.md: proof packet row updated to v0.2.

---

## [Unreleased - prev4]

### Added
- scripts/extract-regions.ps1: BFS flood-fill region extraction from
  parsed-cell.json rows using 4-neighbor connectivity. Outputs
  regions.json (schema, claim, regions[], summary_by_kind[]) and
  regions-report.md. Deterministic sort: kind ASC, pixel_count DESC,
  y ASC, x ASC. Fixed PS5.1 [int] rounding bug: uses $cur % $W for
  flat-index decomposition instead of [int]($cur / $W). Used @() array
  with PSCustomObject elements to avoid ConvertTo-Json wrapping $finalRegions
  as {"value":[...]} instead of a JSON array.
- scripts/test-region-extraction.ps1: 24-assertion harness (output files,
  schema/claim sentinels, dimensions, regions structure, bounds validity,
  centroid within bounds, summary 9 kinds, pixel sum 90000, determinism,
  gitignore proof).
- schemas/pzmapforge.regions.v0.1.schema.json: JSON Schema for regions.json.
- docs/REGION_EXTRACTION.md: 4-neighbor BFS docs, sort order, output fields,
  PS5.1 [int] rounding bug note.
- scripts/test-schema-files.ps1: extended to validate all 3 schemas
  (parsed-cell, proof-packet, regions) — 74 total assertions (was 28).
- scripts/validate.ps1: region extraction step (extract + test) added before
  proof packet.
- docs/IMPLEMENTATION.md: region extraction and updated schema sanity row.

---

## [Unreleased - prev3]

### Added
- scripts/write-proof-packet.ps1: generates .local/mapforge/proof-packet.json
  and proof-packet.md with schema sentinel, UTC timestamp, git state,
  SHA-256 hashes of all 5 artifacts, expected validation counts, and safety
  flags. Runs validate.ps1 first if parsed-cell.json is missing.
- scripts/test-proof-packet.ps1: 32-assertion proof packet validator (output
  files exist, 15 required fields, schema/claim sentinels, 5 SHA-256 formats,
  validation summary counts, 4 safety flags).
- schemas/pzmapforge.proof-packet.v0.1.schema.json: JSON Schema for proof packet.
- scripts/validate.ps1: proof packet write + test steps added after hardening.
- docs/IMPLEMENTATION.md: proof packet row added to ratified table.

---

## [Unreleased - prev2]

### Added
- scripts/test-schema-files.ps1: schema file sanity validator (28 assertions:
  $schema, $id sentinel, title, required list for 11 fields, properties keys
  for those same fields). No external dependencies.
- scripts/validate.ps1: schema sanity step added before artifact contract step.
- docs/IMPLEMENTATION.md: schema sanity row added to ratified table.
- Fixed: CHANGELOG and IMPLEMENTATION.md had stale "33 assertions" for the
  parsed-cell contract; corrected to 40.

---

## [Unreleased - prev]

### Added
- scripts/test-parsed-cell-contract.ps1: deterministic artifact contract check
  (40 assertions: required fields, schema sentinel, claim_boundary, dimensions
  300x300, rows count and length, counts pixel sum, all 9 required kinds,
  outputs keys, matching fields and pixel sum).
- scripts/validate.ps1: contract step added before hardening test harness.
- docs/IMPLEMENTATION.md: contract validation row added to ratified table;
  gap 4 added for JSON Schema type validation.

---

## [Unreleased - prev]

### Added
- Import hardened ImageMapForge MVP from pz-sud-ouest-montreal@5944173.
  - source/image-mapforge.ps1: RGB palette format, Fail+exit pattern, Debug
    exits early (no artifacts), nearest-colour drift cache and drift records
    in JSON and report.
  - source/image-palette.json: RGB array format (9 kinds, contiguous GIDs).
  - tests/test-image-mapforge.ps1: 28-assertion hardening harness.
- docs/GENESIS.md: why PZMapForge exists, scope, identity.
- docs/CONSTITUTION.md: non-negotiable behavioral rules.
- docs/IMPLEMENTATION.md: ratified, provisional, and absent capabilities.
- docs/TOOL_USAGE.md: parameters, examples, palette format, colour matching.
- docs/decisions/0001-independent-mapmaker-layer.md: decision record.
- docs/decisions/0002-planning-artifacts-before-playable-export.md: decision record.
- schemas/pzmapforge.parsed-cell.v0.1.schema.json: JSON Schema for artifact.
- examples/README.md: how to create and use a custom blockout image.
- LICENSE: MIT.
- scripts/validate.ps1: updated to run tests/test-image-mapforge.ps1.
- scripts/new-test-image.ps1: updated to read RGB array palette format.

### Removed
- scripts/test-image-mapforge.ps1: moved to tests/.

---

## [0.0.1] - 2026-05-30

### Added
- Initial independent PZMapForge repository scaffold.
- ImageMapForge MVP script (hex palette format, symbol field).
- Palette configuration (hex colour format).
- Local sample image generator and validation wrapper.
- Claim boundary, ImageMapForge, and roadmap documentation.
- Drift tracking port from pz-sud-ouest-montreal (ff0a21f).
