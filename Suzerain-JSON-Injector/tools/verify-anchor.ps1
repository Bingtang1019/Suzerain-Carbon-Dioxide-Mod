
param([string]$Exe = 'C:\Users\dell\Desktop\Suzerain\Suzerain-JSON Injector\SuzerainGUI.exe')
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Windows.Forms, System.Drawing

$asm = [System.Reflection.Assembly]::LoadFrom($Exe)
$ft = $asm.GetType('SuzerainTrainer.MainForm')
$form = [System.Activator]::CreateInstance($ft, @([string]$null))
$fl = [System.Reflection.BindingFlags]::Instance -bor [System.Reflection.BindingFlags]::NonPublic -bor [System.Reflection.BindingFlags]::Public
$form.Show(); [System.Windows.Forms.Application]::DoEvents(); Start-Sleep -Milliseconds 700; [System.Windows.Forms.Application]::DoEvents()

$tree = $null
foreach ($f in $ft.GetFields($fl)) { try { $v = $f.GetValue($form); if ($v -is [System.Windows.Forms.TreeView]) { $tree = $v; break } } catch { } }
if ($tree) {
    $oi = $tree.Nodes | Where-Object { $_.Text -match 'Oikonomia|安济' } | Select-Object -First 1
    if ($oi -and $oi.Nodes.Count -gt 0) {
        $tree.SelectedNode = $oi.Nodes[0]
        [System.Windows.Forms.Application]::DoEvents(); Start-Sleep -Milliseconds 600; [System.Windows.Forms.Application]::DoEvents()
        Write-Host ('已选中: ' + $oi.Nodes[0].Text)
    }
}

function Walk($c) { foreach ($x in $c.Controls) { $x; Walk $x } }
$seen = New-Object 'System.Collections.Generic.HashSet[int]'
$all = New-Object System.Collections.Generic.List[System.Windows.Forms.Control]
foreach ($c in (@($form) + @(Walk $form))) { if ($seen.Add($c.GetHashCode())) { $all.Add($c) } }

$gb = $all | Where-Object { $_ -is [System.Windows.Forms.GroupBox] -and $_.Text -match '经济结算' } | Select-Object -First 1
if (-not $gb) { Write-Host 'FAIL 未找到经济结算分组（页面未切换？）'; $form.Close(); exit 1 }
$page = $gb.Parent
$vw = $page.ClientSize.Width
Write-Host ('页面容器: {0}   客户区 {1}x{2}   AutoScroll={3}' -f $page.GetType().Name, $vw, $page.ClientSize.Height, $page.AutoScroll)
Write-Host ('可见视口宽: {0} px' -f $vw)
Write-Host ''
Write-Host '── 页面直接子控件 ──'
$over = 0; $cut = 0
foreach ($k in ($page.Controls | Sort-Object Top)) {
    $tx = ''; if ($k.PSObject.Properties['Text']) { $tx = [string]$k.Text; if ($tx.Length -gt 34) { $tx = $tx.Substring(0, 34) + '…' } }
    $right = $k.Left + $k.Width
    $fit = if ($right -le $vw) { '视口内 ✓' } else { $over++; ('框超出 ' + ($right - $vw) + 'px(信息)') }
    $txtFit = ''
    if ($tx -and ($k -is [System.Windows.Forms.Label] -or $k -is [System.Windows.Forms.CheckBox])) {
        try {
            $need = [System.Windows.Forms.TextRenderer]::MeasureText([string]$k.Text, $k.Font, (New-Object System.Drawing.Size([Math]::Max(10, [Math]::Min($k.Width, $vw - $k.Left)), 10000)), [System.Windows.Forms.TextFormatFlags]::WordBreak).Height
            $txtFit = if ($k.AutoSize) { '文本自适应' } elseif ($need -le ($k.Height + 2)) { '文本完整 ✓' } else { $cut++; ('文本裁切 ✗ 需' + $need + 'px') }
        } catch { }
    }
    Write-Host ('  {0,-14} y={1,-4} w={2,-4} h={3,-4} 右={4,-4} {5,-12} {6,-18} 「{7}」' -f $k.GetType().Name, $k.Top, $k.Width, $k.Height, $right, $fit, $txtFit, $tx)
}
Write-Host ''
$hint = $page.Controls | Where-Object { $_ -is [System.Windows.Forms.Label] -and $_.Top -gt 700 -and $_.Text -match '说明' } | Select-Object -First 1
if ($hint) {
    $need = [System.Windows.Forms.TextRenderer]::MeasureText([string]$hint.Text, $hint.Font, (New-Object System.Drawing.Size($hint.Width, 10000)), [System.Windows.Forms.TextFormatFlags]::WordBreak)
    Write-Host ('底部说明: 宽 {0}px，{1} 字 → 实需高 {2}px / 控件高 {3}px → {4}' -f $hint.Width, ([string]$hint.Text).Length, $need.Height, $hint.Height, $(if ($need.Height -le ($hint.Height + 2)) { '完整显示 ✓' } else { '仍被裁 ✗' }))
}
Write-Host ''
Write-Host ('══ 汇总：文本裁切 {0} 项 · 控件框超出视口 {1} 项（信息项，仅影响可滚动宽度） → {2} ══' -f $cut, $over, $(if ($cut -eq 0) { '文本全部完整 ✓' } else { '存在文本裁切 ✗' }))
$form.Close(); $form.Dispose()
