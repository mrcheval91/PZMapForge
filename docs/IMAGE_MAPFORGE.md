# ImageMapForge MVP

ImageMapForge converts a simple PNG or BMP blockout into deterministic map-planning artifacts.

It is designed for fast iteration when the WorldEd GUI path is too slow or fragile.

## .NET engine

`ImageMapForgeParser.Parse(imagePath, palettePath, options)` in
`PZMapForge.Core.ImageParsing` ports the parsing logic to typed C#/.NET 10:

- 300x300 images parse directly.
- Non-300x300 images fail unless `Resize = true`.
- Same exact-match / nearest-colour algorithm as the PS reference.
- Returns `ImageMapForgeResult` with rows, counts, matching stats, palette SHA-256, and
  a `BuildGrid()` helper to get a `SemanticGrid` for the downstream .NET pipeline.
- Windows-only (System.Drawing.Common/GDI+).
- Claim boundary: `planning_artifact_only_not_pz_load_tested`.

CLI commands:

```
image-check --path <image> --palette <palette> [--resize]
```
Read-only: prints dimensions, resized, row/kind counts, exact/nearest/unmapped
pixel counts, palette SHA-256, and status. Does not write artifact files.

```
image-export --path <image> --palette <palette> [--output <dir>] [--resize]
```
Writes `parsed-cell.json` to `--output` (default `.local/mapforge`). Refuses
output outside a `.local/` directory. Output is loadable by `ParsedCellLoader`
and compatible with the full .NET downstream pipeline.

## What it does

The MVP reads an input image, maps each pixel to a semantic cell kind, and writes:

- `parsed-cell.json`
- `parsed-cell-report.md`
- `parsed-cell-preview.png`
- `parsed-cell-tiles.png`
- `parsed-cell-basic.tmx`

The default target size is 300x300 pixels, matching one Project Zomboid cell planning grid.

## Exact claim boundary

This is a planning tool only.

The generated TMX is intended to open in TileZed as a visual planning layer using generated colour tiles. It is not a Project Zomboid load-tested map export. It does not produce `lotpack`, `lotheader`, or `bin` files.

## Palette format

The palette is stored at:

```text
source/image-palette.json
```

Each kind has:

- `kind`: semantic cell kind
- `gid`: TMX tile GID
- `symbol`: one-character symbol used in `parsed-cell.json` rows
- `color`: hex RGB colour used for image matching and preview generation

Supported semantic kinds:

- `grass`
- `road`
- `sidewalk`
- `row_house`
- `depanneur`
- `garage`
- `industrial_yard`
- `landmark`
- `spawn`

## How to run

Generate the deterministic sample input image:

```powershell
powershell -ExecutionPolicy Bypass -File "scripts\new-test-image.ps1"
```

Run ImageMapForge:

```powershell
powershell -ExecutionPolicy Bypass -File "source\image-mapforge.ps1" -ImagePath ".local\mapforge\sample-input.png"
```

If your input is not 300x300, pass `-Resize`:

```powershell
powershell -ExecutionPolicy Bypass -File "source\image-mapforge.ps1" -ImagePath "path\to\blockout.png" -Resize
```

Debug colour frequencies and nearest mappings:

```powershell
powershell -ExecutionPolicy Bypass -File "source\image-mapforge.ps1" -ImagePath "path\to\blockout.png" -Mode Debug
```

## Where outputs go

By default, all generated artifacts go under:

```text
.local/mapforge/
```

This directory is gitignored.

The tool refuses to write outside `.local/mapforge/` unless `-AllowExternalOutput` is explicitly provided. It refuses to write into `media/maps`.

## Why outputs are planning artifacts only

The TMX uses generated colour-strip tiles. Those tiles are not Project Zomboid game tiles. The TMX is useful for visual review in TileZed, semantic inspection, and deterministic pipeline testing, but it is not a playable map export.

## Difference from WorldEd and TileZed

WorldEd and TileZed are GUI tools used by Project Zomboid mapping workflows.

ImageMapForge is a local deterministic generator. It does not replace the full GUI workflow. It creates inspectable intermediate artifacts from an image so layout work can proceed without manual GUI trial-and-error.

## Next steps toward real PZ-compatible export

1. Add a validator for TMX structure and base64/gzip payload size.
2. Add optional local-only config for installed PZ asset paths.
3. Generate real-tile reference TMX using local assets without copying tilesheets.
4. Research the lotpack/lotheader/bin export path separately.
5. Only claim playable output after a real Project Zomboid local load test.
