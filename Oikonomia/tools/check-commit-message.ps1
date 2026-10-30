



param(
    [Parameter(Mandatory=$true)][string]$MessageFile
)
$ErrorActionPreference = 'Stop'
if (-not (Test-Path -PathType Leaf $MessageFile)) { Write-Host '[C4] message file not found'; exit 2 }
$msg = [System.IO.File]::ReadAllText($MessageFile, [System.Text.Encoding]::UTF8)
$stagedRaw = & git diff --cached --name-only 2>$null
if ($LASTEXITCODE -ne 0) { Write-Host '[C4] git diff --cached failed'; exit 2 }
$staged = @($stagedRaw | Where-Object { $_ -and $_.Trim() -ne '' } | ForEach-Object { ($_ -replace '\\','/').Trim() })

$codeExt = @('cs','ps1','csproj','cfg','json')
$tokens = [regex]::Matches($msg, '[A-Za-z0-9_./\\-]+\.(?:cs|ps1|csproj|cfg|json)') | ForEach-Object { ($_.Value -replace '\\','/') }
$bad = @()
foreach ($t in ($tokens | Select-Object -Unique)) {
    $base = Split-Path $t -Leaf
    $tracked = (& git ls-files -- "*$base" 2>$null)
    if (-not $tracked) { continue }                      
    $isStaged = $false
    foreach ($s in $staged) { if ((Split-Path $s -Leaf) -eq $base) { $isStaged = $true; break } }
    if (-not $isStaged) { $bad += $base }
}
if ($bad.Count -gt 0) {
    Write-Host ('[C4-REJECT] message names code file(s) not in the staged diff: ' + ($bad -join ', '))
    Write-Host ('[C4] staged: ' + (($staged | ForEach-Object { Split-Path $_ -Leaf }) -join ', '))
    exit 1
}
Write-Host '[C4-OK] message and staged diff agree'
exit 0