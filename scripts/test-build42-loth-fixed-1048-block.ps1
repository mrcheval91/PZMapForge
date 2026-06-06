#Requires -Version 5.1
<#
.SYNOPSIS
    Tests for analyze-build42-loth-fixed-1048-block.ps1 (MAP-6Y).

    Creates three synthetic LOTH fixtures:
      ref1 (1_1.lotheader): 3 entries, 1048-byte trailer (byte[0]=0x01, rest 0x00)
      ref2 (2_2.lotheader): 4 entries, IDENTICAL 1048-byte trailer to ref1
      ref3 (3_3.lotheader): 5 entries, same trailer EXCEPT byte[64]=0xFF

    Expected results:
      selected_file_count         = 3
      unique_trailer_sha256_count = 2  (ref1==ref2, ref3 differs)
      all_1048_blocks_identical   = false
      stable_byte_count           = 1047
      variable_byte_count         = 1
      stable_prefix_length        = 64  (positions 0-63 stable)
      stable_suffix_length        = 983  (positions 65-1047 stable)
      stable_byte_ranges          = 2 ranges
      variable_byte_ranges        = 1 range

    Expected assertion count: 20
#>

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$scriptDir  = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot   = Split-Path -Parent $scriptDir
$modScript  = Join-Path $repoRoot 'scripts\analyze-build42-loth-fixed-1048-block.ps1'
$tempRoot   = [System.IO.Path]::GetTempPath()

$pass = 0
$fail = 0

function Assert-True {
    param([bool]$Condition, [string]$Label)
    if ($Condition) { Write-Output "  PASS  $Label"; $script:pass++ }
    else            { Write-Output "  FAIL  $Label"; $script:fail++ }
}

function Invoke-Mod {
    param([string]$ReferenceRoot, [string]$Output)
    & powershell -ExecutionPolicy Bypass -File $modScript `
        -ReferenceRoot $ReferenceRoot `
        -Output $Output | Out-Null
    return [int]$LASTEXITCODE
}

Write-Output 'test-build42-loth-fixed-1048-block.ps1'
Write-Output ''

# ---------------------------------------------------------------------------
# Synthetic fixtures
# ---------------------------------------------------------------------------

$testBase = Join-Path $tempRoot ('pzmf-t6y-' + [System.IO.Path]::GetRandomFileName())
$refDir   = Join-Path $testBase '.local\reference'
$outDir   = Join-Path $testBase '.local\output'
$badPath  = Join-Path $tempRoot 'pzmf-t6y-bad-no-local'

New-Item -ItemType Directory -Force -Path $refDir  | Out-Null
New-Item -ItemType Directory -Force -Path $badPath | Out-Null

# Build a standard 1048-byte trailer: byte[0]=0x01, rest 0x00
$standardTrailer = [byte[]]::new(1048)
$standardTrailer[0] = 0x01

# ref3 trailer: same except byte[64] = 0xFF
$variantTrailer = [byte[]]::new(1048)
$standardTrailer.CopyTo($variantTrailer, 0)
$variantTrailer[64] = 0xFF

function Make-Loth {
    param([string]$Path, [int]$EntryCount, [string[]]$Entries, [byte[]]$Trailer)
    $magic = [byte[]]@(0x4C,0x4F,0x54,0x48)
    $ver   = [byte[]]@(0x01,0x00,0x00,0x00)
    $cnt   = [BitConverter]::GetBytes([uint32]$EntryCount)
    $body  = [byte[]]::new(0)
    foreach ($e in $Entries) {
        $body = $body + [System.Text.Encoding]::ASCII.GetBytes($e + "`n")
    }
    $all = [byte[]]($magic + $ver + $cnt + $body + $Trailer)
    [System.IO.File]::WriteAllBytes($Path, $all)
}

Make-Loth -Path (Join-Path $refDir '1_1.lotheader') `
    -EntryCount 3 `
    -Entries @('entry_a','entry_b','entry_c') `
    -Trailer $standardTrailer

Make-Loth -Path (Join-Path $refDir '2_2.lotheader') `
    -EntryCount 4 `
    -Entries @('set_a','set_b','set_c','set_d') `
    -Trailer $standardTrailer

