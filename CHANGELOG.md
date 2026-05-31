# Changelog

All notable changes to PZMapForge will be documented here.

Format: Keep a Changelog.

---

## [Unreleased]

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
