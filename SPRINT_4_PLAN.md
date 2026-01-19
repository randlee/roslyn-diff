# Sprint 4 Plan: Execute Validation Testing

**Branch:** `feature/sample-data-validation-tests`
**Date:** 2026-01-17
**Sprint Goal:** Fix compilation errors, execute validation tests, discover and fix bugs

---

## Executive Summary

### Sprint 4 Mission

**Transform the validation testing infrastructure from "ready to use" to "actively catching bugs"**

Sprint 4 will:
1. Create the 6 missing integration test class files with 36 test methods
2. Execute all validation tests against sample data
3. Document detailed test results (passes and failures)
4. Fix critical bugs discovered by validation tests
5. Establish repeatable validation workflow

### Current State

✅ **Infrastructure:** 100% complete and tested (161 unit tests passing)
❌ **Integration Tests:** 0% complete (zero test files exist)
❌ **Validation Execution:** 0% complete (no tests have run)
❌ **Bug Discovery:** 0% complete (no bugs found via validation)

### Target State (End of Sprint 4)

✅ **Integration Tests:** 100% complete (all 6 test classes compile)
✅ **Validation Execution:** 100% complete (all tests executed on sample data)
✅ **Bug Discovery:** Complete documentation of all failures
✅ **Critical Bug Fixes:** P0 bugs fixed, P1/P2 documented

---

## Sprint 4 Architecture: 3 Parallel Workstreams

### Workstream Overview

```
┌─────────────────────────────────────────────────────────────┐
│                     SPRINT 4 WORKFLOW                        │
├─────────────────────────────────────────────────────────────┤
│                                                              │
│  Workstream A          Workstream B           Workstream C  │
│  (Days 1-2)            (Days 2-3)             (Days 3-5)    │
│                                                              │
│  ┌──────────┐         ┌──────────┐           ┌──────────┐  │
│  │  Create  │         │  Execute │           │   Fix    │  │
│  │  Test    │────────▶│  Tests & │──────────▶│  Bugs &  │  │
│  │  Files   │         │ Document │           │ Re-test  │  │
│  └──────────┘         └──────────┘           └──────────┘  │
│       │                     │                      │        │
│       │                     │                      │        │
│   Compile &             Capture              Verify Fixes   │
│   Discover              Failures             Pass Tests     │
│                                                              │
└─────────────────────────────────────────────────────────────┘
```

### Dependencies

- **Workstream A → B:** Workstream B requires compilable tests from Workstream A
- **Workstream B → C:** Workstream C requires failure documentation from Workstream B
- **Workstream C → B:** Bug fixes may require re-running tests (iterative)

### Timeline

```
Day 1    Day 2    Day 3    Day 4    Day 5
  │        │        │        │        │
  ├─ A ───┤        │        │        │    Workstream A: Create Tests
  │        ├─ B ───┤        │        │    Workstream B: Execute & Document
  │        │        ├───── C ────────┤    Workstream C: Fix Bugs
  │        │        │        │        │
  └────────┴────────┴────────┴────────┘
```

---

## Workstream A: Fix Compilation Errors (Create Test Classes)

### Goal
Create all 6 integration test classes with minimum 30 test methods, ensuring all tests compile and are discoverable by xUnit.

### Duration
**2 days** (16 hours of focused work)

### Prerequisites
- ✅ TestUtilities infrastructure is ready
- ✅ SampleValidationTestBase exists
- ✅ Sample files exist (Calculator.cs, UserService.cs)
- ✅ roslyn-diff CLI is built

### Tasks

#### Task A.1: Create JsonConsistencyTests.cs
**Time:** 3 hours

**Test Methods (minimum 6):**
```csharp
[Fact]
[Trait("Category", "SampleValidation")]
public void Json001_FlagCombinationConsistency_JsonVsJsonQuiet()
{
    // For each sample file pair:
    // 1. Run: roslyn-diff diff old.cs new.cs --json
    // 2. Run: roslyn-diff diff old.cs new.cs --json --quiet
    // 3. Parse both JSON outputs
    // 4. Assert: Content is identical (ignoring timestamps)
}

[Fact]
[Trait("Category", "SampleValidation")]
public void Json002_LineNumberIntegrity_NoOverlaps()
{
    // For each sample file pair:
    // 1. Run: roslyn-diff diff old.cs new.cs --json
    // 2. Parse JSON and extract all line ranges
    // 3. Assert: LineRangeComparer.DetectOverlaps() returns empty
}

[Fact]
[Trait("Category", "SampleValidation")]
public void Json002_LineNumberIntegrity_NoDuplicates()
{
    // For each sample file pair:
    // 1. Run: roslyn-diff diff old.cs new.cs --json
    // 2. Parse JSON and extract all line numbers
    // 3. Assert: No duplicate line numbers within change types
}

[Fact]
[Trait("Category", "SampleValidation")]
public void Json003_Calculator_ValidatesSuccessfully()
{
    // Test: Calculator.cs sample specifically
    // 1. Run: roslyn-diff diff samples/before/Calculator.cs samples/after/Calculator.cs --json
    // 2. Validate line number integrity
    // 3. Validate JSON structure
}

[Fact]
[Trait("Category", "SampleValidation")]
public void Json004_UserService_ValidatesSuccessfully()
{
    // Test: UserService.cs sample specifically
}

[Fact]
[Trait("Category", "SampleValidation")]
public void Json005_AllSamples_JsonParseable()
{
    // Test: All samples produce valid JSON
    // 1. For each sample pair
    // 2. Run with --json
    // 3. Assert: JSON.parse succeeds
}
```

**Deliverable:** JsonConsistencyTests.cs with 6+ test methods, compiles successfully

