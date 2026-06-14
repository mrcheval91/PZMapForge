# DeadMTL System 2 Static Road Local Tile Survey (MAP-22J)

## Purpose

MAP-22I created a survey contract: 6 candidate families, all UNRESOLVED_NEEDS_TILE_SURVEY.

MAP-22J scans the local PZ install root for text-based tile definition files and
produces a candidate list for each family where search terms match. This is a
text-match-only survey. Candidates are not verified at runtime. They are not final
writer choices.

No binary writes are performed. No lotpack files are written. No runtime proof is claimed.

## What this task does

For each candidate family:
- Search `.tiles`, `.lua`, `.txt`, `.xml` files under the PZ install root
- Extract identifiers containing the family's search terms (case-insensitive)
- Deduplicate candidates by name
- Assign confidence: LOCAL_TEXT_MATCH_ONLY

The `road_node_metadata_candidate` family has no search terms and stays
UNRESOLVED_NEEDS_TILE_SURVEY. It is metadata, not a tile surface.

If the PZ install root does not exist, all families are output as
UNRESOLVED_NEEDS_TILE_SURVEY with zero candidates. The command exits zero.

## Search terms by family

| Candidate Family              | Search Terms                            |
|-------------------------------|------------------------------------------|
| asphalt_road_surface_candidate   | asphalt, street, road, pavement       |
| asphalt_alley_surface_candidate  | asphalt, alley, road, pavement        |
| asphalt_service_lane_candidate   | asphalt, service, road, lane, pavement|
| asphalt_parking_access_candidate | asphalt, parking, driveway, pavement  |
| concrete_or_sidewalk_candidate   | concrete, sidewalk, pavement, curb    |
| road_node_metadata_candidate     | (none - metadata only)                |

## Resolution statuses

| Status                        | Meaning                                         |
|-------------------------------|-------------------------------------------------|
| SURVEYED_CANDIDATES_FOUND     | Text matches found; candidates listed           |
| SURVEYED_NO_CANDIDATES_FOUND  | Search ran but no identifiers matched           |
| UNRESOLVED_NEEDS_TILE_SURVEY  | No search performed (PZ root missing, or metadata family) |
| REJECTED_NOT_A_TILE_SURFACE   | Not applicable in MAP-22J                       |

## Confidence

All candidates carry confidence: LOCAL_TEXT_MATCH_ONLY

This reflects that candidates were found by text search only. No runtime tile loading,
no TileZed visual confirmation, and no PZ load test has confirmed these tile IDs.

Candidates must be validated visually or at runtime before use in any writer.

## Output JSON shape

```json
{
  "format": "pzmapforge.deadmtl.system2.static-road-local-tile-survey.v1",
  "status": "LOCAL_TILE_SURVEY_ONLY",
  "runtime_status": "NOT_RUNTIME_PROVEN",
  "writer_status": "NOT_IMPLEMENTED",
  "source_survey": "<path>",
  "pz_root": "D:\\Program Files (x86)\\Steam\\steamapps\\common\\ProjectZomboid",
  "families": [
    {
      "candidate_family": "asphalt_road_surface_candidate",
      "source_intents": ["local_street_asphalt"],
      "role": "road_surface",
      "resolution_status": "SURVEYED_CANDIDATES_FOUND",
      "confidence": "LOCAL_TEXT_MATCH_ONLY",
      "search_terms": ["asphalt", "street", "road", "pavement"],
      "candidate_tiles": [
        {
          "tile_name": "floors_exterior_street_asphalt_01_0",
          "source_file": "relative/path/to/source",
          "match_reason": "matched term: asphalt",
          "confidence": "LOCAL_TEXT_MATCH_ONLY"
        }
      ],
      "notes": "Candidates require visual/runtime validation before writer use."
    }
  ],
  "totals": {
    "family_count": 6,
    "families_with_candidates": 0,
    "families_without_candidates": 6,
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

Generate local tile survey from MAP-22I survey contract:

```powershell
powershell -ExecutionPolicy Bypass -File examples\deadmtl-layer-pack\scripts\run-system2-static-road-local-tile-survey.ps1
```

CLI command directly:

```powershell
dotnet run --project src\PZMapForge.Cli --configuration Release -- `
  system2-build-static-road-local-tile-survey `
  --input   .local\deadmtl-authoring\system2-static-road-tile-family-survey\system2_static_road_tile_family_survey.json `
  --pz-root "D:\Program Files (x86)\Steam\steamapps\common\ProjectZomboid" `
  --output  .local\deadmtl-authoring\system2-static-road-local-tile-survey\system2_static_road_local_tile_survey.json `
  --summary .local\deadmtl-authoring\system2-static-road-local-tile-survey\system2_static_road_local_tile_survey.summary.txt
```

## Output files

```
.local\deadmtl-authoring\system2-static-road-local-tile-survey\system2_static_road_local_tile_survey.json
.local\deadmtl-authoring\system2-static-road-local-tile-survey\system2_static_road_local_tile_survey.summary.txt
```

## What comes next

A future task must visually validate candidate tiles in TileZed or run a local PZ load
test to confirm which candidates correspond to visible tile surfaces. Only then can a
candidate be promoted to a writer-ready tile ID.

## Claim boundary

- Does NOT write lotpack files.
- Does NOT write WorldGenOverride.lua.
- Does NOT install into a live PZ server.
- Runtime proof is NOT claimed.
- Public playable claim is NOT made.
- All candidates carry confidence LOCAL_TEXT_MATCH_ONLY.
- No candidates are final writer choices.

VERDICT: MAP22J_SYSTEM2_STATIC_ROAD_LOCAL_TILE_SURVEY_COMPLETE
