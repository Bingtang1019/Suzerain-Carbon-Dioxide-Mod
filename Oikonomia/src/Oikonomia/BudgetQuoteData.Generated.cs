
using System.Collections.Generic;

namespace Oikonomia
{
    internal static class BudgetQuoteData
    {
        internal static readonly Dictionary<string, BudgetQuote[]> Cells =
            new Dictionary<string, BudgetQuote[]>
        {
            { "Crisis_Collapsing", new[]
                {
                    new BudgetQuote { Text = "国债如果积累到一定程度，在那些国家里几乎无一例外地会摧毁该国的公共信用。", Author = "亚当·斯密", Source = "《国富论》第五卷" },
                    new BudgetQuote { Text = "信用扩张所创造的繁荣，是一场必将以不可避免的崩溃收场的幻梦。", Author = "路德维希·冯·米塞斯", Source = "《人的行为》" },
                    new BudgetQuote { Text = "信用一旦动摇，恐慌便如野火般蔓延，任何准备金都无法阻挡。", Author = "沃尔特·白芝浩", Source = "《伦巴第街》" },
                } },
            { "Crisis_Worsening", new[]
                {
                    new BudgetQuote { Text = "将当代的支出留给后代偿还，名为融资，实为针对未来的一场大规模欺诈。", Author = "约翰·亚当斯", Source = "致托马斯·杰斐逊的信 (1816)" },
                    new BudgetQuote { Text = "国债一旦超过限度，就会像沉重的枷锁一样，压垮国家的所有活力与信用。", Author = "亚当·斯密", Source = "《国富论》第五卷" },
                    new BudgetQuote { Text = "要么国家毁灭公共信用，要么公共信用毁灭国家。", Author = "大卫·休谟", Source = "《论国债》" },
                } },
            { "Crisis_Stagnant", new[]
                {
                    new BudgetQuote { Text = "欧洲各国目前背负的沉重债务，其发展历程一直相当一致，这些债务压迫着它们，最终可能会摧毁它们。", Author = "亚当·斯密", Source = "《国富论》第五卷" },
                    new BudgetQuote { Text = "我们比先进国家落后了五十年至一百年。我们必须在十年内跑完这段距离。要么我们做到这一点，要么我们被人打倒。", Author = "约瑟夫·斯大林", Source = "1931年《论经济工作人员的任务》" },
                    new BudgetQuote { Text = "每个国家的国民，都应当尽可能按照各自能力的比例，缴纳国赋，以维持政府的生存。", Author = "亚当·斯密", Source = "《国富论》第五卷" },
                } },
            { "Crisis_Improving", new[]
                {
                    new BudgetQuote { Text = "一笔国债，只要它不过度，对我们来说将是一份国家的恩赐。", Author = "亚历山大·汉密尔顿", Source = "《关于公共信用的报告》" },
                    new BudgetQuote { Text = "衰退是经济体系自我清理与重生的过程，它无情地清除掉那些无法适应新环境的旧有结构。", Author = "约瑟夫·熊彼特", Source = "《商业周期》" },
                    new BudgetQuote { Text = "要么我们拯救这个国家，要么我们与它一起毁灭。", Author = "阿道夫·希特勒", Source = "1932年竞选演讲" },
                } },
            { "Unstable_Collapsing", new[]
                {
                    new BudgetQuote { Text = "疾病在初起时容易医治，但难以察觉；若任其发展壮大，则无药可救。", Author = "尼可罗·马基雅维利", Source = "《君主论》第三章" },
                    new BudgetQuote { Text = "当心微小的开销；一个小小的漏水口，足以让一艘巨轮沉没。", Author = "本杰明·富兰克林", Source = "《穷理查年鉴》" },
                    new BudgetQuote { Text = "当狂喜退去，债务的现实依然存在。", Author = "约翰·肯尼思·加尔布雷思", Source = "《1929年大崩盘》" },
                } },
            { "Unstable_Worsening", new[]
                {
                    new BudgetQuote { Text = "公共信用一旦丧失，便极难恢复，且必然伴随着国家力量的衰退。", Author = "亚历山大·汉密尔顿", Source = "《联邦党人文集》第三十篇" },
                    new BudgetQuote { Text = "当国家开始无节制地借贷时，它实际上是在给未来的暴政铺路。", Author = "大卫·休谟", Source = "《论国债》" },
                    new BudgetQuote { Text = "国库是国家的血脉，一旦流血不止，躯体的崩溃只是时间问题。", Author = "西塞罗", Source = "《论义务》" },
                } },
            { "Unstable_Stagnant", new[]
                {
                    new BudgetQuote { Text = "从长远来看，我们都已经死了。", Author = "约翰·梅纳德·凯恩斯", Source = "《货币改革论》" },
                    new BudgetQuote { Text = "旧的世界正在死去，新的世界尚未诞生；在这个空位期，各种病态现象涌现出来。", Author = "安东尼奥·葛兰西", Source = "《狱中札记》" },
                    new BudgetQuote { Text = "在经济停滞与动荡的交织期，政策的无力感往往比市场的萧条本身更让人绝望。", Author = "约翰·肯尼思·加尔布雷思", Source = "《不确定的年代》" },
                } },
            { "Unstable_Improving", new[]
                {
                    new BudgetQuote { Text = "成功不是终点，失败并非致命：重要的是继续前进的勇气。", Author = "温斯顿·丘吉尔", Source = "1941年哈罗公学演讲" },
                    new BudgetQuote { Text = "没有经济上的平等，政治自由只是一座建立在沙滩上的城堡。", Author = "萨尔瓦多·阿连德", Source = "1972年联合国大会演讲" },
                    new BudgetQuote { Text = "经济生活的一切都在国家之中。在国家之外，没有任何经济生活存在。", Author = "贝尼托·墨索里尼", Source = "1932年《法西斯主义的学说》" },
                } },
            { "Stable_Collapsing", new[]
                {
                    new BudgetQuote { Text = "文明的死亡，往往是自杀，而不是他杀。", Author = "阿诺德·约瑟夫·汤因比", Source = "《历史研究》" },
                    new BudgetQuote { Text = "在表面的平静之下，最可怕的往往是那些未被察觉的暗流。", Author = "修昔底德", Source = "《伯罗奔尼撒战争史》" },
                    new BudgetQuote { Text = "当国库看似充盈时，往往是最容易滋生腐败与浪费的时刻。", Author = "埃德蒙·伯克", Source = "《对法国大革命的反思》" },
                } },
            { "Stable_Worsening", new[]
                {
                    new BudgetQuote { Text = "金融体系的脆弱性会随着繁荣的持续而不断增加。", Author = "海曼·明斯基", Source = "《金融不稳定假说》" },
                    new BudgetQuote { Text = "对于一个糟糕的政府来说，最危险的时刻往往是它开始改革的时候。", Author = "亚历克西斯·德·托克维尔", Source = "《旧制度与大革命》" },
                    new BudgetQuote { Text = "那些看似最稳固的财政体系，往往崩溃得最为彻底。", Author = "沃尔特·白芝浩", Source = "《伦巴第街》" },
                } },
            { "Stable_Stagnant", new[]
                {
                    new BudgetQuote { Text = "每一个节俭的人，都是公众的恩人。", Author = "亚当·斯密", Source = "《国富论》" },
                    new BudgetQuote { Text = "多余，其实是一件绝对必要的东西。", Author = "伏尔泰", Source = "《世俗之人》" },
                    new BudgetQuote { Text = "一个国家的财富并不取决于其货币的数量，而取决于其生产的商品和服务。", Author = "约翰·斯图亚特·穆勒", Source = "《政治经济学原理》" },
                } },
            { "Stable_Improving", new[]
                {
                    new BudgetQuote { Text = "检验我们进步的标准，不是看我们是否为富裕者锦上添花，而是看我们是否使贫困者丰衣足食。", Author = "富兰克林·D·罗斯福", Source = "1937年第二任就职演说" },
                    new BudgetQuote { Text = "一个国家真正的财富，在于其人民的美德与知识，而非国库里的金银。", Author = "西蒙·玻利瓦尔", Source = "1815年《牙买加来信》" },
                    new BudgetQuote { Text = "每一个人追求自身利益，往往比他有意去促进社会利益时更有效地促进社会利益。", Author = "亚当·斯密", Source = "《国富论》" },
                } },
            { "Wealthy_Collapsing", new[]
                {
                    new BudgetQuote { Text = "经济控制不仅仅是对人类生活一部分的控制，它是对实现所有目的的手段的控制。", Author = "弗里德里希·哈耶克", Source = "《通往奴役之路》" },
                    new BudgetQuote { Text = "财富创造了贫困。富人的繁荣带来了穷人的苦难，而这种极端的失衡最终会吞噬整个社会。", Author = "爱德华多·加莱亚诺", Source = "《拉丁美洲被切开的血管》" },
                    new BudgetQuote { Text = "你不能既把钱花掉，又同时留着它。", Author = "塞缪尔·约翰逊", Source = "《约翰逊传》" },
                } },
            { "Wealthy_Worsening", new[]
                {
                    new BudgetQuote { Text = "当特权阶层不再感到羞耻，当他们把财富的挥霍视为理所当然，这个社会的繁荣就已经开始腐坏了。", Author = "奥克塔维奥·帕斯", Source = "《孤独的迷宫》" },
                    new BudgetQuote { Text = "问题不在于我们是否应该计划，而在于我们是应该为竞争做计划，还是反对竞争。", Author = "弗里德里希·哈耶克", Source = "《通往奴役之路》" },
                    new BudgetQuote { Text = "财富所带来的傲慢，是导致毁灭的根源。", Author = "修昔底德", Source = "《伯罗奔尼撒战争史》" },
                } },
            { "Wealthy_Stagnant", new[]
                {
                    new BudgetQuote { Text = "在巨富中死去，是一种耻辱。", Author = "安德鲁·卡内基", Source = "《财富的福音》" },
                    new BudgetQuote { Text = "只要财富集中在少数人手中，这个国家就不可能拥有真正的和平与稳定。", Author = "西蒙·玻利瓦尔", Source = "1815年《牙买加来信》" },
                    new BudgetQuote { Text = "征税与讨人喜欢，就像爱与明智一样，不能并存。", Author = "埃德蒙·伯克", Source = "《论美洲税收》" },
                } },
            { "Wealthy_Improving", new[]
                {
                    new BudgetQuote { Text = "他们的傲慢，正是由于他们日益增长的财富。", Author = "修昔底德", Source = "《伯罗奔尼撒战争史》" },
                    new BudgetQuote { Text = "支出总是会上升，直到与收入相匹配。", Author = "诺斯古德·帕金森", Source = "《帕金森定律》" },
                    new BudgetQuote { Text = "极度繁荣不仅会产生财富，还会产生一种万物皆可战胜的幻觉。", Author = "约翰·肯尼思·加尔布雷思", Source = "《富裕社会》" },
                } },
        };
    }
}
