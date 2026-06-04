# Compiled Cell Evidence Template

```text
Schema:         pzmapforge.compiled-cell-evidence-template.v0.1
Claim boundary: evidence_inventory_only_not_compiled_not_pz_load_tested
Status:         DRAFT — fill and save to .local/evidence/ (do not commit filled copy)
```

Instructions:
- Copy this file to .local/evidence/<descriptive-name>.md.
- Fill every section with direct local observations.
- Do not copy binary file content into this template.
- Do not copy PZ game assets into the repo.
- Do not commit this filled template if it contains binary excerpts.
- Mark each gap CLOSED only when you have direct observational evidence.

---

## Observation metadata

| Field | Value |
|---|---|
| Date observed | <!-- YYYY-MM-DD --> |
| PZ version | <!-- e.g. Build 41.78 or Build 42.x --> |
| Source of observation | <!-- WorldEd export / existing mod / other --> |
| Local path observed | <!-- e.g. C:\path\to\export (never a repo path) --> |
| Map or mod name | <!-- descriptive name --> |
| Observer notes | <!-- free text --> |

---

## Safety

| Property | Value |
|---|---|
| Any file copied into repo | false |
| Any PZ asset copied into repo | false |
| Playable export claimed | false |
| Compiled writer implemented | false |

---

## Directory tree summary

Paste the directory tree (file names and relative paths only, no binary content):

```
<!-- Example:
media/
  maps/
    mymap/
      map.info
      spawnpoints.lua
      0_0.lotheader
      0_0.lotpack
      map_0_0.bin
-->
```

---

## File inventory

| Relative path | Extension | Size (bytes) | Notes |
|---|---|---|---|
| <!-- fill --> | <!-- fill --> | <!-- fill --> | <!-- fill --> |

(Optionally attach compiled-cell-evidence.json from inspect-compiled-cell-evidence.ps1.)

---

## Cell coordinate naming

| Question | Observation |
|---|---|
| Are cell files named `<cx>_<cy>.<ext>`? | <!-- yes/no/unknown --> |
| What coordinate is cell 0? | <!-- e.g. top-left, or unknown --> |
| What is the observed cell coordinate range? | <!-- e.g. 0_0 only, or 100_100 --> |
| Is there a world grid file? | <!-- yes/no/unknown --> |
| Notes | <!-- free text --> |

---

## map.info observations

Paste the content of map.info (if it is a plain-text file):

```
<!-- Paste here if text-only. If binary, note "binary" and do not paste. -->
```

| Field observed | Value or unknown |
|---|---|
| title or name field | <!-- fill --> |
| lots field (cell range) | <!-- fill --> |
| Other fields | <!-- fill --> |
| Format: text or binary | <!-- fill --> |

---

## spawnpoints.lua observations

Paste the first few lines of spawnpoints.lua (Lua text only, no binary):

```lua
-- Paste here (Lua text only)
```

| Question | Observation |
|---|---|
| Format: Lua text or binary | <!-- fill --> |
| Variable name used | <!-- e.g. SpawnPoints = {} --> |
| Coordinate system (cell/chunk/tile) | <!-- fill or unknown --> |
| Notes | <!-- free text --> |

---

## .lotheader observations

Do not paste binary content. Describe what you can infer from file size and structure.

| Question | Observation |
|---|---|
| File size (bytes) | <!-- fill --> |
| SHA-256 of observed file | <!-- fill or leave blank --> |
| Appears to be text or binary | <!-- fill --> |
| Any readable header/magic bytes? | <!-- fill or unknown --> |
| Notes | <!-- free text --> |

---

## .lotpack observations

Do not paste binary content. Describe what you can infer from file size and structure.

| Question | Observation |
|---|---|
| File size (bytes) | <!-- fill --> |
| SHA-256 of observed file | <!-- fill or leave blank --> |
| Appears to be text or binary | <!-- fill --> |
| Any readable header/magic bytes? | <!-- fill or unknown --> |
| Notes | <!-- free text --> |

---

## map_<cx>_<cy>.bin observations

| Question | Observation |
|---|---|
| Present in export | <!-- yes/no/unknown --> |
| File size (bytes) | <!-- fill or N/A --> |
| Notes | <!-- free text --> |

---

## Minimum viable cell

| Question | Observation |
|---|---|
| How many cell files are present? | <!-- fill --> |
| Does a single cell appear sufficient? | <!-- yes/no/unknown --> |
| Was a load test performed? | <!-- yes/no --> |
| Load test result | <!-- pass/fail/not attempted --> |
| Notes | <!-- free text --> |

---

## Unknowns remaining after this observation

List any gaps from docs/COMPILED_CELL_FORMAT_EVIDENCE.md section 5 that remain OPEN:

- <!-- gap 1 -->
- <!-- gap 2 -->

---

## Risks

- <!-- e.g. format may differ between Build 41 and Build 42 -->
- <!-- e.g. coordinate origin not confirmed -->

---

## Gap closure status

Copy rows from docs/COMPILED_CELL_FORMAT_EVIDENCE.md section 5 and mark each:

| Gap | Status after this observation |
|---|---|
| `.lotheader` binary format | <!-- OPEN / PARTIAL / CLOSED --> |
| `.lotpack` binary format | <!-- OPEN / PARTIAL / CLOSED --> |
| Cell coordinate naming convention | <!-- OPEN / PARTIAL / CLOSED --> |
| Exact directory layout | <!-- OPEN / PARTIAL / CLOSED --> |
| Minimum viable cell count for load | <!-- OPEN / PARTIAL / CLOSED --> |
| Whether single cell loads without world grid | <!-- OPEN / PARTIAL / CLOSED --> |
| Spawn file format | <!-- OPEN / PARTIAL / CLOSED --> |
| Spawn coordinate system | <!-- OPEN / PARTIAL / CLOSED --> |
| map.info required fields | <!-- OPEN / PARTIAL / CLOSED --> |
| Build 41 vs Build 42 format differences | <!-- OPEN / PARTIAL / CLOSED --> |
