# MAP-25I: DeadMTL WorldBuilder Geometry Primitive Schema Contract

**CONTRACT ONLY. No generation. No concrete geometry created. Not writer-ready. Not runtime-proven.**

---

## What This Is

The geometry primitive schema contract defines the primitive types, coordinate space, units,
geometry sources, validation rules, and future consumers for concrete geometry that the
WorldBuilder will eventually produce.

No terrain is generated. No lot subdivision occurs. No sidewalk geometry is created.
No road geometry is created. No building placement occurs. No fences are generated.
No lotpack writing occurs. No worldgen override file is written. No concrete geometry is
created. Layout is not materialized. No runtime proof. Not writer-ready.

---

## Input Chain (4 inputs)

| Input | Source Stage | File |
|-------|-------------|------|
| Neighborhood profile | MAP-25A | deadmtl_baseline_neighborhood_profile.json |
| Zone metadata | MAP-25B | map_00.zone_metadata.json |
| Future world layout plan | MAP-25G | map_00.future_world_layout_plan.json |
| Concrete geometry preflight | MAP-25H | map_00.concrete_geometry_preflight.json |

---

## Coordinate Contract

| Field | Value |
|-------|-------|
| map_id | map_00 |
| source_tile_width_px | 256 |
| source_tile_height_px | 256 |
| authoring_scale | 1_PIXEL_EQUALS_1_WORLD_TILE |
| pixel_origin | TOP_LEFT |
| tile_origin | LOCAL_TILE_ORIGIN_0_0 |
| x_axis | EAST_POSITIVE |
| y_axis | SOUTH_POSITIVE |
| z_axis | LEVEL_POSITIVE |
| default_z | 0 |
| coordinate_units | WORLD_TILES |
| coordinate_precision | INTEGER_TILE_COORDINATES_FIRST_PASS |

---

## 10 Primitive Types

| Type | Class | Creation Status |
|------|-------|-----------------|
| POINT | COORDINATE_PRIMITIVE | NOT_CREATED |
| LINE_SEGMENT | LINEAR_PRIMITIVE | NOT_CREATED |
| POLYLINE | LINEAR_PRIMITIVE | NOT_CREATED |
| RECTANGLE | AREA_PRIMITIVE | NOT_CREATED |
| POLYGON | AREA_PRIMITIVE | NOT_CREATED |
| CORRIDOR | DERIVED_AREA_PRIMITIVE | NOT_CREATED |
| STRIP | DERIVED_AREA_PRIMITIVE | NOT_CREATED |
| SLOT | PLACEMENT_PRIMITIVE | NOT_CREATED |
| BOUNDARY_LINE | LINEAR_PRIMITIVE | NOT_CREATED |
| MASK_REGION | SOURCE_REGION_PRIMITIVE | NOT_CREATED |

All 10 primitive types: creation_status = NOT_CREATED.

---

## No Generation Now

No terrain generated. No lot subdivision. No sidewalk geometry. No road geometry.
No building placement. No fences. No concrete geometry created. Layout not materialized.

---

## Expected Totals (map_00)

| Field | Expected |
|-------|---------|
| primitive_type_count | 10 |
| coordinate_primitive_count | 1 |
| linear_primitive_count | 3 |
| area_primitive_count | 2 |
| derived_area_primitive_count | 2 |
| placement_primitive_count | 1 |
| source_region_primitive_count | 1 |
| created_geometry_count | 0 |
| writer_ready_primitive_count | 0 |
| runtime_validated_primitive_count | 0 |

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
deadmtl-build-worldbuilder-geometry-primitive-schema
  --profile              <neighborhood_profile.json>
  --metadata             <zone_metadata.json>
  --future-layout-plan   <future_world_layout_plan.json>
  --geometry-preflight   <concrete_geometry_preflight.json>
  --output-json          <out.json>
  --output-md            <out.md>
  --output-csv           <out.csv>
  --summary              <out.txt>
```

All output paths must contain `.local`.

---

## Verdict

`MAP25I_WORLDBUILDER_GEOMETRY_PRIMITIVE_SCHEMA_CONTRACT_COMPLETE`
