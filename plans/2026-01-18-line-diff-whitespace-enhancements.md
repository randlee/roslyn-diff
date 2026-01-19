# Design: Line Diff Whitespace/Line-Ending Enhancements

**Document ID:** DESIGN-005
**Date:** 2026-01-18
**Status:** PROPOSED
**Worktree:** `/Users/randlee/Documents/github/roslyn-diff-worktrees/feature/line-diff-whitespace`
**Branch:** `feature/line-diff-whitespace` (based on `develop`)

---

## 1. Overview

This design proposes enhancements to roslyn-diff's `LineDiffer` component to provide fine-grained control over whitespace and line-ending handling. The key insight is that while we must match standard `diff` behavior by default for compatibility, we can provide **superior** whitespace detection in language-aware mode, particularly for whitespace-significant languages like Python and YAML.

### Goals

1. **Default compatibility** - Match `diff` command output exactly by default
2. **Flexible whitespace modes** - Support multiple whitespace handling strategies
3. **Language-aware detection** - Enhanced whitespace issue detection for significant languages
4. **Backward compatibility** - Existing `-w`/`--ignore-whitespace` continues to work

---

## 2. Problem Statement

### Current Limitations

1. **Binary whitespace handling**: Currently only supports `IgnoreWhitespace = true|false`
2. **No standard diff matching**: Current `IgnoreWhitespace = false` may not exactly match `diff` behavior
3. **No language awareness**: Python/YAML indentation changes treated same as C# formatting changes
4. **No whitespace issue detection**: Dangerous whitespace changes (mixed tabs/spaces, indentation changes in Python) not flagged

### User Pain Points

```bash
# Current: Same behavior for semantically different cases
roslyn-diff diff old.py new.py    # Indentation change could break Python
roslyn-diff diff old.cs new.cs    # Indentation change is cosmetic in C#

# Desired: Language-aware warnings
roslyn-diff diff old.py new.py --whitespace-mode=language-aware
# Output: WARNING: Indentation changed on lines 15-20 (Python is whitespace-significant)
```

---

## 3. Current Implementation Analysis

### LineDiffer.cs (Line 67-68)

```csharp
var diffBuilder = new InlineDiffBuilder(_differ);
var diff = diffBuilder.BuildDiffModel(oldContent, newContent, options.IgnoreWhitespace);
```

The current implementation passes `IgnoreWhitespace` directly to DiffPlex, which:
- **When `true`**: Trims leading and trailing whitespace from each line before comparison
- **When `false`**: Compares lines exactly as-is

### DiffOptions.cs (Current)

```csharp
public record DiffOptions
{
    public DiffMode? Mode { get; init; }
    public bool IgnoreWhitespace { get; init; }  // Simple boolean
    public bool IgnoreComments { get; init; }
    public int ContextLines { get; init; } = 3;
    public string? OldPath { get; init; }
    public string? NewPath { get; init; }
}
```

### DiffPlex Capabilities (v1.9.0)

From DiffPlex source analysis:

```csharp
// InlineDiffBuilder.BuildDiffModel signature
public DiffPaneModel BuildDiffModel(
    string oldText,
    string newText,
    bool ignoreWhitespace = true,   // Trims leading/trailing
    bool ignoreCase = false,
    IChunker chunker = null         // Custom line splitting
)
```

DiffPlex supports:
- `ignoreWhitespace`: Trim leading/trailing whitespace
- `ignoreCase`: Case-insensitive comparison
- `IChunker`: Custom line splitting (can be used for advanced whitespace handling)

---

## 4. Proposed Solution

### 4.1 WhitespaceMode Enum

