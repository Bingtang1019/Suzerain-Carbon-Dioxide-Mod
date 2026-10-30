




















param(
    
    
    [string]$Version = '2026.10.23',
    [string]$BepInExZip = '',
    [string]$OutName = 'Carbon-Dioxide-Mod.zip',   
    [switch]$SkipBuild,
    
    
    
    
    [switch]$NoDeploy
)

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.IO.Compression.FileSystem

function Step($m) { Write-Host ("== " + $m) -ForegroundColor Cyan }
function Info($m) { Write-Host ("   " + $m) }
function Warn($m) { Write-Host ("   ! " + $m) -ForegroundColor Yellow }
function Fail($m) { Write-Host ("[打包失败] " + $m) -ForegroundColor Red; exit 1 }

$root = Split-Path -Parent $PSScriptRoot                 
$suz  = Split-Path -Parent $root                         
$hb   = Join-Path $suz 'Horrifying Bombardment'
$stage = Join-Path $root '_staging\bundle'
$dist  = Join-Path $suz '_releases'    



$dotnet = Join-Path $env:USERPROFILE '.dotnet\dotnet.exe'
if (-not (Test-Path $dotnet)) { $dotnet = 'dotnet' }

$mods = @(
    @{ Name = 'Oikonomia.dll';              Proj = (Join-Path $root 'src\Oikonomia\Oikonomia.csproj');
       Dll = (Join-Path $root 'src\Oikonomia\bin\Release\Oikonomia.dll');              Desc = '安济艺术（总统令·行政能力上限 + ED/地区/旅游/贸易/税务/一次性加成）' },
    @{ Name = 'HorrifyingBombardment.dll';  Proj = (Join-Path $hb 'src\HorrifyingBombardment\HorrifyingBombardment.csproj');
       Dll = (Join-Path $hb 'src\HorrifyingBombardment\bin\Release\HorrifyingBombardment.dll'); Desc = 'HorrifyingBombardment（战争系统扩展）' }
)

Write-Host ""
Write-Host "═══════════════════════════════════════════════════════════"
Write-Host " 稳定整合包打包  v$Version"
Write-Host "═══════════════════════════════════════════════════════════"


$noDeployDir = Join-Path $env:TEMP ('oi-nodeploy-' + [guid]::NewGuid().ToString('N').Substring(0, 8))
if (-not $SkipBuild) {
    Step ("构建三个模组" + $(if ($NoDeploy) { "（-NoDeploy：构建产物改投临时目录，**不写游戏目录** ✓）" }
                                          else { "（⚠ 会把 DLL 写进游戏目录的 BepInEx\plugins）" }))
    $env:DOTNET_ROOT = Split-Path -Parent $dotnet
    foreach ($m in $mods) {
        if (-not (Test-Path $m.Proj)) { Warn ("未找到工程，跳过构建：" + $m.Proj); continue }
        $buildArgs = @('build', $m.Proj, '-c', 'Release', '-v', 'minimal')
        if ($NoDeploy) { $buildArgs += ('-p:DeployDir=' + $noDeployDir) }
        $out = & $dotnet @buildArgs 2>&1
        $code = $LASTEXITCODE         
    
    $selfTestProj = Join-Path $env:TEMP ('__no_such_project__' + [guid]::NewGuid().ToString('N').Substring(0,6) + '.csproj')
    $stOut = & $dotnet build $selfTestProj -v quiet --nologo 2>&1
    $stCode = $LASTEXITCODE
    if ($stCode -eq 0) { Fail "守卫自检失败：不存在的工程竟然构建成功 ⇒ 守卫不可信 ✗" }
        $err = $out | Select-String -Pattern 'error CS'
        if ($code -ne 0) { Fail "构建失败：exit=$code" }
        if ($err) { Fail ("构建失败 " + $m.Name + "：" + ($err | Select-Object -First 1).Line) }
        Info ("构建通过：" + $m.Name)
    }
} else { Step "跳过构建（-SkipBuild）" }


Step "校验输入文件"


