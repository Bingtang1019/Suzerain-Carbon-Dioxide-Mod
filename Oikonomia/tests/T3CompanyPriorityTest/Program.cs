


using System;
using Oikonomia;

internal static class Program
{
    private const int ExpectedChecks = 26;
    private static int _fails;
    private static int _checks;

    private static void Check(string name, bool ok, string detail = null)
    {
        _checks++;
        if (!ok) _fails++;
        Console.WriteLine((ok ? "  PASS  " : "  FAIL  ") + name.PadRight(52)
            + (ok ? "" : "  " + (detail ?? "")));
    }

    private static int Main()
    {
        Console.WriteLine("═══ T3CompanyPriority：第一巨构的 Gus/Tusk 差分优先级 ═══");

        Check("Underhall + SSC ⇒ Underhall",
            T3CompanyPriority.Choose("Underhall", "SSC") == "Underhall");
        Check("SSC + Underhall ⇒ Underhall",
            T3CompanyPriority.Choose("SSC", "Underhall") == "Underhall");
        Check("Taurus + SSC ⇒ Taurus",
            T3CompanyPriority.Choose("Taurus", "SSC") == "Taurus");
        Check("SSC + Taurus ⇒ Taurus",
            T3CompanyPriority.Choose("SSC", "Taurus") == "Taurus");
        Check("Underhall + Taurus ⇒ Underhall",
            T3CompanyPriority.Choose("Underhall", "Taurus") == "Underhall");
        Check("只有 SSC ⇒ SSC",
            T3CompanyPriority.Choose("SSC", null) == "SSC");
        Check("只有 Taurus ⇒ Taurus",
            T3CompanyPriority.Choose(null, "Taurus") == "Taurus");
        Check("只有 Underhall ⇒ Underhall",
            T3CompanyPriority.Choose("Underhall", "") == "Underhall");
        Check("都为空 ⇒ null",
            T3CompanyPriority.Choose(null, "") == null);
        Check("未知 + SSC ⇒ SSC（未知项跳过）",
            T3CompanyPriority.Choose("Unknown", "SSC") == "SSC");
        Check("两个都未知 ⇒ null（不猜）",
            T3CompanyPriority.Choose("Unknown", "Unknown") == null);

        Check("旗 Underhall+SSC ⇒ Underhall",
            T3CompanyPriority.ChooseFromFlags(true, false, true) == "Underhall");
        Check("旗 Taurus+SSC ⇒ Taurus",
            T3CompanyPriority.ChooseFromFlags(false, true, true) == "Taurus");
        Check("旗只有 SSC ⇒ SSC",
            T3CompanyPriority.ChooseFromFlags(false, false, true) == "SSC");
        Check("旗全假 ⇒ null",
            T3CompanyPriority.ChooseFromFlags(false, false, false) == null);

        Check("Matches Underhall/Underhall", T3CompanyPriority.Matches("Underhall", "Underhall"));
        Check("Matches 大小写不敏感", T3CompanyPriority.Matches("underhall", "UNDERHALL"));
        Check("Matches Underhall/Taurus = false", !T3CompanyPriority.Matches("Underhall", "Taurus"));
        Check("Matches null = false", !T3CompanyPriority.Matches(null, "SSC"));

        Check("Turn03 Gus Intro Talk 是 T3",
            T3CompanyPriority.IsT3Conversation("Sordland/Turn03/Gus Intro Talk"));
        Check("Turn03 EnT_BusinessCouncil 是 T3",
            T3CompanyPriority.IsT3Conversation("Sordland/Turn03/EnT_BusinessCouncil"));
        Check("真机 Turn03 EnT_GasomProposal（Gus）是 T3",
            T3CompanyPriority.IsT3Conversation("Sordland/Turn03/EnT_GasomProposal"));
        Check("真机 Turn03 Personal_FrancTalk 不是 T3",
            !T3CompanyPriority.IsT3Conversation("Sordland/Turn03/Personal_FrancTalk"));
        Check("真机 Turn03 A_DictatorConstitution 不是 T3",
            !T3CompanyPriority.IsT3Conversation("Sordland/Turn03/A_DictatorConstitution"));
        Check("Turn02 公司选择屏不是 T3",
            !T3CompanyPriority.IsT3Conversation("Sordland/Turn02/InT_HighwayContract"));
        Check("Turn03 其它场景不是 T3",
            !T3CompanyPriority.IsT3Conversation("Sordland/Turn03/Personal_Funeral"));

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
