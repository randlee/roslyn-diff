# RoslynDiff HTML Samples Showcase

This directory contains comprehensive HTML samples demonstrating **all HTML output modes** supported by roslyn-diff. These samples are regenerable via the `generate-html-samples.sh` script.

## Quick Start

**View Samples:**
```bash
# macOS
open tree-document.html

# Linux
xdg-open tree-document.html

# Windows
start tree-document.html
```

**Regenerate All Samples:**
```bash
cd ../
./generate-html-samples.sh
```

## Sample Categories

### 1. Basic Modes (Document vs Fragment)

| Sample | Description | Command Used |
|--------|-------------|--------------|
| `tree-document.html` | Tree view in full HTML document (default) | `--html output.html` |
| `tree-fragment.html` | Tree view as embeddable fragment | `--html output.html --html-mode fragment` |

**What to Notice:**
- **Document mode** includes `<html>`, `<head>`, `<body>` tags with inline CSS
- **Fragment mode** has no wrapper, links to external CSS file
- Fragment mode has `data-*` attributes for JavaScript integration
- Document mode can be opened directly; fragments need parent page

**Use Cases:**
- **Document**: Standalone reports, code review archives, email attachments
- **Fragment**: Embedded in dashboards, CMS, documentation sites, CI/CD reports

---

### 2. Inline View Samples

| Sample | Description | Lines of Context |
|--------|-------------|------------------|
| `inline-document.html` | Full file inline view (git diff style) | All lines |
| `inline-context-3.html` | Inline view with minimal context | 3 lines |
| `inline-context-5.html` | Inline view with standard context | 5 lines |
| `inline-context-10.html` | Inline view with extended context | 10 lines |
| `inline-fragment.html` | Inline view in fragment mode | 5 lines |

**What to Notice:**
- `+` markers for added lines (green background)
- `-` markers for removed lines (red background)
- Context lines shown in gray
- Line numbers for both old and new files
- Syntax highlighting maintained in inline view
- `...` separators between non-contiguous change sections

**Comparison: Tree vs Inline View**

| Feature | Tree View | Inline View |
|---------|-----------|-------------|
| Display | Hierarchical by type | Line-by-line sequential |
| Best For | Structural changes | Traditional diff workflow |
| Context | Changed elements only | Full file or N lines around changes |
| Navigation | Jump to classes/methods | Scroll through code |
| Familiarity | Unique to roslyn-diff | Similar to `git diff` |
| File Size | Smaller (only changes) | Larger (includes context) |

**Use Cases:**
- **Inline Full** (`--inline`): Small files, comprehensive reviews
- **Inline Context** (`--inline=5`): Large files, focused reviews
- **Inline + Fragment**: Embedded line-by-line diffs in web apps

---

### 3. Additional File Samples (UserService)

| Sample | Description | Command Used |
|--------|-------------|--------------|
| `userservice-tree.html` | UserService comparison with tree view | `diff before/UserService.cs after/UserService.cs` |
| `userservice-inline.html` | UserService comparison with inline view | `diff before/UserService.cs after/UserService.cs --inline=5` |
| `userservice-fragment.html` | UserService fragment for embedding | `diff before/UserService.cs after/UserService.cs --html-mode fragment` |

**What to Notice:**
- More complex file with interfaces, classes, methods, and properties
- Shows structural changes and additions
- Demonstrates how roslyn-diff handles interface implementations
- Multiple types in single file (interface and class)
- Dependency injection patterns

**Use Cases:**
- **Service Layer Reviews**: Review business logic changes
- **Architecture Reviews**: Understand structural modifications
- **API Documentation**: Document service contract changes
- **Code Examples**: Show various C# patterns in diff form

---

### 4. Impact Classification Samples

| Sample | Description | Impact Filter |
|--------|-------------|---------------|
| `impact-full.html` | All impact levels with badges | `--impact-level all` |
| `impact-inline.html` | Impact classification in inline view | Default (all) |
| `impact-breaking-public-only.html` | Breaking public API changes only | `--impact-level breaking-public` |
| `impact-breaking-internal-only.html` | Breaking internal API and above | `--impact-level breaking-internal` |

**Impact Levels Explained:**

🔴 **Breaking Public API**
- Changes that break the public API surface
- Require consumers to update their code
- Examples: Method signature changes, public member removal, visibility reduction

🟠 **Breaking Internal API**
- Changes that break internal APIs within the same assembly
- Examples: Internal method changes, internal member removal

