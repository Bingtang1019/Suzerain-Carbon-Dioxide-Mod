

















using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using Oikonomia;

class Program
{
    static int fails = 0;
    static int total = 0;

    
    static void True(string name, bool ok, string note)
    {
        total++;
        if (!ok) fails++;
        Console.WriteLine((ok ? "  PASS  " : "  FAIL  ") + name + "   " + note);
    }

    static void SameInt(string name, int got, int expect)
    {
        total++;
        bool ok = got == expect;
        if (!ok) fails++;
        Console.WriteLine((ok ? "  PASS  " : "  FAIL  ") + name + "   " + got + "（期望 " + expect + "）");
    }

    static void Same(string name, string got, string expect)
    {
        total++;
        bool ok = string.Equals(got, expect, StringComparison.Ordinal);
        if (!ok) fails++;
        Console.WriteLine((ok ? "  PASS  " : "  FAIL  ") + name);
        if (!ok)
        {
            Console.WriteLine("          期望：" + Show(expect));
            Console.WriteLine("          实际：" + Show(got));
        }
    }

    static void Has(string name, string got, string needle)
    {
        total++;
        bool ok = got != null && got.IndexOf(needle, StringComparison.Ordinal) >= 0;
        if (!ok) fails++;
        Console.WriteLine((ok ? "  PASS  " : "  FAIL  ") + name + "   必须含「" + needle + "」");
        if (!ok) Console.WriteLine("          实际：" + Show(got));
    }

    static void NotHas(string name, string got, string needle)
    {
        total++;
        bool ok = got != null && got.IndexOf(needle, StringComparison.Ordinal) < 0;
        if (!ok) fails++;
        Console.WriteLine((ok ? "  PASS  " : "  FAIL  ") + name + "   必须不含「" + needle + "」");
        if (!ok) Console.WriteLine("          实际：" + Show(got));
    }

    
    
    
    
    
    static bool HasTotalLine(string rendered, out string why)
    {
        why = null;
        if (rendered == null) { why = "null"; return false; }
        string plain = Plain(rendered);
        if (plain.IndexOf("Total:", StringComparison.Ordinal) >= 0) { why = "出现字面「Total:」"; return true; }
        if (plain.IndexOf("合计", StringComparison.Ordinal) >= 0) { why = "出现字面「合计」"; return true; }
        if (plain.IndexOf("预计", StringComparison.Ordinal) >= 0) { why = "出现字面「预计」"; return true; }
        why = "无合计行（Total: / 合计 / 预计 三层全部未命中）";
        return false;
    }

    static string Show(string s)
    {
        if (s == null) return "(null)";
        var one = s.Replace("\r", "\\r").Replace("\n", "\\n");
        return "\"" + (one.Length > 400 ? one.Substring(0, 400) + "…" : one) + "\"";
    }

    static int CountOf(string s, string needle)
    {
        if (string.IsNullOrEmpty(s) || string.IsNullOrEmpty(needle)) return 0;
        int n = 0, i = 0;
        while ((i = s.IndexOf(needle, i, StringComparison.Ordinal)) >= 0) { n++; i += needle.Length; }
        return n;
    }

    
    static string Plain(string rendered)
    {
        if (rendered == null) return null;
        return Regex.Replace(rendered, "<[^>]*>", "");
    }

    
    static string[] PlainLines(string rendered)
    {
        var list = new List<string>();
        foreach (var line in rendered.Split('\n'))
        {
            var s = Regex.Replace(line, "<[^>]*>", "");
            if (s.Length > 0) list.Add(s);
        }
        return list.ToArray();
    }

    
    
    
    
    
    
    sealed class Ledger
    {
        public readonly int[] PrevTier = { -1, -1, -1, -1 };
        public readonly bool[] PrevEdge;
        public bool PrevTaxAvoid;
        public bool HasSnapshot;
        public int SnapTurn = -1;
        public int PreviewCalls;

        public Ledger(int edgeCount) { PrevEdge = new bool[edgeCount]; }

        
        public int Preview(SettleState s, StringBuilder parts, List<InGameDetailFormat.Entry> entries)
        {
            PreviewCalls++;
            var input = s.ToInput(this);
            return SettleMath.Compute(input, s.Cfg, s.Rules, parts, entries);
        }

        
        public int Settle(SettleState s, StringBuilder parts, List<InGameDetailFormat.Entry> entries)
        {
            var input = s.ToInput(this);
            int delta = SettleMath.Compute(input, s.Cfg, s.Rules, parts, entries);

            
            for (int r = 0; r < 4; r++) PrevTier[r] = s.Tier[r];
            PrevTaxAvoid = s.TaxAvoid;
            for (int i = 0; i < PrevEdge.Length; i++) PrevEdge[i] = i < s.Edge.Length && s.Edge[i];
            HasSnapshot = true;
            SnapTurn = s.Turn;
            return delta;
        }