#### Task A.2: Create HtmlConsistencyTests.cs
**Time:** 3 hours

**Test Methods (minimum 5):**
```csharp
[Fact]
[Trait("Category", "SampleValidation")]
public void Html001_FlagCombinationConsistency_HtmlToFile()
{
    // Test: --html report.html produces consistent output
}

[Fact]
[Trait("Category", "SampleValidation")]
public void Html002_SectionLineNumberIntegrity_NoOverlaps()
{
    // Test: HTML sections have no overlapping line numbers
}

[Fact]
[Trait("Category", "SampleValidation")]
public void Html003_DataAttributeConsistency_MatchVisualDisplay()
{
    // Test: data-old-line and data-new-line match visual line numbers
}

[Fact]
[Trait("Category", "SampleValidation")]
public void Html004_Calculator_ValidatesSuccessfully()
{
    // Test: Calculator.cs HTML output validates
}

[Fact]
[Trait("Category", "SampleValidation")]
public void Html005_UserService_ValidatesSuccessfully()
{
    // Test: UserService.cs HTML output validates
}
```

**Deliverable:** HtmlConsistencyTests.cs with 5+ test methods, compiles successfully

#### Task A.3: Create CrossFormatConsistencyTests.cs
**Time:** 3 hours

**Test Methods (minimum 5):**
```csharp
[Fact]
[Trait("Category", "SampleValidation")]
public void Xfmt001_JsonVsHtml_LineNumbersMatch()
{
    // For each sample:
    // 1. Generate JSON and HTML
    // 2. Extract line numbers from both
    // 3. Assert: Sets are identical
}

[Fact]
[Trait("Category", "SampleValidation")]
public void Xfmt002_JsonVsText_LineNumbersMatch()
{
    // Test: JSON and Text report same line numbers
}

[Fact]
[Trait("Category", "SampleValidation")]
public void Xfmt003_AllFormats_RoslynMode_Agreement()
{
    // Test: JSON, HTML, Text all agree in Roslyn mode
    // 1. Run all formats with --mode roslyn
    // 2. Compare change counts, line numbers, symbols
}

[Fact]
[Trait("Category", "SampleValidation")]
public void Xfmt004_AllFormats_LineMode_Agreement()
{
    // Test: All formats agree in line-by-line mode
}

[Fact]
[Trait("Category", "SampleValidation")]
public void Xfmt005_Calculator_AllFormatsConsistent()
{
    // Test: Calculator.cs produces consistent output across formats
}
```

**Deliverable:** CrossFormatConsistencyTests.cs with 5+ test methods, compiles successfully

#### Task A.4: Create LineNumberIntegrityTests.cs
**Time:** 2 hours

**Test Methods (minimum 5):**
```csharp
[Fact]
[Trait("Category", "SampleValidation")]
public void LineIntegrity001_AllFormats_NoOverlaps()
{
    // Test: All output formats have no overlapping line ranges
}

[Fact]
[Trait("Category", "SampleValidation")]
public void LineIntegrity002_AllFormats_NoDuplicates()
{
    // Test: All output formats have no duplicate line numbers
}

[Fact]
[Trait("Category", "SampleValidation")]
public void LineIntegrity003_Calculator_IntegrityCheck()
{
    // Test: Calculator.cs line number integrity
}

[Fact]
[Trait("Category", "SampleValidation")]
public void LineIntegrity004_UserService_IntegrityCheck()
{
    // Test: UserService.cs line number integrity
}

[Fact]
[Trait("Category", "SampleValidation")]
public void LineIntegrity005_RoslynMode_SequentialLineNumbers()
{
    // Test: Roslyn mode produces sequential line ranges
}
```

**Deliverable:** LineNumberIntegrityTests.cs with 5+ test methods, compiles successfully

#### Task A.5: Create SampleCoverageTests.cs
**Time:** 2 hours

**Test Methods (minimum 4):**
```csharp
[Fact]
[Trait("Category", "SampleValidation")]
public void Samp001_AllSamplesDirectory_ValidateAll()
{
    // Test: All files in samples/ validate successfully
    // 1. For each file pair in samples/before and samples/after
    // 2. Run SampleDataValidator.ValidateAll()
    // 3. Assert: All validations pass
}

[Fact]
[Trait("Category", "SampleValidation")]
public void Samp002_Calculator_CompleteValidation()
{
    // Test: Calculator.cs complete validation
    var results = SampleDataValidator.ValidateAll(
        "samples/before/Calculator.cs",
        "samples/after/Calculator.cs"
    );
    AssertAllPassed(results);
}

[Fact]
[Trait("Category", "SampleValidation")]
public void Samp003_UserService_CompleteValidation()
{
    // Test: UserService.cs complete validation
}

[Fact]
[Trait("Category", "SampleValidation")]
public void Samp004_SampleCount_MatchesExpected()
{
    // Test: Correct number of sample files discovered
    var pairs = GetSampleFilePairs().ToList();
    pairs.Should().HaveCount(2); // Calculator + UserService
}
```

**Deliverable:** SampleCoverageTests.cs with 4+ test methods, compiles successfully

#### Task A.6: Create ExternalToolCompatibilityTests.cs (Optional, P1)
**Time:** 3 hours

