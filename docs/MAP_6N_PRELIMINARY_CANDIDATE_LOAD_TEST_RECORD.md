# MAP-6N: Preliminary Build 42 Candidate Load Test Record

```text
Schema:           pzmapforge.map6n-preliminary-load-test-record.v0.1
Claim boundary:   candidate_preflight_only_not_load_tested
Candidate:        pzmapforge_build42_candidate_001
Candidate source: MAP-6L / MAP-6M
Test status:      LOAD_TEST_INCONCLUSIVE
BUILD42_CANDIDATE_MOD_LOAD_LOGGED
MANUAL_TEST_ABORTED_OR_CRASHED_AT_MOD_SELECTION
CURRENT_CANDIDATE_ERROR_LOG_NOT_FOUND
STALE_MAPTEST_A_LOGS_EXCLUDED
LOAD_TEST_INCONCLUSIVE
PLAYABLE_EXPORT_CLAIM_ALLOWED=false
```

---

## 1. Context

MAP-6M produced a preflight-verified load-test packet for the MAP-6L candidate
(`pzmapforge_build42_candidate_001`). MAP-6N is the first manual load test attempt
using that packet.

The operator manually copied the MAP-6L/MAP-6M candidate into the local PZ mods
folder and attempted to enable the mod in Project Zomboid Build 42.

---

## 2. Operator observation

- Mod was copied to local PZ mods folder manually (outside this repo).
- Project Zomboid Build 42 was launched.
- When choosing/enabling the candidate mod, PZ crashed or returned to the menu.
- The operator extracted the console.txt log for analysis.

---

## 3. Current candidate evidence extracted from console.txt

The following line was found in the extracted log:

```text
LOG : Mod > loading pzmapforge_build42_candidate_001
```

Status: `BUILD42_CANDIDATE_MOD_LOAD_LOGGED`

Interpretation: PZ acknowledged and began loading the candidate mod by ID. The
mod was discovered and entered the load sequence.

---

## 4. Missing evidence for a PASS determination

The following were NOT found in the extracted current-candidate log lines:

| Expected for PASS | Found |
|---|---|
| LOTP / lotpack load trace with candidate ID | No |
| LOTH / lotheader load trace with candidate ID | No |
| IsoLot load trace with candidate ID | No |
| CellLoader load trace with candidate ID | No |
| Spawn screen confirmation | No |
| World loading confirmation | No |

Status: `CURRENT_CANDIDATE_ERROR_LOG_NOT_FOUND`

No current-candidate-specific stack trace was found in the extracted lines.
It is unknown whether the crash/return was caused by the candidate binary files
or by an unrelated loading step (e.g. map discovery, UI, server list, or
another mod).

---

## 5. Stale evidence warning

The operator noted IsoLot / EOFException / CellLoader stack traces in the raw
Zomboid log. These traces belong to the earlier `pzmapforge_manual_b42_001_maptest_a`
mod path (MAP-6B / MAP-6A sessions), NOT to the current candidate.

Stale mod ID in those traces: `pzmapforge_manual_b42_001_maptest_a`

Status: `STALE_MAPTEST_A_LOGS_EXCLUDED`

These stale traces MUST NOT be attributed to the MAP-6L/MAP-6M candidate. They
are evidence of the earlier binary format failure (MAP-6B) and are irrelevant to
the current candidate assessment.

---

## 6. Status labels

```text
BUILD42_CANDIDATE_MOD_LOAD_LOGGED
  — PZ began loading pzmapforge_build42_candidate_001 (log line found).

MANUAL_TEST_ABORTED_OR_CRASHED_AT_MOD_SELECTION
  — PZ crashed or returned to menu during or after mod enable/selection.
  — Root cause not confirmed from available log evidence.

CURRENT_CANDIDATE_ERROR_LOG_NOT_FOUND
  — No LOTP/LOTH/IsoLot/CellLoader trace referencing the current candidate
    was found in the extracted console.txt lines.

STALE_MAPTEST_A_LOGS_EXCLUDED
  — IsoLot/EOFException traces found belong to pzmapforge_manual_b42_001_maptest_a
    (MAP-6B path). They are excluded and do not count as MAP-6N failure evidence.

LOAD_TEST_INCONCLUSIVE
  — Mod was loaded by ID but no stack trace confirms whether the candidate
    binary files caused a failure. Cannot declare PASS or FAIL.

PLAYABLE_EXPORT_CLAIM_ALLOWED=false
  — Binding. No playable export claim may be made from this result.
```

---

## 7. What this result does NOT establish

- This result does NOT confirm the candidate binary files are accepted by PZ.
- This result does NOT confirm the candidate binary files caused a failure.
- This result does NOT constitute a PASS or a FAIL for MAP-6L/MAP-6M binary format.
- No playable export claim is permitted.

---

## 8. Root cause candidates for the crash/return (unverified)

In order of diagnostic priority:

1. **Mod discovery / ordering issue**: another installed mod conflicted with the
   candidate; PZ returned to menu during mod list resolution.
2. **spawnregions.lua missing or rejected**: MAP-6M packet included a template
   but the operator may not have placed a complete spawnregions.lua at the
   required path.
3. **Candidate binary format rejection**: LOTP/LOTH/chunkdata produced a PZ
   runtime error not captured in the extracted log lines.
4. **PZ version mismatch**: wrong build branch selected.

None of these can be determined from the current extracted evidence.

---

## 9. Next steps

To resolve the INCONCLUSIVE status:

1. **Log capture hardening**: use the triage script
   (`scripts/extract-map6n-current-candidate-log-evidence.ps1`) against a full
   fresh console.txt. Copy the full log under `.local/` before analysis.
2. **Isolated test**: remove all other mods, ensure only the candidate is
   enabled, re-run, capture complete console.txt.
3. **spawnregions.lua verification**: confirm the spawnregions.lua file placed
   in the mod is syntactically valid and references the correct coordinates.
4. **Log line inspection**: look specifically for candidate ID lines adjacent
   to LOTP/LOTH/IsoLot/CellLoader/Exception lines.

The INCONCLUSIVE status should be resolved by a follow-up load test with a full
fresh log capture before any further binary format changes are made.

---

## 10. Non-claims

- No load test was performed successfully (crash/return observed, root cause unknown).
- No playable Project Zomboid map was produced.
- No PZ assets were copied or read by PZMapForge.
- No media/maps writes occurred in this repo.
- No writer change was implemented as part of MAP-6N.
