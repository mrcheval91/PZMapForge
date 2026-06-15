# DEADMTL WorldBuilder Building Selection Policy Plan Contract (MAP-25E)

## Status

CONTRACT ONLY. No building generation. No building placement. No lot subdivision.
No sidewalk generation. No fence placement. No lotpack writing. No worldgen override file.
Not runtime proven. Not writer-ready. Not playable.

This document describes the building selection policy contract established by MAP-25E
for the first 256x256 DeadMTL raw tile (map_00).

## What this is

A future building selection policy derived from:
- Neighborhood profile (MAP-25A) — building family lists and procedural fill policy
- Zone metadata (MAP-25B) — color roles and zone semantics
- Lot subdivision plan (MAP-25C) — subdivision context per zone
- Sidewalk generation plan (MAP-25D) — frontage and sidewalk context

For each color role, the plan records which building families are eligible, how they
will be selected, what placement mode applies, and what constraints govern fit, frontage,
and facade orientation.

## What this is not

- Not a placed building.
- Not a selected building id.
- Not a building file path.
- Not a lot layout.
- Not a chunk layer.
- Not a lotpack.
- No worldgen override file.
- Not runtime proven.

## Inputs

| Input | File |
|-------|------|
| Neighborhood profile | `examples/deadmtl-layer-pack/worldbuilder/neighborhoods/deadmtl_baseline_neighborhood_profile.json` |
| Zone metadata | `examples/deadmtl-layer-pack/worldbuilder/tiles/map_00.zone_metadata.json` |
| Lot subdivision plan | `.local/deadmtl-authoring/worldbuilder-lot-subdivision-plan/map_00/map_00.lot_subdivision_plan.json` |
| Sidewalk generation plan | `.local/deadmtl-authoring/worldbuilder-sidewalk-generation-plan/map_00/map_00.sidewalk_generation_plan.json` |

## Outputs

All outputs are local only (must contain `.local` in path). Not committed.

| Output | Format |
|--------|--------|
| `map_00.building_selection_policy_plan.json` | Full plan JSON |
| `map_00.building_selection_policy_plan.md` | Markdown narrative |
| `map_00.building_selection_policy_plan.csv` | Per-item CSV |
| `map_00.building_selection_policy_plan.summary.txt` | Terminal-friendly summary |

## Plan item fields

| Field | Description |
|-------|-------------|
| `color` | Hex color from map_00.png |
| `role` | ZONE, STREET_CORRIDOR, UNIQUE_PLACEHOLDER, or IGNORE |
| `zone_type` | RESIDENTIAL, COMMERCIAL, GREENSPACE (if role=ZONE) |
| `street_class` | MAIN_ROAD, BACK_ALLEY (if role=STREET_CORRIDOR) |
| `selection_action` | What will happen to building selection for this color |
| `placement_mode` | How a future placer will handle this zone |
| `frontage_requirement` | Frontage street preference |
| `alley_rule` | Back alley use rule |
| `facade_rule` | Facade orientation rule |
| `sidewalk_relation` | Relationship to sidewalk context |
| `allowed_building_families` | Candidate families for future selection |
| `fit_policy` | How buildings will be fit to lots |
| `source_confidence` | Always SEMANTIC_METADATA_ONLY at this stage |

## Selection actions by color

| Color | Zone/Street | Selection Action |
|-------|------------|-----------------|
| `#7200FF` | RESIDENTIAL | PLAN_SELECT_RESIDENTIAL_BUILDING_FAMILY_LATER |
| `#42CCFF` | COMMERCIAL | PLAN_SELECT_COMMERCIAL_BUILDING_FAMILY_LATER |
| `#B2BD87` | UNIQUE_PLACEHOLDER | PLAN_REQUIRE_UNIQUE_BUILDING_ID_LATER |
| `#00AA10` | GREENSPACE | PLAN_NO_BUILDING_SELECTION_GREENSPACE |
| `#FF6600` | MAIN_ROAD | PLAN_NO_BUILDING_SELECTION_STREET |
| `#F000FF` | BACK_ALLEY | PLAN_NO_BUILDING_SELECTION_STREET |
| `#000000` | IGNORE | PLAN_IGNORE |

## Residential policy (#7200FF)

Allowed families: duplex, triplex, plex_block, apartment_lowrise

Fit policy:
- Fit to lot width/depth after subdivision.
- Reject building if footprint exceeds lot.
- Prefer facade width compatible with row.
- Prefer Montreal plex-like frontage.

Frontage: PREFER_MAIN_ROAD_FRONTAGE
Alley: BACK_ALLEY_REAR_SERVICE_ONLY
Facade: ROW_UNIFORM_FRONTAGE
Sidewalk relation: FRONTAGE_SIDEWALK_IF_MAIN_ROAD

## Commercial policy (#42CCFF)

Allowed families: depanneur, pharmacy, restaurant, main_street_storefront, office_small

Fit policy:
- Fit to lot width/depth after subdivision.
- Prefer storefront on main road.
- Allow service/rear access from alley.

Frontage: PREFER_MAIN_ROAD_FRONTAGE
Alley: BACK_ALLEY_SERVICE_ACCESS
Facade: COMMERCIAL_FRONTAGE
Sidewalk relation: FRONTAGE_SIDEWALK_IF_MAIN_ROAD

## Civic unique placeholder policy (#B2BD87)

Allowed families: government, library, community_center, institutional, special_building

Fit policy:
- Future metadata must bind placeholder to a specific building id.
- Reject random procedural building.
- Do not overwrite park/greenspace randomly.

Frontage: MAIN_ROAD_IF_AVAILABLE
Facade: UNIQUE_BUILDING_DEFINED_BY_METADATA
Sidewalk relation: DEPEND_ON_BOUND_UNIQUE_BUILDING

## Greenspace exclusion (#00AA10)

No procedural building placement. May contain UNIQUE_PLACEHOLDER markers.
Do not fill over civic placeholders.

## Street exclusion

MAIN_ROAD (#FF6600): provides frontage and sidewalk context only. No building placement.
BACK_ALLEY (#F000FF): rear/service access only. Not primary frontage. No sidewalks.

## No concrete building ids now

concrete_building_ids_selected_now_count: 0

No building file, no building id, no building geometry is selected, written, or implied.
This plan records policy only. Building selection executes in a future step after
lot subdivision and sidewalk generation are executed.

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
| selects_concrete_building_ids_now | false |

## Running

```powershell
powershell -ExecutionPolicy Bypass -File `
  "examples\deadmtl-layer-pack\scripts\run-deadmtl-worldbuilder-building-selection-policy-plan.ps1"
```

Requires MAP-25C lot subdivision plan and MAP-25D sidewalk generation plan to be present first.

## Verdict

MAP25E_WORLDBUILDER_BUILDING_SELECTION_POLICY_PLAN_CONTRACT_COMPLETE
