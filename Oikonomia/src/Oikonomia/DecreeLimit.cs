using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using BepInEx.Logging;

namespace Oikonomia
{
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    internal static class DecreeLimit
    {
        private static ManualLogSource _log;
        private static MethodInfo _isTrue;

        private static int _cached = -1;
        private static string _cachedDetail = "（尚未计算）";

        public static int Cached { get { return _cached; } }
        public static string CachedDetail { get { return _cachedDetail; } }

        public static void Init(Assembly asm, ManualLogSource log)
        {
            _log = log;
            try
            {
                var t = Reflect.FindType(asm, "ArticyLuaHelper");
                if (t != null) _isTrue = Reflect.FindMethod(t, "IsTrue");
                if (_isTrue == null)
                {
                    Suzerain.DiagKit.Diag.Missing("ArticyLuaHelper.IsTrue（动态上限读变量）");
                    log.LogWarning("[OI][limit] 未找到 ArticyLuaHelper.IsTrue —— 动态上限将退回静态值。");
                }
                else
                {
                    Suzerain.DiagKit.Diag.Ok("ArticyLuaHelper.IsTrue（动态上限读变量）");
                }
            }
            catch (Exception e) { try { log.LogWarning("[OI][limit] 初始化失败：" + e.Message); } catch { } }
        }

        

        
        
        
        
        public static int ComputeCore(
            int baseValue,
            int ministryCount, int ministryBonus, int ministryTotal, int ministryFullBonus,
            bool economyGood, int economyBonus,
            bool constitutionPassed, int constitutionBonus,
            bool emergency, int emergencyBonus,
            bool anyCrisis, int crisisPenalty,
            bool corruption, int corruptionPenalty,
            bool protests, int unrestPenalty,
            bool revolt, int revoltPenalty,
            int floor, out string detail)
        {
            return DecreeLimitCore.Compute(baseValue, ministryCount, ministryBonus, ministryTotal, ministryFullBonus, economyGood, economyBonus,
                constitutionPassed, constitutionBonus, emergency, emergencyBonus, anyCrisis, crisisPenalty,
                corruption, corruptionPenalty,
                protests, unrestPenalty, revolt, revoltPenalty, floor, out detail);
        }
        

        
        public static bool Recompute()
        {
            try
            {
                if (!OIConfig.EffectiveDynamicLimit) { _cached = -1; _cachedDetail = "（动态上限已关闭）"; return false; }

                
                int depts = 0;
                var deptVars = SplitList(OIConfig.VarMinistryBudget.Value);
                foreach (var v in deptVars) if (IsTrue(v)) depts++;

                
                bool economyGood = false;
                if (OIConfig.EconomyBonusTurn.Value <= 0 || CurrentTurnNo() >= OIConfig.EconomyBonusTurn.Value)
                {
                    foreach (var v in SplitList(OIConfig.VarEconomyGood.Value)) if (IsTrue(v)) { economyGood = true; break; }
                }

                
                bool constPassed = IsTrue(OIConfig.VarConstitutionVote.Value + " >= " + OIConfig.ConstitutionVoteThreshold.Value);
                if (constPassed && !string.IsNullOrEmpty(OIConfig.VarConstitutionPassed.Value))
                    constPassed = IsTrue(OIConfig.VarConstitutionPassed.Value);

                
                bool emergency = false;
                foreach (var v in SplitList(OIConfig.VarEmergency.Value)) if (IsTrue(v)) { emergency = true; break; }

                
                bool crisis = false;
                foreach (var v in SplitList(OIConfig.VarCrisis.Value)) if (IsTrue(v)) { crisis = true; break; }

                
                bool corruption = IsTrue(OIConfig.VarCorruption.Value);

                
                bool protests = IsTrue(OIConfig.VarProtests.Value);
                bool revolt = IsTrue(OIConfig.VarRevolt.Value);

                string detail;
                int val = ComputeCore(
                    EffectiveBase,
                    depts, OIConfig.MinistryBonus.Value, deptVars.Count, OIConfig.MinistryFullSetBonus.Value,
                    economyGood, OIConfig.EconomyBonus.Value,
                    constPassed, OIConfig.ConstitutionBonus.Value,
                    emergency, OIConfig.EmergencyBonus.Value,
                    crisis, OIConfig.CrisisPenalty.Value,
                    corruption, OIConfig.CorruptionPenalty.Value,
                    protests, OIConfig.UnrestPenalty.Value,
                    revolt, OIConfig.RevoltPenalty.Value,
                    OIConfig.LimitFloor.Value, out detail);

                _cached = val;
                _cachedDetail = detail + "   [部门 " + depts + "/" + deptVars.Count + " · 经济 " + (economyGood ? "增长" : "非增长")
                    + " · 宪法 " + (constPassed ? "通过" : "未通过") + " · 紧急 " + (emergency ? "是" : "否")
                    + " · 危机 " + (crisis ? "是" : "否") + " · 腐败 " + (corruption ? "是" : "否")
                    + " · 抗议 " + (protests ? "是" : "否")
                    + " · 起义 " + (revolt ? "是" : "否") + "]";
                return true;
            }
            catch (Exception e)
            {
                try { _log.LogWarning("[OI][limit] 上限计算失败（退回静态值）：" + e.Message); } catch { }
                _cached = -1;
                _cachedDetail = "（计算异常，已退回静态值）";
                return false;
            }
        }

        
        public static int EffectiveBase
        {
            get
            {
                try
                {
                    if (Bridge.HasOverride && Bridge.OvLimitBase.HasValue) return Bridge.OvLimitBase.Value;
                    return OIConfig.LimitBase.Value;
                }
                catch { return OIConfig.LimitBase.Value; }
            }
        }

        private static int CurrentTurnNo()
        {
            try
            {
                var flow = EconomyPatches.FlowInstanceForLimit;
                if (flow == null) return -1;
                return Reflect.ToInt(Reflect.Prop(flow, "currentTurnNo"));
            }
            catch { return -1; }
        }

        private static List<string> SplitList(string s)
        {
            var list = new List<string>();
            if (string.IsNullOrEmpty(s)) return list;
            foreach (var part in s.Split(new[] { '|', ';', ',' }, StringSplitOptions.RemoveEmptyEntries))
            {
                var t = part.Trim();
                if (t.Length > 0) list.Add(t);
            }
            return list;
        }

        private static bool IsTrue(string cond)
        {
            try
            {
                if (_isTrue == null || string.IsNullOrEmpty(cond)) return false;
                var r = _isTrue.Invoke(null, new object[] { cond });
                return r is bool b && b;
            }
            catch { return false; }
        }
    }
}
