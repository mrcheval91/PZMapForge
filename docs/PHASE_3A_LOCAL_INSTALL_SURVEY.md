# Phase 3A: Local Project Zomboid Install Survey

Date: 2026-06-01
Baseline commit: 6529000
Status: OPERATOR ACTION REQUIRED -- automated discovery found no install

---

## Claim boundary

planning_artifact_only_not_pz_load_tested

All Phase 3 work produces local planning artifacts only.
No output is a playable Project Zomboid export.

---

## Purpose

Before any Phase 3A code is written, the operator must document:
1. The local PZ install path and directory layout.
2. What tile-related files are present and their format.
3. Which tile names correspond to the semantic planning kinds.

This document provides survey commands to gather that evidence.
The survey is read-only. Nothing is copied, committed, or modified.

---

## What to inspect

| Area | What to find |
|---|---|
| Install root | Does the directory exist? What build version? |
| media/ | What subdirectories are present? |
| media/tiles/ | File types present: .pack, .tiles, .png, other |
| Tilesheet count | How many tilesheet files? |
| Tile naming | Do file or sheet names suggest semantic meaning (ground, floor, road)? |
| media/maps/ | Verify PZMapForge never writes here (check it is read-only from tool) |

---

## Survey output directory

Survey results go to .local/pzmapforge/surveys/ (gitignored).
Never paste paths, file lists, or asset details into committed docs.
Committed docs use placeholders: [PZ_INSTALL_ROOT], [tiles_count], etc.

---

## Latest automated survey status

Last run: 2026-06-01

Automated discovery was attempted using scripts/Run-Phase3ALocalPzSurvey.ps1.

Result: No Project Zomboid installation was found at the searched default paths.

    C:\Program Files (x86)\Steam\steamapps\common\ProjectZomboid  -- not found
    D:\SteamLibrary\steamapps\common\ProjectZomboid               -- not found
    E:\SteamLibrary\steamapps\common\ProjectZomboid               -- not found
    F:\SteamLibrary\steamapps\common\ProjectZomboid               -- not found

Survey output files were written to .local/pzmapforge/surveys/ (gitignored).
No local paths, tilesheet names, tile IDs, or GIDs are committed.

### Phase 3 implementation status

NOT STARTED. Must not begin until this survey is completed.

### Required operator action

1. Locate the Project Zomboid install path using Steam:
   Library > Project Zomboid > right-click > Manage > Browse local files

2. Re-run the survey helper with the explicit path:

       powershell -ExecutionPolicy Bypass `
           -File "scripts\Run-Phase3ALocalPzSurvey.ps1" `
           -PzRoot "<your PZ install path>"

3. Review .local/pzmapforge/surveys/pz-install-survey-redacted-latest.md
   after the re-run. It will report yes/no flags without exposing paths.

4. Manually verify which tile sheets correspond to which semantic planning kinds
   (grass, road, sidewalk, row_house, depanneur, garage, industrial_yard,
   landmark, spawn). This step cannot be automated.

5. Write docs/PHASE_3A_DECISION.md using the placeholder table from the
   "What to paste back" section below. Use placeholders only -- no real paths,
   no tilesheet file names, no GIDs.

6. After docs/PHASE_3A_DECISION.md is committed, Slice 3A-1 (local config
   schema and loader, no real PZ install required for tests) can begin.

Do not commit .local/pzmapforge/surveys/ outputs.
Do not write Phase 3 implementation code before step 5 is complete.

---

## Claude-assisted survey helper

A helper script automates the read-only portions of this survey.

### How to run

    powershell -ExecutionPolicy Bypass -File "scripts\Run-Phase3ALocalPzSurvey.ps1"

