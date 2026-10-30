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
    static class Program
    {
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        static extern bool SetProcessDPIAware();

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        static extern IntPtr GetDC(IntPtr hwnd);

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        static extern int ReleaseDC(IntPtr hwnd, IntPtr hdc);

        [System.Runtime.InteropServices.DllImport("gdi32.dll")]
        static extern int GetDeviceCaps(IntPtr hdc, int index);

        internal static float SystemDpiX()
        {
            IntPtr hdc = GetDC(IntPtr.Zero);
            try { return GetDeviceCaps(hdc, 88); }  
            catch { return 96f; }
            finally { if (hdc != IntPtr.Zero) ReleaseDC(IntPtr.Zero, hdc); }
        }

        [STAThread]
        static int Main(string[] args)
        {
            try { SetProcessDPIAware(); } catch { }
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            AppDomain.CurrentDomain.UnhandledException += delegate(object s, UnhandledExceptionEventArgs e)
            {
                try { File.WriteAllText(Path.Combine(Path.GetTempPath(), "suzerain_gui_err.txt"), (e.ExceptionObject ?? new Exception("null")).ToString()); } catch { }
            };
            Application.ThreadException += delegate(object s, System.Threading.ThreadExceptionEventArgs e)
            {
                try { File.WriteAllText(Path.Combine(Path.GetTempPath(), "suzerain_gui_err.txt"), e.Exception.ToString()); } catch { }
            };
            if (args.Length >= 1 && args[0] == "--snapshot")
            {
                
                
                string snapDir = Path.Combine(Path.GetTempPath(), "SuzerainSnap");
                try { Directory.CreateDirectory(snapDir); } catch { }
                string snapPath = args.Length >= 2 ? args[1] : Path.Combine(snapDir, "self_snap.bmp");
                int snapTab = -1;
                if (args.Length >= 3) int.TryParse(args[2], out snapTab);
                int snapDelay = 2500;
                if (args.Length >= 4) int.TryParse(args[3], out snapDelay);
                string snapLoad = args.Length >= 5 ? args[4] : null;
                MainForm sf = new MainForm(snapLoad);
                Timer st = new Timer(); st.Interval = snapDelay;
                st.Tick += delegate
                {
                    st.Stop();
                    try
                    {
                        try { File.WriteAllText(Path.Combine(snapDir, "snap_log.txt"), sf.DumpStats()); } catch { }
                        sf.Activate(); sf.BringToFront(); sf.Refresh();
                        System.Windows.Forms.Application.DoEvents();
                        System.Threading.Thread.Sleep(200);
                        if (snapTab >= 0 && snapTab < sf.navTree.Nodes.Count)
                        {
                            TreeNode n = sf.navTree.Nodes[snapTab];
                            while (n.Nodes.Count > 0) n = n.Nodes[0];
                            sf.navTree.SelectedNode = n;
                            sf.Refresh();
                        }
                        Bitmap bmp = new Bitmap(sf.Width, sf.Height);
                        Graphics sg = Graphics.FromImage(bmp);
                        Point pt = sf.PointToScreen(Point.Empty);
                        sg.CopyFromScreen(pt.X, pt.Y, 0, 0, new Size(sf.Width, sf.Height));
                        bmp.Save(snapPath, System.Drawing.Imaging.ImageFormat.Bmp);
                        sg.Dispose(); bmp.Dispose();
                    }
                    catch { }
                    sf.Close();
                };
                st.Start();
                Application.Run(sf);
                return 0;
            }
            if (args.Length >= 2 && args[0] == "--check")
            {
                TrainerCore core = new TrainerCore();
                if (!core.LoadFile(args[1])) { Console.WriteLine("LOAD_FAIL"); return 1; }
                Console.WriteLine("LOAD_OK");
                Console.WriteLine("StoryMode=" + core.DetectStoryMode());
                Console.WriteLine("Budget=" + core.GetVar("BaseGame.GovernmentBudget"));
                string old = core.GetVar("BaseGame.GovernmentBudget");
                core.SetVar("BaseGame.GovernmentBudget", "7");
                Console.WriteLine("BudgetSet=" + core.GetVar("BaseGame.GovernmentBudget"));
                core.SetVar("BaseGame.GovernmentBudget", old);
                Console.WriteLine("BudgetRestore=" + core.GetVar("BaseGame.GovernmentBudget"));
                core.SetBoolVar("GameCondition.Turn01_A_Media", "false");
                Console.WriteLine("EventSet=" + core.GetBoolVar("GameCondition.Turn01_A_Media"));
                core.SetBoolVar("GameCondition.Turn01_A_Media", "true");
                Console.WriteLine("EventRestore=" + core.GetBoolVar("GameCondition.Turn01_A_Media"));
                Console.WriteLine("VarCount=" + core.ScanVars("BaseGame.").Count);
                Console.WriteLine("BoolCount=" + core.ScanBoolVars("GameCondition.").Count);
                Console.WriteLine("EntityCount=" + core.ScanEntities().Count);
                return 0;
            }
            if (args.Length >= 2 && args[0] == "--savetest")
            {
                
                TrainerCore c = new TrainerCore();
                if (!c.LoadFile(args[1])) { Console.WriteLine("LOAD_FAIL"); return 1; }
                string before = c.GetVar("BaseGame.Reform_Assembly_Vote");
                bool set = c.SetVar("BaseGame.Reform_Assembly_Vote", "240");
                bool saved = c.Save();
                TrainerCore c2 = new TrainerCore();
                c2.LoadFile(args[1]);
                string after = c2.GetVar("BaseGame.Reform_Assembly_Vote");
                Console.WriteLine("SAVETEST before=" + before + " set=" + set + " saved=" + saved + " after=" + after);
                Console.WriteLine(after == "240" ? "SAVETEST_OK" : "SAVETEST_FAIL");
                return 0;
            }
            if (args.Length >= 1 && args[0] == "--hbtest")
            {
                
                Console.WriteLine("HB_INSTALLED=" + HbBridge.Installed());
                Console.WriteLine("HB_RUNNING=" + HbBridge.Running());
                Console.WriteLine("HB_MODVERSION=" + HbBridge.State("modVersion"));
                Console.WriteLine("HB_ENABLED=" + HbBridge.State("enabled"));
                Console.WriteLine("HB_FIGHTERS=" + HbBridge.State("fighters"));
                Console.WriteLine("HB_AP=" + HbBridge.State("actionPoints"));
                Console.WriteLine("HB_DIR=" + HbBridge.Dir());
                bool w = HbBridge.WriteOverride(true, true, 3, 2);
                Console.WriteLine("HB_WRITE=" + w + (w ? "" : (" err=" + HbBridge.LastError)));
                Console.WriteLine("HB_READBACK_SUPPORT=" + HbBridge.OverrideInt("airSupportActions", -999));
                Console.WriteLine("HB_READBACK_STRIKE=" + HbBridge.OverrideInt("airBombardmentActions", -999));
                Console.WriteLine("HB_READBACK_ENABLED=" + HbBridge.OverrideBool("enabled", false));
                Console.WriteLine("HB_READBACK_AUTO=" + HbBridge.OverrideBool("autoBonusEnabled", false));
                bool d = HbBridge.DeleteOverride();
                Console.WriteLine("HB_DELETE=" + d);
                Console.WriteLine("HB_OVERRIDE_AFTER_DELETE=" + File.Exists(HbBridge.OverridePath));
                bool pass = HbBridge.Installed() && w
                    && HbBridge.OverrideInt("airSupportActions", -999) == -999   
                    && !File.Exists(HbBridge.OverridePath);
                Console.WriteLine(pass ? "HBTEST_OK" : "HBTEST_WARN");
                return 0;
            }
            if (args.Length >= 1 && args[0] == "--pipetest")
            {
                
                
                string pipe = args.Length >= 2 ? args[1] : RealTimeBridge.OiPipeName;
                Console.WriteLine("PIPE_NAME=" + pipe);
                Console.WriteLine("PIPE_TICK=" + Environment.TickCount);
                Console.WriteLine("PIPE_DESCRIBE0=" + RealTimeBridge.Describe(pipe));
                bool avail = RealTimeBridge.Available(pipe);
                Console.WriteLine("PIPE_AVAILABLE=" + avail);
                if (!avail) Console.WriteLine("PIPE_AVAIL_ERR=" + RealTimeBridge.LastErrorOf(pipe));
                bool sent = RealTimeBridge.TrySend("{\"apiVersion\":2,\"enabled\":true,\"probe\":true}", pipe);
                Console.WriteLine("PIPE_SEND=" + sent);
                if (!sent) Console.WriteLine("PIPE_SEND_ERR=" + RealTimeBridge.LastErrorOf(pipe));
                string pulled = RealTimeBridge.TryPullState(pipe);
                Console.WriteLine("PIPE_PULL=" + (pulled == null ? "null" : (pulled.Length + " bytes")));
                Console.WriteLine("PIPE_DESCRIBE=" + RealTimeBridge.Describe(pipe));
                
                bool pass = avail ? sent : (sent == false && pulled == null);
                Console.WriteLine(pass ? "PIPETEST_OK" : "PIPETEST_WARN");
                return 0;
            }
            if (args.Length >= 1 && args[0] == "--oitest")
            {
                
                Console.WriteLine("OI_INSTALLED=" + OiBridge.Installed());
                Console.WriteLine("OI_RUNNING=" + OiBridge.Running());
                Console.WriteLine("OI_MODVERSION=" + OiBridge.State("modVersion"));
                Console.WriteLine("OI_ENABLED=" + OiBridge.State("enabled"));
                Console.WriteLine("OI_MAXDECREES=" + OiBridge.State("maxDecrees"));
                Console.WriteLine("OI_PANEL=" + OiBridge.State("panelName"));
                Console.WriteLine("OI_ENACTED=" + OiBridge.State("enactedCount"));
                Console.WriteLine("OI_DIR=" + OiBridge.Dir());
                bool w = OiBridge.WriteOverride(true, 500, true, true, true, -2, -1, 0, 1, 1, 1, 1, -1, 1,
                    1, 0, -1, 2, 1, 0, 1, true, 4);   
                
                Console.WriteLine("OI_WRITE=" + w + (w ? "" : (" err=" + OiBridge.LastError)));
                Console.WriteLine("OI_READBACK_NEW=" + OiBridge.OverrideInt("tourismGoodDelta", -999) + "/" + OiBridge.OverrideInt("tradeIncreasedDelta", -999) + "/" + OiBridge.OverrideInt("tradeDealBonus", -999));
                Console.WriteLine("OI_READBACK_MAX=" + OiBridge.OverrideInt("maxDecrees", -999));
                Console.WriteLine("OI_READBACK_ENABLED=" + OiBridge.OverrideBool("enabled", false));
                Console.WriteLine("OI_READBACK_HIDECOUNTER=" + OiBridge.OverrideBool("hidePageCounter", false));
                Console.WriteLine("OI_READBACK_HIDESUBTITLE=" + OiBridge.OverrideBool("hideSubtitle", false));
                Console.WriteLine("OI_READBACK_ECOSTATE=" + OiBridge.OverrideBool("economyEnabled", false) + "/" + OiBridge.OverrideInt("dangerDelta", -999) + "/" + OiBridge.OverrideInt("regionStepDelta", -999));
                bool d = OiBridge.DeleteOverride();
                Console.WriteLine("OI_DELETE=" + d);
                Console.WriteLine("OI_OVERRIDE_AFTER_DELETE=" + File.Exists(OiBridge.OverridePath));
                bool pass = w && OiBridge.OverrideInt("maxDecrees", -999) == -999 && !File.Exists(OiBridge.OverridePath);
                Console.WriteLine(pass ? "OITEST_OK" : "OITEST_WARN");
                return 0;
            }
            if (args.Length >= 1 && args[0] == "--tptest")
            {
                Console.WriteLine("TP_INSTALLED=" + TpBridge.Installed());
                Console.WriteLine("TP_RUNNING=" + TpBridge.Running());
                Console.WriteLine("TP_VERSION=" + TpBridge.State("modVersion"));
                Console.WriteLine("TP_ENABLED=" + TpBridge.State("enabled"));
                Console.WriteLine("TP_LAWCOUNT=" + TpBridge.State("lawCount"));
                Console.WriteLine("TP_DEBUG=" + TpBridge.State("debugMode"));
                Console.WriteLine("TP_DIR=" + TpBridge.Dir());
                bool w = TpBridge.WriteDebugOverride(true);
                Console.WriteLine("TP_WRITE=" + w + (w ? "" : (" err=" + TpBridge.LastError)));
                bool readback = TpBridge.OverrideBool("debugMode", false);
                Console.WriteLine("TP_READBACK=" + readback);
                bool d = TpBridge.DeleteOverride();
                Console.WriteLine("TP_DELETE=" + d);
                Console.WriteLine("TP_OVERRIDE_AFTER_DELETE=" + File.Exists(TpBridge.OverridePath));
                Console.WriteLine((w && readback && TpBridge.OverrideBool("debugMode", false) == false
                    && !File.Exists(TpBridge.OverridePath)) ? "TPTEST_OK" : "TPTEST_WARN");
                return 0;
            }
            Application.Run(new MainForm(args.Length > 0 ? args[0] : null));
            return 0;
        }
    }

}
