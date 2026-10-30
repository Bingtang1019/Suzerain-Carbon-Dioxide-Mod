using System;
using BepInEx.Configuration;
using BepInEx.Logging;

namespace Oikonomia
{
    
    
    
    
    
    
    
    
    
    internal static class EconomyConfig
    {
        
        public static ConfigEntry<bool> Enabled;

        
        public static ConfigEntry<bool> EdEnabled;
        public static ConfigEntry<int> DangerDelta;     
        public static ConfigEntry<int> UnstableDelta;   
        public static ConfigEntry<int> StableDelta;     
        public static ConfigEntry<int> GrowingDelta;    
        public static ConfigEntry<int> VeryGoodDelta;   

        
        public static ConfigEntry<bool> RegionsEnabled;
        public static ConfigEntry<int> RegionStepDelta; 

        
        public static ConfigEntry<bool> TaxEnabled;
        public static ConfigEntry<int> TaxEfficientDelta;   
        public static ConfigEntry<int> TaxAvoidanceDelta;   
        public static ConfigEntry<int> TaxEvasionDelta;        
        public static ConfigEntry<string> FixedCostTerms;      
        public static ConfigEntry<string> PerTurnExtraConditions; 

        
        public static ConfigEntry<bool> TradeEnabled;
        public static ConfigEntry<int> TradeIncreasedDelta;   
        public static ConfigEntry<int> TradeRecoveredDelta;   
        public static ConfigEntry<int> TradeDecreasedDelta;   

        
        public static ConfigEntry<bool> TradeDealsEnabled;
        public static ConfigEntry<int> TradeDealBonus;

        
        public static ConfigEntry<bool> TourismEnabled;
        public static ConfigEntry<int> TourismGoodDelta;      
        public static ConfigEntry<int> TourismAverageDelta;   
        public static ConfigEntry<int> TourismBadDelta;       
        
        public static ConfigEntry<bool> GrowthEnabled;
        public static ConfigEntry<int> GrowthBonus;         

        public static ConfigEntry<bool> VerboseLog;
        
        public static ConfigEntry<bool> InGameDetail;
        
        public static ConfigEntry<bool> InGameDetailBackupSlot;
        
        public static ConfigEntry<bool> InGameDetailProducer;
        
        public static ConfigEntry<bool> IsRadioProbe;
        
        public static ConfigEntry<bool> C2MultiSelect;
        
        public static ConfigEntry<string> C2Prefixes;
        
        public static ConfigEntry<int> C2MinChoices;
        public static ConfigEntry<int> C2MaxChoices;
        
        public static ConfigEntry<bool> T1Experiment;
        
        public static ConfigEntry<bool> T1Both;
        public static ConfigEntry<bool> T1HookMenu;
        public static ConfigEntry<bool> T1HookText;
        public static ConfigEntry<bool> T1HookClick;
        public static ConfigEntry<bool> T1HookAsk;
        public static ConfigEntry<bool> T1HookPrefix;
        public static ConfigEntry<string> T1TargetText;
        public static ConfigEntry<string> T1OptionText;

        public static ConfigEntry<string> T1BothLine;
        public static ConfigEntry<string> T1ChainLileas;
        public static ConfigEntry<string> T1ChainGus;
        public static ConfigEntry<string> T1ChainSymon;
        public static ConfigEntry<string> T1ChainSymon2;
        public static ConfigEntry<string> T1Diag;

        public static ConfigEntry<bool> T1AskEnabled;
        public static ConfigEntry<string> T1AskToken;
        public static ConfigEntry<string> T1AskOption;
        public static ConfigEntry<string> T1AskSimon;
        
        
        public static ConfigEntry<string> T1AskSimonEcon;
        
        
        public static ConfigEntry<int> T1AskSimonEconGap;
        
        
        
        public static ConfigEntry<string> LogArchiveDir;
        
        
        
        public static ConfigEntry<bool> CompanyFix;
        
        
        
        public static ConfigEntry<bool> T3Enabled;
        
        
        public static ConfigEntry<bool> T3ForceUnderhall;
        
        
        public static ConfigEntry<bool> T3Probe;
        
        
        
        
        public static ConfigEntry<bool> V2Push;
        
        public static ConfigEntry<int> V2MaxPerProject;
        
        public static ConfigEntry<int> V2MaxTotal;
        
        public static ConfigEntry<bool> ScreenDump;
        
        public static ConfigEntry<bool> BenchEnabled;
        public static ConfigEntry<string> BenchScriptFile;
        public static ConfigEntry<int> BenchMaxSteps;
        public static ConfigEntry<int> BenchStepTimeoutSec;
        public static ConfigEntry<string> BenchReportFile;
        
        
        
        
        public static ConfigEntry<bool> InGameDetailFrame;
        
        
        
        
        public static ConfigEntry<string> InGameDetailStyle;
        
        public static ConfigEntry<bool> QuotesEnabled;
        
