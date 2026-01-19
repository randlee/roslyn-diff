# RoslynDiff.TestUtilities

Test utilities library for validating line number integrity in roslyn-diff outputs.

## Overview

This library provides infrastructure for validating that line numbers in diff results do not overlap or contain duplicates, ensuring the correctness of change detection and application.

## Components

### LineRange Record

Represents a range of line numbers within a source file.

```csharp
public record LineRange(int Start, int End, string? Source = null)
{
    public int LineCount { get; }
    public bool ContainsLine(int line);
    public bool OverlapsWith(LineRange other);
}
```

### LineRangeComparer

Static class providing detection methods for line range validation.

**Methods:**
- `DetectOverlaps(IEnumerable<LineRange> ranges)` - Finds all overlapping range pairs
- `DetectDuplicates(IEnumerable<int> lineNumbers)` - Finds duplicate line numbers
- `AreSequential(IEnumerable<LineRange> ranges)` - Checks if ranges are in order

### LineNumberValidator

Static class providing validation methods that return TestResult objects.

**Methods:**
- `ValidateNoOverlaps(IEnumerable<LineRange> ranges, string context)` - Validates no overlaps exist
- `ValidateNoDuplicates(IEnumerable<int> lineNumbers, string context)` - Validates no duplicates exist
- `ValidateRangesSequential(IEnumerable<LineRange> ranges, string context)` - Validates sequential ordering
- `ValidateRanges(IEnumerable<LineRange> ranges, string context, bool requireSequential)` - Comprehensive validation

### TestResult Model

Represents the result of a validation operation.

```csharp
public record TestResult
{
    public bool Passed { get; }
    public string Context { get; }
    public string Message { get; }
    public IReadOnlyList<string> Issues { get; }

    public static TestResult Pass(string context, string? message = null);
    public static TestResult Fail(string context, string message, IEnumerable<string>? issues = null);
}
```

## Usage Examples

### Basic Overlap Detection

```csharp
var ranges = new[]
{
    new LineRange(1, 5, "Method1"),
    new LineRange(3, 7, "Method2")  // Overlaps with Method1
};

var result = LineNumberValidator.ValidateNoOverlaps(ranges, "Old file changes");

if (!result.Passed)
{
    Console.WriteLine(result.ToString());
    // Output:
    // [FAIL] Old file changes: Found 1 overlapping line range(s)
    //   Issues:
    //     - Lines 1-5 (Method1) overlaps with Lines 3-7 (Method2)
}
```

### Validating Diff Results

```csharp
var changes = GetDiffResults(); // Returns IEnumerable<Change>

// Extract line ranges from old locations
var oldRanges = changes
    .Where(c => c.OldLocation != null)
    .Select(c => new LineRange(
        c.OldLocation!.StartLine,
        c.OldLocation.EndLine,
        $"{c.Kind} {c.Name}"))
    .ToList();

// Validate with sequential requirement
var result = LineNumberValidator.ValidateRanges(
    oldRanges,
    "Old file changes",
    requireSequential: true);

Assert.True(result.Passed);
```

### Duplicate Line Number Detection

```csharp
var lineNumbers = new[] { 1, 2, 3, 2, 4 };

var result = LineNumberValidator.ValidateNoDuplicates(
    lineNumbers,
    "Change start lines");

if (!result.Passed)
{
    // result.Issues will contain: ["Line 2 appears multiple times"]
}
```

## Test Coverage

The library includes comprehensive unit tests covering:
- LineRange functionality (construction, containment, overlap detection)
- Overlap detection with various scenarios
- Duplicate detection
- Sequential validation
- Edge cases (empty collections, single items, adjacent ranges)
- Null parameter validation
- Integration examples with RoslynDiff.Core models

**Total Tests:** 93 tests, all passing

## Dependencies

- RoslynDiff.Core - For Change and Location models

## Implementation Details

- **Time Complexity:** Overlap detection is O(n²) for comparing all pairs
- **Adjacent Ranges:** Ranges like [1-5] and [6-10] are NOT considered overlapping
- **Touching Ranges:** Ranges like [1-5] and [5-10] ARE considered overlapping (share line 5)
- **Sequential Order:** Ranges must satisfy `range[i].End < range[i+1].Start` for all consecutive pairs

