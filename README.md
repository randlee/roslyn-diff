# roslyn-diff

A semantic diff tool for .NET source code using Roslyn.

[![.NET 10](https://img.shields.io/badge/.NET-10.0-512BD4?style=flat-square&logo=dotnet)](https://dotnet.microsoft.com/)
[![License](https://img.shields.io/badge/License-MIT-blue?style=flat-square)](LICENSE)

## Overview

roslyn-diff provides semantic-aware diffing capabilities for .NET source code. Unlike traditional line-by-line diff tools, roslyn-diff understands code structure using the Roslyn compiler platform, enabling it to detect and report changes at the semantic level (classes, methods, properties, etc.).

### What's New

**v0.10.0 (Current)** - Multi-File Comparison
- Git branch comparison (`--git-compare main..feature`)
- Folder comparison with recursive traversal
- File filtering with glob patterns (`--include`, `--exclude`)
- Multi-file HTML reports with navigation
- JSON schema v3 for multi-file results

**v0.9.0** - HTML Enhancements
- Inline diff view with full file or context modes (`--inline`, `--inline=N`)
- Fragment mode for embeddable HTML (`--html-mode fragment`)
- External CSS extraction for shared styling
- Data attributes for JavaScript integration

**v0.8.0** - Intelligence & Multi-TFM
- Impact classification (Breaking Public/Internal, Non-Breaking, Formatting)
- Multi-target framework analysis (`-t net8.0 -t net10.0`)
- Whitespace intelligence with 4 modes
- Conditional compilation detection

See [CHANGELOG.md](CHANGELOG.md) for complete version history.

### Why Roslyn over tree-sitter?

For .NET languages (C# and Visual Basic), roslyn-diff uses Microsoft's Roslyn compiler instead of generic syntax parsers like tree-sitter:

- **Full semantic understanding** - Roslyn provides complete type information, symbol resolution, and semantic analysis, not just syntax trees
- **Rename detection** - Can distinguish between a renamed method vs. delete+add because Roslyn tracks symbol identity
- **Visibility tracking** - Knows if a member is public, internal, private, or protected for impact analysis
- **Type-aware comparisons** - Understands that `int` and `System.Int32` are the same type
- **Reference resolution** - Tracks cross-file dependencies and inheritance hierarchies
- **Official .NET tooling** - Same compiler used by Visual Studio, ensuring 100% compatibility with C# and VB.NET language features

tree-sitter is excellent for general-purpose syntax highlighting and basic structural analysis, but Roslyn provides the deep semantic insight needed for accurate impact classification and change analysis in .NET codebases.

## Features

### Multi-File Comparison (v0.10.0) 🚀

Compare entire changesets across branches, commits, or folders in a single operation:

- **Git branch comparison** - Compare files between branches or commits (`--git-compare main..feature`)
- **Folder comparison** - Automatic folder detection and recursive traversal (`old/ new/` or `--recursive`)
- **File filtering** - Include/exclude patterns with glob support (`--include "*.cs"` `--exclude "*.g.cs"`)
- **Multi-file HTML output** - Navigate between files with summary panel and file tree
- **JSON schema v3** - Structured multi-file results with per-file change tracking
- **Fragment mode integration** - Generate embeddable fragments for each changed file

Perfect for PR reviews, release comparisons, and migration analysis. See examples below for usage patterns.

### HTML Fragment Mode (v0.9.0) 📦

Generate embeddable HTML fragments for integration into web applications:

- **Fragment output** - HTML without document wrapper (`--html-mode fragment`)
- **External CSS** - Extracted stylesheet for shared styling (`--extract-css roslyn-diff.css`)
- **Data attributes** - JavaScript-friendly metadata for integration
- **Multi-fragment support** - Combine with multi-file comparison for scalable dashboards

Ideal for documentation sites, code review tools, CI/CD dashboards, and static site generators.

### Inline Diff View (v0.9.0) 📝

Traditional line-by-line diff view with semantic intelligence:

- **Inline mode** - Git-style diff with +/- markers (`--inline`)
- **Full file view** - Show entire file with changes marked (`--inline`)
- **Context mode** - Show only N lines around changes (`--inline=5`)
- **Syntax highlighting** - Full C# and VB.NET highlighting in inline view
- **Impact integration** - Impact badges and warnings in inline format

Familiar workflow for developers who prefer traditional diff views while maintaining semantic accuracy.

### Impact Classification (v0.7.0) 🎯

Automatically categorizes every code change by its potential impact on consumers:

- **Breaking Public API** - Changes that break external consumers (method signature changes, removed public members)
- **Breaking Internal API** - Changes that break internal consumers (internal method renames, parameter changes)
- **Non-Breaking** - Safe changes with no execution impact (private field renames, code reordering)
- **Formatting Only** - Pure whitespace/comment changes

Each change includes visibility tracking, caveat warnings (e.g., "parameter rename may break named arguments"), and smart filtering. See [Impact Classification Guide](docs/impact-classification.md) for details.

### Whitespace Intelligence (v0.8.0) 🔍

Fine-grained whitespace handling with language-aware detection:

- **4 whitespace modes**: Exact, IgnoreLeadingTrailing, IgnoreAll, LanguageAware
- **Automatic issue detection**: Indentation changes, mixed tabs/spaces, trailing whitespace, line ending changes
- **Language-specific handling**: Exact mode for Python/YAML (whitespace-significant), normalized for C#/Java

See [Whitespace Handling Guide](docs/whitespace-handling.md) for comprehensive details.

### Multi-Target Framework Analysis (v0.8.0) 🎯

Analyze code changes across multiple .NET target frameworks simultaneously:

- **Multi-TFM diff support**: Compare code compiled for different target frameworks (net8.0, net10.0, etc.)
- **Conditional compilation detection**: Identifies changes within `#if`, `#elif`, preprocessor blocks
- **Symbol resolution**: Automatically resolves TFM-specific symbols (NET8_0, NET10_0_OR_GREATER, etc.)
- **TFM-specific change tracking**: Each change indicates which frameworks it applies to
- **Performance optimized**: Pre-scan detects preprocessor directives, skips multi-TFM analysis when not needed
- **Parallel processing**: Analyzes multiple TFMs concurrently for faster results

See [Multi-TFM Support Guide](docs/tfm-support.md) for comprehensive details.

### Core Features

- **Semantic Diff** - Understands code structure, not just text changes
- **C# and VB.NET Support** - Full semantic analysis using Roslyn compiler platform
- **Structural Change Detection** - Identifies changes to classes, methods, properties, and fields
- **Rename Detection** - Detects when symbols are renamed (not just added/removed)
- **Move Detection** - Identifies code that has been moved within a file
- **Multiple Output Formats** - JSON (schema v3), HTML (interactive), text, terminal
- **Class-to-Class Comparison** - Compare specific classes between files with flexible matching
- **Line-by-Line Fallback** - Supports non-.NET files with traditional diff

## Screenshots

### HTML Output with Impact Classification

*Coming soon* - Interactive HTML report showing side-by-side diff with color-coded impact badges, caveat warnings, and IDE integration links. See [docs/images/](docs/images/) for screenshot specifications.

### JSON Output with Impact Breakdown

*Coming soon* - JSON Schema v2 output showing `impactBreakdown` statistics and per-change impact classification. See [docs/images/](docs/images/) for screenshot specifications.

For examples of what the output looks like, check out the [sample outputs](samples/):
- [samples/impact-demo/output.html](samples/impact-demo/output.html) - Full HTML report with impact indicators
- [samples/impact-demo/output.json](samples/impact-demo/output.json) - JSON with impact classification
- [samples/output-example.html](samples/output-example.html) - Calculator example HTML report

## Installation

### As a .NET Tool (Coming Soon)

```bash
dotnet tool install -g roslyn-diff
```

### From Source

```bash
git clone https://github.com/randlee/roslyn-diff.git
cd roslyn-diff
dotnet build
```

### Run without Installing

```bash
cd src/RoslynDiff.Cli
dotnet run -- diff old.cs new.cs
```

## Quick Start

### Compare Two Files

```bash
# Basic diff (colored output if terminal, plain if piped)
roslyn-diff diff old.cs new.cs

# AI use case: JSON for processing + HTML for human review
roslyn-diff diff old.cs new.cs --json --html report.html --open

# Output as JSON (for AI/tooling consumption)
roslyn-diff diff old.cs new.cs --json

# Generate HTML report and open in browser
roslyn-diff diff old.cs new.cs --html report.html --open

# Git-style unified diff (patchable)
roslyn-diff diff old.cs new.cs --git
```

### Inline Diff View (v0.9.0)

```bash
# Inline diff view (like git diff, shows full file)
roslyn-diff diff old.cs new.cs --html report.html --inline

# Inline view with 5 lines of context (compact)
roslyn-diff diff old.cs new.cs --html report.html --inline=5

# Inline with fragment mode (embeddable)
roslyn-diff diff old.cs new.cs --html fragment.html --html-mode fragment --inline=3
```

### HTML Fragment Mode (v0.9.0)

```bash
# Generate embeddable HTML fragment with external CSS
roslyn-diff diff old.cs new.cs --html fragment.html --html-mode fragment

# Outputs: fragment.html + roslyn-diff.css

# Custom CSS filename
roslyn-diff diff old.cs new.cs --html fragment.html --html-mode fragment --extract-css custom.css
```

### Multi-File Comparison (v0.10.0)

```bash
# Compare files between git branches
roslyn-diff diff --git-compare main..feature-branch

# Git comparison with HTML report
roslyn-diff diff --git-compare main..feature --html report.html --open

# Git comparison with JSON output
roslyn-diff diff --git-compare abc123..def456 --json

# Folder comparison (auto-detected)
roslyn-diff diff old/ new/

# Folder comparison with file filtering
roslyn-diff diff old/ new/ --include "*.cs" --exclude "*.Designer.cs"

# Recursive folder comparison
roslyn-diff diff old/ new/ --recursive --include "**/*.cs"

# Multi-file with fragment mode (generates one fragment per file)
roslyn-diff diff --git-compare main..feature --html-mode fragment --html fragments/

# Combine features: git + inline + fragment + filtering
roslyn-diff diff --git-compare main..feature \
  --include "*.cs" --exclude "*.g.cs" \
  --html-mode fragment --inline=3 --html fragments/
```

### Multi-Target Framework Analysis

```bash
# Analyze for a single target framework
roslyn-diff diff old.cs new.cs -t net8.0

# Analyze multiple target frameworks (repeatable flag)
roslyn-diff diff old.cs new.cs -t net8.0 -t net10.0

# Analyze multiple TFMs (semicolon-separated)
roslyn-diff diff old.cs new.cs -T "net8.0;net10.0"

# Multi-TFM with JSON output
roslyn-diff diff old.cs new.cs -t net8.0 -t net10.0 --json

# Multi-TFM with HTML report
roslyn-diff diff old.cs new.cs -T "net8.0;net9.0;net10.0" --html report.html --open
```

### Compare Specific Classes

```bash
# Compare same-named classes
roslyn-diff class old.cs:Calculator new.cs:Calculator

# Compare classes with different names
roslyn-diff class old.cs:OldService new.cs:NewService

# Match by interface implementation
roslyn-diff class old.cs new.cs --match-by interface --interface IRepository

# Match by content similarity
roslyn-diff class old.cs:Foo new.cs --match-by similarity --similarity 0.8
```

## CLI Commands

### `diff` - File Comparison

Compare two files, folders, or git refs and display the differences.

```bash
# Single file comparison
roslyn-diff diff <old-file> <new-file> [options]

# Folder comparison (auto-detected)
roslyn-diff diff <old-folder> <new-folder> [options]

# Git comparison
roslyn-diff diff --git-compare <ref-range> [options]
```

**Arguments:**
- `<old-file>` - Path to the original file (optional with `--git-compare`)
- `<new-file>` - Path to the modified file (optional with `--git-compare`)
- `<old-folder>` - Path to the original folder (auto-detected)
- `<new-folder>` - Path to the modified folder (auto-detected)

**Multi-File Options (v0.10.0):**
| Option | Description |
|--------|-------------|
| `--git-compare <ref-range>` | Compare files between git refs (e.g., `main..feature`, `abc123..def456`) |
| `--include <pattern>` | Include files matching glob pattern (repeatable, e.g., `"*.cs"`) |
| `--exclude <pattern>` | Exclude files matching glob pattern (repeatable, e.g., `"*.g.cs"`) |
| `--recursive` / `-r` | Recursively traverse subdirectories (folder comparison only) |

**Output Format Options (can combine multiple):**
| Option | Description |
|--------|-------------|
| `--json [file]` | JSON output (stdout if no file, or to specified file) |
| `--html <file>` | HTML report to file (required: file path) |
| `--text [file]` | Plain text diff (stdout if no file) |
| `--git [file]` | Git-style unified diff (stdout if no file) |
| `--open` | Open HTML in default browser after generation |

**HTML View Options:**
| Option | Description |
|--------|-------------|
| `--inline [N]` | Use inline diff view (like git diff). Optional: context lines (default: full file) |
| `--html-mode <mode>` | HTML mode: `document` (default) or `fragment` (embeddable) |
| `--extract-css <file>` | CSS filename for fragment mode (default: `roslyn-diff.css`) |

**Output Control:**
| Option | Description |
|--------|-------------|
| `--quiet` | Suppress default console output (for scripting) |
| `--no-color` | Disable colored output even if terminal supports it |

**Impact Filtering Options (v0.7.0):**
| Option | Description | Default |
|--------|-------------|---------|
| `--impact-level <level>` | Filter by impact: `breaking-public`, `breaking-internal`, `non-breaking`, `all` | Format-dependent* |
| `--include-non-impactful` | Include non-breaking and formatting changes (JSON only) | `false` |
| `--include-formatting` | Include formatting-only changes | `false` |

\* JSON defaults to `breaking-internal` (shows breaking changes only), HTML defaults to `all` (shows everything for review)

**Whitespace Options (v0.8.0):**
| Option | Short | Description | Default |
|--------|-------|-------------|---------|
| `--whitespace-mode <mode>` | | Whitespace handling: `exact`, `ignore-leading-trailing`, `ignore-all`, `language-aware` | `exact` |
| `--ignore-whitespace` | `-w` | Shortcut for `--whitespace-mode ignore-all` | `false` |

**Multi-TFM Options (v0.8.0):**
| Option | Short | Description | Default |
|--------|-------|-------------|---------|
| `--target-framework <tfm>` | `-t` | Target framework moniker (repeatable: `-t net8.0 -t net10.0`) | `null` (NET10_0 assumed) |
| `--target-frameworks <tfms>` | `-T` | Semicolon-separated TFMs (e.g., `"net8.0;net10.0"`) | `null` |

**Diff Mode Options:**
| Option | Short | Description | Default |
|--------|-------|-------------|---------|
| `--mode <mode>` | `-m` | Diff mode: `auto`, `roslyn`, `line` | `auto` |
| `--context <lines>` | `-C` | Lines of context to show | `3` |

**Default Behavior:**
- If no format flags: colored console output (if TTY) or plain text (if piped)
- Multiple formats can be combined (e.g., `--json --html report.html`)

**Comparison Modes:**
- **Single file** - Standard file-to-file comparison (default when two file paths provided)
- **Folder** - Auto-detected when paths are directories, compares matching files
- **Git** - Compare files between git refs with `--git-compare ref1..ref2`

**Diff Modes:**
- `auto` - Automatically select based on file extension (.cs/.vb use Roslyn, others use line diff)
- `roslyn` - Force semantic diff (requires .cs or .vb file)
- `line` - Force line-by-line diff

**File Filtering (Folder and Git modes):**
- Use `--include` with glob patterns to select files (e.g., `"*.cs"`, `"src/**/*.cs"`)
- Use `--exclude` to ignore files (e.g., `"*.g.cs"`, `"**/obj/**"`)
- Use `--recursive` to traverse subdirectories in folder mode
- Exclude patterns take precedence over include patterns

### `class` - Class Comparison

Compare specific classes between two files.

```bash
roslyn-diff class <old-spec> <new-spec> [options]
```

**Arguments:**
- `<old-spec>` - Old file specification: `file.cs:ClassName` or `file.cs`
- `<new-spec>` - New file specification: `file.cs:ClassName` or `file.cs`

**Options (same format options as diff command, plus):**
| Option | Short | Description | Default |
|--------|-------|-------------|---------|
| `--match-by <strategy>` | `-m` | Matching strategy: `exact`, `interface`, `similarity`, `auto` | `auto` |
| `--interface <name>` | `-i` | Interface name for interface matching | |
| `--similarity <threshold>` | `-s` | Similarity threshold (0.0-1.0) | `0.8` |

**Match Strategies:**
- `exact` - Match classes by exact name
- `interface` - Match classes implementing the specified interface
- `similarity` - Match by content similarity (for renamed classes)
- `auto` - Try exact name first, then similarity

## Exit Codes (CI/CD Friendly)

| Code | Meaning |
|------|---------|
| `0` | No differences found |
| `1` | Differences found (success, but files differ) |
| `2` | Error (file not found, parse error, etc.) |

**Example CI usage:**
```bash
roslyn-diff diff old.cs new.cs --quiet && echo "No changes"
```

## Output Formats

### JSON (`--json`)

Machine-readable format, ideal for AI consumption and tooling integration.

```bash
# Single file: To stdout (for piping to jq, AI tools, etc.)
roslyn-diff diff old.cs new.cs --json

# Single file: To file
roslyn-diff diff old.cs new.cs --json analysis.json

# Multi-file: Git comparison
roslyn-diff diff --git-compare main..feature --json output.json
```

**Single-File Output (Schema v2):**
```json
{
  "$schema": "roslyn-diff-output-v2",
  "metadata": {
    "version": "0.9.0",
    "timestamp": "2026-01-28T12:00:00Z",
    "mode": "roslyn",
    "analyzedTfms": ["net8.0", "net10.0"],
    "options": {
      "includeNonImpactful": false,
      "targetFrameworks": ["net8.0", "net10.0"]
    }
  },
  "summary": {
    "totalChanges": 4,
    "additions": 2,
    "deletions": 0,
    "modifications": 2,
    "impactBreakdown": {
      "breakingPublicApi": 4,
      "breakingInternalApi": 0,
      "nonBreaking": 0,
      "formattingOnly": 0
    }
  },
  "files": [{
    "changes": [{
      "type": "modified",
      "kind": "method",
      "name": "Process",
      "impact": "breakingPublicApi",
      "visibility": "public",
      "applicableToTfms": ["net8.0", "net10.0"],
      "caveats": ["Parameter rename may break callers using named arguments"]
    }]
  }]
}
```

**Multi-File Output (Schema v3) (v0.10.0):**
```json
{
  "$schema": "roslyn-diff-output-v3",
  "metadata": {
    "version": "0.10.0",
    "mode": "multi-file",
    "comparisonMode": "git",
    "gitRefRange": "main..feature-branch",
    "timestamp": "2026-01-28T12:00:00Z"
  },
  "summary": {
    "totalFiles": 15,
    "modifiedFiles": 8,
    "addedFiles": 5,
    "removedFiles": 2,
    "totalChanges": 47,
    "impactBreakdown": {
      "breakingPublicApi": 5,
      "breakingInternalApi": 8,
      "nonBreaking": 32,
      "formattingOnly": 2
    }
  },
  "files": [
    {
      "oldPath": "src/Calculator.cs",
      "newPath": "src/Calculator.cs",
      "status": "modified",
      "result": {
        "summary": { "totalChanges": 8, "additions": 3, "deletions": 1 },
        "changes": [
          {
            "type": "added",
            "kind": "method",
            "name": "Multiply",
            "impact": "nonBreaking",
            "visibility": "public"
          }
        ]
      }
    },
    {
      "oldPath": null,
      "newPath": "src/NewService.cs",
      "status": "added",
      "result": { "summary": { "totalChanges": 1, "additions": 1 } }
    }
  ]
}
```

See [Output Formats Guide](docs/output-formats.md) for complete schema documentation.

### HTML (`--html`)

Interactive HTML report with multiple modes and views.

**Document Mode (default)** - Standalone HTML report:

```bash
# Generate and open in browser
roslyn-diff diff old.cs new.cs --html report.html --open

# Filter to show only breaking changes in HTML
roslyn-diff diff old.cs new.cs --html report.html --impact-level breaking-public

# Multi-file report with file navigation
roslyn-diff diff --git-compare main..feature --html report.html --open
```

**Fragment Mode (v0.9.0)** - Embeddable HTML for integration:

```bash
# Single file: Generate HTML fragment with external CSS
roslyn-diff diff old.cs new.cs --html fragment.html --html-mode fragment
# Outputs: fragment.html + roslyn-diff.css

# Multi-file: Generate one fragment per file
roslyn-diff diff --git-compare main..feature --html-mode fragment --html fragments/
# Outputs: fragments/src-Calculator.cs.html, fragments/src-Service.cs.html, fragments/roslyn-diff.css
```

**View Modes** - Choose how to display changes:

```bash
# Tree view (default) - Shows structural hierarchy of changes
roslyn-diff diff old.cs new.cs --html report.html

# Inline view (v0.9.0) - Shows full file with +/- diff markers (like git diff)
roslyn-diff diff old.cs new.cs --html report.html --inline

# Inline view with context - Shows only N lines around changes
roslyn-diff diff old.cs new.cs --html report.html --inline=5

# Combine with fragment mode
roslyn-diff diff old.cs new.cs --html fragment.html --html-mode fragment --inline=3

# Multi-file with inline view
roslyn-diff diff --git-compare main..feature --html report.html --inline
```

**Tree View vs. Inline View:**

| Feature | Tree View (Default) | Inline View (`--inline`) |
|---------|---------------------|--------------------------|
| **Display** | Hierarchical (class → method → change) | Line-by-line with +/- markers |
| **Best For** | Structural changes, refactoring | Line-level review, patches |
| **File Display** | Shows only changed elements | Shows full file or context |
| **Familiarity** | IDE-style change tree | Git diff style |
| **Compact** | Very compact for large files | Can be verbose (use `--inline=N` for context mode) |
| **Impact Badges** | Shown on each change | Integrated with line markers |
| **Use Case** | API reviews, structural analysis | Code review, traditional workflow |

**Single-File Features:**
- **Impact classification badges** - Color-coded indicators (Breaking Public API, Breaking Internal API, Non-Breaking, Formatting Only)
- **TFM badges** - Framework-specific indicators (net8.0, net10.0) showing which TFMs each change applies to
- **Caveat warnings** - Yellow warning boxes for edge cases (e.g., "Parameter rename may break named arguments")
- **Whitespace issue indicators** - Warnings for indentation changes, mixed tabs/spaces, etc.
- **Tree view** - Hierarchical display of structural changes by element type
- **Inline view (v0.9.0)** - Line-by-line diff with +/- markers (full file or contextual)
- Syntax highlighting and keyboard navigation (Ctrl+J/K)
- Collapsible sections with copy buttons
- IDE integration links (VS Code, Rider, PyCharm, Zed)

**Multi-File Features (v0.10.0):**
- **File navigation panel** - Jump between changed files with summary counts
- **Overall statistics** - Aggregated impact breakdown across all files
- **File status indicators** - Modified, Added, Removed, Renamed badges
- **Collapsible file sections** - Show/hide individual file diffs
- **Search and filter** - Filter files by name or change type
- **Export options** - Download individual file diffs or full report

**Fragment Mode** for embedding in existing pages:
- Generates `<div>` container (no document wrapper)
- External CSS file for shared styling
- Data attributes for JavaScript integration
- Perfect for dashboards, documentation sites, and code review tools

```html
<!-- Embed in your page -->
<!DOCTYPE html>
<html>
<head>
  <link rel="stylesheet" href="roslyn-diff.css">
</head>
<body>
  <h1>Code Review</h1>
  <!-- Include fragment here -->
  <?php include('fragment.html'); ?>
</body>
</html>
```

See [Output Formats Guide](docs/output-formats.md) for details on all HTML features and [Fragment Mode Examples](samples/fragment-mode/) for integration patterns.

### Text (`--text`)

Structured plain text showing semantic changes:

```bash
roslyn-diff diff old.cs new.cs --text
```

```
Diff: old.cs -> new.cs
Mode: Roslyn (semantic)
Target Frameworks: net8.0, net10.0

Summary: +2 (2 total changes)

Changes:
  File: new.cs
    [+] Method: Multiply (line 5-8) [net8.0, net10.0]
    [+] Method: Divide (line 10-13) [net8.0, net10.0]
```

### Git Unified Diff (`--git`)

Standard unified diff format, compatible with `patch` command:

```bash
roslyn-diff diff old.cs new.cs --git
```

```diff
--- old/Calculator.cs
+++ new/Calculator.cs
@@ -1,7 +1,12 @@
 public class Calculator
 {
     public int Add(int a, int b) => a + b;
+    public int Multiply(int a, int b) => a * b;
 }
```

### Console Output (default)

When connected to a terminal (TTY):
- Color-coded changes (green for additions, red for deletions)
- Tree view for structural changes
- Summary tables

When piped/redirected:
- Falls back to plain text format
- No ANSI escape codes

## Change Types Detected

| Type | Description |
|------|-------------|
| `Added` | Element was added in the new version |
| `Removed` | Element was removed from the old version |
| `Modified` | Element exists in both versions but was changed |
| `Moved` | Element was moved to a different location |
| `Renamed` | Element was renamed (symbol name changed) |

## Use Cases

### Pull Request Reviews
- **Multi-file comparison** - Analyze all changes in a PR with `--git-compare main..feature`
- **Impact filtering** - Focus on breaking changes with `--impact-level breaking-public`
- **Fragment mode** - Embed diffs in code review dashboards
- **JSON output** - Automated PR analysis and gating in CI/CD

### Release Planning
- **Git branch comparison** - Compare release branches to identify all changes
- **Multi-TFM analysis** - Understand framework-specific changes across versions
- **Impact classification** - Categorize changes for release notes and communication
- **Folder comparison** - Compare extracted release packages

### Documentation
- **Fragment mode** - Embed diffs in upgrade guides and changelog pages
- **Inline view** - Traditional diff format for migration documentation
- **External CSS** - Consistent styling across documentation site
- **Static site integration** - Works with Jekyll, Hugo, Docusaurus, Gatsby

### CI/CD Automation
- **JSON output** - Parse changes programmatically for automated decisions
- **Impact filtering** - Fail builds on breaking public API changes
- **Git comparison** - Analyze changes in build pipelines
- **File filtering** - Focus on critical paths with include/exclude patterns

### API Evolution Analysis
- **Semantic diff** - Understand structural changes beyond line edits
- **Visibility tracking** - Track public vs. internal API changes
- **Rename detection** - Distinguish renames from delete+add
- **Caveat warnings** - Identify subtle breaking changes (e.g., parameter renames)

### Code Archaeology
- **Class comparison** - Track how specific classes evolved over time
- **Move detection** - Find code that relocated within files
- **Whitespace analysis** - Identify formatting and indentation changes
- **Historical comparison** - Compare any two commits with `--git-compare`

## Supported Languages

### Semantic Diff (Roslyn)
- **C#** (`.cs`) - Full semantic analysis
- **Visual Basic** (`.vb`) - Full semantic analysis

### Line-by-Line Diff
- All other text files (`.txt`, `.md`, `.json`, `.xml`, `.html`, etc.)

## Integration Patterns

roslyn-diff features can be combined to create powerful workflows for different use cases.

### Pattern 1: PR Review Dashboard (Multi-File + Fragment + Inline)

Create embeddable code review interfaces with per-file fragments:

```bash
# Generate fragments for all changed files
roslyn-diff diff --git-compare main..feature \
  --html-mode fragment \
  --html fragments/ \
  --inline=5 \
  --include "src/**/*.cs"
```

**Dashboard Integration:**
```php
<!DOCTYPE html>
<html>
<head>
  <title>PR #123 - Feature Branch Review</title>
  <link rel="stylesheet" href="fragments/roslyn-diff.css">
  <style>
    .file-section { margin: 20px 0; border: 1px solid #ddd; }
    .has-breaking { border-color: #d73a49; }
  </style>
</head>
<body>
  <h1>Pull Request #123</h1>

  <?php
  // Dynamically load all fragments
  foreach (glob("fragments/*.html") as $fragment) {
    echo "<div class='file-section'>";
    include($fragment);
    echo "</div>";
  }
  ?>

  <script>
    // Highlight files with breaking changes
    document.querySelectorAll('.roslyn-diff-fragment').forEach(frag => {
      if (parseInt(frag.dataset.impactBreakingPublic) > 0) {
        frag.closest('.file-section').classList.add('has-breaking');
      }
    });
  </script>
</body>
</html>
```

### Pattern 2: CI/CD Integration (Git + JSON + Filtering)

Automated analysis in build pipelines:

```bash
# In your CI/CD script
roslyn-diff diff --git-compare main..${CI_BRANCH} \
  --json pr-analysis.json \
  --include "src/**/*.cs" \
  --exclude "**/*.g.cs" \
  --impact-level breaking-public

# Parse JSON and fail build if breaking changes found
if jq -e '.summary.impactBreakdown.breakingPublicApi > 0' pr-analysis.json; then
  echo "Breaking API changes detected. Review required."
  exit 1
fi
```

### Pattern 3: Documentation Site (Fragment + Inline + TFM)

Embed upgrade guides with semantic diffs:

```bash
# Generate diff fragment for migration guide
roslyn-diff diff v1/Calculator.cs v2/Calculator.cs \
  --html-mode fragment \
  --html docs/migration/calculator-changes.html \
  --inline=3 \
  -T "net8.0;net10.0"
```

**Jekyll/Hugo Integration:**
```markdown
---
title: Migration Guide - v1 to v2
---

## Calculator API Changes

The Calculator class has been enhanced with new methods:

{% include migration/calculator-changes.html %}

### Impact Summary
- 2 new methods added (backward compatible)
- No breaking changes
- Available in .NET 8.0 and .NET 10.0
```

### Pattern 4: Release Notes Automation (Multi-File + JSON)

Generate structured release notes from branch comparison:

```bash
# Compare release branches
roslyn-diff diff --git-compare v1.0..v2.0 \
  --json release-changes.json \
  --include "src/**/*.cs"

# Process JSON to generate release notes
jq -r '.files[] |
  select(.status == "modified" or .status == "added") |
  "\(.newPath): \(.result.summary.totalChanges) changes"' \
  release-changes.json > RELEASE_NOTES.txt
```

### Pattern 5: Code Review Bot (Multi-File + Fragment + Impact)

Automated PR comments with inline diffs:

```bash
# Generate analysis for PR bot
roslyn-diff diff --git-compare origin/main..HEAD \
  --json pr-data.json \
  --html-mode fragment \
  --html fragments/ \
  --impact-level breaking-public

# Bot posts comment with breaking changes
```

**GitHub Action Example:**
```yaml
- name: Analyze PR Changes
  run: |
    roslyn-diff diff --git-compare origin/main..HEAD \
      --json pr-analysis.json \
      --html-mode fragment \
      --html pr-fragments/

- name: Post PR Comment
  uses: actions/github-script@v6
  with:
    script: |
      const fs = require('fs');
      const analysis = JSON.parse(fs.readFileSync('pr-analysis.json'));
      const breaking = analysis.summary.impactBreakdown.breakingPublicApi;

      if (breaking > 0) {
        github.rest.issues.createComment({
          issue_number: context.issue.number,
          owner: context.repo.owner,
          repo: context.repo.repo,
          body: `⚠️ This PR contains ${breaking} breaking API changes. Review required.`
        });
      }
```

### Pattern 6: Multi-Framework Analysis (Git + TFM + Filtering)

Identify framework-specific changes in codebases:

```bash
# Analyze changes across .NET versions
roslyn-diff diff --git-compare release/v1..release/v2 \
  --html report.html \
  -T "net462;net8.0;net10.0" \
  --include "src/**/*.cs" \
  --exclude "**/Platform/**"
```

## HTML Fragment Mode

roslyn-diff can generate embeddable HTML fragments for integration into existing web applications, documentation sites, and code review dashboards.

### Quick Start with Fragment Mode

```bash
# Generate HTML fragment with external CSS
roslyn-diff diff old.cs new.cs --html fragment.html --html-mode fragment

# Outputs two files:
# - fragment.html (embeddable HTML)
# - roslyn-diff.css (external stylesheet)
```

### Embedding the Fragment

```html
<!DOCTYPE html>
<html>
<head>
  <title>Code Review Dashboard</title>
  <!-- Include the external CSS -->
  <link rel="stylesheet" href="roslyn-diff.css">
</head>
<body>
  <h1>Pull Request #123 - Changes</h1>

  <!-- Embed the fragment -->
  <div id="diff-container">
    <?php include('fragment.html'); ?>
  </div>
</body>
</html>
```

### Data-Driven Integration

Fragments include data attributes for JavaScript integration:

```javascript
const fragment = document.querySelector('.roslyn-diff-fragment');

// Extract metadata
const stats = {
  totalChanges: parseInt(fragment.dataset.changesTotal),
  breakingPublic: parseInt(fragment.dataset.impactBreakingPublic),
  additions: parseInt(fragment.dataset.changesAdded),
  mode: fragment.dataset.mode
};

// Show alert if breaking changes exist
if (stats.breakingPublic > 0) {
  showWarning(`⚠️ ${stats.breakingPublic} breaking API changes detected`);
}
```

### Use Cases

- **Documentation Sites** - Embed diffs in changelog or upgrade guides
- **Code Review Tools** - Build custom review interfaces with multiple diffs
- **CI/CD Dashboards** - Show diff reports in build pipeline pages
- **Static Site Generators** - Include diffs in Jekyll, Hugo, or Gatsby sites

See [samples/fragment-mode/](samples/fragment-mode/) for complete examples and integration patterns.

## Examples

### Example 1: Detecting Added Methods

**Before (Calculator.cs):**
```csharp
public class Calculator
{
    public int Add(int a, int b) => a + b;
    public int Subtract(int a, int b) => a - b;
}
```

**After (Calculator.cs):**
```csharp
public class Calculator
{
    public int Add(int a, int b) => a + b;
    public int Subtract(int a, int b) => a - b;
    public int Multiply(int a, int b) => a * b;
    public int Divide(int a, int b) => a / b;
}
```

**Command:**
```bash
roslyn-diff diff before.cs after.cs --json
```

**Result:** Detects 2 added methods (`Multiply`, `Divide`)

### Example 2: Comparing Renamed Classes

```bash
# Compare OldService to NewService using content similarity
roslyn-diff class old.cs:OldService new.cs:NewService

# Auto-detect matching class in new file
roslyn-diff class old.cs:OldService new.cs --match-by similarity
```

### Example 3: Generating Reports

```bash
# JSON report for CI/CD
roslyn-diff diff src/old.cs src/new.cs --json diff.json

# HTML report for review and open in browser
roslyn-diff diff src/old.cs src/new.cs --html report.html --open
```

### Example 4: Pull Request Review (v0.10.0)

```bash
# Compare current feature branch against main
roslyn-diff diff --git-compare main..HEAD --html pr-review.html --open

# Show only breaking changes
roslyn-diff diff --git-compare main..feature \
  --impact-level breaking-public \
  --html report.html

# Export JSON for automated analysis
roslyn-diff diff --git-compare main..feature \
  --json pr-changes.json \
  --include "src/**/*.cs" \
  --exclude "**/*.Designer.cs"
```

### Example 5: Fragment Mode Integration (v0.9.0)

```bash
# Generate embeddable fragments for documentation site
roslyn-diff diff old.cs new.cs \
  --html-mode fragment \
  --html fragments/calculator-diff.html \
  --inline=5

# Multi-file fragments for dashboard
roslyn-diff diff --git-compare main..feature \
  --html-mode fragment \
  --html fragments/ \
  --include "*.cs"
```

**Integration Example:**
```html
<!DOCTYPE html>
<html>
<head>
  <title>PR #123 Review</title>
  <link rel="stylesheet" href="fragments/roslyn-diff.css">
  <script>
    // Extract metadata from fragments
    const fragments = document.querySelectorAll('.roslyn-diff-fragment');
    fragments.forEach(fragment => {
      const breakingChanges = parseInt(fragment.dataset.impactBreakingPublic);
      if (breakingChanges > 0) {
        fragment.classList.add('has-breaking-changes');
      }
    });
  </script>
</head>
<body>
  <h1>Pull Request #123</h1>
  <div class="diff-container">
    <?php include('fragments/src-Calculator.cs.html'); ?>
    <?php include('fragments/src-Service.cs.html'); ?>
  </div>
</body>
</html>
```

### Example 6: Inline View Comparison (v0.9.0)

```bash
# Traditional git-style diff view
roslyn-diff diff old.cs new.cs --html report.html --inline

# Compact inline view (3 lines of context)
roslyn-diff diff old.cs new.cs --html report.html --inline=3

# Inline view for entire PR
roslyn-diff diff --git-compare main..feature --html pr.html --inline=5
```

### Example 7: Folder Comparison with Filtering (v0.10.0)

```bash
# Compare two project directories
roslyn-diff diff project-v1/ project-v2/ --recursive

# Only C# files, exclude generated code
roslyn-diff diff old-src/ new-src/ \
  --recursive \
  --include "*.cs" \
  --exclude "*.g.cs" \
  --exclude "*.Designer.cs" \
  --html report.html

# Filter by path pattern
roslyn-diff diff old/ new/ \
  --include "src/**/*.cs" \
  --include "tests/**/*.cs" \
  --exclude "**/obj/**" \
  --exclude "**/bin/**"
```

### Example 8: Multi-Target Framework Analysis

```bash
# Analyze changes across multiple frameworks
roslyn-diff diff old.cs new.cs -t net8.0 -t net10.0 --html report.html

# Identify framework-specific changes
roslyn-diff diff old.cs new.cs -T "net462;net8.0;net10.0" --json analysis.json
```

### Example 9: Combined Features Workflow

```bash
# Complete PR analysis: git + filtering + inline + fragment + multi-TFM
roslyn-diff diff --git-compare main..feature \
  --include "src/**/*.cs" \
  --exclude "**/*.g.cs" \
  --recursive \
  -t net8.0 -t net10.0 \
  --inline=5 \
  --html-mode fragment \
  --html fragments/ \
  --json pr-analysis.json \
  --impact-level breaking-public

# Outputs:
# - fragments/*.html (one per file, embeddable)
# - fragments/roslyn-diff.css (shared stylesheet)
# - pr-analysis.json (structured data for automation)
```

## Programmatic Usage

roslyn-diff can be used as a library in your .NET applications:

```csharp
using RoslynDiff.Core.Differ;
using RoslynDiff.Core.Models;
using RoslynDiff.Output;

// Create differ factory
var factory = new DifferFactory();

// Get appropriate differ for file type
var differ = factory.GetDiffer("file.cs", new DiffOptions());

// Compare content
var result = differ.Compare(oldContent, newContent, new DiffOptions
{
    Mode = null, // Auto-detect
    IgnoreWhitespace = false,
    ContextLines = 3
});

// Format output
var formatterFactory = new OutputFormatterFactory();
var formatter = formatterFactory.GetFormatter("json");
var output = formatter.FormatResult(result, new OutputOptions
{
    PrettyPrint = true,
    IncludeContent = true
});
```

See [docs/api.md](docs/api.md) for complete API documentation.

## Migration Notes

### Hierarchical Output (v2.0+)

The diff engine now uses a recursive tree algorithm that produces **hierarchical output**. Changes to nested members (methods, properties) appear in the `Children` property of their parent change.

**Before (flat output):**
```json
{
  "changes": [
    { "kind": "Class", "name": "Calculator", "type": "Modified" },
    { "kind": "Method", "name": "Add", "type": "Modified" },
    { "kind": "Method", "name": "Multiply", "type": "Added" }
  ]
}
```

**After (hierarchical output):**
```json
{
  "changes": [
    {
      "kind": "Class",
      "name": "Calculator",
      "type": "Modified",
      "children": [
        { "kind": "Method", "name": "Add", "type": "Modified" },
        { "kind": "Method", "name": "Multiply", "type": "Added" }
      ]
    }
  ]
}
```

**For backward compatibility**, use `Flatten()` to get flat output:

```csharp
using RoslynDiff.Core.Models;

// Get hierarchical changes
var changes = differ.Compare(oldContent, newContent, options).FileChanges[0].Changes;

// Flatten for backward compatibility
var flatChanges = changes.Flatten().ToList();
```

This change fixes BUG-003 where duplicate nodes could be reported when using the old flat extraction method.

## Synaptic Canvas Integration

roslyn-diff is available as a skill in the Synaptic Canvas Claude Code plugin marketplace:

**[sc-roslyn-diff](https://github.com/randlee/synaptic-canvas/blob/develop/packages/sc-roslyn-diff/README.md)** - Claude Code integration for semantic .NET diff analysis

This plugin enables AI-powered code review and impact analysis:
- Automatically analyzes pull requests for semantic changes
- Generates natural language summaries of code modifications
- Highlights breaking changes and potential impact on consumers
- Integrates with GitHub workflows for automated code review

See the [Synaptic Canvas documentation](https://github.com/randlee/synaptic-canvas) for installation and usage instructions.

## Documentation

### Feature Guides

- **[Multi-File Comparison Guide](docs/multi-file-comparison.md)** - Git comparison, folder diff, filtering, and integration patterns (v0.10.0) *(coming soon)*
- **[Inline View Guide](docs/output-formats.md#inline-view)** - Line-by-line diff views, context modes, and use cases (v0.9.0)
- **[Fragment Mode Guide](docs/output-formats.md#fragment-mode)** - Embeddable HTML, integration patterns, and data attributes (v0.9.0)
- **[Impact Classification Guide](docs/impact-classification.md)** - Complete guide to impact levels, filtering, caveats, and use cases (v0.7.0)
- **[Whitespace Handling Guide](docs/whitespace-handling.md)** - Whitespace modes, language-aware detection, and best practices (v0.8.0)
- **[Multi-TFM Support Guide](docs/tfm-support.md)** - Multi-target framework analysis, conditional compilation detection, and advanced scenarios (v0.8.0)
- [Output Formats](docs/output-formats.md) - Format specifications, JSON Schema v3, and feature comparison
- [Sample Outputs](samples/README.md) - Example diffs with impact classification

### Reference Documentation

- [Usage Guide](docs/usage.md) - Detailed CLI usage and examples
- [API Reference](docs/api.md) - Programmatic API documentation
- [Architecture](docs/architecture.md) - Project structure and design
- [Screenshot Requirements](docs/screenshot-requirements.md) - Documentation screenshot specifications

## Requirements

- .NET 10.0 or later

## Dependencies

- [Microsoft.CodeAnalysis](https://www.nuget.org/packages/Microsoft.CodeAnalysis/) - Roslyn compiler platform
- [LibGit2Sharp](https://www.nuget.org/packages/LibGit2Sharp/) - Git integration for multi-file comparison (v0.10.0)
- [DiffPlex](https://www.nuget.org/packages/DiffPlex/) - Line-by-line diff algorithm
- [Spectre.Console](https://spectreconsole.net/) - Rich terminal output
- [System.CommandLine](https://www.nuget.org/packages/System.CommandLine/) - CLI framework

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Acknowledgments

- [Roslyn](https://github.com/dotnet/roslyn) - The .NET Compiler Platform
- [DiffPlex](https://github.com/mmanela/diffplex) - .NET Diff library
- [Spectre.Console](https://github.com/spectreconsole/spectre.console) - Beautiful console applications