Make-Loth -Path (Join-Path $refDir '3_3.lotheader') `
    -EntryCount 5 `
    -Entries @('tile_0','tile_1','tile_2','tile_3','tile_4') `
    -Trailer $variantTrailer

# ---------------------------------------------------------------------------
# Test 1: ReferenceRoot outside .local refused
# ---------------------------------------------------------------------------

Write-Output '--- Test 1: ReferenceRoot outside .local refused ---'
$t1Exit = Invoke-Mod -ReferenceRoot $badPath -Output $outDir
Assert-True ($t1Exit -ne 0) 'Test1: ReferenceRoot outside .local exits nonzero'

# ---------------------------------------------------------------------------
# Test 2: Output outside .local refused
# ---------------------------------------------------------------------------

Write-Output ''
Write-Output '--- Test 2: Output outside .local refused ---'
$t2Exit = Invoke-Mod -ReferenceRoot $refDir -Output $badPath
Assert-True ($t2Exit -ne 0) 'Test2: Output outside .local exits nonzero'

# ---------------------------------------------------------------------------
# Tests 3-20: Valid run
# ---------------------------------------------------------------------------

Write-Output ''
Write-Output '--- Tests 3-20: valid run ---'
$t3Exit = Invoke-Mod -ReferenceRoot $refDir -Output $outDir

$jsonFile = Join-Path $outDir 'build42-loth-fixed-1048-block.json'
$mdFile   = Join-Path $outDir 'build42-loth-fixed-1048-block.md'

Assert-True ($t3Exit -eq 0)       'Test3: script exits 0'
Assert-True (Test-Path $jsonFile) 'Test4: JSON report exists'
Assert-True (Test-Path $mdFile)   'Test5: MD report exists'

$data      = if (Test-Path $jsonFile) { Get-Content $jsonFile -Raw | ConvertFrom-Json } else { $null }
$mdContent = if (Test-Path $mdFile)   { Get-Content $mdFile   -Raw } else { '' }

# Test 6: selected_file_count == 3
$selCount = if ($null -ne $data) { [int]$data.selected_file_count } else { -1 }
Assert-True ($selCount -eq 3) "Test6: selected_file_count == 3 (got $selCount)"

# Test 7: unique_trailer_sha256_count == 2
$uniqueCount = if ($null -ne $data) { [int]$data.unique_trailer_sha256_count } else { -1 }
Assert-True ($uniqueCount -eq 2) "Test7: unique_trailer_sha256_count == 2 (got $uniqueCount)"

# Test 8: all_1048_blocks_identical == false
$identical = if ($null -ne $data) { [bool]$data.all_1048_blocks_identical } else { $true }
Assert-True ($identical -eq $false) "Test8: all_1048_blocks_identical == false (got $identical)"

# Test 9: stable_byte_count == 1047
$stableCount = if ($null -ne $data) { [int]$data.stable_byte_count } else { -1 }
Assert-True ($stableCount -eq 1047) "Test9: stable_byte_count == 1047 (got $stableCount)"

# Test 10: variable_byte_count == 1
$varCount = if ($null -ne $data) { [int]$data.variable_byte_count } else { -1 }
Assert-True ($varCount -eq 1) "Test10: variable_byte_count == 1 (got $varCount)"

# Safe count that handles PS 5.1 ConvertFrom-Json unrolling single-element arrays
function Count-Maybe ($obj) {
    if ($null -eq $obj) { return 0 }
    if ($obj -is [System.Array]) { return $obj.Count }
    if ($obj -is [System.Collections.ICollection]) { return $obj.Count }
    return 1
}

# Test 11: stable_byte_ranges has 2 entries (0-63 and 65-1047)
$stableRangesCount = if ($null -ne $data) { Count-Maybe $data.stable_byte_ranges } else { 0 }
Assert-True ($stableRangesCount -eq 2) "Test11: stable_byte_ranges has 2 ranges (got $stableRangesCount)"

# Test 12: variable_byte_ranges has 1 entry (64-64)
$varRangesCount = if ($null -ne $data) { Count-Maybe $data.variable_byte_ranges } else { 0 }
Assert-True ($varRangesCount -eq 1) "Test12: variable_byte_ranges has 1 range (got $varRangesCount)"

# Test 13: stable_prefix_length == 64
$prefixLen = if ($null -ne $data) { [int]$data.stable_prefix_length } else { -1 }
Assert-True ($prefixLen -eq 64) "Test13: stable_prefix_length == 64 (got $prefixLen)"

# Test 14: stable_suffix_length == 983
$suffixLen = if ($null -ne $data) { [int]$data.stable_suffix_length } else { -1 }
Assert-True ($suffixLen -eq 983) "Test14: stable_suffix_length == 983 (got $suffixLen)"

# Test 15: hypotheses array has at least 1 element
$hypsCount = if ($null -ne $data) { Count-Maybe $data.hypotheses } else { 0 }
Assert-True ($hypsCount -ge 1) "Test15: hypotheses has >= 1 element (got $hypsCount)"

# Test 16: writer_readiness field is non-empty
$wr = if ($null -ne $data) { [string]$data.writer_readiness } else { '' }
Assert-True ($wr.Length -gt 0) "Test16: writer_readiness is non-empty (got '$wr')"

# Test 17: MD contains BUILD42_LOTH_FIXED_1048_BLOCK_ANALYSED
Assert-True ($mdContent -match 'BUILD42_LOTH_FIXED_1048_BLOCK_ANALYSED') `
    'Test17: MD contains BUILD42_LOTH_FIXED_1048_BLOCK_ANALYSED'

# Test 18: MD contains WRITER_NOT_CHANGED
Assert-True ($mdContent -match 'WRITER_NOT_CHANGED') `
    'Test18: MD contains WRITER_NOT_CHANGED'

# Test 19: MD contains LOAD_TEST_NOT_PERFORMED
Assert-True ($mdContent -match 'LOAD_TEST_NOT_PERFORMED') `
    'Test19: MD contains LOAD_TEST_NOT_PERFORMED'

# Test 20: MD contains PLAYABLE_EXPORT_CLAIM_ALLOWED=false
Assert-True ($mdContent -match 'PLAYABLE_EXPORT_CLAIM_ALLOWED=false') `
    'Test20: MD contains PLAYABLE_EXPORT_CLAIM_ALLOWED=false'

# ---------------------------------------------------------------------------
# Summary
# ---------------------------------------------------------------------------

Write-Output ''
Write-Output "----------------------------------------"
Write-Output "Results: $pass passed, $fail failed"
Write-Output "----------------------------------------"

if ($fail -gt 0) { exit 1 }
exit 0