```csharp
namespace RoslynDiff.Core.Models;

/// <summary>
/// Specifies how whitespace differences should be handled during diff comparison.
/// </summary>
public enum WhitespaceMode
{
    /// <summary>
    /// Exact character-by-character comparison. Matches standard 'diff' command behavior.
    /// This is the default to ensure compatibility with existing workflows.
    /// </summary>
    Exact = 0,

    /// <summary>
    /// Ignore leading and trailing whitespace on each line.
    /// Equivalent to current DiffPlex behavior with ignoreWhitespace=true.
    /// Similar to 'diff -b' (ignore changes in amount of whitespace).
    /// </summary>
    IgnoreLeadingTrailing = 1,

    /// <summary>
    /// Collapse all whitespace to single spaces and trim.
    /// Multiple spaces/tabs become single space, leading/trailing removed.
    /// Similar to 'diff -w' (ignore all whitespace).
    /// </summary>
    IgnoreAll = 2,

    /// <summary>
    /// Language-aware whitespace handling.
    /// - Whitespace-significant languages (Python, YAML, Makefile): Preserve exact
    ///   whitespace and flag indentation changes as potentially breaking.
    /// - Brace languages (C#, Java, JavaScript): Safe to normalize formatting.
    /// </summary>
    LanguageAware = 3
}
```

### 4.2 Language Classification

```csharp
namespace RoslynDiff.Core.Models;

/// <summary>
/// Classifies languages by their whitespace sensitivity for diff purposes.
/// </summary>
public enum WhitespaceSensitivity
{
    /// <summary>
    /// Unknown or unclassified language. Uses exact comparison for safety.
    /// </summary>
    Unknown = 0,

    /// <summary>
    /// Whitespace is semantically significant (Python, YAML, Makefile, F#).
    /// Indentation changes may break program correctness.
    /// </summary>
    Significant = 1,

    /// <summary>
    /// Whitespace is generally insignificant (C#, Java, JavaScript, Go).
    /// Formatting changes don't affect program behavior.
    /// </summary>
    Insignificant = 2
}
```

Language classification mapping:

| Extension | Language | Sensitivity | Notes |
|-----------|----------|-------------|-------|
| `.py` | Python | Significant | Indentation defines blocks |
| `.yaml`, `.yml` | YAML | Significant | Indentation defines structure |
| `Makefile` | Make | Significant | Tabs required for recipes |
| `.fs`, `.fsx` | F# | Significant | Indentation-based syntax |
| `.nim` | Nim | Significant | Indentation-based |
| `.haml` | Haml | Significant | Indentation-based |
| `.pug`, `.jade` | Pug | Significant | Indentation-based |
| `.coffee` | CoffeeScript | Significant | Indentation-based |
| `.slim` | Slim | Significant | Indentation-based |
| `.cs` | C# | Insignificant | Brace-delimited |
| `.vb` | VB.NET | Insignificant | Line-based but not indentation-sensitive |
| `.java` | Java | Insignificant | Brace-delimited |
| `.js`, `.ts`, `.jsx`, `.tsx` | JS/TS | Insignificant | Brace-delimited |
| `.go` | Go | Insignificant | Brace-delimited |
| `.rs` | Rust | Insignificant | Brace-delimited |
| `.c`, `.cpp`, `.h`, `.hpp` | C/C++ | Insignificant | Brace-delimited |
| `.json` | JSON | Insignificant | Structure-delimited |
| `.xml`, `.html` | XML/HTML | Insignificant | Tag-delimited |

### 4.3 Standard Diff Compatibility

**Critical requirement**: `WhitespaceMode.Exact` (default) must produce output matching `diff` command.

Verification strategy:

```bash
# Generate diffs with both tools
diff old.txt new.txt > diff_output.txt
roslyn-diff diff old.txt new.txt --text > roslyn_output.txt

# Compare (should be identical for line changes)
diff diff_output.txt roslyn_output.txt
```

Key behaviors to match:
1. **Line comparison**: Exact byte-for-byte comparison
2. **Line endings**: Treat `\n`, `\r\n`, `\r` as line terminators
3. **Trailing newline**: File ending without newline is significant
4. **Empty lines**: Empty lines are significant

### 4.4 Language-Aware Detection

In `LanguageAware` mode, we don't just ignore or preserve whitespace - we **analyze and flag** potential issues.

#### Whitespace Issue Types

```csharp
namespace RoslynDiff.Core.Models;

/// <summary>
/// Types of whitespace issues that can be detected in language-aware mode.
/// </summary>
[Flags]
public enum WhitespaceIssue
{
    None = 0,

    /// <summary>
    /// Indentation level changed (significant for Python/YAML).
    /// </summary>
    IndentationChanged = 1 << 0,

    /// <summary>
    /// Mixed tabs and spaces in indentation.
    /// </summary>
    MixedTabsSpaces = 1 << 1,

    /// <summary>
    /// Trailing whitespace added or changed.
    /// </summary>
    TrailingWhitespace = 1 << 2,

    /// <summary>
    /// Line ending style changed (CRLF vs LF).
    /// </summary>
    LineEndingChanged = 1 << 3,

    /// <summary>
    /// Tab width assumption may be incorrect.
    /// </summary>
    AmbiguousTabWidth = 1 << 4
}
```

