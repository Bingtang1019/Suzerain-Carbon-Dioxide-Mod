






















using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using Oikonomia;

partial class Program
{
    

    
    static string RRSamplePath()
    {
        string rel = System.IO.Path.Combine("docs", "rizia-真实明细格式-样本.txt");
        var dir = new System.IO.DirectoryInfo(AppContext.BaseDirectory);
        for (int i = 0; i < 8 && dir != null; i++, dir = dir.Parent)
        {
            string a = System.IO.Path.Combine(dir.FullName, "Oikonomia", rel);
            if (System.IO.File.Exists(a)) return a;
            string b = System.IO.Path.Combine(dir.FullName, rel);
            if (System.IO.File.Exists(b)) return b;
        }
        return null;
    }

    
    static string RRSampleReturn(string path, string stat, int idx)
    {
        if (path == null) return null;
        string head = "data=" + stat + " ";
        const string mark = "返回值=[";
        int seen = 0;
        foreach (var line in System.IO.File.ReadAllLines(path))
        {
            if (line.IndexOf(head, StringComparison.Ordinal) < 0) continue;
            int a = line.IndexOf(mark, StringComparison.Ordinal);
            if (a < 0) continue;
            a += mark.Length;
            int b = line.LastIndexOf(']');
            if (b <= a) continue;
            if (seen++ != idx) continue;
            return line.Substring(a, b - a).Replace("\\n", "\n");
        }
        return null;
    }

    
    struct RRLine
    {
        public int Kind;
        public int Block;         
        public string Hex;        
        public int Value;         
        public string Label;      
        public bool IndentClosed; 
    }

    
    struct RRGroup
    {
        public string Hex;            
        public int Subtotal;          
        public List<RRLine> Entries;  
    }

    
    struct RRBlock
    {
        public string Head;           
        public List<RRGroup> Groups;
        
        public string Title;
        
