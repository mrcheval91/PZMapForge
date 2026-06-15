# MAP-25B: DeadMTL WorldBuilder Raw Tile Zone Metadata Contract

## Purpose

Map raw PNG tile colors from map_00.png to WorldBuilder semantic intents.

**This is metadata only. No terrain generation.**

- No sidewalk generation now
- No lot subdivision now
- No building placement
- No fence placement
- No lotpack writing
- No runtime proof

## Claim boundary

```
metadata_status:              AUTHORING_METADATA_ONLY
runtime_proven:               false
writer_ready_claim:           false
generates_buildings_now:      false
generates_sidewalks_now:      false
subdivides_lots_now:          false
captures_chunk_layers_now:    false
writes_lotpack:               false
writes_worldgen_lua:          false
```

## Color Role Table

| Color | Role | Zone Type | Street Class | Sidewalk Eligible | Notes |
|-------|------|-----------|--------------|-------------------|-------|
| #7200FF | ZONE | RESIDENTIAL | - | false | Residential fabric. Future lot subdivision. Frontage: MAIN_ROAD. |
| #FF6600 | STREET_CORRIDOR | TRANSPORT | MAIN_ROAD | **true** | Main frontage road. Sidewalks from profile. |
| #F000FF | STREET_CORRIDOR | TRANSPORT | BACK_ALLEY | false | Back alley. No sidewalks. Rear/service access only. |
| #42CCFF | ZONE | COMMERCIAL | - | false | Commercial zone. Future lot subdivision. |
| #00AA10 | ZONE | GREENSPACE | - | false | Park/open ground. Not normalized from any other green. |
| #B2BD87 | UNIQUE_PLACEHOLDER | CIVIC_SPECIAL_BUILDING | - | false | Civic/government placeholder. No procedural fill. |
| #000000 | IGNORE | VOID_OR_BORDER | - | false | Opaque black. Not transparency. |

## Sidewalk Note

Sidewalks are NOT drawn as their own color.
They are generated later from eligible MAIN_ROAD corridors using the active neighborhood profile.
Only `#FF6600` (MAIN_ROAD) corridors are `sidewalk_eligible = true`.

## Back Alley Note

`#F000FF` (BACK_ALLEY) corridors do not generate sidewalks.
They serve rear/service access and must not be used as the primary facade frontage.
Residential and commercial lots should prefer MAIN_ROAD as frontage.

## Lot Subdivision Note