## Sprint 1, Workstream C Deliverables

This implementation completes all requirements for Sprint 1, Workstream C:

1. ✅ LineRange record with Start, End, and optional Source
2. ✅ DetectOverlaps method with comprehensive overlap detection
3. ✅ DetectDuplicates method with duplicate line number detection
4. ✅ AreSequential helper method for sequential validation
5. ✅ LineNumberValidator with three validation methods returning TestResult objects
6. ✅ TestResult model with Pass/Fail factory methods
7. ✅ 93 comprehensive unit tests covering all scenarios
8. ✅ XML documentation on all public APIs
9. ✅ Integration examples demonstrating real-world usage with diff results

---

## Sprint 2 Components

Sprint 2 added comprehensive parsing and validation infrastructure to support sample data validation across all output formats.

### Parsers (Workstream D)

#### IOutputParser & ILineNumberParser

Base interfaces for all output format parsers:

```csharp
public interface IOutputParser
{
    string FormatName { get; }
    bool CanParse(string content);
}

public interface ILineNumberParser : IOutputParser
{
    IEnumerable<int> ExtractLineNumbers(string content);
    IEnumerable<LineRange> ExtractLineRanges(string content);
}
```

#### JsonOutputParser

Parses JSON output from roslyn-diff and extracts line number information.

**Features:**
- Validates JSON structure
- Extracts line numbers from old/new locations
- Converts locations to LineRange objects
- Handles all change types (added, removed, modified)

**Usage:**
```csharp
var parser = new JsonOutputParser();
var jsonContent = File.ReadAllText("output.json");

if (parser.CanParse(jsonContent))
{
    var lineNumbers = parser.ExtractLineNumbers(jsonContent);
    var ranges = parser.ExtractLineRanges(jsonContent);
}
```

#### HtmlOutputParser

Parses HTML output and extracts line numbers from diff sections.

**Features:**
- Parses HTML structure using regular expressions
- Extracts line numbers from diff panels
- Identifies change types by CSS classes
- Handles side-by-side diff format

**Usage:**
```csharp
var parser = new HtmlOutputParser();
var htmlContent = File.ReadAllText("report.html");

var ranges = parser.ExtractLineRanges(htmlContent);
// Returns ranges from added, removed, and modified sections
```

#### TextOutputParser

Parses plain text output format.

**Features:**
- Extracts line numbers from text-based diff display
- Parses location headers (e.g., "Lines 10-20")
- Handles both Roslyn and line-diff text formats

**Usage:**
```csharp
var parser = new TextOutputParser();
var textContent = File.ReadAllText("output.txt");

var lineNumbers = parser.ExtractLineNumbers(textContent);
```

#### UnifiedDiffParser

Parses unified diff (git-style) output format.

**Features:**
- Parses hunk headers (@@ -a,b +c,d @@)
- Extracts line numbers for added/removed lines
- Validates unified diff format
- Compatible with standard diff tools

**Usage:**
```csharp
var parser = new UnifiedDiffParser();
var diffContent = File.ReadAllText("output.diff");

var hunkHeaders = parser.ExtractHunkHeaders(diffContent);
var ranges = parser.ExtractLineRanges(diffContent);
```

#### TimestampNormalizer

Utility for normalizing timestamps in output comparisons.

**Features:**
- Removes or normalizes timestamp variations
- Supports multiple timestamp formats
- Enables deterministic output comparison

### Validators (Workstream E)

#### JsonValidator

Validates JSON output for correctness and consistency.

**Methods:**
- `ValidateLineNumberIntegrity(content)` - Ensures no overlapping or duplicate line numbers

**Usage:**
```csharp
var jsonContent = File.ReadAllText("output.json");
var result = JsonValidator.ValidateLineNumberIntegrity(jsonContent);

if (!result.Passed)
{
    Console.WriteLine($"JSON validation failed: {result.Message}");
}
```

#### HtmlValidator

Validates HTML output structure and line numbers.

**Methods:**
- `ValidateAll(content)` - Comprehensive HTML validation

**Checks:**
- Valid HTML structure
- No duplicate line numbers within sections
- No overlapping ranges
- Proper section markers

