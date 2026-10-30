using System;
using System.Reflection;
using System.Text;
using HarmonyLib;
using Suzerain.PatchBus;

namespace Oikonomia.Probe
{
    
    
    
    
    
    
    
    
    
    
    
    internal static class EconomyProbe
    {
        
        private static readonly string[] RegionLabels = { "Bergia", "Gruni", "Lorren", "Agnland" };
        private static readonly string[][] RegionTerms = {
            new[] { "BaseGame.Situation_Economy_Bergia_EconomicDownturn",
                    "BaseGame.Situation_Economy_Bergia_EconomicStabilisation",
                    "BaseGame.Situation_Economy_Bergia_MajorAgriculturalZone" },
            new[] { "BaseGame.Situation_Economy_Gruni_LaggingBehind",
                    "BaseGame.Situation_Economy_Gruni_MaintainingGrowth",
                    "BaseGame.Situation_Economy_Gruni_LightTowerRegion" },
            new[] { "BaseGame.Situation_Economy_Lorren_RustBelt",
                    "BaseGame.Situation_Economy_Lorren_EconomicStabilisation",
                    "BaseGame.Situation_Economy_Lorren_ProductionAndTradeCenter" },
            new[] { "BaseGame.Situation_Economy_Agnland_LackInvestment",
                    "BaseGame.Situation_Economy_Agnland_EconomicStabilisation",
                    "BaseGame.Situation_Economy_Agnland_MajorFishExport" }
        };
        private static readonly string[][] TaxTerms = {
            new[] { "eff",  "BaseGame.Situation_Economy_TaxEfficientEconomy" },
            new[] { "avoid","BaseGame.Situation_Economy_TaxAvoidance" },
            new[] { "evade","BaseGame.Situation_Economy_TaxEvasion" }
        };
        private static readonly string[][] GrowthTerms = {
            new[] { "emp",  "BaseGame.Situation_Economy_HighEmployment" },
            new[] { "prod", "BaseGame.Situation_Economy_ProductionIncreased" },
            new[] { "small","BaseGame.Situation_Economy_SmallBusiness_Growth" },
            new[] { "jobs", "BaseGame.Situation_Economy_LargeBusiness_CreateJobs" },
            new[] { "invA", "BaseGame.Situation_Economy_FinancialInvestment_Arcasia" },
            new[] { "invU", "BaseGame.Situation_Economy_FinancialInvestment_UC" },
            new[] { "mic",  "BaseGame.Situation_Economy_SordishMilitaryIndustrialComplex" },
            new[] { "recT", "BaseGame.Situation_Economy_RecoveredTrade" }
        };
        private static readonly string[] IndicatorConds = {
            "BaseGame.Economy_Indicator == -2",
            "BaseGame.Economy_Indicator == -1",
            "BaseGame.Economy_Indicator == 0",
            "BaseGame.Economy_Indicator >= 1"
        };

        private static Type _flowType;
        private static Type _luaHelperType;
        private static MethodInfo _isTrue;
        private static MethodInfo _runInstant;
        private static object _flowInstance;
        private static object _economyTextStat;
        private static object _budgetStat;
        private static bool _turnEndSubscribed;
        private static int _snapshotSeq;