        public bool HasRule;
    }

    
    
    
    static readonly Regex RRTitleRe = new Regex(
        @"^<align=center><b><color=#(?<hex>[0-9A-Fa-f]{6})>(?<t>.+)</color></b></align>$");
    static readonly Regex RRRuleRe = new Regex(
        @"^<color=#(?<hex>[0-9A-Fa-f]{6})>──────</color>$");

    
    static bool RRIsTitleOrRule(string s)
    {
        return s != null && (RRTitleRe.IsMatch(s) || RRRuleRe.IsMatch(s));
    }

    
    
    
    
    
    
    
    
    
    static bool RRIsBlockHead(string s, bool allowLegacy)
    {
        if (string.Equals(s, InGameDetailFormat.RiziaPerTurnLabel, StringComparison.Ordinal)) return true;
        if (string.Equals(s, InGameDetailFormat.RiziaThisTurnLabel, StringComparison.Ordinal)) return true;
        if (!allowLegacy) return false;
        return string.Equals(s, "Per Turn:", StringComparison.Ordinal)
            || string.Equals(s, "This Turn:", StringComparison.Ordinal);
    }

    
    const string RRLegacyHead = "Per Turn:";

    
    
    
    
    
    
    
    
    
    static string RRNormalizeSampleHead(string sample, out string why)
    {
        why = null;
        if (string.IsNullOrEmpty(sample)) { why = "样本为空"; return sample; }
        string first = sample.Split('\n')[0];
        if (!string.Equals(first, RRLegacyHead, StringComparison.Ordinal))
        { why = "样本首行不是「" + RRLegacyHead + "」而是「" + first + "」⇒ 不能做良定义的替换"; return sample; }
        int at = sample.IndexOf('\n');
        return InGameDetailFormat.RiziaPerTurnLabel + sample.Substring(at);
    }

    
    static readonly Regex RRHeadRe = new Regex(
        @"^<color=#(?<hex>[0-9A-Fa-f]{6})>\((?<val>[+-]?\d+)\)</color> <u>Total:</u><indent=5%>$");
    static readonly Regex RREntryRe = new Regex(
        @"^<color=#(?<hex>[0-9A-Fa-f]{6})>\((?<val>[+-]?\d+)\)</color> (?<label>.+)$");

    
    
    
    
    
    
    
    
    
    
    
    
    
    static List<RRBlock> RRParseBlocks(string rendered, out string why, bool allowLegacy = false)
    {
        why = null;
        var blocks = new List<RRBlock>();
        if (string.IsNullOrEmpty(rendered)) { why = "产物为空串"; return blocks; }

        var raw = rendered.Split('\n');
        var cur = new RRBlock();
        cur.Head = null;
        cur.Groups = new List<RRGroup>();
        var g = new RRGroup();
        g.Entries = new List<RRLine>();
        bool inGroup = false;

        
        
        int start = 0;
        string titleText = null;
        bool hasRule = false;
        if (raw.Length >= 2 && !RRIsBlockHead(raw[0], allowLegacy))
        {
            var mt = RRTitleRe.Match(raw[0]);
            var mr = RRRuleRe.Match(raw[1]);
            if (!mt.Success || !mr.Success)
            { why = "第 1 行既不是块头也不是合法标题行：「" + raw[0] + "」"; return blocks; }
            titleText = mt.Groups["t"].Value;
            hasRule = true;
            start = 2;
        }

        for (int i = start; i < raw.Length; i++)
        {
            string s = raw[i];

            
            if (s.Length == 0)
            {
                if (cur.Head == null || inGroup) { why = "第 " + (i + 1) + " 行的空行位置非法（只允许出现在两块之间）"; return blocks; }
                if (i == raw.Length - 1) { why = "整段以空行收尾（真机以 </indent> 收尾）"; return blocks; }
                continue;
            }

            
            if (RRIsBlockHead(s, allowLegacy))
            {
                if (inGroup) { why = "第 " + (i + 1) + " 行是块头却落在组内（上一组缺 </indent>）"; return blocks; }
                if (cur.Head != null)
                {
                    if (string.Equals(cur.Head, s, StringComparison.Ordinal)) { why = "块头「" + s + "」重复出现"; return blocks; }
                    blocks.Add(cur);
                    cur = new RRBlock();
                    cur.Groups = new List<RRGroup>();
                }
                cur.Head = s;
                continue;
            }

            if (cur.Head == null) { why = "第 " + (i + 1) + " 行出现在任何块头之前：「" + s + "」"; return blocks; }

            bool closed = s.EndsWith(InGameDetailFormat.IndentClose, StringComparison.Ordinal);
            string body = closed ? s.Substring(0, s.Length - InGameDetailFormat.IndentClose.Length) : s;

            var mh = RRHeadRe.Match(body);
            if (mh.Success)
            {
                if (closed) { why = "第 " + (i + 1) + " 行是组头却带 </indent>"; return blocks; }
                if (inGroup) { why = "第 " + (i + 1) + " 行开了新组，但上一组没有 </indent> 闭合"; return blocks; }
                g = new RRGroup();
                g.Hex = "#" + mh.Groups["hex"].Value.ToUpperInvariant();
                g.Subtotal = int.Parse(mh.Groups["val"].Value, NumberStyles.Integer, CultureInfo.InvariantCulture);
                g.Entries = new List<RRLine>();
                inGroup = true;
                continue;
            }

            var me = RREntryRe.Match(body);
            if (me.Success)
            {
                if (!inGroup) { why = "第 " + (i + 1) + " 行明细出现在任何组头之前"; return blocks; }
                var e = new RRLine();
                e.Kind = 2;
                e.Block = blocks.Count;                 
                e.Hex = "#" + me.Groups["hex"].Value.ToUpperInvariant();
                e.Value = int.Parse(me.Groups["val"].Value, NumberStyles.Integer, CultureInfo.InvariantCulture);
                e.Label = me.Groups["label"].Value;
                e.IndentClosed = closed;
                g.Entries.Add(e);
                if (closed) { cur.Groups.Add(g); inGroup = false; }
                continue;
            }

            why = "第 " + (i + 1) + " 行既不是块头也不是组头也不是明细（色标签可能不只包数值）：「" + raw[i] + "」";
            return blocks;
        }

        if (inGroup) { why = "最后一块的组没有 </indent> 闭合"; return blocks; }
        if (cur.Head != null) blocks.Add(cur);
        if (blocks.Count == 0) { why = "没有任何块（连块头都没有）"; return blocks; }
        if (titleText != null)                       
        {
            var b0 = blocks[0];
            b0.Title = titleText;
            b0.HasRule = hasRule;
            blocks[0] = b0;
        }
        return blocks;
    }

    
    
    
    
    
    static List<RRLine> RRParse(string raw, out string why, bool allowLegacy = false)
    {
        var outs = new List<RRLine>();
        var blocks = RRParseBlocks(raw, out why, allowLegacy);
        if (why != null) return outs;

        for (int b = 0; b < blocks.Count; b++)
        {
            var h0 = new RRLine();
            h0.Kind = 0; h0.Block = b; h0.Label = blocks[b].Head;
            outs.Add(h0);

            for (int gi = 0; gi < blocks[b].Groups.Count; gi++)
            {
                var g = blocks[b].Groups[gi];
                var h1 = new RRLine();
                h1.Kind = 1; h1.Block = b; h1.Hex = g.Hex; h1.Value = g.Subtotal; h1.Label = blocks[b].Head;
                outs.Add(h1);
                for (int ei = 0; ei < g.Entries.Count; ei++) outs.Add(g.Entries[ei]);
            }
        }
        return outs;
    }

    
    static List<InGameDetailFormat.Entry> RREntries(List<RRLine> lines)
    {
        var outs = new List<InGameDetailFormat.Entry>();
        for (int i = 0; i < lines.Count; i++)
        {
            if (lines[i].Kind != 2) continue;
            outs.Add(new InGameDetailFormat.Entry(lines[i].Label, lines[i].Value,
                InGameDetailFormat.KindPerTurn, null));
        }
        return outs;
    }

    
    static bool RRSampleReturnsIdentical(string path, string stat)
    {
        string first = RRSampleReturn(path, stat, 0);
        if (first == null) return false;
        int n = 1;
        for (int i = 1; ; i++)
        {
            string next = RRSampleReturn(path, stat, i);
            if (next == null) break;
            n++;
            if (!string.Equals(next, first, StringComparison.Ordinal)) return false;
        }
        return n >= 2;
    }

    
    static int RRFirstDiff(string a, string b)
    {
        if (a == null || b == null) return -2;
        int n = Math.Min(a.Length, b.Length);
        for (int i = 0; i < n; i++) if (a[i] != b[i]) return i;
        return a.Length == b.Length ? -1 : n;
    }

    
    static string RRDiff(string expect, string got)
    {
        int at = RRFirstDiff(expect, got);
        if (at == -1) return "逐字节相同";
        if (at == -2) return "一侧为 null（expect=" + (expect == null ? "null" : "串") + " got=" + (got == null ? "null" : "串") + "）";
        return "首个差异 @" + at + "：真机" + FCode(expect, at) + " vs 我们" + FCode(got, at)
             + "｜长度 真机=" + (expect == null ? -1 : expect.Length) + " 我们=" + (got == null ? -1 : got.Length);
    }

    
    static string Rz8(List<InGameDetailFormat.Entry> e, int total, bool hasSettle,
                      string pos, string neg)
    {
        return InGameDetailFormat.Compose(e, total, hasSettle, null, null, false,
            InGameDetailFormat.StyleRizia, pos, neg);
    }

    
    static void RiziaRealSuite()
    {
        Console.WriteLine("");
        Console.WriteLine("【⑮ Rizia 真机形态（黄金串派生自 docs\\rizia-真实明细格式-样本.txt）】");

        string sample = RRSamplePath();
        True("⑮-A0 归档真机样本可定位（Oikonomia\\docs\\rizia-真实明细格式-样本.txt）",
            sample != null, sample ?? "未找到 —— 后续真机比对无法执行");

        string realBudget = RRSampleReturn(sample, "Rizia_HUDStat_Budget", 0);
        string realAuth = RRSampleReturn(sample, "Rizia_HUDStat_Authority", 0);
        True("⑮-A1 样本里读到 Rizia_HUDStat_Budget 的返回值",
            !string.IsNullOrEmpty(realBudget),
            realBudget == null ? "未读到" : (realBudget.Length + " 字符"));
        True("⑮-A2 样本里读到 Rizia_HUDStat_Authority 的返回值",
            !string.IsNullOrEmpty(realAuth),
            realAuth == null ? "未读到" : (realAuth.Length + " 字符"));
        True("⑮-A3 探针多次调用同一 stat 的返回值逐字节相同（样本自洽，可当夹具）",
            RRSampleReturnsIdentical(sample, "Rizia_HUDStat_Budget")
            && RRSampleReturnsIdentical(sample, "Rizia_HUDStat_Authority"),
            "Budget / Authority 各自多次调用完全一致");

        string why;
        
        
        var bBlocks = RRParseBlocks(realBudget, out why, true);
        var bLines = RRParse(realBudget, out why, true);
        True("⑮-A4 Budget 返回值可逐行解析（块头 / 组头 / 明细三类，且色标签只包数值）",
            why == null && bLines.Count > 0, why ?? (bLines.Count + " 行"));
        True("⑮-A4b 真机返回值只有**一个**块、块头就是实测原样「Per Turn:」（Rizia 只有一种视角 —— 这正是本版要拆两块的原因）",
            why == null && bBlocks.Count == 1
            && string.Equals(bBlocks[0].Head, RRLegacyHead, StringComparison.Ordinal),
            why ?? ("块数 " + bBlocks.Count + "，块头 = " + (bBlocks.Count > 0 ? bBlocks[0].Head : "(无)")));
        
        
        Same("⑮-A4c ★与真机的**唯一**已知形态偏离 = 块①块头字面量（真机「Per Turn:」→ 用户要求的「未来」）",
            RRLegacyHead + " → " + InGameDetailFormat.RiziaPerTurnLabel, "Per Turn: → 未来");
        True("⑮-A4d 该替换是良定义的（样本首行确实只有这一行块头）",
            CountOf(realBudget ?? "", "\n" + RRLegacyHead) == 0
            && (realBudget ?? "").StartsWith(RRLegacyHead, StringComparison.Ordinal),
            "样本首行 = 「" + ((realBudget ?? "").Split('\n')[0]) + "」，且正文里不再出现该字面量");

        int bHeads = 0, bEntries = 0, bPos = 0, bNeg = 0;
        for (int i = 0; i < bLines.Count; i++)
        {
            if (bLines[i].Kind == 1) bHeads++;
            if (bLines[i].Kind != 2) continue;
            bEntries++;
            if (bLines[i].Value > 0) bPos++; else if (bLines[i].Value < 0) bNeg++;
        }
        SameInt("⑮-A5 组头行数 = 2（正数组一个 + 负数组一个）", bHeads, 2);
        SameInt("⑮-A6 明细行数 = 20（10 正 + 10 负）", bEntries, 20);
        SameInt("⑮-A7 正/负条数分别为 10 / 10", bPos * 10 + bNeg, 10 * 10 + 10);

        
        Console.WriteLine("");
        Console.WriteLine("【⑮-B Budget 逐字节黄金串（我们 == 真机）】");

        var bEntriesList = RREntries(bLines);
        int bSum = InGameDetailFormat.SumDeltas(bEntriesList);
        SameInt("⑮-B0 真机 20 条的净额（也应是两组组头之和 +1021-41）", bSum, 980);

        
        
        string realBExp = RRNormalizeSampleHead(realBudget, out why);
        True("⑮-B0b 期望串的构造是良定义的（真机首行 == Per Turn:，替换后首行 == 未来）",
            why == null && realBExp != null && realBExp.StartsWith(InGameDetailFormat.RiziaPerTurnLabel + "\n", StringComparison.Ordinal),
            why ?? ("替换后首行 = 「" + (realBExp ?? "").Split('\n')[0] + "」"));
        string mineB = InGameDetailFormat.Compose(bEntriesList, bSum, true, null, null, false,
            InGameDetailFormat.StyleRizia);
        int atB = RRFirstDiff(realBExp, mineB);
        True("⑮-B1 **Budget 黄金串逐字节 == 真机返回值**（顺序/空格/标签位置/色值全对；仅首行块头按用户要求换字）",
            atB == -1, atB == -1 ? ("除块头外全等｜" + RRDiff(realBExp, mineB)) : RRDiff(realBExp, mineB));
        Same("⑮-B2 同一比对（失败时打印两侧完整转义串，便于定位）", mineB, realBExp);

        
        string posHex = InGameDetailFormat.RgbaToHex(0.055, 0.820, 0.055);
        string negHex = InGameDetailFormat.RgbaToHex(0.820, 0.067, 0.067);
        string mineB2 = Rz8(bEntriesList, bSum, true, posHex, negHex);
        Same("⑮-B3 取色走「运行时实例 RGBA → hex」这一路时，同样逐字节 == 真机（同上口径）", mineB2, realBExp);
        SameInt("⑮-B3b ★偏离只有一行：把我们的产物首行换回 Per Turn: 之后 == 真机原文逐字节",
            RRFirstDiff(realBudget, RRLegacyHead + mineB.Substring(mineB.IndexOf('\n'))), -1);
        Same("⑮-B4 探针 RGBA 换算出的正是样本里的两个色值",
            posHex + "|" + negHex, "#0ED10E|#D11111");

        
        Console.WriteLine("");
        Console.WriteLine("【⑮-C Authority 逐字节黄金串 + 已知差项】");

        var aLines = RRParse(realAuth, out why, true);
        True("⑮-C0 Authority 返回值可逐行解析", why == null && aLines.Count > 0, why ?? (aLines.Count + " 行"));
        var aEntriesList = RREntries(aLines);
        string mineA = InGameDetailFormat.Compose(aEntriesList, InGameDetailFormat.SumDeltas(aEntriesList),
            true, null, null, false, InGameDetailFormat.StyleRizia);

        
        
        var budgetRewrites = RRLabelsRewritten(bLines);
        var authRewrites = RRLabelsRewritten(aLines);
        SameInt("⑮-C1 Budget 的 20 条真机文案经 Display() 后**一字不改**（改写条数 = 0）",
            budgetRewrites.Count, 0);
        Same("⑮-C2 Authority 的改写集合 == 恰好一条「君主罗穆斯·托拉斯 → 与君主罗穆斯达成托拉斯贸易」",
            RRJoin(authRewrites), "君主罗穆斯·托拉斯 → 与君主罗穆斯达成托拉斯贸易");
        string authExpect = realAuth == null ? null
            : RRNormalizeSampleHead(realAuth, out why).Replace("君主罗穆斯·托拉斯", "与君主罗穆斯达成托拉斯贸易");
        Same("⑮-C3 Authority：**除那条已知差项 + 首行块头换字外逐字节一致**（组头/indent/色值/降序全对）",
            mineA, authExpect);
        True("⑮-C4 该差项与 Rizia 形态无关（是 Display 文案映射层的既有行为，本版未动）",
            string.Equals(InGameDetailFormat.Display("君主罗穆斯·托拉斯"), "与君主罗穆斯达成托拉斯贸易",
                StringComparison.Ordinal),
            "Display(君主罗穆斯·托拉斯) = " + InGameDetailFormat.Display("君主罗穆斯·托拉斯"));

        
        Console.WriteLine("");
        Console.WriteLine("【⑮-D 块内逐字节规则（合成条目集：两块 × 各组，规则逐条钉住）】");

        
        
        
        var dList = L(P1("甲", 3), P1("乙", 10), P1("丙", 3), P1("丁", 0), P1("辛", -4),
                      O1("壬", 2), O1("戊", -5), O1("己", -1), O1("庚", 0));
        string d = InGameDetailFormat.Compose(dList, 10, true, InGameDetailFormat.PreviewTitle, null, true,
            InGameDetailFormat.StyleRizia);      
        var dLines = d.Split('\n');

        Same("⑮-D1 ①块①块头常量逐字 == 「未来」（**用户明确要求**：Per turn 换成「未来」）",
            InGameDetailFormat.RiziaPerTurnLabel, "未来");
        Same("⑮-D1b ①块②块头常量逐字 == 「当前」（**用户明确要求**：把「当前」加上）",
            InGameDetailFormat.RiziaThisTurnLabel, "当前");
        True("⑮-D1c ①两块块头都是**纯文本整行**（无标签、无前后空格），且按整行相等各出现 1 次",
            dLines[2] == InGameDetailFormat.RiziaPerTurnLabel && dLines[2].IndexOf('<') < 0
            && RRCountHeadLines(d, InGameDetailFormat.RiziaThisTurnLabel) == 1
            && RRCountHeadLines(d, InGameDetailFormat.RiziaPerTurnLabel) == 1,
            "块①头 = 「" + dLines[2] + "」");
        Same("⑮-D1d ★ 本版形态逐字节（标题 + 分隔线 + 两块 × 各自正负两组；块间一个空行；末尾无换行）", d,
            "<align=center><b><color=#9A9A9A>当前预测下的财政状态</color></b></align>\n"
            + "<color=#9A9A9A>──────</color>\n"
            + "未来\n"
            + "<color=#0ED10E>(+16)</color> <u>Total:</u><indent=5%>\n"
            + "<color=#0ED10E>(+10)</color> 乙\n"
            + "<color=#0ED10E>(+3)</color> 甲\n"
            + "<color=#0ED10E>(+3)</color> 丙</indent>\n"
            + "<color=#D11111>(-4)</color> <u>Total:</u><indent=5%>\n"
            + "<color=#D11111>(-4)</color> 辛</indent>\n"
            + "\n"
            + "当前\n"
            + "<color=#0ED10E>(+2)</color> <u>Total:</u><indent=5%>\n"
            + "<color=#0ED10E>(+2)</color> 壬</indent>\n"
            + "<color=#D11111>(-6)</color> <u>Total:</u><indent=5%>\n"
            + "<color=#D11111>(-5)</color> 戊\n"
            + "<color=#D11111>(-1)</color> 己</indent>");

        
        var dBlocks = RRParseBlocks(d, out why);
        True("⑮-D2 产物可解析成块 → 组 → 明细三层", why == null && dBlocks.Count > 0, why ?? (dBlocks.Count + " 块"));
        True("⑮-D3 ②块头各一次、顺序为「未来」→「当前」、每块都有组",
            RRBlockHeadsOk(dBlocks, out why), why ?? "");
        SameInt("⑮-D4 ②块数 = 2（每回合类一块 + 一次性类一块）", dBlocks.Count, 2);
        True("⑮-D5 行数守恒（标题前缀 + 块头 + 组头 + 明细 + 块间空行 == 总行数）",
            RRLineCountOk(d, dBlocks, out why), why ?? "");
        SameInt("⑮-D6 ②块间恰好一个空行（组间没有空行）", CountOf(d, "\n\n"), 1);
        True("⑮-D7 ②空行数 == 块数 - 1", RRBlankLinesOk(d, dBlocks, out why), why ?? "");
        True("⑮-D8 ②全串**末尾没有换行**（真机以 </indent> 收尾）",
            !d.EndsWith("\n", StringComparison.Ordinal) && d.EndsWith(InGameDetailFormat.IndentClose, StringComparison.Ordinal),
            "末 8 字符 = " + d.Substring(Math.Max(0, d.Length - 8)));

        
        SameInt("⑮-D9 ③组头（<u>Total:</u>）数量 == 4（两块各正负一组）", CountOf(d, "<u>Total:</u>"), 4);
        SameInt("⑮-D10 ③**组头在组前**：每块第 1 组是该块正组、且它与块头之间没有明细行",
            (dBlocks[0].Groups[0].Subtotal == 16 ? 1 : 0) + (dBlocks[1].Groups[0].Subtotal == 2 ? 1 : 0), 1 + 1);
        Same("⑮-D11 ③块①正组组头 = 该组 3 条之和（3+10+3 = +16），标签加下划线、后接 <indent=5%>",
            dLines[3], "<color=#0ED10E>(+16)</color> <u>Total:</u><indent=5%>");
        Same("⑮-D12 ③块①负组组头 = -4", dBlocks[0].Groups[1].Subtotal.ToString(CultureInfo.InvariantCulture), "-4");
        Same("⑮-D13 ③块②正组组头 = +2、负组组头 = -6（-5 与 -1 之和）",
            dBlocks[1].Groups[0].Subtotal + "|" + dBlocks[1].Groups[1].Subtotal, "2|-6");
        True("⑮-D14 ★ 块内先正数组、后负数组（**两块各自**成立）",
            dBlocks[0].Groups[0].Subtotal > 0 && dBlocks[0].Groups[1].Subtotal < 0
            && dBlocks[1].Groups[0].Subtotal > 0 && dBlocks[1].Groups[1].Subtotal < 0,
            "块① " + dBlocks[0].Groups[0].Subtotal + " → " + dBlocks[0].Groups[1].Subtotal
            + "｜块② " + dBlocks[1].Groups[0].Subtotal + " → " + dBlocks[1].Groups[1].Subtotal);

        
        True("⑮-D15 ④<indent=5%> 与 </indent> 成对、数量**恰好等于组数**（4）",
            RRIndentPairsOk(d, dBlocks, out why), why ?? "");
        SameInt("⑮-D16 ⑤组尾 </indent> 贴在该组最后一条明细行尾（单独占行的数量 = 0）",
            RRIndentAloneLines(d), 0);

        
        Same("⑮-D17 ⑥组内按数值降序（并列 |3| 保持输入顺序：甲 → 丙）",
            dBlocks[0].Groups[0].Entries[0].Label + "|" + dBlocks[0].Groups[0].Entries[1].Label
            + "|" + dBlocks[0].Groups[0].Entries[2].Label, "乙|甲|丙");
        True("⑮-D18 ⑥正数组内降序（**逐组**判定）", RRDescending(RRParse(d, out why), true, out why), why ?? "");
        True("⑮-D19 ⑥负数组内按**绝对值**降序（-5 在 -1 之前；**逐组**判定）",
            RRDescending(RRParse(d, out why), false, out why), why ?? "");
        Same("⑮-D20 ⑥块②负组次序 = 戊(-5) → 己(-1)", dBlocks[1].Groups[1].Entries[0].Label + "|" + dBlocks[1].Groups[1].Entries[1].Label, "戊|己");

        
        SameInt("⑮-D21 ④色标签只包数值：`</color> `（闭标签紧跟一格空格）出现次数 == 组头 4 + 明细 7",
            CountOf(d, "</color> "), 4 + 7);
        SameInt("⑮-D22 ④色标签总数 == 组头 4 + 明细 7 + 标题前缀 2（标题行与分隔线各一个 <color>）",
            CountOf(d, "<color="), 4 + 7 + 2);
        True("⑮-D23 ④每条明细行都过严格字节判据（<color> 紧包 (±N)，随后「空格 + 说明」）",
            RREntryStrictAll(d, out why), why ?? "全部合规");
        True("⑮-D24 ④**说明不染色**：每条明细的说明段里一个标签都没有（默认色）",
            RRLabelsTagFree(d, out why), why ?? "");

        
        SameInt("⑮-D25 ⑦零值条目不产生 `(0)` 形态", CountOf(d, "(0)"), 0);
        NotHas("⑮-D26 ⑦零值条目丁一条都不上屏", d, "丁");
        NotHas("⑮-D27 ⑦零值条目庚一条都不上屏", d, "庚");
        SameInt("⑮-D28 ★**标题存在且恰好一次**（2.9.2 用户要求恢复；title 参数本形态现在会读）",
            CountOf(d, InGameDetailFormat.PreviewTitle), 1);
        NotHas("⑮-D29 缺席：Rizia 形态无 Detailed 段头【未来】/【当前】（裸「未来」「当前」不算）", d, "【未来】");
        SameInt("⑮-D30 缺席：无小计行 / 无**框线** / 无文本框（标题下那条 ────── 是标题分隔线，不是框线）",
            CountOf(d, "小计") + (CountOf(d, InGameDetailFormat.TotalDivider) - 1)
            + CountOf(d, InGameDetailFormat.FrameVertical.ToString()), 0);

        
        Console.WriteLine("");
        Console.WriteLine("【⑮-E 零值口径（不进任何一组）与退化口径】");

        Same("⑮-E1 全是零值 + 净额 0 → 单行灰字「本回合无变动」（与 Detailed 同一支）",
            InGameDetailFormat.Compose(L(P1("丁", 0)), 0, true, null, null, false, InGameDetailFormat.StyleRizia),
            Tag("Z", InGameDetailFormat.NoChangeText));
        Same("⑮-E2 全是零值 + 净额非 0 → 同样退化（不吐光秃秃的 Per Turn:）",
            InGameDetailFormat.Compose(L(P1("丁", 0)), 7, true, null, null, false, InGameDetailFormat.StyleRizia),
            Tag("Z", InGameDetailFormat.NoChangeText));
        Same("⑮-E3 无分项 + 净额非 0 → 同样退化（真机形态没有「只有合计」这种行）",
            InGameDetailFormat.Compose(L(), 7, true, null, null, false, InGameDetailFormat.StyleRizia),
            Tag("Z", InGameDetailFormat.NoChangeText));
        True("⑮-E4 组内求和为 0 的组行也被零值口径吃掉（不进任何一组）",
            InGameDetailFormat.EmitRiziaGroups(
                InGameDetailFormat.BuildRows(L(
                    G1("免费处方药(每回合)", -1, InGameDetailFormat.KindPerTurn, InGameDetailFormat.GroupExtraWelfare),
                    G1("免费校餐(每回合)", 1, InGameDetailFormat.KindPerTurn, InGameDetailFormat.GroupExtraWelfare))),
                new List<InGameDetailFormat.Row>(), InGameDetailFormat.RiziaPalette.Measured,
                InGameDetailFormat.PreviewTitle) == null,
            "两组皆空 → 发射器返回 null（调用方退化成灰字；**连标题都不写**）");
        True("⑮-E5 零值与正负值混排：只有 >0 / <0 进组，零值既不占行也不改合计",
            CountOf(InGameDetailFormat.Compose(
                L(P1("甲", 3), P1("丁", 0), O1("己", -1)), 2, true, null, null, false,
                InGameDetailFormat.StyleRizia), "(0)") == 0
            && InGameDetailFormat.Compose(
                L(P1("甲", 3), P1("丁", 0), O1("己", -1)), 2, true, null, null, false,
                InGameDetailFormat.StyleRizia).IndexOf("<u>Total:</u>") > 0,
            "含零值仍正常出两组");
        True("⑮-E6 真机样本里**没有**零值条目（所以零值口径由实现方定，见 CHANGELOG）",
            RRHasNoZero(bLines) && RRHasNoZero(aLines),
            "Budget / Authority 的 33 条明细里 (0) 计数 = 0");

        
        Console.WriteLine("");
        Console.WriteLine("【⑮-F 取色：RgbaToHex 实测锚点 / 逐项回退 / 只影响 Rizia】");

        Same("⑮-F1 RgbaToHex(0.055,0.820,0.055) == #0ED10E（探针实测 → 真机样本里的正色）",
            InGameDetailFormat.RgbaToHex(0.055, 0.820, 0.055), "#0ED10E");
        Same("⑮-F2 RgbaToHex(0.820,0.067,0.067) == #D11111（探针实测 → 真机样本里的负色）",
            InGameDetailFormat.RgbaToHex(0.820, 0.067, 0.067), "#D11111");
        Same("⑮-F3 回退常量与真机样本里的两个色值逐字节相同",
            InGameDetailFormat.RiziaPositiveColor + "|" + InGameDetailFormat.RiziaNegativeColor,
            RRFixtureHexes(bLines));
        Same("⑮-F4 RgbaToHex 也吃「已是 0..255」的分量（15,209,15 → #0FD10F）",
            InGameDetailFormat.RgbaToHex(15, 209, 15), "#0FD10F");
        Same("⑮-F5 RgbaToHex 越界钳制（-1 → 00，300 → FF）",
            InGameDetailFormat.RgbaToHex(-1, 300, 0), "#00FF00");
        Same("⑮-F6 NormalizeHex：#0ed10e → 大写 #0ED10E；#0ED10EFF → 丢 alpha",
            InGameDetailFormat.NormalizeHex("#0ed10e") + "|" + InGameDetailFormat.NormalizeHex("#0ED10EFF"),
            "#0ED10E|#0ED10E");
        Same("⑮-F7 NormalizeHex：非法输入一律 null（null / 空 / 缺# / 位数不对 / 非十六进制）",
            (InGameDetailFormat.NormalizeHex(null) ?? "null") + "|"
            + (InGameDetailFormat.NormalizeHex("") ?? "null") + "|"
            + (InGameDetailFormat.NormalizeHex("0ED10E") ?? "null") + "|"
            + (InGameDetailFormat.NormalizeHex("#0ED10") ?? "null") + "|"
            + (InGameDetailFormat.NormalizeHex("#GGGGGG") ?? "null"),
            "null|null|null|null|null");
        Same("⑮-F8 RiziaPalette.Of(null,null) → 回退实测常量对",
            InGameDetailFormat.RiziaPalette.Of(null, null).Positive + "|"
            + InGameDetailFormat.RiziaPalette.Of(null, null).Negative,
            "#0ED10E|#D11111");
        Same("⑮-F9 RiziaPalette **逐项**回退：只给正色 → 正色用传入值、负色回退常量",
            InGameDetailFormat.RiziaPalette.Of("#123456", null).Positive + "|"
            + InGameDetailFormat.RiziaPalette.Of("#123456", null).Negative,
            "#123456|#D11111");
        Has("⑮-F10 8 参重载的取色真的生效（传入 #010203 → 正数组组头用它）",
            Rz8(L(P1("甲", 1), O1("乙", -1)), 0, true, "#010203", "#040506"),
            "<color=#010203>(+1)</color> <u>Total:</u>");
        Same("⑮-F11 同一份取色参数对 **Detailed** 形态**完全无效**（2.7.8 字节不变）",
            InGameDetailFormat.Compose(L(P1("甲", 1), O1("乙", -1)), 0, true, null, null, false,
                InGameDetailFormat.StyleDetailed, "#010203", "#040506"),
            InGameDetailFormat.Compose(L(P1("甲", 1), O1("乙", -1)), 0, true, null, null, false,
                InGameDetailFormat.StyleDetailed));
        Same("⑮-F12 7 参重载 == 8 参重载传 null（取色缺省即回退实测常量）",
            Rz8(L(P1("甲", 1), O1("乙", -1)), 0, true, null, null),
            InGameDetailFormat.Compose(L(P1("甲", 1), O1("乙", -1)), 0, true, null, null, false,
                InGameDetailFormat.StyleRizia));

        
        Console.WriteLine("");
        Console.WriteLine("【⑮-G 41 条文案：正负分组 / 组内降序 / 零值剔除 / 和守恒】");

        var all41 = FAll41();
        int allTotal = InGameDetailFormat.SumDeltas(all41);
        string rzAll = InGameDetailFormat.Compose(all41, allTotal, true, InGameDetailFormat.PreviewTitle, null,
            false, InGameDetailFormat.StyleRizia);
        var rzAllLines = RRParse(rzAll, out why);
        var rzAllBlocks = RRParseBlocks(rzAll, out why);
        True("⑮-G1 41 条场景的输出可解析成块 → 组 → 明细三层（含块间空行）",
            why == null && rzAllLines.Count > 0, why ?? (rzAllBlocks.Count + " 块 / " + rzAllLines.Count + " 行"));

        int gGroups = 0;
        for (int b = 0; b < rzAllBlocks.Count; b++) gGroups += rzAllBlocks[b].Groups.Count;

        int gHeads = 0, gEntries = 0, gPos = 0, gNeg = 0, gHeadSum = 0;
        for (int i = 0; i < rzAllLines.Count; i++)
        {
            if (rzAllLines[i].Kind == 1)
            {
                gHeads++;
                gHeadSum += rzAllLines[i].Value;
            }
            if (rzAllLines[i].Kind != 2) continue;
            gEntries++;
            if (rzAllLines[i].Value > 0) gPos++; else if (rzAllLines[i].Value < 0) gNeg++;
        }
        SameInt("⑮-G2 ②块数 = 2（41 条里每回合类与一次性类都有 ⇒ 两块都出现）", rzAllBlocks.Count, 2);
        True("⑮-G2b ②块头各一次、顺序 Per Turn: → This Turn:、每块都有组",
            RRBlockHeadsOk(rzAllBlocks, out why), why ?? "");
        True("⑮-G2c ③**逐组**：组头小计 == 组内各条之和（单一符号、取色相符）",
            RRGroupSubtotalsOk(rzAllBlocks, out why), why ?? "");
        SameInt("⑮-G2d ③组头数 == 组数 == indent 对数",
            gHeads * 100 + gGroups, gGroups * 100 + gGroups);
        True("⑮-G2e ④indent 成对且数量 == 组数", RRIndentPairsOk(rzAll, rzAllBlocks, out why), why ?? "");
        True("⑮-G2f 行数守恒（块头 + 组头 + 明细 + 块间空行）", RRLineCountOk(rzAll, rzAllBlocks, out why), why ?? "");
        SameInt("⑮-G3 41 条里 3 条零值被剔除 ⇒ 明细 38 条", gEntries, 38);
        SameInt("⑮-G4 正 / 负条数分别为 29 / 9（逐条数自 FAll41 的增量符号）", gPos * 100 + gNeg, 29 * 100 + 9);
        SameInt("⑮-G5 **和守恒**：所有组头小计之和 == 全部 41 条之和（零值剔除不影响结算口径）",
            gHeadSum, allTotal);

        
        True("⑮-G6 正数组内按数值降序（|值| 单调不增）", RRDescending(rzAllLines, true, out why), why ?? "降序成立");
        True("⑮-G7 负数组内按绝对值降序（|值| 单调不增）", RRDescending(rzAllLines, false, out why), why ?? "降序成立");

        
        string rzAllPlain = Plain(rzAll);
        int missing = 0; string firstMissing = null;
        var zeroTexts = new List<string> { "经济状态保持稳定", "全国旅游业平稳发展", "对外贸易额出现萎缩" };
        for (int i = 0; i < FAll41Texts.Length; i++)
        {
            bool isZero = zeroTexts.Contains(FAll41Texts[i]);
            bool present = rzAllPlain.IndexOf(FAll41Texts[i], StringComparison.Ordinal) >= 0;
            if (isZero ? !present : present) continue;      
            missing++;
            if (firstMissing == null) firstMissing = FAll41Texts[i] + (isZero ? "（零值却上屏了）" : "（非零却缺席）");
        }
        SameInt("⑮-G8 38 条非零文案逐条上屏、3 条零值文案逐条缺席（一条不差）", missing, 0);
        True("⑮-G9 守门：文案表仍是 41 条（不是把断言写空了）", FAll41Texts.Length == 41,
            firstMissing ?? "41 条逐条判过");
        SameInt("⑮-G10 每条明细文案在整段里恰好出现 1 次（无重复、无串行）",
            RREachLabelOnce(rzAllLines, out why), 0);
        SameInt("⑮-G11 组归并仍成立：额外福利支出只出现 1 行",
            CountOfLinesWith(rzAll, InGameDetailFormat.GroupExtraWelfare), 1);
        NotHas("⑮-G12 组内原始标签不上屏", rzAll, "免费处方药");
        Has("⑮-G13 地区多档文案一字不差（在负数组里）", rzAll,
            "<color=#D11111>(-2)</color> 洛伦地区发展大幅倒退（2 档）");

        
        Console.WriteLine("");
        Console.WriteLine("【⑮-H 富文本卫生（标签成对 / 零标记）+ 幂等 20 轮】");

        string gwhy;
        var scenes = new List<KeyValuePair<string, string>>();
        scenes.Add(new KeyValuePair<string, string>("真机 Budget 夹具", mineB));
        scenes.Add(new KeyValuePair<string, string>("41 条", rzAll));
        scenes.Add(new KeyValuePair<string, string>("合成集（含零值）", d));
        for (int i = 0; i < scenes.Count; i++)
        {
            string id = scenes[i].Key, s = scenes[i].Value;
            True("⑮-H" + (i + 1) + "a " + id + "：TMP 标签全部成对闭合（含 <indent=5%>/</indent>）",
                InGameDetailFormat.RichTagsClosed(s, out gwhy), gwhy ?? "全部闭合");
            True("⑮-H" + (i + 1) + "b " + id + "：整段零可见标记字符",
                InGameDetailFormat.IsMarkerFree(s, out gwhy), gwhy ?? "零标记");
            SameInt("⑮-H" + (i + 1) + "c " + id + "：<color> 开闭数量相等",
                CountOf(s, "<color=") - CountOf(s, "</color>"), 0);
            SameInt("⑮-H" + (i + 1) + "d " + id + "：<indent=5%> 与 </indent> 数量相等",
                CountOf(s, InGameDetailFormat.IndentOpen) - CountOf(s, InGameDetailFormat.IndentClose), 0);
            SameInt("⑮-H" + (i + 1) + "e " + id + "：不引入字号/字体/透明度标签（HUD 适配不受影响）",
                CountOf(s, "<size") + CountOf(s, "<font") + CountOf(s, "<alpha"), 0);
            int hasTitle = s.IndexOf(InGameDetailFormat.PreviewTitle, StringComparison.Ordinal) >= 0 ? 1 : 0;
            SameInt("⑮-H" + (i + 1) + "f " + id + "：无框线行 / 无小计行；"
                + "加粗 / 居中 / 分隔线**只属标题行**（有标题时各 1 次，无标题时 0 次）",
                (RzHasFrame(s) ? 1 : 0) + CountOf(s, "小计")
                + Math.Abs(CountOf(s, InGameDetailFormat.TotalDivider) - hasTitle)
                + Math.Abs(CountOf(s, "<b>") - hasTitle)
                + Math.Abs(CountOf(s, "<align") - hasTitle), 0);
            True("⑮-H" + (i + 1) + "g " + id + "：没有「行尾金额」旧形态",
                !HasTrailingValue(s), "行尾带符号整数 = " + (HasTrailingValue(s) ? "有" : "无"));
        }

        const string gameTip = "政府预算 <alpha=#44><size=16>(min: -20, max: +50)";
        var mem = new InGameDetailFormat.DetailMergeMemory();
        object slot = new object();
        bool hit;
        string r0 = mem.Merge(slot, gameTip, mineB, out hit);
        string cur = r0;
        int sameRounds = 0;
        for (int k = 0; k < 20; k++)
        {
            cur = mem.Merge(slot, cur, mineB, out hit);
            if (string.Equals(cur, r0, StringComparison.Ordinal)) sameRounds++;
        }
        SameInt("⑮-H4a 真机 Budget 明细连续 20 轮合并逐字节相同（幂等仍成立）", sameRounds, 20);
        Same("⑮-H4b 20 轮后游戏原文仍是逐字节前缀", cur.Substring(0, gameTip.Length), gameTip);
        SameInt("⑮-H4c 20 轮后 Per Turn: 仍只出现 1 次", CountOf(cur, InGameDetailFormat.RiziaPerTurnLabel), 1);
        SameInt("⑮-H4d 20 轮后组尾 </indent> 仍是 2 个（不累积、不膨胀）",
            CountOf(cur, InGameDetailFormat.IndentClose), 2);

        
        Console.WriteLine("");
        Console.WriteLine("【⑮-I 2.9.1 核心：两类分开成块（分离性 / 单类退化 / 幂等 / 真机夹具不受影响）】");

        List<string> ipt, iot;

        
        RRLabelSets(dList, out ipt, out iot);
        SameInt("⑮-I1 合成集的两类可见条数（每回合类 4 / 一次性类 3；零值各 1 条不上屏）",
            ipt.Count * 10 + iot.Count, 4 * 10 + 3);
        True("⑮-I2 ★C1 分离性（合成集）：Per Turn: 块里零一次性类、This Turn: 块里零每回合类",
            RRSeparation(dBlocks, ipt, iot, out why), why ?? "");

        
        List<string> ipt41, iot41;
        RRLabelSets(all41, out ipt41, out iot41);
        True("⑮-I3 ★C1 分离性（41 条实景）：同上判据，逐条双向核",
            RRSeparation(rzAllBlocks, ipt41, iot41, out why), why ?? "");
        True("⑮-I3b 41 条实景的逐组守恒（组头 == 组内之和，单符号、取色相符）",
            RRGroupSubtotalsOk(rzAllBlocks, out why), why ?? "");

        
        
        string iHead1 = InGameDetailFormat.RiziaPerTurnLabel;
        string iHead2 = InGameDetailFormat.RiziaThisTurnLabel;
        
        string iBad1 = iHead1 + "\n"
            + "<color=#0ED10E>(+1)</color> <u>Total:</u><indent=5%>\n"
            + "<color=#0ED10E>(+1)</color> 一次性甲</indent>";
        
        string iBad2 = iHead2 + "\n"
            + "<color=#0ED10E>(+1)</color> <u>Total:</u><indent=5%>\n"
            + "<color=#0ED10E>(+1)</color> 每回合甲</indent>";
        
        string iBad3 = iHead1 + "\n"
            + "<color=#0ED10E>(+2)</color> <u>Total:</u><indent=5%>\n"
            + "<color=#0ED10E>(+1)</color> 每回合甲\n"
            + "<color=#0ED10E>(+1)</color> 一次性甲</indent>";
        
        string iGood = iHead1 + "\n"
            + "<color=#0ED10E>(+1)</color> <u>Total:</u><indent=5%>\n"
            + "<color=#0ED10E>(+1)</color> 每回合甲</indent>\n"
            + "\n"
            + iHead2 + "\n"
            + "<color=#0ED10E>(+1)</color> <u>Total:</u><indent=5%>\n"
            + "<color=#0ED10E>(+1)</color> 一次性甲</indent>";
        var iPtL = new List<string> { "每回合甲" };
        var iOtL = new List<string> { "一次性甲" };
        string iw1, iw2, iw3;
        bool iOk1 = RRSeparation(RRParseBlocks(iBad1, out iw1), iPtL, iOtL, out iw1);
        bool iOk2 = RRSeparation(RRParseBlocks(iBad2, out iw2), iPtL, iOtL, out iw2);
        bool iOk3 = RRSeparation(RRParseBlocks(iBad3, out iw3), iPtL, iOtL, out iw3);
        True("⑮-I4 ★判据非空断言：一次性类混进「未来」块 → **判得出违规**（反例 1）",
            !iOk1, iOk1 ? "判据漏检了！" : ("已判出：" + iw1));
        True("⑮-I5 ★判据非空断言：每回合类混进「当前」块 → **判得出违规**（反例 2）",
            !iOk2, iOk2 ? "判据漏检了！" : ("已判出：" + iw2));
        True("⑮-I6 ★判据非空断言：2.9.0 那种「两类合并成一个块」→ **判得出违规**（反例 3）",
            !iOk3, iOk3 ? "判据漏检了！" : ("已判出：" + iw3));
        True("⑮-I7 ★判据非空断言：同一对条目按两块正确排布时判据**接受**（证明它不是恒假判据）",
            RRSeparation(RRParseBlocks(iGood, out why), iPtL, iOtL, out why), why ?? "");

        
        string iOnlyPt = InGameDetailFormat.Compose(L(P1("甲", 3), P1("乙", -1)), 2, true, null, null, false,
            InGameDetailFormat.StyleRizia);
        string iOnlyOt = InGameDetailFormat.Compose(L(O1("丙", 2), O1("丁", -1)), 1, true, null, null, false,
            InGameDetailFormat.StyleRizia);
        var iPtBlocks = RRParseBlocks(iOnlyPt, out why);
        var iOtBlocks = RRParseBlocks(iOnlyOt, out why);
        SameInt("⑮-I8 ④只有每回合类 ⇒ 块数 = 1、块头 = Per Turn:、零空行、无 This Turn: 块头",
            (iPtBlocks.Count == 1 ? 1 : 0) * 100
            + (iPtBlocks.Count == 1 && string.Equals(iPtBlocks[0].Head, InGameDetailFormat.RiziaPerTurnLabel, StringComparison.Ordinal) ? 10 : 0)
            + (CountOf(iOnlyPt, "\n\n") == 0 ? 1 : 0) * 1
            + (iOnlyPt.IndexOf(InGameDetailFormat.RiziaThisTurnLabel, StringComparison.Ordinal) < 0 ? 0 : 1000),
            (1 * 100) + (1 * 10) + 1);
        SameInt("⑮-I9 ④只有一次性类 ⇒ 块数 = 1、块头 = This Turn:、零空行、无 Per Turn: 块头",
            (iOtBlocks.Count == 1 ? 1 : 0) * 100
            + (iOtBlocks.Count == 1 && string.Equals(iOtBlocks[0].Head, InGameDetailFormat.RiziaThisTurnLabel, StringComparison.Ordinal) ? 10 : 0)
            + (CountOf(iOnlyOt, "\n\n") == 0 ? 1 : 0) * 1
            + (iOnlyOt.IndexOf(InGameDetailFormat.RiziaPerTurnLabel, StringComparison.Ordinal) < 0 ? 0 : 1000),
            (1 * 100) + (1 * 10) + 1);
        Same("⑮-I10 ④只有每回合类时块内形态与 2.9.0 逐字节相同（单块不带空行；标题由 title 参数决定，此处无标题）",
            iOnlyPt,
            "未来\n"
            + "<color=#0ED10E>(+3)</color> <u>Total:</u><indent=5%>\n"
            + "<color=#0ED10E>(+3)</color> 甲</indent>\n"
            + "<color=#D11111>(-1)</color> <u>Total:</u><indent=5%>\n"
            + "<color=#D11111>(-1)</color> 乙</indent>");

        
        string iZeroOt = InGameDetailFormat.Compose(L(P1("甲", 3), O1("丙", 0)), 3, true, null, null, false,
            InGameDetailFormat.StyleRizia);
        var iZeroBlocks = RRParseBlocks(iZeroOt, out why);
        SameInt("⑮-I11 ⑤一次性类全为 0 ⇒ 只有块①（块②整块不出现）",
            iZeroBlocks.Count * 10 + (iZeroBlocks.Count == 1 && string.Equals(iZeroBlocks[0].Head, InGameDetailFormat.RiziaPerTurnLabel, StringComparison.Ordinal) ? 1 : 0),
            1 * 10 + 1);

        
        var iRealBlocks = RRParseBlocks(mineB, out why);
        SameInt("⑮-I12 ⑥真机 Budget 夹具仍是**单块**（全为每回合类；这正是「改两类关系、不改任何一类样子」的证据）",
            iRealBlocks.Count * 10 + (iRealBlocks.Count == 1 && string.Equals(iRealBlocks[0].Head, InGameDetailFormat.RiziaPerTurnLabel, StringComparison.Ordinal) ? 1 : 0),
            1 * 10 + 1);
        SameInt("⑮-I13 ⑥真机夹具零空行（单块 ⇒ 不引入 RiziaBlockSeparator）", CountOf(mineB, "\n\n"), 0);

        
        var iMem = new InGameDetailFormat.DetailMergeMemory();
        object iSlot = new object();
        bool iHit;
        const string iTip = "政府预算 <alpha=#44><size=16>(min: -20, max: +50)";
        string i0 = iMem.Merge(iSlot, iTip, d, out iHit);
        string iCur = i0;
        int iSame = 0;
        for (int k = 0; k < 20; k++)
        {
            iCur = iMem.Merge(iSlot, iCur, d, out iHit);
            if (string.Equals(iCur, i0, StringComparison.Ordinal)) iSame++;
        }
        SameInt("⑮-I14 ⑦两块形态连续 20 轮合并逐字节相同（幂等仍成立）", iSame, 20);
        SameInt("⑮-I15 ⑦20 轮后两块块头各仍只出现 1 次（按**整行相等**计，标题里的字不算）",
            RRCountHeadLines(iCur, InGameDetailFormat.RiziaPerTurnLabel) * 10
            + RRCountHeadLines(iCur, InGameDetailFormat.RiziaThisTurnLabel), 1 * 10 + 1);
        SameInt("⑮-I16 ⑦20 轮后组尾 </indent> 仍是 4 个（不膨胀、不累积）",
            CountOf(iCur, InGameDetailFormat.IndentClose), 4);
        True("⑮-I17 ⑦20 轮后整段仍零可见标记",
            InGameDetailFormat.IsMarkerFree(iCur, out gwhy), gwhy ?? "零标记");
        True("⑮-I18 ⑦20 轮后我们的明细段仍是同一段（逐字节 == d）且该段 TMP 标签全部成对闭合",
            InGameDetailFormat.RichTagsClosed(d, out gwhy) && iCur.EndsWith(d, StringComparison.Ordinal),
            gwhy ?? "明细段标签成对、且 20 轮后仍是同一段");

        
        string iDet = InGameDetailFormat.Compose(dList, 10, true, InGameDetailFormat.PreviewTitle, null, false,
            InGameDetailFormat.StyleDetailed);
        SameInt("⑮-I19 ⑧Detailed 形态（2.7.8）里不得出现块头「未来」/「当前」（按**整行相等**计；"
            + "Detailed 自己的标题与【未来】【当前】段头不算 —— 本版只动 Rizia 形态）",
            RRCountHeadLines(iDet, InGameDetailFormat.RiziaPerTurnLabel)
            + RRCountHeadLines(iDet, InGameDetailFormat.RiziaThisTurnLabel), 0);
        Has("⑮-I20 ⑧Detailed 形态仍用【未来】段头（未被动过）", iDet, InGameDetailFormat.SectionLabelPerTurn);

        
        Console.WriteLine("");
        Console.WriteLine("【⑮-J 2.9.2 悬停性能：明细缓存键（失效判据逐字段穷尽）】");

        var k0 = new InGameDetailFormat.DetailCacheKey();
        k0.Rev = 7; k0.Style = InGameDetailFormat.StyleRizia; k0.Frame = false;
        k0.Preview = true; k0.HasSettle = true; k0.PosHex = "#0ED10E"; k0.NegHex = "#D11111";

        var kSame = k0;   
        True("⑮-J1 同一组入参 ⇒ 命中（悬停期间每帧取用都走这一支 ⇒ 零重算）",
            k0.Matches(kSame), "七个字段全等 ⇒ Matches = true");

        
        var muts = new List<KeyValuePair<string, InGameDetailFormat.DetailCacheKey>>();
        var m1 = k0; m1.Rev = 8; muts.Add(new KeyValuePair<string, InGameDetailFormat.DetailCacheKey>("Rev（明细版本号：结算/预演刷新）", m1));
        var m2 = k0; m2.Style = InGameDetailFormat.StyleDetailed; muts.Add(new KeyValuePair<string, InGameDetailFormat.DetailCacheKey>("Style（形态切换）", m2));
        var m3 = k0; m3.Frame = true; muts.Add(new KeyValuePair<string, InGameDetailFormat.DetailCacheKey>("Frame（文本框开关）", m3));
        var m4 = k0; m4.Preview = false; muts.Add(new KeyValuePair<string, InGameDetailFormat.DetailCacheKey>("Preview（前瞻口径 ⇒ 决定有没有标题）", m4));
        var m5 = k0; m5.HasSettle = false; muts.Add(new KeyValuePair<string, InGameDetailFormat.DetailCacheKey>("HasSettle（可显示内容位）", m5));
        var m6 = k0; m6.PosHex = "#010203"; muts.Add(new KeyValuePair<string, InGameDetailFormat.DetailCacheKey>("PosHex（正值色）", m6));
        var m7 = k0; m7.NegHex = null; muts.Add(new KeyValuePair<string, InGameDetailFormat.DetailCacheKey>("NegHex（负值色 → null = 回退常量）", m7));

        int stillHit = 0; string firstHit = null;
        for (int i = 0; i < muts.Count; i++)
            if (k0.Matches(muts[i].Value)) { stillHit++; if (firstHit == null) firstHit = muts[i].Key; }
        SameInt("⑮-J2 ★七个入参**逐个**翻转 ⇒ 全部判为「不命中」（穷尽，不是抽样）", stillHit, 0);
        True("⑮-J2b 守门：翻转表本身是 7 项（不是把断言写空了）", muts.Count == 7,
            firstHit == null ? "7 项全部判为不命中" : ("未判出：" + firstHit));

        True("⑮-J3 明细版本号变 ⇒ 必须重算（结算或预演刷新后绝不能拿旧串）", !k0.Matches(m1), "Rev 7→8");
        True("⑮-J4 形态切换 ⇒ 必须重算（Rizia ↔ Detailed 字节完全不同）", !k0.Matches(m2), "Style Rizia→Detailed");
        True("⑮-J5 色值变 ⇒ 必须重算（换统计实例后取色可能变）", !k0.Matches(m6), "#0ED10E→#010203");

        
        string jDeg = InGameDetailFormat.Compose(L(P1("甲", 1)), 0, false, null, null, false,
            InGameDetailFormat.StyleRizia);
        Same("⑮-J6 HasSettle=false ⇒ 仍是「未介入哨兵」空串（缓存键里带这一位，就是为了不让旧内容粘住）",
            jDeg, "");
    }

    

    
    static List<string> RRLabelsRewritten(List<RRLine> lines)
    {
        var outs = new List<string>();
        for (int i = 0; i < lines.Count; i++)
        {
            if (lines[i].Kind != 2) continue;
            var shown = InGameDetailFormat.Display(lines[i].Label);
            if (string.Equals(shown, lines[i].Label, StringComparison.Ordinal)) continue;
            outs.Add(lines[i].Label + " → " + shown);
        }
        return outs;
    }

