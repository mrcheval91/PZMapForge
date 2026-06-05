#Requires -Version 5.1
<#
.SYNOPSIS
    Tests for inspect-build42-lotp-payload-windows.ps1 (MAP-6K).

    Creates synthetic LOTP/LOTH/chunkdata fixtures under temp .local and
    validates the payload window report. Does not use real PZ files.
    Expected assertion count: 20
#>

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$scriptDir    = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot     = Split-Path -Parent $scriptDir
$inspector    = Join-Path $repoRoot 'scripts\inspect-build42-lotp-payload-windows.ps1'
$tempRoot     = [System.IO.Path]::GetTempPath()

$pass = 0
$fail = 0

function Assert-True {
    param([bool]$Condition, [string]$Label)
    if ($Condition) { Write-Output "  PASS  $Label"; $script:pass++ }
    else            { Write-Output "  FAIL  $Label"; $script:fail++ }
}

Write-Output 'test-build42-lotp-payload-windows.ps1'
Write-Output ''

# ---------------------------------------------------------------------------
# Setup: temp .local directories
# ---------------------------------------------------------------------------

$testBase   = Join-Path $tempRoot ('pzmapforge-lotp-test-' + [System.IO.Path]::GetRandomFileName())
$testSource = Join-Path $testBase '.local\source'
$badPath    = Join-Path $tempRoot 'not-local-lotp'

New-Item -ItemType Directory -Force -Path $testSource | Out-Null
New-Item -ItemType Directory -Force -Path $badPath    | Out-Null

# ---------------------------------------------------------------------------
# Synthetic LOTP lotpack
# Build: header(12) + offset_table(1024*8=8192) + 1024 payload chunks(64 bytes each)
# total: 12 + 8192 + 65536 = 73740 bytes
# ---------------------------------------------------------------------------

$chunkCount  = 1024
$chunkSize   = 64
$tableSize   = $chunkCount * 8
$firstOffset = 12 + $tableSize   # = 8204

$lotpBytes = [byte[]]::new(12 + $tableSize + $chunkCount * $chunkSize)

# Header: LOTP + version=1 + chunk_count=1024
$lotpBytes[0] = 0x4C; $lotpBytes[1] = 0x4F; $lotpBytes[2] = 0x54; $lotpBytes[3] = 0x50
$lotpBytes[4] = 0x01                                  # version LE byte 0
$lotpBytes[8] = [byte]($chunkCount -band 0xFF)        # chunk_count LE byte 0 = 0x00
$lotpBytes[9] = [byte](($chunkCount -shr 8) -band 0xFF) # byte 1 = 0x04

# Offset table: entry[i] = firstOffset + i * chunkSize (sequential 64-byte chunks)
for ($i = 0; $i -lt $chunkCount; $i++) {
    $entryPos = 12 + $i * 8
    $offset   = $firstOffset + $i * $chunkSize
    # U64 LE: low U32 = offset, high U32 = 0
    $lotpBytes[$entryPos]     = [byte]($offset -band 0xFF)
    $lotpBytes[$entryPos + 1] = [byte](($offset -shr 8)  -band 0xFF)
    $lotpBytes[$entryPos + 2] = [byte](($offset -shr 16) -band 0xFF)
    $lotpBytes[$entryPos + 3] = [byte](($offset -shr 24) -band 0xFF)
    # high U32 = 0 (already zero-initialized)
}

# Payload: each chunk = 64 bytes of 0xAB (deterministic, non-zero)
$payloadStart = 12 + $tableSize
for ($i = 0; $i -lt $chunkCount * $chunkSize; $i++) {
    $lotpBytes[$payloadStart + $i] = 0xAB
}

[System.IO.File]::WriteAllBytes((Join-Path $testSource 'world_0_0.lotpack'), $lotpBytes)

# ---------------------------------------------------------------------------
# Synthetic LOTH lotheader
# LOTH + version=1 + entry_count=2 + two entries
# ---------------------------------------------------------------------------

