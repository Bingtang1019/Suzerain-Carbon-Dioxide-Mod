using System;
using System.Collections.Generic;
using System.Text;

namespace Oikonomia
{
    
    internal enum QuoteEffect
    {
        Fade = 0,       
        Subtle = 1,     
        Electric = 2,   
        Crt = 3         
    }

    internal static class QuoteAnimation
    {
        internal static readonly QuoteEffect[] All =
        {
            QuoteEffect.Fade, QuoteEffect.Subtle, QuoteEffect.Electric, QuoteEffect.Crt
        };

        private static readonly char[] GlitchChars = { '▓', '▒', '░', '█', '＃' };
        private static readonly char[] PixelChars = { '░', '▒', '▓', '█' };

        
        
        private static readonly QuoteEffect[,] CellEffects =
        {
            { QuoteEffect.Crt,      QuoteEffect.Crt,      QuoteEffect.Electric, QuoteEffect.Subtle },
            { QuoteEffect.Crt,      QuoteEffect.Electric, QuoteEffect.Fade,     QuoteEffect.Subtle },
            { QuoteEffect.Electric, QuoteEffect.Fade,     QuoteEffect.Fade,     QuoteEffect.Subtle },
            { QuoteEffect.Crt,      QuoteEffect.Electric, QuoteEffect.Fade,     QuoteEffect.Subtle }
        };

        private static readonly Dictionary<string, QuoteEffect> Specials =
            new Dictionary<string, QuoteEffect>(StringComparer.Ordinal)
            {
                { "nuclear_war", QuoteEffect.Fade },
                { "rumburg_war_lost", QuoteEffect.Crt },
                { "rumburg_war_win", QuoteEffect.Electric },
                { "black_tuesday", QuoteEffect.Crt },
                { "global_trade_war", QuoteEffect.Subtle }
            };

        internal static string Name(QuoteEffect effect)
        {
            switch (effect)
            {
                case QuoteEffect.Fade: return "Fade";
                case QuoteEffect.Subtle: return "Subtle";
                case QuoteEffect.Electric: return "Electric";
                case QuoteEffect.Crt: return "Crt";
                default: return "Fade";
            }
        }

        internal static QuoteEffect ForCell(int xIndex, int yIndex)
        {
            return CellEffects[(xIndex % 4 + 4) % 4, (yIndex % 4 + 4) % 4];
        }

        internal static QuoteEffect ForId(string id)
        {
            QuoteEffect e;
            if (!string.IsNullOrEmpty(id) && Specials.TryGetValue(id, out e)) return e;
            uint h = 2166136261u;
            if (!string.IsNullOrEmpty(id))
            {
                for (int i = 0; i < id.Length; i++) { h ^= id[i]; h *= 16777619u; }
            }
            return All[(int)(h & 3)];
        }

        
        internal const int BodyTickFrames = 10;
        internal const int BodyStepCount = 15;
        internal const int BodyTotalFrames = BodyTickFrames * BodyStepCount;

        
        internal static bool BodyAdvanceAt(int tick)
        {
            return tick > 0 && tick % BodyTickFrames == 0;
        }

        
        internal static bool BodySwitchAt(int tick)
        {
            return tick >= BodyTotalFrames;
        }

        internal static int FrameCount(QuoteEffect effect)
        {
            switch (effect)
            {
                case QuoteEffect.Fade: return 8;
                case QuoteEffect.Subtle: return 9;
                case QuoteEffect.Electric: return 11;
                case QuoteEffect.Crt: return 64; 
                default: return 8;
            }
        }

        
        internal static string Stable(string quote)
        {
            return "<size=18><i>" + (quote ?? "") + "</i></size>";
        }

        
        internal const string SignalMarker = "[Signal Interruption]";

        
        internal const int SignalTransitionFrames = 30;
        
