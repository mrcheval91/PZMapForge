# MAP-22O: System 2 Static Road Local Tile Survey (Filtered)

## Purpose

MAP-22O produces a filtered version of the MAP-22J local tile survey.
MAP-22J scanned too broadly: sources like `media\profanity\Dictionary.txt`
contributed tile candidates such as `tile_name: "asphalt"` from non-tile contexts.

MAP-22O adds source path filtering to exclude known non-tile text paths before
scanning for tile name candidates.

This task is **source filtering / candidate hygiene only**.

## What this produces

A JSON file, Markdown report, CSV, and summary text with:
- Families from the MAP-22I survey contract
- Per-family candidate tiles found in scanned (non-excluded) files only
- Source filter record listing excluded path fragments, preferred fragments, and allowed extensions
- Totals including `scanned_source_file_count` and `excluded_source_file_count`

## What this does NOT produce

- NOT a runtime-validated tile set
- NOT a writer-ready tile ID list
- NOT a lotpack file
- NOT WorldGenOverride.lua
- NOT a playable map export
- NOT a public mod package

## Confidence level

`LOCAL_FILTERED_TEXT_MATCH_ONLY`

All candidates require visual inspection (TileZed or tile sheet preview) and
runtime validation before writer use.

## Excluded path fragments

The following source path fragments are excluded from scanning:

- `media\profanity`
- `media\lua`
- `media\radio`
- `media\sound`
- `media\music`
- `media\maps`
- `media\scripts\items`
- `media\scripts\vehicles`
- `media\scripts\recipes`
- `media\scripts\clothing`

## Allowed file extensions

`.tiles`, `.tiles2`, `.txt`, `.xml`, `.lua`

## Preferred path fragments (informational)

These fragments flag high-signal sources; they are not required for inclusion:

- `media\tiles`
- `media\tiledefinitions`
- `media\newtiledefinitions`

## Inputs

| Arg | Description |
|---|---|
| `--input` | MAP-22I survey contract JSON (tile family survey) |
| `--pz-root` | Local PZ install root. Missing root → zero-candidate survey, exit 0 |
| `--output-json` | Filtered survey JSON (must be under `.local/`) |
| `--output-md` | Filtered survey Markdown (must be under `.local/`) |
| `--output-csv` | Filtered survey CSV (must be under `.local/`) |
| `--summary` | Summary text file (must be under `.local/`) |

## Output dir

`.local\deadmtl-authoring\system2-static-road-local-tile-survey-filtered\`

## Status fields

| Field | Value |
|---|---|
| `status` | `LOCAL_TILE_SURVEY_FILTERED_ONLY` |
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

Runtime proof is NOT claimed. Human inspection required before writer use.
Human approval of candidates from this survey is NOT writer readiness.

## CSV format

`candidate_family,role,tile_name,source_file,match_reason,confidence,resolution_status`

## Run

```powershell
powershell -ExecutionPolicy Bypass -File examples\deadmtl-layer-pack\scripts\run-system2-static-road-local-tile-survey-filtered.ps1
```

## Verdict

MAP22O_SYSTEM2_STATIC_ROAD_LOCAL_TILE_SURVEY_FILTERED_COMPLETE
