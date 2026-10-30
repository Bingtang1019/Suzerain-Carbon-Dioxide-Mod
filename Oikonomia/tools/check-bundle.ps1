
































param(
    [string]$Zip = ''
)
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.IO.Compression.FileSystem

$root = Split-Path -Parent $PSScriptRoot            
$suz  = Split-Path -Parent $root                    
if (-not $Zip) { $Zip = Join-Path $suz '_releases\Carbon-Dioxide-Mod.zip' }

function Line($m, $color) { Write-Host $m -ForegroundColor $color }





$items = @(
    @{ ZipPath = 'BepInEx/plugins/Oikonomia.dll';             Kind = 'byte'
       Src = (Join-Path $root 'src\Oikonomia\bin\Release\Oikonomia.dll')
       Proj = 'Oikonomia' },
    @{ ZipPath = 'BepInEx/plugins/HorrifyingBombardment.dll'; Kind = 'byte'
       Src = (Join-Path $suz 'Horrifying Bombardment\src\HorrifyingBombardment\bin\Release\HorrifyingBombardment.dll')
       Proj = 'Horrifying Bombardment' },
    @{ ZipPath = 'Suzerain-JSON Injector/SuzerainGUI.exe';    Kind = 'byte'
       Src = (Join-Path $suz 'Suzerain-JSON Injector\SuzerainGUI.exe')
       Proj = 'Suzerain-JSON Injector' },
    @{ ZipPath = 'Suzerain-JSON Injector/README.txt';         Kind = 'byte'; Optional = $true
       Src = (Join-Path $suz 'Suzerain-JSON Injector\README.txt')
       Proj = 'Suzerain-JSON Injector' },
    @{ ZipPath = 'Suzerain-JSON Injector/bin/SuzerainTrainer.exe'; Kind = 'byte'; Optional = $true
       Src = (Join-Path $suz 'Suzerain-JSON Injector\bin\SuzerainTrainer.exe')
       Proj = 'Suzerain-JSON Injector' },
    @{ ZipPath = 'install.ps1';                               Kind = 'byte'
       Src = (Join-Path $root 'tools\installer-src\install.ps1')
       Proj = 'Oikonomia（安装器）' },
    @{ ZipPath = 'install.bat';                               Kind = 'byte'
       Src = (Join-Path $root 'tools\installer-src\install.bat')
       Proj = 'Oikonomia（安装器）' },
    @{ ZipPath = '先读我.txt';                                Kind = 'generated'
       Src = (Join-Path $root 'tools\先读我.template.txt')
       Proj = 'Oikonomia（模板渲染，含版本号替换）' }
)


$requiredEntries = @(
    'winhttp.dll', 'doorstop_config.ini', '.doorstop_version',
    'BepInEx/core/BepInEx.Core.dll',
    'BepInEx/plugins/Oikonomia.dll',
    'BepInEx/plugins/HorrifyingBombardment.dll',
    'install.ps1', 'install.bat', '先读我.txt',
    'Suzerain-JSON Injector/SuzerainGUI.exe',
    'Suzerain-JSON Injector/README.txt'
)


$known = @{}
foreach ($it in $items) { $known[$it.ZipPath] = $true }
$watchedDirs = @('BepInEx/plugins/', 'Suzerain-JSON Injector/')


function Get-VerText([byte[]]$bytes, [string]$name) {
    try {
        $txt = [System.Text.Encoding]::Unicode.GetString($bytes)
        if ($name -eq 'SuzerainGUI.exe') {
            
            $m0 = [regex]::Match($txt, '存档修改器\s*v(\d+\.\d+\.\d+)')
            if ($m0.Success) { return $m0.Groups[1].Value }
        }
        $m = [regex]::Matches($txt, '\bv(\d+\.\d+\.\d+(?:-[A-Za-z0-9.]+)?)\b') | ForEach-Object { $_.Groups[1].Value } | Sort-Object -Unique
        if (-not $m) { $m = [regex]::Matches($txt, '\b\d+\.\d+\.\d+(?:-[A-Za-z0-9.]+)?\b') | ForEach-Object { $_.Value } | Sort-Object -Unique }
        if ($m) { return ($m | Select-Object -Last 1) }
    } catch { }
    return '?'
}

function Get-Sha256OfBytes([byte[]]$bytes) {
    $sha = [System.Security.Cryptography.SHA256]::Create()
    try { return ([BitConverter]::ToString($sha.ComputeHash($bytes))).Replace('-', '') }
    finally { $sha.Dispose() }
}

