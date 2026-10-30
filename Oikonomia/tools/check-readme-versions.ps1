



















param([Parameter(Mandatory = $true)][string]$Readme)
$ErrorActionPreference = 'Stop'
if (-not (Test-Path $Readme)) {
    Write-Host ("[说明版本检查] 找不到文件：" + $Readme) -ForegroundColor Red
    Write-Host "README_VER_CHECK: FAIL stale=<readme-missing> checked=0 skipped=0"; exit 1
}
$text = [System.IO.File]::ReadAllText($Readme, [System.Text.Encoding]::UTF8)


$comps = @(
    @{ Key = 'oikonomia';            Re = '(?i)oikonomia' },
    @{ Key = 'horrifyingbombardment'; Re = '(?i)horrifying\s*bombardment|(?i)\bHB\b' },
    @{ Key = 'warprobe';             Re = '(?i)war\s*probe' },
    @{ Key = 'suzeraingui';          Re = '(?i)suzeraingui|(?i)trainer|(?i)修改器' },
    @{ Key = 'bepinex';              Re = '(?i)bepinex' }
)
$declared = @{}
foreach ($c in $comps) {
    $mm = [regex]::Matches($text, ($c.Re + '[^\r\n]*?v?(\d+\.\d+\.\d+(?:-[0-9A-Za-z.]+)?)'))
    if ($mm.Count -gt 0) {
        
        foreach ($x in $mm) {
            if ($x.Value -match 'Loading') { continue }
            $declared[$c.Key] = $x.Groups[1].Value
        }
    }
}


$checked = 0; $skipped = 0; $stale = New-Object System.Collections.Generic.List[string]
foreach ($m in [regex]::Matches($text, 'Loading\s*\[([^\]\r\n]+)\]')) {
    $inner = $m.Groups[1].Value.Trim()
    $vm = [regex]::Match($inner, '(\d+\.\d+\.\d+(?:-[0-9A-Za-z.]+)?)\s*$')
    if (-not $vm.Success) { $skipped++; continue }          
    $ver = $vm.Groups[1].Value
    $name = $inner.Substring(0, $vm.Index).Trim()
    $hit = $null
    foreach ($c in $comps) { if ($name -match $c.Re) { $hit = $c.Key; break } }
    if (-not $hit -or -not $declared.ContainsKey($hit)) { $skipped++; continue }
    $checked++
    if ($declared[$hit] -ne $ver) {
        $stale.Add($name.Trim() + "=" + $ver + "(正文声明 " + $declared[$hit] + ")")
    }
}
if ($stale.Count -gt 0) {
    Write-Host ("[说明版本检查] ✗ 样例日志的版本与正文声明不符：" + ($stale -join '；')) -ForegroundColor Red
    Write-Host "  ⇒ 样例没跟着组件升级 ✗ —— 请更新 tools\先读我.template.txt 后重打。" -ForegroundColor Red
    Write-Host ("  文档：" + $Readme)
    Write-Host ("README_VER_CHECK: FAIL stale=" + ($stale -join ';') + " checked=" + $checked + " skipped=" + $skipped)
    exit 1
}
Write-Host ("[说明版本检查] ✓ 样例版本与正文声明一致（比对 " + $checked + " 条，跳过 " + $skipped + " 条）") -ForegroundColor Green
Write-Host ("README_VER_CHECK: PASS stale= checked=" + $checked + " skipped=" + $skipped)
exit 0