        public string Fingerprint()
        {
            var sb = new StringBuilder();
            sb.Append("has=").Append(HasSnapshot).Append(" turn=").Append(SnapTurn);
            sb.Append(" prevTier=").Append(string.Join(",", Array.ConvertAll(PrevTier, x => x.ToString())));
            sb.Append(" prevTaxAvoid=").Append(PrevTaxAvoid);
            sb.Append(" prevEdge=").Append(string.Join(",", Array.ConvertAll(PrevEdge, x => x ? "1" : "0")));
            return sb.ToString();
        }
    }

    
    sealed class SettleState
    {
        public SettleCfg Cfg = DefaultCfg();
        public List<PerTurnRule> Rules = new List<PerTurnRule>();
        public int IndicatorTier = 2;                       
        public int[] Tier = { -1, -1, -1, -1 };
        public bool TaxEfficient;
        public bool TaxAvoid;
        public int TradeTier = -1;
        public int TourismTier = -1;
        public bool[] Edge = new bool[0];
        public bool EdgeShort;
        public int EdgeLoaded = -1;
        public int Turn = 10;
        public string[] EdgeNames = new string[0];
        public bool[] EdgeIsTrade = new bool[0];

        public SettleInput ToInput(Ledger l)
        {
            var st = new SettleInput();
            st.Enabled = Cfg.Enabled;
            
            st.BaselineValid = l.HasSnapshot && !(l.HasSnapshot && l.SnapTurn >= 0 && Turn < l.SnapTurn);
            st.IndicatorTier = IndicatorTier;
            st.IndicatorLabels = Labels.Indicator;
            st.Tier = (int[])Tier.Clone();
            st.PrevTier = (int[])l.PrevTier.Clone();       
            st.RegionLabels = Labels.Region;
            st.TaxEfficient = TaxEfficient;
            st.TaxAvoid = TaxAvoid;
            st.PrevTaxAvoid = l.PrevTaxAvoid;
            st.TradeTier = Cfg.TradeOn ? TradeTier : -1;
            st.TourismTier = Cfg.TourismOn ? TourismTier : -1;
            st.TradeLabels = Labels.Trade;
            st.TourismLabels = Labels.Tourism;
            st.Edge = (bool[])Edge.Clone();
            st.PrevEdge = (bool[])l.PrevEdge.Clone();
            st.EdgeIsTrade = (bool[])EdgeIsTrade.Clone();
            st.EdgeNames = EdgeNames;
            st.EdgeShort = EdgeShort;
            st.EdgeLoaded = EdgeLoaded;
            return st;
        }
    }

    static class Labels
    {
        public static readonly string[] Indicator = { "危险", "不稳定", "稳定", "增长", "极快增长" };
        public static readonly string[] Region = { "贝吉亚", "格鲁尼", "洛伦", "阿格兰" };
        public static readonly string[] Trade = { "增加", "恢复", "减少" };
        public static readonly string[] Tourism = { "蓬勃发展", "平均水平", "严重衰退" };
    }

    
    static SettleCfg DefaultCfg()
    {
        var c = new SettleCfg();
        c.Enabled = true;
        c.EdOn = c.RegionsOn = c.TradeOn = c.TourismOn = c.TaxOn = c.GrowthOn = c.TradeDealsOn = true;
        c.EffDangerDelta = -2; c.EffUnstableDelta = -1; c.EffStableDelta = 0;
        c.EffGrowingDelta = 1; c.EffVeryGoodDelta = 1;
        c.EffRegionStepDelta = 1;
        c.EffTaxEfficientDelta = 1; c.EffTaxAvoidanceDelta = -1;
        c.EffTradeIncreasedDelta = 2; c.EffTradeRecoveredDelta = 1; c.EffTradeDecreasedDelta = 0;
        c.EffTourismGoodDelta = 1; c.EffTourismAverageDelta = 0; c.EffTourismBadDelta = -1;
        c.EffGrowthBonus = 1; c.EffTradeDealBonus = 1;
        return c;
    }

    static PerTurnRule R(string label, int delta, string group)
    {
        var r = new PerTurnRule();
        r.Label = label; r.Delta = delta; r.Group = group ?? "";
        return r;
    }

    static List<InGameDetailFormat.Entry> NewEntries() { return new List<InGameDetailFormat.Entry>(); }

    static string EntriesFingerprint(List<InGameDetailFormat.Entry> es)
    {
        var sb = new StringBuilder();
        for (int i = 0; i < es.Count; i++)
            sb.Append(es[i].Text).Append('|').Append(es[i].Delta).Append('|')
              .Append(es[i].Kind).Append('|').Append(es[i].Group).Append(" ; ");
        return sb.ToString();
    }

    
    static SettleState SpecState(Ledger l)
    {
        var s = new SettleState();
        s.Turn = 11;
        s.IndicatorTier = 3;                                  
        s.TourismTier = 0;                                    
        s.Rules.Add(R("免费处方药", -1, InGameDetailFormat.GroupExtraWelfare));
        s.Tier[0] = 2; l.PrevTier[0] = 1;                     
        s.TaxAvoid = true; l.PrevTaxAvoid = false;            
        return s;
    }

    
    static Ledger SteadyLedger(int edgeCount)
    {
        var l = new Ledger(edgeCount);
        l.HasSnapshot = true;
        l.SnapTurn = 10;
        l.PrevTier[0] = 1; l.PrevTier[1] = 1; l.PrevTier[2] = 1; l.PrevTier[3] = 1;
        l.PrevTaxAvoid = false;
        return l;
    }

    
    
    
    
    
    
    
    
    static int DeltaOnly(Action<SettleState> setup) { return DeltaOnly(1, setup); }

