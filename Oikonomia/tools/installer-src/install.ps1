

















param(
    [string]$GameDir = '',
    [switch]$Uninstall,
    [switch]$Quiet,                       
    [switch]$SelfTest,                    
    [string]$SearchRoots = '',            
    [Parameter(ValueFromRemainingArguments = $true)]$Rest
)

$ErrorActionPreference = 'Stop'
$PluginName = 'Oikonomia.dll'
$PluginNames = @('Oikonomia.dll', 'HorrifyingBombardment.dll', 'HB.WarProbe.dll')   
$CfgName    = 'com.dell.suzerain.oikonomia.cfg'
$scriptDir  = Split-Path -Parent $MyInvocation.MyCommand.Path

function Info($m)  { Write-Host ("  " + $m) }
function Warn($m)  { Write-Host ("  ! " + $m) -ForegroundColor Yellow }
function Fail($m)  { Write-Host ("[安装失败] " + $m) -ForegroundColor Red; exit 1 }


function Test-GameDir([string]$dir) {
    if ([string]::IsNullOrWhiteSpace($dir)) { return $false }
    try {
        if (-not (Test-Path (Join-Path $dir 'Suzerain.exe'))) { return $false }
        if (-not (Test-Path (Join-Path $dir 'Suzerain_Data'))) { return $false }
        return $true
    } catch { return $false }
}

function Get-GameScore([string]$dir) {
    $s = 0
    try {
        if (Test-Path (Join-Path $dir 'GameAssembly.dll')) { $s += 2 }      
        if (Test-Path (Join-Path $dir 'UnityPlayer.dll'))  { $s += 2 }
        if (Test-Path (Join-Path $dir 'Suzerain_Data\app.info')) {
            $app = Get-Content (Join-Path $dir 'Suzerain_Data\app.info') -Raw -ErrorAction SilentlyContinue
            if ($app -match 'Torpor') { $s += 3 }
        }
        if (Test-Path (Join-Path $dir 'Suzerain_Data\data.unity3d')) { $s += 1 }
    } catch { }
    return $s
}

function Normalize-Dir([string]$p) {
    if ([string]::IsNullOrWhiteSpace($p)) { return '' }
    $p = $p.Trim().Trim('"')
    try {
        if (Test-Path $p -PathType Leaf) { $p = Split-Path $p -Parent }     # 传进来的是 exe
    } catch { }
    return $p
}

$script:candidates = New-Object System.Collections.ArrayList
function Add-Candidate([string]$dir, [string]$source) {
    $d = Normalize-Dir $dir
    if ([string]::IsNullOrWhiteSpace($d)) { return }
    if (-not (Test-GameDir $d)) { return }
    foreach ($c in $script:candidates) { if ($c.Dir -eq $d) { return } }
    [void]$script:candidates.Add([pscustomobject]@{ Dir = $d; Source = $source; Score = (Get-GameScore $d) })
}

function Get-ArgPaths {
    $out = New-Object System.Collections.ArrayList
    if ($GameDir) { [void]$out.Add($GameDir) }
    if ($Rest) { foreach ($r in $Rest) { if ($r -and $r.ToString().Trim().Length -gt 0) { [void]$out.Add($r.ToString()) } } }
    return $out
}

function Get-FromProcess {
    try {
        $p = Get-Process -Name Suzerain -ErrorAction SilentlyContinue | Select-Object -First 1
        if ($p -and $p.Path) { return (Split-Path $p.Path -Parent) }
    } catch { }
    return ''
}

