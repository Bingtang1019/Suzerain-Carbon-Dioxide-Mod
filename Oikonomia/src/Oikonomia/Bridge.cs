using System;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using BepInEx.Logging;

namespace Oikonomia
{
    
    
    
    
    
    
    
    
    
    internal static class Bridge
    {
        public const int ApiVersion = 2;

        private static ManualLogSource _log;
        private static Timer _poll;
        private static string _lastRaw;
        private static DateTime _lastStateWriteUtc = DateTime.MinValue;

        public static string Dir
        {
            get
            {
                return Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "Oikonomia");
            }
        }

        public static string StatePath { get { return Path.Combine(Dir, "Oikonomia.state.json"); } }
        public static string OverridePath { get { return Path.Combine(Dir, "Oikonomia.override.json"); } }
        
        
        
        
        
        
        
        
        
        public static string LastReadError { get; private set; }

        
        
        
        
        
        
        
        
        public static string LastWriteError { get; private set; }

        
        
        
        
        

        
        public static string LastOutcome { get; private set; }

        
        public static bool HasOverride { get; private set; }
        public static bool? OvEnabled { get; private set; }
        public static int? OvMaxDecrees { get; private set; }
        public static bool? OvHideCounter { get; private set; }
        public static bool? OvHideSubtitle { get; private set; }
        
        public static bool? OvEconomyEnabled { get; private set; }
        public static int? OvDangerDelta { get; private set; }
        public static int? OvUnstableDelta { get; private set; }
        public static int? OvStableDelta { get; private set; }
        public static int? OvGrowingDelta { get; private set; }
        public static int? OvVeryGoodDelta { get; private set; }
        public static int? OvRegionStepDelta { get; private set; }
        public static int? OvTaxEfficientDelta { get; private set; }
        public static int? OvTaxAvoidanceDelta { get; private set; }
        public static int? OvTaxEvasionDelta { get; private set; }
        public static int? OvLimitBase { get; private set; }   
        public static bool? OvDynamicLimit { get; private set; }   
        public static int? OvGrowthBonus { get; private set; }
        public static int? OvTourismGoodDelta { get; private set; }
        public static int? OvTourismAverageDelta { get; private set; }
        public static int? OvTourismBadDelta { get; private set; }
        public static int? OvTradeIncreasedDelta { get; private set; }
        public static int? OvTradeRecoveredDelta { get; private set; }
        public static int? OvTradeDecreasedDelta { get; private set; }
        public static int? OvTradeDealBonus { get; private set; }

        public static void Stop()
        {
            try { _poll?.Dispose(); } catch { }
            try { BridgePipe.Stop(); } catch { }
        }

        public static void Init(ManualLogSource log)
        {
            _log = log;
            LastReadError = "";
            LastWriteError = "";
            try
            {
                Directory.CreateDirectory(Dir);
                
                
                
                if (ReadOverride()) _log.LogInfo("[OI][bridge] " + DescribeOverride());
                _poll = new Timer(_ => Poll(), null, 1000, 1000);
                BridgePipe.Start(_log);
                _log.LogInfo("[OI][bridge] 文件桥目录：" + Dir + "；命名管道=" + BridgePipe.PipeName);
            }
            catch (Exception e)
            {
                LastReadError = e.Message;
                try { _log.LogWarning("[OI][bridge] 初始化失败（不影响模组本体）：" + e.Message); } catch { }
            }
        }

        private static void Poll()
        {
            try
            {
                
                
                
                
                if (ReadOverride())
                    _log.LogInfo("[OI][bridge] " + DescribeOverride());
            }
            catch { }   
        }

        
        
        
        
        
        
        
        
        
        
        
        
        
        private static bool ReadOverride()
        {
            
            var prevOutcome = LastOutcome;
            var prevRaw = _lastRaw;
            var prevHasOverride = HasOverride;

            try
            {
                if (!File.Exists(OverridePath))
                {
                    _lastRaw = null;
                    ResetOverrideFields();
                    LastOutcome = BridgeOutcome.Absent;
                    goto done;
                }
                var raw = File.ReadAllText(OverridePath, Encoding.UTF8);
                _lastRaw = raw;
                ApplyOverrideJson(raw);
            }
            catch (Exception ex)
            {
                LastReadError = ex.Message;
                HasOverride = false;
                LastOutcome = BridgeOutcome.ReadFailed;
                _lastRaw = null;   
            }

        done:
            
            return LastOutcome != prevOutcome || _lastRaw != prevRaw || HasOverride != prevHasOverride;
        }

        
        
        
        
