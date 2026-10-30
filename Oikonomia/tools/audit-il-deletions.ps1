






























param(
    [Parameter(Mandatory = $true)][string]$Base,
    [Parameter(Mandatory = $true)][string]$Cur,
    [string]$Tag = 'il-deletion-audit',
    [int]$ShowLimit = 30
)

$ErrorActionPreference = 'Stop'

$projRoot = Split-Path $PSScriptRoot -Parent              
$repo     = Split-Path $projRoot -Parent                  
$ilDir    = Join-Path $repo '_analysis\il-equiv'

if (-not (Test-Path $Base)) { throw "找不到基线 DLL: $Base" }
if (-not (Test-Path $Cur))  { throw "找不到当前 DLL: $Cur" }
if (-not (Test-Path $ilDir)) { throw "找不到 IL 比对器目录: $ilDir" }

$dotnet = 'C:\Users\dell\.dotnet\dotnet.exe'
if (-not (Test-Path $dotnet)) { $dotnet = 'dotnet' }
$env:DOTNET_ROOT = Split-Path $dotnet -Parent
if ($env:DOTNET_ROOT -eq '.') { $env:DOTNET_ROOT = 'C:\Users\dell\.dotnet' }

$exe = Join-Path $ilDir 'bin\Release\net6.0\il-equiv.dll'
if (-not (Test-Path $exe)) {
    & $dotnet build (Join-Path $ilDir 'il-equiv.csproj') -c Release -v quiet --nologo | Out-Null
}
if (-not (Test-Path $exe)) { throw "比对器未构建成功: $exe" }

$work = Join-Path $env:TEMP ('oi-il-audit-' + $Tag)
New-Item -ItemType Directory -Force -Path $work | Out-Null
$txt  = Join-Path $work 'diff.txt'
$json = Join-Path $work 'diff.json'


& $dotnet @($exe, '--base', $Base, '--cur', $Cur, '--out', $txt, '--json', $json,
            '--normalize-synth', '--hunks', '100000', '--quiet') | Out-Null
if (-not (Test-Path $json)) { throw "比对器没有产出 JSON: $json" }

$doc = Get-Content -Raw -Encoding UTF8 $json | ConvertFrom-Json



$delPrefix = 'A(基线) - '
$insPrefix = 'B(当前) + '
$dumpDelPrefix = 'A  -  '
$dumpInsPrefix = 'B  +  '

$deletions  = New-Object System.Collections.ArrayList
$insertions = New-Object System.Collections.ArrayList
$truncated  = New-Object System.Collections.ArrayList

foreach ($d in $doc.diffs) {
    if (-not $d.hunks) { continue }
    foreach ($h in $d.hunks) {
        $line = [string]$h
        if ($line.Contains('处改动略')) { [void]$truncated.Add($d.member); continue }
        if ($line.StartsWith($delPrefix)) {
            [void]$deletions.Add([pscustomobject]@{ Member = $d.member; Instr = $line.Substring($delPrefix.Length) })
        } elseif ($line.StartsWith($insPrefix)) {
            [void]$insertions.Add([pscustomobject]@{ Member = $d.member; Instr = $line.Substring($insPrefix.Length) })
        }
    }
}

$fromHunks = $deletions.Count + $insertions.Count


function MemberKeysOf([string]$typeName, [bool]$isCur) {
    if ($isCur) { $side = $doc.cur } else { $side = $doc.base }
    $prop = $side.memberOrder.PSObject.Properties[$typeName]
    if ($null -eq $prop) { return @() }
    return @($prop.Value)
}

$dumped = New-Object System.Collections.ArrayList
foreach ($d in $doc.diffs) {
    $targets = New-Object System.Collections.ArrayList
    if ($d.kind -eq 'member-only-in-B' -or $d.kind -eq 'member-only-in-A') {
        [void]$targets.Add([pscustomobject]@{ Type = $d.type; Key = $d.member })
    } elseif ($d.kind -eq 'type-only-in-B' -or $d.kind -eq 'type-only-in-A') {
        $isCur = ($d.kind -eq 'type-only-in-B')
        foreach ($k in (MemberKeysOf $d.type $isCur)) {
            if ("$k".StartsWith('M:')) { [void]$targets.Add([pscustomobject]@{ Type = $d.type; Key = $k }) }
        }
    }
    foreach ($t in $targets) {
        $member = $t.Type + '::' + $t.Key
        $out = & $dotnet @($exe, '--base', $Base, '--cur', $Cur, '--normalize-synth',
                           '--dump', $member) 2>&1
        foreach ($raw in $out) {
            $line = [string]$raw
            if ($line.StartsWith($dumpDelPrefix)) {
                [void]$deletions.Add([pscustomobject]@{ Member = $member; Instr = $line.Substring($dumpDelPrefix.Length) })
            } elseif ($line.StartsWith($dumpInsPrefix)) {
                [void]$insertions.Add([pscustomobject]@{ Member = $member; Instr = $line.Substring($dumpInsPrefix.Length) })
            }
        }
        [void]$dumped.Add($member)
    }
}


function Semantic([string]$s) {
    $t = $s -replace '^(IL_)?_?\d+:\s*', ''
    $t = $t -replace 'IL_-?\d+', 'IL_#'
    return $t
}


