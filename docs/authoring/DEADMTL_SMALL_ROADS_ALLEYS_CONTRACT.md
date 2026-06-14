# DeadMTL Small Roads and Alleys Authoring Contract

## Current WorldGen road capability

### Confirmed WorldGen prefab keys (VISUAL_CONFIRMED)

Only two prefab keys exist in `WorldGenRegistry.cs` and are runtime-proven:

| Key | Direction | Status | MAP ref |
|---|---|---|---|
| `normal_road_WE_00` | West-East strip | VISUAL_CONFIRMED | MAP-20A, MAP-21A, MAP-21B |
| `highway_NS_00` | North-South strip | VISUAL_CONFIRMED | MAP-13C/15C/16B, MAP-21A, MAP-21B |

### What these keys can represent

Both confirmed keys produce infinite directional strips: one WE, one NS. They are suitable
for straight arterials and one-directional highways only. They do not support:

- North-South normal roads
- West-East highways
- Turns or curves
- Intersections (T-junctions, 4-way crossings)
- Local streets, collector roads, alleys, or service lanes
- Pedestrian paths

Any road network beyond simple perpendicular strips requires either more WorldGen prefab
keys or System 2 static tile overlay.

### What cannot currently be claimed

The following are NOT WorldGen-proven as of MAP-21B:

- Any road type other than `normal_road_WE_00` and `highway_NS_00`
- Turn or intersection geometry in WorldGen
- Local street, alley, ruelle, service lane, or parking access at WorldGen layer
- A connected road network at any scale

Do not claim these as WorldGen-capable without a documented runtime proof step.

---

## Road hierarchy at 1 px = 1 m

PZMapForge authoring uses 1 pixel = 1 meter = 1 PZ tile. At this scale, Montreal road
widths are directly usable as pixel widths.

### Road classes and authoring widths

| Class | Authoring width (px) | Montreal examples |
|---|---|---|
| highway | 10-14 px | A-10, A-15, A-20, A-40 |
| arterial_road | 8-10 px | Sherbrooke, Ste-Catherine, St-Denis, Papineau |
| collector_road | 7-8 px | Rachel, Laurier, Mont-Royal, Beaubien |
| local_street | 5-7 px | Standard residential streets |
| alley_ruelle | 3-4 px | Montreal back-alleys (ruelles) |
| service_lane | 2-3 px | Service access, loading docks |
| parking_access | 3-5 px | Parking lot driveways |
| pedestrian_cut | 1-2 px | Passageways, escaliers |

These widths represent the drivable or walkable surface, not including sidewalks.

### Scale rationale

At 1 px = 1 m, a Montreal ruelle (typically 3-4 m wide) maps to a 3-4 pixel feature.
This is viable as a raster feature. At coarser scales (e.g. 1 px = 10 m), ruelles
would be sub-pixel and could not be represented faithfully.

### Authoring approach

**Road centerline vs filled-width:** Two authoring modes are possible.

- Centerline authoring: paint a 1-px line at the road centerline. Width is a style
  property applied at compile time. Simpler to author but requires compiler support.
- Filled-width authoring: paint the full road width as a solid pixel region. Immediate
  raster representation. More verbose but compiler-independent.

Filled-width is the safer approach until a centerline compiler is available.

### Intersections and turns

WE/NS strip prefabs cannot represent turns or intersections. At an intersection where
WE and NS strips cross, the pixels overlap and only one prefab key can occupy a pixel.
WorldGen does not have intersection or turn prefab keys in the current registry.

Options:
1. Discover additional prefab keys via runtime Lua dump (MAP-22B)
2. Use System 2 static tile overlay for intersection geometry
3. Leave intersections unresolved in WorldGen and paint them in System 2

Option 1 must be verified before claiming intersection support. Options 2 and 3 are
the safe conservative choices.

---

## Layer contract

See `examples/deadmtl-layer-pack/road-layers-contract.json` for the machine-readable
layer contract. Summary:

| Layer | Status | Width (px) |
|---|---|---|
| roads_highway | WORLDGEN_LIMITED | 10-14 |
| roads_major | WORLDGEN_LIMITED | 8-10 |
| roads_local | SYSTEM_2_REQUIRED | 5-7 |
| roads_alleys | SYSTEM_2_REQUIRED | 3-4 |
| roads_service | SYSTEM_2_REQUIRED | 2-5 |

**WORLDGEN_LIMITED**: Compiler accepts a subset of this road type. Full coverage
(turns, intersections, opposite direction) requires more prefab discovery.

**SYSTEM_2_REQUIRED**: This road type cannot be authored in WorldGen with current
known prefab keys. Requires System 2 static tile overlay or new prefab discovery.

---

## Why alleys require System 2

Montreal ruelles (3-4 m wide) are a critical feature of the Montreal urban fabric.
At 1 px = 1 m they are paintable as raster features. However:

- No alley or ruelle prefab key exists in `WorldGenRegistry.cs`.
- WorldGen biome keys produce terrain, not road surfaces. Using a biome to represent
  an alley would produce terrain behavior (e.g. grass or sand), not walkable road.
- WorldGen prefab keys produce directional road strips. No alley-specific strip key
  is proven.

Until a dedicated alley prefab is discovered at runtime (MAP-22B) or System 2 static
tile overlay is implemented, alleys must be marked SYSTEM_2_REQUIRED.

Do not paint alleys into a WorldGen layer and claim they are functional roads.

---

## Future proof tasks

### MAP-22B: WorldGen prefab key discovery

Dump all available keys from `worldgen.prefabs` and `worldgen.biomes` in a live PZ
runtime session. The goal is to determine whether additional road prefab keys exist
beyond the two currently proven.

Method: write a debug Lua script that iterates `worldgen.prefabs` and logs all keys.
Load in PZ and capture log output. Document each key found.

Do not add any key to WorldGenRegistry without runtime proof.

### MAP-22C: Road prefab proof board (conditional)

If MAP-22B discovers additional road prefab keys, build a proof board similar to MAP-21B
but for road types: NS normal road, WE highway, turn types, intersection types.

Prove each key with a dedicated swatch and human visual confirmation before marking
any key VISUAL_CONFIRMED.

### MAP-22D: System 2 static road overlay proof

Implement System 2 static tile overlay for local streets, ruelles, and service lanes.
This is independent of WorldGen and requires a tile-layer authoring pipeline.

Scope: define the tile-layer format, paint a local street grid in a proof area, verify
that the static tiles load correctly in PZ.

This task is explicitly deferred. Do not conflate System 2 work with WorldGen road
proof tasks.

---

## Claim boundary

- Only `normal_road_WE_00` and `highway_NS_00` are WorldGen runtime-proven.
- No other road type is compiler-supported or runtime-proven.
- Local streets and alleys are SYSTEM_2_REQUIRED until further prefab discovery.
- This document is a planning contract only. No runtime verification is claimed.
- No public mod packaging is claimed.
