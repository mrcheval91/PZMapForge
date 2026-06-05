# MAP-6O: Clean Isolated Build 42 Candidate Retest Protocol

```text
Schema:           pzmapforge.map6o-retest-protocol.v0.1
Claim boundary:   candidate_preflight_only_not_load_tested
Candidate:        pzmapforge_build42_candidate_001
Candidate source: MAP-6L / MAP-6M
CLEAN_ISOLATED_RETEST_PROTOCOL_CREATED
HUMAN_ONLY_COPY_REQUIRED
LOAD_TEST_NOT_PERFORMED
WRITER_NOT_CHANGED
PLAYABLE_EXPORT_CLAIM_ALLOWED=false
```

---

## 1. Context

MAP-6N recorded LOAD_TEST_INCONCLUSIVE for the first manual test of the
MAP-6L/MAP-6M candidate. The root cause of the crash/return to menu could not
be confirmed because:

- Log evidence was mixed with stale `pzmapforge_manual_b42_001_maptest_a` traces.
- It was unclear whether other mods were enabled simultaneously.
- No complete fresh console.txt was captured from the test session.

MAP-6O defines a clean isolated retest procedure to resolve the INCONCLUSIVE
status with an unambiguous evidence record.

---

## 2. Why isolation matters

Without isolation, ambiguous outcomes arise from:

- **Other enabled mods**: conflicts can crash PZ before the candidate is reached.
- **Old test mod folder present**: PZ may load both old and new test folders,
  producing interleaved log traces that are difficult to attribute.
- **Stale console.txt**: reading an old log file from a previous session
  contaminates the evidence with prior run data.

The goal of MAP-6O is a test session where the candidate is the only unknown,
and the evidence is unambiguous.

---

## 3. Pre-clean (human actions only — not performed by any script)

All steps in this section must be performed manually by the operator. No
PZMapForge script will execute these actions.

### 3.1 Remove old test mod folders