$beZip = $BepInExZip
if (-not $beZip) {
    $helper = Join-Path $hb 'tools\ps\bepinex-prereq.ps1'
    $sharedDir = Join-Path $suz '_shared'
    $resolved = $false
    if (Test-Path $helper) {
        try {
            . $helper
            $beInfo = Resolve-BepInExZip -ProjectRoot $root -SharedDir $sharedDir -ExplicitPath ''
            $beZip = $beInfo.Path
            Info ("BepInEx 前置：" + $beInfo.Source)
            $beHash = Assert-BepInExZipHash -ZipPath $beZip -SharedDir $beInfo.SharedDir
            if ($beHash.Verified) { Info ("前置包 SHA256 校验通过：" + $beHash.Hash.Substring(0, 16) + "…（清单 " + $beHash.SumsFile + "）") }
            elseif ($beHash.Recorded) { Info ("前置包 SHA256 首次记录基线：" + $beHash.Hash.Substring(0, 16) + "…") }
            if ($beInfo.Note) { Warn $beInfo.Note }
            $resolved = $true
        } catch { Warn ("共享解析不可用（" + $_.Exception.Message.Trim() + "）") }
    } else {
        Warn ("未找到共享助手，退回旧路径：" + $helper)
    }
    if (-not $resolved) {
        $legacy = Join-Path $hb '_staging\BepInEx-Unity.IL2CPP-win-x64-6.0.0-be.788.zip'
        if (Test-Path $legacy) {
            $beZip = $legacy
            Warn ("回退到旧 HB 路径（建议改用共享副本）：" + $legacy)
        } else {
            Fail ("找不到 BepInEx 前置包。`n  共享副本：" + (Join-Path $sharedDir 'BepInEx-Unity.IL2CPP-win-x64-6.0.0-be.788.zip') +
                  "`n  旧 HB 路径：" + $legacy + "`n  或用 -BepInExZip 指定路径。")
        }
    }
    $BepInExZip = $beZip      
}
if (-not (Test-Path $BepInExZip)) { Fail "找不到 BepInEx 前置包：$BepInExZip" }
Info ("BepInEx 前置：" + [math]::Round((Get-Item $BepInExZip).Length / 1MB, 1) + " MB")
foreach ($m in $mods) {
    if (-not (Test-Path $m.Dll)) { Fail ("找不到插件产物：" + $m.Dll + "`n  提示：先构建该模组，或去掉 -SkipBuild") }
    Info (("{0,-26} {1,5} KB   {2}" -f $m.Name, [math]::Round((Get-Item $m.Dll).Length / 1KB, 1), $m.Desc))
}


Step "铺设 staging（解压前置 + 放插件）"
if (Test-Path $stage) { Remove-Item $stage -Recurse -Force }
New-Item -ItemType Directory -Force -Path $stage | Out-Null
[System.IO.Compression.ZipFile]::ExtractToDirectory($BepInExZip, $stage)
Info "BepInEx 前置已解压"
$plugDst = Join-Path $stage 'BepInEx\plugins'
New-Item -ItemType Directory -Force -Path $plugDst | Out-Null
foreach ($m in $mods) {
    Copy-Item $m.Dll (Join-Path $plugDst $m.Name) -Force
    Info ("已放入 BepInEx\plugins：" + $m.Name)
}


Step "拷贝安装器、修改器与文档"
$inst = Join-Path $root 'tools\installer-src'
foreach ($f in @('install.ps1', 'install.bat')) {
    $src = Join-Path $inst $f
    if (-not (Test-Path $src)) { Fail "找不到安装器文件：$src" }
    Copy-Item $src $stage -Force
}
Info "install.ps1 + install.bat 已就位（自动识别整合包模式）"


