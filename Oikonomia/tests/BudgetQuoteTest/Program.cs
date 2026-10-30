using System;
using System.Collections.Generic;
using Oikonomia;

internal static class Program
{
    private static int _fail;
    private static int _total;

    private static void Check(bool ok, string name)
    {
        _total++;
        if (ok) return;
        _fail++;
        Console.WriteLine("FAIL " + name);
    }

    private static void Main()
    {
        Check(BudgetQuoteMatrix.CellCount == 16, "16 个格子");
        Check(BudgetQuoteMatrix.QuoteCount == 48, "48 条引文");

        
        Check(BudgetQuoteMatrix.XBand(0.00) == "Crisis", "X 0%");
        Check(BudgetQuoteMatrix.XBand(0.20) == "Crisis", "X 20%");
        Check(BudgetQuoteMatrix.XBand(0.21) == "Unstable", "X 21%");
        Check(BudgetQuoteMatrix.XBand(0.40) == "Unstable", "X 40%");
        Check(BudgetQuoteMatrix.XBand(0.41) == "Stable", "X 41%");
        Check(BudgetQuoteMatrix.XBand(0.60) == "Stable", "X 60%");
        Check(BudgetQuoteMatrix.XBand(0.61) == "Wealthy", "X 61%");
        Check(BudgetQuoteMatrix.XBand(1.00) == "Wealthy", "X 100%");
        Check(BudgetQuoteMatrix.XBand(-1) == "Crisis", "X 下越界夹紧");
        Check(BudgetQuoteMatrix.XBand(2) == "Wealthy", "X 上越界夹紧");

        
        Check(BudgetQuoteMatrix.YBand(-3) == "Collapsing", "Y <-2");
        Check(BudgetQuoteMatrix.YBand(-2) == "Worsening", "Y -2~-1 下界");
        Check(BudgetQuoteMatrix.YBand(-1) == "Worsening", "Y -2~-1 上界");
        Check(BudgetQuoteMatrix.YBand(0) == "Stagnant", "Y 0~2 下界");
        Check(BudgetQuoteMatrix.YBand(2) == "Stagnant", "Y 0~2 上界");
        Check(BudgetQuoteMatrix.YBand(3) == "Improving", "Y >2");
        Check(BudgetQuoteMatrix.YBand(9) == "Improving", "Y 远大于 2");

        
        Check(Math.Abs(BudgetQuoteMatrix.Ratio(0, 0, 100) - 0.0) < 1e-9, "ratio 0");
        Check(Math.Abs(BudgetQuoteMatrix.Ratio(25, 0, 100) - 0.25) < 1e-9, "ratio 25%");
        Check(Math.Abs(BudgetQuoteMatrix.Ratio(100, 0, 100) - 1.0) < 1e-9, "ratio 100%");
        Check(Math.Abs(BudgetQuoteMatrix.Ratio(0, 5, 5) - 0.5) < 1e-9, "min=max 保护");

        
        var q0 = BudgetQuoteMatrix.Select(25, 0, 100, 0, 0);
        var q0b = BudgetQuoteMatrix.Select(25, 0, 100, 0, 0);
        Check(q0 != null && q0 == q0b, "选条确定性");
        Check(BudgetQuoteMatrix.Select(25, 0, 100, 0, 1) != null, "turn=1 可选中");
        Check(BudgetQuoteMatrix.SelectCell("Nope", "Stagnant", 0) == null, "缺格返回 null");

        
        string text = BudgetQuoteMatrix.Compose(q0, true);
        Check(!string.IsNullOrEmpty(text), "Compose 非空");
        Check(text.IndexOf('<') < 0 && text.IndexOf('>') < 0, "Compose 不含 TMP 尖括号");
        Check(!string.IsNullOrEmpty(q0.Author) && !string.IsNullOrEmpty(q0.Source), "引文作者/出处非空");
        Check(BudgetQuoteMatrix.Compose(null, true) == "", "null 引文为空");
        Check(BudgetQuoteMatrix.Compose(q0, false).IndexOf("——", StringComparison.Ordinal) < 0, "Bare 不带出处");

        
        var effectCounts = new int[4];
        var expectedEffects = new[,]
        {
            { QuoteEffect.Crt,      QuoteEffect.Crt,      QuoteEffect.Electric, QuoteEffect.Subtle },
            { QuoteEffect.Crt,      QuoteEffect.Electric, QuoteEffect.Fade,     QuoteEffect.Subtle },
            { QuoteEffect.Electric, QuoteEffect.Fade,     QuoteEffect.Fade,     QuoteEffect.Subtle },
            { QuoteEffect.Crt,      QuoteEffect.Electric, QuoteEffect.Fade,     QuoteEffect.Subtle }
        };
        for (int x = 0; x < 4; x++)
            for (int y = 0; y < 4; y++)
            {
                effectCounts[(int)QuoteAnimation.ForCell(x, y)]++;
                Check(QuoteAnimation.ForCell(x, y) == expectedEffects[x, y],
                    "经济表现映射 x=" + x + " y=" + y);
            }
        for (int i = 0; i < 4; i++) Check(effectCounts[i] == 4, "动效分配均衡 " + i);
        Check(QuoteAnimation.ForId("nuclear_war") == QuoteEffect.Fade, "核战争=Fade");
        Check(QuoteAnimation.ForId("rumburg_war_win") == QuoteEffect.Electric, "伦堡胜利=Electric");
        Check(QuoteAnimation.ForId("rumburg_war_lost") == QuoteEffect.Crt, "伦堡失败=Crt");
        Check(QuoteAnimation.ForId("global_trade_war") == QuoteEffect.Subtle, "贸易战=Subtle");

        string stable = QuoteAnimation.Stable("测试引文");
        Check(stable.IndexOf("<size=18><i>", StringComparison.Ordinal) >= 0, "稳定帧斜体小一号");
        Check(QuoteAnimation.Frame("测试引文", QuoteEffect.Fade, 0, 8) == stable, "Fade 帧 0=稳定");
        Check(QuoteAnimation.Frame("测试引文", QuoteEffect.Fade, 8, 8) == stable, "Fade 末帧=稳定");
        Check(QuoteAnimation.Frame("测试引文", QuoteEffect.Fade, 3, 8) != stable, "Fade 中间帧有变化");
        Check(QuoteAnimation.Frame("测试引文", QuoteEffect.Crt, 3, 12) != stable, "Crt 中间帧有变化");
        for (int f = 0; f < 12; f++)
        {
            string fr = QuoteAnimation.Frame("测试引文", QuoteEffect.Crt, f, 12);
            Check(fr.IndexOf("<color", StringComparison.Ordinal) < 0, "Crt 不变色 f=" + f);
        }
        Check(QuoteAnimation.CrtFullyGlitched("测试引文", 3), "Crt 短句三帧覆盖");

        
        Check(QuoteAnimation.BodyTickFrames == 10 && QuoteAnimation.BodyStepCount == 15
            && QuoteAnimation.BodyTotalFrames == 150, "正文 150 帧 / 15 步");
        Check(QuoteAnimation.BodyAdvanceAt(10) && QuoteAnimation.BodyAdvanceAt(140)
            && QuoteAnimation.BodyAdvanceAt(150), "正文每 10 帧推进一步");
        Check(!QuoteAnimation.BodyAdvanceAt(0) && !QuoteAnimation.BodyAdvanceAt(7)
            && !QuoteAnimation.BodyAdvanceAt(-10), "非节拍帧不推进");
        Check(!QuoteAnimation.BodySwitchAt(149) && QuoteAnimation.BodySwitchAt(150)
            && QuoteAnimation.BodySwitchAt(280), "第 150 帧才切过渡");
        Check(QuoteAnimation.SignalTransitionFrames == 30, "过渡 30 帧");
        Check(QuoteAnimation.BodyTotalFrames + QuoteAnimation.SignalTransitionFrames == 180,
            "正文 150 + 过渡 30 = 180 帧一循环");
        Check(QuoteAnimation.FrameCount(QuoteEffect.Crt) > QuoteAnimation.BodyStepCount, "Crt 帧数够 15 步");

        
        string crtLong = new string('经', 40);
        Check(QuoteAnimation.CrtFullyGlitched(crtLong, QuoteAnimation.BodyStepCount), "Crt 第 15 步仍满覆盖");
        string crtLongBody = QuoteAnimation.Frame(crtLong, QuoteEffect.Crt, QuoteAnimation.BodyStepCount,
            QuoteAnimation.FrameCount(QuoteEffect.Crt));
        Check(crtLongBody.IndexOf("经", StringComparison.Ordinal) < 0, "Crt 正文末步全部故障");
        Check(crtLongBody.IndexOf("<color", StringComparison.Ordinal) < 0, "Crt 正文末步不变色");

        
        string crtAcc = new string('经', 60); 
        var crtMask10 = QuoteAnimation.CrtMask(crtAcc, 10);
        var crtMask15 = QuoteAnimation.CrtMask(crtAcc, 15);
        Check(crtMask10.Count > 0 && crtMask15.IsSupersetOf(crtMask10) && crtMask15.Count > crtMask10.Count,
            "正文故障位置逐步累积");
        string crtBody10 = QuoteFrameBody(QuoteAnimation.Frame(crtAcc, QuoteEffect.Crt, 10,
            QuoteAnimation.FrameCount(QuoteEffect.Crt)));
        string crtBody15 = QuoteFrameBody(QuoteAnimation.Frame(crtAcc, QuoteEffect.Crt, 15,
            QuoteAnimation.FrameCount(QuoteEffect.Crt)));
        bool crtGlyphStable = true, crtGlyphReplaced = true;
        foreach (int p in crtMask10)
        {
            crtGlyphStable &= crtBody10[p] == crtBody15[p];
            crtGlyphReplaced &= crtBody10[p] != crtAcc[p];
        }
        Check(crtGlyphStable, "已故障字字形固定不重掷");
        Check(crtGlyphReplaced, "已故障字确实被替换");
        Check(QuoteAnimation.CrtFullyGlitched(new string('引', 30), QuoteAnimation.BodyStepCount),
            "正文 15 步内覆盖整句");

        string transition0 = QuoteAnimation.SignalTransition(0);
        string transition5 = QuoteAnimation.SignalTransition(5);
        Check(transition0.IndexOf("<align=center>", StringComparison.Ordinal) >= 0, "过渡居中");
        Check(transition0.IndexOf("<size=20><b>", StringComparison.Ordinal) >= 0, "过渡 20 号加粗");
        Check(transition5.IndexOf("<alpha=#00>", StringComparison.Ordinal) >= 0, "消失帧不可见占位");
        Check(transition5.IndexOf("[Signal Interruption]", StringComparison.Ordinal) >= 0, "占位符保留原文");
        Check(transition0.IndexOf("<color", StringComparison.Ordinal) < 0, "过渡不变色");

        
        const int transitionFrames = 30;
        string marker = QuoteAnimation.SignalMarker;
        var slots = QuoteAnimation.SignalSlots();
        Check(slots.Count == 18, "过渡可故障位置 = 18 个字母");
        Check(marker[0] == '[' && marker[marker.Length - 1] == ']', "过渡标记首尾方括号保留");
        Check(slots.IndexOf(0) < 0 && slots.IndexOf(7) < 0 && slots.IndexOf(marker.Length - 1) < 0,
            "过渡方括号与空格不参与故障");

        
        bool maskMonotone = true;
        for (int f = 1; f < transitionFrames; f++)
        {
            var before = QuoteAnimation.SignalMask(f - 1);
            var after = QuoteAnimation.SignalMask(f);
            bool superset = before.Count <= after.Count;
            foreach (int p in before) superset &= after.Contains(p);
            if (!superset) { maskMonotone = false; break; }
        }
        Check(maskMonotone, "累积故障位置逐帧只增不减");

        
        bool renderedMonotone = true;
        HashSet<int> prevRendered = null;
        for (int f = 1; f < transitionFrames; f++)
        {
            if (f % 5 == 0) continue; 
            var now = RenderedGlitchPositions(QuoteAnimation.SignalTransition(f));
            if (prevRendered != null)
            {
                bool ok = prevRendered.Count <= now.Count;
                foreach (int p in prevRendered) ok &= now.Contains(p);
                if (!ok) { renderedMonotone = false; break; }
            }
            prevRendered = now;
        }
        Check(renderedMonotone, "可见过渡帧故障位置不回退");
        Check(RenderedGlitchPositions(QuoteAnimation.SignalTransition(0)).Count == 0, "过渡起始帧干净");

        
        Check(QuoteAnimation.SignalStep(1) == 0 && QuoteAnimation.SignalStep(2) == 1
            && QuoteAnimation.SignalStep(13) == 1, "第 1 个故障步 2..13 帧");
        Check(QuoteAnimation.SignalStep(14) == 2 && QuoteAnimation.SignalStep(25) == 2, "第 2 个故障步 14..25 帧（12 帧/步）");
        Check(QuoteAnimation.SignalStep(26) == 3 && QuoteAnimation.SignalStep(29) == 3, "第 3 个故障步 26..29 帧");
        Check(QuoteAnimation.SignalMask(2).SetEquals(QuoteAnimation.SignalMask(13)), "步内故障位置不再增多");
        var step2Mask = QuoteAnimation.SignalMask(14);
        var step1Mask = QuoteAnimation.SignalMask(13);
        Check(step2Mask.IsSupersetOf(step1Mask) && step2Mask.Count > step1Mask.Count, "进入下一故障步才追加新字符");

        
        string transBody2 = TransitionBody(QuoteAnimation.SignalTransition(2));
        string transBody29 = TransitionBody(QuoteAnimation.SignalTransition(29));
        bool glyphStable = true;
        foreach (int p in QuoteAnimation.SignalMask(2))
            glyphStable &= IsPixelChar(transBody2[p]) && transBody2[p] == transBody29[p];
        Check(glyphStable, "已故障字符保持故障且字形稳定");

        
        var lastMask = QuoteAnimation.SignalMask(29);
        Check(lastMask.Count == slots.Count, "末帧覆盖全部可故障字母");
        Check(lastMask.Count >= (int)Math.Ceiling(marker.Length * 0.8), "末帧覆盖标记大部分字符");
        Check(transBody29[0] == '[' && transBody29[marker.Length - 1] == ']', "末帧保留方括号轮廓");
        Check(transBody29.IndexOf("＃", StringComparison.Ordinal) < 0, "过渡只用 ░▒▓█ 像素字");

        
        foreach (int f in new[] { 5, 10, 15, 20, 25 })
        {
            string t = QuoteAnimation.SignalTransition(f);
            Check(t.IndexOf("<alpha=#00>", StringComparison.Ordinal) >= 0, "隐去帧 alpha=00 f=" + f);
            Check(TransitionBody(t) == marker, "隐去帧占位完整 f=" + f);
            Check(NoPixelChars(t), "隐去帧无故障像素 f=" + f);
            Check(QuoteAnimation.SignalMask(f).SetEquals(QuoteAnimation.SignalMask(f - 1)), "隐去帧累积不丢失 f=" + f);
        }

        
        for (int f = 0; f <= transitionFrames + 4; f++)
        {
            string t = QuoteAnimation.SignalTransition(f);
            Check(t.IndexOf("<color", StringComparison.Ordinal) < 0, "过渡全程不变色 f=" + f);
            Check(t.IndexOf("<align=center>", StringComparison.Ordinal) == 0
                && t.IndexOf("<size=20><b>", StringComparison.Ordinal) > 0
                && t.EndsWith("</b></size></align>", StringComparison.Ordinal), "过渡标签完整 f=" + f);
            Check(t.IndexOf("<alpha=#", StringComparison.Ordinal) == t.LastIndexOf("<alpha=#", StringComparison.Ordinal),
                "过渡只有外层 alpha 标签 f=" + f);
        }

        string fadeTransition = QuoteAnimation.FadeTransition("测试引文", 3);
        string subtleTransition = QuoteAnimation.SubtleTransition("测试引文", 3);
        Check(fadeTransition.IndexOf("[Signal Interruption]", StringComparison.Ordinal) < 0, "Fade 不用信号标记");
        Check(subtleTransition.IndexOf("[Signal Interruption]", StringComparison.Ordinal) < 0, "Subtle 不用信号标记");
        Check(fadeTransition.IndexOf("▓", StringComparison.Ordinal) < 0, "Fade 过渡无像素故障");
        Check(subtleTransition.IndexOf("▓", StringComparison.Ordinal) < 0, "Subtle 过渡无像素故障");
        Check(QuoteAnimation.Frame("测试引文", QuoteEffect.Fade, 3, 8).IndexOf("▓", StringComparison.Ordinal) < 0, "Fade 显示无像素");
        Check(QuoteAnimation.Frame("测试引文", QuoteEffect.Subtle, 3, 9).IndexOf("▓", StringComparison.Ordinal) < 0, "Subtle 显示无像素");

        
        Check(EmergencyQuoteData.Groups.Length == 5, "紧急池 5 组");
        int emergencyQuotes = 0;
        foreach (var g in EmergencyQuoteData.Groups) emergencyQuotes += g.Quotes.Length;
        Check(emergencyQuotes == 15, "紧急池 15 条");
        Check(EmergencyQuoteSelector.BuildTags(false, false, false, false).Count == 0, "非紧急状态不选紧急池");
        var defaultTags = EmergencyQuoteSelector.BuildTags(true, false, false, false);
        Check(defaultTags.Count == 1 && defaultTags[0] == "monarchy", "紧急状态两极未禁运默认禁运伦堡");
        Check(EmergencyQuoteSelector.Select(defaultTags).Id == "embargo_monarchy", "默认走伦堡池");
        Check(EmergencyQuoteSelector.BuildTags(true, true, false, false).Count == 1, "禁运 arcasia 不补伦堡");
        Check(EmergencyQuoteSelector.BuildTags(true, false, true, false).Count == 1, "禁运 contana 不补伦堡");
        Check(EmergencyQuoteSelector.Select(new List<string>()) == null, "显式空集合不选紧急池");
        Check(EmergencyQuoteSelector.Select(new List<string> { "arcasia" }).Id == "embargo_arcasia", "单禁运 arcasia");
        Check(EmergencyQuoteSelector.Select(new List<string> { "contana" }).Id == "embargo_contana", "单禁运 contana");
        Check(EmergencyQuoteSelector.Select(new List<string> { "arcasia", "monarchy" }).Id == "embargo_arcasia", "无精确组合时选匹配单池");
        Check(EmergencyQuoteSelector.Select(new List<string> { "arcasia", "contana" }).Id == "embargo_arcasia_contana", "双极禁运");
        Check(EmergencyQuoteSelector.Select(new List<string> { "arcasia", "contana", "monarchy" }).Id == "embargo_all", "全球禁运优先");
        var eg = EmergencyQuoteSelector.Select(new List<string> { "contana" });
        Check(EmergencyQuoteSelector.Pick(eg, 0) != null && EmergencyQuoteSelector.Pick(eg, 3) == EmergencyQuoteSelector.Pick(eg, 0), "紧急池三条循环");

        Console.WriteLine("═══ 结果：" + (_total - _fail) + "/" + _total + " 全部通过 ✓ ═══");
        Environment.Exit(_fail == 0 ? 0 : 1);
    }

    
    private static string QuoteFrameBody(string frame)
    {
        const string open = "<size=18><i>";
        const string close = "</i></size>";
        int i = frame.IndexOf(open, StringComparison.Ordinal);
        int j = frame.LastIndexOf(close, StringComparison.Ordinal);
        if (i < 0 || j <= i) return "";
        return frame.Substring(i + open.Length, j - i - open.Length);
    }

    
    private static string TransitionBody(string transition)
    {
        const string open = "<size=20><b>";
        const string close = "</b></size></align>";
        int i = transition.IndexOf(open, StringComparison.Ordinal);
        int j = transition.LastIndexOf(close, StringComparison.Ordinal);
        if (i < 0 || j <= i) return "";
        return transition.Substring(i + open.Length, j - i - open.Length);
    }

    private static bool IsPixelChar(char c)
    {
        return c == '░' || c == '▒' || c == '▓' || c == '█';
    }

    
    private static HashSet<int> RenderedGlitchPositions(string transition)
    {
        var set = new HashSet<int>();
        string body = TransitionBody(transition);
        int len = Math.Min(body.Length, QuoteAnimation.SignalMarker.Length);
        for (int i = 0; i < len; i++) if (IsPixelChar(body[i])) set.Add(i);
        return set;
    }

    private static bool NoPixelChars(string s)
    {
        foreach (char c in s) if (IsPixelChar(c)) return false;
        return true;
    }
}
