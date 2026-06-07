# MAP-7R: Variant J Borrowed WorkshopItems Trigger Failure

```text
MAP7R_VARIANT_J_WORKSHOP_TRIGGER_INSUFFICIENT
BORROWED_WORKSHOPITEMS_TRIGGER_EXHAUSTED
STATIC_VARIANTS_ABCDEFGHI_EXHAUSTED
NO_MORE_STATIC_LAYOUT_TESTS
LOAD_TEST_NOT_PERFORMED
PUBLIC_PLAYABLE_CLAIM_ALLOWED=false
```

---

## 1. Summary

MAP-7R records Variant J, which tested whether adding a known-working
WorkshopItems= value (Dru_map's Workshop ID: 3355966216) while keeping
the PZMapForge candidate as a local loose mod would trigger the same
runtime mount path that allowed Dru_map to succeed.

It did not.

WorkshopItems presence alone does not mount arbitrary local loose mods
as custom maps. The Workshop ID registered Dru_map's content, not the
PZMapForge candidate's content.

---

## 2. Variant J wiring and result

### 2.1 Server wiring

```text
Server:        PZMF_B42_CANDIDATE_WS_TRIGGER_J_001
Candidate:     pzmapforge_build42_candidate_v4_001
Mods:          Mods=pzmapforge_build42_candidate_v4_001
WorkshopItems: WorkshopItems=3355966216
Map:           Map=pzmapforge_build42_candidate_v4_001;Muldraugh, KY
Public:        false
```

### 2.2 Human visual result

- Forest/fallback world appeared.
- No built PZMapForge world.
- No evidence of custom map registration for pzmapforge_build42_candidate_v4_001.
- Not a successful custom map load.

### 2.3 Analyzer result

```text
classification:           MAP7R_VARIANT_J_WORKSHOP_TRIGGER_INSUFFICIENT
candidate_loaded:         true
player_data_received:     true
game_loading_completed:   true
map_folders_list_empty:   true
spawn_building_warning:   true
public_playable_claim_allowed=false
```

### 2.4 Key log evidence

- Server/client saw WorkshopItemsCount 1.
- Workshop ID 3355966216 reached Subscribed / Installed / Ready.
- PZMapForge local candidate loaded: `loading pzmapforge_build42_candidate_v4_001`.
- IsoMetaGrid still printed:
  ```
  Looking in these map folders:
  <End of map-folders list>
  ```
- Player data received. Game Mode: Multiplayer.
- Generic CellLoader / lotheader activity appeared but was NOT
  PZMapForge-specific. No log evidence that
  pzmapforge_build42_candidate_v4_001 reached expected-map lotheader
  or meta loading.
- Muldraugh mannequin-zone warning appeared: consistent with
  fallback/builtin map path.

---

## 3. Correct interpretation

### 3.1 What Variant J proved

Workshop ID 3355966216 is bound to Dru_map's Steam Workshop content.
Adding it to the server ini registers Dru_map's runtime mount path, not
the PZMapForge candidate's path. The WorkshopItems line is not a generic
runtime mount trigger for arbitrary local mods.

The PZMapForge candidate still only loads at the mod registration level.
It does not reach IsoMetaGrid map folder registration. It does not reach
expected-map lotheader/meta processing.

### 3.2 Why the empty map scan is still failure evidence here

In MAP-7Q we established that an empty client map-folder scan is NOT
decisive by itself for Build 42 coop/server with Workshop-activated mods.
However, the empty scan becomes meaningful failure evidence when combined
with the absence of any other success signal:

- No PZMapForge-specific lotheader/meta evidence in log.
- No built custom world visible.
- No expected-map paths referenced in lotheader loading sequence.

Dru_map succeeded with an empty scan because it had Workshop Installed/Ready
AND specific Dru_map lotheader evidence AND a visually built custom world.

PZMapForge Variant J had Workshop Installed/Ready (for Dru_map's ID)
AND candidate mod loaded — but the lotheader/meta evidence that followed
was Dru_map's, not PZMapForge's, and the world was fallback forest.

The empty scan + no candidate-specific lotheader evidence + fallback world
together confirm this is a runtime activation failure, not a success.

### 3.3 What is now exhausted

```text
Static layout variants:     A through I (EXHAUSTED)
Borrowed Workshop trigger:  J (EXHAUSTED)
```

No further static folder layout changes or borrowed Workshop IDs should
be attempted. The analysis is pointing clearly to a single root cause:
the PZMapForge candidate needs its OWN Workshop ID and real Workshop
activation, not borrowed credentials.

---

## 4. Next investigation: real candidate Workshop-style activation

The next meaningful diagnostic branch is a real candidate Workshop-style
activation, requiring a human-approved private/unlisted Workshop upload.

That is a distinct future task, not an automatic step from this task.

The future task (if operator approves):
- Stage the PZMapForge candidate as a Workshop package.
- Prepare a human-only upload checklist and instructions.
- After upload, wire server:
  ```
  Mods=pzmapforge_build42_candidate_v4_001
  WorkshopItems=<PZMapForgeWorkshopId>
  Map=pzmapforge_build42_candidate_v4_001;Muldraugh, KY
  ```
- Success condition: expected-map lotheader/meta evidence appears in log.
- Failure condition: still fallback forest and empty-equivalent scan.

### 4.1 Evidence gate for binary writer work

Until PZMapForge candidate produces expected-map lotheader/meta evidence
(log references pzmapforge_build42_candidate_v4_001 lotheader files),
the binary writer quality (LOTH/LOTP/chunkdata format) is NOT the
active blocker.

Do not mutate LOTH/LOTP/chunkdata before the evidence gate is cleared.

---

## 5. Claim boundary

```text
PUBLIC_PLAYABLE_CLAIM_ALLOWED=false
LOAD_TEST_NOT_PERFORMED
NO_BINARY_WRITER_CHANGE
NO_STEAM_WORKSHOP_UPLOAD
NO_FORBIDDEN_PATH_WRITES
```

No playable PZMapForge export is claimed from this task.
No Steam Workshop upload occurred or is authorized by this task.
