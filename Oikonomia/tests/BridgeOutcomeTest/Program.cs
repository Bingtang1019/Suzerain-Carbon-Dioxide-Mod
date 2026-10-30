using System;
using Oikonomia;   

namespace BridgeOutcomeTest
{
    
    
    
    
    
    
    
    
    
    
    
    internal static class Program
    {
        
        private const string SentinelPath = "SENTINEL_PATH_9f3a\\Oikonomia.override.json";

        
        private const string SentinelError = "SENTINEL_ERROR_7c21";

        private static int _pass, _fail;

        private static int Main()
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            Console.WriteLine("=== F2 文案机械断言（BridgeOutcome）===");
            Console.WriteLine();

            
            Preconditions();

            
            PairwiseDistinct();

            
            FailuresCarryPath();

            
            AppliedHasNoFailureWording();

            
            UnknownFallsBack();

            Console.WriteLine();
            Console.WriteLine("=== 结果：PASS=" + _pass + "  FAIL=" + _fail + " ===");
            return _fail == 0 ? 0 : 1;
        }

        
        private static void Preconditions()
        {
            var all = BridgeOutcome.AllOutcomes();
            Check("前置 四态常量各 1 个（含 applied）", 4, all.Length);
            Check("前置 applied 在四态里", true, Array.IndexOf(all, BridgeOutcome.Applied) >= 0);

            
            Check("前置 applied ⇒ null（成功态归调用方）", null,
                BridgeOutcome.Describe(BridgeOutcome.Applied, SentinelPath, SentinelError));

            
            foreach (var o in new[] { BridgeOutcome.Absent, BridgeOutcome.VersionUnsupported, BridgeOutcome.ReadFailed })
            {
                var s = BridgeOutcome.Describe(o, SentinelPath, SentinelError);
                Check("前置 " + o + " ⇒ 非空", true, !string.IsNullOrEmpty(s));
            }
        }

        
        
        
        private static void PairwiseDistinct()
        {
            
            string absent = BridgeOutcome.Describe(BridgeOutcome.Absent, SentinelPath, SentinelError);
            string ver = BridgeOutcome.Describe(BridgeOutcome.VersionUnsupported, SentinelPath, SentinelError);
            string read = BridgeOutcome.Describe(BridgeOutcome.ReadFailed, SentinelPath, SentinelError);

            Check("① absent ≠ version_unsupported", true, !string.Equals(absent, ver, StringComparison.Ordinal));
            Check("① absent ≠ read_failed", true, !string.Equals(absent, read, StringComparison.Ordinal));
            Check("① version_unsupported ≠ read_failed", true, !string.Equals(ver, read, StringComparison.Ordinal));

            
            Check("① 同输入可复现（absent）", true,
                string.Equals(absent, BridgeOutcome.Describe(BridgeOutcome.Absent, SentinelPath, SentinelError),
                    StringComparison.Ordinal));

            
            Check("① 对照 三态在【同路径同错误】下仍互异", true,
                !string.Equals(absent, ver) && !string.Equals(ver, read) && !string.Equals(absent, read));
        }

        
        
        
        private static void FailuresCarryPath()
        {
            string[] fails = { BridgeOutcome.Absent, BridgeOutcome.VersionUnsupported, BridgeOutcome.ReadFailed };
            foreach (var o in fails)
            {
                var s = BridgeOutcome.Describe(o, SentinelPath, SentinelError) ?? "";
                var shortName = o;
                Check("② " + shortName + " 含 overridePath", true,
                    s.IndexOf(SentinelPath, StringComparison.Ordinal) >= 0);
            }

            
            var rf = BridgeOutcome.Describe(BridgeOutcome.ReadFailed, SentinelPath, SentinelError) ?? "";
            Check("② read_failed 另含 lastError", true, rf.IndexOf(SentinelError, StringComparison.Ordinal) >= 0);

            
            var rfNull = BridgeOutcome.Describe(BridgeOutcome.ReadFailed, SentinelPath, null) ?? "";
            Check("② read_failed 在 lastError=null 时可渲染", true, rfNull.Length > 0);
            Check("② read_failed 在 lastError=null 时不出现裸 null 字样", true,
                rfNull.IndexOf("null", StringComparison.OrdinalIgnoreCase) < 0);
        }

        
        
        
        private static void AppliedHasNoFailureWording()
        {
            
            
            
            string applied = BridgeOutcome.Describe(BridgeOutcome.Applied, SentinelPath, SentinelError) ?? "";

            string[] failureWords = { "不存在", "未发现", "不支持", "读失败", "失败", "已忽略" };
            int hits = 0;
            foreach (var w in failureWords)
                if (applied.IndexOf(w, StringComparison.Ordinal) >= 0) hits++;

            Check("③ applied 渲染结果含【任何】失败字样 = 0（全称型）", 0, hits);

            
            int probe = 0;
            foreach (var w in failureWords)
                if ("override 未发现 文件".IndexOf(w, StringComparison.Ordinal) >= 0) probe++;
            Check("③ 对照 判据能 FAIL（故意像失败的串命中 > 0）", true, probe > 0);
        }

        
        
        
        private static void UnknownFallsBack()
        {
            string s = null;
            bool threw = false;
            try { s = BridgeOutcome.Describe("SOME_UNKNOWN_STATE", SentinelPath, SentinelError); }
            catch { threw = true; }

            Check("④ 未知 outcome 不抛异常", false, threw);
            Check("④ 未知 outcome 落【未发现】那句", true,
                s != null && s.IndexOf("未发现", StringComparison.Ordinal) >= 0);
            Check("④ 未知 outcome 仍带路径", true,
                s != null && s.IndexOf(SentinelPath, StringComparison.Ordinal) >= 0);

            
            string sn = null; bool threwN = false;
            try { sn = BridgeOutcome.Describe(null, SentinelPath, SentinelError); }
            catch { threwN = true; }
            Check("④ null outcome 不抛异常", false, threwN);
            Check("④ null outcome 也落兜底", true, sn != null && sn.IndexOf("未发现", StringComparison.Ordinal) >= 0);
        }

        
        private static void Check(string name, object expected, object actual)
        {
            bool ok = Equals(expected, actual);
            if (ok) { _pass++; Console.WriteLine("  PASS  " + name); }
            else
            {
                _fail++;
                Console.WriteLine("  FAIL  " + name + "   期望=" + (expected ?? "(null)")
                    + "  实际=" + (actual ?? "(null)"));
            }
        }
    }
}