        public static void Install(Harmony harmony, Assembly asm, BepInEx.Logging.ManualLogSource log)
        {
            ProbeLog.Mark("[OI] ===== Oikonomia P0.2 探针 v0.1.0 挂载 =====");

            _flowType = Reflect.FindType(asm, "GameFlowManager");
            _luaHelperType = Reflect.FindType(asm, "ArticyLuaHelper");
            var condHelper = Reflect.FindType(asm, "ConditionalInstructionHelper");
            var persistence = Reflect.FindType(asm, "PersistenceManager");
            var textStat = Reflect.FindType(asm, "TemplateHUDTextStat");
            var numStat = Reflect.FindType(asm, "TemplateHUDStat");

            if (_luaHelperType != null)
            {
                _isTrue = Reflect.FindMethod(_luaHelperType, "IsTrue");
                _runInstant = Reflect.FindMethod(_luaHelperType, "RunInstant");
            }

            ProbeLog.Mark("[OI] 类型解析：Flow=" + (_flowType != null)
                + " ArticyLuaHelper=" + (_luaHelperType != null)
                + " IsTrue=" + (_isTrue != null)
                + " RunInstant=" + (_runInstant != null)
                + " CondHelper=" + (condHelper != null)
                + " Persistence=" + (persistence != null)
                + " TemplateHUDTextStat=" + (textStat != null)
                + " TemplateHUDStat=" + (numStat != null));

            int ok = 0, miss = 0;

            
            ok += Add(harmony, _flowType, "EndTurn", nameof(Pre_EndTurn), nameof(Post_EndTurn), ref miss);
            ok += Add(harmony, _flowType, "EndTurnCoroutine", nameof(Pre_EndTurnCoroutine), null, ref miss);

            
            if (condHelper != null)
            {
                var actionEnum = _flowType != null ? _flowType.GetNestedType("GameFlowAction") : null;
                var evalTurn = actionEnum != null
                    ? AccessTools.Method(condHelper, "Evaluate", new[] { actionEnum, typeof(int), typeof(int) })
                    : null;
                if (evalTurn != null)
                {
                    
                    var refusalTurn = SafeResolve.PatchRefusal(evalTurn);
                    if (refusalTurn == null)
                    {
                        try
                        {
                            harmony.Patch(evalTurn,
                                postfix: new HarmonyMethod(typeof(EconomyProbe).GetMethod(nameof(Post_EvalTurnEnd),
                                    BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public)));
                            ok++;
                            ProbeLog.Line("[mount] ✓ ConditionalInstructionHelper.Evaluate(GameFlowAction,int,int)");
                        }
                        catch (Exception e) { miss++; ProbeLog.Error("[mount] ✗ Evaluate(TurnEnd) 失败：" + e.Message); }
                    }
                    else { miss++; ProbeLog.Line("[mount] · Evaluate(TurnEnd) 未挂（分层/体检）：" + refusalTurn); }
                }
                else { miss++; ProbeLog.Line("[mount] 未找到 Evaluate(GameFlowAction,int,int)"); }

                
                var evalData = AccessTools.Method(condHelper, "Evaluate", new[] { Reflect.FindType(asm, "ConditionalInstructionData") });
                if (evalData != null)
                {
                    
                    var refusalData = SafeResolve.PatchRefusal(evalData);
                    if (refusalData == null)
                    {
                        try
                        {
                            harmony.Patch(evalData,
                                postfix: new HarmonyMethod(typeof(EconomyProbe).GetMethod(nameof(Post_EvalData),
                                    BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public)));
                            ok++;
                            ProbeLog.Line("[mount] ✓ ConditionalInstructionHelper.Evaluate(ConditionalInstructionData)");
                        }
                        catch (Exception e) { miss++; ProbeLog.Error("[mount] ✗ Evaluate(data) 失败：" + e.Message); }
                    }
                    else { miss++; ProbeLog.Line("[mount] · Evaluate(data) 未挂（分层/体检）：" + refusalData); }
                }
            }

            
            ok += Add(harmony, persistence, "TurnSave", nameof(Pre_TurnSave), nameof(Post_TurnSave), ref miss);

            
            
            
            var textStatDataType = Reflect.FindType(asm, "HUDTextStatData");
            var numStatDataType = Reflect.FindType(asm, "HUDStatData");
            ok += AddTyped(harmony, textStat, "Setup", new[] { textStatDataType }, null, nameof(Post_TextStatSetup), ref miss);
            ok += AddTyped(harmony, numStat, "Setup", new[] { numStatDataType }, null, nameof(Post_NumStatSetup), ref miss);

            ProbeLog.Mark("[OI] 挂载完成：成功 " + ok + " 个，缺失 " + miss + " 个。");

            try { WriteProbe.Init(asm); } catch (Exception e) { ProbeLog.Error("[WP] 初始化失败：" + e.Message); }
        }

