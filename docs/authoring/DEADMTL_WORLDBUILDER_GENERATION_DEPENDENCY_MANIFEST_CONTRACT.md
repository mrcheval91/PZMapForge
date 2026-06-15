# MAP-25F: DeadMTL WorldBuilder Generation Dependency Manifest Contract

**CONTRACT ONLY. Not writer-ready. Not runtime-proven. No terrain generated.**

---

## What This Is

The generation dependency manifest ties MAP-25A through MAP-25E into one ordered,
auditable pipeline record. It names each input step, verifies that the file exists
and its format field matches the expected value, and records all dependency edges
between steps.

No generation occurs here. No lot subdivision. No sidewalk generation.
No building placement. No fences. No lotpack writing. No worldgen override file.

---

## Five Pipeline Steps

| Order | Step ID                     | Source Stage | Expected Format |
|-------|-----------------------------|-------------|-----------------|
| 1     | PROFILE_CONTRACT            | MAP-25A     | pzmapforge.deadmtl.worldbuilder.neighborhood-profile.v1 |
| 2     | RAW_TILE_ZONE_METADATA      | MAP-25B     | pzmapforge.deadmtl.worldbuilder.raw-tile-zone-metadata.v1 |
| 3     | LOT_SUBDIVISION_PLAN        | MAP-25C     | pzmapforge.deadmtl.worldbuilder.lot-subdivision-plan.v1 |
| 4     | SIDEWALK_GENERATION_PLAN    | MAP-25D     | pzmapforge.deadmtl.worldbuilder.sidewalk-generation-plan.v1 |
| 5     | BUILDING_SELECTION_POLICY_PLAN | MAP-25E  | pzmapforge.deadmtl.worldbuilder.building-selection-policy-plan.v1 |

---

## Ten Dependency Edges

```
PROFILE_CONTRACT            -> RAW_TILE_ZONE_METADATA
PROFILE_CONTRACT            -> LOT_SUBDIVISION_PLAN
RAW_TILE_ZONE_METADATA      -> LOT_SUBDIVISION_PLAN
PROFILE_CONTRACT            -> SIDEWALK_GENERATION_PLAN
RAW_TILE_ZONE_METADATA      -> SIDEWALK_GENERATION_PLAN
LOT_SUBDIVISION_PLAN        -> SIDEWALK_GENERATION_PLAN
PROFILE_CONTRACT            -> BUILDING_SELECTION_POLICY_PLAN
RAW_TILE_ZONE_METADATA      -> BUILDING_SELECTION_POLICY_PLAN
LOT_SUBDIVISION_PLAN        -> BUILDING_SELECTION_POLICY_PLAN
SIDEWALK_GENERATION_PLAN    -> BUILDING_SELECTION_POLICY_PLAN
```

All edges have `dependency_type: REQUIRED_INPUT` and `required: true`.

---

## Expected Totals (map_00 real workflow)

| Field                         | Expected Value |
|-------------------------------|---------------|
| step_count                    | 5             |
| dependency_edge_count         | 10            |
| required_input_count          | 5             |
| existing_input_count          | 5             |
| missing_input_count           | 0             |
| format_match_count            | 5             |
| format_mismatch_count         | 0             |
| contract_only_step_count      | 5             |
| runtime_proven_step_count     | 0             |
| writer_ready_step_count       | 0             |
| generation_executed_step_count| 0             |

---

## Claim Boundary (all false)

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

---

## Future Consumer

BUILDING_SELECTION_POLICY_PLAN is consumed by FUTURE_WORLD_LAYOUT_PLAN.
That future step will combine lot subdivision, sidewalk generation, and building
selection policy into an actual world layout execution plan. It is not yet defined
and is not part of MAP-25F.

---

## CLI Command

```
deadmtl-build-worldbuilder-generation-dependency-manifest
  --profile                 <neighborhood_profile.json>
  --metadata                <zone_metadata.json>
  --lot-plan                <lot_subdivision_plan.json>
  --sidewalk-plan           <sidewalk_generation_plan.json>
  --building-selection-plan <building_selection_policy_plan.json>
  --output-json             <out.json>
  --output-md               <out.md>
  --output-csv              <out.csv>
  --summary                 <out.txt>
```

All output paths must contain `.local`.

---

## Verdict

`MAP25F_WORLDBUILDER_GENERATION_DEPENDENCY_MANIFEST_CONTRACT_COMPLETE`