#### Detection Logic for Significant Languages

```csharp
// For Python/YAML, when comparing lines:
// 1. First, check if lines are textually identical (after normalization)
// 2. If yes, check if indentation differs
// 3. Flag indentation changes as WhitespaceIssue.IndentationChanged

// Example detection:
// Old: "    print('hello')"
// New: "        print('hello')"
// Result: Change flagged with IndentationChanged warning
```

---

## 5. Implementation Details

### 5.1 Files to Modify

| File | Change Type | Description |
|------|-------------|-------------|
| `src/RoslynDiff.Core/Models/DiffOptions.cs` | Modify | Add `WhitespaceMode` property, deprecate `IgnoreWhitespace` |
| `src/RoslynDiff.Core/Models/WhitespaceMode.cs` | Create | New enum file |
| `src/RoslynDiff.Core/Models/WhitespaceSensitivity.cs` | Create | New enum file |
| `src/RoslynDiff.Core/Models/WhitespaceIssue.cs` | Create | New flags enum file |
| `src/RoslynDiff.Core/Models/Change.cs` | Modify | Add `WhitespaceIssues` property |
| `src/RoslynDiff.Core/Differ/LineDiffer.cs` | Modify | Implement whitespace mode handling |
| `src/RoslynDiff.Core/Differ/LanguageClassifier.cs` | Create | File extension to sensitivity mapping |
| `src/RoslynDiff.Core/Differ/WhitespaceAnalyzer.cs` | Create | Whitespace issue detection |
| `src/RoslynDiff.Cli/Commands/DiffCommand.cs` | Modify | Add `--whitespace-mode` option |
| `src/RoslynDiff.Output/Formatters/ConsoleFormatter.cs` | Modify | Display whitespace warnings |

### 5.2 DiffPlex Integration

#### Approach: Custom Preprocessing + Standard DiffPlex

Rather than implement a custom `IChunker`, we preprocess content based on `WhitespaceMode`:

```csharp
// LineDiffer.Compare method update
public DiffResult Compare(string oldContent, string newContent, DiffOptions options)
{
    // Determine effective whitespace handling
    var whitespaceMode = options.WhitespaceMode;
    var sensitivity = GetLanguageSensitivity(options.NewPath ?? options.OldPath);

    // Preprocess content based on mode
    var (processedOld, processedNew, diffPlexIgnoreWs) = whitespaceMode switch
    {
        WhitespaceMode.Exact => (oldContent, newContent, false),
        WhitespaceMode.IgnoreLeadingTrailing => (oldContent, newContent, true),
        WhitespaceMode.IgnoreAll => (CollapseWhitespace(oldContent), CollapseWhitespace(newContent), false),
        WhitespaceMode.LanguageAware => HandleLanguageAware(oldContent, newContent, sensitivity),
        _ => (oldContent, newContent, false)
    };

    var diffBuilder = new InlineDiffBuilder(_differ);
    var diff = diffBuilder.BuildDiffModel(processedOld, processedNew, diffPlexIgnoreWs);

    // Post-process to detect whitespace issues if in LanguageAware mode
    var changes = BuildChanges(diff, options);
    if (whitespaceMode == WhitespaceMode.LanguageAware && sensitivity == WhitespaceSensitivity.Significant)
    {
        AnalyzeWhitespaceIssues(changes, oldContent, newContent);
    }

    return new DiffResult { ... };
}
```

#### CollapseWhitespace Implementation

```csharp
private static string CollapseWhitespace(string content)
{
    var lines = content.Split('\n');
    var processed = lines.Select(line =>
    {
        // Replace all whitespace sequences with single space
        var collapsed = Regex.Replace(line.TrimEnd('\r'), @"\s+", " ");
        return collapsed.Trim();
    });
    return string.Join("\n", processed);
}
```

