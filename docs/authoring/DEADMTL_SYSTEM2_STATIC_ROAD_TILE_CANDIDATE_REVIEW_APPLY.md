# DeadMTL System 2 Static Road Tile Candidate Review Apply (MAP-22M)

## Purpose

MAP-22L created a review packet where every item starts as NEEDS_MANUAL_REVIEW.

MAP-22M allows a human-edited review CSV or JSON to be applied back into a structured
reviewed-candidates file. This creates an auditable status layer:

- APPROVED_BY_HUMAN_REVIEW: human reviewed and considers the tile worth keeping
- REJECTED_BY_HUMAN_REVIEW: human reviewed and excluded the tile
- NEEDS_MANUAL_REVIEW: not yet reviewed

Human approval is NOT runtime validation. Human approval is NOT writer readiness.
No runtime proof is claimed. No lotpack files are written. No WorldGenOverride.lua is written.
No candidates are promoted to writer-ready tile IDs.

## What this task does

- Reads the MAP-22L base review JSON
- Reads a human-edited decisions CSV
- Matches decisions to base review items by candidate_family + rank + tile_name
- Applies status changes (APPROVED_BY_HUMAN_REVIEW or REJECTED_BY_HUMAN_REVIEW)
- Writes four output files: JSON, Markdown, CSV, summary

## Review statuses

| Status | Meaning |
|--------|---------|
| NEEDS_MANUAL_REVIEW | Not yet reviewed; default from MAP-22L |
| APPROVED_BY_HUMAN_REVIEW | Human considers this tile worth keeping for next validation pass |
| REJECTED_BY_HUMAN_REVIEW | Human has excluded this tile |

## Confidence values

| Review status | Confidence |
|--------------|------------|
| NEEDS_MANUAL_REVIEW | LOCAL_TEXT_MATCH_RANKED_ONLY |
| APPROVED_BY_HUMAN_REVIEW | HUMAN_REVIEW_ONLY_NOT_RUNTIME_PROVEN |
| REJECTED_BY_HUMAN_REVIEW | HUMAN_REVIEW_ONLY_NOT_RUNTIME_PROVEN |

## CSV input columns

The decisions CSV is the MAP-22L output CSV (optionally human-edited):

```
candidate_family,role,rank,tile_name,source_file,score,score_reasons,review_status,recommended_next_action,confidence
```

Optional additional column (added by the human reviewer):

```
human_note
```

Matching is done by: `candidate_family` + `rank` + `tile_name`

## Decision rules

- If a CSV row does not match any base item: skip it; increment unknown_decision_count; add warning
- If a CSV row has an invalid review_status: skip it; increment unknown_decision_count; add warning
- If duplicate rows target the same item: last row wins; count as one applied decision; add warning
- None of these failures cause the whole command to fail

## Output files

| File | Description |
|------|-------------|
| system2_static_road_tile_candidate_review_applied.json | Structured applied review data |
| system2_static_road_tile_candidate_review_applied.md | Human-readable Markdown report |
| system2_static_road_tile_candidate_review_applied.csv | Spreadsheet-ready candidate list with statuses |
| system2_static_road_tile_candidate_review_applied.summary.txt | Build summary |

## CSV output columns

```
candidate_family,role,rank,tile_name,source_file,score,score_reasons,review_status,human_note,recommended_next_action,confidence
```

## Commands

Generate applied review packet using the unedited MAP-22L CSV (all items remain NEEDS_MANUAL_REVIEW):

```powershell
powershell -ExecutionPolicy Bypass -File examples\deadmtl-layer-pack\scripts\run-system2-static-road-tile-candidate-review-apply.ps1
```

CLI command directly:

```powershell
dotnet run --project src\PZMapForge.Cli --configuration Release -- `
  system2-apply-static-road-tile-candidate-review `
  --review-json   .local\deadmtl-authoring\system2-static-road-tile-candidate-review\system2_static_road_tile_candidate_review.json `
  --decisions-csv .local\deadmtl-authoring\system2-static-road-tile-candidate-review\system2_static_road_tile_candidate_review.csv `
  --output-json   .local\deadmtl-authoring\system2-static-road-tile-candidate-review-applied\system2_static_road_tile_candidate_review_applied.json `
  --output-md     .local\deadmtl-authoring\system2-static-road-tile-candidate-review-applied\system2_static_road_tile_candidate_review_applied.md `
  --output-csv    .local\deadmtl-authoring\system2-static-road-tile-candidate-review-applied\system2_static_road_tile_candidate_review_applied.csv `
  --summary       .local\deadmtl-authoring\system2-static-road-tile-candidate-review-applied\system2_static_road_tile_candidate_review_applied.summary.txt
```

## Output directory

```
.local\deadmtl-authoring\system2-static-road-tile-candidate-review-applied\
```

## What comes next

A future task (MAP-22N or later) may use the approved candidates as inputs to a writer
mapping pass. Only then, after additional runtime validation, may candidates be promoted
to writer-ready tile IDs.

## Claim boundary

- Does NOT write lotpack files.
- Does NOT write WorldGenOverride.lua.
- Does NOT install into a live PZ server.
- Runtime proof is NOT claimed.
- Public playable claim is NOT made.
- Writer-ready claim is NOT made.
- Human approval is NOT runtime validation.
- All approved candidates carry confidence HUMAN_REVIEW_ONLY_NOT_RUNTIME_PROVEN.
- No candidates are final writer choices.

VERDICT: MAP22M_SYSTEM2_STATIC_ROAD_TILE_CANDIDATE_REVIEW_APPLY_COMPLETE
