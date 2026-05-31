# Constitution

Non-negotiable behavioral rules for PZMapForge. These cannot be overridden by
convenience, deadline pressure, or feature scope.

---

## 1. No Project Zomboid asset redistribution

PZMapForge must not copy, embed, or redistribute Project Zomboid tilesheets,
sprites, sounds, source code, or any other proprietary Indie Stone asset.

Future features may reference a locally installed PZ copy for local generation
only. Any such feature must document the boundary explicitly and keep generated
outputs out of git unless a separate load-test milestone justifies committing them.

## 2. No playable export claim without a local load test

"Planning artifact" is the maximum current claim. Claiming a playable or
PZ-compatible export requires:
1. A local Project Zomboid install.
2. A generated lotpack/lotheader/bin that actually loads without error.
3. A documented record of the load test (date, PZ version, result).

Until all three exist: no playable claim.

## 3. Outputs stay under .local/ by default

Generated artifacts belong under `.local/mapforge/` and must be gitignored.
The tool refuses to write outside `.local/mapforge` unless `-AllowExternalOutput`
is explicitly passed. The tool refuses to write into or over `media/maps`.

## 4. Debug mode exits without writing artifacts

`-Mode Debug` prints colour frequencies, unmapped colours, and diagnostics,
then exits with code 0. It does not write any output files. This ensures
diagnostic runs leave no side effects.

## 5. Palette must define all required kinds

The palette must include all nine required semantic kinds before the tool runs:
grass, road, sidewalk, row_house, depanneur, garage, industrial_yard, landmark, spawn.

GIDs must be positive integers, contiguous from 1, and non-duplicate.
Codes must be exactly one character and non-duplicate.

Any palette that fails these checks causes a nonzero exit before any pixel is read.

## 6. Fail fast and exit nonzero

Every validation failure must exit nonzero immediately. The tool must not silently
absorb bad input or produce partial output on error.

## 7. No official tool claim

PZMapForge is not an official Project Zomboid tool. It is not affiliated with
The Indie Stone. This must not be implied in documentation, output, or metadata.

## 8. WorldEd and TileZed source are not incorporated

WorldEd and TileZed are GPL-licensed tools. Their source may be studied
separately. Any decision to incorporate GPL-derived code requires a documented
license review before that code enters this repository.
