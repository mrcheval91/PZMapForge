# DeadMTL Layer Pack

Authoring skeleton for the DeadMTL/Montreal map.

This is a planning artifact. Not a playable Project Zomboid mod package.
No PZ load test has been performed. No Steam Workshop readiness is claimed.

---

## What this is

A structured layer-painting workspace for the DeadMTL worldgen map.
Each layer is a PNG painted with a palette-mapped color per tile.
The project manifest composes layers in priority order and emits a
`WorldGenOverride.lua` that controls PZ Build 42 world generation.

---

## System boundaries

### System 1 — WorldGen (SUPPORTED by compile-worldgen-project)

Controls biome and major prefab placement at the world level.
These layers are included in `deadmtl_worldgen_project.json`.

| Layer        | Palette key       | Notes                              |
|--------------|-------------------|------------------------------------|
| water        | biome: water      | Open water bodies                  |
| shore        | biome: sand_bank  | Shoreline / riverbanks             |
| parks_forest | biome: birch_forest | Parks, forests, green space      |
| roads_major  | prefab: normal_road_WE_00 | Major arterials and highways |

### System 2 — Binary Overlay (FUTURE — not yet implemented)

Fine-grained building placement, local roads, and props.
These layers exist as transparent placeholders.

| Layer           | Notes                              |
|-----------------|------------------------------------|
| roads_local     | Local streets and alleys           |
| placed_buildings| Individual building footprints     |
| props           | Vehicles, containers, set-dressing |

### System 3 — Zoning Generator (FUTURE — not yet implemented)

Procedural fill for residential, commercial, and industrial zones.
Palette: `palettes/zoning-palette.json` (format not ratified).

| Layer              | Notes                              |
|--------------------|------------------------------------|
| zones_residential  | Low/medium/high density housing    |
| zones_commercial   | Strip malls, dense commercial      |
| zones_industrial   | Light/heavy industry               |

### System 4 — Runtime Society Layer (FUTURE — not yet implemented)

NPC zones, faction territories, ownership, and rent districts.
Palette: `palettes/metadata-palette.json` (format not ratified).

| Layer      | Notes                              |
|------------|------------------------------------|
| npc_zones  | NPC spawn and patrol zones         |
| ownership  | Property ownership boundaries      |

---

## Project coordinates

- origin_x: 10580 (world tile X)
- origin_y: 8200  (world tile Y)
- width: 190 tiles
- height: 140 tiles

---

## Usage

### 1. Generate empty layers (one-time setup)

```powershell
powershell -ExecutionPolicy Bypass -File scripts\generate-empty-layer-pack.ps1
```

This creates transparent PNG stubs for all layers plus sample paint on
the four supported System 1 layers.

### 2. Compile the worldgen project

```powershell
dotnet run --project src\PZMapForge.Cli --configuration Release -- `
  compile-worldgen-project `
  --input examples\deadmtl-layer-pack\deadmtl_worldgen_project.json `
  --output .local\worldgen\deadmtl\worldgen_layers.json
```

### 3. Compile the Lua override

```powershell
dotnet run --project src\PZMapForge.Cli --configuration Release -- `
  compile-worldgen `
  --input .local\worldgen\deadmtl\worldgen_layers.json `
  --output .local\worldgen\deadmtl\WorldGenOverride.lua
```

### 4. Validate the layer pack

```powershell
powershell -ExecutionPolicy Bypass -File scripts\validate-layer-pack.ps1
```

---

## Palette charts and color proof

Generate visual chart sidecars for all palette colors, with proof status
(VISUAL_CONFIRMED / KNOWN_IN_CODE):

```powershell
powershell -ExecutionPolicy Bypass -File scripts\generate-palette-color-charts.ps1
```

Writes to `palettes/`:
- `worldgen-png-palette.chart.png` — visual swatches with hex / type / key / proof status
- `worldgen-png-palette.swatches.txt` — text table with proof status column
- `worldgen-png-palette.layer-guide.txt` — authoring rules and pixel conventions

Run the WorldGen palette proof build (no install):

```powershell
powershell -ExecutionPolicy Bypass -File scripts\run-worldgen-palette-proof-build.ps1
```

With install (-Install installs Lua and clears save folder):

```powershell
powershell -ExecutionPolicy Bypass -File scripts\run-worldgen-palette-proof-build.ps1 -Install
```

See `docs/authoring/DEADMTL_WORLDGEN_PALETTE_PROOF.md` for the full proof chain
and rules for updating proof statuses.

---

## Spawn-centered runtime proof

A pre-built workflow that places all painted features within 50 tiles of the
spawn tile (10650, 8250) so they are visible immediately on first spawn
without walking or searching.

### Features painted (world tile coordinates)

| Layer        | Key                 | World X          | World Y          |
|--------------|---------------------|------------------|------------------|
| roads_major  | normal_road_WE_00   | 10618..10682     | 8248..8252       |
| shore        | sand_bank           | 10632..10668     | 8238..8265       |
| parks_forest | birch_forest        | 10608..10630     | 8238..8272       |
| water        | (base fill)         | entire canvas    | entire canvas    |

Road strip y=8248..8252 passes immediately under the player at spawn tile
10650, 8250, making the WorldGen road visible without any movement.

### Run proof build (no install)

```powershell
powershell -ExecutionPolicy Bypass -File scripts\run-spawn-centered-proof-build.ps1
```

Generates the pack to `.local/deadmtl-authoring/spawn-centered-proof-pack/`,
runs `deadmtl-authoring-build`, and prints the Lua preview and expected
runtime markers. Does NOT write to any PZ game folder.

### Run proof build with install

```powershell
powershell -ExecutionPolicy Bypass -File scripts\run-spawn-centered-proof-build.ps1 -Install
```

Same as above, then:
- Backs up existing `WorldGenOverride.lua`
- Installs new `WorldGenOverride.lua` to the PZ game map folder
- Clears the save folder so PZ regenerates the world
- Byte-checks the installed file (BOM=false, non-ASCII=false)
- Prints VERDICT

### Expected runtime markers

```
PZMAPFORGE_WORLDGENOVERRIDE_MAP_ID=deadmtl_spawn_centered_proof_v1
PZMAPFORGE_WORLDGENOVERRIDE_LOADED
PZMAPFORGE_WORLDGENOVERRIDE_MODULE_COUNT=<N>
```

### Claim boundary

The spawn-centered proof pack is an authoring artifact only.
No public mod packaging is claimed.
No Project Zomboid load test has been performed by any automated tool.
Output must not be treated as a playable map without explicit runtime
verification documented as a human proof step.

---

## Readable terrain proof board

MAP-21B improves on the MAP-21A tiny swatch grid with large terrain patches (25x25+)
and a shaped shoreline for water/sand proof. Avoids isolated water swatches, which
produced a black/unwalkable display anomaly in MAP-21A.

Run readable terrain proof build (no install):

```powershell
powershell -ExecutionPolicy Bypass -File scripts\run-readable-terrain-proof-build.ps1
```

With install (-Install installs Lua and clears save folder):

```powershell
powershell -ExecutionPolicy Bypass -File scripts\run-readable-terrain-proof-build.ps1 -Install
```

See `docs/authoring/DEADMTL_READABLE_TERRAIN_PROOF.md` for the full layout, the
isolated-water anomaly note, and commands.

---

## Small roads and alleys

At 1 px = 1 m authoring scale, Montreal ruelles (3-4 m wide) are directly paintable
as raster features. Wider arterials and collectors are also viable.

**Current WorldGen road support is limited.** Only two prefab keys are runtime-proven:
- `normal_road_WE_00` (west-east strip)
- `highway_NS_00` (north-south strip)

Local streets and ruelles/alleys are currently marked **SYSTEM_2_REQUIRED** unless
additional road prefab keys are discovered at runtime (see MAP-22B planning task).

Generate transparent road layer placeholders (authoring scaffold, no compile):

```powershell
powershell -ExecutionPolicy Bypass -File scripts\generate-road-layer-placeholders.ps1
```

See `docs/authoring/DEADMTL_SMALL_ROADS_ALLEYS_CONTRACT.md` for the full road hierarchy,
authoring widths, layer contract, and future proof task plan (MAP-22B through MAP-22D).

See `road-layers-contract.json` for the machine-readable layer status contract.

---

## WorldGen prefab discovery

MAP-22B runtime harvest performed: 2026-06-14.
Result: `worldgen.prefabs` in PZ Build 42 contains exactly two keys:
- `highway_NS_00` (VISUAL_CONFIRMED)
- `normal_road_WE_00` (VISUAL_CONFIRMED)

No additional road, alley, local street, turn, or intersection prefab keys were found.
MAP-22C proof board is not needed. Small roads and alleys remain SYSTEM_2_REQUIRED.
Next path is MAP-22D (System 2 static tile overlay).

Full result sidecar: `docs/authoring/MAP22B_PREFAB_DUMP_RESULT.txt`

See `docs/authoring/DEADMTL_WORLDGEN_PREFAB_DISCOVERY.md` for the full discovery chain.

The commands below are preserved for reference (probe already run):

Generate and preview probe Lua (no install):

```powershell
powershell -ExecutionPolicy Bypass -File scripts\run-worldgen-prefab-dump.ps1
```

Install probe to PZ game folder and clear save:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\run-worldgen-prefab-dump.ps1 -Install
```

After launching PZ and exiting, harvest results from PZ logs:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\harvest-worldgen-prefab-dump-logs.ps1
```

Results written to: `.local/deadmtl-authoring/worldgen-prefab-dump/prefab-dump-harvest.txt`

