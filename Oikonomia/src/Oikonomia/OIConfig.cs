using System;
using System.IO;
using System.Threading;
using BepInEx.Configuration;
using BepInEx.Logging;

namespace Oikonomia
{
    
    
    
    
    internal static class OIConfig
    {
        private static ConfigFile _cfg;
        private static ManualLogSource _log;
        private static Timer _watch;
        private static DateTime _lastWriteUtc;

        
        public static ConfigEntry<bool> Enabled;

        
        public static ConfigEntry<int> MaxDecrees;

        
        public static ConfigEntry<string> AffectedPanels;

        
        public static ConfigEntry<bool> HidePageCounter;

        
        public static ConfigEntry<bool> HideSubtitle;

        
        public static ConfigEntry<bool> VerboseLog;

        
        public static ConfigEntry<bool> DynamicLimit;
        public static ConfigEntry<int> LimitBase;
        public static ConfigEntry<int> MinistryBonus;
        public static ConfigEntry<int> MinistryFullSetBonus;
        public static ConfigEntry<int> EconomyBonus;
        public static ConfigEntry<int> EconomyBonusTurn;
        public static ConfigEntry<int> ConstitutionBonus;
        public static ConfigEntry<int> ConstitutionVoteThreshold;
        public static ConfigEntry<int> EmergencyBonus;
        public static ConfigEntry<int> CrisisPenalty;
        public static ConfigEntry<int> CorruptionPenalty;
        public static ConfigEntry<int> UnrestPenalty;
        public static ConfigEntry<int> RevoltPenalty;
        public static ConfigEntry<int> LimitFloor;
        
        public static ConfigEntry<string> VarMinistryBudget, VarEconomyGood, VarConstitutionVote,
            VarConstitutionPassed, VarEmergency, VarCrisis, VarCorruption, VarProtests, VarRevolt;

