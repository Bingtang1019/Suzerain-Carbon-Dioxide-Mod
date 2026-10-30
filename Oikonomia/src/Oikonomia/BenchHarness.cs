













using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using HarmonyLib;

namespace Oikonomia
{
    internal static partial class EconomyPatches
    {
        private sealed class BenchStep
        {
            public string Kind;   
            public string Needle;
            public string Flag;   
            public bool Want;
        }

        private static readonly List<BenchStep> _bench = new List<BenchStep>();
        private static int _benchStep;
        private static int _benchLines;
        private static bool _benchDone;
        private static DateTime _benchMenuAt = DateTime.MinValue;
        private static DateTime _benchClickAt = DateTime.MinValue;
        private static object _benchPending;         
        private static object _benchHandler;         
        private static readonly StringBuilder _benchEvidence = new StringBuilder();

        private static bool BenchOn { get { try { return EconomyConfig.BenchEnabled != null && EconomyConfig.BenchEnabled.Value; } catch { return false; } } }
        private static int BenchMaxSteps { get { try { return EconomyConfig.BenchMaxSteps != null ? EconomyConfig.BenchMaxSteps.Value : 120; } catch { return 120; } } }
        private static int BenchTimeoutSec { get { try { return EconomyConfig.BenchStepTimeoutSec != null ? EconomyConfig.BenchStepTimeoutSec.Value : 30; } catch { return 30; } } }

        
        internal static void InstallBench(Harmony harmony, Assembly asm, ref int ok, ref int miss)
        {
            try
            {
                if (!BenchOn) { _log.LogDebug("[OI][bench] 已跳过（cfg 关闭）"); return; }
                string file = null;
                try { if (EconomyConfig.BenchScriptFile != null) file = EconomyConfig.BenchScriptFile.Value; } catch { }
                if (string.IsNullOrEmpty(file) || !File.Exists(file))
                {
                    miss++; _log.LogWarning("[OI][bench] 脚本文件不存在 ⇒ 未挂 ✗：" + (file ?? "(空)"));
                    return;
                }
                LoadBenchScript(file);
                if (_bench.Count == 0) { miss++; _log.LogWarning("[OI][bench] 脚本为空 ⇒ 未挂 ✗"); return; }

                var t = Reflect.FindType(asm, "ConversationHandler");
                if (t == null) { miss++; _log.LogWarning("[OI][bench] 找不到 ConversationHandler ⇒ 未挂 ✗"); return; }
                int n = 0;
                foreach (var m in t.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
                {
                    if (m.Name != "OnConversationResponseMenu") continue;
                    if (m.GetParameters().Length != 1) continue;
                    try
                    {
                        harmony.Patch(m, postfix: new HarmonyMethod(typeof(EconomyPatches).GetMethod(
                            nameof(Post_BenchMenu), BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public)));
                        n++;
                    }
                    catch (Exception e) { _log.LogWarning("[OI][bench] 挂菜单失败：" + e.GetType().Name); }
                }
                _log.LogInfo("[OI][bench] 已挂 " + n + " 个菜单钩子 ✓ ｜ 脚本=" + Path.GetFileName(file)
                    + " ｜ 步数=" + _bench.Count + " ｜ 步数上限=" + BenchMaxSteps + " ｜ 每步超时=" + BenchTimeoutSec + "s ✓");
                if (n > 0) ok++; else miss++;
            }
            catch (Exception e) { miss++; _log.LogWarning("[OI][bench] 安装异常（已吞）：" + e.GetType().Name + " — " + Short(e.Message)); }
        }

        private static void LoadBenchScript(string file)
        {
            try
            {
                _bench.Clear();
                foreach (var raw in File.ReadAllLines(file, Encoding.UTF8))
                {
                    string line = (raw ?? "").Trim();
                    if (line.Length == 0 || line.StartsWith("#", StringComparison.Ordinal)) continue;
                    if (line.StartsWith("stop ", StringComparison.OrdinalIgnoreCase))
                    {
                        _bench.Add(new BenchStep { Kind = "stop", Needle = line.Substring(5).Trim() });
                    }
                    else if (line.StartsWith("expect-flag ", StringComparison.OrdinalIgnoreCase))
                    {
                        var p = line.Substring(12).Trim().Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                        if (p.Length >= 2)
                            _bench.Add(new BenchStep { Kind = "flag", Flag = p[0], Want = p[1].Equals("true", StringComparison.OrdinalIgnoreCase) });
                    }
                    else
                    {
                        _bench.Add(new BenchStep { Kind = "pick", Needle = line });
                    }
                }
            }
            catch (Exception e) { _log.LogWarning("[OI][bench] 读脚本失败：" + e.GetType().Name); }
        }

        
        private static void Post_BenchMenu(object responses, object __instance)
        {
            try
            {
                if (!BenchOn || _benchDone) return;
                _benchHandler = __instance;
                string texts = BenchOptionsText(responses);
                _benchMenuAt = DateTime.UtcNow;
                if (_benchLines < 200)
                {
                    _benchLines++;
                    _log.LogInfo("[OI][bench] 第 " + (_benchStep + 1) + " 步 ｜ 屏选项=" + Short(texts));
                }
                _benchEvidence.Append("屏=").Append(Short(texts)).Append(" ; ");

                
                if (_benchStep >= _bench.Count) { BenchVerdict("脚本已走完"); return; }

                var st = _bench[_benchStep];
                if (st.Kind == "stop")
                {
                    if (texts.IndexOf(st.Needle, StringComparison.Ordinal) >= 0)
                    {
                        _benchDone = true;
                        BenchVerdict("★ 到达停止点（stop）⇒ 不点，供定点查看 ✓");
                    }
                    return;   
                }
                if (st.Kind != "pick") { _benchStep++; return; }

                object hit = BenchFindResponse(responses, st.Needle);
                if (hit == null)
                {
                    if ((DateTime.UtcNow - _benchMenuAt).TotalSeconds > BenchTimeoutSec)
                    {
                        _benchDone = true;
                        BenchVerdict("★ 本步超时：屏里找不到「" + st.Needle + "」✗");
                    }
                    return;
                }
                if (_benchStep >= BenchMaxSteps)
                {
                    _benchDone = true;
                    BenchVerdict("★ 超过步数上限（" + BenchMaxSteps + "）⇒ 停 ✗");
                    return;
                }
                _benchPending = hit;                       
                _benchClickAt = DateTime.UtcNow.AddMilliseconds(400);
                _benchStep++;
                if (_benchLines < 200)
                {
                    _benchLines++;
                    _log.LogInfo("[OI][bench] 选中 ⇒ 「" + st.Needle + "」✓（★ 0.4 秒后调游戏自己的 OnResponseClick ✓）");
                }
            }
            catch (Exception e) { try { _log.LogWarning("[OI][bench] 菜单处理异常：" + e.GetType().Name); } catch { } }
        }

        
        private static void BenchTick()
        {
            try
            {
                if (!BenchOn || _benchDone || _benchPending == null) return;
                if (DateTime.UtcNow < _benchClickAt) return;
                object resp = _benchPending; _benchPending = null;
                if (_benchHandler == null) { _log.LogWarning("[OI][bench] 没有 handler 实例 ⇒ 点不了 ✗"); return; }
                var mi = _benchHandler.GetType().GetMethod("OnResponseClick",
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (mi == null) { _log.LogWarning("[OI][bench] 找不到 OnResponseClick ⇒ 点不了 ✗"); return; }
                mi.Invoke(_benchHandler, new[] { resp });
                if (_benchLines < 200) { _benchLines++; _log.LogInfo("[OI][bench] 已点击 ✓（★ 经由游戏自己的 OnResponseClick ✓）"); }
            }
            catch (Exception e) { try { _log.LogWarning("[OI][bench] 点击异常：" + e.GetType().Name); } catch { } }
        }

        private static string BenchOptionsText(object responses)
        {
            var sb = new StringBuilder();
            try
            {
                var en = responses as IEnumerable;
                if (en == null) return "(取不到选项 ✗)";
                int i = 0;
                foreach (var r in en)
                {
                    if (i++ > 0) sb.Append(" | ");
                    sb.Append(BenchResponseText(r));
                }
            }
            catch { }
            return sb.Length == 0 ? "(空 ✗)" : sb.ToString();
        }

        private static string BenchResponseText(object r)
        {
            try
            {
                object ft = Reflect.Prop(r, "formattedText");
                if (!Reflect.Usable(ft)) ft = Reflect.Prop(r, "FormattedText");
                if (Reflect.Usable(ft))
                {
                    var t = Reflect.Prop(ft, "text");
                    if (t != null) return t.ToString();
                    return ft.ToString();
                }
                return r == null ? "(null)" : r.ToString();
            }
            catch { return "(取文本异常 ✗)"; }
        }

        private static object BenchFindResponse(object responses, string needle)
        {
            try
            {
                var en = responses as IEnumerable;
                if (en == null) return null;
                foreach (var r in en)
                {
                    string t = BenchResponseText(r);
                    if (!string.IsNullOrEmpty(t) && t.IndexOf(needle, StringComparison.Ordinal) >= 0) return r;
                }
            }
            catch { }
            return null;
        }

        
        private static void BenchVerdict(string why)
        {
            try
            {
                var fails = new List<string>();
                var all = new List<string>();
                foreach (var st in _bench)
                {
                    if (st.Kind != "flag") continue;
                    bool got = false;
                    try { got = IsTrue(st.Flag); } catch { }
                    bool pass = (got == st.Want);
                    all.Add(Short(st.Flag) + "=" + (got ? "真" : "假") + (pass ? " ✓" : " ✗(期望" + (st.Want ? "真" : "假") + ")"));
                    if (!pass) fails.Add(Short(st.Flag));
                }
                string line = "[OI][bench] 结论=" + (fails.Count == 0 ? "PASS ✓" : "FAIL ✗")
                    + " ｜ 原因=" + why
                    + " ｜ 步=" + _benchStep + "/" + _bench.Count
                    + " ｜ 判据=" + (all.Count == 0 ? "（脚本没有 expect-flag ⇒ 只报走到哪 ✓）" : string.Join(" ; ", all.ToArray()));
                _log.LogInfo(line);
                try
                {
                    string rf = null;
                    if (EconomyConfig.BenchReportFile != null) rf = EconomyConfig.BenchReportFile.Value;
                    if (!string.IsNullOrEmpty(rf))
                    {
                        string dir = Path.GetDirectoryName(rf);
                        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir)) Directory.CreateDirectory(dir);
                        File.AppendAllText(rf, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "  " + line
                            + Environment.NewLine + "   证据=" + Short(_benchEvidence.ToString()) + Environment.NewLine,
                            new UTF8Encoding(false));
                        _log.LogInfo("[OI][bench] 已写入报告 ✓ " + rf);
                    }
                }
                catch { }
            }
            catch { }
        }
    }
}
