# MAP-5B: Manual Load Test Protocol

```text
Schema:           pzmapforge.load-test-protocol.v0.1
Claim boundary:   experimental_local_only_not_playable_not_load_tested
Status:           MAP-5B attempted — LOAD_TEST_INCONCLUSIVE (packaging/discovery blocker)
Binary writer:    map-export-experimental (MAP-5A)
See:              docs/MAP_5C_BUILD42_MOD_PACKAGING_DISCOVERY.md
```

**MAP-5B result: LOAD_TEST_INCONCLUSIVE**

A MAP-5B load test was attempted on Build 42. The binary map files were not
reached. The blocker is Build 42 mod discovery, not the generated binary files.
See `docs/MAP_5C_BUILD42_MOD_PACKAGING_DISCOVERY.md` for full details.

Binary hypotheses from MAP-5A remain untested.

---

## 1. Purpose

This document defines the manual load test protocol for the MAP-5A experimental
empty cell output. It does not claim the experiment will succeed. It defines
what to do, what to observe, and how to record the result.

A load test result does not exist until the operator performs and records it.
Until then, `load_tested: false` in all generated reports.

---

## 2. What is being tested

The MAP-5A experimental writer generates three hypothesis-only binary files for
a single empty compiled cell:

| File | Size | Hypothesis |
|---|---|---|
| `<cx>_<cy>.lotheader` | 8 bytes | Zero header + 0-entry tileset list |
| `world_<cx>_<cy>.lotpack` | 7208 bytes | hdrA=900, hdrB=7204, all-zero chunk offsets |
| `chunkdata_<cx>_<cy>.bin` | 902 bytes | `00 01` header + 900 zero-byte chunk grid |

These files are hypothesis-only. Any of the three assumptions may be wrong.
The load test determines which assumptions hold.

---

## 3. Prerequisites

- Project Zomboid is installed locally.
- MAP-5A has been run and produced output under `.local/`:
  ```
  dotnet run --project src/PZMapForge.Cli -- map-export-experimental \
    --map-id pzmapforge_test --output .local/map-export-experimental/test-cell
  ```
- The `prepare-map-export-experimental-load-test.ps1` script has been run
  to produce a load-test packet (instructions + record template) under `.local/`.

---

## 4. Load test steps

### Step 1: run the packet preparation script

```powershell
powershell -ExecutionPolicy Bypass -File "scripts\prepare-map-export-experimental-load-test.ps1" `
    -Source ".local\map-export-experimental\<run-name>" `
    -Output ".local\load-tests\<run-name>"
```

Read the generated `MAP_5B_LOAD_TEST_PACKET.md` for the exact copy path.

### Step 2: copy the mod folder to PZ mods directory

Copy the entire MAP-5A output directory to your PZ user mods folder:

```
Source: .local\map-export-experimental\<run-name>\
Destination: C:\Users\<YourName>\Zomboid\mods\<map_id>\
```

**Do not copy to the PZ install directory. Use the user mods folder only.**

The mod folder must be named exactly `<map_id>` (the value you passed to `--map-id`).

### Step 3: launch Project Zomboid

Start PZ in a sandbox or debug mode. Enable the mod in the mod manager before
starting a new game. The mod should appear as `<map_id>` in the mod list.

### Step 4: observe and record

Observe and record:
- Does the mod appear in the mod list?
- Does a map location appear in the spawn/map selection?
- Does the game start without crashing?
- Is the experimental cell visible on the in-game map?
- Can the player spawn in the cell?
- What error messages appear (if any)?
- Check the PZ log file at `C:\Users\<YourName>\Zomboid\Logs\` for errors.

### Step 5: record the result

Fill in the `MAP_5B_LOAD_TEST_RECORD.local-template.md` file (from the load
test packet output) with your observations.

Record the result as one of:
- `LOAD_TEST_PASS` — mod loads, cell is accessible, no crash.
- `LOAD_TEST_FAIL` — mod fails to load, cell is missing, or game crashes.
- `LOAD_TEST_INCONCLUSIVE` — partial results; some features work but not all.

### Step 6: commit the record (if appropriate)

A sanitized load test record (no binary content, no personal paths) may be
committed to `.local/` or to `docs/` if it provides evidence for future slices.
Do not commit the mod files themselves.

---

## 5. What success tells us

If `LOAD_TEST_PASS`:
- Zero-offset lotpack assumption is valid for empty chunks.
- 902-byte all-zero chunkdata is valid for empty cells.
- 0-entry (or minimal) lotheader is valid for a blank cell.
- Single-cell map loads without a world grid.
- MAP-5C (tile-referenced cell or multi-cell map) is unblocked.

## 6. What failure tells us

If `LOAD_TEST_FAIL`, note which symptom appears:
- **PZ crash on load** → likely lotpack format error (zero-offset assumption wrong).
- **Mod visible but no map** → likely map.info `lots` field issue.
- **Map visible but no spawn** → likely spawnpoints.lua coordinate issue.
- **Cell blank/invisible** → likely lotheader tileset issue or lotpack data issue.

Each failure mode points to a specific gap to probe next.

---

## 7. Non-claims

- This protocol does not claim the experiment will succeed.
- No playable export claim is made until `LOAD_TEST_PASS` is recorded.
- Recording `LOAD_TEST_PASS` in a local file does not itself constitute a
  public claim. The result must be reviewed before any public statement.
- Build 41 vs Build 42 differences are not investigated by this test.

---

## 8. Tools

| Tool | Purpose |
|---|---|
| `scripts/prepare-map-export-experimental-load-test.ps1` | Validates MAP-5A output; writes load-test packet and record template |
| `docs/examples/manual-load-test/MAP_5B_LOAD_TEST_RECORD_TEMPLATE.md` | Master fillable template |