### 5.3 Language Detection Logic

```csharp
namespace RoslynDiff.Core.Differ;

/// <summary>
/// Classifies file types by their whitespace sensitivity.
/// </summary>
public static class LanguageClassifier
{
    private static readonly HashSet<string> SignificantExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".py",      // Python
        ".pyw",     // Python Windows
        ".yaml",    // YAML
        ".yml",     // YAML
        ".fs",      // F#
        ".fsx",     // F# Script
        ".fsi",     // F# Signature
        ".nim",     // Nim
        ".haml",    // Haml
        ".pug",     // Pug/Jade
        ".jade",    // Jade
        ".coffee",  // CoffeeScript
        ".slim",    // Slim
        ".sass",    // Sass (indented syntax)
        ".styl",    // Stylus
    };

    private static readonly HashSet<string> SignificantFilenames = new(StringComparer.OrdinalIgnoreCase)
    {
        "Makefile",
        "GNUmakefile",
        "makefile",
    };

    public static WhitespaceSensitivity GetSensitivity(string? filePath)
    {
        if (string.IsNullOrEmpty(filePath))
            return WhitespaceSensitivity.Unknown;

        var fileName = Path.GetFileName(filePath);
        if (SignificantFilenames.Contains(fileName))
            return WhitespaceSensitivity.Significant;

        var extension = Path.GetExtension(filePath);
        if (SignificantExtensions.Contains(extension))
            return WhitespaceSensitivity.Significant;

        // All other recognized extensions are insignificant
        // Unknown extensions return Unknown (treated as Exact for safety)
        return WhitespaceSensitivity.Insignificant;
    }
}
```

### 5.4 WhitespaceAnalyzer Implementation

```csharp
namespace RoslynDiff.Core.Differ;

/// <summary>
/// Analyzes whitespace differences to detect potential issues in significant languages.
/// </summary>
public static class WhitespaceAnalyzer
{
    /// <summary>
    /// Analyzes a change for whitespace issues in whitespace-significant languages.
    /// </summary>
    public static WhitespaceIssue Analyze(string? oldLine, string? newLine)
    {
        var issues = WhitespaceIssue.None;

        if (oldLine == null || newLine == null)
            return issues;

        // Check indentation change
        var oldIndent = GetIndentation(oldLine);
        var newIndent = GetIndentation(newLine);

        if (oldIndent.Length != newIndent.Length || oldIndent != newIndent)
        {
            issues |= WhitespaceIssue.IndentationChanged;
        }

        // Check for mixed tabs/spaces
        if (HasMixedTabsSpaces(newIndent))
        {
            issues |= WhitespaceIssue.MixedTabsSpaces;
        }

        // Check trailing whitespace changes
        var oldTrailing = GetTrailingWhitespace(oldLine);
        var newTrailing = GetTrailingWhitespace(newLine);
        if (oldTrailing != newTrailing)
        {
            issues |= WhitespaceIssue.TrailingWhitespace;
        }

        return issues;
    }

    private static string GetIndentation(string line)
    {
        int i = 0;
        while (i < line.Length && (line[i] == ' ' || line[i] == '\t'))
            i++;
        return line.Substring(0, i);
    }

    private static bool HasMixedTabsSpaces(string indent)
    {
        return indent.Contains(' ') && indent.Contains('\t');
    }

    private static string GetTrailingWhitespace(string line)
    {
        int i = line.Length;
        while (i > 0 && char.IsWhiteSpace(line[i - 1]))
            i--;
        return line.Substring(i);
    }
}
```

---

## 6. CLI Changes

### 6.1 New Options

```csharp
// DiffCommand.Settings additions

/// <summary>
/// Gets or sets the whitespace handling mode.
/// </summary>
[CommandOption("--whitespace-mode <mode>")]
[Description("Whitespace handling: exact (default), ignore-leading-trailing, ignore-all, language-aware")]
[DefaultValue("exact")]
public string WhitespaceMode { get; set; } = "exact";
```

### 6.2 Backward Compatibility

The existing `-w`/`--ignore-whitespace` flag maps to `WhitespaceMode.IgnoreLeadingTrailing`:

