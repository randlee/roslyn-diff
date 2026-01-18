# Bug Report #002: CLI Returns Exit Code 1 When Successfully Writing Output Files

## Summary
The roslyn-diff CLI returns exit code 1 when successfully writing output to a file using format flags with path arguments (e.g., `--json <path>`), even though the file is created correctly and contains valid output.

## Severity
**P0 - CRITICAL**

This bug will block all validation tests even after BUG-001 is fixed, because the validator checks for `exitCode != 0` and treats any non-zero exit code as a failure.

## Affected Components
- **Primary:** `src/RoslynDiff.Cli/` - CLI exit code logic
- **Secondary:** All output formatters when writing to files

## Affected Tests
After BUG-001 is fixed, this bug will block all 24 tests that use file output:
- All 5 JsonConsistencyTests that generate output
- All 4 HtmlConsistencyTests that generate output
- All 4 CrossFormatConsistencyTests that generate output
- All 4 LineNumberIntegrityTests that generate output
- All 4 SampleCoverageTests that generate output
- All 3 ExternalToolCompatibilityTests that generate output

**Note:** This bug is currently masked by BUG-001. It will surface immediately after BUG-001 is fixed.

## Steps to Reproduce

1. Navigate to the test worktree:
   ```bash
   cd /Users/randlee/Documents/github/roslyn-diff-worktrees/feature/sample-data-validation-tests
   ```

2. Run CLI with JSON output to file:
   ```bash
   dotnet run --project src/RoslynDiff.Cli/ -- diff samples/before/Calculator.cs samples/after/Calculator.cs --json /tmp/test-output.json
   echo "Exit code: $?"
   ```

3. Observe:
   - Message printed: "Output written to: /tmp/test-output.json"
   - Exit code: **1** (should be **0**)
   - File created: ✅ YES
   - File content: ✅ Valid JSON

4. Verify file was created successfully:
   ```bash
   ls -lh /tmp/test-output.json
   head -20 /tmp/test-output.json
   ```

5. Compare with stdout output:
   ```bash
   dotnet run --project src/RoslynDiff.Cli/ -- diff samples/before/Calculator.cs samples/after/Calculator.cs --json
   echo "Exit code: $?"
   ```

   Observe:
   - JSON printed to stdout
   - Exit code: **0** ✅ (correct)

## Expected Behavior

When the CLI successfully writes output to a file, it should:
1. Create the file with valid content
2. Print confirmation message (optional)
3. **Return exit code 0** (success)

Example of correct behavior:
```bash
$ roslyn-diff diff old.cs new.cs --json output.json
Output written to: output.json
$ echo $?
0  # ✅ Success
```

## Actual Behavior

When the CLI writes output to a file, it:
1. ✅ Creates the file with valid content
2. ✅ Prints confirmation message
3. ❌ **Returns exit code 1** (error)

Example of current (buggy) behavior:
```bash
$ roslyn-diff diff old.cs new.cs --json output.json
Output written to: output.json
$ echo $?
1  # ❌ Indicates error, but operation succeeded
```

## Test Evidence

**Manual verification:**

```bash
$ cd /Users/randlee/Documents/github/roslyn-diff-worktrees/feature/sample-data-validation-tests

$ dotnet run --project src/RoslynDiff.Cli/ -- diff samples/before/Calculator.cs samples/after/Calculator.cs --json /tmp/test-output.json
Output written to: /tmp/test-output.json
$ echo $?
1

$ ls -lh /tmp/test-output.json
-rw-r--r--@ 1 randlee  wheel    11K Jan 17 19:36 /tmp/test-output.json

$ head -20 /tmp/test-output.json
{
  "$schema": "roslyn-diff-output-v1",
  "metadata": {
    "version": "0.7.0+6d91b37aee7b5274d5043665dc16582fe8baf3c8",
    "timestamp": "2026-01-18T03:36:16.169537+00:00",
    "mode": "roslyn",
    "options": {
      "includeContent": true,
      "contextLines": 3
    }
  },
  "summary": {
    "totalChanges": 7,
    "additions": 4,
    "deletions": 0,
    "modifications": 3,
    "renames": 0,
    "moves": 0
  }
  ...
}
```

**Conclusion:** File is valid, content is correct, but exit code is wrong.

## Impact on Tests

After BUG-001 is fixed, the validator will use corrected syntax:
```csharp
args.Append($"diff \"{oldFile}\" \"{newFile}\"");
args.Append($" --{format} \"{outputFile}\"");  // e.g., --json "/tmp/output.json"
```

The CLI will then:
1. ✅ Execute the correct command
2. ✅ Create the output file
3. ❌ Return exit code 1

