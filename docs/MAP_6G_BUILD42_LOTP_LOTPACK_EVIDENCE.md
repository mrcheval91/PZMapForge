# MAP-6G: Build 42 LOTP Lotpack Evidence

```text
Schema:           pzmapforge.evidence-record.v0.1
Claim boundary:   evidence_record_only_not_load_tested_not_playable
PZ build:         Build 42
Reference mod:    Dru_map (Drummondville) — manually copied to .local/reference-build42-map/Dru_map
Inspector:        scripts/inspect-build42-reference-geometry.ps1 (MAP-6F, hardened in MAP-6G)
BUILD42_LOTP_FORMAT_OBSERVED
LEGACY_900_LOTPACK_HEADER_NOT_APPLICABLE_TO_REFERENCE
GEOMETRY_MODEL_STILL_UNVERIFIED
PLAYABLE_EXPORT_CLAIM_ALLOWED=false
```

---

## 1. Reference run context

The operator manually copied the Drummondville Workshop mod into:
```
.local/reference-build42-map/Dru_map/
```

Original Workshop source path (not read by PZMapForge tools):
```
D:\Program Files (x86)\Steam\steamapps\workshop\content\108600\3355966216\mods\Dru_map
```

The inspector was then run (MAP-6F):
```powershell
powershell -ExecutionPolicy Bypass -File scripts\inspect-build42-reference-geometry.ps1 `
  -Source ".local\reference-build42-map\Dru_map" `
  -Output ".local\reference-build42-geometry-drummondville-lotp"
```

No PZ assets were copied into the repo. The source path under `.local/` was operator-provided.

---

## 2. Inspector discovery counts

| File type | Count found |
|---|---|
| `.lotheader` files | 20 |
| `world_*.lotpack` files | 20 |
| `chunkdata_*.bin` files | 20 |
| `map.info` | 1 |
| `mod.info` | 2 |
| `spawnpoints.lua` | 1 |

---

## 3. LOTP magic header evidence

The first sampled lotpack: `common\media\maps\Dru_map\world_0_0.lotpack`

| Field | Value |
|---|---|
| File size | 1,057,348 bytes |
| First 4 bytes (hex) | `4C 4F 54 50` |
| ASCII interpretation | `LOTP` |
| Bytes 4-7 (U32 LE) | `01 00 00 00` = 1 |

Decimal interpretation: `0x4C4F5450 = 1347702604`.

**`LOTP` is a 4-byte ASCII magic marker.** This is not a chunk-count field. The legacy MAP-4F analysis that read bytes 0-3 as `hdrA` (chunk count ≈ 900) does not apply here. Build 42 uses a new lotpack format identified by the `LOTP` magic header.

---

## 4. Inspector overflow caused by MAP-6F

The original MAP-6F inspector read bytes 0-3 as `hdrA` (U32 LE) without detecting the LOTP magic. For the Drummondville mod:

```
hdrA = 1347702604 (= 0x50544F4C, reversed byte order of LOTP)
inferred_table_bytes = [int] 1347702604 × 8 = 10,781,620,832
```

This overflows `System.Int32` (max 2,147,483,647), causing:
```
Cannot convert value "10781620832" to type "System.Int32"
```

MAP-6G fixes the inspector by:
1. Detecting the LOTP magic at bytes 0-3.
2. Not computing table sizes for LOTP records.
3. Using `[int64]` for all table byte computations in legacy records.

---

## 4b. Chunkdata body evidence from the same reference run

From the Drummondville smoke run (all 20 chunkdata files inspected):

| body_bytes | count | chunk_grid_candidate |
|---|---|---|
| 1024 | 19 | 32×32_1024 |
| other | 1 | unknown (one file different) |

All 19 "minimal" cells have `chunkdata_x_y.bin` size = 1026 bytes:
- 2-byte header: `00 01` (consistent with Build 41 observation)
- 1024-byte body = 32 × 32 chunk grid

