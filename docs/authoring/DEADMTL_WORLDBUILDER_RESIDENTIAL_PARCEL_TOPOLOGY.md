# MAP-28A WorldBuilder Residential Parcel Topology

Deterministic Core/CLI authoring artifact for map_00_component_0001 residential parcel topology.

## Claim boundary

This is a sandbox-only planning artifact.

- `sandbox_only = true`
- `parcel_topology_planning_only = true`
- `writer_ready = false`
- `runtime_valid = false`
- `materialized = false`
- `runtime_proof_claimed = false`
- `public_playable_packaging_claimed = false`

No .lotpack, .lotheader, .lua, .bin, or WorldGenOverride.lua files are written.
Source map_00.png is not mutated.

## Component

- Map ID       : map_00
- Component ID : map_00_component_0001
- Bbox         : X 124-212, Y 10-69 (89 x 60 tiles)

## Geometry (MAP-VIEW-4A corrected)

### Main block (X 124-201, 78 tiles wide)

| Zone            | Y range | Tiles | Description                           |
|-----------------|---------|-------|---------------------------------------|
| N sidewalk      | 10-11   |  2    | 2-tile street-facing access strip     |
| North lots      | 12-38   | 27    | 6 lots facing north (13-tile frontage)|
| Rear boundary   | 39-40   |  2    | REAR_BOUNDARY strip (NOT alley)       |
| South lots      | 41-67   | 27    | 6 lots facing south (13-tile frontage)|
| S sidewalk      | 68-69   |  2    | 2-tile street-facing access strip     |

Lot widths: 13,13,13,13,13,13 (6 lots x 13 = 78 tiles)

### Right column (X 202-212, 11 tiles wide)

| Zone            | X range | Tiles | Description                           |
|-----------------|---------|-------|---------------------------------------|
| East lots       | 202-210 |  9    | 4 lots facing east (15-tile height)   |
| E sidewalk      | 211-212 |  2    | 2-tile street-facing access strip     |

East lot heights: 15,15,15,15 (4 lots x 15 = 60 tiles)

## Topology rules

- No casual double-frontage lots (no lot spans both N and S street zones)
- No invented alleys (`invented_alleys_enabled = false`)
- No through-lots (`through_lots_enabled = false`)
- Every lot has exactly one primary frontage (NORTH, SOUTH, or EAST)
- East lots are spatially separated from north/south main block lots
- Mid-block separator is `REAR_BOUNDARY` kind (not alley)
- Minimum lot frontage: 12 tiles (all lots: N/S=13 tiles, E=15 tiles)
- Sidewalk widths: 2 tiles (N/S/E)

## Parcel color rules

- Blue A (#3A5EAE): North lots even index, East lots odd index
- Blue B (#4A6EBE): North lots odd index, South lots even index
- Blue C (#2A4E9E): East lots even index
- Sidewalk (#B8B8C0): All sidewalk strips
- Rear boundary (#4A3828): Rear boundary strip (dark brown)
- Bbox (#28C0C0): Component bbox outline (CYAN)

No yellow, green, red, or other hues for residential lots.

## Checks (25 total)

Checks 1-15: resolved during Build().
Checks 16-20: resolved in FinalizeAfterOutputs() after output files are written.
Checks 21-25: claim boundary checks, resolved during Build().

Expected: 25 PASS / 0 FAIL when all outputs are valid.

## Output files (10)

| File | Description |
|------|-------------|
| `map_00.residential_parcel_topology.json` | Full result JSON |
| `map_00.residential_parcel_topology_parcels.csv` | Parcel records (16 rows) |
| `map_00.residential_parcel_topology_frontage_edges.csv` | Frontage edge records (16 rows) |
| `map_00.residential_parcel_topology_sidewalk_strips.csv` | Sidewalk/fence strip records (4 rows) |
| `map_00.residential_parcel_topology_checks.csv` | Check results (25 rows) |
| `map_00.residential_parcel_topology.summary.txt` | Human-readable summary |
| `map_00_residential_parcels_topology_clean_native_256.png` | 256x256 clean parcel view |
| `map_00_residential_parcels_topology_debug_native_256.png` | 256x256 debug view (dividers + ticks) |
| `map_00_residential_parcels_topology_overlay_native_256.png` | 256x256 overlay on raw source |
| `map_00_residential_parcels_topology_viewer.html` | ASCII-only CSS zoom viewer |

Output folder: `.local\deadmtl-authoring\worldbuilder-residential-parcel-topology\map_00\`

## CLI command

```
deadmtl-build-worldbuilder-residential-parcel-topology
  --output-root <.local dir>
  --output-json <path>
  --output-parcels-csv <path>
  --output-frontage-edges-csv <path>
  --output-sidewalk-strips-csv <path>
  --output-checks-csv <path>
  --summary <path>
  --output-clean-png <path>
  --output-debug-png <path>
  --output-overlay-png <path>
  --output-html <path>
  [--raw-source-png <path>]
```

All output paths must contain `.local` (sandbox guard).
`--raw-source-png` is optional; overlay uses dark background if not provided or not found.

## Helper script

```
examples\deadmtl-layer-pack\scripts\run-deadmtl-worldbuilder-residential-parcel-topology.ps1
```

## Core classes

| File | Role |
|------|------|
| `DeadMtlWorldBuilderResidentialParcelTopology.cs` | Model types |
| `DeadMtlWorldBuilderResidentialParcelTopologyBuilder.cs` | Builder: Build(), FinalizeAfterOutputs(), Render*() |

## Tests

| Project | Filter | Tests |
|---------|--------|-------|
| Core | `ResidentialParcelTopology` | Builder checks, counts, PNG dims, ASCII outputs |
| CLI | `ResidentialParcelTopology` | Exit codes, file existence, JSON fields, CSV rows, PNGs |
