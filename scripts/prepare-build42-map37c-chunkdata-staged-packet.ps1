#Requires -Version 5.1
<#
.SYNOPSIS
    MAP-37C: Generate a Build 42 candidate packet using the MAP-37B-fixed chunkdata writer
    and produce deterministic binary-shape proof for chunkdata_35_27.bin.

    Outputs to .local\ only. Runs CLI twice; compares SHA-256 to prove determinism.
    Does not write to Steam, Workshop, Project Zomboid install folders, or live server folders.

Claim boundary:
    PLAYABLE_EXPORT_CLAIM_ALLOWED=false
    verified_chunkdata_format=false (structure confirmed by MAP-37A ExactFitScore=3; not load-tested)
    LOAD_TEST_NOT_PERFORMED=true
    staged_output_local_only=true
#>
param(
    [string]$OutputRoot = ''
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot  = Split-Path -Parent $scriptDir

if ($OutputRoot -eq '') {
    $OutputRoot = Join-Path $repoRoot '.local\deadmtl-authoring\map37c-chunkdata-staged-packet'
}

function Assert-LocalPath([string]$p) {
    $localRoot = Join-Path $repoRoot '.local'
    $resolved  = [System.IO.Path]::GetFullPath($p)
    if (-not $resolved.StartsWith([System.IO.Path]::GetFullPath($localRoot), [System.StringComparison]::OrdinalIgnoreCase)) {
        Write-Error "MAP-37C: Output path must be under .local\: $p"
        exit 1
    }
}

Assert-LocalPath $OutputRoot

$mapId   = 'pzmapforge_map37c'
$profile = 'empty_grass_v5'
$cellX   = 35
$cellY   = 27

$run1Out = Join-Path $OutputRoot 'run1'
$run2Out = Join-Path $OutputRoot 'run2'

New-Item -ItemType Directory -Path $OutputRoot -Force | Out-Null

$cliProject = Join-Path $repoRoot 'src\PZMapForge.Cli\PZMapForge.Cli.csproj'

Write-Output "MAP-37C: Run 1..."
Remove-Item -Recurse -Force $run1Out -ErrorAction SilentlyContinue
& dotnet run --project $cliProject --configuration Release --no-build `
    -- map-export-experimental `
    --map-id $mapId `
    --output $run1Out `
    --build42-candidate-writer `
    --build42-candidate-profile $profile `
    --cell-x $cellX `
    --cell-y $cellY
if ($LASTEXITCODE -ne 0) { throw "MAP-37C Run 1 CLI failed (exit $LASTEXITCODE)" }

Write-Output "MAP-37C: Run 2 (determinism check)..."
Remove-Item -Recurse -Force $run2Out -ErrorAction SilentlyContinue
& dotnet run --project $cliProject --configuration Release --no-build `
    -- map-export-experimental `
    --map-id $mapId `
    --output $run2Out `
    --build42-candidate-writer `
    --build42-candidate-profile $profile `
    --cell-x $cellX `
    --cell-y $cellY
if ($LASTEXITCODE -ne 0) { throw "MAP-37C Run 2 CLI failed (exit $LASTEXITCODE)" }

$chunkRel   = "${mapId}_build42_candidate\42\media\maps\${mapId}\chunkdata_35_27.bin"
$chunkPath1 = Join-Path $run1Out $chunkRel
$chunkPath2 = Join-Path $run2Out $chunkRel

if (-not (Test-Path $chunkPath1)) { throw "MAP-37C: chunkdata_35_27.bin not found at $chunkPath1" }
if (-not (Test-Path $chunkPath2)) { throw "MAP-37C: chunkdata_35_27.bin not found at $chunkPath2" }

$bytes1 = [System.IO.File]::ReadAllBytes($chunkPath1)
$bytes2 = [System.IO.File]::ReadAllBytes($chunkPath2)

$fileSize    = $bytes1.Length
$header0     = [int]$bytes1[0]
$header1     = [int]$bytes1[1]
$headerSize  = 2
$recordWidth = 8
$bodyLen     = $fileSize - $headerSize
$exactFit    = ($bodyLen % $recordWidth -eq 0)
$recordCount = if ($exactFit) { $bodyLen / $recordWidth } else { 0 }

$sha   = [System.Security.Cryptography.SHA256]::Create()
$hash1 = [System.BitConverter]::ToString($sha.ComputeHash($bytes1)).Replace('-', '').ToLower()
$hash2 = [System.BitConverter]::ToString($sha.ComputeHash($bytes2)).Replace('-', '').ToLower()
$sha.Dispose()
$deterministic = ($hash1 -eq $hash2)

$header0Hex = '{0:X2}' -f $header0
$header1Hex = '{0:X2}' -f $header1
$generatedAt = (Get-Date -Format 'yyyy-MM-ddTHH:mm:ssZ')

$preflight = [ordered]@{
    schema                          = 'pzmapforge.map37c-chunkdata-staged-packet.v0.1'
    generated_at_utc                = $generatedAt
    map_id                          = $mapId
    profile                         = $profile
    cell_x                          = $cellX
    cell_y                          = $cellY
    chunkdata_file                  = 'chunkdata_35_27.bin'
    chunkdata_file_size             = $fileSize
    chunkdata_header_byte_0         = $header0
    chunkdata_header_byte_1         = $header1
    chunkdata_header_size           = $headerSize
    chunkdata_record_width          = $recordWidth
    chunkdata_record_count          = $recordCount
    chunkdata_exact_fit             = $exactFit
    chunkdata_sha256_run1           = $hash1
    chunkdata_sha256_run2           = $hash2
    chunkdata_deterministic         = $deterministic
    chunkdata_map37b_writer_applied = $true
    verified_chunkdata_format       = $false
    PLAYABLE_EXPORT_CLAIM_ALLOWED   = $false
    LOAD_TEST_NOT_PERFORMED         = $true
    staged_output_local_only        = $true
}

$jsonPath = Join-Path $OutputRoot 'map37c-chunkdata-staged-packet.json'
$preflight | ConvertTo-Json -Depth 4 | Out-File $jsonPath -Encoding utf8

$mdPath = Join-Path $OutputRoot 'map37c-chunkdata-staged-packet.md'
@"
# MAP-37C Chunkdata Staged Candidate Packet

Generated: $generatedAt
Schema: pzmapforge.map37c-chunkdata-staged-packet.v0.1

## Binary proof

| Field | Value |
|---|---|
| chunkdata_file | chunkdata_35_27.bin |
| file_size | $fileSize |
| header_byte_0 | 0x$header0Hex |
| header_byte_1 | 0x$header1Hex |
| header_size | $headerSize |
| record_width | $recordWidth |
| record_count | $recordCount |
| exact_fit | $exactFit |
| sha256_run1 | $hash1 |
| sha256_run2 | $hash2 |
| deterministic | $deterministic |

## Claim boundary

MAP37C_STAGED
CHUNKDATA_MAP37B_WRITER_APPLIED
PLAYABLE_EXPORT_CLAIM_ALLOWED=false
verified_chunkdata_format=false
LOAD_TEST_NOT_PERFORMED=true
staged_output_local_only=true
"@ | Out-File $mdPath -Encoding utf8

Write-Output ""
Write-Output "MAP-37C chunkdata staged packet complete."
Write-Output "  file_size        : $fileSize"
Write-Output "  header           : 0x$header0Hex 0x$header1Hex"
Write-Output "  record_width     : $recordWidth"
Write-Output "  record_count     : $recordCount"
Write-Output "  exact_fit        : $exactFit"
Write-Output "  deterministic    : $deterministic"
Write-Output "  PLAYABLE_EXPORT_CLAIM_ALLOWED=false"
