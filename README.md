# PZMapForge

An independent deterministic map-planning layer for Project Zomboid mod work.

Converts a blockout image into local-only planning artifacts: a semantic cell
grid, a JSON artifact with drift records and SHA-256 hashes, a preview PNG,
a colour-strip tileset, and a TileZed-openable planning TMX.

---

## Claim boundary

**Current verified claim:**

> PNG or BMP blockout -> semantic 300x300 grid -> planning artifacts
> (JSON, report, preview, tileset, TileZed-openable TMX).

Not claimed:
- Playable Project Zomboid map export.
- lotpack / lotheader / bin generation.
- Build 42 compatibility (unverified).
- Official Project Zomboid tool status.
- WorldEd replacement.

See [docs/CLAIM_BOUNDARY.md](docs/CLAIM_BOUNDARY.md) for the full boundary.

---

## .NET engine foundation

A typed .NET engine (C#, .NET 10) is being built alongside the PowerShell
reference implementation. The PowerShell pipeline remains the authority.

Build and test:

```
dotnet build PZMapForge.slnx
dotnet test PZMapForge.slnx
```

Run the multi-layer image pipeline (layer-pipeline) — merges layer images via a manifest, writes 8 artifacts:

```
dotnet run --project src/PZMapForge.Cli -- layer-pipeline --layers <manifest.json> --palette source/image-palette.json --output .local/mapforge
```

With resize:

```
dotnet run --project src/PZMapForge.Cli -- layer-pipeline --layers <manifest.json> --palette source/image-palette.json --output .local/mapforge --resize
```

Run the full image-to-planning pipeline in one command (full-pipeline) — writes 7 artifacts:
parsed-cell.json, regions.json, regions-report.md, primitives.json, primitives-report.md,
plan-recommendations.json, plan-report.md:

```
dotnet run --project src/PZMapForge.Cli -- full-pipeline --path .local/mapforge/sample-input.png --palette source/image-palette.json --output .local/mapforge
```

With resize and custom thresholds:

```
dotnet run --project src/PZMapForge.Cli -- full-pipeline --path mymap.png --palette source/image-palette.json --output .local/mapforge --resize --tiny-threshold 0 --large-threshold 100000
```

Parse a PNG/BMP blockout image (image-check):

```
dotnet run --project src/PZMapForge.Cli -- image-check --path .local/mapforge/sample-input.png --palette source/image-palette.json
```

With resize (non-300x300 input):

```
dotnet run --project src/PZMapForge.Cli -- image-check --path mymap-150x150.png --palette source/image-palette.json --resize
```

Export image to parsed-cell.json (image-export):

```
dotnet run --project src/PZMapForge.Cli -- image-export --path .local/mapforge/sample-input.png --palette source/image-palette.json --output .local/mapforge
```

Validate the palette via the CLI:

```
dotnet run --project src/PZMapForge.Cli -- palette-check --palette source/image-palette.json
```

Validate a parsed-cell artifact via the CLI:

```
dotnet run --project src/PZMapForge.Cli -- parsed-cell-check --path .local/mapforge/parsed-cell.json
```

Extract regions from a parsed-cell artifact:

```
dotnet run --project src/PZMapForge.Cli -- region-check --path .local/mapforge/parsed-cell.json
```

Classify regions into planning primitives:

```
dotnet run --project src/PZMapForge.Cli -- primitive-check --path .local/mapforge/parsed-cell.json
```

Evaluate planning rules and get deterministic recommendations:

```
dotnet run --project src/PZMapForge.Cli -- plan-check --path .local/mapforge/parsed-cell.json
```

Export planning recommendations to local JSON and markdown artifacts:

```
dotnet run --project src/PZMapForge.Cli -- plan-export --path .local/mapforge/parsed-cell.json
```

Use custom planning thresholds (`--tiny-threshold`, `--large-threshold`):

```
dotnet run --project src/PZMapForge.Cli -- plan-check --path .local/mapforge/parsed-cell.json --tiny-threshold 0 --large-threshold 100000
```

The .NET engine does not replace the PowerShell scripts. It is a foundation
for future typed parsing and generation capabilities.

---

## Quickstart

Run the local validation pipeline (creates a sample image, runs all sub-scripts, 381 PS assertions):

```powershell
powershell -ExecutionPolicy Bypass -File "scripts\validate.ps1"
```

Or run ImageMapForge directly against your own blockout:

```powershell
powershell -ExecutionPolicy Bypass -File "source\image-mapforge.ps1" -ImagePath ".local\mapforge\mymap.png"
```

Use `-Mode Debug` to inspect colour frequencies before generating artifacts:

```powershell
powershell -ExecutionPolicy Bypass -File "source\image-mapforge.ps1" -ImagePath ".local\mapforge\mymap.png" -Mode Debug
```

Outputs appear under `.local\mapforge\` (created on first run, gitignored).

---

## Outputs

| File | Purpose |
|---|---|
| `.local/mapforge/parsed-cell.json` | Semantic grid, counts, drift records, SHA-256 hashes |
| `.local/mapforge/parsed-cell-report.md` | Human-readable report with claim boundary and drift table |
| `.local/mapforge/parsed-cell-preview.png` | Visual preview of the parsed semantic grid |
| `.local/mapforge/parsed-cell-tiles.png` | Generated colour-strip tileset used by the TMX |
| `.local/mapforge/parsed-cell-basic.tmx` | TileZed-openable planning TMX |

---

## Testing

```powershell
powershell -ExecutionPolicy Bypass -File "tests\test-image-mapforge.ps1"
```

Expected: 28 assertions, all pass, exit 0.

The test harness covers: bad image path, wrong size without -Resize, external
output refusal, media/maps refusal, Debug mode (no artifacts, correct output),
normal run (all 5 files), kind-count completeness, determinism, drift accuracy,
and .local/ gitignore proof.

---

## Structure

```
source/
  image-mapforge.ps1    image-to-semantic-grid tool
  image-palette.json    RGB palette (9 required semantic kinds)

tests/
  test-image-mapforge.ps1   28-assertion hardening harness

scripts/
  validate.ps1          smoke + full test harness
  new-test-image.ps1    generates deterministic sample input

schemas/
  pzmapforge.parsed-cell.v0.1.schema.json   JSON Schema for parsed-cell.json

docs/
  GENESIS.md            why PZMapForge exists
  CONSTITUTION.md       non-negotiable behavioral rules
  IMPLEMENTATION.md     ratified vs. provisional vs. not present
  TOOL_USAGE.md         detailed usage guide
  CLAIM_BOUNDARY.md     what is and is not claimed
  IMAGE_MAPFORGE.md     ImageMapForge reference
  ROADMAP.md            phased roadmap
  decisions/
    0001-independent-mapmaker-layer.md
    0002-planning-artifacts-before-playable-export.md

examples/
  README.md             how to create your own blockout image
```

---

## Documentation

- [Genesis](docs/GENESIS.md) — why this tool exists
- [Constitution](docs/CONSTITUTION.md) — non-negotiable rules
- [Implementation](docs/IMPLEMENTATION.md) — current state, ratified vs. provisional
- [Validation ledger](docs/VALIDATION_LEDGER.md) — both validation lanes, expected counts, commands
- [Phase 2 decision](docs/PHASE_2_DECISION.md) — Phase 2 direction: multi-layer image conventions
- [Phase 2B / Phase 3 decision](docs/PHASE_2B_OR_PHASE_3_DECISION.md) — Phase 2B (layer authoring) chosen over Phase 3 (PZ tile IDs)
- [Phase 3 local PZ config spec](docs/PHASE_3_LOCAL_PZ_CONFIG_SPEC.md) — precondition documentation for local tile ID mapping; not implemented
- [Phase 3A local install survey](docs/PHASE_3A_LOCAL_INSTALL_SURVEY.md) — operator survey guide: locate PZ install, inventory tiles, document findings
- [Layer authoring guide](docs/LAYER_AUTHORING_GUIDE.md) — manifest format, kind-by-layer table, precedence, conflict policy, workflow, error glossary

Validate a layer manifest and images without writing artifacts (layer-validate):

```
dotnet run --project src/PZMapForge.Cli -- layer-validate --layers <manifest.json> --palette source/image-palette.json
```

With resize:

```
dotnet run --project src/PZMapForge.Cli -- layer-validate --layers <manifest.json> --palette source/image-palette.json --resize
```
- [Tool usage](docs/TOOL_USAGE.md) — parameters, examples, palette format
- [Claim boundary](docs/CLAIM_BOUNDARY.md) — what is verified, what is not
- [Roadmap](docs/ROADMAP.md) — phase plan

---

## Doctrine

- Deterministic over manual.
- Evidence over claims.
- No copied Project Zomboid game source.
- No redistributed Project Zomboid assets.
- No playable claim without a local load test.
- Local-only generated artifacts stay under `.local/`.
