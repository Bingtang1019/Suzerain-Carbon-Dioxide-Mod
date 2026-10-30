

























param(
    [switch]$Quiet
)

$ErrorActionPreference = 'Stop'

$projRoot = Split-Path $PSScriptRoot -Parent
$testsDir = Join-Path $projRoot 'tests'


$gate = Join-Path $PSScriptRoot 'check-version-consistency.ps1'
if (-not (Test-Path -PathType Leaf $gate)) { Write-Host '[仪器坏] 找不到版本门禁脚本 ⇒ exit 2'; exit 2 }




function Assert-GateParses {
    param([string]$Path)
    if (-not (Test-Path -PathType Leaf $Path)) { return 2 }
    $errs = $null
    [void][System.Management.Automation.Language.Parser]::ParseFile($Path, [ref]$null, [ref]$errs)
    if ($errs -and $errs.Count -gt 0) { return 2 }
    return 0
}
$gateParseCode = Assert-GateParses -Path $gate
if ($gateParseCode -ne 0) {
    Write-Host ('[仪器坏] Assert-GateParses 返回 ' + $gateParseCode + ' ⇒ 门禁脚本语法坏 ⇒ exit ' + $gateParseCode)
    exit $gateParseCode
}
if (-not $Quiet) { Write-Host '[接入] 版本门禁脚本语法 OK（★ 由本链外部解析 ✗）' }


$chainBadOk = $false
$chainBadNote = ''
try {
    $badDir = Join-Path $env:TEMP ('oi-gatebad-' + (Get-Date -Format 'HHmmssfff'))
    New-Item -ItemType Directory -Force -Path $badDir | Out-Null
    $badCopy = Join-Path $badDir 'check-version-consistency.ps1'
    $gateText = [System.IO.File]::ReadAllText($gate, [System.Text.Encoding]::UTF8)
    [System.IO.File]::WriteAllText($badCopy, ($gateText.Replace('$ErrorActionPreference = ''Stop''', '$ErrorActionPreference = ''Stop'' (')), (New-Object System.Text.UTF8Encoding($true)))
    $assertCode = Assert-GateParses -Path $badCopy
    $chainBadOk = ($assertCode -eq 2)
    $chainBadNote = if ($chainBadOk) { 'expect=2 got=2 (Assert-GateParses return value, not a hardcoded string)' } else { ('expect=2 got=' + $assertCode + ' -- chain would NOT stop on a broken gate') }
    Remove-Item -Recurse -Force $badDir -ErrorAction SilentlyContinue
} catch { $chainBadOk = $false; $chainBadNote = ('harness error: ' + $_.Exception.Message) }
$tag = if ($chainBadOk) { 'OK' } else { 'BAD' }
Write-Host ('[' + $tag + '] chain-parse-rejects-bad ' + $chainBadNote)
if (-not $chainBadOk) { Write-Host '[仪器坏] 链未能拒绝坏副本 ⇒ exit 2'; exit 2 }


$gateDll = Join-Path $projRoot 'src\Oikonomia\bin\Release\Oikonomia.dll'

function Invoke-Gate {
    param([string]$GatePath, [string]$PluginCs, [string]$Csproj, [string]$Dll, [string]$GeneratedCs)
    & powershell -NoProfile -ExecutionPolicy Bypass -File $GatePath -PluginCs $PluginCs -Csproj $Csproj -Dll $Dll -GeneratedCs $GeneratedCs | Out-Null
    $code = $LASTEXITCODE
    if ($code -eq 0) { return 0 }
    if ($code -eq 1) { return 1 }
    return 2
}
& powershell -NoProfile -ExecutionPolicy Bypass -File $gate -PluginCs (Join-Path $projRoot 'src\Oikonomia\Plugin.cs') -Csproj (Join-Path $projRoot 'src\Oikonomia\Oikonomia.csproj') -Dll $gateDll -GeneratedCs (Join-Path $projRoot 'src\Oikonomia\obj\Release\VersionInfo.g.cs')
$gateCode = $LASTEXITCODE
if ($gateCode -ne 0) {
    if ($gateCode -eq 2) { Write-Host '[仪器坏] 版本门禁自身报告仪器坏 ⇒ 本链同样退 2（★ 不与"真违规"同码 ✗）' -ForegroundColor Red; exit 2 }
    if ($gateCode -eq 3) { Write-Host '[环境错] 版本门禁报告输入/环境问题 ⇒ 本链退 2（★ 绝不退 1 ✗ 免得与违规混淆 ✓）' -ForegroundColor Red; exit 2 }
    Write-Host ('[测试失败] 版本一致性门禁未通过（真违规）⇒ exit=' + $gateCode + '（★ 若原因含 G2 ⇒ 正是预期的"该红时红" ✓）') -ForegroundColor Red
    exit $gateCode
}
if (-not $Quiet) { Write-Host '[接入] 版本一致性门禁通过 ✓' }



