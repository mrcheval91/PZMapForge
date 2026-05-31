# Genesis

PZMapForge exists because the standard Project Zomboid mapping tools (WorldEd,
TileZed, BuildingEd) have GUI workflows that are too fragile for deterministic,
version-controlled map planning.

Specifically: TileZed's tile palette was unusable for block-level layout painting,
WorldEd's Generate Lots produced no output, and assigning a generated TMX to a
WorldEd cell crashed. The tool-assisted GUI path consumed significant session time
without producing a committed artifact.

## What PZMapForge is

An independent, deterministic, code-driven planning layer for Project Zomboid map
mod work. It converts a simple blockout image (PNG or BMP) into:

- a semantic cell grid (300x300 or configured size)
- a JSON artifact with kind counts, drift records, and SHA-256 hashes
- a markdown report (claim boundary, matching summary, drift table)
- a visual preview PNG
- a TileZed-openable planning TMX using generated colour tiles

Every output is reproducible from the same inputs. There are no GUI sessions to
replay.

## Who it serves

Project Zomboid map modders who want to design cell layouts deterministically,
track them in git, and validate them before attempting a real PZ export.

## What is out of scope

- Producing a Project Zomboid load-tested map export.
- Replacing WorldEd, TileZed, or BuildingEd for all workflows.
- Distributing or incorporating Project Zomboid game assets.
- Building a general-purpose game map editor.
- Claiming Build 42 or any specific PZ version compatibility without a documented
  local load test.

## Source of identity

PZMapForge is tool infrastructure. It owns one pipeline step:
image blockout -> semantic grid -> planning artifact.

It does not own the PZ map export step. That step requires a separate verified
toolchain (WorldEd export, local load test, documented result) before any claim
can be made.