The validator will then:
```csharp
if (process.ExitCode != 0)  // ← Will be true (exitCode == 1)
{
    var error = process.StandardError.ReadToEnd();
    throw new InvalidOperationException($"CLI invocation failed with exit code {process.ExitCode}: {error}");
}
```

Result: All tests still fail, with error message "CLI invocation failed with exit code 1"

## Root Cause Analysis

**Hypothesis 1:** CLI intentionally returns 1 when differences are found
- **Likelihood:** HIGH
- **Reasoning:** Many diff tools (git diff, standard diff) return 1 when differences exist, 0 when files are identical
- **Issue:** This behavior conflicts with validator expectations that 0 = success regardless of diff content

**Hypothesis 2:** CLI has a bug in file output path
- **Likelihood:** MEDIUM
- **Reasoning:** Exit code 0 works for stdout, but 1 for file output
- **Issue:** Possible missing return statement or incorrect error handling

**Hypothesis 3:** CLI returns exit code based on diff result instead of operation success
- **Likelihood:** HIGH (same as Hypothesis 1)
- **Reasoning:** This is standard diff tool behavior
- **Issue:** Tests need to accommodate this semantic difference

**Most Likely Cause:** CLI follows standard diff tool convention where:
- Exit code 0 = files are identical (no differences)
- Exit code 1 = files differ (differences found)
- Exit code 2+ = error occurred

**Investigation needed:** Check CLI source code to confirm exit code semantics.

## Proposed Fix Options

### Option 1: Fix CLI to Return 0 on Success (Recommended if wrong behavior)
If the CLI is **supposed** to return 0 for successful operations regardless of diff content:

**Change:** Update CLI to return 0 when file write succeeds
**Location:** `src/RoslynDiff.Cli/` - wherever exit codes are set
**Risk:** Low if this is indeed a bug

### Option 2: Fix Validator to Accept Exit Code 1 (Recommended if standard diff behavior)
If the CLI **intentionally** returns 1 when differences are found (standard diff behavior):

**Change:** Update validator to accept exit codes 0 or 1 as success
**Location:** `RoslynDiff.TestUtilities/Validators/SampleDataValidator.cs`, line 530

**Current (buggy):**
```csharp
if (process.ExitCode != 0)
{
    var error = process.StandardError.ReadToEnd();
    throw new InvalidOperationException($"CLI invocation failed with exit code {process.ExitCode}: {error}");
}
```

**Fixed:**
```csharp
// Exit codes: 0 = no differences, 1 = differences found, 2+ = error
if (process.ExitCode > 1)
{
    var error = process.StandardError.ReadToEnd();
    throw new InvalidOperationException($"CLI invocation failed with exit code {process.ExitCode}: {error}");
}
```

**Risk:** Low - standard diff tools use this convention

### Option 3: Check File Existence Instead of Exit Code (Alternative)
If exit code semantics are ambiguous:

**Change:** Check if output file exists and is valid instead of checking exit code
**Location:** `RoslynDiff.TestUtilities/Validators/SampleDataValidator.cs`, line 530-534

**Replacement:**
```csharp
// Don't rely on exit code - check if output file was created
if (!File.Exists(outputFile))
{
    var error = process.StandardError.ReadToEnd();
    throw new InvalidOperationException($"CLI did not create output file (exit code {process.ExitCode}): {error}");
}

// Verify file has content
var fileInfo = new FileInfo(outputFile);
if (fileInfo.Length == 0)
{
    throw new InvalidOperationException($"CLI created empty output file (exit code {process.ExitCode})");
}
```

**Risk:** Medium - doesn't detect other CLI errors

## Recommended Fix: Option 2

**Rationale:**
1. Exit code 1 for "differences found" is standard diff tool behavior
2. Validator should distinguish between "differences found" (1) and "error occurred" (2+)
3. This follows Unix exit code conventions
4. Minimal code change required
5. Clear semantic meaning

**Implementation:**
```csharp
// In SampleDataValidator.cs, line 530:
// Exit codes: 0 = identical, 1 = differences found, 2+ = error
if (process.ExitCode > 1)
{
    var error = process.StandardError.ReadToEnd();
    throw new InvalidOperationException($"CLI invocation failed with exit code {process.ExitCode}: {error}");
}
```

## Testing the Fix

After applying the recommended fix (Option 2):

### 1. Verify exit code handling
```bash
# Test with files that have differences (should return 1, be accepted)
dotnet test --filter "FullyQualifiedName~Json004_Calculator_ValidatesSuccessfully" -v n

# Test with identical files (should return 0, be accepted)
dotnet test --filter "FullyQualifiedName~Json002_LineNumberIntegrity" -v n
```

### 2. Test error cases
```bash
# Test with invalid files (should return 2+, be rejected)
dotnet run --project src/RoslynDiff.Cli/ -- diff nonexistent1.cs nonexistent2.cs --json /tmp/test.json
echo "Exit code: $?"  # Should be 2 or higher
```

