#Requires -Version 5.1
<#
.SYNOPSIS
    Tests for analyze-build42-loth-trailing-byte-patterns.ps1 (MAP-6W).

    Creates synthetic LOTH fixtures with non-u32-aligned trailing bodies and
    U16 values that reference string table entries.
    Expected assertion count: 20
#>

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$scriptDir   = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot    = Split-Path -Parent $scriptDir
$anaScript   = Join-Path $repoRoot 'scripts\analyze-build42-loth-trailing-byte-patterns.ps1'
$tempRoot    = [System.IO.Path]::GetTempPath()

$pass = 0
$fail = 0

function Assert-True {
    param([bool]$Condition, [string]$Label)
    if ($Condition) { Write-Output "  PASS  $Label"; $script:pass++ }
    else            { Write-Output "  FAIL  $Label"; $script:fail++ }
}

function Invoke-Ana {
    param([string]$ReferenceRoot, [string]$Output)
    & powershell -ExecutionPolicy Bypass -File $anaScript `
        -ReferenceRoot $ReferenceRoot `
        -Output $Output | Out-Null
    return [int]$LASTEXITCODE
}

Write-Output 'test-build42-loth-trailing-byte-patterns.ps1'
Write-Output ''

# ---------------------------------------------------------------------------
# Synthetic fixtures: non-u32-aligned, U16 values < field8
# ---------------------------------------------------------------------------

$testBase = Join-Path $tempRoot ('pzmf-t6w-' + [System.IO.Path]::GetRandomFileName())
$refDir   = Join-Path $testBase '.local\reference'
$outDir   = Join-Path $testBase '.local\output'
$badPath  = Join-Path $tempRoot 'pzmf-t6w-bad-no-local'

New-Item -ItemType Directory -Force -Path $refDir  | Out-Null
New-Item -ItemType Directory -Force -Path $badPath | Out-Null

function Make-BytePatternFixture {
    param([string]$Path, [int]$EntryCount, [string[]]$Entries, [byte[]]$Trailing)
    $magic  = [byte[]]@(0x4C,0x4F,0x54,0x48)
    $ver    = [byte[]]@(0x01,0x00,0x00,0x00)
    $cnt    = [BitConverter]::GetBytes([uint32]$EntryCount)
    $body   = [byte[]]::new(0)
    foreach ($e in $Entries) { $body = $body + [System.Text.Encoding]::ASCII.GetBytes($e + "`n") }
    $all = $magic + $ver + $cnt + $body + $Trailing
    [System.IO.File]::WriteAllBytes($Path, [byte[]]$all)
}

# ref1: field8=4, 4 entries
# Trailing: U16 values 0,1,2,3 (all < field8=4), then 3 extra bytes for non-u32-aligned
$t1 = [byte[]]@(
    0x00,0x00,  # U16 = 0
    0x01,0x00,  # U16 = 1
    0x02,0x00,  # U16 = 2
    0x03,0x00,  # U16 = 3
    0x1E,0x00,  # U16 = 30
    0x00,0x01,  # U16 = 256
    0xFF,0xAA,0xBB  # 3 extra bytes -> total 15 -> mod4=3
)
Make-BytePatternFixture `
    -Path (Join-Path $refDir '5_3.lotheader') `
    -EntryCount 4 `
    -Entries @('tile_0','tile_1','tile_2','tile_3') `
    -Trailing $t1

# ref2: field8=3, 3 entries
# Trailing: similar pattern, mod4=1 (13 bytes trailing)
$t2 = [byte[]]@(
    0x00,0x00,  # U16 = 0
    0x01,0x00,  # U16 = 1
    0x02,0x00,  # U16 = 2
    0x1E,0x00,  # U16 = 30
    0x00,0x00,  # U16 = 0
    0xCC,0xDD,0xEE,0xFF  # 4 bytes -> total 14 -> mod4=2
)
Make-BytePatternFixture `
    -Path (Join-Path $refDir '6_3.lotheader') `
    -EntryCount 3 `
    -Entries @('set_A','set_B','set_C') `
    -Trailing $t2

# ---------------------------------------------------------------------------
# Test 1: ReferenceRoot outside .local refused
# ---------------------------------------------------------------------------

Write-Output '--- Test 1: ReferenceRoot outside .local refused ---'
$t1Exit = Invoke-Ana -ReferenceRoot $badPath -Output $outDir
Assert-True ($t1Exit -ne 0) 'Test1: ReferenceRoot outside .local exits nonzero'

# ---------------------------------------------------------------------------
# Test 2: Output outside .local refused
# ---------------------------------------------------------------------------

Write-Output ''
Write-Output '--- Test 2: Output outside .local refused ---'
$t2Exit = Invoke-Ana -ReferenceRoot $refDir -Output $badPath
Assert-True ($t2Exit -ne 0) 'Test2: Output outside .local exits nonzero'

