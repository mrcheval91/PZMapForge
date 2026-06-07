# MAP-7S: Private Workshop Upload Staging Packet

```text
MAP7S_WORKSHOP_STAGING_PACKET_CREATED
NO_AUTOMATIC_WORKSHOP_UPLOAD
STAGED_PACKAGE_LOCAL_ONLY
LOAD_TEST_NOT_PERFORMED
PUBLIC_PLAYABLE_CLAIM_ALLOWED=false
```

---

## 1. Summary

MAP-7S prepares the human-only private/unlisted Workshop upload staging packet
for the PZMapForge candidate. This is the next diagnostic step after MAP-7R
confirmed that borrowing Dru_map's WorkshopItems=3355966216 is insufficient.

The staging script generates the candidate package under `.local/` only.
Nothing is uploaded to Steam Workshop. No PZ load test is performed.
No binary writer behavior is changed.

---

## 2. What this task does

Adds:
- `scripts/prepare-build42-map7s-private-workshop-staging-packet.ps1`
  Generates the candidate Workshop package under `.local/` only.
  Uses the existing `empty_grass_v4` dotnet CLI candidate generator.
  Applies the Dru_map-aligned layout (root mod.info + 42/mod.info +
  NO common/mod.info + common/media/maps/<MapId>/).
  Writes all staging packet docs to `.local/`.

Staged package layout (under `.local/`):
```text
<CandidateMapId>/
  mod.info
  poster.png (placeholder)
  42/mod.info
  common/media/maps/<CandidateMapId>/
    map.info (lots=NONE, zoomX/Y/S)
    objects.lua
    spawnpoints.lua
    0_0.lotheader
    world_0_0.lotpack
    chunkdata_0_0.bin
    thumb.png
    worldmap.xml
    worldmap-forest.xml
    maps/biomemap_0_0.png
```

Packet docs (all under `.local/`):
- MAP_7S_PRIVATE_WORKSHOP_STAGING_PACKET.md
- MAP_7S_HUMAN_UPLOAD_CHECKLIST.md
- MAP_7S_SERVER_WIRING_AFTER_UPLOAD_TEMPLATE.md
- MAP_7S_LOG_CAPTURE_AFTER_UPLOAD.md
- MAP_7S_SUCCESS_FAILURE_CRITERIA.md
- MAP_7S_STAGED_PACKAGE_MANIFEST.md
- map7s-preflight.json
- map7s-preflight.md

---

## 3. What the human must do

After running the staging script:

1. Create a new private/unlisted Workshop item manually on Steam.
   Do NOT use Workshop ID 3355966216 (Dru_map's ID).
   Record the new PZMapForge Workshop ID.

2. Upload the staged package to the new Workshop item.

3. Wire the server:
   ```ini
   Mods=pzmapforge_build42_candidate_v4_001
   WorkshopItems=<PZMapForgeOwnWorkshopId>
   Map=pzmapforge_build42_candidate_v4_001;Muldraugh, KY
   ```

4. Capture logs and run the analyzer:
   ```powershell
   powershell -ExecutionPolicy Bypass `
       -File .\scripts\inspect-build42-map7d-load-result.ps1 `
       -LogPath <client log path> `
       -Output .\.local\map7s-packet\analysis-after-upload `
       -ExpectedMapId pzmapforge_build42_candidate_v4_001 `
       -VariantLabel VariantWSUpload
   ```

---

## 4. Success and failure conditions

### Success

`expected_map_lotheader_meta_evidence_found=true`
AND/OR: built custom PZMapForge world visible (not fallback forest).

If success: binary writer validation becomes the next investigation focus.

### Failure

Candidate still only loads as mod and fallback forest appears.
`expected_map_lotheader_meta_evidence_found=false`.

If failure: continue runtime mount/activation investigation.

---

## 5. Binary writer gate

```text
BINARY_WRITER_GATE_CLOSED_UNTIL:
  expected_map_lotheader_meta_evidence_found=true
  OR: explicit binary format failure on candidate lotheader

Do not mutate LOTH/LOTP/chunkdata before this gate is cleared.
No binary writer changes from this task.
```

---

## 6. Claim boundary

```text
MAP7S_WORKSHOP_STAGING_PACKET_CREATED
NO_AUTOMATIC_WORKSHOP_UPLOAD
STAGED_PACKAGE_LOCAL_ONLY
LOAD_TEST_NOT_PERFORMED
PUBLIC_PLAYABLE_CLAIM_ALLOWED=false
NO_BINARY_WRITER_CHANGES
```

No playable PZMapForge export is claimed from this task.
No Steam Workshop upload is performed by Claude.
All Workshop upload steps are HUMAN-ONLY.
