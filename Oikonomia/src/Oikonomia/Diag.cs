























using System;
using System.Collections.Generic;
using System.Text;

namespace Suzerain.DiagKit
{
    
    public enum DiagStatus
    {
        
        Ok = 0,
        
        Missing = 1,
        
        Failed = 2
    }

    
    public sealed class DiagEntry
    {
        public DiagEntry(string name, DiagStatus status, string message)
        {
            Name = name ?? "(未命名)";
            Status = status;
            Message = message;
        }

        
        public string Name { get; private set; }
        public DiagStatus Status { get; private set; }
        
        public string Message { get; private set; }

        public override string ToString()
        {
            if (string.IsNullOrEmpty(Message)) return Name;
            return Name + "（" + Message + "）";
        }
    }

    
    
    
    public static class Diag
    {
        private static readonly object Gate = new object();
        private static readonly List<DiagEntry> Entries = new List<DiagEntry>();
        private static readonly Dictionary<string, int> Index = new Dictionary<string, int>(StringComparer.Ordinal);

        
        public static string Label = "补丁点";

        
        public static Action<DiagEntry> Sink;

        

        
        public static void Ok(string name) { Record(name, DiagStatus.Ok, null); }

        
        public static void Missing(string name) { Record(name, DiagStatus.Missing, null); }

        
        public static void Missing(string name, string note) { Record(name, DiagStatus.Missing, note); }

        
        public static void Fail(string name, string message) { Record(name, DiagStatus.Failed, message); }

        private static void Record(string name, DiagStatus status, string message)
        {
            DiagEntry entry;
            lock (Gate)
            {
                entry = new DiagEntry(name, status, message);
                int i;
                if (Index.TryGetValue(entry.Name, out i)) Entries[i] = entry;   
                else { Index[entry.Name] = Entries.Count; Entries.Add(entry); }
            }
            var sink = Sink;
            if (sink != null)
            {
                try { sink(entry); } catch { }   
            }
        }

        

        
        public static void Reset()
        {
            lock (Gate)
            {
                Entries.Clear();
                Index.Clear();
            }
        }

        
        public static int Count { get { lock (Gate) { return Entries.Count; } } }
        public static int OkCount { get { return CountStatus(DiagStatus.Ok); } }
        public static int MissingCount { get { return CountStatus(DiagStatus.Missing); } }
        public static int FailCount { get { return CountStatus(DiagStatus.Failed); } }

        private static int CountStatus(DiagStatus status)
        {
            lock (Gate)
            {
                int n = 0;
                for (int i = 0; i < Entries.Count; i++) if (Entries[i].Status == status) n++;
                return n;
            }
        }

        
        public static List<DiagEntry> All()
        {
            lock (Gate) { return new List<DiagEntry>(Entries); }
        }

        
        public static List<DiagEntry> Problems()
        {
            var list = new List<DiagEntry>();
            lock (Gate)
            {
                for (int i = 0; i < Entries.Count; i++)
                {
                    if (Entries[i].Status != DiagStatus.Ok) list.Add(Entries[i]);
                }
            }
            return list;
        }

        

        
        
        
        
        public static string Summary()
        {
            int total, ok, missing, failed;
            List<DiagEntry> items;
            lock (Gate)
            {
                total = Entries.Count;
                ok = 0; missing = 0; failed = 0;
                for (int i = 0; i < Entries.Count; i++)
                {
                    switch (Entries[i].Status)
                    {
                        case DiagStatus.Ok: ok++; break;
                        case DiagStatus.Missing: missing++; break;
                        default: failed++; break;
                    }
                }
                items = new List<DiagEntry>(Entries);
            }

            if (total == 0) return Label + " 0/0（未上报任何诊断点）";
            if (missing == 0 && failed == 0) return string.Format("{0} {1}/{1} 挂载成功", Label, total);

            var sb = new StringBuilder();
            sb.AppendFormat("{0} {1}/{2} 挂载成功", Label, ok, total);
            if (failed > 0) sb.Append("｜").Append(JoinProblems(items, DiagStatus.Failed, 4, "异常"));
            if (missing > 0) sb.Append("｜").Append(JoinProblems(items, DiagStatus.Missing, 4, "缺失"));
            return sb.ToString();
        }

        
        public static string Detail()
        {
            var problems = Problems();
            if (problems.Count == 0) return "";
            var sb = new StringBuilder();
            for (int i = 0; i < problems.Count; i++)
            {
                var e = problems[i];
                if (sb.Length > 0) sb.Append(Environment.NewLine);
                sb.Append(EntryLine(e));
            }
            return sb.ToString();
        }

        
        public static string EntryLine(DiagEntry e)
        {
            if (e == null) return "";
            string tag = e.Status == DiagStatus.Ok ? "OK" : (e.Status == DiagStatus.Missing ? "缺失" : "异常");
            if (string.IsNullOrEmpty(e.Message)) return string.Format("[{0}] {1}", tag, e.Name);
            return string.Format("[{0}] {1} —— {2}", tag, e.Name, e.Message);
        }

        private static string JoinProblems(List<DiagEntry> items, DiagStatus status, int max, string tag)
        {
            var names = new List<string>();
            int total = 0;
            for (int i = 0; i < items.Count; i++)
            {
                if (items[i].Status != status) continue;
                total++;
                if (names.Count < max) names.Add(items[i].ToString());
            }
            if (total == 0) return tag + " 0";
            string s = tag + " " + total + "：" + string.Join("、", names.ToArray());
            if (total > names.Count) s += string.Format(" 等 {0} 项（见明细）", total);
            return s;
        }
    }
}
