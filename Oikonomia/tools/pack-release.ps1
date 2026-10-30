



















param(
    [string]$Version = '',
    [ValidateSet('PluginOnly', 'Full', 'Both')][string]$Mode = 'PluginOnly',
    
    
    [string]$BepInExZip = '',
    [switch]$SkipBuild,
    [switch]$SkipTests,
    
    
    
    [switch]$NoDeploy
)

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.IO.Compression -ErrorAction SilentlyContinue
Add-Type -AssemblyName System.IO.Compression.FileSystem -ErrorAction SilentlyContinue

$projRoot = Split-Path $PSScriptRoot -Parent
$dist     = Join-Path $projRoot 'dist'
$srcProj  = Join-Path $projRoot 'src\Oikonomia'
$dll      = Join-Path $srcProj 'bin\Release\Oikonomia.dll'
$instSrc  = Join-Path $projRoot 'tools\installer-src'

function Info($m) { Write-Host ("  " + $m) }
function Step($m) { Write-Host ("== " + $m) }
function Warn($m) { Write-Host ("  ! " + $m) -ForegroundColor Yellow }
function Fail($m) { Write-Host ("[打包失败] " + $m) -ForegroundColor Red; exit 1 }




if (-not $SkipTests) {
    $runner = Join-Path $PSScriptRoot 'run-tests.ps1'
    if (-not (Test-Path $runner)) { Fail "找不到测试脚本：$runner（发布门禁必须有测试）" }
    Step "测试门禁：运行 tests\ 全部测试"
    & powershell -ExecutionPolicy Bypass -File $runner -Quiet
    if ($LASTEXITCODE -ne 0) {
        Fail "测试未通过（见上方输出）—— 已中止打包。修复后重试，或加 -SkipTests 显式跳过（不建议）"
    }
    Info "测试门禁通过"
}





if (-not $Version) {
    $csprojPath = Join-Path $srcProj 'Oikonomia.csproj'
    if (-not (Test-Path $csprojPath)) { Fail "找不到 $csprojPath" }
    $m = [regex]::Match((Get-Content $csprojPath -Raw), '<Version>\s*([^<\s]+)\s*</Version>')
    if (-not $m.Success) { Fail "无法从 $csprojPath 解析 <Version>（★ 唯一权威缺失 ⇒ 中止 ✗）" }
    $Version = $m.Groups[1].Value
    Info ("版本号（★ 权威 = csproj ✓）：" + $Version + " ｜ 来源：" + $csprojPath)
}
Step "打包 安济艺术 Oikonomia v$Version（模式 $Mode）"


$noDeployDir = Join-Path $env:TEMP ('oi-nodeploy-rel-' + [guid]::NewGuid().ToString('N').Substring(0, 8))
if (-not $SkipBuild) {
    $dotnet = 'C:\Users\dell\.dotnet\dotnet.exe'
    if (-not (Test-Path $dotnet)) { $dotnet = 'dotnet' }
    $env:DOTNET_ROOT = Split-Path $dotnet -Parent
    Info ("编译中：" + $dotnet + $(if ($NoDeploy) { "（-NoDeploy：产物改投临时目录，**不写游戏目录** ✓）" }
                                              else { "（⚠ 会把 DLL 写进游戏目录的 BepInEx\plugins）" }))
    $buildArgs = @('build', (Join-Path $srcProj 'Oikonomia.csproj'), '-c', 'Release', '-v', 'minimal')
    if ($NoDeploy) { $buildArgs += ('-p:DeployDir=' + $noDeployDir) }
    $out = & $dotnet @buildArgs 2>&1
    $code = $LASTEXITCODE     
    
    
    
    $selfTestProj = Join-Path $env:TEMP ('__no_such_project__' + [guid]::NewGuid().ToString('N').Substring(0,6) + '.csproj')
    $stOut = & $dotnet build $selfTestProj -v quiet --nologo 2>&1
    $stCode = $LASTEXITCODE
    if ($stCode -eq 0) { Fail "守卫自检失败：不存在的工程竟然构建成功 ⇒ 守卫不可信 ✗" }
    Info ("守卫自检 ✓：故意触发（工程不存在）⇒ exit=" + $stCode + "（★ 判失败 ✓）")
    $csErr = $out | Select-String -Pattern 'error CS'
    if ($code -ne 0) { Fail "编译失败：exit=$code" }
    if ($csErr) { $csErr | ForEach-Object { Write-Host $_.Line }; Fail "编译失败" }
    Info "编译通过"
}