**Test Methods (minimum 3):**
```csharp
[Fact]
[Trait("Category", "SampleValidation")]
[Trait("Category", "RequiresExternalTools")]
public void Ext001_RoslynDiffGit_VsStandardDiff()
{
    // Test: roslyn-diff --git vs diff -u
    // Skip if diff not available
    if (!DiffToolRunner.IsAvailable())
    {
        Assert.Skip("diff tool not available");
    }

    // Compare outputs
}

[Fact]
[Trait("Category", "SampleValidation")]
[Trait("Category", "RequiresExternalTools")]
public void Ext002_RoslynDiffGit_VsGitDiff()
{
    // Test: roslyn-diff --git vs git diff --no-index
    if (!GitDiffRunner.IsAvailable())
    {
        Assert.Skip("git not available");
    }
}

[Fact]
[Trait("Category", "SampleValidation")]
[Trait("Category", "RequiresExternalTools")]
public void Ext003_Calculator_ExternalToolCompatibility()
{
    // Test: Calculator.cs compatibility with external tools
}
```

**Deliverable:** ExternalToolCompatibilityTests.cs with 3+ test methods (skips gracefully if tools unavailable)

#### Task A.7: Verify Compilation and Test Discovery
**Time:** 1 hour

**Activities:**
1. Build solution: `dotnet build`
2. Verify zero compilation errors
3. List tests: `dotnet test --list-tests --filter "Category=SampleValidation"`
4. Verify minimum 30 tests discovered
5. Fix any compilation errors
6. Document any issues encountered

**Deliverable:** Clean build with 30+ discoverable validation tests

### Workstream A Success Criteria

✅ All 6 test class files created (.cs files exist on disk)
✅ Minimum 30 test methods total across all classes
✅ All tests use `[Fact]` and `[Trait("Category", "SampleValidation")]`
✅ All test classes inherit from SampleValidationTestBase
✅ Solution compiles with zero errors
✅ All tests are discoverable via `dotnet test --list-tests`
✅ Tests follow xUnit best practices
✅ Tests use FluentAssertions for readable assertions

### Workstream A Deliverables

```
tests/RoslynDiff.Integration.Tests/SampleValidation/
├── SampleValidationTestBase.cs                ✅ Already exists
├── JsonConsistencyTests.cs                    🆕 6+ tests
├── HtmlConsistencyTests.cs                    🆕 5+ tests
├── CrossFormatConsistencyTests.cs             🆕 5+ tests
├── LineNumberIntegrityTests.cs                🆕 5+ tests
├── SampleCoverageTests.cs                     🆕 4+ tests
└── ExternalToolCompatibilityTests.cs          🆕 3+ tests (optional)
```

**Total: 6 new files, 30+ new test methods**

---

## Workstream B: Execute Validation Tests & Document Findings

### Goal
Run every validation test, capture detailed results, document all failures with error messages, and identify patterns in failures.

### Duration
**1.5 days** (12 hours)

### Prerequisites
- ✅ Workstream A complete (all tests compile)
- ✅ roslyn-diff CLI built
- ✅ Sample files available

### Tasks

#### Task B.1: Execute All Validation Tests
**Time:** 2 hours

**Activities:**

1. **Run Complete Test Suite**
   ```bash
   cd /Users/randlee/Documents/github/roslyn-diff-worktrees/feature/sample-data-validation-tests

   # Run all validation tests with detailed output
   dotnet test --filter "Category=SampleValidation" \
               --logger "console;verbosity=detailed" \
               > TEST_RESULTS_RAW.txt 2>&1
   ```

2. **Run Each Test Class Individually**
   ```bash
   # JSON tests
   dotnet test --filter "FullyQualifiedName~JsonConsistencyTests" \
               --logger "console;verbosity=detailed" \
               > TEST_RESULTS_JSON.txt 2>&1

   # HTML tests
   dotnet test --filter "FullyQualifiedName~HtmlConsistencyTests" \
               --logger "console;verbosity=detailed" \
               > TEST_RESULTS_HTML.txt 2>&1

   # Cross-format tests
   dotnet test --filter "FullyQualifiedName~CrossFormatConsistencyTests" \
               --logger "console;verbosity=detailed" \
               > TEST_RESULTS_CROSSFORMAT.txt 2>&1

   # Line number integrity tests
   dotnet test --filter "FullyQualifiedName~LineNumberIntegrityTests" \
               --logger "console;verbosity=detailed" \
               > TEST_RESULTS_LINEINTEGRITY.txt 2>&1

   # Sample coverage tests
   dotnet test --filter "FullyQualifiedName~SampleCoverageTests" \
               --logger "console;verbosity=detailed" \
               > TEST_RESULTS_SAMPLECOVERAGE.txt 2>&1

   # External tool tests (may skip if tools unavailable)
   dotnet test --filter "FullyQualifiedName~ExternalToolCompatibilityTests" \
               --logger "console;verbosity=detailed" \
               > TEST_RESULTS_EXTERNALTOOLS.txt 2>&1
   ```

3. **Generate Test Summary**
   ```bash
   # Count total tests, passes, failures
   dotnet test --filter "Category=SampleValidation" --logger "trx"
   ```

**Deliverable:** 7 test result files capturing all output

#### Task B.2: Document Test Execution Results
**Time:** 3 hours

**Create: TEST_EXECUTION_REPORT.md**

**Structure:**
```markdown
# Test Execution Report - Sprint 4

## Executive Summary
- Total tests: X
- Passed: X (X%)
- Failed: X (X%)
- Skipped: X (X%)

## Test Results by Category

### JsonConsistencyTests
- Total: 6
- Passed: X
- Failed: X

[Detailed results for each test method]

### HtmlConsistencyTests
[...]

### CrossFormatConsistencyTests
[...]

### LineNumberIntegrityTests
[...]

### SampleCoverageTests
[...]

### ExternalToolCompatibilityTests
[...]

## Detailed Failure Analysis

### Failure 1: Json002_LineNumberIntegrity_NoOverlaps
**Status:** FAILED
**Sample File:** Calculator.cs
**Error Message:** [Full error message]
**Stack Trace:** [Relevant stack trace]
**TestResult Issues:**
- Line 10-15 (Method1) overlaps with Lines 14-20 (Method2)
- Line 25-30 (Property) overlaps with Lines 28-35 (Field)

**Root Cause Hypothesis:** [Analysis]
**Severity:** P0 / P1 / P2
**Assigned to Workstream C:** Yes/No

[Repeat for all failures]

## Patterns in Failures

### Pattern 1: Line Number Overlaps
- Affected Tests: Json002, Html002, LineIntegrity001
- Affected Samples: Calculator.cs
- Hypothesis: [Theory about root cause]

### Pattern 2: Format Inconsistencies
[...]

## Passing Tests Summary
[List of all passing tests with brief notes]

## Skipped Tests Summary
[List of skipped tests and reasons]
```