See `docs/authoring/DEADMTL_WORLDGEN_PREFAB_DISCOVERY.md` for the full discovery-to-proof
chain and claim boundary.

---

## System 2 static roads and alleys overlay

MAP-22C closed the WorldGen small-road path. `worldgen.prefabs` in PZ Build 42 exposes
only `highway_NS_00` and `normal_road_WE_00`. No alley, ruelle, local street, service
lane, turn, or intersection prefab key exists.

System 2 (static tile overlay) is now the path for:
- local streets (5-7 px)
- Montreal ruelles and alleys (3-4 px)
- service lanes (2-3 px)
- parking access (3-5 px)
- pedestrian cuts (1-2 px)
- intersections, turns, and dead ends

This is contract-only for now. No .lotpack writer is implemented. No runtime proof is claimed.

Generate System 2 placeholder layers (no compile, no install):

```powershell
powershell -ExecutionPolicy Bypass -File scripts\generate-system2-static-road-placeholders.ps1
```

See `docs/authoring/DEADMTL_SYSTEM2_STATIC_ROADS_ALLEYS_OVERLAY.md` for the full contract,
road classes, authoring widths, and claim boundary.

See `system2-static-road-overlay-contract.json` for the machine-readable layer contract.

See `palettes/system2-static-road-intent-palette.json` for intent color definitions.

---

## System 2 road intent extraction

MAP-22E adds an extractor that reads the System 2 PNG layers and emits structured JSON
describing each opaque pixel as a typed road-intent record.

The extractor does NOT write `.lotpack`. It does NOT write `WorldGenOverride.lua`.
It does NOT claim runtime proof. Output is an inspectable JSON artifact only.

Extract System 2 road intent from authored PNG layers:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\run-system2-static-road-extract.ps1
```

Output (under `.local/`):
- `system2-road-extract.json` -- structured layer summary with pixel runs and node records
- `system2-road-extract-summary.txt` -- human-readable summary with totals and claim boundary

CLI command directly:

```powershell
dotnet run --project src\PZMapForge.Cli --configuration Release -- `
  system2-extract-static-roads `
  --input   examples\deadmtl-layer-pack\system2-static-road-overlay-contract.json `
  --palette examples\deadmtl-layer-pack\palettes\system2-static-road-intent-palette.json `
  --root    examples\deadmtl-layer-pack `
  --output  .local\deadmtl-authoring\system2-road-extract\system2-road-extract.json `
  --summary .local\deadmtl-authoring\system2-road-extract\system2-road-extract-summary.txt
```

See `docs/authoring/DEADMTL_SYSTEM2_STATIC_ROAD_INTENT_EXTRACT.md` for full JSON shape,
coordinate conventions, and claim boundary.

---

## System 2 road sample extraction

MAP-22F proves that authored non-empty PNG masks produce the expected run and node records
in the extract JSON. This is still extract-only, not runtime proof.

The sample pack contains painted pixels at Montreal ruelle authoring scale (1 px = 1 m):
- local street strip, alley strip, service lane, parking access, pedestrian cut
- three road nodes: intersection_node, road_turn_node, dead_end_node

Does NOT write `.lotpack`. Does NOT write `WorldGenOverride.lua`. No runtime proof claimed.

Generate sample and extract:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\run-system2-static-road-sample-extract.ps1
```

Output (under `.local/`):
- `.local\deadmtl-authoring\system2-static-road-sample-extract\system2_static_road_sample_extract.json`
- `.local\deadmtl-authoring\system2-static-road-sample-extract\system2_static_road_sample_extract.summary.txt`

Generate sample pack only (no extraction):

```powershell
powershell -ExecutionPolicy Bypass -File scripts\generate-system2-static-road-sample.ps1
```

See `docs/authoring/DEADMTL_SYSTEM2_STATIC_ROAD_SAMPLE_EXTRACT.md` for expected layer content,
intent types, node coordinates, and claim boundary.

---

## System 2 static road placement plan

MAP-22G converts the System 2 extract JSON into a placement plan: one record per intended
world tile with abstract roles (road_surface, pedestrian_cut, road_node).

This is plan-only. No runtime proof is claimed. No lotpack files are written.
No WorldGenOverride.lua is written.

Generate placement plan from MAP-22F sample extract:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\run-system2-static-road-placement-plan.ps1
```

Output (under `.local/`):
- `.local\deadmtl-authoring\system2-static-road-placement-plan\system2_static_road_placement_plan.json`
- `.local\deadmtl-authoring\system2-static-road-placement-plan\system2_static_road_placement_plan.summary.txt`

See `docs/authoring/DEADMTL_SYSTEM2_STATIC_ROAD_PLACEMENT_PLAN.md` for JSON shape,
intent-to-role mapping, duplicate handling, and claim boundary.

---

## System 2 static road tile-family plan

MAP-22H maps each placement record to a candidate tile family. This is metadata only.
No PZ tile IDs are selected. No binary writes are performed. No runtime proof is claimed.

Each record carries confidence: LOW_METADATA_ONLY.

Does NOT write lotpack files. Does NOT write WorldGenOverride.lua. No runtime proof claimed.

Generate tile-family plan from MAP-22G placement plan:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\run-system2-static-road-tile-family-plan.ps1
```

Output (under `.local/`):
- `.local\deadmtl-authoring\system2-static-road-tile-family-plan\system2_static_road_tile_family_plan.json`
- `.local\deadmtl-authoring\system2-static-road-tile-family-plan\system2_static_road_tile_family_plan.summary.txt`

See `docs/authoring/DEADMTL_SYSTEM2_STATIC_ROAD_TILE_FAMILY_PLAN.md` for the intent-to-family
mapping table, confidence semantics, JSON shape, and claim boundary.

---

## System 2 static road tile-family survey

MAP-22I defines the survey contract for resolving candidate families to actual PZ tile
candidates. All 6 families default to UNRESOLVED_NEEDS_TILE_SURVEY. No tile IDs are
selected in this task.

Does NOT write lotpack files. Does NOT write WorldGenOverride.lua. No runtime proof claimed.

Generate survey contract from MAP-22H tile-family plan:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\run-system2-static-road-tile-family-survey.ps1
```

Output (under `.local/`):
- `.local\deadmtl-authoring\system2-static-road-tile-family-survey\system2_static_road_tile_family_survey.json`
- `.local\deadmtl-authoring\system2-static-road-tile-family-survey\system2_static_road_tile_family_survey.summary.txt`

See `docs/authoring/DEADMTL_SYSTEM2_STATIC_ROAD_TILE_FAMILY_SURVEY.md` for the resolution
status table, JSON shape, and claim boundary.

---

## System 2 static road local tile survey

MAP-22J scans the local PZ install root for text-based tile definition files and
produces a candidate list (confidence: LOCAL_TEXT_MATCH_ONLY) for each candidate
family. Candidates are not runtime-proven and are not final writer choices.

Does NOT write lotpack files. Does NOT write WorldGenOverride.lua. No runtime proof claimed.

Generate local tile survey from MAP-22I survey contract:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\run-system2-static-road-local-tile-survey.ps1
```

If the PZ install root is not at the default path, pass it explicitly:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\run-system2-static-road-local-tile-survey.ps1 -PzRoot "D:\path\to\ProjectZomboid"
```

Output (under `.local/`):
- `.local\deadmtl-authoring\system2-static-road-local-tile-survey\system2_static_road_local_tile_survey.json`
- `.local\deadmtl-authoring\system2-static-road-local-tile-survey\system2_static_road_local_tile_survey.summary.txt`

See `docs/authoring/DEADMTL_SYSTEM2_STATIC_ROAD_LOCAL_TILE_SURVEY.md` for the search term
table, resolution statuses, confidence semantics, and claim boundary.

---

## System 2 static road tile candidate shortlist

MAP-22K reduces MAP-22J's text-match candidates into a ranked shortlist per candidate
family using deterministic string scoring. Default: top 25 per family.
Confidence: LOCAL_TEXT_MATCH_RANKED_ONLY. Candidates are not final writer choices.

Does NOT write lotpack files. Does NOT write WorldGenOverride.lua. No runtime proof claimed.

Generate shortlist from MAP-22J local tile survey:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\run-system2-static-road-tile-candidate-shortlist.ps1
```

Output (under `.local/`):
- `.local\deadmtl-authoring\system2-static-road-tile-candidate-shortlist\system2_static_road_tile_candidate_shortlist.json`
- `.local\deadmtl-authoring\system2-static-road-tile-candidate-shortlist\system2_static_road_tile_candidate_shortlist.summary.txt`

See `docs/authoring/DEADMTL_SYSTEM2_STATIC_ROAD_TILE_CANDIDATE_SHORTLIST.md` for the scoring
table, resolution statuses, confidence semantics, and claim boundary.

---

## System 2 static road tile candidate review packet

MAP-22L turns the MAP-22K shortlist into a human-readable review packet: Markdown table,
CSV, and JSON. Every item starts as NEEDS_MANUAL_REVIEW. No candidate is approved
automatically. Confidence: LOCAL_TEXT_MATCH_RANKED_ONLY.

Does NOT write lotpack files. Does NOT write WorldGenOverride.lua. No runtime proof claimed.

