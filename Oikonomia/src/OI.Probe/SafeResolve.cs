using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using HarmonyLib;

namespace Oikonomia.Probe
{
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    internal static class SafeResolve
    {
        
        public const string Discipline = "OI-PROBE-NO-NAME-LOAD";

        
        public static volatile bool AllowInteropLoadFrom = false;

        private static readonly object _lock = new object();
        private static readonly Dictionary<string, Type> _typeCache = new Dictionary<string, Type>(StringComparer.Ordinal);
        private static readonly Dictionary<string, string> _interopTried = new Dictionary<string, string>(StringComparer.Ordinal);
        private static readonly List<string> _unavailable = new List<string>();

        
        public static string[] Unavailable { get { lock (_lock) return _unavailable.ToArray(); } }

        
        public static readonly string[] WatchedAssemblies =
        {
            "Assembly-CSharp", "Il2CppInterop.Runtime", "UnityEngine.CoreModule", "UnityEngine.UI",
            "UnityEngine.UIModule", "Unity.TextMeshPro", "UnityEngine.TextRenderingModule",
            "UnityEngine.IMGUIModule", "UnityEngine.TextCoreTextEngineModule", "UnityEngine.TextCoreFontEngineModule",
            "UnityEngine", "mscorlib", "Il2Cppmscorlib"
        };

        
        public static readonly string[] WatchedImages =
        {
            "Assembly-CSharp", "UnityEngine.CoreModule", "UnityEngine.UI", "UnityEngine.UIModule",
            "Unity.TextMeshPro", "UnityEngine.TextRenderingModule", "UnityEngine.IMGUIModule"
        };

        
        
        

        
        public static Assembly[] Loaded()
        {
            try { return AppDomain.CurrentDomain.GetAssemblies() ?? new Assembly[0]; }
            catch { return new Assembly[0]; }
        }

        
        public static Assembly LoadedByName(string name)
        {
            var want = Normalize(name);
            if (want.Length == 0) return null;
            try
            {
                foreach (var a in Loaded())
                {
                    try
                    {
                        var n = a.GetName();
                        if (n != null && string.Equals(n.Name, want, StringComparison.OrdinalIgnoreCase))
                            return a;
                    }
                    catch { }
                }
            }
            catch { }
            return null;
        }

        private static string Normalize(string name)
        {
            if (string.IsNullOrEmpty(name)) return "";
            var s = name.Trim();
            if (s.EndsWith(".dll", StringComparison.OrdinalIgnoreCase)) s = s.Substring(0, s.Length - 4);
            return s;
        }

        
        
        

        
        
        
        
        public static Type FindType(string fullName)
        {
            return FindTypePreferred(fullName, null);
        }

        
        public static Type FindTypePreferred(string fullName, string[] preferredAssemblies)
        {
            if (string.IsNullOrEmpty(fullName)) return null;
            lock (_lock)
            {
                Type cached;
                if (_typeCache.TryGetValue(fullName, out cached)) return cached;
            }

            var found = FindInLoaded(fullName, preferredAssemblies);

            
            if (found == null && AllowInteropLoadFrom)
            {
                foreach (var an in PreferredOrAll(fullName, preferredAssemblies))
                {
                    string note;
                    found = TryInteropLoadFrom(an, fullName, out note);
                    if (found != null) break;
                }
            }

            lock (_lock) { _typeCache[fullName] = found; }
            return found;
        }

        
        public static Type FindInLoaded(string fullName, string[] preferredAssemblies)
        {
            if (string.IsNullOrEmpty(fullName)) return null;
            var names = PreferredOrAll(fullName, preferredAssemblies);

            
            for (int pass = 0; pass < 2; pass++)
            {
                if (pass == 0)
                {
                    if (names == null) continue;
                    foreach (var an in names)
                    {
                        var t = GetTypeFrom(LoadedByName(an), fullName);
                        if (t != null) return t;
                    }
                }
                else
                {
                    
                    foreach (var a in Loaded())
                    {
                        var t = GetTypeFrom(a, fullName);
                        if (t != null) return t;
                    }
                }
            }

            
            int i = fullName.LastIndexOf('.');
            if (i > 0)
            {
                var nested = fullName.Substring(0, i) + "+" + fullName.Substring(i + 1);
                foreach (var a in Loaded())
                {
                    var t = GetTypeFrom(a, nested);
                    if (t != null) return t;
                }
            }
            return null;
        }

        private static Type GetTypeFrom(Assembly a, string fullName)
        {
            if (a == null) return null;
            try { return a.GetType(fullName, false); }
            catch { return null; }   
        }

        
        
        
        
        public static string[] CandidateAssemblies(string fullName)
        {
            var l = new List<string>();
            if (fullName == null) fullName = "";
            if (fullName.StartsWith("UnityEngine.UI.", StringComparison.Ordinal)) l.Add("UnityEngine.UI");
            else if (fullName.StartsWith("UnityEngine.UIModule", StringComparison.Ordinal)) l.Add("UnityEngine.UIModule");
            else if (fullName.StartsWith("UnityEngine.EventSystems", StringComparison.Ordinal)) l.Add("UnityEngine.UI");
            else if (fullName.StartsWith("TMPro.", StringComparison.Ordinal)) l.Add("Unity.TextMeshPro");
            else if (fullName.StartsWith("UnityEngine.", StringComparison.Ordinal)) l.Add("UnityEngine.CoreModule");
            else l.Add("Assembly-CSharp");
            l.Add("UnityEngine.UI"); l.Add("UnityEngine.UIModule"); l.Add("UnityEngine.CoreModule");
            l.Add("Unity.TextMeshPro"); l.Add("Assembly-CSharp"); l.Add("UnityEngine");
            return l.ToArray();
        }

        
        private static string[] PreferredOrAll(string fullName, string[] preferred)
        {
            if (preferred != null && preferred.Length > 0) return preferred;
            return CandidateAssemblies(fullName);
        }

        
        
        

        
        public static string InteropDir
        {
            get
            {
                try
                {
                    string root = null;
                    try { root = BepInEx.Paths.BepInExRootPath; } catch { }
                    if (string.IsNullOrEmpty(root))
                    {
                        var loc = typeof(SafeResolve).Assembly.Location;      
                        if (!string.IsNullOrEmpty(loc))
                        {
                            var plugins = Path.GetDirectoryName(loc);
                            var bep = plugins == null ? null : Path.GetDirectoryName(plugins);
                            if (!string.IsNullOrEmpty(bep)) root = bep;
                        }
                    }
                    if (string.IsNullOrEmpty(root)) return null;
                    var dir = Path.Combine(root, "interop");
                    return Directory.Exists(dir) ? dir : null;
                }
                catch { return null; }
            }
        }

        
        
        
        
        
        public static Type TryInteropLoadFrom(string asmName, string fullName, out string note)
        {
            note = null;
            var key = Normalize(asmName);
            if (key.Length == 0) { note = "程序集名空"; return null; }

            lock (_lock)
            {
                string why;
                if (_interopTried.TryGetValue(key, out why))
                {
                    note = "本会话已尝试过（" + (why ?? "已跳过") + "）";
                    return null;
                }
                _interopTried[key] = "尝试中";
            }

            try
            {
                var dir = InteropDir;
                if (dir == null)
                {
                    lock (_lock) { _interopTried[key] = "无 interop 目录"; }
                    note = "无 interop 目录";
                    return null;
                }
                var file = Path.Combine(dir, key + ".dll");
                if (!File.Exists(file))
                {
                    lock (_lock) { _interopTried[key] = "interop 目录无此 DLL"; }
                    note = "interop 目录无此 DLL";
                    return null;
                }

                var a = Assembly.LoadFrom(file);
                var t = GetTypeFrom(a, fullName);
                lock (_lock) { _interopTried[key] = t != null ? "✓ 直载成功" : "直载成功但无该类型"; }
                note = t != null ? "③ interop 直载成功（" + file + "）" : "直载成功但无该类型";
                return t;
            }
            catch (Exception e)
            {
                lock (_lock) { _interopTried[key] = "直载失败：" + e.GetType().Name; }
                note = "③ interop 直载失败（已跳过，不再重试）：" + e.GetType().Name + " : " + e.Message;
                return null;
            }
        }

        
        public static string[] InteropAttempts()
        {
            lock (_lock)
            {
                var l = new List<string>();
                foreach (var kv in _interopTried) l.Add(kv.Key + " → " + kv.Value);
                return l.ToArray();
            }
        }

        
        
        

        
        
        
        
        
        public static volatile bool AllowForeignPatchStack = false;

        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        public static string PatchRefusal(MethodInfo target)
        {
            if (target == null) return "方法不存在（null）";
            
            
            
            var tierRefusal = ProbeTier.TierRefusal(target);
            if (tierRefusal != null) return tierRefusal;
            try
            {
                if (!AccessTools.IsDeclaredMember(target))
                {
                    var decl = AccessTools.GetDeclaredMember(target) as MethodInfo;
                    return "HarmonyX 判为**声明替身**（会改挂到 " + Describe(decl ?? target)
                           + " 并打警告；IL2CPP 上叠加 detour 会 AccessViolation）→ 按建议跳过";
                }
            }
            catch { }
            if (!Patchable(target)) return "无方法体（抽象/接口声明）→ 跳过";
            try
            {
                var owner = ForeignPatchOwner(target);
                if (owner != null && !AllowForeignPatchStack && IsInteropTarget(target))
                    return "已被其它模组 patch（owner=" + owner + "）→ 拒绝叠加 IL2CPP detour";
            }
            catch { }
            return null;
        }

        
        public static string ForeignPatchOwner(MethodBase target)
        {
            try
            {
                var info = HarmonyLib.Harmony.GetPatchInfo(target);
                if (info == null) return null;
                var owners = info.Owners;
                if (owners == null) return null;
                foreach (var o in owners)
                {
                    if (string.IsNullOrEmpty(o)) continue;
                    if (!IsOurs(o)) return o;
                }
            }
            catch { }
            return null;
        }

        
        private static bool IsOurs(string owner)
        {
            return owner == ProbeOwnerPrefix
                || owner.StartsWith(ProbeOwnerPrefix + ".", StringComparison.Ordinal);
        }

        
        
        public const string ProbeOwnerPrefix = "com.dell.suzerain.oikonomia.probe";

        
        public static bool IsInteropTarget(MethodBase m)
        {
            try
            {
                var an = m.DeclaringType == null ? null : m.DeclaringType.Assembly.GetName().Name;
                return !string.Equals(an, "Assembly-CSharp", StringComparison.OrdinalIgnoreCase);
            }
            catch { return true; }   
        }

        
        public static string Describe(MethodBase m)
        {
            try
            {
                if (m == null) return "(null)";
                var dt = m.DeclaringType;
                var sb = new StringBuilder();
                if (dt != null) sb.Append(dt.FullName ?? dt.Name).Append('.');
                sb.Append(m.Name);
                var ps = m.GetParameters();
                if (ps.Length > 0)
                {
                    sb.Append('(');
                    for (int i = 0; i < ps.Length; i++)
                    {
                        if (i > 0) sb.Append(',');
                        sb.Append(ps[i].ParameterType == null ? "?" : ps[i].ParameterType.Name);
                    }
                    sb.Append(')');
                }
                return sb.ToString();
            }
            catch { return "(不可读方法)"; }
        }

        
        
        
        
        
        public static bool Patchable(MethodInfo mi)
        {
            if (mi == null) return false;
            try
            {
                if (mi.IsAbstract) return false;
                if (mi.DeclaringType != null && mi.DeclaringType.IsInterface) return false;
                return mi.GetMethodBody() != null;
            }
            catch { return false; }
        }

        
        public static void MarkUnavailable(string what, string why)
        {
            var line = what + " —— " + why;
            lock (_lock)
            {
                if (!_unavailable.Contains(line)) _unavailable.Add(line);
            }
        }

        public static bool IsUnavailable(string what)
        {
            lock (_lock)
            {
                foreach (var l in _unavailable)
                    if (l.StartsWith(what, StringComparison.Ordinal)) return true;
            }
            return false;
        }

        
        
        

        
        
        
        
        
        
        
        
        
        public static int RunGate(string title, string[] typeNames, Action<string> raw, string[] required)
        {
            int ok = 0;
            if (raw == null) raw = delegate (string s) { };
            try
            {
                raw("");
                raw("────────────────────────────────────────────────────────────────");
                raw("【" + title + "】");
                raw("────────────────────────────────────────────────────────────────");
                raw("  ★ 铁律（grep 锚点 " + Discipline + "）：启动期绝不按名字 Load 程序集、绝不调");
                raw("      IL2CPP.GetIl2CppClass(程序集名,…)（0.3.3 崩溃动作）。取类型只有三条合法路径：");
                raw("      ① 已加载程序集（AppDomain.CurrentDomain.GetAssemblies）  ② 原生 il2cpp（il2cpp_class_from_name…）");
                raw("      ③ interop 目录 Assembly.LoadFrom（默认关、只试一次、失败即降级）");
                raw("      interop 直载当前状态 = " + (AllowInteropLoadFrom ? "★已开启（③）" : "关闭（走 ①/②，零风险）"));

                
                var asms = Loaded();
                raw("");
                raw("  ── ① 已加载的托管程序集：" + asms.Length + " 个（只读快照，全程不加载任何程序集）");
                foreach (var n in WatchedAssemblies)
                {
                    var a = LoadedByName(n);
                    var ver = "";
                    try { if (a != null) ver = "  v" + a.GetName().Version; } catch { }
                    raw("      " + (a != null ? "✓" : "✗") + " " + n.PadRight(38) + (a != null ? "(已加载)" + ver : "(未加载 → 相关采集降级)"));
                }

                
                var images = Il2CppNative.Available ? Il2CppNative.ImageNames() : new string[0];
                raw("");
                if (Il2CppNative.Available)
                {
                    raw("  ── ② il2cpp 原生域镜像（il2cpp_domain_get_assemblies → il2cpp_assembly_get_image）："
                        + images.Length + " 个");
                    raw("      Il2CppInterop 自己的镜像表 = " + Il2CppNative.InteropImageCount()
                        + " 个（仅作对照；本探针**不查这张表**，查它会写 Error 到玩家日志）");
                    foreach (var n in WatchedImages)
                    {
                        var has = Il2CppNative.HasImage(n);
                        raw("      " + (has ? "✓" : "✗") + " " + n.PadRight(38)
                            + (has ? "(IL2CPP 已注册)" : "(IL2CPP 未注册 → 类型名不可解析，且**绝不 Load** ✗)"));
                    }
                }
                else
                {
                    raw("  ── ② il2cpp 原生域镜像：**本节跳过**（" + ProbeTier.TierName
                        + " 层不开原生通道；启动期绝不触碰 il2cpp）");
                }

                
                
                raw("");
                raw(Il2CppNative.Available
                    ? "  ── ②-b 原生类查找 il2cpp_class_from_name（真实类型名 / 谱系 / 外观字段的第二条腿）"
                    : "  ── ②-b 原生类查找：**本节跳过**（" + ProbeTier.TierName + " 层不开原生通道）");
                var nativeRows = Il2CppNative.Available
                    ? new[]
                      {
                          new[] { "UnityEngine", "CanvasGroup" }, new[] { "UnityEngine", "Canvas" },
                          new[] { "UnityEngine.UI", "Image" }, new[] { "UnityEngine.UI", "LayoutGroup" },
                          new[] { "TMPro", "TMP_Text" }
                      }
                    : new string[0][];
                foreach (var pair in nativeRows)
                {
                    var k = Il2CppNative.FindKlass(pair[0], pair[1]);
                    var full = pair[0] + "." + pair[1];
                    raw("      " + (k != IntPtr.Zero ? "✓" : "·") + " " + full.PadRight(38)
                        + (k != IntPtr.Zero
                            ? "原生可读（0x" + k.ToInt64().ToString("x", CultureInfo.InvariantCulture) + "）"
                            : "类型名不可解析（IL2CPP 未注册）→ 该字段降级，**绝不 Load** ✗"));
                    if (k == IntPtr.Zero)
                        MarkUnavailable("原生类 " + full, "IL2CPP 未注册 → 仅该部分外观字段降级");
                }

                
                raw("");
                raw("  ── 关键类型解析（机制①优先；解析不到就进不可用清单，按需降级）");
                if (typeNames != null)
                {
                    foreach (var n in typeNames)
                    {
                        var t = FindType(n);
                        if (t != null) ok++;
                        string src = null;
                        if (t != null)
                        {
                            try { src = t.Assembly.GetName().Name; } catch { }
                        }
                        var req = IsRequired(n, required);
                        raw("      " + (t != null ? "✓" : (req ? "✗(必需)" : "·")) + " " + n.PadRight(52)
                            + (t != null ? ("← 已加载程序集：" + src) : "← 类型名不可解析（IL2CPP 未注册 / 本次不可用）→ 降级"));
                        if (t == null)
                            MarkUnavailable(n, req ? "必需类型解析失败 → 对应采集整体降级"
                                                  : "可选类型解析失败 → 仅该部分外观字段降级");
                    }
                }

                
                raw("");
                var un = Unavailable;
                raw("  ── ★ 本次不可用清单：" + un.Length + " 项" + (un.Length == 0 ? "（全部可用 ✓）" : ""));
                for (int i = 0; i < un.Length; i++) raw("      " + (i + 1) + ". " + un[i]);

                raw("");
                raw("  结论 : 任何一步失败只会让探针**少采集**；绝不抛出、绝不让异常冒到启动链。");
                raw("────────────────────────────────────────────────────────────────");
            }
            catch (Exception e)
            {
                try { raw("  ⚠ 安全闸自身异常（已吞掉，不影响游戏）：" + e.GetType().Name + " : " + e.Message); }
                catch { }
            }
            LastOk = ok;
            LastTotal = typeNames == null ? 0 : typeNames.Length;
            return ok;
        }

        private static bool IsRequired(string n, string[] required)
        {
            if (required == null || string.IsNullOrEmpty(n)) return false;
            foreach (var r in required)
                if (!string.IsNullOrEmpty(r) && n.StartsWith(r, StringComparison.Ordinal)) return true;
            return false;
        }

        
        public static volatile int LastOk;
        public static volatile int LastTotal;

        
        public static string OneLineSummary(int ok, int total)
        {
            LastOk = ok; LastTotal = total;
            var un = Unavailable;
            var sb = new StringBuilder();
            sb.Append("[安全闸] 类型解析 ").Append(ok).Append('/').Append(total)
              .Append("；已加载程序集 ").Append(Loaded().Length).Append(" 个")
              .Append("；il2cpp 镜像 ").Append(Il2CppNative.Available
                  ? Il2CppNative.ImageNames().Length.ToString(CultureInfo.InvariantCulture) + " 个"
                  : "（安全层不枚举原生通道）")
              .Append("；不可用 ").Append(un.Length).Append(" 项");
            if (un.Length > 0)
            {
                sb.Append("（");
                for (int i = 0; i < un.Length && i < 6; i++) { if (i > 0) sb.Append(" · "); sb.Append(un[i]); }
                sb.Append("）");
            }
            sb.Append(" —— 不合格项一律降级，不 Load、不抛异常 ✓");
            return sb.ToString();
        }

        
        public static string OneLineSummary()
        {
            return OneLineSummary(LastOk, LastTotal);
        }
    }
}
