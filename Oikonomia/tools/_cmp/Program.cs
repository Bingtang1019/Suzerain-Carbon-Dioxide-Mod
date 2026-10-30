using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;



class Cmp
{
    static void Main(string[] a)
    {
        var b = Dump(a[0]);
        var c = Dump(a[1]);

        Console.WriteLine("=== 方法 IL 比较 ===");
        Console.WriteLine("基线方法数={0}  当前方法数={1}", b.Count, c.Count);
        var onlyB = b.Keys.Except(c.Keys).ToList();
        var onlyC = c.Keys.Except(b.Keys).ToList();
        Console.WriteLine("仅基线有：{0}", onlyB.Count == 0 ? "（无）" : string.Join(", ", onlyB));
        Console.WriteLine("仅当前有：{0}", onlyC.Count == 0 ? "（无）" : string.Join(", ", onlyC));

        int diff = 0, same = 0;
        foreach (var k in b.Keys.Intersect(c.Keys).OrderBy(x => x))
        {
            if (b[k] == c[k]) { same++; }
            else { diff++; Console.WriteLine("  ★ IL 不同：{0}  基线 {1} 字节 / 当前 {2} 字节", k, b[k].Length, c[k].Length); }
        }
        Console.WriteLine("IL 相同的方法 = {0}，不同的方法 = {1}", same, diff);

        Console.WriteLine();
        Console.WriteLine("=== 字段比较 ===");
        var fb = DumpFields(a[0]);
        var fc = DumpFields(a[1]);
        Console.WriteLine("基线字段数={0}  当前字段数={1}", fb.Count, fc.Count);
        foreach (var k in fb.Keys.Union(fc.Keys).OrderBy(x => x))
        {
            string vb = fb.TryGetValue(k, out var x) ? x : "（缺）";
            string vc = fc.TryGetValue(k, out var y) ? y : "（缺）";
            if (vb != vc) Console.WriteLine("  ★ 字段不同：{0}  基线={1}  当前={2}", k, vb, vc);
        }
        Console.WriteLine("（未列出的字段完全一致）");

        Console.WriteLine();
        Console.WriteLine(diff == 0 && onlyB.Count == 0 && onlyC.Count == 0
            ? "★★★ 结论：所有方法 IL 字节完全一致 → 拆分零语义变化 ★★★"
            : "!!! 存在 IL 差异，需要人工核对 !!!");
    }

    static Dictionary<string, byte[]> Dump(string path)
    {
        var d = new Dictionary<string, byte[]>();
        using var fs = File.OpenRead(path);
        using var pe = new PEReader(fs);
        var md = pe.GetMetadataReader();
        foreach (var th in md.TypeDefinitions)
        {
            var t = md.GetTypeDefinition(th);
            if (md.GetString(t.Name) != "EconomyPatches") continue;
            foreach (var mh in t.GetMethods())
            {
                var m = md.GetMethodDefinition(mh);
                int rva = m.RelativeVirtualAddress;
                d[md.GetString(m.Name)] = rva == 0 ? Array.Empty<byte>() : pe.GetMethodBody(rva).GetILContent().ToArray();
            }
        }
        return d;
    }

    static Dictionary<string, string> DumpFields(string path)
    {
        var d = new Dictionary<string, string>();
        using var fs = File.OpenRead(path);
        using var pe = new PEReader(fs);
        var md = pe.GetMetadataReader();
        foreach (var th in md.TypeDefinitions)
        {
            var t = md.GetTypeDefinition(th);
            if (md.GetString(t.Name) != "EconomyPatches") continue;
            foreach (var fh in t.GetFields())
            {
                var f = md.GetFieldDefinition(fh);
                d[md.GetString(f.Name)] = f.Attributes.ToString();
            }
        }
        return d;
    }
}