Locate and remove the following folders from the PZ user mods directory
(`C:\Users\Palmacede\Zomboid\mods\`) before the test:

```text
HUMAN-ONLY: manually delete or move to a backup location —
  C:\Users\Palmacede\Zomboid\mods\pzmapforge_manual_b42_001_maptest_a\
  C:\Users\Palmacede\Zomboid\mods\pzmapforge_manual_b42_001_maptest_b\
  C:\Users\Palmacede\Zomboid\mods\pzmapforge_manual_b42_001_maptest_c\
  (any other pzmapforge_manual_* folders)
```

This removes the stale MAP-6B/MAP-6A traces that contaminated MAP-6N evidence.

### 3.2 Disable unrelated mods

For the duration of the test, disable all mods not required for the test.
A clean PZ install with only the candidate mod enabled is ideal.

Verify in PZ Mod Manager that no other map mods are active before launching.

### 3.3 Use a fresh or isolated server preset

Create a new server config named `PZMF_B42_CANDIDATE_CLEAN_001` (or equivalent)
with default settings. Do not reuse an existing world save that was created
with previous pzmapforge test mods.

### 3.4 Delete stale console.txt before the test

Before launching PZ for the retest:
```text
HUMAN-ONLY: delete or rename —
  C:\Users\Palmacede\Zomboid\console.txt
```

This ensures the post-test console.txt contains only evidence from the clean
retest session.

---

## 4. Install (human action only — not performed by any script)

Use the MAP-6M candidate output under `.local/` as the source. The operator
must manually copy the following file tree:

```text
SOURCE (under .local — exact path from MAP-6M packet):
  .local\...\pzmapforge_build42_candidate_001_build42_candidate\42\

DESTINATION (human-run copy only):
  C:\Users\Palmacede\Zomboid\mods\pzmapforge_build42_candidate_001_test_clean\42\
```

After the copy the following files must exist at the destination:

```text
42\mod.info
42\media\maps\pzmapforge_build42_candidate_001\map.info
42\media\maps\pzmapforge_build42_candidate_001\spawnpoints.lua
42\media\maps\pzmapforge_build42_candidate_001\objects.lua
42\media\maps\pzmapforge_build42_candidate_001\0_0.lotheader
42\media\maps\pzmapforge_build42_candidate_001\world_0_0.lotpack
42\media\maps\pzmapforge_build42_candidate_001\chunkdata_0_0.bin
```

A `spawnregions.lua` file from the MAP-6M packet should also be placed at:
```text
42\media\maps\pzmapforge_build42_candidate_001\spawnregions.lua
```

The `prepare-build42-candidate-load-test-packet.ps1` script (MAP-6M) generates
an `INSTALL_COPY_COMMANDS_README.txt` with exact path references under `.local/`.

### Preflight verify before launching PZ

Open PowerShell (do not use any PZMapForge script for this) and confirm:

```powershell
Test-Path 'C:\Users\Palmacede\Zomboid\mods\pzmapforge_build42_candidate_001_test_clean\42\mod.info'
Test-Path 'C:\Users\Palmacede\Zomboid\mods\pzmapforge_build42_candidate_001_test_clean\42\media\maps\pzmapforge_build42_candidate_001\0_0.lotheader'
Test-Path 'C:\Users\Palmacede\Zomboid\mods\pzmapforge_build42_candidate_001_test_clean\42\media\maps\pzmapforge_build42_candidate_001\world_0_0.lotpack'
Test-Path 'C:\Users\Palmacede\Zomboid\mods\pzmapforge_build42_candidate_001_test_clean\42\media\maps\pzmapforge_build42_candidate_001\chunkdata_0_0.bin'
```

All must return `True` before proceeding.

---

## 5. Test sequence (human actions only)

1. Launch Project Zomboid Build 42 fresh.
2. Navigate to Mods. Verify `pzmapforge_build42_candidate_001` appears in the list.
3. Enable the candidate mod. Disable all other mods if possible.
4. Record: **does PZ crash or return to menu at mod enable/selection?**
   - YES → record `MOD_SELECTION_CRASH`. Stop. Copy console.txt immediately.
   - NO → continue.
5. Navigate to Host (solo) or Start Server.
6. Select the clean server preset `PZMF_B42_CANDIDATE_CLEAN_001`.
7. Start game. Watch for spawn selection screen.
8. Record: **does the spawn region for the candidate appear?**
   - YES → record `SPAWN_REGION_VISIBLE`. Continue.
   - NO → record `SPAWN_REGION_NOT_VISIBLE`. Continue anyway.
9. Attempt to enter the world (any spawn region is acceptable for this test).
10. Record: **does world loading start?**
    - YES → record `WORLD_LOAD_STARTED`.
    - NO / crash → record first PZ error message visible.
11. Stop after first unrecoverable error or after successful world entry.

At every step: do not modify PZ files. Do not change the mod. Do not re-run
any PZMapForge writer.

---

## 6. Post-test log capture (human action only)

Immediately after the test session ends:

```text
HUMAN-ONLY: copy —
  C:\Users\Palmacede\Zomboid\console.txt
    -> .local\map6o-logs\console-map6o-YYYYMMDD-HHMMSS.txt
```

Copy the entire file to `.local/` before launching PZ again, as subsequent
launches overwrite or append to console.txt.

---

## 7. Log triage (PZMapForge tool)

After copying the fresh console.txt to `.local/`, run the MAP-6N triage tool:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\extract-map6n-current-candidate-log-evidence.ps1 `
    -InputLogFolder .local\map6o-logs `
    -Output .local\map6o-triage
```

This produces `map6n-log-triage-report.json` with:
- `current_candidate_matches` — lines referencing `pzmapforge_build42_candidate_001`
- `stale_maptest_a_matches` — excluded lines from old path
- `candidate_specific_exception_found` — true if IsoLot/Exception on a candidate line
- `result_recommendation` — `LOAD_TEST_INCONCLUSIVE` or `CURRENT_CANDIDATE_EXCEPTION_FOUND`

---

## 8. Required evidence fields for an unambiguous result

Fill in the MAP-6O record template (generated by
`scripts/prepare-map6o-clean-retest-checklist.ps1`) with:

| Field | Value |
|---|---|
| PZ version | (from log or title screen) |
| Build number | (42.x.x) |
| Other mods enabled | (list or "none") |
| Old test folders removed | yes / no |
| Fresh console.txt captured | yes / no |
| mod_selection_crash | yes / no |
| spawn_region_visible | yes / no |
| world_load_started | yes / no |
| candidate_specific_exception_found | yes / no (from triage tool) |
| result | LOAD_TEST_PASS / LOAD_TEST_FAIL / LOAD_TEST_INCONCLUSIVE |

A PASS requires:
- Old test folders removed
- Fresh console.txt captured
- Mod selection: no crash
- Spawn region: visible
- World load: started

A FAIL requires:
- Old test folders removed
- Fresh console.txt captured
- `candidate_specific_exception_found` = true in triage output

INCONCLUSIVE: any other outcome, including missing log or unattributable crash.

---

## 9. Status labels

```text
CLEAN_ISOLATED_RETEST_PROTOCOL_CREATED
  — This protocol document is committed and ready for operator use.

HUMAN_ONLY_COPY_REQUIRED
  — No PZMapForge script copies files to PZ folders.
  — All installs, deletions, and log captures are operator actions.

LOAD_TEST_NOT_PERFORMED
  — MAP-6O defines the protocol only.
  — No load test was executed as part of creating this document.

WRITER_NOT_CHANGED
  — The MAP-6L/MAP-6M candidate binary files were not modified.
  — MAP-6O targets the same candidate as MAP-6N.

PLAYABLE_EXPORT_CLAIM_ALLOWED=false
  — Binding until a LOAD_TEST_PASS record with the required evidence fields
    is committed.
```

---

## 10. Non-claims

- MAP-6O does not perform a load test.
- MAP-6O does not copy any files to PZ folders.
- MAP-6O does not modify the MAP-6L/MAP-6M candidate binary files.
- MAP-6O does not read or copy PZ assets.
- No playable Project Zomboid map was produced.
- No media/maps writes occurred in this repo.