        public static ConfigEntry<bool> QuotesShowWhenDetailEmpty;
        
        public static ConfigEntry<string> QuotesStyle;
        public static ConfigEntry<bool> QuotesAnimations;
        public static ConfigEntry<bool> QuotesProbe;
        public static ConfigEntry<string> QuotesProbeDir;

        private static ManualLogSource _log;

        
        public static bool IsRadioProbeOn { get { return On(IsRadioProbe); } }
        
        public static bool C2MultiSelectOn { get { return On(C2MultiSelect); } }
        
        public static bool T1ExperimentOn { get { return On(T1Experiment); } }
        
        public static bool T1BothOn { get { return On(T1Both); } }
        public static bool T1HookMenuOn { get { return On(T1HookMenu); } }
        public static bool T1HookTextOn { get { return On(T1HookText); } }
        public static bool T1HookClickOn { get { return On(T1HookClick); } }
        public static bool T1HookAskOn { get { return On(T1HookAsk); } }
        public static bool T1HookPrefixOn { get { return On(T1HookPrefix); } }
        public static bool T3ForceUnderhallOn { get { try { return T3ForceUnderhall != null && T3ForceUnderhall.Value; } catch { return false; } } }
        public static bool T3ProbeOn { get { try { return T3Probe != null && T3Probe.Value; } catch { return false; } } }

