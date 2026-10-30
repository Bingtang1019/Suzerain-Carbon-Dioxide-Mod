




param(
    [Parameter(Mandatory = $true)][string]$Dll,
    [string]$GameDir = 'D:\Program Files (x86)\Steam\steamapps\common\Suzerain',
    [int]$WaitSeconds = 0,
    [switch]$NoLaunch
)
$ErrorActionPreference = 'Stop'
$dll = (Resolve-Path -LiteralPath $Dll).Path
if (-not (Test-Path -LiteralPath $dll)) { throw "DLL 不存在：$Dll" }

& (Join-Path $PSScriptRoot 'check-suzerain.ps1') -GameDir $GameDir -WaitSeconds $WaitSeconds
if ($LASTEXITCODE -ne 0) {
    throw "Suzerain 仍在运行，已中止部署；关闭游戏后重试，或加 -WaitSeconds 30。"
}

$dst = Join-Path $GameDir 'BepInEx\plugins\Oikonomia.dll'
$backupDir = Join-Path (Split-Path (Split-Path $PSScriptRoot -Parent) -Parent) '_verify\backups\deploy-auto'
New-Item -ItemType Directory -Path $backupDir -Force | Out-Null
if (Test-Path -LiteralPath $dst) {
    $stamp = Get-Date -Format 'yyyyMMdd-HHmmss'
    Copy-Item -LiteralPath $dst -Destination (Join-Path $backupDir ("Oikonomia-$stamp.dll")) -Force
}
Copy-Item -LiteralPath $dll -Destination $dst -Force
$hash = (Get-FileHash -LiteralPath $dst -Algorithm SHA256).Hash
$ver = (Get-Item -LiteralPath $dst).VersionInfo.FileVersion
Write-Host "[deploy] $dll -> $dst"
Write-Host "[deploy] size=$((Get-Item -LiteralPath $dst).Length) sha256=$hash version=$ver"

if (-not $NoLaunch) {
    $exe = Join-Path $GameDir 'Suzerain.exe'
    $p = Start-Process -FilePath $exe -WorkingDirectory $GameDir -PassThru
    Write-Host "[deploy] 已启动 Suzerain pid=$($p.Id)"
}
