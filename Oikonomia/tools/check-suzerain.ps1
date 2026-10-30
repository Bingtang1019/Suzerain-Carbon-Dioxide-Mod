




param(
    [string]$GameDir = 'D:\Program Files (x86)\Steam\steamapps\common\Suzerain',
    [int]$WaitSeconds = 0,
    [switch]$Json,
    [switch]$Stop
)
$ErrorActionPreference = 'Stop'
$gameExe = Join-Path $GameDir 'Suzerain.exe'

function Get-SuzerainProcesses {
    $result = @()
    try {
        $cim = Get-CimInstance Win32_Process -Filter "Name='Suzerain.exe'" -ErrorAction Stop
        foreach ($c in $cim) {
            $path = $c.ExecutablePath
            
            if ($path -and -not [string]::Equals($path, $gameExe, [StringComparison]::OrdinalIgnoreCase)) { continue }
            $p = Get-Process -Id $c.ProcessId -ErrorAction SilentlyContinue
            $result += [pscustomobject]@{
                Pid = [int]$c.ProcessId
                Path = $path
                StartTime = $c.CreationDate
                Responding = if ($p) { $p.Responding } else { $null }
                MainWindowHandle = if ($p) { [int64]$p.MainWindowHandle } else { 0 }
                MainWindowTitle = if ($p) { $p.MainWindowTitle } else { '' }
            }
        }
    } catch {
        
        foreach ($p in @(Get-Process -Name Suzerain -ErrorAction SilentlyContinue)) {
            $result += [pscustomobject]@{
                Pid = [int]$p.Id
                Path = $null
                StartTime = $p.StartTime
                Responding = $p.Responding
                MainWindowHandle = [int64]$p.MainWindowHandle
                MainWindowTitle = $p.MainWindowTitle
            }
        }
    }
    return @($result)
}

function Show-Processes($items) {
    if ($items.Count -eq 0) { Write-Host '[check] Suzerain 未运行（可以部署）'; return }
    Write-Host "[check] Suzerain 仍在运行：$($items.Count) 个进程"
    foreach ($x in $items) {
        Write-Host ("  pid={0} responding={1} hwnd={2} title={3}" -f $x.Pid, $x.Responding, $x.MainWindowHandle, $x.MainWindowTitle)
        if ($x.Path) { Write-Host ("  path={0}" -f $x.Path) }
    }
}

$items = @(Get-SuzerainProcesses)
if ($Json) {
    [pscustomobject]@{ running = ($items.Count -gt 0); processes = $items } | ConvertTo-Json -Compress -Depth 4
} else {
    Show-Processes $items
}

if ($Stop -and $items.Count -gt 0) {
    foreach ($x in $items) {
        try {
            Stop-Process -Id $x.Pid -Force -ErrorAction Stop
            Write-Host "[check] 已停止 pid=$($x.Pid)"
        } catch {
            Write-Warning "[check] 无法停止 pid=$($x.Pid)：$($_.Exception.Message)"
        }
    }
    Start-Sleep -Milliseconds 500
    $items = @(Get-SuzerainProcesses)
    if (-not $Json) { Show-Processes $items }
}

if ($WaitSeconds -gt 0 -and $items.Count -gt 0) {
    $deadline = (Get-Date).AddSeconds($WaitSeconds)
    while ((Get-Date) -lt $deadline) {
        Start-Sleep -Milliseconds 500
        $items = @(Get-SuzerainProcesses)
        if ($items.Count -eq 0) { break }
    }
    if (-not $Json) { Show-Processes $items }
}

if ($items.Count -gt 0) { exit 1 }
exit 0
