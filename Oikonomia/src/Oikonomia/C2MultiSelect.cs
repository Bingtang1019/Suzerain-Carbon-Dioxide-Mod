
























using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using HarmonyLib;

namespace Oikonomia
{
    internal static partial class EconomyPatches
    {
        private static int _c2Hooks;
        private static int _c2Lines;
        private const int C2LineCap = 80;
        
        private static readonly HashSet<string> _c2Done = new HashSet<string>(StringComparer.Ordinal);
        
        private static readonly HashSet<string> _c2Families = new HashSet<string>(StringComparer.Ordinal);

        
        
        
        
        internal static void InstallC2MultiSelect(Harmony harmony, Assembly asm, ref int ok, ref int miss)
        {
            try
            {
                if (EconomyConfig.C2MultiSelect == null || !EconomyConfig.C2MultiSelectOn)
                {
                    _log.LogDebug("[OI][c2] 决策多选补丁：已关闭（未挂任何钩子）");
                    return;
                }
                if (asm == null) { _log.LogWarning("[OI][c2] 跳过：Assembly-CSharp 未加载"); return; }

                var post = new HarmonyMethod(typeof(EconomyPatches).GetMethod(
                    nameof(Post_C2PageWrite), BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public));

                
                int hit = 0;
                hit += C2Patch(harmony, post, asm, "TemplatePagedDecisionsPage", "SetupMultipleChoicePage", 1, ref miss, "主挂点");
                hit += C2Patch(harmony, post, asm, "TemplatePagedDecisionsPage", "Setup", 1, ref miss, "备挂点（更早）");
                hit += C2Patch(harmony, post, asm, "PagedDecisionPanel", "CreatePage", 1, ref miss, "备挂点（最早）");
                _c2Hooks = hit;

                if (hit > 0) { ok++; }
                else { miss++; _log.LogWarning("[OI][c2] **未挂上任何挂点** ⇒ 本轮 C2 不生效（如实报告，不静默）"); }

                var cf = EconomyConfig.C2Prefixes != null ? EconomyConfig.C2Prefixes.Value : "";
                _log.LogInfo("[OI][c2] 安装完成：钩子 " + hit + " 个｜目标族前缀 = [" + cf + "]｜"
                    + "min=" + C2Min + " max=" + C2Max + "｜回退：cfg [C2] MultiSelect=false（关掉即完全无副作用 ✓）");
            }
            catch (Exception e)
            {
                _log.LogWarning("[OI][c2] 安装异常（已忽略，不影响游戏）：" + e.Message);
            }
        }

