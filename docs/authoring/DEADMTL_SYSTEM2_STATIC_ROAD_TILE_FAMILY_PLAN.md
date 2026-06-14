# DeadMTL System 2 Static Road Tile Family Plan (MAP-22H)

## Purpose

MAP-22G produced a placement plan: one record per intended world tile with abstract
roles (road_surface, pedestrian_cut, road_node).

MAP-22H introduces a candidate tile-family mapping layer. For each placement record,
it assigns a candidate family name based on the intent. This is metadata only.
No PZ tile IDs are selected. No binary writes are performed. No runtime proof is claimed.

## What this task does

For each placement record in the placement plan JSON:
- Map intent to a candidate tile family (see table below)
- Assign confidence: LOW_METADATA_ONLY
- Preserve world_x, world_y, pixel_x, pixel_y, layer_id, class, intent, role, color

Emit one tile-family record per placement record.
record_count must equal placement_count from the source plan.

## Intent to candidate family mapping

| Intent                     | Candidate Family                 |
|----------------------------|----------------------------------|
| local_street_asphalt       | asphalt_road_surface_candidate   |
| alley_ruelle_asphalt       | asphalt_alley_surface_candidate  |
| service_lane               | asphalt_service_lane_candidate   |
| parking_access             | asphalt_parking_access_candidate |
| sidewalk_or_pedestrian_cut | concrete_or_sidewalk_candidate   |
| intersection_node          | road_node_metadata_candidate     |
| road_turn_node             | road_node_metadata_candidate     |
| dead_end_node              | road_node_metadata_candidate     |

These are candidate family names only. They are not PZ tile IDs.
A future task can resolve candidate families to specific tile IDs.

## Confidence

All records carry confidence: LOW_METADATA_ONLY.

This reflects that the assignment is based purely on authoring intent. No runtime
tile survey, no TileZed inspection, and no PZ load test has confirmed which tile IDs
correspond to these families.

## Plan JSON shape

```json
{
  "format": "pzmapforge.deadmtl.system2.static-road-tile-family-plan.v1",
  "status": "PLAN_ONLY",
  "runtime_status": "NOT_RUNTIME_PROVEN",
  "writer_status": "NOT_IMPLEMENTED",
  "source_placement_plan": "<path>",
  "records": [
    {
      "world_x": 10600,
      "world_y": 8250,
      "pixel_x": 20,
      "pixel_y": 50,
      "layer_id": "static_roads_local",
      "class": "local_street",
      "intent": "local_street_asphalt",
      "role": "road_surface",
      "candidate_family": "asphalt_road_surface_candidate",
      "confidence": "LOW_METADATA_ONLY",
      "color": "#404040"
    }
  ],
  "totals": {
    "record_count": 1141,
    "by_candidate_family": {
      "asphalt_road_surface_candidate": 606,
      "asphalt_alley_surface_candidate": 273,
      "asphalt_service_lane_candidate": 123,
      "asphalt_parking_access_candidate": 105,
      "concrete_or_sidewalk_candidate": 31,
      "road_node_metadata_candidate": 3
    },
    "by_role": { "road_surface": 1107, "pedestrian_cut": 31, "road_node": 3 },
    "by_intent": { "local_street_asphalt": 606, "..." : 0 },
    "unmapped_count": 0
  },
  "claim_boundary": {
    "writes_lotpack": false,
    "writes_worldgen_lua": false,
    "runtime_proven": false,
    "public_playable_claim": false
  }
}
```

## Commands

Generate tile-family plan from MAP-22G placement plan:

```powershell
powershell -ExecutionPolicy Bypass -File examples\deadmtl-layer-pack\scripts\run-system2-static-road-tile-family-plan.ps1
```

CLI command directly:

```powershell
dotnet run --project src\PZMapForge.Cli --configuration Release -- `
  system2-build-static-road-tile-family-plan `
  --input   .local\deadmtl-authoring\system2-static-road-placement-plan\system2_static_road_placement_plan.json `
  --output  .local\deadmtl-authoring\system2-static-road-tile-family-plan\system2_static_road_tile_family_plan.json `
  --summary .local\deadmtl-authoring\system2-static-road-tile-family-plan\system2_static_road_tile_family_plan.summary.txt
```

## Output files

```
.local\deadmtl-authoring\system2-static-road-tile-family-plan\system2_static_road_tile_family_plan.json
.local\deadmtl-authoring\system2-static-road-tile-family-plan\system2_static_road_tile_family_plan.summary.txt
```

## What comes next

A future task can resolve candidate families to specific PZ tile IDs by running a
TileZed inspection or tile survey. That task is out of scope for MAP-22H.

## Claim boundary

- Does NOT write lotpack files.
- Does NOT write WorldGenOverride.lua.
- Does NOT install into a live PZ server.
- Runtime proof is NOT claimed.
- Public playable claim is NOT made.
- Candidate family assignments carry confidence LOW_METADATA_ONLY only.

VERDICT: MAP22H_SYSTEM2_STATIC_ROAD_TILE_FAMILY_PLAN_COMPLETE
