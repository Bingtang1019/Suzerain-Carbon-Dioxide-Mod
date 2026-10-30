using System;
using System.Text;

namespace Oikonomia
{
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    internal static class DecreeLimitCore
    {
        public static int Compute(
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
            var sb = new StringBuilder();
            int total = baseValue;
            sb.Append("基数 ").Append(baseValue);

            if (ministryCount > 0)
            {
                int d = ministryCount * ministryBonus;
                total += d;
                sb.Append(" + 部门预算×").Append(ministryCount).Append("(").Append(Signed(d)).Append(")");
                
                if (ministryTotal > 0 && ministryCount >= ministryTotal && ministryFullBonus != 0)
                {
                    total += ministryFullBonus;
                    sb.Append(" + 四部门齐全(").Append(Signed(ministryFullBonus)).Append(")");
                }
            }
            if (economyBonus != 0 && economyGood)
            {
                total += economyBonus;
                sb.Append(" + 经济增长(").Append(Signed(economyBonus)).Append(")");
            }
            if (constitutionBonus != 0 && constitutionPassed)
            {
                total += constitutionBonus;
                sb.Append(" + 宪法通过(").Append(Signed(constitutionBonus)).Append(")");
            }
            if (emergencyBonus != 0 && emergency)
            {
                total += emergencyBonus;
                sb.Append(" + 紧急状态(").Append(Signed(emergencyBonus)).Append(")");
            }
            if (crisisPenalty != 0 && anyCrisis)
            {
                total += crisisPenalty;
                sb.Append(" − 危机(").Append(Signed(crisisPenalty)).Append(")");
            }
            if (corruptionPenalty != 0 && corruption)
            {
                total += corruptionPenalty;
                sb.Append(" − 腐败(").Append(Signed(corruptionPenalty)).Append(")");
            }
            if (unrestPenalty != 0 && protests)
            {
                total += unrestPenalty;
                sb.Append(" − 和平抗议(").Append(Signed(unrestPenalty)).Append(")");
            }
            if (revoltPenalty != 0 && revolt)
            {
                total += revoltPenalty;
                sb.Append(" − 全国起义(").Append(Signed(revoltPenalty)).Append(")");
            }

            int raw = total;
            if (total < floor)
            {
                total = floor;
                sb.Append(" → 触底取 ").Append(floor);
            }
            sb.Append(" = ").Append(total);
            if (raw != total) sb.Append("（原始 ").Append(raw).Append("）");
            detail = sb.ToString();
            return total;
        }

        private static string Signed(int v) { return v >= 0 ? ("+" + v) : v.ToString(); }
    }
}
