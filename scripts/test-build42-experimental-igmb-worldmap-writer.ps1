#Requires -Version 5.1
<#
.SYNOPSIS
    Tests for the MAP-8Y experimental IGMB worldmap writer skeleton.
    30 assertions. All output under .local/.
#>

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$scriptDir    = Split-Path -Parent $MyInvocation.MyCommand.Path
$writerScript = Join-Path $scriptDir 'write-build42-experimental-igmb-worldmap.ps1'
$testOutputDir = Join-Path $scriptDir '.local\test-map8y-writer-output'

$pass = 0
$fail = 0

function Assert-True {
    param([bool]$cond, [string]$msg)
    if ($cond) { Write-Output "PASS: $msg"; $script:pass++ }
    else        { Write-Output "FAIL: $msg"; $script:fail++ }
}

function Assert-Exit-NonZero {
    param([string]$label, [string]$outputPath)
    $savedEAP = $ErrorActionPreference
    $ErrorActionPreference = 'Continue'
    & powershell -ExecutionPolicy Bypass -File $writerScript -Output $outputPath 2>$null
    $ec = $LASTEXITCODE
    $ErrorActionPreference = $savedEAP
    Assert-True ($ec -ne 0) "$label exits nonzero (got $ec)"
}

# 1. Guard: no .local in path
Assert-Exit-NonZero '.local guard' "$env:TEMP\pzmf-map8y-guard-no-local"

# 2. Guard: media/maps
Assert-Exit-NonZero 'media\maps guard' "$env:TEMP\.local-x\media\maps\out"

# 3. Guard: Steam
Assert-Exit-NonZero 'Steam guard' "$env:TEMP\.local-x\Steam\out"

# 4. Guard: workshop
Assert-Exit-NonZero 'workshop guard' "$env:TEMP\.local-x\workshop\out"

# 5. Guard: ProjectZomboid
Assert-Exit-NonZero 'ProjectZomboid guard' "$env:TEMP\.local-x\ProjectZomboid\out"

# Run the writer with a valid .local path
if (Test-Path -LiteralPath $testOutputDir) {
    Remove-Item -LiteralPath $testOutputDir -Recurse -Force
}
$savedEAP2 = $ErrorActionPreference
$ErrorActionPreference = 'Continue'
& powershell -ExecutionPolicy Bypass -File $writerScript -Output $testOutputDir
$writerExit = $LASTEXITCODE
$ErrorActionPreference = $savedEAP2

# 6. Writer exits 0
Assert-True ($writerExit -eq 0) 'writer exits 0 for .local path'

$binPath      = Join-Path $testOutputDir 'worldmap.xml.bin'
$manifestJson = Join-Path $testOutputDir 'worldmap-writer-manifest.json'
$manifestMd   = Join-Path $testOutputDir 'worldmap-writer-manifest.md'

# 7. worldmap.xml.bin exists
Assert-True (Test-Path -LiteralPath $binPath) 'worldmap.xml.bin exists'

# 8. Manifest JSON exists
Assert-True (Test-Path -LiteralPath $manifestJson) 'worldmap-writer-manifest.json exists'

# 9. Manifest MD exists
Assert-True (Test-Path -LiteralPath $manifestMd) 'worldmap-writer-manifest.md exists'

# Load bytes and manifest
$fileBytes = [System.IO.File]::ReadAllBytes($binPath)
$manifest  = Get-Content -LiteralPath $manifestJson -Raw | ConvertFrom-Json

# 10. File size == 65536
Assert-True ($fileBytes.Length -eq 65536) "file size == 65536 (got $($fileBytes.Length))"

# 11. IGMB magic at bytes[0..3]
$magic = [System.Text.Encoding]::ASCII.GetString($fileBytes, 0, 4)
Assert-True ($magic -eq 'IGMB') "bytes[0..3] == IGMB (got '$magic')"

function Read-U32LE { param([byte[]]$b, [int]$off)
    return [uint32]([int]($b[$off]) -bor ([int]($b[$off+1]) -shl 8) -bor ([int]($b[$off+2]) -shl 16) -bor ([int]($b[$off+3]) -shl 24))
}
function Read-U16LE { param([byte[]]$b, [int]$off)
    return [uint16]([int]($b[$off]) -bor ([int]($b[$off+1]) -shl 8))
}

# 12. Offset 4 U32LE == 2 (version)
$v4 = Read-U32LE $fileBytes 4
Assert-True ($v4 -eq 2) "offset 4 U32LE == 2 (got $v4)"

