namespace RoslynDiff.Benchmarks;

using System.Text;
using BenchmarkDotNet.Attributes;
using RoslynDiff.Core.Differ;
using RoslynDiff.Core.Models;

/// <summary>
/// Benchmarks for <see cref="CSharpDiffer"/> performance characteristics.
/// </summary>
[MemoryDiagnoser]
[SimpleJob(launchCount: 1, warmupCount: 3, iterationCount: 5)]
public class CSharpDifferBenchmarks
{
    private string _smallOldCode = "";
    private string _smallNewCode = "";
    private string _mediumOldCode = "";
    private string _mediumNewCode = "";
    private string _largeOldCode = "";
    private string _largeNewCode = "";
    private string _manyChangesOldCode = "";
    private string _manyChangesNewCode = "";
    private CSharpDiffer _differ = new();
    private DiffOptions _options = new();

    [GlobalSetup]
    public void Setup()
    {
        // Small file: ~50 lines, few changes
        _smallOldCode = GenerateClass(methodCount: 3, linesPerMethod: 5);
        _smallNewCode = GenerateClassWithChanges(methodCount: 3, linesPerMethod: 5, methodsToModify: 1);

        // Medium file: ~500 lines, moderate changes
        _mediumOldCode = GenerateClass(methodCount: 30, linesPerMethod: 10);
        _mediumNewCode = GenerateClassWithChanges(methodCount: 30, linesPerMethod: 10, methodsToModify: 5);

        // Large file: ~2000 lines, some changes
        _largeOldCode = GenerateClass(methodCount: 100, linesPerMethod: 15);
        _largeNewCode = GenerateClassWithChanges(methodCount: 100, linesPerMethod: 15, methodsToModify: 10);

        // Many changes scenario: Medium file with many changes
        _manyChangesOldCode = GenerateClass(methodCount: 50, linesPerMethod: 8);
        _manyChangesNewCode = GenerateClassWithChanges(methodCount: 50, linesPerMethod: 8, methodsToModify: 30, addNewMethods: 10);

        _options = new DiffOptions();
    }

    [Benchmark(Description = "Small file (~50 lines, 1 change)")]
    public DiffResult SmallFileDiff()
    {
        return _differ.Compare(_smallOldCode, _smallNewCode, _options);
    }

    [Benchmark(Description = "Medium file (~500 lines, 5 changes)")]
    public DiffResult MediumFileDiff()
    {
        return _differ.Compare(_mediumOldCode, _mediumNewCode, _options);
    }

    [Benchmark(Description = "Large file (~2000 lines, 10 changes)")]
    public DiffResult LargeFileDiff()
    {
        return _differ.Compare(_largeOldCode, _largeNewCode, _options);
    }

    [Benchmark(Description = "Many changes (50 methods, 30 modified + 10 added)")]
    public DiffResult ManyChangesDiff()
    {
        return _differ.Compare(_manyChangesOldCode, _manyChangesNewCode, _options);
    }

    [Benchmark(Description = "Identical files (~500 lines)")]
    public DiffResult IdenticalFilesDiff()
    {
        return _differ.Compare(_mediumOldCode, _mediumOldCode, _options);
    }

    /// <summary>
    /// Generates a C# class with the specified number of methods and lines per method.
    /// </summary>
    private static string GenerateClass(int methodCount, int linesPerMethod)
    {
        var sb = new StringBuilder();
        sb.AppendLine("namespace BenchmarkTest;");
        sb.AppendLine();
        sb.AppendLine("/// <summary>");
        sb.AppendLine("/// A generated class for benchmarking purposes.");
        sb.AppendLine("/// </summary>");
        sb.AppendLine("public class GeneratedClass");
        sb.AppendLine("{");
        sb.AppendLine("    private int _counter;");
        sb.AppendLine("    private string _name = \"Test\";");
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
    private static string GenerateClassWithChanges(int methodCount, int linesPerMethod, int methodsToModify, int addNewMethods = 0)
    {
        var sb = new StringBuilder();
        sb.AppendLine("namespace BenchmarkTest;");
        sb.AppendLine();
        sb.AppendLine("/// <summary>");
        sb.AppendLine("/// A generated class for benchmarking purposes.");
        sb.AppendLine("/// </summary>");
        sb.AppendLine("public class GeneratedClass");
        sb.AppendLine("{");
        sb.AppendLine("    private int _counter;");
        sb.AppendLine("    private string _name = \"Test\";");
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

        // Add new methods
        for (var i = 0; i < addNewMethods; i++)
        {
            GenerateNewMethod(sb, methodCount + i, linesPerMethod);
        }

        sb.AppendLine("}");
        return sb.ToString();
    }

    private static void GenerateMethod(StringBuilder sb, int index, int linesPerMethod)
    {
        sb.AppendLine($"    /// <summary>");
        sb.AppendLine($"    /// Method number {index}.");
        sb.AppendLine($"    /// </summary>");
        sb.AppendLine($"    public int Method{index}(int input)");
        sb.AppendLine("    {");

        for (var j = 0; j < linesPerMethod; j++)
        {
            sb.AppendLine($"        var local{j} = input + {j};");
        }

        sb.AppendLine($"        _counter += input;");
        sb.AppendLine($"        return _counter;");
        sb.AppendLine("    }");
        sb.AppendLine();
    }

    private static void GenerateModifiedMethod(StringBuilder sb, int index, int linesPerMethod)
    {
        sb.AppendLine($"    /// <summary>");
        sb.AppendLine($"    /// Method number {index} - MODIFIED.");
        sb.AppendLine($"    /// </summary>");
        sb.AppendLine($"    public int Method{index}(int input, bool modified = true)"); // Changed signature
        sb.AppendLine("    {");

        for (var j = 0; j < linesPerMethod; j++)
        {
            sb.AppendLine($"        var local{j} = input * {j + 1};"); // Changed operation
        }

        sb.AppendLine($"        _counter = modified ? _counter + input : _counter;"); // Changed logic
        sb.AppendLine($"        return _counter;");
        sb.AppendLine("    }");
        sb.AppendLine();
    }

    private static void GenerateNewMethod(StringBuilder sb, int index, int linesPerMethod)
    {
        sb.AppendLine($"    /// <summary>");
        sb.AppendLine($"    /// New method number {index}.");
        sb.AppendLine($"    /// </summary>");
        sb.AppendLine($"    public string NewMethod{index}(string prefix)");
        sb.AppendLine("    {");

        for (var j = 0; j < linesPerMethod; j++)
        {
            sb.AppendLine($"        var part{j} = prefix + \"{j}\";");
        }

        sb.AppendLine($"        return _name + prefix;");
        sb.AppendLine("    }");
        sb.AppendLine();
    }
}
