# MAP-38ZA: Real Trees Confirmed — Index 0 Was Blank, Not the Whole Category

Date: 2026-07-11
Status: CONFIRMED (human-observed, two tile indices, dramatic result)

## What was tested

MAP-38W found `vegetation_trees_01_0` renders as void (empty walkable
space). Hypothesis: index 0 specifically is a blank/reserved frame in that
tileset, not that the whole `vegetation_trees_01_*` family is incompatible
with the lotpack ground-tile mechanism. Tested two other real indices via
`--renderable-marker-tile`:

- `vegetation_trees_01_8`
- `vegetation_trees_01_24`

Same cell (34_26), same coordinate (8832, 6784), same confirmed lotpack
mechanism as every prior test.

## Results

**`vegetation_trees_01_8`**: "mixed trees with zones of dense evergreens,
and the grass with like a transition dotting scattered" — a genuine,
varied forest render: mixed tree types, denser evergreen clusters, a
scattered transition zone into grass. This is real, object-like tree
content, not a flat blend texture.

**`vegetation_trees_01_24`**: "dark foggy forest, with zombies in it! and
fog stays but empty grass outside the 256 square" — a full atmospheric
deep-forest effect: fog, and zombie spawns, tightly bounded to the
authored cell (fog/zombies stop exactly at the cell boundary, plain grass
immediately outside it).

## Conclusion

**Index 0 was the anomaly, not the rule.** `vegetation_trees_01_*` tiles
render as real trees, and at least one index (`_24`) appears to trigger
substantially more than a static sprite — a full atmospheric zone
(fog + hostile spawns) — from a single ground-tile reference, no
`objects.lua` zone object involved at all. This directly contradicts
MAP-38W's "trees need a separate object/Vegetation layer" hypothesis: they
don't. They just needed a valid index.

`_24`'s effect (fog, zombies) is unusually rich for what should be a single
flat tile record. Two plausible explanations, neither confirmed:
1. This specific tile index is itself tagged with room/atmosphere metadata
   in the vanilla tile definitions (some tiles carry more than visual data
   — ambient effects, spawn hints).
2. The dense repeated placement (the whole central 16x16 chunk block,
   MAP-38H, uses this single tile uniformly) happens to satisfy whatever
   density threshold triggers the game's own atmospheric/spawn logic for
   forest areas — i.e., a lot of the same tree tile in one place reads to
   the engine as "this is a real forest," with matching consequences.

Either way: **this is a strong, confirmed result.** Real vegetation is not
just possible but can trigger genuinely game-relevant behavior (zombie
spawns), not just visual dressing.

## Recommended follow-up

- Test intermediate indices (not yet tried: 1,2,3,9-17) to map out which
  specific frames are blank vs which produce trees vs which (if any others)
  produce the fog/zombie effect, to understand if `_24` is special or if
  any sufficiently-forested tile triggers it.
- Consider whether the fog/zombie effect is desirable or needs to be
  avoided depending on what a future candidate is meant to represent.
- Given zone objects (`Vegitation`, `DeepForest`, 3 attempts) are now
  confirmed fully inert, and this tile-based approach clearly works, all
  future "add content" work should target the lotpack tile mechanism, not
  `objects.lua` zones (aside from `SpawnPoint`, the one confirmed-working
  object type).

## Claim boundary

tree_render_confirmed=true (2 indices, both human-observed 2026-07-11)
atmospheric_zone_effect_confirmed=true (fog + zombie spawns, index 24,
bounded to authored cell)
PLAYABLE_EXPORT_CLAIM_ALLOWED=false
CLAUDE_RAN_PZ=false
