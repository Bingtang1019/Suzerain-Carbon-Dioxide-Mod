


























param(
    [string]$PluginCs = '',
    [string]$Csproj = '',
    [string]$Dll = '',
    [switch]$SelfTest,
    
    [string]$GeneratedCs = ''
)
$ErrorActionPreference = 'Stop'

function Test-SelfSyntax {
    try {
        $self = $PSCommandPath
        if (-not $self) { return @{ Ok = $false; Why = 'no script path' } }
        $errs = $null
        [void][System.Management.Automation.Language.Parser]::ParseFile($self, [ref]$null, [ref]$errs)
        if ($errs -and $errs.Count -gt 0) { return @{ Ok = $false; Why = $errs[0].Message } }
        return @{ Ok = $true; Why = '' }
    } catch { return @{ Ok = $false; Why = $_.Exception.Message } }
}

$syn = Test-SelfSyntax
if (-not $syn.Ok) { Write-Host ('[INSTRUMENT-BROKEN] this script does not parse: ' + $syn.Why); exit 2 }
Write-Host '[self] syntax OK'

$LitRe = 'PluginVersion\s*=\s*"([^"]*)"'   # generic: whether it looks like a version is G1's equality check, not the regex
$VerRe = '<Version>\s*([^<\s]+)\s*</Version>'

function Check-Pair {
    param([string]$PluginPath, [string]$ProjPath, [string]$DllPath)
    $res = @{ Code = 0; Reasons = @() }
    if (-not (Test-Path -PathType Leaf $PluginPath)) { $res.Code = 3; $res.Reasons += ('plugin source is not a file (directory?): ' + $PluginPath); return $res }
    if (-not (Test-Path -PathType Leaf $ProjPath)) { $res.Code = 3; $res.Reasons += ('csproj is not a file (directory?): ' + $ProjPath); return $res }
    if (-not $DllPath) { $res.Code = 3; $res.Reasons += 'no -Dll given: the built artifact is part of the evidence, skipping is not passing'; return $res }
    if (-not (Test-Path -PathType Leaf $DllPath)) { $res.Code = 3; $res.Reasons += ('built artifact is not a file (directory?): ' + $DllPath); return $res }

    $pt = [System.IO.File]::ReadAllText($PluginPath, [System.Text.Encoding]::UTF8)
    $jt = [System.IO.File]::ReadAllText($ProjPath, [System.Text.Encoding]::UTF8)

    $lits = [regex]::Matches($pt, $LitRe)
    Write-Host ('[G2] literal version constants = ' + $lits.Count + '  (target 0; necessary, not sufficient)')
    foreach ($m in $lits) { $res.Reasons += ('G2: literal version constant in source: ' + $m.Groups[1].Value) }

    $constVer = if ($lits.Count -gt 0) { $lits[0].Groups[1].Value } else { $null }
    
    $verAll = [regex]::Matches($jt, $VerRe)
    if ($verAll.Count -ne 1) {
        $res.Code = 2
        $res.Reasons += ('csproj has ' + $verAll.Count + ' <Version> elements; exactly one is required')
        return $res
    }
    $projVer = $verAll[0].Groups[1].Value
    $dv = $null
    try { $dv = [System.Diagnostics.FileVersionInfo]::GetVersionInfo($DllPath).FileVersion } catch { $dv = $null }
    if ($dv) { $dv = ($dv -replace '\.0$', '') }
    
    if (-not $dv) { $res.Code = 3; $res.Reasons += ('artifact has no readable FileVersion (not a PE?): ' + $DllPath); return $res }
    $cv = if ($constVer) { $constVer } else { '(none)' }
    $pv = if ($projVer) { $projVer } else { '(none)' }
    $dd = if ($dv) { $dv } else { '(none)' }
    Write-Host ('[G1] banner const = ' + $cv + '  |  csproj <Version> = ' + $pv + '  |  DLL FileVersion = ' + $dd)

    
    if (-not $projVer) { $res.Reasons += 'G1: no <Version> in csproj' }
    if ($constVer -and $projVer -and ($constVer -ne $projVer)) { $res.Reasons += ('G1: banner const (' + $constVer + ') != csproj (' + $projVer + ')') }
    if ($dv -and $projVer -and ($dv -ne $projVer)) { $res.Reasons += ('G1: DLL FileVersion (' + $dv + ') != csproj (' + $projVer + ') -- stale artifact?') }

    if ($res.Reasons.Count -gt 0 -and $res.Code -eq 0) { $res.Code = 1 }
    return $res
}


