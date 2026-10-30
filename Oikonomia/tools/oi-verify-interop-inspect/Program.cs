using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Mono.Cecil;

internal static class Program
{
    private static int Main(string[] args)
    {
        string path = @"D:\Program Files (x86)\Steam\steamapps\common\Suzerain\BepInEx\interop\Assembly-CSharp.dll";
        string needle = args.Length > 0 ? args[0] : "ConversationHandler";
        int maxTypes = args.Length > 1 && int.TryParse(args[1], out int n) ? n : 40;
        if (args.Length > 2 && !string.IsNullOrWhiteSpace(args[2])) path = args[2];
        if (Directory.Exists(path))
        {
            foreach (string dll in Directory.GetFiles(path, "*.dll").OrderBy(x => x, StringComparer.Ordinal))
            {
                AssemblyDefinition asm2;
                try { asm2 = AssemblyDefinition.ReadAssembly(dll); } catch { continue; }
                var inner = new List<TypeDefinition>();
                foreach (var module in asm2.Modules) Flatten(module.Types, inner);
                foreach (var type in inner.Where(t =>
                    t.FullName.Equals(needle, StringComparison.OrdinalIgnoreCase)
                    || t.FullName.EndsWith("." + needle, StringComparison.OrdinalIgnoreCase)
                    || t.FullName.EndsWith("/" + needle, StringComparison.OrdinalIgnoreCase)))
                {
                    Console.WriteLine("ASM=" + Path.GetFileName(dll) + " TYPE=" + type.FullName);
                    Console.WriteLine("  base=" + (type.BaseType == null ? "(null)" : type.BaseType.FullName));
                    foreach (var field in type.Fields)
                        Console.WriteLine("  F " + field.Name + " : " + field.FieldType.FullName);
                    foreach (var prop in type.Properties)
                        Console.WriteLine("  P " + prop.Name + " : " + prop.PropertyType.FullName
                            + " get=" + (prop.GetMethod != null) + " set=" + (prop.SetMethod != null));
                    foreach (var method in type.Methods)
                    {
                        string ps = string.Join(", ", method.Parameters.Select(p => p.ParameterType.FullName + " " + p.Name));
                        Console.WriteLine("  M " + method.ReturnType.FullName + " " + method.Name
                            + "(" + ps + ") static=" + method.IsStatic + " public=" + method.IsPublic);
                    }
                }
            }
            return 0;
        }
        var asm = AssemblyDefinition.ReadAssembly(path);
        var types = new List<TypeDefinition>();
        foreach (var module in asm.Modules)
            Flatten(module.Types, types);

        var selected = types
            .Where(t => Matches(t, needle))
            .OrderBy(t => t.FullName, StringComparer.Ordinal)
            .Take(maxTypes)
            .ToList();

        Console.WriteLine("NEEDLE=" + needle + " TYPES=" + selected.Count);
        foreach (var type in selected)
        {
            Console.WriteLine("\nTYPE " + type.FullName);
            Console.WriteLine("  base=" + (type.BaseType == null ? "(null)" : type.BaseType.FullName));
            foreach (var field in type.Fields)
                Console.WriteLine("  F " + field.Name + " : " + field.FieldType.FullName);
            foreach (var prop in type.Properties)
                Console.WriteLine("  P " + prop.Name + " : " + prop.PropertyType.FullName
                    + " get=" + (prop.GetMethod != null) + " set=" + (prop.SetMethod != null));
            foreach (var method in type.Methods)
            {
                string ps = string.Join(", ", method.Parameters.Select(p => p.ParameterType.FullName + " " + p.Name));
                Console.WriteLine("  M " + method.ReturnType.FullName + " " + method.Name
                    + "(" + ps + ") static=" + method.IsStatic + " public=" + method.IsPublic);
            }
        }
        return selected.Count == 0 ? 1 : 0;
    }

    private static bool Matches(TypeDefinition t, string needle)
    {
        if (t.FullName.IndexOf(needle, StringComparison.OrdinalIgnoreCase) >= 0) return true;
        if (t.Methods.Any(m => m.Name.IndexOf(needle, StringComparison.OrdinalIgnoreCase) >= 0)) return true;
        if (t.Properties.Any(p => p.Name.IndexOf(needle, StringComparison.OrdinalIgnoreCase) >= 0)) return true;
        return false;
    }

    private static void Flatten(IEnumerable<TypeDefinition> input, List<TypeDefinition> output)
    {
        foreach (var t in input)
        {
            output.Add(t);
            if (t.HasNestedTypes) Flatten(t.NestedTypes, output);
        }
    }
}
