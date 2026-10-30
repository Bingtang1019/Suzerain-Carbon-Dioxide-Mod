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
            void PopulateNav()
            {
                navTree.BeginUpdate();
                navTree.Nodes.Clear();
                contentHost.Controls.Clear();
                TreeNode dash = new TreeNode("仪表盘");
                AddNavPage(dash, "概览", pageDashboard);
                navTree.Nodes.Add(dash);
                if (!core.loaded)
                {
                    
                    AppendModNavNodes();
                    navTree.ExpandAll();
                    if (dash.Nodes.Count > 0) navTree.SelectedNode = dash.Nodes[0];
                    navTree.EndUpdate();
                    return;
                }
                bool showBase = core.storyMode == 1 || core.storyMode == 3;
                bool showDlc = core.storyMode == 2 || core.storyMode == 3;
                if (showBase)
                {
                    TreeNode root = new TreeNode("本体 (Sordland)");
                    if (AdvancedMode)
                    {
                        AddNavPage(root, "快捷修改", pageQuick);
                        AddNavPage(root, "内政与资源", pageBase);
                        AddNavPage(root, "外交", pageDiplo);
                        AddNavPage(root, "人物关系", pageRelation);
                        AddNavPage(root, "总统令", pageDecree);
                        AddNavPage(root, "事件", pageEvent);
                        AddNavPage(root, "高级（全部变量/实体）", pageOther);
                    }
                    else
                    {
                        AddNavPage(root, "新手面板", pageNoviceBase);
                        AddNavPage(root, "总统令", pageDecree);
                    }
                    navTree.Nodes.Add(root);
                }
                if (showDlc)
                {
                    TreeNode root = new TreeNode("DLC (Rizia)");
                    AddNavPage(root, "快捷修改", pageQuick);
                    if (AdvancedMode)
                    {
                        AddNavPage(root, "资源", pageResource);
                        AddNavPage(root, "外交", pageDiplo);
                        AddNavPage(root, "人物关系", pageRelation);
                        AddNavPage(root, "事件", pageEvent);
                        AddNavPage(root, "高级（全部变量/实体）", pageOther);
                    }
                    else
                    {
                        AddNavPage(root, "资源", pageResource);
                    }
                    navTree.Nodes.Add(root);
                }
                TreeNode g = new TreeNode("全局");
                AddNavPage(g, "搜索工作台", pageSearch);
                AddNavPage(g, "收藏", pageFavorites);
                AddNavPage(g, "修改历史", pageHistory);
                AddNavPage(g, "修改方案", pagePresets);
                AddNavPage(g, "存档管理", pageSaveBatch);
                navTree.Nodes.Add(g);
                
                AppendModNavNodes();
                navTree.ExpandAll();
                if (navTree.Nodes.Count > 0 && navTree.Nodes[0].Nodes.Count > 0)
                    navTree.SelectedNode = navTree.Nodes[0].Nodes[0];
                navTree.EndUpdate();
            }

            
            
            
            
            void AppendModNavNodes()
            {
                List<IModPage> installed = ModPageRegistry.InstalledPages();
                for (int i = 0; i < installed.Count; i++)
                {
                    IModPage mp = installed[i];
                    if (mp == null) continue;
                    Control page = null;
                    try { page = mp.Build(); }
                    catch { page = null; }
                    if (page == null) continue;
                    TreeNode group = new TreeNode(mp.GroupTitle);
                    AddNavPage(group, mp.PageTitle, page);
                    navTree.Nodes.Add(group);
                }
            }

            void AddNavPage(TreeNode parent, string text, Control page)
            {
                TreeNode n = new TreeNode(text);
                n.Tag = page;
                parent.Nodes.Add(n);
            }

            void NavigateToSelected()
            {
                if (navTree == null || navTree.SelectedNode == null) return;
                Control page = navTree.SelectedNode.Tag as Control;
                if (page != null)
                {
                    RefreshQuickPanel();
                    RefreshFavoritesPanel();
                    RefreshHistoryPanel();
                    RefreshPresetsPanel();
                    RefreshSaveBatchPanel();
                    RefreshNoviceBasePanel();
                    RefreshDashboard();
                    RefreshHbPanel();
                    RefreshOiPanel();
                    ShowPage(page);
                }
            }

            void ShowPage(Control page)
            {
                contentHost.Controls.Clear();
                page.Dock = DockStyle.Fill;
                contentHost.Controls.Add(page);
                UiApplyPage(page);
            }

    }
}
