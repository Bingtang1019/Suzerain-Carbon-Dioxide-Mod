using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using HarmonyLib;
using Suzerain.PatchBus;

namespace Oikonomia.Probe
{
    
    
    
    
    
    
    
    
    internal static class ProbeD
    {
        private const BindingFlags All = BindingFlags.Static | BindingFlags.Instance
                                       | BindingFlags.Public | BindingFlags.NonPublic;

        private static Type _statType;
        private static Type _dataType;
        private static int _setupCount;
        private static int _tooltipCalls;
        private static volatile bool _inSetup;
        private static int _limitCalls;
        private static readonly HashSet<string> _colorLogged = new HashSet<string>();

        public static void Install(Harmony harmony, Assembly asm)
        {
            ProbeLog.Mark("[D] ===== D 组探针挂载 =====");
            _statType = Reflect.FindType(asm, "TemplateHUDStat");
            _dataType = Reflect.FindType(asm, "HUDStatData");
            ProbeLog.Mark("[D] 类型：TemplateHUDStat=" + (_statType != null) + " HUDStatData=" + (_dataType != null));
            if (_statType == null) { ProbeLog.Error("[D] 无 TemplateHUDStat，退出"); return; }

            
            try
            {
                var fsb = new StringBuilder();
                foreach (var f in _statType.GetFields(All | BindingFlags.DeclaredOnly))
                    fsb.Append(f.FieldType.Name).Append(' ').Append(f.Name).Append("; ");
                ProbeLog.Line("[D][info] TemplateHUDStat 字段: " + fsb);

                var msb = new StringBuilder();
                foreach (var m in _statType.GetMethods(All | BindingFlags.DeclaredOnly))
                {
                    if (m.Name.StartsWith("get_") || m.Name.StartsWith("set_")) continue;
                    msb.Append(m.Name).Append('/').Append(m.GetParameters().Length).Append(' ');
                }
                ProbeLog.Line("[D][info] TemplateHUDStat 方法: " + msb);
            }
            catch (Exception e) { ProbeLog.Error("[D][info] 结构快照失败：" + e.Message); }

            
            try
            {
                var setup = _dataType != null ? AccessTools.Method(_statType, "Setup", new[] { _dataType }) : null;
                if (setup == null) setup = Reflect.FindMethod(_statType, "Setup");
                if (setup != null)
                {
                    var refusal = SafeResolve.PatchRefusal(setup);
                    if (refusal != null)
                    {
                        ProbeLog.Line("[D][mount] · TemplateHUDStat.Setup 未挂（体检）：" + refusal);
                    }
                    else
                    {
                        
                        PatchBus.SubscribePrefixReadOnly(harmony, "OI.Probe.D.Setup.pre",
                            setup, PatchScope.Any, 50, ctx => Pre_Setup());
                        PatchBus.SubscribePostfix(harmony, "OI.Probe.D.Setup.post",
                            setup, PatchScope.Any, 50, ctx =>
                            {
                                object arg0 = ctx.Args != null && ctx.Args.Length > 0 ? ctx.Args[0] : null;
                                Post_Setup(ctx.Instance, arg0);
                            });
                        ProbeLog.Line("[D][mount] ✓ TemplateHUDStat.Setup（PatchBus）");
                    }
                }
                else ProbeLog.Error("[D][mount] ✗ 未找到 Setup");
            }
            catch (Exception e) { ProbeLog.Error("[D][mount] Setup 挂载失败：" + e.Message); }

            
            try
            {
                var g = _dataType != null
                    ? AccessTools.Method(_statType, "GetTooltipModifierTextString", new[] { _dataType })
                    : null;
                if (g == null) g = Reflect.FindMethod(_statType, "GetTooltipModifierTextString");
                if (g != null)
                {
                    var refusal = SafeResolve.PatchRefusal(g);
                    if (refusal != null)
                    {
                        ProbeLog.Line("[D][mount] · TemplateHUDStat.GetTooltipModifierTextString 未挂（体检）：" + refusal);
                    }
                    else
                    {
                        harmony.Patch(g, postfix: new HarmonyMethod(typeof(ProbeD).GetMethod("Post_GetTooltip", All)));
                        ProbeLog.Line("[D][mount] ✓ TemplateHUDStat.GetTooltipModifierTextString");
                    }
                }
                else ProbeLog.Error("[D][mount] ✗ 未找到 GetTooltipModifierTextString");
            }
            catch (Exception e) { ProbeLog.Error("[D][mount] GetTooltip 挂载失败：" + e.Message); }

            
            try
            {
                var flow = Reflect.FindType(asm, "GameFlowManager");
                var endTurn = flow != null ? Reflect.FindMethod(flow, "EndTurn") : null;
                if (endTurn != null)
                {
                    var refusal = SafeResolve.PatchRefusal(endTurn);
                    if (refusal != null)
                    {
                        ProbeLog.Line("[D][mount] · GameFlowManager.EndTurn 未挂（体检）：" + refusal);
                    }
                    else
                    {
                        harmony.Patch(endTurn, postfix: new HarmonyMethod(typeof(ProbeD).GetMethod("Post_FlowEndTurn", All)));
                        ProbeLog.Line("[D][mount] ✓ GameFlowManager.EndTurn（D 探针侧）");
                    }
                }
            }
            catch (Exception e) { ProbeLog.Error("[D][mount] EndTurn 挂载失败：" + e.Message); }

            ProbeLog.Mark("[D] ===== D 组探针挂载完成 =====");
        }

        

