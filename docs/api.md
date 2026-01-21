# API Reference

This document describes the programmatic API for using roslyn-diff as a library in your .NET applications.

## Table of Contents

- [Overview](#overview)
- [Getting Started](#getting-started)
- [Core Components](#core-components)
  - [DifferFactory](#differfactory)
  - [IDiffer Interface](#idiffer-interface)
  - [DiffOptions](#diffoptions)
  - [DiffResult](#diffresult)
- [Output Formatters](#output-formatters)
  - [OutputFormatterFactory](#outputformatterfactory)
  - [IOutputFormatter Interface](#ioutputformatter-interface)
  - [OutputOptions](#outputoptions)
- [Class Matching](#class-matching)
  - [ClassMatcher](#classmatcher)
- [Multi-TFM Support](#multi-tfm-support)
  - [TfmParser](#tfmparser)
  - [TfmSymbolResolver](#tfmsymbolresolver)
  - [Using TFM Support Programmatically](#using-tfm-support-programmatically)
- [Extension Points](#extension-points)
- [Complete Examples](#complete-examples)

## Overview

roslyn-diff is composed of three main packages:

| Package | Description |
|---------|-------------|
| `RoslynDiff.Core` | Core diff engine, models, and comparers |
| `RoslynDiff.Output` | Output formatters (JSON, HTML, text, etc.) |
| `RoslynDiff.Cli` | Command-line interface |

For programmatic use, you typically only need `RoslynDiff.Core` and `RoslynDiff.Output`.

## Getting Started

### Installation

```bash
# From NuGet (coming soon)
dotnet add package RoslynDiff.Core
dotnet add package RoslynDiff.Output
```

### Basic Usage

```csharp
using RoslynDiff.Core.Differ;
using RoslynDiff.Core.Models;
using RoslynDiff.Output;

// Read files
var oldContent = await File.ReadAllTextAsync("old.cs");
var newContent = await File.ReadAllTextAsync("new.cs");

// Create differ
var factory = new DifferFactory();
var differ = factory.GetDiffer("file.cs", new DiffOptions());

// Perform diff
var result = differ.Compare(oldContent, newContent, new DiffOptions());

// Format output
var formatterFactory = new OutputFormatterFactory();
var formatter = formatterFactory.GetFormatter("json");
var output = formatter.FormatResult(result);

Console.WriteLine(output);
```

---

## Core Components

### DifferFactory

The `DifferFactory` class is responsible for selecting the appropriate differ based on file type and options.

**Namespace:** `RoslynDiff.Core.Differ`

#### Constructor

```csharp
public DifferFactory()
```

Creates a new instance with default differs registered:
- `CSharpDiffer` for `.cs` files
- `VisualBasicDiffer` for `.vb` files
- `LineDiffer` for all other files

#### Methods

##### GetDiffer

```csharp
public IDiffer GetDiffer(string filePath, DiffOptions options)
```

Gets the appropriate differ for a file based on extension and options.

**Parameters:**
- `filePath` - The path to the file (used to determine extension)
- `options` - Diff options that may override the differ selection

**Returns:** An `IDiffer` instance

**Behavior:**
- If `options.Mode == DiffMode.Line`, returns `LineDiffer`
- If `options.Mode == DiffMode.Roslyn`:
  - `.cs` returns `CSharpDiffer`
  - `.vb` returns `VisualBasicDiffer`
  - Other extensions throw `NotSupportedException`
- If `options.Mode == null` (auto):
  - `.cs` returns `CSharpDiffer`
  - `.vb` returns `VisualBasicDiffer`
  - Other extensions return `LineDiffer`

```csharp
public IDiffer GetDiffer(string oldPath, string newPath, DiffOptions options)
```

Gets differ using the new file's extension (falls back to old path).

##### SupportsSemantic

```csharp
public static bool SupportsSemantic(string filePath)
```

Checks if a file can be diffed semantically (Roslyn).

**Returns:** `true` for `.cs` and `.vb` files

##### Properties

```csharp
public static IReadOnlyList<string> SemanticExtensions { get; }
// Returns: [".cs", ".vb"]

public IReadOnlyList<IDiffer> RegisteredDiffers { get; }
// Returns all registered differs
```

#### Example

```csharp
var factory = new DifferFactory();

// Auto-detect differ
var differ = factory.GetDiffer("file.cs", new DiffOptions());

// Force line diff
var lineDiffer = factory.GetDiffer("file.cs", new DiffOptions { Mode = DiffMode.Line });

// Check if semantic diff is supported
if (DifferFactory.SupportsSemantic("file.vb"))
{
    Console.WriteLine("VB.NET semantic diff available");
}
```

---

### IDiffer Interface

The core interface for all differ implementations.

**Namespace:** `RoslynDiff.Core.Differ`

```csharp
public interface IDiffer
{
    DiffResult Compare(string oldContent, string newContent, DiffOptions options);
    bool CanHandle(string filePath, DiffOptions options);
}
```

#### Methods

##### Compare

```csharp
DiffResult Compare(string oldContent, string newContent, DiffOptions options)
```

Compares two versions of content and produces a diff result.

**Parameters:**
- `oldContent` - The original content
- `newContent` - The new content to compare against
- `options` - Options controlling diff behavior

**Returns:** A `DiffResult` containing all detected changes

##### CanHandle

```csharp
bool CanHandle(string filePath, DiffOptions options)
```

Determines whether this differ can handle the specified file.

**Parameters:**
- `filePath` - The path to the file
- `options` - Options that may affect handling

**Returns:** `true` if this differ can process the file

#### Built-in Implementations

| Class | Description |
|-------|-------------|
| `CSharpDiffer` | Semantic diff for C# files using Roslyn |
| `VisualBasicDiffer` | Semantic diff for VB.NET files using Roslyn |
| `LineDiffer` | Line-by-line diff using DiffPlex |

---

### DiffOptions

Configuration options for controlling diff behavior.

**Namespace:** `RoslynDiff.Core.Models`

```csharp
public record DiffOptions
{
    public DiffMode? Mode { get; init; }
    public bool IgnoreWhitespace { get; init; }
    public bool IgnoreComments { get; init; }
    public int ContextLines { get; init; } = 3;
    public string? OldPath { get; init; }
    public string? NewPath { get; init; }
}
```

#### Properties

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `Mode` | `DiffMode?` | `null` | Diff mode (null = auto) |
| `IgnoreWhitespace` | `bool` | `false` | Ignore whitespace changes |
| `IgnoreComments` | `bool` | `false` | **Deprecated** - Roslyn inherently ignores comments (trivia) |
| `ContextLines` | `int` | `3` | Context lines around changes |
| `OldPath` | `string?` | `null` | Original file path (for display) |
| `NewPath` | `string?` | `null` | New file path (for display) |

#### DiffMode Enum

```csharp
public enum DiffMode
{
    Roslyn,  // Semantic diff using Roslyn
    Line     // Line-by-line diff
}
```

#### Example

```csharp
var options = new DiffOptions
{
    Mode = null, // Auto-detect
    IgnoreWhitespace = true,
    ContextLines = 5,
    OldPath = "original.cs",
    NewPath = "modified.cs"
};
```

---

### DiffResult

The result of a diff operation.

**Namespace:** `RoslynDiff.Core.Models`

```csharp
public record DiffResult
{
    public string? OldPath { get; init; }
    public string? NewPath { get; init; }
    public DiffMode Mode { get; init; }
    public IReadOnlyList<FileChange> FileChanges { get; init; }
    public DiffStats Stats { get; init; }
}
```

#### Properties

| Property | Type | Description |
|----------|------|-------------|
| `OldPath` | `string?` | Path to original file |
| `NewPath` | `string?` | Path to new file |
| `Mode` | `DiffMode` | Diff mode used |
| `FileChanges` | `IReadOnlyList<FileChange>` | List of file changes |
| `Stats` | `DiffStats` | Summary statistics |

#### Related Types

##### FileChange

```csharp
public record FileChange
{
    public string? Path { get; init; }
    public IReadOnlyList<Change> Changes { get; init; }
}
```

##### Change

```csharp
public record Change
{
    public ChangeType Type { get; init; }
    public ChangeKind Kind { get; init; }
    public string? Name { get; init; }
    public string? OldName { get; init; }
    public Location? OldLocation { get; init; }
    public Location? NewLocation { get; init; }
    public string? OldContent { get; init; }
    public string? NewContent { get; init; }
    public IReadOnlyList<Change>? Children { get; init; }
}
```

##### ChangeType Enum

```csharp
public enum ChangeType
{
    Added,     // Element was added
    Removed,   // Element was removed
    Modified,  // Element was modified
    Moved,     // Element was moved
    Renamed,   // Element was renamed
    Unchanged  // Element is unchanged
}
```

##### ChangeKind Enum

```csharp
public enum ChangeKind
{
    File,       // Entire file
    Namespace,  // Namespace declaration
    Class,      // Class, struct, record, interface
    Method,     // Method or function
    Property,   // Property
    Field,      // Field
    Statement,  // Individual statement
    Line        // Single line (line-based diff)
}
```

##### DiffStats

```csharp
public record DiffStats
{
    public int Additions { get; init; }
    public int Deletions { get; init; }
    public int Modifications { get; init; }
    public int Moves { get; init; }
    public int Renames { get; init; }
    public int TotalChanges => Additions + Deletions + Modifications + Moves + Renames;
}
```

##### Location

```csharp
public record Location
{
    public string? File { get; init; }
    public int StartLine { get; init; }
    public int EndLine { get; init; }
    public int StartColumn { get; init; }
    public int EndColumn { get; init; }
}
```

---

## Output Formatters

### OutputFormatterFactory

Factory for creating output formatters.

**Namespace:** `RoslynDiff.Output`

```csharp
public class OutputFormatterFactory
{
    public OutputFormatterFactory();
    public IOutputFormatter GetFormatter(string format);
    public bool IsFormatSupported(string format);
    public void RegisterFormatter(string format, Func<IOutputFormatter> factory);
    public IReadOnlyList<string> SupportedFormats { get; }
}
```

#### Methods

##### GetFormatter

```csharp
public IOutputFormatter GetFormatter(string format)
```

Gets a formatter for the specified format.

**Parameters:**
- `format` - Format name (case-insensitive): `json`, `html`, `text`, `plain`, `terminal`

**Returns:** An `IOutputFormatter` instance

**Throws:** `ArgumentException` if format is not supported

##### RegisterFormatter

```csharp
public void RegisterFormatter(string format, Func<IOutputFormatter> factory)
```

Registers a custom formatter factory.

**Parameters:**
- `format` - Format name
- `factory` - Factory function that creates the formatter

#### Example

```csharp
var factory = new OutputFormatterFactory();

// Get built-in formatter
var jsonFormatter = factory.GetFormatter("json");

// Check if format is supported
if (factory.IsFormatSupported("yaml"))
{
    // ...
}

// Register custom formatter
factory.RegisterFormatter("yaml", () => new YamlFormatter());

// List all supported formats
foreach (var format in factory.SupportedFormats)
{
    Console.WriteLine(format);
}
```

---

### IOutputFormatter Interface

Interface for all output formatters.

**Namespace:** `RoslynDiff.Output`

```csharp
public interface IOutputFormatter
{
    string Format { get; }
    string ContentType { get; }
    string FormatResult(DiffResult result, OutputOptions? options = null);
    Task FormatResultAsync(DiffResult result, TextWriter writer, OutputOptions? options = null);
}
```

#### Properties

| Property | Description |
|----------|-------------|
| `Format` | Format identifier (e.g., "json", "html") |
| `ContentType` | MIME type (e.g., "application/json") |

#### Methods

##### FormatResult

```csharp
string FormatResult(DiffResult result, OutputOptions? options = null)
```

Formats the diff result into a string.

##### FormatResultAsync

```csharp
Task FormatResultAsync(DiffResult result, TextWriter writer, OutputOptions? options = null)
```

Formats the diff result and writes to a TextWriter.

#### Built-in Formatters

| Class | Format | ContentType |
|-------|--------|-------------|
| `JsonFormatter` | `json` | `application/json` |
| `HtmlFormatter` | `html` | `text/html` |
| `UnifiedFormatter` | `text` | `text/plain` |
| `PlainTextFormatter` | `plain` | `text/plain` |
| `SpectreConsoleFormatter` | `terminal` | `text/plain` |

---

### OutputOptions

Options for controlling output formatting.

**Namespace:** `RoslynDiff.Output`

```csharp
public record OutputOptions
{
    public bool IncludeContent { get; init; } = true;
    public bool PrettyPrint { get; init; } = true;
    public int ContextLines { get; init; } = 3;
    public bool UseColor { get; init; }
    public bool IncludeStats { get; init; } = true;
    public bool Compact { get; init; }
}
```

#### Properties

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `IncludeContent` | `bool` | `true` | Include source code content |
| `PrettyPrint` | `bool` | `true` | Format output with indentation |
| `ContextLines` | `int` | `3` | Context lines to show |
| `UseColor` | `bool` | `false` | Use colored output |
| `IncludeStats` | `bool` | `true` | Include statistics |
| `Compact` | `bool` | `false` | Use compact format |

---

## Tree Comparison

### ITreeComparer Interface

Interface for recursive tree comparison with async support.

**Namespace:** `RoslynDiff.Core.Comparison`

```csharp
public interface ITreeComparer
{
    ValueTask<IReadOnlyList<Change>> CompareAsync(
        SyntaxTree oldTree,
        SyntaxTree newTree,
        DiffOptions options,
        CancellationToken cancellationToken = default);

    IReadOnlyList<Change> Compare(
        SyntaxTree oldTree,
        SyntaxTree newTree,
        DiffOptions options);
}
```

#### Methods

##### CompareAsync

```csharp
ValueTask<IReadOnlyList<Change>> CompareAsync(
    SyntaxTree oldTree,
    SyntaxTree newTree,
    DiffOptions options,
    CancellationToken cancellationToken = default)
```

Compares two syntax trees asynchronously using recursive tree diff.

**Parameters:**
- `oldTree` - The original syntax tree
- `newTree` - The new syntax tree to compare
- `options` - Options controlling comparison behavior
- `cancellationToken` - Token to cancel the operation

**Returns:** Hierarchical list of `Change` objects representing differences

##### Compare

```csharp
IReadOnlyList<Change> Compare(SyntaxTree oldTree, SyntaxTree newTree, DiffOptions options)
```

Synchronous version of `CompareAsync`. Prefer async for large trees.

---

### RecursiveTreeComparer

Level-by-level recursive tree comparison implementation. Addresses BUG-003 (duplicate node detection).

**Namespace:** `RoslynDiff.Core.Comparison`

```csharp
public sealed class RecursiveTreeComparer : ITreeComparer
{
    public RecursiveTreeComparer();
    public RecursiveTreeComparer(NodeMatcher matcher, ParallelOptions parallelOptions);
}
```

#### Constructors

##### Default Constructor

```csharp
public RecursiveTreeComparer()
```

Creates instance with default `NodeMatcher` and parallel options based on processor count.

##### Custom Constructor

```csharp
public RecursiveTreeComparer(NodeMatcher matcher, ParallelOptions parallelOptions)
```

Creates instance with custom node matcher and parallel processing options.

**Parameters:**
- `matcher` - Custom node matcher for comparing trees
- `parallelOptions` - Options controlling parallelism (e.g., `MaxDegreeOfParallelism`)

#### Key Characteristics

- **O(n) complexity** with early termination for identical subtrees
- **Hierarchical output** matching code structure
- **Parallel subtree comparison** via `ValueTask` when beneficial
- **Cancellation support** for long-running comparisons

#### Example

```csharp
using RoslynDiff.Core.Comparison;
using RoslynDiff.Core.Models;
using Microsoft.CodeAnalysis.CSharp;

var oldTree = CSharpSyntaxTree.ParseText(oldContent);
var newTree = CSharpSyntaxTree.ParseText(newContent);

var comparer = new RecursiveTreeComparer();
var options = new DiffOptions { OldPath = "old.cs", NewPath = "new.cs" };

// Async (preferred for large files)
var changes = await comparer.CompareAsync(oldTree, newTree, options);

// Sync (simpler use cases)
var changes = comparer.Compare(oldTree, newTree, options);

// Changes are hierarchical - class changes contain method changes as Children
foreach (var change in changes)
{
    Console.WriteLine($"{change.Kind}: {change.Name} ({change.Type})");
    if (change.Children != null)
    {
        foreach (var child in change.Children)
        {
            Console.WriteLine($"  - {child.Kind}: {child.Name} ({child.Type})");
        }
    }
}
```

---

### ChangeExtensions

Extension methods for working with hierarchical `Change` structures.

**Namespace:** `RoslynDiff.Core.Models`

```csharp
public static class ChangeExtensions
{
    public static IEnumerable<Change> Flatten(this IEnumerable<Change> changes);
    public static int CountAll(this IEnumerable<Change> changes);
    public static Change? FindByName(this IEnumerable<Change> changes, string name);
    public static IEnumerable<Change> OfKind(this IEnumerable<Change> changes, ChangeKind kind);
}
```

#### Methods

##### Flatten

```csharp
public static IEnumerable<Change> Flatten(this IEnumerable<Change> changes)
```

Flattens hierarchical changes into a single-level enumerable (depth-first).

**Use Case:** Backward compatibility with code expecting flat change lists.

```csharp
// Hierarchical changes from RecursiveTreeComparer
var hierarchicalChanges = comparer.Compare(oldTree, newTree, options);

// Flatten for backward compatibility
var flatChanges = hierarchicalChanges.Flatten().ToList();
```

##### CountAll

```csharp
public static int CountAll(this IEnumerable<Change> changes)
```

Counts all changes including nested children.

```csharp
var totalChanges = changes.CountAll();
```

##### FindByName

```csharp
public static Change? FindByName(this IEnumerable<Change> changes, string name)
```

Finds a change by name at any nesting level.

```csharp
var methodChange = changes.FindByName("Calculate");
```

##### OfKind

```csharp
public static IEnumerable<Change> OfKind(this IEnumerable<Change> changes, ChangeKind kind)
```

Gets all changes of a specific kind at any nesting level.

```csharp
var methodChanges = changes.OfKind(ChangeKind.Method).ToList();
var propertyChanges = changes.OfKind(ChangeKind.Property).ToList();
```

---

## Class Matching

### ClassMatcher

Matches classes between files using various strategies.

**Namespace:** `RoslynDiff.Core.Matching`

```csharp
public class ClassMatcher
{
    public ClassMatchResult? FindMatch(
        TypeDeclarationSyntax sourceClass,
        SyntaxTree targetTree,
        ClassMatchOptions options);
}
```

#### ClassMatchOptions

```csharp
public record ClassMatchOptions
{
    public ClassMatchStrategy Strategy { get; init; } = ClassMatchStrategy.Auto;
    public string? InterfaceName { get; init; }
    public double SimilarityThreshold { get; init; } = 0.8;
}
```

#### ClassMatchStrategy Enum

```csharp
public enum ClassMatchStrategy
{
    ExactName,    // Match by exact class name
    Interface,    // Match by interface implementation
    Similarity,   // Match by content similarity
    Auto          // Try exact, then similarity
}
```

#### ClassMatchResult

```csharp
public record ClassMatchResult
{
    public TypeDeclarationSyntax OldClass { get; init; }
    public TypeDeclarationSyntax NewClass { get; init; }
    public double Similarity { get; init; }
    public ClassMatchStrategy StrategyUsed { get; init; }
}
```

#### Example

```csharp
using Microsoft.CodeAnalysis.CSharp;
using RoslynDiff.Core.Matching;

// Parse syntax trees
var oldTree = CSharpSyntaxTree.ParseText(oldContent);
var newTree = CSharpSyntaxTree.ParseText(newContent);

var oldRoot = await oldTree.GetRootAsync();

// Find the source class
var oldClass = oldRoot.DescendantNodes()
    .OfType<TypeDeclarationSyntax>()
    .First(c => c.Identifier.Text == "OldService");

// Find matching class in new file
var matcher = new ClassMatcher();
var result = matcher.FindMatch(oldClass, newTree, new ClassMatchOptions
{
    Strategy = ClassMatchStrategy.Similarity,
    SimilarityThreshold = 0.7
});

if (result != null)
{
    Console.WriteLine($"Matched: {result.NewClass.Identifier.Text}");
    Console.WriteLine($"Similarity: {result.Similarity:P0}");
}
```

---

## Multi-TFM Support

roslyn-diff provides comprehensive support for analyzing multi-targeted projects that compile for different target framework monikers (TFMs). This enables detection of changes that are specific to certain frameworks due to conditional compilation or framework-specific APIs.

### Overview

When analyzing code that targets multiple frameworks (e.g., `net8.0` and `net10.0`), roslyn-diff can:

1. **Parse and validate** TFM strings to ensure they conform to .NET naming conventions
2. **Resolve** TFMs to their corresponding preprocessor symbols (e.g., `NET8_0`, `NET8_0_OR_GREATER`)
3. **Analyze** code separately for each TFM to detect framework-specific changes
4. **Merge** results to identify which changes apply to all TFMs vs. specific TFMs

### TFM Property Semantics

Understanding the TFM-related properties is crucial for working with multi-TFM results:

#### DiffOptions.TargetFrameworks

Controls whether TFM analysis is performed:

- `null` - No TFM analysis (default). Analyze code as single-targeted.
- `["net8.0", "net10.0"]` - Analyze both frameworks and detect TFM-specific changes.

#### DiffResult.AnalyzedTfms

Indicates which TFMs were analyzed:

- `null` - No TFM analysis was performed
- `["net8.0", "net10.0"]` - These frameworks were analyzed

#### Change.ApplicableToTfms

Indicates which TFMs a change applies to:

- `null` - No TFM analysis (single-targeted or TFM analysis not requested)
- `[]` (empty list) - Change appears in **ALL** analyzed TFMs (universal change)
- `["net8.0"]` - Change appears **ONLY** in net8.0 (TFM-specific change)
- `["net8.0", "net10.0"]` - Change appears in these specific TFMs

**Important:** An empty list means "all TFMs", not "no TFMs"!

```csharp
// Check if a change is universal (applies to all TFMs)
if (change.ApplicableToTfms?.Count == 0)
{
    Console.WriteLine($"{change.Name} applies to all TFMs");
}

// Check if a change is TFM-specific
if (change.ApplicableToTfms?.Any() == true)
{
    Console.WriteLine($"{change.Name} only applies to: {string.Join(", ", change.ApplicableToTfms)}");
}

// Check if TFM analysis was performed
if (change.ApplicableToTfms != null)
{
    Console.WriteLine("TFM analysis was performed");
}
```

---

### TfmParser

Parses and validates Target Framework Moniker strings.

**Namespace:** `RoslynDiff.Core.Tfm`

```csharp
public static partial class TfmParser
{
    public static string ParseSingle(string tfm);
    public static string[] ParseMultiple(string tfms);
    public static bool Validate(string tfm);
}
```

#### Supported TFM Formats

| Framework | Format | Examples |
|-----------|--------|----------|
| .NET 5+ | `netX.0` (X >= 5) | `net5.0`, `net6.0`, `net8.0`, `net10.0` |
| .NET Framework | `netXY` or `netX.Y[.Z]` | `net462`, `net48`, `net4.8` |
| .NET Core | `netcoreappX.Y` | `netcoreapp3.1`, `netcoreapp2.1` |
| .NET Standard | `netstandardX.Y` | `netstandard2.0`, `netstandard2.1` |

#### Methods

##### ParseSingle

```csharp
public static string ParseSingle(string tfm)
```

Parses and normalizes a single TFM string.

**Parameters:**
- `tfm` - The TFM string (e.g., "NET8.0", "net8.0", "  net8.0  ")

**Returns:** Normalized TFM in lowercase (e.g., "net8.0")

**Throws:**
- `ArgumentNullException` - TFM is null
- `ArgumentException` - TFM is empty or invalid format

**Example:**

```csharp
using RoslynDiff.Core.Tfm;

// Parse and normalize
var normalized = TfmParser.ParseSingle("NET8.0");
Console.WriteLine(normalized); // "net8.0"

// Whitespace is trimmed
var normalized2 = TfmParser.ParseSingle("  net10.0  ");
Console.WriteLine(normalized2); // "net10.0"

// Invalid format throws exception
try
{
    var invalid = TfmParser.ParseSingle("net8"); // Missing ".0"
}
catch (ArgumentException ex)
{
    Console.WriteLine(ex.Message);
}
```

##### ParseMultiple

```csharp
public static string[] ParseMultiple(string tfms)
```

Parses a semicolon-separated list of TFMs (e.g., from MSBuild TargetFrameworks property).

**Parameters:**
- `tfms` - Semicolon-separated TFMs (e.g., "net8.0;net10.0;netstandard2.1")

**Returns:** Array of normalized TFMs with duplicates removed

**Throws:**
- `ArgumentNullException` - TFMs is null
- `ArgumentException` - TFMs is empty or contains invalid TFMs

**Example:**

```csharp
// Parse multiple TFMs
var tfms = TfmParser.ParseMultiple("net8.0;net10.0;netstandard2.1");
// Returns: ["net8.0", "net10.0", "netstandard2.1"]

// Duplicates are removed (case-insensitive)
var tfms2 = TfmParser.ParseMultiple("net8.0;NET8.0;net10.0");
// Returns: ["net8.0", "net10.0"]

// Empty entries are ignored
var tfms3 = TfmParser.ParseMultiple("net8.0;;net10.0");
// Returns: ["net8.0", "net10.0"]

// Use with MSBuild project files
var projectTfms = "<TargetFrameworks>net8.0;net10.0</TargetFrameworks>";
var extractedTfms = ExtractTfmsFromMSBuild(projectTfms);
var parsedTfms = TfmParser.ParseMultiple(extractedTfms);
```

##### Validate

```csharp
public static bool Validate(string tfm)
```

Validates a TFM string without throwing exceptions.

**Parameters:**
- `tfm` - The TFM string to validate

**Returns:** `true` if valid, `false` otherwise

**Example:**

```csharp
if (TfmParser.Validate("net8.0"))
{
    Console.WriteLine("Valid TFM");
}

if (!TfmParser.Validate("net8")) // Invalid - missing ".0"
{
    Console.WriteLine("Invalid TFM format");
}
```

---

### TfmSymbolResolver

Resolves TFMs to their corresponding preprocessor symbols defined by the .NET compiler.

**Namespace:** `RoslynDiff.Core.Tfm`

```csharp
public static class TfmSymbolResolver
{
    public static string[] GetPreprocessorSymbols(string tfm);
    public static string[] GetDefaultSymbols();
}
```

#### Methods

##### GetPreprocessorSymbols

```csharp
public static string[] GetPreprocessorSymbols(string tfm)
```

Gets all preprocessor symbols for a given TFM.

**Parameters:**
- `tfm` - The Target Framework Moniker (case-insensitive)

**Returns:** Array of preprocessor symbols

**Throws:**
- `ArgumentException` - TFM is null, empty, or unrecognized

**Example:**

```csharp
using RoslynDiff.Core.Tfm;

// Get symbols for .NET 8.0
var symbols = TfmSymbolResolver.GetPreprocessorSymbols("net8.0");
// Returns: ["NET8_0", "NET5_0_OR_GREATER", "NET6_0_OR_GREATER",
//           "NET7_0_OR_GREATER", "NET8_0_OR_GREATER"]

// Get symbols for .NET Framework 4.8
var fxSymbols = TfmSymbolResolver.GetPreprocessorSymbols("net48");
// Returns: ["NET48", "NETFRAMEWORK"]

// Get symbols for .NET Standard 2.0
var stdSymbols = TfmSymbolResolver.GetPreprocessorSymbols("netstandard2.0");
// Returns: ["NETSTANDARD2_0", "NETSTANDARD"]

// Case-insensitive and whitespace-tolerant
var symbols2 = TfmSymbolResolver.GetPreprocessorSymbols("  NET8.0  ");
// Works correctly
```

**Symbol Patterns:**

For .NET 5+, symbols include:
- Base symbol: `NET8_0`
- OR_GREATER symbols: `NET5_0_OR_GREATER`, `NET6_0_OR_GREATER`, ..., `NET8_0_OR_GREATER`

This matches the compiler's behavior where `net8.0` can use `#if NET6_0_OR_GREATER`.

##### GetDefaultSymbols

```csharp
public static string[] GetDefaultSymbols()
```

Gets default symbols for the latest supported .NET version (currently .NET 10.0).

**Returns:** Array of preprocessor symbols for .NET 10.0

**Example:**

```csharp
var defaultSymbols = TfmSymbolResolver.GetDefaultSymbols();
// Returns: ["NET10_0", "NET5_0_OR_GREATER", "NET6_0_OR_GREATER",
//           "NET7_0_OR_GREATER", "NET8_0_OR_GREATER", "NET9_0_OR_GREATER",
//           "NET10_0_OR_GREATER"]

// Use when no TFM is specified
var symbols = options.TargetFrameworks?.Any() == true
    ? TfmSymbolResolver.GetPreprocessorSymbols(options.TargetFrameworks[0])
    : TfmSymbolResolver.GetDefaultSymbols();
```

---

### Using TFM Support Programmatically

This section demonstrates how to use TFM support in your applications.

#### Basic Multi-TFM Analysis

```csharp
using RoslynDiff.Core.Differ;
using RoslynDiff.Core.Models;
using RoslynDiff.Core.Tfm;

public async Task AnalyzeMultiTfmProjectAsync()
{
    var oldContent = await File.ReadAllTextAsync("old.cs");
    var newContent = await File.ReadAllTextAsync("new.cs");

    // Configure diff options with target frameworks
    var options = new DiffOptions
    {
        TargetFrameworks = new[] { "net8.0", "net10.0" },
        OldPath = "old.cs",
        NewPath = "new.cs"
    };

    // Create differ and perform analysis
    var factory = new DifferFactory();
    var differ = factory.GetDiffer("file.cs", options);
    var result = differ.Compare(oldContent, newContent, options);

    // Check which TFMs were analyzed
    Console.WriteLine($"Analyzed TFMs: {string.Join(", ", result.AnalyzedTfms)}");
    // Output: "Analyzed TFMs: net8.0, net10.0"

    // Process results
    foreach (var fileChange in result.FileChanges)
    {
        foreach (var change in fileChange.Changes)
        {
            ProcessChange(change);
        }
    }
}

private void ProcessChange(Change change)
{
    if (change.ApplicableToTfms == null)
    {
        // No TFM analysis performed
        Console.WriteLine($"{change.Name}: No TFM analysis");
    }
    else if (change.ApplicableToTfms.Count == 0)
    {
        // Universal change - applies to all TFMs
        Console.WriteLine($"{change.Name}: Universal change (all TFMs)");
    }
    else
    {
        // TFM-specific change
        Console.WriteLine($"{change.Name}: Only in {string.Join(", ", change.ApplicableToTfms)}");
    }

    // Recursively process children
    if (change.Children != null)
    {
        foreach (var child in change.Children)
        {
            ProcessChange(child);
        }
    }
}
```

#### Filtering TFM-Specific Changes

```csharp
using RoslynDiff.Core.Models;

public class TfmChangeFilter
{
    public IEnumerable<Change> GetUniversalChanges(DiffResult result)
    {
        // Changes that apply to all TFMs (empty list)
        return result.FileChanges
            .SelectMany(fc => fc.Changes)
            .Where(c => c.ApplicableToTfms?.Count == 0);
    }

    public IEnumerable<Change> GetTfmSpecificChanges(DiffResult result)
    {
        // Changes that apply to specific TFMs only
        return result.FileChanges
            .SelectMany(fc => fc.Changes)
            .Where(c => c.ApplicableToTfms?.Any() == true);
    }

    public IEnumerable<Change> GetChangesForTfm(DiffResult result, string tfm)
    {
        // Changes that apply to a specific TFM
        return result.FileChanges
            .SelectMany(fc => fc.Changes)
            .Where(c => c.ApplicableToTfms == null ||
                       c.ApplicableToTfms.Count == 0 ||
                       c.ApplicableToTfms.Contains(tfm, StringComparer.OrdinalIgnoreCase));
    }
}

// Usage
var filter = new TfmChangeFilter();

// Get all universal changes
var universalChanges = filter.GetUniversalChanges(result);
Console.WriteLine($"Universal changes: {universalChanges.Count()}");

// Get changes specific to certain TFMs
var specificChanges = filter.GetTfmSpecificChanges(result);
foreach (var change in specificChanges)
{
    Console.WriteLine($"{change.Name}: {string.Join(", ", change.ApplicableToTfms)}");
}

// Get all changes that apply to net8.0
var net8Changes = filter.GetChangesForTfm(result, "net8.0");
Console.WriteLine($"Changes affecting net8.0: {net8Changes.Count()}");
```

#### Generating TFM-Aware Reports

```csharp
using RoslynDiff.Core.Models;

public class TfmReport
{
    public void GenerateReport(DiffResult result)
    {
        Console.WriteLine("=== Multi-TFM Analysis Report ===\n");

        if (result.AnalyzedTfms == null)
        {
            Console.WriteLine("No TFM analysis performed.");
            return;
        }

        Console.WriteLine($"Target Frameworks: {string.Join(", ", result.AnalyzedTfms)}\n");

        var universalCount = 0;
        var specificCount = 0;
        var tfmBreakdown = new Dictionary<string, int>();

        foreach (var change in result.FileChanges.SelectMany(fc => fc.Changes))
        {
            if (change.ApplicableToTfms?.Count == 0)
            {
                universalCount++;
            }
            else if (change.ApplicableToTfms?.Any() == true)
            {
                specificCount++;
                foreach (var tfm in change.ApplicableToTfms)
                {
                    tfmBreakdown.TryGetValue(tfm, out var count);
                    tfmBreakdown[tfm] = count + 1;
                }
            }
        }

        Console.WriteLine($"Universal changes (all TFMs): {universalCount}");
        Console.WriteLine($"TFM-specific changes: {specificCount}\n");

        if (tfmBreakdown.Any())
        {
            Console.WriteLine("Changes by TFM:");
            foreach (var (tfm, count) in tfmBreakdown.OrderBy(kv => kv.Key))
            {
                Console.WriteLine($"  {tfm}: {count} changes");
            }
        }

        Console.WriteLine($"\nTotal changes: {result.Stats.TotalChanges}");
        Console.WriteLine($"Breaking changes: {result.Stats.BreakingPublicApiCount + result.Stats.BreakingInternalApiCount}");
    }
}

// Usage
var reporter = new TfmReport();
reporter.GenerateReport(result);

// Output:
// === Multi-TFM Analysis Report ===
//
// Target Frameworks: net8.0, net10.0
//
// Universal changes (all TFMs): 15
// TFM-specific changes: 3
//
// Changes by TFM:
//   net10.0: 3 changes
//
// Total changes: 18
// Breaking changes: 2
```

#### Validating and Parsing User Input

```csharp
using RoslynDiff.Core.Tfm;

public class TfmInputHandler
{
    public string[]? ParseUserTfms(string? userInput)
    {
        if (string.IsNullOrWhiteSpace(userInput))
        {
            return null; // No TFM analysis
        }

        try
        {
            // Parse semicolon-separated list
            var tfms = TfmParser.ParseMultiple(userInput);
            Console.WriteLine($"Parsed {tfms.Length} TFMs: {string.Join(", ", tfms)}");
            return tfms;
        }
        catch (ArgumentException ex)
        {
            Console.WriteLine($"Invalid TFMs: {ex.Message}");
            return null;
        }
    }

    public void DisplayTfmInfo(string tfm)
    {
        if (!TfmParser.Validate(tfm))
        {
            Console.WriteLine($"Invalid TFM: {tfm}");
            return;
        }

        var normalized = TfmParser.ParseSingle(tfm);
        var symbols = TfmSymbolResolver.GetPreprocessorSymbols(normalized);

        Console.WriteLine($"TFM: {normalized}");
        Console.WriteLine("Preprocessor Symbols:");
        foreach (var symbol in symbols)
        {
            Console.WriteLine($"  {symbol}");
        }
    }
}

// Usage
var handler = new TfmInputHandler();

// Parse from user input (e.g., command-line argument)
var tfms = handler.ParseUserTfms("net8.0;net10.0");
var options = new DiffOptions { TargetFrameworks = tfms };

// Display information about a specific TFM
handler.DisplayTfmInfo("net8.0");
// Output:
// TFM: net8.0
// Preprocessor Symbols:
//   NET8_0
//   NET5_0_OR_GREATER
//   NET6_0_OR_GREATER
//   NET7_0_OR_GREATER
//   NET8_0_OR_GREATER
```

#### Advanced: Custom TFM Analysis

```csharp
using RoslynDiff.Core.Models;
using RoslynDiff.Core.Tfm;

public class CustomTfmAnalyzer
{
    public async Task<Dictionary<string, DiffResult>> AnalyzePerTfmAsync(
        string oldContent,
        string newContent,
        string[] tfms)
    {
        var results = new Dictionary<string, DiffResult>();
        var factory = new DifferFactory();

        foreach (var tfm in tfms)
        {
            // Analyze each TFM individually
            var options = new DiffOptions
            {
                TargetFrameworks = new[] { tfm },
                OldPath = $"old.cs ({tfm})",
                NewPath = $"new.cs ({tfm})"
            };

            var differ = factory.GetDiffer("file.cs", options);
            var result = differ.Compare(oldContent, newContent, options);

            results[tfm] = result;
        }

        return results;
    }

    public void CompareTfmResults(Dictionary<string, DiffResult> results)
    {
        Console.WriteLine("=== Per-TFM Comparison ===\n");

        foreach (var (tfm, result) in results)
        {
            Console.WriteLine($"{tfm}:");
            Console.WriteLine($"  Total changes: {result.Stats.TotalChanges}");
            Console.WriteLine($"  Additions: {result.Stats.Additions}");
            Console.WriteLine($"  Deletions: {result.Stats.Deletions}");
            Console.WriteLine($"  Modifications: {result.Stats.Modifications}");
            Console.WriteLine();
        }

        // Find changes unique to specific TFMs
        var allTfms = results.Keys.ToArray();
        foreach (var tfm in allTfms)
        {
            var otherTfms = allTfms.Where(t => t != tfm).ToArray();
            var uniqueChanges = FindUniqueChanges(results[tfm], otherTfms.Select(t => results[t]));

            if (uniqueChanges.Any())
            {
                Console.WriteLine($"Changes unique to {tfm}:");
                foreach (var change in uniqueChanges)
                {
                    Console.WriteLine($"  - {change.Kind}: {change.Name} ({change.Type})");
                }
                Console.WriteLine();
            }
        }
    }

    private IEnumerable<Change> FindUniqueChanges(
        DiffResult result,
        IEnumerable<DiffResult> otherResults)
    {
        var thisChanges = result.FileChanges
            .SelectMany(fc => fc.Changes)
            .Select(c => new { c.Kind, c.Name, c.Type })
            .ToHashSet();

        var otherChanges = otherResults
            .SelectMany(r => r.FileChanges)
            .SelectMany(fc => fc.Changes)
            .Select(c => new { c.Kind, c.Name, c.Type })
            .ToHashSet();

        thisChanges.ExceptWith(otherChanges);

        return result.FileChanges
            .SelectMany(fc => fc.Changes)
            .Where(c => thisChanges.Contains(new { c.Kind, c.Name, c.Type }));
    }
}

// Usage
var analyzer = new CustomTfmAnalyzer();
var perTfmResults = await analyzer.AnalyzePerTfmAsync(
    oldContent,
    newContent,
    new[] { "net8.0", "net10.0" });

analyzer.CompareTfmResults(perTfmResults);
```

---

## Extension Points

### Custom Differ

Implement `IDiffer` to add support for new languages or diff algorithms:

```csharp
public class TypeScriptDiffer : IDiffer
{
    public DiffResult Compare(string oldContent, string newContent, DiffOptions options)
    {
        // Your implementation
    }

    public bool CanHandle(string filePath, DiffOptions options)
    {
        var ext = Path.GetExtension(filePath);
        return ext == ".ts" || ext == ".tsx";
    }
}
```

### Custom Formatter

Implement `IOutputFormatter` for custom output formats:

```csharp
public class YamlFormatter : IOutputFormatter
{
    public string Format => "yaml";
    public string ContentType => "application/x-yaml";

    public string FormatResult(DiffResult result, OutputOptions? options = null)
    {
        // Your implementation
    }

    public Task FormatResultAsync(DiffResult result, TextWriter writer, OutputOptions? options = null)
    {
        var output = FormatResult(result, options);
        return writer.WriteAsync(output);
    }
}

// Register with factory
var factory = new OutputFormatterFactory();
factory.RegisterFormatter("yaml", () => new YamlFormatter());
```

---

## Complete Examples

### Example 1: Basic File Comparison

```csharp
using RoslynDiff.Core.Differ;
using RoslynDiff.Core.Models;
using RoslynDiff.Output;

public class BasicDiff
{
    public async Task<string> CompareFilesAsync(string oldPath, string newPath)
    {
        var oldContent = await File.ReadAllTextAsync(oldPath);
        var newContent = await File.ReadAllTextAsync(newPath);

        var factory = new DifferFactory();
        var differ = factory.GetDiffer(newPath, new DiffOptions
        {
            OldPath = oldPath,
            NewPath = newPath
        });

        var result = differ.Compare(oldContent, newContent, new DiffOptions());

        var formatter = new OutputFormatterFactory().GetFormatter("json");
        return formatter.FormatResult(result, new OutputOptions { PrettyPrint = true });
    }
}
```

### Example 2: Class-to-Class Comparison

```csharp
using Microsoft.CodeAnalysis.CSharp;
using RoslynDiff.Core.Comparison;
using RoslynDiff.Core.Matching;
using RoslynDiff.Core.Models;

public class ClassComparison
{
    public async Task<DiffResult> CompareClassesAsync(
        string oldPath, string oldClassName,
        string newPath, string newClassName)
    {
        var oldContent = await File.ReadAllTextAsync(oldPath);
        var newContent = await File.ReadAllTextAsync(newPath);

        var oldTree = CSharpSyntaxTree.ParseText(oldContent);
        var newTree = CSharpSyntaxTree.ParseText(newContent);

        var oldRoot = await oldTree.GetRootAsync();
        var newRoot = await newTree.GetRootAsync();

        var oldClass = oldRoot.DescendantNodes()
            .OfType<TypeDeclarationSyntax>()
            .FirstOrDefault(c => c.Identifier.Text == oldClassName);

        var newClass = newRoot.DescendantNodes()
            .OfType<TypeDeclarationSyntax>()
            .FirstOrDefault(c => c.Identifier.Text == newClassName);

        // Compare using NodeMatcher
        var nodeMatcher = new NodeMatcher();
        var changes = new List<Change>();

        if (oldClass != null && newClass != null)
        {
            var oldNodes = nodeMatcher.ExtractStructuralNodes(oldClass);
            var newNodes = nodeMatcher.ExtractStructuralNodes(newClass);
            var matchResult = nodeMatcher.MatchNodes(oldNodes, newNodes);

            // Process match result into changes...
        }

        return new DiffResult
        {
            OldPath = oldPath,
            NewPath = newPath,
            Mode = DiffMode.Roslyn,
            FileChanges = new[] { new FileChange { Path = newPath, Changes = changes } },
            Stats = new DiffStats
            {
                Additions = changes.Count(c => c.Type == ChangeType.Added),
                Deletions = changes.Count(c => c.Type == ChangeType.Removed),
                Modifications = changes.Count(c => c.Type == ChangeType.Modified)
            }
        };
    }
}
```

### Example 3: Batch Processing with Custom Output

```csharp
using RoslynDiff.Core.Differ;
using RoslynDiff.Core.Models;
using RoslynDiff.Output;

public class BatchProcessor
{
    private readonly DifferFactory _differFactory = new();
    private readonly OutputFormatterFactory _formatterFactory = new();

    public async Task ProcessDirectoryAsync(string oldDir, string newDir, string outputDir)
    {
        var files = Directory.GetFiles(newDir, "*.cs", SearchOption.AllDirectories);
        var htmlFormatter = _formatterFactory.GetFormatter("html");

        foreach (var newFile in files)
        {
            var relativePath = Path.GetRelativePath(newDir, newFile);
            var oldFile = Path.Combine(oldDir, relativePath);

            if (!File.Exists(oldFile)) continue;

            var oldContent = await File.ReadAllTextAsync(oldFile);
            var newContent = await File.ReadAllTextAsync(newFile);

            var differ = _differFactory.GetDiffer(newFile, new DiffOptions());
            var result = differ.Compare(oldContent, newContent, new DiffOptions
            {
                OldPath = oldFile,
                NewPath = newFile
            });

            if (result.Stats.TotalChanges > 0)
            {
                var html = htmlFormatter.FormatResult(result);
                var outputFile = Path.Combine(outputDir, relativePath + ".html");

                Directory.CreateDirectory(Path.GetDirectoryName(outputFile)!);
                await File.WriteAllTextAsync(outputFile, html);
            }
        }
    }
}
```
