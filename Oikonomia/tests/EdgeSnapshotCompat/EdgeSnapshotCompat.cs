


using System;
using System.Text.RegularExpressions;

class EdgeSnapshotCompat
{
    
    static int ParseInt(string raw, string key, int dflt)
    {
        var m = Regex.Match(raw, "\"" + key + "\"\\s*:\\s*(-?\\d+)");
        return m.Success && int.TryParse(m.Groups[1].Value, out var v) ? v : dflt;
    }
    static bool[] ParseBools(string raw, string key, int count)
    {
        var m = Regex.Match(raw, "\"" + key + "\"\\s*:\\s*\\[([^\\]]*)\\]");
        if (!m.Success) return null;
        var parts = m.Groups[1].Value.Split(',');
        var arr = new bool[count];
        for (int i = 0; i < count; i++)
            arr[i] = i < parts.Length && parts[i].Trim() == "true";
        return arr;
    }

    const int EdgeLayoutVersion = 2;
    const int EdgeCount = 21;
    static int _edgeLoaded;
    static bool _edgeShort;
    static bool[] PrevEdge = new bool[EdgeCount];

    
    static void LoadEdges(string raw)
    {
        PrevEdge = new bool[EdgeCount];
        int edgeVer = ParseInt(raw, "edgeVer", -1);
        var edges = ParseBools(raw, "edges", PrevEdge.Length);
        bool legacyGrowth = false;
        if (edges == null) { edges = ParseBools(raw, "growth", PrevEdge.Length); legacyGrowth = true; }
        if (edges != null && edgeVer == EdgeLayoutVersion)
        {
            for (int i = 0; i < PrevEdge.Length; i++) PrevEdge[i] = edges[i];
            _edgeLoaded = PrevEdge.Length;
            _edgeShort = false;
        }
        else if (edges != null && legacyGrowth)
        {
            for (int i = 0; i < 7 && i < PrevEdge.Length; i++) PrevEdge[i] = edges[i];
            _edgeLoaded = 7;
            _edgeShort = true;
        }
        else
        {
            _edgeLoaded = 0;
            _edgeShort = true;
        }
    }

    static bool Skipped(int i) { return _edgeShort && _edgeLoaded >= 0 && i >= _edgeLoaded; }

    static int fails = 0;
    static void Check(string what, bool ok, string detail)
    {
        Console.WriteLine((ok ? "  PASS  " : "  FAIL  ") + what.PadRight(22) + detail);
        if (!ok) fails++;
    }

    static void Case(string title, string json, bool expectShort, int expectLoaded, int expectPrefixTrue)
    {
        Console.WriteLine("\n【" + title + "】");
        LoadEdges(json);
        Check("_edgeShort", _edgeShort == expectShort, "_edgeShort=" + _edgeShort + "（期望 " + expectShort + "）");
        Check("_edgeLoaded", _edgeLoaded == expectLoaded, "_edgeLoaded=" + _edgeLoaded + "（期望 " + expectLoaded + "）");
        int prefixTrue = 0; for (int i = 0; i < 7; i++) if (PrevEdge[i]) prefixTrue++;
        Check("增长组前缀继承", prefixTrue == expectPrefixTrue, "前 7 项为真 " + prefixTrue + " 个（期望 " + expectPrefixTrue + "）");
        
        bool allSkipped = true;
        for (int i = 7; i < EdgeCount; i++) if (!Skipped(i)) allSkipped = false;
        bool noTradeTrue = true;
        for (int i = 7; i < EdgeCount; i++) if (PrevEdge[i]) noTradeTrue = false;
        Check("贸易协议不结算", allSkipped || noTradeTrue, allSkipped ? "全部跳过 ✓" : (noTradeTrue ? "基线全假 ✓" : "★会误发★"));
    }

    static string Falses(int n) { var a = new string[n]; for (int i = 0; i < n; i++) a[i] = "false"; return string.Join(",", a); }

    static void Main()
    {
        Console.WriteLine("═══ 快照兼容性验证（EdgeLayoutVersion=" + EdgeLayoutVersion + "，词条数=" + EdgeCount + "）═══");

        Case("路径1 用户当前旧快照 {growth:[8], 无 edgeVer}",
            "{\"turn\":10,\"campaignHash\":683983305,\"tiers\":[1,2,2,2],\"taxAvoid\":false,\"growth\":[true,false,true,true,false,true,false,false]}",
            true, 7, 4);

        Case("路径2 2.3.0 快照 {edges:[22], 无 edgeVer}",
            "{\"turn\":10,\"edges\":[" + Falses(22) + "]}",
            true, 0, 0);

        Case("路径3 新格式 {edges:[21], edgeVer:2}",
            "{\"turn\":10,\"edgeVer\":2,\"edges\":[true,false,true,true,false,true,false," + Falses(14) + "]}",
            false, 21, 4);

        Case("路径4 版本过期 {edges:[21], edgeVer:1}（未来又改词条表）",
            "{\"turn\":10,\"edgeVer\":1,\"edges\":[true,true,true,true,true,true,true," + Falses(14) + "]}",
            true, 0, 0);

        Console.WriteLine("\n═══ 结果：" + (fails == 0 ? "全部通过 ✓" : fails + " 项失败 ✗") + " ═══");
        Environment.Exit(fails == 0 ? 0 : 1);
    }
}