if (-not (Test-Path $dll)) { Fail "找不到插件 DLL：$dll（请先编译）" }
$dllText = [System.Text.Encoding]::Unicode.GetString([IO.File]::ReadAllBytes($dll))
if ($dllText.IndexOf($Version) -lt 0) { Fail "DLL 内不含版本串 $Version —— 请同步 Plugin.cs 版本号并重新编译" }
Info ("DLL 校验通过（含版本串 $Version）：" + [math]::Round((Get-Item $dll).Length / 1KB, 1) + " KB")
  
  
  $cfgChk = Join-Path $PSScriptRoot 'check-config-defaults.ps1'
  if (Test-Path $cfgChk) {
      & powershell -ExecutionPolicy Bypass -File $cfgChk -Dll $dll
      if ($LASTEXITCODE -ne 0) { Fail "配置默认值里出现本机绝对路径/盘符/用户名 ⇒ 打包中止（见上）✗" }
      Info "配置默认值检查通过（无本机绝对路径）✓"
  } else { Warn2 "找不到 check-config-defaults.ps1 ⇒ 跳过该项检查 ✗" }


$inst = Join-Path $instSrc 'install.ps1'
if (-not (Test-Path $inst)) { Fail "找不到 $inst" }
$bytes = [IO.File]::ReadAllBytes($inst)
if (-not ($bytes.Length -ge 3 -and $bytes[0] -eq 0xEF -and $bytes[1] -eq 0xBB -and $bytes[2] -eq 0xBF)) {
    Fail "install.ps1 缺少 UTF-8 BOM（PS 5.1 会按 GBK 解析导致中文乱码）"
}
Info "install.ps1 编码校验通过（UTF-8 BOM）"

$modes = if ($Mode -eq 'Both') { @('PluginOnly', 'Full') } else { @($Mode) }








if ($modes -contains 'Full') {
    $beZip = $null
    $useHelper = $false
    $helper = Join-Path (Split-Path $projRoot -Parent) 'Horrifying Bombardment\tools\ps\bepinex-prereq.ps1'
    $sharedDir = Join-Path (Split-Path $projRoot -Parent) '_shared'
    if (Test-Path $helper) {
        try {
            . $helper
            $beInfo = Resolve-BepInExZip -ProjectRoot $projRoot -SharedDir $sharedDir -ExplicitPath $BepInExZip
            $beZip = $beInfo.Path
            $BepInExZip = $beZip      
            Info ("BepInEx 前置：" + $beInfo.Source + " → " + $beZip)
            $beHash = Assert-BepInExZipHash -ZipPath $beZip -SharedDir $beInfo.SharedDir
            if ($beHash.Verified) { Info ("前置包 SHA256 校验通过：" + $beHash.Hash.Substring(0, 16) + "…（清单 " + $beHash.SumsFile + "）") }
            elseif ($beHash.Recorded) { Info ("前置包 SHA256 首次记录基线：" + $beHash.Hash.Substring(0, 16) + "… → " + $beHash.SumsFile) }
            if ($beInfo.Note) { Warn $beInfo.Note }
            $useHelper = $true
        } catch { Warn ("共享解析不可用（" + $_.Exception.Message.Trim() + "）") }
    } else {
        Warn ("未找到共享助手，退回旧路径：" + $helper)
    }

    if (-not $useHelper) {
        $legacy = Join-Path (Split-Path $projRoot -Parent) 'Horrifying Bombardment\_staging\BepInEx-Unity.IL2CPP-win-x64-6.0.0-be.788.zip'
        if (Test-Path $legacy) {
            $BepInExZip = $legacy
            $beZip = $legacy
            Warn ("回退到旧 HB 路径（建议改用共享副本）：" + $legacy)
        } else {
            Fail ("找不到 BepInEx 前置包。`n  共享副本：" + (Join-Path $sharedDir 'BepInEx-Unity.IL2CPP-win-x64-6.0.0-be.788.zip') +
                  "`n  旧 HB 路径：" + $legacy + "`n  或用 -BepInExZip 指定路径。")
        }
    }
}

New-Item -ItemType Directory -Force -Path $dist | Out-Null
$made = @()

