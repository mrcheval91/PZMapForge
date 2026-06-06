#Requires -Version 5.1
<#
.SYNOPSIS
    Tests for compare-build42-lotheader-candidate.ps1 (MAP-6Q).

    Creates synthetic .local fixtures: a short candidate lotheader and two
    longer reference lotheaders. Validates comparison output.
    Does not read PZ folders. Does not copy files to PZ.
    Expected assertion count: 13
#>

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$scriptDir  = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot   = Split-Path -Parent $scriptDir
$cmpScript  = Join-Path $repoRoot 'scripts\compare-build42-lotheader-candidate.ps1'
$tempRoot   = [System.IO.Path]::GetTempPath()

$pass = 0
$fail = 0

function Assert-True {
    param([bool]$Condition, [string]$Label)
    if ($Condition) { Write-Output "  PASS  $Label"; $script:pass++ }
    else            { Write-Output "  FAIL  $Label"; $script:fail++ }
}

function Invoke-Cmp {
    param([string]$CandidateLotheader, [string]$ReferenceRoot, [string]$Output)
    & powershell -ExecutionPolicy Bypass -File $cmpScript `
        -CandidateLotheader $CandidateLotheader `
        -ReferenceRoot $ReferenceRoot `
        -Output $Output | Out-Null
    return [int]$LASTEXITCODE
}

Write-Output 'test-build42-lotheader-candidate-comparison.ps1'
Write-Output ''

# ---------------------------------------------------------------------------
# Setup: synthetic fixtures under temp .local dirs
# ---------------------------------------------------------------------------

$testBase  = Join-Path $tempRoot ('pzmf-t6q-' + [System.IO.Path]::GetRandomFileName())
$candDir   = Join-Path $testBase '.local\candidate'
$refDir    = Join-Path $testBase '.local\reference'
$outDir    = Join-Path $testBase '.local\output'
$badPath   = Join-Path $tempRoot 'pzmf-t6q-bad-no-local'

New-Item -ItemType Directory -Force -Path $candDir  | Out-Null
New-Item -ItemType Directory -Force -Path $refDir   | Out-Null
New-Item -ItemType Directory -Force -Path $badPath  | Out-Null

# Candidate: LOTH magic + version=1 + field8=1 + 1 short entry (~38 bytes total)
$lothMagic = [byte[]]@(0x4C, 0x4F, 0x54, 0x48)     # LOTH
$ver1      = [byte[]]@(0x01, 0x00, 0x00, 0x00)      # version=1
$cnt1      = [byte[]]@(0x01, 0x00, 0x00, 0x00)      # entry_count=1
$entry     = [System.Text.Encoding]::ASCII.GetBytes("blends_grassoverlays_01_0`n")
$candBytes = $lothMagic + $ver1 + $cnt1 + $entry     # 38 bytes
$candFile  = Join-Path $candDir '0_0.lotheader'
[System.IO.File]::WriteAllBytes($candFile, [byte[]]$candBytes)

# Reference 1: LOTH + version=1 + field8=36 + 36 entries (much larger)
$cnt36   = [byte[]]@(0x24, 0x00, 0x00, 0x00)        # entry_count=36
$entries = [byte[]]::new(0)
for ($i = 0; $i -lt 36; $i++) {
    $e = [System.Text.Encoding]::ASCII.GetBytes("tileset_pack_name_$i`n")
    $entries = $entries + $e
}
$ref1Bytes = $lothMagic + $ver1 + $cnt36 + $entries
$ref1File  = Join-Path $refDir 'ref1.lotheader'
[System.IO.File]::WriteAllBytes($ref1File, [byte[]]$ref1Bytes)

# Reference 2: similar structure, different entries
$entries2 = [byte[]]::new(0)
for ($i = 0; $i -lt 36; $i++) {
    $e = [System.Text.Encoding]::ASCII.GetBytes("other_pack_$i`n")
    $entries2 = $entries2 + $e
}
$ref2Bytes = $lothMagic + $ver1 + $cnt36 + $entries2
$ref2File  = Join-Path $refDir 'ref2.lotheader'
[System.IO.File]::WriteAllBytes($ref2File, [byte[]]$ref2Bytes)

# ---------------------------------------------------------------------------
# Tests 1-3: Path guards
# ---------------------------------------------------------------------------

Write-Output '--- Tests 1-3: path guards ---'
$t1Exit = Invoke-Cmp -CandidateLotheader $badPath -ReferenceRoot $refDir -Output $outDir
Assert-True ($t1Exit -ne 0) 'Test1: CandidateLotheader outside .local refused'

$t2Exit = Invoke-Cmp -CandidateLotheader $candFile -ReferenceRoot $badPath -Output $outDir
Assert-True ($t2Exit -ne 0) 'Test2: ReferenceRoot outside .local refused'

$t3Exit = Invoke-Cmp -CandidateLotheader $candFile -ReferenceRoot $refDir -Output $badPath
Assert-True ($t3Exit -ne 0) 'Test3: Output outside .local refused'

# ---------------------------------------------------------------------------
# Tests 4-13: Valid run
# ---------------------------------------------------------------------------

Write-Output ''
Write-Output '--- Tests 4-13: valid run ---'
$t4Exit = Invoke-Cmp -CandidateLotheader $candFile -ReferenceRoot $refDir -Output $outDir

$jsonFile = Join-Path $outDir 'build42-lotheader-candidate-comparison.json'
$mdFile   = Join-Path $outDir 'build42-lotheader-candidate-comparison.md'

Assert-True ($t4Exit -eq 0)       'Test4: script exits 0'
Assert-True (Test-Path $jsonFile) 'Test5: comparison JSON exists'
Assert-True (Test-Path $mdFile)   'Test6: comparison MD exists'

$data = if (Test-Path $jsonFile) { Get-Content $jsonFile -Raw | ConvertFrom-Json } else { $null }
$mdContent = if (Test-Path $mdFile) { Get-Content $mdFile -Raw } else { '' }

Assert-True ([string]$data.candidate_magic -eq 'LOTH') 'Test7: candidate magic == LOTH'
Assert-True ((@(@($data.reference_magic_counts) | Where-Object { $_.magic -eq 'LOTH' })).Count -gt 0) `
    'Test8: reference magic LOTH detected'
Assert-True ($data.candidate_smaller_than_all_references -eq $true) `
    'Test9: candidate_smaller_than_all_references == true'
Assert-True ($mdContent -match 'CURRENT_CANDIDATE_LOTHEADER_EOF') `
    'Test10: CURRENT_CANDIDATE_LOTHEADER_EOF in MD'
Assert-True ($mdContent -match 'CANDIDATE_LOTHEADER_TOO_SHORT_OR_INCOMPLETE') `
    'Test11: CANDIDATE_LOTHEADER_TOO_SHORT_OR_INCOMPLETE in MD'
Assert-True ($mdContent -match 'WRITER_NOT_CHANGED') `
    'Test12: WRITER_NOT_CHANGED in MD'
Assert-True ($mdContent -match 'PLAYABLE_EXPORT_CLAIM_ALLOWED=false') `
    'Test13: PLAYABLE_EXPORT_CLAIM_ALLOWED=false in MD'

# ---------------------------------------------------------------------------
# Summary
# ---------------------------------------------------------------------------

Write-Output ''
Write-Output '----------------------------------------'
Write-Output "Results: $pass passed, $fail failed"
Write-Output '----------------------------------------'

if ($fail -gt 0) { exit 1 }
exit 0
