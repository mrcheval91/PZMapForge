#Requires -Version 5.1
<#
.SYNOPSIS
    MAP-7F: Generates a focused registration diagnostic packet for the next
    manual retest of map-folder discovery in Build 42.

    Writes all output under .local/ only.
    Does NOT write to PZ folders.
    Does NOT run PZ.

.PARAMETER Output
    Required. Path under .local/ for packet output.

.PARAMETER MapId
    Optional. Map ID to use in packet. Default: pzmapforge_build42_candidate_v4_001

.PARAMETER ModFolderName
    Optional. Mod folder name. Default: pzmapforge_build42_candidate_v4_001_test

.PARAMETER ServerName
    Optional. Server preset name. Default: PZMF_B42_METADATA_V4_TEST_001

.EXAMPLE
    powershell -ExecutionPolicy Bypass -File .\scripts\prepare-build42-map7f-registration-diagnostic-packet.ps1 `
        -Output .\.local\map7f-packet
#>

param(
    [Parameter(Mandatory=$true)][string]$Output,
    [string]$MapId        = 'pzmapforge_build42_candidate_v4_001',
    [string]$ModFolderName = 'pzmapforge_build42_candidate_v4_001_test',
    [string]$ServerName   = 'PZMF_B42_METADATA_V4_TEST_001'
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Assert-LocalPath {
    param([string]$Path, [string]$Label)
    $norm = $Path.Replace('/', '\')
    if ($norm -notmatch '\\\.local(\\|$)') {
        Write-Error "$Label must be under .local/. Got: $Path"
        exit 1
    }
}

Assert-LocalPath $Output '-Output'

New-Item -ItemType Directory -Force -Path $Output | Out-Null

$fence = '```'

# ---------------------------------------------------------------------------
# MAP_7F_REGISTRATION_DIAGNOSTIC_PACKET.md
# ---------------------------------------------------------------------------

$packetMd = Join-Path $Output 'MAP_7F_REGISTRATION_DIAGNOSTIC_PACKET.md'
Set-Content -Path $packetMd -Encoding ASCII -Value @"
# MAP-7F: Build 42 Map Folder Registration Diagnostic Packet

${fence}text
MAP_FOLDER_SCAN_EMPTY_CONFIRMED
MAP_FOLDER_REGISTRATION_BLOCKER_ACTIVE
ANALYZER_TIMESTAMPED_LOG_BUG_FIXED
LOAD_TEST_NOT_PERFORMED_BY_THIS_SCRIPT
PUBLIC_PLAYABLE_CLAIM_ALLOWED=false
${fence}

## Confirmed state (MAP-7E)

- IsoMetaGrid.Create began scanning directories.
- Map folders list was logged as empty: <End of map-folders list> appeared
  immediately after 'Looking in these map folders:' with no entries between.
- IsoMetaGrid.Create finished scanning in 0.034 seconds.
- IsoMetaGrid.Create finished loading in 11.445 seconds.
- initSpawnBuildings: no room or building at 150,150,0 (spawn building warning persists).
- Player data received from the server.
- game loading took 32 seconds.
- Game Mode: Multiplayer reached.
- Player entered world but world appeared empty.
- No city choice dialog appeared.

## Cleared blockers (MAP-7D/MAP-7E)

- objects.lua LexState/BOM error: CLEARED (no-BOM rewrite applied).
- server spawnregions.lua BOM LexState error: CLEARED.
- spawn null error: CLEARED.
- player-data timeout: CLEARED.

## Remaining blocker

- IsoMetaGrid map folder discovery sees NO map folders.
- Candidate map folder is not registered or not discovered.
- Spawn warning persists: no room or building at 150,150,0.
- No city choice visible.

## Analyzer bug fixed (MAP-7F)

Previous analyzer reported map_folders_list_empty=False when processing a
timestamped DebugLog format log, despite visible empty folder list.