**Deliverable:** Comprehensive test execution report with all failures documented

#### Task B.3: Capture Generated Outputs for Analysis
**Time:** 1 hour

**Activities:**

1. **Preserve CLI Outputs**
   ```bash
   # Create output directory
   mkdir -p test-outputs/calculator
   mkdir -p test-outputs/userservice

   # Generate all formats for Calculator.cs
   roslyn-diff diff samples/before/Calculator.cs samples/after/Calculator.cs \
                --json test-outputs/calculator/output.json

   roslyn-diff diff samples/before/Calculator.cs samples/after/Calculator.cs \
                --html test-outputs/calculator/output.html

   roslyn-diff diff samples/before/Calculator.cs samples/after/Calculator.cs \
                --git > test-outputs/calculator/output.diff

   # Repeat for UserService.cs
   [...]
   ```

2. **Document Output Characteristics**
   - File sizes
   - Line counts
   - Change counts
   - Any anomalies noticed

**Deliverable:** test-outputs/ directory with all CLI outputs preserved

#### Task B.4: Create Bug Reports for Each Failure
**Time:** 4 hours

**For each distinct failure, create a bug report:**

**File: BUG_REPORT_001_Line_Number_Overlaps.md**

```markdown
# Bug Report #001: Line Number Overlaps in Calculator.cs JSON Output

## Summary
JSON output for Calculator.cs contains overlapping line number ranges in modified members.

## Severity
**P0** - Critical data integrity issue

## Affected Components
- JSON output formatter
- Line number calculation logic
- Possibly affects HTML and Text outputs too

## Steps to Reproduce
1. Run: roslyn-diff diff samples/before/Calculator.cs samples/after/Calculator.cs --json
2. Parse JSON changes array
3. Observe: Lines 10-15 and Lines 14-20 both marked as changes

## Expected Behavior
Line ranges should not overlap. Each line should appear in at most one change range.

## Actual Behavior
Line 14 and 15 appear in two different change ranges:
- Change 1 (Method1): Lines 10-15
- Change 2 (Method2): Lines 14-20

## Test Evidence
- Test: Json002_LineNumberIntegrity_NoOverlaps
- Status: FAILED
- Sample: Calculator.cs
- TestResult Issues: [Detailed issues list]

## Root Cause Analysis
[To be filled by Workstream C]

## Proposed Fix
[To be filled by Workstream C]

## Related Tests
- Html002_SectionLineNumberIntegrity_NoOverlaps (may also fail)
- LineIntegrity001_AllFormats_NoOverlaps (may also fail)
- Xfmt003_AllFormats_RoslynMode_Agreement (may be affected)

## Impact
- Affects accuracy of all diff outputs
- May confuse users about which lines changed
- HTML display may show duplicate content
```

**Create separate bug report for each distinct failure pattern**

**Deliverable:** Bug reports for all failures (estimate 3-8 bug reports)

#### Task B.5: Prioritize Bugs for Workstream C
**Time:** 2 hours

**Activities:**

1. **Categorize Bugs by Severity**
   - **P0 (Critical):** Data integrity issues, overlapping line numbers, format inconsistencies
   - **P1 (High):** Edge case failures, minor inconsistencies, performance issues
   - **P2 (Medium):** External tool compatibility issues, cosmetic issues

2. **Categorize Bugs by Component**
   - Line number calculation
   - JSON formatting
   - HTML rendering
   - Cross-format consistency
   - External tool compatibility

3. **Create Prioritized Bug List**

**File: BUG_PRIORITY_LIST.md**

```markdown
# Bug Priority List - Sprint 4

## P0 Bugs (Must Fix in Sprint 4)

1. **BUG-001: Line Number Overlaps in Calculator.cs**
   - Component: Line number calculation
   - Affected outputs: JSON, HTML, Text
   - Blocking tests: 5 tests
   - Est. fix time: 4 hours

2. **BUG-002: [Description]**
   [...]

## P1 Bugs (Should Fix in Sprint 4)

[...]

## P2 Bugs (Document for Future Work)

[...]

## Summary
- P0 bugs: X (must fix)
- P1 bugs: X (attempt to fix)
- P2 bugs: X (document only)
```

**Deliverable:** Prioritized bug list for Workstream C

### Workstream B Success Criteria

✅ All validation tests executed successfully (test run completes)
✅ Test results captured with detailed output
✅ All failures documented with error messages and stack traces
✅ Patterns identified in failures
✅ Bug reports created for each distinct failure
✅ Bugs prioritized by severity (P0/P1/P2)
✅ CLI outputs preserved for analysis
✅ Test execution report completed

### Workstream B Deliverables

