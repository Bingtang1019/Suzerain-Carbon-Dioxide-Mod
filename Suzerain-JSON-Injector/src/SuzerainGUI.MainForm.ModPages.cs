using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using System.Threading;

namespace SuzerainTrainer
{
    
    partial class MainForm
    {
            
            
            
            

            
            void RegisterModPages()
            {
                ModPageRegistry.Register(new HbModPage(this));
                ModPageRegistry.Register(new OiModPage(this));
            }

            
            internal sealed class HbModPage : IModPage
            {
                readonly MainForm form;
                Control cached;
                internal HbModPage(MainForm f) { form = f; }
                public string GroupTitle { get { return "HB 模组 (Horrifying Bombardment)"; } }
                public string PageTitle { get { return "模组开关与状态"; } }
                public bool Installed()
                {
                    
                    try { return HbBridge.Installed(); } catch { return false; }
                }
                public Control Build()
                {
                    
                    if (cached == null) cached = form != null ? form.pageHb : null;
                    return cached;
                }
            }

            
            internal sealed class OiModPage : IModPage
            {
                readonly MainForm form;
                Control cached;
                internal OiModPage(MainForm f) { form = f; }
                public string GroupTitle { get { return "安济艺术 (Oikonomia)"; } }
                public string PageTitle { get { return "模组开关与状态"; } }
                public bool Installed()
                {
                    
                    try { return OiBridge.Installed(); } catch { return false; }
                }
                public Control Build()
                {
                    if (cached == null) cached = form != null ? form.pageOi : null;
                    return cached;
                }
            }

            Control BuildBasePanel()
            {
                Panel p = new Panel(); p.Dock = DockStyle.Fill; p.AutoScroll = true;
                TableLayoutPanel t = new TableLayoutPanel();
                t.Dock = DockStyle.Top;
                t.AutoSize = true;
                t.AutoSizeMode = AutoSizeMode.GrowOnly;
                t.ColumnCount = 2;
                t.Padding = new Padding(8, 4, 8, 4);
                t.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
                t.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));

                string[] resKeysA = {
                    "BaseGame.GovernmentBudget", "BaseGame.Economy", "BaseGame.Healthcare", "BaseGame.Education",
                    "BaseGame.PersonalWealth", "BaseGame.Public_Opinion", "BaseGame.Country_Unrest", "BaseGame.Bludish_Opinion",
                    "BaseGame.Malenyevist", "BaseGame.Sollist", "BaseGame.Democratic", "BaseGame.Capitalist" };
                string[] resNames = { "政府预算 [-20~30]", "经济", "医疗", "教育", "个人财富 [0~15]", "公众舆论", "国内动乱", "布尔德舆论", "马林耶夫主义者", "索尔主义者", "民主派", "资本家" };

                
                t.RowStyles.Add(new RowStyle(SizeType.Absolute, 26));
                Label g1 = new Label(); g1.Text = "本体核心资源与光谱"; g1.Font = new Font(Font, FontStyle.Bold); g1.Dock = DockStyle.Fill;
                t.Controls.Add(g1, 0, 0); t.SetColumnSpan(g1, 2);

                
                for (int i = 0; i < resKeysA.Length; i++)
                {
                    int row = 1 + i / 2;
                    if (i % 2 == 0) t.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
                    Panel cell = MakeNumCell(resNames[i], resKeysA[i]);
                    t.Controls.Add(cell, i % 2, row);
                    resKeys.Add(resKeysA[i]);
                }

                
                t.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
                Panel cellBtn = new Panel(); cellBtn.Dock = DockStyle.Fill;
                Button b1 = new Button(); b1.Text = "应用本体数值"; b1.Left = 8; b1.Top = 4; b1.Width = 130;
                b1.Click += delegate
                {
                    int ok = 0;
                    for (int i = 0; i < resNums.Count; i++)
                        if (SetNum(resKeys[i], resNums[i])) ok++;
                    SetStatus("已应用 " + ok + " 项本体数值");
                    RefreshAllValues();
                };
                cellBtn.Controls.Add(b1);
                t.Controls.Add(cellBtn, 0, 7); t.SetColumnSpan(cellBtn, 2);

                
                t.RowStyles.Add(new RowStyle(SizeType.Absolute, 26));
                Label g2 = new Label(); g2.Text = "流程 (回合/投票)"; g2.Font = new Font(Font, FontStyle.Bold); g2.Dock = DockStyle.Fill;
                t.Controls.Add(g2, 0, 8); t.SetColumnSpan(g2, 2);

                
                t.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
                t.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
                numTurn = MakeNumInCell("回合数", "BaseGameSetup.CurrentTurn");
                Panel cellTurn = numTurn.Parent as Panel;
                t.Controls.Add(cellTurn, 0, 9);
                numAssembly = MakeNumInCell("议会改革票", "BaseGame.Reform_Assembly_Vote");
                Panel cellAssembly = numAssembly.Parent as Panel;
                t.Controls.Add(cellAssembly, 1, 9);
                numCourt = MakeNumInCell("法院改革票", "BaseGame.Reform_Court_Vote");
                Panel cellCourt = numCourt.Parent as Panel;
                t.Controls.Add(cellCourt, 0, 10);
                Panel cellBtn2 = new Panel(); cellBtn2.Dock = DockStyle.Fill;
                Button b2 = new Button(); b2.Text = "应用流程"; b2.Left = 8; b2.Top = 4; b2.Width = 100;
                b2.Click += delegate
                {
                    SetNum("BaseGameSetup.CurrentTurn", numTurn);
                    SetNum("BaseGame.Reform_Assembly_Vote", numAssembly);
                    SetNum("BaseGame.Reform_Court_Vote", numCourt);
                    SetStatus("流程已应用");
                    RefreshAllValues();
                };
                cellBtn2.Controls.Add(b2);
                t.Controls.Add(cellBtn2, 1, 10);

                
                t.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
                Label g3 = new Label(); g3.Text = "自定义变量 (variables 任意键, 含布尔)"; g3.Font = new Font(Font, FontStyle.Bold); g3.Dock = DockStyle.Fill;
                t.Controls.Add(g3, 0, 11); t.SetColumnSpan(g3, 2);
                t.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
                Panel cellCustom = new Panel(); cellCustom.Dock = DockStyle.Fill;
                Label lk = new Label(); lk.Text = "键名:"; lk.Left = 8; lk.Top = 9; lk.AutoSize = true;
                txtCustomKey = new TextBox(); txtCustomKey.Left = 52; txtCustomKey.Top = 5; txtCustomKey.Width = 300;
                Label lv = new Label(); lv.Text = "新值:"; lv.Left = 364; lv.Top = 9; lv.AutoSize = true;
                numCustomVal = new NumericUpDown(); numCustomVal.Left = 404; numCustomVal.Top = 5; numCustomVal.Width = 100; numCustomVal.Minimum = -99999999; numCustomVal.Maximum = 99999999;
                Button btnCustomApply = new Button(); btnCustomApply.Text = "应用"; btnCustomApply.Left = 516; btnCustomApply.Top = 4; btnCustomApply.Width = 64;
                btnCustomApply.Click += delegate
                {
                    if (txtCustomKey.Text.Trim().Length == 0) { SetStatus("请输入键名"); return; }
                    if (core.SetVar(txtCustomKey.Text.Trim(), numCustomVal.Value.ToString(CultureInfo.InvariantCulture)))
                        SetStatus(txtCustomKey.Text.Trim() + " -> " + numCustomVal.Value);
                    else if (core.SetBoolVar(txtCustomKey.Text.Trim(), numCustomVal.Value == 0 ? "false" : "true"))
                        SetStatus(txtCustomKey.Text.Trim() + " -> " + (numCustomVal.Value == 0 ? "false" : "true"));
                    else SetStatus("键不存在: " + txtCustomKey.Text.Trim());
                };
                cellCustom.Controls.Add(lk); cellCustom.Controls.Add(txtCustomKey); cellCustom.Controls.Add(lv); cellCustom.Controls.Add(numCustomVal); cellCustom.Controls.Add(btnCustomApply);
                t.Controls.Add(cellCustom, 0, 12); t.SetColumnSpan(cellCustom, 2);