        internal static void ApplyOverrideJson(string raw)
        {
            ResetOverrideFields();
            LastReadError = "";
            LastOutcome = BridgeOutcome.Applied;

            
            var ver = ReadInt(raw, "apiVersion", ApiVersion);
            if (ver > ApiVersion)
            {
                LastOutcome = BridgeOutcome.VersionUnsupported;
                _log.LogWarning("[OI][bridge] override 的 apiVersion=" + ver + " 高于本模组支持的 " + ApiVersion + "，已忽略。");
                return;
            }

            HasOverride = true;
            var e = ReadBool(raw, "enabled", true);
            if (e.HasValue) OvEnabled = e.Value;
            var m = ReadInt(raw, "maxDecrees", 0);
            if (m > 0) OvMaxDecrees = m;
            var hc = ReadBool(raw, "hidePageCounter", false);
            if (hc.HasValue) OvHideCounter = hc.Value;
            var hs = ReadBool(raw, "hideSubtitle", false);
            if (hs.HasValue) OvHideSubtitle = hs.Value;

            
            var ee = ReadBool(raw, "economyEnabled", false);
            if (ee.HasValue) OvEconomyEnabled = ee.Value;
            OvDangerDelta = ReadNullableInt(raw, "dangerDelta");
            OvUnstableDelta = ReadNullableInt(raw, "unstableDelta");
            OvStableDelta = ReadNullableInt(raw, "stableDelta");
            OvGrowingDelta = ReadNullableInt(raw, "growingDelta");
            OvVeryGoodDelta = ReadNullableInt(raw, "veryGoodDelta");
            OvRegionStepDelta = ReadNullableInt(raw, "regionStepDelta");
            OvTaxEfficientDelta = ReadNullableInt(raw, "taxEfficientDelta");
            OvTaxAvoidanceDelta = ReadNullableInt(raw, "taxAvoidanceDelta");
            OvTaxEvasionDelta = ReadNullableInt(raw, "taxEvasionDelta");
            OvLimitBase = ReadNullableInt(raw, "limitBase");
            OvDynamicLimit = ReadBool(raw, "dynamicLimit", false);
            OvGrowthBonus = ReadNullableInt(raw, "growthBonus");
            OvTourismGoodDelta = ReadNullableInt(raw, "tourismGoodDelta");
            OvTourismAverageDelta = ReadNullableInt(raw, "tourismAverageDelta");
            OvTourismBadDelta = ReadNullableInt(raw, "tourismBadDelta");
            OvTradeIncreasedDelta = ReadNullableInt(raw, "tradeIncreasedDelta");
            OvTradeRecoveredDelta = ReadNullableInt(raw, "tradeRecoveredDelta");
            OvTradeDecreasedDelta = ReadNullableInt(raw, "tradeDecreasedDelta");
            OvTradeDealBonus = ReadNullableInt(raw, "tradeDealBonus");
        }

        private static void ResetOverrideFields()
        {
            HasOverride = false;
            OvEnabled = null;
            OvMaxDecrees = null;
            OvHideCounter = null;
            OvHideSubtitle = null;
            OvEconomyEnabled = null; OvDangerDelta = null; OvUnstableDelta = null; OvStableDelta = null;
            OvGrowingDelta = null; OvVeryGoodDelta = null; OvRegionStepDelta = null;
            OvTaxEfficientDelta = null; OvTaxAvoidanceDelta = null; OvTaxEvasionDelta = null;
            OvLimitBase = null; OvDynamicLimit = null; OvGrowthBonus = null;
            OvTourismGoodDelta = null; OvTourismAverageDelta = null; OvTourismBadDelta = null;
            OvTradeIncreasedDelta = null; OvTradeRecoveredDelta = null; OvTradeDecreasedDelta = null; OvTradeDealBonus = null;
        }