```
/Users/randlee/Documents/github/roslyn-diff-worktrees/feature/sample-data-validation-tests/
├── TEST_EXECUTION_REPORT.md                   🆕 Comprehensive test results
├── TEST_RESULTS_RAW.txt                       🆕 Raw test output
├── TEST_RESULTS_JSON.txt                      🆕 JSON test output
├── TEST_RESULTS_HTML.txt                      🆕 HTML test output
├── TEST_RESULTS_CROSSFORMAT.txt               🆕 Cross-format test output
├── TEST_RESULTS_LINEINTEGRITY.txt             🆕 Line integrity test output
├── TEST_RESULTS_SAMPLECOVERAGE.txt            🆕 Sample coverage test output
├── TEST_RESULTS_EXTERNALTOOLS.txt             🆕 External tools test output
├── BUG_PRIORITY_LIST.md                       🆕 Prioritized bug list
├── BUG_REPORT_001_[Description].md            🆕 Individual bug reports
├── BUG_REPORT_002_[Description].md            🆕 (3-8 reports estimated)
└── test-outputs/                              🆕 Preserved CLI outputs
    ├── calculator/
    │   ├── output.json
    │   ├── output.html
    │   └── output.diff
    └── userservice/
        ├── output.json
        ├── output.html
        └── output.diff
```

---

## Workstream C: Bug Fixes Based on Test Failures

### Goal
Fix all P0 bugs discovered by validation tests, attempt P1 bug fixes, document P2 bugs for future work, and verify fixes by re-running tests.

### Duration
**2.5 days** (20 hours) - Iterative with Workstream B

### Prerequisites
- ✅ Workstream B complete (bug reports available)
- ✅ Bugs prioritized (P0/P1/P2)
- ✅ Test failures documented

### Tasks

#### Task C.1: Analyze P0 Bug Root Causes
**Time:** 4 hours

**For each P0 bug:**

1. **Reproduce the Issue**
   - Run roslyn-diff CLI manually with affected sample
   - Examine output (JSON/HTML/Text)
   - Confirm issue exists

2. **Trace Code Path**
   - Identify source code files involved
   - Review line number calculation logic
   - Review change detection logic
   - Review formatting logic

3. **Identify Root Cause**
   - Pinpoint exact line of code causing issue
   - Understand why it's happening
   - Determine if it's systemic or edge case

4. **Document Analysis**
   - Update bug report with root cause
   - Add code references
   - Propose fix approach

**Example Analysis:**

**BUG-001: Line Number Overlaps**

**Root Cause Analysis:**
```
File: src/RoslynDiff.Core/ChangeAnalyzer.cs
Line: 145-160

Issue: When calculating line ranges for modified members, the algorithm
includes both the original member's full span AND the modified member's
full span, causing overlaps when members are adjacent.

Code snippet:
```csharp
var oldSpan = oldMember.GetLocation().GetLineSpan();
var newSpan = newMember.GetLocation().GetLineSpan();
// Both spans are included without checking for overlaps
changes.Add(new Change {
    OldLocation = oldSpan,  // Lines 10-15
    NewLocation = newSpan   // Lines 14-20
});
```

Proposed Fix:
- Add overlap detection before adding changes
- Merge overlapping ranges
- Or: Only report changed lines, not full spans
```

**Deliverable:** Root cause analysis for all P0 bugs

#### Task C.2: Implement Fixes for P0 Bugs
**Time:** 8-12 hours (depends on bug count and complexity)

**For each P0 bug:**

1. **Create Fix Branch** (optional, or work directly in feature branch)
   ```bash
   # If multiple people working, create sub-branches
   git checkout -b fix/bug-001-line-overlaps
   ```

2. **Implement Fix**
   - Modify source code in `src/` directory
   - Add null checks, validation, etc.
   - Ensure fix is minimal and targeted

3. **Add Unit Tests for Fix** (if applicable)
   - Add regression test to appropriate unit test file
   - Verify fix with unit test

4. **Build and Verify**
   ```bash
   dotnet build
   # Ensure no compilation errors
   ```

5. **Document Fix**
   - Update bug report with fix details
   - Add code comments explaining fix
   - Update CHANGELOG if applicable

**Example Fix:**

**BUG-001 Fix:**
```csharp
// src/RoslynDiff.Core/ChangeAnalyzer.cs

// BEFORE:
changes.Add(new Change {
    OldLocation = oldSpan,
    NewLocation = newSpan
});

// AFTER:
// Check for overlaps before adding
var existingChange = changes.FirstOrDefault(c =>
    c.OldLocation.Overlaps(oldSpan) ||
    c.NewLocation.Overlaps(newSpan));

if (existingChange != null)
{
    // Merge ranges instead of creating duplicate
    existingChange.OldLocation = Merge(existingChange.OldLocation, oldSpan);
    existingChange.NewLocation = Merge(existingChange.NewLocation, newSpan);
}
else
{
    changes.Add(new Change {
        OldLocation = oldSpan,
        NewLocation = newSpan
    });
}
```

**Deliverable:** Source code changes fixing all P0 bugs

#### Task C.3: Re-run Tests to Verify Fixes
**Time:** 2 hours per iteration (may require 2-3 iterations)

**For each bug fix:**

1. **Rebuild CLI**
   ```bash
   dotnet build src/RoslynDiff.Cli/
   ```

2. **Re-run Affected Tests**
   ```bash
   # Re-run specific failing test
   dotnet test --filter "FullyQualifiedName~Json002_LineNumberIntegrity_NoOverlaps"
   ```

3. **Verify Fix**
   - Test should now pass
   - If still fails, iterate on fix

4. **Re-run All Validation Tests**
   ```bash
   # Ensure no regressions
   dotnet test --filter "Category=SampleValidation"
   ```

5. **Update Bug Report**
   - Mark as FIXED
   - Document verification results

**Deliverable:** Updated test results showing P0 bugs fixed

#### Task C.4: Attempt P1 Bug Fixes
**Time:** 4-6 hours (time permitting)

**If time allows after P0 bugs are fixed:**

