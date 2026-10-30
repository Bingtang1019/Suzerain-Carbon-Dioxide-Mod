using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace SuzerainTrainer
{
    
    partial class MainForm
    {
            bool SyncChangesToFile(string path)
            {
                TrainerCore t = new TrainerCore();
                t.AllowEntityWrites = core.AllowEntityWrites;
                if (!t.LoadFile(path)) return false;
                foreach (TrainerCore.ChangeRec c in core.Changes)
                {
                    if (c.Kind == "var") t.SetVar(c.Key, c.NewValue);
                    else if (c.Kind == "bool") t.SetBoolVar(c.Key, c.NewValue);
                    else if (c.Kind == "string") t.SetStringVar(c.Key, c.NewValue);
                    else if (c.Kind == "entity") t.SetEntityValue(c.Key, c.Field, c.NewValue);
                }
                return t.Save();
            }

            
            TabPage MakeTab(string title, Control body)
            {
                TabPage tp = new TabPage(title);
                tp.BackColor = Color.White;   
                tp.AutoScroll = false;
                TableLayoutPanel lt = new TableLayoutPanel();
                lt.Dock = DockStyle.Fill;
                lt.ColumnCount = 1; lt.RowCount = 2;
                lt.Margin = new Padding(0); lt.Padding = new Padding(0);
                lt.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
                lt.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
                
                Panel header = new Panel(); header.Dock = DockStyle.Fill; header.BackColor = Color.White; header.Margin = new Padding(0);
                Label hl = new Label();
                hl.Text = title;
                hl.Font = new Font(Font.FontFamily, 13F, FontStyle.Bold);
                hl.ForeColor = Color.FromArgb(15, 45, 90);
                hl.Left = 18; hl.Top = 10; hl.AutoSize = true;
                Panel line = new Panel(); line.Dock = DockStyle.Bottom; line.Height = 1; line.BackColor = Color.FromArgb(224, 224, 224);
                header.Controls.Add(hl);
                header.Controls.Add(line);
                lt.Controls.Add(header, 0, 0);
                body.Dock = DockStyle.Fill;
                lt.Controls.Add(body, 0, 1);
                tp.Controls.Add(lt);
                return tp;
            }


            
            static string TransWord(string w)
            {
                if (w.Length == 0) return w;
                string v;
                if (TranslateDict.Words.TryGetValue(w, out v)) return v;
                string[] camel = Regex.Split(w, "(?<=[a-z0-9])(?=[A-Z])");
                if (camel.Length == 1) return w;
                for (int i = 0; i < camel.Length; i++)
                    if (TranslateDict.Words.TryGetValue(camel[i], out v)) camel[i] = v;
                return string.Join("", camel);
            }

            static string TranslateVarName(string key)
            {
                string ex;
                if (TranslateDict.Exact.TryGetValue(key, out ex)) return ex;
                string[] parts = key.Split('.');
                string head;
                if (!TranslateDict.Prefixes.TryGetValue(parts[0], out head)) head = parts[0];
                for (int i = 1; i < parts.Length; i++)
                {
                    string[] segs = parts[i].Split('_');
                    for (int j = 0; j < segs.Length; j++) segs[j] = TransWord(segs[j]);
                    head += "." + string.Join("_", segs);
                }
                return head;
            }

            string DisplayName(string raw)
            {
                return ShowRaw ? raw : TranslateVarName(raw);
            }

            
            
            Font uiBoldFont;

            
            System.Drawing.Icon TabIcon(int index)
            {
                switch (index)
                {
                    case 0: return SystemIcons.Application;   
                    case 1: return SystemIcons.Shield;        
                    case 2: return SystemIcons.Hand;          
                    case 3: return SystemIcons.Information;   
                    case 4: return SystemIcons.Exclamation;   
                    case 5: return SystemIcons.WinLogo;       
                    default: return SystemIcons.Information;  
                }
            }

            void TabsDrawItem(object sender, DrawItemEventArgs e)
            {
                TabControl tc = (TabControl)sender;
                Rectangle r = e.Bounds;
                Graphics g = e.Graphics;
                bool selected = e.Index == tc.SelectedIndex;
                if (uiBoldFont == null) uiBoldFont = new Font(Font, FontStyle.Bold);
                Font f = selected ? uiBoldFont : Font;
                string text = tc.TabPages[e.Index].Text;
                
                if (selected)
                {
                    using (SolidBrush bg = new SolidBrush(Color.White))
                        g.FillRectangle(bg, r.X, r.Y, r.Width, r.Height);
                    using (SolidBrush accent = new SolidBrush(Color.FromArgb(0, 103, 192)))
                        g.FillRectangle(accent, r.X, r.Y, r.Width, 3);
                    using (SolidBrush white = new SolidBrush(Color.White))
                        g.FillRectangle(white, r.X, r.Bottom - 2, r.Width, 2);
                }
                else
                {
                    using (SolidBrush bg = new SolidBrush(Color.FromArgb(242, 242, 242)))
                        g.FillRectangle(bg, r.X, r.Y, r.Width, r.Height);
                    using (Pen pen = new Pen(Color.FromArgb(214, 214, 214)))
                        g.DrawLine(pen, r.Right - 1, r.Y + 6, r.Right - 1, r.Bottom - 6);
                }
                
                int isz = 16;
                float tw = g.MeasureString(text, f).Width;
                float totalW = isz + 6 + tw;
                float left = r.X + (r.Width - totalW) / 2;
                if (left < r.X + 4) left = r.X + 4;
                Icon icon = TabIcon(e.Index);
                if (icon != null)
                {
                    using (icon)
                        g.DrawIcon(icon, new Rectangle((int)left, r.Y + (r.Height - isz) / 2, isz, isz));
                }
                RectangleF textRect = new RectangleF(left + isz + 6, r.Y, r.Width - isz - 10, r.Height);
                using (StringFormat sf = new StringFormat())
                {
                    sf.LineAlignment = StringAlignment.Center;
                    sf.Alignment = StringAlignment.Near;
                    sf.Trimming = StringTrimming.EllipsisCharacter;
                    using (SolidBrush br = new SolidBrush(selected ? Color.FromArgb(0, 60, 120) : Color.FromArgb(72, 72, 72)))
                        g.DrawString(text, f, br, textRect, sf);
                }
            }

            
            internal string DumpStats()
            {
                string s = "loaded=" + core.loaded + " story=" + core.storyMode + " cur=" + (core.curFile ?? "null") + " vars=" + (core.json == null ? 0 : core.json.Length) + "\n";
                if (fpDiplo != null) s += "diplo=" + fpDiplo.lv.Items.Count + "\n";
                if (fpRelation != null) s += "relation=" + fpRelation.lv.Items.Count + "\n";
                if (fpAll != null) s += "all=" + fpAll.lv.Items.Count + "\n";
                if (fpDecree != null) s += "decree=" + fpDecree.lv.Items.Count + "\n";
                if (lvEvents != null) s += "events=" + lvEvents.Items.Count + "\n";
                if (lvModifier != null) s += "modifier=" + lvModifier.Items.Count + "\n";
                
                try
                {
                    List<string> parts = new List<string>();
                    foreach (TreeNode n in navTree.Nodes) parts.Add(n.Text + "(" + n.Nodes.Count + ")");
                    s += "nav=" + string.Join(" | ", parts.ToArray()) + "\n";
                }
                catch (Exception e) { s += "nav=err:" + e.Message + "\n"; }
                s += "hb_installed=" + HbBridge.Installed() + " hb_running=" + HbBridge.Running()
                   + " hb_version=" + HbBridge.State("modVersion") + " hb_page=" + (pageHb != null) + "\n";
                s += "oi_installed=" + OiBridge.Installed() + " oi_running=" + OiBridge.Running()
                   + " oi_version=" + OiBridge.State("modVersion") + " oi_page=" + (pageOi != null) + "\n";
                return s;
            }

            
            static readonly Color E_Bg = Color.FromArgb(243, 245, 248);
            static readonly Color E_Card = Color.White;
            static readonly Color E_Border = Color.FromArgb(227, 232, 239);
            static readonly Color E_Text = Color.FromArgb(27, 36, 48);
            static readonly Color E_Muted = Color.FromArgb(107, 119, 135);
            static readonly Color E_Accent = Color.FromArgb(47, 111, 237);
            static readonly Color E_Select = Color.FromArgb(241, 245, 254);

            void UiThemeRecursive(Control root)
            {
                if (root == null) return;
                foreach (Control c in root.Controls)
                {
                    if (c is SplitContainer)
                    {
                        var sc = (SplitContainer)c;
                        sc.Panel1.BackColor = E_Card;
                        sc.Panel2.BackColor = E_Bg;
                    }
                    else if (c is Panel || c is GroupBox || c is TabPage)
                    {
                        c.BackColor = E_Card;
                        if (c is GroupBox) c.ForeColor = E_Text;
                    }
                    else if (c is TabControl)
                    {
                        var tc = (TabControl)c;
                        tc.BackColor = E_Card;
                        tc.DrawMode = TabDrawMode.OwnerDrawFixed;
                        if (!(tc.Tag as string == "ui-tabs"))
                        {
                            tc.Tag = "ui-tabs";
                            tc.DrawItem += UiDrawTab;
                        }
                    }
                    else if (c is ListView)
                    {
                        var lv = (ListView)c;
                        lv.BackColor = E_Card;
                        lv.ForeColor = E_Text;
                        lv.BorderStyle = BorderStyle.None;
                        lv.GridLines = false;
                    }
                    else if (c is Button)
                    {
                        var b = (Button)c;
                        bool primary = string.Equals(b.Tag as string, "primary", StringComparison.Ordinal);
                        b.FlatStyle = FlatStyle.Flat;
                        b.FlatAppearance.BorderSize = 1;
                        b.FlatAppearance.BorderColor = primary ? E_Accent : E_Border;
                        b.BackColor = primary ? E_Accent : E_Card;
                        b.ForeColor = primary ? Color.White : E_Text;
                    }
                    else if (c is TextBoxBase)
                    {
                        c.BackColor = E_Card;
                        c.ForeColor = E_Text;
                        var tb = c as TextBox;
                        if (tb != null) tb.BorderStyle = BorderStyle.FixedSingle;
                    }
                    else if (c is ComboBox || c is NumericUpDown)
                    {
                        c.BackColor = E_Card;
                        c.ForeColor = E_Text;
                    }
                    UiThemeRecursive(c);
                }
            }

            void UiDrawTreeNode(object sender, DrawTreeNodeEventArgs e)
            {
                if (e.Node == null) return;
                bool sel = (e.State & TreeNodeStates.Selected) != 0;
                Rectangle r = e.Bounds;
                using (var b = new SolidBrush(sel ? E_Select : E_Card)) e.Graphics.FillRectangle(b, r);
                if (sel)
                    using (var a = new SolidBrush(E_Accent)) e.Graphics.FillRectangle(a, r.Left, r.Top, 3, r.Height);
                int left = r.Left + 4 + e.Node.Level * 18;
                if (e.Node.Nodes.Count > 0)
                {
                    int cy = r.Top + r.Height / 2;
                    Point[] tri = e.Node.IsExpanded
                        ? new[] { new Point(left, cy - 3), new Point(left + 8, cy - 3), new Point(left + 4, cy + 3) }
                        : new[] { new Point(left + 1, cy - 4), new Point(left + 7, cy), new Point(left + 1, cy + 4) };
                    using (var s = new SolidBrush(E_Muted)) e.Graphics.FillPolygon(s, tri);
                }
                Rectangle tr = new Rectangle(left + 14, r.Top, Math.Max(1, r.Right - left - 16), r.Height);
                Color col = sel ? E_Accent : E_Text;
                TextRenderer.DrawText(e.Graphics, e.Node.Text, e.Node.NodeFont ?? e.Node.TreeView.Font,
                    tr, col, TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
            }

            void UiDrawTab(object sender, DrawItemEventArgs e)
            {
                var tc = sender as TabControl;
                if (tc == null || e.Index < 0) return;
                bool sel = e.Index == tc.SelectedIndex;
                using (var b = new SolidBrush(sel ? E_Card : E_Bg)) e.Graphics.FillRectangle(b, e.Bounds);
                if (sel)
                    using (var a = new SolidBrush(E_Accent)) e.Graphics.FillRectangle(a, e.Bounds.Left, e.Bounds.Top, e.Bounds.Width, 3);
                TextRenderer.DrawText(e.Graphics, tc.TabPages[e.Index].Text, tc.Font, e.Bounds,
                    sel ? E_Accent : E_Muted, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            }

            void UiPaintBorder(object sender, PaintEventArgs e)
            {
                var c = sender as Control;
                if (c == null) return;
                using (var p = new Pen(E_Border, 1f))
                    e.Graphics.DrawRectangle(p, 0, 0, c.Width - 1, c.Height - 1);
            }

            void UiApplyShell()
            {
                BackColor = E_Bg;
                UiThemeRecursive(this);
                if (mainSplit != null)
                {
                    mainSplit.Panel1.BackColor = E_Card;
                    mainSplit.Panel2.BackColor = E_Bg;
                }
                if (contentHost != null)
                {
                    contentHost.BackColor = E_Bg;
                    contentHost.Padding = new Padding((int)(10 * dpiScaleF));
                }
                if (navTree != null)
                {
                    navTree.BackColor = E_Card;
                    navTree.ForeColor = E_Text;
                    navTree.BorderStyle = BorderStyle.None;
                    navTree.ShowLines = false;
                    navTree.ItemHeight = (int)(26 * dpiScaleF);
                    navTree.DrawMode = TreeViewDrawMode.OwnerDrawAll;
                    navTree.DrawNode -= UiDrawTreeNode;
                    navTree.DrawNode += UiDrawTreeNode;
                }
                foreach (Control c in Controls)
                {
                    if (c is Panel && !(c is SplitContainer))
                    {
                        c.BackColor = E_Card;
                        c.Paint -= UiPaintBorder;
                        c.Paint += UiPaintBorder;
                    }
                }
            }

            void UiApplyPage(Control page)
            {
                if (page == null) return;
                page.BackColor = E_Card;
                UiThemeRecursive(page);
                if (page.Tag as string != "ui-card")
                {
                    page.Tag = "ui-card";
                    page.Paint += delegate(object s, PaintEventArgs e)
                    {
                        using (var p = new Pen(E_Border, 1f))
                            e.Graphics.DrawRectangle(p, 0, 0, page.Width - 1, page.Height - 1);
                    };
                }
            }

            int Ui(int logicalPixels) { return (int)(logicalPixels * dpiScaleF); }

            void SelectNavPage(Control page)
            {
                if (navTree == null || page == null) return;
                TreeNode found = null;
                TreeNode[] stack = new TreeNode[navTree.Nodes.Count];
                navTree.Nodes.CopyTo(stack, 0);
                for (int i = 0; i < stack.Length && found == null; i++)
                {
                    TreeNode n = stack[i];
                    if (n == null) continue;
                    if (object.ReferenceEquals(n.Tag, page)) { found = n; break; }
                    foreach (TreeNode child in n.Nodes)
                        if (object.ReferenceEquals(child.Tag, page)) { found = child; break; }
                }
                if (found != null) navTree.SelectedNode = found;
            }

            void ShowSearchWorkbench()
            {
                if (pageSearch == null) return;
                SelectNavPage(pageSearch);
                if (txtGlobalSearch != null)
                {
                    txtGlobalSearch.Focus();
                    txtGlobalSearch.SelectAll();
                }
                SetStatus("搜索工作台：输入键名/中文，回车编辑");
            }

            
            
            void ScaleDialog(Form f)
            {
                try { if (dpiScaleF > 1.01f) f.Scale(new SizeF(dpiScaleF, dpiScaleF)); } catch { }
            }

            sealed class ListViewSnapshot
            {
                internal int TopIndex;
                internal string SelectedKey;
            }

            
            ListViewSnapshot CaptureListView(ListView lv)
            {
                var s = new ListViewSnapshot();
                if (lv == null) return s;
                try { s.TopIndex = lv.TopItem == null ? 0 : lv.TopItem.Index; } catch { s.TopIndex = 0; }
                if (lv.SelectedItems.Count > 0)
                {
                    ListViewItem it = lv.SelectedItems[0];
                    s.SelectedKey = it.Tag as string ?? it.Text;
                }
                return s;
            }

            void RestoreListView(ListView lv, ListViewSnapshot s)
            {
                if (lv == null || s == null || lv.Items.Count == 0) return;
                int top = Math.Min(Math.Max(0, s.TopIndex), lv.Items.Count - 1);
                int target = -1;
                if (!string.IsNullOrEmpty(s.SelectedKey))
                {
                    for (int i = 0; i < lv.Items.Count; i++)
                    {
                        string k = lv.Items[i].Tag as string ?? lv.Items[i].Text;
                        if (k == s.SelectedKey) { target = i; break; }
                    }
                }
                int rows = Math.Max(1, lv.ClientSize.Height / Math.Max(1, lv.Font.Height + 4));
                bool wasVisible = target >= top && target < top + rows;
                if (wasVisible || target < 0)
                {
                    try { lv.TopItem = lv.Items[top]; } catch { }
                }
                if (target >= 0)
                {
                    lv.SelectedItems.Clear();
                    lv.Items[target].Selected = true;
                    lv.Items[target].Focused = true;
                    if (!wasVisible) lv.EnsureVisible(target);
                }
            }

            void RefreshAll()
            {
                if (!core.loaded) return;
                RefreshAllValues();
                RefreshFilterPage(fpDiplo);
                RefreshFilterPage(fpRelation);
                RefreshFilterPage(fpAll);
                RefreshFilterPage(fpDecree);
                RefreshEvents();
                RefreshModifier();
                RefreshEntityList();
                RefreshEntity();
                RefreshQuickPanel();
                RefreshNoviceBasePanel();
                RefreshFavoritesPanel();
                RefreshHistoryPanel();
                RefreshSearchChanges();
                RefreshDashboard();
            }

            void RefreshSaveList()
            {
                loadingSaveList = true;
                cmbSave.Items.Clear();
                string dir = TrainerCore.SaveDir();
                if (!Directory.Exists(dir)) { loadingSaveList = false; return; }
                string[] files = Directory.GetFiles(dir, "*.json");
                Array.Sort(files, delegate(string a, string b) { return File.GetLastWriteTime(b).CompareTo(File.GetLastWriteTime(a)); });
                foreach (string f in files)
                {
                    FileInfo fi = new FileInfo(f);
                    cmbSave.Items.Add(fi.Name + "  (" + fi.LastWriteTime.ToString("MM-dd HH:mm") + ", " + (fi.Length / 1024) + "KB)");
                }
                
                if (core.curFile != null)
                {
                    string baseName = Path.GetFileName(core.curFile);
                    for (int i = 0; i < cmbSave.Items.Count; i++)
                        if (cmbSave.Items[i].ToString().StartsWith(baseName)) { cmbSave.SelectedIndex = i; break; }
                }
                loadingSaveList = false;
            }

            void LoadSelected()
            {
                if (cmbSave.SelectedIndex < 0) { SetStatus("请先选择存档"); return; }
                string dir = TrainerCore.SaveDir();
                string name = cmbSave.SelectedItem.ToString();
                name = name.Substring(0, name.IndexOf("  ("));
                string path = Path.Combine(dir, name);
                if (!core.LoadFile(path)) { SetStatus("载入失败"); return; }
                AfterLoad();
                SetStatus("已载入: " + path);
            }

            void AfterLoad()
            {
                
                switch (core.storyMode)
                {
                    case 1: lblStory.Text = "本体"; lblStory.ForeColor = Color.DodgerBlue; break;
                    case 2: lblStory.Text = "DLC"; lblStory.ForeColor = Color.DarkOrange; break;
                    case 3: lblStory.Text = "本体+DLC"; lblStory.ForeColor = Color.ForestGreen; break;
                    default: lblStory.Text = "未知"; lblStory.ForeColor = Color.Gray; break;
                }
                PopulateNav();
                RebuildEventPrefixes();
                RebuildPrefixes(fpDiplo);
                RebuildPrefixes(fpRelation);
                RebuildPrefixes(fpAll);
                RebuildPrefixes(fpDecree);
                RefreshAll();
                RefreshFavoritesPanel();
                RefreshHistoryPanel();
            }

            void RefreshAllValues()
            {
                if (!core.loaded) return;
                for (int i = 0; i < resNums.Count; i++) SetNumFromVar(resKeys[i], resNums[i]);
                SetNumFromVar("RiziaDLC.Resources_Authority", numAuthority);
                SetNumFromVar("RiziaDLC.Resources_Budget", numBudget);
                SetNumFromVar("RiziaDLC.Resources_Energy", numEnergy);
                SetNumFromVar("BaseGameSetup.CurrentTurn", numTurn);
                SetNumFromVar("BaseGame.Reform_Assembly_Vote", numAssembly);
                SetNumFromVar("BaseGame.Reform_Court_Vote", numCourt);
                for (int i = 0; i < hudNums.Count; i++)
                {
                    string k = hudNums[i].Tag as string;
                    if (k != null) SetNumFromVar(k, hudNums[i]);
                }
            }

            void SetNumFromVar(string key, NumericUpDown n)
            {
                string v = core.GetVar(key);
                if (v == null) return;
                decimal d;
                if (decimal.TryParse(v, NumberStyles.Float, CultureInfo.InvariantCulture, out d))
                    n.Value = Math.Max(n.Minimum, Math.Min(n.Maximum, d));
            }

            
            
            
            
            void RunOiSelfTest()
            {
                string report = BuildOiSelfTestReport();
                if (txtOiSelfTest != null)
                {
                    txtOiSelfTest.Text = report;
                    txtOiSelfTest.SelectionStart = 0;
                    txtOiSelfTest.ScrollToCaret();
                    var scHost = txtOiSelfTest.Parent as ScrollableControl;   
                    if (scHost != null) scHost.ScrollControlIntoView(txtOiSelfTest);
                }
                string last = "";
                foreach (var ln in report.Split('\n')) { if (ln.Trim().Length > 0) last = ln.Trim(); }
                SetStatus("桥自检完成 —— 结果见下方文本框（" + last + "）");
            }

            
            string BuildOiSelfTestReport()
            {
                var sb = new StringBuilder();
                int okN = 0, warnN = 0, failN = 0;
                sb.AppendLine("安济艺术 · 桥自检（只读，不会修改任何文件）");
                sb.AppendLine("时间：" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                sb.AppendLine("────────────────────────────────────────────");

                
                string dir = OiBridge.Dir();
                if (Directory.Exists(dir)) { okN++; sb.AppendLine("[OK] 桥目录：" + dir); }
                else { failN++; sb.AppendLine("[!!] 桥目录不存在：" + dir + "（模组从未运行过？）"); }

                
                string sp = OiBridge.StatePath;
                double ageMin = -1;
                if (File.Exists(sp))
                {
                    okN++;
                    var fi = new FileInfo(sp);
                    ageMin = (DateTime.Now - fi.LastWriteTime).TotalMinutes;
                    string when = ageMin < 1 ? "刚刚" : ((int)ageMin) + " 分钟前";
                    sb.AppendLine("[OK] 状态文件：" + fi.Length + " 字节，" + fi.LastWriteTime.ToString("HH:mm:ss") + "（" + when + "）");
                    string mv = OiBridge.State("modVersion");
                    if (!string.IsNullOrEmpty(mv)) { okN++; sb.AppendLine("[OK] 模组版本：" + mv); }
                    else { warnN++; sb.AppendLine("[??] 读不到 modVersion（状态文件格式过旧？）"); }
                    sb.AppendLine("     总统令：启用=" + OiBridge.State("enabled") + "，上限=" + OiBridge.State("maxDecrees")
                        + "，已签署=" + OiBridge.State("enactedCount"));
                    sb.AppendLine("     经济：总开关=" + OiBridge.State("economyEnabled") + "，状态=" + OiBridge.State("economyState")
                        + "，地区=" + OiBridge.State("regionTiers"));
                    string lt = OiBridge.State("lastSettleTurn");
                    if (!string.IsNullOrEmpty(lt) && lt != "0" && lt != "-1")
                    {
                        okN++;
                        sb.AppendLine("[OK] 最近结算：回合 " + lt + "，合计 " + OiBridge.State("lastSettleTotal"));
                        string detail = OiBridge.State("lastSettleDetail");
                        if (!string.IsNullOrEmpty(detail)) sb.AppendLine("     明细：" + detail);
                    }
                    else { warnN++; sb.AppendLine("[??] 尚无结算记录：进游戏结束一回合后应出现"); }
                    if (ageMin > 10) { warnN++; sb.AppendLine("[??] 状态文件已 " + ((int)ageMin) + " 分钟未更新 → 游戏可能未运行"); }
                }
                else { failN++; sb.AppendLine("[!!] 状态文件不存在：" + sp + "（模组未加载或游戏未运行）"); }

                
                string op = OiBridge.OverridePath;
                if (File.Exists(op))
                {
                    okN++;
                    sb.AppendLine("[OK] 开关文件：" + op);
                    sb.AppendLine("     上限=" + OiBridge.OverrideInt("maxDecrees", -999)
                        + "，ED=" + OiBridge.OverrideInt("dangerDelta", -999) + "/" + OiBridge.OverrideInt("unstableDelta", -999)
                        + "/" + OiBridge.OverrideInt("stableDelta", -999) + "/" + OiBridge.OverrideInt("growingDelta", -999)
                        + "，地区步长=" + OiBridge.OverrideInt("regionStepDelta", -999));
                    int tg = OiBridge.OverrideInt("tourismGoodDelta", -999);
                    int td = OiBridge.OverrideInt("tradeDealBonus", -999);
                    sb.AppendLine("     旅游=" + tg + "/" + OiBridge.OverrideInt("tourismAverageDelta", -999) + "/" + OiBridge.OverrideInt("tourismBadDelta", -999)
                        + "，贸易=" + OiBridge.OverrideInt("tradeIncreasedDelta", -999) + "/" + OiBridge.OverrideInt("tradeRecoveredDelta", -999)
                        + "/" + OiBridge.OverrideInt("tradeDecreasedDelta", -999) + "，协议=" + td);
                    if (tg == -999 || td == -999)
                    { warnN++; sb.AppendLine("[??] 开关文件缺少旅游/贸易字段 → 点「应用到模组」重新写入即可"); }
                }
                else { warnN++; sb.AppendLine("[??] 开关文件不存在 → 模组使用自身 cfg 配置（这不是错误）"); }

                
                string mv2 = OiBridge.State("modVersion");
                if (!string.IsNullOrEmpty(mv2))
                {
                    if (mv2.StartsWith("2.3") || mv2.StartsWith("2.4"))
                    { okN++; sb.AppendLine("[OK] 模组版本支持全部机制（旅游 / 贸易 / 贸易协议）"); }
                    else if (mv2.StartsWith("2.2"))
                    { warnN++; sb.AppendLine("[??] 模组 " + mv2 + " 支持旅游，但不识别贸易字段（需 ≥ 2.3.1）"); }
                    else
                    { warnN++; sb.AppendLine("[??] 模组 " + mv2 + " 不识别旅游/贸易 override 字段（需 ≥ 2.3.1）"); }
                }

                sb.AppendLine("────────────────────────────────────────────");
                sb.AppendLine("结果：通过 " + okN + " · 提示 " + warnN + " · 失败 " + failN);
                sb.AppendLine(failN == 0 ? "结论：桥连通正常" : "结论：存在失败项，请按上面 [!!] 处理");
                return sb.ToString();
            }

            void SetStatus(string s)
            {
                lblStatus.Text = s;
            }
    }
}