        private static int C2Patch(Harmony harmony, HarmonyMethod post, Assembly asm,
            string typeName, string methodName, int paramCount, ref int miss, string tag)
        {
            try
            {
                var t = Reflect.FindType(asm, typeName);
                if (t == null) { _log.LogWarning("[OI][c2] **未挂上**" + tag + "：找不到类型 " + typeName); return 0; }
                int n = 0;
                foreach (var m in t.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
                {
                    if (m == null || m.Name != methodName) continue;
                    if (m.GetParameters().Length != paramCount) continue;
                    try
                    {
                        harmony.Patch(m, postfix: post);
                        n++;
                        _log.LogInfo("[OI][c2] 已挂 Postfix：" + tag + " ⇒ " + typeName + "." + methodName
                            + "(" + paramCount + " 参数)");
                    }
                    catch (Exception e)
                    {
                        _log.LogWarning("[OI][c2] 挂载失败（跳过）：" + typeName + "." + methodName + " — " + e.Message);
                    }
                }
                if (n == 0) _log.LogWarning("[OI][c2] **未挂上**" + tag + "：" + typeName + "." + methodName
                    + "(" + paramCount + ") 不存在或签名不同");
                return n;
            }
            catch (Exception e) { _log.LogWarning("[OI][c2] 挂点异常（忽略）：" + tag + " — " + e.Message); return 0; }
        }

        private static int C2Min
        {
            get { return EconomyConfig.C2MinChoices != null ? EconomyConfig.C2MinChoices.Value : 1; }
        }
        private static int C2Max
        {
            get { return EconomyConfig.C2MaxChoices != null ? EconomyConfig.C2MaxChoices.Value : 9; }
        }

        
        private static void Post_C2PageWrite(object __0)
        {
            try
            {
                if (EconomyConfig.C2MultiSelect == null || !EconomyConfig.C2MultiSelectOn) return;
                var page = __0;                       
                if (page == null) return;
                if (_c2Lines >= C2LineCap) return;

                string id = Identify(page);            
                if (!InScope(id, out string family))
                {
                    
                    C2Log("[OI][c2][skip] page=" + Short(id) + " 不在目标族内（配置的前缀都不匹配）⇒ 保持原样 ✓");
                    return;
                }

                var props = Reflect.Prop(page, "MultipleChoicePageProperties");
                if (props == null)
                {
                    C2Log("[OI][c2][skip] page=" + Short(id) + " 族=" + family
                        + " ⇒ 该页**没有** MultipleChoicePageProperties（可能不是多选页 ✗）");
                    return;
                }

                object oldMin = Reflect.Prop(props, "MinimumChoiceCount");
                object oldMax = Reflect.Prop(props, "MaximumChoiceCount");
                int newMin = C2Min, newMax = C2Max;
                string key = id + "|" + newMin + "|" + newMax;
                if (_c2Done.Contains(key)) return;     

                bool w1 = Reflect.SetPropOrField(props, "MinimumChoiceCount", newMin);
                bool w2 = Reflect.SetPropOrField(props, "MaximumChoiceCount", newMax);
                _c2Done.Add(key);
                _c2Lines++;
                _c2Families.Add(family);

                object chkMin = Reflect.Prop(props, "MinimumChoiceCount");
                object chkMax = Reflect.Prop(props, "MaximumChoiceCount");
                C2Log("[OI][c2][hit] page=" + Short(id) + " 族=" + family
                    + " 旧=(min=" + S(oldMin) + ",max=" + S(oldMax) + ")"
                    + " 写=(min=" + newMin + ",max=" + newMax + ") 写入=" + (w1 && w2 ? "ok" : ("部分失败 min:" + w1 + " max:" + w2))
                    + " **回读=(min=" + S(chkMin) + ",max=" + S(chkMax) + ")**");
            }
            catch { }   
        }

        
        private static string Identify(object page)
        {
            try
            {
                var v = Reflect.Prop(page, "NameInDatabase") ?? Reflect.Prop(page, "nameInDatabase")
                     ?? Reflect.Prop(page, "Path") ?? Reflect.Prop(page, "Id");
                return v == null ? "" : v.ToString();
            }
            catch { return ""; }
        }

        
        private static bool InScope(string id, out string family)
        {
            family = "";
            if (string.IsNullOrEmpty(id)) return false;
            var raw = EconomyConfig.C2Prefixes != null ? EconomyConfig.C2Prefixes.Value : "";
            if (string.IsNullOrEmpty(raw)) return false;
            foreach (var p in raw.Split(new[] { '|', ';', ',' }, StringSplitOptions.RemoveEmptyEntries))
            {
                var q = p.Trim();
                if (q.Length == 0) continue;
                if (id.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0) { family = q; return true; }
            }
            return false;
        }

        private static void C2Log(string s)
        {
            try { _log.LogInfo(s); } catch { }
        }

        private static string S(object o) { return o == null ? "?" : o.ToString(); }

        private static string Short(string s)
        {
            if (string.IsNullOrEmpty(s)) return "(无身份)";
            s = s.Replace('\n', ' ').Replace('\r', ' ').Trim();
            return s.Length <= 64 ? s : s.Substring(0, 64) + "…";
        }
    }
}
