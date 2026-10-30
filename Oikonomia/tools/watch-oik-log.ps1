

param(
    [string]$Log = 'D:\Program Files (x86)\Steam\steamapps\common\Suzerain\BepInEx\LogOutput.log',
    [switch]$All
)
$ErrorActionPreference = 'Continue'
$pattern = if ($All) { '.' } else {
    '\[OI\]\[ui\]\[quote\]|ticker=|tick=|tick frame=|step frame=|anim=|variant=|明细写入回读|Exception|异常|quote'
}
Write-Output "[watch] start log=$Log pattern=$pattern"
$pos = 0L
$lastLength = -1L
while ($true) {
    try {
        if (Test-Path -LiteralPath $Log) {
            $len = (Get-Item -LiteralPath $Log).Length
            if ($len -lt $pos) { $pos = 0 } 
            if ($len -ne $lastLength) {
                $fs = [IO.File]::Open($Log, [IO.FileMode]::Open, [IO.FileAccess]::Read, [IO.FileShare]::ReadWrite)
                try {
                    if ($pos -eq 0 -and $len -gt 0) {
                        
                        $pos = [Math]::Max(0, $len - 4096)
                        [void]$fs.Seek($pos, [IO.SeekOrigin]::Begin)
                        $sr = New-Object IO.StreamReader($fs, [Text.Encoding]::UTF8, $true)
                        $tail = $sr.ReadToEnd()
                        $lines = $tail -split "`r?`n"
                        if ($lines.Count -gt 1) { $lines = $lines[1..($lines.Count - 1)] }
                        foreach ($line in $lines) { if ($line -match $pattern) { Write-Output $line } }
                        $pos = $fs.Position
                    } else {
                        [void]$fs.Seek($pos, [IO.SeekOrigin]::Begin)
                        $sr = New-Object IO.StreamReader($fs, [Text.Encoding]::UTF8, $true)
                        $chunk = $sr.ReadToEnd()
                        if ($chunk) {
                            foreach ($line in ($chunk -split "`r?`n")) {
                                if ($line -match $pattern) { Write-Output $line }
                            }
                        }
                        $pos = $fs.Position
                    }
                } finally { $fs.Dispose() }
                $lastLength = $len
                [Console]::Out.Flush()
            }
        }
    } catch {
        Write-Output "[watch] read error: $($_.Exception.Message)"
    }
    Start-Sleep -Milliseconds 250
}
