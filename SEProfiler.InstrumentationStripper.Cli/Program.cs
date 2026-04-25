using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace SEProfiler.InstrumentationStripper;

internal static class Program
{
    public static int Main(string[] args)
    {
        var parseResult = CliOptions.Parse(args);
        if (!parseResult.IsValid)
        {
            Console.Error.WriteLine(parseResult.ErrorMessage);
            CliOptions.PrintUsage();
            return 2;
        }

        if (parseResult.ShowHelp)
        {
            CliOptions.PrintUsage();
            return 0;
        }

        var files = ExpandToCSharpFiles(parseResult.Paths);
        if (files.Count == 0)
        {
            Console.Error.WriteLine("No .cs files found for the provided paths.");
            return 1;
        }

        var rewriter = new ProfilerInstrumentationStripperRewriter();
        int changedCount = 0;

        foreach (var file in files)
        {
            var originalText = File.ReadAllText(file);
            var transformedText = RewriteFile(originalText, rewriter);
            if (string.Equals(originalText, transformedText, StringComparison.Ordinal))
                continue;

            changedCount++;
            if (parseResult.InPlace)
            {
                File.WriteAllText(file, transformedText, Encoding.UTF8);
            }
            else
            {
                Console.WriteLine("// ---- {0} ----", file);
                Console.WriteLine(transformedText);
            }
        }

        if (parseResult.InPlace)
            Console.Error.WriteLine("Updated {0} file(s).", changedCount);
        else
            Console.Error.WriteLine("Preview complete. {0} file(s) would change. Use --inplace to write changes.", changedCount);

        return 0;
    }

    private static string RewriteFile(string source, CSharpSyntaxRewriter rewriter)
    {
        var root = CSharpSyntaxTree.ParseText(source).GetRoot();
        var rewritten = rewriter.Visit(root);
        if (rewritten == null)
            return source;

        var normalized = rewritten.NormalizeWhitespace(indentation: "    ", eol: Environment.NewLine);
        return normalized.ToFullString() + Environment.NewLine;
    }

