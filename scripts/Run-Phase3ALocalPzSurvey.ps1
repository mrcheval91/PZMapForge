#Requires -Version 5.1
<#
.SYNOPSIS
    Read-only Phase 3A local PZ install survey helper for PZMapForge.

    Searches for a local Project Zomboid installation, inventories the tile
    directory layout, and writes two local survey files:

      .local/pzmapforge/surveys/pz-install-survey-latest.txt   (full, local only)
      .local/pzmapforge/surveys/pz-install-survey-redacted-latest.md  (redacted)

    Both files are gitignored (.local/ is in .gitignore).
    Do NOT commit either file.

    The redacted markdown uses bucketed counts and yes/no flags only.
    No exact local paths, tilesheet names, or GIDs appear in the redacted file.

    Does not copy, open, modify, or redistribute any PZ asset.
    Does not write to media/maps.
    Claim boundary: planning_artifact_only_not_pz_load_tested

.PARAMETER PzRoot
    Optional explicit path to a Project Zomboid installation directory.
    If omitted, the script searches common Steam library locations.

.EXAMPLE
    powershell -ExecutionPolicy Bypass -File ".\scripts\Run-Phase3ALocalPzSurvey.ps1"

.EXAMPLE
    powershell -ExecutionPolicy Bypass -File ".\scripts\Run-Phase3ALocalPzSurvey.ps1" `
        -PzRoot "D:\SteamLibrary\steamapps\common\ProjectZomboid"
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory = $false)]
    [string] $PzRoot = ''
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$scriptDir  = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot   = Split-Path -Parent $scriptDir
$surveyDir  = Join-Path $repoRoot '.local\pzmapforge\surveys'
$surveyDate = Get-Date -Format 'yyyyMMdd-HHmmss'
$fullTxt    = Join-Path $surveyDir 'pz-install-survey-latest.txt'
$redactedMd = Join-Path $surveyDir 'pz-install-survey-redacted-latest.md'

New-Item -ItemType Directory -Force -Path $surveyDir | Out-Null

# ---------------------------------------------------------------------------
# Helpers
# ---------------------------------------------------------------------------

function Append-Full  { param([string]$Line) Add-Content -Path $fullTxt    -Value $Line -Encoding UTF8 }
function Append-Both  { param([string]$Line)
    Add-Content -Path $fullTxt    -Value $Line -Encoding UTF8
    Add-Content -Path $redactedMd -Value $Line -Encoding UTF8
}
function Append-Red   { param([string]$Line) Add-Content -Path $redactedMd -Value $Line -Encoding UTF8 }

function Count-Bucket([int]$N) {
    if ($N -eq 0)        { return '0' }
    if ($N -le 50)       { return '1-50' }
    if ($N -le 200)      { return '51-200' }
    return '200+'
}

# ---------------------------------------------------------------------------
# Initialise output files
# ---------------------------------------------------------------------------

Set-Content -Path $fullTxt    -Value '' -Encoding UTF8
Set-Content -Path $redactedMd -Value '' -Encoding UTF8

Append-Both "# PZMapForge Phase 3A Survey"
Append-Both ""
Append-Both "Date: $surveyDate"
Append-Both "Claim boundary: planning_artifact_only_not_pz_load_tested"
Append-Both ""
Append-Red  "> Redacted version. No local paths, tilesheet names, or GIDs."
Append-Red  "> See pz-install-survey-latest.txt for the full unredacted report."
Append-Red  ""

# ---------------------------------------------------------------------------
# Locate PZ install
# ---------------------------------------------------------------------------

$commonPaths = @(
    'C:\Program Files (x86)\Steam\steamapps\common\ProjectZomboid',
    'D:\SteamLibrary\steamapps\common\ProjectZomboid',
    'E:\SteamLibrary\steamapps\common\ProjectZomboid',
    'F:\SteamLibrary\steamapps\common\ProjectZomboid'
)

$found = $null

if (-not [string]::IsNullOrWhiteSpace($PzRoot)) {
    if (Test-Path $PzRoot -PathType Container) {
        $found = $PzRoot.TrimEnd('\', '/')
    } else {
        Write-Warning "Provided -PzRoot '$PzRoot' does not exist."
    }
} else {
    $hits = @($commonPaths | Where-Object { Test-Path $_ -PathType Container })
    if ($hits.Count -eq 1) {
        $found = $hits[0]
    } elseif ($hits.Count -gt 1) {
        Write-Output "Multiple PZ installs found; pass -PzRoot to select one:"
        $hits | ForEach-Object { Write-Output "  $_" }
    }
}

$installFound = ($null -ne $found)

Append-Both "## Install"
Append-Both ""
Append-Red  ("Install found: " + $(if ($installFound) { 'yes' } else { 'no' }))
Append-Full ("Install root:  " + $(if ($installFound) { $found } else { '(not found)' }))
Append-Both ""

# ---------------------------------------------------------------------------
# If install not found, write guidance and exit
# ---------------------------------------------------------------------------

if (-not $installFound) {
    $msg = @"
## Operator action required

The script could not locate a Project Zomboid installation automatically.

To run the survey manually:

1. Find your PZ install path via Steam:
   Library > Project Zomboid > right-click > Manage > Browse local files

2. Re-run this script with the path:
   powershell -ExecutionPolicy Bypass -File "scripts\Run-Phase3ALocalPzSurvey.ps1" ``
       -PzRoot "<your PZ install path>"

Or run the manual survey steps in docs/PHASE_3A_LOCAL_INSTALL_SURVEY.md.

Operator action required: yes
"@
    $msg | Out-File -FilePath $fullTxt    -Append -Encoding UTF8
    $msg | Out-File -FilePath $redactedMd -Append -Encoding UTF8

    Write-Output ""
    Write-Output "Survey result:   PZ install not found automatically."
    Write-Output "Operator action: Required. Pass -PzRoot <path> or follow manual steps."
    Write-Output "Full report:     $fullTxt"
    Write-Output "Redacted report: $redactedMd"
    Write-Output ""
    Write-Output "REMINDER: Do not commit .local/pzmapforge/surveys/."
    exit 0
}

# ---------------------------------------------------------------------------
# media/ directory
# ---------------------------------------------------------------------------

$mediaDir  = Join-Path $found 'media'
$mediaOk   = Test-Path $mediaDir -PathType Container

Append-Both "## media/ directory"
Append-Both ""
Append-Red  ("media/ present: " + $(if ($mediaOk) { 'yes' } else { 'no' }))

if ($mediaOk) {
    $mediaDirs = @(Get-ChildItem $mediaDir -Directory -ErrorAction SilentlyContinue |
                   Select-Object -ExpandProperty Name | Sort-Object)
    Append-Full ("media/ subdirectories: " + ($mediaDirs -join ', '))
    Append-Red  ("media/ subdirectory count: " + $mediaDirs.Count)
}
Append-Both ""

# ---------------------------------------------------------------------------
# media/tiles/
# ---------------------------------------------------------------------------

$tilesDir = Join-Path $found 'media\tiles'
$tilesOk  = Test-Path $tilesDir -PathType Container

Append-Both "## media/tiles/ directory"
Append-Both ""
Append-Red  ("media/tiles/ present: " + $(if ($tilesOk) { 'yes' } else { 'no' }))

$tileCount = 0
$extCounts = @{}
$firstNames = @()

if ($tilesOk) {
    $allFiles = @(Get-ChildItem $tilesDir -Recurse -File -ErrorAction SilentlyContinue)
    $tileCount = $allFiles.Count

    foreach ($f in $allFiles) {
        $ext = $f.Extension.ToLowerInvariant()
        if ($ext -eq '') { $ext = '(no ext)' }
        $extCounts[$ext] = ($extCounts[$ext] -as [int]) + 1
    }

    $firstNames = $allFiles | Select-Object -First 20 | Select-Object -ExpandProperty Name

    Append-Full  ("Total files in media/tiles/: $tileCount")
    Append-Red   ("Total file count bucket: " + (Count-Bucket $tileCount))
    Append-Both  ""

    Append-Both  "### File extension breakdown"
    Append-Both  ""
    foreach ($ext in ($extCounts.Keys | Sort-Object)) {
        $cnt = $extCounts[$ext]
        Append-Full ("  $ext : $cnt")
        $present = if ($cnt -gt 0) { 'yes' } else { 'no' }
        Append-Red  ("  $ext present: $present  (count bucket: " + (Count-Bucket $cnt) + ")")
    }
    Append-Both ""

    Append-Full  "### First 20 tilesheet file names (local only)"
    Append-Full  ""
    foreach ($n in $firstNames) { Append-Full "  $n" }
    Append-Full  ""

    Append-Red   "### First 20 tilesheet file names"
    Append-Red   ""
    Append-Red   "(redacted: see pz-install-survey-latest.txt)"
    Append-Red   ""
}

# ---------------------------------------------------------------------------
# Naming clue search
# ---------------------------------------------------------------------------

Append-Both "## Semantic kind naming clues"
Append-Both ""
Append-Both "Search for file names containing keywords related to semantic kinds."
Append-Both "Results: yes/no only in redacted report."
Append-Both ""

$kindKeywords = [ordered]@{
    'grass'       = @('grass', 'lawn', 'nature', 'ground_green')
    'road'        = @('road', 'street', 'asphalt', 'tarmac', 'pavement')
    'sidewalk'    = @('sidewalk', 'curb', 'pavement', 'footpath')
    'buildings'   = @('building', 'house', 'residential', 'structure', 'wall')
    'industrial'  = @('industrial', 'warehouse', 'factory', 'yard', 'lot')
    'ground'      = @('ground', 'floor', 'dirt', 'terrain', 'earth')
    'landmark'    = @('landmark', 'icon', 'poi', 'marker')
    'spawn'       = @('spawn', 'start', 'entry', 'player')
}

if ($tilesOk -and $tileCount -gt 0) {
    foreach ($kind in $kindKeywords.Keys) {
        $keywords = $kindKeywords[$kind]
        $hitCount = 0
        foreach ($kw in $keywords) {
            $hits = @(Get-ChildItem $tilesDir -Recurse -File -ErrorAction SilentlyContinue |
                      Where-Object { $_.Name -match $kw })
            $hitCount += $hits.Count
            if ($hits.Count -gt 0) {
                Append-Full "  '$kw' ($kind): $($hits.Count) match(es)"
                $hits | Select-Object -First 3 | ForEach-Object { Append-Full "    $($_.Name)" }
            }
        }
        $clueFound = if ($hitCount -gt 0) { 'yes' } else { 'no' }
        Append-Red  "  $kind naming clues found: $clueFound"
    }
} else {
    Append-Both "(skipped: tiles directory not found or empty)"
}

Append-Both ""

# ---------------------------------------------------------------------------
# Build/version probe (read-only, text files only)
# ---------------------------------------------------------------------------

Append-Both "## Build version probe"
Append-Both ""

$versionHint = 'unknown'
$versionFiles = @(
    (Join-Path $found 'ProjectZomboid64.bat'),
    (Join-Path $found 'ProjectZomboid32.bat')
)
foreach ($vf in $versionFiles) {
    if (Test-Path $vf -PathType Leaf) {
        $lines = Get-Content $vf -TotalCount 10 -ErrorAction SilentlyContinue
        $vline = $lines | Select-String -Pattern 'build|Build|version|Version' | Select-Object -First 1
        if ($vline) {
            $versionHint = $vline.ToString().Trim()
            break
        }
    }
}

Append-Full  ("Version hint: $versionHint")
$vDetected = if ($versionHint -ne 'unknown') { 'yes' } else { 'no' }
Append-Red   ("Build version hint detected: $vDetected")
Append-Both  ""

# ---------------------------------------------------------------------------
# Media/maps guard check
# ---------------------------------------------------------------------------

Append-Both "## media/maps guard"
Append-Both ""
$localMediaMaps = Join-Path $repoRoot 'media\maps'
$mediaMapsExists = Test-Path $localMediaMaps -PathType Container
if ($mediaMapsExists) {
    $mmItems = @(Get-ChildItem $localMediaMaps -Recurse -Force -ErrorAction SilentlyContinue)
    $mmCount = $mmItems.Count
    Append-Both ("repo media/maps/ exists with $mmCount items -- UNEXPECTED")
} else {
    Append-Both "repo media/maps/ does not exist: OK (PZMapForge does not write here)"
}
Append-Both ""

# ---------------------------------------------------------------------------
# Operator action assessment
# ---------------------------------------------------------------------------

$actionRequired = $false
$actionReasons  = @()

if (-not $installFound)   { $actionRequired = $true; $actionReasons += "PZ install not found" }
if (-not $mediaOk)        { $actionRequired = $true; $actionReasons += "media/ not found" }
if (-not $tilesOk)        { $actionRequired = $true; $actionReasons += "media/tiles/ not found" }
if ($tileCount -eq 0)     { $actionRequired = $true; $actionReasons += "no tile files found" }
if ($versionHint -eq 'unknown') { $actionReasons += "build version not auto-detected (manual check needed)" }

Append-Both "## Operator action required"
Append-Both ""
if ($actionRequired) {
    Append-Both "Action required: YES"
    foreach ($r in $actionReasons) { Append-Both "  - $r" }
    Append-Both ""
    Append-Both "See docs/PHASE_3A_LOCAL_INSTALL_SURVEY.md for manual steps."
} else {
    Append-Both "Action required: No automated blockers found."
    Append-Both "Operator must still:"
    Append-Both "  - Verify the tile naming conventions manually."
    Append-Both "  - Confirm which tiles map to each semantic kind (grass, road, etc.)."
    Append-Both "  - Write docs/PHASE_3A_DECISION.md using placeholders only."
    if ($versionHint -eq 'unknown') {
        Append-Both "  - Check PZ build version manually (auto-detection returned no result)."
    }
}
Append-Both ""
Append-Both "---"
Append-Both "Do NOT commit this file. .local/ is gitignored."

# ---------------------------------------------------------------------------
# Console summary
# ---------------------------------------------------------------------------

Write-Output ""
Write-Output "Phase 3A Local PZ Install Survey"
Write-Output "================================="
Write-Output ""
Write-Output ("Install found:       " + $(if ($installFound) { "yes -- $found" } else { 'NO' }))
Write-Output ("media/ present:      " + $(if ($mediaOk)     { 'yes' } else { 'NO' }))
Write-Output ("media/tiles/ present:" + $(if ($tilesOk)     { " yes" } else { ' NO' }))
if ($tilesOk) {
    Write-Output ("Tile file count:     $tileCount (bucket: " + (Count-Bucket $tileCount) + ")")
}
Write-Output ("Build version hint:  $versionHint")
Write-Output ("Operator action req: " + $(if ($actionRequired) { 'YES -- ' + ($actionReasons -join '; ') } else { 'no automated blockers' }))
Write-Output ""
Write-Output "Full report:         $fullTxt"
Write-Output "Redacted report:     $redactedMd"
Write-Output ""
Write-Output "REMINDER: Do NOT commit .local/pzmapforge/surveys/."
Write-Output "REMINDER: Do NOT paste local paths or tilesheet names into committed docs."
