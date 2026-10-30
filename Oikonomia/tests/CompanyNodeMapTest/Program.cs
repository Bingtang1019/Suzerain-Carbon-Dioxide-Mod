


using System;
using System.Collections.Generic;
using Oikonomia;

internal static class Program
{
    private const int ExpectedChecks = 15;
    private static int _fails;
    private static int _checks;

    private static void Check(string name, bool ok, string detail = null)
    {
        _checks++;
        if (!ok) _fails++;
        Console.WriteLine((ok ? "  PASS  " : "  FAIL  ") + name.PadRight(46)
            + (ok ? "" : "  " + (detail ?? "")));
    }

    private static int Main()
    {
        var expected = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "0x010000000000C3A3", "H-3" },
            { "0x010000000000C3C4", "H-3" },
            { "0x0100000100000F2C", "H-3" },
            { "0x0100000100000F4A", "H-3" },
            { "0x010000000000BBCA", "L-1" },
            { "0x010000000000BBEE", "L-1" },
            { "0x010000000000BEB7", "L-1" },
            { "0x0100000100000F64", "L-1" },
            { "0x0100000100000F6A", "L-1" },
            { "0x0100000100000F74", "L-1" },
        };

        Console.WriteLine("═══ CompanyNodeMap：第一巨构选公司屏 → 项目 ═══");
        Check("映射总数 = 10", CompanyNodeMap.Count == 10, "实测=" + CompanyNodeMap.Count);

        foreach (var kv in expected)
        {
            string got;
            bool ok = CompanyNodeMap.TryGet(kv.Key, out got);
            Check(kv.Key + " ⇒ " + kv.Value, ok && got == kv.Value, "实测=" + (got ?? "null"));
        }

        string unused;
        Check("未知节点 ⇒ false", !CompanyNodeMap.TryGet("0xDEADBEEF", out unused));
        Check("空串 ⇒ false", !CompanyNodeMap.TryGet("", out unused));
        Check("null ⇒ false", !CompanyNodeMap.TryGet(null, out unused));

        string lower;
        bool ci = CompanyNodeMap.TryGet("0x010000000000c3a3", out lower) && lower == "H-3";
        Check("节点 id 大小写不敏感", ci, "实测=" + (lower ?? "null"));

        if (_checks != ExpectedChecks)
        {
            _fails++;
            Console.WriteLine("  FAIL  断言总数锁定：期望 " + ExpectedChecks + "，实际 " + _checks);
        }

        Console.WriteLine();
        Console.WriteLine("═══ 结果：" + (_fails == 0
            ? (_checks + "/" + _checks + " 全部通过 ✓")
            : (_fails + " 项失败 ✗（共 " + _checks + " 项）")) + " ═══");
        return _fails == 0 ? 0 : 1;
    }
}
