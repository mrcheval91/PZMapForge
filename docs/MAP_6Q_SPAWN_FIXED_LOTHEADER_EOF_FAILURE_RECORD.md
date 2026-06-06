# MAP-6Q: Spawn Activation Fixed -- Candidate Lotheader EOF Failure Record

```text
Schema:           pzmapforge.map6q-lotheader-failure-record.v0.1
Claim boundary:   candidate_preflight_only_not_load_tested
Candidate:        pzmapforge_build42_candidate_001
SPAWN_ACTIVATION_WIRING_FIXED
CANDIDATE_MAP_FILES_EXERCISED
CURRENT_CANDIDATE_LOTHEADER_EOF
LOTHEADER_STRUCTURE_REJECTED
LOTP_NOT_REACHED
CHUNKDATA_NOT_REACHED
LOAD_TEST_FAIL_CURRENT_CANDIDATE
WRITER_NOT_CHANGED
PLAYABLE_EXPORT_CLAIM_ALLOWED=false
```

---

## 1. Context

MAP-6P established LOAD_TEST_INCONCLUSIVE because the candidate spawn region
was not visible. The root cause was identified as missing spawn activation wiring
(Map= line, spawnregions.lua). MAP-6Q records the outcome after those gaps were
manually fixed by the operator.

---

## 2. Spawn activation wiring fixed (operator actions)

The operator manually applied the following fixes outside this repo:

| Fix | Status |
|---|---|
| spawnregions.lua placed in mod map folder | yes |
| Server ini Mods= includes candidate | yes |
| Server ini Map= includes pzmapforge_build42_candidate_001 | yes |
| Server _spawnregions.lua references candidate spawnpoints.lua | yes |

All four gaps from the MAP-6P diagnostic protocol were resolved before the retest.

---

## 3. Retest outcome after wiring fix

After applying the spawn activation wiring, the retest produced:

- Error 3 crash before city choice screen.
- PZ triage result: `CURRENT_CANDIDATE_EXCEPTION_FOUND`.

Fresh triage from the new console.txt:

| Triage field | Value |
|---|---|
| current_candidate_matches | 3 |
| stale_maptest_a_matches | 0 |
| candidate_specific_exception_found | true |
| result_recommendation | CURRENT_CANDIDATE_EXCEPTION_FOUND |

---

## 4. Exact failure evidence

PZ attempted to load the candidate lotheader and failed:

```text
ERROR loading:
  C:\Users\Palmacede\Zomboid\mods\pzmapforge_build42_candidate_001_test_clean\42\
    media\maps\pzmapforge_build42_candidate_001\0_0.lotheader

Exception:
  java.io.EOFException at IsoLot.readInt(IsoLot.java:75)

Stack:
  zombie.iso.IsoLot.readInt(IsoLot.java:75)
  zombie.iso.IsoMetaGrid$MetaGridLoaderThread.loadCell(IsoMetaGrid.java:...)
```

This is the same EOFException class seen in MAP-6B, now confirmed against the
MAP-6L/MAP-6M LOTH candidate (not the old 8-byte placeholder from MAP-5A).

---

## 5. Interpretation

### What this establishes

- `SPAWN_ACTIVATION_WIRING_FIXED`: Map= and spawnregions.lua wiring is correct.
  The candidate map was discovered and PZ attempted to load its cell files.

- `CANDIDATE_MAP_FILES_EXERCISED`: PZ reached the candidate lotheader file path
  and attempted a read. This is the first time the MAP-6L/MAP-6M binary files
  were actually exercised by PZ.

- `CURRENT_CANDIDATE_LOTHEADER_EOF`: The LOTH lotheader produced by the MAP-6L
  candidate writer caused an `IsoLot.readInt` EOF. The current LOTH structure
  is insufficient for PZ Build 42.

### What remains unproven

- `LOTP_NOT_REACHED`: PZ stopped at lotheader. The MAP-6L LOTP lotpack was not
  read. Its acceptance or rejection is unknown.

- `CHUNKDATA_NOT_REACHED`: PZ stopped at lotheader. The MAP-6L chunkdata was not
  read. Its acceptance or rejection is unknown.

### Root cause of lotheader rejection

The MAP-6L LOTH lotheader (38 bytes) contains:
- LOTH magic (4 bytes)
- version=1 (4 bytes)
- entry_count=1 (4 bytes)
- one ASCII tileset entry + newline (26 bytes total for blends_grassoverlays_01_0)

The IsoLot.readInt at line 75 indicates PZ is attempting to read an integer
that does not exist at the current byte position. Possible causes:
1. The LOTH entry_count field is at offset 8 (bytes 8-11), but PZ may expect
   a different field layout before the entry table.
2. The entry format may require additional length/offset fields before each entry.
3. PZ may expect the LOTH file to be larger (more entries or a header extension).

The smallest fix is to inspect the exact byte layout PZ expects at IsoLot.readInt
line 75, by comparing candidate bytes against reference Build 42 LOTH files.
Use `scripts/compare-build42-lotheader-candidate.ps1` for this comparison.

---

## 6. Status labels

```text
SPAWN_ACTIVATION_WIRING_FIXED
  -- All four MAP-6P gaps resolved: spawnregions.lua placed, Mods=/Map=
     updated, server _spawnregions.lua references candidate.

CANDIDATE_MAP_FILES_EXERCISED
  -- PZ reached and attempted to read 0_0.lotheader from the candidate.
     Binary files are now being exercised.

CURRENT_CANDIDATE_LOTHEADER_EOF
  -- java.io.EOFException at IsoLot.readInt when reading the LOTH candidate.
     The MAP-6L LOTH structure is rejected by PZ Build 42.

LOTHEADER_STRUCTURE_REJECTED
  -- The current 38-byte LOTH candidate is not a valid Build 42 lotheader.
     The writer must be revised before a PASS result is possible.

LOTP_NOT_REACHED
  -- Load stopped before the LOTP lotpack was read.
     LOTP acceptance is unproven.

CHUNKDATA_NOT_REACHED
  -- Load stopped before chunkdata was read.
     Chunkdata acceptance is unproven.

LOAD_TEST_FAIL_CURRENT_CANDIDATE
  -- The candidate produced a confirmed exception.
     This is the first confirmed FAIL result for MAP-6L binary files.

WRITER_NOT_CHANGED
  -- No changes to the MAP-6L candidate writer were made in MAP-6Q.
     The writer revision is the next required step.

PLAYABLE_EXPORT_CLAIM_ALLOWED=false
  -- Binding. No playable export claim permitted until a full PASS
     record is committed.
```

---

## 7. Next required step

The lotheader writer must be revised. Before writing, compare the candidate
LOTH bytes against reference Build 42 LOTH files to identify the missing
fields PZ expects at IsoLot.readInt line 75.

Use:
```text
scripts/compare-build42-lotheader-candidate.ps1
  -CandidateLotheader <.local path to 0_0.lotheader>
  -ReferenceRoot      <.local path containing reference *.lotheader files>
  -Output             <.local output dir>
```

The comparison identifies:
- whether the candidate is shorter than all references
- the field layout difference at the first divergence point
- the stable word pattern in reference files vs candidate

After the comparison, revise the MAP-6L writer (MAP-6R or next slice) to
produce a LOTH that passes IsoLot.readInt.

---

## 8. Non-claims

- No load test was performed as part of MAP-6Q record creation.
- No binary writer was changed.
- No PZ assets were copied or read by PZMapForge scripts.
- No media/maps writes occurred in this repo.
- No playable export claim.
