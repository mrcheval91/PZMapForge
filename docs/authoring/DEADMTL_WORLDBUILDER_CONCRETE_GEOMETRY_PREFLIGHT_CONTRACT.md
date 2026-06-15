# MAP-25H: DeadMTL WorldBuilder Concrete Geometry Preflight Contract

**CONTRACT ONLY. No generation. No concrete geometry created. Not writer-ready. Not runtime-proven.**

---

## What This Is

The concrete geometry preflight contract inspects the MAP-25G future world layout plan and
produces a checklist of all concrete geometry that must exist before the world layout can
actually be materialized.

No terrain is generated. No lot subdivision occurs. No sidewalk geometry is created.
No road geometry is created. No building placement occurs. No fences are generated.
No lotpack writing occurs. No worldgen override file is written. No concrete geometry is
created. Layout is not materialized. No runtime proof. Not writer-ready.

---

## Input Chain (7 inputs)

| Input | Source Stage | File |
|-------|-------------|------|
| Neighborhood profile | MAP-25A | deadmtl_baseline_neighborhood_profile.json |
| Zone metadata | MAP-25B | map_00.zone_metadata.json |
| Lot subdivision plan | MAP-25C | map_00.lot_subdivision_plan.json |
| Sidewalk generation plan | MAP-25D | map_00.sidewalk_generation_plan.json |
| Building selection policy plan | MAP-25E | map_00.building_selection_policy_plan.json |
| Generation dependency manifest | MAP-25F | map_00.generation_dependency_manifest.json |
| Future world layout plan | MAP-25G | map_00.future_world_layout_plan.json |

---

## 9 Preflight Requirements

| Order | Requirement ID | Type |
|-------|---------------|------|
| 1 | LOT_GEOMETRY | GEOMETRY_REQUIREMENT |
| 2 | MAIN_ROAD_CORRIDOR_GEOMETRY | GEOMETRY_REQUIREMENT |
| 3 | BACK_ALLEY_SERVICE_CORRIDOR_GEOMETRY | GEOMETRY_REQUIREMENT |
| 4 | SIDEWALK_GEOMETRY | GEOMETRY_REQUIREMENT |
| 5 | BUILDING_SLOT_GEOMETRY | GEOMETRY_REQUIREMENT |
| 6 | UNIQUE_BUILDING_BINDING | GEOMETRY_REQUIREMENT |
| 7 | FENCE_AND_LOT_BOUNDARY_GEOMETRY | GEOMETRY_REQUIREMENT |
| 8 | TILE_WRITER_IMPLEMENTATION | WRITER_REQUIREMENT |
| 9 | RUNTIME_VALIDATION_PASS | RUNTIME_REQUIREMENT |

All 9 requirements blocked. can_execute_now: false for all.

---

## No Generation Now

No terrain generated. No lot subdivision. No sidewalk geometry. No road geometry.
No building placement. No fences. No concrete geometry created. Layout not materialized.

---

## Expected Totals (map_00)

| Field | Expected |
|-------|---------|
| requirement_count | 9 |
| blocked_requirement_count | 9 |
| can_execute_now_count | 0 |
| geometry_requirement_count | 7 |
| writer_requirement_count | 1 |
| runtime_requirement_count | 1 |
| concrete_geometry_created_now_count | 0 |
| layout_materialized_now_count | 0 |
| writer_ready_requirement_count | 0 |
| runtime_validated_requirement_count | 0 |

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
deadmtl-build-worldbuilder-concrete-geometry-preflight
  --profile                 <neighborhood_profile.json>
  --metadata                <zone_metadata.json>
  --lot-plan                <lot_subdivision_plan.json>
  --sidewalk-plan           <sidewalk_generation_plan.json>
  --building-selection-plan <building_selection_policy_plan.json>
  --dependency-manifest     <generation_dependency_manifest.json>
  --future-layout-plan      <future_world_layout_plan.json>
  --output-json             <out.json>
  --output-md               <out.md>
  --output-csv              <out.csv>
  --summary                 <out.txt>
```

All output paths must contain `.local`.

---

## Verdict

`MAP25H_WORLDBUILDER_CONCRETE_GEOMETRY_PREFLIGHT_CONTRACT_COMPLETE`