function Read-ZipEntryBytes($entry) {
    $ms = New-Object System.IO.MemoryStream
    $es = $entry.Open()
    try { $es.CopyTo($ms) } finally { $es.Close() }
    return $ms.ToArray()
}

function Short([string]$h) { if ($h) { return $h.Substring(0, 16) } else { return '-' } }


Write-Host ""
Line "整合包新鲜度检查（**逐文件 SHA256**，不是比版本串）" Cyan
if (-not (Test-Path $Zip)) {
    Line ("[失败] 找不到整合包：" + $Zip) Red
    Write-Host "BUNDLE_CHECK: FAIL stale=0 hashdiff=0 missing=1 unverified=0 unlisted=0 ok=0"
    exit 1
}
$zi = Get-Item $Zip
Write-Host ("  包：" + $Zip)
Write-Host ("  打包时间：" + $zi.LastWriteTime + "   大小：" + [math]::Round($zi.Length / 1MB, 2) + " MB")
Write-Host ""


$siblings = @(Get-ChildItem (Split-Path -Parent $Zip) -Filter '*.zip' -File -ErrorAction SilentlyContinue |
              Where-Object { $_.FullName -ne $zi.FullName -and $_.LastWriteTime -gt $zi.LastWriteTime })
if ($siblings.Count -gt 0) {
    Line ("  ! 同目录还有更新的包（本次检查的不是最新的那个）：") Yellow
    foreach ($s in $siblings) { Line ("      " + $s.Name + "   " + $s.LastWriteTime + "   " + [math]::Round($s.Length / 1MB, 2) + " MB") Yellow }
    Line "      ⇒ 若你刚打的是它，请用 -Zip 显式指定；否则本次结论不代表最新产物。" Yellow
    Write-Host ""
}


$stale = @(); $hashdiff = @(); $missing = @(); $unverified = @(); $unlisted = @()
$okCount = 0
$z = $null
try {
    $z = [System.IO.Compression.ZipFile]::OpenRead($Zip)

    
    $names = @($z.Entries | ForEach-Object { $_.FullName -replace '\\', '/' })
    foreach ($need in $requiredEntries) {
        if ($known.ContainsKey($need)) { continue }
        if ($names -notcontains $need) { $missing += $need; Line ("  {0,-52} 包内缺失 ✗" -f $need) Red }
    }

    
    foreach ($it in $items) {
        $leaf = $it.ZipPath
        $hits = @($z.Entries | Where-Object { ($_.FullName -replace '\\', '/') -eq $it.ZipPath })
        if ($hits.Count -eq 0) {
            if ($it.Optional) {
                if (-not (Test-Path $it.Src)) { Line ("  {0,-52} 可选组件：包内与源都没有（跳过）" -f $leaf) DarkGray }
                else { $missing += $leaf; Line ("  {0,-52} 包内缺失 ✗（源存在 ⇒ 应重打）" -f $leaf) Red }
            } else {
                $missing += $leaf
                Line ("  {0,-52} 包内缺失 ✗" -f $leaf) Red
            }
            continue
        }
        if ($hits.Count -gt 1) { Line ("  {0,-52} ! 包内有 {1} 个同名条目（取最后一个核对）" -f $leaf, $hits.Count) Yellow }
        $entry = $hits[$hits.Count - 1]

        
        if ($it.Kind -eq 'generated') {
            if (-not (Test-Path $it.Src)) {
                $unverified += $leaf
                Line ("  {0,-52} **无法核对（模板缺失）** ✗  模板：{1}" -f $leaf, $it.Src) Yellow
            } else {
                $okCount++
                Line ("  {0,-46} 生成物：包内存在 ✓（模板渲染，字节不可比；模板 {1}）" -f $leaf, (Split-Path $it.Src -Leaf)) Gray
            }
            continue
        }

        
        if (-not (Test-Path $it.Src)) {
            if ($it.Optional) { Line ("  {0,-52} 无法核对（源缺失，可选组件）：{1}" -f $leaf, $it.Src) DarkGray; continue }
            $unverified += $leaf
            Line ("  {0,-52} **无法核对（源缺失）** ✗  源路径：{1}" -f $leaf, $it.Src) Yellow
            continue
        }

        $bIn  = Read-ZipEntryBytes $entry
        $hIn  = Get-Sha256OfBytes $bIn
        $hCur = (Get-FileHash $it.Src -Algorithm SHA256).Hash
        $vIn  = Get-VerText $bIn (Split-Path $it.ZipPath -Leaf)
        $vCur = Get-VerText ([System.IO.File]::ReadAllBytes($it.Src)) (Split-Path $it.ZipPath -Leaf)
        $srcTime = (Get-Item $it.Src).LastWriteTime

        if ($hIn -eq $hCur) {
            $okCount++
            Line ("  {0,-46} v{1,-16} 一致 ✓   包内 {2}  源 {3}" -f (Split-Path $leaf -Leaf), $vCur, (Short $hIn), (Short $hCur)) Gray
            continue
        }

        
        if ($vIn -ne '?' -and $vCur -ne '?' -and $vIn -ne $vCur) {
            $stale += $leaf
            Line ("  {0,-46} 包内 v{1,-16} 源 v{2,-16} **版本过期** ✗" -f (Split-Path $leaf -Leaf), $vIn, $vCur) Red
            Line ("      包内 {0}  源 {1}   源改动时间 {2}" -f (Short $hIn), (Short $hCur), $srcTime) Red
        } else {
            $hashdiff += $leaf
            Line ("  {0,-46} **同版本不同字节（构建已变，需重打）** ✗  v{1}" -f (Split-Path $leaf -Leaf), $vCur) Red
            Line ("      包内 {0}  源 {1}   源改动时间 {2}" -f (Short $hIn), (Short $hCur), $srcTime) Red
        }
    }

    
    foreach ($e in $z.Entries) {
        $p = $e.FullName -replace '\\', '/'
        if ($e.Length -eq 0) { continue }
        if ($p.EndsWith('/')) { continue }
        $watched = $false
        foreach ($d in $watchedDirs) { if ($p.StartsWith($d, [System.StringComparison]::Ordinal)) { $watched = $true } }
        if (-not $watched) { continue }
        if ($known.ContainsKey($p)) { continue }
        $unlisted += $p
    }
    if ($unlisted.Count -gt 0) {
        Write-Host ""
        Line "  未登记条目（在这些目录里但清单没有它 ⇒ 请把源路径补进 `$items）：" Yellow
        foreach ($u in $unlisted) { Line ("      " + $u) Yellow }
    }
}
finally { if ($z) { $z.Dispose() } }


