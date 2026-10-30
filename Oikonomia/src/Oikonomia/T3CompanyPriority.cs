using System;

namespace Oikonomia
{
    
    
    
    
    
    
    
    
    
    
    
    
    internal static class T3CompanyPriority
    {
        internal const string Underhall = "Underhall";
        internal const string Taurus = "Taurus";
        internal const string Ssc = "SSC";

        
        internal static string Choose(string h3Company, string l1Company)
        {
            string h3 = Normalize(h3Company);
            string l1 = Normalize(l1Company);

            if (Is(h3, Underhall) || Is(l1, Underhall)) return Underhall;
            if (Is(h3, Taurus) || Is(l1, Taurus)) return Taurus;
            if (Is(h3, Ssc) || Is(l1, Ssc)) return Ssc;
            return null;
        }

        
        
        
        
        internal static string ChooseFromFlags(bool underhall, bool taurus, bool ssc)
        {
            if (underhall) return Underhall;
            if (taurus) return Taurus;
            if (ssc) return Ssc;
            return null;
        }

        
        internal static bool Matches(string conditionCompany, string priorityCompany)
        {
            if (string.IsNullOrEmpty(conditionCompany) || string.IsNullOrEmpty(priorityCompany)) return false;
            return string.Equals(Normalize(conditionCompany), Normalize(priorityCompany), StringComparison.Ordinal);
        }

        
        
        
        
        
        internal static bool IsT3Conversation(string conversationTitle)
        {
            if (string.IsNullOrEmpty(conversationTitle)) return false;
            bool turn3 = conversationTitle.IndexOf("Turn03", StringComparison.OrdinalIgnoreCase) >= 0
                      || conversationTitle.IndexOf("Turn3", StringComparison.OrdinalIgnoreCase) >= 0;
            if (!turn3) return false;
            
            
            
            return conversationTitle.IndexOf("Gus", StringComparison.OrdinalIgnoreCase) >= 0
                || conversationTitle.IndexOf("BusinessCouncil", StringComparison.OrdinalIgnoreCase) >= 0
                || conversationTitle.IndexOf("Tusk", StringComparison.OrdinalIgnoreCase) >= 0
                || conversationTitle.IndexOf("GasomProposal", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static bool Is(string value, string expected)
        {
            return string.Equals(value, expected, StringComparison.Ordinal);
        }

        private static string Normalize(string value)
        {
            if (string.IsNullOrEmpty(value)) return "";
            string s = value.Trim();
            if (string.Equals(s, "SSC", StringComparison.OrdinalIgnoreCase)) return Ssc;
            if (string.Equals(s, "Taurus", StringComparison.OrdinalIgnoreCase)) return Taurus;
            if (string.Equals(s, "Underhall", StringComparison.OrdinalIgnoreCase)) return Underhall;
            return s;
        }
    }
}
