#Requires -Version 5.1
<#
.SYNOPSIS
    Experimental IGMB worldmap writer skeleton.
    Produces a PZMapForge-owned worldmap.xml.bin candidate under .local/ using the
    observed IGMB structure from MAP-8Q through MAP-8X.

    Output is local-only. This skeleton is NOT load-tested and makes no playable claim.
    All bytes are generated from PZMapForge-owned data; no third-party bytes are copied.

.PARAMETER Output
    Required. Output directory. Must contain '.local'. Forbidden paths refused.

.PARAMETER MapId
    Logical map identifier embedded in the manifest. Default: pzmapforge_build42_candidate_v4_001.

.PARAMETER ParentMapFolder
    Parent map folder name embedded in the manifest. Default: PZMapForge.

.PARAMETER TotalBytes
    Total output file size in bytes. Range: 8192-65536. Default: 65536.

.PARAMETER TransitionOffset
    Byte offset where the triplet and payload begin (after FF padding). Default: 6389.

.PARAMETER StringPoolEndOffset
    Byte offset where the string pool ends and FF padding begins. Default: 133.
#>

[CmdletBinding()]
param(
    [Parameter(Mandatory=$true)][string]$Output,
    [string]$MapId               = 'pzmapforge_build42_candidate_v4_001',
    [string]$ParentMapFolder     = 'PZMapForge',
    [int]   $TotalBytes          = 65536,
    [int]   $TransitionOffset    = 6389,
    [int]   $StringPoolEndOffset = 133
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

# .local guard
$absOutput = [System.IO.Path]::GetFullPath($Output)
if (-not $absOutput.Contains('.local')) {
    Write-Error "-Output must be under .local/. Got: $absOutput"
    exit 1
}

# Forbidden path guards
$forbiddenSubstrings = @('media\maps', 'media/maps', 'Steam', 'workshop', 'ProjectZomboid',
    'C:\Program Files', 'D:\Program Files')
foreach ($f in $forbiddenSubstrings) {
    if ($absOutput.Contains($f)) {
        Write-Error "-Output path is forbidden (contains '$f'): $absOutput"
        exit 1
    }
}

# Parameter validation
if ($TotalBytes -lt 8192 -or $TotalBytes -gt 65536) {
    Write-Error "-TotalBytes must be between 8192 and 65536. Got: $TotalBytes"
    exit 1
}
if ($TransitionOffset -ge $TotalBytes) {
    Write-Error "-TransitionOffset ($TransitionOffset) must be less than TotalBytes ($TotalBytes)"
    exit 1
}
if ($StringPoolEndOffset -ge $TransitionOffset) {
    Write-Error "-StringPoolEndOffset ($StringPoolEndOffset) must be less than TransitionOffset ($TransitionOffset)"
    exit 1
}

# Create output directory
if (-not (Test-Path -LiteralPath $absOutput)) {
    New-Item -ItemType Directory -Path $absOutput -Force | Out-Null
}

# Build the binary buffer (zero-initialized)
$buf = [byte[]]::new($TotalBytes)

# 1. IGMB magic (49 47 4D 42)
$buf[0] = 0x49; $buf[1] = 0x47; $buf[2] = 0x4D; $buf[3] = 0x42

# 2. IGMB header fields - inline U32LE assignments, no function (PS5.1 typed-param safety)
# version = 2 (U32LE at offset 4: 02 00 00 00)
$buf[4] = [byte]2;  $buf[5] = [byte]0;  $buf[6] = [byte]0;  $buf[7] = [byte]0
# unknown_a = 256 (U32LE at offset 8: 00 01 00 00)
$buf[8] = [byte]0;  $buf[9] = [byte]1;  $buf[10] = [byte]0; $buf[11] = [byte]0
# unknown_b = 59 (U32LE at offset 12: 3B 00 00 00)
$buf[12] = [byte]59; $buf[13] = [byte]0; $buf[14] = [byte]0; $buf[15] = [byte]0
# unknown_c = 68 (U32LE at offset 16: 44 00 00 00)
$buf[16] = [byte]68; $buf[17] = [byte]0; $buf[18] = [byte]0; $buf[19] = [byte]0
# string_pool_count = 12 (U32LE at offset 20: 0C 00 00 00)
$buf[20] = [byte]12; $buf[21] = [byte]0; $buf[22] = [byte]0; $buf[23] = [byte]0

# 3. String pool: 12 U16LE LP UTF-8 strings (MAP-8R evidence)
# All string lengths < 256, so high byte of length prefix is always 0.
$poolStrings = [string[]]@(
    'Polygon', 'highway', 'primary', 'trail', 'natural', 'forest',
    'water', 'river', 'tertiary', 'building', 'Residential', 'secondary'
)
$pos = 24
foreach ($s in $poolStrings) {
    $bytes = [System.Text.Encoding]::UTF8.GetBytes($s)
    $slen  = $bytes.Length  # Int32; all strings < 256 chars
    $buf[$pos]   = [byte]($slen -band 0xFF)
    $buf[$pos+1] = [byte]0
    $pos += 2
    [System.Array]::Copy($bytes, 0, $buf, $pos, $bytes.Length)
    $pos += $bytes.Length
}

# 4. FF padding: StringPoolEndOffset .. TransitionOffset-1 (MAP-8T/8U/8V evidence)
for ($i = $StringPoolEndOffset; $i -lt $TransitionOffset; $i++) {
    $buf[$i] = 0xFF
}

# 5. Candidate header triplet (MAP-8V/8W/8X evidence, observed_only_unconfirmed)
# 30 = U32LE at TransitionOffset: 1E 00 00 00
$buf[$TransitionOffset]     = [byte]30; $buf[$TransitionOffset+1] = [byte]0
$buf[$TransitionOffset+2]   = [byte]0;  $buf[$TransitionOffset+3] = [byte]0
# 26 = U32LE at TransitionOffset+4: 1A 00 00 00
$buf[$TransitionOffset+4]   = [byte]26; $buf[$TransitionOffset+5] = [byte]0
$buf[$TransitionOffset+6]   = [byte]0;  $buf[$TransitionOffset+7] = [byte]0
# 9 = U32LE at TransitionOffset+8: 09 00 00 00
$buf[$TransitionOffset+8]   = [byte]9;  $buf[$TransitionOffset+9] = [byte]0
$buf[$TransitionOffset+10]  = [byte]0;  $buf[$TransitionOffset+11] = [byte]0

# 6. Synthetic PZMapForge-owned U16LE payload (generated from scratch, not copied)
# 16 U16LE values < 256 as local cell-space references for cell 35_27.
# Each U16LE: [low byte, 0x00] since all values < 256.
$synPayload = [int[]]@(
    246, 0,   188, 0,   247, 0,   189, 0,
    248, 0,   190, 0,   249, 0,   191, 0,
    250, 0,   192, 0,   251, 0,   193, 0,
    252, 0,   194, 0,   255, 0,   197, 0
)
$payloadOffset = $TransitionOffset + 12
for ($j = 0; $j -lt $synPayload.Length; $j++) {
    $buf[$payloadOffset + $j] = [byte]$synPayload[$j]
}
$payloadOffset += $synPayload.Length

# 7. Pad remainder with 0xFF
for ($i = $payloadOffset; $i -lt $TotalBytes; $i++) {
    $buf[$i] = 0xFF
}

# 8. Write worldmap.xml.bin
$binPath = Join-Path $absOutput 'worldmap.xml.bin'
[System.IO.File]::WriteAllBytes($binPath, $buf)

# 9. Compute SHA-256
$sha = [System.Security.Cryptography.SHA256]::Create()
$hashBytes = $null
try {
    $hashBytes = $sha.ComputeHash($buf)
} finally {
    $sha.Dispose()
}
$sha256 = ($hashBytes | ForEach-Object { $_.ToString('x2') }) -join ''

# 10. Build manifest
$generatedAt = [System.DateTime]::UtcNow.ToString('yyyy-MM-ddTHH:mm:ssZ')
$manifest = [ordered]@{
    schema                          = 'pzmapforge.map8y-experimental-igmb-writer-manifest.v0.1'
    generated_at_utc                = $generatedAt
    map_id                          = $MapId
    parent_map_folder               = $ParentMapFolder
    output_file                     = 'worldmap.xml.bin'
    total_bytes                     = $TotalBytes
    magic                           = 'IGMB'
    igmb_version_u32le              = 2
    unknown_a_u32le                 = 256
    unknown_b_u32le                 = 59
    unknown_c_u32le                 = 68
    string_pool_count               = 12
    string_pool_start_offset        = 24
    string_pool_end_offset          = $StringPoolEndOffset
    ff_padding_start                = $StringPoolEndOffset
    ff_padding_end                  = ($TransitionOffset - 1)
    ff_padding_length               = ($TransitionOffset - $StringPoolEndOffset)
    transition_offset               = $TransitionOffset
    candidate_header_triplet_first  = 30
    candidate_header_triplet_second = 26
    candidate_header_triplet_third  = 9
    candidate_header_triplet_basis  = 'observed_only_unconfirmed_map8x'
    payload_start_offset            = ($TransitionOffset + 12)
    payload_kind                    = 'synthetic_pzmapforge_owned_u16le_pairs'
    payload_source                  = 'generated_from_scratch_not_copied'
    sha256                          = $sha256
    experimental                    = $true
    writer_status                   = 'experimental_skeleton_not_load_proven'
    third_party_bytes_copied        = $false
    project_russia_file_read        = $false
    pz_run_performed                = $false
    workshop_upload_performed       = $false
    playable_claim_allowed          = $false
    load_test_performed             = $false
    full_igmb_format_understood     = $false
    cell_index_understood           = $false
    geometry_payload_understood     = $false
    triplet_fields_proven           = $false
    output_scope                    = '.local_only'
    next_branch                     = 'map8z_controlled_install_packet_pending_operator_approval'
}

$jsonPath = Join-Path $absOutput 'worldmap-writer-manifest.json'
$manifest | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath $jsonPath -Encoding UTF8

$mdLines = [System.Collections.ArrayList]::new()
[void]$mdLines.Add('# MAP-8Y Experimental IGMB Writer Manifest')
[void]$mdLines.Add('')
[void]$mdLines.Add("Generated: $generatedAt")
[void]$mdLines.Add("Map ID: $MapId")
[void]$mdLines.Add("Output: worldmap.xml.bin ($TotalBytes bytes)")
[void]$mdLines.Add("SHA-256: $sha256")
[void]$mdLines.Add('')
[void]$mdLines.Add('## Safety')
[void]$mdLines.Add('')
[void]$mdLines.Add('```text')
[void]$mdLines.Add('MAP8Y_EXPERIMENTAL_IGMB_WRITER_SKELETON_ADDED')
[void]$mdLines.Add('EXPERIMENTAL_WRITER_LOCAL_ONLY')
[void]$mdLines.Add('WRITER_STATUS=experimental_skeleton_not_load_proven')
[void]$mdLines.Add('THIRD_PARTY_BYTES_COPIED=false')
[void]$mdLines.Add('PROJECT_RUSSIA_FILE_READ=false')
[void]$mdLines.Add('PLAYABLE_CLAIM_ALLOWED=false')
[void]$mdLines.Add('FULL_IGMB_FORMAT_UNDERSTOOD=false')
[void]$mdLines.Add('CELL_INDEX_UNDERSTOOD=false')
[void]$mdLines.Add('GEOMETRY_PAYLOAD_UNDERSTOOD=false')
[void]$mdLines.Add('```')
$mdPath = Join-Path $absOutput 'worldmap-writer-manifest.md'
[System.IO.File]::WriteAllLines($mdPath, $mdLines)

Write-Output "OK: wrote worldmap.xml.bin ($TotalBytes bytes)"
Write-Output "OK: SHA-256 = $sha256"
Write-Output "OK: wrote worldmap-writer-manifest.json"
Write-Output "OK: wrote worldmap-writer-manifest.md"
Write-Output "MAP8Y: experimental IGMB writer skeleton complete"
exit 0
