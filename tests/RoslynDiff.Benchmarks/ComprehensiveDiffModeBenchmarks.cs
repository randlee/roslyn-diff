namespace RoslynDiff.Benchmarks;

using System.Text;
using BenchmarkDotNet.Attributes;
using RoslynDiff.Core.Differ;
using RoslynDiff.Core.Models;

/// <summary>
/// Comprehensive benchmarks comparing Text Diff vs Semantic (Roslyn) Diff
/// across different file sizes and change densities.
///
/// Four scenarios per file size:
/// 1. Text Diff - Small Changes (minimal lines changed)
/// 2. Text Diff - Large Changes (30% body changed + re-arrangement)
/// 3. Semantic Diff - Small Changes
/// 4. Semantic Diff - Large Changes
/// </summary>
[MemoryDiagnoser]
[SimpleJob(launchCount: 1, warmupCount: 1, iterationCount: 3)]
public class ComprehensiveDiffModeBenchmarks
{
    // Small file (50 lines)
    private string _small50LineOld = "";
    private string _small50LineNewSmallChanges = "";
    private string _small50LineNewLargeChanges = "";

    // Medium file (500 lines)
    private string _medium500LineOld = "";
    private string _medium500LineNewSmallChanges = "";
    private string _medium500LineNewLargeChanges = "";

    // Large file (2000 lines)
    private string _large2000LineOld = "";
    private string _large2000LineNewSmallChanges = "";
    private string _large2000LineNewLargeChanges = "";

    // Extra large file (5000 lines)
    private string _xlarge5000LineOld = "";
    private string _xlarge5000LineNewSmallChanges = "";
    private string _xlarge5000LineNewLargeChanges = "";

    private CSharpDiffer _differ = new();
    private LineDiffer _lineDiffer = new();
    private DiffOptions _options = new();

    [GlobalSetup]
    public void Setup()
    {
        // 50-line file: 5 methods, 5 lines each
        _small50LineOld = GenerateClass(methodCount: 5, linesPerMethod: 5);
        _small50LineNewSmallChanges = ModifyOneLineOnly(_small50LineOld, lineNumber: 12); // 1 line = ~2%
        _small50LineNewLargeChanges = GenerateClassWithRearrangement(methodCount: 5, linesPerMethod: 5, methodsToModify: 2, rearrange: true);

        // 500-line file: 50 methods, 5 lines each
        _medium500LineOld = GenerateClass(methodCount: 50, linesPerMethod: 5);
        _medium500LineNewSmallChanges = ModifyNLines(_medium500LineOld, linesToModify: 10); // 10 lines = ~2%
        _medium500LineNewLargeChanges = GenerateClassWithRearrangement(methodCount: 50, linesPerMethod: 5, methodsToModify: 15, rearrange: true);

        // 2000-line file: 100 methods, 15 lines each
        _large2000LineOld = GenerateClass(methodCount: 100, linesPerMethod: 15);
        _large2000LineNewSmallChanges = ModifyNLines(_large2000LineOld, linesToModify: 10); // 10 lines = ~0.5%
        _large2000LineNewLargeChanges = GenerateClassWithRearrangement(methodCount: 100, linesPerMethod: 15, methodsToModify: 30, rearrange: true);

        // 5000-line file: 250 methods, 15 lines each
        _xlarge5000LineOld = GenerateClass(methodCount: 250, linesPerMethod: 15);
        _xlarge5000LineNewSmallChanges = ModifyNLines(_xlarge5000LineOld, linesToModify: 10); // 10 lines = ~0.2%
        _xlarge5000LineNewLargeChanges = GenerateClassWithRearrangement(methodCount: 250, linesPerMethod: 15, methodsToModify: 75, rearrange: true);

        _options = new DiffOptions();
    }

    // ===== SMALL FILE (50 lines) =====

    [Benchmark(Description = "Text Diff - 50 lines, 1 line changed (~2%)")]
    public DiffResult TextDiff_50_SmallChange()
    {
        return _lineDiffer.Compare(_small50LineOld, _small50LineNewSmallChanges, _options);
    }

