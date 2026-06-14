# DeadMTL System 2 Static Road Placement Plan (MAP-22G)

## Purpose

MAP-22E/F proved that the System 2 extractor reads authored PNG masks and produces
structured JSON containing horizontal pixel runs (for road layers) and node records
(for the road node layer).

MAP-22G converts that extract JSON into a placement plan: one record per intended
world tile, with abstract roles rather than PZ tile IDs.

This placement plan is the final safe intermediate before any future System 2 writer
research. It contains all the spatial information needed for a future tile writer,
but does not perform any binary writes, does not write lotpack files, does not write
WorldGenOverride.lua, and does not claim runtime proof.

## What this task does

For each run in the extract JSON:
- Expand x_start..x_end into one placement record per pixel/tile
- Set world_x = origin_x + pixel_x, world_y = origin_y + pixel_y
- Map intent to abstract role (see table below)
- Preserve layer_id, class, intent, color

For each node in the extract JSON:
- Emit one placement record per node pixel
- Role = road_node
- Preserve intent and color

## Intent to role mapping

| Intent                    | Role           |
|---------------------------|----------------|
| local_street_asphalt      | road_surface   |
| alley_ruelle_asphalt      | road_surface   |
| service_lane              | road_surface   |
| parking_access            | road_surface   |
| sidewalk_or_pedestrian_cut| pedestrian_cut |
| intersection_node         | road_node      |
| road_turn_node            | road_node      |
| dead_end_node             | road_node      |

Roles are abstract. They are not PZ tile IDs. A future task can map abstract roles
to tile candidates, but that is not this task.

## Duplicate positions

If two layers have pixels at the same world (x, y), both placement records are emitted.
The totals field `duplicate_position_count` records how many positions were seen more
than once. The plan does not fail on duplicates in v1.

## Plan JSON shape

```json
{
  "format": "pzmapforge.deadmtl.system2.static-road-placement-plan.v1",
  "status": "PLAN_ONLY",
  "runtime_status": "NOT_RUNTIME_PROVEN",
  "writer_status": "NOT_IMPLEMENTED",
  "source_extract": "<path>",
  "origin_x": 10580,
  "origin_y": 8200,
  "width": 220,
  "height": 170,
  "placements": [
    {
      "world_x": 10600,
      "world_y": 8250,
      "pixel_x": 20,
      "pixel_y": 50,
      "layer_id": "static_roads_local",
      "class": "local_street",
      "intent": "local_street_asphalt",
      "role": "road_surface",
      "color": "#404040"
    }
  ],
  "totals": {
    "placement_count": 1141,
    "run_source_count": 18,
    "node_source_count": 3,
    "duplicate_position_count": 3,
    "by_role": { "road_surface": 1107, "pedestrian_cut": 31, "road_node": 3 },
    "by_intent": { "local_street_asphalt": 606, ... }
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

Generate placement plan from MAP-22F sample extract:

```powershell
powershell -ExecutionPolicy Bypass -File examples\deadmtl-layer-pack\scripts\run-system2-static-road-placement-plan.ps1
```

CLI command directly:

```powershell
dotnet run --project src\PZMapForge.Cli --configuration Release -- `
  system2-build-static-road-placement-plan `
  --input   .local\deadmtl-authoring\system2-static-road-sample-extract\system2_static_road_sample_extract.json `
  --output  .local\deadmtl-authoring\system2-static-road-placement-plan\system2_static_road_placement_plan.json `
  --summary .local\deadmtl-authoring\system2-static-road-placement-plan\system2_static_road_placement_plan.summary.txt
```

## Output files

```
.local\deadmtl-authoring\system2-static-road-placement-plan\system2_static_road_placement_plan.json
.local\deadmtl-authoring\system2-static-road-placement-plan\system2_static_road_placement_plan.summary.txt
```

## What comes next

A future task (MAP-22H or later) can map abstract roles (`road_surface`, `pedestrian_cut`,
`road_node`) to PZ tile ID candidates. That task is out of scope for MAP-22G.

## Claim boundary

- Does NOT write lotpack files.
- Does NOT write WorldGenOverride.lua.
- Does NOT install into a live PZ server.
- Runtime proof is NOT claimed.
- Public playable claim is NOT made.

VERDICT: MAP22G_SYSTEM2_STATIC_ROAD_PLACEMENT_PLAN_COMPLETE