1. Prioritize P1 bugs by impact
2. Attempt fixes for highest priority P1 bugs
3. Follow same process as P0 bugs
4. If time runs out, document findings and defer to future sprint

**Deliverable:** P1 bugs fixed (if time allows) or documented for future work

#### Task C.5: Document Remaining P2 Bugs
**Time:** 2 hours

**For P2 bugs not fixed in Sprint 4:**

1. **Create GitHub Issues** (or equivalent)
   - One issue per bug
   - Include bug report details
   - Tag with "validation-testing", "p2", etc.

2. **Update BUG_PRIORITY_LIST.md**
   - Mark which bugs were fixed
   - Mark which bugs are deferred
   - Link to GitHub issues

3. **Create Future Work Document**

**File: VALIDATION_TESTING_FUTURE_WORK.md**

```markdown
# Validation Testing - Future Work

## P2 Bugs to Address

### BUG-005: External Tool Timestamp Format Differences
- Status: Deferred to future sprint
- Reason: Low priority, doesn't affect functionality
- Workaround: Normalize timestamps in comparison
- GitHub Issue: #123

[...]

## Test Coverage Gaps

### Missing Test Scenarios
- Empty file to empty file diff
- Very large files (>10K lines)
- Unicode content
- Files with syntax errors

### Missing Sample Files
- VB.NET samples
- Complex nested class structures
- Multiple namespaces

## Performance Optimization Opportunities
[...]

## External Tool Integration Enhancements
[...]
```

**Deliverable:** Future work documented for deferred bugs

#### Task C.6: Final Validation Test Run
**Time:** 2 hours

**After all fixes implemented:**

1. **Clean Build**
   ```bash
   dotnet clean
   dotnet build
   ```

2. **Run Complete Test Suite**
   ```bash
   dotnet test
   ```

3. **Run Validation Tests**
   ```bash
   dotnet test --filter "Category=SampleValidation" \
               --logger "console;verbosity=detailed" \
               > TEST_RESULTS_FINAL.txt 2>&1
   ```

4. **Generate Final Report**

**File: FINAL_VALIDATION_RESULTS.md**

```markdown
# Final Validation Results - Sprint 4

## Executive Summary
- Total validation tests: X
- Passed: X (X%)
- Failed: X (X%)
- Skipped: X (X%)

## Comparison to Initial Run

| Metric | Initial (Workstream B) | Final (After Fixes) | Improvement |
|--------|------------------------|---------------------|-------------|
| Total Tests | 30 | 30 | - |
| Passed | 12 | 28 | +133% |
| Failed | 18 | 2 | -89% |
| Pass Rate | 40% | 93% | +53% |

## Bugs Fixed
- BUG-001: Line Number Overlaps - ✅ FIXED
- BUG-002: [Description] - ✅ FIXED
- BUG-003: [Description] - ✅ FIXED

## Remaining Failures
- BUG-007: [P2 issue] - Deferred to future sprint
- BUG-008: [P2 issue] - Deferred to future sprint

## Test Results by Category
[Detailed breakdown]

## Conclusion
Sprint 4 successfully implemented validation testing framework and fixed
X critical bugs. Remaining P2 bugs documented for future work.
```

**Deliverable:** Final validation results with comparison to initial run

### Workstream C Success Criteria

✅ All P0 bugs analyzed and root causes identified
✅ All P0 bugs fixed with source code changes
✅ Fixes verified by re-running tests
✅ P0 test failures reduced to zero (or documented if unfixable)
✅ P1 bugs attempted (if time allows)
✅ P2 bugs documented for future work
✅ Final test run shows significant improvement
✅ Bug reports updated with fix details
✅ Future work documented

### Workstream C Deliverables

```
/Users/randlee/Documents/github/roslyn-diff-worktrees/feature/sample-data-validation-tests/
├── FINAL_VALIDATION_RESULTS.md                🆕 Final test results
├── VALIDATION_TESTING_FUTURE_WORK.md          🆕 Deferred work documentation
├── TEST_RESULTS_FINAL.txt                     🆕 Final test output
├── BUG_REPORT_001_[Description].md            📝 Updated with fix details
├── BUG_REPORT_002_[Description].md            📝 Updated with fix details
└── BUG_PRIORITY_LIST.md                       📝 Updated with fix status

src/RoslynDiff.Core/                            📝 Source code fixes
src/RoslynDiff.Output/                          📝 Source code fixes
(Various source files with bug fixes)
```

---

## Sprint 4 Success Criteria

### Must Have (P0) - Sprint 4 CANNOT be considered complete without these:

✅ **All integration test classes created**
- 6 test class files exist as .cs files on disk
- Minimum 30 test methods total
- All tests compile without errors

✅ **All integration tests executable**
- Tests are discoverable by `dotnet test --list-tests`
- Tests can be run with `dotnet test --filter "Category=SampleValidation"`
- No xUnit framework errors

✅ **All validation tests executed at least once**
- Test run completes successfully
- Results captured in TEST_EXECUTION_REPORT.md
- All failures documented

✅ **Test results documented**
- Pass/fail status for each test
- Error messages for all failures
- Bug reports created for distinct failures

✅ **Critical bugs identified and prioritized**
- P0 bugs clearly identified
- Root causes analyzed
- Bug reports created

✅ **Critical bugs fixed**
- All P0 bugs have attempted fixes
- Fixes verified by re-running tests
- If unfixable, documented with reasons

### Should Have (P1) - Important but Sprint 4 can succeed without:

⚠️ **High pass rate on validation tests**
- Target: 80%+ tests passing after bug fixes
- Acceptable: 60%+ tests passing if remaining failures are P2