    [Benchmark(Description = "Text Diff - 50 lines, 40% body changed + rearranged")]
    public DiffResult TextDiff_50_LargeChange()
    {
        return _lineDiffer.Compare(_small50LineOld, _small50LineNewLargeChanges, _options);
    }

    [Benchmark(Description = "Semantic Diff - 50 lines, 1 line changed (~2%)")]
    public DiffResult SemanticDiff_50_SmallChange()
    {
        return _differ.Compare(_small50LineOld, _small50LineNewSmallChanges, _options);
    }

    [Benchmark(Description = "Semantic Diff - 50 lines, 40% body changed + rearranged")]
    public DiffResult SemanticDiff_50_LargeChange()
    {
        return _differ.Compare(_small50LineOld, _small50LineNewLargeChanges, _options);
    }

    // ===== MEDIUM FILE (500 lines) =====

    [Benchmark(Description = "Text Diff - 500 lines, 10 lines changed (~2%)")]
    public DiffResult TextDiff_500_SmallChange()
    {
        return _lineDiffer.Compare(_medium500LineOld, _medium500LineNewSmallChanges, _options);
    }

    [Benchmark(Description = "Text Diff - 500 lines, 30% body changed + rearranged")]
    public DiffResult TextDiff_500_LargeChange()
    {
        return _lineDiffer.Compare(_medium500LineOld, _medium500LineNewLargeChanges, _options);
    }

    [Benchmark(Description = "Semantic Diff - 500 lines, 10 lines changed (~2%)")]
    public DiffResult SemanticDiff_500_SmallChange()
    {
        return _differ.Compare(_medium500LineOld, _medium500LineNewSmallChanges, _options);
    }

    [Benchmark(Description = "Semantic Diff - 500 lines, 30% body changed + rearranged")]
    public DiffResult SemanticDiff_500_LargeChange()
    {
        return _differ.Compare(_medium500LineOld, _medium500LineNewLargeChanges, _options);
    }

    // ===== LARGE FILE (2000 lines) =====

    [Benchmark(Description = "Text Diff - 2000 lines, 10 lines changed (~0.5%)")]
    public DiffResult TextDiff_2000_SmallChange()
    {
        return _lineDiffer.Compare(_large2000LineOld, _large2000LineNewSmallChanges, _options);
    }

    [Benchmark(Description = "Text Diff - 2000 lines, 30% body changed + rearranged")]
    public DiffResult TextDiff_2000_LargeChange()
    {
        return _lineDiffer.Compare(_large2000LineOld, _large2000LineNewLargeChanges, _options);
    }

    [Benchmark(Description = "Semantic Diff - 2000 lines, 10 lines changed (~0.5%)")]
    public DiffResult SemanticDiff_2000_SmallChange()
    {
        return _differ.Compare(_large2000LineOld, _large2000LineNewSmallChanges, _options);
    }

    [Benchmark(Description = "Semantic Diff - 2000 lines, 30% body changed + rearranged")]
    public DiffResult SemanticDiff_2000_LargeChange()
    {
        return _differ.Compare(_large2000LineOld, _large2000LineNewLargeChanges, _options);
    }

    // ===== EXTRA LARGE FILE (5000 lines) =====

    [Benchmark(Description = "Text Diff - 5000 lines, 10 lines changed (~0.2%)")]
    public DiffResult TextDiff_5000_SmallChange()
    {
        return _lineDiffer.Compare(_xlarge5000LineOld, _xlarge5000LineNewSmallChanges, _options);
    }

    [Benchmark(Description = "Text Diff - 5000 lines, 30% body changed + rearranged")]
    public DiffResult TextDiff_5000_LargeChange()
    {
        return _lineDiffer.Compare(_xlarge5000LineOld, _xlarge5000LineNewLargeChanges, _options);
    }

