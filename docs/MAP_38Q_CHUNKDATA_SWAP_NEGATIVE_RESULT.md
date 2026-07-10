# MAP-38Q: Real Chunkdata Swap — No Observed Effect

Date: 2026-07-10
Status: NEGATIVE RESULT (human-observed)

## What was tested

Candidate `pzmapforge_map38q` (cell 34_26, otherwise identical to the
confirmed-working `pzmapforge_map38p`) with `chunkdata_34_26.bin` replaced
by real vanilla `chunkdata_36_27.bin` (3906 bytes — the largest/most
complex chunkdata sampled in MAP-38M's analysis, vs. the writer's normal
all-zero 1026-byte body).

## Result

Operator confirmed: same repeating carpet tile at spawn, same plain empty
grass at the second zone location. No observed difference from the
all-zero-chunkdata baseline.

## Conclusion

**Chunkdata content has no observed effect on rendering in this pathway**,
now tested three ways this session:
- MAP-38C (early): non-zero chunkdata (wrong cell 35_27, wrong coordinate
  math at the time) — inconclusive due to the coordinate bug.
- MAP-38L/P/Q baseline: all-zero chunkdata at the corrected coordinate —
  confirmed rendering (the carpet tile).
- MAP-38Q: real, complex, non-zero chunkdata (borrowed from a different
  real cell) at the same corrected coordinate — identical result to
  all-zero.

This matches MAP-38A's own original (untested-at-the-time) note that
all-zero chunkdata didn't visibly break anything, now independently
reconfirmed with a real complex swap in the other direction: chunkdata
appears to be genuinely inert for whatever this writer's tests can
observe, at least via simple whole-file substitution.

## Combined with MAP-38O/P (Vegitation zones)

Three content-placement experiments this session, three clean negatives:
1. Vegitation zone over the marker-tile block — no effect.
2. Vegitation zone over untouched ground — no effect.
3. Real complex chunkdata swap — no effect.

**Within this testing pathway** (mod's own per-cell `objects.lua` +
`lotpack` + `chunkdata`, chained to Muldraugh via `lots=Muldraugh, KY`),
the only things confirmed to actually affect observable output are:
- The lotpack's tile content (ground tiles) — confirmed working (MAP-38K/L).
- The `objects.lua` `SpawnPoint` object — confirmed working (MAP-38G,
  registration/spawn location).

Chunkdata and `Vegitation`-type zone objects show no effect in every test
run this session.

## Why this is a reasonable stopping point for blind experimentation

Without either (a) a decoded chunkdata format from a source outside this
repo, or (b) a real, inspectable example of a per-cell `objects.lua` zone
actually producing a visible effect, further guessing has low expected
value — this session already ruled out every readily-testable hypothesis
for "content beyond ground tiles." The remaining productive paths are
external: ask the PZ modding Discord directly (channel already referenced
in `docs/B42_MAPPING_DISCORD_TUTORIALS.md`), or find/decode a second real
worked example of chunkdata's structure with more time than a single
session allows.

## Claim boundary

chunkdata_content_effect_confirmed=false (tested with real complex data,
no effect)
PLAYABLE_EXPORT_CLAIM_ALLOWED=false
CLAUDE_RAN_PZ=false
