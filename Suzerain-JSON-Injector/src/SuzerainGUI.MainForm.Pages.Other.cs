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
            Control BuildOtherPanel(TabControl tabs)
            {
                Panel p = new Panel();
                SplitContainer sp = new SplitContainer(); sp.Dock = DockStyle.Fill; sp.Orientation = Orientation.Horizontal; sp.SplitterDistance = 360;
                
                fpAll = CreateFilterPage(tabs, "", ALL_PREFIXES, new string[] { });
                fpAll.paged = true;
                sp.Panel1.Controls.Add(fpAll.lv.Parent);
                
                Panel p2 = new Panel(); p2.Dock = DockStyle.Fill;
                Panel top = new Panel(); top.Dock = DockStyle.Top; top.Height = 34;
                Label lp = new Label(); lp.Text = "实体:"; lp.Left = 10; lp.Top = 8; lp.AutoSize = true;
                cmbEntity = new ComboBox(); cmbEntity.DropDownStyle = ComboBoxStyle.DropDownList; cmbEntity.Left = 52; cmbEntity.Top = 5; cmbEntity.Width = 260;
                Label ls = new Label(); ls.Text = "搜索:"; ls.Left = 322; ls.Top = 8; ls.AutoSize = true;
                txtEntitySearch = new TextBox(); txtEntitySearch.Left = 362; txtEntitySearch.Top = 5; txtEntitySearch.Width = 130;
                Button b1 = new Button(); b1.Text = "刷新"; b1.Left = 500; b1.Top = 5; b1.Width = 60;
                top.Controls.Add(lp); top.Controls.Add(cmbEntity); top.Controls.Add(ls); top.Controls.Add(txtEntitySearch); top.Controls.Add(b1);
                txtEntitySearch.TextChanged += delegate { RefreshEntityList(); RefreshEntity(); };
                lvEntity = new ListView(); lvEntity.Dock = DockStyle.Fill; lvEntity.View = View.Details; lvEntity.FullRowSelect = true;
                lvEntity.GridLines = true; lvEntity.MultiSelect = false;
                lvEntity.Columns.Add("字段", -2); lvEntity.Columns.Add("值", 120);
                AttachFavoriteMenu(lvEntity);
                p2.Controls.Add(lvEntity);
                p2.Controls.Add(top);   
                Label hint = new Label(); hint.Dock = DockStyle.Bottom; hint.Height = 24; hint.Padding = new Padding(6, 4, 0, 0);
                hint.Text = "双击行修改字段值 (GDP/人口/关系/状态效果等)；实体写入默认关闭，首次会要求确认。";
                hint.ForeColor = Color.DimGray;
                p2.Controls.Add(hint);
                cmbEntity.SelectedIndexChanged += delegate { RefreshEntity(); };
                b1.Click += delegate { RefreshEntityList(); RefreshEntity(); };
                lvEntity.DoubleClick += delegate
                {
                    if (lvEntity.SelectedItems.Count == 0 || !core.loaded) return;
                    if (cmbEntity.SelectedIndex <= 0) return;
                    string entity = cmbEntity.SelectedItem.ToString();
                    string field = lvEntity.SelectedItems[0].Tag as string ?? lvEntity.SelectedItems[0].SubItems[0].Text;
                    string cur = core.GetEntityValue(entity, field);
                    if (cur == null) return;
                    string input = InputBox("修改字段", entity + "  " + field + "  (当前=" + FormatVal(cur) + ")", FormatVal(cur));
                    if (input == null) return;
                    if (!core.AllowEntityWrites)
                    {
                        if (MessageBox.Show(this,
                            "实体字段属于 entityUpdates，不是 variables。\n默认保存只写 variables；继续将显式允许 entityUpdates 写入。",
                            "高级实体写入", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
                            return;
                        core.AllowEntityWrites = true;
                    }
                    if (core.SetEntityValue(entity, field, input)) { RefreshEntity(); SetStatus(entity + "." + field + " -> " + input); }
                    else SetStatus("修改失败");
                };
                sp.Panel2.Controls.Add(p2);
                p.Controls.Add(sp);
                return p;
            }

            void RefreshEntityList()
            {
                if (!core.loaded) return;
                string sel = cmbEntity.SelectedIndex > 0 ? cmbEntity.SelectedItem.ToString() : null;
                cmbEntity.Items.Clear();
                cmbEntity.Items.Add("(全部实体 - 请选择)");
                List<string> ents = core.ScanEntities();
                ents.Sort();
                string filter = txtEntitySearch == null ? "" : txtEntitySearch.Text.Trim();
                foreach (string e in ents)
                    if (filter.Length == 0 || e.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0)
                        cmbEntity.Items.Add(e);
                if (sel != null)
                {
                    int idx = cmbEntity.Items.IndexOf(sel);
                    if (idx > 0) cmbEntity.SelectedIndex = idx;
                }
            }

            void RefreshEntity()
            {
                lvEntity.BeginUpdate();
                lvEntity.Items.Clear();
                if (!core.loaded || cmbEntity.SelectedIndex <= 0) { lvEntity.EndUpdate(); return; }
                string entity = cmbEntity.SelectedItem.ToString();
                List<string> fields = core.ScanEntityFields(entity);
                foreach (string f in fields)
                {
                    ListViewItem it = new ListViewItem(DisplayName(f));
                    it.Tag = f;
                    it.ToolTipText = entity + "." + f;
                    it.SubItems.Add(core.GetEntityValue(entity, f) ?? "?");
                    lvEntity.Items.Add(it);
                }
                lvEntity.EndUpdate();
            }

    }
}
