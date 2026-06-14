# DeadMTL System 2 Human-Approved Tile Candidates (MAP-22N)

## Purpose

MAP-22M applies human review decisions to the tile candidate packet.

MAP-22N reads the applied review JSON and produces a clean manifest of:

- approved candidates (APPROVED_BY_HUMAN_REVIEW)
- rejected candidates (REJECTED_BY_HUMAN_REVIEW)
- pending candidates (NEEDS_MANUAL_REVIEW)

This manifest is an audit and selection layer only. It does NOT validate tiles at runtime
and does NOT create writer-ready tile mappings.

Human approval is NOT runtime validation.
Human approval is NOT writer readiness.
No runtime proof is claimed. No lotpack files are written. No WorldGenOverride.lua is written.
No candidates are promoted to writer-ready tile IDs.

## What this task does

- Reads the MAP-22M applied review JSON
- Splits review items into three buckets: approved, rejected, pending
- Assigns recommended_next_action per bucket
- Writes four output files: JSON, Markdown, CSV, summary

## Confidence values

| Bucket | Confidence |
|--------|------------|
| approved | HUMAN_REVIEW_ONLY_NOT_RUNTIME_PROVEN |
| rejected | (as set by MAP-22M) |
| pending | LOCAL_TEXT_MATCH_RANKED_ONLY |

## Default real workflow result

Because the current applied review uses the unedited MAP-22L CSV:

- approved_count = 0
- rejected_count = 0
- pending_count = 113

This is expected. All 113 candidates remain pending until a human edits the
decisions CSV and re-runs MAP-22M.

## Output files

| File | Description |
|------|-------------|
| system2_static_road_human_approved_tile_candidates.json | Structured manifest with three buckets |
| system2_static_road_human_approved_tile_candidates.md | Human-readable Markdown report |
| system2_static_road_human_approved_tile_candidates.csv | Spreadsheet-ready candidate list with bucket column |
| system2_static_road_human_approved_tile_candidates.summary.txt | Build summary |

## CSV columns

```
bucket,candidate_family,role,rank,tile_name,source_file,score,score_reasons,human_note,review_status,confidence,recommended_next_action
```

Where `bucket` is one of: `approved`, `rejected`, `pending`

## Commands

Generate manifest from MAP-22M applied review:

```powershell
powershell -ExecutionPolicy Bypass -File examples\deadmtl-layer-pack\scripts\run-system2-static-road-human-approved-tile-candidates.ps1
```

CLI command directly:

```powershell
dotnet run --project src\PZMapForge.Cli --configuration Release -- `
  system2-build-static-road-human-approved-tile-candidates `
  --input       .local\deadmtl-authoring\system2-static-road-tile-candidate-review-applied\system2_static_road_tile_candidate_review_applied.json `
  --output-json .local\deadmtl-authoring\system2-static-road-human-approved-tile-candidates\system2_static_road_human_approved_tile_candidates.json `
  --output-md   .local\deadmtl-authoring\system2-static-road-human-approved-tile-candidates\system2_static_road_human_approved_tile_candidates.md `
  --output-csv  .local\deadmtl-authoring\system2-static-road-human-approved-tile-candidates\system2_static_road_human_approved_tile_candidates.csv `
  --summary     .local\deadmtl-authoring\system2-static-road-human-approved-tile-candidates\system2_static_road_human_approved_tile_candidates.summary.txt
```

## Output directory

```
.local\deadmtl-authoring\system2-static-road-human-approved-tile-candidates\
```

## What comes next

A future task may use the approved candidates as inputs to a writer mapping pass.
Only after additional runtime validation may approved candidates be promoted to
writer-ready tile IDs. The approved list from MAP-22N is the starting point for that
validation, not the end product.

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

VERDICT: MAP22N_SYSTEM2_STATIC_ROAD_HUMAN_APPROVED_TILE_CANDIDATES_COMPLETE