Generate review packet from MAP-22K shortlist:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\run-system2-static-road-tile-candidate-review.ps1
```

Output (under `.local/`):
- `.local\deadmtl-authoring\system2-static-road-tile-candidate-review\system2_static_road_tile_candidate_review.json`
- `.local\deadmtl-authoring\system2-static-road-tile-candidate-review\system2_static_road_tile_candidate_review.md`
- `.local\deadmtl-authoring\system2-static-road-tile-candidate-review\system2_static_road_tile_candidate_review.csv`
- `.local\deadmtl-authoring\system2-static-road-tile-candidate-review\system2_static_road_tile_candidate_review.summary.txt`

See `docs/authoring/DEADMTL_SYSTEM2_STATIC_ROAD_TILE_CANDIDATE_REVIEW.md` for review statuses,
output format, and claim boundary.

---

## System 2 static road tile candidate review apply

MAP-22M applies a human-edited decisions CSV back into a structured reviewed-candidates file.
Each item may be set to APPROVED_BY_HUMAN_REVIEW, REJECTED_BY_HUMAN_REVIEW, or left as
NEEDS_MANUAL_REVIEW. Human approval is NOT runtime validation. Human approval is NOT writer readiness.

Default run uses the unedited MAP-22L CSV as decisions input, so all 113 items remain NEEDS_MANUAL_REVIEW
until a human edits the CSV to change statuses.

Does NOT write lotpack files. Does NOT write WorldGenOverride.lua. No runtime proof claimed.

Generate applied review packet:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\run-system2-static-road-tile-candidate-review-apply.ps1
```

Output (under `.local/`):
- `.local\deadmtl-authoring\system2-static-road-tile-candidate-review-applied\system2_static_road_tile_candidate_review_applied.json`
- `.local\deadmtl-authoring\system2-static-road-tile-candidate-review-applied\system2_static_road_tile_candidate_review_applied.md`
- `.local\deadmtl-authoring\system2-static-road-tile-candidate-review-applied\system2_static_road_tile_candidate_review_applied.csv`
- `.local\deadmtl-authoring\system2-static-road-tile-candidate-review-applied\system2_static_road_tile_candidate_review_applied.summary.txt`

See `docs/authoring/DEADMTL_SYSTEM2_STATIC_ROAD_TILE_CANDIDATE_REVIEW_APPLY.md` for review statuses,
CSV format, matching rules, and claim boundary.

---

## System 2 static road human-approved tile candidates

MAP-22N reads the MAP-22M applied review JSON and produces a clean manifest split into three buckets:
approved, rejected, and still-pending candidates. This is an audit and selection layer only.

Human approval is NOT runtime validation. Human approval is NOT writer readiness.
Default run (unedited CSV): approved=0, rejected=0, pending=113.

Does NOT write lotpack files. Does NOT write WorldGenOverride.lua. No runtime proof claimed.

Generate human-approved candidate manifest:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\run-system2-static-road-human-approved-tile-candidates.ps1
```

Output (under `.local/`):
- `.local\deadmtl-authoring\system2-static-road-human-approved-tile-candidates\system2_static_road_human_approved_tile_candidates.json`
- `.local\deadmtl-authoring\system2-static-road-human-approved-tile-candidates\system2_static_road_human_approved_tile_candidates.md`
- `.local\deadmtl-authoring\system2-static-road-human-approved-tile-candidates\system2_static_road_human_approved_tile_candidates.csv`
- `.local\deadmtl-authoring\system2-static-road-human-approved-tile-candidates\system2_static_road_human_approved_tile_candidates.summary.txt`

See `docs/authoring/DEADMTL_SYSTEM2_STATIC_ROAD_HUMAN_APPROVED_TILE_CANDIDATES.md` for bucket
definitions, CSV format, and claim boundary.

## Raw 256x256 map tile inspection

MAP-23A provides a safe inspection and intake layer for raw 256x256 authoring map tiles.
Reads the PNG, computes SHA256, counts pixels, validates 256x256 size, produces top N colors,
and compares against worldgen and System 2 palettes to find unknown colors.
Inspection only. No compilation. No runtime proof. No writer readiness.

Does NOT write lotpack files. Does NOT write WorldGenOverride.lua.

Inspect raw tile:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\run-deadmtl-raw-map-tile-inspection.ps1
```

Output (under `.local/`):
- `.local\deadmtl-authoring\raw-map-tile-inspection\map_00\map_00.raw_tile_inspection.json`
- `.local\deadmtl-authoring\raw-map-tile-inspection\map_00\map_00.raw_tile_inspection.md`
- `.local\deadmtl-authoring\raw-map-tile-inspection\map_00\map_00.raw_tile_colors.csv`
- `.local\deadmtl-authoring\raw-map-tile-inspection\map_00\map_00.raw_tile_inspection.summary.txt`

See `docs/authoring/DEADMTL_RAW_256_MAP_TILE_INSPECTION.md` for field definitions and claim boundary.

---

## Raw tile palette mapping contract

MAP-23B classifies each source color from the MAP-23A inspection as either exactly matched
to an existing palette entry (worldgen or System 2 static road) or unmapped and requiring
a human decision. Near-match suggestions are provided where RGB distance ≤ 32.
Mapping contract only. No compilation. No runtime proof. No writer readiness.

Does NOT write lotpack files. Does NOT write WorldGenOverride.lua.

Requires MAP-23A output to be present first. Then run:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\run-deadmtl-raw-map-tile-palette-mapping.ps1
```

Output (under `.local/`):
- `.local\deadmtl-authoring\raw-map-tile-palette-mapping\map_00\map_00.raw_tile_palette_mapping.json`
- `.local\deadmtl-authoring\raw-map-tile-palette-mapping\map_00\map_00.raw_tile_palette_mapping.md`
- `.local\deadmtl-authoring\raw-map-tile-palette-mapping\map_00\map_00.raw_tile_palette_mapping.csv`
- `.local\deadmtl-authoring\raw-map-tile-palette-mapping\map_00\map_00.raw_tile_palette_mapping.summary.txt`

See `docs/authoring/DEADMTL_RAW_TILE_PALETTE_MAPPING_CONTRACT.md` for field definitions and claim boundary.

---

## Vanilla building source discovery

MAP-24A scans all known PZ install, modding-tools, user Zomboid, and workspace roots.
Discovery only — no building extraction claimed, no editable catalogue claimed, no runtime proof.

Does NOT write lotpack files. Does NOT write WorldGenOverride.lua.

Run:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\run-deadmtl-vanilla-building-source-discovery.ps1
```

Output (under `.local/`):
- `.local\deadmtl-authoring\vanilla-building-source-discovery\vanilla_building_source_discovery.json`
- `.local\deadmtl-authoring\vanilla-building-source-discovery\vanilla_building_source_discovery.md`
- `.local\deadmtl-authoring\vanilla-building-source-discovery\vanilla_building_source_discovery.csv`
- `.local\deadmtl-authoring\vanilla-building-source-discovery\vanilla_building_source_discovery.summary.txt`

See `docs/authoring/DEADMTL_VANILLA_BUILDING_SOURCE_DISCOVERY.md` for field definitions and claim boundary.

---

## WorldBuilder neighborhood profile contract

MAP-25A introduces the neighborhood profile schema. Contract only — no terrain generation, no
building placement, no lotpack writing, no WorldGenOverride.lua, no runtime proof.

Profile: `examples/deadmtl-layer-pack/worldbuilder/neighborhoods/deadmtl_baseline_neighborhood_profile.json`

Run:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\run-deadmtl-worldbuilder-neighborhood-profile-validation.ps1
```

Output (under `.local/`):
- `.local\deadmtl-authoring\worldbuilder-neighborhood-profile\deadmtl_baseline\deadmtl_baseline.neighborhood_profile.validation.json`
- `.local\deadmtl-authoring\worldbuilder-neighborhood-profile\deadmtl_baseline\deadmtl_baseline.neighborhood_profile.validation.md`
- `.local\deadmtl-authoring\worldbuilder-neighborhood-profile\deadmtl_baseline\deadmtl_baseline.neighborhood_profile.validation.csv`
- `.local\deadmtl-authoring\worldbuilder-neighborhood-profile\deadmtl_baseline\deadmtl_baseline.neighborhood_profile.validation.summary.txt`

See `docs/authoring/DEADMTL_WORLDBUILDER_NEIGHBORHOOD_PROFILE_CONTRACT.md` for full field definitions and validation rules.

---

## WorldBuilder raw tile zone metadata contract

MAP-25B maps raw PNG colors from map_00.png to WorldBuilder semantic intents.
Metadata only — no terrain generation, no sidewalk generation, no lot subdivision,
no building placement, no fence placement, no lotpack writing, no runtime proof.

Metadata: `examples/deadmtl-layer-pack/worldbuilder/tiles/map_00.zone_metadata.json`

Colors: `#7200FF` RESIDENTIAL, `#FF6600` MAIN_ROAD (sidewalk_eligible), `#F000FF` BACK_ALLEY
(no sidewalks), `#42CCFF` COMMERCIAL, `#00AA10` GREENSPACE, `#B2BD87` CIVIC_SPECIAL_BUILDING,
`#000000` IGNORE.

Run:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\run-deadmtl-worldbuilder-zone-metadata-validation.ps1
```

Output (under `.local/`):

- `.local\deadmtl-authoring\worldbuilder-zone-metadata\map_00\map_00.zone_metadata.validation.json`
- `.local\deadmtl-authoring\worldbuilder-zone-metadata\map_00\map_00.zone_metadata.validation.md`
- `.local\deadmtl-authoring\worldbuilder-zone-metadata\map_00\map_00.zone_metadata.validation.csv`
- `.local\deadmtl-authoring\worldbuilder-zone-metadata\map_00\map_00.zone_metadata.validation.summary.txt`

See `docs/authoring/DEADMTL_WORLDBUILDER_RAW_TILE_ZONE_METADATA_CONTRACT.md` for full field definitions and validation rules.

---

## WorldBuilder lot subdivision plan contract

MAP-25C derives a future subdivision plan from the MAP-25B zone metadata.
Contract only — no terrain generation, no lot subdivision executed, no building placement,
no sidewalk generation, no fence placement, no lotpack writing, no worldgen override file,
no runtime proof. Not writer-ready.

Run:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\run-deadmtl-worldbuilder-lot-subdivision-plan.ps1
```

