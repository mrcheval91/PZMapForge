# MAP-38ZJ: Real WorldEd `Generate Lots` output — negative in-game result, root cause matches MAP-38A's known "populated cell" requirement

## Context

Everything documented in MAP-6 through MAP-38ZI used PZMapForge's own hand-rolled/
reverse-engineered lotpack writer (`Build42CandidateWriterCommand` and friends) —
byte formats guessed from Workshop mod archaeology, never the real PZ mapping
tools. This entry is the first test using the **actual WorldEd Unofficial Fork's
own `File > Generate Lots 8x8 (B42+ map)` compiler**, fed a real building `.tbx`
(from the 35-file Montreal building set) and a real base-map `.tmx`.

## What was done

1. Reverse-engineered (via manual-drag + `.pzw` diff, since synthetic GUI
   drag-and-drop into WorldEd's Maps panel never worked — see session transcript)
   the plain-XML `<cell><lot .../></cell>` placement format WorldEd itself writes:
   ```xml
   <cell x="0" y="0" map="0_0.tmx">
    <lot x="165" y="127" level="0" width="33" height="21"
         map="../../../assets/Building to use/Buildings to Use - Cleaned/Apartment/Apartment BS Trinitaire Possible.tbx"/>
   </cell>
   ```
2. Confirmed cells with `map=""` (no base terrain tmx) are silently skipped by
   the lot compiler entirely (0 buildings processed until fixed).
3. Ran `File > Generate Lots 8x8 (B42+ map) > All Cells...` in WorldEd
   (`E:\Omni\Zomboid\scratch\worlded-canal-garage-cell\source\canal-garage-cell.pzw`,
   world grid expanded to 6x6, World Origin left at 0,0). WorldEd's own success
   dialog reported: **Buildings: 1, Rooms: 72, Room rects: 82, Room objects: 104.**
4. Real `.lotheader`/`.lotpack`/`chunkdata_*.bin` files were written to
   `E:\Omni\Zomboid\scratch\worlded-canal-garage-cell\export\` for cells
   (0,0), (0,1), (1,0), (1,1).
5. Staged a mod (`pzmapforge_worlded_realbuild_test001`) following the exact
   folder/file structure MAP-38A already validated (`<mod>/common/media/maps/<mapId>/`,
   `map.info` with `lots=Muldraugh, KY`, `objects.lua`, `spawnpoints.lua`).
6. **Human load test, fresh save** (not a cache artifact — new game, new
   character, never visited before): teleported via debug menu to absolute
   world tile (180,135,0) — dead center of the placed building's footprint
   (local lot spans x=165-198, y=127-148 within cell 0,0).

## Result: negative, but informative

Right-click Tile Report at (180,135,0) reported `floors_exterior_street_01_0`
— an **exterior street tile from the base `.tmx`**, not interior flooring, not
a wall, not any building-derived tile. The building did not render at all.
Terrain did render correctly (proving the mod's base map layer loads fine).

## Byte-level side investigation (inconclusive — do not reuse this method alone)

Parsed `world_0_0.lotpack` per the MAP-38A format spec (`LOTP` magic, version=1,
chunk_count=1024, u64 offset table, 32x32 chunk grid). Chunks covering the
building footprint (e.g. chunk (20,15)) all begin with marker `02000000`.
**Control chunks far from any building** (e.g. (0,0), (2,2), (31,0)) show the
*same* `02000000` marker — this is a generic per-chunk constant (matching
MAP-38ZG's `field1=2` hypothesis, seen in every file this project has ever
produced), **not evidence of building-specific content**. This line of
investigation was dropped as inconclusive without a real confirmed-working
Workshop lotpack to diff chunk structure against (not yet done).

## Root cause (near-certain): empty/unpopulated cell

MAP-38A already documented this exact failure mode under "3. Cell-choice
mistake": `lots=Muldraugh, KY` mods can only **overlay cells the base vanilla
map already has** — they cannot conjure a buildable cell at a coordinate
vanilla Muldraugh doesn't populate. Cell (12,12) was confirmed absent from
all 4065 real populated Muldraugh cells and never rendered across multiple
format-correct attempts. Cell **(0,0)** — used in this test — is Muldraugh's
far-NW corner and is, with near certainty, equally unpopulated (ocean/void).
None of this project's cells (WorldEd `.pzw` grid 0-5 x 0-5) are anywhere
near the real populated Muldraugh coordinate range (~29-35 per known-good
samples: MyMapMod's 31/32,45/46; pzmapforge_map38zf's 34,26; MAP-38A's
own 35,27).

**This was never tested with WorldEd's own compiler until now** — MAP-38A's
"populated cell" finding was established using the synthetic writer. This
entry extends that finding to real WorldEd output: same likely failure mode,
independent confirmation path.

## Fix identified, not yet tested

WorldEd's `Generate Lots` dialog has a **World Origin X/Y** field (tooltip:
"The world origin affects the names of generated files") — this almost
certainly remaps the `.pzw`'s internal 0-based cell grid onto real Muldraugh
coordinates at export time, without needing to renumber cells inside the
`.pzw` itself. Next step: set World Origin to a known-populated coordinate
(e.g. 31,45 — the same cell MyMapMod's sample already uses, so populated
status is certain) and re-run Generate Lots, re-stage the mod with the
renamed output (`31_45.lotheader`, `world_31_45.lotpack`, etc.), and retest
at absolute world tile (31*300+165, 45*300+127, 0) = (9465, 13627, 0).

## Status

PROVISIONAL. Negative result explained by a known, already-documented root
cause (unpopulated cell), not yet re-tested with the fix applied. Do not
treat the WorldEd `Generate Lots` pipeline itself as broken based on this
result alone — the compile succeeded (72 rooms reported) and the failure
point is plausibly just cell selection, which is a data/coordinate choice,
not a format bug.