⚠️ **P1 bugs addressed**
- P1 bugs attempted if time allows
- Documented for future work if not fixed

⚠️ **External tool tests implemented**
- ExternalToolCompatibilityTests.cs created
- Tests run (may skip if tools unavailable)

⚠️ **Test execution in CI**
- Validation tests run in CI pipeline
- Results published as artifacts

### Nice to Have (P2) - Valuable but not required for Sprint 4 success:

○ **100% test pass rate**
- All tests passing
- Zero P2 bugs

○ **Performance optimizations**
- Tests run faster than 60 seconds total

○ **Visual diff reports**
- HTML reports generated from failures

---

## Estimated Effort

### Time Breakdown by Workstream

| Workstream | Duration | Tasks | Deliverables |
|------------|----------|-------|--------------|
| **A: Create Test Classes** | 2 days (16 hours) | 7 tasks | 6 test class files, 30+ tests |
| **B: Execute & Document** | 1.5 days (12 hours) | 5 tasks | Test results, bug reports |
| **C: Fix Bugs** | 2.5 days (20 hours) | 6 tasks | Bug fixes, final validation |
| **TOTAL** | **6 days (48 hours)** | **18 tasks** | **Multiple deliverables** |

### Parallelization Opportunities

- **Days 1-2:** Workstream A (single developer focus)
- **Days 2-3:** Workstream B (can start when tests compile)
- **Days 3-5:** Workstream C (iterative with B)

**If multiple developers available:**
- Developer 1: Workstream A → Workstream C
- Developer 2: Workstream B → Workstream C (pair on bug fixes)

### Risk Contingency

- **If more P0 bugs than expected:** Extend Workstream C by 1 day
- **If tests don't compile:** Add 4 hours to Workstream A
- **If external tools unavailable:** Skip ExternalToolCompatibilityTests (saves 3 hours)

---

## Sprint 4 Workflow Summary

### Day-by-Day Plan

#### Day 1: Create Test Classes (Workstream A)
**Focus:** JsonConsistencyTests, HtmlConsistencyTests

**Deliverables:**
- JsonConsistencyTests.cs (6+ tests)
- HtmlConsistencyTests.cs (5+ tests)
- Both compile successfully

**End of Day Check:**
```bash
dotnet build
dotnet test --list-tests --filter "FullyQualifiedName~JsonConsistencyTests"
# Should list 6+ tests
```

#### Day 2: Complete Test Classes & Start Execution (Workstreams A + B)
**Focus:** Finish test creation, start running tests

**Morning (Workstream A):**
- Create CrossFormatConsistencyTests.cs
- Create LineNumberIntegrityTests.cs
- Create SampleCoverageTests.cs
- Verify all compile

**Afternoon (Workstream B):**
- Run all validation tests
- Capture results
- Start documenting failures

**End of Day Check:**
```bash
dotnet test --filter "Category=SampleValidation"
# Should show test run results
```

#### Day 3: Document Results & Analyze Bugs (Workstreams B + C)
**Focus:** Complete failure documentation, start bug fixes

**Morning (Workstream B):**
- Complete TEST_EXECUTION_REPORT.md
- Create bug reports for all failures
- Prioritize bugs (P0/P1/P2)

**Afternoon (Workstream C):**
- Analyze P0 bug root causes
- Start implementing fixes for first P0 bug

**End of Day Check:**
- BUG_PRIORITY_LIST.md complete
- At least 1 P0 bug fix attempted

#### Day 4: Fix P0 Bugs (Workstream C)
**Focus:** Implement and verify bug fixes

**Activities:**
- Continue P0 bug fixes
- Re-run tests after each fix
- Iterate on fixes that don't work
- Update bug reports with fix details

**End of Day Check:**
```bash
dotnet test --filter "Category=SampleValidation"
# Should show improved pass rate
```

#### Day 5: Final Validation & Wrap-Up (Workstream C)
**Focus:** Complete remaining fixes, final validation, documentation

**Morning:**
- Finish any remaining P0 fixes
- Attempt P1 fixes if time allows
- Document P2 bugs for future work

**Afternoon:**
- Final validation test run
- Create FINAL_VALIDATION_RESULTS.md
- Create VALIDATION_TESTING_FUTURE_WORK.md
- Review all deliverables

**End of Day Check:**
- All P0 bugs fixed or documented
- Final test results show significant improvement
- All documentation complete

---

## Deliverables Checklist

### Workstream A Deliverables

- [ ] JsonConsistencyTests.cs (6+ tests)
- [ ] HtmlConsistencyTests.cs (5+ tests)
- [ ] CrossFormatConsistencyTests.cs (5+ tests)
- [ ] LineNumberIntegrityTests.cs (5+ tests)
- [ ] SampleCoverageTests.cs (4+ tests)
- [ ] ExternalToolCompatibilityTests.cs (3+ tests) [Optional P1]
- [ ] All tests compile (dotnet build succeeds)
- [ ] All tests discoverable (dotnet test --list-tests works)

### Workstream B Deliverables

- [ ] TEST_EXECUTION_REPORT.md
- [ ] TEST_RESULTS_RAW.txt
- [ ] TEST_RESULTS_JSON.txt
- [ ] TEST_RESULTS_HTML.txt
- [ ] TEST_RESULTS_CROSSFORMAT.txt
- [ ] TEST_RESULTS_LINEINTEGRITY.txt
- [ ] TEST_RESULTS_SAMPLECOVERAGE.txt
- [ ] TEST_RESULTS_EXTERNALTOOLS.txt
- [ ] BUG_PRIORITY_LIST.md
- [ ] BUG_REPORT_001_[Description].md (for each bug)
- [ ] test-outputs/ directory with CLI outputs

### Workstream C Deliverables

