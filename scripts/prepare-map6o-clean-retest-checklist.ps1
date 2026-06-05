#Requires -Version 5.1
<#
.SYNOPSIS
    MAP-6O: Prepares a clean isolated retest checklist for the Build 42
    candidate (pzmapforge_build42_candidate_001).

    Validates the MAP-6L/MAP-6M candidate source under .local/, then writes:
      MAP_6O_CLEAN_RETEST_CHECKLIST.md
      MAP_6O_CLEAN_RETEST_RECORD.local-template.md
      MAP_6O_CLEAN_RETEST_TRIAGE_COMMANDS.md

    Both -CandidateSource and -Output must be under .local/.
    Refuses all Zomboid/mods/Workshop/Server/PZ-install paths.
    Does NOT copy any files to PZ folders.

.PARAMETER CandidateSource
    Path under .local/ containing the MAP-6L/MAP-6M candidate output
    (the directory that contains the 42/ versioned layout).

.PARAMETER Output
    Path under .local/ for checklist output.

.PARAMETER ModFolderName
    Name of the test mod folder the operator will create manually in PZ mods.
    Default: pzmapforge_build42_candidate_001_test_clean

.PARAMETER ServerName
    Name of the isolated PZ server preset to create for the test.
    Default: PZMF_B42_CANDIDATE_CLEAN_001

.EXAMPLE
    powershell -ExecutionPolicy Bypass -File scripts\prepare-map6o-clean-retest-checklist.ps1 `
        -CandidateSource .local\candidate\pzmapforge_build42_candidate_001_build42_candidate `
        -Output .local\map6o-checklist
#>

