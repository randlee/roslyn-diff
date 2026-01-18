# Design: Non-Impactful Change Detection

**Document ID:** DESIGN-004
**Date:** 2026-01-18
**Status:** PROPOSED
**Worktree:** `/Users/randlee/Documents/github/roslyn-diff-worktrees/feature/non-impactful-detection`
**Branch:** `feature/non-impactful-detection` (based on `develop`)

---

## 1. Overview

This document describes the design for enhancing roslyn-diff with the ability to detect and distinguish **non-impactful changes** from impactful ones. Non-impactful changes are modifications that do not affect code execution or binary compatibility, such as parameter renames, private member renames, local variable renames, and code reordering within the same scope.

The goal is to provide users with a clearer understanding of what actually changed in a meaningful way, while still showing all changes for completeness.

---

## 2. Problem Statement

Currently, roslyn-diff treats all changes equally. When reviewing diffs, users (and AI consumers) cannot easily distinguish between:

1. **Breaking changes** - Public API modifications that could break consumers
2. **Internal changes** - Modifications to internal/private members that may affect internal callers
3. **Non-breaking changes** - Renames and reorderings that don't affect execution
4. **Formatting changes** - Whitespace-only modifications

This lack of categorization leads to:
- Wasted time reviewing non-impactful changes
- Difficulty prioritizing code review efforts
- AI agents spending tokens analyzing changes that don't matter
- Missed critical breaking changes buried among formatting noise

---

## 3. Proposed Solution

### 3.1 ChangeImpact Enum

Add a new enum to categorize the impact level of each change:

```csharp
namespace RoslynDiff.Core.Models;

/// <summary>
/// Categorizes the impact level of a code change.
/// </summary>
public enum ChangeImpact
{
    /// <summary>
    /// Changes to public API surface that could break external consumers.
    /// Examples: public method signature changes, public class renames,
    /// removal of public members.
    /// </summary>
    BreakingPublicApi,

    /// <summary>
    /// Changes to internal API surface that could break internal consumers.
    /// Examples: internal method changes, protected member modifications.
    /// </summary>
    BreakingInternalApi,

    /// <summary>
    /// Changes that don't affect code execution or API contracts.
    /// Examples: parameter renames, private member renames, local variable
    /// renames, code reordering within same scope.
    /// </summary>
    NonBreaking,

    /// <summary>
    /// Whitespace-only or comment-only changes.
    /// </summary>
    FormattingOnly
}
```

### 3.2 Detection Logic

The detection logic will be **always enabled** (not optional) and integrated into the `SemanticComparer`. The algorithm:

#### 3.2.1 Visibility Tracking

Extend `Change` record to track visibility:

```csharp
public record Change
{
    // ... existing properties ...

    /// <summary>
    /// Gets the impact level of this change.
    /// </summary>
    public ChangeImpact Impact { get; init; }

    /// <summary>
    /// Gets the visibility/accessibility of the affected symbol.
    /// </summary>
    public Visibility? Visibility { get; init; }

    /// <summary>
    /// Gets any caveats or warnings about this change's impact assessment.
    /// </summary>
    /// <remarks>
    /// For example, parameter renames may break named argument callers,
    /// or private member renames may break reflection-based code.
    /// </remarks>
    public IReadOnlyList<string>? Caveats { get; init; }
}

/// <summary>
/// Represents the visibility/accessibility of a symbol.
/// </summary>
public enum Visibility
{
    Public,
    Protected,
    Internal,
    ProtectedInternal,
    PrivateProtected,
    Private,
    Local  // For local variables, parameters
}
```

#### 3.2.2 Impact Classification Rules

