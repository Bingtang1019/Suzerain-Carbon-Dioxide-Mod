using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using BepInEx.Logging;
using HarmonyLib;

namespace Oikonomia
{
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    internal static partial class EconomyPatches
    {
        
        private static readonly string[] RegionLabels = { "贝吉亚", "格鲁尼", "洛伦", "阿格兰" };

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


    
    
    
    
    
    
    
    
    
    
        
        
        
        
        
        
        
        

        
        public static object FlowInstanceForLimit { get { return _flowInstance; } }

        private static string SnapshotPath
        {
            get
            {
                return Path.Combine(
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Oikonomia"),
                    "terms-snapshot.json");
            }
        }

        
        public static void Install(Harmony harmony, Assembly asm, ManualLogSource log)
        {
            _log = log;

            var flowType = Reflect.FindType(asm, "GameFlowManager");
            var condHelper = Reflect.FindType(asm, "ConditionalInstructionHelper");
            var persistence = Reflect.FindType(asm, "PersistenceManager");
            _pmType = persistence;
            var dataType = Reflect.FindType(asm, "ConditionalInstructionData");
            _luaHelper = Reflect.FindType(asm, "ArticyLuaHelper");

            if (_luaHelper != null)
            {
                _isTrue = Reflect.FindMethod(_luaHelper, "IsTrue");
                _runInstant = Reflect.FindMethod(_luaHelper, "RunInstant");
            }

            int ok = 0, miss = 0;

            
            ok += Patch(harmony, flowType, "EndTurn", nameof(Pre_EndTurn), null, ref miss);

            
            if (condHelper != null && flowType != null)
            {
                var actionEnum = flowType.GetNestedType("GameFlowAction");
                var m = actionEnum != null
                    ? AccessTools.Method(condHelper, "Evaluate", new[] { actionEnum, typeof(int), typeof(int) })
                    : null;
                ok += PatchMethod(harmony, m, null, nameof(Post_EvalTurnEnd), ref miss, "ConditionalInstructionHelper.Evaluate(TurnEnd)");
            }

            
            if (condHelper != null && dataType != null)
            {
                var m = AccessTools.Method(condHelper, "Evaluate", new[] { dataType });
                ok += PatchMethod(harmony, m, null, nameof(Post_EvalData), ref miss, "ConditionalInstructionHelper.Evaluate(data)");
            }

            
            ok += Patch(harmony, persistence, "TurnSave", nameof(Pre_TurnSave), null, ref miss);

            
            var numStat = Reflect.FindType(asm, "TemplateHUDStat");
            var numStatData = Reflect.FindType(asm, "HUDStatData");
            if (numStat != null && numStatData != null)
            {
                var m = AccessTools.Method(numStat, "Setup", new[] { numStatData });
                ok += PatchMethod(harmony, m, null, nameof(Post_StatSetup), ref miss, "TemplateHUDStat.Setup(HUDStatData)");
            }
            else { miss++; Suzerain.DiagKit.Diag.Missing("TemplateHUDStat/HUDStatData", "HUD 刷新不可用"); _log.LogWarning("[OI][economy] 未找到 TemplateHUDStat/HUDStatData，HUD 刷新不可用"); }

            
            
            InstallInGameDetail(harmony, asm, ref ok, ref miss);

            
            InstallIsRadioProbe(harmony, asm, ref ok, ref miss);

            
            InstallC2MultiSelect(harmony, asm, ref ok, ref miss);

            
            InstallT1Experiment(harmony, asm, ref ok, ref miss);

            
            InstallT1BothProjects(harmony, asm, ref ok, ref miss);

            
            InstallCompanyProbe(harmony, ref ok, ref miss);

            
            InstallBench(harmony, asm, ref ok, ref miss);

            
            ok += Patch(harmony, persistence, "LoadSaveFile", null, nameof(Post_GameLoaded), ref miss);
            ok += Patch(harmony, persistence, "LoadLastCheckpoint", null, nameof(Post_GameLoaded), ref miss);
            ok += Patch(harmony, persistence, "CreateNewCampaign", null, nameof(Post_GameLoaded), ref miss);

            if (_isTrue == null || _runInstant == null)
            {
                Suzerain.DiagKit.Diag.Fail("ArticyLuaHelper.IsTrue/RunInstant（经济读写入口）", "句柄缺失，经济结算不可用");
                _log.LogError("[OI][economy] ArticyLuaHelper 读写入口缺失，经济结算不可用。");
            }
            else
            {
                Suzerain.DiagKit.Diag.Ok("ArticyLuaHelper.IsTrue/RunInstant（经济读写入口）");
            }

            _log.LogInfo("[OI][economy] 经济结算挂载：成功 " + ok + " 个，缺失 " + miss + " 个。");
            _log.LogInfo("[OI][economy] 每回合条件词条（规则表）：" + RuleTable.Describe());
            LoadSnapshot();
        }

        private static int Patch(Harmony harmony, Type type, string method, string prefix, string postfix, ref int miss)
        {
            if (type == null) { miss++; return 0; }
            var m = Reflect.FindMethod(type, method);
            return PatchMethod(harmony, m, prefix, postfix, ref miss, type.Name + "." + method);
        }

        private static int PatchMethod(Harmony harmony, MethodInfo target, string prefix, string postfix,
                                       ref int miss, string label)
        {
            if (target == null) { miss++; Suzerain.DiagKit.Diag.Missing(label); return 0; }
            try
            {
                var pre = prefix == null ? null : new HarmonyMethod(
                    typeof(EconomyPatches).GetMethod(prefix, BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public));
                var post = postfix == null ? null : new HarmonyMethod(
                    typeof(EconomyPatches).GetMethod(postfix, BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public));
                harmony.Patch(target, prefix: pre, postfix: post);
                Suzerain.DiagKit.Diag.Ok(label);
                return 1;
            }
            catch (Exception e)
            {
                miss++;
                Suzerain.DiagKit.Diag.Fail(label, e.Message);
                _log.LogWarning("[OI][economy] 挂载失败 " + label + "：" + e.Message);
                return 0;
            }
        }


        

        private static void Pre_EndTurn(object __instance)
        {
            try { _flowInstance = __instance; } catch { }
        }

        private static void Post_EvalTurnEnd(object __0, object __1, object __2)
        {
            try
            {
                var action = Reflect.Str(__0) ?? "";
                if (action.IndexOf("TurnEnd", StringComparison.OrdinalIgnoreCase) < 0) return;
                int turn = Reflect.ToInt(__1);
                _pendingTurn = turn;
                
                
                
                
                try { RefreshBudgetHud("回合边界（每回合一次）"); }
                catch (Exception eHud) { _log.LogWarning("[OI][hud] 回合边界刷新异常：" + eHud.GetType().Name); }
                
                
                try { RefreshDetailPreview("回合边界"); }
                catch (Exception ePrev) { _log.LogWarning("[OI][hud] 回合边界预演异常：" + ePrev.GetType().Name); }
                if (EconomyConfig.VerboseLog != null && EconomyConfig.VerboseLog.Value)
                    _log.LogInfo("[OI][economy] 新回合开始 turn=" + turn + "，等待经济指示器重算后结算…");
            }
            catch (Exception e) { _log.LogWarning("[OI][economy] TurnEnd 处理异常：" + e.Message); }
        }

        private static void Post_EvalData(object __0)
        {
            try
            {
                if (_pendingTurn < 0) return;
                var name = Reflect.Str(Reflect.Prop(__0, "NameInDatabase")) ?? "";
                if (name.IndexOf("Calculation_Economy_Indicator", StringComparison.OrdinalIgnoreCase) < 0) return;
                Settle("指示器重算后");
            }
            catch (Exception e) { _log.LogWarning("[OI][economy] 结算点处理异常：" + e.Message); }
        }

        
        private static void Post_StatSetup(object __instance, object __0)
        {
            try
            {
                var name = Reflect.Str(Reflect.Prop(__0, "NameInDatabase")) ?? "";
                if (name.IndexOf("GovernmentBudget", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    _budgetStat = __instance;
                    
                    EnsureModifierVisible(__instance, name);
                    
                    
                    RefreshDetailPreview("捕获预算统计");
                    if (EconomyConfig.VerboseLog != null && EconomyConfig.VerboseLog.Value)
                        _log.LogInfo("[OI][economy] 已捕获预算 HUD 统计：" + name);
                }
            }
            catch { }
        }

        
        private static void Post_GameLoaded(object __instance)
        {
            try
            {
                if (__instance != null) _pmInstance = __instance;
                _needsRebase = true;
                _lastSettledTurn = -1;
                _budgetStat = null;   
                
                    
                    
                    try
                    {
                        ClearPendingCluster("读档/新战役");
                        _log.LogInfo("[OI][company-map] 读档 ⇒ 待消费戳已清 ✓（★ 防旧戳被误用 ✓）");
                    }
                    catch { }
                    _log.LogInfo("[OI][economy] 检测到读档/新战役 → 本回合只重建基线（升降与一次性机制本期不结算，ED/税务效率照常）");
            }
            catch { }
        }

        private static void Pre_TurnSave(object __instance)
        {
            try
            {
                if (__instance != null) _pmInstance = __instance;
                if (_pendingTurn < 0) return;
                Settle("回合存档前（兜底）");
            }
            catch (Exception e) { _log.LogWarning("[OI][economy] 兜底结算异常：" + e.Message); }
        }

        

        
        
        
        
        
        private static bool DecideRebase(int turn)
        {
            bool rebase = _needsRebase;
            if (_hasSnapshot && _snapTurn >= 0 && turn < _snapTurn) rebase = true;
            if (_snapCampaignHash != 0 && CampaignHash() != _snapCampaignHash) rebase = true;
            return rebase;
        }

        
        
        
        
        private static SettleCfg ReadSettleCfg()
        {
            var c = new SettleCfg();
            c.Enabled = EconomyConfig.EffectiveEnabled;
            c.EdOn = EconomyConfig.EdOn;
            c.RegionsOn = EconomyConfig.RegionsOn;
            c.TradeOn = EconomyConfig.TradeOn;
            c.TourismOn = EconomyConfig.TourismOn;
            c.TaxOn = EconomyConfig.TaxOn;
            c.GrowthOn = EconomyConfig.GrowthOn;
            c.TradeDealsOn = EconomyConfig.TradeDealsOn;
            c.EffDangerDelta = EconomyConfig.EffDangerDelta;
            c.EffUnstableDelta = EconomyConfig.EffUnstableDelta;
            c.EffStableDelta = EconomyConfig.EffStableDelta;
            c.EffGrowingDelta = EconomyConfig.EffGrowingDelta;
            c.EffVeryGoodDelta = EconomyConfig.EffVeryGoodDelta;
            c.EffRegionStepDelta = EconomyConfig.EffRegionStepDelta;
            c.EffTaxEfficientDelta = EconomyConfig.EffTaxEfficientDelta;
            c.EffTaxAvoidanceDelta = EconomyConfig.EffTaxAvoidanceDelta;
            c.EffTradeIncreasedDelta = EconomyConfig.EffTradeIncreasedDelta;
            c.EffTradeRecoveredDelta = EconomyConfig.EffTradeRecoveredDelta;
            c.EffTradeDecreasedDelta = EconomyConfig.EffTradeDecreasedDelta;
            c.EffTourismGoodDelta = EconomyConfig.EffTourismGoodDelta;
            c.EffTourismAverageDelta = EconomyConfig.EffTourismAverageDelta;
            c.EffTourismBadDelta = EconomyConfig.EffTourismBadDelta;
            c.EffGrowthBonus = EconomyConfig.EffGrowthBonus;
            c.EffTradeDealBonus = EconomyConfig.EffTradeDealBonus;
            return c;
        }

        
        
        
        
        
        
        
        private static SettleInput ReadSettleState(bool baselineValid, SettleCfg cfg)
        {
            var st = new SettleInput();
            st.Enabled = cfg.Enabled;
            st.BaselineValid = baselineValid;

            st.IndicatorTier = ReadIndicatorTier();           
            st.IndicatorLabels = IndicatorLabels;

            st.Tier = new int[4];
            for (int r = 0; r < 4; r++) st.Tier[r] = ReadRegionTier(r);
            st.RegionLabels = RegionLabels;

            st.TaxAvoid = IsTrue(TaxAvoidance);
            st.TaxEfficient = IsTrue(TaxEfficient);

            st.Edge = new bool[EdgeTerms.Length];
            st.EdgeIsTrade = new bool[EdgeTerms.Length];
            st.EdgeNames = new string[EdgeTerms.Length];
            for (int i = 0; i < st.Edge.Length; i++)
            {
                st.Edge[i] = IsTrue(EdgeTerms[i][1]);
                st.EdgeIsTrade[i] = EdgeTerms[i][2] == "T";
                st.EdgeNames[i] = EdgeTerms[i][0];
            }

            
            st.PrevTier = (int[])PrevTier.Clone();
            st.PrevEdge = (bool[])PrevEdge.Clone();
            st.PrevTaxAvoid = PrevTaxAvoid;
            st.EdgeShort = _edgeShort;
            st.EdgeLoaded = _edgeLoaded;

            st.TradeLabels = TradeLabels;
            st.TourismLabels = TourismLabels;
            st.TradeTier = cfg.TradeOn ? ReadTradeTier() : -1;
            st.TourismTier = cfg.TourismOn ? ReadTourismTier() : -1;
            return st;
        }

        
        
        
        
        
        private static List<PerTurnRule> ReadPerTurnRules()
        {
            var list = new List<PerTurnRule>();
            foreach (var rule in RuleTable.PerTurnConditions())
            {
                if (rule.Delta == 0) continue;
                if (!IsTrue(rule.Condition)) continue;
                var r = new PerTurnRule();
                r.Label = rule.Label;
                r.Delta = rule.Delta;
                r.Group = GroupNameOf(rule);
                list.Add(r);
            }
            return list;
        }

        
        
        
        
        
        
        
        
        
        
        private static int ComputeDelta(bool baselineValid, StringBuilder parts,
                                        List<InGameDetailFormat.Entry> entries, out SettleInput state)
        {
            var cfg = ReadSettleCfg();
            state = ReadSettleState(baselineValid, cfg);
            return SettleMath.Compute(state, cfg, ReadPerTurnRules(), parts, entries);
        }

        
        
        
        
        
        private static bool TryComputePreview(bool baselineValid, StringBuilder parts,
                                              List<InGameDetailFormat.Entry> entries, out int delta)
        {
            var cfg = ReadSettleCfg();
            var state = ReadSettleState(baselineValid, cfg);
            delta = 0;
            if (!SettleMath.HasSignal(state)) return false;
            delta = SettleMath.Compute(state, cfg, ReadPerTurnRules(), parts, entries);
            return true;
        }

        
        
        
        
        
        
        
        private static int PreviewTurn()
        {
            if (_pendingTurn >= 0) return _pendingTurn;
            if (_lastSettledTurn >= 0) return _lastSettledTurn;
            if (_snapTurn >= 0) return _snapTurn;
            return 0;
        }

        
        
        
        
        
        
        internal static void RefreshDetailPreview(string where)
        {
            try
            {
                if (EconomyConfig.InGameDetail == null || !EconomyConfig.InGameDetail.Value) return;

                int turn = PreviewTurn();
                bool baselineValid = _hasSnapshot && !DecideRebase(turn);

                var parts = new StringBuilder();
                var entries = new List<InGameDetailFormat.Entry>();
                int delta;
                if (!TryComputePreview(baselineValid, parts, entries, out delta))
                {
                    if (EconomyConfig.VerboseLog != null && EconomyConfig.VerboseLog.Value)
                        _log.LogInfo("[OI][ui] 预演跳过（未读到战役经济信号，不接管原生 tooltip）触发点=" + where);
                    return;
                }

                PublishPreview(delta, entries, where);
            }
            catch (Exception e) { _log.LogWarning("[OI][ui] 预演异常：" + e.Message); }
        }

        

        private static void Settle(string where)
        {
            if (_inSettle) return;
            _inSettle = true;
            try { SettleCore(where); }
            catch (Exception e) { _log.LogWarning("[OI][economy] 结算异常：" + e.Message); }
            finally { _inSettle = false; }
        }

        private static void SettleCore(string where)
        {
            int turn = CurrentTurnNo();
            if (turn < 0) turn = _pendingTurn;

            
            if (turn == _lastSettledTurn) { _pendingTurn = -1; return; }

            bool rebase = DecideRebase(turn);
            bool baselineValid = _hasSnapshot && !rebase;   
            if (rebase)
                _log.LogInfo("[OI][economy] 本回合基线重建（turn=" + turn + "，快照 turn=" + _snapTurn + "）");

            
            
            
            var parts = new StringBuilder();
            
            
            var detailEntries = new List<InGameDetailFormat.Entry>();
            SettleInput state;
            int delta = ComputeDelta(baselineValid, parts, detailEntries, out state);

            
            int indicatorTier = state.IndicatorTier;          
            var tier = state.Tier;
            bool taxAvoid = state.TaxAvoid;
            bool taxEfficient = state.TaxEfficient;
            var edge = state.Edge;
            bool enabled = state.Enabled;

            
            
            

            
            int budgetBefore = ReadIntVar("BaseGame.GovernmentBudget", -40, 120);
            int budgetAfter = budgetBefore;
            string applied = "未改动";
            if (!enabled)
            {
                
                applied = "总开关关闭，跳过写入";
            }
            else if (delta != 0)
            {
                string instr = delta > 0
                    ? "BaseGame.GovernmentBudget += " + delta.ToString(CultureInfo.InvariantCulture) + ";"
                    : "BaseGame.GovernmentBudget -= " + (-delta).ToString(CultureInfo.InvariantCulture) + ";";
                bool ok = RunInstant(instr);
                budgetAfter = ReadIntVar("BaseGame.GovernmentBudget", -40, 120);
                string hudNote = RefreshBudgetHud("结算写入后");
                applied = ok
                    ? ("写入 " + instr + "（预算 " + BudgetText(budgetBefore) + " → " + BudgetText(budgetAfter) + "）" + hudNote)
                    : "写入失败";
            }
            else
            {
                
                
                
                
                string hudNote0 = RefreshBudgetHud("结算（delta=0 · 无写入）");
                applied = "未改动（delta=0）" + hudNote0;
            }

            _log.LogInfo("[OI][economy] 结算 turn=" + turn + "（" + where + "）"
                + " 合计=" + Signed(delta)
                + " ｜ " + (parts.Length == 0 ? "无分项" : parts.ToString().Trim())
                + " ｜ " + applied);

            
            try
            {
                int growthActive = 0;
                for (int i = 0; i < edge.Length; i++) if (edge[i] && EdgeTerms[i][2] == "G") growthActive++;
                Bridge.SetEconomyState(TierLabel(indicatorTier), TierString(tier), taxEfficient, taxAvoid,
                    growthActive, turn, delta, parts.ToString().Trim(), budgetAfter);
                Bridge.WriteState("回合结算", null, -1);
            }
            catch { }

            
            
            PublishDetail(delta, detailEntries);

            
            SaveBaseline(turn, tier, taxAvoid, edge);
            _needsRebase = false;
            _lastSettledTurn = turn;
            _pendingTurn = -1;

            
            
            
            RefreshDetailPreview("结算后");
    }
}
}
