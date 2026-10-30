using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using HarmonyLib;

namespace Suzerain.PatchBus
{
    public enum PatchScope
    {
        Any = 0,
        Main = 1,
        Rizia = 2
    }

    public enum PatchHook
    {
        Postfix = 0,
        PrefixReadOnly = 1
    }

    public sealed class PatchContext
    {
        public object Instance;
        public object[] Args;
        public object Result;
        public string Owner;
        public PatchHook Hook;
    }

    public sealed class PatchSubscription : IDisposable
    {
        internal readonly MethodBase Target;
        internal readonly string Owner;
        internal readonly PatchHook Hook;
        internal readonly PatchScope Scope;
        internal readonly int Priority;
        internal readonly Action<PatchContext> Callback;
        internal bool Disposed;

        internal PatchSubscription(MethodBase target, string owner, PatchHook hook,
            PatchScope scope, int priority, Action<PatchContext> callback)
        {
            Target = target;
            Owner = owner;
            Hook = hook;
            Scope = scope;
            Priority = priority;
            Callback = callback;
        }

        public void Dispose()
        {
            if (Disposed) return;
            Disposed = true;
            PatchBus.Unsubscribe(this);
        }
    }

    
    
    
    
    public static class PatchBus
    {
        private sealed class TargetState
        {
            public MethodBase Target;
            public Harmony Harmony;
            public MethodInfo Postfix;
            public MethodInfo PostfixWithResult;
            public MethodInfo Prefix;
            public readonly List<PatchSubscription> Subs = new List<PatchSubscription>();
        }

        private static readonly object Gate = new object();
        private static readonly Dictionary<MethodBase, TargetState> States =
            new Dictionary<MethodBase, TargetState>();
        private static Func<string> _storyPackProvider;
        private static Action<string> _sink;

        public static void Configure(Func<string> storyPackProvider, Action<string> logSink)
        {
            lock (Gate)
            {
                _storyPackProvider = storyPackProvider;
                _sink = logSink;
            }
        }

        public static PatchSubscription SubscribePostfix(Harmony harmony, string owner,
            MethodBase target, PatchScope scope, int priority, Action<PatchContext> callback)
        {
            return Subscribe(harmony, owner, target, PatchHook.Postfix, scope, priority, callback);
        }

        public static PatchSubscription SubscribePrefixReadOnly(Harmony harmony, string owner,
            MethodBase target, PatchScope scope, int priority, Action<PatchContext> callback)
        {
            return Subscribe(harmony, owner, target, PatchHook.PrefixReadOnly, scope, priority, callback);
        }

        private static PatchSubscription Subscribe(Harmony harmony, string owner, MethodBase target,
            PatchHook hook, PatchScope scope, int priority, Action<PatchContext> callback)
        {
            if (harmony == null) throw new ArgumentNullException(nameof(harmony));
            if (target == null) throw new ArgumentNullException(nameof(target));
            if (callback == null) throw new ArgumentNullException(nameof(callback));
            owner = string.IsNullOrEmpty(owner) ? "unknown" : owner;
            lock (Gate)
            {
                TargetState st;
                if (!States.TryGetValue(target, out st))
                {
                    st = new TargetState { Target = target, Harmony = harmony };
                    States[target] = st;
                }
                if (hook == PatchHook.Postfix && st.Postfix == null && st.PostfixWithResult == null)
                {
                    if (target is MethodInfo && ((MethodInfo)target).ReturnType != typeof(void))
                    {
                        st.PostfixWithResult = typeof(PatchBus).GetMethod(nameof(PostfixDispatchWithResult),
                            BindingFlags.Static | BindingFlags.NonPublic);
                        harmony.Patch(target, postfix: new HarmonyMethod(st.PostfixWithResult));
                        Emit("[bus] install postfix(result) " + Describe(target));
                    }
                    else
                    {
                        st.Postfix = typeof(PatchBus).GetMethod(nameof(PostfixDispatch),
                            BindingFlags.Static | BindingFlags.NonPublic);
                        harmony.Patch(target, postfix: new HarmonyMethod(st.Postfix));
                        Emit("[bus] install postfix " + Describe(target));
                    }
                }
                if (hook == PatchHook.PrefixReadOnly && st.Prefix == null)
                {
                    st.Prefix = typeof(PatchBus).GetMethod(nameof(PrefixDispatch),
                        BindingFlags.Static | BindingFlags.NonPublic);
                    harmony.Patch(target, prefix: new HarmonyMethod(st.Prefix));
                    Emit("[bus] install prefix-readonly " + Describe(target));
                }
                var sub = new PatchSubscription(target, owner, hook, scope, priority, callback);
                st.Subs.Add(sub);
                st.Subs.Sort((a, b) => a.Priority.CompareTo(b.Priority));
                Emit("[bus] subscribe " + owner + " " + hook + " p" + priority
                    + " scope=" + scope + " -> " + Describe(target));
                return sub;
            }
        }