| Change Type | Symbol Visibility | Impact |
|------------|-------------------|--------|
| Renamed | Public | BreakingPublicApi |
| Renamed | Internal | BreakingInternalApi |
| Renamed | Private | NonBreaking (with caveat) |
| Renamed | Local/Parameter | NonBreaking (with caveat for params) |
| Added | Public | BreakingPublicApi |
| Added | Internal | BreakingInternalApi |
| Added | Private/Local | NonBreaking |
| Removed | Public | BreakingPublicApi |
| Removed | Internal | BreakingInternalApi |
| Removed | Private/Local | NonBreaking |
| Modified (signature) | Public | BreakingPublicApi |
| Modified (signature) | Internal | BreakingInternalApi |
| Modified (body only) | Any | NonBreaking |
| Moved (same scope) | Any | NonBreaking |
| Moved (diff scope) | Public | BreakingPublicApi |

#### 3.2.3 Detection Algorithm

```
function ClassifyChangeImpact(change, oldTree, newTree):
    visibility = ExtractVisibility(change, oldTree, newTree)
    caveats = []

    // Formatting-only detection
    if IsWhitespaceOnly(change):
        return (FormattingOnly, visibility, [])

    // Renamed symbols
    if change.Type == Renamed:
        if visibility in [Public, Protected, ProtectedInternal]:
            return (BreakingPublicApi, visibility, [])
        if visibility in [Internal, PrivateProtected]:
            return (BreakingInternalApi, visibility, [])
        if change.Kind == Parameter:
            caveats.Add("Parameter rename may break callers using named arguments")
        if visibility == Private:
            caveats.Add("Private member rename may break reflection or serialization")
        return (NonBreaking, visibility, caveats)

    // Added/Removed/Modified
    if change.Type in [Added, Removed, Modified]:
        if IsSignatureChange(change):
            if visibility in [Public, Protected, ProtectedInternal]:
                return (BreakingPublicApi, visibility, [])
            if visibility in [Internal, PrivateProtected]:
                return (BreakingInternalApi, visibility, [])
        return (NonBreaking, visibility, caveats)

    // Moved symbols
    if change.Type == Moved:
        if HasSameContainingType(change):
            return (NonBreaking, visibility, ["Code reordering within same scope"])
        // Moved to different type/namespace
        if visibility in [Public, Protected]:
            return (BreakingPublicApi, visibility, [])
        if visibility == Internal:
            return (BreakingInternalApi, visibility, [])
        return (NonBreaking, visibility, [])

    return (NonBreaking, visibility, [])
```

### 3.3 HTML Output Changes

Non-impactful changes will be visually distinguished but still visible:

#### 3.3.1 New CSS Variables

```css
:root {
    /* Existing colors... */

    /* Non-breaking changes - muted/gray appearance */
    --color-nonbreaking-bg: #f6f8fa;
    --color-nonbreaking-border: #9ca3af;
    --color-nonbreaking-text: #6b7280;

    /* Formatting-only - even more muted */
    --color-formatting-bg: #fafafa;
    --color-formatting-border: #d1d5db;
    --color-formatting-text: #9ca3af;
}
```

#### 3.3.2 New Badge Classes

```css
.badge-nonbreaking {
    background-color: var(--color-nonbreaking-bg);
    color: var(--color-nonbreaking-text);
    border: 1px dashed var(--color-nonbreaking-border);
}

.badge-formatting {
    background-color: var(--color-formatting-bg);
    color: var(--color-formatting-text);
    border: 1px dotted var(--color-formatting-border);
    font-style: italic;
}

/* Impact indicator in change header */
.impact-indicator {
    font-size: 10px;
    padding: 1px 6px;
    border-radius: 3px;
    margin-left: 8px;
}

.impact-breaking-public {
    background-color: #fee2e2;
    color: #dc2626;
}

.impact-breaking-internal {
    background-color: #fef3c7;
    color: #d97706;
}

.impact-nonbreaking {
    background-color: #f3f4f6;
    color: #6b7280;
}

.impact-formatting {
    background-color: #f9fafb;
    color: #9ca3af;
}
```

#### 3.3.3 Caveat Display

When a change has caveats, display them as a subtle warning:

```html
<div class="change-caveats">
    <span class="caveat-icon">&#9888;</span>
    <span class="caveat-text">Parameter rename may break callers using named arguments</span>
</div>
```

