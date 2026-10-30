
using System.Collections.Generic;

namespace Oikonomia
{
    internal sealed class EmergencyQuoteGroup
    {
        internal string Id;
        internal string Name;
        internal string[] BannedTags;
        internal int Priority;
        internal BudgetQuote[] Quotes;
    }

    internal static class EmergencyQuoteData
    {
        internal static readonly EmergencyQuoteGroup[] Groups =
        {
            new EmergencyQuoteGroup
            {
                Id = "embargo_arcasia",
                Name = "禁运阿卡西亚（ATO）",
                BannedTags = new[] { "arcasia" },
                Priority = 85,
                Quotes = new[]
                {
                    new BudgetQuote { Text = "在旧的国际分工体系中，外围国家注定只能处于落后地位，任由中心国家剥削。", Author = "劳尔·普雷维什", Source = "《拉丁美洲的经济发展及其主要问题》" },
                    new BudgetQuote { Text = "帝国主义不是一个可以合作的体制，而是一个必须被摧毁的体制。", Author = "切·格瓦拉", Source = "1962年联合国大会演讲" },
                    new BudgetQuote { Text = "外国资本对我们而言不仅仅是资本，它是帝国主义的直接干预，是对我们主权的侵犯。", Author = "萨尔瓦多·阿连德", Source = "1972年联合国大会演讲" },
                }
            },
            new EmergencyQuoteGroup
            {
                Id = "embargo_contana",
                Name = "禁运康塔纳（CSP）",
                BannedTags = new[] { "contana" },
                Priority = 85,
                Quotes = new[]
                {
                    new BudgetQuote { Text = "计划导致独裁。", Author = "弗里德里希·哈耶克", Source = "《通往奴役之路》" },
                    new BudgetQuote { Text = "资本主义的内在罪恶是幸福的不平等分配，而社会主义的内在美德是苦难的平等分配。", Author = "温斯顿·丘吉尔", Source = "1945年下议院演讲" },
                    new BudgetQuote { Text = "那些试图在地上建立天堂的人，最终总是把大地变成了地狱。", Author = "卡尔·波普尔", Source = "《开放社会及其敌人》" },
                }
            },
            new EmergencyQuoteGroup
            {
                Id = "embargo_monarchy",
                Name = "禁运君主国（伦堡）",
                BannedTags = new[] { "monarchy" },
                Priority = 85,
                Quotes = new[]
                {
                    new BudgetQuote { Text = "绝对的君主制是地球上唯一违背人类理性的政府形式。", Author = "阿尔杰农·西德尼", Source = "《论政府》" },
                    new BudgetQuote { Text = "没有任何一种制度比世袭制更能产生如此众多的暴君和傻瓜。", Author = "托马斯·杰斐逊", Source = "致约翰·亚当斯的信 (1816)" },
                    new BudgetQuote { Text = "世袭君主制不过是一场荒诞剧，它剥夺了后世子孙选择自己政府形式的权利。", Author = "托马斯·潘恩", Source = "《人的权利》" },
                }
            },
            new EmergencyQuoteGroup
            {
                Id = "embargo_arcasia_contana",
                Name = "同时禁运两极（ATO与CSP）",
                BannedTags = new[] { "arcasia", "contana" },
                Priority = 95,
                Quotes = new[]
                {
                    new BudgetQuote { Text = "我们拒绝加入任何一方阵营，我们只属于我们自己。", Author = "约瑟普·布罗兹·铁托", Source = "1948年南斯拉夫共产党代表大会" },
                    new BudgetQuote { Text = "不结盟不是消极的政策，而是积极的独立。", Author = "贾瓦哈拉尔·尼赫鲁", Source = "1961年贝尔格莱德不结盟运动峰会" },
                    new BudgetQuote { Text = "如果两个超级大国都不愿接纳我们，那我们就自己造一艘方舟。", Author = "菲德尔·卡斯特罗", Source = "1961年关于猪湾事件的演讲" },
                }
            },
            new EmergencyQuoteGroup
            {
                Id = "embargo_all",
                Name = "全球禁运（极端孤立）",
                BannedTags = new[] { "arcasia", "contana", "monarchy" },
                Priority = 100,
                Quotes = new[]
                {
                    new BudgetQuote { Text = "被所有人孤立的国家，如果不武装自己，就注定灭亡。", Author = "尼可罗·马基雅维利", Source = "《战争的艺术》" },
                    new BudgetQuote { Text = "自然状态是一切人反对一切人的战争。", Author = "托马斯·霍布斯", Source = "《利维坦》" },
                    new BudgetQuote { Text = "如果我们不能团结一致，我们必定会被一个个地送上绞刑架。", Author = "本杰明·富兰克林", Source = "1776年关于签署《独立宣言》的演讲" },
                }
            },
        };
    }
}
