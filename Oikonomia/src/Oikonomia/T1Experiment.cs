
















using System;
using System.Reflection;
using HarmonyLib;

namespace Oikonomia
{
    internal static partial class EconomyPatches
    {
        private static int _t1Lines;
        private const int T1LineCap = 20;
        private static int _t1Appended;
        private static string _t1LastKey = "";
        private static DateTime _t1LastTime = DateTime.MinValue;

        
        internal static void InstallT1Experiment(Harmony harmony, Assembly asm, ref int ok, ref int miss)
        {
            try
            {
                if (EconomyConfig.T1Experiment == null || !EconomyConfig.T1ExperimentOn)
                {
                    _log.LogDebug("[OI][t1exp] T1 最小实验：已关闭（未挂任何钩子）");
                    return;
                }
                if (asm == null) { _log.LogWarning("[OI][t1exp] 跳过：Assembly-CSharp 未加载"); return; }

                var t = Reflect.FindType(asm, "ConversationHandler");
                if (t == null) { miss++; _log.LogWarning("[OI][t1exp] **未挂上**：找不到 ConversationHandler"); return; }
                var post = new HarmonyMethod(typeof(EconomyPatches).GetMethod(
                    nameof(Post_T1ResponseMenu), BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public));
                int n = 0;
                foreach (var m in t.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
                {
                    if (m == null || m.Name != "OnConversationResponseMenu") continue;
                    try { harmony.Patch(m, postfix: post); n++; _log.LogInfo("[OI][t1exp] 已挂 Postfix：ConversationHandler.OnConversationResponseMenu(" + m.GetParameters().Length + " 参数)"); }
                    catch (Exception e) { _log.LogWarning("[OI][t1exp] 挂载失败（跳过）：" + e.Message); }
                }
                if (n > 0) ok++; else { miss++; _log.LogWarning("[OI][t1exp] **未挂上**：OnConversationResponseMenu 不存在或签名不同"); }
            }
            catch (Exception e) { _log.LogWarning("[OI][t1exp] 安装异常（忽略，不影响游戏）：" + e.Message); }
        }

        
        
        
        
        private static void Post_T1ResponseMenu(object __instance, object __0)
        {
            try
            {
                if (EconomyConfig.T1Experiment == null || !EconomyConfig.T1ExperimentOn) return;
                if (_t1Lines >= T1LineCap) return;
                if (__instance == null || __0 == null) return;

                
                int cnt = -1;
                try { var lv = Reflect.Prop(__0, "Length"); if (lv != null) cnt = Convert.ToInt32(lv); } catch { }
                if (cnt < 0) { try { var cv = Reflect.Prop(__0, "Count"); if (cv != null) cnt = Convert.ToInt32(cv); } catch { } }
                object first = null;
                try
                {
                    var ty0 = __0.GetType();
                    var mi = ty0.GetMethod("get_Item", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                    if (mi == null) mi = ty0.GetMethod("Get", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                    if (mi != null && mi.GetParameters().Length == 1) { if (cnt > 0) first = mi.Invoke(__0, new object[] { 0 }); }
                    else
                    {
                        var pi = ty0.GetProperty("Item", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                        if (pi != null && pi.GetIndexParameters().Length == 1 && cnt > 0) first = pi.GetValue(__0, new object[] { 0 });
                    }
                }
                catch (Exception e0) { _log.LogWarning("[OI][t1exp] 读首项异常（已忽略）：" + e0.GetType().Name); }

                string firstText;
                object firstFt = null;
                try
                {
                    firstFt = Reflect.Prop(first, "formattedText");
                    var ftText = Reflect.Prop(firstFt, "text") ?? Reflect.Prop(firstFt, "Text") ?? firstFt;
                    firstText = ftText == null ? "（无文本）" : Short(ftText.ToString());
                }
                catch { firstText = "?"; }

                _t1Lines++;
                _log.LogInfo("[OI][t1exp][menu] 菜单构建：Length=" + (cnt < 0 ? "读不到 ✗" : cnt.ToString())
                    + " 首项=" + (first == null ? "无" : first.GetType().Name) + " 首项文本=" + firstText);

                string dkey = __instance.GetHashCode().ToString() + "|" + cnt;
                if (_t1LastKey == dkey && (DateTime.UtcNow - _t1LastTime).TotalSeconds < 1.0)
                {
                    _log.LogInfo("[OI][t1exp][skip] 重复触发（同实例同长度 <1s）⇒ **不再追加** ✓ 已追加=" + _t1Appended);
                    return;
                }
                _t1LastKey = dkey; _t1LastTime = DateTime.UtcNow;

                if (first == null) { _log.LogWarning("[OI][t1exp] 无可用 response(Length=" + cnt + ") ⇒ 不追加"); return; }

                
                string cbHow;
                object cb = BuildUnityAction(out cbHow);

                
                object destEntry = null;
                try { destEntry = Reflect.Prop(first, "destinationEntry"); } catch { }
                object clone = null; string cloneHow;
                try
                {
                    clone = Activator.CreateInstance(first.GetType(), new object[] { firstFt, destEntry, true });
                    cloneHow = "克隆(原文本, 原.destinationEntry=" + (destEntry == null ? "**null ✗**" : "非null ✓") + ", true) ✓";
                }
                catch (Exception e4)
                {
                    clone = first;   
                    cloneHow = "克隆失败 ✗(" + e4.GetType().Name + ") ⇒ 复用首项原对象 ✓（destEntry=" + (destEntry == null ? "null ✗" : "非null ✓") + "）";
                }
                _log.LogInfo("[OI][t1exp][add] " + cloneHow + " 回调参数=" + cbHow);

                var tpl = Reflect.Prop(__instance, "templateResponse");
                if (tpl == null) { _log.LogWarning("[OI][t1exp] 未取到 templateResponse ⇒ 不追加（如实报告）"); return; }
                var setup = tpl.GetType().GetMethod("Setup", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (setup == null || setup.GetParameters().Length != 3) { _log.LogWarning("[OI][t1exp] Setup(3 参数) 未找到 ⇒ 不追加"); return; }
                bool cbNull = cb == null;
                bool deNull = Reflect.Prop(clone, "destinationEntry") == null;
                _log.LogInfo("[OI][t1exp][try] 调用 Setup ⇒ onClick=" + (cbNull ? "**null ✗**" : "非null ✓")
                    + " destinationEntry=" + (deNull ? "**null ✗**" : "非null ✓"));
                try
                {
                    setup.Invoke(tpl, new object[] { clone, 0, cb });
                    _t1Appended++;
                    _log.LogInfo("[OI][t1exp][ok] ★ Setup(克隆, 0, cb) **未抛异常** ⇒ 判读：**克隆 + 真回调可行** ✓✓"
                        + " ⇒ **T1 的形态定了** ✓（点击由我们自己的回调执行那两句指令 ⇒ 走 RunInstant ✓）已追加=" + _t1Appended);
                }
                catch (Exception e2)
                {
                    _log.LogWarning("[OI][t1exp][fail] ★ Setup 抛异常 ✗ ｜**两个参数状态**：onClick=" + (cbNull ? "null ✗" : "非null ✓")
                        + " destinationEntry=" + (deNull ? "null ✗" : "非null ✓")
                        + " ⇒ 判读：若两者都非 null 仍 NRE ⇒ **Setup 还有别的依赖** ✗（另想办法 ✓）");
                    DumpChain(e2, "Setup");
                }
            }
            catch { }   
        }

        
        private static object BuildUnityAction(out string how)
        {
            var steps = new System.Text.StringBuilder();
            try
            {
                var mClick = typeof(EconomyPatches).GetMethod("T1ExpClick",
                    BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
                if (mClick == null) { how = "null（未找到 T1ExpClick ✗）"; return null; }
                var ua = ResolveUnityAction();
                if (ua == null) { how = "null（UnityAction 类型未解析到 ✗）"; return null; }

                
                try
                {
                    var d1 = Delegate.CreateDelegate(ua, mClick);
                    if (d1 != null) { how = "真 UnityAction ✓[S1 CreateDelegate]"; return d1; }
                    steps.Append("S1返回null;");
                }
                catch (Exception e1) { steps.Append("S1 " + e1.GetType().Name + "(" + Short(e1.Message) + ");"); }

                
                try
                {
                    var act = Delegate.CreateDelegate(typeof(Action), mClick);
                    Type ds = null;
                    foreach (var a in AppDomain.CurrentDomain.GetAssemblies())
                    {
                        try { ds = a.GetType("Il2CppInterop.Runtime.DelegateSupport"); if (ds != null) break; } catch { }
                    }
                    if (ds != null)
                    {
                        foreach (var mm in ds.GetMethods(BindingFlags.Public | BindingFlags.Static))
                        {
                            if (mm.Name != "ConvertDelegate" || !mm.IsGenericMethodDefinition || mm.GetParameters().Length != 1) continue;
                            try
                            {
                                var g = mm.MakeGenericMethod(ua);
                                var d2 = g.Invoke(null, new object[] { act });
                                if (d2 != null) { how = "真 UnityAction ✓[S2 DelegateSupport.ConvertDelegate]"; return d2; }
                                steps.Append("S2返回null;");
                            }
                            catch (Exception e2b) { steps.Append("S2 " + e2b.GetType().Name + ";"); }
                            break;
                        }
                        if (ds != null) steps.Append("S2已试;");
                    }
                    else steps.Append("S2未找到DelegateSupport;");
                }
                catch (Exception e2) { steps.Append("S2外 " + e2.GetType().Name + ";"); }

                
                try
                {
                    var ctor = ua.GetConstructor(new[] { typeof(object), typeof(IntPtr) });
                    if (ctor != null)
                    {
                        var d3 = ctor.Invoke(new object[] { null, IntPtr.Zero });
                        if (d3 != null) { how = "真 UnityAction ✓[S3 ctor(object,IntPtr)]"; return d3; }
                        steps.Append("S3返回null;");
                    }
                    else steps.Append("S3无该ctor;");
                }
                catch (Exception e3) { steps.Append("S3 " + e3.GetType().Name + ";"); }

                how = "null（三策略全失败 ✗：" + steps.ToString() + "）";
                return null;
            }
            catch (Exception e) { how = "null（构建异常 ✗ " + e.GetType().Name + "）"; return null; }
        }

        
        private static Type ResolveUnityAction()
        {
            try
            {
                var t = Type.GetType("UnityEngine.Events.UnityAction, UnityEngine.CoreModule");
                if (t != null) return t;
                foreach (var a in AppDomain.CurrentDomain.GetAssemblies())
                {
                    try { var x = a.GetType("UnityEngine.Events.UnityAction"); if (x != null) return x; } catch { }
                }
            }
            catch { }
            return null;
        }

        
        private static void DumpChain(Exception e, string tag)
        {
            try
            {
                int n = 0;
                var cur = e;
                while (cur != null && n < 8)
                {
                    n++;
                    string msg = cur.Message == null ? "" : cur.Message.Replace('\n', ' ').Replace('\r', ' ');
                    _log.LogWarning("[OI][t1exp][cause] " + tag + " 第 " + n + " 层：" + cur.GetType().FullName + " — " + msg);
                    if (msg.IndexOf("UnityAction", StringComparison.OrdinalIgnoreCase) >= 0)
                        _log.LogWarning("[OI][t1exp][cause] ↑ 判读：**回调类型问题** ✓");
                    if (msg.IndexOf("destinationEntry", StringComparison.OrdinalIgnoreCase) >= 0
                        || msg.IndexOf("NullReference", StringComparison.OrdinalIgnoreCase) >= 0)
                        _log.LogWarning("[OI][t1exp][cause] ↑ 判读：**NRE ⇒ 看上面两个参数的 null 状态** ✓（哪一个为 null ✓）");
                    cur = cur.InnerException;
                }
                if (n == 0) _log.LogWarning("[OI][t1exp][cause] 异常链为空 ✗");
            }
            catch { }
        }

        
        private static void T1ExpClick()
        {
            try { _log.LogInfo("[OI][t1exp][click] ★ 测试项被点击了 ✓（回调生效；本实验**不执行任何指令** ✗）"); } catch { }
        }


    }
}
