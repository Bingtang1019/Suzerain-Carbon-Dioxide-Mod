using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace Oikonomia.Probe
{
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    internal static class ProbeHealth
    {
        private static readonly object _lock = new object();

        public static string Dir
        {
            get
            {
                return Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "Oikonomia");
            }
        }

        public static string FilePath { get { return Path.Combine(Dir, "probe-health.json"); } }

        public static bool PreviousFound;
        public static bool PreviousCleanExit = true;
        public static int PreviousStreak;
        public static int Sessions;
        public static int CleanExits;
        public static bool SignalAvailable;

        
        public static int EffectiveTier = ProbeTier.Safe;
        
        public static string Reason = "";

        private static bool _cleanMarked;
        private static string[] _mounts = new string[0];

        
        
        
        public static int BeginSession(int requestedTier, bool autoDegrade, bool signalOverride)
        {
            lock (_lock)
            {
                ReadPrevious();
                Sessions = SessionsPrev + 1;
                CleanExits = CleanExitsPrev;
                SignalAvailable = CleanExits > 0;

                int streak = 0;
                if (PreviousFound && !PreviousCleanExit) streak = PreviousStreak + 1;
                PreviousStreak = streak;

                int effective = requestedTier;
                string reason = "";

                bool signalUnknown = (Sessions >= 2 && CleanExits == 0);
                if (autoDegrade && signalUnknown && !signalOverride)
                {
                    if (requestedTier > ProbeTier.Safe)
                    {
                        effective = ProbeTier.Safe;
                        reason = "干净退出信号从未观测到（sessions=" + Sessions + ", cleanExits=0）"
                                 + " ⇒ 不能把「未清标记」当崩溃证据，上限钉在安全层"
                                 + "（要强制完整层：oi-probe.json 的 health.signalOverride=true）";
                    }
                    else
                    {
                        effective = ProbeTier.Safe;
                        reason = "干净退出信号从未观测到（sessions=" + Sessions + ", cleanExits=0）"
                                 + " ⇒ 保持安全层运行，不自我禁用";
                    }
                }
                else if (autoDegrade && streak >= 2)
                {
                    effective = ProbeTier.Off;
                    reason = "连续 " + streak + " 次未正常退出（lastCleanExit=false）⇒ 自愈：**完全禁用自己**";
                }
                else if (autoDegrade && streak == 1)
                {
                    effective = Math.Min(requestedTier, ProbeTier.Safe);
                    reason = "上次未正常退出（lastCleanExit=false）⇒ 自愈：自动降级到安全层"
                             + (requestedTier > ProbeTier.Safe ? "（本次请求的是 " + ProbeTier.Name(requestedTier) + "）" : "");
                }
                else if (streak > 0)
                {
                    reason = "上次未正常退出，但 autoDegrade=false ⇒ 不降级（用户显式关闭自愈）";
                }

                EffectiveTier = effective;
                Reason = reason;
                _cleanMarked = false;
                WriteNow();
                return effective;
            }
        }

        
        public static void MarkCleanExit(string why)
        {
            lock (_lock)
            {
                if (_cleanMarked) return;
                _cleanMarked = true;
                CleanExits = CleanExits + 1;
                WriteNow();
            }
            try
            {
                ProbeLog.Mark("[OI][③] 已标记「正常退出」（" + why + "）→ 下次启动不会降级"
                              + " ｜ sessions=" + Sessions + " cleanExits=" + CleanExits);
            }
            catch { }
        }

        
        public static void SetMounts(string[] mounts)
        {
            lock (_lock) { _mounts = mounts ?? new string[0]; }
            WriteNow();
        }

        public static void WriteNow()
        {
            try
            {
                Directory.CreateDirectory(Dir);
                var sb = new StringBuilder();
                sb.Append("{\n");
                sb.Append("  \"sessionStart\": ").Append(J(_sessionStart)).Append(",\n");
                sb.Append("  \"pid\": ").Append(_pid.ToString(CultureInfo.InvariantCulture)).Append(",\n");
                sb.Append("  \"lastCleanExit\": ").Append(_cleanMarked ? "true" : "false").Append(",\n");
                sb.Append("  \"tier\": ").Append(J(ProbeTier.Name(EffectiveTier))).Append(",\n");
                sb.Append("  \"crashStreak\": ").Append(PreviousStreak.ToString(CultureInfo.InvariantCulture)).Append(",\n");
                sb.Append("  \"sessions\": ").Append(Sessions.ToString(CultureInfo.InvariantCulture)).Append(",\n");
                sb.Append("  \"cleanExits\": ").Append(CleanExits.ToString(CultureInfo.InvariantCulture)).Append(",\n");
                sb.Append("  \"signalAvailable\": ").Append(CleanExits > 0 ? "true" : "false").Append(",\n");
                sb.Append("  \"downgradeReason\": ").Append(J(Reason)).Append(",\n");
                sb.Append("  \"mounts\": [");
                for (int i = 0; i < _mounts.Length; i++)
                {
                    if (i > 0) sb.Append(", ");
                    sb.Append(J(_mounts[i]));
                }
                sb.Append("]\n}\n");
                File.WriteAllText(FilePath, sb.ToString(), new UTF8Encoding(false));
            }
            catch { }
        }

        

        private static string _sessionStart = "";

        private static int SessionsPrev;
        private static int CleanExitsPrev;

        private static void ReadPrevious()
        {
            PreviousFound = false;
            PreviousCleanExit = true;
            PreviousStreak = 0;
            SessionsPrev = 0;
            CleanExitsPrev = 0;
            _sessionStart = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
            _pid = 0;
            try { _pid = System.Diagnostics.Process.GetCurrentProcess().Id; } catch { }
            try
            {
                if (!File.Exists(FilePath)) return;
                var raw = File.ReadAllText(FilePath, Encoding.UTF8);
                if (string.IsNullOrEmpty(raw)) return;
                PreviousFound = true;
                var m = Regex.Match(raw, "\"lastCleanExit\"\\s*:\\s*(true|false)", RegexOptions.IgnoreCase);
                if (m.Success) PreviousCleanExit = m.Groups[1].Value.Equals("true", StringComparison.OrdinalIgnoreCase);
                else PreviousCleanExit = true;                       
                PreviousStreak = ReadInt(raw, "crashStreak", 0);
                SessionsPrev = ReadInt(raw, "sessions", 0);
                CleanExitsPrev = ReadInt(raw, "cleanExits", 0);
            }
            catch { }
        }

        private static int _pid;

        private static int ReadInt(string raw, string key, int dflt)
        {
            try
            {
                var m = Regex.Match(raw, "\"" + Regex.Escape(key) + "\"\\s*:\\s*(\\d+)", RegexOptions.IgnoreCase);
                if (m.Success)
                {
                    int v;
                    if (int.TryParse(m.Groups[1].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out v)) return v;
                }
            }
            catch { }
            return dflt;
        }

        private static string J(string s)
        {
            if (s == null) return "\"\"";
            var sb = new StringBuilder("\"");
            foreach (var ch in s)
            {
                switch (ch)
                {
                    case '"': sb.Append("\\\""); break;
                    case '\\': sb.Append("\\\\"); break;
                    case '\n': sb.Append("\\n"); break;
                    case '\r': sb.Append("\\r"); break;
                    default: sb.Append(ch); break;
                }
            }
            sb.Append('"');
            return sb.ToString();
        }
    }
}
