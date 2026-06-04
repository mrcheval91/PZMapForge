# Compiled Cell Format Evidence

```text
Status:           MAP-4A evidence inventory process
Claim boundary:   evidence_inventory_only_not_compiled_not_pz_load_tested
Compiler status:  not implemented
PZ assets:        not copied into repo
media/maps:       forbidden in repo
```

---

## 1. Purpose

This document defines what compiled cell format evidence is required before
PZMapForge can implement a MAP-4 compiled cell writer. No writer is implemented
here. This is the evidence specification and gap record only.

MAP-4 is blocked until the evidence gaps listed in section 5 are closed by
direct local observation. No gap may be closed by inference, LLM output, or
documentation alone.

---

## 2. Why MAP-4 is blocked

MAP-3B (map-scaffold) produces a text-only mod skeleton under `.local/`. It
does not write compiled cell files. Project Zomboid requires compiled binary
files (`.lotpack`, `.lotheader`) for map cells to load. The exact binary format
of these files is unknown without direct local inspection of a known-good
WorldEd export.

The following are unknown and must not be assumed:

- Binary layout of `.lotheader`
- Binary layout of `.lotpack`
- Cell coordinate naming convention
- Minimum viable cell set for a map to load
- Whether a single cell is sufficient without a world grid
- Spawn file format and coordinate system
- Build 41 vs Build 42 format differences

---

## 3. Suspected file types and roles

These are suspected from public Project Zomboid modding knowledge. None have
been verified by local inspection for this repo.

| File type | Suspected role | Verified locally |
|---|---|---|
| `<cx>_<cy>.lotpack` | Packed cell tile data | No |
| `<cx>_<cy>.lotheader` | Cell geometry/header for game engine | No |
| `map_<cx>_<cy>.bin` | Auxiliary binary map data | No |
| `media/maps/<name>/map.info` | Map metadata (name, lots range) | No |
| `media/maps/<name>/spawnpoints.lua` | Player spawn definitions | No |

Where `<cx>` and `<cy>` are cell X/Y coordinates. The exact naming convention
(e.g. `0_0`, `100_100`) is unknown without local evidence.

---

## 4. Directory layout hypothesis

Based on public modding resources — not locally confirmed. Must not be treated
as authoritative.

```text
<mod root>/
  mod.info
  media/
    maps/
      <map_name>/
        map.info
        spawnpoints.lua
        <cx>_<cy>.lotpack
        <cx>_<cy>.lotheader
        map_<cx>_<cy>.bin      (may be optional)
```

The cell files may live directly in `media/maps/<map_name>/` or in a
subdirectory. This must be confirmed by direct inspection.

---

## 5. Evidence gaps — must be closed before MAP-4

| Gap | Investigation method | Status |
|---|---|---|
| `.lotheader` binary format | Inspect a known-good WorldEd export locally with `inspect-compiled-cell-evidence.ps1` | OPEN |
| `.lotpack` binary format | Inspect a known-good WorldEd export locally with `inspect-compiled-cell-evidence.ps1` | OPEN |
| Cell coordinate naming convention | Observe file names in a WorldEd export | OPEN |
| Exact directory layout for cell files | Observe directory tree in a WorldEd export | OPEN |
| Minimum viable cell count for load | Local load test with smallest possible cell | OPEN |
| Whether single cell loads without world grid | Local load test | OPEN |
| Spawn file format (`spawnpoints.lua`) | Inspect existing mod spawn files locally | OPEN |
| Spawn coordinate system (cell/chunk) | Inspect mod spawn files and PZ source references | OPEN |
| `map.info` required fields | Inspect existing mod map.info files locally | OPEN |
| Build 41 vs Build 42 format differences | Local comparison; no assumption made | OPEN |

All gaps must be marked CLOSED with a filled evidence template before MAP-4
implementation begins. No gap may be closed by assumption.

---

## 6. How to collect evidence

### Step 1: obtain a known-good WorldEd export

Export a minimal test map using WorldEd to a local directory outside this
repo. Do not copy any output into this repo.

### Step 2: run the evidence inspector

```powershell
powershell -ExecutionPolicy Bypass -File "scripts\inspect-compiled-cell-evidence.ps1" `
    -Path  "C:\path\to\your\worlded-export" `
    -Output ".local\evidence\worlded-export-01"
```

The script enumerates file names, extensions, sizes, and SHA-256 hashes.
It does not copy files into the repo. Output stays under `.local/`.

### Step 3: fill the evidence template

Copy `docs/examples/compiled-cell-evidence/COMPILED_CELL_EVIDENCE_TEMPLATE.md`
to `.local/evidence/` and fill it with your observations.

The template is not evidence itself. The filled copy in `.local/` is local
evidence. Do not commit filled evidence templates to the repo unless they
are sanitized and contain no PZ binary content.

### Step 4: close gaps

For each gap in section 5 above:
- Record the local finding in the filled template.
- Update this document to mark the gap CLOSED.
- Note the PZ version, build, and mod source.

---

## 7. Forbidden actions during evidence collection

- Do not copy `.lotpack`, `.lotheader`, `.bin`, or any compiled cell files
  into this repo.
- Do not copy PZ game assets (tiles, textures, sprites) into this repo.
- Do not commit the filled evidence template if it contains binary excerpts.
- Do not write to `media/maps/` inside this repo.
- Do not begin a MAP-4 writer implementation until all gaps in section 5
  are closed.
- Do not claim playable export at any stage of evidence collection.
- Do not infer binary formats from documentation or LLM output.

---

## 8. Decision gate for MAP-4

MAP-4 implementation is permitted only when all of the following are true:

- [ ] All gaps in section 5 are marked CLOSED.
- [ ] At least one filled evidence template exists in `.local/`.
- [ ] The cell coordinate naming convention is confirmed.
- [ ] The directory layout is confirmed.
- [ ] `map.info` required fields are confirmed.
- [ ] `spawnpoints.lua` format is confirmed.
- [ ] Binary formats of `.lotheader` and `.lotpack` are described at byte level
      (field offsets, types, and sizes), not just from file presence.
- [ ] A minimum viable cell count is estimated.
- [ ] No PZ assets were copied into this repo during evidence collection.
- [ ] This document has been updated with CLOSED status for all gaps.
- [ ] A decision record (`docs/decisions/`) is committed before any writer is
      implemented.

---

## 9. Tools

| Tool | Purpose |
|---|---|
| `scripts/inspect-compiled-cell-evidence.ps1` | Enumerates files, sizes, SHA-256 — local only, no copying |
| `docs/examples/compiled-cell-evidence/COMPILED_CELL_EVIDENCE_TEMPLATE.md` | Fillable observation template |

---

## 10. Claim boundary

This document and all associated scripts are evidence inventory only:

```text
claim_boundary: evidence_inventory_only_not_compiled_not_pz_load_tested
```

No compiled writer is implemented. No PZ assets are read or copied.
No playable export is claimed. No `media/maps` writes occur in the repo.
