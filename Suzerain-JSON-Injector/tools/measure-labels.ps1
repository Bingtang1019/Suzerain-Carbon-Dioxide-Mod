

param([string]$Exe = 'C:\Users\dell\Desktop\Suzerain\Suzerain-JSON Injector\SuzerainGUI.exe')
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Windows.Forms, System.Drawing

$asm = [System.Reflection.Assembly]::LoadFrom($Exe)
$ft = $asm.GetType('SuzerainTrainer.MainForm')
$form = [System.Activator]::CreateInstance($ft, @([string]$null))
$flags = [System.Reflection.BindingFlags]::Instance -bor [System.Reflection.BindingFlags]::NonPublic -bor [System.Reflection.BindingFlags]::Public

function Walk($c) { foreach ($x in $c.Controls) { $x; Walk $x } }
$roots = @($form)
foreach ($f in $ft.GetFields($flags)) { try { $v = $f.GetValue($form); if ($v -is [System.Windows.Forms.Control]) { $roots += $v } } catch { } }
$seen = New-Object 'System.Collections.Generic.HashSet[int]'
$all = New-Object System.Collections.Generic.List[System.Windows.Forms.Control]
foreach ($r in $roots) { foreach ($c in (@($r) + @(Walk $r))) { if ($seen.Add($c.GetHashCode())) { $all.Add($c) } } }


$gb = $all | Where-Object { $_ -is [System.Windows.Forms.GroupBox] -and $_.Text -match '经济结算' } | Select-Object -First 1
$pages = @()
if ($gb) { $pages += $gb.Parent }

$hbPage = $all | Where-Object { $_ -is [System.Windows.Forms.Panel] } | Where-Object {
    $t = ($_.Controls | Where-Object { $_ -is [System.Windows.Forms.Label] } | ForEach-Object { $_.Text }) -join ' '
    $t -match 'HB 模组的状态文件'
} | Select-Object -First 1
if ($hbPage -and $pages -notcontains $hbPage) { $pages += $hbPage }

foreach ($pg in $pages) {
    Write-Host ''
    Write-Host ('══ 页面：' + $pg.GetType().Name + '（' + $pg.Controls.Count + ' 个直接子控件）══')
    foreach ($k in ($pg.Controls | Sort-Object Top)) {
        if (-not ($k -is [System.Windows.Forms.Label])) { continue }
        $txt = [string]$k.Text
        if (-not $txt) { continue }
        $need = [System.Windows.Forms.TextRenderer]::MeasureText($txt, $k.Font, (New-Object System.Drawing.Size([Math]::Max(10, $k.Width), 10000)), [System.Windows.Forms.TextFormatFlags]::WordBreak)
        $fits = ($k.AutoSize) -or ($need.Height -le ($k.Height + 2))
        Write-Host ("  [{0}] y={1,-4} w={2,-4} h={3,-4} 实需高={4,-4} 字数={5,-4}" -f $(if ($fits) { 'OK ' } else { '裁切' }), $k.Top, $k.Width, $k.Height, $need.Height, $txt.Length)
        if (-not $fits) {
            Write-Host ('        文本：' + $txt)
            Write-Host ('        → 需要 {0} px 高，实际只有 {1} px（差 {2} px ≈ {3} 行）' -f $need.Height, $k.Height, ($need.Height - $k.Height), [Math]::Ceiling(($need.Height - $k.Height) / 19.0))
        }
    }
}
$form.Dispose()