```csharp
// In ExecuteAsync:
var whitespaceMode = settings.WhitespaceMode.ToLowerInvariant() switch
{
    "exact" => Models.WhitespaceMode.Exact,
    "ignore-leading-trailing" => Models.WhitespaceMode.IgnoreLeadingTrailing,
    "ignore-all" => Models.WhitespaceMode.IgnoreAll,
    "language-aware" => Models.WhitespaceMode.LanguageAware,
    _ => Models.WhitespaceMode.Exact
};

// -w/--ignore-whitespace takes precedence (backward compatibility)
if (settings.IgnoreWhitespace)
{
    whitespaceMode = Models.WhitespaceMode.IgnoreLeadingTrailing;
}
```

### 6.3 Usage Examples

```bash
# Default - exact comparison matching standard diff
roslyn-diff diff old.txt new.txt

# Ignore leading/trailing whitespace (equivalent to -w)
roslyn-diff diff old.cs new.cs --ignore-whitespace
roslyn-diff diff old.cs new.cs --whitespace-mode=ignore-leading-trailing

# Ignore all whitespace differences
roslyn-diff diff old.cs new.cs --whitespace-mode=ignore-all

# Language-aware mode - warns about dangerous whitespace changes
roslyn-diff diff old.py new.py --whitespace-mode=language-aware
# Output includes: WARNING: Indentation changed on line 15 (Python is whitespace-significant)

# Language-aware mode for brace language - normalizes formatting
roslyn-diff diff old.cs new.cs --whitespace-mode=language-aware
# Treats whitespace changes as cosmetic, no warnings
```

---

## 7. Test Strategy

### 7.1 Unit Tests

#### WhitespaceMode Tests

```csharp
// WhitespaceModeTests.cs

[Theory]
[InlineData("  hello  ", "  hello  ", WhitespaceMode.Exact, false)]      // Identical
[InlineData("  hello", "hello  ", WhitespaceMode.Exact, true)]           // Different
[InlineData("  hello", "hello  ", WhitespaceMode.IgnoreLeadingTrailing, false)] // Same after trim
[InlineData("hello   world", "hello world", WhitespaceMode.IgnoreAll, false)]   // Same collapsed
public void Compare_WithWhitespaceMode_ReturnsExpectedResult(
    string old, string @new, WhitespaceMode mode, bool hasDifferences)
{
    // Arrange
    var differ = new LineDiffer();
    var options = new DiffOptions { WhitespaceMode = mode };

    // Act
    var result = differ.Compare(old, @new, options);

    // Assert
    Assert.Equal(hasDifferences, result.HasChanges);
}
```

#### Language Classification Tests

```csharp
[Theory]
[InlineData("test.py", WhitespaceSensitivity.Significant)]
[InlineData("test.yaml", WhitespaceSensitivity.Significant)]
[InlineData("Makefile", WhitespaceSensitivity.Significant)]
[InlineData("test.cs", WhitespaceSensitivity.Insignificant)]
[InlineData("test.js", WhitespaceSensitivity.Insignificant)]
[InlineData("test.unknown", WhitespaceSensitivity.Unknown)]
public void GetSensitivity_ReturnsCorrectValue(string path, WhitespaceSensitivity expected)
{
    Assert.Equal(expected, LanguageClassifier.GetSensitivity(path));
}
```

#### Whitespace Issue Detection Tests

```csharp
[Fact]
public void Analyze_IndentationChange_DetectsIssue()
{
    var issues = WhitespaceAnalyzer.Analyze("    print(x)", "        print(x)");
    Assert.True(issues.HasFlag(WhitespaceIssue.IndentationChanged));
}

[Fact]
public void Analyze_MixedTabsSpaces_DetectsIssue()
{
    var issues = WhitespaceAnalyzer.Analyze("    code", "\t code");
    Assert.True(issues.HasFlag(WhitespaceIssue.MixedTabsSpaces));
}
```

### 7.2 Integration Tests

#### Standard Diff Compatibility Tests

