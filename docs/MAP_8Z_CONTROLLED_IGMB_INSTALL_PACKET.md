# MAP-8Z Controlled IGMB Install Packet

```text
Status: MAP-8Z controlled install packet defined
Classification: MAP8Z_CONTROLLED_IGMB_INSTALL_PACKET_DEFINED
Human manual copy required: true
Playable claim: not allowed
```

## Purpose

The MAP-8Y experimental writer produced a PZMapForge-owned worldmap.xml.bin candidate
(sha256=b5204f805f0fd29c54a56ce0f80e964830ec2f7864f80bbd4956ed0cbe668f6f, size=65536 bytes,
operator-confirmed). MAP-8Z creates a controlled install packet that stages the generated
file and provides explicit human-only steps to install and test it in the candidate
Workshop mod. No automated install is performed. No Steam, Workshop, or Project Zomboid
files are modified by Claude or any automated process.

## Packet script

Script: `scripts/prepare-build42-map8z-controlled-igmb-install-packet.ps1`

Parameters:
- `-Output` — required; must be under `.local/`
- `-GeneratedWorldmapBinPath` — required; must exist; must be under `.local/`
- `-MapId` — default: `pzmapforge_build42_candidate_v4_001`
- `-ParentMapFolder` — default: `PZMapForge`
- `-WorkshopItemId` — default: `3740642200`

## Staged output layout (under .local/)

```text
.local/map8z-staged-install-packet/
  staged/common/media/maps/PZMapForge/worldmap.xml.bin   <- copy of generated file
  map8z-controlled-igmb-install-packet.json
  map8z-controlled-igmb-install-packet.md
  MAP_8Z_HUMAN_INSTALL_STEPS.md
```

## Human install target

The human (operator) manually copies the staged worldmap.xml.bin to:

```text
D:\Program Files (x86)\Steam\steamapps\workshop\content\108600\3740642200\mods\
  pzmapforge_build42_candidate_v4_001\common\media\maps\PZMapForge\worldmap.xml.bin
```

Claude does NOT copy to this path. Claude does NOT touch Steam, Workshop, or PZ files.

## Test goal

Determine whether the generated worldmap.xml.bin changes WorldMapDataAssetManager
or IsoMetaGrid behavior in the candidate mod.

Success signal: runtime log attempts or accepts the generated file without
worldmap_xml_bin_parse_error.

Failure signal: worldmap_xml_bin_parse_error or no behavior change observed.

## Safety

```text
MAP8Z_CONTROLLED_IGMB_INSTALL_PACKET_DEFINED
HUMAN_MANUAL_COPY_REQUIRED=true
CLAUDE_COPIED_TO_WORKSHOP=false
CLAUDE_MODIFIED_STEAM_FILES=false
CLAUDE_RAN_PZ=false
CLAUDE_UPLOADED_WORKSHOP=false
THIRD_PARTY_BYTES_COPIED=false
GENERATED_FILE_ONLY=true
PLAYABLE_CLAIM_ALLOWED=false
LOAD_TEST_PERFORMED=false
WRITER_STATUS=experimental_skeleton_not_load_proven
PUBLIC_PLAYABLE_CLAIM_ALLOWED=false
NO_PZ_RUN_BY_CLAUDE
NO_WORKSHOP_UPLOAD_BY_CLAUDE
NO_STEAM_OR_WORKSHOP_WRITES_BY_CLAUDE
```

## Next branch

next_branch=human_runtime_test_pending

Operator manually installs the staged worldmap.xml.bin and runs the server with the
same controlled config as prior tests. Operator captures and reports runtime logs.
Playable claim is not allowed until runtime logs confirm successful mount.
