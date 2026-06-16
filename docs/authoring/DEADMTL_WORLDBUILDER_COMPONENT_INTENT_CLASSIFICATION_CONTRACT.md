# MAP-25L: DeadMTL WorldBuilder Component Intent Classification Contract

**CONTRACT ONLY. Intent classification only. No concrete geometry created. Not writer-ready. Not runtime-proven.**

---

## What This Is

The component intent classification contract reads the 45 connected components from MAP-25K
and assigns each component a semantic future geometry intent bucket.

This produces intent records — classified pixel-space components bound to future geometry
work items. These are NOT concrete lot polygons, road geometries, building slots, sidewalk
geometry, or fence geometry. No generation of any kind occurs.

No terrain is generated. No lot subdivision occurs. No sidewalk geometry is created.
No road geometry is created. No building placement occurs. No fences are generated.
No lotpack writing occurs. No worldgen override file is written. No concrete geometry is
created. Layout is not materialized. No runtime proof. Not writer-ready.

---

## Input Chain (5 inputs)

| Input | Source | File |
|-------|--------|------|
| Zone metadata | MAP-25B | map_00.zone_metadata.json |
| Source mask region extraction | MAP-25J | map_00.source_mask_region_extraction.json |
| Connected component extraction | MAP-25K | map_00.connected_component_extraction.json |
| Geometry primitive schema | MAP-25I | map_00.geometry_primitive_schema.json |
| Concrete geometry preflight | MAP-25H | map_00.concrete_geometry_preflight.json |

---

## Classification Contract

| Field | Value |
|-------|-------|
| tile_id | map_00 |
| parent_connected_component_count | 45 |
| source_component_contract | MAP25K_CONNECTED_COMPONENT_EXTRACTION |
| classification_input_status | CONNECTED_COMPONENTS_ONLY_NO_GEOMETRY_CREATED |
| classification_output_status | INTENT_BUCKETS_ONLY |
| component_bounds_are_pixel_bounds_not_geometry | true |
| intent_records_are_not_geometry | true |
| geometry_must_be_created_by_future_step | true |

---

## 7 Intent Bucket Definitions

| Intent Bucket | Intent Family | Component Count | Pixel Total |
|---------------|---------------|-----------------|-------------|
| RESIDENTIAL_LOT_BLOCK | LOT_AND_BUILDING_SLOT_CANDIDATE | 22 | 36740 |
| COMMERCIAL_LOT_BLOCK | LOT_AND_BUILDING_SLOT_CANDIDATE | 4 | 926 |
| MAIN_ROAD_CORRIDOR | STREET_CORRIDOR_CANDIDATE | 1 | 11378 |
| BACK_ALLEY_CORRIDOR | SERVICE_CORRIDOR_CANDIDATE | 10 | 3746 |
| GREENSPACE_MASS | GREENSPACE_LAYER_CANDIDATE | 1 | 10898 |
| CIVIC_PLACEHOLDER | UNIQUE_BUILDING_PLACEHOLDER_CANDIDATE | 2 | 960 |
| IGNORE_BORDER | IGNORE_NO_GEOMETRY | 5 | 888 |

All 45 intent records: geometry_status=INTENT_ONLY_NO_GEOMETRY_CREATED,
source_component_status=CONNECTED_PIXEL_COMPONENT_ONLY,
classification_confidence=COMPONENT_COLOR_ROLE_METADATA_MATCH.

---

## No Generation Now

No terrain generated. No lot subdivision. No sidewalk geometry. No road geometry.
No building placement. No fences. No concrete geometry created. Layout not materialized.

---

## Expected Totals (map_00)

| Field | Expected |
|-------|---------|
| parent_connected_component_count | 45 |
| classified_component_count | 45 |
| unclassified_component_count | 0 |
| intent_bucket_count | 7 |
| classified_pixel_total | 65536 |
| residential_lot_block_count | 22 |
| commercial_lot_block_count | 4 |
| main_road_corridor_count | 1 |
| back_alley_corridor_count | 10 |
| greenspace_mass_count | 1 |
| civic_placeholder_count | 2 |
| ignore_border_count | 5 |
| residential_lot_block_pixel_total | 36740 |
| commercial_lot_block_pixel_total | 926 |
| main_road_corridor_pixel_total | 11378 |
| back_alley_corridor_pixel_total | 3746 |
| greenspace_mass_pixel_total | 10898 |
| civic_placeholder_pixel_total | 960 |
| ignore_border_pixel_total | 888 |
| created_geometry_count | 0 |
| writer_ready_intent_count | 0 |
| runtime_validated_intent_count | 0 |
| materialized_intent_count | 0 |

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
deadmtl-build-worldbuilder-component-intent-classification
  --metadata                  <zone_metadata.json>
  --source-mask-regions       <source_mask_region_extraction.json>
  --connected-components      <connected_component_extraction.json>
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

`MAP25L_WORLDBUILDER_COMPONENT_INTENT_CLASSIFICATION_CONTRACT_COMPLETE`
