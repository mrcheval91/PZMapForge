# Phase 2B / Phase 3 Decision Record

Date: 2026-06-01
Baseline commit: 7a7ff2c
Author: operator

---

## Background

Phase 2A is complete. The layer-pipeline accepts a manifest of per-layer images,
merges them deterministically, and produces 8 planning artifacts through the full
downstream pipeline. Tests: 184/184. Proof packet: v0.11.

Before adding more capability, this record compares two candidate directions:

  Option A: Phase 2B -- deepen layer authoring conventions
  Option B: Phase 3  -- begin local PZ install / tile ID mapping

---

## Current verified capability (Phase 2A)

Input:
  layer manifest (JSON) + per-layer PNG/BMP images + palette

Pipeline:
  manifest validation -> layer image parsing -> deterministic merge
  (precedence: markers > buildings > roads > terrain) -> parsed-cell.json
  -> full downstream pipeline (regions, primitives, planning)

Artifacts:
  parsed-cell.json
  layer-merge-report.md
  regions.json + regions-report.md
  primitives.json + primitives-report.md
  plan-recommendations.json + plan-report.md

Claim boundary: planning_artifact_only_not_pz_load_tested

---

## Option A: Phase 2B -- layer authoring conventions

### What it is

Strengthen the layer authoring model before any PZ tile mapping work.
Define conventions, add examples, make the pipeline easier and safer to use.

Candidate work for Phase 2B:

  docs/LAYER_AUTHORING_GUIDE.md
    Kind-by-layer recommendations (which kinds belong in which layer)
    Conflict resolution guide (what happens when layers overlap)
    Naming conventions for manifest, layers, output directories
    Error message glossary (what each merge failure means)
    Workflow walkthrough from blank PNG to planning artifacts

  tests/fixtures/layers/
    Layer fixture README
    example-2b/ -- a minimal 4-layer manifest example with documented structure
    (Binary PNG fixtures are not committed unless generated deterministically
     by test code or a fixture-generator script in a later slice)

  Optional later:
    layer-validate CLI command (check manifest + images without merging)
    layer diagnostics output (per-layer kind frequency, coverage heat map)

### Pros

  - Builds on existing validated Phase 2A surface without new risk
  - Independent from PZ internals, assets, or tile format
  - No copied PZ assets (forbidden)
  - No fake playable export claim
  - Helps users author better layer input before any export work begins
  - Strengthens semantic layer quality before Phase 3 depends on it
  - Low risk: docs and fixtures only in 2B-1

### Cons

  - Still not a real PZ export
  - Does not solve tile ID mapping
  - Adds authoring surface that could grow unbounded without scope control

---

## Option B: Phase 3 -- local PZ install / tile ID mapping

### What it is

Begin preparing a bridge toward TileZed/PZ-compatible export by mapping
semantic kinds to local PZ tile GIDs using a locally installed PZ copy.

Candidate work for Phase 3:

  Local install config (path to PZ installation, not committed)
  Tilesheet discovery (find .pack / .tiles under local install)
  Semantic kind to local tile reference map
    grass -> ground tile GID
    road -> road tile GID
    row_house -> residential structure tile GID
    etc.
  No copied tilesheets (forbidden)
  No committed PZ assets (forbidden)
  No official tooling claim

### Why it is deferred

  Higher risk than Phase 2B:
    PZ tile semantics change between Build 41 and 42
    Tile ID tables are fragile and install-layout dependent
    A tile map committed at one version becomes stale at the next

  Claim boundary would need to advance:
    Using real PZ tiles implies closer compatibility with PZ format
    That claim cannot be made without a real local load test
    A local load test requires a documented test harness and evidence record

  Premature before Phase 2B:
    Poorly defined semantic layers make tile mapping ambiguous
    Flat or inconsistent layer input produces low-quality tile assignments
    Phase 2B first makes Phase 3 cleaner and more verifiable

  Conditions to revisit Phase 3:
    Phase 2B authoring guide and fixtures are complete and stable
    A local PZ install config mechanism exists and is documented
    A local load test harness plan exists or is written
    A decision record documents the load test evidence requirements