```css
.change-caveats {
    font-size: 11px;
    color: #b45309;
    background-color: #fffbeb;
    padding: 4px 8px;
    border-radius: 4px;
    margin-top: 4px;
}
```

### 3.4 JSON Output Changes

#### 3.4.1 Design Decision: Include Non-Impactful by Default = False

**Rationale for AI Consumers:**

After consideration, the recommendation is to **exclude non-impactful changes by default** in JSON output for these reasons:

1. **Token efficiency**: AI models have limited context windows. Including non-impactful changes wastes tokens on changes that typically don't require action.

2. **Signal-to-noise ratio**: AI consumers need to identify what matters quickly. Filtering non-impactful changes improves the quality of information.

3. **Explicit opt-in**: When AI does need to see all changes (e.g., for comprehensive code review), it can explicitly request them via flag.

4. **Backward compatibility**: Existing AI integrations won't suddenly receive more data than expected.

However, when non-impactful changes ARE included, they should be clearly marked for filtering.

#### 3.4.2 JSON Schema Changes

```json
{
  "$schema": "roslyn-diff-output-v2",
  "metadata": {
    "version": "2.0.0",
    "timestamp": "2026-01-18T...",
    "mode": "roslyn",
    "options": {
      "includeContent": true,
      "contextLines": 3,
      "includeNonImpactful": false
    }
  },
  "summary": {
    "totalChanges": 10,
    "breakingPublicApi": 2,
    "breakingInternalApi": 1,
    "nonBreaking": 5,
    "formattingOnly": 2,
    "additions": 3,
    "deletions": 2,
    "modifications": 3,
    "renames": 1,
    "moves": 1
  },
  "files": [{
    "oldPath": "...",
    "newPath": "...",
    "changes": [{
      "type": "renamed",
      "kind": "method",
      "name": "NewMethodName",
      "oldName": "OldMethodName",
      "impact": "nonBreaking",
      "visibility": "private",
      "caveats": [
        "Private member rename may break reflection or serialization"
      ],
      "location": { ... },
      "content": "..."
    }]
  }]
}
```

#### 3.4.3 Summary Enhancement

The summary section provides quick impact assessment:

```json
"summary": {
  "totalChanges": 10,
  "impactBreakdown": {
    "breakingPublicApi": 2,
    "breakingInternalApi": 1,
    "nonBreaking": 5,
    "formattingOnly": 2
  },
  "typeBreakdown": {
    "additions": 3,
    "deletions": 2,
    "modifications": 3,
    "renames": 1,
    "moves": 1
  },
  "requiresReview": true,
  "hasBreakingChanges": true
}
```

### 3.5 CLI Options

#### 3.5.1 New Flags

```
--include-non-impactful    Include non-impactful changes in JSON output (default: false)
--include-formatting       Include formatting-only changes (default: false)
--impact-level <level>     Minimum impact level to show: breaking-public|breaking-internal|non-breaking|all
                           (default: all for HTML, breaking-internal for JSON)
```

#### 3.5.2 Examples

```bash
# Default: HTML shows all, JSON excludes non-impactful
roslyn-diff old.cs new.cs -o html > diff.html  # Shows all changes
roslyn-diff old.cs new.cs -o json              # Excludes non-impactful

# Include everything in JSON
roslyn-diff old.cs new.cs -o json --include-non-impactful

# Show only breaking changes in HTML
roslyn-diff old.cs new.cs -o html --impact-level breaking-internal

# CI mode: fail on breaking public API changes
roslyn-diff old.cs new.cs -o json --impact-level breaking-public
```

---

## 4. Implementation Details

### 4.1 Files to Modify

