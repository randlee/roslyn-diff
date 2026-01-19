# Testing Documentation

This document provides comprehensive guidance on testing in the roslyn-diff project, with a focus on the sample data validation framework added in Sprints 1-3.

## Table of Contents

- [Overview](#overview)
- [Test Projects](#test-projects)
- [Validation Testing Strategy](#validation-testing-strategy)
- [Running Tests](#running-tests)
- [Adding New Tests](#adding-new-tests)
- [Interpreting Test Results](#interpreting-test-results)
- [Adding New Sample Files](#adding-new-sample-files)
- [Troubleshooting](#troubleshooting)

---

## Overview

The roslyn-diff project uses xUnit for testing and includes multiple test projects:

- **RoslynDiff.Core.Tests** - Unit tests for core diff engine
- **RoslynDiff.TestUtilities.Tests** - Unit tests for test utility libraries
- **RoslynDiff.Integration.Tests** - Integration tests for CLI and full workflows

### Testing Philosophy

Tests are organized by scope and purpose:
1. **Unit Tests** - Fast, isolated tests of individual components
2. **Integration Tests** - Tests of complete workflows including CLI invocation
3. **Validation Tests** - Cross-format consistency and integrity validation

---

## Test Projects

### RoslynDiff.Core.Tests

Tests the core semantic diffing engine and models.

**Key Test Classes:**
- `DiffEngineTests` - Core diffing logic
- `ChangeDetectionTests` - Change classification
- `LocationTrackingTests` - Line number tracking

**Running:**
```bash
dotnet test tests/RoslynDiff.Core.Tests/
```

### RoslynDiff.TestUtilities.Tests

Tests the validation framework components.

**Key Test Classes:**
- `LineRangeComparerTests` - Line range overlap detection
- `LineNumberValidatorTests` - Line number validation logic
- `JsonOutputParserTests` - JSON parsing
- `HtmlOutputParserTests` - HTML parsing
- `SampleDataValidatorTests` - End-to-end validation

**Running:**
```bash
dotnet test tests/RoslynDiff.TestUtilities.Tests/
```

### RoslynDiff.Integration.Tests

Tests the CLI and full application workflows.

**Key Test Classes:**
- `CSharpDiffIntegrationTests` - C# semantic diff scenarios
- `VisualBasicDiffIntegrationTests` - VB semantic diff scenarios
- `LineDiffIntegrationTests` - Line-by-line diff scenarios
- `OutputFormatIntegrationTests` - Output format generation
- `SampleValidation/*` - Cross-format validation tests

**Running:**
```bash
dotnet test tests/RoslynDiff.Integration.Tests/
```

---

## Validation Testing Strategy

The validation testing framework ensures output consistency and correctness across all formats.

### What Is Validated

#### 1. Line Number Integrity
- No overlapping line ranges within change sections
- No duplicate line numbers
- Sequential ordering where required

#### 2. Format Consistency
- JSON, HTML, and Text outputs contain the same information
- Same change counts across formats
- Same line numbers referenced

#### 3. Output Correctness
- Valid JSON structure
- Valid HTML structure
- Correct change classifications
- Accurate location tracking

### Validation Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                   SampleDataValidator                        │
│                     (Main Entry Point)                       │
└─────────────────────────────────────────────────────────────┘
                             │
        ┌────────────────────┼────────────────────┐
        │                    │                    │
        ▼                    ▼                    ▼
┌──────────────┐    ┌──────────────┐    ┌──────────────┐
│   Parsers    │    │  Validators  │    │ roslyn-diff  │
│              │    │              │    │     CLI      │
├──────────────┤    ├──────────────┤    ├──────────────┤
│ JSON Parser  │    │ JSON Valid.  │    │ JSON Output  │
│ HTML Parser  │    │ HTML Valid.  │    │ HTML Output  │
│ Text Parser  │    │ Line Valid.  │    │ Text Output  │
│ Git Parser   │    │ Git Valid.   │    │ Git Output   │
└──────────────┘    └──────────────┘    └──────────────┘
```

### Validation Workflow

1. **Generate Outputs**: Invoke roslyn-diff CLI to generate all formats
2. **Parse Outputs**: Extract line numbers and ranges from each format
3. **Validate Individually**: Check each format for correctness
4. **Cross-Validate**: Ensure all formats agree on the same information
5. **Report Results**: Return detailed pass/fail results with issues

---

## Running Tests

### Run All Tests

```bash
# From solution root
dotnet test

# With detailed output
dotnet test --logger "console;verbosity=detailed"
```

### Run Specific Test Project

```bash
dotnet test tests/RoslynDiff.Core.Tests/
dotnet test tests/RoslynDiff.TestUtilities.Tests/
dotnet test tests/RoslynDiff.Integration.Tests/
```

### Run Specific Test Class

```bash
dotnet test --filter "FullyQualifiedName~CSharpDiffIntegrationTests"
dotnet test --filter "FullyQualifiedName~SampleDataValidatorTests"
```

### Run Tests by Category

```bash
# Run only unit tests
dotnet test --filter "Category=Unit"

# Run only integration tests
dotnet test --filter "Category=Integration"

# Run only validation tests
dotnet test --filter "Category=SampleValidation"
```

### Run Specific Test Method

```bash
dotnet test --filter "FullyQualifiedName~SampleDataValidatorTests.ValidateAll_WithValidFiles_ReturnsAllPassed"
```

### Run Tests with Coverage

```bash
dotnet test --collect:"XPlat Code Coverage"
```

### Run Sample Validation Tests

Sample validation tests are in `tests/RoslynDiff.Integration.Tests/SampleValidation/`:

```bash
# Run all validation tests
dotnet test tests/RoslynDiff.Integration.Tests/ --filter "FullyQualifiedName~SampleValidation"

# Run specific validation category
dotnet test --filter "FullyQualifiedName~JsonConsistencyTests"
dotnet test --filter "FullyQualifiedName~HtmlConsistencyTests"
dotnet test --filter "FullyQualifiedName~CrossFormatConsistencyTests"
```

---

## Adding New Tests

### Adding a Unit Test

```csharp
using Xunit;
using RoslynDiff.Core;

namespace RoslynDiff.Core.Tests;

public class MyFeatureTests
{
    [Fact]
    public void MyFeature_WithValidInput_ReturnsExpectedResult()
    {
        // Arrange
        var input = "test";

        // Act
        var result = MyFeature.Process(input);

        // Assert
        Assert.Equal("expected", result);
    }

    [Theory]
    [InlineData("input1", "output1")]
    [InlineData("input2", "output2")]
    public void MyFeature_WithVariousInputs_ReturnsCorrectOutput(
        string input, string expected)
    {
        var result = MyFeature.Process(input);
        Assert.Equal(expected, result);
    }
}
```

### Adding an Integration Test

```csharp
using Xunit;
using RoslynDiff.TestUtilities.Validators;

namespace RoslynDiff.Integration.Tests;

public class MyIntegrationTests
{
    [Fact]
    public void TestScenario_WithSampleFiles_ProducesCorrectOutput()
    {
        // Arrange
        var oldFile = "TestFixtures/Sample_Old.cs";
        var newFile = "TestFixtures/Sample_New.cs";

        // Act
        var results = SampleDataValidator.ValidateAll(oldFile, newFile);

        // Assert
        Assert.All(results, r => Assert.True(r.Passed, r.ToString()));
    }
}
```

### Adding a Validation Test

```csharp
using Xunit;
using RoslynDiff.TestUtilities.Validators;
using RoslynDiff.TestUtilities.Models;

namespace RoslynDiff.Integration.Tests.SampleValidation;

public class MyValidationTests
{
    [Fact]
    public void ValidateSpecificScenario()
    {
        // Arrange
        var oldFile = "TestFixtures/Scenario_Old.cs";
        var newFile = "TestFixtures/Scenario_New.cs";
        var options = new SampleDataValidatorOptions
        {
            DiffMode = DiffMode.Roslyn,
            PreserveTempFiles = false
        };

        // Act
        var results = SampleDataValidator.ValidateLineNumberIntegrity(
            oldFile, newFile, options);

        // Assert
        Assert.All(results, r =>
        {
            Assert.True(r.Passed, $"{r.Context}: {r.Message}");
        });
    }
}
```

---

## Interpreting Test Results

### TestResult Model

```csharp
public record TestResult
{
    public bool Passed { get; }
    public string Context { get; }
    public string Message { get; }
    public IReadOnlyList<string> Issues { get; }
}
```

### Example Outputs

#### Passing Test

```
[PASS] JSON Output Line Numbers: No overlapping ranges detected
[PASS] HTML Output Line Numbers: No overlapping ranges detected
[PASS] Cross-Format Consistency: All formats report 5 changes
```

#### Failing Test

```
[FAIL] JSON Output Line Numbers: Found 2 overlapping line range(s)
  Issues:
    - Lines 10-15 (Method1) overlaps with Lines 14-20 (Method2)
    - Lines 25-30 (Property) overlaps with Lines 28-35 (Field)

[FAIL] Cross-Format Consistency: Change counts don't match
  Issues:
    - JSON reports 5 changes
    - HTML reports 6 changes
    - Text reports 5 changes
```

### Test Output Verbosity

Adjust console output verbosity:

```bash
# Minimal output (default)
dotnet test

# Normal output
dotnet test --logger "console;verbosity=normal"

# Detailed output
dotnet test --logger "console;verbosity=detailed"
```

### Understanding Validation Failures

When a validation test fails, the output includes:

1. **Context**: Which aspect failed (e.g., "JSON Output Line Numbers")
2. **Message**: High-level description of the failure
3. **Issues**: Detailed list of specific problems

**Example Analysis:**

```
[FAIL] HTML Output Line Numbers: Found overlapping ranges
  Issues:
    - Lines 10-15 (AddMethod) overlaps with Lines 14-20 (UpdateMethod)
```

This indicates:
- The HTML output has a problem
- Two methods have overlapping line ranges
- Lines 14-15 are duplicated between the two changes
- This could indicate a bug in the diff engine or HTML generator

---

## Adding New Sample Files

Sample files are used for integration and validation testing.

### File Locations

1. **TestFixtures/** - Permanent test cases committed to repository
2. **TempTestCases/** - Temporary ad-hoc testing (gitignored)

### File Naming Convention

Use the pattern: `{TestName}_Old.{ext}` and `{TestName}_New.{ext}`

**Examples:**
```
TestFixtures/CSharp/Calculator_Old.cs
TestFixtures/CSharp/Calculator_New.cs

TestFixtures/VisualBasic/Customer_Old.vb
TestFixtures/VisualBasic/Customer_New.vb

TempTestCases/QuickTest_Old.cs
TempTestCases/QuickTest_New.cs
```

### Adding to TestFixtures (Permanent)

1. Create old/new file pair in appropriate subdirectory:
   ```
   tests/RoslynDiff.Integration.Tests/TestFixtures/CSharp/
   tests/RoslynDiff.Integration.Tests/TestFixtures/VisualBasic/
   ```

2. Follow naming convention

3. Add a test that uses the files:
   ```csharp
   [Fact]
   public void ValidateCalculatorSample()
   {
       var oldFile = "TestFixtures/CSharp/Calculator_Old.cs";
       var newFile = "TestFixtures/CSharp/Calculator_New.cs";

       var results = SampleDataValidator.ValidateAll(oldFile, newFile);

       Assert.All(results, r => Assert.True(r.Passed));
   }
   ```

4. Commit files to repository

### Adding to TempTestCases (Temporary)

1. Place files in `tests/RoslynDiff.Integration.Tests/TempTestCases/`

2. Files are automatically gitignored

3. Test directly or add temporary test method

4. Delete when done (not committed)

See `tests/RoslynDiff.Integration.Tests/TempTestCases/README.md` for detailed guide.

### Sample File Guidelines

**Good Sample Files:**
- Focus on a specific scenario
- Are minimal but complete (no unnecessary code)
- Have clear expected behavior
- Include comments explaining the test case
- Are syntactically valid

**Example:**

```csharp
// Test: Method parameter rename detection
// Expected: One modification (parameter names changed)
public class Calculator
{
    // Old parameters: x, y
    // New parameters: a, b
    public int Add(int a, int b) => a + b;
}
```

---

## Troubleshooting

### Common Issues

#### 1. Tests Fail with "CLI not found"

**Symptom:**
```
[FAIL] Validation Exception: roslyn-diff CLI not found
```

**Solutions:**
- Build the CLI first: `dotnet build src/RoslynDiff.Cli/`
- Ensure CLI is in PATH
- Specify explicit path in test:
  ```csharp
  var options = new SampleDataValidatorOptions
  {
      RoslynDiffCliPath = "/path/to/roslyn-diff"
  };
  ```

#### 2. Tests Timeout

**Symptom:**
```
[FAIL] Validation Exception: Process timed out after 30000ms
```

**Solutions:**
- Increase timeout in options:
  ```csharp
  var options = new SampleDataValidatorOptions
  {
      CliTimeoutMs = 60000  // 60 seconds
  };
  ```
- Use smaller sample files for faster tests

#### 3. Cross-Format Consistency Failures

**Symptom:**
```
[FAIL] Cross-Format Consistency: Line numbers don't match
  - JSON reports lines 10-15
  - HTML reports lines 10-16
```

**Solutions:**
- Check if this is a legitimate bug in format generators
- Use `PreserveTempFiles = true` to inspect actual outputs:
  ```csharp
  var options = new SampleDataValidatorOptions
  {
      PreserveTempFiles = true,
      TempOutputDirectory = "/tmp/roslyn-diff-debug"
  };
  ```
- Manually inspect generated files to identify discrepancy

#### 4. Parsing Failures

**Symptom:**
```
[FAIL] JSON Consistency: Failed to parse JSON output
```

**Solutions:**
- Check CLI is generating valid JSON/HTML
- Inspect CLI output directly: `roslyn-diff file old.cs new.cs --output json`
- Update parsers if output format changed
- Check parser unit tests for failures

#### 5. Line Number Overlap Detected

**Symptom:**
```
[FAIL] Line Number Integrity: Found overlapping ranges
```

**Solutions:**
- This indicates a potential bug in the diff engine
- Isolate the sample files causing the issue
- Copy to TempTestCases for focused debugging
- Check if specific change types cause overlaps
- Review diff engine logic for the scenario

### Debugging Tips

#### 1. Preserve Temp Files

```csharp
var options = new SampleDataValidatorOptions
{
    PreserveTempFiles = true,
    TempOutputDirectory = "/tmp/roslyn-diff-debug"
};
```

This allows you to:
- Inspect generated JSON/HTML/Text outputs
- Manually verify format correctness
- Compare formats side-by-side

#### 2. Run Individual Validations

Instead of `ValidateAll()`, run specific validations:

```csharp
// Just JSON
var jsonResults = SampleDataValidator.ValidateJsonConsistency(oldFile, newFile);

// Just HTML
var htmlResults = SampleDataValidator.ValidateHtmlConsistency(oldFile, newFile);

// Just line numbers
var lineResults = SampleDataValidator.ValidateLineNumberIntegrity(oldFile, newFile);
```

#### 3. Test with CLI Directly

Bypass validation framework to test CLI directly:

```bash
dotnet run --project src/RoslynDiff.Cli/ -- file old.cs new.cs --output json
dotnet run --project src/RoslynDiff.Cli/ -- file old.cs new.cs --output html --out-file report.html
```

#### 4. Check Parser Unit Tests

If parsers fail, check their unit tests:

```bash
dotnet test tests/RoslynDiff.TestUtilities.Tests/ --filter "FullyQualifiedName~ParserTests"
```

#### 5. Use Minimal Reproduction

Create smallest possible sample file that reproduces the issue:

```csharp
// Minimal test case
public class Test
{
    public void Method() { }
}
```

### Getting Help

When reporting test failures, include:
1. Test name and location
2. Full test output (use `--logger "console;verbosity=detailed"`)
3. Sample files that reproduce the issue (if possible)
4. roslyn-diff version
5. Operating system and .NET version

---

## Advanced Testing

### Testing Specific Diff Modes

```csharp
// Force Roslyn semantic diff
var roslynOptions = new SampleDataValidatorOptions
{
    DiffMode = DiffMode.Roslyn
};

// Force line-by-line diff
var lineOptions = new SampleDataValidatorOptions
{
    DiffMode = DiffMode.Line
};

// Test both modes
var bothOptions = new SampleDataValidatorOptions
{
    DiffMode = DiffMode.Both
};
```

### Testing External Tool Compatibility

For line-diff mode, validate compatibility with standard tools:

```csharp
var options = new SampleDataValidatorOptions
{
    DiffMode = DiffMode.Line,
    IncludeExternalTools = true
};

var results = SampleDataValidator.ValidateAll(oldFile, newFile, options);
// This will also compare against 'diff' and 'git diff' outputs
```

### Performance Testing

Add performance benchmarks:

```csharp
[Fact]
public void ValidateLargeFile_PerformanceTest()
{
    var stopwatch = Stopwatch.StartNew();

    var results = SampleDataValidator.ValidateAll(
        "LargeFile_Old.cs",
        "LargeFile_New.cs");

    stopwatch.Stop();

    Assert.True(stopwatch.ElapsedMilliseconds < 5000,
        $"Validation took {stopwatch.ElapsedMilliseconds}ms (expected < 5000ms)");
}
```

---

## CI/CD Integration

### GitHub Actions Example

```yaml
name: Tests

on: [push, pull_request]

jobs:
  test:
    runs-on: ubuntu-latest

    steps:
    - uses: actions/checkout@v3

    - name: Setup .NET
      uses: actions/setup-dotnet@v3
      with:
        dotnet-version: '10.0.x'

    - name: Restore dependencies
      run: dotnet restore

    - name: Build
      run: dotnet build --no-restore

    - name: Run tests
      run: dotnet test --no-build --verbosity normal --logger "trx;LogFileName=test-results.trx"

    - name: Upload test results
      if: always()
      uses: actions/upload-artifact@v3
      with:
        name: test-results
        path: '**/test-results.trx'
```

### Running Validation Tests in CI

```yaml
- name: Run Sample Validation Tests
  run: |
    dotnet test --filter "Category=SampleValidation" \
                --logger "trx;LogFileName=validation-results.trx"

- name: Upload validation results
  if: always()
  uses: actions/upload-artifact@v3
  with:
    name: validation-results
    path: '**/validation-results.trx'
```

---

## Summary

This testing framework provides:
- Comprehensive unit test coverage
- Integration testing of CLI workflows
- Cross-format validation ensuring consistency
- Flexible test organization and discovery
- Detailed failure reporting for debugging
- Support for both permanent and ad-hoc test cases

For more details, see:
- `tests/RoslynDiff.TestUtilities/README.md` - Utility library documentation
- `docs/testing-strategy-sample-validation.md` - Detailed validation strategy
- `tests/RoslynDiff.Integration.Tests/TempTestCases/README.md` - Ad-hoc testing guide
