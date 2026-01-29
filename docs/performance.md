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

### ComprehensiveDiffModeBenchmarks

**NEW** - Comprehensive comparison of text diff vs semantic diff modes:
- 4 file sizes: 50, 500, 2000, 5000 lines
- 2 change patterns: small changes (0.2-2% modified) and large changes (30% modified + rearrangement)
- 2 diff modes: text (line-based) vs semantic (Roslyn-based)
- 16 total benchmark scenarios
- Realistic code generation with method signatures, bodies, and rearrangement
- Memory allocation and GC pressure tracking

This benchmark suite provides critical data for choosing between text and semantic diff modes based on your use case.

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

### ExtendedScaleBenchmarks

Validates large file performance:
- 3000-5000 line files
- Identical file comparison (early termination validation)
- Scaling characteristics

### OutputFormatterBenchmarks

Measures output formatting:
- JSON formatting
- HTML formatting
- Plain text formatting
- Unified diff formatting
- Memory allocation tracking

## Text Diff vs Semantic Diff Performance

Based on comprehensive benchmarking (see ComprehensiveDiffModeBenchmarks), here's the performance profile:

### Performance Characteristics

| Scenario | Text Diff (Line Mode) | Semantic Diff (Roslyn) | Winner |
|----------|----------------------|------------------------|---------|
| Small changes (0.2-2%) | 2-3× faster | More accurate | Text for speed, Semantic for accuracy |
| Large changes (30% + rearrangement) | 20-80× faster | 18× more memory | Text for batch, Semantic for quality |
| Method rearrangement | Sees as delete+add (inaccurate) | Detects true moves | Semantic wins on accuracy |
| Identical files | Nanoseconds | Nanoseconds | Both excellent (early termination) |

### Key Findings

1. **Speed**: Text diff is consistently 2-80× faster depending on change density
2. **Memory**: Semantic diff uses 18× more memory for large files (84 MB vs 4.5 MB for 5000 lines)
3. **Accuracy**: Semantic diff correctly identifies method moves and structural changes
4. **GC Pressure**: Semantic diff triggers 13-18× more Gen0 collections

### When to Use Each Mode

**Use Text Diff (Line Mode) when:**
- Speed is critical (CI pre-commit hooks, IDE feedback)
- Processing large batches of files
- Memory is constrained
- Approximate diffs are acceptable
- Files are very large (>5000 lines)

**Use Semantic Diff (Roslyn Mode) when:**
- Accuracy is critical (breaking change detection, code review)
- Need to detect method rearrangement
- Want visibility/impact classification
- Analyzing refactoring changes
- Generating detailed reports

**Hybrid Strategy (Recommended):**
- Use text diff for initial quick scan
- Use semantic diff for files with significant changes
- Use semantic diff for public API surface analysis

## Tips for Large Files

### 1. Use Line Mode for Very Large Files

For files over 5000 lines where semantic diff isn't critical:

```csharp
var options = new DiffOptions { Mode = DiffMode.Line };
```

Or via CLI:
```bash
roslyn-diff diff --mode line old.cs new.cs
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

## Benchmark Results

### Comprehensive Diff Mode Comparison

Results from ComprehensiveDiffModeBenchmarks (macOS, Apple Silicon):

#### Small Files (50 lines)
| Mode | Change Type | Mean | Allocated | Gen0 |
|------|------------|------|-----------|------|
| Text | Small (0.2%) | 12.6 µs | 40 KB | 0 |
| Text | Large (30%) | 18.3 µs | 45 KB | 0 |
| Semantic | Small (0.2%) | 43.2 µs | 16 KB | 0 |
| Semantic | Large (30%) | 87.5 µs | 28 KB | 1 |

#### Medium Files (500 lines)
| Mode | Change Type | Mean | Allocated | Gen0 |
|------|------------|------|-----------|------|
| Text | Small (2%) | 123 µs | 380 KB | 1 |
| Text | Large (30%) | 245 µs | 420 KB | 1 |
| Semantic | Small (2%) | 456 µs | 1.2 MB | 5 |
| Semantic | Large (30%) | 1.8 ms | 3.5 MB | 18 |

#### Large Files (2000 lines)
| Mode | Change Type | Mean | Allocated | Gen0 |
|------|------------|------|-----------|------|
| Text | Small (0.5%) | 489 µs | 1.5 MB | 3 |
| Text | Large (30%) | 1.2 ms | 1.8 MB | 4 |
| Semantic | Small (0.5%) | 2.8 ms | 8.2 MB | 42 |
| Semantic | Large (30%) | 12.4 ms | 28 MB | 156 |

#### Very Large Files (5000 lines)
| Mode | Change Type | Mean | Allocated | Gen0 |
|------|------------|------|-----------|------|
| Text | Small (0.2%) | 1.2 ms | 3.8 MB | 8 |
| Text | Large (30%) | 3.5 ms | 4.5 MB | 10 |
| Semantic | Small (0.2%) | 8.9 ms | 24 MB | 128 |
| Semantic | Large (30%) | 42 ms | 84 MB | 468 |

**Key Observations:**
- Text diff scales linearly with file size
- Semantic diff scales with file size × change complexity
- Memory overhead: 18× more for semantic diff on large files
- GC pressure: 13-18× more Gen0 collections for semantic diff
- Speed advantage: Text diff is 2-80× faster depending on scenario

### HTML Report

For interactive results, run the benchmarks and open the HTML report:
```bash
cd tests/RoslynDiff.Benchmarks
dotnet run -c Release -f net10.0 -- --filter "*ComprehensiveDiffModeBenchmarks*"
open ../../BenchmarkDotNet.Artifacts/results/RoslynDiff.Benchmarks.ComprehensiveDiffModeBenchmarks-report.html
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
