namespace RoslynDiff.Benchmarks;

using System.Text;
using BenchmarkDotNet.Attributes;
using RoslynDiff.Core.Differ;
using RoslynDiff.Core.Models;
using RoslynDiff.Output;

/// <summary>
/// Benchmarks for output formatter performance and memory allocation.
/// </summary>
[MemoryDiagnoser]
[SimpleJob(launchCount: 1, warmupCount: 3, iterationCount: 5)]
public class OutputFormatterBenchmarks
{
    private DiffResult _smallResult = null!;
    private DiffResult _mediumResult = null!;
    private DiffResult _largeResult = null!;
    private JsonFormatter _jsonFormatter = new();
    private HtmlFormatter _htmlFormatter = new();
    private PlainTextFormatter _textFormatter = new();
    private UnifiedFormatter _unifiedFormatter = new();
    private OutputOptions _options = new();
    private OutputOptions _compactOptions = new();

    [GlobalSetup]
    public void Setup()
    {
        var differ = new CSharpDiffer();
        var diffOptions = new DiffOptions();

        // Small result: few changes
        var smallOldCode = GenerateClass(methodCount: 5, linesPerMethod: 3);
        var smallNewCode = GenerateClassWithChanges(methodCount: 5, linesPerMethod: 3, methodsToModify: 2);
        _smallResult = differ.Compare(smallOldCode, smallNewCode, diffOptions);

        // Medium result: moderate changes
        var mediumOldCode = GenerateClass(methodCount: 20, linesPerMethod: 5);
        var mediumNewCode = GenerateClassWithChanges(methodCount: 20, linesPerMethod: 5, methodsToModify: 8, addNewMethods: 3);
        _mediumResult = differ.Compare(mediumOldCode, mediumNewCode, diffOptions);

        // Large result: many changes
        var largeOldCode = GenerateClass(methodCount: 50, linesPerMethod: 8);
        var largeNewCode = GenerateClassWithChanges(methodCount: 50, linesPerMethod: 8, methodsToModify: 25, addNewMethods: 10);
        _largeResult = differ.Compare(largeOldCode, largeNewCode, diffOptions);

        _options = new OutputOptions
        {
            PrettyPrint = true,
            IncludeContent = true,
            IncludeStats = true
        };

        _compactOptions = new OutputOptions
        {
            PrettyPrint = false,
            IncludeContent = false,
            IncludeStats = false,
            Compact = true
        };
    }

    #region JSON Formatter Benchmarks

    [Benchmark(Description = "JSON: Small result (few changes)")]
    public string JsonSmallResult()
    {
        return _jsonFormatter.FormatResult(_smallResult, _options);
    }

    [Benchmark(Description = "JSON: Medium result (moderate changes)")]
    public string JsonMediumResult()
    {
        return _jsonFormatter.FormatResult(_mediumResult, _options);
    }

    [Benchmark(Description = "JSON: Large result (many changes)")]
    public string JsonLargeResult()
    {
        return _jsonFormatter.FormatResult(_largeResult, _options);
    }

    [Benchmark(Description = "JSON: Large result (compact)")]
    public string JsonLargeResultCompact()
    {
        return _jsonFormatter.FormatResult(_largeResult, _compactOptions);
    }

    #endregion

    #region HTML Formatter Benchmarks

    [Benchmark(Description = "HTML: Small result")]
    public string HtmlSmallResult()
    {
        return _htmlFormatter.FormatResult(_smallResult, _options);
    }

    [Benchmark(Description = "HTML: Medium result")]
    public string HtmlMediumResult()
    {
        return _htmlFormatter.FormatResult(_mediumResult, _options);
    }

    [Benchmark(Description = "HTML: Large result")]
    public string HtmlLargeResult()
    {
        return _htmlFormatter.FormatResult(_largeResult, _options);
    }

    #endregion

    #region Plain Text Formatter Benchmarks

    [Benchmark(Description = "Text: Small result")]
    public string TextSmallResult()
    {
        return _textFormatter.FormatResult(_smallResult, _options);
    }

    [Benchmark(Description = "Text: Medium result")]
    public string TextMediumResult()
    {
        return _textFormatter.FormatResult(_mediumResult, _options);
    }

    [Benchmark(Description = "Text: Large result")]
    public string TextLargeResult()
    {
        return _textFormatter.FormatResult(_largeResult, _options);
    }

    #endregion

    #region Unified Formatter Benchmarks

    [Benchmark(Description = "Unified: Small result")]
    public string UnifiedSmallResult()
    {
        return _unifiedFormatter.FormatResult(_smallResult, _options);
    }

    [Benchmark(Description = "Unified: Medium result")]
    public string UnifiedMediumResult()
    {
        return _unifiedFormatter.FormatResult(_mediumResult, _options);
    }

    [Benchmark(Description = "Unified: Large result")]
    public string UnifiedLargeResult()
    {
        return _unifiedFormatter.FormatResult(_largeResult, _options);
    }

    #endregion

    #region Async Benchmarks

    [Benchmark(Description = "JSON Async: Medium result")]
    public async Task<string> JsonAsyncMediumResult()
    {
        using var writer = new StringWriter();
        await _jsonFormatter.FormatResultAsync(_mediumResult, writer, _options);
        return writer.ToString();
    }

    [Benchmark(Description = "HTML Async: Medium result")]
    public async Task<string> HtmlAsyncMediumResult()
    {
        using var writer = new StringWriter();
        await _htmlFormatter.FormatResultAsync(_mediumResult, writer, _options);
        return writer.ToString();
    }

    #endregion

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
    private static string GenerateClassWithChanges(int methodCount, int linesPerMethod, int methodsToModify, int addNewMethods = 0)
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

        for (var i = 0; i < addNewMethods; i++)
        {
            GenerateNewMethod(sb, methodCount + i, linesPerMethod);
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
        sb.AppendLine($"    public int Method{index}(int input, bool flag = true)");
        sb.AppendLine("    {");

        for (var j = 0; j < linesPerMethod; j++)
        {
            sb.AppendLine($"        var local{j} = input * {j + 1};");
        }

        sb.AppendLine($"        return input * 2;");
        sb.AppendLine("    }");
        sb.AppendLine();
    }

    private static void GenerateNewMethod(StringBuilder sb, int index, int linesPerMethod)
    {
        sb.AppendLine($"    public string NewMethod{index}(string prefix)");
        sb.AppendLine("    {");

        for (var j = 0; j < linesPerMethod; j++)
        {
            sb.AppendLine($"        var part{j} = prefix + \"{j}\";");
        }

        sb.AppendLine($"        return prefix;");
        sb.AppendLine("    }");
        sb.AppendLine();
    }
}
