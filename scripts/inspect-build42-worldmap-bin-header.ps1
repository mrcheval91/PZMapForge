[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$CandidateWorldmapBinPath,
    [Parameter(Mandatory)][string]$ReferenceWorldmapBinPath,
    [Parameter(Mandatory)][string]$Output
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

if (-not $Output.Contains('.local')) {
    Write-Error "-Output must be a path under .local/ (got: $Output)"
    exit 1
}

$outDir = $Output
if (-not (Test-Path $outDir)) {
    New-Item -ItemType Directory -Force -Path $outDir | Out-Null
}

$maxBytes = 64

function Read-BinHeader([string]$filePath) {
    $r = [ordered]@{
        present            = $false
        size_bytes         = [long]0
        bytes_read_count   = 0
        first_16_bytes_hex = ''
        first_64_bytes_hex = ''
        ascii_preview      = ''
        detected_signature = 'unknown'
    }
    if (-not (Test-Path -LiteralPath $filePath)) {
        return $r
    }
    $r.present = $true
    $info = Get-Item -LiteralPath $filePath
    $r.size_bytes = [long]$info.Length

    $readCount = [int][Math]::Min($maxBytes, $r.size_bytes)
    if ($readCount -eq 0) { return $r }

    $buf = [byte[]]::new($readCount)
    $fs = [System.IO.FileStream]::new($filePath, [System.IO.FileMode]::Open, [System.IO.FileAccess]::Read, [System.IO.FileShare]::Read)
    try {
        $actual = $fs.Read($buf, 0, $readCount)
        if ($actual -lt $readCount) { $buf = $buf[0..($actual - 1)] }
        $r.bytes_read_count = $actual
    } finally {
        $fs.Close()
    }

    $hexAll = ($buf | ForEach-Object { $_.ToString('X2') }) -join ' '
    $r.first_64_bytes_hex = $hexAll
    $r.first_16_bytes_hex = if ($buf.Count -ge 16) {
        ($buf[0..15] | ForEach-Object { $_.ToString('X2') }) -join ' '
    } else { $hexAll }

    $r.ascii_preview = -join ($buf | ForEach-Object {
        if ($_ -ge 0x20 -and $_ -le 0x7E) { [char]$_ } else { '.' }
    })

    if ($buf.Count -ge 2) {
        $b0 = $buf[0]; $b1 = $buf[1]
        if ($b0 -eq 0x1F -and $b1 -eq 0x8B) {
            $r.detected_signature = 'gzip'
        } elseif ($b0 -eq 0x78 -and ($b1 -eq 0x01 -or $b1 -eq 0x5E -or $b1 -eq 0x9C -or $b1 -eq 0xDA)) {
            $r.detected_signature = 'zlib'
        } elseif ($b0 -eq 0x50 -and $b1 -eq 0x4B) {
            $r.detected_signature = 'zip'
        } elseif ($buf.Count -ge 4 -and $b0 -eq 0x49 -and $b1 -eq 0x47 -and $buf[2] -eq 0x4D -and $buf[3] -eq 0x42) {
            $r.detected_signature = 'igmb'
        } elseif ($buf.Count -ge 4 -and $b0 -eq 0x53 -and $b1 -eq 0x51 -and $buf[2] -eq 0x4C -and $buf[3] -eq 0x69) {
            $r.detected_signature = 'sqlite'
        } elseif ($b0 -eq 0x3C) {
            $r.detected_signature = 'xml_or_text'
        }
    }

    return $r
}

$schema = 'pzmapforge.map8o-worldmap-bin-header-inspection.v0.1'

$cand = Read-BinHeader $CandidateWorldmapBinPath
$ref  = Read-BinHeader $ReferenceWorldmapBinPath

$result = [ordered]@{
    schema                          = $schema
    candidate_present               = $cand.present
    candidate_size_bytes            = $cand.size_bytes
    candidate_bytes_read_count      = $cand.bytes_read_count
    candidate_first_16_bytes_hex    = $cand.first_16_bytes_hex
    candidate_first_64_bytes_hex    = $cand.first_64_bytes_hex
    candidate_ascii_preview         = $cand.ascii_preview
    candidate_detected_signature    = $cand.detected_signature
    reference_present               = $ref.present
    reference_size_bytes            = $ref.size_bytes
    reference_bytes_read_count      = $ref.bytes_read_count
    reference_first_16_bytes_hex    = $ref.first_16_bytes_hex
    reference_first_64_bytes_hex    = $ref.first_64_bytes_hex
    reference_ascii_preview         = $ref.ascii_preview
    reference_detected_signature    = $ref.detected_signature
    max_bytes_allowed               = $maxBytes
    binary_contents_read_scope      = 'first_64_bytes_only'
    binary_contents_full_read       = $false
    third_party_files_copied        = $false
    playable_claim_allowed          = $false
    binary_writer_gate_closed       = $true
    next_branch                     = 'worldmap_xml_bin_minimal_pzmapforge_owned_encoder_research_pending_evidence'
}

$jsonPath = Join-Path $outDir 'worldmap-bin-header-inspection.json'
$mdPath   = Join-Path $outDir 'worldmap-bin-header-inspection.md'

$result | ConvertTo-Json -Depth 4 | Set-Content -Encoding UTF8 -Path $jsonPath

$mdLines = @(
    '# MAP-8O: Worldmap Bin Header Inspection'
    ''
    "Schema: ``$schema``"
    ''
    '| Field | Value |'
    '|-------|-------|'
)
foreach ($key in $result.Keys) {
    $val = $result[$key]
    $display = if ($val -is [bool]) { $val.ToString().ToLower() } else { [string]$val }
    $mdLines += "| $key | $display |"
}
$mdLines | Set-Content -Encoding UTF8 -Path $mdPath

Write-Host "worldmap-bin-header-inspection.json -> $jsonPath"
Write-Host "worldmap-bin-header-inspection.md   -> $mdPath"
exit 0