| File | Changes |
|------|---------|
| `src/RoslynDiff.Core/Models/Change.cs` | Add `Impact`, `Visibility`, `Caveats` properties |
| `src/RoslynDiff.Core/Models/ChangeType.cs` | No changes needed |
| `src/RoslynDiff.Core/Models/DiffOptions.cs` | Add `IncludeNonImpactful`, `MinimumImpactLevel` |
| `src/RoslynDiff.Core/Models/DiffResult.cs` | Update `DiffStats` with impact breakdown |
| `src/RoslynDiff.Core/Comparison/SemanticComparer.cs` | Add impact classification logic |
| `src/RoslynDiff.Core/Comparison/SymbolMatcher.cs` | Add visibility extraction methods |
| `src/RoslynDiff.Output/HtmlFormatter.cs` | Add impact-based styling and caveats |
| `src/RoslynDiff.Output/JsonFormatter.cs` | Add impact fields and filtering |
| `src/RoslynDiff.Output/OutputOptions.cs` | Add `IncludeNonImpactful` |
| `src/RoslynDiff.Cli/Commands/DiffCommand.cs` | Add new CLI flags |

### 4.2 New Files

| File | Purpose |
|------|---------|
| `src/RoslynDiff.Core/Models/ChangeImpact.cs` | New enum for impact classification |
| `src/RoslynDiff.Core/Models/Visibility.cs` | New enum for symbol visibility |
| `src/RoslynDiff.Core/Comparison/ImpactClassifier.cs` | Impact classification logic |
| `src/RoslynDiff.Core/Comparison/VisibilityExtractor.cs` | Extract visibility from Roslyn nodes |

### 4.3 Test Strategy

#### 4.3.1 Unit Tests

```csharp
// ImpactClassifierTests.cs
[Theory]
[InlineData("public void Foo(int x)", "public void Foo(int y)", ChangeImpact.BreakingPublicApi)]
[InlineData("private void Foo(int x)", "private void Foo(int y)", ChangeImpact.NonBreaking)]
[InlineData("internal void Foo()", "internal void Bar()", ChangeImpact.BreakingInternalApi)]
[InlineData("void Foo() { int x = 1; }", "void Foo() { int y = 1; }", ChangeImpact.NonBreaking)]
public void ClassifyImpact_ParameterRename_ReturnsExpectedImpact(
    string oldCode, string newCode, ChangeImpact expected) { ... }

// VisibilityExtractorTests.cs
[Theory]
[InlineData("public class Foo { }", Visibility.Public)]
[InlineData("internal class Foo { }", Visibility.Internal)]
[InlineData("class Foo { private int x; }", Visibility.Private)]
public void ExtractVisibility_FromDeclaration_ReturnsCorrectVisibility(
    string code, Visibility expected) { ... }
```

#### 4.3.2 Integration Tests

```csharp
[Fact]
public void SemanticComparer_PrivateRename_MarksAsNonBreaking()
{
    var oldCode = "class C { private void Foo() { } }";
    var newCode = "class C { private void Bar() { } }";

    var result = _differ.Compare(oldCode, newCode, DiffOptions.Default);

    var change = result.FileChanges[0].Changes[0];
    Assert.Equal(ChangeType.Renamed, change.Type);
    Assert.Equal(ChangeImpact.NonBreaking, change.Impact);
    Assert.Contains("reflection", change.Caveats[0], StringComparison.OrdinalIgnoreCase);
}

[Fact]
public void JsonFormatter_ExcludesNonImpactfulByDefault()
{
    var result = CreateResultWithMixedImpacts();
    var options = new OutputOptions { IncludeNonImpactful = false };

    var json = new JsonFormatter().FormatResult(result, options);
    var parsed = JsonDocument.Parse(json);

    var changes = parsed.RootElement
        .GetProperty("files")[0]
        .GetProperty("changes");

    Assert.All(changes.EnumerateArray(), c =>
        Assert.NotEqual("nonBreaking", c.GetProperty("impact").GetString()));
}
```

#### 4.3.3 Test Cases for Caveats

| Scenario | Expected Caveat |
|----------|-----------------|
| Parameter rename | "Parameter rename may break callers using named arguments" |
| Private field rename | "Private member rename may break reflection or serialization" |
| Private property rename | "Private member rename may break reflection or serialization" |
| Internal method rename | None (BreakingInternalApi, no caveat needed) |
| Local variable rename | None (purely non-breaking) |
| Code reordering | "Code reordering within same scope" |

