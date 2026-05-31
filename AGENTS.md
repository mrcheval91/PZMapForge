# AGENTS.md

## Repository role

This repository is PZMapForge, an independent deterministic Project Zomboid mapmaker tool.

This file is the shared coding-agent instruction file for Codex-style agents and other repository agents. It is intentionally tool-neutral.

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

## Agent role

Act as a bounded implementation or review agent.

You may:

- inspect the repository
- edit source, tests, docs, schemas, and scripts
- run local validation
- propose commits
- produce review findings

You must not:

- treat your output as truth
- approve claims by yourself
- waive failed validation
- claim playable Project Zomboid export without load-test evidence
- copy or redistribute Project Zomboid assets
- write generated files into mod `media/maps` folders

Repository evidence is the authority.

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

## Required foundation files

Make sure these files exist and remain truthful:

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

If any are missing, add them before broad feature expansion.

## Expected MVP layout

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

These outputs must not be staged or committed.

## Implementation constraints

- PowerShell 5.1 compatible.
- Use `System.Drawing` for local image processing.
- Use `System.IO.Compression` for TMX gzip layer encoding.
- Keep source and docs ASCII-only unless explicitly justified.
- Prefer simple scripts over framework architecture.
- Do not add package managers, services, daemons, databases, or network calls for the MVP.
- Do not depend on WorldEd or TileZed to generate artifacts.
- TileZed may be used only as a visual inspection tool for generated planning TMX.

## TMX claim boundary

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

For every task:

1. Inspect repo state.

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

5. If ImageMapForge behavior changes, run the focused harness.

```powershell
powershell -ExecutionPolicy Bypass -File ".\tests\test-image-mapforge.ps1"
```

6. Confirm `.local/` is not visible in git status.

7. Stage only intentional files.

8. Commit only after validation passes.

## Review checklist

Before reporting success, verify:

- `.local/` is absent from git status.
- No `media/maps` writes were added.
- No PZ assets were copied.
- No playable export claim was added.
- Docs match current behavior.
- Tests or validation were run.
- `git diff --check` is clean.
- Generated outputs remain local-only.

## Search checks

Use these when appropriate.

Unsafe output and network search:

```powershell
Select-String -Path ".\source\*.ps1",".\scripts\*.ps1",".\tests\*.ps1" -Pattern "media\\maps|media/maps|lotpack|lotheader|Copy-Item|Invoke-WebRequest|Invoke-RestMethod"
```

Claim boundary search:

```powershell
Select-String -Path ".\README.md",".\docs\*.md" -Pattern "playable|official|Workshop|Build 42|load-tested|lotpack|lotheader"
```

Mentions in safety docs are allowed. Actual unsafe writes, copied assets, or overclaims are not.

## Commit style

Use concise imperative commit messages.

Examples:

```text
Add Solcogito foundation doctrine
Import hardened ImageMapForge MVP
Harden ImageMapForge validation and diagnostics
Add parsed-cell artifact schema validation
Document planning artifact claim boundary
```

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
