# MAP-25N: DeadMTL WorldBuilder Adjacency Planning Candidate Extraction Contract

**CONTRACT ONLY. Planning candidate extraction only. No concrete geometry created. Not writer-ready. Not runtime-proven.**

---

## What This Is

The adjacency planning candidate extraction contract reads the MAP-25M component adjacency graph
(82 edges) and converts each adjacency edge into a planning candidate record with a typed candidate
classification, priority, candidate family, future geometry requirement ID, and blocked-by requirements.

This produces 82 planning candidate records: 71 actionable and 11 ignored. These records are NOT
lot polygons, road geometries, frontage geometry, rear access geometry, building slots, sidewalk
geometry, fence geometry, or any materialized artifact.

No generation of any kind occurs. No terrain is generated. No lot subdivision occurs.
No sidewalk geometry is created. No road geometry is created. No building placement occurs.
No fences are generated. No lotpack writing occurs. No worldgen override file is written.
No concrete geometry is created. Layout is not materialized. No runtime proof. Not writer-ready.

---

## Input Chain (5 inputs)

| Input | Source | File |
|-------|--------|------|
| Component adjacency graph | MAP-25M | map_00.component_adjacency_graph.json |
| Connected component extraction | MAP-25K | map_00.connected_component_extraction.json |
| Component intent classification | MAP-25L | map_00.component_intent_classification.json |
| Geometry primitive schema | MAP-25I | map_00.geometry_primitive_schema.json |
| Concrete geometry preflight | MAP-25H | map_00.concrete_geometry_preflight.json |

---

## Extraction Contract

| Field | Value |
|-------|-------|
| tile_id | map_00 |
| source_adjacency_graph_contract | MAP25M_COMPONENT_ADJACENCY_GRAPH |
| total_adjacency_edges_input | 82 |
| candidate_records_extracted | 82 |
| actionable_candidates | 71 |
| ignored_candidates | 11 |
| extraction_produces_geometry | false |
| extraction_produces_materialization | false |

---

## Candidate Type Mapping

| Source Adjacency Relationship | Candidate Type | Count | Priority | Family |
|-------------------------------|----------------|-------|----------|--------|
| FRONTAGE_CANDIDATE | FRONTAGE_PLANNING_CANDIDATE | 26 | 10 | FRONTAGE |
| REAR_OR_SERVICE_ACCESS_CANDIDATE | REAR_SERVICE_ACCESS_PLANNING_CANDIDATE | 26 | 20 | REAR_SERVICE_ACCESS |
| STREET_NETWORK_TOUCHPOINT | STREET_NETWORK_TOUCHPOINT_CANDIDATE | 10 | 30 | STREET_NETWORK |
| GREENSPACE_ACCESS_EDGE | GREENSPACE_ACCESS_CANDIDATE | 1 | 40 | GREENSPACE_ACCESS |
| GREENSPACE_CIVIC_EDGE | CIVIC_GREENSPACE_CONTEXT_CANDIDATE | 2 | 50 | CIVIC_GREENSPACE |
| MIXED_LOT_BLOCK_EDGE | MIXED_LOT_BLOCK_BOUNDARY_CANDIDATE | 6 | 60 | MIXED_LOT_BLOCK |
| IGNORE_BOUNDARY_ADJACENCY | IGNORED_BOUNDARY_ADJACENCY | 11 | 999 | IGNORED |

---

## Global Totals

| Field | Value |
|-------|-------|
| total_adjacency_edges_input | 82 |
| total_candidate_records | 82 |
| actionable_candidate_count | 71 |
| ignored_candidate_count | 11 |
| frontage_planning_candidate_count | 26 |
| rear_service_access_planning_candidate_count | 26 |
| street_network_touchpoint_candidate_count | 10 |
| greenspace_access_candidate_count | 1 |
| civic_greenspace_context_candidate_count | 2 |
| mixed_lot_block_boundary_candidate_count | 6 |
| ignored_boundary_adjacency_count | 11 |
| frontage_contact_total_px | 1860 |
| rear_service_access_contact_total_px | 1713 |
| street_network_contact_total_px | 91 |
| greenspace_access_contact_total_px | 291 |
| civic_greenspace_contact_total_px | 158 |
| mixed_lot_block_contact_total_px | 164 |
| ignored_contact_total_px | 258 |
| created_geometry_count | 0 |
| writer_ready_count | 0 |
| runtime_validated_count | 0 |
| materialized_count | 0 |

---

## Candidate Record Fields (26 fields)

