#Requires -Version 5.1
<#
.SYNOPSIS
    Local-only smoke script for the map-scaffold command (MAP-3B/MAP-3C).

    Runs map-scaffold against examples/map-source/minimal-cell.json and verifies:
      - exactly four text files written under .local
      - no compiled output extensions present
      - boundary language present in all generated files
      - required safety lines present in stdout

    Output: .local\map-scaffold\minimal-cell-smoke (gitignored)
    Claim boundary: map_scaffold_text_only_not_compiled_not_pz_load_tested
    No PZ assets read or copied. No media/maps writes outside .local.
    No compiled outputs. No playable export.
#>

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$scriptDir  = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot   = Split-Path -Parent $scriptDir
$sourcePath = Join-Path $repoRoot 'examples\map-source\minimal-cell.json'
$outputDir  = Join-Path $repoRoot '.local\map-scaffold\minimal-cell-smoke'
$mapId      = 'deadmtl_minimal_test'

$pass = 0
$fail = 0

function Assert-True {
    param([bool]$Condition, [string]$Label)
    if ($Condition) { Write-Output "  PASS  $Label"; $script:pass++ }
    else            { Write-Output "  FAIL  $Label"; $script:fail++ }
}

Write-Output 'smoke-map-scaffold-minimal.ps1'
Write-Output "Root:   $repoRoot"
Write-Output "Source: $sourcePath"
Write-Output "Output: $outputDir"
Write-Output ''

# ---------------------------------------------------------------------------
# Step 1: required source file
# ---------------------------------------------------------------------------

Write-Output '--- Required files ---'
Assert-True (Test-Path -LiteralPath $sourcePath) 'source file exists: examples\map-source\minimal-cell.json'

if ($fail -gt 0) {
    Write-Error 'Required source file missing. Aborting.'
    exit 1
}

# ---------------------------------------------------------------------------
# Step 2: clean prior output and run map-scaffold
# ---------------------------------------------------------------------------

Write-Output ''
Write-Output '--- Running map-scaffold ---'

if (Test-Path -LiteralPath $outputDir) {
    Remove-Item -LiteralPath $outputDir -Recurse -Force -ErrorAction SilentlyContinue
}

$stdoutLines = & dotnet run --project (Join-Path $repoRoot 'src\PZMapForge.Cli') `
    --configuration Release --no-build `
    -- map-scaffold `
    --source $sourcePath `
    --output $outputDir
$exitCode = $LASTEXITCODE

$stdoutLines | ForEach-Object { Write-Output "  $_" }
Write-Output ''

# ---------------------------------------------------------------------------
# Step 3: exit code
# ---------------------------------------------------------------------------

Write-Output '--- Exit code ---'
Assert-True ($exitCode -eq 0) "map-scaffold exits 0 (got $exitCode)"

if ($exitCode -ne 0) {
    Write-Error 'map-scaffold failed. Aborting.'
    exit 1
}

# ---------------------------------------------------------------------------
# Step 4: four expected files exist
# ---------------------------------------------------------------------------

Write-Output ''
Write-Output '--- Expected files ---'
$modInfo = Join-Path $outputDir 'mod.info'
$mapInfo = Join-Path $outputDir "media\maps\$mapId\map.info"
$spawn   = Join-Path $outputDir "media\maps\$mapId\spawnpoints.lua"
$readme  = Join-Path $outputDir "media\maps\$mapId\README_PZMAPFORGE_BOUNDARY.txt"

Assert-True (Test-Path -LiteralPath $modInfo) 'mod.info exists'
Assert-True (Test-Path -LiteralPath $mapInfo) "media\maps\$mapId\map.info exists"
Assert-True (Test-Path -LiteralPath $spawn)   "media\maps\$mapId\spawnpoints.lua exists"
Assert-True (Test-Path -LiteralPath $readme)  "media\maps\$mapId\README_PZMAPFORGE_BOUNDARY.txt exists"

# ---------------------------------------------------------------------------
# Step 5: exactly four files total
# ---------------------------------------------------------------------------

Write-Output ''
Write-Output '--- File count ---'
$allFiles = @(Get-ChildItem -LiteralPath $outputDir -Recurse -File -ErrorAction SilentlyContinue)
Assert-True ($allFiles.Count -eq 4) "exactly 4 files written (got $($allFiles.Count))"

# ---------------------------------------------------------------------------
# Step 6: no compiled output extensions
# ---------------------------------------------------------------------------

Write-Output ''
Write-Output '--- No compiled output extensions ---'
$forbidden = @('.lotpack', '.lotheader', '.bin', '.tmx', '.pzw')
$badFiles  = @($allFiles | Where-Object { $forbidden -contains $_.Extension.ToLowerInvariant() })
Assert-True ($badFiles.Count -eq 0) 'no compiled output files (.lotpack/.lotheader/.bin/.tmx/.pzw)'

# ---------------------------------------------------------------------------
# Step 7: boundary language in generated file content
# ---------------------------------------------------------------------------

Write-Output ''
Write-Output '--- Boundary language ---'
$modInfoText = Get-Content -LiteralPath $modInfo -Raw
$mapInfoText = Get-Content -LiteralPath $mapInfo -Raw
$spawnText   = Get-Content -LiteralPath $spawn   -Raw
$readmeText  = Get-Content -LiteralPath $readme  -Raw
$allText     = $modInfoText + $mapInfoText + $spawnText + $readmeText

Assert-True ($allText -imatch 'text-only scaffold')                  "boundary: 'Text-only scaffold' present in files"
Assert-True ($allText -imatch 'not playable|not a playable')         "boundary: 'Not playable' present in files"
Assert-True ($allText -imatch 'no pz assets|pz_assets_included=false') "boundary: 'No PZ assets' present in files"
Assert-True ($allText -imatch 'not load-tested')                     "boundary: 'Not load-tested' present in files"

# ---------------------------------------------------------------------------
# Step 8: stdout safety lines
# ---------------------------------------------------------------------------

Write-Output ''
Write-Output '--- Stdout safety lines ---'
$stdout = ($stdoutLines | Out-String)

Assert-True ($stdout -imatch 'text_only_scaffold_written:\s*true')  'stdout: text_only_scaffold_written: true'
Assert-True ($stdout -imatch 'compiled_outputs_written:\s*false')   'stdout: compiled_outputs_written: false'
Assert-True ($stdout -imatch 'playable_export_generated:\s*false')  'stdout: playable_export_generated: false'
Assert-True ($stdout -imatch 'pz_assets_read_or_copied:\s*false')   'stdout: pz_assets_read_or_copied: false'

# ---------------------------------------------------------------------------
# Summary
# ---------------------------------------------------------------------------

Write-Output ''
Write-Output '========================================'
Write-Output 'smoke-map-scaffold-minimal summary'
Write-Output '========================================'
Write-Output ("  Results:        $pass passed, $fail failed")
Write-Output "  Output:         $outputDir"
Write-Output '  Claim boundary: map_scaffold_text_only_not_compiled_not_pz_load_tested'
Write-Output '  Local only:     true'
Write-Output '  PZ assets:      false'
Write-Output '  Compiled out:   false'
Write-Output '  Playable:       false'
Write-Output ''

if ($fail -gt 0) { exit 1 }
exit 0
