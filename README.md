# roslyn-diff

A semantic diff tool for .NET source code using Roslyn.

[![.NET 10](https://img.shields.io/badge/.NET-10.0-512BD4?style=flat-square&logo=dotnet)](https://dotnet.microsoft.com/)
[![License](https://img.shields.io/badge/License-MIT-blue?style=flat-square)](LICENSE)

## Overview

roslyn-diff provides semantic-aware diffing capabilities for .NET source code. Unlike traditional line-by-line diff tools, roslyn-diff understands code structure using the Roslyn compiler platform, enabling it to detect and report changes at the semantic level (classes, methods, properties, etc.).

## Features

- **Semantic Diff** - Understands code structure, not just text changes
- **C# and VB.NET Support** - Full semantic analysis for both languages
- **Structural Change Detection** - Identifies changes to classes, methods, properties, and fields
- **Rename Detection** - Detects when symbols are renamed (not just added/removed)
- **Move Detection** - Identifies code that has been moved within a file
- **Multiple Output Formats** - JSON, HTML, text, terminal
- **Class-to-Class Comparison** - Compare specific classes between files with flexible matching
- **Line-by-Line Fallback** - Supports non-.NET files with traditional diff

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
# Basic diff
roslyn-diff diff old.cs new.cs

# Output as JSON (for AI/tooling consumption)
roslyn-diff diff old.cs new.cs --output json

# Generate HTML report
roslyn-diff diff old.cs new.cs --output html --out-file report.html

# Rich terminal output with colors
roslyn-diff diff old.cs new.cs --rich
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

**Options:**
| Option | Short | Description | Default |
|--------|-------|-------------|---------|
| `--mode <mode>` | `-m` | Diff mode: `auto`, `roslyn`, `line` | `auto` |
| `--ignore-whitespace` | `-w` | Ignore whitespace differences | `false` |
| `--ignore-comments` | `-c` | Ignore comment differences (Roslyn mode only) | `false` |
| `--context <lines>` | `-C` | Lines of context to show | `3` |
| `--output <format>` | `-o` | Output format: `json`, `html`, `text`, `plain`, `terminal` | `text` |
| `--out-file <path>` | | Write output to file instead of stdout | |
| `--rich` | `-r` | Use rich terminal output with colors | `false` |

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

**Options:**
| Option | Short | Description | Default |
|--------|-------|-------------|---------|
| `--match-by <strategy>` | `-m` | Matching strategy: `exact`, `interface`, `similarity`, `auto` | `auto` |
| `--interface <name>` | `-i` | Interface name for interface matching | |
| `--similarity <threshold>` | `-s` | Similarity threshold (0.0-1.0) | `0.8` |
| `--output <format>` | `-o` | Output format: `json`, `html`, `text`, `plain`, `terminal` | `text` |
| `--out-file <path>` | `-f` | Write output to file | |

**Match Strategies:**
- `exact` - Match classes by exact name
- `interface` - Match classes implementing the specified interface
- `similarity` - Match by content similarity (for renamed classes)
- `auto` - Try exact name first, then similarity

## Output Formats

### JSON (`--output json`)

Machine-readable format, ideal for AI consumption and tooling integration.

```json
{
  "$schema": "roslyn-diff-output-v1",
  "metadata": {
    "tool": "roslyn-diff",
    "mode": "roslyn",
    "oldPath": "old/Calculator.cs",
    "newPath": "new/Calculator.cs"
  },
  "summary": {
    "totalChanges": 3,
    "additions": 2,
    "deletions": 0,
    "modifications": 1
  },
  "changes": [...]
}
```

### HTML (`--output html`)

Interactive HTML report with:
- Side-by-side diff view
- Syntax highlighting
- Collapsible sections
- Navigation and summary statistics

### Text (`--output text`)

Unified diff format, similar to `git diff`:

```diff
--- old/Calculator.cs
+++ new/Calculator.cs
@@ class Calculator @@
+ public int Multiply(int a, int b)
+ {
+     return a * b;
+ }
```

### Terminal (`--output terminal` or `--rich`)

Rich terminal output using Spectre.Console with:
- Color-coded changes (green for additions, red for deletions)
- Tree view for structural changes
- Summary tables

### Plain (`--output plain`)

Simple text output without ANSI codes, suitable for piping and redirection.

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
roslyn-diff diff before.cs after.cs --output json
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
roslyn-diff diff src/old.cs src/new.cs -o json --out-file diff.json

# HTML report for review
roslyn-diff diff src/old.cs src/new.cs -o html --out-file report.html
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
    IgnoreComments = false,
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

## Documentation

- [Usage Guide](docs/usage.md) - Detailed CLI usage and examples
- [Output Formats](docs/output-formats.md) - Format specifications and schemas
- [API Reference](docs/api.md) - Programmatic API documentation
- [Architecture](docs/architecture.md) - Project structure and design

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