**Usage:**
```csharp
var htmlContent = File.ReadAllText("report.html");
var results = HtmlValidator.ValidateAll(htmlContent);

foreach (var result in results)
{
    Console.WriteLine(result);
}
```

#### TextValidator

Validates plain text output format.

**Methods:**
- `ValidateLineNumberIntegrity(content)` - Validates text format line numbers

#### GitDiffValidator

Validates unified diff format output.

**Methods:**
- `ValidateUnifiedDiffFormat(content)` - Ensures valid unified diff structure
- `ValidateHunkHeaders(content)` - Validates @@ hunk header syntax

**Usage:**
```csharp
var diffContent = File.ReadAllText("output.diff");
var result = GitDiffValidator.ValidateUnifiedDiffFormat(diffContent);
```

#### SampleDataValidator (Workstream F)

Main orchestration class that coordinates all validations.

**Core Methods:**

##### ValidateAll(oldFile, newFile, options)
Runs all validation methods and returns aggregated results.

```csharp
var results = SampleDataValidator.ValidateAll("old.cs", "new.cs");

foreach (var result in results)
{
    Console.WriteLine(result);
}
```

##### ValidateLineNumberIntegrity(oldFile, newFile, options)
Validates line numbers across all output formats.

##### ValidateJsonConsistency(oldFile, newFile, options)
Validates JSON output for consistency and correctness.

##### ValidateHtmlConsistency(oldFile, newFile, options)
Validates HTML output for consistency and correctness.

##### ValidateCrossFormatConsistency(oldFile, newFile, options)
Validates that all formats report the same information.

**Usage Example:**
```csharp
using RoslynDiff.TestUtilities.Models;
using RoslynDiff.TestUtilities.Validators;

// Validate all aspects
var results = SampleDataValidator.ValidateAll("old.cs", "new.cs");

// Or run individual validations
var options = new SampleDataValidatorOptions
{
    DiffMode = DiffMode.Roslyn,
    PreserveTempFiles = true
};

var lineResults = SampleDataValidator.ValidateLineNumberIntegrity(
    "old.cs", "new.cs", options);
var crossFormatResults = SampleDataValidator.ValidateCrossFormatConsistency(
    "old.cs", "new.cs", options);
```

### Models

#### SampleDataValidatorOptions

Configuration options for validation behavior.

**Properties:**
- `DiffMode` - Auto, Roslyn, Line, or Both
- `IgnoreTimestamps` - Whether to ignore timestamp differences
- `IncludeExternalTools` - Whether to include external tool validation
- `TempOutputDirectory` - Directory for temporary output files
- `PreserveTempFiles` - Keep temp files for debugging
- `CliTimeoutMs` - Timeout for CLI invocations (default 30000ms)
- `RoslynDiffCliPath` - Custom path to roslyn-diff CLI

**Usage:**
```csharp
var options = new SampleDataValidatorOptions
{
    DiffMode = DiffMode.Roslyn,
    PreserveTempFiles = true,
    CliTimeoutMs = 60000,
    RoslynDiffCliPath = "/custom/path/to/roslyn-diff"
};
```

---

## Sprint 3 Components

Sprint 3 focused on integration testing and real-world validation.

### Test Execution

The TestUtilities library is designed to be used in xUnit integration tests. Tests are located in:
- `tests/RoslynDiff.Integration.Tests/SampleValidation/` - Main validation tests
- `tests/RoslynDiff.TestUtilities.Tests/` - Unit tests for utilities

**Running Tests:**

```bash
# Run all tests in solution
dotnet test

# Run only TestUtilities unit tests
dotnet test tests/RoslynDiff.TestUtilities.Tests/

# Run only integration/validation tests
dotnet test tests/RoslynDiff.Integration.Tests/

# Run with detailed output
dotnet test --logger "console;verbosity=detailed"

# Run specific test class
dotnet test --filter "FullyQualifiedName~SampleDataValidatorTests"
```

### Adding New Sample Files

To add new sample files for validation:

1. Place old/new file pairs in `tests/RoslynDiff.Integration.Tests/TestFixtures/`
2. Or use `TempTestCases/` folder for temporary ad-hoc testing
3. Files are auto-discovered by naming convention: `{Name}_Old.{ext}` and `{Name}_New.{ext}`
4. Tests automatically validate all discovered file pairs

### Interpreting Test Results

