namespace RoslynDiff.Benchmarks;

using System.Text;
using BenchmarkDotNet.Attributes;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using RoslynDiff.Core.Comparison;
using RoslynDiff.Core.Models;

/// <summary>
/// Extended benchmarks for large-scale file comparison to validate DESIGN-003 performance projections.
/// </summary>
[MemoryDiagnoser]
[SimpleJob(launchCount: 1, warmupCount: 1, iterationCount: 1)]
public class ExtendedScaleBenchmarks
{
    private SyntaxTree _scale3000Old = null!;
    private SyntaxTree _scale3000New = null!;
    private SyntaxTree _scale5000Old = null!;
    private SyntaxTree _scale5000New = null!;
    private SyntaxTree _scale3000Identical = null!;
    private SyntaxTree _scale5000Identical = null!;
    private SyntaxComparer _comparer = new();
    private DiffOptions _options = new();

    [GlobalSetup]
    public void Setup()
    {
        var parseOptions = new CSharpParseOptions(
            languageVersion: LanguageVersion.Latest,
            documentationMode: DocumentationMode.Parse);

        // 3000 line scale: ~1500 methods
        var scale3000OldCode = GenerateClass(methodCount: 150, linesPerMethod: 10);
        var scale3000NewCode = GenerateClassWithChanges(methodCount: 150, linesPerMethod: 10, methodsToModify: 15);
        _scale3000Old = CSharpSyntaxTree.ParseText(scale3000OldCode, parseOptions);
        _scale3000New = CSharpSyntaxTree.ParseText(scale3000NewCode, parseOptions);
        _scale3000Identical = _scale3000Old; // For identical comparison test

        // 5000 line scale: ~2500 methods
        var scale5000OldCode = GenerateClass(methodCount: 250, linesPerMethod: 10);
        var scale5000NewCode = GenerateClassWithChanges(methodCount: 250, linesPerMethod: 10, methodsToModify: 25);
        _scale5000Old = CSharpSyntaxTree.ParseText(scale5000OldCode, parseOptions);
        _scale5000New = CSharpSyntaxTree.ParseText(scale5000NewCode, parseOptions);
        _scale5000Identical = _scale5000Old; // For identical comparison test

        _options = new DiffOptions();
    }

    [Benchmark(Description = "Compare 3000-line trees (~1500 nodes, 10% changed)")]
    public List<Change> Scale3000Compare()
    {
        return _comparer.Compare(_scale3000Old, _scale3000New, _options);
    }

    [Benchmark(Description = "Compare 5000-line trees (~2500 nodes, 10% changed)")]
    public List<Change> Scale5000Compare()
    {
        return _comparer.Compare(_scale5000Old, _scale5000New, _options);
    }

    [Benchmark(Description = "Identical 3000-line trees (early termination)")]
    public List<Change> Scale3000Identical()
    {
        return _comparer.Compare(_scale3000Identical, _scale3000Identical, _options);
    }

    [Benchmark(Description = "Identical 5000-line trees (early termination)")]
    public List<Change> Scale5000Identical()
    {
        return _comparer.Compare(_scale5000Identical, _scale5000Identical, _options);
    }

    /// <summary>
    /// Generates a C# class with many methods to simulate large real-world files.
    /// </summary>
    private static string GenerateClass(int methodCount, int linesPerMethod)
    {
        var sb = new StringBuilder();
        sb.AppendLine("namespace LargeScaleTest;");
        sb.AppendLine();
        sb.AppendLine("/// <summary>");
        sb.AppendLine("/// A large generated class for benchmarking performance at scale.");
        sb.AppendLine("/// </summary>");
        sb.AppendLine("public class LargeGeneratedClass");
        sb.AppendLine("{");
        sb.AppendLine("    private int _counter;");
        sb.AppendLine("    private List<string> _cache = new();");
        sb.AppendLine();

        for (var i = 0; i < methodCount; i++)
        {
            GenerateMethod(sb, i, linesPerMethod);
        }

        sb.AppendLine("}");
        return sb.ToString();
    }

    /// <summary>
    /// Generates a C# class with some methods modified for change detection.
    /// </summary>
    private static string GenerateClassWithChanges(int methodCount, int linesPerMethod, int methodsToModify)
    {
        var sb = new StringBuilder();
        sb.AppendLine("namespace LargeScaleTest;");
        sb.AppendLine();
        sb.AppendLine("/// <summary>");
        sb.AppendLine("/// A large generated class for benchmarking performance at scale.");
        sb.AppendLine("/// </summary>");
        sb.AppendLine("public class LargeGeneratedClass");
        sb.AppendLine("{");
        sb.AppendLine("    private int _counter;");
        sb.AppendLine("    private List<string> _cache = new();");
        sb.AppendLine();

        for (var i = 0; i < methodCount; i++)
        {
            if (i < methodsToModify)
            {
                GenerateModifiedMethod(sb, i, linesPerMethod);
            }
            else
            {
                GenerateMethod(sb, i, linesPerMethod);
            }
        }

        sb.AppendLine("}");
        return sb.ToString();
    }

    private static void GenerateMethod(StringBuilder sb, int index, int linesPerMethod)
    {
        sb.AppendLine($"    /// <summary>Method {index} - performs calculation.</summary>");
        sb.AppendLine($"    public long Method{index}(long input, int iterations = 1)");
        sb.AppendLine("    {");
        sb.AppendLine($"        var result = input;");

        for (var j = 0; j < linesPerMethod; j++)
        {
            sb.AppendLine($"        result = result + {j} * iterations + (input >> {j % 8});");
        }

        sb.AppendLine($"        _counter += (int)result;");
        sb.AppendLine($"        _cache.Add($\"Method{index}:{{result}}\");");
        sb.AppendLine($"        return result;");
        sb.AppendLine("    }");
        sb.AppendLine();
    }

    private static void GenerateModifiedMethod(StringBuilder sb, int index, int linesPerMethod)
    {
        sb.AppendLine($"    /// <summary>Method {index} - OPTIMIZED for performance.</summary>");
        sb.AppendLine($"    public long Method{index}(long input, int iterations = 2)"); // Changed default
        sb.AppendLine("    {");
        sb.AppendLine($"        var result = input * 2;"); // Changed calculation

        for (var j = 0; j < linesPerMethod; j++)
        {
            sb.AppendLine($"        result = result * {j + 2} + (input << {j % 8});"); // Changed operation
        }

        sb.AppendLine($"        _counter = _counter + (int)(result % int.MaxValue);"); // Changed logic
        sb.AppendLine($"        _cache.Add($\"Method{index}_v2:{{result}}\");"); // Changed string
        sb.AppendLine($"        return result;");
        sb.AppendLine("    }");
        sb.AppendLine();
    }
}
