# Bug Priority List - Sprint 4 Sample Data Validation

**Report Date:** 2026-01-17
**Test Execution Summary:** 34 tests, 10 passed (29.4%), 24 failed (70.6%)
**Critical Finding:** Single P0 bug blocks 70.6% of all tests

## Executive Summary

- **Total bugs discovered:** 2 confirmed, 0-5 predicted
- **P0 bugs (must fix):** 2
- **P1 bugs (should fix):** 0 (pending discovery after P0 fixes)
- **P2 bugs (nice to have):** 0 (pending discovery after P0 fixes)

**Status:** Ready for Agent C (Bug Fixer) to begin work

## P0 Bugs - MUST FIX in Sprint 4

### BUG-001: CLI Command Syntax Error in SampleDataValidator ⚠️ BLOCKING
- **Severity:** P0 - CRITICAL
- **Status:** CONFIRMED, NOT FIXED
- **Component:** RoslynDiff.TestUtilities/Validators/SampleDataValidator.cs
- **Lines affected:** 500-501
- **Affected tests:** 24 tests (70.6% of suite)
- **Affected outputs:** All formats (JSON, HTML, Text, Git)
- **Blocking tests:**
  - 5 JsonConsistencyTests
  - 4 HtmlConsistencyTests
  - 4 CrossFormatConsistencyTests
  - 4 LineNumberIntegrityTests
  - 4 SampleCoverageTests
  - 3 ExternalToolCompatibilityTests
- **Est. fix time:** 35 minutes (15 min code + 15 min test + 5 min doc)
- **Fix complexity:** VERY LOW (2-line code change)
- **File:** `BUG_REPORT_001_CLI_Command_Syntax_Error.md`

**Impact:** Blocks all validation tests that require CLI output generation. Without fixing this, no meaningful validation can occur.

**Root Cause:** Validator uses incorrect CLI command `file` instead of `diff`, and incorrect flag format `--output {format}` instead of `--{format}`.

**Fix Required:**
```diff
- args.Append($"file \"{oldFile}\" \"{newFile}\"");
+ args.Append($"diff \"{oldFile}\" \"{newFile}\"");

- args.Append($" --output {format}");
- args.Append($" --out-file \"{outputFile}\"");
+ args.Append($" --{format.ToLowerInvariant()} \"{outputFile}\"");
```

**Note:** The CLI uses path as argument to format flag (e.g., `--json <path>`), not a separate `--out-file` flag.

**Verification Steps:**
1. Apply fix to SampleDataValidator.cs
2. Run: `dotnet test --filter "FullyQualifiedName~Json004_Calculator"`
3. Verify: Test passes or fails with different (more specific) error
4. Run: `dotnet test --filter "Category=SampleValidation"`
5. Verify: Pass rate increases from 29.4% to ≥ 60%

**Post-Fix Actions:**
After fixing BUG-001, BUG-002 will immediately surface. Must fix BUG-002 before tests can pass.

### BUG-002: CLI Returns Exit Code 1 When Successfully Writing Files ⚠️ BLOCKING
- **Severity:** P0 - CRITICAL
- **Status:** CONFIRMED, NOT FIXED (Currently masked by BUG-001)
- **Component:** RoslynDiff.TestUtilities/Validators/SampleDataValidator.cs (or CLI)
- **Lines affected:** 530
- **Affected tests:** 24 tests (same as BUG-001)
- **Root Cause:** CLI returns exit code 1 when differences are found (standard diff behavior), but validator treats any non-zero exit code as failure
- **Est. fix time:** 20 minutes (5 min investigate + 5 min code + 10 min test)
- **Fix complexity:** VERY LOW (single-line code change)
- **File:** `BUG_REPORT_002_CLI_Exit_Code_When_Writing_Files.md`

**Impact:** After BUG-001 is fixed, this bug will prevent all tests from passing because the validator incorrectly interprets exit code 1 as an error.

**Fix Required (Recommended Option 2):**
```diff
- if (process.ExitCode != 0)
+ // Exit codes: 0 = identical, 1 = differences found, 2+ = error
+ if (process.ExitCode > 1)
{
    var error = process.StandardError.ReadToEnd();
    throw new InvalidOperationException($"CLI invocation failed with exit code {process.ExitCode}: {error}");
}
```

**Rationale:** Standard diff tools return 1 when differences exist, 0 when identical. This is correct behavior. Validator should accept 0 or 1 as success.

