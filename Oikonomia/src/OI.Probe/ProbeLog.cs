using System;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using BepInEx.Logging;

namespace Oikonomia.Probe
{
    
    
    
    
    
    internal static class ProbeLog
    {
        private static ManualLogSource _log;
        private static readonly object _lock = new object();
        private static string _logPath;
        private static string _cfgPath;
        private static string _lastCfgRaw;
        private static Timer _timer;

        
        public static volatile bool TestWrite;
        
        public static volatile bool WriteTest;
        public static volatile bool Verbose = true;
        
        public static volatile bool HoverTrace;
        
        
        
        
        public static volatile string Stamp = "";
        
        
        
        
        
        
        public static volatile bool InteropLoadFrom;
        
        
        
        
        public static volatile bool PatchOnForeign;

        public static string Dir
        {
            get
            {
                return Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "Oikonomia");
            }
        }

        public static void Init(ManualLogSource log)
        {
            _log = log;
            try
            {
                Directory.CreateDirectory(Dir);
                _logPath = Path.Combine(Dir, "oi-probe.log");
                _cfgPath = Path.Combine(Dir, "oi-probe.json");
                if (!File.Exists(_cfgPath))
                {
                    File.WriteAllText(_cfgPath, DefaultCfg(), new UTF8Encoding(false));
                }
                else
                {
                    
                    
                    try
                    {
                        var old = File.ReadAllText(_cfgPath, Encoding.UTF8);
                        if (old.IndexOf("\"tier\"", StringComparison.Ordinal) < 0)
                        {
                            var bak = _cfgPath + ".bak";
                            if (!File.Exists(bak)) File.WriteAllText(bak, old, new UTF8Encoding(false));
                            File.WriteAllText(_cfgPath, DefaultCfg(), new UTF8Encoding(false));
                            _log.LogInfo("[OI] oi-probe.json 已升级到 0.4.0 分层模板（原文件备份为 oi-probe.json.bak）");
                        }
                    }
                    catch { }
                }
                ReadConfig();
                WriteHeader();
                _timer = new Timer(_ => Poll(), null, 1000, 1000);
            }
            catch (Exception e)
            {
                try { _log.LogError("[OI] 探针日志初始化失败：" + e.Message); } catch { }
            }
        }

        
        
        
        
        public static string DefaultCfg()
        {
            return "{\n"
                + "  \"//说明\": \"OI.Probe 0.4.0 分层开关。tier 决定挂哪一层；其余每个危险能力单独一个开关。改完保存，探针每秒热重载。\",\n"
                + "  \"tier\": \"safe\",\n"
                + "  \"//tier\": \"safe（默认＝安全层）＝纯托管只读 + 只挂 Assembly-CSharp 上的钩子 + 绝不叠加别的模组 detour；full ＝额外放开原生 il2cpp 只读通道；off ＝一条钩子都不挂\",\n"
                + "  \"//安全层包含的钩子\": \"EconomyProbe（回合结算/ED/税务/增长/旅游/贸易）+ ProbeD（TemplateHUDStat.Setup / GetStatTooltipText / WarFlowManager.EndTurn）+ HoverProbe 与 RiziaProbe 的游戏程序集钩子（TemplateHUDStat.* / HUDPanel.* / PersistenceManager.GetCurrentStoryPack）\",\n"
                + "  \"//增强层包含的能力\": \"原生 il2cpp 通道（真类型名 / 谱系判断 / 外观字段兜底采集）—— 由 nativeChannel 单独控制\",\n"
                + "  \"nativeChannel\": false,\n"
                + "  \"//nativeChannel\": \"原生 il2cpp 通道（il2cpp_class_from_name / il2cpp_runtime_invoke / 字段读取）。只在 tier=full 时可用。默认 false ＝绝不碰原生\",\n"
                + "  \"tmpHooks\": false,\n"
                + "  \"//tmpHooks\": \"★允许挂 Unity/TMP interop 代理方法（如 TMP_Text::set_text）。默认 false —— 0.3.3 / 0.3.4 两次真机 coreclr AccessViolation 秒死事故就出在这一类上（4 个 IL2CPP detour 叠在一根链上 ⇒ 反调链断裂）\",\n"
                + "  \"patchOnForeign\": false,\n"
                + "  \"//patchOnForeign\": \"★允许把 patch 叠加到别的模组已 patch 过的 interop 方法上。默认 false\",\n"
                + "  \"safeStackOnForeignGameMethod\": false,\n"
                + "  \"//safeStackOnForeignGameMethod\": \"连游戏程序集（Assembly-CSharp）上的方法也拒绝叠加（探针宁可不采也不冒叠加风险）。默认 false\",\n"
                + "  \"interopLoadFrom\": false,\n"
                + "  \"//interopLoadFrom\": \"③ 号通道：从 BepInEx/interop 目录 Assembly.LoadFrom 取类型。默认 false ＝绝不按名 Load 任何程序集（0.3.3 崩溃动作）\",\n"
                + "  \"collect\": true,\n"
                + "  \"//collect\": \"采集总闸（热生效）：false ＝所有走 ProbeTier.Guard 的钩子体直接返回，不写任何采集数据\",\n"
                + "  \"panic\": false,\n"
                + "  \"//panic\": \"热开关：置 true 的下一秒立即卸载全部探针钩子（救急用，无需重启游戏）\",\n"
                + "  \"autoDegrade\": true,\n"
                + "  \"//autoDegrade\": \"自愈：上次未正常退出（probe-health.json 的 lastCleanExit=false）⇒ 自动降级到安全层；连续两次 ⇒ 完全禁用自己\",\n"
                + "  \"signalOverride\": false,\n"
                + "  \"//signalOverride\": \"true ＝即使从未观测到过「正常退出」信号，也允许按 tier 请求值运行（默认 false ＝上限钉在安全层）\",\n"
                + "  \"testWrite\": false,\n  \"writeTest\": false,\n  \"verbose\": true,\n"
                + "  \"hoverTrace\": false,\n  \"stamp\": \"\"\n}\n";
        }

