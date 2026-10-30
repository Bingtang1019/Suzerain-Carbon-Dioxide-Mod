using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BepInEx.Logging;
using UnityEngine;

namespace OiVerify
{
    internal sealed class CommandResult
    {
        public bool Ok;
        public object Data;
        public string ErrorType;
        public string Error;
        public string Stack;

        public static CommandResult Success(object data)
        {
            return new CommandResult { Ok = true, Data = data };
        }

        public static CommandResult Fail(string type, string message, Exception e = null)
        {
            string stack = null;
            if (e != null)
            {
                try
                {
                    stack = e.StackTrace;
                    if (stack != null && stack.Length > 4000) stack = stack.Substring(0, 4000);
                }
                catch { }
            }
            return new CommandResult
            {
                Ok = false,
                ErrorType = type ?? "unknown",
                Error = message ?? "",
                Stack = stack
            };
        }
    }

    internal sealed class VerifyBehaviour : MonoBehaviour
    {
        private ManualLogSource _log;
        private string _artifactsDir;
        private int _maxSteps;
        private int _stepTimeoutSec;
        private bool _autoQuit;
        private int _exitWatchdogSec;
        private int _mainThreadId;
        private DateTime _quitAtUtc = DateTime.MinValue;
        private bool _quitObserved;
        private int _watchdogStarted;
        private Action _shutdownAction;
        private GameDriver _driver;

        private sealed class Job
        {
            public Func<CommandResult> Work;
            public TaskCompletionSource<CommandResult> Completion;
        }

        private sealed class ShotRequest
        {
            public string Name;
            public int CreatedFrame;
            public TaskCompletionSource<CommandResult> Completion;
        }

        private readonly ConcurrentQueue<Job> _jobs = new ConcurrentQueue<Job>();
        private readonly ConcurrentQueue<ShotRequest> _shots = new ConcurrentQueue<ShotRequest>();

        internal void Initialize(
            ManualLogSource log,
            string artifactsDir,
            int maxSteps,
            int stepTimeoutSec,
            bool autoQuit,
            int exitWatchdogSec)
        {
            _log = log;
            _artifactsDir = artifactsDir;
            _maxSteps = maxSteps;
            _stepTimeoutSec = stepTimeoutSec;
            _autoQuit = autoQuit;
            _exitWatchdogSec = Math.Max(3, exitWatchdogSec);
            _mainThreadId = Thread.CurrentThread.ManagedThreadId;
            _log.LogInfo("[OiVerify] main-thread dispatcher ready; thread=" + _mainThreadId
                + " maxSteps=" + _maxSteps + " stepTimeoutSec=" + _stepTimeoutSec
                + " exitWatchdogSec=" + _exitWatchdogSec);
            try { Application.runInBackground = true; } catch { }
            _driver = new GameDriver(_log, _artifactsDir);
            _driver.Initialize();
            WriteEvidence("oi-verify-mainthread.json",
                "\"mainThreadId\":" + _mainThreadId
                + ",\"maxSteps\":" + _maxSteps
                + ",\"stepTimeoutSec\":" + _stepTimeoutSec
                + ",\"exitWatchdogSec\":" + _exitWatchdogSec);
        }

        internal void SetShutdownAction(Action action)
        {
            _shutdownAction = action;
        }

        internal Task<CommandResult> SubmitAsync(Func<CommandResult> work)
        {
            var job = new Job
            {
                Work = work,
                Completion = new TaskCompletionSource<CommandResult>(
                    TaskCreationOptions.RunContinuationsAsynchronously)
            };
            _jobs.Enqueue(job);
            return job.Completion.Task;
        }

        internal Task<CommandResult> PingAsync()
        {
            return SubmitAsync(delegate
            {
                try
                {
                    var data = new Dictionary<string, object>
                    {
                        { "ready", true },
                        { "pluginVersion", Plugin.Version },
                        { "gameVersion", Safe(() => Application.version) },
                        { "unityVersion", Safe(() => Application.unityVersion) },
                        { "productName", Safe(() => Application.productName) },
                        { "persistentDataPath", Safe(() => Application.persistentDataPath) },
                        { "dataPath", Safe(() => Application.dataPath) },
                        { "currentDirectory", Safe(() => Environment.CurrentDirectory) },
                        { "pid", Environment.ProcessId },
                        { "mainThreadId", _mainThreadId },
                        { "isMainThread", Thread.CurrentThread.ManagedThreadId == _mainThreadId }
                    };
                    return CommandResult.Success(data);
                }
                catch (Exception e)
                {
                    return CommandResult.Fail("ping_failed", e.Message, e);
                }
            });
        }

