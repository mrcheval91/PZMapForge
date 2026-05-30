# PZMapForge

PZMapForge is an independent deterministic Project Zomboid map-planning layer.

It turns a simple image or blockout into local-only planning artifacts:

- parsed semantic cell JSON
- markdown report
- preview PNG
- TileZed-openable planning TMX using generated colour tiles

## Claim boundary

This repository does not generate a playable Project Zomboid map yet.

Current claim:

> Image input -> semantic grid -> preview PNG -> TileZed-openable planning TMX.

No `lotpack`, `lotheader`, or `bin` export is claimed. No Project Zomboid game source or assets are copied into this repository. Local installed Project Zomboid assets may only be referenced later for local generation after a documented boundary review.

## Quickstart

Create a deterministic sample image under `.local/mapforge/`:

```powershell
powershell -ExecutionPolicy Bypass -File "scripts\new-test-image.ps1"
```

Generate ImageMapForge outputs:

```powershell
powershell -ExecutionPolicy Bypass -File "source\image-mapforge.ps1" -ImagePath ".local\mapforge\sample-input.png"
```

Or run the local validation wrapper:

```powershell
powershell -ExecutionPolicy Bypass -File "scripts\validate.ps1"
```

Outputs are written under `.local/mapforge/` and are gitignored.

## Outputs

| File | Purpose |
|---|---|
| `.local/mapforge/parsed-cell.json` | Semantic grid and deterministic counts |
| `.local/mapforge/parsed-cell-report.md` | Human-readable generation report |
| `.local/mapforge/parsed-cell-preview.png` | Visual preview of parsed semantic grid |
| `.local/mapforge/parsed-cell-tiles.png` | Generated colour-strip tileset used by the TMX |
| `.local/mapforge/parsed-cell-basic.tmx` | TileZed-openable planning TMX |

## Documentation

- [ImageMapForge MVP](docs/IMAGE_MAPFORGE.md)
- [Claim boundary](docs/CLAIM_BOUNDARY.md)
- [Roadmap](docs/ROADMAP.md)

## Doctrine

- Deterministic over manual.
- Evidence over claims.
- No official tool claim.
- No copied Project Zomboid game source.
- No redistributed Project Zomboid assets.
- No fake playable claim.
- Local-only generated artifacts stay under `.local/`.
