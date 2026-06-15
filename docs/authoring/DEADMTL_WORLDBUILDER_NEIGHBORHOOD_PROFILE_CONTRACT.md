# MAP-25A: DeadMTL WorldBuilder Neighborhood Profile Contract

## Purpose

Define the neighborhood profile schema for the WorldBuilder system. A profile describes local
generation rules: sidewalks, street corridors, zoning intensity, building families, procedural
fill policy, and future PNG metadata / chunk layer capture hooks.

**Contract only. No generation executed here.**

## Claim boundary

```
profile_status:               AUTHORING_PROFILE_ONLY
runtime proof is NOT claimed: runtime_proven = false
writer readiness is NOT claimed: writer_ready_claim = false
generates_buildings_now:      false
captures_chunk_layers_now:    false
writes_lotpack:               false
writes_worldgen_lua:          false
```

## Sidewalk Model

The street zone owns the full corridor width including sidewalks.

| sidewalk_source | Meaning |
|-----------------|---------|
| `INSIDE_STREET_ZONE` | Sidewalks carved from street corridor; adjacent lots untouched |
| `OUTSIDE_STREET_ZONE` | Sidewalks taken from lot boundary |
| `MIXED` | Context-dependent |
| `NONE` | No sidewalks generated |

Rule: if sidewalk_source is NONE, default_has_sidewalks must be false.
Rule: if default_has_sidewalks is true, at least one of left/right width must be > 0.

## Street Corridor Model

```
corridor = left_sidewalk + left_parking + lanes + right_parking + right_sidewalk + snowbank
```

Changing corridor width regenerates the full interior without eating adjacent lots.

## Zoning Intensity Scale

| Intensity | Meaning |
|-----------|---------|
| EMPTY | No generated buildings unless unique override |
| LIGHT | Sparse, detached/low use, lots of gaps |
| MEDIUM | Normal urban blocks |
| DENSE | Strong urban fabric, repeated buildings |
| CONDENSED | Tight city fabric, little wasted space |
| CROWDED | Maximal packed urban fabric |

## Procedural Fill Policy

- `fill_missing_until_unique_override: true` — WorldBuilder fills until a unique override is assigned.
- `unique_override_priority: UNIQUE_OVERRIDES_WIN` — explicit placements always win over fill.
- `deterministic_seed_policy: PROFILE_PLUS_TILE_COORDINATE` — fill is reproducible per tile.

## PNG Metadata and Chunk Layer Capture (Future)

These capabilities are NOT implemented now. They are marked as future in the profile.

- `zone_color_metadata_status: FUTURE_MAP25B` — zone intent encoded in PNG metadata
- `building_color_metadata_status: FUTURE_MAP25C` — building catalog color references
- `chunk_layer_capture_status: FUTURE` — layered PNG export of ground/roads/vegetation/objects/etc.

## Command

```
deadmtl-validate-worldbuilder-neighborhood-profile \
  --profile     examples\deadmtl-layer-pack\worldbuilder\neighborhoods\deadmtl_baseline_neighborhood_profile.json \
  --output-json .local\deadmtl-authoring\worldbuilder-neighborhood-profile\deadmtl_baseline\deadmtl_baseline.neighborhood_profile.validation.json \
  --output-md   .local\deadmtl-authoring\worldbuilder-neighborhood-profile\deadmtl_baseline\deadmtl_baseline.neighborhood_profile.validation.md \
  --output-csv  .local\deadmtl-authoring\worldbuilder-neighborhood-profile\deadmtl_baseline\deadmtl_baseline.neighborhood_profile.validation.csv \
  --summary     .local\deadmtl-authoring\worldbuilder-neighborhood-profile\deadmtl_baseline\deadmtl_baseline.neighborhood_profile.validation.summary.txt
```

## Helper script

```
examples\deadmtl-layer-pack\scripts\run-deadmtl-worldbuilder-neighborhood-profile-validation.ps1
```

## Output files