| Field | Type | Notes |
|-------|------|-------|
| candidate_order | int | 1-based sequential order |
| candidate_id | string | map_00_candidate_NNNN |
| source_edge_id | string | from adjacency graph edge_id |
| source_edge_order | int | from adjacency graph edge_order |
| source_adjacency_relationship | string | original relationship classification |
| candidate_type | string | mapped candidate type |
| candidate_priority | int | lower = higher priority |
| candidate_family | string | family grouping |
| component_a_id | string | from source edge |
| component_a_order | int | from source edge |
| component_a_source_color | string | from source edge |
| component_a_intent | string | from source edge |
| component_a_intent_family | string | from source edge |
| component_b_id | string | from source edge |
| component_b_order | int | from source edge |
| component_b_source_color | string | from source edge |
| component_b_intent | string | from source edge |
| component_b_intent_family | string | from source edge |
| contact_length_px | int | from source edge |
| contact_units | string | SOURCE_PIXEL_EDGES (fixed) |
| is_actionable | bool | false only for IGNORED_BOUNDARY_ADJACENCY |
| future_geometry_requirement_id | string | requirement ID for the geometry generator |
| blocked_by_requirements | list<string> | ["NONE"] for ignored; 3-item list for actionable |
| extraction_status | string | ADJACENCY_PLANNING_CANDIDATE_EXTRACTED (fixed) |
| geometry_status | string | NO_GEOMETRY_CREATED (fixed) |
| notes | string | human-readable explanation |

---

## Future Geometry Requirement IDs

| Candidate Type | Future Geometry Requirement ID |
|----------------|-------------------------------|
| FRONTAGE_PLANNING_CANDIDATE | MAP25N_REQ_FRONTAGE_GEOMETRY_GENERATOR |
| REAR_SERVICE_ACCESS_PLANNING_CANDIDATE | MAP25N_REQ_REAR_SERVICE_GEOMETRY_GENERATOR |
| STREET_NETWORK_TOUCHPOINT_CANDIDATE | MAP25N_REQ_STREET_NETWORK_GEOMETRY_GENERATOR |
| GREENSPACE_ACCESS_CANDIDATE | MAP25N_REQ_GREENSPACE_ACCESS_GEOMETRY_GENERATOR |
| CIVIC_GREENSPACE_CONTEXT_CANDIDATE | MAP25N_REQ_CIVIC_GREENSPACE_GEOMETRY_GENERATOR |
| MIXED_LOT_BLOCK_BOUNDARY_CANDIDATE | MAP25N_REQ_LOT_BLOCK_BOUNDARY_GEOMETRY_GENERATOR |
| IGNORED_BOUNDARY_ADJACENCY | MAP25N_REQ_NONE |

---

## Validation Rules

| Rule | Value |
|------|-------|
| source_adjacency_graph_must_exist | true |
| adjacency_edge_count_must_equal_82 | true |
| candidate_count_must_equal_82 | true |
| actionable_candidate_count_must_equal_71 | true |
| ignored_candidate_count_must_equal_11 | true |
| frontage_candidate_count_must_equal_26 | true |
| rear_service_access_candidate_count_must_equal_26 | true |
| street_network_touchpoint_candidate_count_must_equal_10 | true |
| greenspace_access_candidate_count_must_equal_1 | true |
| civic_greenspace_context_candidate_count_must_equal_2 | true |
| mixed_lot_block_boundary_candidate_count_must_equal_6 | true |
| ignored_boundary_adjacency_count_must_equal_11 | true |
| no_geometry_created | true |
| no_runtime_claim | true |
| no_writer_claim | true |
| no_materialization_claim | true |

---

## Claim Boundary (15 fields, all false)

No terrain, no lot polygons, no sidewalk geometry, no road geometry, no frontage geometry,
no rear access geometry, no building slots, no fences, no concrete building IDs, no lotpack,
no WorldGenOverride.lua. Do not install anything into Project Zomboid.
Do not claim runtime proof, writer-ready, or public playable.

| Field | Value |
|-------|-------|
| writes_lotpack | false |
| writes_worldgen_lua | false |
| runtime_proven | false |
| public_playable_claim | false |
| writer_ready_claim | false |
| generates_terrain_now | false |
| generates_buildings_now | false |
| generates_sidewalks_now | false |
| subdivides_lots_now | false |
| captures_chunk_layers_now | false |
| places_fences_now | false |
| places_unique_buildings_now | false |
| selects_concrete_building_ids_now | false |
| creates_concrete_geometry_now | false |
| materializes_layout_now | false |

---

## Why This Still Cannot Execute

Planning candidates are extracted records derived from adjacency edges. They are NOT
concrete lot polygons, road geometries, frontage geometry, rear access geometry, building slots,
or any materialized artifact. No generation of any kind occurs in this step.

- created_geometry_count: 0
- writer_ready_count: 0
- runtime_validated_count: 0
- materialized_count: 0

No concrete geometry generator exists. No tile writer exists.
No runtime validation pass has been performed.
