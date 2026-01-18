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
