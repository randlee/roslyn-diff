# Bug Report #001: CLI Command Syntax Error in SampleDataValidator

## Summary
The `SampleDataValidator.GenerateOutput()` method uses an incorrect CLI command syntax, invoking `roslyn-diff file` instead of `roslyn-diff diff`, causing all CLI-invoked validation tests to fail with exit code 255.

## Severity
**P0 - CRITICAL**

This bug blocks 70.6% of all validation tests (24 out of 34 tests). It prevents any validation that requires generating fresh CLI output.

## Affected Components
- **Primary:** `RoslynDiff.TestUtilities/Validators/SampleDataValidator.cs` (line 500)
- **Secondary:** CLI argument formatting (line 501)

## Affected Tests
All 24 tests that invoke `SampleDataValidator.GenerateOutput()`:

### JsonConsistencyTests (5 failures)
- Json001_FlagCombinationConsistency_JsonVsJsonQuiet
- Json004_Calculator_ValidatesSuccessfully
- Json005_UserService_ValidatesSuccessfully
- Json006_AllSamples_JsonParseable
- Json007_LineMode_Calculator_ValidatesSuccessfully

### HtmlConsistencyTests (4 failures)
- Html001_FlagCombinationConsistency_HtmlToFile
- Html004_Calculator_ValidatesSuccessfully
- Html005_UserService_ValidatesSuccessfully
- Html006_AllSamples_HtmlParseable

### CrossFormatConsistencyTests (4 failures)
- Xfmt002_JsonVsText_LineNumbersMatch
- Xfmt004_AllFormats_LineMode_Agreement
- Xfmt005_Calculator_AllFormatsConsistent
- Xfmt006_UserService_AllFormatsConsistent

### LineNumberIntegrityTests (4 failures)
- LineIntegrity003_Calculator_IntegrityCheck
- LineIntegrity004_UserService_IntegrityCheck
- LineIntegrity005_RoslynMode_SequentialLineNumbers
- LineIntegrity006_LineMode_SequentialLineNumbers

### SampleCoverageTests (4 failures)
- Samp001_AllSamplesDirectory_ValidateAll
- Samp002_Calculator_CompleteValidation
- Samp003_UserService_CompleteValidation
- Samp005_AllSamples_LineMode_ValidateAll

### ExternalToolCompatibilityTests (3 failures)
- Ext001_RoslynDiffGit_VsStandardDiff
- Ext003_Calculator_ExternalToolCompatibility
- Ext004_UnifiedDiffFormat_ValidatesCorrectly

## Steps to Reproduce

1. Navigate to the test worktree:
   ```bash
   cd /Users/randlee/Documents/github/roslyn-diff-worktrees/feature/sample-data-validation-tests
   ```

2. Run any test that invokes CLI generation:
   ```bash
   dotnet test --filter "FullyQualifiedName~Json004_Calculator_ValidatesSuccessfully"
   ```

3. Observe: Test fails with "CLI invocation failed with exit code 255"

4. Verify the incorrect command manually:
   ```bash
   dotnet run --project src/RoslynDiff.Cli/ -- file samples/before/Calculator.cs samples/after/Calculator.cs --output json
   ```

   Output:
   ```
   Error: Unknown command 'file'.
   Exit code: 255
   ```

5. Verify the correct command works:
   ```bash
   dotnet run --project src/RoslynDiff.Cli/ -- diff samples/before/Calculator.cs samples/after/Calculator.cs --json
   ```

   Output:
   ```json
   {
     "$schema": "roslyn-diff-output-v1",
     "metadata": { ... },
     "summary": { "totalChanges": 7, ... }
   }
   Exit code: 0
   ```

## Expected Behavior

The validator should invoke the CLI with the correct command syntax:
```bash
roslyn-diff diff <old-file> <new-file> --<format>
```

Examples:
```bash
roslyn-diff diff old.cs new.cs --json
roslyn-diff diff old.cs new.cs --html --out-file output.html
roslyn-diff diff old.cs new.cs --text
roslyn-diff diff old.cs new.cs --git
```

## Actual Behavior

The validator currently invokes the CLI with incorrect syntax:
```bash
roslyn-diff file <old-file> <new-file> --output <format>
```

This causes the CLI to:
1. Not recognize the `file` command
2. Return exit code 255
3. Print error: "Unknown command 'file'"

The validator then:
1. Catches the non-zero exit code
2. Throws `InvalidOperationException`
3. Causes all dependent tests to fail

## Test Evidence

**Error Message (repeated across all 24 failures):**
```
Error during validation: CLI invocation failed with exit code 255:
System.InvalidOperationException: CLI invocation failed with exit code 255:
   at RoslynDiff.TestUtilities.Validators.SampleDataValidator.GenerateOutput(String oldFile, String newFile, String format, SampleDataValidatorOptions options)
   in /Users/randlee/Documents/github/roslyn-diff-worktrees/feature/sample-data-validation-tests/tests/RoslynDiff.TestUtilities/Validators/SampleDataValidator.cs:line 533
```

