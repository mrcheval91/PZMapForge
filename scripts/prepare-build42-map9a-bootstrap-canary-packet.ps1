#Requires -Version 5.1
<#
.SYNOPSIS
    Writes the MAP-9A Muldraugh bootstrap canary overlay packet to .local/.

    Records MAP-8Z runtime fallback result and defines the MAP-9A controlled test:
    - Muldraugh required as bottom/bootstrap entry in Map line
    - Fresh world reset required
    - Unmistakable PZMapForge canary cell required
    - Canary writer state: blocked (empty_grass profile not visually distinctive)

    Writes under -Output (must be under .local/).
    Does NOT run Project Zomboid.
    Does NOT write to Steam or Workshop.
    Does NOT copy third-party files.
    Does NOT claim playable.
#>

param(
    [string]$Output = ''
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot  = Split-Path -Parent $scriptDir

if (-not $Output) {
    $Output = Join-Path $repoRoot '.local\map9a'
}

# .local guard
$localRoot = Join-Path $repoRoot '.local'
$resolvedOutput = [System.IO.Path]::GetFullPath($Output)
$resolvedLocal  = [System.IO.Path]::GetFullPath($localRoot)
if (-not $resolvedOutput.StartsWith($resolvedLocal)) {
    Write-Error "Output '$Output' must be under .local/. Refusing to write outside .local/."
    exit 1
}

# Forbidden path guards
$forbidden = @(
    'media\maps',
    'Steam',
    'workshop',
    'ProjectZomboid',
    'C:\Program Files',
    'D:\Program Files'
)
foreach ($f in $forbidden) {
    if ($resolvedOutput -like "*$f*") {
        Write-Error "Output path contains forbidden segment '$f'. Refusing."
        exit 1
    }
}

New-Item -ItemType Directory -Force -Path $Output | Out-Null

$generatedAt = (Get-Date).ToUniversalTime().ToString('yyyy-MM-ddTHH:mm:ssZ')

# ---------------------------------------------------------------------------
# Packet JSON
# ---------------------------------------------------------------------------

$packet = [ordered]@{
    schema                          = 'pzmapforge.map9a-bootstrap-canary-packet.v0.1'
    generated_at_utc                = $generatedAt
    map8z_result_recorded           = $true
    no_muldraugh_strategy_rejected  = $true
    muldraugh_bootstrap_required    = $true
    server_map_line                 = 'Map=pzmapforge_build42_candidate_v4_001;PZMapForge;Muldraugh, KY'
    fresh_world_required            = $true
    canary_required                 = $true
    canary_writer_available         = $false
    canary_writer_blocked           = $true
    canary_writer_blocked_reason    = 'current_cell_writer_produces_empty_grass_only_not_visually_distinguishable_from_muldraugh_fallback_and_igmb_cell_index_model_not_confirmed'
    canary_description              = 'canary_blocked_empty_grass_profile_indistinguishable_from_vanilla_muldraugh_fallback'
    staged_output_local_only        = $true
    steam_write_performed           = $false
    workshop_upload_performed       = $false
    pz_run_performed                = $false
    third_party_files_copied        = $false
    playable_claim_allowed          = $false
    success_signal                  = 'visible_unmistakable_canary_cell_and_logs_support_PZMapForge_mount'
    failure_signal                  = 'vanilla_muldraugh_or_isometagrid_does_not_list_PZMapForge'
    next_branch                     = 'map9a_human_runtime_test_pending'
    classification                  = [string[]]@(
        'MAP9A_MULDRAUGH_BOOTSTRAP_CANARY_OVERLAY_DEFINED',
        'MAP8Z_RUNTIME_FALLBACK_MULDRAUGH_CONFIRMED',
        'MAP8Z_NO_MULDRAUGH_STILL_VANILLA_FALLBACK',
        'NO_MULDRAUGH_STRATEGY_REJECTED',
        'NO_PLAYABLE_CLAIM'
    )
    human_action_sequence           = [string[]]@(
        '1. Close PZ/server.',
        '2. Stage the candidate Workshop payload (PZMapForge-owned files only).',
        '3. Keep Map line: Map=pzmapforge_build42_candidate_v4_001;PZMapForge;Muldraugh, KY',
        '4. Park or delete the old generated save/db for the test server only.',
        '5. Run a fresh server.',
        '6. Look for an unmistakable PZMapForge canary cell.',
        '7. Capture logs: IsoMetaGrid map folder list, lotheader parse attempts.'
    )
}

$jsonPath = Join-Path $Output 'map9a-bootstrap-canary-packet.json'
$packet | ConvertTo-Json -Depth 4 | Set-Content -Path $jsonPath -Encoding UTF8
Write-Output "Packet JSON: $jsonPath"

# ---------------------------------------------------------------------------
# Packet MD
# ---------------------------------------------------------------------------

$mdPath = Join-Path $Output 'map9a-bootstrap-canary-packet.md'
$md = @"
# MAP-9A Bootstrap Canary Packet

Generated: $generatedAt
Schema: pzmapforge.map9a-bootstrap-canary-packet.v0.1

## Classification

- MAP9A_MULDRAUGH_BOOTSTRAP_CANARY_OVERLAY_DEFINED
- MAP8Z_RUNTIME_FALLBACK_MULDRAUGH_CONFIRMED
- MAP8Z_NO_MULDRAUGH_STILL_VANILLA_FALLBACK
- NO_MULDRAUGH_STRATEGY_REJECTED
- NO_PLAYABLE_CLAIM

## MAP-8Z result recorded

map8z_result_recorded=true

The MAP-8Z runtime test confirmed that the generated worldmap.xml.bin
did not produce a visible custom-map mount. The visible world was Muldraugh/vanilla fallback.
The no-Muldraugh hard-fail test still showed vanilla fallback.
No-Muldraugh strategy is REJECTED.

## MAP-9A controlled server Map line

``````text
Map=pzmapforge_build42_candidate_v4_001;PZMapForge;Muldraugh, KY
``````

no_muldraugh_strategy_rejected=true
muldraugh_bootstrap_required=true
Muldraugh must remain as the last/bottom/bootstrap entry.

## Fresh world

fresh_world_required=true
Park or delete the old generated save/db before running MAP-9A.

## Canary state

canary_required=true
canary_writer_available=false
canary_writer_blocked=true
canary_writer_blocked_reason=current_cell_writer_produces_empty_grass_only_not_visually_distinguishable_from_muldraugh_fallback_and_igmb_cell_index_model_not_confirmed

No visual-success claim is allowed until a real canary is observed.

## Safety

staged_output_local_only=true
steam_write_performed=false
workshop_upload_performed=false
pz_run_performed=false
third_party_files_copied=false
playable_claim_allowed=false

## Signals

success_signal=visible_unmistakable_canary_cell_and_logs_support_PZMapForge_mount
failure_signal=vanilla_muldraugh_or_isometagrid_does_not_list_PZMapForge

## Next branch

next_branch=map9a_human_runtime_test_pending
"@
Set-Content -Path $mdPath -Value $md -Encoding UTF8
Write-Output "Packet MD: $mdPath"

# ---------------------------------------------------------------------------
# Human overlay doc
# ---------------------------------------------------------------------------

$docPath = Join-Path $Output 'MAP_9A_MULDRAUGH_BOOTSTRAP_CANARY_OVERLAY_PACKET.md'
$docContent = @"
# MAP-9A: Muldraugh Bootstrap Canary Overlay Packet

Generated: $generatedAt

## Purpose

This packet defines the MAP-9A controlled test strategy following the MAP-8Z runtime
fallback result. The goal is a fresh-world run with Muldraugh last in the Map line,
using an unmistakable PZMapForge canary cell as the success signal.

## MAP-8Z runtime fallback (recorded)

- Generated worldmap.xml.bin installed and SHA-256 verified.
- Server reached in-game.
- Visible world: Muldraugh / vanilla fallback.
- No-Muldraugh hard-fail test: also still showed vanilla fallback.
- No-Muldraugh strategy: REJECTED.
- Interpretation: Build 42 coop/server silently bootstraps to vanilla world behavior.

## Controlled server config for MAP-9A

``````text
Mods=pzmapforge_build42_candidate_v4_001
Map=pzmapforge_build42_candidate_v4_001;PZMapForge;Muldraugh, KY
WorkshopItems=3740642200
``````

Muldraugh, KY must remain as the last entry (bootstrap/fallback).

## Human action sequence

1. Close PZ/server.
2. Stage the candidate Workshop payload (PZMapForge-owned files only).
3. Keep Muldraugh last in the Map line.
4. Park or delete the old generated save/db for the test server only.
5. Run a fresh server.
6. Look for an unmistakable PZMapForge canary cell.
7. Capture logs: IsoMetaGrid map folder list, lotheader parse attempts.

## Canary requirement

The canary must be visually impossible to confuse with vanilla Muldraugh.
Examples: all-asphalt cell, all-water cell, giant road/cross pattern, cleared square
with unique marker.

canary_writer_blocked=true
reason: current cell writer produces empty_grass only; empty grass is not visually
distinguishable from vanilla Muldraugh fallback terrain; IGMB cell index model not confirmed.

Do not claim visual success without observing an unmistakable canary cell.

## Safety contract

- staged_output_local_only=true
- steam_write_performed=false
- workshop_upload_performed=false
- pz_run_performed=false
- third_party_files_copied=false
- playable_claim_allowed=false

## Next branch

next_branch=map9a_human_runtime_test_pending
"@
Set-Content -Path $docPath -Value $docContent -Encoding UTF8
Write-Output "Overlay doc: $docPath"

Write-Output "MAP-9A bootstrap canary packet complete."
