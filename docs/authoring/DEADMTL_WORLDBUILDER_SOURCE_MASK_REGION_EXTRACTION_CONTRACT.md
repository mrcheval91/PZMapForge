# MAP-25J: DeadMTL WorldBuilder Source Mask Region Extraction Contract

**CONTRACT ONLY. Source mask regions only. No concrete geometry created. Not writer-ready. Not runtime-proven.**

---

## What This Is

The source mask region extraction contract reads the raw `map_00.png` color map, counts
pixels by color, computes pixel-space bounding boxes, and binds each color region to its
corresponding WorldBuilder zone metadata entry.

This produces source mask region records — the direct pixel-space inputs for future
geometry generation. These are NOT concrete lot polygons, road geometries, building slots,
sidewalk geometry, or fence geometry.

No terrain is generated. No lot subdivision occurs. No sidewalk geometry is created.
No road geometry is created. No building placement occurs. No fences are generated.
No lotpack writing occurs. No worldgen override file is written. No concrete geometry is
created. Layout is not materialized. No runtime proof. Not writer-ready.

---

## Input Chain (4 inputs)

| Input | Source | File |
|-------|--------|------|
| Source PNG | Raw authoring asset | E:\Omni\Zomboid\assets\raw\map_00.png |
| Zone metadata | MAP-25B | map_00.zone_metadata.json |
| Geometry primitive schema | MAP-25I | map_00.geometry_primitive_schema.json |
| Concrete geometry preflight | MAP-25H | map_00.concrete_geometry_preflight.json |

---

## Source PNG Contract

| Field | Value |
|-------|-------|
| tile_id | map_00 |
| source_png_width_px | 256 |
| source_png_height_px | 256 |
| source_pixel_count | 65536 |
| authoring_scale | 1_PIXEL_EQUALS_1_WORLD_TILE |
| pixel_origin | TOP_LEFT |
| x_axis | EAST_POSITIVE |
| y_axis | SOUTH_POSITIVE |
| coordinate_units | SOURCE_PIXELS |
| bounds_are_pixel_bounds_not_geometry | true |

---

## 7 Mask Region Records (map_00)

| Color | Role | Zone Type / Street Class | Pixel Count |
|-------|------|--------------------------|-------------|
| #7200FF | ZONE | RESIDENTIAL | 36740 |
| #FF6600 | STREET_CORRIDOR | MAIN_ROAD | 11378 |
| #00AA10 | ZONE | GREENSPACE | 10898 |
| #F000FF | STREET_CORRIDOR | BACK_ALLEY | 3746 |
| #B2BD87 | UNIQUE_PLACEHOLDER | CIVIC_SPECIAL_BUILDING | 960 |
| #42CCFF | ZONE | COMMERCIAL | 926 |
| #000000 | IGNORE | VOID_OR_BORDER | 888 |

All 7 regions: primitive_type=MASK_REGION, geometry_status=SOURCE_MASK_ONLY_NO_GEOMETRY_CREATED.

---

## No Generation Now

No terrain generated. No lot subdivision. No sidewalk geometry. No road geometry.
No building placement. No fences. No concrete geometry created. Layout not materialized.

---

## Expected Totals (map_00)

| Field | Expected |
|-------|---------|
| source_png_width_px | 256 |
| source_png_height_px | 256 |
| source_pixel_count | 65536 |
| mask_region_count | 7 |
| known_color_region_count | 7 |
| unknown_color_region_count | 0 |
| metadata_matched_region_count | 7 |
| metadata_missing_region_count | 0 |
| mask_region_pixel_total | 65536 |
| residential_pixel_count | 36740 |
| main_road_pixel_count | 11378 |
| greenspace_pixel_count | 10898 |
| back_alley_pixel_count | 3746 |
| civic_placeholder_pixel_count | 960 |
| commercial_pixel_count | 926 |
| ignore_pixel_count | 888 |
| created_geometry_count | 0 |
| writer_ready_region_count | 0 |
| runtime_validated_region_count | 0 |

---

## Claim Boundary (all false, 15 fields)

- writes_lotpack: false
- writes_worldgen_lua: false
- runtime_proven: false
- public_playable_claim: false
- writer_ready_claim: false
- generates_terrain_now: false
- generates_buildings_now: false
- generates_sidewalks_now: false
- subdivides_lots_now: false
- captures_chunk_layers_now: false
- places_fences_now: false
- places_unique_buildings_now: false
- selects_concrete_building_ids_now: false
- creates_concrete_geometry_now: false
- materializes_layout_now: false

---

## CLI Command

```
deadmtl-build-worldbuilder-source-mask-region-extraction
  --source-png                <map_00.png>
  --metadata                  <zone_metadata.json>
  --geometry-primitive-schema <geometry_primitive_schema.json>
  --geometry-preflight        <concrete_geometry_preflight.json>
  --output-json               <out.json>
  --output-md                 <out.md>
  --output-csv                <out.csv>
  --summary                   <out.txt>
```

All output paths must contain `.local`.

---

## Verdict

`MAP25J_WORLDBUILDER_SOURCE_MASK_REGION_EXTRACTION_CONTRACT_COMPLETE`
