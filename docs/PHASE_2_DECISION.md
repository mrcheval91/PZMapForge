# Phase 2 Decision Record

Date: 2026-05-31
Baseline commit: d15634b
Author: operator

---

## Background

PZMapForge Phase 1 is complete. The tool accepts a single PNG/BMP blockout image,
maps pixels to semantic kinds via a palette, and emits deterministic planning
artifacts (parsed-cell.json, regions.json/report, primitives.json/report,
plan-recommendations.json/report). The pipeline is fully process-tested, proof-packed,
and documented in the validation ledger.

Phase 2 must improve mapmaker expressiveness without compromising the claim boundary,
importing PZ assets, or claiming playable export.

---

## Current verified capability (Phase 1)

Input:
  single PNG/BMP blockout (300x300 or resizable)

Pipeline:
  image parsing -> parsed-cell.json
  BFS region extraction -> regions.json + regions-report.md
  primitive classification -> primitives.json + primitives-report.md
  planning rule evaluation -> plan-recommendations.json + plan-report.md

Tests:
  152 .NET xUnit tests (123 Core + 29 CLI)
  381 PowerShell validation assertions

Claim boundary: planning_artifact_only_not_pz_load_tested

---

## Option A: Multi-layer image conventions

### What it is

Accept a folder or manifest of separate per-layer images instead of a single
monolithic blockout. Each image encodes one semantic channel.

Proposed standard layers:

  terrain.png    background land kinds (grass, industrial_yard, etc.)
  roads.png      road and sidewalk kinds
  buildings.png  structure kinds (row_house, depanneur, garage, landmark)
  markers.png    special designations (spawn, etc.)

Each layer uses a restricted palette subset or its own palette section.
Layers are merged into one semantic grid before the existing downstream pipeline.

Merge precedence (highest wins per pixel):
  markers > buildings > roads > terrain

### Why this direction

- Improves map planning expressiveness immediately.
  Separate layers are easier to author and revise than one monolithic blockout.
  Road edits do not require redrawing buildings; spawn changes do not require
  touching terrain.

- Stays independent of PZ assets.
  Layer images are author-created blockouts, same as the current single image.
  No PZ tilesheets, lotpacks, or lotheader files are required.

- Avoids fake playable-export claims.
  The output is still parsed-cell.json and planning artifacts. The merge step
  is internal to the tool; the artifact contract is unchanged.

- Creates cleaner input semantics before any future tile/PZ bridge.
  If Option B is pursued later, a well-defined semantic grid with proper layer
  attribution is a better starting point than a single flat blockout.

- Fits the existing pipeline cleanly.
  The merge produces a SemanticGrid, which is already the typed input to
  RegionExtractor and PrimitiveClassifier. No downstream types need to change.

### Risks

- Manifest/folder convention must be documented clearly and locked to a schema.
  Ambiguity in layer order or missing-layer handling will produce wrong output.

- Palette subset per layer adds authoring complexity.
  Mitigation: permit the full palette in each layer but enforce that unexpected
  kinds raise a warning rather than an error.

- Precedence resolution is deterministic but not always obvious to authors.
  Mitigation: layer-merge-report.md should list conflict counts per cell.

### Phase 2 MVP scope

Input:
  folder containing one or more named layer files, or a manifest JSON

Layer discovery:
  terrain.png, roads.png, buildings.png, markers.png
  Missing layers are treated as fully transparent (no contribution).

Merge algorithm:
  For each cell, take the highest-precedence non-empty kind.
  markers > buildings > roads > terrain
  If a cell has no kind from any layer, it becomes the palette default (grass).

Outputs (same as Phase 1):
  parsed-cell.json  (from merged grid, schema unchanged)
  layer-merge-report.md  (NEW: conflict count, layer contributions summary)
  regions.json + regions-report.md
  primitives.json + primitives-report.md
  plan-recommendations.json + plan-report.md

New CLI command:
  layer-pipeline --layers <dir> --palette <palette.json> --output <outdir>

Existing commands remain unchanged.

Artifact claim boundary unchanged:
  planning_artifact_only_not_pz_load_tested

### What is NOT in Phase 2 MVP

- PZ tile ID mapping (deferred to Phase 3)
- Writing to media/maps (forbidden)
- Copying PZ tilesheets (forbidden)
- lotpack/lotheader/bin generation (Phase 4+)
- TMX generation from layer data (may be trivial extension, not in MVP)
- More than 4 layers (can be added later by extending manifest schema)

---

## Option B: Semantic kind -> PZ tile ID planning

### What it is

Map semantic cell kinds to PZ tile GIDs so that generated TMX uses actual
PZ tiles instead of color-strip planning tiles.

### Why it is deferred

- Requires access to local PZ installation.
  Tile ID tables change between PZ versions. The tool must not commit or
  distribute any PZ-owned file.

- PZ tile semantics are fragile.
  GIDs, tilesheet names, and tile counts differ by Build version (41, 42).
  A tile ID table committed at one version becomes wrong at the next.

