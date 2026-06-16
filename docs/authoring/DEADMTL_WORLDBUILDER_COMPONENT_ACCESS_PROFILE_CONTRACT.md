# MAP-25O: DeadMTL WorldBuilder Component Access Profile Contract

**CONTRACT ONLY. Access profile only. No concrete geometry created. Not writer-ready. Not runtime-proven.**

---

## What This Is

The component access profile contract reads MAP-25N planning candidates and MAP-25L component intents
and produces a per-component access profile for all 45 classified components.

For each component, the profile records: how many candidates of each type touch it, which neighbor
has the largest contact for frontage and rear-service purposes, and an access readiness class.

These are NOT concrete lot polygons, road geometries, building slots, sidewalk geometry, fence
geometry, or frontage geometry. No generation of any kind occurs.

No terrain is generated. No lot subdivision occurs. No sidewalk geometry is created.
No road geometry is created. No building placement occurs. No fences are generated.
No lotpack writing occurs. No worldgen override file is written. No concrete geometry is
created. Layout is not materialized. No runtime proof. Not writer-ready.

---

## Input Chain (2 inputs)

| Input | Source | File |
|-------|--------|------|
| Adjacency planning candidates | MAP-25N | map_00.adjacency_planning_candidate_extraction.json |
| Component intent classification | MAP-25L | map_00.component_intent_classification.json |

---

## Access Profile Contract

| Field | Value |
|-------|-------|
| tile_id | map_00 |
| total_components_input | 45 |
| source_candidate_contract | MAP25N_ADJACENCY_PLANNING_CANDIDATE_EXTRACTION |
| source_intent_contract | MAP25L_COMPONENT_INTENT_CLASSIFICATION |

---

## Access Readiness Classification Rules

| Intent | Classification |
|--------|----------------|
| RESIDENTIAL_LOT_BLOCK or COMMERCIAL_LOT_BLOCK (frontage≥1 AND rear≥1) | DUAL_ACCESS_CANDIDATE |
| RESIDENTIAL_LOT_BLOCK or COMMERCIAL_LOT_BLOCK (frontage≥1, rear=0) | FRONTAGE_ONLY_CANDIDATE |
| RESIDENTIAL_LOT_BLOCK or COMMERCIAL_LOT_BLOCK (frontage=0, rear≥1) | REAR_SERVICE_ONLY_CANDIDATE |
| RESIDENTIAL_LOT_BLOCK or COMMERCIAL_LOT_BLOCK (frontage=0, rear=0) | LANDLOCKED_CANDIDATE |
| MAIN_ROAD_CORRIDOR | MAIN_ROAD_CORRIDOR_NODE |
| BACK_ALLEY_CORRIDOR | BACK_ALLEY_CORRIDOR_NODE |
| GREENSPACE_MASS | GREENSPACE_MASS_NODE |
| CIVIC_PLACEHOLDER | CIVIC_PLACEHOLDER_NODE |
| IGNORE_BORDER | IGNORED_BOUNDARY_COMPONENT |

---

## Primary Candidate Selection Rule

For each lot-block component, the primary frontage partner is the FRONTAGE_PLANNING_CANDIDATE
neighbor with the largest contact_length_px. Tiebreak: smallest component_id (lexicographic).
Same rule for primary rear-service partner from REAR_SERVICE_ACCESS_PLANNING_CANDIDATE neighbors.
Non-lot-block components: primary_frontage_component_id = empty, primary_frontage_contact_px = 0.

---

## No Generation Now

No terrain generated. No lot subdivision. No sidewalk geometry. No road geometry.
No building placement. No fences. No concrete geometry created. Layout not materialized.

---

## Expected Totals (map_00)

| Field | Expected |
|-------|---------|
| profile_records_extracted | 45 |
| dual_access_candidate_count | 25 |
| frontage_only_candidate_count | 1 |
| rear_service_only_candidate_count | 0 |
| landlocked_candidate_count | 0 |
| main_road_corridor_node_count | 1 |
| back_alley_corridor_node_count | 10 |
| greenspace_mass_node_count | 1 |
| civic_placeholder_node_count | 2 |
| ignored_boundary_component_count | 5 |
| created_geometry_count | 0 |
| writer_ready_profile_count | 0 |
| runtime_validated_profile_count | 0 |

---

## Per-Component Spot Values (map_00)

| comp | intent | frontage | rear | primary_frontage_id | frontage_px | primary_rear_id | rear_px | class |
|------|--------|----------|------|---------------------|-------------|-----------------|---------|-------|
| 1 | RESIDENTIAL_LOT_BLOCK | 1 | 2 | map_00_component_0023 | 178 | map_00_component_0030 | 60 | DUAL_ACCESS_CANDIDATE |
| 23 | MAIN_ROAD_CORRIDOR | 26 | 0 | (empty) | 0 | (empty) | 0 | MAIN_ROAD_CORRIDOR_NODE |
| 40 | COMMERCIAL_LOT_BLOCK | 1 | 0 | map_00_component_0023 | 8 | (empty) | 0 | FRONTAGE_ONLY_CANDIDATE |
| 41 | IGNORE_BORDER | 0 | 0 | (empty) | 0 | (empty) | 0 | IGNORED_BOUNDARY_COMPONENT |

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
deadmtl-build-worldbuilder-component-access-profile
  --planning-candidates  <map_00.adjacency_planning_candidate_extraction.json>
  --component-intents    <map_00.component_intent_classification.json>
  --output-json          <out.json>
  --output-md            <out.md>
  --output-csv           <out.csv>
  --summary              <out.txt>
```

All output paths must contain `.local`.

---

## Verdict

`MAP25O_WORLDBUILDER_COMPONENT_ACCESS_PROFILE_CONTRACT_COMPLETE`