| File | Format |
|------|--------|
| `deadmtl_baseline.neighborhood_profile.validation.json` | Full validation result |
| `deadmtl_baseline.neighborhood_profile.validation.md` | Human-readable summary |
| `deadmtl_baseline.neighborhood_profile.validation.csv` | One row per check: rule_id,severity,passed,message |
| `deadmtl_baseline.neighborhood_profile.validation.summary.txt` | Terminal-friendly summary |

## Validation checks

| rule_id | Severity | Description |
|---------|----------|-------------|
| FILE_EXISTS | ERROR | Profile file must exist |
| FORMAT_VALID | ERROR | format must equal the v1 string |
| PROFILE_ID_NON_EMPTY | ERROR | profile_id must be non-empty |
| SIDEWALK_SOURCE_VALID | ERROR | Must be INSIDE_STREET_ZONE / OUTSIDE_STREET_ZONE / MIXED / NONE |
| SIDEWALK_LEFT_WIDTH_NON_NEGATIVE | ERROR | default_left_width_tiles >= 0 |
| SIDEWALK_RIGHT_WIDTH_NON_NEGATIVE | ERROR | default_right_width_tiles >= 0 |
| SIDEWALK_NONE_REQUIRES_NO_SIDEWALKS | ERROR | sidewalk_source NONE → default_has_sidewalks false |
| SIDEWALK_HAS_REQUIRES_WIDTH | ERROR | has_sidewalks true → at least one width > 0 |
| STREET_CORRIDOR_WIDTH_POSITIVE | ERROR | default_corridor_width_tiles > 0 |
| STREET_LANE_WIDTH_POSITIVE | ERROR | default_lane_width_tiles > 0 |
| STREET_LANE_COUNT_NON_NEGATIVE | ERROR | default_lane_count >= 0 |
| RESIDENTIAL_POLICY_EXISTS | ERROR | residential zoning policy present |
| COMMERCIAL_POLICY_EXISTS | ERROR | commercial zoning policy present |
| INDUSTRIAL_POLICY_EXISTS | ERROR | industrial zoning policy present |
| RESIDENTIAL_INTENSITY_VALID | ERROR | Valid intensity value |
| COMMERCIAL_INTENSITY_VALID | ERROR | Valid intensity value |
| INDUSTRIAL_INTENSITY_VALID | ERROR | Valid intensity value |
| PROCEDURAL_FILL_POLICY_EXISTS | ERROR | procedural_fill_policy present |
| PROCEDURAL_FILL_FILLS_UNTIL_OVERRIDE | WARNING | fill_missing_until_unique_override should be true |
| PNG_ZONE_METADATA_IS_FUTURE | ERROR | zone status not IMPLEMENTED |
| PNG_BUILDING_METADATA_IS_FUTURE | ERROR | building status not IMPLEMENTED |
| PNG_CHUNK_CAPTURE_IS_FUTURE | ERROR | chunk capture status not IMPLEMENTED |
| CLAIM_BOUNDARY_EXISTS | ERROR | claim_boundary present |
| CLAIM_WRITES_LOTPACK_FALSE | ERROR | writes_lotpack = false |
| CLAIM_WRITES_WORLDGEN_LUA_FALSE | ERROR | writes_worldgen_lua = false |
| CLAIM_RUNTIME_PROVEN_FALSE | ERROR | runtime_proven = false |
| CLAIM_PUBLIC_PLAYABLE_FALSE | ERROR | public_playable_claim = false |
| CLAIM_WRITER_READY_FALSE | ERROR | writer_ready_claim = false |
| CLAIM_GENERATES_BUILDINGS_FALSE | ERROR | generates_buildings_now = false |
| CLAIM_CAPTURES_CHUNK_LAYERS_FALSE | ERROR | captures_chunk_layers_now = false |

**VERDICT: MAP25A_WORLDBUILDER_NEIGHBORHOOD_PROFILE_CONTRACT_COMPLETE**
