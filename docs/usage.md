# Usage Guide

This guide provides comprehensive documentation for using roslyn-diff from the command line.

## Table of Contents

- [Getting Started](#getting-started)
- [Commands](#commands)
  - [diff Command](#diff-command)
  - [class Command](#class-command)
  - [Multi-File Comparison](#multi-file-comparison)
- [Output Formats](#output-formats)
- [Advanced Usage](#advanced-usage)
- [Tips and Best Practices](#tips-and-best-practices)

## Getting Started

### Basic Usage

The most common usage is comparing two source files:

```bash
roslyn-diff diff old.cs new.cs
```

This will:
1. Automatically detect the file type from the extension
2. Use Roslyn semantic diff for `.cs` files
3. Output a unified diff to the console

### Help

Get help for any command:

```bash
roslyn-diff --help
roslyn-diff diff --help
roslyn-diff class --help
```

## Commands

### diff Command

Compare two files and display the differences.

#### Syntax

```bash
roslyn-diff diff <old-file> <new-file> [options]
```

#### Arguments

| Argument | Description |
|----------|-------------|
| `<old-file>` | Path to the original (old) file |
| `<new-file>` | Path to the modified (new) file |

#### Options

##### Mode Selection (`-m`, `--mode`)

Controls how the diff is performed:

```bash
# Auto-detect mode (default)
roslyn-diff diff old.cs new.cs -m auto

# Force Roslyn semantic diff
roslyn-diff diff old.cs new.cs -m roslyn

# Force line-by-line diff
roslyn-diff diff old.cs new.cs -m line
```

**Mode values:**
- `auto` - Automatically select based on file extension
  - `.cs` files use C# Roslyn differ
  - `.vb` files use VB.NET Roslyn differ
  - All other files use line-by-line diff
- `roslyn` - Force semantic diff (only works with `.cs` and `.vb` files)
- `line` - Force line-by-line diff (works with any text file)

##### Ignore Whitespace (`-w`, `--ignore-whitespace`)

Ignore whitespace differences when comparing:

```bash
roslyn-diff diff old.cs new.cs -w
roslyn-diff diff old.cs new.cs --ignore-whitespace
```

This is useful when:
- Comparing files with different indentation styles
- Ignoring trailing whitespace changes
- Comparing files reformatted by different tools

##### Ignore Comments (`-c`, `--ignore-comments`)

Ignore comment differences (only effective in Roslyn mode):

```bash
roslyn-diff diff old.cs new.cs -c
roslyn-diff diff old.cs new.cs --ignore-comments
```

This is useful when:
- Focusing on code logic changes only
- Ignoring documentation updates
- Comparing files with different comment styles

##### Context Lines (`-C`, `--context`)

Control how many lines of unchanged context appear around changes:

```bash
# Show 5 lines of context
roslyn-diff diff old.cs new.cs -C 5
roslyn-diff diff old.cs new.cs --context 5

# Show no context
roslyn-diff diff old.cs new.cs -C 0
```

Default: `3`

##### Output Format (`-o`, `--output`)

Specify the output format:

```bash
roslyn-diff diff old.cs new.cs -o json
roslyn-diff diff old.cs new.cs --output html
```

**Available formats:**
- `text` - Unified diff format (default)
- `json` - Machine-readable JSON
- `html` - Interactive HTML report
- `plain` - Plain text without ANSI codes
- `terminal` - Rich terminal output with colors

##### Output File (`--out-file`)

Write output to a file instead of stdout:

```bash
roslyn-diff diff old.cs new.cs -o json --out-file diff.json
roslyn-diff diff old.cs new.cs -o html --out-file report.html
```

##### Rich Output (`-r`, `--rich`)

Enable rich terminal output with colors and formatting:

```bash
roslyn-diff diff old.cs new.cs --rich
roslyn-diff diff old.cs new.cs -r
```

This uses Spectre.Console for enhanced visual presentation.

#### Examples

```bash
# Basic comparison
roslyn-diff diff before/Service.cs after/Service.cs

# JSON output for CI/CD integration
roslyn-diff diff old.cs new.cs -o json --out-file result.json

# HTML report with custom context
roslyn-diff diff old.cs new.cs -o html -C 5 --out-file report.html

# Compare ignoring whitespace and comments
roslyn-diff diff old.cs new.cs -w -c

# Force line diff on a .cs file
roslyn-diff diff old.cs new.cs -m line

# Rich terminal output
roslyn-diff diff old.cs new.cs --rich
```

---

### class Command

Compare specific classes between two files. This is useful for:
- Comparing specific classes in large files
- Tracking refactored classes across renames
- Comparing different implementations of an interface

#### Syntax

```bash
roslyn-diff class <old-spec> <new-spec> [options]
```

#### Arguments

| Argument | Description |
|----------|-------------|
| `<old-spec>` | Old file specification |
| `<new-spec>` | New file specification |

**Specification format:**
- `file.cs:ClassName` - Specify a particular class
- `file.cs` - Use the first class in the file (or auto-match)

#### Options

##### Match Strategy (`-m`, `--match-by`)

Control how classes are matched between files:

```bash
roslyn-diff class old.cs new.cs --match-by exact
roslyn-diff class old.cs new.cs -m similarity
```

**Match strategies:**
- `exact` - Match classes by exact name only
- `interface` - Match classes that implement a specified interface
- `similarity` - Match classes by content similarity (useful for renamed classes)
- `auto` - Try exact match first, fall back to similarity (default)

##### Interface Name (`-i`, `--interface`)

Specify the interface name when using interface matching:

```bash
roslyn-diff class old.cs new.cs --match-by interface --interface IRepository
roslyn-diff class old.cs new.cs -m interface -i IUserService
```

##### Similarity Threshold (`-s`, `--similarity`)

Set the content similarity threshold (0.0 to 1.0):

```bash
roslyn-diff class old.cs new.cs --match-by similarity --similarity 0.7
roslyn-diff class old.cs new.cs -m similarity -s 0.9
```

Default: `0.8` (80% similarity)

##### Output Format (`-o`, `--output`)

Same as the diff command:

```bash
roslyn-diff class old.cs:Foo new.cs:Foo -o json
```

##### Output File (`-f`, `--out-file`)

Write output to a file:

```bash
roslyn-diff class old.cs:Foo new.cs:Foo -o html -f comparison.html
```

#### Examples

```bash
# Compare same-named classes
roslyn-diff class before/Service.cs:UserService after/Service.cs:UserService

# Compare classes with different names (explicit)
roslyn-diff class old.cs:OldName new.cs:NewName

# Auto-find matching class in new file
roslyn-diff class old.cs:UserService new.cs --match-by similarity

# Find class implementing interface
roslyn-diff class old.cs new.cs --match-by interface --interface IRepository

# High similarity threshold for precise matching
roslyn-diff class old.cs:Foo new.cs --match-by similarity --similarity 0.95

# Generate JSON comparison
roslyn-diff class old.cs:Service new.cs:Service -o json --out-file class-diff.json
```

---

### Multi-File Comparison

Compare multiple files in a single operation using git comparison or folder comparison.

**Version**: 0.10.0+

#### Git Branch Comparison

Compare files between two git references (branches, tags, or commits):

```bash
# Compare branches
roslyn-diff diff --git-compare main..feature --json changes.json

# Compare tags
roslyn-diff diff --git-compare v1.0.0..v2.0.0 --json release.json

# Compare commits
roslyn-diff diff --git-compare abc123..def456 --json diff.json
```

**Ref Range Syntax**: `<old-ref>..<new-ref>`
- `main..feature` - Compare main branch to feature branch
- `v1.0.0..v2.0.0` - Compare two tags
- `HEAD~5..HEAD` - Compare current HEAD to 5 commits ago

#### Folder Comparison

Compare all files in two directories:

```bash
# Auto-detected (both arguments are directories)
roslyn-diff diff old/ new/ --json changes.json

# Recursive comparison
roslyn-diff diff old/ new/ --recursive --json changes.json
```

**Recursive Option**:
- Default: Non-recursive (top-level files only)
- `--recursive` or `-r`: Traverse all subdirectories

#### File Filtering

Use glob patterns to include or exclude specific files:

```bash
# Include only C# files
roslyn-diff diff old/ new/ --include "*.cs" --json changes.json

# Exclude generated files
roslyn-diff diff old/ new/ \
  --include "*.cs" \
  --exclude "*.g.cs" \
  --exclude "*.Designer.cs" \
  --json changes.json

# Multiple patterns
roslyn-diff diff --git-compare main..feature \
  --include "src/**/*.cs" \
  --include "src/**/*.vb" \
  --exclude "bin/**" \
  --exclude "obj/**" \
  --json changes.json
```

**Glob Pattern Syntax**:
- `*` - Matches any characters (except directory separator)
- `**` - Matches any number of directories recursively
- `?` - Matches single character

**Common Patterns**:
- `*.cs` - All C# files in current directory
- `**/*.cs` - All C# files recursively
- `src/**/*.cs` - All C# files under src/
- `**/bin/**` - All files in any bin directory
- `**/*.g.cs` - All generated C# files

**Filter Precedence**:
1. Include patterns (OR logic - match any)
2. Exclude patterns (takes precedence)
3. If no include patterns: all files are candidates

#### Multi-File Output

Multi-file comparison uses JSON schema v3:

```json
{
  "$schema": "roslyn-diff-output-v3",
  "metadata": {
    "mode": "multi-file",
    "comparisonMode": "git",
    "gitRefRange": "main..feature"
  },
  "summary": {
    "totalFiles": 15,
    "modifiedFiles": 8,
    "addedFiles": 5,
    "removedFiles": 2,
    "totalChanges": 47
  },
  "files": [
    {
      "oldPath": "src/Calculator.cs",
      "newPath": "src/Calculator.cs",
      "status": "modified",
      "result": { /* DiffResult */ }
    }
  ]
}
```

**Note**: HTML output for multi-file is not yet implemented in v0.10.0.

#### Multi-File Examples

**PR Review**:
```bash
roslyn-diff diff --git-compare main..pr-branch \
  --include "src/**/*.cs" \
  --impact-level breaking-public \
  --json pr-review.json
```

**Release Comparison**:
```bash
roslyn-diff diff --git-compare v1.0.0..v2.0.0 \
  --include "src/**/*.cs" \
  --exclude "**/*.g.cs" \
  --json release-changes.json
```

**Folder Migration Analysis**:
```bash
roslyn-diff diff old-project/ new-project/ \
  --recursive \
  --include "*.cs" \
  --exclude "bin/**" \
  --exclude "obj/**" \
  --json migration.json
```

For complete documentation, see [Multi-File Comparison Guide](./multi-file-comparison.md).

---

## Output Formats

### text (default)

Produces unified diff format similar to `git diff`:

```bash
roslyn-diff diff old.cs new.cs -o text
```

Output:
```diff
--- old/Calculator.cs
+++ new/Calculator.cs
@@ class Calculator @@
- public int Subtract(int a, int b)
- {
-     return a - b;
- }
+ public int Subtract(int a, int b) => a - b;
```

### json

Machine-readable JSON format for integration with other tools:

```bash
roslyn-diff diff old.cs new.cs -o json
```

### html

Interactive HTML report:

```bash
roslyn-diff diff old.cs new.cs -o html --out-file report.html
```

### plain

Plain text without ANSI escape codes:

```bash
roslyn-diff diff old.cs new.cs -o plain
```

### terminal

Rich terminal output with colors (requires terminal support):

```bash
roslyn-diff diff old.cs new.cs -o terminal
# or
roslyn-diff diff old.cs new.cs --rich
```

---

## Advanced Usage

### Combining Options

Options can be combined for fine-grained control:

```bash
# Full-featured comparison
roslyn-diff diff old.cs new.cs \
    --ignore-whitespace \
    --ignore-comments \
    --context 5 \
    --output html \
    --out-file detailed-report.html
```

### Pipeline Integration

Use plain text format for piping to other tools:

```bash
# Pipe to grep
roslyn-diff diff old.cs new.cs -o plain | grep "Method"

# Count changes
roslyn-diff diff old.cs new.cs -o json | jq '.summary.totalChanges'
```

### Scripting

Use JSON output for script processing:

```bash
#!/bin/bash

# Get diff as JSON
diff_result=$(roslyn-diff diff old.cs new.cs -o json)

# Extract statistics
total=$(echo "$diff_result" | jq '.summary.totalChanges')
additions=$(echo "$diff_result" | jq '.summary.additions')

echo "Total changes: $total"
echo "Additions: $additions"
```

### CI/CD Integration

Generate reports for continuous integration:

```bash
# In your CI script
roslyn-diff diff src/old.cs src/new.cs -o json --out-file diff-report.json

# Check for breaking changes
roslyn-diff diff old.cs new.cs -o json | jq -e '.summary.deletions == 0'
```

---

## Tips and Best Practices

### 1. Choose the Right Mode

- Use `auto` (default) for most cases
- Use `roslyn` when you specifically need semantic analysis
- Use `line` for non-.NET files or when you want simple text diff

### 2. Use Appropriate Output Format

| Use Case | Recommended Format |
|----------|-------------------|
| Quick review | `text` or `terminal` |
| CI/CD pipelines | `json` |
| Code reviews | `html` |
| Scripting/piping | `plain` |

### 3. Adjust Context Lines

- Use more context (`-C 5` or higher) for complex changes
- Use less context (`-C 0` or `-C 1`) for quick overview

### 4. Handle Large Files

For large files with many changes:
1. Use `class` command to focus on specific classes
2. Generate HTML report for easier navigation
3. Use JSON format for programmatic analysis

### 5. Compare Refactored Code

When classes have been renamed:
```bash
roslyn-diff class old.cs:OldClassName new.cs --match-by similarity
```

When matching by interface:
```bash
roslyn-diff class old.cs new.cs --match-by interface --interface IService
```

### 6. Batch Processing

Process multiple files with a shell loop:

```bash
for file in src/*.cs; do
    old_file="old/$file"
    if [ -f "$old_file" ]; then
        roslyn-diff diff "$old_file" "$file" -o json --out-file "diffs/$(basename $file).json"
    fi
done
```

### 7. Debugging Unexpected Results

If the diff seems incorrect:
1. Try line mode (`-m line`) to see raw differences
2. Check if whitespace (`-w`) or comments (`-c`) are affecting results
3. Increase context (`-C 10`) for more visibility
