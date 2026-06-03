# Tilesheet Format Investigation Decision Record

Status: Slice 3A-6-pre implemented
Date: 2026-06-02
Baseline commit: 297ef11

---

## Claim boundary

planning_artifact_only_not_pz_load_tested

All work under this gate produces local planning artifacts only.
No output produced under this record is a playable Project Zomboid export.

---

## Purpose

This decision record defines what must be known before any future TileZed
planning export or local tile GID work can begin.

It is a governance gate, not an implementation record.

No tile export is implemented here.
No .pack or .tiles contents are read here.
No PZ assets are copied or inspected here.

---

## Why Slice 3A-6 is blocked

The Phase 3A survey (PHASE_3A_DECISION.md) confirmed:

- Approximately 20 .pack files exist in the local PZ media/ directory.
- Approximately 7 .tiles files exist in the local PZ media/ directory.
- media/tiles/ does not exist; tilesheet assets are elsewhere.
- The internal format of .pack and .tiles files is not yet documented.

Without understanding the internal format of these files, the following
cannot be done safely:

- Extracting local tile GIDs for use in a planning TMX.
- Referencing specific tiles by ID in a TileZed-openable export.
- Claiming any compatibility with TileZed tile display.

Proceeding without this knowledge risks producing artifacts that reference
nonexistent tile IDs, creating silent failures that are hard to diagnose.

---

## What format knowledge is needed

Before Slice 3A-6 can begin, the following must be documented in a committed
decision record or operator-provided format summary:

1. The structure of a .pack file (magic bytes, header layout, entry table).
2. The structure of a .tiles file (whether it is text-based or binary;
   whether it references a .pack file by name or by embedded data).
3. How tile GIDs are assigned in these files and whether they match the
   GID space used by TileZed TMX files.
4. Whether tile GID extraction requires reading asset contents or whether
   a safe metadata-only path exists (e.g., reading only header fields,
   not pixel data).
5. Whether any public documentation or open-source tooling covers this
   format and is license-compatible with PZMapForge.

---

## Allowed investigation sources

The following sources are permitted when investigating the tilesheet format:

- Public Project Zomboid documentation and wiki pages.
- Operator-authored notes derived from personal inspection.
- Operator-provided format findings committed as redacted summaries
  (no file contents, no local paths, no asset data).
- Open-source tools or libraries that document or process .pack/.tiles
  files, provided their license is confirmed compatible before use.
- Local PZ install metadata summaries (directory listings, file counts,
  extension distributions) that do not read asset file contents.
- Generated test fixtures created by PZMapForge using fabricated data only.

---

## Forbidden investigation actions

The following actions are forbidden in any investigation under this gate:

- Reading real PZ asset file contents (bytes, pixel data, binary blobs).
- Copying Project Zomboid assets into the PZMapForge repository.
- Committing real local install paths, file names, or directory trees.
- Writing to media/maps.
- Reverse-engineering proprietary binary file formats without documented
  legal basis.
- Generating lotpack, lotheader, or bin files.
- Claiming a playable Project Zomboid export.
- Starting Slice 3A-6 implementation before this gate is closed by a
  committed format evidence record.

---

## Decision options

### Option A: Stay with planning artifacts only

Continue generating TileZed-openable planning TMX files that use only
fabricated or palette-derived tile references.
Do not attempt real GID mapping.

Consequence: TMX output loads in TileZed but displays no real PZ tiles.
The planning artifact remains a layout reference, not a visual match.

### Option B: Use documented public TileZed/TMX-compatible planning export only

Use the TileZed TMX specification (public XML format) to produce planning
exports that reference tile IDs from a fabricated or operator-provided tile
table.
Do not read .pack or .tiles contents.
The tile ID table would be authored manually by the operator from public
sources.

Consequence: Real-tile visual fidelity is possible only where the operator
can document tile IDs from public sources, not from direct asset inspection.

### Option C: Operator-approved local metadata survey only

Operator runs a read-only survey against their local .pack/.tiles files
(header fields only, no pixel data) and commits a redacted format summary.
PZMapForge uses only the committed summary; it never reads asset contents
in committed code.

Consequence: Requires operator to manually produce and commit a format
evidence record. Tile GID extraction would still need separate approval.

### Option D: Defer real tile export until licensed/approved format evidence exists

Do not attempt local tile GID extraction at all until a clear legal and
technical basis is documented.
Continue expanding planning artifact quality without real tile references.

Consequence: No real-tile visual fidelity in the near term. Planning
artifacts remain layout-only.

---

## Recommended decision

Option B + Option D.

Continue producing deterministic TileZed-openable planning TMX artifacts
using the existing planning-artifact-only approach (Option B).
Defer real tile GID extraction until format evidence is approved and a
separate load-test gate is explicitly created (Option D).

This keeps the product moving forward on TMX quality without requiring
.pack/.tiles format knowledge that is not yet safely obtained.

Real tile export is not blocked forever. It is blocked until:
1. Format evidence is documented and committed under a separate decision
   record.
2. A load-test gate with explicit evidence requirements is defined.
3. The operator explicitly approves the investigation path.

---

## Evidence required before proceeding to Slice 3A-6

All of the following must exist in the repository before Slice 3A-6 begins:

- [ ] A committed format evidence record naming the approved investigation
      source and summarizing .pack/.tiles structure in redacted form.
- [ ] Confirmation that the investigation source is license-compatible.
- [ ] A documented tile GID extraction approach that does not require reading
      PZ asset pixel data in committed code.
- [ ] Operator sign-off on the investigation path committed to a decision
      record.
- [ ] validate.ps1 passing at the current baseline.

---

## Future allowed next slice after gate passes

When the above evidence is committed and the operator approves:

Slice 3A-6 may implement a TileZed planning export that uses local tile GIDs
from a committed tile reference table. It must:

- Write output only to .local/ (gitignored).
- Not copy PZ assets.
- Not write to media/maps.
- Not generate lotpack, lotheader, or bin files.
- Maintain claim_boundary = planning_artifact_only_not_pz_load_tested unless
  a separate load-test gate is explicitly created and passed.

---

## Non-claims

- We do not yet claim TileZed-compatible local tile export.
- We do not yet know enough about .pack/.tiles/.lotpack/.lotheader/.bin
  handling to generate real PZ map output.
- Any future work must remain planning_artifact_only_not_pz_load_tested
  unless a separate load-test gate is explicitly created and passed.
- Slice 3A-6 may only begin after this decision record names an approved
  safe investigation path and the evidence checklist above is complete.

---

## Operator checklist

Before authorizing Slice 3A-6:

- [ ] Tilesheet format (.pack/.tiles) internal structure is documented in a
      committed evidence record.
- [ ] Investigation source is named and license-compatible.
- [ ] Tile GID extraction approach does not require reading asset pixel data
      in committed code.
- [ ] Option B + D (or an alternative) is explicitly confirmed by operator.
- [ ] validate.ps1 passes at baseline before any implementation begins.
- [ ] Slice 3A-6 decision record is committed before any code is written.
