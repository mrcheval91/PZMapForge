# Claim Boundary

PZMapForge is an independent map-planning tool for a Project Zomboid mod workflow.

## Current verified claim

The current MVP can convert a palette/blockout image into deterministic planning artifacts:

```text
PNG or BMP input
  -> semantic 300x300 grid
  -> parsed-cell.json
  -> parsed-cell-report.md
  -> parsed-cell-preview.png
  -> parsed-cell-basic.tmx
```

The generated TMX is a TileZed-openable planning artifact using generated colour tiles.

## Not claimed

- Not a Project Zomboid playable map export.
- Not a WorldEd replacement for all workflows.
- Not an official Project Zomboid tool.
- Not a fork of TileZed or WorldEd.
- Not Build 42 compatible until verified by local load tests.
- Not a `lotpack`, `lotheader`, or `bin` exporter.

## Asset boundary

PZMapForge must not copy or redistribute Project Zomboid source code, tilesheets, sprites, sounds, or proprietary assets.

Future steps may reference a locally installed Project Zomboid copy for local generation only. Any such feature must keep generated local outputs out of git unless a separate documented load-test milestone proves they are safe and intended to commit.

## Output boundary

Generated artifacts belong under `.local/mapforge/` by default.

The tool refuses to write into `media/maps`. It also refuses external output paths unless explicitly run with `-AllowExternalOutput`.
