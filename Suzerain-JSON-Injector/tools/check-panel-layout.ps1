




param(
    [string]$Exe = 'C:\Users\dell\Desktop\Suzerain\Suzerain-JSON Injector\SuzerainGUI.exe',
    [string]$GroupMatch = '经济结算'
)
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Windows.Forms, System.Drawing

$asm = [System.Reflection.Assembly]::LoadFrom($Exe)
$formType = $asm.GetType('SuzerainTrainer.MainForm')
if (-not $formType) { Write-Host ('FAIL 未找到 MainForm 类型，可用类型：'); $asm.GetTypes() | ForEach-Object { '  ' + $_.FullName }; exit 1 }
Write-Host ('已加载: ' + $formType.FullName)

$form = [System.Activator]::CreateInstance($formType, @([string]$null))
Write-Host ('窗体创建成功: ' + $form.Text + '   客户区 ' + $form.ClientSize.Width + 'x' + $form.ClientSize.Height)

function Walk([System.Windows.Forms.Control]$c) {
    foreach ($x in $c.Controls) { $x; Walk $x }
}
$roots = New-Object System.Collections.Generic.List[System.Windows.Forms.Control]
$roots.Add($form)

$flags = [System.Reflection.BindingFlags]::Instance -bor [System.Reflection.BindingFlags]::NonPublic -bor [System.Reflection.BindingFlags]::Public
foreach ($fld in $formType.GetFields($flags)) {
    $v = $null
    try { $v = $fld.GetValue($form) } catch { }
    if ($v -is [System.Windows.Forms.Control]) { $roots.Add($v) }
}
Write-Host ("扫描根控件: " + $roots.Count + " 个（窗体 + 字段控件）")
$all = @()
foreach ($r in $roots) { $all += $r; $all += @(Walk $r) }
$seen = New-Object 'System.Collections.Generic.HashSet[int]'
$uniq = New-Object System.Collections.Generic.List[System.Windows.Forms.Control]
foreach ($c in $all) { if ($seen.Add($c.GetHashCode())) { $uniq.Add($c) } }
$all = $uniq
Write-Host ("控件总数（去重）: " + $all.Count)

$gb = $all | Where-Object { $_ -is [System.Windows.Forms.GroupBox] -and $_.Text -match $GroupMatch } | Select-Object -First 1
$client = $gb.ClientSize
Write-Host ('客户区: ' + $client.Width + 'x' + $client.Height)
Write-Host ''

$kids = @($gb.Controls)
Write-Host ('子控件数: ' + $kids.Count)
Write-Host ''
Write-Host '── 截断检查 ──'
$trunc = 0
foreach ($k in ($kids | Sort-Object { $_.Top }, { $_.Left })) {
    $txt = ''
    if ($k -is [System.Windows.Forms.Label]) { $txt = $k.Text }
    elseif ($k -is [System.Windows.Forms.CheckBox]) { $txt = $k.Text }
    elseif ($k -is [System.Windows.Forms.NumericUpDown]) { $txt = '(数值 ' + $k.Value + ')' }
    if (-not $txt) { continue }
    $pref = 0
    try { $pref = $k.PreferredWidth } catch { }
    $clipped = $false
    $isText = ($k -is [System.Windows.Forms.Label]) -or ($k -is [System.Windows.Forms.CheckBox]) -or ($k -is [System.Windows.Forms.Button])
    if (-not $k.AutoSize -and $isText -and $txt -and $k.Width -gt 0 -and $k -isnot [System.Windows.Forms.NumericUpDown]) {
        $needH = [System.Windows.Forms.TextRenderer]::MeasureText($txt, $k.Font, (New-Object System.Drawing.Size($k.Width, 10000)), [System.Windows.Forms.TextFormatFlags]::WordBreak).Height
        if ($needH -gt ($k.Height + 2)) { $clipped = $true; $trunc++ }
    }
    $flag = if ($clipped) { '✗ 截断' } else { 'ok' }
    Write-Host ('  {0,-34} x={1,-4} y={2,-4} w={3,-4} auto={4,-6} pref={5,-4} {6}' -f $txt, $k.Left, $k.Top, $k.Width, $k.AutoSize, $pref, $flag)
}

Write-Host ''
Write-Host '── 越界检查（容器客户区）──'
$over = 0
foreach ($k in $kids) {
    $right = $k.Left + $k.Width; $bottom = $k.Top + $k.Height
    if ($right -gt $client.Width -or $bottom -gt $client.Height) {
        Write-Host ('  ✗ {0} 越界：right={1} (限 {2})  bottom={3} (限 {4})' -f $k.GetType().Name, $right, $client.Width, $bottom, $client.Height)
        $over++
    }
}
if ($over -eq 0) { Write-Host '  全部在客户区内 ✓' }

