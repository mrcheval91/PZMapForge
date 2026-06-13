# validate-layer-pack.ps1
# Validates that all expected layer files exist and the project manifest is well-formed.
#
# Usage:
#   powershell -ExecutionPolicy Bypass -File scripts\validate-layer-pack.ps1

$root    = $PSScriptRoot | Split-Path -Parent
$errors  = @()
$ok      = 0

function Check-File([string]$rel) {
    $path = Join-Path $root $rel
    if (Test-Path $path) {
        Write-Host "  OK  $rel"
        $script:ok++
    } else {
        Write-Host "  MISSING  $rel"
        $script:errors += "Missing: $rel"
    }
}

Write-Host "=== DeadMTL Layer Pack Validation ==="
Write-Host ""

Write-Host "--- Project files ---"
Check-File "deadmtl_worldgen_project.json"
Check-File "README.md"

Write-Host ""
Write-Host "--- Palette files ---"
Check-File "palettes\worldgen-png-palette.json"
Check-File "palettes\zoning-palette.json"
Check-File "palettes\metadata-palette.json"

Write-Host ""
Write-Host "--- System 1 layers (compile-supported) ---"
Check-File "layers\water.png"
Check-File "layers\shore.png"
Check-File "layers\parks_forest.png"
Check-File "layers\roads_major.png"

Write-Host ""
Write-Host "--- System 2-4 placeholder layers ---"
Check-File "layers\roads_local.png"
Check-File "layers\zones_residential.png"
Check-File "layers\zones_commercial.png"
Check-File "layers\zones_industrial.png"
Check-File "layers\placed_buildings.png"
Check-File "layers\props.png"
Check-File "layers\npc_zones.png"
Check-File "layers\ownership.png"

Write-Host ""
Write-Host "--- Project manifest format check ---"
$manifestPath = Join-Path $root "deadmtl_worldgen_project.json"
if (Test-Path $manifestPath) {
    $json = Get-Content $manifestPath -Raw | ConvertFrom-Json
    $format = $json.format
    if ($format -eq "pzmapforge.worldgen.project.v1") {
        Write-Host "  OK  format = $format"
        $ok++
    } else {
        Write-Host "  INVALID  format = $format"
        $errors += "Wrong format: $format"
    }

    $supportedIds = @("water", "shore", "parks_forest", "roads_major")
    $layerIds     = $json.layers | ForEach-Object { $_.id }
    $unsupported  = $layerIds | Where-Object { $_ -notin $supportedIds }
    if ($unsupported) {
        Write-Host "  INVALID  unsupported layer ids in project: $($unsupported -join ', ')"
        $errors += "Unsupported layers in project: $($unsupported -join ', ')"
    } else {
        Write-Host "  OK  only supported layers in project: $($layerIds -join ', ')"
        $ok++
    }
}

Write-Host ""
if ($errors.Count -eq 0) {
    Write-Host "Status: OK ($ok checks passed)"
    exit 0
} else {
    Write-Host "Status: INVALID ($($errors.Count) errors)"
    foreach ($e in $errors) { Write-Host "  error: $e" }
    exit 1
}
