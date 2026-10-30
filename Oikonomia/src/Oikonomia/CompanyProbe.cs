













using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;

namespace Oikonomia
{
    internal static partial class EconomyPatches
    {
        
        private static readonly HashSet<string> _cpSeen = new HashSet<string>(StringComparer.Ordinal);
        private static int _cpHits;

        
        private static readonly string[] CompanyProbeTokens = new string[]
        {
            "Turn02_InT_Corporation_SordishState",
            "Turn02_InT_Corporation_TaurusHolding",
            "Turn02_InT_Corporation_UnderhallConstruction",
            "Journal_Turn02_ConstructionSSC",
            "Journal_Turn02_ConstructionTaurus",
            "Journal_Turn02_ConstructionUnderhall",
        };

        private static bool TokenHit(string s)
        {
            if (string.IsNullOrEmpty(s)) return false;
            for (int i = 0; i < CompanyProbeTokens.Length; i++)
                if (s.IndexOf(CompanyProbeTokens[i], StringComparison.Ordinal) >= 0) return true;
            return false;
        }

        
        
        
        
        private static readonly Dictionary<string, int> _cpHitsByKind = new Dictionary<string, int>(StringComparer.Ordinal);
        private static long _cpNoteCalls;
        private static long _cpNotePrinted;
        private static long _cpDropQuota;
        private static long _cpDropDup;
        
        
        
        private static long _cpDropDupNoted;
        private static long _cpDistinctConds;

        private static void NoteOnce(string kind, string key, string detail)
        {
            _cpNoteCalls++;
            try
            {
                if (kind == null) kind = "?";
                int used; _cpHitsByKind.TryGetValue(kind, out used);
                if (used >= 60)
                {
                    _cpDropQuota++;
                    if (_cpDropQuota == 1 || _cpDropQuota % 50 == 0)
                        _log.LogInfo("[OI][note-drop] 配额用尽 => 丢弃累计 " + _cpDropQuota + " 条（kind=" + kind + "）");
                    
                    
                    if (_cpDropQuota == 1 || _cpDropQuota % 100 == 0)
                        EconomyPatches.NoteTruncationForDisplay(
                            "census 配额", 60, used, (int)_cpDropQuota, "每 kind 配额",
                            "CompanyProbe.cs 的 NoteOnce 配额（该 kind used >= 60）");
                    return;
                }
                if (key == null) key = "?";
                if (!_cpSeen.Add(kind + "|" + key)) { _cpDropDup++; _cpDropDupNoted++;
                    
                    if (_cpDropDupNoted == 1 || _cpDropDupNoted % 100 == 0)
                        EconomyPatches.NoteTruncationForDisplay(
                            "census 去重", 0, (int)_cpNotePrinted, (int)_cpDropDup, "重复键",
                            "CompanyProbe.cs 的 NoteOnce 去重（同 kind|key 只记一次）");
                    return; }
                _cpHitsByKind[kind] = used + 1;
                _cpNotePrinted++;
                if (kind == "条件串全部" || kind == "条件串原样") _cpDistinctConds++;
                _log.LogInfo("[OI][company-spy] " + kind + " => " + Short(key) + (string.IsNullOrEmpty(detail) ? "" : " | " + detail));
            }
            catch { }
        }

        
        
        internal static void LogConfigFacts()
        {
            try
            {
                string path = "（取不到 ✗）";
                try { if (_cfgFile != null) path = _cfgFile.ConfigFilePath; } catch { }
                _log.LogInfo("[OI][cfg] 配置文件=" + path);
                _log.LogDebug("[OI][cfg] 开关现值 ⇒ [T1] Enabled=" + (EconomyConfig.T1BothOn ? "true" : "false")
                    + " ｜ [Company] Enabled=" + (CompanyOn ? "true" : "false")
                    + " ｜ [V2] Enabled=" + (V2On ? "true" : "false")
                    + " ｜ [Diag] ScreenDump=" + (EconomyConfig.ScreenDump != null && EconomyConfig.ScreenDump.Value ? "true" : "false")
                    + " ｜ [Bench] Enabled=" + (EconomyConfig.BenchEnabled != null && EconomyConfig.BenchEnabled.Value ? "true" : "false")
                    + "（★ 若与你改的文件不符 ⇒ 你改的不是它读的那份 ✗）");
                try { CfgGuard.CheckCfgIntent(); CfgGuard.LogDecisionPanelApi(); } catch { }
                try
                {
                    string beCfg = null;
                    try { beCfg = System.IO.Path.Combine(BepInEx.Paths.ConfigPath, "BepInEx.cfg"); } catch { }
                    string app = "（读不到 ✗）";
                    if (beCfg != null && System.IO.File.Exists(beCfg))
                        foreach (var ln in System.IO.File.ReadAllLines(beCfg))
                        { var s2 = ln.Trim(); if (s2.StartsWith("AppendLog")) { app = s2; break; } }
                    _log.LogDebug("[OI][cfg] BepInEx AppendLog=" + app + " 文件=" + (beCfg ?? "（取不到）"));
                }
                catch { }
            }
            catch { }
        }

        
        internal static void InstallCompanyProbe(Harmony harmony, ref int ok, ref int miss)
        {
            try
            {
                
                
                InstallScreenDump(harmony, ref ok, ref miss);
                InstallMenuNodeHook(harmony, ref ok, ref miss);

                
                if (EconomyConfig.T1Both == null || !EconomyConfig.T1BothOn)
                {
                    _log.LogDebug("[OI][company-spy] 已跳过（cfg 关闭）");
                    return;
                }
                var luaT = ResolveTypeSimple("Lua");
                if (luaT != null)
                {
                    int n = 0;
                    foreach (var m in luaT.GetMethods(BindingFlags.Public | BindingFlags.Static))
                    {
                        if (m.Name != "IsTrue") continue;
                        var ps = m.GetParameters();
                        if (ps.Length != 1 || ps[0].ParameterType != typeof(string)) continue;
                        try
                        {
                            harmony.Patch(m,
                                prefix: new HarmonyMethod(typeof(EconomyPatches).GetMethod(
                                    nameof(Pre_SpyIsTrue), BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public)),
                                postfix: new HarmonyMethod(typeof(EconomyPatches).GetMethod(
                                    nameof(Post_SpyIsTrue), BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public)));
                            n++;
                        }
                        catch (Exception e) { _log.LogWarning("[OI][company-spy] 挂 IsTrue 失败：" + e.GetType().Name); }
                    }
                    _log.LogInfo("[OI][company-spy] Lua.IsTrue 读口：挂上 " + n + " 个（★ 只登记、不改变返回值 ✓）");
                    if (n > 0) ok++; else miss++;
                }
                else { miss++; _log.LogWarning("[OI][company-spy] 未找到 Lua 类型 ⇒ 读口探针未挂 ✗（不影响本体 ✓）"); }

                var dlT = ResolveTypeSimple("DialogueLua");
                if (dlT != null)
                {
                    
                    
                    
                    int n2 = 0; var cands = new List<string>();
                    foreach (var m in dlT.GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy))
                    {
                        if (m.Name.IndexOf("GetVariable", StringComparison.Ordinal) < 0) continue;
                        var ps = m.GetParameters();
                        cands.Add(Sig(m));
                        if (ps.Length != 1 || ps[0].ParameterType != typeof(string)) continue;
                        try
                        {
                            harmony.Patch(m, prefix: new HarmonyMethod(typeof(EconomyPatches).GetMethod(
                                nameof(Pre_SpyGetVariable), BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public)));
                            n2++;
                        }
                        catch (Exception e) { _log.LogWarning("[OI][company-spy] 挂 GetVariable 失败：" + e.GetType().Name); }
                    }
                    _log.LogInfo("[OI][company-spy] DialogueLua.GetVariable 读口：挂上 " + n2 + " 个（★ 只登记、不改变返回值 ✓）"
                        + " ｜ 候选签名：" + (cands.Count == 0 ? "（无 ✗）" : string.Join(" ; ", cands.ToArray())));
                    if (n2 > 0) ok++; else miss++;

                    
                    
                    int n2b = 0;
                    foreach (var m in dlT.GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy))
                    {
                        if (m.Name != "GetVariableLuaValue") continue;
                        var ps = m.GetParameters();
                        if (ps.Length != 1) continue;
                        if (ps[0].ParameterType.Name.IndexOf("Variable", StringComparison.OrdinalIgnoreCase) < 0) continue;
                        try
                        {
                            harmony.Patch(m, prefix: new HarmonyMethod(typeof(EconomyPatches).GetMethod(
                                nameof(Pre_SpyGetVariableLuaValue), BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public)));
                            n2b++;
                        }
                        catch (Exception e) { _log.LogWarning("[OI][company-spy] 挂 GetVariableLuaValue 失败：" + e.GetType().Name); }
                    }
                    if (n2b > 0) _log.LogInfo("[OI][company-spy] DialogueLua.GetVariableLuaValue 读口：挂上 " + n2b + " 个");

                    
                    
                    
                    int n2c = 0;
                    foreach (var m in dlT.GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy))
                    {
                        if (m.Name != "GetQuestField") continue;
                        var ps = m.GetParameters();
                        if (ps.Length != 2 || ps[0].ParameterType != typeof(string)) continue;
                        try
                        {
                            harmony.Patch(m, prefix: new HarmonyMethod(typeof(EconomyPatches).GetMethod(
                                nameof(Pre_SpyGetQuestField), BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public)));
                            n2c++;
                        }
                        catch (Exception e) { _log.LogWarning("[OI][company-spy] 挂 GetQuestField 失败：" + e.GetType().Name); }
                    }
                    if (n2c > 0) _log.LogInfo("[OI][company-spy] DialogueLua.GetQuestField 读口：挂上 " + n2c + " 个");
                }

                
                
