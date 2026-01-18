# Architecture

This document describes the architecture and design of roslyn-diff.

## Table of Contents

- [Overview](#overview)
- [Project Structure](#project-structure)
- [Component Architecture](#component-architecture)
- [Data Flow](#data-flow)
- [Key Classes](#key-classes)
- [Design Decisions](#design-decisions)

## Overview

roslyn-diff is built as a modular .NET application with three main components:

```
┌─────────────────────────────────────────────────────────────────┐
│                        RoslynDiff.Cli                           │
│                    (Command Line Interface)                      │
├─────────────────────────────────────────────────────────────────┤
│                       RoslynDiff.Output                          │
│         (Formatters: JSON, HTML, Text, Terminal)                 │
├─────────────────────────────────────────────────────────────────┤
│                       RoslynDiff.Core                            │
│    (Differs, Comparers, Models, Matching)                        │
├─────────────────────────────────────────────────────────────────┤
│                    External Dependencies                         │
│  Roslyn (CodeAnalysis) │ DiffPlex │ Spectre.Console             │
└─────────────────────────────────────────────────────────────────┘
```

## Project Structure

```
roslyn-diff/
├── src/
│   ├── RoslynDiff.Core/           # Core diff engine
│   │   ├── Comparison/            # Syntax/semantic comparison
│   │   │   ├── NodeMatcher.cs     # Match syntax nodes
│   │   │   ├── SyntaxComparer.cs  # Compare syntax trees
│   │   │   ├── SemanticComparer.cs# Compare semantics
│   │   │   └── SymbolMatcher.cs   # Match symbols
│   │   ├── Differ/                # Diff implementations
│   │   │   ├── IDiffer.cs         # Core interface
│   │   │   ├── DifferFactory.cs   # Factory for differs
│   │   │   ├── CSharpDiffer.cs    # C# semantic diff
│   │   │   ├── VisualBasicDiffer.cs # VB semantic diff
│   │   │   ├── RoslynDifferBase.cs# Shared Roslyn logic
│   │   │   └── LineDiffer.cs      # Line-by-line diff
│   │   ├── Matching/              # Class matching
│   │   │   ├── ClassMatcher.cs    # Match classes
│   │   │   ├── ClassMatchOptions.cs
│   │   │   └── ClassMatchResult.cs
│   │   ├── Models/                # Data models
│   │   │   ├── DiffResult.cs      # Main result type
│   │   │   ├── Change.cs          # Individual change
│   │   │   ├── ChangeType.cs      # Change types enum
│   │   │   ├── ChangeKind.cs      # Change kinds enum
│   │   │   ├── DiffOptions.cs     # Diff configuration
│   │   │   └── Location.cs        # Source location
│   │   └── Syntax/
│   │       └── ISyntaxComparer.cs
│   │
│   ├── RoslynDiff.Output/         # Output formatters
│   │   ├── IOutputFormatter.cs    # Formatter interface
│   │   ├── OutputFormatterFactory.cs
│   │   ├── OutputOptions.cs
│   │   ├── JsonFormatter.cs       # JSON output
│   │   ├── JsonOutputFormatter.cs # Alternative JSON
│   │   ├── HtmlFormatter.cs       # HTML output
│   │   ├── UnifiedFormatter.cs    # Unified diff
│   │   ├── PlainTextFormatter.cs  # Plain text
│   │   ├── SpectreConsoleFormatter.cs # Terminal
│   │   └── Templates/             # HTML templates
│   │
│   └── RoslynDiff.Cli/            # CLI application
│       ├── Program.cs             # Entry point
│       ├── ClassSpecParser.cs     # Parse class:file specs
│       └── Commands/
│           ├── DiffCommand.cs     # diff command
│           └── ClassCommand.cs    # class command
│
├── tests/
│   ├── RoslynDiff.Core.Tests/
│   │   ├── CSharpDifferTests.cs
│   │   ├── LineDifferTests.cs
│   │   ├── ModelsTests.cs
│   │   └── TestFixtures/          # Test data files
│   ├── RoslynDiff.Output.Tests/
│   └── RoslynDiff.Cli.Tests/
│
├── samples/                        # Example files
│   ├── before/
│   │   └── Calculator.cs
│   └── after/
│       └── Calculator.cs
│
└── docs/                           # Documentation
```

## Component Architecture

### RoslynDiff.Core

The core library provides the diff engine and all domain models.

```
┌─────────────────────────────────────────────────────────────────┐
│                       RoslynDiff.Core                            │
├─────────────────────────────────────────────────────────────────┤
│                                                                  │
│  ┌──────────────────┐    ┌──────────────────┐                   │
│  │  DifferFactory   │───>│     IDiffer      │                   │
│  └──────────────────┘    └──────────────────┘                   │
│                                  ▲                               │
│           ┌──────────────────────┼──────────────────────┐       │
│           │                      │                      │       │
│  ┌────────┴───────┐    ┌────────┴───────┐    ┌────────┴──────┐ │
│  │  CSharpDiffer  │    │VisualBasicDiffer│   │  LineDiffer   │ │
│  └────────────────┘    └─────────────────┘    └───────────────┘ │
│           │                      │                               │
│           └──────────┬───────────┘                               │
│                      ▼                                           │
│           ┌──────────────────┐                                   │
│           │ RoslynDifferBase │                                   │
│           └──────────────────┘                                   │
│                      │                                           │
│           ┌──────────┴──────────┐                               │
│           ▼                     ▼                                │
│  ┌────────────────┐    ┌────────────────┐                       │
│  │ SyntaxComparer │    │   NodeMatcher  │                       │
│  └────────────────┘    └────────────────┘                       │
│                                                                  │
├─────────────────────────────────────────────────────────────────┤
│  Models: DiffResult, Change, ChangeType, ChangeKind, Location   │
└─────────────────────────────────────────────────────────────────┘
```

#### Key Interfaces

**IDiffer**
```csharp
public interface IDiffer
{
    DiffResult Compare(string oldContent, string newContent, DiffOptions options);
    bool CanHandle(string filePath, DiffOptions options);
}
```

**ISyntaxComparer**
```csharp
public interface ISyntaxComparer
{
    IReadOnlyList<Change> Compare(SyntaxNode oldNode, SyntaxNode newNode);
}
```

### RoslynDiff.Output

The output library provides formatting of diff results.

```
┌─────────────────────────────────────────────────────────────────┐
│                      RoslynDiff.Output                           │
├─────────────────────────────────────────────────────────────────┤
│                                                                  │
│  ┌────────────────────────┐    ┌─────────────────────┐          │
│  │ OutputFormatterFactory │───>│  IOutputFormatter   │          │
│  └────────────────────────┘    └─────────────────────┘          │
│                                         ▲                        │
│         ┌───────────┬───────────┬───────┼───────┬──────────┐    │
│         │           │           │       │       │          │    │
│  ┌──────┴─────┐ ┌───┴────┐ ┌────┴───┐ ┌─┴──┐ ┌──┴───┐ ┌────┴──┐│
│  │JsonFormatter│ │Html-  │ │Unified │ │Plain│ │Spectre│ │Other ││
│  └────────────┘ │Formatter│ │Formatter││Text │ │Console│ │Custom││
│                 └────────┘ └────────┘ └────┘ └──────┘ └───────┘│
│                                                                  │
├─────────────────────────────────────────────────────────────────┤
│  OutputOptions: PrettyPrint, IncludeContent, UseColor, etc.     │
└─────────────────────────────────────────────────────────────────┘
```

### RoslynDiff.Cli

The CLI application using Spectre.Console.Cli.

```
┌─────────────────────────────────────────────────────────────────┐
│                        RoslynDiff.Cli                            │
├─────────────────────────────────────────────────────────────────┤
│                                                                  │
│  Program.cs (Entry Point)                                        │
│       │                                                          │
│       ▼                                                          │
│  ┌────────────────┐                                              │
│  │  CommandApp    │  (Spectre.Console.Cli)                       │
│  └────────────────┘                                              │
│       │                                                          │
│       ├────────────────────────────────────┐                     │
│       ▼                                    ▼                     │
│  ┌────────────┐                      ┌────────────┐              │
│  │ DiffCommand│                      │ClassCommand│              │
│  └────────────┘                      └────────────┘              │
│       │                                    │                     │
│       │ Uses                               │ Uses                │
│       ▼                                    ▼                     │
│  ┌────────────────┐              ┌──────────────────┐            │
│  │ DifferFactory  │              │  ClassMatcher    │            │
│  │ OutputFormatter│              │  OutputFormatter │            │
│  └────────────────┘              └──────────────────┘            │
│                                                                  │
└─────────────────────────────────────────────────────────────────┘
```

## Data Flow

### File Diff Flow

```
┌─────────┐     ┌────────────────┐     ┌──────────────────┐
│  Input  │     │  Diff Engine   │     │  Output Format   │
└────┬────┘     └───────┬────────┘     └────────┬─────────┘
     │                  │                       │
     │  1. Read Files   │                       │
     ▼                  │                       │
┌─────────────┐         │                       │
│ old.cs      │         │                       │
│ new.cs      │         │                       │
└─────┬───────┘         │                       │
      │                 │                       │
      │  2. Select      │                       │
      │     Differ      │                       │
      ▼                 │                       │
┌─────────────┐         │                       │
│DifferFactory│────────>│                       │
└─────────────┘         │                       │
                        │                       │
      ┌─────────────────┤                       │
      │                 │                       │
      ▼                 │                       │
┌──────────────┐        │                       │
│ CSharpDiffer │        │                       │
│      or      │        │                       │
│  LineDiffer  │        │                       │
└──────┬───────┘        │                       │
       │                │                       │
       │  3. Parse &    │                       │
       │     Compare    │                       │
       ▼                │                       │
┌──────────────┐        │                       │
│ Roslyn Parse │        │                       │
│ SyntaxTrees  │        │                       │
└──────┬───────┘        │                       │
       │                │                       │
       │  4. Match      │                       │
       │     Nodes      │                       │
       ▼                │                       │
┌──────────────┐        │                       │
│ NodeMatcher  │        │                       │
│ SyntaxComparer│       │                       │
└──────┬───────┘        │                       │
       │                │                       │
       │  5. Build      │                       │
       │     Result     │                       │
       ▼                │                       │
┌──────────────┐        │                       │
│  DiffResult  │────────┼──────────────────────>│
└──────────────┘        │                       │
                        │                       │
                        │  6. Format            │
                        │     Output            │
                        ▼                       │
                 ┌──────────────┐               │
                 │OutputFormatter│──────────────┼────>┌────────┐
                 └──────────────┘               │     │ Output │
                                                │     │ (JSON/ │
                                                │     │  HTML/ │
                                                │     │  Text) │
                                                │     └────────┘
```

### Roslyn Diff Flow (Detailed)

```
┌─────────────┐    ┌─────────────┐
│ Old Content │    │ New Content │
└──────┬──────┘    └──────┬──────┘
       │                  │
       ▼                  ▼
┌─────────────────────────────────┐
│     CSharpSyntaxTree.Parse()    │
└──────┬──────────────────┬───────┘
       │                  │
       ▼                  ▼
┌────────────┐      ┌────────────┐
│ Old Syntax │      │ New Syntax │
│    Tree    │      │    Tree    │
└──────┬─────┘      └──────┬─────┘
       │                   │
       └─────────┬─────────┘
                 │
                 ▼
       ┌─────────────────┐
       │   NodeMatcher   │
       └────────┬────────┘
                │
                ▼
       ┌─────────────────────┐
       │ Extract Structural  │
       │    Nodes (classes,  │
       │  methods, props...) │
       └────────┬────────────┘
                │
                ▼
       ┌─────────────────────┐
       │   Match Nodes by:   │
       │   - Name            │
       │   - Structure       │
       │   - Content hash    │
       └────────┬────────────┘
                │
                ▼
       ┌─────────────────────┐
       │   Generate Changes: │
       │   - Matched pairs   │
       │   - Unmatched old   │
       │   - Unmatched new   │
       └────────┬────────────┘
                │
                ▼
       ┌─────────────────────┐
       │     DiffResult      │
       │   - FileChanges     │
       │   - Stats           │
       └─────────────────────┘
```

## Key Classes

### Differ Layer

| Class | Responsibility |
|-------|----------------|
| `DifferFactory` | Creates appropriate differ based on file type |
| `CSharpDiffer` | Semantic diff for C# using Roslyn |
| `VisualBasicDiffer` | Semantic diff for VB.NET using Roslyn |
| `RoslynDifferBase` | Shared Roslyn diff logic |
| `LineDiffer` | Line-by-line diff using DiffPlex |

### Comparison Layer

| Class | Responsibility |
|-------|----------------|
| `SyntaxComparer` | Compare Roslyn syntax trees |
| `NodeMatcher` | Match syntax nodes between trees |
| `SemanticComparer` | Compare semantic information |
| `SymbolMatcher` | Match symbols by name/signature |

### Matching Layer

| Class | Responsibility |
|-------|----------------|
| `ClassMatcher` | Match classes by various strategies |
| `ClassMatchOptions` | Configuration for class matching |
| `ClassMatchResult` | Result of class matching operation |

### Model Layer

| Class | Responsibility |
|-------|----------------|
| `DiffResult` | Top-level diff result container |
| `FileChange` | Changes for a single file |
| `Change` | Individual change with metadata |
| `DiffStats` | Summary statistics |
| `Location` | Source code location |
| `DiffOptions` | Configuration for diff operation |

### Output Layer

| Class | Responsibility |
|-------|----------------|
| `OutputFormatterFactory` | Create formatters by name |
| `JsonFormatter` | JSON output |
| `HtmlFormatter` | HTML with syntax highlighting |
| `UnifiedFormatter` | Unified diff format |
| `PlainTextFormatter` | Plain text without ANSI |
| `SpectreConsoleFormatter` | Rich terminal output |

## Design Decisions

### 1. Factory Pattern for Differs

**Decision:** Use `DifferFactory` to select appropriate differ based on file extension and options.

**Rationale:**
- Encapsulates differ selection logic
- Easy to add new language support
- Clean separation of concerns

### 2. Roslyn for Semantic Analysis

**Decision:** Use Microsoft.CodeAnalysis (Roslyn) for C# and VB.NET parsing.

**Rationale:**
- Industry-standard .NET compiler platform
- Accurate syntax tree representation
- Rich semantic model support
- Active maintenance by Microsoft

### 3. DiffPlex for Line Diff

**Decision:** Use DiffPlex library for line-by-line diff.

**Rationale:**
- Well-tested, mature library
- Good performance
- Supports unified diff output
- MIT licensed

### 4. Spectre.Console for CLI

**Decision:** Use Spectre.Console and Spectre.Console.Cli for the CLI application.

**Rationale:**
- Rich terminal output capabilities
- Declarative command definitions
- Cross-platform support
- Active community

### 5. Record Types for Models

**Decision:** Use C# record types for domain models.

**Rationale:**
- Immutable by default
- Value-based equality
- Concise syntax
- With-expressions for modifications

### 6. Strategy Pattern for Class Matching

**Decision:** Use strategy pattern for class matching (exact, interface, similarity).

**Rationale:**
- Flexible matching approaches
- Easy to add new strategies
- User can select appropriate strategy

### 7. Modular Output Formatters

**Decision:** Implement formatters as separate classes implementing `IOutputFormatter`.

**Rationale:**
- Single responsibility principle
- Easy to add new formats
- Testable in isolation
- Factory pattern for instantiation

### 8. Self-Contained HTML Output

**Decision:** HTML output includes embedded CSS and JavaScript.

**Rationale:**
- Single file output (easy to share)
- No external dependencies
- Works offline
- Portable across systems

### 9. Recursive Tree Diff Algorithm (BUG-003 Fix)

**Decision:** Use recursive tree comparison instead of flat node extraction.

**Rationale:**
- Fixes BUG-003 (duplicate node detection) where the old flat extraction method could report the same node multiple times
- Each node is processed exactly once at its natural tree level
- Produces hierarchical output that mirrors the code structure
- Enables early termination when subtrees are identical (O(n) complexity)

**Old Approach (Flat Extraction):**
```
1. Extract ALL structural nodes from both trees (recursive descent)
2. Match extracted nodes using name/signature
3. Compare matched pairs
```
Problem: A method inside a class would be extracted both as a child of the class AND independently, causing duplicate detection.

**New Approach (Recursive Tree Comparison):**
```
1. Extract IMMEDIATE children only at each level
2. Match siblings using O(n) hash-based lookup
3. Recurse into matched pairs
4. Report additions/removals at their natural level
```
Benefit: Each node is visited exactly once, and changes are reported hierarchically.

## Recursive Tree Comparison

The `RecursiveTreeComparer` implements level-by-level tree diffing:

```
┌─────────────────────────────────────────────────────────────────┐
│                    RecursiveTreeComparer                         │
├─────────────────────────────────────────────────────────────────┤
│                                                                  │
│  Old Tree                          New Tree                      │
│  ────────                          ────────                      │
│  Namespace                         Namespace                     │
│    └── Class                         └── Class                   │
│          ├── Method1                       ├── Method1 (mod)     │
│          ├── Method2                       ├── Method3 (new)     │
│          └── Property                      └── Property          │
│                                                                  │
│  Algorithm:                                                      │
│  1. Extract immediate children at level N                        │
│  2. Match by (name, kind, signature) hash                        │
│  3. For matched pairs: compare → recurse if different            │
│  4. Unmatched old = Removed, Unmatched new = Added               │
│  5. Changes are nested: Class.Children contains Method changes   │
│                                                                  │
├─────────────────────────────────────────────────────────────────┤
│  Output: Hierarchical Changes                                    │
│  ─────────────────────────                                       │
│  Change(Class, Modified)                                         │
│    └── Children:                                                 │
│          ├── Change(Method1, Modified)                           │
│          ├── Change(Method2, Removed)                            │
│          └── Change(Method3, Added)                              │
└─────────────────────────────────────────────────────────────────┘
```

### Key Classes

| Class | Responsibility |
|-------|----------------|
| `ITreeComparer` | Interface for tree comparison with sync/async support |
| `RecursiveTreeComparer` | Level-by-level recursive comparison implementation |
| `ChangeExtensions` | Helper methods for working with hierarchical changes |

### Hierarchical vs Flat Output

The recursive algorithm produces hierarchical `Change` objects where nested changes appear in the `Children` property. For backward compatibility, use `ChangeExtensions.Flatten()`:

```csharp
// Hierarchical (new default)
var changes = comparer.Compare(oldTree, newTree, options);
// Changes[0].Children contains nested method/property changes

// Flat (backward compatible)
var flatChanges = changes.Flatten().ToList();
// All changes at same level, like the old behavior
```
