# DeadMTL System 2 - Static Roads and Alleys Overlay

## Why System 2 is needed

MAP-22B runtime discovery probe (2026-06-14) enumerated all keys in worldgen.prefabs
in PZ Build 42. Only two prefab keys were found:
- highway_NS_00 (VISUAL_CONFIRMED)
- normal_road_WE_00 (VISUAL_CONFIRMED)

No alley, ruelle, local street, service lane, parking access, pedestrian cut,
road turn, intersection, or dead-end prefab key exists in worldgen.prefabs.

MAP-22C confirmed: WorldGen cannot support Montreal small roads, alleys, or ruelles.
The WorldGen small-road path is closed.

WorldGen (System 1) remains responsible for:
- broad terrain placement (water, forest, grass)
- coarse road corridors (highway_NS_00, normal_road_WE_00)

System 2 will eventually be responsible for:
- local streets, alleys, and ruelles
- service lanes and parking access
- pedestrian cuts and paths
- turns, intersections, dead ends
- sidewalks, curbs, asphalt markings, and lane markings
- crosswalks and stop markings

## What System 2 means

System 2 is a future static tile overlay layer. When implemented, it will write
detailed tile placement and intent data for road types that WorldGen cannot represent.

System 2 is:
- not currently runtime-proven
- not yet implemented (no .lotpack writer exists)
- a planning and authoring contract for future development

System 2 is NOT:
- a WorldGen layer
- a biome or prefab palette
- a playable map output
- a Workshop-ready product

## Road classes

### local_street
- Authoring width: 5-7 px
- Montreal examples: standard residential streets
- Status: SYSTEM_2_REQUIRED

### alley_ruelle
- Authoring width: 3-4 px
- Montreal examples: back alleys (ruelles), service alleys between residential blocks
- Status: SYSTEM_2_REQUIRED
- Note: ruelles are a defining feature of the Montreal urban fabric. At 1 px = 1 m they
  are raster-representable but require static tile overlay, not WorldGen biomes.

### service_lane
- Authoring width: 2-3 px
- Montreal examples: service access, loading dock access
- Status: SYSTEM_2_REQUIRED

### parking_access
- Authoring width: 3-5 px
- Montreal examples: parking lot driveways, garage ramps
- Status: SYSTEM_2_REQUIRED

### pedestrian_cut
- Authoring width: 1-2 px
- Montreal examples: escaliers, narrow passage cuts between buildings
- Status: SYSTEM_2_REQUIRED

### intersection
- Authoring approach: explicit node pixels at road crossing points
- Status: SYSTEM_2_REQUIRED
- Note: WorldGen strip prefabs cannot represent intersections. When WE and NS strips cross
  in a WorldGen layer, only one prefab key can occupy a pixel. System 2 must handle
  intersection geometry explicitly.

### road_turn
- Authoring approach: curved pixel regions or single turn-node pixels
- Status: SYSTEM_2_REQUIRED

### dead_end
- Authoring approach: terminal node pixel at the end of a road segment
- Status: SYSTEM_2_REQUIRED

## Authoring scale

PZMapForge authoring uses 1 px = 1 meter = 1 PZ world tile.

At this scale:
- A 3-4 m Montreal ruelle is 3-4 pixels wide
- A 5-7 m local street is 5-7 pixels wide
- An intersection node can be a single pixel

## Filled-width raster masks vs centerline vectors

Two authoring modes are possible:

Filled-width: paint the full road width as a solid pixel region.
- Immediate raster representation
- Dimensions directly match pixel counts
- No compiler support needed for width interpretation
- Preferred until a centerline compiler exists

Centerline vector: paint a 1-px centerline; width is a style property applied at compile time.
- More compact to author
- Requires compiler support for width expansion
- Not available yet

Filled-width is the default authoring approach for now.

## Why intersections must be explicit

WorldGen strip prefabs produce infinite directional strips. At a crossing of two strips,
both strips fight for the same pixel. WorldGen has no intersection primitive.

System 2 must:
- Paint explicit intersection-node pixels at every road crossing
- Define a tile intent for each node type (T-junction, 4-way, offset crossing)
- Not rely on strip overlap to produce correct intersection geometry

## Why alleys should not be terrain biomes

WorldGen biome keys (water, grass_plain, birch_forest, etc.) produce terrain surfaces,
not road surfaces. Painting an alley as a biome would produce:
- grass or dirt terrain, not asphalt
- walkable terrain without road-surface behavioral flags
- no alignment with PZ road navigation

Alleys must use System 2 static tile overlay to produce correct road surfaces.

## Why sidewalks, curbs, and asphalt markings belong to System 2

These features are sub-tile details that require static tile placement:
- Sidewalks: typically 1-2 tile strips alongside roads
- Curbs: edge tiles at road boundaries
- Lane markings: overlay sprites on road surface tiles
- Crosswalks: overlay pattern tiles at intersections

None of these are representable as WorldGen biomes or prefabs. All require System 2.

## Claim boundary

- No .lotpack writer exists or is planned in this task.
- No runtime proof is claimed for any System 2 layer.
- No public playable map output is claimed.
- This document is a planning and authoring contract only.
- System 2 layers are placeholders until a static tile overlay writer is implemented
  and runtime-verified with human visual confirmation.

Do not mark any System 2 feature as runtime-proven without:
1. A working static tile overlay writer (not yet implemented)
2. A proof board compiled and installed to the PZ game folder
3. Human visual confirmation in a running PZ session
4. A documented proof record
