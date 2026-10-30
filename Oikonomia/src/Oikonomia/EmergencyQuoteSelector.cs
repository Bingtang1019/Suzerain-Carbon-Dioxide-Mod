using System;
using System.Collections.Generic;

namespace Oikonomia
{
    
    internal static class EmergencyQuoteSelector
    {
        
        
        
        internal static List<string> BuildTags(bool emergency, bool arcasia, bool contana, bool monarchy)
        {
            var tags = new List<string>();
            if (!emergency) return tags;
            if (!arcasia && !contana) monarchy = true;
            if (arcasia) tags.Add("arcasia");
            if (contana) tags.Add("contana");
            if (monarchy) tags.Add("monarchy");
            return tags;
        }

        internal static EmergencyQuoteGroup Select(IList<string> activeTags)
        {
            EmergencyQuoteGroup best = null;
            if (activeTags == null || activeTags.Count == 0) return null;
            foreach (var g in EmergencyQuoteData.Groups)
            {
                if (g == null || g.BannedTags == null || g.BannedTags.Length == 0) continue;
                bool all = true;
                foreach (var tag in g.BannedTags)
                {
                    if (!activeTags.Contains(tag)) { all = false; break; }
                }
                if (!all) continue;
                if (best == null || g.Priority > best.Priority
                    || (g.Priority == best.Priority && string.CompareOrdinal(g.Id, best.Id) < 0))
                    best = g;
            }
            return best;
        }

        internal static BudgetQuote Pick(EmergencyQuoteGroup group, int variant)
        {
            if (group == null || group.Quotes == null || group.Quotes.Length == 0) return null;
            int i = variant % group.Quotes.Length;
            if (i < 0) i += group.Quotes.Length;
            return group.Quotes[i];
        }

        internal static string TagList(IList<string> tags)
        {
            return tags == null || tags.Count == 0 ? "" : string.Join(",", tags);
        }
    }
}