🟡 **Non-Breaking**
- Changes that don't break APIs but may have subtle effects
- Examples: Private member changes (reflection), parameter renames (named arguments)

🟢 **Formatting Only**
- Pure formatting changes with no functional impact
- Examples: Whitespace, comments, code reorganization

**What to Notice:**
- Color-coded impact badges on each change
- Filtering removes lower-impact changes from view
- Summary statistics by impact level
- Caveat warnings for subtle impacts (e.g., reflection, named arguments)

**Use Cases:**
- **breaking-public**: Library releases, API reviews, version planning
- **breaking-internal**: Internal refactoring reviews
- **all**: Comprehensive code reviews, change documentation

---

### 5. Multi-TFM (Target Framework Moniker) Samples

| Sample | Description | Target Frameworks |
|--------|-------------|-------------------|
| `tfm-tree.html` | Multi-TFM with tree view | net8.0, net10.0 |
| `tfm-inline.html` | Multi-TFM with inline view | net8.0, net10.0 |
| `tfm-fragment.html` | Multi-TFM fragment mode | net8.0, net10.0 |

**What to Notice:**
- TFM badges on framework-specific changes (e.g., "NET8.0", "NET10.0")
- Changes without badges apply to all analyzed frameworks
- Summary shows which TFMs were analyzed
- Conditional compilation (`#if NET8_0_OR_GREATER`) detection

**Examples:**
- Change marked "NET10.0" only appears in .NET 10.0 builds
- Change with no badge applies to all frameworks
- Helps understand multi-targeting impact

**Use Cases:**
- **Library Maintenance**: Track framework-specific changes
- **Migration Planning**: Understand API evolution across .NET versions
- **Compatibility Review**: Ensure changes work across target frameworks

---

### 6. Whitespace Handling Samples

| Sample | Description | Whitespace Mode |
|--------|-------------|-----------------|
| `whitespace-exact.html` | Exact whitespace matching (default) | `--whitespace-mode exact` |
| `whitespace-ignore-leading-trailing.html` | Ignore leading/trailing whitespace | `--whitespace-mode ignore-leading-trailing` |
| `whitespace-ignore-all.html` | Ignore all whitespace differences | `--whitespace-mode ignore-all` |
| `whitespace-language-aware.html` | Language-aware (semantically significant) | `--whitespace-mode language-aware` |

**Whitespace Modes Explained:**