        internal Task<CommandResult> QuitAsync()
        {
            return SubmitAsync(delegate
            {
                try
                {
                    if (!_autoQuit)
                        return CommandResult.Fail("quit_disabled", "AutoQuit=false");
                    _quitAtUtc = DateTime.UtcNow.AddMilliseconds(350);
                    _log.LogInfo("[OiVerify] quit accepted; Application.Quit scheduled in 350 ms");
                    var data = new Dictionary<string, object>
                    {
                        { "accepted", true },
                        { "scheduledAtUtc", _quitAtUtc.ToString("o") },
                        { "pid", Environment.ProcessId },
                        { "requestedExitMode", "application-quit" },
                        { "exitMode", "pending" },
                        { "cleanExit", false },
                        { "watchdogSec", _exitWatchdogSec }
                    };
                    return CommandResult.Success(data);
                }
                catch (Exception e)
                {
                    return CommandResult.Fail("quit_failed", e.Message, e);
                }
            });
        }

        internal Task<CommandResult> StateAsync()
        {
            return SubmitAsync(delegate
            {
                try { return CommandResult.Success(_driver.Snapshot()); }
                catch (Exception e) { return CommandResult.Fail("state_failed", e.Message, e); }
            });
        }

        internal Task<CommandResult> MenuAsync()
        {
            return SubmitAsync(delegate
            {
                try { return CommandResult.Success(_driver.Menu()); }
                catch (Exception e) { return CommandResult.Fail("menu_failed", e.Message, e); }
            });
        }

        internal Task<CommandResult> UiDumpAsync(int max)
        {
            return SubmitAsync(delegate
            {
                try { return CommandResult.Success(_driver.UiDump(max)); }
                catch (Exception e) { return CommandResult.Fail("ui_dump_failed", e.Message, e); }
            });
        }

        internal Task<CommandResult> ClickAsync(string text, string value = null)
        {
            return SubmitAsync(delegate
            {
                try { return CommandResult.Success(_driver.Click(text, value)); }
                catch (Exception e) { return CommandResult.Fail("click_failed", e.Message, e); }
            });
        }

        internal Task<CommandResult> AdvanceAsync()
        {
            return SubmitAsync(delegate
            {
                try { return CommandResult.Success(_driver.Advance()); }
                catch (Exception e) { return CommandResult.Fail("advance_failed", e.Message, e); }
            });
        }

        internal Task<CommandResult> ReadAsync(string name)
        {
            return SubmitAsync(delegate
            {
                try { return CommandResult.Success(_driver.ReadVariableChecked(name)); }
                catch (Exception e) { return CommandResult.Fail("read_failed", e.Message, e); }
            });
        }

        internal Task<CommandResult> ShotAsync(string name)
        {
            var request = new ShotRequest
            {
                Name = name,
                CreatedFrame = Time.frameCount,
                Completion = new TaskCompletionSource<CommandResult>(
                    TaskCreationOptions.RunContinuationsAsynchronously)
            };
            _shots.Enqueue(request);
            return request.Completion.Task;
        }