    static string RRJoin(List<string> items)
    {
        if (items == null || items.Count == 0) return "（无）";
        return string.Join("；", items.ToArray());
    }

    
    static string RRFixtureHexes(List<RRLine> lines)
    {
        string pos = null, neg = null;
        for (int i = 0; i < lines.Count; i++)
        {
            if (lines[i].Kind != 1) continue;
            if (lines[i].Value > 0 && pos == null) pos = lines[i].Hex;
            if (lines[i].Value < 0 && neg == null) neg = lines[i].Hex;
        }
        return (pos ?? "?") + "|" + (neg ?? "?");
    }

    static bool RRHasNoZero(List<RRLine> lines)
    {
        for (int i = 0; i < lines.Count; i++) if (lines[i].Kind == 2 && lines[i].Value == 0) return false;
        return true;
    }

    
    static bool RREntryStrictAll(string rendered, out string why)
    {
        why = null;
        if (string.IsNullOrEmpty(rendered)) { why = "空串"; return false; }
        int n = 0;
        foreach (var raw in rendered.Split('\n'))
        {
            if (raw.Length == 0) continue;
            if (RRIsTitleOrRule(raw)) continue;                    
            if (raw.StartsWith("<color=#", StringComparison.Ordinal) == false) continue;
            var s = raw;
            if (s.EndsWith(InGameDetailFormat.IndentClose, StringComparison.Ordinal))
                s = s.Substring(0, s.Length - InGameDetailFormat.IndentClose.Length);
            if (RRHeadRe.IsMatch(s)) continue;                     
            if (RREntryRe.IsMatch(s)) { n++; continue; }            
            why = "不合形态的行：「" + raw + "」";
            return false;
        }
        why = n + " 条明细行全部是「<color=#RRGGBB>(±N)</color> 说明」形态";
        return n > 0;
    }

    
    static bool RRLabelsTagFree(string rendered, out string why)
    {
        why = null;
        if (string.IsNullOrEmpty(rendered)) { why = "空串"; return false; }
        int n = 0;
        foreach (var raw in rendered.Split('\n'))
        {
            if (raw.Length == 0) continue;
            if (RRIsTitleOrRule(raw)) continue;                  
            var s = raw;
            if (s.EndsWith(InGameDetailFormat.IndentClose, StringComparison.Ordinal))
                s = s.Substring(0, s.Length - InGameDetailFormat.IndentClose.Length);
            if (RRHeadRe.IsMatch(s)) continue;                  
            var me = RREntryRe.Match(s);
            if (!me.Success) continue;
            var label = me.Groups["label"].Value;
            if (label.IndexOf('<') >= 0 || label.IndexOf('>') >= 0)
            {
                why = "说明段里出现标签字符：「" + label + "」";
                return false;
            }
            n++;
        }
        why = n + " 条明细的说明段里零标签字符";
        return n > 0;
    }

    
    static int RRIndentAloneLines(string rendered)
    {
        int n = 0;
        foreach (var raw in rendered.Split('\n'))
        {
            if (raw.Length == 0) continue;
            if (raw.StartsWith(InGameDetailFormat.IndentClose, StringComparison.Ordinal)) n++;
        }
        return n;
    }

    
    
    
    
    
    
    
    
