# Inline View Samples

This directory contains sample HTML outputs demonstrating the inline view feature (v0.10.0).

## What is Inline View?

Inline view displays diffs line-by-line with +/- markers, similar to traditional git diff output, while maintaining roslyn-diff's semantic intelligence and impact classification features.

## Samples Overview

### calculator-inline-full.html

**Generated with:**
```bash
roslyn-diff diff Calculator.cs NewCalculator.cs --html calculator-inline-full.html --inline
```

**Features:**
- Full file view with all lines shown
- Added lines marked with `+` (green)
- Unchanged lines shown for complete context
- Syntax highlighting maintained
- Impact classification badges

**Best for:**
- Small to medium files
- Comprehensive code reviews
- Understanding changes in full context

---

### calculator-inline-context3.html

**Generated with:**
```bash
roslyn-diff diff Calculator.cs NewCalculator.cs --html calculator-inline-context3.html --inline=3
```

**Features:**
- Context mode with 3 lines around changes
- More compact than full file view
- Only shows relevant sections
- Similar to `git diff -U3`

**Best for:**
- Large files with isolated changes
- Quick reviews focused on changes
- Reducing scrolling and file size

---

### calculator-inline-context5.html

**Generated with:**
```bash
roslyn-diff diff Calculator.cs NewCalculator.cs --html calculator-inline-context5.html --inline=5
```

**Features:**
- Context mode with 5 lines around changes
- Balanced between full file and compact view
- Provides more context than 3-line mode

**Best for:**
- Medium to large files
- Understanding change context
- Standard review workflow

---

### impact-demo-inline.html

**Generated with:**
```bash
roslyn-diff diff old.cs new.cs --html impact-demo-inline.html --inline=5
```

**Features:**
- Demonstrates impact classification in inline view
- Breaking public API changes highlighted
- Breaking internal API changes shown
- Non-breaking and formatting changes included
- Caveat warnings for subtle impacts

**Best for:**
- Understanding impact classification
- API review workflows
- Identifying breaking changes

---

### calculator-inline-fragment.html

**Generated with:**
```bash
roslyn-diff diff Calculator.cs NewCalculator.cs --html calculator-inline-fragment.html --html-mode fragment --inline=5
```

**Features:**
- HTML fragment mode (embeddable)
- External CSS file (roslyn-diff.css)
- Data attributes for JavaScript integration
- Inline view with 5 lines of context

**Best for:**
- Embedding in existing web pages
- Integration with dashboards
- Documentation sites
- Code review platforms

## Comparison: Tree View vs. Inline View

| Feature | Tree View | Inline View |
|---------|-----------|-------------|
| **Display** | Hierarchical by type | Line-by-line sequential |
| **Best For** | Structural changes | Traditional diff workflow |
| **Context** | Changed elements only | Full file or N lines |
| **Navigation** | Jump to classes/methods | Scroll through code |
| **Familiarity** | Unique to roslyn-diff | Similar to git diff |

## Usage Examples

### Full File View

```bash
# Show entire file with inline markers
roslyn-diff diff old.cs new.cs --html report.html --inline
```

### Context View

```bash
# Show 3 lines of context
roslyn-diff diff old.cs new.cs --html report.html --inline=3

# Show 5 lines of context
roslyn-diff diff old.cs new.cs --html report.html --inline=5

# Show 10 lines of context
roslyn-diff diff old.cs new.cs --html report.html --inline=10
```

### Combined with Other Options

```bash
# Inline view with impact filtering
roslyn-diff diff old.cs new.cs --html report.html --inline --impact-level breaking-public

# Inline view with fragment mode
roslyn-diff diff old.cs new.cs --html fragment.html --html-mode fragment --inline=5

# Inline view with formatting changes included
roslyn-diff diff old.cs new.cs --html report.html --inline=3 --include-formatting

# Inline view with multi-TFM analysis
roslyn-diff diff old.cs new.cs --html report.html --inline=5 -t net8.0 -t net10.0
```

## When to Use Inline View

**Choose Inline View When:**
- You're familiar with git diff and prefer line-by-line review
- You need to see exact line-level changes including whitespace
- You're reviewing patches or generating documentation
- You want a traditional diff format with semantic intelligence

**Choose Tree View When:**
- You're focused on structural/API changes
- You need to quickly identify breaking changes
- You're reviewing large changesets with many files
- You want to jump directly to specific classes or methods

## Regenerating Samples

To regenerate these samples after code changes:

```bash
cd src/RoslynDiff.Cli

# Full file view
dotnet run --framework net10.0 -- diff \
  ../../samples/before/Calculator.cs \
  ../../samples/after/Calculator.cs \
  --html ../../samples/inline-view/calculator-inline-full.html \
  --inline

# Context view (3 lines)
dotnet run --framework net10.0 -- diff \
  ../../samples/before/Calculator.cs \
  ../../samples/after/Calculator.cs \
  --html ../../samples/inline-view/calculator-inline-context3.html \
  --inline=3

# Context view (5 lines)
dotnet run --framework net10.0 -- diff \
  ../../samples/before/Calculator.cs \
  ../../samples/after/Calculator.cs \
  --html ../../samples/inline-view/calculator-inline-context5.html \
  --inline=5

# Impact demo
dotnet run --framework net10.0 -- diff \
  ../../samples/impact-demo/old.cs \
  ../../samples/impact-demo/new.cs \
  --html ../../samples/inline-view/impact-demo-inline.html \
  --inline=5

# Fragment mode
dotnet run --framework net10.0 -- diff \
  ../../samples/before/Calculator.cs \
  ../../samples/after/Calculator.cs \
  --html ../../samples/inline-view/calculator-inline-fragment.html \
  --html-mode fragment \
  --inline=5
```

## Version History

- v0.10.0: Initial release of inline view feature
