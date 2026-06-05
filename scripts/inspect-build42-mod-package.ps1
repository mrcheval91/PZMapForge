#Requires -Version 5.1
<#
.SYNOPSIS
    Local-only Build 42 mod package structure inspector (MAP-5C).

    Compares an operator-provided mod package folder against the PZ ModTemplate
    structure and reads small text metadata files (workshop.txt, mod.info, map.info).
    Writes a local-only diagnostic JSON and Markdown report.

    Does NOT copy files.
    Does NOT modify PackageRoot or TemplateRoot.
    Does NOT write to PZ install directories.
    Does NOT read .lotheader, .lotpack, or .bin files.
    Does NOT touch repo media/maps.
    Does NOT claim playable export.
    Output must be under .local only.

Usage:
    .\scripts\inspect-build42-mod-package.ps1 `
        -PackageRoot  <path to mod package folder> `
        -TemplateRoot <path to ModTemplate folder> `
        -Output       <output directory (must be under .local)>

Example:
    .\scripts\inspect-build42-mod-package.ps1 `
        -PackageRoot  "C:\Users\<Name>\Zomboid\Workshop\MyPackage" `
        -TemplateRoot "D:\...\ProjectZomboid\Workshop\ModTemplate" `
        -Output       ".local\packaging\inspection-01"
#>