**Stack Trace Pattern:**
```
at SampleDataValidator.GenerateOutput(...) line 533
at SampleDataValidator.ValidateJsonConsistency(...) line 185
  OR
at SampleDataValidator.ValidateHtmlConsistency(...) line 265
  OR
at SampleDataValidator.ValidateCrossFormatConsistency(...) line 346
  OR
at SampleDataValidator.ValidateLineNumberIntegrity(...) line 79
```

## Source Files

**Primary Bug Location:**
File: `/Users/randlee/Documents/github/roslyn-diff-worktrees/feature/sample-data-validation-tests/tests/RoslynDiff.TestUtilities/Validators/SampleDataValidator.cs`

Lines 490-510 (current buggy code):
```csharp
private static string GenerateOutput(
    string oldFile,
    string newFile,
    string format,
    SampleDataValidatorOptions options)
{
    var outputDir = options.TempOutputDirectory ?? Path.GetTempPath();
    var outputFile = Path.Combine(outputDir, $"roslyn-diff-{Guid.NewGuid()}.{format}");

    // Determine the CLI path
    var cliPath = options.RoslynDiffCliPath ?? FindRoslynDiffCli();

    // Build arguments
    var args = new StringBuilder();
    args.Append($"file \"{oldFile}\" \"{newFile}\"");        // ❌ BUG: Should be "diff"
    args.Append($" --output {format}");                       // ❌ BUG: Should be "--{format}"
    args.Append($" --out-file \"{outputFile}\"");

    // Add mode flag if specified
    if (options.DiffMode != DiffMode.Auto)
    {
        args.Append($" --mode {options.DiffMode.ToString().ToLowerInvariant()}");
    }

    // Execute the CLI
    var startInfo = new ProcessStartInfo
    {
        FileName = cliPath,
        Arguments = args.ToString(),
        UseShellExecute = false,
        RedirectStandardOutput = true,
        RedirectStandardError = true,
        CreateNoWindow = true
    };

    using var process = new Process { StartInfo = startInfo };
    process.Start();

    if (!process.WaitForExit(options.CliTimeoutMs))
    {
        process.Kill();
        throw new TimeoutException($"CLI invocation timed out after {options.CliTimeoutMs}ms");
    }

    if (process.ExitCode != 0)  // ← Fails here with exit code 255
    {
        var error = process.StandardError.ReadToEnd();
        throw new InvalidOperationException($"CLI invocation failed with exit code {process.ExitCode}: {error}");
    }

    return outputFile;
}
```

## Root Cause Analysis

**Root Cause:** Developer error during test infrastructure implementation.

**Why this happened:**
1. The roslyn-diff CLI supports multiple commands: `diff`, `class`, etc.
2. The validator author may have assumed a `file` command existed
3. The CLI argument structure `--output <format>` is not the actual CLI design
4. The actual CLI uses format-specific flags: `--json`, `--html`, `--text`, `--git`

**Why tests passed locally (hypothesis):**
The 10 passing tests don't invoke `GenerateOutput()` - they validate existing pre-generated files. This suggests:
1. Sample output files may have been generated manually during development
2. Tests were designed to work with existing outputs first
3. CLI invocation was added later without proper verification

## Proposed Fix

### Change 1: Fix command name
**File:** `RoslynDiff.TestUtilities/Validators/SampleDataValidator.cs`
**Line:** 500

**Current (buggy):**
```csharp
args.Append($"file \"{oldFile}\" \"{newFile}\"");
```

**Fixed:**
```csharp
args.Append($"diff \"{oldFile}\" \"{newFile}\"");
```

### Change 2: Fix output format flag and file path
**File:** `RoslynDiff.TestUtilities/Validators/SampleDataValidator.cs`
**Lines:** 501-502

**Current (buggy):**
```csharp
args.Append($" --output {format}");
args.Append($" --out-file \"{outputFile}\"");
```

**Fixed:**
```csharp
args.Append($" --{format.ToLowerInvariant()} \"{outputFile}\"");
// Remove the --out-file line entirely
```

**Explanation:** The CLI uses format flags that accept an optional path argument:
- `--json [PATH]` - Output JSON (to stdout if no path, to file if path provided)
- `--html [PATH]` - Output HTML (to stdout if no path, to file if path provided)
- `--text [PATH]` - Output text (to stdout if no path, to file if path provided)
- `--git [PATH]` - Output git diff (to stdout if no path, to file if path provided)

The CLI does NOT have a separate `--out-file` flag. The path is passed directly to the format flag.

### Full corrected method (lines 498-503):
```csharp
// Build arguments
var args = new StringBuilder();
args.Append($"diff \"{oldFile}\" \"{newFile}\"");                    // ✅ FIXED: Changed "file" to "diff"
args.Append($" --{format.ToLowerInvariant()} \"{outputFile}\"");    // ✅ FIXED: Combined format flag and path
// Remove: args.Append($" --out-file \"{outputFile}\"");             // ✅ REMOVED: CLI doesn't use --out-file
```

## Testing the Fix

After applying the fix, verify with:

