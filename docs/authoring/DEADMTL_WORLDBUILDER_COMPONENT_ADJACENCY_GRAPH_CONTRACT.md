# MAP-25M: DeadMTL WorldBuilder Component Adjacency Graph Contract

**CONTRACT ONLY. Adjacency graph only. No concrete geometry created. Not writer-ready. Not runtime-proven.**

---

## What This Is

The component adjacency graph contract reads the raw PNG, reproduces the 45 connected component
labels from MAP-25K, binds them to the intent records from MAP-25L, and emits undirected adjacency
edges between components that touch in 4-way pixel space.

This produces adjacency edge records — pixel-space contact relationships between classified
components bound to future planning work items. These are NOT concrete lot polygons, road
geometries, building slots, sidewalk geometry, fence geometry, or frontage geometry.
No generation of any kind occurs.

No terrain is generated. No lot subdivision occurs. No sidewalk geometry is created.
No road geometry is created. No building placement occurs. No fences are generated.
No lotpack writing occurs. No worldgen override file is written. No concrete geometry is
created. Layout is not materialized. No runtime proof. Not writer-ready.

---

## Input Chain (5 inputs)

| Input | Source | File |
|-------|--------|------|
| Raw source PNG | MAP-25A | map_00.png |
| Connected component extraction | MAP-25K | map_00.connected_component_extraction.json |
| Component intent classification | MAP-25L | map_00.component_intent_classification.json |
| Geometry primitive schema | MAP-25I | map_00.geometry_primitive_schema.json |
| Concrete geometry preflight | MAP-25H | map_00.concrete_geometry_preflight.json |

---

## Adjacency Graph Contract

| Field | Value |
|-------|-------|
| tile_id | map_00 |
| source_png_width_px | 256 |
| source_png_height_px | 256 |
| source_pixel_count | 65536 |
| component_node_count | 45 |
| source_component_contract | MAP25K_CONNECTED_COMPONENT_EXTRACTION |
| source_intent_contract | MAP25L_COMPONENT_INTENT_CLASSIFICATION |
| connectivity_rule | FOUR_WAY_NEIGHBOR_PIXELS |
| diagonal_adjacency_enabled | false |
| edge_contact_units | SOURCE_PIXEL_EDGES |
| adjacency_edges_are_undirected | true |
| adjacency_edges_are_not_geometry | true |
| component_bounds_are_pixel_bounds_not_geometry | true |
| geometry_must_be_created_by_future_step | true |

---

## Adjacency Detection Rule

4-way pixel adjacency only. For every source pixel, compare right (x+1, y) and
down (x, y+1) neighbors to avoid double-counting. If the neighbor belongs to a different
component, create or increment an undirected edge (component_a_order < component_b_order).
contact_length_px counts touching 4-way pixel edges between the two components.
No diagonal contact. No self-edges. No duplicate edges.

---

## Relationship Classification Rules

| Intent A | Intent B | Relationship |
|----------|----------|--------------|
| MAIN_ROAD_CORRIDOR | RESIDENTIAL_LOT_BLOCK | FRONTAGE_CANDIDATE |
| MAIN_ROAD_CORRIDOR | COMMERCIAL_LOT_BLOCK | FRONTAGE_CANDIDATE |
| MAIN_ROAD_CORRIDOR | CIVIC_PLACEHOLDER | FRONTAGE_CANDIDATE |
| BACK_ALLEY_CORRIDOR | RESIDENTIAL_LOT_BLOCK | REAR_OR_SERVICE_ACCESS_CANDIDATE |
| BACK_ALLEY_CORRIDOR | COMMERCIAL_LOT_BLOCK | REAR_OR_SERVICE_ACCESS_CANDIDATE |
| BACK_ALLEY_CORRIDOR | CIVIC_PLACEHOLDER | REAR_OR_SERVICE_ACCESS_CANDIDATE |
| MAIN_ROAD_CORRIDOR | BACK_ALLEY_CORRIDOR | STREET_NETWORK_TOUCHPOINT |
| MAIN_ROAD_CORRIDOR | GREENSPACE_MASS | GREENSPACE_ACCESS_EDGE |
| BACK_ALLEY_CORRIDOR | GREENSPACE_MASS | GREENSPACE_ACCESS_EDGE |
| GREENSPACE_MASS | CIVIC_PLACEHOLDER | GREENSPACE_CIVIC_EDGE |
| RESIDENTIAL_LOT_BLOCK | COMMERCIAL_LOT_BLOCK | MIXED_LOT_BLOCK_EDGE |
| (any) | IGNORE_BORDER | IGNORE_BOUNDARY_ADJACENCY |
| (other) | (other) | OTHER_INTENT_ADJACENCY |