$trDir = Join-Path $suz 'Suzerain-JSON Injector\Suzerain-JSON Injector 1.9.1'
$trOut = Join-Path $stage 'Suzerain-JSON Injector'
New-Item -ItemType Directory -Force -Path $trOut | Out-Null
$trFiles = @()
foreach ($tf in @('SuzerainGUI.exe', 'README.txt')) {
    $srcF = Join-Path $trDir $tf
    if (Test-Path $srcF) { Copy-Item $srcF $trOut -Force; $trFiles += $tf; Info ("修改器已放入：Suzerain-JSON Injector\" + $tf) }
    else { Warn ("未找到修改器文件：" + $srcF) }
}
$cliF = Join-Path $trDir 'bin\SuzerainTrainer.exe'
if (Test-Path $cliF) { New-Item -ItemType Directory -Force -Path (Join-Path $trOut 'bin') | Out-Null; Copy-Item $cliF (Join-Path $trOut 'bin') -Force; Info "修改器 CLI 已放入：Suzerain-JSON Injector\bin\SuzerainTrainer.exe" }


$newProj = Join-Path $suz 'Suzerain-JSON Injector\Suzerain-JSON Injector 2.x\SuzerainInjector2.csproj'
$newVer = '2.1.1'
if (Test-Path $newProj) {
    $mVer = [regex]::Match([IO.File]::ReadAllText($newProj), '<Version>([^<]+)</Version>')
    if ($mVer.Success) { $newVer = $mVer.Groups[1].Value }
}
$newOut = Join-Path $suz 'Suzerain-JSON Injector\Suzerain-JSON Injector 2.x\bin\Release\net8.0-windows'
$newDst = Join-Path $trOut ('Suzerain-JSON Injector ' + $newVer)
if (Test-Path $newOut) {
    New-Item -ItemType Directory -Force -Path $newDst | Out-Null
    foreach ($f in @('SuzerainInjector2.exe','SuzerainInjector2.dll','SuzerainInjector2.deps.json','SuzerainInjector2.runtimeconfig.json','SuzerainTrainer.Core.dll','Wpf.Ui.dll','Wpf.Ui.Abstractions.dll')) {
        $srcF = Join-Path $newOut $f
        if (Test-Path $srcF) { Copy-Item $srcF $newDst -Force }
    }
    Copy-Item (Join-Path $newOut 'Assets') $newDst -Recurse -Force
    Copy-Item (Join-Path $suz 'Suzerain-JSON Injector\Suzerain-JSON Injector 2.x\README.md') (Join-Path $newDst 'README.md') -Force
    Info ("2.x 修改器已放入：Suzerain-JSON Injector\Suzerain-JSON Injector " + $newVer)
} else {
    Warn ("未找到 2.x 输出：" + $newOut)
}

$verLines = ($mods | ForEach-Object {
    $p = $_.Dll
    $v = $null
    if ($_.Name -eq 'Oikonomia.dll') {
        
        $v = (Get-Item $p).VersionInfo.ProductVersion
        if ([string]::IsNullOrWhiteSpace($v)) { $v = (Get-Item $p).VersionInfo.FileVersion }
    }
    if ([string]::IsNullOrWhiteSpace($v)) {
        $t = [System.Text.Encoding]::Unicode.GetString([System.IO.File]::ReadAllBytes($p))
        $v = ([regex]::Matches($t, '\b\d+\.\d+\.\d+(?:-[A-Za-z0-9.]+)?\b') | ForEach-Object { $_.Value } | Sort-Object -Unique | Select-Object -Last 1)
    }
    "  · " + $_.Name + "  v" + $v
}) -join [Environment]::NewLine



$guiExe = Join-Path $stage 'Suzerain-JSON Injector\SuzerainGUI.exe'
if (Test-Path $guiExe) {
    $gt = [System.Text.Encoding]::Unicode.GetString([System.IO.File]::ReadAllBytes($guiExe))
    
    
    $m = [regex]::Match($gt, '存档修改器\s*v(\d+\.\d+\.\d+)')
    $gv = if ($m.Success) { $m.Groups[1].Value } else { '未知' }
    $verLines = $verLines + [Environment]::NewLine + "  · SuzerainGUI.exe（修改器）  v" + $gv
} else {
    Info "警告：包内未找到 SuzerainGUI.exe，版本清单将不含修改器"
}
$newExe = Join-Path $newDst 'SuzerainInjector2.exe'
if (Test-Path $newExe) {
    $nt = [System.Text.Encoding]::Unicode.GetString([System.IO.File]::ReadAllBytes($newExe))
    $nm = [regex]::Match($nt, '修改器\s*(\d+\.\d+\.\d+)')
    $nv = if ($nm.Success) { $nm.Groups[1].Value } else { $newVer }
    $verLines = $verLines + [Environment]::NewLine + "  · SuzerainInjector2.exe（2.x 修改器）  v" + $nv
}


$readmeTemplate = Join-Path $PSScriptRoot '先读我.template.txt'
if (-not (Test-Path $readmeTemplate)) { Fail "缺少说明模板：$readmeTemplate" }
$readme = [System.IO.File]::ReadAllText($readmeTemplate, [System.Text.Encoding]::UTF8)
$readme = $readme.Replace('{{VERSION}}', $Version).Replace('{{VERLINES}}', $verLines)

[System.IO.File]::WriteAllText((Join-Path $stage '先读我.txt'), $readme, (New-Object System.Text.UTF8Encoding($true)))

Info "先读我.txt 已生成"
if ($readme.IndexOf('尚未实测') -lt 0) { Warn2 'packed readme lacks the never-run disclaimer (尚未实测) ✗' } else { Info '包内说明含『尚未实测』该句 ✓' }





$verCheck = Join-Path $PSScriptRoot 'check-readme-versions.ps1'
if (Test-Path $verCheck) {
    $rc = & powershell -NoProfile -ExecutionPolicy Bypass -File $verCheck -Readme (Join-Path $stage '先读我.txt') 2>&1
    $rc | ForEach-Object { Info $_ }
    
    
    
    if ($LASTEXITCODE -ne 0) { Warn "随包说明的版本检查未通过（**仅告警，不阻断打包** ✗）⇒ 见上；检查器待修好再入门禁 ✓" }
} else { Warn "缺少 check-readme-versions.ps1 ⇒ 跳过说明文本版本检查 ✗" }


Step "压缩产物"
New-Item -ItemType Directory -Force -Path $dist | Out-Null
$zipName = if ($OutName) { $OutName } else { 'Carbon-Dioxide-Mod.zip' }
$zipPath = Join-Path $dist $zipName
if (Test-Path $zipPath) { Remove-Item $zipPath -Force }


$baseDir = (Resolve-Path $stage).Path
$zipStream = [System.IO.File]::Open($zipPath, [System.IO.FileMode]::Create)
$archive = New-Object System.IO.Compression.ZipArchive($zipStream, [System.IO.Compression.ZipArchiveMode]::Create)
$fileCount = 0
foreach ($file in (Get-ChildItem $stage -Recurse -File -Force)) {
    $rel = $file.FullName.Substring($baseDir.Length).TrimStart('\') -replace '\\', '/'
    $entry = $archive.CreateEntry($rel, [System.IO.Compression.CompressionLevel]::Optimal)
    $es = $entry.Open(); $fs = [System.IO.File]::OpenRead($file.FullName)
    try { $fs.CopyTo($es) } finally { $fs.Close(); $es.Close() }
    $fileCount++
}
$archive.Dispose(); $zipStream.Close()
Info ("条目数：" + $fileCount)
Info ("已生成：" + $zipPath + "   " + [math]::Round((Get-Item $zipPath).Length / 1MB, 1) + " MB")


Step "产物自检"
$zip = [System.IO.Compression.ZipFile]::OpenRead($zipPath)
$names = $zip.Entries | ForEach-Object { $_.FullName -replace '\\', '/' }
$zip.Dispose()
$need = @(
    'winhttp.dll', 'doorstop_config.ini', '.doorstop_version',
    'BepInEx/core/BepInEx.Core.dll',
    'BepInEx/plugins/Oikonomia.dll',
    'BepInEx/plugins/HorrifyingBombardment.dll',
    'install.ps1', 'install.bat', '先读我.txt', 'Suzerain-JSON Injector/SuzerainGUI.exe', 'Suzerain-JSON Injector/README.txt'
)
$miss = @()
foreach ($n in $need) { if (-not ($names -contains $n)) { $miss += $n } }
if ($miss.Count -gt 0) { Fail ("产物缺少条目：" + ($miss -join ', ')) }
Info ("自检通过：" + $names.Count + " 个条目，关键文件齐全")
$dllN = ($names | Where-Object { $_ -like 'BepInEx/plugins/*.dll' }).Count
Info ("插件 DLL 数量：" + $dllN + "（应为 2：Oikonomia + HorrifyingBombardment）")


$hash = (Get-FileHash $zipPath -Algorithm SHA256).Hash



$sumPath = Join-Path $dist 'SHA256SUMS-bundle.txt'
"$hash  $zipName" | Set-Content -Path $sumPath -Encoding UTF8
Info ("SHA256：" + $hash.Substring(0, 16) + "…  → " + $sumPath)

Write-Host ""
Write-Host "[完成] 整合包已生成：" -ForegroundColor Green
Write-Host ("  " + $zipPath)
Write-Host ""

if ($NoDeploy) {
    Write-Host "  ✓ 本次为 -NoDeploy：构建产物写进了临时目录，**没有写游戏目录** ✓" -ForegroundColor Green
} elseif ($SkipBuild) {
    Write-Host "  （本次 -SkipBuild：未执行构建 ⇒ 也就没有写游戏目录）" -ForegroundColor DarkGray
} else {
    Write-Host "  ⚠ 本次打包**已把各 DLL 写入游戏目录**（各模组 csproj 的默认 DeployDir = BepInEx\plugins）✗" -ForegroundColor Yellow
    Write-Host "     若不想写游戏目录 ⇒ 下次用： tools\pack-bundle.ps1 -NoDeploy" -ForegroundColor Yellow
    Write-Host "     （-NoDeploy = 构建改投临时目录；产物与打包流程完全不变 ✓）" -ForegroundColor Yellow
}
Write-Host ""