### 1. Manual CLI test
```bash
cd /Users/randlee/Documents/github/roslyn-diff-worktrees/feature/sample-data-validation-tests

# Test JSON output
dotnet run --project src/RoslynDiff.Cli/ -- diff samples/before/Calculator.cs samples/after/Calculator.cs --json

# Test HTML output
dotnet run --project src/RoslynDiff.Cli/ -- diff samples/before/Calculator.cs samples/after/Calculator.cs --html --out-file /tmp/test.html

# Verify exit codes
echo $?  # Should be 0
```

### 2. Run a single failing test
```bash
dotnet test --filter "FullyQualifiedName~Json004_Calculator_ValidatesSuccessfully" -v n
```

Expected: Test should now pass (or fail with a different, more specific validation error)

### 3. Run all JSON tests
```bash
dotnet test --filter "FullyQualifiedName~JsonConsistencyTests" -v n
```

Expected: Some or all tests should now pass

### 4. Run full validation suite
```bash
dotnet test --filter "Category=SampleValidation" -v n
```

Expected: Pass rate should increase from 29.4% to ≥ 60%

## Related Issues

**None** - This is the primary blocking bug. Secondary issues may surface after this fix.

## Impact Assessment

**Current Impact:**
- **Tests blocked:** 24 out of 34 (70.6%)
- **Test categories blocked:** 6 out of 6
- **Validation coverage:** Only 29.4% testable

**After Fix:**
- **Tests blocked:** Expected 0 (or new, specific validation failures)
- **Test categories blocked:** Expected 0
- **Validation coverage:** Expected 60-80% or higher

**Production Impact:**
- **Users affected:** None (test infrastructure only)
- **Data integrity risk:** None (production CLI is unaffected)
- **Performance impact:** None

**Regression Risk:**
- **Risk level:** VERY LOW
- **Reason:** Change is isolated to test utilities, doesn't affect production code
- **Mitigation:** Comprehensive test run will immediately verify fix

## Priority Justification

**Why P0:**
1. **Blocks majority of tests:** 70.6% of validation suite cannot run
2. **Blocks all test categories:** Every test category has failures
3. **Prevents validation:** Cannot validate CLI output correctness
4. **Prevents further development:** Secondary issues cannot be discovered
5. **Simple fix:** Low-complexity, high-impact resolution

**Why must fix in Sprint 4:**
1. Sprint 4 goal is to validate sample data quality
2. Cannot achieve goal without fixing this bug
3. Remaining sprint time depends on unblocking these tests
4. Other bugs cannot be discovered until this is fixed

## Estimated Fix Time

**Code changes:** 15 minutes
- Change line 500: `file` → `diff`
- Change lines 501-502: Combine into single flag with path argument
- Remove separate `--out-file` line

**Testing:** 15 minutes
- Run manual CLI commands
- Run single test
- Run test class
- Verify exit codes

**Documentation:** 5 minutes
- Update this bug report with "FIXED" status
- Document any additional issues found

**Total:** 35 minutes

## Fix Verification Checklist

After implementing the fix, verify:

- [ ] Manual CLI invocation with `diff` command works
- [ ] Manual CLI invocation with `--json` flag works
- [ ] Manual CLI invocation with `--html` flag works
- [ ] Manual CLI invocation with `--text` flag works
- [ ] Manual CLI invocation with `--git` flag works
- [ ] Manual CLI invocation with `--out-file` flag works (or implement alternative)
- [ ] Test Json004_Calculator_ValidatesSuccessfully passes (or fails with new error)
- [ ] Test Html004_Calculator_ValidatesSuccessfully passes (or fails with new error)
- [ ] Test Xfmt005_Calculator_AllFormatsConsistent passes (or fails with new error)
- [ ] Overall test pass rate increases above 50%
- [ ] No tests that previously passed now fail (regression check)

## Additional Notes

**Why 10 tests passed despite this bug:**

The 10 passing tests all share a characteristic: they don't invoke `GenerateOutput()`. They either:
1. Validate existing output files (Json002, Json003, Html002, Html003, etc.)
2. Perform file system checks (Samp004)

This indicates that some sample outputs were pre-generated during development, allowing validation logic tests to pass while CLI invocation tests failed.

**Post-fix expectations:**

After fixing BUG-001, expect:
1. Many tests will now execute CLI successfully
2. Some tests may reveal new, specific validation issues:
   - Line number overlaps
   - Line number duplicates
   - Cross-format inconsistencies
   - HTML parsing errors
   - External tool compatibility issues
3. These will be documented as separate bugs (BUG-002, BUG-003, etc.)

**CLI command reference:**

For developer reference, the roslyn-diff CLI structure is:
```
roslyn-diff <command> [arguments] [options]

Commands:
  diff <old-file> <new-file>    Compare files/directories
  class <OLD-SPEC> <NEW-SPEC>   Compare specific classes

Options for 'diff' command:
  --json                Output as JSON
  --html                Output as HTML
  --text                Output as text
  --git                 Output as git-style diff
  --mode <mode>         Diff mode: roslyn, line, or auto
  --out-file <file>     Write output to file (needs verification)
  --quiet               Suppress console output
  --context <n>         Number of context lines
```
