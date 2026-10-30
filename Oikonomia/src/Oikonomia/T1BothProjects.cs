























using System;
using System.Collections;
using System.Reflection;
using HarmonyLib;

namespace Oikonomia
{
    internal static partial class EconomyPatches
    {
        private static int _t1bLines;
        private static int _t1bAppended;
        private static string _t1bLastKey = "";

        
        internal static void InstallT1BothProjects(Harmony harmony, Assembly asm, ref int ok, ref int miss)
        {
            try
            {
                if (EconomyConfig.T1Both == null || !EconomyConfig.T1BothOn)
                {
                    _log.LogDebug("[OI][t1] T1 双项目补丁：已关闭（未挂任何钩子）");
                    return;
                }
                if (asm == null) { _log.LogWarning("[OI][t1] 跳过：Assembly-CSharp 未加载"); return; }
                var t = Reflect.FindType(asm, "ConversationHandler");
                if (t == null) { miss++; _log.LogWarning("[OI][t1] **未挂上**：找不到 ConversationHandler"); return; }
                var post = new HarmonyMethod(typeof(EconomyPatches).GetMethod(
                    nameof(Post_T1Both), BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public));
                int n = 0;
                if (!EconomyConfig.T1HookMenuOn)
                {
                    _log.LogInfo("[OI][t1][install] HookMenu=false ⇒ 跳过菜单 Postfix");
                }
                else
                {
                    foreach (var m in t.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
                    {
                        if (m == null || m.Name != "OnConversationResponseMenu" || m.GetParameters().Length != 1) continue;
                        try { harmony.Patch(m, postfix: post); n++; _log.LogInfo("[OI][t1] 已挂 Postfix：ConversationHandler.OnConversationResponseMenu(1 参数)"); }
                        catch (Exception e) { _log.LogWarning("[OI][t1] 挂载失败（跳过）：" + e.Message); }
                    }
                    if (n > 0) ok++; else { miss++; _log.LogWarning("[OI][t1] **未挂上**：OnConversationResponseMenu(1) 不存在或签名不同"); }
                }
                
                try
                {
                    if (!EconomyConfig.T1HookTextOn)
                    {
                        _log.LogInfo("[OI][t1][install] HookText=false ⇒ 跳过 SetLocalizableTexts Postfix");
                    }
                    else
                    {
                        var tResp = Reflect.FindType(asm, "TemplateConversationResponse");
                        var mTxt = tResp == null ? null : tResp.GetMethod("SetLocalizableTexts", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                        if (mTxt != null)
                        {
                            harmony.Patch(mTxt, postfix: new HarmonyMethod(typeof(EconomyPatches).GetMethod(nameof(Post_T1SetLocalizableTexts), BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public)));
                            _log.LogInfo("[OI][t1] 已挂 Postfix：TemplateConversationResponse.SetLocalizableTexts() ✓（文本装配点 ✓）");
                        }
                        else _log.LogWarning("[OI][t1] 未挂上 ✗：SetLocalizableTexts 未找到");
                    }
                }
                catch (Exception eT) { _log.LogWarning("[OI][t1] 文本钩子安装异常（忽略）✗：" + eT.GetType().Name); }
                
                try
                {

                
                try
                {
                    if (!EconomyConfig.T1HookClickOn)
                    {
                        _log.LogInfo("[OI][t1][install] HookClick=false ⇒ 跳过点击拦截");
                    }
                    else
                    {
                        var mClick = t.GetMethod("OnResponseClick", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                        if (mClick != null)
                        {
                            harmony.Patch(mClick, prefix: new HarmonyMethod(typeof(EconomyPatches).GetMethod(nameof(Prefix_T1ResponseClick), BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public)));
                            _log.LogInfo("[OI][t1][install] ★ 点击拦截（OnResponseClick Prefix）= 挂上 ✓");
                        }
                        else _log.LogWarning("[OI][t1][install] OnResponseClick 未找到 ⇒ 点击拦截未挂 ✗");
                    }
                }
                catch (Exception eCi) { _log.LogWarning("[OI][t1][install] 点击拦截挂载失败 ✗：" + eCi.GetType().Name + " — " + Short(eCi.Message)); }

                try
                {
                    if (!EconomyConfig.T1HookAskOn)
                    {
                        _log.LogInfo("[OI][t1][install] HookAsk=false ⇒ 跳过阶段二 Ask Prefix");
                    }
                    else
                    {
                        var mAsk = t.GetMethod("OnConversationResponseMenu", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                        if (mAsk != null)
                        {
                            harmony.Patch(mAsk, prefix: new HarmonyMethod(typeof(EconomyPatches).GetMethod(nameof(Prefix_T1Ask), BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public)));
                            _log.LogInfo("[OI][t1][install] ★ 阶段二框架（T1Ask Prefix）= 挂上 ✓（★ 默认 Enabled=false ⇒ 完全惰性 ✓）");
                        }
                    }
                }
                catch (Exception eAi) { _log.LogWarning("[OI][t1][install] 阶段二框架挂载失败 ✗：" + eAi.GetType().Name); }
                    if (!EconomyConfig.T1HookPrefixOn)
                    {
                        _log.LogInfo("[OI][t1][install] HookPrefix=false ⇒ 跳过两条 OnConversationResponseMenu Prefix");
                    }
                    else
                    {
                        var mMenu = t.GetMethod("OnConversationResponseMenu", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                        if (mMenu != null)
                        {
                            try
                            {
                                harmony.Patch(mMenu, prefix: new HarmonyMethod(typeof(EconomyPatches).GetMethod(nameof(Prefix_T1RefArray), BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public)));
                                _log.LogInfo("[OI][t1][install] ★ (b1) ref 版 Prefix = 挂上 ✓（★ 若 HarmonyX 拒绝 ref object ⇒ 上面会另有日志 ✓）");
                            }
                            catch (Exception eP1) { _log.LogWarning("[OI][t1][install] (b1) ref 版 Prefix **未挂上** ✗：" + eP1.GetType().Name + " — " + Short(eP1.Message) + "（★ 走 (b2) 退路 ✓）"); }
                            try
                            {
                                harmony.Patch(mMenu, prefix: new HarmonyMethod(typeof(EconomyPatches).GetMethod(nameof(Prefix_T1CreateBefore), BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public)));
                                _log.LogInfo("[OI][t1][install] ★ (b2) 退路 Prefix（先调 CreateResponse）= 挂上 ✓");
                            }
                            catch (Exception eP2) { _log.LogWarning("[OI][t1][install] (b2) 退路 Prefix 未挂上 ✗：" + eP2.GetType().Name); }
                        }
                        else _log.LogWarning("[OI][t1][install] OnConversationResponseMenu 未找到 ⇒ 两条 Prefix 都未挂 ✗");
                    }
                }
                catch (Exception ePx) { _log.LogWarning("[OI][t1][install] Prefix 安装异常：" + ePx.GetType().Name); }
                _log.LogInfo("[OI][t1] 安装完成：钩子 " + n + "｜特征串=[" + CfgT1("TargetText", "")
                    + "]｜文案=[" + CfgT1("OptionText", "同时为两个项目拨款") + "]｜回退：cfg [T1] Enabled=false ✓");
            }
            catch (Exception e) { _log.LogWarning("[OI][t1] 安装异常（忽略，不影响游戏）：" + e.Message); }
        }

        private static string CfgT1(string key, string dflt)
        {
            try
            {
                if (key == "TargetText") return EconomyConfig.T1TargetText != null ? EconomyConfig.T1TargetText.Value : dflt;
                if (key == "OptionText") return EconomyConfig.T1OptionText != null ? EconomyConfig.T1OptionText.Value : dflt;

                if (key == "T1BothLine") return EconomyConfig.T1BothLine != null ? EconomyConfig.T1BothLine.Value : dflt;
                if (key == "T1ChainLileas") return EconomyConfig.T1ChainLileas != null ? EconomyConfig.T1ChainLileas.Value : dflt;
                if (key == "T1ChainGus") return EconomyConfig.T1ChainGus != null ? EconomyConfig.T1ChainGus.Value : dflt;
                if (key == "T1ChainSymon") return EconomyConfig.T1ChainSymon != null ? EconomyConfig.T1ChainSymon.Value : dflt;
                if (key == "T1ChainSymon2") return EconomyConfig.T1ChainSymon2 != null ? EconomyConfig.T1ChainSymon2.Value : dflt;
                
                
                if (key == "Diag") return EconomyConfig.T1Diag != null ? (EconomyConfig.T1Diag.Value ?? "").Trim().ToLowerInvariant() : dflt;

                
                
                if (key == "T1AskEnabled") return EconomyConfig.T1AskEnabled != null ? (EconomyConfig.T1AskEnabled.Value ? "true" : "false") : dflt;
                if (key == "T1AskToken") return EconomyConfig.T1AskToken != null ? EconomyConfig.T1AskToken.Value : dflt;
                if (key == "T1AskOption") return EconomyConfig.T1AskOption != null ? EconomyConfig.T1AskOption.Value : dflt;
                if (key == "T1AskSimon") return EconomyConfig.T1AskSimon != null ? EconomyConfig.T1AskSimon.Value : dflt;
                if (key == "T1AskSimonEcon") return EconomyConfig.T1AskSimonEcon != null ? EconomyConfig.T1AskSimonEcon.Value : dflt;
                _log.LogWarning("[OI][t1][cfg] 未登记键=" + key + " ⇒ 用默认值 ✓（★ 若这是台词/开关 ⇒ 说明登记漏了一节，请补 CfgT1 的 if ✓）");
            }
            catch { }
            return dflt;
        }

        
        private static void Post_T1Both(object __instance, object __0)
        {
            try
            {
                if (EconomyConfig.T1Both == null || !EconomyConfig.T1BothOn) return;
                if (_t1bLines >= 40) return;
                if (__instance == null || __0 == null) return;

                
                int cnt = -1;
                try { var lv = Reflect.Prop(__0, "Length"); if (lv != null) cnt = Convert.ToInt32(lv); } catch { }
                if (cnt < 0) { try { var cv = Reflect.Prop(__0, "Count"); if (cv != null) cnt = Convert.ToInt32(cv); } catch { } }
                object first = Index0(__0, cnt);   
                object src = first;
                int srcIdx = 0;
                if (cnt > 1)
                {
                    var s2 = IndexN(__0, cnt - 1);
                    if (s2 != null) { src = s2; srcIdx = cnt - 1; }
                }
                _log.LogInfo("[OI][t1][src] srcIdx=" + srcIdx + " cnt=" + cnt);
                if (first == null) return;

                object ft = null; string text = "?";
                try
                {
                    ft = Reflect.Prop(first, "formattedText");
                    var t2 = Reflect.Prop(ft, "text") ?? Reflect.Prop(ft, "Text") ?? ft;
                    text = t2 == null ? "?" : t2.ToString();
                }
                catch { }
                _t1bLines++;

                
                
                string target = CfgT1("TargetText", "");
                
                
                bool hit = false; int hitAt = -1; string hitText = "";
                if (!string.IsNullOrEmpty(target))
                {
                    int nAll = cnt < 0 ? 0 : cnt;
                    for (int ri = 0; ri < nAll; ri++)
                    {
                        var rt = ItemText(__0, ri);
                        if (string.IsNullOrEmpty(rt)) continue;
                        foreach (var tok in target.Split(new[] { '|', ';', ',' }, StringSplitOptions.RemoveEmptyEntries))
                        {
                            var q = tok.Trim();
                            if (q.Length == 0) continue;
                            if (rt.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0) { hit = true; hitAt = ri; hitText = rt; break; }
                        }
                        if (hit) break;
                    }
                }
                if (hit) _log.LogInfo("[OI][t1][hit] ★ 目标菜单（**第 " + hitAt + " 项**命中 ✓：" + Short(hitText) + "／共 " + cnt + " 项）⇒ 追加 ✓");
                if (!hit)
                {
                    _log.LogInfo("[OI][t1][skip] 非目标对话（已遍历 " + cnt + " 项；需命中[" + target + "]之一；首项=" + Short(text) + "）⇒ 不改 ✓");
                    return;
                }
                string dkey = __instance.GetHashCode().ToString() + "|" + text;
                if (_t1bLastKey == dkey) { _log.LogInfo("[OI][t1][skip] 同一菜单重复触发 ⇒ 不重复追加 ✓"); return; }
                _t1bLastKey = dkey;

                var tpl = Reflect.Prop(__instance, "templateResponse");
                var listObj = Reflect.Prop(__instance, "instantiatedResponses");
                var list = listObj as IList;   
                var cont = Reflect.Prop(__instance, "responseContainer");
                _log.LogInfo("[OI][t1][probe] 可写面：templateResponse=" + (tpl == null ? "无 ✗" : tpl.GetType().Name)
                    + " instantiatedResponses=" + (list == null ? "非 IList ✗（缺 API ✗）" : "IList ✓ Count=" + list.Count)
                    + " responseContainer=" + (cont == null ? "无 ✗" : cont.GetType().Name));

                if (tpl == null) { _log.LogWarning("[OI][t1] 缺 API ✗：templateResponse 取不到 ⇒ 不追加"); return; }
                _log.LogInfo("[OI][t1][probe2] 列表 IList=" + (list != null) + " 反射 Count=" + ListCount(listObj) + "（★ 2.10.1：不是 IList 也继续 ⇒ 走反射 Add ✓）");

                
                object inst = null; string how1;
                try
                {
                    var uo = ResolveType("UnityEngine.Object");
                    var mi = uo == null ? null : uo.GetMethod("Instantiate", new[] { uo });
                    if (mi == null) { how1 = "缺 API ✗（UnityEngine.Object.Instantiate 未解析到）"; }
                    else { inst = mi.Invoke(null, new object[] { tpl }); how1 = inst == null ? "Instantiate 返回 null ✗" : "✓"; }
                }
                catch (Exception e1) { how1 = "抛异常 ✗ " + e1.GetType().Name; }
                
                string myText = CfgT1("OptionText", "同时为两个项目拨款");
                int v2Before = ChildCount(cont);
                string sList = listObj == null ? "null" : "有";
                string sCont = cont == null ? "null" : "有";
                _log.LogInfo("[OI][t1][v2] 起点 child=" + v2Before + " listObj=" + sList + " cont=" + sCont);
                int listBefore = ListCount(listObj);   
                try
                {
                    object ui0 = IndexN(listObj, 0);
                    object uiGo = ui0 == null ? null : Reflect.Prop(ui0, "gameObject");
                    var uoType = ResolveType("UnityEngine.Object");
                    string sUi = ui0 == null ? "无" : ui0.GetType().Name;
                    string sGo = uiGo == null ? "无" : "有";
                    _log.LogInfo("[OI][t1][v2] ui0=" + sUi + " uiGo=" + sGo);
                    if (uiGo != null && uoType != null)
                    {
                        var miInst = uoType.GetMethod("Instantiate", new[] { uoType });
                        object newGo = miInst == null ? null : miInst.Invoke(null, new object[] { uiGo });
                        _log.LogInfo("[OI][t1][v2-2] 克隆 gameObject = " + (newGo == null ? "null ✗" : "ok ✓"));
                        if (newGo != null)
                        {
                            var tyC = ui0.GetType();
                            object newComp = null;
                            var gc1 = tyC.GetMethod("GetComponent", new[] { typeof(Type) });
                            if (gc1 != null) newComp = gc1.Invoke(newGo, new object[] { tyC });
                            if (newComp == null)
                            {
                                var gcs = tyC.GetMethod("GetComponentsInChildren", new[] { typeof(Type) });
                                var arrC = gcs == null ? null : gcs.Invoke(newGo, new object[] { tyC });
                                newComp = arrC == null ? null : IndexN(arrC, 0);
                            }
                            _log.LogInfo("[OI][t1][v2-2] 新组件 = " + (newComp == null ? "null ✗" : newComp.GetType().Name + " ✓"));
                                if (newComp != null) { _t1Ours.Add(newComp.GetHashCode()); _log.LogInfo("[OI][t1][v2-2] 已登记为我们新增的项 ✓（只它会走 text-hook ✓）"); }
                            if (newComp != null)
                            {
                                object txt = Reflect.Prop(newComp, "text");
                                bool okTxt = txt != null && Reflect.SetPropOrField(txt, "text", myText);
                                _log.LogInfo("[OI][t1][v2-2] get_text()=" + (txt == null ? "null ✗" : txt.GetType().Name) + " 写入=" + okTxt + "（★ 直接改 UI 文本 ✓ 不碰 FormattedText ✗）");
                                object btn = Reflect.Prop(newComp, "responseButton");
                                object onClick = btn == null ? null : Reflect.Prop(btn, "onClick");
                                _log.LogInfo("[OI][t1][v2-2] get_responseButton()=" + (btn == null ? "null ✗" : btn.GetType().Name) + " onClick=" + (onClick == null ? "null ✗" : onClick.GetType().Name));
                                if (onClick != null)
                                {
                                    try { var ral = onClick.GetType().GetMethod("RemoveAllListeners", Type.EmptyTypes); if (ral != null) ral.Invoke(onClick, null); } catch { }
                                    string cbHow2; object cb2 = BuildUnityActionFor(typeof(EconomyPatches).GetMethod("T1BothClick", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public), out cbHow2);
                                    var uaType2 = ResolveType("UnityEngine.Events.UnityAction");
                                    var addL = (uaType2 == null) ? null : onClick.GetType().GetMethod("AddListener", new[] { uaType2 });
                                    if (addL != null && cb2 != null) { addL.Invoke(onClick, new object[] { cb2 }); _log.LogInfo("[OI][t1][v2-2] 点击监听已加 ✓（" + cbHow2 + "）"); }
                                    else _log.LogWarning("[OI][t1][v2-2] 点击监听未加 ✗（addListener=" + (addL != null) + " cb=" + (cb2 != null) + "）");
                                }
                            }
                        }
                    }
                    else _log.LogWarning("[OI][t1][v2] 前提缺 ✗：uiGo 或 UnityEngine.Object 未解析到");
                }
                catch (Exception ev2) { _log.LogWarning("[OI][t1][v2] 抛异常 ✗：" + ev2.GetType().Name + " — " + Short(ev2.Message)); }
                try
                {
                    
                    object cloneR = null;
                    try { cloneR = Activator.CreateInstance(src.GetType(), new object[] { Reflect.Prop(src, "formattedText"), Reflect.Prop(src, "destinationEntry"), true }); } catch (Exception eR) { _log.LogWarning("[OI][t1][v2-1] 造 Response 失败 ✗：" + eR.GetType().Name); }
                    if (cloneR != null)
                    {
                        string cbHow1; object cb1 = BuildUnityActionFor(typeof(EconomyPatches).GetMethod("T1BothClick", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public), out cbHow1);
                        var cr = __instance.GetType().GetMethod("CreateResponse", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                        _log.LogInfo("[OI][t1][v2-1] CreateResponse=" + (cr == null ? "未找到 ✗" : cr.GetParameters().Length + " 参 ✓") + " cb=" + cbHow1);
                        if (cr != null && cr.GetParameters().Length == 3)
                        {
                            cr.Invoke(__instance, new object[] { cloneR, cnt, cb1 });
                            
                            try
                            {
                                int ln = ListCount(listObj);
                                object nu = IndexN(listObj, ln - 1);
                                    
                                    try
                                    {
                                        object goNew = Reflect.Prop(nu, "gameObject");
                                        object rtNew = Reflect.Prop(nu, "transform");
                                        if (rtNew == null && goNew != null)
                                        {
                                            var rtType = ResolveType("UnityEngine.RectTransform");
                                            var g3 = rtType == null ? null : goNew.GetType().GetMethod("GetComponent", new[] { typeof(Type) });
                                            if (g3 != null) rtNew = g3.Invoke(goNew, new object[] { rtType });
                                        }
                                        object u0 = IndexN(listObj, 0);
                                        object rt0 = u0 == null ? null : Reflect.Prop(u0, "transform");
                                        string sGeom(object rt)
                                        {
                                            if (rt == null) return "rt=null ✗";
                                            var sb3 = new System.Text.StringBuilder();
                                            sb3.Append("type=").Append(rt.GetType().Name);
                                            var sd4 = Reflect.Prop(rt, "sizeDelta"); sb3.Append(" sizeDelta=").Append(sd4 == null ? "读失败 ✗" : sd4.ToString());
                                            var ap4 = Reflect.Prop(rt, "anchoredPosition"); sb3.Append(" anchoredPos=").Append(ap4 == null ? "读失败 ✗" : ap4.ToString());
                                            var lp4 = Reflect.Prop(rt, "localPosition"); sb3.Append(" localPos=").Append(lp4 == null ? "读失败 ✗" : lp4.ToString());
                                            var gp4 = Reflect.Prop(rt, "parent");
                                            string pn4 = gp4 == null ? "null ✗" : Short(Convert.ToString(Reflect.Prop(gp4, "name")));
                                            sb3.Append(" parent=").Append(pn4);
                                            return sb3.ToString();
                                        }
                                        Diag("[OI][t1][geom] 新项：" + sGeom(rtNew));
                                        Diag("[OI][t1][geom] 第 0 项：" + sGeom(rt0));
                                        object parN = rtNew == null ? null : Reflect.Prop(rtNew, "parent");
                                        object par0 = rt0 == null ? null : Reflect.Prop(rt0, "parent");
                                        string pn = parN == null ? "null ✗" : Short(Convert.ToString(Reflect.Prop(parN, "name")));
                                        string p0 = par0 == null ? "null ✗" : Short(Convert.ToString(Reflect.Prop(par0, "name")));
                                        bool samePar = pn == p0 && pn != "null ✗";
                                        Diag("[OI][t1][geom] 父节点对比：新=" + pn + " 第0项=" + p0 + " 同一父=" + samePar + "（★ 不同父 ⇒ 渲染源不对 ✗）");
                                        object sibN = Reflect.Prop(rtNew, "GetSiblingIndex");
                                        object sib0 = Reflect.Prop(rt0, "GetSiblingIndex");
                                        Diag("[OI][t1][geom] sibling：新=" + (sibN == null ? "读失败 ✗" : sibN.ToString()) + " 第0项=" + (sib0 == null ? "读失败 ✗" : sib0.ToString()));
                                    
                                    try
                                    {
                                        object goNew2 = Reflect.Prop(nu, "gameObject");
                                        object u0b = IndexN(listObj, 0);
                                        object go0b = u0b == null ? null : Reflect.Prop(u0b, "gameObject");
                                        object idNew = goNew2 == null ? null : Reflect.Prop(goNew2, "GetInstanceID");
                                        object id0 = go0b == null ? null : Reflect.Prop(go0b, "GetInstanceID");
                                        _log.LogInfo("[OI][t1][id] 新项 hash=" + nu.GetHashCode() + " instanceID=" + (idNew == null ? "读失败 ✗" : idNew.ToString()) + " name=" + Short(Convert.ToString(Reflect.Prop(goNew2, "name"))));
                                        _log.LogInfo("[OI][t1][id] 第0项 hash=" + (u0b == null ? "null ✗" : u0b.GetHashCode().ToString()) + " instanceID=" + (id0 == null ? "读失败 ✗" : id0.ToString()) + " name=" + Short(Convert.ToString(Reflect.Prop(go0b, "name"))));
                                        string sameId = (idNew != null && id0 != null && idNew.ToString() == id0.ToString()) ? "**相同 ✗ ⇒ 读到的就是第 0 项自己**" : "不同 ✓ ⇒ 新项是独立对象 ✓";
                                        _log.LogInfo("[OI][t1][id] ★ 判据：两者 instanceID " + sameId);
                                        int ourN = _t1Ours.Count; string ourH = "无 ✗";
                                        foreach (int hh in _t1Ours) { ourH = hh.ToString(); break; }
                                        _log.LogInfo("[OI][t1][id] 已登记(_t1Ours) 数量=" + ourN + " 首个登记 hash=" + ourH + "（★ 与 新项 hash=" + nu.GetHashCode() + " 对照 ✓）");
                                        
                                        try
                                        {
                                            int lnNow = ListCount(listObj);
                                            object lastUi = lnNow > 0 ? IndexN(listObj, lnNow - 1) : null;
                                            if (lastUi != null)
                                            {
                                                string nmNow = Short(Convert.ToString(Reflect.Prop(lastUi, "name")));
                                                bool isClone = nmNow.Contains("(Clone)");
                                                _t1Ours.Add(lastUi.GetHashCode());
                                                
                                                
                                                try
                                                {
                                                    string want3 = CfgT1("OptionText", "同时为两个项目拨款");
                                                    var miSlt = lastUi.GetType().GetMethod("SetLocalizableTexts", Type.EmptyTypes);
                                                    if (miSlt != null) { miSlt.Invoke(lastUi, null); _log.LogInfo("[OI][t1][text2] ① 已主动调用我们那一项的 SetLocalizableTexts() ✓"); }
                                                    else _log.LogWarning("[OI][t1][text2] ① 缺 API：SetLocalizableTexts");
                                                    object txt2 = Reflect.Prop(lastUi, "text");
                                                    bool okW = txt2 != null && Reflect.SetPropOrField(txt2, "text", want3);
                                                    object rb = txt2 == null ? null : Reflect.Prop(txt2, "text");
                                                    string rbS = rb == null ? "读失败 ✗" : Short(rb.ToString());
                                                    _log.LogInfo("[OI][t1][text2] ② 直接写 TMP.text=" + okW + " 复读=" + rbS + "（★ 只写我们那一项 ✓）");
                                                }
                                                catch (Exception eT2) { _log.LogWarning("[OI][t1][text2] 设文字异常：" + eT2.GetType().Name); }
                                                
                                                object lgNew = Reflect.Prop(lastUi, "verticalLayoutGroup");
                                                object u0v = IndexN(listObj, 0);
                                                object lg0 = u0v == null ? null : Reflect.Prop(u0v, "verticalLayoutGroup");
                                                string sLg(object lg)
                                                {
                                                    if (lg == null) return "null ✗";
                                                    var sbL = new System.Text.StringBuilder();
                                                    sbL.Append("有 ✓ enabled=").Append(Reflect.Prop(lg, "enabled"));
                                                    sbL.Append(" spacing=").Append(Reflect.Prop(lg, "spacing"));
                                                    sbL.Append(" padding=").Append(Reflect.Prop(lg, "padding"));
                                                    return sbL.ToString();
                                                }
                                                _log.LogInfo("[OI][t1][lay] 新项 verticalLayoutGroup：" + sLg(lgNew));
                                                _log.LogInfo("[OI][t1][lay] 第0项 verticalLayoutGroup：" + sLg(lg0));
                                                _log.LogInfo("[OI][t1][lay] 同一布局组=" + (lgNew != null && lg0 != null && lgNew.GetHashCode() == lg0.GetHashCode()) + "（★ 若新项为 null/disabled ⇒ 那就是根因 ✓）");
                                                
                                                try
                                                {
                                                    object lgGo = reflectGo(lgNew);
                                                    object lgTr = Reflect.Prop(lgNew, "transform");
                                                    string lgName = Short(Convert.ToString(Reflect.Prop(lgGo, "name")));
                                                    bool sameAsCont = (lgTr != null && cont != null && lgTr.GetHashCode() == cont.GetHashCode());
                                                    object lgKids = lgTr == null ? null : Reflect.Prop(lgTr, "childCount");
                                                    _log.LogInfo("[OI][t1][lay2] 布局组宿主：name=" + lgName + " ==容器? " + sameAsCont + " childCount=" + (lgKids == null ? "读不到 ✗" : lgKids.ToString()) + "（★ 这一行确定布局组管着谁 ✓）");
                                                    _log.LogInfo("[OI][t1][lay2] ①a 切换 enabled：原=" + Reflect.Prop(lgNew, "enabled"));
                                                    
                                                    Type rtReal = cont == null ? null : cont.GetType();
                                                    _log.LogInfo("[OI][t1][type] RectTransform 类型：ResolveType（按全名）=" + (ResolveType("UnityEngine.RectTransform") == null ? "null ✗" : "有 ✓")
                                                        + " ｜ 容器实例类型=" + (rtReal == null ? "null ✗" : rtReal.FullName + " ✓")
                                                        + " ｜ ResolveTypeSimple=" + (ResolveTypeSimple("RectTransform") == null ? "null ✗" : "有 ✓"));
                                                    if (rtReal == null) { var tmpR = ResolveTypeSimple("RectTransform"); rtReal = tmpR; }
                                                    if (rtReal != null && cont != null)
                                                    {
                                                        var lrT3 = ResolveType("UnityEngine.UI.LayoutRebuilder") ?? ResolveTypeSimple("LayoutRebuilder");
                                                        var miRb3 = lrT3 == null ? null : lrT3.GetMethod("ForceRebuildLayoutImmediate", BindingFlags.Public | BindingFlags.Static, null, new[] { rtReal }, null);
                                                        var mkRb3 = lrT3 == null ? null : lrT3.GetMethod("MarkLayoutForRebuild", BindingFlags.Public | BindingFlags.Static, null, new[] { rtReal }, null);
                                                        if (miRb3 != null) { miRb3.Invoke(null, new object[] { cont }); _log.LogInfo("[OI][t1][lay3] ★ ①b 已 ForceRebuildLayoutImmediate(容器) ✓（★ 布局组宿主=容器 ✓ 这次类型对了 ✓）"); }
                                                        else _log.LogWarning("[OI][t1][lay3] ①b 仍缺 API ✗（LayoutRebuilder=" + (lrT3 == null ? "null ✗" : "有 ✓") + "）");
                                                        if (mkRb3 != null) { mkRb3.Invoke(null, new object[] { cont }); _log.LogInfo("[OI][t1][lay3] ①c 已 MarkLayoutForRebuild(容器) ✓"); }
                                                        else _log.LogWarning("[OI][t1][lay3] ①c 仍缺 API ✗");
                                                        object sdC = Reflect.Prop(cont, "sizeDelta");
                                                        object apC = Reflect.Prop(cont, "anchoredPosition");
                                                        _log.LogInfo("[OI][t1][lay3] 重读容器几何：sizeDelta=" + (sdC == null ? "读失败 ✗" : sdC.ToString()) + " anchoredPos=" + (apC == null ? "读失败 ✗" : apC.ToString()) + "（★ 读到值 ⇒ 类型修好了 ✓）");
                                                        
                                                        try
                                                        {
                                                            string chain2 = "";
                                                            object cur2 = cont;
                                                            int lvl2 = 0;
                                                            while (cur2 != null && lvl2 < 6)
                                                            {
                                                                string cn2 = Short(Convert.ToString(Reflect.Prop(cur2, "name")));
                                                                object sd8 = Reflect.Prop(cur2, "sizeDelta");
                                                                bool hasMask2 = false;
                                                                var gc8 = cur2.GetType().GetMethod("GetComponent", new[] { typeof(Type) });
                                                                var mkT2 = ResolveType("UnityEngine.UI.Mask") ?? ResolveTypeSimple("Mask");
                                                                var rmT2 = ResolveType("UnityEngine.UI.RectMask2D") ?? ResolveTypeSimple("RectMask2D");
                                                                if (gc8 != null && mkT2 != null) { try { if (gc8.Invoke(cur2, new object[] { mkT2 }) != null) hasMask2 = true; } catch { } }
                                                                if (gc8 != null && rmT2 != null) { try { if (gc8.Invoke(cur2, new object[] { rmT2 }) != null) hasMask2 = true; } catch { } }
                                                                chain2 = chain2 + " │[" + lvl2 + "] " + cn2 + " sizeDelta=" + (sd8 == null ? "读失败 ✗" : sd8.ToString()) + " Mask=" + hasMask2;
                                                                cur2 = Reflect.Prop(cur2, "parent");
                                                                lvl2++;
                                                            }
                                                        
                                                        
                                                        try
                                                        {
                                                            object vp = null; object ct2 = null; object sc = null;
                                                            object walk = cont;
                                                            for (int wi = 0; wi < 6 && walk != null; wi++)
                                                            {
                                                                string wn = Convert.ToString(Reflect.Prop(walk, "name"));
                                                                if (wn == "Viewport") vp = walk;
                                                                if (wn == "Content") ct2 = walk;
                                                                if (sc == null)
                                                                {
                                                                    var scT = ResolveType("UnityEngine.UI.ScrollRect") ?? ResolveTypeSimple("ScrollRect");
                                                                    var gc9 = walk.GetType().GetMethod("GetComponent", new[] { typeof(Type) });
                                                                    if (gc9 != null && scT != null) { try { sc = gc9.Invoke(walk, new object[] { scT }); } catch { } }
                                                                }
                                                                walk = Reflect.Prop(walk, "parent");
                                                            }
                                                            string sVpN = vp == null ? "未找到 ✗" : Short(Convert.ToString(Reflect.Prop(vp, "name")));
                                                            string sCtN = ct2 == null ? "未找到 ✗" : Short(Convert.ToString(Reflect.Prop(ct2, "name")));
                                                            string sSc = sc == null ? "未找到 ✗" : "有 ✓ vNorm=" + Convert.ToString(Reflect.Prop(sc, "verticalNormalizedPosition"));
                                                            _log.LogInfo("[OI][t1][scroll] Viewport=" + sVpN + " ｜ Content=" + sCtN + " ｜ ScrollRect=" + sSc);
                                                            if (ct2 != null)
                                                            {
                                                                var vT2 = ResolveType("UnityEngine.Vector2") ?? ResolveTypeSimple("Vector2");
                                                                object sdC2 = Reflect.Prop(ct2, "sizeDelta");
                                                                if (vT2 != null)
                                                                {
                                                                    object vBig = Activator.CreateInstance(vT2, new object[] { 642f, 178f });
                                                                    bool okC = Reflect.SetPropOrField(ct2, "sizeDelta", vBig);
                                                                    string sdC3 = Convert.ToString(Reflect.Prop(ct2, "sizeDelta"));
                                                                    _log.LogInfo("[OI][t1][scroll] ★ Content 撑到 (642,178)：写入=" + okC + " 原=" + Convert.ToString(sdC2) + " 复读=" + sdC3 + "（★ 只改滚动容器 ✓ 不碰原项 ✓）");
                                                                }
                                                            }
                                                            if (sc != null)
                                                            {
                                                                bool okS = Reflect.SetPropOrField(sc, "verticalNormalizedPosition", 0f);
                                                                string vn2 = Convert.ToString(Reflect.Prop(sc, "verticalNormalizedPosition"));
                                                                _log.LogInfo("[OI][t1][scroll] ★ 已把 ScrollRect 滚到底 vNorm=0：写入=" + okS + " 复读=" + vn2 + "（★ 看它是否出现 ✓）");
                                                            }
                                                            else _log.LogWarning("[OI][t1][scroll] 未找到 ScrollRect（那是另一类原因 ✓）");
                                                        }
                                                        catch (Exception eSc) { _log.LogWarning("[OI][t1][scroll] 异常：" + eSc.GetType().Name + " — " + Short(eSc.Message)); }
                                                        try
                                                        {
                                                            var rtT6 = cont == null ? null : cont.GetType();
                                                            object goL = Reflect.Prop(lastUi, "gameObject");
                                                            object rt6 = null;
                                                            var gcA = goL == null ? null : goL.GetType().GetMethod("GetComponent", new[] { typeof(Type) });
                                                            if (gcA != null && rtT6 != null) rt6 = gcA.Invoke(goL, new object[] { rtT6 });
                                                            object sd6 = rt6 == null ? null : Reflect.Prop(rt6, "sizeDelta");
                                                            object ap6 = rt6 == null ? null : Reflect.Prop(rt6, "anchoredPosition");
                                                            string sRt6 = rt6 == null ? "取不到 ✗" : "有 ✓";
                                                            string sSd6 = sd6 == null ? "读失败 ✗" : sd6.ToString();
                                                            string sAp6 = ap6 == null ? "读失败 ✗" : ap6.ToString();
                                                            Diag("[OI][t1][size] 新项（★ 用容器同款取法 ✓）：rt=" + sRt6 + " sizeDelta=" + sSd6 + " anchoredPos=" + sAp6);
                                                        
                                                        try
                                                        {
                                                            object walk2 = cont;
                                                            for (int wi2 = 0; wi2 < 6 && walk2 != null; wi2++)
                                                            {
                                                                string wn2 = Convert.ToString(Reflect.Prop(walk2, "name"));
                                                                Type wt2 = walk2.GetType();
                                                                string full2 = wt2 == null ? "null ✗" : wt2.FullName;
                                                                object sdW2 = Reflect.Prop(walk2, "sizeDelta");
                                                                object apW2 = Reflect.Prop(walk2, "anchoredPosition");
                                                                string sdS2 = sdW2 == null ? "读失败 ✗" : sdW2.ToString();
                                                                string apS2 = apW2 == null ? "读失败 ✗" : apW2.ToString();
                                                                Diag("[OI][t1][wt] [" + wi2 + "] name=" + Short(wn2) + " GetType=" + full2 + " sizeDelta=" + sdS2 + " anchoredPos=" + apS2);
                                                                walk2 = Reflect.Prop(walk2, "parent");
                                                            }
                                                        }
                                                        catch (Exception eWt) { _log.LogWarning("[OI][t1][wt] 异常：" + eWt.GetType().Name); }
                                                        
                                                        
                                                        try
                                                        {
                                                            object trN9 = Reflect.Prop(lastUi, "transform");
                                                            object u0p = IndexN(listObj, 0);
                                                            object tr0p = u0p == null ? null : Reflect.Prop(u0p, "transform");
                                                            object u1p = IndexN(listObj, 1);
                                                            object tr1p = u1p == null ? null : Reflect.Prop(u1p, "transform");
                                                            object pos0 = tr0p == null ? null : Reflect.Prop(tr0p, "localPosition");
                                                            object pos1 = tr1p == null ? null : Reflect.Prop(tr1p, "localPosition");
                                                            object posNow = trN9 == null ? null : Reflect.Prop(trN9, "localPosition");
                                                            string s0 = pos0 == null ? "读失败 ✗" : pos0.ToString();
                                                            string s1 = pos1 == null ? "读失败 ✗" : pos1.ToString();
                                                            string sN = posNow == null ? "读失败 ✗" : posNow.ToString();
                                                            _log.LogInfo("[OI][t1][pos] 改写前：第0项=" + s0 + " ｜ 第1项=" + s1 + " ｜ 我们那一项=" + sN);
                                                            if (pos0 != null && trN9 != null)
                                                            {
                                                                bool okP = Reflect.SetPropOrField(trN9, "localPosition", pos0);
                                                                object posAfter = Reflect.Prop(trN9, "localPosition");
                                                                string sA = posAfter == null ? "读失败 ✗" : posAfter.ToString();
                                                                _log.LogInfo("[OI][t1][pos] ★ 已把我们的项摆到第 0 项位置：写入=" + okP + " 复读=" + sA + "（★ 只改我们那一项 ✓ · 会与第 0 项重叠但先求可见 ✓）");
                                                            }
                                                            else _log.LogWarning("[OI][t1][pos] 缺前提：pos0=" + (pos0 == null) + " tr=" + (trN9 == null));
                                                        }
                                                        catch (Exception ePos) { _log.LogWarning("[OI][t1][pos] 异常：" + ePos.GetType().Name + " — " + Short(ePos.Message)); }
                                                        
                                                        try
                                                        {
                                                            var rtReal2 = cont == null ? null : cont.GetType();   
                                                            string sRtT = rtReal2 == null ? "null ✗" : rtReal2.FullName;
                                                            _log.LogInfo("[OI][t1][rt] 真 RectTransform 类型（取自容器 ✓）= " + sRtT);
                                                            object RtOf(object obj)
                                                            {
                                                                if (obj == null || rtReal2 == null) return null;
                                                                var gcv = obj.GetType().GetMethod("GetComponent", new[] { typeof(Type) });
                                                                if (gcv == null) return null;
                                                                try { return gcv.Invoke(obj, new object[] { rtReal2 }); } catch { return null; }
                                                            }
                                                            object trOur = Reflect.Prop(lastUi, "transform");
                                                            object goOur = Reflect.Prop(lastUi, "gameObject");
                                                            object rtOur = RtOf(goOur) ?? RtOf(trOur);
                                                            object rt0b = RtOf(Reflect.Prop(IndexN(listObj, 0), "gameObject"));
                                                            object rt1b = RtOf(Reflect.Prop(IndexN(listObj, 1), "gameObject"));
                                                            string SdOf(object rt) { if (rt == null) return "rt=null ✗"; var v = Reflect.Prop(rt, "sizeDelta"); return v == null ? "读失败 ✗" : v.ToString(); }
                                                            _log.LogInfo("[OI][t1][rt] 新项 rt=" + (rtOur == null ? "取不到 ✗" : "有 ✓") + " sizeDelta=" + SdOf(rtOur));
                                                            _log.LogInfo("[OI][t1][rt] 第0项 sizeDelta=" + SdOf(rt0b) + " ｜ 第1项 sizeDelta=" + SdOf(rt1b));
                                                            
                                                            if (rtOur != null && rt0b != null)
                                                            {
                                                                object sd0v = Reflect.Prop(rt0b, "sizeDelta");
                                                                object sdNv = Reflect.Prop(rtOur, "sizeDelta");
                                                                string sdNs = sdNv == null ? "读失败 ✗" : sdNv.ToString();
                                                                bool okSd = Reflect.SetPropOrField(rtOur, "sizeDelta", sd0v);
                                                                string sdAfter = Convert.ToString(Reflect.Prop(rtOur, "sizeDelta"));
                                                                _log.LogInfo("[OI][t1][rt] ★ 已把新项 sizeDelta 设为第 0 项的值：写入=" + okSd + " 原=" + sdNs + " 复读=" + sdAfter + "（★ 只改新项 ✓）");
                                                            }
                                                            
                                                            if (goOur != null)
                                                            {
                                                                var gcs2 = goOur.GetType().GetMethod("GetComponents", new[] { typeof(Type) });
                                                                var objT2 = ResolveType("UnityEngine.Object") ?? ResolveTypeSimple("Object");
                                                                if (gcs2 != null && objT2 != null)
                                                                {
                                                                    var all2 = gcs2.Invoke(goOur, new object[] { objT2 });
                                                                    int n2 = 0; object len2 = all2 == null ? null : Reflect.Prop(all2, "Length");
                                                                    if (len2 != null) n2 = Convert.ToInt32(len2);
                                                                    string lst = ""; string imgA = "无 Image";
                                                                    for (int i2 = 0; i2 < n2 && i2 < 14; i2++)
                                                                    {
                                                                        object cc2 = IndexN(all2, i2);
                                                                        if (cc2 == null) continue;
                                                                        string cn2 = cc2.GetType().Name;
                                                                        if (cn2.Length > 36) cn2 = cn2.Substring(0, 36);
                                                                        lst = lst + cn2 + " ";
                                                                        if (cn2.Contains("Image"))
                                                                        {
                                                                            object col = Reflect.Prop(cc2, "color");
                                                                            object a2 = col == null ? null : Reflect.Prop(col, "a");
                                                                            imgA = "Image.color.a=" + (a2 == null ? "读失败 ✗" : a2.ToString());
                                                                        }
                                                                    }
                                                                    _log.LogInfo("[OI][t1][comp] 新项组件=[" + lst.Trim() + "] ｜ " + imgA + "（★ 无 Image 或 alpha=0 ⇒ 也看不见 ✓）");
                                                                }
                                                            }
                                                            
                                                            try
                                                            {
                                                                var leT = ResolveType("UnityEngine.UI.LayoutElement") ?? ResolveTypeSimple("LayoutElement");
                                                                object le = RtOf(goOur) == null ? null : null;
                                                                if (leT != null && goOur != null)
                                                                {
                                                                    var gcl = goOur.GetType().GetMethod("GetComponent", new[] { typeof(Type) });
                                                                    if (gcl != null) le = gcl.Invoke(goOur, new object[] { leT });
                                                                    if (le == null)
                                                                    {
                                                                        var addl = goOur.GetType().GetMethod("AddComponent", new[] { typeof(Type) });
                                                                        if (addl != null) { le = addl.Invoke(goOur, new object[] { leT }); _log.LogInfo("[OI][t1][lay4] 新项没有 LayoutElement ⇒ 已 AddComponent ✓（★ 只加在新项上 ✓）"); }
                                                                    }
                                                                    else _log.LogInfo("[OI][t1][lay4] 新项已有 LayoutElement ✓");
                                                                    if (le != null)
                                                                    {
                                                                        bool okIg = Reflect.SetPropOrField(le, "ignoreLayout", true);
                                                                        _log.LogInfo("[OI][t1][lay4] ★ 已设 ignoreLayout=true：写入=" + okIg + " 复读=" + Convert.ToString(Reflect.Prop(le, "ignoreLayout")) + "（★ 防布局组覆盖我们的位置 ✓）");
                                                                    }
                                                                }
                                                                else _log.LogWarning("[OI][t1][lay4] LayoutElement 类型未解析到（le=" + (leT != null) + "）");
                                                            }
                                                            catch (Exception eLe) { _log.LogWarning("[OI][t1][lay4] 异常：" + eLe.GetType().Name); }
                                                        }
                                                        catch (Exception eRt) { _log.LogWarning("[OI][t1][rt] 异常：" + eRt.GetType().Name + " — " + Short(eRt.Message)); }
                                                        }
                                                        catch (Exception eSz6) { _log.LogWarning("[OI][t1][size] 异常：" + eSz6.GetType().Name); }
                                                            _log.LogInfo("[OI][t1][clip] 祖先链（★ 有 Mask 的层会裁掉框外子项 ✓）：" + chain2);
                                                        }
                                                        catch (Exception eC8) { _log.LogWarning("[OI][t1][clip] 祖先链异常 ✗：" + eC8.GetType().Name); }
                                                        try
                                                        {
                                                            object trL3 = Reflect.Prop(lastUi, "transform");
                                                            object sdTr3 = trL3 == null ? null : Reflect.Prop(trL3, "sizeDelta");
                                                            object u0s = IndexN(listObj, 0);
                                                            object tr0s = u0s == null ? null : Reflect.Prop(u0s, "transform");
                                                            object sd0s = tr0s == null ? null : Reflect.Prop(tr0s, "sizeDelta");
                                                            _log.LogInfo("[OI][t1][clip] 新项 transform sizeDelta=" + (sdTr3 == null ? "读失败 ✗" : sdTr3.ToString()) + " ｜ 第0项=" + (sd0s == null ? "读失败 ✗" : sd0s.ToString()) + "（★ 新高=0 ⇒ 也是看不见的原因 ✓）");
                                                        }
                                                        catch (Exception eSz2) { _log.LogWarning("[OI][t1][clip] 读尺寸异常 ✗：" + eSz2.GetType().Name); }
                                                        try
                                                        {
                                                            var vType = ResolveType("UnityEngine.Vector2") ?? ResolveTypeSimple("Vector2");
                                                            if (vType != null && cont != null)
                                                            {
                                                                object vNew = Activator.CreateInstance(vType, new object[] { 642f, 178f });
                                                                bool okBig = Reflect.SetPropOrField(cont, "sizeDelta", vNew);
                                                                object sdAf = Reflect.Prop(cont, "sizeDelta");
                                                                _log.LogInfo("[OI][t1][clip] ★ 已把容器撑高到 (642,178)（★ 只改容器 ✓）：写入=" + okBig + " 复读=" + (sdAf == null ? "读失败 ✗" : sdAf.ToString()) + " ⇒ 看它是否出现 ✓");
                                                            }
                                                            else _log.LogWarning("[OI][t1][clip] 撑高缺 API ✗：Vector2 类型=" + (vType == null ? "null ✗" : "有 ✓"));
                                                        }
                                                        catch (Exception eBig2) { _log.LogWarning("[OI][t1][clip] 撑高异常 ✗：" + eBig2.GetType().Name + " — " + Short(eBig2.Message)); }
                                                    }
                                                    else _log.LogWarning("[OI][t1][lay3] 仍拿不到 RectTransform 类型 ✗ ⇒ 无法重绘 ✓（如实报 ✓）");
                                                    bool off = Reflect.SetPropOrField(lgNew, "enabled", false);
                                                    bool on = Reflect.SetPropOrField(lgNew, "enabled", true);
                                                    _log.LogInfo("[OI][t1][lay2] ①a 已 enabled=false→true：F=" + off + " T=" + on + " 复读=" + Reflect.Prop(lgNew, "enabled") + "（★ 看屏幕是否摆开 ✓）");
                                                    var lrT2 = ResolveType("UnityEngine.UI.LayoutRebuilder");
                                                    var miRb2 = lrT2 == null ? null : lrT2.GetMethod("ForceRebuildLayoutImmediate", BindingFlags.Public | BindingFlags.Static);
                                                    var mkRb = lrT2 == null ? null : lrT2.GetMethod("MarkLayoutForRebuild", BindingFlags.Public | BindingFlags.Static);
                                                    object lgRect = null;
                                                    var rtT5 = ResolveType("UnityEngine.RectTransform");
                                                    var gc5 = lgGo == null ? null : lgGo.GetType().GetMethod("GetComponent", new[] { typeof(Type) });
                                                    if (gc5 != null && rtT5 != null) lgRect = gc5.Invoke(lgGo, new object[] { rtT5 });
                                                    _log.LogInfo("[OI][t1][lay2] 布局组宿主 RectTransform=" + (lgRect == null ? "取不到 ✗" : "有 ✓"));
                                                    if (miRb2 != null && lgRect != null) { miRb2.Invoke(null, new object[] { lgRect }); _log.LogInfo("[OI][t1][lay2] ①b 已 ForceRebuildLayoutImmediate(布局组宿主) ✓"); }
                                                    else _log.LogWarning("[OI][t1][lay2] ①b 缺 API ✗：（rb=" + (miRb2 != null) + " rect=" + (lgRect != null) + "）");
                                                    if (mkRb != null && lgRect != null) { mkRb.Invoke(null, new object[] { lgRect }); _log.LogInfo("[OI][t1][lay2] ①c 已 MarkLayoutForRebuild(布局组宿主) ✓"); }
                                                    else _log.LogWarning("[OI][t1][lay2] ①c 缺 API ✗：（mk=" + (mkRb != null) + "）");
                                                }
                                                catch (Exception eL2) { _log.LogWarning("[OI][t1][lay2] 布局组操作异常 ✗：" + eL2.GetType().Name + " — " + Short(eL2.Message)); }
                                                try
                                                {
                                                    object trS = Reflect.Prop(lastUi, "transform");
                                                    var setS3 = trS == null ? null : trS.GetType().GetMethod("SetSiblingIndex", new[] { typeof(int) });
                                                    if (setS3 != null) { setS3.Invoke(trS, new object[] { 3 }); var gotS = trS.GetType().GetMethod("GetSiblingIndex", Type.EmptyTypes); object sNow = gotS == null ? null : gotS.Invoke(trS, null); _log.LogInfo("[OI][t1][lay2] ④ SetSiblingIndex(3) ✓ 回读 sibling=" + (sNow == null ? "读不到 ✗" : sNow.ToString()) + "（★ 回读=3 才说明生效 ✓）"); }
                                                    else _log.LogWarning("[OI][t1][lay2] ④ 缺 API ✗：SetSiblingIndex");
                                                }
                                                catch (Exception eS4) { _log.LogWarning("[OI][t1][lay2] ④ 异常 ✗：" + eS4.GetType().Name + " — " + Short(eS4.Message)); }
                                                if (lgNew == null && lg0 != null)
                                                {
                                                    bool okCp = Reflect.SetPropOrField(lastUi, "verticalLayoutGroup", lg0);
                                                    _log.LogInfo("[OI][t1][lay] 新项无布局组 ⇒ 从第 0 项复制 ✓ 写入=" + okCp + "（★ 只改新项 ✓）");
                                                }
                                                
                                                try
                                                {
                                                    var miRef = lastUi.GetType().GetMethod("RefreshButton", Type.EmptyTypes);
                                                    if (miRef != null) { miRef.Invoke(lastUi, null); _log.LogInfo("[OI][t1][lay] ① 已调 RefreshButton() ✓（★ 看屏幕是否摆开 ✓）"); }
                                                    else _log.LogWarning("[OI][t1][lay] ① 缺 API ✗：RefreshButton 未找到");
                                                }
                                                catch (Exception e1x) { _log.LogWarning("[OI][t1][lay] ① 异常 ✗：" + e1x.GetType().Name); }
                                                
                                                try
                                                {
                                                    object trN4 = Reflect.Prop(lastUi, "transform");
                                                    object rtN4 = null;
                                                    var rtT4 = ResolveType("UnityEngine.RectTransform");
                                                    var gc4 = trN4 == null ? null : trN4.GetType().GetMethod("GetComponent", new[] { typeof(Type) });
                                                    if (gc4 != null && rtT4 != null) rtN4 = gc4.Invoke(trN4, new object[] { rtT4 });
                                                    if (rtN4 == null) rtN4 = trN4;
                                                    var lrT = ResolveType("UnityEngine.UI.LayoutRebuilder");
                                                    var miRb = lrT == null ? null : lrT.GetMethod("ForceRebuildLayoutImmediate", BindingFlags.Public | BindingFlags.Static);
                                                    if (miRb != null && rtN4 != null) { miRb.Invoke(null, new object[] { rtN4 }); _log.LogInfo("[OI][t1][lay] ③ 已对新项自己的 RectTransform 强制重绘 ✓（★ 上版是对容器 ✗）"); }
                                                    else _log.LogWarning("[OI][t1][lay] ③ 缺 API ✗：ForceRebuildLayoutImmediate / RectTransform（rt=" + (rtN4 != null) + "）");
                                                }
                                                catch (Exception e3x) { _log.LogWarning("[OI][t1][lay] ③ 异常全量 ✗：" + e3x.GetType().FullName + " — " + Short(e3x.Message) + " ｜ 前几行：" + Short(e3x.StackTrace)); }
                                                _log.LogInfo("[OI][t1][reg] ★ 已登记 hash=" + lastUi.GetHashCode() + " 名字=" + nmNow + " 克隆体=" + isClone
                                                    + " ｜名单数量=" + _t1Ours.Count + "（★ 判据：登记 hash 取自【最终在列表里的对象】✓）");
                                                try
                                                {
                                                    object trL = Reflect.Prop(lastUi, "transform");
                                                    var setSib = trL == null ? null : trL.GetType().GetMethod("SetSiblingIndex", new[] { typeof(int) });
                                                    if (setSib != null) { setSib.Invoke(trL, new object[] { 1 }); _log.LogInfo("[OI][t1][reg] 已 SetSiblingIndex(1) ✓（★ 若它出现在第 1 位 ⇒ 排布由 sibling 驱动 ✓）"); }
                                                    else _log.LogWarning("[OI][t1][reg] 缺 API ✗：SetSiblingIndex 未找到");
                                                }
                                                catch (Exception eS) { _log.LogWarning("[OI][t1][reg] 设 sibling 异常 ✗：" + eS.GetType().Name); }
                                                try
                                                {
                                                    var sbM = new System.Text.StringBuilder();
                                                    foreach (var mm in lastUi.GetType().GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
                                                    {
                                                        string mn = mm.Name;
                                                        bool cand = System.Text.RegularExpressions.Regex.IsMatch(mn, "Show|Refresh|Layout|Setup|Update|Rebuild|Arrange");
                                                        if (cand && sbM.Length < 300) sbM.Append(mn).Append("(").Append(mm.GetParameters().Length).Append(") ");
                                                    }
                                                    _log.LogInfo("[OI][t1][reg] 组件上的摆位/刷新候选：" + (sbM.Length == 0 ? "无 ✗" : sbM.ToString()));
                                                }
                                                catch (Exception eC) { _log.LogWarning("[OI][t1][reg] 列候选异常 ✗：" + eC.GetType().Name); }
                                            }
                                            else _log.LogWarning("[OI][t1][reg] 回读失败 ✗：列表取不到最后一项（lnNow=" + lnNow + "）");
                                        }
                                        catch (Exception eRg) { _log.LogWarning("[OI][t1][reg] 登记异常 ✗：" + eRg.GetType().Name); }
                                        if (cont != null)
                                        {
                                            object cntChild2 = Reflect.Prop(cont, "childCount");
                                            int nc = cntChild2 == null ? -1 : Convert.ToInt32(cntChild2);
                                            Diag("[OI][t1][kids] 容器 childCount=" + nc + " 逐个列出 ✓：");
                                            var getCh = cont.GetType().GetMethod("GetChild", new[] { typeof(int) });
                                            var sibM = cont.GetType().GetMethod("GetSiblingIndex", Type.EmptyTypes);
                                            for (int ci = 0; ci < (nc < 0 ? 0 : nc); ci++)
                                            {
                                                object ch = null;
                                                if (getCh != null) ch = getCh.Invoke(cont, new object[] { ci });
                                                if (ch == null) { Diag("[OI][t1][kids]   [" + ci + "] 取不到 ✗"); continue; }
                                                object chGo = Reflect.Prop(ch, "gameObject");
                                                object lpos = Reflect.Prop(ch, "localPosition");
                                                object asf2 = chGo == null ? null : Reflect.Prop(chGo, "activeSelf");
                                                object sibV = null;
                                                if (sibM != null) sibV = sibM.Invoke(ch, null);
                                                object rect2 = null;
                                                var rtT = ResolveType("UnityEngine.RectTransform");
                                                var gc2 = chGo == null ? null : chGo.GetType().GetMethod("GetComponent", new[] { typeof(Type) });
                                                if (gc2 != null && rtT != null) rect2 = gc2.Invoke(chGo, new object[] { rtT });
                                                object sdV = rect2 == null ? null : Reflect.Prop(rect2, "sizeDelta");
                                                object apV = rect2 == null ? null : Reflect.Prop(rect2, "anchoredPosition");
                                                Diag("[OI][t1][kids]   [" + ci + "] name=" + Short(Convert.ToString(Reflect.Prop(chGo, "name")))
                                                    + " sibling=" + (sibV == null ? "读失败 ✗" : sibV.ToString())
                                                    + " localPos=" + (lpos == null ? "读失败 ✗" : lpos.ToString())
                                                    + " active=" + (asf2 == null ? "读失败 ✗" : asf2.ToString())
                                                    + " rect=" + (rect2 == null ? "取不到 ✗" : "有 ✓")
                                                    + " sizeDelta=" + (sdV == null ? "读失败 ✗" : sdV.ToString())
                                                    + " anchoredPos=" + (apV == null ? "读失败 ✗" : apV.ToString()));
                                            }
                                        }
                                        object rtNew3 = null;
                                        var rtT3 = ResolveType("UnityEngine.RectTransform");
                                        var gc3 = goNew2 == null ? null : goNew2.GetType().GetMethod("GetComponent", new[] { typeof(Type) });
                                        if (gc3 != null && rtT3 != null) rtNew3 = gc3.Invoke(goNew2, new object[] { rtT3 });
                                        object sdN3 = rtNew3 == null ? null : Reflect.Prop(rtNew3, "sizeDelta");
                                        object apN3 = rtNew3 == null ? null : Reflect.Prop(rtNew3, "anchoredPosition");
                                        Diag("[OI][t1][geom2] 新项（★ 改在 RectTransform 上读 ✓）：rect=" + (rtNew3 == null ? "取不到 ✗" : "有 ✓")
                                            + " sizeDelta=" + (sdN3 == null ? "读失败 ✗" : sdN3.ToString())
                                            + " anchoredPos=" + (apN3 == null ? "读失败 ✗" : apN3.ToString()) + "（★ 读到值 ⇒ 上版读失败只是读在了 Transform 上 ✗）");
                                    }
                                    catch (Exception eId) { _log.LogWarning("[OI][t1][id] 诊断异常 ✗：" + eId.GetType().Name); }
                                        try
                                        {
                                            var lrType = ResolveType("UnityEngine.UI.LayoutRebuilder");
                                            var miReb = lrType == null ? null : lrType.GetMethod("ForceRebuildLayoutImmediate", BindingFlags.Public | BindingFlags.Static);
                                            if (miReb != null && cont != null) { miReb.Invoke(null, new object[] { cont }); Diag("[OI][t1][geom] 已强制重绘容器 ✓ child=" + ChildCount(cont)); }
                                            else _log.LogWarning("[OI][t1][geom] 缺 API ✗：ForceRebuildLayoutImmediate（容器=" + (cont != null) + "）");
                                        }
                                        catch (Exception eR3) { _log.LogWarning("[OI][t1][geom] 重绘异常 ✗：" + eR3.GetType().Name); }
                                        try
                                        {
                                            object rasNow = Reflect.Prop(__instance, "responsesAreShown");
                                            bool okA = Reflect.SetPropOrField(__instance, "responsesAreShown", false);
                                            bool okB = Reflect.SetPropOrField(__instance, "responsesAreShown", true);
                                            Diag("[OI][t1][geom] responsesAreShown 置位切换：原=" + rasNow + " F=" + okA + " T=" + okB + "（★ 只动状态 ✓）");
                                        }
                                        catch (Exception eS3) { _log.LogWarning("[OI][t1][geom] 置位异常 ✗：" + eS3.GetType().Name); }
                                    }
                                    catch (Exception eG2) { _log.LogWarning("[OI][t1][geom] 几何诊断异常 ✗：" + eG2.GetType().Name); }
                                Diag("[OI][t1][vis] 新项下标=" + (ln - 1) + " 取到=" + (nu == null ? "null ✗" : nu.GetType().Name + " ✓"));
                                if (nu != null)
                                {
                                    object goN = Reflect.Prop(nu, "gameObject");   
                                    object asf0 = Reflect.Prop(nu, "activeSelf");
                                    object aih0 = Reflect.Prop(nu, "activeInHierarchy");
                                    object asf = goN == null ? null : Reflect.Prop(goN, "activeSelf");
                                    object aih = goN == null ? null : Reflect.Prop(goN, "activeInHierarchy");
                                    Diag("[OI][t1][vis] activeSelf：组件读=" + (asf0 == null ? "读失败 ✗" : asf0.ToString()) + " ｜ gameObject 读=" + (asf == null ? "读失败 ✗" : asf.ToString()));
                                    Diag("[OI][t1][vis] activeInHierarchy：组件读=" + (aih0 == null ? "读失败 ✗" : aih0.ToString()) + " ｜ gameObject 读=" + (aih == null ? "读失败 ✗" : aih.ToString()));
                                    if (goN != null && string.Equals(Convert.ToString(asf), "False", StringComparison.OrdinalIgnoreCase))
                                    {
                                        var sa2 = goN.GetType().GetMethod("SetActive", new[] { typeof(bool) });
                                        if (sa2 != null) { sa2.Invoke(goN, new object[] { true }); Diag("[OI][t1][vis] 原为 inactive ⇒ 已 SetActive(true) ✓ 复读 activeSelf=" + Reflect.Prop(nu, "activeSelf")); }
                                        else _log.LogWarning("[OI][t1][vis] 缺 API ✗：gameObject.SetActive 未找到");
                                    }
                                    object trN = Reflect.Prop(nu, "transform");
                                    object par = trN == null ? null : Reflect.Prop(trN, "parent");
                                    object sib = trN == null ? null : Reflect.Prop(trN, "GetSiblingIndex");
                                    object rt2 = trN == null ? null : Reflect.Prop(trN, "sizeDelta");
                                    string sRt = rt2 == null ? "sizeDelta=读失败 ✗（试过 Reflect.Prop ✓；struct 可能需 Il2CppStructArray ✓）" : ("sizeDelta=" + rt2.ToString() + " ✓");
                                    var apv = trN == null ? null : Reflect.Prop(trN, "anchoredPosition");
                                    string sAp = apv == null ? "anchoredPosition=读失败 ✗（同上 ✓）" : ("anchoredPosition=" + apv.ToString() + " ✓");
                                    var lsv = trN == null ? null : Reflect.Prop(trN, "localScale");
                                    string sLs = lsv == null ? "localScale=读失败 ✗（同上 ✓）" : ("localScale=" + lsv.ToString() + " ✓");
                                    Diag("[OI][t1][vis] parent=" + (par == null ? "读失败 ✗" : par.GetType().Name + " ✓") + " sibling=" + (sib == null ? "读失败 ✗" : sib.ToString()) + " ｜ " + sRt + " ｜ " + sAp + " ｜ " + sLs);
                                    if (cont != null) Diag("[OI][t1][vis] 容器子节点数=" + ChildCount(cont) + "（★ 容器 vs 列表 都要看 ✓）");
                                    
                                    try
                                    {
                                        object ui0b = IndexN(listObj, 0);
                                        object tr0 = ui0b == null ? null : Reflect.Prop(ui0b, "transform");
                                        if (tr0 != null && trN != null)
                                        {
                                            var sd = Reflect.Prop(tr0, "sizeDelta");
                                            if (sd != null && !string.Equals(Convert.ToString(rt2), Convert.ToString(sd), StringComparison.Ordinal))
                                            {
                                                bool okSd = Reflect.SetPropOrField(trN, "sizeDelta", sd);
                                                Diag("[OI][t1][vis] 尺寸与第 0 项不一致 ⇒ 复制已有项 sizeDelta=" + sd + " 写入=" + okSd + "（只改新项 ✓）");
                                            }
                                            else Diag("[OI][t1][vis] 尺寸与第 0 项一致或无退化 ✓（无需复制）");
                                        }
                                    }
                                    catch (Exception eL) { _log.LogWarning("[OI][t1][vis] 布局复制异常 ✗：" + eL.GetType().Name); }
                                    
                                    try
                                    {
                                        object ras = Reflect.Prop(__instance, "responsesAreShown");
                                        Diag("[OI][t1][vis] responsesAreShown=" + ras + "（★ 若为 False ⇒ 菜单可能没重绘 ✓ 候选：置位切换 ✓；本版**不调用 OnContinue()** ✗ 以免推进对话 ✓）");
                                    }
                                    catch { }
                                }
                            }
                            catch (Exception ev3) { _log.LogWarning("[OI][t1][vis] 诊断异常 ✗：" + ev3.GetType().Name); }
                            _log.LogInfo("[OI][t1][v2-1] CreateResponse 已调用 ✓（由游戏自己上界面 ✓）");
                        }
                    }
                }
                catch (Exception ev1) { _log.LogWarning("[OI][t1][v2-1] 抛异常 ✗：" + ev1.GetType().Name + " — " + Short(ev1.Message)); }
                int v2After = ChildCount(cont);
                _log.LogInfo("[OI][t1][v2] ★ 硬判据：child " + v2Before + " ⇒ " + ChildCount(cont) + " ｜ listCnt " + listBefore + " ⇒ " + ListCount(listObj) + "（★ 两项都要动才算数 ✓；只动一项 ⇒ 记下是哪一项 ✓）");
                return;
                _log.LogInfo("[OI][t1][step1] 实例化：" + how1);
                if (inst == null) return;

                
                string how2 = "跳过（responseContainer 缺失 ✗）";
                try
                {
                    if (cont != null)
                    {
                        var tr = Reflect.Prop(inst, "transform");
                        var trt = tr.GetType();
                        var tried = new System.Text.StringBuilder();
                        foreach (var cand in trt.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
                        {
                            if (cand.Name != "SetParent") continue;
                            var ps = cand.GetParameters();
                            tried.Append(ps.Length).Append("参(");
                            for (int pi2 = 0; pi2 < ps.Length; pi2++) { if (pi2 > 0) tried.Append(","); tried.Append(ps[pi2].ParameterType.Name); }
                            tried.Append(") ");
                            try
                            {
                                if (ps.Length == 1 && ps[0].ParameterType.IsInstanceOfType(cont)) { cand.Invoke(tr, new object[] { cont }); how2 = "✓ SetParent(1 参)"; break; }
                                if (ps.Length == 2 && ps[0].ParameterType.IsInstanceOfType(cont) && ps[1].ParameterType == typeof(bool)) { cand.Invoke(tr, new object[] { cont, false }); how2 = "✓ SetParent(2 参)"; break; }
                            }
                            catch (Exception e2b) { tried.Append("[抛" + e2b.GetType().Name + "] "); }
                        }
                        if (how2.StartsWith("跳过")) how2 = "缺 API ✗（SetParent 试过的签名：" + tried.ToString() + "）";
                    }
                }
                catch (Exception e2) { how2 = "抛异常 ✗ " + e2.GetType().Name; }
                _log.LogInfo("[OI][t1][nulls] cont=" + (cont == null) + " tpl=" + (tpl == null) + " inst=" + (inst == null) + " listObj=" + (listObj == null));
                _log.LogInfo("[OI][t1][step2] 挂容器：" + how2);

                
                string orig0 = Short(ItemText(__0, 0));   
                _log.LogInfo("[OI][t1][keep] 写入前 orig0=" + orig0);
                object destEntry = null; try { destEntry = Reflect.Prop(first, "destinationEntry"); } catch { }
                string want = CfgT1("OptionText", "同时为两个项目拨款");
                
                object ftNew = ft;   
                string label = "原文案";
                string why1 = "ft 为 null ✗", why2 = "", why3 = "";
                if (ft != null)
                {
                    var ftType = ft.GetType();
                    try { var a = Activator.CreateInstance(ftType, new object[] { want }); if (a != null) { ftNew = a; label = "自定义 ✓[S1 ctor(string)]"; why1 = "S1 成功 ✓"; } else why1 = "S1 返回 null"; }
                    catch (Exception ex1) { why1 = "S1 抛 " + ex1.GetType().Name; }
                    why2 = "S2/S3（就地改 text 属性/字段）**已按纪律移除** ✗（它们会改写原项 ✓）";
                    try
                    {
                        var cl = ftType.GetMethod("Clone", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                        if (cl == null || cl.GetParameters().Length != 0) why3 = "S1b：无 Clone() ✗";
                        else
                        {
                            var c2 = cl.Invoke(ft, null);
                            var pi2 = c2 == null ? null : c2.GetType().GetProperty("text", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                            if (pi2 != null && pi2.CanWrite) { pi2.SetValue(c2, want, null); ftNew = c2; label = "自定义 ✓[S1b Clone+改副本]"; why3 = "S1b 成功 ✓"; }
                            else why3 = "S1b：Clone 成功但副本无可写 text ✗";
                        }
                    }
                    catch (Exception ex3) { why3 = "S1b 抛 " + ex3.GetType().Name; }
                }
                _log.LogInfo("[OI][t1][text] 文案策略=" + label + "｜" + why1 + "｜" + why2 + "｜" + why3 + "（★ 原项对象未被修改 ✓）");
                object clone = null;
                try { clone = Activator.CreateInstance(first.GetType(), new object[] { ftNew, destEntry, true }); }
                catch (Exception e3) { _log.LogWarning("[OI][t1] 克隆失败 ✗：" + e3.GetType().Name + " ⇒ 不追加"); return; }

                string cbHow; object cb = BuildUnityActionFor(
                    typeof(EconomyPatches).GetMethod("T1BothClick", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public), out cbHow);
                int cntList = ListCount(listObj);
                if (cntList < 0) cntList = ListCount(list);
                int cntChild = ChildCount(cont);
                int newIndex = cntChild;   
                if (newIndex < 0) newIndex = cntList;
                if (newIndex < 0) newIndex = cntChild;
                _log.LogInfo("[OI][t1][probe3] listCnt=" + cntList + " childCnt=" + cntChild + " newIndex=" + newIndex);
                if (newIndex < 0)
                {
                    _log.LogWarning("[OI][t1] 缺 API: listCnt 与 childCnt 都读不到 => 本次不追加（不顶掉原项）");
                    return;
                }
                _log.LogInfo("[OI][t1][keep] orig0=" + orig0);
                try
                {
                
                MethodInfo setup = null; object setupTarget = null;   
                var bf2 = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
                setup = inst.GetType().GetMethod("Setup", bf2);
                if (setup != null) setupTarget = inst;
                if (setup == null) { setup = tpl.GetType().GetMethod("Setup", bf2); if (setup != null) setupTarget = tpl; }
                if (setup != null) _log.LogInfo("[OI][t1][setup] 属主=" + setup.DeclaringType.Name + " 调用对象=" + (setupTarget == null ? "?" : setupTarget.GetType().Name) + " 参数=" + setup.GetParameters().Length);
                if (setup == null)
                {
                    var cands = new System.Text.StringBuilder();
                    foreach (var mm in tpl.GetType().GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
                    { if (mm.Name != "Setup") continue; cands.Append(mm.GetParameters().Length).Append("参; "); }
                    _log.LogWarning("[OI][t1] 缺 API ✗：Setup 未找到（候选：" + cands.ToString() + "）");
                    return;
                }
                    if (setup == null || setup.GetParameters().Length != 3) { _log.LogWarning("[OI][t1] 缺 API ✗：Setup(3 参数) 未找到"); return; }
                    setup.Invoke(setupTarget, new object[] { clone, newIndex, cb });
                    _log.LogInfo("[OI][t1][step3] Setup(克隆, index=" + newIndex + "（末端 ✓）, " + cbHow + ") 未抛异常 ✓ 文案=" + label);
                }
                catch (Exception e4) { _log.LogWarning("[OI][t1][step3] Setup 抛异常 ✗：" + e4.GetType().Name + " — " + Short(e4.Message)); DumpChain(e4, "T1"); return; }

                
                
                
                string how4;
                try
                {
                    if (list != null) { list.Add(inst); how4 = "路B ✓ IList.Add ⇒ Count=" + list.Count; }
                    else
                    {
                        var add = listObj == null ? null : listObj.GetType().GetMethod("Add", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                        if (add != null && add.GetParameters().Length == 1) { add.Invoke(listObj, new object[] { inst }); how4 = "路B ✓ 反射 Add ⇒ Count=" + ListCount(listObj); }
                        else how4 = "路B ✗ 缺 API（既非 IList、也无 Add(1)）⇒ 只靠路A（SetParent）看是否显示 ✓";
                    }
                }
                catch (Exception e5) { how4 = "路B ✗ 抛异常 " + e5.GetType().Name + " ⇒ 只靠路A 看是否显示 ✓"; }
                int childBefore = ChildCount(cont);
                _log.LogInfo("[OI][t1][step4] " + how4 + " child " + childBefore + " -> " + ChildCount(cont));
                try
                {
                    var go = Reflect.Prop(inst, "gameObject");
                    var sa = go == null ? null : go.GetType().GetMethod("SetActive", new[] { typeof(bool) });
                    if (sa != null) { sa.Invoke(go, new object[] { true }); _log.LogInfo("[OI][t1][step5] SetActive(true) ✓"); }
                    else _log.LogWarning("[OI][t1][step5] 缺 API ✗：gameObject.SetActive 未找到（可能不显示 ✗）");
                }
                catch (Exception e6) { _log.LogWarning("[OI][t1][step5] 激活抛异常 ✗：" + e6.GetType().Name); }

                _t1bAppended++;
                _log.LogInfo("[OI][t1][ok] appended=" + _t1bAppended + " orig0=" + orig0 + " listCount=" + ListCount(listObj) + " child=" + ChildCount(cont));
            }
            catch { }   
        }

        
        
        
        
        
        
        private static readonly System.Collections.Generic.Dictionary<string, Type> _typeCache =
            new System.Collections.Generic.Dictionary<string, Type>(StringComparer.Ordinal);
        private static readonly System.Collections.Generic.HashSet<string> _typeLogged =
            new System.Collections.Generic.HashSet<string>(StringComparer.Ordinal);

        private static Type ResolveTypeSimple(string simple)
        {
            try
            {
                if (simple != null && _typeCache.ContainsKey(simple)) return _typeCache[simple];
                int n = 0; string first = "无"; Type firstType = null;
                foreach (var asm2 in AppDomain.CurrentDomain.GetAssemblies())
                {
                    try
                    {
                        foreach (var ty2 in asm2.GetTypes())
                        {
                            if (ty2 == null || ty2.Name != simple) continue;
                            n++; if (n == 1) { first = asm2.GetName().Name + "!" + ty2.FullName; firstType = ty2; }
                        }
                    }
                    catch { }
                }
                if (simple != null && firstType != null) { try { _typeCache[simple] = firstType; } catch { } }
                if (simple != null && _typeLogged.Add(simple))
                    _log.LogDebug("[OI][t1][type] 按简单名找 " + simple + " ⇒ 命中 " + n + " 个，首个=" + first);
                if (n > 0)
                {
                    foreach (var asm3 in AppDomain.CurrentDomain.GetAssemblies())
                    {
                        try { foreach (var ty3 in asm3.GetTypes()) { if (ty3 != null && ty3.Name == simple) return ty3; } } catch { }
                    }
                }
            }
            catch (Exception e) { _log.LogWarning("[OI][t1][type] 简单名解析异常 ✗：" + e.GetType().Name); }
            return null;
        }

        private static object reflectGo(object comp)
        {
            try { return comp == null ? null : Reflect.Prop(comp, "gameObject"); } catch { return null; }
        }

        
        private static string FieldList(object obj)
        {
            try
            {
                if (obj == null) return "null";
                var ty = obj.GetType();
                var sb = new System.Text.StringBuilder();
                foreach (var pi in ty.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
                {
                    if (pi.GetIndexParameters().Length > 0) continue;
                    string v = "?";
                    try { var pv = pi.GetValue(obj, null); v = pv == null ? "null" : Short(pv.ToString()); } catch { v = "readfail"; }
                    if (sb.Length < 240) sb.Append(pi.Name).Append(":").Append(pi.PropertyType.Name).Append("=").Append(v).Append(" ; ");
                }
                foreach (var fi in ty.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
                {
                    if (fi.Name.StartsWith("Native")) continue;
                    string v2 = "?";
                    try { var fv = fi.GetValue(obj); v2 = fv == null ? "null" : Short(fv.ToString()); } catch { v2 = "readfail"; }
                    if (sb.Length < 240) sb.Append(fi.Name).Append(":").Append(fi.FieldType.Name).Append("=").Append(v2).Append(" ; ");
                }
                return sb.ToString();
            }
            catch (Exception e) { return "exception " + e.GetType().Name; }
        }

        private static object IndexN(object arr, int i)
        {
            try
            {
                if (arr == null || i < 0) return null;
                var ty = arr.GetType();
                var mi = ty.GetMethod("get_Item", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance) ?? ty.GetMethod("Get", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (mi != null && mi.GetParameters().Length == 1) return mi.Invoke(arr, new object[] { i });
                var pi = ty.GetProperty("Item", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (pi != null && pi.GetIndexParameters().Length == 1) return pi.GetValue(arr, new object[] { i });
            }
            catch { }
            return null;
        }

        private static int ChildCount(object tr)
        {
            try
            {
                if (tr == null) return -1;
                var v = Reflect.Prop(tr, "childCount");
                return v == null ? -1 : Convert.ToInt32(v);
            }
            catch { return -1; }
        }

        private static string ItemText(object arr, int i)
        {
            try
            {
                if (arr == null || i < 0) return null;
                object e = null;
                var ty = arr.GetType();
                var mi = ty.GetMethod("get_Item", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                      ?? ty.GetMethod("Get", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (mi != null && mi.GetParameters().Length == 1) e = mi.Invoke(arr, new object[] { i });
                else { var pi = ty.GetProperty("Item", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance); if (pi != null && pi.GetIndexParameters().Length == 1) e = pi.GetValue(arr, new object[] { i }); }
                if (e == null) return null;
                var ft = Reflect.Prop(e, "formattedText");
                var t = Reflect.Prop(ft, "text") ?? Reflect.Prop(ft, "Text") ?? ft;
                return t == null ? null : t.ToString();
            }
            catch { return null; }
        }

        private static int ListCount(object o)
        {
            try
            {
                if (o == null) return -1;
                var v = Reflect.Prop(o, "Count") ?? Reflect.Prop(o, "Length");
                return v == null ? -1 : Convert.ToInt32(v);
            }
            catch { return -1; }
        }

        private static Type ResolveType(string full)
        {
            try
            {
                foreach (var a in AppDomain.CurrentDomain.GetAssemblies())
                {
                    try { var t = a.GetType(full); if (t != null) return t; } catch { }
                }
            }
            catch { }
            return null;
        }

        private static object Index0(object arr, int cnt)
        {
            try
            {
                if (cnt <= 0) return null;
                var ty = arr.GetType();
                var mi = ty.GetMethod("get_Item", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                      ?? ty.GetMethod("Get", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (mi != null && mi.GetParameters().Length == 1) return mi.Invoke(arr, new object[] { 0 });
                var pi = ty.GetProperty("Item", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (pi != null && pi.GetIndexParameters().Length == 1) return pi.GetValue(arr, new object[] { 0 });
            }
            catch { }
            return null;
        }

        
        private static object BuildUnityActionFor(MethodInfo target, out string how)
        {
            var steps = new System.Text.StringBuilder();
            try
            {
                if (target == null) { how = "null（回调方法未找到 ✗）"; return null; }
                var ua = ResolveType("UnityEngine.Events.UnityAction");
                if (ua == null) { how = "null（UnityAction 类型未解析到 ✗）"; return null; }
                try { var d1 = Delegate.CreateDelegate(ua, target); if (d1 != null) { how = "真 UnityAction ✓[S1]"; return d1; } } catch (Exception e1) { steps.Append("S1 " + e1.GetType().Name + ";"); }
                try
                {
                    var act = Delegate.CreateDelegate(typeof(Action), target);
                    Type ds = null;
                    foreach (var a in AppDomain.CurrentDomain.GetAssemblies()) { try { ds = a.GetType("Il2CppInterop.Runtime.DelegateSupport"); if (ds != null) break; } catch { } }
                    if (ds != null)
                    {
                        foreach (var mm in ds.GetMethods(BindingFlags.Public | BindingFlags.Static))
                        {
                            if (mm.Name != "ConvertDelegate" || !mm.IsGenericMethodDefinition || mm.GetParameters().Length != 1) continue;
                            try { var d2 = mm.MakeGenericMethod(ua).Invoke(null, new object[] { act }); if (d2 != null) { how = "真 UnityAction ✓[S2 DelegateSupport.ConvertDelegate]"; return d2; } } catch (Exception e2b) { steps.Append("S2 " + e2b.GetType().Name + ";"); }
                            break;
                        }
                    }
                    else steps.Append("S2 无 DelegateSupport;");
                }
                catch (Exception e2) { steps.Append("S2外 " + e2.GetType().Name + ";"); }
                how = "null（策略失败 ✗：" + steps.ToString() + "）";
                return null;
            }
            catch (Exception e) { how = "null（异常 ✗ " + e.GetType().Name + "）"; return null; }
        }

        
        
        
        private static readonly System.Collections.Generic.HashSet<int> _t1Ours = new System.Collections.Generic.HashSet<int>();

        
        
        
        private static void Post_T1SetLocalizableTexts(object __instance)
        {
            
            try
            {
                if (__instance != null)
                {
                    int hh4 = __instance.GetHashCode();
                    bool inList4 = _t1Ours.Contains(hh4);
                    string nm4 = "";
                    try { nm4 = Short(Convert.ToString(Reflect.Prop(__instance, "name"))); } catch { }
                    _log.LogInfo("[OI][t1][text-hook] 被调用 ✓ 实例 hash=" + hh4 + " 名字=" + nm4 + " ｜ 白名单数量=" + _t1Ours.Count + " 命中=" + inList4 + "（★ 不出现 ⇒ 钩子没挂上 ✗；出现但命中=False ⇒ 口径不一致 ✓）");
                }
                else _log.LogInfo("[OI][t1][text-hook] 被调用 ✓ 但 __instance=null ✗");
            }
            catch (Exception eDbg3) { _log.LogWarning("[OI][t1][text-hook] 诊断行异常 ✗：" + eDbg3.GetType().Name); }
            try
            {
                if (EconomyConfig.T1Both == null || !EconomyConfig.T1BothOn) return;
                if (__instance == null) return;
                if (!_t1Ours.Contains(__instance.GetHashCode())) return;   
                string want2 = CfgT1("OptionText", "同时为两个项目拨款");
                object txt = Reflect.Prop(__instance, "text");
                bool ok = txt != null && Reflect.SetPropOrField(txt, "text", want2);
                _log.LogInfo("[OI][t1][text-hook] ★ SetLocalizableTexts 命中我们的项 ✓ 写入=" + ok
                    + " 文本=" + want2 + "（★ 这是「玩家能看见」的那一步 ✓）");
            }
            catch (Exception e) { _log.LogWarning("[OI][t1][text-hook] 异常（已吞）✗：" + e.GetType().Name); }
        }

        
        
        
        private static bool Prefix_T1RefArray(object __instance, ref object __0)
        {
            try
            {
                if (EconomyConfig.T1Both == null || !EconomyConfig.T1BothOn) return true;

                if (__0 == null) return true;
                {
                    string firstTxt = ItemText(__0, 0);
                    bool inScopeNow = InScopeFirst2(__0);
                    if (!inScopeNow)
                    {
                        _log.LogInfo("[OI][t1][scope] 非目标 ⇒ Prefix 不追加（首项=" + Short(firstTxt) + "）");
                        return true;
                    }
                    _log.LogInfo("[OI][t1][scope] ★ 目标 ⇒ Prefix 追加（首项=" + Short(firstTxt) + "）");
                }
                _log.LogInfo("[OI][t1][b1] ★ Prefix 被调用（ref 版）⇒ 试造更大的数组 ✓");
                object arr = __0;

                _log.LogInfo("[OI][t1][cnt] 追加前 Length=" + Convert.ToString(Reflect.Prop(__0, "Length")) + "（★ 目标菜单应 2 ⇒ 3；若 4 ⇒ 重复追加 ✗）");
                var tyA = arr.GetType();
                object lenO = Reflect.Prop(arr, "Length");
                int n = lenO == null ? -1 : Convert.ToInt32(lenO);
                if (n < 0) { _log.LogWarning("[OI][t1][b1] Length 读不到 ⇒ 放弃 ✓"); return true; }
                object first = IndexN(arr, 0);
                object ours = null;
                
                try
                {
                    object f0 = Reflect.Prop(first, "formattedText");
                    object u1x = IndexN(arr, 1);
                    object f1 = u1x == null ? null : Reflect.Prop(u1x, "formattedText");
                    string t0 = f0 == null ? "ft=null" : Convert.ToString(Reflect.Prop(f0, "text"));
                    string t1 = f1 == null ? "ft=null" : Convert.ToString(Reflect.Prop(f1, "text"));
                    Diag("[OI][t1][cmp2] 第0项 formattedText.text=" + Short(t0));
                    Diag("[OI][t1][cmp2] 第1项 formattedText.text=" + Short(t1) + "（★ 两项不同处即文本来源 ✓）");
                    if (f0 != null) Diag("[OI][t1][cmp2] FormattedText 成员：" + FieldList(f0));
                }
                catch (Exception eCmp2) { _log.LogWarning("[OI][t1][cmp2] 字段对照异常：" + eCmp2.GetType().Name); }
                
                
                object ftOurs = null;
                try
                {
                    object f0b = Reflect.Prop(first, "formattedText");
                    string want9 = CfgT1("OptionText", "同时为两个项目拨款");
                    if (f0b != null)
                    {
                        var tyF2 = f0b.GetType();
                        System.Reflection.MethodInfo parseM = null;
                        foreach (var mm in tyF2.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static))
                        {
                            if (mm.Name != "Parse") continue;
                            var ps = mm.GetParameters();
                            if (ps.Length == 2 && ps[0].ParameterType == typeof(string)) { parseM = mm; break; }
                        }
                        if (parseM == null) _log.LogWarning("[OI][t1][b1] 未找到 FormattedText.Parse(string, …) ✗");
                        else
                        {
                            try { ftOurs = parseM.Invoke(null, new object[] { want9, null }); } catch (Exception eP0) { _log.LogWarning("[OI][t1][b1] Parse(text, null) 失败：" + eP0.GetType().Name); }
                            if (ftOurs == null)
                            {
                                try
                                {
                                    var p1 = parseM.GetParameters()[1].ParameterType;
                                    var elem = p1.IsArray ? p1.GetElementType() : null;
                                    object emptyArr = elem == null ? null : Array.CreateInstance(elem, 0);
                                    ftOurs = parseM.Invoke(null, new object[] { want9, emptyArr });
                                    _log.LogInfo("[OI][t1][b1] Parse(text, 空数组) 成功 ✓");
                                }
                                catch (Exception eP1) { _log.LogWarning("[OI][t1][b1] Parse(text, 空数组) 失败：" + eP1.GetType().Name); }
                            }
                        }
                        if (ftOurs != null)
                        {
                            bool okFt = Reflect.SetPropOrField(ftOurs, "text", want9);
                            string rbFt = Convert.ToString(Reflect.Prop(ftOurs, "text"));
                            _log.LogInfo("[OI][t1][b1] ★ 新 FormattedText（Parse 造 ✓）：设 text=" + okFt + " 复读=" + Short(rbFt) + "（★ 只写新造那份 ✓ 原项未动 ✓）");
                        }
                        else _log.LogWarning("[OI][t1][b1] Parse 未能造出 FormattedText ⇒ 第三项会显示 H-3 ✗（★ 兜底：GetUninitializedObject + 手设 backing field ✓）");
                    }
                }
                catch (Exception eFt) { _log.LogWarning("[OI][t1][b1] FormattedText 处理异常：" + eFt.GetType().Name); }
                try { ours = Activator.CreateInstance(first.GetType(), new object[] { ftOurs ?? Reflect.Prop(first, "formattedText"), Reflect.Prop(first, "destinationEntry"), true }); }
                catch (Exception eC) { _log.LogWarning("[OI][t1][b1] 克隆 Response 失败：" + eC.GetType().Name); }
                if (ours == null) return true;
                if (ours != null && ftOurs != null) { bool okAs = Reflect.SetPropOrField(ours, "formattedText", ftOurs); _log.LogInfo("[OI][t1][b1] ★ 二路装上 formattedText（可写属性 ✓）：写入=" + okAs + " 复读.text=" + Short(Convert.ToString(Reflect.Prop(Reflect.Prop(ours, "formattedText"), "text"))) + "（★ 双保险 ✓）"); }
                
                
                
                if (ours != null)
                {
                    try
                    {
                        object de0 = Reflect.Prop(first, "destinationEntry");
                        if (de0 == null) _log.LogWarning("[OI][t1][dest] 第 0 项 destinationEntry 为 null ⇒ 无法克隆 ✗");
                        else
                        {
                            var tyDE = de0.GetType();
                            var ctorDE = tyDE.GetConstructor(new[] { tyDE });
                            object deClone = null;
                            if (ctorDE != null) { deClone = ctorDE.Invoke(new object[] { de0 }); _log.LogInfo("[OI][t1][dest] ★ 已用拷贝构造克隆对话条目 ✓"); }
                            else _log.LogWarning("[OI][t1][dest] 未找到拷贝构造 ⇒ 无法克隆（★ 退路：仍用原条目 ⇒ 行为会走 H-3 ✗）");
                            if (deClone != null)
                            {
                                string seq = "Variable[\"BaseGameSupport.Journal_Turn01_PickedHighway\"] = true;\nVariable[\"BaseGameSupport.Journal_Turn01_PickedRailway\"] = true;";
                                bool okSeq = Reflect.SetPropOrField(deClone, "Sequence", seq);
                                bool okCur = Reflect.SetPropOrField(deClone, "currentSequence", seq);
                                string cnd = Short(Convert.ToString(Reflect.Prop(deClone, "conditionsString")));
                                string usr = Short(Convert.ToString(Reflect.Prop(deClone, "userScript")));
                                string did = Convert.ToString(Reflect.Prop(deClone, "id"));
                                string ttl = Short(Convert.ToString(Reflect.Prop(deClone, "Title")));
                                _log.LogInfo("[OI][t1][dest] 克隆体：Sequence 写入=" + okSeq + " currentSequence=" + okCur
                                    + " ｜ conditionsString=" + cnd + " userScript=" + usr + " id=" + did + " Title=" + ttl);
                                try
                                {
                                    if (!string.IsNullOrEmpty(cnd)) { bool okCl = Reflect.SetPropOrField(deClone, "conditionsString", ""); _log.LogInfo("[OI][t1][dest] 克隆体带条件 ⇒ 已清空：" + okCl + "（★ 只清克隆体 ✓ 原条目未动 ✓）"); }
                                }
                                catch (Exception eCl) { _log.LogWarning("[OI][t1][dest] 清条件异常：" + eCl.GetType().Name); }
                                bool okDe = Reflect.SetPropOrField(ours, "destinationEntry", deClone);

                                
                                try
                                {
                                    string line = CfgT1("T1BothLine", "我决定同时推进 H-3 公路与 L-1 高铁两个项目——两者即刻开工。");
                                    
                                    bool okT1 = Reflect.SetPropOrField(deClone, "currentDialogueText", line);
                                    bool okT2 = Reflect.SetPropOrField(deClone, "currentLocalizedDialogueText", line);
                                    bool okT3 = Reflect.SetPropOrField(deClone, "currentMenuText", line);
                                    bool okT4 = Reflect.SetPropOrField(deClone, "currentLocalizedMenuText", line);
                                    string rbT = Short(Convert.ToString(Reflect.Prop(deClone, "currentDialogueText")));
                                    _log.LogInfo("[OI][t1][entry] ★ 写我们的句子：dialogue=" + okT1 + " localizedDialogue=" + okT2 + " menu=" + okT3 + " localizedMenu=" + okT4 + " 复读=" + rbT);
                                    
                                    object orig = Reflect.Prop(first, "destinationEntry");
                                    object origLinks = orig == null ? null : Reflect.Prop(orig, "outgoingLinks");
                                    object nCopy = null;
                                    try { var clm = deClone.GetType().GetMethod("CopyLinks"); if (clm != null && origLinks != null) nCopy = clm.Invoke(deClone, new object[] { origLinks }); }
                                    catch (Exception eCl2) { _log.LogWarning("[OI][t1][entry] CopyLinks 调用异常：" + eCl2.GetType().Name); }
                                    if (nCopy != null) { bool okL = Reflect.SetPropOrField(deClone, "outgoingLinks", nCopy); _log.LogInfo("[OI][t1][entry] ★ 链接已接回原流程（CopyLinks ✓）：写入=" + okL); }
                                    else
                                    {
                                        bool okL2 = Reflect.SetPropOrField(deClone, "outgoingLinks", origLinks);
                                        _log.LogInfo("[OI][t1][entry] 链接改走直接赋值（CopyLinks 不可用）：写入=" + okL2 + "（★ 与原条目共用同一 List ⇒ 只读它、绝不改它 ✓）");
                                    }
                                    object linksNow = Reflect.Prop(deClone, "outgoingLinks");
                                    object cntNow = linksNow == null ? null : Reflect.Prop(linksNow, "Count");
                                    _log.LogInfo("[OI][t1][entry] 我们那条：文本=" + rbT + " ｜ 链接数=" + (cntNow == null ? "读不到 ✗" : cntNow.ToString()) + "（★ 剧情先播我们这句 ⇒ 再回原流程 ✓）");
                                }
                                catch (Exception eEnt) { _log.LogWarning("[OI][t1][entry] 写句子/接链接异常：" + eEnt.GetType().Name + " — " + Short(eEnt.Message)); }

                                
                                
                                
                                try
                                {
                                    object db = null; string howDb = "none";
                                    var ctrlT = ResolveType("PixelCrushers.DialogueSystem.DialogueSystemController") ?? ResolveTypeSimple("DialogueSystemController");
                                    if (ctrlT != null)
                                    {
                                        object ctrl = null;
                                        var objT2 = ResolveType("UnityEngine.Object") ?? ResolveTypeSimple("Object");
                                        var fot = objT2 == null ? null : objT2.GetMethod("FindObjectOfType", BindingFlags.Public | BindingFlags.Static, null, new[] { typeof(Type) }, null);
                                        if (fot != null) { try { ctrl = fot.Invoke(null, new object[] { ctrlT }); } catch (Exception eF) { _log.LogWarning("[OI][t1][walk] FindObjectOfType 异常：" + eF.GetType().Name); } }
                                        else _log.LogWarning("[OI][t1][walk] FindObjectOfType 未解析到（objT=" + (objT2 != null) + "）");
                                        Diag("[OI][t1][walk] DialogueSystemController=" + (ctrl == null ? "取不到 ✗" : "有 ✓"));
                                        if (ctrl != null)
                                        {
                                            object dm = null;
                                            try { var gdm = ctrl.GetType().GetMethod("get_databaseManager", Type.EmptyTypes); if (gdm != null) dm = gdm.Invoke(ctrl, null); } catch { }
                                            Diag("[OI][t1][walk] DatabaseManager=" + (dm == null ? "取不到 ✗" : "有 ✓ 成员：" + FieldList(dm)));
                                            if (dm != null)
                                            {
                                                foreach (var pn in new[] { "database", "masterDatabase" })
                                                {
                                                    try { object v = Reflect.Prop(dm, pn); if (v != null) { db = v; howDb = "DatabaseManager." + pn; break; } } catch { }
                                                }
                                            }
                                        }
                                    }
                                    if (db == null)
                                    {
                                        var dmT = ResolveType("PixelCrushers.DialogueSystem.DialogueManager") ?? ResolveTypeSimple("DialogueManager");
                                        Diag("[OI][t1][walk] DialogueManager 类型=" + (dmT == null ? "未解析 ✗" : "有 ✓"));
                                        if (dmT != null)
                                        {
                                            foreach (var pn in new[] { "database", "masterDatabase" })
                                            {
                                                try { object v = Reflect.Prop(dmT, pn); if (v != null) { db = v; howDb = "DialogueManager." + pn; break; } } catch { }
                                            }
                                        }
                                    }
                                    Diag("[OI][t1][walk] DialogueDatabase=" + (db == null ? "取不到 ✗（两条路径都试过 ✓）" : "有 ✓ via " + howDb));
                                    if (db != null)
                                    {
                                        var linkT = ResolveType("PixelCrushers.DialogueSystem.Link") ?? ResolveTypeSimple("Link");
                                        var gdeL = linkT == null ? null : db.GetType().GetMethod("GetDialogueEntry", new[] { linkT });
                                        Diag("[OI][t1][walk] Link 类型=" + (linkT == null ? "未解析 ✗" : "有 ✓") + " GetDialogueEntry(Link)=" + (gdeL != null));
                                        object cur = deClone;
                                        for (int hop = 0; hop < 20 && cur != null; hop++)
                                        {
                                            string tx = Convert.ToString(Reflect.Prop(cur, "currentDialogueText"));
                                            if (string.IsNullOrEmpty(tx)) tx = Convert.ToString(Reflect.Prop(cur, "currentMenuText"));
                                            object linksObj = Reflect.Prop(cur, "outgoingLinks");
                                            int ln = 0;
                                            object len2 = linksObj == null ? null : Reflect.Prop(linksObj, "Count");
                                            if (len2 != null) ln = Convert.ToInt32(len2);
                                            Diag("[OI][t1][walk] hop=" + hop
                                                + " id=" + Convert.ToString(Reflect.Prop(cur, "id"))
                                                + " Actor=" + Convert.ToString(Reflect.Prop(cur, "ActorID")) + "/" + Convert.ToString(Reflect.Prop(cur, "ConversantID"))
                                                + " 文本=" + Short(tx) + " 链接数=" + ln);
                                            if (ln <= 0) { Diag("[OI][t1][walk] ★ 链在此断开（hop=" + hop + " ⇒ 无 outgoingLinks ✗）"); break; }
                                            object firstLink = IndexN(linksObj, 0);
                                            object nxt = null;
                                            if (gdeL != null && firstLink != null) { try { nxt = gdeL.Invoke(db, new object[] { firstLink }); } catch (Exception eL) { _log.LogWarning("[OI][t1][walk] GetDialogueEntry(Link) 异常：" + eL.GetType().Name); } }
                                            if (nxt == null) { Diag("[OI][t1][walk] ★ 下一跳取不到（hop=" + hop + " ⇒ 遍历停止 ✗）"); break; }
                                            cur = nxt;
                                        }
                                    }
                                }
                                catch (Exception eWalk) { _log.LogWarning("[OI][t1][walk] 遍历异常：" + eWalk.GetType().Name + " — " + Short(eWalk.Message)); }


                                
                                
                                
                                try
                                {
                                    
                                    object db2 = null; string howDb2 = "none";

                                    
                                    
                                    
                                    try
                                    {
                                        var dmT3 = ResolveType("PixelCrushers.DialogueSystem.DialogueManager") ?? ResolveTypeSimple("DialogueManager");
                                        if (dmT3 == null) _log.LogWarning("[OI][t1][db] DialogueManager 类型未解析 ✗");
                                        else
                                        {
                                            object mdb = null;
                                            try { mdb = Reflect.Prop(dmT3, "MasterDatabase"); } catch { }
                                            if (mdb == null) { try { mdb = Reflect.Prop(dmT3, "masterDatabase"); } catch { } }
                                            if (mdb == null)
                                            {
                                                try
                                                {
                                                    var pm = dmT3.GetMethod("PreloadMasterDatabase", BindingFlags.Public | BindingFlags.Static, null, Type.EmptyTypes, null);
                                                    if (pm != null) { pm.Invoke(null, null); _log.LogInfo("[OI][t1][db] 已调 PreloadMasterDatabase() ✓"); mdb = Reflect.Prop(dmT3, "MasterDatabase"); }
                                                    else _log.LogWarning("[OI][t1][db] PreloadMasterDatabase 未找到 ✗");
                                                }
                                                catch (Exception ePm) { _log.LogWarning("[OI][t1][db] PreloadMasterDatabase 异常：" + ePm.GetType().Name); }
                                            }
                                            _log.LogInfo("[OI][t1][db] MasterDatabase=" + (mdb == null ? "空 ✗" : "有 ✓"));
                                            if (mdb != null) { db2 = mdb; howDb2 = "DialogueManager.MasterDatabase(static)"; }

                                    
                                    if (db2 == null)
                                    {
                                        try
                                        {
                                            var dmT4 = ResolveType("PixelCrushers.DialogueSystem.DialogueManager") ?? ResolveTypeSimple("DialogueManager");
                                            
                                            if (dmT4 != null)
                                            {
                                                object dbm = null;
                                                try { var gdm4 = dmT4.GetMethod("get_DatabaseManager", BindingFlags.Public | BindingFlags.Static, null, Type.EmptyTypes, null); if (gdm4 != null) dbm = gdm4.Invoke(null, null); } catch { }
                                                _log.LogInfo("[OI][t1][db2] DialogueManager.DatabaseManager=" + (dbm == null ? "空 ✗" : "有 ✓"));
                                                if (dbm != null)
                                                {
                                                    foreach (var pn in new[] { "database", "masterDatabase" })
                                                    {
                                                        try { object v = Reflect.Prop(dbm, pn); if (v != null) { db2 = v; howDb2 = "DialogueManager.DatabaseManager." + pn; break; } } catch { }
                                                    }
                                                    if (db2 == null) _log.LogInfo("[OI][t1][db2] DatabaseManager 成员：" + FieldList(dbm));
                                                }
                                                
                                                if (db2 == null)
                                                {
                                                    object inst = null;
                                                    try { var gi = dmT4.GetMethod("get_Instance", BindingFlags.Public | BindingFlags.Static, null, Type.EmptyTypes, null); if (gi != null) inst = gi.Invoke(null, null); } catch { }
                                                    _log.LogInfo("[OI][t1][db2] DialogueManager.Instance=" + (inst == null ? "空 ✗" : "有 ✓"));
                                                    if (inst != null)
                                                    {
                                                        foreach (var pn in new[] { "database", "masterDatabase", "get_database", "get_masterDatabase" })
                                                        {
                                                            try { object v = Reflect.Prop(inst, pn); if (v != null) { db2 = v; howDb2 = "DialogueManager.Instance." + pn; break; } } catch { }
                                                        }
                                                    }
                                                }
                                            }
                                            
                                            if (db2 == null)
                                            {
                                                var cT = ResolveType("PixelCrushers.DialogueSystem.DialogueSystemController") ?? ResolveTypeSimple("DialogueSystemController");
                                                object ci = null;
                                                if (cT != null) { try { ci = Reflect.Prop(cT, "m_instance"); } catch (Exception eC4) { _log.LogWarning("[OI][t1][db2] 读 m_instance 异常：" + eC4.GetType().Name); } }
                                                _log.LogInfo("[OI][t1][db2] DialogueSystemController.m_instance=" + (ci == null ? "空 ✗" : "有 ✓"));
                                                if (ci != null)
                                                {
                                                    object dm4 = null;
                                                    try { var g4 = ci.GetType().GetMethod("get_databaseManager", Type.EmptyTypes); if (g4 != null) dm4 = g4.Invoke(ci, null); } catch { }
                                                    if (dm4 != null) { foreach (var pn in new[] { "database", "masterDatabase" }) { try { object v = Reflect.Prop(dm4, pn); if (v != null) { db2 = v; howDb2 = "m_instance.databaseManager." + pn; break; } } catch { } } }
                                                    if (db2 == null) { foreach (var pn in new[] { "database", "masterDatabase" }) { try { object v = Reflect.Prop(ci, pn); if (v != null) { db2 = v; howDb2 = "m_instance." + pn; break; } } catch { } } }
                                                    if (db2 == null) _log.LogInfo("[OI][t1][db2] m_instance 成员：" + FieldList(ci));
                                                }
                                            }
                                            _log.LogInfo("[OI][t1][db2] 三条候选之后 ⇒ db=" + (db2 == null ? "仍为空 ✗" : "有 ✓ via " + howDb2));
                                        }
                                        catch (Exception eD2) { _log.LogWarning("[OI][t1][db2] 候选取库异常：" + eD2.GetType().Name + " — " + Short(eD2.Message)); }
                                    }
                                        }
                                    }
                                    catch (Exception eDb) { _log.LogWarning("[OI][t1][db] 静态取库异常：" + eDb.GetType().Name); }
                                    var ctrlT2 = ResolveType("PixelCrushers.DialogueSystem.DialogueSystemController") ?? ResolveTypeSimple("DialogueSystemController");
                                    if (ctrlT2 != null)
                                    {
                                        object ctrl2 = null;
                                        var objT3 = ResolveType("UnityEngine.Object") ?? ResolveTypeSimple("Object");
                                        var fot3 = objT3 == null ? null : objT3.GetMethod("FindObjectOfType", BindingFlags.Public | BindingFlags.Static, null, new[] { typeof(Type) }, null);
                                        if (fot3 != null) { try { ctrl2 = fot3.Invoke(null, new object[] { ctrlT2 }); } catch { } }
                                        if (ctrl2 != null)
                                        {
                                            object dm2 = null;
                                            try { var gdm2 = ctrl2.GetType().GetMethod("get_databaseManager", Type.EmptyTypes); if (gdm2 != null) dm2 = gdm2.Invoke(ctrl2, null); } catch { }
                                            if (dm2 != null) { foreach (var pn in new[] { "database", "masterDatabase" }) { try { object v = Reflect.Prop(dm2, pn); if (v != null) { db2 = v; howDb2 = "DatabaseManager." + pn; break; } } catch { } } }
                                        }
                                    }
                                    if (db2 == null)
                                    {
                                        var dmT2 = ResolveType("PixelCrushers.DialogueSystem.DialogueManager") ?? ResolveTypeSimple("DialogueManager");
                                        if (dmT2 != null) { foreach (var pn in new[] { "database", "masterDatabase" }) { try { object v = Reflect.Prop(dmT2, pn); if (v != null) { db2 = v; howDb2 = "DialogueManager." + pn; break; } } catch { } } }
                                    }
                                    _log.LogInfo("[OI][t1][chain3] DialogueDatabase=" + (db2 == null ? "取不到 ✗" : "有 ✓ via " + howDb2));
                                    if (db2 != null)
                                    {
                                        object convId2 = Reflect.Prop(deClone, "conversationID");
                                        var gconv = db2.GetType().GetMethod("GetConversation", new[] { typeof(int) });
                                        object conv = gconv == null ? null : gconv.Invoke(db2, new object[] { Convert.ToInt32(convId2) });
                                        _log.LogInfo("[OI][t1][chain3] Conversation(convId=" + Convert.ToString(convId2) + ")=" + (conv == null ? "取不到 ✗" : "有 ✓"));
                                        if (conv != null)
                                        {
                                            var anm = conv.GetType().GetMethod("AddNewDialogueEntry");
                                            _log.LogInfo("[OI][t1][chain3] AddNewDialogueEntry 存在=" + (anm != null));
                                            if (anm != null)
                                            {
                                                object origLinks3 = Reflect.Prop(deClone, "outgoingLinks");
                                                int origCnt3 = 0;
                                                object oc3 = origLinks3 == null ? null : Reflect.Prop(origLinks3, "Count");
                                                if (oc3 != null) origCnt3 = Convert.ToInt32(oc3);
                                                _log.LogInfo("[OI][t1][chain3] 原链接数=" + origCnt3 + "（★ 用于链尾接回 ✓）");
                                                string[] lines3 = new[] {
                                                    CfgT1("T1ChainLileas", "感谢上帝，我就知道你是个明白事理的人。"),
                                                    CfgT1("T1ChainGus", "这是个了不起的选择，总统先生。"),
                                                    CfgT1("T1ChainSymon", "谈判将于今年年中开始，您可以将合同授予您选择的公司。") };
                                                int[] actors3 = new[] { 44, 35, 51 };

                                                
                                                
                                                
                                                
                                                try
                                                {
                                                    string s1 = CfgT1("T1ChainSymon", "谈判将于今年年中开始，您可以将合同授予您选择的公司。");
                                                    string s2 = CfgT1("T1ChainSymon2", "我们估计，如果每一步都能顺利完成，工程将在两年内完成。");
                                                    if (!string.IsNullOrEmpty(s2)) lines3[2] = s1 + "\n" + s2;
                                                    else lines3[2] = s1;
                                                    _log.LogInfo("[OI][t1][chain3] ★ 西蒙节点文本（两句 ✓）=" + Short(lines3[2]));
                                                }
                                                catch (Exception eS2) { _log.LogWarning("[OI][t1][chain3] 西蒙两句拼接异常：" + eS2.GetType().Name); }
                                                object[] made = new object[3];
                                                for (int k = 0; k < 3; k++)
                                                {
                                                    object nn = null;
                                                    try { nn = anm.Invoke(conv, new object[] { deClone, lines3[k], 0, false }); }
                                                    catch (Exception eAdd) { _log.LogWarning("[OI][t1][chain3] AddNewDialogueEntry 异常：" + eAdd.GetType().Name + " — " + Short(eAdd.Message)); }
                                                    if (nn == null) { _log.LogWarning("[OI][t1][chain3] 第 " + k + " 条登记失败 ⇒ 停 ✗"); break; }
                                                    made[k] = nn;
                                                    Reflect.SetPropOrField(nn, "ActorID", actors3[k]);
                                                    Reflect.SetPropOrField(nn, "ConversantID", 5);
                                                    Reflect.SetPropOrField(nn, "currentDialogueText", lines3[k]);
                                                    Reflect.SetPropOrField(nn, "currentLocalizedDialogueText", lines3[k]);
                                                    Reflect.SetPropOrField(nn, "currentMenuText", lines3[k]);
                                                    Reflect.SetPropOrField(nn, "currentLocalizedMenuText", lines3[k]);
                                                    _log.LogInfo("[OI][t1][chain3] ★ 已登记第 " + k + " 条：游戏给的 id=" + Convert.ToString(Reflect.Prop(nn, "id")) + " Actor=" + actors3[k] + " 文本=" + Short(Convert.ToString(Reflect.Prop(nn, "currentDialogueText"))));
                                                }
                                                if (made[0] != null && made[1] != null && made[2] != null)
                                                {
                                                    var clm4 = deClone.GetType().GetMethod("CopyLinks");
                                                    for (int k = 0; k < 3; k++)
                                                    {
                                                        object lk = null;
                                                        if (clm4 != null && origLinks3 != null) { try { lk = clm4.Invoke(made[k], new object[] { origLinks3 }); } catch { } }
                                                        if (lk == null) { _log.LogWarning("[OI][t1][chain3] 第 " + k + " 条链接造不出 ⇒ 停 ✗"); break; }
                                                        object f3 = IndexN(lk, 0);
                                                        if (f3 != null && k < 2)
                                                        {
                                                            Reflect.SetPropOrField(f3, "destinationConversationID", Reflect.Prop(made[k + 1], "conversationID"));
                                                            Reflect.SetPropOrField(f3, "destinationDialogueID", Reflect.Prop(made[k + 1], "id"));
                                                            _log.LogInfo("[OI][t1][chain3] 第 " + k + " 条 ⇒ 指向 id=" + Convert.ToString(Reflect.Prop(made[k + 1], "id")) + " ✓");
                                                        }
                                                        else _log.LogInfo("[OI][t1][chain3] 第 " + k + " 条 ⇒ 保持原链接（★ 接回原流程 ✓）");

                                                        
                                                        
                                                        
                                                        if (k == 2 && lk != null && db2 != null)
                                                        {
                                                            try
                                                            {
                                                                var gdeL2 = db2.GetType().GetMethod("GetDialogueEntry", new[] { (ResolveType("PixelCrushers.DialogueSystem.Link") ?? ResolveTypeSimple("Link")) });
                                                                object fw = IndexN(lk, 0);
                                                                object cur3 = (gdeL2 != null && fw != null) ? gdeL2.Invoke(db2, new object[] { fw }) : null;
                                                                object symonNode = null; object rejoinNode = null;
                                                                for (int h = 0; h < 8 && cur3 != null; h++)
                                                                {
                                                                    string tx3 = Convert.ToString(Reflect.Prop(cur3, "currentDialogueText"));
                                                                    if (string.IsNullOrEmpty(tx3)) tx3 = Convert.ToString(Reflect.Prop(cur3, "currentMenuText"));
                                                                    _log.LogInfo("[OI][t1][rejoin] hop=" + h + " id=" + Convert.ToString(Reflect.Prop(cur3, "id")) + " Actor=" + Convert.ToString(Reflect.Prop(cur3, "ActorID")) + " 文本=" + Short(tx3));
                                                                    if (!string.IsNullOrEmpty(tx3) && tx3.IndexOf("谈判将于今年年中开始", StringComparison.OrdinalIgnoreCase) >= 0) { symonNode = cur3; }
                                                                    object lk3 = Reflect.Prop(cur3, "outgoingLinks");
                                                                    object n3 = lk3 == null ? null : IndexN(lk3, 0);
                                                                    object nx3 = (gdeL2 != null && n3 != null) ? gdeL2.Invoke(db2, new object[] { n3 }) : null;
                                                                    if (symonNode != null && !ReferenceEquals(nx3, cur3) && nx3 != null) { rejoinNode = nx3; break; }
                                                                    cur3 = nx3;
                                                                }
                                                                if (rejoinNode != null && fw != null)
                                                                {
                                                                    bool okc1 = Reflect.SetPropOrField(fw, "destinationConversationID", Reflect.Prop(rejoinNode, "conversationID"));
                                                                    bool okc2 = Reflect.SetPropOrField(fw, "destinationDialogueID", Reflect.Prop(rejoinNode, "id"));
                                                                    _log.LogInfo("[OI][t1][rejoin] ★ 接回点已后移到西蒙之后：id=" + Convert.ToString(Reflect.Prop(rejoinNode, "id")) + " 文本=" + Short(Convert.ToString(Reflect.Prop(rejoinNode, "currentDialogueText"))) + "（conv 写=" + okc1 + " id 写=" + okc2 + "）");
                                                                }
                                                                else _log.LogWarning("[OI][t1][rejoin] 未在 8 跳内认出西蒙 ⇒ 保持原接回点 ✗（★ 会重复一次 ✓）");
                                                            }
                                                            catch (Exception eRj) { _log.LogWarning("[OI][t1][rejoin] 异常：" + eRj.GetType().Name + " — " + Short(eRj.Message)); }
                                                        }
                                                        Reflect.SetPropOrField(made[k], "outgoingLinks", lk);
                                                    }
                                                    object entryLk3 = null;
                                                    if (clm4 != null && origLinks3 != null) { try { entryLk3 = clm4.Invoke(deClone, new object[] { origLinks3 }); } catch { } }
                                                    object ef3 = entryLk3 == null ? null : IndexN(entryLk3, 0);
                                                    if (ef3 != null)
                                                    {
                                                        Reflect.SetPropOrField(ef3, "destinationConversationID", Reflect.Prop(made[0], "conversationID"));
                                                        Reflect.SetPropOrField(ef3, "destinationDialogueID", Reflect.Prop(made[0], "id"));
                                                        bool okE3 = Reflect.SetPropOrField(deClone, "outgoingLinks", entryLk3);
                                                        _log.LogInfo("[OI][t1][chain3] ★ 入口已改指第 ① 条（id=" + Convert.ToString(Reflect.Prop(made[0], "id")) + "）写入=" + okE3 + " ⇒ 旁白 ⇒ 莉亚斯 ⇒ 格斯 ⇒ 西蒙 ⇒ 原流程 ✓");
                                                    }
                                                }
                                                else _log.LogWarning("[OI][t1][chain3] 三条未全部登记 ⇒ 未串链（★ 原流程不受影响 ✓）");
                                            }
                                        }
                                    }
                                }
                                catch (Exception eC3) { _log.LogWarning("[OI][t1][chain3] 造链异常：" + eC3.GetType().Name + " — " + Short(eC3.Message)); }

                                
                                
                                
                                
                                try
                                {
                                    var mkLua = typeof(EconomyPatches).GetMethod("RunInstant", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
                                    if (mkLua == null) _log.LogWarning("[OI][t1][inv2] RunInstant 未找到 ⇒ 无法补写 ✗");
                                    else
                                    {
                                        string[] luaLines = new string[] {
                                            "Variable[\"BaseGame.Policy_Economy_HighwayProject\"] = true;",
                                            "Variable[\"BaseGame.Policy_Economy_RailwayProject\"] = true;",
                                            "Variable[\"BaseGame.Decision_Turn01_InfrastructureProject_Invest\"] = true;",
                                            "Variable[\"BaseGame.Decision_Turn01_InfrastructureProject_RejectThePlans\"] = false;",
                                            "Turn01_PickedHighway.Index = 5;",
                                            "Turn01_PickedRailway.Index = 5;",
                                            "Progress_Highway.IsNotificationActive = true;",
                                            "Progress_Railway.IsNotificationActive = true;",
                                            "Sordland_City_Lachaven.HasNewStatusEffect = true;",
                                            "Sordland_City_Holsord.HasNewStatusEffect = true;",
                                            "AddTokenStatus(\"Sordland_City_Lachaven\", \"Progress_Highway\");",
                                            "AddTokenStatus(\"Sordland_City_Holsord\", \"Progress_Railway\");",
                                            "RefreshAll();",
                                            "RefreshNoviceBasePanel();"
                                        };
                                        for (int q = 0; q < luaLines.Length; q++)
                                        {
                                            try
                                            {
                                                mkLua.Invoke(null, new object[] { luaLines[q] });
                                                _log.LogInfo("[OI][t1][inv2] 写 " + (q + 1) + "/" + luaLines.Length + " ✓ :: " + luaLines[q]);
                                            }
                                            catch (Exception eQ) { _log.LogWarning("[OI][t1][inv2] 第 " + (q + 1) + " 行失败 ✗ :: " + luaLines[q] + " ⇒ " + eQ.GetType().Name + " " + Short(eQ.Message)); }
                                        }
                                        try
                                        {

                                        
                                        try
                                        {
                                            var dlT = ResolveType("PixelCrushers.DialogueSystem.DialogueLua") ?? ResolveTypeSimple("DialogueLua");
                                            var doesExist = dlT == null ? null : dlT.GetMethod("DoesVariableExist", BindingFlags.Public | BindingFlags.Static, null, new[] { typeof(string) }, null);
                                            string[] names2 = new string[] {
                                                "BaseGame.Turn01_InT_Investment_Highway",
                                                "BaseGame.Turn01_InT_Investment_Railway",
                                                "BaseGame.Policy_Economy_HighwayProject",
                                                "BaseGame.Policy_Economy_RailwayProject",
                                                "BaseGame.Decision_Turn01_InfrastructureProject_Invest",
                                                "BaseGame.Decision_Turn01_InfrastructureProject_RejectThePlans" };
                                            _log.LogInfo("[OI][t1][name] DoesVariableExist 已解析=" + (doesExist != null));
                                            foreach (var nm2 in names2)
                                            {
                                                string ex1 = "?", ex2 = "?";
                                                if (doesExist != null)
                                                {
                                                    try { ex1 = Convert.ToString(doesExist.Invoke(null, new object[] { nm2 })); } catch { }
                                                    try { ex2 = Convert.ToString(doesExist.Invoke(null, new object[] { nm2.Replace("BaseGame.", "") })); } catch { }
                                                }
                                                _log.LogInfo("[OI][t1][name] " + nm2 + " ⇒ 带前缀存在=" + ex1 + " ｜ 无前缀存在=" + ex2);
                                            }
                                            var isT3 = typeof(EconomyPatches).GetMethod("IsTrue", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
                                            if (isT3 != null)
                                            {
                                                var sb2 = new System.Text.StringBuilder();
                                                foreach (var nm3 in names2)
                                                {
                                                    object r1 = null; object r2 = null;
                                                    try { r1 = isT3.Invoke(null, new object[] { nm3 + " == true" }); } catch { }
                                                    try { r2 = isT3.Invoke(null, new object[] { nm3.Replace("BaseGame.", "") + " == true" }); } catch { }
                                                    sb2.Append(nm3).Append(" ⇒ 带前缀=").Append(Convert.ToString(r1)).Append(" 无前缀=").Append(Convert.ToString(r2)).Append(" ｜ ");
                                                }
                                                _log.LogInfo("[OI][t1][name] ★ 分组回读 ⇒ " + sb2.ToString());
                                            }
                                        }
                                        catch (Exception eNm) { _log.LogWarning("[OI][t1][name] 名字检查异常：" + eNm.GetType().Name); }
                                            var isT2 = typeof(EconomyPatches).GetMethod("IsTrue", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
                                            if (isT2 != null)
                                            {
                                                string[] chk = new string[] {
                                                    "BaseGame.Turn01_InT_Investment_Highway == true",
                                                    "BaseGame.Turn01_InT_Investment_Railway == true",
                                                    "BaseGame.Policy_Economy_HighwayProject == true",
                                                    "BaseGame.Policy_Economy_RailwayProject == true",
                                                    "BaseGame.Decision_Turn01_InfrastructureProject_Invest == true",
                                                    "BaseGame.Decision_Turn01_InfrastructureProject_RejectThePlans == false" };
                                                var sbR = new System.Text.StringBuilder();
                                                foreach (var ck in chk)
                                                {
                                                    object rv = null;
                                                    try { rv = isT2.Invoke(null, new object[] { ck }); } catch { }
                                                    sbR.Append(ck.Replace("BaseGame.", "").Replace(" == true", "").Replace(" == false", "(应为false)")).Append("=").Append(Convert.ToString(rv)).Append(" ｜ ");
                                                }
                                                _log.LogInfo("[OI][t1][inv2] ★ 回读 ⇒ " + sbR.ToString());
                                            }
                                            else _log.LogWarning("[OI][t1][inv2] IsTrue 未找到 ⇒ 未回读 ✗");
                                        }
                                        catch (Exception eRb2) { _log.LogWarning("[OI][t1][inv2] 回读异常：" + eRb2.GetType().Name); }
                                    }
                                }
                                catch (Exception eInv2) { _log.LogWarning("[OI][t1][inv2] 实体层补写异常：" + eInv2.GetType().Name + " — " + Short(eInv2.Message)); }
                                _log.LogInfo("[OI][t1][dest] ★ 已把【克隆条目】装到我们那一项：写入=" + okDe + "（★ 原两项的条目未动 ✓）");

                                _log.LogInfo("[OI][t1][dest] 复读我们那一项：destinationEntry.Sequence=" + Short(Convert.ToString(Reflect.Prop(Reflect.Prop(ours, "destinationEntry"), "Sequence"))) + "（★ 应含两个 Picked… ✓）");
                            }
                        }
                    }
                    catch (Exception eD) { _log.LogWarning("[OI][t1][dest] 异常：" + eD.GetType().Name + " — " + Short(eD.Message)); }
                }
                object newArr = null;
                try { newArr = Activator.CreateInstance(tyA, new object[] { (long)(n + 1) }); }
                catch (Exception eN) { _log.LogWarning("[OI][t1][b1] ctor(Int64) 失败：" + eN.GetType().Name); }
                if (newArr == null) return true;
                var setI = tyA.GetMethod("set_Item", new[] { typeof(int), first.GetType() });
                if (setI == null) { _log.LogWarning("[OI][t1][b1] set_Item 未找到 ⇒ 放弃 ✓"); return true; }
                for (int i = 0; i < n; i++) { setI.Invoke(newArr, new object[] { i, IndexN(arr, i) }); }
                setI.Invoke(newArr, new object[] { n, ours });
                __0 = newArr;
                object newLen = Reflect.Prop(__0, "Length");
                _log.LogInfo("[OI][t1][b1] ★ 已替换为更大的数组：原长=" + n + " 新长=" + (newLen == null ? "读失败 ✗" : newLen.ToString()) + "（★ 若游戏据此建 UI ⇒ 第三项由它自己建 ✓）");
                _t1B1Done = true;   

                try
                {
                    var cr2 = __instance.GetType().GetMethod("CreateResponse", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                    string how2; object cb2 = BuildUnityActionFor(typeof(EconomyPatches).GetMethod("T1BothClick", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public), out how2);
                    if (cr2 != null && cb2 != null)
                    {
                        cr2.Invoke(__instance, new object[] { ours, n, cb2 });
                        _log.LogInfo("[OI][t1][act] ★ 已给我们那一项挂上回调（" + how2 + "）⇒ 点击应执行 T1BothClick（两个 Picked… 置 true ✓）");
                    }
                    else _log.LogWarning("[OI][t1][act] 挂回调失败：cr=" + (cr2 != null) + " cb=" + (cb2 != null));
                }
                catch (Exception eAct) { _log.LogWarning("[OI][t1][act] 挂回调异常：" + eAct.GetType().Name + " — " + Short(eAct.Message)); }
                return true;
            }
            catch (Exception eB1) { _log.LogWarning("[OI][t1][b1] 异常：" + eB1.GetType().Name + " — " + Short(eB1.Message)); return true; }
        }

        
        
        private static bool Prefix_T1CreateBefore(object __instance, object __0)
        {
            try
            {
                if (EconomyConfig.T1Both == null || !EconomyConfig.T1BothOn) return true;
                if (_t1B1Done) { _log.LogInfo("[OI][t1][b2] b1 已成功 ⇒ 本条跳过 ✓（互斥 ✓）"); return true; }

                if (__0 == null) return true;
                {
                    string firstTxt = ItemText(__0, 0);
                    bool inScopeNow = InScopeFirst2(__0);
                    if (!inScopeNow)
                    {
                        _log.LogInfo("[OI][t1][scope] 非目标 ⇒ Prefix 不追加（首项=" + Short(firstTxt) + "）");
                        return true;
                    }
                    _log.LogInfo("[OI][t1][scope] ★ 目标 ⇒ Prefix 追加（首项=" + Short(firstTxt) + "）");
                }
                object lenO = Reflect.Prop(__0, "Length");
                int n = lenO == null ? -1 : Convert.ToInt32(lenO);
                if (n < 0) return true;
                object first = IndexN(__0, 0);
                object ours = null;
                try { ours = Activator.CreateInstance(first.GetType(), new object[] { Reflect.Prop(first, "formattedText"), Reflect.Prop(first, "destinationEntry"), true }); }
                catch (Exception eC2) { _log.LogWarning("[OI][t1][b2] 克隆 Response 失败：" + eC2.GetType().Name); }
                if (ours == null) return true;
                var cr = __instance.GetType().GetMethod("CreateResponse", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                string cbHow; object cb = BuildUnityActionFor(typeof(EconomyPatches).GetMethod("T1BothClick", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public), out cbHow);
                if (cr != null && cr.GetParameters().Length == 3)
                {
                    cr.Invoke(__instance, new object[] { ours, n, cb });
                    _log.LogInfo("[OI][t1][b2] ★ 已在【建 UI 之前】调用游戏自己的 CreateResponse(index=" + n + ", " + cbHow + ") ✓ ⇒ 第三项应由它自己建 ✓");
                }
                else _log.LogWarning("[OI][t1][b2] CreateResponse 不可用（cr=" + (cr != null) + "）");
            }
            catch (Exception eB2) { _log.LogWarning("[OI][t1][b2] 异常：" + eB2.GetType().Name + " — " + Short(eB2.Message)); }
            return true;
        }

        

        
        
        
        

        
        
        
        
        private static bool InScopeFirst2(object arr)
        {
            try
            {
                string first = ItemText(arr, 0);
                if (string.IsNullOrEmpty(first)) { Diag("[OI][t1][scope3] 首项读不到 ⇒ 视为非目标 ✗"); return false; }
                int len = first.Length;
                var codes = new System.Text.StringBuilder();
                for (int i = 0; i < len && i < 8; i++) { codes.Append(((int)first[i]).ToString()).Append(' '); }
                string probe = "我已决定实施";
                bool hasc = first.Contains(probe);
                bool swc = first.StartsWith(probe);
                bool swo = first.StartsWith(probe, StringComparison.Ordinal);
                bool hasearly = first.Contains("高速公路");
                Diag("[OI][t1][scope3] 首项长度=" + len + " 前8字符码=[" + codes.ToString().Trim() + "] 首1码=" + ((int)first[0]));
                Diag("[OI][t1][scope3] 三种比较：Contains=" + hasc + " StartsWith(默认)=" + swc + " StartsWith(Ordinal)=" + swo + " ｜ 含「高速公路」=" + hasearly);
                if (hasc)
                {
                    Diag("[OI][t1][scope3] ★ 目标（首项【包含】「我已决定实施」✓）首项=" + Short(first));
                    return true;
                }
                Diag("[OI][t1][scope3] 非目标（首项不含「我已决定实施」✗）首项=" + Short(first));
                return false;
            }
            catch (Exception e) { _log.LogWarning("[OI][t1][scope3] 判据异常：" + e.GetType().Name); return false; }
        }
        private static bool InScopeFirst(object arr)
        {
            try
            {
                string first = ItemText(arr, 0);
                if (string.IsNullOrEmpty(first)) { _log.LogInfo("[OI][t1][scope] 首项读不到 ⇒ 视为非目标 ✗"); return false; }
                string target = CfgT1("TargetText", "");
                if (!string.IsNullOrEmpty(target))
                {
                    foreach (var tok in target.Split(new[] { (char)124, (char)59, (char)44 }, StringSplitOptions.RemoveEmptyEntries))
                    {
                        var q = tok.Trim();
                        if (q.Length > 0 && first.StartsWith(q, StringComparison.OrdinalIgnoreCase))
                        {
                            _log.LogInfo("[OI][t1][scope] ★ 目标（首项以 cfg 令牌「" + q + "」开头 ✓）首项=" + Short(first));
                            return true;
                        }
                    }
                }
                if (first.StartsWith("我已决定实施", StringComparison.OrdinalIgnoreCase))
                {
                    _log.LogInfo("[OI][t1][scope] ★ 目标（首项以「我已决定实施」开头 ✓）首项=" + Short(first));
                    return true;
                }
                _log.LogInfo("[OI][t1][scope] 非目标（首项既不以 cfg 令牌开头、也不以「我已决定实施」开头 ✗）首项=" + Short(first));
                return false;
            }
            catch (Exception e) { _log.LogWarning("[OI][t1][scope] 判据异常：" + e.GetType().Name); return false; }
        }
        private static bool InScopeArray(object arr)
        {
            try
            {
                string target = CfgT1("TargetText", "");
                if (string.IsNullOrEmpty(target)) return false;
                object lenO = Reflect.Prop(arr, "Length");
                int n = lenO == null ? 0 : Convert.ToInt32(lenO);
                for (int i = 0; i < n; i++)
                {
                    string txt = ItemText(arr, i);
                    if (string.IsNullOrEmpty(txt)) continue;
                    foreach (var tok in target.Split(new[] { (char)124, (char)59, (char)44 }, StringSplitOptions.RemoveEmptyEntries))
                    {
                        var q = tok.Trim();
                        if (q.Length > 0 && txt.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0) return true;
                    }
                }
            }
            catch { }
            return false;
        }

        
        private static bool _t1B1Done;


        
        
        
        
        
        private static bool Prefix_T1ResponseClick(object __instance, object __0)
        {
            try
            {
                if (EconomyConfig.T1Both == null || !EconomyConfig.T1BothOn) return true;
                string want = CfgT1("OptionText", "同时为两个项目拨款");
                string txt = "";
                if (__0 != null)
                {
                    object ft = Reflect.Prop(__0, "formattedText");
                    if (ft != null) txt = Convert.ToString(Reflect.Prop(ft, "text"));
                }
                if (!string.IsNullOrEmpty(want) && !string.IsNullOrEmpty(txt) && txt.IndexOf(want, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    _log.LogInfo("[OI][t1][clickin] ★ 我们的项被点击 ⇒ 自己执行、跳过原方法 ✓（文本=" + Short(txt) + "）");

                    _log.LogInfo("[OI][t1][clickin] ★ 我们的项被点击 ⇒ 已置两个变量 ⇒ 【放行原方法】（不跳过 ✓）文本=" + Short(txt));
                    T1BothClick();

                    
                    
                    
                    
                    
                    try
                    {
                        var mkInv = typeof(EconomyPatches).GetMethod("RunInstant", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
                        if (mkInv == null) _log.LogWarning("[OI][t1][inv] RunInstant 未找到 ⇒ 无法置开工标记 ✗");
                        else
                        {
                            string[] invKeys = new[] { "BaseGame.Turn01_InT_Investment_Highway", "BaseGame.Turn01_InT_Investment_Railway" };
                            foreach (var k in invKeys)
                            {
                                try
                                {
                                    mkInv.Invoke(null, new object[] { "Variable[\"" + k + "\"] = true;" });
                                    _log.LogInfo("[OI][t1][inv] 已写 " + k + " = true ✓");
                                }
                                catch (Exception eK) { _log.LogWarning("[OI][t1][inv] 写 " + k + " 异常：" + eK.GetType().Name); }
                            }
                            _log.LogInfo("[OI][t1][inv] ★ 两个开工标记均已写入 ⇒ 请对照存档/战报确认两条都算开工 ✓（★ 未回读 ✗）");

                    
                    try
                    {
                        var isT = typeof(EconomyPatches).GetMethod("IsTrue", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
                        if (isT == null) _log.LogWarning("[OI][t1][inv] IsTrue 未找到 ⇒ 无法回读 ✗（★ 那就只看行为 ✓）");
                        else
                        {
                            object hb = isT.Invoke(null, new object[] { "BaseGame.Turn01_InT_Investment_Highway == true" });
                            object rb = isT.Invoke(null, new object[] { "BaseGame.Turn01_InT_Investment_Railway == true" });
                            _log.LogInfo("[OI][t1][inv] ★ 回读 ⇒ Highway=" + Convert.ToString(hb) + " Railway=" + Convert.ToString(rb) + "（★ 两个都 True ⇒ 我们写的保住了 ✓；Railway=False ⇒ 被游戏后续覆盖 ✗）");
                        }
                    }
                    catch (Exception eRb) { _log.LogWarning("[OI][t1][inv] 回读异常：" + eRb.GetType().Name); }
                        }
                    }
                    catch (Exception eInv) { _log.LogWarning("[OI][t1][inv] 置开工标记异常：" + eInv.GetType().Name + " — " + Short(eInv.Message)); }
                    _log.LogInfo("[OI][t1][clickin] 已放行 ⇒ 剧情由游戏自己推进 ✓（★ 变量已是 true，游戏按「两个项目都开工」算 ✓）");
                    return true;
                }
                _log.LogInfo("[OI][t1][clickin] 非我们的项 ⇒ 放行 ✓（文本=" + Short(txt) + "）");
                return true;
            }
            catch (Exception e) { _log.LogWarning("[OI][t1][clickin] 异常（放行）：" + e.GetType().Name); return true; }
        }
        
        
        
        
        
        private static void Diag(string msg)
        {
            try { if (CfgT1("Diag", "false") == "true") _log.LogInfo(msg); }
            catch { }
        }

        
        
        private static string _t1AskLastKey = "";
        
        private static string _t1AskTryKey = "";
        private static int _t1AskTry = 0;
        
        private static string _t1AskCreatedKey = "";
        
        private static string _t1AskNodeKey = "";
        private static object _t1AskNode = null;
        
        private static string _t1AskDumpKey = "";

        
        
        private static string EntryField(object entry, string title)
        {
            try
            {
                
                
                var fld = entry == null ? null : entry.GetType().GetField("fields",
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (fld == null) return null;
                object fs = fld.GetValue(entry);
                int n = ListCount(fs);
                for (int i = 0; i < n; i++)
                {
                    object f = IndexN(fs, i);
                    if (f == null) continue;
                    string t = Convert.ToString(Reflect.Prop(f, "title"));
                    if (!string.Equals(t, title, StringComparison.OrdinalIgnoreCase)) continue;
                    return Convert.ToString(Reflect.Prop(f, "value"));
                }
            }
            catch { }
            return null;
        }

        
        
        
        
        
        
        private static object FindTargetEntry(object db, object conv, int convId, string askKey, out string how)
        {
            how = "none";
            try
            {
                object list = Reflect.Prop(conv, "dialogueEntries");
                int n = ListCount(list);
                bool dump = (_t1AskDumpKey != askKey);
                if (dump) { _t1AskDumpKey = askKey; _log.LogInfo("[OI][t1ask][dump] 会话条目清单：共 " + n + " 条 ✓（最多列 40 条 ✓）"); }
                object byText = null;
                for (int i = 0; i < n; i++)
                {
                    object e = IndexN(list, i);
                    if (e == null) continue;
                    string aid = EntryField(e, "Articy Id");
                    string tx = Convert.ToString(Reflect.Prop(e, "currentMenuText"));
                    if (string.IsNullOrEmpty(tx)) tx = Convert.ToString(Reflect.Prop(e, "currentDialogueText"));
                    if (dump && i < 40)
                    {
                        _log.LogInfo("[OI][t1ask][dump]   [" + i + "] id=" + Convert.ToString(Reflect.Prop(e, "id"))
                            + " articy=" + (string.IsNullOrEmpty(aid) ? "-" : aid)
                            + " actor=" + Convert.ToString(Reflect.Prop(e, "ActorID"))
                            + " 文本=" + Short(tx));
                    }
                    if (byText == null && !string.IsNullOrEmpty(tx) && tx.IndexOf("我已经决定了。", StringComparison.Ordinal) >= 0) byText = e;
                    if (!string.IsNullOrEmpty(aid) && string.Equals(aid.Trim(), "0x010000000002EF9A", StringComparison.OrdinalIgnoreCase))
                    {
                        how = "Articy Id 字段命中（" + aid.Trim() + " ✓）";
                        return e;
                    }
                }
                if (byText != null) { how = "文本命中「我已经决定了。」✓"; return byText; }
                var gde = db == null ? null : db.GetType().GetMethod("GetDialogueEntry", new[] { typeof(int), typeof(int) });
                object t3 = (gde == null || convId < 0) ? null : gde.Invoke(db, new object[] { convId, 192410 });
                if (t3 != null) { how = "GetDialogueEntry(convId,192410) ✓（低 32 位那条 ✓）"; return t3; }
            }
            catch (Exception e) { how = "异常 " + e.GetType().Name; }
            return null;
        }

        
        
        private static void T1AskClick()
        {
            try { _log.LogInfo("[OI][t1ask] ★ 我们的第三项被点击（回调进来了 ✓）⇒ 跳转交给游戏自己的 OnResponseClick ✓"); }
            catch { }
        }

        
        
        
        
        
        
        private static object ResolveDb(out string how)
        {
            how = "none";
            try
            {
                var dmT = ResolveType("PixelCrushers.DialogueSystem.DialogueManager") ?? ResolveTypeSimple("DialogueManager");
                if (dmT != null)
                {
                    
                    object mdb = null;
                    try { mdb = Reflect.Static(dmT, "MasterDatabase", "masterDatabase"); } catch { }
                    
                    if (mdb == null)
                    {
                        try
                        {
                            var pm = dmT.GetMethod("PreloadMasterDatabase", BindingFlags.Public | BindingFlags.Static, null, Type.EmptyTypes, null);
                            if (pm != null) { pm.Invoke(null, null); mdb = Reflect.Static(dmT, "MasterDatabase", "masterDatabase"); }
                        }
                        catch { }
                    }
                    if (Reflect.Usable(mdb)) { how = "DialogueManager.MasterDatabase(static)"; return mdb; }
                    
                    try
                    {
                        object dbm = null;
                        var gdm = dmT.GetMethod("get_DatabaseManager", BindingFlags.Public | BindingFlags.Static, null, Type.EmptyTypes, null);
                        if (gdm != null) dbm = gdm.Invoke(null, null);
                        if (Reflect.Usable(dbm))
                        {
                            foreach (var pn in new[] { "database", "masterDatabase" })
                            {
                                var v = Reflect.Prop(dbm, pn);
                                if (Reflect.Usable(v)) { how = "DialogueManager.DatabaseManager." + pn; return v; }
                            }
                        }
                    }
                    catch { }
                    
                    try
                    {
                        object inst = null;
                        var gi = dmT.GetMethod("get_Instance", BindingFlags.Public | BindingFlags.Static, null, Type.EmptyTypes, null);
                        if (gi != null) inst = gi.Invoke(null, null);
                        if (Reflect.Usable(inst))
                        {
                            foreach (var pn in new[] { "database", "masterDatabase", "get_database", "get_masterDatabase" })
                            {
                                var v = Reflect.Prop(inst, pn);
                                if (Reflect.Usable(v)) { how = "DialogueManager.Instance." + pn; return v; }
                            }
                        }
                    }
                    catch { }
                }
                
                var cT = ResolveType("PixelCrushers.DialogueSystem.DialogueSystemController") ?? ResolveTypeSimple("DialogueSystemController");
                if (cT != null)
                {
                    object ci = null;
                    try { ci = Reflect.Static(cT, "m_instance"); } catch { }
                    if (Reflect.Usable(ci))
                    {
                        object dm4 = null;
                        try { var g4 = ci.GetType().GetMethod("get_databaseManager", Type.EmptyTypes); if (g4 != null) dm4 = g4.Invoke(ci, null); } catch { }
                        if (Reflect.Usable(dm4))
                        {
                            foreach (var pn in new[] { "database", "masterDatabase" })
                            {
                                var v = Reflect.Prop(dm4, pn);
                                if (Reflect.Usable(v)) { how = "m_instance.databaseManager." + pn; return v; }
                            }
                        }
                        foreach (var pn in new[] { "database", "masterDatabase" })
                        {
                            var v = Reflect.Prop(ci, pn);
                            if (Reflect.Usable(v)) { how = "m_instance." + pn; return v; }
                        }
                    }
                }
            }
            catch (Exception e) { how = "异常 " + e.GetType().Name; }
            return null;
        }

        
        
        
        
        
        private static bool Prefix_T1Ask(object __instance, ref object __0)
        {
            try
            {
                if (EconomyConfig.T1Both == null || !EconomyConfig.T1BothOn) return true;
                if (CfgT1("T1AskEnabled", "false") != "true") return true;
                string tok = CfgT1("T1AskToken", "");
                if (string.IsNullOrEmpty(tok)) return true;
                if (__0 == null) return true;

                
                
                
                object lenAll = Reflect.Prop(__0, "Length");
                int nAll = lenAll == null ? -1 : Convert.ToInt32(lenAll);
                if (nAll <= 0) return true;
                int hitAt = -1;
                var sbItems = new System.Text.StringBuilder();
                for (int i = 0; i < nAll; i++)
                {
                    string it = ItemText(__0, i);
                    sbItems.Append("[").Append(i).Append("]").Append(Short(it)).Append(" ｜ ");
                    if (hitAt < 0 && !string.IsNullOrEmpty(it) && it.IndexOf(tok, StringComparison.OrdinalIgnoreCase) >= 0) hitAt = i;
                }
                if (hitAt < 0) return true;
                
                
                if (InScopeFirst2(__0)) { _log.LogInfo("[OI][t1ask] Token 命中，但这一屏是【阶段一】决策屏 ⇒ 阶段二不动手 ✓（判据=[" + tok + "]）"); return true; }
                _log.LogInfo("[OI][t1ask] ★ 阶段二判据命中（判据=[" + tok + "] 落在第 " + hitAt + " 项 ✓ · 共 " + nAll + " 项 · 首项=" + Short(ItemText(__0, 0)) + "）：" + sbItems.ToString());

                
                
                
                
                string askKey = (__instance == null ? "?" : __instance.GetHashCode().ToString()) + "|" + nAll + "|" + Short(ItemText(__0, 0));
                if (_t1AskTryKey == askKey) _t1AskTry++; else { _t1AskTryKey = askKey; _t1AskTry = 1; }

                string ourText = CfgT1("T1AskOption", "如果我们设法追加预算，能否同时启动两个项目？");
                int haveAt = -1;
                for (int i = 0; i < nAll; i++)
                {
                    string it = ItemText(__0, i);
                    if (!string.IsNullOrEmpty(it) && !string.IsNullOrEmpty(ourText) && it.IndexOf(ourText, StringComparison.Ordinal) >= 0) { haveAt = i; break; }
                }
                bool freshUi = (_t1AskCreatedKey != askKey);   
                _log.LogInfo("[OI][t1ask] 第 " + _t1AskTry + " 次调用：数组里" + (haveAt >= 0
                    ? "【已有】我们的项（第 " + haveAt + " 项 ✓）"
                    : "【没有】我们的项 ✗ ⇒ 补上 ✓")
                    + "（共 " + nAll + " 项）｜ 本屏建过 UI 项=" + (!freshUi)
                    + " ｜ 数组写过=" + (_t1AskLastKey == askKey) + " ✓");
                if (haveAt >= 0 && !freshUi) { _log.LogInfo("[OI][t1ask] 数组已含 ＋ UI 项已建过 ⇒ 本屏什么都不做 ✓"); return true; }

                object tplResp = IndexN(__0, 0);            
                object hitResp = IndexN(__0, hitAt);
                object srcEntry = Reflect.Prop(hitResp != null ? hitResp : tplResp, "destinationEntry");
                if (tplResp == null || srcEntry == null) { _log.LogWarning("[OI][t1ask] 模板项或源条目缺 ⇒ 放弃 ✗"); return true; }
                
                
                
                
                
                
                
                string simonBase = CfgT1("T1AskSimon", "“事实上，当然可以，但那样一来所付出的预算很可能将同我们所需的成果不匹配。”");
                string simonEcon = CfgT1("T1AskSimonEcon", "“这很有魄力。”");
                int simonGap = 1;
                try { if (EconomyConfig.T1AskSimonEconGap != null) simonGap = EconomyConfig.T1AskSimonEconGap.Value; } catch { }
                if (simonGap < 0) simonGap = 0; if (simonGap > 3) simonGap = 3;
                bool eduEcon = false;
                try { eduEcon = IsTrue("BaseGame.Prologue_Studyplace_Lachaven_Economics"); } catch { }
                int eduNum = -99;
                try { eduNum = ReadIntVar("BaseGame.Education", -1, 8); } catch { }
                string simonText = (eduEcon && !string.IsNullOrEmpty(simonEcon))
                    ? (simonEcon + "\n" + new string('\n', simonGap) + simonBase)   
                    : simonBase;
                _log.LogInfo("[OI][edu] 经济学=" + (eduEcon ? "有" : "无")
                    + "（判据=BaseGame.Prologue_Studyplace_Lachaven_Economics ✓；旁证 BaseGame.Education=" + (eduNum == int.MinValue ? "读不到" : eduNum.ToString()) + " ✓）⇒ 西蒙用 "
                    + ((eduEcon && !string.IsNullOrEmpty(simonEcon)) ? "「" + simonEcon + "」＋ " + simonGap + " 个空行 ＋ 原句" : "原句")
                    + " ✓（★ 改 cfg [T1Ask] SimonEcon / SimonEconGap 即可 ✓）");
                
                string howDbAsk = "none";
                object db = ResolveDb(out howDbAsk);
                _log.LogInfo("[OI][t1ask] 取库 ⇒ " + (db == null
                    ? "取不到 ✗（静态口 ＋ Preload ＋ DatabaseManager ＋ Instance ＋ 控制器 都试过 ✓ ⇒ 本屏下一次调用再试 ✓）"
                    : "有 ✓ via " + howDbAsk));
                if (db == null) return true;   
                object convIdO = Reflect.Prop(srcEntry, "conversationID");
                int convId = convIdO == null ? -1 : Convert.ToInt32(convIdO);
                var gconv = db.GetType().GetMethod("GetConversation", new[] { typeof(int) });
                object conv = (gconv == null || convId < 0) ? null : gconv.Invoke(db, new object[] { convId });
                _log.LogInfo("[OI][t1ask] db=有 ✓ convId=" + convId + " Conversation=" + (conv == null ? "取不到 ✗" : "有 ✓"));
                if (conv == null) return true;
                
                object simonNode = (_t1AskNodeKey == askKey) ? _t1AskNode : null;
                if (simonNode != null) _log.LogInfo("[OI][t1ask] 复用本屏先前建好的节点 ✓ id=" + Convert.ToString(Reflect.Prop(simonNode, "id")));
                else
                {
                    var anm2 = conv.GetType().GetMethod("AddNewDialogueEntry");
                    if (anm2 != null) { try { simonNode = anm2.Invoke(conv, new object[] { srcEntry, simonText, 0, false }); } catch (Exception eA2) { _log.LogWarning("[OI][t1ask] AddNewDialogueEntry 异常：" + eA2.GetType().Name); } }
                    if (simonNode == null) { _log.LogWarning("[OI][t1ask] 新节点登记失败 ⇒ 放弃 ✗"); return true; }
                    _t1AskNodeKey = askKey; _t1AskNode = simonNode;
                }
                Reflect.SetPropOrField(simonNode, "ActorID", 51);
                Reflect.SetPropOrField(simonNode, "ConversantID", 5);
                Reflect.SetPropOrField(simonNode, "currentDialogueText", simonText);
                Reflect.SetPropOrField(simonNode, "currentLocalizedDialogueText", simonText);
                Reflect.SetPropOrField(simonNode, "currentMenuText", simonText);
                Reflect.SetPropOrField(simonNode, "currentLocalizedMenuText", simonText);
                _log.LogInfo("[OI][t1ask] ★ 西蒙节点已登记：id=" + Convert.ToString(Reflect.Prop(simonNode, "id")) + " 文本=" + Short(simonText) + " ✓");
                
                
                
                try
                {
                    string cnd0 = Convert.ToString(Reflect.Prop(simonNode, "conditionsString"));
                    if (!string.IsNullOrEmpty(cnd0)) { bool okC = Reflect.SetPropOrField(simonNode, "conditionsString", ""); _log.LogInfo("[OI][t1ask] 新节点清掉继承的条件：" + okC + "（★ 只动我们这条 ✓）"); }
                    bool okS1 = Reflect.SetPropOrField(simonNode, "Sequence", "Continue()");
                    bool okS2 = Reflect.SetPropOrField(simonNode, "currentSequence", "Continue()");
                    _log.LogInfo("[OI][t1ask] 新节点行为 = Continue()（Sequence 写=" + okS1 + " · currentSequence 写=" + okS2 + "）✓");
                }
                catch (Exception eCln) { _log.LogWarning("[OI][t1ask] 清条件/设行为异常：" + eCln.GetType().Name); }
                
                string howTarget = "none";
                object target = FindTargetEntry(db, conv, convId, askKey, out howTarget);
                _log.LogInfo("[OI][t1ask] 目标节点（我已经决定了。）=" + (target == null
                    ? "取不到 ✗（Articy Id / 文本 / int 三种都试过 ✓ —— 见上面的条目清单 ✓）"
                    : "有 ✓ id=" + Convert.ToString(Reflect.Prop(target, "id")) + " via " + howTarget));
                object lnk = null;
                var clm = simonNode.GetType().GetMethod("CopyLinks");
                object srcLinks = Reflect.Prop(srcEntry, "outgoingLinks");
                if (clm != null && srcLinks != null) { try { lnk = clm.Invoke(simonNode, new object[] { srcLinks }); } catch { } }
                if (lnk != null && target != null)
                {
                    object f0 = IndexN(lnk, 0);
                    if (f0 != null)
                    {
                        Reflect.SetPropOrField(f0, "destinationConversationID", Reflect.Prop(target, "conversationID"));
                        Reflect.SetPropOrField(f0, "destinationDialogueID", Reflect.Prop(target, "id"));
                        Reflect.SetPropOrField(simonNode, "outgoingLinks", lnk);
                        _log.LogInfo("[OI][t1ask] ★ 节点链接 ⇒ 指向「我已经决定了。」（id=" + Convert.ToString(Reflect.Prop(target, "id")) + "）✓");
                    }
                }
                else _log.LogWarning("[OI][t1ask] 链接或目标缺 ⇒ 节点已建但不会跳转 ✗（★ 不影响阶段一 ✓）");
                object ours = null;
                try
                {
                    object ftOrig = Reflect.Prop(tplResp, "formattedText");
                    ours = Activator.CreateInstance(tplResp.GetType(), new object[] { ftOrig, srcEntry, true });
                    object ftNew = null;
                    if (ftOrig != null)
                    {
                        var parseM = ftOrig.GetType().GetMethod("Parse", BindingFlags.Public | BindingFlags.Static, null, new[] { typeof(string), (ResolveTypeSimple("Il2CppReferenceArray`1") ?? ftOrig.GetType()) }, null);
                        if (parseM != null) { try { ftNew = parseM.Invoke(null, new object[] { ourText, null }); } catch { } }
                        if (ftNew == null) { foreach (var mm in ftOrig.GetType().GetMethods(BindingFlags.Public | BindingFlags.Static)) { if (mm.Name != "Parse") continue; var ps = mm.GetParameters(); if (ps.Length == 2 && ps[0].ParameterType == typeof(string)) { try { ftNew = mm.Invoke(null, new object[] { ourText, null }); } catch { } break; } } }
                        if (ftNew != null) { Reflect.SetPropOrField(ftNew, "text", ourText); Reflect.SetPropOrField(ours, "formattedText", ftNew); }
                    }
                    if (ftNew == null) _log.LogWarning("[OI][t1ask] 新文本对象未造出 ⇒ 选项将沿用首项文字 ✗");
                }
                catch (Exception eO) { _log.LogWarning("[OI][t1ask] 造响应异常：" + eO.GetType().Name); }
                if (ours == null) return true;
                if (simonNode != null) Reflect.SetPropOrField(ours, "destinationEntry", simonNode);
                object lenO = Reflect.Prop(__0, "Length");
                int n = lenO == null ? -1 : Convert.ToInt32(lenO);
                if (n < 0) { _log.LogWarning("[OI][t1ask] Length 读不到 ⇒ 不追加（★ 本屏下一次调用会再试 ✓）"); return true; }

                
                if (haveAt < 0)
                {
                    object newArr = null;
                    try { newArr = Activator.CreateInstance(__0.GetType(), new object[] { (long)(n + 1) }); } catch { }
                    if (newArr == null) { _log.LogWarning("[OI][t1ask] 造更大的数组失败 ⇒ 未追加（★ 本屏下一次调用会再试 ✓）"); return true; }
                    var setI = __0.GetType().GetMethod("set_Item", new[] { typeof(int), tplResp.GetType() });
                    if (setI == null) { _log.LogWarning("[OI][t1ask] set_Item 未找到 ⇒ 未追加（★ 本屏下一次调用会再试 ✓）"); return true; }
                    for (int i = 0; i < n; i++) { setI.Invoke(newArr, new object[] { i, IndexN(__0, i) }); }
                    setI.Invoke(newArr, new object[] { n, ours });
                    __0 = newArr;
                    _t1AskLastKey = askKey;
                    _log.LogInfo("[OI][t1ask] ★ 已把我们的项写进数组：原长=" + n + " 新长=" + Convert.ToString(Reflect.Prop(__0, "Length")) + " ✓（第 " + _t1AskTry + " 次调用 ✓）");
                }
                else _log.LogInfo("[OI][t1ask] 这一次的数组里已经有我们的项 ⇒ 不重复写数组 ✓");

                
                
                
                if (freshUi)
                {
                    object cont = Reflect.Prop(__instance, "responseContainer");
                    _log.LogInfo("[OI][t1ask] CreateResponse 之前：容器 child=" + ChildCount(cont)
                        + " ｜ instantiatedResponses=" + ListCount(Reflect.Prop(__instance, "instantiatedResponses")));
                    try
                    {
                        var cr = __instance.GetType().GetMethod("CreateResponse", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                        string cbHow; object cb = BuildUnityActionFor(typeof(EconomyPatches).GetMethod("T1AskClick", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public), out cbHow);
                        if (cr != null && cr.GetParameters().Length == 3)
                        {
                            cr.Invoke(__instance, new object[] { ours, n, cb });
                            _t1AskCreatedKey = askKey;   
                            _log.LogInfo("[OI][t1ask] ★★ 已调【游戏自己的】CreateResponse(index=" + n + " · 回调=" + cbHow + "）✓ 之后：容器 child="
                                + ChildCount(cont) + " ｜ instantiatedResponses=" + ListCount(Reflect.Prop(__instance, "instantiatedResponses")) + " ✓");
                        }
                        else _log.LogWarning("[OI][t1ask] CreateResponse 不可用（cr=" + (cr != null) + "）⇒ 只靠数组那条路 ✓");
                    }
                    catch (Exception eCr) { _log.LogWarning("[OI][t1ask] CreateResponse 异常：" + eCr.GetType().Name + " — " + Short(eCr.Message)); }
                }
                return true;
            }
            catch (Exception eAsk) { _log.LogWarning("[OI][t1ask] 框架异常（放行）：" + eAsk.GetType().Name + " — " + Short(eAsk.Message)); return true; }
        }
        private static void T1BothClick()
        {
            try
            {
                string[] ins =
                {
                    "Variable[\"BaseGameSupport.Journal_Turn01_PickedHighway\"] = true;",
                    "Variable[\"BaseGameSupport.Journal_Turn01_PickedRailway\"] = true;"
                };
                var fi = typeof(EconomyPatches).GetField("_runInstant", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
                var run = fi == null ? null : (MethodInfo)fi.GetValue(null);
                if (run == null) { _log.LogWarning("[OI][t1][click] ★ 被点击，但 `_runInstant` 取不到 ⇒ **未执行指令** ✗（如实报告）"); return; }
                for (int i = 0; i < ins.Length; i++)
                {
                    try { run.Invoke(null, new object[] { ins[i] }); _log.LogInfo("[OI][t1][click] 已执行：" + ins[i]); }
                    catch (Exception e) { _log.LogWarning("[OI][t1][click] 执行失败 ✗：" + ins[i] + " — " + e.GetType().Name); }
                }
                try
                {
                    bool hw = IsTrue("BaseGameSupport.Journal_Turn01_PickedHighway");
                    bool rw = IsTrue("BaseGameSupport.Journal_Turn01_PickedRailway");
                    _log.LogInfo("[OI][t1][click] 回读 ⇒ Highway=" + hw + " Railway=" + rw
                        + "（★ 两者皆 true ⇒ 变量已写入 ✓；★ 未证④：**游戏认不认账**仍要另看 ✓）");
                }
                catch { }
            }
            catch { }
        }
    }
}
