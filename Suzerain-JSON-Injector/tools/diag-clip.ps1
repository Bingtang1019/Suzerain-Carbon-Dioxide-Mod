












param(
    [string]$Exe = (Join-Path $PSScriptRoot '..\SuzerainGUI.exe'),
    [string]$Out = (Join-Path $PSScriptRoot '..\_out\snap\diag-clip.txt')
)
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Windows.Forms, System.Drawing

$asm = [System.Reflection.Assembly]::LoadFrom((Resolve-Path $Exe))
$BF = [System.Reflection.BindingFlags]
$progType = $asm.GetType('SuzerainTrainer.Program')
$progType.GetMethod('SetProcessDPIAware', ($BF::Static -bor $BF::NonPublic -bor $BF::Public)).Invoke($null, @()) | Out-Null
$formType = $asm.GetType('SuzerainTrainer.MainForm')
$form = [System.Activator]::CreateInstance($formType, @([string]$null))

$sb = New-Object System.Text.StringBuilder
function W([string]$s) { [void]$sb.AppendLine($s); Write-Host $s }

$scaleF = 1.0
try { $scaleF = [float]$formType.GetField('dpiScaleF', ($BF::Instance -bor $BF::NonPublic -bor $BF::Public)).GetValue($form) } catch { }
$contentHost = $formType.GetField('contentHost', ($BF::Instance -bor $BF::NonPublic -bor $BF::Public)).GetValue($form)
W ("DPI_SCALE=" + $scaleF.ToString('0.00') + "  Form.Font=" + $form.Font.Name + " " + $form.Font.SizeInPoints.ToString('0.0') + "pt (Height=" + $form.Font.Height + "px)  form client=" + $form.ClientSize.Width + "x" + $form.ClientSize.Height)
W ("contentHost=" + $contentHost.ClientSize.Width + "x" + $contentHost.ClientSize.Height + "  Font=" + $contentHost.Font.Name + " " + $contentHost.Font.SizeInPoints.ToString('0.0') + "pt")
W ''

function LineHeight([System.Drawing.Font]$f) {
    $r = [System.Windows.Forms.TextRenderer]::MeasureText('Chinese_Test', $f, (New-Object System.Drawing.Size(100000, 100000)),
        [System.Windows.Forms.TextFormatFlags]::SingleLine -bor [System.Windows.Forms.TextFormatFlags]::NoPadding)
    return $r.Height
}
function WrappedHeight([string]$t, [System.Drawing.Font]$f, [int]$w) {
    if ($w -lt 8) { $w = 8 }
    $r = [System.Windows.Forms.TextRenderer]::MeasureText($t, $f, (New-Object System.Drawing.Size($w, 10000)),
        [System.Windows.Forms.TextFormatFlags]::WordBreak -bor [System.Windows.Forms.TextFormatFlags]::Left -bor [System.Windows.Forms.TextFormatFlags]::Top)
    return $r.Height
}
function Clip([string]$t) { if ($t.Length -gt 46) { return $t.Substring(0, 46) + '~' } return $t }

$pages = [ordered]@{}
foreach ($name in @('pageHb', 'pageOi', 'pageBase', 'pageNoviceBase')) {
    try { $v = $formType.GetField($name, ($BF::Instance -bor $BF::NonPublic -bor $BF::Public)).GetValue($form); if ($v -is [System.Windows.Forms.Control]) { $pages[$name] = $v } } catch { }
}

$bad = 0
foreach ($kv in $pages.GetEnumerator()) {
    $page = $kv.Value
    
    $contentHost.Controls.Clear()
    $page.Dock = [System.Windows.Forms.DockStyle]::Fill
    $contentHost.Controls.Add($page)
    $form.PerformLayout(); $page.PerformLayout(); [System.Windows.Forms.Application]::DoEvents()
    
    if ($page -is [System.Windows.Forms.TabControl]) {
        $dr = $page.DisplayRectangle
        foreach ($tp in $page.TabPages) {
            $tp.Size = New-Object System.Drawing.Size($dr.Width, $dr.Height)
            $tp.PerformLayout()
        }
        [System.Windows.Forms.Application]::DoEvents()
    }

    W ('======== ' + $kv.Key + ' ' + $page.GetType().Name + '  client=' + $page.ClientSize.Width + 'x' + $page.ClientSize.Height +
       '  font=' + $page.Font.Name + ' ' + $page.Font.SizeInPoints.ToString('0.0') + 'pt  autoscroll=' + $page.AutoScroll + ' ========')

    $script:kids = @()
    function Collect([System.Windows.Forms.Control]$c) { foreach ($x in $c.Controls) { $script:kids += $x; Collect $x } }
    Collect $page

    $rows = @()
    foreach ($k in $script:kids) {
        $isText = ($k -is [System.Windows.Forms.Label]) -or ($k -is [System.Windows.Forms.CheckBox]) -or
                  ($k -is [System.Windows.Forms.Button]) -or ($k -is [System.Windows.Forms.GroupBox]) -or
                  ($k -is [System.Windows.Forms.RadioButton])
        if (-not $isText) { continue }
        $txt = [string]$k.Text
        if (-not $txt) { continue }
        $lh = LineHeight $k.Font
        $wh = WrappedHeight $txt $k.Font ($k.Width - 10)
        $flag = 'ok'
        if (-not $k.AutoSize -and $k.Height -lt $lh) { $flag = 'HALF-GLYPH' }
        elseif (-not $k.AutoSize -and $k.Height -lt $wh) { $flag = 'wrap-cut' }
        elseif ($k -is [System.Windows.Forms.CheckBox] -and -not $k.AutoSize) {
            $tw = ([System.Windows.Forms.TextRenderer]::MeasureText($txt, $k.Font)).Width + 24
            if ($tw -gt $k.Width) { $flag = 'hcut' }
        }
        if ($flag -eq 'ok') {
            $po = $k.Parent
            if ($po -and -not $po.AutoScroll -and -not ($po -is [System.Windows.Forms.TabPage])) {
                if ($k.Top + $k.Height -gt $po.ClientSize.Height + 1) { $flag = 'parent-cut' }
            }
        }
        $rows += [pscustomobject]@{ Flag = $flag; Kind = $k.GetType().Name; Auto = $k.AutoSize; Text = (Clip $txt)
            FontPt = $k.Font.SizeInPoints.ToString('0.0'); X = $k.Left; Y = $k.Top; W = $k.Width; H = $k.Height
            LineH = $lh; NeedH = $wh }
    }
    foreach ($r in ($rows | Where-Object { $_.Flag -ne 'ok' } | Sort-Object Y)) {
        W ("  [{0,-10}] {1,-11} auto={2,-5} font={3,4}pt y={4,4} x={5,4} w={6,4} h={7,3} line={8,3} need={9,3}  {10}" -f `
            $r.Flag, $r.Kind, $r.Auto, $r.FontPt, $r.Y, $r.X, $r.W, $r.H, $r.LineH, $r.NeedH, $r.Text)
    }
    $nBad = @($rows | Where-Object { $_.Flag -ne 'ok' }).Count
    $bad += $nBad
    W ('  -- ' + (@($rows).Count - $nBad) + ' ok / ' + $rows.Count + ' text controls, ' + $nBad + ' problem(s)')
    W ''
}

W ('TOTAL_PROBLEMS=' + $bad)
[System.IO.File]::WriteAllText($Out, $sb.ToString(), (New-Object System.Text.UTF8Encoding($false)))
Write-Host ('written: ' + $Out)
$form.Dispose()
