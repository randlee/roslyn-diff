# Sample Data Validation Testing Strategy

**Document Version:** 1.0
**Created:** 2026-01-17
**Status:** Draft - Pending Review
**Worktree Branch:** `feature/sample-data-validation-tests` (based off `develop`)

## Executive Summary

This document outlines a comprehensive testing strategy to validate sample data consistency across all output formats (JSON, HTML, Text, Git-style) and diff modes (Roslyn semantic, Line-by-line). The goal is to ensure that:

1. All output formats contain identical diff information within the same mode
2. Line numbers are consistent and non-overlapping across change sections
3. External tool compatibility is verified for line-by-line mode
4. Sample data is fully covered by automated tests

---

## 1. Problem Statement

### Current Gaps Identified

1. **No cross-format validation** - JSON, HTML, and Text outputs are tested independently but not verified to contain the same information
2. **No line number integrity checks** - No tests verify that reported line numbers don't overlap or duplicate within change sections
3. **No external tool comparison** - Line-diff output is not compared against standard `diff` tools
4. **Sample files not systematically tested** - Files in `samples/` are demonstration files but not part of automated test suite

### Risk Areas

- Output formats could drift and report different line numbers
- HTML panels could show overlapping/duplicate content (recently fixed but needs regression tests)
- External tool compatibility claims are not verified

---

## 2. Test Architecture

### 2.1 Test Utility Helper Class

**Location:** `tests/RoslynDiff.TestUtilities/SampleDataValidator.cs`

```
┌─────────────────────────────────────────────────────────────┐
│                   SampleDataValidator                        │
├─────────────────────────────────────────────────────────────┤
│ Core Methods:                                                │
│   - ValidateAll(oldFile, newFile) → IEnumerable<TestResult> │
│   - ValidateJsonConsistency(...) → IEnumerable<TestResult>  │
│   - ValidateHtmlConsistency(...) → IEnumerable<TestResult>  │
│   - ValidateLineNumberIntegrity(...) → IEnumerable<TestResult>│
│   - ValidateCrossFormatConsistency(...) → IEnumerable<TestResult>│
│   - ValidateExternalToolCompatibility(...) → IEnumerable<TestResult>│
├─────────────────────────────────────────────────────────────┤
│ Options:                                                     │
│   - IgnoreTimestamps: bool                                   │
│   - DiffMode: Roslyn | Line | Both                          │
│   - IncludeExternalTools: bool                              │
└─────────────────────────────────────────────────────────────┘
```

### 2.2 Test Result Model

```csharp
public record TestResult
{
    public string TestName { get; init; }
    public bool Passed { get; init; }
    public string? Message { get; init; }
    public string? Details { get; init; }
    public string? SourceFile { get; init; }
    public int? LineNumber { get; init; }
}
```

### 2.3 Test Data Discovery

**Attribute-based Test Case Discovery:**

```csharp
// Scans TestFixtures directories
[SampleDataSource("TestFixtures/CSharp")]
[SampleDataSource("TestFixtures/VisualBasic")]
[SampleDataSource("TempTestCases", Optional = true)]  // .gitignored
public class SampleDataValidationTests { ... }
```

**Naming Convention for Auto-Discovery:**
- `{TestName}_Old.{ext}` / `{TestName}_New.{ext}`
- Example: `Calculator_Old.cs` / `Calculator_New.cs`

---

## 3. Test Groupings

### 3.1 By Diff Mode

| Mode | Description | Formats to Compare | External Tools |
|------|-------------|-------------------|----------------|
| **Roslyn Semantic** | C#/VB semantic diff | JSON, HTML, Text | None (unique) |
| **Line-by-Line** | Text-based diff | JSON, HTML, Text, Git | `diff`, `git diff` |

### 3.2 By Validation Type

| Validation | Applies To | Description |
|------------|-----------|-------------|
| **Format Consistency** | All modes | Same information across JSON/HTML/Text |
| **Line Number Integrity** | All modes | No duplicates/overlaps in sections |
| **Cross-Run Consistency** | All modes | Same output with different flag combinations |
| **External Tool Compat** | Line mode only | Match standard diff tools |

---

## 4. Detailed Test Cases

### 4.1 JSON Consistency Tests

**Test ID:** `JSON-001` - Flag Combination Consistency

```
For each test file pair:
  For each flag combination that produces JSON:
    - --json
    - --json --quiet
    - --json output.json
    - --json --html report.html (co-generation)

  Verify: JSON content is identical (excluding timestamps if IgnoreTimestamps=true)
```

**Test ID:** `JSON-002` - Line Number Integrity

```
For each JSON output:
  Parse changes array
  For each change type (added, removed, modified):
    Collect all source line ranges
    Collect all destination line ranges

  Verify:
    - No overlapping ranges within same change type (source)
    - No overlapping ranges within same change type (destination)
    - No duplicate line numbers in any range
```

