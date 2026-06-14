# DeadMTL System 2 Static Road Tile Candidate Shortlist (MAP-22K)

## Purpose

MAP-22J produced a local text-match survey: up to 3100 candidate tile names per candidate
family, each with confidence LOCAL_TEXT_MATCH_ONLY.

MAP-22K reduces those candidates into a ranked shortlist per family using deterministic
string scoring. Default top N: 25 candidates per family.

No binary writes are performed. No lotpack files are written. No runtime proof is claimed.
No candidates are promoted to final tile IDs.

## What this task does

For each candidate family:
- Score each candidate by tile_name and source_file string matching
- Apply positive terms (family-specific) and negative terms (all surface families)
- Sort by score desc, then tile_name asc
- Take top N (default 25)
- Assign confidence: LOCAL_TEXT_MATCH_RANKED_ONLY

The `road_node_metadata_candidate` family stays UNRESOLVED_METADATA_ONLY unless
MAP-22J provided candidates for it.

## Scoring terms by family

### asphalt_road_surface_candidate
| Term | Points | Field |
|------|--------|-------|
| asphalt  | +50 | tile_name |
| street   | +25 | tile_name |
| road     | +20 | tile_name |
| exterior | +10 | tile_name |
| tiles    |  +8 | source_file |
| media    |  +5 | source_file |

### asphalt_alley_surface_candidate
| Term | Points | Field |
|------|--------|-------|
| asphalt  | +50 | tile_name |
| alley    | +30 | tile_name |
| street   | +20 | tile_name |
| road     | +20 | tile_name |
| exterior | +10 | tile_name |

### asphalt_service_lane_candidate
| Term | Points | Field |
|------|--------|-------|
| asphalt  | +50 | tile_name |
| service  | +25 | tile_name |
| lane     | +20 | tile_name |
| road     | +15 | tile_name |
| exterior | +10 | tile_name |

### asphalt_parking_access_candidate
| Term | Points | Field |
|------|--------|-------|
| asphalt   | +40 | tile_name |
| parking   | +35 | tile_name |
| driveway  | +25 | tile_name |
| pavement  | +15 | tile_name |
| exterior  | +10 | tile_name |

### concrete_or_sidewalk_candidate
| Term | Points | Field |
|------|--------|-------|
| sidewalk | +50 | tile_name |
| concrete | +40 | tile_name |
| curb     | +25 | tile_name |
| pavement | +15 | tile_name |
| exterior | +10 | tile_name |

### Negative terms (all surface families, tile_name)
| Term | Points |
|------|--------|
| wall       | -100 |
| roof       | -100 |
| window     | -100 |
| door       | -100 |
| furniture  |  -80 |
| vehicle    |  -80 |
| sign       |  -60 |
| vegetation |  -60 |
| tree       |  -60 |
| water      |  -60 |
| blood      |  -40 |
| shadow     |  -30 |
| overlay    |  -30 |

## Resolution statuses

| Status | Meaning |
|--------|---------|
| SHORTLISTED_CANDIDATES_PRESENT | Scored candidates produced for this family |
| SHORTLISTED_NO_CANDIDATES | No input candidates available |
| UNRESOLVED_METADATA_ONLY | road_node family with no input candidates |

## Confidence

All shortlisted candidates carry confidence: LOCAL_TEXT_MATCH_RANKED_ONLY

This reflects that candidates were found by text search and ranked by string scoring only.
No runtime tile loading, no TileZed visual confirmation, and no PZ load test has confirmed
these tile IDs. Candidates must be validated visually or at runtime before use in any writer.

## Output JSON shape

```json
{
  "format": "pzmapforge.deadmtl.system2.static-road-tile-candidate-shortlist.v1",
  "status": "SHORTLIST_ONLY",
  "runtime_status": "NOT_RUNTIME_PROVEN",
  "writer_status": "NOT_IMPLEMENTED",
  "source_local_tile_survey": "<path>",
  "top_per_family": 25,
  "families": [
    {
      "candidate_family": "asphalt_road_surface_candidate",
      "role": "road_surface",
      "source_intents": ["local_street_asphalt"],
      "input_candidate_count": 123,
      "shortlisted_candidate_count": 25,
      "resolution_status": "SHORTLISTED_CANDIDATES_PRESENT",
      "confidence": "LOCAL_TEXT_MATCH_RANKED_ONLY",
      "candidates": [
        {
          "rank": 1,
          "tile_name": "floors_exterior_street_asphalt_01_0",
          "source_file": "relative/path/to/source",
          "score": 88,
          "score_reasons": ["tile_name contains asphalt", "tile_name contains street", "source_file contains tiles"],
          "source_confidence": "LOCAL_TEXT_MATCH_ONLY",
          "confidence": "LOCAL_TEXT_MATCH_RANKED_ONLY"
        }
      ],
      "notes": "Shortlist requires visual/runtime validation before writer use."
    }
  ],
  "totals": {
    "family_count": 6,
    "input_candidate_count": 3100,
    "shortlisted_candidate_count": 125,
    "families_with_shortlist": 5,
    "families_without_shortlist": 1
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

Generate shortlist from MAP-22J local tile survey:

```powershell
powershell -ExecutionPolicy Bypass -File examples\deadmtl-layer-pack\scripts\run-system2-static-road-tile-candidate-shortlist.ps1
```

CLI command directly:

```powershell
dotnet run --project src\PZMapForge.Cli --configuration Release -- `
  system2-build-static-road-tile-candidate-shortlist `
  --input   .local\deadmtl-authoring\system2-static-road-local-tile-survey\system2_static_road_local_tile_survey.json `
  --output  .local\deadmtl-authoring\system2-static-road-tile-candidate-shortlist\system2_static_road_tile_candidate_shortlist.json `
  --summary .local\deadmtl-authoring\system2-static-road-tile-candidate-shortlist\system2_static_road_tile_candidate_shortlist.summary.txt `
  --top 25
```

## Output files

```
.local\deadmtl-authoring\system2-static-road-tile-candidate-shortlist\system2_static_road_tile_candidate_shortlist.json
.local\deadmtl-authoring\system2-static-road-tile-candidate-shortlist\system2_static_road_tile_candidate_shortlist.summary.txt
```

## What comes next

A future task must visually validate shortlisted candidates in TileZed or run a local PZ
load test to confirm which candidates correspond to visible tile surfaces. Only then can a
candidate be promoted to a writer-ready tile ID.

## Claim boundary

- Does NOT write lotpack files.
- Does NOT write WorldGenOverride.lua.
- Does NOT install into a live PZ server.
- Runtime proof is NOT claimed.
- Public playable claim is NOT made.
- All candidates carry confidence LOCAL_TEXT_MATCH_RANKED_ONLY.
- No candidates are final writer choices.
- Ranking is by deterministic string scoring only.

VERDICT: MAP22K_SYSTEM2_STATIC_ROAD_TILE_CANDIDATE_SHORTLIST_COMPLETE
