






param(
    [Parameter(Mandatory=$true)][string]$Nodes,
    [Parameter(Mandatory=$true)][string]$OutDir,
    [int]$Samples = 12
)
$ErrorActionPreference = 'Stop'
if (-not (Test-Path $OutDir)) { New-Item -ItemType Directory -Path $OutDir | Out-Null }

function F([string]$line, [string]$name) {
    $m = [regex]::Match($line, '"' + $name + '":"((?:[^"\\]|\\.)*)"')
    if ($m.Success) { return $m.Groups[1].Value }
    return $null
}
function N([string]$s) {
    if ([string]::IsNullOrEmpty($s)) { return $null }
    $t = $s.ToLowerInvariant() -replace '[^a-z0-9\u4e00-\u9fff]+', ''
    if ($t.Length -eq 0) { return $null }
    return $t
}
function Cut([string]$s, [int]$n) {
    if ([string]::IsNullOrEmpty($s)) { return '' }
    if ($s.Length -le $n) { return $s }
    return $s.Substring(0, $n) + '...'
}

$index = New-Object 'System.Collections.Generic.Dictionary[string,string]'
$total = 0; $entries = 0; $entrySeq = 0; $entryJump = 0; $seqOkAll = 0
foreach ($line in [System.IO.File]::ReadLines($Nodes)) {
    if ($line.Length -lt 20) { continue }
    $total++
    $id = F $line 'id'; $t = F $line 'title'; $m = F $line 'menu'; $d = F $line 'dialogue'
    $sq = F $line 'sequence'; $jp = F $line 'jump'
    if ($sq) { $seqOkAll++ }
    $kt = N $t; if ($kt -and $kt.Length -ge 4 -and -not $index.ContainsKey($kt)) { $index[$kt] = $id }
    foreach ($f in @($d, $m)) { $k = N $f; if ($k -and $k.Length -ge 12 -and -not $index.ContainsKey($k)) { $index[$k] = $id } }
    $schema = $false
    if ($t -and ($t -match 'CustomFieldType|InputId|OutputId|Sequence|Description')) { $schema = $true }
    if ((($d) -or ($m)) -and -not $schema) {
        $entries++
        if ($sq) { $entrySeq++ }
        if ($jp) { $entryJump++ }
    }
}

$out = New-Object System.Collections.Generic.List[string]
$tEdges = 0; $tEntryEdges = 0; $tResolved = 0
$n = 0; $resolved = 0
foreach ($line in [System.IO.File]::ReadLines($Nodes)) {
    if ($line.Length -lt 20) { continue }
    $t = F $line 'title'; $m = F $line 'menu'; $d = F $line 'dialogue'; $jp = F $line 'jump'
    if (-not $jp) { continue }
    $schema2 = $false; if ($t -and ($t -match 'CustomFieldType|InputId|OutputId|Sequence|Description')) { $schema2 = $true }
    $tEdges++
    $isEntry = ((($d) -or ($m)) -and -not $schema2)
    if ($isEntry) { $tEntryEdges++ } else { continue }   # edge table only over tightened real entries
    if ($n -ge $Samples) { break }
    $id = F $line 'id'
    $to = $null
    $k = N $jp
    if ($k) { if ($index.ContainsKey($k)) { $to = $index[$k] } }
    $tResolved++; if ($to) { $resolved++ }
    $n++
    $out.Add('[' + $n + '] from=' + $id + '  to=' + ($(if ($to) { $to } else { '(unresolved)' })))
    $out.Add('    src-text: ' + (Cut $t 150) + (Cut $d 80))
    $out.Add('    jump    : ' + (Cut $jp 150))
}
$rate = 0.0; if ($entries -gt 0) { $rate = [math]::Round(100.0 * $entrySeq / $entries, 1) }
$lines = @()
$lines += "records-total=$total  records-with-sequence=$seqOkAll"
$lines += "FILTER: real entry = has Title OR Menu Text OR Dialogue Text"
$lines += "real-entries=$entries  entries-with-sequence=$entrySeq  entry-sequence-rate=$rate%"
$lines += "real-entries-with-jump=$entryJump"
$rate2 = 0.0; if ($tEntryEdges -gt 0) { $rate2 = [math]::Round(100.0 * $tResolved / $tEntryEdges, 1) }
$lines += "tightened: edges-all=$tEdges  edges-in-real-entries=$tEntryEdges  resolved=$tResolved  resolution-rate=$rate2%"
$lines += "sampled=$n  resolved-in-sample=$resolved"
$lines += "--- samples (manual review) ---"
$lines += $out
$lines | Set-Content -Encoding UTF8 (Join-Path $OutDir 'edge-samples.txt')
$lines | Select-Object -First 6
Write-Output '--- samples ---'
$out | ForEach-Object { Write-Output $_ }