        private static void Pre_Setup() { _inSetup = true; }

        private static void Post_Setup(object __instance, object __0)
        {
            try
            {
                _inSetup = false;
                _setupCount++;
                string name = NameOf(__0);
                var sb = new StringBuilder();
                sb.Append("[D-V1V3V5] Setup#").Append(_setupCount).Append(" data=").Append(name);

                object hide = Reflect.PropOrField(__instance, "hideModifier", "hideModifier");
                sb.Append(" | hideModifier=").Append(hide == null ? "<null>" : (Reflect.ToBool(hide) ? "TRUE" : "false"));

                var tt = Reflect.PropOrField(__instance, "statTooltipModifierText", "statTooltipModifierText");
                sb.Append(" | tooltipTextObj=").Append(tt == null ? "<null>" : tt.GetType().Name);
                if (Reflect.Usable(tt))
                {
                    string txt = Reflect.Str(Reflect.Prop(tt, "text"));
                    if (txt == null) txt = Reflect.Str(Reflect.PropOrField(tt, "m_text", "m_text"));
                    sb.Append(" text=[").Append(Esc(txt)).Append("](len=").Append(txt == null ? -1 : txt.Length).Append(')');
                    var go = Reflect.Prop(tt, "gameObject");
                    sb.Append(" go.activeSelf=").Append(Reflect.Str(Reflect.Prop(go, "activeSelf")));
                    sb.Append(" enabled=").Append(Reflect.Str(Reflect.Prop(tt, "enabled")));
                }
                ProbeLog.Mark(sb.ToString());

                
                foreach (var cn in new[] { "tooltipPositiveColor", "tooltipNegativeColor", "tooltipNeutralColor",
                                           "tooltipModifierColor", "positiveColor", "negativeColor", "modifierColor" })
                {
                    object v = Reflect.PropOrField(__instance, cn, cn);
                    if (v == null) continue;
                    ProbeLog.Line("[D-V5] " + cn + " 读到 " + ColorStr(v)
                        + (_colorLogged.Add(cn) ? "  ★首次" : ""));
                }

                
                if (ShouldDump(name))
                {
                    DumpFields("[D-V1V5][fields] " + name, __instance);
                    DumpFields("[D-V1V5][data] " + name, __0);
                    ProbeDecreeLimit("HUD.Setup(" + name + ")");
                }
            }
            catch (Exception e) { ProbeLog.Error("[D] Post_Setup 异常：" + e.GetType().Name + " : " + e.Message); }
        }

