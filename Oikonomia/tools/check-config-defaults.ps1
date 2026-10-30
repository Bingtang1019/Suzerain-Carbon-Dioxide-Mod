











param(
    [Parameter(Mandatory=$true)][string]$Dll,
    [string]$CoreDir = '',
    [string]$Probe = ''
)
$ErrorActionPreference = 'Stop'
if (-not (Test-Path $Dll)) { Write-Host "[STOP] DLL not found: $Dll"; exit 2 }

$repo = Split-Path $PSScriptRoot -Parent
if (-not $Probe) { $Probe = Join-Path $repo '_staging\cfg-probe\cfg-probe.csproj' }
if (-not $CoreDir) {
    $cands = @(
        'D:\Program Files (x86)\Steam\steamapps\common\Suzerain\BepInEx\core',
        'C:\Program Files (x86)\Steam\steamapps\common\Suzerain\BepInEx\core'
    )
    if ($env:SUZERAIN_GAME) { $cands = @((Join-Path $env:SUZERAIN_GAME 'BepInEx\core')) + $cands }
    foreach ($c in $cands) { if (Test-Path $c) { $CoreDir = $c; break } }
}
if (-not (Test-Path $Probe)) { Write-Host "[WARN] probe project not found: $Probe -- gate skipped"; exit 0 }
if (-not $CoreDir -or -not (Test-Path $CoreDir)) { Write-Host "[WARN] BepInEx core dir not found -- gate skipped"; exit 0 }

$tmp = Join-Path $env:TEMP ('cfgdef-' + (Get-Date -Format 'HHmmss'))
New-Item -ItemType Directory -Force -Path $tmp | Out-Null
$cfg = Join-Path $tmp 'defaults.cfg'
$dotnet = 'C:\Users\dell\.dotnet\dotnet.exe'
if (-not (Test-Path $dotnet)) { $dotnet = 'dotnet' }
$env:DOTNET_ROOT = Split-Path $dotnet -Parent
& $dotnet run --project $Probe -c Release -v quiet --nologo -- $Dll $cfg $CoreDir 2>&1 | Out-Null
if (-not (Test-Path $cfg)) { Write-Host "[WARN] probe produced no cfg -- gate skipped"; exit 0 }

$pats = @('[A-Za-z]:\\', '[A-Za-z]:/', '^\\\\[A-Za-z0-9]', '^/home/', '^/Users/')
if ($env:USERNAME) { $pats += [regex]::Escape($env:USERNAME) }

$hits = New-Object System.Collections.Generic.List[string]
foreach ($line in [System.IO.File]::ReadLines($cfg)) {
    $t = $line.Trim()
    if ($t.Length -eq 0 -or $t.StartsWith('#')) { continue }
    $eq = $t.IndexOf('=')
    if ($eq -lt 0) { continue }
    $key = $t.Substring(0, $eq).Trim()
    $val = $t.Substring($eq + 1).Trim()
    if ($val.Length -eq 0) { continue }
    foreach ($p in $pats) {
        if ([regex]::IsMatch($val, $p)) { $hits.Add($key + ' = ' + $val); break }
    }
}
$uniq = $hits | Sort-Object -Unique
if ($uniq.Count -gt 0) {
    Write-Host ('[FAIL] machine-local absolute path(s) in config defaults (' + $uniq.Count + '):')
    $uniq | Select-Object -First 12 | ForEach-Object { Write-Host ('   ' + $_) }
    Write-Host '[HINT] default such values to "" (off) or to a path relative to the plugin folder.'
    exit 1
}
Write-Host ('[PASS] no machine-local absolute path in the config defaults of ' + (Split-Path $Dll -Leaf))
exit 0