        public static void Init(ConfigFile cfg, ManualLogSource log)
        {
            _log = log;

            T1Both = cfg.Bind("T1", "Enabled", false,
                "T1 真补丁（2.10.0）：Turn1 基建那段对话里追加一个「同时为两个项目拨款」选项，点击时本补丁执行那两句指令。"
                + "只做内存操作 + 日志、不改任何数据文件；false = 一个钩子都不挂（完全无副作用）。");
            T1HookMenu = cfg.Bind("T1", "HookMenu", true,
                "T1 逐钩子隔离：菜单 Postfix（OnConversationResponseMenu）。排查 UI 消失时可单独关。");
            T1HookText = cfg.Bind("T1", "HookText", true,
                "T1 逐钩子隔离：选项文本 Postfix（TemplateConversationResponse.SetLocalizableTexts）。");
            T1HookClick = cfg.Bind("T1", "HookClick", true,
                "T1 逐钩子隔离：点击拦截（OnResponseClick Prefix）。");
            T1HookAsk = cfg.Bind("T1", "HookAsk", true,
                "T1 逐钩子隔离：阶段二 Ask Prefix。");
            T1HookPrefix = cfg.Bind("T1", "HookPrefix", true,
                "T1 逐钩子隔离：两条 OnConversationResponseMenu Prefix（ref/CreateBefore）。");
            T1TargetText = cfg.Bind("T1", "TargetText", "基础设施",
                "作用域特征串：首项文本包含它才动手（不写死条目名；改成实测到的特征串即可）。");
            T1OptionText = cfg.Bind("T1", "OptionText", "同时为两个项目拨款",
                "追加项的文案（中文；改它不用重编译）。");

            T1BothLine = cfg.Bind("T1", "BothLine", "我决定同时推进 H-3 公路与 L-1 高铁两个项目——两者即刻开工。",
                "T1：第三项点击后先播的旁白（★ 改字不用重编译 ✓）");
            T1ChainLileas = cfg.Bind("T1", "ChainLileas", "“感谢上帝，我就知道你是个明白事理的人。”",
                "T1：同时开工链——莉亚斯那句（★ 与单选版措辞不同 ✓；★ 2.10.50 起带弯引号 “ ” ✓ —— 与游戏自己的角色台词同款 ✓）");
            T1ChainGus = cfg.Bind("T1", "ChainGus", "“这是个了不起的选择，总统先生。”",
                "T1：同时开工链——格斯那句（★ 与单选版措辞不同 ✓；★ 2.10.50 起带弯引号 “ ” ✓）");
            T1ChainSymon = cfg.Bind("T1", "ChainSymon", "“谈判将于今年年中开始，您可以将合同授予您选择的公司。”",
                "T1：同时开工链——西蒙第一句（★ 2.10.50 起带弯引号 “ ” ✓）");
            T1ChainSymon2 = cfg.Bind("T1", "ChainSymon2", "“我们估计，如果每一步都能顺利完成，工程将在两年内完成。”",
                "T1：同时开工链——西蒙第二句（★ 置空则只播第一句 ✓；★ 2.10.50 起带弯引号 “ ” ✓）");
            T1Diag = cfg.Bind("T1", "Diag", "false",
                "T1：诊断日志总开关（★ 默认 false ⇒ 排查期噪声静默 ✓；出问题时改 true ⇒ 37 行诊断全回来 ✓）");

            T1AskEnabled = cfg.Bind("T1Ask", "Enabled", true,
                "阶段二：在「那么，您想先听哪个？」那屏加一项（★ 2.10.46 起【默认启用】✓；"
                + "改 false ⇒ 一个钩子也不动手 ✓）。注意：已存在的旧 cfg 里若已有本行 = false ⇒ BepInEx 以文件为准 ✓，"
                + "要让新默认生效就把这一行改成 true（或整行删掉 ✓）。");
            T1AskToken = cfg.Bind("T1Ask", "Token", "H-3 高速公路项目。",
                "阶段二：作用域判据——该屏【任意一项】的文本含此串就动手 ✓（★ 不只看首项 ✗）。"
                + "默认串 = 实测那一屏的首项（★ 实时日志：2 项菜单，首项=「H-3 高速公路项目。」✓）；"
                + "置空 ⇒ 永不触发 ✓。与阶段一的判据（首项含「我已决定实施」）互斥 ✓。");
            T1AskOption = cfg.Bind("T1Ask", "Option", "如果我们设法追加预算，能否同时启动两个项目？",
                "阶段二：新增选项文本");
            T1AskSimon = cfg.Bind("T1Ask", "Simon", "“事实上，当然可以，但那样一来所付出的预算很可能将同我们所需的成果不匹配。”",
                "阶段二：点该选项后西蒙的回应（★ 2.10.50 起带【弯引号 “ ”】✓ —— 与游戏自己的数据同款 ✓："
                + "英文数据里就是 `“I have settled on a decision.”` ✓）。★ 引号属于文本本身 ✓ ⇒ 屏幕上所见即这里所写 ✓；"
                + "要改成别的形态 ⇒ 直接改这一行即可 ✓（改字不用重编译 ✓）");
            T1AskSimonEcon = cfg.Bind("T1Ask", "SimonEcon", "“这很有魄力。”",
                "阶段二：★ **学过经济学**时，西蒙【前置】多加的那一句 ✓（★ 用户原话：「如果学了经济学你说要考虑修两个，"
                + "西蒙会说这很有魄力」✓；★ 2.10.54 起【带句号】✓ —— 用户要求 ✓）。判据变量 = `BaseGame.Prologue_Studyplace_Lachaven_Economics` ✓"
                + "（★ 数据侧实测 ✓；同族另两个 = `…_Holsord_Law` ✓ / `…_Deyr_History` ✓）。"
                + "★ 行为 ✓：有经济学 ⇒ 这一句 ＋（按 `SimonEconGap` 个空行）＋ 原句 ✓；无 ⇒ 只用原句 ✓。"
                + "★ 置空 ⇒ 等于关掉这条 ✓（改字/关掉都不用重编译 ✓）");
            T1AskSimonEconGap = cfg.Bind("T1Ask", "SimonEconGap", 1,
                "阶段二：`SimonEcon` 与原句之间插几个【空行】（★ 1 = 空一行 ✓ —— 用户要求「多一个换行」✓；"
                + "0 = 紧接 ✓；★ 上限 3 ✓ 超出按 3 处理 ✓）");
            LogArchiveDir = cfg.Bind("Diag", "LogArchiveDir", "",
                "★【2.10.59】退出时把 BepInEx 的 `LogOutput.log` 归档到这个目录 ✓（★ 留最近 12 份 ✓）——"
                + "★ 实测 BepInEx 每次启动【覆盖】日志 ✗ ⇒ ★ 不归档就丢 ✓；★ 置空 ⇒ 关掉归档 ✓。"
                + "★ 注 ✓：★ 归档写在**本仓目录**里 ✗（★ 不往游戏目录里写 ✓ 守住只读规矩 ✓）");
            CompanyFix = cfg.Bind("Company", "Enabled", false,
                "★【2.10.60】按项目返回公司旗 ⇒ ★ 修「两个巨构各选一家却被显示成同一家」✓（★ 默认 false ✗）。"
                + "★ 原理 ✓：★ 公司旗的写入走【对话系统内部 Lua VM】✗ ⇒ 抓不到写口 ✗ ⇒ "
                + "★ 改为在【条件求值读口】按项目自答 ✓：★ 命中「`Investment_Highway/Railway` × `Corporation_*`」"
                + "且【已记下该项目选的公司】⇒ ★ 由我们判真假 ✓；★ 其余情形一律放行真值 ✗（★ 不误伤 ✓）。"
                + "★ 归属来源 ✓：★ 0.35 秒心跳 ＋ 【当前对话节点在哪一簇】✓（★ 判不出簇 ⇒ 不记 ✗ ⇒ 不介入 ✓）。");
            T3Enabled = cfg.Bind("T3", "Enabled", false,
                "★【2.10.112】T3（第三回合）Gus/Tusk 公司差分：按 Underhall > Taurus > SSC 归并作答。"
                + "★ 默认 false。只在 T3 场景白名单（Turn03 + Gus/BusinessCouncil/Tusk）内介入；"
                + "条件必须只含一个公司旗且已记下归属。Gus 缺 Underhall 分支时只记录、不硬改。"
                + "★ 需同时开启 [Company] Enabled=true 才有归属记录。");
            T3ForceUnderhall = cfg.Bind("T3", "ForceUnderhall", false,
                "★【2.10.116】调试用：T3 强制按 Underhall 读取（默认 false）。"
                + "打开后忽略项目归属，Journal/Corporation 公司读取只让 Underhall 为真，"
                + "用于只读采集 Underhall 差分。★ 只改读取名，不写任何游戏变量。");
            T3Probe = cfg.Bind("T3", "Probe", false,
                "★【2.10.124-dev】一次性完整 T3 Graph Probe（默认 false）："
                + "收集 master/default/loaded/model 数据库清单、当前对话、全部 entry/link、"
                + "关键 Journal/Corporation 变量，写 _verify/artifacts/t3-probe-*.md。只读。");
            V2Push = cfg.Bind("V2", "Enabled", false,
                "★【2.10.61】版本 2「解锁式推进」⇒ ★ 修「选了一个巨构后，另一个永远没机会」✓（★ 默认 false ✗）。"
                + "★ 原理 ✓：★ 游戏数据里【本来就有】一条「读 `Listened_*` 旗再 `Jump to:` 回讲解」的循环 ✓"
                + "（★ F2 实测 ✓：节点 92–101 ✓）⇒ ★ 我们只在读口对 `…Listened_X == false` 答 false ✓ ⇒ "
                + "★ 引擎自己跳回讲解 ✓ ⇒ ★ 两个项目**依次**落地、互不覆盖 ✓。"
                + "★★ 立身之本 ✓：★ **只回答一个 bool** ✗ —— ★ **绝不写任何游戏变量** ✓；"
                + "★ 判不出 / 未命中 / 超限 ⇒ ★ **一律放行真值** ✓（★ 绝不错改 ✓）。");
            V2MaxPerProject = cfg.Bind("V2", "MaxPerProject", 1,
                "★【2.10.61】每个项目最多推进几次（★ 默认 1 ✓ —— ★ 防无限跳 ✗）");
            BenchEnabled = cfg.Bind("Bench", "Enabled", false,
                "★【2.10.62】自动化测试台 ⇒ ★ 脚本列出「依次选哪些项」✓ ⇒ ★ 插件调游戏自己的 OnResponseClick（等价于点它）✓ · "
                + "★ 支持 `stop <子串>`（★ 只跑到某一屏就停 ✓）与 `expect-flag <变量> <true|false>`（★ 自动出 PASS/FAIL ✓）· "
                + "★ 默认关 ✓ · ★ 步数/超时都有硬上限 ✓ · ★ 不改游戏变量 ✗");
            BenchScriptFile = cfg.Bind("Bench", "ScriptFile", "",
                "★【2.10.62】测试脚本路径 ✓（★ UTF-8 ✓ · ★ `#` 开头 = 注释 ✓）—— ★ **默认空 = 不用** ✗（★ 不留本机绝对路径 ✓）");
            BenchMaxSteps = cfg.Bind("Bench", "MaxSteps", 120, "★【2.10.62】步数硬上限 ✓（★ 防跑飞 ✓）");
            BenchStepTimeoutSec = cfg.Bind("Bench", "StepTimeoutSec", 30, "★【2.10.62】每步超时（秒）✓ ⇒ ★ 超时即出 FAIL ＋ 停 ✓");
            BenchReportFile = cfg.Bind("Bench", "ReportFile", "",
                "★【2.10.62】PASS/FAIL 报告落盘路径 ✓（★ 追加写 ✓ —— ★ 供无人值守 ✓）");
            ScreenDump = cfg.Bind("Diag", "ScreenDump", false,
                "★【2.10.62】屏幕转储 ✓：★ 每屏把「回应项 id ↔ 文本」写进日志 ✓ ⇒ ★ 供【离线抽取 ↔ 运行时真值】对撞 ✓（★ 只读 ✓ 默认关 ✓ 上限 400 行 ✓）");
            V2MaxTotal = cfg.Bind("V2", "MaxTotal", 4,
                "★【2.10.61】全局最多推进几次（★ 默认 4 ✓ —— ★ 防无限跳 ✗；★ 超限 ⇒ 放行真值 ＋ 醒目日志 ✓）");
            T1Experiment = cfg.Bind("T1Exp", "Enabled", false,
                "T1 最小实验（2.9.8）：对话菜单建好后追加一个测试项，点击只打一行日志。"
                + "**不执行任何指令、不改任何数据**；false = 一个钩子都不挂（完全无副作用 ✓）");
            C2MultiSelect = cfg.Bind("C2", "MultiSelect", false,
                "投资类决策页可多选（C2，2.9.4）：把目标决策页的 Min/MaxChoiceCount 放宽 ⇒ 玩家能多选/全选。"
                + "**只改内存、不写任何文件**；false = 一个钩子都不挂（完全无副作用 ✓）");
            C2Prefixes = cfg.Bind("C2", "Prefixes",
                "Turn06_Decision_Megaproject|Turn04_Decision_RegionalInvestment|Turn08_Decision_IndustrialExpansion|Turn01_Decision_InfrastructureProject",
                "目标决策的**身份前缀族**（`|` 分隔）—— 按前缀匹配 ⇒ 不写死条目名 ⇒ RoR 新增项（如 …Megaproject_Arvory）自动覆盖 ✓");
            C2MinChoices = cfg.Bind("C2", "MinChoices", 1,
                "最少可选数（1 = 至少选一个；0 = 允许一个都不选，取决于游戏是否接受 ✓）");
            C2MaxChoices = cfg.Bind("C2", "MaxChoices", 9,
                "最多可选数（放宽到该值 ⇒ 可多选/全选；改这个数不用重编译 ✓）");

            IsRadioProbe = cfg.Bind("Probe", "IsRadioProbe", true,
                "只读探针（2.9.3）：在日志里打印决策页的 radio / 多选形状与选项集合，用于判断"
                + "「Turn 1/4/6/8 的决策页到底是单选还是多选」。**只写日志，不改变任何游戏行为**；"
                + "false = 一个钩子都不挂（探针完全停用）");

            Enabled = cfg.Bind("Economy", "Enabled", true,
                "经济结算总开关（false = 四条机制全部停用，完全等同原版）");

            EdEnabled = cfg.Bind("Economy.ED", "Enabled", true,
                "ED 机制：按经济显示状态每回合调整预算（本条为主机制）");
            DangerDelta = cfg.Bind("Economy.ED", "DangerDelta", -2, "显示「危险」时每回合预算变化");
            UnstableDelta = cfg.Bind("Economy.ED", "UnstableDelta", -1, "显示「不稳定」时每回合预算变化");
            StableDelta = cfg.Bind("Economy.ED", "StableDelta", 0, "显示「稳定」时每回合预算变化");
            GrowingDelta = cfg.Bind("Economy.ED", "GrowingDelta", 1, "显示「增长」时每回合预算变化");
            VeryGoodDelta = cfg.Bind("Economy.ED", "VeryGoodDelta", 1, "显示「极快增长」时每回合预算变化（本体不出现，DLC 用）");

            RegionsEnabled = cfg.Bind("Economy.Regions", "Enabled", true,
                "地区三档词条：贝吉亚/格鲁尼/洛伦/阿格兰 升档或降档时调整预算");
            RegionStepDelta = cfg.Bind("Economy.Regions", "StepDelta", 1,
                "每升降一个档位的预算变化（按档数计：跨两档 = 该值 ×2）");

            TaxEnabled = cfg.Bind("Economy.Tax", "Enabled", true, "税务组：税务效率经济 + 避税");
            TaxEfficientDelta = cfg.Bind("Economy.Tax", "EfficientDelta", 1,
                "「税务效率经济」为真时每回合预算变化");
            TaxAvoidanceDelta = cfg.Bind("Economy.Tax", "AvoidanceDelta", -1,
                "「避税」由假变真时一次性预算变化（不重复触发；逃税按设计不产生减益）");
            TaxEvasionDelta = cfg.Bind("Economy.Tax", "EvasionDelta", -1, "逃税为真时**每回合**预算变化（2.4.0 由「无效果」恢复为每回合扣除）");
            FixedCostTerms = cfg.Bind("Economy.PerTurn", "FixedCosts", "BaseGame.Situation_Welfare_FreeMedicine=-1",
                "每回合固定成本词条。两种写法都支持："
                + "① 变量名=增量（多个用 | 或 ; 分隔，旧格式）；"
                + "② 变量名|显示名|增量|组名（第 4 段组名可省略；同组项的增量求和后只显示一行，用组名作文案）。"
                + "默认：免费处方药（FreeMedicine）每回合 -1，自动归入「额外福利支出」组。");
            PerTurnExtraConditions = cfg.Bind("Economy.PerTurn", "ExtraConditions", "",
                "额外「每回合条件词条」：格式 变量名|显示名|增量 或 变量名|显示名|增量|组名（第 4 段为组名，可省略），"
                + "多条用 ; 分隔。填在这里即可新增同类机制，**无需改代码**"
                + "（例：BaseGame.Situation_Order_Corruption|腐败|-1 或 "
                + "BaseGame.Situation_X|某福利|-1|额外福利支出）。");

            TradeEnabled = cfg.Bind("Economy.Trade", "Enabled", true,
                "贸易量三档：每回合按当前档位调整预算（增加 / 恢复 / 减少）");
            TradeIncreasedDelta = cfg.Bind("Economy.Trade", "IncreasedDelta", 2, "贸易量「增加」时每回合预算变化");
            TradeRecoveredDelta = cfg.Bind("Economy.Trade", "RecoveredDelta", 1, "贸易量「恢复」时每回合预算变化");
            TradeDecreasedDelta = cfg.Bind("Economy.Trade", "DecreasedDelta", 0, "贸易量「减少」时每回合预算变化");

            TradeDealsEnabled = cfg.Bind("Economy.TradeDeals", "Enabled", true,
                "贸易协议：每签订一份（首次生效）一次性加成");
            TradeDealBonus = cfg.Bind("Economy.TradeDeals", "BonusPerDeal", 1, "每份贸易协议的一次性预算变化");

            TourismEnabled = cfg.Bind("Economy.Tourism", "Enabled", true,
                "旅游业三档：每回合按当前档位调整预算（蓬勃发展 / 平均水平 / 严重衰退）");
            TourismGoodDelta = cfg.Bind("Economy.Tourism", "GoodDelta", 1, "旅游业「蓬勃发展」时每回合预算变化");
            TourismAverageDelta = cfg.Bind("Economy.Tourism", "AverageDelta", 0, "旅游业「平均水平」时每回合预算变化");
            TourismBadDelta = cfg.Bind("Economy.Tourism", "BadDelta", -1, "旅游业「严重衰退」时每回合预算变化");

            GrowthEnabled = cfg.Bind("Economy.Growth", "Enabled", true,
                "增长组：8 个增长类词条由假变真时各触发一次");
            GrowthBonus = cfg.Bind("Economy.Growth", "BonusPerTerm", 1,
                "每个增长词条由假变真时的一次性预算变化");

            VerboseLog = cfg.Bind("Economy", "VerboseLog", false,
                "输出每次结算的明细日志（含各机制分项）");

            InGameDetail = cfg.Bind("Economy", "InGameDetail", true,
                "游戏内预算加减明细：把每回合「为何加减」渲染进 HUD「政府预算」的 tooltip"
                + "（2.7.3 起追加到 statTooltipText —— 游戏悬停真正渲染的字段；游戏原文原样保留，"
                + "明细段用成对标记包住，重复注入幂等）。只对本体战役生效。"
                + "false = 完全不介入，行为等同 2.6.0（明细只写日志与文件桥）");

            InGameDetailBackupSlot = cfg.Bind("Economy", "InGameDetailBackupSlot", true,
                "同时把同一份明细写进备用槽 statTooltipModifierText（2.7.2 的旧路径，游戏实测不渲染它，"
                + "留着是给「万一某些界面读它」上保险）。"
                + "若悬停里明细出现了**两份**（说明该槽位某个界面也在渲染），把这一项设为 false 即可。");

            InGameDetailProducer = cfg.Bind("Economy", "InGameDetailProducer", true,
                "【2.7.4 路线①】把明细注入到**悬停渲染源的生产者** TemplateHUDStat.GetStatTooltipText() 的返回值里，"
                + "并挂一条重写看门狗（游戏每次改写主槽文本时即时补回明细）。"
                + "理由：2.7.2/2.7.3 都证明了「只写字段」会被游戏随后的重写冲掉（真机日志 #3 读到 51 字符）——"
                + "在生产者处注入，游戏自己拼出来的那串文本里就带着明细，谁渲染都跑不掉。"
                + "false = 退回 2.7.3 行为（只写字段，可能仍被冲掉）");

            InGameDetailFrame = cfg.Bind("Economy", "InGameDetailFrame", false,
                "【2.7.9 引入 · 2.8.0 起默认 false】把预算明细**画进一个文本框**"
                + "（在文本里画出 ASCII 边框/内边距/居中标题，每行显示宽度严格相等）。"
                + "【为什么默认关了】用户真机实测 2.7.9 的文本框架**散架**（「各条之间甚至不能构成一个框」）："
                + "① TMP 的 enableWordWrapping=true 时，框整行比纯文本宽 6~8 列（两根竖线 + 左右内边距 + 条目缩进），"
                + "一旦超出 tooltip 文本框宽就会在框内折行；② 游戏字体未必等宽，靠空格凑出来的列（边框/内边距/居中）会错位。"
                + "【作用范围】只对 Detailed 形态有效 —— Rizia 形态**没有框**，本开关在那边完全无效（写了也不会画框）。"
                + "true = 把 Detailed 形态画进文本框（2.7.9 的行为，代码原样保留，随时可开回来）。");

            QuotesEnabled = cfg.Bind("Economy.Quotes", "Enabled", false,
                "【2.11.0】政府预算 HUD tooltip 最下方追加 4×4 预算引文。默认关；"
                + "关闭时与 2.10.125 的 tooltip 文本逐字节相同。");

            QuotesShowWhenDetailEmpty = cfg.Bind("Economy.Quotes", "ShowWhenDetailEmpty", true,
                "明细为空（尚未预演）时仍显示引文；false = 退回只在有明细时显示。");

            QuotesAnimations = cfg.Bind("Economy.Quotes", "Animations", true,
                "引文文本动效总开关：Fade/Subtle/Electric/Crt 四种效果按引文格自动分配；false = 稳定静态引文。");
            QuotesProbe = cfg.Bind("Economy.Quotes", "Probe", false,
                "【排查】一次性动画探针：写详细报告到 ProbeDir；默认 false。");
            QuotesProbeDir = cfg.Bind("Economy.Quotes", "ProbeDir", @"C:\Users\dell\Desktop\Suzerain\_verify\artifacts",
                "【排查】探针报告目录。");
            QuotesStyle = cfg.Bind("Economy.Quotes", "Style", "Attributed",
                "引文排版：Attributed = 正文 + ——作者，《出处》；Bare = 只显示正文。");

            InGameDetailStyle = cfg.Bind("Economy", "InGameDetailStyle", "Rizia",
                "【2.8.0 引入 · 2.9.0 修正为真机形态】明细形态。"
                + "Rizia（默认）= 逐字节照搬 Rizia 的真实形态（2.9.0 按真机探针实测返回值重做）："
                + "首行 Per Turn:，然后**按正负分两组** —— 正数组在前、负数组在后，每组组头是 (±该组合计) <u>Total:</u>，"
                + "组内明细按数值降序、用 <indent=5%> … </indent> 包住，每条形态为 <color=色>(±N)</color> 说明"
                + "（**颜色只包数值**，色值取统计实例的 tooltipPositiveColor/tooltipNegativeColor，"
                + "实测 #0ED10E / #D11111，读不到回退这两个常量）；零值条目不进任何一组。"
                + "没有框、没有标题、没有【未来】/【当前】段头、没有分隔线、没有小计、全串末尾无换行"
                + "（证据：docs/rizia-真实明细格式-样本.txt，本机探针实测归档）。"
                + "Detailed = 2.7.8 的形态（标题 + 两段段头 + 小计，可选文本框），与 2.7.8 **逐字节相同**，"
                + "既有黄金串与基线比对全部照旧。"
                + "其余任何取值（含大小写不同与拼写错误）都按 Rizia 处理 —— 配置写坏也只会退回 Rizia，"
                + "绝不会意外退回那个真机散架的带框形态。");
            QuoteAnimProbe.Init(QuotesProbeOn, QuotesProbeDirValue);
        }

        
        public static bool EffectiveEnabled { get { return Bridge.OvEconomyEnabled ?? (Enabled != null && Enabled.Value); } }
        public static int EffDangerDelta { get { return Bridge.OvDangerDelta ?? Value(DangerDelta, -2); } }
        public static int EffUnstableDelta { get { return Bridge.OvUnstableDelta ?? Value(UnstableDelta, -1); } }
        public static int EffStableDelta { get { return Bridge.OvStableDelta ?? Value(StableDelta, 0); } }
        public static int EffGrowingDelta { get { return Bridge.OvGrowingDelta ?? Value(GrowingDelta, 1); } }
        public static int EffVeryGoodDelta { get { return Bridge.OvVeryGoodDelta ?? Value(VeryGoodDelta, 1); } }
        public static int EffRegionStepDelta { get { return Bridge.OvRegionStepDelta ?? Value(RegionStepDelta, 1); } }
        public static int EffTaxEfficientDelta { get { return Bridge.OvTaxEfficientDelta ?? Value(TaxEfficientDelta, 1); } }
        public static int EffTaxAvoidanceDelta { get { return Bridge.OvTaxAvoidanceDelta ?? Value(TaxAvoidanceDelta, -1); } }
        public static int EffTaxEvasionDelta { get { return Bridge.OvTaxEvasionDelta ?? Value(TaxEvasionDelta, -1); } }
        public static string FixedCostsRaw { get { return FixedCostTerms != null ? (FixedCostTerms.Value ?? "") : ""; } }
        
