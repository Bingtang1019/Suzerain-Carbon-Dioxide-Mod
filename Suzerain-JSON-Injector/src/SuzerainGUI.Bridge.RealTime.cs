using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;

namespace SuzerainTrainer
{
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    internal static class RealTimeBridge
    {
        
        internal const string OiPipeName = "OikonomiaBridge";

        
        internal const string HbPipeName = "HbBridge";

        
        internal const string PipeName = OiPipeName;

        
        const int ConnectTimeoutMs = 120;

        
        
        
        
        const int IoTimeoutMs = 1000;

        
        const int FailThreshold = 3;

        
        const int CooldownMs = 30000;

        
        sealed class PipeState
        {
            internal int Fails;
            internal long CooldownUntilMs;      
            internal string LastError = "";
            internal string CachedJson;
            internal DateTime CachedUtc = DateTime.MinValue;
        }

        static readonly object gate = new object();
        static readonly Dictionary<string, PipeState> states = new Dictionary<string, PipeState>(StringComparer.Ordinal);

        
        static readonly System.Diagnostics.Stopwatch clock = System.Diagnostics.Stopwatch.StartNew();
        static long NowMs { get { return clock.ElapsedMilliseconds; } }

        static PipeState StateOf(string pipeName)
        {
            string key = string.IsNullOrEmpty(pipeName) ? OiPipeName : pipeName;
            PipeState s;
            if (!states.TryGetValue(key, out s)) { s = new PipeState(); states[key] = s; }
            return s;
        }

        static bool InCooldown(PipeState s) { return NowMs < s.CooldownUntilMs; }

        static void NoteSuccess(PipeState s)
        {
            s.Fails = 0; s.CooldownUntilMs = 0; s.LastError = "";
        }

        static void NoteFailure(PipeState s, string why)
        {
            s.LastError = why ?? "";
            s.Fails++;
            if (s.Fails >= FailThreshold)
            {
                s.CooldownUntilMs = NowMs + CooldownMs;
                s.Fails = 0;
            }
        }

        
        internal static string LastErrorOf(string pipeName)
        {
            lock (gate) { return StateOf(pipeName).LastError; }
        }

        
        internal static string LastError { get { return LastErrorOf(OiPipeName); } }

        
        
        
        
        
        
        
        
        
        static NamedPipeClientStream Connect(string pipeName)
        {
            NamedPipeClientStream c = new NamedPipeClientStream(
                ".", pipeName, PipeDirection.InOut, PipeOptions.Asynchronous);
            try
            {
                c.Connect(ConnectTimeoutMs);
                return c;
            }
            catch
            {
                try { c.Dispose(); } catch { }
                throw;
            }
        }

        sealed class ReadResult { internal bool Ok; internal byte[] Data; }

        
        
        
        
        
        
        
        
        
        static ReadResult ReadWithTimeout(Stream s, int count)
        {
            ReadResult r = new ReadResult();
            byte[] buf = new byte[count];
            int got = 0;
            Exception err = null;
            Thread t = new Thread(delegate ()
            {
                try
                {
                    while (got < count)
                    {
                        int n = s.Read(buf, got, count - got);
                        if (n <= 0) break;              
                        got += n;
                    }
                }
                catch (Exception e) { err = e; }
            });
            t.IsBackground = true;
            t.Start();
            if (!t.Join(IoTimeoutMs))
            {
                r.Ok = false;                            
                return r;
            }
            if (err != null || got == 0) { r.Ok = false; return r; }
            if (got == count) { r.Data = buf; r.Ok = true; return r; }
            byte[] part = new byte[got];                 
            Array.Copy(buf, part, got);
            r.Data = part;
            r.Ok = true;
            return r;
        }

        
        
        
        
        
        
        
        
        
        
        
        
        
        static bool WriteWithTimeout(Stream s, byte[] data)
        {
            bool done = false;
            Thread t = new Thread(delegate ()
            {
                try { s.Write(data, 0, data.Length); s.Flush(); done = true; }
                catch { done = false; }
            });
            t.IsBackground = true;
            t.Start();
            return t.Join(IoTimeoutMs) && done;
        }

        
        internal static bool Available() { return Available(OiPipeName); }

        internal static bool Available(string pipeName)
        {
            lock (gate) { if (InCooldown(StateOf(pipeName))) return false; }
            try
            {
                using (NamedPipeClientStream c = Connect(pipeName))
                {
                    lock (gate) { NoteSuccess(StateOf(pipeName)); }
                    return true;
                }
            }
            catch (Exception e)
            {
                lock (gate) { NoteFailure(StateOf(pipeName), e.Message); }
                return false;
            }
        }

        

        
        internal static bool TrySend(string payload) { return TrySend(payload, OiPipeName); }

        
        
        
        
        internal static bool TrySend(string payload, string pipeName)
        {
            if (string.IsNullOrEmpty(payload)) return false;
            PipeState s = StateOf(pipeName);
            lock (gate) { if (InCooldown(s)) return false; }
            try
            {
                using (NamedPipeClientStream c = Connect(pipeName))
                {
                    byte[] body = Encoding.UTF8.GetBytes(payload);
                    
                    if (!WriteWithTimeout(c, body))
                    {
                        lock (gate) { NoteFailure(s, "发送超时（模组未读取，已放弃本次推送）"); }
                        return false;
                    }
                    
                    ReadResult rr = ReadWithTimeout(c, 1);
                    int ack = (rr.Ok && rr.Data != null && rr.Data.Length > 0) ? rr.Data[0] : -1;
                    lock (gate)
                    {
                        if (ack == 0x06) { NoteSuccess(s); return true; }
                        NoteFailure(s, rr.Ok ? ("模组未回 ACK（收到 " + ack + "）") : "等待 ACK 超时");
                    }
                    return false;
                }
            }
            catch (Exception e)
            {
                lock (gate) { NoteFailure(s, e.Message); }
                return false;
            }
        }

        

        
        internal static string TryPullState() { return TryPullState(OiPipeName); }

        
        
        
        
        internal static string TryPullState(string pipeName)
        {
            PipeState s = StateOf(pipeName);
            lock (gate) { if (InCooldown(s)) return null; }
            try
            {
                using (NamedPipeClientStream c = Connect(pipeName))
                {
                    
                    
                    
                    if (!WriteWithTimeout(c, new byte[] { (byte)'S' }))
                    {
                        lock (gate) { NoteFailure(s, "state 请求发送超时（模组未读取）"); }
                        return null;
                    }
                    ReadResult lr = ReadWithTimeout(c, 4);
                    if (!lr.Ok)
                    {
                        lock (gate) { NoteFailure(s, "state 长度头读取失败/超时"); }
                        return null;
                    }
                    int len = BitConverter.ToInt32(lr.Data, 0);
                    if (len <= 0 || len > 4 * 1024 * 1024)
                    {
                        lock (gate) { NoteFailure(s, "state 长度异常：" + len); }
                        return null;
                    }
                    ReadResult br = ReadWithTimeout(c, len);
                    if (!br.Ok)
                    {
                        lock (gate) { NoteFailure(s, "state 正文读取失败/超时"); }
                        return null;
                    }
                    string json = Encoding.UTF8.GetString(br.Data);
                    lock (gate) { NoteSuccess(s); s.CachedJson = json; s.CachedUtc = DateTime.UtcNow; }
                    return json;
                }
            }
            catch (Exception e)
            {
                lock (gate) { NoteFailure(s, e.Message); }
                return null;
            }
        }

        static bool ReadExact(Stream s, byte[] buf, int count)
        {
            int got = 0;
            while (got < count)
            {
                int n = s.Read(buf, got, count - got);
                if (n <= 0) return false;
                got += n;
            }
            return true;
        }
        
        internal static string CachedStateJson { get { return CachedStateJsonOf(OiPipeName); } }

        internal static string CachedStateJsonOf(string pipeName)
        {
            lock (gate) { return StateOf(pipeName).CachedJson; }
        }

        
        internal static DateTime CachedStateUtc { get { return CachedStateUtcOf(OiPipeName); } }

        internal static DateTime CachedStateUtcOf(string pipeName)
        {
            lock (gate) { return StateOf(pipeName).CachedUtc; }
        }

        
        internal static string StateFromCache(string key) { return StateFromCache(key, OiPipeName); }

        
        internal static string StateFromCache(string key, string pipeName)
        {
            string raw = CachedStateJsonOf(pipeName);
            if (string.IsNullOrEmpty(raw)) return "";
            try
            {
                Match m = Regex.Match(raw, "\"" + Regex.Escape(key) + "\"\\s*:\\s*(\"[^\"]*\"|true|false|null|-?\\d+(?:\\.\\d+)?)");
                if (!m.Success) return "";
                string v = m.Groups[1].Value;
                return v.StartsWith("\"") ? v.Substring(1, v.Length - 2) : v;
            }
            catch { return ""; }
        }

        
        internal static void ClearCache() { ClearCache(OiPipeName); ClearCache(HbPipeName); }

        internal static void ClearCache(string pipeName)
        {
            lock (gate)
            {
                PipeState s = StateOf(pipeName);
                s.CachedJson = null;
                s.CachedUtc = DateTime.MinValue;
            }
        }

        
        
        
        
        internal static string Describe() { return Describe(OiPipeName); }

        internal static string Describe(string pipeName)
        {
            lock (gate)
            {
                PipeState s = StateOf(pipeName);
                StringBuilder sb = new StringBuilder();
                sb.Append("管道 ").Append(pipeName).Append("：");
                sb.Append(s.CachedUtc == DateTime.MinValue
                    ? "未收到过数据（模组侧未实现或游戏未运行）"
                    : ("最近 " + s.CachedUtc.ToLocalTime().ToString("HH:mm:ss") + " 收到 state"));
                if (InCooldown(s))
                {
                    long left = s.CooldownUntilMs - NowMs;
                    if (left < 0) left = 0;
                    sb.Append("，探测冷却中（约 ").Append(left / 1000).Append(" 秒后重试）");
                }
                if (!string.IsNullOrEmpty(s.LastError)) sb.Append("，末次错误：").Append(s.LastError);
                return sb.ToString();
            }
        }
    }
}