**Verification Steps:**
1. Apply fix after BUG-001 is fixed
2. Run: `dotnet test --filter "FullyQualifiedName~Json004_Calculator"`
3. Verify: Test passes (or fails with actual validation error, not exit code error)
4. Run: `dotnet test --filter "Category=SampleValidation"`
5. Verify: Pass rate increases to ≥ 60%

**Alternative:** If investigation reveals CLI should return 0, fix CLI instead of validator.

## P1 Bugs - SHOULD FIX in Sprint 4 (if time permits)

**Status:** No P1 bugs discovered yet. The following issues are **predicted** to surface after BUG-001 is fixed:

### Predicted Issue: Line Number Overlaps
- **Likelihood:** MEDIUM
- **Evidence:** Tests Json002, Html002, LineIntegrity001 passed with existing files, but may fail with fresh CLI output
- **Hypothesis:** CLI may generate overlapping line ranges when reporting changes to nested code structures
- **Tests that may fail:** LineIntegrity003, LineIntegrity004
- **Will create bug report:** BUG-002 (if confirmed after BUG-001 fix)

### Predicted Issue: Line Number Duplicates
- **Likelihood:** LOW
- **Evidence:** Tests Json003, LineIntegrity002 passed with existing files
- **Hypothesis:** CLI may duplicate line numbers when reporting changes at different nesting levels
- **Tests that may fail:** LineIntegrity005, LineIntegrity006
- **Will create bug report:** BUG-003 (if confirmed after BUG-001 fix)

### Predicted Issue: Cross-Format Inconsistencies
- **Likelihood:** MEDIUM
- **Evidence:** Tests Xfmt001, Xfmt003 passed with existing files
- **Hypothesis:** Different output formats may report different line ranges for the same changes
- **Tests that may fail:** Xfmt002, Xfmt004, Xfmt005, Xfmt006
- **Will create bug report:** BUG-004 (if confirmed after BUG-001 fix)

### Predicted Issue: HTML Parsing Failures
- **Likelihood:** LOW
- **Evidence:** Tests Html002, Html003 passed, indicating HTML structure is valid
- **Hypothesis:** Fresh HTML output may have minor parsing issues
- **Tests that may fail:** Html006
- **Will create bug report:** BUG-005 (if confirmed after BUG-001 fix)

### Predicted Issue: External Tool Compatibility
- **Likelihood:** MEDIUM
- **Evidence:** Test Ext002 passed, showing git-diff compatibility works
- **Hypothesis:** Standard diff and unified diff formats may have minor incompatibilities
- **Tests that may fail:** Ext001, Ext004
- **Will create bug report:** BUG-006 (if confirmed after BUG-001 fix)

### Predicted Issue: --out-file Flag Not Supported (CONFIRMED as part of BUG-001)
- **Likelihood:** ~~HIGH~~ **CONFIRMED**
- **Evidence:** CLI help shows format flags accept path argument: `--json [PATH]`, not `--out-file`
- **Resolution:** Fixed as part of BUG-001 - use `--{format} <path>` instead of `--output {format} --out-file <path>`
- **Tests affected:** Included in BUG-001 fix
- **Status:** Will be resolved by BUG-001 fix

**Note:** These are predictions based on test design. Actual bugs will be documented after BUG-001 is fixed and tests re-run.

## P2 Bugs - DOCUMENT for Future Work

**Status:** No P2 bugs discovered yet.

P2 bugs are minor issues, edge cases, or cosmetic problems that don't affect core functionality. These will be documented after P0 and P1 bugs are addressed.

## Bug Clustering Analysis

### Cluster 1: CLI Invocation Issues (CONFIRMED - 2 bugs)
- **BUG-001:** CLI command syntax error
- **BUG-002:** CLI exit code handling
- **Common root cause:** Validator's CLI invocation logic has incorrect assumptions about command syntax and exit code semantics
- **Fix strategy:** Fix BUG-001 first (command syntax), then BUG-002 (exit code handling). Both required to unblock tests.
- **Total fix time:** 55 minutes (35 min + 20 min)

### Cluster 2: Line Number Integrity (PREDICTED)
- **Predicted BUG-003:** Line number overlaps
- **Predicted BUG-004:** Line number duplicates
- **Common root cause:** Line range calculation in diff engine
- **Fix strategy:** Single fix may resolve both issues if they're related

### Cluster 3: Cross-Format Consistency (PREDICTED)
- **Predicted BUG-005:** Format inconsistencies
- **Common root cause:** Different formatters may use different line range interpretations
- **Fix strategy:** Standardize line range calculation before formatting

