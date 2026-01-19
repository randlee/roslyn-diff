# Whitespace Handling Guide

**Version:** 0.8.0+
**Last Updated:** January 2026

roslyn-diff provides sophisticated whitespace handling capabilities that go beyond simple "ignore whitespace" flags. The system includes multiple whitespace modes, language-aware detection, and automatic identification of potentially problematic whitespace issues.

## Table of Contents

- [Overview](#overview)
- [Whitespace Modes](#whitespace-modes)
- [Whitespace Issue Detection](#whitespace-issue-detection)
- [CLI Options](#cli-options)
- [Mode Selection Guide](#mode-selection-guide)
- [Examples](#examples)
- [Language Classification](#language-classification)

---

## Overview

Different languages treat whitespace differently. Python and YAML use indentation for semantics, while C# and Java treat whitespace as purely cosmetic. roslyn-diff understands these differences and can adapt its behavior accordingly.

### Key Capabilities

- **Four whitespace modes**: From exact comparison to language-aware handling
- **Issue detection**: Automatically flags mixed tabs/spaces, indentation changes, trailing whitespace
- **Language awareness**: Knows which languages care about whitespace and which don't
- **Backward compatible**: Existing `-w`/`--ignore-whitespace` continues to work
- **Visual warnings**: Whitespace issues displayed with color-coded warnings in all output formats

---

## Whitespace Modes

roslyn-diff supports four distinct whitespace handling modes:

### 1. Exact Mode (Default)

**Behavior**: Character-by-character comparison. Matches standard Unix `diff` command behavior.

**When to use**:
- Default mode for maximum compatibility
- When exact output matching `diff` is required
- When whitespace changes are significant
- For files where you're unsure of language semantics

**Example**:
```bash
roslyn-diff diff old.cs new.cs
# or explicitly:
roslyn-diff diff old.cs new.cs --whitespace-mode exact
```

**What counts as different**:
```
"hello"     vs "hello"      → Different (trailing space)
"  code"    vs " code"      → Different (leading space count)
"int x=1;"  vs "int x = 1;" → Different (spacing around =)
```

### 2. IgnoreLeadingTrailing Mode

**Behavior**: Ignores leading and trailing whitespace on each line. Equivalent to `diff -b`.

**When to use**:
- Code review where indentation differences don't matter
- Comparing files with inconsistent trailing whitespace
- When you care about content but not formatting

**Example**:
```bash
roslyn-diff diff old.cs new.cs --whitespace-mode ignore-leading-trailing
# or using backward-compatible flag:
roslyn-diff diff old.cs new.cs -w
roslyn-diff diff old.cs new.cs --ignore-whitespace
```

**What counts as different**:
```
"  hello  " vs "hello"      → Same (trimmed)
"hello"     vs "  hello  "  → Same (trimmed)
"a  b"      vs "a b"        → Different (internal whitespace)
"a\tb"      vs "a    b"     → Different (tab vs spaces internally)
```

### 3. IgnoreAll Mode

**Behavior**: Collapses all whitespace sequences to single spaces, then trims. Similar to `diff -w`.

**When to use**:
- Comparing generated code with different formatters
- When only non-whitespace content matters
- Detecting logic changes independent of formatting

**Example**:
```bash
roslyn-diff diff old.cs new.cs --whitespace-mode ignore-all
```

**What counts as different**:
```
"  hello  world  " vs "hello world"      → Same (collapsed)
"a\t\tb"           vs "a b"              → Same (collapsed)
"x    =    1;"     vs "x = 1;"           → Same (collapsed)
"hello"            vs "goodbye"          → Different (content changed)
```

### 4. LanguageAware Mode

**Behavior**: Adapts whitespace handling based on language. Preserves exact whitespace for whitespace-significant languages (Python, YAML), normalizes for others (C#, Java). Automatically detects and flags whitespace issues.

**When to use**:
- Mixed-language repositories
- When you want smart handling without manual configuration
- To detect dangerous whitespace changes in Python/YAML
- For comprehensive whitespace issue reporting

**Example**:
```bash
roslyn-diff diff old.py new.py --whitespace-mode language-aware
# Detects indentation changes and warns:
# ⚠ WARNING: Indentation changed on line 15 (Python is whitespace-significant)

roslyn-diff diff old.cs new.cs --whitespace-mode language-aware
# Treats C# formatting changes as cosmetic (no warnings)
```

**Language-specific behavior**:

| Language Category | Extensions | Behavior |
|------------------|------------|----------|
| **Whitespace-significant** | `.py`, `.yaml`, `.yml`, `Makefile` | Exact whitespace preserved, issues flagged |
| **Whitespace-insignificant** | `.cs`, `.java`, `.js`, `.go` | Whitespace normalized (like IgnoreAll) |
| **Unknown** | Other files | Exact comparison (safe default) |

---

## Whitespace Issue Detection

In `LanguageAware` mode, roslyn-diff automatically detects and flags five types of whitespace issues:

### 1. IndentationChanged

**Description**: Leading whitespace (indentation) level changed between old and new line.

**Critical for**: Python, YAML, Makefile, F#, Nim, CoffeeScript, Haml, Pug

**Example**:
```python
# Old
    print("hello")

# New
        print("hello")  # ⚠ Indentation changed from 4 to 8 spaces
```

**Impact**: Can break Python/YAML execution by changing block structure.

### 2. MixedTabsSpaces

**Description**: Line contains both tabs and spaces in indentation.

**Critical for**: All languages (style issue), especially Python (can cause IndentationError)

**Example**:
```python
# New line has mixed tabs/spaces
→   print("hello")  # ⚠ Mixed tabs and spaces detected
```

**Impact**: May cause Python IndentationError. Makes code display differently in different editors.

### 3. TrailingWhitespace

**Description**: Trailing whitespace added, removed, or changed.

**Critical for**: Markdown (affects line breaks), generally considered bad practice

**Example**:
```markdown
# Old
This is a line

# New
This is a line     # ⚠ Trailing whitespace added
```

**Impact**: Can affect Markdown rendering (two trailing spaces = line break). Generally clutters diffs.

### 4. LineEndingChanged

**Description**: Line ending style changed (CRLF ↔ LF ↔ CR).

**Critical for**: All files (git may auto-convert), shell scripts on Unix

**Example**:
```
# Old: Unix line endings (LF)
line1\n

# New: Windows line endings (CRLF)
line1\r\n  # ⚠ Line ending changed from LF to CRLF
```

**Impact**: Git may treat as changed. Shell scripts may fail on Unix if they have CRLF.

### 5. AmbiguousTabWidth

**Description**: Tab character present where visual width may be ambiguous.

**Critical for**: Files mixing tabs and spaces, languages with alignment-sensitive syntax

**Example**:
```python
def foo():
→   x = 1
→   →   y = 2  # ⚠ Tab width ambiguous (might display as 8 or 4 columns)
```

**Impact**: Code may display differently in different editors depending on tab width setting.

---

## CLI Options

### `--whitespace-mode <mode>`

Specify how whitespace differences should be handled.

**Values**:
- `exact` - Character-by-character comparison (default)
- `ignore-leading-trailing` - Trim whitespace from line ends
- `ignore-all` - Collapse all whitespace to single spaces
- `language-aware` - Adapt based on file type

**Examples**:
```bash
# Default exact mode
roslyn-diff diff old.cs new.cs

# Ignore indentation and trailing whitespace
roslyn-diff diff old.cs new.cs --whitespace-mode ignore-leading-trailing

# Ignore all whitespace differences
roslyn-diff diff old.cs new.cs --whitespace-mode ignore-all

# Smart language-based handling
roslyn-diff diff old.py new.py --whitespace-mode language-aware
```

### `--ignore-whitespace` / `-w`

Backward-compatible flag that maps to `--whitespace-mode ignore-leading-trailing`.

**Examples**:
```bash
# These are equivalent:
roslyn-diff diff old.cs new.cs -w
roslyn-diff diff old.cs new.cs --ignore-whitespace
roslyn-diff diff old.cs new.cs --whitespace-mode ignore-leading-trailing
```

### Precedence Rules

When multiple whitespace options are specified:

1. **`--ignore-whitespace` takes priority** over `--whitespace-mode`
2. Most recent flag wins if multiple `--whitespace-mode` flags are provided

**Examples**:
```bash
# -w overrides --whitespace-mode
roslyn-diff diff old.cs new.cs --whitespace-mode exact -w
# Result: Uses ignore-leading-trailing mode

# Later flag wins
roslyn-diff diff old.cs new.cs --whitespace-mode exact --whitespace-mode ignore-all
# Result: Uses ignore-all mode
```

---

## Mode Selection Guide

Use this decision tree to choose the right whitespace mode:

```
Are you comparing Python, YAML, or Makefile?
├─ Yes → Use --whitespace-mode language-aware
│         (Detects dangerous indentation changes)
└─ No
    ├─ Do you need exact diff compatibility?
    │  └─ Yes → Use default (exact mode)
    └─ No
        ├─ Do you care about internal whitespace?
        │  ├─ Yes → Use --whitespace-mode ignore-leading-trailing
        │  │         (Ignores indentation, preserves internal spacing)
        │  └─ No  → Use --whitespace-mode ignore-all
        │            (Treats all whitespace as cosmetic)
```

### By Use Case

| Use Case | Recommended Mode | Reasoning |
|----------|------------------|-----------|
| Code review (C#/Java) | `ignore-leading-trailing` | Formatting differences don't matter |
| Python refactoring | `language-aware` | Catch dangerous indentation changes |
| Generated code comparison | `ignore-all` | Different formatters produce different whitespace |
| Git pre-commit hook | `exact` | Detect all changes including formatting |
| CI/CD logic verification | `ignore-all` | Ensure logic unchanged despite formatting |
| Mixed-language repo | `language-aware` | Adapts to each language automatically |
| Documentation (Markdown) | `exact` or `language-aware` | Trailing spaces affect rendering |

---

## Examples

### Example 1: Python Indentation Change (Dangerous)

**Files**:
```python
# old.py
def process():
    if True:
        print("A")
        print("B")

# new.py
def process():
    if True:
        print("A")
    print("B")  # Dedented - now outside if block!
```

**Command**:
```bash
roslyn-diff diff old.py new.py --whitespace-mode language-aware
```

**Output**:
```
⚠ WARNING: Indentation changed on line 4 (Python is whitespace-significant)

- Line 4:         print("B")
+ Line 4:     print("B")

This change affects program logic. The statement is now outside the if block.
```

**Impact**: Logic bug introduced. Statement moved from inside to outside conditional.

### Example 2: C# Formatting Change (Cosmetic)

**Files**:
```csharp
// old.cs
public class Calculator
{
public int Add(int a,int b)
{
return a+b;
}
}

// new.cs
public class Calculator
{
    public int Add(int a, int b)
    {
        return a + b;
    }
}
```

**Command**:
```bash
roslyn-diff diff old.cs new.cs --whitespace-mode language-aware
```

**Output**:
```
No differences detected (whitespace changes only in brace-delimited language)
```

**Impact**: None. Language-aware mode recognizes C# doesn't care about formatting.

### Example 3: Mixed Tabs and Spaces Detection

**Files**:
```python
# old.py
def foo():
    return 1

# new.py
def foo():
→   return 1  # Tab + spaces mixed
```

**Command**:
```bash
roslyn-diff diff old.py new.py --whitespace-mode language-aware
```

**Output**:
```
⚠ WARNING: Mixed tabs and spaces detected on line 2

- Line 2:     return 1
+ Line 2: →   return 1

Python may raise IndentationError. Use consistent indentation (all spaces or all tabs).
```

### Example 4: Comparing Generated Code

**Files**:
```csharp
// generated-v1.cs (Formatter A)
public class Generated{public void Run(){Console.WriteLine("Hello");}}

// generated-v2.cs (Formatter B)
public class Generated
{
    public void Run()
    {
        Console.WriteLine("Hello");
    }
}
```

**Command**:
```bash
roslyn-diff diff generated-v1.cs generated-v2.cs --whitespace-mode ignore-all
```

**Output**:
```
No differences detected (content identical after whitespace normalization)
```

**Use case**: Verify different code generators produce logically identical output.

### Example 5: Trailing Whitespace in Markdown

**Files**:
```markdown
<!-- old.md -->
This is line 1
This is line 2

<!-- new.md -->
This is line 1
This is line 2
```

**Command**:
```bash
roslyn-diff diff old.md new.md --whitespace-mode exact
```

**Output**:
```
⚠ WARNING: Trailing whitespace added on line 1

- Line 1: This is line 1
+ Line 1: This is line 1

Note: In Markdown, two trailing spaces create a line break (<br>).
```

**Impact**: Changes rendering. First line now forces a line break before line 2.

### Example 6: Line Ending Normalization

**Files**:
```
# old.txt (Unix LF endings)
line1\n
line2\n

# new.txt (Windows CRLF endings)
line1\r\n
line2\r\n
```

**Command**:
```bash
# Exact mode detects difference
roslyn-diff diff old.txt new.txt --whitespace-mode exact
# Output: Line endings changed (LF → CRLF)

# Ignore-all mode normalizes line endings
roslyn-diff diff old.txt new.txt --whitespace-mode ignore-all
# Output: No differences detected
```

---

## Language Classification

roslyn-diff classifies languages into three categories for whitespace handling:

### Whitespace-Significant Languages

**Indentation defines code structure.**

| Language | Extensions | Significance |
|----------|-----------|--------------|
| Python | `.py`, `.pyw` | Indentation defines blocks |
| YAML | `.yaml`, `.yml` | Indentation defines nesting |
| Make | `Makefile`, `GNUmakefile` | Tabs required for recipes |
| F# | `.fs`, `.fsx`, `.fsi` | Indentation-based syntax |
| Nim | `.nim` | Indentation defines blocks |
| CoffeeScript | `.coffee` | Indentation defines blocks |
| Haml | `.haml` | Indentation defines nesting |
| Pug/Jade | `.pug`, `.jade` | Indentation defines nesting |
| Slim | `.slim` | Indentation defines structure |
| Sass | `.sass` | Indentation-based (not `.scss`) |
| Stylus | `.styl` | Indentation-based |

**LanguageAware mode behavior**: Preserves exact whitespace, flags all indentation changes as warnings.

### Whitespace-Insignificant Languages

**Whitespace is cosmetic.**

| Language | Extensions | Delimiters |
|----------|-----------|------------|
| C# | `.cs` | Braces `{ }` |
| VB.NET | `.vb` | Keywords (End If, etc) |
| Java | `.java` | Braces `{ }` |
| JavaScript | `.js`, `.jsx` | Braces `{ }` |
| TypeScript | `.ts`, `.tsx` | Braces `{ }` |
| Go | `.go` | Braces `{ }` |
| Rust | `.rs` | Braces `{ }` |
| C/C++ | `.c`, `.cpp`, `.h`, `.hpp` | Braces `{ }` |
| Swift | `.swift` | Braces `{ }` |
| Kotlin | `.kt`, `.kts` | Braces `{ }` |
| PHP | `.php` | Braces `{ }` |
| Scala | `.scala` | Braces `{ }` |
| JSON | `.json` | Braces/brackets |
| XML/HTML | `.xml`, `.html` | Tags `< >` |

**LanguageAware mode behavior**: Normalizes whitespace (like IgnoreAll mode), no warnings for formatting.

### Unknown Languages

**File type not recognized.**

**LanguageAware mode behavior**: Falls back to Exact mode for safety (preserves all whitespace).

### Detection Logic

Language detection uses file extension and filename:

1. Check if filename matches special names (e.g., `Makefile`)
2. Extract file extension (e.g., `.py`)
3. Look up extension in classification tables
4. Return `Significant`, `Insignificant`, or `Unknown`

---

## Output Format Support

All output formats display whitespace issues:

### JSON Format

Whitespace issues appear in the `whitespaceIssues` array on each change:

```json
{
  "type": "modified",
  "kind": "line",
  "oldContent": "    print('hello')",
  "newContent": "        print('hello')",
  "whitespaceIssues": [
    "indentationChanged"
  ]
}
```

Issue names in JSON:
- `indentationChanged`
- `mixedTabsSpaces`
- `trailingWhitespace`
- `lineEndingChanged`
- `ambiguousTabWidth`

### HTML Format

Whitespace warnings appear as yellow alert boxes below affected lines:

```html
<div class="change-item">
  <div class="change-content">
    <span class="removed">    print('hello')</span>
    <span class="added">        print('hello')</span>
  </div>
  <div class="whitespace-warning">
    ⚠ Indentation changed (Python is whitespace-significant)
  </div>
</div>
```

Styled with yellow background and warning icon.

### Terminal Format (Spectre.Console)

Whitespace warnings appear in yellow with warning icon:

```
- Line 4:     print('hello')
+ Line 4:         print('hello')

⚠ Indentation changed (Python is whitespace-significant)
```

### Plain Text Format

Whitespace warnings appear as plain text prefixed with WARNING:

```
- Line 4:     print('hello')
+ Line 4:         print('hello')

WARNING: Indentation changed (Python is whitespace-significant)
```

---

## Interaction with Other Features

### Impact Classification

Whitespace-only changes are classified as `FormattingOnly` impact:

```json
{
  "type": "modified",
  "impact": "formattingOnly",
  "whitespaceIssues": ["trailingWhitespace"]
}
```

Even if whitespace issues are detected, the impact remains `FormattingOnly` since no code logic changed.

### Context Lines

Context lines are subject to whitespace mode:

```bash
# Exact mode: Context lines must match exactly
roslyn-diff diff old.cs new.cs -C 3 --whitespace-mode exact

# Ignore-all: Context lines matched after normalization
roslyn-diff diff old.cs new.cs -C 3 --whitespace-mode ignore-all
```

### Semantic Diff (Roslyn Mode)

Whitespace mode **only affects line-based diff**. Roslyn semantic diff is inherently whitespace-agnostic (compares AST nodes, not text):

```bash
# Whitespace mode has no effect in roslyn mode
roslyn-diff diff old.cs new.cs --mode roslyn --whitespace-mode ignore-all
# (whitespace-mode ignored for semantic comparison)
```

---

## Best Practices

### 1. Use Language-Aware Mode by Default for Mixed Repos

```bash
# Safe for all file types
roslyn-diff diff old/ new/ --whitespace-mode language-aware
```

Automatically adapts to each language, flags issues only where they matter.

### 2. Use Exact Mode for Git Pre-Commit Hooks

```bash
# Catch all changes including formatting
roslyn-diff diff HEAD old-file.cs --whitespace-mode exact
```

Ensures no unintended whitespace changes slip through.

### 3. Use Ignore-All for Generated Code Testing

```bash
# Verify logic unchanged despite formatter differences
roslyn-diff diff generated-a.cs generated-b.cs --whitespace-mode ignore-all
```

Focus on semantic changes, ignore formatting artifacts.

### 4. Standardize Tab Width in Repos

Add `.editorconfig` to avoid mixed tabs/spaces:

```ini
[*.{py,cs,java}]
indent_style = space
indent_size = 4
```

### 5. Configure Git to Normalize Line Endings

Add `.gitattributes`:

```
* text=auto
*.cs text eol=lf
*.py text eol=lf
```

Prevents line ending issues across platforms.

---

## Limitations

### What Whitespace Modes Do NOT Handle

- **Unicode whitespace**: Only detects ASCII space, tab, CR, LF
- **Semantic whitespace in strings**: Whitespace inside string literals always preserved
- **IDE-specific formatting**: Cannot replicate specific formatter behavior
- **Custom language rules**: Only knows built-in language classifications

### Known Edge Cases

- **Python docstrings**: Triple-quoted string indentation may affect rendering
- **Markdown code blocks**: Indentation in fenced code blocks treated as content
- **Here-documents**: Shell here-docs preserve internal whitespace regardless of mode
- **YAML multiline strings**: Block scalar indentation rules are language-specific

---

## Further Reading

- [Impact Classification](impact-classification.md) - Understanding change impact levels
- [Output Formats](output-formats.md) - How whitespace warnings appear in different formats
- [Testing Strategy](testing.md) - How whitespace handling is tested

---

**Need help?** Open an issue at https://github.com/randlee/roslyn-diff/issues
