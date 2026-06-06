# MAP-6T: Build 42 LOTH v2 Load Test Packet

```text
Schema:           pzmapforge.map6t-loth-v2-load-test-packet.v0.1
Claim boundary:   candidate_preflight_only_not_load_tested
Candidate:        pzmapforge_build42_candidate_v1
Profile:          empty_grass_v1
MAP6T_LOTH_V2_LOAD_TEST_PACKET_CREATED
EMPTY_GRASS_V1_CANDIDATE_GENERATED
HUMAN_ONLY_COPY_REQUIRED
LOAD_TEST_NOT_PERFORMED
WRITER_NOT_CHANGED
PLAYABLE_EXPORT_CLAIM_ALLOWED=false
```

---

## 1. MAP-6S basis

MAP-6S added the `empty_grass_v1` profile to the Build 42 candidate writer.
The v1 LOTH lotheader generates 1024 contiguous grass overlay entries:
- Size: 28598 bytes (vs v0: 38 bytes)
- entry_count: 1024
- Entry range: blends_grassoverlays_01_0 ... blends_grassoverlays_01_1023
- Strategy: generated_contiguous_grass_overlay_range (not copied from any mod)
- Known risk: generated_entries_may_not_match_loaded_tile_definitions

MAP-6Q confirmed the v0 failure was at `IsoLot.readInt` on 0_0.lotheader.
MAP-6T prepares a controlled retest of the v1 candidate under the same wiring
that worked in MAP-6O/MAP-6P (spawn activation verified, server wiring verified).

---

## 2. What the packet prepares

`scripts/prepare-build42-loth-v2-load-test-packet.ps1` does the following:

1. Generates the `empty_grass_v1` candidate under `.local/` using the CLI.
2. Runs a 20-point preflight inspection on the generated candidate.
3. Writes operator-facing documents under `.local/`:
   - `MAP_6T_LOAD_TEST_PACKET.md` -- step-by-step test instructions
   - `MAP_6T_LOAD_TEST_RECORD.local-template.md` -- fillable result template
   - `MAP_6T_INSTALL_AND_SERVER_WIRING_COMMANDS.md` -- exact wiring commands
   - `map6t-preflight.json` -- machine-readable preflight results
   - `map6t-preflight.md` -- human-readable preflight summary

The script does NOT copy any files to PZ folders.

---

## 3. Candidate profile

```text
Profile:       empty_grass_v1
Map ID:        pzmapforge_build42_candidate_v1_001
Mod folder:    pzmapforge_build42_candidate_v1_001_test
Server preset: PZMF_B42_LOTH_V2_TEST_001
```

---

## 4. Expected diagnostic value

This test resolves one of the MAP-6S remaining unknowns: `loth_generated_entry_acceptance`.

| Observed outcome | Diagnostic conclusion | Next step |
|---|---|---|
| IsoLot.readInt EOF on 0_0.lotheader again | LOAD_TEST_FAIL_LOTH: v2 structure still wrong | Investigate LOTH format more deeply |
| No lotheader error; LOTP/lotpack error | LOAD_TEST_FAIL_LOTP: LOTH accepted; LOTP structure wrong | LOTP writer v2 (MAP-6U) |
| No lotheader or LOTP error; chunkdata error | LOAD_TEST_FAIL_CHUNKDATA: LOTH+LOTP accepted | Chunkdata writer v2 |
| Spawn region visible; world loads | LOAD_TEST_PASS: all binary files accepted | Record carefully; no public claim until reviewed |
| Crash before mod selection | LOAD_TEST_INCONCLUSIVE | Diagnose pre-load crash |

---

## 5. No load test, no PZ writes, no writer change

- MAP-6T does not perform a load test.
- The packet script does not copy any files to PZ folders.
- The candidate writer was not changed in MAP-6T.
- All PZ-targeting actions are human-only, documented in the packet.
- No media/maps writes occurred in this repo.
- PLAYABLE_EXPORT_CLAIM_ALLOWED=false is binding.

---

## 6. Non-claims

- No playable Project Zomboid map was produced.
- No binary writer was changed.
- No PZ assets were copied or read by PZMapForge scripts.
- No load test was performed.
