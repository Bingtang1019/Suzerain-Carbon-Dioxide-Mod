using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Text;
using System.Threading;
using BepInEx.Logging;
using HarmonyLib;

namespace Oikonomia
{
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    internal static partial class EconomyPatches
    {
        
        
        private static bool _detailPathA;

        private static int _tooltipCallCount;      
        private static bool _observed;             
        private static string _lastTooltipText = "";

        
        private static bool _firstTooltipLogged;   
        private static bool _budgetAdoptLogged;    
        private static bool _statMissLogged;       
        private static bool _slotMissLogged;       
        private static bool _slotLogged;           
        private static readonly HashSet<string> _readBackLogged = new HashSet<string>(StringComparer.Ordinal); 
        private static bool _injectTargetLogged;   
        private static int _injectDetailLen = -1;  
        private static bool _hideLogged;           
        private static int _writeCount;            
        private static int _budgetSetupCount;      

        
        
        
        
        
        
        
        private static object _mainTmp;
        
        private static object _backupTmp;
        
        private static bool _producerHooked;
        
        private static bool _rewriteGuardHooked;
        
        private static bool _writing;
        private static int _producerInjectCount;
        private static int _reassertCount;
        private static bool _producerLogged;
        private static bool _reassertLogged;

        
        
        
        
        
        
        
        
        
        

        
        
        private static int _detailRev;
        private static string _cacheText;                 
        private static bool _cacheValid;
        private static InGameDetailFormat.DetailCacheKey _cacheKey;   
        private static int _composeRuns;                  
        private static int _composeHits;                  

        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        internal static void NoteTruncationForDisplay(string capName, int capValue, int written,
            int dropped, string reason, string fixHint)
        {
            
            InGameDetailFormat.NoteTruncation(capName, capValue, written, dropped, reason, fixHint);
            _detailRev++;   
        }

        
        
        
        
        
        
        internal static string DiagStatusLine()
        {
            
            
            int total = Suzerain.DiagKit.Diag.Count;
            int ok = Suzerain.DiagKit.Diag.OkCount;

            List<string> names = null;
            if (Suzerain.DiagKit.Diag.MissingCount + Suzerain.DiagKit.Diag.FailCount > 0)
            {
                var problems = Suzerain.DiagKit.Diag.Problems();
                if (problems != null)
                {
                    names = new List<string>(problems.Count);
                    for (int i = 0; i < problems.Count; i++) names.Add(problems[i].Name);
                }
            }

            return InGameDetailFormat.BuildDiagLine(total, ok, names);
        }

        
        private const int PerfProbeFires = 30;
        private static int _perfProbeLeft = PerfProbeFires;
        private static long _probeTicks, _fastTicks;      
        private static int _probeN, _fastN;
        private static int _wdFires, _wdNoop, _wdWrites;  
        
        private static string _lastDetailWritten;
        private static long _composeTicks;                
        private static int _producerCalls;                
        private static bool _perfReported;

        private static readonly List<InGameDetailFormat.Entry> _detailEntries =
            new List<InGameDetailFormat.Entry>();
        private static int _detailTotal;
        
        
        
        
        
        
        
        
        private static bool _hasSettle;
        
        
        
        
        
        
        
        
        
        private static bool _preview;

        
        private static string _detailMount = "未安装";

        
        
        
        
        
        
        
        
        
        
        
        
        
        private static readonly InGameDetailFormat.DetailMergeMemory _mainMerge =
            new InGameDetailFormat.DetailMergeMemory();

        
        
        
        
        private static object MergeKey(object fallback)
        {
            var main = _mainTmp;
            return main ?? fallback;
        }

        
        private const string SlotKeyMain = "②主 statTooltipText";

        
        private const string SlotKeyBackup = "①备用 statTooltipModifierText";

        
        
        
        
        
        private static object _hudPanel;

        
        
        
        
        
        
        
        
        internal static void PublishDetail(int total, IList<InGameDetailFormat.Entry> entries)
        {
            try
            {
                _detailTotal = total;
                _detailEntries.Clear();
                if (entries != null)
                {
                    for (int i = 0; i < entries.Count; i++) _detailEntries.Add(entries[i]);
                }
                _hasSettle = true;
                _preview = false;   
                _detailRev++;       

                
                
                ApplyDetailToBudgetStat();
            }
            catch (Exception e) { _log.LogWarning("[OI][ui] 明细搬运异常：" + e.Message); }
        }

        
        
        
        
        
        
        
        
        
        internal static void PublishPreview(int total, IList<InGameDetailFormat.Entry> entries, string where)
        {
            try
            {
                _detailTotal = total;
                _detailEntries.Clear();
                if (entries != null)
                {
                    for (int i = 0; i < entries.Count; i++) _detailEntries.Add(entries[i]);
                }
                _hasSettle = true;    
                _preview = true;      
                _detailRev++;         

                if (EconomyConfig.VerboseLog != null && EconomyConfig.VerboseLog.Value)
                    _log.LogInfo("[OI][ui] 预演已更新（" + where + "）预计=" + total
                        + " 分项=" + _detailEntries.Count);

                ApplyDetailToBudgetStat();
            }
            catch (Exception e) { _log.LogWarning("[OI][ui] 预演搬运异常：" + e.Message); }
        }

        
        private static string ComposeDetail()
        {
            
            try
            {
                
                
                
                
                
                
                
                
                
                
                
                
                bool frame = EconomyConfig.InGameDetailFrameOn;
                int style = EconomyConfig.InGameDetailStyleKind;
                EnsureRiziaColors();
                return _preview
                    ? InGameDetailFormat.Compose(_detailEntries, _detailTotal, _hasSettle,
                        InGameDetailFormat.PreviewTitle, null, frame, style, _riziaPosHex, _riziaNegHex)
                    : InGameDetailFormat.Compose(_detailEntries, _detailTotal, _hasSettle, null, null, frame, style,
                        _riziaPosHex, _riziaNegHex);
            }
            catch (Exception e)
            {
                _log.LogWarning("[OI][ui] 明细文本生成异常：" + e.Message);
                return "";
            }
        }

        

        
        
        
        
        
        
        
        
        
        
        
        
        
        
        private static bool _quoteLayoutLogged;
        private static float _baseTooltipWidth = -1f;

        
        private static void CaptureBaseTooltipWidth(object tmp)
        {
            try
            {
                var prt = TooltipRectOf(tmp);
                var size = prt == null ? null : Reflect.Prop(prt, "sizeDelta");
                if (size == null) return;
                float x = Convert.ToSingle(Reflect.Prop(size, "x"));
                if (x > 0f && _baseTooltipWidth <= 0f)
                {
                    _baseTooltipWidth = x;
                    _log.LogInfo("[OI][ui][quote] 固定 Tooltip 宽度=" + x + "（尚未引入引文）");
                }
            }
            catch { }
        }

        private static object TooltipRectOf(object tmp)
        {
            try
            {
                var rt = Reflect.Prop(tmp, "rectTransform");
                var parent = Reflect.Prop(rt, "parent");
                var go = Reflect.Prop(parent, "gameObject");
                var rtType = ResolveType("UnityEngine.RectTransform") ?? ResolveTypeSimple("RectTransform");
                return rtType == null || go == null ? null : GetComponentGeneric(go, rtType);
            }
            catch { return null; }
        }

        
        private static void LockQuoteWidth(object tmp)
        {
            try
            {
                var prt = TooltipRectOf(tmp);
                if (prt == null) return;
                var size = Reflect.Prop(prt, "sizeDelta");
                if (size == null) return;
                float x = _baseTooltipWidth > 0f ? _baseTooltipWidth : Convert.ToSingle(Reflect.Prop(size, "x"));
                float y = Convert.ToSingle(Reflect.Prop(size, "y"));
                
                var detailOnly = ComposeDetailCached();
                var pref = Reflect.Call1(tmp, "GetPreferredValues", detailOnly);
                if (pref != null)
                {
                    float pw = Convert.ToSingle(Reflect.Prop(pref, "x"));
                    
                    if (pw > 0f) x = (pw + 24f) * 1.5f * 0.85f * 1.2f;
                    _log.LogInfo("[OI][ui][quote] 纯明细首选宽度=" + pw + " → Tooltip 宽度=" + x + "（1.5×0.85×1.2=1.53）");
                }
                var go = Reflect.Prop(prt, "gameObject");
                var fitType = ResolveType("UnityEngine.UI.ContentSizeFitter");
                var fit = fitType == null || go == null ? null : GetComponentGeneric(go, fitType);
                if (fit != null) Reflect.SetPropOrField(fit, "horizontalFit", 0); 
                object locked = Activator.CreateInstance(size.GetType(), new object[] { x, y });
                if (Reflect.SetPropOrField(prt, "sizeDelta", locked))
                    _log.LogInfo("[OI][ui][quote] Tooltip 宽度锁定=" + x + "，高度=" + y);
            }
            catch (Exception e) { _log.LogWarning("[OI][ui][quote] 宽度锁定失败：" + e.Message); }
        }

        
        
        
        
        private static bool TmpActive(object tmp)
        {
            try { return Reflect.ToBool(Reflect.Prop(Reflect.Prop(tmp, "gameObject"), "activeInHierarchy")); }
            catch { return false; }
        }

        
        private static object FindVisibleQuoteTmp(object hidden)
        {
            try
            {
                var tmpType = ResolveType("TMPro.TextMeshProUGUI");
                if (tmpType == null || hidden == null) return null;
                object tr = Reflect.Prop(hidden, "transform");
                object root = Reflect.Prop(tr, "root");
                foreach (var r in new[] { root, tr, hidden })
                {
                    if (r == null) continue;
                    object go = Reflect.Prop(r, "gameObject");
                    Array comps = ComponentsInChildren(go, tmpType);
                    if (comps == null) comps = ComponentsInChildren(r, tmpType);
                    QuoteAnimProbe.Line("components in children: " + (comps == null ? "null" : comps.Length.ToString()));
                    if (comps == null) continue;
                    foreach (var c in comps)
                    {
                        if (c == null) continue;
                        string t = Reflect.Str(Reflect.Prop(c, "text")) ?? "";
                        bool active = TmpActive(c);
                        bool hasQuote = !string.IsNullOrEmpty(_lastQuoteStyled)
                            && t.IndexOf(_lastQuoteStyled, StringComparison.Ordinal) >= 0;
                        QuoteAnimProbe.Line("tmp candidate name=" + Reflect.Str(Reflect.Prop(c, "name"))
                            + " active=" + active + " len=" + t.Length + " hash=" + QuoteAnimProbe.Hash(t)
                            + " hasQuote=" + hasQuote + " head=" + QuoteAnimProbe.Clip(t, 70));
                        if (active && (hasQuote || t.IndexOf(_lastQuotePlain ?? "", StringComparison.Ordinal) >= 0))
                            return c;
                    }
                }

                
                var hit = WalkVisibleTmp(root ?? tr, tmpType, 0, 0);
                if (hit != null) return hit;

                
                var resources = ResolveType("UnityEngine.Resources");
                var findAll = resources == null ? null : resources.GetMethod("FindObjectsOfTypeAll",
                    BindingFlags.Static | BindingFlags.Public, null, new[] { typeof(Type) }, null);
                var all = findAll == null ? null : findAll.Invoke(null, new object[] { tmpType }) as Array;
                QuoteAnimProbe.Line("FindObjectsOfTypeAll: " + (all == null ? "null" : all.Length.ToString()));
                if (all != null)
                {
                    foreach (var c in all)
                    {
                        if (c == null) continue;
                        string t = Reflect.Str(Reflect.Prop(c, "text")) ?? "";
                        bool active = TmpActive(c);
                        bool hasQuote = !string.IsNullOrEmpty(_lastQuoteStyled)
                            && t.IndexOf(_lastQuoteStyled, StringComparison.Ordinal) >= 0;
                        QuoteAnimProbe.Line("global tmp name=" + Reflect.Str(Reflect.Prop(c, "name"))
                            + " active=" + active + " len=" + t.Length + " hasQuote=" + hasQuote);
                        if (active && (hasQuote || t.IndexOf(_lastQuotePlain ?? "", StringComparison.Ordinal) >= 0))
                            return c;
                    }
                }
            }
            catch (Exception e) { QuoteAnimProbe.Line("find visible tmp failed: " + e.Message); }
            return null;
        }

        private static object WalkVisibleTmp(object tr, Type tmpType, int depth, int nodes)
        {
            if (tr == null || tmpType == null || depth > 8 || nodes > 1600) return null;
            try
            {
                object go = Reflect.Prop(tr, "gameObject");
                object c = GetComponentGeneric(go, tmpType);
                if (c != null)
                {
                    string t = Reflect.Str(Reflect.Prop(c, "text")) ?? "";
                    bool active = TmpActive(c);
                    bool hasQuote = !string.IsNullOrEmpty(_lastQuoteStyled)
                        && t.IndexOf(_lastQuoteStyled, StringComparison.Ordinal) >= 0;
                    QuoteAnimProbe.Line("walk tmp name=" + Reflect.Str(Reflect.Prop(tr, "name"))
                        + " active=" + active + " len=" + t.Length + " hasQuote=" + hasQuote);
                    if (active && (hasQuote || t.IndexOf(_lastQuotePlain ?? "", StringComparison.Ordinal) >= 0))
                        return c;
                }
                int n = Reflect.ToInt(Reflect.Prop(tr, "childCount"));
                for (int i = 0; i < n && i < 80; i++)
                {
                    object ch = Reflect.Call1(tr, "GetChild", i);
                    var hit = WalkVisibleTmp(ch, tmpType, depth + 1, nodes + i + 1);
                    if (hit != null) return hit;
                }
            }
            catch { }
            return null;
        }

        private static Array ComponentsInChildren(object obj, Type type)
        {
            if (obj == null || type == null) return null;
            foreach (var m in obj.GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            {
                if (m.Name != "GetComponentsInChildren") continue;
                var ps = m.GetParameters();
                if (ps.Length != 2 || ps[0].ParameterType != typeof(Type) || ps[1].ParameterType != typeof(bool)) continue;
                return m.Invoke(obj, new object[] { type, true }) as Array;
            }
            return null;
        }

        
        private static void StartQuoteAnimation(object tmp)
        {
            if (!EconomyConfig.QuotesAnimationsOn) return;
            try
            {
                string stable = _lastQuoteStyled;
                if (string.IsNullOrEmpty(stable) || string.IsNullOrEmpty(_lastQuotePlain)) return;
                if (_quoteTransitionActive) return;
                var visible = FindVisibleQuoteTmp(tmp);
                if (visible != null) tmp = visible;
                string full = Reflect.Str(Reflect.Prop(tmp, "text")) ?? "";
                if (full.IndexOf(stable, StringComparison.Ordinal) < 0) return;
                string key = _quoteCacheKey + "|" + stable;
                if (string.Equals(key, _quoteAnimKey, StringComparison.Ordinal)) return;

                StopQuoteAnimation();
                lock (_quoteAnimLock)
                {
                    _quoteAnimTmp = tmp;
                    _quoteAnimFull = full;
                    _quoteAnimStable = stable;
                    _quoteAnimKey = key;
                    _quoteAnimFrame = 0;
                    _quoteAnimTotal = QuoteAnimation.FrameCount(_lastQuoteEffect);
                    _quoteAnimActive = true;
                    _quoteAnimGen++;
                    _quoteAnimNext = DateTime.UtcNow.AddMilliseconds(60);
                    _quoteAnimStarts++;
                    _quoteTickFrame = 0;
                    _quoteSwitchCount = 0;
                    EnsureQuoteTicker();
                    _log.LogInfo("[OI][ui][quote] anim=" + QuoteAnimation.Name(_lastQuoteEffect)
                        + " frames=" + _quoteAnimTotal + " start#" + _quoteAnimStarts);
                    QuoteAnimProbe.Line("start#" + _quoteAnimStarts + " effect=" + QuoteAnimation.Name(_lastQuoteEffect)
                        + " frames=" + _quoteAnimTotal + " fullLen=" + full.Length
                        + " fullHash=" + QuoteAnimProbe.Hash(full) + " stableLen=" + stable.Length
                        + " stableHash=" + QuoteAnimProbe.Hash(stable));
                    ProbeTmpState(tmp, "start");
                    try
                    {
                        if (Reflect.SetPropOrField(tmp, "richText", true))
                            QuoteAnimProbe.Line("richText 已设为 true");
                    }
                    catch { }
                }
            }
            catch (Exception e) { _log.LogWarning("[OI][ui][quote] 动画启动失败：" + e.Message); }
        }

        
        private static void EnsureQuoteTicker()
        {
            if (_quoteTickerHooked) return;
            try
            {
                var app = ResolveType("UnityEngine.Application") ?? ResolveTypeSimple("Application");
                var ev = app == null ? null : app.GetEvent("onBeforeRender", BindingFlags.Static | BindingFlags.Public);
                var mi = typeof(EconomyPatches).GetMethod("OnQuoteBeforeRender",
                    BindingFlags.Static | BindingFlags.NonPublic);
                QuoteAnimProbe.Line("ticker candidates: Application=" + (app == null ? "null" : app.FullName)
                    + " onBeforeRender=" + (ev == null ? "null" : ev.EventHandlerType.Name));
                if (ev != null && ev.EventHandlerType != null && mi != null)
                {
                    var del = Delegate.CreateDelegate(ev.EventHandlerType, mi);
                    ev.AddEventHandler(null, del);
                    _quoteTickerHooked = true;
                    _log.LogInfo("[OI][ui][quote] ticker=Application.onBeforeRender（10帧/动效，150帧/换条）");
                    QuoteAnimProbe.Line("ticker hooked=Application.onBeforeRender");
                    return;
                }
                _log.LogWarning("[OI][ui][quote] ticker: Application.onBeforeRender 不可用，改用 EventSystem.Update");
                QuoteAnimProbe.Line("ticker fallback: Application.onBeforeRender unavailable");
            }
            catch (Exception e) { _log.LogWarning("[OI][ui][quote] ticker Application 失败：" + e.Message); }

            
            try
            {
                var es = ResolveType("UnityEngine.EventSystems.EventSystem") ?? ResolveTypeSimple("EventSystem");
                var update = es == null ? null : es.GetMethod("Update",
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                QuoteAnimProbe.Line("ticker candidates: EventSystem=" + (es == null ? "null" : es.FullName)
                    + " Update=" + (update == null ? "null" : "found"));
                ProbeTickerCandidates(es);
                if (update == null)
                {
                    _log.LogWarning("[OI][ui][quote] ticker: 找不到 EventSystem.Update");
                    return;
                }
                var mi = typeof(EconomyPatches).GetMethod("Post_QuoteFrameTick",
                    BindingFlags.Static | BindingFlags.NonPublic);
                if (mi == null) return;
                var h = new Harmony("com.dell.suzerain.oikonomia.quote.ticker");
                h.Patch(update, postfix: new HarmonyMethod(mi));
                _quoteTickerHooked = true;
                _log.LogInfo("[OI][ui][quote] ticker=EventSystem.Update（10帧/动效，150帧/换条）");
                QuoteAnimProbe.Line("ticker hooked=EventSystem.Update");
            }
            catch (Exception e)
            {
                _log.LogWarning("[OI][ui][quote] ticker EventSystem 失败：" + e.Message);
                QuoteAnimProbe.Line("ticker EventSystem failed: " + e.Message);
            }
        }

        private static void Post_QuoteFrameTick()
        {
            QuoteAnimProbe.Line("EventSystem.Update postfix fired");
            OnQuoteBeforeRender();
        }

        private static void ProbeTmpState(object tmp, string tag)
        {
            try
            {
                string text = Reflect.Str(Reflect.Prop(tmp, "text"));
                string parsed = Reflect.Str(Reflect.Call0(tmp, "GetParsedText"));
                object info = Reflect.Prop(tmp, "textInfo");
                QuoteAnimProbe.Line(tag + " TMP textLen=" + (text == null ? -1 : text.Length)
                    + " textHash=" + QuoteAnimProbe.Hash(text)
                    + " enabled=" + Reflect.Str(Reflect.Prop(tmp, "enabled"))
                    + " activeInHierarchy=" + Reflect.Str(Reflect.Prop(Reflect.Prop(tmp, "gameObject"), "activeInHierarchy"))
                    + " richText=" + Reflect.Str(Reflect.Prop(tmp, "richText"))
                    + " alpha=" + Reflect.Str(Reflect.Prop(tmp, "alpha"))
                    + " fontSize=" + Reflect.Str(Reflect.Prop(tmp, "fontSize"))
                    + " maxVisibleCharacters=" + Reflect.Str(Reflect.Prop(tmp, "maxVisibleCharacters"))
                    + " overflowMode=" + Reflect.Str(Reflect.Prop(tmp, "overflowMode"))
                    + " enableAutoSizing=" + Reflect.Str(Reflect.Prop(tmp, "enableAutoSizing"))
                    + " charCount=" + Reflect.Str(Reflect.Prop(info, "characterCount"))
                    + " lineCount=" + Reflect.Str(Reflect.Prop(info, "lineCount"))
                    + " parsedLen=" + (parsed == null ? -1 : parsed.Length)
                    + " parsedTail=" + QuoteAnimProbe.Clip(parsed == null ? null : parsed.Substring(Math.Max(0, parsed.Length - 80)), 100));
            }
            catch (Exception e) { QuoteAnimProbe.Line(tag + " TMP probe failed: " + e.Message); }
        }

        private static void ProbeTickerCandidates(Type es)
        {
            try
            {
                if (es == null) return;
                foreach (var m in es.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
                {
                    if (m.Name == "Update" || m.Name == "LateUpdate" || m.Name == "Process")
                        QuoteAnimProbe.Line("candidate method: " + es.Name + "." + m.Name + "("
                            + m.GetParameters().Length + " params)");
                }
                var cur = es.GetProperty("current", BindingFlags.Static | BindingFlags.Public);
                var inst = cur == null ? null : cur.GetValue(null);
                QuoteAnimProbe.Line("EventSystem.current=" + (inst == null ? "null" : inst.GetType().FullName));
            }
            catch (Exception e) { QuoteAnimProbe.Line("candidate probe failed: " + e.Message); }
        }

        private static void OnQuoteBeforeRender()
        {
            try
            {
                if (!_quoteTickSeen)
                {
                    _quoteTickSeen = true;
                    _log.LogInfo("[OI][ui][quote] tick=first EventSystem frame");
                }
                _quoteTickCallbackCount++;
                if (_quoteTickCallbackCount <= 3)
                    QuoteAnimProbe.Line("tick callback#" + _quoteTickCallbackCount
                        + " active=" + _quoteAnimActive + " frame=" + _quoteTickFrame);
                if (_quoteTransitionActive)
                {
                    AdvanceQuoteTransition();
                    return;
                }
                if (!_quoteAnimActive) return;
                _quoteTickFrame++;
                bool bodyStep = QuoteAnimation.BodyAdvanceAt(_quoteTickFrame);
                if (_quoteTickFrame < 3 || bodyStep)
                    QuoteAnimProbe.Line("tick frame=" + _quoteTickFrame + " effectFrame="
                        + _quoteAnimFrame + "/" + _quoteAnimTotal + " active=" + _quoteAnimActive);
                if (bodyStep)
                {
                    _log.LogInfo("[OI][ui][quote] tick frame=" + _quoteTickFrame
                        + " effectFrame=" + _quoteAnimFrame + "/" + _quoteAnimTotal);
                    AdvanceQuoteAnimationStep();
                }
                
                if (QuoteAnimation.BodySwitchAt(_quoteTickFrame))
                    AdvanceQuoteVariant();
            }
            catch { StopQuoteAnimation(); }
        }

        
        private static void AdvanceQuoteAnimationStep()
        {
            string full, stable, plain;
            int total, next;
            QuoteEffect effect;
            object tmp;
            lock (_quoteAnimLock)
            {
                if (!_quoteAnimActive) return;
                full = _quoteAnimFull;
                stable = _quoteAnimStable;
                plain = _lastQuotePlain;
                effect = _lastQuoteEffect;
                total = _quoteAnimTotal;
                tmp = _quoteAnimTmp;
                next = _quoteAnimFrame + 1;
            }
            if (tmp == null) { StopQuoteAnimation(); return; }
            if (next >= total) return; 
            string frame = QuoteAnimation.Frame(plain, effect, next, total);
            string text = full.Replace(stable, frame);
            QuoteAnimProbe.Line("step candidate " + next + "/" + total + " effect=" + QuoteAnimation.Name(effect)
                + " outLen=" + text.Length + " outHash=" + QuoteAnimProbe.Hash(text)
                + " head=" + QuoteAnimProbe.Clip(text, 100));
            if (!string.Equals(text, full, StringComparison.Ordinal) && WriteQuoteFrame(tmp, text))
            {
                lock (_quoteAnimLock) { _quoteAnimFrame = next; }
                _log.LogInfo("[OI][ui][quote] step frame=" + next + "/" + total + " effect=" + QuoteAnimation.Name(effect));
            }
        }

        
        private static void AdvanceQuoteVariant()
        {
            try
            {
                if (!_quoteAnimActive || _quoteTransitionActive) return;
                string oldStable, full;
                object tmp;
                lock (_quoteAnimLock)
                {
                    oldStable = _quoteAnimStable;
                    full = _quoteAnimFull;
                    tmp = _quoteAnimTmp;
                }
                if (tmp == null || string.IsNullOrEmpty(oldStable) || string.IsNullOrEmpty(full))
                {
                    StopQuoteAnimation();
                    return;
                }
                _quoteSwitchCount++;
                _quoteTransitionActive = true;
                _quoteTransitionFrame = 0;
                _quoteTransitionOldFull = full;
                _quoteTransitionOldStable = oldStable;
                _quoteTransitionOldPlain = _lastQuotePlain;
                _quoteTransitionEffect = _lastQuoteEffect;
                _quoteTransitionTmp = tmp;
                _quoteTransitionPendingVariant = (_quoteVariant + 1) % 3;
                _quoteAnimActive = false;
                WriteQuoteFrame(tmp, full.Replace(oldStable, TransitionText(0)));
                _log.LogInfo("[OI][ui][quote] transition=" + TransitionName() + " 30f");
            }
            catch { StopQuoteAnimation(); }
        }

        private static string TransitionName()
        {
            switch (_quoteTransitionEffect)
            {
                case QuoteEffect.Crt:
                case QuoteEffect.Electric:
                    return "Signal Interruption";
                case QuoteEffect.Subtle:
                    return "Subtle Breath";
                default:
                    return "Fade Out";
            }
        }

        private static string TransitionText(int frame)
        {
            if (_quoteTransitionEffect == QuoteEffect.Fade)
                return QuoteAnimation.FadeTransition(_quoteTransitionOldPlain, frame);
            if (_quoteTransitionEffect == QuoteEffect.Subtle)
                return QuoteAnimation.SubtleTransition(_quoteTransitionOldPlain, frame);
            return QuoteAnimation.SignalTransition(frame);
        }

        private static void AdvanceQuoteTransition()
        {
            try
            {
                _quoteTransitionFrame++;
                bool signal = _quoteTransitionEffect == QuoteEffect.Crt || _quoteTransitionEffect == QuoteEffect.Electric;
                bool invisible = signal && (_quoteTransitionFrame % 5 == 0);
                string marker = TransitionText(_quoteTransitionFrame);
                string text = _quoteTransitionOldFull.Replace(_quoteTransitionOldStable, marker);
                WriteQuoteFrame(_quoteTransitionTmp, text);
                QuoteAnimProbe.Line("transition frame=" + _quoteTransitionFrame
                    + " kind=" + TransitionName() + " invisiblePlaceholder=" + invisible);
                if (_quoteTransitionFrame >= 30) FinishQuoteTransition();
            }
            catch { StopQuoteAnimation(); }
        }

        private static void FinishQuoteTransition()
        {
            try
            {
                _quoteVariant = _quoteTransitionPendingVariant;
                _quoteCacheKey = "";
                _quoteCacheText = "";
                _quoteNextRead = DateTime.MinValue;
                string nextBlocks = ComposeBudgetBlocks();
                string nextStable = _lastQuoteStyled;
                if (string.IsNullOrEmpty(nextBlocks) || string.IsNullOrEmpty(nextStable) || _quoteTransitionTmp == null)
                {
                    StopQuoteAnimation();
                    return;
                }
                string nextFull = _quoteTransitionOldFull.Replace(_quoteTransitionOldStable, nextStable);
                if (string.Equals(nextFull, _quoteTransitionOldFull, StringComparison.Ordinal))
                {
                    StopQuoteAnimation();
                    return;
                }
                WriteQuoteFrame(_quoteTransitionTmp, nextFull);
                lock (_quoteAnimLock)
                {
                    _quoteAnimFull = nextFull;
                    _quoteAnimStable = nextStable;
                    _quoteAnimTmp = _quoteTransitionTmp;
                    _quoteAnimFrame = 0;
                    _quoteAnimTotal = QuoteAnimation.FrameCount(_lastQuoteEffect);
                    _quoteAnimActive = true;
                    _quoteTickFrame = 0;
                }
                _quoteTransitionActive = false;
                _quoteTransitionFrame = 0;
                _log.LogInfo("[OI][ui][quote] variant=" + _quoteVariant + " effect="
                    + QuoteAnimation.Name(_lastQuoteEffect) + " after transition");
            }
            catch { StopQuoteAnimation(); }
        }

        
        private static void AdvanceQuoteAnimation()
        {
            try
            {
                if (!_quoteAnimActive || DateTime.UtcNow < _quoteAnimNext) return;
                string full, stable, plain;
                int total, next;
                QuoteEffect effect;
                object tmp;
                lock (_quoteAnimLock)
                {
                    if (!_quoteAnimActive || DateTime.UtcNow < _quoteAnimNext) return;
                    _quoteAnimNext = DateTime.UtcNow.AddMilliseconds(60);
                    full = _quoteAnimFull;
                    stable = _quoteAnimStable;
                    plain = _lastQuotePlain;
                    effect = _lastQuoteEffect;
                    total = _quoteAnimTotal;
                    tmp = _quoteAnimTmp;
                    next = _quoteAnimFrame + 1;
                }
                if (tmp == null) { StopQuoteAnimation(); return; }
                if (next >= total)
                {
                    WriteQuoteFrame(tmp, full);
                    
                    _quoteVariant = (_quoteVariant + 1) % 3;
                    _quoteCacheKey = "";
                    _quoteCacheText = "";
                    _quoteNextRead = DateTime.MinValue;
                    string nextBlocks = ComposeBudgetBlocks();
                    string nextStable = _lastQuoteStyled;
                    if (string.IsNullOrEmpty(nextBlocks) || string.IsNullOrEmpty(nextStable))
                    {
                        StopQuoteAnimation();
                        return;
                    }
                    string nextFull = full.Replace(stable, nextStable);
                    if (string.Equals(nextFull, full, StringComparison.Ordinal))
                    {
                        StopQuoteAnimation();
                        return;
                    }
                    WriteQuoteFrame(tmp, nextFull);
                    lock (_quoteAnimLock)
                    {
                        _quoteAnimFull = nextFull;
                        _quoteAnimStable = nextStable;
                        _quoteAnimFrame = 0;
                        _quoteAnimTotal = QuoteAnimation.FrameCount(_lastQuoteEffect);
                        _quoteAnimNext = DateTime.UtcNow.AddMilliseconds(60);
                        _quoteAnimKey = _quoteCacheKey + "|" + nextStable;
                        _quoteAnimActive = true;
                    }
                    _log.LogInfo("[OI][ui][quote] anim=next variant=" + _quoteVariant
                        + " effect=" + QuoteAnimation.Name(_lastQuoteEffect));
                    return;
                }
                string frame = QuoteAnimation.Frame(plain, effect, next, total);
                string text = full.Replace(stable, frame);
                if (string.Equals(text, full, StringComparison.Ordinal) || !WriteQuoteFrame(tmp, text))
                    StopQuoteAnimation();
                else
                    lock (_quoteAnimLock) { _quoteAnimFrame = next; }
            }
            catch { StopQuoteAnimation(); }
        }

        private static void QuoteTimerTick(object state)
        {
            try
            {
                int gen = (int)state;
                var ctx = _quoteSync;
                if (ctx != null) ctx.Post(delegate { QuoteAnimStep(gen); }, null);
            }
            catch { }
        }

        private static void QuoteAnimStep(int gen)
        {
            try
            {
                string full;
                string stable;
                int total;
                int next;
                QuoteEffect effect;
                string plain;
                object tmp;
                lock (_quoteAnimLock)
                {
                    if (!_quoteAnimActive || gen != _quoteAnimGen) return;
                    full = _quoteAnimFull;
                    stable = _quoteAnimStable;
                    total = _quoteAnimTotal;
                    effect = _lastQuoteEffect;
                    plain = _lastQuotePlain;
                    tmp = _quoteAnimTmp;
                    next = _quoteAnimFrame + 1;
                }
                if (tmp == null) { StopQuoteAnimation(); return; }
                if (next >= total)
                {
                    WriteQuoteFrame(tmp, full);
                    StopQuoteAnimation();
                    return;
                }
                string frame = QuoteAnimation.Frame(plain, effect, next, total);
                string text = full.Replace(stable, frame);
                if (string.Equals(text, full, StringComparison.Ordinal) || !WriteQuoteFrame(tmp, text))
                {
                    StopQuoteAnimation();
                    return;
                }
                lock (_quoteAnimLock) { _quoteAnimFrame = next; }
            }
            catch { StopQuoteAnimation(); }
        }

        private static bool WriteQuoteFrame(object tmp, string text)
        {
            QuoteAnimProbe.Line("write out len=" + (text == null ? -1 : text.Length)
                + " hash=" + QuoteAnimProbe.Hash(text)
                + " head=" + QuoteAnimProbe.Clip(text, 80)
                + " tail=" + QuoteAnimProbe.Clip(text == null ? null : text.Substring(Math.Max(0, text.Length - 140)), 180));
            _writing = true;
            try
            {
                bool ok = Reflect.SetPropOrField(tmp, "text", text);
                string back = Reflect.Str(Reflect.Prop(tmp, "text"));
                string parsed = Reflect.Str(Reflect.Call0(tmp, "GetParsedText"));
                QuoteAnimProbe.Line("write result ok=" + ok + " backLen=" + (back == null ? -1 : back.Length)
                    + " backHash=" + QuoteAnimProbe.Hash(back)
                    + " parsedLen=" + (parsed == null ? -1 : parsed.Length)
                    + " parsedTail=" + QuoteAnimProbe.Clip(parsed == null ? null : parsed.Substring(Math.Max(0, parsed.Length - 80)), 100));
                _probeWriteCount++;
                if (QuoteAnimProbe.Enabled && _probeWriteCount <= 12) ProbeTmpState(tmp, "after-write#" + _probeWriteCount);
                return ok;
            }
            catch (Exception e)
            {
                QuoteAnimProbe.Line("write exception: " + e.Message);
                return false;
            }
            finally { _writing = false; }
        }

        private static void StopQuoteAnimation()
        {
            if (QuoteAnimProbe.Enabled)
            {
                try
                {
                    var caller = new System.Diagnostics.StackTrace(1, false).GetFrame(0);
                    QuoteAnimProbe.Line("stop caller=" + (caller == null ? "?" : caller.GetMethod().Name)
                        + " active=" + _quoteAnimActive + " frame=" + _quoteTickFrame
                        + " variant=" + _quoteVariant);
                }
                catch { }
            }
            _quoteTransitionActive = false;
            _quoteTransitionFrame = 0;
            lock (_quoteAnimLock)
            {
                _quoteAnimActive = false;
                _quoteAnimGen++;
                if (_quoteTimer != null) { try { _quoteTimer.Dispose(); } catch { } _quoteTimer = null; }
            }
        }

        
        private static void ProbeQuoteLayout(object mainSlot, string text)
        {
            if (_quoteLayoutLogged) return;
            _quoteLayoutLogged = true;
            try
            {
                _log.LogInfo("[OI][ui][quote] layout: tmp=" + mainSlot.GetType().Name
                    + " textLen=" + (text == null ? -1 : text.Length)
                    + " maxVisibleCharacters=" + Reflect.Str(Reflect.Prop(mainSlot, "maxVisibleCharacters"))
                    + " overflowMode=" + Reflect.Str(Reflect.Prop(mainSlot, "overflowMode"))
                    + " enableAutoSizing=" + Reflect.Str(Reflect.Prop(mainSlot, "enableAutoSizing"))
                    + " fontSize=" + Reflect.Str(Reflect.Prop(mainSlot, "fontSize")));
                object rt = Reflect.Prop(mainSlot, "rectTransform");
                object parent = Reflect.Prop(rt, "parent");
                object go = Reflect.Prop(parent, "gameObject");
                object prt = null;
                var rtType = ResolveType("UnityEngine.RectTransform") ?? ResolveTypeSimple("RectTransform");
                try { if (rtType != null && go != null) prt = GetComponentGeneric(go, rtType); } catch { }
                if (prt == null) { try { if (rtType != null && go != null) prt = Reflect.Call1(go, "GetComponent", rtType); } catch { } }
                if (prt == null) { try { if (go != null) prt = Reflect.Call1(go, "GetComponentByName", "RectTransform"); } catch { } }
                if (prt == null) { try { if (parent != null) prt = Reflect.Call1(parent, "GetComponentByName", "RectTransform"); } catch { } }
                if (prt == null) { try { if (rtType != null && parent != null) prt = Reflect.Call1(parent, "GetComponent", rtType); } catch { } }
                _log.LogInfo("[OI][ui][quote] layout parent: type=" + (parent == null ? "null" : parent.GetType().Name)
                    + " name=" + Reflect.Str(Reflect.Prop(parent, "name"))
                    + " go=" + (go == null ? "null" : go.GetType().Name)
                    + " rt=" + (prt == null ? "null" : prt.GetType().Name)
                    + " sizeDelta=" + (prt == null ? "" : Reflect.Str(Reflect.Prop(prt, "sizeDelta")))
                    + " rect=" + (prt == null ? "" : Reflect.Str(Reflect.Prop(prt, "rect"))));
                
                Type imgType = ResolveType("UnityEngine.UI.Image") as Type;
                object cur = parent;
                bool expanded = false;
                for (int d = 0; d < 8 && cur != null; d++)
                {
                    object cgo = Reflect.Prop(cur, "gameObject");
                    object crt = GetComponentGeneric(cgo, rtType);
                    object img = imgType == null ? null : GetComponentGeneric(cgo, imgType);
                    string sz = crt == null ? "" : Reflect.Str(Reflect.Prop(crt, "sizeDelta"));
                    _log.LogInfo("[OI][ui][quote] ancestor[" + d + "]: name=" + Reflect.Str(Reflect.Prop(cur, "name"))
                        + " rt=" + (crt == null ? "null" : crt.GetType().Name)
                        + " image=" + (img == null ? "no" : "yes")
                        + " sizeDelta=" + sz);
                    if (!expanded && img != null && crt != null)
                    {
                        object size = Reflect.Prop(crt, "sizeDelta");
                        float w = size == null ? 0 : Convert.ToSingle(Reflect.Prop(size, "x"));
                        if (w >= 100f) { TryExpandTooltip(crt); expanded = true; }
                    }
                    cur = Reflect.Prop(cur, "parent");
                }

                
                int childCount = Reflect.ToInt(Reflect.Prop(parent, "childCount"));
                for (int i = 0; i < childCount; i++)
                {
                    object ch = Reflect.Call1(parent, "GetChild", i);
                    if (ch == null) continue;
                    object cgo = Reflect.Prop(ch, "gameObject");
                    object crt = GetComponentGeneric(cgo, rtType);
                    object img = imgType == null ? null : GetComponentGeneric(cgo, imgType);
                    object sz = crt == null ? null : Reflect.Prop(crt, "sizeDelta");
                    float w = sz == null ? 0 : Convert.ToSingle(Reflect.Prop(sz, "x"));
                    _log.LogInfo("[OI][ui][quote] child[" + i + "]: name=" + Reflect.Str(Reflect.Prop(ch, "name"))
                        + " image=" + (img == null ? "no" : "yes")
                        + " sizeDelta=" + (sz == null ? "" : Reflect.Str(sz))
                        + " rect=" + (crt == null ? "" : Reflect.Str(Reflect.Prop(crt, "rect"))));
                    if (img != null && crt != null && w >= 100f)
                    {
                        TryExpandTooltip(crt);
                        _log.LogInfo("[OI][ui][quote] child[" + i + "] 作为背景容器加高");
                    }
                }
            }
            catch (Exception e) { _log.LogWarning("[OI][ui][quote] layout 诊断失败：" + e.Message); }
        }

        
        private static object GetComponentGeneric(object go, Type compType)
        {
            try
            {
                foreach (var m in go.GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
                {
                    if (m.Name != "GetComponent" || !m.IsGenericMethodDefinition) continue;
                    if (m.GetGenericArguments().Length != 1) continue;
                    return m.MakeGenericMethod(compType).Invoke(go, null);
                }
            }
            catch { }
            return null;
        }

        
        private static void TryExpandTooltip(object prt)
        {
            try
            {
                object size = Reflect.Prop(prt, "sizeDelta");
                if (size == null) return;
                float x = Convert.ToSingle(Reflect.Prop(size, "x"));
                float y = Convert.ToSingle(Reflect.Prop(size, "y"));
                object grew = Activator.CreateInstance(size.GetType(), new object[] { x, y + 64f });
                if (Reflect.SetPropOrField(prt, "sizeDelta", grew))
                    _log.LogInfo("[OI][ui][quote] Tooltip 高度 +64：sizeDelta " + x + "x" + y + " → " + x + "x" + (y + 64f));
            }
            catch (Exception e) { _log.LogWarning("[OI][ui][quote] Tooltip 加高失败：" + e.Message); }
        }

        private static string _quoteCacheKey;
        private static string _quoteCacheText;
        private static int _quoteLogCount;
        private static DateTime _quoteNextRead = DateTime.MinValue;

        private static string _lastQuotePlain = "";
        private static string _lastQuoteStyled = "";
        private static QuoteEffect _lastQuoteEffect = QuoteEffect.Fade;
        private static string _quoteBaseKey = "";
        private static int _quoteVariant;
        private static DateTime _quoteAnimNext = DateTime.MinValue;

        private static readonly object _quoteAnimLock = new object();
        private static Timer _quoteTimer;
        private static SynchronizationContext _quoteSync;
        private static object _quoteAnimTmp;
        private static string _quoteAnimFull;
        private static string _quoteAnimStable;
        private static string _quoteAnimKey;
        private static int _quoteAnimFrame;
        private static int _quoteAnimTotal;
        private static int _quoteAnimGen;
        private static bool _quoteAnimActive;
        private static int _quoteAnimStarts;
        private static bool _quoteTransitionActive;
        private static int _quoteTransitionFrame;
        private static string _quoteTransitionOldFull;
        private static string _quoteTransitionOldStable;
        private static string _quoteTransitionOldPlain;
        private static QuoteEffect _quoteTransitionEffect;
        private static object _quoteTransitionTmp;
        private static int _quoteTransitionPendingVariant;
        private static bool _quoteTickerHooked;
        private static bool _quoteTickSeen;
        private static int _quoteTickCallbackCount;
        private static int _probeSetTextCount;
        private static int _probeWriteCount;
        private static int _quoteTickFrame;
        private static int _quoteSwitchCount;

        
        
        
        
        private static List<string> ActiveEmergencyTags()
        {
            var tags = new List<string>();
            try
            {
                bool emergency = IsTrue("BaseGame.Policy_Law_EmergencyDecree");
                bool arcasia = IsTrue("BaseGame.Reform_EmergencyDecree_Embargo_Arcasia");
                bool contana = IsTrue("BaseGame.Reform_EmergencyDecree_Embargo_UC");
                bool monarchy = IsTrue("BaseGame.Reform_EmergencyDecree_Embargo_Rumburg");
                tags = EmergencyQuoteSelector.BuildTags(emergency, arcasia, contana, monarchy);
            }
            catch { }
            return tags;
        }

        
        private static string ComposeBudgetQuoteCached()
        {
            try
            {
                
                if (DateTime.UtcNow < _quoteNextRead) return _quoteCacheText ?? "";
                _quoteNextRead = DateTime.UtcNow.AddMilliseconds(750);

                int min = ReadIntVar("BaseGame.GovernmentBudget_Min", -40, 120);
                int max = ReadIntVar("BaseGame.GovernmentBudget_Max", -40, 120);
                if (min == int.MinValue) min = -20;
                if (max == int.MinValue || max <= min) max = 30;
                int budget = ReadIntVar("BaseGame.GovernmentBudget", -40, 120);
                if (budget == int.MinValue) budget = min;
                int turn = PreviewTurn();

                var activeTags = ActiveEmergencyTags();
                var emergency = EmergencyQuoteSelector.Select(activeTags);
                string emergencyId = emergency == null ? "" : emergency.Id;
                string baseKey = min + "|" + max + "|" + budget + "|" + _detailTotal + "|" + turn
                    + "|" + emergencyId;
                if (!string.Equals(baseKey, _quoteBaseKey, StringComparison.Ordinal))
                {
                    _quoteBaseKey = baseKey;
                    _quoteVariant = 0;
                }
                string key = baseKey + "|" + _quoteVariant;
                if (string.Equals(key, _quoteCacheKey, StringComparison.Ordinal)) return _quoteCacheText;

                double ratio = BudgetQuoteMatrix.Ratio(budget, min, max);
                string xBand = BudgetQuoteMatrix.XBand(ratio);
                string yBand = BudgetQuoteMatrix.YBand(_detailTotal);
                BudgetQuote q;
                if (emergency != null)
                {
                    q = EmergencyQuoteSelector.Pick(emergency, _quoteVariant);
                    _lastQuoteEffect = QuoteEffect.Crt; 
                }
                else
                {
                    q = BudgetQuoteMatrix.Select(budget, min, max, _detailTotal, turn, _quoteVariant);
                    _lastQuoteEffect = BudgetQuoteMatrix.EffectForBudget(ratio, _detailTotal);
                }
                string text = BudgetQuoteMatrix.Compose(q, EconomyConfig.QuotesAttributedOn);
                _quoteCacheKey = key;
                _quoteCacheText = text;
                _lastQuotePlain = text;
                if (_quoteLogCount < 20 && !string.IsNullOrEmpty(text))
                {
                    _quoteLogCount++;
                    _log.LogInfo("[OI][ui][quote] cell=" + xBand + "_" + yBand
                        + " effect=" + QuoteAnimation.Name(_lastQuoteEffect)
                        + (emergency == null ? "" : " emergency=" + emergency.Id
                            + " tags=" + EmergencyQuoteSelector.TagList(activeTags))
                        + " budget=" + budget + "/[" + min + "," + max + "]"
                        + " delta=" + _detailTotal + " turn=" + turn);
                }
                return text;
            }
            catch { return ""; }
        }

        private static string ComposeDetailCached()
        {
            EnsureRiziaColors();

            var key = new InGameDetailFormat.DetailCacheKey();
            key.Rev = _detailRev;
            key.Style = EconomyConfig.InGameDetailStyleKind;
            key.Frame = EconomyConfig.InGameDetailFrameOn;
            key.Preview = _preview;
            key.HasSettle = _hasSettle;
            key.PosHex = _riziaPosHex;
            key.NegHex = _riziaNegHex;

            if (_cacheValid && _cacheKey.Matches(key))
            {
                _composeHits++;
                return _cacheText;
            }

            long t0 = System.Diagnostics.Stopwatch.GetTimestamp();
            var text = ComposeDetail();
            _composeTicks += System.Diagnostics.Stopwatch.GetTimestamp() - t0;

            _cacheText = text;
            _cacheValid = true;
            _cacheKey = key;
            _composeRuns++;
            return text;
        }

        
        private const string QuoteBlockGap = "\n\n";

        
        
        
        
        
        private static string ComposeBudgetBlocks()
        {
            var detail = ComposeDetailCached();
            if (!EconomyConfig.QuotesEnabledOn) return detail;
            var quote = ComposeBudgetQuoteCached();
            if (string.IsNullOrEmpty(quote)) return detail;
            if (detail.Length == 0 && !EconomyConfig.QuotesShowWhenDetailEmptyOn) return detail;
            
            quote = QuoteAnimation.Stable(quote);
            _lastQuoteStyled = quote;
            return detail.Length == 0 ? quote : detail + QuoteBlockGap + quote;
        }

        
        private static string PerfUs(long ticks, int n)
        {
            if (n <= 0) return "n/a";
            double us = ticks * 1000000.0 / System.Diagnostics.Stopwatch.Frequency / n;
            return us.ToString("F1", CultureInfo.InvariantCulture) + "µs";
        }

        
        
        
        
        private static void PerfLog(string tag)
        {
            try
            {
                _log.LogInfo("[OI][ui][perf] " + tag
                    + "｜主槽 set_text 命中=" + _wdFires
                    + "（早退 " + _wdNoop + " / 真补写 " + _wdWrites + "）"
                    + "｜旧路径 " + PerfUs(_probeTicks, _probeN) + "×" + _probeN
                    + "｜新路径 " + PerfUs(_fastTicks, _fastN) + "×" + _fastN
                    + "｜明细重算 " + _composeRuns + " 次 共 " + PerfUs(_composeTicks, _composeRuns)
                    + "（缓存命中 " + _composeHits + "）"
                    + "｜生产者进入 " + _producerCalls);
            }
            catch { }
        }

        
        
        private static string _riziaPosHex;
        private static string _riziaNegHex;
        
        private static object _riziaColorOwner;

        
        
        
        
        
        
        
        
        
        
        private static void EnsureRiziaColors()
        {
            var st = _budgetStat;
            if (ReferenceEquals(_riziaColorOwner, st) && _riziaPosHex != null && _riziaNegHex != null) return;
            _riziaColorOwner = st;
            try
            {
                if (!Reflect.Usable(st)) return;
                var pos = ColorToHex(Reflect.Prop(st, "tooltipPositiveColor"));
                var neg = ColorToHex(Reflect.Prop(st, "tooltipNegativeColor"));
                if (pos != null) _riziaPosHex = pos;
                if (neg != null) _riziaNegHex = neg;
                if (pos != null || neg != null)
                {
                    _log.LogInfo("[OI][ui] Rizia 形态取色：正=" + (pos ?? "(读不到，回退实测常量)")
                        + " 负=" + (neg ?? "(读不到，回退实测常量)")
                        + " 来源=统计实例 tooltipPositiveColor/tooltipNegativeColor");
                }
            }
            catch (Exception e)
            {
                _log.LogWarning("[OI][ui] Rizia 取色异常（回退实测常量）：" + e.Message);
            }
        }

        
        private static string ColorToHex(object color)
        {
            if (color == null) return null;
            try
            {
                var r = Reflect.Prop(color, "r");
                var g = Reflect.Prop(color, "g");
                var b = Reflect.Prop(color, "b");
                if (r == null || g == null || b == null) return null;
                return InGameDetailFormat.RgbaToHex(
                    Convert.ToDouble(r, CultureInfo.InvariantCulture),
                    Convert.ToDouble(g, CultureInfo.InvariantCulture),
                    Convert.ToDouble(b, CultureInfo.InvariantCulture));
            }
            catch { return null; }
        }

        
        
        
        
        
        
        
        
        
        private static void Post_TooltipModifier(object __instance, object __0, ref string __result)
        {
            try
            {
                
                _tooltipCallCount++;
                var statName = StatNameOf(__instance);
                var dataName = Reflect.Str(Reflect.Prop(__0, "NameInDatabase"));
                var isBudget = IsBudgetStat(__instance, __0, statName);

                if (!_firstTooltipLogged)
                {
                    _firstTooltipLogged = true;
                    _log.LogInfo("[OI][ui] A 首次被调用（自观测）：统计=" + Describe(statName)
                        + " HUDStatData.NameInDatabase=" + Describe(dataName)
                        + " 判为预算=" + isBudget
                        + " 调用序号=" + _tooltipCallCount
                        + " 已捕获预算实例=" + (_budgetStat != null));
                }

                
                if (EconomyConfig.InGameDetail == null || !EconomyConfig.InGameDetail.Value) return;

                
                if (!isBudget) return;

                
                AdoptBudgetStat(__instance, statName, "tooltip调用");

                ObserveOnce(__instance, __0, statName, __result);

                
                
                RefreshDetailPreview("tooltip调用");

                var text = ComposeBudgetBlocks();   
                if (text.Length == 0) return;      

                __result = text;
                _lastTooltipText = text;

                if (EconomyConfig.VerboseLog != null && EconomyConfig.VerboseLog.Value)
                    _log.LogInfo("[OI][ui] 明细已注入（A）len=" + text.Length + " stat=" + statName);
            }
            catch { }   
        }

        
        
        
        
        
        private static void Post_StatSetupData(object __instance, object __0)
        {
            try
            {
                _budgetSetupCount++;
                var dataName = Reflect.Str(Reflect.Prop(__0, "NameInDatabase"));
                var isBudget = IsBudgetStat(__instance, __0, StatNameOf(__instance));

                
                if (_budgetSetupCount == 1 || isBudget)
                {
                    _log.LogInfo("[OI][ui] Setup(HUDStatData) 观测：序号=" + _budgetSetupCount
                        + " NameInDatabase=" + Describe(dataName)
                        + " 判为预算=" + isBudget
                        + " 已捕获预算实例=" + (_budgetStat != null));
                }

                if (!isBudget) return;

                AdoptBudgetStat(__instance, dataName, "Setup");
                
                
                CaptureBaseTooltipWidth(Reflect.Prop(__instance, "statTooltipText"));
                
                RefreshDetailPreview("HUD Setup(HUDStatData)");
            }
            catch { }
        }

        
        
        
        
        
        private static void AdoptBudgetStat(object instance, string statName, string how)
        {
            try
            {
                if (instance == null || !Reflect.Usable(instance)) return;
                if (_budgetStat != null && Reflect.Usable(_budgetStat)) return;

                _budgetStat = instance;
                if (!_budgetAdoptLogged)
                {
                    _budgetAdoptLogged = true;
                    _log.LogInfo("[OI][ui] 已（补）捕获预算 HUD 统计实例：stat=" + Describe(statName)
                        + " 来源=" + how);
                }
            }
            catch { }
        }

        
        
        
        
        
        
        
        private static void Post_StatSetupNoArg(object __instance)
        {
            try
            {
                if (EconomyConfig.InGameDetail == null || !EconomyConfig.InGameDetail.Value) return;
                if (__instance == null || !ReferenceEquals(__instance, _budgetStat)) return;

                var name = StatNameOf(__instance);
                ObserveOnce(__instance, name != null ? Reflect.Prop(__instance, "currentHUDStatData") : null, name, null);

                
                
                RefreshDetailPreview("HUD Setup()");
            }
            catch { }
        }

        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        private static void ApplyDetailToBudgetStat()
        {
            try
            {
                if (EconomyConfig.InGameDetail == null || !EconomyConfig.InGameDetail.Value) return;

                var text = ComposeBudgetBlocks();   

                
                
                
                if (!_hasSettle) { ProbeSlotsOnce(ResolveBudgetStat()); return; }

                if (text.Length == 0) return;      

                var stat = ResolveBudgetStat();
                if (stat == null)
                {
                    FailOnce(ref _statMissLogged, "本体预算 HUD 统计实例",
                        "未捕获到 Sordland_HUDStat_GovernmentBudget 的 TemplateHUDStat 实例（Setup 时机未命中），明细无法写入");
                    return;
                }

                
                EnsureModifierVisible(stat, StatNameOf(stat));

                
                var backupSlot = Reflect.Prop(stat, "statTooltipModifierText");
                var mainSlot = Reflect.Prop(stat, "statTooltipText");
                var backupSlotOk = Reflect.Usable(backupSlot);
                var mainSlotOk = Reflect.Usable(mainSlot);
                ProbeSlotsOnce(stat, backupSlot, mainSlot, backupSlotOk, mainSlotOk);
                LogInjectTargetOnce(mainSlotOk, backupSlotOk);

                
                
                if (mainSlotOk) _mainTmp = mainSlot;
                if (backupSlotOk) _backupTmp = backupSlot;

                
                var main = new AppendResult();
                bool mainWritten = mainSlotOk && AppendToMainSlot(mainSlot, text, out main);
                if (mainWritten && EconomyConfig.QuotesEnabledOn)
                {
                    LockQuoteWidth(mainSlot);
                    StartQuoteAnimation(mainSlot);
                }

                
                bool backupWritten = backupSlotOk && EconomyConfig.InGameDetailBackupSlotOn
                                     && WriteBackupSlot(backupSlot, text);

                if (!mainWritten && !backupWritten)
                {
                    FailOnce(ref _slotMissLogged, "本体 HUD 预算明细槽位",
                        "statTooltipText 与 statTooltipModifierText 均不可用（字段不存在或为 null）—— 本体 HUD 无可用明细槽位");
                    return;
                }

                _lastTooltipText = text;
                _writeCount++;
                LogInjection(stat, text, main, mainWritten, mainSlotOk, backupWritten, backupSlotOk);
            }
            catch (Exception e) { _log.LogWarning("[OI][ui] 路径 B 写入异常：" + e.Message); }
        }

        
        
        
        
        
        
        
        private static object ResolveBudgetStat()
        {
            try
            {
                var stat = _budgetStat;
                if (stat != null && Reflect.Usable(stat)) return stat;

                var list = _hudPanel == null ? null : Reflect.Call0(_hudPanel, "get_instantiatedHUDStats");
                var count = Reflect.ListCount(list);
                if (count <= 0) return null;

                var items = list as System.Collections.IList;
                for (int i = 0; i < count; i++)
                {
                    object s;
                    try
                    {
                        
                        s = items != null ? items[i] : Reflect.Call1(list, "get_Item", i);
                    }
                    catch { continue; }
                    if (!Reflect.Usable(s)) continue;

                    var n = Reflect.Str(Reflect.Prop(Reflect.Prop(s, "currentHUDStatData"), "NameInDatabase"));
                    if (!IsBudgetName(n)) continue;

                    _budgetStat = s;
                    if (!_budgetAdoptLogged)
                    {
                        _budgetAdoptLogged = true;
                        _log.LogInfo("[OI][ui] 已（补）捕获预算 HUD 统计实例：stat=" + Describe(n)
                            + " 来源=HUDPanel.instantiatedHUDStats[" + i + "]");
                    }
                    return s;
                }
            }
            catch (Exception e) { _log.LogWarning("[OI][ui] 定位预算统计异常：" + e.Message); }
            return null;
        }

        
        
        
        
        
        private struct AppendResult
        {
            public bool Ok;          
            public bool Same;        
            public bool Fingerprint; 
            public int OrigLen;      
            public int KeepLen;      
            public int SegLen;       
            public int MergedLen;    
            public int BackLen;      
        }

        
        
        
        
        
        
        
        
        
        private static bool AppendToMainSlot(object tmp, string detail, out AppendResult res)
        {
            res = new AppendResult();

            var orig = Reflect.Str(Reflect.Prop(tmp, "text")) ?? "";
            res.OrigLen = orig.Length;

            bool fingerprint;
            var merged = _mainMerge.Merge(MergeKey(tmp), orig, detail, out fingerprint);
            res.Fingerprint = fingerprint;
            res.KeepLen = _mainMerge.LastBase == null ? 0 : _mainMerge.LastBase.Length;
            res.SegLen = merged.Length - res.KeepLen;
            res.MergedLen = merged.Length;

            if (string.Equals(merged, orig, StringComparison.Ordinal))
            {
                
                res.BackLen = orig.Length;
                res.Same = true;
                res.Ok = true;
                return true;
            }

            
            
            _writing = true;
            try
            {
                if (!Reflect.SetPropOrField(tmp, "text", merged)) return false;
            }
            finally { _writing = false; }

            var back = Reflect.Str(Reflect.Prop(tmp, "text"));
            res.BackLen = back == null ? -1 : back.Length;
            res.Same = string.Equals(merged, back, StringComparison.Ordinal);
            res.Ok = true;

            LogReadBack(SlotKeyMain,
                "主 statTooltipText（只追加，游戏原文保留 "
                + (res.KeepLen == 0 ? "无（原文是空串）" : res.KeepLen + " 字符") + "）",
                Describe(orig), merged, back);
            return true;
        }

        
        
        
        
        
        
        
        private static bool WriteBackupSlot(object tmp, string text)        {
            var before = Describe(Reflect.Str(Reflect.Prop(tmp, "text")));
            if (!Reflect.SetPropOrField(tmp, "text", text)) return false;

            var back = Reflect.Str(Reflect.Prop(tmp, "text"));
            LogReadBack(SlotKeyBackup, "备用 statTooltipModifierText（整体覆盖）", before, text, back);
            return true;
        }

        
        
        
        
        
        
        
        private static void LogInjectTargetOnce(bool mainOk, bool backupOk)
        {
            if (_injectTargetLogged) return;
            _injectTargetLogged = true;
            _log.LogInfo("[OI][ui] 明细注入目标=statTooltipText（主 · 悬停真正渲染的就是它 · 只追加不覆盖）"
                + " + statTooltipModifierText（备用 · 整体覆盖写）"
                + " ｜ 本次槽位可用性：主=" + (mainOk ? "可用" : "不可用")
                + " 备=" + (backupOk ? "可用" : "不可用"));
        }

        
        
        
        
        
        
        
        
        private static void LogInjection(object stat, string detail, AppendResult main,
            bool mainWritten, bool mainSlotOk, bool backupWritten, bool backupSlotOk)
        {
            var contentChanged = detail.Length != _injectDetailLen;
            _injectDetailLen = detail.Length;

            _log.LogInfo("[OI][ui] 明细注入 #" + _writeCount
                + "：目标=statTooltipText（主）"
                + " 追加段长度=" + (mainSlotOk ? main.SegLen : -1)
                + " 回读一致=" + (mainSlotOk && mainWritten ? main.Same.ToString() : "（未写）")
                + " 目标原文长度=" + (mainSlotOk ? main.OrigLen : -1)
                + (mainSlotOk ? "（其中游戏原文 " + main.KeepLen + " 字符已保留）" : "")
                + " 合并后长度=" + (mainSlotOk ? main.MergedLen : -1)
                + (main.Fingerprint ? " 幂等：内存指纹命中（从记下的原文重拼，无标记）" : " 首次写入/游戏原文已重写")
                + (contentChanged ? " 明细正文已变" : " 明细正文与上次相同")
                + " ｜ 备用槽=" + (backupSlotOk ? (backupWritten ? "已写" : "写失败") : "不可用")
                + " ｜ stat=" + Describe(StatNameOf(stat)));
        }

        
        
        
        
        
        
        
        
        private static void ProbeSlotsOnce(object stat)
        {
            if (_slotLogged) return;
            try
            {
                if (stat == null) return;    
                var backup = Reflect.Prop(stat, "statTooltipModifierText");
                var main = Reflect.Prop(stat, "statTooltipText");
                ProbeSlotsOnce(stat, backup, main, Reflect.Usable(backup), Reflect.Usable(main));
            }
            catch { }
        }

        
        private static void ProbeSlotsOnce(object stat, object backup, object main, bool backupOk, bool mainOk)
        {
            if (_slotLogged) return;
            _slotLogged = true;
            try
            {
                var mainOrig = mainOk ? Reflect.Str(Reflect.Prop(main, "text")) : null;
                var backupOrig = backupOk ? Reflect.Str(Reflect.Prop(backup, "text")) : null;

                _log.LogInfo("[OI][ui] 明细槽位探明：统计=" + Describe(StatNameOf(stat))
                    + " | statTooltipText=" + (mainOk ? "找到" : "未找到/null")
                    + "【主 · 悬停真渲染】原文长度=" + (mainOrig == null ? -1 : mainOrig.Length)
                    + " 原文本=" + Describe(mainOrig)
                    + " | statTooltipModifierText=" + (backupOk ? "找到" : "未找到/null")
                    + "【备用 · 2.7.2 写了却看不见】原文长度=" + (backupOrig == null ? -1 : backupOrig.Length)
                    + " 原文本=" + Describe(backupOrig));
            }
            catch { }
        }

        
        
        
        private static void LogReadBack(string slotKey, string slot, string before, string wrote, string back)
        {
            if (!_readBackLogged.Add(slotKey)) return;
            var same = string.Equals(wrote, back, StringComparison.Ordinal);
            _log.LogInfo("[OI][ui] 明细写入回读：" + slot
                + " 原文本=" + before
                + " 写入长度=" + (wrote == null ? -1 : wrote.Length)
                + " 回读长度=" + (back == null ? -1 : back.Length)
                + " 一致=" + same);
            if (!same)
                _log.LogWarning("[OI][ui] 明细写入回读不一致 —— TMP 可能未接受该文本（槽位=" + slot + "）");
        }

        
        private static void FailOnce(ref bool flag, string name, string why)
        {
            if (flag) return;
            flag = true;
            try { Suzerain.DiagKit.Diag.Fail(name, why); } catch { }
            _log.LogWarning("[OI][ui] " + name + "：" + why);
        }

        
        
        
        
        
        
        
        
        
        private static void EnsureModifierVisible(object stat, string statName)
        {
            try
            {
                if (stat == null || !Reflect.Usable(stat)) return;
                var cur = Reflect.Prop(stat, "hideModifier");

                if (!_hideLogged)
                {
                    _hideLogged = true;
                    _log.LogInfo("[OI][ui] hideModifier 原值="
                        + (cur == null ? "(字段未取到)" : cur.ToString())
                        + "（目标 false）stat=" + Describe(statName));
                }

                if (cur == null) return;                      

                if (Reflect.ToBool(cur))
                {
                    if (Reflect.SetPropOrField(stat, "hideModifier", false))
                    {
                        var back = Reflect.Prop(stat, "hideModifier");
                        _log.LogInfo("[OI][ui] 预算明细区原为隐藏（hideModifier：true → "
                            + (back == null ? "?" : back.ToString()) + "）stat=" + Describe(statName));
                        if (Reflect.ToBool(back))
                            _log.LogWarning("[OI][ui] hideModifier 置 false 后回读仍为 true —— 明细可能不可见 stat=" + Describe(statName));
                    }
                    else
                        _log.LogWarning("[OI][ui] hideModifier 原为 true，但写入失败 —— 明细可能不可见 stat=" + Describe(statName));
                }
            }
            catch { }
        }

        
        
        
        
        
        
        
        private static bool IsBudgetStat(object instance, object statData, string statName)
        {
            try
            {
                if (instance != null && _budgetStat != null && ReferenceEquals(instance, _budgetStat)) return true;

                if (IsBudgetName(statName)) return true;

                var n = Reflect.Str(Reflect.Prop(statData, "NameInDatabase"));
                if (IsBudgetName(n)) return true;
            }
            catch { }
            return false;
        }

        
        
        
        
        private static bool IsBudgetName(string name)
        {
            if (string.IsNullOrEmpty(name)) return false;
            return name.IndexOf("GovernmentBudget", StringComparison.OrdinalIgnoreCase) >= 0
                || name.IndexOf("HUDStat_Budget", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        
        
        
        
        private static string StatNameOf(object instance)
        {
            try
            {
                var data = Reflect.Prop(instance, "currentHUDStatData");
                var n = Reflect.Str(Reflect.Prop(data, "NameInDatabase"));
                if (!string.IsNullOrEmpty(n)) return n;
            }
            catch { }
            return null;
        }

        
        
        
        
        
        
        
        private static void ObserveOnce(object stat, object statData, string statName, string incoming)
        {
            if (_observed) return;
            _observed = true;   
            try
            {
                var hide = Reflect.Prop(stat, "hideModifier");
                var tmp = Reflect.Prop(stat, "statTooltipModifierText");
                var orig = tmp == null ? null : Reflect.Str(Reflect.Prop(tmp, "text"));
                var dataName = statData == null ? null : Reflect.Str(Reflect.Prop(statData, "NameInDatabase"));

                _log.LogInfo("[OI][ui] 明细槽位自观测（仅一次）："
                    + " stat=" + (statName ?? "(未取到)")
                    + " | HUDStatData.NameInDatabase=" + (dataName ?? "(未取到)")
                    + " | hideModifier原值=" + (hide == null ? "(字段未取到)" : hide.ToString())
                    + " | statTooltipModifierText原文本=" + Describe(orig)
                    + " | 原 result=" + Describe(incoming)
                    + " | GetTooltipModifierTextString调用次数=" + _tooltipCallCount);
            }
            catch (Exception e)
            {
                try { _log.LogWarning("[OI][ui] 自观测失败：" + e.Message); } catch { }
            }
        }

        
        private static string Describe(string s)
        {
            if (s == null) return "(null)";
            if (s.Length == 0) return "(空串)";
            var one = s.Replace("\r", "\\r").Replace("\n", "\\n");
            return "\"" + (one.Length > 120 ? one.Substring(0, 120) + "…(" + one.Length + "字符)" : one) + "\"";
        }

        
        
        
        
        
        private static void InstallInGameDetail(Harmony harmony, Assembly asm, ref int ok, ref int miss)
        {
            var statType = Reflect.FindType(asm, "TemplateHUDStat");
            var dataType = Reflect.FindType(asm, "HUDStatData");
            if (statType == null || dataType == null)
            {
                miss++;
                Suzerain.DiagKit.Diag.Missing("TemplateHUDStat/HUDStatData（游戏内明细）", "类型缺失，明细只能留在日志与文件桥");
                _log.LogWarning("[OI][ui] 未找到 TemplateHUDStat/HUDStatData，游戏内预算明细不可用");
                _detailMount = "无（类型缺失）";
                return;
            }

            
            MethodInfo tip = null;
            try { tip = AccessTools.Method(statType, "GetTooltipModifierTextString", new[] { dataType }); }
            catch { }

            if (tip != null)
            {
                try
                {
                    var post = new HarmonyMethod(typeof(EconomyPatches).GetMethod(
                        nameof(Post_TooltipModifier),
                        BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public));
                    harmony.Patch(tip, postfix: post);
                    _detailPathA = true;
                    ok++;
                    Suzerain.DiagKit.Diag.Ok("TemplateHUDStat.GetTooltipModifierTextString(HUDStatData)");
                }
                catch (Exception e)
                {
                    
                    miss++;
                    _detailPathA = false;
                    Suzerain.DiagKit.Diag.Missing("TemplateHUDStat.GetTooltipModifierTextString(HUDStatData)",
                        "Postfix 挂载失败，已回退到路径 B（直接写 statTooltipModifierText）");
                    _log.LogWarning("[OI][ui] 路径 A 挂载失败，回退路径 B：" + e.Message);
                }
            }
            else
            {
                miss++;
                _detailPathA = false;
                Suzerain.DiagKit.Diag.Missing("TemplateHUDStat.GetTooltipModifierTextString(HUDStatData)",
                    "方法或签名不符，已回退到路径 B（直接写 statTooltipModifierText）");
                _log.LogWarning("[OI][ui] 未找到 GetTooltipModifierTextString(HUDStatData)，回退路径 B");
            }

            
            try
            {
                var setupNoArg = statType.GetMethod("Setup", BindingFlags.Public | BindingFlags.Instance,
                    null, Type.EmptyTypes, null);
                if (setupNoArg != null)
                {
                    var post = new HarmonyMethod(typeof(EconomyPatches).GetMethod(
                        nameof(Post_StatSetupNoArg),
                        BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public));
                    harmony.Patch(setupNoArg, postfix: post);
                    ok++;
                    Suzerain.DiagKit.Diag.Ok("TemplateHUDStat.Setup()");
                }
                else
                {
                    miss++;
                    Suzerain.DiagKit.Diag.Missing("TemplateHUDStat.Setup()", "无参 Setup 不存在，路径 B 刷新可能被覆盖");
                }
            }
            catch (Exception e)
            {
                miss++;
                Suzerain.DiagKit.Diag.Fail("TemplateHUDStat.Setup()", e.Message);
            }

            
            
            
            try
            {
                var setupData = AccessTools.Method(statType, "Setup", new[] { dataType });
                if (setupData != null)
                {
                    var post = new HarmonyMethod(typeof(EconomyPatches).GetMethod(
                        nameof(Post_StatSetupData),
                        BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public));
                    harmony.Patch(setupData, postfix: post);
                    ok++;
                    Suzerain.DiagKit.Diag.Ok("TemplateHUDStat.Setup(HUDStatData)（明细观测）");
                }
                else
                {
                    miss++;
                    Suzerain.DiagKit.Diag.Missing("TemplateHUDStat.Setup(HUDStatData)（明细观测）",
                        "签名不符，无法观测统计实例捕获时机");
                }
            }
            catch (Exception e)
            {
                miss++;
                Suzerain.DiagKit.Diag.Fail("TemplateHUDStat.Setup(HUDStatData)（明细观测）", e.Message);
            }

            
            try
            {
                var panelType = Reflect.FindType(asm, "HUDPanel");
                var showMethod = panelType == null
                    ? null
                    : panelType.GetMethod("Show", BindingFlags.Public | BindingFlags.Instance, null, Type.EmptyTypes, null);
                if (showMethod != null)
                {
                    var post = new HarmonyMethod(typeof(EconomyPatches).GetMethod(
                        nameof(Post_HudPanelShow),
                        BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public));
                    harmony.Patch(showMethod, postfix: post);
                    ok++;
                    Suzerain.DiagKit.Diag.Ok("HUDPanel.Show()（预算统计兜底定位）");
                }
                else
                {
                    miss++;
                    Suzerain.DiagKit.Diag.Missing("HUDPanel.Show()（预算统计兜底定位）", "方法缺失，B 路径的自愈定位不可用");
                }
            }
            catch (Exception e)
            {
                miss++;
                Suzerain.DiagKit.Diag.Fail("HUDPanel.Show()（预算统计兜底定位）", e.Message);
            }

            
            
            try
            {
                var loc = statType.GetMethod("SetLocalizableTexts", BindingFlags.Public | BindingFlags.Instance,
                    null, Type.EmptyTypes, null);
                if (loc != null)
                {
                    var post = new HarmonyMethod(typeof(EconomyPatches).GetMethod(
                        nameof(Post_SetLocalizableTexts),
                        BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public));
                    harmony.Patch(loc, postfix: post);
                    ok++;
                    Suzerain.DiagKit.Diag.Ok("TemplateHUDStat.SetLocalizableTexts()（切语言后补写）");
                }
                else
                {
                    miss++;
                    Suzerain.DiagKit.Diag.Missing("TemplateHUDStat.SetLocalizableTexts()（切语言后补写）",
                        "方法缺失：切语言后明细可能被本地化重写掉，下次结算/刷新会补回");
                }
            }
            catch (Exception e)
            {
                miss++;
                Suzerain.DiagKit.Diag.Fail("TemplateHUDStat.SetLocalizableTexts()（切语言后补写）", e.Message);
            }

            
            
            
            try
            {
                var getter = statType.GetMethod("GetStatTooltipText",
                    BindingFlags.NonPublic | BindingFlags.Instance, null, Type.EmptyTypes, null)
                    ?? statType.GetMethod("GetStatTooltipText",
                    BindingFlags.Public | BindingFlags.Instance, null, Type.EmptyTypes, null);
                if (getter != null)
                {
                    var post = new HarmonyMethod(typeof(EconomyPatches).GetMethod(
                        nameof(Post_GetStatTooltipText),
                        BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public));
                    
                    post.priority = Priority.Last;
                    harmony.Patch(getter, postfix: post);
                    try
                    {
                        var pi = Harmony.GetPatchInfo(getter);
                        if (pi != null)
                        {
                            foreach (var p in pi.Postfixes)
                                _log.LogDebug("[OI][ui] 同目标 Postfix：owner=" + p.owner
                                    + " priority=" + p.priority + " method=" + p.PatchMethod.Name);
                        }
                    }
                    catch { }
                    _producerHooked = true;
                    ok++;
                    Suzerain.DiagKit.Diag.Ok("TemplateHUDStat.GetStatTooltipText()（悬停渲染源 · 明细在生产者处注入）");
                }
                else
                {
                    miss++;
                    Suzerain.DiagKit.Diag.Missing("TemplateHUDStat.GetStatTooltipText()",
                        "方法缺失：明细只能走「写槽位」老路，仍可能被游戏的重写冲掉");
                    _log.LogWarning("[OI][ui] 未找到 GetStatTooltipText() —— 路线①-A 不可用，退回只写槽位");
                }
            }
            catch (Exception e)
            {
                miss++;
                Suzerain.DiagKit.Diag.Fail("TemplateHUDStat.GetStatTooltipText()", e.Message);
            }

            
            
            try
            {
                var tmpType = FindTmpTextType();
                MethodInfo setter = null;
                if (tmpType != null)
                {
                    foreach (var m in tmpType.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
                    {
                        if (m.Name != "set_text") continue;
                        var ps = m.GetParameters();
                        if (ps.Length == 1 && ps[0].ParameterType == typeof(string)) { setter = m; break; }
                    }
                    if (setter == null)
                    {
                        var prop = tmpType.GetProperty("text",
                            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                        if (prop != null) setter = prop.GetSetMethod(true);
                    }
                }

                if (setter != null)
                {
                    var post = new HarmonyMethod(typeof(EconomyPatches).GetMethod(
                        nameof(Post_TmpTextAssigned),
                        BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public));
                    harmony.Patch(setter, postfix: post);
                    _rewriteGuardHooked = true;
                    ok++;
                    Suzerain.DiagKit.Diag.Ok(setter.DeclaringType.Name + ".set_text（重写看门狗：被改回即即时补回明细）");
                }
                else
                {
                    miss++;
                    Suzerain.DiagKit.Diag.Missing("TMPro.TextMeshProUGUI.set_text（重写看门狗）",
                        tmpType == null
                            ? "未加载到 Unity.TextMeshPro —— 主槽被游戏重写后只能等下一次刷新才补回"
                            : "set_text 签名不符 —— 同上");
                    _log.LogWarning("[OI][ui] 重写看门狗不可用（TMP set_text 未挂上）—— 主槽被重写后靠下次刷新补回");
                }
            }
            catch (Exception e)
            {
                miss++;
                Suzerain.DiagKit.Diag.Fail("TMPro.TextMeshProUGUI.set_text（重写看门狗）", e.Message);
            }

            
            _detailMount = (_detailPathA ? "B+A" : "B（仅主槽）")
                + "｜生产者=" + (_producerHooked ? "开" : "未挂")
                + " 看门狗=" + (_rewriteGuardHooked ? "开" : "未挂")
                + " 形态=" + (EconomyConfig.InGameDetailStyleKind == InGameDetailFormat.StyleRizia ? "Rizia" : "Detailed");
            if (EconomyConfig.InGameDetailStyleKind == InGameDetailFormat.StyleRizia
                && EconomyConfig.InGameDetailFrameOn)
            {
                
                _log.LogInfo("[OI][ui] [Economy] InGameDetailFrame=true 在 Rizia 形态下**无效**"
                    + "（Rizia 实测无框，本形态不会画任何框线）；要文本框请把 InGameDetailStyle 设为 Detailed");
            }
            _log.LogInfo("[OI][ui] 预算明细渲染：已选路径 " + _detailMount
                + (EconomyConfig.InGameDetail != null && EconomyConfig.InGameDetail.Value ? "（开关开）" : "（开关关，不介入）"));
        }

        
        
        
        
        
        
        
        private static Type FindTmpTextType()
        {
            foreach (var n in new[] { "Unity.TextMeshPro", "UnityEngine.TextMeshPro" })
            {
                try
                {
                    var a = Assembly.Load(n);
                    var t = a == null ? null : a.GetType("TMPro.TextMeshProUGUI");
                    if (t != null) return t;
                }
                catch { }
            }
            try
            {
                foreach (var a in AppDomain.CurrentDomain.GetAssemblies())
                {
                    try
                    {
                        var t = a.GetType("TMPro.TextMeshProUGUI") ?? a.GetType("TMPro.TMP_Text");
                        if (t != null) return t;
                    }
                    catch { }
                }
            }
            catch { }
            return null;
        }

        
        
        
        
        private static void Post_HudPanelShow(object __instance)
        {
            try
            {
                if (__instance == null || !Reflect.Usable(__instance)) return;
                var first = _hudPanel == null;
                _hudPanel = __instance;
                if (first)
                    _log.LogInfo("[OI][ui] 已记录 HUDPanel 实例（供预算统计兜底定位）");
            }
            catch { }
        }

        
        
        
        
        
        
        private static void Post_SetLocalizableTexts(object __instance)
        {
            try
            {
                if (__instance == null || !ReferenceEquals(__instance, _budgetStat)) return;
                
                RefreshDetailPreview("切语言/重新本地化后");
            }
            catch { }
        }

        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        

        
        
        
        
        
        
        
        
        
        
        
        
        
        
        private static void Post_GetStatTooltipText(object __instance, ref string __result)
        {
            try
            {
                if (EconomyConfig.InGameDetail == null || !EconomyConfig.InGameDetail.Value) return;
                if (!EconomyConfig.InGameDetailProducerOn) return;

                
                bool budget = __instance != null && _budgetStat != null && ReferenceEquals(__instance, _budgetStat);
                string statName = null;
                if (!budget)
                {
                    statName = StatNameOf(__instance);
                    budget = IsBudgetStat(__instance, null, statName);
                }
                if (!budget) return;
                if (statName == null) statName = StatNameOf(__instance);
                AdoptBudgetStat(__instance, statName, "GetStatTooltipText");
                AdoptTooltipTmps(__instance);

                _producerCalls++;                                   
                
                var text = ComposeBudgetBlocks();
                if (text.Length == 0) return;              

                var before = __result;
                bool fingerprint;
                var merged = _mainMerge.Merge(MergeKey(__instance), before, text, out fingerprint);
                if (string.Equals(merged, before, StringComparison.Ordinal)) return;   
                __result = merged;

                _producerInjectCount++;
                if (!_producerLogged || _producerInjectCount % 50 == 0)
                {
                    _producerLogged = true;
                    _log.LogInfo("[OI][ui] 生产者注入 #" + _producerInjectCount
                        + "：GetStatTooltipText 返回 " + (before == null ? -1 : before.Length)
                        + " → " + merged.Length + " 字符（明细 " + text.Length + " 字符）"
                        + " 内存指纹命中=" + fingerprint
                        + " stat=" + Describe(statName));
                }
            }
            catch { }   
        }

        
        
        
        
        
        
        
        
        
        
        
        
        
        private static void Post_TmpTextAssigned(object __instance, object __0)
        {
            if (__instance == null) return;
            if (QuoteAnimProbe.Enabled)
            {
                _probeSetTextCount++;
                if (_probeSetTextCount <= 5 || _probeSetTextCount % 100 == 0)
                {
                    string curProbe = Reflect.Str(Reflect.Prop(__instance, "text"));
                    QuoteAnimProbe.Line("set_text#" + _probeSetTextCount + " len="
                        + (curProbe == null ? -1 : curProbe.Length) + " hash=" + QuoteAnimProbe.Hash(curProbe)
                        + " writing=" + _writing + " active=" + _quoteAnimActive);
                }
            }
            if (!ReferenceEquals(__instance, _mainTmp)) return;      
            if (_writing) return;                                    
            if (EconomyConfig.InGameDetail == null || !EconomyConfig.InGameDetail.Value) return;
            if (!EconomyConfig.InGameDetailProducerOn) return;
            
            
            
            if (EconomyConfig.QuotesEnabledOn)
            {
                if (!_quoteTickerHooked) AdvanceQuoteAnimation(); 
                return;
            }

            bool probe = _perfProbeLeft > 0;                         
            if (probe) _perfProbeLeft--;
            long t0 = System.Diagnostics.Stopwatch.GetTimestamp();
            _wdFires++;
            try
            {
                
                
                
                string cur = probe ? null : Reflect.Str(__0);
                if (cur == null) cur = Reflect.Str(Reflect.Prop(__instance, "text"));
                if (cur == null) return;

                
                
                string text;
                if (probe)
                {
                    long c0 = System.Diagnostics.Stopwatch.GetTimestamp();
                    text = ComposeDetail();
                    _composeTicks += System.Diagnostics.Stopwatch.GetTimestamp() - c0;
                    _composeRuns++;
                }
                else
                {
                    text = ComposeBudgetBlocks();
                }
                if (text.Length == 0) return;

                
                
                
                
                if (!probe
                    && ReferenceEquals(_mainMerge.Owner, __instance)
                    && string.Equals(cur, _mainMerge.LastMerged, StringComparison.Ordinal)
                    && string.Equals(text, _lastDetailWritten, StringComparison.Ordinal))
                {
                    _wdNoop++;
                    return;
                }

                bool fingerprint;
                var merged = _mainMerge.Merge(MergeKey(__instance), cur, text, out fingerprint);
                _lastDetailWritten = text;

                
                if (string.Equals(merged, cur, StringComparison.Ordinal)) { _wdNoop++; return; }

                _writing = true;
                try
                {
                    if (!Reflect.SetPropOrField(__instance, "text", merged)) return;
                }
                finally { _writing = false; }

                var back = Reflect.Str(Reflect.Prop(__instance, "text"));
                _reassertCount++;
                _wdWrites++;
                if (!_reassertLogged || _reassertCount % 25 == 0)
                {
                    _reassertLogged = true;
                    _log.LogInfo("[OI][ui] 重写看门狗 #" + _reassertCount
                        + "：主 statTooltipText 被改成 " + cur.Length + " 字符（不含我方明细，内存指纹命中=" + fingerprint + "）"
                        + " → 即时补回 " + merged.Length + " 字符，回读一致="
                        + string.Equals(merged, back, StringComparison.Ordinal)
                        + " ｜ 原文=" + Describe(cur)
                        + " stat=" + Describe(StatNameOf(_budgetStat)));
                }
            }
            catch { }
            finally
            {
                
                long dt = System.Diagnostics.Stopwatch.GetTimestamp() - t0;
                if (probe) { _probeTicks += dt; _probeN++; } else { _fastTicks += dt; _fastN++; }

                if (probe && _perfProbeLeft == 0 && !_perfReported)
                {
                    _perfReported = true;
                    PerfLog("A/B 探针完成（同一段悬停：前 " + PerfProbeFires + " 次=旧路径，之后=新路径）");
                }
                else if (!probe && _wdFires % 200 == 0)
                {
                    PerfLog("稳态 #" + _wdFires);
                }
            }
        }

        
        
        
        
        private static void AdoptTooltipTmps(object stat)
        {
            try
            {
                if (stat == null || !Reflect.Usable(stat)) return;
                var main = Reflect.Prop(stat, "statTooltipText");
                if (Reflect.Usable(main)) _mainTmp = main;
                var backup = Reflect.Prop(stat, "statTooltipModifierText");
                if (Reflect.Usable(backup)) _backupTmp = backup;
            }
            catch { }
        }
    }
}
