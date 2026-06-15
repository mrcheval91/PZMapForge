# MAP-25G: DeadMTL WorldBuilder Future World Layout Plan Contract

**CONTRACT ONLY. No generation. No concrete geometry. Not writer-ready. Not runtime-proven.**

---

## What This Is

The future world layout plan combines MAP-25A through MAP-25F into one auditable layout
planning object. It records what each color zone, street corridor, and unique placeholder
will eventually produce, without executing any of it.

No terrain is generated. No lot subdivision occurs. No sidewalk generation occurs.
No building placement occurs. No fences are generated. No lotpack writing occurs.
No worldgen override file is written. No concrete geometry is created. Layout is not
materialized. No runtime proof. Not writer-ready.

---

## Input Chain (6 inputs)

| Input | Source Stage | File |
|-------|-------------|------|
| Neighborhood profile | MAP-25A | deadmtl_baseline_neighborhood_profile.json |
| Zone metadata | MAP-25B | map_00.zone_metadata.json |
| Lot subdivision plan | MAP-25C | map_00.lot_subdivision_plan.json |
| Sidewalk generation plan | MAP-25D | map_00.sidewalk_generation_plan.json |
| Building selection policy plan | MAP-25E | map_00.building_selection_policy_plan.json |
| Generation dependency manifest | MAP-25F | map_00.generation_dependency_manifest.json |

---

## Layout Components (map_00)

| Color | Role | Zone/Street | Component Type | Future Action |
|-------|------|-------------|----------------|---------------|
| #7200FF | ZONE | RESIDENTIAL | ZONE_LAYOUT_COMPONENT | FUTURE_SUBDIVIDE_INTO_LOTS_AND_ASSIGN_RESIDENTIAL_BUILDINGS |
| #FF6600 | STREET_CORRIDOR | MAIN_ROAD | STREET_LAYOUT_COMPONENT | FUTURE_GENERATE_MAIN_ROAD_CORRIDOR_AND_SIDEWALK_CONTEXT |
| #F000FF | STREET_CORRIDOR | BACK_ALLEY | STREET_LAYOUT_COMPONENT | FUTURE_GENERATE_BACK_ALLEY_SERVICE_CORRIDOR |
| #42CCFF | ZONE | COMMERCIAL | ZONE_LAYOUT_COMPONENT | FUTURE_SUBDIVIDE_INTO_LOTS_AND_ASSIGN_COMMERCIAL_BUILDINGS |
| #00AA10 | ZONE | GREENSPACE | ZONE_LAYOUT_COMPONENT | FUTURE_GENERATE_GREENSPACE_LAYER |
| #B2BD87 | UNIQUE_PLACEHOLDER | CIVIC_SPECIAL_BUILDING | UNIQUE_PLACEHOLDER_LAYOUT_COMPONENT | FUTURE_BIND_UNIQUE_BUILDING_ID_AND_PLACE |
| #000000 | IGNORE | VOID_OR_BORDER | IGNORE_LAYOUT_COMPONENT | IGNORE |

---

## No Generation Now

No terrain is generated. No lot subdivision. No sidewalk generation. No building placement.
No fences. No lotpack writing. No worldgen override file. No concrete geometry.
Layout is not materialized.

---

## Future Execution Requirements

All of the following must be implemented before this plan can execute:

- requires_actual_lot_geometry: true
- requires_actual_sidewalk_geometry: true
- requires_building_catalogue: true
- requires_unique_building_bindings: true
- requires_tile_writer: true
- requires_runtime_validation: true
- can_execute_now: false
- blocked_reason: CONTRACT_ONLY_NO_GEOMETRY_NO_WRITER_NO_RUNTIME_PROOF

---

## Expected Totals (map_00)

| Field | Expected |
|-------|---------|
| layout_component_count | 7 |
| zone_layout_component_count | 3 |
| street_layout_component_count | 2 |
| unique_placeholder_layout_component_count | 1 |
| ignore_layout_component_count | 1 |
| future_lot_group_policy_count | 2 |
| future_sidewalk_corridor_policy_count | 1 |
| future_back_alley_service_corridor_policy_count | 1 |
| future_building_slot_policy_count | 3 |
| concrete_geometry_created_now_count | 0 |
| concrete_building_ids_selected_now_count | 0 |
| layout_materialized_now_count | 0 |

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
deadmtl-build-worldbuilder-future-world-layout-plan
  --profile                 <neighborhood_profile.json>
  --metadata                <zone_metadata.json>
  --lot-plan                <lot_subdivision_plan.json>
  --sidewalk-plan           <sidewalk_generation_plan.json>
  --building-selection-plan <building_selection_policy_plan.json>
  --dependency-manifest     <generation_dependency_manifest.json>
  --output-json             <out.json>
  --output-md               <out.md>
  --output-csv              <out.csv>
  --summary                 <out.txt>
```

All output paths must contain `.local`.

---

## Verdict

`MAP25G_WORLDBUILDER_FUTURE_WORLD_LAYOUT_PLAN_CONTRACT_COMPLETE`
