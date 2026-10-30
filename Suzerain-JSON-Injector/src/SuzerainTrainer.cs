



using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

public class SuzerainTrainer
{
    static string saveDir;
    static string curFile;   
    static string json;      
    static bool hasBom;      
    static bool loaded;

    static string DefaultSaveDir()
    {
        string p = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        return Path.Combine(p, "AppData", "LocalLow", "Torpor Games", "Suzerain");
    }

    
    

    static string GetVar(string key)
    {
        Match m = Regex.Match(json, @"\[\\""" + Regex.Escape(key) + @"\\""\]=(-?\d+(?:\.\d+)?)");
        return m.Success ? m.Groups[1].Value : null;
    }

    static bool SetVar(string key, string value)
    {
        string pattern = @"\[\\""" + Regex.Escape(key) + @"\\""\]=-?\d+(?:\.\d+)?";
        string repl = "[\\\"" + key + "\\\"]=" + value;
        if (!Regex.IsMatch(json, pattern)) return false;
        json = Regex.Replace(json, pattern, repl);
        return true;
    }

    
    static List<string> ScanVars(string prefix)
    {
        List<string> list = new List<string>();
        Regex re = new Regex(@"\[\\""(" + Regex.Escape(prefix) + @"[A-Za-z0-9_\.]+)\\""\]=-?\d+(?:\.\d+)?");
        foreach (Match m in re.Matches(json))
            if (!list.Contains(m.Groups[1].Value)) list.Add(m.Groups[1].Value);
        return list;
    }

    
    static string GetBoolVar(string key)
    {
        Match m = Regex.Match(json, @"\[\\""" + Regex.Escape(key) + @"\\""\]=(true|false)");
        return m.Success ? m.Groups[1].Value : null;
    }

    static bool SetBoolVar(string key, string value)
    {
        string pattern = @"(\[\\""" + Regex.Escape(key) + @"\\""\]=)(true|false)";
        if (!Regex.IsMatch(json, pattern)) return false;
        json = Regex.Replace(json, pattern, "$1" + value);
        return true;
    }

