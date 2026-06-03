# Local Tile Reference Survey

Status: Slice 3A-3 implemented

Claim boundary: planning_artifact_only_not_pz_load_tested

## Purpose

The local tile reference survey is a local-only planning artifact foundation.

It records summary evidence already produced by LocalPzInstallValidator:

- install_root_exists
- tiles_root_exists
- extension_counts
- likely_tile_data_present
- png_present
- pack_present
- tiles_present
- lotpack_present
- lotheader_present
- bin_present

The writer emits:

- .local/local-tile-reference-survey.json
- .local/local-tile-reference-survey.md

## Non-claims

Slice 3A-3 does not generate a tile catalog.

Slice 3A-3 does not map semantic kinds to PZ tiles.

Slice 3A-3 does not generate lotpack, lotheader, or bin files.

Slice 3A-3 does not add a CLI command.

Slice 3A-3 does not claim a playable Project Zomboid export.

## Safety

The writer consumes LocalPzInstallValidationResult summary data only.

It does not scan the PZ install.

It does not read PZ asset contents.

It does not copy PZ assets.

It does not write to media/maps.

The emitted artifact records:

- pz_assets_copied = false
- media_maps_touched = false
- playable_export_claimed = false