        private static void WriteHeader()
        {
            var sb = new StringBuilder();
            sb.AppendLine();
            sb.AppendLine("================================================================");
            sb.AppendLine("[OI.Probe] 会话开始 " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture));
            sb.AppendLine("  日志文件 : " + _logPath);
            sb.AppendLine("  开关文件 : " + _cfgPath);
            sb.AppendLine("  testWrite=" + TestWrite + "  verbose=" + Verbose
                + "  hoverTrace=" + HoverTrace + "  stamp=" + (Stamp.Length == 0 ? "(空)" : Stamp));
            sb.AppendLine("================================================================");
            Append(sb.ToString());
        }

        private static void Poll()
        {
            try
            {
                if (!File.Exists(_cfgPath)) return;
                var raw = File.ReadAllText(_cfgPath, Encoding.UTF8);
                if (raw == _lastCfgRaw) return;
                bool oldT = TestWrite, oldW = WriteTest, oldV = Verbose, oldH = HoverTrace;
                string oldS = Stamp;
                _lastCfgRaw = raw;
                ProbeTier.OnConfigRaw(raw);   
                TestWrite = ReadBool(raw, "testWrite", TestWrite);
                WriteTest = ReadBool(raw, "writeTest", WriteTest);
                Verbose = ReadBool(raw, "verbose", Verbose);
                HoverTrace = ReadBool(raw, "hoverTrace", HoverTrace);
                Stamp = ReadStr(raw, "stamp", Stamp);
                InteropLoadFrom = ReadBool(raw, "interopLoadFrom", InteropLoadFrom);
                SafeResolve.AllowInteropLoadFrom = InteropLoadFrom;
                PatchOnForeign = ReadBool(raw, "patchOnForeign", PatchOnForeign);
                SafeResolve.AllowForeignPatchStack = PatchOnForeign;
                if (oldT != TestWrite || oldW != WriteTest || oldV != Verbose || oldH != HoverTrace
                    || !string.Equals(oldS, Stamp, StringComparison.Ordinal))
                    Line("★开关变更 → testWrite=" + TestWrite + " writeTest=" + WriteTest
                        + " verbose=" + Verbose + " hoverTrace=" + HoverTrace
                        + " patchOnForeign=" + PatchOnForeign
                        + " stamp=" + (Stamp.Length == 0 ? "(空)" : Stamp));
            }
            catch { }
        }

        private static void ReadConfig()
        {
            try
            {
                if (!File.Exists(_cfgPath)) return;
                var raw = File.ReadAllText(_cfgPath, Encoding.UTF8);
                _lastCfgRaw = raw;
                TestWrite = ReadBool(raw, "testWrite", false);
                WriteTest = ReadBool(raw, "writeTest", false);
                Verbose = ReadBool(raw, "verbose", true);
                HoverTrace = ReadBool(raw, "hoverTrace", false);
                Stamp = ReadStr(raw, "stamp", "");
                InteropLoadFrom = ReadBool(raw, "interopLoadFrom", false);
                SafeResolve.AllowInteropLoadFrom = InteropLoadFrom;
                PatchOnForeign = ReadBool(raw, "patchOnForeign", false);
                SafeResolve.AllowForeignPatchStack = PatchOnForeign;
            }
            catch { }
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

        private static string ReadStr(string raw, string key, string dflt)
        {
            try
            {
                var m = Regex.Match(raw, "\"" + Regex.Escape(key) + "\"\\s*:\\s*\"([^\"]*)\"", RegexOptions.IgnoreCase);
                if (m.Success) return m.Groups[1].Value;
            }
            catch { }
            return dflt ?? "";
        }

        public static void Line(string msg)
        {
            if (!Verbose) return;
            var line = "[OI " + DateTime.Now.ToString("HH:mm:ss.fff", CultureInfo.InvariantCulture) + "] " + msg;
            Append(line + Environment.NewLine);
            try { if (_log != null) _log.LogInfo(line); } catch { }
        }

        public static void Mark(string msg)
        {
            var line = "[OI*" + DateTime.Now.ToString("HH:mm:ss.fff", CultureInfo.InvariantCulture) + "] " + msg;
            Append(line + Environment.NewLine);
            try { if (_log != null) _log.LogInfo(line); } catch { }
        }

        public static void Error(string msg)
        {
            var line = "[OI!" + DateTime.Now.ToString("HH:mm:ss.fff", CultureInfo.InvariantCulture) + "] " + msg;
            Append(line + Environment.NewLine);
            try { if (_log != null) _log.LogError(line); } catch { }
        }

        
        
        
        
        public static void Raw(string msg)
        {
            Append(msg + Environment.NewLine);
        }

        private static void Append(string text)
        {
            lock (_lock)
            {
                try { if (_logPath != null) File.AppendAllText(_logPath, text, new UTF8Encoding(false)); }
                catch { }
            }
        }
    }
}
