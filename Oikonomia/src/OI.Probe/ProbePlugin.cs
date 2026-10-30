using System;
using System.IO;
using System.Reflection;
using BepInEx;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using Suzerain.PatchBus;

namespace Oikonomia.Probe
{
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    [BepInPlugin(Guid, "Oikonomia Probe", PluginVersion)]
    public class ProbePlugin : BasePlugin
    {
        public const string Guid = "com.dell.suzerain.oikonomia.probe";
        
        public const string PluginVersion = "0.4.0";

        public override void Load()
        {
            try { ProbeLog.Init(Log); } catch { }

            ProbeTier.StartupPhase = true;

            
            try
            {
                ProbeTier.LoadConfig();
                int eff = ProbeHealth.BeginSession(ProbeTier.RequestedTier, ProbeTier.AutoDegrade, ProbeTier.SignalOverride);
                ProbeTier.Tier = eff;
                ProbeTier.TierSource = string.IsNullOrEmpty(ProbeHealth.Reason)
                    ? "cfg tier=" + ProbeTier.Name(ProbeTier.RequestedTier)
                    : "自愈：" + ProbeHealth.Reason;
                ProbeStatus.Begin(Path.Combine(ProbeLog.Dir, "probe-status.json"),
                    ProbeTier.Name(eff), ProbeTier.TierSource, ProbeHealth.Reason,
                    ProbeHealth.PreviousCleanExit, ProbeHealth.PreviousStreak);
            }
            catch (Exception e)
            {
                try { ProbeLog.Error("[OI] 分层/自愈初始化失败（按最保守方式继续）：" + e.Message); } catch { }
                ProbeTier.Tier = ProbeTier.Safe;
            }

            
            try
            {
                ProbeLog.Mark("[OI] === Oikonomia Probe " + PluginVersion + " 启动 ===");
                ProbeLog.Mark("[OI][①] 本次层 = " + ProbeTier.TierName
                              + " ｜ 来源 = " + ProbeTier.TierSource
                              + " ｜ " + ProbeTier.ConfigSummary());
                if (!string.IsNullOrEmpty(ProbeHealth.Reason))
                    ProbeLog.Mark("[OI][③] ★自愈生效：" + ProbeHealth.Reason);
                foreach (var l in ProbeTier.LayerDoc()) ProbeLog.Line("[OI][①] " + l);
                ProbeLog.Line("[OI][③] 健康文件：" + ProbeHealth.FilePath
                              + " ｜ 上次正常退出=" + ProbeHealth.PreviousCleanExit
                              + " ｜ 连续异常=" + ProbeHealth.PreviousStreak
                              + " ｜ 历史 sessions=" + ProbeHealth.Sessions
                              + " cleanExits=" + ProbeHealth.CleanExits);
            }
            catch { }

            
            InstallCleanExitHooks();

            if (ProbeTier.Tier == ProbeTier.Off)
            {
                try
                {
                    ProbeLog.Mark("[OI][③] ★★ 自愈已完全禁用探针：本次不挂任何钩子、不碰任何游戏对象。"
                                  + "（复位办法：删除 " + ProbeHealth.FilePath + " 或把 oi-probe.json 的 tier 设为 safe）");
                    ProbeStatus.Record("self", "skipped", "探针", "tier=off（自愈：连续两次未正常退出）");
                    ProbeStatus.Flush();
                }
                catch { }
                ProbeTier.StartupPhase = false;
                return;
            }

            try
            {
                
                Assembly asm = null;
                try { asm = SafeResolve.LoadedByName("Assembly-CSharp"); }
                catch (Exception e) { ProbeLog.Error("[OI] 取 Assembly-CSharp 失败（已吞掉）：" + e.Message); }

                if (asm == null)
                {
                    ProbeLog.Error("[OI] 已加载程序集里没有 Assembly-CSharp ⇒ 探针不工作"
                                   + "（也**不会**去 Load 它 ✗）。已加载程序集数 = " + SafeResolve.Loaded().Length);
                    ProbeStatus.Record("self", "skipped", "Assembly-CSharp", "未加载 ⇒ 探针不工作");
                    return;
                }
                ProbeLog.Mark("[OI] Assembly-CSharp 就绪（机制①：已加载程序集直查，零副作用）");

                
                PatchBus.Configure(null, line => ProbeLog.Line(line));

                
                
                
                ProbeLog.Line("[OI] 启动安全闸由各组分别执行（HoverProbe / RiziaProbe 内的 RunGate）");

                
                var harmony = new Harmony(Guid);
                ProbeTier.Register("oi", harmony);

                MountGroup("economy", delegate
                {
                    var h = new Harmony(Guid + ".economy");
                    ProbeTier.Register("economy", h);
                    EconomyProbe.Install(h, asm, Log);
                });
                MountGroup("d", delegate
                {
                    var h = new Harmony(Guid + ".d");
                    ProbeTier.Register("d", h);
                    ProbeD.Install(h, asm);
                });
                MountGroup("h", delegate
                {
                    var h = new Harmony(Guid + ".h");
                    ProbeTier.Register("h", h);
                    HoverProbe.Install(h, asm);
                });
                MountGroup("r", delegate
                {
                    var h = new Harmony(Guid + ".r");
                    ProbeTier.Register("r", h);
                    RiziaProbe.Install(h, asm);
                });
            }
            catch (Exception e)
            {
                ProbeLog.Error("[OI] 挂载阶段异常（已吞掉，游戏继续）：" + e.GetType().Name + " : " + e.Message);
            }
            finally
            {
                try
                {
                    ProbeTier.EndStartup();
                    ProbeStatus.Flush();
                    ProbeHealth.SetMounts(ProbeStatus.MountedTargets());
                    ProbeLog.Mark("[OI][⑥] " + ProbeStatus.OneLine());
                    ProbeLog.Mark("[OI][④] " + ProbeNativeGuard.Describe());
                    ProbeLog.Mark("[OI][⑥] 状态文件：" + ProbeStatus.Path);
                    ProbeLog.Mark("[OI][安全闸] 挂载结束。" + SafeResolve.OneLineSummary());
                }
                catch { }
            }
        }

