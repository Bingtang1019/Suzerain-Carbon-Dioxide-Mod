using System;
using System.IO;
using System.IO.Pipes;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text;
using System.Threading;
using BepInEx.Logging;

namespace Oikonomia
{
    
    
    
    
    
    
    
    internal static class BridgePipe
    {
        internal const string PipeName = "OikonomiaBridge";
        const int RequestTimeoutMs = 200;

        private static Thread _thread;
        private static volatile bool _stop;
        private static ManualLogSource _log;

        internal static void Start(ManualLogSource log)
        {
            if (_thread != null) return;
            _log = log;
            _stop = false;
            _thread = new Thread(Loop) { IsBackground = true, Name = "Oikonomia.BridgePipe" };
            _thread.Start();
        }

        internal static void Stop()
        {
            _stop = true;
            try { if (_thread != null) _thread.Join(500); } catch { }
            _thread = null;
        }

        private static void Loop()
        {
            while (!_stop)
            {
                try
                {
                    using (var server = CreateServer())
                    {
                        
                        
                        
                        server.WaitForConnection();
                        Serve(server);
                    }
                }
                catch (Exception e)
                {
                    try { _log?.LogWarning("[OI][pipe] " + e.Message); } catch { }
                    try { Thread.Sleep(5000); } catch { }
                }
            }
        }

        private static NamedPipeServerStream CreateServer()
        {
            
            
            var security = new PipeSecurity();
            security.AddAccessRule(new PipeAccessRule(
                new SecurityIdentifier(WellKnownSidType.BuiltinUsersSid, null),
                PipeAccessRights.ReadWrite, AccessControlType.Allow));
            return NamedPipeServerStreamAcl.Create(
                PipeName, PipeDirection.InOut, 1, PipeTransmissionMode.Byte,
                PipeOptions.None, 4096, 4096, security,
                HandleInheritability.None, (PipeAccessRights)0);
        }

        private static void Serve(NamedPipeServerStream server)
        {
            try
            {
                var buf = new byte[4096];
                int n = ReadWithTimeout(server, buf, RequestTimeoutMs);
                if (n <= 0) return;

                if (buf[0] == (byte)'S')
                {
                    string json = Bridge.BuildStateForPipe();
                    byte[] body = Encoding.UTF8.GetBytes(json);
                    byte[] len = BitConverter.GetBytes(body.Length);
                    server.Write(len, 0, 4);
                    server.Write(body, 0, body.Length);
                    server.Flush();
                    return;
                }

                if (buf[0] != (byte)'{') return;

                
                server.WriteByte(0x06);
                server.Flush();

                var sb = new StringBuilder();
                sb.Append(Encoding.UTF8.GetString(buf, 0, n));
                int m;
                while ((m = ReadWithTimeout(server, buf, RequestTimeoutMs)) > 0)
                    sb.Append(Encoding.UTF8.GetString(buf, 0, m));

                Bridge.ApplyOverrideJson(sb.ToString());
            }
            catch (Exception e)
            {
                try { _log?.LogWarning("[OI][pipe] 处理失败（已忽略）：" + e.Message); } catch { }
            }
        }

        private static int ReadWithTimeout(Stream s, byte[] buf, int timeoutMs)
        {
            int got = -1;
            var t = new Thread(delegate () { try { got = s.Read(buf, 0, buf.Length); } catch { got = -1; } });
            t.IsBackground = true;
            t.Start();
            return t.Join(timeoutMs) ? got : -1;
        }
    }
}
