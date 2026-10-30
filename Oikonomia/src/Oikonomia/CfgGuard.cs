using BepInEx.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace Oikonomia
{
    
    
    
    
    
    
    
    
    
    
    internal static class CfgGuard
    {
        private static ManualLogSource _log;
        private static int _inferLogged;

        internal static void Init(ManualLogSource log) { _log = log; }

        private static string IntentPath()
        {
            try { return Path.Combine(BepInEx.Paths.ConfigPath, "oikonomia.intent.txt"); }
            catch { return null; }
        }

        

        
        internal static bool FlagBool(string name)
        {
            try
            {
                var t0 = FindType("Lua");
                if (t0 != null)
                {
                    var m = t0.GetMethod("IsTrue", BindingFlags.Public | BindingFlags.Static, null, new[] { typeof(string) }, null);
                    if (m != null) return Truthy(m.Invoke(null, new object[] { name }));
                }
            }
            catch { }
            return false;
        }

        
        internal static string FlagStr(string name)
        {
            try
            {
                var t0 = FindType("DialogueLua");
                if (t0 == null) return "type-null ✗";
                var m = t0.GetMethod("GetVariable", BindingFlags.Public | BindingFlags.Static, null, new[] { typeof(string) }, null);
                if (m == null) return "method-null ✗";
                var o = m.Invoke(null, new object[] { name });
                if (o == null) return "null ✗";
                string tn = o.GetType().FullName ?? o.GetType().Name;
                if (tn.IndexOf("Result", StringComparison.Ordinal) >= 0)
                    return "Result(" + o + " -> " + (Truthy(o) ? "true" : "false") + ")";
                return tn + "(" + o + ")";
            }
            catch (Exception e) { return "err:" + e.GetType().Name + " ✗"; }
        }

        
        internal static bool Truthy(object o)
        {
            try
            {
                if (o == null) return false;
                if (o is bool b) return b;
                var t = o.GetType();
                foreach (var pn in new[] { "asBool", "AsBool", "Result", "boolVal", "BoolVal", "Value" })
                {
                    try
                    {
                        var p = t.GetProperty(pn, BindingFlags.Public | BindingFlags.Instance);
                        if (p == null || p.GetIndexParameters().Length != 0) continue;
                        var v = p.GetValue(o);
                        if (v is bool pb) return pb;
                        if (v != null) { string s0 = v.ToString(); if (Eq(s0)) return true; if (s0 == "false") return false; }
                    }
                    catch { }
                }
                string s = o.ToString();
                if (Eq(s)) return true;
                if (s == "false") return false;
            }
            catch { }
            return false;
        }

        private static bool Eq(string s) { return s == "true" || s == "True"; }

        private static Type FindType(string simple)
        {
            foreach (var asmName in new[] { "Assembly-CSharp-firstpass", "Assembly-CSharp" })
            {
                try
                {
                    var asm = Assembly.Load(asmName);
                    if (asm == null) continue;
                    foreach (var t in asm.GetTypes()) if (t != null && t.Name == simple) return t;
                }
                catch { }
            }
            try
            {
                foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
                {
                    try { foreach (var t in asm.GetTypes()) if (t != null && t.Name == simple) return t; } catch { }
                }
            }
            catch { }
            return null;
        }

        

        internal static void LogLoadInference()
        {
            try
            {
                if (_log == null || _inferLogged > 10) return;
                _inferLogged++;
                bool hw = FlagBool("BaseGame.Turn01_InT_Investment_Highway");
                bool rw = FlagBool("BaseGame.Turn01_InT_Investment_Railway");
                string verdict = (hw && rw) ? "歧义 ✗（★ 两个项目都在跑 ⇒ 标未知 ＋ 安全降级 ✓）"
                    : (hw ? "确定 ✓（★ 在跑项目 = H-3 ✓）" : (rw ? "确定 ✓（★ 在跑项目 = L-1 ✓）" : "都不在跑 ⇒ 无归属 ✓"));
                _log.LogInfo("[OI][company-map] 读档场景 ⇒ Highway=" + (hw ? "true" : "false") + " / Railway=" + (rw ? "true" : "false")
                    + "（★ 原始读值 H=" + FlagStr("BaseGame.Turn01_InT_Investment_Highway")
                    + " R=" + FlagStr("BaseGame.Turn01_InT_Investment_Railway") + " ✓）⇒ 归属=" + verdict);
            }
            catch { }
        }

        

        internal static void LogDecisionPanelApi()
        {
            try
            {
                if (_log == null) return;
                var names = new List<string>();
                foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
                {
                    try
                    {
                        foreach (var t in asm.GetTypes())
                        {
                            if (t == null || t.Name == null) continue;
                            string n = t.Name;
                            if (n.IndexOf("Decision", StringComparison.Ordinal) < 0) continue;
                            var members = new List<string>();
                            try
                            {
                                foreach (var m in t.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static))
                                {
                                    string mn = m.Name;
                                    if (mn.IndexOf("Decision", StringComparison.Ordinal) >= 0 || mn.IndexOf("Show", StringComparison.Ordinal) >= 0
                                        || mn.IndexOf("Option", StringComparison.Ordinal) >= 0 || mn.IndexOf("Choose", StringComparison.Ordinal) >= 0
                                        || mn.IndexOf("Click", StringComparison.Ordinal) >= 0 || mn.IndexOf("IsShowing", StringComparison.Ordinal) >= 0)
                                        members.Add(mn);
                                }
                                foreach (var p in t.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static))
                                {
                                    string pn = p.Name;
                                    if (pn.IndexOf("IsShowing", StringComparison.Ordinal) >= 0 || pn.IndexOf("Decision", StringComparison.Ordinal) >= 0
                                        || pn.IndexOf("Option", StringComparison.Ordinal) >= 0) members.Add(pn + "(p)");
                                }
                            }
                            catch { }
                            if (members.Count > 0) names.Add(n + " => " + string.Join(",", members.ToArray()));
                            if (names.Count >= 24) break;
                        }
                    }
                    catch { }
                    if (names.Count >= 24) break;
                }
                _log.LogDebug("[OI][decision-api] 候选类型 " + names.Count + " 个：");
                foreach (var s in names) _log.LogDebug("[OI][decision-api]   - " + s);
            }
            catch { }
        }

        

        internal static void CheckCfgIntent()
        {
            try
            {
                string file = IntentPath();
                if (file == null) return;
                var now = new Dictionary<string, string>(StringComparer.Ordinal);
                now["[T1] Enabled"] = EconomyConfig.T1BothOn ? "true" : "false";
                now["[Company] Enabled"] = EconomyPatches.CompanyOnPublic ? "true" : "false";
                now["[T3] Enabled"] = (EconomyConfig.T3Enabled != null && EconomyConfig.T3Enabled.Value) ? "true" : "false";
                now["[V2] Enabled"] = EconomyPatches.V2OnPublic ? "true" : "false";
                now["[Diag] ScreenDump"] = (EconomyConfig.ScreenDump != null && EconomyConfig.ScreenDump.Value) ? "true" : "false";
                now["[Bench] Enabled"] = (EconomyConfig.BenchEnabled != null && EconomyConfig.BenchEnabled.Value) ? "true" : "false";

                if (File.Exists(file))
                {
                    foreach (var ln in File.ReadAllLines(file))
                    {
                        int eq = ln.IndexOf('=');
                        if (eq <= 0) continue;
                        string k = ln.Substring(0, eq).Trim();
                        string v = ln.Substring(eq + 1).Trim();
                        if (v == "true" && now.ContainsKey(k) && now[k] == "false" && _log != null)
                            _log.LogWarning("[OI][cfg] ★ 警告：" + k + " 上一局我记的是 true，现在是 false ✗"
                                + "（★ 请核对【节名 ＋ 键 ＋ 值】三者 ✓ —— ★ 键若落在别的节里 ⇒ 它定义的是那一节的键 ✗ 不是这个 ✓）");
                    }
                }

                var sb = new System.Text.StringBuilder();
                foreach (var kv in now) sb.Append(kv.Key).Append('=').Append(kv.Value).Append('\n');
                File.WriteAllText(file, sb.ToString(), new System.Text.UTF8Encoding(false));
            }
            catch { }
        }
    }
}