---

## Recommendation

Choose Phase 2B first.

Rationale:
  1. Phase 2A made the pipeline technically possible.
  2. Phase 2B makes the authoring model usable, inspectable, and
     safer to build on.
  3. Tile mapping (Phase 3) should wait until layer conventions are
     stable and documented.
  4. Cleaner semantic layers will reduce risk and ambiguity in Phase 3.
  5. Phase 2B is docs-first and low-risk. No new product claims.
  6. The project remains planning-only and local-first.

---

## Non-claims (unchanged from Phase 1 and 2A)

The following are not claimed and must not be added without documented evidence:

  - Playable Project Zomboid map export
  - lotpack, lotheader, or bin file generation
  - WorldEd replacement or compatibility
  - Official Project Zomboid tool status
  - Build 42 tested compatibility
  - Steam Workshop readiness
  - Copying or redistributing Project Zomboid game assets
  - Writing to media/maps

---

## Slice 2B-1: COMPLETE

Files added:
  docs/LAYER_AUTHORING_GUIDE.md
  tests/fixtures/layers/README.md
  tests/fixtures/layers/example-2b/layer-manifest.json
  tests/fixtures/layers/example-2b/README.md

No binary images. No code changes. No validation count changes.

## Slice 2B-2: COMPLETE

Files added:
  tests/fixtures/layers/example-2b/new-example-images.ps1
  tests/fixtures/layers/example-2b/generated-layer-manifest.json
  .gitignore (generated/ pattern added)
  tests/fixtures/layers/example-2b/README.md (updated)

Generates 4 deterministic PNGs (terrain, roads, buildings, markers).
Pipeline verified: 4 layers, 36 expected conflicts (spawn inside row_house),
15 regions, Status OK. Generated/ is gitignored.

## Phase 3 precondition: STARTED

docs/PHASE_3_LOCAL_PZ_CONFIG_SPEC.md has been added.

The document defines:
  - What Phase 3 is and is not allowed to do
  - Proposed local config file path (.local/pzmapforge/pz-install-config.json)
  - Proposed config schema shape
  - 8 mandatory safety checks before any tile mapping
  - Evidence required before implementation begins
  - 5 proposed future slices (3A-1 through 3A-5)

Phase 3 implementation must not begin until the precondition doc is reviewed
and operator evidence (local install path, tilesheet layout, tile naming) is
documented in a Phase 3A decision record.

## Conditions to begin Phase 3A-1 implementation

1. This spec document is ratified (committed in main -- done).
2. A local PZ installation is accessible to the operator.
3. A tilesheet layout survey is documented.
4. A tile naming convention spec for the semantic kinds exists.
5. A Phase 3A decision record is written.

---

## First implementation slice (archived): Slice 2B-1

Slice 2B-1: Layer authoring guide and fixture pack

Files to add:

  docs/LAYER_AUTHORING_GUIDE.md
    Kind-by-layer reference table (9 kinds, 4 default layers, recommended mapping)
    Conflict resolution explanation and precedence table
    Common errors and what they mean
    Workflow: from blockout image to layer manifest
    Naming conventions
    Claim boundary reminder

  tests/fixtures/layers/README.md
    Purpose of the fixtures directory
    How to use the valid-layer-manifest.json fixture
    Note on binary images: not committed, generated by test code

  tests/fixtures/layers/example-2b/layer-manifest.json
    4-layer manifest example using all 9 kinds across 4 layers
    Includes comments-by-convention in README
    Follows the documented layer conventions

  tests/fixtures/layers/example-2b/README.md
    Describes what each layer represents
    Documents what the merged output should look like
    Notes that images are not committed and must be generated by test or script

No binary PNG fixtures in this slice.
No code changes in this slice.
No CLI command changes in this slice.

Commit: Add layer authoring guide and fixture examples
