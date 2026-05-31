#Requires -Version 5.1
<#
.SYNOPSIS
    TMX structural integrity validator for PZMapForge.

    Validates .local/mapforge/parsed-cell-basic.tmx:
    - XML structure and required attributes (map, tileset, layer, data)
    - base64 layer payload decodes and gzip-decompresses correctly
    - decompressed byte length == 300 * 300 * 4 (360000)
    - uint32 GID count == 90000
    - all GIDs are in valid palette range 1..9

    If the TMX is missing, runs new-test-image.ps1 + image-mapforge.ps1
    directly to avoid a recursion loop with validate.ps1.
    Does not touch media/maps. Does not commit .local/.
    Does not claim playable Project Zomboid export.
#>

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

Add-Type -AssemblyName 'System.IO.Compression'

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot  = Split-Path -Parent $scriptDir
$outputDir = Join-Path $repoRoot '.local\mapforge'
$tmxPath   = Join-Path $outputDir 'parsed-cell-basic.tmx'

$pass = 0
$fail = 0

function Assert-True {
    param([bool]$Condition, [string]$Label)
    if ($Condition) { Write-Output "  PASS  $Label"; $script:pass++ }
    else            { Write-Output "  FAIL  $Label"; $script:fail++ }
}

# ---------------------------------------------------------------------------
# Ensure TMX exists — run image-mapforge.ps1 directly, not validate.ps1,
# to avoid a recursion loop (validate.ps1 calls this script)
# ---------------------------------------------------------------------------

