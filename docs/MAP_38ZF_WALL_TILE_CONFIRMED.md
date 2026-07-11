# MAP-38ZF: Wall Tile Confirmed — Sixth Content Type, New Category

Date: 2026-07-11
Status: CONFIRMED (human-observed, Tile Report-verified)

## What was tested

`pzmapforge_map38zf` (cell 34_26), generated via
`--renderable-marker-tile walls_exterior_house_01_4` — the same safe
single-tile mechanism used for every prior confirmed content type (floor,
foliage, grass overlay, forest, atmospheric deep-forest), but this is the
first structural/wall-category tile tested (as opposed to ground blends
and vegetation).

## Result

**Confirmed via Tile Report** (first use of the newly-discovered debug
tool for a positive result, not just disambiguating a negative one):
`Tile Report: walls_exterior_house_01_4`, `Coordinates Report x: 8831,
y: 6782, z: 0`.

Visually: a large diagonal brick-wall-textured stripe across the screen,
with black (unrendered/occluded) area on either side, rather than a
coherent rectangular room.

## Interpretation

This is genuinely useful evidence and a real category expansion (walls,
not just ground/vegetation), but the visual result is not "a room" — it's
the wall sprite graphic being placed via the same flat ground-tile
position mechanism used for floor/vegetation tiles. Wall sprites in
isometric games are typically tall, offset graphics representing a
vertical surface, not flat tileable ground textures — placing one this way
likely produces exactly what was observed: an oversized, diagonal-looking
smear as adjacent copies of the same large sprite overlap, rather than a
proper wall segment with correct orientation/doors/corners.

**This does not mean walls "don't work"** — it means real room-building
would need actual wall-placement logic (likely a different, wall-specific
data structure, distinct from the flat ground `tile_index` records this
writer currently emits), not just referencing a wall tile name in the same
mechanism used for ground textures. This is analogous to MAP-38W's
original "trees render as void" finding turning out to be about needing
the right mechanism, not the right tile name — but for walls, the tile
name IS confirmed to have a real texture and DOES render, just not in a
structurally coherent way via this mechanism.

## What this confirms

- The lotpack ground-tile mechanism can reference **any** real,
  textured tile name across at least 3 broad categories now: ground
  blends/overlays, vegetation/trees, and wall textures. No content-type
  gate at the rendering level (as MAP-38ZA already showed for trees).
- Getting an actual **coherent room** (proper wall orientation, corners,
  doors) is a materially bigger feature than a tile-name swap — it would
  need real wall/object placement logic this writer doesn't have, similar
  in scope to the object/tree-layer question MAP-38W originally (and
  wrongly) suspected for vegetation.

## Claim boundary

wall_tile_render_confirmed=true (Tile Report-verified, 2026-07-11)
coherent_room_structure_confirmed=false (wall texture renders but not as
a structurally correct room)
PLAYABLE_EXPORT_CLAIM_ALLOWED=false
CLAUDE_RAN_PZ=false