Residential (#7200FF) and commercial (#42CCFF) zones are subdivided into lots in a future pass (FUTURE_MAP25C).
`lot_subdivision_policy.enabled_later = true` marks them as eligible.
No subdivision is executed now.

## Facade Orientation Note

Residential lots use `ROW_UNIFORM_FRONTAGE`:
- If a block has one main road frontage, all lots in that row face that road.
- Alleys serve as rear/service access only.
- If multiple possible frontages exist, the layout system picks the best based on road class, block shape, and neighborhood rules.

## Commercial Lot Subdivision Note

Commercial zones (#42CCFF) are subdivided if large enough.
Commercial lots prefer MAIN_ROAD frontage (`preferred_frontage = MAIN_ROAD`).

## Civic Placeholder Note

`#B2BD87` is a `UNIQUE_PLACEHOLDER` for civic/government/library/institutional buildings.
`procedural_fill = false` — no random building will fill this space.
`unique_override_allowed = true` — future metadata can assign a specific building id.

## Fence / Cloture Note

Fences and clotures are a future layer generated on lot lines AFTER lot subdivision.
`lot_line_fence_policy = FUTURE_LAYER` — not part of this metadata contract.

## Procedural Fill Note

| Role | procedural_fill | Effect |
|------|----------------|--------|
| ZONE | true | WorldBuilder fills with buildings from the zone family |
| UNIQUE_PLACEHOLDER | false | Reserved; no random fill |
| IGNORE | false | Never filled |
| STREET_CORRIDOR | false | Roads, not fill targets |

## Unique Override Note

| Role | unique_override_allowed | Effect |
|------|------------------------|--------|
| ZONE | true | Hand-authored building can replace procedural fill |
| UNIQUE_PLACEHOLDER | true | Explicit building assignment expected |
| IGNORE | false | Cannot accept overrides |

## Command

```
deadmtl-validate-worldbuilder-zone-metadata \
  --metadata    examples\deadmtl-layer-pack\worldbuilder\tiles\map_00.zone_metadata.json \
  --inspection  .local\deadmtl-authoring\raw-map-tile-inspection\map_00\map_00.raw_tile_inspection.json \
  --profile     examples\deadmtl-layer-pack\worldbuilder\neighborhoods\deadmtl_baseline_neighborhood_profile.json \
  --output-json .local\deadmtl-authoring\worldbuilder-zone-metadata\map_00\map_00.zone_metadata.validation.json \
  --output-md   .local\deadmtl-authoring\worldbuilder-zone-metadata\map_00\map_00.zone_metadata.validation.md \
  --output-csv  .local\deadmtl-authoring\worldbuilder-zone-metadata\map_00\map_00.zone_metadata.validation.csv \
  --summary     .local\deadmtl-authoring\worldbuilder-zone-metadata\map_00\map_00.zone_metadata.validation.summary.txt
```

## Helper script

```
examples\deadmtl-layer-pack\scripts\run-deadmtl-worldbuilder-zone-metadata-validation.ps1
```

## Output files

| File | Format |
|------|--------|
| `map_00.zone_metadata.validation.json` | Full validation result |
| `map_00.zone_metadata.validation.md` | Human-readable summary |
| `map_00.zone_metadata.validation.csv` | One row per check: rule_id,severity,passed,message |
| `map_00.zone_metadata.validation.summary.txt` | Terminal-friendly summary |

## Validation checks

| rule_id | Severity | Description |
|---------|----------|-------------|
| METADATA_FILE_EXISTS | ERROR | Metadata file must exist |
| FORMAT_VALID | ERROR | format must equal the v1 string |
| TILE_ID_NON_EMPTY | ERROR | tile_id must be non-empty |
| NEIGHBORHOOD_PROFILE_ID_NON_EMPTY | ERROR | neighborhood_profile_id must be non-empty |
| NEIGHBORHOOD_PROFILE_EXISTS | ERROR | Neighborhood profile file must exist |
| INSPECTION_JSON_EXISTS | ERROR | Inspection JSON must exist |
| NO_DUPLICATE_COLORS | ERROR | No duplicate colors in color_roles |
| ALL_INSPECTION_COLORS_IN_METADATA | ERROR | Every inspection color appears in metadata |
| ALL_METADATA_COLORS_IN_INSPECTION | ERROR | Every metadata color appears in inspection |
| ALL_ROLES_VALID | ERROR | Role values must be ZONE / STREET_CORRIDOR / UNIQUE_PLACEHOLDER / IGNORE |
| ALL_ZONE_TYPES_NON_EMPTY | ERROR | zone_type must be non-empty for all entries |
| ALL_INTENSITIES_VALID | ERROR | development_intensity valid when present |
| STREET_CORRIDOR_HAS_STREET_CLASS | ERROR | STREET_CORRIDOR entries require street_class |
| ALL_STREET_CLASSES_VALID | ERROR | street_class must be a valid value |
| MAIN_ROAD_SIDEWALK_ELIGIBLE_TRUE | ERROR | MAIN_ROAD must have sidewalk_eligible = true |
| BACK_ALLEY_SIDEWALK_ELIGIBLE_FALSE | ERROR | BACK_ALLEY must have sidewalk_eligible = false |
| SIDEWALK_ELIGIBLE_REQUIRES_NEIGHBORHOOD_PROFILE | ERROR | sidewalk_eligible=true requires sidewalk_policy_source=NEIGHBORHOOD_PROFILE |
| NON_STREET_NOT_SIDEWALK_ELIGIBLE | ERROR | Non-STREET_CORRIDOR roles must not be sidewalk_eligible |
| RESIDENTIAL_LOT_SUBDIVISION_ENABLED_LATER | ERROR | RESIDENTIAL zones require lot_subdivision_policy.enabled_later = true |
| COMMERCIAL_LOT_SUBDIVISION_ENABLED_LATER | ERROR | COMMERCIAL zones require lot_subdivision_policy.enabled_later = true |
| RESIDENTIAL_PREFERRED_FRONTAGE_MAIN_ROAD | ERROR | RESIDENTIAL preferred_frontage = MAIN_ROAD |
| RESIDENTIAL_AVOID_FRONTAGE_INCLUDES_BACK_ALLEY | ERROR | RESIDENTIAL avoid_frontage must include BACK_ALLEY |
| RESIDENTIAL_FACADE_ORIENTATION_ROW_UNIFORM | ERROR | RESIDENTIAL facade_orientation_policy = ROW_UNIFORM_FRONTAGE |
| COMMERCIAL_PREFERRED_FRONTAGE_MAIN_ROAD | ERROR | COMMERCIAL preferred_frontage = MAIN_ROAD |
| UNIQUE_PLACEHOLDER_NOT_PROCEDURAL_FILL | ERROR | UNIQUE_PLACEHOLDER: procedural_fill=false, unique_override_allowed=true |
| IGNORE_NOT_PROCEDURAL_FILL | ERROR | IGNORE: procedural_fill=false, unique_override_allowed=false |
| CLAIM_BOUNDARY_EXISTS | ERROR | claim_boundary must be present |
| CLAIM_WRITES_LOTPACK_FALSE | ERROR | writes_lotpack = false |
| CLAIM_WRITES_WORLDGEN_LUA_FALSE | ERROR | writes_worldgen_lua = false |
| CLAIM_RUNTIME_PROVEN_FALSE | ERROR | runtime_proven = false |
| CLAIM_PUBLIC_PLAYABLE_FALSE | ERROR | public_playable_claim = false |
| CLAIM_WRITER_READY_FALSE | ERROR | writer_ready_claim = false |
| CLAIM_GENERATES_BUILDINGS_FALSE | ERROR | generates_buildings_now = false |
| CLAIM_GENERATES_SIDEWALKS_FALSE | ERROR | generates_sidewalks_now = false |
| CLAIM_SUBDIVIDES_LOTS_FALSE | ERROR | subdivides_lots_now = false |
| CLAIM_CAPTURES_CHUNK_LAYERS_FALSE | ERROR | captures_chunk_layers_now = false |
| NO_COLOR_NORMALIZE_00AA10 | ERROR | #00AA10 must not be replaced by #00AA00 |
| NO_COLOR_NORMALIZE_F000FF | ERROR | #F000FF must not be replaced by #FF00FF |
| BASELINE_7200FF_RESIDENTIAL | ERROR | #7200FF must be ZONE / RESIDENTIAL |
| BASELINE_FF6600_MAIN_ROAD | ERROR | #FF6600 must be STREET_CORRIDOR / MAIN_ROAD |
| BASELINE_F000FF_BACK_ALLEY | ERROR | #F000FF must be STREET_CORRIDOR / BACK_ALLEY |
| BASELINE_42CCFF_COMMERCIAL | ERROR | #42CCFF must be ZONE / COMMERCIAL |
| BASELINE_00AA10_GREENSPACE | ERROR | #00AA10 must be ZONE / GREENSPACE |
| BASELINE_B2BD87_CIVIC_SPECIAL | ERROR | #B2BD87 must be UNIQUE_PLACEHOLDER / CIVIC_SPECIAL_BUILDING |
| BASELINE_000000_IGNORE | ERROR | #000000 must be IGNORE |

**VERDICT: MAP25B_WORLDBUILDER_RAW_TILE_ZONE_METADATA_CONTRACT_COMPLETE**