$stale      = @($stale      | Select-Object -Unique)
$hashdiff   = @($hashdiff   | Select-Object -Unique)
$missing    = @($missing    | Select-Object -Unique)
$unverified = @($unverified | Select-Object -Unique)
$unlisted   = @($unlisted   | Select-Object -Unique)


$fail = ($stale.Count + $hashdiff.Count + $missing.Count + $unverified.Count + $unlisted.Count)
Write-Host ""
if ($fail -eq 0) {
    Line ("结论：包内 " + $okCount + " 个组件与当前构建产物**逐字节一致** ✓") Green
} else {
    if ($stale.Count      -gt 0) { Line ("  · 版本过期（stale）："     + ($stale      -join ', ')) Red }
    if ($hashdiff.Count   -gt 0) { Line ("  · 同版本不同字节（hashdiff）：" + ($hashdiff -join ', ') + "  ← 构建已变，需重打") Red }
    if ($missing.Count    -gt 0) { Line ("  · 包内缺失（missing）："   + ($missing    -join ', ')) Red }
    if ($unverified.Count -gt 0) { Line ("  · 无法核对（源缺失 unverified）：" + ($unverified -join ', ')) Yellow }
    if ($unlisted.Count   -gt 0) { Line ("  · 未登记条目（unlisted）：" + ($unlisted   -join ', ')) Yellow }
    Write-Host ""
    Line "结论：整合包**不是**当前构建产物 ⇒ 请重新打包（有写入方在跑时，等它停下来再打）：" Red
    Write-Host "  powershell -ExecutionPolicy Bypass -File tools\pack-bundle.ps1 -SkipBuild"
}

$verdict = 'FAIL'; if ($fail -eq 0) { $verdict = 'PASS' }
Write-Host ("BUNDLE_CHECK: " + $verdict +
            " stale=" + $stale.Count + " hashdiff=" + $hashdiff.Count + " missing=" + $missing.Count +
            " unverified=" + $unverified.Count + " unlisted=" + $unlisted.Count + " ok=" + $okCount)
if ($fail -eq 0) { exit 0 } else { exit 1 }
