using System;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Threading;

namespace MockModBridge
{
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    static class Program
    {
        
        const int BufSize = 4096;

        static readonly object logGate = new object();
        static StreamWriter log;

        
        static readonly int T0 = Environment.TickCount;
        static int Since() { return unchecked(Environment.TickCount - T0); }

        static void Main(string[] args)
        {
            string pipe = args.Length > 0 ? args[0] : "OikonomiaBridge";
            int times = args.Length > 1 ? int.Parse(args[1]) : 4;
            string logPath = args.Length > 2 ? args[2] : null;
            if (logPath != null)
            {
                try { log = new StreamWriter(logPath, false, new UTF8Encoding(false)); log.AutoFlush = true; } catch { }
            }
            Write("serve-start pipe=" + pipe + " times=" + times);

            for (int k = 0; k < times; k++)
            {
                try
                {
                    
                    using (NamedPipeServerStream p = new NamedPipeServerStream(
                        pipe, PipeDirection.InOut, 1, PipeTransmissionMode.Byte, PipeOptions.None,
                        BufSize, BufSize))
                    {
                        Write("[" + k + "] @" + Since() + "ms created waiting");
                        p.WaitForConnection();
                        Write("[" + k + "] @" + Since() + "ms connected");
                        Serve(p, k);
                    }
                }
                catch (Exception e)
                {
                    Write("[" + k + "] @" + Since() + "ms accept-err " + e.GetType().Name + ": " + e.Message);
                    Thread.Sleep(120);   
                }
            }
            Write("serve-end @" + Since() + "ms");
            if (log != null) { try { log.Flush(); log.Dispose(); } catch { } }
        }

        static void Serve(NamedPipeServerStream p, int idx)
        {
            try
            {
                
                byte[] buf = new byte[BufSize];
                int n = p.Read(buf, 0, buf.Length);
                if (n <= 0) { Write("[" + idx + "] @" + Since() + "ms client-closed-no-data"); return; }

                if (buf[0] == (byte)'S')
                {
                    
                    byte[] body = Encoding.UTF8.GetBytes("{\"modVersion\":\"9.9.9-mock\",\"enabled\":true}");
                    byte[] len = BitConverter.GetBytes(body.Length);
                    p.Write(len, 0, 4);
                    p.Write(body, 0, body.Length);
                    p.Flush();
                    Write("[" + idx + "] @" + Since() + "ms STATE replied " + body.Length + " bytes");
                    return;
                }

                
                
                p.WriteByte(0x06);
                p.Flush();
                Write("[" + idx + "] @" + Since() + "ms ACK sent");

                StringBuilder sb = new StringBuilder();
                sb.Append(Encoding.UTF8.GetString(buf, 0, n));
                int m;
                try
                {
                    while ((m = p.Read(buf, 0, buf.Length)) > 0) sb.Append(Encoding.UTF8.GetString(buf, 0, m));
                }
                catch (IOException) {  }
                catch (ObjectDisposedException) { }
                Write("[" + idx + "] @" + Since() + "ms OVERRIDE " + sb.Length + " chars: " + Preview(sb.ToString()));
            }
            catch (Exception e) { Write("[" + idx + "] @" + Since() + "ms err " + e.GetType().Name + ": " + e.Message); }
        }

        static string Preview(string s)
        {
            s = s.Replace("\r", "").Replace("\n", " ");
            return s.Length > 70 ? s.Substring(0, 70) + "..." : s;
        }

        static void Write(string s)
        {
            lock (logGate)
            {
                Console.WriteLine(s);
                if (log != null) { try { log.WriteLine(s); } catch { } }
            }
        }
    }
}
