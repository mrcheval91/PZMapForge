# MAP-8K: Parent Map Metadata Contract Comparator

```text
MAP8K_PARENT_METADATA_CONTRACT_COMPARATOR_DEFINED
BINARY_WRITER_GATE_STILL_CLOSED
PUBLIC_PLAYABLE_CLAIM_ALLOWED=false
NO_PZ_RUN_BY_CLAUDE
NO_WORKSHOP_UPLOAD_BY_CLAUDE
NO_THIRD_PARTY_FILES_COPIED
NO_BINARY_CONTENTS_READ
```

---

## 1. Purpose

MAP-8J confirmed MAP-8I result:

```text
MAP8I_SPAWNPOINT_FIXED_BUT_ISOMETAGRID_NOT_MOUNTED
spawn_coordinate_matches_35_27=true
iso_meta_grid_map_folder_list_empty=true
next_branch=parent_metadata_or_binary_cell_mount_contract
```

The spawnpoint profession error is resolved. Player spawns at the correct
35_27 coordinate. The remaining blocker is: IsoMetaGrid does not mount or
list the PZMapForge parent map folder.

MAP-8K defines the evidence step for the parent metadata branch.

MAP-8K does not solve the mount.
MAP-8K defines the parent metadata comparator for that investigation.

---

## 2. Known-working reference

Project Russia (Workshop ID 3734334068) uses a confirmed working parent/child
map contract in Build 42. Observed contract:

```text
Child city example  common\media\maps\Dmitrov\map.info:
  title=Dmitrov
  lots=Project Russia
  description=Chunk size is 8x8, Cell size is 256x256
  zoomX=9000
  zoomY=9300
  zoomS=14.5
  demoVideo=PR.bik

Parent folder: common\media\maps\Project Russia
  No lots field.
  Contains actual cell binaries.
  common\media\maps is NOT ignored in Build 42.
```

---

## 3. Current PZMapForge parent shape (MAP-8H/MAP-8I)

```text
common\media\maps\PZMapForge\
  map.info
  35_27.lotheader
  world_35_27.lotpack
  chunkdata_35_27.bin
  objects.lua
  spawnpoints.lua
  worldmap.xml
  thumb.png
  MAP8H_PARENT_CHILD_PROBE.txt
  MAP8I_DUAL_SPAWNPOINT_KEYS.txt
```

---

## 4. Comparator

Script: `scripts\inspect-build42-parent-map-metadata-contract.ps1`

Parameters:
- `-CandidateParentRoot` -- path to PZMapForge parent map folder
- `-ReferenceParentRoot` -- path to Project Russia parent map folder (read-only)
- `-Output`              -- output path (must be under .local/)
- `-CandidateParentMapId` -- label (default: PZMapForge)
- `-ReferenceParentMapId` -- label (default: Project Russia)

Guards:
- `-Output` must be under `.local/`. Script exits nonzero otherwise.
- Script does NOT copy any files from `-ReferenceParentRoot`.
- Script does NOT read binary file contents.

Allowed reads from reference:
- `map.info` text key/value fields.
- File name, count, extension, byte size (not contents) for binary files.
- For worldmap.xml, objects.lua, spawnpoints.lua: byte size, line count,
  skeletal/substantial classification. No full content copied.

Forbidden reads:
- `*.lotheader` contents
- `*.lotpack` contents
- `chunkdata_*.bin` contents
- `*.bin` contents
- `*.png` contents
- `*.bik` contents
- `*.pack` contents

Outputs (under -Output/):
- `build42-parent-map-metadata-contract.json`
- `build42-parent-map-metadata-contract.md`

---

## 5. Suggested operator run

### Step A: Copy candidate parent (PZMapForge-owned files only)

```powershell
$src = "D:\Program Files (x86)\Steam\steamapps\workshop\content\108600\3740642200\mods\pzmapforge_build42_candidate_v4_001\common\media\maps\PZMapForge"
$dst = ".\.local\map8k-candidate\PZMapForge"
if (-not (Test-Path $dst)) { New-Item -ItemType Directory -Force -Path $dst | Out-Null }
Get-ChildItem -LiteralPath $src -File | Copy-Item -Destination $dst
Write-Output "Copied PZMapForge parent files to $dst"
```

Do NOT run this against the Project Russia folder.
Do NOT copy any Project Russia files.

### Step B: Run comparator

```powershell
$candRoot = ".\.local\map8k-candidate\PZMapForge"
$refRoot  = "D:\Program Files (x86)\Steam\steamapps\workshop\content\108600\3734334068\mods\Project Russia\common\media\maps\Project Russia"
$outDir   = ".\.local\map8k-comparator-output"
powershell -ExecutionPolicy Bypass -File .\scripts\inspect-build42-parent-map-metadata-contract.ps1 `
    -CandidateParentRoot $candRoot `
    -ReferenceParentRoot $refRoot `
    -Output $outDir `
    -CandidateParentMapId "PZMapForge" `
    -ReferenceParentMapId "Project Russia"
```

---

## 6. Claim boundary

```text
MAP8K_PARENT_METADATA_CONTRACT_COMPARATOR_DEFINED
BINARY_WRITER_GATE_STILL_CLOSED
PUBLIC_PLAYABLE_CLAIM_ALLOWED=false
NO_PZ_RUN_BY_CLAUDE
NO_WORKSHOP_UPLOAD_BY_CLAUDE
NO_THIRD_PARTY_FILES_COPIED
NO_BINARY_CONTENTS_READ
```

Non-claims:
- MAP-8K does not claim IsoMetaGrid mount.
- MAP-8K does not claim playable export.
- Comparator is a metadata evidence tool only.
- Binary writer gate remains closed.
- Cell content in Workshop package is generated PZMapForge-owned files only.
