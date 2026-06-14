# DeadMTL System 2 Static Road Sample Extract (MAP-22F)

## Purpose

MAP-22E proved that the System 2 extractor runs correctly on transparent placeholder layers
(layer_count=6, non_empty_pixels=0). MAP-22F proves that authored non-empty PNG masks
produce the expected run and node records in the extract JSON.

This is still extract-only. It is not runtime proof. It does not write .lotpack.
It does not write WorldGenOverride.lua.

## What is in the sample pack

Width: 220 px. Height: 170 px. Origin: 10580, 8200. Scale: 1 px = 1 m = 1 PZ tile.

| Layer                         | Intent                    | Color   | Region               |
|-------------------------------|---------------------------|---------|----------------------|
| static_roads_local.png        | local_street_asphalt      | #404040 | x=20..120  y=50..55  |
| static_roads_alleys.png       | alley_ruelle_asphalt      | #303030 | x=25..115  y=70..72  |
| static_roads_service.png      | service_lane              | #505050 | x=130..170 y=40..42  |
| static_roads_parking_access.png | parking_access          | #606060 | x=140..160 y=80..84  |
| static_pedestrian_cuts.png    | sidewalk_or_pedestrian_cut| #B0B0B0 | x=60..90   y=90      |
| static_road_nodes.png         | intersection_node         | #FF00FF | pixel (70,52)        |
| static_road_nodes.png         | road_turn_node            | #00FFFF | pixel (120,55)       |
| static_road_nodes.png         | dead_end_node             | #FF9900 | pixel (115,72)       |

## Expected extract output

The extractor reads each layer and emits:

- `static_roads_local`: runs with intent `local_street_asphalt`, non_empty_pixels > 0
- `static_roads_alleys`: runs with intent `alley_ruelle_asphalt`, non_empty_pixels > 0
- `static_roads_service`: runs with intent `service_lane`, non_empty_pixels > 0
- `static_roads_parking_access`: runs with intent `parking_access`, non_empty_pixels > 0
- `static_pedestrian_cuts`: runs with intent `sidewalk_or_pedestrian_cut`, non_empty_pixels > 0
- `static_road_nodes`: nodes (not runs) at (70,52), (120,55), (115,72)

Totals: layer_count=6, non_empty_pixels > 0, unknown_opaque_pixels=0.

## Expected run intent types

- `local_street_asphalt`
- `alley_ruelle_asphalt`
- `service_lane`
- `parking_access`
- `sidewalk_or_pedestrian_cut`

## Expected node intent types

- `intersection_node`
- `road_turn_node`
- `dead_end_node`

## Commands

Generate sample pack and extract:

```powershell
powershell -ExecutionPolicy Bypass -File examples\deadmtl-layer-pack\scripts\run-system2-static-road-sample-extract.ps1
```

Generate sample pack only:

```powershell
powershell -ExecutionPolicy Bypass -File examples\deadmtl-layer-pack\scripts\generate-system2-static-road-sample.ps1
```

## Output files

```
.local\deadmtl-authoring\system2-static-road-sample-extract\system2_static_road_sample_extract.json
.local\deadmtl-authoring\system2-static-road-sample-extract\system2_static_road_sample_extract.summary.txt
```

## Claim boundary

- Does NOT write `.lotpack`.
- Does NOT write `WorldGenOverride.lua`.
- Does NOT install into a live PZ server.
- Runtime proof is NOT claimed.
- Public playable claim is NOT made.

VERDICT: MAP22F_SYSTEM2_STATIC_ROAD_SAMPLE_EXTRACT_COMPLETE