### Cluster 4: External Tool Compatibility (PREDICTED)
- **Predicted BUG-006:** HTML parsing issues
- **Predicted BUG-007:** External diff compatibility
- **Common root cause:** Output format edge cases
- **Fix strategy:** Address on case-by-case basis

## Recommended Fix Order

### Phase 1: Unblock Test Suite (CRITICAL - 1.5 hours)
1. **BUG-001** - Fix CLI command syntax (35 minutes)
   - Change `file` to `diff`
   - Fix `--output {format} --out-file` to `--{format} <path>`
   - Verify with manual CLI tests
   - Run single test to verify

2. **BUG-002** - Fix CLI exit code handling (20 minutes)
   - Change exit code check from `!= 0` to `> 1`
   - Add comment explaining exit code semantics
   - Verify with manual CLI tests
   - Run single test to verify

3. **Re-run full test suite** (10 minutes)
   - Execute: `dotnet test --filter "Category=SampleValidation"`
   - Capture new results
   - Document new pass/fail counts

4. **Analyze new failures** (15 minutes)
   - Identify distinct failure patterns
   - Categorize by root cause
   - Create bug reports for each

**Expected outcome after Phase 1:** Test pass rate 60-80% (20-27 tests passing)

### Phase 2: Address Secondary Issues (RECOMMENDED - 3-6 hours)
After Phase 1, prioritize by:
1. Number of tests affected (fix bugs blocking most tests first)
2. Severity of data integrity impact
3. Complexity of fix (easy wins first if equal impact)

Likely order (to be confirmed after Phase 1):
1. BUG-003 (line overlaps) - May affect 10+ tests
2. BUG-005 (cross-format issues) - May affect 8+ tests
3. BUG-007 (external tool compatibility) - May affect 3+ tests
4. BUG-004 (line duplicates) - May affect 2+ tests
5. BUG-006 (HTML parsing) - May affect 1+ tests

### Phase 3: Achieve Sprint Goals (FINAL - 1-2 hours)
- Target: 80%+ test pass rate (27+ tests passing)
- Minimum: 60%+ test pass rate (20+ tests passing)
- Document any remaining issues as P2 (future work)

## Risk Assessment

### High Risk Bugs (complex fixes)
**None identified yet.**

Potential high-risk scenarios (after BUG-001 fix):
- If line number calculation has fundamental design issues
- If multiple formatters need refactoring for consistency
- If external tool compatibility requires CLI architecture changes

### Low Risk Bugs (straightforward fixes)
**BUG-001:** CLI command syntax error
- **Why low risk:** Isolated to test utilities
- **Why straightforward:** 3-line code change (command + flag format + path argument)
- **Why high confidence:** Manual CLI testing confirms correct syntax works

**BUG-002:** CLI exit code handling
- **Why low risk:** Isolated to test utilities, standard diff behavior
- **Why straightforward:** Single-line code change
- **Why high confidence:** Well-understood Unix exit code conventions

## Success Metrics for Agent C

### Minimum Success Criteria (Must Achieve)
- ✅ Fix BUG-001 (CLI command syntax)
- ✅ Fix BUG-002 (CLI exit code handling)
- ✅ Test pass rate ≥ 60% (20+ tests passing)
- ✅ All test categories have at least 1 passing test
- ✅ No regression (10 previously passing tests still pass)

### Target Success Criteria (Should Achieve)
- 🎯 Fix all P0 bugs (currently 2 confirmed, may discover more)
- 🎯 Fix 50%+ of P1 bugs discovered
- 🎯 Test pass rate ≥ 80% (27+ tests passing)
- 🎯 Document all remaining issues with bug reports

### Stretch Success Criteria (Ideal)
- 🌟 Fix all P0 and P1 bugs
- 🌟 Test pass rate ≥ 90% (30+ tests passing)
- 🌟 All 6 test categories have ≥ 50% pass rate
- 🌟 Complete validation suite ready for CI/CD integration

## Test Pass Rate Projections

### Current State (Before Any Fixes)
- Total: 34 tests
- Passed: 10 (29.4%)
- Failed: 24 (70.6%)
- Status: **BLOCKED - Cannot validate**

### After BUG-001 Fix (Projected)
- Total: 34 tests
- Passed: 20-25 (60-75%) - **Optimistic projection**
- Passed: 15-20 (45-60%) - **Realistic projection**
- Passed: 10-15 (30-45%) - **Pessimistic projection** (if many secondary issues)
- Status: **UNBLOCKED - Can identify real issues**

