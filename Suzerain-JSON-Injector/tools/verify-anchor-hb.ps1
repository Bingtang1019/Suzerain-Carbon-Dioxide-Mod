
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
    $hb = $tree.Nodes | Where-Object { $_.Text -match 'HB|Horrifying' } | Select-Object -First 1
    if ($hb -and $hb.Nodes.Count -gt 0) {
        $tree.SelectedNode = $hb.Nodes[0]
        [System.Windows.Forms.Application]::DoEvents(); Start-Sleep -Milliseconds 600; [System.Windows.Forms.Application]::DoEvents()
        Write-Host ('已选中: ' + $hb.Nodes[0].Text)
    }
}

function Walk($c) { foreach ($x in $c.Controls) { $x; Walk $x } }
$seen = New-Object 'System.Collections.Generic.HashSet[int]'
$all = New-Object System.Collections.Generic.List[System.Windows.Forms.Control]
foreach ($c in (@($form) + @(Walk $form))) { if ($seen.Add($c.GetHashCode())) { $all.Add($c) } }


$hintLbl = $all | Where-Object { $_ -is [System.Windows.Forms.Label] -and $_.Text -match 'HB 模组的状态文件' } | Select-Object -First 1
if (-not $hintLbl) { Write-Host 'FAIL 未找到 HB 说明标签'; $form.Close(); exit 1 }
$page = $hintLbl.Parent
Write-Host ('HB 页容器: {0}   客户区 {1}x{2}   AutoScroll={3}' -f $page.GetType().Name, $page.ClientSize.Width, $page.ClientSize.Height, $page.AutoScroll)
$vw = $page.ClientSize.Width
Write-Host ('可见视口宽: {0} px' -f $vw)
Write-Host ''
Write-Host '── 页面直接子控件宽度 ──'
foreach ($k in ($page.Controls | Sort-Object Top)) {
    $tx = ''; if ($k.PSObject.Properties['Text']) { $tx = [string]$k.Text; if ($tx.Length -gt 34) { $tx = $tx.Substring(0, 34) + '…' } }
    $right = $k.Left + $k.Width
    $fit = if ($right -le $vw) { '视口内 ✓' } else { ('超出 ' + ($right - $vw) + 'px') }
    
    $needTxt = 0
    if ($tx -and ($k -is [System.Windows.Forms.Label] -or $k -is [System.Windows.Forms.CheckBox])) {
        try { $needTxt = [System.Windows.Forms.TextRenderer]::MeasureText([string]$k.Text, $k.Font, (New-Object System.Drawing.Size([Math]::Max(10, [Math]::Min($k.Width, $vw - $k.Left)), 10000)), [System.Windows.Forms.TextFormatFlags]::WordBreak).Height } catch { }
    }
    $txtFit = ''
    if ($needTxt -gt 0) { $txtFit = if ($k.AutoSize) { '文本自适应' } elseif ($needTxt -le ($k.Height + 2)) { '文本完整 ✓' } else { ('文本裁切 ✗ 需' + $needTxt + 'px') } }
    Write-Host ('  {0,-14} y={1,-4} w={2,-4} h={3,-4} 右={4,-4} {5,-12} {6,-18} 「{7}」' -f $k.GetType().Name, $k.Top, $k.Width, $k.Height, $right, $fit, $txtFit, $tx)
}
Write-Host ''
Write-Host '── HB 底部说明完整文本 ──'
$need = [System.Windows.Forms.TextRenderer]::MeasureText([string]$hintLbl.Text, $hintLbl.Font, (New-Object System.Drawing.Size($hintLbl.Width, 10000)), [System.Windows.Forms.TextFormatFlags]::WordBreak)
Write-Host ('  宽度 {0}px，{1} 字 → 实需高 {2}px，控件高 {3}px → {4}' -f $hintLbl.Width, ([string]$hintLbl.Text).Length, $need.Height, $hintLbl.Height, $(if ($need.Height -le ($hintLbl.Height + 2)) { '完整显示 ✓' } else { '仍被裁 ✗' }))
Write-Host ('  文本：' + [string]$hintLbl.Text)
Write-Host ''
$cutN = 0
foreach ($k in $page.Controls) {
    if (-not (($k -is [System.Windows.Forms.Label]) -or ($k -is [System.Windows.Forms.CheckBox]))) { continue }
    if ($k.AutoSize -or -not $k.Text) { continue }
    $visW = [Math]::Max(10, [Math]::Min($k.Width, $vw - $k.Left))
    $needN = [System.Windows.Forms.TextRenderer]::MeasureText([string]$k.Text, $k.Font, (New-Object System.Drawing.Size($visW, 10000)), [System.Windows.Forms.TextFormatFlags]::WordBreak).Height
    if ($needN -gt ($k.Height + 2)) { $cutN++; Write-Host ('  ✗ 文本裁切：{0} 需 {1}px / 高 {2}px' -f $k.GetType().Name, $needN, $k.Height) }
}
Write-Host ('══ 汇总：文本裁切 {0} 项 → {1} ══' -f $cutN, $(if ($cutN -eq 0) { 'HB 页文本全部完整 ✓' } else { '存在裁切 ✗' }))
$form.Close(); $form.Dispose()