Root cause: regex assumed bare console format. Timestamped lines have prefix:
  [date] LOG : General     , timestamp> Message text.
Regex 'Looking in these map folders:\r?\n(.*?)<End of map-folders list>'
failed because the trailing period on the start line and the full log prefix
before the end marker prevented a match.

Fix: line-by-line parser strips the log prefix and trailing period before
comparing semantic message text. Both bare and timestamped formats now
correctly set map_folders_list_empty=true when markers are adjacent.

New fields: map_folder_parser_strategy, timestamped_debuglog_detected,
map_folder_lines.

## Hypotheses to test

### A. Map= line format variant

The server ini Map= line may need a different format to include the candidate.

Variants:
- A: Map=$MapId;Muldraugh, KY
- B: Map=$MapId
- C: Map=Muldraugh, KY;$MapId

Each variant must be tested separately with a fresh log capture.

### B. spawnregions.lua and server _spawnregions.lua format

Even with the correct Map= line the server must recognise the spawn region.
Each test must confirm no-BOM on spawnregions.lua and server _spawnregions.lua.

### C. lotheader/lotpack/chunkdata errors

If the map folder IS discovered, the next failure will be in the binary files.
Capture any lotheader/lotpack/chunkdata error lines from the DebugLog-server.

## Claim boundary

- This packet is a local diagnostic artifact only.
- No load test is performed by this script.
- LOAD_TEST_NOT_PERFORMED_BY_THIS_SCRIPT
- PUBLIC_PLAYABLE_CLAIM_ALLOWED=false: binding.
"@

# ---------------------------------------------------------------------------
# MAP_7F_MANUAL_RETEST_RECORD.local-template.md
# ---------------------------------------------------------------------------

$recordMd = Join-Path $Output 'MAP_7F_MANUAL_RETEST_RECORD.local-template.md'
Set-Content -Path $recordMd -Encoding ASCII -Value @"
# MAP-7F Manual Retest Record (local template)

Fill this in after each test run. Do not commit.

## Test metadata

- Date:
- PZ version:
- Map= variant tested (A / B / C):
- Map= value used:
- Server preset name: $ServerName
- Mod folder name: $ModFolderName
- Map ID: $MapId

## Pre-test checks (HUMAN-ONLY -- do not automate)

- [ ] spawnregions.lua saved with no-BOM UTF-8 (verify in hex editor)
- [ ] server _spawnregions.lua saved with no-BOM UTF-8
- [ ] server ini Map= updated to variant under test
- [ ] server ini Mods= includes $MapId
- [ ] Old log files deleted before launch
- [ ] Only the candidate mod enabled

## Log capture

- DebugLog path:
- DebugLog-server path:
- coop-console.txt path:

## Map folder scan section

Paste the exact lines from DebugLog between IsoMetaGrid.Create scanning and
finished scanning:

${fence}text
(paste here)
${fence}

- Did pzmapforge appear in the map folder list? YES / NO
- Were any map folders listed? YES / NO
- Map folders list count:

## Result

- Classification: (PARTIAL_PASS_IN_GAME / FAIL_MAP_FOLDER_EMPTY / FAIL_LOTH / FAIL_LOTP / FAIL_CHUNKDATA / INCONCLUSIVE)
- Spawn warning at 150,150,0 persists? YES / NO
- Any lotheader/lotpack/chunkdata errors? YES / NO (list below)
- Player entered world? YES / NO
- World appeared empty? YES / NO

## Error lines observed

${fence}text
(paste any error lines here)
${fence}

## Notes

PUBLIC_PLAYABLE_CLAIM_ALLOWED=false: do not claim playable export until
all blockers cleared and a full pass result is documented.
"@

# ---------------------------------------------------------------------------
# MAP_7F_MAP_LINE_VARIANTS_TO_TEST.md
# ---------------------------------------------------------------------------

$variantsMd = Join-Path $Output 'MAP_7F_MAP_LINE_VARIANTS_TO_TEST.md'
Set-Content -Path $variantsMd -Encoding ASCII -Value @"
# MAP-7F: Map= Line Variants To Test