        private static bool ShouldDump(string name)
        {
            if (string.IsNullOrEmpty(name)) return false;
            return name.IndexOf("Budget", StringComparison.OrdinalIgnoreCase) >= 0
                || name.IndexOf("Government", StringComparison.OrdinalIgnoreCase) >= 0
                || name.IndexOf("Authority", StringComparison.OrdinalIgnoreCase) >= 0
                || name.IndexOf("Decree", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        

        private static void Post_GetTooltip(object __instance, object __0, object __result)
        {
            try
            {
                int n = ++_tooltipCalls;
                if (n > 40) return;
                ProbeLog.Mark("[D-V2] GetTooltipModifierTextString 调用#" + n
                    + " data=" + NameOf(__0)
                    + " 调用时在Setup内=" + (_inSetup ? "是" : "否")
                    + " 返回值=[" + Esc(Reflect.Str(__result)) + "]");
            }
            catch { }
        }

        

        private static void Post_FlowEndTurn()
        {
            try { ProbeDecreeLimit("EndTurn.post"); } catch { }
        }

        private static void ProbeDecreeLimit(string where)
        {
            if (_limitCalls >= 3) return;
            _limitCalls++;
            try
            {
                Assembly oi = null;
                foreach (var a in AppDomain.CurrentDomain.GetAssemblies())
                {
                    try { if (a.GetName().Name == "Oikonomia") { oi = a; break; } } catch { }
                }
                if (oi == null) { ProbeLog.Error("[D-LIMIT] 未找到 Oikonomia 程序集"); return; }

                var ep = oi.GetType("Oikonomia.EconomyPatches");
                var dl = oi.GetType("Oikonomia.DecreeLimit");
                if (dl == null) { ProbeLog.Error("[D-LIMIT] 未找到 Oikonomia.DecreeLimit"); return; }

                
                object flow = null;
                try
                {
                    var f = typeof(EconomyProbe).GetField("_flowInstance", All);
                    if (f != null) flow = f.GetValue(null);
                }
                catch { }
                bool injected = false;
                var flowField = ep != null ? ep.GetField("_flowInstance", All) : null;
                if (flowField != null && Reflect.Usable(flow))
                {
                    try { flowField.SetValue(null, flow); injected = true; } catch { }
                }
                ProbeLog.Mark("[D-LIMIT] " + where + " | OI 程序集=✓ EconomyPatches=" + (ep != null)
                    + " | 探针捕获的 flow=" + (Reflect.Usable(flow) ? "非空(回合=" + Reflect.Str(Reflect.Prop(flow, "currentTurnNo")) + ")" : "<null>")
                    + " | 已注入 flow=" + injected);

                var recompute = dl.GetMethod("Recompute", All);
                if (recompute == null) { ProbeLog.Error("[D-LIMIT] 未找到 Recompute()"); return; }
                object okObj = recompute.Invoke(null, null);
                bool ok = okObj is bool b && b;
                var pc = dl.GetProperty("Cached", All);
                var pd = dl.GetProperty("CachedDetail", All);
                ProbeLog.Mark("[D-LIMIT] 真机复算结果：Recompute()=" + ok
                    + " 上限=" + (pc != null ? Reflect.Str(pc.GetValue(null)) : "?")
                    + " 明细=" + (pd != null ? Reflect.Str(pd.GetValue(null)) : "?"));
            }
            catch (Exception e)
            {
                ProbeLog.Error("[D-LIMIT] 异常：" + e.GetType().Name + " : " + e.Message
                    + (e.InnerException != null ? " | inner: " + e.InnerException.Message : ""));
            }
        }

        

        private static string NameOf(object data)
        {
            if (!Reflect.Usable(data)) return "<null>";
            string n = Reflect.Str(Reflect.Prop(data, "NameInDatabase"));
            if (string.IsNullOrEmpty(n)) n = Reflect.Str(Reflect.PropOrField(data, "name", "name"));
            return n ?? "?";
        }

        private static string Esc(string s)
        {
            if (s == null) return "<null>";
            return s.Replace("\\", "\\\\").Replace("\n", "\\n").Replace("\r", "\\r").Replace("\t", "\\t");
        }

        private static string Fmt(object v)
        {
            if (v == null) return "<null>";
            try
            {
                var t = v.GetType();
                if (t.FullName == "UnityEngine.Color" || t.Name == "Color") return ColorStr(v);
                return Reflect.Str(v) ?? "<null-str>";
            }
            catch { return "<err>"; }
        }

        private static string ColorStr(object v)
        {
            try
            {
                var t = v.GetType();
                var sb = new StringBuilder();
                sb.Append(t.FullName).Append('{');
                foreach (var fn in new[] { "r", "g", "b", "a" })
                {
                    var f = t.GetField(fn, All);
                    sb.Append(fn).Append('=').Append(f != null ? Reflect.Str(f.GetValue(v)) : "?").Append(' ');
                }
                sb.Append("} ToString=").Append(Reflect.Str(v));
                return sb.ToString();
            }
            catch (Exception e) { return "<Color读取失败:" + e.GetType().Name + ">"; }
        }

        
        private static void DumpFields(string tag, object obj)
        {
            if (!Reflect.Usable(obj)) { ProbeLog.Line(tag + " <null/unusable>"); return; }
            var sb = new StringBuilder();
            sb.Append(tag).Append(" type=").Append(obj.GetType().FullName);
            try
            {
                foreach (var f in obj.GetType().GetFields(All))
                {
                    string v;
                    try { v = Fmt(f.GetValue(obj)); }
                    catch (Exception e) { v = "<ERR:" + e.GetType().Name + ">"; }
                    sb.Append("\n      ").Append(f.FieldType.Name).Append(' ').Append(f.Name).Append(" = ").Append(v);
                }
            }
            catch (Exception e) { sb.Append(" <fields ERR:" + e.Message + ">"); }
            ProbeLog.Line(sb.ToString());
        }
    }
}