        public static string PerTurnExtraConditionsRaw { get { return PerTurnExtraConditions != null ? (PerTurnExtraConditions.Value ?? "") : ""; } }
        public static int EffGrowthBonus { get { return Bridge.OvGrowthBonus ?? Value(GrowthBonus, 1); } }

        private static int Value(ConfigEntry<int> e, int dflt) { return e != null ? e.Value : dflt; }
        private static bool On(ConfigEntry<bool> e) { return e != null && e.Value; }
        
        private static string Raw(ConfigEntry<string> e) { return e != null ? (e.Value ?? "") : ""; }

        public static bool EdOn { get { return On(EdEnabled); } }
        
        public static bool InGameDetailBackupSlotOn { get { return On(InGameDetailBackupSlot); } }
        
        public static bool InGameDetailProducerOn { get { return On(InGameDetailProducer); } }
        public static bool QuotesEnabledOn { get { return On(QuotesEnabled); } }
        public static bool QuotesShowWhenDetailEmptyOn { get { return On(QuotesShowWhenDetailEmpty); } }
        public static bool QuotesAnimationsOn { get { return On(QuotesAnimations); } }
        public static bool QuotesProbeOn { get { return On(QuotesProbe); } }
        public static string QuotesProbeDirValue
        {
            get { return QuotesProbeDir == null ? "" : (QuotesProbeDir.Value ?? "").Trim(); }
        }
        public static bool QuotesAttributedOn
        {
            get
            {
                try
                {
                    return QuotesStyle == null
                        || string.IsNullOrEmpty(QuotesStyle.Value)
                        || !string.Equals(QuotesStyle.Value.Trim(), "Bare", StringComparison.OrdinalIgnoreCase);
                }
                catch { return true; }
            }
        }
        