- [ ] Source code fixes for P0 bugs
- [ ] Updated bug reports with fix details
- [ ] TEST_RESULTS_FINAL.txt
- [ ] FINAL_VALIDATION_RESULTS.md
- [ ] VALIDATION_TESTING_FUTURE_WORK.md
- [ ] GitHub issues for deferred bugs
- [ ] All P0 bugs fixed or documented as unfixable

---

## Risk Management

### Identified Risks

#### Risk 1: More P0 Bugs Than Expected
**Probability:** High
**Impact:** High (timeline extension)
**Mitigation:**
- Prioritize bugs strictly (only fix critical issues)
- Document lower priority bugs for future sprints
- Accept 60%+ pass rate instead of 100%

#### Risk 2: Tests Don't Compile
**Probability:** Low
**Impact:** High (blocks entire sprint)
**Mitigation:**
- Start with 1-2 test methods per class, verify compilation
- Incrementally add more tests
- Use existing test patterns from Integration.Tests

#### Risk 3: No Bugs Discovered (Tests All Pass)
**Probability:** Low
**Impact:** Medium (less work, but less validation)
**Response:**
- Celebrate! Document success
- Add more edge case tests
- Move to P1 external tool testing

#### Risk 4: Unfixable P0 Bugs
**Probability:** Low
**Impact:** High (can't achieve 100% pass rate)
**Mitigation:**
- Document why unfixable
- Create design discussion for future fix
- Accept partial success if architectural change needed

#### Risk 5: External Tools Unavailable
**Probability:** Medium
**Impact:** Low (only affects P1 tests)
**Response:**
- Tests should skip gracefully
- Document tool requirements
- Test in environments where tools available

---

## Definition of Done

### Sprint 4 is DONE when:

✅ All 6 integration test class files exist on disk as .cs files
✅ Minimum 30 test methods implemented across all classes
✅ All tests compile with zero errors
✅ All tests discoverable and executable via dotnet test
✅ All tests executed at least once
✅ Test execution results documented in TEST_EXECUTION_REPORT.md
✅ All failures analyzed and documented as bug reports
✅ All P0 bugs have fix attempts (source code changes)
✅ Fixes verified by re-running tests
✅ Final validation results show improvement over initial run
✅ Remaining bugs documented for future work
✅ All deliverables committed to feature branch

### Optional (P1) - Sprint 4 is EXCELLENT when:

✅ 80%+ test pass rate achieved
✅ External tool compatibility tests implemented and passing
✅ P1 bugs fixed
✅ Tests integrated into CI pipeline

---

## Next Steps After Sprint 4

### Integration into CI/CD

Add to `.github/workflows/` or equivalent:

```yaml
- name: Run Validation Tests
  run: |
    dotnet test --filter "Category=SampleValidation" \
                --logger "trx;LogFileName=validation-results.trx"

- name: Upload Validation Results
  if: always()
  uses: actions/upload-artifact@v3
  with:
    name: validation-test-results
    path: '**/validation-results.trx'
```

### Future Enhancements

1. **Add More Sample Files**
   - VB.NET samples
   - Edge cases (empty files, large files, etc.)
   - Complex scenarios (multiple namespaces, nested classes)

2. **Expand Test Coverage**
   - More flag combinations
   - Performance tests
   - Stress tests with large files

3. **Visual Reporting**
   - HTML test reports
   - Trend analysis
   - Coverage visualization

4. **Automation**
   - Nightly validation runs
   - Regression testing on PRs
   - Performance tracking over time

---

## Appendix: Command Reference

### Quick Commands

```bash
# Navigate to worktree
cd /Users/randlee/Documents/github/roslyn-diff-worktrees/feature/sample-data-validation-tests

# Build solution
dotnet build

# List validation tests
dotnet test --list-tests --filter "Category=SampleValidation"

# Run all validation tests
dotnet test --filter "Category=SampleValidation"

# Run specific test class
dotnet test --filter "FullyQualifiedName~JsonConsistencyTests"

# Run with detailed output
dotnet test --filter "Category=SampleValidation" \
            --logger "console;verbosity=detailed"

# Generate test coverage
dotnet test --collect:"XPlat Code Coverage" \
            --filter "Category=SampleValidation"

# Run specific test method
dotnet test --filter "FullyQualifiedName~Json002_LineNumberIntegrity_NoOverlaps"

# Check roslyn-diff CLI
dotnet run --project src/RoslynDiff.Cli/ -- diff --help
```

### Sample File Commands

```bash
# Test Calculator.cs manually
dotnet run --project src/RoslynDiff.Cli/ -- diff \
  samples/before/Calculator.cs samples/after/Calculator.cs --json

# Test with HTML output
dotnet run --project src/RoslynDiff.Cli/ -- diff \
  samples/before/Calculator.cs samples/after/Calculator.cs \
  --html test-output.html

# Test with all formats
dotnet run --project src/RoslynDiff.Cli/ -- diff \
  samples/before/UserService.cs samples/after/UserService.cs \
  --json output.json --html output.html
```

---

## Sprint 4 Sign-Off

**Sprint 4 Start Date:** [To be filled]
**Sprint 4 End Date:** [To be filled]
**Sprint Lead:** [To be filled]

**Workstream Owners:**
- Workstream A (Create Tests): [Name]
- Workstream B (Execute & Document): [Name]
- Workstream C (Fix Bugs): [Name]

**Success Metrics:**
- [ ] All 6 test classes created (30+ tests)
- [ ] All tests executed and documented
- [ ] All P0 bugs fixed or documented
- [ ] Final pass rate: ___% (target: 60%+)

**Retrospective Notes:**
[To be filled after sprint completion]

---

**End of Sprint 4 Plan**
