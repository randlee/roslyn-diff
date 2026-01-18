# Sample Validation Tests

## Purpose

This folder contains integration tests that validate sample data consistency across different output formats in RoslynDiff. These tests ensure that JSON, HTML, Text, and Git-style outputs maintain semantic accuracy, structural integrity, and cross-format consistency.

## Sprint 3 Summary

Sprint 3 completed the validation testing framework with comprehensive test coverage across all output formats and validation scenarios.

### Test Classes and Purpose

#### 1. SampleValidationTestBase
**Purpose:** Base class providing common infrastructure for validation tests

**Features:**
- Sample file discovery and loading
- Test result assertion helpers
- Common setup and teardown
- Shared configuration options

**Usage:**
```csharp
public class MyValidationTests : SampleValidationTestBase
{
    [Fact]
    public void MyTest()
    {
        var (oldFile, newFile, name) = GetSampleFilePairs().First();
        var results = SampleDataValidator.ValidateAll(oldFile, newFile);
        AssertAllPassed(results);
    }
}
```

#### 2. JsonConsistencyTests
**Purpose:** Validates JSON output consistency and correctness

**Test Coverage:**
- JSON-001: Flag combination consistency
  - Tests that JSON output is identical across different CLI flag combinations
  - Validates `--json`, `--json --quiet`, and file output modes
- JSON-002: Line number integrity
  - Ensures no overlapping line ranges
  - Validates no duplicate line numbers
  - Checks sequential ordering where appropriate

**Key Tests:**
- `Json001_FlagCombinationConsistency_JsonVsJsonQuiet()` - Flag variations
- `Json002_LineNumberIntegrity_NoOverlaps()` - Overlap detection
- `Json002_LineNumberIntegrity_NoDuplicates()` - Duplicate detection

**Test Category:** `[Trait("Category", "SampleValidation")]`

#### 3. HtmlConsistencyTests
**Purpose:** Validates HTML output structure and line numbers

**Test Coverage:**
- HTML-001: Flag combination consistency
  - Tests HTML output across different CLI flags
  - Validates file output and co-generation modes
- HTML-002: Section line number integrity
  - Ensures no duplicate line numbers within sections
  - Validates no overlapping ranges in added/removed/modified panels
- HTML-003: Data attribute consistency
  - Validates `data-old-content` and `data-new-content` attributes
  - Ensures attributes match visual display

**Key Tests:**
- `Html001_FlagCombinationConsistency_HtmlToFile()` - Flag variations
- `Html002_SectionLineNumberIntegrity()` - Section validation
- `Html003_DataAttributeConsistency()` - Attribute validation

**Test Category:** `[Trait("Category", "SampleValidation")]`

#### 4. LineNumberIntegrityTests
**Purpose:** Validates line number integrity across all formats

**Test Coverage:**
- Line number overlap detection across all output formats
- Duplicate line number detection
- Sequential ordering validation
- Cross-file consistency checks

**Key Tests:**
- `LineNumberIntegrity_JsonFormat_NoOverlaps()` - JSON validation
- `LineNumberIntegrity_HtmlFormat_NoOverlaps()` - HTML validation
- `LineNumberIntegrity_TextFormat_NoOverlaps()` - Text validation
- `LineNumberIntegrity_AllFormats_ConsistentLineNumbers()` - Cross-format check

**Test Category:** `[Trait("Category", "SampleValidation")]`

#### 5. CrossFormatConsistencyTests
**Purpose:** Validates consistency across all output formats

**Test Coverage:**
- XFMT-001: JSON vs HTML line numbers
  - Ensures JSON and HTML report identical line numbers
- XFMT-002: JSON vs Text line numbers
  - Ensures JSON and Text report identical line numbers
- XFMT-003: All formats agreement (Roslyn mode)
  - Validates JSON, HTML, Text all report same information
  - Checks change counts, line numbers, and symbol names
- XFMT-004: All formats agreement (Line mode)
  - Validates all formats including Git-style unified diff
  - Ensures line-by-line mode consistency

**Key Tests:**
- `Xfmt001_JsonVsHtmlLineNumbers()` - JSON/HTML comparison
- `Xfmt002_JsonVsTextLineNumbers()` - JSON/Text comparison
- `Xfmt003_AllFormatsAgreement_RoslynMode()` - Multi-format Roslyn
- `Xfmt004_AllFormatsAgreement_LineMode()` - Multi-format Line mode

**Test Category:** `[Trait("Category", "SampleValidation")]`

---

## Test Categories and Traits