# ---------------------------------------------------------------------------
# Tests 3-19: Valid run
# ---------------------------------------------------------------------------

Write-Output ''
Write-Output '--- Tests 3-19: valid run ---'
$t3Exit = Invoke-Ana -ReferenceRoot $refDir -Output $outDir

$jsonFile = Join-Path $outDir 'build42-loth-trailing-byte-patterns.json'
$mdFile   = Join-Path $outDir 'build42-loth-trailing-byte-patterns.md'

Assert-True ($t3Exit -eq 0)       'Test3: script exits 0'
Assert-True (Test-Path $jsonFile) 'Test4: JSON report exists'
Assert-True (Test-Path $mdFile)   'Test5: MD report exists'

$data      = if (Test-Path $jsonFile) { Get-Content $jsonFile -Raw | ConvertFrom-Json } else { $null }
$mdContent = if (Test-Path $mdFile)   { Get-Content $mdFile   -Raw } else { '' }

# Test 6: LOTH header detected (all_records have field8_u32le-like reference in data)
$allRecs = @($data.all_records)
Assert-True ($allRecs.Count -gt 0) 'Test6: all_records non-empty (LOTH header detected)'

# Test 7: ASCII entry count detected
$hasAscii = @($allRecs | Where-Object { $null -ne $_.trailing_bytes_count -and [int]$_.trailing_bytes_count -gt 0 }).Count -gt 0
Assert-True $hasAscii 'Test7: trailing_bytes_count > 0 in all_records'

# Test 8: trailing_bytes_count detected
Assert-True ([int]$data.min_trailing_bytes -gt 0) 'Test8: min_trailing_bytes > 0'

# Test 9: mod2/mod4/mod8 fields recorded via count fields
Assert-True ($null -ne $data.count_mod2_aligned) 'Test9: count_mod2_aligned field present'
Assert-True ($null -ne $data.count_mod4_aligned) 'Test9b: count_mod4_aligned field present'

# Test 10: byte histogram via entropy
$hasEntropy = @($allRecs | Where-Object { $null -ne $_.entropy_estimate }).Count -gt 0
Assert-True $hasEntropy 'Test10: entropy_estimate recorded in all_records'

# Test 11: U16 analysis recorded
$hasU16 = @($allRecs | Where-Object { $null -ne $_.plausible_string_index_ratio_u16 }).Count -gt 0
Assert-True $hasU16 'Test11: U16 analysis (plausible_string_index_ratio_u16) recorded'

# Test 12: U32 analysis exists in focus_records
$focusRecs = @($data.focus_records)
$hasU32 = $focusRecs.Count -gt 0 -and $null -ne $focusRecs[0].PSObject.Properties['u16le_first_64']
Assert-True $hasU32 'Test12: U32/U16 words in focus_records'

# Test 13: string index ratio recorded
$hasRatio = @($allRecs | Where-Object {
    $null -ne $_.plausible_string_index_ratio_u16 -and [double]$_.plausible_string_index_ratio_u16 -gt 0
}).Count -gt 0
Assert-True $hasRatio 'Test13: plausible_string_index_ratio_u16 > 0 for at least one file'

# Test 14: compression candidate scan recorded (even if empty list)
$hasComp = $focusRecs.Count -gt 0 -and $null -ne $focusRecs[0].PSObject.Properties['compression_candidates']
Assert-True $hasComp 'Test14: compression_candidates field in focus_records'

# Test 15: non-u32-aligned fixture detected (our fixtures have mod4 != 0)
$hasNonAligned = [int]$data.count_mod4_aligned -lt $allRecs.Count
Assert-True $hasNonAligned 'Test15: at least one non-u32-aligned file detected'

# Tests 16-19: status labels in MD
Assert-True ($mdContent -match 'BUILD42_LOTH_TRAILING_BYTE_PATTERNS_ANALYSED') 'Test16: BUILD42_LOTH_TRAILING_BYTE_PATTERNS_ANALYSED in MD'
Assert-True ($mdContent -match 'WRITER_NOT_CHANGED')                           'Test17: WRITER_NOT_CHANGED in MD'
Assert-True ($mdContent -match 'LOAD_TEST_NOT_PERFORMED')                      'Test18: LOAD_TEST_NOT_PERFORMED in MD'
Assert-True ($mdContent -match 'PLAYABLE_EXPORT_CLAIM_ALLOWED=false')          'Test19: PLAYABLE_EXPORT_CLAIM_ALLOWED=false in MD'

# ---------------------------------------------------------------------------
# Summary
# ---------------------------------------------------------------------------

Write-Output ''
Write-Output '----------------------------------------'
Write-Output "Results: $pass passed, $fail failed"
Write-Output '----------------------------------------'

if ($fail -gt 0) { exit 1 }
exit 0
