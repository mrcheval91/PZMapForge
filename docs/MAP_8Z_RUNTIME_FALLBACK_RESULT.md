# MAP-8Z Runtime Fallback Result

```text
Status: MAP-8Z runtime result recorded
Classification: MAP8Z_RUNTIME_FALLBACK_MULDRAUGH_CONFIRMED
No-Muldraugh strategy: REJECTED
Playable claim: not allowed
Next branch: MAP-9A Muldraugh bootstrap canary overlay
```

---

## Source basis

MAP-8Z defined the controlled install packet for the MAP-8Y generated worldmap.xml.bin
(sha256=b5204f805f0fd29c54a56ce0f80e964830ec2f7864f80bbd4956ed0cbe668f6f, size=65536 bytes).
The operator staged the file, manually copied it to the candidate Workshop path, and ran
a Build 42 coop/server with the controlled server config.

---

## Runtime test result — controlled server (Muldraugh present)

Server config used:

```text
Mods=pzmapforge_build42_candidate_v4_001
Map=pzmapforge_build42_candidate_v4_001;PZMapForge;Muldraugh, KY
WorkshopItems=3740642200
```

Result:
- Server reached in-game.
- MAP-8Y generated worldmap.xml.bin was staged and copied correctly.
- SHA-256 matched: b5204f805f0fd29c54a56ce0f80e964830ec2f7864f80bbd4956ed0cbe668f6f
- Visible world: Muldraugh / vanilla fallback.
- PZMapForge custom content was NOT visibly mounted.
- generated_worldmap_xml_bin_produced_custom_map_mount=false
- visible_world=muldraugh_vanilla_fallback
- no_playable_claim=true

---

## Hard-fail test — no-Muldraugh server config

A second test was run with Muldraugh explicitly removed from the Map line:

```text
Mods=pzmapforge_build42_candidate_v4_001
Map=pzmapforge_build42_candidate_v4_001;PZMapForge
WorkshopItems=3740642200
```

Result:
- Server still reached in-game.
- Visible world: still Muldraugh / vanilla fallback.
- Build 42 coop/server silently bootstraps or falls back to vanilla world behavior
  even when Muldraugh is not in the explicit Map line.
- no_muldraugh_still_shows_vanilla_fallback=true

---

## Conclusion and strategy rejection

The no-Muldraugh strategy is REJECTED.

Removing Muldraugh from the Map line does not prevent vanilla Muldraugh fallback.
Build 42 appears to silently bootstrap or fall back to vanilla world state regardless
of whether Muldraugh is listed explicitly.

Therefore:
- NO_MULDRAUGH_STRATEGY_REJECTED=true
- Muldraugh must remain in the controlled Map line as the bottom/fallback/bootstrap entry.
- The correct Map line remains: Map=pzmapforge_build42_candidate_v4_001;PZMapForge;Muldraugh, KY
- The real next branch is not layout changes but a fresh world reset + unmistakable canary.

---

## Classification labels

```text
MAP8Z_RUNTIME_FALLBACK_MULDRAUGH_CONFIRMED
MAP8Z_NO_MULDRAUGH_STILL_VANILLA_FALLBACK
NO_PLAYABLE_CLAIM
NO_MULDRAUGH_STRATEGY_REJECTED
MULDRAUGH_BOOTSTRAP_REQUIRED=true
PUBLIC_PLAYABLE_CLAIM_ALLOWED=false
```

---

## Next branch

NEXT_BRANCH=map9a_muldraugh_bootstrap_canary_overlay

See docs/MAP_9A_MULDRAUGH_BOOTSTRAP_CANARY_OVERLAY.md.