        private static int Add(Harmony harmony, Type type, string method, string prefix, string postfix, ref int miss)
        {
            return AddTyped(harmony, type, method, null, prefix, postfix, ref miss);
        }

        
        private static int AddTyped(Harmony harmony, Type type, string method, Type[] paramTypes,
                                    string prefix, string postfix, ref int miss)
        {
            if (type == null) { miss++; return 0; }
            try
            {
                var target = paramTypes == null
                    ? Reflect.FindMethod(type, method)
                    : AccessTools.Method(type, method, paramTypes);
                if (target == null)
                {
                    miss++;
                    ProbeLog.Line("[mount] 未找到 " + type.Name + "." + method
                        + (paramTypes == null ? "" : "(" + string.Join(",", Array.ConvertAll(paramTypes, p => p == null ? "?" : p.Name)) + ")"));
                    return 0;
                }
                var pre = prefix == null ? null : new HarmonyMethod(
                    typeof(EconomyProbe).GetMethod(prefix, BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public));
                var post = postfix == null ? null : new HarmonyMethod(
                    typeof(EconomyProbe).GetMethod(postfix, BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public));
                
                
                var refusal = SafeResolve.PatchRefusal(target);
                if (refusal != null)
                {
                    miss++;
                    ProbeLog.Line("[mount] · " + type.Name + "." + method + " 未挂（体检）：" + refusal);
                    return 0;
                }
                
                if (string.Equals(type.Name, "TemplateHUDTextStat", StringComparison.Ordinal)
                    && string.Equals(method, "Setup", StringComparison.Ordinal) && postfix != null)
                {
                    PatchBus.SubscribePostfix(harmony, "OI.Probe.TextStat.Setup",
                        target, PatchScope.Any, 50, ctx =>
                        {
                            object arg0 = ctx.Args != null && ctx.Args.Length > 0 ? ctx.Args[0] : null;
                            Post_TextStatSetup(ctx.Instance, arg0);
                        });
                    ProbeLog.Line("[mount] ✓ " + type.Name + "." + method + "（PatchBus）");
                    return 1;
                }
                
                if (string.Equals(method, "EndTurnCoroutine", StringComparison.Ordinal) && pre != null)
                {
                    PatchBus.SubscribePrefixReadOnly(harmony, "OI.Probe.EndTurnCoroutine",
                        target, PatchScope.Any, 50, ctx =>
                        {
                            object arg0 = ctx.Args != null && ctx.Args.Length > 0 ? ctx.Args[0] : null;
                            Pre_EndTurnCoroutine(ctx.Instance, arg0);
                        });
                    if (post != null)
                        PatchBus.SubscribePostfix(harmony, "OI.Probe.EndTurnCoroutine.post",
                            target, PatchScope.Any, 50, ctx => { });
                    ProbeLog.Line("[mount] ✓ " + type.Name + "." + method + "（PatchBus）");
                    return 1;
                }
                harmony.Patch(target, prefix: pre, postfix: post);
                ProbeLog.Line("[mount] ✓ " + type.Name + "." + method
                    + (paramTypes == null ? "" : "(" + string.Join(",", Array.ConvertAll(paramTypes, p => p == null ? "?" : p.Name)) + ")"));
                return 1;
            }
            catch (Exception e)
            {
                miss++;
                ProbeLog.Error("[mount] ✗ " + type.Name + "." + method + " ：" + e.Message);
                return 0;
            }
        }

        

        private static bool IsTrue(string cond)
        {
            try
            {
                if (_isTrue == null) return false;
                var r = _isTrue.Invoke(null, new object[] { cond });
                return r is bool b && b;
            }
            catch (Exception e)
            {
                ProbeLog.Line("[lua] IsTrue 异常：" + cond + " → " + e.Message);
                return false;
            }
        }

        private static void RunInstant(string instruction)
        {
            try
            {
                if (_runInstant == null) { ProbeLog.Error("[lua] RunInstant 不可用"); return; }
                _runInstant.Invoke(null, new object[] { instruction });
            }
            catch (Exception e) { ProbeLog.Error("[lua] RunInstant 异常：" + instruction + " → " + e.Message); }
        }

        

