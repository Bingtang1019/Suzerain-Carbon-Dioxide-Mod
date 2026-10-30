using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Text;
using HarmonyLib;
using Suzerain.PatchBus;

namespace Oikonomia.Probe
{
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    internal static class HoverProbe
    {
        private const BindingFlags All = BindingFlags.Static | BindingFlags.Instance
                                       | BindingFlags.Public | BindingFlags.NonPublic;

        
        private static Type _statType, _dataType, _modType, _hudPanelType, _panelDataType;
        private static Type _componentType;      
        private static Type _eventTriggerType;   
        private static Type _buttonType;         
        private static Type _selectableType;     
        private static Type _tmpType;            
        private static Type _canvasGroupType;    
        private static Type _rectMaskType;       
        private static Type _maskType;           
        private static Type _canvasType;         

        
        private static readonly string[] TypeRows =
        {
            "TemplateHUDStat", "HUDStatData", "HUDPeriodicStatModifierData", "HUDPanel", "HUDPanelData",
            "UnityEngine.GameObject", "UnityEngine.Component", "UnityEngine.Transform", "UnityEngine.RectTransform",
            "UnityEngine.EventSystems.EventTrigger", "UnityEngine.UI.Button", "UnityEngine.UI.Selectable",
            "UnityEngine.UI.Image", "UnityEngine.UI.LayoutGroup", "UnityEngine.UI.HorizontalOrVerticalLayoutGroup",
            "UnityEngine.UI.ContentSizeFitter", "UnityEngine.UI.LayoutElement", "UnityEngine.UI.RectMask2D",
            "UnityEngine.UI.Mask", "UnityEngine.CanvasGroup", "UnityEngine.Canvas",
            "UnityEngine.Sprite", "UnityEngine.RectOffset",
            "TMPro.TMP_Text", "TMPro.TextMeshProUGUI"
        };

        
        private static object _budgetStat;       
        private static object _mainTmp;          
        private static object _backupTmp;        
        private static bool _dumped;             
        private static bool _capturedLogged;
        private static int _traceCount;          
        private static int _tmpSetSeen;          

        
        
        
        
        private static int TraceCap { get { return ProbeLog.HoverTrace ? 600 : 150; } }

        
        private const string StampTag = " 【OI-STAMP】";

        
        private const string MarkerToken = "OI-BUDGET-DETAIL";

        
        
        

        public static void Install(Harmony harmony, Assembly asm)
        {
            ProbeLog.Mark("[H] ===== H 组探针：悬停真渲染源取证 挂载 =====");

            _statType = Reflect.FindType(asm, "TemplateHUDStat");
            _dataType = Reflect.FindType(asm, "HUDStatData");
            _modType = Reflect.FindType(asm, "HUDPeriodicStatModifierData");
            _hudPanelType = Reflect.FindType(asm, "HUDPanel");
            _panelDataType = Reflect.FindType(asm, "HUDPanelData");

            ResolveUnityTypes();

            
            try
            {
                var gate = new List<string>();
                int ok = SafeResolve.RunGate(
                    "H-TYPE 启动安全闸（0.3.4）：H 组所需程序集/类型 + il2cpp 原生注册情况 + 本次不可用清单",
                    TypeRows, gate.Add, new[] { "UnityEngine.Component", "TemplateHUDStat" });
                ProbeLog.Raw(string.Join(Environment.NewLine, gate.ToArray()));
                ProbeLog.Mark("[H] " + SafeResolve.OneLineSummary(ok, TypeRows.Length));
            }
            catch (Exception e) { ProbeLog.Error("[H] 安全闸异常（已吞掉）：" + e.GetType().Name + " : " + e.Message); }

            ProbeLog.Mark("[H][type] TemplateHUDStat=" + Yn(_statType)
                + " HUDStatData=" + Yn(_dataType)
                + " HUDPeriodicStatModifierData=" + Yn(_modType)
                + " HUDPanel=" + Yn(_hudPanelType)
                + " | UnityEngine.Component=" + Yn(_componentType)
                + " EventTrigger=" + Yn(_eventTriggerType)
                + " Button=" + Yn(_buttonType)
                + " TMP=" + Yn(_tmpType)
                + " Canvas=" + Yn(_canvasType)
                + " CanvasGroup=" + Yn(_canvasGroupType)
                + " | Il2CppInterop.Il2CppType.From=" + (Il2CppTypeBridgeAvailable() ? "✓" : "✗"));
            ProbeLog.Mark("[H][type] 降级说明：标 ✗ 的项 = 类型名不可解析（IL2CPP 未注册 / 本次未加载），"
                + "探针**不会**去 Load 它 ✗ —— 只是这部分字段不采（原生 il2cpp 通道仍可兜底读到外观字段）。");

            if (_statType == null) { ProbeLog.Error("[H] 无 TemplateHUDStat，探针退出"); return; }

            
            PatchPost(harmony, MethodOf(_statType, "Setup", _dataType), "Post_SetupData",
                "TemplateHUDStat.Setup(HUDStatData)");
            
            PatchPost(harmony, MethodOfEmpty(_statType, "Setup"), "Post_SetupNoArg",
                "TemplateHUDStat.Setup()");
            PatchPost(harmony, MethodOfEmpty(_statType, "SetLocalizableTexts"), "Post_SetLocalizableTexts",
                "TemplateHUDStat.SetLocalizableTexts()");
            PatchPostBus(harmony, MethodOf(_statType, "SetupStatText", typeof(int)), "Post_SetupStatText",
                "TemplateHUDStat.SetupStatText(Int32)");
            PatchPost(harmony, MethodOfEmpty(_statType, "GetStatTooltipText"), "Post_GetStatTooltipText",
                "TemplateHUDStat.GetStatTooltipText()");
            PatchPost(harmony, MethodOf(_statType, "GetTooltipModifierTextString", _dataType), "Post_GetTooltipModifier",
                "TemplateHUDStat.GetTooltipModifierTextString(HUDStatData)");
            PatchPostBus(harmony, MethodOf(_statType, "GetModifierText", _modType), "Post_GetModifierText",
                "TemplateHUDStat.GetModifierText(HUDPeriodicStatModifierData)");

            if (_hudPanelType != null)
            {
                PatchPost(harmony, MethodOfEmpty(_hudPanelType, "Show"), "Post_HudPanelShow",
                    "HUDPanel.Show()", warnOnly: true);
                PatchPostBus(harmony, MethodOf(_hudPanelType, "Setup", _panelDataType), "Post_HudPanelSetup",
                    "HUDPanel.Setup(HUDPanelData)", warnOnly: true);
            }

            
            InstallTmpSetterHook(harmony);

            ProbeLog.Mark("[H] ===== H 组探针挂载完成（dump 将在捕获到 Sordland_HUDStat_GovernmentBudget 时触发一次）=====");
        }

        private static void ResolveUnityTypes()
        {
            _componentType = FindInAssemblies("UnityEngine.Component",
                "UnityEngine.CoreModule", "UnityEngine");
            _eventTriggerType = FindInAssemblies("UnityEngine.EventSystems.EventTrigger",
                "UnityEngine.UI", "UnityEngine.CoreModule", "UnityEngine");
            _buttonType = FindInAssemblies("UnityEngine.UI.Button", "UnityEngine.UI", "UnityEngine");
            _selectableType = FindInAssemblies("UnityEngine.UI.Selectable", "UnityEngine.UI", "UnityEngine");
            _tmpType = FindInAssemblies("TMPro.TextMeshProUGUI", "Unity.TextMeshPro", "UnityEngine.TextMeshPro")
                       ?? FindInAssemblies("TMPro.TMP_Text", "Unity.TextMeshPro", "UnityEngine.TextMeshPro");
            
            
            
            
            _canvasGroupType = FindInAssemblies("UnityEngine.CanvasGroup",
                "UnityEngine.UIModule", "UnityEngine.CoreModule", "UnityEngine", "UnityEngine.UI");
            _rectMaskType = FindInAssemblies("UnityEngine.UI.RectMask2D", "UnityEngine.UI");
            _maskType = FindInAssemblies("UnityEngine.UI.Mask", "UnityEngine.UI");
            _canvasType = FindInAssemblies("UnityEngine.Canvas",
                "UnityEngine.UIModule", "UnityEngine.CoreModule", "UnityEngine");
        }

        
        
        
        
        
        
        
        
        
        
        private static Type FindInAssemblies(string typeName, params string[] asmNames)
        {
            return SafeResolve.FindTypePreferred(typeName, asmNames);
        }

        private static string Yn(object o) { return o == null ? "✗" : "✓"; }

        private static MethodInfo MethodOf(Type t, string name, params Type[] args)
        {
            if (t == null || args == null || args.Length == 0) return null;
            foreach (var a in args) if (a == null) return null;
            try { return AccessTools.Method(t, name, args); } catch { return null; }
        }

        private static MethodInfo MethodOfEmpty(Type t, string name)
        {
            if (t == null) return null;
            try
            {
                return t.GetMethod(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance,
                    null, Type.EmptyTypes, null);
            }
            catch { return null; }
        }

        private static void PatchPost(Harmony harmony, MethodInfo target, string postfixName, string label,
            bool warnOnly = false)
        {
            if (target == null)
            {
                ProbeLog.Mark("[H][mount] ✗ " + label + " —— 方法不存在（该行本身即结论：这条路走不通）");
                return;
            }
            
            var refusal = SafeResolve.PatchRefusal(target);
            if (refusal != null)
            {
                ProbeLog.Line("[H][mount] · " + label + " 未挂（体检）：" + refusal);
                return;
            }
            try
            {
                harmony.Patch(target,
                    postfix: new HarmonyMethod(typeof(HoverProbe).GetMethod(postfixName, All)));
                ProbeLog.Mark("[H][mount] ✓ " + label);
            }
            catch (Exception e)
            {
                if (warnOnly) ProbeLog.Line("[H][mount] ✗ " + label + " 挂载失败：" + e.GetType().Name + " : " + e.Message);
                else ProbeLog.Error("[H][mount] ✗ " + label + " 挂载失败：" + e.GetType().Name + " : " + e.Message);
            }
        }

        private static void PatchPostBus(Harmony harmony, MethodInfo target, string postfixName, string label,
            bool warnOnly = false)
        {
            if (target == null)
            {
                ProbeLog.Mark("[H][mount] ✗ " + label + " —— 方法不存在（该行本身即结论：这条路走不通）");
                return;
            }
            var refusal = SafeResolve.PatchRefusal(target);
            if (refusal != null)
            {
                ProbeLog.Line("[H][mount] · " + label + " 未挂（体检）：" + refusal);
                return;
            }
            try
            {
                Action<PatchContext> cb;
                if (postfixName == "Post_SetupStatText")
                    cb = ctx =>
                    {
                        object arg0 = ctx.Args != null && ctx.Args.Length > 0 ? ctx.Args[0] : null;
                        Post_SetupStatText(ctx.Instance, arg0);
                    };
                else if (postfixName == "Post_GetModifierText")
                    cb = ctx =>
                    {
                        string result = ctx.Result as string ?? "";
                        Post_GetModifierText(ctx.Instance, ref result);
                    };
                else if (postfixName == "Post_HudPanelSetup")
                    cb = ctx => Post_HudPanelSetup(ctx.Instance);
                else
                {
                    ProbeLog.Line("[H][mount] ✗ " + label + " 未挂：未知 PatchBus 回调 " + postfixName);
                    return;
                }
                PatchBus.SubscribePostfix(harmony, "OI.Probe.H." + postfixName,
                    target, PatchScope.Any, 50, cb);
                ProbeLog.Mark("[H][mount] ✓ " + label + "（PatchBus）");
            }
            catch (Exception e)
            {
                if (warnOnly) ProbeLog.Line("[H][mount] ✗ " + label + " 挂载失败：" + e.GetType().Name + " : " + e.Message);
                else ProbeLog.Error("[H][mount] ✗ " + label + " 挂载失败：" + e.GetType().Name + " : " + e.Message);
            }
        }

        
        
        
        
        
        
        
        
        
        private static void InstallTmpSetterHook(Harmony harmony)
        {
            try
            {
                if (_tmpType == null)
                {
                    ProbeLog.Error("[H][mount] ✗ 未找到 TMPro.TextMeshProUGUI interop 类型 —— 重写取证（set_text）不可用");
                    return;
                }

                MethodInfo setter = null;
                foreach (var m in _tmpType.GetMethods(All))
                {
                    if (m.Name != "set_text") continue;
                    var ps = m.GetParameters();
                    if (ps.Length == 1 && ps[0].ParameterType == typeof(string)) { setter = m; break; }
                }
                if (setter == null)
                {
                    var p = _tmpType.GetProperty("text", All);
                    if (p != null) setter = p.GetSetMethod(true);
                }
                if (setter == null)
                {
                    ProbeLog.Error("[H][mount] ✗ TMPro.set_text 找不到（属性可能被 interop 改写）—— 重写取证不可用");
                    return;
                }

                
                
                
                
                
                
                var refusal = SafeResolve.PatchRefusal(setter);
                if (refusal != null)
                {
                    ProbeLog.Line("[H][mount] · set_text 预期降级（⑦ 重写取证关闭）：" + refusal);
                    return;
                }

                harmony.Patch(setter, postfix: new HarmonyMethod(typeof(HoverProbe).GetMethod("Post_TmpSetText", All)));
                ProbeLog.Mark("[H][mount] ✓ " + setter.DeclaringType.Name + ".set_text"
                    + "（重写取证：谁把 tooltip 文本改掉、改成多长、调用栈）");
            }
            catch (Exception e)
            {
                ProbeLog.Error("[H][mount] ✗ TMP set_text 挂载失败：" + e.GetType().Name + " : " + e.Message);
            }
        }

        
        
        

        private static void Post_TmpSetText(object __instance)
        {
            try
            {
                if (__instance == null) return;

                string tag = null;
                if (_mainTmp != null && ReferenceEquals(__instance, _mainTmp)) tag = "主 statTooltipText";
                else if (_backupTmp != null && ReferenceEquals(__instance, _backupTmp)) tag = "备 statTooltipModifierText";
                if (tag == null) return;

                _tmpSetSeen++;
                
                
                if (_traceCount >= TraceCap) return;
                _traceCount++;

                var now = Reflect.Str(Reflect.Prop(__instance, "text"));
                ProbeLog.Mark("[H-SET] #" + _tmpSetSeen + " " + tag + " 被赋值 → 长度="
                    + (now == null ? -1 : now.Length)
                    + " 含我方标记=" + Yn(ContainsMarker(now))
                    + " 内容=" + Esc(Trunc(now, 100))
                    + " ｜ 调用栈：" + Stack(5));
            }
            catch { }
        }

        
        private static string Stack(int n)
        {
            try
            {
                var st = new System.Diagnostics.StackTrace(2, false);
                var sb = new StringBuilder();
                int c = 0;
                for (int i = 0; i < st.FrameCount && c < n; i++)
                {
                    var f = st.GetFrame(i);
                    var m = f == null ? null : f.GetMethod();
                    if (m == null) continue;
                    var dt = m.DeclaringType;
                    if (dt != null && dt.Name == "HoverProbe") continue;
                    sb.Append(dt == null ? "?" : dt.Name).Append('.').Append(m.Name);
                    if (++c < n) sb.Append(" < ");
                }
                return sb.Length == 0 ? "(无托管帧 —— 调用方在 IL2CPP 原生侧)" : sb.ToString();
            }
            catch (Exception e) { return "(栈不可读:" + e.GetType().Name + ")"; }
        }

        
        
        

        private static void Post_SetupData(object __instance, object __0)
        {
            try
            {
                string name = NameOfData(__0);
                bool budget = IsBudget(__instance, name);
                if (!budget) return;

                if (!_capturedLogged)
                {
                    _capturedLogged = true;
                    ProbeLog.Mark("[H] 已捕获预算统计实例（dump 触发点）：NameInDatabase=" + D(name)
                        + " 实例可用=" + Yn(Reflect.Usable(__instance)));
                }

                CaptureSlots(__instance);
                TryDump(__instance, "Setup(HUDStatData)");
            }
            catch { }
        }

        private static void Post_SetupNoArg(object __instance)
        {
            try
            {
                if (!IsBudget(__instance, null)) return;
                TraceProducer("Setup()", "（无返回值）");
            }
            catch { }
        }

        private static void Post_SetLocalizableTexts(object __instance)
        {
            try
            {
                if (!IsBudget(__instance, null)) return;
                TraceProducer("SetLocalizableTexts()", "（无返回值）");
            }
            catch { }
        }

        private static void Post_SetupStatText(object __instance, object __0)
        {
            try
            {
                if (!IsBudget(__instance, null)) return;
                TraceProducer("SetupStatText(" + Reflect.Str(__0) + ")", "（无返回值）");
            }
            catch { }
        }

        private static void Post_GetStatTooltipText(object __instance, ref string __result)
        {
            try
            {
                if (!IsBudget(__instance, null)) return;
                var s = __result;
                TraceProducer("GetStatTooltipText()", Describe(s));
                if (WantStamp("GetStatTooltipText") && s != null && s.IndexOf(StampTag, StringComparison.Ordinal) < 0)
                    __result = s + StampTag;
            }
            catch { }
        }

        private static void Post_GetTooltipModifier(object __instance, object __0, ref string __result)
        {
            try
            {
                if (!IsBudget(__instance, null)) return;
                TraceProducer("GetTooltipModifierTextString(" + D(NameOfData(__0)) + ")", Describe(__result));
                if (WantStamp("GetTooltipModifierTextString") && __result != null
                    && __result.IndexOf(StampTag, StringComparison.Ordinal) < 0)
                    __result = __result + StampTag;
            }
            catch { }
        }

        private static void Post_GetModifierText(object __instance, ref string __result)
        {
            try
            {
                if (!IsBudget(__instance, null)) return;
                TraceProducer("GetModifierText()", Describe(__result));
            }
            catch { }
        }

        private static void Post_HudPanelShow(object __instance)
        {
            try
            {
                if (!Reflect.Usable(__instance)) return;
                TraceProducer("HUDPanel.Show()", "面板=" + Esc(GoName(Reflect.Prop(__instance, "gameObject"))));
            }
            catch { }
        }

        private static void Post_HudPanelSetup(object __instance)
        {
            try
            {
                if (!Reflect.Usable(__instance)) return;
                TraceProducer("HUDPanel.Setup(HUDPanelData)", "面板=" + Esc(GoName(Reflect.Prop(__instance, "gameObject"))));
            }
            catch { }
        }

        
        private static void TraceProducer(string who, string what)
        {
            try
            {
                if (_traceCount >= TraceCap) return;
                _traceCount++;
                ProbeLog.Mark("[H-CALL] " + who + " ← 被调用 返回值=" + what
                    + " ｜ 当前主槽长度=" + CurLen(_mainTmp)
                    + " 主槽含我方标记=" + Yn(ContainsMarker(Reflect.Str(Reflect.Prop(_mainTmp, "text")))));
            }
            catch { }
        }

        private static bool WantStamp(string producer)
        {
            var s = ProbeLog.Stamp;
            return !string.IsNullOrEmpty(s) && string.Equals(s, producer, StringComparison.OrdinalIgnoreCase);
        }

        private static int CurLen(object tmp)
        {
            try
            {
                var t = Reflect.Str(Reflect.Prop(tmp, "text"));
                return t == null ? -1 : t.Length;
            }
            catch { return -1; }
        }

        private static void CaptureSlots(object stat)
        {
            try
            {
                _budgetStat = stat;
                if (_mainTmp == null) _mainTmp = Reflect.Prop(stat, "statTooltipText");
                if (_backupTmp == null) _backupTmp = Reflect.Prop(stat, "statTooltipModifierText");
            }
            catch { }
        }

        private static bool ContainsMarker(string s)
        {
            return s != null && s.IndexOf(MarkerToken, StringComparison.Ordinal) >= 0;
        }

        
        
        

        private static bool IsBudget(object stat, string dataName)
        {
            try
            {
                if (stat != null && _budgetStat != null && ReferenceEquals(stat, _budgetStat)) return true;
                if (dataName != null && dataName.IndexOf("GovernmentBudget", StringComparison.OrdinalIgnoreCase) >= 0)
                    return true;
                var n = NameOfData(Reflect.Prop(stat, "currentHUDStatData"));
                return n != null && n.IndexOf("GovernmentBudget", StringComparison.OrdinalIgnoreCase) >= 0;
            }
            catch { return false; }
        }

        private static string NameOfData(object data)
        {
            try
            {
                if (!Reflect.Usable(data)) return null;
                var n = Reflect.Str(Reflect.Prop(data, "NameInDatabase"));
                if (!string.IsNullOrEmpty(n)) return n;
                return Reflect.Str(Reflect.Prop(data, "name"));
            }
            catch { return null; }
        }

        
        
        

        private static void TryDump(object stat, string why)
        {
            if (_dumped) return;
            _dumped = true;                 

            try
            {
                var go = Reflect.Prop(stat, "gameObject");
                var tr = Reflect.Prop(stat, "transform");

                ProbeLog.Raw("");
                ProbeLog.Raw("################################################################");
                ProbeLog.Raw("# [H][DUMP] 悬停真渲染源 —— 一次性取证（触发=" + why + "）");
                ProbeLog.Raw("#   时刻 = " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture));
                ProbeLog.Raw("#   统计 = " + D(NameOfData(Reflect.Prop(stat, "currentHUDStatData"))));
                ProbeLog.Raw("#   说明 = 本 dump 只在捕获到预算统计实例时打一次；之后只保留有上限的 [H-CALL]/[H-SET] 追踪。");
                ProbeLog.Raw("################################################################");

                
                ProbeLog.Raw("");
                ProbeLog.Raw("== ① GameObject 层级路径（本物体 → 根，每级附 activeSelf）==");
                DumpPath(tr);

                ProbeLog.Raw("");
                ProbeLog.Raw("== ① 同物体全部组件（" + GoName(go) + "）==");
                var comps = ComponentsOf(go);
                if (comps == null) ProbeLog.Raw("   （组件枚举不可用 —— UnityEngine.Component 类型未解析到；下面各项仍照常取证）");
                else
                {
                    for (int i = 0; i < comps.Count; i++)
                    {
                        var c = comps[i];
                        if (c == null) continue;
                        ProbeLog.Raw("   [" + i + "] " + c.GetType().FullName
                            + "  enabled=" + S(Reflect.Prop(c, "enabled"))
                            + "  " + MaybeTooltipish(c.GetType()));
                    }
                }

                
                ProbeLog.Raw("");
                ProbeLog.Raw("== ② 组件里的字符串 / TextMeshPro 字段（名字 + 当前值，截断 200 字符）==");
                if (comps != null)
                {
                    for (int i = 0; i < comps.Count; i++) DumpComponentText(comps[i]);
                }
                DumpComponentText(stat);      

                
                ProbeLog.Raw("");
                ProbeLog.Raw("== ⑥ 两个 tooltip TMP 的可见性 / 几何（『写进去了却看不见』的直接判据）==");
                DumpTmpState("主 statTooltipText", Reflect.Prop(stat, "statTooltipText"));
                DumpTmpState("备 statTooltipModifierText", Reflect.Prop(stat, "statTooltipModifierText"));
                ProbeLog.Raw("   hideModifier = " + S(Reflect.Prop(stat, "hideModifier"))
                    + "   currentValue = " + S(Reflect.Prop(stat, "currentValue")));

                
                DumpCarrierContainer("主 statTooltipText", Reflect.Prop(stat, "statTooltipText"));

                
                ProbeLog.Raw("");
                ProbeLog.Raw("== ⑤ 悬停事件链（本物体 + 祖先上的 EventTrigger / Button / Selectable / IPointerEnter 实现）==");
                DumpHoverChain(tr, stat);

                
                ProbeLog.Raw("");
                ProbeLog.Raw("== ③/④ 场景扫描：可疑 tooltip/hover 组件 + 全局 tooltip 面板 ==");
                DumpSceneScan(stat);

                ProbeLog.Raw("");
                ProbeLog.Raw("# [H][DUMP] 取证结束。下一步判读见 docs/p0.2-probe-report.md 的 H 组说明：");
                ProbeLog.Raw("#   ① 看 [H-CALL] 里悬停瞬间冒出哪一行 → 那就是重写点；");
                ProbeLog.Raw("#   ② 看 [H-SET] 有没有把主槽改回 51 字符、调用栈是谁；");
                ProbeLog.Raw("#   ③ 若两者都不够 → 把 oi-probe.json 的 stamp 设成候选生产者名，悬停看标记。");
                ProbeLog.Raw("################################################################");
                ProbeLog.Raw("");
            }
            catch (Exception e)
            {
                ProbeLog.Error("[H][DUMP] 异常中断：" + e.GetType().Name + " : " + e.Message);
            }
        }

