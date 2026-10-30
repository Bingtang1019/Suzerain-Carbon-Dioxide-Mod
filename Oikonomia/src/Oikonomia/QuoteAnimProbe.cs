using System;
using System.IO;
using System.Text;

namespace Oikonomia
{
    
    internal static class QuoteAnimProbe
    {
        private static readonly object Gate = new object();
        private static bool _enabled;
        private static string _path;
        private static int _lines;

        internal static bool Enabled { get { return _enabled; } }
        internal static string PathFile { get { return _path; } }

        internal static void Init(bool enabled, string dir)
        {
            _enabled = enabled;
            if (!enabled) return;
            try
            {
                if (string.IsNullOrEmpty(dir)) dir = Path.Combine(Path.GetTempPath(), "oik-quote-probe");
                Directory.CreateDirectory(dir);
                _path = Path.Combine(dir, "quote-anim-probe-" + DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".md");
                File.WriteAllText(_path,
                    "# Oikonomia 引文动画探针\n\n"
                    + "- 开始：" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "\n"
                    + "- 说明：一次悬停的节拍、状态、写入/回读、候选钩子全部记录在此。\n\n",
                    new UTF8Encoding(false));
            }
            catch { _enabled = false; _path = null; }
        }

        internal static void Line(string s)
        {
            if (!_enabled || _path == null || _lines >= 900) return;
            try
            {
                lock (Gate)
                {
                    if (_lines >= 900) return;
                    _lines++;
                    File.AppendAllText(_path,
                        "- [" + DateTime.Now.ToString("HH:mm:ss.fff") + "] " + (s ?? "") + "\n",
                        new UTF8Encoding(false));
                }
            }
            catch { }
        }

        internal static string Hash(string s)
        {
            if (s == null) return "null";
            uint h = 2166136261u;
            for (int i = 0; i < s.Length; i++) { h ^= s[i]; h *= 16777619u; }
            return h.ToString("X8");
        }

        internal static string Clip(string s, int max)
        {
            if (s == null) return "null";
            s = s.Replace("\r", "").Replace("\n", "\\n");
            return s.Length <= max ? s : s.Substring(0, max) + "…(" + s.Length + ")";
        }
    }
}
