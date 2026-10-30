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
                
                
                string snapPath = args.Length >= 2 ? args[1] : @"C:\Users\dell\Desktop\Suzerain修改器\self_snap.bmp";
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
                        try { File.WriteAllText(@"C:\Users\dell\Desktop\Suzerain修改器\snap_log.txt", sf.DumpStats()); } catch { }
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
            Application.Run(new MainForm(args.Length > 0 ? args[0] : null));
            return 0;
        }
    }

    
    class TrainerCore
    {
        public string curFile = null;
        public string json = null;
        public bool loaded = false;
        
        public int storyMode = 0;
        public string storyPack = null;

        
        public class ChangeRec
        {
            public string Kind;    
            public string Key;
            public string Field;
            public string OldValue;
            public string NewValue;
        }
        public List<ChangeRec> Changes = new List<ChangeRec>();
        bool suppressLog = false;

        void LogChange(string kind, string key, string field, string oldValue, string newValue)
        {
            if (suppressLog || oldValue == newValue) return;
            Changes.Add(new ChangeRec { Kind = kind, Key = key, Field = field, OldValue = oldValue, NewValue = newValue });
        }

        public void ClearChanges() { Changes.Clear(); }
        public int ChangeCount { get { return Changes.Count; } }

        public bool UndoLast()
        {
            if (Changes.Count == 0) return false;
            ChangeRec c = Changes[Changes.Count - 1];
            Changes.RemoveAt(Changes.Count - 1);
            suppressLog = true;
            bool ok = false;
            if (c.Kind == "var") ok = SetVar(c.Key, c.OldValue);
            else if (c.Kind == "bool") ok = SetBoolVar(c.Key, c.OldValue);
            else if (c.Kind == "string") ok = SetStringVar(c.Key, c.OldValue);
            else if (c.Kind == "entity") ok = SetEntityValue(c.Key, c.Field, c.OldValue);
            suppressLog = false;
            return ok;
        }

        public static string SaveDir()
        {
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                "AppData/LocalLow/Torpor Games/Suzerain");
        }

        public bool LoadFile(string path)
        {
            try
            {
                json = File.ReadAllText(path); curFile = path; loaded = true; storyMode = DetectStoryMode();
                Changes.Clear(); suppressLog = false;
                return true;
            }
            catch (Exception e) { Console.WriteLine(e.Message); return false; }
        }

        public int DetectStoryMode()
        {
            storyPack = null;
            Match m = Regex.Match(json, "\"currentStoryPack\": \"([^\"]+)\"");
            
            
            
            if (m.Success)
            {
                storyPack = m.Groups[1].Value;
                return storyPack.IndexOf("Rizia", StringComparison.OrdinalIgnoreCase) >= 0 ? 2 : 1;
            }
            
            bool hasBase = GetVar("BaseGame.GovernmentBudget") != null || GetVar("BaseGameSetup.CurrentTurn") != null;
            bool hasDlc = GetVar("RiziaDLC.Resources_Authority") != null || GetVar("RiziaDLCSetup.CurrentTurn") != null;
            if (hasBase && hasDlc) return 3;
            if (hasBase) return 1;
            if (hasDlc) return 2;
            return 0;
        }

        public string GetVar(string key)
        {
            Match m = Regex.Match(json, @"\[\\""" + Regex.Escape(key) + @"\\""\]=(-?\d+(?:\.\d+)?(?:[eE][+-]?\d+)?)");
            return m.Success ? m.Groups[1].Value : null;
        }

        public bool SetVar(string key, string value)
        {
            string old = GetVar(key);
            string pattern = @"\[\\""" + Regex.Escape(key) + @"\\""\]=-?\d+(?:\.\d+)?(?:[eE][+-]?\d+)?";
            if (!Regex.IsMatch(json, pattern)) return false;
            string repl = "[\\\"" + key + "\\\"]=" + value;
            json = Regex.Replace(json, pattern, repl);
            if (old != null) LogChange("var", key, null, old, value);
            return true;
        }

        public string GetBoolVar(string key)
        {
            Match m = Regex.Match(json, @"\[\\""" + Regex.Escape(key) + @"\\""\]=(true|false)");
            return m.Success ? m.Groups[1].Value : null;
        }

        public bool SetBoolVar(string key, string value)
        {
            string old = GetBoolVar(key);
            string pattern = @"(\[\\""" + Regex.Escape(key) + @"\\""\]=)(true|false)";
            if (!Regex.IsMatch(json, pattern)) return false;
            json = Regex.Replace(json, pattern, "$1" + value);
            if (old != null) LogChange("bool", key, null, old, value);
            return true;
        }

        public string GetStringVar(string key)
        {
            Match m = Regex.Match(json, @"\[\\""" + Regex.Escape(key) + @"\\""\]=\""([^\""]*)\""");
            return m.Success ? m.Groups[1].Value : null;
        }

        public bool SetStringVar(string key, string value)
        {
            string old = GetStringVar(key);
            string pattern = @"\[\\""" + Regex.Escape(key) + @"\\""\]=\""[^\""]*\""";
            if (!Regex.IsMatch(json, pattern)) return false;
            string repl = "[\\\"" + key + "\\\"]=\"" + value + "\"";
            json = Regex.Replace(json, pattern, repl);
            if (old != null) LogChange("string", key, null, old, value);
            return true;
        }

        public List<string> ScanVars(string prefix)
        {
            List<string> list = new List<string>();
            Regex re = new Regex(@"\[\\""(" + Regex.Escape(prefix) + @"[A-Za-z0-9_\.]+)\\""\]=-?\d+(?:\.\d+)?(?:[eE][+-]?\d+)?");
            foreach (Match m in re.Matches(json))
            {
                string k = m.Groups[1].Value;
                if (!list.Contains(k)) list.Add(k);
            }
            return list;
        }

        public List<string> ScanBoolVars(string prefix)
        {
            List<string> list = new List<string>();
            Regex re = new Regex(@"\[\\""(" + Regex.Escape(prefix) + @"[A-Za-z0-9_\.]+)\\""\]=(true|false)");
            foreach (Match m in re.Matches(json))
            {
                string k = m.Groups[1].Value;
                if (!list.Contains(k)) list.Add(k);
            }
            return list;
        }

        public List<string> ScanStringVars(string prefix)
        {
            List<string> list = new List<string>();
            Regex re = new Regex(@"\[\\""(" + Regex.Escape(prefix) + @"[A-Za-z0-9_\.]+)\\""\]=\""[^\""]*\""");
            foreach (Match m in re.Matches(json))
            {
                string k = m.Groups[1].Value;
                if (!list.Contains(k)) list.Add(k);
            }
            return list;
        }

        public List<string> ScanEntities()
        {
            List<string> list = new List<string>();
            Regex re = new Regex("\"nameInDatabase\": \"([A-Za-z0-9_ ]+)\"");
            foreach (Match m in re.Matches(json))
                if (!list.Contains(m.Groups[1].Value)) list.Add(m.Groups[1].Value);
            return list;
        }

        public List<string> ScanEntityFields(string entity)
        {
            List<string> list = new List<string>();
            Regex re = new Regex("\"nameInDatabase\": \"" + Regex.Escape(entity) + "\",\\s*\"fieldName\": \"([^\"]+)\"");
            foreach (Match m in re.Matches(json))
                if (!list.Contains(m.Groups[1].Value)) list.Add(m.Groups[1].Value);
            return list;
        }

        public string GetEntityValue(string entity, string field)
        {
            string pattern = "\"nameInDatabase\": \"" + Regex.Escape(entity) + "\",\\s*\"fieldName\": \"" + Regex.Escape(field) + "\",\\s*\"fieldValue\": \"([^\"]*)\"";
            Match m = Regex.Match(json, pattern);
            return m.Success ? m.Groups[1].Value : null;
        }

        public bool SetEntityValue(string entity, string field, string value)
        {
            string old = GetEntityValue(entity, field);
            string pattern = "\"nameInDatabase\": \"" + Regex.Escape(entity) + "\",\\s*\"fieldName\": \"" + Regex.Escape(field) + "\",\\s*\"fieldValue\": \"([^\"]*)\"";
            if (!Regex.IsMatch(json, pattern)) return false;
            string repl = "\"nameInDatabase\": \"" + entity + "\",\n            \"fieldName\": \"" + field + "\",\n            \"fieldValue\": \"" + value + "\"";
            json = Regex.Replace(json, pattern, delegate(Match m2) { return repl; });
            if (old != null) LogChange("entity", entity, field, old, value);
            return true;
        }

        
        public bool SetOrAddEntityValue(string entity, string field, string value)
        {
            if (SetEntityValue(entity, field, value)) return true;
            string marker = "\n    ]\n}";
            int idx = json.LastIndexOf(marker, StringComparison.Ordinal);
            if (idx < 0) return false;
            string entry = ",\n        {\n            \"nameInDatabase\": \"" + entity + "\",\n            \"fieldName\": \"" + field + "\",\n            \"fieldValue\": \"" + value + "\"\n        }";
            json = json.Insert(idx, entry);
            return true;
        }

        public bool Save()
        {
            if (!loaded || curFile == null) return false;
            try
            {
                File.Copy(curFile, curFile + ".bak", true);
                File.WriteAllText(curFile, json, new UTF8Encoding(false));
                return true;
            }
            catch (Exception e) { Console.WriteLine(e.Message); return false; }
        }
    }

    
    
    
    
    
    
    
    
    
    
    static class HbBridge
    {
        public const string StateName = "HorrifyingBombardment.state.json";
        public const string OverrideName = "HorrifyingBombardment.override.json";
        public static string LastError = "";

        
        public static string Dir()
        {
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "HorrifyingBombardment");
        }

        
        public static string LegacyDir()
        {
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                "AppData", "LocalLow", "Torpor Games", "Suzerain");
        }

        private static string Pick(string name)
        {
            string cur = Path.Combine(Dir(), name);
            if (File.Exists(cur)) return cur;
            string old = Path.Combine(LegacyDir(), name);
            if (File.Exists(old)) return old;
            return cur;   
        }

        public static string StatePath { get { return Pick(StateName); } }
        
        public static string OverridePath { get { return Path.Combine(Dir(), OverrideName); } }
        
        public static string OverridePathInUse { get { return Pick(OverrideName); } }

        
        public static bool Installed()
        {
            try { return File.Exists(StatePath); }
            catch { return false; }
        }

        
        public static bool Running()
        {
            DateTime t = UpdatedAt();
            return t != DateTime.MinValue && (DateTime.Now - t).TotalMinutes <= 5;
        }

        public static DateTime UpdatedAt()
        {
            try
            {
                string raw = File.ReadAllText(StatePath, Encoding.UTF8);
                Match m = Regex.Match(raw, "\"updatedAt\"\\s*:\\s*\"([^\"]+)\"");
                if (m.Success)
                {
                    DateTime t;
                    if (DateTime.TryParse(m.Groups[1].Value, out t)) return t;
                }
            }
            catch { }
            return DateTime.MinValue;
        }

        
        public static string State(string key)
        {
            try
            {
                string raw = File.ReadAllText(StatePath, Encoding.UTF8);
                Match m = Regex.Match(raw, "\"" + Regex.Escape(key) + "\"\\s*:\\s*(\"[^\"]*\"|true|false|null|-?\\d+(?:\\.\\d+)?)");
                if (!m.Success) return "";
                string v = m.Groups[1].Value;
                return v.StartsWith("\"") ? v.Substring(1, v.Length - 2) : v;
            }
            catch { return ""; }
        }

        
        public static int OverrideInt(string key, int fallback)
        {
            try
            {
                string p = OverridePathInUse;
                if (!File.Exists(p)) return fallback;
                string raw = File.ReadAllText(p, Encoding.UTF8);
                Match m = Regex.Match(raw, "\"" + Regex.Escape(key) + "\"\\s*:\\s*(-?\\d+)");
                if (!m.Success) return fallback;
                int v;
                return int.TryParse(m.Groups[1].Value, out v) ? v : fallback;
            }
            catch { return fallback; }
        }

        public static bool OverrideBool(string key, bool fallback)
        {
            try
            {
                string p = OverridePathInUse;
                if (!File.Exists(p)) return fallback;
                string raw = File.ReadAllText(p, Encoding.UTF8);
                Match m = Regex.Match(raw, "\"" + Regex.Escape(key) + "\"\\s*:\\s*(true|false)");
                if (!m.Success) return fallback;
                return string.Equals(m.Groups[1].Value, "true", StringComparison.OrdinalIgnoreCase);
            }
            catch { return fallback; }
        }

        
        public static bool WriteOverride(bool enabled, bool autoBonus, int supportActions, int strikeActions)
        {
            try
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendLine("{");
                sb.AppendLine("  \"apiVersion\": 1,");
                sb.AppendLine("  \"enabled\": " + (enabled ? "true" : "false") + ",");
                sb.AppendLine("  \"autoBonusEnabled\": " + (autoBonus ? "true" : "false") + ",");
                sb.AppendLine("  \"airSupportActions\": " + supportActions + ",");
                sb.AppendLine("  \"airBombardmentActions\": " + strikeActions + ",");
                sb.AppendLine("  \"updatedAt\": \"" + DateTime.Now.ToString("yyyy-MM-dd'T'HH:mm:ssK", CultureInfo.InvariantCulture) + "\",");
                sb.AppendLine("  \"source\": \"Suzerain 修改器 v1.7.0\"");
                sb.AppendLine("}");

                string tmp = OverridePath + ".tmp";
                File.WriteAllText(tmp, sb.ToString(), new UTF8Encoding(false));
                File.Copy(tmp, OverridePath, true);
                File.Delete(tmp);
                LastError = "";
                return true;
            }
            catch (Exception e) { LastError = e.Message; return false; }
        }

        
        public static bool DeleteOverride()
        {
            try
            {
                bool any = false;
                foreach (string p in new string[] { OverridePath, Path.Combine(LegacyDir(), OverrideName) })
                {
                    if (File.Exists(p)) { File.Delete(p); any = true; }
                }
                LastError = any ? "" : "（没有找到开关文件，已是默认状态）";
                return true;
            }
            catch (Exception e) { LastError = e.Message; return false; }
        }
    }

    
    
    
    
    
    static class OiBridge
    {
        public const string StateName = "Oikonomia.state.json";
        public const string OverrideName = "Oikonomia.override.json";
        public static string LastError = "";

        public static string Dir()
        {
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Oikonomia");
        }

        public static string StatePath { get { return Path.Combine(Dir(), StateName); } }
        public static string OverridePath { get { return Path.Combine(Dir(), OverrideName); } }

        
        public static bool Installed()
        {
            try { return File.Exists(StatePath); }
            catch { return false; }
        }

        public static DateTime UpdatedAt()
        {
            try
            {
                string raw = File.ReadAllText(StatePath, Encoding.UTF8);
                Match m = Regex.Match(raw, "\"updatedAt\"\\s*:\\s*\"([^\"]+)\"");
                if (m.Success)
                {
                    DateTime t;
                    if (DateTime.TryParse(m.Groups[1].Value, out t)) return t;
                }
            }
            catch { }
            return DateTime.MinValue;
        }

        
        public static bool Running()
        {
            DateTime t = UpdatedAt();
            return t != DateTime.MinValue && (DateTime.Now - t).TotalMinutes <= 5;
        }

        public static string State(string key)
        {
            try
            {
                string raw = File.ReadAllText(StatePath, Encoding.UTF8);
                Match m = Regex.Match(raw, "\"" + Regex.Escape(key) + "\"\\s*:\\s*(\"[^\"]*\"|true|false|null|-?\\d+(?:\\.\\d+)?)");
                if (!m.Success) return "";
                string v = m.Groups[1].Value;
                return v.StartsWith("\"") ? v.Substring(1, v.Length - 2) : v;
            }
            catch { return ""; }
        }

        public static int OverrideInt(string key, int fallback)
        {
            try
            {
                if (!File.Exists(OverridePath)) return fallback;
                string raw = File.ReadAllText(OverridePath, Encoding.UTF8);
                Match m = Regex.Match(raw, "\"" + Regex.Escape(key) + "\"\\s*:\\s*(-?\\d+)");
                if (!m.Success) return fallback;
                int v;
                return int.TryParse(m.Groups[1].Value, out v) ? v : fallback;
            }
            catch { return fallback; }
        }

        public static bool OverrideBool(string key, bool fallback)
        {
            try
            {
                if (!File.Exists(OverridePath)) return fallback;
                string raw = File.ReadAllText(OverridePath, Encoding.UTF8);
                Match m = Regex.Match(raw, "\"" + Regex.Escape(key) + "\"\\s*:\\s*(true|false)");
                if (!m.Success) return fallback;
                return string.Equals(m.Groups[1].Value, "true", StringComparison.OrdinalIgnoreCase);
            }
            catch { return fallback; }
        }

        
        
        public static bool WriteOverride(bool enabled, int maxDecrees, bool hidePageCounter, bool hideSubtitle,
            bool ecoEnabled, int dangerDelta, int unstableDelta, int stableDelta, int growingDelta, int veryGoodDelta,
            int regionStepDelta, int taxEfficientDelta, int taxAvoidanceDelta, int growthBonus,
            int tourismGoodDelta, int tourismAverageDelta, int tourismBadDelta,
            int tradeIncreasedDelta, int tradeRecoveredDelta, int tradeDecreasedDelta, int tradeDealBonus,
            bool dynamicLimit, int limitBase)
        {
            try
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendLine("{");
                sb.AppendLine("  \"apiVersion\": 2,");
                sb.AppendLine("  \"enabled\": " + (enabled ? "true" : "false") + ",");
                sb.AppendLine("  \"maxDecrees\": " + maxDecrees + ",");
                sb.AppendLine("  \"hidePageCounter\": " + (hidePageCounter ? "true" : "false") + ",");
                sb.AppendLine("  \"hideSubtitle\": " + (hideSubtitle ? "true" : "false") + ",");
                sb.AppendLine("  \"economyEnabled\": " + (ecoEnabled ? "true" : "false") + ",");
                sb.AppendLine("  \"dangerDelta\": " + dangerDelta + ",");
                sb.AppendLine("  \"unstableDelta\": " + unstableDelta + ",");
                sb.AppendLine("  \"stableDelta\": " + stableDelta + ",");
                sb.AppendLine("  \"growingDelta\": " + growingDelta + ",");
                sb.AppendLine("  \"veryGoodDelta\": " + veryGoodDelta + ",");
                sb.AppendLine("  \"regionStepDelta\": " + regionStepDelta + ",");
                sb.AppendLine("  \"taxEfficientDelta\": " + taxEfficientDelta + ",");
                sb.AppendLine("  \"taxAvoidanceDelta\": " + taxAvoidanceDelta + ",");
                sb.AppendLine("  \"growthBonus\": " + growthBonus + ",");
                sb.AppendLine("  \"tourismGoodDelta\": " + tourismGoodDelta + ",");
                sb.AppendLine("  \"tourismAverageDelta\": " + tourismAverageDelta + ",");
                sb.AppendLine("  \"tourismBadDelta\": " + tourismBadDelta + ",");
                sb.AppendLine("  \"tradeIncreasedDelta\": " + tradeIncreasedDelta + ",");
                sb.AppendLine("  \"tradeRecoveredDelta\": " + tradeRecoveredDelta + ",");
                sb.AppendLine("  \"tradeDecreasedDelta\": " + tradeDecreasedDelta + ",");
                sb.AppendLine("  \"tradeDealBonus\": " + tradeDealBonus + ",");
                sb.AppendLine("  \"dynamicLimit\": " + (dynamicLimit ? "true" : "false") + ",");
                sb.AppendLine("  \"limitBase\": " + limitBase + ",");
                sb.AppendLine("  \"updatedAt\": \"" + DateTime.Now.ToString("yyyy-MM-dd'T'HH:mm:ssK", CultureInfo.InvariantCulture) + "\",");
                sb.AppendLine("  \"source\": \"Suzerain 修改器 v1.7.0\"");
                sb.AppendLine("}");

                string tmp = OverridePath + ".tmp";
                File.WriteAllText(tmp, sb.ToString(), new UTF8Encoding(false));
                File.Copy(tmp, OverridePath, true);
                File.Delete(tmp);
                LastError = "";
                return true;
            }
            catch (Exception e) { LastError = e.Message; return false; }
        }

        
        public static bool DeleteOverride()
        {
            try
            {
                if (File.Exists(OverridePath)) { File.Delete(OverridePath); LastError = ""; }
                else LastError = "（没有找到开关文件，已是 cfg 配置）";
                return true;
            }
            catch (Exception e) { LastError = e.Message; return false; }
        }
    }
    class MainForm : Form
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
        
        Control pageQuick, pageSearch, pageFavorites, pageHistory, pagePresets, pageSaveBatch;
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
            Text = "Suzerain 存档修改器 v1.7.0 GUI";
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
                }
            }
            catch { }
            Shown += delegate
            {
                try { if (mainSplit != null) mainSplit.SplitterDistance = 220; } catch { }
                if (navTree != null) navTree.Invalidate(true);
                if (contentHost != null) contentHost.Invalidate(true);
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

            Panel bottom = new Panel(); bottom.Dock = DockStyle.Bottom; bottom.Height = 40; bottom.Padding = new Padding(4);
            Button btnSave = new Button(); btnSave.Text = "保存修改 (自动备份)"; btnSave.Left = 8; btnSave.Top = 7; btnSave.Width = 170;
            Button btnReload = new Button(); btnReload.Text = "重新载入"; btnReload.Left = 186; btnReload.Top = 7; btnReload.Width = 90;
            Button btnUndo = new Button(); btnUndo.Text = "撤销上一步"; btnUndo.Left = 284; btnUndo.Top = 7; btnUndo.Width = 100;
            lblStatus = new Label(); lblStatus.AutoSize = true; lblStatus.Left = 396; lblStatus.Top = 12; lblStatus.ForeColor = Color.DarkGreen;
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
            bottom.Controls.Add(btnSave); bottom.Controls.Add(btnReload); bottom.Controls.Add(btnUndo); bottom.Controls.Add(lblStatus);
            Controls.Add(bottom);
            Controls.Add(top);   
        }

        
        void PopulateNav()
        {
            navTree.BeginUpdate();
            navTree.Nodes.Clear();
            contentHost.Controls.Clear();
            if (!core.loaded)
            {
                Label empty = new Label();
                empty.Text = "请先在上方选择存档";
                empty.Dock = DockStyle.Fill;
                empty.TextAlign = ContentAlignment.MiddleCenter;
                empty.ForeColor = Color.Gray;
                empty.Font = new Font(Font, FontStyle.Bold);
                contentHost.Controls.Add(empty);
                
                if (HbBridge.Installed())
                {
                    TreeNode hbOnly = new TreeNode("HB 模组 (Horrifying Bombardment)");
                    AddNavPage(hbOnly, "模组开关与状态", pageHb);
                    navTree.Nodes.Add(hbOnly);
                    navTree.ExpandAll();
                }
                if (OiBridge.Installed())
                {
                    TreeNode rpOnly = new TreeNode("安济艺术 (Oikonomia)");
                    AddNavPage(rpOnly, "模组开关与状态", pageOi);
                    navTree.Nodes.Add(rpOnly);
                    navTree.ExpandAll();
                }
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
            AddNavPage(g, "全局搜索", pageSearch);
            AddNavPage(g, "收藏", pageFavorites);
            AddNavPage(g, "修改历史", pageHistory);
            AddNavPage(g, "修改方案", pagePresets);
            AddNavPage(g, "存档管理", pageSaveBatch);
            navTree.Nodes.Add(g);
            
            if (HbBridge.Installed())
            {
                TreeNode hb = new TreeNode("HB 模组 (Horrifying Bombardment)");
                AddNavPage(hb, "模组开关与状态", pageHb);
                navTree.Nodes.Add(hb);
            }
            
            if (OiBridge.Installed())
            {
                TreeNode rp = new TreeNode("安济艺术 (Oikonomia)");
                AddNavPage(rp, "模组开关与状态", pageOi);
                navTree.Nodes.Add(rp);
            }
            navTree.ExpandAll();
            if (navTree.Nodes.Count > 0 && navTree.Nodes[0].Nodes.Count > 0)
                navTree.SelectedNode = navTree.Nodes[0].Nodes[0];
            navTree.EndUpdate();
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
        }

        
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
            if (!core.loaded) return;
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
            if (cur == "true" || cur == "false")
            {
                string nv = cur == "true" ? "false" : "true";
                if (core.SetBoolVar(key, nv))
                {
                    RefreshAll(); RefreshQuickPanel(); RefreshFavoritesPanel(); RefreshHistoryPanel();
                    SetStatus(key + ": " + cur + " -> " + nv);
                }
                return;
            }
            string input = InputBox("修改数值", key + "  (当前=" + FormatVal(cur) + ")", FormatVal(cur));
            if (input == null) return;
            bool ok = IsNumeric(cur) ? core.SetVar(key, input) : core.SetStringVar(key, input);
            if (ok)
            {
                RefreshAll(); RefreshQuickPanel(); RefreshFavoritesPanel(); RefreshHistoryPanel();
                SetStatus(key + " -> " + input);
            }
            else SetStatus("修改失败: " + key);
        }

        
        Control BuildQuickPanel()
        {
            Panel p = new Panel();
            lvQuick = new ListView();
            lvQuick.Dock = DockStyle.Fill; lvQuick.View = View.Details; lvQuick.FullRowSelect = true;
            lvQuick.GridLines = true; lvQuick.MultiSelect = false;
            lvQuick.Columns.Add("常用项", 420); lvQuick.Columns.Add("当前值", 160);
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
            lv.Columns.Add("项目", 420); lv.Columns.Add("当前值", 160);
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

        
        Control BuildSearchPanel()
        {
            Panel p = new Panel();
            Panel top = new Panel(); top.Dock = DockStyle.Top; top.Height = 36;
            Label l = new Label(); l.Text = "搜索:"; l.Left = 10; l.Top = 9; l.AutoSize = true;
            txtGlobalSearch = new TextBox(); txtGlobalSearch.Left = 52; txtGlobalSearch.Top = 5; txtGlobalSearch.Width = 320;
            Button b = new Button(); b.Text = "搜索"; b.Left = 382; b.Top = 4; b.Width = 70;
            Button b2 = new Button(); b2.Text = "清空"; b2.Left = 458; b2.Top = 4; b2.Width = 70;
            top.Controls.Add(l); top.Controls.Add(txtGlobalSearch); top.Controls.Add(b); top.Controls.Add(b2);
            lvSearch = new ListView(); lvSearch.Dock = DockStyle.Fill; lvSearch.View = View.Details; lvSearch.FullRowSelect = true;
            lvSearch.GridLines = true; lvSearch.MultiSelect = false;
            lvSearch.Columns.Add("类型", 90); lvSearch.Columns.Add("名称/键", 420); lvSearch.Columns.Add("值", 140);
            AttachFavoriteMenu(lvSearch);
            p.Controls.Add(lvSearch); p.Controls.Add(top);
            b.Click += delegate { DoGlobalSearch(); };
            b2.Click += delegate { txtGlobalSearch.Text = ""; lvSearch.Items.Clear(); };
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

        void DoGlobalSearch()
        {
            lvSearch.BeginUpdate();
            lvSearch.Items.Clear();
            if (!core.loaded) { lvSearch.EndUpdate(); return; }
            string q = txtGlobalSearch.Text.Trim();
            if (q.Length == 0) { lvSearch.EndUpdate(); return; }
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
                    it.Tag = k; it.ToolTipText = k;
                    it.SubItems.Add(DisplayName(k));
                    it.SubItems.Add(FormatVal(v));
                    lvSearch.Items.Add(it);
                }
            }
            
            foreach (string e in core.ScanEntities())
            {
                if (e.IndexOf(q, StringComparison.OrdinalIgnoreCase) < 0) continue;
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
            lvFavorites.Columns.Add("收藏项", 420); lvFavorites.Columns.Add("当前值", 160);
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
            lvHistory.Columns.Add("类型", 70); lvHistory.Columns.Add("键/实体", 300); lvHistory.Columns.Add("旧值", 130); lvHistory.Columns.Add("新值", 130);
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
            lvPresets.Columns.Add("方案名", 300); lvPresets.Columns.Add("文件", 260);
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
            lvSaveBatch.Columns.Add("存档文件", 250); lvSaveBatch.Columns.Add("故事包", 160); lvSaveBatch.Columns.Add("保存名", 120); lvSaveBatch.Columns.Add("修改时间", 140);
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

        bool SyncChangesToFile(string path)
        {
            TrainerCore t = new TrainerCore();
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
            lvModifier.Columns.Add("修正项", 480); lvModifier.Columns.Add("当前值", 120);
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
            Panel top = new Panel(); top.Dock = DockStyle.Top; top.Height = 34;
            Label lp = new Label(); lp.Text = "范围:"; lp.Left = 10; lp.Top = 8; lp.AutoSize = true;
            fp.cmbPrefix = new ComboBox(); fp.cmbPrefix.DropDownStyle = ComboBoxStyle.DropDownList; fp.cmbPrefix.Left = 52; fp.cmbPrefix.Top = 5; fp.cmbPrefix.Width = 340;
            Label ls = new Label(); ls.Text = "搜索:"; ls.Left = 400; ls.Top = 8; ls.AutoSize = true;
            fp.txtSearch = new TextBox(); fp.txtSearch.Left = 440; fp.txtSearch.Top = 5; fp.txtSearch.Width = 140;
            Button btnRefresh = new Button(); btnRefresh.Text = "刷新"; btnRefresh.Left = 588; btnRefresh.Top = 5; btnRefresh.Width = 60;
            fp.lblPage = new Label(); fp.lblPage.AutoSize = true; fp.lblPage.Left = 660; fp.lblPage.Top = 10; fp.lblPage.ForeColor = Color.DimGray;
            fp.btnPrev = new Button(); fp.btnPrev.Text = "<"; fp.btnPrev.Left = 730; fp.btnPrev.Top = 5; fp.btnPrev.Width = 34;
            fp.btnNext = new Button(); fp.btnNext.Text = ">"; fp.btnNext.Left = 770; fp.btnNext.Top = 5; fp.btnNext.Width = 34;
            fp.cmbPrefix.SelectedIndexChanged += delegate { fp.page = 0; RefreshFilterPage(fp); };
            fp.txtSearch.TextChanged += delegate { fp.page = 0; RefreshFilterPage(fp); };
            btnRefresh.Click += delegate { fp.page = 0; RefreshFilterPage(fp); };
            fp.btnPrev.Click += delegate { if (fp.page > 0) { fp.page--; RefreshFilterPage(fp); } };
            fp.btnNext.Click += delegate { fp.page++; RefreshFilterPage(fp); };
            top.Controls.Add(lp); top.Controls.Add(fp.cmbPrefix); top.Controls.Add(ls); top.Controls.Add(fp.txtSearch);
            top.Controls.Add(btnRefresh); top.Controls.Add(fp.lblPage); top.Controls.Add(fp.btnPrev); top.Controls.Add(fp.btnNext);
            fp.lv = new ListView(); fp.lv.Dock = DockStyle.Fill; fp.lv.View = View.Details; fp.lv.FullRowSelect = true;
            fp.lv.GridLines = true; fp.lv.MultiSelect = true;
            fp.lv.Columns.Add("变量", 540); fp.lv.Columns.Add("当前值", 150);
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
                if (fp.txtSearch.Text.Trim().Length > 0 && !k.Contains(fp.txtSearch.Text.Trim())) continue;
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
            if (cur == "true" || cur == "false")
            {
                string nv = cur == "true" ? "false" : "true";
                if (core.SetBoolVar(key, nv)) { RefreshFilterPage(fp); SetStatus(key + ": " + cur + " -> " + nv); }
                return;
            }
            if (cur == "?") return;
            string input = InputBox("修改数值", key + "  (当前=" + FormatVal(cur) + ")", FormatVal(cur));
            if (input == null) return;
            bool ok;
            if (IsNumeric(cur)) ok = core.SetVar(key, input);
            else ok = core.SetStringVar(key, input);
            if (ok) { RefreshFilterPage(fp); SetStatus(key + " -> " + input); }
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
            lvEvents.Columns.Add("事件", 560); lvEvents.Columns.Add("值", 120);
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

        void RefreshOiPanel()
        {
            if (lblOiStatus == null) return;
            try
            {
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
                    + "     当前经济状态：" + OiBridge.State("economyState")
                    + "     地区档位：" + OiBridge.State("regionTiers"));
                sb.AppendLine("税务效率：" + (OiBridge.State("taxEfficient") == "true" ? "真" : "假")
                    + "     避税：" + (OiBridge.State("taxAvoidance") == "true" ? "真" : "假")
                    + "     增长词条生效：" + OiBridge.State("growthActive") + " 个"
                    + "     当前预算：" + OiBridge.State("budget"));
                sb.AppendLine("最近结算：回合 " + OiBridge.State("lastSettleTurn")
                    + "     合计 " + OiBridge.State("lastSettleTotal")
                    + "     明细：" + OiBridge.State("lastSettleDetail"));
                if (lblOiEcoState != null)
                    lblOiEcoState.Text = "经济状态：" + OiBridge.State("economyState")
                        + "     地区：" + OiBridge.State("regionTiers")
                        + "     税务效率：" + (OiBridge.State("taxEfficient") == "true" ? "真" : "假")
                        + "     避税：" + (OiBridge.State("taxAvoidance") == "true" ? "真" : "假")
                        + "     增长词条：" + OiBridge.State("growthActive") + " 个";
                if (lblOiEcoSettle != null)
                    lblOiEcoSettle.Text = "最近结算：回合 " + OiBridge.State("lastSettleTurn")
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
            cmbEntity = new ComboBox(); cmbEntity.DropDownStyle = ComboBoxStyle.DropDownList; cmbEntity.Left = 52; cmbEntity.Top = 5; cmbEntity.Width = 300;
            Label ls = new Label(); ls.Text = "搜索:"; ls.Left = 360; ls.Top = 8; ls.AutoSize = true;
            txtEntitySearch = new TextBox(); txtEntitySearch.Left = 400; txtEntitySearch.Top = 5; txtEntitySearch.Width = 150;
            Button b1 = new Button(); b1.Text = "刷新"; b1.Left = 558; b1.Top = 5; b1.Width = 60;
            top.Controls.Add(lp); top.Controls.Add(cmbEntity); top.Controls.Add(ls); top.Controls.Add(txtEntitySearch); top.Controls.Add(b1);
            txtEntitySearch.TextChanged += delegate { RefreshEntityList(); RefreshEntity(); };
            lvEntity = new ListView(); lvEntity.Dock = DockStyle.Fill; lvEntity.View = View.Details; lvEntity.FullRowSelect = true;
            lvEntity.GridLines = true; lvEntity.MultiSelect = false;
            lvEntity.Columns.Add("字段", 440); lvEntity.Columns.Add("值", 200);
            AttachFavoriteMenu(lvEntity);
            p2.Controls.Add(lvEntity);
            p2.Controls.Add(top);   
            Label hint = new Label(); hint.Dock = DockStyle.Bottom; hint.Height = 24; hint.Padding = new Padding(6, 4, 0, 0);
            hint.Text = "双击行修改字段值 (GDP/人口/关系/状态效果等)。";
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
