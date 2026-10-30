



using System;
using Oikonomia;

class Program
{
    static int fails = 0;
    static int total = 0;

    static void Check(string name, int expect, int baseV,
        int depts, int ministryBonus, bool eco, int ecoBonus, bool con, int conBonus,
        bool emg, int emgBonus, bool crisis, int crisisPenalty,
        bool protest, int unrest, bool revolt, int revoltPenalty, int floor,
        string mustContain = null,
        bool corruption = false, int corruptionPenalty = -1)
    {
        total++;
        string detail;
        int got = DecreeLimitCore.Compute(baseV, depts, ministryBonus, 4, 2, eco, ecoBonus, con, conBonus,
            emg, emgBonus, crisis, crisisPenalty, corruption, corruptionPenalty,
            protest, unrest, revolt, revoltPenalty, floor, out detail);
        bool ok = got == expect;
        if (mustContain != null && detail.IndexOf(mustContain, StringComparison.Ordinal) < 0) ok = false;
        if (!ok) fails++;
        Console.WriteLine((ok ? "  PASS  " : "  FAIL  ") + name.PadRight(34)
            + " = " + got + "（期望 " + expect + "）"
            + (mustContain != null ? ("  含「" + mustContain + "」" + (detail.IndexOf(mustContain, StringComparison.Ordinal) >= 0 ? "✓" : "✗")) : ""));
        if (name.StartsWith("明细")) Console.WriteLine("          明细：" + detail);
    }

    static void Main()
    {
        Console.WriteLine("═══ 总统令动态上限：计算核心单元测试 ═══");
        Console.WriteLine("（默认参数：基数 4 / 部门 +1 / 经济 +1 / 宪法 +1 / 紧急 +2 / 危机 −2 / 腐败 −1 / 抗议 −1 / 起义 −2 / 下限 2）");
        Console.WriteLine("");

        Check("① 纯基线（无任何加成）", 4, 4, 0, 1, false, 1, false, 1, false, 2, false, -2, false, -1, false, -2, 2);
        Check("② 四部门全加（+4 再额外 +2）", 10, 4, 4, 1, false, 1, false, 1, false, 2, false, -2, false, -1, false, -2, 2, "四部门齐全(+2)");
        Check("③ 四部门 + 经济 + 宪法", 12, 4, 4, 1, true, 1, true, 1, false, 2, false, -2, false, -1, false, -2, 2, "+4");
        Check("④ 全部加成（含紧急 +2）", 14, 4, 4, 1, true, 1, true, 1, true, 2, false, -2, false, -1, false, -2, 2, "紧急状态");
        Check("⑤ 危机 −2", 12, 4, 4, 1, true, 1, true, 1, true, 2, true, -2, false, -1, false, -2, 2, "危机");
        Check("⑥ 危机 + 和平抗议", 11, 4, 4, 1, true, 1, true, 1, true, 2, true, -2, true, -1, false, -2, 2, "和平抗议");
        Check("⑦ 危机 + 起义 −2", 10, 4, 4, 1, true, 1, true, 1, true, 2, true, -2, false, -1, true, -2, 2, "全国起义");
        Check("⑧ 下限触发（4−2−1−2=−1→2）", 2, 4, 0, 1, false, 1, false, 1, false, 2, true, -2, true, -1, true, -2, 2, "触底取 2");
        Check("⑨ 长局负面全开仍不低于下限", 2, 4, 0, 1, false, 1, false, 1, false, 2, true, -2, true, -1, true, -2, 2);
        Check("⑩ 单项加成 0 但四部门齐全仍 +2", 6, 4, 4, 0, false, 1, false, 1, false, 2, false, -2, false, -1, false, -2, 2);
        Check("⑪ 危机惩罚设为 0 时不扣", 4, 4, 0, 1, false, 1, false, 1, false, 2, true, 0, false, -1, false, -2, 2);
        Check("⑫ 下限可配（改 5）", 5, 4, 0, 1, false, 1, false, 1, false, 2, true, -2, true, -1, true, -2, 5, "触底取 5");
        Check("⑬ 基数可配（改 10，负面全开）", 5, 10, 0, 1, false, 1, false, 1, false, 2, true, -2, true, -1, true, -2, 2);
        
        Check("⑭ 明细含负原始值", 2, 4, 0, 1, false, 1, false, 1, false, 2, true, -2, true, -1, true, -2, 2, "原始 -1");
        
        Check("⑮ 明细含零原始值", 2, 4, 1, 1, false, 1, false, 1, false, 2, true, -2, true, -1, true, -2, 2, "原始 0");

        
        
        Check("⑯ 仅腐败 −1（4−1=3）", 3, 4, 0, 1, false, 1, false, 1, false, 2, false, -2, false, -1, false, -2, 2, "腐败(-1)", true, -1);
        Check("⑰ 腐败 + 危机 → 触底 2", 2, 4, 0, 1, false, 1, false, 1, false, 2, true, -2, false, -1, false, -2, 2, "触底取 2", true, -1);
        Check("⑱ 腐败 + 四部门全加 = 9", 9, 4, 4, 1, false, 1, false, 1, false, 2, false, -2, false, -1, false, -2, 2, "腐败(-1)", true, -1);
        Check("⑲ CorruptionPenalty=0 不扣", 4, 4, 0, 1, false, 1, false, 1, false, 2, false, -2, false, -1, false, -2, 2, null, true, 0);

        Console.WriteLine("");
        Console.WriteLine("═══ 结果：" + (fails == 0 ? (total + "/" + total + " 全部通过 ✓") : (fails + " 项失败 ✗（共 " + total + " 项）")) + " ═══");
        Environment.Exit(fails == 0 ? 0 : 1);
    }
}
