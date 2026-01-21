# Multi-Target Framework Support Guide

**Version:** 0.8.0+
**Last Updated:** January 2026

roslyn-diff includes comprehensive support for analyzing code changes across multiple .NET target frameworks (TFMs). This allows developers to understand how changes behave differently across framework versions, particularly when using conditional compilation (`#if`, `#elif`) or framework-specific APIs.

## Table of Contents

- [Overview](#overview)
- [Why Multi-TFM Analysis](#why-multi-tfm-analysis)
- [Supported Target Frameworks](#supported-target-frameworks)
- [CLI Usage](#cli-usage)
- [How It Works](#how-it-works)
- [Output Formats](#output-formats)
- [Advanced Scenarios](#advanced-scenarios)
- [Performance Considerations](#performance-considerations)
- [Troubleshooting](#troubleshooting)
- [Best Practices](#best-practices)
- [FAQ](#faq)

---

## Overview

Multi-TFM analysis enables roslyn-diff to:

1. **Analyze code across multiple target frameworks simultaneously** - Compare how code changes affect different .NET versions
2. **Detect conditional compilation** - Identify changes within `#if NET8_0`, `#if NET10_0_OR_GREATER`, etc.
3. **Resolve framework-specific symbols** - Automatically map TFMs to their preprocessor symbols
4. **Track TFM-specific changes** - Each change indicates which frameworks it applies to
5. **Optimize performance** - Pre-scan detects preprocessor directives, skips multi-TFM analysis when not needed

### Key Features

- **Automatic symbol resolution**: Maps `net8.0` → `NET8_0`, `NET8_0_OR_GREATER`, etc.
- **Dependency chain support**: Understands that `NET10_0` implies `NET5_0_OR_GREATER` through `NET10_0_OR_GREATER`
- **Parallel processing**: Analyzes multiple TFMs concurrently for performance
- **Smart defaults**: Assumes NET10_0 when no TFM specified
- **Pre-scan optimization**: Detects `#if` directives before performing expensive multi-TFM analysis

---

## Why Multi-TFM Analysis

### Problem: Framework-Specific Code Changes

Modern .NET libraries often target multiple frameworks to support various runtime environments. Code may use conditional compilation to provide different implementations:

```csharp
public class DataProcessor
{
#if NET8_0
    public void Process(Span<byte> data)
    {
        // .NET 8 implementation using Span<T>
    }
#elif NET10_0
    public void Process(ReadOnlySpan<byte> data)
    {
        // .NET 10 implementation with improved API
    }
#else
    public void Process(byte[] data)
    {
        // Fallback implementation for older frameworks
    }
#endif
}
```

**Without multi-TFM analysis**, a traditional diff would:
- Show changes to all conditional blocks, making it unclear which frameworks are affected
- Fail to identify that a change only impacts specific TFMs
- Require manual inspection to understand framework-specific behavior

**With multi-TFM analysis**, roslyn-diff:
- Compiles the code for each specified TFM
- Identifies which changes apply to which frameworks
- Labels each change with its applicable TFMs (e.g., `[net8.0]`, `[net10.0]`)
- Provides clear understanding of framework-specific impacts

### Use Cases

1. **Multi-targeted library development** - Understand how changes affect different .NET versions
2. **Migration planning** - Identify code that differs between old and new framework versions
3. **API evolution tracking** - See which framework-specific APIs are being adopted or deprecated
4. **Code review** - Quickly spot framework-specific changes that need special attention
5. **CI/CD validation** - Ensure changes work correctly across all supported frameworks

---

## Supported Target Frameworks

roslyn-diff supports all major .NET target framework monikers:

### .NET 5+ (Modern .NET)

| TFM | Preprocessor Symbols | OR_GREATER Symbols |
|-----|---------------------|-------------------|
| `net5.0` | `NET5_0` | `NET5_0_OR_GREATER` |
| `net6.0` | `NET6_0` | `NET5_0_OR_GREATER`, `NET6_0_OR_GREATER` |
| `net7.0` | `NET7_0` | `NET5_0_OR_GREATER` through `NET7_0_OR_GREATER` |
| `net8.0` | `NET8_0` | `NET5_0_OR_GREATER` through `NET8_0_OR_GREATER` |
| `net9.0` | `NET9_0` | `NET5_0_OR_GREATER` through `NET9_0_OR_GREATER` |
| `net10.0` | `NET10_0` | `NET5_0_OR_GREATER` through `NET10_0_OR_GREATER` |

**Note:** Modern .NET TFMs automatically include all `OR_GREATER` symbols from version 5.0 up to and including the current version.

### .NET Framework

| TFM Format | Examples | Preprocessor Symbols |
|------------|----------|---------------------|
| 3-digit | `net20`, `net35`, `net40`, `net45`, `net46`, `net47`, `net48` | `NET20`, `NET35`, etc. + `NETFRAMEWORK` |
| Dotted | `net4.5`, `net4.6.2`, `net4.7.2`, `net4.8` | `NET45`, `NET462`, etc. + `NETFRAMEWORK` |

**Supported versions:** .NET Framework 2.0 through 4.8

### .NET Core

| TFM | Preprocessor Symbols |
|-----|---------------------|
| `netcoreapp1.0` | `NETCOREAPP1_0`, `NETCOREAPP` |
| `netcoreapp1.1` | `NETCOREAPP1_1`, `NETCOREAPP` |
| `netcoreapp2.0` | `NETCOREAPP2_0`, `NETCOREAPP` |
| `netcoreapp2.1` | `NETCOREAPP2_1`, `NETCOREAPP` |
| `netcoreapp2.2` | `NETCOREAPP2_2`, `NETCOREAPP` |
| `netcoreapp3.0` | `NETCOREAPP3_0`, `NETCOREAPP` |
| `netcoreapp3.1` | `NETCOREAPP3_1`, `NETCOREAPP` |

### .NET Standard

| TFM | Preprocessor Symbols |
|-----|---------------------|
| `netstandard1.0` through `netstandard1.6` | `NETSTANDARD1_0` through `NETSTANDARD1_6` + `NETSTANDARD` |
| `netstandard2.0` | `NETSTANDARD2_0`, `NETSTANDARD` |
| `netstandard2.1` | `NETSTANDARD2_1`, `NETSTANDARD` |

### Symbol Dependency Chains

Modern .NET (5+) uses `OR_GREATER` symbols to indicate version ranges:

```csharp
// When analyzing net8.0, these symbols are defined:
// - NET8_0
// - NET5_0_OR_GREATER
// - NET6_0_OR_GREATER
// - NET7_0_OR_GREATER
// - NET8_0_OR_GREATER

#if NET8_0_OR_GREATER
    // This code is active for net8.0, net9.0, net10.0, etc.
#endif

#if NET8_0
    // This code is ONLY active for net8.0
#endif
```

---

## CLI Usage

### Basic Single TFM Analysis

```bash
# Analyze for .NET 8.0
roslyn-diff diff old.cs new.cs -t net8.0

# Analyze for .NET 10.0 (using long form)
roslyn-diff diff old.cs new.cs --target-framework net10.0
```

### Multiple TFM Analysis (Repeatable Flag)

```bash
# Analyze for both .NET 8.0 and .NET 10.0
roslyn-diff diff old.cs new.cs -t net8.0 -t net10.0

# Analyze three frameworks
roslyn-diff diff old.cs new.cs -t net8.0 -t net9.0 -t net10.0
```

### Multiple TFM Analysis (Semicolon-Separated)

```bash
# Equivalent to repeating -t flag
roslyn-diff diff old.cs new.cs -T "net8.0;net10.0"

# Multiple frameworks
roslyn-diff diff old.cs new.cs --target-frameworks "net8.0;net9.0;net10.0"
```

**Note:** The `-t` and `-T` flags cannot be used together. Choose one style.

### Default Behavior (No TFM Specified)

When no TFM is specified, roslyn-diff assumes `NET10_0` with all applicable `OR_GREATER` symbols:

```bash
# Implicitly uses NET10_0 with NET5_0_OR_GREATER through NET10_0_OR_GREATER
roslyn-diff diff old.cs new.cs
```

This default ensures that modern code using `#if NET8_0_OR_GREATER` and similar conditions is analyzed correctly.

### Output with TFM Analysis

```bash
# JSON output with TFM information
roslyn-diff diff old.cs new.cs -t net8.0 -t net10.0 --json

# HTML report with TFM badges
roslyn-diff diff old.cs new.cs -T "net8.0;net10.0" --html report.html --open

# Plain text with TFM labels
roslyn-diff diff old.cs new.cs -t net8.0 -t net10.0 --text
```

### Class Comparison with TFM

```bash
# Compare specific classes across TFMs
roslyn-diff class old.cs:Service new.cs:Service -t net8.0 -t net10.0

# Auto-match with TFM analysis
roslyn-diff class old.cs new.cs --match-by similarity -T "net8.0;net10.0"
```

---

## How It Works

### Architecture Overview

Multi-TFM analysis in roslyn-diff follows this process:

```
1. Pre-scan (Preprocessor Directive Detection)
   ↓
2. TFM Symbol Resolution
   ↓
3. Parallel Compilation (one per TFM)
   ↓
4. Syntax Tree Comparison
   ↓
5. Result Merging
   ↓
6. TFM-Specific Change Annotation
```

### 1. Pre-scan: Preprocessor Directive Detection

**Purpose:** Optimize performance by detecting if multi-TFM analysis is needed.

**How it works:**
- Fast text-based scan for `#if`, `#elif`, `#else`, `#endif`, `#define`, `#undef` directives
- Conservative approach: allows false positives (directives in comments/strings)
- Zero false negatives: never misses actual preprocessor directives

**Performance impact:**
- O(n) scan where n is file size
- Minimal allocations
- Skips expensive multi-TFM compilation when no preprocessor directives exist

```csharp
// File with preprocessor directives → multi-TFM analysis performed
#if NET8_0
public void Foo() { }
#endif

// File without directives → single-TFM analysis (faster)
public void Bar() { }
```

### 2. TFM Symbol Resolution

**Purpose:** Map TFM strings to their preprocessor symbols.

**Symbol resolution examples:**

```
net8.0  →  ["NET8_0", "NET5_0_OR_GREATER", "NET6_0_OR_GREATER",
            "NET7_0_OR_GREATER", "NET8_0_OR_GREATER"]

net462  →  ["NET462", "NETFRAMEWORK"]

netcoreapp3.1  →  ["NETCOREAPP3_1", "NETCOREAPP"]

netstandard2.0  →  ["NETSTANDARD2_0", "NETSTANDARD"]
```

**OR_GREATER chain generation (for .NET 5+):**
- Extract version number from TFM (e.g., `net8.0` → 8)
- Generate all `OR_GREATER` symbols from 5 to current version
- Include base symbol (e.g., `NET8_0`)

### 3. Parallel Compilation

**Purpose:** Compile code for each TFM concurrently for performance.

**Process:**
1. Create separate `CSharpParseOptions` for each TFM
2. Configure preprocessor symbols for each TFM
3. Parse syntax trees in parallel using `Parallel.ForEach`
4. Each TFM gets its own isolated compilation context

**Example:** Analyzing 3 TFMs compiles code 3 times in parallel:
```
TFM: net8.0  → Parse with NET8_0, NET5_0_OR_GREATER, ... NET8_0_OR_GREATER
TFM: net9.0  → Parse with NET9_0, NET5_0_OR_GREATER, ... NET9_0_OR_GREATER
TFM: net10.0 → Parse with NET10_0, NET5_0_OR_GREATER, ... NET10_0_OR_GREATER
```

### 4. Syntax Tree Comparison

**Purpose:** Compare the compiled syntax trees for each TFM.

**Key insight:** Preprocessor directives are evaluated during parsing, so different TFMs produce different syntax trees:

```csharp
// Source code
#if NET8_0
public void OldMethod() { }
#elif NET10_0
public void NewMethod() { }
#endif

// Syntax tree for net8.0
public void OldMethod() { }

// Syntax tree for net10.0
public void NewMethod() { }
```

The comparison detects that `OldMethod` exists only in `net8.0` and `NewMethod` exists only in `net10.0`.

### 5. Result Merging

**Purpose:** Combine per-TFM results into a unified diff.

**Merging logic:**
1. Compare old and new syntax trees for each TFM
2. Group identical changes across TFMs
3. Deduplicate changes that appear in multiple TFMs
4. Annotate each change with applicable TFMs

**Example:**

```csharp
// Change appears in both TFMs → merged
Change: "Method Add modified"
ApplicableToTfms: ["net8.0", "net10.0"]

// Change appears in only one TFM → TFM-specific
Change: "Method NewFeature added"
ApplicableToTfms: ["net10.0"]
```

### 6. TFM-Specific Change Annotation

**Purpose:** Label changes with the frameworks they affect.

Each `Change` object includes an `ApplicableToTfms` property:

```json
{
  "type": "Added",
  "kind": "Method",
  "name": "ProcessDataAsync",
  "applicableToTfms": ["net10.0"],
  "newContent": "public async Task ProcessDataAsync() { ... }"
}
```

If `applicableToTfms` is `null` or omitted, the change applies to all analyzed TFMs.

---

## Output Formats

### JSON Output

Multi-TFM information appears in two places:

**1. Metadata (analyzed TFMs):**

```json
{
  "$schema": "roslyn-diff-output-v2",
  "metadata": {
    "version": "0.8.0",
    "analyzedTfms": ["net8.0", "net10.0"],
    "options": {
      "targetFrameworks": ["net8.0", "net10.0"]
    }
  }
}
```

**2. Per-change annotations:**

```json
{
  "changes": [
    {
      "type": "Added",
      "kind": "Method",
      "name": "NewMethod",
      "applicableToTfms": ["net10.0"],
      "visibility": "public"
    },
    {
      "type": "Modified",
      "kind": "Method",
      "name": "Update",
      "applicableToTfms": ["net8.0", "net10.0"],
      "visibility": "public"
    }
  ]
}
```

**If `applicableToTfms` is missing or `null`:** The change applies to all analyzed TFMs.

### HTML Output

Multi-TFM information is displayed as colored badges next to each change:

```html
<div class="change">
  <span class="change-type added">Added</span>
  <span class="change-kind">Method</span>
  <span class="change-name">ProcessDataAsync</span>
  <span class="tfm-badge net10">net10.0</span>
</div>
```

**Visual representation:**
- **Blue badge**: `net8.0`
- **Green badge**: `net9.0`
- **Purple badge**: `net10.0`
- Multiple badges when change applies to multiple TFMs

**Summary section:**
```
Target Frameworks Analyzed: net8.0, net10.0

Framework-Specific Changes:
  net8.0 only: 2 changes
  net10.0 only: 5 changes
  Both: 8 changes
```

### Text Output

TFM information appears in headers and change annotations:

```
Diff: old.cs -> new.cs
Mode: Roslyn (semantic)
Target Frameworks: net8.0, net10.0

Summary: 15 total changes

Changes:
  File: new.cs
    [+] Method: ProcessDataAsync (line 42-56) [net10.0]
    [~] Method: Update (line 15-28) [net8.0, net10.0]
    [-] Method: LegacyProcess (line 30-40) [net8.0]
```

**Legend:**
- `[+]` - Added
- `[-]` - Removed
- `[~]` - Modified
- `[>>]` - Moved
- `[→]` - Renamed

TFM labels in `[net8.0, net10.0]` format indicate which frameworks the change affects.

---

## Advanced Scenarios

### Scenario 1: Framework-Specific API Usage

**Problem:** Code uses different APIs depending on framework version.

**Example:**

```csharp
public class StringHelper
{
#if NET8_0_OR_GREATER
    public static bool Contains(string text, string value)
    {
        return text.Contains(value, StringComparison.OrdinalIgnoreCase);
    }
#else
    public static bool Contains(string text, string value)
    {
        return text.IndexOf(value, StringComparison.OrdinalIgnoreCase) >= 0;
    }
#endif
}
```

**Analysis:**

```bash
roslyn-diff diff old.cs new.cs -t net462 -t net8.0
```

**Result:** Shows that `net8.0` uses `Contains` while `net462` uses `IndexOf`, making the framework-specific implementation clear.

### Scenario 2: Migration from .NET Framework to .NET 8

**Problem:** Identify code that behaves differently when migrating from Framework to modern .NET.

**Analysis:**

```bash
roslyn-diff diff net-framework-version.cs net8-version.cs -t net48 -t net8.0
```

**Use case:**
- See which `#if NETFRAMEWORK` blocks are being removed
- Identify new `#if NET8_0_OR_GREATER` additions
- Understand API differences between frameworks

### Scenario 3: Progressive Enhancement Across Versions

**Problem:** Library adds features for newer frameworks while maintaining backward compatibility.

**Example:**

```csharp
public class Logger
{
    public void Log(string message)
    {
        Console.WriteLine(message);
    }

#if NET9_0_OR_GREATER
    public void LogStructured(LogLevel level, string message, params object[] args)
    {
        // Structured logging available in .NET 9+
    }
#endif

#if NET10_0_OR_GREATER
    public async Task LogAsync(string message, CancellationToken ct)
    {
        // Async logging with cancellation in .NET 10+
    }
#endif
}
```

**Analysis:**

```bash
roslyn-diff diff v1.0/Logger.cs v2.0/Logger.cs -T "net8.0;net9.0;net10.0"
```

**Result:**
- Shows `LogStructured` added for `net9.0` and `net10.0`
- Shows `LogAsync` added only for `net10.0`
- Baseline `Log` method present in all frameworks

### Scenario 4: OR_GREATER Symbol Usage

**Problem:** Understanding how `OR_GREATER` symbols affect multiple frameworks.

**Example:**

```csharp
#if NET8_0_OR_GREATER
public void ModernFeature()
{
    // Available in .NET 8, 9, 10, etc.
}
#endif
```

**Analysis:**

```bash
roslyn-diff diff old.cs new.cs -t net8.0 -t net9.0 -t net10.0
```

**Result:** `ModernFeature` annotated with `[net8.0, net9.0, net10.0]` - shows it applies to all three.

### Scenario 5: Framework-Specific Breaking Changes

**Problem:** A change breaks public API, but only for certain frameworks.

**Example:**

```csharp
// Old version
public void Process(string data) { }

// New version
#if NET10_0_OR_GREATER
public void Process(ReadOnlySpan<char> data) { }
#else
public void Process(string data) { }
#endif
```

**Analysis:**

```bash
roslyn-diff diff old.cs new.cs -t net8.0 -t net10.0 --json
```

**Result:**

```json
{
  "changes": [
    {
      "type": "Modified",
      "kind": "Method",
      "name": "Process",
      "applicableToTfms": ["net10.0"],
      "impact": "breakingPublicApi",
      "visibility": "public",
      "caveats": ["Parameter type changed from string to ReadOnlySpan<char>"]
    }
  ]
}
```

**Insight:** The breaking change only affects `net10.0` consumers, not `net8.0`.

---

## Performance Considerations

### When Multi-TFM Analysis is Triggered

Multi-TFM analysis involves compiling code multiple times. To optimize performance, roslyn-diff uses intelligent detection:

**Pre-scan triggers:**
1. **Preprocessor directive detection**: File contains `#if`, `#elif`, `#else`, `#endif`, `#define`, `#undef`
2. **Explicit TFM specification**: User provides `-t` or `-T` flags

**Pre-scan skips:**
1. **No preprocessor directives detected**: Single-TFM analysis performed (default NET10_0)
2. **No TFM flags provided + no directives**: Fast path with minimal overhead

### Performance Optimization Strategies

**1. Parallel TFM Processing**

```csharp
// Pseudo-code
Parallel.ForEach(targetFrameworks, tfm =>
{
    var symbols = TfmSymbolResolver.GetPreprocessorSymbols(tfm);
    var parseOptions = CSharpParseOptions.Default.WithPreprocessorSymbols(symbols);
    var tree = CSharpSyntaxTree.ParseText(content, parseOptions);
    // Compare...
});
```

**Speedup:** Near-linear scaling with CPU cores (analyzing 4 TFMs on 4 cores ≈ same time as 1 TFM).

**2. Conservative Preprocessor Detection**

```csharp
// Fast character-by-character scan
if (!content.Contains('#'))
    return false; // Fast path: no directives possible

// Scan for #if, #elif, #else, #endif, #define, #undef
// Allows false positives (directives in strings/comments) for simplicity
```

**Trade-off:** May occasionally perform multi-TFM analysis when not strictly necessary, but never misses actual directives.

**3. Result Deduplication**

Identical changes across TFMs are merged:

```
Instead of:
  - Change A (net8.0)
  - Change A (net9.0)
  - Change A (net10.0)

Produces:
  - Change A (net8.0, net9.0, net10.0)
```

**Savings:** Reduced memory usage and faster output generation.

### Performance Benchmarks

Approximate performance characteristics (measured on MacBook Pro M1, .NET 10):

| Scenario | File Size | TFMs | Time | Notes |
|----------|-----------|------|------|-------|
| No preprocessor directives | 10 KB | 1 (default) | ~50ms | Fast path |
| No preprocessor directives | 10 KB | 3 specified | ~50ms | Pre-scan skip |
| With preprocessor directives | 10 KB | 1 | ~100ms | Single compilation |
| With preprocessor directives | 10 KB | 3 | ~120ms | Parallel compilation |
| With preprocessor directives | 100 KB | 3 | ~500ms | Larger file |
| With preprocessor directives | 1 MB | 5 | ~2s | Very large file |

**Key insight:** Multi-TFM overhead is primarily in parsing, not comparison. Parallel processing keeps total time low.

---

## Troubleshooting

### Issue 1: TFM Not Recognized

**Error:**

```
Error: Invalid TFM 'net8': Invalid TFM format: 'net8'.
Expected format is 'net8.0', 'net462', 'netcoreapp3.1', or 'netstandard2.0'.
```

**Cause:** Modern .NET TFMs require a version number with a dot (e.g., `net8.0`, not `net8`).

**Solution:**

```bash
# Wrong
roslyn-diff diff old.cs new.cs -t net8

# Correct
roslyn-diff diff old.cs new.cs -t net8.0
```

### Issue 2: Missing TFM-Specific Changes

**Problem:** Changes exist in code with preprocessor directives, but output doesn't show TFM differences.

**Possible causes:**

1. **No TFM specified**: Default NET10_0 is used
   ```bash
   # May not show differences if code has #if NET8_0
   roslyn-diff diff old.cs new.cs

   # Solution: specify TFMs
   roslyn-diff diff old.cs new.cs -t net8.0 -t net10.0
   ```

2. **Preprocessor directive uses undefined symbols**
   ```csharp
   #if CUSTOM_SYMBOL
   // This won't be detected by TFM analysis
   #endif
   ```
   **Solution:** TFM analysis only resolves standard framework symbols. Custom symbols require custom build configuration.

### Issue 3: Performance Slow with Many TFMs

**Problem:** Analysis takes a long time when specifying 5+ TFMs.

**Explanation:** Each TFM requires a full parse of the syntax tree. More TFMs = more work.

**Solutions:**

1. **Limit TFMs to only those you care about:**
   ```bash
   # Instead of analyzing all 6 versions
   roslyn-diff diff old.cs new.cs -T "net5.0;net6.0;net7.0;net8.0;net9.0;net10.0"

   # Analyze only key versions
   roslyn-diff diff old.cs new.cs -t net8.0 -t net10.0
   ```

2. **Use OR_GREATER symbols in code** to reduce TFM-specific branches:
   ```csharp
   // Instead of separate blocks for each version
   #if NET8_0_OR_GREATER
   // Code for .NET 8+
   #endif
   ```

### Issue 4: Unexpected Default Behavior

**Problem:** Analysis shows `NET10_0` symbols when no TFM specified.

**Explanation:** This is intentional. When no TFM is specified, roslyn-diff assumes the latest supported framework (NET10_0) with all applicable `OR_GREATER` symbols.

**Why:** Modern code often uses `#if NET8_0_OR_GREATER` and similar patterns. Defaulting to NET10_0 ensures these are analyzed correctly.

**Solution:** If you need a specific TFM, always specify it explicitly:

```bash
roslyn-diff diff old.cs new.cs -t net8.0
```

### Issue 5: Cannot Mix -t and -T Flags

**Error:**

```
Error: Cannot specify both --target-framework (-t) and --target-frameworks (-T).
Use one or the other.
```

**Cause:** The repeatable `-t` and semicolon-separated `-T` flags are mutually exclusive.

**Solution:** Choose one style:

```bash
# Option 1: Repeatable flag (recommended for CLI)
roslyn-diff diff old.cs new.cs -t net8.0 -t net10.0

# Option 2: Semicolon-separated (recommended for scripts)
roslyn-diff diff old.cs new.cs -T "net8.0;net10.0"
```

---

## Best Practices

### 1. Specify TFMs Explicitly for Clarity

**Recommendation:** Always specify TFMs when analyzing multi-targeted code.

```bash
# Good: Clear intent
roslyn-diff diff old.cs new.cs -t net8.0 -t net10.0

# Avoid: Relies on default behavior
roslyn-diff diff old.cs new.cs
```

**Why:** Explicit TFMs make it clear what you're analyzing and prevent surprises from default behavior changes.

### 2. Use Semicolon-Separated Format in Scripts

**Recommendation:** Use `-T` flag in CI/CD scripts for easier variable substitution.

```bash
# Easy to parameterize
TFMS="net8.0;net9.0;net10.0"
roslyn-diff diff old.cs new.cs -T "$TFMS"

# Harder to parameterize
roslyn-diff diff old.cs new.cs -t net8.0 -t net9.0 -t net10.0
```

### 3. Limit TFM Analysis to Relevant Frameworks

**Recommendation:** Only analyze frameworks you actively support.

```bash
# If you support .NET 8 and .NET 10
roslyn-diff diff old.cs new.cs -t net8.0 -t net10.0

# Don't analyze every version unnecessarily
roslyn-diff diff old.cs new.cs -T "net5.0;net6.0;net7.0;net8.0;net9.0;net10.0"
```

**Why:** Reduces analysis time and output noise.

### 4. Combine with Impact Classification

**Recommendation:** Use multi-TFM analysis with impact filtering to focus on breaking changes.

```bash
# Show only breaking changes across TFMs
roslyn-diff diff old.cs new.cs -t net8.0 -t net10.0 --impact-level breaking-public --json
```

**Use case:** Identify framework-specific breaking changes during API reviews.

### 5. Use HTML Output for Complex Multi-TFM Diffs

**Recommendation:** HTML format provides the best visualization of TFM-specific changes.

```bash
roslyn-diff diff old.cs new.cs -T "net8.0;net9.0;net10.0" --html report.html --open
```

**Why:**
- Color-coded TFM badges make it easy to see which frameworks are affected
- Summary section shows framework-specific change counts
- Side-by-side diff with TFM annotations

### 6. Prefer OR_GREATER Symbols in Code

**Recommendation:** Use `OR_GREATER` symbols instead of exact version checks when possible.

```csharp
// Good: Works for current and future versions
#if NET8_0_OR_GREATER
public void ModernFeature() { }
#endif

// Avoid: Requires update for each new version
#if NET8_0 || NET9_0 || NET10_0
public void ModernFeature() { }
#endif
```

**Why:** Reduces maintenance burden and makes TFM analysis more meaningful.

### 7. Document TFM-Specific Changes in Commit Messages

**Recommendation:** When changes affect specific frameworks, mention it in commit messages.

```
feat: Add ProcessDataAsync for .NET 10+

- Implements async processing using new .NET 10 APIs
- Only available in net10.0 target (requires #if NET10_0_OR_GREATER)
- Maintains synchronous Process method for older frameworks

TFMs affected: net10.0
```

### 8. Validate Multi-TFM Changes in CI

**Recommendation:** Run roslyn-diff with TFM analysis in CI to catch framework-specific issues.

```yaml
# GitHub Actions example
- name: Analyze Multi-TFM Changes
  run: |
    roslyn-diff diff main-version.cs pr-version.cs \
      -T "net8.0;net10.0" \
      --json changes.json \
      --impact-level breaking-public

    # Fail if breaking changes detected
    if grep -q '"breakingPublicApi": [1-9]' changes.json; then
      echo "Breaking changes detected!"
      exit 1
    fi
```

---

## FAQ

### Q: What does "NET10_0 assumed" mean?

**A:** When you don't specify any TFMs, roslyn-diff analyzes the code as if it's compiled for .NET 10.0. This means the following symbols are defined:

- `NET10_0`
- `NET5_0_OR_GREATER`
- `NET6_0_OR_GREATER`
- `NET7_0_OR_GREATER`
- `NET8_0_OR_GREATER`
- `NET9_0_OR_GREATER`
- `NET10_0_OR_GREATER`

This default ensures modern code patterns using `#if NET8_0_OR_GREATER` are analyzed correctly.

### Q: Can I analyze custom preprocessor symbols?

**A:** No, roslyn-diff only supports standard .NET TFM symbols. Custom symbols like `DEBUG`, `RELEASE`, or project-specific symbols are not currently supported via the `-t` flag.

**Workaround:** Use the programmatic API to configure custom preprocessor symbols directly via `CSharpParseOptions`.

### Q: How do I know if multi-TFM analysis was performed?

**A:** Check the output:

**JSON:** Look for `analyzedTfms` in metadata:
```json
{
  "metadata": {
    "analyzedTfms": ["net8.0", "net10.0"]
  }
}
```

**Text/HTML:** Look for "Target Frameworks:" in the header:
```
Target Frameworks: net8.0, net10.0
```

If these are missing, single-TFM analysis was performed (default NET10_0).

### Q: Why does my change show applicableToTfms: null?

**A:** This means the change applies to **all** analyzed TFMs. Only changes that differ between TFMs get specific `applicableToTfms` values.

**Example:**

```csharp
// This change applies to all TFMs → applicableToTfms: null
public void CommonMethod() { }

// This change only in net10.0 → applicableToTfms: ["net10.0"]
#if NET10_0
public void Net10Feature() { }
#endif
```

### Q: Can I analyze .NET Framework and modern .NET together?

**A:** Yes! You can mix any supported TFMs:

```bash
roslyn-diff diff old.cs new.cs -t net48 -t net8.0
```

This is useful for migration scenarios where you're comparing behavior between .NET Framework and modern .NET.

### Q: Does multi-TFM analysis work with VB.NET?

**A:** Yes, multi-TFM analysis supports both C# and VB.NET. The preprocessor directive detection is case-insensitive to handle VB.NET's case-insensitive nature.

### Q: What's the difference between -t and -T?

**A:** Both specify target frameworks:

- **`-t` / `--target-framework`**: Repeatable flag for individual TFMs
  ```bash
  roslyn-diff diff old.cs new.cs -t net8.0 -t net10.0
  ```

- **`-T` / `--target-frameworks`**: Semicolon-separated TFMs in a single value
  ```bash
  roslyn-diff diff old.cs new.cs -T "net8.0;net10.0"
  ```

**Choose `-T` for:** Scripts and CI/CD (easier to parameterize)

**Choose `-t` for:** Interactive CLI use (more readable)

### Q: How do OR_GREATER symbols work?

**A:** `OR_GREATER` symbols are automatically generated for .NET 5+ TFMs:

```csharp
// When analyzing net8.0:
#if NET8_0             // TRUE (exact match)
#if NET8_0_OR_GREATER  // TRUE (8 >= 8)
#if NET6_0_OR_GREATER  // TRUE (8 >= 6)
#if NET10_0_OR_GREATER // FALSE (8 < 10)
```

roslyn-diff automatically includes all applicable `OR_GREATER` symbols when you specify a TFM.

### Q: Can I see which files have preprocessor directives?

**A:** Not directly via CLI output, but you can infer it:

- If `analyzedTfms` is present and you specified TFMs → preprocessor directives detected
- If no TFMs specified but `analyzedTfms` is null → no preprocessor directives

**Tip:** Use `grep` to find files with directives:
```bash
grep -l "^#if" *.cs
```

### Q: What happens if I specify an invalid TFM?

**A:** roslyn-diff validates TFMs and returns an error:

```
Error: Invalid TFM 'net8': Invalid TFM format: 'net8'.
Expected format is 'net8.0', 'net462', 'netcoreapp3.1', or 'netstandard2.0'.
```

Validation occurs before any analysis begins, so no partial results are generated.

### Q: Is there a limit on the number of TFMs I can analyze?

**A:** No hard limit, but practical considerations:

- **Performance:** Each TFM requires a full parse. 10+ TFMs may be slow.
- **Memory:** Each TFM's syntax tree is kept in memory during analysis.
- **Output size:** More TFMs = larger JSON output with more `applicableToTfms` annotations.

**Recommendation:** Limit to 3-5 TFMs for typical use cases.

### Q: Can I use multi-TFM analysis with the `class` command?

**A:** Yes! The `class` command supports the same `-t` and `-T` flags:

```bash
roslyn-diff class old.cs:Service new.cs:Service -t net8.0 -t net10.0
```

This analyzes class-specific changes across multiple TFMs.

---

## See Also

- [Impact Classification Guide](impact-classification.md) - Understand change impact levels
- [Output Formats](output-formats.md) - Detailed schema and format documentation
- [Usage Guide](usage.md) - General CLI usage and examples
- [Architecture](architecture.md) - Internal design and implementation details

---

**Questions or issues?** Please file an issue on the [roslyn-diff GitHub repository](https://github.com/randlee/roslyn-diff/issues).
