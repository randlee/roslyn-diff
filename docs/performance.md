# Performance Guide

This document describes the performance characteristics of roslyn-diff and provides guidance for working with large files.

## Expected Performance Characteristics

roslyn-diff uses Roslyn for semantic code analysis, which provides accurate structural comparison at the cost of some performance overhead compared to simple text-based diffing.

### Typical Performance

| File Size | Method Count | Expected Time | Memory Usage |
|-----------|-------------|---------------|--------------|
| Small     | < 50        | < 100ms       | < 10 MB      |
| Medium    | 50-200      | 100-500ms     | 10-50 MB     |
| Large     | 200-1000    | 500ms-3s      | 50-150 MB    |
| Very Large| 1000+       | 3-10s         | 150-500 MB   |

*Note: These are approximate values. Actual performance depends on hardware, code complexity, and number of changes.*

### Performance Factors

The following factors affect performance:

1. **File Size**: Larger files with more syntax nodes take longer to parse and compare.
2. **Number of Changes**: More changes require more comparison work.
3. **Code Complexity**: Complex nested structures (deeply nested classes, many overloads) increase matching complexity.
4. **Change Distribution**: Scattered changes across the file may be slower than localized changes.

## Running Benchmarks

The project includes BenchmarkDotNet benchmarks for measuring performance.

### Running All Benchmarks

```bash
cd tests/RoslynDiff.Benchmarks
dotnet run -c Release -- --filter '*'
```

### Running Specific Benchmarks

```bash
# Run only CSharpDiffer benchmarks
dotnet run -c Release -- --filter '*CSharpDiffer*'

# Run only output formatter benchmarks
dotnet run -c Release -- --filter '*OutputFormatter*'

# Run only SyntaxComparer benchmarks
dotnet run -c Release -- --filter '*SyntaxComparer*'
```

### Quick Benchmark Run

For a faster iteration during development:

```bash
dotnet run -c Release -- --filter '*' --job short
```

### Export Results

Export benchmark results to various formats:

```bash
# Export to JSON
dotnet run -c Release -- --filter '*' --exporters json

# Export to Markdown
dotnet run -c Release -- --filter '*' --exporters markdown

# Export to HTML
dotnet run -c Release -- --filter '*' --exporters html
```

## Benchmark Categories

### CSharpDifferBenchmarks

Measures end-to-end diff performance:
- Small file diffs (< 100 lines)
- Medium file diffs (100-1000 lines)
- Large file diffs (1000+ lines)
- Many changes scenarios

### SyntaxComparerBenchmarks

Measures internal comparison engine:
- Syntax tree comparison
- Node extraction performance
- Node matching performance

### OutputFormatterBenchmarks

Measures output formatting:
- JSON formatting
- HTML formatting
- Plain text formatting
- Unified diff formatting
- Memory allocation tracking

## Tips for Large Files

### 1. Use Line Mode for Very Large Files

For files over 5000 lines where semantic diff isn't critical:

```csharp
var options = new DiffOptions { Mode = DiffMode.Line };
```

### 2. Split Large Files

Consider breaking up monolithic files:
- Large files with many classes should be split
- Each file under 1000 methods performs best

### 3. Incremental Comparison

For CI pipelines comparing many files:
- Process files in parallel
- Cache parse results when possible
- Skip unchanged files

### 4. Memory Considerations

For memory-constrained environments:
- Process files sequentially rather than in parallel
- Use streaming output formatters when possible
- Consider the compact output options

## Memory Usage Guidelines

### Typical Memory Patterns

1. **Parsing**: Each syntax tree requires memory proportional to file size
2. **Comparison**: Node matching requires storing both trees simultaneously
3. **Results**: Output size depends on number of changes and format

### Memory Optimization

```csharp
// Use compact options to reduce output memory
var outputOptions = new OutputOptions
{
    Compact = true,
    IncludeContent = false
};
```

### Streaming Large Results

For large results, use async streaming:

```csharp
await using var writer = new StreamWriter(outputPath);
await formatter.FormatResultAsync(result, writer, options);
```

## Benchmark Results (Placeholder)

*Run benchmarks to populate this section with actual results for your hardware.*

### Sample Results

```
BenchmarkDotNet v0.14.0

| Method              | Mean      | Error    | StdDev   | Gen0    | Allocated |
|-------------------- |----------:|---------:|---------:|--------:|----------:|
| SmallFileDiff       |   XX.X ms |  X.XX ms |  X.XX ms |   XXX.X |    XX MB  |
| MediumFileDiff      |  XXX.X ms |  X.XX ms |  X.XX ms |  XXXX.X |    XX MB  |
| LargeFileDiff       | X,XXX.X ms| XX.XX ms | XX.XX ms | XXXXX.X |   XXX MB  |
```

## Performance Testing

The project includes xUnit-based performance tests that verify reasonable performance characteristics:

```bash
# Run performance tests
dotnet test tests/RoslynDiff.Core.Tests --filter "FullyQualifiedName~PerformanceTests"
```

These tests ensure:
- Large file diffs complete within 5 seconds
- Medium file diffs complete within 1 second
- Memory usage stays within reasonable bounds
- No memory leaks with repeated operations

## Profiling

For detailed performance analysis:

### CPU Profiling

```bash
dotnet run -c Release -- --filter '*' --profiler ETW  # Windows
dotnet run -c Release -- --filter '*' --profiler perf # Linux
```

### Memory Profiling

```bash
dotnet run -c Release -- --filter '*' --memoryRandomization
```

### Using dotnet-trace

```bash
dotnet trace collect -- dotnet run -c Release -- --filter '*CSharpDiffer*'
```

## Contributing Performance Improvements

When contributing performance improvements:

1. Run benchmarks before and after your change
2. Include benchmark results in your PR
3. Ensure performance tests pass
4. Document any trade-offs between performance and accuracy