    static bool RRDescending(List<RRLine> lines, bool positive, out string why)
    {
        why = null;
        long prev = long.MaxValue;
        int n = 0, groups = 0;
        for (int i = 0; i < lines.Count; i++)
        {
            if (lines[i].Kind != 2) { prev = long.MaxValue; if (lines[i].Kind == 1) groups++; continue; }  
            if ((lines[i].Value > 0) != positive) continue;
            long mag = Math.Abs((long)lines[i].Value);
            if (mag > prev)
            {
                why = "第 " + (i + 1) + " 行 |" + mag + "| > 上一行 |" + prev + "|（同组内非降序）";
                return false;
            }
            prev = mag;
            n++;
        }
        why = n + " 条按 |值| 降序（跨 " + groups + " 个组，逐组判定）";
        return n > 0;
    }

    
    static int RREachLabelOnce(List<RRLine> lines, out string why)
    {
        why = null;
        var seen = new Dictionary<string, int>(StringComparer.Ordinal);
        int dup = 0;
        for (int i = 0; i < lines.Count; i++)
        {
            if (lines[i].Kind != 2) continue;
            int c;
            if (seen.TryGetValue(lines[i].Label, out c))
            {
                seen[lines[i].Label] = c + 1;
                if (c + 1 == 2) { dup++; why = "重复文案：" + lines[i].Label; }
                continue;
            }
            seen[lines[i].Label] = 1;
        }
        if (why == null) why = seen.Count + " 条文案各不相同";
        return dup;
    }

    
    
    

    
    
    
    
    
    
    
    
    
    static void RRLabelSets(List<InGameDetailFormat.Entry> entries,
                           out List<string> perTurn, out List<string> oneTime)
    {
        perTurn = new List<string>();
        oneTime = new List<string>();
        var rows = InGameDetailFormat.BuildRows(entries);
        for (int i = 0; i < rows.Count; i++)
        {
            if (rows[i].Delta == 0) continue;                       
            if (rows[i].Kind == InGameDetailFormat.KindOneTime) oneTime.Add(rows[i].Label);
            else perTurn.Add(rows[i].Label);
        }
    }

    
    
    
    
    
    
    
    
    
    static bool RRSeparation(List<RRBlock> blocks,
                             List<string> perTurnLabels, List<string> oneTimeLabels, out string why)
    {
        why = null;
        var pt = new HashSet<string>(perTurnLabels, StringComparer.Ordinal);
        var ot = new HashSet<string>(oneTimeLabels, StringComparer.Ordinal);
        var ptSeen = new HashSet<string>(StringComparer.Ordinal);
        var otSeen = new HashSet<string>(StringComparer.Ordinal);

        for (int b = 0; b < blocks.Count; b++)
        {
            bool perTurnBlock = string.Equals(blocks[b].Head, InGameDetailFormat.RiziaPerTurnLabel, StringComparison.Ordinal);
            bool oneTimeBlock = string.Equals(blocks[b].Head, InGameDetailFormat.RiziaThisTurnLabel, StringComparison.Ordinal);
            if (!perTurnBlock && !oneTimeBlock)
            { why = "第 " + (b + 1) + " 块的块头不是已知块头：「" + blocks[b].Head + "」"; return false; }

            for (int gi = 0; gi < blocks[b].Groups.Count; gi++)
            {
                var g = blocks[b].Groups[gi];
                for (int ei = 0; ei < g.Entries.Count; ei++)
                {
                    string lb = g.Entries[ei].Label;
                    if (perTurnBlock)
                    {
                        if (ot.Contains(lb)) { why = "★C1 违例：**一次性类**条目「" + lb + "」出现在 Per Turn: 块里"; return false; }
                        if (!pt.Contains(lb)) { why = "Per Turn: 块里出现归属不明的条目「" + lb + "」"; return false; }
                        ptSeen.Add(lb);
                    }
                    else
                    {
                        if (pt.Contains(lb)) { why = "★C1 违例：**每回合类**条目「" + lb + "」出现在 This Turn: 块里"; return false; }
                        if (!ot.Contains(lb)) { why = "This Turn: 块里出现归属不明的条目「" + lb + "」"; return false; }
                        otSeen.Add(lb);
                    }
                }
            }
        }

        if (!ptSeen.SetEquals(pt))
        {
            why = "每回合类 " + pt.Count + " 条里只有 " + ptSeen.Count + " 条进了 Per Turn: 块（缺："
                + RRFirstMissing(pt, ptSeen) + "）";
            return false;
        }
        if (!otSeen.SetEquals(ot))
        {
            why = "一次性类 " + ot.Count + " 条里只有 " + otSeen.Count + " 条进了 This Turn: 块（缺："
                + RRFirstMissing(ot, otSeen) + "）";
            return false;
        }
        why = "块① " + ptSeen.Count + " 条全为每回合类／块② " + otSeen.Count + " 条全为一次性类，零越界零缺失";
        return true;
    }

    
    static string RRFirstMissing(HashSet<string> want, HashSet<string> got)
    {
        foreach (var s in want) if (!got.Contains(s)) return s;
        return "（无）";
    }

    
    
    
    
