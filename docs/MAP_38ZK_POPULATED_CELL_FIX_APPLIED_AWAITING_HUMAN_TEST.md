# MAP-38ZK: Populated-cell fix applied and file-level confirmed — awaiting human in-game test

## Context

Follow-on to [MAP_38ZJ](MAP_38ZJ_REAL_WORLDED_COMPILER_OUTPUT_NEGATIVE_ON_EMPTY_CELL.md),
which diagnosed the negative in-game result (fresh save, teleport to
180,135,0, Tile Report showed only `floors_exterior_street_01_0`) as almost
certainly caused by targeting an unpopulated vanilla Muldraugh cell (0,0),
matching MAP-38A's already-documented "cell-choice mistake" failure mode.

## What was done

1. **Ground-truthed the hypothesis directly**, instead of continuing to
   guess coordinates: checked the real Project Zomboid install
   (`D:\Program Files (x86)\Steam\steamapps\common\ProjectZomboid\media\maps\Muldraugh, KY\`,
   4065 real `.lotheader` files) for the exact cell names in play.
   - `0_0.lotheader`, `0_1.lotheader`, `1_0.lotheader`, `1_1.lotheader` —
     **all four confirmed ABSENT**. This is hard proof the v1 test target
     was genuinely unpopulated, not a hypothesis.
2. Re-ran WorldEd's `Generate Lots` on the same pzw cell (0,0) — note the
   `.pzw` had since been rewritten by the 35-building batch-placement script,
   so the lot's local position is now `x=5 y=5` (not the earlier manual-drag
   test's `x=165 y=127`) — with **World Origin X=26, Y=38**. Output:
   `30_44.lotheader` (7606 bytes, 132-entry signature, matching the
   previous run's building-cell fingerprint), plus companions `31_44`,
   `30_45`, `31_45`. WorldEd's success dialog: **Buildings: 1, Rooms: 72,
   Room rects: 82, Room objects: 0**.
3. **Checked `30_44` against the same real vanilla map folder: EXISTS.**
   Confirmed populated, real content, safe overlay target.
4. **Important negative finding**: the World-Origin-input → output-cell
   mapping is **not a fixed offset**. Two data points:
   - input (31,45) → output block top-left (36,52) — offset (+5,+7)
   - input (26,38) → output block top-left (30,44) — offset (+4,+6)

   The offset itself changes between runs. Do not assume a formula or
   extrapolate a target coordinate from World Origin input alone — always
   generate, read the actual output filename, and cross-check it against
   the real vanilla map folder's file list before trusting it as a target.
   This directly checking approach (existence check against the real game's
   own map folder) is now the validated method, replacing coordinate math
   guesses.
5. Re-staged `pzmapforge_worlded_realbuild_test001` mod folder
   (`C:\Users\Palmacede\Zomboid\mods\pzmapforge_worlded_realbuild_test001\`):
   removed the old unpopulated-cell files (`0_0`/`0_1`/`1_0`/`1_1` lotheader/
   lotpack/chunkdata — note `world_0_0.lotpack` could not be deleted, file
   locked by a running `ProjectZomboid64` process; harmless orphan since its
   matching `.lotheader` is gone), copied in `30_44`/`31_44`/`30_45`/`31_45`
   lotheader/lotpack/chunkdata. Updated `map.info` description, `spawnpoints.lua`,
   `objects.lua`, and `README_TEST_STATUS.txt` with the new target: absolute
   world tile **X=9015, Y=13215, Z=0** (vanilla cell 30,44 base 9000,13200
   + lot local offset 5,5), inside the building's ~33x21 tile footprint
   (X 9005-9038, Y 13205-13226).

## Status

PROVISIONAL — file-level fix confirmed (target cell verified present in the
real vanilla map data, which the v1 target was not), mod re-staged with
correct files and instructions. **Not yet human-tested in-game.** Next step
is the same fresh-save + debug-teleport + Tile-Report verification pattern
already used for v1, now at the corrected coordinate.

## Open items

- Human in-game test at (9015,13215,0), fresh save, not yet performed.
- `world_0_0.lotpack` orphan file in the mod folder should be deleted once
  the game process holding it is closed (cosmetic only, not expected to
  affect loading since its `.lotheader` is gone).
- The World-Origin → output-cell mapping remains unexplained (not just
  unconfirmed) — worth deriving properly if/when this needs to scale to all
  35 buildings, since per-building trial-and-error against the vanilla file
  list is viable for one building but not for 35.

## UPDATE: v2 tested, confirmed negative — but at the WRONG coordinate (300 vs 256-tile cell math)

Human test (fresh restart, correct files on disk confirmed via a stale-process
false start first — see below) landed exactly at (9017,13213,0), Tile Report
`f_bushes_1_11` — plain vegetation, not the building. This looked like a real
negative until re-checking this repo's own prior finding: **MAP-38J already
established real B42 cells are 256 tiles wide, not 300** (confirmed via a
human test at cell 34_26 landing exactly on a `cell*256` computed target).
This session's v2 coordinate (9015,13215) was computed as `30*300+5,
44*300+5` — the wrong, pre-MAP-38J formula. Correct target: `30*256+5,
44*256+5` = **(7685,11269)**, center-ish safe point (7700,11280).

**Process note (also real, cost a full test cycle)**: the first "test" of v2
wasn't a real test at all — the game process had already spawned the
character (23:35:46) using the OLD stale mod files, over 35 minutes *before*
the corrected files were copied onto disk (00:11:09). Confirmed via debug
log (`no room or building at 170,135,0` — the v1 coordinate). A full game
restart is required after any mod-folder file change; an already-running
process does not pick up edits.

v3 (this update): re-staged mod's `objects.lua`/`spawnpoints.lua` with the
256-tile-corrected target (7700,11280,0 / legacy fields worldX=25,worldY=37,
posX=200,posY=180). Not yet human-tested at the corrected coordinate.

## UPDATE: REAL ROOT CAUSE FOUND — WorldEd compiled the building's tile catalog but never wrote its geometry into the lotpack

v3 tested at the coordinate-corrected target (7700,11280,0): still fallback
forest. Before requesting a fourth human test, did a full byte-level audit
of `world_30_44.lotpack` instead — this should have been done before the
v2/v3 test cycles, not after.

**Method**: parsed every one of the 1024 chunks' body length. Only one
contiguous block (chunk cols 19-31, rows 15-31 — a ~104x136 tile area
anchored at the cell's far corner) is non-default; every other chunk,
including the entire region covering the building's placed lot (cols 0-4,
rows 0-3, local tile x=5-38,y=5-26), is the 8-byte "whole-chunk-default"
shorthand — literally nothing written there.

**Then decoded the Type-B (explicit tile) records inside that one non-default
block** and cross-referenced tile indices against the lotheader's 132-name
catalog. Result: only 3 distinct tiles are ever actually placed anywhere in
the file — `floors_exterior_natural_01_0`, `floors_exterior_street_01_0`,
`floors_exterior_tilesandstone_01_0`. All plain ground/terrain surface
tiles, consistent with being the base `.tmx`'s own terrain, not the
building. **None of the other 129 catalogued tile names (appliances_*,
fixtures_bathroom_*, fixtures_counters_*, fixtures_doors_*, ceilings_*,
etc. — the actual apartment building content) are referenced by a single
chunk record anywhere in the file.**

**Conclusion**: WorldEd's `Generate Lots 8x8 (B42+ map)` compiler parsed
the placed `.tbx` well enough to catalog its tile vocabulary into the
lotheader and report accurate room/rect counts in its own success dialog
("Buildings: 1, Rooms: 72") — but never rasterized the building into actual
per-chunk tile records in the lotpack. The success dialog's counts are
metadata-level (parsed from the .tbx's own internal room definitions),
not proof the compile step wrote real geometry. **This is a compiler
limitation/bug in this specific WorldEd Unofficial Fork build
(Build 20250402) for this pipeline, not a coordinate, cache-staleness, or
cell-population issue** — all three of those were real, correctly-diagnosed
side issues (fixed along the way) but none was the actual blocker.

**Retracted**: do not trust WorldEd's Generate Lots success dialog
("Buildings: N, Rooms: N") as evidence of a complete compile. Always
byte-audit the resulting `world_X_Y.lotpack` (scan for non-default chunks,
decode Type-B records, cross-reference tile names against the lotheader)
before staging a mod or requesting a human test.

**Not yet investigated**: whether there's a missing manual step before
Generate Lots (e.g. a "bake"/"convert to lot" action distinct from the
drag-and-drop placement — TileZed's own "Convert Selection To Lot..." menu
item was seen greyed-out in the 2026-07-12 session and never resolved),
or whether this fork's Generate Lots simply doesn't support building
rasterization at all and only ever worked for flat ground-tile content
(matching every actually-confirmed-working candidate in this whole
project's history, MAP-38K through MAP-38ZI, which were all flat/ground/
vegetation/wall-texture tiles — never a real multi-tile building compiled
by any tool, hand-rolled or WorldEd, until this attempt, which now also
appears to not have worked).

## MAJOR CORRECTION: the "no building tiles" audit was wrong -- real building content IS present

v3's negative result (fallback forest at the coordinate-corrected target)
triggered a positive-control check before requesting a 4th human test:
audited a REAL vanilla lotpack (`31_45.lotpack`, Rosewood Firestation area,
already known-populated from this project's own history) with the exact
same audit script used to declare v1-v3 negative. **Result: only 1 distinct
tile ever found, despite the lotheader cataloguing 1350 real building tile
names.** The audit script itself was broken, not the compile.

**Root cause of the audit bug**: the script stopped reading each chunk
once its 64-slot floor-tile stream was consumed. Real chunks (confirmed via
raw hex inspection of `31_45`'s chunk (0,0)) contain a SECOND section after
the floor stream: a variable-length list of 16-byte `[tag=3][tile_index or
0xFFFFFFFF][x][y]` object records, encoding walls/doors/furniture/fixtures/
appliances layered on top of the floor. The old script never read this
section at all -- it wasn't that buildings write zero content, it's that
the parser was blind to the section where they live.

**Corrected parser re-run on `31_45`**: 837 distinct real building-category
tiles found (walls_exterior_house_*, fixtures_*, appliances_*, furniture_*,
ceilings_*, roofs_*) -- positive control passes, parser now trustworthy.

**Corrected parser re-run on OUR OWN exported `30_44` and `0_0`**: **both
show real building content** -- walls_exterior_house_01_1/16/17,
fixtures_doors_01_5, fixtures_doors_frames_01_2, fixtures_bathroom_01_0,
fixtures_counters_*, appliances_cooking_01_33/3, appliances_refrigeration_
01_30, appliances_television_01_0/2, furniture_seating_indoor_01_46,
furniture_tables_low_01_11, ceilings_01_0, roofs_01_54/36, location_
hospitality_sunstarmotel_* (this last one is odd/likely a base-map leftover,
not ours, but doesn't change the building-tile finding). **WorldEd's
Generate Lots DID correctly rasterize the building.** MAP-38ZJ and this
doc's earlier "REAL ROOT CAUSE FOUND" section, MAP-38ZK's original framing,
and this session's `feedback_verify_before_human_test.md` memory's headline
claim are all **retracted as based on a flawed audit script**, not a real
compiler defect.

**What actually explains v1/v2/v3's negative in-game results**: a genuine,
separate positional bug. The building's real wall tiles sit at local chunk
(17-20, 15-19) = local tile x=136-168, y=120-160 within the exported
256-tile cell -- NOT at x=5,y=5 as specified in the `.pzw`'s `<lot x="5"
y="5">`. The source `.tmx` is a 300-tile canvas; Generate Lots slices it
into the real 256-tile output grid with a translation offset this project
never accounted for. v1 (cell 0,0, unpopulated -- real, separate bug, stays
fixed), v2/v3 (256-vs-300 cell math -- real, separate bug, stays fixed)
were both correctly diagnosed; this local-offset bug is a THIRD, independent
issue, only now discovered because the audit script could finally see the
real content well enough to locate it.

**Corrected absolute target (v4)**: cell (30,44) origin (7680,11264) + local
center (152,140) = **(7832, 11404, 0)**. Mod re-staged with this coordinate
in `spawnpoints.lua`/`objects.lua`/`README_TEST_STATUS.txt`. Not yet
human-tested.

**Process lesson**: a from-scratch reverse-engineered binary parser needs a
positive control against known-good real data BEFORE its negative results
are trusted -- this should have been step one of MAP-38ZJ, not a correction
applied after three human test cycles. Recorded as its own binding lesson.

## CONFIRMED: real multi-room building rendering in-game (v4 SUCCESS)

Human test at the v4-corrected coordinate (near 7832,11404,0, character
ended up at 7822,11424,0 after walking to the wall): **a real, large,
multi-story building rendered** -- brick and stone exterior walls, glass
windows, a double door, matching the Apartment BS Trinitaire Possible
building's real geometry. Right-click Tile Report confirmed
`walls_exterior_house_01_1` (a real wall tile, not fallback/void/street).

**This is the first confirmed real multi-room building placed and rendered
in-game via WorldEd's own Generate Lots 8x8 (B42+ map) compiler in this
project's entire history** (every prior confirmed content type across
MAP-38K through MAP-38ZI was flat ground/vegetation/wall-texture-smear
content, never a real structure).

Status upgraded: PROVISIONAL -> CONFIRMED (single human test, one cell,
one building). Not yet Ratified per this repo's own doctrine (needs a
second independent confirmation and/or a differential control test before
being treated as fully settled), but this closes the open question this
whole MAP-38ZJ/38ZK thread was chasing.

**Full confirmed pipeline for real building placement (documented for
reuse/scaling to the other 34 Montreal buildings)**:
1. Author/clean the `.tbx` (strip empty `tile=""` placeholder entries --
   MAP-38ZG-era fix).
2. Place it in a WorldEd `.pzw` cell via manual drag from the Maps panel
   (creates `<lot x y level width height map="....tbx"/>` in the cell) --
   or presumably any other mechanism that populates the same XML.
3. Ensure the cell has a real base `.tmx` assigned (`map=""` cells are
   silently skipped).
4. Run `File > Generate Lots 8x8 (B42+ map)`, targeting a World Origin that
   resolves to a REAL vanilla-populated cell (checked directly against the
   installed game's own `media/maps/Muldraugh, KY/` folder -- do not trust
   the World-Origin-to-output-cell mapping, it's not a fixed formula).
5. Copy the resulting `.lotheader`/`.lotpack`/`chunkdata_*.bin` (2x2 block)
   into `<mod>/common/media/maps/<map_id>/`, alongside `map.info`
   (`lots=Muldraugh, KY`), `objects.lua`, `spawnpoints.lua`.
6. **Compute the teleport/spawn target correctly**: absolute tile =
   cell*256 (NOT *300, per MAP-38J) + the building's TRUE local position
   inside the exported cell -- which may NOT match the `.pzw` lot's
   specified local x/y, due to a 300-tile-source-canvas to 256-tile-
   output-grid slicing offset. Verify the true local position by byte-
   auditing the lotpack for the building's own wall/fixture tile names
   (see the corrected two-section chunk parser below) rather than trusting
   the `.pzw` coordinate directly.

**Corrected chunk parser (for any future audit of these files)**: each
non-default chunk = a 64-slot floor-tile stream (Type A `[0xFFFFFFFF][run_
length]` / Type B `[2][0xFFFFFFFF][tile_index]`, as before) FOLLOWED BY a
variable-length list of 16-byte `[tag=3][tile_index or 0xFFFFFFFF][x][y]`
object records extending to the end of the chunk's allocated byte length --
this second section is where walls/doors/furniture/fixtures/appliances
live. The original MAP-37B-era parser spec (used everywhere in this repo
until now) only ever documented the first section.

## Second building placed via the confirmed pipeline (church, cell 30,45)

Applied the confirmed pipeline to a second building (`Eglise Monk A Refine.tbx`,
pzw cell 0,1, which had the same `map=""`-cell-skip bug as the first building
until patched to `map="0_0.tmx"`). One process note: WorldEd re-saved its
stale in-memory state over the external file edit the first time this was
attempted -- fix is to Close (Ctrl+F4, no save prompt if the doc is already
"clean" from WorldEd's own perspective) then reopen via File > Open, not
just edit-on-disk while the project stays open.

WorldEd's own menus in this fork render as separate detached top-level
windows (title "PZWorldEd"), not overlay dropdowns within the parent
window -- worth knowing for any future GUI automation on this tool.

Selected the target cell directly in the World view (status bar's "Current
cell" readout confirms selection) and ran `Generate Lots 8x8 > Selected
Cells Only...` rather than "All Cells" this time -- same World Origin
(26,38) as before produced a different output cell this time: `30_45`
(entry_count 184, vs the 4-5-entry default companions), confirmed present
in the real vanilla map folder.

Byte-audit (corrected two-section parser) found real church content:
`carpentry_01_16`, `ceilings_01_0`, `floors_interior_carpet_01_*`,
`roofs_01_*`, `walls_interior_detailing_01_32`,
`location_community_church_small_01_*`. Wall/fixture chunks at local tile
x=184-232, y=120-160 -- again NOT the pzw's x=5,y=5, confirming the
300-to-256 slicing offset is a real, per-building, per-cell thing to check
each time, not a one-off fluke.

Target: cell 30,45 origin (7680,11520) + local center (208,140) =
**(7888, 11660, 0)**. Staged as `pzmapforge_worlded_realbuild_test002`.
Not yet human-tested.

## Church test v1 negative -- chunk-bounding-box center was too coarse

Human test at (7888,11660,0) [actual position ~7882,11649]: plain grass,
`floors_exterior_natural_01_0`. Investigated rather than re-guessing blind:
the tested point's chunk (row 16 locally) has a ceiling reference but no
interior floor override -- likely a roof overhang/eave, architecturally
"under the building" but not real walkable interior.

Root cause: the "bounding box of all wall/fixture-bearing chunks, take the
center" heuristic (used for both the apartment and this church) is too
coarse -- it can land in a real gap (courtyard, eave, disconnected
sub-structure) within that box, not necessarily inside the building.

Fix: re-scanned with a corrected whole-chunk parser (tag=2 explicit-tile
records read across the ENTIRE chunk, not stopped after an assumed 64-slot
floor stream) and isolated chunks containing `floors_interior_carpet_01_9`
specifically -- real walkable interior flooring, a much stronger "you can
stand here and Tile Report will show it" signal than "some wall tile
exists somewhere in this chunk's records". New target: local x=200-208,
y=144-152 -> absolute **(7884, 11668, 0)**. Not yet re-tested.

**Lesson for the remaining buildings**: don't use the wall-chunk bounding-
box center as the target. Use a chunk that specifically contains
FLOOR-layer interior content (floors_interior_*, floors_interior_carpet_*,
etc.) -- that's what Tile Report actually reads, and it's a much tighter,
more reliable localization than "near some wall reference".