```csharp
[Fact]
public async Task ExactMode_MatchesStandardDiff()
{
    // Arrange
    var oldContent = "line1\nline2\nline3";
    var newContent = "line1\nmodified\nline3";
    var tempOld = Path.GetTempFileName();
    var tempNew = Path.GetTempFileName();

    try
    {
        await File.WriteAllTextAsync(tempOld, oldContent);
        await File.WriteAllTextAsync(tempNew, newContent);

        // Get standard diff output
        var diffOutput = await RunDiffCommand(tempOld, tempNew);

        // Get roslyn-diff output
        var roslynOutput = await RunRoslynDiff(tempOld, tempNew, "--whitespace-mode=exact");

        // Compare change detection (not exact format, but same changes detected)
        AssertSameChangesDetected(diffOutput, roslynOutput);
    }
    finally
    {
        File.Delete(tempOld);
        File.Delete(tempNew);
    }
}
```

### 7.3 Test Files to Create

```
tests/
  RoslynDiff.Tests/
    Differ/
      LineDifferWhitespaceTests.cs      # Unit tests for whitespace modes
      LanguageClassifierTests.cs        # Language detection tests
      WhitespaceAnalyzerTests.cs        # Issue detection tests
    Integration/
      StandardDiffCompatibilityTests.cs # Verify matches 'diff' output
    TestData/
      Whitespace/
        python_indent_change.old.py
        python_indent_change.new.py
        csharp_format_change.old.cs
        csharp_format_change.new.cs
        mixed_tabs_spaces.txt
        trailing_whitespace.txt
```

---

## 8. Implementation Checklist

### Phase 1: Core Types (Est: 2 hours)

- [ ] Create `WhitespaceMode.cs` enum
- [ ] Create `WhitespaceSensitivity.cs` enum
- [ ] Create `WhitespaceIssue.cs` flags enum
- [ ] Update `DiffOptions.cs` with `WhitespaceMode` property
- [ ] Add `WhitespaceIssues` property to `Change.cs`
- [ ] **QA Gate:** Run `dotnet test` - must be 100% pass
- [ ] **QA Gate:** Stage, commit, push, create PR to develop

### Phase 2: Language Classification (Est: 1 hour)

- [ ] Create `LanguageClassifier.cs`
- [ ] Add comprehensive extension mappings
- [ ] Add unit tests for classification
- [ ] **QA Gate:** Run `dotnet test` - must be 100% pass
- [ ] **QA Gate:** Stage, commit, push to PR

### Phase 3: LineDiffer Updates (Est: 4 hours)

- [ ] Implement `WhitespaceMode.Exact` (verify matches `diff`)
- [ ] Implement `WhitespaceMode.IgnoreLeadingTrailing` (existing behavior)
- [ ] Implement `WhitespaceMode.IgnoreAll` (collapse whitespace)
- [ ] Implement `WhitespaceMode.LanguageAware` routing
- [ ] Add whitespace issue detection for significant languages
- [ ] **QA Gate:** Run `dotnet test` - must be 100% pass
- [ ] **QA Gate:** Stage, commit, push to PR

### Phase 4: WhitespaceAnalyzer (Est: 3 hours)

- [ ] Create `WhitespaceAnalyzer.cs`
- [ ] Implement indentation change detection
- [ ] Implement mixed tabs/spaces detection
- [ ] Implement trailing whitespace detection
- [ ] Add comprehensive unit tests
- [ ] **QA Gate:** Run `dotnet test` - must be 100% pass
- [ ] **QA Gate:** Stage, commit, push to PR

### Phase 5: CLI Integration (Est: 2 hours)

- [ ] Add `--whitespace-mode` option to `DiffCommand`
- [ ] Ensure `-w` backward compatibility
- [ ] Update help text and examples
- [ ] **QA Gate:** Run `dotnet test` - must be 100% pass
- [ ] **QA Gate:** Stage, commit, push to PR

### Phase 6: Output Formatting (Est: 2 hours)

- [ ] Update `ConsoleFormatter` to display whitespace warnings
- [ ] Update `TextFormatter` with warnings
- [ ] Update `HtmlFormatter` with visual indicators
- [ ] Update `JsonFormatter` with issue data
- [ ] **QA Gate:** Run `dotnet test` - must be 100% pass
- [ ] **QA Gate:** Stage, commit, push to PR

### Phase 7: Testing & Documentation (Est: 3 hours)

- [ ] Create standard diff compatibility tests
- [ ] Create language-aware mode tests
- [ ] Create test data files
- [ ] Update CLI help documentation
- [ ] **QA Gate:** Run `dotnet test` - must be 100% pass
- [ ] **QA Gate:** Final commit, push, PR ready for merge