Output (under `.local/`):

- `.local\deadmtl-authoring\worldbuilder-lot-subdivision-plan\map_00\map_00.lot_subdivision_plan.json`
- `.local\deadmtl-authoring\worldbuilder-lot-subdivision-plan\map_00\map_00.lot_subdivision_plan.md`
- `.local\deadmtl-authoring\worldbuilder-lot-subdivision-plan\map_00\map_00.lot_subdivision_plan.csv`
- `.local\deadmtl-authoring\worldbuilder-lot-subdivision-plan\map_00\map_00.lot_subdivision_plan.summary.txt`

See `docs/authoring/DEADMTL_WORLDBUILDER_LOT_SUBDIVISION_PLAN_CONTRACT.md` for plan item fields,
subdivision actions by color, frontage/back-alley/sidewalk policies, and claim boundary.

---

## WorldBuilder sidewalk generation plan contract

MAP-25D derives a future sidewalk generation plan from the neighborhood profile, zone metadata,
and lot subdivision plan. Contract only — no sidewalk generation, no terrain mutation, no PNG
changes, no lotpack writing, no worldgen override file, no runtime proof. Not writer-ready.

Only `MAIN_ROAD` (`#FF6600`) corridors with `sidewalk_eligible = true` plan sidewalks.
`BACK_ALLEY` (`#F000FF`) corridors always plan `PLAN_NO_SIDEWALKS`.
Sidewalk dimensions come from the active neighborhood profile (`deadmtl_baseline`):
left_width_tiles=2, right_width_tiles=2, sidewalk_source=INSIDE_STREET_ZONE.

Requires MAP-25C lot subdivision plan to be present first.

Run:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\run-deadmtl-worldbuilder-sidewalk-generation-plan.ps1
```

Output (under `.local/`):

- `.local\deadmtl-authoring\worldbuilder-sidewalk-generation-plan\map_00\map_00.sidewalk_generation_plan.json`
- `.local\deadmtl-authoring\worldbuilder-sidewalk-generation-plan\map_00\map_00.sidewalk_generation_plan.md`
- `.local\deadmtl-authoring\worldbuilder-sidewalk-generation-plan\map_00\map_00.sidewalk_generation_plan.csv`
- `.local\deadmtl-authoring\worldbuilder-sidewalk-generation-plan\map_00\map_00.sidewalk_generation_plan.summary.txt`

See `docs/authoring/DEADMTL_WORLDBUILDER_SIDEWALK_GENERATION_PLAN_CONTRACT.md` for eligibility rules,
back-alley rule, sidewalk source semantics, and claim boundary.

---

## WorldBuilder building selection policy plan contract

MAP-25E derives a future building selection policy from the neighborhood profile, zone metadata,
lot subdivision plan, and sidewalk generation plan. Contract only — no building generation, no
building placement, no lot subdivision, no sidewalk generation, no fence placement, no lotpack
writing, no worldgen override file, no runtime proof. Not writer-ready.

No concrete building id is selected now. `concrete_building_ids_selected_now_count: 0`.

Allowed families per zone:
- RESIDENTIAL: duplex, triplex, plex_block, apartment_lowrise
- COMMERCIAL: depanneur, pharmacy, restaurant, main_street_storefront, office_small
- CIVIC_SPECIAL_BUILDING: government, library, community_center, institutional, special_building

Requires MAP-25C lot subdivision plan and MAP-25D sidewalk generation plan to be present first.

Run:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\run-deadmtl-worldbuilder-building-selection-policy-plan.ps1
```

Output (under `.local/`):

- `.local\deadmtl-authoring\worldbuilder-building-selection-policy-plan\map_00\map_00.building_selection_policy_plan.json`
- `.local\deadmtl-authoring\worldbuilder-building-selection-policy-plan\map_00\map_00.building_selection_policy_plan.md`
- `.local\deadmtl-authoring\worldbuilder-building-selection-policy-plan\map_00\map_00.building_selection_policy_plan.csv`
- `.local\deadmtl-authoring\worldbuilder-building-selection-policy-plan\map_00\map_00.building_selection_policy_plan.summary.txt`

See `docs/authoring/DEADMTL_WORLDBUILDER_BUILDING_SELECTION_POLICY_PLAN_CONTRACT.md` for selection
actions by color, allowed families, fit policy, frontage/facade rules, and claim boundary.

---

## WorldBuilder generation dependency manifest contract

MAP-25F ties MAP-25A through MAP-25E into one ordered, auditable pipeline record. It reads each
input file, verifies the format field, records all 5 steps in dependency order, and produces
10 dependency edges between them. Contract only — no terrain generation, no lot subdivision,
no sidewalk generation, no building placement, no fences, no lotpack writing, no worldgen
override file, no runtime proof. Not writer-ready.

Generate the generation dependency manifest:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\run-deadmtl-worldbuilder-generation-dependency-manifest.ps1
```

Output (under `.local/`):

- `.local\deadmtl-authoring\worldbuilder-generation-dependency-manifest\map_00\map_00.generation_dependency_manifest.json`
- `.local\deadmtl-authoring\worldbuilder-generation-dependency-manifest\map_00\map_00.generation_dependency_manifest.md`
- `.local\deadmtl-authoring\worldbuilder-generation-dependency-manifest\map_00\map_00.generation_dependency_manifest.csv`
- `.local\deadmtl-authoring\worldbuilder-generation-dependency-manifest\map_00\map_00.generation_dependency_manifest.summary.txt`

See `docs/authoring/DEADMTL_WORLDBUILDER_GENERATION_DEPENDENCY_MANIFEST_CONTRACT.md` for step
definitions, dependency edges, totals, and claim boundary.

---

## WorldBuilder future world layout plan contract

MAP-25G combines MAP-25A through MAP-25F into one auditable future layout planning object.
It records what each zone, street corridor, and unique placeholder will eventually produce,
without executing any of it. Contract only — no terrain generation, no lot subdivision,
no sidewalk generation, no building placement, no fences, no lotpack writing, no worldgen
override file, no concrete geometry created, layout not materialized, no runtime proof.
Not writer-ready.

Generate the future world layout plan:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\run-deadmtl-worldbuilder-future-world-layout-plan.ps1
```

Output (under `.local/`):

- `.local\deadmtl-authoring\worldbuilder-future-world-layout-plan\map_00\map_00.future_world_layout_plan.json`
- `.local\deadmtl-authoring\worldbuilder-future-world-layout-plan\map_00\map_00.future_world_layout_plan.md`
- `.local\deadmtl-authoring\worldbuilder-future-world-layout-plan\map_00\map_00.future_world_layout_plan.csv`
- `.local\deadmtl-authoring\worldbuilder-future-world-layout-plan\map_00\map_00.future_world_layout_plan.summary.txt`

See `docs/authoring/DEADMTL_WORLDBUILDER_FUTURE_WORLD_LAYOUT_PLAN_CONTRACT.md` for layout
components, future execution requirements, totals, and claim boundary.

---

## WorldBuilder concrete geometry preflight contract

MAP-25H inspects the MAP-25G future world layout plan and produces a checklist of all concrete
geometry that must exist before the world layout can be materialized. Contract only — no terrain
generation, no lot subdivision, no sidewalk geometry, no road geometry, no building placement,
no fences, no lotpack writing, no worldgen override file, no concrete geometry created, layout
not materialized, no runtime proof. Not writer-ready.

9 preflight requirements: 7 geometry, 1 writer, 1 runtime. All blocked. can_execute_now: 0.

Generate the concrete geometry preflight:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\run-deadmtl-worldbuilder-concrete-geometry-preflight.ps1
```

Output (under `.local/`):

- `.local\deadmtl-authoring\worldbuilder-concrete-geometry-preflight\map_00\map_00.concrete_geometry_preflight.json`
- `.local\deadmtl-authoring\worldbuilder-concrete-geometry-preflight\map_00\map_00.concrete_geometry_preflight.md`
- `.local\deadmtl-authoring\worldbuilder-concrete-geometry-preflight\map_00\map_00.concrete_geometry_preflight.csv`
- `.local\deadmtl-authoring\worldbuilder-concrete-geometry-preflight\map_00\map_00.concrete_geometry_preflight.summary.txt`

See `docs/authoring/DEADMTL_WORLDBUILDER_CONCRETE_GEOMETRY_PREFLIGHT_CONTRACT.md` for preflight
requirements, totals, and claim boundary.

---

## WorldBuilder geometry primitive schema contract

MAP-25I defines the geometry primitive types, coordinate space, units, geometry sources,
validation rules, and future consumers that the WorldBuilder will eventually use when creating
concrete geometry. Schema definition only — no terrain generation, no lot subdivision, no
sidewalk geometry, no road geometry, no building placement, no fences, no lotpack writing,
no worldgen override file, no concrete geometry created, layout not materialized, no runtime
proof. Not writer-ready.

10 primitive types defined. All creation_status: NOT_CREATED. created_geometry_count: 0.

Generate the geometry primitive schema:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\run-deadmtl-worldbuilder-geometry-primitive-schema.ps1
```

Output (under `.local/`):

