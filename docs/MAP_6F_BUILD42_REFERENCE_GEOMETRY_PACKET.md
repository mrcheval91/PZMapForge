# MAP-6F: Build 42 Reference Geometry Inspector Packet

```text
Schema:           pzmapforge.geometry-inspector-packet.v0.1
Claim boundary:   evidence_record_only_not_load_tested_not_playable
PZ build:         Build 42
MAP-6E status:    GEOMETRY_MODEL_UNVERIFIED / BUILD42_256_MODEL_OPERATOR_REPORTED
MAP-6F scope:     inspector script to resolve geometry uncertainty
REFERENCE_GEOMETRY_OBSERVED — populated when operator runs inspector against reference map
PLAYABLE_EXPORT_CLAIM_ALLOWED=false
```

---

## 1. Purpose

MAP-6E recorded that PZMapForge assumes a 300×300 / 30×30 chunk geometry model
and that the operator has reported Build 42 may use 256×256. That uncertainty
blocks all further load tests (LOAD_TEST_BLOCKED_PENDING_GEOMETRY_DECISION).

MAP-6F adds `scripts/inspect-build42-reference-geometry.ps1` — a local-only
inspector that can resolve or narrow this uncertainty by inspecting a known-good
Build 42 map mod or WorldEd export that the operator manually places under
`.local/`.

---

## 2. Required operator action (not automated)

Before running the inspector, the operator must:

1. Obtain a known-good Build 42 map mod or WorldEd-exported map directory.
   Examples:
   - A Workshop mod confirmed to load correctly in Build 42.
   - A local WorldEd Build 42 export from a vanilla or modded cell.

2. Copy it manually into:
   ```
   .local/reference-build42-map/<map_folder_name>/
   ```

3. Run the inspector:
   ```powershell
   powershell -ExecutionPolicy Bypass -File scripts\inspect-build42-reference-geometry.ps1 `
     -Source ".local\reference-build42-map\<map_folder_name>" `
     -Output ".local\reference-build42-geometry-01"
   ```

4. Review `build42-reference-geometry-report.json` and `.md` for the geometry status.

**This script does NOT automate the copy.** The operator must perform steps 1-2
manually. This maintains the safety boundary: no PZ assets enter the repo.

---

## 3. What the inspector does

```
inspect-build42-reference-geometry.ps1
  -Source <path under .local>   (required)
  -Output <path under .local>   (required)
  -MaxFiles <int, default 20>   (optional)
```

- Refuses Source and Output outside `.local/`.
- Refuses Zomboid mods/Workshop/Server/PZ-install paths in Source.
- Scans source tree for `*.lotheader`, `world_*.lotpack`, `chunkdata_*.bin`,
  `map.info`, `mod.info`, `spawnpoints.lua`.
- For each `.lotpack`: reads first 8 bytes, parses hdrA (U32 LE) and hdrB (U32 LE),
  derives inferred table entry count and table end offset.
- For each `chunkdata_*.bin`: records file size, computes body_bytes (size − 2),
  maps body_bytes to chunk_grid_candidate:
  - `900` → `30x30_900` (300×300 model, 10-tile chunks)
  - `1024` → `32x32_1024` (256×256 model, 8-tile chunks candidate)
  - `256` → `16x16_256` (alternative 256-model candidate)
  - other → `unknown_body_N`
- For each `.lotheader`: reads first 32 bytes.
- Writes `build42-reference-geometry-report.json` and `.md` under Output.
- Does NOT copy files. Does NOT write to the repo. Does NOT read PZ assets.

---

## 4. Output geometry statuses

| Status | Condition |
|---|---|
| `BUILD42_300_MODEL_SUPPORTED` | All sampled lotpacks have hdrA=900 AND all chunkdata body=900, no 256-model evidence |
| `BUILD42_300_MODEL_PARTIALLY_SUPPORTED` | Some but not all evidence consistent with 300-model |
| `BUILD42_256_MODEL_SUPPORTED` | Any chunkdata body=1024 or body=256 observed |
| `BUILD42_GEOMETRY_STILL_UNKNOWN` | No binary files found, or results inconclusive |

---

## 5. What this resolves

If the inspector finds hdrA=900 and body=900 in a confirmed Build 42 mod:
- The 300×300 / 30×30 chunk model is supported for Build 42.
- LOAD_TEST_BLOCKED_PENDING_GEOMETRY_DECISION can be lifted.
- The current binary writer values (hdrA=900, 7208-byte lotpack, 902-byte chunkdata)
  would be supported for Build 42.

If the inspector finds hdrA=1024 or body=1024:
- BUILD42_256_MODEL_SUPPORTED is confirmed.
- Binary writers must be updated before any load test.
- hdrA, hdrB, lotpack size, and chunkdata size all need recalculation.

---

## 6. Safety record

| Property | Value |
|---|---|
| Reference files copied into repo | false — operator copies to .local only |
| PZ assets copied | false |
| media/maps touched in repo | false |
| Playable export claimed | false |
| Load test performed | false |
| Script writes to repo | false |

PLAYABLE_EXPORT_CLAIM_ALLOWED=false is binding regardless of what the inspector finds.

---

## 7. Non-claims

- This script does not perform a load test.
- This script does not modify any source files.
- This script does not copy PZ assets.
- Geometry status produced by the inspector is evidence, not a guarantee.
- The inspector requires a valid reference file to produce meaningful results.
- No playable export claim.
