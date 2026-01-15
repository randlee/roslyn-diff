namespace RoslynDiff.Benchmarks;

using System.Text;
using BenchmarkDotNet.Attributes;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using RoslynDiff.Core.Comparison;
using RoslynDiff.Core.Models;

/// <summary>
/// Benchmarks for <see cref="SyntaxComparer"/> performance.
/// </summary>
[MemoryDiagnoser]
[SimpleJob(launchCount: 1, warmupCount: 3, iterationCount: 5)]
public class SyntaxComparerBenchmarks
{
    private SyntaxTree _smallOldTree = null!;
    private SyntaxTree _smallNewTree = null!;
    private SyntaxTree _mediumOldTree = null!;
    private SyntaxTree _mediumNewTree = null!;
    private SyntaxTree _largeOldTree = null!;
    private SyntaxTree _largeNewTree = null!;
    private SyntaxComparer _comparer = new();
    private DiffOptions _options = new();

    [GlobalSetup]
    public void Setup()
    {
        var parseOptions = new CSharpParseOptions(
            languageVersion: LanguageVersion.Latest,
            documentationMode: DocumentationMode.Parse);

        // Small tree: ~50 nodes
        var smallOldCode = GenerateClass(methodCount: 5, linesPerMethod: 3);
        var smallNewCode = GenerateClassWithChanges(methodCount: 5, linesPerMethod: 3, methodsToModify: 2);
        _smallOldTree = CSharpSyntaxTree.ParseText(smallOldCode, parseOptions);
        _smallNewTree = CSharpSyntaxTree.ParseText(smallNewCode, parseOptions);

        // Medium tree: ~300 nodes
        var mediumOldCode = GenerateClass(methodCount: 25, linesPerMethod: 6);
        var mediumNewCode = GenerateClassWithChanges(methodCount: 25, linesPerMethod: 6, methodsToModify: 5);
        _mediumOldTree = CSharpSyntaxTree.ParseText(mediumOldCode, parseOptions);
        _mediumNewTree = CSharpSyntaxTree.ParseText(mediumNewCode, parseOptions);

        // Large tree: ~1000 nodes
        var largeOldCode = GenerateClass(methodCount: 80, linesPerMethod: 8);
        var largeNewCode = GenerateClassWithChanges(methodCount: 80, linesPerMethod: 8, methodsToModify: 10);
        _largeOldTree = CSharpSyntaxTree.ParseText(largeOldCode, parseOptions);
        _largeNewTree = CSharpSyntaxTree.ParseText(largeNewCode, parseOptions);

        _options = new DiffOptions();
    }

    [Benchmark(Description = "Compare small trees (~50 nodes)")]
    public List<Change> SmallTreeCompare()
    {
        return _comparer.Compare(_smallOldTree, _smallNewTree, _options);
    }

    [Benchmark(Description = "Compare medium trees (~300 nodes)")]
    public List<Change> MediumTreeCompare()
    {
        return _comparer.Compare(_mediumOldTree, _mediumNewTree, _options);
    }

    [Benchmark(Description = "Compare large trees (~1000 nodes)")]
    public List<Change> LargeTreeCompare()
    {
        return _comparer.Compare(_largeOldTree, _largeNewTree, _options);
    }

    [Benchmark(Description = "Parsing (large file)")]
    public SyntaxTree ParsingLargeFile()
    {
        var code = GenerateClass(methodCount: 80, linesPerMethod: 8);
        return CSharpSyntaxTree.ParseText(code);
    }

    [Benchmark(Description = "Compare + Parse combined (medium)")]
    public List<Change> CompareWithParsing()
    {
        var oldCode = GenerateClass(methodCount: 25, linesPerMethod: 6);
        var newCode = GenerateClassWithChanges(methodCount: 25, linesPerMethod: 6, methodsToModify: 5);
        var oldTree = CSharpSyntaxTree.ParseText(oldCode);
        var newTree = CSharpSyntaxTree.ParseText(newCode);
        return _comparer.Compare(oldTree, newTree, _options);
    }

    [Benchmark(Description = "Identical trees comparison (medium)")]
    public List<Change> IdenticalTreesCompare()
    {
        return _comparer.Compare(_mediumOldTree, _mediumOldTree, _options);
    }

    /// <summary>
    /// Generates a C# class with the specified number of methods and lines per method.
    /// </summary>
    private static string GenerateClass(int methodCount, int linesPerMethod)
    {
        var sb = new StringBuilder();
        sb.AppendLine("namespace BenchmarkTest;");
        sb.AppendLine();
        sb.AppendLine("public class GeneratedClass");
        sb.AppendLine("{");
        sb.AppendLine("    private int _counter;");
        sb.AppendLine();

        for (var i = 0; i < methodCount; i++)
        {
            GenerateMethod(sb, i, linesPerMethod);
        }

        sb.AppendLine("}");
        return sb.ToString();
    }

    /// <summary>
    /// Generates a C# class with some methods modified.
    /// </summary>
    private static string GenerateClassWithChanges(int methodCount, int linesPerMethod, int methodsToModify)
    {
        var sb = new StringBuilder();
        sb.AppendLine("namespace BenchmarkTest;");
        sb.AppendLine();
        sb.AppendLine("public class GeneratedClass");
        sb.AppendLine("{");
        sb.AppendLine("    private int _counter;");
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
        sb.AppendLine($"    public int Method{index}(int input)");
        sb.AppendLine("    {");

        for (var j = 0; j < linesPerMethod; j++)
        {
            sb.AppendLine($"        var local{j} = input + {j};");
        }

        sb.AppendLine($"        return input;");
        sb.AppendLine("    }");
        sb.AppendLine();
    }

    private static void GenerateModifiedMethod(StringBuilder sb, int index, int linesPerMethod)
    {
        sb.AppendLine($"    public int Method{index}(int input, bool flag = true)"); // Changed signature
        sb.AppendLine("    {");

        for (var j = 0; j < linesPerMethod; j++)
        {
            sb.AppendLine($"        var local{j} = input * {j + 1};"); // Changed operation
        }

        sb.AppendLine($"        return input * 2;"); // Changed return
        sb.AppendLine("    }");
        sb.AppendLine();
    }
}