**This strongly supports the 256×256 tile model:**
```
32 chunks per side × 8 tiles per chunk = 256 tiles per side
→ 256 × 256 = 65536 tiles per cell
```

This is consistent with the operator-reported BUILD42_256_MODEL_OPERATOR_REPORTED observation from MAP-6E. The evidence is now BUILD42_256_MODEL_OPERATOR_REPORTED + chunkdata body=1024 supporting observation.

The full geometry claim for Build 42 remains GEOMETRY_MODEL_STILL_UNVERIFIED until:
- The 2-byte `00 01` header role in Build 42 chunkdata is confirmed.
- The tiles-per-chunk value (8 tiles for 256-model) is confirmed.
- A successful load test is achieved.

Also noted: lotheader files have field0 = 1213484876 = 0x485A544C.
Bytes in file order (LE): `4C 54 5A 48` = ASCII "LTZH". This may be a Build 42
lotheader magic header (analogous to LOTP for lotpacks). Not yet confirmed.

---

## 5. What LOTP means for the geometry model

The LOTP magic confirms that Build 42 has changed the lotpack format. However, the LOTP format is not yet parsed. We do not know:
- The LOTP internal structure.
- Whether the chunk grid (and therefore cell geometry) changed.
- Whether Build 42 chunkdata format also changed.

The `chunkdata_*.bin` files from this reference run have not yet been analyzed for body size. Their body size would tell us whether Build 42 still uses a 900-byte (30×30) or different chunk grid.

**GEOMETRY_MODEL_STILL_UNVERIFIED**: While we know the lotpack format changed in Build 42, we cannot determine whether the underlying cell tile dimensions changed from 300×300 until chunkdata body sizes are inspected from a Build 42 reference.

---

## 6. Status labels

```text
BUILD42_LOTP_FORMAT_OBSERVED
LEGACY_900_LOTPACK_HEADER_NOT_APPLICABLE_TO_REFERENCE
GEOMETRY_MODEL_STILL_UNVERIFIED
PLAYABLE_EXPORT_CLAIM_ALLOWED=false
```

- `BUILD42_LOTP_FORMAT_OBSERVED`: The `LOTP` magic header was detected in Drummondville reference lotpacks. Build 42 uses a new lotpack format.
- `LEGACY_900_LOTPACK_HEADER_NOT_APPLICABLE_TO_REFERENCE`: The MAP-4F hdrA=900 / hdrB=7204 offset table structure applies to the observed Build 41 Workshop mods. It does not apply to the Build 42 Drummondville reference. The current PZMapForge binary writer (hdrA=900, 7208-byte lotpack) produces files incompatible with Build 42.
- `GEOMETRY_MODEL_STILL_UNVERIFIED`: The tile cell geometry (300×300 or 256×256) is not determinable from lotpack magic alone. Chunkdata body size inspection would narrow this.
- `PLAYABLE_EXPORT_CLAIM_ALLOWED=false`: Binding, unchanged.

---

## 7. Impact on binary writer

The MAP-5A experimental lotpack writer generates:
- Bytes 0-3: `84 03 00 00` (hdrA=900, chunk count)
- Bytes 4-7: `24 1C 00 00` (hdrB=7204, table end)
- Total: 7208 bytes

Build 42 lotpacks start with `LOTP` magic. The current writer produces files that Build 42 will not recognize as valid lotpacks.

No change to the writer is made in MAP-6G. The next required step is:
1. Inspect LOTP format beyond the magic bytes (requires further evidence).
2. Implement a LOTP-format lotpack writer once the format is understood.

---

## 8. Non-claims

- No PZ assets were copied into the repo.
- No load test was performed.
- No playable export claim.
- LOTP format structure beyond the 8-byte prefix is not yet known.
- No full LOTP parser has been implemented.
- PLAYABLE_EXPORT_CLAIM_ALLOWED=false is binding.
