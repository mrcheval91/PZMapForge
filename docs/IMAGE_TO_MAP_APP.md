# Image-to-Map App Export

Status: Slice 3A-6 implemented; APP-2 visual viewer; APP-3 blockout UX applied

Claim boundary: planning_artifact_only_not_pz_load_tested

---

## Purpose

The `app-export` command runs the full PZMapForge pipeline on an input image
and writes a self-contained local HTML report alongside all pipeline artifacts.

The output is a local folder you can open in any browser as a file. No server
is needed. No network calls are made.

This is a planning artifact viewer. It is not a playable Project Zomboid export.

---

## Command

```
pzmapforge app-export --path <image> --palette <palette> [--output <dir>] [--run-name <name>] [--resize]
                      [--tiny-threshold <int>] [--large-threshold <int>]
```

| Argument | Required | Description |
|---|---|---|
| `--path <image>` | Yes | Input image (PNG or BMP) |
| `--palette <palette>` | Yes | Palette JSON file |
| `--output <dir>` | No | Output root (must contain `.local`; defaults to `.\.local\app`) |
| `--run-name <name>` | No | Named subdirectory under `--output`; unsafe chars sanitized to `-` |
| `--resize` | No | Resize image to 300x300 before parsing |
| `--tiny-threshold <int>` | No | Tiny building pixel threshold (default from PlanningRuleOptions) |
| `--large-threshold <int>` | No | Large ground pixel threshold (default from PlanningRuleOptions) |

Short forms: `-p` for `--path`, `-o` for `--output`.

---

## Example

```powershell
dotnet run --project .\src\PZMapForge.Cli --configuration Release --no-build -- `
  app-export `
  --path    .\source\sample.png `
  --palette .\source\image-palette.json `
  --output  .\.local\app
```

Expected console output on success:

```
App index:        .\.local\app\index.html
Artifacts:        .\.local\app\artifacts
Dimensions:       300x300
Regions:          <n>
Primitives:       <n>
Recommendations:  <n>
Warnings:         0
Status:           OK
```

---

## Outputs

All output is written under the `--output` directory. The directory must
contain a `.local` path segment.

| File | Description |
|---|---|
| `index.html` | Local HTML report viewer |
| `artifacts/parsed-cell.json` | Parsed cell grid (schema v0.1) |
| `artifacts/regions.json` | Extracted regions |
| `artifacts/regions-report.md` | Regions markdown report |
| `artifacts/primitives.json` | Classified primitives |
| `artifacts/primitives-report.md` | Primitives markdown report |
| `artifacts/plan-recommendations.json` | Plan recommendations (schema v0.1) |
| `artifacts/plan-report.md` | Plan markdown report |
| `images/input-image.<ext>` | Copy of input image (local only) |

None of these files should be committed. The `.local/` directory is gitignored.

---

## What the HTML viewer shows

The `index.html` report includes:

- Claim boundary notice (prominent, top of page).
- Pipeline summary cards: dimensions, regions, primitives, recommendations, warnings.
- Input section: the input image displayed alongside a metadata table (file name,
  dimensions, resize flag, thresholds, generation timestamp).
- Artifact links: card-style links to all JSON and markdown artifact files.
- Non-claims section: explicit list of what is not produced or claimed.

The viewer uses dark-themed static HTML and CSS. No JavaScript framework. No server.
Open directly from the filesystem. The input image is shown using a local relative
`<img>` reference (pixelated rendering for pixel-art maps).

Additional sections added in APP-3:

- **Visual Legend**: palette kinds with color swatches and pixel counts from the
  parsed cell. Shows exact/nearest/unmapped color match summary.
- **Nearest Color Drift**: table of pixels that had no exact palette match, showing
  source RGB, count, nearest kind, and distance. Omitted if all pixels matched exactly.
- **JSON Artifacts** and **Markdown Reports** split into separate sections for faster
  navigation.
- **Palette kinds card** in the Pipeline Summary row.
- **`--run-name <name>`**: writes output to `<output>/<sanitized-name>/` to support
  multiple named runs from the same source image.

The report uses plain static HTML and CSS. No JavaScript framework. No server.
Open directly from the filesystem.

---

## Pipeline stages

The `app-export` command runs the same sequence as `full-pipeline`:

1. Parse image -> `parsed-cell.json`
2. Load parsed cell -> SemanticGrid
3. Extract regions -> `regions.json` + `regions-report.md`
4. Classify primitives -> `primitives.json` + `primitives-report.md`
5. Evaluate planning rules -> `plan-recommendations.json` + `plan-report.md`
6. Write `index.html`

The only difference from `full-pipeline` is that artifacts go to
`<output>/artifacts/` and `index.html` is added at `<output>/index.html`.

---

## Safety boundaries

- Output is rejected unless the path contains a `.local` directory segment.
- No Project Zomboid assets are read or copied.
- The `media/maps` directory is never written.
- No lotpack, lotheader, or bin files are produced.
- Tile GIDs are not used; tile content is not read.

---

## Non-claims

- `app-export` does not produce a playable Project Zomboid map.
- The HTML viewer does not display PZ tiles or textures.
- No TileZed tile IDs are referenced or assigned.
- Build 41 / Build 42 compatibility is not claimed.
- The artifact links in the HTML viewer are relative paths; the viewer
  must be opened from its output directory or a browser that supports
  local file navigation.

---

## Next possible slices

These are not committed scope; they are candidate directions only.

| Candidate | What it would add |
|---|---|
| Preview image embedding | Copy or reference a generated preview PNG in index.html |
| TMX planning export | Add TileZed-openable TMX planning file to artifact set (blocked on tilesheet format gate -- see TILESHEET_FORMAT_INVESTIGATION_DECISION.md) |
| Palette visualization | Show palette swatches and kind legend in HTML |
| Region map overlay | SVG or canvas region outlines over a placeholder grid |
