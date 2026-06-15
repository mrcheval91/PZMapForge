# MAP-22P: System 2 Static Road Filtered Tile Candidate Shortlist

## Purpose

MAP-22P ranks and shortlists candidates from the MAP-22O source-filtered local tile survey.

MAP-22K ranked candidates from the broad MAP-22J survey, which included polluted sources
(e.g. `media\profanity\Dictionary.txt`). MAP-22P operates on MAP-22O's cleaner filtered survey
and applies deterministic scoring tuned for tile-definition sources.

This task is **filtered shortlist / ranking only**.

## What this produces

A JSON shortlist, Markdown report, CSV, and summary text with:
- Up to 25 ranked candidates per surface family (configurable via `--top`)
- Deterministic scoring with positive terms, source file bonuses, and negative exclusions
- `road_node_metadata_candidate` stays UNRESOLVED_METADATA_ONLY (no tile surface terms)
- All candidates at confidence: LOCAL_FILTERED_TEXT_MATCH_RANKED_ONLY

## What this does NOT produce

- NOT a runtime-validated tile set
- NOT a writer-ready tile ID list
- NOT a lotpack file
- NOT WorldGenOverride.lua
- NOT a playable map export
- NOT a public mod package

## Confidence level

`LOCAL_FILTERED_TEXT_MATCH_RANKED_ONLY`

Filtered shortlist is **not runtime proof** and **not writer readiness**.
All candidates require visual inspection (TileZed or tile sheet preview) and
runtime validation before writer use.

## Scoring

Positive primary terms per family (each: +20):

| Family | Terms |
|---|---|
| asphalt_road_surface_candidate | asphalt, street, road, pavement |
| asphalt_alley_surface_candidate | asphalt, alley, road, pavement |
| asphalt_service_lane_candidate | asphalt, service, road, lane, pavement |
| asphalt_parking_access_candidate | asphalt, parking, driveway, pavement |
| concrete_or_sidewalk_candidate | concrete, sidewalk, pavement, curb |

Tile name surface bonus terms (+10 each):
`floor`, `exterior`, `pavement`, `asphalt`, `concrete`

Source file bonuses:
- `media/tiles` or `media\tiles`: +12
- `tiledefinitions` or `newtiledefinitions`: +10
- `exterior`: +5

Negative tile name terms:
- `wall`, `roof`, `window`, `door`: -100 each
- `furniture`, `vehicle`, `car`, `sign`: -80 each
- `vegetation`, `tree`, `bush`, `water`: -60 each
- `blood`, `shadow`, `overlay`: -30 each

Sort: score descending → tile_name ascending → source_file ascending.

## Inputs

| Arg | Description |
|---|---|
| `--input` | MAP-22O filtered local tile survey JSON |
| `--output-json` | Shortlist JSON (must be under `.local/`) |
| `--output-md` | Shortlist Markdown (must be under `.local/`) |
| `--output-csv` | Shortlist CSV (must be under `.local/`) |
| `--summary` | Summary text file (must be under `.local/`) |
| `--top` | Max candidates per family (default: 25) |

## Output dir

`.local\deadmtl-authoring\system2-static-road-filtered-tile-candidate-shortlist\`

## Status fields

| Field | Value |
|---|---|
| `status` | `FILTERED_SHORTLIST_ONLY` |
| `runtime_status` | `NOT_RUNTIME_PROVEN` |
| `writer_status` | `NOT_IMPLEMENTED` |

## Claim boundary

| Claim | Value |
|---|---|
| writes_lotpack | false |
| writes_worldgen_lua | false |
| runtime_proven | false |
| public_playable_claim | false |
| writer_ready_claim | false |

Runtime proof is NOT claimed. Filtered shortlist is not writer-ready.

## CSV format

`candidate_family,role,rank,tile_name,source_file,score,score_reasons,source_confidence,confidence,resolution_status`

## Run

```powershell
powershell -ExecutionPolicy Bypass -File examples\deadmtl-layer-pack\scripts\run-system2-static-road-filtered-tile-candidate-shortlist.ps1
```

## Verdict

MAP22P_SYSTEM2_STATIC_ROAD_FILTERED_TILE_CANDIDATE_SHORTLIST_COMPLETE