    static List<string> ScanBoolVars(string prefix)
    {
        List<string> list = new List<string>();
        Regex re = new Regex(@"\[\\""(" + Regex.Escape(prefix) + @"[A-Za-z0-9_\.]+)\\""\]=(true|false)");
        foreach (Match m in re.Matches(json))
            if (!list.Contains(m.Groups[1].Value)) list.Add(m.Groups[1].Value);
        return list;
    }

    
    static string GetField(string entity, string field)
    {
        Match m = Regex.Match(json, @"""nameInDatabase"": """ + Regex.Escape(entity) + @""",\s*""fieldName"": """ + Regex.Escape(field) + @""",\s*""fieldValue"": ""([^""]*)");
        return m.Success ? m.Groups[1].Value : null;
    }

    static bool SetField(string entity, string field, string value)
    {
        string pattern = @"""nameInDatabase"": """ + Regex.Escape(entity) + @""",\s*""fieldName"": """ + Regex.Escape(field) + @""",\s*""fieldValue"": ""[^""]*""";
        string repl = "\"nameInDatabase\": \"" + entity + "\",\n            \"fieldName\": \"" + field + "\",\n            \"fieldValue\": \"" + value + "\"";
        if (!Regex.IsMatch(json, pattern)) return false;
        json = Regex.Replace(json, pattern, repl);
        return true;
    }

    static List<string> ScanEntities(string entityPrefix, string field)
    {
        List<string> list = new List<string>();
        Regex re = new Regex(@"""nameInDatabase"": """ + Regex.Escape(entityPrefix) + @"([A-Za-z0-9_\.]+)"",\s*""fieldName"": """ + Regex.Escape(field) + @"""");
        foreach (Match m in re.Matches(json))
            if (!list.Contains(m.Groups[1].Value)) list.Add(m.Groups[1].Value);
        return list;
    }


    static bool LoadFile(string path)
    {
        try
        {
            byte[] raw = File.ReadAllBytes(path);
            hasBom = raw.Length >= 3 && raw[0] == 0xEF && raw[1] == 0xBB && raw[2] == 0xBF;
            string text = Encoding.UTF8.GetString(raw, hasBom ? 3 : 0, raw.Length - (hasBom ? 3 : 0));
            if (text.Trim().Length == 0)
            { Console.WriteLine("读取失败: 文件为空（" + path + "）"); loaded = false; return false; }
            if (text.IndexOf("\"variables\"", StringComparison.Ordinal) < 0 &&
                text.IndexOf("\"currentStoryPack\"", StringComparison.Ordinal) < 0)
            { Console.WriteLine("读取失败: 不像 Suzerain 存档（既无 variables 也无 currentStoryPack 字段）"); loaded = false; return false; }
            json = text;
            curFile = path;
            loaded = true;
            return true;
        }
        catch (Exception e)
        {
            Console.WriteLine("读取失败: " + e.Message);
            loaded = false;
            return false;
        }
    }

    static bool SaveFile()
    {
        if (!loaded) { Console.WriteLine("尚未选择存档"); return false; }
        try
        {
            string bak = curFile + ".bak";
            File.Copy(curFile, bak, true);
            byte[] data = Encoding.UTF8.GetBytes(json);
            if (hasBom)
            {
                byte[] full = new byte[data.Length + 3];
                full[0] = 0xEF; full[1] = 0xBB; full[2] = 0xBF;
                Array.Copy(data, 0, full, 3, data.Length);
                data = full;
            }
            File.WriteAllBytes(curFile, data);
            Console.WriteLine("已保存: " + curFile);
            Console.WriteLine("备份: " + bak);
            return true;
        }
        catch (Exception e)
        {
            Console.WriteLine("保存失败: " + e.Message);
            return false;
        }
    }

    static void ListSaves()
    {
        if (!Directory.Exists(saveDir)) { Console.WriteLine("存档目录不存在: " + saveDir); return; }
        string[] files = Directory.GetFiles(saveDir, "*.json");
        Array.Sort(files, delegate(string a, string b) { return File.GetLastWriteTime(b).CompareTo(File.GetLastWriteTime(a)); });
        Console.WriteLine("存档目录: " + saveDir + "  (共 " + files.Length + " 个存档)");
        int n = Math.Min(files.Length, 30);
        for (int i = 0; i < n; i++)
        {
            FileInfo fi = new FileInfo(files[i]);
            Console.WriteLine("  [" + i + "] " + fi.Name + "  (" + (fi.Length / 1024) + " KB, " + fi.LastWriteTime.ToString("yyyy-MM-dd HH:mm") + ")");
        }
        if (files.Length == 0) return;
        Console.Write("输入编号选择存档 (直接回车=最近一个): ");
        string input = Console.ReadLine().Trim();
        int idx = 0;
        if (input.Length > 0 && !int.TryParse(input, out idx)) { Console.WriteLine("无效输入"); return; }
        if (idx < 0 || idx >= n) { Console.WriteLine("超出范围"); return; }
        if (LoadFile(files[idx])) Console.WriteLine("已载入: " + files[idx]);
    }


    static string ReadNewValue(string prompt, string cur)
    {
        Console.Write(prompt + " (当前=" + cur + ", 回车跳过): ");
        string s = Console.ReadLine().Trim();
        if (s.Length == 0 || s == "q" || s == "Q") return null;
        return s;
    }

    static void EditNumList(string title, List<string> keys, Func<string, string> getter)
    {
        Console.WriteLine("==== " + title + " ====");
        if (keys.Count == 0) { Console.WriteLine("(存档中未找到相关字段)"); return; }
        for (int i = 0; i < keys.Count; i++)
            Console.WriteLine("  [" + i + "] " + keys[i] + " = " + getter(keys[i]));
        while (true)
        {
            Console.Write("输入编号修改 (q 返回): ");
            string s = Console.ReadLine().Trim();
            if (s == "q" || s == "Q" || s.Length == 0) break;
            int idx;
            if (!int.TryParse(s, out idx) || idx < 0 || idx >= keys.Count) { Console.WriteLine("无效编号"); continue; }
            string key = keys[idx];
            string cur = getter(key);
            string nv = ReadNewValue("  " + key, cur);
            if (nv == null) continue;
            if (!SetVar(key, nv))
                Console.WriteLine("替换失败: 字段不存在或格式变化");
            else
            {
                Console.WriteLine("已修改 " + key + ": " + cur + " -> " + nv);
                Console.WriteLine("校验: " + getter(key));
            }
        }
    }

    static void EditFieldList(string title, string prefix, string field)
    {
        Console.WriteLine("==== " + title + " ====");
        List<string> entities = ScanEntities(prefix, field);
        if (entities.Count == 0) { Console.WriteLine("(存档中未找到相关字段)"); return; }
        for (int i = 0; i < entities.Count; i++)
            Console.WriteLine("  [" + i + "] " + prefix + entities[i] + "." + field + " = " + GetField(prefix + entities[i], field));
        while (true)
        {
            Console.Write("输入编号修改 (q 返回): ");
            string s = Console.ReadLine().Trim();
            if (s == "q" || s == "Q" || s.Length == 0) break;
            int idx;
            if (!int.TryParse(s, out idx) || idx < 0 || idx >= entities.Count) { Console.WriteLine("无效编号"); continue; }
            string entity = prefix + entities[idx];
            string cur = GetField(entity, field);
            string nv = ReadNewValue("  " + entity + "." + field, cur);
            if (nv == null) continue;
            if (!SetField(entity, field, nv)) Console.WriteLine("替换失败");
            else Console.WriteLine("已修改: " + cur + " -> " + nv + " (校验=" + GetField(entity, field) + ")");
        }
    }

    static void EditCoreResources()
    {
        List<string> keys = new List<string>();
        keys.Add("BaseGame.GovernmentBudget");   // 政府预算 HUD -20~30
        keys.Add("BaseGame.Economy");            // 经济 0-100
        keys.Add("BaseGame.Healthcare");         // 医疗
        keys.Add("BaseGame.Education");          // 教育
        keys.Add("BaseGame.PersonalWealth");     // 个人财富 0-15
        keys.Add("BaseGame.Public_Opinion");     // 公众舆论 0-100
        keys.Add("BaseGame.Country_Unrest");     // 国内动乱 0-10
        keys.Add("BaseGame.Bludish_Opinion");    // Bludish舆论 -100~100
        EditNumList("核心资源", keys, delegate(string k) { return GetVar(k) ?? "(不存在)"; });
    }

    static void EditPolitics()
    {
        List<string> keys = new List<string>();
        keys.Add("BaseGame.Malenyevist"); // 马列主义者 -100~100
        keys.Add("BaseGame.Sollist");     // 索尔主义者
        keys.Add("BaseGame.Democratic");  // 民主派
        keys.Add("BaseGame.Capitalist");  // 资本家
        EditNumList("政治光谱", keys, delegate(string k) { return GetVar(k) ?? "(不存在)"; });
    }

    static void EditRelations()
    {
        List<string> keys = ScanVars("BaseGame.Relations_");
        List<string> numeric = new List<string>();
        foreach (string k in keys)
        {
            string v = GetVar(k);
            if (v != null && v != "true" && v != "false") numeric.Add(k);
        }
        EditNumList("人物好感/关系 (BaseGame.Relations_*)", numeric, delegate(string k) { return GetVar(k); });
    }

    static void EditCountries()
    {
        EditFieldList("国家关系 (0-5, 值越大越友好)", "Sordland_Country_", "Relations");
        List<string> keys = new List<string>();
        keys.Add("BaseGame.AgnoliaTradeDeal_Negotiation");
        keys.Add("BaseGame.WehlenTradeDeal_Negotiation");
        EditNumList("贸易谈判点", keys, delegate(string k) { return GetVar(k) ?? "(不存在)"; });
    }

    static void EditDLCResources()
    {
        while (true)
        {
            Console.WriteLine("==== DLC 资源 (Rizia) ====");
            Console.WriteLine("  [1] 权威 Authority = " + (GetVar("RiziaDLC.Resources_Authority") ?? "?"));
            Console.WriteLine("  [2] 预算 Budget    = " + (GetVar("RiziaDLC.Resources_Budget") ?? "?"));
            Console.WriteLine("  [3] 能源 Energy    = " + (GetVar("RiziaDLC.Resources_Energy") ?? "?"));
            Console.WriteLine("  [4] 修正合计 (ModifiersTotal)");
            Console.WriteLine("  [5] 修正项浏览 (Authority_Modifier_*/Budget_Modifier_*/Energy_Modifier_*)");
            Console.WriteLine("  [6] HUD 上下限 (本体+DLC 共10项)");
            Console.WriteLine("  [q] 返回");
            Console.Write("选择: ");
            string s = Console.ReadLine().Trim();
            if (s == "q" || s == "Q") break;
            if (s == "1" || s == "2" || s == "3")
            {
                string key = s == "1" ? "RiziaDLC.Resources_Authority" :
                            s == "2" ? "RiziaDLC.Resources_Budget" : "RiziaDLC.Resources_Energy";
                string nv = ReadNewValue(key, GetVar(key));
                if (nv != null && SetVar(key, nv)) Console.WriteLine("已修改, 校验=" + GetVar(key));
            }
            else if (s == "4")
            {
                List<string> keys = new List<string>();
                keys.Add("RiziaDLC.Resources_AuthorityModifiersTotal");
                keys.Add("RiziaDLC.Resources_BudgetModifiersTotal");
                keys.Add("RiziaDLC.Resources_EnergyModifiersTotal");
                EditNumList("修正合计", keys, delegate(string k) { return GetVar(k) ?? "?"; });
            }
            else if (s == "5")
            {
                List<string> keys = ScanVars("RiziaDLC.Authority_Modifier_");
                List<string> bkeys = ScanVars("RiziaDLC.Budget_Modifier_");
                List<string> ekeys = ScanVars("RiziaDLC.Energy_Modifier_");
                keys.AddRange(bkeys); keys.AddRange(ekeys);
                keys.Sort();
                Console.WriteLine("权威/预算/能源修正项共 " + keys.Count + " 个 (正=增益 负=减益):");
                EditNumList("Modifier 修正项", keys, delegate(string k) { return GetVar(k); });
            }
            else if (s == "6") EditHUDLimits();
        }
    }

    static void EditHUDLimits()
    {
        List<string> keys = new List<string>();
        keys.Add("BaseGame.Sordland_HUDStat_GovernmentBudget_Min");
        keys.Add("BaseGame.Sordland_HUDStat_GovernmentBudget_Max");
        keys.Add("BaseGame.Sordland_HUDStat_PersonalWealth_Min");
        keys.Add("BaseGame.Sordland_HUDStat_PersonalWealth_Max");
        keys.Add("RiziaDLC.Rizia_HUDStat_Authority_Min");
        keys.Add("RiziaDLC.Rizia_HUDStat_Authority_Max");
        keys.Add("RiziaDLC.Rizia_HUDStat_Budget_Min");
        keys.Add("RiziaDLC.Rizia_HUDStat_Budget_Max");
        keys.Add("RiziaDLC.Rizia_HUDStat_Energy_Min");
        keys.Add("RiziaDLC.Rizia_HUDStat_Energy_Max");
        EditNumList("HUD 上下限", keys, delegate(string k) { return GetVar(k) ?? "?"; });
    }

    static List<string> curEvents = new List<string>();
    static string curEventPrefix = "GameCondition.";
    static int curEventFilter = 0; // 0=全部, 否则按回合

    static void LoadEvents()
    {
        curEvents = ScanBoolVars(curEventPrefix);
        if (curEventFilter > 0)
        {
            List<string> filtered = new List<string>();
            string turnTag = (curEventFilter < 10 ? "Turn0" : "Turn") + curEventFilter;
            foreach (string k in curEvents)
                if (k.Contains(turnTag)) filtered.Add(k);
            curEvents = filtered;
        }
        curEvents.Sort();
    }

    static void EditEvents()
    {
        while (true)
        {
            LoadEvents();
            Console.WriteLine("==== 事件链修改 ====");
            Console.WriteLine("  前缀: " + curEventPrefix + " (当前匹配 " + curEvents.Count + " 个事件)");
            Console.WriteLine("  回合过滤: " + (curEventFilter == 0 ? "全部" : "Turn" + curEventFilter.ToString("00")));
            Console.WriteLine("  [1] 切换前缀 (GameCondition / BaseGameIsolated / RiziaDLCIsolated / BaseGame / RiziaDLC)");
            Console.WriteLine("  [2] 回合过滤 (0=全部, 1-10=对应回合)");
            Console.WriteLine("  [3] 列出事件与当前值");
            Console.WriteLine("  [4] 全部置为 true (解锁)");
            Console.WriteLine("  [5] 全部置为 false (重置)");
            Console.WriteLine("  [6] 按编号切换单项");
            Console.WriteLine("  [q] 返回");
            Console.Write("选择: ");
            string s = Console.ReadLine().Trim();
            if (s == "q" || s == "Q") break;
            if (s == "1")
            {
                string[] prefs = new string[] { "GameCondition.", "BaseGameIsolated.", "RiziaDLCIsolated.", "BaseGame.", "RiziaDLC." };
                int cur = Array.IndexOf(prefs, curEventPrefix);
                curEventPrefix = prefs[(cur + 1) % prefs.Length];
                Console.WriteLine("前缀已切换: " + curEventPrefix);
            }
            else if (s == "2")
            {
                Console.Write("输入回合号 (0=全部, 1-10): ");
                string v = Console.ReadLine().Trim();
                int t;
                if (int.TryParse(v, out t) && t >= 0 && t <= 10) curEventFilter = t;
                else Console.WriteLine("无效输入");
            }
            else if (s == "3")
            {
                Console.WriteLine("==== " + curEventPrefix + " 事件列表 (共 " + curEvents.Count + ") ====");
                for (int i = 0; i < curEvents.Count; i++)
                    Console.WriteLine("  [" + i + "] " + curEvents[i] + " = " + GetBoolVar(curEvents[i]));
            }
            else if (s == "4" || s == "5")
            {
                string val = s == "4" ? "true" : "false";
                Console.Write("确认将 " + curEvents.Count + " 个事件全部置为 " + val + "? (y/n): ");
                string c = Console.ReadLine().Trim();
                if (c == "y" || c == "Y")
                {
                    int ok = 0;
                    foreach (string k in curEvents)
                        if (SetBoolVar(k, val)) ok++;
                    Console.WriteLine("已置 " + val + ": " + ok + " 个");
                }
            }
            else if (s == "6")
            {
                Console.Write("输入编号 (回车返回): ");
                string v = Console.ReadLine().Trim();
                int idx;
                if (v.Length > 0 && int.TryParse(v, out idx) && idx >= 0 && idx < curEvents.Count)
                {
                    string k = curEvents[idx];
                    string cur = GetBoolVar(k);
                    string nv = cur == "true" ? "false" : "true";
                    if (SetBoolVar(k, nv)) Console.WriteLine(k + ": " + cur + " -> " + nv);
                }
            }
        }
    }

    static void EditMisc()
    {
        while (true)
        {
            Console.WriteLine("==== 其它 ====");
            Console.WriteLine("  [1] 回合数 BaseGameSetup.CurrentTurn = " + (GetVar("BaseGameSetup.CurrentTurn") ?? "?"));
            Console.WriteLine("  [2] 议会改革票 = " + (GetVar("BaseGame.Reform_Assembly_Vote") ?? "?"));
            Console.WriteLine("  [3] 法院改革票 = " + (GetVar("BaseGame.Reform_Court_Vote") ?? "?"));
            Console.WriteLine("  [4] 项目进度 (Progress_* 百分比)");
            Console.WriteLine("  [5] DLC 人物好感 (RiziaDLC.Relations_*)");
            Console.WriteLine("  [6] 自定义变量 (输入任意 variables 键名)");
            Console.WriteLine("  [q] 返回");
            Console.Write("选择: ");
            string s = Console.ReadLine().Trim();
            if (s == "q" || s == "Q") break;
            if (s == "1")
            {
                string nv = ReadNewValue("回合数", GetVar("BaseGameSetup.CurrentTurn"));
                if (nv != null && SetVar("BaseGameSetup.CurrentTurn", nv)) Console.WriteLine("已修改, 校验=" + GetVar("BaseGameSetup.CurrentTurn"));
            }
            else if (s == "2" || s == "3")
            {
                string key = s == "2" ? "BaseGame.Reform_Assembly_Vote" : "BaseGame.Reform_Court_Vote";
                string nv = ReadNewValue(key, GetVar(key));
                if (nv != null && SetVar(key, nv)) Console.WriteLine("已修改, 校验=" + GetVar(key));
            }
            else if (s == "4")
            {
                EditFieldList("项目进度百分比", "Progress_", "ProgressPercentage");
            }
            else if (s == "5")
            {
                List<string> keys = ScanVars("RiziaDLC.Relations_");
                List<string> numeric = new List<string>();
                foreach (string k in keys)
                {
                    string v = GetVar(k);
                    if (v != null && v != "true" && v != "false") numeric.Add(k);
                }
                EditNumList("DLC 人物好感", numeric, delegate(string k) { return GetVar(k); });
            }
            else if (s == "6")
            {
                Console.Write("变量键名 (如 BaseGame.Economy): ");
                string key = Console.ReadLine().Trim();
                if (key.Length == 0) continue;
                string cur = GetVar(key);
                Console.WriteLine("当前值: " + (cur ?? "(不存在)"));
                Console.Write("新值: ");
                string nv = Console.ReadLine().Trim();
                if (nv.Length == 0) continue;
                if (SetVar(key, nv)) Console.WriteLine("已修改 " + key + " = " + nv);
                else Console.WriteLine("该键不存在于 variables, 未修改");
            }
        }
    }

    static void Main(string[] args)
    {
        if (args.Length > 0 && args[0] == "--version")
        {
            Console.WriteLine("SuzerainTrainer " + BuildInfo.Version);
            return;
        }

        Console.WriteLine("========================================");
        Console.WriteLine("  Suzerain 存档修改器 " + BuildInfo.Version);
        Console.WriteLine("  (政府预算/经济/好感/国家关系/光谱等)");
        Console.WriteLine("========================================");
        saveDir = DefaultSaveDir();
        if (args.Length > 0 && File.Exists(args[0]))
        {
            if (LoadFile(args[0])) Console.WriteLine("已载入: " + args[0]);
        }
        else if (args.Length > 0 && Directory.Exists(args[0]))
        {
            saveDir = args[0];
            ListSaves();
        }
        while (true)
        {
            Console.WriteLine("");
            Console.WriteLine("==== 主菜单 ====");
            Console.WriteLine("  [1] 选择存档文件   (当前: " + (loaded ? Path.GetFileName(curFile) : "未选择") + ")");
            Console.WriteLine("  [2] 核心资源 (预算/经济/医疗/教育/财富/舆论)");
            Console.WriteLine("  [3] 政治光谱 (马列/索尔/民主/资本)");
            Console.WriteLine("  [4] 人物好感 (Relations_*)");
            Console.WriteLine("  [5] 国家关系与谈判");
            Console.WriteLine("  [6] DLC 资源与 HUD 上下限 (权威/预算/能源)");
            Console.WriteLine("  [7] 事件链修改 (剧情事件解锁/重置)");
            Console.WriteLine("  [8] 其它 (回合/投票/进度/DLC好感/自定义)");
            Console.WriteLine("  [9] 保存修改 (自动备份 .bak)");
            Console.WriteLine("  [10] 重新载入 (撤销未保存修改)");
            Console.WriteLine("  [0] 退出");
            Console.Write("选择: ");
            string s = Console.ReadLine().Trim();
            if (s == "0") break;
            if (s == "1") ListSaves();
            else if (s == "2") EditCoreResources();
            else if (s == "3") EditPolitics();
            else if (s == "4") EditRelations();
            else if (s == "5") EditCountries();
            else if (s == "6") EditDLCResources();
            else if (s == "7") EditEvents();
            else if (s == "8") EditMisc();
            else if (s == "9") SaveFile();
            else if (s == "10") { if (loaded && LoadFile(curFile)) Console.WriteLine("已重新载入"); }
            else Console.WriteLine("无效选择");
        }
    }
}
