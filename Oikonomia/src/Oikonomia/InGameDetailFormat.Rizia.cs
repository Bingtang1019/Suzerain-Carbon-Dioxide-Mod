














































































using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Oikonomia
{
    internal static partial class InGameDetailFormat
    {
        

        
        
        
        
        
        
        
        
        
        public const string RiziaPerTurnLabel = "未来";

        
        
        
        
        
        
        
        public const string RiziaThisTurnLabel = "当前";

        
        
        
        
        
        
        
        public const string RiziaBlockSeparator = "\n\n";

        
        
        
        
        
        
        
        public const string RiziaPositiveColor = "#0ED10E";

        
        
        
        
        public const string RiziaNegativeColor = "#D11111";

        
        public const string IndentOpen = "<indent=5%>";

        
        public const string IndentClose = "</indent>";

        

        
        
        
        
        
        
        
        
        
        public struct RiziaPalette
        {
            
            public string Positive;

            
            public string Negative;

            
            public RiziaPalette(string positive, string negative)
            {
                Positive = NormalizeHex(positive) ?? RiziaPositiveColor;
                Negative = NormalizeHex(negative) ?? RiziaNegativeColor;
            }

            
            public static RiziaPalette Measured { get { return new RiziaPalette(null, null); } }

            
            public static RiziaPalette Of(string positiveHex, string negativeHex)
            {
                return new RiziaPalette(positiveHex, negativeHex);
            }
        }

        
        
        
        
        
        public static string NormalizeHex(string s)
        {
            if (string.IsNullOrEmpty(s)) return null;
            var t = s.Trim();
            if (t.Length != 7 && t.Length != 9) return null;
            if (t[0] != '#') return null;
            for (int i = 1; i < t.Length; i++)
            {
                char c = t[i];
                bool hex = (c >= '0' && c <= '9') || (c >= 'a' && c <= 'f') || (c >= 'A' && c <= 'F');
                if (!hex) return null;
            }
            return t.Substring(0, 7).ToUpperInvariant();
        }

        
        
        
        
        
        
        
        
        
        
        
        
        
        public static string RgbaToHex(double r, double g, double b)
        {
            return "#" + ByteHex(r) + ByteHex(g) + ByteHex(b);
        }

        
        private static string ByteHex(double d)
        {
            int i = (int)Math.Round(d <= 1.0000001 && d >= 0 ? d * 255.0 : d);
            if (i < 0) i = 0;
            if (i > 255) i = 255;
            return i.ToString("X2", CultureInfo.InvariantCulture);
        }

        

        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        public static string EmitRiziaGroups(List<Row> perTurn, List<Row> oneTime, RiziaPalette palette, string title)
        {
            string a = EmitRiziaBlock(RiziaPerTurnLabel, perTurn, palette);      
            string b = EmitRiziaBlock(RiziaThisTurnLabel, oneTime, palette);     
            if (a == null && b == null) return null;                             

            string body;
            if (a == null) body = b;                    
            else if (b == null) body = a;               
            else body = a + RiziaBlockSeparator + b;    

            if (string.IsNullOrEmpty(title)) return body;
            return AlignCenter(Bold(Tint(ZeroColor, Escape(title)))) + LineSeparator   
                 + Tint(ZeroColor, TotalDivider) + LineSeparator                      
                 + body;
        }

        
        
        
        
        
        
        
        
        
        private static string EmitRiziaBlock(string label, List<Row> src, RiziaPalette palette)
        {
            var pos = new List<Row>();
            var neg = new List<Row>();
            CollectSignGroups(src, pos, neg);
            if (pos.Count == 0 && neg.Count == 0) return null;      

            SortByMagnitudeDesc(pos);
            SortByMagnitudeDesc(neg);

            var sb = new StringBuilder(64 + (pos.Count + neg.Count) * 48);
            sb.Append(label).Append(LineSeparator);                  
            if (pos.Count > 0)
            {
                AppendRiziaGroup(sb, pos, palette.Positive);
                if (neg.Count > 0) sb.Append(LineSeparator);         
            }
            if (neg.Count > 0) AppendRiziaGroup(sb, neg, palette.Negative);

            return sb.ToString();                                    
        }

        
        
        
        
        private static void CollectSignGroups(List<Row> src, List<Row> pos, List<Row> neg)
        {
            if (src == null) return;
            for (int i = 0; i < src.Count; i++)
            {
                int d = src[i].Delta;
                if (d > 0) pos.Add(src[i]);
                else if (d < 0) neg.Add(src[i]);        
            }
        }

        
        
        
        
        
        
        
        
        private static void AppendRiziaGroup(StringBuilder sb, List<Row> rows, string hex)
        {
            int sum = 0;
            for (int i = 0; i < rows.Count; i++) sum += rows[i].Delta;

            
            sb.Append(ColorTag(hex, ParenValue(Signed(sum), null)))
              .Append(' ')
              .Append(Underline(Escape(TotalLabel)))
              .Append(IndentOpen)
              .Append(LineSeparator);

            for (int i = 0; i < rows.Count; i++)
            {
                
                sb.Append(ColorTag(hex, ParenValue(Signed(rows[i].Delta), null)))
                  .Append(' ')
                  .Append(Escape(rows[i].Label));

                
                if (i == rows.Count - 1) sb.Append(IndentClose);
                else sb.Append(LineSeparator);
            }
        }

        
        
        
        
        
        private static void SortByMagnitudeDesc(List<Row> rows)
        {
            for (int i = 1; i < rows.Count; i++)
            {
                var cur = rows[i];
                long k = Magnitude(cur.Delta);
                int j = i - 1;
                while (j >= 0 && Magnitude(rows[j].Delta) < k)      
                {
                    rows[j + 1] = rows[j];
                    j--;
                }
                rows[j + 1] = cur;
            }
        }

        
        private static long Magnitude(int d)
        {
            return d < 0 ? -(long)d : (long)d;
        }
    }
}
