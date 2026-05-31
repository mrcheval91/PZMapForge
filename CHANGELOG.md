# Changelog

All notable changes to PZMapForge will be documented here.

Format: Keep a Changelog.

---

## [Unreleased]

### Added
- Import hardened ImageMapForge MVP from pz-sud-ouest-montreal@5944173.
  - source/image-mapforge.ps1: RGB palette format, Fail+exit pattern, Debug
    exits early (no artifacts), nearest-colour drift cache and drift records
    in JSON and report.
  - source/image-palette.json: RGB array format (9 kinds, contiguous GIDs).
  - tests/test-image-mapforge.ps1: 28-assertion hardening harness.
- docs/GENESIS.md: why PZMapForge exists, scope, identity.
- docs/CONSTITUTION.md: non-negotiable behavioral rules.
- docs/IMPLEMENTATION.md: ratified, provisional, and absent capabilities.
- docs/TOOL_USAGE.md: parameters, examples, palette format, colour matching.
- docs/decisions/0001-independent-mapmaker-layer.md: decision record.
- docs/decisions/0002-planning-artifacts-before-playable-export.md: decision record.
- schemas/pzmapforge.parsed-cell.v0.1.schema.json: JSON Schema for artifact.
- examples/README.md: how to create and use a custom blockout image.
- LICENSE: MIT.
- scripts/validate.ps1: updated to run tests/test-image-mapforge.ps1.
- scripts/new-test-image.ps1: updated to read RGB array palette format.

### Removed
- scripts/test-image-mapforge.ps1: moved to tests/.

---

## [0.0.1] - 2026-05-30

### Added
- Initial independent PZMapForge repository scaffold.
- ImageMapForge MVP script (hex palette format, symbol field).
- Palette configuration (hex colour format).
- Local sample image generator and validation wrapper.
- Claim boundary, ImageMapForge, and roadmap documentation.
- Drift tracking port from pz-sud-ouest-montreal (ff0a21f).
