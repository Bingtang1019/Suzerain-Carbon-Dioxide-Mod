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
            
            protected override bool ProcessDialogKey(Keys keyData)
            {
                if ((keyData & Keys.Control) != 0 && (keyData & Keys.K) == Keys.K)
                {
                    OpenCommandPalette();
                    return true;
                }
                return base.ProcessDialogKey(keyData);
            }

            void OpenCommandPalette()
            {
                if (!core.loaded) { SetStatus("请先载入存档"); return; }
                
                ShowSearchWorkbench();
                return;
                Form f = new Form();
                f.Text = "快速命令 (Ctrl+K)";
                f.FormBorderStyle = FormBorderStyle.FixedDialog;
                f.StartPosition = FormStartPosition.CenterParent;
                f.ClientSize = new Size(620, 420);
                f.Font = Font;
                f.TopMost = true;
                TextBox txt = new TextBox(); txt.Dock = DockStyle.Top; txt.Height = 30; txt.Font = new Font(Font.FontFamily, 11F);
                ListView lv = new ListView(); lv.Dock = DockStyle.Fill; lv.View = View.Details; lv.FullRowSelect = true; lv.GridLines = true;
                lv.Columns.Add("类型", 80); lv.Columns.Add("名称/键", 380); lv.Columns.Add("值", 140);
                f.Controls.Add(lv); f.Controls.Add(txt);
                f.AcceptButton = null;
                txt.TextChanged += delegate { FillCommandList(lv, txt.Text); };
                lv.DoubleClick += delegate
                {
                    if (lv.SelectedItems.Count == 0) return;
                    ApplyCommandListRow(lv);
                    f.Close();
                };
                f.KeyDown += delegate(object s, KeyEventArgs e) { if (e.KeyCode == Keys.Escape) f.Close(); };
                f.Shown += delegate { txt.Select(); };
                ScaleDialog(f);
                f.ShowDialog(this);
            }

            void FillCommandList(ListView lv, string q)
            {
                lv.BeginUpdate(); lv.Items.Clear();
                if (q.Length == 0) { lv.EndUpdate(); return; }
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
                        if (k.IndexOf(q, StringComparison.OrdinalIgnoreCase) < 0 &&
                            DisplayName(k).IndexOf(q, StringComparison.OrdinalIgnoreCase) < 0) continue;
                        string v = core.GetVar(k);
                        if (v == null) v = core.GetBoolVar(k);
                        if (v == null) v = core.GetStringVar(k);
                        if (v == null) continue;
                        ListViewItem it = new ListViewItem("变量");
                        it.Tag = k;
                        it.SubItems.Add(DisplayName(k));
                        it.SubItems.Add(FormatVal(v));
                        lv.Items.Add(it);
                    }
                }
                foreach (string e in core.ScanEntities())
                {
                    if (e.IndexOf(q, StringComparison.OrdinalIgnoreCase) < 0) continue;
                    if (seen.Contains("entity:" + e)) continue;
                    seen.Add("entity:" + e);
                    ListViewItem it = new ListViewItem("实体");
                    it.Tag = "entity:" + e;
                    it.SubItems.Add(e);
                    it.SubItems.Add("");
                    lv.Items.Add(it);
                }
                lv.EndUpdate();
            }

            void ApplyCommandListRow(ListView lv)
            {
                if (lv.SelectedItems.Count == 0) return;
                string tag = lv.SelectedItems[0].Tag as string;
                if (tag != null && tag.StartsWith("entity:"))
                {
                    string entity = tag.Substring(7);
                    string field = InputBox("修改实体字段", entity + " 要修改的字段名:", "");
                    if (field == null || field.Length == 0) return;
                    string cur = core.GetEntityValue(entity, field);
                    if (cur == null) return;
                    string input = InputBox("修改字段", entity + " " + field, FormatVal(cur));
                    if (input == null) return;
                    if (core.SetEntityValue(entity, field, input)) { RefreshAll(); SetStatus(entity + "." + field + " -> " + input); }
                }
                else EditVarListRow(lv);
            }

            
            string FavoritesPath()
            {
                string dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SuzerainTrainer");
                return Path.Combine(dir, "favorites.json");
            }

            void LoadFavorites()
            {
                try
                {
                    if (File.Exists(FavoritesPath()))
                        foreach (string line in File.ReadAllLines(FavoritesPath(), Encoding.UTF8))
                            if (line.Trim().Length > 0) favorites.Add(line.Trim());
                }
                catch { }
            }

            void SaveFavorites()
            {
                try
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(FavoritesPath()));
                    File.WriteAllLines(FavoritesPath(), new List<string>(favorites), Encoding.UTF8);
                }
                catch { }
            }

            void AttachFavoriteMenu(ListView lv)
            {
                if (lv == null) return;
                ContextMenuStrip menu = new ContextMenuStrip();
                ToolStripMenuItem item = new ToolStripMenuItem("收藏/取消收藏当前行");
                item.Click += delegate { ToggleFavorite(lv); };
                menu.Items.Add(item);
                lv.ContextMenuStrip = menu;
            }

            void ToggleFavorite(ListView lv)
            {
                if (lv == null || lv.SelectedItems.Count == 0 || !core.loaded) return;
                string key = lv.SelectedItems[0].Tag as string;
                if (key == null) key = lv.SelectedItems[0].SubItems[0].Text;
                ToggleFavoriteKey(key);
            }

            void ToggleFavoriteKey(string key)
            {
                if (key == null || key.Length == 0) return;
                if (favorites.Contains(key)) { favorites.Remove(key); SetStatus("已取消收藏: " + key); }
                else { favorites.Add(key); SetStatus("已收藏: " + key); }
                SaveFavorites();
                RefreshFavoritesPanel();
            }

            bool IsAllowedInCurrentStory(string key)
            {
                if (core.storyMode <= 0 || core.storyMode == 3) return true;
                foreach (string pr in ALL_PREFIXES)
                    if (PrefixAllowed(pr) && key.StartsWith(pr)) return true;
                return false;
            }

            
            void EditVarListRow(ListView lv)
            {
                if (lv == null || lv.SelectedItems.Count == 0 || !core.loaded) return;
                string key = lv.SelectedItems[0].Tag as string ?? lv.SelectedItems[0].SubItems[0].Text;
                string cur = core.GetVar(key);
                if (cur == null) cur = core.GetBoolVar(key);
                if (cur == null) cur = core.GetStringVar(key);
                if (cur == null) return;
                ListViewSnapshot snap = CaptureListView(lv);
                if (cur == "true" || cur == "false")
                {
                    string nv = cur == "true" ? "false" : "true";
                    if (core.SetBoolVar(key, nv))
                    {
                        RefreshAll(); RefreshQuickPanel(); RefreshFavoritesPanel(); RefreshHistoryPanel(); RestoreListView(lv, snap);
                        SetStatus(key + ": " + cur + " -> " + nv);
                    }
                    return;
                }
                string input = InputBox("修改数值", key + "  (当前=" + FormatVal(cur) + ")", FormatVal(cur));
                if (input == null) return;
                bool ok = IsNumeric(cur) ? core.SetVar(key, input) : core.SetStringVar(key, input);
                if (ok)
                {
                    RefreshAll(); RefreshQuickPanel(); RefreshFavoritesPanel(); RefreshHistoryPanel(); RestoreListView(lv, snap);
                    SetStatus(key + " -> " + input);
                }
                else SetStatus("修改失败: " + key);
            }

            
    }
}
