
















using System;
using System.Collections.Generic;
using Oikonomia;

partial class Program
{
    
    struct FScene
    {
        public string Id;
        public string Flat;      
        public string Framed;    
    }

    static void AddScene(List<FScene> list, string id,
                         List<InGameDetailFormat.Entry> e, int total, bool hasSettle, string title)
    {
        var s = new FScene();
        s.Id = id;
        s.Flat = title == null
            ? InGameDetailFormat.Compose(e, total, hasSettle)
            : InGameDetailFormat.Compose(e, total, hasSettle, title, null);
        s.Framed = InGameDetailFormat.Compose(e, total, hasSettle, title, null, true);
        list.Add(s);
    }

    static InGameDetailFormat.Entry FE(string t, int d, int kind, string g)
    { return new InGameDetailFormat.Entry(t, d, kind, g); }

    
    static List<InGameDetailFormat.Entry> FAll41()
    {
        return L(
            P1("ED=危险", -2), P1("ED=不稳定", -1), P1("ED=稳定", 0), P1("ED=增长", 1), P1("ED=极快增长", 1),
            P1("旅游业=蓬勃发展", 1), P1("旅游业=平均水平", 0), P1("旅游业=严重衰退", -1),
            P1("贸易量=增加", 2), P1("贸易量=恢复", 1), P1("贸易量=减少", 0),
            P1("税效率", 1), P1("逃税", -1), P1("腐败", -1),
            G1("免费处方药(每回合)", -1, InGameDetailFormat.KindPerTurn, InGameDetailFormat.GroupExtraWelfare),
            O1("避税(一次性)", -1),
            O1("高就业", 1), O1("产量增加", 1), O1("小企业成长", 1), O1("大企业创造就业", 1),
            O1("军工复合体", 1), O1("阿卡西亚投资", 1), O1("康塔纳投资", 1),
            O1("韦伦·石油自由贸易", 1), O1("阿格诺利亚·农业市场", 1), O1("伦堡·天然气管道", 1),
            O1("莱斯皮亚·煤炭贸易", 1), O1("莱斯皮亚·鱼类贸易", 1), O1("莱斯皮亚·钨贸易", 1),
            O1("莱斯皮亚·小麦贸易", 1), O1("莱斯皮亚·石油出售", 1), O1("莱斯皮亚自由贸易区", 1),
            O1("瓦尔戈斯兰·煤炭贸易", 1), O1("瓦尔戈斯兰·鱼类贸易", 1), O1("瓦尔戈斯兰·钨贸易", 1),
            O1("瓦尔戈斯兰·小麦贸易", 1), O1("瓦尔戈斯兰自由贸易区", 1),
            O1("贝吉亚↑1档", 1), O1("贝吉亚↑2档", 2), O1("洛伦↓1档", -1), O1("洛伦↓2档", -2));
    }

    
    static readonly string[] FAll41Texts = new string[]
    {
        "经济状态濒临崩溃", "经济状态动荡不安", "经济状态保持稳定", "经济状态日益增长", "经济状态高速增长",
        "全国旅游业蓬勃发展", "全国旅游业平稳发展", "全国旅游业持续衰退",
        "对外贸易额显著增加", "对外贸易额逐步恢复", "对外贸易额出现萎缩",
        "税收征管效率提升", "逃税现象造成财政损失", "腐败侵蚀行政效率", "额外福利支出",
        "资本避税问题", "就业率显著提高", "工业产量增加", "中小企业蓬勃发展", "大型企业创造就业",
        "军工复合体扩张", "阿卡西亚投资涌入", "康塔纳投资涌入",
        "与韦伦达成石油自由贸易", "与阿格诺利亚开放农业市场", "与伦堡签署天然气管道协议",
        "与莱斯皮亚达成煤炭贸易", "与莱斯皮亚达成鱼类贸易", "与莱斯皮亚达成钨贸易",
        "与莱斯皮亚达成小麦贸易", "与莱斯皮亚达成石油出售", "与莱斯皮亚建立自由贸易区",
        "与瓦尔戈斯兰达成煤炭贸易", "与瓦尔戈斯兰达成鱼类贸易", "与瓦尔戈斯兰达成钨贸易",
        "与瓦尔戈斯兰达成小麦贸易", "与瓦尔戈斯兰建立自由贸易区",
        "贝吉亚地区发展提速", "贝吉亚地区发展大幅提速（2 档）",
        "洛伦地区发展倒退", "洛伦地区发展大幅倒退（2 档）",
    };

    
    static List<FScene> FrameScenes()
    {
        var list = new List<FScene>();
        string T = InGameDetailFormat.PreviewTitle;
        string W = InGameDetailFormat.GroupExtraWelfare;

        AddScene(list, "F1 两段都有（标题 + 段间分隔线）",
            L(P1("ED=增长", 1), P1("旅游业=蓬勃发展", 1),
              G1("免费处方药(每回合)", -1, 0, W), O1("贝吉亚↑1档", 1), O1("避税(一次性)", -1)),
            1, true, T);

        AddScene(list, "F2 只有每回合段",
            L(P1("ED=增长", 2), P1("税效率", 3)), 5, true, T);

        AddScene(list, "F3 只有一次性段（无段间分隔线）",
            L(O1("贝吉亚↑1档", 4), O1("避税(一次性)", -5)), -1, true, T);

        AddScene(list, "F4 无标题（3 参路 + 框架）",
            L(P1("ED=增长", 1), O1("避税(一次性)", -1)), 0, true, null);

        AddScene(list, "F5 长文案（洛伦地区发展大幅倒退（2 档）→ 加宽整个框）",
            L(P1("ED=增长", 1), P1("逃税", -1), O1("洛伦↓2档", -2)), -2, true, T);

        AddScene(list, "F6 全 41 条同时上屏（最宽形态）", FAll41(), 12, true, T);

        AddScene(list, "F7 零值灰行 + 未知标签 + < > & 转义",
            L(P1("ED=稳定", 0), P1("某个没见过的词条", 2), P1("<B>C&D", -1)), 1, true, T);

        AddScene(list, "F8 无分项但 total≠0（只剩标题）", L(), 5, true, T);

        AddScene(list, "F9 大数值（多位数不串位）",
            L(P1("ED=增长", 123), P1("逃税", -456), O1("贝吉亚↑2档", 78)), -255, true, T);

        return list;
    }

    

    
    static string FEsc(string s)
    {
        if (s == null) return "(null)";
        return s.Replace("\\", "\\\\").Replace("\r", "\\r").Replace("\n", "\\n");
    }

    
    static string FCode(string s, int i)
    {
        if (s == null) return "(null)";
        if (i < 0 || i >= s.Length) return "[越界，长度 " + s.Length + "]";
        char c = s[i];
        return "[U+" + ((int)c).ToString("X4") + " '" + (c == '\n' ? "\\n" : (c == '\r' ? "\\r" : c.ToString())) + "']";
    }

    
    