If PZ is not in a standard Steam location, pass the path explicitly:

    powershell -ExecutionPolicy Bypass -File "scripts\Run-Phase3ALocalPzSurvey.ps1" `
        -PzRoot "D:\SteamLibrary\steamapps\common\ProjectZomboid"

### What the script automates

- Searches common Steam paths for a PZ installation.
- Checks whether media/ and media/tiles/ exist.
- Counts files by extension, bucketed (no exact counts in redacted output).
- Searches tilesheet file names for semantic kind keywords (grass, road, etc.).
- Probes build version from .bat files (read-only).
- Confirms repo media/maps/ is untouched.
- Writes two local survey files to .local/pzmapforge/surveys/:
    pz-install-survey-latest.txt            (full, with local paths)
    pz-install-survey-redacted-latest.md    (redacted, yes/no and buckets only)

### What the script cannot decide

- Whether a tilesheet name actually maps to a given semantic kind.
  (Names may hint at meaning but cannot be confirmed without visual inspection.)
- Whether the discovered tile GID range is correct for the target build.
- Whether the build version is 41 or 42.
  (May require manual inspection of the exe or Steam manifest.)
- The operator must still fill in the placeholder table and write PHASE_3A_DECISION.md.

### What the operator must still verify

After running the script:
1. Check the redacted report at .local/pzmapforge/surveys/pz-install-survey-redacted-latest.md.
2. Manually verify which tile sheets visually represent each semantic kind.
3. Note approximate GID ranges per kind (do not commit exact GIDs).
4. Fill in the placeholder table (see "What to paste back" section below).
5. Write docs/PHASE_3A_DECISION.md using the placeholder table only.

### Notes

- Do NOT commit either survey file.
- Do NOT paste local paths or tilesheet names into committed docs.
- The .local/ directory is already gitignored.

---

## Step 1: Locate the Project Zomboid install

Run each block in PowerShell from the repo root.

Check common Steam library paths:

    $steamRoots = @(
        'C:\Program Files (x86)\Steam\steamapps\common\ProjectZomboid',
        'D:\SteamLibrary\steamapps\common\ProjectZomboid',
        'E:\SteamLibrary\steamapps\common\ProjectZomboid',
        'F:\SteamLibrary\steamapps\common\ProjectZomboid'
    )
    foreach ($p in $steamRoots) {
        if (Test-Path $p) { Write-Output "FOUND: $p" }
        else              { Write-Output "not found: $p" }
    }

If none found, locate manually:

    # Open Steam -> Library -> Project Zomboid -> right-click -> Manage -> Browse local files
    # Then paste the path here for the next steps:
    $pzRoot = '<your PZ install path>'

---

## Step 2: Check the build version

    $versionFile = Join-Path $pzRoot 'ProjectZomboid64.bat'
    if (Test-Path $versionFile) { Get-Content $versionFile | Select-String 'version' }

    # Or look for a version file:
    Get-ChildItem $pzRoot -Filter 'version*' -ErrorAction SilentlyContinue
    Get-ChildItem $pzRoot -Filter '*.txt' -ErrorAction SilentlyContinue |
        Where-Object { $_.Name -match 'version|build' }

---

## Step 3: List the media directory

    $mediaDir = Join-Path $pzRoot 'media'
    if (Test-Path $mediaDir) {
        Get-ChildItem $mediaDir -Directory |
            Select-Object Name, LastWriteTime |
            Format-Table -AutoSize
    } else {
        Write-Output "media/ directory not found under $pzRoot"
    }

---

## Step 4: Survey the tiles directory

    $tilesDir = Join-Path $pzRoot 'media\tiles'
    if (Test-Path $tilesDir) {
        Write-Output "tiles/ exists"
        Write-Output ""
        Write-Output "--- File type counts ---"
        Get-ChildItem $tilesDir -Recurse -File -ErrorAction SilentlyContinue |
            Group-Object Extension |
            Sort-Object Count -Descending |
            Select-Object Name, Count |
            Format-Table -AutoSize
        Write-Output ""
        Write-Output "--- Total files ---"
        (Get-ChildItem $tilesDir -Recurse -File -ErrorAction SilentlyContinue).Count
    } else {
        Write-Output "media/tiles/ not found under $pzRoot"
        Write-Output "Try listing media/ subdirectories again."
    }

---

## Step 5: Sample tilesheet names

List the first 30 tilesheet file names (not contents):

    Get-ChildItem (Join-Path $pzRoot 'media\tiles') -Recurse -File |
        Select-Object -First 30 |
        Select-Object Name, Length |
        Format-Table -AutoSize

Note: names only. Do not open or read file contents during the survey.

---

## Step 6: Search for other asset types

    $extensions = @('*.pack', '*.tiles', '*.lotheader', '*.lotpack', '*.bin')
    foreach ($ext in $extensions) {
        $count = @(Get-ChildItem $pzRoot -Recurse -Filter $ext -ErrorAction SilentlyContinue).Count
        Write-Output ("  {0,-14} {1,6} files" -f $ext, $count)
    }

---

## Step 7: Look for tile naming conventions

Search tilesheet file names for semantic clues (no file content opened):

    $tilesDir = Join-Path $pzRoot 'media\tiles'
    $keywords = @('ground', 'floor', 'road', 'wall', 'grass', 'nature',
                  'building', 'structure', 'furniture', 'vehicle')
    foreach ($kw in $keywords) {
        $matches = @(Get-ChildItem $tilesDir -Recurse -File -Filter "*$kw*" `
                        -ErrorAction SilentlyContinue)
        if ($matches.Count -gt 0) {
            Write-Output "keyword '$kw': $($matches.Count) file(s)"
            $matches | Select-Object -First 3 | ForEach-Object { Write-Output "  $($_.Name)" }
        }
    }

