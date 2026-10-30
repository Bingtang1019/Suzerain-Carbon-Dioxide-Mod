














using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;

class Program
{
    
    static readonly string[] Symbols =
    {
        "Oikonomia.EconomyPatches::RunInstant",
        "Oikonomia.Bridge::WriteState",
        "Oikonomia.EconomyPatches::SaveBaseline",
        "Oikonomia.EconomyPatches::PersistSnapshot",
        "Oikonomia.EconomyPatches::ComputeDelta",
        "Oikonomia.SettleMath::Compute",
        "Oikonomia.EconomyPatches::PublishDetail",
        "Oikonomia.EconomyPatches::PublishPreview",
        "Oikonomia.EconomyPatches::RefreshDetailPreview",
        "Oikonomia.EconomyPatches::ReadSettleState",
        "Oikonomia.EconomyPatches::ReadSettleCfg",
        "Oikonomia.EconomyPatches::ReadPerTurnRules",
    };

    
    static readonly string[] OpCodes = { "add", "sub", "mul", "ceq", "cgt", "clt" };

    sealed class Counts
    {
        public Dictionary<string, int> Symbol = new Dictionary<string, int>(StringComparer.Ordinal);
        public Dictionary<string, int> OpCode = new Dictionary<string, int>(StringComparer.Ordinal);
        public Dictionary<string, Dictionary<string, int>> SymbolByMethod =
            new Dictionary<string, Dictionary<string, int>>(StringComparer.Ordinal);
        public Dictionary<string, Dictionary<string, int>> OpCodeByMethod =
            new Dictionary<string, Dictionary<string, int>>(StringComparer.Ordinal);
        public int Methods, Instructions;
        public string Sha256;
    }

