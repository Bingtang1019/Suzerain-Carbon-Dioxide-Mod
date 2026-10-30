using System;
using System.Reflection;
using BepInEx;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;

namespace Oikonomia
{
    
    
    
    
    
    
    
    
    
    
    [BepInPlugin(Guid, "Oikonomia", PluginVersion)]
    public class Plugin : BasePlugin
    {
        public const string Guid = "com.dell.suzerain.oikonomia";
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        public const string PluginVersion = VersionInfo.PluginVersion;

        public override void Load()
        {
            Log.LogInfo("[OI] === 安济艺术 Oikonomia " + PluginVersion + " 启动 ===");

            try
            {
                OIConfig.Init(Config, Log);
                EconomyConfig.Init(Config, Log);
            }
            catch (Exception e)
            {
                Log.LogError("[OI] 配置初始化失败（将使用内置默认值）：" + e.Message);
            }

            try
            {
                Bridge.Init(Log);
                Bridge.WriteState("插件启动", "", 0);
            }
            catch (Exception e)
            {
                Log.LogWarning("[OI] 文件桥初始化失败（不影响本体）：" + e.Message);
            }

            Assembly asm;
            try
            {
                asm = Assembly.Load("Assembly-CSharp");
            }
            catch (Exception e)
            {
                Log.LogError("[OI] 无法加载 Assembly-CSharp，模组禁用：" + e.Message);
                return;
            }

            try
            {
                var harmony = new Harmony(Guid);
                
                Suzerain.DiagKit.Diag.Reset();
                Suzerain.DiagKit.Diag.Label = "挂载点";
                Suzerain.DiagKit.Diag.Sink = e =>
                {
                    var line = Suzerain.DiagKit.Diag.EntryLine(e);
                    if (e.Status == Suzerain.DiagKit.DiagStatus.Failed) Log.LogError(line);
                    else if (e.Status == Suzerain.DiagKit.DiagStatus.Missing) Log.LogWarning(line);
                };
                DecreeLimit.Init(asm, Log);
                DecreePatches.Install(harmony, asm, Log);
                EconomyPatches.Install(harmony, asm, Log);
            }
            catch (Exception e)
            {
                Log.LogError("[OI] 补丁安装失败，模组禁用：" + e.Message);
            }

            Log.LogInfo("[OI][diag] " + Suzerain.DiagKit.Diag.Summary());
            if (Suzerain.DiagKit.Diag.Problems().Count > 0)
                Log.LogInfo("[OI][diag] " + Suzerain.DiagKit.Diag.Detail());
            try { CfgGuard.Init(Log); EconomyPatches.BindConfigFile(this.Config); EconomyPatches.LogConfigFacts(); } catch { }
            Log.LogInfo("[OI] 初始化完成。");
        }

        
        
        private void OnDestroy()
        {
            try { Bridge.Stop(); } catch { }
            try { EconomyPatches.ArchiveLogForMe("退出"); } catch { }
        }

        
        private void OnApplicationQuit()
        {
            try { EconomyPatches.ArchiveLogForMe("退出quit"); } catch { }
        }
    }
}