        internal async Task<CommandResult> WaitAsync(
            string kind, string contains, string flagName, bool flagWant, int timeoutMs)
        {
            try
            {
                if (timeoutMs <= 0) timeoutMs = _stepTimeoutSec * 1000;
                if (timeoutMs > 120000) timeoutMs = 120000;
                var started = DateTime.UtcNow;
                var deadline = started.AddMilliseconds(timeoutMs);
                string last = "";
                while (DateTime.UtcNow < deadline)
                {
                    CommandResult probe = await SubmitAsync(delegate
                    {
                        try { return CommandResult.Success(_driver.WaitProbe(kind, flagName)); }
                        catch (Exception e) { return CommandResult.Fail("wait_probe_failed", e.Message, e); }
                    });
                    if (probe == null || !probe.Ok) return probe;
                    var data = probe.Data as Dictionary<string, object>;
                    last = DescribeWait(kind, data);
                    if (WaitMatches(kind, contains, flagWant, data))
                    {
                        return CommandResult.Success(new Dictionary<string, object>
                        {
                            { "matched", true },
                            { "kind", kind },
                            { "contains", contains ?? "" },
                            { "elapsedMs", (int)(DateTime.UtcNow - started).TotalMilliseconds },
                            { "state", data }
                        });
                    }
                    await Task.Delay(150);
                }
                return CommandResult.Fail(
                    "wait_timeout",
                    "timed out after " + timeoutMs + "ms waiting for " + kind
                    + " contains=" + (contains ?? "") + " | last=" + last);
            }
            catch (Exception e)
            {
                return CommandResult.Fail("wait_failed", e.Message, e);
            }
        }