        public static void Init(ConfigFile cfg, ManualLogSource log)
        {
            _cfg = cfg;
            _log = log;

            Enabled = cfg.Bind("General", "Enabled", true,
                "总开关。关闭后行为完全等同原版（同时恢复被隐藏的 UI 元素）。");
            MaxDecrees = cfg.Bind("General", "MaxDecrees", 500,
                "总统令上限。0 = 不修改（原版 3）；默认 500。注意：数值越大，签署时原生循环开销越高"
                + "（实测 Max=20 约 0.2 秒、10000 约 1.6 秒、21 亿约 6 秒），500 几乎无感。");
            AffectedPanels = cfg.Bind("General", "AffectedPanels", "*",
                "生效面板（名称子串，逗号分隔）。默认 * = 全部一次性面板（总统令 + 法案）。");
            HidePageCounter = cfg.Bind("UI", "HidePageCounter", true,
                "隐藏底部的页码点位（原生会按上限创建点位）。");
            HideSubtitle = cfg.Bind("UI", "HideSubtitle", true,
                "隐藏面板副标题（原版文案写死'最多 3 个'，与本模组行为不符）。");
            VerboseLog = cfg.Bind("General", "VerboseLog", false, "输出详细日志（排查用）。");

            DynamicLimit = cfg.Bind("Decrees", "DynamicLimit", true,
                "动态上限（行政能力）：按「基数 + 部门预算 + 经济/宪法/紧急 − 危机/腐败/抗议/起义」计算，下限 2、上限不限。"
                + "关闭则使用 General.MaxDecrees 的固定值。");
            LimitBase = cfg.Bind("Decrees", "Base", 4, "可签署数量基数（原版 3；本模组默认 4）。");
            MinistryBonus = cfg.Bind("Decrees", "MinistryBonus", 1, "每增加一个部门的预算：+N。");
            MinistryFullSetBonus = cfg.Bind("Decrees", "MinistryFullSetBonus", 2, "四个部门预算**全部**增加时，在上述基础上额外 +N（规则：四部门全加 = +4 再额外 +2）。");
            EconomyBonus = cfg.Bind("Decrees", "EconomyBonus", 1, "经济状态为「增长」时：+N。");
            EconomyBonusTurn = cfg.Bind("Decrees", "EconomyBonusTurn", 8, "经济加成生效的最小回合（T8）；0 = 不限制。");
            ConstitutionBonus = cfg.Bind("Decrees", "ConstitutionBonus", 1, "宪法改革以达标票数通过议会：+N。");
            ConstitutionVoteThreshold = cfg.Bind("Decrees", "ConstitutionVoteThreshold", 225, "宪法通过所需票数门槛（默认 225）。");
            EmergencyBonus = cfg.Bind("Decrees", "EmergencyBonus", 2, "开启紧急状态：+N。");
            CrisisPenalty = cfg.Bind("Decrees", "CrisisPenalty", -2, "触发任一危机：N（负数为扣减）。");
            CorruptionPenalty = cfg.Bind("Decrees", "CorruptionPenalty", -1, "存在「腐败」词条时：N（负数为扣减）。");
            UnrestPenalty = cfg.Bind("Decrees", "UnrestPenalty", -1, "仍有「全国范围的和平抗议」：N。");
            RevoltPenalty = cfg.Bind("Decrees", "RevoltPenalty", -2, "仍有「全国起义（武斗）」：N。");
            LimitFloor = cfg.Bind("Decrees", "Floor", 2, "可签署数量下限（避免被扣到 0）。");

            VarMinistryBudget = cfg.Bind("Decrees.Vars", "MinistryBudget",
                "BaseGame.Turn03_EnT_Military_Budget_Increase|BaseGame.Turn03_EnT_LawEnforcement_Budget_Increase|"
                + "BaseGame.Turn03_EnT_Healthcare_Budget_Increase|BaseGame.Turn03_EnT_Education_Budget_Increase",
                "四个部门的「预算增加」标志（任一为 true 记 1 个部门）。");
            VarEconomyGood = cfg.Bind("Decrees.Vars", "EconomyGood",
                "BaseGame.Economy_Indicator == 1|BaseGame.Economy_Indicator == 2",
                "经济状态为「增长」的判定条件（Articy 表达式，任一成立即算）。");
            VarConstitutionVote = cfg.Bind("Decrees.Vars", "ConstitutionVote", "BaseGame.Reform_Assembly_Vote",
                "宪法改革议会票数变量（数值型，与 ConstitudeVoteThreshold 比较）。");
            VarConstitutionPassed = cfg.Bind("Decrees.Vars", "ConstitutionPassed", "BaseGame.Reform_Reform_AssemblyVote_Success",
                "宪法改革是否通过（true/false 变量；留空则只看票数）。");
            VarEmergency = cfg.Bind("Decrees.Vars", "Emergency", "BaseGame.Policy_Law_EmergencyDecree",
                "紧急状态是否生效（可用 | 列多个，任一为 true 即算）。");
            VarCrisis = cfg.Bind("Decrees.Vars", "Crisis",
                "BaseGame.Situation_Economy_GlobalTradeWar|BaseGame.Situation_Economy_MarketsCrash|"
                + "BaseGame.Situation_Economy_DebtCrisis|BaseGame.Faction_USP_Obstructionist",
                "危机词条（任一为 true 即 −2）。前三项为已确认的本体词条；"
                + "「党内沟通断绝」本体无同名变量，暂用 Faction_USP_Obstructionist（党内阻挠）代理，若不准请改此项。");
            VarCorruption = cfg.Bind("Decrees.Vars", "Corruption", "BaseGame.Situation_Order_Corruption",
                "「腐败」词条判定变量（2.5.0 新增；已核实真实存档中存在且可为 true）。");
            VarProtests = cfg.Bind("Decrees.Vars", "Protests", "BaseGame.Situation_Order_Nationwide_Protests", "和平抗议词条。");
            VarRevolt = cfg.Bind("Decrees.Vars", "Revolt", "BaseGame.Situation_Order_Nationwide_Revolt", "武斗/起义词条。");

            LogValues("初始化");
            StartWatch();
        }

        
        public static bool IsPanelAffected(string panelName)
        {
            var filter = AffectedPanels == null ? "*" : (AffectedPanels.Value ?? "*");
            filter = filter.Trim();
            if (filter.Length == 0 || filter == "*") return true;
            if (string.IsNullOrEmpty(panelName)) return false;
            foreach (var part in filter.Split(','))
            {
                var token = part.Trim();
                if (token.Length == 0) continue;
                if (panelName.IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0) return true;
            }
            return false;
        }

        
        public static bool ShouldApply
        {
            get { return EffectiveEnabled && EffectiveMax > 0; }
        }

        