---

## 5. API Changes

### 5.1 Breaking Changes

None. All changes are additive:
- New properties on `Change` record use `init` accessors
- New enum types don't affect existing code
- New CLI flags are optional with sensible defaults

### 5.2 New Public API

```csharp
// Models
public enum ChangeImpact { ... }
public enum Visibility { ... }

// Change record extensions
public record Change
{
    public ChangeImpact Impact { get; init; }
    public Visibility? Visibility { get; init; }
    public IReadOnlyList<string>? Caveats { get; init; }
}

// Options
public record DiffOptions
{
    public bool IncludeNonImpactful { get; init; } = true; // For core library
    public ChangeImpact MinimumImpactLevel { get; init; } = ChangeImpact.FormattingOnly;
}

public record OutputOptions
{
    public bool IncludeNonImpactful { get; init; } = false; // For JSON output
}
```

---

## 6. Migration/Compatibility

### 6.1 Existing Users

- **HTML output**: Will see additional visual distinctions (gray for non-impactful). Fully backward compatible.
- **JSON output**: Schema version bumped to v2. Existing consumers should handle gracefully.
- **CLI**: New flags are optional. Existing scripts unchanged.

### 6.2 Schema Versioning

- Current: `roslyn-diff-output-v1`
- Proposed: `roslyn-diff-output-v2`

The v2 schema adds:
- `impact` field on each change
- `visibility` field on each change
- `caveats` array on each change
- `impactBreakdown` in summary
- `options.includeNonImpactful` in metadata

---

## 7. Implementation Checklist

### Phase 1: Core Infrastructure (Day 1-2) ✅ COMPLETED 2026-01-18

- [x] Create `ChangeImpact.cs` enum
- [x] Create `Visibility.cs` enum
- [x] Create `SymbolKind.cs` enum (added - needed by ImpactClassifier)
- [x] Add properties to `Change.cs` record (Impact, Visibility, Caveats)
- [x] Create `VisibilityExtractor.cs` with Roslyn visitor
- [x] Create `ImpactClassifier.cs` with classification logic
- [x] Add unit tests for both new classes (59 tests: 23 + 36)
- [x] **QA Gate:** Run `dotnet test` - 844 tests pass (100%)
- [x] **QA Gate:** Stage, commit, push, create PR to develop
- **PR:** https://github.com/randlee/roslyn-diff/pull/34
- **Commit:** 6476acd

### Phase 2: SemanticComparer Integration (Day 3) ✅ COMPLETED 2026-01-18

- [x] Integrate `VisibilityExtractor` into symbol matching
- [x] Integrate `ImpactClassifier` into change creation
- [x] Update `DiffOptions` with new properties (IncludeNonImpactful, MinimumImpactLevel)
- [x] Update `DiffStats` with impact breakdown (BreakingPublicApiCount, etc.)
- [x] Add integration tests (5 tests for impact classification scenarios)
- [x] **QA Gate:** Run `dotnet test` - 849 tests pass (100%)
- [x] **QA Gate:** Stage, commit, push to PR
- **Commit:** 690fd30

### Phase 3: Output Formatters (Day 4)

- [ ] Update `JsonFormatter` with impact fields and filtering
- [ ] Update `HtmlFormatter` with impact styling
- [ ] Update `OutputOptions` with new properties
- [ ] Add formatter tests
- [ ] **QA Gate:** Run `dotnet test` - must be 100% pass
- [ ] **QA Gate:** Stage, commit, push to PR

### Phase 4: CLI Integration (Day 5)

- [ ] Add new CLI flags to `DiffCommand`
- [ ] Update help text and documentation
- [ ] Add end-to-end tests
- [ ] Manual testing
- [ ] **QA Gate:** Run `dotnet test` - must be 100% pass
- [ ] **QA Gate:** Stage, commit, push to PR

### Phase 5: Polish (Day 6)

