# DEADMTL WorldBuilder Lot Subdivision Plan Contract (MAP-25C)

## Status

CONTRACT ONLY. No terrain generation. No lot subdivision executed. No building placement.
No sidewalk generation. No fence placement. No lotpack writing. Not runtime proven.
Not writer-ready. Not playable.

This document describes the planning contract established by MAP-25C for the first
256x256 DeadMTL raw tile (map_00).

## What this is

A future subdivision plan derived from semantic zone metadata (MAP-25B) and a
neighborhood profile (MAP-25A). For each color role in the zone metadata, the plan
records the intended subdivision action and a set of policies that will govern future
generation steps.

## What this is not

- Not a lot layout.
- Not a tile grid.
- Not a chunk layer.
- Not a lotpack.
- Not a WorldEd project.
- Not a runtime-proven output.
- No WorldGenOverride file is written.
- No worldgen override file is produced.

## Inputs

| Input | File |
|-------|------|
| Zone metadata | `examples/deadmtl-layer-pack/worldbuilder/tiles/map_00.zone_metadata.json` |
| Neighborhood profile | `examples/deadmtl-layer-pack/worldbuilder/neighborhoods/deadmtl_baseline_neighborhood_profile.json` |
| Tile inspection (optional) | `.local/deadmtl-authoring/raw-map-tile-inspection/map_00/map_00.raw_tile_inspection.json` |

## Outputs

All outputs are local only (must contain `.local` in path). Not committed.

| Output | Format |
|--------|--------|
| `map_00.lot_subdivision_plan.json` | Full plan JSON |
| `map_00.lot_subdivision_plan.md` | Markdown narrative |
| `map_00.lot_subdivision_plan.csv` | Per-item CSV |
| `map_00.lot_subdivision_plan.summary.txt` | Terminal-friendly summary |

## Plan item fields

Each color role in the zone metadata produces one plan item with the following fields:

| Field | Description |
|-------|-------------|
| `color` | Hex color from map_00.png |
| `role` | ZONE, STREET_CORRIDOR, UNIQUE_PLACEHOLDER, or IGNORE |
| `zone_type` | RESIDENTIAL, COMMERCIAL, GREENSPACE (if role=ZONE) |
| `street_class` | MAIN_ROAD, BACK_ALLEY (if role=STREET_CORRIDOR) |
| `subdivision_action` | What will happen to this color at subdivision time |
| `frontage_policy` | Primary street frontage rule |
| `rear_access_policy` | Rear/service access rule |
| `sidewalk_generation_policy` | Sidewalk eligibility |
| `lot_line_policy` | Lot line and fence eligibility |
| `building_selection_policy` | Building selection method |
| `facade_orientation_policy` | Facade orientation rule |
| `source_confidence` | Always SEMANTIC_METADATA_ONLY at this stage |

## Subdivision actions by color

| Color | Role | Subdivision Action |
|-------|------|--------------------|
| `#7200FF` | RESIDENTIAL zone | PLAN_SUBDIVIDE_RESIDENTIAL_LATER |
| `#42CCFF` | COMMERCIAL zone | PLAN_SUBDIVIDE_COMMERCIAL_LATER |
| `#FF6600` | MAIN_ROAD corridor | PLAN_NO_SUBDIVISION_STREET_CORRIDOR |
| `#F000FF` | BACK_ALLEY corridor | PLAN_NO_SUBDIVISION_STREET_CORRIDOR |
| `#00AA10` | GREENSPACE zone | PLAN_NO_SUBDIVISION_GREENSPACE |
| `#B2BD87` | UNIQUE_PLACEHOLDER | PLAN_NO_SUBDIVISION_UNIQUE_PLACEHOLDER |
| `#000000` | IGNORE | PLAN_IGNORE |

## Frontage policy

Residential and commercial zones use PREFER_MAIN_ROAD as primary frontage.
BACK_ALLEY corridors use NO_PRIMARY_FRONTAGE.
MAIN_ROAD corridors are NOT_APPLICABLE (they are the road, not the frontage).

## Back alley note

BACK_ALLEY (#F000FF) is rear/service access only. It must never be primary frontage.
Sidewalk generation policy for back alleys is NO_SIDEWALKS.

## Sidewalk generation

Sidewalks are not generated here. They are a future layer generated from MAIN_ROAD
corridors using the active neighborhood profile. Policy is
FUTURE_FROM_NEIGHBORHOOD_PROFILE_ON_MAIN_ROAD for eligible zones and main roads.

## Fence and cloture

Fences are not generated here. Lot line policy is FUTURE_LOT_LINES_AND_FENCES
for residential and commercial zones. Not placed in MAP-25C.

## Unique placeholder

#B2BD87 (UNIQUE_PLACEHOLDER / CIVIC_SPECIAL_BUILDING) has
building_selection_policy = FUTURE_UNIQUE_BUILDING_ID_REQUIRED.
No procedural fill. No subdivision. Future metadata must bind this to a specific
building identifier (library, government building, community center, etc.).

## Claim boundary

All claim boundary flags are false:

| Flag | Value |
|------|-------|
| writes_lotpack | false |
| writes_worldgen_lua | false |
| runtime_proven | false |
| public_playable_claim | false |
| writer_ready_claim | false |
| generates_buildings_now | false |
| generates_sidewalks_now | false |
| subdivides_lots_now | false |
| captures_chunk_layers_now | false |
| places_fences_now | false |
| places_unique_buildings_now | false |

## Color normalization guards

The builder refuses to accept:
- `#00AA00` in metadata when `#00AA10` is absent (wrong green)
- `#FF00FF` in metadata when `#F000FF` is absent (wrong magenta)

Do not auto-normalize near colors.

## Running

```powershell
powershell -ExecutionPolicy Bypass -File `
  "examples\deadmtl-layer-pack\scripts\run-deadmtl-worldbuilder-lot-subdivision-plan.ps1"
```

Or via CLI directly:

```
dotnet run --project src/PZMapForge.Cli -- `
  deadmtl-build-worldbuilder-lot-subdivision-plan `
  --metadata examples/deadmtl-layer-pack/worldbuilder/tiles/map_00.zone_metadata.json `
  --profile examples/deadmtl-layer-pack/worldbuilder/neighborhoods/deadmtl_baseline_neighborhood_profile.json `
  --output-json .local/deadmtl-authoring/worldbuilder-lot-subdivision-plan/map_00/map_00.lot_subdivision_plan.json `
  --output-md   .local/deadmtl-authoring/worldbuilder-lot-subdivision-plan/map_00/map_00.lot_subdivision_plan.md `
  --output-csv  .local/deadmtl-authoring/worldbuilder-lot-subdivision-plan/map_00/map_00.lot_subdivision_plan.csv `
  --summary     .local/deadmtl-authoring/worldbuilder-lot-subdivision-plan/map_00/map_00.lot_subdivision_plan.summary.txt
```

## Verdict

MAP25C_WORLDBUILDER_LOT_SUBDIVISION_PLAN_CONTRACT_COMPLETE
