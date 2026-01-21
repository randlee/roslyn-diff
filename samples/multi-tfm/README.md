# Multi-TFM (Target Framework Moniker) Sample Files

This directory contains sample C# files demonstrating TFM-specific conditional compilation for testing and demonstrating RoslynDiff's multi-TFM analysis capabilities.

## Files

### old-conditional-code.cs
The "before" version of a sample API client that uses conditional compilation directives (`#if NET8_0_OR_GREATER`, `#if NET10_0_OR_GREATER`, etc.) to provide different implementations for different .NET target frameworks.

**Features:**
- Methods that exist in all TFMs
- NET8.0+ specific methods using modern .NET features
- NET10.0+ specific methods using the latest APIs
- Legacy methods for older frameworks
- Properties with TFM-specific implementations

### new-conditional-code.cs
The "after" version with various changes across different TFMs:

**Changes included:**
- **All TFMs:** Modified `GetDataAsync()` to add logging
- **NET8.0+ only:** Enhanced `GetJsonAsync<T>()` with better options, added `HealthCheckAsync()` method
- **NET10.0+ only:** Added `GetJsonWithCancellationAsync<T>()` method, added `EnableMetrics` property
- **NET10.0+ only:** Added entirely new `MetricsCollector` class
- **Configuration changes:** Updated timeout defaults, property modifications
- **All TFMs:** Added `Validate()` method to `AppConfiguration`

## Usage Examples

### Basic Multi-TFM Diff
```bash
roslyn-diff diff old-conditional-code.cs new-conditional-code.cs -t net8.0 -t net10.0 --json
```

### JSON Output with TFM Metadata
```bash
roslyn-diff diff old-conditional-code.cs new-conditional-code.cs \
  -t net8.0 -t net10.0 \
  --json output.json
```

### HTML Report with TFM Badges
```bash
roslyn-diff diff old-conditional-code.cs new-conditional-code.cs \
  -t net8.0 -t net10.0 \
  --html report.html --open
```

### Semicolon-Separated TFMs
```bash
roslyn-diff diff old-conditional-code.cs new-conditional-code.cs \
  -T "net8.0;net10.0" \
  --json
```

## Expected Output

When analyzing these files with multi-TFM support, RoslynDiff will:

1. **Detect TFM-specific changes:**
   - Changes that only apply to NET8.0 (e.g., `HealthCheckAsync()`)
   - Changes that only apply to NET10.0 (e.g., `MetricsCollector` class)
   - Changes that apply to all TFMs (e.g., `GetDataAsync()` logging)

2. **Annotate each change with applicable TFMs:**
   - `ApplicableToTfms: ["net8.0"]` - NET8.0 only changes
   - `ApplicableToTfms: ["net10.0"]` - NET10.0 only changes
   - `ApplicableToTfms: []` - Changes common to all analyzed TFMs

3. **Include metadata in output:**
   - JSON: `metadata.targetFrameworks` array
   - HTML: TFM badges on applicable changes
   - PlainText: TFM annotations in output

## Testing

These files are used by the integration test suite in:
- `tests/RoslynDiff.Integration.Tests/MultiTfmIntegrationTests.cs`

The tests verify:
- Single and multiple TFM analysis
- TFM-specific change detection
- Output format correctness (JSON, HTML, PlainText)
- Error handling and validation
- Preprocessor directive optimization
- Parallel processing performance

## Conditional Compilation Patterns

The samples demonstrate common .NET conditional compilation patterns:

```csharp
// Included only in NET8.0 and later
#if NET8_0_OR_GREATER
    public required string ApiKey { get; set; }
#endif

// Included only in NET10.0 and later
#if NET10_0_OR_GREATER
    public TimeProvider TimeProvider { get; set; }
#endif

// Included in older frameworks (before NET8.0)
#if !NET8_0_OR_GREATER
    public string ApiKey { get; set; } = string.Empty;
#endif
```

## Educational Value

These samples illustrate:
- How multi-targeted libraries evolve across .NET versions
- Framework-specific API usage patterns
- Migration strategies from older to newer .NET versions
- Importance of TFM-aware diff analysis for library maintainers
