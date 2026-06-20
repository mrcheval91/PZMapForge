# MAP-29A: DeadMTL Worldbuilder Residential Blue Quadrilateral Lot Fill

## Purpose

Detects residential blue quadrilateral shapes in the source map PNG and replaces
every original residential-blue pixel with deterministic beige lot fills.
Produces a replacement PNG plus JSON/CSV proof artifacts.

## Claim boundary

- sandbox_only = true
- writer_ready = false
- runtime_valid = false
- materialized = false
- runtime_proof_claimed = false
- public_playable_packaging_claimed = false

NOT a playable Project Zomboid export.
NOT .lotpack / .lotheader / .lua / .bin. Source PNG is never mutated.

## Color classifiers

### IsResidentialBlue

Exact known colors:
- RGB 58,94,174
- RGB 74,110,190
- RGB 42,78,158

Conservative classifier (all conditions must hold):
- B >= 120
- B >= R + 25
- B >= G + 10
- R >= 30 or G >= 30 (not near-black)
- not IsStreetAccessOrange
- not cyan (40,192,192)

### IsStreetAccessOrange

- R >= 180
- G >= 80 and G <= 180
- B <= 80
- R >= G + 40
- G >= B + 20

## Detection pipeline

1. Load source PNG (must be 256x256).
2. Classify each pixel with IsResidentialBlue.
3. Run 4-connectivity BFS to extract connected components.
4. Sort components by (bboxY1, bboxX1) for deterministic ordering.
5. For each component: compute bounding box, pixel count, fill ratio.
6. Components with fill_ratio < 0.70 are marked unsupported.
7. Inspect 1 pixel outside each bbox side for orange adjacency.
8. Adjacency threshold: max(2, ceil(sideLength * 0.25)) orange contacts.

## Lot orientation cases

- A (N+S): north and south adjacency both true -> 2 horizontal rows
- B (N):   north only -> 1 row, all lots face NORTH
- C (S):   south only -> 1 row, all lots face SOUTH
- D (E+W): east and west adjacency both true -> 2 vertical columns
- E (E):   east only -> 1 column, all lots face EAST
- F (W):   west only -> 1 column, all lots face WEST

Priority: A > D > B > C > E > F.

## Lot count calculation

    lotCount = max(1, round(frontageSpan / 15))

where frontageSpan is the X-extent (N/S) or Y-extent (E/W) of the component.

## SplitInclusiveSpan

Splits an inclusive range [start..end] into count adjacent sub-ranges.
max(width) - min(width) <= 1. First (total % count) ranges get +1 tile.

## Shade assignment

Palette: (200,168,120), (176,140,96), (152,116,76).

    shade = palette[(compOrder * 7 + primaryIndex + secondaryIndex) % 3]

where:
- primaryIndex = column index for N/S rows, row index for E/W columns
- secondaryIndex = row index (0=north/west, 1=south/east)

Adjacent lots always differ. Vertically adjacent north/south lots differ.

## Output PNG

Full-size copy of source PNG. Every residential-blue pixel in a processed
component is replaced with its lot's beige shade. All other pixels are preserved
exactly. Source PNG is never modified.

## CLI command

    deadmtl-build-worldbuilder-residential-blue-quadrilateral-lot-fill
      --source-png <map.png>
      --output-root <.local dir>
      --output-json <json>
      --output-lots-csv <csv>
      --output-facades-csv <csv>
      --output-checks-csv <csv>
      --output-png <png>
      --output-html <html>
      --summary <txt>

All output paths must contain ".local".
