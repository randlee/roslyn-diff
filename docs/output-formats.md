# Output Formats

roslyn-diff supports multiple output formats to suit different use cases. This document describes each format in detail.

## Table of Contents

- [Overview](#overview)
- [JSON Format](#json-format)
- [HTML Format](#html-format)
- [Text Format](#text-format)
- [Plain Format](#plain-format)
- [Terminal Format](#terminal-format)
- [Choosing the Right Format](#choosing-the-right-format)

## Overview

| Format | Description | MIME Type | Best For |
|--------|-------------|-----------|----------|
| `json` | Structured machine-readable | `application/json` | AI, tooling, CI/CD |
| `html` | Interactive visual report | `text/html` | Code reviews, documentation |
| `text` | Unified diff format | `text/plain` | Quick review, git-like output |
| `plain` | Simple text (no ANSI) | `text/plain` | Piping, scripting |
| `terminal` | Rich console output | `text/plain` | Interactive terminal use |

---

## JSON Format

The JSON format provides a comprehensive, machine-readable representation of diff results. It's designed for AI consumption, tooling integration, and programmatic analysis.

### Usage

```bash
roslyn-diff diff old.cs new.cs -o json
roslyn-diff diff old.cs new.cs -o json --out-file diff.json
```

### Schema

```json
{
  "$schema": "roslyn-diff-output-v1",
  "metadata": {
    "tool": "roslyn-diff",
    "version": "1.0.0",
    "timestamp": "2025-01-14T20:00:00Z",
    "mode": "roslyn",
    "oldPath": "old/Service.cs",
    "newPath": "new/Service.cs"
  },
  "summary": {
    "totalChanges": 5,
    "additions": 2,
    "deletions": 1,
    "modifications": 2,
    "moves": 0,
    "renames": 0
  },
  "fileChanges": [
    {
      "path": "new/Service.cs",
      "changes": [...]
    }
  ]
}
```

### Change Object Structure

Each change in the `changes` array has the following structure:

```json
{
  "type": "Modified",
  "kind": "Method",
  "name": "ProcessOrder",
  "oldName": null,
  "oldLocation": {
    "file": "old/Service.cs",
    "startLine": 42,
    "endLine": 58,
    "startColumn": 5,
    "endColumn": 6
  },
  "newLocation": {
    "file": "new/Service.cs",
    "startLine": 45,
    "endLine": 65,
    "startColumn": 5,
    "endColumn": 6
  },
  "oldContent": "public void ProcessOrder(int orderId) { ... }",
  "newContent": "public async Task ProcessOrder(int orderId, CancellationToken ct) { ... }",
  "children": []
}
```

### Field Reference

#### Metadata

| Field | Type | Description |
|-------|------|-------------|
| `tool` | string | Tool name ("roslyn-diff") |
| `version` | string | Tool version |
| `timestamp` | string | ISO 8601 timestamp |
| `mode` | string | Diff mode used ("roslyn" or "line") |
| `oldPath` | string | Path to the original file |
| `newPath` | string | Path to the new file |

#### Summary

| Field | Type | Description |
|-------|------|-------------|
| `totalChanges` | int | Total number of changes detected |
| `additions` | int | Number of added elements |
| `deletions` | int | Number of removed elements |
| `modifications` | int | Number of modified elements |
| `moves` | int | Number of moved elements |
| `renames` | int | Number of renamed elements |

#### Change Types

| Value | Description |
|-------|-------------|
| `Added` | Element was added in the new version |
| `Removed` | Element was removed from the old version |
| `Modified` | Element exists in both but was changed |
| `Moved` | Element was moved to a different location |
| `Renamed` | Element was renamed |
| `Unchanged` | Element is the same (rarely included) |

#### Change Kinds

| Value | Description |
|-------|-------------|
| `File` | Entire file change |
| `Namespace` | Namespace declaration |
| `Class` | Class, struct, record, or interface |
| `Method` | Method or function |
| `Property` | Property |
| `Field` | Field |
| `Statement` | Individual statement |
| `Line` | Single line (line-based diff) |

### Example: Processing JSON with jq

```bash
# Get total changes
roslyn-diff diff old.cs new.cs -o json | jq '.summary.totalChanges'

# List all added methods
roslyn-diff diff old.cs new.cs -o json | jq '.fileChanges[].changes[] | select(.type == "Added" and .kind == "Method") | .name'

# Check for any deletions
roslyn-diff diff old.cs new.cs -o json | jq -e '.summary.deletions == 0' && echo "No deletions"
```

---

## HTML Format

The HTML format generates an interactive, self-contained HTML document with:
- Side-by-side diff view
- Syntax highlighting for C# and VB.NET
- Collapsible change sections
- Navigation sidebar
- Summary statistics

### Usage

```bash
roslyn-diff diff old.cs new.cs -o html --out-file report.html
```

### Features

#### Side-by-Side View

Changes are displayed in a side-by-side format:
- Left side: Original content (with deletions highlighted in red)
- Right side: New content (with additions highlighted in green)

#### Syntax Highlighting

The HTML formatter includes built-in syntax highlighting for:
- C# keywords and types
- VB.NET keywords and types
- String literals
- Comments
- Numeric literals

#### Navigation

For files with multiple changes:
- Sidebar navigation to jump to specific changes
- File-level sections for multi-file diffs
- Summary at the top showing statistics

#### Styling

The HTML output is self-contained with embedded CSS:
- Dark/light friendly color scheme
- Responsive layout
- Print-friendly styling

### Customization

The HTML template can be customized by modifying `src/RoslynDiff.Output/Templates/` (future feature).

---

## Text Format

The text format produces a unified diff output similar to `git diff` or the standard Unix `diff` command.

### Usage

```bash
roslyn-diff diff old.cs new.cs -o text
roslyn-diff diff old.cs new.cs  # text is the default
```

### Output Structure

```diff
--- old/Calculator.cs
+++ new/Calculator.cs
@@ class Calculator @@
 public class Calculator
 {
     public int Add(int a, int b) => a + b;
-    public int Subtract(int a, int b)
-    {
-        return a - b;
-    }
+    public int Subtract(int a, int b) => a - b;
+    public int Multiply(int a, int b) => a * b;
 }
```

### Line Prefixes

| Prefix | Meaning |
|--------|---------|
| `-` | Line removed from old file |
| `+` | Line added in new file |
| ` ` (space) | Unchanged context line |
| `@@ ... @@` | Section/hunk header |

### Context Control

Use the `--context` or `-C` option to control context lines:

```bash
# Show 5 lines of context
roslyn-diff diff old.cs new.cs -o text -C 5

# Show no context
roslyn-diff diff old.cs new.cs -o text -C 0
```

---

## Plain Format

The plain format produces simple text output without any ANSI escape codes or formatting. This is ideal for:
- Piping to other commands
- Writing to log files
- Processing with text tools

### Usage

```bash
roslyn-diff diff old.cs new.cs -o plain
roslyn-diff diff old.cs new.cs -o plain | grep "Method"
```

### Characteristics

- No color codes
- No special formatting
- Simple, parseable output
- Safe for all terminals and text processors

---

## Terminal Format

The terminal format uses Spectre.Console to produce rich, colorful output with:
- Color-coded changes (green for additions, red for deletions)
- Tree view for structural changes
- Tables for statistics
- Progress indicators for large diffs

### Usage

```bash
roslyn-diff diff old.cs new.cs -o terminal
roslyn-diff diff old.cs new.cs --rich
roslyn-diff diff old.cs new.cs -r
```

### Features

#### Color Coding

| Color | Meaning |
|-------|---------|
| Green | Added content |
| Red | Removed content |
| Yellow | Modified content |
| Blue | Moved content |
| Cyan | Renamed element |

#### Tree View

Structural changes are displayed in a hierarchical tree:

```
Calculator.cs
├── class Calculator
│   ├── [+] method Multiply
│   ├── [+] method Divide
│   └── [~] method Subtract
```

#### Statistics Table

A summary table shows change statistics:

```
┌────────────────┬───────┐
│ Change Type    │ Count │
├────────────────┼───────┤
│ Additions      │     2 │
│ Deletions      │     0 │
│ Modifications  │     1 │
│ Moves          │     0 │
│ Renames        │     0 │
├────────────────┼───────┤
│ Total Changes  │     3 │
└────────────────┴───────┘
```

### Requirements

The terminal format requires a terminal that supports ANSI escape codes. Most modern terminals support this, including:
- Windows Terminal
- PowerShell
- iTerm2
- GNOME Terminal
- VS Code integrated terminal

---

## Choosing the Right Format

### Decision Guide

```
Need machine-readable output?
├─ Yes → json
└─ No
    ├─ Need visual report for sharing?
    │   └─ Yes → html
    └─ No
        ├─ Using in terminal interactively?
        │   ├─ Want colors? → terminal
        │   └─ Simple output? → text
        └─ Piping/scripting?
            └─ plain
```

### Use Case Recommendations

| Use Case | Recommended Format |
|----------|-------------------|
| CI/CD integration | `json` |
| AI/LLM consumption | `json` |
| Code review | `html` |
| Quick terminal check | `text` or `terminal` |
| Git-like workflow | `text` |
| Shell scripting | `plain` or `json` |
| Log files | `plain` |
| Documentation | `html` |
| Email reports | `html` |
| Automated testing | `json` |

### Performance Considerations

| Format | Performance | Size |
|--------|-------------|------|
| `json` | Fast | Medium |
| `html` | Slower (template processing) | Larger |
| `text` | Fastest | Smallest |
| `plain` | Fastest | Smallest |
| `terminal` | Fast | Medium (ANSI codes) |

For large diffs with many changes, `text` or `plain` formats will be the fastest and produce the smallest output.
