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
                string live = RealTimeBridge.StateFromCache(key);
                if (!string.IsNullOrEmpty(live)) return live;
            }
            catch { }
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
                sb.AppendLine("  \"source\": \"Suzerain 修改器 v1.7.1\"");
                sb.AppendLine("}");

                string json = sb.ToString();
                bool pipeSent = false;
                
                try { pipeSent = RealTimeBridge.TrySend(json); } catch { }

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
                if (File.Exists(OverridePath)) { File.Delete(OverridePath); LastError = ""; }
                else LastError = "（没有找到开关文件，已是 cfg 配置）";
                try { RealTimeBridge.ClearCache(); } catch { }
                return true;
            }
            catch (Exception e) { LastError = e.Message; return false; }
        }
    }
}