---

## No Generation Now

No terrain generated. No lot subdivision. No sidewalk geometry. No road geometry.
No building placement. No fences. No concrete geometry created. Layout not materialized.

---

## Expected Totals (map_00)

| Field | Expected |
|-------|---------|
| component_node_count | 45 |
| adjacency_edge_count | 82 |
| known_component_edge_count | 82 |
| unknown_component_edge_count | 0 |
| self_edge_count | 0 |
| duplicate_edge_count | 0 |
| diagonal_edge_count | 0 |
| contact_length_total_px | 4535 |
| frontage_candidate_edge_count | 26 |
| rear_or_service_access_candidate_edge_count | 26 |
| street_network_touchpoint_edge_count | 10 |
| greenspace_access_edge_count | 1 |
| greenspace_civic_edge_count | 2 |
| mixed_lot_block_edge_count | 6 |
| ignore_boundary_adjacency_edge_count | 11 |
| other_intent_adjacency_edge_count | 0 |
| frontage_candidate_contact_total_px | 1860 |
| rear_or_service_access_contact_total_px | 1713 |
| street_network_touchpoint_contact_total_px | 91 |
| greenspace_access_contact_total_px | 291 |
| greenspace_civic_contact_total_px | 158 |
| mixed_lot_block_contact_total_px | 164 |
| ignore_boundary_adjacency_contact_total_px | 258 |
| other_intent_adjacency_contact_total_px | 0 |
| created_geometry_count | 0 |
| writer_ready_edge_count | 0 |
| runtime_validated_edge_count | 0 |
| materialized_edge_count | 0 |

---

## Intent Pair Edge Totals (map_00)

| Intent Pair | Edge Count | Contact Total px |
|-------------|------------|------------------|
| RESIDENTIAL_LOT_BLOCK <-> MAIN_ROAD_CORRIDOR | 22 | 1794 |
| COMMERCIAL_LOT_BLOCK <-> MAIN_ROAD_CORRIDOR | 4 | 66 |
| RESIDENTIAL_LOT_BLOCK <-> BACK_ALLEY_CORRIDOR | 23 | 1681 |
| COMMERCIAL_LOT_BLOCK <-> BACK_ALLEY_CORRIDOR | 3 | 32 |
| MAIN_ROAD_CORRIDOR <-> BACK_ALLEY_CORRIDOR | 10 | 91 |
| MAIN_ROAD_CORRIDOR <-> GREENSPACE_MASS | 1 | 291 |
| GREENSPACE_MASS <-> CIVIC_PLACEHOLDER | 2 | 158 |
| RESIDENTIAL_LOT_BLOCK <-> COMMERCIAL_LOT_BLOCK | 6 | 164 |
| MAIN_ROAD_CORRIDOR <-> IGNORE_BORDER | 5 | 234 |
| BACK_ALLEY_CORRIDOR <-> IGNORE_BORDER | 6 | 24 |

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
deadmtl-build-worldbuilder-component-adjacency-graph
  --source-png               <map_00.png>
  --connected-components     <connected_component_extraction.json>
  --component-intents        <component_intent_classification.json>
  --geometry-primitive-schema <geometry_primitive_schema.json>
  --geometry-preflight       <concrete_geometry_preflight.json>
  --output-json              <out.json>
  --output-md                <out.md>
  --output-csv               <out.csv>
  --summary                  <out.txt>
```

All output paths must contain `.local`.

---

## Verdict

`MAP25M_WORLDBUILDER_COMPONENT_ADJACENCY_GRAPH_CONTRACT_COMPLETE`
