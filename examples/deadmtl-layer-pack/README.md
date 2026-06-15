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