        public static bool InGameDetailFrameOn { get { return On(InGameDetailFrame); } }
        
        public static int InGameDetailStyleKind
        {
            get { return InGameDetailFormat.ParseStyle(InGameDetailStyle != null ? InGameDetailStyle.Value : null); }
        }
        
        public static string InGameDetailStyleRaw
        {
            get { return InGameDetailStyle != null ? (InGameDetailStyle.Value ?? "") : ""; }
        }
        public static bool RegionsOn { get { return On(RegionsEnabled); } }
        public static bool TaxOn { get { return On(TaxEnabled); } }
        public static bool GrowthOn { get { return On(GrowthEnabled); } }
        public static bool TourismOn { get { return On(TourismEnabled); } }
        public static bool TradeOn { get { return On(TradeEnabled); } }
        public static bool TradeDealsOn { get { return On(TradeDealsEnabled); } }
        public static int EffTradeIncreasedDelta { get { return Bridge.OvTradeIncreasedDelta ?? Value(TradeIncreasedDelta, 2); } }
        public static int EffTradeRecoveredDelta { get { return Bridge.OvTradeRecoveredDelta ?? Value(TradeRecoveredDelta, 1); } }
        public static int EffTradeDecreasedDelta { get { return Bridge.OvTradeDecreasedDelta ?? Value(TradeDecreasedDelta, 0); } }
        public static int EffTradeDealBonus { get { return Bridge.OvTradeDealBonus ?? Value(TradeDealBonus, 1); } }
        public static int EffTourismGoodDelta { get { return Bridge.OvTourismGoodDelta ?? Value(TourismGoodDelta, 1); } }
        public static int EffTourismAverageDelta { get { return Bridge.OvTourismAverageDelta ?? Value(TourismAverageDelta, 0); } }
        public static int EffTourismBadDelta { get { return Bridge.OvTourismBadDelta ?? Value(TourismBadDelta, -1); } }
        public static void LogValues(string reason)
        {
            try
            {
                _log.LogInfo("[OI] " + reason + "："
                    + "经济=" + Enabled.Value
                    + " | ED=" + EdEnabled.Value + "(" + DangerDelta.Value + "/" + UnstableDelta.Value
                        + "/" + StableDelta.Value + "/" + GrowingDelta.Value + ")"
                    + " | 地区=" + RegionsEnabled.Value + "(步长" + RegionStepDelta.Value + ")"
                    + " | 税务=" + TaxEnabled.Value + "(效率" + TaxEfficientDelta.Value + ",避税" + TaxAvoidanceDelta.Value + ")"
                    + " | 增长=" + GrowthEnabled.Value + "(+" + GrowthBonus.Value + ")"
                    + " | 旅游/贸易=" + TourismEnabled.Value + "/" + TradeEnabled.Value
                    + " | 明细=" + (InGameDetail != null && InGameDetail.Value)
                        + "（" + (InGameDetailStyleKind == InGameDetailFormat.StyleRizia ? "Rizia" : "Detailed")
                        + "，生产者=" + (InGameDetailProducerOn ? "开" : "关") + "）");
                if (VerboseLog == null || !VerboseLog.Value) return;
                _log.LogDebug("[OI] " + reason + "（详细）："
                    + "旅游=" + TourismGoodDelta.Value + "/" + TourismAverageDelta.Value + "/" + TourismBadDelta.Value
                    + " 贸易=" + TradeIncreasedDelta.Value + "/" + TradeRecoveredDelta.Value + "/" + TradeDecreasedDelta.Value
                    + " 协议=" + TradeDealsEnabled.Value + "(+" + TradeDealBonus.Value + ")"
                    + " | T1=" + (T1Both != null && T1Both.Value)
                        + " 判据=[" + Raw(T1TargetText) + "]"
                        + " 选项=[" + Raw(T1OptionText) + "]"
                        + " Diag=[" + Raw(T1Diag) + "]"
                    + " | T1Ask=" + (T1AskEnabled != null && T1AskEnabled.Value)
                    + " | T3=" + (T3Enabled != null && T3Enabled.Value));
            }
            catch { }
        }
    }
}
