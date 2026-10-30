using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace Oikonomia.Probe
{
    
    
    
    
    
    
    internal static class ProbeStatus
    {
        private sealed class Row
        {
            public string Id;
            public string State;
            public string Target;
            public string Note;
        }

        private static readonly object _lock = new object();
        private static readonly List<Row> _rows = new List<Row>();
        private static readonly Dictionary<string, int> _index = new Dictionary<string, int>(StringComparer.Ordinal);

        private static string _path;
        private static string _tier = "?";
        private static string _tierSource = "";
        private static string _reason = "";
        private static string _sessionStart = "";
        private static string _pid = "";
        private static bool _lastCleanExit = true;
        private static int _crashStreak;

        public static string Path { get { lock (_lock) { return _path; } } }

        public static void Begin(string path, string tier, string tierSource, string reason,
            bool lastCleanExit, int crashStreak)
        {
            lock (_lock)
            {
                _path = path;
                _tier = tier;
                _tierSource = tierSource;
                _reason = reason;
                _lastCleanExit = lastCleanExit;
                _crashStreak = crashStreak;
                _sessionStart = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
                try { _pid = System.Diagnostics.Process.GetCurrentProcess().Id.ToString(CultureInfo.InvariantCulture); }
                catch { _pid = "?"; }
            }
            Flush();
        }

        
        public static void Record(string id, string state, string target, string note)
        {
            if (string.IsNullOrEmpty(id)) return;
            lock (_lock)
            {
                int i;
                if (_index.TryGetValue(id, out i))
                {
                    var r = _rows[i];
                    r.State = state ?? r.State;
                    if (!string.IsNullOrEmpty(target)) r.Target = target;
                    r.Note = note ?? "";
                }
                else
                {
                    _index[id] = _rows.Count;
                    _rows.Add(new Row { Id = id, State = state ?? "?", Target = target ?? "", Note = note ?? "" });
                }
            }
            Flush();
        }

        public static int Count(string state)
        {
            int n = 0;
            lock (_lock)
            {
                foreach (var r in _rows)
                    if (string.Equals(r.State, state, StringComparison.Ordinal)) n++;
            }
            return n;
        }

        public static int Total { get { lock (_lock) { return _rows.Count; } } }

        public static string OneLine()
        {
            return "钩子 " + Total + " 条：已挂 " + Count("mounted")
                   + " / 跳过 " + Count("skipped")
                   + " / 失败 " + Count("failed")
                   + " / 运行期卸下 " + Count("unmounted");
        }

        
        public static string[] MountedTargets()
        {
            var list = new List<string>();
            lock (_lock)
            {
                foreach (var r in _rows)
                    if (string.Equals(r.State, "mounted", StringComparison.Ordinal)) list.Add(r.Target);
            }
            return list.ToArray();
        }

        public static void Flush()
        {
            string path;
            Row[] rows;
            string tier, src, reason, start, pid;
            bool clean;
            int streak;
            lock (_lock)
            {
                path = _path;
                rows = _rows.ToArray();
                tier = _tier; src = _tierSource; reason = _reason;
                start = _sessionStart; pid = _pid; clean = _lastCleanExit; streak = _crashStreak;
            }
            if (string.IsNullOrEmpty(path)) return;
            try
            {
                var sb = new StringBuilder();
                sb.Append("{\n");
                sb.Append("  \"updated\": ").Append(J(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture))).Append(",\n");
                sb.Append("  \"sessionStart\": ").Append(J(start)).Append(",\n");
                sb.Append("  \"pid\": ").Append(J(pid)).Append(",\n");
                sb.Append("  \"tier\": ").Append(J(tier)).Append(",\n");
                sb.Append("  \"tierSource\": ").Append(J(src)).Append(",\n");
                sb.Append("  \"downgradeReason\": ").Append(J(reason)).Append(",\n");
                sb.Append("  \"lastCleanExit\": ").Append(clean ? "true" : "false").Append(",\n");
                sb.Append("  \"crashStreak\": ").Append(streak.ToString(CultureInfo.InvariantCulture)).Append(",\n");
                sb.Append("  \"capabilities\": { \"nativeChannel\": ").Append(ProbeTier.NativeChannel ? "true" : "false");
                sb.Append(", \"interopTmpHooks\": ").Append(ProbeTier.TmpHooks ? "true" : "false");
                sb.Append(", \"interopLoadFrom\": ").Append(SafeResolve.AllowInteropLoadFrom ? "true" : "false");
                sb.Append(", \"patchOnForeign\": ").Append(SafeResolve.AllowForeignPatchStack ? "true" : "false");
                sb.Append(", \"collect\": ").Append(ProbeTier.Collect ? "true" : "false").Append(" },\n");
                sb.Append("  \"summary\": ").Append(J(OneLine())).Append(",\n");
                sb.Append("  \"hooks\": [\n");
                for (int i = 0; i < rows.Length; i++)
                {
                    sb.Append("    { \"id\": ").Append(J(rows[i].Id))
                      .Append(", \"state\": ").Append(J(rows[i].State))
                      .Append(", \"target\": ").Append(J(rows[i].Target))
                      .Append(", \"note\": ").Append(J(rows[i].Note)).Append(" }");
                    if (i < rows.Length - 1) sb.Append(',');
                    sb.Append('\n');
                }
                sb.Append("  ]\n}\n");
                File.WriteAllText(path, sb.ToString(), new UTF8Encoding(false));
            }
            catch { }
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
                    case '\t': sb.Append("\\t"); break;
                    default:
                        if (ch < ' ') sb.Append("\\u").Append(((int)ch).ToString("x4", CultureInfo.InvariantCulture));
                        else sb.Append(ch);
                        break;
                }
            }
            sb.Append('"');
            return sb.ToString();
        }
    }
}
