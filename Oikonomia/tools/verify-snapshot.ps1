




















[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)][string]$Path,
    [Parameter(Mandatory = $true)][string]$OutFile
)

$ErrorActionPreference = 'Stop'

$root = [System.IO.Path]::GetFullPath($Path)
if (-not [System.IO.Directory]::Exists($root)) {
    throw "Snapshot path does not exist: $root"
}

$outFull = [System.IO.Path]::GetFullPath($OutFile)
$rootWithSep = $root.TrimEnd([System.IO.Path]::DirectorySeparatorChar) + [System.IO.Path]::DirectorySeparatorChar
if ($outFull.StartsWith($rootWithSep, [System.StringComparison]::OrdinalIgnoreCase)) {
    throw "OutFile must not be inside the snapshot tree: $outFull"
}

$rows = [System.Collections.Generic.List[object]]::new()
$files = [System.IO.Directory]::EnumerateFiles($root, '*', [System.IO.SearchOption]::AllDirectories)
foreach ($file in $files) {
    $info = [System.IO.FileInfo]::new($file)
    $relative = [System.IO.Path]::GetRelativePath($root, $file).Replace('\', '/')
    $hash = (Get-FileHash -LiteralPath $file -Algorithm SHA256).Hash.ToLowerInvariant()
    $rows.Add([pscustomobject]@{
        path      = $relative
        length    = $info.Length
        mtimeUtc  = $info.LastWriteTimeUtc.ToString('o')
        sha256    = $hash
    })
}

$sorted = @($rows | Sort-Object -Property path)
$tab = [char]9
$canonical = ($sorted | ForEach-Object {
    $_.path + $tab + $_.length + $tab + $_.mtimeUtc + $tab + $_.sha256
}) -join "`n"

$sha = [System.Security.Cryptography.SHA256]::Create()
try {
    $treeHash = [Convert]::ToHexString($sha.ComputeHash([System.Text.Encoding]::UTF8.GetBytes($canonical))).ToLowerInvariant()
} finally {
    $sha.Dispose()
}

$result = [pscustomobject]@{
    schemaVersion = 1
    root          = $root
    capturedAtUtc = [DateTime]::UtcNow.ToString('o')
    fileCount     = $sorted.Count
    totalBytes    = ($sorted | Measure-Object -Property length -Sum).Sum
    treeSha256    = $treeHash
    files         = $sorted
}

$outDir = [System.IO.Path]::GetDirectoryName($outFull)
if ($outDir) { [System.IO.Directory]::CreateDirectory($outDir) | Out-Null }
$json = $result | ConvertTo-Json -Depth 5
[System.IO.File]::WriteAllText($outFull, $json + "`n", [System.Text.UTF8Encoding]::new($false))
Write-Output "SNAPSHOT $($result.fileCount) files $($result.totalBytes) bytes sha256=$treeHash -> $outFull"