        private static void DumpPath(object tr)
        {
            try
            {
                var chain = new List<string>();
                var cur = tr;
                for (int depth = 0; depth < 12 && cur != null; depth++)
                {
                    var go = Reflect.Prop(cur, "gameObject");
                    chain.Add(Esc(GoName(go)) + "[activeSelf=" + S(Reflect.Prop(go, "activeSelf"))
                        + ",sib=" + S(Reflect.Call0(cur, "GetSiblingIndex")) + "]");
                    cur = Reflect.Prop(cur, "parent");
                }
                chain.Reverse();
                ProbeLog.Raw("   " + string.Join(" / ", chain.ToArray()));
            }
            catch (Exception e) { ProbeLog.Raw("   （层级读取失败：" + e.GetType().Name + "）"); }
        }

        private static void DumpTmpState(string label, object tmp)
        {
            try
            {
                if (!Reflect.Usable(tmp))
                {
                    ProbeLog.Raw("   " + label + " = <null/不可用>  ← 槽位本身就不存在，任何写入都不可能被看见");
                    return;
                }
                var go = Reflect.Prop(tmp, "gameObject");
                var txt = Reflect.Str(Reflect.Prop(tmp, "text"));
                var sb = new StringBuilder();
                sb.Append("   ").Append(label).Append(" 类型=").Append(tmp.GetType().Name);
                sb.Append(" text长度=").Append(txt == null ? -1 : txt.Length);
                sb.Append(" 含我方标记=").Append(Yn(ContainsMarker(txt)));
                sb.Append(" | activeSelf=").Append(S(Reflect.Prop(go, "activeSelf")));
                sb.Append(" activeInHierarchy=").Append(S(Reflect.Prop(go, "activeInHierarchy")));
                sb.Append(" enabled=").Append(S(Reflect.Prop(tmp, "enabled")));
                sb.Append(" alpha=").Append(S(Reflect.Prop(tmp, "alpha")));
                sb.Append(" fontSize=").Append(S(Reflect.Prop(tmp, "fontSize")));
                sb.Append(" overflowMode=").Append(S(Reflect.Prop(tmp, "overflowMode")));
                sb.Append(" enableWordWrapping=").Append(S(Reflect.Prop(tmp, "enableWordWrapping")));
                sb.Append(" color=").Append(ColorStr(Reflect.Prop(tmp, "color")));
                ProbeLog.Raw(sb.ToString());
                ProbeLog.Raw("      内容=" + Esc(Trunc(txt, 200)));

                
                var rt = Reflect.Prop(tmp, "rectTransform") ?? Reflect.Prop(tmp, "transform");
                ProbeLog.Raw("      rect: sizeDelta=" + S(Reflect.Prop(rt, "sizeDelta"))
                    + " anchoredPosition=" + S(Reflect.Prop(rt, "anchoredPosition")));
                DumpAncestorMasking(Reflect.Prop(tmp, "transform"), 6);
            }
            catch (Exception e) { ProbeLog.Raw("   " + label + " 读取失败：" + e.GetType().Name + " : " + e.Message); }
        }

        
        private static void DumpAncestorMasking(object tr, int maxDepth)
        {
            try
            {
                var cur = Reflect.Prop(tr, "parent");
                for (int d = 0; d < maxDepth && cur != null; d++)
                {
                    var go = Reflect.Prop(cur, "gameObject");
                    var sb = new StringBuilder();
                    sb.Append("        祖先[").Append(d + 1).Append("] ").Append(Esc(GoName(go)));
                    var cg = GetComponent(go, _canvasGroupType);
                    if (Reflect.Usable(cg))
                        sb.Append(" CanvasGroup{alpha=").Append(S(Reflect.Prop(cg, "alpha")))
                          .Append(",interactable=").Append(S(Reflect.Prop(cg, "interactable")))
                          .Append(",blocksRaycasts=").Append(S(Reflect.Prop(cg, "blocksRaycasts"))).Append('}');
                    if (Reflect.Usable(GetComponent(go, _rectMaskType))) sb.Append(" +RectMask2D(裁剪)");
                    if (Reflect.Usable(GetComponent(go, _maskType))) sb.Append(" +Mask(裁剪)");
                    sb.Append(" activeSelf=").Append(S(Reflect.Prop(go, "activeSelf")));
                    sb.Append(" activeInHierarchy=").Append(S(Reflect.Prop(go, "activeInHierarchy")));
                    ProbeLog.Raw(sb.ToString());
                    cur = Reflect.Prop(cur, "parent");
                }
            }
            catch { }
        }

