# Decision 0002: Commit to planning artifacts before attempting a playable export

Date: 2026-05-30
Status: Accepted

---

## Context

The temptation when building a new mapmaker tool is to aim directly at the end
goal: a playable Project Zomboid map. That end goal requires:

1. Correct lotpack/lotheader/bin generation.
2. A local PZ install that loads the map without error.
3. A documented load test result.

All three steps are currently unresolved. Attempting them without a stable
planning layer first would produce neither good planning artifacts nor a
reliable export.

## Decision

Phase 1 of PZMapForge delivers planning artifacts only:
- parsed-cell.json (semantic grid, counts, drift)
- parsed-cell-report.md
- parsed-cell-preview.png
- parsed-cell-tiles.png
- parsed-cell-basic.tmx (TileZed-openable, not PZ-loadable)

No playable export is claimed at this phase. The TMX is explicitly described as
a planning artifact in every output it appears in.

## Claim boundary that follows from this decision

Until a local load test passes:
- No lotpack/lotheader/bin claim.
- No "playable" or "PZ-compatible" claim.
- No Build 42 compatibility claim.
- No Steam Workshop upload.

These boundaries are enforced in docs/CONSTITUTION.md and in the tool output
itself (claim_boundary field in the JSON artifact).

## Consequences

**Good:**
- The current claim is provably true and testable.
- Each phase builds on a stable, evidence-bearing previous phase.
- The risk of overpromising is eliminated.

**Accepted cost:**
- A playable map is further away in the roadmap.
- The export step (Phase 4) will require separate WorldEd/format research.

## Trigger for Phase 4

Phase 4 (PZ-compatible export research) begins only when:
- The planning artifact pipeline is stable (28 tests pass).
- The TMX format is validated structurally (Phase 2 task).
- A documented investigation of the WorldEd lotpack format has been started.
