# roslyn-diff

A semantic diff tool for .NET source code using Roslyn.

[![.NET 10](https://img.shields.io/badge/.NET-10.0-512BD4?style=flat-square&logo=dotnet)](https://dotnet.microsoft.com/)
[![License](https://img.shields.io/badge/License-MIT-blue?style=flat-square)](LICENSE)

## Overview

roslyn-diff provides semantic-aware diffing capabilities for .NET source code. Unlike traditional line-by-line diff tools, roslyn-diff understands code structure using the Roslyn compiler platform, enabling it to detect and report changes at the semantic level (classes, methods, properties, etc.).

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

### Multi-Target Framework Analysis (v0.9.0) 🎯

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
- **Multiple Output Formats** - JSON (schema v2), HTML (interactive), text, terminal
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

Compare two files and display the differences.

```bash
roslyn-diff diff <old-file> <new-file> [options]
```

**Arguments:**
- `<old-file>` - Path to the original file
- `<new-file>` - Path to the modified file

**Output Format Options (can combine multiple):**
| Option | Description |
|--------|-------------|
| `--json [file]` | JSON output (stdout if no file, or to specified file) |
| `--html <file>` | HTML report to file (required: file path) |
| `--text [file]` | Plain text diff (stdout if no file) |
| `--git [file]` | Git-style unified diff (stdout if no file) |
| `--open` | Open HTML in default browser after generation |

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

**Multi-TFM Options (v0.9.0):**
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

**Diff Modes:**
- `auto` - Automatically select based on file extension (.cs/.vb use Roslyn, others use line diff)
- `roslyn` - Force semantic diff (requires .cs or .vb file)
- `line` - Force line-by-line diff

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
# To stdout (for piping to jq, AI tools, etc.)
roslyn-diff diff old.cs new.cs --json

# To file
roslyn-diff diff old.cs new.cs --json analysis.json
```

```json
{
  "$schema": "roslyn-diff-output-v2",
  "metadata": {
    "version": "0.9.0",
    "timestamp": "2026-01-19T22:45:12Z",
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

See [Output Formats Guide](docs/output-formats.md) for complete schema documentation.

### HTML (`--html`)

Interactive HTML report with:
- **Impact classification badges** - Color-coded indicators (Breaking Public API, Breaking Internal API, Non-Breaking, Formatting Only)
- **TFM badges** - Framework-specific indicators (net8.0, net10.0) showing which TFMs each change applies to
- **Caveat warnings** - Yellow warning boxes for edge cases (e.g., "Parameter rename may break named arguments")
- **Whitespace issue indicators** - Warnings for indentation changes, mixed tabs/spaces, etc.
- Side-by-side diff view with syntax highlighting
- Keyboard navigation (Ctrl+J/K for next/previous change)
- Collapsible sections with copy buttons
- IDE integration links (VS Code, Rider, PyCharm, Zed)
- Navigation panel and summary statistics

```bash
# Generate and open in browser
roslyn-diff diff old.cs new.cs --html report.html --open

# Filter to show only breaking changes in HTML
roslyn-diff diff old.cs new.cs --html report.html --impact-level breaking-public
```

See [Output Formats Guide](docs/output-formats.md) for details on all HTML features.

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

## Supported Languages

### Semantic Diff (Roslyn)
- **C#** (`.cs`) - Full semantic analysis
- **Visual Basic** (`.vb`) - Full semantic analysis

### Line-by-Line Diff
- All other text files (`.txt`, `.md`, `.json`, `.xml`, `.html`, etc.)

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

- **[Impact Classification Guide](docs/impact-classification.md)** - Complete guide to impact levels, filtering, caveats, and use cases (v0.7.0)
- **[Whitespace Handling Guide](docs/whitespace-handling.md)** - Whitespace modes, language-aware detection, and best practices (v0.8.0)
- **[Multi-TFM Support Guide](docs/tfm-support.md)** - Multi-target framework analysis, conditional compilation detection, and advanced scenarios (v0.9.0)
- [Output Formats](docs/output-formats.md) - Format specifications, JSON Schema v2, and feature comparison
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