        private static void DumpComponentText(object comp)
        {
            if (comp == null || !Reflect.Usable(comp)) return;
            try
            {
                var t = comp.GetType();
                
                if (t.Name.IndexOf("TextMeshPro", StringComparison.OrdinalIgnoreCase) >= 0
                    || t.Name.IndexOf("TMP_Text", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    var txt = Reflect.Str(Reflect.Prop(comp, "text"));
                    ProbeLog.Raw("   TMP " + t.Name + ".text(len=" + (txt == null ? -1 : txt.Length)
                        + ") = " + Esc(Trunc(txt, 200)));
                }

                foreach (var f in t.GetFields(All))
                {
                    try
                    {
                        if (f.IsStatic) continue;
                        var ftn = f.FieldType.Name;
                        bool interesting = f.FieldType == typeof(string)
                                           || ftn.IndexOf("Text", StringComparison.OrdinalIgnoreCase) >= 0
                                           || f.Name.IndexOf("text", StringComparison.OrdinalIgnoreCase) >= 0;
                        if (!interesting) continue;
                        if (f.Name.StartsWith("Native", StringComparison.Ordinal)) continue;
                        var v = f.GetValue(comp);
                        ProbeLog.Raw("   " + t.Name + "." + f.Name + " (" + ftn + ") = " + Val(v)
                            + TmpTextOf(v));
                    }
                    catch (Exception e) { ProbeLog.Raw("   " + t.Name + "." + f.Name + " <读失败:" + e.GetType().Name + ">"); }
                }
            }
            catch { }
        }

        
        
        

        
        
        
        
        
        
        
        
        
        
        
        private static void DumpCarrierContainer(string label, object tmp)
        {
            ProbeLog.Raw("");
            ProbeLog.Raw("== ⑩ 承载容器：从「" + label + "」往上（内边距 / 背景 / 边框 / Canvas 层级）==");
            if (!Reflect.Usable(tmp))
            {
                ProbeLog.Raw("   （" + label + " 不可用 —— 没有实例就没有容器，本节跳过）");
                return;
            }

            
            try
            {
                var self = ComponentsOf(Reflect.Prop(tmp, "gameObject"));
                if (self != null)
                    ProbeLog.Raw("   [自检] TMP 本体的组件枚举 = " + self.Count + " 个 ✓（0.3.0 这里恒为「不可用」）");
                else
                    ProbeLog.Raw("   [自检] ✗ 组件枚举仍不可用 —— 四条路径全失败（把这一行连同 oi-probe.json 一起回报）");
            }
            catch { }

            var cur = Reflect.Prop(tmp, "transform");
            for (int d = 0; d < 6 && cur != null; d++)
            {
                try
                {
                    var go = Reflect.Prop(cur, "gameObject");
                    ProbeLog.Raw("");
                    ProbeLog.Raw("   ── 层[" + d + "] " + Esc(GoName(go))
                        + "  activeSelf=" + S(Reflect.Prop(go, "activeSelf"))
                        + " activeInHierarchy=" + S(Reflect.Prop(go, "activeInHierarchy")));
                    ProbeLog.Raw("      RectTransform: sizeDelta=" + S(Reflect.Prop(cur, "sizeDelta"))
                        + " anchorMin=" + S(Reflect.Prop(cur, "anchorMin"))
                        + " anchorMax=" + S(Reflect.Prop(cur, "anchorMax"))
                        + " pivot=" + S(Reflect.Prop(cur, "pivot"))
                        + " anchoredPosition=" + S(Reflect.Prop(cur, "anchoredPosition"))
                        + " rect=" + S(Reflect.Prop(cur, "rect")));

                    var cs = ComponentsOf(go);
                    if (cs == null)
                    {
                        ProbeLog.Raw("      组件=[枚举不可用]");
                    }
                    else
                    {
                        var names = new StringBuilder();
                        for (int i = 0; i < cs.Count; i++)
                        {
                            if (i > 0) names.Append(" + ");
                            names.Append(cs[i] == null ? "<null>" : cs[i].GetType().Name);
                        }
                        ProbeLog.Raw("      组件[" + cs.Count + "] = " + names);
                        for (int i = 0; i < cs.Count; i++) DumpComponentDetail(cs[i], "        ");
                    }
                }
                catch (Exception e)
                {
                    ProbeLog.Raw("      层[" + d + "] 读取失败：" + e.GetType().Name + " : " + e.Message);
                }
                cur = Reflect.Prop(cur, "parent");
            }

            
            try
            {
                var c = Reflect.Prop(tmp, "transform");
                for (int d = 0; d < 10 && c != null; d++)
                {
                    var cgo = Reflect.Prop(c, "gameObject");
                    var cv = GetComponent(cgo, _canvasType);
                    if (Reflect.Usable(cv))
                    {
                        ProbeLog.Raw("");
                        ProbeLog.Raw("   ── Canvas（第一个祖先 Canvas，层深 " + d + "）：" + Esc(GoName(cgo)));
                        ProbeLog.Raw("      sortingOrder=" + S(Reflect.Prop(cv, "sortingOrder"))
                            + " overrideSorting=" + S(Reflect.Prop(cv, "overrideSorting"))
                            + " renderMode=" + S(Reflect.Prop(cv, "renderMode"))
                            + " sortingLayerName=" + S(Reflect.Prop(cv, "sortingLayerName"))
                            + " scaleFactor=" + S(Reflect.Prop(cv, "scaleFactor")));
                        break;
                    }
                    c = Reflect.Prop(c, "parent");
                }
            }
            catch { }
        }

        
        
        
        
        
        private static void DumpComponentDetail(object comp, string indent)
        {
            if (comp == null || !Reflect.Usable(comp)) return;
            try
            {
                var t = comp.GetType();
                var n = t.Name;
                var sb = new StringBuilder();
                sb.Append(indent).Append("- ").Append(n)
                  .Append(" enabled=").Append(S(Reflect.Prop(comp, "enabled")));
                bool any = false;

                
                var color = Reflect.Prop(comp, "color");
                if (color != null)
                {
                    sb.Append(" color=").Append(ColorStr(color));
                    sb.Append(" raycastTarget=").Append(S(Reflect.Prop(comp, "raycastTarget")));
                    any = true;
                }

                
                if (n.IndexOf("Image", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    var sprite = Reflect.Prop(comp, "sprite");
                    sb.Append(" sprite=").Append(!Reflect.Usable(sprite)
                        ? "<无>"
                        : Esc(Reflect.Str(Reflect.Prop(sprite, "name"))));
                    sb.Append(" type=").Append(S(Reflect.Prop(comp, "type")));
                    sb.Append(" hasBorder=").Append(S(Reflect.Prop(comp, "hasBorder")));
                    sb.Append(" preserveAspect=").Append(S(Reflect.Prop(comp, "preserveAspect")));
                    sb.Append(" fillCenter=").Append(S(Reflect.Prop(comp, "fillCenter")));
                    var border = Reflect.Prop(sprite, "border");
                    if (border != null)
                        sb.Append(" border{l=").Append(S(Reflect.Prop(border, "left")))
                          .Append(",r=").Append(S(Reflect.Prop(border, "right")))
                          .Append(",t=").Append(S(Reflect.Prop(border, "top")))
                          .Append(",b=").Append(S(Reflect.Prop(border, "bottom"))).Append('}');
                    any = true;
                }

                
                var padding = Reflect.Prop(comp, "padding");
                if (padding != null)
                {
                    sb.Append(" padding{l=").Append(S(Reflect.Prop(padding, "left")))
                      .Append(",r=").Append(S(Reflect.Prop(padding, "right")))
                      .Append(",t=").Append(S(Reflect.Prop(padding, "top")))
                      .Append(",b=").Append(S(Reflect.Prop(padding, "bottom"))).Append('}');
                    any = true;
                }
                if (n.IndexOf("LayoutGroup", StringComparison.OrdinalIgnoreCase) >= 0
                    || n.IndexOf("LayoutElement", StringComparison.OrdinalIgnoreCase) >= 0
                    || n.IndexOf("ContentSizeFitter", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    sb.Append(" spacing=").Append(S(Reflect.Prop(comp, "spacing")));
                    sb.Append(" childAlignment=").Append(S(Reflect.Prop(comp, "childAlignment")));
                    sb.Append(" childControlW/H=").Append(S(Reflect.Prop(comp, "childControlWidth")))
                      .Append('/').Append(S(Reflect.Prop(comp, "childControlHeight")));
                    sb.Append(" childForceExpandW/H=").Append(S(Reflect.Prop(comp, "childForceExpandWidth")))
                      .Append('/').Append(S(Reflect.Prop(comp, "childForceExpandHeight")));
                    sb.Append(" ignoreLayout=").Append(S(Reflect.Prop(comp, "ignoreLayout")));
                    sb.Append(" fitH/V=").Append(S(Reflect.Prop(comp, "horizontalFit")))
                      .Append('/').Append(S(Reflect.Prop(comp, "verticalFit")));
                    sb.Append(" minW/H=").Append(S(Reflect.Prop(comp, "minWidth")))
                      .Append('/').Append(S(Reflect.Prop(comp, "minHeight")));
                    sb.Append(" prefW/H=").Append(S(Reflect.Prop(comp, "preferredWidth")))
                      .Append('/').Append(S(Reflect.Prop(comp, "preferredHeight")));
                    any = true;
                }

                
                if (n.IndexOf("CanvasGroup", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    sb.Append(" alpha=").Append(S(Reflect.Prop(comp, "alpha")))
                      .Append(" interactable=").Append(S(Reflect.Prop(comp, "interactable")))
                      .Append(" blocksRaycasts=").Append(S(Reflect.Prop(comp, "blocksRaycasts")));
                    any = true;
                }

                
                if (string.Equals(n, "Canvas", StringComparison.Ordinal)
                    || n.IndexOf("CanvasScaler", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    sb.Append(" sortingOrder=").Append(S(Reflect.Prop(comp, "sortingOrder")))
                      .Append(" overrideSorting=").Append(S(Reflect.Prop(comp, "overrideSorting")))
                      .Append(" renderMode=").Append(S(Reflect.Prop(comp, "renderMode")))
                      .Append(" referenceResolution=").Append(S(Reflect.Prop(comp, "referenceResolution")))
                      .Append(" scaleFactor=").Append(S(Reflect.Prop(comp, "scaleFactor")));
                    any = true;
                }

                
                if (n.IndexOf("TextMeshPro", StringComparison.OrdinalIgnoreCase) >= 0
                    || n.IndexOf("TMP_Text", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    var txt = Reflect.Str(Reflect.Prop(comp, "text"));
                    sb.Append(" text长度=").Append(txt == null ? -1 : txt.Length);
                    sb.Append(" fontSize=").Append(S(Reflect.Prop(comp, "fontSize")));
                    sb.Append(" enableWordWrapping=").Append(S(Reflect.Prop(comp, "enableWordWrapping")));
                    sb.Append(" overflowMode=").Append(S(Reflect.Prop(comp, "overflowMode")));
                    sb.Append(" margin=").Append(S(Reflect.Prop(comp, "margin")));
                    any = true;
                }

                if (!any) sb.Append(" （无外观字段：结构性组件）");
                ProbeLog.Raw(sb.ToString());
            }
            catch (Exception e)
            {
                ProbeLog.Raw(indent + "- <组件读取失败:" + e.GetType().Name + ">");
            }
        }

        
        
        

        private static void DumpHoverChain(object tr, object stat)
        {
            try
            {
                
                var cur = tr;
                for (int d = 0; d < 8 && cur != null; d++)
                {
                    var go = Reflect.Prop(cur, "gameObject");
                    string gname = Esc(GoName(go));
                    bool any = false;

                    var et = GetComponent(go, _eventTriggerType);
                    if (Reflect.Usable(et)) { any = true; DumpEventTrigger("EventTrigger@" + gname, et); }

                    var btn = GetComponent(go, _buttonType);
                    if (Reflect.Usable(btn))
                    {
                        any = true;
                        DumpUnityEvent("Button.onClick@" + gname, Reflect.Prop(btn, "onClick"));
                        DumpPointerInterfaces(btn.GetType(), "Button@" + gname);
                    }

                    var sel = GetComponent(go, _selectableType);
                    if (Reflect.Usable(sel) && !Reflect.Usable(btn))
                    {
                        any = true;
                        ProbeLog.Raw("   Selectable@" + gname + " 类型=" + sel.GetType().Name);
                        DumpPointerInterfaces(sel.GetType(), "Selectable@" + gname);
                    }

                    if (any) ProbeLog.Raw("   ↑ 层级深度 " + d + "（0 = 统计物体本身）");

                    
                    var comps = ComponentsOf(go);
                    if (comps != null)
                    {
                        for (int i = 0; i < comps.Count; i++)
                        {
                            var c = comps[i];
                            if (c == null) continue;
                            DumpPointerInterfaces(c.GetType(), c.GetType().Name + "@" + gname);
                        }
                    }

                    cur = Reflect.Prop(cur, "parent");
                }

                
                var ownBtn = Reflect.Prop(stat, "button");
                if (Reflect.Usable(ownBtn))
                {
                    ProbeLog.Raw("   TemplateHUDStat.button = " + ownBtn.GetType().Name
                        + " on " + Esc(GoName(Reflect.Prop(ownBtn, "gameObject"))));
                    DumpUnityEvent("TemplateHUDStat.button.onClick", Reflect.Prop(ownBtn, "onClick"));
                }
                else ProbeLog.Raw("   TemplateHUDStat.button = <null>（该统计物体上没有 Button —— 悬停不由 Button 驱动）");
            }
            catch (Exception e) { ProbeLog.Raw("   （事件链读取失败：" + e.GetType().Name + " : " + e.Message + "）"); }
        }

        private static void DumpPointerInterfaces(Type t, string label)
        {
            try
            {
                if (t == null) return;
                foreach (var i in t.GetInterfaces())
                {
                    var n = i.Name;
                    if (n.IndexOf("Pointer", StringComparison.OrdinalIgnoreCase) < 0
                        && n.IndexOf("Hover", StringComparison.OrdinalIgnoreCase) < 0
                        && n.IndexOf("Select", StringComparison.OrdinalIgnoreCase) < 0) continue;
                    var ms = new StringBuilder();
                    foreach (var m in t.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
                    {
                        if (m.DeclaringType == null || m.DeclaringType != t) continue;
                        if (m.Name.IndexOf("Pointer", StringComparison.OrdinalIgnoreCase) < 0
                            && m.Name.IndexOf("Hover", StringComparison.OrdinalIgnoreCase) < 0
                            && m.Name.IndexOf("Select", StringComparison.OrdinalIgnoreCase) < 0) continue;
                        ms.Append(m.Name).Append(' ');
                    }
                    ProbeLog.Raw("   ★ " + label + " 实现 " + i.FullName
                        + (ms.Length > 0 ? " ｜ 本类方法：" + ms : " ｜ （方法定义在基类）"));
                }
            }
            catch { }
        }

        private static void DumpEventTrigger(string label, object et)
        {
            try
            {
                var triggers = Reflect.Prop(et, "triggers");
                int n = Reflect.ListCount(triggers);
                ProbeLog.Raw("   " + label + " 类型=" + et.GetType().Name + " 触发器数=" + n);
                for (int i = 0; i < n && i < 12; i++)
                {
                    var e = Reflect.Index(triggers, i);
                    if (e == null) continue;
                    ProbeLog.Raw("      [" + i + "] eventID=" + S(Reflect.Prop(e, "eventID"))
                        + " → " + DescribeUnityEvent(Reflect.Prop(e, "callback")));
                }
            }
            catch (Exception ex) { ProbeLog.Raw("   " + label + " 读取失败：" + ex.GetType().Name); }
        }

        private static void DumpUnityEvent(string label, object ev)
        {
            try
            {
                if (!Reflect.Usable(ev)) { ProbeLog.Raw("   " + label + " = <null/无>"); return; }
                ProbeLog.Raw("   " + label + " → " + DescribeUnityEvent(ev));
            }
            catch { }
        }

        
        
        
        
        private static string DescribeUnityEvent(object ev)
        {
            try
            {
                if (!Reflect.Usable(ev)) return "<无>";
                int n = Reflect.ToInt(Reflect.Call0(ev, "GetPersistentEventCount"));
                if (n <= 0) return "持久化回调 0 个";
                var sb = new StringBuilder();
                sb.Append("持久化回调 ").Append(n).Append(" 个：");
                for (int i = 0; i < n && i < 8; i++)
                {
                    var tgt = Reflect.Call1(ev, "GetPersistentTarget", i);
                    var meth = Reflect.Str(Reflect.Call1(ev, "GetPersistentMethodName", i));
                    sb.Append(" [").Append(i).Append("] ")
                      .Append(tgt == null ? "<null目标>" : tgt.GetType().Name + "(" + GoName(Reflect.Prop(tgt, "gameObject")) + ")")
                      .Append('.').Append(meth ?? "?");
                }
                return sb.ToString();
            }
            catch (Exception e) { return "<UnityEvent 读取失败:" + e.GetType().Name + ">"; }
        }

        
        
        

        private static void DumpSceneScan(object stat)
        {
            try
            {
                var tr = Reflect.Prop(stat, "transform");
                var root = Reflect.Call0(tr, "GetRoot") ?? Reflect.Prop(tr, "root");
                if (!Reflect.Usable(root))
                {
                    ProbeLog.Raw("   （拿不到 transform.root —— 跳过层级扫描，仅扫统计物体自身）");
                    root = tr;
                }
                ProbeLog.Raw("   扫描范围 = \"（根）" + Esc(GoName(Reflect.Prop(root, "gameObject")))
                    + "\" 子树（深度 ≤ 9、节点 ≤ 700，只对**名字命中**的节点展开明细）");

                var hits = new List<string>();
                int visited = 0;
                Walk(Reflect.Prop(root, "gameObject"), "", 0, 9, ref visited, hits, 700);

                ProbeLog.Raw("   访问节点 " + visited + " 个，名字命中 tooltip/hover/hint/tip 的 " + hits.Count + " 个：");
                for (int i = 0; i < hits.Count && i < 60; i++) ProbeLog.Raw("      " + hits[i]);
                if (hits.Count == 0)
                    ProbeLog.Raw("      （无命中 —— HUD 层级里没有名字含 tooltip 的物体；"
                        + "说明 tooltip 文本就挂在统计物体自己的子物体上，见 ⑥ 的祖先链）");
            }
            catch (Exception e) { ProbeLog.Raw("   （场景扫描失败：" + e.GetType().Name + " : " + e.Message + "）"); }
        }

        private static readonly string[] Suspect = { "tooltip", "hover", "hint", "tip", "pointer" };

        private static void Walk(object go, string path, int depth, int maxDepth, ref int visited,
            List<string> hits, int cap)
        {
            try
            {
                if (!Reflect.Usable(go) || visited >= cap) return;
                visited++;
                string name = GoName(go);
                string here = path + "/" + name;
                string lower = (name ?? "").ToLowerInvariant();

                bool suspect = false;
                foreach (var s in Suspect) if (lower.IndexOf(s, StringComparison.Ordinal) >= 0) { suspect = true; break; }

                var tr = Reflect.Prop(go, "transform");
                var comps = ComponentsOf(go);
                if (comps != null)
                {
                    for (int i = 0; i < comps.Count; i++)
                    {
                        var c = comps[i];
                        if (c == null) continue;
                        var cn = c.GetType().Name.ToLowerInvariant();
                        foreach (var s in Suspect) if (cn.IndexOf(s, StringComparison.Ordinal) >= 0) { suspect = true; break; }
                    }
                }

                if (suspect && hits.Count < 60)
                {
                    var sb = new StringBuilder();
                    sb.Append(here).Append("  activeInHierarchy=").Append(S(Reflect.Prop(go, "activeSelf")));
                    sb.Append(" 组件=[");
                    if (comps == null) sb.Append("枚举不可用");
                    else
                    {
                        for (int i = 0; i < comps.Count; i++)
                        {
                            if (i > 0) sb.Append(',');
                            sb.Append(comps[i] == null ? "?" : comps[i].GetType().Name);
                        }
                    }
                    sb.Append(']');
                    
                    if (comps != null)
                    {
                        for (int i = 0; i < comps.Count; i++)
                        {
                            var c = comps[i];
                            if (c == null) continue;
                            if (c.GetType().Name.IndexOf("TextMeshPro", StringComparison.OrdinalIgnoreCase) < 0) continue;
                            var txt = Reflect.Str(Reflect.Prop(c, "text"));
                            sb.Append(" | TMP.text(len=").Append(txt == null ? -1 : txt.Length).Append(")=")
                              .Append(Esc(Trunc(txt, 200)));
                        }
                    }
                    hits.Add(sb.ToString());
                }

                if (depth >= maxDepth) return;
                int n = Reflect.ToInt(Reflect.Prop(tr, "childCount"));
                for (int i = 0; i < n; i++)
                {
                    if (visited >= cap) return;
                    var child = Reflect.Call1(tr, "GetChild", i);
                    Walk(Reflect.Prop(child, "gameObject"), here, depth + 1, maxDepth, ref visited, hits, cap);
                }
            }
            catch { }
        }

        
        
        

        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        private static List<object> ComponentsOf(object go)
        {
            if (!Reflect.Usable(go)) return null;

            
            var byIndex = ComponentsByIndex(go);
            if (byIndex != null) return byIndex;

            
            var byIl2Cpp = ComponentsByTypeArg(go, ToIl2CppType(_componentType));
            if (byIl2Cpp != null) return byIl2Cpp;

            
            var byGeneric = ComponentsByGeneric(go, _componentType);
            if (byGeneric != null) return byGeneric;

            
            return ComponentsByTypeArg(go, _componentType);
        }

        
        private static List<object> ComponentsByIndex(object go)
        {
            try
            {
                var countM = FindMethodByName(go.GetType(), "GetComponentCount", 0);
                var atM = FindMethodByName(go.GetType(), "GetComponentAtIndex", 1);
                if (countM == null || atM == null) return null;
                if (atM.GetParameters()[0].ParameterType != typeof(int)) return null;

                var cv = countM.Invoke(go, null);
                if (!(cv is int)) return null;
                int n = (int)cv;
                if (n <= 0) return null;              

                var list = new List<object>(n);
                for (int i = 0; i < n; i++) list.Add(atM.Invoke(go, new object[] { i }));
                return list;
            }
            catch { return null; }
        }

        
        
        
        
        private static List<object> ComponentsByTypeArg(object go, object typeArg)
        {
            if (typeArg == null) return null;
            try
            {
                foreach (var m in go.GetType().GetMethods(All))
                {
                    if (m.Name != "GetComponents") continue;
                    var ps = m.GetParameters();
                    if (ps.Length != 1) continue;
                    if (!ps[0].ParameterType.IsInstanceOfType(typeArg)) continue;
                    var arr = m.Invoke(go, new object[] { typeArg });
                    var list = ToObjectList(arr);
                    if (list != null) return list;
                }
            }
            catch { }
            return null;
        }

        
        private static List<object> ComponentsByGeneric(object go, Type compType)
        {
            if (compType == null) return null;
            try
            {
                foreach (var m in go.GetType().GetMethods(All))
                {
                    if (m.Name != "GetComponents") continue;
                    if (!m.IsGenericMethodDefinition) continue;
                    if (m.GetGenericArguments().Length != 1) continue;
                    if (m.GetParameters().Length != 0) continue;
                    var arr = m.MakeGenericMethod(compType).Invoke(go, null);
                    var list = ToObjectList(arr);
                    if (list != null) return list;
                }
            }
            catch { }
            return null;
        }

        
        private static List<object> ToObjectList(object arr)
        {
            if (arr == null) return null;
            try
            {
                var en = arr as IEnumerable;
                if (en == null) return null;
                var list = new List<object>();
                foreach (var o in en) list.Add(o);
                return list;
            }
            catch { return null; }
        }

        
        
        
        
        
        
        
        private static object ToIl2CppType(Type t)
        {
            if (t == null) return null;
            try
            {
                var m = Il2CppTypeFrom();
                return m == null ? null : m.Invoke(null, new object[] { t });
            }
            catch { }
            return null;
        }

        
        private static bool Il2CppTypeBridgeAvailable() { return Il2CppTypeFrom() != null; }

        
        private static MethodInfo Il2CppTypeFrom()
        {
            try
            {
                
                
                var asm = typeof(Il2CppInterop.Runtime.IL2CPP).Assembly;
                var it = asm == null ? null : asm.GetType("Il2CppInterop.Runtime.Il2CppType");
                if (it == null) return null;
                foreach (var m in it.GetMethods(BindingFlags.Public | BindingFlags.Static))
                {
                    if (m.Name != "From") continue;
                    var ps = m.GetParameters();
                    if (ps.Length == 1 && ps[0].ParameterType == typeof(Type)) return m;
                }
            }
            catch { }
            return null;
        }

        
        private static MethodInfo FindMethodByName(Type t, string name, int paramCount)
        {
            if (t == null) return null;
            try
            {
                foreach (var m in t.GetMethods(All))
                {
                    if (m.Name != name) continue;
                    if (m.GetParameters().Length != paramCount) continue;
                    return m;
                }
            }
            catch { }
            return null;
        }

        
        private static object GetComponent(object go, Type compType)
        {
            if (!Reflect.Usable(go) || compType == null) return null;

            
            try
            {
                var byName = FindMethodByName(go.GetType(), "GetComponentByName", 1);
                if (byName != null && byName.GetParameters()[0].ParameterType == typeof(string))
                {
                    var got = byName.Invoke(go, new object[] { compType.Name });
                    if (Reflect.Usable(got) && compType.IsAssignableFrom(got.GetType())) return got;
                }
            }
            catch { }

            
            var byIl2Cpp = GetComponentByTypeArg(go, ToIl2CppType(compType));
            if (byIl2Cpp != null) return byIl2Cpp;

            
            return GetComponentByTypeArg(go, compType);
        }

        private static object GetComponentByTypeArg(object go, object typeArg)
        {
            if (typeArg == null) return null;
            try
            {
                foreach (var m in go.GetType().GetMethods(All))
                {
                    if (m.Name != "GetComponent") continue;
                    var ps = m.GetParameters();
                    if (ps.Length != 1) continue;
                    if (!ps[0].ParameterType.IsInstanceOfType(typeArg)) continue;
                    return m.Invoke(go, new object[] { typeArg });
                }
            }
            catch { }
            return null;
        }

        private static string MaybeTooltipish(Type t)
        {
            try
            {
                var sb = new StringBuilder();
                foreach (var i in t.GetInterfaces())
                {
                    var n = i.Name;
                    if (n.IndexOf("Pointer", StringComparison.OrdinalIgnoreCase) >= 0
                        || n.IndexOf("Hover", StringComparison.OrdinalIgnoreCase) >= 0)
                        sb.Append(" ★实现").Append(n);
                }
                foreach (var f in t.GetFields(All))
                {
                    var fn = f.Name;
                    foreach (var s in Suspect)
                        if (fn.IndexOf(s, StringComparison.OrdinalIgnoreCase) >= 0) { sb.Append(" ★字段").Append(fn); break; }
                }
                return sb.ToString();
            }
            catch { return ""; }
        }

        private static string GoName(object go)
        {
            try
            {
                if (!Reflect.Usable(go)) return "<无GO>";
                var n = Reflect.Str(Reflect.Prop(go, "name"));
                return string.IsNullOrEmpty(n) ? "<无名>" : n;
            }
            catch { return "<GO名读不到>"; }
        }

        private static string Val(object v)
        {
            try
            {
                if (v == null) return "<null>";
                if (v is string) return Esc(Trunc((string)v, 200));
                var s = Reflect.Str(v);
                return Esc(Trunc(s, 200));
            }
            catch { return "<读失败>"; }
        }

        
        
        
        
        private static string TmpTextOf(object v)
        {
            try
            {
                if (!Reflect.Usable(v)) return "";
                var tn = v.GetType().Name;
                if (tn.IndexOf("TextMeshPro", StringComparison.OrdinalIgnoreCase) < 0
                    && tn.IndexOf("TMP_Text", StringComparison.OrdinalIgnoreCase) < 0) return "";
                var txt = Reflect.Str(Reflect.Prop(v, "text"));
                var go = Reflect.Prop(v, "gameObject");
                return "\n        ↳ 该 TMP 的 text(len=" + (txt == null ? -1 : txt.Length)
                    + ") = " + Esc(Trunc(txt, 200))
                    + "  ｜ GO=" + Esc(GoName(go))
                    + " activeSelf=" + S(Reflect.Prop(go, "activeSelf"));
            }
            catch { return ""; }
        }

        private static string ColorStr(object v)
        {
            try
            {
                if (v == null) return "<null>";
                var t = v.GetType();
                var sb = new StringBuilder("(");
                foreach (var fn in new[] { "r", "g", "b", "a" })
                {
                    var f = t.GetField(fn, All);
                    sb.Append(fn).Append('=').Append(f == null ? "?" : Reflect.Str(f.GetValue(v))).Append(' ');
                }
                return sb.Append(')').ToString();
            }
            catch { return "<?>"; }
        }

        private static string S(object o) { return o == null ? "?" : (Reflect.Str(o) ?? "?"); }

        private static string D(string s) { return string.IsNullOrEmpty(s) ? "(未取到)" : "\"" + s + "\""; }

        private static string Describe(string s)
        {
            if (s == null) return "(null)";
            return "长度=" + s.Length + " [" + Esc(Trunc(s, 110)) + "]";
        }

        private static string Trunc(string s, int max)
        {
            if (s == null) return null;
            var one = s.Replace("\r", "\\r").Replace("\n", "\\n").Replace("\t", "\\t");
            return one.Length > max ? one.Substring(0, max) + "…(共" + one.Length + "字符)" : one;
        }

        private static string Esc(string s) { return s == null ? "(null)" : "\"" + s + "\""; }
    }
}
