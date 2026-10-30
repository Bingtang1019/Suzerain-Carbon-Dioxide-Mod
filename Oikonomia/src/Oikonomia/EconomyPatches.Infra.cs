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
                
                if (_isTrueErrLogged < 20)
                {
                    _isTrueErrLogged++;
                    _log.LogWarning("[OI][lua] IsTrue 异常 cond=" + (cond ?? "（null）")
                        + " err=" + e.GetType().Name + " " + e.Message);
                }
                return false;
            }
        }
        private static int _isTrueErrLogged;

        private static bool RunInstant(string instruction)
        {
            try
            {
                if (_runInstant == null) return false;
                _runInstant.Invoke(null, new object[] { instruction });
                return true;
            }
            catch (Exception e)
            {
                _log.LogWarning("[OI][economy] 指令执行失败：" + instruction + " → " + e.Message);
                return false;
            }
        }
        
        private static void RecalcIndicator()
        {
            try
            {
                if (_flowInstance == null) return;
                var m = _flowInstance.GetType().GetMethod("EvaluateConditionalInstruction",
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null,
                    new[] { typeof(string) }, null);
                if (m != null) m.Invoke(_flowInstance, new object[] { "Calculation_Economy_Indicator" });
            }
            catch (Exception e) { _log.LogWarning("[OI][P2] 指示器重算失败：" + e.Message); }
        }

        private static void SaveBaseline(int turn, int[] tier, bool taxAvoid, bool[] edge)
        {
            for (int r = 0; r < 4; r++) PrevTier[r] = tier[r];
            PrevTaxAvoid = taxAvoid;
            for (int i = 0; i < PrevEdge.Length; i++) PrevEdge[i] = i < edge.Length && edge[i];
            _edgeLoaded = edge.Length;
            _edgeShort = false;
            _hasSnapshot = true;
            _snapTurn = turn;
            _snapCampaignHash = CampaignHash();
            PersistSnapshot();
        }


        
        
        
        
        private static int ReadIntVar(string varName, int lo, int hi)
        {
            for (int v = lo; v <= hi; v++)
                if (IsTrue(varName + " == " + v)) return v;
            return int.MinValue;
        }

        
        
        
        private static string HudShownText()
        {
            try
            {
                var stat = _budgetStat;
                if (stat == null) return null;
                var st = Reflect.Prop(stat, "statText");
                if (st == null) return null;
                var tx = Reflect.Prop(st, "text");
                return tx == null ? null : Convert.ToString(tx);
            }
            catch { return null; }
        }

        
        
        
        
        
        
        
        private static string RefreshBudgetHud(string why)
        {
            try
            {
                var stat = _budgetStat;
                if (stat == null) return "（HUD 未捕获，未刷新）";
                if (!Reflect.Usable(stat)) { _budgetStat = null; return "（HUD 引用失效，未刷新）"; }

                
                var data = Reflect.Prop(stat, "currentHUDStatData");
                var name = Reflect.Str(Reflect.Prop(data, "NameInDatabase"));
                if (string.IsNullOrEmpty(name)) { _budgetStat = null; return "（HUD 验活失败，未刷新）"; }

                
                string shownBefore = HudShownText();
                int valBefore = Reflect.ToInt(Reflect.Call0(stat, "GetCurrentValue"));
                int varBefore = ReadIntVar("BaseGame.GovernmentBudget", -40, 120);

                var t = stat.GetType();
                var setup = t.GetMethod("Setup", BindingFlags.Public | BindingFlags.Instance, null, Type.EmptyTypes, null);
                if (setup == null) return "（无参 Setup 不存在）";
                setup.Invoke(stat, null);

                
                
                ApplyDetailToBudgetStat();

                
                string shownAfter = HudShownText();
                int valAfter = Reflect.ToInt(Reflect.Call0(stat, "GetCurrentValue"));
                int varAfter = ReadIntVar("BaseGame.GovernmentBudget", -40, 120);

                bool ok = shownAfter != null && shownAfter.Trim() == BudgetText(varAfter);
                _log.LogInfo("[OI][hud] 刷新（" + (string.IsNullOrEmpty(why) ? "写入后" : why) + "）@"
                    + System.DateTime.Now.ToString("HH:mm:ss.fff")
                    + " ｜ 显示=[" + (shownBefore ?? "?") + "] → [" + (shownAfter ?? "?") + "]"
                    + " ｜ GetCurrentValue=" + BudgetText(valBefore) + " → " + BudgetText(valAfter)
                    + " ｜ 变量=" + BudgetText(varBefore) + " → " + BudgetText(varAfter)
                    + " ｜ 显示=变量? " + (ok ? "一致 ✓" : "不一致 ✗"));

                return "，HUD 已刷新为 " + BudgetText(valAfter) + "（显示层读数=" + (shownAfter ?? "?") + "）";
            }
            catch (Exception e)
            {
                return "（HUD 刷新异常：" + e.GetType().Name + "）";
            }
        }

        
        private static string RefreshBudgetHud() { return RefreshBudgetHud("写入后"); }

        private static string BudgetText(int v)
        {
            return v == int.MinValue ? "?" : v.ToString(CultureInfo.InvariantCulture);
        }

        
        
        
        
        
        
        private static string GroupNameOf(RuleDef rule)
        {
            if (rule == null) return "";
            if (!string.IsNullOrEmpty(rule.Group)) return rule.Group;
            return RuleTable.KnownGroupForLabel(rule.Label);
        }

        

        private static int CampaignHash()
        {
            try
            {
                var name = CampaignName();
                if (string.IsNullOrEmpty(name)) return 0;
                int h = 17;
                foreach (var c in name) h = h * 31 + c;
                return h;
            }
            catch { return 0; }
        }

        private static string CampaignName()
        {
            try
            {
                object pm = _pmInstance;
                if (pm == null && _pmType != null) pm = Reflect.Static(_pmType, "Instance", "instance", "_instance");
                if (pm == null) return null;
                var file = Reflect.Prop(pm, "activeSaveFile");
                if (file == null) return null;
                var data = Reflect.Prop(file, "saveData");
                var cn = Reflect.Str(Reflect.Prop(data, "campaignName"));
                if (!string.IsNullOrEmpty(cn)) return cn;
                var fn = Reflect.Str(Reflect.Prop(file, "fileName"));
                if (!string.IsNullOrEmpty(fn)) return fn;
            }
            catch { }
            return null;
    }
}
}
