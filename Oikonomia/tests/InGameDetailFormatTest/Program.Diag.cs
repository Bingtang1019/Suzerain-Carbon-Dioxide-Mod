using System;
using System.Collections.Generic;
using Oikonomia;

















partial class Program
{
    static void DiagSuite()
    {
        Console.WriteLine("");
            Console.WriteLine("【⑯ F3 模组自检行：BuildDiagLine（★ 阴性对照可离线断言）】");

            
            
            
            var green = new List<string>();
            Same("⑯-1a 全绿（total=5, ok=5, 无问题项）⇒ 空串", InGameDetailFormat.BuildDiagLine(5, 5, green), "");

            
            
            var greenButBroken = InGameDetailFormat.BuildDiagLine(5, 4, new List<string>());
            True("⑯-1b 对照：ok<total ⇒ 判据【不】返回空串（证明 ⑯-1a 不是恒空）",
                greenButBroken.Length > 0, "实际长度=" + greenButBroken.Length);

            
            
            
            Same("⑯-2a total=0（未上报）⇒ 固定串「未上报」，不再空串",
                 InGameDetailFormat.BuildDiagLine(0, 0, null), InGameDetailFormat.NoDiagReportedLine);
            Same("⑯-2b total=0 且 ok=0、名字非空 ⇒ 仍为同一固定串（该支不看 names）",
                 InGameDetailFormat.BuildDiagLine(0, 0, new List<string> { "X" }), InGameDetailFormat.NoDiagReportedLine);
            Same("⑯-2c 该串逐字 == 模组自检：本会话未上报任何诊断点（Lead 批准口径，改字即判红）",
                 InGameDetailFormat.NoDiagReportedLine, "模组自检：本会话未上报任何诊断点");
            True("⑯-2d 该串不得含「未生效」（防把『没上报』说成『没生效』）",
                 InGameDetailFormat.NoDiagReportedLine.IndexOf("未生效", StringComparison.Ordinal) < 0, "");
            True("⑯-2e 该串不得含「未加载」（不得宣称覆盖『插件未加载』）",
                 InGameDetailFormat.NoDiagReportedLine.IndexOf("未加载", StringComparison.Ordinal) < 0, "");
            string whyMarker;
            True("⑯-2f 该串不含可见标记字符（沿用 2.7.5 去标记约束）",
                 !InGameDetailFormat.ContainsVisibleMarker(InGameDetailFormat.NoDiagReportedLine, out whyMarker),
                 "标记=" + (whyMarker ?? "无"));
            Same("⑯-2g 阴性对照：全绿（total=5, ok=5）仍为空串 ⇒ 两支确实不同（本段不是恒出串）",
                 InGameDetailFormat.BuildDiagLine(5, 5, null), "");

            
            var two = new List<string> { "WarFlowManager.PlayerTurn", "e3: WarToken.Bomb" };
            string line = InGameDetailFormat.BuildDiagLine(5, 3, two);
            Has("⑯-3a 不全绿 ⇒ 含 " + "3" + "/" + "5", line, "3/5");
            Has("⑯-3b 含「挂载成功」", line, "挂载成功");
            Has("⑯-3c 含第一个未挂者名字", line, "WarFlowManager.PlayerTurn");
            Has("⑯-3d 含第二个未挂者名字", line, "e3: WarToken.Bomb");

            
            string line2 = InGameDetailFormat.BuildDiagLine(9, 7, two);
            Has("⑯-3e 数字随参数变化（9/7 而非 5/3）", line2, "7/9");
            True("⑯-3f 两组参数产出的行【不同】（证明数字真的来自参数）",
                !string.Equals(line, line2, StringComparison.Ordinal), "");

            
            var five = new List<string> { "A1", "A2", "A3", "A4", "A5" };
            string capped = InGameDetailFormat.BuildDiagLine(9, 4, five);
            Has("⑯-4a 显示前三个：含 A1", capped, "A1");
            Has("⑯-4b 含 A2", capped, "A2");
            Has("⑯-4c 含 A3", capped, "A3");
            NotHas("⑯-4d 【不】出现第四个名字 A4（截断生效）", capped, "A4");
            NotHas("⑯-4e 【不】出现第五个名字 A5", capped, "A5");
            Has("⑯-4f 报出剩余项数「等 2 项」", capped, "等 2 项");

            
            var exactly = new List<string> { "B1", "B2", "B3" };
            string ex = InGameDetailFormat.BuildDiagLine(9, 6, exactly);
            NotHas("⑯-4g 恰好 3 个（= MaxDiagNames）⇒ 无「等 N 项」", ex, "等 ");

            
            string withNull = null;
            bool threw = false;
            try { withNull = InGameDetailFormat.BuildDiagLine(4, 3, null); }
            catch { threw = true; }
            True("⑯-5a 名字列表为 null ⇒ 不抛异常", !threw, "");
            Has("⑯-5b 名字为 null 时仍报 N/M", withNull ?? "", "3/4");

            string withEmpty = InGameDetailFormat.BuildDiagLine(4, 2, new List<string> { "", null, "C3" });
            Has("⑯-5c 空名/null 被跳过、不阻断后续名字", withEmpty ?? "", "C3");

            
            
            string why;
            True("⑯-5d 产出行零可见标记（ContainsVisibleMarker = false）",
                !InGameDetailFormat.ContainsVisibleMarker(line, out why), why ?? "");
            True("⑯-5e 截断行同样零可见标记",
                !InGameDetailFormat.ContainsVisibleMarker(capped, out why), why ?? "");

            
            SameInt("⑯-6a MaxDiagNames = 3", InGameDetailFormat.MaxDiagNames, 3);

            
            
            
            
            string over = null; bool overThrew = false;
            try { over = InGameDetailFormat.BuildDiagLine(3, 5, new List<string> { "Z" }); }
            catch { overThrew = true; }
            True("⑯-7a ok > total（畸形）⇒ 不抛异常", !overThrew, "");
            Same("⑯-7b ok > total（畸形）⇒ 空串（不产出「5/3 挂载成功」这类自相矛盾的话）", over ?? "", "");

            
            string neg = null; bool negThrew = false;
            try { neg = InGameDetailFormat.BuildDiagLine(-1, -1, new List<string> { "Z" }); }
            catch { negThrew = true; }
            True("⑯-7c 负数输入 ⇒ 不抛异常", !negThrew, "");
            Same("⑯-7d 负数 total ⇒ 空串", neg ?? "", "");

            
            
            
            
            
            
            var withBlanks = new List<string> { "", null, "R1", "R2", "R3", "R4" };
            string blanks = InGameDetailFormat.BuildDiagLine(9, 4, withBlanks);
            Has("⑯-8a 空名/null 被跳过 ⇒ 仍能取到 R1", blanks ?? "", "R1");
            Has("⑯-8b 仍能取到 R2", blanks ?? "", "R2");
            Has("⑯-8c 仍能取到 R3（共取满 MaxDiagNames 个【真名】）", blanks ?? "", "R3");
            NotHas("⑯-8d R4 被截断（名额已被三个真名占满）", blanks ?? "", "R4");
            
            Has("⑯-8e 余数按【真名】算 = 「等 1 项」（空名不占名额、也不算作剩余）", blanks ?? "", "等 1 项");
            
            NotHas("⑯-8f 余数【不是】把空名也算进去的「等 3 项」", blanks ?? "", "等 3 项");
    }
}
