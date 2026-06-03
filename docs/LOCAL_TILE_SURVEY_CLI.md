# Local Tile Survey CLI

Status: Slice 3A-5 implemented

Claim boundary: planning_artifact_only_not_pz_load_tested

---

## Purpose

The `local-tile-survey` command reads a local PZ install config, validates
the referenced install directory, and writes a local-only planning artifact
recording what was found.

It does not read PZ asset contents. It does not copy assets. It does not
write to `media/maps`. It does not produce a playable PZ export.

The artifact is evidence that a local PZ install is configured and the
expected directory structure exists. It is a precondition record, not a
tile catalog.

---

## Command syntax

```
pzmapforge local-tile-survey --config <path> [--output <dir>]
```

| Argument | Required | Description |
|---|---|---|
| `--config <path>` | Yes | Path to a local PZ install config JSON file |
| `--output <dir>` | No | Output directory (must end with `.local`; defaults to `.\.local`) |

Short forms: `-c` for `--config`, `-o` for `--output`.

---

## Example (fake paths only)

```powershell
pzmapforge local-tile-survey `
  --config C:\Projects\MyMod\.local\pz-install-config.json `
  --output  C:\Projects\MyMod\.local
```

Expected console output on success:

```
Schema:                   pzmapforge.local-tile-reference-survey.v0.1
Claim boundary:           planning_artifact_only_not_pz_load_tested
Install root exists:      True
Tiles root exists:        True
Likely tile data present: True
Survey JSON:              C:\Projects\MyMod\.local\local-tile-reference-survey.json
Survey MD:                C:\Projects\MyMod\.local\local-tile-reference-survey.md
PZ assets copied:         False
media/maps touched:       False
Playable export claimed:  False
Status:                   OK
```

---

## Output files

Both files are written to the `--output` directory.

| File | Purpose |
|---|---|
| `local-tile-reference-survey.json` | Machine-readable survey record (schema v0.1) |
| `local-tile-reference-survey.md`   | Human-readable summary with safety evidence |

These files are local-only artifacts. They must not be committed to the
repository. The `.local/` directory is gitignored.

---

## Validation behavior

The command runs two layers of checks before writing:

1. **Config load** -- reads and parses the config JSON; verifies required
   fields and safety flags (`allow_asset_copy: false`,
   `allow_media_maps_write: false`).

2. **Install validation** -- checks that `pz_install_root` exists and that
   the `tiles_root` subdirectory exists; records extension counts for
   `.pack`, `.tiles`, `.png`, `.lotpack`, `.lotheader`, `.bin` files.

If either layer fails, the command exits 1 and writes nothing.

---

## Safety guarantees

- The command reads directory listings only. It does not open or parse any
  PZ asset file.
- No PZ assets are copied.
- The `media/maps` directory is never written.
- The `allow_asset_copy` and `allow_media_maps_write` flags in the config
  must both be `false`; the command refuses to run if either is `true`.
- Output is rejected unless the final directory segment is `.local`.

---

## Non-claims

- This command does not generate a tile catalog.
- This command does not map semantic kinds to PZ tile IDs.
- This command does not produce lotpack, lotheader, or bin files.
- This command does not claim a playable Project Zomboid export.
- Tile format internals (.pack, .tiles) are not parsed; their presence is
  recorded but their contents are not read.

---

## Troubleshooting

| Symptom | Likely cause | Action |
|---|---|---|
| `local-tile-survey requires --config <path>` | `--config` argument missing | Supply a valid config path |
| `--output must end with a .local directory` | Output path does not end with `.local` | Change `--output` to a path ending in `.local` |
| `Status: INVALID (config validation failed)` | Config JSON is malformed or safety flags are wrong | Check `allow_asset_copy` and `allow_media_maps_write` are both `false`; verify schema and required fields |
| `Install root exists: False` | `pz_install_root` in config does not exist | Confirm the path in the config points to a real directory |
| `Tiles root exists: False` | `tiles_root` subdirectory not found under install root | Check that the `tiles_root` field names a directory that exists |
