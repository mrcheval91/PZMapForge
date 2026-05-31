# CLAUDE.md

## Repository role

This repository is PZMapForge, an independent deterministic Project Zomboid mapmaker tool.

PZMapForge is not the Project Zomboid map mod itself. It is the tool layer that turns image or blockout inputs into local, inspectable planning artifacts.

Canonical local path:

```text
E:\Omni\Zomboid\PZMapForge
```

Related consumer repo:

```text
E:\Omni\Zomboid\pz-sud-ouest-montreal
```

The map-mod repo may consume generated planning artifacts locally, but PZMapForge owns the mapmaker tooling.

## Mission

Build a deterministic local map-planning pipeline for Project Zomboid-style map work without relying on fragile WorldEd GUI iteration.

Current narrow success condition:

```text
image or blockout input
  -> semantic grid
  -> parsed JSON
  -> markdown report
  -> preview PNG
  -> TileZed-openable planning TMX
```

No playable Project Zomboid export is claimed until a real local load test exists.

## Non-negotiable laws

- Deterministic over manual.
- Evidence over claims.
- Local-first.
- No fake playable claim.
- No official Project Zomboid tool claim.
- No copied Project Zomboid game source.
- No redistributed Project Zomboid assets.
- Local installed PZ assets may be referenced only for local generation.
- Do not commit generated `.local` files.
- Do not write into any `media/maps` folder.
- Do not generate or commit `lotpack`, `lotheader`, or `bin` map export files unless a real load test is explicitly achieved and documented.
- LLM output is advisory evidence only, never authority.
- Repository state, validation results, and committed files are the authority.

## Solcogito foundation files

Before broad feature work, make sure these files exist and are truthful:

```text
docs/GENESIS.md
docs/CONSTITUTION.md
docs/IMPLEMENTATION.md
docs/TOOL_USAGE.md
docs/CLAIM_BOUNDARY.md
docs/ROADMAP.md
docs/decisions/0001-independent-mapmaker-layer.md
docs/decisions/0002-planning-artifacts-before-playable-export.md
```

If any are missing, add them before expanding the tool.

## Current MVP surface

Expected layout:

```text
source/
  image-mapforge.ps1
  image-palette.json

tests/
  test-image-mapforge.ps1

scripts/
  validate.ps1
  new-test-image.ps1

docs/
  IMAGE_MAPFORGE.md
```

Expected generated outputs, all local-only:

```text
.local/mapforge/parsed-cell.json
.local/mapforge/parsed-cell-report.md
.local/mapforge/parsed-cell-preview.png
.local/mapforge/parsed-cell-tiles.png
.local/mapforge/parsed-cell-basic.tmx
```

## Implementation constraints

- PowerShell 5.1 compatible.
- Use `System.Drawing` for local image processing.
- Use `System.IO.Compression` for TMX gzip layer encoding.
- Keep source and docs ASCII-only unless there is an explicit reason.
- Prefer simple scripts over framework architecture.
- Do not add package managers, services, daemons, databases, or network calls for the MVP.
- Do not depend on WorldEd or TileZed to generate artifacts.
- TileZed may be used only as a visual inspection tool for generated planning TMX.

## TMX claim boundary

The generated TMX is a planning artifact.

Allowed claim:

```text
TileZed-openable planning TMX generated deterministically from image input.
```

Forbidden claim:

```text
Playable Project Zomboid map export.
```

Until proven by local load test, do not claim:

- playable map
- working PZ cell
- WorldEd-compatible production export
- Steam Workshop readiness
- Build 42 compatibility

## Work protocol

For every slice:

1. Inspect current repo state.

```powershell
git status --short --branch
git log --oneline -5
```

2. Read relevant docs before editing.

```powershell
Get-ChildItem docs -Recurse -File
```

3. Make the smallest coherent change.

4. Run validation.

```powershell
powershell -ExecutionPolicy Bypass -File ".\scripts\validate.ps1"
git diff --check
git status --short
```

5. If the slice changes ImageMapForge behavior, run the test harness directly.

```powershell
powershell -ExecutionPolicy Bypass -File ".\tests\test-image-mapforge.ps1"
```

6. Confirm `.local/` is not visible in git status.

7. Commit only source, tests, docs, schemas, and scripts.

8. Use clear commit messages.

## Commit style

Use concise imperative commit messages.

Examples:

```text
Add Solcogito foundation doctrine
Import hardened ImageMapForge MVP
Harden ImageMapForge validation and diagnostics
Add parsed-cell schema validation
Document planning artifact claim boundary
```

## Forbidden actions

Do not:

- write into `media/maps`
- copy PZ tilesheets into the repo
- commit generated `.local` artifacts
- commit local machine paths
- add cloud or web dependencies for core generation
- call the tool official
- claim playable export without load-test evidence
- create broad architecture before the current slice is validated
- keep duplicate nested source trees
- silently ignore failed validation

## Current recommended next slices

### Slice A: Foundation doctrine

Add Solcogito foundation docs and decision records.

Commit:

```text
Add Solcogito foundation doctrine
```

### Slice B: Import hardened ImageMapForge

Import the hardened implementation from:

```text
E:\Omni\Zomboid\pz-sud-ouest-montreal
branch: phase-1-canal-garage-cell
commit: 5944173 Harden ImageMapForge validation and diagnostics
```

Mapping:

```text
source/cellforge/image-mapforge.ps1      -> source/image-mapforge.ps1
source/cellforge/image-palette.json      -> source/image-palette.json
source/cellforge/test-image-mapforge.ps1 -> tests/test-image-mapforge.ps1
docs/IMAGE_MAPFORGE.md                   -> docs/IMAGE_MAPFORGE.md
```

Adapt paths to standalone repo layout.

Commit:

```text
Import hardened ImageMapForge MVP
```

### Slice C: Schema and artifact contract

Add JSON schema for `parsed-cell.json` and validate generated artifacts locally.

Commit:

```text
Add parsed-cell artifact schema validation
```

## Reporting format

When done, report:

```text
Commit:
Files changed:
Validation:
Claim boundary:
Remaining blockers:
Next recommended slice:
```

Be precise. Do not overclaim.