These are the three Map= line variants to test for map folder registration.
Test each separately. Record results in MAP_7F_MANUAL_RETEST_RECORD.local-template.md.

All edits to server INI, spawnregions.lua, and host.ini are HUMAN-ONLY.
All files must be saved with no-BOM UTF-8 encoding.

## Variant A: Candidate first, Muldraugh append

${fence}ini
Map=$MapId;Muldraugh, KY
${fence}

Hypothesis: candidate discovered first; Muldraugh appended for fallback spawn.

## Variant B: Candidate only

${fence}ini
Map=$MapId
${fence}

Hypothesis: minimal case; no vanilla map; simplest registration test.
Risk: spawn buildings may not be found if only custom map is listed.

## Variant C: Muldraugh first, candidate append

${fence}ini
Map=Muldraugh, KY;$MapId
${fence}

Hypothesis: Muldraugh provides base world; candidate appended as overlay.

## Observation target

For each variant, in DebugLog look for:

${fence}text
IsoMetaGrid.Create: begin scanning directories
Looking in these map folders:
  (expect: $MapId appears here)
<End of map-folders list>
IsoMetaGrid.Create: finished scanning directories
${fence}

If pzmapforge does NOT appear in the map folder list for any variant, the
registration blocker is upstream of the Map= line (file discovery, mod structure).

## No-BOM requirement

The following files must all be saved with no-BOM UTF-8 (HUMAN-ONLY writes):
- server ini (server preset .ini)
- spawnregions.lua
- server _spawnregions.lua
- host.ini if modified

HUMAN-ONLY: do not automate these writes. Verify in a hex editor.
PUBLIC_PLAYABLE_CLAIM_ALLOWED=false
"@

# ---------------------------------------------------------------------------
# MAP_7F_LOG_CAPTURE_AND_ANALYSIS_COMMANDS.md
# ---------------------------------------------------------------------------

$cmdsMd = Join-Path $Output 'MAP_7F_LOG_CAPTURE_AND_ANALYSIS_COMMANDS.md'
Set-Content -Path $cmdsMd -Encoding ASCII -Value @"
# MAP-7F: Log Capture and Analysis Commands

After each manual retest run, capture logs and run the analyzer.

## Log locations (typical)

${fence}text
Client DebugLog:
  C:\Users\<user>\Zomboid\Logs\<date>_DebugLog.txt

Server DebugLog-server:
  C:\Users\<user>\Zomboid\Logs\<date>_DebugLog-server.txt

coop-console.txt:
  C:\Users\<user>\Zomboid\coop-console.txt
${fence}

Copy logs to .local before analysis. Do not analyze in-place.

## Copy commands (HUMAN-ONLY -- adjust paths)

${fence}powershell
Copy-Item "C:\Users\<user>\Zomboid\Logs\<date>_DebugLog.txt" `
    ".\.local\map7f-logs\DebugLog-variant-A.txt"

Copy-Item "C:\Users\<user>\Zomboid\Logs\<date>_DebugLog-server.txt" `
    ".\.local\map7f-logs\DebugLog-server-variant-A.txt"
${fence}

## Run analyzer (PowerShell)