$insCount = @{}
$insWhere = @{}
foreach ($x in $insertions) {
    $k = Semantic $x.Instr
    if ($insCount.ContainsKey($k)) { $insCount[$k] = $insCount[$k] + 1 } else { $insCount[$k] = 1 }
    if ($insWhere.ContainsKey($k)) {
        if (-not $insWhere[$k].Contains($x.Member)) { [void]$insWhere[$k].Add($x.Member) }
    } else {
        $l = New-Object System.Collections.ArrayList
        [void]$l.Add($x.Member)
        $insWhere[$k] = $l
    }
}

$netDeleted = New-Object System.Collections.ArrayList
foreach ($x in $deletions) {
    $k = Semantic $x.Instr
    if ($insCount.ContainsKey($k) -and $insCount[$k] -gt 0) {
        $insCount[$k] = $insCount[$k] - 1
    } else {
        [void]$netDeleted.Add([pscustomobject]@{ Member = $x.Member; Instr = $x.Instr; Key = $k })
    }
}




$writeCall = '^(call|callvirt)\b.*(RunInstant|Bridge::WriteState|SaveBaseline|PersistSnapshot)'
$mathOp    = '^(add|sub)(\.|$)'

$rawWrite = @($deletions  | Where-Object { (Semantic $_.Instr) -match $writeCall })
$netWrite = @($netDeleted | Where-Object { $_.Key -match $writeCall })
$rawMath  = @($deletions  | Where-Object { (Semantic $_.Instr) -match $mathOp })
$netMath  = @($netDeleted | Where-Object { $_.Key -match $mathOp })

Write-Host ""
Write-Host "═══ IL 删除指令机器审计（搬家中和 + 新成员体折叠）═══"
Write-Host ("基线 A : " + $Base)
Write-Host ("当前 B : " + $Cur)
Write-Host ("报告   : " + $txt)
Write-Host ""
Write-Host ("删除指令行数 : " + $deletions.Count + "（变更成员共 " + $fromHunks + " 行，另折叠仅一侧成员体）")
Write-Host ("插入指令行数 : " + $insertions.Count)
Write-Host ("净删除行数   : " + $netDeleted.Count + "（扣除「同一条在别处插入」= 搬家）")
Write-Host ("折叠的仅一侧成员 : " + $dumped.Count + " 个")
Write-Host ""
Write-Host ("① 写入/推进**调用**在原始删除项里的命中        : " + $rawWrite.Count + "（LCS 重排噪声，逐条举证配对插入）")
Write-Host ("   同一批调用在**净删除**里的命中（必须 0）   : " + $netWrite.Count)
Write-Host ("② add/sub 在原始删除项里的命中（搬家，允许非 0）: " + $rawMath.Count)
Write-Host ("   add/sub 在**净删除**里的命中（必须 0）     : " + $netMath.Count)
Write-Host ""

$bad = 0
if ($truncated.Count -gt 0) {
    Write-Host ("[FAIL] 报告被截断（" + $truncated.Count + " 处「其余 N 处改动略」）—— 审计不完整") -ForegroundColor Red
    $bad++
}

if ($rawWrite.Count -gt 0) {
    Write-Host "── 原始删除命中「写入/推进调用」的逐条举证（每条都必须配到插入）──"
    foreach ($x in $rawWrite) {
        $k = Semantic $x.Instr
        if ($insWhere.ContainsKey($k)) {
            Write-Host ("   [搬家] " + $x.Member + " :: " + $k)
            Write-Host ("          配对插入于：" + ($insWhere[$k] -join " / "))
        } else {
            Write-Host ("   [丢失?] " + $x.Member + " :: " + $k + "   ← 找不到配对插入") -ForegroundColor Red
            $bad++
        }
    }
    Write-Host ""
}

if ($netWrite.Count -gt 0) {
    Write-Host "[FAIL] 净删除里出现写入/推进调用：" -ForegroundColor Red
    foreach ($x in $netWrite) { Write-Host ("        " + $x.Member + " :: " + $x.Key) }
    $bad++
}
if ($netMath.Count -gt 0) {
    Write-Host "[FAIL] 净删除里出现结算算术（add/sub）—— 数学被真删了：" -ForegroundColor Red
    foreach ($x in ($netMath | Select-Object -First $ShowLimit)) { Write-Host ("        " + $x.Member + " :: " + $x.Key) }
    $bad++
}

if ($netDeleted.Count -gt 0) {
    Write-Host "── 净删除清单（人工复核用，最多列 $ShowLimit 条）──"
    foreach ($x in ($netDeleted | Select-Object -First $ShowLimit)) {
        Write-Host ("   " + $x.Member + " :: " + $x.Key)
    }
    if ($netDeleted.Count -gt $ShowLimit) { Write-Host ("   …（其余 " + ($netDeleted.Count - $ShowLimit) + " 条略）") }
    Write-Host ""
}

if ($bad -eq 0) {
    Write-Host "[审计通过] 写入/推进调用与 add/sub 在净删除里均 0 命中：写入指令没有丢，结算数学只是搬了家。"
    exit 0
} else {
    Write-Host ("[审计失败] " + $bad + " 项未通过") -ForegroundColor Red
    exit 1
}
