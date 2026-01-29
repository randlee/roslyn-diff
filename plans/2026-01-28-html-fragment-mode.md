# Design: HTML Fragment Mode for Embedding

**Document ID:** DESIGN-006
**Date:** 2026-01-28
**Status:** IN PROGRESS
**Worktree:** `/Users/randlee/Documents/github/roslyn-diff-worktrees/feature/html-fragment-mode`
**Branch:** `feature/html-fragment-mode` (based on `develop`)
**Related Issue:** [#46](https://github.com/randlee/roslyn-diff/issues/46)
**Target Version:** v0.9.0

---

## 1. Overview

Add `--html-mode fragment` option to generate embeddable HTML fragments without document wrapper (`<html>`, `<head>`, `<body>` tags). This enables roslyn-diff HTML output to be embedded in larger reports, documentation sites, and custom UIs without iframe overhead.

### Goals

1. **Embeddable output** - Generate HTML fragments that can be directly included in parent documents
2. **Self-contained styling** - Fragments include all necessary CSS (inline, scoped, or external)
3. **Metadata exposure** - Add data attributes for parent document integration
4. **Backward compatibility** - Keep current `document` mode as default
5. **Minimal complexity** - Start simple, extend based on real usage patterns

### Non-Goals (for initial release)

- Multi-file aggregation (covered by separate multi-file diff feature)
- Custom CSS theme injection (can be added later)
- Fragment-level JavaScript interactivity customization (use document mode if needed)

---

## 2. Problem Statement

### Current Limitations

**Current HTML output** is a complete document:
```html
<!DOCTYPE html>
<html>
<head>
  <meta charset="utf-8">
  <title>Diff: old.cs → new.cs</title>
  <style>/* ~500 lines of CSS */</style>
  <script>/* navigation, keyboard shortcuts */</script>
</head>
<body>
  <div class="diff-container">
    <!-- actual diff content -->
  </div>
</body>
</html>
```

**Embedding challenges**:
1. **Iframe overhead** - Parent must use iframes, causing scroll/sizing issues
2. **Style extraction** - Must parse HTML and re-inject styles manually
3. **No metadata** - Parent can't easily access summary statistics
4. **Navigation conflicts** - Multiple full documents can't share navigation

### User Pain Points

```bash
# Current workaround: Parse and extract body
roslyn-diff diff old.cs new.cs --html full.html
# Then manually extract <body> content and styles with external tools

# Desired workflow: Direct fragment generation
roslyn-diff diff old.cs new.cs --html fragment.html --html-mode fragment
# Include directly in parent document
```

**Real-world use case** (from issue #46):
```bash
# Generate fragments for PR review
for file in changed_files; do
  roslyn-diff diff target/$file source/$file \
    --html fragments/${file}.html \
    --html-mode fragment
done

# Combine into unified report
cat > report.html <<EOF
<!DOCTYPE html>
<html><body>
  <h1>PR #1234 Review</h1>
  $(cat fragments/*.html)
</body></html>
EOF
```

---

## 3. Current Implementation Analysis

### HTML Output Location

**File**: `src/RoslynDiff.Output/Formatters/HtmlOutputFormatter.cs`

Current structure:
```csharp
public class HtmlOutputFormatter : IOutputFormatter
{
    public string FormatResult(DiffResult result, OutputOptions options)
    {
        var sb = new StringBuilder();

        // Full document generation
        sb.AppendLine("<!DOCTYPE html>");
        sb.AppendLine("<html>");
        sb.AppendLine("<head>");
        sb.AppendLine("  <meta charset=\"utf-8\">");
        sb.AppendLine($"  <title>Diff: {result.OldPath} → {result.NewPath}</title>");
        sb.AppendLine("  <style>");
        sb.AppendLine(GetEmbeddedCss());  // ~500 lines
        sb.AppendLine("  </style>");
        sb.AppendLine("  <script>");
        sb.AppendLine(GetEmbeddedJavaScript());
        sb.AppendLine("  </script>");
        sb.AppendLine("</head>");
        sb.AppendLine("<body>");
        sb.AppendLine(GenerateDiffContent(result, options));  // Core content
        sb.AppendLine("</body>");
        sb.AppendLine("</html>");

        return sb.ToString();
    }

    private string GenerateDiffContent(DiffResult result, OutputOptions options)
    {
        // This is what we want to extract for fragment mode
        // Currently returns: <div class="diff-container">...</div>
    }
}
```

### Key Methods to Modify

1. **`FormatResult`** - Add mode detection and conditional wrapper
2. **`GenerateDiffContent`** - Already generates core content (good!)
3. **`GetEmbeddedCss`** - Need to scope or inline for fragments
4. **`GetEmbeddedJavaScript`** - Need to inline or make optional

### CSS Embedded Resource

**File**: `src/RoslynDiff.Output/Resources/diff-styles.css`
- ~500 lines of CSS
- Uses class selectors (`.diff-container`, `.change-item`, etc.)
- No scoping prefix currently

---

## 4. Proposed Solution

### 4.1 Add HtmlMode Enum

**File**: `src/RoslynDiff.Core/Models/OutputOptions.cs`

```csharp
public enum HtmlMode
{
    /// <summary>
    /// Generate complete HTML document with &lt;html&gt;, &lt;head&gt;, &lt;body&gt; tags.
    /// Includes embedded CSS and JavaScript. Default mode.
    /// </summary>
    Document,

    /// <summary>
    /// Generate embeddable HTML fragment without document wrapper.
    /// Includes scoped CSS in &lt;style&gt; block or inline styles.
    /// </summary>
    Fragment
}

public record OutputOptions
{
    // ... existing properties ...

    /// <summary>
    /// HTML generation mode. Default: Document.
    /// Only applies when HtmlOutputPath is set.
    /// </summary>
    public HtmlMode HtmlMode { get; init; } = HtmlMode.Document;

    /// <summary>
    /// Fragment style mode. Default: Scoped.
    /// Only applies when HtmlMode = Fragment.
    /// </summary>
    public FragmentStyleMode FragmentStyleMode { get; init; } = FragmentStyleMode.Scoped;
}
```

### 4.2 Add FragmentStyleMode Enum (Optional for v1)

```csharp
public enum FragmentStyleMode
{
    /// <summary>
    /// Include &lt;style&gt; block with scoped selectors (.roslyn-diff-fragment .class).
    /// Recommended for most embedding scenarios.
    /// </summary>
    Scoped,

    /// <summary>
    /// Use inline styles (style="...") on each element.
    /// Maximum isolation but verbose output.
    /// </summary>
    Inline,

    /// <summary>
    /// No styles included. Parent document must provide CSS.
    /// For scenarios where styles are loaded separately.
    /// </summary>
    External
}
```

**Decision for v1**: Start with **Scoped only**. Add Inline/External in v0.9.1 if users request it.

### 4.3 CLI Integration

**File**: `src/RoslynDiff.Cli/Commands/DiffCommand.cs`

```csharp
public class DiffCommand : Command
{
    private static Option<string> HtmlModeOption = new(
        aliases: new[] { "--html-mode" },
        description: "HTML generation mode: 'document' (full HTML doc) or 'fragment' (embeddable). Default: document",
        getDefaultValue: () => "document"
    );

    // Add validation
    HtmlModeOption.AddValidator(result =>
    {
        var value = result.GetValueOrDefault<string>();
        if (value != "document" && value != "fragment")
        {
            result.ErrorMessage = "HTML mode must be 'document' or 'fragment'";
        }
    });
}
```

**CLI examples**:
```bash
# Generate fragment (scoped CSS)
roslyn-diff diff old.cs new.cs --html output.html --html-mode fragment

# Full document (default, backward compatible)
roslyn-diff diff old.cs new.cs --html output.html
roslyn-diff diff old.cs new.cs --html output.html --html-mode document
```

### 4.4 Fragment Output Structure

**Fragment output** should be:

```html
<!-- Scoped CSS -->
<style>
.roslyn-diff-fragment { /* base container styles */ }
.roslyn-diff-fragment .diff-header { /* header styles */ }
.roslyn-diff-fragment .change-item { /* change item styles */ }
/* ... all existing CSS with .roslyn-diff-fragment prefix ... */
</style>

<!-- Fragment container with metadata -->
<div class="roslyn-diff-fragment"
     data-old-file="old.cs"
     data-new-file="new.cs"
     data-mode="roslyn"
     data-version="0.9.0"
     data-changes-total="12"
     data-changes-added="5"
     data-changes-removed="2"
     data-changes-modified="5"
     data-impact-breaking-public="2"
     data-impact-breaking-internal="1"
     data-impact-non-breaking="7"
     data-impact-formatting="2">

  <!-- Existing diff content (from GenerateDiffContent) -->
  <div class="diff-header">
    <h2>Diff: old.cs → new.cs</h2>
    <!-- ... -->
  </div>

  <div class="diff-summary">
    <!-- ... -->
  </div>

  <div class="changes-list">
    <!-- ... -->
  </div>

  <!-- Inline JavaScript (if needed) -->
  <script>
  (function() {
    // Namespaced JavaScript for this fragment
    const fragment = document.currentScript.previousElementSibling;
    // ... navigation, keyboard shortcuts scoped to this fragment ...
  })();
  </script>
</div>
```

### 4.5 CSS Scoping Implementation

**Option 1: Runtime prefix injection** (Recommended)
```csharp
private string GetScopedCss()
{
    var originalCss = GetEmbeddedCss();
    var scoped = new StringBuilder();

    // Simple regex-based prefixing
    // Convert ".diff-header" -> ".roslyn-diff-fragment .diff-header"
    var lines = originalCss.Split('\n');
    foreach (var line in lines)
    {
        if (line.TrimStart().StartsWith('.'))
        {
            scoped.AppendLine($".roslyn-diff-fragment {line}");
        }
        else
        {
            scoped.AppendLine(line);
        }
    }

    return scoped.ToString();
}
```

**Option 2: Pre-scoped CSS file** (Cleaner but requires build change)
- Maintain two CSS files: `diff-styles.css` and `diff-styles-scoped.css`
- Build process generates scoped version automatically
- More reliable for complex selectors

**Decision**: Start with **Option 1** for simplicity. Move to Option 2 if CSS complexity increases.

### 4.6 JavaScript Handling

**Current JavaScript features**:
- Keyboard navigation (Ctrl+J/K)
- Copy to clipboard buttons
- Collapsible sections
- IDE link generation

**Fragment mode considerations**:
1. **Namespace functions** - Avoid global scope pollution
2. **Scope event listeners** - Only apply to this fragment
3. **Make optional** - Allow `--no-scripts` for fragments

**Implementation**:
```javascript
(function() {
  'use strict';

  // Get reference to current fragment
  const fragment = document.currentScript.closest('.roslyn-diff-fragment');
  if (!fragment) return;

  // Scoped keyboard navigation
  document.addEventListener('keydown', function(e) {
    // Only handle if focus is within this fragment
    if (!fragment.contains(document.activeElement)) return;

    if (e.ctrlKey && e.key === 'j') {
      // Navigate to next change within this fragment
    }
  });

  // Scoped copy buttons
  fragment.querySelectorAll('.copy-button').forEach(button => {
    button.addEventListener('click', function() {
      // Copy logic scoped to this fragment
    });
  });
})();
```

---

## 5. Implementation Plan

### Phase 1: Core Fragment Generation (v0.9.0-alpha.1)
**Estimated effort**: 2-3 hours

**Tasks**:
- [x] Add `HtmlMode` enum to `OutputOptions.cs`
- [x] Add `--html-mode` CLI option to `DiffCommand.cs`
- [x] Add `--extract-css` CLI option for custom CSS filename
- [x] Modify `HtmlOutputFormatter.FormatResult()`:
  - Detect `options.HtmlMode`
  - Generate fragment (no wrapper) with external CSS reference
  - Generate CSS file (default: roslyn-diff.css)
- [x] Add data attributes to fragment container
- [x] Update `DiffResult` to include summary statistics for data attributes

**Output**: Working `--html-mode fragment` with external CSS file

### Phase 1-QA: Quality Assurance (LESSON LEARNED)
**Estimated effort**: 1 hour

**Tasks**:
- [x] Run full test suite (`dotnet test`)
- [x] Verify 100% pass rate locally
- [ ] Check cross-platform compatibility ⚠️ **SKIPPED - led to CI failures**
- [ ] Fix test failures on macOS/Windows

**Deliverable**: All tests passing on all platforms

**IMPORTANT**: Do NOT commit, push, or create PR until Phase QA is complete and all tests pass.

**Retrospective**: Phase 1 was committed without running tests on macOS/Windows, leading to CI failures. Future sprints MUST include cross-platform QA before commit.

### Phase 2: Testing & Documentation (v0.9.0-beta.1)
**Estimated effort**: 2-3 hours

**Tasks**:
- [x] Add unit tests for fragment generation
- [x] Add integration test: multiple fragments with shared CSS
- [x] Update `docs/output-formats.md` with fragment mode docs
- [x] Add `samples/fragment-mode/` with examples
- [x] Update README.md with fragment mode example
- [x] Update CHANGELOG.md

**Output**: Tested, documented feature ready for release

### Phase 3: Polish & Release (v0.9.0)
**Estimated effort**: 1 hour

**Tasks**:
- [x] Add screenshot to `docs/images/fragment-mode-example.png`
- [x] Validate all documentation links
- [x] Final testing with real-world embedding scenarios
- [x] Tag and release v0.9.0

### Phase 3-QA: Fix CI Failures (Remediation)
**Estimated effort**: 1-2 hours

**Tasks**:
- [ ] Reproduce test failures on macOS (in progress)
- [ ] Identify root cause (path separators, line endings, etc.)
- [ ] Fix cross-platform issues
- [ ] Re-run tests until all pass
- [ ] Commit and push fix

**Deliverable**: All CI checks passing (macOS, Windows, Ubuntu)

**Status**: QA agent currently running to fix CI failures

---

## 6. Testing Strategy

### 6.1 Unit Tests

**File**: `tests/RoslynDiff.Output.Tests/HtmlFragmentTests.cs`

```csharp
[Fact]
public void FormatResult_WithFragmentMode_ExcludesDocumentWrapper()
{
    var result = CreateSampleDiffResult();
    var options = new OutputOptions
    {
        HtmlMode = HtmlMode.Fragment
    };

    var html = _formatter.FormatResult(result, options);

    Assert.DoesNotContain("<!DOCTYPE html>", html);
    Assert.DoesNotContain("<html>", html);
    Assert.DoesNotContain("<head>", html);
    Assert.DoesNotContain("<body>", html);
}

[Fact]
public void FormatResult_WithFragmentMode_IncludesScopedCss()
{
    var result = CreateSampleDiffResult();
    var options = new OutputOptions
    {
        HtmlMode = HtmlMode.Fragment
    };

    var html = _formatter.FormatResult(result, options);

    Assert.Contains("<style>", html);
    Assert.Contains(".roslyn-diff-fragment", html);
}

[Fact]
public void FormatResult_WithFragmentMode_IncludesDataAttributes()
{
    var result = CreateSampleDiffResult();
    var options = new OutputOptions
    {
        HtmlMode = HtmlMode.Fragment
    };

    var html = _formatter.FormatResult(result, options);

    Assert.Contains("data-old-file=", html);
    Assert.Contains("data-new-file=", html);
    Assert.Contains("data-changes-total=", html);
    Assert.Contains("data-impact-breaking-public=", html);
}

[Fact]
public void FormatResult_WithDocumentMode_IncludesFullDocument()
{
    var result = CreateSampleDiffResult();
    var options = new OutputOptions
    {
        HtmlMode = HtmlMode.Document
    };

    var html = _formatter.FormatResult(result, options);

    Assert.Contains("<!DOCTYPE html>", html);
    Assert.Contains("<html>", html);
    Assert.Contains("<head>", html);
    Assert.Contains("<body>", html);
}
```

### 6.2 Integration Tests

**File**: `tests/RoslynDiff.Integration.Tests/FragmentEmbeddingTests.cs`

```csharp
[Fact]
public void MultipleFragments_CanBeEmbeddedInSinglePage()
{
    // Generate three fragments
    var fragment1 = GenerateFragment("Calculator.cs", "Add method");
    var fragment2 = GenerateFragment("Service.cs", "Refactor");
    var fragment3 = GenerateFragment("Utils.cs", "New helper");

    // Combine into parent document
    var parentHtml = $@"
        <!DOCTYPE html>
        <html>
        <body>
            <h1>PR Review</h1>
            {fragment1}
            {fragment2}
            {fragment3}
        </body>
        </html>
    ";

    // Validate HTML structure
    var doc = new HtmlDocument();
    doc.LoadHtml(parentHtml);

    var fragments = doc.DocumentNode.SelectNodes("//div[@class='roslyn-diff-fragment']");
    Assert.Equal(3, fragments.Count);

    // Validate each fragment has unique data attributes
    Assert.Equal("Calculator.cs", fragments[0].GetAttributeValue("data-old-file", ""));
    Assert.Equal("Service.cs", fragments[1].GetAttributeValue("data-old-file", ""));
    Assert.Equal("Utils.cs", fragments[2].GetAttributeValue("data-old-file", ""));
}

[Fact]
public void Fragment_CssScopingPreventsLeakage()
{
    var fragment = GenerateFragment("Test.cs", "Change");

    // All CSS selectors should be scoped
    Assert.Contains(".roslyn-diff-fragment .diff-header", fragment);
    Assert.DoesNotContain(".diff-header {", fragment); // Unscoped selector
}
```

### 6.3 Manual Testing Checklist

- [ ] Generate fragment for single file diff
- [ ] Embed fragment in test HTML page
- [ ] Verify styles render correctly
- [ ] Test keyboard navigation (Ctrl+J/K) works within fragment
- [ ] Test copy buttons work
- [ ] Embed multiple fragments on same page
- [ ] Verify no style conflicts between fragments
- [ ] Verify data attributes are correct
- [ ] Test in multiple browsers (Chrome, Firefox, Safari)
- [ ] Test with parent document that has conflicting CSS classes

---

## 7. Documentation Updates

### 7.1 README.md

Add section under "Output Formats":

```markdown
### HTML Fragment Mode

Generate embeddable HTML fragments for integration into larger reports:

```bash
# Generate fragment (for embedding)
roslyn-diff diff old.cs new.cs --html fragment.html --html-mode fragment

# Generate full document (default)
roslyn-diff diff old.cs new.cs --html report.html --html-mode document
```

**Fragment output includes:**
- Scoped CSS to prevent style conflicts
- Data attributes with summary statistics
- Interactive features (keyboard nav, copy buttons)
- No `<html>`, `<head>`, or `<body>` wrapper

**Example use case** - Combine multiple diffs:
```bash
# Generate fragments for each changed file
for file in *.cs; do
  roslyn-diff diff old/$file new/$file \
    --html fragments/$file.html \
    --html-mode fragment
done

# Combine into unified report
cat > review.html <<EOF
<!DOCTYPE html>
<html>
<body>
  <h1>Code Review</h1>
  $(cat fragments/*.html)
</body>
</html>
EOF
```

See [Output Formats Guide](docs/output-formats.md#html-fragment-mode) for details.
```

### 7.2 docs/output-formats.md

Add comprehensive section on fragment mode with:
- Detailed explanation of document vs fragment modes
- CSS scoping explanation
- Data attributes reference
- JavaScript scoping details
- Multiple-fragment embedding examples
- Best practices for parent document integration

### 7.3 samples/fragment-mode/

Create example directory with:
- `example-fragment.html` - Single fragment output
- `multiple-fragments.html` - Parent page with multiple embedded fragments
- `README.md` - Explanation of examples

---

## 8. Future Enhancements (Post-v0.9.0)

### v0.9.1: Advanced Style Modes
- Add `--fragment-style-mode inline` for inline styles
- Add `--fragment-style-mode external` for parent-provided styles
- Add `--fragment-css-prefix` for custom scope prefix

### v0.9.2: Script Control
- Add `--fragment-include-scripts` flag (default: true)
- Add `--fragment-script-mode` (inline, external, none)
- Support external script loading for shared code

### v1.0: Multi-File Integration
- When combined with multi-file diff feature, automatically generate one fragment per file
- Add `--html-output-dir` for directory of fragments
- Add master index.html that aggregates all fragments

---

## 9. Breaking Changes & Migration

### Backward Compatibility

✅ **Fully backward compatible**
- Default mode is `document` (current behavior)
- Existing commands work unchanged
- No breaking changes to API or CLI

### Migration Path

**None required** - This is an additive feature.

Users who want fragment mode must explicitly opt-in:
```bash
roslyn-diff diff old.cs new.cs --html output.html --html-mode fragment
```

---

## 10. Success Metrics

### Acceptance Criteria

- [ ] `--html-mode fragment` generates embeddable HTML without document wrapper
- [ ] Generates external CSS file (default: roslyn-diff.css) with shared styles
- [ ] Fragment includes data attributes with summary statistics
- [ ] Multiple fragments can coexist on same page without conflicts
- [ ] No JavaScript in fragments (static content only)
- [ ] Default mode remains `document` (backward compatible)
- [ ] Documentation includes examples of embedding fragments
- [ ] Integration test validates multi-fragment embedding with shared CSS

### User Validation

Test with use case from issue #46:
1. Generate fragments for multiple files
2. Combine into unified PR review report
3. Verify styles render correctly
4. Verify interactive features work
5. Verify no iframe workarounds needed

---

## 11. Open Questions

### Q1: Should fragments include navigation between files?

**Context**: If user embeds multiple fragments, should each fragment have "next/previous file" navigation?

**Options**:
1. **No** - Keep fragments independent, let parent handle navigation
2. **Yes** - Use data attributes to find sibling fragments and add nav
3. **Optional** - Add `--fragment-include-nav` flag

**Decision**: **No** (Option 1) for v1. Parent document should control navigation between fragments. We provide data attributes to make this easy.

### Q2: Should we support CSS variable customization?

**Context**: Allow parent document to customize colors via CSS variables?

**Example**:
```css
/* Parent document */
.roslyn-diff-fragment {
  --diff-add-color: #00ff00;
  --diff-remove-color: #ff0000;
}
```

**Decision**: **Not for v1**. Current CSS is comprehensive. Can add in v0.9.1 if users request it.

### Q3: How to handle very large diffs in fragments?

**Context**: Should fragments include collapse-all functionality?

**Decision**: **Yes**, inherit from document mode. Large diffs should be collapsible regardless of mode.

---

## 12. Risks & Mitigations

| Risk | Likelihood | Impact | Mitigation |
|------|------------|--------|------------|
| CSS scoping doesn't cover all selectors | Medium | High | Comprehensive testing with multiple fragments on same page |
| JavaScript conflicts between fragments | Low | Medium | Proper namespacing and scoping in IIFE |
| Data attributes not sufficient for parent needs | Medium | Low | Can add more attributes in patch release |
| Performance impact of inline CSS | Low | Low | CSS is only ~500 lines, negligible impact |
| Browser compatibility issues | Low | Medium | Test in Chrome, Firefox, Safari; use standard CSS/JS |

---

## 13. Alternatives Considered

### Alternative 1: Server-Side Rendering Components

**Description**: Instead of HTML fragments, provide React/Vue/Blazor components

**Pros**:
- Richer integration with modern frameworks
- Better state management
- Type-safe props

**Cons**:
- Much higher complexity
- Requires framework-specific implementations
- Limits use cases to JS/TS ecosystems

**Decision**: **Rejected**. HTML fragments are universal and framework-agnostic.

### Alternative 2: JSON + Client-Side Rendering

**Description**: Output JSON only, let clients render HTML

**Pros**:
- Maximum flexibility for clients
- Smaller output size
- Easier to customize

**Cons**:
- Shifts rendering burden to users
- No standard visualization
- Requires JavaScript expertise

**Decision**: **Rejected** for this feature. JSON output already exists for this use case. Fragment mode is for users who want ready-made HTML.

### Alternative 3: Iframe Mode

**Description**: Keep full documents, improve iframe embedding support

**Pros**:
- No CSS scoping complexity
- Complete isolation

**Cons**:
- Scrolling issues
- Sizing challenges
- Navigation complexity

**Decision**: **Rejected**. Iframes are the problem we're solving, not the solution.

---

## 14. References

- **Issue**: [#46 - Feature Request: HTML Fragment Mode for Embedding](https://github.com/randlee/roslyn-diff/issues/46)
- **Related**: Multi-file diff feature (separate design document)
- **HTML Output**: `src/RoslynDiff.Output/Formatters/HtmlOutputFormatter.cs`
- **CSS Styles**: `src/RoslynDiff.Output/Resources/diff-styles.css`

---

## Appendix A: Fragment Output Example

```html
<style>
.roslyn-diff-fragment { font-family: -apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, Oxygen-Sans, Ubuntu, Cantarell, "Helvetica Neue", sans-serif; }
.roslyn-diff-fragment .diff-header { background: #f6f8fa; padding: 16px; border-bottom: 1px solid #d0d7de; }
/* ... ~500 lines of scoped CSS ... */
</style>

<div class="roslyn-diff-fragment"
     data-old-file="Calculator.cs"
     data-new-file="Calculator.cs"
     data-mode="roslyn"
     data-version="0.9.0"
     data-changes-total="2"
     data-changes-added="2"
     data-changes-removed="0"
     data-changes-modified="0"
     data-impact-breaking-public="2"
     data-impact-breaking-internal="0"
     data-impact-non-breaking="0"
     data-impact-formatting="0">

  <div class="diff-header">
    <h2>Calculator.cs</h2>
    <div class="summary">2 changes (+2)</div>
  </div>

  <div class="changes-list">
    <div class="change-item added">
      <span class="badge breaking-public">Breaking Public API</span>
      <code>public int Multiply(int a, int b)</code>
    </div>
    <div class="change-item added">
      <span class="badge breaking-public">Breaking Public API</span>
      <code>public int Divide(int a, int b)</code>
    </div>
  </div>

  <script>
  (function() {
    const fragment = document.currentScript.previousElementSibling;
    // ... scoped JavaScript ...
  })();
  </script>
</div>
```

---

**Status**: Ready for implementation
**Next Step**: Create feature branch and begin Phase 1
