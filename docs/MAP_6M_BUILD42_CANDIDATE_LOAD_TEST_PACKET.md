# MAP-6M: Build 42 Candidate Load Test Packet

```text
Schema:           pzmapforge.build42-candidate-preflight.v0.1
Claim boundary:   candidate_preflight_only_not_load_tested
PZ build:         Build 42
MAP-6L status:    BUILD42_CANDIDATE_WRITER_IMPLEMENTED
BUILD42_CANDIDATE_LOAD_TEST_PACKET_CREATED
CANDIDATE_PREFLIGHT_VERIFIED
MANUAL_LOAD_TEST_REQUIRED
LOAD_TEST_NOT_PERFORMED
PLAYABLE_EXPORT_CLAIM_ALLOWED=false
```

---

## 1. Context

MAP-6L implemented the first Build 42 candidate writer MVP producing LOTP, LOTH,
and chunkdata files under `.local/`. MAP-6M adds a packet preparation script that:
1. Validates the MAP-6L candidate output with a binary preflight.
2. Produces copy instructions for the operator.
3. Produces a fillable load-test record template.
4. Prepares a spawnregions.lua reference.

MAP-6M does NOT copy any files to PZ folders. MAP-6M does NOT run Project Zomboid.

---

## 2. Script

```
scripts/prepare-build42-candidate-load-test-packet.ps1
  -Source  ".local\...\<map_id>_build42_candidate"
  -Output  ".local\...\<packet_dir>"
  [-ServerName PZMF_B42_CANDIDATE_TEST_001]
  [-ModFolderName <name>]
```

Both `-Source` and `-Output` must be under `.local/`.

---

## 3. Preflight checks (31 checks on Drummondville smoke)

All 31 checks PASS for the MAP-6L `empty_grass_v0` candidate:

| Category | Checks |
|---|---|
| Report safety flags | 11 (schema, candidate_writer, profile, writer_implemented, scope, load_tested, playable_generated, playable_claimed, pz_assets_copied, pz_assets_read, media_maps_clean) |
| Required files | 8 (mod.info, report.json, map.info, spawnpoints.lua, objects.lua, lotheader, lotpack, chunkdata) |
| LOTH binary | 3 (magic=LOTH, version=1, entry_count>=1) |
| LOTP binary | 6 (magic=LOTP, version=1, chunk_count=1024, first_offset=8204, second_offset=9228, size=1056780) |
| Chunkdata binary | 3 (size=1026, header=0001, body_all_zero) |

---

## 4. Output files

| File | Purpose |
|---|---|
| `BUILD42_CANDIDATE_PREFLIGHT.json` | Machine-readable preflight report |
| `BUILD42_CANDIDATE_LOAD_TEST_PACKET.md` | Operator copy instructions |
| `BUILD42_CANDIDATE_LOAD_TEST_RECORD.local-template.md` | Fillable test record |
| `pzmapforge_candidate_spawnregions.lua` | SpawnRegions() Lua template |
| `INSTALL_COPY_COMMANDS_README.txt` | Reference copy commands |

---

## 5. Non-claims

- MAP-6M does not perform a load test.
- MAP-6M does not copy any files to PZ folders.
- MAP-6M does not read or copy PZ assets.
- The candidate remains unproven until a load test is recorded.
- PLAYABLE_EXPORT_CLAIM_ALLOWED=false is binding.

---

## 6. Recommended next task: MAP-6N

MAP-6N: Manual Build 42 candidate load test (operator action).

The operator uses the MAP-6M packet to:
1. Copy the MAP-6L candidate to the PZ mods directory.
2. Run Project Zomboid.
3. Record observations in the local template.
4. Return results for analysis.

No automated copy. No automated test. Human operator required.