function Check-Generated {
    param([string]$GeneratedCs, [string]$ProjPath, [string]$DllPath)
    $res = @{ Code = 0; Reasons = @() }
    try {
        if (-not $GeneratedCs) { $res.Code = 3; $res.Reasons += 'no -GeneratedCs given: the generated source is the third version source and skipping is not passing'; return $res }
        if (-not (Test-Path -PathType Leaf $GeneratedCs)) { $res.Code = 3; $res.Reasons += ('generated version source not found: ' + $GeneratedCs); return $res }
        $gt = [System.IO.File]::ReadAllText($GeneratedCs, [System.Text.Encoding]::UTF8)
        $gm = [regex]::Match($gt, $LitRe)
        $gv = if ($gm.Success) { $gm.Groups[1].Value } else { $null }
        $pm = [regex]::Match([System.IO.File]::ReadAllText($ProjPath, [System.Text.Encoding]::UTF8), $VerRe)
        $pv = if ($pm.Success) { $pm.Groups[1].Value } else { $null }
        $dv = $null
        try { $dv = ([System.Diagnostics.FileVersionInfo]::GetVersionInfo($DllPath).FileVersion -replace '\.0$', '') } catch { $dv = $null }
        $gg = if ($gv) { $gv } else { '(none)' }
        $pp = if ($pv) { $pv } else { '(none)' }
        $dd = if ($dv) { $dv } else { '(none)' }
        Write-Host ('[G1b] generated = ' + $gg + '  |  csproj = ' + $pp + '  |  DLL = ' + $dd)
        if (-not $gv) { $res.Reasons += 'G1b: generated file carries no version constant' }
        if ($gv -and $pv -and ($gv -ne $pv)) { $res.Reasons += ('G1b: generated (' + $gv + ') != csproj (' + $pv + ')') }
        if ($gv -and $dv -and ($gv -ne $dv)) { $res.Reasons += ('G1b: generated (' + $gv + ') != DLL (' + $dv + ')') }
        if ($res.Reasons.Count -gt 0) { $res.Code = 1 }
        return $res
    } catch { $res.Code = 2; $res.Reasons += ('instrument error: ' + $_.Exception.Message); return $res }
}

function New-Fixture {
    param([string]$Dir, [string]$Const, [string]$Proj)
    if (-not (Test-Path $Dir)) { New-Item -ItemType Directory -Path $Dir | Out-Null }
    $p = Join-Path $Dir 'Plugin.cs'
    $j = Join-Path $Dir 'Oikonomia.csproj'
    $body = if ([string]::IsNullOrEmpty($Const)) { 'public class P { }' } else { 'public class P { public const string PluginVersion = "' + $Const + '"; }' }
    [System.IO.File]::WriteAllText($p, ($body + "`n"), (New-Object System.Text.UTF8Encoding($false)))
    [System.IO.File]::WriteAllText($j, ('<Project><PropertyGroup><Version>' + $Proj + '</Version></PropertyGroup></Project>' + "`n"), (New-Object System.Text.UTF8Encoding($false)))
    return @{ Plugin = $p; Proj = $j }
}

