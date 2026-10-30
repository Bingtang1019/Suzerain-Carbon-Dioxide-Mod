using System;
using System.Collections;
using System.Reflection;
using HarmonyLib;

namespace Oikonomia
{
    
    
    
    
    
    
    internal static class Reflect
    {
        public static Type FindType(Assembly asm, string name)
        {
            try { return asm == null ? null : asm.GetType(name); }
            catch { return null; }
        }

        
        public static MethodInfo FindMethod(Type type, params string[] names)
        {
            if (type == null) return null;
            foreach (var n in names)
            {
                try
                {
                    var m = AccessTools.Method(type, n);
                    if (m != null) return m;
                }
                catch { }
            }
            return null;
        }

        public static bool Usable(object obj)
        {
            if (obj == null) return false;
            try
            {
                var b = obj as Il2CppInterop.Runtime.InteropTypes.Il2CppObjectBase;
                if (b != null && b.Pointer == IntPtr.Zero) return false;
            }
            catch { return false; }
            return true;
        }

        public static object Prop(object obj, string name)
        {
            if (!Usable(obj)) return null;
            try
            {
                var t = obj.GetType();
                var p = AccessTools.Property(t, name);
                if (p != null && p.CanRead) return p.GetValue(obj);
                var m = AccessTools.Method(t, "get_" + name);
                if (m != null) return m.Invoke(obj, null);
                var f = AccessTools.Field(t, name);
                if (f != null) return f.GetValue(obj);
            }
            catch { }
            return null;
        }

        
        public static object Static(object type, params string[] names)
        {
            var t = type as Type;
            if (t == null) return null;
            foreach (var n in names)
            {
                try
                {
                    var p = t.GetProperty(n, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
                    if (p != null && p.CanRead) return p.GetValue(null);
                    var f = t.GetField(n, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
                    if (f != null) return f.GetValue(null);
                }
                catch { }
            }
            return null;
        }

        
        public static object Call0(object obj, string name)
        {
            if (!Usable(obj)) return null;
            try
            {
                var m = obj.GetType().GetMethod(name,
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                    null, Type.EmptyTypes, null);
                return m == null ? null : m.Invoke(obj, null);
            }
            catch { return null; }
        }
        public static object Call1(object obj, string name, object arg)        {
            if (!Usable(obj)) return null;
            try
            {
                var t = obj.GetType();
                var flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
                foreach (var m in t.GetMethods(flags))
                {
                    if (m.Name != name) continue;
                    if (m.GetParameters().Length == 1) return m.Invoke(obj, new[] { arg });
                }
            }
            catch { }
            return null;
        }

        public static int ToInt(object o)
        {
            try { return o == null ? 0 : Convert.ToInt32(o); } catch { return 0; }
        }

        public static bool ToBool(object o)
        {
            try { return o != null && Convert.ToBoolean(o); } catch { return false; }
        }

        public static string Str(object o)
        {
            try { return o == null ? null : o.ToString(); } catch { return null; }
        }

        public static int ListCount(object list)
        {
            if (!Usable(list)) return -1;
            try
            {
                var o = Prop(list, "Count");
                if (o != null) return ToInt(o);
                var c = list as ICollection;
                if (c != null) return c.Count;
            }
            catch { }
            return -1;
        }

        public static bool SetPropOrField(object obj, string name, object value)
        {
            if (!Usable(obj)) return false;
            try
            {
                var t = obj.GetType();
                var p = AccessTools.Property(t, name);
                if (p != null && p.CanWrite) { p.SetValue(obj, value); return true; }
                var f = AccessTools.Field(t, name);
                if (f != null) { f.SetValue(obj, value); return true; }
            }
            catch { }
            return false;
        }

        
        
        
        
        
        public static string SetPropOrFieldReason(object obj, string name, object value)
        {
            if (!Usable(obj)) return "对象不可用（null 或 Il2Cpp 空指针）";
            return MemberWriteCore.SetPropOrFieldReason(obj, name, value);
        }
    }
}
