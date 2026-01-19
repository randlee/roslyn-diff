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

The JSON schema version depends on the roslyn-diff version:
- **v1 schema**: Used by roslyn-diff 0.6.x and earlier
- **v2 schema**: Used by roslyn-diff 0.7.0+, adds impact classification support

#### Schema v2 (0.7.0+)

```json
{
  "$schema": "roslyn-diff-output-v2",
  "metadata": {
    "tool": "roslyn-diff",
    "version": "0.7.0",
    "timestamp": "2026-01-19T10:30:00Z",
    "mode": "roslyn",
    "oldPath": "old/Service.cs",
    "newPath": "new/Service.cs",
    "options": {
      "includeContent": true,
      "contextLines": 3,
      "includeNonImpactful": false,
      "includeFormatting": false,
      "impactLevel": "breaking-internal"
    }
  },
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
    }
  },
  "fileChanges": [
    {
      "path": "new/Service.cs",
      "changes": [...]
    }
  ]
}
```

#### Schema v1 (Legacy)

```json
{
  "$schema": "roslyn-diff-output-v1",
  "metadata": {
    "tool": "roslyn-diff",
    "version": "0.5.0",
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

#### Schema v2 (0.7.0+)

```json
{
  "type": "modified",
  "kind": "method",
  "name": "ProcessOrder",
  "oldName": null,
  "impact": "breakingPublicApi",
  "visibility": "public",
  "caveats": [],
  "whitespaceIssues": [],
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

#### Schema v1 (Legacy)

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

**Schema v2 (0.7.0+)** includes impact breakdown:

| Field | Type | Description |
|-------|------|-------------|
| `totalChanges` | int | Total number of changes detected |
| `impactBreakdown` | object | Count of changes by impact level |
| `impactBreakdown.breakingPublicApi` | int | Changes breaking public API |
| `impactBreakdown.breakingInternalApi` | int | Changes breaking internal API |
| `impactBreakdown.nonBreaking` | int | Non-breaking changes |
| `impactBreakdown.formattingOnly` | int | Formatting-only changes |
| `typeBreakdown` | object | Count of changes by type |
| `typeBreakdown.additions` | int | Number of added elements |
| `typeBreakdown.deletions` | int | Number of removed elements |
| `typeBreakdown.modifications` | int | Number of modified elements |
| `typeBreakdown.renames` | int | Number of renamed elements |
| `typeBreakdown.moves` | int | Number of moved elements |

**Schema v1 (Legacy)**:

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

#### Schema v2 (0.7.0+)

```bash
# Get total changes
roslyn-diff diff old.cs new.cs -o json | jq '.summary.totalChanges'

# Check for breaking public API changes
roslyn-diff diff old.cs new.cs -o json | jq '.summary.impactBreakdown.breakingPublicApi'

# List all breaking changes with details
roslyn-diff diff old.cs new.cs -o json | jq '.fileChanges[].changes[] | select(.impact == "breakingPublicApi" or .impact == "breakingInternalApi")'

# List all added methods with impact level
roslyn-diff diff old.cs new.cs -o json | jq '.fileChanges[].changes[] | select(.type == "added" and .kind == "method") | {name, impact, visibility}'

# Check if changes are formatting-only
roslyn-diff diff old.cs new.cs -o json | jq -e '.summary.totalChanges == .summary.impactBreakdown.formattingOnly' && echo "Formatting only"

# Find changes with caveats
roslyn-diff diff old.cs new.cs -o json | jq '.fileChanges[].changes[] | select(.caveats | length > 0) | {name, caveats}'
```

#### Schema v1 (Legacy)

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
- **Keyboard navigation (v0.7.0+)**: Use `Ctrl+J` (next change) and `Ctrl+K` (previous change)

#### Impact Classification (v0.7.0+)

Visual indicators for change impact levels:
- **Breaking Public API**: Red badge with "BREAKING PUBLIC API" label
- **Breaking Internal API**: Orange badge with "BREAKING INTERNAL API" label
- **Non-Breaking**: Gray badge with "NON-BREAKING" label
- **Formatting Only**: Light gray badge with "FORMATTING ONLY" label

Impact badges appear next to the change type indicator.

#### Caveat Warnings (v0.7.0+)

Changes with caveats display yellow warning boxes:
- Warning icon (⚠) with descriptive text
- Examples: "Parameter rename may break named arguments", "Private member may break reflection"
- Positioned below the change content for visibility

#### Whitespace Issue Warnings (v0.8.0+)

Whitespace problems are highlighted with yellow warning indicators:
- **Indentation Changed**: Warns when indentation level changes (critical for Python/YAML)
- **Mixed Tabs/Spaces**: Flags lines with both tabs and spaces
- **Trailing Whitespace**: Highlights trailing whitespace changes
- **Line Ending Changed**: Warns when CRLF/LF line endings change

Warnings include tooltips explaining the issue and potential impact.

#### Interactive Features (v0.7.0+)

- **Copy buttons**: Quickly copy file path, change JSON, or unified diff
- **Collapsible sections**: Expand/collapse individual changes or entire files
- **Search highlighting**: Browser find (Ctrl+F) works within diff content

#### Styling

The HTML output is self-contained with embedded CSS:
- Dark/light friendly color scheme
- Responsive layout
- Print-friendly styling
- Color-coded impact badges
- Accessible contrast ratios

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

## New Features in v0.7.0 and v0.8.0

### Impact Classification (v0.7.0)

All output formats now include **impact classification** that categorizes changes by their potential impact on consumers:

#### Impact Levels

1. **BreakingPublicApi** - Changes to public API that break external consumers
2. **BreakingInternalApi** - Changes to internal API that break internal consumers
3. **NonBreaking** - Changes with no execution impact (renames, reordering)
4. **FormattingOnly** - Pure whitespace/comment changes

#### How Impact Appears in Each Format

**JSON**: Each change includes `impact`, `visibility`, and `caveats` fields:
```json
{
  "type": "modified",
  "kind": "method",
  "name": "Process",
  "impact": "breakingPublicApi",
  "visibility": "public",
  "caveats": ["Parameter rename may break callers using named arguments"]
}
```

**HTML**: Color-coded badges and warning boxes:
- Red badge: Breaking Public API
- Orange badge: Breaking Internal API
- Gray badge: Non-Breaking
- Light gray badge: Formatting Only
- Yellow warning boxes for caveats

**Terminal**: Color-coded labels with icons:
- 🔴 Breaking Public API (red)
- 🟠 Breaking Internal API (orange)
- ⚪ Non-Breaking (gray)
- ⚫ Formatting Only (dim)
- ⚠ Caveat warnings (yellow)

**Plain Text**: Text labels without colors:
```
[BREAKING PUBLIC API] Method: Process (modified)
  WARNING: Parameter rename may break callers using named arguments
```

#### Filtering by Impact

New CLI options control which changes appear in output:

```bash
# Show only breaking changes (default for JSON)
roslyn-diff diff old.cs new.cs -o json --impact-level breaking-internal

# Show all including formatting (default for HTML)
roslyn-diff diff old.cs new.cs -o html --impact-level all

# Include non-impactful changes in JSON
roslyn-diff diff old.cs new.cs -o json --include-non-impactful
```

See [Impact Classification Guide](impact-classification.md) for comprehensive details.

### Whitespace Handling (v0.8.0)

roslyn-diff now provides sophisticated **whitespace mode** options and automatic issue detection:

#### Whitespace Modes

1. **Exact** (default) - Character-by-character comparison, matches `diff` command
2. **IgnoreLeadingTrailing** - Trims whitespace from line ends, like `diff -b`
3. **IgnoreAll** - Collapses all whitespace to single spaces, like `diff -w`
4. **LanguageAware** - Adapts based on language (exact for Python/YAML, normalized for C#/Java)

```bash
# Default exact comparison
roslyn-diff diff old.cs new.cs

# Ignore leading/trailing whitespace
roslyn-diff diff old.cs new.cs --whitespace-mode ignore-leading-trailing

# Language-aware mode (smart handling)
roslyn-diff diff old.py new.py --whitespace-mode language-aware
```

#### Whitespace Issue Detection

In `LanguageAware` mode, roslyn-diff automatically detects and flags:

- **IndentationChanged** - Critical for Python, YAML, Makefiles
- **MixedTabsSpaces** - Dangerous in Python, bad practice everywhere
- **TrailingWhitespace** - Affects Markdown rendering, clutters diffs
- **LineEndingChanged** - CRLF ↔ LF changes
- **AmbiguousTabWidth** - Tab display may vary by editor

#### How Whitespace Issues Appear

**JSON**: Array of issue flags on each change:
```json
{
  "type": "modified",
  "oldContent": "    print('hello')",
  "newContent": "        print('hello')",
  "whitespaceIssues": ["indentationChanged"]
}
```

**HTML**: Yellow warning boxes with explanatory text:
```
⚠ Indentation changed (Python is whitespace-significant)
This change may affect program logic.
```

**Terminal**: Yellow warnings with icon:
```
⚠ WARNING: Indentation changed on line 15 (Python is whitespace-significant)
```

**Plain Text**: Text warnings:
```
WARNING: Indentation changed on line 15 (Python is whitespace-significant)
```

See [Whitespace Handling Guide](whitespace-handling.md) for comprehensive details.

### JSON Schema v2 Summary

The JSON schema was updated from v1 to v2 in roslyn-diff 0.7.0:

**What's new in v2**:
- Schema identifier: `"$schema": "roslyn-diff-output-v2"`
- Metadata includes `options` object with filtering settings
- Summary includes `impactBreakdown` and `typeBreakdown` objects
- Each change includes `impact`, `visibility`, `caveats`, `whitespaceIssues`
- All enum values use camelCase (e.g., `"modified"` instead of `"Modified"`)

**Backward compatibility**:
- All v1 fields remain unchanged
- New fields are purely additive
- Old consumers can safely ignore new fields
- Schema version clearly indicates capabilities

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
