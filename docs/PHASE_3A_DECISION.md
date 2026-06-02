# Phase 3A Decision Record

Date: 2026-06-02
Baseline commit: 14065b7
Author: operator

---

## Claim boundary

planning_artifact_only_not_pz_load_tested

All Phase 3A work produces local planning artifacts only.
No Phase 3A output is a playable Project Zomboid export.

---

## Survey summary

A read-only media layout survey was run against a local Project Zomboid
installation using scripts/Run-Phase3ALocalPzSurvey.ps1 and the additional
media layout commands from docs/PHASE_3A_LOCAL_INSTALL_SURVEY.md.

No local paths, file names, or asset contents are committed below.
The following redacted counts are the only evidence committed.

### Install layout

| Field | Redacted value |
|---|---|
| Install found | yes |
| media/ present | yes |
| media/ subdirectory count | 33 |
| Build version auto-detected | no |

### Extension counts present in media/ (approximate only)

| Extension | Approximate count | Interpretation |
|---|---|---|
| .png | ~4400 | Image assets (various; not all tile-specific) |
| .bin | ~2900 | Binary game data |
| .lotpack | ~2900 | Map lot pack files (map data, not tilesheets) |
| .lotheader | ~2900 | Map lot header files (map data) |
| .xml | ~2700 | Configuration and definition files |
| .pack | ~20 | Tile pack files (likely tilesheet containers) |
| .tiles | ~7 | Tile definition files (newer format) |

Note: counts are approximate buckets rounded to nearest 50-100.
Exact counts and file names are not committed.

### Keyword hits in file names under media/ (approximate)

| Keyword | File hits | Directory hits | Interpretation |
|---|---|---|---|
| tile | ~40 | 0 | Tile-related files present; no separate tiles/ dir |
| tiles | ~10 | 0 | Tile-related files present; no separate tiles/ dir |
| pack | ~3000 | 2 | pack keyword is pervasive (includes lot packs) |
| world | ~3000 | 6 | World/map layout files |
| texture | ~10 | 8 | Texture-related directories and files |
| floors | ~10 | 0 | Floor tile references likely present |
| definition | ~25 | 1 | Definition files present |

### Key finding

The expected media/tiles/ directory does not exist. Instead:
- .pack files (~20) in media/ are the likely tilesheet containers.
- .tiles files (~7) are a separate tile definition format.
- These are the primary targets for Phase 3A tile reference work.

The exact location of .pack and .tiles files within media/ is locally known
to the operator but is not committed here.

---

## Decision

Phase 3A-1 may begin.

Rationale:
1. The local Project Zomboid installation exists and is accessible.
2. The media/ directory is present with a known layout (33 subdirectories).
3. Tile-related asset formats are confirmed present:
   .pack (~20 files) and .tiles (~7 files) are the likely tilesheet containers.
4. Enough structural evidence exists to design a local config loader
   with 8 mandatory safety checks (no real PZ install required for unit tests).
5. The install path and tiles_root can be captured in a local-only config file
   that is gitignored and validated at runtime.

The decision to begin 3A-1 is based on structural evidence only.
It does not constitute a claim about PZ tile compatibility, playability,
or Build 41/42 support.

---

## Scope of Phase 3A-1

Slice 3A-1 is strictly limited to:
- A JSON schema for the local PZ install config file
- A typed loader that validates the config document
- Unit tests that use JSON fixture files only (no real PZ install required)

Slice 3A-1 does NOT include:
- Any tile catalog generation
- Any file system access to the PZ install
- Any tile reference mapping
- Any CLI command that reads the real PZ install
- Any TMX or artifact output changes

---

## Still blocked after Phase 3A-1

The following remain blocked and must not be attempted until explicitly
unblocked by a separate decision record:

1. Tile catalog generation (Slice 3A-3)
   Requires: knowing which .pack/.tiles files contain semantic kind tiles,
   and understanding the internal structure of those formats.

2. Semantic kind to tile reference mapping (Slice 3A-4)
   Requires: visual tile inspection or a documented tile naming convention.
   The survey confirmed file presence but not which tiles map to grass,
   road, sidewalk, row_house, etc.

3. TileZed/PZ-compatible export experiment (Slice 3A-5)
   Requires: tile catalog, kind mapping, and local load test plan.

4. Playable export claim
   Requires: a real local load test with documented evidence.
   This has not occurred. The claim boundary is unchanged.

---

## Required safety rules for all Phase 3A work

All Phase 3A code must enforce the following, verified by unit tests:

1. The local config file is stored at .local/pzmapforge/pz-install-config.json.
2. The local config file is never committed (.local/ is gitignored).
3. allow_asset_copy must be false. A loader violation must fail validation.
4. allow_media_maps_write must be false. A loader violation must fail validation.
5. tile_reference_mode must be "local_reference_only".
6. pz_install_root must be non-empty in the config document.
7. tiles_root must be non-empty in the config document.
8. No PZ asset content, metadata, file names, or GIDs are committed to the repo.

---

## Next implementation slice: Slice 3A-1

Files to add:

    schemas/pzmapforge.local-pz-install-config.v0.1.schema.json

    src/PZMapForge.Core/LocalPz/LocalPzInstallConfig.cs
    src/PZMapForge.Core/LocalPz/LocalPzInstallConfigLoadResult.cs
    src/PZMapForge.Core/LocalPz/LocalPzInstallConfigLoader.cs

    tests/PZMapForge.Core.Tests/LocalPz/LocalPzInstallConfigLoaderTests.cs
    tests/fixtures/local-pz/valid-local-pz-install-config.json

Loader validation rules:
- schema == "pzmapforge.local-pz-install-config.v0.1"
- claim_boundary == "planning_artifact_only_not_pz_load_tested"
- allow_asset_copy == false (must reject true)
- allow_media_maps_write == false (must reject true)
- tile_reference_mode == "local_reference_only" (must reject other values)
- pz_install_root non-empty
- tiles_root non-empty

Tests required:
- valid config loads successfully
- missing file fails
- wrong schema fails
- wrong claim_boundary fails
- allow_asset_copy true fails
- allow_media_maps_write true fails
- wrong tile_reference_mode fails
- empty pz_install_root fails
- empty tiles_root fails
- loader does NOT require real PZ install to exist (path is validated later)

Commit: Add local PZ install config schema and loader