    static bool RRBlockHeadsOk(List<RRBlock> blocks, out string why)
    {
        why = null;
        if (blocks.Count == 0) { why = "没有任何块"; return false; }
        if (blocks.Count > 2) { why = "块数 = " + blocks.Count + "（最多两块）"; return false; }
        var seen = new HashSet<string>(StringComparer.Ordinal);
        for (int b = 0; b < blocks.Count; b++)
        {
            if (!seen.Add(blocks[b].Head)) { why = "块头「" + blocks[b].Head + "」出现了不止一次"; return false; }
            if (blocks[b].Groups.Count == 0) { why = "块「" + blocks[b].Head + "」只有一个光秃秃的块头（没有任何组）"; return false; }
        }
        if (blocks.Count == 2 && !string.Equals(blocks[0].Head, InGameDetailFormat.RiziaPerTurnLabel, StringComparison.Ordinal))
        { why = "两块时块①必须是 Per Turn:，实际是「" + blocks[0].Head + "」"; return false; }
        why = blocks.Count + " 个块：" + RRHeads(blocks) + "（各一次、顺序正确、每块都有组）";
        return true;
    }

    
    static string RRHeads(List<RRBlock> blocks)
    {
        var parts = new List<string>();
        for (int i = 0; i < blocks.Count; i++) parts.Add(blocks[i].Head);
        return string.Join(" → ", parts.ToArray());
    }

    
    
    
    
    
    
    
    static int RRCountHeadLines(string rendered, string s)
    {
        if (string.IsNullOrEmpty(rendered) || string.IsNullOrEmpty(s)) return 0;
        int n = 0;
        foreach (var ln in rendered.Split('\n')) if (string.Equals(ln, s, StringComparison.Ordinal)) n++;
        return n;
    }

    
    
    
    
