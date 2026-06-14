# DeadMTL System 2 Static Road Tile Candidate Review Packet (MAP-22L)

## Purpose

MAP-22K created a ranked shortlist of text-match tile candidates per candidate family
(confidence: LOCAL_TEXT_MATCH_RANKED_ONLY).

MAP-22L turns that shortlist into a human-readable review packet for manual inspection.
Every item starts as NEEDS_MANUAL_REVIEW. No item is approved or rejected automatically.

No binary writes are performed. No lotpack files are written. No runtime proof is claimed.
No candidates are promoted to final tile IDs.

## What this task does

- Reads the MAP-22K shortlist JSON
- Preserves family, order, rank, score, source_file, score_reasons
- Sets every review_status to NEEDS_MANUAL_REVIEW
- Writes four output files: JSON, Markdown, CSV, summary

## Review statuses

| Status | Meaning |
|--------|---------|
| NEEDS_MANUAL_REVIEW | Default; candidate requires human inspection |
| APPROVED_BY_HUMAN_REVIEW | Human has confirmed candidate is suitable |
| REJECTED_BY_HUMAN_REVIEW | Human has rejected candidate |

MAP-22L sets all items to NEEDS_MANUAL_REVIEW. A future task (MAP-22M or later) would
allow a human operator to update statuses after visual/runtime validation.

## Confidence

All review items carry confidence: LOCAL_TEXT_MATCH_RANKED_ONLY

## Output files

| File | Description |
|------|-------------|
| system2_static_road_tile_candidate_review.json | Structured review data |
| system2_static_road_tile_candidate_review.md | Human-readable Markdown packet |
| system2_static_road_tile_candidate_review.csv | Spreadsheet-ready candidate list |
| system2_static_road_tile_candidate_review.summary.txt | Build summary |

## CSV columns

```
candidate_family,role,rank,tile_name,source_file,score,score_reasons,review_status,recommended_next_action,confidence
```

## Commands

Generate review packet from MAP-22K shortlist:

```powershell
powershell -ExecutionPolicy Bypass -File examples\deadmtl-layer-pack\scripts\run-system2-static-road-tile-candidate-review.ps1
```

CLI command directly:

```powershell
dotnet run --project src\PZMapForge.Cli --configuration Release -- `
  system2-build-static-road-tile-candidate-review `
  --input       .local\deadmtl-authoring\system2-static-road-tile-candidate-shortlist\system2_static_road_tile_candidate_shortlist.json `
  --output-json .local\deadmtl-authoring\system2-static-road-tile-candidate-review\system2_static_road_tile_candidate_review.json `
  --output-md   .local\deadmtl-authoring\system2-static-road-tile-candidate-review\system2_static_road_tile_candidate_review.md `
  --output-csv  .local\deadmtl-authoring\system2-static-road-tile-candidate-review\system2_static_road_tile_candidate_review.csv `
  --summary     .local\deadmtl-authoring\system2-static-road-tile-candidate-review\system2_static_road_tile_candidate_review.summary.txt
```

## Output directory

```
.local\deadmtl-authoring\system2-static-road-tile-candidate-review\
```

## What comes next

A human operator must open the review packet (Markdown or CSV) and inspect each
candidate in TileZed or a tile sheet preview. Confirmed candidates can be marked
APPROVED_BY_HUMAN_REVIEW in a future pass. Only approved candidates may be promoted
to writer-ready tile IDs.

## Claim boundary

- Does NOT write lotpack files.
- Does NOT write WorldGenOverride.lua.
- Does NOT install into a live PZ server.
- Runtime proof is NOT claimed.
- Public playable claim is NOT made.
- All candidates carry confidence LOCAL_TEXT_MATCH_RANKED_ONLY.
- No candidates are final writer choices.
- Review statuses are set by the builder, not by runtime confirmation.

VERDICT: MAP22L_SYSTEM2_STATIC_ROAD_TILE_CANDIDATE_REVIEW_PACKET_COMPLETE