- `.local\deadmtl-authoring\worldbuilder-geometry-primitive-schema\map_00\map_00.geometry_primitive_schema.json`
- `.local\deadmtl-authoring\worldbuilder-geometry-primitive-schema\map_00\map_00.geometry_primitive_schema.md`
- `.local\deadmtl-authoring\worldbuilder-geometry-primitive-schema\map_00\map_00.geometry_primitive_schema.csv`
- `.local\deadmtl-authoring\worldbuilder-geometry-primitive-schema\map_00\map_00.geometry_primitive_schema.summary.txt`

See `docs/authoring/DEADMTL_WORLDBUILDER_GEOMETRY_PRIMITIVE_SCHEMA_CONTRACT.md` for primitive
type definitions, coordinate contract, validation rules, and claim boundary.

---

## WorldBuilder source mask region extraction contract

MAP-25J reads the raw `map_00.png` color map, counts pixels by color, computes pixel-space
bounding boxes, and binds each color region to its zone metadata entry. Source mask regions
only — no concrete lot/road/sidewalk/building geometry is created from these masks.

All 7 colors: MASK_REGION primitive, SOURCE_MASK_ONLY_NO_GEOMETRY_CREATED status.
No terrain generation, no lot subdivision, no lotpack writing, no worldgen override file,
no concrete geometry created, layout not materialized, no runtime proof. Not writer-ready.

Generate the source mask region extraction:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\run-deadmtl-worldbuilder-source-mask-region-extraction.ps1
```

Output (under `.local/`):

- `.local\deadmtl-authoring\worldbuilder-source-mask-region-extraction\map_00\map_00.source_mask_region_extraction.json`
- `.local\deadmtl-authoring\worldbuilder-source-mask-region-extraction\map_00\map_00.source_mask_region_extraction.md`
- `.local\deadmtl-authoring\worldbuilder-source-mask-region-extraction\map_00\map_00.source_mask_region_extraction.csv`
- `.local\deadmtl-authoring\worldbuilder-source-mask-region-extraction\map_00\map_00.source_mask_region_extraction.summary.txt`

See `docs/authoring/DEADMTL_WORLDBUILDER_SOURCE_MASK_REGION_EXTRACTION_CONTRACT.md` for mask
region fields, pixel count totals, future geometry requirement mapping, and claim boundary.

---

## WorldBuilder connected component extraction contract

MAP-25K splits each MAP-25J source mask region into individual 4-way connected pixel islands
(components). 45 components extracted across 7 parent colors using 4-way connectivity only
(no diagonal). Components are pixel-space islands — not concrete lot polygons, road geometries,
building slots, sidewalk geometry, or fence geometry.

No terrain generation, no lot subdivision, no concrete geometry created, no lotpack writing,
no worldgen override file, layout not materialized, no runtime proof. Not writer-ready.

Generate the connected component extraction:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\run-deadmtl-worldbuilder-connected-component-extraction.ps1
```

Output (under `.local/`):

- `.local\deadmtl-authoring\worldbuilder-connected-component-extraction\map_00\map_00.connected_component_extraction.json`
- `.local\deadmtl-authoring\worldbuilder-connected-component-extraction\map_00\map_00.connected_component_extraction.md`
- `.local\deadmtl-authoring\worldbuilder-connected-component-extraction\map_00\map_00.connected_component_extraction.csv`
- `.local\deadmtl-authoring\worldbuilder-connected-component-extraction\map_00\map_00.connected_component_extraction.summary.txt`

See `docs/authoring/DEADMTL_WORLDBUILDER_CONNECTED_COMPONENT_EXTRACTION_CONTRACT.md` for component
fields, connectivity contract, component count totals, and claim boundary.

---

## WorldBuilder component intent classification contract

MAP-25L classifies the 45 connected components from MAP-25K into 7 future geometry intent
buckets (RESIDENTIAL_LOT_BLOCK, COMMERCIAL_LOT_BLOCK, MAIN_ROAD_CORRIDOR, BACK_ALLEY_CORRIDOR,
GREENSPACE_MASS, CIVIC_PLACEHOLDER, IGNORE_BORDER). Intent classification only — not concrete
lot polygons, road geometries, building slots, sidewalk geometry, or fence geometry.

No terrain generation, no lot subdivision, no concrete geometry created, no lotpack writing,
no worldgen override file, layout not materialized, no runtime proof. Not writer-ready.

Generate the component intent classification:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\run-deadmtl-worldbuilder-component-intent-classification.ps1
```

Output (under `.local/`):

- `.local\deadmtl-authoring\worldbuilder-component-intent-classification\map_00\map_00.component_intent_classification.json`
- `.local\deadmtl-authoring\worldbuilder-component-intent-classification\map_00\map_00.component_intent_classification.md`
- `.local\deadmtl-authoring\worldbuilder-component-intent-classification\map_00\map_00.component_intent_classification.csv`
- `.local\deadmtl-authoring\worldbuilder-component-intent-classification\map_00\map_00.component_intent_classification.summary.txt`

See `docs/authoring/DEADMTL_WORLDBUILDER_COMPONENT_INTENT_CLASSIFICATION_CONTRACT.md` for intent
bucket definitions, classification rules, totals, and claim boundary.

---

## MAP-25M: WorldBuilder Component Adjacency Graph

MAP-25M reads the raw PNG, reproduces the 45 connected component labels from MAP-25K, binds them
to MAP-25L intent records, and emits undirected 4-way pixel adjacency edges between touching
components. 82 adjacency edges are detected across 7 relationship types.

This is adjacency graph only. It is NOT concrete lot polygons, road geometries, building slots,
sidewalk geometry, or fence geometry. No generation of any kind occurs.

No terrain generation, no lot subdivision, no concrete geometry created, no lotpack writing,
no worldgen override file, layout not materialized, no runtime proof. Not writer-ready.

Generate the component adjacency graph:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\run-deadmtl-worldbuilder-component-adjacency-graph.ps1
```

Output (under `.local/`):

- `.local\deadmtl-authoring\worldbuilder-component-adjacency-graph\map_00\map_00.component_adjacency_graph.json`
- `.local\deadmtl-authoring\worldbuilder-component-adjacency-graph\map_00\map_00.component_adjacency_graph.md`
- `.local\deadmtl-authoring\worldbuilder-component-adjacency-graph\map_00\map_00.component_adjacency_graph.csv`
- `.local\deadmtl-authoring\worldbuilder-component-adjacency-graph\map_00\map_00.component_adjacency_graph.summary.txt`

See `docs/authoring/DEADMTL_WORLDBUILDER_COMPONENT_ADJACENCY_GRAPH_CONTRACT.md` for adjacency
detection rules, relationship classification, edge totals, and claim boundary.

---

## MAP-25N: WorldBuilder Adjacency Planning Candidate Extraction

MAP-25N reads the MAP-25M component adjacency graph (82 edges) and converts each edge into a
planning candidate record with candidate type, priority, family, future geometry requirement ID,
and blocked-by requirements. 82 candidate records are produced: 71 actionable and 11 ignored.

This is planning candidate extraction only. It is NOT lot polygons, road geometries, frontage
geometry, rear access geometry, building slots, sidewalk geometry, fence geometry, or any
materialized artifact. No generation of any kind occurs.

No terrain generation, no lot subdivision, no concrete geometry created, no lotpack writing,
no worldgen override file, layout not materialized, no runtime proof. Not writer-ready.

Generate the adjacency planning candidate extraction:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\run-deadmtl-worldbuilder-adjacency-planning-candidate-extraction.ps1
```

Output (under `.local/`):

- `.local\deadmtl-authoring\worldbuilder-adjacency-planning-candidate-extraction\map_00\map_00.adjacency_planning_candidate_extraction.json`
- `.local\deadmtl-authoring\worldbuilder-adjacency-planning-candidate-extraction\map_00\map_00.adjacency_planning_candidate_extraction.md`
- `.local\deadmtl-authoring\worldbuilder-adjacency-planning-candidate-extraction\map_00\map_00.adjacency_planning_candidate_extraction.csv`
- `.local\deadmtl-authoring\worldbuilder-adjacency-planning-candidate-extraction\map_00\map_00.adjacency_planning_candidate_extraction.summary.txt`

See `docs/authoring/DEADMTL_WORLDBUILDER_ADJACENCY_PLANNING_CANDIDATE_EXTRACTION_CONTRACT.md` for
candidate type mapping, priority policy, totals, and claim boundary.

---

## MAP-25O WorldBuilder Component Access Profile

MAP-25O reads the MAP-25N planning candidates and MAP-25L component intents and produces a
per-component access profile for all 45 classified components. For each component it records:
candidate counts by type, primary frontage and rear-service partners (by largest contact), and
an access readiness class (DUAL_ACCESS_CANDIDATE, FRONTAGE_ONLY_CANDIDATE, MAIN_ROAD_CORRIDOR_NODE,
etc.).

This is access profile extraction only. It is NOT lot polygons, road geometries, frontage
geometry, rear access geometry, building slots, sidewalk geometry, fence geometry, or any
materialized artifact. No generation of any kind occurs.

No terrain generation, no lot subdivision, no concrete geometry created, no lotpack writing,
no worldgen override file, layout not materialized, no runtime proof. Not writer-ready.

Generate the component access profile:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\run-deadmtl-worldbuilder-component-access-profile.ps1
```

Output (under `.local/`):

- `.local\deadmtl-authoring\worldbuilder-component-access-profile\map_00\map_00.component_access_profile.json`
- `.local\deadmtl-authoring\worldbuilder-component-access-profile\map_00\map_00.component_access_profile.md`
- `.local\deadmtl-authoring\worldbuilder-component-access-profile\map_00\map_00.component_access_profile.csv`
- `.local\deadmtl-authoring\worldbuilder-component-access-profile\map_00\map_00.component_access_profile.summary.txt`

