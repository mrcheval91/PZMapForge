# MAP-25K: DeadMTL WorldBuilder Connected Component Extraction Contract

**CONTRACT ONLY. Connected component extraction only. No concrete geometry created. Not writer-ready. Not runtime-proven.**

---

## What This Is

The connected component extraction contract reads the raw `map_00.png` color map and splits
each source mask region from MAP-25J into individual 4-way connected pixel islands (components).

This produces connected component records — pixel-space islands of each source color, each
bound to its parent mask region and zone metadata entry.

These are NOT concrete lot polygons, road geometries, building slots, sidewalk geometry,
or fence geometry. No generation of any kind occurs.

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
| Source mask region extraction | MAP-25J | map_00.source_mask_region_extraction.json |

---

## Source PNG / Connectivity Contract

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
| connectivity_rule | FOUR_WAY_NEIGHBOR_PIXELS |
| diagonal_connectivity_enabled | false |
| bounds_are_pixel_bounds_not_geometry | true |
| components_are_not_geometry | true |

---

## 7 Parent Mask Regions / Component Counts (map_00)

| Color | Role | Zone Type / Street Class | Component Count | Pixel Total |
|-------|------|--------------------------|-----------------|-------------|
| #7200FF | ZONE | RESIDENTIAL | 22 | 36740 |
| #FF6600 | STREET_CORRIDOR | MAIN_ROAD | 1 | 11378 |
| #00AA10 | ZONE | GREENSPACE | 1 | 10898 |
| #F000FF | STREET_CORRIDOR | BACK_ALLEY | 10 | 3746 |
| #B2BD87 | UNIQUE_PLACEHOLDER | CIVIC_SPECIAL_BUILDING | 2 | 960 |
| #42CCFF | ZONE | COMMERCIAL | 4 | 926 |
| #000000 | IGNORE | VOID_OR_BORDER | 5 | 888 |

Total connected components: 45. Total pixels: 65536.

All 45 components: primitive_type=MASK_REGION, component_status=CONNECTED_PIXEL_COMPONENT_ONLY,
geometry_status=CONNECTED_COMPONENT_ONLY_NO_GEOMETRY_CREATED.

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
| parent_mask_region_count | 7 |
| connected_component_count | 45 |
| known_color_component_count | 45 |
| unknown_color_component_count | 0 |
| component_pixel_total | 65536 |
| residential_component_count | 22 |
| main_road_component_count | 1 |
| greenspace_component_count | 1 |
| back_alley_component_count | 10 |
| civic_placeholder_component_count | 2 |
| commercial_component_count | 4 |
| ignore_component_count | 5 |
| residential_pixel_total | 36740 |
| main_road_pixel_total | 11378 |
| greenspace_pixel_total | 10898 |
| back_alley_pixel_total | 3746 |
| civic_placeholder_pixel_total | 960 |
| commercial_pixel_total | 926 |
| ignore_pixel_total | 888 |
| created_geometry_count | 0 |
| writer_ready_component_count | 0 |
| runtime_validated_component_count | 0 |

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
deadmtl-build-worldbuilder-connected-component-extraction
  --source-png                <map_00.png>
  --metadata                  <zone_metadata.json>
  --geometry-primitive-schema <geometry_primitive_schema.json>
  --source-mask-regions       <source_mask_region_extraction.json>
  --output-json               <out.json>
  --output-md                 <out.md>
  --output-csv                <out.csv>
  --summary                   <out.txt>
```

All output paths must contain `.local`.

---

## Verdict

`MAP25K_WORLDBUILDER_CONNECTED_COMPONENT_EXTRACTION_CONTRACT_COMPLETE`
