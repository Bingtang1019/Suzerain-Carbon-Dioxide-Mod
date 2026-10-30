using System;
using System.Reflection;
using BepInEx.Logging;
using HarmonyLib;

namespace Oikonomia
{
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    internal static class DecreePatches
    {
        private static ManualLogSource _log;
        private static Type _panelType;

        private static bool _inSetup;
        private static bool _mountedOnce;
        private static string _currentPanelName;

        public static void Install(Harmony harmony, Assembly asm, ManualLogSource log)
        {
            _log = log;

            _panelType = Reflect.FindType(asm, "OneTimeDecreesPanel");
            if (_panelType == null)
            {
                Suzerain.DiagKit.Diag.Fail("OneTimeDecreesPanel", "类型未找到 —— 模组功能全部禁用");
                _log.LogError("[OI] 未找到 OneTimeDecreesPanel —— 模组功能全部禁用（游戏版本可能已变化）。");
                return;
            }

            int ok = 0, missing = 0;

            
            ok += Add(harmony, "Setup", nameof(Pre_PanelSetup), nameof(Post_PanelSetup), ref missing);
            ok += Add(harmony, "ShowDecreeList", nameof(Pre_ShowDecreeList), nameof(Post_ShowDecreeList), ref missing);
            ok += Add(harmony, "ResetPageCounter", nameof(Pre_ResetPageCounter), nameof(Post_ResetPageCounter), ref missing);

            
            ok += Add(harmony, "ShowDecreeDetails", nameof(Pre_EnsureTarget), null, ref missing);
            ok += Add(harmony, "OnDecreeClick", nameof(Pre_EnsureTarget), null, ref missing);
            ok += Add(harmony, "OnSignConfirmed", nameof(Pre_EnsureTarget), nameof(Post_OnSignConfirmed), ref missing);

            _log.LogInfo("[OI] 补丁挂载完成：成功 " + ok + " 个，缺失 " + missing + " 个。");
        }

        private static int Add(Harmony harmony, string methodName, string prefix, string postfix, ref int missing)
        {
            try
            {
                var target = Reflect.FindMethod(_panelType, methodName);
                if (target == null)
                {
                    missing++;
                    Suzerain.DiagKit.Diag.Missing("OneTimeDecreesPanel." + methodName);
                    _log.LogWarning("[OI] 未找到 OneTimeDecreesPanel." + methodName + " —— 跳过该补丁。");
                    return 0;
                }
                var pre = prefix == null ? null : new HarmonyMethod(
                    typeof(DecreePatches).GetMethod(prefix, BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public));
                var post = postfix == null ? null : new HarmonyMethod(
                    typeof(DecreePatches).GetMethod(postfix, BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public));
                harmony.Patch(target, prefix: pre, postfix: post);
                Suzerain.DiagKit.Diag.Ok("OneTimeDecreesPanel." + methodName);
                _log.LogDebug("[OI] 已挂载 OneTimeDecreesPanel." + methodName);
                return 1;
            }
            catch (Exception e)
            {
                missing++;
                Suzerain.DiagKit.Diag.Fail("OneTimeDecreesPanel." + methodName, e.Message);
                _log.LogError("[OI] 挂载 " + methodName + " 失败：" + e.Message);
                return 0;
            }
        }

        

        private static object PropsOf(object panel)
        {
            var data = Reflect.Prop(panel, "currentOneTimeDecreesPanelData");
            if (data == null) return null;
            return Reflect.Prop(data, "OneTimeDecreesPanelProperties");
        }

        private static string PanelNameOf(object data)
        {
            return Reflect.Str(Reflect.Prop(data, "NameInDatabase"));
        }

        
        private static string WriteMax(object props, int value)
        {
            if (props == null) return "面板属性对象为空";
            try
            {
                
                
                var err = Reflect.SetPropOrFieldReason(props, "MaxNumberOfEnactedDecrees", value);
                if (err != null) return err;
                int back = Reflect.ToInt(Reflect.Prop(props, "MaxNumberOfEnactedDecrees"));
                return back == value ? null : ("写入后读回=" + back);
            }
            catch (Exception e) { return "写入后回读异常：" + e.Message; }
        }

        private static void EnsureTarget(object panel)
        {
            try
            {
                if (!OIConfig.ShouldApply) return;
                var props = PropsOf(panel);
                if (props == null) return;
                int now = Reflect.ToInt(Reflect.Prop(props, "MaxNumberOfEnactedDecrees"));
                if (now == OIConfig.TargetValue) return;
                var err = WriteMax(props, OIConfig.TargetValue);
                if (err != null) _log.LogWarning("[OI] 写入上限失败：" + err);
                else if (OIConfig.VerboseLog.Value) _log.LogInfo("[OI] 上限已写入：" + now + " → " + OIConfig.TargetValue);
            }
            catch { }
        }

        

        private static void ApplyUiState(object panel)
        {
            try
            {
                bool modOn = OIConfig.EffectiveEnabled;
                bool hideCounter = modOn && OIConfig.EffectiveHideCounter;
                bool hideSubtitle = modOn && OIConfig.EffectiveHideSubtitle;

                
                var canvas = Reflect.Prop(panel, "pageCounterContainerCanvas");
                if (Reflect.Usable(canvas))
                {
                    bool want = !hideCounter;
                    bool now = Reflect.ToBool(Reflect.Prop(canvas, "enabled"));
                    if (now != want) Reflect.SetPropOrField(canvas, "enabled", want);
                }

                
                var desc = Reflect.Prop(panel, "description");
                if (Reflect.Usable(desc))
                {
                    bool want = !hideSubtitle;
                    bool now = Reflect.ToBool(Reflect.Prop(desc, "enabled"));
                    if (now != want) Reflect.SetPropOrField(desc, "enabled", want);
                }
            }
            catch { }
        }

        

        private static void Pre_PanelSetup(object __instance, object __0)
        {
            try
            {
                _inSetup = true;
                _currentPanelName = PanelNameOf(__0);

                if (!_mountedOnce)
                {
                    _mountedOnce = true;
                    OIConfig.LogValues("模组生效");
                }

                if (!OIConfig.ShouldApply) return;
                if (!OIConfig.IsPanelAffected(_currentPanelName)) return;

                
                
                
                if (OIConfig.EffectiveDynamicLimit)
                {
                    bool ok = DecreeLimit.Recompute();
                    _log.LogInfo("[OI][limit] 总统令上限" + (ok ? "计算" : "计算失败，退回静态值") + "：" + DecreeLimit.CachedDetail);
                    Bridge.SetDecreeLimit(OIConfig.TargetValue, DecreeLimit.CachedDetail);
                }

                
                
                
                
                
                var props = Reflect.Prop(__0, "OneTimeDecreesPanelProperties");
                var err = WriteMax(props, OIConfig.TargetValue);
                if (err != null) _log.LogWarning("[OI] 上限写入失败：" + err);
                else if (OIConfig.VerboseLog.Value)
                    _log.LogInfo("[OI] 面板打开：上限写为 " + OIConfig.TargetValue + "（点位将按此数量创建）");

                Bridge.WriteState("面板打开", _currentPanelName, 0);
            }
            catch (Exception e) { _log.LogWarning("[OI] Setup 前置处理异常：" + e.Message); }
        }

        private static void Post_PanelSetup(object __instance, object __0)
        {
            try
            {
                _inSetup = false;
                if (OIConfig.ShouldApply && OIConfig.IsPanelAffected(_currentPanelName))
                {
                    EnsureTarget(__instance);
                }
                ApplyUiState(__instance);
            }
            catch (Exception e) { _log.LogWarning("[OI] Setup 后置处理异常：" + e.Message); }
        }

        private static void Pre_ShowDecreeList(object __instance)
        {
            try
            {
                if (!OIConfig.ShouldApply) return;
                if (!OIConfig.IsPanelAffected(_currentPanelName)) return;
                
                EnsureTarget(__instance);
            }
            catch { }
        }

        private static void Post_ShowDecreeList(object __instance)
        {
            try { ApplyUiState(__instance); } catch { }
        }

        private static void Pre_ResetPageCounter(object __instance)
        {
            try
            {
                if (!OIConfig.ShouldApply) return;
                if (!OIConfig.IsPanelAffected(_currentPanelName)) return;
                
                EnsureTarget(__instance);
            }
            catch { }
        }

        private static void Post_ResetPageCounter(object __instance)
        {
            try
            {
                if (OIConfig.ShouldApply && OIConfig.IsPanelAffected(_currentPanelName))
                    EnsureTarget(__instance);
                ApplyUiState(__instance);
            }
            catch { }
        }

        private static void Pre_EnsureTarget(object __instance)
        {
            try
            {
                if (!OIConfig.ShouldApply) return;
                if (!OIConfig.IsPanelAffected(_currentPanelName)) return;
                EnsureTarget(__instance);
            }
            catch { }
        }

        
        private static void Post_OnSignConfirmed(object __instance, object __0)
        {
            try
            {
                Bridge.WriteState("签署法令", _currentPanelName, PanelCount(__instance));
            }
            catch { }
        }

        private static int PanelCount(object panel)
        {
            try { return Reflect.ToInt(Reflect.Prop(panel, "currentEnactedDecreeCount")); } catch { return 0; }
        }
    }
}