        private const int SignalStepFrames = 12;
        private const int SignalFirstGlitchFrame = 2;

        
        private static int SignalStepCount()
        {
            int span = SignalTransitionFrames - SignalFirstGlitchFrame;
            return (span + SignalStepFrames - 1) / SignalStepFrames;
        }

        
        internal static int SignalStep(int frame)
        {
            if (frame < SignalFirstGlitchFrame) return 0;
            int step = 1 + (frame - SignalFirstGlitchFrame) / SignalStepFrames;
            int max = SignalStepCount();
            return step > max ? max : step;
        }

        
        internal static List<int> SignalSlots()
        {
            var slots = new List<int>();
            for (int i = 1; i < SignalMarker.Length - 1; i++)
                if (SignalMarker[i] != ' ') slots.Add(i);
            uint seed = Fnv(SignalMarker + "|transition|slots");
            for (int i = slots.Count - 1; i > 0; i--)
            {
                seed = seed * 1664525u + 1013904223u;
                int j = (int)(seed % (uint)(i + 1));
                int t = slots[i];
                slots[i] = slots[j];
                slots[j] = t;
            }
            return slots;
        }

        
        
        
        
        internal static HashSet<int> SignalMask(int frame)
        {
            var mask = new HashSet<int>();
            int step = SignalStep(frame);
            if (step <= 0) return mask;
            var slots = SignalSlots();
            int total = SignalStepCount();
            
            
            
            int target = (slots.Count * step + total - 1) / total;
            for (int i = 0; i < target && i < slots.Count; i++) mask.Add(slots[i]);
            return mask;
        }

        
        private static char SignalGlyph(int index)
        {
            uint h = Fnv(SignalMarker + "|transition|" + index);
            return PixelChars[(int)(h % (uint)PixelChars.Length)];
        }

        
        
        
        
        internal static string SignalTransition(int frame)
        {
            if (frame < 0) frame = 0;
            bool invisible = frame > 0 && frame % 5 == 0;
            int alpha = invisible ? 0 : (frame % 3 == 0 ? 205 : 255);
            string marker = SignalMarker;
            if (!invisible)
            {
                var mask = SignalMask(frame);
                if (mask.Count > 0)
                {
                    var chars = marker.ToCharArray();
                    foreach (int idx in mask) chars[idx] = SignalGlyph(idx);
                    marker = new string(chars);
                }
                
                marker += " " + new string(PixelChars[frame % PixelChars.Length], 1 + frame % 3);
            }
            return "<align=center><alpha=#" + alpha.ToString("X2")
                + "><size=20><b>" + marker + "</b></size></align>";
        }

        
        internal static string FadeTransition(string quote, int frame)
        {
            int a = 255 - Math.Max(0, Math.Min(30, frame)) * 8;
            if (a < 0) a = 0;
            return "<size=18><i><alpha=#" + a.ToString("X2") + ">"
                + (quote ?? "") + "<alpha=#FF></i></size>";
        }

        
        internal static string SubtleTransition(string quote, int frame)
        {
            int a = 235 - (int)(18 * Math.Abs(Math.Sin(frame / 4.0)));
            return "<size=18><i><alpha=#" + a.ToString("X2") + ">"
                + (quote ?? "") + "<alpha=#FF></i></size>";
        }

        
        internal static string Frame(string quote, QuoteEffect effect, int frame, int total)
        {
            quote = quote ?? "";
            if (total <= 1 || frame <= 0 || frame >= total) return Stable(quote);

            
            string source = effect == QuoteEffect.Electric ? PixelizeBody(quote, frame) : quote;
            string body;
            switch (effect)
            {
                case QuoteEffect.Fade:
                    body = FadeBody(source, frame, total);
                    break;
                case QuoteEffect.Subtle:
                    body = SubtleBody(source, frame, total);
                    break;
                case QuoteEffect.Electric:
                    body = ElectricBody(source, frame, total);
                    break;
                case QuoteEffect.Crt:
                    body = CrtBody(quote, frame);
                    break;
                default:
                    body = source;
                    break;
            }
            return "<size=18><i>" + body + "</i></size>";
        }

        private static string FadeBody(string quote, int frame, int total)
        {
            
            int[] seq = { 0, 20, 60, 120, 200, 255, 90, 255 };
            int a = seq[(frame - 1) % seq.Length];
            return "<alpha=#" + a.ToString("X2") + ">" + quote + "<alpha=#FF>";
        }

