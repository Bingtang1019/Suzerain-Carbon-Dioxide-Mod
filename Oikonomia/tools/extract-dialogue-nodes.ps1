







param(
    [Parameter(Mandatory=$true)][string]$Bundle,
    [Parameter(Mandatory=$true)][string]$Out,
    [int]$MaxRecords = 250000
)
$ErrorActionPreference = 'Stop'
$DOT = [char]0x00B7


function Clean2([string]$s) {
    if ([string]::IsNullOrEmpty($s)) { return $null }
    $t = $s -replace '[\u0000-\u001F]', ' '
    $t = $t -replace '\u00B7', ' '
    $t = $t.Trim()
    if ($t.Length -eq 0) { return $null }
    if ($t.Length -gt 600) { $t = $t.Substring(0, 600) }
    return $t
}
$text = [System.IO.File]::ReadAllText($Bundle, [System.Text.Encoding]::UTF8)
$nMenu = 0; $nDial = 0; $nJump = 0; $nJumpReal = 0; $nSeqLabel = 0; $nSeqOk = 0; $nSeqBad = 0; $prevT = $null; $prevM = $null; $prevD = $null; $prevS = $null; $prevJ = $null
Write-Output ("[bundle chars] " + $text.Length)


$parts = [regex]::Split($text, '(?=Articy Id)')
Write-Output ("[parts] " + $parts.Count)

$rxId    = [regex]('Articy Id[^0-9A-Fa-f]{0,8}(0x[0-9A-Fa-f]{16})')
$rxTitle = [regex]('Title[^0-9A-Za-z\u4e00-\u9fff]{0,8}([^' + $DOT + ']{1,400})')
$rxMenu  = [regex]('Menu Text[^0-9A-Za-z\u4e00-\u9fff]{0,8}([^' + $DOT + ']{1,400})')
$rxDial  = [regex]('Dialogue Text[^0-9A-Za-z\u4e00-\u9fff]{0,8}([^' + $DOT + ']{1,900})')
$rxSeq   = [regex]('Sequence[^A-Za-z\u4e00-\u9fff]{0,12}([\s\S]{0,200}?)(?=Description|Actor|Conversant|Menu Text|Dialogue Text|CustomFieldType|\u00B7{2,}|\u0000)')   
$rxActor = [regex]('Actor[^0-9]{0,8}(\d{1,3})')
$rxConv  = [regex]('Conversant[^0-9]{0,8}(\d{1,3})')
$rxJump  = [regex]('Jump to:[^A-Za-z\u4e00-\u9fff]{0,4}([^\]]{1,240})')
$rxSeqLabel = [regex]('Sequence')


function SeqOk([string]$s) {
    if ([string]::IsNullOrEmpty($s)) { return $false }
    if ($s -match 'Continue\s*\(') { return $true }
    if ($s -match 'Jump to:') { return $true }
    if ($s -match 'Variable\s*\[') { return $true }
    if ($s -match '\bWait\b|\bSet\b|\bSendMessage\b') { return $true }
    return $false
}

$sw = New-Object System.IO.StreamWriter($Out, $false, (New-Object System.Text.UTF8Encoding($false)))
$nMenu = 0; $nDial = 0; $nJump = 0; $nJumpReal = 0; $nSeqLabel = 0; $nSeqOk = 0; $nSeqBad = 0; $prevT = $null; $prevM = $null; $prevD = $null; $prevS = $null; $prevJ = $null
$seen = New-Object 'System.Collections.Generic.HashSet[string]'
$n = 0
foreach ($p in $parts) {
    if ($p.Length -lt 40) { continue }
    $mid = $rxId.Match($p)
    if (-not $mid.Success) { continue }
    $id = $mid.Groups[1].Value
    if (-not $seen.Add($id)) { continue }
    
    $title = $prevT; $menu = $prevM; $dial = $prevD; $seq = $prevS; $jump = $prevJ
    if ($title -or $menu -or $dial -or $seq -or $jump) {
        $script:nJump++
        if ($jump) { $script:nJumpReal++ }
        if ($seq) { $script:nSeqOk++ }
        $actor = $null; $conv = $null
        $m = $rxActor.Match($p); if ($m.Success) { $actor = $m.Groups[1].Value }
        $m = $rxConv.Match($p);  if ($m.Success) { $conv  = $m.Groups[1].Value }
        $o = [ordered]@{ id = $id; actor = $actor; conversant = $conv; title = $title; menu = $menu; dialogue = $dial; sequence = $seq; jump = $jump; pairing = 'n+1' }
        $sw.WriteLine(($o | ConvertTo-Json -Compress))
        $n++
        if ($n -ge $MaxRecords) { Write-Output "[cap] hit MaxRecords"; break }
    }
    
    $prevT = $null; $prevM = $null; $prevD = $null; $prevS = $null; $prevJ = $null
    $m = $rxTitle.Match($p); if ($m.Success) { $prevT = Clean2 $m.Groups[1].Value }
    $m = $rxMenu.Match($p);  if ($m.Success) { $prevM = Clean2 $m.Groups[1].Value; if ($prevM) { $script:nMenu++ } }
    $m = $rxDial.Match($p);  if ($m.Success) { $prevD = Clean2 $m.Groups[1].Value; if ($prevD) { $script:nDial++ } }
    $m = $rxSeq.Match($p)
    if ($m.Success) {
        $script:nSeqLabel++
        $cand = Clean2 $m.Groups[1].Value
        if (SeqOk $cand) { $prevS = $cand } else { $script:nSeqBad++ }
    }
    $m = $rxJump.Match($p);  if ($m.Success) { $prevJ = Clean2 $m.Groups[1].Value }
}
$sw.Flush(); $sw.Close()
Write-Output ("[records] " + $n)
Write-Output ("[anchor] menu-records=" + $nMenu + "  dialogue-records=" + $nDial)
Write-Output ("[seq-label] " + $nSeqLabel + "  [seq-ok] " + $nSeqOk + "  [seq-bad] " + $nSeqBad + "  [seq-rate] " + [math]::Round(100.0*$nSeqOk/[Math]::Max(1,$nSeqLabel),1) + "%")
Write-Output ("[jump-lines] " + $nJump + "  [real-jump] " + $nJumpReal)
Write-Output ("[out] " + $Out)
