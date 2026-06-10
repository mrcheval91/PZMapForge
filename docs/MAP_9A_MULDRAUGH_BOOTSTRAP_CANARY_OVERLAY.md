# MAP-9A Muldraugh Bootstrap Canary Overlay

```text
Status: MAP-9A packet defined
Classification: MAP9A_MULDRAUGH_BOOTSTRAP_CANARY_OVERLAY_DEFINED
Muldraugh bootstrap required: true
Fresh world required: true
Canary required: true
Playable claim: not allowed
Next branch: map9a_human_runtime_test_pending
```

---

## Source basis

MAP-8Z runtime result (MAP8Z_RUNTIME_FALLBACK_MULDRAUGH_CONFIRMED) established:
- worldmap.xml.bin was installed but visible world was still Muldraugh/vanilla fallback.
- Removing Muldraugh from the Map line still shows vanilla fallback (no-Muldraugh strategy rejected).
- Build 42 coop/server silently bootstraps to vanilla world behavior.
- The correct next strategy: Muldraugh-last bootstrap + fresh world reset + unmistakable canary.

---

## Controlled server Map line

The server Map line for MAP-9A testing:

```text
Map=pzmapforge_build42_candidate_v4_001;PZMapForge;Muldraugh, KY
```

- pzmapforge_build42_candidate_v4_001 is the child map (selector metadata).
- PZMapForge is the parent map (cell binaries).
- Muldraugh, KY is the bootstrap/fallback at the bottom of the Map line.
- Muldraugh must remain as the last entry. Removing it is not a valid strategy.

---

## Fresh world requirement

Before the MAP-9A human runtime test:
1. Close PZ/server.
2. Stage the candidate Workshop payload.
3. Keep Muldraugh last in the Map line.
4. Park or delete the old generated save/db for the test server only.
5. Run a fresh server.

fresh_world_required=true

A stale save may preserve old vanilla world state even after the Workshop payload changes.
The fresh world requirement exists to eliminate saved-state interference.

---

## Canary requirement

The test requires an unmistakable PZMapForge-owned visual canary cell.

Rationale:
- Vanilla Muldraugh has flat terrain, roads, and buildings.
- A canary cell must be visually impossible to confuse with anything in vanilla Muldraugh.
- Examples: all-asphalt cell, all-water cell, giant high-contrast road/cross pattern,
  completely cleared square with a unique PZMapForge marker pattern.

canary_required=true

---

## Canary writer state

canary_writer_available=false
canary_writer_blocked=true

Reason:
The current Build 42 cell writer (MapExportBuild42CandidateWriterV2, profile empty_grass_v2)
produces only empty grass content. Empty grass is visually indistinguishable from vanilla
Muldraugh's flat fallback terrain. Additionally, the IGMB cell index model is not confirmed:
the worldmap.xml.bin writer (MAP-8Y) produces a synthetic payload but the engine has not been
observed to mount PZMapForge content from it. Without a confirmed cell index and a writer
that can generate visually distinctive content, an unmistakable canary cannot be generated.

canary_writer_blocked_reason=current_cell_writer_produces_empty_grass_only_not_visually_distinguishable_from_muldraugh_fallback_and_igmb_cell_index_model_not_confirmed

Do not fake a canary. No visual-success claim is allowed until a real canary is observed.

---

## Expected human action sequence

1. Close PZ/server.
2. Stage the candidate Workshop payload (PZMapForge-owned files only).
3. Keep Map line: Map=pzmapforge_build42_candidate_v4_001;PZMapForge;Muldraugh, KY
4. Park or delete the old generated save/db for the test server only.
5. Run a fresh server.
6. Look for an unmistakable PZMapForge canary cell (will not appear until canary writer unblocked).
7. Capture logs: IsoMetaGrid map folder list, lotheader parse attempts.

---

## Success and failure signals

success_signal=visible_unmistakable_canary_cell_and_logs_support_PZMapForge_mount
failure_signal=vanilla_muldraugh_or_isometagrid_does_not_list_PZMapForge

---

## Classification labels

```text
MAP9A_MULDRAUGH_BOOTSTRAP_CANARY_OVERLAY_DEFINED
MAP8Z_RESULT_RECORDED=true
NO_MULDRAUGH_STRATEGY_REJECTED=true
MULDRAUGH_BOOTSTRAP_REQUIRED=true
FRESH_WORLD_REQUIRED=true
CANARY_REQUIRED=true
CANARY_WRITER_AVAILABLE=false
CANARY_WRITER_BLOCKED=true
STAGED_OUTPUT_LOCAL_ONLY=true
STEAM_WRITE_PERFORMED=false
WORKSHOP_UPLOAD_PERFORMED=false
PZ_RUN_PERFORMED=false
THIRD_PARTY_FILES_COPIED=false
PUBLIC_PLAYABLE_CLAIM_ALLOWED=false
```

---

## Next branch

next_branch=map9a_human_runtime_test_pending

The canary writer must be unblocked before a meaningful runtime test can yield a positive signal.
Unblocking requires: a Build 42 cell writer capable of producing visually distinctive content,
and a confirmed IGMB cell index model that correctly references PZMapForge cells.