### 4.2 HTML Consistency Tests

**Test ID:** `HTML-001` - Flag Combination Consistency

```
For each test file pair:
  For each flag combination that produces HTML:
    - --html report.html
    - --html report.html --quiet
    - --html report.html --json (co-generation)
    - --html report.html --open (ignore browser action)

  Verify: HTML content is identical (excluding timestamps if IgnoreTimestamps=true)
```

**Test ID:** `HTML-002` - Section Line Number Integrity

```
For each HTML output:
  Parse diff sections (added, removed, modified panels)
  Extract line numbers from each section

  Verify:
    - No duplicate line numbers within a single section
    - No overlapping line ranges between sections of same type
```

**Test ID:** `HTML-003` - Data Attribute Consistency

```
For each HTML output:
  Extract data-old-content and data-new-content attributes
  Parse embedded line numbers

  Verify: Matches visual display line numbers
```

### 4.3 Cross-Format Consistency Tests

**Test ID:** `XFMT-001` - JSON vs HTML Line Numbers

```
For each test file pair:
  Generate JSON output
  Generate HTML output

  Extract line numbers from both:
    - JSON: changes[].location.startLine, endLine
    - HTML: diff-line elements, data attributes

  Verify: All line numbers match exactly
```

**Test ID:** `XFMT-002` - JSON vs Text Line Numbers

```
For each test file pair:
  Generate JSON output
  Generate Text output

  Parse line references from both formats

  Verify: All line numbers match exactly
```

**Test ID:** `XFMT-003` - All Formats Agreement (Roslyn Mode)

```
For Roslyn-compatible files (.cs, .vb):
  Generate: JSON, HTML, Text

  Extract for each format:
    - Total change count
    - Change types (added, removed, modified)
    - Line number ranges
    - Symbol names (if applicable)

  Verify: All formats report identical information
```

**Test ID:** `XFMT-004` - All Formats Agreement (Line Mode)

```
For any text files:
  Generate: JSON, HTML, Text, Git-style

  Extract for each format:
    - Total change count
    - Line ranges

  Verify: All formats report identical information
```

### 4.4 External Tool Compatibility Tests

**Test ID:** `EXT-001` - Standard diff Compatibility

```
For line-diff test files:
  Run: roslyn-diff diff old.txt new.txt --git
  Run: diff -u old.txt new.txt

  Parse unified diff format from both

  Verify:
    - Same hunk headers (@@ lines)
    - Same added/removed line counts
    - Same line content (normalized whitespace)
```

**Test ID:** `EXT-002` - git diff Compatibility

```
For line-diff test files in git repo:
  Create temp git repo with old.txt
  Modify to new.txt
  Run: git diff
  Run: roslyn-diff diff old.txt new.txt --git

  Verify: Unified diff output matches
```

### 4.5 Sample Data Coverage Tests

**Test ID:** `SAMP-001` - All Samples Validated

```
For each file in samples/before/ and samples/after/:
  Run ValidateAll()

  Verify: All validations pass
```

**Test ID:** `SAMP-002` - All TestFixtures Validated

```
For each file pair in tests/*/TestFixtures/:
  Run ValidateAll()

  Verify: All validations pass
```

---

## 5. Implementation Plan

### Phase 1: Infrastructure (Priority: High)

| Task | Description | Est. Effort |
|------|-------------|-------------|
| 1.1 | Create `RoslynDiff.TestUtilities` project | Small |
| 1.2 | Implement `TestResult` model | Small |
| 1.3 | Implement `SampleDataValidator` core class | Medium |
| 1.4 | Create `[SampleDataSource]` attribute | Small |
| 1.5 | Add `TempTestCases/` to .gitignore | Trivial |

### Phase 2: Format Validators (Priority: High)

| Task | Description | Est. Effort |
|------|-------------|-------------|
| 2.1 | JSON parsing and line number extraction | Medium |
| 2.2 | HTML parsing and section extraction | Medium |
| 2.3 | Text format parsing | Small |
| 2.4 | Git unified diff parsing | Small |
| 2.5 | Timestamp normalization logic | Small |

### Phase 3: Consistency Validators (Priority: High)

| Task | Description | Est. Effort |
|------|-------------|-------------|
| 3.1 | Flag combination testing harness | Medium |
| 3.2 | Cross-format comparison logic | Medium |
| 3.3 | Line number overlap detection | Medium |
| 3.4 | Duplicate detection algorithms | Small |

### Phase 4: External Tool Integration (Priority: Medium)

| Task | Description | Est. Effort |
|------|-------------|-------------|
| 4.1 | Standard `diff` invocation wrapper | Small |
| 4.2 | `git diff` invocation wrapper | Small |
| 4.3 | Unified diff format parser | Medium |
| 4.4 | Output normalization for comparison | Small |