param(
    [Parameter(Mandatory=$true)][string]$CandidateSource,
    [Parameter(Mandatory=$true)][string]$Output,
    [string]$ModFolderName = 'pzmapforge_build42_candidate_001_test_clean',
    [string]$ServerName    = 'PZMF_B42_CANDIDATE_CLEAN_001'
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$CandidateId = 'pzmapforge_build42_candidate_001'

# ---------------------------------------------------------------------------
# Path guards
# ---------------------------------------------------------------------------

function Assert-LocalPath {
    param([string]$Path, [string]$Label)
    $norm = $Path.Replace('/', '\')
    if ($norm -notmatch '\\\.local(\\|$)') {
        Write-Error "$Label must be under .local/. Got: $Path"
        exit 1
    }
}

function Assert-NotForbiddenPath {
    param([string]$Path, [string]$Label)
    $norm = $Path.Replace('/', '\').ToLower()
    $forbidden = @('zomboid\mods', 'zomboid\workshop', 'zomboid\server', 'project zomboid', 'steamapps\common')
    foreach ($f in $forbidden) {
        if ($norm -match [regex]::Escape($f)) {
            Write-Error "$Label contains a forbidden path segment '$f'. Refusing: $Path"
            exit 1
        }
    }
}

Assert-LocalPath $CandidateSource '-CandidateSource'
Assert-LocalPath $Output '-Output'
Assert-NotForbiddenPath $CandidateSource '-CandidateSource'
Assert-NotForbiddenPath $Output '-Output'

# ---------------------------------------------------------------------------
# Verify candidate source files
# ---------------------------------------------------------------------------

$v42Dir     = Join-Path $CandidateSource '42'
$mapDataDir = Join-Path $v42Dir "media\maps\$CandidateId"

$requiredFiles = @(
    (Join-Path $v42Dir     'mod.info');
    (Join-Path $mapDataDir 'map.info');
    (Join-Path $mapDataDir 'spawnpoints.lua');
    (Join-Path $mapDataDir 'objects.lua');
    (Join-Path $mapDataDir '0_0.lotheader');
    (Join-Path $mapDataDir 'world_0_0.lotpack');
    (Join-Path $mapDataDir 'chunkdata_0_0.bin')
)

$missing = @()
foreach ($f in $requiredFiles) {
    if (-not (Test-Path -LiteralPath $f)) { $missing += $f }
}

if ($missing.Count -gt 0) {
    Write-Error "Candidate source is missing required files:`n$($missing -join "`n")"
    exit 1
}

Write-Output "Candidate source validated: $CandidateSource"
Write-Output "  Required files: $($requiredFiles.Count) found."

New-Item -ItemType Directory -Force -Path $Output | Out-Null

$destBase = "C:\Users\Palmacede\Zomboid\mods\$ModFolderName\42"

# ---------------------------------------------------------------------------
# MAP_6O_CLEAN_RETEST_CHECKLIST.md
# ---------------------------------------------------------------------------

$checklistPath = Join-Path $Output 'MAP_6O_CLEAN_RETEST_CHECKLIST.md'

$checklist = @"
# MAP-6O Clean Isolated Retest Checklist

```text
Candidate:  $CandidateId
ModFolder:  $ModFolderName
ServerName: $ServerName
Generated:  $(Get-Date -Format 'yyyy-MM-ddTHH:mm:ssZ')
HUMAN_ONLY_COPY_REQUIRED
LOAD_TEST_NOT_PERFORMED
PLAYABLE_EXPORT_CLAIM_ALLOWED=false
```

---

## Pre-Clean (HUMAN ONLY - do not automate)

- [ ] **Delete old test mod folders** (run manually in PowerShell or Explorer):
  ```
  HUMAN-ONLY: Remove-Item -Recurse -Force 'C:\Users\Palmacede\Zomboid\mods\pzmapforge_manual_b42_001_maptest_a' -ErrorAction SilentlyContinue
  HUMAN-ONLY: Remove-Item -Recurse -Force 'C:\Users\Palmacede\Zomboid\mods\pzmapforge_manual_b42_001_maptest_b' -ErrorAction SilentlyContinue
  HUMAN-ONLY: Remove-Item -Recurse -Force 'C:\Users\Palmacede\Zomboid\mods\pzmapforge_manual_b42_001_maptest_c' -ErrorAction SilentlyContinue
  ```
  Verify no `pzmapforge_manual_*` folders remain in `C:\Users\Palmacede\Zomboid\mods\`.

- [ ] **Disable unrelated mods** in PZ Mod Manager before the test.
  Only `$CandidateId` should be active.

- [ ] **Delete stale console.txt** before launching PZ:
  ```
  HUMAN-ONLY: Remove-Item -Force 'C:\Users\Palmacede\Zomboid\console.txt' -ErrorAction SilentlyContinue
  ```

- [ ] **Create isolated server preset** named `$ServerName` with default settings.
  Do not reuse a world created with previous pzmapforge test mods.

---

## Install (HUMAN ONLY - this script does NOT copy to PZ)

SOURCE (from this .local candidate output):
  $CandidateSource\42\

DESTINATION (copy manually):
  $destBase\

**Exact copy command (HUMAN-ONLY - run this yourself):**
```
HUMAN-ONLY: Copy-Item -Recurse -Force '$($CandidateSource)\42' '$($destBase | Split-Path -Parent)'
```

After copy, verify these files exist:
  $destBase\mod.info
  $destBase\media\maps\$CandidateId\map.info
  $destBase\media\maps\$CandidateId\spawnpoints.lua
  $destBase\media\maps\$CandidateId\objects.lua
  $destBase\media\maps\$CandidateId\0_0.lotheader
  $destBase\media\maps\$CandidateId\world_0_0.lotpack
  $destBase\media\maps\$CandidateId\chunkdata_0_0.bin

Also place `spawnregions.lua` from the MAP-6M packet at:
  $destBase\media\maps\$CandidateId\spawnregions.lua

Preflight verify (PowerShell, human-run):
```powershell
Test-Path '$destBase\mod.info'
Test-Path '$destBase\media\maps\$CandidateId\0_0.lotheader'
Test-Path '$destBase\media\maps\$CandidateId\world_0_0.lotpack'
Test-Path '$destBase\media\maps\$CandidateId\chunkdata_0_0.bin'
```
All must return True.

---

## Test Sequence (HUMAN ONLY)

1. Launch Project Zomboid Build 42 fresh.
2. Go to Mods. Confirm `$CandidateId` appears.
3. Enable it. Disable all other mods.
4. Does PZ crash or return to menu at mod selection?
   - YES -> record MOD_SELECTION_CRASH; stop; copy console.txt immediately.
   - NO  -> record MOD_SELECTION_PASS; continue.
5. Navigate to Host (solo) and select `$ServerName`.
6. Start. Does a spawn selection screen appear with the candidate region?
   - YES -> record SPAWN_REGION_VISIBLE; continue.
   - NO  -> record SPAWN_REGION_NOT_VISIBLE; continue anyway.
7. Attempt to enter the world.
8. Does world loading start?
   - YES -> record WORLD_LOAD_STARTED.
   - NO / crash -> note the first error visible.
9. Stop after first unrecoverable error or after successful world entry.

Do not modify PZ files during the test. Do not rerun the PZMapForge writer.

---

## Post-Test Log Capture (HUMAN ONLY)

Immediately after the test:
```
HUMAN-ONLY: Copy-Item 'C:\Users\Palmacede\Zomboid\console.txt' '.local\map6o-logs\console-map6o-<TIMESTAMP>.txt'
```
Copy the full file before launching PZ again (subsequent launches overwrite it).

---

## Log Triage (PZMapForge tool - safe to run)

After copying the fresh log to .local/:
```powershell
powershell -ExecutionPolicy Bypass -File scripts\extract-map6n-current-candidate-log-evidence.ps1 ``
    -InputLogFolder .local\map6o-logs ``
    -Output .local\map6o-triage
```
Outputs: map6n-log-triage-report.json with result_recommendation.

---

## Safety

- HUMAN_ONLY_COPY_REQUIRED: no automatic copy to PZ folders.
- LOAD_TEST_NOT_PERFORMED: this checklist does not perform the test.
- PLAYABLE_EXPORT_CLAIM_ALLOWED=false: binding.
- pzmapforge_build42_candidate_001: candidate under test.
"@

Set-Content -Path $checklistPath -Value $checklist -Encoding UTF8
Write-Output "Checklist: $checklistPath"

# ---------------------------------------------------------------------------
# MAP_6O_CLEAN_RETEST_RECORD.local-template.md
# ---------------------------------------------------------------------------

$recordPath = Join-Path $Output 'MAP_6O_CLEAN_RETEST_RECORD.local-template.md'

$record = @"
# MAP-6O Clean Retest Record

```text
Candidate:  $CandidateId
ModFolder:  $ModFolderName
ServerName: $ServerName
Date:       [FILL IN]
PZ version: [FILL IN e.g. Build 42.0.4]
Operator:   [FILL IN]
```

## Pre-Clean Confirmation

- old_test_folders_removed:    [ yes / no ]
- unrelated_mods_disabled:     [ yes / no ]
- stale_console_txt_deleted:   [ yes / no ]
- fresh_server_preset_created: [ yes / no ]

## Install Verification

- mod.info present:         [ yes / no ]
- map.info present:         [ yes / no ]
- spawnpoints.lua present:  [ yes / no ]
- objects.lua present:      [ yes / no ]
- spawnregions.lua present: [ yes / no ]
- lotheader present:        [ yes / no ]
- lotpack present:          [ yes / no ]
- chunkdata present:        [ yes / no ]

## Test Observations

- mod_selection_crash:              [ yes / no ]
- mod_selection_pass:               [ yes / no ]
- spawn_region_visible:             [ yes / no ]
- world_load_started:               [ yes / no ]
- first_error_message:              [FILL IN or "none"]

## Triage Tool Result

- current_candidate_matches:        [FILL IN integer]
- stale_maptest_a_matches:          [FILL IN integer]
- candidate_specific_exception_found: [ yes / no ]
- result_recommendation:            [LOAD_TEST_INCONCLUSIVE / CURRENT_CANDIDATE_EXCEPTION_FOUND]

## Final Result

- result: [ LOAD_TEST_PASS / LOAD_TEST_FAIL / LOAD_TEST_INCONCLUSIVE ]

Notes:
[FILL IN]

---

LOAD_TEST_INCONCLUSIVE — status until this template is completed with evidence.
PLAYABLE_EXPORT_CLAIM_ALLOWED=false
"@

Set-Content -Path $recordPath -Value $record -Encoding UTF8
Write-Output "Record template: $recordPath"

# ---------------------------------------------------------------------------
# MAP_6O_CLEAN_RETEST_TRIAGE_COMMANDS.md
# ---------------------------------------------------------------------------

$triageCmdsPath = Join-Path $Output 'MAP_6O_CLEAN_RETEST_TRIAGE_COMMANDS.md'

$triageCmds = @"
# MAP-6O Triage Commands Reference

All commands below are safe to run (read .local/ only; no PZ writes).

## 1. Run log triage after copying fresh console.txt to .local\map6o-logs\

```powershell
powershell -ExecutionPolicy Bypass -File scripts\extract-map6n-current-candidate-log-evidence.ps1 ``
    -InputLogFolder .local\map6o-logs ``
    -Output .local\map6o-triage
```

Output: .local\map6o-triage\map6n-log-triage-report.json

## 2. Inspect triage JSON result

```powershell
Get-Content .local\map6o-triage\map6n-log-triage-report.json | ConvertFrom-Json
```

## 3. Validate candidate source preflight

```powershell
powershell -ExecutionPolicy Bypass -File scripts\prepare-build42-candidate-load-test-packet.ps1 ``
    -Source '$CandidateSource' ``
    -Output .local\map6o-preflight
```

## 4. Key triage fields to check

| Field | Desired for PASS |
|---|---|
| current_candidate_matches | >= 1 |
| stale_maptest_a_matches | any (excluded from result) |
| candidate_specific_exception_found | false |
| result_recommendation | LOAD_TEST_INCONCLUSIVE (no exception) |

---

PLAYABLE_EXPORT_CLAIM_ALLOWED=false
LOAD_TEST_NOT_PERFORMED
"@

Set-Content -Path $triageCmdsPath -Value $triageCmds -Encoding UTF8
Write-Output "Triage commands: $triageCmdsPath"

Write-Output ""
Write-Output "MAP-6O checklist packet written to: $Output"
Write-Output "Files:"
Write-Output "  MAP_6O_CLEAN_RETEST_CHECKLIST.md"
Write-Output "  MAP_6O_CLEAN_RETEST_RECORD.local-template.md"
Write-Output "  MAP_6O_CLEAN_RETEST_TRIAGE_COMMANDS.md"
Write-Output ""
Write-Output "NOTE: No files copied to PZ. Human operator required for all install steps."
Write-Output "LOAD_TEST_NOT_PERFORMED"
Write-Output "PLAYABLE_EXPORT_CLAIM_ALLOWED=false"
Write-Output "Done."
