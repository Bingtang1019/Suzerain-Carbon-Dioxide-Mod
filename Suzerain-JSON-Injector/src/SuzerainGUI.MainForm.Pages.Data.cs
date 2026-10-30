using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace SuzerainTrainer
{
    
    partial class MainForm
    {
            Control BuildQuickPanel()
            {
                Panel p = new Panel();
                lvQuick = new ListView();
                lvQuick.Dock = DockStyle.Fill; lvQuick.View = View.Details; lvQuick.FullRowSelect = true;
                lvQuick.GridLines = true; lvQuick.MultiSelect = false;
                lvQuick.Columns.Add("常用项", -2); lvQuick.Columns.Add("当前值", 120);
                AttachFavoriteMenu(lvQuick);
                Label hint = new Label(); hint.Dock = DockStyle.Bottom; hint.Height = 26; hint.Padding = new Padding(6, 5, 0, 0);
                hint.Text = "高频修改：核心资源/回合投票/总统令/DLC资源，双击编辑，右键可收藏。";
                hint.ForeColor = Color.DimGray;
                p.Controls.Add(lvQuick); p.Controls.Add(hint);
                lvQuick.DoubleClick += delegate { EditVarListRow(lvQuick); };
                return p;
            }

            void RefreshQuickPanel()
            {
                if (!core.loaded || lvQuick == null) return;
                lvQuick.BeginUpdate(); lvQuick.Items.Clear();
                List<string> keys = new List<string>();
                if (core.storyMode == 1 || core.storyMode == 3)
                {
                    keys.Add("BaseGameSetup.CurrentTurn");
                    keys.Add("BaseGame.GovernmentBudget");
                    keys.Add("BaseGame.Economy");
                    keys.Add("BaseGame.Public_Opinion");
                    keys.Add("BaseGame.Country_Unrest");
                    keys.Add("BaseGame.PersonalWealth");
                    keys.Add("BaseGame.Malenyevist");
                    keys.Add("BaseGame.Sollist");
                    keys.Add("BaseGame.Democratic");
                    keys.Add("BaseGame.Capitalist");
                    keys.Add("BaseGame.Reform_Assembly_Vote");
                    keys.Add("BaseGame.Reform_Court_Vote");
                    keys.AddRange(core.ScanBoolVars("BaseGameUI.PresidentialPower_Condition_"));
                }
                if (core.storyMode == 2 || core.storyMode == 3)
                {
                    keys.Add("RiziaDLCSetup.CurrentTurn");
                    keys.Add("RiziaDLC.Resources_Authority");
                    keys.Add("RiziaDLC.Resources_Budget");
                    keys.Add("RiziaDLC.Resources_Energy");
                    keys.AddRange(core.ScanVars("RiziaDLC.Relations_"));
                }
                HashSet<string> seen = new HashSet<string>();
                foreach (string k in keys)
                {
                    if (!seen.Add(k)) continue;
                    string v = core.GetVar(k);
                    if (v == null) v = core.GetBoolVar(k);
                    if (v == null) v = core.GetStringVar(k);
                    if (v == null) continue;
                    ListViewItem it = new ListViewItem(DisplayName(k));
                    it.Tag = k; it.ToolTipText = k;
                    it.SubItems.Add(FormatVal(v));
                    lvQuick.Items.Add(it);
                }
                lvQuick.EndUpdate();
            }

            
            ListView MakeNoviceList()
            {
                ListView lv = new ListView();
                lv.Dock = DockStyle.Fill; lv.View = View.Details; lv.FullRowSelect = true;
                lv.GridLines = true; lv.MultiSelect = true;
                lv.Columns.Add("项目", -2); lv.Columns.Add("当前值", 120);
                AttachFavoriteMenu(lv);
                lv.DoubleClick += delegate { EditVarListRow(lv); };
                return lv;
            }

            Control BuildNoviceBasePanel()
            {
                TabControl tc = new TabControl();
                tc.Dock = DockStyle.Fill;

                
                Panel eco = new Panel(); eco.Dock = DockStyle.Fill;
                lvNoviceEco = MakeNoviceList();
                Panel ecoTop = new Panel(); ecoTop.Dock = DockStyle.Top; ecoTop.Height = 34;
                Button bClose = new Button(); bClose.Text = "全部危机设为 False"; bClose.Left = 10; bClose.Top = 4; bClose.Width = 160;
                Button bInfra = new Button(); bInfra.Text = "开启H3高速+L1铁路"; bInfra.Left = 178; bInfra.Top = 4; bInfra.Width = 160;
                Label ecoHint = new Label(); ecoHint.Dock = DockStyle.Bottom; ecoHint.Height = 24; ecoHint.Padding = new Padding(6, 4, 0, 0);
                ecoHint.Text = "地区经济直接改数值；危机项设为 false 后永久不判定。";
                ecoHint.ForeColor = Color.DimGray;
                ecoTop.Controls.Add(bClose); ecoTop.Controls.Add(bInfra);
                eco.Controls.Add(lvNoviceEco); eco.Controls.Add(ecoTop); eco.Controls.Add(ecoHint);
                bClose.Click += delegate { SetAllCrisisFalse(); RefreshNoviceBasePanel(); };
                bInfra.Click += delegate { if (MessageBox.Show(this, "将当前存档同时开启 H3高速与 L1铁路，写变量+实体+城市状态。继续？", "基础设施双工程", MessageBoxButtons.YesNo) == DialogResult.Yes) EnableBothInfrastructureProjects(); };
                TabPage tabEco = new TabPage("经济与危机");
                tabEco.Controls.Add(eco);
                tc.TabPages.Add(tabEco);

                
                Panel dip = new Panel(); dip.Dock = DockStyle.Fill;
                lvNoviceDiplo = MakeNoviceList();
                dip.Controls.Add(lvNoviceDiplo);
                TabPage tabDip = new TabPage("外交与结盟");
                tabDip.Controls.Add(dip);
                tc.TabPages.Add(tabDip);

                
                Panel law = new Panel(); law.Dock = DockStyle.Fill;
                lvNoviceLaw = MakeNoviceList();
                law.Controls.Add(lvNoviceLaw);
                TabPage tabLaw = new TabPage("基础与法治");
                tabLaw.Controls.Add(law);
                tc.TabPages.Add(tabLaw);

                return tc;
            }

            void RefreshNoviceBasePanel()
            {
                if (!core.loaded || lvNoviceEco == null) return;
                FillNoviceList(lvNoviceEco, new string[] {
                    "BaseGame.Economy_Agnland", "BaseGame.Economy_Bergia", "BaseGame.Economy_Lorren", "BaseGame.Economy_Gruni",
                    "BaseGame.EconomicReliance",
                    "BaseGame.BlackTuesdayHappened", "BaseGame.SuperpowerTradeWarHappened",
                    "BaseGame.Situation_Economy_GlobalTradeWar", "BaseGame.Situation_Economy_MarketsCrash",
                    "BaseGame.Situation_Economy_DebtCrisis", "BaseGame.Situation_Economy_UnemploymentCrisis",
                    "BaseGame.Situation_Economy_DecreasedTrade", "BaseGame.LateGameCrisis"
                });
                FillNoviceList(lvNoviceDiplo, new string[] {
                    "BaseGame.Valgsland_Alliance", "BaseGame.Lespia_Alliance", "BaseGame.Agnolia_Alliance",
                    "BaseGame.Valgsland_Alliance_Cancelled", "BaseGame.Lespia_Alliance_Cancelled", "BaseGame.Agnolia_Alliance_Cancelled",
                    "BaseGame.Wehlen_TradeDeal", "BaseGame.Agnolia_TradeDeal", "BaseGame.Lespia_TradeDeal", "BaseGame.Valgsland_TradeDeal",
                    "BaseGame.Wehlen_JointOperation", "BaseGame.Wehlen_JoinedWar", "BaseGame.Wehlen_DefendBorder",
                    "BaseGame.Wehlen_Retreated", "BaseGame.Wehlen_PeaceConference",
                    "BaseGame.AgnoliaTradeDeal_Negotiation", "BaseGame.WehlenTradeDeal_Negotiation",
                    "BaseGame.TradeDeal_Negotiation_Lespia", "BaseGame.TradeDeal_Negotiation_Valgsland",
                    "BaseGame.Rumburg_War"
                });
                FillNoviceList(lvNoviceLaw, new string[] {
                    "BaseGameSetup.CurrentTurn",
                    "BaseGame.Reform_Assembly_Vote", "BaseGame.Reform_Court_Vote",
                    "BaseGame.Policy_Law_Decree_Stronger", "BaseGame.Policy_Law_Decree_Default",
                    "BaseGame.Policy_Law_Decree_Weaker", "BaseGame.Policy_Law_Decree_Removed",
                    "BaseGame.Policy_Law_SpecialZoneOfBergia", "BaseGame.Policy_Law_AutonomousZoneOfBergia",
                    "BaseGame.Policy_Law_MinorityRights",
                    "BaseGame.Public_Opinion", "BaseGame.Country_Unrest", "BaseGame.Bludish_Opinion",
                    "BaseGame.PersonalWealth", "BaseGame.SordlandArmySize"
                });
            }

            void FillNoviceList(ListView lv, string[] keys)
            {
                lv.BeginUpdate(); lv.Items.Clear();
                foreach (string k in keys)
                {
                    string v = core.GetVar(k);
                    if (v == null) v = core.GetBoolVar(k);
                    if (v == null) v = core.GetStringVar(k);
                    if (v == null) continue;
                    ListViewItem it = new ListViewItem(DisplayName(k));
                    it.Tag = k; it.ToolTipText = k;
                    it.SubItems.Add(FormatVal(v));
                    lv.Items.Add(it);
                }
                lv.EndUpdate();
            }

            void SetAllCrisisFalse()
            {
                string[] crisisKeys = {
                    "BaseGame.BlackTuesdayHappened", "BaseGame.SuperpowerTradeWarHappened",
                    "BaseGame.Situation_Economy_GlobalTradeWar", "BaseGame.Situation_Economy_MarketsCrash",
                    "BaseGame.Situation_Economy_DebtCrisis", "BaseGame.Situation_Economy_UnemploymentCrisis",
                    "BaseGame.Situation_Economy_DecreasedTrade", "BaseGame.LateGameCrisis"
                };
                int ok = 0;
                foreach (string k in crisisKeys)
                    if (core.SetBoolVar(k, "false")) ok++;
                RefreshAll();
                SetStatus("已关闭 " + ok + " 个危机/贸易战判定开关");
            }

            
            void EnableBothInfrastructureProjects()
            {
                if (!core.loaded) { SetStatus("请先载入存档"); return; }
                if (!core.AllowEntityWrites)
                {
                    if (MessageBox.Show(this,
                        "启用 H3+L1 还会写入 entityUpdates（T01 事件链的 Index 字段）。\n这会超出“默认只写 variables”的范围，是否继续？",
                        "高级实体写入", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
                        return;
                    core.AllowEntityWrites = true;
                }
                int ok = 0;
                string[] boolKeys = {
                    "BaseGame.Turn01_InT_Investment_Highway",
                    "BaseGame.Turn01_InT_Investment_Railway",
                    "BaseGame.Policy_Economy_HighwayProject",
                    "BaseGame.Policy_Economy_RailwayProject",
                    "BaseGameSupport.Journal_Turn01_PickedHighway",
                    "BaseGameSupport.Journal_Turn01_PickedRailway",
                    "BaseGame.Decision_Turn01_InfrastructureProject_Invest"
                };
                foreach (string k in boolKeys)
                    if (core.SetBoolVar(k, "true")) ok++;
                core.SetBoolVar("BaseGame.Decision_Turn01_InfrastructureProject_RejectThePlans", "false");

                
                if (core.SetOrAddEntityValue("Turn01_PickedHighway", "Index", "5")) ok++;
                if (core.SetOrAddEntityValue("Turn01_PickedRailway", "Index", "5")) ok++;

                
                if (core.SetOrAddEntityValue("Progress_Highway", "IsNotificationActive", "True")) ok++;
                if (core.SetOrAddEntityValue("Progress_Railway", "IsNotificationActive", "True")) ok++;

                
                if (core.SetOrAddEntityValue("Sordland_City_Lachaven", "HasNewStatusEffect", "True")) ok++;
                if (core.SetOrAddEntityValue("Sordland_City_Holsord", "HasNewStatusEffect", "True")) ok++;
                AddTokenStatusFreeSlot("Sordland_City_Lachaven", "Progress_Highway");
                AddTokenStatusFreeSlot("Sordland_City_Holsord", "Progress_Railway");

                RefreshAll();
                RefreshNoviceBasePanel();
                SetStatus("已启用 H3高速 + L1铁路（变量/实体/城市状态已写入 " + ok + " 项）");
            }

            void AddTokenStatusFreeSlot(string city, string token)
            {
                List<string> fields = core.ScanEntityFields(city);
                for (int i = 1; i <= 9; i++)
                {
                    string slot = "StatusEffect_" + i;
                    if (!fields.Contains(slot))
                    {
                        core.SetOrAddEntityValue(city, slot, token);
                        return;
                    }
                    string cur = core.GetEntityValue(city, slot);
                    if (cur == null || cur.Length == 0)
                    {
                        core.SetOrAddEntityValue(city, slot, token);
                        return;
                    }
                    if (cur == token) return;
                }
            }

            
            Panel MakeDashCard(string title, string body, int width, int height)
            {
                Panel card = new Panel();
                card.Width = width; card.Height = height;
                card.Margin = new Padding(Ui(6));
                card.BackColor = Color.White;
                card.Paint += delegate(object s, PaintEventArgs e)
                {
                    using (var p = new Pen(E_Border, 1f))
                        e.Graphics.DrawRectangle(p, 0, 0, card.Width - 1, card.Height - 1);
                };
                int pad = Ui(12);
                Label t = new Label(); t.Text = title; t.Left = pad; t.Top = Ui(8); t.Width = width - pad * 2;
                t.AutoEllipsis = true; t.Font = new Font(Font, FontStyle.Bold); t.ForeColor = E_Text;
                Label b = new Label(); b.Text = body; b.Left = pad; b.Top = Ui(30); b.Width = width - pad * 2;
                b.Height = height - Ui(36); b.AutoEllipsis = true; b.ForeColor = E_Muted;
                card.Controls.Add(t); card.Controls.Add(b);
                return card;
            }

            Control BuildDashboardPanel()
            {
                Panel p = new Panel(); p.Dock = DockStyle.Fill; p.AutoScroll = true;
                dashFlow = new FlowLayoutPanel();
                dashFlow.Dock = DockStyle.Fill;
                dashFlow.AutoScroll = true;
                dashFlow.FlowDirection = FlowDirection.TopDown;
                dashFlow.WrapContents = false;
                dashFlow.Padding = new Padding(Ui(6));
                p.Controls.Add(dashFlow);
                
                return p;
            }

            void LoadDashboardFile(string path)
            {
                if (string.IsNullOrEmpty(path) || !File.Exists(path)) { SetStatus("存档不存在: " + path); return; }
                if (!core.LoadFile(path)) { SetStatus("载入失败: " + path); return; }
                AfterLoad();
                RefreshSaveList();
                SetStatus("已载入: " + path);
            }

            void RefreshDashboard()
            {
                if (dashFlow == null) return;
                dashFlow.SuspendLayout();
                dashFlow.Controls.Clear();
                
                
                int flowW = contentHost != null && contentHost.ClientSize.Width > 0
                    ? contentHost.ClientSize.Width - contentHost.Padding.Horizontal
                    : (dashFlow.Parent != null && dashFlow.Parent.ClientSize.Width > 0
                        ? dashFlow.Parent.ClientSize.Width
                        : (dashFlow.ClientSize.Width > 0 ? dashFlow.ClientSize.Width : (int)(596 * dpiScaleF)));
                
                
                int cardW = Math.Min(Math.Max(320, flowW - Ui(32)), Ui(740));
                int fullW = cardW;
                string state = core.loaded
                    ? "已载入：" + Path.GetFileName(core.curFile) + "\n"
                      + "预算 " + (core.GetVar("BaseGame.GovernmentBudget") ?? "—")
                      + " · 舆论 " + (core.GetVar("BaseGame.Public_Opinion") ?? "—")
                      + " · 动乱 " + (core.GetVar("BaseGame.Country_Unrest") ?? "—")
                      + " · 回合 " + (core.GetVar("BaseGameSetup.CurrentTurn") ?? "—")
                    : "尚未载入存档。\n先从下面选一个存档，或点“打开搜索工作台”查找变量。";
                dashFlow.Controls.Add(MakeDashCard("当前局面", state, fullW, Ui(104)));

                string hb = HbBridge.Installed()
                    ? (HbBridge.Running() ? "运行中 · " : "已安装/未运行 · ") + HbBridge.State("modVersion")
                    : "未检测到 HB";
                string oi = OiBridge.Installed()
                    ? (OiBridge.Running() ? "运行中 · " : "已安装/未运行 · ") + OiBridge.State("modVersion")
                    : "未检测到 Oikonomia";
                dashFlow.Controls.Add(MakeDashCard("模组桥", "HB：" + hb + "\nOikonomia：" + oi, cardW, Ui(88)));
                Panel actions = MakeDashCard("快速入口", "搜索变量或打开存档目录", cardW, Ui(96));
                actions.Controls[1].Height = Ui(18);
                Button bSearch = new Button(); bSearch.Text = "搜索工作台"; bSearch.Left = Ui(12); bSearch.Top = Ui(56); bSearch.Width = Ui(105); bSearch.Height = Ui(24);
                bSearch.Click += delegate { ShowSearchWorkbench(); };
                Button bDir = new Button(); bDir.Text = "存档目录"; bDir.Left = Ui(125); bDir.Top = Ui(56); bDir.Width = Ui(100); bDir.Height = Ui(24);
                bDir.Click += delegate { try { System.Diagnostics.Process.Start(TrainerCore.SaveDir()); } catch { } };
                actions.Controls.Add(bSearch); actions.Controls.Add(bDir);
                dashFlow.Controls.Add(actions);

                string dir = TrainerCore.SaveDir();
                if (Directory.Exists(dir))
                {
                    string[] files = Directory.GetFiles(dir, "*.json");
                    Array.Sort(files, delegate(string a, string b) { return File.GetLastWriteTime(b).CompareTo(File.GetLastWriteTime(a)); });
                    int shown = 0;
                    foreach (string f in files)
                    {
                        if (f.EndsWith(".bak", StringComparison.OrdinalIgnoreCase)) continue;
                        if (shown++ >= 6) break;
                        FileInfo fi = new FileInfo(f);
                        string title = Path.GetFileName(f) + (f.StartsWith("Active_") ? "  [Active]" : "");
                        Panel save = MakeDashCard(title,
                            fi.LastWriteTime.ToString("yyyy-MM-dd HH:mm") + " · " + (fi.Length / 1024) + " KB · 点击载入",
                            cardW, Ui(76));
                        string path = f;
                        EventHandler loadHandler = delegate { LoadDashboardFile(path); };
                        save.Cursor = Cursors.Hand;
                        save.Click += loadHandler;
                        foreach (Control sc in save.Controls) sc.Click += loadHandler;
                        dashFlow.Controls.Add(save);
                    }
                    if (shown == 0)
                        dashFlow.Controls.Add(MakeDashCard("存档", "存档目录里还没有 .json 存档。", fullW, Ui(64)));
                }
                dashFlow.ResumeLayout();
                UiApplyPage(pageDashboard);
            }

            
            Control BuildSearchPanel()
            {
                Panel p = new Panel();
                Panel top = new Panel(); top.Dock = DockStyle.Top; top.Height = 66;
                Label l = new Label(); l.Text = "搜索:"; l.Left = 10; l.Top = 9; l.AutoSize = true;
                txtGlobalSearch = new TextBox(); txtGlobalSearch.Left = 52; txtGlobalSearch.Top = 5; txtGlobalSearch.Width = 300;
                Button b = new Button(); b.Text = "搜索"; b.Left = 360; b.Top = 4; b.Width = 60;
                Button b2 = new Button(); b2.Text = "清空"; b2.Left = 428; b2.Top = 4; b2.Width = 60;
                chkSearchOnlyChanged = new CheckBox(); chkSearchOnlyChanged.Text = "只看已改"; chkSearchOnlyChanged.Left = 10; chkSearchOnlyChanged.Top = 38; chkSearchOnlyChanged.AutoSize = true;
                chkSearchOnlyChanged.CheckedChanged += delegate { DoGlobalSearch(); };
                top.Controls.Add(l); top.Controls.Add(txtGlobalSearch); top.Controls.Add(b); top.Controls.Add(b2); top.Controls.Add(chkSearchOnlyChanged);
                lvSearch = new ListView(); lvSearch.Dock = DockStyle.Fill; lvSearch.View = View.Details; lvSearch.FullRowSelect = true;
                lvSearch.GridLines = true; lvSearch.MultiSelect = false;
                lvSearch.Columns.Add("类型", 70); lvSearch.Columns.Add("名称/键", -2); lvSearch.Columns.Add("值", 110);
                AttachFavoriteMenu(lvSearch);

                Panel drawer = new Panel(); drawer.Dock = DockStyle.Bottom; drawer.Height = 142;
                Label dl = new Label(); dl.Text = "改动抽屉（双击结果编辑，下面可撤销最近一步）"; dl.Dock = DockStyle.Top; dl.Height = 22; dl.Padding = new Padding(6, 4, 0, 0); dl.ForeColor = E_Muted;
                lvSearchChanges = new ListView(); lvSearchChanges.Dock = DockStyle.Fill; lvSearchChanges.View = View.Details; lvSearchChanges.FullRowSelect = true;
                lvSearchChanges.Columns.Add("类型", 60); lvSearchChanges.Columns.Add("键/实体", -2); lvSearchChanges.Columns.Add("旧值", 90); lvSearchChanges.Columns.Add("新值", 90);
                Button undo = new Button(); undo.Text = "撤销最近一步"; undo.Dock = DockStyle.Bottom; undo.Height = 26;
                undo.Click += delegate { if (core.UndoLast()) { RefreshAll(); RefreshHistoryPanel(); SetStatus("已撤销上一步"); } else SetStatus("没有可撤销的修改"); };
                drawer.Controls.Add(lvSearchChanges); drawer.Controls.Add(undo); drawer.Controls.Add(dl);

                p.Controls.Add(lvSearch); p.Controls.Add(drawer); p.Controls.Add(top);
                b.Click += delegate { DoGlobalSearch(); };
                b2.Click += delegate { txtGlobalSearch.Text = ""; lvSearch.Items.Clear(); RefreshSearchChanges(); };
                txtGlobalSearch.TextChanged += delegate
                {
                    if (searchDebounce == null)
                    {
                        searchDebounce = new Timer(); searchDebounce.Interval = 250;
                        searchDebounce.Tick += delegate { searchDebounce.Stop(); DoGlobalSearch(); };
                    }
                    searchDebounce.Stop(); searchDebounce.Start();
                };
                txtGlobalSearch.KeyDown += delegate(object s, KeyEventArgs e) { if (e.KeyCode == Keys.Enter) { DoGlobalSearch(); e.SuppressKeyPress = true; } };
                lvSearch.DoubleClick += delegate
                {
                    if (lvSearch.SelectedItems.Count == 0) return;
                    string tag = lvSearch.SelectedItems[0].Tag as string;
                    if (tag != null && tag.StartsWith("entity:"))
                    {
                        string entity = tag.Substring(7);
                        string field = InputBox("修改实体字段", entity + "  要修改的字段名:", "");
                        if (field == null || field.Length == 0) return;
                        string cur = core.GetEntityValue(entity, field);
                        if (cur == null) { SetStatus("该实体没有此字段: " + entity + "." + field); return; }
                        string input = InputBox("修改字段", entity + "  " + field + "  (当前=" + FormatVal(cur) + ")", FormatVal(cur));
                        if (input == null) return;
                        if (core.SetEntityValue(entity, field, input)) { RefreshAll(); RefreshHistoryPanel(); SetStatus(entity + "." + field + " -> " + input); }
                        else SetStatus("修改失败: " + entity + "." + field);
                    }
                    else EditVarListRow(lvSearch);
                };
                return p;
            }

            void RefreshSearchChanges()
            {
                if (lvSearchChanges == null) return;
                lvSearchChanges.BeginUpdate();
                lvSearchChanges.Items.Clear();
                if (core.loaded)
                {
                    for (int i = 0; i < core.Changes.Count; i++)
                    {
                        TrainerCore.ChangeRec c = core.Changes[i];
                        ListViewItem it = new ListViewItem(c.Kind);
                        it.SubItems.Add(c.Kind == "entity" ? c.Key + "." + c.Field : c.Key);
                        it.SubItems.Add(c.OldValue ?? "");
                        it.SubItems.Add(c.NewValue ?? "");
                        lvSearchChanges.Items.Add(it);
                    }
                }
                lvSearchChanges.EndUpdate();
            }

            bool SearchKeyChanged(string key)
            {
                for (int i = 0; i < core.Changes.Count; i++)
                {
                    TrainerCore.ChangeRec c = core.Changes[i];
                    if (c.Key == key) return true;
                }
                return false;
            }

            void DoGlobalSearch()
            {
                lvSearch.BeginUpdate();
                lvSearch.Items.Clear();
                if (!core.loaded) { lvSearch.EndUpdate(); return; }
                RefreshSearchChanges();
                string q = txtGlobalSearch.Text.Trim();
                bool onlyChanged = chkSearchOnlyChanged != null && chkSearchOnlyChanged.Checked;
                if (q.Length == 0 && !onlyChanged) { lvSearch.EndUpdate(); return; }
                HashSet<string> seen = new HashSet<string>();
                
                foreach (string pr in ALL_PREFIXES)
                {
                    if (!PrefixAllowed(pr)) continue;
                    List<string> keys = new List<string>();
                    keys.AddRange(core.ScanVars(pr));
                    keys.AddRange(core.ScanBoolVars(pr));
                    keys.AddRange(core.ScanStringVars(pr));
                    foreach (string k in keys)
                    {
                        if (!seen.Add(k)) continue;
                        if (onlyChanged && !SearchKeyChanged(k)) continue;
                        if (q.Length > 0 &&
                            k.IndexOf(q, StringComparison.OrdinalIgnoreCase) < 0 &&
                            DisplayName(k).IndexOf(q, StringComparison.OrdinalIgnoreCase) < 0) continue;
                        string v = core.GetVar(k);
                        if (v == null) v = core.GetBoolVar(k);
                        if (v == null) v = core.GetStringVar(k);
                        if (v == null) continue;
                        ListViewItem it = new ListViewItem("变量");
                        it.Tag = k; it.ToolTipText = k;
                        it.SubItems.Add(DisplayName(k));
                        it.SubItems.Add(FormatVal(v));
                        lvSearch.Items.Add(it);
                    }
                }
                
                foreach (string e in core.ScanEntities())
                {
                    if (onlyChanged && !SearchKeyChanged(e)) continue;
                    if (q.Length > 0 && e.IndexOf(q, StringComparison.OrdinalIgnoreCase) < 0) continue;
                    if (seen.Contains("entity:" + e)) continue;
                    seen.Add("entity:" + e);
                    ListViewItem it = new ListViewItem("实体");
                    it.Tag = "entity:" + e; it.ToolTipText = e;
                    it.SubItems.Add(e);
                    it.SubItems.Add("");
                    lvSearch.Items.Add(it);
                }
                lvSearch.EndUpdate();
                SetStatus("搜索完成: " + lvSearch.Items.Count + " 条结果");
            }

            
            Control BuildFavoritesPanel()
            {
                Panel p = new Panel();
                lvFavorites = new ListView();
                lvFavorites.Dock = DockStyle.Fill; lvFavorites.View = View.Details; lvFavorites.FullRowSelect = true;
                lvFavorites.GridLines = true; lvFavorites.MultiSelect = false;
                lvFavorites.Columns.Add("收藏项", -2); lvFavorites.Columns.Add("当前值", 120);
                AttachFavoriteMenu(lvFavorites);
                Label hint = new Label(); hint.Dock = DockStyle.Bottom; hint.Height = 24; hint.Padding = new Padding(6, 4, 0, 0);
                hint.Text = "在其他列表右键“收藏/取消收藏”，这里只显示当前故事包内的收藏。";
                hint.ForeColor = Color.DimGray;
                p.Controls.Add(lvFavorites); p.Controls.Add(hint);
                lvFavorites.DoubleClick += delegate { EditVarListRow(lvFavorites); };
                return p;
            }

            void RefreshFavoritesPanel()
            {
                if (!core.loaded || lvFavorites == null) return;
                lvFavorites.BeginUpdate(); lvFavorites.Items.Clear();
                List<string> list = new List<string>(favorites);
                list.Sort();
                foreach (string k in list)
                {
                    if (!IsAllowedInCurrentStory(k)) continue;
                    string v = core.GetVar(k);
                    if (v == null) v = core.GetBoolVar(k);
                    if (v == null) v = core.GetStringVar(k);
                    if (v == null) continue;
                    ListViewItem it = new ListViewItem(DisplayName(k));
                    it.Tag = k; it.ToolTipText = k;
                    it.SubItems.Add(FormatVal(v));
                    lvFavorites.Items.Add(it);
                }
                lvFavorites.EndUpdate();
            }

            
            Control BuildHistoryPanel()
            {
                Panel p = new Panel();
                lvHistory = new ListView();
                lvHistory.Dock = DockStyle.Fill; lvHistory.View = View.Details; lvHistory.FullRowSelect = true;
                lvHistory.GridLines = true; lvHistory.MultiSelect = false;
                lvHistory.Columns.Add("类型", 60); lvHistory.Columns.Add("键/实体", -2); lvHistory.Columns.Add("旧值", 90); lvHistory.Columns.Add("新值", 90);
                Label hint = new Label(); hint.Dock = DockStyle.Bottom; hint.Height = 24; hint.Padding = new Padding(6, 4, 0, 0);
                hint.Text = "未保存的修改历史；底部【撤销上一步】或重新载入可回滚。";
                hint.ForeColor = Color.DimGray;
                p.Controls.Add(lvHistory); p.Controls.Add(hint);
                return p;
            }

            void RefreshHistoryPanel()
            {
                if (!core.loaded || lvHistory == null) return;
                lvHistory.BeginUpdate(); lvHistory.Items.Clear();
                for (int i = 0; i < core.Changes.Count; i++)
                {
                    TrainerCore.ChangeRec c = core.Changes[i];
                    ListViewItem it = new ListViewItem(c.Kind);
                    it.SubItems.Add(c.Kind == "entity" ? c.Key + "." + c.Field : c.Key);
                    it.SubItems.Add(c.OldValue ?? "");
                    it.SubItems.Add(c.NewValue ?? "");
                    lvHistory.Items.Add(it);
                }
                lvHistory.EndUpdate();
            }

            
            string PresetsDir()
            {
                return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SuzerainTrainer", "presets");
            }

            Control BuildPresetsPanel()
            {
                Panel p = new Panel();
                Panel top = new Panel(); top.Dock = DockStyle.Top; top.Height = 34;
                Button bSave = new Button(); bSave.Text = "保存当前修改为方案"; bSave.Left = 10; bSave.Top = 4; bSave.Width = 150;
                Button bApply = new Button(); bApply.Text = "应用选中方案"; bApply.Left = 168; bApply.Top = 4; bApply.Width = 110;
                Button bRefresh = new Button(); bRefresh.Text = "刷新"; bRefresh.Left = 286; bRefresh.Top = 4; bRefresh.Width = 70;
                Button bDelete = new Button(); bDelete.Text = "删除"; bDelete.Left = 364; bDelete.Top = 4; bDelete.Width = 70;
                top.Controls.Add(bSave); top.Controls.Add(bApply); top.Controls.Add(bRefresh); top.Controls.Add(bDelete);
                lvPresets = new ListView(); lvPresets.Dock = DockStyle.Fill; lvPresets.View = View.Details; lvPresets.FullRowSelect = true;
                lvPresets.GridLines = true; lvPresets.MultiSelect = false;
                lvPresets.Columns.Add("方案名", 300); lvPresets.Columns.Add("文件", -2);
                Label hint = new Label(); hint.Dock = DockStyle.Bottom; hint.Height = 24; hint.Padding = new Padding(6, 4, 0, 0);
                hint.Text = "方案保存在 %APPDATA%\\SuzerainTrainer\\presets；双击或点“应用选中方案”即可套用到当前存档。";
                hint.ForeColor = Color.DimGray;
                p.Controls.Add(lvPresets); p.Controls.Add(top); p.Controls.Add(hint);
                bSave.Click += delegate { SaveCurrentAsPreset(); };
                bApply.Click += delegate { ApplySelectedPreset(); };
                bRefresh.Click += delegate { RefreshPresetsPanel(); };
                bDelete.Click += delegate { DeleteSelectedPreset(); };
                lvPresets.DoubleClick += delegate { ApplySelectedPreset(); };
                return p;
            }

            void RefreshPresetsPanel()
            {
                if (lvPresets == null) return;
                lvPresets.BeginUpdate(); lvPresets.Items.Clear();
                try
                {
                    Directory.CreateDirectory(PresetsDir());
                    string[] files = Directory.GetFiles(PresetsDir(), "*.txt");
                    Array.Sort(files);
                    foreach (string f in files)
                    {
                        string name = Path.GetFileNameWithoutExtension(f);
                        ListViewItem it = new ListViewItem(name);
                        it.Tag = f;
                        it.SubItems.Add(Path.GetFileName(f));
                        lvPresets.Items.Add(it);
                    }
                }
                catch { }
                lvPresets.EndUpdate();
            }

            void SaveCurrentAsPreset()
            {
                if (!core.loaded || core.ChangeCount == 0) { SetStatus("当前没有未保存修改，无法保存方案"); return; }
                string name = InputBox("保存修改方案", "方案名称:", "");
                if (name == null || name.Length == 0) return;
                string safe = string.Concat(name.Split(Path.GetInvalidFileNameChars()));
                if (safe.Length == 0) safe = "preset";
                string path = Path.Combine(PresetsDir(), safe + ".txt");
                List<string> lines = new List<string>();
                lines.Add("#" + name);
                foreach (TrainerCore.ChangeRec c in core.Changes)
                {
                    if (c.Kind == "entity")
                        lines.Add(c.Kind + "\t" + c.Key + "\t" + c.Field + "\t" + (c.NewValue ?? ""));
                    else
                        lines.Add(c.Kind + "\t" + c.Key + "\t\t" + (c.NewValue ?? ""));
                }
                try
                {
                    Directory.CreateDirectory(PresetsDir());
                    File.WriteAllLines(path, lines, Encoding.UTF8);
                    RefreshPresetsPanel();
                    SetStatus("已保存方案: " + name);
                }
                catch (Exception e) { SetStatus("保存方案失败: " + e.Message); }
            }

            void ApplySelectedPreset()
            {
                if (lvPresets.SelectedItems.Count == 0 || !core.loaded) return;
                string path = lvPresets.SelectedItems[0].Tag as string;
                if (path == null || !File.Exists(path)) { SetStatus("方案文件不存在"); return; }
                int ok = 0;
                foreach (string line in File.ReadAllLines(path, Encoding.UTF8))
                {
                    if (line.Trim().Length == 0 || line.StartsWith("#")) continue;
                    string[] parts = line.Split('\t');
                    if (parts.Length < 2) continue;
                    string kind = parts[0];
                    string key = parts[1];
                    string field = parts.Length > 2 ? parts[2] : "";
                    string value = parts.Length > 3 ? parts[3] : "";
                    bool done = false;
                    if (kind == "var") done = core.SetVar(key, value);
                    else if (kind == "bool") done = core.SetBoolVar(key, value);
                    else if (kind == "string") done = core.SetStringVar(key, value);
                    else if (kind == "entity") done = core.SetEntityValue(key, field, value);
                    if (done) ok++;
                }
                RefreshAll();
                RefreshHistoryPanel();
                SetStatus("已应用方案: " + Path.GetFileNameWithoutExtension(path) + "，成功 " + ok + " 项");
            }

            void DeleteSelectedPreset()
            {
                if (lvPresets.SelectedItems.Count == 0) return;
                string path = lvPresets.SelectedItems[0].Tag as string;
                if (path == null || !File.Exists(path)) return;
                if (MessageBox.Show(this, "删除方案文件?", "删除", MessageBoxButtons.YesNo) != DialogResult.Yes) return;
                try { File.Delete(path); } catch { }
                RefreshPresetsPanel();
            }

            
            Control BuildSaveBatchPanel()
            {
                Panel p = new Panel();
                Panel top = new Panel(); top.Dock = DockStyle.Top; top.Height = 34;
                Button bApply = new Button(); bApply.Text = "同步当前修改到选中存档"; bApply.Left = 10; bApply.Top = 4; bApply.Width = 190;
                Button bRefresh = new Button(); bRefresh.Text = "刷新"; bRefresh.Left = 208; bRefresh.Top = 4; bRefresh.Width = 70;
                top.Controls.Add(bApply); top.Controls.Add(bRefresh);
                lvSaveBatch = new ListView(); lvSaveBatch.Dock = DockStyle.Fill; lvSaveBatch.View = View.Details; lvSaveBatch.FullRowSelect = true;
                lvSaveBatch.GridLines = true; lvSaveBatch.MultiSelect = true;
                lvSaveBatch.Columns.Add("存档文件", 190); lvSaveBatch.Columns.Add("故事包", -2); lvSaveBatch.Columns.Add("保存名", 110); lvSaveBatch.Columns.Add("修改时间", 120);
                Label hint = new Label(); hint.Dock = DockStyle.Bottom; hint.Height = 24; hint.Padding = new Padding(6, 4, 0, 0);
                hint.Text = "可多选存档；把当前未保存修改批量写入选中的存档。";
                hint.ForeColor = Color.DimGray;
                p.Controls.Add(lvSaveBatch); p.Controls.Add(top); p.Controls.Add(hint);
                bApply.Click += delegate { SyncSelectedSavesFromBatch(); };
                bRefresh.Click += delegate { RefreshSaveBatchPanel(); };
                return p;
            }

            void RefreshSaveBatchPanel()
            {
                if (lvSaveBatch == null) return;
                lvSaveBatch.BeginUpdate(); lvSaveBatch.Items.Clear();
                string dir = TrainerCore.SaveDir();
                if (!Directory.Exists(dir)) { lvSaveBatch.EndUpdate(); return; }
                string[] files = Directory.GetFiles(dir, "*.json");
                Array.Sort(files, delegate(string a, string b) { return File.GetLastWriteTime(b).CompareTo(File.GetLastWriteTime(a)); });
                foreach (string f in files)
                {
                    string story = GetSaveStoryPack(f);
                    string saveName = ReadSaveField(f, "saveFileName");
                    string lm = ReadSaveField(f, "lastModified");
                    ListViewItem it = new ListViewItem(Path.GetFileName(f));
                    it.Tag = f;
                    it.SubItems.Add(story ?? "?");
                    it.SubItems.Add(saveName ?? "?");
                    it.SubItems.Add(lm ?? File.GetLastWriteTime(f).ToString("yyyy-MM-dd HH:mm:ss"));
                    lvSaveBatch.Items.Add(it);
                }
                lvSaveBatch.EndUpdate();
            }

            void SyncSelectedSavesFromBatch()
            {
                if (!core.loaded) { SetStatus("请先载入存档"); return; }
                if (core.ChangeCount == 0) { SetStatus("当前没有未保存修改，无法同步"); return; }
                if (lvSaveBatch.SelectedItems.Count == 0) { SetStatus("请选择要同步的存档"); return; }
                int ok = 0;
                foreach (ListViewItem it in lvSaveBatch.SelectedItems)
                {
                    string path = it.Tag as string;
                    if (path != null && SyncChangesToFile(path)) ok++;
                }
                RefreshAll();
                SetStatus("批量同步完成: " + ok + "/" + lvSaveBatch.SelectedItems.Count + " 个存档");
            }

            
            bool ValidateCurrentSave()
            {
                if (core.json == null || core.json.Length < 20) return false;
                string j = core.json.Trim();
                if (j[0] != '{' || j[j.Length - 1] != '}') return false;
                if (j.IndexOf("\"variables\"", StringComparison.Ordinal) < 0) return false;
                if (j.IndexOf("\"entityUpdates\"", StringComparison.Ordinal) < 0) return false;
                return true;
            }

            
            bool ShowSavePreview()
            {
                if (core.ChangeCount == 0)
                    return MessageBox.Show(this, "没有未保存的修改。仍要直接保存吗？", "保存确认", MessageBoxButtons.YesNo) == DialogResult.Yes;
                Form f = new Form();
                f.Text = "保存差异预览";
                f.StartPosition = FormStartPosition.CenterParent;
                f.ClientSize = new Size(640, 420);
                f.Font = Font;
                ListView lv = new ListView(); lv.Dock = DockStyle.Fill; lv.View = View.Details; lv.FullRowSelect = true;
                lv.GridLines = true;
                lv.Columns.Add("类型", 70); lv.Columns.Add("键/实体", 300); lv.Columns.Add("旧值", 130); lv.Columns.Add("新值", 130);
                foreach (TrainerCore.ChangeRec c in core.Changes)
                {
                    ListViewItem it = new ListViewItem(c.Kind);
                    it.SubItems.Add(c.Kind == "entity" ? c.Key + "." + c.Field : c.Key);
                    it.SubItems.Add(c.OldValue ?? "");
                    it.SubItems.Add(c.NewValue ?? "");
                    lv.Items.Add(it);
                }
                Panel bottom = new Panel(); bottom.Dock = DockStyle.Bottom; bottom.Height = 38;
                Button ok = new Button(); ok.Text = "确认保存"; ok.DialogResult = DialogResult.OK; ok.Left = 440; ok.Top = 6; ok.Width = 90;
                Button cc = new Button(); cc.Text = "取消"; cc.DialogResult = DialogResult.Cancel; cc.Left = 538; cc.Top = 6; cc.Width = 80;
                bottom.Controls.Add(ok); bottom.Controls.Add(cc);
                f.Controls.Add(lv); f.Controls.Add(bottom);
                f.AcceptButton = ok; f.CancelButton = cc;
                ScaleDialog(f);
                return f.ShowDialog(this) == DialogResult.OK;
            }

            
            string ReadSaveField(string path, string field)
            {
                try
                {
                    using (StreamReader sr = new StreamReader(path, Encoding.UTF8))
                    {
                        char[] buf = new char[4096];
                        int n = sr.ReadBlock(buf, 0, buf.Length);
                        string head = new string(buf, 0, n);
                        Match m = Regex.Match(head, "\"" + field + "\":\\s*\"([^\"]+)\"");
                        return m.Success ? m.Groups[1].Value : null;
                    }
                }
                catch { return null; }
            }

            DateTime? SaveTimestamp(string path)
            {
                string lm = ReadSaveField(path, "lastModified");
                DateTime dt;
                if (lm != null && DateTime.TryParseExact(lm, "dd-MM-yyyy_HH-mm-ss", CultureInfo.InvariantCulture, DateTimeStyles.None, out dt))
                    return dt;
                try { return File.GetLastWriteTime(path); }
                catch { return null; }
            }

            string GetSaveStoryPack(string path)
            {
                return ReadSaveField(path, "currentStoryPack");
            }

            string FindActiveFileForCurrentStory()
            {
                string dir = TrainerCore.SaveDir();
                string[] files = Directory.GetFiles(dir, "Active_*.json");
                foreach (string f in files)
                    if (GetSaveStoryPack(f) == core.storyPack) return f;
                return null;
            }

            string FindLatestNonActiveFileForCurrentStory()
            {
                string dir = TrainerCore.SaveDir();
                string[] files = Directory.GetFiles(dir, "*.json");
                DateTime? best = null;
                string bestPath = null;
                foreach (string f in files)
                {
                    string name = Path.GetFileName(f);
                    if (name.StartsWith("Active_")) continue;
                    if (name.EndsWith(".bak")) continue;
                    if (GetSaveStoryPack(f) != core.storyPack) continue;
                    DateTime? ts = SaveTimestamp(f);
                    if (ts == null) continue;
                    if (best == null || ts > best) { best = ts; bestPath = f; }
                }
                return bestPath;
            }

            bool IsCurrentFileLatest()
            {
                if (core.curFile == null) return false;
                DateTime? cur = SaveTimestamp(core.curFile);
                if (cur == null) return false;
                string dir = TrainerCore.SaveDir();
                string[] files = Directory.GetFiles(dir, "*.json");
                foreach (string f in files)
                {
                    if (f == core.curFile) continue;
                    if (Path.GetFileName(f).EndsWith(".bak")) continue;
                    DateTime? ts = SaveTimestamp(f);
                    if (ts != null && ts > cur) return false;
                }
                return true;
            }

            List<string> GetAutoSyncTargets()
            {
                List<string> targets = new List<string>();
                if (!core.loaded || core.curFile == null) return targets;
                bool isActive = Path.GetFileName(core.curFile).StartsWith("Active_");
                if (isActive)
                {
                    
                    string latest = FindLatestNonActiveFileForCurrentStory();
                    if (latest != null && latest != core.curFile) targets.Add(latest);
                }
                else if (IsCurrentFileLatest())
                {
                    
                    string active = FindActiveFileForCurrentStory();
                    if (active != null && active != core.curFile) targets.Add(active);
                }
                return targets;
            }

            int SyncChangesToTargets(List<string> targets)
            {
                int ok = 0;
                foreach (string path in targets)
                {
                    if (SyncChangesToFile(path)) ok++;
                    else SetStatus("同步失败: " + path);
                }
                return ok;
            }

    }
}
