using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using HarmonyLib;

namespace Oikonomia.Probe
{
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    internal static class ProbeTier
    {
        public const int Off = 0;
        public const int Safe = 1;
        public const int Full = 2;

        private static readonly object _lock = new object();
        private static readonly List<KeyValuePair<string, Harmony>> _harmonies = new List<KeyValuePair<string, Harmony>>();
        private static readonly HashSet<string> _seenRefusal = new HashSet<string>(StringComparer.Ordinal);

        
        public static volatile int Tier = Safe;
        public static volatile string TierSource = "默认（安全层）";
        public static volatile bool NativeChannel;
        public static volatile bool TmpHooks;
        public static volatile bool Collect = true;
        public static volatile bool StartupPhase = true;

        
        public static int RequestedTier = Safe;
        public static bool AutoDegrade = true;
        public static bool SignalOverride;
        public static bool SafeStackOnForeignGameMethod;
        public static bool Panic;

        private static string _cfgPath;
        private static string _lastRaw;

        public static string Name(int t)
        {
            if (t == Off) return "off";
            if (t == Full) return "full";
            return "safe";
        }

        public static string TierName { get { return Name(Tier); } }

        
        
        

        public static void LoadConfig()
        {
            try
            {
                _cfgPath = Path.Combine(ProbeLog.Dir, "oi-probe.json");
                if (!File.Exists(_cfgPath)) return;
                var raw = File.ReadAllText(_cfgPath, Encoding.UTF8);
                _lastRaw = raw;
                ApplyConfig(raw, true);
            }
            catch { }
        }

        
        public static void OnConfigRaw(string raw)
        {
            if (raw == null || raw == _lastRaw) return;
            _lastRaw = raw;
            ApplyConfig(raw, false);
        }

        private static void ApplyConfig(string raw, bool initial)
        {
            try
            {
                var tierStr = ReadStr(raw, "tier", initial ? "safe" : Name(RequestedTier));
                int req = Safe;
                if (string.Equals(tierStr, "full", StringComparison.OrdinalIgnoreCase)) req = Full;
                else if (string.Equals(tierStr, "off", StringComparison.OrdinalIgnoreCase)) req = Off;
                else if (string.Equals(tierStr, "safe", StringComparison.OrdinalIgnoreCase)) req = Safe;

                bool native = ReadBool(raw, "nativeChannel", req == Full);
                bool tmp = ReadBool(raw, "tmpHooks", TmpHooks);
                bool stack = ReadBool(raw, "safeStackOnForeignGameMethod", SafeStackOnForeignGameMethod);
                bool collect = ReadBool(raw, "collect", true);
                bool panic = ReadBool(raw, "panic", false);
                bool autoDeg = ReadBool(raw, "autoDegrade", true);
                bool sigOv = ReadBool(raw, "signalOverride", false);
                bool interop = ReadBool(raw, "interopLoadFrom", SafeResolve.AllowInteropLoadFrom);
                bool foreign = ReadBool(raw, "patchOnForeign", SafeResolve.AllowForeignPatchStack);

                SafeResolve.AllowInteropLoadFrom = interop;
                SafeResolve.AllowForeignPatchStack = foreign;

                if (initial)
                {
                    RequestedTier = req;
                    NativeChannel = native && req == Full;
                    TmpHooks = tmp;
                    SafeStackOnForeignGameMethod = stack;
                    Collect = collect;
                    AutoDegrade = autoDeg;
                    SignalOverride = sigOv;
                    Panic = panic;
                    return;
                }

                
                bool tierChanged = (req != RequestedTier) || (native != NativeChannel) || (tmp != TmpHooks);
                Collect = collect;
                AutoDegrade = autoDeg;
                SignalOverride = sigOv;
                SafeStackOnForeignGameMethod = stack;
                RequestedTier = req;
                NativeChannel = native;
                TmpHooks = tmp;
                Panic = panic;
                if (panic)
                {
                    ProbeLog.Mark("[OI][⑦] ★cfg panic=true → 立即卸载全部探针钩子（热生效）");
                    UnpatchAll("cfg panic");
                    Tier = Off;
                    TierSource = "cfg panic（热）";
                }
                else if (tierChanged)
                {
                    ProbeLog.Mark("[OI][⑦] ★cfg 变更：tier=" + Name(req)
                                  + " nativeChannel=" + native + " tmpHooks=" + tmp
                                  + " ｜ 挂载决策在启动时做出，**本次运行不改变已挂的钩子**（重启生效）");
                }
                else
                {
                    ProbeLog.Line("[OI][⑦] cfg 热重载：collect=" + collect
                                  + " patchOnForeign=" + SafeResolve.AllowForeignPatchStack
                                  + " interopLoadFrom=" + SafeResolve.AllowInteropLoadFrom
                                  + " safeStackOnForeignGameMethod=" + stack);
                }
            }
            catch { }
        }

        public static string ConfigSummary()
        {
            return "tier=" + Name(RequestedTier)
                   + " nativeChannel=" + NativeChannel
                   + " tmpHooks=" + TmpHooks
                   + " patchOnForeign=" + SafeResolve.AllowForeignPatchStack
                   + " interopLoadFrom=" + SafeResolve.AllowInteropLoadFrom
                   + " safeStackOnForeignGameMethod=" + SafeStackOnForeignGameMethod
                   + " collect=" + Collect
                   + " health.autoDegrade=" + AutoDegrade
                   + " health.signalOverride=" + SignalOverride;
        }

        
        public static string[] LayerDoc()
        {
            return new[]
            {
                "安全层 safe（默认必挂）：纯托管 + 只读 + 不碰原生指针；只挂 Assembly-CSharp 上的钩子；绝不叠加别的模组的 detour",
                "    包含：EconomyProbe（回合结算/ED/税务/增长/旅游/贸易，全部游戏程序集）",
                "    包含：ProbeD（TemplateHUDStat.Setup / GetStatTooltipText / WarFlowManager.EndTurn）",
                "    包含：HoverProbe 的游戏程序集钩子（TemplateHUDStat.Setup / GetTooltipModifierTextString）",
                "    包含：RiziaProbe 的游戏程序集钩子（TemplateHUDStat.* / HUDPanel.* / PersistenceManager.GetCurrentStoryPack）",
                "增强层 full（默认关，cfg tier=\"full\" 才开）：额外放开**原生 il2cpp 只读通道**（真类型名/谱系/外观字段），仍不挂 interop detour",
                "    开关：nativeChannel（tier=full 时默认 true）",
                "实验级（默认关，需两个显式开关）：interop/Unity/TMP 目标 detour + 与其它模组叠加 detour —— 0.3.3/0.3.4 两次事故就在这一级",
                "    开关：tmpHooks=true（允许挂 Unity/TMP 代理方法）",
                "    开关：patchOnForeign=true（允许叠加到别的模组已 patch 的方法上）",
                "    开关：interopLoadFrom=true（允许从 BepInEx\\interop 目录 Assembly.LoadFrom 取类型）",
                "    开关：safeStackOnForeignGameMethod=true（连游戏程序集也允许叠加；默认 false = 更保守）"
            };
        }

        
        
        

        
        
        
        
        public static string TierRefusal(MethodInfo target)
        {
            string reason = null;
            string targetName = "";
            try { targetName = SafeResolve.Describe(target); } catch { }
            try
            {
                if (Tier == Off)
                {
                    reason = "tier=off（自愈禁用：连续两次未正常退出，cfg tier 或 probe-health.json 可复位）";
                }
                else if (target == null)
                {
                    reason = "目标方法不存在（null）";
                }
                else
                {
                    bool interop = SafeResolve.IsInteropTarget(target);
                    var owner = SafeResolve.ForeignPatchOwner(target);
                    if (interop)
                    {
                        if (!TmpHooks)
                        {
                            reason = "目标在 Unity/TMP interop 代理程序集上 ⇒ 安全层不挂 interop detour"
                                     + "（0.3.3/0.3.4 两次 AccessViolation 事故就出在这一类上；"
                                     + "要挂请显式设 oi-probe.json 的 tmpHooks=true）";
                        }
                        else if (owner != null && !SafeResolve.AllowForeignPatchStack)
                        {
                            reason = "已被其它模组 patch（owner=" + owner + "）⇒ 拒绝叠加 IL2CPP detour"
                                     + "（要放行设 oi-probe.json 的 patchOnForeign=true）";
                        }
                    }
                    else
                    {
                        if (owner != null && !SafeStackOnForeignGameMethod)
                        {
                            reason = "已被其它模组 patch（owner=" + owner + "）⇒ 拒绝叠加 IL2CPP detour"
                                     + "（要放行设 oi-probe.json 的 safeStackOnForeignGameMethod=true）";
                        }
                        else if (!SafeStackOnForeignGameMethod)
                        {
                            
                            
                            int owners = OwnerCount(target);
                            if (owners > 0)
                                reason = "该方法上已有 " + owners + " 个 patch owner（含探针自己的其它组）"
                                         + "⇒ 安全层不建立多 detour 链（要放行设 oi-probe.json 的"
                                         + " safeStackOnForeignGameMethod=true）";
                        }
                    }
                }
            }
            catch (Exception e)
            {
                reason = "分层体检异常 ⇒ 保守拒绝（" + e.GetType().Name + "）";
            }

            if (reason != null) RecordSkip(targetName, reason);
            return reason;
        }

        
        public static int OwnerCount(MethodBase target)
        {
            try
            {
                var info = HarmonyLib.Harmony.GetPatchInfo(target);
                if (info == null || info.Owners == null) return 0;
                return info.Owners.Count;
            }
            catch { return 0; }
        }

        private static void RecordSkip(string targetName, string reason)
        {
            try
            {
                var key = targetName;
                bool first;
                lock (_lock) { first = _seenRefusal.Add(key); }
                ProbeStatus.Record("skip:" + key, "skipped", key, reason);
                if (first) ProbeLog.Line("[OI][①] 未挂（分层闸门）：" + key + " ｜ " + reason);
            }
            catch { }
        }

        
        
        

        public static void Register(string ownerId, Harmony harmony)
        {
            if (harmony == null) return;
            lock (_lock) { _harmonies.Add(new KeyValuePair<string, Harmony>(ownerId ?? "?", harmony)); }
        }

        
        public static void EndStartup()
        {
            StartupPhase = false;
            ProbeNativeGuard.BindGameThread("启动期结束");
            try
            {
                List<KeyValuePair<string, Harmony>> hs;
                lock (_lock) hs = new List<KeyValuePair<string, Harmony>>(_harmonies);
                foreach (var kv in hs)
                {
                    var patched = PatchedOf(kv.Value);
                    if (patched.Count == 0)
                    {
                        ProbeStatus.Record("harmony:" + kv.Key, "mounted", kv.Key,
                            "该 Harmony 实例 patch 表为空（本组没有挂任何钩子）");
                        continue;
                    }
                    foreach (var m in patched)
                    {
                        var nm = "";
                        try { nm = SafeResolve.Describe(m); } catch { }
                        ProbeStatus.Record("hook:" + nm, "mounted", nm, "harmony=" + kv.Key + "（Harmony 实例真实 patch 表）");
                    }
                }
            }
            catch { }
        }

        private static List<MethodBase> PatchedOf(Harmony h)
        {
            var list = new List<MethodBase>();
            try
            {
                var mi = typeof(Harmony).GetMethod("GetPatchedMethods",
                    BindingFlags.Public | BindingFlags.Instance);
                if (mi == null) return list;
                var e = mi.Invoke(h, null) as System.Collections.IEnumerable;
                if (e == null) return list;
                foreach (var o in e)
                {
                    var mb = o as MethodBase;
                    if (mb != null) list.Add(mb);
                }
            }
            catch { }
            return list;
        }

        
        public static bool Guard(string hookId, Action body)
        {
            if (!Collect) return false;
            if (body == null) return false;
            try
            {
                body();
                return true;
            }
            catch (Exception e)
            {
                try
                {
                    ProbeStatus.Record("trip:" + hookId, "tripped", hookId,
                        "运行期异常 → 立即卸载自己：" + e.GetType().Name + " : " + e.Message);
                    ProbeLog.Error("[OI][②] 钩子 " + hookId + " 运行期抛异常 ⇒ 立即卸载自己（不让它反复崩）："
                                   + e.GetType().Name + " : " + e.Message);
                }
                catch { }
                Unpatch(hookId, "运行期异常自我卸载");
                return false;
            }
        }

        
        public static void Unpatch(string hookId, string why)
        {
            try
            {
                List<KeyValuePair<string, Harmony>> hs;
                lock (_lock) hs = new List<KeyValuePair<string, Harmony>>(_harmonies);
                foreach (var kv in hs)
                {
                    try { kv.Value.UnpatchSelf(); } catch { }
                }
                ProbeStatus.Record("unpatch:" + hookId, "unmounted", hookId, why);
            }
            catch { }
        }

        
        public static void UnpatchAll(string why)
        {
            try
            {
                List<KeyValuePair<string, Harmony>> hs;
                lock (_lock) hs = new List<KeyValuePair<string, Harmony>>(_harmonies);
                foreach (var kv in hs)
                {
                    try
                    {
                        kv.Value.UnpatchSelf();
                        ProbeLog.Mark("[OI][②] 已卸载 " + kv.Key + " 的全部钩子（" + why + "）");
                    }
                    catch (Exception e)
                    {
                        ProbeLog.Error("[OI][②] 卸载 " + kv.Key + " 失败：" + e.GetType().Name + " : " + e.Message);
                    }
                }
                ProbeStatus.Record("unpatch-all", "unmounted", "全部", why);
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
    }
}
