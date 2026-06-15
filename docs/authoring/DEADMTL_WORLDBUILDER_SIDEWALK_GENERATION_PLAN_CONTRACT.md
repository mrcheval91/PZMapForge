# DEADMTL WorldBuilder Sidewalk Generation Plan Contract (MAP-25D)

## Status

CONTRACT ONLY. No sidewalk generation. No terrain mutation. No PNG changes.
No lotpack writing. No worldgen override file. Not runtime proven. Not writer-ready.
Not playable.

This document describes the sidewalk planning contract established by MAP-25D for the
first 256x256 DeadMTL raw tile (map_00).

## What this is

A future sidewalk generation plan derived from:
- Neighborhood profile (MAP-25A) — provides sidewalk dimensions and source policy
- Zone metadata (MAP-25B) — provides color roles and sidewalk eligibility
- Lot subdivision plan (MAP-25C) — provides context on what zones will be subdivided

For each `STREET_CORRIDOR` color role, the plan records whether sidewalks will be
generated later and from which policy source.

## What this is not

- Not a generated sidewalk.
- Not a painted pixel.
- Not a chunk layer.
- Not a lotpack.
- No worldgen override file.
- Not runtime proven.
- Not writer-ready.

## Inputs

| Input | File |
|-------|------|
| Neighborhood profile | `examples/deadmtl-layer-pack/worldbuilder/neighborhoods/deadmtl_baseline_neighborhood_profile.json` |
| Zone metadata | `examples/deadmtl-layer-pack/worldbuilder/tiles/map_00.zone_metadata.json` |
| Lot subdivision plan | `.local/deadmtl-authoring/worldbuilder-lot-subdivision-plan/map_00/map_00.lot_subdivision_plan.json` |

## Outputs

All outputs are local only (must contain `.local` in path). Not committed.

| Output | Format |
|--------|--------|
| `map_00.sidewalk_generation_plan.json` | Full plan JSON |
| `map_00.sidewalk_generation_plan.md` | Markdown narrative |
| `map_00.sidewalk_generation_plan.csv` | Per-item CSV |
| `map_00.sidewalk_generation_plan.summary.txt` | Terminal-friendly summary |

## Plan item fields

Only `STREET_CORRIDOR` color roles produce plan items. Zone and non-street colors
are excluded.

| Field | Description |
|-------|-------------|
| `color` | Hex color from map_00.png |
| `role` | Always STREET_CORRIDOR for plan items |
| `street_class` | MAIN_ROAD or BACK_ALLEY |
| `sidewalk_action` | PLAN_GENERATE_SIDEWALKS_LATER or PLAN_NO_SIDEWALKS |
| `left_width_tiles` | Tiles to reserve left of corridor for sidewalk |
| `right_width_tiles` | Tiles to reserve right of corridor for sidewalk |
| `sidewalk_source` | INSIDE_STREET_ZONE, OUTSIDE_STREET_ZONE, or NONE |
| `source_policy` | NEIGHBORHOOD_PROFILE, BACK_ALLEY_RULE, or NOT_SIDEWALK_ELIGIBLE |
| `claim_status` | Always NOT_EXECUTED at this stage |

## Sidewalk eligibility

Only `MAIN_ROAD` corridors with `sidewalk_eligible = true` in zone metadata are
eligible for sidewalk generation. Sidewalk dimensions come from the active
neighborhood profile.

## Back alley rule

`BACK_ALLEY` corridors (`#F000FF`) never receive sidewalks.
`source_policy = BACK_ALLEY_RULE`. `sidewalk_source = NONE`. Width is always 0.
Back alleys are rear/service access only. Not primary frontage.

## Sidewalk source: INSIDE_STREET_ZONE

The active profile (`deadmtl_baseline`) uses `sidewalk_source = INSIDE_STREET_ZONE`.
This means sidewalks are carved from inside the street corridor width, not stolen
from adjacent residential or commercial lots.

If a future profile uses `OUTSIDE_STREET_ZONE`, sidewalks may consume lot boundary
space. MAP-25D plans the policy only; it does not execute.

## Expected totals for map_00

| Field | Value |
|-------|-------|
| plan_item_count | 2 |
| main_road_sidewalk_later_count | 1 |
| back_alley_no_sidewalk_count | 1 |
| left_width_tiles_total | 2 |
| right_width_tiles_total | 2 |
| sidewalk_source | INSIDE_STREET_ZONE |
| generation_status | NOT_EXECUTED |

## Expected plan rows for map_00

`#FF6600` MAIN_ROAD:
- sidewalk_action: PLAN_GENERATE_SIDEWALKS_LATER
- left_width_tiles: 2
- right_width_tiles: 2
- sidewalk_source: INSIDE_STREET_ZONE
- source_policy: NEIGHBORHOOD_PROFILE

`#F000FF` BACK_ALLEY:
- sidewalk_action: PLAN_NO_SIDEWALKS
- left_width_tiles: 0
- right_width_tiles: 0
- sidewalk_source: NONE
- source_policy: BACK_ALLEY_RULE

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

## Running

```powershell
powershell -ExecutionPolicy Bypass -File `
  "examples\deadmtl-layer-pack\scripts\run-deadmtl-worldbuilder-sidewalk-generation-plan.ps1"
```

Requires MAP-25C lot subdivision plan to be present first.

## Verdict

MAP25D_WORLDBUILDER_SIDEWALK_GENERATION_PLAN_CONTRACT_COMPLETE
