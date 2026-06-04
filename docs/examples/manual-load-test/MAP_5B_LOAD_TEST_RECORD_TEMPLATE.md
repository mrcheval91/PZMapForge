# MAP-5B Load Test Record

```text
Schema:           pzmapforge.load-test-record.v0.1
Claim boundary:   experimental_local_only_not_playable_not_load_tested
Status:           DRAFT — fill in after performing the load test
Instructions:     Save a filled copy to .local/load-tests/<run-name>/
                  Do not commit if it contains personal paths or binary excerpts.
```

---

## Test metadata

| Field | Value |
|---|---|
| Date / time | <!-- YYYY-MM-DD HH:MM --> |
| Tester | <!-- operator name or initials --> |
| PZ version | <!-- e.g. Build 41.78 or Build 42.x --> |
| PZ install path | <!-- e.g. D:\SteamApps\...\ProjectZomboid --> |
| MAP-5A source output | <!-- .local path of the experimental output --> |
| Copied-to path | <!-- C:\Users\<Name>\Zomboid\mods\<map_id>\ --> |
| map_id used | <!-- e.g. pzmapforge_test --> |
| Cell coordinates | <!-- e.g. (0, 0) --> |

---

## Safety confirmation

| Property | Value |
|---|---|
| Mod files copied from .local only | <!-- yes / no --> |
| PZ install directory modified | <!-- no (must be no) --> |
| Repo media/maps touched | <!-- no (must be no) --> |
| PZ assets copied into repo | <!-- no (must be no) --> |
| Playable export claimed before this test | <!-- no (must be no) --> |

---

## Observation checklist

| Observation | Result | Notes |
|---|---|---|
| Mod appears in PZ mod list | <!-- yes / no / unknown --> | |
| Map location appears in spawn or map selection | <!-- yes / no / unknown --> | |
| Game starts without crashing | <!-- yes / no --> | |
| PZ crashed during load | <!-- yes / no --> | |
| Experimental cell visible on in-game map | <!-- yes / no / unknown --> | |
| Player can spawn in the experimental cell | <!-- yes / no / unknown --> | |
| Cell appears blank (no tiles, no terrain) | <!-- yes / no / unknown --> | |
| Cell appears with default terrain | <!-- yes / no / unknown --> | |
| Error messages observed | <!-- yes / no --> | |

---

## Error messages and log excerpts

Paste relevant PZ log lines here (no binary content):

```
<!-- PZ log path: C:\Users\<Name>\Zomboid\Logs\ -->
<!-- Paste relevant lines only. Truncate long logs. -->
```

---

## Observed behavior (free text)

<!-- Describe what happened when the mod was loaded and the game was started. -->
<!-- Be specific about what was visible or absent. -->

---

## Screenshots

| Screenshot | Taken | Description |
|---|---|---|
| Mod list showing mod enabled | <!-- yes / no --> | |
| Spawn selection or world map | <!-- yes / no --> | |
| In-game view of experimental cell | <!-- yes / no --> | |
| Error screen or crash dialog | <!-- yes / no --> | |

Screenshots are local only. Do not commit screenshots to the repo.

---

## Failure diagnosis (if LOAD_TEST_FAIL or LOAD_TEST_INCONCLUSIVE)

| Symptom | Likely cause |
|---|---|
| PZ crash on load | lotpack zero-offset assumption wrong |
| Mod visible but no map location | map.info `lots` field not recognized |
| Map visible but no spawn point | spawnpoints.lua coordinate issue |
| Cell blank / missing tiles | lotheader 0-entry assumption wrong |
| Other | <!-- describe --> |

Identified likely failure cause: <!-- fill in -->

---

## Result

```text
RESULT: <!-- LOAD_TEST_PASS / LOAD_TEST_FAIL / LOAD_TEST_INCONCLUSIVE -->
```

| Property | Value |
|---|---|
| playable_claim_allowed | false — not until result reviewed and approved |
| load_tested | true (this record constitutes a load test) |
| load_test_result | <!-- PASS / FAIL / INCONCLUSIVE --> |
| pz_version_tested | <!-- fill in --> |
| build_tested | <!-- 41 / 42 / unknown --> |

---

## Notes and next steps

<!-- What should be investigated next based on this result? -->
<!-- If PASS: what does MAP-5C look like? -->
<!-- If FAIL: which gap needs another evidence probe? -->

---

## Non-claims

- Recording `LOAD_TEST_PASS` in this file does not constitute a public playable
  export claim. That requires operator review and explicit approval.
- This record covers one specific build/version. Results may differ on other builds.
- Build 41 vs Build 42 format differences are not addressed by MAP-5A.
