# MAP-38ZC: Multi-Tile-Type Boundary Crashes Blending.changeGround

Date: 2026-07-11
Status: CONFIRMED CRASH (human-observed, real engine exception)

## What was tested

`pzmapforge_map38zb` (MAP-38ZB's palette showcase), dividing the central
16x16 chunk block into four quadrants (four different explicit tile types
meeting at the spawn point) plus a fifth region south of the block.

## Result

Game crashed during world load. Operator reported a red error panel and an
empty world with no character (loading never completed). Debug log shows:

```
java.lang.NullPointerException: Cannot read field "sprite" because the
return value of "zombie.iso.IsoGridSquare.getFloor()" is null at
Blending.changeGround(Blending.java:120).

zombie.iso.worldgen.blending.Blending.changeGround(Blending.java:120)
zombie.iso.worldgen.blending.Blending.applyBlending(Blending.java:56)
zombie.iso.IsoChunk.update(IsoChunk.java:3112)
zombie.iso.IsoChunk.doLoadGridsquare(IsoChunk.java:3862)
zombie.iso.IsoChunkMap.processAllLoadGridSquare(IsoChunkMap.java:174)
zombie.gameStates.IngameState.enter(IngameState.java:740)
```

## Interpretation

The crash occurs inside the game's own automatic ground-blending system,
triggered during chunk load (`IngameState.enter` →
`processAllLoadGridSquare` → `IsoChunk.doLoadGridsquare`) — this runs for
every chunk on load, not just when a player walks near a boundary, so any
mod with this structure will crash on load, every time.

Every prior single-tile candidate (MAP-38K through MAP-38ZA) used ONE
explicit tile type in the central block surrounded by Type-A default —
exactly one boundary "shape" (marker ↔ default), and never crashed.
MAP-38ZB introduced FOUR different explicit tile types meeting at internal
seams (quadrant boundaries) plus a fifth separate region — multiple
different-tile-to-different-tile boundaries, and it crashed.

**Leading hypothesis: adjacent grid squares with different explicit floor
tiles need blend-transition data this writer doesn't provide**, and the
vanilla blending system assumes that data exists (crashing with a null
floor reference rather than degrading gracefully when it's absent). This
is plausibly connected to chunkdata's still-undecoded structure (MAP-38M) —
chunkdata may be exactly the blend-transition data the engine expects at
tile-type boundaries, which this writer has always emitted as all-zero
(MAP-38Q found all-zero vs. a real complex swap made no *visible* rendering
difference for a single-tile-type region, but that test never had adjacent
*different* tile types to blend in the first place — MAP-38Q and this
crash are not necessarily in tension).

## What this means for MAP-38ZB going forward

**Do not use `--renderable-palette` (or any candidate with multiple
different explicit tile types adjacent to each other) until this is
understood.** Single-tile-type regions (any one real tile, uniform across
the whole marked block) remain confirmed safe — every MAP-38K through
MAP-38ZA candidate used exactly this shape and none crashed.

## Recommended next step (if resumed)

- Test whether *any* two-different-tiles-adjacent boundary crashes, or
  only specific combinations — e.g. try just two quadrants (not four) to
  see if the crash needs 3+ distinct boundaries or triggers with just one.
- Test whether a buffer/gap of Type-A default tiles *between* different
  explicit regions (instead of them touching directly) avoids the crash —
  if so, `--renderable-palette` could be fixed by spacing regions apart
  rather than needing chunkdata's real semantics.
- If chunkdata really does hold blend-transition data, this is the
  strongest lead yet for actually needing to decode it, since a real
  multi-content map is otherwise impossible with this writer.

## Claim boundary

multi_tile_boundary_crash_confirmed=true
single_tile_type_regions_confirmed_safe=true (all MAP-38K-ZA candidates)
PLAYABLE_EXPORT_CLAIM_ALLOWED=false
CLAUDE_RAN_PZ=false