foreach ($md in $modes) {
    Step "生成 $md 包"
    $stage = Join-Path $projRoot ("_staging\pack-" + $md.ToLower())
    if (Test-Path $stage) { Remove-Item $stage -Recurse -Force }
    New-Item -ItemType Directory -Force -Path $stage | Out-Null

    Copy-Item $dll (Join-Path $stage 'Oikonomia.dll') -Force
    Copy-Item (Join-Path $projRoot 'README.md') $stage -Force
    Copy-Item (Join-Path $projRoot 'CHANGELOG.md') $stage -Force
    Copy-Item $inst $stage -Force
    Copy-Item (Join-Path $instSrc 'install.bat') $stage -Force

    $readme = @"
安济艺术 Oikonomia v$Version — Suzerain 总统令无限制 + 经济结算四机制（$md 包）

安装（三选一）：
  1) 双击「install.bat」——自动定位游戏目录并安装
  2) 手动：把 Oikonomia.dll 复制到 <游戏目录>\BepInEx\plugins\
  3) 命令行：powershell -ExecutionPolicy Bypass -File install.ps1 -GameDir "<游戏目录>"

★ 实验功能【默认全部关闭】；其运行时表现【尚未实测】✗
  开关：BepInEx\config\com.dell.suzerain.oikonomia.cfg ⇒ [Bench] Enabled · [Company] Enabled · [Diag] ScreenDump · [V2] Enabled（默认全 false）
  要试用请自行打开并自负风险；不打开 ⇒ 行为 = 已验证功能集 ✓

前置：BepInEx 6 (IL2CPP)。本包不含前置；如需一并安装请使用整合包（Full）。
配置：首次启动游戏后生成 <游戏目录>\BepInEx\config\com.dell.suzerain.oikonomia.cfg
卸载：删除 Oikonomia.dll 即可（或用 install.ps1 -Uninstall）

详见 README.md / CHANGELOG.md
"@
    [System.IO.File]::WriteAllText((Join-Path $stage '先读我.txt'), $readme, (New-Object System.Text.UTF8Encoding($true)))

    if ($md -eq 'Full') {
        Copy-Item $BepInExZip (Join-Path $stage (Split-Path $BepInExZip -Leaf)) -Force
        Info ("已包含 BepInEx 前置：" + (Split-Path $BepInExZip -Leaf))
    }

    $zipPath = Join-Path $dist ("Oikonomia-v$Version-$md.zip")
    if (Test-Path $zipPath) { Remove-Item $zipPath -Force }
    [System.IO.Compression.ZipFile]::CreateFromDirectory($stage, $zipPath, [System.IO.Compression.CompressionLevel]::Optimal, $false)

    
    $zip = [System.IO.Compression.ZipFile]::OpenRead($zipPath)
    try {
        $names = $zip.Entries | ForEach-Object { $_.FullName }
if (Test-Path (Join-Path $stage '先读我.txt')) { $rmBody = [System.IO.File]::ReadAllText((Join-Path $stage '先读我.txt'), [System.Text.Encoding]::UTF8); if ($rmBody.IndexOf('尚未实测') -lt 0) { Warn2 'packed readme lacks the never-run disclaimer ✗' } else { Info '包内说明含『尚未实测』该句 ✓' } }
	foreach ($need in @('Oikonomia.dll', 'README.md', 'CHANGELOG.md', 'install.ps1', 'install.bat', '先读我.txt')) {
            if ($names -notcontains $need) { Fail "$md 包缺少 $need" }
        }
        Info ("自检通过：$($names.Count) 个条目；" + [math]::Round((Get-Item $zipPath).Length / 1KB, 1) + " KB -> " + $zipPath)
    } finally { $zip.Dispose() }
    $made += $zipPath
}


Step "生成校验和"
$sumFile = Join-Path $dist 'SHA256SUMS.txt'
  
$lines = @('# 本清单对应【本次打包】产物（★ 整合包清单是 Suzerain\_releases\SHA256SUMS-bundle.txt ✗ 勿混用 ✓）')
foreach ($z in $made) {
    $h = (Get-FileHash $z -Algorithm SHA256).Hash.ToLower()
    $lines += ($h + '  ' + (Split-Path $z -Leaf))
    Info ((Split-Path $z -Leaf) + "  " + $h.Substring(0, 16) + "…")
}


[System.IO.File]::WriteAllText($sumFile, (($lines -join "`n") + "`n"), (New-Object System.Text.UTF8Encoding($false)))
Info "已写入 $sumFile"

Write-Host ""
Write-Host "[完成] 打包结束，产物在 $dist"
