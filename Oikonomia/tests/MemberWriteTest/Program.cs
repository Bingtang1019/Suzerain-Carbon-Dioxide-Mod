

using System;
using Oikonomia;

internal static class Program
{
    private const string Name = "MaxNumberOfEnactedDecrees";
    private static int _fails;
    private static int _total;

    private sealed class FieldOnly
    {
        public int MaxNumberOfEnactedDecrees = 3;
    }

    private sealed class PrivateFieldOnly
    {
        private int MaxNumberOfEnactedDecrees = 3;
        public int ReadBack() { return MaxNumberOfEnactedDecrees; }
    }

    private sealed class PropertyOnly
    {
        public int MaxNumberOfEnactedDecrees { get; set; } = 3;
    }

    private sealed class PrivateSetterProperty
    {
        public int MaxNumberOfEnactedDecrees { get; private set; } = 3;
    }

    private sealed class ReadOnlyProperty
    {
        public int MaxNumberOfEnactedDecrees { get; } = 3;
    }

    private sealed class MissingBoth
    {
        public int Other = 3;
    }

    private class ThrowingPropertyBase
    {
        public int MaxNumberOfEnactedDecrees
        {
            get { return 3; }
            set { throw new InvalidOperationException("setter boom"); }
        }
    }

    private sealed class ThrowingPropertyWithField : ThrowingPropertyBase
    {
        public new int MaxNumberOfEnactedDecrees = 3;
    }

    private class WritablePropertyBase
    {
        public int MaxNumberOfEnactedDecrees { get; set; } = 3;
    }

    private sealed class WritablePropertyWithField : WritablePropertyBase
    {
        public new int MaxNumberOfEnactedDecrees = 3;
    }

    private static void Check(string name, Func<string> run)
    {
        _total++;
        string failure;
        try { failure = run(); }
        catch (Exception e) { failure = "未捕获异常：" + e; }

        bool ok = failure == null;
        if (!ok) _fails++;
        Console.WriteLine((ok ? "  PASS  " : "  FAIL  ") + name.PadRight(38)
            + (ok ? "" : "  " + failure));
    }

    private static string ExpectSuccess(string reason, int actual, int expected)
    {
        if (reason != null) return "写入失败：" + reason;
        if (actual != expected) return "读回=" + actual + "，期望=" + expected;
        return null;
    }

    private static string ExpectFailure(string reason, int actual, int expected, params string[] needles)
    {
        if (reason == null) return "错误地报告写入成功";
        foreach (string needle in needles)
        {
            if (reason.IndexOf(needle, StringComparison.Ordinal) < 0)
                return "失败原因缺少「" + needle + "」：" + reason;
        }
        if (actual != expected) return "失败写入不应改变原值：读回=" + actual + "，期望=" + expected;
        return null;
    }

    private static void Main()
    {
        Console.WriteLine("═══ 属性/字段双探写入：MemberWriteCore 单元测试 ═══");

        Check("① 仅有公开字段 → 写字段", () =>
        {
            var o = new FieldOnly();
            string r = MemberWriteCore.SetPropOrFieldReason(o, Name, 500);
            return ExpectSuccess(r, o.MaxNumberOfEnactedDecrees, 500);
        });

        Check("② 仅有私有字段 → 写字段", () =>
        {
            var o = new PrivateFieldOnly();
            string r = MemberWriteCore.SetPropOrFieldReason(o, Name, 500);
            return ExpectSuccess(r, o.ReadBack(), 500);
        });

        Check("③ 仅有公开属性 → 写属性", () =>
        {
            var o = new PropertyOnly();
            string r = MemberWriteCore.SetPropOrFieldReason(o, Name, 500);
            return ExpectSuccess(r, o.MaxNumberOfEnactedDecrees, 500);
        });

        Check("④ 属性私有 setter → 写属性", () =>
        {
            var o = new PrivateSetterProperty();
            string r = MemberWriteCore.SetPropOrFieldReason(o, Name, 500);
            return ExpectSuccess(r, o.MaxNumberOfEnactedDecrees, 500);
        });

        Check("⑤ 只读属性且无字段 → 双原因失败", () =>
        {
            var o = new ReadOnlyProperty();
            string r = MemberWriteCore.SetPropOrFieldReason(o, Name, 500);
            return ExpectFailure(r, o.MaxNumberOfEnactedDecrees, 3, "属性存在但不可写", "未找到字段");
        });

        Check("⑥ 属性与字段都缺失 → 双原因失败", () =>
        {
            var o = new MissingBoth();
            string r = MemberWriteCore.SetPropOrFieldReason(o, Name, 500);
            return ExpectFailure(r, o.Other, 3, "未找到属性", "未找到字段");
        });

        Check("⑦ 属性 setter 抛错 → 回退写字段", () =>
        {
            var o = new ThrowingPropertyWithField();
            string r = MemberWriteCore.SetPropOrFieldReason(o, Name, 500);
            return ExpectSuccess(r, o.MaxNumberOfEnactedDecrees, 500);
        });

        Check("⑧ 属性可写 → 优先写属性而非字段", () =>
        {
            var o = new WritablePropertyWithField();
            string r = MemberWriteCore.SetPropOrFieldReason(o, Name, 500);
            if (r != null) return "写入失败：" + r;
            int propertyValue = ((WritablePropertyBase)o).MaxNumberOfEnactedDecrees;
            if (propertyValue != 500) return "属性读回=" + propertyValue + "，期望=500";
            if (o.MaxNumberOfEnactedDecrees != 3) return "字段不应被写：读回=" + o.MaxNumberOfEnactedDecrees;
            return null;
        });

        Check("⑨ null 对象 → 明确失败", () =>
        {
            string r = MemberWriteCore.SetPropOrFieldReason(null, Name, 500);
            return r == "对象为空" ? null : "原因=" + (r ?? "(null)");
        });

        Check("⑩ 空成员名 → 明确失败", () =>
        {
            string r = MemberWriteCore.SetPropOrFieldReason(new FieldOnly(), "", 500);
            return r == "成员名为空" ? null : "原因=" + (r ?? "(null)");
        });

        Console.WriteLine("");
        Console.WriteLine("═══ 结果：" + (_fails == 0 ? (_total + "/" + _total + " 全部通过 ✓") : (_fails + " 项失败 ✗（共 " + _total + " 项）")) + " ═══");
        Environment.Exit(_fails == 0 ? 0 : 1);
    }
}
