using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SEProfiler.InstrumentationStripper;

namespace SEProfiler.InstrumentationStripper.Cli.Tests;

[TestClass]
public class StripperRewriterTests
{
    [TestMethod]
    public void Removes_Attributes_And_Explicit_Profiler_Calls()
    {
        const string source = """
using SEProfiler;

public class Example
{
    [SEProfiler.Counter("ticks", 1)]
    [SEProfiler.Scope("update")]
    [SEProfiler.Event("phase", "before")]
    public void Tick()
    {
        Profiler.Counter("ticks", 1);
        Profiler.Gauge("players", 12);
        Profiler.Event("phase", "run");
    }
}
""";

        var result = Rewrite(source);

        Assert.IsFalse(result.Contains("[SEProfiler.Counter("));
        Assert.IsFalse(result.Contains("[SEProfiler.Scope("));
        Assert.IsFalse(result.Contains("[SEProfiler.Event("));
        Assert.IsFalse(result.Contains("Profiler.Counter("));
        Assert.IsFalse(result.Contains("Profiler.Gauge("));
        Assert.IsFalse(result.Contains("Profiler.Event("));
        StringAssert.Contains(result, "public void Tick()");
    }

    [TestMethod]
    public void Unwraps_Profiler_Scope_Using_And_UsingVar()
    {
        const string source = """
using SEProfiler;

public class Example
{
    public void Tick()
    {
        using (Profiler.Scope("outer"))
        {
            Work();
        }

        using var _ = Profiler.Scope("inner");
        Work2();
    }

    private void Work() { }
    private void Work2() { }
}
""";

        var result = Rewrite(source);

        Assert.IsFalse(result.Contains("using (Profiler.Scope("));
        Assert.IsFalse(result.Contains("using var _ = Profiler.Scope("));
        StringAssert.Contains(result, "Work();");
        StringAssert.Contains(result, "Work2();");
    }

    private static string Rewrite(string source)
    {
        var root = CSharpSyntaxTree.ParseText(source).GetRoot();
        var rewritten = new ProfilerInstrumentationStripperRewriter().Visit(root);
        Assert.IsNotNull(rewritten);
        return rewritten.NormalizeWhitespace(indentation: "    ", eol: "\n").ToFullString();
    }
}
