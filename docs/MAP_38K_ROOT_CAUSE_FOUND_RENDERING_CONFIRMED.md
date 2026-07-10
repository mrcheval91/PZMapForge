# MAP-38K: Root Cause Found — Renderable Content Confirmed On This Install

Date: 2026-07-10
Status: CONFIRMED (human-observed, coordinate-verified)
PZ Build tested: 42.19.0 (revision 1aa820d7bb66c4e55513cae04022bdacdac5b34e)

This document **supersedes MAP-38I's conclusion**. MAP-38I found that even
the community reference sample (`MyMapMod`) failed to render on this install
and concluded the root cause was upstream and unconfirmed, recommending no
further writer changes. That test's own coordinate was wrong, for the same
reason every previous test in this session (and likely this entire
repo's MAP-6A-onward lineage) was wrong. The root cause is now identified,
fixed, and confirmed working.

## The actual root cause

**Build 42 map cells are 256x256 tiles, not Build 41's 300x300.** Confirmed
via `pzwiki.net/wiki/Mapping`, which gives a concrete worked example: a
Build 41 cell at (31,23) [300-tile grid] exports to six Build 42 cells,
(36,26) through (37,28) [256-tile grid] — the wiki explicitly states mappers
still author at 300x300 but the tools export at 256x256, leaving empty
border tiles.

This repo's entire coordinate math — every `SpawnPoint=` INI value, every
`spawnpoints.lua` `worldX`/`worldY`, every `objects.lua` absolute
coordinate, across MAP-9K/9L/9Q/37D/37E/38A/38B/38C/38D/38G/38H/38I and this
session's own early tests — computed spawn targets as `cellX*300+offset`.
Since the lotheader/lotpack/chunkdata FILES are named/addressed on the real
256-tile grid, every test placed the player **outside the footprint of the
very file being tested**, landing on whatever real, unrelated vanilla
terrain happened to exist at the miscalculated coordinate. This explains
every single observation this session and prior sessions recorded: the
"dirt road," "electrical pylon," "pine forest" were all real content at
wrong locations, not evidence about our files at all.

## How this was found

A positive control (MyMapMod, MAP-38I) was retested with the coordinate
corrected: instead of `31*300+150, 45*300+150` = (9450,13650), the true
center of cell 31_45's real 256-tile range (`31*256+128, 45*256+128` =
(8064,11648)) was used. Operator confirmed landing inside a real building,
then found a clear patch of flat grass with zero procedural shrubs/trees at
a nearby coordinate (8143,11737) — the exact signature of authored terrain
actually being read (Build 42's procedural fallback never places buildings
or produces vegetation-free patches).

## The fix, applied to `renderable_v1`

`src/PZMapForge.Cli/Program.cs`:
- `spawnpoints.lua`'s `worldX`/`worldY`/`posX`/`posY` (which the engine
  still interprets via the legacy 300-based formula) are now computed by
  first finding the true absolute target (`cellX*256+128, cellY*256+128`)
  and converting that BACK into legacy form
  (`worldX = target/300, posX = target%300`), so the two coordinate systems
  agree instead of conflating a 256-addressed cell index with a 300-based
  spawn formula.
- `objects.lua`'s `SpawnPoint` object `x`/`y` now uses the same true
  absolute target directly (`cellX*256+128, cellY*256+128`).
- This fix is scoped to `renderable_v1` only (`isRenderableV1` branch) —
  other profiles (`empty_grass_v0`-`v5`) are unchanged, out of scope for
  this correction.
- 22 existing CLI process tests still pass unmodified (none hardcoded a
  specific coordinate value).

## Confirmed result

Candidate `pzmapforge_map38j` (renderable_v1, cell 34_26, corrected
coordinate) installed and tested by the operator. On-screen coordinate:
(8835, 6785) — matches the computed target (8832, 6784) within normal
in-game movement tolerance. Observed: **"endless repeating street or
indoor tile"** — the real `floors_rugs_01_0` marker tile (MAP-38D),
rendering as an unmistakably artificial, non-procedural, repeating floor
pattern. This is the first time in this session (and, per the doctrine
trio's own history, arguably the first time since the original unrepeatable
2026-07-08 hand-crafted test) that this repo's own committed C# writer has
been confirmed producing visibly distinct, non-fallback terrain.

## What this means for every other MAP-3x/9x test in this repo's history

Any prior test that used `worldX=cellX, worldY=cellY` directly (the
pre-MAP-38J convention — this covers essentially every runtime test this
repo has ever recorded, including MAP-9K/9L/9Q/37D/37E and the original
MAP-38A test itself, since MAP-38A's hand-crafted Python scaffolding is not
in this repo and its exact coordinate handling is unknown) placed the
player at the WRONG absolute location relative to the file being tested.
Every `FALLBACK_INDISTINGUISHABLE` result recorded before this fix
(MAP-37E, MAP-38C, MAP-38I) should be treated as **uninterpreted, not
disproven** — they may simply have never tested the actual target cell.

## Recommended next step

1. **Ratify with a differential control test** (MAP-38B-style Run
   A/Run B) using the corrected coordinate math, to convert this single
   confirmation into repeatable, automated-adjacent evidence before
   promoting `renderable_v1` past PROVISIONAL.
2. Consider re-running MAP-37E's original differential packet with
   corrected coordinates — it may also have been testing the wrong
   location the whole time.
3. Chunkdata semantics (MAP-38C's original suspect) and mixed-encoding
   percentage tuning (MAP-38H's central-block heuristic) remain open, but
   are now testable against a working baseline instead of blind guessing.

## Claim boundary

PLAYABLE_EXPORT_CLAIM_ALLOWED=false (single human confirmation, not yet
differential-control-tested with corrected coordinates)
RENDERABLE_MOUNT_SUCCESS=true (human-observed, coordinate-verified,
2026-07-10)
root_cause_identified=true
root_cause=build42_uses_256_tile_cells_not_300_tile_legacy_coordinate_math
CLAUDE_RAN_PZ=false (operator installed, launched, and observed; coordinate
and visual result both independently reported by operator)