### 3. Run full suite
```bash
dotnet test --filter "Category=SampleValidation" -v n
```

Expected: Pass rate should increase to 60%+ after fixing BUG-001 and BUG-002

## Related Issues

**BUG-001: CLI Command Syntax Error**
- Must be fixed before BUG-002 surfaces
- BUG-002 is currently masked by BUG-001

**No other known related issues**

## Impact Assessment

**Current Impact:**
- **Hidden by BUG-001** - Not yet affecting tests

**After BUG-001 Fix:**
- **Tests blocked:** 24 out of 24 newly-unblocked tests
- **Test pass rate:** Will remain at ~29% instead of improving
- **Validation coverage:** Still blocked

**After Both BUG-001 and BUG-002 Fixed:**
- **Tests blocked:** Expected 0-10 (only tests with actual validation failures)
- **Test pass rate:** Expected 60-80%
- **Validation coverage:** Fully functional

**Production Impact:**
- **Users affected:** None (test infrastructure only)
- **Data integrity risk:** None
- **CLI functionality:** Not affected (exit code is semantic, not functional)

**Regression Risk:**
- **Risk level:** VERY LOW
- **Reason:** Change is isolated to test utilities
- **Mitigation:** Comprehensive test run will verify

## Priority Justification

**Why P0:**
1. **Blocks all tests after BUG-001 fix** - Will prevent validation suite from working
2. **Zero test improvement without fix** - Pass rate won't increase
3. **Simple fix** - Single-line code change
4. **High confidence** - Standard diff tool behavior is well-understood
5. **Must fix before other bugs** - Prevents discovery of actual validation issues

**Why must fix in Sprint 4:**
1. Required to unblock test suite after BUG-001
2. Required to achieve Sprint 4 goals (validate sample data)
3. Required to discover any remaining bugs
4. Simple fix that should take < 15 minutes

## Estimated Fix Time

**Investigation:** 5 minutes
- Verify exit code semantics in CLI source code
- Confirm this is standard diff behavior

**Code changes:** 5 minutes
- Change line 530 from `!= 0` to `> 1`
- Add comment explaining exit code semantics

**Testing:** 10 minutes
- Test with differences (exit code 1)
- Test with no differences (exit code 0)
- Test with errors (exit code 2+)
- Run full test suite

**Total:** 20 minutes

## Fix Verification Checklist

After implementing the fix, verify:

- [ ] Validator accepts exit code 0 (no differences)
- [ ] Validator accepts exit code 1 (differences found)
- [ ] Validator rejects exit code 2+ (error)
- [ ] Test with Calculator sample succeeds (has differences)
- [ ] Test with UserService sample succeeds (has differences)
- [ ] Test with identical files succeeds (no differences, if such test exists)
- [ ] Test with invalid files fails appropriately (error case)
- [ ] Overall test pass rate increases above 50%
- [ ] No previously passing tests now fail

## Additional Notes

**Exit Code Conventions:**

Standard Unix/Linux diff tools use:
- **0** = Files are identical (no differences)
- **1** = Files differ (differences found)
- **2** = Error occurred (file not found, permission denied, etc.)

Examples:
```bash
$ diff file1.txt file1.txt    # Identical
$ echo $?
0

$ diff file1.txt file2.txt    # Different
$ echo $?
1

$ diff nonexistent.txt file1.txt    # Error
diff: nonexistent.txt: No such file or directory
$ echo $?
2
```

The roslyn-diff CLI appears to follow this convention, which is correct behavior. The validator needs to accommodate this standard.

**Validator Philosophy:**

The validator should:
- Treat exit code 0 or 1 as "operation successful"
- Treat exit code 2+ as "error occurred"
- Rely on file existence and content validity, not just exit code
- Distinguish between "CLI error" and "no differences found"

**Impact on Other Tests:**

Some tests may expect specific diff results:
- Tests comparing identical files: Should see exit code 0
- Tests comparing different files: Should see exit code 1
- Both cases should be treated as success by validator

## Fix Dependency Chain

This bug has a dependency relationship:

```
BUG-001 (CLI command syntax)
    ↓ Must fix first
BUG-002 (CLI exit code) ← You are here
    ↓ Must fix second
Actual Validation Issues (if any)
    ↓ Can fix in any order
Sprint 4 Complete
```

**Critical Path:**
1. Fix BUG-001 (35 minutes)
2. Fix BUG-002 (20 minutes)
3. Re-run tests (10 minutes)
4. **Total time to unblock:** 65 minutes (~1 hour)

After 1 hour, the validation suite should be fully functional and reveal any actual data quality issues.
