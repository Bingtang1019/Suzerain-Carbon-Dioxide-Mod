







param(
    [string]$File = 'D:\Program Files (x86)\Steam\steamapps\common\Suzerain\GameAssembly.dll',
    [string[]]$Needles = @(
        'GetTooltipModifierTextString',
        'statTooltipModifierText',
        'hideModifier',
        'ModifiersTotalVariable',
        'no modifiers',
        'GetModifierText'
    ),
    [int]$Context = 96,
    [int]$ChunkMB = 8
)

$ErrorActionPreference = 'Stop'
if (-not (Test-Path $File)) { throw "找不到文件: $File" }

function Find-Bytes([byte[]]$hay, [byte[]]$needle) {
    $res = New-Object System.Collections.Generic.List[int]
    $n = $needle.Length
    $last = $hay.Length - $n
    for ($i = 0; $i -le $last; $i++) {
        if ($hay[$i] -ne $needle[0]) { continue }
        $ok = $true
        for ($j = 1; $j -lt $n; $j++) {
            if ($hay[$i + $j] -ne $needle[$j]) { $ok = $false; break }
        }
        if ($ok) { $res.Add($i) }
    }
    return $res
}

Write-Host ("文件: " + $File)
Write-Host ("大小: " + (Get-Item $File).Length + " 字节")

$fs = [System.IO.File]::Open($File, [System.IO.FileMode]::Open, [System.IO.FileAccess]::Read, [System.IO.FileShare]::ReadWrite)
try {
    $chunk = $ChunkMB * 1MB
    $buf = New-Object byte[] ($chunk + 4096)
    $carry = 0
    $base = 0L
    $found = @{}
    foreach ($nd in $Needles) { $found[$nd] = New-Object System.Collections.Generic.List[object] }

    while ($true) {
        $read = $fs.Read($buf, $carry, $chunk)
        if ($read -le 0) { break }
        $total = $carry + $read
        $seg = New-Object byte[] $total
        [Array]::Copy($buf, 0, $seg, 0, $total)

        foreach ($nd in $Needles) {
            foreach ($enc in @('utf8', 'utf16')) {
                $nb = if ($enc -eq 'utf8') { [System.Text.Encoding]::UTF8.GetBytes($nd) }
                      else { [System.Text.Encoding]::Unicode.GetBytes($nd) }
                foreach ($off in (Find-Bytes $seg $nb)) {
                    $abs = $base + $off
                    $s = [Math]::Max(0, $off - $Context)
                    $e = [Math]::Min($total - 1, $off + $nb.Length + $Context)
                    $ctx = [System.Text.Encoding]::UTF8.GetString($seg, $s, ($e - $s + 1))
                    $ctx = ($ctx -replace '[^\x20-\x7E]', '.')
                    $found[$nd].Add([pscustomobject]@{ Enc = $enc; Off = $abs; Ctx = $ctx })
                }
            }
        }

        
        $carry = [Math]::Min(4096, $total)
        [Array]::Copy($seg, ($total - $carry), $buf, 0, $carry)
        $base += ($total - $carry)
    }
}
finally { $fs.Dispose() }

Write-Host ""
foreach ($nd in $Needles) {
    $list = $found[$nd]
    Write-Host ("=== [" + $nd + "] 命中 " + $list.Count + " 处 ===")
    $shown = 0
    foreach ($h in $list) {
        if ($shown -ge 6) { Write-Host ("    … 其余 " + ($list.Count - $shown) + " 处省略"); break }
        Write-Host ("   " + $h.Enc + " @0x" + $h.Off.ToString('X') + "  …" + $h.Ctx + "…")
        $shown++
    }
    Write-Host ""
}
Write-Host '=== 完成（只读）==='
