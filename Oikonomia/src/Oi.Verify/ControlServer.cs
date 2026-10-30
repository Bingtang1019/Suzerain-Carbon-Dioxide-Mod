using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using BepInEx.Logging;

namespace OiVerify
{
    
    
    
    
    
    internal sealed class ControlServer
    {
        private const int MaxRequestBytes = 1024 * 1024;
        private readonly int _port;
        private readonly string _tokenFile;
        private readonly ManualLogSource _log;
        private readonly VerifyBehaviour _behaviour;
        private readonly CancellationTokenSource _cts = new CancellationTokenSource();
        private string _token;
        private TcpListener _listener;

        internal ControlServer(int port, string tokenFile, ManualLogSource log, VerifyBehaviour behaviour)
        {
            _port = port;
            _tokenFile = tokenFile;
            _log = log;
            _behaviour = behaviour;
        }

        internal void Start()
        {
            if (_port < 1 || _port > 65535)
                throw new ArgumentOutOfRangeException(nameof(_port), "port must be 1..65535");
            _token = File.ReadAllText(_tokenFile, Encoding.UTF8).Trim();
            if (_token.Length < 16)
                throw new InvalidOperationException("token must contain at least 16 characters");

            _listener = new TcpListener(IPAddress.Loopback, _port);
            _listener.Start();
            _log.LogInfo("[OiVerify] listening on 127.0.0.1:" + _port);
            Task.Run(AcceptLoop);
        }

        internal void Stop()
        {
            try { _cts.Cancel(); } catch { }
            try { _listener?.Stop(); } catch { }
            _listener = null;
            _log.LogInfo("[OiVerify] control server stopped");
        }

        private async Task AcceptLoop()
        {
            while (!_cts.IsCancellationRequested)
            {
                TcpClient client;
                try
                {
                    client = await _listener.AcceptTcpClientAsync();
                }
                catch (ObjectDisposedException) { break; }
                catch (SocketException e)
                {
                    if (_cts.IsCancellationRequested) break;
                    _log.LogWarning("[OiVerify] accept failed: " + e.Message);
                    continue;
                }
                catch (Exception e)
                {
                    if (_cts.IsCancellationRequested) break;
                    _log.LogWarning("[OiVerify] accept exception: " + e.Message);
                    continue;
                }
                _ = Task.Run(() => HandleClient(client));
            }
        }

        private async Task HandleClient(TcpClient client)
        {
            using (client)
            {
                client.NoDelay = true;
                NetworkStream stream = client.GetStream();
                var reader = new StreamReader(
                    stream, new UTF8Encoding(false), false, 4096, leaveOpen: true);
                var writer = new StreamWriter(
                    stream, new UTF8Encoding(false), 4096, leaveOpen: true) { AutoFlush = false };

                try
                {
                    string line = await reader.ReadLineAsync();
                    if (line == null) return;
                    if (line.Length > MaxRequestBytes)
                    {
                        await WriteError(writer, null, "request_too_large", "request exceeds 1 MiB");
                        return;
                    }

                    using (JsonDocument document = JsonDocument.Parse(line))
                    {
                        JsonElement root = document.RootElement;
                        string idRaw = ReadIdRaw(root);
                        string token = ReadString(root, "token");
                        if (!TokenEquals(token, _token))
                        {
                            _log.LogWarning("[OiVerify] rejected unauthenticated request id=" + idRaw);
                            await WriteError(writer, idRaw, "unauthorized", "missing or invalid token");
                            return;
                        }

                        string cmd = (ReadString(root, "cmd") ?? "").Trim().ToLowerInvariant();
                        _log.LogInfo("[OiVerify] request id=" + idRaw + " cmd=" + cmd);
                        RequestArgs args = RequestArgs.From(root);

                        CommandResult result;
                        switch (cmd)
                        {
                            case "ping":
                                result = await _behaviour.PingAsync();
                                break;
                            case "state":
                                result = await _behaviour.StateAsync();
                                break;
                            case "menu":
                                result = await _behaviour.MenuAsync();
                                break;
                            case "ui_dump":
                                result = await _behaviour.UiDumpAsync(args.GetInt("max", 200));
                                break;
                            case "click":
                            case "click_text":
                                result = await _behaviour.ClickAsync(
                                    args.GetString("text"), args.GetString("value"));
                                break;
                            case "advance":
                                result = await _behaviour.AdvanceAsync();
                                break;
                            case "read":
                                result = await _behaviour.ReadAsync(args.GetString("name"));
                                break;
                            case "shot":
                                result = await _behaviour.ShotAsync(args.GetString("name"));
                                break;
                            case "wait":
                                result = await _behaviour.WaitAsync(
                                    args.GetString("kind"),
                                    args.GetString("contains"),
                                    args.GetString("name"),
                                    args.GetBool("value", true),
                                    args.GetInt("timeoutMs", 0));
                                break;
                            case "quit":
                                result = await _behaviour.QuitAsync();
                                break;
                            default:
                                result = CommandResult.Fail(
                                    "unsupported_command",
                                    "command is not in the whitelist: " + cmd);
                                break;
                        }

                        await WriteResult(writer, idRaw, result);
                    }
                }
                catch (JsonException e)
                {
                    _log.LogWarning("[OiVerify] bad JSON: " + e.Message);
                    try { await WriteError(writer, null, "bad_json", e.Message); } catch { }
                }
                catch (IOException) { }
                catch (ObjectDisposedException) { }
                catch (Exception e)
                {
                    _log.LogError("[OiVerify] request handler failed: " + e);
                    try { await WriteError(writer, null, "internal_error", e.Message); } catch { }
                }
            }
        }