        private static void MountGroup(string name, Action mount)
        {
            try
            {
                mount();
            }
            catch (Exception e)
            {
                try
                {
                    ProbeStatus.Record("group:" + name, "failed", name, e.GetType().Name + " : " + e.Message);
                    ProbeLog.Error("[OI][②] 分组 " + name + " 挂载异常（只影响本组，其它组继续）："
                                   + e.GetType().Name + " : " + e.Message);
                }
                catch { }
            }
        }

        
        
        

        private static bool _cleanExitHooked;

        private static void InstallCleanExitHooks()
        {
            if (_cleanExitHooked) return;
            _cleanExitHooked = true;

            
            try
            {
                AppDomain.CurrentDomain.ProcessExit += delegate { ProbeHealth.MarkCleanExit("AppDomain.ProcessExit"); };
                ProbeLog.Line("[OI][③] 已注册 AppDomain.ProcessExit（纯托管）作为「正常退出」判据");
            }
            catch (Exception e)
            {
                ProbeLog.Error("[OI][③] 注册 ProcessExit 失败（退化为「下次启动判定」）：" + e.Message);
            }

            
            try
            {
                var appType = SafeResolve.FindTypePreferred("UnityEngine.Application",
                    new[] { "UnityEngine.CoreModule", "UnityEngine" });
                if (appType == null)
                {
                    ProbeLog.Line("[OI][③] UnityEngine.Application 不在已加载程序集 ⇒ 只用 ProcessExit");
                    return;
                }
                var ev = appType.GetEvent("quitting", BindingFlags.Public | BindingFlags.Static);
                if (ev == null)
                {
                    ProbeLog.Line("[OI][③] Application.quitting 事件不存在 ⇒ 只用 ProcessExit");
                    return;
                }
                var mi = typeof(ProbePlugin).GetMethod("OnUnityQuitting", BindingFlags.NonPublic | BindingFlags.Static);
                if (mi == null || ev.EventHandlerType == null) return;
                var d = Delegate.CreateDelegate(ev.EventHandlerType, mi);
                ev.AddEventHandler(null, d);
                ProbeLog.Line("[OI][③] 已订阅 Application.quitting（Unity 侧正常退出也会标记）");
            }
            catch (Exception e)
            {
                ProbeLog.Line("[OI][③] 订阅 Application.quitting 失败（退化：只靠 ProcessExit）：" + e.Message);
            }
        }

        private static void OnUnityQuitting()
        {
            try { ProbeHealth.MarkCleanExit("Application.quitting"); } catch { }
        }
    }
}
