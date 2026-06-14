# DeadMTL System 2 Static Road Tile Family Survey (MAP-22I)

## Purpose

MAP-22H mapped each placement record to a candidate tile family with confidence
LOW_METADATA_ONLY. The candidate families are:

- asphalt_road_surface_candidate
- asphalt_alley_surface_candidate
- asphalt_service_lane_candidate
- asphalt_parking_access_candidate
- concrete_or_sidewalk_candidate
- road_node_metadata_candidate

MAP-22I defines the survey contract: a structured manifest that records the current
resolution status of each candidate family and what must be done to resolve them.

This is a contract document only. No tile IDs are selected. No binary writes are
performed. No runtime proof is claimed.

## What this task does

For each distinct candidate family in the tile-family plan:
- Emit one survey family entry
- Status: UNRESOLVED_NEEDS_TILE_SURVEY (all families in MAP-22I)
- Confidence: NONE_YET
- Candidate tiles: empty (not yet surveyed)
- Notes: instruction for the future survey task

## Resolution statuses

| Status                        | Meaning                                         |
|-------------------------------|-------------------------------------------------|
| UNRESOLVED_NEEDS_TILE_SURVEY  | No tile ID surveyed yet                         |
| RESOLVED_BY_LOCAL_TILE_SURVEY | Tile ID confirmed by local TileZed inspection   |
| REJECTED_NOT_A_TILE_SURFACE   | No matching tile surface exists                 |

All families default to UNRESOLVED_NEEDS_TILE_SURVEY in MAP-22I.

## Survey JSON shape

```json
{
  "format": "pzmapforge.deadmtl.system2.static-road-tile-family-survey.v1",
  "status": "SURVEY_CONTRACT_ONLY",
  "runtime_status": "NOT_RUNTIME_PROVEN",
  "writer_status": "NOT_IMPLEMENTED",
  "source_tile_family_plan": "<path>",
  "families": [
    {
      "candidate_family": "asphalt_road_surface_candidate",
      "source_intents": ["local_street_asphalt"],
      "role": "road_surface",
      "resolution_status": "UNRESOLVED_NEEDS_TILE_SURVEY",
      "confidence": "NONE_YET",
      "candidate_tiles": [],
      "notes": "Future task must inspect local PZ tile definitions / TileZed tilesets before selecting tile IDs."
    }
  ],
  "totals": {
    "family_count": 6,
    "resolved_family_count": 0,
    "unresolved_family_count": 6,
    "candidate_tile_count": 0
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

Generate survey contract from MAP-22H tile-family plan:

```powershell
powershell -ExecutionPolicy Bypass -File examples\deadmtl-layer-pack\scripts\run-system2-static-road-tile-family-survey.ps1
```

CLI command directly:

```powershell
dotnet run --project src\PZMapForge.Cli --configuration Release -- `
  system2-build-static-road-tile-family-survey `
  --input   .local\deadmtl-authoring\system2-static-road-tile-family-plan\system2_static_road_tile_family_plan.json `
  --output  .local\deadmtl-authoring\system2-static-road-tile-family-survey\system2_static_road_tile_family_survey.json `
  --summary .local\deadmtl-authoring\system2-static-road-tile-family-survey\system2_static_road_tile_family_survey.summary.txt
```

## Output files

```
.local\deadmtl-authoring\system2-static-road-tile-family-survey\system2_static_road_tile_family_survey.json
.local\deadmtl-authoring\system2-static-road-tile-family-survey\system2_static_road_tile_family_survey.summary.txt
```

## What comes next

A future task must run a local tile survey (TileZed inspection or local tile definition
scan) to resolve each UNRESOLVED_NEEDS_TILE_SURVEY family to a specific tile ID or set
of tile ID candidates. That task updates `resolution_status` to
`RESOLVED_BY_LOCAL_TILE_SURVEY` and populates `candidate_tiles`.

No tile IDs are selected in MAP-22I. This is a contract document only.

## Claim boundary

- Does NOT write lotpack files.
- Does NOT write WorldGenOverride.lua.
- Does NOT install into a live PZ server.
- Runtime proof is NOT claimed.
- Public playable claim is NOT made.
- All families are UNRESOLVED_NEEDS_TILE_SURVEY. No tile IDs are committed.

VERDICT: MAP22I_SYSTEM2_STATIC_ROAD_TILE_FAMILY_SURVEY_COMPLETE