param(
    [Parameter(Mandatory=$true)]
    [string]$PackageRoot,

    [Parameter(Mandatory=$true)]
    [string]$TemplateRoot,

    [Parameter(Mandatory=$true)]
    [string]$Output
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

Write-Output 'inspect-build42-mod-package.ps1'
Write-Output "PackageRoot:  $PackageRoot"
Write-Output "TemplateRoot: $TemplateRoot"
Write-Output "Output:       $Output"
Write-Output ''

# ---------------------------------------------------------------------------
# Guard: Output must be under .local
# ---------------------------------------------------------------------------

$outputFull  = [System.IO.Path]::GetFullPath($Output)
$sep         = [System.IO.Path]::DirectorySeparatorChar
$localMarker = $sep + '.local' + $sep
$endsLocal   = $outputFull.EndsWith($sep + '.local')

if (-not ($outputFull.Contains($localMarker) -or $endsLocal)) {
    Write-Error "inspect-build42-mod-package: refusing to write outside a .local/ directory: $outputFull"
    Write-Error "  Pass -Output to an explicit .local/ path."
    exit 1
}

# ---------------------------------------------------------------------------
# Guard: PackageRoot and TemplateRoot must exist
# ---------------------------------------------------------------------------

$packageFull  = [System.IO.Path]::GetFullPath($PackageRoot)
$templateFull = [System.IO.Path]::GetFullPath($TemplateRoot)

if (-not (Test-Path -LiteralPath $packageFull)) {
    Write-Error "PackageRoot not found: $packageFull"
    exit 1
}
if (-not (Test-Path -LiteralPath $templateFull)) {
    Write-Error "TemplateRoot not found: $templateFull"
    exit 1
}

# ---------------------------------------------------------------------------
# Helper: read small text file safely (max 8KB)
# ---------------------------------------------------------------------------

function Read-SmallTextFile {
    param([string]$FilePath, [int]$MaxBytes = 8192)
    if (-not (Test-Path -LiteralPath $FilePath)) { return $null }
    $size = (Get-Item -LiteralPath $FilePath).Length
    if ($size -gt $MaxBytes) { return "(file too large to read: $size bytes)" }
    return (Get-Content -LiteralPath $FilePath -Raw -Encoding UTF8 -ErrorAction SilentlyContinue)
}

# ---------------------------------------------------------------------------
# Helper: parse key=value lines from text content
# ---------------------------------------------------------------------------

function Parse-KeyValues {
    param([string]$Content)
    $kv = [ordered]@{}
    if ($null -eq $Content) { return $kv }
    foreach ($line in ($Content -split "`n")) {
        $t  = $line.Trim()
        if ($t -eq '' -or $t.StartsWith('#') -or $t.StartsWith(';')) { continue }
        $eq = $t.IndexOf('=')
        if ($eq -gt 0) {
            $key = $t.Substring(0, $eq).Trim()
            $val = $t.Substring($eq + 1).Trim()
            if ($key -ne '' -and -not $kv.Contains($key)) { $kv[$key] = $val }
        }
    }
    return $kv
}

# ---------------------------------------------------------------------------
# Enumerate files in a folder (names/paths/sizes only, no binary reads)
# ---------------------------------------------------------------------------

function Get-FileSummary {
    param([string]$Root)
    $files = @(Get-ChildItem -LiteralPath $Root -Recurse -File -ErrorAction SilentlyContinue |
        Sort-Object FullName |
        ForEach-Object {
            $rel = $_.FullName.Substring($Root.Length).TrimStart([System.IO.Path]::DirectorySeparatorChar)
            [ordered]@{
                relative_path = ($rel -replace '\\', '/')
                extension     = $_.Extension.ToLowerInvariant()
                size_bytes    = $_.Length
            }
        })
    return $files
}

$generatedAt = (Get-Date).ToUniversalTime().ToString('yyyy-MM-ddTHH:mm:ssZ')

# ---------------------------------------------------------------------------
# Enumerate package and template files
# ---------------------------------------------------------------------------

Write-Output '--- Package file inventory ---'
$packageFiles  = @(Get-FileSummary $packageFull)
$templateFiles = @(Get-FileSummary $templateFull)

$packageFiles  | ForEach-Object { Write-Output ("  {0,-70} {1,8} bytes" -f $_.relative_path, $_.size_bytes) }
Write-Output ''

# ---------------------------------------------------------------------------
# Check expected Build 42 Workshop paths in the package
# ---------------------------------------------------------------------------

Write-Output '--- Checking expected Build 42 package paths ---'

$rootWorkshopTxt  = Test-Path (Join-Path $packageFull 'workshop.txt')
$rootPreviewPng   = Test-Path (Join-Path $packageFull 'preview.png')
$contentsMods     = Test-Path (Join-Path $packageFull 'Contents\mods')
$rootModInfo      = Test-Path (Join-Path $packageFull 'mod.info')

# Detect mod_id: look for nested mod.info under Contents\mods\<id>\
$packageModId     = $null
$nestedModInfo    = $null
$nestedModInfoKv  = [ordered]@{}
$contentsMapsPath = $null
$nestedMapInfoKv  = [ordered]@{}

$contentsModsPath = Join-Path $packageFull 'Contents\mods'
if (Test-Path -LiteralPath $contentsModsPath) {
    $modDirs = @(Get-ChildItem -LiteralPath $contentsModsPath -Directory -ErrorAction SilentlyContinue)
    if ($modDirs.Count -gt 0) {
        $packageModId  = $modDirs[0].Name
        $nestedModInfo = Join-Path $modDirs[0].FullName 'mod.info'
        if (Test-Path -LiteralPath $nestedModInfo) {
            $nestedModInfoKv = Parse-KeyValues (Read-SmallTextFile $nestedModInfo)
        } else { $nestedModInfo = $null }
        # Look for map.info under media/maps/<any>/
        $mediaMapsDir = Join-Path $modDirs[0].FullName 'media\maps'
        if (Test-Path -LiteralPath $mediaMapsDir) {
            $mapDirs = @(Get-ChildItem -LiteralPath $mediaMapsDir -Directory -ErrorAction SilentlyContinue)
            if ($mapDirs.Count -gt 0) {
                $mapInfoPath = Join-Path $mapDirs[0].FullName 'map.info'
                if (Test-Path -LiteralPath $mapInfoPath) {
                    $nestedMapInfoKv = Parse-KeyValues (Read-SmallTextFile $mapInfoPath)
                    $contentsMapsPath = "Contents/mods/$packageModId/media/maps/$($mapDirs[0].Name)/map.info"
                }
            }
        }
    }
}

$nestedModInfoPresent = ($null -ne $nestedModInfo)
$nestedMediaMapsPresent = ($null -ne $contentsMapsPath)

# Root mod.info (flat-layout probe)
$rootModInfoKv = [ordered]@{}
if ($rootModInfo) {
    $rootModInfoKv = Parse-KeyValues (Read-SmallTextFile (Join-Path $packageFull 'mod.info'))
}

# workshop.txt content
$workshopTxtKv = [ordered]@{}
if ($rootWorkshopTxt) {
    $workshopTxtKv = Parse-KeyValues (Read-SmallTextFile (Join-Path $packageFull 'workshop.txt'))
}

$expected = [ordered]@{
    root_workshop_txt        = $rootWorkshopTxt
    root_preview_png         = $rootPreviewPng
    contents_mods_directory  = $contentsMods
    nested_mod_info          = $nestedModInfoPresent
    nested_media_maps_map_info = $nestedMediaMapsPresent
}

foreach ($k in $expected.Keys) {
    $ok = if ($expected[$k]) { 'PRESENT' } else { 'ABSENT ' }
    Write-Output "  $ok  $k"
}
Write-Output "  mod_id detected:  $(if ($packageModId) { $packageModId } else { '(none)' })"
Write-Output "  root mod.info:    $(if ($rootModInfo) { 'PRESENT' } else { 'ABSENT' })"
Write-Output ''

# ---------------------------------------------------------------------------
# Template comparison
# ---------------------------------------------------------------------------

Write-Output '--- Template structure summary ---'
$templateFiles | ForEach-Object { Write-Output ("  $($_.relative_path)") }
$templateModId = $null
$templateContentsModsPath = Join-Path $templateFull 'Contents\mods'
if (Test-Path -LiteralPath $templateContentsModsPath) {
    $tDirs = @(Get-ChildItem -LiteralPath $templateContentsModsPath -Directory -ErrorAction SilentlyContinue)
    if ($tDirs.Count -gt 0) { $templateModId = $tDirs[0].Name }
}
Write-Output ''

# Read template mod.info for comparison
$templateModInfoKv  = [ordered]@{}
$templateModInfoPath = Join-Path $templateFull "Contents\mods\$templateModId\mod.info"
if (Test-Path -LiteralPath $templateModInfoPath) {
    $templateModInfoKv = Parse-KeyValues (Read-SmallTextFile $templateModInfoPath)
}

$templateMapInfoKv = [ordered]@{}
$templateMapsDir   = Join-Path $templateFull "Contents\mods\$templateModId\media\maps"
if (Test-Path -LiteralPath $templateMapsDir) {
    $tMapDirs = @(Get-ChildItem -LiteralPath $templateMapsDir -Directory -ErrorAction SilentlyContinue)
    if ($tMapDirs.Count -gt 0) {
        $tMapInfoPath = Join-Path $tMapDirs[0].FullName 'map.info'
        if (Test-Path -LiteralPath $tMapInfoPath) {
            $templateMapInfoKv = Parse-KeyValues (Read-SmallTextFile $tMapInfoPath)
        }
    }
}

# ---------------------------------------------------------------------------
# mod.info field comparison
# ---------------------------------------------------------------------------

Write-Output '--- mod.info field comparison ---'
$allModInfoKeys = @($nestedModInfoKv.Keys) + @($templateModInfoKv.Keys) + @($rootModInfoKv.Keys) | Sort-Object -Unique
foreach ($k in $allModInfoKeys) {
    $pkgVal  = if ($nestedModInfoKv.Contains($k))  { $nestedModInfoKv[$k]  } elseif ($rootModInfoKv.Contains($k)) { "[root] $($rootModInfoKv[$k])" } else { '(absent)' }
    $tplVal  = if ($templateModInfoKv.Contains($k)) { $templateModInfoKv[$k] } else { '(absent)' }
    Write-Output ("  {0,-15} package={1,-40} template={2}" -f $k, $pkgVal, $tplVal)
}
Write-Output ''

# ---------------------------------------------------------------------------
# Write JSON output
# ---------------------------------------------------------------------------

New-Item -ItemType Directory -Force -Path $outputFull | Out-Null

$evidence = [ordered]@{
    schema                      = 'pzmapforge.build42-mod-package-inspection.v0.1'
    claim_boundary              = 'packaging_diagnostic_only_not_load_tested'
    generated_at_utc            = $generatedAt
    package_root                = ($packageFull  -replace '\\', '/')
    template_root               = ($templateFull -replace '\\', '/')
    package_files_summary       = $packageFiles
    template_files_summary      = $templateFiles
    expected_paths_present      = $expected
    root_mod_info_present       = $rootModInfo
    nested_mod_info_present     = $nestedModInfoPresent
    package_mod_id              = $packageModId
    template_mod_id             = $templateModId
    workshop_txt_fields         = $workshopTxtKv
    package_mod_info_fields     = if ($nestedModInfoPresent) { $nestedModInfoKv } else { $rootModInfoKv }
    template_mod_info_fields    = $templateModInfoKv
    package_map_info_fields     = $nestedMapInfoKv
    template_map_info_fields    = $templateMapInfoKv
    copied_files                = $false
    modified_input_files        = $false
    binary_files_read           = $false
    pz_assets_copied            = $false
    playable_export_claimed     = $false
    load_tested                 = $false
}

$jsonPath = Join-Path $outputFull 'build42-mod-package-inspection.json'
$evidence | ConvertTo-Json -Depth 5 | Set-Content -Path $jsonPath -Encoding UTF8

# ---------------------------------------------------------------------------
# Write Markdown output
# ---------------------------------------------------------------------------

$checklistRows = foreach ($k in $expected.Keys) {
    $icon = if ($expected[$k]) { 'YES' } else { 'NO ' }
    "| $icon | $k |"
}
$checklistTable = $checklistRows -join "`n"

$modInfoRows = foreach ($k in $allModInfoKeys) {
    $pkgVal = if ($nestedModInfoKv.Contains($k)) { $nestedModInfoKv[$k] } elseif ($rootModInfoKv.Contains($k)) { "[root] $($rootModInfoKv[$k])" } else { '(absent)' }
    $tplVal = if ($templateModInfoKv.Contains($k)) { $templateModInfoKv[$k] } else { '(absent)' }
    "| ``$k`` | $pkgVal | $tplVal |"
}
$modInfoTable = $modInfoRows -join "`n"

$pkgFileRows = ($packageFiles | ForEach-Object { "| ``$($_.relative_path)`` | $($_.size_bytes) |" }) -join "`n"
$tplFileRows = ($templateFiles | ForEach-Object { "| ``$($_.relative_path)`` | $($_.size_bytes) |" }) -join "`n"

$md = @"
# Build 42 Mod Package Inspection

Schema:         pzmapforge.build42-mod-package-inspection.v0.1
Claim boundary: packaging_diagnostic_only_not_load_tested
Generated:      $generatedAt
Package:        $($packageFull -replace '\\', '/')
Template:       $($templateFull -replace '\\', '/')

## Package structure checklist

| Present | Expected path |
|---|---|
$checklistTable

## Package files

| Relative path | Bytes |
|---|---:|
$pkgFileRows

## Template files (ModTemplate reference)

| Relative path | Bytes |
|---|---:|
$tplFileRows

## mod.info field comparison

| Field | Package | Template |
|---|---|---|
$modInfoTable

## Non-claims

- Packaging diagnostic only.
- No load test performed.
- No binary files read (.lotheader, .lotpack, .bin).
- No PZ assets copied.
- No playable export claimed.
- No input files copied or modified.
- Output is under .local only.
"@

$mdPath = Join-Path $outputFull 'build42-mod-package-inspection.md'
Set-Content -Path $mdPath -Value $md -Encoding UTF8

# ---------------------------------------------------------------------------
# Summary
# ---------------------------------------------------------------------------

Write-Output "Inspection JSON:   $jsonPath"
Write-Output "Inspection MD:     $mdPath"
Write-Output "Package mod_id:    $(if ($packageModId) { $packageModId } else { '(not detected)' })"
Write-Output "Template mod_id:   $(if ($templateModId) { $templateModId } else { '(not detected)' })"
Write-Output "copied_files:             false"
Write-Output "binary_files_read:        false"
Write-Output "pz_assets_copied:         false"
Write-Output "playable_export_claimed:  false"
Write-Output "load_tested:              false"
Write-Output "Status:                   OK"