        private static bool WaitMatches(
            string kind, string contains, bool flagWant, Dictionary<string, object> data)
        {
            if (data == null) return false;
            string k = (kind ?? "").Trim().ToLowerInvariant();
            if (k == "flag")
            {
                object raw;
                if (!data.TryGetValue("waitFlag", out raw) || raw == null) return false;
                var flag = raw as Dictionary<string, object>;
                if (flag == null || !flag.ContainsKey("value")) return false;
                object actual = flag["value"];
                bool b;
                try { b = Convert.ToBoolean(actual); } catch { b = false; }
                return b == flagWant;
            }
            string haystack;
            if (k == "title") haystack = Convert.ToString(Get(data, "conversationTitle")) ?? "";
            else if (k == "line") haystack = Convert.ToString(Get(data, "line")) ?? "";
            else if (k == "menu" || k == "response") haystack = ResponseText(data);
            else if (k == "button") haystack = ButtonText(data);
            else haystack = Jsonify(data);
            return string.IsNullOrEmpty(contains)
                || haystack.IndexOf(contains, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static string DescribeWait(string kind, Dictionary<string, object> data)
        {
            if (data == null) return "";
            string k = (kind ?? "").Trim().ToLowerInvariant();
            if (k == "title") return Short(Convert.ToString(Get(data, "conversationTitle")));
            if (k == "line") return Short(Convert.ToString(Get(data, "line")));
            if (k == "menu" || k == "response") return Short(ResponseText(data));
            if (k == "button") return Short(ButtonText(data));
            if (k == "flag") return Jsonify(Get(data, "waitFlag"));
            return Short(Jsonify(data));
        }

        private static string ResponseText(Dictionary<string, object> data)
        {
            object raw = Get(data, "responses");
            var list = raw as System.Collections.IEnumerable;
            if (list == null) return "";
            var parts = new List<string>();
            foreach (object item in list)
            {
                var d = item as Dictionary<string, object>;
                if (d != null) parts.Add(Convert.ToString(Get(d, "text")) ?? "");
            }
            return string.Join(" | ", parts.ToArray());
        }

        private static string ButtonText(Dictionary<string, object> data)
        {
            object raw = Get(data, "menuButtons");
            var list = raw as System.Collections.IEnumerable;
            if (list == null) return "";
            var parts = new List<string>();
            foreach (object item in list) parts.Add(Convert.ToString(item) ?? "");
            return string.Join(" | ", parts.ToArray());
        }

        private static object Get(Dictionary<string, object> data, string name)
        {
            object value;
            return data != null && data.TryGetValue(name, out value) ? value : null;
        }

        private static string Jsonify(object value)
        {
            try { return System.Text.Json.JsonSerializer.Serialize(value); }
            catch { return Convert.ToString(value) ?? ""; }
        }

        private static string Short(string value)
        {
            if (string.IsNullOrEmpty(value)) return "";
            return value.Length <= 300 ? value : value.Substring(0, 300) + "…";
        }

        private void Update()
        {
            int budget = 16;
            Job job;
            while (budget-- > 0 && _jobs.TryDequeue(out job))
            {
                CommandResult result;
                try
                {
                    result = job.Work == null
                        ? CommandResult.Fail("empty_job", "queued job had no work")
                        : job.Work();
                }
                catch (Exception e)
                {
                    result = CommandResult.Fail("main_thread_exception", e.Message, e);
                }
                try { job.Completion.TrySetResult(result); }
                catch { }
            }

            int shotBudget = 4;
            ShotRequest shot;
            while (shotBudget-- > 0 && _shots.TryPeek(out shot))
            {
                
                if (Time.frameCount <= shot.CreatedFrame) break;
                _shots.TryDequeue(out shot);
                CommandResult shotResult;
                try
                {
                    var data = _driver.Screenshot(shot.Name);
                    data["exists"] = true;
                    data["capturedAtUtc"] = DateTime.UtcNow.ToString("o");
                    shotResult = CommandResult.Success(data);
                }
                catch (Exception e)
                {
                    shotResult = CommandResult.Fail("shot_failed", e.Message, e);
                }
                try { shot.Completion.TrySetResult(shotResult); }
                catch { }
            }

            if (_quitAtUtc != DateTime.MinValue && DateTime.UtcNow >= _quitAtUtc)
            {
                _quitAtUtc = DateTime.MinValue;
                try
                {
                    _log.LogInfo("[OiVerify] Application.Quit()");
                    WriteEvidence("oi-verify-quit-requested.json",
                        "\"requestedAtUtc\":\"" + DateTime.UtcNow.ToString("o")
                        + "\",\"requestedExitMode\":\"application-quit\""
                        + ",\"watchdogSec\":" + _exitWatchdogSec);
                    Application.Quit();
                }
                catch (Exception e)
                {
                    _log.LogError("[OiVerify] Application.Quit failed: " + e.Message);
                }
            }
        }

        private void OnApplicationQuit()
        {
            if (_quitObserved) return;
            _quitObserved = true;
            try
            {
                _log.LogInfo("[OiVerify] OnApplicationQuit observed");
                WriteEvidence("oi-verify-quit-observed.json",
                    "\"observedAtUtc\":\"" + DateTime.UtcNow.ToString("o")
                    + "\",\"pid\":" + Environment.ProcessId
                    + ",\"exitMode\":\"application-quit\""
                    + ",\"cleanExit\":\"pending\""
                    + ",\"watchdogSec\":" + _exitWatchdogSec);
                try { _shutdownAction?.Invoke(); }
                catch (Exception e) { _log.LogWarning("[OiVerify] shutdown action failed: " + e.Message); }
                StartExitWatchdog();
            }
            catch { }
        }

        private void StartExitWatchdog()
        {
            if (Interlocked.Exchange(ref _watchdogStarted, 1) != 0) return;
            var thread = new Thread(delegate ()
            {
                try
                {
                    Thread.Sleep(TimeSpan.FromSeconds(_exitWatchdogSec));
                    try
                    {
                        _log.LogWarning("[OiVerify] quit watchdog expired after "
                            + _exitWatchdogSec + "s; forcing Environment.Exit(0)");
                    }
                    catch { }
                    try
                    {
                        WriteEvidence("oi-verify-force-exit.json",
                            "\"forcedAtUtc\":\"" + DateTime.UtcNow.ToString("o")
                            + "\",\"watchdogSec\":" + _exitWatchdogSec
                            + ",\"pid\":" + Environment.ProcessId
                            + ",\"exitMode\":\"watchdog\""
                            + ",\"cleanExit\":false");
                    }
                    catch { }
                    Environment.Exit(0);
                }
                catch { }
            });
            thread.IsBackground = true;
            thread.Name = "OiVerifyExitWatchdog";
            thread.Start();
        }

        private void WriteEvidence(string fileName, string fields)
        {
            try
            {
                if (string.IsNullOrEmpty(_artifactsDir)) return;
                Directory.CreateDirectory(_artifactsDir);
                string json = "{\"atUtc\":\"" + DateTime.UtcNow.ToString("o") + "\"," + fields + "}\n";
                File.WriteAllText(
                    Path.Combine(_artifactsDir, fileName),
                    json,
                    new UTF8Encoding(false));
            }
            catch (Exception e)
            {
                try { _log?.LogWarning("[OiVerify] evidence write failed: " + e.Message); } catch { }
            }
        }

        private static string Safe(Func<string> getter)
        {
            try { return getter() ?? ""; } catch { return ""; }
        }
    }
}