        public static bool EffectiveEnabled
        {
            get
            {
                try
                {
                    if (Bridge.HasOverride && Bridge.OvEnabled.HasValue) return Bridge.OvEnabled.Value;
                    return Enabled != null && Enabled.Value;
                }
                catch { return false; }
            }
        }

        public static int EffectiveMax
        {
            get
            {
                try
                {
                    if (Bridge.HasOverride && Bridge.OvMaxDecrees.HasValue) return Bridge.OvMaxDecrees.Value;
                    return MaxDecrees == null ? 0 : MaxDecrees.Value;
                }
                catch { return 0; }
            }
        }

        public static bool EffectiveHideCounter
        {
            get
            {
                try
                {
                    if (Bridge.HasOverride && Bridge.OvHideCounter.HasValue) return Bridge.OvHideCounter.Value;
                    return HidePageCounter != null && HidePageCounter.Value;
                }
                catch { return false; }
            }
        }

        public static bool EffectiveHideSubtitle
        {
            get
            {
                try
                {
                    if (Bridge.HasOverride && Bridge.OvHideSubtitle.HasValue) return Bridge.OvHideSubtitle.Value;
                    return HideSubtitle != null && HideSubtitle.Value;
                }
                catch { return false; }
            }
        }

        
        public static bool EffectiveDynamicLimit
        {
            get
            {
                try
                {
                    if (Bridge.HasOverride && Bridge.OvDynamicLimit.HasValue) return Bridge.OvDynamicLimit.Value;
                    return DynamicLimit != null && DynamicLimit.Value;
                }
                catch { return false; }
            }
        }

        public static int TargetValue
        {
            get
            {
                try
                {
                    if (DynamicLimit != null && DynamicLimit.Value && DecreeLimit.Cached > 0) return DecreeLimit.Cached;
                }
                catch { }
                return EffectiveMax;
            }
        }

        private static void StartWatch()
        {
            try
            {
                var path = _cfg != null ? _cfg.ConfigFilePath : null;
                if (string.IsNullOrEmpty(path) || !File.Exists(path)) return;
                _lastWriteUtc = File.GetLastWriteTimeUtc(path);
                _watch = new Timer(_ => Poll(path), null, 1000, 1000);
            }
            catch (Exception e)
            {
                try { _log.LogWarning("[OI] 配置热重载监听启动失败：" + e.Message); } catch { }
            }
        }

        private static void Poll(string path)
        {
            try
            {
                if (!File.Exists(path)) return;
                var t = File.GetLastWriteTimeUtc(path);
                if (t == _lastWriteUtc) return;
                _lastWriteUtc = t;
                _cfg.Reload();
                LogValues("配置已热重载");
            }
            catch { }
        }

        public static void LogValues(string reason)
        {
            try
            {
                _log.LogInfo("[OI] " + reason + "："
                    + "Enabled=" + Enabled.Value
                    + ", MaxDecrees=" + MaxDecrees.Value
                    + ", AffectedPanels=" + AffectedPanels.Value
                    + ", HidePageCounter=" + HidePageCounter.Value
                    + ", HideSubtitle=" + HideSubtitle.Value);
                _log.LogInfo("[OI] 动态上限=" + (DynamicLimit != null && DynamicLimit.Value ? ("开（基数 " + LimitBase.Value + "，下限 " + LimitFloor.Value + "）") : "关")
                    + (DecreeLimit.Cached > 0 ? ("，当前值 " + DecreeLimit.Cached) : ""));
                EconomyConfig.LogValues(reason + "（经济）");
            }
            catch { }
        }
    }
}