function Get-SteamLibraries {
    $roots = New-Object System.Collections.ArrayList
    foreach ($hive in @('HKCU:\Software\Valve\Steam', 'HKLM:\SOFTWARE\WOW6432Node\Valve\Steam', 'HKLM:\SOFTWARE\Valve\Steam')) {
        try {
            $v = (Get-ItemProperty -Path $hive -ErrorAction SilentlyContinue)
            foreach ($name in @('SteamPath', 'InstallPath')) {
                if ($v -and $v.$name) { [void]$roots.Add($v.$name) }
            }
        } catch { }
    }
    foreach ($p in @('C:\Program Files (x86)\Steam', 'C:\Program Files\Steam', 'D:\Steam', 'E:\Steam', 'F:\Steam')) {
        if (Test-Path $p) { [void]$roots.Add($p) }
    }
    $libs = New-Object System.Collections.ArrayList
    foreach ($r in ($roots | Select-Object -Unique)) {
        [void]$libs.Add($r)
        foreach ($vdfName in @('steamapps\libraryfolders.vdf', 'config\libraryfolders.vdf')) {
            $vdf = Join-Path $r $vdfName
            if (Test-Path $vdf) {
                try {
                    $txt = Get-Content $vdf -Raw
                    foreach ($m in [regex]::Matches($txt, '"path"\s+"([^"]+)"')) {
                        $p = $m.Groups[1].Value -replace '\\\\', '\'
                        if ($p) { [void]$libs.Add($p) }
                    }
                } catch { }
            }
        }
    }
    return ($libs | Select-Object -Unique)
}


function Get-FromGog {
    $out = New-Object System.Collections.ArrayList
    foreach ($base in @('HKLM:\SOFTWARE\WOW6432Node\GOG.com\Games', 'HKLM:\SOFTWARE\GOG.com\Games', 'HKCU:\SOFTWARE\GOG.com\Games')) {
        if (-not (Test-Path $base)) { continue }
        foreach ($k in (Get-ChildItem $base -ErrorAction SilentlyContinue)) {
            try {
                $v = Get-ItemProperty -Path $k.PSPath -ErrorAction SilentlyContinue
                if ($v.path) { [void]$out.Add($v.path) }
                elseif ($v.exe) { [void]$out.Add((Split-Path ($v.exe -replace '"', '') -Parent)) }
            } catch { }
        }
    }
    return $out
}