if (-not (Test-Path $tmxPath -PathType Leaf)) {
    Write-Output "parsed-cell-basic.tmx not found. Generating via image-mapforge.ps1..."
    $sampleImg = Join-Path $outputDir 'sample-input.png'
    if (-not (Test-Path $sampleImg -PathType Leaf)) {
        & powershell -ExecutionPolicy Bypass -File (Join-Path $repoRoot 'scripts\new-test-image.ps1')
        if ($LASTEXITCODE -ne 0) { Write-Error "new-test-image.ps1 failed."; exit 1 }
    }
    & powershell -ExecutionPolicy Bypass -File (Join-Path $repoRoot 'source\image-mapforge.ps1') `
        -ImagePath $sampleImg
    if ($LASTEXITCODE -ne 0) { Write-Error "image-mapforge.ps1 failed."; exit 1 }
}

Write-Output "TMX integrity validation: $tmxPath"
Write-Output ""

# ---------------------------------------------------------------------------
# File check
# ---------------------------------------------------------------------------

Write-Output "--- File ---"
Assert-True (Test-Path $tmxPath -PathType Leaf) "TMX file exists"

if ($fail -gt 0) {
    Write-Output ""; Write-Output "--- ABORT: TMX missing ---"
    Write-Output "Results: $pass passed, $fail failed"; exit 1
}

# ---------------------------------------------------------------------------
# XML parse
# ---------------------------------------------------------------------------

Write-Output ""
Write-Output "--- XML structure ---"

$xml = $null
$xmlOk = $false
try {
    $xml   = [xml](Get-Content -LiteralPath $tmxPath -Raw -Encoding UTF8)
    $xmlOk = $true
    Assert-True $true "XML parses without error"
}
catch {
    Write-Output "  FAIL  XML parses without error ($($_.Exception.Message))"
    $script:fail++
}

if (-not $xmlOk) {
    Write-Output ""; Write-Output "--- ABORT: XML invalid ---"
    Write-Output "Results: $pass passed, $fail failed"; exit 1
}

# ---------------------------------------------------------------------------
# Map-level attributes
# ---------------------------------------------------------------------------

$map = $xml.map
Assert-True ($map.version     -eq '1.0')         "map version == '1.0'"
Assert-True ($map.orientation -eq 'orthogonal')   "orientation == 'orthogonal'"
Assert-True ([int]$map.width  -eq 300)            "map width == 300"
Assert-True ([int]$map.height -eq 300)            "map height == 300"
Assert-True ([int]$map.tilewidth  -eq 32)         "map tilewidth == 32"
Assert-True ([int]$map.tileheight -eq 32)         "map tileheight == 32"

# ---------------------------------------------------------------------------
# Tileset attributes
# ---------------------------------------------------------------------------

Write-Output ""
Write-Output "--- Tileset ---"
$ts = $map.tileset
Assert-True ([int]$ts.firstgid -eq 1)                            "tileset firstgid == 1"
Assert-True ($ts.image.source -eq 'parsed-cell-tiles.png')       "tileset image source == 'parsed-cell-tiles.png'"

# ---------------------------------------------------------------------------
# Layer attributes
# ---------------------------------------------------------------------------

Write-Output ""
Write-Output "--- Layer ---"
$layer = $map.layer
Assert-True ($null -ne $layer -and $layer.name -eq 'Ground')  "layer named 'Ground' exists"
Assert-True ([int]$layer.width  -eq 300)                       "layer width == 300"
Assert-True ([int]$layer.height -eq 300)                       "layer height == 300"

$data = $layer.data
Assert-True ($data.encoding    -eq 'base64') "data encoding == 'base64'"
Assert-True ($data.compression -eq 'gzip')   "data compression == 'gzip'"

# ---------------------------------------------------------------------------
# Payload: base64 decode
# ---------------------------------------------------------------------------

Write-Output ""
Write-Output "--- Payload ---"

$rawB64        = $data.InnerText.Trim()
$compressedBytes = $null
$b64Ok = $false
try {
    $compressedBytes = [Convert]::FromBase64String($rawB64)
    $b64Ok = $true
    Assert-True $true "base64 decodes without error"
}
catch {
    Write-Output "  FAIL  base64 decodes without error ($($_.Exception.Message))"
    $script:fail++
}

# Gzip decompress
$decompressed = $null
$gzOk = $false
if ($b64Ok) {
    try {
        $ms  = [System.IO.MemoryStream]::new($compressedBytes, 0, $compressedBytes.Length)
        $gz  = [System.IO.Compression.GZipStream]::new($ms, [System.IO.Compression.CompressionMode]::Decompress)
        $out = [System.IO.MemoryStream]::new()
        $gz.CopyTo($out)
        $gz.Close()
        $decompressed = $out.ToArray()
        $out.Dispose()
        $ms.Dispose()
        $gzOk = $true
        Assert-True $true "gzip decompresses without error"
    }
    catch {
        Write-Output "  FAIL  gzip decompresses without error ($($_.Exception.Message))"
        $script:fail++
    }
}
else {
    Write-Output "  SKIP  gzip decompresses (base64 failed)"
}

# Byte length
if ($gzOk) {
    $expectedBytes = 300 * 300 * 4   # 360000
    Assert-True ($decompressed.Length -eq $expectedBytes) `
        "decompressed byte length == $expectedBytes (got $($decompressed.Length))"

    # Convert bytes -> uint32[]
    $gidCount = $decompressed.Length / 4
    $gids     = New-Object uint32[] $gidCount
    [System.Buffer]::BlockCopy($decompressed, 0, $gids, 0, $decompressed.Length)

    Assert-True ($gids.Length -eq 90000) "uint32 GID count == 90000 (got $($gids.Length))"

    # Range validation — single pass for min/max
    $minGid = [uint32]4294967295   # uint32.MaxValue
    $maxGid = [uint32]0
    for ($i = 0; $i -lt $gids.Length; $i++) {
        if ($gids[$i] -lt $minGid) { $minGid = $gids[$i] }
        if ($gids[$i] -gt $maxGid) { $maxGid = $gids[$i] }
    }
    Assert-True ($minGid -ge 1) "all GIDs >= 1 (no zero GIDs; min was $minGid)"
    Assert-True ($maxGid -le 9) "all GIDs <= 9 (valid palette range; max was $maxGid)"
}
else {
    Write-Output "  SKIP  payload size check (decompress failed)"
    Write-Output "  SKIP  GID count (decompress failed)"
    Write-Output "  SKIP  GID range check (decompress failed)"
}

# ---------------------------------------------------------------------------
# Summary
# ---------------------------------------------------------------------------

Write-Output ""
Write-Output "----------------------------------------"
Write-Output "Results: $pass passed, $fail failed"
Write-Output "----------------------------------------"

if ($fail -gt 0) { exit 1 }
exit 0