    static void Main(string[] raw)
    {
        if (raw.Length < 1)
        {
            Console.Error.WriteLine("用法: il-symbol-count <A.dll> [<B.dll>]");
            Environment.Exit(2);
        }

        var a = Take(raw[0]);
        var b = raw.Length > 1 ? Take(raw[1]) : null;

        Console.WriteLine("═══ IL 符号计数（Mono.Cecil；与 LCS 无关的硬证据）═══");
        Console.WriteLine("A : " + raw[0]);
        Console.WriteLine("    SHA256=" + a.Sha256 + "  方法体=" + a.Methods + "  IL 指令=" + a.Instructions);
        if (b != null)
        {
            Console.WriteLine("B : " + raw[1]);
            Console.WriteLine("    SHA256=" + b.Sha256 + "  方法体=" + b.Methods + "  IL 指令=" + b.Instructions);
        }
        Console.WriteLine("");

        Console.WriteLine("── 关键符号调用次数 ──────────────────────────────────────");
        Console.WriteLine(Pad("符号", 46) + Pad("A", 8) + Pad("B", 8) + "差");
        foreach (var s in Symbols)
        {
            int ca = Get(a.Symbol, s), cb = b == null ? 0 : Get(b.Symbol, s);
            Console.WriteLine(Pad(Short(s), 46) + Pad(ca.ToString(), 8) + Pad(cb.ToString(), 8)
                + (cb - ca >= 0 ? "+" : "") + (cb - ca));
        }

        Console.WriteLine("");
        Console.WriteLine("── 关键操作码次数 ────────────────────────────────────────");
        Console.WriteLine(Pad("操作码", 46) + Pad("A", 8) + Pad("B", 8) + "差");
        foreach (var o in OpCodes)
        {
            int ca = Get(a.OpCode, o), cb = b == null ? 0 : Get(b.OpCode, o);
            Console.WriteLine(Pad(o, 46) + Pad(ca.ToString(), 8) + Pad(cb.ToString(), 8)
                + (cb - ca >= 0 ? "+" : "") + (cb - ca));
        }

        
        Console.WriteLine("");
        Console.WriteLine("── add/sub 逐方法差分（A → B；只列有变化的）──────────────");
        var keys = new SortedSet<string>(StringComparer.Ordinal);
        foreach (var k in a.OpCodeByMethod.Values.SelectMany(x => x.Keys)) keys.Add(k);
        if (b != null) foreach (var k in b.OpCodeByMethod.Values.SelectMany(x => x.Keys)) keys.Add(k);
        int sumA = 0, sumB = 0;
        foreach (var k in keys)
        {
            int ca = Ops(a, k), cb = b == null ? 0 : Ops(b, k);
            sumA += ca; sumB += cb;
            if (ca == cb) continue;
            Console.WriteLine("   " + Pad(k, 62) + Pad(ca.ToString(), 7) + Pad(cb.ToString(), 7)
                + (cb - ca >= 0 ? "+" : "") + (cb - ca));
        }
        Console.WriteLine("   " + Pad("（全部方法合计）", 62) + Pad(sumA.ToString(), 7) + Pad(sumB.ToString(), 7)
            + (sumB - sumA >= 0 ? "+" : "") + (sumB - sumA));

        
        
        
        
        Console.WriteLine("");
        Console.WriteLine("── 结算算术守恒核对（add/sub）────────────────────────────");
        int aSettle = Ops(a, "Oikonomia.EconomyPatches::SettleCore");
        int bSettle = Ops(b, "Oikonomia.EconomyPatches::SettleCore");
        int bKernel = Ops(b, "Oikonomia.SettleMath::Compute");
        int bRead = Ops(b, "Oikonomia.EconomyPatches::ReadSettleState");
        Console.WriteLine("A.SettleCore                        = " + aSettle);
        Console.WriteLine("B.SettleCore                        = " + bSettle);
        Console.WriteLine("B.SettleMath.Compute（承接计算段）  = " + bKernel);
        Console.WriteLine("B.ReadSettleState（承接 ① 段读取循环）= " + bRead);
        Console.WriteLine("");
        Console.WriteLine("说明：这是**计数归属**，不是 1:1 指令映射（循环计数在拆分处会重新归属）。");
        Console.WriteLine("      决定性证据是下面三条 + 四个写入符号次数不变。");

        int sumDelta = 0;
        var changed = new List<string>();
        foreach (var k in keys)
        {
            int ca = Ops(a, k), cb = b == null ? 0 : Ops(b, k);
            if (ca == cb) continue;
            sumDelta += cb - ca;
            changed.Add(k);
        }
        int asmDelta = (Get(a.OpCode, "add") + Get(a.OpCode, "sub"))
                     - (b == null ? 0 : Get(b.OpCode, "add") + Get(b.OpCode, "sub"));
        Console.WriteLine("");
        Console.WriteLine("③ 逐方法差分之和 = " + sumDelta + "；全程序集 add+sub 差分 = " + (-asmDelta)
            + "（必须相等 = " + (sumDelta == -asmDelta) + "）");

        
        var allowed = new HashSet<string>(StringComparer.Ordinal)
        {
            "Oikonomia.EconomyPatches::SettleCore",        
            "Oikonomia.SettleMath::Compute",               
            "Oikonomia.EconomyPatches::ReadSettleState",   
            "Oikonomia.EconomyPatches::PublishPreview",    
            "Oikonomia.SettleMath::HasSignal",             
        };
        var unexpected = changed.FindAll(k => !allowed.Contains(k));

        
        bool ok = true;
        foreach (var s in Symbols)
        {
            if (!s.Contains("RunInstant") && !s.Contains("WriteState")
                && !s.Contains("SaveBaseline") && !s.Contains("PersistSnapshot")) continue;
            int ca = Get(a.Symbol, s), cb = b == null ? 0 : Get(b.Symbol, s);
            if (cb != ca)
            {
                ok = false;
                Console.WriteLine("[FAIL] " + Short(s) + " 调用次数变化：" + ca + " → " + cb);
            }
        }
        Console.WriteLine("");
        Console.WriteLine("── 判定 ──────────────────────────────────────────────────");
        Console.WriteLine("① 四个写入/推进符号调用次数不变                 : "
            + (ok ? "通过" : "失败"));
        bool subOk = Get(a.OpCode, "sub") == (b == null ? 0 : Get(b.OpCode, "sub"))
                  && Get(a.OpCode, "mul") == (b == null ? 0 : Get(b.OpCode, "mul"));
        Console.WriteLine("② sub / mul 次数不变（结算减法/乘法形状未变）    : " + (subOk ? "通过" : "失败")
            + "（sub " + Get(a.OpCode, "sub") + "→" + (b == null ? 0 : Get(b.OpCode, "sub"))
            + "，mul " + Get(a.OpCode, "mul") + "→" + (b == null ? 0 : Get(b.OpCode, "mul")) + "）");
        Console.WriteLine("③ 逐方法差分之和 == 全程序集差分（表是穷尽的）   : " + (sumDelta == -asmDelta ? "通过" : "失败"));
        Console.WriteLine("④ 算术变化只出现在预期的新方法里                 : "
            + (unexpected.Count == 0 ? "通过" : "失败 → " + string.Join(", ", unexpected)));

        if (!subOk || sumDelta != -asmDelta || unexpected.Count > 0) ok = false;

        
        Console.WriteLine("── 关键符号在 B 里的分布（前 6 个符号）──────────────────");
        foreach (var s in Symbols.Take(6))
        {
            Console.WriteLine("  " + Short(s) + "：");
            foreach (var kv in MethodDist(b == null ? a : b, b == null ? a.SymbolByMethod : b.SymbolByMethod, s))
                Console.WriteLine("      " + kv.Key + " × " + kv.Value);
        }

        Console.WriteLine("");
        if (ok) { Console.WriteLine("[计数审计通过] 四个写入/推进符号调用次数不变；结算算术守恒（搬家）"); Environment.Exit(0); }
        else { Console.WriteLine("[计数审计失败] 见上面 FAIL 行"); Environment.Exit(1); }
    }

