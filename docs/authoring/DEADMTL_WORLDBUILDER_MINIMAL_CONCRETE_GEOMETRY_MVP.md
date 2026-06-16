# DeadMTL WorldBuilder Minimal Concrete Geometry MVP

CONTRACT ONLY — No lotpack writes. No WorldGenOverride.lua. No compile-worldgen. No runtime claim.

## Purpose

MAP-26A produces the first concrete integer rectangle geometry for one DUAL_ACCESS_CANDIDATE
lot-block component (component_order=1, RESIDENTIAL_LOT_BLOCK) derived deterministically from
the source PNG pixel labels, the connected component extraction, and the access profile.

This is a geometry MVP, not a writer-ready output.

## What this task produces

- Component bounding box (integer pixel/tile coordinates)
- Lot rectangles: integer-clipped slices of the component bbox along the frontage axis
- Building slot rectangles: per-lot, with side insets and frontage/rear setbacks applied
- Slot acceptance gate: ACCEPTED if width ≥ 4px AND height ≥ 4px

## What this task does NOT produce

- No generation of PZ tile data
- No lotpack files
- No WorldGenOverride.lua
- Not writer-ready (writer_ready_geometry_count = 0)
- Not runtime-validated (runtime_validated_geometry_count = 0)
- Not materialized (materialized_geometry_count = 0)

## Coordinate system

- x = east (pixel column, 0-indexed from left)
- y = south (pixel row, 0-indexed from top)
- z = 0
- 1 pixel = 1 PZ world tile ≈ 1 meter

## Lot slicing algorithm

1. Run BFS pixel labeling (exact replica of MAP-25K/MAP-25M algorithm)
2. Isolate target component pixels by label
3. Compute integer bounding box (min_x, min_y, max_x, max_y)
4. Detect 4-directional contact with frontage component → classify NORTH/SOUTH/EAST/WEST
5. If NORTH or SOUTH: split bbox along X axis into vertical lots
6. If EAST or WEST: split bbox along Y axis into horizontal lots
7. lot_count = clamp(floor(frontage_span / 12), 1, 8); reduce if any lot < 8px
8. Integer distribution: base = span / count; first (span % count) lots get base+1

## Building slot insets

| Parameter       | Value |
|-----------------|-------|
| side_inset      | 2 px  |
| frontage_setback| 3 px  |
| rear_setback    | 3 px  |
| min slot width  | 4 px  |
| min slot height | 4 px  |

## Claim boundary

| Field                         | Value |
|-------------------------------|-------|
| writes_lotpack                | false |
| writes_worldgen_lua           | false |
| runtime_proven                | false |
| public_playable_claim         | false |
| writer_ready_claim            | false |
| writer_ready_geometry_count   | 0     |
| runtime_validated_geometry_count | 0  |
| materialized_geometry_count   | 0     |

## Verdict

`MAP26A_WORLDBUILDER_MINIMAL_CONCRETE_GEOMETRY_MVP_COMPLETE`
