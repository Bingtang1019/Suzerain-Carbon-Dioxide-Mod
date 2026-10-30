using System;
using System.Collections.Generic;
using System.Text;

namespace Oikonomia
{
    
    internal enum RuleKind
    {
        
        PerTurnCondition
    }

    
    internal sealed class RuleDef
    {
        public RuleKind Kind;
        
        public string Id;
        
        public string Label;
        
        public string Condition;
        
        public int Delta;
        
        public string Group;
        
        public string Source;

        public override string ToString()
        {
            return Label + "(" + (Delta >= 0 ? "+" + Delta : Delta.ToString()) + ") ← " + Condition + "  [" + Source + "]";
        }
    }

    
    
    
    
    
    
    
    
    
    
    internal static class RuleTable
    {
        
        public static List<RuleDef> PerTurnConditions()
        {
            var list = new List<RuleDef>();

            
            if (EconomyConfig.TaxOn)
            {
                list.Add(new RuleDef
                {
                    Kind = RuleKind.PerTurnCondition,
                    Id = "BaseGame.Situation_Economy_TaxEvasion",
                    Label = "逃税",
                    Condition = "BaseGame.Situation_Economy_TaxEvasion",
                    Delta = EconomyConfig.EffTaxEvasionDelta,
                    Source = "Economy.Tax.EvasionDelta"
                });
            }

            
            
            
            foreach (var fc in EconomyPatches.FixedCostList())
            {
                list.Add(fc);
            }

            
            foreach (var extra in ParseExtras(EconomyConfig.PerTurnExtraConditionsRaw))
            {
                
                bool dup = false;
                foreach (var r in list) { if (r.Condition == extra.Condition) { dup = true; break; } }
                if (!dup) list.Add(extra);
            }

            return list;
        }

        
        
        
        
        
        
        
        
        private static List<RuleDef> ParseExtras(string raw)
        {
            var list = new List<RuleDef>();
            if (string.IsNullOrEmpty(raw)) return list;
            foreach (var part in raw.Split(new[] { ';', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries))
            {
                var seg = part.Trim();
                if (seg.Length == 0 || seg.StartsWith("#")) continue;
                var f = seg.Split('|');
                if (f.Length < 3) continue;
                var varName = f[0].Trim();
                var label = f[1].Trim();
                int delta;
                if (varName.Length == 0 || !int.TryParse(f[2].Trim(), out delta)) continue;
                var shown = label.Length > 0 ? label : varName;
                var group = f.Length >= 4 ? f[3].Trim() : "";
                list.Add(new RuleDef
                {
                    Kind = RuleKind.PerTurnCondition,
                    Id = varName,
                    Label = shown,
                    Condition = varName,
                    Delta = delta,
                    Group = group.Length > 0 ? group : KnownGroupForLabel(shown),
                    Source = "Economy.PerTurn.ExtraConditions"
                });
            }
            return list;
        }

        
        
        
        
        public static string KnownGroupForLabel(string label)
        {
            if (string.IsNullOrEmpty(label)) return "";
            if (label == "免费处方药" || label == "福利支出") return InGameDetailFormat.GroupExtraWelfare;
            return "";
        }

        
        public static string Describe()
        {
            var rules = PerTurnConditions();
            if (rules.Count == 0) return "（无每回合条件词条）";
            var sb = new StringBuilder();
            for (int i = 0; i < rules.Count; i++)
            {
                if (i > 0) sb.Append(" · ");
                sb.Append(rules[i].Label).Append(rules[i].Delta >= 0 ? "+" : "").Append(rules[i].Delta);
            }
            return rules.Count + " 条：" + sb;
        }
    }
}
