#Requires -Version 5.1
<#
.SYNOPSIS
    MAP-8Z controlled install packet for experimental IGMB worldmap bin.
    Reads the MAP-8Y generated worldmap.xml.bin from .local/, stages a copy,
    and writes a controlled install packet with human-only install steps.

    Does NOT copy to Steam, Workshop, or Project Zomboid. Human manual install only.
    Output is .local/ only.

.PARAMETER Output
    Required. Output directory. Must contain '.local'. Forbidden paths refused.

.PARAMETER GeneratedWorldmapBinPath
    Required. Path to MAP-8Y generated worldmap.xml.bin. Must exist. Must be under .local/.

.PARAMETER MapId
    Mod/map identifier. Default: pzmapforge_build42_candidate_v4_001.

.PARAMETER ParentMapFolder
    Parent map folder name. Default: PZMapForge.

.PARAMETER WorkshopItemId
    Workshop item ID. Default: 3740642200.
#>

[CmdletBinding()]
param(
    [Parameter(Mandatory=$true)][string]$Output,
    [Parameter(Mandatory=$true)][string]$GeneratedWorldmapBinPath,
    [string]$MapId           = 'pzmapforge_build42_candidate_v4_001',
    [string]$ParentMapFolder = 'PZMapForge',
    [string]$WorkshopItemId  = '3740642200'
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

# .local guard on -Output
$absOutput = [System.IO.Path]::GetFullPath($Output)
if (-not $absOutput.Contains('.local')) {
    Write-Error "-Output must be under .local/. Got: $absOutput"
    exit 1
}

# Forbidden paths for -Output
$forbidden = @('media\maps', 'media/maps', 'Steam', 'workshop', 'ProjectZomboid',
    'C:\Program Files', 'D:\Program Files')
foreach ($f in $forbidden) {
    if ($absOutput.Contains($f)) {
        Write-Error "-Output path is forbidden (contains '$f'): $absOutput"
        exit 1
    }
}

# -GeneratedWorldmapBinPath must exist
$absGenPath = [System.IO.Path]::GetFullPath($GeneratedWorldmapBinPath)
if (-not (Test-Path -LiteralPath $absGenPath)) {
    Write-Error "-GeneratedWorldmapBinPath does not exist: $absGenPath"
    exit 1
}

# -GeneratedWorldmapBinPath must be under .local/
if (-not $absGenPath.Contains('.local')) {
    Write-Error "-GeneratedWorldmapBinPath must be under .local/. Got: $absGenPath"
    exit 1
}

# Read source and validate size
$srcBytes = [System.IO.File]::ReadAllBytes($absGenPath)
$srcSize  = $srcBytes.Length
if ($srcSize -ne 65536) {
    Write-Error "-GeneratedWorldmapBinPath must be exactly 65536 bytes. Got: $srcSize"
    exit 1
}

# Compute source SHA-256
$sha1 = [System.Security.Cryptography.SHA256]::Create()
$srcHashBytes = $null
try { $srcHashBytes = $sha1.ComputeHash($srcBytes) } finally { $sha1.Dispose() }
$srcSha = ($srcHashBytes | ForEach-Object { $_.ToString('x2') }) -join ''

# Create output directory
if (-not (Test-Path -LiteralPath $absOutput)) {
    New-Item -ItemType Directory -Path $absOutput -Force | Out-Null
}

# Create staged directory and copy
$stagedDir = Join-Path $absOutput "staged\common\media\maps\$ParentMapFolder"
if (-not (Test-Path -LiteralPath $stagedDir)) {
    New-Item -ItemType Directory -Path $stagedDir -Force | Out-Null
}
$stagedBinPath = Join-Path $stagedDir 'worldmap.xml.bin'
[System.IO.File]::Copy($absGenPath, $stagedBinPath, $true)

# Read staged file and verify SHA-256 match
$stgBytes = [System.IO.File]::ReadAllBytes($stagedBinPath)
$sha2 = [System.Security.Cryptography.SHA256]::Create()
$stgHashBytes = $null
try { $stgHashBytes = $sha2.ComputeHash($stgBytes) } finally { $sha2.Dispose() }
$stgSha = ($stgHashBytes | ForEach-Object { $_.ToString('x2') }) -join ''
$shaMatch = ($srcSha -eq $stgSha)

if (-not $shaMatch) {
    Write-Error "SHA-256 mismatch after staging. Source=$srcSha Staged=$stgSha"
    exit 1
}

# Build install packet JSON
$generatedAt      = [System.DateTime]::UtcNow.ToString('yyyy-MM-ddTHH:mm:ssZ')
$humanInstallPath = "D:\Program Files (x86)\Steam\steamapps\workshop\content\108600\$WorkshopItemId\mods\$MapId\common\media\maps\$ParentMapFolder\worldmap.xml.bin"

$packet = [ordered]@{
    schema                              = 'pzmapforge.map8z-controlled-igmb-install-packet.v0.1'
    generated_at_utc                    = $generatedAt
    controlled_install_packet_created   = $true
    source_generated_worldmap_bin_path  = $absGenPath
    source_sha256                       = $srcSha
    source_size_bytes                   = $srcSize
    staged_worldmap_bin_path            = $stagedBinPath
    staged_sha256                       = $stgSha
    staged_size_bytes                   = $stgBytes.Length
    sha256_match                        = $shaMatch
    map_id                              = $MapId
    parent_map_folder                   = $ParentMapFolder
    workshop_item_id                    = $WorkshopItemId
    target_relative_path                = "common/media/maps/$ParentMapFolder/worldmap.xml.bin"
    target_workshop_path_human_only     = $humanInstallPath
    output_scope                        = '.local_only'
    human_manual_copy_required          = $true
    claude_copied_to_workshop           = $false
    claude_modified_steam_files         = $false
    claude_ran_pz                       = $false
    claude_uploaded_workshop            = $false
    third_party_bytes_copied            = $false
    generated_file_only                 = $true
    playable_claim_allowed              = $false
    load_test_performed                 = $false
    writer_status                       = 'experimental_skeleton_not_load_proven'
    test_goal                           = 'determine_whether_generated_worldmap_bin_changes_WorldMapDataAssetManager_or_IsoMetaGrid_behavior'
    success_signal                      = 'runtime_log_attempts_or_accepts_generated_worldmap_xml_bin_without_worldmap_xml_bin_parse_error'
    failure_signal                      = 'worldmap_xml_bin_parse_error_or_no_behavior_change'
    next_branch                         = 'human_runtime_test_pending'
}

$jsonPath = Join-Path $absOutput 'map8z-controlled-igmb-install-packet.json'
$packet | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath $jsonPath -Encoding UTF8

# Build MD packet
$mdLines = [System.Collections.ArrayList]::new()
[void]$mdLines.Add('# MAP-8Z Controlled IGMB Install Packet')
[void]$mdLines.Add('')
[void]$mdLines.Add("Generated: $generatedAt")
[void]$mdLines.Add('Schema: pzmapforge.map8z-controlled-igmb-install-packet.v0.1')
[void]$mdLines.Add('')
[void]$mdLines.Add('## Source')
[void]$mdLines.Add('')
[void]$mdLines.Add("Path: $absGenPath")
[void]$mdLines.Add("SHA-256: $srcSha")
[void]$mdLines.Add("Size: $srcSize bytes")
[void]$mdLines.Add('')
[void]$mdLines.Add('## Staged')
[void]$mdLines.Add('')
[void]$mdLines.Add("Path: $stagedBinPath")
[void]$mdLines.Add("SHA-256: $stgSha")
[void]$mdLines.Add("SHA-256 match: $shaMatch")
[void]$mdLines.Add('')
[void]$mdLines.Add('## Safety')
[void]$mdLines.Add('')
[void]$mdLines.Add('```text')
[void]$mdLines.Add('MAP8Z_CONTROLLED_IGMB_INSTALL_PACKET_CREATED')
[void]$mdLines.Add('HUMAN_MANUAL_COPY_REQUIRED=true')
[void]$mdLines.Add('CLAUDE_COPIED_TO_WORKSHOP=false')
[void]$mdLines.Add('CLAUDE_MODIFIED_STEAM_FILES=false')
[void]$mdLines.Add('CLAUDE_RAN_PZ=false')
[void]$mdLines.Add('CLAUDE_UPLOADED_WORKSHOP=false')
[void]$mdLines.Add('THIRD_PARTY_BYTES_COPIED=false')
[void]$mdLines.Add('GENERATED_FILE_ONLY=true')
[void]$mdLines.Add('PLAYABLE_CLAIM_ALLOWED=false')
[void]$mdLines.Add('```')
$mdPath = Join-Path $absOutput 'map8z-controlled-igmb-install-packet.md'
[System.IO.File]::WriteAllLines($mdPath, $mdLines)

# Write MAP_8Z_HUMAN_INSTALL_STEPS.md
$stepLines = [System.Collections.ArrayList]::new()
[void]$stepLines.Add('# MAP-8Z Human Install Steps')
[void]$stepLines.Add('')
[void]$stepLines.Add('IMPORTANT: These steps must be performed manually by the operator.')
[void]$stepLines.Add('Claude does not copy to Steam, Workshop, or Project Zomboid directories.')
[void]$stepLines.Add('Playable claim is NOT allowed until runtime logs confirm successful mount.')
[void]$stepLines.Add('')
[void]$stepLines.Add('## Source file (generated by MAP-8Y writer, PZMapForge-owned bytes only)')
[void]$stepLines.Add('')
[void]$stepLines.Add("- Path: $absGenPath")
[void]$stepLines.Add("- Size: $srcSize bytes")
[void]$stepLines.Add("- SHA-256: $srcSha")
[void]$stepLines.Add('')
[void]$stepLines.Add('## Staged file (copy of PZMapForge-generated file, not third-party)')
[void]$stepLines.Add('')
[void]$stepLines.Add("- Path: $stagedBinPath")
[void]$stepLines.Add("- SHA-256: $stgSha")
[void]$stepLines.Add('')
[void]$stepLines.Add('## Install steps')
[void]$stepLines.Add('')
[void]$stepLines.Add('1. Close Project Zomboid and any running PZ server.')
[void]$stepLines.Add('')
[void]$stepLines.Add('2. Confirm the candidate Workshop item path exists:')
[void]$stepLines.Add("   D:\Program Files (x86)\Steam\steamapps\workshop\content\108600\$WorkshopItemId\mods\$MapId\common\media\maps\$ParentMapFolder\")
[void]$stepLines.Add('')
[void]$stepLines.Add('3. Back up the existing target file if present:')
[void]$stepLines.Add("   $humanInstallPath")
[void]$stepLines.Add('   (rename to worldmap.xml.bin.bak if it exists)')
[void]$stepLines.Add('')
[void]$stepLines.Add('4. Copy staged worldmap.xml.bin to target path:')
[void]$stepLines.Add("   FROM: $stagedBinPath")
[void]$stepLines.Add("   TO:   $humanInstallPath")
[void]$stepLines.Add('')
[void]$stepLines.Add("5. Confirm SHA-256 after copy matches: $stgSha")
[void]$stepLines.Add('')
[void]$stepLines.Add('6. Launch the same controlled server config as previous tests.')
[void]$stepLines.Add('')
[void]$stepLines.Add('7. Use Map line:')
[void]$stepLines.Add("   Map=$MapId;$ParentMapFolder;Muldraugh, KY")
[void]$stepLines.Add('')
[void]$stepLines.Add("8. Spawn/select $MapId if prompted.")
[void]$stepLines.Add('')
[void]$stepLines.Add('9. Capture client and server logs.')
[void]$stepLines.Add('')
[void]$stepLines.Add('10. Do not claim playable unless generated content visibly mounts and logs confirm it.')
[void]$stepLines.Add('')
[void]$stepLines.Add('11. Report whether there is:')
[void]$stepLines.Add('    - worldmap bin parse error')
[void]$stepLines.Add('    - WorldMapDataAssetManager success/failure signal')
[void]$stepLines.Add('    - IsoMetaGrid map folder still empty')
[void]$stepLines.Add('    - spawn still exact at 10746,8288,0')
[void]$stepLines.Add('    - visual content difference')
[void]$stepLines.Add('    - crash or disconnect')
[void]$stepLines.Add('')
[void]$stepLines.Add('## Safety')
[void]$stepLines.Add('')
[void]$stepLines.Add('```text')
[void]$stepLines.Add('HUMAN_MANUAL_COPY_REQUIRED=true')
[void]$stepLines.Add('CLAUDE_COPIED_TO_WORKSHOP=false')
[void]$stepLines.Add('CLAUDE_RAN_PZ=false')
[void]$stepLines.Add('NO_PLAYABLE_CLAIM')
[void]$stepLines.Add('next_branch=human_runtime_test_pending')
[void]$stepLines.Add('```')
$stepsPath = Join-Path $absOutput 'MAP_8Z_HUMAN_INSTALL_STEPS.md'
[System.IO.File]::WriteAllLines($stepsPath, $stepLines)

Write-Output "OK: staged worldmap.xml.bin to $stagedBinPath"
Write-Output "OK: SHA-256 match = $shaMatch"
Write-Output "OK: wrote map8z-controlled-igmb-install-packet.json"
Write-Output "OK: wrote map8z-controlled-igmb-install-packet.md"
Write-Output "OK: wrote MAP_8Z_HUMAN_INSTALL_STEPS.md"
Write-Output "MAP8Z: controlled install packet complete"
exit 0