    [Benchmark(Description = "Semantic Diff - 5000 lines, 10 lines changed (~0.2%)")]
    public DiffResult SemanticDiff_5000_SmallChange()
    {
        return _differ.Compare(_xlarge5000LineOld, _xlarge5000LineNewSmallChanges, _options);
    }

    [Benchmark(Description = "Semantic Diff - 5000 lines, 30% body changed + rearranged")]
    public DiffResult SemanticDiff_5000_LargeChange()
    {
        return _differ.Compare(_xlarge5000LineOld, _xlarge5000LineNewLargeChanges, _options);
    }

    // ===== CODE GENERATION HELPERS =====

    private static string GenerateClass(int methodCount, int linesPerMethod)
    {
        var sb = new StringBuilder();
        sb.AppendLine("namespace BenchmarkTest;");
        sb.AppendLine();
        sb.AppendLine("public class TestClass");
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

    private static void GenerateMethod(StringBuilder sb, int index, int linesPerMethod)
    {
        sb.AppendLine($"    public long Method{index}(long input)");
        sb.AppendLine("    {");
        sb.AppendLine($"        var result = input;");

        for (var j = 0; j < linesPerMethod; j++)
        {
            sb.AppendLine($"        result = result + {j} + (input >> {j % 8});");
        }

        sb.AppendLine($"        return result;");
        sb.AppendLine("    }");
        sb.AppendLine();
    }

    private static string ModifyOneLineOnly(string original, int lineNumber)
    {
        var lines = original.Split('\n');
        if (lineNumber >= lines.Length) return original;

        lines[lineNumber] = lines[lineNumber] + " // MODIFIED";
        return string.Join('\n', lines);
    }

    private static string ModifyNLines(string original, int linesToModify)
    {
        var lines = original.Split('\n').ToList();
        var random = new Random(42); // Deterministic for benchmarking

        for (var i = 0; i < linesToModify && lines.Count > 0; i++)
        {
            var randomLine = random.Next(lines.Count);
            lines[randomLine] = lines[randomLine] + " // MODIFIED";
        }

        return string.Join('\n', lines);
    }

    private static string GenerateClassWithRearrangement(int methodCount, int linesPerMethod, int methodsToModify, bool rearrange)
    {
        var sb = new StringBuilder();
        sb.AppendLine("namespace BenchmarkTest;");
        sb.AppendLine();
        sb.AppendLine("public class TestClass");
        sb.AppendLine("{");
        sb.AppendLine("    private int _counter;");
        sb.AppendLine("    private List<string> _cache = new();");
        sb.AppendLine();

        // Generate with modifications
        var methodIndices = Enumerable.Range(0, methodCount).ToList();

        // Rearrange if requested: move first 30% to end
        if (rearrange)
        {
            var rearrangeCount = (int)(methodCount * 0.3);
            var rearranged = methodIndices.Skip(rearrangeCount).ToList();
            rearranged.AddRange(methodIndices.Take(rearrangeCount));
            methodIndices = rearranged;
        }

        // Generate methods in potentially rearranged order
        for (var i = 0; i < methodCount; i++)
        {
            var actualIndex = methodIndices[i];
            if (i < methodsToModify)
            {
                GenerateModifiedMethod(sb, actualIndex, linesPerMethod);
            }
            else
            {
                GenerateMethod(sb, actualIndex, linesPerMethod);
            }
        }

        sb.AppendLine("}");
        return sb.ToString();
    }

    private static void GenerateModifiedMethod(StringBuilder sb, int index, int linesPerMethod)
    {
        sb.AppendLine($"    public long Method{index}(long input, bool optimized = true)"); // Changed signature
        sb.AppendLine("    {");
        sb.AppendLine($"        var result = input * 2;"); // Changed operation

        for (var j = 0; j < linesPerMethod; j++)
        {
            sb.AppendLine($"        result = result * {j + 2} + (input << {j % 8});"); // Changed operation
        }

        sb.AppendLine($"        return result;"); // Changed logic
        sb.AppendLine("    }");
        sb.AppendLine();
    }
}