${fence}powershell
powershell -ExecutionPolicy Bypass `
    -File .\scripts\inspect-build42-map7d-load-result.ps1 `
    -LogPath .\.local\map7f-logs\DebugLog-variant-A.txt `
    -Output .\.local\map7f-analysis\variant-A
${fence}

## Check map folder scan section

Look for:
${fence}text
IsoMetaGrid.Create: begin scanning directories
Looking in these map folders:
<End of map-folders list>
IsoMetaGrid.Create: finished scanning directories
${fence}

Key analyzer output fields:
- map_folders_scan_found
- map_folders_list_empty
- map_folders_list_count
- map_folder_parser_strategy
- timestamped_debuglog_detected
- classification

## Repeat for each variant (A, B, C)

Test each Map= variant separately. Use a fresh server preset each time.
Delete old log files before launch to avoid log contamination.

PUBLIC_PLAYABLE_CLAIM_ALLOWED=false
LOAD_TEST_NOT_PERFORMED_BY_THIS_SCRIPT
"@

# ---------------------------------------------------------------------------
# map7f-registration-preflight.json
# ---------------------------------------------------------------------------

$preflightJson = Join-Path $Output 'map7f-registration-preflight.json'
$preflight = [ordered]@{
    schema                              = 'pzmapforge.map7f-registration-preflight.v0.1'
    map_id                              = $MapId
    mod_folder_name                     = $ModFolderName
    server_name                         = $ServerName
    map_folder_scan_empty_confirmed     = $true
    map_folder_registration_blocker     = 'active'
    analyzer_timestamped_log_bug_fixed  = $true
    map_line_variants_to_test           = @('A', 'B', 'C')
    variant_a_map_line                  = "Map=$MapId;Muldraugh, KY"
    variant_b_map_line                  = "Map=$MapId"
    variant_c_map_line                  = "Map=Muldraugh, KY;$MapId"
    all_server_writes_human_only        = $true
    all_files_require_no_bom_utf8       = $true
    load_test_not_performed             = $true
    public_playable_claim_allowed       = $false
}
$preflight | ConvertTo-Json -Depth 4 | Set-Content -Path $preflightJson -Encoding UTF8
Write-Output "JSON: $preflightJson"

# ---------------------------------------------------------------------------
# map7f-registration-preflight.md
# ---------------------------------------------------------------------------

$preflightMd = Join-Path $Output 'map7f-registration-preflight.md'
Set-Content -Path $preflightMd -Encoding ASCII -Value @"
# MAP-7F Registration Preflight

${fence}text
map_id=$MapId
map_folder_registration_blocker=active
analyzer_timestamped_log_bug_fixed=true
variant_a_map_line=Map=$MapId;Muldraugh, KY
variant_b_map_line=Map=$MapId
variant_c_map_line=Map=Muldraugh, KY;$MapId
all_server_writes_human_only=true
all_files_require_no_bom_utf8=true
load_test_not_performed=true
public_playable_claim_allowed=false
${fence}

All server INI and Lua file edits are HUMAN-ONLY.
Do not automate writes to PZ server folders.
PUBLIC_PLAYABLE_CLAIM_ALLOWED=false: binding.
"@
Write-Output "MD: $preflightMd"

# ---------------------------------------------------------------------------
# Summary
# ---------------------------------------------------------------------------

Write-Output ""
Write-Output "MAP_7F_REGISTRATION_DIAGNOSTIC_PACKET.md:         $(Test-Path $packetMd)"
Write-Output "MAP_7F_MANUAL_RETEST_RECORD.local-template.md:   $(Test-Path $recordMd)"
Write-Output "MAP_7F_MAP_LINE_VARIANTS_TO_TEST.md:              $(Test-Path $variantsMd)"
Write-Output "MAP_7F_LOG_CAPTURE_AND_ANALYSIS_COMMANDS.md:      $(Test-Path $cmdsMd)"
Write-Output "map7f-registration-preflight.json:                $(Test-Path $preflightJson)"
Write-Output "map7f-registration-preflight.md:                  $(Test-Path $preflightMd)"
Write-Output ""
Write-Output "MAP_FOLDER_SCAN_EMPTY_CONFIRMED"
Write-Output "MAP_FOLDER_REGISTRATION_BLOCKER_ACTIVE"
Write-Output "ANALYZER_TIMESTAMPED_LOG_BUG_FIXED"
Write-Output "LOAD_TEST_NOT_PERFORMED_BY_THIS_SCRIPT"
Write-Output "PUBLIC_PLAYABLE_CLAIM_ALLOWED=false"
Write-Output "Done."
