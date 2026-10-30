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
                string live = RealTimeBridge.StateFromCache("updatedAt", RealTimeBridge.HbPipeName);
                if (!string.IsNullOrEmpty(live))
                {
                    DateTime lt;
                    if (DateTime.TryParse(live, out lt)) return lt;
                }
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
                string live = RealTimeBridge.StateFromCache(key, RealTimeBridge.HbPipeName);
                if (!string.IsNullOrEmpty(live)) return live;
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
                sb.AppendLine("  \"source\": \"Suzerain 修改器 v1.7.1\"");
                sb.AppendLine("}");

                string json = sb.ToString();
                bool pipeSent = false;
                
                try { pipeSent = RealTimeBridge.TrySend(json, RealTimeBridge.HbPipeName); } catch { }

                try
                {
                    string tmp = OverridePath + ".tmp";
                    File.WriteAllText(tmp, json, new UTF8Encoding(false));
                    File.Copy(tmp, OverridePath, true);
                    File.Delete(tmp);
                    LastError = "";
                    return true;
                }
                catch (Exception fileError)
                {
                    if (pipeSent)
                    {
                        LastError = "管道已应用，但文件回退写入失败：" + fileError.Message;
                        return true;
                    }
                    throw;
                }
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
}