        internal static void Unsubscribe(PatchSubscription sub)
        {
            if (sub == null) return;
            lock (Gate)
            {
                TargetState st;
                if (!States.TryGetValue(sub.Target, out st)) return;
                st.Subs.Remove(sub);
                Emit("[bus] unsubscribe " + sub.Owner + " -> " + Describe(sub.Target));
                
            }
        }

        public static string DumpMatrix()
        {
            lock (Gate)
            {
                var sb = new StringBuilder();
                sb.AppendLine("[bus] targets=" + States.Count);
                foreach (var kv in States)
                {
                    var st = kv.Value;
                    sb.AppendLine("  " + Describe(st.Target)
                        + " prefix=" + (st.Prefix == null ? "no" : "yes")
                        + " postfix=" + (st.Postfix == null ? "no" : "yes")
                        + " subs=" + st.Subs.Count);
                    foreach (var s in st.Subs)
                        sb.AppendLine("    - " + s.Owner + " " + s.Hook
                            + " p" + s.Priority + " scope=" + s.Scope);
                }
                return sb.ToString();
            }
        }

        private static bool PrefixDispatch(object __instance, object[] __args, MethodBase __originalMethod)
        {
            
            Dispatch(PatchHook.PrefixReadOnly, __originalMethod, __instance, __args, null);
            return true;
        }

        private static void PostfixDispatch(object __instance, object[] __args, MethodBase __originalMethod)
        {
            Dispatch(PatchHook.Postfix, __originalMethod, __instance, __args, null);
        }

        private static void PostfixDispatchWithResult(object __instance, object[] __args,
            MethodBase __originalMethod, object __result)
        {
            Dispatch(PatchHook.Postfix, __originalMethod, __instance, __args, __result);
        }

        private static void Dispatch(PatchHook hook, MethodBase target, object instance, object[] args,
            object result)
        {
            PatchSubscription[] snapshot;
            string story;
            lock (Gate)
            {
                story = SafeStoryPack();
                TargetState st;
                snapshot = target != null && States.TryGetValue(target, out st)
                    ? st.Subs.Where(s => s.Hook == hook && !s.Disposed)
                        .OrderBy(s => s.Priority).ToArray()
                    : Array.Empty<PatchSubscription>();
            }
            foreach (var sub in snapshot)
            {
                if (!ScopeMatches(sub.Scope, story)) continue;
                try
                {
                    sub.Callback(new PatchContext
                    {
                        Instance = instance,
                        Args = args,
                        Result = result,
                        Owner = sub.Owner,
                        Hook = sub.Hook
                    });
                }
                catch (Exception e)
                {
                    Emit("[bus] subscriber " + sub.Owner + " threw " + e.GetType().Name
                        + " on " + Describe(sub.Target) + ": " + e.Message);
                }
            }
        }

        private static bool ScopeMatches(PatchScope scope, string story)
        {
            if (scope == PatchScope.Any) return true;
            if (string.IsNullOrEmpty(story)) return false;
            var rizia = story.IndexOf("Rizia", StringComparison.OrdinalIgnoreCase) >= 0;
            return scope == PatchScope.Rizia ? rizia : !rizia;
        }

        private static string SafeStoryPack()
        {
            try { return _storyPackProvider == null ? null : _storyPackProvider(); }
            catch { return null; }
        }

        private static string Describe(MethodBase m)
        {
            if (m == null) return "<null>";
            return m.DeclaringType == null ? m.Name : m.DeclaringType.Name + "." + m.Name;
        }

        private static void Emit(string line)
        {
            try { _sink?.Invoke(line); } catch { }
        }
    }
}