All validation tests use the trait `[Trait("Category", "SampleValidation")]` to enable filtering:

```bash
# Run all sample validation tests
dotnet test --filter "Category=SampleValidation"

# Run specific test class
dotnet test --filter "FullyQualifiedName~JsonConsistencyTests"

# Run specific test method
dotnet test --filter "FullyQualifiedName~Json001_FlagCombinationConsistency"
```

---

## Running Specific Test Groups

### Run All Validation Tests

```bash
cd /Users/randlee/Documents/github/roslyn-diff-worktrees/feature/sample-data-validation-tests

dotnet test tests/RoslynDiff.Integration.Tests/ --filter "Category=SampleValidation"
```

### Run JSON Consistency Tests Only

```bash
dotnet test --filter "FullyQualifiedName~JsonConsistencyTests"
```

### Run HTML Consistency Tests Only

```bash
dotnet test --filter "FullyQualifiedName~HtmlConsistencyTests"
```

### Run Line Number Integrity Tests Only

```bash
dotnet test --filter "FullyQualifiedName~LineNumberIntegrityTests"
```

### Run Cross-Format Consistency Tests Only

```bash
dotnet test --filter "FullyQualifiedName~CrossFormatConsistencyTests"
```

### Run All Tests with Detailed Output

```bash
dotnet test --filter "Category=SampleValidation" --logger "console;verbosity=detailed"
```

---

## Expected vs Actual Test Results

### Expected Results

All validation tests should pass for sample files that:
- Are syntactically valid C# or VB code
- Have clear semantic changes (additions, removals, modifications)
- Don't contain edge cases that expose known bugs

### Actual Results

As of Sprint 3 completion:

**Test Summary:**
- Total validation test classes: 5 (including base class)
- Total validation test methods: 20+
- Expected pass rate: 100% for valid sample files

**Known Test Outcomes:**

#### Passing Tests
- JSON consistency across flag combinations ✅
- HTML consistency across flag combinations ✅
- Line number integrity for JSON format ✅
- Line number integrity for HTML format ✅
- Line number integrity for Text format ✅
- Cross-format consistency (JSON vs HTML) ✅
- Cross-format consistency (JSON vs Text) ✅

#### Tests Requiring Sample Files
Some tests will skip if no appropriate sample files are found:
- Tests use `Assert.Skip()` when sample files are missing
- This is expected behavior during development

#### Known Limitations
- External tool compatibility tests (if implemented) require `diff` and `git` to be installed
- Tests assume roslyn-diff CLI is built and accessible
- Some tests may timeout with very large sample files

---

## Test Execution Instructions

### Prerequisites

1. **Build the solution:**
   ```bash
   dotnet build
   ```

2. **Ensure CLI is built:**
   ```bash
   dotnet build src/RoslynDiff.Cli/
   ```

3. **Verify sample files exist:**
   ```bash
   ls tests/RoslynDiff.Integration.Tests/TestFixtures/
   ```

### Running Tests

#### Quick Validation (All Tests)

```bash
dotnet test tests/RoslynDiff.Integration.Tests/SampleValidation/
```

#### Comprehensive Validation (Entire Solution)

```bash
dotnet test
```

#### With Coverage

```bash
dotnet test --collect:"XPlat Code Coverage"
```

### Interpreting Results

#### All Tests Pass

```
Starting test execution, please wait...
A total of 1 test files matched the specified pattern.

Passed!  - Failed:     0, Passed:    20, Skipped:     0, Total:    20
```

This indicates all validations passed successfully.

#### Some Tests Fail

```
Failed!  - Failed:     3, Passed:    17, Skipped:     0, Total:    20

Failed Tests:
  - JsonConsistencyTests.Json002_LineNumberIntegrity_NoOverlaps
  - HtmlConsistencyTests.Html002_SectionLineNumberIntegrity
  - CrossFormatConsistencyTests.Xfmt003_AllFormatsAgreement_RoslynMode
```

When tests fail:
1. Review the detailed output to see which validations failed
2. Check the TestResult messages and Issues list
3. Use `PreserveTempFiles = true` to inspect generated outputs
4. Isolate the failing sample file to TempTestCases for debugging

#### Some Tests Skipped

```
Passed!  - Failed:     0, Passed:    18, Skipped:     2, Total:    20

Skipped Tests:
  - ExternalToolCompatibilityTests.Ext001_StandardDiffCompatibility
    Reason: External tool 'diff' not found on system
```

Skipped tests indicate:
- Missing sample files (expected during development)
- Missing external tools (optional tests)
- Platform-specific tests not applicable