# 13. Offset 8 U32LE == 256 (unknown_a)
$v8 = Read-U32LE $fileBytes 8
Assert-True ($v8 -eq 256) "offset 8 U32LE == 256 (got $v8)"

# 14. Offset 12 U32LE == 59 (unknown_b)
$v12 = Read-U32LE $fileBytes 12
Assert-True ($v12 -eq 59) "offset 12 U32LE == 59 (got $v12)"

# 15. Offset 16 U32LE == 68 (unknown_c)
$v16 = Read-U32LE $fileBytes 16
Assert-True ($v16 -eq 68) "offset 16 U32LE == 68 (got $v16)"

# 16. Offset 20 U32LE == 12 (string_pool_count)
$v20 = Read-U32LE $fileBytes 20
Assert-True ($v20 -eq 12) "offset 20 U32LE == 12 (string_pool_count, got $v20)"

# 17. bytes[24..25] U16LE == 7 (length of first string 'Polygon')
$firstLen = Read-U16LE $fileBytes 24
Assert-True ($firstLen -eq 7) "bytes[24..25] U16LE == 7 (Polygon length, got $firstLen)"

# 18. bytes[26..32] == 'Polygon'
$polygonStr = [System.Text.Encoding]::ASCII.GetString($fileBytes, 26, 7)
Assert-True ($polygonStr -eq 'Polygon') "bytes[26..32] == 'Polygon' (got '$polygonStr')"

# 19. byte[133] == 0xFF (FF padding starts at StringPoolEndOffset)
Assert-True ($fileBytes[133] -eq 0xFF) "byte[133] == 0xFF (got $($fileBytes[133]))"

# 20. All bytes 133..6388 are 0xFF (FF padding region)
$ffOk = $true
for ($i = 133; $i -le 6388; $i++) {
    if ($fileBytes[$i] -ne 0xFF) { $ffOk = $false; break }
}
Assert-True $ffOk 'bytes 133..6388 are all 0xFF'

# 21. Offset 6389 U32LE == 30 (triplet first, MAP-8X evidence)
$t1 = Read-U32LE $fileBytes 6389
Assert-True ($t1 -eq 30) "offset 6389 U32LE == 30 (got $t1)"

# 22. Offset 6393 U32LE == 26 (triplet second)
$t2 = Read-U32LE $fileBytes 6393
Assert-True ($t2 -eq 26) "offset 6393 U32LE == 26 (got $t2)"

# 23. Offset 6397 U32LE == 9 (triplet third)
$t3 = Read-U32LE $fileBytes 6397
Assert-True ($t3 -eq 9) "offset 6397 U32LE == 9 (got $t3)"

# 24. File does not contain 'Project Russia' ASCII text
$fileText = [System.Text.Encoding]::ASCII.GetString($fileBytes)
Assert-True (-not $fileText.Contains('Project Russia')) 'file does not contain Project Russia ASCII text'

# 25. Manifest schema correct
$schema = $manifest.schema
Assert-True ($schema -eq 'pzmapforge.map8y-experimental-igmb-writer-manifest.v0.1') "manifest schema correct (got '$schema')"

# 26. third_party_bytes_copied == false
Assert-True ($manifest.third_party_bytes_copied -eq $false) 'manifest third_party_bytes_copied == false'

# 27. project_russia_file_read == false
Assert-True ($manifest.project_russia_file_read -eq $false) 'manifest project_russia_file_read == false'

# 28. playable_claim_allowed == false
Assert-True ($manifest.playable_claim_allowed -eq $false) 'manifest playable_claim_allowed == false'

# 29. writer_status == 'experimental_skeleton_not_load_proven'
$ws = $manifest.writer_status
Assert-True ($ws -eq 'experimental_skeleton_not_load_proven') "manifest writer_status correct (got '$ws')"

# 30. SHA-256 in manifest matches computed hash of file
$sha = [System.Security.Cryptography.SHA256]::Create()
$hashBytes = $null
try { $hashBytes = $sha.ComputeHash($fileBytes) } finally { $sha.Dispose() }
$sha256Actual = ($hashBytes | ForEach-Object { $_.ToString('x2') }) -join ''
Assert-True ($sha256Actual -eq $manifest.sha256) 'SHA-256 in manifest matches file'

# Report
Write-Output ""
Write-Output "Results: $pass passed, $fail failed"
if ($fail -gt 0) { exit 1 }
exit 0
