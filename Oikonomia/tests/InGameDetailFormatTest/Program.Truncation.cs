























using System;
using System.Collections.Generic;
using Oikonomia;

partial class Program
{
    static void TruncationSuite()
    {
        Console.WriteLine("");
        Console.WriteLine("【⑰ N1 段一 上限截断自述：NoteTruncation / DescribeTruncations（★ 阴性对照可离线断言）】");

        
        InGameDetailFormat.ClearTruncations();
        SameInt("⑰-0a 起点清空后 TruncationCount = 0", InGameDetailFormat.TruncationCount, 0);
        Same("⑰-0b 无任何记录 ⇒ DescribeTruncations() 返回空串（不刷屏）",
            InGameDetailFormat.DescribeTruncations(), "");

        
        InGameDetailFormat.NoteTruncation("页行", 500, 500, 1, "硬上限",
            "IsRadioProbe.cs 的 ProbeLineCap（=500）");
        string s1 = InGameDetailFormat.DescribeTruncations();
        Has("⑰-1a 含约定串「[OI][上限] 上限名=」", s1, "[OI][上限] 上限名=");
        Has("⑰-1b 列·上限名 = 页行", s1, "上限名=页行");
        Has("⑰-1c 列·上限值 = 500", s1, "上限值=500");
        Has("⑰-1d 列·实际写入数 = 500", s1, "实际写入数=500");
        Has("⑰-1e 列·丢弃数 = 1", s1, "丢弃数=1");
        Has("⑰-1f 列·丢弃原因 = 硬上限", s1, "丢弃原因=硬上限");
        Has("⑰-1g 列·时点（ISO 形 yyyy-MM-ddTHH:mm:ss）", s1, "时点=");
        Has("⑰-1h 可选列·调整处（C-3③ 报警后有出路）", s1, "调整处=");
        Has("⑰-1i 调整处给出上限在哪调", s1, "ProbeLineCap");

        
        var rec = InGameDetailFormat.GetTruncation("页行");
        True("⑰-1j GetTruncation(\"页行\") 取得到记录", rec != null, "");
        True("⑰-1k 记录里的 ObservedAt 非空且形如 ISO（长度 >= 19，含 'T'）",
            rec != null && !string.IsNullOrEmpty(rec.ObservedAt)
                && rec.ObservedAt.Length >= 19
                && rec.ObservedAt.IndexOf('T') > 0,
            rec == null ? "记录为空" : ("ObservedAt=" + rec.ObservedAt));

        
        SameInt("⑰-2a 越限 1 种上限 ⇒ TruncationCount = 1（分母＝上限种数）",
            InGameDetailFormat.TruncationCount, 1);
        InGameDetailFormat.NoteTruncation("页钩", 200, 200, 1, "硬上限",
            "IsRadioProbe.cs 的 ProbePageHookCap（=200）");
        SameInt("⑰-2b 再加一种上限 ⇒ TruncationCount = 2", InGameDetailFormat.TruncationCount, 2);

        
        InGameDetailFormat.NoteTruncation("页行", 500, 500, 7, "硬上限",
            "IsRadioProbe.cs 的 ProbeLineCap（=500）");
        SameInt("⑰-3a 同 capName 二次调用 ⇒ TruncationCount 仍为 2（★ 更新而非新增）",
            InGameDetailFormat.TruncationCount, 2);
        var rec2 = InGameDetailFormat.GetTruncation("页行");
        SameInt("⑰-3b 该记录的 Dropped 已更新为 7（★ 不是冻结在首次的 1）",
            rec2 == null ? -1 : rec2.Dropped, 7);
        string s2 = InGameDetailFormat.DescribeTruncations();
        Has("⑰-3c 明细里报的是当前真实丢弃数 7", s2, "丢弃数=7");
        
        
        
        int iSeg = s2.IndexOf("上限名=页行", StringComparison.Ordinal);
        int iNext = iSeg < 0 ? -1 : s2.IndexOf("上限名=", iSeg + 1, StringComparison.Ordinal);
        string segPage = (iSeg >= 0) ? (iNext > iSeg ? s2.Substring(iSeg, iNext - iSeg) : s2.Substring(iSeg)) : "";
        Has("⑰-3d-0 已切出「页行」那一段（前置）", segPage, "上限名=页行");
        Has("⑰-3d 该段内报的是新值 7", segPage, "丢弃数=7");
        NotHas("⑰-3d-2 该段内不再残留旧值 1（防记录说谎；★ 只在本段内判，避免页钩那条的合法 1 造成假红）",
            segPage, "丢弃数=1 ");
        SameInt("⑰-3e 「上限名=页行」在整段里恰好出现 1 次（不新增行）",
            CountOf(s2, "上限名=页行"), 1);

        
        string a = InGameDetailFormat.DescribeTruncations();
        string b = InGameDetailFormat.DescribeTruncations();
        True("⑰-4a 连续两次调用逐字节相同（顺序稳定 ⇒ 可复现）",
            string.Equals(a, b, StringComparison.Ordinal), "");
        int iPage = a.IndexOf("上限名=页行", StringComparison.Ordinal);
        int iHook = a.IndexOf("上限名=页钩", StringComparison.Ordinal);
        True("⑰-4b 两种上限都上屏（分母 2 全在）", iPage >= 0 && iHook >= 0,
            "页行@" + iPage + " 页钩@" + iHook);
        True("⑰-4c ordinal 序：'页行' 在 '页钩' 之前（行 vs 钩 的序数关系）",
            iPage >= 0 && iHook >= 0 && iPage < iHook, "");

        
        
        
        InGameDetailFormat.ClearTruncations();
        InGameDetailFormat.NoteTruncation("未越限", 500, 3, 0, "硬上限", "x");
        SameInt("⑰-5a dropped=0 ⇒ 不入表（TruncationCount 仍为 0）",
            InGameDetailFormat.TruncationCount, 0);
        Same("⑰-5b dropped=0 ⇒ DescribeTruncations() 仍返回空串（不污染常态悬停）",
            InGameDetailFormat.DescribeTruncations(), "");
        True("⑰-5c 阴性不是恒真：同参数只把 dropped 改成 1 ⇒ 立刻有记录（证明 5a 判得出差别）",
            MakeOneAndHasLine(), "");
        InGameDetailFormat.ClearTruncations();

        
        InGameDetailFormat.NoteTruncation(null, 1, 1, 1, "r", "f");
        InGameDetailFormat.NoteTruncation("", 1, 1, 1, "r", "f");
        SameInt("⑰-6 空 / null 上限名 ⇒ 不入表（TruncationCount 仍为 0）",
            InGameDetailFormat.TruncationCount, 0);

        
        
        
        
        
        
        
        
        True("⑰-7a 恒等式①（修复后两项式）：512 == 502 + 10",
            Id2(512, 502, 10), "");
        True("⑰-7b 反例：旧① 形态 512 == 500 + 10 ⇒ 必须【不】成立（防退回修复前写法）",
            !Id2(512, 500, 10), "");
        True("⑰-7c 恒等式②：502 == 500 + 2（真行触发 == 上行 + 因上限丢弃）",
            Id2(502, 500, 2), "");
        True("⑰-7c-反例：502 == 500 + 1 ⇒ 必须【不】成立（丢弃数少记 1 就应判红）",
            !Id2(502, 500, 1), "");
        True("⑰-7d 恒等式③（三项式）：512 == 500 + 2 + 10",
            Id3(512, 500, 2, 10), "");
        True("⑰-7d-反例：把去重丢弃漏掉 ⇒ 512 == 500 + 2 ⇒ 必须【不】成立（RF-iii 的三项式要求）",
            !Id2(512, 500, 2), "");
        True("⑰-7e 恒等式④（全局三项式）：100 == 60 + 20 + 20",
            Id3(100, 60, 20, 20), "");
        True("⑰-7e-反例：把 _cpDropDup 当'单列'剔除 ⇒ 100 == 60 + 20 ⇒ 必须【不】成立（RF-ii）",
            !Id2(100, 60, 20), "");

        
        
        
        
        
        SourceStructureAnchors();
    }

    
    static bool Id2(long total, long a, long b) { return total == a + b; }

    
    static bool Id3(long total, long a, long b, long c) { return total == a + b + c; }

    
    
    
    