$mapProbe = Join-Path $projRoot 'tests\DecreeLimitTest\DecreeLimitTest.csproj'
if (Test-Path -PathType Leaf $mapProbe) {
    $mapCode = Invoke-Gate -GatePath $gate -PluginCs (Join-Path $projRoot 'src\Oikonomia\Plugin.cs') -Csproj (Join-Path $projRoot 'src\Oikonomia\Oikonomia.csproj') -Dll $mapProbe -GeneratedCs (Join-Path $projRoot 'src\Oikonomia\obj\Release\VersionInfo.g.cs')
    $mapOk = ($mapCode -eq 2)
    $mapTag = if ($mapOk) { 'OK' } else { 'BAD' }
    Write-Host ('[' + $mapTag + '] chain-gate-env-maps-to-2 expect=2 got=' + $mapCode + ' (non-PE artifact ⇒ gate 3 ⇒ chain 2)')
    if (-not $mapOk) { Write-Host '[仪器坏] 3→2 映射失效 ⇒ exit 2'; exit 2 }
} else {
    Write-Host '[仪器坏] 对照组所需的非 PE 文件不存在 ⇒ exit 2'; exit 2
}


$dotnet = 'C:\Users\dell\.dotnet\dotnet.exe'
if (-not (Test-Path $dotnet)) { $dotnet = 'dotnet' }
$env:DOTNET_ROOT = Split-Path $dotnet -Parent
if ($env:DOTNET_ROOT -eq '.') { $env:DOTNET_ROOT = 'C:\Users\dell\.dotnet' }

function Step($m) { if (-not $Quiet) { Write-Host ("== " + $m) } }
function Info($m) { if (-not $Quiet) { Write-Host ("  " + $m) } }


$suites = @(
    @{ Name = 'DecreeLimitTest';    Desc = '总统令动态上限纯计算核心' },
    @{ Name = 'EdgeSnapshotCompat'; Desc = '一次性边沿快照兼容分支' },
    @{ Name = 'InGameDetailFormatTest'; Desc = '游戏内预算加减明细格式化内核' },
    @{ Name = 'SettlePreviewTest';     Desc = '「下一回合预算变化」只读预演内核' },
    @{ Name = 'MemberWriteTest';       Desc = '属性/字段双探写入内核' },
    @{ Name = 'InteropContractTest';   Desc = 'task-19 原生成字段 vs interop 代理属性' },
    @{ Name = 'CompanyNodeMapTest';    Desc = '第一巨构 10 个选公司屏 → 项目映射' },
    @{ Name = 'T3CompanyPriorityTest'; Desc = 'T3 Gus/Tusk 差分优先级与场景白名单' },
    @{ Name = 'BudgetQuoteTest';      Desc = '2.11 预算引文 4×4 比例轴/选条/排版' }
)

$results = @()
foreach ($s in $suites) {
    $csproj = Join-Path $testsDir (Join-Path $s.Name ($s.Name + '.csproj'))
    Step ("运行 " + $s.Name + "（" + $s.Desc + "）")
    if (-not (Test-Path $csproj)) {
        $results += [pscustomobject]@{ Name = $s.Name; Pass = $false; Note = "找不到工程 $csproj" }
        continue
    }

    $out = @(& $dotnet run --project $csproj -c Release -v quiet --nologo 2>&1)
    $code = $LASTEXITCODE

    if (-not $Quiet) { $out | ForEach-Object { Write-Host ("  | " + $_) } }

    
    $failLines = @($out | Select-String -Pattern 'FAIL' -SimpleMatch)
    $pass = ($code -eq 0) -and ($failLines.Count -eq 0)

    
    $verdictLine = @($out | Where-Object { "$_" -match '结果：' }) | Select-Object -Last 1
    $note = if ($verdictLine) {
        ("结果：" + ("$verdictLine" -replace '^.*?结果：', '' -replace '\s*═+\s*$', '').Trim())
    } else { "exit=$code" }
    $results += [pscustomobject]@{
        Name = $s.Name
        Pass = $pass
        Note = $note
    }
}


Write-Host ""
Write-Host "═══ Oikonomia 测试汇总 ═══"
$bad = 0
foreach ($r in $results) {
    $mark = if ($r.Pass) { '  PASS  ' } else { '  FAIL  ' }
    if (-not $r.Pass) { $bad++ }
    Write-Host ($mark + $r.Name.PadRight(22) + $r.Note)
}

if ($bad -eq 0) {
    Write-Host ""
    Write-Host ("[测试通过] " + $results.Count + "/" + $results.Count + " 个套件全部通过")
    exit 0
} else {
    Write-Host ""
    Write-Host ("[测试失败] " + $bad + "/" + $results.Count + " 个套件未通过 —— 中止后续流程") -ForegroundColor Red
    exit 1
}
