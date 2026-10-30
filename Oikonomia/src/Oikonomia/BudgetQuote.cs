using System;
using System.Collections.Generic;

namespace Oikonomia
{
    
    internal sealed class BudgetQuote
    {
        internal string Text;
        internal string Author;
        internal string Source;
    }

    
    
    
    
    
    
    
    
    
    
    
    
    internal static class BudgetQuoteMatrix
    {
        internal static readonly string[] XStates = { "Crisis", "Unstable", "Stable", "Wealthy" };
        internal static readonly string[] YStates = { "Collapsing", "Worsening", "Stagnant", "Improving" };

        private static readonly Dictionary<string, BudgetQuote[]> Cells =
            BudgetQuoteData.Cells ?? new Dictionary<string, BudgetQuote[]>();

        internal static int CellCount { get { return Cells.Count; } }

        internal static int QuoteCount
        {
            get
            {
                int n = 0;
                foreach (var kv in Cells) n += kv.Value.Length;
                return n;
            }
        }

        internal static string CellId(string x, string y) { return x + "_" + y; }

        
        internal static int XIndex(string x)
        {
            int i = Array.IndexOf(XStates, x);
            return i < 0 ? 0 : i;
        }

        internal static int YIndex(string y)
        {
            int i = Array.IndexOf(YStates, y);
            return i < 0 ? 0 : i;
        }

        internal static QuoteEffect EffectForBudget(double ratio, int delta)
        {
            return QuoteAnimation.ForCell(XIndex(XBand(ratio)), YIndex(YBand(delta)));
        }

        internal static string XBand(double ratio)
        {
            double r = ratio;
            if (double.IsNaN(r) || double.IsInfinity(r)) r = 0;
            if (r < 0) r = 0;
            if (r > 1) r = 1;
            if (r <= 0.20) return "Crisis";
            if (r <= 0.40) return "Unstable";  
            if (r <= 0.60) return "Stable";    
            return "Wealthy";                  
        }

        
        internal static string YBand(int delta)
        {
            if (delta > 2) return "Improving";   
            if (delta >= 0) return "Stagnant";   
            if (delta >= -2) return "Worsening"; 
            return "Collapsing";                 
        }

        internal static double Ratio(int budget, int min, int max)
        {
            if (max <= min) return 0.5;
            double r = (budget - (double)min) / (max - (double)min);
            if (double.IsNaN(r) || double.IsInfinity(r)) return 0.5;
            if (r < 0) return 0;
            if (r > 1) return 1;
            return r;
        }

        
        
        
        
        internal static BudgetQuote Select(int budget, int min, int max, int delta, int turn)
        {
            return Select(budget, min, max, delta, turn, 0);
        }

        internal static BudgetQuote Select(int budget, int min, int max, int delta, int turn, int variant)
        {
            string x = XBand(Ratio(budget, min, max));
            string y = YBand(delta);
            return SelectCell(x, y, turn, variant);
        }

        internal static BudgetQuote SelectCell(string x, string y, int turn)
        {
            return SelectCell(x, y, turn, 0);
        }

        internal static BudgetQuote SelectCell(string x, string y, int turn, int variant)
        {
            BudgetQuote[] arr;
            if (!Cells.TryGetValue(CellId(x, y), out arr) || arr == null || arr.Length == 0) return null;
            int idx = (turn + variant) % arr.Length;
            if (idx < 0) idx += arr.Length;
            return arr[idx];
        }

        
        internal static string Compose(BudgetQuote q, bool attributed)
        {
            if (q == null || string.IsNullOrEmpty(q.Text)) return "";
            string text = Sanitize(q.Text);
            if (!attributed) return text;
            string author = Sanitize(q.Author);
            string source = Sanitize(q.Source);
            string line = "";
            if (!string.IsNullOrEmpty(author)) line = "—— " + author;
            if (!string.IsNullOrEmpty(source))
                line += (line.Length > 0 ? "，" : "—— ") + (source.StartsWith("《", StringComparison.Ordinal) ? source : "《" + source + "》");
            return line.Length > 0 ? text + "\n" + line : text;
        }

        private static string Sanitize(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            
            return s.Replace('<', '＜').Replace('>', '＞').Replace("&", "＆").Trim();
        }


    }
}
