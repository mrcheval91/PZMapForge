# MAP-28B WorldBuilder Residential Building Footprint Plan

Deterministic Core/CLI authoring artifact for map_00_component_0001 residential building footprints.
Promotes MAP-28A parcel topology into building footprint planning artifacts.

## Claim boundary

This is a sandbox-only planning artifact.

- `sandbox_only = true`
- `building_footprint_planning_only = true`
- `writer_ready = false`
- `runtime_valid = false`
- `materialized = false`
- `runtime_proof_claimed = false`
- `public_playable_packaging_claimed = false`

No .lotpack, .lotheader, .lua, .bin, or WorldGenOverride.lua files are written.
Source map_00.png is not mutated.

## Source topology

- Map ID            : map_00
- Component ID      : map_00_component_0001
- Source parcel count : 16 (from MAP-28A)
- Source valid        : true when MAP-28A checks all pass

## Footprint geometry

### North-facing lots (6 footprints)

Parent parcel: 13 wide x 27 deep (Y 12-38)

| Field             | Value |
|-------------------|-------|
| x1                | parcel.x1 + 1 |
| x2                | parcel.x2 - 1 |
| y1                | parcel.y1 + 3 |
| y2                | y1 + 15       |
| width             | 11 tiles      |
| depth             | 16 tiles      |
| setback_front     | 3 (from north street edge) |
| setback_rear      | 8 (to rear boundary) |
| setback_side      | 1 each side |
| building_kind     | ROWHOUSE_MAIN_VOLUME |

### South-facing lots (6 footprints)

Parent parcel: 13 wide x 27 deep (Y 41-67)

| Field             | Value |
|-------------------|-------|
| x1                | parcel.x1 + 1 |
| x2                | parcel.x2 - 1 |
| y2                | parcel.y2 - 3 |
| y1                | y2 - 15       |
| width             | 11 tiles      |
| depth             | 16 tiles      |
| setback_front     | 3 (from south street edge) |
| setback_rear      | 8 (to rear boundary) |
| setback_side      | 1 each side |
| building_kind     | ROWHOUSE_MAIN_VOLUME |

### East-facing lots (4 footprints)

Parent parcel: 9 wide x 15 tall (X 202-210)

| Field             | Value |
|-------------------|-------|
| x1                | parcel.x1 + 1 = 203 |
| x2                | parcel.x2 - 2 = 208 |
| y1                | parcel.y1 + 1 |
| y2                | parcel.y2 - 1 |
| width             | 6 tiles       |
| height            | 13 tiles      |
| setback_front     | 2 (from east edge) |
| setback_rear      | 1 (from west edge) |
| setback_side      | 1 each (top/bottom) |
| building_kind     | EAST_EDGE_RESIDENTIAL_VOLUME |

## Topology rules

- One footprint per parcel (16 total)
- No footprint overlaps a sidewalk strip
- No footprint overlaps REAR_BOUNDARY (Y 39-40)
- No footprint exceeds its parent parcel bounds
- No inter-footprint overlaps
- No invented alley access (invented_alleys_enabled=false)
- No footprint is runtime or materialized

## Checks (34 total)

Checks 1-22 and 30-34: resolved during Build().
Checks 23-29: resolved in FinalizeAfterOutputs() after output files are written.
Expected: 34 PASS / 0 FAIL when all outputs are valid.

## Output files (10)

| File | Description |
|------|-------------|
| `map_00.residential_building_footprint_plan.json` | Full result JSON |
| `map_00.residential_building_footprint_plan_footprints.csv` | Footprint records (16 rows) |
| `map_00.residential_building_footprint_plan_checks.csv` | Check results (34 rows) |
| `map_00.residential_building_footprint_plan.summary.txt` | Human-readable summary |
| `README_MAP28B_RESIDENTIAL_BUILDING_FOOTPRINT_PLAN.md` | ASCII-only planning README |
| `map_00_residential_building_footprints_clean_native_256.png` | 256x256 clean footprint view |
| `map_00_residential_building_footprints_debug_native_256.png` | 256x256 debug view (outlines + ticks) |
| `map_00_residential_building_footprints_overlay_native_256.png` | 256x256 overlay on raw source |
| `map_00_residential_building_footprints_viewer.html` | ASCII-only CSS zoom viewer |
| `map_00.residential_building_footprint_plan_parent_manifest.json` | Parcel -> footprint mapping |

Output folder: `.local\deadmtl-authoring\worldbuilder-residential-building-footprint-plan\map_00\`

## CLI command

```
deadmtl-build-worldbuilder-residential-building-footprint-plan
  --output-root <.local dir>
  --output-json <path>
  --output-footprints-csv <path>
  --output-checks-csv <path>
  --summary <path>
  --output-readme <path>
  --output-clean-png <path>
  --output-debug-png <path>
  --output-overlay-png <path>
  --output-html <path>
  --output-parent-manifest <path>
  [--raw-source-png <path>]
```

All output paths must contain `.local` (sandbox guard).
`--raw-source-png` is optional; overlay uses dark background if not provided or not found.

## Helper script

```
examples\deadmtl-layer-pack\scripts\run-deadmtl-worldbuilder-residential-building-footprint-plan.ps1
```

## Core classes

| File | Role |
|------|------|
| `DeadMtlWorldBuilderResidentialBuildingFootprintPlan.cs` | Model types |
| `DeadMtlWorldBuilderResidentialBuildingFootprintPlanBuilder.cs` | Builder: Build(), FinalizeAfterOutputs(), Render*() |

## Tests

| Project | Filter | Tests |
|---------|--------|-------|
| Core | `ResidentialBuildingFootprintPlan` | Footprint counts, geometry, overlaps, claim boundary, PNGs, renders |
| CLI | `ResidentialBuildingFootprintPlan` | Exit codes, 10 output files, JSON fields, PNGs, ASCII, helper script |