### Total Estimated Time: 17 hours

---

## QA Requirements

**After every phase:**
1. Run `dotnet test --no-build` in worktree - **MUST achieve 100% pass rate**
2. Stage all changes: `git add -A`
3. Commit with descriptive message
4. Push to branch: `git push origin feature/line-diff-whitespace`
5. Verify CI passes on PR

**Agent workflow:**
- Deploy QA background agent after each phase completes
- QA agent verifies tests, commits, pushes
- QA agent reports any failures for resolution before proceeding

---

## 9. Open Questions

### 9.1 Line Ending Normalization

**Question**: Should we add a separate `LineEndingMode` option, or handle within `WhitespaceMode`?

**Options**:
1. Include in `WhitespaceMode` - simpler API
2. Separate `LineEndingMode` enum - more granular control
3. Add `NormalizeLineEndings` boolean - simple toggle

**Recommendation**: Start with option 1 (include in `WhitespaceMode.Exact` = preserve, others = normalize to LF). Can add separate option later if needed.

### 9.2 Tab Width Assumption

**Question**: When analyzing indentation, what tab width should we assume?

**Options**:
1. Assume 4 spaces (common default)
2. Make configurable via `--tab-width` option
3. Detect from file content (look for consistent patterns)

**Recommendation**: Default to 4, add `--tab-width` option for override. Detection is complex and error-prone.

### 9.3 Whitespace Issue Severity

**Question**: Should whitespace issues be warnings or errors?

**Options**:
1. Always warnings (informational)
2. Configurable `--strict-whitespace` to treat as errors
3. Severity based on issue type (indentation = error, trailing = warning)

**Recommendation**: Start with warnings only. Add `--strict-whitespace` in future if needed.

### 9.4 Performance Impact

**Question**: What is the performance impact of whitespace analysis?

**Analysis**:
- `WhitespaceMode.Exact` and `IgnoreLeadingTrailing` should have no impact
- `IgnoreAll` requires preprocessing (single pass O(n))
- `LanguageAware` adds post-processing analysis (single pass O(n))

**Recommendation**: Benchmark with large files (>10K lines). If needed, add streaming analysis.

### 9.5 Interaction with Roslyn Semantic Diff

**Question**: Should `WhitespaceMode` affect Roslyn semantic diff?

**Current state**: Roslyn semantic diff is inherently whitespace-agnostic (compares AST nodes).

**Recommendation**: `WhitespaceMode` only affects `LineDiffer`. Document this clearly in help text.

---

## Appendix A: DiffPlex API Reference

```csharp
// Key DiffPlex interfaces and classes

public interface IDiffer
{
    DiffResult CreateDiffs(
        string oldText,
        string newText,
        bool ignoreWhitespace,
        bool ignoreCase,
        IChunker chunker);
}

public interface IChunker
{
    string[] Chunk(string text);
}

public class InlineDiffBuilder
{
    public DiffPaneModel BuildDiffModel(
        string oldText,
        string newText,
        bool ignoreWhitespace = true,
        bool ignoreCase = false,
        IChunker chunker = null);
}
```

## Appendix B: Standard Diff Behavior Reference

```bash
# diff options for whitespace handling:
# (none)     - exact comparison
# -b         - ignore changes in amount of whitespace
# -w         - ignore all whitespace
# -B         - ignore blank lines
# --strip-trailing-cr - strip trailing CR

# Line ending behavior:
# diff treats \n as line terminator
# \r\n files: \r shown as part of line content
# Trailing newline: "\ No newline at end of file" warning
```

## Appendix C: Migration Guide

### For Users

```bash
# Old behavior (current)
roslyn-diff diff old.cs new.cs -w

# New equivalent
roslyn-diff diff old.cs new.cs --whitespace-mode=ignore-leading-trailing
# Or still use -w (backward compatible)
roslyn-diff diff old.cs new.cs -w
```

### For API Consumers

```csharp
// Old (deprecated but still works)
var options = new DiffOptions { IgnoreWhitespace = true };

// New (preferred)
var options = new DiffOptions { WhitespaceMode = WhitespaceMode.IgnoreLeadingTrailing };
```
