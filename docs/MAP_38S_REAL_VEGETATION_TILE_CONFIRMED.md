# MAP-38S: Real Vegetation Tile Confirmed Rendering

Date: 2026-07-10
Status: CONFIRMED (human-observed, coordinate-verified)

## What was tested

`pzmapforge_map38r` (cell 34_26), generated via the new
`--renderable-marker-tile vegetation_foliage_01_8` CLI override (MAP-38R),
using the exact same confirmed lotpack mechanism as MAP-38K/L/P/Q (central
16x16 chunk block, explicit Type-B tile records) — just referencing a real
vegetation tile name instead of a floor tile.

## Result

Operator observed **"vegetation full"** — real, dense vegetation covering
the marker-tile area, at the same confirmed coordinate (8832, 6784).

## Why this matters

This is the second confirmed content type through this repo's writer (the
first being the `floors_rugs_01_0` floor tile, MAP-38K), and it's much
closer to MAP-38A's original goal: authored, non-procedural, thematically
plausible terrain content, not just an arbitrary distinctive marker.

It also retroactively validates the community-sourced tile list (TUT-03 in
`docs/B42_MAPPING_DISCORD_TUTORIALS.md`: `vegetation_foliage_01_*`,
`blends_grassoverlays_01_*`, `e_americanholly_01`/`e_canadianhemlock_01`
first 4 trees) as directly applicable — even though that tutorial describes
a Vegetation+Furniture layer combo for upper-level (building interior)
placement, the same tile name works via a simple ground-level explicit
lotpack record too.

This also confirms MAP-38O/P's `Vegitation` zone-object negative results
were the wrong mechanism, not evidence that vegetation can't be placed at
all — real vegetation tiles work fine through the lotpack, the same way
floor tiles do.

## What's still open

- Only one vegetation tile name tested (`vegetation_foliage_01_8`). The
  community list has several more candidates
  (`blends_grassoverlays_01_*`, `e_americanholly_01`/`e_canadianhemlock_01`
  first 4 trees) — untested but likely to work given the mechanism is now
  confirmed general-purpose.
- Still only ground-level placement confirmed. The tutorial's
  Vegetation+Furniture layer combo for upper levels (building interiors)
  is a different, untested mechanism (this repo's writer has no concept of
  building levels/floors yet).
- chunkdata's semantics remain undecoded (MAP-38M); confirmed inert for
  every test run so far (MAP-38Q).

## Claim boundary

vegetation_tile_render_confirmed=true (human-observed, coordinate-verified,
2026-07-10)
PLAYABLE_EXPORT_CLAIM_ALLOWED=false
CLAUDE_RAN_PZ=false