See `docs/authoring/DEADMTL_WORLDBUILDER_COMPONENT_ACCESS_PROFILE_CONTRACT.md` for
access readiness classification rules, primary candidate selection, totals, and claim boundary.

---

## MAP-26A WorldBuilder Minimal Concrete Geometry MVP

MAP-26A is the first pivot from pure metadata to concrete integer rectangle geometry.
It reads the source PNG, the connected component extraction, and the MAP-25O access profile
and produces actual pixel/tile coordinate records for component_order=1 (RESIDENTIAL_LOT_BLOCK,
DUAL_ACCESS_CANDIDATE):

- **Component bounding box**: min_x, min_y, max_x, max_y, width_px, height_px
- **Lot rectangles**: integer-clipped bbox slices along the frontage axis
- **Building slot rectangles**: per-lot, with side insets (2px) and frontage/rear setbacks (3px)

Lot slicing: target_lot_width=12px, min=8px, max_count=8. Side is detected by 4-directional
pixel contact count against the primary frontage component (comp 23, MAIN_ROAD) and the primary
rear component (comp 30, BACK_ALLEY).

`created_geometry_count > 0`. Not writer-ready. Not runtime-proven. No lotpack. No WorldGenOverride.lua.

Generate the minimal concrete geometry MVP:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\run-deadmtl-worldbuilder-minimal-concrete-geometry-mvp.ps1
```

Output (under `.local/`):

- `.local\deadmtl-authoring\worldbuilder-minimal-concrete-geometry-mvp\map_00\map_00.minimal_concrete_geometry_mvp.json`
- `.local\deadmtl-authoring\worldbuilder-minimal-concrete-geometry-mvp\map_00\map_00.minimal_concrete_geometry_mvp.md`
- `.local\deadmtl-authoring\worldbuilder-minimal-concrete-geometry-mvp\map_00\map_00.minimal_concrete_geometry_mvp.csv`
- `.local\deadmtl-authoring\worldbuilder-minimal-concrete-geometry-mvp\map_00\map_00.minimal_concrete_geometry_mvp.summary.txt`

See `docs/authoring/DEADMTL_WORLDBUILDER_MINIMAL_CONCRETE_GEOMETRY_MVP.md` for
the geometry algorithm, inset parameters, and claim boundary.

---

## MAP-26B WorldBuilder Minimal Concrete Geometry QA Overlay

MAP-26B is a QA/debug visualization step. It reads the source PNG and the MAP-26A
minimal concrete geometry MVP JSON and renders a 4x-scale overlay PNG (plus
JSON/MD/CSV/summary records) proving the MAP-26A component bbox, lot rectangles,
and accepted building slot rectangles sit where expected on `map_00.png`.

It is QA-only: no new geometry is derived, no PZ-consumable format is written,
and the source PNG is never mutated. `writer_ready`, `runtime_valid`, and
`materialized` all remain false.

Generate the QA overlay:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\run-deadmtl-worldbuilder-minimal-concrete-geometry-qa-overlay.ps1
```

Output (under `.local/`):

- `.local\deadmtl-authoring\worldbuilder-minimal-concrete-geometry-qa-overlay\map_00\map_00.minimal_concrete_geometry_qa_overlay.png`
- `.local\deadmtl-authoring\worldbuilder-minimal-concrete-geometry-qa-overlay\map_00\map_00.minimal_concrete_geometry_qa_overlay.json`
- `.local\deadmtl-authoring\worldbuilder-minimal-concrete-geometry-qa-overlay\map_00\map_00.minimal_concrete_geometry_qa_overlay.md`
- `.local\deadmtl-authoring\worldbuilder-minimal-concrete-geometry-qa-overlay\map_00\map_00.minimal_concrete_geometry_qa_overlay.csv`
- `.local\deadmtl-authoring\worldbuilder-minimal-concrete-geometry-qa-overlay\map_00\map_00.minimal_concrete_geometry_qa_overlay.summary.txt`

See `docs/authoring/DEADMTL_WORLDBUILDER_MINIMAL_CONCRETE_GEOMETRY_QA_OVERLAY_CONTRACT.md`
for overlay semantics and claim boundary.

---

## MAP-26C WorldBuilder Minimal Concrete Geometry QA Review Packet

MAP-26C reads the MAP-26A geometry MVP JSON and the MAP-26B overlay outputs and produces a
structured review packet that cross-checks the whole minimal concrete geometry chain.
It verifies component ID/bbox consistency, lot/slot counts (7/7), feature counts (15),
CSV feature breakdown (1/7/7), inclusive right/bottom semantics, source-bound containment,
and that all three boundary flags remain false (`writer_ready`, `runtime_valid`, `materialized`).

It is QA-only: it creates no geometry, writes no PZ-consumable files, and does not compile
or install anything.

Generate the review packet:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\run-deadmtl-worldbuilder-minimal-concrete-geometry-qa-review-packet.ps1
```

Output (under `.local/`):

- `.local\deadmtl-authoring\worldbuilder-minimal-concrete-geometry-qa-review-packet\map_00\map_00.minimal_concrete_geometry_qa_review_packet.json`
- `.local\deadmtl-authoring\worldbuilder-minimal-concrete-geometry-qa-review-packet\map_00\map_00.minimal_concrete_geometry_qa_review_packet.md`
- `.local\deadmtl-authoring\worldbuilder-minimal-concrete-geometry-qa-review-packet\map_00\map_00.minimal_concrete_geometry_qa_review_packet.csv`
- `.local\deadmtl-authoring\worldbuilder-minimal-concrete-geometry-qa-review-packet\map_00\map_00.minimal_concrete_geometry_qa_review_packet.summary.txt`

See `docs/authoring/DEADMTL_WORLDBUILDER_MINIMAL_CONCRETE_GEOMETRY_QA_REVIEW_PACKET.md`
for check semantics and claim boundary.

---

## MAP-26D WorldBuilder Minimal Concrete Geometry Writer Input Manifest

MAP-26D reads all MAP-26A/B/C output artifacts and packages them into a locked, hash-verified
input bundle. It performs 17 checks: all 8 artifacts exist and are SHA-256 hashed, MAP-26A/B/C
verdicts are complete, MAP-26C passed all 18 review checks, and all geometry values (component ID,
bbox, lot/slot counts, feature count, source dimensions) are stable. All three boundary flags
remain false. The writer experiment gate is locked: `approved_for_writer_experiment: false`,
`writer_experiment_gate_status: LOCKED_PENDING_OPERATOR_APPROVAL`.

MAP-26D creates no geometry, writes no PZ-consumable files, and does not compile or install
anything.

Generate the writer input manifest:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\run-deadmtl-worldbuilder-minimal-concrete-geometry-writer-input-manifest.ps1
```

Output (under `.local/`):

- `.local\deadmtl-authoring\worldbuilder-minimal-concrete-geometry-writer-input-manifest\map_00\map_00.minimal_concrete_geometry_writer_input_manifest.json`
- `.local\deadmtl-authoring\worldbuilder-minimal-concrete-geometry-writer-input-manifest\map_00\map_00.minimal_concrete_geometry_writer_input_manifest.md`
- `.local\deadmtl-authoring\worldbuilder-minimal-concrete-geometry-writer-input-manifest\map_00\map_00.minimal_concrete_geometry_writer_input_manifest.csv`
- `.local\deadmtl-authoring\worldbuilder-minimal-concrete-geometry-writer-input-manifest\map_00\map_00.minimal_concrete_geometry_writer_input_manifest.summary.txt`

See `docs/authoring/DEADMTL_WORLDBUILDER_MINIMAL_CONCRETE_GEOMETRY_WRITER_INPUT_MANIFEST.md`
for check semantics, SHA-256 hashing, writer gate, and claim boundary.

---

## MAP-26E WorldBuilder Minimal Concrete Geometry Writer Experiment Scope Record

MAP-26E reads the MAP-26D manifest and produces a deterministic writer experiment scope /
approval record. It documents allowed future actions, forbidden actions, required preconditions,
rollback requirements, and a risk register. It does NOT authorize any writer experiment.

```powershell
powershell -ExecutionPolicy Bypass -File scripts\run-deadmtl-worldbuilder-minimal-concrete-geometry-writer-experiment-scope-record.ps1
```

Output paths:
- `.local\deadmtl-authoring\worldbuilder-minimal-concrete-geometry-writer-experiment-scope-record\map_00\map_00.minimal_concrete_geometry_writer_experiment_scope_record.json`
- `.local\deadmtl-authoring\worldbuilder-minimal-concrete-geometry-writer-experiment-scope-record\map_00\map_00.minimal_concrete_geometry_writer_experiment_scope_record.md`
- `.local\deadmtl-authoring\worldbuilder-minimal-concrete-geometry-writer-experiment-scope-record\map_00\map_00.minimal_concrete_geometry_writer_experiment_scope_record.csv`
- `.local\deadmtl-authoring\worldbuilder-minimal-concrete-geometry-writer-experiment-scope-record\map_00\map_00.minimal_concrete_geometry_writer_experiment_scope_record.summary.txt`

See `docs/authoring/DEADMTL_WORLDBUILDER_MINIMAL_CONCRETE_GEOMETRY_WRITER_EXPERIMENT_SCOPE_RECORD.md`
for allowed/forbidden actions, preconditions, rollback, risk register, and claim boundary.

---

## MAP-26F WorldBuilder Minimal Concrete Geometry Writer Dry-Run Design

MAP-26F reads the MAP-26E scope record, MAP-26D manifest, and MAP-26A geometry MVP and produces
a deterministic dry-run writer design record. It documents planned output records, sandbox
constraints, forbidden output guards, and rollback checks. It does NOT emit any writer outputs.

