# MAP-38L Corrected-Coordinate Differential Control Test

MAP38L_CORRECTED_COORDINATE_DIFFERENTIAL_PACKET_DEFINED

## Why this packet exists

MAP-38B's differential packet used cellX*300+offset coordinate math --
Build 41's cell width, not Build 42's. MAP-38J found and fixed this (Build 42
cells are 256x256 tiles, confirmed via pzwiki.net/wiki/Mapping). MAP-38K
confirmed renderable_v1 actually renders at the corrected coordinate: cell
34_26, candidate pzmapforge_map38j, operator observed "endless repeating
street/indoor tile" -- the real floors_rugs_01_0 marker tile.

That confirmation was ONE human test, not yet a differential control test.
This packet reruns the MAP-37E/MAP-38B differential pattern (Run A vs Run B)
at the SAME cell and corrected coordinate, to convert MAP-38K's single
confirmation into repeatable evidence before promoting renderable_v1 to
Ratified.

## Expected result if MAP-38K's finding is real

- Run A (files-present): endless repeating street/indoor floor tile pattern
  at spawn -- unmistakably artificial, not procedural forest.
- Run B (files-removed): procedural fallback (forest, scattered
  shrubs/trees, or the real underlying vanilla content for that coordinate
  if it happens to be populated) -- NOT the repeating tile pattern.

If Run A shows the repeating pattern and Run B does not, that is
RENDERABLE_MOUNT_SUCCESS, properly differential this time. If both runs look
the same, MAP-38K's confirmation does not reproduce and needs its own
investigation.

## Docs in this packet

- MAP_38L_HUMAN_INSTALL_STEPS.md       -- Run A / Run B install checklist (HUMAN-ONLY)
- MAP_38L_RUNTIME_RESULT_RECORD.md     -- two-run result recording template

HUMAN_ONLY_INSTALL_REQUIRED=true
CLAUDE_RAN_PZ=false
CLAUDE_WROTE_STEAM=false
CLAUDE_WROTE_WORKSHOP=false
PLAYABLE_EXPORT_CLAIM_ALLOWED=false
