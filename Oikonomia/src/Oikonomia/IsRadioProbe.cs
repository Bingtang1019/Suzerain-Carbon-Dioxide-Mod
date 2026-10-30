




















using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using HarmonyLib;

namespace Oikonomia
{
    internal static partial class EconomyPatches
    {
        
        private static int _probeHooks;
        
        
        
        
        private static int _probeLines;
        
        private const int ProbeLineCap = 500;
        
        
        
        private static int _probeRealTriggers;
        
        
        private static int _probeLinesCapDropped;
        
        private static int _probeEmptyLines;
        private static int _probeTriggers;
        private static readonly HashSet<string> _probeEmptyLogged = new HashSet<string>(StringComparer.Ordinal);
        private static readonly Dictionary<string, int> _probeEmptyByMethod = new Dictionary<string, int>(StringComparer.Ordinal);
        
        private static readonly Dictionary<string, int> _probeByMethod = new Dictionary<string, int>(StringComparer.Ordinal);
        
        private static readonly HashSet<string> _probeSeen = new HashSet<string>(StringComparer.Ordinal);
        
        private static readonly string[] ProbeMethodTokens =
            { "Setup", "Show", "Open", "Init", "Build", "Refresh", "Display", "Update" };
        
        
        
        
        
        
        
        
        private static readonly string[] ProbeTypeNames =
        {
            "DecisionPanel", "PagedDecisionPanel", "DecisionData", "DecisionsData", "DecisionProperties",
            "DecisionOption", "MultipleChoicePageData", "MultipleChoiceOptionData",
            "MultipleChoiceOptionProperties", "MultipleChoicePageProperties", "MultipleChoiceOptionDataJson",
            "TemplatePagedDecisionsPage"
        };

        
        
        
        internal static void InstallIsRadioProbe(Harmony harmony, Assembly asm, ref int ok, ref int miss)
        {
            try
            {
                if (EconomyConfig.IsRadioProbe == null || !EconomyConfig.IsRadioProbeOn)
                {
                    _log.LogDebug("[OI][probe] 决策页 radio 探针：已关闭（未挂任何钩子）");
                    return;
                }
                if (asm == null) { _log.LogWarning("[OI][probe] 探针跳过：Assembly-CSharp 未加载"); return; }

                
                var sb = new StringBuilder();
                sb.Append("[OI][probe][A] 候选类型：");
                int present = 0;
                for (int i = 0; i < ProbeTypeNames.Length; i++)
                {
                    var t = Reflect.FindType(asm, ProbeTypeNames[i]);
                    sb.Append(ProbeTypeNames[i]).Append(t == null ? "=无 " : "=有 ");
                    if (t == null) continue;
                    present++;
                    AppendMemberShape(sb, t, "isRadio");
                    AppendMemberShape(sb, t, "IsRadioMultipleChoice");
                }
                _log.LogDebug(sb.ToString());
                
                
                
                _log.LogDebug("[OI][probe][A2] 检索空间：候选类型=" + ProbeTypeNames.Length
                    + " 命中=" + present
                    + "｜能力对照（★ 应命中，若全为无 ⇒ 判仪器坏、不判世界无）："
                    + "TemplatePagedDecisionsPage=有 · MultipleChoicePageProperties=有 · MultipleChoicePageData=有");

                
                var panelTypes = new List<Type>();
                foreach (var n in new[] { "DecisionPanel", "PagedDecisionPanel" })
                {
                    var t = Reflect.FindType(asm, n);
                    if (t != null && !panelTypes.Contains(t)) panelTypes.Add(t);
                }
                if (panelTypes.Count == 0)
                {
                    miss++;
                    _log.LogWarning("[OI][probe][B] **未挂上**：找不到 DecisionPanel / PagedDecisionPanel ⇒ 无法探测决策页（如实报告，不静默）");
                    return;
                }

                var post = new HarmonyMethod(typeof(EconomyPatches).GetMethod(
                    nameof(Post_IsRadioProbe), BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public));
                int hit = 0, tried = 0;
                foreach (var t in panelTypes)
                {
                    foreach (var m in t.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
                    {
                        if (m == null || m.IsAbstract || m.ContainsGenericParameters) continue;
                        if (!NameMatches(m.Name)) continue;
                        if (m.GetParameters().Length > 4) continue;          
                        tried++;
                        if (tried > 12) break;                              
                        try
                        {
                            harmony.Patch(m, postfix: post);
                            hit++;
                            _log.LogDebug("[OI][probe][B] 已挂 Postfix：" + t.Name + "." + m.Name
                                + "(" + m.GetParameters().Length + " 参数)");
                        }
                        catch (Exception e)
                        {
                            _log.LogWarning("[OI][probe][B] 挂载失败（跳过）：" + t.Name + "." + m.Name + " — " + e.Message);
                        }
                    }
                }
                _probeHooks = hit;
                if (hit > 0) { ok++; }
                else
                {
                    miss++;
                    _log.LogWarning("[OI][probe][B] **未挂上**：决策面板类型在，但没有任何匹配 "
                        + string.Join("/", ProbeMethodTokens) + " 的实例方法 ⇒ 本轮探不到（如实报告）");
                }
                _log.LogInfo("[OI][probe] 安装完成：钩子 " + hit + " 个");

                
                InstallIsRadioProbePageHooks(harmony, asm);
            }
            catch (Exception e)
            {
                _log.LogWarning("[OI][probe] 安装异常（已忽略，不影响游戏）：" + e.Message);
            }
        }

        
        private static void Post_IsRadioProbe(object __instance, MethodBase __originalMethod)
        {
            try
            {
                if (__instance == null) return;
                if (EconomyConfig.IsRadioProbe == null || !EconomyConfig.IsRadioProbeOn) return;
                
                
                
                
                
                
                
                

                
                
                
                
                
                var idObj = Member(__instance, "currentDecisionData")
                         ?? Member(__instance, "DecisionProperties")
                         ?? Member(__instance, "currentMultipleChoicePageData")
                         ?? Member(__instance, "currentDecision")
                         ?? Member(__instance, "decision")
                         ?? Member(__instance, "m_Decision");
                var dataObj = idObj;   
                string id = DeepId(dataObj);
                
                var dp = Member(dataObj, "DecisionProperties") ?? Member(__instance, "DecisionProperties");
                var opts = FindCollection(__instance, dp) ?? FindCollection(__instance, dataObj);
                string optVars = DescribeOptions(opts);

                string isRadio = MemberText(__instance, idObj, "isRadio");
                string multiFlag = MemberText(__instance, idObj, "IsRadioMultipleChoice");
                string toggle = HasMember(__instance, idObj, "multipleChoicesToggleGroup") ? "有" : "无";

                string key = __instance.GetType().Name + "|" + id + "|" + (opts == null ? -1 : opts.Count)
                           + "|" + isRadio + "|" + multiFlag;
                if (!_probeSeen.Add(key)) return;          

                
                bool real = (opts != null) || !string.IsNullOrEmpty(id);
                string mname = __originalMethod == null ? "?" : __originalMethod.Name;
                if (!_probeByMethod.ContainsKey(mname)) _probeByMethod[mname] = 0;
                _probeByMethod[mname]++;
                _probeTriggers++;

                if (real)
                {
                    
                    
                    
                    
                    
                    
                    _probeRealTriggers++;
                    if (_probeLines >= ProbeLineCap)
                    {
                        _probeLinesCapDropped++;
                        
                        
                        if (_probeLinesCapDropped % 100 == 1) ProbeSum(true);
                        
                        
                        EconomyPatches.NoteTruncationForDisplay(
                            "页行", ProbeLineCap, _probeLines, _probeLinesCapDropped, "硬上限",
                            "IsRadioProbe.cs 的 ProbeLineCap（=500）");
                        
                        
                        return;
                    }
                    _probeLines++;
                    _log.LogInfo("[OI][probe][page] id=" + id
                        + " pageType=" + __instance.GetType().Name
                        + " opts=" + (opts == null ? "不存在" : opts.Count.ToString())
                        + " optVars=[" + optVars + "]"
                        + " isRadio=" + isRadio + " multiFlag=" + multiFlag + " toggleGroup=" + toggle
                        + " method=" + mname);
                }
                else
                {
                    
                    _probeEmptyLines++;
                    if (!_probeEmptyByMethod.ContainsKey(mname)) _probeEmptyByMethod[mname] = 0;
                    _probeEmptyByMethod[mname]++;
                    if (_probeEmptyLogged.Add(mname))
                    {
                        _log.LogInfo("[OI][probe][page][empty] pageType=" + __instance.GetType().Name
                            + " method=" + mname
                            + " （该面板此刻还没有页数据 ✗ —— **同方法后续只计数、不再逐条打印** ✓，见 [sum] 汇总 ✓）");
                    }
                }
                
                if (real || (_probeTriggers % 25 == 0)) ProbeSum(false);
            }
            catch { }   
        }

        
        private static void ProbeSum(bool dropped)
        {
            try
            {
                var all = new StringBuilder();
                foreach (var kv in _probeByMethod)
                {
                    if (all.Length > 0) all.Append(" · ");
                    all.Append(kv.Key).Append('=').Append(kv.Value);
                }
                var emp = new StringBuilder();
                foreach (var kv in _probeEmptyByMethod)
                {
                    if (emp.Length > 0) emp.Append(" · ");
                    emp.Append(kv.Key).Append('×').Append(kv.Value);
                }
                _log.LogInfo("[OI][probe][sum] 触发总数=" + _probeTriggers
                    + " 真行触发=" + _probeRealTriggers
                    + " 真行上行=" + _probeLines + "/" + ProbeLineCap
                    + " 真行因上限丢弃=" + _probeLinesCapDropped
                    + " 空壳行=" + _probeEmptyLines
                    + " ｜按方法：" + (all.Length == 0 ? "（无）" : all.ToString())
                    + " ｜空壳分布：" + (emp.Length == 0 ? "（无）" : emp.ToString())
                    
                    
                    + (dropped ? " ⚠ 真行已超上限 ⇒ 已丢弃且丢弃数计入『真行因上限丢弃』（并可被悬停明细读取）✓" : "")
                    + " ｜判读：**真行里出现带 id= 与 pageType 的行** ⇒ 那几页走的就是这两个面板 ✓；"
                    + "若只有空壳行 ⇒ 那几页**不走**这两个面板 ✗");
            }
            catch { }
        }

        private static bool NameMatches(string n)
        {
            if (string.IsNullOrEmpty(n)) return false;
            
            if (n.StartsWith("get_", StringComparison.Ordinal)) return false;
            if (n.StartsWith("set_", StringComparison.Ordinal)) return false;
            if (n.StartsWith("Is", StringComparison.Ordinal)) return false;
            if (n.StartsWith("Has", StringComparison.Ordinal)) return false;
            for (int i = 0; i < ProbeMethodTokens.Length; i++)
                if (n.IndexOf(ProbeMethodTokens[i], StringComparison.Ordinal) >= 0) return true;
            return false;
        }

        
        private static object Member(object o, string name)
        {
            if (o == null || string.IsNullOrEmpty(name)) return null;
            try
            {
                var t = o.GetType();
                var f = t.GetField(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (f != null) return f.GetValue(o);
                var p = t.GetProperty(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (p != null && p.CanRead && p.GetIndexParameters().Length == 0) return p.GetValue(o);
            }
            catch { }
            return null;
        }

        private static bool HasMember(object a, object b, string name)
        {
            return Member(a, name) != null || Member(b, name) != null
                || HasMemberDeclared(TypeOf(a), name) || HasMemberDeclared(TypeOf(b), name);
        }

        private static bool HasMemberDeclared(Type t, string name)
        {
            if (t == null) return false;
            try
            {
                return t.GetField(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance) != null
                    || t.GetProperty(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance) != null
                    || FindMethod(t, name) != null;
            }
            catch { return false; }
        }

        
        private static MethodInfo FindMethod(Type t, string name)
        {
            if (t == null || string.IsNullOrEmpty(name)) return null;
            try
            {
                var ms = t.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                for (int i = 0; i < ms.Length; i++)
                    if (ms[i] != null && string.Equals(ms[i].Name, name, StringComparison.Ordinal)) return ms[i];
            }
            catch { }
            return null;
        }

        private static string MemberText(object a, object b, string name)
        {
            var v = Member(a, name);
            if (v == null) v = Member(b, name);
            if (v != null) return Short(v);
            if (HasMemberDeclared(TypeOf(a), name) || HasMemberDeclared(TypeOf(b), name)) return "有成员但读不到值";
            return "不存在";
        }

        private static Type TypeOf(object o)
        {
            try { return o == null ? null : o.GetType(); }
            catch { return null; }
        }

        
        private const int ProbePageHookCap = 200;
        
        private static int _probePageLines;
        
        private static int _probePageCapDropped;
        
        
        
        private static int _probePageDedupDropped;
        
        private static int _probePageTriggers;
        
        private static readonly HashSet<string> _probePageSeen = new HashSet<string>(StringComparer.Ordinal);
        
        private static readonly Dictionary<string, int> _probePageByMethod = new Dictionary<string, int>(StringComparer.Ordinal);

        
        
        
        
        
        
        
        
        
        
        
        
        
        private static void InstallIsRadioProbePageHooks(Harmony harmony, Assembly asm)
        {
            try
            {
                var t = Reflect.FindType(asm, "TemplatePagedDecisionsPage");
                if (t == null)
                {
                    _log.LogWarning("[OI][probe][B2] **未挂上**：找不到 TemplatePagedDecisionsPage"
                        + " ⇒ 分页多选族本轮**观测不到**（如实报告，不静默 ✗）");
                    return;
                }

                var postSetup = new HarmonyMethod(typeof(EconomyPatches).GetMethod(
                    nameof(Post_ProbePagedSetup), BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public));
                var postMulti = new HarmonyMethod(typeof(EconomyPatches).GetMethod(
                    nameof(Post_ProbeMultipleChoicePage), BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public));
                var postRadio = new HarmonyMethod(typeof(EconomyPatches).GetMethod(
                    nameof(Post_ProbeIsRadioMultipleChoice), BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public));

                int total = 0;
                total += ProbePatchByName(harmony, postSetup, t, "Setup", "只读");
                total += ProbePatchByName(harmony, postMulti, t, "SetupMultipleChoicePage", "只读");
                total += ProbePatchByName(harmony, postRadio, t, "IsRadioMultipleChoice", "只读（放行真值）");

                if (total > 0)
                {
                    _log.LogInfo("[OI][probe][B2] 已挂 " + total + " 个 Postfix（TemplatePagedDecisionsPage）");
                }
                else
                {
                    _log.LogWarning("[OI][probe][B2] **未挂上任何 Postfix**：TemplatePagedDecisionsPage 在，"
                        + "但三个方法名一个都没匹配到 ⇒ 本轮**观测不到**多选族（如实报告 ✗）");
                }
            }
            catch (Exception e)
            {
                _log.LogWarning("[OI][probe][B2] 安装异常（已忽略，不影响游戏）：" + e.Message);
            }
        }

        
        private static int ProbePatchByName(Harmony harmony, HarmonyMethod post, Type t, string name, string tag)
        {
            int cand = 0, hit = 0;
            try
            {
                var ms = t.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                for (int i = 0; i < ms.Length; i++)
                {
                    var m = ms[i];
                    if (m == null || !string.Equals(m.Name, name, StringComparison.Ordinal)) continue;
                    cand++;
                    try { harmony.Patch(m, postfix: post); hit++; }
                    catch (Exception e) { _log.LogWarning("[OI][probe][B2] 挂载失败（跳过）：" + name + " — " + e.Message); }
                }
                
                _log.LogDebug("[OI][probe][B2] 挂载自证：" + tag + " " + name
                    + " 候选=" + cand + " 挂上=" + hit);
            }
            catch (Exception e) { _log.LogWarning("[OI][probe][B2] 挂载异常（忽略）：" + name + " — " + e.Message); }
            return hit;
        }

        
        private static void Post_ProbePagedSetup(object[] __args, MethodBase __originalMethod)
        {
            try
            {
                if (EconomyConfig.IsRadioProbe == null || !EconomyConfig.IsRadioProbeOn) return;
                if (__args == null || __args.Length < 1) return;
                var page = __args[0];
                if (page == null) return;
                string line = "[OI][probe][B2][setup] pageClass=" + page.GetType().Name
                    + " id=" + IdentifyPage(page)
                    + " 参数个数=" + __args.Length
                    + " 重载=" + ShortMethod(__originalMethod);
                ProbePageLog("Setup", page, line);
            }
            catch { }
        }

        
        
        
        
        
        private static void Post_ProbeMultipleChoicePage(object[] __args, MethodBase __originalMethod)
        {
            try
            {
                if (EconomyConfig.IsRadioProbe == null || !EconomyConfig.IsRadioProbeOn) return;
                if (__args == null || __args.Length < 1) return;
                var page = __args[0];
                if (page == null) return;
                var props = Member(page, "MultipleChoicePageProperties");
                string id = IdentifyPage(page);
                string cnt = CountOptions(page, props);
                
                string diag = (cnt == "?") ? " ｜ " + DescribeOptionsTypeForLog(page, props) : "";
                string line = "[OI][probe][B2][multi] pageClass=" + page.GetType().Name
                    + " id=" + id
                    + " props=" + (props == null ? "不存在" : props.GetType().Name)
                    + " opMin=" + (props == null ? "?" : Short(Member(props, "MinimumChoiceCount")))
                    + " opMax=" + (props == null ? "?" : Short(Member(props, "MaximumChoiceCount")))
                    + " 选项数=" + cnt
                    + " 重载=" + ShortMethod(__originalMethod)
                    + diag;
                ProbePageLog("SetupMultipleChoicePage", page, line);
            }
            catch { }
        }

        
        
        
        
        
        
        private static void Post_ProbeIsRadioMultipleChoice(object __instance, object[] __args, object __result, MethodBase __originalMethod)
        {
            try
            {
                if (EconomyConfig.IsRadioProbe == null || !EconomyConfig.IsRadioProbeOn) return;
                if (__args == null || __args.Length < 1) return;
                var page = __args[0];
                var props = page == null ? null : Member(page, "MultipleChoicePageProperties");
                string line = "[OI][probe][B2][radio] 返回=" + Short(__result)
                    + " 参数Class=" + (page == null ? "null" : page.GetType().Name)
                    + " id=" + (page == null ? "(无)" : IdentifyPage(page))
                    + " opMax=" + (props == null ? "?" : Short(Member(props, "MaximumChoiceCount")))
                    + " 实例=" + InstanceName(__instance)
                    + " 重载=" + ShortMethod(__originalMethod);
                ProbePageLog("IsRadioMultipleChoice", page ?? __instance, line);
            }
            catch { }
        }

        
        private static string IdentifyPage(object o)
        {
            try
            {
                if (o == null) return "(无身份)";
                var v = Member(o, "NameInDatabase") ?? Member(o, "nameInDatabase")
                     ?? Member(o, "Path") ?? Member(o, "Id") ?? Member(o, "id");
                if (v == null) return "(无身份)";
                var s = v.ToString();
                return string.IsNullOrEmpty(s) ? "(无身份)" : Short(s);
            }
            catch { return "(无身份)"; }
        }

        
        
        
        
        
        
        private static object MemberDiag(object o, string name, out string err)
        {
            err = null;
            if (o == null || string.IsNullOrEmpty(name)) return null;
            try
            {
                var t = o.GetType();
                var f = t.GetField(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (f != null) return f.GetValue(o);
                var p = t.GetProperty(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (p != null && p.CanRead && p.GetIndexParameters().Length == 0) return p.GetValue(o);
                err = "无此成员";
            }
            catch (Exception e) { err = e.GetType().Name + ":" + Short(e.Message); }
            return null;
        }

        
        
        
        
        
        private static string DescribeOptionsTypeForLog(object page, object props)
        {
            try
            {
                
                string why;
                object v = MemberDiag(props, "Options", out why);
                object host = props;
                string hostTag = "props";
                if (v == null)
                {
                    string why2;
                    v = MemberDiag(page, "Options", out why2);
                    host = page;
                    hostTag = "page";
                    why = "props:" + (why ?? "-") + "／page:" + (why2 ?? "-");
                }
                return "类型诊断=成员源[" + hostTag + ":" + TypeName(host) + "]"
                     + "值类型[" + TypeName(v) + "]"
                     + "集合接口[" + CollectionInterfaces(v) + "]"
                     + "取法[" + Sanitize(why, 120) + "]";
            }
            catch (Exception e) { return "类型诊断=(自身异常 " + e.GetType().Name + ")"; }
        }

        
        private static string Sanitize(string s, int max)
        {
            if (string.IsNullOrEmpty(s)) return "-";
            var b = new StringBuilder(s.Length);
            for (int i = 0; i < s.Length && b.Length < max; i++)
            {
                char c = s[i];
                b.Append((c == '\n' || c == '\r' || c == '\t' || c == '｜' || c == ';') ? ' ' : c);
            }
            return b.ToString();
        }

        
        private static string TypeName(object o)
        {
            try { return o == null ? "null" : o.GetType().FullName; }
            catch { return "?(取型失败)"; }
        }

        
        private static string CollectionInterfaces(object v)
        {
            try
            {
                if (v == null) return "无(值为 null)";
                var t = v.GetType();
                var names = new List<string>();
                foreach (var i in t.GetInterfaces())
                {
                    var n = i.Name;
                    if (n == "ICollection" || n == "IList" || n == "IDictionary" || n.StartsWith("ICollection`") || n.StartsWith("IList`") || n.StartsWith("IReadOnlyCollection`"))
                        names.Add(n);
                }
                return names.Count == 0 ? "（无非泛型 IList／ICollection）" : string.Join(",", names.ToArray());
            }
            catch { return "?(取接口失败)"; }
        }

        
        
        
        
        
        private static int? CountOf(object v)
        {
            try
            {
                if (v == null) return null;
                var c = v as ICollection;                       
                if (c != null) return c.Count;
                var t = v.GetType();
                var pc = t.GetProperty("Count", BindingFlags.Public | BindingFlags.Instance);   
                if (pc != null && pc.CanRead && pc.PropertyType == typeof(int)) return (int)pc.GetValue(v);
                var pl = t.GetProperty("Length", BindingFlags.Public | BindingFlags.Instance);  
                if (pl != null && pl.CanRead && pl.PropertyType == typeof(int)) return (int)pl.GetValue(v);
                var en = v as IEnumerable;                      
                if (en != null) { int k = 0; foreach (var _ in en) k++; return k; }
                return null;
            }
            catch { return null; }
        }

        
        private static string CountOptions(object page, object props)
        {
            try
            {
                object v = Member(props, "Options");
                if (v == null) v = Member(page, "Options");
                var n = CountOf(v);
                return n.HasValue ? n.Value.ToString() : "?";
            }
            catch { return "?"; }
        }

        
        private static string InstanceName(object o)
        {
            try
            {
                if (o == null) return "(无实例)";
                var n = Member(o, "name");
                if (n != null)
                {
                    var s = n.ToString();
                    if (!string.IsNullOrEmpty(s)) return Short(s);
                }
                return o.GetType().Name;
            }
            catch { return "(读不到)"; }
        }

        
        private static string ShortMethod(MethodBase m)
        {
            try
            {
                if (m == null) return "?";
                var ps = m.GetParameters();
                var sb = new StringBuilder();
                sb.Append(m.Name).Append('/').Append(ps.Length);
                for (int i = 0; i < ps.Length; i++) sb.Append(i == 0 ? '(' : ',').Append(ps[i].ParameterType.Name);
                if (ps.Length > 0) sb.Append(')');
                return sb.ToString();
            }
            catch { return "?"; }
        }

        
        private static void ProbePageLog(string method, object instance, string line)
        {
            try
            {
                _probePageTriggers++;
                if (!_probePageByMethod.ContainsKey(method)) _probePageByMethod[method] = 0;
                _probePageByMethod[method]++;

                string key = method + "|" + InstanceName(instance) + "|" + line;
                if (!_probePageSeen.Add(key)) { _probePageDedupDropped++; return; }      
                if (_probePageLines >= ProbePageHookCap)
                {
                    
                    
                    _probePageCapDropped++;
                    EconomyPatches.NoteTruncationForDisplay(
                        "页钩", ProbePageHookCap, _probePageLines, _probePageCapDropped, "硬上限",
                        "IsRadioProbe.cs 的 ProbePageHookCap（=200）");
                    if (_probePageTriggers % 50 == 0) ProbePageSum(true);
                    return;
                }
                _probePageLines++;
                _log.LogInfo(line);
                ProbePageSum(false);
            }
            catch { }
        }

        
        private static void ProbePageSum(bool dropped)
        {
            try
            {
                var sb = new StringBuilder();
                foreach (var kv in _probePageByMethod)
                {
                    if (sb.Length > 0) sb.Append(" · ");
                    sb.Append(kv.Key).Append('=').Append(kv.Value);
                }
                _log.LogInfo("[OI][probe][B2][sum] 触发总数=" + _probePageTriggers
                    + " 去重后行=" + _probePageLines + "/" + ProbePageHookCap
                    + " 因上限丢弃=" + _probePageCapDropped
                    + " 因去重丢弃=" + _probePageDedupDropped
                    + " ｜按方法：" + (sb.Length == 0 ? "（无）" : sb.ToString())
                    
                    + (dropped ? " ⚠ 已超上限 ⇒ 已丢弃且丢弃数分别计入『因上限丢弃』与『因去重丢弃』✓" : "")
                    + " ｜判读：**出现 [B2][multi] 行** ⇒ 该页走的是分页多选族 ⇒ opMax 就是「能选几个」的真值 ✓；"
                    + "若从无 [B2] 行而 [page] 行有 ⇒ 那些决策**不走**这一族 ✗");
            }
            catch { }
        }

        
        private static void AppendMemberShape(StringBuilder sb, Type t, string name)
        {
            try
            {
                var f = t.GetField(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
                if (f != null) { sb.Append("·").Append(name).Append(":字段(").Append(f.FieldType.Name).Append(") "); return; }
                var p = t.GetProperty(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
                if (p != null) { sb.Append("·").Append(name).Append(":属性(").Append(p.PropertyType.Name).Append(") "); return; }
                var m = FindMethod(t, name);
                if (m != null)
                {
                    sb.Append("·").Append(name).Append(":方法->").Append(m.ReturnType.Name)
                      .Append("(").Append(m.GetParameters().Length).Append("参) ");
                }
            }
            catch { }
        }

        
        private static IList FindCollection(object page, object data)
        {
            var hints = new[] { "Choices", "choices", "Options", "options", "MultipleChoiceOptionData",
                                "instantiatedMultipleChoiceOptions", "m_Choices", "m_Options" };
            foreach (var o in new[] { page, data })
            {
                if (o == null) continue;
                foreach (var h in hints)
                {
                    var v = Member(o, h) as IList;
                    if (v != null) return v;
                }
                
                try
                {
                    var t = o.GetType();
                    foreach (var f in t.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
                    {
                        if (f.Name.IndexOf("Choice", StringComparison.Ordinal) < 0
                            && f.Name.IndexOf("Option", StringComparison.Ordinal) < 0) continue;
                        var v = f.GetValue(o) as IList;
                        if (v != null) return v;
                    }
                    foreach (var p in t.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
                    {
                        if (!p.CanRead || p.GetIndexParameters().Length != 0) continue;
                        if (p.Name.IndexOf("Choice", StringComparison.Ordinal) < 0
                            && p.Name.IndexOf("Option", StringComparison.Ordinal) < 0) continue;
                        var v = p.GetValue(o) as IList;
                        if (v != null) return v;
                    }
                }
                catch { }
            }
            return null;
        }

        
        
        private static string DeepId(object o)
        {
            try
            {
                if (o == null) return "null";
                var v = Member(o, "NameInDatabase") ?? Member(o, "nameInDatabase")
                     ?? Member(o, "Id") ?? Member(o, "id");
                if (v != null)
                {
                    var s = v.ToString();
                    if (!string.IsNullOrEmpty(s)) return Short(s);
                }
                return Short(o);
            }
            catch { return "?"; }
        }

        
        private static string DescribeOptions(IList opts)
        {
            if (opts == null) return "不存在";
            var sb = new StringBuilder();
            int n = Math.Min(opts.Count, 30);
            for (int i = 0; i < n; i++)
            {
                if (i > 0) sb.Append(',');
                object e = null;
                try { e = opts[i]; } catch { }
                if (e == null) { sb.Append("null"); continue; }
                
                
                var v = Member(e, "Variable") ?? Member(e, "variable") ?? Member(e, "NameInDatabase")
                     ?? Member(e, "Text") ?? Member(e, "text") ?? Member(e, "Name") ?? Member(e, "name")
                     ?? Member(e, "Id");
                if (v == null) { sb.Append(e.GetType().Name); continue; }
                var s = v.ToString().Replace('\n', ' ').Replace('\r', ' ').Trim();
                sb.Append(s.Length <= 20 ? s : s.Substring(0, 20) + "…");
            }
            if (opts.Count > n) sb.Append("…(+" + (opts.Count - n) + ")");
            return sb.ToString();
        }

        
        private static string Short(object o)
        {
            if (o == null) return "null";
            string s;
            try { s = o.ToString(); } catch { return "ToString异常"; }
            if (s == null) return "null";
            s = s.Replace('\n', ' ').Replace('\r', ' ').Trim();
            return s.Length <= 80 ? s : s.Substring(0, 80) + "…";
        }
    }
}
