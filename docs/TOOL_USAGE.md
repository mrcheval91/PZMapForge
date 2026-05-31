# Tool Usage

## Prerequisites

- Windows PowerShell 5.1 or later
- .NET Framework 4.x (System.Drawing, System.IO.Compression)
- No TileZed or WorldEd installation required to run ImageMapForge

---

## Quickstart

Generate a deterministic sample input image and run the full pipeline:

```powershell
cd E:\Omni\Zomboid\PZMapForge
powershell -ExecutionPolicy Bypass -File "scripts\validate.ps1"
```

This runs `new-test-image.ps1`, `image-mapforge.ps1`, checks all 5 outputs,
then runs the full 28-assertion test harness. All outputs land in `.local/`.

---

## Running ImageMapForge directly

```powershell
powershell -ExecutionPolicy Bypass -File "source\image-mapforge.ps1" -ImagePath "path\to\blockout.png"
```

### Parameters

| Parameter | Required | Default | Description |
|---|---|---|---|
| `-ImagePath` | Yes | — | Path to input PNG or BMP |
| `-Mode` | No | `Palette` | `Palette` (full run) or `Debug` (diagnostics only, no output files) |
| `-Resize` | No | off | Resize input to palette cell dimensions using nearest-neighbour |
| `-OutputDir` | No | `.local\mapforge` | Output directory. Must be under `.local\mapforge` unless `-AllowExternalOutput` |
| `-AllowExternalOutput` | No | off | Allow output outside `.local\mapforge` |

### Examples

Run against a 300x300 blockout:
```powershell
powershell -ExecutionPolicy Bypass -File "source\image-mapforge.ps1" -ImagePath ".local\mapforge\mymap.png"
```

Run Debug mode to inspect colours before generating artifacts:
```powershell
powershell -ExecutionPolicy Bypass -File "source\image-mapforge.ps1" -ImagePath ".local\mapforge\mymap.png" -Mode Debug
```

Resize a non-300x300 image and run:
```powershell
powershell -ExecutionPolicy Bypass -File "source\image-mapforge.ps1" -ImagePath "bigmap.png" -Resize
```

Output to a different mod repo (external path allowed):
```powershell
powershell -ExecutionPolicy Bypass -File "source\image-mapforge.ps1" `
    -ImagePath "blockout.png" `
    -OutputDir "E:\Omni\Zomboid\my-mod\.local\mapforge" `
    -AllowExternalOutput
```

---

## Outputs

All outputs go to `.local\mapforge\` (gitignored):

| File | Contents |
|---|---|
| `parsed-cell.json` | Schema-versioned semantic grid, legend, counts, drift records, SHA-256 hashes |
| `parsed-cell-report.md` | Human-readable report: claim boundary, matching summary, drift table, counts, legend |
| `parsed-cell-preview.png` | Visual preview rendered from the semantic grid at 3px/tile |
| `parsed-cell-tiles.png` | Generated colour-strip tileset (one 32x32 tile per palette kind) |
| `parsed-cell-basic.tmx` | TileZed-openable planning TMX using the generated tileset |

---

## Palette format

The palette at `source/image-palette.json` maps input image RGB colours to
semantic planning kinds.

```json
{
  "schema": "pzmapforge.image-palette.v0.1",
  "cell_width": 300,
  "cell_height": 300,
  "preview_scale": 3,
  "tile_size": 32,
  "kinds": [
    {
      "kind": "grass",
      "code": "g",
      "gid": 1,
      "rgb": [100, 140, 70],
      "description": "Grass or open ground"
    }
  ]
}
```

Rules:
- All 9 required kinds must be present.
- GIDs must be contiguous from 1.
- Codes must be exactly one character, unique.
- RGB values must be in range 0..255.

---

## Colour matching

Exact match: pixel RGB matches a palette entry exactly. Recorded as an exact pixel.

Nearest-colour match: pixel RGB does not match any palette entry. The entry with
the smallest squared RGB distance is used. Each unique unmapped colour is resolved
once and cached. The drift table in the report and JSON shows all nearest-colour
mappings with their distance.

Use `-Mode Debug` to see colour frequencies and unmapped colours before running
the full pipeline.

---

## Running the tests

```powershell
powershell -ExecutionPolicy Bypass -File "tests\test-image-mapforge.ps1"
```

Expected: 28 assertions, 28 passed, exit 0.
