# Image-to-Map App Export

Status: Slice 3A-6 implemented; APP-2–7 app export improvements; SVG-2 structure inspector; SVG-9 planning manifest

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
| `--annotation <image>` | No | Labeled reference image; copied to `images/` and shown as Annotation Reference panel; not used for parsing |
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
- **Palette kinds card** in the Summary row.
- **`--run-name <name>`**: writes output to `<output>/<sanitized-name>/` to support
  multiple named runs from the same source image.

Workbench layout added in APP-4:

- **Two-column workbench layout** (`div.workbench`): left panel (Map Preview + Visual
  Legend) and right panel (Summary cards + metadata + Artifact Files + Non-claims).
- **Map Preview** section: input image displayed up to 600px wide with pixelated
  rendering. Expands to fill the left panel.
- **Artifact Files** section in the right panel: all seven artifact links in a compact
  grid (JSON and markdown combined).
- Responsive: stacks to single column below 860px.
- Section headers: Map Preview, Summary, Visual Legend, Palette Health, Artifact Files, Non-claims.

Added in APP-5:

- **Original Input / Parsed Preview** side-by-side in the Map Preview section. The
  parsed preview renders each pixel in its snapped palette color, showing what the
  pipeline actually interpreted.
- **Palette Health** section in the right panel: health badge (Palette clean /
  Not palette-clean / Unknown), color match stats, and guidance text. The badge
  reflects whether the blockout contained off-palette colors (text labels,
  antialiasing, gradients). The guidance note "Text labels and antialiasing can
  affect parsing" is always shown.
- `images/parsed-preview.png` written alongside `images/input-image.<ext>` in the
  output directory.

Added in APP-6:

- **Annotation Reference** panel: when `--annotation <image>` is provided, the image
  is copied to `images/annotation-image.<ext>` and displayed as a third preview panel
  labelled "Annotation Reference". Not used for parsing.
- **Analysis Input** label replaces "Original Input" to clarify the split between the
  clean analysis image (used for parsing) and the annotated reference image.
- Palette Health guidance explicitly states: "Use `--path` for a clean palette-only
  analysis image. Text labels and antialiasing should not be part of the analysis image."
- If `--annotation` is provided but the file does not exist, the command exits 1 with
  a clear error message.

Added in APP-7:

- **SVG annotation detection**: when `--annotation` extension is `.svg`, the panel
  label changes to "SVG Vector Reference" and a guidance note appears in the viewer.
- **Guidance text** (always visible when SVG): "SVG is displayed as a reference only.
  SVG is not parsed into map geometry. Streets, borough limits, IDs, labels, and paths
  are not converted in this slice."
- **`artifacts/svg-reference-summary.json`** written when annotation is SVG. Contains
  schema, claim boundary, source/copied file names, file size, extension, `detected_svg`,
  `parsed_as_geometry` (false), and all safety flags.
- SVG annotation is displayed via a standard `<img src="...">` reference — browser
  renders inline SVG from file. No SVG parsing or geometry conversion.

Added in SVG-2:

- **`artifacts/svg-reference-structure.json`** written when annotation is SVG. Contains
  schema v0.1, element counts (svg/g/path/polyline/polygon/line/rect/circle/ellipse/
  text/image/use), id\_count, class\_count, sample\_ids (max 20), sample\_classes
  (max 20), sample\_text\_labels (max 20), likely flags, and all safety flags
  (`parsed_as_geometry: false`, `converted_to_map_geometry: false`).
- **SVG Structure Summary** panel added to the HTML right panel when SVG annotation
  is present. Shows: parse_status badge, metadata table (file, size, root, width,
  height, viewBox), Element Counts table, Sample IDs chips (max 20), Sample Text
  Labels chips (max 20), non-conversion note, artifact links. Backed by
  `SvgStructureResult` record; no JSON re-parsing.
- SVG XML is parsed safely (DTD disabled, no external entities, no network).
  Only element names, attribute values, and text node content are read.
  Path coordinate geometry is counted but not extracted or interpreted.
- **`artifacts/svg-layer-candidates.json`** (SVG-4/5): pattern-classifies IDs, class names, and
  text labels into candidate buckets: water, outline, technical\_layer, borough/district,
  street/route, transit/station, park/green\_space, labels, unknown. Method:
  `metadata_name_pattern_only`. Includes `candidate_generation_notes` (rules applied) and
  `inspected_metadata_sources`. No coordinates read. All safety flags false.
- **`--svg-selection <json>`** (SVG-8): optional argument accepting an operator-edited
  selection file. Copies it to `artifacts/svg-layer-selection.input.json` and writes
  `artifacts/svg-layer-selection-review.json` (schema v0.1, `selected_count`,
  `selected_items` with bucket/value/intended\_use/operator\_note). HTML shows
  "SVG Selection Review" panel with selected chips and notes that selected candidates
  are not converted to geometry and not exported to Project Zomboid.
