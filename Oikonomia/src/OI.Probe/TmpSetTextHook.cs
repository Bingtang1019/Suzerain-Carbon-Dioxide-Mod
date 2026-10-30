using System;
using System.Reflection;
using HarmonyLib;

namespace Oikonomia.Probe
{
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    internal static class TmpSetTextHook
    {
        
        public static readonly string[] CandidateTypes = { "TMPro.TextMeshProUGUI", "TMPro.TextMeshPro" };
        private static readonly string[] AsmCandidates = { "Unity.TextMeshPro", "UnityEngine.TextMeshPro" };

        
        
        
        
        public static void Install(Harmony harmony, Type tmpBaseType, MethodInfo postfix)
        {
            if (harmony == null || postfix == null) return;
            bool any = false;

            foreach (var n in CandidateTypes)
            {
                var t = SafeResolve.FindTypePreferred(n, AsmCandidates);
                var setter = Setter(t);
                if (setter == null)
                {
                    RiziaLog.Line("[R][mount] · " + n + ".set_text 不可用（类型名不可解析 / 不存在）→ 跳过");
                    continue;
                }
                
                
                if (setter.DeclaringType != t)
                {
                    RiziaLog.Line("[R][mount] · " + n + " 自己没有 set_text（解析到基类 "
                                  + SafeResolve.Describe(setter) + " 的声明）→ 跳过，绝不改挂声明");
                    continue;
                }
                if (Patch(harmony, setter, n, postfix)) any = true;
            }

            if (!any)
                RiziaLog.Error("[R][mount] ✗ ① 属性赋值路径（set_text）不可用 → 按设计降级："
                               + "只保留 SetText 直写路径 + 生产者钩子 + 槽位快照（这三条足以覆盖① 渲染文本原始值）");
        }

        
        private static MethodInfo Setter(Type t)
        {
            if (t == null) return null;
            try
            {
                var p = t.GetProperty("text", BindingFlags.Instance | BindingFlags.Public
                    | BindingFlags.NonPublic | BindingFlags.Static);
                var setter = p == null ? null : p.GetSetMethod(true);
                if (setter != null) return setter;
            }
            catch { }
            try { return t.GetMethod("set_text", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                null, new[] { typeof(string) }, null); }
            catch { return null; }
        }

        
        private static bool Patch(Harmony harmony, MethodInfo setter, string label, MethodInfo postfix)
        {
            
            
            
            
            
            
            
            
            var refusal = SafeResolve.PatchRefusal(setter);
            if (refusal != null)
            {
                RiziaLog.Error("[R][mount] ✗ " + label + ".set_text **拒绝挂载**：" + refusal);
                return false;
            }
            try
            {
                harmony.Patch(setter, postfix: new HarmonyMethod(postfix));
                RiziaLog.Mark("[R][mount] ✓ " + label + ".set_text(string)"
                              + "  ← ① 属性赋值路径（已通过体检：" + SafeResolve.Describe(setter) + "）");
                return true;
            }
            catch (Exception e)
            {
                RiziaLog.Error("[R][mount] ✗ " + label + ".set_text 挂载失败：" + e.GetType().Name + " : " + e.Message);
                return false;
            }
        }
    }
}
