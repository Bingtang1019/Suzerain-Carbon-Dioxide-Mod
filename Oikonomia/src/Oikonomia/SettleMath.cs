using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Oikonomia
{
    
    
    
    
    
    
    
    internal struct SettleInput
    {
        
        public bool Enabled;

        
        
        
        
        
        public bool BaselineValid;

        
        public int IndicatorTier;

        
        public string[] IndicatorLabels;

        
        public int[] Tier;

        
        public int[] PrevTier;

        
        public string[] RegionLabels;

        
        public bool TaxEfficient;

        
        public bool TaxAvoid;

        
        public bool PrevTaxAvoid;

        
        public int TradeTier;

        
        public string[] TradeLabels;

        
        public int TourismTier;

        
        public string[] TourismLabels;

        
        public bool[] Edge;

        
        public bool[] PrevEdge;

        
        public bool[] EdgeIsTrade;

        
        public string[] EdgeNames;

        
        public bool EdgeShort;

        
        public int EdgeLoaded;
    }

    
    
    
    
    
    
    
    internal struct SettleCfg
    {
        
        public bool Enabled;

        
        public bool EdOn;

        
        public bool RegionsOn;

        
        public bool TradeOn;

        
        public bool TourismOn;

        
        public bool TaxOn;

        
        public bool GrowthOn;

        
        public bool TradeDealsOn;

        
        public int EffDangerDelta, EffUnstableDelta, EffStableDelta, EffGrowingDelta, EffVeryGoodDelta;

        
        public int EffRegionStepDelta;

        
        public int EffTaxEfficientDelta;

        
        public int EffTaxAvoidanceDelta;

        
        public int EffTradeIncreasedDelta, EffTradeRecoveredDelta, EffTradeDecreasedDelta;

        
        public int EffTourismGoodDelta, EffTourismAverageDelta, EffTourismBadDelta;

        
        public int EffGrowthBonus;

        
        public int EffTradeDealBonus;
    }

    
    
    
    
    
    
    
    internal struct PerTurnRule
    {
        
        public string Label;

        
        public int Delta;

        
        public string Group;
    }

    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    internal static class SettleMath
    {
        
        
        
        
        
        
        
        
        
        public static int Compute(SettleInput st, SettleCfg cfg, IList<PerTurnRule> rules,
                                  StringBuilder parts, List<InGameDetailFormat.Entry> entries)
        {
            bool enabled = cfg.Enabled;
            int delta = 0;

            
            if (enabled && cfg.EdOn)
            {
                int d = IndicatorDelta(cfg, st.IndicatorTier);
                if (d != 0)
                {
                    delta += d;
                    parts.Append("ED=").Append(TierLabel(st, st.IndicatorTier)).Append(Signed(d)).Append("  ");
                    entries.Add(new InGameDetailFormat.Entry("ED=" + TierLabel(st, st.IndicatorTier), d));
                }
                else parts.Append("ED=").Append(TierLabel(st, st.IndicatorTier)).Append("(0)  ");
            }

            
            if (enabled && st.BaselineValid && cfg.RegionsOn)
            {
                for (int r = 0; r < 4; r++)
                {
                    if (st.Tier[r] < 0 || st.PrevTier[r] < 0) continue;
                    int steps = st.Tier[r] - st.PrevTier[r];
                    if (steps == 0) continue;
                    int d = steps * cfg.EffRegionStepDelta;
                    delta += d;
                    parts.Append(st.RegionLabels[r]).Append(steps > 0 ? "↑" : "↓")
                         .Append(Math.Abs(steps)).Append("档").Append(Signed(d)).Append("  ");
                    
                    
                    entries.Add(new InGameDetailFormat.Entry(
                        st.RegionLabels[r] + (steps > 0 ? "↑" : "↓")
                        + Math.Abs(steps).ToString(CultureInfo.InvariantCulture) + "档",
                        d, InGameDetailFormat.KindOneTime, null));
                }
            }

            
            if (enabled && rules != null)
            {
                for (int i = 0; i < rules.Count; i++)
                {
                    var rule = rules[i];
                    if (rule.Delta == 0) continue;
                    delta += rule.Delta;
                    parts.Append(rule.Label).Append("(每回合)").Append(Signed(rule.Delta)).Append("  ");
                    
                    
                    entries.Add(new InGameDetailFormat.Entry(
                        rule.Label + "(每回合)", rule.Delta, InGameDetailFormat.KindPerTurn, rule.Group));
                }
            }

            
            if (enabled && cfg.TradeOn)
            {
                int tti = st.TradeTier;
                if (tti >= 0)
                {
                    int d = tti == 0 ? cfg.EffTradeIncreasedDelta
                          : tti == 1 ? cfg.EffTradeRecoveredDelta
                                     : cfg.EffTradeDecreasedDelta;
                    if (d != 0)
                    {
                        delta += d;
                        parts.Append("贸易量=").Append(st.TradeLabels[tti]).Append(Signed(d)).Append("  ");
                        entries.Add(new InGameDetailFormat.Entry("贸易量=" + st.TradeLabels[tti], d));
                    }
                    else parts.Append("贸易量=").Append(st.TradeLabels[tti]).Append("(0)  ");
                }
            }

            
            if (enabled && cfg.TourismOn)
            {
                int tt = st.TourismTier;
                if (tt >= 0)
                {
                    int d = tt == 0 ? cfg.EffTourismGoodDelta
                          : tt == 1 ? cfg.EffTourismAverageDelta
                                    : cfg.EffTourismBadDelta;
                    if (d != 0)
                    {
                        delta += d;
                        parts.Append("旅游业=").Append(st.TourismLabels[tt]).Append(Signed(d)).Append("  ");
                        entries.Add(new InGameDetailFormat.Entry("旅游业=" + st.TourismLabels[tt], d));
                    }
                    else parts.Append("旅游业=").Append(st.TourismLabels[tt]).Append("(0)  ");
                }
            }

            
            if (enabled && cfg.TaxOn)
            {
                if (st.TaxEfficient)
                {
                    int d = cfg.EffTaxEfficientDelta;
                    delta += d;
                    parts.Append("税务效率").Append(Signed(d)).Append("  ");
                    entries.Add(new InGameDetailFormat.Entry("税务效率", d));
                }
                if (st.BaselineValid && st.TaxAvoid && !st.PrevTaxAvoid)
                {
                    int d = cfg.EffTaxAvoidanceDelta;
                    delta += d;
                    parts.Append("避税(一次性)").Append(Signed(d)).Append("  ");
                    entries.Add(new InGameDetailFormat.Entry(
                        "避税(一次性)", d, InGameDetailFormat.KindOneTime, null));
                }
            }

            
            if (enabled && st.BaselineValid && (cfg.GrowthOn || cfg.TradeDealsOn))
            {
                for (int i = 0; i < st.Edge.Length; i++)
                {
                    if (!st.Edge[i]) continue;
                    
                    
                    if (i < st.PrevEdge.Length && st.PrevEdge[i]) continue;
                    bool isTrade = st.EdgeIsTrade[i];
                    if (isTrade && !cfg.TradeDealsOn) continue;
                    if (!isTrade && !cfg.GrowthOn) continue;
                    if (st.EdgeShort && st.EdgeLoaded >= 0 && i >= st.EdgeLoaded) continue;   
                    int d = isTrade ? cfg.EffTradeDealBonus : cfg.EffGrowthBonus;
                    delta += d;
                    parts.Append(st.EdgeNames[i]).Append("(一次性)").Append(Signed(d)).Append("  ");
                    entries.Add(new InGameDetailFormat.Entry(
                        st.EdgeNames[i] + "(一次性)", d, InGameDetailFormat.KindOneTime, null));
                }
            }

            return delta;
        }

        
        
        
        
        
        
        
        
        
        
        public static bool HasSignal(SettleInput st)
        {
            if (st.IndicatorTier >= 0) return true;
            if (st.TaxEfficient || st.TaxAvoid) return true;
            if (st.TradeTier >= 0 || st.TourismTier >= 0) return true;

            if (st.Tier != null)
                for (int r = 0; r < st.Tier.Length; r++) if (st.Tier[r] >= 0) return true;

            if (st.Edge != null)
                for (int i = 0; i < st.Edge.Length; i++) if (st.Edge[i]) return true;

            return false;
        }

        
        
        
        
        
        
        public static string Signed(int v)
        {
            return (v >= 0 ? "+" : "") + v.ToString(CultureInfo.InvariantCulture);
        }

        
        private static int IndicatorDelta(SettleCfg cfg, int tier)
        {
            switch (tier)
            {
                case 0: return cfg.EffDangerDelta;
                case 1: return cfg.EffUnstableDelta;
                case 2: return cfg.EffStableDelta;
                case 3: return cfg.EffGrowingDelta;
                case 4: return cfg.EffVeryGoodDelta;
                default: return 0;
            }
        }

        
        private static string TierLabel(SettleInput st, int tier)
        {
            var labels = st.IndicatorLabels;
            return labels != null && tier >= 0 && tier < labels.Length ? labels[tier] : "未知";
        }
    }
}