- **exact**: All whitespace differences are detected (default)
- **ignore-leading-trailing**: Ignores whitespace at start/end of lines
- **ignore-all**: Ignores all whitespace (spaces, tabs, newlines)
- **language-aware**: Only detects semantically significant whitespace (C# specific)

**What to Notice:**
- Compare these samples with the same source files
- Notice which changes disappear with different modes
- Formatting-only changes clearly labeled
- Comment-only changes included with `--include-formatting`

**Use Cases:**
- **ignore-all**: Focus on logic changes, ignore code formatting
- **language-aware**: Detect meaningful whitespace in C# string literals
- **exact**: Code style reviews, whitespace policy enforcement

---

### 7. Combination Samples

| Sample | Description | Features Combined |
|--------|-------------|-------------------|
| `combo-inline-impact-tfm.html` | Inline + Impact + Formatting | `--inline=5 --include-formatting` |
| `combo-fragment-inline-breaking.html` | Fragment + Inline + Breaking changes | `--html-mode fragment --inline=5 --impact-level breaking-internal` |
| `combo-userservice-fragment-tree.html` | UserService + Fragment + Tree | `diff before/UserService.cs after/UserService.cs --html-mode fragment` |

**What to Notice:**
- Multiple features work together seamlessly
- Fragment mode enables dashboard embedding with any view
- Inline view compatible with impact filtering
- Context reduction useful for multi-file fragments

**Use Cases:**
- **Dashboard Embedding**: Fragments with various views for CI/CD
- **API Review Workflow**: Inline + breaking changes only for focused review
- **Documentation Sites**: Fragment mode with various view combinations
- **Complex File Reviews**: UserService sample shows comprehensive changes

---

## Understanding the Output Structure

### Document Mode Structure
```html
<!DOCTYPE html>
<html>
<head>
  <title>Diff: old.cs vs new.cs</title>
  <style>/* Inline CSS */</style>
</head>
<body>
  <div class="roslyn-diff-report">
    <header>
      <h1>Semantic Diff Report</h1>
      <div class="summary">...</div>
    </header>
    <main>
      <div class="change-group">...</div>
    </main>
  </div>
</body>
</html>
```

### Fragment Mode Structure
```html
<link rel="stylesheet" href="roslyn-diff.css">

<div class="roslyn-diff-fragment"
     data-old-file="old.cs"
     data-new-file="new.cs"
     data-changes-total="4"
     data-changes-added="2"
     data-changes-removed="0"
     data-changes-modified="2"
     data-impact-breaking-public="0"
     data-impact-breaking-internal="0"
     data-impact-non-breaking="2"
     data-impact-formatting="2"
     data-mode="roslyn"
     data-target-frameworks="net8.0;net10.0">
  <header>...</header>
  <main>...</main>
</div>
```

### Data Attributes for JavaScript

Access metadata from fragments:
```javascript
const fragment = document.querySelector('.roslyn-diff-fragment');

// File information
const oldFile = fragment.dataset.oldFile;
const newFile = fragment.dataset.newFile;

// Change statistics
const totalChanges = parseInt(fragment.dataset.changesTotal);
const additions = parseInt(fragment.dataset.changesAdded);
const deletions = parseInt(fragment.dataset.changesRemoved);
const modifications = parseInt(fragment.dataset.changesModified);

// Impact breakdown
const breakingPublic = parseInt(fragment.dataset.impactBreakingPublic);
const breakingInternal = parseInt(fragment.dataset.impactBreakingInternal);
const nonBreaking = parseInt(fragment.dataset.impactNonBreaking);
const formattingOnly = parseInt(fragment.dataset.impactFormatting);

// Analysis mode and frameworks
const mode = fragment.dataset.mode; // "roslyn" or "line"
const tfms = fragment.dataset.targetFrameworks?.split(';') || [];

// Show alert for breaking changes
if (breakingPublic > 0) {
  alert(`⚠️ Contains ${breakingPublic} breaking public API changes!`);
}
```

---

## Viewing Samples

### In Browser
```bash
# Open directly (document mode only)
open tree-document.html

# Open multiple samples
open tree-document.html inline-document.html multi-file-tree.html
```

### For Fragment Mode
Fragment samples need a parent HTML page. See `../fragment-mode/parent.html` for an embedding example.

```html
<!DOCTYPE html>
<html>
<head>
  <title>My Dashboard</title>
  <link rel="stylesheet" href="html-samples/tree-fragment.css">
</head>
<body>
  <h1>Code Review Dashboard</h1>

  <!-- Embed fragment -->
  <div id="diff-container">
    <!-- Load tree-fragment.html content here -->
  </div>

  <script>
    fetch('html-samples/tree-fragment.html')
      .then(r => r.text())
      .then(html => {
        document.getElementById('diff-container').innerHTML = html;
      });
  </script>
</body>
</html>
```

---

## Regenerating Samples

### Regenerate All
```bash
cd samples
./generate-html-samples.sh
```

### Regenerate Individual Samples

**Basic tree document:**
```bash
roslyn-diff diff before/Calculator.cs after/Calculator.cs --html html-samples/tree-document.html
```

**Inline view with context:**
```bash
roslyn-diff diff before/Calculator.cs after/Calculator.cs --html html-samples/inline-context-5.html --inline=5
```

**Fragment mode:**
```bash
roslyn-diff diff before/Calculator.cs after/Calculator.cs --html html-samples/tree-fragment.html --html-mode fragment
```

**Additional file (UserService):**
```bash
roslyn-diff diff before/UserService.cs after/UserService.cs --html html-samples/userservice-tree.html
```

**Impact filtering:**
```bash
roslyn-diff diff impact-demo/old.cs impact-demo/new.cs --html html-samples/impact-breaking-public-only.html --impact-level breaking-public
```

**Multi-TFM:**
```bash
roslyn-diff diff multi-tfm/old-conditional-code.cs multi-tfm/new-conditional-code.cs -t net8.0 -t net10.0 --html html-samples/tfm-tree.html
```

**Whitespace modes:**
```bash
roslyn-diff diff impact-demo/old.cs impact-demo/new.cs --html html-samples/whitespace-ignore-all.html --whitespace-mode ignore-all
```

---

## Key Features to Explore

### 1. Collapsible Sections
Click on namespace/class headers to expand/collapse sections for easier navigation.

### 2. Syntax Highlighting
C# code is syntax-highlighted in both tree and inline views for better readability.

### 3. Impact Badges
Color-coded badges indicate the severity of each change:
- 🔴 RED: Breaking public API
- 🟠 ORANGE: Breaking internal API
- 🟡 YELLOW: Non-breaking
- 🟢 GREEN: Formatting only

### 4. TFM Badges
When analyzing multiple target frameworks, changes show which frameworks they affect:
- "NET8.0" - Only in .NET 8.0
- "NET10.0" - Only in .NET 10.0
- No badge - All analyzed frameworks

### 5. Line Numbers
Inline view shows line numbers for both old and new versions, making it easy to reference specific locations.

### 6. Change Summary
Each report includes a summary with:
- Total changes count
- Breakdown by type (added/removed/modified)
- Breakdown by impact level
- File metadata

### 7. Responsive Design
HTML reports work on desktop, tablet, and mobile browsers.

### 8. Print-Friendly
Document mode reports are optimized for printing (CSS print styles included).

---

## Sample Comparison Table

Quick reference for choosing the right sample:

| Need | Sample to View |
|------|---------------|
| Quick start / default behavior | `tree-document.html` |
| Traditional git diff style | `inline-document.html` |
| Embed in web app | `tree-fragment.html` or `inline-fragment.html` |
| Review complex files | `userservice-tree.html` |
| Focus on breaking changes | `impact-breaking-public-only.html` |
| Multi-framework library | `tfm-tree.html` |
| Ignore formatting noise | `whitespace-ignore-all.html` |
| Line-by-line with context | `inline-context-5.html` |
| Dashboard integration | `combo-userservice-fragment-tree.html` |

---

## Tips for Effective Use

1. **Start with tree view** (`tree-document.html`) for structural overview
2. **Switch to inline view** (`inline-document.html`) for detailed line-by-line review
3. **Use impact filtering** when dealing with large changesets to focus on critical changes
4. **Try context modes** (`inline-context-3.html`) for large files to reduce scrolling
5. **Fragment mode** is ideal for integrating into existing tools and workflows
6. **Multi-TFM analysis** helps library maintainers understand framework-specific impacts
7. **Whitespace modes** help focus on functional changes vs formatting changes

---

## File Size Comparison

Sample file sizes (approximate):

| Sample Type | Size Range | Notes |
|-------------|------------|-------|
| Tree document | 50-200 KB | Depends on change count |
| Tree fragment | 10-50 KB | CSS external, smaller |
| Inline full | 100-500 KB | Includes all lines |
| Inline context | 30-150 KB | Only changed sections |
| Complex files | 100-300 KB | Like UserService samples |

Fragment mode produces smaller files since CSS is external and shared.

---

## Browser Compatibility

HTML samples work in all modern browsers:
- ✅ Chrome/Edge 90+
- ✅ Firefox 88+
- ✅ Safari 14+
- ✅ Opera 76+

No JavaScript required for viewing (but useful for fragment metadata access).

---

## Troubleshooting

### Sample Doesn't Display Properly
- **Check CSS**: For fragment mode, ensure CSS file is in same directory
- **Check path**: Verify relative paths in `<link>` tags
- **Check browser**: Use a modern browser (see compatibility above)

### Colors Look Wrong
- **Check mode**: Document mode has inline CSS; fragment needs external CSS
- **Check theme**: Browser extensions may override colors
- **Check file**: Verify complete file (not truncated download)

### Can't See All Changes
- **Check filtering**: Review `--impact-level` used during generation
- **Check context**: Inline context mode hides distant unchanged lines
- **Check whitespace mode**: Some modes hide whitespace-only changes

---

## Additional Resources

- [Main Samples README](../README.md) - Overview of all sample types
- [Inline View README](../inline-view/README.md) - Detailed inline view guide
- [Fragment Mode README](../fragment-mode/README.md) - Embedding patterns and examples
- [Multi-TFM README](../multi-tfm/README.md) - Target framework moniker analysis
- [RoslynDiff Documentation](../../README.md) - Complete tool documentation

---

## Version History

- **v0.11.0**: Complete HTML samples collection with generation script
- **v0.10.0**: Added inline view samples
- **v0.9.0**: Added fragment mode samples
- **v0.8.0**: Added impact classification samples

---

**Generated by**: `generate-html-samples.sh`
**Last Updated**: 2026-01-29
**RoslynDiff Version**: 0.11.0