    static string FDecode(string s)
    {
        if (string.IsNullOrEmpty(s)) return s;
        return s.Replace("&lt;", "<").Replace("&gt;", ">").Replace("&amp;", "&");
    }

    
    static string FVisible(string rawLine) { return FDecode(StripTags(rawLine)); }

    
    static string[] FLines(string rendered)
    {
        var outs = new List<string>();
        if (rendered == null) return outs.ToArray();
        foreach (var raw in rendered.Split('\n'))
        {
            if (raw.Length == 0) continue;
            outs.Add(FVisible(raw));
        }
        return outs.ToArray();
    }

    
    static bool FIsRule(string v)
    {
        if (string.IsNullOrEmpty(v) || v.Length < 2) return false;
        if (v[0] != InGameDetailFormat.FrameCorner || v[v.Length - 1] != InGameDetailFormat.FrameCorner) return false;
        for (int i = 1; i < v.Length - 1; i++) if (v[i] != InGameDetailFormat.FrameHorizontal) return false;
        return true;
    }

    
    static bool FIsContent(string v)
    {
        return !string.IsNullOrEmpty(v) && v.Length >= 2
            && v[0] == InGameDetailFormat.FrameVertical
            && v[v.Length - 1] == InGameDetailFormat.FrameVertical;
    }

    
    static bool FAllSameWidth(string[] lines, out int w, out string why)
    {
        w = -1; why = null;
        if (lines.Length == 0) { why = "没有可见行"; return false; }
        for (int i = 0; i < lines.Length; i++)
        {
            int x = InGameDetailFormat.DisplayWidth(lines[i]);
            if (w < 0) { w = x; continue; }
            if (x != w)
            {
                why = "第 " + (i + 1) + " 行宽 " + x + " ≠ 第 1 行宽 " + w + "：" + lines[i];
                return false;
            }
        }
        why = w + " 列 × " + lines.Length + " 行全部相等";
        return true;
    }

    
    static string FInner(string contentLine) { return contentLine.Substring(1, contentLine.Length - 2); }

    
    static string FBody(string contentLine) { return FInner(contentLine).Trim(); }

    
    static bool FBodyIsParenRow(string body, out string why)
    {
        return IsParenRow("<color=#000000>" + body + "</color>", out why);
    }

    
    static string FTitleLine(string[] lines, string title)
    {
        foreach (var v in lines)
            if (FIsContent(v) && FInner(v).IndexOf(title, StringComparison.Ordinal) >= 0) return v;
        return null;
    }

    
    static void FrameSuite()
    {
        var scenes = FrameScenes();

        
        Console.WriteLine("");
        Console.WriteLine("【⑬-A 开关关闭（frame=false）→ 与 2.7.8 基线逐字节相同】");

        string basePath = System.IO.Path.Combine(AppContext.BaseDirectory, "baseline-278.txt");
        True("⑬-A0 基线夹具在输出目录里（由**改动源码之前**的 2.7.8 源码 dump 得到）",
            System.IO.File.Exists(basePath), basePath);

        var golden = new Dictionary<string, string>(StringComparer.Ordinal);
        if (System.IO.File.Exists(basePath))
        {
            foreach (var ln in System.IO.File.ReadAllLines(basePath))
            {
                if (ln.Length == 0 || ln[0] != '[') continue;
                int rb = ln.IndexOf(']');
                if (rb < 0) continue;
                int sep = ln.IndexOf(" :: ", StringComparison.Ordinal);
                if (sep < 0) continue;
                golden[ln.Substring(1, rb - 1)] = ln.Substring(sep + 4);
            }
        }
        SameInt("⑬-A1 基线里读到 18 个场景（不是空跑）", golden.Count, 18);

        var baseScenes = BaselineScenesEscaped();
        SameInt("⑬-A2 本测试复现的基线场景数", baseScenes.Count, 18);
        foreach (var kv in baseScenes)
        {
            
            string got = kv.Value;
            string exp;
            bool has = golden.TryGetValue(kv.Key, out exp);
            bool same = has && string.Equals(got, exp, StringComparison.Ordinal);
            string note;
            if (!has) note = "基线里没有这个场景";
            else if (same) note = "逐字节相同（" + got.Length + " 字符）";
            else
            {
                int d = 0, n = Math.Min(got.Length, exp.Length);
                while (d < n && got[d] == exp[d]) d++;
                note = "首个差异 @" + d + "（实际长 " + got.Length + " / 基线长 " + exp.Length + "）："
                     + "实际" + FCode(got, d) + " vs 基线" + FCode(exp, d);
            }
            True("⑬-A3[" + kv.Key + "] frame=false 输出 == 2.7.8 基线逐字节", same, note);
        }

        
        string degenFlat = InGameDetailFormat.Compose(L(), 0, true, InGameDetailFormat.PreviewTitle, null);
        string degenFramed = InGameDetailFormat.Compose(L(), 0, true, InGameDetailFormat.PreviewTitle, null, true);
        Same("⑬-A4 全 0 退化：开/关框架输出逐字节相同（不画框）", degenFramed, degenFlat);
        Same("⑬-A5 全 0 退化 == 2.7.8 原样（无末尾换行）",
            degenFramed, "<color=#9A9A9A>本回合无变动</color>");
        Same("⑬-A6 未结算哨兵：开/关框架都是空串（不接管游戏文本）",
            InGameDetailFormat.Compose(L(), 0, false, InGameDetailFormat.PreviewTitle, null, true), "");

        True("⑬-A7 守门断言：框架开关**真的起作用**（同一输入的开/关输出必须不同）",
            scenes[0].Flat != scenes[0].Framed,
            "关 = " + scenes[0].Flat.Length + " 字符；开 = " + scenes[0].Framed.Length + " 字符");

        
        Console.WriteLine("");
        Console.WriteLine("【⑬-B 所有行显示宽度严格相等（本版核心不变量）】");
        int minLines = int.MaxValue;
        for (int i = 0; i < scenes.Count; i++)
        {
            var lines = FLines(scenes[i].Framed);
            int w; string why;
            bool ok = FAllSameWidth(lines, out w, out why);
            True("⑬-B" + (i + 1) + " " + scenes[i].Id + "：所有行显示宽度相等", ok, why);
            if (lines.Length < minLines) minLines = lines.Length;
        }
        True("⑬-B10 守门断言：每个场景都至少 4 行（不是空断言）", minLines >= 4,
            "最少行数 = " + minLines);
        int wDummy; string whyDummy;
        True("⑬-B11 守门断言：等宽断言**能**判出不等宽（对人工造的错行必须为假）",
            !FAllSameWidth(new string[] { "+--+", "+---+", "+--+" }, out wDummy, out whyDummy),
            whyDummy ?? "");

        
        Console.WriteLine("");
        Console.WriteLine("【⑬-C 框线与内容列对齐（左/右框线列固定）】");
        for (int i = 0; i < scenes.Count; i++)
        {
            var lines = FLines(scenes[i].Framed);
            int bad = 0;
            string detail = null;
            for (int k = 0; k < lines.Length; k++)
            {
                bool rule = FIsRule(lines[k]);
                bool content = FIsContent(lines[k]);
                if (!rule && !content) { bad++; if (detail == null) detail = "第 " + (k + 1) + " 行既非横线也非内容行：" + lines[k]; }
                else if (rule && (lines[k][0] != InGameDetailFormat.FrameCorner)) { bad++; }
                else if (content && (lines[k][0] != InGameDetailFormat.FrameVertical || lines[k][lines[k].Length - 1] != InGameDetailFormat.FrameVertical)) { bad++; }
            }
            True("⑬-C" + (i + 1) + " " + scenes[i].Id + "：每行都是「横线」或「| 内容 |」（左右框线列固定）",
                bad == 0, bad == 0 ? ("首行 = " + lines[0] + "；末行 = " + lines[lines.Length - 1]) : detail);

            True("⑬-C" + (i + 1) + "b " + scenes[i].Id + "：首行与末行都是完整横线（上/下边框）",
                FIsRule(lines[0]) && FIsRule(lines[lines.Length - 1]),
                "首 = " + lines[0].Substring(0, Math.Min(12, lines[0].Length)) + "…");
        }

        
        Console.WriteLine("");
        Console.WriteLine("【⑬-D 标题在框内按显示宽度居中（±1 列取整口径；不依赖 <align=center>）】");
        string T2 = InGameDetailFormat.PreviewTitle;
        for (int i = 0; i < scenes.Count; i++)
        {
            var lines = FLines(scenes[i].Framed);
            string tl = FTitleLine(lines, T2);
            if (tl == null) continue;
            string inner = FInner(tl);
            int idx = inner.IndexOf(T2, StringComparison.Ordinal);
            int left = InGameDetailFormat.DisplayWidth(inner.Substring(0, idx));
            int right = InGameDetailFormat.DisplayWidth(inner.Substring(idx + T2.Length));
            True("⑬-D" + (i + 1) + " " + scenes[i].Id + "：标题在框内居中（左右空格差 ≤ 1 列，左取整）",
                Math.Abs(left - right) <= 1 && left >= InGameDetailFormat.FramePadLeft,
                "左 " + left + " 列 / 右 " + right + " 列（差 " + Math.Abs(left - right) + "）");
        }
        True("⑬-D10 框架形态**不使用** <align=center>（空格居中自足，少一个标签少一种意外）",
            scenes[0].Framed.IndexOf("<align", StringComparison.Ordinal) < 0,
            "框架产物里 <align 计数 = " + CountOf(scenes[0].Framed, "<align"));
        True("⑬-D11 对照：同一输入关闭框架时**仍**带 <align=center>（2.7.8 形态未被动过）",
            scenes[0].Flat.IndexOf("<align=center>", StringComparison.Ordinal) >= 0,
            "纯文本产物里 <align=center> 计数 = " + CountOf(scenes[0].Flat, "<align=center>"));
        SameInt("⑬-D12 框架里标题恰好出现 1 次（没有把段头也居中了）",
            CountOf(scenes[0].Framed, T2), 1);

        
        Console.WriteLine("");
        Console.WriteLine("【⑬-E 长文案不撑破边框（加宽整个框，不截断）】");
        var longScene = scenes[4];                       
        var lLines = FLines(longScene.Framed);
        string plainLong = Plain(longScene.Framed);
        Has("⑬-E1 长文案完整在屏上（含全角括号与「2 档」）",
            plainLong, "洛伦地区发展大幅倒退（2 档）");
        Has("⑬-E2 长文案行完整保留「(值 说明)」形态（不因超宽被截断）",
            plainLong, "(-2 洛伦地区发展大幅倒退（2 档）)");

        int longW = 0;
        foreach (var v in lLines)
        {
            if (!FIsContent(v)) continue;
            int cw = InGameDetailFormat.DisplayWidth(FBody(v));
            if (cw > longW) longW = cw;
        }
        int innerW = InGameDetailFormat.DisplayWidth(lLines[0]) - 2;
        SameInt("⑬-E3 框内宽 == 最宽可见正文 + 条目缩进 + 左右内边距（**框随最宽行加宽**，恰好贴住）",
            innerW, longW + InGameDetailFormat.FrameBodyIndent
                    + InGameDetailFormat.FramePadLeft + InGameDetailFormat.FramePadRight);
        True("⑬-E4 长文案行的可见宽度 > 标题宽度（证明这条确实是最宽行、且框确实被它撑宽了）",
            longW > InGameDetailFormat.DisplayWidth(T2),
            "最宽正文 " + longW + " 列 > 标题 " + InGameDetailFormat.DisplayWidth(T2) + " 列");

        string longRow = null;
        foreach (var v in lLines)
            if (FIsContent(v) && FBody(v).IndexOf("洛伦地区发展大幅倒退", StringComparison.Ordinal) >= 0) longRow = v;
        True("⑬-E5 长文案行既**没被截断**、右框线也落在同一列（正文逐字完整 + 行宽等于框宽）",
            longRow != null
            && FBody(longRow) == "(-2 洛伦地区发展大幅倒退（2 档）)"
            && InGameDetailFormat.DisplayWidth(longRow) == InGameDetailFormat.DisplayWidth(lLines[0]),
            longRow == null ? "(没找到长文案行)" : ("正文 = |" + FBody(longRow) + "|"));

        
        Console.WriteLine("");
        Console.WriteLine("【⑬-F 接口：开关关闭与既有重载逐字节一致】");
        var e0 = L(P1("ED=增长", 1), O1("避税(一次性)", -1));
        Same("⑬-F1 5 参重载（无 frame）== 显式 frame:false",
            InGameDetailFormat.Compose(e0, 0, true, T2, null),
            InGameDetailFormat.Compose(e0, 0, true, T2, null, false));
        Same("⑬-F2 3 参重载 == 显式 frame:false（无标题）",
            InGameDetailFormat.Compose(e0, 0, true),
            InGameDetailFormat.Compose(e0, 0, true, null, null, false));
        string noTitleFramed = InGameDetailFormat.Compose(e0, 0, true, null, null, true);
        True("⑬-F3 3 参 + 框架：没有标题行（不写空标题），但框与两段都在",
            noTitleFramed.IndexOf(T2, StringComparison.Ordinal) < 0
            && noTitleFramed.IndexOf("【未来】", StringComparison.Ordinal) >= 0
            && noTitleFramed.IndexOf("【当前】", StringComparison.Ordinal) >= 0
            && FLines(noTitleFramed)[0][0] == InGameDetailFormat.FrameCorner,
            "首行 = " + FLines(noTitleFramed)[0]);

        
        Console.WriteLine("");
        Console.WriteLine("【⑬-G 富文本卫生（标签闭合 / 零标记 / 框线恒 1 列 / 不引入 align）】");
        string gwhy;
        SameInt("⑬-G1 框线字符 DisplayWidth 恒为 1 列（横 / 竖 / 岔口）",
            InGameDetailFormat.DisplayWidth(InGameDetailFormat.FrameHorizontal.ToString())
            + InGameDetailFormat.DisplayWidth(InGameDetailFormat.FrameVertical.ToString())
            + InGameDetailFormat.DisplayWidth(InGameDetailFormat.FrameCorner.ToString()), 3);
        True("⑬-G2 框线字符全是 ASCII（East_Asian_Width = Na ⇒ **任何字体**都是 1 列，与 DisplayWidth 口径永远一致）",
            InGameDetailFormat.FrameHorizontal < 0x80
            && InGameDetailFormat.FrameVertical < 0x80
            && InGameDetailFormat.FrameCorner < 0x80,
            "码位 = U+" + ((int)InGameDetailFormat.FrameHorizontal).ToString("X2")
            + " / U+" + ((int)InGameDetailFormat.FrameVertical).ToString("X2")
            + " / U+" + ((int)InGameDetailFormat.FrameCorner).ToString("X2"));
        True("⑬-G3 内边距是半角空格且 ≥ 1（左 " + InGameDetailFormat.FramePadLeft + " / 右 " + InGameDetailFormat.FramePadRight + "）",
            InGameDetailFormat.FramePadLeft >= 1 && InGameDetailFormat.FramePadRight >= 1, "");

        for (int i = 0; i < scenes.Count; i++)
        {
            string f = scenes[i].Framed;
            True("⑬-G4" + (i + 1) + " " + scenes[i].Id + "：TMP 标签全部成对闭合",
                InGameDetailFormat.RichTagsClosed(f, out gwhy), gwhy ?? "全部闭合");
            True("⑬-G5" + (i + 1) + " " + scenes[i].Id + "：整段零可见标记字符",
                !InGameDetailFormat.ContainsVisibleMarker(f, out gwhy), gwhy ?? "零标记");
            SameInt("⑬-G6" + (i + 1) + " " + scenes[i].Id + "：<color> 开闭数量相等",
                CountOf(f, "<color=") - CountOf(f, "</color>"), 0);
        }

        
        Console.WriteLine("");
        Console.WriteLine("【⑬-H 幂等：框架产物连续 20 轮合并逐字节相同】");
        const string gameTip = "政府预算 <alpha=#44><size=16>(min: -20, max: +50)</size></alpha>";
        var memF = new InGameDetailFormat.DetailMergeMemory();
        object slotF = new object();
        bool hitF;
        string f0 = memF.Merge(slotF, gameTip, scenes[0].Framed, out hitF);
        string cursorF = f0;
        int sameF = 0;
        for (int k = 0; k < 20; k++)
        {
            bool h;
            cursorF = memF.Merge(slotF, cursorF, scenes[0].Framed, out h);
            if (string.Equals(cursorF, f0, StringComparison.Ordinal)) sameF++;
        }
        SameInt("⑬-H1 连续 20 轮合并逐字节相同（框架不破坏幂等）", sameF, 20);
        Same("⑬-H2 游戏原文仍是逐字节前缀", cursorF.Substring(0, gameTip.Length), gameTip);
        SameInt("⑬-H3 20 轮后标题只出现 1 次", CountOf(cursorF, T2), 1);
        True("⑬-H4 20 轮后整段仍零可见标记",
            !InGameDetailFormat.ContainsVisibleMarker(cursorF, out gwhy), gwhy ?? "零标记");
        True("⑬-H5 框架产物比纯文本产物长（框线确实写进去了），且没有膨胀",
            f0.Length - gameTip.Length - 2 == scenes[0].Framed.Length,
            "合并段 = " + (f0.Length - gameTip.Length - 2) + " 字符；纯文本 = " + scenes[0].Flat.Length + " 字符");

        
        Console.WriteLine("");
        Console.WriteLine("【⑬-I 内容零改动（41 条文案 · 两段归属 · 行格式 · 小计 · 颜色）】");
        string allFramed = scenes[5].Framed;             
        string allPlain = Plain(allFramed);
        int missing = 0;
        string firstMissing = null;
        for (int i = 0; i < FAll41Texts.Length; i++)
        {
            if (allPlain.IndexOf(FAll41Texts[i], StringComparison.Ordinal) < 0)
            {
                missing++;
                if (firstMissing == null) firstMissing = FAll41Texts[i];
            }
        }
        SameInt("⑬-I1 41 条文案在框架形态下**逐条**仍上屏（一条不缺）", missing, 0);
        True("⑬-I2 文案表本身是 41 条（守门：不是把断言写空了）", FAll41Texts.Length == 41,
            firstMissing == null ? "41 条全部命中" : ("首条缺失 = " + firstMissing));

        int iFuture = allPlain.IndexOf("【未来】", StringComparison.Ordinal);
        int iNow = allPlain.IndexOf("【当前】", StringComparison.Ordinal);
        int iPerturn = allPlain.IndexOf("经济状态日益增长", StringComparison.Ordinal);
        int iOnetime = allPlain.IndexOf("资本避税问题", StringComparison.Ordinal);
        True("⑬-I3 两段都在且顺序不变（【未来】在【当前】之前）",
            iFuture >= 0 && iNow >= 0 && iFuture < iNow,
            "【未来】@" + iFuture + " ＜ 【当前】@" + iNow);
        True("⑬-I4 归属不变：每回合项落在【未来】段，一次性项落在【当前】段",
            iPerturn > iFuture && iPerturn < iNow && iOnetime > iNow,
            "每回合项@" + iPerturn + "（段内）｜一次性项@" + iOnetime + "（段内）");

        SameInt("⑬-I5 两段小计都在（每条小计一行）", CountOf(allPlain, "小计"), 2);
        Has("⑬-I6 小计格式未变：值与标签同在括号内、带 ── 前导",
            allPlain, "── (+20 小计)");

        
        
        var aLines = FLines(allFramed);
        int bodyRows = 0, badRows = 0;
        string badWhy = null;
        for (int i = 0; i < aLines.Length; i++)
        {
            if (!FIsContent(aLines[i])) continue;
            if (FInner(aLines[i]).IndexOf(T2, StringComparison.Ordinal) >= 0) continue;   
            string body = FBody(aLines[i]);
            if (body.Length == 0) continue;
            if (body.StartsWith("【", StringComparison.Ordinal)) continue;                
            bodyRows++;
            string w2;
            bool isSub = body.StartsWith("── (", StringComparison.Ordinal)
                      && body.EndsWith(" 小计)", StringComparison.Ordinal);
            if (isSub) continue;
            if (!FBodyIsParenRow(body, out w2)) { badRows++; if (badWhy == null) badWhy = body + " → " + w2; }
        }
        True("⑬-I7 框架内每一条明细行仍是「(值 说明)」字节形态（金额在行首括号内）",
            badRows == 0, badRows == 0 ? (bodyRows + " 条明细/小计行全部合规") : badWhy);
        SameInt("⑬-I8 明细 + 小计行数 = 41 + 2", bodyRows, 43);

        Same("⑬-I9 颜色映射未变：正值行取绿", ColorOfLine(allFramed, "经济状态日益增长"), "P");
        Same("⑬-I10 颜色映射未变：负值行取红", ColorOfLine(allFramed, "经济状态濒临崩溃"), "N");
        Same("⑬-I11 颜色映射未变：零值行取灰", ColorOfLine(allFramed, "经济状态保持稳定"), "Z");
        Same("⑬-I12 颜色映射未变：段头取灰（不与数值抢色）", ColorOfLine(allFramed, "【未来】"), "Z");
        SameInt("⑬-I13 颜色标签成对（框架每行都带自己的色标）",
            CountOf(allFramed, "<color=") - CountOf(allFramed, "</color>"), 0);

        NotHas("⑬-I14 框架形态里仍**不含合计行**（2.7.8 的取消结论保持）", allPlain, "Total:");
        NotHas("⑬-I15 框架形态里也不出现中文「合计」/「预计」", allPlain, "合计");
        NotHas("⑬-I16 框架形态里也不出现中文「预计」", allPlain, "预计");
    }

    
    
    
    static List<KeyValuePair<string, string>> BaselineScenesEscaped()
    {
        var outs = new List<KeyValuePair<string, string>>();
        string T = InGameDetailFormat.PreviewTitle;
        string W = InGameDetailFormat.GroupExtraWelfare;

        
        
        outs.Add(new KeyValuePair<string, string>("S01", FEsc(InGameDetailFormat.Compose(
            L(P1("ED=增长", 1), P1("旅游业=蓬勃发展", 1), G1("免费处方药(每回合)", -1, 0, W),
              O1("贝吉亚↑1档", 1), O1("避税(一次性)", -1)), 1, true))));

        outs.Add(new KeyValuePair<string, string>("S02", FEsc(InGameDetailFormat.Compose(
            L(P1("ED=增长", 1), P1("旅游业=蓬勃发展", 1), G1("免费处方药(每回合)", -1, 0, W),
              O1("贝吉亚↑1档", 1), O1("避税(一次性)", -1)), 1, true, T, null))));

        outs.Add(new KeyValuePair<string, string>("S03", FEsc(InGameDetailFormat.Compose(
            L(P1("ED=增长", 1), P1("旅游业=蓬勃发展", -2),
              G1("免费处方药(每回合)", 3, 0, W)), 1, true, T, null))));

        outs.Add(new KeyValuePair<string, string>("S04", FEsc(InGameDetailFormat.Compose(
            L(O1("贝吉亚↑1档", 4), O1("避税(一次性)", -5)), -1, true, T, null))));

        outs.Add(new KeyValuePair<string, string>("S05", FEsc(InGameDetailFormat.Compose(
            FAll41(), 12, true, T, null))));

        outs.Add(new KeyValuePair<string, string>("S06", FEsc(InGameDetailFormat.Compose(
            L(P1("ED=稳定", 0)), 0, true, T, null))));

        outs.Add(new KeyValuePair<string, string>("S07", FEsc(InGameDetailFormat.Compose(L(), 0, true))));

        outs.Add(new KeyValuePair<string, string>("S08", FEsc(InGameDetailFormat.Compose(
            FAll41(), 12, false, T, null))));

        outs.Add(new KeyValuePair<string, string>("S09", FEsc(InGameDetailFormat.Compose(
            L(), 5, true, T, null))));

        outs.Add(new KeyValuePair<string, string>("S10", FEsc(InGameDetailFormat.Compose(
            L(P1("ED=稳定", 0), P1("ED=增长", 2)), 2, true, T, null))));

        outs.Add(new KeyValuePair<string, string>("S11", FEsc(InGameDetailFormat.Compose(
            L(P1("ED=增长", 1), O1("洛伦↓2档", -2)), -1, true, T, null))));

        outs.Add(new KeyValuePair<string, string>("S12", FEsc(InGameDetailFormat.Compose(
            L(P1("某个没见过的词条", 1), P1("<B>C&D", -1), P1("ED=危险", -2)), -2, true, T, null))));

        outs.Add(new KeyValuePair<string, string>("S13", FEsc(InGameDetailFormat.Compose(
            L(P1("ED=危险", -5), P1("逃税", -3), O1("洛伦↓1档", -1), O1("避税(一次性)", -2)),
            -11, true, T, null))));

        outs.Add(new KeyValuePair<string, string>("S14", FEsc(InGameDetailFormat.Compose(
            L(G1("免费处方药(每回合)", -1, 0, W), G1("福利支出", 1, 0, W), P1("ED=增长", 1)),
            1, true, T, null))));

        outs.Add(new KeyValuePair<string, string>("S15", FEsc(InGameDetailFormat.Compose(
            L(P1("ED=增长", 1), P1("税效率", 1)), 2, true))));

        outs.Add(new KeyValuePair<string, string>("S16", FEsc(InGameDetailFormat.Compose(
            L(O1("避税(一次性)", -3)), -3, true, T, null))));

        outs.Add(new KeyValuePair<string, string>("S17", FEsc(InGameDetailFormat.Compose(
            L(P1("ED=增长", 123), P1("逃税", -456), O1("贝吉亚↑2档", 78)), -255, true, T, null))));

        outs.Add(new KeyValuePair<string, string>("S18", FEsc(InGameDetailFormat.Compose(
            null, 0, true, T, null))));

        return outs;
    }
}