```powershell
powershell -ExecutionPolicy Bypass -File scripts\run-deadmtl-worldbuilder-minimal-concrete-geometry-writer-dry-run-design.ps1
```

Output paths:
- `.local\deadmtl-authoring\worldbuilder-minimal-concrete-geometry-writer-dry-run-design\map_00\map_00.minimal_concrete_geometry_writer_dry_run_design.json`
- `.local\deadmtl-authoring\worldbuilder-minimal-concrete-geometry-writer-dry-run-design\map_00\map_00.minimal_concrete_geometry_writer_dry_run_design.md`
- `.local\deadmtl-authoring\worldbuilder-minimal-concrete-geometry-writer-dry-run-design\map_00\map_00.minimal_concrete_geometry_writer_dry_run_design.csv`
- `.local\deadmtl-authoring\worldbuilder-minimal-concrete-geometry-writer-dry-run-design\map_00\map_00.minimal_concrete_geometry_writer_dry_run_design.summary.txt`

See `docs/authoring/DEADMTL_WORLDBUILDER_MINIMAL_CONCRETE_GEOMETRY_WRITER_DRY_RUN_DESIGN.md`
for planned records, sandbox constraints, forbidden guards, rollback checks, and claim boundary.

---

## MAP-26G WorldBuilder Minimal Concrete Geometry Writer Dry-Run Emitter

MAP-26G reads the MAP-26F dry-run design and the MAP-26A geometry MVP and emits deterministic
dry-run records under `.local` only. It answers what a future writer would emit from the real
MAP-26A geometry, without writing any PZ runtime files, `.lotpack`, `.lotheader`,
`WorldGenOverride.lua`, or calling `compile-worldgen`.

```powershell
powershell -ExecutionPolicy Bypass -File scripts\run-deadmtl-worldbuilder-minimal-concrete-geometry-writer-dry-run-emitter.ps1
```

Main output paths:
- `.local\deadmtl-authoring\worldbuilder-minimal-concrete-geometry-writer-dry-run-emitter\map_00\map_00.minimal_concrete_geometry_writer_dry_run_emitter.json`
- `.local\deadmtl-authoring\worldbuilder-minimal-concrete-geometry-writer-dry-run-emitter\map_00\map_00.minimal_concrete_geometry_writer_dry_run_emitter.md`
- `.local\deadmtl-authoring\worldbuilder-minimal-concrete-geometry-writer-dry-run-emitter\map_00\map_00.minimal_concrete_geometry_writer_dry_run_emitter.csv`
- `.local\deadmtl-authoring\worldbuilder-minimal-concrete-geometry-writer-dry-run-emitter\map_00\map_00.minimal_concrete_geometry_writer_dry_run_emitter.summary.txt`

Dry-run emitted record files (8 total, all under the same directory):
- `map_00.component_writer_record.json`
- `map_00.lot_writer_records.json`
- `map_00.building_slot_writer_records.json`
- `map_00.frontage_access_record.json`
- `map_00.rear_service_access_record.json`
- `map_00.forbidden_output_scan.json`
- `map_00.rollback_record.json`
- `map_00.claim_boundary_record.json`

See `docs/authoring/DEADMTL_WORLDBUILDER_MINIMAL_CONCRETE_GEOMETRY_WRITER_DRY_RUN_EMITTER.md`
for checks, claim boundary, and what this does and does not prove.

---

## MAP-26H WorldBuilder Minimal Concrete Geometry Writer Dry-Run Emission Audit Receipt

MAP-26H independently verifies MAP-26G's 8 emitted dry-run records after emission. It answers:
Did MAP-26G emit exactly the expected records under `.local`, with stable hashes, correct claim
boundaries, and no forbidden runtime artifacts?

This is NOT a real PZ writer and NOT runtime proof. Requires MAP-26G to be run first.

```powershell
powershell -ExecutionPolicy Bypass -File scripts\run-deadmtl-worldbuilder-minimal-concrete-geometry-writer-dry-run-emission-audit-receipt.ps1
```

Main output paths (all under `.local`):
- `.local\deadmtl-authoring\worldbuilder-minimal-concrete-geometry-writer-dry-run-emission-audit-receipt\map_00\map_00.minimal_concrete_geometry_writer_dry_run_emission_audit_receipt.json`
- `.local\deadmtl-authoring\worldbuilder-minimal-concrete-geometry-writer-dry-run-emission-audit-receipt\map_00\map_00.minimal_concrete_geometry_writer_dry_run_emission_audit_receipt.md`
- `.local\deadmtl-authoring\worldbuilder-minimal-concrete-geometry-writer-dry-run-emission-audit-receipt\map_00\map_00.minimal_concrete_geometry_writer_dry_run_emission_audit_receipt.csv`
- `.local\deadmtl-authoring\worldbuilder-minimal-concrete-geometry-writer-dry-run-emission-audit-receipt\map_00\map_00.minimal_concrete_geometry_writer_dry_run_emission_audit_receipt.summary.txt`

Audits 12 files: 4 main MAP-26G outputs + 8 emitted records. Runs 29 checks.

See `docs/authoring/DEADMTL_WORLDBUILDER_MINIMAL_CONCRETE_GEOMETRY_WRITER_DRY_RUN_EMISSION_AUDIT_RECEIPT.md`
for checks, claim boundary, and what this does and does not prove.

---

## MAP-26I WorldBuilder Minimal Concrete Geometry Writer Adapter Contract

MAP-26I normalizes the 8 dry-run records emitted by MAP-26G (and verified by MAP-26H) into a
single bounded adapter contract document. All normalized records carry `writer_consumable: false`
and `runtime_consumable: false`. 8 forbidden output families are explicitly declared.

This is NOT a real PZ writer and NOT runtime proof. Requires MAP-26G and MAP-26H to be run first.

```powershell
powershell -ExecutionPolicy Bypass -File scripts\run-deadmtl-worldbuilder-minimal-concrete-geometry-writer-adapter-contract.ps1
```

Main output paths (all under `.local`):
- `.local\deadmtl-authoring\worldbuilder-minimal-concrete-geometry-writer-adapter-contract\map_00\map_00.minimal_concrete_geometry_writer_adapter_contract.json`
- `.local\deadmtl-authoring\worldbuilder-minimal-concrete-geometry-writer-adapter-contract\map_00\map_00.minimal_concrete_geometry_writer_adapter_contract.md`
- `.local\deadmtl-authoring\worldbuilder-minimal-concrete-geometry-writer-adapter-contract\map_00\map_00.minimal_concrete_geometry_writer_adapter_contract.csv`
- `.local\deadmtl-authoring\worldbuilder-minimal-concrete-geometry-writer-adapter-contract\map_00\map_00.minimal_concrete_geometry_writer_adapter_contract.summary.txt`

Normalizes: 1 component, 7 lots, 7 building slots, 2 access records. Runs 28 checks.
`adapter_contract_status: NORMALIZED_DRY_RUN_RECORDS_ONLY`.

See `docs/authoring/DEADMTL_WORLDBUILDER_MINIMAL_CONCRETE_GEOMETRY_WRITER_ADAPTER_CONTRACT.md`
for checks, forbidden output families, and claim boundaries.

---

## MAP-27A WorldBuilder Minimal Concrete Geometry Sandbox Writer V0

MAP-27A is the first writer seam. It reads the canonical MAP-26I/MAP-26J adapter contract and
emits concrete writer-operation artifacts under `.local`.

It writes sandbox writer operation artifacts only. It does not write PZ runtime files.
It does not prove runtime validity.

```powershell
powershell -ExecutionPolicy Bypass -File scripts\run-deadmtl-worldbuilder-minimal-concrete-geometry-sandbox-writer-v0.ps1
```

Main output paths (all under `.local`):
- `.local\deadmtl-authoring\worldbuilder-minimal-concrete-geometry-sandbox-writer-v0\map_00\map_00.minimal_concrete_geometry_sandbox_writer_v0.json`
- `.local\deadmtl-authoring\worldbuilder-minimal-concrete-geometry-sandbox-writer-v0\map_00\map_00.minimal_concrete_geometry_sandbox_writer_v0.summary.txt`

Emitted sandbox writer operation files (all under `.local`):
- `map_00.sandbox_writer_component_operations.json` (1 op)
- `map_00.sandbox_writer_lot_operations.json` (7 ops)
- `map_00.sandbox_writer_building_slot_operations.json` (7 ops)
- `map_00.sandbox_writer_access_operations.json` (2 ops)
- `map_00.sandbox_writer_forbidden_output_guard.json` (8 guards)

Operations: 17 total, all `runtime_effect: NONE`. Runs 33 checks.
`writer_stage: SANDBOX_WRITER_V0`, `sandbox_only: true`, `writer_ready: false`.

See `docs/authoring/DEADMTL_WORLDBUILDER_MINIMAL_CONCRETE_GEOMETRY_SANDBOX_WRITER_V0.md`
for checks, operation kinds, and claim boundary.

---

## MAP-27B WorldBuilder Minimal Concrete Geometry Sandbox Writer Tile Buffer V0

MAP-27B consumes MAP-27A operation files and applies them into an internal 256x256 tile buffer.
No PZ runtime files are written. No runtime validity is claimed.

```powershell
powershell -ExecutionPolicy Bypass -File scripts\run-deadmtl-worldbuilder-minimal-concrete-geometry-sandbox-writer-tile-buffer-v0.ps1
```