Write-Host ''
Write-Host '── 重叠检查（控件两两相交）──'
$hit = 0
for ($i = 0; $i -lt $kids.Count; $i++) {
    for ($j = $i + 1; $j -lt $kids.Count; $j++) {
        $a = $kids[$i]; $b = $kids[$j]
        $ra = New-Object System.Drawing.Rectangle($a.Left, $a.Top, $a.Width, $a.Height)
        $rb = New-Object System.Drawing.Rectangle($b.Left, $b.Top, $b.Width, $b.Height)
        if ($ra.IntersectsWith($rb)) {
            $na = if ($a -is [System.Windows.Forms.Label]) { $a.Text } else { $a.GetType().Name }
            $nb = if ($b -is [System.Windows.Forms.Label]) { $b.Text } else { $b.GetType().Name }
            Write-Host ('  ✗ 「{0}」({1},{2},{3}x{4}) 与 「{5}」({6},{7},{8}x{9}) 重叠' -f $na, $a.Left, $a.Top, $a.Width, $a.Height, $nb, $b.Left, $b.Top, $b.Width, $b.Height)
            $hit++
        }
    }
}
if ($hit -eq 0) { Write-Host '  无重叠 ✓' }

Write-Host ''


$page = $gb.Parent
if ($page) {
    Write-Host ""
    Write-Host ("── 页面级检查（容器 " + $page.GetType().Name + " 客户区 " + $page.ClientSize.Width + "x" + $page.ClientSize.Height + "，子控件 " + $page.Controls.Count + " 个）──")
    $pOver = 0; $pTrunc = 0; $pHit = 0
    foreach ($k in $page.Controls) {
        $right = $k.Left + $k.Width; $bottom = $k.Top + $k.Height
        if (-not $page.AutoScroll -and ($right -gt $page.ClientSize.Width -or $bottom -gt $page.ClientSize.Height)) {
            Write-Host ("  ✗ 越界：{0} right={1}/{2} bottom={3}/{4}" -f $k.GetType().Name, $right, $page.ClientSize.Width, $bottom, $page.ClientSize.Height); $pOver++
        }
        if (-not $k.AutoSize) {
            $pw = 0; try { $pw = $k.PreferredWidth } catch { }
            $needH2 = [System.Windows.Forms.TextRenderer]::MeasureText([string]$k.Text, $k.Font, (New-Object System.Drawing.Size($k.Width, 10000)), [System.Windows.Forms.TextFormatFlags]::WordBreak).Height
            if ($needH2 -gt ($k.Height + 2)) {
                $tx = ''; if ($k.PSObject.Properties['Text']) { $tx = [string]$k.Text }
                if ($tx.Length -gt 60) { $tx = $tx.Substring(0, 60) + '…' }
                Write-Host ("  ✗ 截断：{0} w={1} pref={2}  文本：「{3}」" -f $k.GetType().Name, $k.Width, $pw, $tx)
                $pTrunc++
            }
        }
    }
    $arr = @($page.Controls)
    for ($i = 0; $i -lt $arr.Count; $i++) {
        for ($j = $i + 1; $j -lt $arr.Count; $j++) {
            $a = $arr[$i]; $b = $arr[$j]
            if ($a -is [System.Windows.Forms.GroupBox] -or $b -is [System.Windows.Forms.GroupBox]) { continue }
            $ra = New-Object System.Drawing.Rectangle($a.Left, $a.Top, $a.Width, $a.Height)
            $rb = New-Object System.Drawing.Rectangle($b.Left, $b.Top, $b.Width, $b.Height)
            if ($ra.IntersectsWith($rb)) {
                $na = if ($a -is [System.Windows.Forms.Label] -and $a.Text) { "Label:" + $a.Text.Substring(0, [Math]::Min(20, $a.Text.Length)) } elseif ($a -is [System.Windows.Forms.Button] -or $a -is [System.Windows.Forms.CheckBox]) { $a.Text } else { $a.GetType().Name }
                $nb = if ($b -is [System.Windows.Forms.Label] -and $b.Text) { "Label:" + $b.Text.Substring(0, [Math]::Min(20, $b.Text.Length)) } elseif ($b -is [System.Windows.Forms.Button] -or $b -is [System.Windows.Forms.CheckBox]) { $b.Text } else { $b.GetType().Name }
                Write-Host ("  ✗ 重叠：「{0}」与「{1}」" -f $na, $nb); $pHit++
            }
        }
    }
    if ($pOver -eq 0 -and $pTrunc -eq 0 -and $pHit -eq 0) { Write-Host "  页面级无越界/截断/重叠 ✓" }
    $over += $pOver; $trunc += $pTrunc; $hit += $pHit
}
$verdict = ($trunc -eq 0 -and $over -eq 0 -and $hit -eq 0)
Write-Host ('══ 结论：截断 {0} · 越界 {1} · 重叠 {2} → {3} ══' -f $trunc, $over, $hit, $(if ($verdict) { '布局通过 ✓' } else { '存在问题 ✗' }))
$form.Dispose()
exit $(if ($verdict) { 0 } else { 1 })