### After All P0 Fixes (Target)
- Total: 34 tests
- Passed: 27+ (80%+)
- Failed: 7 or fewer
- Status: **FUNCTIONAL - Ready for integration**

### After All P0 + P1 Fixes (Ideal)
- Total: 34 tests
- Passed: 30+ (90%+)
- Failed: 4 or fewer (P2 issues only)
- Status: **PRODUCTION READY**

## Testing Strategy for Agent C

### After Each Bug Fix
1. **Run affected tests individually** - Verify fix works
2. **Run affected test class** - Check for regressions within category
3. **Run full validation suite** - Check for regressions across categories
4. **Document results** - Update bug report with FIXED status

### After All Fixes
1. **Generate test summary** - Final pass/fail counts
2. **Generate TRX report** - Formal test results
3. **Update bug reports** - Mark all as FIXED or WONTFIX
4. **Create summary document** - Overall Sprint 4 results

## Dependencies and Blockers

### Current Blockers
- **BUG-001 blocks everything** - Must fix first
- **BUG-002 blocks everything after BUG-001** - Must fix second
- **Cannot discover other bugs until both BUG-001 and BUG-002 are fixed**

### No External Dependencies
- All sample files exist and are valid
- All test code compiles successfully
- CLI tool is built and functional
- Test framework is working correctly

### Post-Fix Dependencies
After BUG-001 and BUG-002 are fixed, may discover:
- Diff engine bugs (line number calculation)
- Formatter bugs (output generation)
- External tool compatibility issues
- None of these are external dependencies - all fixable in this codebase

## Communication Plan

### Agent C Should Report
1. **After fixing BUG-001 and BUG-002** (expected: 1-1.5 hours)
   - Confirm both fixes applied
   - Report new pass rate (target: 60%+)
   - List new bugs discovered (if any)

2. **After fixing each subsequent bug** (expected: every 1-2 hours)
   - Report bug ID and status
   - Report current pass rate
   - Estimate remaining work

3. **Final report** (expected: end of Sprint 4)
   - Total bugs fixed
   - Final pass rate
   - List of remaining P2 issues
   - Recommendations for future work

### Escalation Criteria
Agent C should escalate if:
- BUG-001 and BUG-002 fixes don't improve pass rate at all
- More than 10 distinct bugs are discovered (indicates systemic issues)
- Pass rate remains below 50% after fixing all discovered P0 bugs
- Any fix requires architecture changes (beyond Sprint 4 scope)

## Appendix: Test Category Analysis

### Tests That Work (Don't Require CLI Generation)
These 10 tests validate existing files or perform file system operations:

**JsonConsistencyTests:**
- Json002_LineNumberIntegrity_NoOverlaps ✅
- Json003_LineNumberIntegrity_NoDuplicates ✅

**HtmlConsistencyTests:**
- Html002_SectionLineNumberIntegrity_NoOverlaps ✅
- Html003_DataAttributeConsistency_MatchVisualDisplay ✅

**CrossFormatConsistencyTests:**
- Xfmt001_JsonVsHtml_LineNumbersMatch ✅
- Xfmt003_AllFormats_RoslynMode_Agreement ✅

**LineNumberIntegrityTests:**
- LineIntegrity001_AllFormats_NoOverlaps ✅
- LineIntegrity002_AllFormats_NoDuplicates ✅

**SampleCoverageTests:**
- Samp004_SampleCount_MatchesExpected ✅

**ExternalToolCompatibilityTests:**
- Ext002_RoslynDiffGit_VsGitDiff ✅

### Tests Blocked by BUG-001 (All 24 Failed Tests)
All tests that call `SampleDataValidator.GenerateOutput()` are blocked.

See BUG-001 report for complete list.

## Conclusion

Sprint 4 bug discovery has been **highly effective**:
1. ✅ Identified 2 critical blocking bugs (BUG-001, BUG-002)
2. ✅ Confirmed bugs affect 70.6% of tests (24 out of 34)
3. ✅ Documented clear, low-complexity fix paths for both
4. ✅ Established baseline metrics and success criteria
5. ✅ Prepared comprehensive bug reports with verification steps

**Next Steps:** Agent C should begin with BUG-001 fix, then immediately fix BUG-002. Estimated time to unblock suite: **1-1.5 hours** (55 min fixes + test runs). Estimated time to reach 80% pass rate: **4-8 hours** (depending on secondary issues discovered).

**Sprint 4 Success Probability:** HIGH - Both blocking bugs have straightforward fixes, and test infrastructure is sound. Expected 60-80% pass rate after Phase 1 fixes.
