









param(
    [string]$Dll = 'D:\Program Files (x86)\Steam\steamapps\common\Suzerain\BepInEx\interop\Assembly-CSharp.dll',
    [string]$TypeFilter = 'TemplateHUDStat'
)

$ErrorActionPreference = 'Stop'
if (-not (Test-Path $Dll)) { throw "找不到 DLL: $Dll" }

$cecil = 'D:\Program Files (x86)\Steam\steamapps\common\Suzerain\BepInEx\core\Mono.Cecil.dll'
if (-not (Test-Path $cecil)) { throw "找不到 Mono.Cecil: $cecil" }
Add-Type -Path $cecil

$asm = [Mono.Cecil.AssemblyDefinition]::ReadAssembly($Dll)
Write-Host ("Assembly: " + $asm.Name.FullName)

function Get-AllTypes($types) {
    $out = @()
    foreach ($t in $types) {
        $out += $t
        if ($t.HasNestedTypes) { $out += Get-AllTypes $t.NestedTypes }
    }
    return $out
}
$all = Get-AllTypes $asm.MainModule.Types


$needles = @(
    'GetTooltipModifierTextString',
    'GetStatTooltipText',
    'GetModifierText',
    'set_statTooltipModifierText',
    'set_statTooltipText',
    'set_hideModifier',
    'get_hideModifier',
    'SetLocalizableTexts'
)

$rows = New-Object System.Collections.Generic.List[string]
foreach ($t in $all) {
    if (-not $t.HasMethods) { continue }
    foreach ($m in $t.Methods) {
        if (-not $m.HasBody) { continue }
        $hits = @()
        foreach ($ins in $m.Body.Instructions) {
            $op = $ins.Operand
            if ($op -eq $null) { continue }
            $nm = $null
            if ($op -is [Mono.Cecil.MethodReference]) { $nm = $op.Name }
            if ($nm -eq $null) { continue }
            if ($needles -contains $nm) { $hits += ($nm + '@IL_' + $ins.Offset.ToString('x4')) }
        }
        if ($hits.Count -gt 0) {
            $rows.Add(($t.FullName + ' :: ' + $m.Name + '(' + (($m.Parameters | ForEach-Object { $_.ParameterType.Name }) -join ', ') + ')   ->   ' + ($hits -join ', ')))
        }
    }
}

Write-Host ""
Write-Host ("=== 调用点（共 " + $rows.Count + " 处）===")
$rows | Sort-Object | ForEach-Object { Write-Host ('  ' + $_) }


Write-Host ""
Write-Host ("=== " + $TypeFilter + " 方法体概览 ===")
$targets = $all | Where-Object { $_.Name -eq $TypeFilter }
foreach ($t in $targets) {
    Write-Host ("`n### " + $t.FullName)
    foreach ($m in $t.Methods) {
        if (-not $m.HasBody) { continue }
        Write-Host ("`n-- " + $m.Name + '(' + (($m.Parameters | ForEach-Object { $_.ParameterType.Name + ' ' + $_.Name }) -join ', ') + ')  IL=' + $m.Body.Instructions.Count)
        foreach ($ins in $m.Body.Instructions) {
            $op = $ins.Operand
            $txt = ''
            if ($op -ne $null) {
                if ($op -is [Mono.Cecil.MethodReference]) { $txt = $op.DeclaringType.Name + '::' + $op.Name }
                elseif ($op -is [Mono.Cecil.FieldReference]) { $txt = $op.DeclaringType.Name + '::' + $op.Name }
                elseif ($op -is [string]) { $txt = '"' + $op + '"' }
                else { $txt = $op.ToString() }
            }
            Write-Host ('   IL_' + $ins.Offset.ToString('x4') + '  ' + $ins.OpCode.Name + '  ' + $txt)
        }
    }
}

Write-Host ""
Write-Host '=== 完成（只读，未修改任何文件）==='