if ($SelfTest) {
    $tmp = Join-Path $env:TEMP ('vctl-' + (Get-Date -Format 'HHmmss'))
    New-Item -ItemType Directory -Path $tmp | Out-Null
    $cases = @()

    
    $repoRoot = Split-Path $PSScriptRoot -Parent
    $cands = @(
        (Join-Path $repoRoot 'src\Oikonomia\bin\Release\Oikonomia.dll'),
        (Join-Path $repoRoot 'src\Oikonomia\bin\Release\net6.0\Oikonomia.dll'),
        (Join-Path $repoRoot '_staging\deploy-pending\Oikonomia.dll')
    )
    $realDll = $null
    foreach ($c in $cands) { if (Test-Path -PathType Leaf $c) { $realDll = $c; break } }
    $fake = Join-Path $tmp 'fake-not-pe.dll'
    [System.IO.File]::WriteAllText($fake, 'not a real assembly', (New-Object System.Text.UTF8Encoding($false)))
    if (-not $realDll) {
        $cases += [pscustomobject]@{ Case = 'real-pe-available'; Expect = 0; Got = 2; Reason = ('no built artifact found in: ' + ($cands -join ' | ')) }
    } else {
        $realVer = ([System.Diagnostics.FileVersionInfo]::GetVersionInfo($realDll).FileVersion -replace '\.0$', '')
        $cases += [pscustomobject]@{ Case = 'real-pe-available'; Expect = 0; Got = 0; Reason = ('using ' + $realDll + ' version ' + $realVer) }
    }

    
    $f1 = New-Fixture -Dir (Join-Path $tmp 'pos') -Const '9.9.9' -Proj '0.0.1'
    $r1 = Check-Pair -PluginPath $f1.Plugin -ProjPath $f1.Proj -DllPath $(if ($realDll) { $realDll } else { $fake })
    $cases += [pscustomobject]@{ Case = 'positive(mismatch)'; Expect = $(if ($realDll) { 1 } else { 2 }); Got = $r1.Code; Reason = ($r1.Reasons -join ' ; ') }

    $r3 = Check-Pair -PluginPath $f1.Plugin -ProjPath $f1.Proj -DllPath ''
    $cases += [pscustomobject]@{ Case = 'missing-dll'; Expect = 3; Got = $r3.Code; Reason = ($r3.Reasons -join ' ; ') }

    $bad = Join-Path $tmp 'bad.ps1'
    [System.IO.File]::WriteAllText($bad, ('function f( { ' + "`n"), (New-Object System.Text.UTF8Encoding($false)))
    $errs = $null
    [void][System.Management.Automation.Language.Parser]::ParseFile($bad, [ref]$null, [ref]$errs)
    $badDetected = [bool]($errs -and $errs.Count -gt 0)
    $cases += [pscustomobject]@{ Case = 'bad-syntax-detected'; Expect = $true; Got = $badDetected; Reason = 'parser rejects the malformed file' }

    
    $plainSrc = Join-Path $repoRoot 'src\Oikonomia\Reflect.cs'
    if ($realDll -and (Test-Path -PathType Leaf $plainSrc)) {
        $ver = ([System.Diagnostics.FileVersionInfo]::GetVersionInfo($realDll).FileVersion -replace '\.0$', '')
        $f6 = New-Fixture -Dir (Join-Path $tmp 'neg') -Const '' -Proj $ver
        $r6 = Check-Pair -PluginPath $f6.Plugin -ProjPath $f6.Proj -DllPath $realDll
        $cases += [pscustomobject]@{ Case = 'negative(agree)'; Expect = 0; Got = $r6.Code; Reason = ($r6.Reasons -join ' ; ') }
    } else {
        $cases += [pscustomobject]@{ Case = 'negative(agree)'; Expect = 0; Got = 2; Reason = 'no real artifact: cannot prove the green path (instrument broken, never skipped)' }
    }

    
    $f7 = New-Fixture -Dir (Join-Path $tmp 'nonpe') -Const '' -Proj '1.1.1'
    $r7 = Check-Pair -PluginPath $f7.Plugin -ProjPath $f7.Proj -DllPath $fake
    $cases += [pscustomobject]@{ Case = 'non-pe-artifact'; Expect = 3; Got = $r7.Code; Reason = ($r7.Reasons -join ' ; ') }

    
    $verForGen = if ($realDll) { ([System.Diagnostics.FileVersionInfo]::GetVersionInfo($realDll).FileVersion -replace '\.0$','') } else { $null }
    if ($realDll -and $verForGen) {
        $genDir = Join-Path $tmp 'gen'
        New-Item -ItemType Directory -Force -Path $genDir | Out-Null
        $genFile = Join-Path $genDir 'VersionInfo.g.cs'
        [System.IO.File]::WriteAllText($genFile, ('namespace Oikonomia { internal static class VersionInfo { internal const string PluginVersion = "' + $verForGen + '"; } }' + "`n"), (New-Object System.Text.UTF8Encoding($false)))
        $f8 = New-Fixture -Dir (Join-Path $tmp 'three') -Const '' -Proj $verForGen
        $r8 = Check-Generated -GeneratedCs $genFile -ProjPath $f8.Proj -DllPath $realDll
        $cases += [pscustomobject]@{ Case = 'three-sources'; Expect = 0; Got = $r8.Code; Reason = ($r8.Reasons -join ' ; ') }
        $r9 = Check-Generated -GeneratedCs (Join-Path $genDir 'does-not-exist.g.cs') -ProjPath $f8.Proj -DllPath $realDll
        $cases += [pscustomobject]@{ Case = 'missing-generated'; Expect = 3; Got = $r9.Code; Reason = ($r9.Reasons -join ' ; ') }
    } else {
        $cases += [pscustomobject]@{ Case = 'three-sources'; Expect = 0; Got = 2; Reason = 'no real artifact available' }
        $cases += [pscustomobject]@{ Case = 'missing-generated'; Expect = 3; Got = 2; Reason = 'no real artifact available' }
    }

    $fail = 0
    foreach ($c in $cases) {
        $ok = ($c.Got -eq $c.Expect)
        if (-not $ok) { $fail++ }
        $tag = if ($ok) { 'OK' } else { 'BAD' }
        Write-Host ('[' + $tag + '] ' + $c.Case + '  expect=' + $c.Expect + ' got=' + $c.Got + '  ' + $c.Reason)
    }
    Write-Host ('SUMMARY cases=' + $cases.Count + ' failed=' + $fail + '  (gate self-test; the chain layer adds 2 controls in run-tests.ps1 -- entry-parse return value and exit-code mapping -- for ' + ($cases.Count + 2) + ' controls total)')
    if ($fail -gt 0) { Write-Host '[INSTRUMENT-BROKEN] self-test failed'; exit 2 }
    Write-Host '[PASS] self-test'
    exit 0
}

if (-not $PluginCs -or -not $Csproj) { Write-Host '[ENV] need -PluginCs and -Csproj (or -SelfTest)'; exit 3 }
$r = Check-Pair -PluginPath $PluginCs -ProjPath $Csproj -DllPath $Dll
if ($r.Code -eq 0) {
    
    $gen = $GeneratedCs
    if (-not $gen) { $gen = Join-Path (Split-Path $PSScriptRoot -Parent) 'src\Oikonomia\obj\Release\VersionInfo.g.cs' }
    $rg = Check-Generated -GeneratedCs $gen -ProjPath $Csproj -DllPath $Dll
    if ($rg.Code -ne 0) { $r = $rg }
}
if ($r.Code -eq 0) { Write-Host '[PASS] version sources agree'; exit 0 }
Write-Host ('[FAIL] ' + $r.Reasons.Count + ' reason(s):')
$r.Reasons | ForEach-Object { Write-Host ('   - ' + $_) }
exit $r.Code
