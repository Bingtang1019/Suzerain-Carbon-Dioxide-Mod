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
        

    
    
    
        private const string TaxEfficient = "BaseGame.Situation_Economy_TaxEfficientEconomy";
        private const string TaxEvasion = "BaseGame.Situation_Economy_TaxEvasion";   
        private const string TaxAvoidance = "BaseGame.Situation_Economy_TaxAvoidance";

        
        private static readonly string[] TourismTerms = {
            "BaseGame.Situation_Economy_Tourism_Booming",     
            "BaseGame.Situation_Economy_Tourism_Average",     
            "BaseGame.Situation_Economy_Tourism_Declining"    
        };
        private static readonly string[] TourismLabels = { "蓬勃发展", "平均水平", "严重衰退" };

        
        private static readonly string[] TradeTerms = {
            "BaseGame.Situation_Economy_IncreasedTrade",    
            "BaseGame.Situation_Economy_RecoveredTrade",    
            "BaseGame.Situation_Economy_DecreasedTrade"     
        };
        private static readonly string[] TradeLabels = { "增加", "恢复", "减少" };

        
        
        
        
        private static readonly string[][] EdgeTerms = {
            
            new[] { "高就业",       "BaseGame.Situation_Economy_HighEmployment", "G" },
            new[] { "产量增加",     "BaseGame.Situation_Economy_ProductionIncreased", "G" },
            new[] { "小企业成长",   "BaseGame.Situation_Economy_SmallBusiness_Growth", "G" },
            new[] { "大企业创造就业","BaseGame.Situation_Economy_LargeBusiness_CreateJobs", "G" },
            new[] { "阿卡西亚投资", "BaseGame.Situation_Economy_FinancialInvestment_Arcasia", "G" },
            new[] { "康塔纳投资",   "BaseGame.Situation_Economy_FinancialInvestment_UC", "G" },
            new[] { "军工复合体",   "BaseGame.Situation_Economy_SordishMilitaryIndustrialComplex", "G" },
            
            new[] { "莱斯皮亚·煤炭贸易",   "BaseGame.Situation_Diplomacy_Lespia_Coal_Trade", "T" },
            new[] { "莱斯皮亚·鱼类贸易",   "BaseGame.Situation_Diplomacy_Lespia_Fish_Trade", "T" },
            new[] { "莱斯皮亚·钨贸易",     "BaseGame.Situation_Diplomacy_Lespia_Tungsten_Trade", "T" },
            new[] { "莱斯皮亚·小麦贸易",   "BaseGame.Situation_Diplomacy_Lespia_Wheat_Trade", "T" },
            new[] { "莱斯皮亚·石油出售",   "BaseGame.Situation_Diplomacy_Lespia_SordishPetroleum", "T" },
            new[] { "莱斯皮亚自由贸易区",  "BaseGame.Situation_Diplomacy_FTZLespia", "T" },
            new[] { "瓦尔戈斯兰·煤炭贸易", "BaseGame.Situation_Diplomacy_Valgsland_Coal_Trade", "T" },
            new[] { "瓦尔戈斯兰·鱼类贸易", "BaseGame.Situation_Diplomacy_Valgsland_Fish_Trade", "T" },
            new[] { "瓦尔戈斯兰·钨贸易",   "BaseGame.Situation_Diplomacy_Valgsland_Tungsten_Trade", "T" },
            new[] { "瓦尔戈斯兰·小麦贸易", "BaseGame.Situation_Diplomacy_Valgsland_Wheat_Trade", "T" },
            new[] { "瓦尔戈斯兰自由贸易区","BaseGame.Situation_Diplomacy_FTZValgsland", "T" },
            new[] { "韦伦·石油自由贸易",   "BaseGame.Situation_Diplomacy_Wehlen_FreeTariffs_Oil", "T" },
            new[] { "阿格诺利亚·农业市场","BaseGame.Situation_Diplomacy_Agnolia_Agriculture_Market_Access", "T" },
            new[] { "伦堡·天然气管道",     "BaseGame.Situation_Diplomacy_Rumburg_GasPipeline", "T" }
        };

        
        private static readonly string[] IndicatorConds = {
            "BaseGame.Economy_Indicator >= 2",
            "BaseGame.Economy_Indicator == 1",
            "BaseGame.Economy_Indicator == 0",
            "BaseGame.Economy_Indicator == -1",
            "BaseGame.Economy_Indicator <= -2"
        };
        private static readonly string[] IndicatorLabels = { "危险", "不稳定", "稳定", "增长", "极快增长" };

        
        
        
        
        private static ManualLogSource _log;
        private static Type _luaHelper;
        private static MethodInfo _isTrue;
        private static MethodInfo _runInstant;

        private static object _budgetStat;            
        private static object _flowInstance;
        
        
        private static object _pmInstance;
        private static Type _pmType;
        private static int _pendingTurn = -1;          
        private static bool _needsRebase;             
        private static bool _inSettle;                
        private static int _lastSettledTurn = -1;

        private static bool _hasSnapshot;
        private static int _snapTurn = -1;
        private static int _snapCampaignHash;
        private static readonly int[] PrevTier = { -1, -1, -1, -1 };
        private static bool PrevTaxAvoid;
        
        private const int EdgeLayoutVersion = 2;

        private static readonly bool[] PrevEdge = new bool[EdgeTerms.Length];   
        private static int _edgeLoaded = -1;                       
        private static bool _edgeShort;                            

        

        private static int CurrentTurnNo()
        {
            try
            {
                if (_flowInstance == null) return -1;
                var v = Reflect.Prop(_flowInstance, "currentTurnNo");
                return v == null ? -1 : Reflect.ToInt(v);
            }
            catch { return -1; }
        }

        private static int ReadIndicatorTier()
        {
            for (int i = 0; i < IndicatorConds.Length; i++)
                if (IsTrue(IndicatorConds[i])) return 4 - i;   
            return -1;
        }

        private static List<RuleDef> _fixedCosts;
        private static string _fixedCostsRaw;

        
        
        
        
        
        
        
        
        
        internal static List<RuleDef> FixedCostList()
        {
            var raw = EconomyConfig.FixedCostsRaw ?? "";
            if (_fixedCosts != null && _fixedCostsRaw == raw) return _fixedCosts;

            var list = new List<RuleDef>();
            foreach (var part in raw.Split(new[] { '|', ';', ',', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries))
            {
                var seg = part.Trim();
                if (seg.Length == 0 || seg.StartsWith("#")) continue;

                
                int eq = seg.LastIndexOf('=');
                if (eq > 0)
                {
                    var name = seg.Substring(0, eq).Trim();
                    int d;
                    if (name.Length == 0 || !int.TryParse(seg.Substring(eq + 1).Trim(), out d)) continue;
                    var label = FixedCostLabel(name);
                    list.Add(FixedCostDef(name, label, d, ""));
                    continue;
                }

                
                var f = seg.Split('|');
                if (f.Length < 3) continue;
                var varName = f[0].Trim();
                int delta;
                if (varName.Length == 0 || !int.TryParse(f[2].Trim(), out delta)) continue;
                var shown = f[1].Trim();
                if (shown.Length == 0) shown = FixedCostLabel(varName);
                var group = f.Length >= 4 ? f[3].Trim() : "";
                list.Add(FixedCostDef(varName, shown, delta, group));
            }
            _fixedCosts = list; _fixedCostsRaw = raw;
            return list;
        }

        
        private static RuleDef FixedCostDef(string varName, string label, int delta, string group)
        {
            return new RuleDef
            {
                Kind = RuleKind.PerTurnCondition,
                Id = varName,
                Label = label,
                Condition = varName,
                Delta = delta,
                Group = !string.IsNullOrEmpty(group) ? group : RuleTable.KnownGroupForLabel(label),
                Source = "Economy.PerTurn.FixedCosts"
            };
        }

        
        internal static string FixedCostLabel(string varName)
        {
            if (varName == "BaseGame.Situation_Welfare_FreeMedicine") return "免费处方药";
            int dot = varName.LastIndexOf('.');
            return dot >= 0 ? varName.Substring(dot + 1) : varName;
        }

        
        private static int ReadTradeTier()
        {
            int found = -1;
            for (int i = 0; i < TradeTerms.Length; i++)
            {
                if (!IsTrue(TradeTerms[i])) continue;
                if (found >= 0) return -1;
                found = i;
            }
            return found;
        }

        
        private static int ReadTourismTier()
        {
            int found = -1;
            for (int i = 0; i < TourismTerms.Length; i++)
            {
                if (!IsTrue(TourismTerms[i])) continue;
                if (found >= 0) return -1;   
                found = i;
            }
            return found;
        }

        private static int ReadRegionTier(int r)
        {
            int found = -1;
            for (int t = 0; t < 3; t++)
            {
                if (!IsTrue(RegionTerms[r][t])) continue;
                if (found >= 0) return -1;   
                found = t;
            }
            return found;
        }
        
        

        private static string TierLabel(int tier)
        {
            return tier >= 0 && tier < IndicatorLabels.Length ? IndicatorLabels[tier] : "未知";
        }

        private static string TierString(int[] tier)
        {
            var sb = new StringBuilder();
            for (int r = 0; r < 4; r++)
            {
                if (r > 0) sb.Append('/');
                sb.Append(tier[r] < 0 ? "?" : new[] { "坏", "中", "好" }[tier[r]]);
            }
            return sb.ToString();
        }

        
        
        
        
        
        
        private static string Signed(int v)
        {
            return SettleMath.Signed(v);
    }
}
}
