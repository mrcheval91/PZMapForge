# MAP-38W: Tile Category Results — Ground-Layer Blends Work, Trees/Groundcover Don't

Date: 2026-07-11
Status: CONFIRMED (human-observed, 5 tile names tested)

## Results so far, all via `--renderable-marker-tile`, same mechanism, same cell/coordinate

| Tile name | Result |
|---|---|
| `floors_rugs_01_0` | CONFIRMED — repeating carpet/rug pattern (MAP-38K) |
| `vegetation_foliage_01_8` | CONFIRMED — "vegetation full" (MAP-38S) |
| `blends_grassoverlays_01_0` | CONFIRMED — "long herbs, all around" (MAP-38W) |
| `vegetation_trees_01_0` | **VOID** — empty walkable space, character fully visible, not darkness, just nothing rendered |
| `vegetation_groundcover_01_0` | **VOID** — same as trees |

## Interpretation

Every successful tile is a **flat, tileable "blend"/"overlay"-style ground
texture** — the kind of thing meant to be painted directly onto the base
ground layer. `vegetation_trees_01_0` and `vegetation_groundcover_01_0`
render as void, not fallback content and not an error — consistent with
these being a **different tile category that this simple single-layer
lotpack mechanism cannot place**, most likely because real trees (and
possibly groundcover) are rendered as objects/sprites on a separate
"Vegetation" layer above the ground layer (matching TUT-03's own workflow:
paint on a Furniture layer, copy, paste onto a dedicated Vegetation layer —
implying vegetation-as-object needs a second layer this writer's lotpack
format has no concept of).

This is consistent, not contradictory, with MAP-38S: `vegetation_foliage_*`
tiles are apparently ground-layer-compatible blend textures despite the
"vegetation" name, while `vegetation_trees_*` and (at least index 0 of)
`vegetation_groundcover_*` are not.

## What this confirms about the writer's current ceiling

This repo's `renderable_v1` lotpack writer can place any real, textured,
**ground-layer blend/overlay tile** and have it render correctly. It
cannot place discrete objects (trees, and apparently some groundcover) —
that would need either a second data layer this writer doesn't emit, or
real per-tile object placement (distinct from the flat ground blend
records currently written), which is a materially bigger feature than a
tile-name swap.

## Claim boundary

ground_layer_blend_tile_placement_confirmed=true (3 tile names: floors_rugs,
vegetation_foliage, blends_grassoverlays)
tree_object_placement_confirmed=false (renders as void)
groundcover_placement_confirmed=false (index 0 renders as void; other
indices untested)
PLAYABLE_EXPORT_CLAIM_ALLOWED=false
CLAUDE_RAN_PZ=false