                p.Controls.Add(t);
                return p;
            }

            
            Panel MakeNumCell(string label, string key)
            {
                Panel cell = new Panel(); cell.Dock = DockStyle.Fill; cell.Margin = new Padding(2);
                Label l = new Label(); l.Text = label; l.Left = 8; l.Top = 8; l.AutoSize = true;
                NumericUpDown n = new NumericUpDown();
                n.Left = 150; n.Top = 4; n.Width = 90; n.Minimum = -999999; n.Maximum = 999999; n.Tag = key;
                Button b = new Button(); b.Text = "设置"; b.Left = 248; b.Top = 3; b.Width = 52; b.Height = 24;
                b.Click += delegate
                {
                    if (!core.loaded) return;
                    if (core.SetVar(key, n.Value.ToString(CultureInfo.InvariantCulture))) SetStatus(TranslateVarName(key) + " = " + n.Value);
                    else SetStatus("设置失败: " + key);
                };
                cell.Controls.Add(l); cell.Controls.Add(n); cell.Controls.Add(b);
                resNums.Add(n);
                return cell;
            }

            
            NumericUpDown MakeNumInCell(string label, string key)
            {
                Panel cell = new Panel(); cell.Dock = DockStyle.Fill; cell.Margin = new Padding(2);
                Label l = new Label(); l.Text = label; l.Left = 8; l.Top = 8; l.AutoSize = true;
                NumericUpDown n = new NumericUpDown();
                n.Left = 130; n.Top = 4; n.Width = 110; n.Minimum = -999999; n.Maximum = 999999; n.Tag = key;
                cell.Controls.Add(l); cell.Controls.Add(n);
                return n;
            }

            NumericUpDown AddRow(Panel p, int y, string label, string key)
            {
                Label l = new Label(); l.Text = label; l.Left = 16; l.Top = y + 2; l.AutoSize = true;
                NumericUpDown n = new NumericUpDown();
                n.Left = 500; n.Top = y; n.Width = 110; n.Minimum = -999999; n.Maximum = 999999;
                n.Tag = key;
                p.Controls.Add(l); p.Controls.Add(n);
                return n;
            }

            bool SetNum(string key, NumericUpDown n)
            {
                if (!core.loaded) return false;
                return core.SetVar(key, n.Value.ToString(CultureInfo.InvariantCulture));
            }

            
            void ApplyAllPending()
            {
                if (!core.loaded) return;
                int ok = 0;
                for (int i = 0; i < resNums.Count; i++)
                {
                    string k = resNums[i].Tag as string;
                    if (k != null && core.SetVar(k, resNums[i].Value.ToString(CultureInfo.InvariantCulture))) ok++;
                }
                if (numTurn != null && core.SetVar("BaseGameSetup.CurrentTurn", numTurn.Value.ToString(CultureInfo.InvariantCulture))) ok++;
                if (numAssembly != null && core.SetVar("BaseGame.Reform_Assembly_Vote", numAssembly.Value.ToString(CultureInfo.InvariantCulture))) ok++;
                if (numCourt != null && core.SetVar("BaseGame.Reform_Court_Vote", numCourt.Value.ToString(CultureInfo.InvariantCulture))) ok++;
                if (numAuthority != null && core.SetVar("RiziaDLC.Resources_Authority", numAuthority.Value.ToString(CultureInfo.InvariantCulture))) ok++;
                if (numBudget != null && core.SetVar("RiziaDLC.Resources_Budget", numBudget.Value.ToString(CultureInfo.InvariantCulture))) ok++;
                if (numEnergy != null && core.SetVar("RiziaDLC.Resources_Energy", numEnergy.Value.ToString(CultureInfo.InvariantCulture))) ok++;
                for (int i = 0; i < hudNums.Count; i++)
                {
                    string k = hudNums[i].Tag as string;
                    if (k != null && core.SetVar(k, hudNums[i].Value.ToString(CultureInfo.InvariantCulture))) ok++;
                }
                if (txtCustomKey != null && numCustomVal != null && txtCustomKey.Text.Trim().Length > 0)
                {
                    string k = txtCustomKey.Text.Trim();
                    string v = numCustomVal.Value.ToString(CultureInfo.InvariantCulture);
                    if (core.SetVar(k, v) || core.SetBoolVar(k, numCustomVal.Value == 0 ? "false" : "true")) ok++;
                }
                if (ok > 0) SetStatus("保存前已同步 " + ok + " 项输入框");
            }

            
            Control BuildDlcPanel()
            {
                Panel p = new Panel();
                SplitContainer sp = new SplitContainer(); sp.Dock = DockStyle.Fill; sp.Orientation = Orientation.Horizontal; sp.SplitterDistance = 150;
                Panel p1 = new Panel(); p1.Dock = DockStyle.Fill; p1.AutoScroll = true;
                int y = 12;
                Label g1 = new Label(); g1.Text = "DLC 三大资源"; g1.Font = new Font(Font, FontStyle.Bold); g1.Left = 16; g1.Top = y; g1.AutoSize = true;
                p1.Controls.Add(g1); y += 26;
                numAuthority = AddRow(p1, y, "权威 Authority  [RiziaDLC.Resources_Authority]", "RiziaDLC.Resources_Authority"); y += 30;
                numBudget = AddRow(p1, y, "预算 Budget  [RiziaDLC.Resources_Budget]", "RiziaDLC.Resources_Budget"); y += 30;
                numEnergy = AddRow(p1, y, "能源 Energy  [RiziaDLC.Resources_Energy]", "RiziaDLC.Resources_Energy"); y += 30;
                Button b1 = new Button(); b1.Text = "应用三大资源"; b1.Left = 16; b1.Top = y; b1.Width = 120;
                b1.Click += delegate
                {
                    SetNum("RiziaDLC.Resources_Authority", numAuthority);
                    SetNum("RiziaDLC.Resources_Budget", numBudget);
                    SetNum("RiziaDLC.Resources_Energy", numEnergy);
                    SetStatus("DLC 资源已应用");
                    RefreshAllValues();
                };
                p1.Controls.Add(b1); y += 34;
                Label g2 = new Label(); g2.Text = "HUD 上下限 (本体 4 + DLC 6)"; g2.Font = new Font(Font, FontStyle.Bold); g2.Left = 16; g2.Top = y; g2.AutoSize = true;
                p1.Controls.Add(g2); y += 26;
                string[] hudKeysA = {
                    "BaseGame.Sordland_HUDStat_GovernmentBudget_Min", "BaseGame.Sordland_HUDStat_GovernmentBudget_Max",
                    "BaseGame.Sordland_HUDStat_PersonalWealth_Min", "BaseGame.Sordland_HUDStat_PersonalWealth_Max",
                    "RiziaDLC.Rizia_HUDStat_Authority_Min", "RiziaDLC.Rizia_HUDStat_Authority_Max",
                    "RiziaDLC.Rizia_HUDStat_Budget_Min", "RiziaDLC.Rizia_HUDStat_Budget_Max",
                    "RiziaDLC.Rizia_HUDStat_Energy_Min", "RiziaDLC.Rizia_HUDStat_Energy_Max" };
                for (int i = 0; i < hudKeysA.Length; i++)
                {
                    hudNums.Add(AddRow(p1, y, hudKeysA[i], hudKeysA[i]));
                    y += 28;
                }
                Button b2 = new Button(); b2.Text = "应用 HUD 上下限"; b2.Left = 16; b2.Top = y; b2.Width = 130;
                b2.Click += delegate
                {
                    int ok = 0;
                    for (int i = 0; i < hudKeysA.Length; i++)
                        if (core.SetVar(hudKeysA[i], FindNum(p1, hudKeysA[i]).Value.ToString(CultureInfo.InvariantCulture))) ok++;
                    SetStatus("HUD 上下限已应用 " + ok + " 项");
                    RefreshAllValues();
                };
                p1.Controls.Add(b2);
                sp.Panel1.Controls.Add(p1);
                Panel p2 = new Panel(); p2.Dock = DockStyle.Fill;
                Label l2 = new Label(); l2.Text = "修正项 (Authority/Budget/Energy_Modifier_*, 双击改, 正=增益 负=减益):"; l2.Dock = DockStyle.Top; l2.Height = 22; l2.Padding = new Padding(4, 2, 0, 0);
                lvModifier = new ListView(); lvModifier.Dock = DockStyle.Fill; lvModifier.View = View.Details; lvModifier.FullRowSelect = true;
                lvModifier.GridLines = true; lvModifier.MultiSelect = false;
                lvModifier.Columns.Add("修正项", -2); lvModifier.Columns.Add("当前值", 100);
                AttachFavoriteMenu(lvModifier);
                p2.Controls.Add(lvModifier); p2.Controls.Add(l2);
                lvModifier.DoubleClick += delegate
                {
                    if (lvModifier.SelectedItems.Count == 0 || !core.loaded) return;
                    string key = lvModifier.SelectedItems[0].Tag as string ?? lvModifier.SelectedItems[0].SubItems[0].Text;
                    string cur = core.GetVar(key);
                    if (cur == null) return;
                    string nv = InputBox("修改修正项", key + "  (当前=" + FormatVal(cur) + ")", FormatVal(cur));
                    if (nv == null) return;
                    if (core.SetVar(key, nv)) { RefreshModifier(); SetStatus(key + " -> " + nv); }
                    else SetStatus("修改失败: " + key);
                };
                sp.Panel2.Controls.Add(p2);
                p.Controls.Add(sp);
                return p;
            }

            NumericUpDown FindNum(Panel p, string key)
            {
                foreach (Control c in p.Controls)
                {
                    NumericUpDown n = c as NumericUpDown;
                    if (n != null && n.Tag != null && (string)n.Tag == key) return n;
                }
                return null;
            }

            void RefreshModifier()
            {
                if (!core.loaded) return;
                lvModifier.BeginUpdate();
                lvModifier.Items.Clear();
                List<string> mods = core.ScanVars("RiziaDLC.Authority_Modifier_");
                mods.AddRange(core.ScanVars("RiziaDLC.Budget_Modifier_"));
                mods.AddRange(core.ScanVars("RiziaDLC.Energy_Modifier_"));
                mods.Sort();
                foreach (string k in mods)
                {
                    ListViewItem it = new ListViewItem(DisplayName(k));
                    it.Tag = k;
                    it.ToolTipText = k;
                    it.SubItems.Add(FormatVal(core.GetVar(k) ?? "?"));
                    lvModifier.Items.Add(it);
                }
                lvModifier.EndUpdate();
            }

            
            FilterPage CreateFilterPage(TabControl tabs, string title, string[] prefixes, string[] keywords)
            {
                FilterPage fp = new FilterPage();
                fp.title = title; fp.keywords = keywords;
                foreach (string pr in prefixes) fp.allPrefixes.Add(pr);
                Panel p = new Panel(); p.Dock = DockStyle.Fill;
                Panel top = new Panel(); top.Dock = DockStyle.Top; top.Height = 64;
                Label lp = new Label(); lp.Text = "范围:"; lp.Left = 10; lp.Top = 8; lp.AutoSize = true;
                fp.cmbPrefix = new ComboBox(); fp.cmbPrefix.DropDownStyle = ComboBoxStyle.DropDownList; fp.cmbPrefix.Left = 52; fp.cmbPrefix.Top = 5; fp.cmbPrefix.Width = 300;
                Label ls = new Label(); ls.Text = "搜索:"; ls.Left = 362; ls.Top = 8; ls.AutoSize = true;
                fp.txtSearch = new TextBox(); fp.txtSearch.Left = 402; fp.txtSearch.Top = 5; fp.txtSearch.Width = 150;
                Button btnRefresh = new Button(); btnRefresh.Text = "刷新"; btnRefresh.Left = 10; btnRefresh.Top = 36; btnRefresh.Width = 60;
                fp.lblPage = new Label(); fp.lblPage.AutoSize = false; fp.lblPage.Left = 78; fp.lblPage.Top = 41; fp.lblPage.Width = 190; fp.lblPage.ForeColor = Color.DimGray;
                fp.btnPrev = new Button(); fp.btnPrev.Text = "<"; fp.btnPrev.Left = 514; fp.btnPrev.Top = 36; fp.btnPrev.Width = 34;
                fp.btnNext = new Button(); fp.btnNext.Text = ">"; fp.btnNext.Left = 552; fp.btnNext.Top = 36; fp.btnNext.Width = 34;
                fp.cmbPrefix.SelectedIndexChanged += delegate { fp.page = 0; RefreshFilterPage(fp); };
                fp.txtSearch.TextChanged += delegate { fp.page = 0; RefreshFilterPage(fp); };
                btnRefresh.Click += delegate { fp.page = 0; RefreshFilterPage(fp); };
                fp.btnPrev.Click += delegate { if (fp.page > 0) { fp.page--; RefreshFilterPage(fp); } };
                fp.btnNext.Click += delegate { fp.page++; RefreshFilterPage(fp); };
                top.Controls.Add(lp); top.Controls.Add(fp.cmbPrefix); top.Controls.Add(ls); top.Controls.Add(fp.txtSearch);
                top.Controls.Add(btnRefresh); top.Controls.Add(fp.lblPage); top.Controls.Add(fp.btnPrev); top.Controls.Add(fp.btnNext);
                fp.lv = new ListView(); fp.lv.Dock = DockStyle.Fill; fp.lv.View = View.Details; fp.lv.FullRowSelect = true;
                fp.lv.GridLines = true; fp.lv.MultiSelect = true;
                fp.lv.Columns.Add("变量", -2); fp.lv.Columns.Add("当前值", 120);
                ContextMenuStrip menu = new ContextMenuStrip();
                ToolStripMenuItem miBatch = new ToolStripMenuItem("批量编辑选中项");
                miBatch.Click += delegate { BatchEditFilterPage(fp); };
                menu.Items.Add(miBatch);
                ToolStripMenuItem miFav = new ToolStripMenuItem("收藏/取消收藏当前行");
                miFav.Click += delegate { ToggleFavorite(fp.lv); };
                menu.Items.Add(miFav);
                fp.lv.ContextMenuStrip = menu;
                fp.lv.DoubleClick += delegate { EditFilterItem(fp); };
                p.Controls.Add(fp.lv);
                p.Controls.Add(top);   
                
                if (title.Length > 0)
                    tabs.TabPages.Add(MakeTab(title, p));
                return fp;
            }

            void RebuildPrefixes(FilterPage fp)
            {
                fp.prefixes.Clear();
                foreach (string pr in fp.allPrefixes)
                    if (PrefixAllowed(pr)) fp.prefixes.Add(pr);
                fp.cmbPrefix.Items.Clear();
                fp.cmbPrefix.Items.Add("全部范围");
                foreach (string pr in fp.prefixes) fp.cmbPrefix.Items.Add(pr);
                fp.cmbPrefix.SelectedIndex = 0;
            }

            void RefreshFilterPage(FilterPage fp)
            {
                fp.lv.BeginUpdate();
                fp.lv.Items.Clear();
                if (!core.loaded) { fp.lv.EndUpdate(); return; }
                List<string> all = new List<string>();
                foreach (string pr in fp.prefixes)
                {
                    all.AddRange(core.ScanVars(pr));
                    all.AddRange(core.ScanBoolVars(pr));
                    all.AddRange(core.ScanStringVars(pr));
                }
                List<string> keys = new List<string>();
                HashSet<string> seen = new HashSet<string>();
                foreach (string k in all)
                {
                    if (!seen.Add(k)) continue;
                    if (fp.cmbPrefix.SelectedIndex > 0 && !k.StartsWith(fp.prefixes[fp.cmbPrefix.SelectedIndex - 1])) continue;
                    bool ok = fp.keywords.Length == 0;
                    if (!ok)
                        foreach (string kw in fp.keywords)
                            if (k.Contains(kw)) { ok = true; break; }
                    if (!ok) continue;
                    if (fp.txtSearch.Text.Trim().Length > 0) { string _q = fp.txtSearch.Text.Trim(); string _cn; if (!k.Contains(_q) && !(TranslateDict.Words.TryGetValue(k, out _cn) && _cn != null && _cn.Contains(_q))) continue; }
                    keys.Add(k);
                }
                keys.Sort();
                int total = keys.Count;
                int start = fp.paged ? fp.page * fp.pageSize : 0;
                int end = fp.paged ? Math.Min(total, start + fp.pageSize) : total;
                if (start >= total && total > 0) { fp.page = 0; start = 0; end = Math.Min(total, fp.pageSize); }
                for (int i = start; i < end; i++)
                {
                    string k = keys[i];
                    string v = core.GetVar(k);
                    if (v == null) v = core.GetBoolVar(k);
                    if (v == null) v = core.GetStringVar(k);
                    if (v == null) v = "?";
                    ListViewItem it = new ListViewItem(DisplayName(k));
                    it.Tag = k;
                    it.ToolTipText = k;
                    it.SubItems.Add(FormatVal(v));
                    fp.lv.Items.Add(it);
                }
                fp.lv.EndUpdate();
                if (fp.paged)
                    fp.lblPage.Text = "第 " + (fp.page + 1) + "/" + Math.Max(1, (total + fp.pageSize - 1) / fp.pageSize) + " 页 (共 " + total + ")";
                else
                    fp.lblPage.Text = "共 " + total + " 项";
            }

            void EditFilterItem(FilterPage fp)
            {
                if (fp.lv.SelectedItems.Count == 0 || !core.loaded) return;
                string key = fp.lv.SelectedItems[0].Tag as string;
                if (key == null) key = fp.lv.SelectedItems[0].SubItems[0].Text;
                string cur = fp.lv.SelectedItems[0].SubItems[1].Text;
                ListViewSnapshot snap = CaptureListView(fp.lv);
                if (cur == "true" || cur == "false")
                {
                    string nv = cur == "true" ? "false" : "true";
                    if (core.SetBoolVar(key, nv)) { RefreshFilterPage(fp); RestoreListView(fp.lv, snap); SetStatus(key + ": " + cur + " -> " + nv); }
                    return;
                }
                if (cur == "?") return;
                string input = InputBox("修改数值", key + "  (当前=" + FormatVal(cur) + ")", FormatVal(cur));
                if (input == null) return;
                bool ok;
                if (IsNumeric(cur)) ok = core.SetVar(key, input);
                else ok = core.SetStringVar(key, input);
                if (ok) { RefreshFilterPage(fp); RestoreListView(fp.lv, snap); SetStatus(key + " -> " + input); }
                else SetStatus("修改失败: " + key);
            }

            void BatchEditFilterPage(FilterPage fp)
            {
                if (fp.lv.SelectedItems.Count == 0 || !core.loaded) return;
                int count = fp.lv.SelectedItems.Count;
                string input = InputBox("批量修改", "对选中 " + count + " 项设置值。留空 = 布尔项全部翻转：", "");
                if (input == null) return;
                int ok = 0;
                foreach (ListViewItem it in fp.lv.SelectedItems)
                {
                    string key = it.Tag as string ?? it.SubItems[0].Text;
                    string cur = core.GetVar(key);
                    if (cur == null) cur = core.GetBoolVar(key);
                    if (cur == null) cur = core.GetStringVar(key);
                    if (cur == null) continue;
                    bool done = false;
                    if (cur == "true" || cur == "false")
                    {
                        if (input.Length == 0)
                            done = core.SetBoolVar(key, cur == "true" ? "false" : "true");
                        else if (input == "true" || input == "false")
                            done = core.SetBoolVar(key, input);
                    }
                    else if (input.Length > 0)
                    {
                        if (IsNumeric(cur)) done = core.SetVar(key, input);
                        else done = core.SetStringVar(key, input);
                    }
                    if (done) ok++;
                }
                RefreshFilterPage(fp);
                RefreshQuickPanel();
                RefreshFavoritesPanel();
                RefreshHistoryPanel();
                SetStatus("批量修改完成: " + ok + "/" + count + " 项");
            }

            bool IsNumeric(string s)
            {
                double d;
                return double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out d);
            }

            
            string FormatVal(string v)
            {
                if (v != null && (v.IndexOf('e') >= 0 || v.IndexOf('E') >= 0))
                {
                    try { return decimal.Parse(v, NumberStyles.Float, CultureInfo.InvariantCulture).ToString(CultureInfo.InvariantCulture); }
                    catch { }
                }
                return v;
            }

            string InputBox(string title, string prompt, string def)
            {
                Form f = new Form(); f.Text = title; f.FormBorderStyle = FormBorderStyle.FixedDialog;
                f.StartPosition = FormStartPosition.CenterParent; f.ClientSize = new Size(400, 120); f.Font = Font;
                Label l = new Label(); l.Text = prompt; l.Left = 12; l.Top = 12; l.AutoSize = true; l.MaximumSize = new Size(370, 40);
                TextBox t = new TextBox(); t.Left = 12; t.Top = 44; t.Width = 376; t.Text = def;
                Button ok = new Button(); ok.Text = "确定"; ok.DialogResult = DialogResult.OK; ok.Left = 210; ok.Top = 80; ok.Width = 80;
                Button cc = new Button(); cc.Text = "取消"; cc.DialogResult = DialogResult.Cancel; cc.Left = 300; cc.Top = 80; cc.Width = 80;
                f.Controls.Add(l); f.Controls.Add(t); f.Controls.Add(ok); f.Controls.Add(cc);
                f.AcceptButton = ok; f.CancelButton = cc;
                ScaleDialog(f);
                if (f.ShowDialog(this) == DialogResult.OK) return t.Text.Trim();
                return null;
            }

            
            Control BuildDecreePanel(TabControl tabs)
            {
                
                fpDecree = CreateFilterPage(tabs, "", DECREE_PREFIXES, DECREE_KEYWORDS);
                fpDecree.paged = false;
                return fpDecree.lv.Parent;
            }

            
            Control BuildEventPanel()
            {
                Panel p = new Panel();
                Panel top = new Panel(); top.Dock = DockStyle.Top; top.Height = 34;
                Label lp = new Label(); lp.Text = "前缀:"; lp.Left = 10; lp.Top = 8; lp.AutoSize = true;
                cmbEvPrefix = new ComboBox(); cmbEvPrefix.DropDownStyle = ComboBoxStyle.DropDownList; cmbEvPrefix.Left = 52; cmbEvPrefix.Top = 5; cmbEvPrefix.Width = 220;
                cmbEvPrefix.Items.AddRange(new object[] { "GameCondition.(本体剧情)", "BaseGameIsolated.(本体回合)", "RiziaDLCIsolated.(DLC回合)", "BaseGame.(本体核心)", "RiziaDLC.(DLC核心)" });
                cmbEvPrefix.SelectedIndex = 0;
                Label lt = new Label(); lt.Text = "回合:"; lt.Left = 282; lt.Top = 8; lt.AutoSize = true;
                cmbEvTurn = new ComboBox(); cmbEvTurn.DropDownStyle = ComboBoxStyle.DropDownList; cmbEvTurn.Left = 322; cmbEvTurn.Top = 5; cmbEvTurn.Width = 90;
                cmbEvTurn.Items.Add("全部");
                for (int i = 1; i <= 20; i++) cmbEvTurn.Items.Add("Turn" + i.ToString("00"));
                cmbEvTurn.SelectedIndex = 0;
                Button b1 = new Button(); b1.Text = "刷新"; b1.Left = 422; b1.Top = 5; b1.Width = 60;
                Button b2 = new Button(); b2.Text = "全部 true"; b2.Left = 490; b2.Top = 5; b2.Width = 80;
                Button b3 = new Button(); b3.Text = "全部 false"; b3.Left = 578; b3.Top = 5; b3.Width = 80;
                top.Controls.Add(lp); top.Controls.Add(cmbEvPrefix); top.Controls.Add(lt); top.Controls.Add(cmbEvTurn);
                top.Controls.Add(b1); top.Controls.Add(b2); top.Controls.Add(b3);
                lvEvents = new ListView(); lvEvents.Dock = DockStyle.Fill; lvEvents.View = View.Details; lvEvents.FullRowSelect = true;
                lvEvents.GridLines = true; lvEvents.MultiSelect = false;
                lvEvents.Columns.Add("事件", -2); lvEvents.Columns.Add("值", 100);
                AttachFavoriteMenu(lvEvents);
                p.Controls.Add(lvEvents);
                p.Controls.Add(top);   
                Label hint = new Label(); hint.Dock = DockStyle.Bottom; hint.Height = 30; hint.Padding = new Padding(6, 6, 0, 0);
                hint.Text = "双击行切换 true/false。解锁事件可能跳过正常剧情, 建议单项操作并先备份。";
                hint.ForeColor = Color.DarkRed;
                p.Controls.Add(hint);
                b1.Click += delegate { RefreshEvents(); };
                b2.Click += delegate { SetAllEvents("true"); };
                b3.Click += delegate { SetAllEvents("false"); };
                lvEvents.DoubleClick += delegate
                {
                    if (lvEvents.SelectedItems.Count == 0) return;
                    string key = lvEvents.SelectedItems[0].Tag as string ?? lvEvents.SelectedItems[0].SubItems[0].Text;
                    string cur = core.GetBoolVar(key);
                    string nv = cur == "true" ? "false" : "true";
                    if (core.SetBoolVar(key, nv)) { RefreshEvents(); SetStatus(key + ": " + cur + " -> " + nv); }
                };
                return p;
            }

            void RebuildEventPrefixes()
            {
                if (cmbEvPrefix == null) return;
                cmbEvPrefix.Items.Clear();
                bool showBase = core.storyMode == 1 || core.storyMode == 3;
                bool showDlc = core.storyMode == 2 || core.storyMode == 3;
                if (showBase)
                {
                    cmbEvPrefix.Items.Add("GameCondition.(本体剧情)");
                    cmbEvPrefix.Items.Add("BaseGameIsolated.(本体回合)");
                    cmbEvPrefix.Items.Add("BaseGame.(本体核心)");
                }
                if (showDlc)
                {
                    cmbEvPrefix.Items.Add("RiziaDLCIsolated.(DLC回合)");
                    cmbEvPrefix.Items.Add("RiziaDLC.(DLC核心)");
                }
                if (cmbEvPrefix.Items.Count > 0) cmbEvPrefix.SelectedIndex = 0;
            }

            string EventPrefix()
            {
                string s = cmbEvPrefix.SelectedItem as string ?? "";
                if (s.IndexOf("GameCondition.") >= 0) return "GameCondition.";
                if (s.IndexOf("BaseGameIsolated.") >= 0) return "BaseGameIsolated.";
                if (s.IndexOf("RiziaDLCIsolated.") >= 0) return "RiziaDLCIsolated.";
                if (s.IndexOf("BaseGame.") >= 0) return "BaseGame.";
                if (s.IndexOf("RiziaDLC.") >= 0) return "RiziaDLC.";
                return "GameCondition.";
            }

            void RefreshEvents()
            {
                lvEvents.BeginUpdate();
                lvEvents.Items.Clear();
                if (!core.loaded) { lvEvents.EndUpdate(); return; }
                string prefix = EventPrefix();
                List<string> keys = core.ScanBoolVars(prefix);
                int turn = cmbEvTurn.SelectedIndex;
                if (turn > 0)
                {
                    string tag = "Turn" + turn.ToString("00");
                    List<string> f = new List<string>();
                    foreach (string k in keys) if (k.Contains(tag)) f.Add(k);
                    keys = f;
                }
                keys.Sort();
                foreach (string k in keys)
                {
                    ListViewItem it = new ListViewItem(DisplayName(k));
                    it.Tag = k;
                    it.ToolTipText = k;
                    it.SubItems.Add(core.GetBoolVar(k) ?? "?");
                    lvEvents.Items.Add(it);
                }
                lvEvents.EndUpdate();
                SetStatus("事件: " + prefix + " 共 " + keys.Count + " 个");
            }

            void SetAllEvents(string val)
            {
                if (!core.loaded) return;
                if (MessageBox.Show(this, "确认将当前列表 " + lvEvents.Items.Count + " 个事件全部置为 " + val + "?", "事件", MessageBoxButtons.YesNo) != DialogResult.Yes)
                    return;
                int ok = 0;
                foreach (ListViewItem it in lvEvents.Items)
                    if (core.SetBoolVar(it.Tag as string ?? it.SubItems[0].Text, val)) ok++;
                RefreshEvents();
                SetStatus("已置 " + val + ": " + ok + " 个");
            }

            
            

            Control BuildHbPanel()
            {
                Panel p = new Panel();
                p.Dock = DockStyle.Fill;
                p.AutoScroll = true;

                Label title = new Label();
                title.Text = "Horrifying Bombardment（HB）模组联动";
                title.Font = new Font(Font, FontStyle.Bold);
                title.Left = 12; title.Top = 10; title.AutoSize = true;
                p.Controls.Add(title);

                lblHbStatus = new Label();
                lblHbStatus.Left = 12; lblHbStatus.Top = 34; lblHbStatus.AutoSize = true;
                lblHbStatus.ForeColor = Color.DimGray;
                p.Controls.Add(lblHbStatus);

                chkHbEnabled = new CheckBox();
                chkHbEnabled.Text = "启用 Horrifying Bombardment（取消勾选 = 完全等同原版）";
                chkHbEnabled.Left = 12; chkHbEnabled.Top = 66; chkHbEnabled.Width = 480; chkHbEnabled.Checked = true;
                p.Controls.Add(chkHbEnabled);

                chkHbAuto = new CheckBox();
                chkHbAuto.Text = "按空军编制自动加成（每 2 个战斗机编制 +1 次支援；每 2 个轰炸机编制 +1 次轰炸）";
                chkHbAuto.Left = 12; chkHbAuto.Top = 92; chkHbAuto.Width = 620; chkHbAuto.Checked = true;
                p.Controls.Add(chkHbAuto);

                Label l1 = new Label(); l1.Text = "空中支援额外次数："; l1.Left = 12; l1.Top = 128; l1.AutoSize = true;
                numHbSupport = new NumericUpDown(); numHbSupport.Left = 132; numHbSupport.Top = 124; numHbSupport.Width = 70;
                numHbSupport.Minimum = -1; numHbSupport.Maximum = 99; numHbSupport.Value = -1;
                Label h1 = new Label(); h1.Text = "-1 = 自动"; h1.Left = 208; h1.Top = 128; h1.AutoSize = true; h1.ForeColor = Color.Gray;
                p.Controls.Add(l1); p.Controls.Add(numHbSupport); p.Controls.Add(h1);

                Label l2 = new Label(); l2.Text = "空中轰炸额外次数："; l2.Left = 290; l2.Top = 128; l2.AutoSize = true;
                numHbStrike = new NumericUpDown(); numHbStrike.Left = 410; numHbStrike.Top = 124; numHbStrike.Width = 70;
                numHbStrike.Minimum = -1; numHbStrike.Maximum = 99; numHbStrike.Value = -1;
                Label h2 = new Label(); h2.Text = "-1 = 自动"; h2.Left = 486; h2.Top = 128; h2.AutoSize = true; h2.ForeColor = Color.Gray;
                p.Controls.Add(l2); p.Controls.Add(numHbStrike); p.Controls.Add(h2);

                Button btnApply = new Button(); btnApply.Text = "应用到模组"; btnApply.Left = 12; btnApply.Top = 158; btnApply.Width = 110;
                Button btnDefault = new Button(); btnDefault.Text = "恢复默认（删除开关文件）"; btnDefault.Left = 130; btnDefault.Top = 158; btnDefault.Width = 190;
                Button btnRefresh = new Button(); btnRefresh.Text = "刷新"; btnRefresh.Left = 328; btnRefresh.Top = 158; btnRefresh.Width = 70;
                p.Controls.Add(btnApply); p.Controls.Add(btnDefault); p.Controls.Add(btnRefresh);

                btnApply.Click += delegate
                {
                    bool ok = HbBridge.WriteOverride(chkHbEnabled.Checked, chkHbAuto.Checked,
                        (int)numHbSupport.Value, (int)numHbStrike.Value);
                    if (ok) SetStatus("已写入 HB 开关文件（游戏内 ≤1 秒生效；额度在回合开始时结算）");
                    else SetStatus("写入 HB 开关文件失败：" + HbBridge.LastError);
                    RefreshHbPanel();
                };
                btnDefault.Click += delegate
                {
                    bool ok = HbBridge.DeleteOverride();
                    hbControlsLoaded = false;
                    if (ok) SetStatus("已删除 HB 开关文件（恢复默认：启用 + 按编制自动加成）");
                    else SetStatus("删除失败：" + HbBridge.LastError);
                    RefreshHbPanel();
                };
                btnRefresh.Click += delegate { RefreshHbPanel(); };

                Label lv = new Label(); lv.Text = "HB 模组运行状态（只读，每 2 秒刷新）"; lv.Left = 12; lv.Top = 196; lv.AutoSize = true;
                lv.ForeColor = Color.DimGray;
                p.Controls.Add(lv);

                lblHbVars = new Label();
                lblHbVars.Left = 12; lblHbVars.Top = 218; lblHbVars.Width = 760; lblHbVars.Height = 190;
                lblHbVars.Font = new Font("Consolas", 9.5f);
                lblHbVars.BorderStyle = BorderStyle.FixedSingle;
                lblHbVars.Padding = new Padding(6);
                p.Controls.Add(lblHbVars);

                Label hint = new Label();
                hint.Left = 12; hint.Top = 418; hint.Width = 760; hint.Height = 40;
                hint.ForeColor = Color.DimGray;
                hint.Text = "说明：本页仅在检测到 HB 模组的状态文件时出现。开关与时次数写入 override 文件，HB 每秒轮询生效；"
                          + "额外空中行动同样消耗战争行动点（每回合 6 点），因此次数填再大也会被行动点自然限制。";
                p.Controls.Add(hint);

                
                
                p.Resize += delegate
                {
                    int vw = p.ClientSize.Width - 26;
                    if (vw < 220) vw = 220;
                    try { hint.Width = vw; } catch { }
                    try { if (lblHbVars != null) lblHbVars.Width = vw; } catch { }
                    try { if (chkHbAuto != null) chkHbAuto.Width = vw; } catch { }   
                    try
                    {
                        int need = TextRenderer.MeasureText(hint.Text, hint.Font,
                            new Size(vw, 10000), TextFormatFlags.WordBreak).Height + 6;
                        if (need < 40) need = 40;
                        hint.Height = need;
                    }
                    catch { }
                };

                RefreshHbPanel();
                return p;
            }

            

            Control BuildOiPanel()
            {
                Panel p = new Panel();
                p.Dock = DockStyle.Fill;
                p.AutoScroll = true;

                Label title = new Label();
                title.Text = "安济艺术（Oikonomia）— 本体总统令无限制";
                title.Font = new Font(Font, FontStyle.Bold);
                title.Left = 12; title.Top = 10; title.AutoSize = true;
                p.Controls.Add(title);

                lblOiStatus = new Label();
                lblOiStatus.Left = 12; lblOiStatus.Top = 34; lblOiStatus.AutoSize = true;
                lblOiStatus.ForeColor = Color.DimGray;
                p.Controls.Add(lblOiStatus);

                chkOiEnabled = new CheckBox();
                chkOiEnabled.Text = "启用 安济艺术（取消勾选 = 完全等同原版，恢复 3 个上限）";
                chkOiEnabled.Left = 12; chkOiEnabled.Top = 66; chkOiEnabled.Width = 380;
                chkOiDynamic = new CheckBox(); chkOiDynamic.Text = "动态上限（行政能力）";
                chkOiDynamic.Left = 396; chkOiDynamic.Top = 66; chkOiDynamic.AutoSize = true; chkOiDynamic.Checked = true;
                p.Controls.Add(chkOiDynamic);
                p.Controls.Add(chkOiEnabled);

                Label l1 = new Label(); l1.Text = "上限基数："; l1.Left = 12; l1.Top = 100; l1.AutoSize = true;
                numOiMax = new NumericUpDown(); numOiMax.Left = 104; numOiMax.Top = 96; numOiMax.Width = 90;
                numOiMax.Minimum = 0; numOiMax.Maximum = 5000; numOiMax.Value = 500;
                Label h1 = new Label();
                h1.Text = "动态模式下作「基数」（默认 4）；关闭动态则作固定上限";
                h1.Left = 204; h1.Top = 100; h1.AutoSize = true; h1.ForeColor = Color.Gray;
                p.Controls.Add(l1); p.Controls.Add(numOiMax); p.Controls.Add(h1);

                chkOiHideCounter = new CheckBox();
                chkOiHideCounter.Text = "隐藏底部页码点位（推荐）";
                chkOiHideCounter.Left = 12; chkOiHideCounter.Top = 128; chkOiHideCounter.Width = 300; chkOiHideCounter.Checked = true;
                p.Controls.Add(chkOiHideCounter);

                chkOiHideSubtitle = new CheckBox();
                chkOiHideSubtitle.Text = "隐藏面板副标题（原版写死\u201c最多 3 个\u201d）";
                chkOiHideSubtitle.Left = 330; chkOiHideSubtitle.Top = 128; chkOiHideSubtitle.Width = 420; chkOiHideSubtitle.Checked = true;
                p.Controls.Add(chkOiHideSubtitle);

                Button btnApply = new Button(); btnApply.Text = "应用到模组"; btnApply.Left = 12; btnApply.Top = 160; btnApply.Width = 110;
                Button btnDefault = new Button(); btnDefault.Text = "恢复默认（删除开关文件）"; btnDefault.Left = 130; btnDefault.Top = 160; btnDefault.Width = 190;
                Button btnRefresh = new Button(); btnRefresh.Text = "刷新"; btnRefresh.Left = 328; btnRefresh.Top = 160; btnRefresh.Width = 70;
                Button btnOiSelfTest = new Button(); btnOiSelfTest.Text = "桥自检"; btnOiSelfTest.Left = 408; btnOiSelfTest.Top = 160; btnOiSelfTest.Width = 90;
                p.Controls.Add(btnOiSelfTest);
                p.Controls.Add(btnApply); p.Controls.Add(btnDefault); p.Controls.Add(btnRefresh);

                btnApply.Click += delegate
                {
                    bool ok = OiBridge.WriteOverride(chkOiEnabled.Checked, (int)numOiMax.Value,
                        chkOiHideCounter.Checked, chkOiHideSubtitle.Checked,
                        chkOiEcoEnabled.Checked,
                        (int)numOiDanger.Value, (int)numOiUnstable.Value, (int)numOiStable.Value,
                        (int)numOiGrowing.Value, (int)numOiVeryGood.Value,
                        (int)numOiRegionStep.Value, (int)numOiTaxEff.Value, (int)numOiTaxAvoid.Value,
                        (int)numOiGrowthBonus.Value,
                        (int)numOiTourismGood.Value, (int)numOiTourismAvg.Value, (int)numOiTourismBad.Value,
                        (int)numOiTradeInc.Value, (int)numOiTradeRec.Value, (int)numOiTradeDec.Value,
                        (int)numOiTradeDeal.Value,
                        chkOiDynamic.Checked, (int)numOiMax.Value);
                    if (ok) SetStatus("已写入 RP 开关文件（游戏内 ≤1 秒生效；上限在下次打开面板时生效）");
                    else SetStatus("写入 RP 开关文件失败：" + OiBridge.LastError);
                    RefreshOiPanel();
                };
                btnDefault.Click += delegate
                {
                    bool ok = OiBridge.DeleteOverride();
                    oiControlsLoaded = false;
                    if (ok) SetStatus("已删除 RP 开关文件（恢复模组 cfg 配置）");
                    else SetStatus("删除失败：" + OiBridge.LastError);
                    RefreshOiPanel();
                };
                btnRefresh.Click += delegate { oiControlsLoaded = false; RefreshOiPanel(); };

                p.AutoScroll = true;   

                
                GroupBox gbEco = new GroupBox();
                gbEco.Text = "经济结算（ED / 地区 / 旅游 / 贸易 / 税务 / 一次性 · 写入 override 后 ≤1 秒生效）";
                gbEco.Left = 12; gbEco.Top = 196; gbEco.Width = 790; gbEco.Height = 286;
                p.Controls.Add(gbEco);

                chkOiEcoEnabled = new CheckBox();
                chkOiEcoEnabled.Text = "启用经济结算（总开关；取消勾选 = 完全等同原版）";
                chkOiEcoEnabled.Left = 12; chkOiEcoEnabled.Top = 22; chkOiEcoEnabled.Width = 520;
                gbEco.Controls.Add(chkOiEcoEnabled);

                numOiDanger   = MakeEcoNum(gbEco, "危险",     12, 54, -2, 44);
                numOiUnstable = MakeEcoNum(gbEco, "不稳定",  146, 54, -1, 60);
                numOiStable   = MakeEcoNum(gbEco, "稳定",    292, 54,  0, 44);
                numOiGrowing  = MakeEcoNum(gbEco, "增长",    420, 54,  1, 44);
                numOiVeryGood = MakeEcoNum(gbEco, "极快增长", 548, 54,  1, 70);

                numOiRegionStep  = MakeEcoNum(gbEco, "地区步长",      12, 90,  1, 72);
                numOiTaxEff      = MakeEcoNum(gbEco, "税务效率",     210, 90,  1, 72);
                numOiTaxAvoid    = MakeEcoNum(gbEco, "避税(一次性)", 400, 90, -1, 104);
                numOiGrowthBonus = MakeEcoNum(gbEco, "增长(一次性)", 620, 90,  1, 104);

                numOiTourismGood = MakeEcoNum(gbEco, "旅游·蓬勃", 12, 126,  1, 84);
                numOiTourismAvg  = MakeEcoNum(gbEco, "旅游·平均", 196, 126, 0, 84);
                numOiTourismBad  = MakeEcoNum(gbEco, "旅游·衰退", 380, 126, -1, 84);
                numOiTradeDeal   = MakeEcoNum(gbEco, "贸易协议(每份)", 560, 126, 1, 120);

                numOiTradeInc = MakeEcoNum(gbEco, "贸易·增加", 12, 162,  2, 84);
                numOiTradeRec = MakeEcoNum(gbEco, "贸易·恢复", 196, 162, 1, 84);
                numOiTradeDec = MakeEcoNum(gbEco, "贸易·减少", 380, 162, 0, 84);

                Label lbEcoNote = new Label();
                lbEcoNote.Text = "旅游/贸易 = 每回合结算 · 协议/避税/增长 = 一次性 · 改动写入 override 后 ≤1 秒生效";
                lbEcoNote.Left = 12; lbEcoNote.Top = 194; lbEcoNote.AutoSize = true; lbEcoNote.ForeColor = Color.DimGray;
                gbEco.Controls.Add(lbEcoNote);

                lblOiEcoState = new Label();
                lblOiEcoState.Left = 12; lblOiEcoState.Top = 216; lblOiEcoState.Width = 766; lblOiEcoState.Height = 20; lblOiEcoState.AutoSize = false;
                lblOiEcoState.ForeColor = Color.DimGray;
                gbEco.Controls.Add(lblOiEcoState);

                lblOiEcoSettle = new Label();
                lblOiEcoSettle.Left = 12; lblOiEcoSettle.Top = 240; lblOiEcoSettle.Width = 766; lblOiEcoSettle.Height = 40; lblOiEcoSettle.AutoSize = false;
                lblOiEcoSettle.ForeColor = Color.DimGray;
                gbEco.Controls.Add(lblOiEcoSettle);

                Label lv = new Label(); lv.Text = "安济艺术运行状态（只读，每 2 秒刷新）"; lv.Left = 12; lv.Top = 492; lv.AutoSize = true;
                lv.ForeColor = Color.DimGray;
                p.Controls.Add(lv);

                lblOiVars = new Label();
                lblOiVars.Left = 12; lblOiVars.Top = 514; lblOiVars.Width = 760; lblOiVars.Height = 210;
                lblOiVars.Font = new Font("Consolas", 9.5f);
                lblOiVars.BorderStyle = BorderStyle.FixedSingle;
                lblOiVars.Padding = new Padding(6);
                p.Controls.Add(lblOiVars);

                Label hint = new Label();
                hint.Left = 12; hint.Top = 732; hint.Width = 760; hint.Height = 60;
                hint.ForeColor = Color.DimGray;
                hint.Text = "说明：本页仅在检测到 安济艺术的状态文件时出现。开关写入 override 文件，模组每秒轮询生效（无需重启游戏）；"
                          + "上限在\u201c下次打开总统令面板\u201d时生效。存档不会被模组写入——它只改运行时内存，卸载即还原原版。";
                p.Controls.Add(hint);

                Label lblOiSt = new Label();
                lblOiSt.Text = "桥自检结果（点上方「桥自检」按钮生成；只读，可选中复制）";
                lblOiSt.Left = 12; lblOiSt.Top = 802; lblOiSt.AutoSize = true; lblOiSt.ForeColor = Color.DimGray;
                p.Controls.Add(lblOiSt);

                txtOiSelfTest = new TextBox();
                txtOiSelfTest.Left = 12; txtOiSelfTest.Top = 824; txtOiSelfTest.Width = 790; txtOiSelfTest.Height = 180;
                txtOiSelfTest.Multiline = true; txtOiSelfTest.ReadOnly = true; txtOiSelfTest.ScrollBars = ScrollBars.Both;
                txtOiSelfTest.WordWrap = false; txtOiSelfTest.BackColor = Color.WhiteSmoke;
                txtOiSelfTest.Font = new Font("Consolas", 9F);
                txtOiSelfTest.Text = "（尚未执行自检）";
                p.Controls.Add(txtOiSelfTest);

                
                
                
                p.Resize += delegate
                {
                    int vw = p.ClientSize.Width - 26;          
                    if (vw < 220) vw = 220;
                    try { hint.Width = vw; } catch { }
                    try { lblOiVars.Width = vw; } catch { }
                    if (txtOiSelfTest != null) { txtOiSelfTest.Width = vw; txtOiSelfTest.Left = 12; }
                    try
                    {
                        int need = TextRenderer.MeasureText(hint.Text, hint.Font,
                            new Size(vw, 10000), TextFormatFlags.WordBreak).Height + 6;
                        if (need < 40) need = 40;
                        if (hint.Height != need)
                        {
                            hint.Height = need;
                            int y = 732 + need + 8;
                            if (lblOiSt != null) lblOiSt.Top = y;
                            if (txtOiSelfTest != null) txtOiSelfTest.Top = y + 22;
                        }
                    }
                    catch { }
                };
                
                try { p.PerformLayout(); } catch { }

                RefreshOiPanel();
                return p;
            }

            
            NumericUpDown MakeEcoNum(Control parent, string label, int left, int top, int value, int minLabelWidth)
            {
                
                
                Label l = new Label();
                l.Text = label; l.Left = left; l.Top = top + 3; l.AutoSize = true;
                parent.Controls.Add(l);
                int w = Math.Max(minLabelWidth, l.PreferredWidth);
                NumericUpDown n = new NumericUpDown();
                n.Left = left + w + 6; n.Top = top; n.Width = 52;
                n.Minimum = -9; n.Maximum = 9; n.Value = value;
                parent.Controls.Add(n);
                return n;
            }

        
        static string OiTxt(string v)
        {
            return string.IsNullOrEmpty(v) ? "未读到" : v;
        }

        static string OiTurn(string v)
        {
            if (string.IsNullOrEmpty(v)) return "未读到";
            return v == "-1" ? "不适用／未结算" : v;
        }

        static string OiFlag(string v)
        {
            if (string.IsNullOrEmpty(v)) return "未读到";
            return v == "true" ? "真" : "假";
        }

            void RefreshOiPanel()
            {
                if (lblOiStatus == null) return;
                try
                {
                    
                    try { RealTimeBridge.TryPullState(); } catch { }
                    if (!OiBridge.Installed())
                    {
                        lblOiStatus.ForeColor = Color.Firebrick;
                        lblOiStatus.Text = "未检测到 安济艺术（缺少 " + OiBridge.StateName + "）。安装模组并启动一次游戏即可生成。";
                        if (lblOiVars != null) lblOiVars.Text = "桥目录：" + OiBridge.Dir();
                        return;
                    }

                    bool running = OiBridge.Running();
                    DateTime up = OiBridge.UpdatedAt();
                    lblOiStatus.ForeColor = running ? Color.DarkGreen : Color.DimGray;
                    
                    int vwStatus = (lblOiStatus.Parent != null ? lblOiStatus.Parent.ClientSize.Width : 800);
                    bool narrow = vwStatus > 0 && vwStatus < 700;
                    lblOiStatus.Text = (running ? "● 游戏运行中" : "○ 未检测到游戏")
                        + "   模组 " + OiBridge.State("modVersion")
                        + (narrow ? "" : ("   最后更新 " + (up == DateTime.MinValue ? "（未知）" : up.ToString("HH:mm:ss"))
                                          + "   桥目录：" + OiBridge.Dir()));

                    if (!oiControlsLoaded)
                    {
                        oiControlsLoaded = true;
                        chkOiEnabled.Checked = OiBridge.OverrideBool("enabled", true);
                        chkOiDynamic.Checked = OiBridge.OverrideBool("dynamicLimit", true);
                        numOiMax.Value = ClampNum(chkOiDynamic.Checked ? OiBridge.OverrideInt("limitBase", 4) : OiBridge.OverrideInt("maxDecrees", 500), numOiMax);
                        chkOiHideCounter.Checked = OiBridge.OverrideBool("hidePageCounter", true);
                        chkOiHideSubtitle.Checked = OiBridge.OverrideBool("hideSubtitle", true);
                        chkOiEcoEnabled.Checked = OiBridge.OverrideBool("economyEnabled", true);
                        numOiDanger.Value = ClampNum(OiBridge.OverrideInt("dangerDelta", -2), numOiDanger);
                        numOiUnstable.Value = ClampNum(OiBridge.OverrideInt("unstableDelta", -1), numOiUnstable);
                        numOiStable.Value = ClampNum(OiBridge.OverrideInt("stableDelta", 0), numOiStable);
                        numOiGrowing.Value = ClampNum(OiBridge.OverrideInt("growingDelta", 1), numOiGrowing);
                        numOiVeryGood.Value = ClampNum(OiBridge.OverrideInt("veryGoodDelta", 1), numOiVeryGood);
                        numOiRegionStep.Value = ClampNum(OiBridge.OverrideInt("regionStepDelta", 1), numOiRegionStep);
                        numOiTaxEff.Value = ClampNum(OiBridge.OverrideInt("taxEfficientDelta", 1), numOiTaxEff);
                        numOiTaxAvoid.Value = ClampNum(OiBridge.OverrideInt("taxAvoidanceDelta", -1), numOiTaxAvoid);
                        numOiGrowthBonus.Value = ClampNum(OiBridge.OverrideInt("growthBonus", 1), numOiGrowthBonus);
                        numOiTourismGood.Value = ClampNum(OiBridge.OverrideInt("tourismGoodDelta", 1), numOiTourismGood);
                        numOiTourismAvg.Value = ClampNum(OiBridge.OverrideInt("tourismAverageDelta", 0), numOiTourismAvg);
                        numOiTourismBad.Value = ClampNum(OiBridge.OverrideInt("tourismBadDelta", -1), numOiTourismBad);
                        numOiTradeInc.Value = ClampNum(OiBridge.OverrideInt("tradeIncreasedDelta", 2), numOiTradeInc);
                        numOiTradeRec.Value = ClampNum(OiBridge.OverrideInt("tradeRecoveredDelta", 1), numOiTradeRec);
                        numOiTradeDec.Value = ClampNum(OiBridge.OverrideInt("tradeDecreasedDelta", 0), numOiTradeDec);
                        numOiTradeDeal.Value = ClampNum(OiBridge.OverrideInt("tradeDealBonus", 1), numOiTradeDeal);
                    }

                    StringBuilder sb = new StringBuilder();
                    sb.AppendLine("模组当前生效：" + (OiBridge.State("enabled") == "true" ? "是" : "否")
                        + "     总统令上限：" + OiBridge.State("maxDecrees")
                        + "     隐藏点位：" + (OiBridge.State("hidePageCounter") == "true" ? "是" : "否")
                        + "     隐藏副标题：" + (OiBridge.State("hideSubtitle") == "true" ? "是" : "否"));
                    sb.AppendLine("配置来源：" + (OiBridge.State("source") == "override" ? "修改器 override 文件（本页控制）" : "模组 cfg 文件"));
                    sb.AppendLine("最近事件：" + OiBridge.State("reason")
                        + "     面板：" + OiBridge.State("panelName")
                        + "     已签署：" + OiBridge.State("enactedCount") + " 个");
                    sb.AppendLine("状态文件：" + OiBridge.StatePath);
                    sb.AppendLine("开关文件：" + (File.Exists(OiBridge.OverridePath) ? OiBridge.OverridePath : "（不存在，使用模组 cfg 配置）"));
                    sb.AppendLine("──────── 经济结算 ────────");
                    sb.AppendLine("经济结算总开关：" + (OiBridge.State("economyEnabled") == "true" ? "开" : "关")
                        + "     当前经济状态：" + OiTxt(OiBridge.State("economyState"))
                        + "     地区档位：" + OiBridge.State("regionTiers"));
                    sb.AppendLine("税务效率：" + OiFlag(OiBridge.State("taxEfficient"))
                        + "     避税：" + OiFlag(OiBridge.State("taxAvoidance"))
                        + "     增长词条生效：" + OiBridge.State("growthActive") + " 个"
                        + "     当前预算：" + OiTxt(OiBridge.State("budget")));
                    sb.AppendLine("最近结算：回合 " + OiTurn(OiBridge.State("lastSettleTurn"))
                        + "     合计 " + OiBridge.State("lastSettleTotal")
                        + "     明细：" + OiBridge.State("lastSettleDetail"));
                    if (lblOiEcoState != null)
                        lblOiEcoState.Text = "经济状态：" + OiTxt(OiBridge.State("economyState"))
                            + "     地区：" + OiBridge.State("regionTiers")
                            + "     税务效率：" + OiFlag(OiBridge.State("taxEfficient"))
                            + "     避税：" + OiFlag(OiBridge.State("taxAvoidance"))
                            + "     增长词条：" + OiBridge.State("growthActive") + " 个";
                    if (lblOiEcoSettle != null)
                        lblOiEcoSettle.Text = "最近结算：回合 " + OiTurn(OiBridge.State("lastSettleTurn"))
                            + "，合计 " + OiBridge.State("lastSettleTotal")
                            + "     明细：" + OiBridge.State("lastSettleDetail");
                    lblOiVars.Text = sb.ToString();
                }
                catch (Exception e)
                {
                    lblOiStatus.ForeColor = Color.Firebrick;
                    lblOiStatus.Text = "读取 RP 状态失败：" + e.Message;
                }
            }
            static decimal ClampNum(int v, NumericUpDown n)
            {
                if ((decimal)v < n.Minimum) return n.Minimum;
                if ((decimal)v > n.Maximum) return n.Maximum;
                return v;
            }

            void RefreshHbPanel()
            {
                if (lblHbStatus == null) return;
                try
                {
                    try { RealTimeBridge.TryPullState(RealTimeBridge.HbPipeName); } catch { }
                    if (!HbBridge.Installed())
                    {
                        lblHbStatus.ForeColor = Color.Firebrick;
                        lblHbStatus.Text = "未检测到 HB 模组（缺少 " + HbBridge.StateName + "）。启动一次游戏即可生成。";
                        if (lblHbVars != null) lblHbVars.Text = "存档目录：" + HbBridge.Dir();
                        return;
                    }

                    bool running = HbBridge.Running();
                    DateTime up = HbBridge.UpdatedAt();
                    lblHbStatus.ForeColor = running ? Color.DarkGreen : Color.DimGray;
                    int vwHb = (lblHbStatus.Parent != null ? lblHbStatus.Parent.ClientSize.Width : 800);
                    bool narrowHb = vwHb > 0 && vwHb < 700;
                    lblHbStatus.Text = narrowHb
                        ? string.Format("{0} ｜ 模组 {1}", running ? "● 运行中" : "○ 未运行", HbBridge.State("modVersion"))
                        : string.Format("{0} ｜ 模组版本 {1} ｜ 最后更新 {2} ｜ {3}",
                            running ? "● 游戏运行中" : "○ 游戏未运行（以下为上次运行快照）",
                            HbBridge.State("modVersion"),
                            up == DateTime.MinValue ? "未知" : up.ToString("HH:mm:ss"),
                            HbBridge.Dir());

                    if (!hbControlsLoaded)
                    {
                        chkHbEnabled.Checked = HbBridge.OverrideBool("enabled", true);
                        chkHbAuto.Checked = HbBridge.OverrideBool("autoBonusEnabled", true);
                        numHbSupport.Value = ClampNum(HbBridge.OverrideInt("airSupportActions", -1), numHbSupport);
                        numHbStrike.Value = ClampNum(HbBridge.OverrideInt("airBombardmentActions", -1), numHbStrike);
                        hbControlsLoaded = true;
                    }

                    StringBuilder sb = new StringBuilder();
                    sb.AppendLine("编制（R1/R2 来源）：战斗机 " + HbBridge.State("fighters") + " ／ 轰炸机 " + HbBridge.State("bombers"));
                    sb.AppendLine("冷却：空中支援 " + HbBridge.State("supportCooldown") + " ／ 空中轰炸 " + HbBridge.State("strikeCooldown") + "   （1 = 可用，触发加成）");
                    sb.AppendLine("本回合剩余额外次数：支援 " + HbBridge.State("supportRemaining") + " ／ 轰炸 " + HbBridge.State("strikeRemaining"));
                    sb.AppendLine("本回合已授予：R1 " + HbBridge.State("supportGranted") + " ／ R2 " + HbBridge.State("strikeGranted") + " ／ R4 冷却溢出 " + HbBridge.State("overflowGranted"));
                    sb.AppendLine("战争行动点：" + HbBridge.State("actionPoints") + " ／ 每回合 " + HbBridge.State("actionPointsPerTurn"));
                    sb.AppendLine("模组当前生效：" + (HbBridge.State("enabled") == "true" ? "是" : "否") + "   按编制自动加成：" + (HbBridge.State("autoBonusEnabled") == "true" ? "开" : "关"));
                    sb.AppendLine("状态文件：" + HbBridge.StatePath);
                    sb.AppendLine("开关文件：" + (File.Exists(HbBridge.OverridePath) ? HbBridge.OverridePath : "（不存在，使用默认）"));
                    lblHbVars.Text = sb.ToString();
                }
                catch (Exception e)
                {
                    lblHbStatus.Text = "刷新失败：" + e.Message;
                }
            }

    }
}
