





using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using Mono.Cecil;

internal static class Program
{
    private const string TypeName = "OneTimeDecreesPanelProperties";
    private const string MemberName = "MaxNumberOfEnactedDecrees";
    private const string ExpectedInteropSha =
        "f4df4960b1a7e90d2db5e7e01713d31d8150632b82c743cac975f95cacb7299b";
    private const int ExpectedChecks = 11;
    private static int _fails;
    private static int _total;

    private static void Check(string name, bool ok, string detail = null)
    {
        _total++;
        if (!ok) _fails++;
        Console.WriteLine((ok ? "  PASS  " : "  FAIL  ") + name.PadRight(52)
            + (ok ? "" : "  " + (detail ?? "")));
    }

    private static int Main()
    {
        string interop = FindInterop();
        if (interop == null)
        {
            Console.Error.WriteLine("[环境错误] 找不到 BepInEx/interop/Assembly-CSharp.dll");
            return 2;
        }

        Console.WriteLine("═══ task-19：PC 运行时 interop 代理成员面 ═══");
        Console.WriteLine("interop = " + interop);

        string sha = Sha256Of(interop);
        Check("interop DLL sha256 已锁定", string.Equals(sha, ExpectedInteropSha, StringComparison.OrdinalIgnoreCase),
            "actual=" + sha);

        var asm = AssemblyDefinition.ReadAssembly(interop);
        TypeDefinition type = asm.MainModule.Types.FirstOrDefault(t => t.FullName == TypeName);
        Check("PC interop 类型存在：" + TypeName, type != null);
        if (type != null)
        {
            PropertyDefinition prop = type.Properties.FirstOrDefault(p => p.Name == MemberName);
            Check("PC interop 代理暴露同名【属性】", prop != null);
            Check("PC interop 代理属性有 getter", prop != null && prop.GetMethod != null);
            Check("PC interop 代理属性有 setter", prop != null && prop.SetMethod != null);
            Check("PC interop 代理属性 getter/setter 都是 public",
                prop != null && prop.GetMethod != null && prop.GetMethod.IsPublic
                && prop.SetMethod != null && prop.SetMethod.IsPublic);
            Check("PC interop 代理属性类型为 Int32",
                prop != null && prop.PropertyType.FullName == "System.Int32",
                prop == null ? "属性缺失" : prop.PropertyType.FullName);

            FieldDefinition instanceField = type.Fields.FirstOrDefault(f => f.Name == MemberName && !f.IsStatic);
            Check("PC interop 代理【没有】同名实例字段（AccessTools.Field 会落空）", instanceField == null);

            FieldDefinition ptr = type.Fields.FirstOrDefault(f => f.Name == "NativeFieldInfoPtr_" + MemberName);
            Check("PC interop 代理保留静态 NativeFieldInfoPtr_" + MemberName, ptr != null && ptr.IsStatic);

            Check("PC interop 代理有 get_" + MemberName + " 方法",
                type.Methods.Any(m => m.Name == "get_" + MemberName && m.Parameters.Count == 0));
            Check("PC interop 代理有 set_" + MemberName + " 方法",
                type.Methods.Any(m => m.Name == "set_" + MemberName && m.Parameters.Count == 1));
        }

        if (_total != ExpectedChecks)
        {
            _fails++;
            Console.WriteLine("  FAIL  断言总数锁定：期望 " + ExpectedChecks + "，实际 " + _total);
        }

        Console.WriteLine();
        Console.WriteLine("═══ 结果：" + (_fails == 0
            ? (_total + "/" + _total + " 全部通过 ✓")
            : (_fails + " 项失败 ✗（共 " + _total + " 项）")) + " ═══");
        if (_fails == 0)
        {
            Console.WriteLine("结论：PC 运行时 interop 代理暴露可写属性；");
            Console.WriteLine("      “原生成元数据是字段 ⇒ PC 上 AccessTools.Property 必为 null”不成立。");
        }
        return _fails == 0 ? 0 : 1;
    }

    private static string Sha256Of(string path)
    {
        using (var sha = SHA256.Create())
        using (var fs = File.OpenRead(path))
            return BitConverter.ToString(sha.ComputeHash(fs)).Replace("-", "");
    }

    private static string FindInterop()
    {
        var candidates = new List<string>();
        string game = Environment.GetEnvironmentVariable("SUZERAIN_GAME");
        if (!string.IsNullOrEmpty(game))
            candidates.Add(Path.Combine(game, "BepInEx", "interop", "Assembly-CSharp.dll"));
        candidates.Add(@"D:\Program Files (x86)\Steam\steamapps\common\Suzerain\BepInEx\interop\Assembly-CSharp.dll");
        candidates.Add(@"C:\Program Files (x86)\Steam\steamapps\common\Suzerain\BepInEx\interop\Assembly-CSharp.dll");
        return candidates.FirstOrDefault(File.Exists);
    }
}
