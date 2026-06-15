# MAP-24A: DeadMTL Vanilla Building Source Discovery

## Purpose

Produce a formal index of local building/map-source candidates across all known PZ
installation and workspace roots. This is discovery only — no buildings are extracted,
no editable catalogue is produced, no runtime proof is claimed.

## Claim boundary

```
status:                          VANILLA_BUILDING_SOURCE_DISCOVERY_ONLY
runtime proof is NOT claimed:    runtime_proven = false
writer readiness is NOT claimed: writer_ready_claim = false
building_extraction_claim:       false
editable_vanilla_catalogue_claim: false
writes_lotpack:                  false
writes_worldgen_lua:             false
```

No claim of extracted buildings or reusable building templates is made unless
`.tbx` or `.building` files are found and that is what the discovery says.

## What qualifies as a map folder group

A directory qualifies if it contains at least one of:
- `.lotheader` file
- `.lotpack` file
- `.tmx` file
- `.tbx` file
- `.building` file
- `map.info` file

## Folder kinds

| Kind | Source |
|------|--------|
| `VANILLA_COMPILED_MAP` | PZ install root |
| `MODDING_TOOL_EXAMPLE` | PZ Modding Tools root |
| `USER_MOD_COMPILED_MAP` | User Zomboid folder |
| `WORKSPACE_COMPILED_MAP` | E:\Omni\Zomboid workspace |
| `UNKNOWN_MAP_SOURCE` | Any other root |

## Claim per group

- `editable_template_source = true` only when `.tbx` or `.building` files are found.
- `compiled_map_source = true` when `.lotheader` or `.lotpack` files are found.
- `vanilla_source = true` when kind is `VANILLA_COMPILED_MAP`.

## Recommendation strings

| Value | Meaning |
|-------|---------|
| `EDITABLE_TEMPLATE_SOURCES_FOUND` | `.tbx` or `.building` sources discovered |
| `NO_EDITABLE_VANILLA_CATALOGUE_FOUND_USE_COMPILED_MAP_EXTRACTION_AND_MANUAL_DEADMTL_CATALOGUE` | No editable sources; only compiled maps found |
| `NO_BUILDING_SOURCES_FOUND` | No qualifying folders found |

## Command

```
deadmtl-discover-vanilla-building-sources \
  --output-json  .local\deadmtl-authoring\vanilla-building-source-discovery\vanilla_building_source_discovery.json \
  --output-md    .local\deadmtl-authoring\vanilla-building-source-discovery\vanilla_building_source_discovery.md \
  --output-csv   .local\deadmtl-authoring\vanilla-building-source-discovery\vanilla_building_source_discovery.csv \
  --summary      .local\deadmtl-authoring\vanilla-building-source-discovery\vanilla_building_source_discovery.summary.txt
```

## Helper script

```
examples\deadmtl-layer-pack\scripts\run-deadmtl-vanilla-building-source-discovery.ps1
```

## Output files

| File | Format |
|------|--------|
| `vanilla_building_source_discovery.json` | Full discovery result, format `pzmapforge.deadmtl.vanilla-building-source-discovery.v1` |
| `vanilla_building_source_discovery.md` | Human-readable summary |
| `vanilla_building_source_discovery.csv` | One row per map folder group |
| `vanilla_building_source_discovery.summary.txt` | Terminal-friendly summary |

## JSON format

```json
{
  "format": "pzmapforge.deadmtl.vanilla-building-source-discovery.v1",
  "status": "VANILLA_BUILDING_SOURCE_DISCOVERY_ONLY",
  "runtime_status": "NOT_RUNTIME_PROVEN",
  "writer_status": "NOT_IMPLEMENTED",
  "scan_roots": [...],
  "file_type_counts": { "lotheader": 0, "lotpack": 0, "tmx": 0, "tbx": 0, "building": 0 },
  "map_folder_groups": [
    {
      "root": "...",
      "map_folder": "...",
      "relative_path": "...",
      "kind": "VANILLA_COMPILED_MAP",
      "counts": { ... },
      "sample_files": ["0_0.lotheader", "0_0.lotpack"],
      "claim": { "editable_template_source": false, "compiled_map_source": true, "vanilla_source": true }
    }
  ],
  "editable_source_candidates": [],
  "compiled_map_candidates": [...],
  "recommendation": "NO_EDITABLE_VANILLA_CATALOGUE_FOUND_USE_COMPILED_MAP_EXTRACTION_AND_MANUAL_DEADMTL_CATALOGUE",
  "claim_boundary": {
    "writes_lotpack": false,
    "writes_worldgen_lua": false,
    "runtime_proven": false,
    "public_playable_claim": false,
    "writer_ready_claim": false,
    "building_extraction_claim": false,
    "editable_vanilla_catalogue_claim": false
  }
}
```

**VERDICT: MAP24A_VANILLA_BUILDING_SOURCE_DISCOVERY_COMPLETE**
