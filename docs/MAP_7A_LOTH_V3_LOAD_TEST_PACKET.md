# MAP-7A: Build 42 LOTH v3 Load Test Packet

```text
Schema:           pzmapforge.map7a-loth-v3-load-test-packet.v0.1
Claim boundary:   build42_candidate_only_not_load_tested_not_playable
MAP7A_LOTH_V3_LOAD_TEST_PACKET_CREATED
EMPTY_GRASS_V2_CANDIDATE_GENERATED
HUMAN_ONLY_COPY_REQUIRED
LOAD_TEST_NOT_PERFORMED
WRITER_NOT_CHANGED
PLAYABLE_EXPORT_CLAIM_ALLOWED=false
```

---

## 1. MAP-6Z basis

MAP-6Z implemented the `empty_grass_v2` candidate profile:
- Same 1024 generated entries as v1.
- MAP-6Y canonical 1048-byte stable trailer appended.
- Total LOTH size: 29646 bytes.
- Trailer SHA-256: 93a8f3ccf2cafdc2fb7cd4f3836c29d87076f244f5ba685f92659fbdaf778ec7

MAP-6T confirmed that empty_grass_v1 (28598 bytes, no trailer) fails with
`IsoLot.readInt EOFException`. The v3 candidate directly addresses this by
appending the stable trailer observed in all 80 sampled Dru_map simple cells.

---

## 2. What the packet prepares

`scripts/prepare-build42-loth-v3-load-test-packet.ps1` performs:
1. Generates a fresh `empty_grass_v2` candidate via CLI under `.local/`.
2. Runs 24-point preflight verifying all binary properties.
3. Writes human-only install/wiring instructions.
4. Writes a fillable load-test record template.
5. Does NOT copy any files to PZ folders.
6. Does NOT change the writer.

---

## 3. Candidate profile: empty_grass_v2

| Field | Value |
|---|---|
| Profile | empty_grass_v2 |
| Entry count | 1024 |
| First entry | blends_grassoverlays_01_0 |
| Last entry | blends_grassoverlays_01_1023 |
| LOTH size | 29646 bytes |
| LOTH trailer size | 1048 bytes |
| LOTH trailer SHA-256 | 93a8f3ccf2cafdc2fb7cd4f3836c29d87076f244f5ba685f92659fbdaf778ec7 |
| LOTH trailer source | MAP-6Y reference research (80 Dru_map simple cells, all identical) |
| LOTP size | 1056780 bytes (unchanged from MAP-6S/MAP-6Z) |
| chunkdata size | 1026 bytes (unchanged) |

---

## 4. LOTP and chunkdata: unchanged

The LOTP (lotpack) and chunkdata formats are unchanged from MAP-6L/MAP-6S:
- LOTP: LOTP magic + version=1 + 1024 chunks x 1024 zero bytes = 1056780 bytes.
- chunkdata: 00 01 header + 1024 zero bytes = 1026 bytes.
- Both remain unproven at load time.

---

## 5. objects.lua secondary parse issue

The objects.lua secondary parse error (`LuaManager.RunLuaInternal`) observed in
MAP-6B remains pending. `return {}` is the current candidate (MAP-6C).

If the LOTH EOF is cleared by the v3 trailer, objects.lua is the next expected blocker.

---

## 6. Diagnostic value table

| If PZ reports... | Classification | Next task |
|---|---|---|
| lotheader EOF again | LOAD_TEST_FAIL_LOTH | MAP-7B: deepen LOTH analysis |
| lotpack/LOTP error | LOAD_TEST_FAIL_LOTP | MAP-7B: LOTP payload format research |
| chunkdata error | LOAD_TEST_FAIL_CHUNKDATA | MAP-7B: chunkdata format research |
| objects.lua error | LOAD_TEST_FAIL_OBJECTS_LUA | MAP-7B: objects.lua fix |
| World enters | LOAD_TEST_PASS | Record carefully; no public claim until reviewed |
| No log / crash | LOAD_TEST_INCONCLUSIVE | Repeat with clean environment |

---

## 7. Non-claims

- `LOAD_TEST_NOT_PERFORMED`: MAP-7A is packet preparation only.
- `WRITER_NOT_CHANGED`: no writer change in MAP-7A.
- `HUMAN_ONLY_COPY_REQUIRED`: all PZ folder operations are human-only.
- `PLAYABLE_EXPORT_CLAIM_ALLOWED=false`: binding.
- No PZ assets copied or read into the repo.
- No repo media/maps writes.
- Candidate only. No PZ compatibility claim.
