using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Text;
using HarmonyLib;
using Il2CppInterop.Runtime;
using Il2Cpp = Il2CppInterop.Runtime.IL2CPP;

namespace Oikonomia.Probe
{
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    internal static class RiziaProbe
    {
        public const string Version = "0.3.5";

        private const BindingFlags All = BindingFlags.Static | BindingFlags.Instance
                                       | BindingFlags.Public | BindingFlags.NonPublic;

        
        private static Type _statType;        
        private static Type _dataType;        
        private static Type _modType;         
        private static Type _hudPanelType;    
        private static Type _panelDataType;   
        private static Type _persistType;     
        private static Type _tmpBaseType;     

        
        private static IntPtr _statPtr;                       
        private static string _statName = "";
        private static readonly Dictionary<IntPtr, string> _slots = new Dictionary<IntPtr, string>();  
        private static readonly HashSet<string> _containerDone = new HashSet<string>(StringComparer.Ordinal);
        private static readonly HashSet<string> _tmpParamsDone = new HashSet<string>(StringComparer.Ordinal);
        private static bool _listDumped;
        private static int _statDumps;
        private static int _suppressed;
        private static string _storyPack;
        private static bool _storyPackKnown;
        private static bool _gateNoteWritten;
        private static bool _active;

        
        private static int _cSetupData, _cTmpSetSeen;
        private static int _cGetStatTooltip, _cGetTooltipModifier, _cGetModifierText;

        
        
        
        
        private static readonly string[] RequiredTypes = { "TemplateHUDStat", "HUDStatData", "TMPro.TMP_Text" };

        
        
        

        
        private static readonly string[] TypeProbeRows =
        {
            "TemplateHUDStat", "HUDStatData", "HUDStatProperties", "HUDPeriodicStatModifierData",
            "HUDPanel", "HUDPanelData", "PersistenceManager",
            "TMPro.TMP_Text", "TMPro.TextMeshProUGUI",
            "UnityEngine.GameObject", "UnityEngine.Transform", "UnityEngine.RectTransform",
            "UnityEngine.UI.Image", "UnityEngine.UI.LayoutGroup",
            "UnityEngine.UI.HorizontalOrVerticalLayoutGroup", "UnityEngine.UI.ContentSizeFitter",
            "UnityEngine.UI.LayoutElement", "UnityEngine.UI.RectMask2D", "UnityEngine.UI.Mask",
            "UnityEngine.UI.Selectable", "UnityEngine.UI.Button", "UnityEngine.UI.Graphic",
            "UnityEngine.CanvasGroup", "UnityEngine.Canvas", "UnityEngine.Sprite", "UnityEngine.RectOffset"
        };

        public static void Install(Harmony harmony, Assembly asm)
        {
            RiziaLog.Init();

            
            
            var gate = new List<string>();
            int resolved = 0;
            try
            {
                resolved = SafeResolve.RunGate(
                    "R-TYPE 启动安全闸（0.3.4）：程序集/类型解析 + il2cpp 原生注册情况 + 本次不可用清单",
                    TypeProbeRows, gate.Add, RequiredTypes);
            }
            catch (Exception e)
            {
                gate.Add("  ⚠ 安全闸异常（已吞掉）：" + e.GetType().Name + " : " + e.Message);
            }

            _statType = Reflect.FindType(asm, "TemplateHUDStat");
            _dataType = Reflect.FindType(asm, "HUDStatData");
            _modType = Reflect.FindType(asm, "HUDPeriodicStatModifierData");
            _hudPanelType = Reflect.FindType(asm, "HUDPanel");
            _panelDataType = Reflect.FindType(asm, "HUDPanelData");
            _persistType = Reflect.FindType(asm, "PersistenceManager");
            _tmpBaseType = LoadType("TMPro.TMP_Text") ?? LoadType("TMPro.TextMeshProUGUI");

            RiziaLog.WriteHeader(resolved, TypeProbeRows.Length, "（逐类型明细见下方 [R-TYPE] 段）");
            RiziaLog.Mark("===== R 组（Rizia 实况探针 " + Version + "）挂载 =====");
            RiziaLog.Raw("  完整取证只写本文件（rizia-ui-probe.log），不写 oi-probe.log。");
            RiziaLog.Raw(string.Join(Environment.NewLine, gate.ToArray()));

            try { ProbeLog.Mark("[R] " + SafeResolve.OneLineSummary(resolved, TypeProbeRows.Length)); }
            catch { }

            if (_statType == null)
            {
                RiziaLog.Error("[R] 致命：Assembly-CSharp 里没有 TemplateHUDStat —— 探针无法工作。");
                return;
            }

            
            PatchPost(harmony, MethodExact(_statType, "Setup", _dataType), "OnStatSetupData",
                "TemplateHUDStat.Setup(HUDStatData)");
            PatchPost(harmony, MethodEmpty(_statType, "Setup"), "OnStatSetupNoArg",
                "TemplateHUDStat.Setup()");
            PatchPost(harmony, MethodEmpty(_statType, "SetLocalizableTexts"), "OnSetLocalizableTexts",
                "TemplateHUDStat.SetLocalizableTexts()");
            PatchPost(harmony, MethodEmpty(_statType, "GetStatTooltipText"), "OnGetStatTooltipText",
                "TemplateHUDStat.GetStatTooltipText()  ← ②");
            PatchPost(harmony, MethodExact(_statType, "GetTooltipModifierTextString", _dataType),
                "OnGetTooltipModifierTextString", "TemplateHUDStat.GetTooltipModifierTextString(HUDStatData)  ← ②");
            PatchPost(harmony, MethodExact(_statType, "GetModifierText", _modType), "OnGetModifierText",
                "TemplateHUDStat.GetModifierText(HUDPeriodicStatModifierData)  ← ②");

            
            if (_hudPanelType != null)
            {
                PatchPost(harmony, MethodExact(_hudPanelType, "Setup", _panelDataType), "OnPanelSetupData",
                    "HUDPanel.Setup(HUDPanelData)  ← ③");
                PatchPost(harmony, MethodEmpty(_hudPanelType, "Setup"), "OnPanelSetupNoArg",
                    "HUDPanel.Setup()  ← ③", warnOnly: true);
            }

            
            InstallTmpTextHooks(harmony);

            
            if (_persistType != null)
                PatchPost(harmony, MethodEmpty(_persistType, "GetCurrentStoryPack"), "OnStoryPack",
                    "PersistenceManager.GetCurrentStoryPack()（剧本包佐证）", warnOnly: true);

            RiziaLog.Mark("===== 挂载完成。等待「Rizia 档 · 悬停预算」触发采集 =====");
            RiziaLog.WriteSummary("挂载清单（本段即结论：哪些钩子生效了）", MountLines());
        }

        
        private static readonly string[] TmpSetterCandidates = TmpSetTextHook.CandidateTypes;

        private static void InstallTmpTextHooks(Harmony harmony)
        {
            if (_tmpBaseType == null)
            {
                RiziaLog.Error("[R] 致命：找不到 TMPro.TMP_Text —— ① 渲染文本原始值不可采集（降级）。");
                return;
            }

            
            TmpSetTextHook.Install(harmony, _tmpBaseType, typeof(RiziaProbe).GetMethod("OnTmpSetText", All));

            
            PatchSetTextOverload(harmony, new[] { typeof(string) });
            PatchSetTextOverload(harmony, new[] { typeof(string), typeof(bool) });
        }

        private static void PatchSetTextOverload(Harmony harmony, Type[] args)
        {
            try
            {
                var mi = MethodExact(_tmpBaseType, "SetText", args);
                if (mi == null)
                {
                    RiziaLog.Line("[R][mount] · TMP_Text.SetText(" + ArgNames(args) + ") 不存在，跳过");
                    return;
                }
                if (!SafeResolve.Patchable(mi))
                {
                    RiziaLog.Line("[R][mount] · TMP_Text.SetText(" + ArgNames(args)
                                  + ") 无实现 → 按 HarmonyX 建议跳过，不挂");
                    return;
                }
                var refusal = SafeResolve.PatchRefusal(mi);
                if (refusal != null)
                {
                    RiziaLog.Line("[R][mount] · TMP_Text.SetText(" + ArgNames(args) + ") 体检未过 → 跳过：" + refusal);
                    return;
                }
                harmony.Patch(mi, postfix: new HarmonyMethod(
                    typeof(RiziaProbe).GetMethod("OnTmpSetText", All)));
                RiziaLog.Mark("[R][mount] ✓ TMP_Text.SetText(" + ArgNames(args) + ")"
                              + "  ← ① 直写 m_text 路径（**绕过 set_text**，上一轮漏的就是它）");
            }
            catch (Exception e)
            {
                RiziaLog.Error("[R][mount] ✗ SetText(" + ArgNames(args) + ") 挂载失败：" + e.Message);
            }
        }

        private static string ArgNames(Type[] a)
        {
            var sb = new StringBuilder();
            for (int i = 0; i < a.Length; i++) { if (i > 0) sb.Append(','); sb.Append(a[i].Name); }
            return sb.ToString();
        }

        
        
        

        
        

        
        
        
        private static Type LoadType(string fullName)
        {
            if (string.IsNullOrEmpty(fullName)) return null;
            return SafeResolve.FindTypePreferred(fullName, SafeResolve.CandidateAssemblies(fullName));
        }

        
        
        

        private static MethodInfo MethodExact(Type t, string name, params Type[] args)
        {
            if (t == null) return null;
            foreach (var a in args) if (a == null) return null;
            try
            {
                return t.GetMethod(name, All, null, args, null)
                       ?? AccessTools.Method(t, name, args);
            }
            catch { return null; }
        }

        private static MethodInfo MethodEmpty(Type t, string name)
        {
            if (t == null) return null;
            try
            {
                return t.GetMethod(name, All, null, Type.EmptyTypes, null)
                       ?? AccessTools.Method(t, name, Type.EmptyTypes);
            }
            catch { return null; }
        }

        private static void PatchPost(Harmony harmony, MethodInfo target, string postfixName, string label,
            bool warnOnly = false)
        {
            if (target == null)
            {
                RiziaLog.Mark("[R][mount] �?" + label + " —�?方法不存在（这行本身即结论）");
                return;
            }
            
            var refusal = SafeResolve.PatchRefusal(target);
            if (refusal != null)
            {
                RiziaLog.Line("[R][mount] · " + label + " 未挂（体检）：" + refusal);
                return;
            }
            try
            {
                harmony.Patch(target, postfix: new HarmonyMethod(
                    typeof(RiziaProbe).GetMethod(postfixName, All)));
                RiziaLog.Mark("[R][mount] �?" + label);
            }
            catch (Exception e)
            {
                var m = "[R][mount] ✗ " + label + " 挂载失败：" + e.GetType().Name + " : " + e.Message;
                if (warnOnly) RiziaLog.Line(m); else RiziaLog.Error(m);
            }
        }

        private static List<string> MountLines()
        {
            var l = new List<string>();
            l.Add("日志文件          : " + RiziaLog.LogPath);
            l.Add("归档文件          : " + RiziaLog.ArchivePath);
            l.Add("全文目录          : " + RiziaLog.TextDir);
            l.Add("主开关文�?       : " + RiziaLog.CfgPath);
            l.Add("兼容开关文件      : " + RiziaLog.OiCfgPath + "（同名键优先）");
            l.Add("状态              : " + (_active ? "已激活（见到 Rizia 统计）" : "待命（尚未见到 Rizia 统计）"));
            l.Add("剧本�?           : " + (_storyPackKnown ? _storyPack : "(尚未取到 —�?存档/回合保存时会取到)"));
            l.Add("门禁说明          : 默认只在统计 NameInDatabase/Path 含 \"Rizia\" 时工作；本体档静默。");
            return l;
        }

        
        
        

        private static bool Contains(string s, string needle)
        {
            return !string.IsNullOrEmpty(s)
                && s.IndexOf(needle, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        
        private static bool Gate(string nameInDb, string path)
        {
            if (!RiziaLog.Enabled) return false;
            if (RiziaLog.Force) return true;

            bool rizia = Contains(nameInDb, "Rizia") || Contains(path, "Rizia")
                         || (Contains(_storyPack, "Rizia") && !string.IsNullOrEmpty(nameInDb) && Contains(nameInDb, "HUDStat"));
            if (rizia)
            {
                if (!_active)
                {
                    _active = true;
                    RiziaLog.Mark("[R][gate] �?已进�?Rizia 采集模式 —�?触发统计：NameInDatabase=" + Q(nameInDb)
                                  + "  Path=" + Q(path)
                                  + "  剧本�?" + (_storyPackKnown ? _storyPack : "(未知)"));
                }
                return true;
            }

            _suppressed++;
            if (!_gateNoteWritten && _suppressed >= 3)
            {
                _gateNoteWritten = true;
                RiziaLog.Raw("  [R][gate] 已静�?" + _suppressed + " 条非 Rizia 统计"
                             + "（本探针只在 Rizia 档工作；这是**唯一**一条门禁提示，本体档下不会再有输出）。");
            }
            return false;
        }

        private static string StatName(object data)
        {
            if (!Reflect.Usable(data)) return null;
            return Reflect.Str(Reflect.Prop(data, "NameInDatabase"));
        }

        private static string StatPath(object data)
        {
            if (!Reflect.Usable(data)) return null;
            return Reflect.Str(Reflect.Prop(data, "Path"));
        }

        
        
        

        private static void OnStatSetupData(object __instance, object __0)
        {
            try
            {
                if (!Reflect.Usable(__instance)) return;
                var name = StatName(__0);
                var path = StatPath(__0);
                if (!Gate(name, path)) return;

                _cSetupData++;
                _statPtr = Reflect.Ptr(__instance);
                _statName = name ?? "(无名)";
                CaptureSlots(__instance, _statName);

                RiziaLog.Timeline("Setup(HUDStatData)  #" + _cSetupData + "  " + Q(_statName)
                                  + "  Path=" + Q(path) + "  实例指针=0x"
                                  + _statPtr.ToInt64().ToString("x", CultureInfo.InvariantCulture));

                if (RiziaLog.Once("STATDETAIL|" + _statName))
                    DumpStatDetail(__instance, __0, "Setup(HUDStatData) 。");

                
                SnapshotSlots(__instance, "悬停前基准（Setup 时刻 · 尚未悬停）");

                
                TryPanelFromStat(__instance);
            }
            catch (Exception e) { RiziaLog.Error("[R] OnStatSetupData 异常：" + e.GetType().Name + " : " + e.Message); }
        }

        private static void OnStatSetupNoArg(object __instance)
        {
            try
            {
                if (!Reflect.Usable(__instance)) return;
                if (!IsCapturedStat(__instance)) return;
                RiziaLog.Timeline("Setup()（无参）被调�? 统计=" + Q(_statName));
                CaptureSlots(__instance, _statName);
            }
            catch { }
        }

        private static void OnSetLocalizableTexts(object __instance)
        {
            try
            {
                if (!Reflect.Usable(__instance)) return;
                if (!IsCapturedStat(__instance)) return;
                RiziaLog.Timeline("SetLocalizableTexts() 被调�? 统计=" + Q(_statName));
                CaptureSlots(__instance, _statName);
                SnapshotSlots(__instance, "SetLocalizableTexts() 之后（本地化重写点）");
            }
            catch { }
        }

        private static bool IsCapturedStat(object stat)
        {
            if (!Reflect.Usable(stat)) return false;
            var p = Reflect.Ptr(stat);
            if (_statPtr != IntPtr.Zero && p == _statPtr) return true;
            
            return false;
        }

        private static void CaptureSlots(object stat, string name)
        {
            try
            {
                
                
                var stale = new List<IntPtr>();
                foreach (var kv in _slots)
                    if (!string.IsNullOrEmpty(kv.Value) && kv.Value.StartsWith(name + " / ", StringComparison.Ordinal))
                        stale.Add(kv.Key);
                for (int i = 0; i < stale.Count; i++) _slots.Remove(stale[i]);
                if (_slots.Count > 64) _slots.Clear();   
                var main = Reflect.Prop(stat, "statTooltipText");
                var mod = Reflect.Prop(stat, "statTooltipModifierText");
                var mp = Reflect.Ptr(main);
                var dp = Reflect.Ptr(mod);
                if (mp != IntPtr.Zero) _slots[mp] = name + " / ①主�?statTooltipText";
                if (dp != IntPtr.Zero) _slots[dp] = name + " / ②明细槽 statTooltipModifierText";
            }
            catch { }
        }

        
        private static string StatOfSlot(string slot)
        {
            if (string.IsNullOrEmpty(slot)) return _statName;
            int i = slot.IndexOf(" / ", StringComparison.Ordinal);
            return i > 0 ? slot.Substring(0, i) : slot;
        }

        private static void TryPanelFromStat(object stat)
        {
            try
            {
                if (_listDumped || _hudPanelType == null) return;
                var tr = Reflect.Prop(stat, "transform");
                for (int d = 0; d < 8 && Reflect.Usable(tr); d++)
                {
                    var go = Reflect.Prop(tr, "gameObject");
                    var panel = GetComponent(go, _hudPanelType);
                    if (Reflect.Usable(panel))
                    {
                        RiziaLog.Mark("[R][③] 从统计实例向上" + d + " 层找到 HUDPanel"
                                      + Q(GoName(go)));
                        DumpPanelStats(panel, "由统计实例的祖先链定位（层深 " + d + "）");
                        return;
                    }
                    tr = Reflect.Prop(tr, "parent");
                }
            }
            catch { }
        }

        private static void OnPanelSetupData(object __instance, object __0)
        {
            try
            {
                if (!Reflect.Usable(__instance)) return;
                var pname = StatName(__0);
                var ppath = StatPath(__0);
                
                if (!_active && !RiziaLog.Force)
                {
                    if (!Gate(pname, ppath)) return;
                }
                RiziaLog.Mark("[R][③] HUDPanel.Setup(HUDPanelData)  NameInDatabase=" + Q(pname)
                              + "  Path=" + Q(ppath));
                DumpPanelStats(__instance, "HUDPanel.Setup(HUDPanelData)");
            }
            catch (Exception e) { RiziaLog.Error("[R] OnPanelSetupData 异常：" + e.GetType().Name + " : " + e.Message); }
        }

        private static void OnPanelSetupNoArg(object __instance)
        {
            try
            {
                if (!Reflect.Usable(__instance) || (!_active && !RiziaLog.Force)) return;
                RiziaLog.Timeline("HUDPanel.Setup()（无参）被调用");
                DumpPanelStats(__instance, "HUDPanel.Setup()");
            }
            catch { }
        }

        
        private static void DumpPanelStats(object panel, string why)
        {
            if (_listDumped) return;
            if (!Reflect.Usable(panel)) return;
            _listDumped = true;

            RiziaLog.Section("【HUDPanel.instantiatedHUDStats 全清单（" + why + "）");
            try
            {
                RiziaLog.Raw("  面板 GameObject : " + Q(GoName(Reflect.Prop(panel, "gameObject"))));
                RiziaLog.Raw("  面板 isShowing  : " + S(Reflect.Prop(panel, "isShowing")));
                var cg = Reflect.Prop(panel, "canvasGroup");
                if (Reflect.Usable(cg))
                    RiziaLog.Raw("  面板 CanvasGroup: alpha=" + S(Reflect.Prop(cg, "alpha"))
                                 + " interactable=" + S(Reflect.Prop(cg, "interactable"))
                                 + " blocksRaycasts=" + S(Reflect.Prop(cg, "blocksRaycasts")));
                var slotC = Reflect.Prop(panel, "hudSlotContainer");
                if (Reflect.Usable(slotC))
                    RiziaLog.Raw("  hudSlotContainer: " + Q(GoName(Reflect.Prop(slotC, "gameObject")))
                                 + "  rect=" + RectStr(slotC));

                var list = Reflect.Prop(panel, "instantiatedHUDStats");
                int n = Reflect.ListCount(list);
                RiziaLog.Raw("  instantiatedHUDStats.Count = " + n);
                if (n <= 0)
                {
                    RiziaLog.Raw("  （清单为�?—�?面板尚未创建统计条目，或此时机不对）");
                    RiziaLog.Raw("  提示：如需要，可再悬停一次预算让面板重建，然后回看本段。");
                    _listDumped = false;   
                    return;
                }

                for (int i = 0; i < n && i < 64; i++)
                {
                    var st = ListAt(list, i);
                    if (!Reflect.Usable(st)) { RiziaLog.Raw("  [" + i + "] <读取失败>"); continue; }
                    var data = Reflect.Prop(st, "currentHUDStatData");
                    var nm = StatName(data) ?? "(无名)";
                    var pa = StatPath(data);
                    var props = Reflect.Prop(data, "HUDStatProperties");
                    var mods = props == null ? null : Reflect.Prop(props, "Modifiers");
                    int mc = Reflect.ListCount(mods);
                    bool isRizia = Contains(nm, "Rizia") || Contains(pa, "Rizia");

                    var colors = ColorStr(Reflect.Prop(st, "tooltipPositiveColor")) + " / "
                               + ColorStr(Reflect.Prop(st, "tooltipNegativeColor")) + " / "
                               + ColorStr(Reflect.Prop(st, "tooltipZeroColor"));

                    RiziaLog.Raw("  [" + i + "] " + (isRizia ? "★Rizia " : "  本体  ") + nm
                                 + "  |  Path=" + Q(pa)
                                 + "  |  Modifiers=" + (mc < 0 ? "?" : mc.ToString(CultureInfo.InvariantCulture))
                                 + "  |  ModifiersTotalVariable=" + Q(props == null ? null : Reflect.Str(Reflect.Prop(props, "ModifiersTotalVariable")))
                                 + "  |  StatVariable=" + Q(props == null ? null : Reflect.Str(Reflect.Prop(props, "StatVariable"))));

                    RiziaLog.Raw("        hideModifier=" + S(Reflect.Prop(st, "hideModifier"))
                                 + "  currentValue=" + S(Reflect.Prop(st, "currentValue"))
                                 + "  Title=" + Q(props == null ? null : Reflect.Str(Reflect.Prop(props, "Title")))
                                 + "  ImagePath=" + Q(props == null ? null : Reflect.Str(Reflect.Prop(props, "ImagePath"))));
                    RiziaLog.Raw("        tooltip 三色(�?�?�? = " + colors);
                    RiziaLog.Raw("        statText=" + TmpOneLine(Reflect.Prop(st, "statText"))
                                 + "  statIcon=" + SpriteStr(Reflect.Prop(Reflect.Prop(st, "statIcon"), "sprite")));
                }

                if (n > 64) RiziaLog.Raw("  （共 " + n + " 条，只打了前 64 条）");
                RiziaLog.Raw("");
                RiziaLog.Raw("  �?对比要点：Rizia 的预算统�?Modifiers 应有 48 条（本体 GovernmentBudget �?0 条）"
                             + " —— 这就是「明细从哪来」的差异来源。");
            }
            catch (Exception e)
            {
                RiziaLog.Error("[R] 统计清单 dump 失败：" + e.GetType().Name + " : " + e.Message);
                _listDumped = false;
            }
        }

        
        private static void DumpStatDetail(object stat, object data, string when)
        {
            RiziaLog.Section("【统计条目详情（" + when + "）：" + (_statName ?? "?"));
            try
            {
                var props = Reflect.Prop(data, "HUDStatProperties");
                RiziaLog.Raw("  HUDStatData.NameInDatabase     = " + Q(StatName(data)));
                RiziaLog.Raw("  HUDStatData.Path               = " + Q(StatPath(data)));
                RiziaLog.Raw("  HUDStatData.IsWIP              = " + S(Reflect.Prop(data, "IsWIP")));
                if (props != null)
                {
                    RiziaLog.Raw("  HUDStatProperties.Title        = " + Q(Reflect.Str(Reflect.Prop(props, "Title"))));
                    RiziaLog.Raw("  HUDStatProperties.ImagePath    = " + Q(Reflect.Str(Reflect.Prop(props, "ImagePath"))));
                    RiziaLog.Raw("  HUDStatProperties.StatVariable = " + Q(Reflect.Str(Reflect.Prop(props, "StatVariable"))));
                    RiziaLog.Raw("  …ModifiersTotalVariable        = " + Q(Reflect.Str(Reflect.Prop(props, "ModifiersTotalVariable"))));
                    RiziaLog.Raw("  …IsStatValueColored            = " + S(Reflect.Prop(props, "IsStatValueColored")));
                    RiziaLog.Raw("  …HasMinValue/HasMaxValue       = " + S(Reflect.Prop(props, "HasMinValue")) + " / " + S(Reflect.Prop(props, "HasMaxValue")));
                    RiziaLog.Raw("  …MinValue/MaxValue             = " + Q(Reflect.Str(Reflect.Prop(props, "MinValue"))) + " / " + Q(Reflect.Str(Reflect.Prop(props, "MaxValue"))));

                    var mods = Reflect.Prop(props, "Modifiers");
                    int mc = Reflect.ListCount(mods);
                    RiziaLog.Raw("  �?Modifiers 条数               = " + (mc < 0 ? "?" : mc.ToString(CultureInfo.InvariantCulture)));
                    if (mc > 0)
                    {
                        for (int i = 0; i < mc && i < 64; i++)
                        {
                            var s = Reflect.Str(ListAt(mods, i));
                            RiziaLog.Raw("      [" + i.ToString("00", CultureInfo.InvariantCulture) + "] " + Q(s));
                        }
                        if (mc > 64) RiziaLog.Raw("      …（共" + mc + " 条，只列前 64 条；完整名单见 docs\\rizia-预算UI研究.md §3.1）");
                    }
                }

                RiziaLog.Raw("  --- TemplateHUDStat 实例字段 ---");
                RiziaLog.Raw("  hideModifier                   = " + S(Reflect.Prop(stat, "hideModifier")));
                RiziaLog.Raw("  currentValue                   = " + S(Reflect.Prop(stat, "currentValue")));
                RiziaLog.Raw("  positiveColor/negativeColor/zeroColor = "
                             + ColorStr(Reflect.Prop(stat, "positiveColor")) + " / "
                             + ColorStr(Reflect.Prop(stat, "negativeColor")) + " / "
                             + ColorStr(Reflect.Prop(stat, "zeroColor")));
                RiziaLog.Raw("  tooltip �?�?�?              = "
                             + ColorStr(Reflect.Prop(stat, "tooltipPositiveColor")) + " / "
                             + ColorStr(Reflect.Prop(stat, "tooltipNegativeColor")) + " / "
                             + ColorStr(Reflect.Prop(stat, "tooltipZeroColor")));
                var btn = Reflect.Prop(stat, "button");
                RiziaLog.Raw("  button                         = " + (Reflect.Usable(btn)
                             ? (ManagedName(btn) + " interactable=" + S(Reflect.Prop(btn, "interactable")))
                             : "(�?"));
                RiziaLog.Raw("  statText (.text)               = " + TmpOneLine(Reflect.Prop(stat, "statText")));
                RiziaLog.Raw("  statIcon.sprite                = " + SpriteStr(Reflect.Prop(Reflect.Prop(stat, "statIcon"), "sprite")));
                RiziaLog.Raw("  statTooltipText                = " + TmpOneLine(Reflect.Prop(stat, "statTooltipText")));
                RiziaLog.Raw("  statTooltipModifierText        = " + TmpOneLine(Reflect.Prop(stat, "statTooltipModifierText")));
                RiziaLog.Raw("  实例指针                       = 0x"
                             + Reflect.Ptr(stat).ToInt64().ToString("x", CultureInfo.InvariantCulture));
            }
            catch (Exception e)
            {
                RiziaLog.Error("[R] 统计详情 dump 失败：" + e.GetType().Name + " : " + e.Message);
            }
        }

        
        

        private static void OnGetStatTooltipText(object __instance, ref string __result)
        {
            try
            {
                if (!Reflect.Usable(__instance) || !IsCapturedStat(__instance)) return;
                _cGetStatTooltip++;
                LogProducer("GetStatTooltipText()", null, __result, "①②");
            }
            catch { }
        }

        private static void OnGetTooltipModifierTextString(object __instance, object __0, ref string __result)
        {
            try
            {
                if (!Reflect.Usable(__instance) || !IsCapturedStat(__instance)) return;
                _cGetTooltipModifier++;
                var nm = StatName(__0);
                var props = Reflect.Usable(__0) ? Reflect.Prop(__0, "HUDStatProperties") : null;
                var mods = props == null ? null : Reflect.Prop(props, "Modifiers");
                int mc = Reflect.ListCount(mods);
                var argDesc = "参数 HUDStatData: NameInDatabase=" + Q(nm)
                              + " Path=" + Q(StatPath(__0))
                              + " Modifiers=" + (mc < 0 ? "?" : mc.ToString(CultureInfo.InvariantCulture))
                              + " ModifiersTotalVariable=" + Q(props == null ? null : Reflect.Str(Reflect.Prop(props, "ModifiersTotalVariable")))
                              + " hideModifier=" + S(Reflect.Prop(__instance, "hideModifier"));
                LogProducer("GetTooltipModifierTextString(HUDStatData)", argDesc, __result, "②★");
                
                SnapshotSlots(__instance, "GetTooltipModifierTextString 返回时（悬停中）");
            }
            catch { }
        }

        private static void OnGetModifierText(object __instance, object __0, ref string __result)
        {
            try
            {
                if (!Reflect.Usable(__instance) || !IsCapturedStat(__instance)) return;
                _cGetModifierText++;
                var props = Reflect.Usable(__0) ? Reflect.Prop(__0, "HUDPeriodicStatModifierProperties") : null;
                var argDesc = "参数 HUDPeriodicStatModifierData: NameInDatabase=" + Q(StatName(__0))
                              + " Variable=" + Q(props == null ? null : Reflect.Str(Reflect.Prop(props, "Variable")))
                              + " Condition=" + Q(props == null ? null : Reflect.Str(Reflect.Prop(props, "Condition")))
                              + " Description=" + Q(props == null ? null : Reflect.Str(Reflect.Prop(props, "Description")));
                LogProducer("GetModifierText(HUDPeriodicStatModifierData)", argDesc, __result, "②★");
            }
            catch { }
        }

        
        private static void LogProducer(string who, string argDesc, string ret, string tag)
        {
            try
            {
                int len = ret == null ? -1 : ret.Length;
                var sha = RiziaLog.Sha256Hex(ret);
                RiziaLog.Timeline("[R-HOVER] " + tag + " " + who + " 被调�? 返回值长�?" + len
                                  + "  sha256=" + Short(sha) + "  栈：" + Stack(4));

                var key = "CALL|" + who + "|" + _statName + "|" + sha;
                if (!RiziaLog.Counted(key, 3)) return;

                RiziaLog.Raw("");
                RiziaLog.Raw("  ★[R-CALL] " + who + "  ×" + RiziaLog.Hits(key) + " 次调用");
                RiziaLog.Raw("      统计实例     : " + Q(_statName));
                RiziaLog.Raw("      参数         : " + (argDesc ?? "(�?"));

                if (ret == null)
                {
                    RiziaLog.Raw("      返回�?      : (null)");
                }
                else
                {
                    RiziaLog.Raw("      返回长度     : " + len + " 字符 / " + RiziaLog.Utf8Len(ret) + " UTF-8 字节");
                    RiziaLog.Raw("      返回 SHA256  : " + sha);
                    RiziaLog.Raw("      返回 \u8f6c义头  : " + RiziaLog.UnicodeEscape(ret, 240));
                    if (RiziaLog.FullTextOnce("call-" + MethodSlug(who), sha, ret))
                        RiziaLog.Raw("      返回全文文件 : " + RiziaLog.TextFilePath("call-" + MethodSlug(who), sha));
                    RiziaLog.Raw("      返回全文（不截断�?");
                    RiziaLog.Raw(Indent(RiziaLog.Esc(ret)));
                }
                var st = Stack(8);
                RiziaLog.Raw("      调用�?      : " + st);
            }
            catch { }
        }

        private static string MethodSlug(string who)
        {
            int i = who.IndexOf('(');
            return i > 0 ? who.Substring(0, i) : who;
        }

        
        

        private static void OnTmpSetText(object __instance, string __0)
        {
            try
            {
                if (!RiziaLog.Enabled) return;
                if (_slots.Count == 0 || __instance == null) return;

                var p = Reflect.Ptr(__instance);
                string slot;
                if (!_slots.TryGetValue(p, out slot)) return;   

                _cTmpSetSeen++;
                RiziaLog.Timeline("[R-SET] #" + _cTmpSetSeen + " 写入 " + slot
                                  + "  长度=" + (__0 == null ? -1 : __0.Length)
                                  + "  sha256=" + Short(RiziaLog.Sha256Hex(__0))
                                  + "  栈：" + Stack(5));

                var key = "SET|" + slot + "|" + RiziaLog.Sha256Hex(__0);
                if (!RiziaLog.Counted(key, 2)) return;

                RiziaLog.Raw("");
                RiziaLog.Raw("  ★[R-TEXT] �?渲染文本原始值（" + slot + "�? �?"
                             + RiziaLog.Hits(key) + " 次见到该内容");
                RiziaLog.Raw("      统计实例 : " + Q(StatOfSlot(slot)));
                LogTextBlock(slot, __0);
                RiziaLog.Raw("      调用�?  : " + Stack(10));

                
                MaybeDumpContainer(__instance, slot, "首次捕获到该槽位的文本写入");
            }
            catch { }
        }

        
        private static void LogTextBlock(string slot, string text)
        {
            if (text == null)
            {
                RiziaLog.Raw("      内容     : (null)");
                return;
            }
            var sha = RiziaLog.Sha256Hex(text);
            RiziaLog.Raw("      字符�?  : " + text.Length + "   UTF-8 字节�? " + RiziaLog.Utf8Len(text));
            RiziaLog.Raw("      SHA256   : " + sha);
            RiziaLog.Raw("      转义�?  : " + RiziaLog.UnicodeEscape(text, 240));
            if (RiziaLog.FullTextOnce(slot, sha, text))
            {
                RiziaLog.Raw("      独立全文 : " + RiziaLog.TextFilePath(slot, sha)
                             + "  （逐字节原文，UTF-8 �?BOM，文件内�?=== 游戏字符串）");
            }
            RiziaLog.Raw("      完整内容（不截断，\\n 已显形）:");
            RiziaLog.Raw(Indent(RiziaLog.Esc(text)));
        }

        private static string Indent(string s)
        {
            if (s == null) return "        (null)";
            var parts = s.Split('\n');
            var sb = new StringBuilder(s.Length + parts.Length * 8 + 16);
            for (int i = 0; i < parts.Length; i++)
                sb.Append("        | ").Append(parts[i]).Append('\n');
            return sb.ToString().TrimEnd('\n');
        }

        
        private static void SnapshotSlots(object stat, string when)
        {
            try
            {
                if (!Reflect.Usable(stat)) return;
                var main = Reflect.Prop(stat, "statTooltipText");
                var mod = Reflect.Prop(stat, "statTooltipModifierText");
                RiziaLog.Raw("");
                RiziaLog.Raw("  ── 槽位快照（" + when + "）");
                SnapshotOne("主槽 statTooltipText", main);
                SnapshotOne("明细�?statTooltipModifierText", mod);

                
                DumpTmpParams(main, "主槽 statTooltipText", when);
                DumpTmpParams(mod, "明细�?statTooltipModifierText", when);
            }
            catch (Exception e)
            {
                RiziaLog.Raw("     （快照失败：" + e.GetType().Name + " : " + e.Message + "）");
            }
        }

        private static void SnapshotOne(string label, object tmp)
        {
            if (!Reflect.Usable(tmp))
            {
                RiziaLog.Raw("     " + label + " = (不可�?/ 未接�?");
                return;
            }
            var txt = Reflect.Str(Reflect.Prop(tmp, "text"));
            var sha = RiziaLog.Sha256Hex(txt);
            RiziaLog.Raw("     " + label + " : activeInHierarchy="
                         + S(Reflect.Prop(Reflect.Prop(tmp, "gameObject"), "activeInHierarchy"))
                         + " GameObject=" + Q(GoName(Reflect.Prop(tmp, "gameObject"))));
            RiziaLog.Raw("        长度=" + (txt == null ? -1 : txt.Length)
                         + "  UTF-8字节=" + (txt == null ? -1 : RiziaLog.Utf8Len(txt))
                         + "  sha256=" + sha);
            if (RiziaLog.FullTextOnce("snap-" + MethodSlug(label), sha, txt))
                RiziaLog.Raw("        全文文件=" + RiziaLog.TextFilePath("snap-" + MethodSlug(label), sha));
            RiziaLog.Raw("        内容=" + RiziaLog.Esc(txt));
        }

        private static void OnStoryPack(ref string __result)
        {
            try
            {
                if (_storyPackKnown && string.Equals(_storyPack, __result, StringComparison.Ordinal)) return;
                _storyPack = __result;
                _storyPackKnown = true;
                RiziaLog.Mark("[R][gate] 剧本�?= " + Q(__result)
                              + "  �?" + (Contains(__result, "Rizia")
                                  ? "Rizia DLC ✓（采集规则：见�?Rizia 统计即工作）"
                                  : "�?Rizia（探针保持静默）"));
            }
            catch { }
        }

        
        
        

        private static void MaybeDumpContainer(object liveTmp, string slot, string why)
        {
            try
            {
                var statName = StatOfSlot(slot);
                if (string.IsNullOrEmpty(statName)) statName = _statName;
                if (string.IsNullOrEmpty(statName)) return;
                if (_containerDone.Contains(statName)) return;
                if (_statDumps >= RiziaLog.MaxStatDumps) return;
                bool isBudget = Contains(statName, "Budget");
                if (!isBudget && !RiziaLog.AllStats) return;

                _containerDone.Add(statName);
                _statDumps++;
                DumpContainerChain(liveTmp, slot, why, statName);
                if (_statDumps >= RiziaLog.MaxStatDumps || RiziaLog.AllStats)
                    RiziaLog.WriteSummary("�?容器 dump 后的总结（只写一次）", ContainerSummaryLines(statName));
            }
            catch (Exception e)
            {
                RiziaLog.Error("[R] 容器 dump 失败：" + e.GetType().Name + " : " + e.Message);
            }
        }

        private static List<string> ContainerSummaryLines(string statName)
        {
            var l = new List<string>();
            l.Add("�?�?dump 容器层级：统�?" + statName + "，次�?" + _statDumps + " / " + RiziaLog.MaxStatDumps);
            l.Add("   槽位识别�?     : " + _slots.Count + "（主�?明细槽，按指针）");
            l.Add("   Rizia Setup 次数: " + _cSetupData);
            l.Add("   TMP 文本写入次数: " + _cTmpSetSeen);
            l.Add("   �?生产者调用次数（�?的关键结论）：GetStatTooltipText=" + _cGetStatTooltip
                  + "  GetTooltipModifierTextString=" + _cGetTooltipModifier
                  + "  GetModifierText=" + _cGetModifierText);
            if (_cGetTooltipModifier == 0)
                l.Add("   ★ GetTooltipModifierTextString 未被调用：Rizia 明细**不来自**这条 modifier 管线。"
                      + "要另找渲染源（见 [R-SET] 时间线里真正的写入方）。");
            else
                l.Add("   �?GetTooltipModifierTextString 被调用了 " + _cGetTooltipModifier
                      + " 次：Rizia 明细**走的就是**同一条 modifier 管线（格式串推断成立）。");
            l.Add("   被门禁静音的�?Rizia 统计�? " + _suppressed);
            l.Add("   剧本�?         : " + (_storyPackKnown ? _storyPack : "(未取�?"));
            l.Add("   日志            : " + RiziaLog.LogPath + "（" + RiziaLog.BytesWritten + " 字节，归档 "
                  + RiziaLog.Rotations + " 次）");
            return l;
        }

        private static void DumpContainerChain(object liveTmp, string slot, string why, string statName)
        {
            RiziaLog.Section("【承载容器完整外观（从" + slot + "」向上，why=" + why + "）");
            RiziaLog.Raw("  统计实例 : " + Q(statName));
            RiziaLog.Raw("  【修复说明】上一轮所有组件都被打成匿名<UnityEngine.Component>。"
                         + "本节每个组件同时给出「托管代理类型」与「il2cpp 权威类型」，差异一眼可见。");

            
            DumpTmpParams(liveTmp, slot, why);

            object cur = Reflect.Prop(liveTmp, "transform");
            int depth = 0;
            for (; depth < 8; depth++)
            {
                if (!Reflect.Usable(cur)) break;
                DumpLayer(depth, cur);
                var parent = Reflect.Prop(cur, "parent");
                if (!Reflect.Usable(parent)) break;
                cur = parent;
            }

            DumpCanvasAncestor(liveTmp);
            DumpSubtreeScan(liveTmp);
        }

        private static void DumpLayer(int depth, object tr)
        {
            try
            {
                var go = Reflect.Prop(tr, "gameObject");
                RiziaLog.Raw("");
                RiziaLog.Raw("  ── 层[" + depth + "] " + Q(GoName(go))
                             + "   activeSelf=" + S(Reflect.Prop(go, "activeSelf"))
                             + " activeInHierarchy=" + S(Reflect.Prop(go, "activeInHierarchy"))
                             + " layer=" + S(Reflect.Prop(go, "layer"))
                             + " tag=" + Q(Reflect.Str(Reflect.Prop(go, "tag"))));
                RiziaLog.Raw("     RectTransform : " + RectStr(tr));
                RiziaLog.Raw("       锚点 anchorMin=" + S(Reflect.Prop(tr, "anchorMin"))
                             + " anchorMax=" + S(Reflect.Prop(tr, "anchorMax"))
                             + " pivot=" + S(Reflect.Prop(tr, "pivot")));
                RiziaLog.Raw("       位置 anchoredPosition=" + S(Reflect.Prop(tr, "anchoredPosition"))
                             + " localPosition=" + S(Reflect.Prop(tr, "localPosition")));
                RiziaLog.Raw("       偏移 offsetMin=" + S(Reflect.Prop(tr, "offsetMin"))
                             + " offsetMax=" + S(Reflect.Prop(tr, "offsetMax")));
                RiziaLog.Raw("       缩放 localScale=" + S(Reflect.Prop(tr, "localScale"))
                             + " lossyScale=" + S(Reflect.Prop(tr, "lossyScale"))
                             + " childCount=" + S(Reflect.Prop(tr, "childCount")));

                var comps = ComponentsOf(go, out string how);
                if (comps == null)
                {
                    RiziaLog.Raw("        组件 : [�?四条枚举路径全失�?—�?把这一行回报]");
                }
                else
                {
                    RiziaLog.Raw("        组件[" + comps.Count + "] （枚举路径：" + how + "）");
                    for (int i = 0; i < comps.Count; i++)
                    {
                        if (comps[i] == null) { RiziaLog.Raw("          [" + i + "] <null>"); continue; }
                        DumpComponent(comps[i], "          [" + i + "] ");
                    }
                }
            }
            catch (Exception e)
            {
                RiziaLog.Raw("     层[" + depth + "] 读取失败：" + e.GetType().Name + " : " + e.Message);
            }
        }

        
        private static void DumpComponent(object comp, string prefix)
        {
            try
            {
                var managed = comp.GetType();
                var ptr = Reflect.Ptr(comp);
                var il2cppFull = Il2CppFullName(ptr);
                var il2cppAsm = Il2CppAsmName(ptr);

                RiziaLog.Raw(prefix + "真实类型 = " + (il2cppFull ?? "(取不�?")
                             + "   [asm=" + (il2cppAsm ?? "?") + "]"
                             + "   托管代理 = " + (managed.FullName ?? managed.Name));

                var authoritative = string.IsNullOrEmpty(il2cppFull) ? null : LoadType(il2cppFull);

                if (!Reflect.Usable(comp))
                {
                    RiziaLog.Raw(prefix + "  �?组件指针为空（Unity 空对象）—�?不读字段");
                    return;
                }

                
                
                
                if (authoritative == null)
                {
                    RiziaLog.Raw(prefix + "  �?权威类型 " + (il2cppFull ?? "(取不�?")
                                 + " 在托管侧解析不到 = 类型名不可解析（IL2CPP 未注�?/ 本次不可用）");
                    RiziaLog.Raw(prefix + "    ⚠ 降级：托管成员表不可用；改用原生 il2cpp 通道兜底采集（只读 getter/invoke）");
                    Il2CppNative.DumpAppearance(ptr, prefix + "  ", RiziaLog.Raw);
                    return;
                }

                var declared = authoritative;

                
                PrintMembers(comp, declared, prefix + "  · ", new[]
                {
                    "enabled", "isActiveAndEnabled", "hideFlags", "useGUILayout"
                });

                
                if (HasAnc(declared, "UnityEngine.UI.Graphic"))
                {
                    PrintMembers(comp, declared, prefix + "  · Graphic: ", new[]
                    {
                        "color", "raycastTarget", "raycastPadding", "depth", "useLegacyMeshGeneration"
                    });
                    var mat = Rd(comp, declared, "material");
                    RiziaLog.Raw(prefix + "  · Graphic: material=" + Fmt(mat));
                }

                if (HasAnc(declared, "UnityEngine.UI.Image"))
                {
                    RiziaLog.Raw(prefix + "  �?Image（背�?边框判据�?");
                    PrintMembers(comp, declared, prefix + "      ", new[]
                    {
                        "sprite", "overrideSprite", "activeSprite", "type", "preserveAspect", "fillCenter",
                        "fillMethod", "fillAmount", "fillClockwise", "fillOrigin", "useSpriteMesh",
                        "hasBorder", "pixelsPerUnitMultiplier", "pixelsPerUnit", "alphaHitTestMinimumThreshold"
                    });
                    var sp = Rd(comp, declared, "sprite");
                    if (Reflect.Usable(sp))
                    {
                        RiziaLog.Raw(prefix + "      sprite.name  = " + Q(Reflect.Str(Reflect.Prop(sp, "name"))));
                        RiziaLog.Raw(prefix + "      sprite.border= " + Fmt(Reflect.Prop(sp, "border"))
                                     + "   （x=�?y=�?z=�?w=上；九宫格边框，�?0 = 无边框）");
                        RiziaLog.Raw(prefix + "      sprite.rect  = " + Fmt(Reflect.Prop(sp, "rect"))
                                     + "  pixelsPerUnit=" + S(Reflect.Prop(sp, "pixelsPerUnit")));
                        var tex = Reflect.Prop(sp, "texture");
                        RiziaLog.Raw(prefix + "      sprite.texture.name = "
                                     + Q(Reflect.Str(Reflect.Prop(tex, "name"))));
                    }
                    else RiziaLog.Raw(prefix + "      sprite = (null / 空对�? �?�?Image 不画图，"
                                              + "�?color 仍会以白图渲染（纯色背景块）");
                }

                if (HasAnc(declared, "UnityEngine.UI.LayoutGroup"))
                {
                    RiziaLog.Raw(prefix + "  �?LayoutGroup（内边距/间距判据�?");
                    var pad = Rd(comp, declared, "padding");
                    if (Reflect.Usable(pad))
                        RiziaLog.Raw(prefix + "      padding = left=" + S(Reflect.Prop(pad, "left"))
                                     + " right=" + S(Reflect.Prop(pad, "right"))
                                     + " top=" + S(Reflect.Prop(pad, "top"))
                                     + " bottom=" + S(Reflect.Prop(pad, "bottom"))
                                     + "  (horizontal=" + S(Reflect.Prop(pad, "horizontal"))
                                     + " vertical=" + S(Reflect.Prop(pad, "vertical")) + ")");
                    else
                        RiziaLog.Raw(prefix + "      padding = (读不�?");
                    PrintMembers(comp, declared, prefix + "      ", new[]
                    {
                        "childAlignment", "isRootLayoutGroup",
                        "minWidth", "preferredWidth", "flexibleWidth",
                        "minHeight", "preferredHeight", "flexibleHeight"
                    });
                    var rc = Rd(comp, declared, "rectChildren");
                    RiziaLog.Raw(prefix + "      rectChildren.Count = " + Reflect.ListCount(rc));
                }

                if (HasAnc(declared, "UnityEngine.UI.HorizontalOrVerticalLayoutGroup"))
                {
                    PrintMembers(comp, declared, prefix + "  �?排布: ", new[]
                    {
                        "spacing", "childForceExpandWidth", "childForceExpandHeight",
                        "childControlWidth", "childControlHeight",
                        "childScaleWidth", "childScaleHeight", "reverseArrangement"
                    });
                }

                if (HasAnc(declared, "UnityEngine.UI.ContentSizeFitter"))
                {
                    PrintMembers(comp, declared, prefix + "  �?ContentSizeFitter: ", new[]
                    {
                        "horizontalFit", "verticalFit"
                    });
                }

                if (HasAnc(declared, "UnityEngine.UI.LayoutElement"))
                {
                    PrintMembers(comp, declared, prefix + "  �?LayoutElement: ", new[]
                    {
                        "ignoreLayout", "minWidth", "minHeight", "preferredWidth", "preferredHeight",
                        "flexibleWidth", "flexibleHeight", "layoutPriority"
                    });
                }

                if (HasAnc(declared, "UnityEngine.UI.RectMask2D"))
                {
                    PrintMembers(comp, declared, prefix + "  �?RectMask2D（裁剪）: ", new[]
                    {
                        "padding", "softness"
                    });
                    RiziaLog.Raw(prefix + "      canvasRect=" + Fmt(Rd(comp, declared, "canvasRect"))
                                 + " rootCanvasRect=" + Fmt(Rd(comp, declared, "rootCanvasRect")));
                }

                if (HasAnc(declared, "UnityEngine.UI.Mask") && !HasAnc(declared, "UnityEngine.UI.RectMask2D"))
                {
                    PrintMembers(comp, declared, prefix + "  �?Mask: ", new[] { "showMaskGraphic" });
                }

                if (HasAnc(declared, "UnityEngine.UI.Selectable"))
                {
                    PrintMembers(comp, declared, prefix + "  · Selectable: ", new[] { "transition", "interactable" });
                    var cb = Rd(comp, declared, "colors");
                    RiziaLog.Raw(prefix + "      colors: " + Fmt(cb));
                    var tg = Rd(comp, declared, "targetGraphic");
                    RiziaLog.Raw(prefix + "      targetGraphic = " + Fmt(tg));
                }

                if (HasAnc(declared, "UnityEngine.CanvasGroup"))
                {
                    PrintMembers(comp, declared, prefix + "  �?CanvasGroup: ", new[]
                    {
                        "alpha", "interactable", "blocksRaycasts", "ignoreParentGroups"
                    });
                }

                if (HasAnc(declared, "UnityEngine.Canvas"))
                {
                    PrintMembers(comp, declared, prefix + "  �?Canvas: ", new[]
                    {
                        "renderMode", "sortingOrder", "overrideSorting", "sortingLayerName",
                        "scaleFactor", "referencePixelsPerUnit", "isRootCanvas", "pixelPerfect", "renderOrder"
                    });
                }

                if (HasAnc(declared, "TMPro.TMP_Text"))
                {
                    RiziaLog.Raw(prefix + "  �?TMP_Text（渲染参数见 [R-TMP] 段；此处只报关键几项�?");
                    PrintMembers(comp, declared, prefix + "      ", new[]
                    {
                        "fontSize", "enableAutoSizing", "alignment", "enableWordWrapping",
                        "textWrappingMode", "overflowMode", "margin", "richText",
                        "isTextOverflowing", "isTextTruncated", "preferredWidth", "preferredHeight"
                    });
                }
            }
            catch (Exception e)
            {
                RiziaLog.Raw(prefix + "  （组件读取失败：" + e.GetType().Name + " : " + e.Message + "）");
            }
        }

        private static void DumpCanvasAncestor(object liveTmp)
        {
            try
            {
                var canvasType = LoadType("UnityEngine.Canvas");
                if (canvasType == null) return;
                object c = Reflect.Prop(liveTmp, "transform");
                for (int d = 0; d < 12 && Reflect.Usable(c); d++)
                {
                    var cgo = Reflect.Prop(c, "gameObject");
                    var cv = GetComponent(cgo, canvasType);
                    if (Reflect.Usable(cv))
                    {
                        RiziaLog.Raw("");
                        RiziaLog.Raw("  ── Canvas（第一个祖�?Canvas，层�?" + d + "）：" + Q(GoName(cgo)));
                        PrintMembers(cv, canvasType, "     ", new[]
                        {
                            "renderMode", "sortingOrder", "overrideSorting", "sortingLayerName",
                            "scaleFactor", "referencePixelsPerUnit", "isRootCanvas", "planeDistance", "renderOrder"
                        });
                        var rt = Reflect.Prop(cv, "rootCanvas");
                        RiziaLog.Raw("     rootCanvas=" + Q(GoName(Reflect.Prop(rt, "gameObject"))));
                        break;
                    }
                    c = Reflect.Prop(c, "parent");
                }
            }
            catch { }
        }

        
        private static void DumpSubtreeScan(object liveTmp)
        {
            RiziaLog.Section("�?明细块子树扫描（图标 / 其他视觉元素）—�?有界扫描");
            try
            {
                var root = Reflect.Prop(liveTmp, "transform");
                
                for (int i = 0; i < 2; i++)
                {
                    var p = Reflect.Prop(root, "parent");
                    if (!Reflect.Usable(p)) break;
                    root = p;
                }
                RiziaLog.Raw("  扫描�?: " + Q(GoName(Reflect.Prop(root, "gameObject"))));

                int visited = 0, images = 0, tmps = 0, sprites = 0, canvases = 0;
                var sb = new StringBuilder();
                WalkSubtree(root, 0, 0, ref visited, ref images, ref tmps, ref sprites, ref canvases, sb);
                RiziaLog.Raw(sb.ToString());
                RiziaLog.Raw("  小结 : 节点=" + visited + "  Image=" + images
                             + "（其中有精灵=" + sprites + "�? TMP=" + tmps + "  Canvas=" + canvases);
                RiziaLog.Raw("  判据 : ★ Image>0 且其中「有精灵」的个数 > 0 → 明细区域**确实有图片**。"
                             + "若 Image 全是「无精灵的纯色块」→ 那只是背景/分隔线。");
            }
            catch (Exception e)
            {
                RiziaLog.Raw("  （子树扫描失败：" + e.GetType().Name + " : " + e.Message + "）");
            }
        }

        private static void WalkSubtree(object tr, int depth, int indent, ref int visited, ref int images,
            ref int tmps, ref int sprites, ref int canvases, StringBuilder sb)
        {
            if (!Reflect.Usable(tr) || depth > 5 || visited >= 80) return;
            visited++;
            var pad = new string(' ', 2 + indent * 2);
            try
            {
                var go = Reflect.Prop(tr, "gameObject");
                sb.Append(pad).Append("· ").Append(GoName(go))
                  .Append("  active=").Append(S(Reflect.Prop(go, "activeInHierarchy")))
                  .Append("  rect=").Append(RectStr(tr)).Append('\n');

                var comps = ComponentsOf(go, out string how);
                if (comps != null)
                {
                    for (int i = 0; i < comps.Count; i++)
                    {
                        var c = comps[i];
                        if (c == null) continue;
                        var cptr = Reflect.Ptr(c);
                        var full = Il2CppFullName(cptr) ?? (c.GetType().FullName ?? c.GetType().Name);
                        
                        
                        var d = LoadType(full);
                        var native = d == null;

                        if (IsA(cptr, d, "UnityEngine.Canvas")) canvases++;
                        if (IsA(cptr, d, "UnityEngine.UI.Image"))
                        {
                            images++;
                            var spObj = native ? null : Rd(c, d, "sprite");
                            var spPtr = native ? Il2CppNative.MemberRef(cptr, "sprite") : Reflect.Ptr(spObj);
                            string sn = null;
                            if (spPtr != IntPtr.Zero)
                            {
                                sprites++;
                                sn = native ? Il2CppNative.Member(spPtr, "name") : Reflect.Str(Reflect.Prop(spObj, "name"));
                            }
                            sb.Append(pad).Append("    [Image] ").Append(full)
                              .Append(native ? "（原生通道）" : "")
                              .Append("  color=").Append(native ? Il2CppNative.Member(cptr, "color") : ColorStr(Rd(c, d, "color")))
                              .Append("  sprite=").Append(sn == null ? "(�?" : Q(sn))
                              .Append("  hasBorder=").Append(native ? Il2CppNative.Member(cptr, "hasBorder") : S(Rd(c, d, "hasBorder")))
                              .Append('\n');
                        }
                        else if (IsA(cptr, d, "TMPro.TMP_Text"))
                        {
                            tmps++;
                            var txt = native ? Il2CppNative.MemberString(cptr, "text") : Reflect.Str(Reflect.Prop(c, "text"));
                            sb.Append(pad).Append("    [TMP] ").Append(full)
                              .Append(native ? "（原生通道）" : "")
                              .Append("  len=").Append(txt == null ? -1 : txt.Length)
                              .Append("  sha=").Append(Short(RiziaLog.Sha256Hex(txt)))
                              .Append("  text=").Append(Trunc(RiziaLog.Esc(txt), 90))
                              .Append('\n');
                        }
                        else if (IsA(cptr, d, "UnityEngine.UI.Graphic"))
                        {
                            sb.Append(pad).Append("    [Graphic] ").Append(full)
                              .Append(native ? "（原生通道）" : "")
                              .Append("  color=").Append(native ? Il2CppNative.Member(cptr, "color") : ColorStr(Rd(c, d, "color"))).Append('\n');
                        }
                        else if (!IsA(cptr, d, "UnityEngine.Transform") && !IsA(cptr, d, "UnityEngine.RectTransform")
                                 && !IsA(cptr, d, "UnityEngine.CanvasRenderer"))
                        {
                            sb.Append(pad).Append("    [其他] ").Append(full).Append('\n');
                        }
                    }
                }

                int cc = Reflect.ToInt(Reflect.Prop(tr, "childCount"));
                for (int i = 0; i < cc && visited < 80; i++)
                {
                    var ch = Reflect.Call1(tr, "GetChild", i);
                    if (!Reflect.Usable(ch)) continue;
                    WalkSubtree(ch, depth + 1, indent + 1, ref visited, ref images, ref tmps,
                        ref sprites, ref canvases, sb);
                }
            }
            catch (Exception e)
            {
                sb.Append(pad).Append("（子树节点读取失败：").Append(e.GetType().Name).Append("）\n");
            }
        }

        
        
        

        private static void DumpTmpParams(object tmp, string label, string when)
        {
            if (!Reflect.Usable(tmp)) return;
            var key = "TMP|" + label;
            bool first = RiziaLog.Once(key);
            if (!first) return;   
            var il2cppFull = Il2CppFullName(Reflect.Ptr(tmp));
            var d = (il2cppFull == null ? null : LoadType(il2cppFull)) ?? tmp.GetType();
            if (!HasAnc(d, "TMPro.TMP_Text")) d = LoadType("TMPro.TMP_Text") ?? d;

            _tmpParamsDone.Add(label);
            RiziaLog.Section("【TMP 渲染参数" + label + "（首次读到时写一次；when=" + when + "）");
            RiziaLog.Raw("  权威类型   : " + (il2cppFull ?? "?") + "   托管代理=" + (tmp.GetType().FullName ?? tmp.GetType().Name));
            PrintMembers(tmp, d, "  ", new[] { "fontSize", "enableAutoSizing", "fontSizeMin", "fontSizeMax" });
            PrintMembers(tmp, d, "  ", new[] { "alignment", "horizontalAlignment", "verticalAlignment" });
            RiziaLog.Raw("  �?折行/溢出（决定「多宽会折行」）:");
            PrintMembers(tmp, d, "    ", new[]
            {
                "enableWordWrapping", "textWrappingMode", "wordWrappingRatios",
                "overflowMode", "isTextOverflowing", "isTextTruncated", "firstOverflowCharacterIndex"
            });
            RiziaLog.Raw("  �?margin（Vector4，全�?x=�?y=�?z=�?w=上）:");
            RiziaLog.Raw("    margin = " + Fmt(Rd(tmp, d, "margin")));
            PrintMembers(tmp, d, "  ", new[]
            {
                "marginLeft", "marginRight", "marginWidth", "marginHeight",
                "preferredWidth", "preferredHeight", "renderedWidth", "renderedHeight"
            });
            RiziaLog.Raw("  【几何 / 尺寸】" + label + "：");
            var rt = Rd(tmp, d, "rectTransform");
            if (!Reflect.Usable(rt)) rt = Reflect.Prop(tmp, "transform");
            RiziaLog.Raw("    rectTransform.rect = " + RectStr(rt));
            PrintMembers(tmp, d, "  ", new[]
            {
                "richText", "parseCtrlCharacters", "enableKerning", "extraPadding",
                "characterSpacing", "wordSpacing", "lineSpacing", "paragraphSpacing",
                "isRightToLeftText", "autoSizeTextContainer", "raycastTarget",
                "color", "alpha", "maxVisibleLines", "firstVisibleCharacter", "maxVisibleCharacters"
            });
            var f = Rd(tmp, d, "font");
            RiziaLog.Raw("  font = " + Q(Reflect.Str(Reflect.Prop(f, "name"))));
            var ti = Rd(tmp, d, "textInfo");
            if (Reflect.Usable(ti))
                RiziaLog.Raw("  textInfo: lineCount=" + S(Reflect.Prop(ti, "lineCount"))
                             + " characterCount=" + S(Reflect.Prop(ti, "characterCount"))
                             + " wordCount=" + S(Reflect.Prop(ti, "wordCount"))
                             + " spriteCount=" + S(Reflect.Prop(ti, "spriteCount"))
                             + "  ★ lineCount>1 = 当前确实折了行。");
            RiziaLog.Raw("  textBounds = " + Fmt(Rd(tmp, d, "textBounds")));
            var txt = Reflect.Str(Reflect.Prop(tmp, "text"));
            RiziaLog.Raw("  当前 text : 长度=" + (txt == null ? -1 : txt.Length)
                         + "  sha256=" + RiziaLog.Sha256Hex(txt));
            RiziaLog.Raw("  当前 text 内容: " + RiziaLog.Esc(txt));
            bool hasSpriteTag = txt != null && txt.IndexOf("<sprite", StringComparison.OrdinalIgnoreCase) >= 0;
            RiziaLog.Raw("  ★ 文本里是否含内联图标标签 <sprite ...> : " + (hasSpriteTag ? "是（明细里有内联图标）" : "否"));
            RiziaLog.Raw("  （⑥ 子树扫描另会报出独立�?Image 图标；两者合起来回答「有没有图标」）");
        }

        
        

        private static List<object> ComponentsOf(object go, out string how)
        {
            how = "?";
            if (!Reflect.Usable(go)) return null;

            var byIndex = ComponentsByIndex(go);
            if (byIndex != null) { how = "①GetComponentCount+GetComponentAtIndex（无 Type 参数，最稳）"; return byIndex; }

            var ct = LoadType("UnityEngine.Component");
            var byIl2 = ComponentsByTypeArg(go, ToIl2CppType(ct));
            if (byIl2 != null) { how = "②GetComponents(Il2CppSystem.Type)"; return byIl2; }

            var byGen = ComponentsByGeneric(go, ct);
            if (byGen != null) { how = "③泛�?GetComponents<T>()"; return byGen; }

            var byMono = ComponentsByTypeArg(go, ct);
            if (byMono != null) { how = "④GetComponents(System.Type)"; return byMono; }

            return null;
        }

        private static List<object> ComponentsByIndex(object go)
        {
            try
            {
                var cm = FindMethod(go.GetType(), "GetComponentCount", 0);
                var am = FindMethod(go.GetType(), "GetComponentAtIndex", 1);
                if (cm == null || am == null) return null;
                if (am.GetParameters()[0].ParameterType != typeof(int)) return null;
                var cv = cm.Invoke(go, null);
                if (!(cv is int)) return null;
                int n = (int)cv;
                if (n <= 0) return null;
                var list = new List<object>(n);
                for (int i = 0; i < n; i++) list.Add(am.Invoke(go, new object[] { i }));
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
                    var list = ToObjectList(m.Invoke(go, new object[] { typeArg }));
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
                    var list = ToObjectList(m.MakeGenericMethod(compType).Invoke(go, null));
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
                var l = new List<object>();
                foreach (var o in en) l.Add(o);
                return l;
            }
            catch { return null; }
        }

        private static object GetComponent(object go, Type compType)
        {
            if (!Reflect.Usable(go) || compType == null) return null;
            try
            {
                foreach (var m in go.GetType().GetMethods(All))
                {
                    if (m.Name != "GetComponent") continue;
                    var ps = m.GetParameters();
                    if (ps.Length != 1) continue;
                    if (!ps[0].ParameterType.IsInstanceOfType(compType)) continue;
                    var r = m.Invoke(go, new object[] { compType });
                    if (Reflect.Usable(r)) return r;
                }
            }
            catch { }
            var byIl2 = GetComponentByTypeArg(go, ToIl2CppType(compType));
            return byIl2;
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

        private static MethodInfo FindMethod(Type t, string name, int paramCount)
        {
            if (t == null) return null;
            try
            {
                foreach (var m in t.GetMethods(All))
                    if (m.Name == name && m.GetParameters().Length == paramCount) return m;
            }
            catch { }
            return null;
        }

        private static object ToIl2CppType(Type t)
        {
            if (t == null) return null;
            try
            {
                
                
                var asm = typeof(Il2CppInterop.Runtime.IL2CPP).Assembly;
                var it = asm == null ? null : asm.GetType("Il2CppInterop.Runtime.Il2CppType");
                if (it == null) return null;
                foreach (var m in it.GetMethods(BindingFlags.Public | BindingFlags.Static))
                {
                    if (m.Name != "From") continue;
                    var ps = m.GetParameters();
                    if (ps.Length >= 1 && ps[0].ParameterType == typeof(Type))
                        return m.Invoke(null, m.GetParameters().Length == 1
                            ? new object[] { t } : new object[] { t, false });
                }
            }
            catch { }
            return null;
        }

        

        private static string Il2CppFullName(IntPtr ptr)
        {
            if (ptr == IntPtr.Zero) return null;
            
            
            if (!ProbeNativeGuard.NativeAllowed) { ProbeNativeGuard.Note("RiziaProbe.Il2CppFullName"); return null; }
            try
            {
                var klass = Il2Cpp.il2cpp_object_get_class(ptr);
                if (klass == IntPtr.Zero) return null;
                var type = Il2Cpp.il2cpp_class_get_type(klass);
                if (type != IntPtr.Zero)
                {
                    var n = Il2Cpp.il2cpp_type_get_name_(type);
                    if (!string.IsNullOrEmpty(n)) return n;
                }
                var ns = Il2Cpp.il2cpp_class_get_namespace_(klass);
                var nm = Il2Cpp.il2cpp_class_get_name_(klass);
                if (string.IsNullOrEmpty(nm)) return null;
                return string.IsNullOrEmpty(ns) ? nm : ns + "." + nm;
            }
            catch { return null; }
        }

        private static string Il2CppAsmName(IntPtr ptr)
        {
            if (ptr == IntPtr.Zero) return null;
            
            if (!ProbeNativeGuard.NativeAllowed) { ProbeNativeGuard.Note("RiziaProbe.Il2CppAsmName"); return null; }
            try
            {
                var klass = Il2Cpp.il2cpp_object_get_class(ptr);
                if (klass == IntPtr.Zero) return null;
                return Il2Cpp.il2cpp_class_get_assemblyname_(klass);
            }
            catch { return null; }
        }

        private static string ManagedName(object o)
        {
            try { return o == null ? "(null)" : (o.GetType().FullName ?? o.GetType().Name); }
            catch { return "?"; }
        }

        

        
        
        
        
        private static bool IsA(IntPtr ptr, Type declared, string fullName)
        {
            if (declared != null && HasAnc(declared, fullName)) return true;
            return Il2CppNative.HasAncestor(ptr, fullName);
        }

        private static bool HasAnc(Type t, string fullName)
        {
            try
            {
                for (var c = t; c != null; c = c.BaseType)
                    if (string.Equals(c.FullName, fullName, StringComparison.Ordinal)) return true;
            }
            catch { }
            return false;
        }

        
        private static object Rd(object target, Type declared, string member)
        {
            if (target == null || declared == null || string.IsNullOrEmpty(member)) return null;
            if (member.StartsWith("Native", StringComparison.Ordinal)) return null;
            for (var t = declared; t != null; t = t.BaseType)
            {
                try
                {
                    var ps = t.GetProperties(All);
                    foreach (var p in ps)
                    {
                        if (p.Name != member) continue;
                        if (!p.CanRead) continue;
                        if (p.GetIndexParameters().Length != 0) continue;
                        return p.GetValue(target);
                    }
                }
                catch { }
                try
                {
                    var f = t.GetField(member, All);
                    if (f != null) return f.GetValue(target);
                }
                catch { }
                try
                {
                    var m = t.GetMethod("get_" + member, All, null, Type.EmptyTypes, null);
                    if (m != null) return m.Invoke(target, null);
                }
                catch { }
            }
            return null;
        }

        private static void PrintMembers(object target, Type declared, string prefix, string[] members)
        {
            var sb = new StringBuilder();
            foreach (var m in members)
            {
                object v;
                try { v = Rd(target, declared, m); } catch { v = null; }
                sb.Append(prefix).Append(m).Append(" = ").Append(Fmt(v)).Append('\n');
            }
            if (sb.Length > 0) RiziaLog.Raw(sb.ToString().TrimEnd('\n'));
        }

        
        

        private static string RectStr(object tr)
        {
            try
            {
                if (!Reflect.Usable(tr)) return "(�?RectTransform)";
                var r = Reflect.Prop(tr, "rect");
                if (r == null) return "(rect 读不�?";
                return "x=" + Fmt(Reflect.Prop(r, "x")) + " y=" + Fmt(Reflect.Prop(r, "y"))
                     + " ★width=" + Fmt(Reflect.Prop(r, "width"))
                     + " ★height=" + Fmt(Reflect.Prop(r, "height"))
                     + "  sizeDelta=" + Fmt(Reflect.Prop(tr, "sizeDelta"));
            }
            catch (Exception e) { return "(rect 异常:" + e.GetType().Name + ")"; }
        }

        private static string Fmt(object v)
        {
            if (v == null) return "(null)";
            try
            {
                if (v is string) return Q((string)v);
                var t = v.GetType();
                var fn = t.FullName ?? t.Name;

                if (v is bool || v is int || v is long || v is float || v is double
                    || v is uint || v is short || v is byte) return Convert.ToString(v, CultureInfo.InvariantCulture);
                if (t.IsEnum) return v.ToString();

                if (fn == "UnityEngine.Rect")
                    return "(x=" + Fmt(Reflect.Prop(v, "x")) + " y=" + Fmt(Reflect.Prop(v, "y"))
                         + " width=" + Fmt(Reflect.Prop(v, "width"))
                         + " height=" + Fmt(Reflect.Prop(v, "height")) + ")";
                if (fn == "UnityEngine.Vector4")
                    return "(x=" + Fmt(Reflect.Prop(v, "x")) + " y=" + Fmt(Reflect.Prop(v, "y"))
                         + " z=" + Fmt(Reflect.Prop(v, "z")) + " w=" + Fmt(Reflect.Prop(v, "w")) + ")";
                if (fn == "UnityEngine.Vector3")
                    return "(" + Fmt(Reflect.Prop(v, "x")) + ", " + Fmt(Reflect.Prop(v, "y"))
                         + ", " + Fmt(Reflect.Prop(v, "z")) + ")";
                if (fn == "UnityEngine.Vector2" || fn == "UnityEngine.Vector2Int")
                    return "(" + Fmt(Reflect.Prop(v, "x")) + ", " + Fmt(Reflect.Prop(v, "y")) + ")";
                if (fn == "UnityEngine.Color" || fn == "UnityEngine.Color32")
                    return ColorStr(v);
                if (fn == "UnityEngine.Bounds")
                    return "(center=" + Fmt(Reflect.Prop(v, "center")) + " size=" + Fmt(Reflect.Prop(v, "size")) + ")";
                if (fn == "UnityEngine.UI.ColorBlock")
                    return "(normal=" + ColorStr(Reflect.Prop(v, "normalColor"))
                         + " highlighted=" + ColorStr(Reflect.Prop(v, "highlightedColor"))
                         + " pressed=" + ColorStr(Reflect.Prop(v, "pressedColor"))
                         + " selected=" + ColorStr(Reflect.Prop(v, "selectedColor"))
                         + " disabled=" + ColorStr(Reflect.Prop(v, "disabledColor"))
                         + " fadeDuration=" + Fmt(Reflect.Prop(v, "fadeDuration")) + ")";

                var b = v as Il2CppInterop.Runtime.InteropTypes.Il2CppObjectBase;
                if (b != null)
                {
                    if (b.Pointer == IntPtr.Zero) return "(Unity 空对�?";
                    var il = Il2CppFullName(b.Pointer);
                    var nm = Reflect.Str(Reflect.Prop(v, "name"));
                    var s = "<" + (il ?? t.Name) + ">";
                    if (nm != null) s += " name=" + Q(nm);
                    return s;
                }

                return "(" + (fn) + ") " + SafeToString(v);
            }
            catch { return "(格式异常)"; }
        }

        private static string SafeToString(object v)
        {
            try { return v.ToString(); } catch { return "?"; }
        }

        private static string ColorStr(object c)
        {
            if (c == null) return "(null)";
            try
            {
                var r = Reflect.Prop(c, "r");
                var g = Reflect.Prop(c, "g");
                var b = Reflect.Prop(c, "b");
                var a = Reflect.Prop(c, "a");
                string hex = "#" + ByteHex(r) + ByteHex(g) + ByteHex(b) + ByteHex(a);
                return hex + " (r=" + Fmt(r) + " g=" + Fmt(g) + " b=" + Fmt(b) + " a=" + Fmt(a) + ")";
            }
            catch { return "(颜色读取失败)"; }
        }

        private static string ByteHex(object v)
        {
            try
            {
                double d = Convert.ToDouble(v, CultureInfo.InvariantCulture);
                int i = (int)Math.Round(d <= 1.0000001 && d >= 0 ? d * 255.0 : d);
                if (i < 0) i = 0; if (i > 255) i = 255;
                return i.ToString("x2", CultureInfo.InvariantCulture);
            }
            catch { return "??"; }
        }

        private static string SpriteStr(object sp)
        {
            if (!Reflect.Usable(sp)) return "(null / 无精�?";
            return Q(Reflect.Str(Reflect.Prop(sp, "name")))
                 + " border=" + Fmt(Reflect.Prop(sp, "border"))
                 + " rect=" + Fmt(Reflect.Prop(sp, "rect"));
        }

        private static string TmpOneLine(object tmp)
        {
            if (!Reflect.Usable(tmp)) return "(未接�?/ 不可�?";
            var s = Reflect.Str(Reflect.Prop(tmp, "text"));
            return "len=" + (s == null ? -1 : s.Length) + " sha=" + Short(RiziaLog.Sha256Hex(s))
                 + " 内容=" + Trunc(RiziaLog.Esc(s), 160);
        }

        private static object ListAt(object list, int i)
        {
            if (!Reflect.Usable(list)) return null;
            try
            {
                var t = list.GetType();
                foreach (var m in t.GetMethods(All))
                {
                    if (m.Name != "get_Item") continue;
                    var ps = m.GetParameters();
                    if (ps.Length == 1 && ps[0].ParameterType == typeof(int))
                        return m.Invoke(list, new object[] { i });
                }
                var p = t.GetProperty("Item", All);
                if (p != null) return p.GetValue(list, new object[] { i });
            }
            catch { }
            return null;
        }

        private static string GoName(object go)
        {
            try
            {
                if (!Reflect.Usable(go)) return "(�?GameObject)";
                return Reflect.Str(Reflect.Prop(go, "name")) ?? "(无名)";
            }
            catch { return "(读名失败)"; }
        }

        private static string S(object o)
        {
            try
            {
                if (o == null) return "(null)";
                if (o is bool) return ((bool)o) ? "true" : "false";
                return Convert.ToString(o, CultureInfo.InvariantCulture) ?? "(null)";
            }
            catch { return "?"; }
        }

        private static string Q(string s)
        {
            return s == null ? "(null)" : "\"" + RiziaLog.Esc(s) + "\"  [len=" + s.Length + "]";
        }

        private static string Trunc(string s, int max)
        {
            if (s == null) return "(null)";
            return s.Length <= max ? s : s.Substring(0, max) + "�?+" + (s.Length - max) + ")";
        }

        private static string Short(string sha)
        {
            if (string.IsNullOrEmpty(sha)) return "?";
            return sha.Length <= 16 ? sha : sha.Substring(0, 16);
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
                    if (dt != null && dt.Name == "RiziaProbe") continue;
                    sb.Append(dt == null ? "?" : dt.Name).Append('.').Append(m.Name);
                    if (++c < n) sb.Append(" < ");
                }
                return sb.Length == 0 ? "(无托管帧 —�?调用方在 IL2CPP 原生�?" : sb.ToString();
            }
            catch (Exception e) { return "(栈不可读:" + e.GetType().Name + ")"; }
        }
    }
}
