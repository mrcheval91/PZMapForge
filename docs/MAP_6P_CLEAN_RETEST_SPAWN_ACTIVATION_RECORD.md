# MAP-6P: Clean Retest and Spawn Activation Gap Record

```text
Schema:           pzmapforge.map6p-clean-retest-record.v0.1
Claim boundary:   candidate_preflight_only_not_load_tested
Candidate:        pzmapforge_build42_candidate_001
Candidate source: MAP-6L / MAP-6M
Retest session:   MAP-6O clean isolated retest
BUILD42_CANDIDATE_MOD_LOAD_CONFIRMED
VANILLA_WORLD_ENTRY_WITH_CANDIDATE_ENABLED
CANDIDATE_SPAWN_REGION_NOT_VISIBLE
CANDIDATE_MAP_CELL_NOT_PROVEN_LOADED
LOAD_TEST_INCONCLUSIVE
WRITER_NOT_CHANGED
PLAYABLE_EXPORT_CLAIM_ALLOWED=false
```

---

## 1. Context

MAP-6O defined a clean isolated retest protocol. MAP-6P records the outcome
of running that protocol.

The MAP-6N session was LOAD_TEST_INCONCLUSIVE due to mixed log evidence and an
unknown crash. MAP-6O resolved that ambiguity with a clean session: old
maptest_a folders removed, other mods disabled, fresh server preset, fresh
console.txt captured.

---

## 2. MAP-6O clean retest observations

| Field | Value |
|---|---|
| Candidate mod | pzmapforge_build42_candidate_001 |
| Mod folder | pzmapforge_build42_candidate_001_test_clean |
| Server preset | PZMF_B42_CANDIDATE_CLEAN_001 |
| PZ crashed at mod selection | No |
| PZ reached spawn selection screen | Yes |
| Spawn screen showed vanilla cities | Yes |
| PZMapForge Candidate Cell visible in spawn screen | No |
| Vanilla world entry succeeded with candidate enabled | Yes |

---

## 3. Triage evidence

From `extract-map6n-current-candidate-log-evidence.ps1` run on the fresh
console.txt from the MAP-6O session:

| Field | Value |
|---|---|
| current_candidate_matches | 2 |
| stale_maptest_a_matches | 0 |
| candidate_specific_exception_found | false |
| result_recommendation | LOAD_TEST_INCONCLUSIVE |

Interpretation: PZ loaded and referenced the candidate mod ID at least twice
(e.g. mod registration and map discovery lines). No exception was thrown that
referenced the candidate ID. No stale maptest_a contamination.

---

## 4. What this establishes

- `BUILD42_CANDIDATE_MOD_LOAD_CONFIRMED`: PZ recognized the candidate mod and
  processed it without a candidate-specific crash. The versioned 42/ layout is
  accepted by PZ's mod loader.

- `VANILLA_WORLD_ENTRY_WITH_CANDIDATE_ENABLED`: a vanilla world could be entered
  while the candidate mod was active. The candidate mod did not break PZ's
  existing map and spawn infrastructure.

- `CANDIDATE_SPAWN_REGION_NOT_VISIBLE`: the spawn selection screen did not show
  a PZMapForge Candidate Cell option. This means either the spawnregions.lua is
  not correctly wired, the server preset does not include the candidate map, or
  the map activation (Map= line) is missing from the server config.

- `CANDIDATE_MAP_CELL_NOT_PROVEN_LOADED`: because the spawn screen did not show
  the candidate region, the binary files (LOTH/LOTP/chunkdata) were never
  exercised during this session. Their acceptance or rejection remains unproven.

---

## 5. Root cause candidates for CANDIDATE_SPAWN_REGION_NOT_VISIBLE

In order of diagnostic priority:

1. **Map= line missing from server config**: the server preset ini file may not
   list `pzmapforge_build42_candidate_001` in its `Map=` field. PZ requires the
   map to be explicitly activated in the server settings to make its spawn
   regions available.

2. **spawnregions.lua missing or incorrect**: the MAP-6M packet provides a
   `pzmapforge_candidate_spawnregions.lua` template. It must be renamed to
   `spawnregions.lua` and placed at the correct path inside the mod. If it is
   absent or has the wrong format, the spawn region will not appear.

3. **Server spawnregions.lua not referencing the candidate map**: the server
   itself has a spawnregions file at
   `C:\Users\Palmacede\Zomboid\Server\<ServerName>_spawnregions.lua`. If this
   file does not reference the candidate map path, PZ will not display the
   candidate spawn region.

4. **WorkshopItems= mismatch**: for non-Workshop loose mods, the server may
   require specific configuration. Mods= and WorkshopItems= may both need
   to reference the candidate.

---

## 6. Status labels

```text
BUILD42_CANDIDATE_MOD_LOAD_CONFIRMED
  -- PZ loaded pzmapforge_build42_candidate_001 without a candidate-specific
     exception. current_candidate_matches=2 in fresh triage.

VANILLA_WORLD_ENTRY_WITH_CANDIDATE_ENABLED
  -- A vanilla PZ world was entered successfully with the candidate mod active.
     The candidate mod does not crash or break vanilla world loading.

CANDIDATE_SPAWN_REGION_NOT_VISIBLE
  -- The spawn selection screen showed only vanilla cities.
     The candidate map/spawn region was not activated.

CANDIDATE_MAP_CELL_NOT_PROVEN_LOADED
  -- Binary files (LOTH lotheader, LOTP lotpack, chunkdata) were not exercised
     because the spawn region was not visible. Their acceptance is unproven.

LOAD_TEST_INCONCLUSIVE
  -- Mod loads without crashing but the candidate map is not activated.
     Cannot declare PASS or FAIL for the binary format.

WRITER_NOT_CHANGED
  -- The MAP-6L/MAP-6M candidate binary files were not modified.
     MAP-6P is a diagnostic record only.

PLAYABLE_EXPORT_CLAIM_ALLOWED=false
  -- Binding. No playable export claim permitted.
```

---

## 7. Next required step

Diagnose and fix the spawn activation gap:

- Verify server preset `Map=` line includes `pzmapforge_build42_candidate_001`.
- Verify `spawnregions.lua` is present at the correct mod path.
- Verify server `_spawnregions.lua` references the candidate map.

Use `scripts/prepare-map6p-spawn-activation-diagnostic.ps1` to generate
operator-facing inspection commands and a fillable record template.
After the activation gap is resolved, re-run the clean retest protocol
(MAP-6O) and record a new result.

---

## 8. Non-claims

- No load test was performed as part of MAP-6P.
- No playable Project Zomboid map was produced.
- No binary writer was changed.
- No PZ assets were copied or read by PZMapForge.
- No media/maps writes occurred in this repo.
