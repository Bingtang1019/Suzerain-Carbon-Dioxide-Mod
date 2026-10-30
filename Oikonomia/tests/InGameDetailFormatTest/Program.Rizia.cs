























using System;
using System.Collections.Generic;
using System.Globalization;
using Oikonomia;

partial class Program
{
    
    
    static string Rz(List<InGameDetailFormat.Entry> e, int total, bool hasSettle, string title, bool frame)
    {
        return InGameDetailFormat.Compose(e, total, hasSettle, title, null, frame, InGameDetailFormat.StyleRizia);
    }

    
    static string RzDetailed(List<InGameDetailFormat.Entry> e, int total, bool hasSettle, string title, bool frame)
    {
        return InGameDetailFormat.Compose(e, total, hasSettle, title, null, frame, InGameDetailFormat.StyleDetailed);
    }

    
    static string RzDetailed3(List<InGameDetailFormat.Entry> e, int total, bool hasSettle)
    {
        return InGameDetailFormat.Compose(e, total, hasSettle, null, null, false, InGameDetailFormat.StyleDetailed);
    }

    

    
    static List<string> RzDetailRows(string rendered)
    {
        var outs = new List<string>();
        if (string.IsNullOrEmpty(rendered)) return outs;
        foreach (var raw in rendered.Split('\n'))
        {
            if (raw.Length == 0) continue;
            string w;
            if (IsParenRow(raw, out w)) outs.Add(raw);
        }
        return outs;
    }

    
    static bool RzDetailedPair(string rawLine, out int value, out string label)
    {
        value = 0; label = null;
        string inner;
        if (!SplitColorLine(rawLine, out inner)) return false;
        if (inner.Length < 4 || inner[0] != '(' || inner[inner.Length - 1] != ')') return false;
        string body = inner.Substring(1, inner.Length - 2);
        int sp = body.IndexOf(' ');
        string v = sp < 0 ? body : body.Substring(0, sp);
        if (!IsSignedInt(v)) return false;
        int i = 0, sign = 1;
        if (v[0] == '+') i = 1;
        else if (v[0] == '-') { sign = -1; i = 1; }
        int mag;
        if (!int.TryParse(v.Substring(i), NumberStyles.Integer, CultureInfo.InvariantCulture, out mag)) return false;
        value = sign * mag;
        label = sp < 0 ? "" : body.Substring(sp + 1);
        return true;
    }

    
    
    static bool RzRiziaPair(string rawLine, out int value, out string label, out string hex)
    {
        value = 0; label = null; hex = null;
        var s = rawLine ?? "";
        if (s.EndsWith(InGameDetailFormat.IndentClose, StringComparison.Ordinal))
            s = s.Substring(0, s.Length - InGameDetailFormat.IndentClose.Length);
        if (RRHeadRe.IsMatch(s)) return false;              
        var m = RREntryRe.Match(s);
        if (!m.Success) return false;
        hex = "#" + m.Groups["hex"].Value.ToUpperInvariant();
        value = int.Parse(m.Groups["val"].Value, NumberStyles.Integer, CultureInfo.InvariantCulture);
        label = m.Groups["label"].Value;
        return true;
    }

    
    static bool RzHasFrame(string rendered)
    {
        if (string.IsNullOrEmpty(rendered)) return false;
        foreach (var raw in rendered.Split('\n'))
        {
            if (raw.Length == 0) continue;
            var v = FVisible(raw);
            if (FIsRule(v)) return true;
            if (v.IndexOf(InGameDetailFormat.FrameVertical) >= 0) return true;
        }
        return false;
    }

    
    static bool RzSamePairs(List<string> a, List<string> b, out string why)
    {
        why = null;
        var x = new List<string>(a); var y = new List<string>(b);
        x.Sort(StringComparer.Ordinal); y.Sort(StringComparer.Ordinal);
        if (x.Count != y.Count) { why = "条数不同：" + x.Count + " vs " + y.Count; return false; }
        for (int i = 0; i < x.Count; i++)
        {
            if (string.Equals(x[i], y[i], StringComparison.Ordinal)) continue;
            why = "第 " + (i + 1) + " 条不同：" + x[i] + " vs " + y[i];
            return false;
        }
        why = x.Count + " 条逐条相同（次序无关）";
        return true;
    }

    
    struct RzCase
    {
        public string Id;
        public List<InGameDetailFormat.Entry> Entries;
        public int Total;
        public string Title;
    }

    static RzCase RzCaseOf(string id, List<InGameDetailFormat.Entry> e, int total, string title)
    {
        var c = new RzCase();
        c.Id = id; c.Entries = e; c.Total = total; c.Title = title;
        return c;
    }

    
    
    
    
