




















using System;
using System.Collections.Generic;
using Oikonomia;

partial class Program
{
    static int fails = 0;
    static int total = 0;

    static readonly string NL = "\n";
    static readonly string P = InGameDetailFormat.PositiveColor;   
    static readonly string N = InGameDetailFormat.NegativeColor;   
    static readonly string Z = InGameDetailFormat.ZeroColor;       

    
    static InGameDetailFormat.Entry E(string text, int delta) { return new InGameDetailFormat.Entry(text, delta); }
    static InGameDetailFormat.Entry P1(string text, int delta) { return new InGameDetailFormat.Entry(text, delta, InGameDetailFormat.KindPerTurn, null); }
    static InGameDetailFormat.Entry O1(string text, int delta) { return new InGameDetailFormat.Entry(text, delta, InGameDetailFormat.KindOneTime, null); }
    static InGameDetailFormat.Entry G1(string text, int delta, int kind, string group) { return new InGameDetailFormat.Entry(text, delta, kind, group); }
    static List<InGameDetailFormat.Entry> L(params InGameDetailFormat.Entry[] items) { return new List<InGameDetailFormat.Entry>(items); }

    static string T(string hex, string inner) { return "<color=" + hex + ">" + inner + "</color>"; }

    
    static readonly Dictionary<string, string> C = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        { "P", InGameDetailFormat.PositiveColor },
        { "N", InGameDetailFormat.NegativeColor },
        { "Z", InGameDetailFormat.ZeroColor },
    };
    static string Tag(string key, string inner) { return T(C[key], inner); }

    
    
    
    
    
    
    
    
    
    
    
    static string Line(string key, string valueAndText) { return Tag(key, "(" + valueAndText + ")") + NL; }
    static string Head(string label) { return InGameDetailFormat.Bold(Tag("Z", label)) + NL; }

    
    static readonly string Blank = NL;
    static string Sub(string key, string val)
    {
        return Tag(key, "── (" + val + " <u>小计</u>)") + NL;
    }
    static string TitleLine()
    {
        return InGameDetailFormat.AlignCenter(InGameDetailFormat.Bold(Tag("Z", InGameDetailFormat.PreviewTitle))) + NL;
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

    static void Has(string name, string got, string needle)
    {
        total++;
        bool ok = got != null && got.IndexOf(needle, StringComparison.Ordinal) >= 0;
        if (!ok) fails++;
        Console.WriteLine((ok ? "  PASS  " : "  FAIL  ") + name
            + "   必须含「" + needle + "」" + (ok ? " ✓" : " ✗"));
        if (!ok) Console.WriteLine("          实际：" + Show(got));
    }

    static void NotHas(string name, string got, string needle)
    {
        total++;
        bool ok = got != null && got.IndexOf(needle, StringComparison.Ordinal) < 0;
        if (!ok) fails++;
        Console.WriteLine((ok ? "  PASS  " : "  FAIL  ") + name
            + "   必须不含「" + needle + "」" + (ok ? " ✓" : " ✗"));
        if (!ok) Console.WriteLine("          实际：" + Show(got));
    }

    static string Show(string s)
    {
        if (s == null) return "(null)";
        return "\"" + s.Replace("\r", "\\r").Replace("\n", "\\n") + "\"";
    }

    static int CountOf(string s, string needle)
    {
        if (string.IsNullOrEmpty(s) || string.IsNullOrEmpty(needle)) return 0;
        int n = 0, i = 0;
        while ((i = s.IndexOf(needle, i, StringComparison.Ordinal)) >= 0) { n++; i += needle.Length; }
        return n;
    }

    
    static bool IsSignedInt(string s)
    {
        if (string.IsNullOrEmpty(s)) return false;
        int i = (s[0] == '+' || s[0] == '-') ? 1 : 0;
        if (i >= s.Length) return false;
        for (; i < s.Length; i++) if (s[i] < '0' || s[i] > '9') return false;
        return true;
    }

    
    static bool SplitColorLine(string rawLine, out string inner)
    {
        inner = null;
        if (string.IsNullOrEmpty(rawLine)) return false;
        const string open = "<color=#";
        const string close = "</color>";
        if (!rawLine.StartsWith(open, StringComparison.Ordinal)) return false;
        if (!rawLine.EndsWith(close, StringComparison.Ordinal)) return false;
        int gt = rawLine.IndexOf('>');
        if (gt < 0) return false;
        if (!IsHex(rawLine.Substring(open.Length - 1, gt - (open.Length - 1)))) return false;   
        inner = rawLine.Substring(gt + 1, rawLine.Length - gt - 1 - close.Length);
        return inner.Length > 0;
    }

    
    
    
    
    static bool IsParenRow(string rawLine, out string why)
    {
        why = null;
        string inner;
        if (!SplitColorLine(rawLine, out inner)) { why = "不是 <color=#RRGGBB>…</color> 单行"; return false; }
        if (inner.Length < 4 || inner[0] != '(' || inner[inner.Length - 1] != ')')
        { why = "不是「(… )」形态：" + inner; return false; }
        string body = inner.Substring(1, inner.Length - 2);
        int sp = body.IndexOf(' ');
        if (sp <= 0) { why = "括号里没有「值 + 空格 + 说明」：" + inner; return false; }
        if (!IsSignedInt(body.Substring(0, sp))) { why = "值不是带符号整数：" + body.Substring(0, sp); return false; }
        if (body.Length <= sp + 1) { why = "逗号后的说明为空：" + inner; return false; }
        if (body[sp + 1] == ' ') { why = "值与说明之间多于一个空格：" + inner; return false; }
        why = inner;
        return true;
    }

    
    
    
    
    
    
    
    
    
    
    
    static bool IsParenTotal(string rawLine, out string why)
    {
        why = null;
        string inner;
        if (!SplitColorLine(rawLine, out inner)) { why = "不是 <color=#RRGGBB>…</color> 单行"; return false; }
        const string tail = " <u>Total:</u>";
        if (!inner.EndsWith(tail, StringComparison.Ordinal)) { why = "结尾不是「 <u>Total:</u>」：" + inner; return false; }
        string head = inner.Substring(0, inner.Length - tail.Length);
        if (head.Length < 3 || head[0] != '(' || head[head.Length - 1] != ')')
        { why = "额度不是「(净额)」形态：" + inner; return false; }
        if (!IsSignedInt(head.Substring(1, head.Length - 2)))
        { why = "括号里不是带符号整数：" + head; return false; }
        why = inner;
        return true;
    }

    
    
    
    
    
    
    
    
    
    
    static bool HasTotalLine(string rendered, out string why)
    {
        why = null;
        if (rendered == null) { why = "null"; return false; }

        foreach (var raw in rendered.Split('\n'))
        {
            if (raw.Length == 0) continue;
            string w;
            if (IsParenTotal(raw, out w)) { why = "出现合计行形态：" + w; return true; }
        }

        string plain = StripTags(rendered);
        if (plain.IndexOf("Total:", StringComparison.Ordinal) >= 0) { why = "出现字面「Total:」"; return true; }
        if (plain.IndexOf("合计", StringComparison.Ordinal) >= 0) { why = "出现字面「合计」"; return true; }
        if (plain.IndexOf("预计", StringComparison.Ordinal) >= 0) { why = "出现字面「预计」"; return true; }

        why = "无合计行（形态 / Total: / 合计 / 预计 四层全部未命中）";
        return false;
    }

    
    
    
    
    
    static bool AllRowsParen(string rendered, out int count, out string why)
    {
        count = 0;
        why = null;
        if (string.IsNullOrEmpty(rendered)) { why = "空串"; return false; }
        foreach (var raw in rendered.Split('\n'))
        {
            if (raw.Length == 0) continue;
            string plain = StripTags(raw);
            if (plain.IndexOf('【') >= 0) continue;                 
            if (plain.IndexOf('─') >= 0) continue;                 
            if (plain.IndexOf("小计", StringComparison.Ordinal) >= 0) continue;
            if (plain.IndexOf(InGameDetailFormat.PreviewTitle, StringComparison.Ordinal) >= 0) continue;
            count++;
            string why1;
            if (!IsParenRow(raw, out why1)) { why = "第 " + count + " 条明细行不合规：" + why1; return false; }
        }
        if (count == 0) { why = "一条明细行都没有"; return false; }
        why = count + " 条明细行全部是「(值 说明)」形态";
        return true;
    }

    
    static bool HasTrailingValue(string rendered)
    {
        if (string.IsNullOrEmpty(rendered)) return false;
        const string close = "</color>";
        foreach (var raw in rendered.Split('\n'))
        {
            if (!raw.EndsWith(close, StringComparison.Ordinal)) continue;
            int k = raw.Length - close.Length - 1;
            if (k >= 0 && raw[k] >= '0' && raw[k] <= '9') return true;   
        }
        return false;
    }

    
    static bool AnyDoubleSpace(string rendered)
    {
        return !string.IsNullOrEmpty(rendered) && rendered.IndexOf("  ", StringComparison.Ordinal) >= 0;
    }

    
    static string LastNonEmpty(string rendered)
    {
        if (string.IsNullOrEmpty(rendered)) return null;
        var parts = rendered.Split('\n');
        for (int i = parts.Length - 1; i >= 0; i--) if (parts[i].Length > 0) return parts[i];
        return null;
    }

    
    static int CountOfLinesWith(string rendered, string needle)
    {
        if (string.IsNullOrEmpty(rendered)) return 0;
        int n = 0;
        foreach (var t in rendered.Split('\n')) if (t.IndexOf(needle, StringComparison.Ordinal) >= 0) n++;
        return n;
    }

    static bool IsHex(string s)
    {
        if (s == null || s.Length != 7 || s[0] != '#') return false;
        for (int i = 1; i < 7; i++)
        {
            char c = s[i];
            bool hex = (c >= '0' && c <= '9') || (c >= 'a' && c <= 'f') || (c >= 'A' && c <= 'F');
            if (!hex) return false;
        }
        return true;
    }

    
    static string StripTags(string t)
    {
        if (t == null) return null;
        var sb = new System.Text.StringBuilder(t.Length);
        int i = 0;
        while (i < t.Length)
        {
            if (t[i] == '<')
            {
                int gt = t.IndexOf('>', i + 1);
                if (gt < 0) { sb.Append(t.Substring(i)); break; }
                i = gt + 1;
                continue;
            }
            sb.Append(t[i]);
            i++;
        }
        return sb.ToString();
    }

    
    static string Plain(string rendered)
    {
        if (rendered == null) return null;
        var parts = rendered.Split('\n');
        for (int i = 0; i < parts.Length; i++) parts[i] = StripTags(parts[i]);
        return string.Join("\n", parts);
    }

    
    static string[] PlainLines(string rendered)
    {
        if (rendered == null) return new string[0];
        var tagged = rendered.Split('\n');
        var outp = new List<string>();
        foreach (var t in tagged)
        {
            if (t.Length == 0) continue;
            outp.Add(StripTags(t));
        }
        return outp.ToArray();
    }

    
    static string ColorOfLine(string rendered, string label)
    {
        if (rendered == null) return "?";
        foreach (var t in rendered.Split('\n'))
        {
            if (StripTags(t).IndexOf(label, StringComparison.Ordinal) < 0) continue;
            if (t.IndexOf(P, StringComparison.Ordinal) >= 0) return "P";
            if (t.IndexOf(N, StringComparison.Ordinal) >= 0) return "N";
            if (t.IndexOf(Z, StringComparison.Ordinal) >= 0) return "Z";
        }
        return "?";
    }

    static void Main()
    {
        string whyTotal = null;      
        Console.WriteLine("═══ 游戏内预算加减明细：格式化内核单元测试（规范 §7 逐条）═══");
        Console.WriteLine("（颜色：正 " + P + " / 负 " + N + " / 零 " + Z + "）");
        Console.WriteLine("");

        
        Console.WriteLine("【① 不变量：各项之和 == 本回合结算增量（规范 §7.1，最重要）】");

        
        var specExample = L(
            E("ED=增长", 1), E("旅游业=蓬勃发展", 1),
            G1("免费处方药(每回合)", -1, InGameDetailFormat.KindPerTurn, InGameDetailFormat.GroupExtraWelfare),
            O1("贝吉亚↑1档", 1), O1("避税(一次性)", -1));
        const int specTotal = 1;
        SameInt("①-1a 规范 §5 样例：各项之和 = +1（1+1-1+1-1）", InGameDetailFormat.SumDeltas(specExample), specTotal);
        SameInt("①-1b 归并后各行之和仍 = +1（归并不增不减）",
            InGameDetailFormat.SumRows(InGameDetailFormat.BuildRows(specExample)), specTotal);
        string specRendered = InGameDetailFormat.Compose(specExample, specTotal, true);
        Same("①-1c 规范 §5 样例：整段**不含合计行**（2.7.8 取消合计行的验收判据，四层口径）",
            HasTotalLine(specRendered, out whyTotal) ? "有合计行" : "无合计行", "无合计行");
        Same("①-1d 规范 §5 样例逐字对照（两段式 + 小计；2.7.8 起末行就是小计）", specRendered,
            Head("【未来】")
            + Line("P", "+1 经济状态日益增长")
            + Line("P", "+1 全国旅游业蓬勃发展")
            + Line("N", "-1 额外福利支出")
            + Sub("P", "+1")
            + Blank
            + Head("【当前】")
            + Line("P", "+1 贝吉亚地区发展提速")
            + Line("N", "-1 资本避税问题")
            + Sub("Z", "0"));

        
        var neg = L(E("ED=危险", -2), E("逃税(每回合)", -1), O1("洛伦↓2档", -2));
        SameInt("①-2a 负合计：各项之和 = -5", InGameDetailFormat.SumDeltas(neg), -5);
        Same("①-2b 负合计场景整段同样**不含合计行**（负额度也不会漏出一行）",
            HasTotalLine(InGameDetailFormat.Compose(neg, -5, true), out whyTotal) ? "有合计行" : "无合计行", "无合计行");

        
        for (int k = 0; k < 5; k++)
        {
            int a = k * 3 - 6, b = 7 - k * 2, c = k - 2, d = 11 - k * 4;
            var set = L(
                G1("免费处方药(每回合)", a, InGameDetailFormat.KindPerTurn, InGameDetailFormat.GroupExtraWelfare),
                G1("免费校餐(每回合)", b, InGameDetailFormat.KindPerTurn, InGameDetailFormat.GroupExtraWelfare),
                E("ED=增长", c), O1("高就业(一次性)", d));
            int sum = a + b + c + d;
            string rendered = InGameDetailFormat.Compose(set, sum, true);
            
            
            string w3 = null;
            bool ok = InGameDetailFormat.SumDeltas(set) == sum
                && InGameDetailFormat.SumRows(InGameDetailFormat.BuildRows(set)) == sum
                && !HasTotalLine(rendered, out w3);
            True("①-3." + k + " 组合守恒（明细和 = 归并和 = 实扣值 " + sum + "；且画面无合计行）", ok,
                "a=" + a + " b=" + b + " c=" + c + " d=" + d + " ｜ " + (w3 ?? ""));
        }

        
        Console.WriteLine("");
        Console.WriteLine("【② 分段：每回合 / 本回合 / 小计 / 合计（规范 §5）】");

        Same("②-1 只有每回合类 → 只有【每回合】段（本回合段整段不显示）",
            InGameDetailFormat.Compose(L(E("ED=增长", 1), E("旅游业=蓬勃发展", 1)), 2, true),
            Head("【未来】")
            + Line("P", "+1 经济状态日益增长")
            + Line("P", "+1 全国旅游业蓬勃发展")
            + Sub("P", "+2"));

        Same("②-2 每回合 + 本回合两段分明，各自小计",
            InGameDetailFormat.Compose(L(E("ED=增长", 1), O1("贝吉亚↑1档", 1)), 2, true),
            Head("【未来】")
            + Line("P", "+1 经济状态日益增长")
            + Sub("P", "+1")
            + Blank
            + Head("【当前】")
            + Line("P", "+1 贝吉亚地区发展提速")
            + Sub("P", "+1"));

        Same("②-3 只有本回合类（地区升降 + 避税）→ 小计 -2 / 合计 -2，全红",
            InGameDetailFormat.Compose(L(O1("贝吉亚↑1档", 1), O1("洛伦↓2档", -2), O1("避税(一次性)", -1)), -2, true),
            Head("【当前】")
            + Line("P", "+1 贝吉亚地区发展提速")
            + Line("N", "-2 洛伦地区发展大幅倒退（2 档）")
            + Line("N", "-1 资本避税问题")
            + Sub("N", "-2"));

        True("②-4 段头顺序：【未来】在【当前】之前",
            InGameDetailFormat.Compose(L(E("ED=增长", 1), O1("贝吉亚↑1档", 1)), 2, true)
                .IndexOf("【未来】", StringComparison.Ordinal)
            < InGameDetailFormat.Compose(L(E("ED=增长", 1), O1("贝吉亚↑1档", 1)), 2, true)
                .IndexOf("【当前】", StringComparison.Ordinal),
            "两段顺序 = 每回合类（【未来】）→ 一次性类（【当前】）");

        
        Console.WriteLine("");
        Console.WriteLine("【③ 组归并：同组求和、只显示一行、组为 0 不显示（规范 §7.2）】");

        string twoMember = InGameDetailFormat.Compose(L(
            G1("免费处方药(每回合)", -1, InGameDetailFormat.KindPerTurn, InGameDetailFormat.GroupExtraWelfare),
            G1("免费校餐(每回合)", -2, InGameDetailFormat.KindPerTurn, InGameDetailFormat.GroupExtraWelfare)), -3, true);
        Same("③-1 同组两项求和后只显示一行「额外福利支出 −3」", twoMember,
            Head("【未来】")
            + Line("N", "-3 额外福利支出")
            + Sub("N", "-3"));
        NotHas("③-2 组内原始标签（免费处方药 / 免费校餐）不得单独出现", twoMember, "免费校餐");
        SameInt("③-3 组行增量 = 组内求和（-1 + -2 = -3）",
            InGameDetailFormat.BuildRows(L(
                G1("免费处方药(每回合)", -1, InGameDetailFormat.KindPerTurn, InGameDetailFormat.GroupExtraWelfare),
                G1("免费校餐(每回合)", -2, InGameDetailFormat.KindPerTurn, InGameDetailFormat.GroupExtraWelfare)))[0].Delta, -3);

        string groupZero = InGameDetailFormat.Compose(L(
            G1("免费处方药(每回合)", -1, InGameDetailFormat.KindPerTurn, InGameDetailFormat.GroupExtraWelfare),
            G1("免费校餐(每回合)", 1, InGameDetailFormat.KindPerTurn, InGameDetailFormat.GroupExtraWelfare)), 0, true);
        Same("③-4 组内求和为 0 → 该组整行不显示；此时又无其它行 → 退化成「本回合无变动」", groupZero,
            Tag("Z", InGameDetailFormat.NoChangeText));
        NotHas("③-4b 组为 0 时不得渲染成「额外福利支出 本回合无变动」", groupZero, "额外福利支出");
        SameInt("③-5 组为 0 时 BuildRows 不产出该组行",
            InGameDetailFormat.BuildRows(L(
                G1("免费处方药(每回合)", -1, InGameDetailFormat.KindPerTurn, InGameDetailFormat.GroupExtraWelfare),
                G1("免费校餐(每回合)", 1, InGameDetailFormat.KindPerTurn, InGameDetailFormat.GroupExtraWelfare))).Count, 0);

        SameInt("③-6 不同组名不互相合并（两组各一行）",
            InGameDetailFormat.BuildRows(L(
                G1("免费处方药(每回合)", -1, InGameDetailFormat.KindPerTurn, InGameDetailFormat.GroupExtraWelfare),
                G1("军费(每回合)", -1, InGameDetailFormat.KindPerTurn, "军费支出"),
                G1("免费校餐(每回合)", -2, InGameDetailFormat.KindPerTurn, InGameDetailFormat.GroupExtraWelfare))).Count, 2);
        Has("③-7 组名即文案（组行显示「额外福利支出」，不是原始词条名）", twoMember, "额外福利支出");
        SameInt("③-8 同组项分散声明也能合并（相隔一项）",
            InGameDetailFormat.BuildRows(L(
                G1("免费处方药(每回合)", -1, InGameDetailFormat.KindPerTurn, InGameDetailFormat.GroupExtraWelfare),
                E("ED=增长", 1),
                G1("免费校餐(每回合)", -2, InGameDetailFormat.KindPerTurn, InGameDetailFormat.GroupExtraWelfare)))[0].Delta, -3);
        Same("③-9 组内只有一项时也照组名显示（不退回原始标签）",
            InGameDetailFormat.Compose(L(
                G1("免费处方药(每回合)", -1, InGameDetailFormat.KindPerTurn, InGameDetailFormat.GroupExtraWelfare)), -1, true),
            Head("【未来】")
            + Line("N", "-1 额外福利支出")
            + Sub("N", "-1"));

        
        Console.WriteLine("");
        Console.WriteLine("【④ 类别归属（规范 §7.3：结算点的打标口径）】");

        
        var perTurn = L(
            P1("ED=增长", 1),                   
            P1("旅游业=蓬勃发展", 1),            
            P1("贸易量=增加", 2),                
            P1("税务效率(每回合)", 1),            
            P1("逃税(每回合)", -1),               
            P1("免费处方药(每回合)", -1),          
            P1("腐败(每回合)", -1));              
        SameInt("④-1 每回合 7 项全部落【每回合】段",
            BuildRowsOfKind(perTurn, InGameDetailFormat.KindPerTurn), 7);
        SameInt("④-2 每回合 7 项没有一项落【本回合】段",
            BuildRowsOfKind(perTurn, InGameDetailFormat.KindOneTime), 0);
        Has("④-3 每回合类段头出现（屏上写【未来】）", InGameDetailFormat.Compose(perTurn, 3, true), "【未来】");
        NotHas("④-4 每回合 7 项不产生【当前】段", InGameDetailFormat.Compose(perTurn, 3, true), "【当前】");

        
        var oneTime = L(
            O1("贝吉亚↑1档", 1),                              
            O1("高就业(一次性)", 1),                          
            O1("产量增加(一次性)", 1),
            O1("小企业成长(一次性)", 1),
            O1("大企业创造就业(一次性)", 1),
            O1("军工复合体(一次性)", 1),
            O1("阿卡西亚投资(一次性)", 1),
            O1("康塔纳投资(一次性)", 1),
            O1("莱斯皮亚·煤炭贸易(一次性)", 1),                
            O1("莱斯皮亚·鱼类贸易(一次性)", 1),
            O1("莱斯皮亚·钨贸易(一次性)", 1),
            O1("莱斯皮亚·小麦贸易(一次性)", 1),
            O1("莱斯皮亚·石油出售(一次性)", 1),
            O1("莱斯皮亚自由贸易区(一次性)", 1),
            O1("瓦尔戈斯兰·煤炭贸易(一次性)", 1),
            O1("瓦尔戈斯兰·鱼类贸易(一次性)", 1),
            O1("瓦尔戈斯兰·钨贸易(一次性)", 1),
            O1("瓦尔戈斯兰·小麦贸易(一次性)", 1),
            O1("瓦尔戈斯兰自由贸易区(一次性)", 1),
            O1("韦伦·石油自由贸易(一次性)", 1),
            O1("阿格诺利亚·农业市场(一次性)", 1),
            O1("伦堡·天然气管道(一次性)", 1),
            O1("避税(一次性)", -1));                          
        SameInt("④-5 本回合 23 项全部落【本回合】段",
            BuildRowsOfKind(oneTime, InGameDetailFormat.KindOneTime), 23);
        SameInt("④-6 本回合 23 项没有一项落【每回合】段",
            BuildRowsOfKind(oneTime, InGameDetailFormat.KindPerTurn), 0);
        SameInt("④-7 本回合：21 个边沿 + 地区 + 避税 数量自检（EdgeTerms 7 增长 + 14 贸易）",
            BuildRowsOfKind(oneTime, InGameDetailFormat.KindOneTime), 7 + 14 + 1 + 1);
        NotHas("④-8 本回合 23 项不产生【未来】段", InGameDetailFormat.Compose(oneTime, 15, true), "【未来】");
        NotHas("④-9 类别打标不靠文案形态：地区标签无 (一次性) 后缀也落本回合",
            InGameDetailFormat.Compose(L(O1("贝吉亚↑1档", 1)), 1, true), "【未来】");
        Same("④-10 显式 OneTime 优先于文案推断（带 (每回合) 后缀也落本回合）",
            InGameDetailFormat.Compose(L(O1("奇怪的词条(每回合)", 1)), 1, true),
            Head("【当前】")
            + Line("P", "+1 奇怪的词条")
            + Sub("P", "+1"));
        Same("④-11 兼容路径：文案含 (一次性) 时 2 参构造也推断为一次性",
            InGameDetailFormat.Compose(L(E("避税(一次性)", -1)), -1, true),
            Head("【当前】")
            + Line("N", "-1 资本避税问题")
            + Sub("N", "-1"));

        
        Console.WriteLine("");
        Console.WriteLine("【⑤ 文案映射（规范 §7.4：§4 逐条）】");

        
        Same("⑤-1 ED=危险 → 经济状态濒临崩溃", InGameDetailFormat.Display("ED=危险"), "经济状态濒临崩溃");
        Same("⑤-2 ED=不稳定 → 经济状态动荡不安", InGameDetailFormat.Display("ED=不稳定"), "经济状态动荡不安");
        Same("⑤-3 ED=稳定 → 经济状态保持稳定", InGameDetailFormat.Display("ED=稳定"), "经济状态保持稳定");
        Same("⑤-4 ED=增长 → 经济状态日益增长（验收口径原文）", InGameDetailFormat.Display("ED=增长"), "经济状态日益增长");
        Same("⑤-5 ED=极快增长 → 经济状态高速增长", InGameDetailFormat.Display("ED=极快增长"), "经济状态高速增长");
        Same("⑤-6 旅游业=蓬勃发展 → 全国旅游业蓬勃发展（验收口径原文）", InGameDetailFormat.Display("旅游业=蓬勃发展"), "全国旅游业蓬勃发展");
        Same("⑤-7 旅游业=平均水平 → 全国旅游业平稳发展", InGameDetailFormat.Display("旅游业=平均水平"), "全国旅游业平稳发展");
        Same("⑤-8 旅游业=严重衰退 → 全国旅游业持续衰退", InGameDetailFormat.Display("旅游业=严重衰退"), "全国旅游业持续衰退");
        Same("⑤-9 贸易量=增加 → 对外贸易额显著增加", InGameDetailFormat.Display("贸易量=增加"), "对外贸易额显著增加");
        Same("⑤-10 贸易量=恢复 → 对外贸易额逐步恢复", InGameDetailFormat.Display("贸易量=恢复"), "对外贸易额逐步恢复");
        Same("⑤-11 贸易量=减少 → 对外贸易额出现萎缩", InGameDetailFormat.Display("贸易量=减少"), "对外贸易额出现萎缩");
        Same("⑤-12 税效率 → 税收征管效率提升", InGameDetailFormat.Display("税效率"), "税收征管效率提升");
        Same("⑤-13 税务效率 → 税收征管效率提升（同义标签）", InGameDetailFormat.Display("税务效率"), "税收征管效率提升");
        Same("⑤-14 逃税 → 逃税现象造成财政损失", InGameDetailFormat.Display("逃税"), "逃税现象造成财政损失");
        Same("⑤-15 腐败 → 腐败侵蚀行政效率", InGameDetailFormat.Display("腐败"), "腐败侵蚀行政效率");
        Same("⑤-16 免费处方药 → 额外福利支出（组名，规范 §4）", InGameDetailFormat.Display("免费处方药"), "额外福利支出");
        Same("⑤-17 组行文案 = 组名", InGameDetailFormat.Display(InGameDetailFormat.GroupExtraWelfare), "额外福利支出");
        True("⑤-18 组名可识别（IsKnownGroup）", InGameDetailFormat.IsKnownGroup("额外福利支出"), "额外福利支出 = 已知组");

        
        Same("⑤-19 贝吉亚↑1档 → 贝吉亚地区发展提速（验收口径原文）", InGameDetailFormat.Display("贝吉亚↑1档"), "贝吉亚地区发展提速");
        Same("⑤-20 贝吉亚↑2档 → 贝吉亚地区发展大幅提速（2 档）", InGameDetailFormat.Display("贝吉亚↑2档"), "贝吉亚地区发展大幅提速（2 档）");
        Same("⑤-21 贝吉亚↑3档 → 贝吉亚地区发展大幅提速（3 档）", InGameDetailFormat.Display("贝吉亚↑3档"), "贝吉亚地区发展大幅提速（3 档）");
        Same("⑤-22 洛伦↓1档 → 洛伦地区发展倒退", InGameDetailFormat.Display("洛伦↓1档"), "洛伦地区发展倒退");
        Same("⑤-23 洛伦↓2档 → 洛伦地区发展大幅倒退（2 档）", InGameDetailFormat.Display("洛伦↓2档"), "洛伦地区发展大幅倒退（2 档）");
        Same("⑤-24 格鲁尼↑1档 → 格鲁尼地区发展提速", InGameDetailFormat.Display("格鲁尼↑1档"), "格鲁尼地区发展提速");
        Same("⑤-25 阿格兰↓1档 → 阿格兰地区发展倒退", InGameDetailFormat.Display("阿格兰↓1档"), "阿格兰地区发展倒退");
        Same("⑤-26 带 (一次性) 后缀的地区标签同样映射正确", InGameDetailFormat.Display("贝吉亚↑1档(一次性)"), "贝吉亚地区发展提速");
        Same("⑤-27 地区名已含「地区」时不重复叠加", InGameDetailFormat.Display("贝吉亚地区↑2档"), "贝吉亚地区发展大幅提速（2 档）");

        
        Same("⑤-28 避税(一次性) → 资本避税问题（验收口径原文）", InGameDetailFormat.Display("避税(一次性)"), "资本避税问题");
        Same("⑤-29 避税（无后缀）→ 资本避税问题", InGameDetailFormat.Display("避税"), "资本避税问题");
        Same("⑤-30 高就业 → 就业率显著提高", InGameDetailFormat.Display("高就业"), "就业率显著提高");
        Same("⑤-31 产量增加 → 工业产量增加", InGameDetailFormat.Display("产量增加"), "工业产量增加");
        Same("⑤-32 小企业成长 → 中小企业蓬勃发展", InGameDetailFormat.Display("小企业成长"), "中小企业蓬勃发展");
        Same("⑤-33 大企业创造就业 → 大型企业创造就业", InGameDetailFormat.Display("大企业创造就业"), "大型企业创造就业");
        Same("⑤-34 军工复合体 → 军工复合体扩张", InGameDetailFormat.Display("军工复合体"), "军工复合体扩张");
        Same("⑤-35 阿卡西亚投资 → 阿卡西亚投资涌入", InGameDetailFormat.Display("阿卡西亚投资"), "阿卡西亚投资涌入");
        Same("⑤-36 康塔纳投资 → 康塔纳投资涌入", InGameDetailFormat.Display("康塔纳投资"), "康塔纳投资涌入");

        
        Same("⑤-37 特例 韦伦·石油自由贸易 → 与韦伦达成石油自由贸易",
            InGameDetailFormat.Display("韦伦·石油自由贸易"), "与韦伦达成石油自由贸易");
        Same("⑤-38 特例 阿格诺利亚·农业市场 → 与阿格诺利亚开放农业市场",
            InGameDetailFormat.Display("阿格诺利亚·农业市场"), "与阿格诺利亚开放农业市场");
        Same("⑤-39 特例 伦堡·天然气管道 → 与伦堡签署天然气管道协议",
            InGameDetailFormat.Display("伦堡·天然气管道"), "与伦堡签署天然气管道协议");
        Same("⑤-40 带 (一次性) 后缀的贸易特例同样映射正确",
            InGameDetailFormat.Display("韦伦·石油自由贸易(一次性)"), "与韦伦达成石油自由贸易");

        
        Same("⑤-41 规则 X·Y贸易 → 与X达成Y贸易（莱斯皮亚·煤炭贸易）",
            InGameDetailFormat.Display("莱斯皮亚·煤炭贸易"), "与莱斯皮亚达成煤炭贸易");
        Same("⑤-42 规则生成不得出现「贸易贸易」（重复后缀 bug 回归）",
            InGameDetailFormat.Display("莱斯皮亚·鱼类贸易"), "与莱斯皮亚达成鱼类贸易");
        Same("⑤-43 规则 X·Y贸易（钨）", InGameDetailFormat.Display("莱斯皮亚·钨贸易"), "与莱斯皮亚达成钨贸易");
        Same("⑤-44 规则 X·Y贸易（小麦）", InGameDetailFormat.Display("莱斯皮亚·小麦贸易"), "与莱斯皮亚达成小麦贸易");
        Same("⑤-45 规则 X·Y贸易（石油出售 → 不加「贸易」二字）",
            InGameDetailFormat.Display("莱斯皮亚·石油出售"), "与莱斯皮亚达成石油出售");
        Same("⑤-46 规则 X自由贸易区 → 与X建立自由贸易区（无「·」形态）",
            InGameDetailFormat.Display("莱斯皮亚自由贸易区"), "与莱斯皮亚建立自由贸易区");
        Same("⑤-47 规则 X·Y自由贸易区 → 与X建立自由贸易区（带「·」形态）",
            InGameDetailFormat.Display("莱斯皮亚·自由贸易区"), "与莱斯皮亚建立自由贸易区");
        Same("⑤-48 规则生成（瓦尔戈斯兰）", InGameDetailFormat.Display("瓦尔戈斯兰·煤炭贸易"), "与瓦尔戈斯兰达成煤炭贸易");
        Same("⑤-49 规则生成（瓦尔戈斯兰自贸区）", InGameDetailFormat.Display("瓦尔戈斯兰自由贸易区"), "与瓦尔戈斯兰建立自由贸易区");
        Same("⑤-50 泛化 X·Y → 与X达成Y贸易（X·Y 贸易 形态）",
            InGameDetailFormat.Display("X·Y贸易"), "与X达成Y贸易");

        
        Console.WriteLine("");
        Console.WriteLine("【⑥ 边界（规范 §7.5：空明细 / 全 0 / 未知标签 / 负值 / 超长）】");

        Same("⑥-1 未结算（无记录）→ 空串哨兵：调用方不得接管游戏文本", InGameDetailFormat.Compose(L(E("ED=增长", 1)), 1, false), "");
        Same("⑥-2 未结算 + null 明细 → 空串哨兵", InGameDetailFormat.Compose(null, 0, false), "");
        Same("⑥-3 已结算 + 空明细 + 合计 0 → 本回合无变动（不返回空串）",
            InGameDetailFormat.Compose(null, 0, true), Tag("Z", InGameDetailFormat.NoChangeText));
        Same("⑥-4 已结算 + 空列表 + 合计 0 → 本回合无变动",
            InGameDetailFormat.Compose(L(), 0, true), Tag("Z", InGameDetailFormat.NoChangeText));
        Same("⑥-5 全 0 项（普通 0 显示「本回合无变动」，一次性 0 整行不显示）",
            InGameDetailFormat.Compose(L(E("ED=稳定", 0), O1("高就业(一次性)", 0)), 0, true),
            Head("【未来】")
            + Line("Z", "0 经济状态保持稳定 本回合无变动")
            + Sub("Z", "0"));
        NotHas("⑥-5b 一次性 0 项不得出现在明细里（高就业整行不显示）",
            InGameDetailFormat.Compose(L(E("ED=稳定", 0), O1("高就业(一次性)", 0)), 0, true), "高就业");
        SameInt("⑥-6 全 0 项的 SumDeltas = 0", InGameDetailFormat.SumDeltas(L(E("ED=稳定", 0), O1("高就业(一次性)", 0))), 0);
        NotHas("⑥-7 零值项不得渲染成「+0」", InGameDetailFormat.Compose(L(E("ED=稳定", 0)), 0, true), "+0");
        Same("⑥-8 0 值普通项（有其它非零项时）→ 显示「本回合无变动」且为灰",
            InGameDetailFormat.Compose(L(E("ED=稳定", 0), E("旅游业=蓬勃发展", 1)), 1, true),
            Head("【未来】")
            + Line("Z", "0 经济状态保持稳定 本回合无变动")
            + Line("P", "+1 全国旅游业蓬勃发展")
            + Sub("P", "+1"));
        Same("⑥-9 一次性项为 0 时不显示该行（规范 §5 括注）",
            InGameDetailFormat.Compose(L(E("ED=增长", 1), O1("高就业(一次性)", 0)), 1, true),
            Head("【未来】")
            + Line("P", "+1 经济状态日益增长")
            + Sub("P", "+1"));

        string unknown = InGameDetailFormat.Compose(L(
            P1("某个没见过的词条(每回合)", -3), O1("X·Y贸易(一次性)", 1)), -2, true);
        Same("⑥-10 未知标签原样回退显示（不崩、不丢项）", unknown,
            Head("【未来】")
            + Line("N", "-3 某个没见过的词条")
            + Sub("N", "-3")
            + Blank
            + Head("【当前】")
            + Line("P", "+1 与X达成Y贸易")
            + Sub("P", "+1"));
        Has("⑥-11 未知标签内容不得丢失", unknown, "某个没见过的词条");
        Same("⑥-12 空文案项被丢弃（无信息量）",
            InGameDetailFormat.Compose(L(E("", 1), E("ED=增长", 1)), 1, true),
            Head("【未来】")
            + Line("P", "+1 经济状态日益增长")
            + Sub("P", "+1"));
        Same("⑥-13 负值行写 -1（不是 +-1）",
            InGameDetailFormat.Compose(L(O1("避税(一次性)", -1)), -1, true),
            Head("【当前】")
            + Line("N", "-1 资本避税问题")
            + Sub("N", "-1"));
        NotHas("⑥-14 负值行不得出现 +-1", InGameDetailFormat.Compose(L(O1("避税(一次性)", -1)), -1, true), "+-1");
        Has("⑥-15 超长文案不截断（整条照写，金额仍在行首括号内）",
            InGameDetailFormat.Compose(L(P1("这是一个特别特别长的词条名称用来把列宽撑爆(每回合)", -1)), -1, true),
            "-1 这是一个特别特别长的词条名称用来把列宽撑爆");
        Same("⑥-16 大数值不回退",
            InGameDetailFormat.Compose(L(P1("X", 123456)), 123456, true),
            Head("【未来】")
            + Line("P", "+123456 X")
            + Sub("P", "+123456"));
        
        
        
        string onlyTotalRendered = InGameDetailFormat.Compose(L(), 5, true);
        True("⑥-17 无分项但合计非 0 → 整段不含合计行（2.7.8 起退化到无内容可显示）",
            !HasTotalLine(onlyTotalRendered, out whyTotal), whyTotal ?? "");
        True("⑥-17b 该退化串比 2.7.7 的「分隔线 + 合计行」短（旧形态 = 2 行）",
            onlyTotalRendered.Replace(Environment.NewLine, "\n").TrimEnd('\n').IndexOf('\n') < 0,
            "长度 = " + onlyTotalRendered.Length + "，换行数 = " + CountOf(onlyTotalRendered, "\n"));
        Same("⑥-18 无分项 + 合计非 0：整段仍**不含合计行**（2.7.8 的极端退化场景）",
            HasTotalLine(InGameDetailFormat.Compose(L(), 5, true), out whyTotal) ? "有合计行" : "无合计行", "无合计行");

        
        Console.WriteLine("");
        Console.WriteLine("【⑦ 形态 / 颜色 / 转义（规范 §5）】");

        string escapeCase = InGameDetailFormat.Compose(L(P1("A<B>C&D(每回合)", 1)), 1, true);
        Same("⑦-1 明细文案里的 < > & 必须转义（TMP 富文本安全）", escapeCase,
            Head("【未来】")
            + Line("P", "+1 A&lt;B&gt;C&amp;D")
            + Sub("P", "+1"));
        NotHas("⑦-2 转义后不得残留裸 <B>", escapeCase, "<B>");
        Has("⑦-3 转义后必须含 &lt; / &gt; / &amp;",
            escapeCase, "&lt;B&gt;C&amp;D");
        Same("⑦-4 Escape 纯函数：& 必须先于 < 处理", InGameDetailFormat.Escape("&lt;"), "&amp;lt;");
        Same("⑦-5 Escape 空 / null", InGameDetailFormat.Escape(null) + "|" + InGameDetailFormat.Escape(""), "|");
        Same("⑦-6 Escape 不碰中文 / 箭头 / 括号", InGameDetailFormat.Escape("贝吉亚↑2档（2 档）"), "贝吉亚↑2档（2 档）");

        Same("⑦-7 正绿 / 负红 / 零灰", InGameDetailFormat.DeltaColor(1) + "|"
            + InGameDetailFormat.DeltaColor(-1) + "|" + InGameDetailFormat.DeltaColor(0), P + "|" + N + "|" + Z);
        True("⑦-8 颜色是合法 #RRGGBB", IsHex(P) && IsHex(N) && IsHex(Z), P + " / " + N + " / " + Z);
        True("⑦-9 颜色标记成对闭合", CountOf(specRendered, "<color=") == CountOf(specRendered, "</color>"),
            CountOf(specRendered, "<color=") + " 开 / " + CountOf(specRendered, "</color>") + " 闭");
        True("⑦-10 每行以 \\n 结尾（游戏原生换行方式）", specRendered.EndsWith(NL, StringComparison.Ordinal),
            "行数 = " + PlainLines(specRendered).Length);
        NotHas("⑦-11 不得出现游戏英文兜底串 no modifiers", specRendered, "no modifiers");
        NotHas("⑦-12 不得出现空颜色标记 <color=>", specRendered, "<color=>");
        True("⑦-13 正值行取绿", ColorOfLine(specRendered, "经济状态日益增长") == "P", "经济状态日益增长 → P");
        True("⑦-14 负值行取红", ColorOfLine(specRendered, "资本避税问题") == "N", "资本避税问题 → N");
        True("⑦-15 小计为 0 取灰", ColorOfLine(specRendered, "0 小计") == "Z", "小计 0 → Z");
        True("⑦-16 小计为负取红",
            ColorOfLine(InGameDetailFormat.Compose(L(E("ED=危险", -2), E("逃税(每回合)", -1)), -3, true), "-3 小计") == "N",
            "小计 -3 → N");
        True("⑦-17 段头取灰（不与数值抢色）", ColorOfLine(specRendered, "【未来】") == "Z", "【未来】 → Z");
        Same("⑦-18 Signed 形态 +1 / -1 / 0",
            InGameDetailFormat.Signed(1) + "|" + InGameDetailFormat.Signed(-1) + "|" + InGameDetailFormat.Signed(0),
            "+1|-1|0");
        SameInt("⑦-19 显示列宽：CJK 2 列 / ASCII 1 列（8 个汉字 = 16 列）",
            InGameDetailFormat.DisplayWidth("经济状态日益增长") * 100 + InGameDetailFormat.DisplayWidth("abc"),
            16 * 100 + 3);
        Same("⑦-20 ParenValue 纯函数：括号包住「值 + 空格 + 说明」（2.7.7 行格式的内核）",
            InGameDetailFormat.ParenValue("+1", "经济状态日益增长"), "(+1 经济状态日益增长)");
        Same("⑦-20b ParenValue：没有说明时只吐数值括号（原合计行的额度段曾用此形态；纯函数语义不变）",
            InGameDetailFormat.ParenValue("+3", null) + "|" + InGameDetailFormat.ParenValue("0", ""), "(+3)|(0)");

        
        
        
        int rowCount; string rowWhy;
        True("⑦-21 每一条明细行都是「(值 说明)」字节形态（金额在行首括号内）",
            AllRowsParen(specRendered, out rowCount, out rowWhy), rowWhy ?? "");
        True("⑦-21b 旧口径的「行尾金额」形态一条都不剩（金额不再靠补空格成列）",
            !HasTrailingValue(specRendered),
            "行尾带符号整数 = " + (HasTrailingValue(specRendered) ? "有" : "无"));

        var lines = PlainLines(specRendered);
        SameInt("⑦-22 行数比 2.7.7 少 2 行（合计行 + 它前面那条分隔线）：11 → 9", lines.Length, 9);
        Same("⑦-23 首行 = 段头【未来】", lines[0], "【未来】");
        Same("⑦-24 末行 = 小计行（2.7.8 起整段以小计收尾，不再是合计行）",
            lines[lines.Length - 1], "── (0 小计)");
        NotHas("⑦-25 整段没有任何合计行（Total: / 合计 / 预计 均不出现）",
            specRendered, "(+1) Total:");
        True("⑦-25b 合计行缺席判据（形态 + 字面四层口径）",
            !HasTotalLine(specRendered, out whyTotal), whyTotal ?? "");
        Same("⑦-26 画面文本逐行（剥标签后核对可读性）",
            string.Join(" / ", lines),
            "【未来】 / (+1 经济状态日益增长) / (+1 全国旅游业蓬勃发展) / (-1 额外福利支出) / ── (+1 小计)"
            + " / 【当前】 / (+1 贝吉亚地区发展提速) / (-1 资本避税问题) / ── (0 小计)");

        
        SameInt("⑦-27 ParseLineValue 读回「额外福利支出」= -1",
            InGameDetailFormat.ParseLineValue(specRendered, "额外福利支出") ?? 999, -1);
        SameInt("⑦-28 ParseLineValue 读回「贝吉亚地区发展提速」= +1",
            InGameDetailFormat.ParseLineValue(specRendered, "贝吉亚地区发展提速") ?? 999, 1);
        SameInt("⑦-29 ParseLineValue 仍能读回小计行（合计行取消后小计行就是末行）= +1",
            InGameDetailFormat.ParseLineValue(specRendered, "小计") ?? 999, 1);

        
        Console.WriteLine("");
        Console.WriteLine("【⑧ 全量文案无遗漏（规范 §4 每一条都能渲染出来）】");
        string[] allLabels =
        {
            "ED=危险", "ED=不稳定", "ED=稳定", "ED=增长", "ED=极快增长",
            "旅游业=蓬勃发展", "旅游业=平均水平", "旅游业=严重衰退",
            "贸易量=增加", "贸易量=恢复", "贸易量=减少",
            "税效率", "逃税", "腐败", "免费处方药",
            "避税(一次性)", "高就业", "产量增加", "小企业成长", "大企业创造就业", "军工复合体",
            "阿卡西亚投资", "康塔纳投资",
            "莱斯皮亚·煤炭贸易", "莱斯皮亚·鱼类贸易", "莱斯皮亚·钨贸易", "莱斯皮亚·小麦贸易",
            "莱斯皮亚·石油出售", "莱斯皮亚自由贸易区",
            "瓦尔戈斯兰·煤炭贸易", "瓦尔戈斯兰·鱼类贸易", "瓦尔戈斯兰·钨贸易", "瓦尔戈斯兰·小麦贸易",
            "瓦尔戈斯兰自由贸易区", "韦伦·石油自由贸易", "阿格诺利亚·农业市场", "伦堡·天然气管道",
            "贝吉亚↑1档", "贝吉亚↑2档", "洛伦↓1档", "洛伦↓2档",
        };
        int sameCount = 0, unknownCount = 0;
        var unknownList = new List<string>();
        foreach (var lb in allLabels)
        {
            var d = InGameDetailFormat.Display(lb);
            if (string.Equals(d, InGameDetailFormat.StripKindSuffix(lb), StringComparison.Ordinal)) { unknownCount++; unknownList.Add(lb); }
            else sameCount++;
            True("⑧ 文案：" + lb + " → " + d, d.Length > 0, "");
        }
        SameInt("⑧-1 §4 全部命中映射（" + allLabels.Length + " 条无一回退）", sameCount, allLabels.Length);
        True("⑧-2 没有任何标签回退成原名", unknownCount == 0,
            "未命中数 = " + unknownCount);

        
        var big = new List<InGameDetailFormat.Entry>();
        foreach (var lb in allLabels)
        {
            
            bool isPerTurnLabel = lb.StartsWith("ED=", StringComparison.Ordinal)
                || lb.StartsWith("旅游业=", StringComparison.Ordinal)
                || lb.StartsWith("贸易量=", StringComparison.Ordinal)
                || lb == "税效率" || lb == "逃税" || lb == "腐败" || lb == "免费处方药";
            big.Add(isPerTurnLabel ? P1(lb + "(每回合)", 1) : O1(lb + "(一次性)", 1));
        }
        string bigOut = InGameDetailFormat.Compose(big, InGameDetailFormat.SumDeltas(big), true);
        SameInt("⑧-3 全量 " + allLabels.Length + " 条同时上屏：各项之和 == 归并后各行之和（归并不增不减）",
            InGameDetailFormat.SumRows(InGameDetailFormat.BuildRows(big)), InGameDetailFormat.SumDeltas(big));
        True("⑧-3b 全量 " + allLabels.Length + " 条同时上屏：整段**不含合计行**",
            !HasTotalLine(bigOut, out whyTotal), whyTotal ?? "");
        True("⑧-4 全量上屏不崩且两段都在",
            bigOut.IndexOf("【未来】", StringComparison.Ordinal) >= 0
                && bigOut.IndexOf("【当前】", StringComparison.Ordinal) >= 0,
            "行数 = " + PlainLines(bigOut).Length);
        True("⑧-5 全量上屏后颜色标记仍成对闭合",
            CountOf(bigOut, "<color=") == CountOf(bigOut, "</color>"),
            CountOf(bigOut, "<color=") + " 开 / " + CountOf(bigOut, "</color>") + " 闭");
        int bigRows; string bigWhy;
        True("⑧-6 全量 " + allLabels.Length + " 条上屏后每一条明细行仍是「(值 说明)」形态",
            AllRowsParen(bigOut, out bigRows, out bigWhy), bigWhy ?? "");
        True("⑧-6b 全量上屏后没有任何「行尾金额」残留",
            !HasTrailingValue(bigOut), "行尾带符号整数 = " + (HasTrailingValue(bigOut) ? "有" : "无"));

        Console.WriteLine("");
        
        
        
        
        
        
        
        
        
        
        
        Console.WriteLine("");
        Console.WriteLine("【⑨ 悬停注入：只追加 / 零标记字符 / 内存指纹幂等（2.7.5）】");

        
        const string gameTooltip = "政府预算 <alpha=#44><size=16>(min: -20, max: +50)";

        var detail = InGameDetailFormat.Compose(
            new List<InGameDetailFormat.Entry> { P1("ED=增长", 1), O1("避税(一次性)", -1) }, 0, true,
            InGameDetailFormat.PreviewTitle, null);
        string merged = InGameDetailFormat.MergeDetail(gameTooltip, detail);
        string why;

        True("⑨-1 只追加：游戏原文逐字节保留为前缀",
            merged.StartsWith(gameTooltip, StringComparison.Ordinal),
            "原文 " + gameTooltip.Length + " 字符 → 合并 " + merged.Length + " 字符");
        True("⑨-2 明细挂在游戏原文之后（不插进原文中间）",
            merged.IndexOf(InGameDetailFormat.PreviewTitle, StringComparison.Ordinal) > gameTooltip.Length,
            "明细起点 = " + merged.IndexOf(InGameDetailFormat.PreviewTitle, StringComparison.Ordinal));
        Same("⑨-3 合并串的前缀就是游戏原文（逐字节）",
            merged.Substring(0, gameTooltip.Length), gameTooltip);

        
        True("⑨-4 整段文本零可见标记字符（逐字符检查 <!-- / --> / OI-DETAIL）",
            InGameDetailFormat.IsMarkerFree(merged, out why) && !InGameDetailFormat.ContainsVisibleMarker(merged, out why),
            why ?? "零标记");
        NotHas("⑨-4b 不含旧开标记 <!--OI-DETAIL-->", merged, "<!--OI-DETAIL-->");
        NotHas("⑨-4c 不含旧闭标记 <!--/OI-DETAIL-->", merged, "<!--/OI-DETAIL-->");
        SameInt("⑨-4d 不含任何 '<!--'（计数 = 0）", CountOf(merged, "<!--"), 0);
        SameInt("⑨-4e 不含任何 '-->'（计数 = 0）", CountOf(merged, "-->"), 0);
        SameInt("⑨-4f 不含 'OI-DETAIL' 字样（计数 = 0）", CountOf(merged, "OI-DETAIL"), 0);
        SameInt("⑨-4g 全量 41 条上屏的合并串同样零标记",
            InGameDetailFormat.ContainsVisibleMarker(
                InGameDetailFormat.MergeDetail(gameTooltip, bigOut), out why) ? 1 : 0, 0);

        
        Same("⑨-5 原文与明细之间恰好一个空行（原文 → 空行 → 明细）",
            merged.Substring(gameTooltip.Length, InGameDetailFormat.DetailGap.Length), InGameDetailFormat.DetailGap);
        Same("⑨-5b 空行之后就是明细正文本身（中间没有任何标记）",
            merged.Substring(gameTooltip.Length + InGameDetailFormat.DetailGap.Length), detail);
        SameInt("⑨-5c 合并长度 == 原文 + 空行 + 明细（无额外字符）",
            merged.Length, gameTooltip.Length + InGameDetailFormat.DetailGap.Length + detail.Length);

        
        var mem = new InGameDetailFormat.DetailMergeMemory();
        object slot = new object();                       

        bool hit;
        string m1 = mem.Merge(slot, gameTooltip, detail, out hit);
        Same("⑨-6 首次合并（输入 = 游戏原文）== 纯拼接结果", m1, merged);
        True("⑨-6b 首次合并**不**命中指纹（输入是游戏原文，不是我方产物）", !hit, "命中=" + hit);
        Same("⑨-6c 记忆里记下的合并前原文 == 游戏原文", mem.LastBase, gameTooltip);
        Same("⑨-6d 记忆里的合并串 == 写出的文本", mem.LastMerged, merged);
        SameInt("⑨-6e 记忆里只有 1 条历史", mem.HistoryCount, 1);

        
        bool allSame20 = true;
        for (int i = 0; i < 20; i++)
        {
            var again = mem.Merge(slot, gameTooltip, detail, out hit);
            if (!string.Equals(again, merged, StringComparison.Ordinal)) allSame20 = false;
        }
        True("⑨-6f 幂等：输入恒为游戏原文 → 连续 20 轮输出逐字节相同（不回落、不膨胀）",
            allSame20, "长度 = " + merged.Length + "，累计合并 = " + mem.Merges + " 次");
        SameInt("⑨-6g 这 20 轮一次都没命中指纹（说明没把自己的产物当原文回灌）",
            mem.FingerprintHits, 0);

        
        var regurge = mem.Merge(slot, merged, detail, out hit);
        Same("⑨-7 回灌我方产物：命中指纹且输出逐字节不变（绝不累积）", regurge, merged);
        True("⑨-7b 回灌这一次确实命中了指纹", hit, "命中=" + hit);
        Same("⑨-7c 回灌后记忆里的原文仍是游戏原文（没被污染成合并串）", mem.LastBase, gameTooltip);
        SameInt("⑨-7d 回灌后长度 == 首次合并长度", regurge.Length, merged.Length);
        SameInt("⑨-7e 回灌后整段仍是零标记字符", InGameDetailFormat.ContainsVisibleMarker(regurge, out why) ? 1 : 0, 0);

        
        var detail3 = InGameDetailFormat.Compose(
            new List<InGameDetailFormat.Entry> { P1("ED=增长", 3) }, 3, true,
            InGameDetailFormat.PreviewTitle, null);
        string merged3 = mem.Merge(slot, merged, detail3, out hit);
        Same("⑨-8 明细变化 + 输入是旧产物 → 结果 == 原文 + 新明细（替换，不是叠加）",
            merged3, InGameDetailFormat.MergeDetail(gameTooltip, detail3));
        SameInt("⑨-8b 新明细只有一段（标题只出现 1 次）",
            CountOf(merged3, InGameDetailFormat.PreviewTitle), 1);
        NotHas("⑨-8c 旧明细不再在屏上（逐字整段判据：不是靠「合计行」这一行代替）", merged3, detail);
        Has("⑨-8d 新明细已在屏上（逐字整段）", merged3, detail3);
        Same("⑨-8e 明细变化后游戏原文仍逐字节保留为前缀",
            merged3.Substring(0, gameTooltip.Length), gameTooltip);
        True("⑨-8f 明细变化后的整段文本同样零标记字符",
            InGameDetailFormat.IsMarkerFree(merged3, out why), why ?? "零标记");

        True("⑨-8g 追加段 TMP 标签成对闭合（<color>/<b>/<u>/<align> 逐个配对）",
            InGameDetailFormat.RichTagsClosed(detail, out why),
            why ?? ("color 开=" + CountOf(detail, "<color=") + " 闭=" + CountOf(detail, "</color>")
                + "｜b 开=" + CountOf(detail, "<b>") + " 闭=" + CountOf(detail, "</b>")
                + "｜u 开=" + CountOf(detail, "<u>") + " 闭=" + CountOf(detail, "</u>")
                + "｜align 开=" + CountOf(detail, "<align=center>") + " 闭=" + CountOf(detail, "</align>")));
        SameInt("⑨-8i 明细正文里没有任何 '-->'（旧闭标记的残留形态也不会出现）",
            CountOf(detail, "-->"), 0);

        Has("⑨-9 游戏原文的无闭合标签 <alpha=#44> 原样保留", merged, "<alpha=#44>");
        Has("⑨-9b 游戏原文的 (min: -20, max: +50) 原样保留", merged, "(min: -20, max: +50)");

        
        const string rewritten = "政府预算 (min: -20, max: +50)";
        var memRew = new InGameDetailFormat.DetailMergeMemory();
        object slotRew = new object();
        memRew.Merge(slotRew, gameTooltip, detail, out hit);
        string rew = memRew.Merge(slotRew, rewritten, detail, out hit);
        Same("⑨-10 游戏重写 → 以新原文为基准重拼（不是叠在旧文本上）",
            rew, InGameDetailFormat.MergeDetail(rewritten, detail));
        True("⑨-10b 这一次不命中指纹（游戏原文 ≠ 我方产物）", !hit, "命中=" + hit);
        Has("⑨-10c 游戏重写后明细仍在（新标题在屏上）", rew, InGameDetailFormat.PreviewTitle);
        NotHas("⑨-10d 重写后的整段里没有旧原文的残留", rew, gameTooltip);

        
        
        var memStale = new InGameDetailFormat.DetailMergeMemory();
        object slotStale = new object();
        string gen1 = memStale.Merge(slotStale, rewritten, detail, out hit);     
        string gen2 = memStale.Merge(slotStale, rewritten, detail3, out hit);    
        string stale = memStale.Merge(slotStale, gen1, detail3, out hit);        
        Same("⑨-11 在途的**上一代**合并串命中历史指纹 → 从原文重拼，绝不累积", stale, gen2);
        True("⑨-11b 这一次命中的是历史（不是最后一条）", hit, "命中=" + hit);
        SameInt("⑨-11c 历史命中后标题仍只有 1 段", CountOf(stale, InGameDetailFormat.PreviewTitle), 1);
        SameInt("⑨-11d 历史条数按合并次数增长（未到上限时不丢）", memStale.HistoryCount, 3);
        for (int i = 0; i < InGameDetailFormat.DetailMergeMemory.Capacity + 3; i++)
            memStale.Merge(slotStale, rewritten, "明细" + i, out hit);
        SameInt("⑨-11e 历史栏位到顶后不再增长（上限 = Capacity）",
            memStale.HistoryCount, InGameDetailFormat.DetailMergeMemory.Capacity);
        True("⑨-11f 超出上限后最老的一条被挤出（第 1 代产物不再被认成我方文本）",
            !memStale.MatchesMerged(gen1), "容量 = " + InGameDetailFormat.DetailMergeMemory.Capacity);

        
        string onlyOurs = InGameDetailFormat.MergeDetail("", detail);
        True("⑨-13 原文为空 → 只有「空行 + 明细」，没有任何标记占位",
            onlyOurs.StartsWith(InGameDetailFormat.DetailGap, StringComparison.Ordinal),
            Show(onlyOurs.Substring(0, Math.Min(28, onlyOurs.Length))));
        Same("⑨-13b 明细为空 → 原样返回原文（不写空段、不留空行）",
            InGameDetailFormat.MergeDetail(merged, ""), merged);
        Same("⑨-13c 明细为 null → 原样返回原文",
            InGameDetailFormat.MergeDetail(gameTooltip, null), gameTooltip);
        Same("⑨-13d 原文为 null + 明细非空 → 空行 + 明细（不抛异常）",
            InGameDetailFormat.MergeDetail(null, detail), InGameDetailFormat.DetailGap + detail);
        Same("⑨-13e 记忆：明细为空时原样放行，且不写记忆",
            new InGameDetailFormat.DetailMergeMemory().Merge(new object(), gameTooltip, "", out hit), gameTooltip);

        
        var d14 = InGameDetailFormat.Compose(new List<InGameDetailFormat.Entry> { P1("ED=增长", 1) }, 1, true,
            InGameDetailFormat.PreviewTitle, null);
        string m14 = InGameDetailFormat.MergeDetail(gameTooltip, d14);
        SameInt("⑨-14 合并后明细行（按文案定位）仍可读回 = +1",
            InGameDetailFormat.ParseLineValue(m14, "经济状态日益增长") ?? -999, 1);
        True("⑨-14b 合并后整段仍**不含合计行**（Total: 也没有跟着恢复出来）",
            !HasTotalLine(m14, out whyTotal), whyTotal ?? "");

        SameInt("⑨-15 CountOf 非重叠计数", InGameDetailFormat.CountOf("aXbXcX", "X"), 3);
        SameInt("⑨-15b CountOf 空 needle = 0", InGameDetailFormat.CountOf("abc", ""), 0);
        True("⑨-15c 守门断言不是空断言：含标记的文本确实会被判出",
            InGameDetailFormat.ContainsVisibleMarker("x" + "<!--OI-DETAIL-->" + "y", out why)
            && !InGameDetailFormat.IsMarkerFree("<!--OI-DETAIL-->", out why), why ?? "");

        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        Console.WriteLine("");
        Console.WriteLine("【⑩ 生产者注入 + 重写看门狗：内存指纹收敛（2.7.4 引入 / 2.7.5 去标记）】");

        
        var memP = new InGameDetailFormat.DetailMergeMemory();
        object slotP = new object();
        string producerOut = null;
        bool allSame = true;
        const int producerRounds = 20;
        for (int i = 0; i < producerRounds; i++)
        {
            string produced = memP.Merge(slotP, gameTooltip, detail, out hit);
            if (i == 0) producerOut = produced;
            else if (!string.Equals(produced, producerOut, StringComparison.Ordinal)) allSame = false;
        }
        True("⑩-1 生产者输入恒为游戏原文 → 连续 " + producerRounds + " 次输出逐字节相同（不回落、不膨胀）",
            allSame, "长度 = " + (producerOut == null ? -1 : producerOut.Length));
        SameInt("⑩-1b 生产者输出长度 == 单次合并长度", producerOut.Length, merged.Length);
        Same("⑩-1c 生产者输出与 ⑨ 段单次合并结果一致", producerOut, merged);
        SameInt("⑩-1d " + producerRounds + " 轮后标题仍只有 1 段",
            CountOf(producerOut, InGameDetailFormat.PreviewTitle), 1);
        SameInt("⑩-1e " + producerRounds + " 轮后整段仍零标记字符",
            InGameDetailFormat.ContainsVisibleMarker(producerOut, out why) ? 1 : 0, 0);
        Same("⑩-1f " + producerRounds + " 轮后前缀仍是游戏原文（逐字节）",
            producerOut.Substring(0, gameTooltip.Length), gameTooltip);

        
        var memW = new InGameDetailFormat.DetailMergeMemory();
        object slotW = new object();
        string watchdogOut = memW.Merge(slotW, gameTooltip, detail, out hit);
        string field = watchdogOut;
        for (int i = 0; i < 20; i++)
        {
            field = gameTooltip;                                        
            var want = memW.Merge(slotW, field, detail, out hit);       
            if (!string.Equals(want, field, StringComparison.Ordinal))
                field = want;                                           
        }
        Same("⑩-2 「游戏重写 → 看门狗补回」交替 20 轮后收敛到生产者产物（长度不回落到 51）", field, watchdogOut);
        SameInt("⑩-2b 交替 20 轮后长度 != 游戏原文长度",
            field.Length == gameTooltip.Length ? -1 : field.Length, watchdogOut.Length);
        SameInt("⑩-2c 20 次重写全被判为「需补回」（输入是游戏原文 ⇒ 一次都不命中指纹）",
            memW.FingerprintHits, 0);
        SameInt("⑩-2d 交替 20 轮后标题仍只有 1 段", CountOf(field, InGameDetailFormat.PreviewTitle), 1);

        
        var memC = new InGameDetailFormat.DetailMergeMemory();
        object slotC = new object();
        string cur = memC.Merge(slotC, gameTooltip, detail, out hit);
        Same("⑩-3 判据：传入 == 我方产物 → 该写什么与现值相同 → 早退（不写回、不自激）",
            memC.Merge(slotC, cur, detail, out hit), cur);
        True("⑩-3b 这一次命中了指纹（判据来自内存，不是文本标记）", hit, "命中=" + hit);
        SameInt("⑩-3c 判据命中后长度不增长", cur.Length, merged.Length);

        
        var detailV2 = InGameDetailFormat.Compose(
            new List<InGameDetailFormat.Entry> { P1("ED=增长", -2), O1("避税(一次性)", -1) }, -3, true,
            InGameDetailFormat.PreviewTitle, null);
        var memV2 = new InGameDetailFormat.DetailMergeMemory();
        object slotV2 = new object();
        memV2.Merge(slotV2, gameTooltip, detail, out hit);
        string producerOutV2 = memV2.Merge(slotV2, gameTooltip, detailV2, out hit);
        NotHas("⑩-4 明细内容变化 → 旧内容不再在屏上（逐字判据）", producerOutV2, detail);
        Has("⑩-4b 明细内容变化 → 新内容上屏（逐字判据）", producerOutV2, detailV2);
        Same("⑩-4c 明细内容变化后游戏原文仍逐字节保留为前缀",
            producerOutV2.Substring(0, gameTooltip.Length), gameTooltip);
        SameInt("⑩-4d 明细内容变化后仍零标记字符",
            InGameDetailFormat.ContainsVisibleMarker(producerOutV2, out why) ? 1 : 0, 0);

        
        var memE = new InGameDetailFormat.DetailMergeMemory();
        object slotOld = new object();
        object slotNew = new object();
        string oldSlotText = memE.Merge(slotOld, gameTooltip, detail, out hit);
        int mergesBefore = memE.Merges;
        string newSlotText = memE.Merge(slotNew, gameTooltip, detail, out hit);
        True("⑩-5 换 TMP 实例 → 记忆作废（Owner 换成新实例）",
            ReferenceEquals(memE.Owner, slotNew), "Owner = " + (ReferenceEquals(memE.Owner, slotNew) ? "新实例" : "旧实例"));
        True("⑩-5b 换实例后不把新实例的文本认成旧实例的产物（不命中指纹）", !hit, "命中=" + hit);
        Same("⑩-5c 换实例后输出与旧实例完全相同（同一份原文 + 同一份明细）", newSlotText, oldSlotText);
        SameInt("⑩-5d 换实例后历史被清空并重新计数（旧实例记了 " + mergesBefore + " 次）",
            memE.HistoryCount, 1);

        
        
        
        
        
        Console.WriteLine("");
        Console.WriteLine("【⑪ 标题 = 当前预测下的财政状态（2.7.6 逐字指定）】");

        Same("⑪-1 标题常量逐字 == 当前预测下的财政状态（2.7.5 曾是「…的未来财政状态」）",
            InGameDetailFormat.PreviewTitle, "当前预测下的财政状态");
        NotHas("⑪-1b 2.7.6 标题里不再有「未来」二字（按用户本次示例）",
            InGameDetailFormat.PreviewTitle, "未来");
        True("⑪-2 标题不再含旧文案「下一回合预算变化」",
            InGameDetailFormat.PreviewTitle.IndexOf("下一回合预算变化", StringComparison.Ordinal) < 0, "");
        True("⑪-3 标题不再含旧后缀「（按当前词条）」",
            InGameDetailFormat.PreviewTitle.IndexOf("（按当前词条）", StringComparison.Ordinal) < 0, "");
        Same("⑪-4 明细首行 == 新标题（标题仍在段头之前）", PlainLines(detail)[0], "当前预测下的财政状态");
        Same("⑪-5 第 2 行 = 标题下新增的细分隔线（2.7.6）", PlainLines(detail)[1], "──────");
        Same("⑪-5b 第 3 行 = 【未来】段头", PlainLines(detail)[2], "【未来】");
        Has("⑪-6 段头 = 【未来】", detail, "【未来】");
        Has("⑪-7 段头 = 【当前】", detail, "【当前】");
        NotHas("⑪-7b 旧段头【每回合】已不存在", detail, "【每回合】");
        NotHas("⑪-7c 旧段头【本回合】已不存在", detail, "【本回合】");
        Has("⑪-8 小计行仍在（两段各一，带 ── 前导，值与标签同在括号内）", Plain(detail), "── (+1 小计)");
        Has("⑪-8b 【当前】段小计（净额 -1）同样在括号内", Plain(detail), "── (-1 小计)");
        True("⑪-9 明细整段**不含合计行**（2.7.8 取消：Total: / 合计 / 预计 与合计行形态全部未命中）",
            !HasTotalLine(detail, out whyTotal), whyTotal ?? "");
        NotHas("⑪-9b 字面「Total:」不再出现（Rizia 英文原词也一并消失）", detail, "Total:");
        NotHas("⑪-9c 中文「合计」「预计」两个旧标签都不出现", detail, "合计");
        NotHas("⑪-9d 中文「预计」同样不出现", detail, "预计");
        Same("⑪-10 分隔线字符未动（仍只用于标题下；合计行取消后整段只剩它一条）",
            InGameDetailFormat.TotalDivider, "──────");
        SameInt("⑪-10b 该分隔线在带标题的明细里恰好出现 1 次（2.7.7 是 2 次）",
            CountOf(detail, InGameDetailFormat.TotalDivider), 1);
        SameInt("⑪-11 分项文案表仍是 41 条（用户要求：41 条分项文案一个字都不动）", allLabels.Length, 41);
        SameInt("⑪-12 41 条文案逐条仍全部命中映射（无回退）", sameCount, allLabels.Length);

        
        
        
        
        Console.WriteLine("");
        Console.WriteLine("【⑫ 排版契约（(值 说明) 行格式 / 居中标题 / 段头 / 下划线 / 分隔线 / 黄金串）】");

        
        SameInt("⑫-A1 DisplayWidth：ASCII 每个 1 列（\"abc\" → 3）", InGameDetailFormat.DisplayWidth("abc"), 3);
        SameInt("⑫-A2 DisplayWidth：CJK 每个 2 列（8 汉字 → 16）", InGameDetailFormat.DisplayWidth("经济状态日益增长"), 16);
        SameInt("⑫-A3 DisplayWidth：混合「贝吉亚↑2档（2 档）」= 7×2 + 箭头2 + 1 + 档2 + 全角（2 + 空格1 + 2 + 档2 + 全角）2 = 18",
            InGameDetailFormat.DisplayWidth("贝吉亚↑2档（2 档）"), 18);
        SameInt("⑫-A4 DisplayWidth：空串 → 0", InGameDetailFormat.DisplayWidth(""), 0);
        SameInt("⑫-A5 DisplayWidth：null → 0（不抛）", InGameDetailFormat.DisplayWidth(null), 0);
        SameInt("⑫-A6 DisplayWidth：半角空格仍算 1 列", InGameDetailFormat.DisplayWidth("  "), 2);
        SameInt("⑫-A7 emoji 边界：单个代理对（U+1F600）算 2 列，**不按 2 个 char 各算 1 列**",
            InGameDetailFormat.DisplayWidth("\U0001F600"), 2);
        SameInt("⑫-A8 emoji 边界：混合 \"a😀b\" = 1 + 2 + 1 = 4（代理对整体算一格）",
            InGameDetailFormat.DisplayWidth("a\U0001F600b"), 4);
        SameInt("⑫-A9 emoji 边界：零宽连接符 U+200D 不占列（\"a\\u200Db\" → 2）",
            InGameDetailFormat.DisplayWidth("a\u200Db"), 2);
        SameInt("⑫-A10 emoji 边界：变体选择符 U+FE0F 不占列（\"❤️\" → 1）",
            InGameDetailFormat.DisplayWidth("\u2764\uFE0F"), 1);
        SameInt("⑫-A11 孤立代理（未配对的高位代理）不崩，按 1 列兜底",
            InGameDetailFormat.DisplayWidth("a\uD83D"), 2);
        True("⑫-A12 列补齐入口已随 2.7.7 全部删除（PadColumns / DisplayColumns / RowIndent / MinLabelColumns —— 编译期即证）",
            InGameDetailFormat.DisplayWidth("经济状态日益增长") == 16,
            "DisplayWidth 语义不变（8 汉字 = 16 列）；**2.7.9 起重新有生产调用点**（EmitFramed 算框宽/居中），见 ⑬ 段");

        
        string aligned = InGameDetailFormat.Compose(
            L(P1("ED=增长", 1), P1("旅游业=蓬勃发展", -2), P1("免费处方药(每回合)", 3),
              O1("贝吉亚↑1档", 4), O1("避税(一次性)", -5)), 1, true,
            InGameDetailFormat.PreviewTitle, null);
        var aLines = PlainLines(aligned);
        string why2;
        int alignedRows;
        True("⑫-B1 每一条明细行都是「(值 说明)」形态（中文 / 全角 / 正负号混合）",
            AllRowsParen(aligned, out alignedRows, out why2), why2 ?? "全部合规");
        True("⑫-B2 金额落在**行首括号内**（第 0 列就是 '('，不再有缩进、不再补空格）",
            aLines[3].StartsWith("(", StringComparison.Ordinal)
                && aLines[4].StartsWith("(", StringComparison.Ordinal)
                && aLines[5].StartsWith("(", StringComparison.Ordinal)
                && aLines[8].StartsWith("(", StringComparison.Ordinal)
                && aLines[9].StartsWith("(", StringComparison.Ordinal)
                && aLines[3][1] == '+' && aLines[4][1] == '-' && aLines[5][1] == '+',
            "5 条明细行首 = ( ｜ 第 2 个字符 = 符号（" + aLines[3][1] + aLines[4][1] + aLines[5][1] + aLines[8][1] + aLines[9][1] + "）");
        True("⑫-B3 数值与说明之间恰好一个空格（不补列宽：整段不得出现连续两个空格）",
            !AnyDoubleSpace(aligned),
            "连续两个空格 = " + (AnyDoubleSpace(aligned) ? "有" : "无"));
        True("⑫-B4 排版不依赖容器宽度：整段里不得出现 <pos= / <align=right> / <margin=",
            aligned.IndexOf("<pos=", StringComparison.Ordinal) < 0
                && aligned.IndexOf("<align=right", StringComparison.Ordinal) < 0
                && aligned.IndexOf("<margin=", StringComparison.Ordinal) < 0
                && aligned.IndexOf("<size=", StringComparison.Ordinal) < 0
                && aligned.IndexOf("<font", StringComparison.Ordinal) < 0,
            "只允许 color/b/u/align=center 四类标签");

        
        string longLabel = InGameDetailFormat.Compose(L(O1("洛伦↓2档", -2)), -2, true,
            InGameDetailFormat.PreviewTitle, null);
        var lLines = PlainLines(longLabel);
        string longWhy;
        True("⑫-B5 超长文案不截断（整条照写；金额仍在行首括号内，不因文案长短错位）",
            lLines[3] == "(-2 洛伦地区发展大幅倒退（2 档）)"
                && IsParenRow(longLabel.Split('\n')[3], out longWhy),
            "第 4 行 = " + lLines[3] + "（" + InGameDetailFormat.DisplayWidth("洛伦地区发展大幅倒退（2 档）") + " 列）");

        
        SameInt("⑫-C1 <align=center> 开标签整段恰好 1 个", CountOf(aligned, InGameDetailFormat.AlignCenterOpen), 1);
        SameInt("⑫-C2 </align> 闭标签整段恰好 1 个（成对闭合）", CountOf(aligned, InGameDetailFormat.AlignCenterClose), 1);
        SameInt("⑫-C3 含居中标签的行**只有标题那一行**", CountOfLinesWith(aligned, InGameDetailFormat.AlignCenterOpen), 1);
        True("⑫-C4 居中标签与标题在同一行、且标题被标签包住",
            aligned.StartsWith(InGameDetailFormat.AlignCenterOpen + InGameDetailFormat.BoldOpen
                + "<color=" + Z + ">" + InGameDetailFormat.PreviewTitle + "</color>"
                + InGameDetailFormat.BoldClose + InGameDetailFormat.AlignCenterClose + NL, StringComparison.Ordinal),
            "标题行 = <align=center><b><color=…>标题</color></b></align>");
        SameInt("⑫-C5 标题行是整段第 1 行", aLines[0] == InGameDetailFormat.PreviewTitle ? 1 : 0, 1);
        True("⑫-C6 标题行里没有任何可见标记（剥标签后恰好等于标题本身）",
            StripTags(aligned.Split('\n')[0]) == InGameDetailFormat.PreviewTitle, StripTags(aligned.Split('\n')[0]));

        
        Same("⑫-D1 段头常量（每回合类）逐字 == 【未来】", InGameDetailFormat.SectionLabelPerTurn, "【未来】");
        Same("⑫-D2 段头常量（一次性类）逐字 == 【当前】", InGameDetailFormat.SectionLabelOneTime, "【当前】");
        Has("⑫-D3 屏上出现【未来】", aligned, "【未来】");
        Has("⑫-D4 屏上出现【当前】", aligned, "【当前】");
        NotHas("⑫-D5 屏上不再出现旧段头【每回合】", aligned, "【每回合】");
        NotHas("⑫-D6 屏上不再出现旧段头【本回合】", aligned, "【本回合】");
        SameInt("⑫-D7 两段段头各出现 1 次", CountOf(aligned, "【未来】") + CountOf(aligned, "【当前】"), 2);
        True("⑫-D8 段头加粗（<b> 包住段头文字）",
            aligned.IndexOf("<b><color=" + Z + ">" + InGameDetailFormat.SectionLabelPerTurn + "</color></b>", StringComparison.Ordinal) >= 0
                && aligned.IndexOf("<b><color=" + Z + ">" + InGameDetailFormat.SectionLabelOneTime + "</color></b>", StringComparison.Ordinal) >= 0,
            "段头 = <b><color=#9A9A9A>【…】</color></b>");

        
        Has("⑫-E1 小计行带 ── 前导且值与标签同在括号内", Plain(aligned), "── (+2 小计)");
        Has("⑫-E2 小计标签加下划线（在括号内）", aligned, "<u>" + InGameDetailFormat.SubtotalLabel + "</u>");
        NotHas("⑫-E3 整段不再出现合计行标签 <u>Total:</u>（2.7.8 取消合计行的字节判据）",
            aligned, "<u>Total:</u>");
        SameInt("⑫-E4 下划线标签成对：<u> 与 </u> 各 2 个（只剩两段小计；2.7.7 是 3 个含合计行）",
            CountOf(aligned, "<u>") * 100 + CountOf(aligned, "</u>"), 2 * 100 + 2);
        NotHas("⑫-E5 数值不在下划线内（下划线只压标签词）", aligned, "<u>+1</u>");
        NotHas("⑫-E5b 小计的数值同样不在下划线内", aligned, "<u>+2</u>");

        
        True("⑫-F1 每条明细行都以「(」开头（金额在行首括号内 —— 取代旧的两格缩进）",
            aLines[3].StartsWith("(", StringComparison.Ordinal)
                && aLines[4].StartsWith("(", StringComparison.Ordinal)
                && aLines[5].StartsWith("(", StringComparison.Ordinal)
                && aLines[8].StartsWith("(", StringComparison.Ordinal)
                && aLines[9].StartsWith("(", StringComparison.Ordinal),
            "5 条明细行首 = 「(值 说明)」");
        SameInt("⑫-F2 段间恰好一个空行（\\n\\n，不是两个）", CountOf(aligned, "\n\n"), 1);
        SameInt("⑫-F3 段间空行的位置：在【当前】段头之前",
            aligned.IndexOf("\n\n" + InGameDetailFormat.BoldOpen, StringComparison.Ordinal) >= 0 ? 1 : 0, 1);
        SameInt("⑫-F4 分隔线共 1 条：只剩标题下那条（2.7.7 是 2 条，含合计前那条）",
            CountOf(aligned, InGameDetailFormat.TotalDivider), 1);
        SameInt("⑫-F5 标题下的分隔线紧跟标题行（第 2 行）", aLines[1] == InGameDetailFormat.TotalDivider ? 1 : 0, 1);
        SameInt("⑫-F6 末行**不是**分隔线（合计前那条已随合计行删除）—— 末行 = 末段小计行",
            (aLines[aLines.Length - 1].IndexOf("小计", StringComparison.Ordinal) >= 0
             && aLines[aLines.Length - 1] != InGameDetailFormat.TotalDivider) ? 1 : 0, 1);
        True("⑫-F7 整段末尾没有合计行 —— 末行过不了「(净额) <u>Total:</u>」字节判据",
            !IsParenTotal(LastNonEmpty(aligned), out why2), why2 ?? "");
        True("⑫-F7b 整段合计行缺席判据（形态 + 字面四层口径）",
            !HasTotalLine(aligned, out whyTotal), whyTotal ?? "");

        
        True("⑫-G1 RichTagsClosed：新排版整段全部标签配对",
            InGameDetailFormat.RichTagsClosed(aligned, out why2), why2 ?? "全部闭合");
        True("⑫-G2 RichTagsClosed：未闭合的 <b> 会被判出（守门断言不是空断言）",
            !InGameDetailFormat.RichTagsClosed("<b>x", out why2), why2 ?? "");
        True("⑫-G3 RichTagsClosed：交错的标签会被判出",
            !InGameDetailFormat.RichTagsClosed("<b><u>x</b></u>", out why2), why2 ?? "");
        True("⑫-G4 RichTagsClosed：未识别的标签会被判出（TMP 会把它当普通文本显示）",
            !InGameDetailFormat.RichTagsClosed("<shake>x</shake>", out why2), why2 ?? "");
        True("⑫-G5 整段文本零可见标记字符",
            InGameDetailFormat.IsMarkerFree(aligned, out why2), why2 ?? "零标记");
        SameInt("⑫-G6 四类标签各自开闭数量相等",
            (CountOf(aligned, "<color=") == CountOf(aligned, "</color>")
             && CountOf(aligned, "<b>") == CountOf(aligned, "</b>")
             && CountOf(aligned, "<u>") == CountOf(aligned, "</u>")
             && CountOf(aligned, "<align=center>") == CountOf(aligned, "</align>")) ? 1 : 0, 1);
        SameInt("⑫-G7 追加段不引入字号/字体标签（HUD 适配不受影响）",
            CountOf(aligned, "<size") + CountOf(aligned, "<font") + CountOf(aligned, "<alpha"), 0);

        
        string golden = InGameDetailFormat.Compose(
            L(E("ED=增长", 1), E("旅游业=蓬勃发展", 1),
              G1("免费处方药(每回合)", -1, InGameDetailFormat.KindPerTurn, InGameDetailFormat.GroupExtraWelfare),
              O1("贝吉亚↑1档", 1), O1("避税(一次性)", -1)), 1, true,
            InGameDetailFormat.PreviewTitle, null);
        Same("⑫-H1 目标形态逐字节对照（居中标题 / 两段 / (值 说明) / ── 小计；2.7.8 已无合计行）", golden,
            "<align=center><b><color=#9A9A9A>当前预测下的财政状态</color></b></align>\n"
            + "<color=#9A9A9A>──────</color>\n"
            + "<b><color=#9A9A9A>【未来】</color></b>\n"
            + "<color=#6FBF52>(+1 经济状态日益增长)</color>\n"
            + "<color=#6FBF52>(+1 全国旅游业蓬勃发展)</color>\n"
            + "<color=#D9534F>(-1 额外福利支出)</color>\n"
            + "<color=#6FBF52>── (+1 <u>小计</u>)</color>\n"
            + "\n"
            + "<b><color=#9A9A9A>【当前】</color></b>\n"
            + "<color=#6FBF52>(+1 贝吉亚地区发展提速)</color>\n"
            + "<color=#D9534F>(-1 资本避税问题)</color>\n"
            + "<color=#9A9A9A>── (0 <u>小计</u>)</color>\n");
        SameInt("⑫-H2 黄金串 11 行（不含空行）/ 2.7.7 是 13 行（少掉合计行 + 它前面那条分隔线）",
            PlainLines(golden).Length, 11);
        NotHas("⑫-H3 黄金串末尾没有合计行（逐字节：连 <u>Total:</u> 都不出现）", golden, "<u>Total:</u>");
        SameInt("⑫-H4 黄金串两段小计可读回（+1 / 0）",
            (InGameDetailFormat.ParseLineValue(golden, "小计") ?? 999) == 1 ? 1 : 0, 1);
        string hWhy; int hRows;
        True("⑫-H5 黄金串的每一条明细行都过「(值 说明)」字节判据",
            AllRowsParen(golden, out hRows, out hWhy), hWhy ?? "");
        True("⑫-H6 黄金串里整段合计行缺席（形态 + 字面四层口径）",
            !HasTotalLine(golden, out hWhy), hWhy ?? "");
        True("⑫-H7 黄金串里没有任何「行尾金额」残留（旧形态彻底消失）",
            !HasTrailingValue(golden), "行尾带符号整数 = " + (HasTrailingValue(golden) ? "有" : "无"));
        SameInt("⑫-H8 黄金串里 <u> 只包住标签词（只剩小计 ×2），不包数值",
            CountOf(golden, "<u>小计</u>") * 100 + CountOf(golden, "<u>Total:</u>"), 2 * 100 + 0);

        
        var mem20 = new InGameDetailFormat.DetailMergeMemory();
        object slot20 = new object();
        bool hitFirst;
        string round0 = mem20.Merge(slot20, gameTooltip, golden, out hitFirst);   
        string cursor = round0;
        int same = 0;
        for (int k = 0; k < 20; k++)
        {
            bool hit20;
            cursor = mem20.Merge(slot20, cursor, golden, out hit20);
            if (string.Equals(cursor, round0, StringComparison.Ordinal)) same++;
        }
        SameInt("⑫-I1 连续 20 轮合并逐字节相同（幂等仍成立）", same, 20);
        SameInt("⑫-I2 20 轮后整段零可见标记",
            InGameDetailFormat.ContainsVisibleMarker(cursor, out why) ? 1 : 0, 0);
        Same("⑫-I3 20 轮后游戏原文仍是逐字节前缀",
            cursor.Substring(0, gameTooltip.Length), gameTooltip);
        SameInt("⑫-I4 20 轮后标题仍只出现 1 次", CountOf(cursor, InGameDetailFormat.PreviewTitle), 1);

        
        FrameSuite();

        
        RiziaSuite();

        
        RiziaRealSuite();

        
        DiagSuite();

        
        TruncationSuite();

        Console.WriteLine("");
        Console.WriteLine("═══ 结果：" + (fails == 0 ? (total + "/" + total + " 全部通过 ✓") : (fails + " 项失败 ✗（共 " + total + " 项）")) + " ═══");
        Environment.Exit(fails == 0 ? 0 : 1);
    }

    
    static int BuildRowsOfKind(List<InGameDetailFormat.Entry> entries, int kind)
    {
        int n = 0;
        var rows = InGameDetailFormat.BuildRows(entries);
        for (int i = 0; i < rows.Count; i++) if (rows[i].Kind == kind) n++;
        return n;
    }
}