---

## Adding New Validation Tests

### Template for New Test Class

```csharp
using FluentAssertions;
using RoslynDiff.TestUtilities.Models;
using RoslynDiff.TestUtilities.Validators;
using Xunit;

namespace RoslynDiff.Integration.Tests.SampleValidation;

/// <summary>
/// Tests for [describe what aspect you're testing].
/// </summary>
[Trait("Category", "SampleValidation")]
public class MyNewValidationTests : SampleValidationTestBase
{
    [Fact]
    public void MyTest_Scenario_ExpectedOutcome()
    {
        // Arrange
        var samplePairs = GetSampleFilePairs().ToList();
        if (!samplePairs.Any())
        {
            Assert.Skip("No sample files available");
        }

        var (oldFile, newFile, name) = samplePairs.First();
        var options = new SampleDataValidatorOptions
        {
            // Configure as needed
        };

        // Act
        var results = SampleDataValidator.ValidateAll(oldFile, newFile, options);

        // Assert
        AssertAllPassed(results);
    }
}
```

### Best Practices

1. **Inherit from SampleValidationTestBase** for common infrastructure
2. **Use descriptive test names** following pattern: `TestId_Scenario_ExpectedOutcome()`
3. **Add trait** `[Trait("Category", "SampleValidation")]` for filtering
4. **Handle missing samples** with `Assert.Skip()` when appropriate
5. **Use AssertAllPassed()** helper for validation results
6. **Document test purpose** in XML comments

---

## Known Failing Tests (If Any)

### As of Sprint 3 Completion

**Status:** No known consistently failing tests with valid sample files

**Intermittent Issues:**
- None reported

**Platform-Specific Issues:**
- None reported

### Reporting Test Failures

If you encounter failing tests:

1. **Document the failure:**
   - Test name
   - Sample files used
   - Full error message
   - TestResult Issues list

2. **Isolate the issue:**
   - Copy sample files to TempTestCases
   - Test with roslyn-diff CLI directly
   - Check if it's format-specific

3. **Create bug report:**
   - Include sample files (if not proprietary)
   - Include CLI output
   - Include expected vs actual behavior

---

## Integration with CI/CD

These tests are designed to run in CI pipelines:

### GitHub Actions Example

```yaml
- name: Run Sample Validation Tests
  run: |
    dotnet test --filter "Category=SampleValidation" \
                --logger "trx;LogFileName=validation-results.trx"

- name: Upload Test Results
  if: always()
  uses: actions/upload-artifact@v3
  with:
    name: sample-validation-results
    path: '**/validation-results.trx'
```

### Azure Pipelines Example

```yaml
- task: DotNetCoreCLI@2
  displayName: 'Run Sample Validation Tests'
  inputs:
    command: 'test'
    arguments: '--filter "Category=SampleValidation" --logger trx'
    publishTestResults: true
```

---

## Performance Characteristics

### Execution Time

Typical execution times per test class:
- **JsonConsistencyTests:** ~2-5 seconds per sample file
- **HtmlConsistencyTests:** ~2-5 seconds per sample file
- **LineNumberIntegrityTests:** ~5-10 seconds (tests all formats)
- **CrossFormatConsistencyTests:** ~5-10 seconds (tests all formats)

Total execution time for all validation tests: ~30-60 seconds (depends on sample file count)

### Optimization Tips

1. **Use smaller sample files** for faster tests
2. **Run specific test classes** during development
3. **Cache CLI builds** in CI to avoid rebuilding
4. **Parallel test execution** (xUnit runs tests in parallel by default)

---

## Related Documentation

- **TestUtilities README:** `tests/RoslynDiff.TestUtilities/README.md` - Detailed utility documentation
- **Testing Strategy:** `docs/testing-strategy-sample-validation.md` - Overall strategy document
- **Testing Guide:** `docs/testing.md` - General testing documentation
- **TempTestCases Guide:** `tests/RoslynDiff.Integration.Tests/TempTestCases/README.md` - Ad-hoc testing

---

## Summary

The Sample Validation test suite provides comprehensive validation of roslyn-diff output formats ensuring:

✅ **Format Consistency** - All formats report identical information
✅ **Line Number Integrity** - No overlaps or duplicates
✅ **Cross-Format Agreement** - JSON, HTML, Text, and Git formats agree
✅ **Structural Correctness** - Valid JSON, HTML, and diff formats
✅ **Modular Test Organization** - Granular failure reporting
✅ **CI/CD Integration** - Automated validation in pipelines

**Sprint 3 Status:** Complete and fully functional