        private static async Task WriteResult(StreamWriter writer, string idRaw, CommandResult result)
        {
            string payload;
            if (result != null && result.Ok)
            {
                string data = JsonSerializer.Serialize(result.Data);
                payload = "{\"id\":" + idRaw + ",\"ok\":true,\"data\":" + data + "}\n";
            }
            else
            {
                CommandResult r = result ?? CommandResult.Fail("empty_result", "no result");
                string error =
                    "{\"type\":" + JsonSerializer.Serialize(r.ErrorType ?? "unknown")
                    + ",\"message\":" + JsonSerializer.Serialize(r.Error ?? "")
                    + ",\"stack\":" + JsonSerializer.Serialize(r.Stack ?? "") + "}";
                payload = "{\"id\":" + idRaw + ",\"ok\":false,\"error\":" + error + "}\n";
            }
            await writer.WriteAsync(payload);
            await writer.FlushAsync();
        }

        private static async Task WriteError(StreamWriter writer, string idRaw, string type, string message)
        {
            string error =
                "{\"type\":" + JsonSerializer.Serialize(type ?? "unknown")
                + ",\"message\":" + JsonSerializer.Serialize(message ?? "") + "}";
            string payload = "{\"id\":" + (idRaw ?? "null") + ",\"ok\":false,\"error\":" + error + "}\n";
            await writer.WriteAsync(payload);
            await writer.FlushAsync();
        }

        private bool TokenEquals(string got, string expected)
        {
            if (got == null || expected == null) return false;
            byte[] a = Encoding.UTF8.GetBytes(got);
            byte[] b = Encoding.UTF8.GetBytes(expected);
            if (a.Length != b.Length) return false;
            int diff = 0;
            for (int i = 0; i < a.Length; i++) diff |= a[i] ^ b[i];
            return diff == 0;
        }

        private static string ReadIdRaw(JsonElement root)
        {
            try
            {
                JsonElement id;
                if (!root.TryGetProperty("id", out id)) return "null";
                if (id.ValueKind == JsonValueKind.String)
                    return JsonSerializer.Serialize(id.GetString());
                return id.GetRawText();
            }
            catch { return "null"; }
        }

        private static string ReadString(JsonElement root, string name)
        {
            try
            {
                JsonElement value;
                if (!root.TryGetProperty(name, out value)) return null;
                return value.ValueKind == JsonValueKind.String ? value.GetString() : null;
            }
            catch { return null; }
        }
    }
}