    static int DeltaOnly(int edgeCount, Action<SettleState> setup)
    {
        var l = SteadyLedger(edgeCount);
        var s = new SettleState();
        s.Turn = 11;
        s.IndicatorTier = -1;
        s.Cfg.EdOn = s.Cfg.RegionsOn = s.Cfg.TradeOn = s.Cfg.TourismOn = false;
        s.Cfg.TaxOn = s.Cfg.GrowthOn = s.Cfg.TradeDealsOn = false;
        if (setup != null) setup(s);
        return l.Preview(s, new StringBuilder(), NewEntries());
    }

    static void Main()
    {
        Console.WriteLine("═══ SettlePreviewTest —— 「当前预测下的未来财政状态」只读预演 ═══");
        Console.WriteLine("");

        
        Console.WriteLine("【① 只读性（结构证明：内核层面无法写入任何状态）】");
        var compute = typeof(SettleMath).GetMethod("Compute", BindingFlags.Public | BindingFlags.Static);
        True("①-1 SettleMath.Compute 存在且是 public static", compute != null,
            "找到 = " + (compute != null));

        if (compute != null)
        {
            var ps = compute.GetParameters();
            bool anyByRef = false;
            foreach (var p in ps) if (p.ParameterType.IsByRef) anyByRef = true;
            True("①-2 Compute 没有任何 ref/out 参数（无法回写调用方状态）", !anyByRef,
                "参数 " + ps.Length + " 个，byref = " + (anyByRef ? "有" : "无"));

            bool pure = true;
            var ns = new List<string>();
            foreach (var p in ps)
            {
                var n = p.ParameterType.Namespace;
                ns.Add(p.ParameterType.Name);
                if (n != "System" && n != "System.Text" && n != "System.Collections.Generic" && n != "Oikonomia")
                    pure = false;
            }
            True("①-3 Compute 的参数全是纯数据类型（无游戏 / 存档 / 反射 / IO 类型）", pure,
                string.Join(", ", ns));
        }

        var staticFields = typeof(SettleMath).GetFields(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        SameInt("①-4 SettleMath 没有任何静态字段（无静态可变状态可写）", staticFields.Length, 0);

        var instMembers = new List<string>();
        foreach (var f in typeof(SettleMath).GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)) instMembers.Add("F:" + f.Name);
        foreach (var m in typeof(SettleMath).GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly)) instMembers.Add("M:" + m.Name);
        foreach (var c in typeof(SettleMath).GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)) instMembers.Add("C:" + c.Name);
        SameInt("①-5 SettleMath 没有任何实例成员（纯静态，无处藏状态）", instMembers.Count, 0);

        
        Console.WriteLine("");
        Console.WriteLine("【② 预演不推进快照（连续 N 次 → PrevTier/PrevEdge/PrevTaxAvoid/快照逐字节不变）】");
        {
            var l = SteadyLedger(21);
            var s = SpecState(l);
            s.Edge = new bool[21];
            s.EdgeNames = new string[21];
            s.EdgeIsTrade = new bool[21];
            for (int i = 0; i < 21; i++) s.EdgeNames[i] = "词条" + i;
            s.Edge[5] = true;                                  

            string before = l.Fingerprint();
            int first = -999;
            string firstEntries = null, lastEntries = null;
            var deltas = new List<int>();
            for (int k = 0; k < 5; k++)
            {
                var parts = new StringBuilder();
                var es = NewEntries();
                int d = l.Preview(s, parts, es);
                deltas.Add(d);
                if (k == 0) { first = d; firstEntries = EntriesFingerprint(es); }
                if (k == 4) lastEntries = EntriesFingerprint(es);
            }

            Same("②-0 连续 5 次预演：快照指纹逐字节不变", l.Fingerprint(), before);
            SameInt("②-1 连续 5 次预演：调用计数 = 5（确实调了，不是没跑）", l.PreviewCalls, 5);
            bool allSame = true;
            for (int k = 0; k < deltas.Count; k++) if (deltas[k] != first) allSame = false;
            True("②-2 连续 5 次预演：delta 完全一致（幂等、无消耗）", allSame,
                string.Join("/", deltas.ConvertAll(x => x.ToString())));
            SameInt("②-3 预演 5 次后 HasSnapshot 未被改写", l.HasSnapshot ? 1 : 0, 1);
            SameInt("②-4 预演 5 次后 SnapTurn 未被推进", l.SnapTurn, 10);
            True("②-5 预演 5 次后 PrevEdge[5]（本次为真的增长词条）仍为假", l.PrevEdge[5] == false,
                "PrevEdge[5] = " + l.PrevEdge[5]);
            True("②-6 预演读到的快照仍是**旧**的（新快照只在真实结算时写）", l.PrevTier[0] == 1,
                "PrevTier[0] = " + l.PrevTier[0]);
            Same("②-7 第 1 次与第 5 次的明细条目完全一致", lastEntries, firstEntries);
            True("②-8 该场景确实命中了一次性词条（+1），不是空跑",
                deltas[0] == 1 + 1 - 1 + 1 - 1 + 1, "delta = " + deltas[0] + "（含增长词条 #5 的 +1）");
        }

        
        Console.WriteLine("");
        Console.WriteLine("【③ 预演 delta == 随后真实结算的 delta（同一词条状态下）】");
        {
            var l = SteadyLedger(21);
            var s = SpecState(l);

            var pParts = new StringBuilder(); var pEs = NewEntries();
            int pd = l.Preview(s, pParts, pEs);

            var sParts = new StringBuilder(); var sEs = NewEntries();
            int sd = l.Settle(s, sParts, sEs);

            SameInt("③-1 预演 delta == 结算 delta", pd, sd);
            Same("③-2 预演 parts（日志/文件桥文本）== 结算 parts", pParts.ToString(), sParts.ToString());
            Same("③-3 预演明细条目 == 结算明细条目", EntriesFingerprint(pEs), EntriesFingerprint(sEs));
            SameInt("③-4 明细之和 == delta（不变量）", InGameDetailFormat.SumDeltas(pEs), pd);
            Same("③-5 结算 parts 逐字等于旧口径形态（抽取未改日志/文件桥文案）",
                pParts.ToString(), "ED=增长+1  贝吉亚↑1档+1  免费处方药(每回合)-1  旅游业=蓬勃发展+1  避税(一次性)-1  ");
            SameInt("③-6 结算推进了快照（SnapTurn 10 → 11）", l.SnapTurn, 11);
            SameInt("③-7 结算后 PrevTier[0] 更新为 2", l.PrevTier[0], 2);
        }

        
        Console.WriteLine("");
        Console.WriteLine("【④ 一次性机制不被预演消耗（最关键的一条）】");
        {
            var l = SteadyLedger(3);
            var s = new SettleState();
            s.Turn = 11;
            s.IndicatorTier = 2;                                   
            s.Tier[0] = 1;                                         
            s.Edge = new bool[] { true, false, false };             
            s.EdgeNames = new[] { "高就业", "产量增加", "小企业成长" };
            s.EdgeIsTrade = new[] { false, false, false };

            int d1 = l.Preview(s, new StringBuilder(), NewEntries());
            int d2 = l.Preview(s, new StringBuilder(), NewEntries());
            int d3 = l.Preview(s, new StringBuilder(), NewEntries());
            SameInt("④-1 预演第 1 次：一次性增长 +1", d1, 1);
            SameInt("④-2 预演第 2 次：仍是 +1（没被消耗）", d2, 1);
            SameInt("④-3 预演第 3 次：仍是 +1（没被消耗）", d3, 1);
            SameInt("④-4 预演 3 次后 PrevEdge[0] 仍为假", l.PrevEdge[0] ? 1 : 0, 0);

            int ds = l.Settle(s, new StringBuilder(), NewEntries());
            SameInt("④-5 真实结算：+1", ds, 1);
            SameInt("④-6 真实结算后 PrevEdge[0] 才被置真（一次性此时才被消费）", l.PrevEdge[0] ? 1 : 0, 1);

            int d4 = l.Preview(s, new StringBuilder(), NewEntries());
            SameInt("④-7 结算之后预演：一次性不再计入（0）—— 预演看到的是**下一回合**", d4, 0);
        }

        
        {
            var l = new Ledger(3);
            var s = new SettleState();
            s.Turn = 3;
            s.IndicatorTier = 3;                                   
            s.Edge = new bool[] { true, false, false };
            s.EdgeNames = new[] { "高就业", "产量增加", "小企业成长" };
            s.EdgeIsTrade = new[] { false, false, false };
            s.TaxAvoid = true;

            var pp = new StringBuilder(); var pe = NewEntries();
            int pd = l.Preview(s, pp, pe);
            var sp = new StringBuilder(); var se = NewEntries();
            int sd = l.Settle(s, sp, se);
            SameInt("④-8 无快照（重建基线回合）：一次性词条不结算 → 只有每回合类 +1", pd, 1);
            SameInt("④-9 该回合预演 == 结算", pd, sd);
        }

        
        Console.WriteLine("");
        Console.WriteLine("【⑤ cfg 参数逐个生效（防「抽 cfg 结构时漏字段」）】");
        {
            SameInt("⑤-1 ED=危险 用 EffDangerDelta",
                DeltaOnly(x => { x.Cfg.EdOn = true; x.Cfg.EffDangerDelta = -5; x.IndicatorTier = 0; }), -5);
            SameInt("⑤-2 ED=不稳定 用 EffUnstableDelta",
                DeltaOnly(x => { x.Cfg.EdOn = true; x.Cfg.EffUnstableDelta = -5; x.IndicatorTier = 1; }), -5);
            SameInt("⑤-3 ED=稳定 用 EffStableDelta",
                DeltaOnly(x => { x.Cfg.EdOn = true; x.Cfg.EffStableDelta = 7; x.IndicatorTier = 2; }), 7);
            SameInt("⑤-4 ED=增长 用 EffGrowingDelta",
                DeltaOnly(x => { x.Cfg.EdOn = true; x.Cfg.EffGrowingDelta = 7; x.IndicatorTier = 3; }), 7);
            SameInt("⑤-5 ED=极快增长 用 EffVeryGoodDelta",
                DeltaOnly(x => { x.Cfg.EdOn = true; x.Cfg.EffVeryGoodDelta = 7; x.IndicatorTier = 4; }), 7);
            SameInt("⑤-6 地区步长用 EffRegionStepDelta（贝吉亚升 1 档 × 3）",
                DeltaOnly(x => { x.Cfg.RegionsOn = true; x.Cfg.EffRegionStepDelta = 3; x.IndicatorTier = -1; x.Tier[0] = 2; }), 3);
            SameInt("⑤-7 旅游业好 用 EffTourismGoodDelta",
                DeltaOnly(x => { x.Cfg.TourismOn = true; x.Cfg.EffTourismGoodDelta = 5; x.TourismTier = 0; }), 5);
            SameInt("⑤-8 旅游业中 用 EffTourismAverageDelta",
                DeltaOnly(x => { x.Cfg.TourismOn = true; x.Cfg.EffTourismAverageDelta = 5; x.TourismTier = 1; }), 5);
            SameInt("⑤-9 旅游业坏 用 EffTourismBadDelta",
                DeltaOnly(x => { x.Cfg.TourismOn = true; x.Cfg.EffTourismBadDelta = -5; x.TourismTier = 2; }), -5);
            SameInt("⑤-10 贸易量增加 用 EffTradeIncreasedDelta",
                DeltaOnly(x => { x.Cfg.TradeOn = true; x.Cfg.EffTradeIncreasedDelta = 4; x.TradeTier = 0; }), 4);
            SameInt("⑤-11 贸易量恢复 用 EffTradeRecoveredDelta",
                DeltaOnly(x => { x.Cfg.TradeOn = true; x.Cfg.EffTradeRecoveredDelta = 4; x.TradeTier = 1; }), 4);
            SameInt("⑤-12 贸易量减少 用 EffTradeDecreasedDelta",
                DeltaOnly(x => { x.Cfg.TradeOn = true; x.Cfg.EffTradeDecreasedDelta = -4; x.TradeTier = 2; }), -4);
            SameInt("⑤-13 税效率用 EffTaxEfficientDelta",
                DeltaOnly(x => { x.Cfg.TaxOn = true; x.Cfg.EffTaxEfficientDelta = 6; x.TaxEfficient = true; }), 6);
            SameInt("⑤-14 避税用 EffTaxAvoidanceDelta",
                DeltaOnly(x => { x.Cfg.TaxOn = true; x.Cfg.EffTaxAvoidanceDelta = -6; x.TaxAvoid = true; }), -6);
            SameInt("⑤-15 增长组用 EffGrowthBonus",
                DeltaOnly(x =>
                {
                    x.Cfg.GrowthOn = true; x.Cfg.EffGrowthBonus = 9;
                    x.Edge = new bool[] { true }; x.EdgeNames = new[] { "高就业" }; x.EdgeIsTrade = new bool[] { false };
                }), 9);
            SameInt("⑤-16 贸易协议用 EffTradeDealBonus",
                DeltaOnly(x =>
                {
                    x.Cfg.TradeDealsOn = true; x.Cfg.EffTradeDealBonus = 9;
                    x.Edge = new bool[] { true }; x.EdgeNames = new[] { "韦伦·石油自由贸易" }; x.EdgeIsTrade = new bool[] { true };
                }), 9);
            SameInt("⑤-17 每回合条件词条用规则自带 Delta",
                DeltaOnly(x => { x.IndicatorTier = -1; x.Rules.Add(R("逃税", -7, null)); }), -7);

            
            SameInt("⑤-18 总开关关闭 → delta = 0（ED 已开且增量 7）",
                DeltaOnly(x => { x.Cfg.Enabled = false; x.Cfg.EdOn = true; x.Cfg.EffGrowingDelta = 7; x.IndicatorTier = 3; }), 0);
            SameInt("⑤-19 EdOn=false → 指示器不计入（对照 ⑤-4 的 +7）",
                DeltaOnly(x => { x.Cfg.EdOn = false; x.Cfg.EffGrowingDelta = 7; x.IndicatorTier = 3; }), 0);
            SameInt("⑤-20 RegionsOn=false → 地区升降不计入（对照 ⑤-6 的 +3）",
                DeltaOnly(x => { x.Cfg.RegionsOn = false; x.Tier[0] = 2; }), 0);
            SameInt("⑤-21 TradeOn=false → 贸易量不计入（对照 ⑤-10 的 +4）",
                DeltaOnly(x => { x.Cfg.TradeOn = false; x.TradeTier = 0; }), 0);
            SameInt("⑤-22 TourismOn=false → 旅游业不计入（对照 ⑤-7 的 +5）",
                DeltaOnly(x => { x.Cfg.TourismOn = false; x.TourismTier = 0; }), 0);
            SameInt("⑤-23 TaxOn=false → 税务组不计入（对照 ⑤-13/⑤-14）",
                DeltaOnly(x => { x.Cfg.TaxOn = false; x.TaxEfficient = true; x.TaxAvoid = true; }), 0);
            SameInt("⑤-24 GrowthOn=false → 增长组不计入（对照 ⑤-15 的 +9）",
                DeltaOnly(x =>
                {
                    x.Cfg.GrowthOn = false;
                    x.Edge = new bool[] { true }; x.EdgeNames = new[] { "高就业" }; x.EdgeIsTrade = new bool[] { false };
                }), 0);
            SameInt("⑤-25 TradeDealsOn=false → 贸易协议不计入（对照 ⑤-16 的 +9）",
                DeltaOnly(x =>
                {
                    x.Cfg.TradeDealsOn = false;
                    x.Edge = new bool[] { true }; x.EdgeNames = new[] { "韦伦·石油自由贸易" }; x.EdgeIsTrade = new bool[] { true };
                }), 0);
            SameInt("⑤-26 规则 Delta=0 → 内核跳过（双保险；生产侧读取阶段也剔除）",
                DeltaOnly(x => { x.Rules.Add(R("逃税", 0, null)); }), 0);

            
            SameInt("⑤-27 EdgeShort 且下标 ≥ EdgeLoaded → 不补发",
                DeltaOnly(2, x =>
                {
                    x.Cfg.GrowthOn = true;
                    x.Edge = new bool[] { false, true };
                    x.EdgeNames = new[] { "高就业", "产量增加" };
                    x.EdgeIsTrade = new bool[] { false, false };
                    x.EdgeShort = true; x.EdgeLoaded = 1;
                }), 0);
            SameInt("⑤-28 EdgeShort 但下标 < EdgeLoaded → 照常结算",
                DeltaOnly(2, x =>
                {
                    x.Cfg.GrowthOn = true;
                    x.Edge = new bool[] { true, false };
                    x.EdgeNames = new[] { "高就业", "产量增加" };
                    x.EdgeIsTrade = new bool[] { false, false };
                    x.EdgeShort = true; x.EdgeLoaded = 1;
                }), 1);
        }

        
        Console.WriteLine("");
        Console.WriteLine("【⑥ 文案：前瞻标题 + 两段式小计（2.7.8 取消末尾合计行）】");
        {
            string whyTotal = null;      
            var l = SteadyLedger(0);
            var s = SpecState(l);
            var parts = new StringBuilder();
            var es = NewEntries();
            int d = l.Preview(s, parts, es);

            SameInt("⑥-1 预计增量 = +1", d, 1);

            string out3 = InGameDetailFormat.Compose(es, d, true);
            string out5 = InGameDetailFormat.Compose(es, d, true,
                InGameDetailFormat.PreviewTitle, null);   

            NotHas("⑥-2 三参重载（旧调用点）不带标题 —— 既有测试的行为没变", out3,
                InGameDetailFormat.PreviewTitle);
            True("⑥-3 三参重载（结算路）整段**不含合计行**（2.7.8 取消合计行的验收判据）",
                !HasTotalLine(out3, out whyTotal), whyTotal ?? "");
            Has("⑥-4 五参重载：标题行 = 当前预测下的财政状态", out5,
                InGameDetailFormat.PreviewTitle);
            Same("⑥-4b 标题常量逐字 == 当前预测下的财政状态（2.7.6 用户逐字指定；2.7.5 曾是「…的未来财政状态」）",
                InGameDetailFormat.PreviewTitle, "当前预测下的财政状态");
            True("⑥-4c 标题不再含旧文案「下一回合预算变化（按当前词条）」",
                InGameDetailFormat.PreviewTitle.IndexOf("下一回合预算变化", StringComparison.Ordinal) < 0
                && InGameDetailFormat.PreviewTitle.IndexOf("（按当前词条）", StringComparison.Ordinal) < 0, "");
            True("⑥-5 五参重载（预演路）整段同样**不含合计行**（两条路都取消）",
                !HasTotalLine(out5, out whyTotal), whyTotal ?? "");
            NotHas("⑥-6 五参重载不再出现中文「预计」（2.7.7 起就不再出现）", out5, "预计");
            NotHas("⑥-7 五参重载不再出现「本回合无变动」（有分项）", out5, InGameDetailFormat.NoChangeText);

            var lines = PlainLines(out5);
            Same("⑥-8 首行 = 标题（标题必须在段头之前）", lines[0], InGameDetailFormat.PreviewTitle);
            Same("⑥-9 末行 = 末段小计行（2.7.8 起整段以小计收尾，不再是合计行）",
                lines[lines.Length - 1], "── (0 小计)");
            Same("⑥-10 第 2 行 = 标题下的细分隔线（2.7.6 新增；2.7.8 起整段只剩这一条）", lines[1], "──────");
            Same("⑥-10b 第 3 行 = 【未来】段头", lines[2], "【未来】");
            True("⑥-11 两段都在（段头已改名为【未来】/【当前】）",
                out5.IndexOf("【未来】", StringComparison.Ordinal) >= 0
                && out5.IndexOf("【当前】", StringComparison.Ordinal) >= 0, "行数 = " + lines.Length);

            SameInt("⑥-12 额外福利支出行 = -1（组归并后一行）",
                InGameDetailFormat.ParseLineValue(out5, "额外福利支出") ?? 999, -1);
            SameInt("⑥-13 贝吉亚地区发展提速行 = +1（本回合类）",
                InGameDetailFormat.ParseLineValue(out5, "贝吉亚地区发展提速") ?? 999, 1);
            SameInt("⑥-14 资本避税问题行 = -1（一次性）",
                InGameDetailFormat.ParseLineValue(out5, "资本避税问题") ?? 999, -1);
            SameInt("⑥-15 经济状态日益增长行 = +1",
                InGameDetailFormat.ParseLineValue(out5, "经济状态日益增长") ?? 999, 1);
            SameInt("⑥-16 全国旅游业蓬勃发展行 = +1",
                InGameDetailFormat.ParseLineValue(out5, "全国旅游业蓬勃发展") ?? 999, 1);
            SameInt("⑥-17 两条路都已没有合计行 ⇒ 原本「读回画面合计」的判据改为：数值只能从小计行读回",
                InGameDetailFormat.ParseLineValue(out5, "小计") ?? 999, 1);
            
            SameInt("⑥-18 两段小计逐行核对：第 1 段 = +1 / 第 2 段 = 0（末行）",
                (lines[6] == "── (+1 小计)" ? 10 : 0) + (lines[lines.Length - 1] == "── (0 小计)" ? 1 : 0), 11);

            int subCount = 0;
            foreach (var ln in lines) if (ln.IndexOf("小计", StringComparison.Ordinal) >= 0) subCount++;
            SameInt("⑥-19 两个小计行（每段各一）", subCount, 2);
            Has("⑥-20 每回合小计 = `── (+1 小计)`", Plain(out5), "── (+1 小计)");
            Has("⑥-21 本回合小计 = 0（+1 与 -1 相抵 → `── (0 小计)`）", Plain(out5), "── (0 小计)");
            SameInt("⑥-22 分隔线整段只剩 1 条（标题下那条；合计前那条已随合计行删除）",
                CountOf(out5, "──────"), 1);
            NotHas("⑥-22b 字面「Total:」不再出现（2.7.8 取消合计行的逐字节判据）", out5, "Total:");

            
            
            
            
            
            True("⑥-23 逐行内容与用户口径一致（居中标题 / 分隔线 / 两段 / 两小计 / (值 说明)；2.7.8 无合计行）",
                lines.Length == 11
                && lines[0] == InGameDetailFormat.PreviewTitle
                && lines[1] == "──────"
                && lines[2] == "【未来】"
                && lines[3] == "(+1 经济状态日益增长)"
                && lines[4] == "(-1 额外福利支出)"
                && lines[5] == "(+1 全国旅游业蓬勃发展)"
                && lines[6] == "── (+1 小计)"
                && lines[7] == "【当前】"
                && lines[8] == "(+1 贝吉亚地区发展提速)"
                && lines[9] == "(-1 资本避税问题)"
                && lines[10] == "── (0 小计)",
                "行数 = " + lines.Length);

            SameInt("⑥-24 颜色标记成对闭合（标题行未破标签）",
                CountOf(out5, "<color=") == CountOf(out5, "</color>") ? 1 : 0, 1);
            Has("⑥-25 标题居中 + 带颜色标记（与段头同款灰，标签成对）",
                out5, "<align=center><b><color=" + InGameDetailFormat.ZeroColor + ">"
                    + InGameDetailFormat.PreviewTitle + "</color></b></align>");
            SameInt("⑥-26 标题居中标签整段只出现 1 对（不会把后面的行也居中了）",
                CountOf(out5, "<align=center>") * 10 + CountOf(out5, "</align>"), 11);
            SameInt("⑥-27 段间恰好 1 个空行", CountOf(out5, "\n\n"), 1);
        }

        
        Console.WriteLine("");
        Console.WriteLine("【⑦ 全 0 / 无内容：不显示空标题】");
        {
            var l = SteadyLedger(0);
            var s = new SettleState();
            s.Turn = 11;
            s.IndicatorTier = 2;                       
            s.TourismTier = 1;                         
            s.Tier[0] = 1;                             
            var parts = new StringBuilder();
            var es = NewEntries();
            int d = l.Preview(s, parts, es);

            SameInt("⑦-1 全 0 场景 delta = 0", d, 0);
            SameInt("⑦-2 全 0 场景无明细条目（零值项本就不入表）", es.Count, 0);

            string zero = InGameDetailFormat.Compose(es, d, true,
                InGameDetailFormat.PreviewTitle, null);
            Same("⑦-3 全 0 → 退化成「本回合无变动」（规范原样，未改）", zero,
                "<color=" + InGameDetailFormat.ZeroColor + ">" + InGameDetailFormat.NoChangeText + "</color>");
            NotHas("⑦-4 全 0 时不出现标题（无内容不显示空标题）", zero, InGameDetailFormat.PreviewTitle);
            Same("⑦-5 未介入哨兵：hasSettleData=false → 空串（放行游戏原生文本）",
                InGameDetailFormat.Compose(es, 0, false, InGameDetailFormat.PreviewTitle, null), "");

            string whyTotal = null;
            string onlyTotal = InGameDetailFormat.Compose(NewEntries(), 3, true,
                InGameDetailFormat.PreviewTitle, null);
            Has("⑦-6 无分项但 total≠0 → 标题仍在屏上（合计行已取消，故只剩标题）",
                onlyTotal, InGameDetailFormat.PreviewTitle);
            True("⑦-6b 该场景整段**不含合计行**（2.7.7 的 `(+3) Total:` 已消失）",
                !HasTotalLine(onlyTotal, out whyTotal), whyTotal ?? "");
            NotHas("⑦-6c 字面「Total:」不再出现", onlyTotal, "Total:");
            Has("⑦-7 同场景仍有标题", onlyTotal, InGameDetailFormat.PreviewTitle);
            Has("⑦-8 同场景标题下的分隔线仍在", onlyTotal, "──────");
        }

        
        Console.WriteLine("");
        Console.WriteLine("【⑧ 介入闸门 HasSignal（主菜单 / 载入中不接管原生 tooltip）】");
        {
            var blank = new SettleState();
            blank.IndicatorTier = -1;
            blank.TourismTier = -1;
            blank.TradeTier = -1;
            blank.Edge = new bool[7];
            True("⑧-1 全未知 + 全假 → 无信号（不接管）",
                !SettleMath.HasSignal(blank.ToInput(new Ledger(7))), "主菜单式状态");

            var s2 = new SettleState(); s2.IndicatorTier = 2;
            True("⑧-2 读到指示器档位（哪怕增量为 0）→ 有信号",
                SettleMath.HasSignal(s2.ToInput(new Ledger(0))), "在战役内");

            var s3 = new SettleState(); s3.IndicatorTier = -1; s3.TaxEfficient = true;
            True("⑧-3 读到税效率词条 → 有信号",
                SettleMath.HasSignal(s3.ToInput(new Ledger(0))), "税效率");

            var s4 = new SettleState(); s4.IndicatorTier = -1; s4.Edge = new bool[] { false, true };
            True("⑧-4 读到一次性词条为真 → 有信号",
                SettleMath.HasSignal(s4.ToInput(new Ledger(2))), "边沿词条");

            var s5 = new SettleState(); s5.IndicatorTier = -1; s5.Tier[2] = 1;
            True("⑧-5 读到地区档位 → 有信号",
                SettleMath.HasSignal(s5.ToInput(new Ledger(0))), "地区档位");

            var s6 = new SettleState(); s6.IndicatorTier = -1; s6.TradeTier = 1;
            True("⑧-6 读到贸易量档位 → 有信号",
                SettleMath.HasSignal(s6.ToInput(new Ledger(0))), "贸易量");

            var s7 = new SettleState(); s7.IndicatorTier = -1; s7.TourismTier = 1;
            True("⑧-7 读到旅游业档位 → 有信号",
                SettleMath.HasSignal(s7.ToInput(new Ledger(0))), "旅游业");
        }

        
        Console.WriteLine("");
        Console.WriteLine("【⑨ 两个 Signed 口径互不串味（日志 +0 / 显示 0）】");
        {
            Same("⑨-1 日志口径 SettleMath.Signed(0) = \"+0\"", SettleMath.Signed(0), "+0");
            Same("⑨-2 日志口径 SettleMath.Signed(1) = \"+1\"", SettleMath.Signed(1), "+1");
            Same("⑨-3 日志口径 SettleMath.Signed(-2) = \"-2\"", SettleMath.Signed(-2), "-2");
            Same("⑨-4 显示口径 InGameDetailFormat.Signed(0) = \"0\"", InGameDetailFormat.Signed(0), "0");
            Same("⑨-5 显示口径 InGameDetailFormat.Signed(3) = \"+3\"", InGameDetailFormat.Signed(3), "+3");

            var l = SteadyLedger(0);
            var s = new SettleState(); s.Turn = 11; s.IndicatorTier = 2;
            var parts = new StringBuilder(); var es = NewEntries();
            l.Preview(s, parts, es);
            Same("⑨-6 增量为 0 的每回合项在 parts 里写 (0)（旧形态逐字保留）",
                parts.ToString(), "ED=稳定(0)  ");
            SameInt("⑨-7 该场景无明细条目（0 值不进明细表）", es.Count, 0);
        }

        
        Console.WriteLine("");
        Console.WriteLine("【⑩ 类别归属：地区/避税/增长组/贸易协议 → 本回合；ED/旅游/贸易量/税效率/规则 → 每回合】");
        {
            var l = SteadyLedger(2);
            l.PrevTier[1] = 2;
            var s = new SettleState();
            s.Turn = 11;
            s.IndicatorTier = 3;
            s.TourismTier = 0;
            s.TradeTier = 0;
            s.TaxEfficient = true;
            s.TaxAvoid = true;
            s.Rules.Add(R("逃税", -1, null));
            s.Tier[1] = 0;                                          
            s.Edge = new bool[] { true, true };
            s.EdgeNames = new[] { "高就业", "韦伦·石油自由贸易" };
            s.EdgeIsTrade = new[] { false, true };

            var parts = new StringBuilder();
            var es = NewEntries();
            int d = l.Preview(s, parts, es);

            int perTurn = 0, oneTime = 0;
            foreach (var e in es) { if (e.Kind == InGameDetailFormat.KindOneTime) oneTime++; else perTurn++; }
            SameInt("⑩-1 每回合类条目数 = 5（ED/旅游/贸易量/税效率/逃税）", perTurn, 5);
            SameInt("⑩-2 本回合类条目数 = 4（地区/避税/增长组/贸易协议）", oneTime, 4);
            SameInt("⑩-3 明细之和 == delta", InGameDetailFormat.SumDeltas(es), d);
            SameInt("⑩-4 delta = 1+1+2+1-1-2-1+1+1 = 3", d, 3);

            string whyTotal = null;
            string rendered = InGameDetailFormat.Compose(es, d, true,
                InGameDetailFormat.PreviewTitle, null);
            True("⑩-5 该场景渲染整段**不含合计行**（2.7.8；2.7.7 这里是 `(+3) Total:`）",
                !HasTotalLine(rendered, out whyTotal), whyTotal ?? "");
            Has("⑩-6 格鲁尼倒退文案在屏上（2 档口径）", rendered, "格鲁尼地区发展大幅倒退");
            Has("⑩-7 贸易协议文案在屏上", rendered, "与韦伦达成石油自由贸易");
            Has("⑩-8 逃税文案在屏上（每回合类）", rendered, "逃税现象造成财政损失");
        }

        Console.WriteLine("");
        Console.WriteLine("═══ 结果：" + (fails == 0 ? (total + "/" + total + " 全部通过 ✓") : (fails + " 项失败 ✗（共 " + total + " 项）")) + " ═══");
        Environment.Exit(fails == 0 ? 0 : 1);
    }
}