                var chT = ResolveTypeSimple("ConversationHandler");
                if (chT != null)
                {
                    int nLine = 0;
                    foreach (var m in chT.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
                    {
                        if (m.Name != "OnConversationLine" || m.GetParameters().Length != 1) continue;
                        try
                        {
                            harmony.Patch(m, postfix: new HarmonyMethod(typeof(EconomyPatches).GetMethod(
                                nameof(Post_ConversationHandlerLine), BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public)));
                            nLine++;
                        }
                        catch (Exception e) { _log.LogWarning("[OI][company-spy] 挂 ConversationHandler.OnConversationLine 失败：" + e.GetType().Name); }
                    }
                    if (nLine > 0) _log.LogInfo("[OI][company-spy] ConversationHandler.OnConversationLine 诊断：挂上 " + nLine + " 个");
                }

                
                
                var cmT = ResolveTypeSimple("ConversationModel");
                if (cmT != null)
                {
                    MethodInfo upd = null;
                    foreach (var m in cmT.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
                    {
                        if (m.Name == "UpdateResponses" && m.GetParameters().Length == 1) { upd = m; break; }
                    }
                    if (upd != null)
                    {
                        try
                        {
                            harmony.Patch(upd, postfix: new HarmonyMethod(typeof(EconomyPatches).GetMethod(
                                nameof(Post_ConversationModelUpdateResponses), BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public)));
                            _log.LogInfo("[OI][company-spy] ConversationModel.UpdateResponses 诊断：挂上 1 个");
                        }
                        catch (Exception e) { _log.LogWarning("[OI][company-spy] 挂 ConversationModel.UpdateResponses 失败：" + e.GetType().Name); }
                    }
                }

                
                var qcT = ResolveTypeSimple("QuestCondition");
                if (qcT != null)
                {
                    var isTrue = qcT.GetProperty("IsTrue", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                    var getter = isTrue == null ? null : isTrue.GetGetMethod(true);
                    if (getter != null)
                    {
                        try
                        {
                            harmony.Patch(getter, postfix: new HarmonyMethod(typeof(EconomyPatches).GetMethod(
                                nameof(Post_QuestConditionIsTrue), BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public)));
                            _log.LogInfo("[OI][company-spy] QuestCondition.IsTrue 读口：挂上 1 个");
                        }
                        catch (Exception e) { _log.LogWarning("[OI][company-spy] 挂 QuestCondition.IsTrue 失败：" + e.GetType().Name); }
                    }
                }
                else { miss++; _log.LogWarning("[OI][company-spy] 未找到 DialogueLua 类型 ⇒ 探针未挂 ✗（不影响本体 ✓）"); }

                
                if (luaT != null)
                {
                    int nw = 0; var candsR = new List<string>();
                    foreach (var m in luaT.GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy))
                    {
                        
                        if (m.Name != "Run" && m.Name != "RunRaw") continue;
                        var ps = m.GetParameters();
                        candsR.Add(Sig(m));
                        if (ps.Length != 1 || ps[0].ParameterType != typeof(string)) continue;
                        try
                        {
                            harmony.Patch(m, prefix: new HarmonyMethod(typeof(EconomyPatches).GetMethod(
                                nameof(Pre_SpyRun), BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public)));
                            nw++;
                        }
                        catch (Exception e) { _log.LogWarning("[OI][company-spy-w] 挂 Lua.Run 失败：" + e.GetType().Name); }
                    }
                    _log.LogInfo("[OI][company-spy-w] Lua.Run/RunRaw 写口：挂上 " + nw + " 个（★ 只登记、一律放行 ✓）"
                        + " ｜ 候选签名：" + (candsR.Count == 0 ? "（无 ✗）" : string.Join(" ; ", candsR.ToArray())));
                    if (nw > 0) ok++; else miss++;
                }

                if (dlT != null)
                {
                    int nw2 = 0; var cands2 = new List<string>();
                    
                    foreach (var m in dlT.GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy))
                    {
                        if (m.Name.IndexOf("SetVariable", StringComparison.Ordinal) < 0) continue;
                        var ps2 = m.GetParameters();
                        cands2.Add(Sig(m));
                        if (ps2.Length != 2 || ps2[0].ParameterType != typeof(string)) continue;
                        try
                        {
                            harmony.Patch(m, prefix: new HarmonyMethod(typeof(EconomyPatches).GetMethod(
                                nameof(Pre_SpySetVariable), BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public)));
                            nw2++;
                        }
                        catch (Exception e) { _log.LogWarning("[OI][company-spy-w] 挂 SetVariable 失败：" + e.GetType().Name); }
                    }
                    _log.LogInfo("[OI][company-spy-w] DialogueLua.SetVariable 写口：挂上 " + nw2 + " 个（★ 只登记、一律放行 ✓）"
                        + " ｜ 候选签名：" + (cands2.Count == 0 ? "（无 ✗）" : string.Join(" ; ", cands2.ToArray())));
                    if (nw2 > 0) ok++; else miss++;
                }

                
                try
                {
                    var ciT = ResolveTypeSimple("ConditionalInstructionHelper");
                    int nc = 0;
                    if (ciT != null)
                    {
                        foreach (var m in ciT.GetMethods(BindingFlags.Public | BindingFlags.Static))
                        {
                            if (m.Name != "Evaluate") continue;
                            var ps = m.GetParameters();
                            if (ps.Length != 1) continue;
                            string pn = ps[0].ParameterType.Name;
                            if (pn.IndexOf("ConditionalInstructionData", StringComparison.Ordinal) < 0) continue;
                            try
                            {
                                harmony.Patch(m, prefix: new HarmonyMethod(typeof(EconomyPatches).GetMethod(
                                    nameof(Pre_SpyEvaluate), BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public)));
                                nc++;
                            }
                            catch (Exception e) { _log.LogWarning("[OI][company-spy-w] 挂 Evaluate 失败：" + e.GetType().Name); }
                        }
                    }
                    _log.LogInfo("[OI][company-spy-w] ConditionalInstructionHelper.Evaluate 上下文：挂上 " + nc + " 个（★ 只读 ✓）");
                    if (nc > 0) ok++; else miss++;
                }
                catch (Exception e) { miss++; _log.LogWarning("[OI][company-spy-w] Evaluate 上下文未挂：" + e.GetType().Name); }
            }
            catch (Exception e) { miss++; _log.LogWarning("[OI][company-spy] 安装异常（已吞）：" + e.GetType().Name + " — " + Short(e.Message)); }
        }

        
        
        private static bool Pre_SpyIsTrue(string luaCondition, ref bool __result)
        {
            try
            {
                
                
                if (_cpInSnap) return true;                  
                if (luaCondition == null) return true;
                bool relevant =
                    luaCondition.IndexOf("Turn02_InT_Corporation_", StringComparison.Ordinal) >= 0
                    || luaCondition.IndexOf("Journal_Turn02_Construction", StringComparison.Ordinal) >= 0
                    || luaCondition.IndexOf("Turn01_InT_Investment_", StringComparison.Ordinal) >= 0
                    || luaCondition.IndexOf("Listened_", StringComparison.Ordinal) >= 0;
                if (!relevant) return true;

                
                
                if (TryT3PriorityAnswer(luaCondition, ref __result)) { LogShort("T3", luaCondition, __result); return false; }
                
                if (TryPerProjectAnswer(luaCondition, ref __result)) { LogShort("PerProject", luaCondition, __result); return false; }   
                
                if (TryV2Push(luaCondition, ref __result)) { LogShort("V2", luaCondition, __result); return false; }              
                if (TokenHit(luaCondition)) NoteOnce("Lua.IsTrue 读了", luaCondition, "（★ 这就是条件求值读口 ✓）");
                
                if (ProjectHit(luaCondition)) NoteProjectInput(luaCondition);
                
                
                if (luaCondition != null && (
                        luaCondition.IndexOf("Listened", StringComparison.Ordinal) >= 0 ||
                        luaCondition.IndexOf("Investment", StringComparison.Ordinal) >= 0 ||
                        luaCondition.IndexOf("Corporation", StringComparison.Ordinal) >= 0 ||
                        luaCondition.IndexOf("Progress_", StringComparison.Ordinal) >= 0))
                    NoteOnce("条件串原样", luaCondition, "（★ 真实形态 ✗ 供改判据 ✓）");
                
                
                
                
                if (luaCondition != null && luaCondition.Length > 10 && _cpCondDumped.Count < 200 &&
                    (luaCondition.IndexOf("==", StringComparison.Ordinal) >= 0 ||
                     luaCondition.IndexOf("Turn0", StringComparison.Ordinal) >= 0 ||
                     luaCondition.IndexOf("BaseGame", StringComparison.Ordinal) >= 0))
                    if (_cpCondDumped.Add(luaCondition)) NoteOnce("条件串全部", luaCondition, "（★ 非平凡真串 ✗ 供改判据 ✓）");
                _cpIsTrueCalls++;                            
                SnapshotTick();                              
                BenchTick();                                 
            }
            catch { }
            return true;
        }

        

        
        
        
        
        
        private static readonly string[] CompanyFlagNames = new string[]
        {
            "Journal_Turn02_ConstructionSSC",
            "Journal_Turn02_ConstructionTaurus",
            "Journal_Turn02_ConstructionUnderhall",
            
            "Turn02_InT_Corporation_SordishState",
            "Turn02_InT_Corporation_TaurusHolding",
            "Turn02_InT_Corporation_UnderhallConstruction",
        };

        
        
        
        private static long _cpGaHits, _cpGaWithNode;
        
        private static long _cpAqFalseForm, _cpAqTrueForm;
        private static long _cpAaEntry, _cpAaNoSwitch, _cpAaBang, _cpAaFalse, _cpAaNoProj, _cpAaTwoProj, _cpAaNoWant, _cpAaNoCompany, _cpAaPrinted;
        private static string _cpCompanyH3;
        private static string _cpCompanyL1;
        
        
        private static bool _cpFlagHighway, _cpFlagRailway;
        private static bool _cpFlagSsc, _cpFlagTaurus, _cpFlagUnderhall;
        private static readonly HashSet<string> _cpDecided = new HashSet<string>(StringComparer.Ordinal);

        
        private static bool CompanyOn { get { try { return EconomyConfig.CompanyFix != null && EconomyConfig.CompanyFix.Value; } catch { return false; } } }

        
        private static bool T3On { get { try { return EconomyConfig.T3Enabled != null && EconomyConfig.T3Enabled.Value; } catch { return false; } } }
        private static readonly HashSet<string> _cpT3Logged = new HashSet<string>(StringComparer.Ordinal);
        private static readonly HashSet<string> _cpT3TitleSeen = new HashSet<string>(StringComparer.Ordinal);
        private static readonly HashSet<string> _cpT3DialogueSeen = new HashSet<string>(StringComparer.Ordinal);
        private static readonly HashSet<string> _cpT3LinkSeen = new HashSet<string>(StringComparer.Ordinal);
        private static long _cpT3Answers, _cpT3NoPriority, _cpT3GusUnderhall;
        private static readonly HashSet<string> _cpShortLogged = new HashSet<string>(StringComparer.Ordinal);

        
        private static void LogShort(string who, string cond, bool answer)
        {
            try
            {
                if (_cpShortLogged.Count >= 80 || !_cpShortLogged.Add(who + "|" + cond + "|" + answer)) return;
                _log.LogInfo("[OI][short] who=" + who + " answer=" + (answer ? "true" : "false")
                    + " cond=" + Short(cond));
            }
            catch { }
        }

        
        private static string T3Priority()
        {
            if (EconomyConfig.T3ForceUnderhallOn) return T3CompanyPriority.Underhall;
            string p = T3CompanyPriority.Choose(_cpCompanyH3, _cpCompanyL1);
            if (!string.IsNullOrEmpty(p)) return p;
            return T3CompanyPriority.ChooseFromFlags(_cpFlagUnderhall, _cpFlagTaurus, _cpFlagSsc);
        }

        private static bool IsJournalCompanyFlag(string variable)
        {
            if (variable == null) return false;
            return variable.IndexOf("Journal_Turn02_ConstructionSSC", StringComparison.Ordinal) >= 0
                || variable.IndexOf("Journal_Turn02_ConstructionTaurus", StringComparison.Ordinal) >= 0
                || variable.IndexOf("Journal_Turn02_ConstructionUnderhall", StringComparison.Ordinal) >= 0;
        }

        private static bool IsCorporationCompanyFlag(string variable)
        {
            if (variable == null) return false;
            return variable.IndexOf("Turn02_InT_Corporation_SordishState", StringComparison.Ordinal) >= 0
                || variable.IndexOf("Turn02_InT_Corporation_TaurusHolding", StringComparison.Ordinal) >= 0
                || variable.IndexOf("Turn02_InT_Corporation_UnderhallConstruction", StringComparison.Ordinal) >= 0;
        }

        private static bool IsCompanyReadFlag(string variable)
        {
            return IsJournalCompanyFlag(variable) || IsCorporationCompanyFlag(variable);
        }

        private static bool CompanyFlagIsPriority(string variable, string priority)
        {
            if (variable == null || priority == null) return false;
            if (priority == T3CompanyPriority.Underhall)
                return variable.IndexOf("Journal_Turn02_ConstructionUnderhall", StringComparison.Ordinal) >= 0
                    || variable.IndexOf("Turn02_InT_Corporation_UnderhallConstruction", StringComparison.Ordinal) >= 0;
            if (priority == T3CompanyPriority.Taurus)
                return variable.IndexOf("Journal_Turn02_ConstructionTaurus", StringComparison.Ordinal) >= 0
                    || variable.IndexOf("Turn02_InT_Corporation_TaurusHolding", StringComparison.Ordinal) >= 0;
            if (priority == T3CompanyPriority.Ssc)
                return variable.IndexOf("Journal_Turn02_ConstructionSSC", StringComparison.Ordinal) >= 0
                    || variable.IndexOf("Turn02_InT_Corporation_SordishState", StringComparison.Ordinal) >= 0;
            return false;
        }

        private static bool JournalFlagIsPriority(string variable, string priority)
        {
            if (variable == null || priority == null) return false;
            if (priority == T3CompanyPriority.Underhall)
                return variable.IndexOf("Journal_Turn02_ConstructionUnderhall", StringComparison.Ordinal) >= 0;
            if (priority == T3CompanyPriority.Taurus)
                return variable.IndexOf("Journal_Turn02_ConstructionTaurus", StringComparison.Ordinal) >= 0;
            if (priority == T3CompanyPriority.Ssc)
                return variable.IndexOf("Journal_Turn02_ConstructionSSC", StringComparison.Ordinal) >= 0;
            return false;
        }

        private static string T3CorporationFlag(string priority)
        {
            if (priority == T3CompanyPriority.Underhall) return "BaseGame.Turn02_InT_Corporation_UnderhallConstruction";
            if (priority == T3CompanyPriority.Taurus) return "BaseGame.Turn02_InT_Corporation_TaurusHolding";
            if (priority == T3CompanyPriority.Ssc) return "BaseGame.Turn02_InT_Corporation_SordishState";
            return null;
        }

        private static string T3TruthyProjectFlag()
        {
            if (_cpFlagHighway) return "BaseGame.Turn01_InT_Investment_Highway";
            if (_cpFlagRailway) return "BaseGame.Turn01_InT_Investment_Railway";
            return null;
        }

        
        private static void RefreshT3FlagsFromGame()
        {
            if (_cpInSnap) return;
            try
            {
                _cpInSnap = true;
                _cpFlagHighway = IsTrue(SnapNames[0]);
                _cpFlagRailway = IsTrue(SnapNames[1]);
                _cpFlagSsc = IsTrue(SnapNames[5]);
                _cpFlagTaurus = IsTrue(SnapNames[6]);
                _cpFlagUnderhall = IsTrue(SnapNames[7]);
            }
            catch { }
            finally { _cpInSnap = false; }
        }

        
        
        
        
        
        
        private static string T3RedirectCompany(string variable)
        {
            if (!T3On || !IsCompanyReadFlag(variable)) return variable;
            if (!_cpFlagHighway && !_cpFlagRailway && !_cpFlagSsc && !_cpFlagTaurus && !_cpFlagUnderhall)
                RefreshT3FlagsFromGame();
            string priority = T3Priority();
            if (string.IsNullOrEmpty(priority)) return variable;
            if (!CompanyFlagIsPriority(variable, priority))
                return "Oikonomia.T3.JournalNotPriority";
            
            string target = IsCorporationCompanyFlag(variable) ? variable : T3CorporationFlag(priority);
            if (string.IsNullOrEmpty(target)) return variable;
            
            
            bool rawTrue = priority == T3CompanyPriority.Underhall ? _cpFlagUnderhall
                         : priority == T3CompanyPriority.Taurus ? _cpFlagTaurus
                         : _cpFlagSsc;
            if (rawTrue) return target;
            string truthy = T3TruthyProjectFlag();
            return string.IsNullOrEmpty(truthy) ? variable : truthy;
        }

        private static string CompanyShort(string flag)
        {
            
            if (flag.IndexOf("Turn02_InT_Corporation_SordishState", StringComparison.Ordinal) >= 0) return "SSC";
            if (flag.IndexOf("Turn02_InT_Corporation_TaurusHolding", StringComparison.Ordinal) >= 0) return "Taurus";
            if (flag.IndexOf("Turn02_InT_Corporation_UnderhallConstruction", StringComparison.Ordinal) >= 0) return "Underhall";
            if (flag == null) return "?";
            if (flag.EndsWith("SSC", StringComparison.Ordinal)) return "SSC";
            if (flag.EndsWith("Taurus", StringComparison.Ordinal)) return "Taurus";
            if (flag.EndsWith("Underhall", StringComparison.Ordinal)) return "Underhall";
            return "?";
        }

        
        private static bool TryPerProjectAnswer(string cond, ref bool result)
        {
            _cpAaEntry++;
            try
            {
                if (cond == null) return false;
                if (!CompanyOn) { _cpAaNoSwitch++; return false; }
                if (cond.IndexOf('!') >= 0) { _cpAaBang++; return false; }
                
                
                if (cond.IndexOf("InT_Investment_", StringComparison.Ordinal) >= 0)
                {
                    if (cond.IndexOf("== false", StringComparison.Ordinal) >= 0) _cpAqFalseForm++; else _cpAqTrueForm++;
                    NoteOnce("含项目条件原样", cond, "（★ 原样 ✓ 供定语义 ✓）");
                }
                if (cond.IndexOf("== false", StringComparison.Ordinal) >= 0) { _cpAaFalse++; return false; }
                bool hw = cond.IndexOf("Investment_Highway", StringComparison.Ordinal) >= 0;
                bool rw = cond.IndexOf("Investment_Railway", StringComparison.Ordinal) >= 0;
                if (!hw && !rw) { _cpAaNoProj++; return false; }
                if (hw && rw) { _cpAaTwoProj++; return false; }
                string want = hw ? _cpCompanyH3 : _cpCompanyL1;
                if (string.IsNullOrEmpty(want))
                {
                    _cpAaNoWant++;
                    
                    string _h3 = _cpCompanyH3;
                    string _l1 = _cpCompanyL1;
                    string _t = DateTime.Now.ToString("HH:mm:ss.fff");
                    NoteOnce("answer-t want空", cond, "（时刻=" + _t + " ｜ 归属 H3=" + (_h3 ?? "-") + " L1=" + (_l1 ?? "-") + "）");
                    return false;
                }
                string got = null; int n = 0;
                for (int i = 0; i < CompanyFlagNames.Length; i++)
                {
                    if (cond.IndexOf(CompanyFlagNames[i], StringComparison.Ordinal) < 0) continue;
                    n++; got = CompanyShort(CompanyFlagNames[i]);
                }
                if (n != 1) { _cpAaNoCompany++; return false; }
                result = string.Equals(got, want, StringComparison.Ordinal);
                
                NoteOnce("answer-t 打印", cond,
                    "（时刻=" + DateTime.Now.ToString("HH:mm:ss.fff") + " ｜ 结果=" + (result ? "真 ✓" : "假 ✗") + " ✓）");
                _cpAaPrinted++;
                string key = (hw ? "H-3" : "L-1") + "|" + got + "|" + (result ? "T" : "F");
                if (_cpDecided.Count < 60 && _cpDecided.Add(key))
                    _log.LogInfo("[OI][company-answer] " + DateTime.Now.ToString("HH:mm:ss.fff")
                        + " 按项目自答 ⇒ 项目=" + (hw ? "H-3 ✓" : "L-1 ✓")
                        + " ｜ 条件要=" + got + " ｜ 该项目选的是=" + want
                        + " ⇒ 结果=" + (result ? "真 ✓" : "假 ✗") + " ｜ 条件=" + Short(cond));
                return true;
            }
            catch { return false; }
        }

        

        
        private static string CurrentConversationTitle(out string how)
        {
            how = "无 ✗";
            try
            {
                var dmT = ResolveTypeSimple("DialogueManager");
                if (dmT == null) { how = "DialogueManager 类型没找到 ✗"; return null; }
                object model = Reflect.Static(dmT, "conversationModel");
                if (!Reflect.Usable(model))
                {
                    object ctrl = Reflect.Static(dmT, "conversationController");
                    if (Reflect.Usable(ctrl)) model = Reflect.Prop(ctrl, "conversationModel");
                }
                if (!Reflect.Usable(model)) { how = "conversationModel=null ✗"; return null; }
                string title = Reflect.Str(Reflect.Prop(model, "conversationTitle"));
                if (string.IsNullOrEmpty(title)) { how = "conversationTitle 空 ✗"; return null; }
                how = "有值 ✓";
                return title;
            }
            catch (Exception e) { how = "读异常：" + e.GetType().Name; return null; }
        }

        
        private static bool T3Scope(out string title)
        {
            string how;
            title = CurrentConversationTitle(out how);
            bool hit = T3CompanyPriority.IsT3Conversation(title);
            
            
            
            try
            {
                if (T3On && _cpT3TitleSeen.Count < 20
                    && _cpT3TitleSeen.Add((title ?? "（null ✗）") + "|" + how))
                    _log.LogInfo("[OI][t3] 对话标题诊断 ⇒ 原样=[" + (title ?? "（null ✗）") + "]"
                        + " ｜ 读法=" + how + " ｜ T3命中=" + (hit ? "是 ✓" : "否 ✗"));
            }
            catch { }
            return hit;
        }

        
        
        
        
        
        private static bool TryT3PriorityAnswer(string cond, ref bool result)
        {
            try
            {
                if (cond == null || !T3On) return false;
                string title;
                
                bool inT3 = T3Scope(out title);
                if (!inT3) return false;
                
                if (cond.IndexOf("InT_Investment_", StringComparison.Ordinal) >= 0) return false;
                
                if (cond.IndexOf('!') >= 0 || cond.IndexOf("== false", StringComparison.Ordinal) >= 0) return false;

                var companies = new HashSet<string>(StringComparer.Ordinal);
                for (int i = 0; i < CompanyFlagNames.Length; i++)
                {
                    if (cond.IndexOf(CompanyFlagNames[i], StringComparison.Ordinal) >= 0)
                        companies.Add(CompanyShort(CompanyFlagNames[i]));
                }
                if (companies.Count != 1) return false;

                string priority = T3Priority();
                if (string.IsNullOrEmpty(priority))
                {
                    _cpT3NoPriority++;
                    string key = "nop|" + title + "|" + cond;
                    if (_cpT3Logged.Count < 60 && _cpT3Logged.Add(key))
                        _log.LogInfo("[OI][t3] 有公司条件但【无归属】⇒ 放行真值 ✗ ｜ 对话=" + title
                            + " ｜ 条件=" + Short(cond));
                    return false;
                }

                bool gus = title.IndexOf("Gus", StringComparison.OrdinalIgnoreCase) >= 0;
                if (gus && string.Equals(priority, T3CompanyPriority.Underhall, StringComparison.Ordinal))
                {
                    _cpT3GusUnderhall++;
                    string key = "gus-uh|" + title + "|" + cond;
                    if (_cpT3Logged.Count < 60 && _cpT3Logged.Add(key))
                        _log.LogInfo("[OI][t3] Gus 缺 Underhall 分支 ⇒ 不硬改，放行真值 ✗ ｜ 对话=" + title
                            + " ｜ 条件=" + Short(cond) + "（★ 这是已知内容缺口，不用假答案填）");
                    return false;
                }

                string wanted = null;
                foreach (var c in companies) { wanted = c; break; }
                bool answer = T3CompanyPriority.Matches(wanted, priority);
                result = answer;
                _cpT3Answers++;
                string logKey = title + "|" + wanted + "|" + priority + "|" + (answer ? "T" : "F");
                if (_cpT3Logged.Count < 60 && _cpT3Logged.Add(logKey))
                    _log.LogInfo("[OI][t3] 作答 ⇒ 对话=" + title
                        + " ｜ 条件公司=" + wanted + " ｜ 优先级=" + priority
                        + " ｜ 结果=" + (answer ? "真 ✓" : "假 ✗") + " ｜ 条件=" + Short(cond));
                return true;
            }
            catch { return false; }
        }

        

        private static BepInEx.Configuration.ConfigFile _cfgFile;

        
        internal static void BindConfigFile(BepInEx.Configuration.ConfigFile f) { _cfgFile = f; }
        internal static void InstallMenuNodeHook(Harmony harmony, ref int ok, ref int miss)
        {
            try
            {
                if (!CompanyOn) { _log.LogDebug("[OI][company-node] 已跳过（cfg 关闭）"); return; }
                var t = ResolveTypeSimple("ConversationHandler");
                if (t == null) { miss++; _log.LogWarning("[OI][company-node] 找不到 ConversationHandler ⇒ 未挂 ✗"); return; }
                int n = 0;
                var sigs = new System.Collections.Generic.List<string>();
                var mi = typeof(EconomyPatches).GetMethod(nameof(Post_MenuNodeArgs),
                    BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
                foreach (var m in t.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
                {
                    var mn = m.Name;
                    if (mn.IndexOf("OnConversationResponseMenu", StringComparison.Ordinal) < 0
                        && mn.IndexOf("OnResponseMenu", StringComparison.Ordinal) < 0
                        && mn.IndexOf("ShowResponseMenu", StringComparison.Ordinal) < 0
                        && mn.IndexOf("OnConversationLine", StringComparison.Ordinal) < 0) continue;
                    var ps = m.GetParameters();
                    sigs.Add(mn + "(" + string.Join(",", Array.ConvertAll(ps, x => x.ParameterType.Name)) + ")");
                    try { harmony.Patch(m, postfix: new HarmonyMethod(mi)); n++; }
                    catch (Exception e) { _log.LogWarning("[OI][company-node] 挂 " + mn + " 失败 ⇒ " + e.GetType().Name); }
                }
                _log.LogInfo("[OI][company-node] 候选菜单/行签名 " + sigs.Count + " 个：" + string.Join(" ｜ ", sigs.ToArray()));
                _log.LogInfo("[OI][company-node] ★ 挂上 " + n + " 个（★ 0 ⇒ 就是这个病 ✗ ✓ · ★ [Company] Enabled=" + (CompanyOn ? "true" : "false") + " ✓）");
                _log.LogInfo("[OI][company-node] W4 公司选择屏节点表：" + CompanyNodeMap.Count
                    + " 项（★ 2.10.111 起覆盖离线台账 §1.2 的 10 个屏 ✓；未命中仍不记 ✓）");
                if (n > 0) ok++; else miss++;
            }
            catch (Exception e) { miss++; _log.LogWarning("[OI][company-node] 安装异常：" + e.GetType().Name); }
        }
        internal static bool CompanyOnPublic { get { return CompanyOn; } }
        internal static bool V2OnPublic { get { return V2On; } }
        internal static void ClearAttribution() { _cpCompanyH3 = null; _cpCompanyL1 = null; }
        
        
        
        
        
        
        internal static string W5Cluster()
        {
            try
            {
                
                
                
                string[] h3 = { "News_Turn02_HP_H3_Taurus", "News_Turn02_HP_H3_SSC", "News_Turn02_ST_H3_Taurus", "News_Turn02_ST_H3_SSC", "Turn04_InT_HighwayIssue_Solved" };
                string[] l1 = { "News_Turn02_HP_L1_Taurus", "News_Turn02_HP_L1_Underhall", "News_Turn02_ST_L1_Taurus", "Turn04_InT_RailwayIssue_Solved" };
                bool h = false, l = false;
                foreach (var n in h3) { if (CfgGuard.FlagBool(n)) { h = true; break; } }
                foreach (var n in l1) { if (CfgGuard.FlagBool(n)) { l = true; break; } }
                if (h && !l) return "H-3";
                if (l && !h) return "L-1";
                return null;
            }
            catch { return null; }
        }
        internal static void ClearPendingCluster(string why)
        {
            _cpLastCluster = null; _cpLastClusterAt = default(DateTime); _cpLastClusterSrc = "（读档已清 ✗）";
            _log.LogInfo("[OI][company-map] 待消费戳已清 ⇒ 原因=" + why + " ✓");
        }

        private static int _dumpLines;
        private static long _cpIsTrueCalls;

        private static bool DumpOn { get { try { return (EconomyConfig.ScreenDump != null && EconomyConfig.ScreenDump.Value)
            || (EconomyConfig.BenchEnabled != null && EconomyConfig.BenchEnabled.Value); } catch { return false; } } }
        

        
        internal static void InstallScreenDump(Harmony harmony, ref int ok, ref int miss)
        {
            try
            {
                if (!DumpOn) { _log.LogDebug("[OI][dump] 已跳过（cfg 关闭）"); return; }
                var t = ResolveTypeSimple("ConversationHandler");
                if (t == null) { miss++; _log.LogWarning("[OI][dump] 找不到 ConversationHandler ⇒ 未挂 ✗"); return; }
                int n = 0;
                var sigs = new System.Collections.Generic.List<string>();
                var mi = typeof(EconomyPatches).GetMethod(nameof(Post_DumpMenuArgs),
                    BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
                foreach (var m in t.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
                {
                    if (m.Name != "OnConversationResponseMenu") continue;
                    var ps = m.GetParameters();
                    sigs.Add(m.Name + "(" + string.Join(",", Array.ConvertAll(ps, x => x.ParameterType.Name)) + ")");
                    try { harmony.Patch(m, postfix: new HarmonyMethod(mi)); n++; }
                    catch (Exception e) { _log.LogWarning("[OI][dump] 挂菜单失败：" + m.Name + " ⇒ " + e.GetType().Name); }
                }
                _log.LogDebug("[OI][dump] 候选人选签名 " + sigs.Count + " 个：" + string.Join(" ｜ ", sigs.ToArray()));
                _log.LogInfo("[OI][dump] 屏幕转储已挂 " + n + " 个（只读）");
                if (n > 0) ok++; else miss++;
            }
            catch (Exception e) { miss++; _log.LogWarning("[OI][dump] 安装异常：" + e.GetType().Name); }
        }

        
        
        
        
        private static void Post_MenuNode(object responses)
        {
            try
            {
                if (!CompanyOn) return;
                
                
                
                string how; string id = CurrentNodeArticy(out how);
                
                
                
                
                
                
                try
                {
                    if (CompanyOn && id != null)
                    {
                        
                        
                        string hit;
                        if (CompanyNodeMap.TryGet(id, out hit))
                        {
                            _cpLastCluster = hit;
                            _cpLastClusterSrc = "W4-节点表";
                            _cpLastClusterAt = DateTime.UtcNow;
                            _log.LogInfo("[OI][company-map] 归属依据=节点表 ⇒ 节点=" + id + " ⇒ 归属=" + hit
                                + " ✓（★ 只读 ✓ 不看旗状态 ✓ ⇒ 两个项目都在跑时也成立 ✓）");
                        }
                        else if (_cpWhyLogged.Count < 90 && _cpWhyLogged.Add("node4|" + id))
                            _log.LogInfo("[OI][company-map] 节点表未命中 ⇒ 节点=" + id + " ⇒ 未知 ✗ ⇒ 不记 ✓（★ 闸门保留 ✓）");
                    }
                }
                catch { }
                var parts = new System.Collections.Generic.List<string>();
                string hitCluster = null; int hitIndex = 0; string hitDestId = null;
                try
                {
                    var en = responses as System.Collections.IEnumerable;
                    if (en != null)
                    {
                        int k = 0;
                        foreach (var r in en)
                        {
                            k++;
                            string destId = null;
                            try { object de = Reflect.Prop(r, "destinationEntry"); if (Reflect.Usable(de)) destId = EntryField(de, "Articy Id"); } catch { }
                            string cl = ClusterOf(destId);
                            
                            
                            
                            
                            
                            if ((cl.StartsWith("H-3", StringComparison.Ordinal) || cl.StartsWith("L-1", StringComparison.Ordinal))
                                && hitCluster == null) { hitCluster = cl; hitIndex = k; hitDestId = destId; }
                            if (parts.Count < 8) parts.Add("项#" + k + " dest=" + (destId ?? "（无 ✗）") + " ⇒ 簇=" + cl + " ｜ " + Short(BenchResponseText(r)));
                        }
                    }
                }
                catch { }
                
                
                
                bool dupMenu = hitCluster != null && !_cpMenuLogged.Add((id ?? "null") + "|" + (hitCluster ?? "-"));
                if (hitCluster != null)
                {
                    _log.LogInfo("[OI][cp-write] 写入者=W2-菜单 ｜ 值=" + (hitCluster.StartsWith("H-3", StringComparison.Ordinal) ? "H-3" : "L-1")
                        + " ｜ dest=" + (hitDestId ?? "-") + " ｜ 命中项=#" + hitIndex + " ｜ 当前节点=" + (id ?? "null")
                        + " ｜ dup=" + (dupMenu ? "true（★ 同一次菜单重复 ⇒ 不重复打戳 ✓）" : "false ✓")
                        + " ｜ 时间=" + DateTime.Now.ToString("HH:mm:ss.fff"));
                }
                if (hitCluster != null && !dupMenu)
                {
                    _cpLastCluster = hitCluster.StartsWith("H-3", StringComparison.Ordinal) ? "H-3" : "L-1";
                    _cpLastClusterSrc = "W2-菜单";
                    _cpLastClusterAt = DateTime.UtcNow;
                }
                if (!dupMenu)
                    _log.LogInfo("[OI][company-node] " + DateTime.Now.ToString("HH:mm:ss.fff")
                        + " 对话层菜单 ⇒ 当前节点=" + (id ?? "（null ✗）") + " ｜ 读法=" + how
                        + " ｜ ★ 按选项 destination 判出的簇=" + (hitCluster ?? "未知 ✗") + "（★ 命中项=#" + hitIndex + " ✓ dest=" + (hitDestId ?? "-") + " ✓）"
                        + " ｜ 选项：" + string.Join(" ;; ", parts.ToArray()));
            }
            catch { }
        }

        private static object FirstOf(System.Collections.IEnumerable en)
        {
            try { foreach (var x in en) return x; } catch { }
            return null;
        }

        private static readonly HashSet<string> _cpMenuLogged = new HashSet<string>(StringComparer.Ordinal);

        private static void Post_DumpMenuArgs(object[] __args)
        {
            try { Post_DumpMenu(__args != null && __args.Length > 0 ? __args[0] : null); } catch { }
        }

        private static void Post_MenuNodeArgs(object[] __args)
        {
            try { Post_MenuNode(__args != null && __args.Length > 0 ? __args[0] : null); } catch { }
        }

        private static void Post_DumpMenu(object responses)
        {
            try
            {
                if (!DumpOn || _dumpLines >= 400) return;
                var en = responses as System.Collections.IEnumerable;
                if (en == null) return;
                int i = 0;
                foreach (var r in en)
                {
                    if (_dumpLines >= 400) break;
                    i++;
                    string id = null;
                    try
                    {
                        object de = Reflect.Prop(r, "destinationEntry");
                        if (Reflect.Usable(de)) id = EntryField(de, "Articy Id");
                    }
                    catch { }
                    _dumpLines++;
                    _log.LogInfo("[OI][dump] " + DateTime.Now.ToString("HH:mm:ss.fff")
                        + " 项#" + i + " ｜ id=" + (id ?? "（无 ✗）") + " ｜ 文本=" + Short(BenchResponseText(r)));
                }
            }
            catch { }
        }

        

        
        
        private static readonly string[] V2ListenedNames = new string[]
        {
            "BaseGameIsolated.Turn01_EnT_Listened_Highway",
            "BaseGameIsolated.Turn01_EnT_Listened_Railway",
        };

        private static int _v2PushH3;
        private static int _v2PushL1;
        private static int _v2PushTotal;
        private static readonly HashSet<string> _v2Logged = new HashSet<string>(StringComparer.Ordinal);

        private static bool V2On { get { try { return EconomyConfig.V2Push != null && EconomyConfig.V2Push.Value; } catch { return false; } } }
        private static int V2MaxPer { get { try { return EconomyConfig.V2MaxPerProject != null ? EconomyConfig.V2MaxPerProject.Value : 1; } catch { return 1; } } }
        private static int V2MaxAll { get { try { return EconomyConfig.V2MaxTotal != null ? EconomyConfig.V2MaxTotal.Value : 4; } catch { return 4; } } }

        
        
        private static bool TryV2Push(string cond, ref bool result)
        {
            try
            {
                if (cond == null || !V2On) return false;
                
                bool hw = cond.IndexOf(V2ListenedNames[0], StringComparison.Ordinal) >= 0;
                bool rw = cond.IndexOf(V2ListenedNames[1], StringComparison.Ordinal) >= 0;
                if (hw == rw) return false;                              
                if (cond.IndexOf("== false", StringComparison.Ordinal) < 0) return false;
                if (V2MaxAll > 0 && _v2PushTotal >= V2MaxAll)
                {
                    V2LogOnce("超全局上限 ⇒ 放行真值 ✗", cond, 0);
                    return false;
                }
                int mine = hw ? _v2PushH3 : _v2PushL1;
                if (V2MaxPer > 0 && mine >= V2MaxPer) { V2LogOnce("该项目已达上限 ⇒ 放行真值 ✗", cond, 0); return false; }

                
                bool tgt = false, oth = false;
                try
                {
                    tgt = IsTrue(hw ? "BaseGame.Turn01_InT_Investment_Highway" : "BaseGame.Turn01_InT_Investment_Railway");
                    oth = IsTrue(hw ? "BaseGame.Turn01_InT_Investment_Railway" : "BaseGame.Turn01_InT_Investment_Highway");
                }
                catch { return false; }
                if (tgt || !oth)
                {
                    V2LogOnce("未命中判据（目标已落地 或 另一项目未落地）⇒ 放行真值 ✗", cond, 0);
                    return false;
                }

                result = false;                                          
                if (hw) _v2PushH3++; else _v2PushL1++;
                _v2PushTotal++;
                _log.LogInfo("[OI][v2-push] " + DateTime.Now.ToString("HH:mm:ss.fff")
                    + " 推进 ⇒ 目标=" + (hw ? "H-3 ✓" : "L-1 ✓")
                    + "（★ 另一项目已落地 ✓ 而它未落地 ✓）"
                    + " ｜ 上限：该项目 " + (hw ? _v2PushH3 : _v2PushL1) + "/" + V2MaxPer + " · 总 " + _v2PushTotal + "/" + V2MaxAll
                    + " ｜ 回答=false ⇒ ★ 引擎自己跳回讲解 ✓ ｜ 条件=" + Short(cond));
                return true;
            }
            catch { return false; }
        }

        private static void V2LogOnce(string why, string cond, int dummy)
        {
            try
            {
                string key = why + "|" + cond;
                if (_v2Logged.Count >= 40 || !_v2Logged.Add(key)) return;
                _log.LogInfo("[OI][v2-push] " + DateTime.Now.ToString("HH:mm:ss.fff") + " " + why + " ｜ 条件=" + Short(cond));
            }
            catch { }
        }

        

        private static readonly string[] SnapNames = new string[]
        {
            "BaseGame.Turn01_InT_Investment_Highway",
            "BaseGame.Turn01_InT_Investment_Railway",
            "Journal_Turn02_ConstructionSSC",
            "Journal_Turn02_ConstructionTaurus",
            "Journal_Turn02_ConstructionUnderhall",
            "BaseGame.Turn02_InT_Corporation_SordishState",
            "BaseGame.Turn02_InT_Corporation_TaurusHolding",
            "BaseGame.Turn02_InT_Corporation_UnderhallConstruction",
        };

        private static DateTime _cpNextSnap = DateTime.MinValue;
        
        private static readonly HashSet<string> _cpCondDumped = new HashSet<string>(StringComparer.Ordinal);
        
        
        private static DateTime _cpCensusAt = DateTime.MinValue;
        private static long _cpGetVarCalls;
        private static string _cpSnapPrev;
        private static bool _cpInSnap;
        private static int _cpSnapLines;

        
        private static void SnapshotTick()
        {
            if (_cpInSnap) return;
            
            if ((DateTime.UtcNow - _cpCensusAt).TotalSeconds >= 60.0)
            {
                _cpCensusAt = DateTime.UtcNow;
                _log.LogInfo("[OI][census] 读口普查 => Lua.IsTrue=" + _cpIsTrueCalls + " | GetVar=" + _cpGetVarCalls + " | NoteOnce 执行=" + _cpNoteCalls + " 打印=" + _cpNotePrinted + " 配额丢弃=" + _cpDropQuota + " 去重丢弃=" + _cpDropDup + " 不同条件串=" + _cpDistinctConds + " | answer: 入口=" + _cpAaEntry + " !CompanyOn=" + _cpAaNoSwitch + " 有!=" + _cpAaBang + " ==false=" + _cpAaFalse + " 项目0=" + _cpAaNoProj + " 项目>=2=" + _cpAaTwoProj + " want空=" + _cpAaNoWant + " 公司!=1=" + _cpAaNoCompany + " 打印=" + _cpAaPrinted + " | 变量口公司族 命中=" + _cpGaHits + " 可判项目=" + _cpGaWithNode + " | 含项目条件 false形=" + _cpAqFalseForm + " 其他形=" + _cpAqTrueForm
                    + " | T3: 作答=" + _cpT3Answers + " 无归属=" + _cpT3NoPriority + " Gus缺Underhall=" + _cpT3GusUnderhall);
            }
            if (DateTime.UtcNow < _cpNextSnap) return;
            _cpInSnap = true;
            try
            {
                _cpNextSnap = DateTime.UtcNow.AddMilliseconds(120);

                
                try
                {
                    string how0; string nodeId = CurrentNodeArticy(out how0);
                    string cl = ClusterOf(nodeId);
                    if (cl.StartsWith("H-3", StringComparison.Ordinal) || cl.StartsWith("L-1", StringComparison.Ordinal))
                    {
                        
                        
                        
                        if (CompanyOn && _cpWhyLogged.Count < 60 && _cpWhyLogged.Add("oldcut|" + (nodeId ?? "null")))
                            _log.LogInfo("[OI][company-map] 旧路已切断 ⇒ ★ 不再用当前节点归属（★ 节点=" + (nodeId ?? "null") + " ｜ 该节点簇=" + cl + " 仅供参考 ✗）");
                        _cpLastClusterAt = DateTime.UtcNow;
                    }
                    else if (CompanyOn && _cpWhyLogged.Count < 20 && _cpWhyLogged.Add(how0 + "|" + (nodeId ?? "null")))
                    {
                        try { ClearAttribution(); CfgGuard.LogLoadInference(); } catch { }
                        _log.LogInfo("[OI][company-spy] Lua.IsTrue 累计调用=" + _cpIsTrueCalls + " 次 ✓（★ 屏很多却近 0 ⇒ 读口不在这一层 ✗）");
                        _log.LogInfo("[OI][company-map] 判不出簇（诊断）⇒ 节点=" + (nodeId ?? "（null ✗）") + " ｜ 读节点结果=" + how0
                            + "（★ 六 id 之外或读取失败 ⇒ 未知 ✗ ⇒ 安全降级：不记 ✓ 读口放行真值 ✓）");
                    }
                }
                catch { }

                var sb = new System.Text.StringBuilder();
                for (int i = 0; i < SnapNames.Length; i++)
                {
                    bool v = false;
                    try { v = IsTrue(SnapNames[i]); } catch { }
                    sb.Append(v ? '1' : '0');
                }
                string cur = sb.ToString();
                try
                {
                    _cpFlagHighway = cur[0] == '1';
                    _cpFlagRailway = cur[1] == '1';
                    _cpFlagSsc = cur[5] == '1';
                    _cpFlagTaurus = cur[6] == '1';
                    _cpFlagUnderhall = cur[7] == '1';
                }
                catch { }

                
                try
                {
                    for (int k = 0; k < CompanyFlagNames.Length; k++)
                    {
                        int idx = -1;
                        for (int i = 0; i < SnapNames.Length; i++) if (SnapNames[i].EndsWith(CompanyFlagNames[k], StringComparison.Ordinal)) { idx = i; break; }
                        if (idx < 0) continue;
                        bool nowT = cur[idx] == '1';
                        bool wasT = _cpPrevCompany != null && idx < _cpPrevCompany.Length && _cpPrevCompany[idx] == '1';
                        if (!nowT || wasT) continue;
                        string comp = CompanyShort(CompanyFlagNames[k]);
                        
                        
                        
                        
                        
                        
                        
                        
                        
                        
                        
                        
                        
                        
                        
                        
                        
                        
                        string w5 = CompanyOn ? W5Cluster() : null;
                        string useCluster = _cpLastCluster ?? w5;
                        string useSrc = (w5 != null && _cpLastCluster != null)
                            ? (_cpLastCluster == w5 ? "W4-节点表（★ W5 交叉校验一致 ✓）" : "★ 冲突 ✗")
                            : (_cpLastCluster != null ? _cpLastClusterSrc : "W5-项目独占旗（★ 仅交叉校验 ✗ 待独立见证 ✓）");
                        if (w5 != null && _cpLastCluster != null && _cpLastCluster != w5)
                        {
                            useCluster = null;   
                            if (_cpWhyLogged.Count < 120 && _cpWhyLogged.Add("conflict|" + _cpLastCluster + w5))
                                _log.LogInfo("[OI][company-map] ★ 判据冲突 ✗ ⇒ W4=" + _cpLastCluster + " ／ W5=" + w5
                                    + " ⇒ **本次不记** ✓（★ 同体判定规避：两路独立 ✗ 冲突即不信 ✓）");
                        }
                        if (w5 != null && _cpWhyLogged.Count < 120 && _cpWhyLogged.Add("w5|" + w5))
                            _log.LogInfo("[OI][company-map] 交叉校验 ⇒ W5（项目独占旗）=" + w5
                                + " ｜ W4（节点表）=" + (_cpLastCluster ?? "（无 ✗）") + " ✓（★ 旗真值已读出 ✗ 不是只证明『读到了』✓）");
                        bool pending = useCluster != null;
                        bool fresh = pending;
                        if (fresh)
                        {
                            
                    
                    bool freshAttr = pending;   
                    if (!freshAttr)
                    {
                        if (_cpWhyLogged.Count < 30 && _cpWhyLogged.Add("stale|" + (comp ?? "-")))
                            _log.LogInfo("[OI][company-map] 归属闸门 ⇒ ★ 无【新鲜】菜单归属（★ 距今 >15 秒 ✗）⇒ 本次不记 ✓（★ 宁可未知 ✗ 也不错归 ✓）");
                    }
                    else if (useCluster == "H-3") _cpCompanyH3 = comp; else _cpCompanyL1 = comp;
                            _log.LogInfo("[OI][company-map] " + DateTime.Now.ToString("HH:mm:ss.fff")
                                + " 记下 ⇒ " + useCluster + " 选的是 " + comp + " ✓（★ 归属依据=" + useSrc + " ✓ —— ★ 真实来源标签 ✗ 不再写死 ✓）");
                    
                    
                    
                    _log.LogInfo("[OI][company-map] 归属消费 ⇒ ★ 已用掉并清掉 ✓（★ 簇=" + useCluster + " ｜ 来源=" + useSrc + " ✓）");
                    _cpLastCluster = null; _cpLastClusterAt = default(DateTime); _cpLastClusterSrc = "（已消费 ✗）";
                        }
                        else
                        {
                            _log.LogInfo("[OI][company-map] " + DateTime.Now.ToString("HH:mm:ss.fff")
                                + " " + comp + " 变真 ✓ 但【判不出簇】✗ ⇒ 未记（★ 读口因此会放行真值 ✓ 不会误改 ✗）");
                        }
                    }
                }
                catch { }
                _cpPrevCompany = cur;

                if (cur.IndexOf('1') < 0) return;            
                if (_cpSnapLines >= 60) return;
                if (_cpSnapPrev == null)
                {
                    _cpSnapLines++;
                                        
                    
                    
                    try
                    {
                        bool prevSnap2 = _cpInSnap; _cpInSnap = true;
                        try
                        {
                            bool hw2 = CfgGuard.FlagBool("BaseGame.Turn01_InT_Investment_Highway");
                            bool rw2 = CfgGuard.FlagBool("BaseGame.Turn01_InT_Investment_Railway");
                            if (hw2 ^ rw2)
                            {
                                _cpLastCluster = hw2 ? "H-3" : "L-1";
                                _cpLastClusterSrc = "W3-单项目";
                                _cpLastClusterAt = DateTime.UtcNow;
                                _log.LogInfo("[OI][cp-write] 写入者=W3-单项目 ｜ 值=" + _cpLastCluster + " ｜ Highway=" + (hw2 ? "true" : "false") + " Railway=" + (rw2 ? "true" : "false") + " ｜ 时间=" + DateTime.Now.ToString("HH:mm:ss.fff"));
                                if (_cpWhyLogged.Count < 50 && _cpWhyLogged.Add("single|" + _cpLastCluster))
                                    _log.LogInfo("[OI][company-spy] Lua.IsTrue 累计（★ 此刻值 ✗ 非最终 ✓）=" + _cpIsTrueCalls + " 次");
                                    _log.LogInfo("[OI][company-map] 归属依据=单项目在跑 ⇒ 确定（★ 项目=" + _cpLastCluster + " ✓ · Highway=" + (hw2 ? "true" : "false") + " Railway=" + (rw2 ? "true" : "false") + " ✓）");
                            }
                            else if (hw2 && rw2 && _cpWhyLogged.Count < 50 && _cpWhyLogged.Add("both"))
                                _log.LogInfo("[OI][company-map] 归属依据 ⇒ ★ 两个项目都在跑 ⇒ 判不出 ⇒ 不记 ✓（★ 安全降级保留 ✓）");
                        }
                        finally { _cpInSnap = prevSnap2; }
                    }
                    catch { }                    _log.LogInfo("[OI][company-snap] " + DateTime.Now.ToString("HH:mm:ss.fff")
                        + " 起始快照 ⇒ " + SnapText(cur) + " ｜ 当前簇=" + (_cpLastCluster ?? "未知 ✗"));
                }
                else if (cur != _cpSnapPrev)
                {
                    _cpSnapLines++;
                    _log.LogInfo("[OI][company-snap] " + DateTime.Now.ToString("HH:mm:ss.fff")
                        + " 变化 ⇒ " + SnapText(cur) + " ｜ 当前簇=" + (_cpLastCluster ?? "未知 ✗")
                        + " ｜ 变化项=" + SnapDiff(_cpSnapPrev, cur));
                }
                _cpSnapPrev = cur;
            }
            catch { }
            finally { _cpInSnap = false; }
        }

        private static readonly HashSet<string> _cpWhyLogged = new HashSet<string>(StringComparer.Ordinal);
        private static string _cpLastCluster;
        
        private static string _cpLastClusterSrc = "（无 ✗）";
        private static DateTime _cpLastClusterAt;
        private static string _cpPrevCompany;

        private static string SnapText(string bits)
        {
            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < SnapNames.Length && i < bits.Length; i++)
            {
                if (i > 0) sb.Append(' ');
                sb.Append(Short(SnapNames[i])).Append('=').Append(bits[i] == '1' ? "真 ✓" : "假 ✗");
            }
            return sb.ToString();
        }

        private static string SnapDiff(string a, string b)
        {
            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < SnapNames.Length && i < a.Length && i < b.Length; i++)
            {
                if (a[i] == b[i]) continue;
                if (sb.Length > 0) sb.Append(" ｜ ");
                sb.Append(Short(SnapNames[i])).Append(' ').Append(a[i] == '1' ? "真→假" : "假→真");
            }
            return sb.Length == 0 ? "（无 ✗）" : sb.ToString();
        }

        
        
        internal static void ArchiveLogForMe(string why)
        {
            try
            {
                string dir = null;
                try { if (EconomyConfig.LogArchiveDir != null) dir = EconomyConfig.LogArchiveDir.Value; } catch { }
                if (string.IsNullOrEmpty(dir)) { _log.LogInfo("[OI][log] 归档目录为空 ⇒ 跳过 ✓（cfg [Diag] LogArchiveDir ✓）"); return; }
                string asm = typeof(EconomyPatches).Assembly.Location;            
                string pluginsDir = System.IO.Path.GetDirectoryName(asm);
                string bepRoot = System.IO.Path.GetDirectoryName(pluginsDir);
                string logPath = System.IO.Path.Combine(bepRoot ?? ".", "LogOutput.log");
                if (!System.IO.File.Exists(logPath)) { _log.LogWarning("[OI][log] 找不到 " + logPath + " ⇒ 跳过 ✗"); return; }
                if (!System.IO.Directory.Exists(dir)) System.IO.Directory.CreateDirectory(dir);
                string stamp = DateTime.Now.ToString("yyyyMMdd-HHmmss");
                string dst = System.IO.Path.Combine(dir, "LogOutput-" + stamp + "-" + why + ".log");
                System.IO.File.Copy(logPath, dst, true);
                var files = new List<string>(System.IO.Directory.GetFiles(dir, "LogOutput-*.log"));
                files.Sort(StringComparer.Ordinal);
                while (files.Count > 12) { try { System.IO.File.Delete(files[0]); } catch { } files.RemoveAt(0); }
                _log.LogInfo("[OI][log] 已归档 ⇒ " + dst + "（★ 现留 " + files.Count + " 份 ✓）");
            }
            catch (Exception e) { try { _log.LogWarning("[OI][log] 归档失败（已吞）：" + e.GetType().Name); } catch { } }
        }

        

        
        private static readonly string[] ProjectTokens = new string[]
        {
            "Turn01_InT_Investment_Highway", "Turn01_InT_Investment_Railway",
            "Journal_Turn01_PickedHighway", "Journal_Turn01_PickedRailway",
        };

        private static readonly HashSet<string> _cpProjSeen = new HashSet<string>(StringComparer.Ordinal);
        private static int _cpProjLines;

        private static bool ProjectHit(string s)
        {
            if (string.IsNullOrEmpty(s)) return false;
            for (int i = 0; i < ProjectTokens.Length; i++)
                if (s.IndexOf(ProjectTokens[i], StringComparison.Ordinal) >= 0) return true;
            return false;
        }

        
        private static void NoteProjectInput(string cond)
        {
            if (_cpProjLines >= 80) return;
            if (!_cpProjSeen.Add("IN|" + cond)) return;
            _cpProjLines++;
            _log.LogInfo("[OI][company-spy-p] " + DateTime.Now.ToString("HH:mm:ss.fff")
                + " 求值（项目旗）⇒ " + Short(cond));
        }

        
        private static void Post_SpyIsTrue(string luaCondition, bool __result)
        {
            try
            {
                if (!ProjectHit(luaCondition)) return;
                if (_cpProjLines >= 80) return;
                if (!_cpProjSeen.Add("R|" + luaCondition + "|" + (__result ? "T" : "F"))) return;
                _cpProjLines++;
                _log.LogInfo("[OI][company-spy-p] " + DateTime.Now.ToString("HH:mm:ss.fff")
                    + " 结果=" + (__result ? "真 ✓" : "假 ✗") + " ⇐ " + Short(luaCondition));
            }
            catch { }
        }

        
        private static string T3QuestNameOverride(string quest)
        {
            if (!T3On || !EconomyConfig.T3ForceUnderhallOn || string.IsNullOrEmpty(quest)) return quest;
            if (quest.IndexOf("Turn02_DidntFulfillTuskBribeDemand", StringComparison.Ordinal) >= 0)
                return quest.Replace("Turn02_DidntFulfillTuskBribeDemand", "Turn02_FulfilledTuskBribeDemand");
            if (quest.IndexOf("Turn02_ConstructionSSC", StringComparison.Ordinal) >= 0)
                return quest.Replace("Turn02_ConstructionSSC", "Turn02_ConstructionUnderhall");
            return quest;
        }

        
        private static bool? T3QuestConditionOverride(string quest)
        {
            if (!T3On || !EconomyConfig.T3ForceUnderhallOn || string.IsNullOrEmpty(quest)) return null;
            if (quest.IndexOf("Turn02_DidntFulfillTuskBribeDemand", StringComparison.Ordinal) >= 0
                || quest.IndexOf("Turn02_ConstructionSSC", StringComparison.Ordinal) >= 0)
                return false;
            if (quest.IndexOf("Turn02_FulfilledTuskBribeDemand", StringComparison.Ordinal) >= 0
                || quest.IndexOf("Turn02_ConstructionUnderhall", StringComparison.Ordinal) >= 0)
                return true;
            return null;
        }

        private static bool Pre_SpyGetQuestField(ref string quest, string field)
        {
            try
            {
                string overridden = T3QuestNameOverride(quest);
                if (string.Equals(overridden, quest, StringComparison.Ordinal)) return true;
                NoteOnce("T3 GetQuestField 重定向", quest,
                    "（→ " + Short(overridden) + " ｜ field=" + field + " ｜ 只改读取名 · 不写任务状态 ✓）");
                quest = overridden;
            }
            catch { }
            return true;
        }

        private static int T3ListCount(object list)
        {
            try
            {
                if (list == null) return -1;
                var p = list.GetType().GetProperty("Count");
                if (p == null) return -1;
                return Convert.ToInt32(p.GetValue(list, null));
            }
            catch { return -1; }
        }

        private static object T3ListItem(object list, int i)
        {
            try
            {
                if (list == null) return null;
                var p = list.GetType().GetProperty("Item");
                if (p == null) return null;
                return p.GetValue(list, new object[] { i });
            }
            catch { return null; }
        }

        
        
        private static object FindConversation(object db, int convId)
        {
            try
            {
                object convs = Reflect.Prop(db, "conversations");
                int n = T3ListCount(convs);
                for (int i = 0; i < n; i++)
                {
                    object c = T3ListItem(convs, i);
                    if (c == null) continue;
                    if (Reflect.ToInt(Reflect.Prop(c, "id")) == convId) return c;
                }
            }
            catch { }
            return null;
        }

        private static List<object> DatabaseCandidates()
        {
            var dbs = new List<object>();
            try
            {
                var dmT = ResolveTypeSimple("DialogueManager");
                if (dmT == null) return dbs;
                object master = Reflect.Static(dmT, "masterDatabase");
                if (master != null) dbs.Add(master);
                object dmgr = Reflect.Static(dmT, "databaseManager");
                if (dmgr != null)
                {
                    object def = Reflect.Prop(dmgr, "defaultDatabase");
                    if (def != null) dbs.Add(def);
                    object loaded = Reflect.Prop(dmgr, "loadedDatabases");
                    int ln = T3ListCount(loaded);
                    for (int i = 0; i < ln && i < 20; i++)
                    {
                        object one = T3ListItem(loaded, i);
                        if (one != null) dbs.Add(one);
                    }
                }
                object model = Reflect.Static(dmT, "conversationModel");
                object mdb = model == null ? null : Reflect.Prop(model, "m_database");
                if (mdb != null) dbs.Add(mdb);
            }
            catch { }
            return dbs;
        }

        private static object FindConversationAny(int convId)
        {
            foreach (var db in DatabaseCandidates())
            {
                object conv = FindConversation(db, convId);
                if (conv != null) return conv;
            }
            return null;
        }

        
        private static void ApplyT3EntryConditionOverride(object de)
        {
            try
            {
                if (!T3On || de == null) return;
                
                if (!string.Equals(T3Priority(), T3CompanyPriority.Underhall, StringComparison.Ordinal)) return;
                int convId = Reflect.ToInt(Reflect.Prop(de, "conversationID"));
                if (convId == 0) return;
                if (_cpT3EntryOverrideConvs.Contains(convId)) return;
                object conv = FindConversationAny(convId);
                if (conv == null)
                {
                    _log.LogInfo("[OI][t3] 条目条件覆盖检查 ⇒ conv=" + convId + " 找不到 conversation ✗");
                    return;
                }
                string convTitle = Reflect.Str(Reflect.Prop(conv, "Title"));
                if (!T3CompanyPriority.IsT3Conversation(convTitle))
                {
                    _log.LogInfo("[OI][t3] 条目条件覆盖检查 ⇒ conv=" + convId
                        + " title=" + (convTitle ?? "（null ✗）") + " 不在 T3 白名单 ✗");
                    return;
                }
                object entries = FieldOf(conv, "dialogueEntries");
                if (entries == null) entries = Reflect.Prop(conv, "dialogueEntries");
                int n = T3ListCount(entries);
                if (n <= 0)
                {
                    _log.LogInfo("[OI][t3] 条目条件覆盖检查 ⇒ conv=" + convId + " entries=" + n + " ✗");
                    return;
                }
                _cpT3EntryOverrideConvs.Add(convId);
                int changed = 0;
                for (int i = 0; i < n; i++)
                {
                    object e = T3ListItem(entries, i);
                    if (e == null) continue;
                    string cond = Reflect.Str(Reflect.Prop(e, "conditionsString"));
                    if (string.IsNullOrEmpty(cond)) continue;
                    bool hasSsc = cond.IndexOf("Turn02_InT_Corporation_SordishState", StringComparison.Ordinal) >= 0;
                    bool hasTaurus = cond.IndexOf("Turn02_InT_Corporation_TaurusHolding", StringComparison.Ordinal) >= 0;
                    bool hasUnder = cond.IndexOf("Turn02_InT_Corporation_UnderhallConstruction", StringComparison.Ordinal) >= 0;
                    if (!hasSsc && !hasTaurus && !hasUnder) continue;
                    bool negative = cond.IndexOf("== false", StringComparison.Ordinal) >= 0
                                 || cond.TrimStart().StartsWith("!", StringComparison.Ordinal);
                    bool want = hasUnder; 
                    if (hasSsc || hasTaurus) want = !negative ? false : true;
                    else if (hasUnder) want = !negative ? true : false;
                    string replacement = want ? "true" : "false";
                    bool ok = Reflect.SetPropOrField(e, "conditionsString", replacement);
                    if (ok) changed++;
                    _log.LogInfo("[OI][t3] 条目条件覆盖 ⇒ conv=" + convId
                        + " entryId=" + (Reflect.Str(Reflect.Prop(e, "id")) ?? "?")
                        + " ｜ 旧=" + Short(cond) + " ｜ 新=" + replacement
                        + " ｜ 设置=" + (ok ? "成功 ✓" : "失败 ✗"));
                }
                _log.LogInfo("[OI][t3] 条目条件覆盖完成 ⇒ conv=" + convId
                    + " title=" + convTitle + " entries=" + n + " changed=" + changed);
            }
            catch (Exception e) { _log.LogWarning("[OI][t3] 条目条件覆盖异常：" + e.GetType().Name + " " + e.Message); }
        }

        private static object ResolveDialogueEntry(int convId, int dialogueId)
        {
            try
            {
                object conv = FindConversationAny(convId);
                if (conv == null) return null;
                object entry = Reflect.Call1(conv, "GetDialogueEntry", dialogueId);
                return entry;
            }
            catch { return null; }
        }

        private static void LogEntrySummary(string tag, object entry)
        {
            try
            {
                if (entry == null) { _log.LogInfo("[OI][t3][diag] " + tag + " ⇒ （null ✗）"); return; }
                string id = Reflect.Str(Reflect.Prop(entry, "id"));
                string title = Reflect.Str(Reflect.Prop(entry, "Title"));
                string cond = Reflect.Str(Reflect.Prop(entry, "conditionsString"));
                int n = T3ListCount(Reflect.Prop(entry, "outgoingLinks"));
                _log.LogInfo("[OI][t3][diag] " + tag + " ⇒ id=" + (id ?? "?")
                    + " ｜ title=" + (title ?? "（空）")
                    + " ｜ 条件=" + Short(cond)
                    + " ｜ links=" + n);
            }
            catch { }
        }

        
        private static void LogOutgoingLinks(object de, string id)
        {
            try
            {
                if (de == null || _cpT3LinkSeen.Count >= 80) return;
                object links = Reflect.Prop(de, "outgoingLinks");
                int n = T3ListCount(links);
                string key = (id ?? "") + "|" + n;
                if (!_cpT3LinkSeen.Add(key)) return;
                if (n <= 0)
                {
                    _log.LogInfo("[OI][t3][diag] links ⇒ entry=" + (id ?? "?") + " count=0");
                    return;
                }
                var sb = new System.Text.StringBuilder();
                int convId = Reflect.ToInt(Reflect.Prop(de, "conversationID"));
                for (int i = 0; i < n && i < 20; i++)
                {
                    object link = T3ListItem(links, i);
                    if (link == null) continue;
                    int dest = Reflect.ToInt(Reflect.Prop(link, "destinationDialogueID"));
                    if (sb.Length > 0) sb.Append(" ; ");
                    sb.Append("origin=").Append(Reflect.ToInt(Reflect.Prop(link, "originDialogueID")))
                      .Append("→dest=").Append(dest)
                      .Append(" p=").Append(Reflect.Str(Reflect.Prop(link, "priority")));
                    
                    LogEntrySummary("links.dest[" + (id ?? "?") + "→" + dest + " conv=" + convId + "]",
                        ResolveDialogueEntry(convId, dest));
                }
                _log.LogInfo("[OI][t3][diag] links ⇒ entry=" + (id ?? "?") + " count=" + n + " ｜ " + sb);
            }
            catch { }
        }

        private static readonly HashSet<int> _cpT3ProbeConvs = new HashSet<int>();
        private static readonly HashSet<int> _cpT3EntryOverrideConvs = new HashSet<int>();

        private static object FieldOf(object obj, string name)
        {
            try
            {
                if (obj == null) return null;
                var f = obj.GetType().GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                return f == null ? null : f.GetValue(obj);
            }
            catch { return null; }
        }

        private static string EntryTitle(object de)
        {
            return de == null ? null : Reflect.Str(Reflect.Prop(de, "Title"));
        }

        private static string EntryText(object de)
        {
            return de == null ? null : Reflect.Str(Reflect.Prop(de, "DialogueText"));
        }

        
        private static void RunT3Probe(object subtitle)
        {
            if (!T3On || !EconomyConfig.T3ProbeOn) return;
            var sb = new System.Text.StringBuilder();
            try
            {
                object de = subtitle == null ? null : Reflect.Prop(subtitle, "dialogueEntry");
                int convId = Reflect.ToInt(de == null ? 0 : Reflect.Prop(de, "conversationID"));
                
                if (!_cpT3ProbeConvs.Add(convId)) return;
                string title; string howTitle;
                title = CurrentConversationTitle(out howTitle);
                sb.AppendLine("# T3 Graph Probe");
                sb.AppendLine();
                sb.AppendLine("- 时间：" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                sb.AppendLine("- conversationID：" + convId);
                sb.AppendLine("- conversationTitle：" + (title ?? "（null ✗）") + "（" + howTitle + "）");
                sb.AppendLine("- 当前 entryId：" + (de == null ? "null" : Reflect.Str(Reflect.Prop(de, "id"))));
                sb.AppendLine("- 当前 entryTitle：" + (EntryTitle(de) ?? "（空）"));
                sb.AppendLine("- 当前 conditionsString：" + Short(de == null ? null : Reflect.Str(Reflect.Prop(de, "conditionsString"))));
                sb.AppendLine("- ForceUnderhall：" + EconomyConfig.T3ForceUnderhallOn);
                sb.AppendLine();
                sb.AppendLine("## 关键变量");
                string[] keys = new string[]
                {
                    "GameCondition.Turn02_A_ContractBribe",
                    "BaseGame.Turn01_InT_Investment_Highway",
                    "BaseGame.Turn01_InT_Investment_Railway",
                    "BaseGame.Turn02_InT_Corporation_SordishState",
                    "BaseGame.Turn02_InT_Corporation_TaurusHolding",
                    "BaseGame.Turn02_InT_Corporation_UnderhallConstruction",
                    "BaseGameSupport.Journal_Turn02_ConstructionSSC",
                    "BaseGameSupport.Journal_Turn02_ConstructionTaurus",
                    "BaseGameSupport.Journal_Turn02_ConstructionUnderhall",
                };
                foreach (var k in keys) sb.AppendLine("- `" + k + "` = " + (IsTrue(k) ? "true" : "false"));
                sb.AppendLine("- T3Priority = " + (T3Priority() ?? "null"));
                sb.AppendLine();

                var dmT = ResolveTypeSimple("DialogueManager");
                if (dmT == null)
                {
                    sb.AppendLine("## 数据库\n\nDialogueManager 类型未找到。");
                }
                else
                {
                    var labels = new List<string>();
                    var dbs = new List<object>();
                    object master = Reflect.Static(dmT, "masterDatabase");
                    if (master != null) { labels.Add("masterDatabase"); dbs.Add(master); }
                    object dmgr = Reflect.Static(dmT, "databaseManager");
                    if (dmgr != null)
                    {
                        object def = Reflect.Prop(dmgr, "defaultDatabase");
                        if (def != null) { labels.Add("databaseManager.defaultDatabase"); dbs.Add(def); }
                        object loaded = Reflect.Prop(dmgr, "loadedDatabases");
                        int ln = T3ListCount(loaded);
                        for (int i = 0; i < ln && i < 20; i++)
                        {
                            object one = T3ListItem(loaded, i);
                            if (one != null) { labels.Add("loadedDatabases[" + i + "]"); dbs.Add(one); }
                        }
                    }
                    object model = Reflect.Static(dmT, "conversationModel");
                    object mdb = model == null ? null : Reflect.Prop(model, "m_database");
                    if (mdb != null) { labels.Add("conversationModel.m_database"); dbs.Add(mdb); }

                    sb.AppendLine("## 数据库清单（" + dbs.Count + "）");
                    sb.AppendLine();
                    for (int di = 0; di < dbs.Count; di++)
                    {
                        object db = dbs[di];
                        string dbName = Reflect.Str(Reflect.Prop(db, "Name")) ?? db.GetType().Name;
                        object convs = Reflect.Prop(db, "conversations");
                        int cc = T3ListCount(convs);
                        object conv = FindConversation(db, convId);
                        bool found = conv != null;
                        sb.AppendLine("### " + (di + 1) + ". " + labels[di] + " / " + dbName);
                        sb.AppendLine("- type=" + db.GetType().FullName + " ｜ conversations=" + cc + " ｜ contains conv=" + convId + " ⇒ " + found);
                        if (!found) continue;
                        object entries = FieldOf(conv, "dialogueEntries");
                        if (entries == null) entries = Reflect.Prop(conv, "dialogueEntries");
                        int en = T3ListCount(entries);
                        sb.AppendLine("- conversation.Title=" + (Reflect.Str(Reflect.Prop(conv, "Title")) ?? "（空）") + " ｜ dialogueEntries=" + en);
                        for (int ei = 0; ei < en && ei < 800; ei++)
                        {
                            object e = T3ListItem(entries, ei);
                            if (e == null) continue;
                            string eid = Reflect.Str(Reflect.Prop(e, "id"));
                            string et = EntryTitle(e);
                            string ec = Reflect.Str(Reflect.Prop(e, "conditionsString"));
                            string etxt = EntryText(e);
                            sb.AppendLine("- entry " + (eid ?? "?") + " ｜ title=" + (et ?? "（空）")
                                + " ｜ conditions=" + Short(ec) + " ｜ text=" + Short(etxt));
                            object links = Reflect.Prop(e, "outgoingLinks");
                            int len = T3ListCount(links);
                            for (int li = 0; li < len && li < 30; li++)
                            {
                                object link = T3ListItem(links, li);
                                if (link == null) continue;
                                sb.AppendLine("  - link " + Reflect.ToInt(Reflect.Prop(link, "originDialogueID"))
                                    + " → " + Reflect.ToInt(Reflect.Prop(link, "destinationDialogueID"))
                                    + " p=" + Reflect.Str(Reflect.Prop(link, "priority")));
                            }
                        }
                    }
                }
                string dir = @"C:\Users\dell\Desktop\Suzerain\_verify\artifacts";
                System.IO.Directory.CreateDirectory(dir);
                string path = System.IO.Path.Combine(dir, "t3-probe-" + DateTime.Now.ToString("yyyyMMdd-HHmmss") + "-conv" + convId + ".md");
                System.IO.File.WriteAllText(path, sb.ToString(), new System.Text.UTF8Encoding(false));
                _log.LogInfo("[OI][t3][probe] 报告=" + path + " ｜ 行数=" + sb.ToString().Split('\n').Length);
            }
            catch (Exception e)
            {
                _log.LogWarning("[OI][t3][probe] 失败：" + e.GetType().Name + " " + e.Message);
            }
        }

        
        private static void Post_ConversationHandlerLine(object subtitle)
        {
            try
            {
                if (!T3On || _cpT3DialogueSeen.Count >= 60) return;
                object de = subtitle == null ? null : Reflect.Prop(subtitle, "dialogueEntry");
                ApplyT3EntryConditionOverride(de);
                RunT3Probe(subtitle);
                int convId = Reflect.ToInt(de == null ? 0 : Reflect.Prop(de, "conversationID"));
                string convHow; string convTitle = CurrentConversationTitle(out convHow);
                string id = de == null ? null : Reflect.Str(Reflect.Prop(de, "id"));
                string title = de == null ? null : Reflect.Str(Reflect.Prop(de, "Title"));
                string text = de == null ? null : Reflect.Str(Reflect.Prop(de, "DialogueText"));
                string cond = de == null ? null : Reflect.Str(Reflect.Prop(de, "conditionsString"));
                string key = (id ?? "") + "|" + (title ?? "") + "|" + (text ?? "") + "|" + (cond ?? "");
                if (_cpT3DialogueSeen.Add(key))
                    _log.LogInfo("[OI][t3][diag] 显示条目 ⇒ conv=" + convId
                        + " title=" + (convTitle ?? "（null ✗）")
                        + " ｜ entryId=" + (id ?? "（null ✗）")
                        + " ｜ entryTitle=" + (title ?? "（空）")
                        + " ｜ 条件=" + Short(cond)
                        + " ｜ 文本=" + Short(text));
                LogOutgoingLinks(de, id);
            }
            catch { }
        }

        
        private static void Post_ConversationModelUpdateResponses(object __instance, object state)
        {
            try
            {
                if (!T3On || _cpT3DialogueSeen.Count >= 40) return;
                string conv = Reflect.Str(Reflect.Prop(__instance, "conversationTitle"));
                object sub = Reflect.Prop(state, "subtitle");
                object de = sub == null ? null : Reflect.Prop(sub, "dialogueEntry");
                string id = de == null ? null : Reflect.Str(Reflect.Prop(de, "id"));
                string title = de == null ? null : Reflect.Str(Reflect.Prop(de, "Title"));
                string text = de == null ? null : Reflect.Str(Reflect.Prop(de, "DialogueText"));
                string cond = de == null ? null : Reflect.Str(Reflect.Prop(de, "conditionsString"));
                string key = (conv ?? "") + "|" + (id ?? "") + "|" + (title ?? "")
                           + "|" + (text ?? "") + "|" + (cond ?? "");
                if (!_cpT3DialogueSeen.Add(key)) return;
                _log.LogInfo("[OI][t3][diag] 对话条目 ⇒ 对话=" + (conv ?? "（null ✗）")
                    + " ｜ entryId=" + (id ?? "（null ✗）")
                    + " ｜ entryTitle=" + (title ?? "（空）")
                    + " ｜ 条件=" + Short(cond)
                    + " ｜ 文本=" + Short(text));
            }
            catch { }
        }

        private static void Post_QuestConditionIsTrue(object __instance, ref bool __result)
        {
            try
            {
                string quest = Reflect.Str(Reflect.Prop(__instance, "questName"));
                bool? forced = T3QuestConditionOverride(quest);
                if (!forced.HasValue) return;
                __result = forced.Value;
                NoteOnce("T3 任务条件覆盖", quest,
                    "（→ " + (forced.Value ? "真 ✓" : "假 ✗") + " ｜ ForceUnderhall=true）");
            }
            catch { }
        }

        
        private static bool Pre_SpyGetVariableLuaValue(object field)
        {
            try
            {
                if (field == null || !T3On) return true;
                string name = Reflect.Str(Reflect.Prop(field, "Name"));
                if (!IsCompanyReadFlag(name)) return true;
                string redirected = T3RedirectCompany(name);
                if (string.Equals(redirected, name, StringComparison.Ordinal)) return true;
                bool set = Reflect.SetPropOrField(field, "Name", redirected);
                NoteOnce("T3 LuaValue 重定向", name,
                    "（→ " + Short(redirected) + " ｜ 优先级=" + (T3Priority() ?? "（无 ✗）")
                    + " ｜ 设置=" + (set ? "成功 ✓" : "失败 ✗") + "）");
            }
            catch { }
            return true;
        }

        
        private static bool Pre_SpyGetVariable(ref string variable)
        {
            string original = variable;
            try
            {
                
                
                
                if (T3On && IsCompanyReadFlag(original))
                {
                    string redirected = T3RedirectCompany(original);
                    if (!string.Equals(redirected, original, StringComparison.Ordinal))
                    {
                        variable = redirected;
                        NoteOnce("T3 GetVar 重定向", original,
                            "（→ " + Short(redirected) + " ｜ 优先级=" + (T3Priority() ?? "（无 ✗）")
                            + " ｜ 只改读取名 · 不写变量 ✓）");
                    }
                }
                if (TokenHit(original))
                {
                    _cpGetVarCalls++;
                    bool isCompany = false;
                    for (int i = 0; i < CompanyFlagNames.Length; i++)
                    {
                        if (variable != null && variable.IndexOf(CompanyFlagNames[i], StringComparison.Ordinal) >= 0) { isCompany = true; break; }
                    }
                    if (isCompany)
                    {
                        
                        
                        _cpGaHits++;
                        string howN; string nodeId = CurrentNodeArticy(out howN);
                        if (!string.IsNullOrEmpty(nodeId)) _cpGaWithNode++;
                        NoteOnce("GetVar 公司族", variable,
                            "（★ 不作答 · return true 不变 ✓ ｜ 节点=" + (nodeId ?? "（无 ✗）") + " ｜ 读法=" + howN
                            + " ｜ 调用方=" + CallerHint() + " ✓）");
                        
                        if (T3On)
                        {
                            string t3Title;
                            if (T3Scope(out t3Title))
                            {
                                string t3Priority = T3CompanyPriority.Choose(_cpCompanyH3, _cpCompanyL1);
                                NoteOnce("T3 GetVar", variable,
                                    "（★ 只记录 · 不改返回值 ✓ ｜ 对话=" + t3Title
                                    + " ｜ 优先级=" + (t3Priority ?? "（无归属 ✗）")
                                    + " ｜ 调用方=" + CallerHint() + "）");
                            }
                        }
                    }
                    else
                    {
                        NoteOnce("DialogueLua.GetVariable 读了", variable,
                            "（★ 通用读取口 ✓ ｜ " + DateTime.Now.ToString("HH:mm:ss.fff") + " ｜ 调用方=" + CallerHint() + "）");
                    }
                }
            }
            catch { }
            return true;
        }

        
        private static string Sig(MethodInfo m)
        {
            try
            {
                var ps = m.GetParameters();
                var sb = new System.Text.StringBuilder();
                sb.Append(m.Name).Append('(');
                for (int i = 0; i < ps.Length; i++)
                {
                    if (i > 0) sb.Append(", ");
                    sb.Append(ps[i].ParameterType.Name);
                }
                sb.Append(')');
                return sb.ToString();
            }
            catch { return m.Name + "(?)"; }
        }

        
        private static string CallerHint()
        {
            try
            {
                var st = new System.Diagnostics.StackTrace(2, false);
                var sb = new System.Text.StringBuilder();
                int added = 0;
                for (int i = 0; i < st.FrameCount && added < 3; i++)
                {
                    var fr = st.GetFrame(i);
                    var md = fr == null ? null : fr.GetMethod();
                    if (md == null) continue;
                    string tn = md.DeclaringType == null ? "?" : md.DeclaringType.Name;
                    if (tn.IndexOf("CompanyProbe", StringComparison.Ordinal) >= 0) continue;
                    if (added > 0) sb.Append(" ← ");
                    sb.Append(tn).Append('.').Append(md.Name);
                    added++;
                }
                return added == 0 ? "（取不到 ✗）" : sb.ToString();
            }
            catch { return "（取不到 ✗）"; }
        }

        

        
        
        private static readonly string[] ClusterH3 = new string[]
        {
            "0x0100000600035067", "0x010000060003506D", "0x0100000600035073",
        };
        private static readonly string[] ClusterL1 = new string[]
        {
            "0x010000060003508E", "0x0100000600035094", "0x010000060003509A",
        };

        private static int _cpWrites;
        private static int _cpDropped;   
        private static string _cpLastCond = "（还没记到 ✗）";

        private static string ClusterOf(string articyId)
        {
            if (string.IsNullOrEmpty(articyId)) return "未知 ✗";
            for (int i = 0; i < ClusterH3.Length; i++) if (string.Equals(articyId, ClusterH3[i], StringComparison.OrdinalIgnoreCase)) return "H-3 ✓";
            for (int i = 0; i < ClusterL1.Length; i++) if (string.Equals(articyId, ClusterL1[i], StringComparison.OrdinalIgnoreCase)) return "L-1 ✓";
            return "未知 ✗";
        }

        
        private static string CurrentNodeArticy(out string how)
        {
            how = "无 ✗";
            try
            {
                var dmT = ResolveTypeSimple("DialogueManager");
                if (dmT == null) { how = "DialogueManager 类型没找到 ✗"; return null; }
                object cs = Reflect.Static(dmT, "CurrentConversationState");   
                if (!Reflect.Usable(cs)) { how = "CurrentConversationState=null ✗"; return null; }
                object sub = Reflect.Prop(cs, "subtitle");
                if (!Reflect.Usable(sub)) { how = "subtitle=null ✗"; return null; }
                object de = Reflect.Prop(sub, "dialogueEntry");
                if (!Reflect.Usable(de)) { how = "dialogueEntry=null ✗"; return null; }
                string id = null;
                try { id = EntryField(de, "Articy Id"); } catch { }
                how = (id == null) ? "节点头里没有 Articy Id ✗" : "有值 ✓";
                return id;
            }
            catch (Exception e) { how = "读异常：" + e.GetType().Name; return null; }
        }

        
        private static string LastCondText() { return _cpLastCond; }

        
        private static void NoteCondition(object data)
        {
            try
            {
                if (!Reflect.Usable(data)) return;
                string id = null;
                string title = null;
                
                
                if (data.GetType().Name.IndexOf("ConditionalInstructionData", StringComparison.Ordinal) >= 0)
                {
                    id = Reflect.Str(Reflect.Prop(data, "Id"));
                    title = Reflect.Str(Reflect.Prop(data, "NameInDatabase"));
                }
                else
                {
                    try { id = EntryField(data, "Articy Id"); } catch { }
                    if (id == null) { try { id = EntryField(data, "articyId"); } catch { } }
                    try { title = EntryField(data, "Title"); } catch { }
                    if (title == null) { try { title = EntryField(data, "title"); } catch { } }
                }
                _cpLastCond = data.GetType().Name
                    + (string.IsNullOrEmpty(id) ? "" : " ｜ id=" + id)
                    + (string.IsNullOrEmpty(title) ? "" : " ｜ " + Short(title));
            }
            catch { }
        }

        private static bool Pre_SpyRun(string luaCode)
        {
            try { if (TokenHit(luaCode)) NoteWrite("Lua.Run", luaCode); }
            catch { }
            return true;
        }

        
        
        
        private static bool Pre_SpySetVariable(string variable)
        {
            try
            {
                if (TokenHit(variable))
                    NoteWrite("DialogueLua.SetVariable", variable + " = （值略 ✓）");
            }
            catch { }
            return true;
        }

        private static bool Pre_SpyEvaluate(object conditionalInstructionData)
        {
            try { NoteCondition(conditionalInstructionData); } catch { }
            return true;
        }

        
        private static void NoteWrite(string api, string what)
        {
            try
            {
                if (_cpWrites >= 60)                        
                {
                    
                    
                    _cpDropped++;
                    if (_cpDropped == 1)
                        _log.LogInfo("[OI][company-spy-w][cap] 上限名=写口 上限值=60 实际写入数=" + _cpWrites
                            + " 丢弃数=" + _cpDropped + " 丢弃原因=硬上限 时点=" + DateTime.Now.ToString("yyyy-MM-dd'T'HH:mm:ss"));
                    
                    
                    
                    EconomyPatches.NoteTruncationForDisplay(
                        "写口", 60, _cpWrites, _cpDropped, "硬上限",
                        "CompanyProbe.cs 的 NoteWrite 上限（_cpWrites >= 60）");
                    return;
                }
                _cpWrites++;
                string how; string id = CurrentNodeArticy(out how);
                _log.LogInfo("[OI][company-spy-w] 写入=" + Short(what)
                    + " ｜ 经由=" + api
                    + " ｜ 一级 当前节点 Articy=" + (id == null ? "（无 ✗：" + how + "）" : id)
                    + " ⇒ 簇=" + ClusterOf(id)
                    + " ｜ 二级 最近条件=" + LastCondText()
                    + " ｜ 三级 第 " + _cpWrites + " 次匹配写入 ✓");
            }
            catch { }
        }
    }
}
