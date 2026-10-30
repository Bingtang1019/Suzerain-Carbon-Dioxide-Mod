











param(
    [Parameter(Mandatory=$true)][string]$Nodes,
    [Parameter(Mandatory=$true)][string]$OutDir
)
$ErrorActionPreference = 'Stop'
if (-not (Test-Path $OutDir)) { New-Item -ItemType Directory -Path $OutDir | Out-Null }

function Field([string]$line, [string]$name) {
    $m = [regex]::Match($line, '"' + $name + '":"((?:[^"\\]|\\.)*)"')
    if ($m.Success) { return $m.Groups[1].Value }
    return $null
}
function Norm([string]$s) {
    if ([string]::IsNullOrEmpty($s)) { return $null }
    $t = $s.ToLowerInvariant()
    $t = $t -replace '[^a-z0-9\u4e00-\u9fff]+', ''
    if ($t.Length -eq 0) { return $null }
    return $t
}

$index = New-Object 'System.Collections.Generic.Dictionary[string,string]'
$total = 0
foreach ($line in [System.IO.File]::ReadLines($Nodes)) {
    if ($line.Length -lt 20) { continue }
    $total++
    $id = Field $line 'id'
    if (-not $id) { continue }
    foreach ($f in @('dialogue', 'menu')) {
        $v = Field $line $f
        $k = Norm $v
        if ($k -and $k.Length -ge 12) { if (-not $index.ContainsKey($k)) { $index[$k] = $id } }
    }
}
Write-Output ("[nodes] " + $total + "  [label-keys] " + $index.Count)

$sw = New-Object System.IO.StreamWriter((Join-Path $OutDir 'edges.jsonl'), $false, (New-Object System.Text.UTF8Encoding($false)))
$rxJump = [regex]'Jump to:\s*\[?([^\]\r\n]{1,200})'
$rxCond = [regex]'(Variable\s*\[|==\s*(true|false|\d)|!\(|&&|\band\b|\bor\b)'
$nEdge = 0; $nResolved = 0; $a = 0; $b = 0; $c = 0; $noSeq = 0
$unresolved = New-Object System.Collections.Generic.List[string]
foreach ($line in [System.IO.File]::ReadLines($Nodes)) {
    if ($line.Length -lt 20) { continue }
    $id = Field $line 'id'
    $seq = Field $line 'sequence'
    $title = Field $line 'title'
    $hasCond = ($title -and $rxCond.IsMatch($title))
    $jump = $null
    if ($seq) { $m = $rxJump.Match($seq); if ($m.Success) { $jump = $m.Groups[1].Value.Trim() } }
    if ($jump) {
        $nEdge++
        $to = $null
        $k = Norm $jump
        if ($k) {
            if ($index.ContainsKey($k)) { $to = $index[$k] }
            else {
                foreach ($kv in $index.GetEnumerator()) {
                    if ($kv.Key.Length -ge 16 -and ($kv.Key.StartsWith($k.Substring(0, [Math]::Min(16, $k.Length))) -or $k.StartsWith($kv.Key.Substring(0, [Math]::Min(16, $kv.Key.Length))))) { $to = $kv.Value; break }
                }
            }
        }
        if ($to) { $nResolved++ } elseif ($unresolved.Count -lt 25) { $unresolved.Add($jump) }
        $o = [ordered]@{ from = $id; to = $to; kind = 'jump'; resolved = [bool]$to; jump = $jump }
        $sw.WriteLine(($o | ConvertTo-Json -Compress))
    }
    if ($hasCond) { $a++ } elseif ($jump) { $b++ } else { $c++ }
    if (-not $seq) { $noSeq++ }
}
$sw.Flush(); $sw.Close()

$rate = 0.0
if ($nEdge -gt 0) { $rate = [math]::Round(100.0 * $nResolved / $nEdge, 1) }
$lines = @()
$lines += "nodes=$total  label-keys=" + $index.Count
$lines += "edges=$nEdge  resolved=$nResolved  resolution-rate=$rate%"
$lines += "class-a(has-cond)=$a  class-b(edge-no-cond)=$b  class-c(no-edge-no-cond)=$c"
$lines += "records-without-sequence=$noSeq"
$lines += "--- unresolved sample (max 25) ---"
$lines += $unresolved
$lines | Set-Content -Encoding UTF8 (Join-Path $OutDir 'stats.txt')
$lines | Select-Object -First 6