    private static List<string> ExpandToCSharpFiles(IReadOnlyList<string> paths)
    {
        var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var inputPath in paths)
        {
            var fullPath = Path.GetFullPath(inputPath);
            if (File.Exists(fullPath))
            {
                if (string.Equals(Path.GetExtension(fullPath), ".cs", StringComparison.OrdinalIgnoreCase))
                    result.Add(fullPath);
                continue;
            }

            if (!Directory.Exists(fullPath))
                continue;

            foreach (var file in Directory.EnumerateFiles(fullPath, "*.cs", SearchOption.AllDirectories))
            {
                if (file.IndexOf("\\bin\\", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    file.IndexOf("\\obj\\", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    file.IndexOf("\\.git\\", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    continue;
                }

                result.Add(file);
            }
        }

        return result.OrderBy(path => path, StringComparer.OrdinalIgnoreCase).ToList();
    }
}

internal sealed class CliOptions
{
    public bool IsValid { get; private set; }
    public bool ShowHelp { get; private set; }
    public bool InPlace { get; private set; }
    public string ErrorMessage { get; private set; } = string.Empty;
    public IReadOnlyList<string> Paths { get; private set; } = Array.Empty<string>();

    public static CliOptions Parse(string[] args)
    {
        var options = new CliOptions();
        if (args.Length == 0)
        {
            options.IsValid = true;
            options.Paths = new[] { "." };
            return options;
        }

        var remaining = new List<string>();
        foreach (var arg in args)
        {
            switch (arg)
            {
                case "--help":
                case "-h":
                    options.ShowHelp = true;
                    options.IsValid = true;
                    return options;
                case "--inplace":
                    options.InPlace = true;
                    break;
                case "strip-instrumentation":
                    // Allows calling style: dotnet run -- strip-instrumentation <path>
                    break;
                default:
                    if (arg.StartsWith("-", StringComparison.Ordinal))
                    {
                        options.IsValid = false;
                        options.ErrorMessage = "Unknown option: " + arg;
                        return options;
                    }
                    remaining.Add(arg);
                    break;
            }
        }

        options.Paths = remaining.Count == 0 ? new[] { "." } : remaining;
        options.IsValid = true;
        return options;
    }

    public static void PrintUsage()
    {
        Console.Error.WriteLine("strip-instrumentation [--inplace] [path ...]");
        Console.Error.WriteLine("  Removes SEProfiler instrumentation attributes/calls from C# files.");
        Console.Error.WriteLine("  Default mode prints transformed files to stdout.");
        Console.Error.WriteLine("  Use --inplace to overwrite files.");
    }
}

public sealed class ProfilerInstrumentationStripperRewriter : CSharpSyntaxRewriter
{
    public override SyntaxNode? VisitAttributeList(AttributeListSyntax node)
    {
        var rewritten = (AttributeListSyntax?)base.VisitAttributeList(node);
        if (rewritten == null)
            return null;

        var keptAttributes = rewritten.Attributes.Where(a => !IsProfilerAttribute(a)).ToArray();
        if (keptAttributes.Length == rewritten.Attributes.Count)
            return rewritten;

        if (keptAttributes.Length == 0)
            return null;

        return rewritten.WithAttributes(SyntaxFactory.SeparatedList(keptAttributes));
    }

    public override SyntaxNode? VisitUsingStatement(UsingStatementSyntax node)
    {
        if (IsProfilerScopeUsing(node))
        {
            // Unwrap the scope body and keep traversing it.
            return Visit(node.Statement);
        }

        return base.VisitUsingStatement(node);
    }

    public override SyntaxNode? VisitLocalDeclarationStatement(LocalDeclarationStatementSyntax node)
    {
        if (node.UsingKeyword != default && IsProfilerScopeUsingDeclaration(node.Declaration))
            return null;

        return base.VisitLocalDeclarationStatement(node);
    }

    public override SyntaxNode? VisitExpressionStatement(ExpressionStatementSyntax node)
    {
        if (node.Expression is InvocationExpressionSyntax invocation &&
            IsProfilerInvocation(invocation, "Counter", "Gauge", "Event"))
        {
            return null;
        }

        return base.VisitExpressionStatement(node);
    }

    private static bool IsProfilerAttribute(AttributeSyntax attribute)
    {
        var name = GetRightMostName(attribute.Name);
        return name == "Scope" || name == "ScopeAttribute" ||
               name == "Counter" || name == "CounterAttribute" ||
               name == "Gauge" || name == "GaugeAttribute" ||
               name == "Event" || name == "EventAttribute";
    }

    private static bool IsProfilerScopeUsing(UsingStatementSyntax usingStatement)
    {
        if (usingStatement.Expression is InvocationExpressionSyntax exprInvocation &&
            IsProfilerInvocation(exprInvocation, "Scope"))
        {
            return true;
        }

        if (usingStatement.Declaration != null)
            return IsProfilerScopeUsingDeclaration(usingStatement.Declaration);

        return false;
    }

    private static bool IsProfilerScopeUsingDeclaration(VariableDeclarationSyntax declaration)
    {
        foreach (var variable in declaration.Variables)
        {
            var invocation = variable.Initializer?.Value as InvocationExpressionSyntax;
            if (invocation != null && IsProfilerInvocation(invocation, "Scope"))
                return true;
        }
        return false;
    }

    private static bool IsProfilerInvocation(InvocationExpressionSyntax invocation, params string[] methodNames)
    {
        if (invocation.Expression is MemberAccessExpressionSyntax memberAccess)
        {
            var target = GetRightMostName(memberAccess.Expression);
            var method = memberAccess.Name.Identifier.ValueText;
            return string.Equals(target, "Profiler", StringComparison.Ordinal) &&
                   methodNames.Contains(method, StringComparer.Ordinal);
        }
        return false;
    }

    private static string GetRightMostName(NameSyntax name)
    {
        return name switch
        {
            IdentifierNameSyntax id => id.Identifier.ValueText,
            QualifiedNameSyntax q => GetRightMostName(q.Right),
            AliasQualifiedNameSyntax a => a.Name.Identifier.ValueText,
            _ => string.Empty
        };
    }

    private static string GetRightMostName(ExpressionSyntax expression)
    {
        return expression switch
        {
            IdentifierNameSyntax id => id.Identifier.ValueText,
            MemberAccessExpressionSyntax m => m.Name.Identifier.ValueText,
            QualifiedNameSyntax q => GetRightMostName(q.Right),
            AliasQualifiedNameSyntax a => a.Name.Identifier.ValueText,
            _ => string.Empty
        };
    }
}
