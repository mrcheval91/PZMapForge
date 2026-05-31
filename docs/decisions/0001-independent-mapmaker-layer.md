# Decision 0001: Build an independent mapmaker layer instead of continuing WorldEd GUI work

Date: 2026-05-30
Status: Accepted

---

## Context

Project Zomboid map mods are typically built using The Indie Stone's GUI tools:
TileZed (tile painting), WorldEd (cell layout and lot generation), and
BuildingEd (interior layout). These tools work for their intended workflow but
proved problematic for a code-first, version-controlled approach:

- TileZed's tile palette did not present usable tiles for block-level layout painting.
- WorldEd's Generate Lots produced no lotpack output after multiple attempts.
- Assigning a generated TMX to a WorldEd cell crashed WorldEd.

The GUI session time produced no committed artifacts.

## Decision

Build an independent deterministic mapmaker layer (PZMapForge) that owns the
pipeline from blockout image to planning artifact, without depending on the
WorldEd GUI export path.

## Consequences

**Good:**
- Every output is reproducible from committed inputs.
- No fragile GUI session to replay.
- Planning artifacts are version-controlled JSON, Markdown, and PNG.
- The TMX format is simple enough to generate without WorldEd.

**Accepted cost:**
- The lotpack/lotheader/bin export step (required for a playable PZ map) is not
  covered by this layer. It requires a separate Phase 4 effort.
- WorldEd/TileZed remain necessary for the final export step. This layer does not
  replace them; it precedes them.

## Alternatives considered

**Continue WorldEd GUI work**: Rejected. No viable path forward without resolving
the crashes and palette issues, which require time not currently budgeted.

**Fork WorldEd**: Rejected for now. WorldEd is GPL. A fork would require a separate
license decision, significant C++/Qt investment, and carries distribution obligations.
Tracked as a future research item.

**Use a third-party TMX library**: Not explored. The TMX format (base64+gzip uint32
LE) is simple enough to generate directly in PowerShell without a library dependency.
