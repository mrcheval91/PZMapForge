#Requires -Version 5.1
<#
.SYNOPSIS
    MAP-7B: Inspects Lua and metadata files in a generated Build 42 candidate.

    Reads mod.info, map.info, spawnpoints.lua, and objects.lua from a candidate
    42/ directory generated under .local/. Does NOT read PZ install files.
    Does NOT write outside .local/.

    Writes:
      <Output>/build42-candidate-lua-metadata.json
      <Output>/build42-candidate-lua-metadata.md

.PARAMETER CandidateRoot
    Path under .local/ to the candidate 42/ directory (the one containing mod.info).

.PARAMETER Output
    Path under .local/ for report output.

.EXAMPLE
    powershell -ExecutionPolicy Bypass -File .\scripts\inspect-build42-candidate-lua-metadata.ps1 `
        -CandidateRoot .\.local\map7a-packet\candidate\pzmapforge_build42_candidate_v2_001_build42_candidate\42 `
        -Output .\.local\map7b-lua-metadata
#>

param(
    [Parameter(Mandatory=$true)][string]$CandidateRoot,
    [Parameter(Mandatory=$true)][string]$Output
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

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

Assert-LocalPath $CandidateRoot '-CandidateRoot'
Assert-LocalPath $Output        '-Output'

if (-not (Test-Path -LiteralPath $CandidateRoot -PathType Container)) {
    Write-Error "CandidateRoot not found or not a directory: $CandidateRoot"
    exit 1
}

New-Item -ItemType Directory -Force -Path $Output | Out-Null

# Sentinel: candidate_files_read (not PZ install files)
$candidateFilesRead = $true
$pzAssetsRead       = $false
$pzInstallRead      = $false

# ---------------------------------------------------------------------------
# Helpers
# ---------------------------------------------------------------------------

function bytes-hex ([byte[]]$b, [int]$len) {
    $end = [Math]::Min($len, $b.Length) - 1
    if ($end -lt 0) { return '' }
    ($b[0..$end] | ForEach-Object { $_.ToString('x2') }) -join ''
}

function is-ascii-clean ([byte[]]$b) {
    # Start after UTF-8 BOM if present (EF BB BF)
    $start = if ($b.Length -ge 3 -and $b[0] -eq 0xEF -and $b[1] -eq 0xBB -and $b[2] -eq 0xBF) { 3 } else { 0 }
    for ($i = $start; $i -lt $b.Length; $i++) {
        $bv = [int]$b[$i]
        if ($bv -lt 0x09) { return $false }
        if ($bv -eq 0x0B -or $bv -eq 0x0C) { return $false }
        if ($bv -ge 0x0E -and $bv -le 0x1F) { return $false }
        if ($bv -eq 0x7F) { return $false }
        if ($bv -ge 0x80) { return $false }
    }
    return $true
}

function first-lines ([byte[]]$b, [int]$maxLines) {
    $text  = [System.Text.Encoding]::UTF8.GetString($b)
    $lines = $text -split "`n" | Select-Object -First $maxLines
    [string[]]$lines
}

function inspect-file ([string]$path, [int]$readLimit) {
    if (-not $path -or -not (Test-Path $path)) {
        return [ordered]@{
            exists          = $false
            size_bytes      = 0
            first_bytes_hex = ''
            first_lines     = [string[]]@()
            ascii_clean     = $false
        }
    }
    $size    = (Get-Item $path).Length
    $readN   = [Math]::Min($size, $readLimit)
    $bytes   = [byte[]]::new($readN)
    $fs      = [System.IO.File]::OpenRead($path)
    try { [void]$fs.Read($bytes, 0, $readN) } finally { $fs.Dispose() }
    [ordered]@{
        exists          = $true
        size_bytes      = $size
        first_bytes_hex = bytes-hex $bytes 32
        first_lines     = first-lines $bytes 20
        ascii_clean     = is-ascii-clean $bytes
    }
}

# ---------------------------------------------------------------------------
# Detect map_id from mod.info
# ---------------------------------------------------------------------------

$modInfoPath = Join-Path $CandidateRoot 'mod.info'
$modInfo     = inspect-file $modInfoPath 4096

$mapId = ''
if ($modInfo.exists) {
    $modText = [System.IO.File]::ReadAllText($modInfoPath)
    if ($modText -match '(?m)^id\s*=\s*(\S+)') { $mapId = $Matches[1].Trim() }
}

Write-Output "CandidateRoot: $CandidateRoot"
Write-Output "Detected map_id: $(if ($mapId) { $mapId } else { '(not found)' })"

# ---------------------------------------------------------------------------
# Resolve paths
# ---------------------------------------------------------------------------

$mapDataDir  = if ($mapId) { Join-Path $CandidateRoot "media\maps\$mapId" } else { '' }
$mapInfoPath = if ($mapDataDir) { Join-Path $mapDataDir 'map.info'       } else { '' }
$spawnPath   = if ($mapDataDir) { Join-Path $mapDataDir 'spawnpoints.lua' } else { '' }
$objPath     = if ($mapDataDir) { Join-Path $mapDataDir 'objects.lua'     } else { '' }

# ---------------------------------------------------------------------------
# Inspect each file
# ---------------------------------------------------------------------------

$notFound  = [ordered]@{ exists = $false; size_bytes = 0; first_bytes_hex = ''; first_lines = [string[]]@(); ascii_clean = $false }
$mapInfo   = if ($mapInfoPath) { inspect-file $mapInfoPath  4096 } else { $notFound }
$spawnInfo = if ($spawnPath)   { inspect-file $spawnPath    8192 } else { $notFound }
$objInfo   = if ($objPath)     { inspect-file $objPath      4096 } else { $notFound }

# ---------------------------------------------------------------------------
# mod.info analysis
# ---------------------------------------------------------------------------

$modInfoId       = $mapId
$modInfoIdMatches = ($mapId -ne '')

# ---------------------------------------------------------------------------
# map.info analysis
# ---------------------------------------------------------------------------

$mapInfoLots        = ''
$mapInfoLotsMatches = $false
if ($mapInfo.exists -and $mapInfoPath) {
    $miText = [System.IO.File]::ReadAllText($mapInfoPath)
    if ($miText -match '(?m)^lots\s*=\s*(\S+)') {
        $mapInfoLots = $Matches[1].Trim()
        $mapInfoLotsMatches = ($mapInfoLots -eq $mapId)
    }
}

# ---------------------------------------------------------------------------
# spawnpoints.lua analysis
# ---------------------------------------------------------------------------

$spawnHasFunction    = $false
$spawnHasReturnTable = $false
$spawnCompatible     = $false
$spawnHasWorldX      = $false
$spawnHasWorldY      = $false
$spawnHasPosX        = $false
$spawnHasPosY        = $false
$spawnHasPosZ        = $false
$spawnHasUnemployed  = $false
if ($spawnInfo.exists -and $spawnPath) {
    $spText = [System.IO.File]::ReadAllText($spawnPath)
    $spawnHasFunction    = ($spText -match '(?i)function\s+SpawnPoints\s*\(')
    $spawnHasReturnTable = ($spText -match 'return\s*\{')
    $spawnCompatible     = ($spawnHasFunction -and $spawnHasReturnTable)
    $spawnHasWorldX      = ($spText -match 'worldX\s*=')
    $spawnHasWorldY      = ($spText -match 'worldY\s*=')
    $spawnHasPosX        = ($spText -match 'posX\s*=')
    $spawnHasPosY        = ($spText -match 'posY\s*=')
    $spawnHasPosZ        = ($spText -match 'posZ\s*=')
    $spawnHasUnemployed  = ($spText -match 'unemployed\s*=')
}

# ---------------------------------------------------------------------------
# objects.lua analysis
# ---------------------------------------------------------------------------

function get-objects-lua-type ([string]$path) {
    if (-not (Test-Path $path)) { return 'missing' }
    $size = (Get-Item $path).Length
    if ($size -eq 0) { return 'empty' }

    $readN = [Math]::Min($size, 4096)
    $bytes = [byte[]]::new($readN)
    $fs    = [System.IO.File]::OpenRead($path)
    try { [void]$fs.Read($bytes, 0, $readN) } finally { $fs.Dispose() }

    # Skip UTF-8 BOM (EF BB BF) if present before binary detection
    $scanStart = if ($bytes.Length -ge 3 -and $bytes[0] -eq 0xEF -and $bytes[1] -eq 0xBB -and $bytes[2] -eq 0xBF) { 3 } else { 0 }

    # Check for binary (non-text) content
    for ($bi = $scanStart; $bi -lt $bytes.Length; $bi++) {
        $bv = [int]$bytes[$bi]
        if ($bv -lt 0x09) { return 'binary_looking' }
        if ($bv -eq 0x0B -or $bv -eq 0x0C) { return 'binary_looking' }
        if ($bv -ge 0x0E -and $bv -le 0x1F) { return 'binary_looking' }
        if ($bv -eq 0x7F) { return 'binary_looking' }
        if ($bv -ge 0x80) { return 'binary_looking' }
    }

    # Decode text; TrimStart removes UTF-8 BOM char U+FEFF that Encoding.UTF8 may produce
    $text    = [System.Text.Encoding]::UTF8.GetString($bytes).TrimStart([char]0xFEFF)
    $trimmed = $text.Trim()

    # Detect comment-only: all non-empty, non-whitespace lines start with '--'
    $nonCommentLines = @($text -split "`n" | Where-Object { $_.Trim().Length -gt 0 -and -not $_.Trim().StartsWith('--') })
    if ($nonCommentLines.Count -eq 0) { return 'comment_only' }

    # Detect return-only pattern: return {} or return{} with optional whitespace
    if ($trimmed -match '^return\s*\{\s*\}\s*$') { return 'return_only' }

    return 'other_lua'
}

$objectsLuaContentType = if ($objPath) { get-objects-lua-type $objPath } else { 'missing' }

# Recommendations
$objectsLuaRecommendation = switch ($objectsLuaContentType) {
    'comment_only' { 'safe_candidate_try_next' }
    'return_only'  { 'risky_led_to_map7a_failure_try_comment_only' }
    'empty'        { 'unknown_effect_worth_trying_if_pz_tolerates_absent' }
    'missing'      { 'file_absent_may_be_acceptable_pending_retest' }
    default        { 'unknown_content_inspect_manually' }
}
$spawnpointsLuaRecommendation = if ($spawnCompatible -and $spawnHasUnemployed) {
    'explicit_unemployed_key_present'
} elseif ($spawnCompatible) {
    'compatible_shape_but_no_unemployed_key'
} else {
    'incompatible_shape_missing_function_or_return'
}

# ---------------------------------------------------------------------------
# Build report
# ---------------------------------------------------------------------------

$statusLabels = [string[]]@(
    'MAP7A_CLEAN_RETEST_RECORDED',
    'LOTH_V3_EOF_NOT_OBSERVED',
    'ISO_META_GRID_FINISHED_LOADING',
    'OBJECTS_LUA_PRIMARY_BLOCKER',
    'SPAWN_REGION_SECONDARY_BLOCKER',
    'LOAD_TEST_FAIL_OBJECTS_LUA',
    'WRITER_NOT_CHANGED',
    'LOAD_TEST_NOT_PERFORMED',
    'PLAYABLE_EXPORT_CLAIM_ALLOWED=false'
)

$report = [ordered]@{
    schema                       = 'pzmapforge.build42-candidate-lua-metadata.v0.1'
    candidate_root               = $CandidateRoot
    map_id                       = $mapId
    mod_info_exists              = $modInfo.exists
    mod_info_size_bytes          = $modInfo.size_bytes
    mod_info_first_bytes_hex     = $modInfo.first_bytes_hex
    mod_info_first_lines         = $modInfo.first_lines
    mod_info_ascii_clean         = $modInfo.ascii_clean
    mod_info_id                  = $modInfoId
    mod_info_id_matches          = $modInfoIdMatches
    map_info_exists              = $mapInfo.exists
    map_info_size_bytes          = $mapInfo.size_bytes
    map_info_first_bytes_hex     = $mapInfo.first_bytes_hex
    map_info_first_lines         = $mapInfo.first_lines
    map_info_ascii_clean         = $mapInfo.ascii_clean
    map_info_lots                = $mapInfoLots
    map_info_lots_matches        = $mapInfoLotsMatches
    spawnpoints_lua_exists       = $spawnInfo.exists
    spawnpoints_lua_size_bytes   = $spawnInfo.size_bytes
    spawnpoints_lua_first_bytes_hex = $spawnInfo.first_bytes_hex
    spawnpoints_lua_first_lines  = $spawnInfo.first_lines
    spawnpoints_lua_ascii_clean  = $spawnInfo.ascii_clean
    spawnpoints_lua_has_function     = $spawnHasFunction
    spawnpoints_lua_has_return_table = $spawnHasReturnTable
    spawnpoints_lua_compatible_shape = $spawnCompatible
    spawnpoints_lua_has_worldX       = $spawnHasWorldX
    spawnpoints_lua_has_worldY       = $spawnHasWorldY
    spawnpoints_lua_has_posX         = $spawnHasPosX
    spawnpoints_lua_has_posY         = $spawnHasPosY
    spawnpoints_lua_has_posZ         = $spawnHasPosZ
    spawnpoints_lua_has_unemployed   = $spawnHasUnemployed
    spawnpoints_lua_recommendation   = $spawnpointsLuaRecommendation
    objects_lua_exists               = $objInfo.exists
    objects_lua_size_bytes       = $objInfo.size_bytes
    objects_lua_first_bytes_hex  = $objInfo.first_bytes_hex
    objects_lua_first_lines      = $objInfo.first_lines
    objects_lua_ascii_clean      = $objInfo.ascii_clean
    objects_lua_content_type     = $objectsLuaContentType
    objects_lua_recommendation   = $objectsLuaRecommendation
    candidate_files_read         = $candidateFilesRead
    pz_assets_read               = $pzAssetsRead
    pz_install_read              = $pzInstallRead
    playable_export_claimed      = $false
    status_labels                = $statusLabels
}

$jsonPath = Join-Path $Output 'build42-candidate-lua-metadata.json'
$mdPath   = Join-Path $Output 'build42-candidate-lua-metadata.md'

$report | ConvertTo-Json -Depth 6 | Set-Content -Path $jsonPath -Encoding UTF8
Write-Output "JSON: $jsonPath"

# ---------------------------------------------------------------------------
# Markdown
# ---------------------------------------------------------------------------

$fence = '```'
$md = @"
# MAP-7B Build 42 Candidate Lua Metadata Inspection

${fence}text
MAP7A_CLEAN_RETEST_RECORDED
OBJECTS_LUA_PRIMARY_BLOCKER
SPAWN_REGION_SECONDARY_BLOCKER
LOAD_TEST_FAIL_OBJECTS_LUA
WRITER_NOT_CHANGED
LOAD_TEST_NOT_PERFORMED
PLAYABLE_EXPORT_CLAIM_ALLOWED=false
${fence}

## Summary

| Field | Value |
|---|---|
| map_id | $mapId |
| mod_info_exists | $($modInfo.exists) |
| mod_info_id_matches | $modInfoIdMatches |
| map_info_exists | $($mapInfo.exists) |
| map_info_lots_matches | $mapInfoLotsMatches |
| spawnpoints_lua_exists | $($spawnInfo.exists) |
| spawnpoints_lua_compatible | $spawnCompatible |
| objects_lua_exists | $($objInfo.exists) |
| objects_lua_content_type | $objectsLuaContentType |

## Interpretation

objects_lua_content_type=$objectsLuaContentType

$(if ($objectsLuaContentType -eq 'return_only') {
    'objects.lua contains return {} -- valid Lua syntax but rejected by PZ Lua engine (MAP-7A finding).'
} elseif ($objectsLuaContentType -eq 'empty') {
    'objects.lua is empty. PZ may reject empty files.'
} elseif ($objectsLuaContentType -eq 'binary_looking') {
    'objects.lua contains non-ASCII bytes. Likely not valid Lua.'
} else {
    'objects.lua content type: other_lua or missing.'
})

## Non-claims

- WRITER_NOT_CHANGED: no writer change in MAP-7B.
- LOAD_TEST_NOT_PERFORMED: this is a local-only inspection.
- PLAYABLE_EXPORT_CLAIM_ALLOWED=false: binding.
"@

Set-Content -Path $mdPath -Value $md -Encoding ASCII
Write-Output "MD:   $mdPath"
Write-Output ""
Write-Output "map_id:                        $mapId"
Write-Output "mod_info_exists:               $($modInfo.exists)"
Write-Output "mod_info_id_matches:           $modInfoIdMatches"
Write-Output "map_info_exists:               $($mapInfo.exists)"
Write-Output "map_info_lots_matches:         $mapInfoLotsMatches"
Write-Output "spawnpoints_lua_compatible:    $spawnCompatible"
Write-Output "objects_lua_content_type:      $objectsLuaContentType"
Write-Output "candidate_files_read:          $candidateFilesRead"
Write-Output "pz_assets_read:                $pzAssetsRead"
Write-Output "MAP7A_CLEAN_RETEST_RECORDED"
Write-Output "OBJECTS_LUA_PRIMARY_BLOCKER"
Write-Output "LOAD_TEST_NOT_PERFORMED"
Write-Output "PLAYABLE_EXPORT_CLAIM_ALLOWED=false"
Write-Output "Done."
