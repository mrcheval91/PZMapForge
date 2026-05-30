# Roadmap

## Phase 1: ImageMapForge MVP

Goal: convert a deterministic blockout image into semantic map-planning outputs.

Status: scaffolded.

Deliverables:

- palette JSON
- image parser
- semantic grid JSON
- markdown report
- preview PNG
- TileZed-openable planning TMX

## Phase 2: Better semantic planning

Potential next steps:

- multi-layer image conventions
- object markers
- street labels
- zone definitions
- spawn/landmark validation
- round-trip validation of TMX layer size and gzip payload

## Phase 3: Real tile reference experiment

Potential next steps:

- local-only config for installed PZ asset paths
- generated TMX using locally referenced PZ tileset names
- no copied tilesheets in repository
- no playable claim

## Phase 4: PZ-compatible export research

Only after evidence from earlier phases:

- study WorldEd/TileZed export expectations separately
- document fragile or undocumented blockers honestly
- attempt local load tests
- claim playable only after real local load success