    static void SourceStructureAnchors()
    {
        string root = FindRepoSrcDir();
        if (root == null)
        {
            True("⑰-8-0 找得到 src\\Oikonomia（结构锚前置）", false, "未找到 src\\Oikonomia");
            return;
        }
        string isRadio = ReadAllTextSafe(System.IO.Path.Combine(root, "IsRadioProbe.cs"));
        string comp = ReadAllTextSafe(System.IO.Path.Combine(root, "CompanyProbe.cs"));
        True("⑰-8-0 两个真实源文件都读到（结构锚前置）",
            !string.IsNullOrEmpty(isRadio) && !string.IsNullOrEmpty(comp), "dir=" + root);

        
        
        NotHas("⑰-8a ★ 入口闸已删除（源码里不得再出现 'if (_probeLines >= ProbeLineCap) return;'）",
            isRadio, "if (_probeLines >= ProbeLineCap) return;");

        
        
        int iReal = isRadio.LastIndexOf("_probeRealTriggers++;", StringComparison.Ordinal);
        int iCap = isRadio.IndexOf("if (_probeLines >= ProbeLineCap)", StringComparison.Ordinal);
        True("⑰-8b ★ _probeRealTriggers++ 出现在上限判定之前（先计数、后判超）",
            iReal >= 0 && iCap >= 0 && iReal < iCap,
            "realTriggers@" + iReal + " capCheck@" + iCap);

        
        
        int iLinesInc = isRadio.LastIndexOf("_probeLines++;", StringComparison.Ordinal);
        True("⑰-8c ★ _probeLines++ 出现在上限判定之后（后自增 ⇒ 语义＝实际上行条数）",
            iCap >= 0 && iLinesInc > iCap, "capCheck@" + iCap + " linesInc@" + iLinesInc);

        
        int iCapBlockEnd = isRadio.IndexOf("NoteTruncationForDisplay(", iCap, StringComparison.Ordinal);
        int iRet = iCapBlockEnd < 0 ? -1 : isRadio.IndexOf("return;", iCapBlockEnd, StringComparison.Ordinal);
        True("⑰-8d ★ 上限分支内先送可见面（NoteTruncationForDisplay）、其后紧跟 return（I-4）",
            iCapBlockEnd > 0 && iRet > iCapBlockEnd,
            "note@" + iCapBlockEnd + " return@" + iRet);

        
        int iCapBlockStart = iCap;
        string capBlock = (iCapBlockStart >= 0 && iRet > iCapBlockStart)
            ? isRadio.Substring(iCapBlockStart, iRet - iCapBlockStart) : "";
        Has("⑰-8e ★ 上限分支里确实调了 NoteTruncationForDisplay（通道＝游戏内悬停明细，不是只写日志）",
            capBlock, "NoteTruncationForDisplay(");
        Has("⑰-8e2 且传的是「页行」上限名（与 ProbeLineCap 对应）", capBlock, "\"页行\"");

        
        Has("⑰-8f 计数器 _probeRealTriggers 已声明", isRadio, "_probeRealTriggers");
        Has("⑰-8g 计数器 _probeLinesCapDropped 已声明", isRadio, "_probeLinesCapDropped");
        Has("⑰-8h 计数器 _probePageDedupDropped 已声明（CAP B 去重丢弃）", isRadio, "_probePageDedupDropped");

        
        Has("⑰-8i CAP B 去重丢弃已计数（_probePageSeen.Add 失败即 ++）",
            isRadio, "_probePageSeen.Add(key)) { _probePageDedupDropped++; return; }");

        
        
        
        NotHas("⑰-8j 旧输出串『只汇总、不丢计数』已从 ProbeSum 的代码里移除（注释引用不算）",
            isRadio, "dropped ? \" ⚠ 真行已超上限 ⇒ **只汇总、不丢计数**");
        Has("⑰-8k ProbeSum 报出三项式三值（真行触发／真行上行／真行因上限丢弃）",
            isRadio, "真行触发=");
        Has("⑰-8l ProbeSum 报出「真行因上限丢弃」列", isRadio, "真行因上限丢弃=");
        Has("⑰-8m B2 汇总报出去重丢弃列", isRadio, "因去重丢弃=");

        
        Has("⑰-8n NoteOnce 配额丢弃已接可见面", comp, "\"census 配额\"");
        Has("⑰-8o NoteOnce 去重丢弃已接可见面", comp, "\"census 去重\"");
    }

    static string FindRepoSrcDir()
    {
        try
        {
            var d = new System.IO.DirectoryInfo(AppContext.BaseDirectory);
            for (int i = 0; i < 12 && d != null; i++)
            {
                string cand = System.IO.Path.Combine(d.FullName, "src", "Oikonomia");
                if (System.IO.Directory.Exists(cand)) return cand;
                d = d.Parent;
            }
        }
        catch { }
        return null;
    }

    static string ReadAllTextSafe(string p)
    {
        try { return System.IO.File.Exists(p) ? System.IO.File.ReadAllText(p) : null; }
        catch { return null; }
    }

    
    static bool MakeOneAndHasLine()
    {
        InGameDetailFormat.NoteTruncation("未越限", 500, 3, 1, "硬上限", "x");
        string s = InGameDetailFormat.DescribeTruncations();
        return s.Length > 0 && s.IndexOf("上限名=未越限", StringComparison.Ordinal) >= 0;
    }
}
