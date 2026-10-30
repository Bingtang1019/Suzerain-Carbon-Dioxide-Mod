using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace SuzerainTrainer
{
    
    
    
    
    
    static class TpBridge
    {
        public const string StateName = "TerraPromissa.state.json";
        public const string OverrideName = "TerraPromissa.override.json";
        public static string LastError = "";

        public static string Dir()
        {
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "TerraPromissa");
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

        public static bool WriteDebugOverride(bool debugMode)
        {
            return WriteOverrides(debugMode, true);
        }

        
        public static bool WriteOverrides(bool debugMode, bool globalSaveMode)
        {
            LastError = "";
            try
            {
                Directory.CreateDirectory(Dir());
                string json = "{\"debugMode\":" + (debugMode ? "true" : "false")
                    + ",\"globalSaveMode\":" + (globalSaveMode ? "true" : "false") + "}";
                string tmp = OverridePath + ".tmp";
                File.WriteAllText(tmp, json, new UTF8Encoding(false));
                if (File.Exists(OverridePath)) File.Delete(OverridePath);
                File.Move(tmp, OverridePath);
                return true;
            }
            catch (Exception e)
            {
                LastError = e.GetType().Name + ": " + e.Message;
                return false;
            }
        }

        public static bool DeleteOverride()
        {
            try
            {
                if (File.Exists(OverridePath)) File.Delete(OverridePath);
                return true;
            }
            catch (Exception e)
            {
                LastError = e.GetType().Name + ": " + e.Message;
                return false;
            }
        }
    }
}
