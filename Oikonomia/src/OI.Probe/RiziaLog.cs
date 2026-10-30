using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace Oikonomia.Probe
{
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    internal static class RiziaLog
    {
        
        
        public static volatile bool Enabled = true;
        
        public static volatile bool Force = false;
        
        public static volatile bool AllStats = false;
        
        public static volatile int MaxStatDumps = 3;
        
        public static volatile int MaxBytes = 2 * 1024 * 1024;
        
        public static volatile int MaxTimeline = 120;

        private static readonly object _lock = new object();
        private static readonly object _cfgLock = new object();

        private static string _logPath, _archivePath, _textDir, _cfgPath, _oiCfgPath;
        private static string _lastCfgRaw, _lastOiRaw;
        private static long _bytes;
        private static int _rotations;
        private static readonly DateTime _t0 = DateTime.UtcNow;

        private static readonly HashSet<string> _once = new HashSet<string>(StringComparer.Ordinal);
        private static readonly Dictionary<string, int> _hits = new Dictionary<string, int>(StringComparer.Ordinal);
        private static readonly HashSet<string> _textFiles = new HashSet<string>(StringComparer.Ordinal);
        private static int _timelineLines;
        private static Timer _timer;
        private static bool _ready;

        public static string LogPath { get { return _logPath; } }
        public static string ArchivePath { get { return _archivePath; } }
        public static string TextDir { get { return _textDir; } }
        public static string CfgPath { get { return _cfgPath; } }
        public static string OiCfgPath { get { return _oiCfgPath; } }
        public static int Rotations { get { return _rotations; } }
        public static long BytesWritten { get { return Interlocked.Read(ref _bytes); } }
        public static int TimelineLines { get { return _timelineLines; } }
        public static int DistinctOnceKeys { get { lock (_lock) return _once.Count; } }

        
        public static long SinceMs
        {
            get { return (long)(DateTime.UtcNow - _t0).TotalMilliseconds; }
        }

        
        
        

        public static void Init()
        {
            try
            {
                var dir = ProbeLog.Dir;                       
                Directory.CreateDirectory(dir);
                _logPath = Path.Combine(dir, "rizia-ui-probe.log");
                _archivePath = Path.Combine(dir, "rizia-ui-probe.1.log");
                _textDir = Path.Combine(dir, "rizia-ui-text");
                _cfgPath = Path.Combine(dir, "rizia-probe.json");
                _oiCfgPath = Path.Combine(dir, "oi-probe.json");

                if (!File.Exists(_cfgPath))
                {
                    File.WriteAllText(_cfgPath,
                        "{\n"
                        + "  \"//\": \"Rizia 实况探针（R 组）开关 —— 每秒热重载。只读采集，绝不修改游戏对象。\",\n"
                        + "  \"enabled\": true,\n"
                        + "  \"force\": false,\n"
                        + "  \"allStats\": false,\n"
                        + "  \"maxStatDumps\": 3,\n"
                        + "  \"maxLogBytes\": 2097152,\n"
                        + "  \"maxTimeline\": 120\n"
                        + "}\n",
                        new UTF8Encoding(false));
                }

                ReadConfig(initial: true);
                _ready = true;
                _timer = new Timer(_ => Poll(), null, 1000, 1000);
            }
            catch (Exception e)
            {
                try { ProbeLog.Error("[R] 日志初始化失败：" + e.Message); } catch { }
            }
        }

        public static bool Ready { get { return _ready; } }

        private static void Poll()
        {
            try
            {
                lock (_cfgLock)
                {
                    var changed = ReadConfig(initial: false);
                    if (changed)
                        Line("★开关变更 → enabled=" + Enabled + " force=" + Force
                            + " allStats=" + AllStats + " maxStatDumps=" + MaxStatDumps
                            + " maxLogBytes=" + MaxBytes + " maxTimeline=" + MaxTimeline);
                }
            }
            catch { }
        }

        
        private static bool ReadConfig(bool initial)
        {
            bool oE = Enabled, oF = Force, oA = AllStats;
            int oM = MaxStatDumps, oB = MaxBytes, oT = MaxTimeline;

            string a = null, b = null;
            try { if (File.Exists(_cfgPath)) a = File.ReadAllText(_cfgPath, Encoding.UTF8); } catch { }
            try { if (File.Exists(_oiCfgPath)) b = File.ReadAllText(_oiCfgPath, Encoding.UTF8); } catch { }

            if (!initial && string.Equals(a, _lastCfgRaw, StringComparison.Ordinal)
                && string.Equals(b, _lastOiRaw, StringComparison.Ordinal))
                return false;

            _lastCfgRaw = a; _lastOiRaw = b;
            var raw = (a ?? "") + "\n" + (b ?? "");   

            Enabled = ReadBool(raw, "enabled", ReadBool(raw, "riziaProbe", Enabled));
            Force = ReadBool(raw, "force", Force);
            AllStats = ReadBool(raw, "allStats", AllStats);
            MaxStatDumps = ReadInt(raw, "maxStatDumps", MaxStatDumps);
            MaxBytes = ReadInt(raw, "maxLogBytes", MaxBytes);
            MaxTimeline = ReadInt(raw, "maxTimeline", MaxTimeline);
            if (MaxBytes < 64 * 1024) MaxBytes = 64 * 1024;
            if (MaxStatDumps < 1) MaxStatDumps = 1;
            if (MaxTimeline < 10) MaxTimeline = 10;

            return !(oE == Enabled && oF == Force && oA == AllStats
                     && oM == MaxStatDumps && oB == MaxBytes && oT == MaxTimeline);
        }

        private static bool ReadBool(string raw, string key, bool dflt)
        {
            try
            {
                var m = Regex.Match(raw, "\"" + Regex.Escape(key) + "\"\\s*:\\s*(true|false)", RegexOptions.IgnoreCase);
                if (m.Success) return m.Groups[1].Value.Equals("true", StringComparison.OrdinalIgnoreCase);
            }
            catch { }
            return dflt;
        }

        private static int ReadInt(string raw, string key, int dflt)
        {
            try
            {
                var m = Regex.Match(raw, "\"" + Regex.Escape(key) + "\"\\s*:\\s*(\\d+)", RegexOptions.IgnoreCase);
                if (m.Success) { int v; if (int.TryParse(m.Groups[1].Value, out v)) return v; }
            }
            catch { }
            return dflt;
        }

        
        
        

        
        public static bool Once(string key)
        {
            lock (_lock)
            {
                if (_once.Contains(key)) return false;
                _once.Add(key);
                return true;
            }
        }

        
        public static bool Counted(string key, int firstN)
        {
            lock (_lock)
            {
                int n;
                _hits.TryGetValue(key, out n);
                n++;
                _hits[key] = n;
                if (n <= firstN) return true;
                if (n == firstN + 1)
                    Raw("   ↳（同类事件已出现 " + (firstN + 1) + " 次，后续**仅计数不落盘**；总计见 [R-SUMMARY]）");
                return false;
            }
        }

        public static int Hits(string key)
        {
            lock (_lock) { int n; _hits.TryGetValue(key, out n); return n; }
        }

        
        public static List<KeyValuePair<string, int>> HitsSnapshot()
        {
            lock (_lock) return new List<KeyValuePair<string, int>>(_hits);
        }

        public static void Raw(string msg)
        {
            Append(msg + Environment.NewLine);
        }

        public static void Line(string msg)
        {
            Append("[" + Stamp() + "] " + msg + Environment.NewLine);
        }

        public static void Mark(string msg)
        {
            Append("[R*" + Stamp() + "] " + msg + Environment.NewLine);
        }

        public static void Error(string msg)
        {
            Append("[R!" + Stamp() + "] " + msg + Environment.NewLine);
        }

        
        public static bool Timeline(string msg)
        {
            lock (_lock)
            {
                if (_timelineLines >= MaxTimeline)
                {
                    if (_timelineLines == MaxTimeline)
                    {
                        _timelineLines++;
                        Append("[R+" + Stamp() + "] （时间线已达上限 " + MaxTimeline + " 行，后续仅计数；调大 rizia-probe.json 的 maxTimeline 可放开）"
                               + Environment.NewLine);
                    }
                    return false;
                }
                _timelineLines++;
            }
            Append("[R+" + Stamp() + "] " + msg + Environment.NewLine);
            return true;
        }

        public static void Section(string title)
        {
            Raw("");
            Raw("────────────────────────────────────────────────────────────────");
            Raw("【" + title + "】");
            Raw("────────────────────────────────────────────────────────────────");
        }

        private static string Stamp()
        {
            return "+" + SinceMs.ToString(CultureInfo.InvariantCulture) + "ms "
                 + DateTime.Now.ToString("HH:mm:ss.fff", CultureInfo.InvariantCulture);
        }

        private static void Append(string text)
        {
            if (text == null) return;
            lock (_lock)
            {
                try
                {
                    if (_logPath == null) return;
                    RotateIfNeeded(Encoding.UTF8.GetByteCount(text));
                    File.AppendAllText(_logPath, text, new UTF8Encoding(false));
                    _bytes += Encoding.UTF8.GetByteCount(text);
                }
                catch { }
            }
        }

        
        private static void RotateIfNeeded(int incoming)
        {
            try
            {
                long len = 0;
                var fi = new FileInfo(_logPath);
                if (fi.Exists) len = fi.Length;
                else len = _bytes;
                if (len + incoming <= MaxBytes) { _bytes = len; return; }

                if (File.Exists(_archivePath)) File.Delete(_archivePath);
                if (File.Exists(_logPath)) File.Move(_logPath, _archivePath);
                _rotations++;
                _bytes = 0;
                var note = "================================================================" + Environment.NewLine
                         + "[RiziaProbe] 日志已达上限（" + MaxBytes + " 字节）→ 上一份已归档到：" + Environment.NewLine
                         + "   " + _archivePath + Environment.NewLine
                         + "   本文件从零重新开始（第 " + _rotations + " 次归档）。" + Environment.NewLine
                         + "   要拿到更长的连续记录：把 rizia-probe.json 的 maxLogBytes 调大。"
                         + Environment.NewLine
                         + "================================================================" + Environment.NewLine;
                File.AppendAllText(_logPath, note, new UTF8Encoding(false));
                _bytes = Encoding.UTF8.GetByteCount(note);
            }
            catch { }
        }

        
        
        

        public static string Sha256Hex(string s)
        {
            try
            {
                using (var sha = SHA256.Create())
                {
                    var h = sha.ComputeHash(new UTF8Encoding(false).GetBytes(s ?? ""));
                    var sb = new StringBuilder(h.Length * 2);
                    for (int i = 0; i < h.Length; i++) sb.Append(h[i].ToString("x2", CultureInfo.InvariantCulture));
                    return sb.ToString();
                }
            }
            catch { return "(计算失败)"; }
        }

        public static int Utf8Len(string s)
        {
            try { return new UTF8Encoding(false).GetByteCount(s ?? ""); } catch { return -1; }
        }

        
        public static string Esc(string s)
        {
            if (s == null) return "(null)";
            var sb = new StringBuilder(s.Length + 16);
            foreach (var c in s)
            {
                switch (c)
                {
                    case '\n': sb.Append("\\n"); break;
                    case '\r': sb.Append("\\r"); break;
                    case '\t': sb.Append("\\t"); break;
                    case '\u000B': sb.Append("\\v"); break;
                    case '\u00A0': sb.Append("\\u00A0"); break;   
                    case '\u200B': sb.Append("\\u200B"); break;   
                    case '\uFEFF': sb.Append("\\uFEFF"); break;   
                    default:
                        if (c < 0x20 || c == 0x7F) sb.Append("\\u").Append(((int)c).ToString("x4", CultureInfo.InvariantCulture));
                        else sb.Append(c);
                        break;
                }
            }
            return sb.ToString();
        }

        
        public static string UnicodeEscape(string s, int maxChars)
        {
            if (s == null) return "(null)";
            int n = Math.Min(s.Length, maxChars);
            var sb = new StringBuilder(n * 6 + 32);
            for (int i = 0; i < n; i++)
            {
                char c = s[i];
                if (c >= 0x20 && c < 0x7F) sb.Append(c);
                else sb.Append("\\u").Append(((int)c).ToString("x4", CultureInfo.InvariantCulture));
            }
            if (s.Length > n) sb.Append("…(共 ").Append(s.Length).Append(" 字符，此处仅前 ").Append(n).Append(" 个)");
            return sb.ToString();
        }

        
        
        
        
        public static bool FullTextOnce(string slot, string sha, string text)
        {
            var key = slot + "|" + sha;
            lock (_lock)
            {
                if (_textFiles.Contains(key)) return false;
                _textFiles.Add(key);
            }
            try
            {
                if (_textDir == null) return true;
                Directory.CreateDirectory(_textDir);
                var f = Path.Combine(_textDir, Sanitize(slot) + "-" + Short(sha) + ".txt");
                
                File.WriteAllText(f, text ?? "", new UTF8Encoding(false));
            }
            catch { }
            return true;
        }

        public static string TextFilePath(string slot, string sha)
        {
            try { return Path.Combine(_textDir, Sanitize(slot) + "-" + Short(sha) + ".txt"); }
            catch { return "(不可用)"; }
        }

        private static string Short(string sha)
        {
            if (string.IsNullOrEmpty(sha)) return "00000000";
            return sha.Length <= 12 ? sha : sha.Substring(0, 12);
        }

        private static string Sanitize(string s)
        {
            if (string.IsNullOrEmpty(s)) return "x";
            var sb = new StringBuilder(s.Length);
            foreach (var c in s)
                sb.Append(char.IsLetterOrDigit(c) || c == '-' || c == '_' ? c : '_');
            return sb.ToString();
        }

        
        public static void WriteHeader(int typeOk, int typeTotal, string extra)
        {
            var sb = new StringBuilder();
            sb.AppendLine();
            sb.AppendLine("================================================================");
            sb.AppendLine("[RiziaProbe] Rizia 实况探针（R 组）会话开始 "
                          + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture));
            sb.AppendLine("  本探针用途 : 只读采集 Rizia 档「悬停预算」时的真实渲染文本 / 生产者调用 / 承载容器外观");
            sb.AppendLine("  本日志     : " + _logPath);
            sb.AppendLine("  归档文件   : " + _archivePath);
            sb.AppendLine("  全文目录   : " + _textDir + "\\<槽位>-<SHA前12>.txt（逐字节原文，UTF-8 无 BOM）");
            sb.AppendLine("  开关文件   : " + _cfgPath + "（主） / " + _oiCfgPath + "（同名键优先）");
            sb.AppendLine("  上限       : " + MaxBytes + " 字节；超出即归档重开（不会无限增长）");
            sb.AppendLine("  类型解析   : " + typeOk + " / " + typeTotal + " 个关键类型解析成功");
            sb.AppendLine("  开关值     : enabled=" + Enabled + " force=" + Force + " allStats=" + AllStats
                          + " maxStatDumps=" + MaxStatDumps + " maxTimeline=" + MaxTimeline);
            if (!string.IsNullOrEmpty(extra)) sb.AppendLine("  " + extra);
            sb.AppendLine("  ★ 只读承诺 : 全程不写游戏对象（不改文本 / 不改颜色 / 不改 active）");
            sb.AppendLine("================================================================");
            Append(sb.ToString());
        }

        
        public static void WriteSummary(string title, IEnumerable<string> lines)
        {
            var sb = new StringBuilder();
            sb.AppendLine();
            sb.AppendLine("【" + title + "】");
            sb.AppendLine("  写盘时间 : " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture)
                          + "（探针启动后 +" + SinceMs + " ms）");
            sb.AppendLine("  日志字节 : " + Interlocked.Read(ref _bytes) + " / " + MaxBytes + "  归档次数=" + _rotations);
            sb.AppendLine("  时间线行 : " + _timelineLines + " / " + MaxTimeline);
            if (lines != null)
                foreach (var l in lines) sb.AppendLine("  " + l);
            Append(sb.ToString());
        }
    }
}