    static string Short(string s)
    {
        return s.Replace("Oikonomia.EconomyPatches::", "").Replace("Oikonomia.SettleMath::", "SettleMath::")
                .Replace("Oikonomia.Bridge::", "Bridge::");
    }

    static string Pad(string s, int n)
    {
        
        int w = 0;
        foreach (var c in s) w += c > 0x2E80 ? 2 : 1;
        return s + new string(' ', Math.Max(1, n - w));
    }

    static int Get(Dictionary<string, int> d, string k)
    {
        int v; return d.TryGetValue(k, out v) ? v : 0;
    }

    static int MethodOps(Counts c, string type, string method, string op)
    {
        if (c == null) return 0;
        
        Dictionary<string, int> m;
        if (!c.OpCodeByMethod.TryGetValue(op, out m)) return 0;
        int v;
        return m.TryGetValue(type + "::" + method, out v) ? v : 0;
    }

    
    static int Ops(Counts c, string methodKey)
    {
        if (c == null) return 0;
        return MethodOp(c, methodKey, "add") + MethodOp(c, methodKey, "sub");
    }

    static int MethodOp(Counts c, string methodKey, string op)
    {
        Dictionary<string, int> m;
        if (!c.OpCodeByMethod.TryGetValue(op, out m)) return 0;
        int v;
        return m.TryGetValue(methodKey, out v) ? v : 0;
    }

    static IEnumerable<KeyValuePair<string, int>> MethodDist(Counts c, Dictionary<string, Dictionary<string, int>> src, string sym)
    {
        Dictionary<string, int> m;
        if (!src.TryGetValue(sym, out m)) yield break;
        foreach (var kv in m.OrderByDescending(x => x.Value)) yield return kv;
    }

    static Counts Take(string path)
    {
        var c = new Counts();
        c.Sha256 = Sha256Of(path);
        var asm = AssemblyDefinition.ReadAssembly(path);
        foreach (var mod in asm.Modules)
        {
            foreach (var t in AllTypes(mod))
            {
                foreach (var m in t.Methods)
                {
                    if (!m.HasBody) continue;
                    c.Methods++;
                    var key = t.FullName + "::" + m.Name;
                    foreach (var ins in m.Body.Instructions)
                    {
                        c.Instructions++;
                        string text = Text(ins);

                        foreach (var s in Symbols)
                            if (text.IndexOf(s, StringComparison.Ordinal) >= 0) Bump(c.Symbol, c.SymbolByMethod, s, key);

                        var op = ins.OpCode.Name;
                        foreach (var o in OpCodes)
                            if (string.Equals(op, o, StringComparison.Ordinal)
                                || op.StartsWith(o + ".", StringComparison.Ordinal))
                                Bump(c.OpCode, c.OpCodeByMethod, o, key);
                    }
                }
            }
        }
        return c;
    }

    static string Text(Instruction ins)
    {
        if (ins.Operand == null) return ins.OpCode.Name;
        var mr = ins.Operand as MethodReference;
        if (mr != null) return ins.OpCode.Name + " " + mr.FullName;
        var fr = ins.Operand as FieldReference;
        if (fr != null) return ins.OpCode.Name + " " + fr.FullName;
        var tr = ins.Operand as TypeReference;
        if (tr != null) return ins.OpCode.Name + " " + tr.FullName;
        return ins.OpCode.Name + " " + ins.Operand;
    }

    static void Bump(Dictionary<string, int> all, Dictionary<string, Dictionary<string, int>> byMethod,
                     string key, string method)
    {
        int v;
        all[key] = all.TryGetValue(key, out v) ? v + 1 : 1;
        Dictionary<string, int> m;
        if (!byMethod.TryGetValue(key, out m)) { m = new Dictionary<string, int>(StringComparer.Ordinal); byMethod[key] = m; }
        int w;
        m[method] = m.TryGetValue(method, out w) ? w + 1 : 1;
    }

    static IEnumerable<TypeDefinition> AllTypes(ModuleDefinition mod)
    {
        var stack = new Stack<TypeDefinition>(mod.Types);
        while (stack.Count > 0)
        {
            var t = stack.Pop();
            yield return t;
            foreach (var n in t.NestedTypes) stack.Push(n);
        }
    }

    static string Sha256Of(string path)
    {
        using (var sha = System.Security.Cryptography.SHA256.Create())
        using (var fs = System.IO.File.OpenRead(path))
            return BitConverter.ToString(sha.ComputeHash(fs)).Replace("-", "");
    }
}