$entry1 = [System.Text.Encoding]::ASCII.GetBytes("blends_grassoverlays_01_0`n")
$entry2 = [System.Text.Encoding]::ASCII.GetBytes("blends_natural_01_0`n")
$lothBytes = [byte[]]::new(12 + $entry1.Length + $entry2.Length)
$lothBytes[0] = 0x4C; $lothBytes[1] = 0x4F; $lothBytes[2] = 0x54; $lothBytes[3] = 0x48  # LOTH
$lothBytes[4] = 0x01                                                                       # version=1
$lothBytes[8] = 0x02                                                                       # entry_count=2
$entry1.CopyTo($lothBytes, 12)
$entry2.CopyTo($lothBytes, 12 + $entry1.Length)
[System.IO.File]::WriteAllBytes((Join-Path $testSource '0_0.lotheader'), $lothBytes)

# ---------------------------------------------------------------------------
# Synthetic chunkdata: 1026 bytes, 00 01 + 1024 zero bytes
# ---------------------------------------------------------------------------

$chunkdataBytes = [byte[]]::new(1026)
$chunkdataBytes[0] = 0x00; $chunkdataBytes[1] = 0x01
[System.IO.File]::WriteAllBytes((Join-Path $testSource 'chunkdata_0_0.bin'), $chunkdataBytes)

# ---------------------------------------------------------------------------
# Helper: run inspector without stopping on nonzero exit
# ---------------------------------------------------------------------------

function Invoke-Inspector {
    param([string[]]$ArgList)
    $saved = $ErrorActionPreference; $ErrorActionPreference = 'Continue'
    $null = & powershell -ExecutionPolicy Bypass -File $inspector @ArgList
    $ec = $LASTEXITCODE; $ErrorActionPreference = $saved; return $ec
}

# ---------------------------------------------------------------------------
# Test 1: Source outside .local exits nonzero
# ---------------------------------------------------------------------------

Write-Output '--- Test 1: Source outside .local exits nonzero ---'
$ec1 = Invoke-Inspector @('-Source', $badPath, '-Output', (Join-Path $testBase '.local\out'))
Assert-True ($ec1 -ne 0) 'Source outside .local exits nonzero'

# ---------------------------------------------------------------------------
# Test 2: Output outside .local exits nonzero
# ---------------------------------------------------------------------------

Write-Output '--- Test 2: Output outside .local exits nonzero ---'
$ec2 = Invoke-Inspector @('-Source', $testSource, '-Output', $badPath)
Assert-True ($ec2 -ne 0) 'Output outside .local exits nonzero'

# ---------------------------------------------------------------------------
# Test 3: Valid run exits 0
# ---------------------------------------------------------------------------

Write-Output '--- Test 3: Valid run exits 0 ---'
$validOut = Join-Path $testBase '.local\out'
$ec3 = Invoke-Inspector @('-Source', $testSource, '-Output', $validOut, '-MaxCells', '3', '-MaxChunksPerCell', '4', '-WindowBytes', '32')
Assert-True ($ec3 -eq 0) 'Valid run exits 0'

$jsonFile = Join-Path $validOut 'build42-lotp-payload-window-report.json'
$mdFile   = Join-Path $validOut 'build42-lotp-payload-window-report.md'
$r = Get-Content $jsonFile -Raw | ConvertFrom-Json

# ---------------------------------------------------------------------------
# Tests 4-5: Report files exist
# ---------------------------------------------------------------------------

Write-Output '--- Test 4: JSON report exists ---'
Assert-True (Test-Path $jsonFile) 'build42-lotp-payload-window-report.json exists'

Write-Output '--- Test 5: MD report exists ---'
Assert-True (Test-Path $mdFile) 'build42-lotp-payload-window-report.md exists'

# ---------------------------------------------------------------------------
# Tests 6-10: LOTP assertions
# ---------------------------------------------------------------------------

Write-Output '--- Test 6: LOTP magic detected ---'
$lotpRec = @($r.lotp_analysis.records) | Select-Object -First 1
Assert-True ($null -ne $lotpRec -and $lotpRec.magic -eq 'LOTP') 'LOTP magic == LOTP'

Write-Output '--- Test 7: LOTP chunk_count == 1024 ---'
Assert-True ($null -ne $lotpRec -and [int]$lotpRec.chunk_count -eq 1024) 'LOTP chunk_count == 1024'