### Phase 5: Test Implementation (Priority: High)

| Task | Description | Est. Effort |
|------|-------------|-------------|
| 5.1 | JSON consistency test class | Medium |
| 5.2 | HTML consistency test class | Medium |
| 5.3 | Cross-format test class | Medium |
| 5.4 | External tool test class | Medium |
| 5.5 | Sample coverage test class | Small |

### Phase 6: Documentation & Cleanup (Priority: Medium)

| Task | Description | Est. Effort |
|------|-------------|-------------|
| 6.1 | README for TestUtilities project | Small |
| 6.2 | Update main testing documentation | Small |
| 6.3 | Add TempTestCases usage guide | Small |

---

## 6. File Structure

```
tests/
├── RoslynDiff.TestUtilities/           # NEW PROJECT
│   ├── RoslynDiff.TestUtilities.csproj
│   ├── Models/
│   │   └── TestResult.cs
│   ├── Validators/
│   │   ├── SampleDataValidator.cs      # Main entry point
│   │   ├── JsonValidator.cs
│   │   ├── HtmlValidator.cs
│   │   ├── TextValidator.cs
│   │   ├── GitDiffValidator.cs
│   │   └── LineNumberValidator.cs
│   ├── Parsers/
│   │   ├── JsonOutputParser.cs
│   │   ├── HtmlOutputParser.cs
│   │   ├── UnifiedDiffParser.cs
│   │   └── TextOutputParser.cs
│   ├── Comparers/
│   │   ├── FormatComparer.cs
│   │   └── LineRangeComparer.cs
│   ├── ExternalTools/
│   │   ├── DiffToolRunner.cs
│   │   └── GitDiffRunner.cs
│   └── Attributes/
│       └── SampleDataSourceAttribute.cs
│
├── RoslynDiff.SampleValidation.Tests/  # NEW PROJECT
│   ├── RoslynDiff.SampleValidation.Tests.csproj
│   ├── JsonConsistencyTests.cs
│   ├── HtmlConsistencyTests.cs
│   ├── CrossFormatConsistencyTests.cs
│   ├── LineNumberIntegrityTests.cs
│   ├── ExternalToolCompatibilityTests.cs
│   ├── SampleCoverageTests.cs
│   └── TempTestCases/                  # .gitignored
│       └── .gitkeep
│
└── ... (existing test projects)
```

---

## 7. Success Criteria

### Must Have (P0)

- [ ] All existing samples pass validation
- [ ] JSON/HTML/Text report same line numbers within each mode
- [ ] No overlapping line ranges in any output format
- [ ] TempTestCases folder available for ad-hoc testing
- [ ] Modular test structure allowing granular failure reporting

### Should Have (P1)

- [ ] External tool compatibility verified for line-diff mode
- [ ] Automatic test case discovery from directories
- [ ] ValidateAll() method for quick screening

### Nice to Have (P2)

- [ ] Performance benchmarks for validation
- [ ] CI integration with artifact preservation
- [ ] Visual diff of failing comparisons

---

## 8. Risks & Mitigations

| Risk | Impact | Mitigation |
|------|--------|------------|
| Different modes have fundamentally different outputs | High | Clear mode grouping, separate test classes |
| External tools have different formatting | Medium | Normalize before comparison, focus on line numbers |
| HTML structure changes break parsing | Medium | Use stable selectors, test parser separately |
| Timestamp comparison edge cases | Low | Configurable IgnoreTimestamps flag |

---

## 9. Appendix: Flag Combinations Matrix

### JSON Output Flags

| Flags | Generates JSON | Notes |
|-------|---------------|-------|
| `--json` | stdout | Default |
| `--json file.json` | file | Explicit path |
| `--json --quiet` | stdout | No console messages |
| `--json --html r.html` | stdout + file | Co-generation |

### HTML Output Flags

| Flags | Generates HTML | Notes |
|-------|---------------|-------|
| `--html file.html` | file | Required path |
| `--html file.html --open` | file + browser | Opens after |
| `--html file.html --quiet` | file | No console messages |
| `--html file.html --json` | file + stdout | Co-generation |

### Mode Selection

| Flags | Mode | Notes |
|-------|------|-------|
| (none, .cs/.vb file) | Roslyn | Auto-detected |
| `--mode roslyn` | Roslyn | Explicit |
| `--mode line` | Line | Force line-by-line |
| (none, .txt file) | Line | Auto-detected |

---

## 10. Review Checklist

- [ ] Test groupings make sense for different modes
- [ ] All sample files will be covered
- [ ] External tool comparison scope is appropriate
- [ ] TempTestCases approach meets needs
- [ ] Test utility API is intuitive
- [ ] Implementation phases are prioritized correctly

---

**Next Steps:**
1. Review and approve this strategy document
2. Create worktree: `feature/sample-data-validation-tests`
3. Begin Phase 1 implementation