    static string RzTitlePrefix()
    {
        return "<align=center><b><color=#9A9A9A>当前预测下的财政状态</color></b></align>\n"
             + "<color=#9A9A9A>──────</color>\n";
    }

    
    static List<RzCase> RiziaCases()
    {
        var list = new List<RzCase>();
        string T = InGameDetailFormat.PreviewTitle;
        string W = InGameDetailFormat.GroupExtraWelfare;

        list.Add(RzCaseOf("R1 两段都有（两组都出）",
            L(E("ED=增长", 1), E("旅游业=蓬勃发展", 1),
              G1("免费处方药(每回合)", -1, InGameDetailFormat.KindPerTurn, W),
              O1("贝吉亚↑1档", 1), O1("避税(一次性)", -1)), 1, T));

        list.Add(RzCaseOf("R2 只有正数（单组）",
            L(P1("ED=增长", 2), P1("税效率", 3)), 5, T));

        list.Add(RzCaseOf("R3 只有负数（单组、负净额）",
            L(O1("贝吉亚↑1档", 4), O1("避税(一次性)", -5)), -1, T));

        list.Add(RzCaseOf("R4 无标题（3 参路 + Rizia）",
            L(P1("ED=增长", 1), O1("避税(一次性)", -1)), 0, null));

        list.Add(RzCaseOf("R5 长文案（最宽行）",
            L(P1("ED=增长", 1), P1("逃税", -1), O1("洛伦↓2档", -2)), -2, T));

        list.Add(RzCaseOf("R6 全 41 条同时上屏", FAll41(), InGameDetailFormat.SumDeltas(FAll41()), T));

        list.Add(RzCaseOf("R7 零值灰行 + 未知标签 + < > & 转义",
            L(P1("ED=稳定", 0), P1("某个没见过的词条", 2), P1("<B>C&D", -1)), 1, T));

        list.Add(RzCaseOf("R8 大数值（多位数不串位）",
            L(P1("ED=增长", 123), P1("逃税", -456), O1("贝吉亚↑2档", 78)), -255, T));

        list.Add(RzCaseOf("R9 全零（退化口径）",
            L(P1("ED=稳定", 0), P1("旅游业=平均水平", 0)), 0, T));

        return list;
    }

    
    static void RiziaSuite()
    {
        string T = InGameDetailFormat.PreviewTitle;

        
        Console.WriteLine("");
        Console.WriteLine("【⑭-A Rizia 形态逐字节黄金串（首行 Per Turn: + 正负两组 + 组头合计 + <indent=5%>）】");

        var rzSpec = L(
            E("ED=增长", 1), E("旅游业=蓬勃发展", 1),
            G1("免费处方药(每回合)", -1, InGameDetailFormat.KindPerTurn, InGameDetailFormat.GroupExtraWelfare),
            O1("贝吉亚↑1档", 1), O1("避税(一次性)", -1));
        
        string rzGolden = Rz(rzSpec, 1, true, T, true);

        Same("⑭-A1 目标形态逐字节黄金串（**标题行 + 分隔线 + 两块**；块头 未来/当前；块间一空行；只染数值）",
            rzGolden,
            "<align=center><b><color=#9A9A9A>当前预测下的财政状态</color></b></align>\n"
            + "<color=#9A9A9A>──────</color>\n"
            + "未来\n"
            + "<color=#0ED10E>(+2)</color> <u>Total:</u><indent=5%>\n"
            + "<color=#0ED10E>(+1)</color> 经济状态日益增长\n"
            + "<color=#0ED10E>(+1)</color> 全国旅游业蓬勃发展</indent>\n"
            + "<color=#D11111>(-1)</color> <u>Total:</u><indent=5%>\n"
            + "<color=#D11111>(-1)</color> 额外福利支出</indent>\n"
            + "\n"
            + "当前\n"
            + "<color=#0ED10E>(+1)</color> <u>Total:</u><indent=5%>\n"
            + "<color=#0ED10E>(+1)</color> 贝吉亚地区发展提速</indent>\n"
            + "<color=#D11111>(-1)</color> <u>Total:</u><indent=5%>\n"
            + "<color=#D11111>(-1)</color> 资本避税问题</indent>");

        
        string rzWhy;
        var rzLines = rzGolden.Split('\n');
        var rzBlocks = RRParseBlocks(rzGolden, out rzWhy);
        True("⑭-A1b 产物可解析成块 → 组 → 明细三层（标题前缀被正确识别）",
            rzWhy == null && rzBlocks.Count == 2, rzWhy ?? (rzBlocks.Count + " 块"));
        
        Same("⑭-A2a ★标题行逐字节 == 2.7.6 形态（居中 + 加粗 + 零灰；与 Detailed 平铺形态同源）",
            rzLines[0],
            "<align=center><b><color=#9A9A9A>" + T + "</color></b></align>");
        Same("⑭-A2b ★标题下分隔线逐字节 == <color=#9A9A9A>──────</color>（整段恰好一条）",
            rzLines[1], "<color=#9A9A9A>" + InGameDetailFormat.TotalDivider + "</color>");
        Same("⑭-A2c 标题文本 == 用户指定字面量「当前预测下的财政状态」", rzBlocks[0].Title, "当前预测下的财政状态");
        True("⑭-A2d 标题行在整段**最上方**（第 1 行），块头从第 3 行开始",
            rzLines[0].StartsWith("<align=center>", StringComparison.Ordinal) && rzLines[2] == "未来",
            "行1 = " + rzLines[0].Substring(0, 20) + "…｜行3 = " + rzLines[2]);
        Same("⑭-A2e ★块①块头（第 3 行）== 常量 == 用户指定字面量「未来」（纯文本，无标签）",
            rzLines[2] + "|" + InGameDetailFormat.RiziaPerTurnLabel, "未来|未来");
        SameInt("⑭-A2f ★块②块头「当前」按**整行相等**恰好出现 1 次（标题里的「当前」二字不算）",
            RRCountHeadLines(rzGolden, InGameDetailFormat.RiziaThisTurnLabel), 1);
        SameInt("⑭-A2g ②块头总数 = 2（两块各一次，按整行相等计）",
            RRCountHeadLines(rzGolden, InGameDetailFormat.RiziaPerTurnLabel)
            + RRCountHeadLines(rzGolden, InGameDetailFormat.RiziaThisTurnLabel), 2);
        True("⑭-A3 全串末尾**无换行**（真机以 </indent> 收尾）",
            !rzGolden.EndsWith("\n", StringComparison.Ordinal)
            && rzGolden.EndsWith(InGameDetailFormat.IndentClose, StringComparison.Ordinal),
            "末 10 字符 = " + rzGolden.Substring(rzGolden.Length - 10));
        SameInt("⑭-A4 组头行数 = 4（两块各出正负两组）",
            CountOf(rzGolden, "<u>Total:</u>"), 4);
        SameInt("⑭-A5 <indent=5%> 与 </indent> 各 4 个（每组一对，闭合）",
            CountOf(rzGolden, InGameDetailFormat.IndentOpen) * 10 + CountOf(rzGolden, InGameDetailFormat.IndentClose),
            4 * 10 + 4);
        True("⑭-A5b ④indent 成对且数量**恰好等于组数**（4）", RRIndentPairsOk(rzGolden, rzBlocks, out rzWhy), rzWhy ?? "");
        SameInt("⑭-A6 组尾 </indent> 独占一行的数量 = 0（真机贴在组内最后一条明细行尾）",
            RRIndentAloneLines(rzGolden), 0);
        SameInt("⑭-A7 整段行数 = 标题 2 + 2 块头 + 4 组头 + 5 明细 + 1 块间空行 = 14",
            rzLines.Length, 14);
        True("⑭-A7b 行数守恒（标题前缀 + 块头 + 组头 + 明细 + 块间空行 == 总行数）",
            RRLineCountOk(rzGolden, rzBlocks, out rzWhy), rzWhy ?? "");
        True("⑭-A8 每条明细行都过严格字节判据（色标签只包数值 + 说明零标签）",
            RREntryStrictAll(rzGolden, out rzWhy) && RRLabelsTagFree(rzGolden, out rzWhy), rzWhy ?? "全部合规");
        True("⑭-A8b ④**块内先正数组、后负数组**（两块各自成立）",
            rzBlocks[0].Groups[0].Subtotal > 0 && rzBlocks[0].Groups[1].Subtotal < 0
            && rzBlocks[1].Groups[0].Subtotal > 0 && rzBlocks[1].Groups[1].Subtotal < 0,
            "块① " + rzBlocks[0].Groups[0].Subtotal + " → " + rzBlocks[0].Groups[1].Subtotal
            + "｜块② " + rzBlocks[1].Groups[0].Subtotal + " → " + rzBlocks[1].Groups[1].Subtotal);
        SameInt("⑭-A9 正数组在前（块① 第 1 组是正组；块② 第 1 组也是正组）",
            (rzBlocks[0].Groups[0].Subtotal > 0 ? 1 : 0) * 10 + (rzBlocks[1].Groups[0].Subtotal > 0 ? 1 : 0), 1 * 10 + 1);

        
        SameInt("⑭-A10 ★标题出现且**恰好一次**（用户第 1 项要求：恢复标题行）",
            CountOf(rzGolden, T), 1);
        NotHas("⑭-A11 无段头【未来】（新块头是裸「未来」，不带方括号 —— 与 Detailed 的【未来】不同）",
            rzGolden, "【未来】");
        NotHas("⑭-A12 无段头【当前】", rzGolden, "【当前】");
        SameInt("⑭-A13 ★分隔线**恰好一条**（= 标题下那一条；2.9.1 的 Rizia 形态一条都没有）",
            CountOf(rzGolden, InGameDetailFormat.TotalDivider), 1);
        SameInt("⑭-A14 无小计行（「小计」计数 = 0）", CountOf(rzGolden, "小计"), 0);
        SameInt("⑭-A15 「──」只出现在标题分隔线里（含 ── 的行数 == 1，且那一行就是分隔线）",
            CountOfLinesWith(rzGolden, "──"), 1);
        SameInt("⑭-A16 ★加粗 <b> 只属标题行（含 <b> 的行数 == 1；明细/组头不带加粗）",
            CountOfLinesWith(rzGolden, "<b>"), 1);
        SameInt("⑭-A17 ★居中 <align 只属标题行（含 <align 的行数 == 1，且成对）",
            CountOfLinesWith(rzGolden, "<align"), 1);
        NotHas("⑭-A18 无框线字符 |", rzGolden, InGameDetailFormat.FrameVertical.ToString());
        True("⑭-A19 整段没有任何框线行（横线 / 竖线都没有；标题分隔线不算框线 —— 它是 ────── 无 + 角）",
            !RzHasFrame(rzGolden), "框线行 = " + (RzHasFrame(rzGolden) ? "有" : "无"));
        SameInt("⑭-A20 空行数 = 1（恰好是两块之间那一个；块内组间没有空行）", CountOf(rzGolden, "\n\n"), 1);
        True("⑭-A20b ②空行数 == 块数 - 1", RRBlankLinesOk(rzGolden, rzBlocks, out rzWhy), rzWhy ?? "");
        
        List<string> rzPtL, rzOtL;
        RRLabelSets(rzSpec, out rzPtL, out rzOtL);
        SameInt("⑭-A20c 场景里两类条数 = 每回合 3 / 一次性 2",
            rzPtL.Count * 10 + rzOtL.Count, 3 * 10 + 2);
        True("⑭-A20d ★C1 分离性：Per Turn: 块里零一次性类、This Turn: 块里零每回合类",
            RRSeparation(rzBlocks, rzPtL, rzOtL, out rzWhy), rzWhy ?? "");
        Same("⑭-A20e ★逐组守恒：每个组头小计 == 该组各条之和（且组内单一符号、取色相符）",
            RRGroupSubtotalsOk(rzBlocks, out rzWhy) ? "OK" : ("违规：" + rzWhy), "OK");
        True("⑭-A21 不靠补空格对齐（连续两个空格 = 0，金额天然在行首括号里）",
            !AnyDoubleSpace(rzGolden), "连续两个空格 = " + (AnyDoubleSpace(rzGolden) ? "有" : "无"));

        
        
        
        string rzGuardDetailed = RzDetailed(rzSpec, 1, true, T, false);
        string rzGuardFramed = RzDetailed(rzSpec, 1, true, T, true);
        Has("⑭-A22 守门：同一份内容在 Detailed 形态下**有**标题", rzGuardDetailed, T);
        Has("⑭-A23 守门：Detailed 形态下**有**【未来】段头", rzGuardDetailed, "【未来】");
        Has("⑭-A24 守门：Detailed 形态下**有**分隔线", rzGuardDetailed, InGameDetailFormat.TotalDivider);
        Has("⑭-A25 守门：Detailed 形态下**有**小计行", rzGuardDetailed, "<u>小计</u>");
        NotHas("⑭-A26 守门：Detailed 形态下**没有** <indent> 标签（该标签只属 Rizia 形态）",
            rzGuardDetailed, InGameDetailFormat.IndentOpen);
        True("⑭-A27 守门：同一份内容在 Detailed + frame=true 下**会**画框（文本框开关真的起作用）",
            RzHasFrame(rzGuardFramed),
            "Detailed + frame=true 有框线 = " + RzHasFrame(rzGuardFramed));

        
        Console.WriteLine("");
        Console.WriteLine("【⑭-B 41 条文案 · 归并 · 分组 · 取色 一字不差】");

        var all = FAll41();
        int allTotal = InGameDetailFormat.SumDeltas(all);
        string rzAll = Rz(all, allTotal, true, T, true);
        
        
        string deAll = RzDetailed(all, allTotal, true, T, false);

        var dePairs = new List<string>();
        var deZeroLabels = new List<string>();
        foreach (var raw in RzDetailRows(deAll))
        {
            int v; string lb;
            if (!RzDetailedPair(raw, out v, out lb)) continue;
            if (v == 0) deZeroLabels.Add(lb);
            else dePairs.Add(v + "|" + lb);
        }
        var rzAllParsed = RRParse(rzAll, out rzWhy);
        True("⑭-B1 41 条场景的 Rizia 产物可逐行解析（首行 / 组头 / 明细）",
            rzWhy == null && rzAllParsed.Count > 0, rzWhy ?? (rzAllParsed.Count + " 行"));
        var rzPairs = new List<string>();
        for (int i = 0; i < rzAllParsed.Count; i++)
            if (rzAllParsed[i].Kind == 2) rzPairs.Add(rzAllParsed[i].Value + "|" + rzAllParsed[i].Label);

        SameInt("⑭-B2 Detailed 明细行 = 41（含 3 条零值行）", dePairs.Count + deZeroLabels.Count, 41);
        SameInt("⑭-B3 Detailed 的零值行恰 3 条（Rizia 形态把它们剔除，口径见 CHANGELOG）", deZeroLabels.Count, 3);
        SameInt("⑭-B4 Rizia 明细行 = 38（41 条减去 3 条零值）", rzPairs.Count, 38);
        True("⑭-B5 ★ 两种形态的明细**文案集逐条相同**（Rizia 的 38 条 == Detailed 的 41 条去掉 3 条零值行）",
            RzSamePairs(rzPairs, dePairs, out rzWhy), rzWhy ?? "");

        
        string rzPlain = Plain(rzAll);
        var zeroTexts = new List<string> { "经济状态保持稳定", "全国旅游业平稳发展", "对外贸易额出现萎缩" };
        int missing = 0; string firstMissing = null;
        for (int i = 0; i < FAll41Texts.Length; i++)
        {
            bool isZero = zeroTexts.Contains(FAll41Texts[i]);
            bool present = rzPlain.IndexOf(FAll41Texts[i], StringComparison.Ordinal) >= 0;
            if (isZero ? !present : present) continue;
            missing++;
            if (firstMissing == null) firstMissing = FAll41Texts[i];
        }
        SameInt("⑭-B6 38 条非零文案逐条上屏、3 条零值文案逐条缺席（一条不差）", missing, 0);
        True("⑭-B7 守门：文案表本身是 41 条（不是把断言写空了）", FAll41Texts.Length == 41,
            firstMissing == null ? "41 条全部判过" : ("首条不符 = " + firstMissing));
        SameInt("⑭-B8 Rizia 明细文案逐条唯一（无重复、无串行）", RREachLabelOnce(rzAllParsed, out rzWhy), 0);

        
        True("⑭-B9 正数组内按数值降序", RRDescending(rzAllParsed, true, out rzWhy), rzWhy ?? "");
        True("⑭-B10 负数组内按绝对值降序（真机样本 -11,-6,-4,…）", RRDescending(rzAllParsed, false, out rzWhy), rzWhy ?? "");

        
        int colorBad = 0; string colorWhy = null;
        for (int i = 0; i < rzAllParsed.Count; i++)
        {
            if (rzAllParsed[i].Kind != 2) continue;
            string want = rzAllParsed[i].Value > 0 ? "#0ED10E" : "#D11111";
            if (string.Equals(rzAllParsed[i].Hex, want, StringComparison.Ordinal)) continue;
            colorBad++;
            if (colorWhy == null) colorWhy = rzAllParsed[i].Label + " 取色 = " + rzAllParsed[i].Hex + "，应为 " + want;
        }
        SameInt("⑭-B11 每条明细的取色 == 真机色对（正 #0ED10E / 负 #D11111）", colorBad, 0);
        True("⑭-B12 守门：取色判据不是空断言（38 条都真的读出了值）", colorWhy == null, colorWhy ?? "38 条全部合规");
        NotHas("⑭-B13 2.8.0 的旧色值一个都不剩", rzAll, "#6FBF52");
        NotHas("⑭-B14 2.8.0 的旧色值（负）一个都不剩", rzAll, "#D9534F");

        
        SameInt("⑭-B15 组归并仍成立：额外福利支出只出现 1 行", CountOfLinesWith(rzAll, "额外福利支出"), 1);
        NotHas("⑭-B16 组内原始标签不出现在屏上", rzAll, "免费处方药");
        Has("⑭-B17 地区多档文案一字不差（大幅倒退（2 档），在负数组）", rzAll,
            "<color=#D11111>(-2)</color> 洛伦地区发展大幅倒退（2 档）");
        Has("⑭-B18 贸易协议文案一字不差", rzAll,
            "<color=#0ED10E>(+1)</color> 与莱斯皮亚达成煤炭贸易");
        
        string rzEscape = Rz(L(P1("ED=稳定", 0), P1("某个没见过的词条", 2), P1("<B>C&D", -1)), 1, true, T, false);
        Has("⑭-B19 未知标签原样回退（某个没见过的词条）", rzEscape,
            "<color=#0ED10E>(+2)</color> 某个没见过的词条");
        Has("⑭-B20 转义仍成立（<B>C&D → &lt;B&gt;C&amp;D，且只在说明段）", rzEscape,
            "<color=#D11111>(-1)</color> &lt;B&gt;C&amp;D");
        True("⑭-B20b 含转义说明的那一段仍过严格字节判据（说明排在 </color> 之后，未被染色）",
            RREntryStrictAll(rzEscape, out rzWhy), rzWhy ?? "");

        
        var rzAllBlocks = RRParseBlocks(rzAll, out rzWhy);
        int headSum = 0, groupN = 0;
        for (int b = 0; b < rzAllBlocks.Count; b++)
        {
            groupN += rzAllBlocks[b].Groups.Count;
            for (int gi = 0; gi < rzAllBlocks[b].Groups.Count; gi++)
                headSum += rzAllBlocks[b].Groups[gi].Subtotal;
        }
        True("⑭-B21 ★逐组：组头小计 == 该组各条之和（每块每组都判；组内单一符号、取色与符号相符）",
            RRGroupSubtotalsOk(rzAllBlocks, out rzWhy), rzWhy ?? "");
        SameInt("⑭-B22 组数 = 4（两块各有正负两组）", groupN, 4);
        SameInt("⑭-B23 所有组头小计之和 == 41 条净额（零值剔除不改变结算口径）", headSum, allTotal);
        True("⑭-B24 行数守恒（块头 + 组头 + 明细 + 块间空行 == 总行数）",
            RRLineCountOk(rzAll, rzAllBlocks, out rzWhy), rzWhy ?? "");
        True("⑭-B24b 41 条实景的两块结构（块头各一次、顺序正确、每块都有组）",
            RRBlockHeadsOk(rzAllBlocks, out rzWhy), rzWhy ?? "");
        List<string> pt41, ot41;
        RRLabelSets(all, out pt41, out ot41);
        True("⑭-B24c ★C1 分离性（41 条）：Per Turn: 块零一次性类、This Turn: 块零每回合类",
            RRSeparation(rzAllBlocks, pt41, ot41, out rzWhy), rzWhy ?? "");

        
        Console.WriteLine("");
        Console.WriteLine("【⑭-C Detailed 形态与 2.7.8 逐字节相同（新 7 参重载直对 baseline-278.txt）】");

        string basePath = System.IO.Path.Combine(AppContext.BaseDirectory, "baseline-278.txt");
        True("⑭-C0 2.7.8 基线夹具在输出目录里", System.IO.File.Exists(basePath), basePath);

        string fixtureSha = null;
        if (System.IO.File.Exists(basePath))
        {
            using (var fs = System.IO.File.OpenRead(basePath))
            using (var sha256 = System.Security.Cryptography.SHA256.Create())
                fixtureSha = BitConverter.ToString(sha256.ComputeHash(fs)).Replace("-", "");
        }
        Same("⑭-C1 基线夹具未被本版改动（SHA256 仍是入库时那一个）", fixtureSha,
            "FFC567D500C208BB8A2E37308D78991113589E4BB23662F653533E795D28A293");

        var baseGolden = new Dictionary<string, string>(StringComparer.Ordinal);
        if (System.IO.File.Exists(basePath))
        {
            foreach (var ln in System.IO.File.ReadAllLines(basePath))
            {
                if (ln.Length == 0 || ln[0] != '[') continue;
                int rb = ln.IndexOf(']');
                if (rb < 0) continue;
                int sep = ln.IndexOf(" :: ", StringComparison.Ordinal);
                if (sep < 0) continue;
                baseGolden[ln.Substring(1, rb - 1)] = ln.Substring(sep + 4);
            }
        }
        SameInt("⑭-C2 基线里读到 18 个场景", baseGolden.Count, 18);

        var viaStyle = BaselineScenesViaStyleDetailed();
        SameInt("⑭-C3 本测试复现的基线场景数", viaStyle.Count, 18);
        int baseBad = 0; string baseWhy = null;
        foreach (var kv in viaStyle)
        {
            string exp;
            bool has = baseGolden.TryGetValue(kv.Key, out exp);
            bool same = has && string.Equals(kv.Value, exp, StringComparison.Ordinal);
            if (same) continue;
            baseBad++;
            if (baseWhy == null)
            {
                if (!has) baseWhy = kv.Key + " 基线里没有这个场景";
                else
                {
                    int d = 0, m = Math.Min(kv.Value.Length, exp.Length);
                    while (d < m && kv.Value[d] == exp[d]) d++;
                    baseWhy = kv.Key + " 首个差异 @" + d + "：实际" + FCode(kv.Value, d) + " vs 基线" + FCode(exp, d);
                }
            }
        }
        SameInt("⑭-C4 **新 7 参重载 + StyleDetailed** 的 18 个场景 == 2.7.8 基线逐字节（差异数 = 0）", baseBad, 0);
        True("⑭-C5 守门：比对器不是空跑（18 个场景都真的比过、且基线串非空）",
            viaStyle.Count == 18 && baseGolden.Count == 18,
            baseWhy ?? "18/18 逐字节相同");

        
        var e0 = L(P1("ED=增长", 1), O1("避税(一次性)", -1));
        Same("⑭-C6 3 参重载 == 7 参 + StyleDetailed（无标题）",
            InGameDetailFormat.Compose(e0, 0, true),
            RzDetailed3(e0, 0, true));
        Same("⑭-C7 5 参重载 == 7 参 + StyleDetailed（带标题）",
            InGameDetailFormat.Compose(e0, 0, true, T, null),
            RzDetailed(e0, 0, true, T, false));
        Same("⑭-C8 6 参重载（frame=true）== 7 参 + StyleDetailed + frame=true",
            InGameDetailFormat.Compose(e0, 0, true, T, null, true),
            RzDetailed(e0, 0, true, T, true));
        Same("⑭-C9 6 参重载（frame=false）== 7 参 + StyleDetailed + frame=false",
            InGameDetailFormat.Compose(e0, 0, true, T, null, false),
            RzDetailed(e0, 0, true, T, false));

        
        Console.WriteLine("");
        Console.WriteLine("【⑭-D Rizia 形态下 InGameDetailFrame=true 不产生框（开关在本形态无效）】");

        var cases = RiziaCases();
        int frameDiff = 0; string frameWhy = null;
        int framedBad = 0; string framedWhy = null;
        for (int i = 0; i < cases.Count; i++)
        {
            var c = cases[i];
            string off = Rz(c.Entries, c.Total, true, c.Title, false);
            string on = Rz(c.Entries, c.Total, true, c.Title, true);

            Same("⑭-D" + (i + 1) + "a " + c.Id + "：frame 开/关逐字节相同（文本框开关在 Rizia 形态下无效）", on, off);
            if (!string.Equals(on, off, StringComparison.Ordinal))
            {
                frameDiff++;
                if (frameWhy == null) frameWhy = c.Id;
            }
            if (RzHasFrame(on))
            {
                framedBad++;
                if (framedWhy == null) framedWhy = c.Id + "：产物里出现了框线";
            }
        }
        SameInt("⑭-D10 " + cases.Count + " 个场景里「开关真的无效」计数 == " + cases.Count, frameDiff, 0);
        SameInt("⑭-D11 " + cases.Count + " 个场景的 Rizia 产物里都**没有任何框线**", framedBad, 0);
        True("⑭-D12 守门：同一批场景在 Detailed 下 frame 开关**确实起作用**（开≠关）",
            RzDetailed(L(P1("ED=增长", 1), O1("避税(一次性)", -1)), 0, true, T, true)
            != RzDetailed(L(P1("ED=增长", 1), O1("避税(一次性)", -1)), 0, true, T, false),
            "Detailed：开 = " + RzDetailed(L(P1("ED=增长", 1), O1("避税(一次性)", -1)), 0, true, T, true).Length
            + " 字符 vs 关 = " + RzDetailed(L(P1("ED=增长", 1), O1("避税(一次性)", -1)), 0, true, T, false).Length + " 字符");

        
        Console.WriteLine("");
        Console.WriteLine("【⑭-E 富文本卫生：零可见标记 / 标签成对 / <indent> 成对 / 幂等 20 轮】");

        string gwhy;
        for (int i = 0; i < cases.Count; i++)
        {
            var c = cases[i];
            string s = Rz(c.Entries, c.Total, true, c.Title, true);
            True("⑭-E" + (i + 1) + "a " + c.Id + "：TMP 标签全部成对闭合（含 <indent=5%>/</indent>）",
                InGameDetailFormat.RichTagsClosed(s, out gwhy), gwhy ?? "全部闭合");
            True("⑭-E" + (i + 1) + "b " + c.Id + "：整段零可见标记字符",
                InGameDetailFormat.IsMarkerFree(s, out gwhy), gwhy ?? "零标记");
            SameInt("⑭-E" + (i + 1) + "c " + c.Id + "：<color> 开闭数量相等",
                CountOf(s, "<color=") - CountOf(s, "</color>"), 0);
            SameInt("⑭-E" + (i + 1) + "d " + c.Id + "：不引入字号/字体/透明度标签（HUD 适配不受影响）",
                CountOf(s, "<size") + CountOf(s, "<font") + CountOf(s, "<alpha"), 0);
            SameInt("⑭-E" + (i + 1) + "e " + c.Id + "：<indent=5%> 与 </indent> 数量相等（每组一对）",
                CountOf(s, InGameDetailFormat.IndentOpen) - CountOf(s, InGameDetailFormat.IndentClose), 0);
            SameInt("⑭-E" + (i + 1) + "f " + c.Id + "：<u>Total:</u> 的数量 == 组数（1 或 2，且 == indent 对数）",
                CountOf(s, "<u>Total:</u>") - CountOf(s, InGameDetailFormat.IndentOpen), 0);
        }

        const string gameTip = "政府预算 <alpha=#44><size=16>(min: -20, max: +50)";
        var memR = new InGameDetailFormat.DetailMergeMemory();
        object slotR = new object();
        bool hitR;
        string r0 = memR.Merge(slotR, gameTip, rzGolden, out hitR);
        string cursorR = r0;
        int sameR = 0;
        for (int k = 0; k < 20; k++)
        {
            bool h;
            cursorR = memR.Merge(slotR, cursorR, rzGolden, out h);
            if (string.Equals(cursorR, r0, StringComparison.Ordinal)) sameR++;
        }
        SameInt("⑭-E20 Rizia 产物连续 20 轮合并逐字节相同（幂等仍成立）", sameR, 20);
        Same("⑭-E21 20 轮后游戏原文仍是逐字节前缀", cursorR.Substring(0, gameTip.Length), gameTip);
        SameInt("⑭-E22 20 轮后块①块头「未来」仍只出现 1 次（按**整行相等**计）",
            RRCountHeadLines(cursorR, InGameDetailFormat.RiziaPerTurnLabel), 1);
        SameInt("⑭-E22b 20 轮后块②块头「当前」仍只出现 1 次（标题里的「当前」二字不算 —— 按整行相等计）",
            RRCountHeadLines(cursorR, InGameDetailFormat.RiziaThisTurnLabel), 1);
        SameInt("⑭-E23 20 轮后组尾 </indent> 仍是 4 个（两块各两组；不膨胀、不累积）",
            CountOf(cursorR, InGameDetailFormat.IndentClose), 4);
        True("⑭-E23b 20 轮后仍可解析成两块四组、且逐组守恒（只取合并串里属于我们的那一段）",
            RRGroupSubtotalsOk(RRParseBlocks(
                cursorR.Substring(cursorR.IndexOf(InGameDetailFormat.RiziaPerTurnLabel, StringComparison.Ordinal)), out gwhy),
                out gwhy), gwhy ?? "");
        SameInt("⑭-E24 20 轮后明细行仍是 5 条（不膨胀、不累积）", RzRiziaRowsOf(cursorR), 5);
        True("⑭-E25 20 轮后整段仍零可见标记",
            !InGameDetailFormat.ContainsVisibleMarker(cursorR, out gwhy), gwhy ?? "零标记");

        
        Console.WriteLine("");
        Console.WriteLine("【⑭-F ParseStyle 纯函数（「默认 = Rizia」在解析层就成立）】");

        SameInt("⑭-F1 形态常量：StyleRizia = 0 / StyleDetailed = 1（兜底值就是默认值）",
            InGameDetailFormat.StyleRizia * 10 + InGameDetailFormat.StyleDetailed, 0 * 10 + 1);
        SameInt("⑭-F2 ParseStyle(\"Rizia\") → Rizia",
            InGameDetailFormat.ParseStyle("Rizia"), InGameDetailFormat.StyleRizia);
        SameInt("⑭-F3 ParseStyle(\"Detailed\") → Detailed",
            InGameDetailFormat.ParseStyle("Detailed"), InGameDetailFormat.StyleDetailed);
        SameInt("⑭-F4 大小写不敏感：\"detailed\" / \"DETAILED\" / \"DeTaIlEd\" → Detailed",
            InGameDetailFormat.ParseStyle("detailed") * 100 + InGameDetailFormat.ParseStyle("DETAILED") * 10
            + InGameDetailFormat.ParseStyle("DeTaIlEd"), 1 * 100 + 1 * 10 + 1);
        SameInt("⑭-F5 两端空白忽略：\" Detailed \" → Detailed",
            InGameDetailFormat.ParseStyle("  Detailed  "), InGameDetailFormat.StyleDetailed);
        SameInt("⑭-F6 null → Rizia（默认形态）",
            InGameDetailFormat.ParseStyle(null), InGameDetailFormat.StyleRizia);
        SameInt("⑭-F7 空串 → Rizia", InGameDetailFormat.ParseStyle(""), InGameDetailFormat.StyleRizia);
        SameInt("⑭-F8 拼错的取值 → Rizia（绝不意外退回带框形态）",
            InGameDetailFormat.ParseStyle("Detail") * 100 + InGameDetailFormat.ParseStyle("detailed2") * 10
            + InGameDetailFormat.ParseStyle("Rizia2"), 0);
        Same("⑭-F9 配置取值常量逐字 == Rizia / Detailed",
            InGameDetailFormat.StyleNameRizia + "|" + InGameDetailFormat.StyleNameDetailed, "Rizia|Detailed");

        
        Console.WriteLine("");
        Console.WriteLine("【⑭-G 边界：未结算哨兵 / 全 0 退化 / 无分项但净额非 0】");

        Same("⑭-G1 未结算（无记录）→ 空串哨兵（Rizia 与 Detailed 相同，不接管游戏文本）",
            Rz(L(E("ED=增长", 1)), 1, false, T, true), "");
        Same("⑭-G2 未结算 + 明细为 null → 空串哨兵",
            Rz(null, 0, false, T, true), "");
        Same("⑭-G3 全 0 退化（无分项 + 净额 0）→ 单行「本回合无变动」",
            Rz(L(), 0, true, T, true), Tag("Z", InGameDetailFormat.NoChangeText));
        Same("⑭-G4 全 0 退化：Rizia 与 Detailed 逐字节相同（这一支两种形态共用）",
            Rz(L(), 0, true, T, true),
            RzDetailed(L(), 0, true, T, true));
        NotHas("⑭-G5 全 0 退化里不出现 Total:", Rz(L(), 0, true, T, true), InGameDetailFormat.TotalLabel);
        NotHas("⑭-G5b 全 0 退化里也不出现 Per Turn:（不吐光秃秃的首行）",
            Rz(L(), 0, true, T, true), InGameDetailFormat.RiziaPerTurnLabel);

        
        string rzGroupZero = Rz(L(
            G1("免费处方药(每回合)", -1, InGameDetailFormat.KindPerTurn, InGameDetailFormat.GroupExtraWelfare),
            G1("免费校餐(每回合)", 1, InGameDetailFormat.KindPerTurn, InGameDetailFormat.GroupExtraWelfare)),
            0, true, T, true);
        Same("⑭-G6 归并后一条可见行都没有 + 净额 0 → 仍是「本回合无变动」（Rizia 同样如此）",
            rzGroupZero, Tag("Z", InGameDetailFormat.NoChangeText));

        
        Same("⑭-G7 无分项但净额 +5 → 退化成「本回合无变动」（不吐孤立的 Per Turn:）",
            Rz(L(), 5, true, T, true), Tag("Z", InGameDetailFormat.NoChangeText));
        
        
        Same("⑭-G8 同一输入在 Detailed（无标题）形态下什么都不写（2.7.8 的退化行为，未被动过）",
            RzDetailed3(L(), 5, true), "");
        Same("⑭-G8b 对照：既有 3 参重载在同一输入下同样什么都不写",
            InGameDetailFormat.Compose(L(), 5, true), "");

        
        Same("⑭-G9 分项全为 0 → 退化（零值不进任何一组）",
            Rz(L(P1("ED=稳定", 0), P1("旅游业=平均水平", 0)), 0, true, T, true),
            Tag("Z", InGameDetailFormat.NoChangeText));

        
        Same("⑭-G10 只有负数（且是**一次性类**）→ 标题 + 只有块②（「当前」）、单个负数组",
            Rz(L(O1("避税(一次性)", -3)), -3, true, T, true),
            RzTitlePrefix()
            + "当前\n"
            + "<color=#D11111>(-3)</color> <u>Total:</u><indent=5%>\n"
            + "<color=#D11111>(-3)</color> 资本避税问题</indent>");
        Same("⑭-G10b 只有负数（**每回合类**）→ 标题 + 只有块①（「未来」）、单个负数组",
            Rz(L(P1("逃税", -3)), -3, true, T, true),
            RzTitlePrefix()
            + "未来\n"
            + "<color=#D11111>(-3)</color> <u>Total:</u><indent=5%>\n"
            + "<color=#D11111>(-3)</color> 逃税现象造成财政损失</indent>");
        Same("⑭-G11 净额为 0（一正一负 + **一每回合一一次性**）→ 两块各自的单组，各写 ±N（真机形态不写 0 合计行）",
            Rz(L(P1("ED=增长", 1), O1("避税(一次性)", -1)), 0, true, T, true),
            RzTitlePrefix()
            + "未来\n"
            + "<color=#0ED10E>(+1)</color> <u>Total:</u><indent=5%>\n"
            + "<color=#0ED10E>(+1)</color> 经济状态日益增长</indent>\n"
            + "\n"
            + "当前\n"
            + "<color=#D11111>(-1)</color> <u>Total:</u><indent=5%>\n"
            + "<color=#D11111>(-1)</color> 资本避税问题</indent>");
        NotHas("⑭-G12 组头里不写 +0 / 也不写 (0)",
            Rz(L(P1("ED=增长", 1), O1("避税(一次性)", -1)), 0, true, T, true), "+0");
        SameInt("⑭-G12b 组头里 (0) 形态计数 = 0",
            CountOf(Rz(L(P1("ED=增长", 1), O1("避税(一次性)", -1)), 0, true, T, true), "(0)"), 0);
        Same("⑭-G13 ★无标题（title = null）⇒ 产物 == 带标题产物**去掉那两行前缀**（标题只由 title 参数驱动，块结构一字不变）",
            Rz(rzSpec, 1, true, null, true), rzGolden.Substring(RzTitlePrefix().Length));
    }

    
    static int RzRiziaRowsOf(string rendered)
    {
        int n = 0;
        if (string.IsNullOrEmpty(rendered)) return 0;
        foreach (var raw in rendered.Split('\n'))
        {
            if (raw.Length == 0) continue;
            int v; string lb, hx;
            if (RzRiziaPair(raw, out v, out lb, out hx)) n++;
        }
        return n;
    }

    
    static int RRSumOf(List<RRLine> lines, bool positive)
    {
        int s = 0;
        for (int i = 0; i < lines.Count; i++)
        {
            if (lines[i].Kind != 2) continue;
            if ((lines[i].Value > 0) == positive) s += lines[i].Value;
        }
        return s;
    }

    
    
    
    
    
    static List<KeyValuePair<string, string>> BaselineScenesViaStyleDetailed()
    {
        var outs = new List<KeyValuePair<string, string>>();
        string T = InGameDetailFormat.PreviewTitle;
        string W = InGameDetailFormat.GroupExtraWelfare;

        outs.Add(new KeyValuePair<string, string>("S01", FEsc(RzDetailed3(
            L(P1("ED=增长", 1), P1("旅游业=蓬勃发展", 1), G1("免费处方药(每回合)", -1, 0, W),
              O1("贝吉亚↑1档", 1), O1("避税(一次性)", -1)), 1, true))));

        outs.Add(new KeyValuePair<string, string>("S02", FEsc(RzDetailed(
            L(P1("ED=增长", 1), P1("旅游业=蓬勃发展", 1), G1("免费处方药(每回合)", -1, 0, W),
              O1("贝吉亚↑1档", 1), O1("避税(一次性)", -1)), 1, true, T, false))));

        outs.Add(new KeyValuePair<string, string>("S03", FEsc(RzDetailed(
            L(P1("ED=增长", 1), P1("旅游业=蓬勃发展", -2),
              G1("免费处方药(每回合)", 3, 0, W)), 1, true, T, false))));

        outs.Add(new KeyValuePair<string, string>("S04", FEsc(RzDetailed(
            L(O1("贝吉亚↑1档", 4), O1("避税(一次性)", -5)), -1, true, T, false))));

        outs.Add(new KeyValuePair<string, string>("S05", FEsc(RzDetailed(
            FAll41(), 12, true, T, false))));

        outs.Add(new KeyValuePair<string, string>("S06", FEsc(RzDetailed(
            L(P1("ED=稳定", 0)), 0, true, T, false))));

        outs.Add(new KeyValuePair<string, string>("S07", FEsc(RzDetailed3(L(), 0, true))));

        outs.Add(new KeyValuePair<string, string>("S08", FEsc(InGameDetailFormat.Compose(
            FAll41(), 12, false, T, null, false, InGameDetailFormat.StyleDetailed))));

        outs.Add(new KeyValuePair<string, string>("S09", FEsc(RzDetailed(
            L(), 5, true, T, false))));

        outs.Add(new KeyValuePair<string, string>("S10", FEsc(RzDetailed(
            L(P1("ED=稳定", 0), P1("ED=增长", 2)), 2, true, T, false))));

        outs.Add(new KeyValuePair<string, string>("S11", FEsc(RzDetailed(
            L(P1("ED=增长", 1), O1("洛伦↓2档", -2)), -1, true, T, false))));

        outs.Add(new KeyValuePair<string, string>("S12", FEsc(RzDetailed(
            L(P1("某个没见过的词条", 1), P1("<B>C&D", -1), P1("ED=危险", -2)), -2, true, T, false))));

        outs.Add(new KeyValuePair<string, string>("S13", FEsc(RzDetailed(
            L(P1("ED=危险", -5), P1("逃税", -3), O1("洛伦↓1档", -1), O1("避税(一次性)", -2)),
            -11, true, T, false))));

        outs.Add(new KeyValuePair<string, string>("S14", FEsc(RzDetailed(
            L(G1("免费处方药(每回合)", -1, 0, W), G1("福利支出", 1, 0, W), P1("ED=增长", 1)),
            1, true, T, false))));

        outs.Add(new KeyValuePair<string, string>("S15", FEsc(RzDetailed3(
            L(P1("ED=增长", 1), P1("税效率", 1)), 2, true))));

        outs.Add(new KeyValuePair<string, string>("S16", FEsc(RzDetailed(
            L(O1("避税(一次性)", -3)), -3, true, T, false))));

        outs.Add(new KeyValuePair<string, string>("S17", FEsc(RzDetailed(
            L(P1("ED=增长", 123), P1("逃税", -456), O1("贝吉亚↑2档", 78)), -255, true, T, false))));

        outs.Add(new KeyValuePair<string, string>("S18", FEsc(InGameDetailFormat.Compose(
            null, 0, true, T, null, false, InGameDetailFormat.StyleDetailed))));

        return outs;
    }
}
