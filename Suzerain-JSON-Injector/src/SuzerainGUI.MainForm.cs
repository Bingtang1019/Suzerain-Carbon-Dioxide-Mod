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
    partial class MainForm : Form
    {
        TrainerCore core = new TrainerCore();
        ComboBox cmbSave;
        internal TreeView navTree;     
        Panel contentHost;             
        SplitContainer mainSplit;
        float dpiScaleF = 1f;          
        Label lblStory;
        Label lblStatus;
        CheckBox chkRaw;
        CheckBox chkAdvanced;
        bool ShowRaw { get { return chkRaw != null && chkRaw.Checked; } }
        bool AdvancedMode { get { return chkAdvanced != null && chkAdvanced.Checked; } }
        
        List<NumericUpDown> resNums = new List<NumericUpDown>();
        List<NumericUpDown> hudNums = new List<NumericUpDown>();
        bool loadingSaveList;
        List<string> resKeys = new List<string>();
        NumericUpDown numAuthority, numBudget, numEnergy, numTurn, numAssembly, numCourt;
        TextBox txtCustomKey; NumericUpDown numCustomVal;
        
        class FilterPage
        {
            public string title;
            public List<string> prefixes = new List<string>();
            public List<string> allPrefixes = new List<string>(); 
            public string[] keywords;
            public ListView lv;
            public ComboBox cmbPrefix;
            public TextBox txtSearch;
            public int page = 0;
            public int pageSize = 800;
            public bool paged = false;
            public Label lblPage;
            public Button btnPrev, btnNext;
        }
        FilterPage fpDiplo, fpRelation, fpAll, fpDecree;
        ListView lvEvents;
        ComboBox cmbEvPrefix, cmbEvTurn;
        ComboBox cmbEntity; TextBox txtEntitySearch; ListView lvEntity;
        ListView lvModifier;
        
        Control pageDashboard, pageQuick, pageSearch, pageFavorites, pageHistory, pagePresets, pageSaveBatch;
        FlowLayoutPanel dashFlow;
        ListView lvSearchChanges;
        CheckBox chkSearchOnlyChanged;
        Timer searchDebounce;
        Control pageBase, pageDiplo, pageRelation, pageDecree, pageEvent, pageResource, pageOther;
        Control pageNoviceBase;
        ListView lvQuick, lvSearch, lvFavorites, lvHistory, lvPresets, lvSaveBatch;
        ListView lvNoviceEco, lvNoviceDiplo, lvNoviceLaw;
        TextBox txtGlobalSearch;
        HashSet<string> favorites = new HashSet<string>();

        
        Control pageHb;
        Label lblHbStatus, lblHbVars;
        CheckBox chkHbEnabled, chkHbAuto;
        NumericUpDown numHbSupport, numHbStrike;
        Timer hbTimer;
        bool hbControlsLoaded;

        
        Control pageOi;
        Label lblOiStatus, lblOiVars;
        CheckBox chkOiEnabled, chkOiHideCounter, chkOiHideSubtitle, chkOiDynamic;
        NumericUpDown numOiMax;
        
        CheckBox chkOiEcoEnabled;
        TextBox txtOiSelfTest;                  
        NumericUpDown numOiDanger, numOiUnstable, numOiStable, numOiGrowing, numOiVeryGood;
        NumericUpDown numOiTourismGood, numOiTourismAvg, numOiTourismBad;
        NumericUpDown numOiTradeInc, numOiTradeRec, numOiTradeDec;
        NumericUpDown numOiTradeDeal;
        NumericUpDown numOiRegionStep, numOiTaxEff, numOiTaxAvoid, numOiGrowthBonus;
        Label lblOiEcoState, lblOiEcoSettle;
        bool oiControlsLoaded;

        
        static readonly string[] ALL_PREFIXES = {
            "BaseGame.", "BaseGameIsolated.", "BaseGameSetup.", "BaseGameSupport.", "BaseGameUI.", "GameCondition.",
            "RiziaDLC.", "RiziaDLCIsolated.", "RiziaDLCSetup.", "RiziaDLCSupport.", "RiziaDLCUI.", "SharedSupport." };
        static readonly string[] DIPLO_PREFIXES = {
            "BaseGame.Policy_Diplomacy_", "BaseGame.Situation_Diplomacy_", "BaseGame.Valgsland_Alliance", "BaseGame.Lespia_Alliance",
            "BaseGame.Agnolia_Alliance", "BaseGame.Valgsland_TradeDeal", "BaseGame.Lespia_TradeDeal", "BaseGame.Agnolia_TradeDeal",
            "BaseGame.Wehlen_TradeDeal", "BaseGame.Joined_", "BaseGame.Rumburg_War", "BaseGame.RumburgWar_", "BaseGame.Rumburg_",
            "BaseGame.TradeAmount", "BaseGame.TradeDeal_", "BaseGame.AgnoliaTradeDeal_", "BaseGame.WehlenTradeDeal_",
            "BaseGame.UnitedContana_", "BaseGame.Arcasia_", "BaseGame.Promise_Diplomacy",
            "RiziaDLC.Diplomacy_", "RiziaDLC.CountryRelation_", "RiziaDLC.MITZ_", "RiziaDLC.Policy_Diplomacy_",
            "RiziaDLC.Situation_Diplomacy_", "RiziaDLC.TradeDeal_", "RiziaDLC.War_", "RiziaDLC.Resources_Military_",
            "RiziaDLC.Derdia_", "RiziaDLC.Pales_", "RiziaDLC.Wehlen_", "RiziaDLC.Morella_", "RiziaDLC.Rumburg_",
            "RiziaDLC.Sordland_", "RiziaDLC.Energy_ProjectedIncome", "RiziaDLC.Energy_Promised" };
        static readonly string[] RELATION_PREFIXES = {
            "BaseGame.Relations_", "BaseGameIsolated.", "RiziaDLC.Relations_", "RiziaDLC.Country_", "RiziaDLC.Lucita_",
            "RiziaDLC.Pabel_", "RiziaDLC.Vina_" };
        static readonly string[] RELATION_KEYWORDS = {
            "Opinion", "Relation", "Like", "Trust", "Loyalty", "Approval", "Romance", "Together", "Relationship", "Promise" };
        static readonly string[] DECREE_PREFIXES = {
            "BaseGameUI.PresidentialPower_Condition_" };
        static readonly string[] DECREE_KEYWORDS = new string[] { };

        bool PrefixAllowed(string prefix)
        {
            if (core.storyMode <= 0 || core.storyMode == 3) return true;
            if (core.storyMode == 1)
                return prefix.IndexOf("BaseGame") >= 0 || prefix.IndexOf("GameCondition") >= 0 || prefix.IndexOf("SharedSupport") >= 0;
            return prefix.IndexOf("RiziaDLC") >= 0 || prefix.IndexOf("SharedSupport") >= 0;
        }

        public MainForm(string startFile)
        {
            Text = "Suzerain 存档修改器 " + BuildInfo.Version + " GUI";
            Font = new Font("Microsoft YaHei UI", 9F);
            
            
            AutoScaleMode = AutoScaleMode.None;
            ClientSize = new Size(820, 520);
            MinimumSize = new Size(700, 460);
            StartPosition = FormStartPosition.CenterScreen;
            KeyPreview = true;
            LoadFavorites();
            BuildUI();
            
            hbTimer = new Timer();
            hbTimer.Interval = 2000;
            hbTimer.Tick += delegate
            {
                try { if (pageHb != null && pageHb.Visible) RefreshHbPanel(); }
                catch { }
                try { if (pageOi != null && pageOi.Visible) RefreshOiPanel(); }
                catch { }
            };
            hbTimer.Start();
            PopulateNav();   
            
            try
            {
                float dpiScale = Program.SystemDpiX() / 96f;
                if (dpiScale > 1.01f || dpiScale < 0.99f)
                {
                    dpiScaleF = dpiScale;
                    Scale(new SizeF(dpiScale, dpiScale));
                    MinimumSize = new Size((int)(700 * dpiScale), (int)(460 * dpiScale));
                    ScaleLazyPages();   
                }
            }
            catch { }
            UiApplyShell();
            Shown += delegate
            {
                
                try { if (mainSplit != null) mainSplit.SplitterDistance = (int)(220 * dpiScaleF); } catch { }
                RefreshDashboard();   
                if (navTree != null) navTree.Invalidate(true);
                if (contentHost != null) contentHost.Invalidate(true);
            };
            FormClosing += delegate(object s, FormClosingEventArgs e)
            {
                if (core != null && core.ChangeCount > 0)
                {
                    if (MessageBox.Show(this,
                        "还有 " + core.ChangeCount + " 项修改尚未保存。\n退出会丢弃这些修改，确定退出？",
                        "未保存修改", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
                        e.Cancel = true;
                }
            };
            RefreshSaveList();
            if (startFile != null && File.Exists(startFile))
            {
                
                string sf = startFile;
                Shown += delegate { if (core.LoadFile(sf)) { AfterLoad(); SetStatus("已载入: " + sf); } };
            }
            
        }

        void BuildUI()
        {
            Panel top = new Panel(); top.Dock = DockStyle.Top; top.Height = 36; top.Padding = new Padding(4);
            cmbSave = new ComboBox(); cmbSave.DropDownStyle = ComboBoxStyle.DropDownList; cmbSave.Width = 360; cmbSave.Top = 6; cmbSave.Left = 8;
            Button btnLoad = new Button(); btnLoad.Text = "载入"; btnLoad.Left = 374; btnLoad.Top = 5; btnLoad.Width = 60;
            Button btnRefresh = new Button(); btnRefresh.Text = "刷新列表"; btnRefresh.Left = 440; btnRefresh.Top = 5; btnRefresh.Width = 80;
            lblStory = new Label(); lblStory.AutoSize = true; lblStory.Left = 528; lblStory.Top = 11;
            lblStory.Text = "未检测"; lblStory.ForeColor = Color.Gray; lblStory.Font = new Font(Font, FontStyle.Bold);
            chkRaw = new CheckBox(); chkRaw.Text = "显示原始键名"; chkRaw.AutoSize = true; chkRaw.Left = 600; chkRaw.Top = 8;
            chkRaw.CheckedChanged += delegate { RefreshAll(); };
            chkAdvanced = new CheckBox(); chkAdvanced.Text = "高级模式"; chkAdvanced.AutoSize = true; chkAdvanced.Left = 704; chkAdvanced.Top = 8;
            chkAdvanced.CheckedChanged += delegate { PopulateNav(); };
            btnLoad.Click += delegate { LoadSelected(); };
            btnRefresh.Click += delegate { RefreshSaveList(); };
            cmbSave.SelectedIndexChanged += delegate { if (!loadingSaveList && cmbSave.SelectedIndex >= 0) LoadSelected(); };
            top.Controls.Add(cmbSave); top.Controls.Add(btnLoad); top.Controls.Add(btnRefresh);
            top.Controls.Add(lblStory); top.Controls.Add(chkRaw); top.Controls.Add(chkAdvanced);
            

            
            SplitContainer main = new SplitContainer();
            main.Dock = DockStyle.Fill;
            main.Orientation = Orientation.Vertical;
            mainSplit = main;
            navTree = new TreeView();
            navTree.Dock = DockStyle.Fill;
            navTree.HideSelection = false;
            navTree.FullRowSelect = true;
            navTree.ShowLines = true;
            navTree.Indent = 18;
            navTree.AfterSelect += delegate { NavigateToSelected(); };
            main.Panel1.Controls.Add(navTree);
            contentHost = new Panel(); contentHost.Dock = DockStyle.Fill;
            main.Panel2.Controls.Add(contentHost);
            Controls.Add(main);

            
            pageDashboard = BuildDashboardPanel();
            pageQuick = BuildQuickPanel();
            pageNoviceBase = BuildNoviceBasePanel();
            pageSearch = BuildSearchPanel();
            pageFavorites = BuildFavoritesPanel();
            pageHistory = BuildHistoryPanel();
            pagePresets = BuildPresetsPanel();
            pageSaveBatch = BuildSaveBatchPanel();
            fpDiplo = CreateFilterPage(null, "", DIPLO_PREFIXES, new string[] { });
            fpRelation = CreateFilterPage(null, "", RELATION_PREFIXES, RELATION_KEYWORDS);
            fpDecree = CreateFilterPage(null, "", DECREE_PREFIXES, DECREE_KEYWORDS);
            pageDiplo = fpDiplo.lv.Parent;
            pageRelation = fpRelation.lv.Parent;
            pageDecree = fpDecree.lv.Parent;
            pageBase = BuildBasePanel();
            pageEvent = BuildEventPanel();
            pageResource = BuildDlcPanel();
            pageOther = BuildOtherPanel(null);
            pageHb = BuildHbPanel();      
            pageOi = BuildOiPanel();      
            RegisterModPages();           

            Panel bottom = new Panel(); bottom.Dock = DockStyle.Bottom; bottom.Height = 40; bottom.Padding = new Padding(4);
            Button btnSave = new Button(); btnSave.Text = "保存修改 (自动备份)"; btnSave.Left = 8; btnSave.Top = 7; btnSave.Width = 170; btnSave.Tag = "primary";
            Button btnReload = new Button(); btnReload.Text = "重新载入"; btnReload.Left = 186; btnReload.Top = 7; btnReload.Width = 90;
            Button btnUndo = new Button(); btnUndo.Text = "撤销上一步"; btnUndo.Left = 284; btnUndo.Top = 7; btnUndo.Width = 100;
            Button btnRestore = new Button(); btnRestore.Text = "从 .bak 恢复"; btnRestore.Left = 392; btnRestore.Top = 7; btnRestore.Width = 116;
            lblStatus = new Label(); lblStatus.AutoSize = true; lblStatus.Left = 516; lblStatus.Top = 12; lblStatus.ForeColor = Color.DarkGreen;
            btnSave.Click += delegate
            {
                if (!core.loaded) { SetStatus("未载入存档"); return; }
                ApplyAllPending();   
                if (!ValidateCurrentSave()) { SetStatus("存档结构校验失败，已阻止保存"); return; }
                if (!ShowSavePreview()) return;
                if (core.Save())
                {
                    int synced = 0;
                    if (core.ChangeCount > 0)
                    {
                        List<string> targets = GetAutoSyncTargets();
                        synced = SyncChangesToTargets(targets);
                    }
                    core.ClearChanges();
                    SetStatus("已保存: " + core.curFile + " (备份 .bak)" + (synced > 0 ? "，同步 " + synced + " 个关联存档" : ""));
                    RefreshHistoryPanel();
                }
                else SetStatus("保存失败");
            };
            btnReload.Click += delegate { if (core.curFile != null && core.LoadFile(core.curFile)) { AfterLoad(); SetStatus("已重新载入"); } };
            btnUndo.Click += delegate { if (core.UndoLast()) { RefreshAll(); RefreshHistoryPanel(); SetStatus("已撤销上一步"); } else SetStatus("没有可撤销的修改"); };
            btnRestore.Click += delegate
            {
                if (!core.loaded || core.curFile == null) { SetStatus("未载入存档"); return; }
                string bak = core.curFile + ".bak";
                if (!File.Exists(bak)) { SetStatus("没有 .bak 备份"); return; }
                if (MessageBox.Show(this, "用 .bak 覆盖当前存档？", "从备份恢复", MessageBoxButtons.YesNo) != DialogResult.Yes) return;
                if (core.RestoreBackup()) { AfterLoad(); SetStatus("已从 .bak 恢复: " + core.curFile); }
                else SetStatus("恢复失败");
            };
            bottom.Controls.Add(btnSave); bottom.Controls.Add(btnReload); bottom.Controls.Add(btnUndo); bottom.Controls.Add(btnRestore); bottom.Controls.Add(lblStatus);
            Controls.Add(bottom);
            Controls.Add(top);   
        }

        
        
        
        
        
        
        
        
        
        void ScaleLazyPages()
        {
            SizeF factor = new SizeF(dpiScaleF, dpiScaleF);
            HashSet<Control> done = new HashSet<Control>();
            Control[] pages = {
                pageDashboard, pageQuick, pageNoviceBase, pageSearch, pageFavorites, pageHistory, pagePresets, pageSaveBatch,
                pageBase, pageDiplo, pageRelation, pageDecree, pageEvent, pageResource, pageOther,
                pageHb, pageOi
            };
            foreach (Control page in pages)
            {
                if (page == null || !done.Add(page)) continue;
                try { page.Scale(factor); } catch { }
            }
        }

        
    }
}