---

## Step 8: Capture survey output

Run all the above steps and redirect to a local survey file:

    $surveyDir = Join-Path (Get-Location) '.local\pzmapforge\surveys'
    New-Item -ItemType Directory -Force -Path $surveyDir | Out-Null
    $surveyFile = Join-Path $surveyDir ("pz-install-survey-{0:yyyyMMdd}.txt" -f (Get-Date))

    # Paste or pipe the output of steps 1-7 into $surveyFile, for example:
    "Survey date: $(Get-Date)" | Out-File $surveyFile -Encoding UTF8 -Append
    "PZ root: $pzRoot"         | Out-File $surveyFile -Encoding UTF8 -Append

The survey file is gitignored (.local/ is already in .gitignore).
Do not copy the survey file contents into any committed document.

---

## What to paste back

After running the survey, update the following table in a PHASE_3A_DECISION.md
(committed doc using placeholders, not real paths or asset names):

    | Field | Value |
    |---|---|
    | PZ install found | yes / no |
    | Build version | [e.g. Build 41.78 or Build 42.x] |
    | media/tiles present | yes / no |
    | Tilesheet format | [.tiles / .pack / both / other] |
    | Tilesheet count | [approximate: 0-50 / 50-200 / 200+] |
    | Naming conventions | [e.g. "sheets named by material/kind"] |
    | Grass-equivalent tile found | yes / no |
    | Road-equivalent tile found | yes / no |
    | Semantic kind coverage | [fraction of 9 kinds matched] |

Use ONLY the placeholders above. Do not write actual paths, GIDs, or sheet names
into any committed document.

---

## What NOT to copy

Do not copy or commit:
- The PZ install path
- Tilesheet file contents
- Tile GIDs or pixel data
- Any extracted asset metadata beyond the placeholder table above
- Any .pack, .tiles, .png, or .bin file

---

## What NOT to commit

Do not commit:
- .local/pzmapforge/surveys/ (gitignored)
- .local/pzmapforge/pz-install-config.json (future, gitignored)
- Any file containing real local paths
- Any file containing PZ asset data

---

## Next decision gate

After completing the survey, write:
  docs/PHASE_3A_DECISION.md

That document must include:
1. Survey results (placeholder table only, no real paths)
2. Confirmation that all 5 precondition evidence requirements are met
   (from docs/PHASE_3_LOCAL_PZ_CONFIG_SPEC.md)
3. Tile naming convention spec: which tiles map to which semantic kinds
4. Decision: proceed with Slice 3A-1 or defer

Until docs/PHASE_3A_DECISION.md is committed, no Phase 3 code is written.