        private static void Snapshot(string where)
        {
            try
            {
                try { WriteProbe.DeferredCheck(IsTrue); } catch { }
                int seq = ++_snapshotSeq;
                var sb = new StringBuilder();
                sb.Append("SNAP#").Append(seq).Append('[').Append(where).Append(']');

                
                sb.Append(" ED");
                for (int i = 0; i < IndicatorConds.Length; i++)
                    sb.Append(IsTrue(IndicatorConds[i]) ? "1" : "0");
                sb.Append("(v-2,v-1,v0,v>=1)");

                
                sb.Append(" | REG");
                for (int r = 0; r < RegionTerms.Length; r++)
                {
                    sb.Append(' ').Append(RegionLabels[r]).Append(':');
                    for (int t = 0; t < 3; t++) sb.Append(IsTrue(RegionTerms[r][t]) ? "1" : "0");
                }

                
                sb.Append(" | TAX");
                for (int i = 0; i < TaxTerms.Length; i++)
                    sb.Append(' ').Append(TaxTerms[i][0]).Append('=').Append(IsTrue(TaxTerms[i][1]) ? "1" : "0");

                
                sb.Append(" | GROW");
                for (int i = 0; i < GrowthTerms.Length; i++)
                    sb.Append(' ').Append(GrowthTerms[i][0]).Append('=').Append(IsTrue(GrowthTerms[i][1]) ? "1" : "0");

                
                if (_economyTextStat != null)
                {
                    var txt = Reflect.Prop(Reflect.Prop(_economyTextStat, "statValueText"), "text");
                    sb.Append(" | HUD经济文本=\"").Append(Reflect.Str(txt) ?? "?").Append('"');
                }
                if (_budgetStat != null)
                {
                    var v = Reflect.Call(_budgetStat, "GetCurrentValue");
                    if (v == null) v = Reflect.Prop(_budgetStat, "currentValue");
                    sb.Append(" | HUD预算=").Append(Reflect.Str(v) ?? "?");
                }

                ProbeLog.Mark(sb.ToString());

                
                if (ProbeLog.TestWrite)
                {
                    string marker = "BaseGame.OI_ProbeWrite = " + seq;
                    RunInstant(marker);
                    bool readBack = IsTrue("BaseGame.OI_ProbeWrite == " + seq);
                    ProbeLog.Mark("[H2] 写入实验：RunInstant(\"" + marker + "\") → 回读 " + (readBack ? "成功 ✓" : "失败 ✗"));
                }
            }
            catch (Exception e) { ProbeLog.Error("[snap] 异常：" + e.Message); }
        }

        

        private static void Pre_EndTurn(object __instance)
        {
            try
            {
                _flowInstance = __instance;
                int turn = Reflect.ToInt(Reflect.PropOrField(__instance, "currentTurnNo", "currentTurnNo"));
                ProbeLog.Mark("[EVT] EndTurn.pre  回合=" + turn);
                TrySubscribeTurnEnd(__instance);
            }
            catch { }
        }

        private static void Post_EndTurn(object __instance)
        {
            try { Snapshot("EndTurn.post"); } catch { }
            try { WriteProbe.RunAtTurnBoundary(IsTrue, RunInstant); } catch (Exception e) { ProbeLog.Error("[WP] 实验异常：" + e.Message); }
        }

        private static void Pre_EndTurnCoroutine(object __instance, object __0)
        {
            try { ProbeLog.Mark("[EVT] EndTurnCoroutine 迭代器创建（参数 nextTurnNo=" + Reflect.Str(__0) + "）"); }
            catch { }
        }

        private static void Post_EvalTurnEnd(object __0, object __1, object __2)
        {
            try
            {
                string action = Reflect.Str(__0) ?? "?";
                ProbeLog.Mark("[EVT] Evaluate.post  动作=" + action + " turn=" + Reflect.Str(__1) + " step=" + Reflect.Str(__2));
                if (action.IndexOf("TurnEnd", StringComparison.OrdinalIgnoreCase) >= 0)
                    Snapshot("Evaluate.TurnEnd.post");
            }
            catch { }
        }