- [ ] Code review and refactoring
- [ ] Performance testing
- [ ] Documentation updates
- [ ] Final testing pass
- [ ] **QA Gate:** Run `dotnet test` - must be 100% pass
- [ ] **QA Gate:** Final commit, push, PR ready for merge

---

## QA Requirements

**After every phase:**
1. Run `dotnet test --no-build` in worktree - **MUST achieve 100% pass rate**
2. Stage all changes: `git add -A`
3. Commit with descriptive message
4. Push to branch: `git push origin feature/non-impactful-detection`
5. Verify CI passes on PR

**Agent workflow:**
- Deploy QA background agent after each phase completes
- QA agent verifies tests, commits, pushes
- QA agent reports any failures for resolution before proceeding

---

## 8. Open Questions

### 8.1 Resolved

1. **Q: Should non-impactful detection be optional?**
   A: No. Detection is always on. Only the output filtering is configurable.

2. **Q: Default for JSON `includeNonImpactful`?**
   A: False. AI consumers benefit from focused output.

### 8.2 Under Discussion

1. **Parameter rename caveat threshold**: Should we caveat ALL parameter renames, or only those where the method is called with named arguments somewhere in the codebase? (Current: caveat all)

2. **Cross-assembly impact**: Should we consider InternalsVisibleTo when classifying internal changes? This would require analyzing project files.

3. **Serialization detection**: Should we try to detect [JsonProperty], [DataMember] etc. to provide smarter caveats for private renames?

4. **Impact inheritance**: If a class is modified, should child changes inherit the parent's impact level or be classified independently?

### 8.3 Future Considerations

1. **Binary compatibility analysis**: Integration with tools like ApiCompat for deeper breaking change detection.

2. **Git blame integration**: Could we provide "who changed this" context for breaking changes?

3. **Impact scoring**: Numeric severity scores in addition to categories (e.g., BreakingPublicApi = 100, NonBreaking = 10).

---

## Appendix A: Example Output

### A.1 JSON Example

```json
{
  "$schema": "roslyn-diff-output-v2",
  "metadata": {
    "version": "2.0.0",
    "options": {
      "includeNonImpactful": true
    }
  },
  "summary": {
    "totalChanges": 4,
    "impactBreakdown": {
      "breakingPublicApi": 1,
      "breakingInternalApi": 0,
      "nonBreaking": 2,
      "formattingOnly": 1
    }
  },
  "files": [{
    "changes": [
      {
        "type": "modified",
        "kind": "method",
        "name": "ProcessData",
        "impact": "breakingPublicApi",
        "visibility": "public",
        "caveats": []
      },
      {
        "type": "renamed",
        "kind": "parameter",
        "name": "input",
        "oldName": "data",
        "impact": "nonBreaking",
        "visibility": "local",
        "caveats": ["Parameter rename may break callers using named arguments"]
      },
      {
        "type": "renamed",
        "kind": "field",
        "name": "_cache",
        "oldName": "_data",
        "impact": "nonBreaking",
        "visibility": "private",
        "caveats": ["Private member rename may break reflection or serialization"]
      },
      {
        "type": "modified",
        "kind": "line",
        "impact": "formattingOnly",
        "visibility": null,
        "caveats": []
      }
    ]
  }]
}
```

### A.2 HTML Visual Example

```
+----------------------------------------------------------+
| [MODIFIED] Method: ProcessData                           |
| [BREAKING PUBLIC API]                    line 42 -> 45   |
| [Copy: JSON] [Copy: Diff]                                |
+----------------------------------------------------------+
|  - public string ProcessData(int count)                  |
|  + public string ProcessData(int count, bool validate)   |
+----------------------------------------------------------+

+----------------------------------------------------------+
| [RENAMED] Parameter: data -> input        (grayed out)   |
| [NON-BREAKING]                           line 50         |
| Warning: Parameter rename may break callers using        |
|          named arguments                                 |
| [Copy: JSON] [Copy: Diff]                                |
+----------------------------------------------------------+
|  - public void Foo(int data)                             |
|  + public void Foo(int input)                            |
+----------------------------------------------------------+
```

---

*End of Design Document*
