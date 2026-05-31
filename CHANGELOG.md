# Changelog

All notable changes to PZMapForge will be documented here.

Format: Keep a Changelog.

---

## [Unreleased]

### Added
- scripts/write-proof-packet.ps1: generates .local/mapforge/proof-packet.json
  and proof-packet.md with schema sentinel, UTC timestamp, git state,
  SHA-256 hashes of all 5 artifacts, expected validation counts, and safety
  flags. Runs validate.ps1 first if parsed-cell.json is missing.
- scripts/test-proof-packet.ps1: 32-assertion proof packet validator (output
  files exist, 15 required fields, schema/claim sentinels, 5 SHA-256 formats,
  validation summary counts, 4 safety flags).
- schemas/pzmapforge.proof-packet.v0.1.schema.json: JSON Schema for proof packet.
- scripts/validate.ps1: proof packet write + test steps added after hardening.
- docs/IMPLEMENTATION.md: proof packet row added to ratified table.

---

## [Unreleased - prev2]

### Added
- scripts/test-schema-files.ps1: schema file sanity validator (28 assertions:
  $schema, $id sentinel, title, required list for 11 fields, properties keys
  for those same fields). No external dependencies.
- scripts/validate.ps1: schema sanity step added before artifact contract step.
- docs/IMPLEMENTATION.md: schema sanity row added to ratified table.
- Fixed: CHANGELOG and IMPLEMENTATION.md had stale "33 assertions" for the
  parsed-cell contract; corrected to 40.

---

## [Unreleased - prev]

### Added
- scripts/test-parsed-cell-contract.ps1: deterministic artifact contract check
  (40 assertions: required fields, schema sentinel, claim_boundary, dimensions
  300x300, rows count and length, counts pixel sum, all 9 required kinds,
  outputs keys, matching fields and pixel sum).
- scripts/validate.ps1: contract step added before hardening test harness.
- docs/IMPLEMENTATION.md: contract validation row added to ratified table;
  gap 4 added for JSON Schema type validation.

---

## [Unreleased - prev]

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
