# MAP-5C: Build 42 Mod Packaging Discovery

```text
Schema:           pzmapforge.packaging-discovery.v0.1
Claim boundary:   packaging_diagnostic_only_not_load_tested
MAP-5B result:    LOAD_TEST_INCONCLUSIVE
Binary test:      NOT REACHED in MAP-5B — see MAP-6A for versioned-route discovery
PZ build:         Build 42
MAP-6A update:    Versioned loose-mod layout confirmed: <mods>/<folder>/42/mod.info
```

---

## 1. MAP-5B result record

**Result: LOAD_TEST_INCONCLUSIVE**

The MAP-5B manual load test did not reach the binary file evaluation stage.
The generated `.lotheader`, `.lotpack`, and `chunkdata_*.bin` files were not
proven to pass or fail. The blocker was mod discovery/packaging, not binary
format correctness.

### Attempted paths

| Path | Result |
|---|---|
| `C:\Users\Palmacede\Zomboid\mods\pzmapforge_empty_cell_test\` | Mod did not appear in Build 42 Mods screen |
| `C:\Users\Palmacede\Zomboid\Workshop\PZMapForgeEmptyCellTest\Contents\mods\pzmapforge_empty_cell_test\` | Mod did not appear in Build 42 Mods screen |
| `D:\Program Files (x86)\Steam\steamapps\common\ProjectZomboid\Workshop\PZMapForgeEmptyCellTest\Contents\mods\pzmapforge_empty_cell_test\` | Workshop upload flow complained; no successful enable reached |

### Observed behavior

- Loose local mod folders (flat structure under `Zomboid\mods\`) did not expose
  the experimental mod in the Build 42 Mods screen.
- The Workshop item directory chooser showed the Workshop folder but the mod was
  not successfully enabled.
- No game was launched. No load or crash test was reached.

### Non-claims (MAP-5B)

- The generated binary map files (`.lotheader`, `.lotpack`, `chunkdata_*.bin`)
  were **not proven to fail**. They were not loaded.
- MAP-5B is INCONCLUSIVE, not FAIL.
- The binary file hypotheses from MAP-5A remain untested.

---

## 2. Build 42 mod discovery — what was learned

### Working reference: `avisibleprobe`

A separate probe (`C:\Users\Palmacede\Zomboid\mods\avisibleprobe\`) **does** appear
in the Build 42 Mods screen. Its structure:

```text
avisibleprobe\
  mod.info          (name, id, description, category, modversion)
  media\lua\client\avisibleprobe.lua
```

This confirms flat loose-mod layout can work in Build 42. The difference is
`mod.info` field content.

### MAP-5A `mod.info` vs `avisibleprobe` `mod.info`

| Field | avisibleprobe (visible) | MAP-5A (not visible) |
|---|---|---|
| `name` | `AAA Visible Probe` | `PZMapForge Experimental - ...` |
| `id` | `avisibleprobe` | `pzmapforge_empty_cell_test` |
| `description` | short, clean | long with special chars (`--`, `!`) |
| `category` | `utility` | **MISSING** |
| `modversion` | `1.0` | **MISSING** |
| `poster` | (not present) | `poster=` (empty value) |

**Candidate cause:** MAP-5A `mod.info` is missing `category` and `modversion`
fields, and has an empty `poster=` line. One or more of these may prevent Build 42
from recognising the mod entry.

### ModTemplate package structure (Build 42 Workshop standard)

The PZ ModTemplate at `<PZ install>/Workshop/ModTemplate/` defines the expected
Workshop package layout for Build 42:

```text
<package_name>/
  workshop.txt          (version, title, description, tags, visibility)
  preview.png           (required for Workshop upload)
  Contents/
    mods/
      <mod_id>/
        mod.info        (name, id, description, poster)
        poster.png
        media/
          maps/
            <world_name>/
              map.info  (title, description — NO lots field in template)
              spawnpoints.lua
              thumb.png