    static bool RRGroupSubtotalsOk(List<RRBlock> blocks, out string why)
    {
        why = null;
        int n = 0;
        for (int b = 0; b < blocks.Count; b++)
        {
            for (int gi = 0; gi < blocks[b].Groups.Count; gi++)
            {
                var g = blocks[b].Groups[gi];
                string at = "第 " + (b + 1) + " 块第 " + (gi + 1) + " 组";
                if (g.Entries.Count == 0) { why = at + "里一条明细都没有"; return false; }

                int sum = 0;
                bool anyPos = false, anyNeg = false;
                for (int ei = 0; ei < g.Entries.Count; ei++)
                {
                    sum += g.Entries[ei].Value;
                    if (g.Entries[ei].Value > 0) anyPos = true;
                    if (g.Entries[ei].Value < 0) anyNeg = true;
                }
                if (anyPos && anyNeg) { why = at + "里正负混排（组应当是单一符号）"; return false; }
                if (sum != g.Subtotal) { why = at + "：组头小计 " + g.Subtotal + " != 组内各条之和 " + sum; return false; }

                string wantHex = anyPos ? InGameDetailFormat.RiziaPositiveColor : InGameDetailFormat.RiziaNegativeColor;
                if (!string.Equals(g.Hex, wantHex, StringComparison.Ordinal))
                { why = at + "：取色 " + g.Hex + " 与符号不符（应为 " + wantHex + "）"; return false; }
                n++;
            }
        }
        why = n + " 个组：组头小计 == 组内之和，且组内单一符号、取色与符号相符";
        return n > 0;
    }

    
    
    
    static bool RRIndentPairsOk(string rendered, List<RRBlock> blocks, out string why)
    {
        why = null;
        int groups = 0;
        for (int b = 0; b < blocks.Count; b++) groups += blocks[b].Groups.Count;
        int open = CountOf(rendered, InGameDetailFormat.IndentOpen);
        int close = CountOf(rendered, InGameDetailFormat.IndentClose);
        if (open != groups || close != groups)
        { why = "indent 开 " + open + " / 闭 " + close + "，组数 " + groups + "（三者必须相等）"; return false; }
        why = "indent 对 " + groups + " == 组数 " + groups;
        return true;
    }

    
    
    
    