- **`artifacts/svg-layer-selection.template.json`** (SVG-7): operator-editable selection
  template generated from the candidate samples. Each candidate item has `value`,
  `selected: false`, `intended_use: ""`, `operator_note: ""`. `selection_status`:
  `"operator_review_required"`. Selecting an item here does not convert SVG geometry.
- **SVG Layer Selection Template** HTML panel: links to the template and explicitly
  states "Selecting a candidate does not convert SVG geometry."
- Classification now uses all collected IDs/classes/text labels (up to 500 each, deduplicated),
  not just the 20-item samples. JSON reports `total_id_values_inspected`,
  `total_class_values_inspected`, `total_text_labels_inspected`,
  `total_metadata_values_inspected`. Bucket counts reflect the full classified set;
  samples are still capped at 30. HTML shows "Metadata Values Inspected" totals.
- Word-boundary matching for "eau"/"lac" prevents false positives like "Plateau" → water.
- All-caps alphabetic text labels (length ≥ 4) classified as transit/station candidates.
- **SVG Layer Candidates** HTML panel: chip display of each bucket with count, plus
  explicit note "These are metadata candidates only. No SVG geometry is converted."
- Parse cap raised to 50 MB (`max_characters_in_document: 50_000_000`).
  `parse_status` is `"parsed"` on success or `"failed"` with `parse_error` on failure.
  Failures are recorded honestly — the structure JSON is still written.

Added in SVG-9:

- **`artifacts/svg-planning-manifest.json`**: written when `--svg-selection` is provided
  and `selected_count > 0`. Schema `pzmapforge.svg-planning-manifest.v0.1`. Fields:
  `schema`, `claim_boundary`, `source_selection_file_name`, `generated_from`,
  `selected_count`, `selected_by_bucket` (grouped items with `value`, `intended_use`,
  `operator_note`), `intended_uses` (distinct sorted list), `operator_notes` (bounded
  at 50), `planning_status: "operator_selected_metadata_only"`, and all safety flags
  (`parsed_as_geometry`, `converted_to_map_geometry`, `exported_to_project_zomboid`,
  `pz_assets_copied`, `media_maps_touched`, `playable_export_claimed`: all false).
- **`artifacts/svg-planning-manifest.md`**: human-readable planning manifest. Includes:
  title "SVG Planning Manifest", claim boundary, source, selected count, planning
  status, selected items grouped by bucket, intended uses list, and an explicit
  non-claims section: "No SVG geometry converted", "No SVG coordinates extracted",
  "No Project Zomboid export generated", "No media/maps writes", "No PZ assets
  copied or read". ASCII only.
- **SVG Planning Manifest** HTML section in the right panel. States: "This is an
  inert planning manifest. It records selected SVG metadata only. It does not convert
  or export SVG geometry." Links `svg-planning-manifest.json` and
  `svg-planning-manifest.md`.
- If `--svg-selection` is provided but `selected_count == 0`: command does not fail;
  review artifact is still written; manifest files are not written; HTML section says
  "No selected SVG metadata was available for a planning manifest."
- 9 new tests: manifest JSON written, manifest MD written, manifest JSON contains
  `operator_selected_metadata_only`, manifest JSON contains
  `exported_to_project_zomboid: false`, manifest MD contains "SVG Planning Manifest",
  manifest MD contains "No SVG geometry converted", index.html contains "SVG Planning
  Manifest", index.html contains "inert planning manifest", zero-selected does not fail
  and does not write manifest. Total: 327 tests (190 Core + 137 CLI).

Added in SVG-10:

- **SVG Planning Manifest HTML summary**: when `--svg-selection` is provided and
  `selected_count > 0`, the SVG Planning Manifest section in the HTML right panel now
  shows the manifest contents directly in-page:
  - Metadata table: `selected_count` and `planning_status: operator_selected_metadata_only`.
  - Intended uses: chip list of distinct `intended_use` values from selected items.
  - Selected items grouped by bucket: chip + `intended_use` label + `operator_note`
    (italic) per item, matching the SVG Selection Review layout.
  - Non-claims list (inline, not only in the artifact): "No SVG geometry converted",
    "No SVG coordinates extracted", "No Project Zomboid export generated",
    "No media/maps writes", "No PZ assets copied or read".
  - Artifact links to `svg-planning-manifest.json` and `svg-planning-manifest.md`
    retained.
- If `selected_count == 0`: behavior unchanged. HTML says "No selected SVG metadata
  was available for a planning manifest." No table or items rendered.
- 8 new tests: index.html contains `selected_count`, `operator_selected_metadata_only`,
  `Eaux`, `water body`, `No SVG geometry converted`, `No SVG coordinates extracted`,
  `No Project Zomboid export generated`; zero-selected HTML contains "no selected SVG
  metadata was available". Total: 335 tests (190 Core + 145 CLI).

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