```

**Key observations from ModTemplate:**
- `map.info` in the template has only `title` and `description` — **no `lots` field**.
  MAP-5A was writing `lots=<map_id>` which may not be correct for Build 42.
- `spawnpoints.lua` uses `worldX = 1, worldY = 1` — not zero-origin.
  MAP-5A was using `worldX = 0, worldY = 0`.
- `preview.png` and `poster.png` are required at specific locations.

---

## 3. Three distinct discovery screens (not to be confused)

| Screen | Purpose | Scope |
|---|---|---|
| **Build 42 Mods screen** | Enable/disable mods for a game session | Shows `Zomboid\mods\<id>\` and `Workshop\...\Contents\mods\<id>\` packages |
| **Workshop upload / item directory screen** | Upload or manage Workshop items | Shows `Zomboid\Workshop\<package_name>\` and PZ install `Workshop\` folders |
| **TileZed / WorldEd** | Map source editing tool | Separate from PZ mod loading; not relevant to discovery |

The MAP-5B attempts mixed paths from the Workshop upload screen with mod discovery
paths. These are distinct flows in Build 42.

---

## 4. Package inspection results (MAP-5C diagnostic run)

`inspect-build42-mod-package.ps1` was run against the `PZMapForgeEmptyCellTest`
Workshop package. All 5 expected Build 42 paths are **present**:

| Check | Result |
|---|---|
| `workshop.txt` at root | PRESENT |
| `preview.png` at root | PRESENT |
| `Contents/mods/` directory | PRESENT |
| Nested `mod.info` | PRESENT |
| Nested `media/maps/*/map.info` | PRESENT |

The nested `mod.info` has correct fields for Build 42:
`category=map`, `modversion=1.0`, `pzversion=42.0`, `versionMin=42.0`, `poster=poster.png`.

**The package structure is correct.** The blocker is NOT a missing file or wrong structure.

## 5. Current blocker

```text
BLOCKER: Build 42 mod discovery mechanism not yet confirmed.
```

The `PZMapForgeEmptyCellTest` Workshop package has the correct structure (matches
ModTemplate) AND correct `mod.info` fields, but the mod still did not appear in
the Build 42 Mods screen. The blocker is not packaging structure.

Possible causes (not yet investigated):
1. Build 42 Workshop mods must be subscribed via Steam Workshop to appear, even
   if placed in the correct folder manually.
2. The `workshop.txt` `visibility=public` field or a missing Steam item ID may
   prevent local-only packages from being loaded.
3. The `Contents/mods/<id>/` nested path may require a local-mod registry entry
   that the game writes when downloading from Workshop.
4. A `poster.png` vs `icon` field discrepancy, or `map.info` missing fields
   required by Build 42.

A reliable Build 42 packaging path that shows a map mod in the Mods screen has
not been proven. The `inspect-build42-mod-package.ps1` script confirms the structure
is correct; the next investigation step is the discovery mechanism itself.

---

## 5. Required proof before MAP-5B can be revisited

- [ ] One confirmed path where a map-content mod (not just a script mod) appears
      in the Build 42 Mods screen.
- [ ] Confirmed `mod.info` fields required for Build 42 map mod visibility.
- [ ] Confirmed correct `worldX`/`worldY` coordinate origin for Build 42.
- [ ] Confirmed whether `map.info` requires a `lots` field in Build 42.

---

## 6. MAP-5A binary hypotheses — status unchanged

| File | Size | Status |
|---|---|---|
| `.lotheader` | 8 bytes | UNTESTED — packaging blocker |
| `.lotpack` | 7208 bytes | UNTESTED — packaging blocker |
| `chunkdata_*.bin` | 902 bytes | UNTESTED — packaging blocker |

All three binary hypotheses remain unconfirmed. They may be correct, incorrect,
or partially correct. No conclusion can be drawn until a mod is loaded.

---

## 7. Tools

| Tool | Purpose |
|---|---|
| `scripts/inspect-build42-mod-package.ps1` | Compares a package against ModTemplate; identifies structural gaps |
| `docs/examples/manual-load-test/MAP_5B_LOAD_TEST_RECORD_TEMPLATE.md` | Load test record template |

---

## 8. Non-claims

- MAP-5B result is INCONCLUSIVE, not FAIL. Binary file hypotheses are unproven.
- This document does not claim the binary map files are correct or incorrect.
- No playable export claim.
- No PZ install files were modified.
- No PZ assets were copied.