    static bool RRLineCountOk(string rendered, List<RRBlock> blocks, out string why)
    {
        why = null;
        int groups = 0, entries = 0;
        for (int b = 0; b < blocks.Count; b++)
        {
            groups += blocks[b].Groups.Count;
            for (int gi = 0; gi < blocks[b].Groups.Count; gi++) entries += blocks[b].Groups[gi].Entries.Count;
        }
        int prefix = (blocks.Count > 0 && blocks[0].Title != null) ? 2 : 0;   
        int expect = prefix + blocks.Count + groups + entries + Math.Max(0, blocks.Count - 1);
        int got = rendered.Split('\n').Length;
        if (got != expect) { why = "行数 = " + got + "，按结构应为 " + expect + "（标题前缀 " + prefix + " + 块头 " + blocks.Count + " + 组头 " + groups + " + 明细 " + entries + " + 空行 " + Math.Max(0, blocks.Count - 1) + "）"; return false; }
        why = got + " 行 = 标题前缀 " + prefix + " + 块头 " + blocks.Count + " + 组头 " + groups + " + 明细 " + entries + " + 块间空行 " + Math.Max(0, blocks.Count - 1);
        return true;
    }

    
    
    
    static bool RRBlankLinesOk(string rendered, List<RRBlock> blocks, out string why)
    {
        why = null;
        int blanks = 0;
        foreach (var ln in rendered.Split('\n')) if (ln.Length == 0) blanks++;
        int want = Math.Max(0, blocks.Count - 1);
        if (blanks != want) { why = "空行 = " + blanks + "，块数 " + blocks.Count + " 时应为 " + want; return false; }
        why = "空行 " + blanks + " == 块数 " + blocks.Count + " - 1";
        return true;
    }
}