        private static string SubtleBody(string quote, int frame, int total)
        {
            if (quote.Length == 0) return quote;
            
            int a = 230 - ((frame * 17) % 50);
            int span = Math.Max(2, quote.Length / 5);
            int start = ((frame - 1) * Math.Max(1, quote.Length / 4)) % Math.Max(1, quote.Length);
            int len = Math.Min(span, quote.Length - start);
            if (len <= 0) return quote;
            return quote.Substring(0, start)
                + "<alpha=#" + a.ToString("X2") + ">" + quote.Substring(start, len) + "<alpha=#FF>"
                + quote.Substring(start + len);
        }

        private static string ElectricBody(string quote, int frame, int total)
        {
            if (quote.Length == 0) return quote;
            
            int count = 2 + (frame % 2);
            var mask = new HashSet<int>();
            for (int k = 0; k < count; k++)
            {
                int pos = (frame * 13 + k * 7 + 3) % quote.Length;
                mask.Add(pos);
            }
            var sb = new StringBuilder(quote.Length);
            for (int i = 0; i < quote.Length; i++)
                sb.Append(mask.Contains(i) ? GlitchChars[(i + frame) % GlitchChars.Length] : quote[i]);
            return sb.ToString();
        }

        
        internal static HashSet<int> CrtMask(string quote, int frame)
        {
            var mask = new HashSet<int>();
            string body = SplitAttribution(quote, out _);
            int len = body.Length;
            if (len == 0) return mask;

            for (int step = 1; step <= frame && mask.Count < len; step++)
            {
                uint h = Fnv(body + "|" + step);
                int count = 3 + (int)(h % 3); 
                for (int k = 0; k < count && mask.Count < len; k++)
                {
                    h = h * 1664525u + 1013904223u;
                    int idx = (int)(h % (uint)len);
                    while (mask.Contains(idx))
                    {
                        idx++;
                        if (idx >= len) idx = 0;
                    }
                    mask.Add(idx);
                }
            }
            return mask;
        }

        
        internal static bool CrtFullyGlitched(string quote, int frame)
        {
            string body = SplitAttribution(quote, out _);
            return body.Length > 0 && CrtMask(quote, frame).Count >= body.Length;
        }

        private static string CrtBody(string quote, int frame)
        {
            string body = SplitAttribution(quote, out string tail);
            var mask = CrtMask(quote, frame);
            var sb = new StringBuilder(body.Length + tail.Length);
            for (int i = 0; i < body.Length; i++)
            {
                if (mask.Contains(i))
                {
                    
                    uint h = Fnv(body + "|" + i);
                    sb.Append(GlitchChars[(int)(h % (uint)GlitchChars.Length)]);
                }
                else sb.Append(body[i]);
            }
            sb.Append(tail);
            return sb.ToString();
        }

        private static string PixelizeBody(string quote, int frame)
        {
            string body = SplitAttribution(quote, out string tail);
            if (body.Length == 0) return quote;
            var chars = body.ToCharArray();
            int count = 1 + (frame % 2);
            for (int k = 0; k < count; k++)
            {
                uint h = Fnv(body + "|pixel|" + frame + "|" + k);
                int idx = (int)(h % (uint)chars.Length);
                chars[idx] = PixelChars[(int)(h % (uint)PixelChars.Length)];
            }
            return new string(chars) + tail;
        }

        private static string SplitAttribution(string quote, out string tail)
        {
            quote = quote ?? "";
            int i = quote.IndexOf("\n——", StringComparison.Ordinal);
            if (i < 0)
            {
                tail = "";
                return quote;
            }
            tail = quote.Substring(i);
            return quote.Substring(0, i);
        }

        private static uint Fnv(string s)
        {
            uint h = 2166136261u;
            if (s == null) return h;
            for (int i = 0; i < s.Length; i++) { h ^= s[i]; h *= 16777619u; }
            return h;
        }
    }
}