        public static string DescribeOverride()
        {
            if (!HasOverride)
            {
                
                var line = BridgeOutcome.Describe(LastOutcome, OverridePath, LastReadError);
                if (line != null) return line;
                return "未发现 override 文件（路径：" + OverridePath + "），使用 cfg 配置。";
            }
            return "已应用修改器设置：Enabled=" + (OvEnabled.HasValue ? OvEnabled.Value.ToString() : "(cfg)")
                + ", MaxDecrees=" + (OvMaxDecrees.HasValue ? OvMaxDecrees.Value.ToString() : "(cfg)")
                + ", HidePageCounter=" + (OvHideCounter.HasValue ? OvHideCounter.Value.ToString() : "(cfg)")
                + ", HideSubtitle=" + (OvHideSubtitle.HasValue ? OvHideSubtitle.Value.ToString() : "(cfg)")
                + " ｜ 经济=" + (OvEconomyEnabled.HasValue ? OvEconomyEnabled.Value.ToString() : "(cfg)")
                + " ED[" + NullInt(OvDangerDelta) + "/" + NullInt(OvUnstableDelta) + "/" + NullInt(OvStableDelta)
                + "/" + NullInt(OvGrowingDelta) + "/" + NullInt(OvVeryGoodDelta) + "]"
                + " 地区步长=" + NullInt(OvRegionStepDelta)
                + " 税务[" + NullInt(OvTaxEfficientDelta) + "/" + NullInt(OvTaxAvoidanceDelta) + "]"
                + " 增长=" + NullInt(OvGrowthBonus);
        }

        
        private static string _ecoEdState = "";
        private static string _ecoRegions = "";
        private static bool _ecoTaxEff, _ecoTaxAvoid;
        private static int _ecoGrowthActive = -1;
        private static int _ecoLastTurn = -1, _ecoLastTotal, _ecoBudget = int.MinValue;
        private static string _ecoLastDetail = "";

        public static void SetEconomyState(string edState, string regions, bool taxEff, bool taxAvoid,
                                           int growthActive, int lastTurn, int lastTotal, string lastDetail, int budget)
        {
            try
            {
                _ecoEdState = edState ?? "";
                _ecoRegions = regions ?? "";
                _ecoTaxEff = taxEff;
                _ecoTaxAvoid = taxAvoid;
                _ecoGrowthActive = growthActive;
                _ecoLastTurn = lastTurn;
                _ecoLastTotal = lastTotal;
                _ecoLastDetail = lastDetail ?? "";
                _ecoBudget = budget;
            }
            catch { }
        }

        

        private static int _decreeLimit = -1;
        private static string _decreeLimitDetail = "";

        
        public static void SetDecreeLimit(int value, string detail)
        {
            try { _decreeLimit = value; _decreeLimitDetail = detail ?? ""; } catch { }
        }
        private static string _lastPanelName = "";
        private static int _lastEnacted;

        public static void WriteState(string reason, string panelName, int enactedCount)
        {
            try
            {
                
                if (!string.IsNullOrEmpty(panelName)) _lastPanelName = panelName;
                if (enactedCount >= 0) _lastEnacted = enactedCount;
                panelName = _lastPanelName;
                enactedCount = _lastEnacted;
                
                var now = DateTime.UtcNow;
                if ((now - _lastStateWriteUtc).TotalMilliseconds < 900) return;
                _lastStateWriteUtc = now;

                string json = BuildStateJson(reason, panelName, enactedCount);
                var tmp = StatePath + ".tmp";
                File.WriteAllText(tmp, json, new UTF8Encoding(false));
                File.Copy(tmp, StatePath, true);
                File.Delete(tmp);
            }
            catch (Exception e) { LastWriteError = e.Message; }
        }

        
        internal static string BuildStateForPipe()
        {
            return BuildStateJson("pipe", _lastPanelName, _lastEnacted);
        }