Main output paths (all under `.local`):
- `.local\deadmtl-authoring\worldbuilder-minimal-concrete-geometry-sandbox-writer-tile-buffer-v0\map_00\map_00.minimal_concrete_geometry_sandbox_writer_tile_buffer_v0.json`
- `.local\deadmtl-authoring\worldbuilder-minimal-concrete-geometry-sandbox-writer-tile-buffer-v0\map_00\map_00.minimal_concrete_geometry_sandbox_writer_tile_buffer_v0.summary.txt`

Extra output files (same dir):
- `map_00.sandbox_writer_tile_buffer_cells.csv` (touched cells, operation_ids in `<file>#<kind>#<order>` format)
- `map_00.sandbox_writer_tile_buffer_ownership.json` (4 ownership kinds)
- `map_00.sandbox_writer_tile_buffer_replay_log.json` (17 replay entries)
- `map_00.sandbox_writer_tile_buffer_collision_report.json` (collision records)
- `map_00.sandbox_writer_tile_buffer_forbidden_output_guard.json` (inherited guard)

Buffer: 256x256, `PNG_PIXEL_TILE_SPACE`, origin top-left. 38 checks.
`writer_stage: SANDBOX_WRITER_TILE_BUFFER_V0`, `sandbox_only: true`, `writer_ready: false`.
ACCESS_LINK edge cells derived from component bbox using `access_kind`/`side` fields.

See `docs/authoring/DEADMTL_WORLDBUILDER_MINIMAL_CONCRETE_GEOMETRY_SANDBOX_WRITER_TILE_BUFFER_V0.md`
for buffer model, ownership priorities, edge derivation, and claim boundary.

---

## MAP-27C WorldBuilder Minimal Concrete Geometry Sandbox Writer Tile Materializer V0

MAP-27C consumes MAP-27B1 tile buffer outputs and materializes every touched cell into
deterministic sandbox tile/material records. No PZ runtime files are written.

```powershell
powershell -ExecutionPolicy Bypass -File scripts\run-deadmtl-worldbuilder-minimal-concrete-geometry-sandbox-writer-tile-materializer-v0.ps1
```

Main output paths (all under `.local`):
- `.local\deadmtl-authoring\worldbuilder-minimal-concrete-geometry-sandbox-writer-tile-materializer-v0\map_00\map_00.minimal_concrete_geometry_sandbox_writer_tile_materializer_v0.json`
- `.local\deadmtl-authoring\worldbuilder-minimal-concrete-geometry-sandbox-writer-tile-materializer-v0\map_00\map_00.minimal_concrete_geometry_sandbox_writer_tile_materializer_v0.summary.txt`

Extra output files (same dir):
- `map_00.sandbox_writer_tile_materialized_cells.csv` (materialized cells with material_kind, layer_kind)
- `map_00.sandbox_writer_tile_material_palette.json` (5 material kinds with cell counts)
- `map_00.sandbox_writer_tile_layer_stack.json` (5 layers: COMPONENT/LOT/ACCESS/FLOOR/WALL)
- `map_00.sandbox_writer_tile_materialization_replay_log.json` (cells per owner_kind × material_kind)
- `map_00.sandbox_writer_tile_materialization_ownership_summary.json` (cells per owner_kind)
- `map_00.sandbox_writer_tile_materializer_forbidden_output_guard.json` (inherited guard)

30 checks. Key invariant: `materialized_cell_count == input_touched_cell_count`.
`writer_stage: SANDBOX_WRITER_TILE_MATERIALIZER_V0`, `sandbox_only: true`, `sandbox_materialized: true`, `writer_ready: false`.
BUILDING_FOOTPRINT cells classified as WALL (exterior) or FLOOR (interior) per per-slot bbox.

See `docs/authoring/DEADMTL_WORLDBUILDER_MINIMAL_CONCRETE_GEOMETRY_SANDBOX_WRITER_TILE_MATERIALIZER_V0.md`
for materialization rules, material palette, layer stack, and claim boundary.

---

## MAP-27D WorldBuilder Minimal Concrete Geometry Sandbox Writer Tile Materializer QA Overlay V0

MAP-27D reads the MAP-27C materialized cells CSV and renders a deterministic 1024x1024 PNG
visual QA overlay. Each 256x256 tile-space cell is drawn as a 4x4 pixel block.

Sandbox-only diagnostic artifact. No PZ runtime files are written. No runtime validity claimed.

```powershell
powershell -ExecutionPolicy Bypass -File scripts\run-deadmtl-worldbuilder-minimal-concrete-geometry-sandbox-writer-tile-materializer-qa-overlay-v0.ps1
```

Output (under `.local/`):
- `worldbuilder-minimal-concrete-geometry-sandbox-writer-tile-materializer-qa-overlay-v0.local\map_00.sandbox_writer_tile_materializer_qa_overlay.png` (1024x1024 PNG)
- `worldbuilder-minimal-concrete-geometry-sandbox-writer-tile-materializer-qa-overlay-v0.local\map_00.minimal_concrete_geometry_sandbox_writer_tile_materializer_qa_overlay_v0.json`
- `worldbuilder-minimal-concrete-geometry-sandbox-writer-tile-materializer-qa-overlay-v0.local\map_00.minimal_concrete_geometry_sandbox_writer_tile_materializer_qa_overlay_v0.md`
- `worldbuilder-minimal-concrete-geometry-sandbox-writer-tile-materializer-qa-overlay-v0.local\map_00.minimal_concrete_geometry_sandbox_writer_tile_materializer_qa_overlay_v0.csv`
- `worldbuilder-minimal-concrete-geometry-sandbox-writer-tile-materializer-qa-overlay-v0.local\map_00.minimal_concrete_geometry_sandbox_writer_tile_materializer_qa_overlay_v0.summary.txt`

39 checks. `writer_stage: SANDBOX_WRITER_TILE_MATERIALIZER_QA_OVERLAY_V0`.
`sandbox_only: true`, `visual_qa_overlay_written: true`, `pz_runtime_materialized: false`,
`writer_ready: false`, `materialized: false`.

Material color palette: WALL=#1F1F1F, FLOOR=#A8A8A8, ACCESS=#2F6FDB, LOT=#4F8A3B,
RESIDUAL=#7A4E2A, background=#101010.

See `docs/authoring/DEADMTL_WORLDBUILDER_MINIMAL_CONCRETE_GEOMETRY_SANDBOX_WRITER_TILE_MATERIALIZER_QA_OVERLAY_V0.md`
for color palette, rendering rules, input requirements, and claim boundary.

---

## System 2 static road filtered tile candidate shortlist

MAP-22P ranks and shortlists candidates from the MAP-22O filtered local tile survey.
Applies deterministic scoring (primary terms +20, surface bonus +10, source file bonuses,
negative terms). Default: top 25 per surface family. road_node_metadata_candidate stays
UNRESOLVED_METADATA_ONLY. Confidence: LOCAL_FILTERED_TEXT_MATCH_RANKED_ONLY.

Filtered shortlist is NOT runtime proof and NOT writer-ready.
Does NOT write lotpack files. Does NOT write WorldGenOverride.lua.

Generate filtered tile candidate shortlist:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\run-system2-static-road-filtered-tile-candidate-shortlist.ps1
```

Output (under `.local/`):
- `.local\deadmtl-authoring\system2-static-road-filtered-tile-candidate-shortlist\system2_static_road_filtered_tile_candidate_shortlist.json`
- `.local\deadmtl-authoring\system2-static-road-filtered-tile-candidate-shortlist\system2_static_road_filtered_tile_candidate_shortlist.md`
- `.local\deadmtl-authoring\system2-static-road-filtered-tile-candidate-shortlist\system2_static_road_filtered_tile_candidate_shortlist.csv`
- `.local\deadmtl-authoring\system2-static-road-filtered-tile-candidate-shortlist\system2_static_road_filtered_tile_candidate_shortlist.summary.txt`

See `docs/authoring/DEADMTL_SYSTEM2_STATIC_ROAD_FILTERED_TILE_CANDIDATE_SHORTLIST.md` for scoring,
CSV format, and claim boundary.

---

## System 2 static road local tile survey (filtered)

MAP-22O produces a filtered local tile survey that excludes known non-tile text sources.
MAP-22J scanned too broadly (e.g. `tile_name: "asphalt"` from `media\profanity\Dictionary.txt`).
MAP-22O excludes path fragments like `media/profanity`, `media/lua`, `media/scripts/items`, etc.
Confidence: LOCAL_FILTERED_TEXT_MATCH_ONLY. No runtime proof claimed.

Does NOT write lotpack files. Does NOT write WorldGenOverride.lua. No writer-ready claim.

Generate filtered local tile survey:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\run-system2-static-road-local-tile-survey-filtered.ps1
```

Output (under `.local/`):
- `.local\deadmtl-authoring\system2-static-road-local-tile-survey-filtered\system2_static_road_local_tile_survey_filtered.json`
- `.local\deadmtl-authoring\system2-static-road-local-tile-survey-filtered\system2_static_road_local_tile_survey_filtered.md`
- `.local\deadmtl-authoring\system2-static-road-local-tile-survey-filtered\system2_static_road_local_tile_survey_filtered.csv`
- `.local\deadmtl-authoring\system2-static-road-local-tile-survey-filtered\system2_static_road_local_tile_survey_filtered.summary.txt`

See `docs/authoring/DEADMTL_SYSTEM2_STATIC_ROAD_LOCAL_TILE_SURVEY_FILTERED.md` for excluded
fragments, CSV format, and claim boundary.

---

## Claim boundary

This skeleton does not constitute a playable Project Zomboid map.
No PZ load test has been performed.
No public mod packaging is claimed.
Lua output is a planning artifact only until a real local load test is documented.