function Get-FromEpic {
    $out = New-Object System.Collections.ArrayList
    $dirs = @("$env:ProgramData\Epic\EpicGamesLauncher\Data\Manifests",
              "$env:LOCALAPPDATA\EpicGamesLauncher\Saved\Config\Windows")
    foreach ($d in $dirs) {
        if (-not (Test-Path $d)) { continue }
        foreach ($f in (Get-ChildItem $d -Filter *.item -ErrorAction SilentlyContinue)) {
            try {
                $json = Get-Content $f.FullName -Raw
                if ($json -match 'Suzerain') {
                    $m = [regex]::Match($json, '"InstallLocation"\s*:\s*"([^"]+)"')
                    if ($m.Success) { [void]$out.Add(($m.Groups[1].Value -replace '\\\\', '\')) }
                }
            } catch { }
        }
    }
    return $out
}


function Get-FromUninstall {
    $out = New-Object System.Collections.ArrayList
    $bases = @('HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall',
               'HKLM:\SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall',
               'HKCU:\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall')
    foreach ($base in $bases) {
        if (-not (Test-Path $base)) { continue }
        foreach ($k in (Get-ChildItem $base -ErrorAction SilentlyContinue)) {
            try {
                $v = Get-ItemProperty -Path $k.PSPath -ErrorAction SilentlyContinue
                if (-not $v.DisplayName) { continue }
                if ($v.DisplayName -notmatch 'Suzerain') { continue }
                if ($v.InstallLocation) { [void]$out.Add($v.InstallLocation) }
                if ($v.DisplayIcon) { [void]$out.Add((Split-Path ($v.DisplayIcon -replace '"', '').Split(',')[0] -Parent)) }
                if ($v.UninstallString) {
                    $u = ($v.UninstallString -replace '"', '').Split(' ')[0]
                    if ($u) { [void]$out.Add((Split-Path $u -Parent)) }
                }
            } catch { }
        }
    }
    return $out
}


function Get-LikelyPaths {
    $out = New-Object System.Collections.ArrayList
    $drives = @()
    try {
        $drives = [System.IO.DriveInfo]::GetDrives() | Where-Object { $_.IsReady -and ($_.DriveType -eq 'Fixed' -or $_.DriveType -eq 'Removable') } | ForEach-Object { $_.RootDirectory.FullName }
    } catch { $drives = @('C:\') }
    $rel = @('Suzerain', 'Games\Suzerain', 'GOG Games\Suzerain', 'GOG Galaxy\Games\Suzerain', 'Epic Games\Suzerain',
             'Program Files\Suzerain', 'Program Files (x86)\Suzerain', 'SteamLibrary\steamapps\common\Suzerain',
             'Steam\steamapps\common\Suzerain', 'Games\Steam\steamapps\common\Suzerain', 'Suzerain\Suzerain')
    foreach ($d in $drives) {
        foreach ($r in $rel) { [void]$out.Add((Join-Path $d $r)) }
    }
    return $out
}


function Search-GameDirs([string[]]$roots, [int]$maxDepth = 3, [int]$maxDirs = 3000) {
    $skip = @('windows', '$recycle.bin', 'system volume information', 'programdata', 'appdata', 'application data',
              'node_modules', '.git', 'recovery', 'perflogs', 'msocache', 'temp', 'tmp', 'cache', '$windows.~bt',
              '$windows.~ws', 'onedrivetemp', 'packages', 'driverstore', 'winsxs', 'installer', 'assembly',
              'windowsapps', 'servicing', 'softwaredistribution')
    $found = New-Object System.Collections.ArrayList
    foreach ($root in $roots) {
        if (-not (Test-Path $root)) { continue }
        
        $level = @($root)
        $visited = 0
        for ($depth = 0; $depth -le $maxDepth; $depth++) {
            if ($found.Count -gt 0) { break }
            $next = New-Object System.Collections.ArrayList
            foreach ($dir in $level) {
                if ($visited -ge $maxDirs) { break }
                $visited++
                if (Test-GameDir $dir) { [void]$found.Add($dir); break }
                if ($depth -eq $maxDepth) { continue }
                try {
                    foreach ($sub in [System.IO.Directory]::GetDirectories($dir)) {
                        $name = [System.IO.Path]::GetFileName($sub).ToLower()
                        if ($name.StartsWith('$')) { continue }
                        if ($skip -contains $name) { continue }
                        [void]$next.Add($sub)
                    }
                } catch { }
            }
            if ($found.Count -gt 0) { break }
            if ($next.Count -eq 0) { break }
            $level = $next.ToArray()
        }
        if ($found.Count -gt 0) { break }
    }
    return $found
}
function Get-FixedRoots {
    try {
        return ([System.IO.DriveInfo]::GetDrives() | Where-Object { $_.IsReady -and ($_.DriveType -eq 'Fixed' -or $_.DriveType -eq 'Removable') } | ForEach-Object { $_.RootDirectory.FullName })
    } catch { return @('C:\') }
}


function Ask-GameDir {
    if ($Quiet) { return '' }
    Write-Host ""
    Write-Host "  无法自动定位游戏目录。请手动指定：" -ForegroundColor Yellow
    Write-Host "   · 可直接把 Suzerain.exe 或游戏文件夹拖到本窗口后回车"
    Write-Host "   · 或输入完整路径（例如 D:\Games\Suzerain）"
    try {
        Add-Type -AssemblyName System.Windows.Forms -ErrorAction SilentlyContinue
        $dlg = New-Object System.Windows.Forms.FolderBrowserDialog
        $dlg.Description = "请选择 Suzerain 游戏目录（包含 Suzerain.exe 的那一层）"
        $dlg.ShowNewFolderButton = $false
        if ($dlg.ShowDialog() -eq [System.Windows.Forms.DialogResult]::OK) {
            if (Test-GameDir $dlg.SelectedPath) { return $dlg.SelectedPath }
            Warn ("所选目录不是 Suzerain 游戏目录：" + $dlg.SelectedPath)
        }
    } catch { }
    try {
        $typed = Read-Host "  路径"
        $typed = Normalize-Dir $typed
        if (Test-GameDir $typed) { return $typed }
        Warn ("该路径不是 Suzerain 游戏目录：" + $typed)
    } catch { }
    return ''
}


Write-Host "=============================================="
Write-Host " 安济艺术 Oikonomia - Suzerain 总统令无限制 + 经济结算"
Write-Host "=============================================="
Write-Host ""


if ($SelfTest) {
    Write-Host "  [自检模式] 验证定位逻辑（不会安装或修改任何文件）"
    $ok = 0; $bad = 0
    $sandbox = Join-Path $env:TEMP ("rp-selftest-" + [guid]::NewGuid().ToString('N').Substring(0, 8))
    $fakeGame = Join-Path $sandbox "Games\Suzerain"
    try {
        New-Item -ItemType Directory -Force -Path (Join-Path $fakeGame 'Suzerain_Data'), (Join-Path $fakeGame 'BepInEx\core') | Out-Null
        Set-Content (Join-Path $fakeGame 'Suzerain.exe') 'dummy' -NoNewline
        Set-Content (Join-Path $fakeGame 'Suzerain_Data\app.info') "Torpor Games`nSuzerain" -NoNewline
        Set-Content (Join-Path $fakeGame 'Suzerain_Data\data.unity3d') 'dummy' -NoNewline
        Set-Content (Join-Path $fakeGame 'GameAssembly.dll') 'dummy' -NoNewline
        Set-Content (Join-Path $fakeGame 'UnityPlayer.dll') 'dummy' -NoNewline
        $emptyDir = Join-Path $sandbox 'Empty'
        New-Item -ItemType Directory -Force -Path $emptyDir | Out-Null

        if (Test-GameDir $fakeGame) { Write-Host "  [OK] 签名识别：伪游戏目录被正确识别"; $ok++ }
        else { Write-Host "  [FAIL] 签名识别失败" -ForegroundColor Red; $bad++ }

        if (-not (Test-GameDir $emptyDir)) { Write-Host "  [OK] 负例：空目录未被误判"; $ok++ }
        else { Write-Host "  [FAIL] 空目录被误判为游戏" -ForegroundColor Red; $bad++ }

        $score = Get-GameScore $fakeGame
        if ($score -ge 6) { Write-Host ("  [OK] 评分=" + $score + "（GameAssembly/UnityPlayer/app.info 命中）"); $ok++ }
        else { Write-Host ("  [FAIL] 评分过低：" + $score) -ForegroundColor Red; $bad++ }

        $found = @(Search-GameDirs @($sandbox))     
        $hit = ($found.Count -ge 1 -and ([string]$found[0]).TrimEnd('\') -ieq ([string]$fakeGame).TrimEnd('\'))
        if ($hit) { Write-Host "  [OK] 磁盘浅层扫描：命中深 2 层的伪游戏目录"; $ok++ }
        else {
            Write-Host ("  [FAIL] 扫描未命中（找到 " + $found.Count + " 个）") -ForegroundColor Red
            Write-Host ("         期望：" + $fakeGame)
            if ($found.Count -ge 1) { Write-Host ("         实际：" + $found[0]) }
            $bad++
        }

        $script:candidates.Clear()
        Add-Candidate $fakeGame "自检"
        if ($script:candidates.Count -eq 1) { Write-Host "  [OK] 候选登记与去重"; $ok++ }
        else { Write-Host "  [FAIL] 候选登记异常" -ForegroundColor Red; $bad++ }

        Add-Candidate (Join-Path $fakeGame 'Suzerain.exe') "自检(exe 路径)"
        if ($script:candidates.Count -eq 1) { Write-Host "  [OK] 传入 exe 路径可归一化为目录（未产生重复候选）"; $ok++ }
        else { Write-Host "  [FAIL] exe 归一化失败" -ForegroundColor Red; $bad++ }

        Add-Candidate $emptyDir "自检(非游戏目录)"
        if ($script:candidates.Count -eq 1) { Write-Host "  [OK] 非游戏目录被拒绝登记"; $ok++ }
        else { Write-Host "  [FAIL] 非游戏目录被误登记" -ForegroundColor Red; $bad++ }
    }
    catch { Write-Host ("  [FAIL] 自检异常：" + $_.Exception.Message) -ForegroundColor Red; $bad++ }
    finally { Remove-Item $sandbox -Recurse -Force -ErrorAction SilentlyContinue }

    Write-Host ""
    Write-Host ("  自检结果：通过 " + $ok + "，失败 " + $bad)
    if ($bad -eq 0) { Write-Host "SELFTEST_OK"; exit 0 } else { Write-Host "SELFTEST_FAIL"; exit 1 }
}


$explicitPaths = @(Get-ArgPaths)
foreach ($p in $explicitPaths) { Add-Candidate $p "命令行/拖拽路径" }


if ($explicitPaths.Count -gt 0 -and $script:candidates.Count -eq 0) {
    $badList = ($explicitPaths -join "；")
    $badMsg = "指定的路径不是有效的 Suzerain 游戏目录：" + $badList
    $badMsg = $badMsg + "。该目录应包含 Suzerain.exe 与 Suzerain_Data（可传游戏目录，也可直接传 exe）。"
    $badMsg = $badMsg + "如不确定位置，请不要带 -GameDir，让脚本自动搜索。"
    Fail $badMsg
}

if ($script:candidates.Count -eq 0) { $q = Get-FromProcess; if ($q) { Add-Candidate $q "运行中的游戏进程" } }
if ($script:candidates.Count -eq 0) {
    foreach ($lib in (Get-SteamLibraries)) { Add-Candidate (Join-Path $lib 'steamapps\common\Suzerain') "Steam 库" }
}
if ($script:candidates.Count -eq 0) {
    foreach ($g in (Get-FromGog)) { Add-Candidate $g "GOG 注册表" }
}
if ($script:candidates.Count -eq 0) {
    foreach ($e in (Get-FromEpic)) { Add-Candidate $e "Epic 清单" }
}
if ($script:candidates.Count -eq 0) {
    foreach ($u in (Get-FromUninstall)) { Add-Candidate $u "卸载注册表" }
}
if ($script:candidates.Count -eq 0) {
    foreach ($p in (Get-LikelyPaths)) { Add-Candidate $p "常见安装路径" }
}
if ($script:candidates.Count -eq 0 -and $SearchRoots) {
    foreach ($r in ($SearchRoots -split ';')) { Add-Candidate $r "指定搜索根" }
}
if ($script:candidates.Count -eq 0) {
    Write-Host "  未在常见来源中找到，正在扫描磁盘（深度 3，跳过系统目录）…"
    $found = Search-GameDirs (Get-FixedRoots)
    foreach ($g in $found) { Add-Candidate $g "磁盘扫描" }
}
if ($script:candidates.Count -eq 0) {
    Add-Candidate (Ask-GameDir) "手动指定"
}

if ($script:candidates.Count -eq 0) {
    Fail "未能定位 Suzerain 游戏目录。`n  请用 -GameDir 指定（该目录应包含 Suzerain.exe 与 Suzerain_Data），例如：`n    powershell -ExecutionPolicy Bypass -File install.ps1 -GameDir `"D:\Games\Suzerain`""
}

$best = $script:candidates | Sort-Object -Property Score -Descending | Select-Object -First 1
$game = (Resolve-Path $best.Dir).Path
Info ("游戏目录：" + $game + "    [来源：" + $best.Source + "，签名评分 " + $best.Score + "]")
if ($best.Score -lt 4) { Warn "该目录缺少 GameAssembly.dll / UnityPlayer.dll 等特征文件，可能不是完整安装（仍继续尝试）" }

$pluginDir = Join-Path $game 'BepInEx\plugins'
$cfgDir    = Join-Path $game 'BepInEx\config'


if ($Uninstall) {
    $any = $false
    foreach ($pn in $PluginNames) { $p = Join-Path $pluginDir $pn; if (Test-Path $p) { Remove-Item $p -Force -ErrorAction SilentlyContinue; Info "已删除 $pn"; $any = $true } }
    if (-not $any) { Info "未安装（插件目录下未找到本包内的模组）" }
    Info "配置与日志保留（如不需要可手动删除 BepInEx\config\$CfgName）"
    Write-Host ""
    Write-Host "[完成] 卸载结束。"
    exit 0
}


$isBundle = Test-Path (Join-Path $scriptDir 'BepInEx\core')
Info ("安装模式：" + $(if ($isBundle) { "整合包（含 BepInEx 前置 + 三个模组）" } else { "插件包（复用已装前置）" }))
if (-not $isBundle -and -not (Test-Path (Join-Path $game 'BepInEx\core'))) {
    Fail "未检测到 BepInEx 6（缺少 BepInEx\core）。请先安装 BepInEx-Unity.IL2CPP 前置，或使用整合包版本。"
}
Info "BepInEx 前置：就绪"

$running = Get-Process -Name Suzerain -ErrorAction SilentlyContinue
if ($running) { Fail "检测到游戏正在运行（Suzerain.exe, PID=$($running.Id -join ',')）。请先关闭游戏再安装。" }




if ($isBundle) {
    $copied = 0
    $skipFiles = @('install.ps1', 'install.bat', '先读我.txt', 'README.md', 'SHA256SUMS.txt', 'SHA256SUMS-bundle.txt')
    foreach ($item in (Get-ChildItem $scriptDir -Force)) {
        if ($skipFiles -contains $item.Name) { continue }
        if (-not $item.PSIsContainer) {
            try { Copy-Item $item.FullName (Join-Path $game $item.Name) -Force -ErrorAction Stop; $copied++ }
            catch { Warn ("前置文件 " + $item.Name + " 复制失败：" + $_.Exception.Message) }
            continue
        }
        $srcTree = $item.FullName
        $dstTree = Join-Path $game $item.Name
        New-Item -ItemType Directory -Force -Path $dstTree | Out-Null   
        foreach ($dir in (Get-ChildItem $srcTree -Recurse -Directory -Force)) {
            $rel = $dir.FullName.Substring($srcTree.Length).TrimStart('\')
            New-Item -ItemType Directory -Force -Path (Join-Path $dstTree $rel) | Out-Null
        }
        foreach ($file in (Get-ChildItem $srcTree -Recurse -File -Force)) {
            $rel = $file.FullName.Substring($srcTree.Length).TrimStart('\')
            if ($rel -like 'config\*' -and (Test-Path (Join-Path $dstTree $rel))) { continue }
            try { Copy-Item $file.FullName (Join-Path $dstTree $rel) -Force -ErrorAction Stop; $copied++ }
            catch { Warn ($item.Name + "\" + $rel + " 复制失败：" + $_.Exception.Message) }
        }
    }
    Info ("前置已铺设：$copied 个文件（含 dotnet / BepInEx 全部目录；已有 config 保留）")
}

New-Item -ItemType Directory -Force -Path $pluginDir | Out-Null

$legacyDll = Join-Path $pluginDir 'RealPresident.dll'
if (Test-Path $legacyDll) {
    try { Remove-Item $legacyDll -Force -ErrorAction Stop; Info "已清除旧版 RealPresident.dll（升级到 Oikonomia）" }
    catch { Warn ("旧版 RealPresident.dll 删除失败（游戏可能正在运行）：" + $_.Exception.Message) }
}
$legacyCfg = Join-Path (Split-Path $pluginDir -Parent) 'config\com.dell.suzerain.realpresident.cfg'
if (Test-Path $legacyCfg) {
    try { Remove-Item $legacyCfg -Force -ErrorAction Stop; Info "已清除旧版配置（设置请在新 cfg 中确认）" } catch { }
}


$installed = 0
foreach ($pn in $PluginNames) {
    $cand = @((Join-Path $scriptDir $pn), (Join-Path (Split-Path -Parent $scriptDir) $pn))
    $srcDll = $cand | Where-Object { Test-Path $_ } | Select-Object -First 1
    if (-not $srcDll) {
        if (Test-Path (Join-Path $pluginDir $pn)) { Info ("已就位（随前置铺设）：" + $pn); $installed++; continue }
        Warn ("安装包内找不到 $pn，已跳过"); continue
    }
    try { Copy-Item $srcDll (Join-Path $pluginDir $pn) -Force -ErrorAction Stop; Info ("已安装：" + $pn); $installed++ }
    catch { Warn ("$pn 复制失败：" + $_.Exception.Message) }
}
if ($installed -eq 0) { Fail "没有任何插件被安装（请确认安装包完整）" }

Write-Host ""
Write-Host "[完成] 安装成功。"
Write-Host ""
Write-Host "  下一步："
Write-Host "   1) 启动游戏（可先在 BepInEx\LogOutput.log 里确认三个模组都已加载）"
Write-Host "   2) 本体（索德兰）：总统令不再受 3 个限制；每回合结束按经济状态/词条结算预算"
Write-Host "   3) HB 战争模组与本包同装，互不冲突"
Write-Host ""
Write-Host "  配置文件（首次启动游戏后生成，改动 1 秒内热生效）："
Write-Host ("   " + (Join-Path $cfgDir $CfgName))
Write-Host ""
Write-Host "  卸载：重复运行本脚本并加 -Uninstall"