        private static string BuildStateJson(string reason, string panelName, int enactedCount)
        {
            var sb = new StringBuilder();
            sb.AppendLine("{");
            sb.AppendLine("  \"apiVersion\": " + ApiVersion + ",");
            sb.AppendLine("  \"name\": \"Oikonomia\",");
            sb.AppendLine("  \"modVersion\": \"" + Plugin.PluginVersion + "\",");
            sb.AppendLine("  \"pid\": " + System.Diagnostics.Process.GetCurrentProcess().Id + ",");
            sb.AppendLine("  \"updatedAt\": \"" + DateTime.Now.ToString("yyyy-MM-dd'T'HH:mm:ssK", CultureInfo.InvariantCulture) + "\",");
            sb.AppendLine("  \"reason\": \"" + Escape(reason) + "\",");
            sb.AppendLine("  \"enabled\": " + Bool(OIConfig.EffectiveEnabled) + ",");
            sb.AppendLine("  \"maxDecrees\": " + OIConfig.EffectiveMax + ",");
            sb.AppendLine("  \"hidePageCounter\": " + Bool(OIConfig.EffectiveHideCounter) + ",");
            sb.AppendLine("  \"hideSubtitle\": " + Bool(OIConfig.EffectiveHideSubtitle) + ",");
            sb.AppendLine("  \"source\": \"" + (HasOverride ? "override" : "config") + "\",");
            sb.AppendLine("  \"panelName\": \"" + Escape(panelName ?? "") + "\",");
            sb.AppendLine("  \"enactedCount\": " + enactedCount + ",");
            sb.AppendLine("  \"economyEnabled\": " + Bool(EconomyConfig.EffectiveEnabled) + ",");
            sb.AppendLine("  \"economyState\": \"" + Escape(_ecoEdState) + "\",");
            sb.AppendLine("  \"regionTiers\": \"" + Escape(_ecoRegions) + "\",");
            sb.AppendLine("  \"taxEfficient\": " + Bool(_ecoTaxEff) + ",");
            sb.AppendLine("  \"taxAvoidance\": " + Bool(_ecoTaxAvoid) + ",");
            sb.AppendLine("  \"growthActive\": " + _ecoGrowthActive + ",");
            sb.AppendLine("  \"lastSettleTurn\": " + _ecoLastTurn + ",");
            sb.AppendLine("  \"lastSettleTotal\": " + _ecoLastTotal + ",");
            sb.AppendLine("  \"lastSettleDetail\": \"" + Escape(_ecoLastDetail) + "\",");
            sb.AppendLine("  \"budget\": " + (_ecoBudget == int.MinValue ? "null" : _ecoBudget.ToString(CultureInfo.InvariantCulture)));
            sb.AppendLine("}");
            return sb.ToString();
        }

        private static string Bool(bool b) { return b ? "true" : "false"; }

        private static string Escape(string s)
        {
            return (s ?? "").Replace("\\", "\\\\").Replace("\"", "\\\"");
        }

        private static bool? ReadBool(string raw, string key, bool dfltUnused)
        {
            var m = Regex.Match(raw, "\"" + Regex.Escape(key) + "\"\\s*:\\s*(true|false)", RegexOptions.IgnoreCase);
            if (!m.Success) return null;
            return m.Groups[1].Value.Equals("true", StringComparison.OrdinalIgnoreCase);
        }

        private static string NullInt(int? v) { return v.HasValue ? v.Value.ToString(CultureInfo.InvariantCulture) : "(cfg)"; }

        
        private static int? ReadNullableInt(string raw, string key)
        {
            var m = Regex.Match(raw, "\"" + Regex.Escape(key) + "\"\\s*:\\s*(-?\\d+)");
            if (!m.Success) return null;
            int v;
            return int.TryParse(m.Groups[1].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out v) ? (int?)v : null;
        }

        private static int ReadInt(string raw, string key, int dflt)
        {
            var m = Regex.Match(raw, "\"" + Regex.Escape(key) + "\"\\s*:\\s*(-?\\d+)");
            if (!m.Success) return dflt;
            int v;
            return int.TryParse(m.Groups[1].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out v) ? v : dflt;
        }
    }
}
