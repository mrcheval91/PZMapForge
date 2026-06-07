#Requires -Version 5.1
<#
.SYNOPSIS
    MAP-7S: Generates the private Workshop upload staging packet.

    Stages the PZMapForge candidate Workshop package under .local/ only.
    Uses the existing empty_grass_v4 dotnet CLI candidate generator.
    Applies the Dru_map-aligned layout (MAP-7O contract).
    Writes all staging packet docs and preflight to .local/.

    Does NOT upload to Steam Workshop.
    Does NOT run Project Zomboid.
    Does NOT write to mods, Server, Workshop, or PZ install paths.
    Does NOT change binary writer behavior.

    All output is under .local/ only.

.PARAMETER Output
    Required. Path under .local/ for packet output.

.PARAMETER CandidateMapId
    Optional. Map ID. Default: pzmapforge_build42_candidate_v4_001
#>

param(
    [Parameter(Mandatory=$true)][string]$Output,
    [string]$CandidateMapId = 'pzmapforge_build42_candidate_v4_001'
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot  = Split-Path -Parent $scriptDir

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

Write-Output "MAP-7S: Private Workshop Upload Staging Packet"
Write-Output "Output:       $Output"
Write-Output "CandidateMapId: $CandidateMapId"
Write-Output ""

# ---------------------------------------------------------------------------
# Step 1: Generate empty_grass_v4 candidate via dotnet CLI
# ---------------------------------------------------------------------------

$candidateOut = Join-Path $Output 'candidate'
Write-Output "Generating empty_grass_v4 candidate..."

& dotnet run --project (Join-Path $repoRoot 'src\PZMapForge.Cli') `
    --configuration Release --no-build `
    -- map-export-experimental `
    --map-id $CandidateMapId `
    --output $candidateOut `
    --build42-candidate-writer `
    --build42-candidate-profile empty_grass_v4

if ($LASTEXITCODE -ne 0) {
    Write-Error "CLI candidate generation failed (exit $LASTEXITCODE)"
    exit 1
}

$candDir    = Join-Path $candidateOut ($CandidateMapId + '_build42_candidate')
$src42Dir   = Join-Path $candDir '42'
$srcMapData = Join-Path $src42Dir "media\maps\$CandidateMapId"

# ---------------------------------------------------------------------------
# Step 2: Build staged Workshop package (Dru_map-aligned layout)
# ---------------------------------------------------------------------------

Write-Output "Building staged Workshop package (Dru_map-aligned layout)..."

$stagedRoot      = Join-Path $Output "staged-workshop\$CandidateMapId"
$staged42Dir     = Join-Path $stagedRoot '42'
$stagedCommonDir = Join-Path $stagedRoot 'common'
$stagedMapsDir   = Join-Path $stagedCommonDir "media\maps\$CandidateMapId"
$stagedMapsSubdir = Join-Path $stagedMapsDir 'maps'

New-Item -ItemType Directory -Force -Path $staged42Dir     | Out-Null
New-Item -ItemType Directory -Force -Path $stagedMapsDir   | Out-Null
New-Item -ItemType Directory -Force -Path $stagedMapsSubdir | Out-Null

function Test-HasBom {
    param([string]$Path)
    if (-not (Test-Path -LiteralPath $Path)) { return $false }
    $b = [System.IO.File]::ReadAllBytes($Path)
    return ($b.Length -ge 3 -and $b[0] -eq 0xEF -and $b[1] -eq 0xBB -and $b[2] -eq 0xBF)
}

# root mod.info (from 42/mod.info, no-BOM)
$src42ModInfo = Join-Path $src42Dir 'mod.info'
Copy-Item -LiteralPath $src42ModInfo `
          -Destination (Join-Path $stagedRoot 'mod.info') -Force
# 42/mod.info
Copy-Item -LiteralPath $src42ModInfo `
          -Destination (Join-Path $staged42Dir 'mod.info') -Force
# NO common/mod.info (intentional -- Dru_map alignment)

# Map data files
$mapDataFiles = @('map.info', 'spawnpoints.lua', 'objects.lua',
                  '0_0.lotheader', 'world_0_0.lotpack', 'chunkdata_0_0.bin')
foreach ($f in $mapDataFiles) {
    $src = Join-Path $srcMapData $f
    if (Test-Path -LiteralPath $src) {
        Copy-Item -LiteralPath $src `
                  -Destination (Join-Path $stagedMapsDir $f) -Force
    }
}

# Modify map.info: lots=NONE, add zoomX/Y/S (Dru_map alignment)
$mapInfoPath = Join-Path $stagedMapsDir 'map.info'
if (Test-Path -LiteralPath $mapInfoPath) {
    $mapInfoContent = [System.IO.File]::ReadAllText(
        $mapInfoPath, [System.Text.UTF8Encoding]::new($false))
    $mapInfoContent = [regex]::Replace($mapInfoContent, '(?m)^lots=.*$', 'lots=NONE')
    $mapInfoContent = $mapInfoContent.TrimEnd() + "`n"
    if ($mapInfoContent -notmatch '(?m)^zoomX=') { $mapInfoContent += "zoomX=0`n" }
    if ($mapInfoContent -notmatch '(?m)^zoomY=') { $mapInfoContent += "zoomY=0`n" }
    if ($mapInfoContent -notmatch '(?m)^zoomS=') { $mapInfoContent += "zoomS=1`n" }
    [System.IO.File]::WriteAllText($mapInfoPath, $mapInfoContent,
        [System.Text.UTF8Encoding]::new($false))
    Write-Output "  map.info: lots=NONE, zoomX/Y/S set"
}

# Placeholder image files (poster.png, thumb.png, biomemap)
function Write-PlaceholderPng {
    param([string]$Path)
    try {
        Add-Type -AssemblyName System.Drawing -ErrorAction Stop
        $bmp = New-Object System.Drawing.Bitmap(64, 64)
        $g   = [System.Drawing.Graphics]::FromImage($bmp)
        $g.Clear([System.Drawing.Color]::DarkGray)
        $g.Dispose()
        $bmp.Save($Path, [System.Drawing.Imaging.ImageFormat]::Png)
        $bmp.Dispose()
    } catch {
        [System.IO.File]::WriteAllBytes($Path, [byte[]]@())
    }
}

Write-PlaceholderPng (Join-Path $stagedRoot 'poster.png')
Write-PlaceholderPng (Join-Path $stagedMapsDir 'thumb.png')
Write-PlaceholderPng (Join-Path $stagedMapsSubdir 'biomemap_0_0.png')

# worldmap stubs
[System.IO.File]::WriteAllText(
    (Join-Path $stagedMapsDir 'worldmap.xml'),
    "<?xml version=""1.0"" encoding=""UTF-8""?>`n<worldmap />`n",
    [System.Text.UTF8Encoding]::new($false))
[System.IO.File]::WriteAllText(
    (Join-Path $stagedMapsDir 'worldmap-forest.xml'),
    "<?xml version=""1.0"" encoding=""UTF-8""?>`n<worldmap />`n",
    [System.Text.UTF8Encoding]::new($false))

Write-Output "Staged package: $stagedRoot"

# ---------------------------------------------------------------------------
# Step 3: Verify staged package and build manifest
# ---------------------------------------------------------------------------

$requiredRelPaths = @(
    'mod.info',
    '42\mod.info',
    "common\media\maps\$CandidateMapId\map.info",
    "common\media\maps\$CandidateMapId\spawnpoints.lua",
    "common\media\maps\$CandidateMapId\objects.lua",
    "common\media\maps\$CandidateMapId\0_0.lotheader",
    "common\media\maps\$CandidateMapId\world_0_0.lotpack",
    "common\media\maps\$CandidateMapId\chunkdata_0_0.bin"
)

$missingFiles  = [System.Collections.Generic.List[string]]::new()
$presentFiles  = [System.Collections.Generic.List[string]]::new()
$manifestLines = [System.Collections.Generic.List[string]]::new()

foreach ($rel in $requiredRelPaths) {
    $absPath = Join-Path $stagedRoot $rel
    if (Test-Path -LiteralPath $absPath) {
        $size = (Get-Item -LiteralPath $absPath).Length
        $presentFiles.Add($rel)
        $manifestLines.Add("  PRESENT  $rel  ($size bytes)")
    } else {
        $missingFiles.Add($rel)
        $manifestLines.Add("  MISSING  $rel")
    }
}

$requiredFilesPresent = ($missingFiles.Count -eq 0)
$stagedRelPath = "staged-workshop/$CandidateMapId"

# ---------------------------------------------------------------------------
# Step 4: Write packet docs
# ---------------------------------------------------------------------------

# MAP_7S_STAGED_PACKAGE_MANIFEST.md
$manifestPath = Join-Path $Output 'MAP_7S_STAGED_PACKAGE_MANIFEST.md'
$manifestMd = @"
# MAP-7S: Staged Workshop Package Manifest

``````text
MAP7S_WORKSHOP_STAGING_PACKET_CREATED
STAGED_PACKAGE_LOCAL_ONLY
NO_AUTOMATIC_WORKSHOP_UPLOAD
PUBLIC_PLAYABLE_CLAIM_ALLOWED=false
``````

## Package root

``````text
$stagedRoot
``````

## Required files

$(($manifestLines | ForEach-Object { $_ }) -join "`n")

## Optional placeholder files

$(if (Test-Path (Join-Path $stagedRoot 'poster.png')) {
    "  poster.png (placeholder, 64x64)"
} else { "  poster.png MISSING (placeholder not generated)" })
$(if (Test-Path (Join-Path $stagedMapsDir 'thumb.png')) {
    "  common/media/maps/$CandidateMapId/thumb.png (placeholder, 64x64)"
} else { "  thumb.png MISSING" })

## Status

``````text
required_files_present: $($requiredFilesPresent.ToString().ToLower())
missing_count: $($missingFiles.Count)
``````
"@
Set-Content -Path $manifestPath -Value $manifestMd -Encoding ASCII
Write-Output "Wrote: MAP_7S_STAGED_PACKAGE_MANIFEST.md"

# MAP_7S_HUMAN_UPLOAD_CHECKLIST.md
$checklistPath = Join-Path $Output 'MAP_7S_HUMAN_UPLOAD_CHECKLIST.md'
$checklistMd = @"
# MAP-7S: Human Upload Checklist

``````text
HUMAN_ONLY_UPLOAD_REQUIRED
NO_AUTOMATIC_WORKSHOP_UPLOAD
PUBLIC_PLAYABLE_CLAIM_ALLOWED=false
``````

## This script does NOT upload anything.

All Workshop upload steps are HUMAN-ONLY.
Claude does not perform Workshop uploads.

## Steps

### Step 1: Review the staged package

Review the staged package at:
  $stagedRoot

Verify required files are present (see MAP_7S_STAGED_PACKAGE_MANIFEST.md).

### Step 2: Create a new private/unlisted Workshop item on Steam

IMPORTANT: Do NOT use Workshop ID 3355966216 (that is Dru_map's Workshop ID).
Create a NEW Workshop item for the PZMapForge candidate.
Set visibility to Private or Unlisted.
Record the new PZMapForge Workshop ID.

### Step 3: Upload the staged package content

Upload the content of:
  $stagedRoot
to the new Workshop item.

### Step 4: Wire the server

After the Workshop item is uploaded and available, create a server preset:
  Server: PZMF_B42_WS_CANDIDATE_K_001

``````ini
Mods=$CandidateMapId
WorkshopItems=<PZMapForgeOwnWorkshopId>
Map=$CandidateMapId;Muldraugh, KY
Public=false
``````

Replace <PZMapForgeOwnWorkshopId> with the actual Workshop ID recorded in Step 2.

### Step 5: Test and capture logs

Launch server and client. After game loading completes (or fails), copy logs:
  Copy the client log to .local/map7s-logs/

### Step 6: Analyze

``````powershell
powershell -ExecutionPolicy Bypass ``
    -File .\scripts\inspect-build42-map7d-load-result.ps1 ``
    -LogPath .\.local\map7s-logs\DebugLog-client.txt ``
    -Output .\.local\map7s-packet\analysis-after-upload ``
    -ExpectedMapId $CandidateMapId ``
    -VariantLabel VariantWSUpload
``````

### Step 7: Evaluate result

Look for expected_map_lotheader_meta_evidence_found=true in the analyzer output.
See MAP_7S_SUCCESS_FAILURE_CRITERIA.md for interpretation.
"@
Set-Content -Path $checklistPath -Value $checklistMd -Encoding ASCII
Write-Output "Wrote: MAP_7S_HUMAN_UPLOAD_CHECKLIST.md"

# MAP_7S_SERVER_WIRING_AFTER_UPLOAD_TEMPLATE.md
$wiringPath = Join-Path $Output 'MAP_7S_SERVER_WIRING_AFTER_UPLOAD_TEMPLATE.md'
$wiringMd = @"
# MAP-7S: Server Wiring After Upload Template

## IMPORTANT: Do NOT use 3355966216

Workshop ID 3355966216 is Dru_map's ID. Using it again will activate Dru_map,
not the PZMapForge candidate. You must use the NEW Workshop ID you recorded
when uploading the PZMapForge candidate.

## Server ini wiring template

After uploading the staged package and recording the new Workshop ID:

``````ini
; Server: PZMF_B42_WS_CANDIDATE_K_001
Mods=$CandidateMapId
WorkshopItems=<PZMapForgeOwnWorkshopId>
Map=$CandidateMapId;Muldraugh, KY
Public=false
``````

Replace <PZMapForgeOwnWorkshopId> with the actual new Workshop ID.

## Success signal

Workshop Installed/Ready state for PZMapForge Workshop ID.
expected_map_lotheader_meta_evidence_found=true in analyzer output.
OR: Built custom PZMapForge world visible (not fallback forest).

## Failure signal

Fallback forest appears.
expected_map_lotheader_meta_evidence_found=false.
Map folders scan still empty (expected map not mounted).
"@
Set-Content -Path $wiringPath -Value $wiringMd -Encoding ASCII
Write-Output "Wrote: MAP_7S_SERVER_WIRING_AFTER_UPLOAD_TEMPLATE.md"

# MAP_7S_LOG_CAPTURE_AFTER_UPLOAD.md
$logCapturePath = Join-Path $Output 'MAP_7S_LOG_CAPTURE_AFTER_UPLOAD.md'
$logCaptureContent = @"
# MAP-7S: Log Capture After Upload

## HUMAN-ONLY: Copy log to .local before analyzing.

``````powershell
New-Item -ItemType Directory -Force -Path ".\.local\map7s-logs"
Copy-Item "C:\Users\Palmacede\Zomboid\Logs\DebugLog-client.txt" ``
    ".\.local\map7s-logs\DebugLog-ws-upload-test.txt"
``````

## Analyzer command

``````powershell
powershell -ExecutionPolicy Bypass ``
    -File .\scripts\inspect-build42-map7d-load-result.ps1 ``
    -LogPath .\.local\map7s-logs\DebugLog-ws-upload-test.txt ``
    -Output .\.local\map7s-packet\analysis-after-upload ``
    -ExpectedMapId $CandidateMapId ``
    -VariantLabel VariantWSUpload
``````

## Key fields to check in the analyzer output

``````text
expected_map_lotheader_meta_evidence_found: true/false
lotheader_meta_evidence_found:             true/false
lotheader_meta_paths_or_names:             [list]
workshop_installed_seen:                   true/false
workshop_ready_seen:                       true/false
expected_mod_loaded:                       true/false
multiplayer_reached:                       true/false
map_folders_list_empty:                    true/false
``````

## Important: empty map_folders_list_empty is NOT decisive

As confirmed in MAP-7Q, an empty printed client map-folder scan can coexist
with a working Workshop map (Dru_map had an empty scan but loaded correctly).
The decisive signals are:
- expected_map_lotheader_meta_evidence_found=true
- Built custom world visible (not fallback forest)
"@
Set-Content -Path $logCapturePath -Value $logCaptureContent -Encoding ASCII
Write-Output "Wrote: MAP_7S_LOG_CAPTURE_AFTER_UPLOAD.md"

# MAP_7S_SUCCESS_FAILURE_CRITERIA.md
$criteriaPath = Join-Path $Output 'MAP_7S_SUCCESS_FAILURE_CRITERIA.md'
$criteriaContent = @"
# MAP-7S: Success and Failure Criteria

## Success

If Workshop upload test produces:
  expected_map_lotheader_meta_evidence_found=true
  (analyzer detects pzmapforge candidate ID near .lotheader references)

OR:
  Built custom PZMapForge world visible (roads, buildings, custom content)
  Not the fallback forest world.

Interpretation:
  Runtime activation gate is cleared.
  Binary writer validation becomes the next investigation focus.
  Investigate LOTH/LOTP/chunkdata format acceptance.

## Failure (candidate still mod-only)

expected_map_lotheader_meta_evidence_found=false
Fallback forest appears.
Map folders scan empty (no custom-map mounting evidence).

Interpretation:
  Runtime activation still blocked even with own Workshop ID.
  Investigate: server-side logs, Workshop content path vs mods path,
  whether PZ build 42 requires specific Workshop mod structure.

## Binary writer gate

``````text
BINARY_WRITER_GATE_CLOSED_UNTIL:
  expected_map_lotheader_meta_evidence_found=true

Do not mutate LOTH/LOTP/chunkdata until this gate is cleared.
``````

## Claim boundary

``````text
PUBLIC_PLAYABLE_CLAIM_ALLOWED=false
LOAD_TEST_NOT_PERFORMED_BY_SCRIPT
NO_AUTOMATIC_WORKSHOP_UPLOAD
``````
"@
Set-Content -Path $criteriaPath -Value $criteriaContent -Encoding ASCII
Write-Output "Wrote: MAP_7S_SUCCESS_FAILURE_CRITERIA.md"

# MAP_7S_PRIVATE_WORKSHOP_STAGING_PACKET.md (main)
$packetPath = Join-Path $Output 'MAP_7S_PRIVATE_WORKSHOP_STAGING_PACKET.md'
$packetContent = @"
# MAP-7S: Private Workshop Upload Staging Packet

``````text
MAP7S_WORKSHOP_STAGING_PACKET_CREATED
STAGED_PACKAGE_LOCAL_ONLY
NO_AUTOMATIC_WORKSHOP_UPLOAD
LOAD_TEST_NOT_PERFORMED_BY_SCRIPT
PUBLIC_PLAYABLE_CLAIM_ALLOWED=false
``````

## Purpose

This packet stages the PZMapForge candidate as a Workshop package
for the operator to manually upload as a private/unlisted Workshop item.

## Why this is needed

MAP-7R confirmed WorkshopItems=3355966216 (Dru_map's ID) is insufficient.
WorkshopItems= must reference the candidate's OWN Workshop ID.
The PZMapForge candidate needs a real Workshop activation to proceed.

## What was staged

Staged package layout (Dru_map-aligned, MAP-7O contract):
``````text
$stagedRoot
  mod.info
  poster.png (placeholder)
  42/mod.info
  common/media/maps/$CandidateMapId/
    map.info (lots=NONE, zoomX/Y/S)
    objects.lua
    spawnpoints.lua
    0_0.lotheader
    world_0_0.lotpack
    chunkdata_0_0.bin
    thumb.png (placeholder)
    worldmap.xml (stub)
    worldmap-forest.xml (stub)
    maps/biomemap_0_0.png (placeholder)
``````

## What NOT to do

- Do NOT use WorkshopItems=3355966216 (that is Dru_map's ID, not ours).
- Do NOT upload automatically. This is HUMAN-ONLY.
- Do NOT claim playable export.
- Do NOT mutate LOTH/LOTP/chunkdata before expected_map_lotheader_meta_evidence_found=true.

## Packet files

``````text
MAP_7S_PRIVATE_WORKSHOP_STAGING_PACKET.md  (this file)
MAP_7S_HUMAN_UPLOAD_CHECKLIST.md
MAP_7S_SERVER_WIRING_AFTER_UPLOAD_TEMPLATE.md
MAP_7S_LOG_CAPTURE_AFTER_UPLOAD.md
MAP_7S_SUCCESS_FAILURE_CRITERIA.md
MAP_7S_STAGED_PACKAGE_MANIFEST.md
map7s-preflight.json
map7s-preflight.md
``````

## Safety

``````text
PUBLIC_PLAYABLE_CLAIM_ALLOWED=false
LOAD_TEST_NOT_PERFORMED_BY_SCRIPT
NO_AUTOMATIC_WORKSHOP_UPLOAD
NO_BINARY_WRITER_CHANGES
No PZ run performed by script.
No writes to Workshop, mods, Server, or PZ install paths.
All Workshop upload steps are HUMAN-ONLY.
``````
"@
Set-Content -Path $packetPath -Value $packetContent -Encoding ASCII
Write-Output "Wrote: MAP_7S_PRIVATE_WORKSHOP_STAGING_PACKET.md"

# ---------------------------------------------------------------------------
# Step 5: Write preflight JSON and MD
# ---------------------------------------------------------------------------

$preflight = [ordered]@{
    schema                             = 'pzmapforge.map7s-preflight.v0.1'
    candidate_map_id                   = $CandidateMapId
    variant_j_result                   = 'MAP7R_VARIANT_J_WORKSHOP_TRIGGER_INSUFFICIENT'
    borrowed_workshopitems_trigger_insufficient = $true
    requires_own_workshop_id           = $true
    automatic_workshop_upload_performed = $false
    load_test_performed_by_script      = $false
    binary_writer_changed              = $false
    public_playable_claim_allowed      = $false
    staged_package_created             = $true
    staged_package_path                = $stagedRelPath
    required_files_present             = $requiredFilesPresent
    required_files_missing             = [string[]]($missingFiles.ToArray())
}

$preflightJsonPath = Join-Path $Output 'map7s-preflight.json'
$preflight | ConvertTo-Json -Depth 3 | Set-Content -Path $preflightJsonPath -Encoding ASCII
Write-Output "Wrote: map7s-preflight.json"

$preflightMdPath = Join-Path $Output 'map7s-preflight.md'
$preflightMdContent = @"
# MAP-7S Preflight

## State

``````text
MAP7S_WORKSHOP_STAGING_PACKET_CREATED
STAGED_PACKAGE_LOCAL_ONLY
NO_AUTOMATIC_WORKSHOP_UPLOAD
PUBLIC_PLAYABLE_CLAIM_ALLOWED=false
``````

## Preflight fields

``````text
candidate_map_id:                     $CandidateMapId
variant_j_result:                     MAP7R_VARIANT_J_WORKSHOP_TRIGGER_INSUFFICIENT
borrowed_workshopitems_trigger_insufficient: true
requires_own_workshop_id:             true
automatic_workshop_upload_performed:  false
load_test_performed_by_script:        false
binary_writer_changed:                false
staged_package_created:               true
staged_package_path:                  $stagedRelPath
required_files_present:               $($requiredFilesPresent.ToString().ToLower())
``````
"@
Set-Content -Path $preflightMdPath -Value $preflightMdContent -Encoding ASCII
Write-Output "Wrote: map7s-preflight.md"

Write-Output ""
Write-Output "MAP-7S packet complete."
Write-Output "Staged package: $stagedRoot"
Write-Output "required_files_present: $requiredFilesPresent"
Write-Output "automatic_workshop_upload_performed=false"
Write-Output "PUBLIC_PLAYABLE_CLAIM_ALLOWED=false"
Write-Output "LOAD_TEST_NOT_PERFORMED_BY_SCRIPT"
Write-Output "NO_AUTOMATIC_WORKSHOP_UPLOAD"
Write-Output "Done."