Test results follow the `TestResult` model:

```csharp
public record TestResult
{
    public bool Passed { get; }
    public string Context { get; }
    public string Message { get; }
    public IReadOnlyList<string> Issues { get; }
}
```

**Example Output:**
```
[PASS] JSON Output Line Numbers: No overlapping ranges detected
[FAIL] HTML Output Line Numbers: Found 2 overlapping line range(s)
  Issues:
    - Lines 10-15 (Method1) overlaps with Lines 14-20 (Method2)
    - Lines 25-30 (Property) overlaps with Lines 28-35 (Field)
```

### Troubleshooting

#### Tests Fail with "CLI not found"

**Problem:** SampleDataValidator cannot locate the roslyn-diff CLI executable.

**Solutions:**
1. Build the CLI project first: `dotnet build src/RoslynDiff.Cli/`
2. Ensure the CLI is in your PATH
3. Specify explicit path in options:
   ```csharp
   var options = new SampleDataValidatorOptions
   {
       RoslynDiffCliPath = "/path/to/roslyn-diff"
   };
   ```

#### Tests Timeout

**Problem:** CLI invocations exceed timeout limit.

**Solution:** Increase timeout in options:
```csharp
var options = new SampleDataValidatorOptions
{
    CliTimeoutMs = 60000  // 60 seconds
};
```

#### Temp Files Not Cleaned Up

**Problem:** Temporary output files accumulate in temp directory.

**Solution:**
- Tests automatically clean up unless `PreserveTempFiles = true`
- Manually clean temp directory if needed
- Check that tests complete successfully (cleanup in finally block)

#### Validation Fails on Timestamps

**Problem:** Cross-format validation fails due to timestamp differences.

**Solution:** Enable timestamp normalization:
```csharp
var options = new SampleDataValidatorOptions
{
    IgnoreTimestamps = true
};
```

#### HTML Parser Fails

**Problem:** HTML structure changes cause parser to fail.

**Solutions:**
1. Check HTML format is valid: `HtmlValidator.ValidateAll(content)`
2. Update parser regex patterns if HTML structure changed
3. Use `PreserveTempFiles = true` to inspect actual HTML output

#### JSON Parser Fails

**Problem:** JSON structure is invalid or unexpected.

**Solutions:**
1. Validate JSON is well-formed: `JsonValidator.ValidateLineNumberIntegrity(content)`
2. Check roslyn-diff CLI is generating correct JSON
3. Inspect actual JSON with `PreserveTempFiles = true`

### Integration with CI/CD

The validation tests can be integrated into CI pipelines:

```yaml
# Example GitHub Actions step
- name: Run Sample Validation Tests
  run: |
    dotnet build
    dotnet test --filter "Category=SampleValidation" --logger "trx;LogFileName=validation-results.trx"

- name: Upload Test Results
  if: always()
  uses: actions/upload-artifact@v3
  with:
    name: validation-test-results
    path: '**/validation-results.trx'
```

### Performance Considerations

Validation tests invoke the CLI multiple times per test case:
- JSON generation
- HTML generation
- Text generation
- Unified diff generation (for line mode)

**Tips for Faster Tests:**
- Use smaller sample files for unit tests
- Run validations in parallel where possible
- Cache CLI outputs when running multiple validations
- Use `DiffMode` to skip unnecessary format generations

### Best Practices

1. **Test Organization:** Group related tests in the same class
2. **Naming Conventions:** Use descriptive test names indicating what is validated
3. **Isolation:** Each test should work with its own file pairs
4. **Cleanup:** Always use try-finally for temp file cleanup
5. **Options:** Use consistent options across related tests
6. **Assertions:** Provide clear failure messages with context
7. **Documentation:** Document expected behavior and known limitations

---

## Architecture

For detailed architecture documentation, see:
- `ARCHITECTURE.md` - Component diagrams and data flow
- `IMPLEMENTATION_SUMMARY.md` - Sprint 2 implementation details
- `docs/testing-strategy-sample-validation.md` - Overall testing strategy

## Total Test Coverage

- **Sprint 1:** 93 unit tests for line number validation
- **Sprint 2:** 45+ unit tests for parsers and validators
- **Sprint 3:** Integration tests for real-world sample validation
- **Total:** 140+ tests across all components
