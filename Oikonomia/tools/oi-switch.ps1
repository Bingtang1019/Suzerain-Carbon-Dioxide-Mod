








param(
    [string]$Unlimited = 'false',
    [int]$MaxValue = 10000,
    [string]$ForceSignable = 'false',
    [string]$HideCounter = 'true',
    [string]$HideSubtitle = 'true',
    [string]$Verbose = 'true'
)

function ConvertTo-Bool([string]$v) {
    switch -Regex ($v.Trim().ToLower()) {
        '^(true|1|yes|on)$'  { return $true }
        default              { return $false }
    }
}

$dir = Join-Path $env:LOCALAPPDATA 'Oikonomia'
New-Item -ItemType Directory -Force -Path $dir | Out-Null
$path = Join-Path $dir 'probe.json'

$json = @"
{
  "unlimited": $((ConvertTo-Bool $Unlimited).ToString().ToLower()),
  "maxValue": $MaxValue,
  "forceSignable": $((ConvertTo-Bool $ForceSignable).ToString().ToLower()),
  "hideCounter": $((ConvertTo-Bool $HideCounter).ToString().ToLower()),
  "hideSubtitle": $((ConvertTo-Bool $HideSubtitle).ToString().ToLower()),
  "fixSubtitle": false,
  "verbose": $((ConvertTo-Bool $Verbose).ToString().ToLower())
}
"@

[System.IO.File]::WriteAllText($path, $json, (New-Object System.Text.UTF8Encoding($false)))
Write-Host "已写入 $path :"
Get-Content $path