Write-Output '--- Test 8: LOTP first_offset == 8204 ---'
Assert-True ($null -ne $lotpRec -and [long]$lotpRec.first_offset -eq 8204) 'LOTP first_offset == 8204'

Write-Output '--- Test 9: LOTP monotonic_offsets == true ---'
Assert-True ($null -ne $lotpRec -and $lotpRec.monotonic_offsets -eq $true) 'LOTP monotonic_offsets == true'

Write-Output '--- Test 10: LOTP sampled windows exist ---'
$windows = @($lotpRec.sampled_windows)
Assert-True ($windows.Count -gt 0) 'LOTP sampled_windows.Count > 0'

# ---------------------------------------------------------------------------
# Tests 11-14: LOTH assertions
# ---------------------------------------------------------------------------

Write-Output '--- Test 11: LOTH magic detected ---'
$lothRec = @($r.loth_analysis.records) | Select-Object -First 1
Assert-True ($null -ne $lothRec -and $lothRec.magic -eq 'LOTH') 'LOTH magic == LOTH'

Write-Output '--- Test 12: LOTH declared_entry_count == 2 ---'
Assert-True ($null -ne $lothRec -and [int]$lothRec.declared_entry_count -eq 2) 'LOTH declared_entry_count == 2'

Write-Output '--- Test 13: LOTH parsed_entry_count == 2 ---'
Assert-True ($null -ne $lothRec -and [int]$lothRec.parsed_entry_count -eq 2) 'LOTH parsed_entry_count == 2'

Write-Output '--- Test 14: LOTH first entry matches fixture ---'
$firstEntry = @($lothRec.first_20_entries) | Select-Object -First 1
Assert-True ($firstEntry -eq 'blends_grassoverlays_01_0') 'LOTH first_entry == blends_grassoverlays_01_0'

# ---------------------------------------------------------------------------
# Tests 15-16: Chunkdata assertions
# ---------------------------------------------------------------------------

Write-Output '--- Test 15: Chunkdata size == 1026 ---'
$cdRec = @($r.chunkdata_analysis.records) | Select-Object -First 1
Assert-True ($null -ne $cdRec -and [int]$cdRec.size_bytes -eq 1026) 'chunkdata size_bytes == 1026'

Write-Output '--- Test 16: Chunkdata all_zero_body == true ---'
Assert-True ($null -ne $cdRec -and $cdRec.all_zero_body -eq $true) 'chunkdata all_zero_body == true'

# ---------------------------------------------------------------------------
# Tests 17-20: Safety/claim assertions
# ---------------------------------------------------------------------------

Write-Output '--- Test 17: WRITER_NOT_IMPLEMENTED in JSON ---'
Assert-True ($r.WRITER_NOT_IMPLEMENTED -eq $true) 'WRITER_NOT_IMPLEMENTED == true'

Write-Output '--- Test 18: LOAD_TEST_NOT_PERFORMED in JSON ---'
Assert-True ($r.LOAD_TEST_NOT_PERFORMED -eq $true) 'LOAD_TEST_NOT_PERFORMED == true'

Write-Output '--- Test 19: PLAYABLE_EXPORT_CLAIM_ALLOWED=false in JSON ---'
Assert-True ($r.PLAYABLE_EXPORT_CLAIM_ALLOWED -eq 'false') 'PLAYABLE_EXPORT_CLAIM_ALLOWED == false'

Write-Output '--- Test 20: No playable claim in JSON ---'
$jsonText = Get-Content $jsonFile -Raw
Assert-True ($jsonText -notmatch '"playable_export_claimed"\s*:\s*true') 'No playable_export_claimed:true'

# ---------------------------------------------------------------------------
# Cleanup
# ---------------------------------------------------------------------------

try { Remove-Item -LiteralPath $testBase -Recurse -Force -ErrorAction SilentlyContinue } catch {}
try { Remove-Item -LiteralPath $badPath  -Recurse -Force -ErrorAction SilentlyContinue } catch {}

Write-Output ''
Write-Output '----------------------------------------'
Write-Output "Results: $pass passed, $fail failed"
Write-Output '----------------------------------------'
if ($fail -gt 0) { exit 1 }
exit 0