        private static void Post_EvalData(object __0)
        {
            try
            {
                string name = Reflect.Str(Reflect.Prop(__0, "NameInDatabase"));
                if (!string.IsNullOrEmpty(name) && name.IndexOf("Economy", StringComparison.OrdinalIgnoreCase) >= 0)
                    ProbeLog.Line("[EVT] Evaluate(data).post  " + name);
            }
            catch { }
        }

        private static void Pre_TurnSave()
        {
            try { ProbeLog.Mark("[EVT] TurnSave.pre"); Snapshot("TurnSave.pre"); } catch { }
        }

        private static void Post_TurnSave()
        {
            try { ProbeLog.Mark("[EVT] TurnSave.post"); } catch { }
        }

        

        private static void Post_TextStatSetup(object __instance, object __0)
        {
            try
            {
                string name = Reflect.Str(Reflect.Prop(__0, "NameInDatabase"));
                var txt = Reflect.Str(Reflect.Prop(Reflect.Prop(__instance, "statValueText"), "text"));
                ProbeLog.Line("[hud] TemplateHUDTextStat.Setup: " + name + " → \"" + txt + "\"");
                if (name != null && name.IndexOf("Economy", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    _economyTextStat = __instance;
                    ProbeLog.Mark("[hud] ★ 已捕获经济文本统计实例：" + name);
                }
            }
            catch { }
        }

        private static void Post_NumStatSetup(object __instance, object __0)
        {
            try
            {
                string name = Reflect.Str(Reflect.Prop(__0, "NameInDatabase"));
                var v = Reflect.Call(__instance, "GetCurrentValue");
                if (v == null) v = Reflect.Prop(__instance, "currentValue");
                ProbeLog.Line("[hud] TemplateHUDStat.Setup: " + name + " = " + Reflect.Str(v));
                if (name != null && (name.IndexOf("Budget", StringComparison.OrdinalIgnoreCase) >= 0
                                     || name.IndexOf("Authority", StringComparison.OrdinalIgnoreCase) >= 0))
                {
                    _budgetStat = __instance;
                    ProbeLog.Mark("[hud] ★ 已捕获数值统计实例：" + name + " = " + Reflect.Str(v));
                }
            }
            catch { }
        }

        

        private static void TrySubscribeTurnEnd(object flowInstance)
        {
            if (_turnEndSubscribed || flowInstance == null) return;
            _turnEndSubscribed = true;
            try
            {
                var nested = _flowType.GetNestedType("OnTurnEnd");
                if (nested == null) { ProbeLog.Error("[H6] 未找到嵌套类型 GameFlowManager/OnTurnEnd"); return; }
                var opImplicit = nested.GetMethod("op_Implicit",
                    BindingFlags.Public | BindingFlags.Static, null, new[] { typeof(Action) }, null);
                if (opImplicit == null) { ProbeLog.Error("[H6] OnTurnEnd 无 op_Implicit(Action)"); return; }

                object ours = opImplicit.Invoke(null, new object[] { (Action)OnGameTurnEnd });
                if (ours == null) { ProbeLog.Error("[H6] 构造 il2cpp 委托失败"); return; }

                var cur = Reflect.Prop(flowInstance, "onTurnEnd");
                object combined = ours;
                if (cur != null)
                {
                    var opAdd = nested.GetMethod("op_Addition", BindingFlags.Public | BindingFlags.Static, null, new[] { nested, nested }, null);
                    if (opAdd != null) combined = opAdd.Invoke(null, new[] { cur, ours });
                    else ProbeLog.Line("[H6] 无 op_Addition，将仅覆盖（不合并）");
                }
                bool ok = Reflect.SetPropOrField(flowInstance, "onTurnEnd", combined);
                ProbeLog.Mark("[H6] 订阅 onTurnEnd：" + (ok ? "写入成功 ✓（等待回调验证）" : "写入失败 ✗"));
            }
            catch (Exception e)
            {
                ProbeLog.Error("[H6] 订阅异常：" + e.Message);
            }
        }

        private static void OnGameTurnEnd()
        {
            try
            {
                ProbeLog.Mark("[H6] ★ onTurnEnd 委托回调触发");
                Snapshot("onTurnEnd.callback");
            }
            catch { }
        }
    }
}
