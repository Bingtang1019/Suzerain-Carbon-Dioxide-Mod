using System;
using System.Collections.Generic;
using Il2Cpp = Il2CppInterop.Runtime.IL2CPP;

namespace Oikonomia.Probe
{
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    internal static class ProbeNativeGuard
    {
        private static readonly object _lock = new object();
        private static readonly HashSet<string> _noted = new HashSet<string>(StringComparer.Ordinal);
        private static int _gameThreadId;
        private static int _refused;

        
        public static int RefusedCount { get { return _refused; } }

        
        public static bool Bound { get { lock (_lock) { return _gameThreadId != 0; } } }

        public static int GameThreadId { get { lock (_lock) { return _gameThreadId; } } }

        
        public static bool OnGameThread
        {
            get
            {
                int id;
                lock (_lock) { id = _gameThreadId; }
                if (id == 0) return false;
                return Environment.CurrentManagedThreadId == id;
            }
        }

        
        public static bool NativeAllowed
        {
            get
            {
                try
                {
                    return ProbeTier.Tier == ProbeTier.Full
                           && ProbeTier.NativeChannel
                           && !ProbeTier.StartupPhase
                           && OnGameThread;
                }
                catch { return false; }
            }
        }

        
        public static void BindGameThread(string why)
        {
            int id;
            lock (_lock)
            {
                if (_gameThreadId != 0) return;
                _gameThreadId = Environment.CurrentManagedThreadId;
                id = _gameThreadId;
            }
            try
            {
                ProbeLog.Mark("[OI][④] 游戏线程已绑定 tid=" + id + "（" + why + "）"
                              + " ｜ 此后只有该线程允许触碰 il2cpp");
            }
            catch { }
        }

        
        public static bool Alive(IntPtr p)
        {
            return p != IntPtr.Zero;
        }

        
        
        
        
        public static bool Probe(IntPtr objPtr, string what)
        {
            if (!NativeAllowed) { Note("Probe(" + what + ")"); return false; }
            if (!Alive(objPtr)) return false;
            try
            {
                var k = Il2Cpp.il2cpp_object_get_class(objPtr);
                if (k == IntPtr.Zero) return false;
                var nm = Il2Cpp.il2cpp_class_get_name_(k);
                return !string.IsNullOrEmpty(nm);
            }
            catch { return false; }
        }

        
        public static void Note(string api)
        {
            _refused++;
            var key = string.IsNullOrEmpty(api) ? "?" : api;
            bool first;
            lock (_lock) { first = _noted.Add(key); }
            if (!first) return;
            try
            {
                ProbeLog.Mark("[OI][④] 原生访问被拒（同类只记一次）：" + key
                              + " ｜ tier=" + ProbeTier.TierName
                              + " nativeChannel=" + ProbeTier.NativeChannel
                              + " 启动期=" + ProbeTier.StartupPhase
                              + " 当前线程=游戏线程? " + OnGameThread);
            }
            catch { }
        }

        public static string Describe()
        {
            return "原生闸门：tier=" + ProbeTier.TierName
                   + " nativeChannel=" + ProbeTier.NativeChannel
                   + " 启动期=" + ProbeTier.StartupPhase
                   + " 游戏线程tid=" + GameThreadId
                   + " 本线程在游戏线程=" + OnGameThread
                   + " 拒绝次数=" + _refused;
        }
    }
}
