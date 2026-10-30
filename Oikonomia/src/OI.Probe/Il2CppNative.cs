using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
using Il2Cpp = Il2CppInterop.Runtime.IL2CPP;

namespace Oikonomia.Probe
{
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    internal static class Il2CppNative
    {
        
        private const int T_VOID = 0x01, T_BOOLEAN = 0x02, T_CHAR = 0x03, T_I1 = 0x04, T_U1 = 0x05,
                          T_I2 = 0x06, T_U2 = 0x07, T_I4 = 0x08, T_U4 = 0x09, T_I8 = 0x0a, T_U8 = 0x0b,
                          T_R4 = 0x0c, T_R8 = 0x0d, T_STRING = 0x0e, T_PTR = 0x0f, T_VALUETYPE = 0x11,
                          T_CLASS = 0x12, T_ARRAY = 0x14, T_OBJECT = 0x1c, T_SZARRAY = 0x1d;

        private static readonly object _lock = new object();
        private static string[] _imageNames;                       
        private static readonly Dictionary<string, IntPtr> _klassCache = new Dictionary<string, IntPtr>(StringComparer.Ordinal);
        private static readonly HashSet<string> _missingLogged = new HashSet<string>(StringComparer.Ordinal);

        
        
        

        
        
        
        
        public static bool Available { get { return ProbeNativeGuard.NativeAllowed; } }

        
        public static string[] ImageNames()
        {
            if (!ProbeNativeGuard.NativeAllowed) { ProbeNativeGuard.Note("Il2CppNative.ImageNames"); return new string[0]; }
            lock (_lock)
            {
                if (_imageNames != null) return _imageNames;
            }
            var list = new List<string>();
            try
            {
                var dom = Il2Cpp.il2cpp_domain_get();
                if (dom != IntPtr.Zero)
                {
                    uint n = 0;
                    unsafe
                    {
                        IntPtr* arr = Il2Cpp.il2cpp_domain_get_assemblies(dom, ref n);
                        if (arr != null && n > 0 && n < 4096)          
                        {
                            for (uint i = 0; i < n; i++)
                            {
                                var asm = arr[i];
                                if (asm == IntPtr.Zero) continue;
                                var img = Il2Cpp.il2cpp_assembly_get_image(asm);
                                if (img == IntPtr.Zero) continue;
                                var nm = Il2Cpp.il2cpp_image_get_name_(img);
                                if (!string.IsNullOrEmpty(nm)) list.Add(nm);
                            }
                        }
                    }
                }
            }
            catch { }

            var res = list.ToArray();
            lock (_lock) { if (_imageNames == null) _imageNames = res; return _imageNames; }
        }

        
        public static bool HasImage(string name)
        {
            if (string.IsNullOrEmpty(name)) return false;
            var want = name.EndsWith(".dll", StringComparison.OrdinalIgnoreCase)
                ? name.Substring(0, name.Length - 4) : name;
            foreach (var n in ImageNames())
            {
                if (string.Equals(n, want, StringComparison.OrdinalIgnoreCase)) return true;
                if (n != null && n.EndsWith(".dll", StringComparison.OrdinalIgnoreCase)
                    && string.Equals(n.Substring(0, n.Length - 4), want, StringComparison.OrdinalIgnoreCase)) return true;
            }
            return false;
        }

        
        
        
        
        public static int InteropImageCount()
        {
            try
            {
                var t = typeof(Il2Cpp).GetMethod("GetIl2CppImages",
                    System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic
                    | System.Reflection.BindingFlags.Public);
                if (t == null) return -3;
                var arr = t.Invoke(null, null) as Array;
                return arr == null ? -1 : arr.Length;
            }
            catch { return -2; }
        }

        
        
        

        
        public static IntPtr FindKlass(string ns, string name)
        {
            if (string.IsNullOrEmpty(name)) return IntPtr.Zero;
            if (!ProbeNativeGuard.NativeAllowed) { ProbeNativeGuard.Note("Il2CppNative.FindKlass"); return IntPtr.Zero; }
            var key = (ns ?? "") + "|" + name;
            lock (_lock)
            {
                IntPtr c;
                if (_klassCache.TryGetValue(key, out c)) return c;
            }

            IntPtr found = IntPtr.Zero;
            try
            {
                var dom = Il2Cpp.il2cpp_domain_get();
                if (dom != IntPtr.Zero)
                {
                    uint n = 0;
                    unsafe
                    {
                        IntPtr* arr = Il2Cpp.il2cpp_domain_get_assemblies(dom, ref n);
                        if (arr != null && n > 0 && n < 4096)
                        {
                            for (uint i = 0; i < n && found == IntPtr.Zero; i++)
                            {
                                var asm = arr[i];
                                if (asm == IntPtr.Zero) continue;
                                var img = Il2Cpp.il2cpp_assembly_get_image(asm);
                                if (img == IntPtr.Zero) continue;
                                try { found = Il2Cpp.il2cpp_class_from_name(img, ns ?? "", name); }
                                catch { found = IntPtr.Zero; }
                            }
                        }
                    }
                }
            }
            catch { found = IntPtr.Zero; }

            lock (_lock) { _klassCache[key] = found; }
            return found;
        }

        
        public static IntPtr KlassOf(IntPtr obj)
        {
            if (obj == IntPtr.Zero) return IntPtr.Zero;
            if (!ProbeNativeGuard.NativeAllowed) { ProbeNativeGuard.Note("Il2CppNative.KlassOf"); return IntPtr.Zero; }
            try { return Il2Cpp.il2cpp_object_get_class(obj); }
            catch { return IntPtr.Zero; }
        }

        
        public static string KlassName(IntPtr klass)
        {
            if (klass == IntPtr.Zero) return null;
            if (!ProbeNativeGuard.NativeAllowed) { ProbeNativeGuard.Note("Il2CppNative.KlassName"); return null; }
            try
            {
                var ns = Il2Cpp.il2cpp_class_get_namespace_(klass);
                var nm = Il2Cpp.il2cpp_class_get_name_(klass);
                if (string.IsNullOrEmpty(nm)) return null;
                return string.IsNullOrEmpty(ns) ? nm : ns + "." + nm;
            }
            catch { return null; }
        }

        
        public static string NativeTypeName(IntPtr obj)
        {
            return KlassName(KlassOf(obj));
        }

        
        private static readonly Dictionary<string, string[]> AncestorMap = new Dictionary<string, string[]>(StringComparer.Ordinal)
        {
            { "UnityEngine.Component",                            new[] { "UnityEngine", "Component" } },
            { "UnityEngine.Behaviour",                            new[] { "UnityEngine", "Behaviour" } },
            { "UnityEngine.MonoBehaviour",                        new[] { "UnityEngine", "MonoBehaviour" } },
            { "UnityEngine.Transform",                            new[] { "UnityEngine", "Transform" } },
            { "UnityEngine.RectTransform",                        new[] { "UnityEngine", "RectTransform" } },
            { "UnityEngine.CanvasRenderer",                       new[] { "UnityEngine", "CanvasRenderer" } },
            { "UnityEngine.Sprite",                               new[] { "UnityEngine", "Sprite" } },
            { "UnityEngine.RectOffset",                           new[] { "UnityEngine", "RectOffset" } },
            { "UnityEngine.CanvasGroup",                          new[] { "UnityEngine", "CanvasGroup" } },
            { "UnityEngine.Canvas",                               new[] { "UnityEngine", "Canvas" } },
            { "UnityEngine.UI.Graphic",                           new[] { "UnityEngine.UI", "Graphic" } },
            { "UnityEngine.UI.MaskableGraphic",                   new[] { "UnityEngine.UI", "MaskableGraphic" } },
            { "UnityEngine.UI.Image",                             new[] { "UnityEngine.UI", "Image" } },
            { "UnityEngine.UI.RawImage",                          new[] { "UnityEngine.UI", "RawImage" } },
            { "UnityEngine.UI.Text",                              new[] { "UnityEngine.UI", "Text" } },
            { "UnityEngine.UI.LayoutGroup",                       new[] { "UnityEngine.UI", "LayoutGroup" } },
            { "UnityEngine.UI.HorizontalOrVerticalLayoutGroup",   new[] { "UnityEngine.UI", "HorizontalOrVerticalLayoutGroup" } },
            { "UnityEngine.UI.HorizontalLayoutGroup",             new[] { "UnityEngine.UI", "HorizontalLayoutGroup" } },
            { "UnityEngine.UI.VerticalLayoutGroup",               new[] { "UnityEngine.UI", "VerticalLayoutGroup" } },
            { "UnityEngine.UI.GridLayoutGroup",                   new[] { "UnityEngine.UI", "GridLayoutGroup" } },
            { "UnityEngine.UI.ContentSizeFitter",                 new[] { "UnityEngine.UI", "ContentSizeFitter" } },
            { "UnityEngine.UI.LayoutElement",                     new[] { "UnityEngine.UI", "LayoutElement" } },
            { "UnityEngine.UI.RectMask2D",                        new[] { "UnityEngine.UI", "RectMask2D" } },
            { "UnityEngine.UI.Mask",                              new[] { "UnityEngine.UI", "Mask" } },
            { "UnityEngine.UI.Selectable",                        new[] { "UnityEngine.UI", "Selectable" } },
            { "UnityEngine.UI.Button",                            new[] { "UnityEngine.UI", "Button" } },
            { "UnityEngine.EventSystems.EventTrigger",            new[] { "UnityEngine.EventSystems", "EventTrigger" } },
            { "TMPro.TMP_Text",                                   new[] { "TMPro", "TMP_Text" } },
            { "TMPro.TextMeshProUGUI",                            new[] { "TMPro", "TextMeshProUGUI" } },
            { "TMPro.TextMeshPro",                                new[] { "TMPro", "TextMeshPro" } }
        };

        
        
        
        
        public static bool HasAncestor(IntPtr obj, string managedFullName)
        {
            var klass = KlassOf(obj);
            return KlassIs(klass, managedFullName);
        }

        
        public static bool KlassIs(IntPtr klass, string managedFullName)
        {
            if (klass == IntPtr.Zero || string.IsNullOrEmpty(managedFullName)) return false;
            if (!ProbeNativeGuard.NativeAllowed) { ProbeNativeGuard.Note("Il2CppNative.KlassIs"); return false; }
            string[] pair;
            if (!AncestorMap.TryGetValue(managedFullName, out pair))
            {
                lock (_lock)
                {
                    if (_missingLogged.Add(managedFullName))
                        ProbeLog.Line("[原生] 谱系表无此名（按 ✗ 处理）：" + managedFullName);
                }
                return false;
            }
            var target = FindKlass(pair[0], pair[1]);
            if (target == IntPtr.Zero) return false;
            try
            {
                if (klass == target) return true;
                return Il2Cpp.il2cpp_class_is_subclass_of(klass, target, false);
            }
            catch { return false; }
        }

        
        
        

        
        
        
        
        public static bool TryMember(IntPtr obj, string member, out string display, out IntPtr refObj)
        {
            display = null; refObj = IntPtr.Zero;
            if (obj == IntPtr.Zero || string.IsNullOrEmpty(member)) return false;
            if (!ProbeNativeGuard.NativeAllowed) { ProbeNativeGuard.Note("Il2CppNative.TryMember"); return false; }
            try
            {
                var klass = KlassOf(obj);
                if (klass == IntPtr.Zero) return false;

                
                var m = Il2Cpp.il2cpp_class_get_method_from_name(klass, "get_" + member, 0);
                if (m != IntPtr.Zero)
                {
                    string d; IntPtr r;
                    if (TryInvoke(obj, m, out d, out r)) { display = d; refObj = r; return true; }
                }

                
                var f = Il2Cpp.il2cpp_class_get_field_from_name(klass, member);
                if (f != IntPtr.Zero)
                {
                    string d; IntPtr r;
                    if (TryReadField(obj, f, out d, out r)) { display = d; refObj = r; return true; }
                }
            }
            catch { }
            return false;
        }

        
        public static string Member(IntPtr obj, string member)
        {
            string d; IntPtr r;
            return TryMember(obj, member, out d, out r) ? d : "(读不到)";
        }

        
        public static IntPtr MemberRef(IntPtr obj, string member)
        {
            string d; IntPtr r;
            return TryMember(obj, member, out d, out r) ? r : IntPtr.Zero;
        }

        
        public static string MemberString(IntPtr obj, string member)
        {
            if (obj == IntPtr.Zero || string.IsNullOrEmpty(member)) return null;
            if (!ProbeNativeGuard.NativeAllowed) { ProbeNativeGuard.Note("Il2CppNative.MemberString"); return null; }
            try
            {
                var klass = KlassOf(obj);
                if (klass == IntPtr.Zero) return null;
                var m = Il2Cpp.il2cpp_class_get_method_from_name(klass, "get_" + member, 0);
                if (m == IntPtr.Zero) return null;
                IntPtr ret, exc = IntPtr.Zero;
                unsafe { ret = Il2Cpp.il2cpp_runtime_invoke(m, obj, null, ref exc); }
                if (exc != IntPtr.Zero || ret == IntPtr.Zero) return null;
                var rt = Il2Cpp.il2cpp_method_get_return_type(m);
                if (rt == IntPtr.Zero || Il2Cpp.il2cpp_type_get_type(rt) != T_STRING) return null;
                return Il2Cpp.Il2CppStringToManaged(ret);
            }
            catch { return null; }
        }

        private static bool TryInvoke(IntPtr obj, IntPtr method, out string display, out IntPtr refObj)
        {
            display = null; refObj = IntPtr.Zero;
            IntPtr ret, exc = IntPtr.Zero;
            try
            {
                
                unsafe { ret = Il2Cpp.il2cpp_runtime_invoke(method, obj, null, ref exc); }
            }
            catch { return false; }
            if (exc != IntPtr.Zero) return false;                 
            try
            {
                var rt = Il2Cpp.il2cpp_method_get_return_type(method);
                if (rt == IntPtr.Zero) return false;
                
                return Interpret(rt, ret, true, out display, out refObj);
            }
            catch { return false; }
        }

        private static bool TryReadField(IntPtr obj, IntPtr field, out string display, out IntPtr refObj)
        {
            display = null; refObj = IntPtr.Zero;
            try
            {
                var ft = Il2Cpp.il2cpp_field_get_type(field);
                if (ft == IntPtr.Zero) return false;
                var kind = Il2Cpp.il2cpp_type_get_type(ft);
                var boxed = Il2Cpp.il2cpp_field_get_value_object(field, obj);
                if (boxed == IntPtr.Zero) { display = "(null)"; return true; }

                if (IsPrimitive(kind))
                {
                    var raw = Il2Cpp.il2cpp_object_unbox(boxed);   
                    if (raw == IntPtr.Zero) return false;
                    return Interpret(ft, raw, false, out display, out refObj);
                }
                return Interpret(ft, boxed, false, out display, out refObj);
            }
            catch { return false; }
        }

        private static bool IsPrimitive(int kind)
        {
            return kind == T_BOOLEAN || kind == T_CHAR || kind == T_I1 || kind == T_U1 || kind == T_I2
                || kind == T_U2 || kind == T_I4 || kind == T_U4 || kind == T_I8 || kind == T_U8
                || kind == T_R4 || kind == T_R8;
        }

        
        
        
        
        private static bool Interpret(IntPtr type, IntPtr val, bool boxed, out string display, out IntPtr refObj)
        {
            display = null; refObj = IntPtr.Zero;
            try
            {
                var kind = Il2Cpp.il2cpp_type_get_type(type);

                
                if (val == IntPtr.Zero)
                {
                    if (kind == T_STRING) { display = "(null)"; return true; }
                    if (kind == T_CLASS || kind == T_OBJECT || kind == T_ARRAY || kind == T_SZARRAY || kind == T_PTR)
                    { display = "(null)"; return true; }
                    display = "(null/空指针)";
                    return true;
                }

                
                if (boxed && (IsPrimitive(kind) || kind == T_VALUETYPE))
                {
                    var raw = Il2Cpp.il2cpp_object_unbox(val);
                    if (raw == IntPtr.Zero) { display = "(拆箱失败)"; return true; }
                    val = raw;
                }

                switch (kind)
                {
                    case T_VOID: display = "(void)"; return true;
                    case T_BOOLEAN: display = Marshal.ReadByte(val) != 0 ? "true" : "false"; return true;
                    case T_CHAR: display = "'" + ((char)Marshal.ReadInt16(val)) + "'"; return true;
                    case T_I1: display = ((sbyte)Marshal.ReadByte(val)).ToString(CultureInfo.InvariantCulture); return true;
                    case T_U1: display = Marshal.ReadByte(val).ToString(CultureInfo.InvariantCulture); return true;
                    case T_I2: display = Marshal.ReadInt16(val).ToString(CultureInfo.InvariantCulture); return true;
                    case T_U2: display = ((ushort)Marshal.ReadInt16(val)).ToString(CultureInfo.InvariantCulture); return true;
                    case T_I4: display = Marshal.ReadInt32(val).ToString(CultureInfo.InvariantCulture); return true;
                    case T_U4: display = ((uint)Marshal.ReadInt32(val)).ToString(CultureInfo.InvariantCulture); return true;
                    case T_I8: display = Marshal.ReadInt64(val).ToString(CultureInfo.InvariantCulture); return true;
                    case T_R4: display = N(Marshal.PtrToStructure<float>(val)); return true;
                    case T_R8: display = Marshal.PtrToStructure<double>(val).ToString("0.####", CultureInfo.InvariantCulture); return true;
                    case T_STRING:
                        {
                            var s = Il2Cpp.Il2CppStringToManaged(val);
                            display = s == null ? "(null)" : "\"" + Esc(s, 160) + "\"";
                            return true;
                        }
                    case T_CLASS: case T_OBJECT: case T_ARRAY: case T_SZARRAY: case T_PTR:
                        {
                            refObj = val;
                            var tn = NativeTypeName(val);
                            display = val == IntPtr.Zero ? "(null)" : ("<" + (tn ?? "对象") + ">");
                            return true;
                        }
                    case T_VALUETYPE:
                        {
                            var k = Il2Cpp.il2cpp_class_from_type(type);
                            var nm = KlassName(k);
                            display = StructText(nm, val);
                            return true;
                        }
                    default:
                        display = "(类型kind=" + kind + ")";
                        return true;
                }
            }
            catch { return false; }
        }

        
        private static string StructText(string klassName, IntPtr val)
        {
            try
            {
                switch (klassName)
                {
                    case "UnityEngine.Color":
                        {
                            var v = Marshal.PtrToStructure<F4>(val);
                            return "(r=" + N(v.a) + " g=" + N(v.b) + " b=" + N(v.c) + " a=" + N(v.d) + ")";
                        }
                    case "UnityEngine.Color32":
                        {
                            var b = new byte[4];
                            Marshal.Copy(val, b, 0, 4);
                            return "(r=" + b[0] + " g=" + b[1] + " b=" + b[2] + " a=" + b[3] + ")";
                        }
                    case "UnityEngine.Vector2":
                        {
                            var v = Marshal.PtrToStructure<F2>(val);
                            return "(" + N(v.a) + ", " + N(v.b) + ")";
                        }
                    case "UnityEngine.Vector3":
                        {
                            var v = Marshal.PtrToStructure<F3>(val);
                            return "(" + N(v.a) + ", " + N(v.b) + ", " + N(v.c) + ")";
                        }
                    case "UnityEngine.Vector4":
                        {
                            var v = Marshal.PtrToStructure<F4>(val);
                            return "(" + N(v.a) + ", " + N(v.b) + ", " + N(v.c) + ", " + N(v.d) + ")";
                        }
                    case "UnityEngine.Rect":
                        {
                            var v = Marshal.PtrToStructure<F4>(val);
                            return "(x=" + N(v.a) + " y=" + N(v.b) + " w=" + N(v.c) + " h=" + N(v.d) + ")";
                        }
                    case "UnityEngine.Quaternion":
                        {
                            var v = Marshal.PtrToStructure<F4>(val);
                            return "(" + N(v.a) + ", " + N(v.b) + ", " + N(v.c) + ", " + N(v.d) + ")";
                        }
                    default:
                        return "(值类型 " + (klassName ?? "?") + ")";
                }
            }
            catch { return "(值类型 " + (klassName ?? "?") + " 读取失败)"; }
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct F2 { public float a, b; }
        [StructLayout(LayoutKind.Sequential)]
        private struct F3 { public float a, b, c; }
        [StructLayout(LayoutKind.Sequential)]
        private struct F4 { public float a, b, c, d; }

        private static string N(float f) { return f.ToString("0.####", CultureInfo.InvariantCulture); }

        private static string Esc(string s, int max)
        {
            if (s == null) return "(null)";
            var sb = new StringBuilder(Math.Min(s.Length, max) + 8);
            int n = Math.Min(s.Length, max);
            for (int i = 0; i < n; i++)
            {
                var c = s[i];
                if (c == '\n') sb.Append("\\n");
                else if (c == '\r') sb.Append("\\r");
                else if (c == '\t') sb.Append("\\t");
                else if (c < 0x20) sb.Append("\\u").Append(((int)c).ToString("x4", CultureInfo.InvariantCulture));
                else sb.Append(c);
            }
            if (s.Length > n) sb.Append("…(共 ").Append(s.Length).Append(" 字符)");
            return sb.ToString();
        }

        
        
        

        
        public static bool Is(IntPtr obj, string managedFullName)
        {
            return HasAncestor(obj, managedFullName);
        }

        
        
        
        
        public static void DumpAppearance(IntPtr comp, string prefix, Action<string> raw)
        {
            if (raw == null) return;
            if (!ProbeNativeGuard.NativeAllowed)
            {
                ProbeNativeGuard.Note("Il2CppNative.DumpAppearance");
                raw(prefix + "（" + ProbeTier.TierName + " 层不开原生通道 → 外观兜底采集跳过；"
                    + "要这条数据请把 oi-probe.json 的 tier 设为 full）");
                return;
            }
            if (comp == IntPtr.Zero) { raw(prefix + "⚠ 组件指针为空（Unity 空对象）—— 不读字段"); return; }
            try
            {
                raw(prefix + "★ 外观（原生 il2cpp 通道采集：getter + invoke，不经过托管程序集）");

                if (Is(comp, "UnityEngine.UI.Graphic"))
                {
                    Row(raw, comp, prefix, "· Graphic", new[] { "color", "raycastTarget", "depth" });
                    var mat = MemberRef(comp, "material");
                    raw(prefix + "    material.name = " + (mat == IntPtr.Zero ? "(null)" : Member(mat, "name")));
                }

                if (Is(comp, "UnityEngine.UI.Image"))
                {
                    raw(prefix + "  ★ Image（背景/边框判据）:");
                    Row(raw, comp, prefix, "    ", new[]
                    {
                        "sprite", "type", "preserveAspect", "fillCenter", "fillMethod", "fillAmount",
                        "fillClockwise", "fillOrigin", "useSpriteMesh", "hasBorder", "pixelsPerUnitMultiplier",
                        "pixelsPerUnit", "alphaHitTestMinimumThreshold"
                    });
                    var sp = MemberRef(comp, "sprite");
                    if (sp != IntPtr.Zero)
                    {
                        raw(prefix + "      sprite.name   = " + Member(sp, "name"));
                        raw(prefix + "      sprite.border  = " + Member(sp, "border") + "   （x=左 y=下 z=右 w=上；全 0 = 无边框）");
                        raw(prefix + "      sprite.rect    = " + Member(sp, "rect")
                                      + "  pixelsPerUnit=" + Member(sp, "pixelsPerUnit"));
                        var tex = MemberRef(sp, "texture");
                        raw(prefix + "      sprite.texture.name = " + (tex == IntPtr.Zero ? "(null)" : Member(tex, "name")));
                    }
                    else raw(prefix + "      sprite = (null / 空对象) ⇒ 该 Image 不画图，但 color 仍以白图渲染（纯色块）");
                }

                if (Is(comp, "UnityEngine.UI.LayoutGroup"))
                {
                    raw(prefix + "  ★ LayoutGroup（内边距/间距判据）:");
                    var pad = MemberRef(comp, "padding");
                    if (pad != IntPtr.Zero)
                        raw(prefix + "      padding = left=" + Member(pad, "left") + " right=" + Member(pad, "right")
                                      + " top=" + Member(pad, "top") + " bottom=" + Member(pad, "bottom")
                                      + "  (horizontal=" + Member(pad, "horizontal") + " vertical=" + Member(pad, "vertical") + ")");
                    else raw(prefix + "      padding = (读不到)");
                    Row(raw, comp, prefix, "      ", new[]
                    {
                        "childAlignment", "isRootLayoutGroup", "minWidth", "preferredWidth", "flexibleWidth",
                        "minHeight", "preferredHeight", "flexibleHeight"
                    });
                }

                if (Is(comp, "UnityEngine.UI.HorizontalOrVerticalLayoutGroup"))
                    Row(raw, comp, prefix, "  ★ 排布: ", new[]
                    {
                        "spacing", "childForceExpandWidth", "childForceExpandHeight", "childControlWidth",
                        "childControlHeight", "childScaleWidth", "childScaleHeight", "reverseArrangement"
                    });

                if (Is(comp, "UnityEngine.UI.ContentSizeFitter"))
                    Row(raw, comp, prefix, "  ★ ContentSizeFitter: ", new[] { "horizontalFit", "verticalFit" });

                if (Is(comp, "UnityEngine.UI.LayoutElement"))
                    Row(raw, comp, prefix, "  ★ LayoutElement: ", new[]
                    {
                        "ignoreLayout", "minWidth", "minHeight", "preferredWidth", "preferredHeight",
                        "flexibleWidth", "flexibleHeight", "layoutPriority"
                    });

                if (Is(comp, "UnityEngine.UI.RectMask2D"))
                    Row(raw, comp, prefix, "  ★ RectMask2D（裁剪）: ", new[] { "padding", "softness" });

                if (Is(comp, "UnityEngine.UI.Selectable"))
                {
                    Row(raw, comp, prefix, "  · Selectable: ", new[] { "transition", "interactable" });
                    var tg = MemberRef(comp, "targetGraphic");
                    raw(prefix + "      targetGraphic = " + (tg == IntPtr.Zero ? "(null)" : NativeTypeName(tg)));
                }

                if (Is(comp, "UnityEngine.CanvasGroup"))
                    Row(raw, comp, prefix, "  ★ CanvasGroup: ", new[]
                    {
                        "alpha", "interactable", "blocksRaycasts", "ignoreParentGroups"
                    });

                if (Is(comp, "UnityEngine.Canvas"))
                    Row(raw, comp, prefix, "  ★ Canvas: ", new[]
                    {
                        "renderMode", "sortingOrder", "overrideSorting", "sortingLayerName", "scaleFactor",
                        "referencePixelsPerUnit", "isRootCanvas", "pixelPerfect", "renderOrder"
                    });

                if (Is(comp, "TMPro.TMP_Text"))
                {
                    raw(prefix + "  ★ TMP_Text（渲染参数）:");
                    Row(raw, comp, prefix, "      ", new[]
                    {
                        "fontSize", "enableAutoSizing", "alignment", "enableWordWrapping", "overflowMode",
                        "margin", "richText", "isTextOverflowing", "isTextTruncated", "preferredWidth", "preferredHeight"
                    });
                }
            }
            catch (Exception e)
            {
                try { raw(prefix + "（原生外观采集失败：" + e.GetType().Name + " : " + e.Message + "）"); } catch { }
            }
        }

        private static void Row(Action<string> raw, IntPtr obj, string prefix, string head, string[] members)
        {
            var sb = new StringBuilder();
            sb.Append(prefix).Append(head).Append(' ');
            for (int i = 0; i < members.Length; i++)
            {
                if (i > 0) sb.Append("  ");
                sb.Append(members[i]).Append('=').Append(Member(obj, members[i]));
            }
            raw(sb.ToString());
        }
    }
}
