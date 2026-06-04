#Requires -Version 5.1
<#
.SYNOPSIS
    Prepares a local-only MAP-5B load test packet from a MAP-5A experimental output.

    Validates the MAP-5A source output, reads the report JSON, and writes a
    load-test instructions packet and a fillable record template under .local only.

    Does NOT copy mod files to the PZ mods folder.
    Does NOT touch the PZ install directory.
    Does NOT touch repo media/maps.
    Does NOT copy PZ assets.
    Does NOT claim playable export.
    Output must be under .local only.

Usage:
    .\scripts\prepare-map-export-experimental-load-test.ps1 `
        -Source <.local map-export-experimental output dir> `
        -Output <.local load-test packet output dir>

Example:
    .\scripts\prepare-map-export-experimental-load-test.ps1 `
        -Source ".local\map-export-experimental\empty-cell-01" `
        -Output ".local\load-tests\empty-cell-01"
#>

param(
    [Parameter(Mandatory=$true)]
    [string]$Source,

    [Parameter(Mandatory=$true)]
    [string]$Output
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

Write-Output 'prepare-map-export-experimental-load-test.ps1'
Write-Output "Source: $Source"
Write-Output "Output: $Output"
Write-Output ''

# ---------------------------------------------------------------------------
# Guard: Output must be under .local
# ---------------------------------------------------------------------------

$outputFull  = [System.IO.Path]::GetFullPath($Output)
$sep         = [System.IO.Path]::DirectorySeparatorChar
$localMarker = $sep + '.local' + $sep
$endsLocal   = $outputFull.EndsWith($sep + '.local')

if (-not ($outputFull.Contains($localMarker) -or $endsLocal)) {
    Write-Error "prepare-map-export-experimental-load-test: refusing to write outside a .local/ directory: $outputFull"
    Write-Error "  Pass -Output to an explicit .local/ path."
    exit 1
}

# ---------------------------------------------------------------------------
# Guard: Source must exist
# ---------------------------------------------------------------------------

$sourceFull = [System.IO.Path]::GetFullPath($Source)

if (-not (Test-Path -LiteralPath $sourceFull)) {
    Write-Error "Source path not found: $sourceFull"
    exit 1
}

# ---------------------------------------------------------------------------
# Read and validate the experimental report JSON
# ---------------------------------------------------------------------------

Write-Output '--- Validating MAP-5A report ---'

$reportJsonPath = Join-Path $sourceFull 'experimental-map-export-report.json'
if (-not (Test-Path -LiteralPath $reportJsonPath)) {
    Write-Error "Missing experimental-map-export-report.json in source: $sourceFull"
    exit 1
}

$report = Get-Content -LiteralPath $reportJsonPath -Raw | ConvertFrom-Json

# Verify safety fields
if ($report.playable_export_generated -ne $false) {
    Write-Error "Source report has playable_export_generated=true. Refusing to prepare load test for an invalid source."
    exit 1
}
Write-Output "OK: playable_export_generated=false"

if ($report.load_tested -ne $false) {
    Write-Error "Source report has load_tested=true. This output may already have a load test record."
    exit 1
}
Write-Output "OK: load_tested=false"

if ($report.experimental_writer -ne $true) {
    Write-Error "Source report does not have experimental_writer=true. Source may not be a MAP-5A output."
    exit 1
}
Write-Output "OK: experimental_writer=true"

$mapId    = $report.map_id
$cellX    = $report.cell_x
$cellY    = $report.cell_y
$cellCoord = "${cellX}_${cellY}"

Write-Output "  map_id:     $mapId"
Write-Output "  cell:       ($cellX, $cellY)"
Write-Output "  generated:  $($report.generated_at_utc)"
Write-Output ''

# ---------------------------------------------------------------------------
# Verify expected files exist in source
# ---------------------------------------------------------------------------

Write-Output '--- Verifying expected source files ---'

$mapDir = Join-Path $sourceFull "media\maps\$mapId"

$expectedFiles = @(
    (Join-Path $sourceFull 'mod.info')
    (Join-Path $mapDir 'map.info')
    (Join-Path $mapDir 'spawnpoints.lua')
    (Join-Path $mapDir 'objects.lua')
    (Join-Path $mapDir 'README_PZMAPFORGE_BOUNDARY_EXPERIMENTAL.txt')
    (Join-Path $mapDir "${cellCoord}.lotheader")
    (Join-Path $mapDir "world_${cellCoord}.lotpack")
    (Join-Path $mapDir "chunkdata_${cellCoord}.bin")
)

$allPresent = $true
foreach ($fp in $expectedFiles) {
    if (Test-Path -LiteralPath $fp) {
        $size = (Get-Item -LiteralPath $fp).Length
        Write-Output ("  OK  {0,-60} {1,8} bytes" -f ($fp.Replace($sourceFull, '').TrimStart($sep) -replace '\\', '/'), $size)
    } else {
        Write-Output "  MISSING: $($fp.Replace($sourceFull, '').TrimStart($sep) -replace '\\', '/')"
        $allPresent = $false
    }
}

if (-not $allPresent) {
    Write-Error "Source is missing expected files. Run map-export-experimental first."
    exit 1
}

Write-Output ''

# ---------------------------------------------------------------------------
# Write load test packet
# ---------------------------------------------------------------------------

New-Item -ItemType Directory -Force -Path $outputFull | Out-Null

$generatedAt  = (Get-Date).ToUniversalTime().ToString('yyyy-MM-ddTHH:mm:ssZ')
$modsDestPath = "C:\Users\Palmacede\Zomboid\mods\$mapId"

$packetContent = @"
# MAP-5B Load Test Packet

Generated:  $generatedAt
Source:     $($sourceFull -replace '\\', '/')
Map ID:     $mapId
Cell:       ($cellX, $cellY)

## Claim boundary

experimental_local_only_not_playable_not_load_tested

This output is experimental and hypothesis-only.
Not a playable Project Zomboid map. Not load-tested.
No playable export claim.

## Step-by-step load test instructions

### Step 1: locate the MAP-5A output folder

Source folder (already verified):
$($sourceFull -replace '\\', '/')

All expected files are present.

### Step 2: copy the mod folder to your PZ user mods directory

Copy the ENTIRE contents of the source folder to:
$modsDestPath\

The destination folder must be named exactly: $mapId

Example (PowerShell — run manually, NOT automatic):

    Copy-Item -Recurse -Force ``
        "$($sourceFull.TrimEnd($sep))" ``
        "$modsDestPath"

Do NOT copy to the PZ install directory (steamapps/common/...).
Use ONLY the user Zomboid folder.

### Step 3: launch Project Zomboid

1. Start Project Zomboid.
2. Go to Mods menu.
3. Enable the mod named: $mapId
4. Start a new Sandbox game.

### Step 4: observe and record

Observe and note:
- Does "$mapId" appear in the Mods list?
- Does a map location appear in the spawn/map selection?
- Does the game start without crashing?
- Is the experimental cell visible on the in-game map?
- Can you spawn in the cell?
- What happens when you enter the cell area?
- Check for errors in: C:\Users\Palmacede\Zomboid\Logs\

### Step 5: fill in the record template

Open and fill:
$($outputFull -replace '\\', '/')/MAP_5B_LOAD_TEST_RECORD.local-template.md

Record your result as:
  LOAD_TEST_PASS         -- mod loads, cell accessible, no crash
  LOAD_TEST_FAIL         -- mod fails to load, cell missing, or crash
  LOAD_TEST_INCONCLUSIVE -- partial; some features work but not all

### Step 6: clean up

After the test, you may delete the mod from the mods folder.
Do not leave experimental mods in your PZ mods folder permanently.

## Binary hypotheses being tested

| File | Size | Hypothesis |
|---|---|---|
| ${cellCoord}.lotheader | 8 bytes | Zero header + 0-entry tileset list |
| world_${cellCoord}.lotpack | 7208 bytes | hdrA=900, hdrB=7204, all-zero chunk offsets |
| chunkdata_${cellCoord}.bin | 902 bytes | 0x0001 header + 900 zero-byte chunk grid |

## Non-claims

- No playable export claim until LOAD_TEST_PASS is recorded and reviewed.
- This packet does not perform the load test automatically.
- No PZ install was modified by generating this packet.
- No PZ assets were copied.
- Output is under .local only.
"@

$packetPath = Join-Path $outputFull 'MAP_5B_LOAD_TEST_PACKET.md'
Set-Content -Path $packetPath -Value $packetContent -Encoding UTF8
Write-Output "Load test packet: $packetPath"

# ---------------------------------------------------------------------------
# Write fillable record template
# ---------------------------------------------------------------------------

$templateContent = @"
# MAP-5B Load Test Record

Schema:           pzmapforge.load-test-record.v0.1
Claim boundary:   experimental_local_only_not_playable_not_load_tested
Source:           $($sourceFull -replace '\\', '/')
Map ID:           $mapId
Cell:             ($cellX, $cellY)
Packet generated: $generatedAt

---

## Test metadata

| Field | Value |
|---|---|
| Date / time | <!-- YYYY-MM-DD HH:MM --> |
| Tester | <!-- operator name --> |
| PZ version | <!-- e.g. Build 41.78 or Build 42.x --> |
| PZ install path | <!-- local path --> |
| Copied-to path | $modsDestPath |
| map_id | $mapId |
| Cell coordinates | ($cellX, $cellY) |

---

## Safety confirmation

| Property | Value |
|---|---|
| Mod files copied from .local only | <!-- yes --> |
| PZ install directory modified | <!-- no (must be no) --> |
| Repo media/maps touched | <!-- no (must be no) --> |
| PZ assets copied into repo | <!-- no (must be no) --> |

---

## Observation checklist

| Observation | Result | Notes |
|---|---|---|
| Mod appears in PZ mod list | <!-- yes / no --> | |
| Map location appears in spawn/map selection | <!-- yes / no / unknown --> | |
| Game starts without crashing | <!-- yes / no --> | |
| PZ crashed during load | <!-- yes / no --> | |
| Experimental cell visible on in-game map | <!-- yes / no / unknown --> | |
| Player can spawn in the experimental cell | <!-- yes / no / unknown --> | |
| Cell appears blank (no tiles, no terrain) | <!-- yes / no / unknown --> | |
| Error messages observed | <!-- yes / no --> | |

---

## Error messages and log excerpts

``````
<!-- Paste relevant PZ log lines here. No binary content. -->
``````

---

## Observed behavior

<!-- Describe what happened. Be specific. -->

---

## Result

``````
RESULT: <!-- LOAD_TEST_PASS / LOAD_TEST_FAIL / LOAD_TEST_INCONCLUSIVE -->
``````

| Property | Value |
|---|---|
| playable_claim_allowed | false — not until result reviewed |
| load_tested | true |
| load_test_result | <!-- PASS / FAIL / INCONCLUSIVE --> |
| pz_version_tested | <!-- fill in --> |

---

## Notes and next steps

<!-- What to investigate next? -->
"@

$templatePath = Join-Path $outputFull 'MAP_5B_LOAD_TEST_RECORD.local-template.md'
Set-Content -Path $templatePath -Value $templateContent -Encoding UTF8
Write-Output "Record template:  $templatePath"

# ---------------------------------------------------------------------------
# Summary
# ---------------------------------------------------------------------------

Write-Output ''
Write-Output "========================================"
Write-Output "MAP-5B load test packet ready"
Write-Output "========================================"
Write-Output "  Map ID:        $mapId"
Write-Output "  Cell:          ($cellX, $cellY)"
Write-Output "  Packet:        $packetPath"
Write-Output "  Template:      $templatePath"
Write-Output "  Mods dest:     $modsDestPath"
Write-Output ''
Write-Output "  Next steps:"
Write-Output "  1. Read MAP_5B_LOAD_TEST_PACKET.md for copy instructions."
Write-Output "  2. Manually copy mod folder to PZ mods directory."
Write-Output "  3. Perform the load test."
Write-Output "  4. Fill in MAP_5B_LOAD_TEST_RECORD.local-template.md."
Write-Output ''
Write-Output "  playable_export_claimed:     false"
Write-Output "  pz_assets_copied:            false"
Write-Output "  media_maps_touched_in_repo:  false"
Write-Output "  Status:                      OK"