- The claim boundary would need to advance.
  Using real PZ tiles implies closer compatibility with PZ format.
  That claim cannot be made without a real local load test.

- Premature before Phase 2.
  A flat single-layer blockout with real tile IDs is still a worse planning
  surface than a properly layered blockout with planning-only tiles.
  Option A first makes Option B cleaner.

### Conditions to revisit Option B

- Phase 2 MVP is complete and stable.
- A local PZ install configuration mechanism exists.
- A local load test harness exists or is planned.
- A decision record is written documenting the load test evidence.

---

## Recommendation

Proceed with Option A.

Phase 2 MVP = multi-layer image pipeline.

Rationale:
1. Improves tool expressiveness immediately.
2. Keeps claim boundary unchanged.
3. No PZ asset dependency.
4. Fits existing typed pipeline without downstream type changes.
5. Creates a better starting point for any future Option B work.

Option B is not rejected. It is deferred pending Phase 2 stability and a
documented local load test mechanism.

---

## Slice 2A-1: COMPLETE

Slice 2A-1 shipped in commit immediately following this decision record.

Files added:
  schemas/pzmapforge.layer-manifest.v0.1.schema.json
  src/PZMapForge.Core/Layers/LayerManifest.cs
  src/PZMapForge.Core/Layers/LayerManifestLayer.cs
  src/PZMapForge.Core/Layers/LayerManifestLoader.cs
  src/PZMapForge.Core/Layers/LayerManifestLoadResult.cs
  tests/PZMapForge.Core.Tests/Layers/LayerManifestLoaderTests.cs
  tests/fixtures/layers/valid-layer-manifest.json

Tests: 12 new (valid fixture, missing file, wrong schema, wrong claim boundary,
  wrong dimensions, duplicate layer names, missing layer in precedence,
  unknown layer in precedence, duplicate precedence entry, unknown allowed kind,
  empty allowed_kinds, empty layer path).

dotnet test: 164/164 (135 Core + 29 Cli). PS lane unchanged at 381.

## Slice 2A-2: COMPLETE

Files added:
  src/PZMapForge.Core/Layers/LayerMergeOptions.cs
  src/PZMapForge.Core/Layers/LayerMergeContribution.cs
  src/PZMapForge.Core/Layers/LayerMergeConflict.cs
  src/PZMapForge.Core/Layers/LayerMergeResult.cs
  src/PZMapForge.Core/Layers/LayerMerger.cs
  tests/PZMapForge.Core.Tests/Layers/LayerMergerTests.cs

LayerMerger.Merge(manifestPath, palettePath, options):
  validates manifest, loads palette, resolves image paths relative to
  manifest directory, parses each layer image via ImageMapForgeParser,
  validates allowed_kinds, merges into one SemanticGrid using precedence,
  reports per-layer contributions, total conflict count, and up to 100
  sampled conflicts.

Tests: 12 new (all grass, two-layer precedence, 4-layer non-overlapping,
  missing image, disallowed kind, conflict count, conflict sample capped,
  resize true/false, determinism, grid passable to RegionExtractor,
  claim boundary).

dotnet test: 176/176 (147 Core + 29 Cli). PS lane unchanged at 381.

## Slice 2A-3: COMPLETE

Files added/modified:
  src/PZMapForge.Core/Layers/LayerMergeArtifactWriter.cs
  src/PZMapForge.Cli/Program.cs  (layer-pipeline command added)
  tests/PZMapForge.Core.Tests/Layers/LayerMergeArtifactWriterTests.cs  (7 tests)
  tests/PZMapForge.Cli.Tests/LayerPipelineProcessTests.cs  (1 process test)

LayerMergeArtifactWriter.Write(outputDir, manifestPath, palettePath, palette,
  mergeResult, options): writes parsed-cell.json (loadable by ParsedCellLoader)
  and layer-merge-report.md (claim boundary, conflict summary, per-layer table,
  optional conflict sample table).

layer-pipeline CLI command:
  --layers <manifest> --palette <palette> [--output <dir>] [--resize]
  [--tiny-threshold <int>] [--large-threshold <int>]
  Writes 8 artifacts: parsed-cell.json, layer-merge-report.md,
  regions.json, regions-report.md, primitives.json, primitives-report.md,
  plan-recommendations.json, plan-report.md.

dotnet test: 184/184 (154 Core + 30 Cli). PS lane unchanged at 381.

## Next steps

Phase 2A is functionally complete (manifest validation, layer merge, pipeline CLI).

Recommended next work:
  - Proof packet v0.11 to record updated .NET test counts (176→184)
  - Add layer-pipeline to README.md command reference
  - Add process test for layer-pipeline with --resize flag
  - Add process test for layer-pipeline that refuses non-.local output

---

## Non-claims (unchanged from Phase 1)

- No playable Project Zomboid map export.
- No lotpack, lotheader, or bin file generation.
- No WorldEd replacement claim.
- No official PZ tool status.
- No copying or redistributing PZ game assets.
- No writes to media/maps.
- No Build 42 tested compatibility claim.
- No Steam Workshop readiness.
