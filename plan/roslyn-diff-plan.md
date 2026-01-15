# roslyn-diff - Project Plan

## Overview

A semantic diff tool for .NET languages (C# and Visual Basic) using Roslyn, with fallback to line-by-line comparison for non-.NET files. Outputs AI-friendly JSON, human-readable HTML, and terminal output via Spectre.Console.

---

## Requirements Summary

### Core Requirements
- **CLI Framework**: System.CommandLine
- **Terminal Output**: Spectre.Console (see REMINDER below)
- **Target Framework**: .NET 10
- **Output Formats**: Terminal (mandatory), JSON (AI-friendly), HTML (human-readable)

### Diff Capabilities
1. **File Diff**: Compare two .NET source files (C# or VB.NET)
2. **Class Diff**: Compare classes across different files/projects
   - Same class, different versions
   - Same class, different names (refactoring)
   - Different implementations of same interface
   - Cross-language comparison (C# ↔ VB.NET) - Future enhancement
3. **Line-by-line Fallback**:
   - When Roslyn parsing fails
   - Non-.NET files (text, markdown, html, etc.)
   - Explicit `--mode line` option

### Supported Languages

**Semantic Diff (Roslyn):**
- **C#** (.cs files) - via Microsoft.CodeAnalysis.CSharp
- **Visual Basic** (.vb files) - via Microsoft.CodeAnalysis.VisualBasic

**Line-by-line Fallback:**
- All other text files (.txt, .md, .json, .xml, .html, .ts, .js, etc.)

**Not Supported by Roslyn (potential future enhancements):**
- **F#** (.fs, .fsx) - requires FSharp.Compiler.Service (separate compiler, not Roslyn)
- **TypeScript/JavaScript** - would require tree-sitter or TypeScript compiler API
- **HTML/CSS** - would require dedicated parsers or tree-sitter

### Testing
- Comprehensive unit tests
- Integration tests
- Corner case coverage

---

## REMINDER: Spectre.Console

> **When reaching terminal output implementation phase:**
> Review existing Spectre.Console usage in user's other project for patterns and best practices.

---

## Release Phases

### Phase 1: Core CLI (v1.0)
- File-to-file diff
- Class-to-class diff
- All output formats (terminal, JSON, HTML)
- Line-by-line fallback
- Comprehensive tests

### Phase 2: MCP Server (v1.x)
- Expose diff capabilities as MCP tools
- AI-consumable JSON responses

### Phase 3: Folder/Project Comparison (v2.0)
- Compare entire folders
- Project-level (.csproj) comparison
- Git integration (compare commits, branches)

### Phase 4: Solution Level (Nice-to-have)
- Solution-wide comparison
- Cross-project refactoring detection

---

## Architecture

### Solution Structure

```
roslyn-diff/
├── src/
│   ├── RoslynDiff.Cli/              # CLI application
│   │   ├── Program.cs
│   │   ├── Commands/
│   │   │   ├── FileCommand.cs       # diff file <old> <new>
│   │   │   ├── ClassCommand.cs      # diff class <file1:Class1> <file2:Class2>
│   │   │   └── RootCommand.cs
│   │   └── RoslynDiff.Cli.csproj
│   │
│   ├── RoslynDiff.Core/             # Core diff engine
│   │   ├── Differ/
│   │   │   ├── IDiffer.cs
│   │   │   ├── RoslynDifferBase.cs  # Shared Roslyn diff logic
│   │   │   ├── CSharpDiffer.cs      # C# semantic diff
│   │   │   ├── VisualBasicDiffer.cs # VB.NET semantic diff
│   │   │   ├── LineDiffer.cs        # Line-by-line fallback
│   │   │   └── DifferFactory.cs     # Factory to select differ by file extension
│   │   ├── Comparison/
│   │   │   ├── SyntaxComparer.cs    # Syntax tree comparison
│   │   │   ├── SemanticComparer.cs  # Symbol/semantic comparison
│   │   │   └── ClassMatcher.cs      # Match classes by content/interface
│   │   ├── Models/
│   │   │   ├── DiffResult.cs
│   │   │   ├── ChangeSet.cs
│   │   │   ├── Change.cs
│   │   │   ├── ChangeType.cs
│   │   │   └── SymbolInfo.cs
│   │   └── RoslynDiff.Core.csproj
│   │
│   ├── RoslynDiff.Output/           # Output formatters
│   │   ├── IOutputFormatter.cs
│   │   ├── JsonFormatter.cs         # AI-friendly JSON
│   │   ├── HtmlFormatter.cs         # Human-readable HTML
│   │   ├── TerminalFormatter.cs     # Spectre.Console output
│   │   ├── Templates/
│   │   │   └── report.html          # HTML template
│   │   └── RoslynDiff.Output.csproj
│   │
│   └── RoslynDiff.Mcp/              # MCP Server (Phase 2)
│       ├── McpServer.cs
│       ├── Tools/
│       │   ├── DiffFileTool.cs
│       │   └── DiffClassTool.cs
│       └── RoslynDiff.Mcp.csproj
│
├── tests/
│   ├── RoslynDiff.Core.Tests/
│   │   ├── RoslynDifferTests.cs
│   │   ├── LineDifferTests.cs
│   │   ├── SyntaxComparerTests.cs
│   │   ├── SemanticComparerTests.cs
│   │   ├── ClassMatcherTests.cs
│   │   └── TestFixtures/            # Sample C# files for testing
│   │       ├── Simple/
│   │       ├── Refactored/
│   │       ├── EdgeCases/
│   │       └── NonCSharp/
│   │
│   ├── RoslynDiff.Output.Tests/
│   │   ├── JsonFormatterTests.cs
│   │   ├── HtmlFormatterTests.cs
│   │   └── TerminalFormatterTests.cs
│   │
│   └── RoslynDiff.Cli.Tests/
│       └── CommandTests.cs
│
├── samples/                          # Example files for manual testing
│   ├── before/
│   └── after/
│
├── docs/
│   ├── usage.md
│   └── output-formats.md
│
├── roslyn-diff.sln
├── README.md
├── .gitignore
└── Directory.Build.props            # Shared build properties
```

---

## Core Components

### 1. Diff Engine (RoslynDiff.Core)

#### IDiffer Interface
```csharp
public interface IDiffer
{
    DiffResult Compare(string oldContent, string newContent, DiffOptions options);
    bool CanHandle(string filePath, DiffOptions options);
}
```

#### DifferFactory
Selects appropriate differ based on:
1. User-specified `--mode` option
2. File extension:
   - `.cs` → CSharpDiffer
   - `.vb` → VisualBasicDiffer
   - Others → LineDiffer
3. Parse success (Roslyn fails → fallback to LineDiffer)

#### RoslynDifferBase (abstract)
- Common diff logic shared between C# and VB.NET
- Build semantic model if needed
- Compare syntax trees
- Detect: additions, deletions, modifications, moves, renames

#### CSharpDiffer : RoslynDifferBase
- Parse files with `CSharpSyntaxTree.ParseText()`
- C#-specific syntax handling

#### VisualBasicDiffer : RoslynDifferBase
- Parse files with `VisualBasicSyntaxTree.ParseText()`
- VB.NET-specific syntax handling

#### LineDiffer
- Standard line-by-line diff algorithm
- Supports any text file
- Uses DiffPlex or similar library

#### ClassMatcher
Matches classes between files using:
1. **Exact name match**
2. **Interface implementation match**
3. **Content similarity** (for renamed classes)
4. **Explicit mapping** (user-specified)

### 2. Comparison Models

```csharp
public record DiffResult
{
    public string OldPath { get; init; }
    public string NewPath { get; init; }
    public DiffMode Mode { get; init; }  // Roslyn or Line
    public IReadOnlyList<FileChange> FileChanges { get; init; }
    public DiffStats Stats { get; init; }
}

public record FileChange
{
    public string Path { get; init; }
    public IReadOnlyList<Change> Changes { get; init; }
}

public record Change
{
    public ChangeType Type { get; init; }      // Added, Removed, Modified, Moved, Renamed
    public ChangeKind Kind { get; init; }      // Class, Method, Property, Statement, Line
    public string Name { get; init; }          // Symbol name (if semantic)
    public Location OldLocation { get; init; }
    public Location NewLocation { get; init; }
    public string OldContent { get; init; }
    public string NewContent { get; init; }
    public IReadOnlyList<Change> Children { get; init; }  // Nested changes
}

public enum ChangeType { Added, Removed, Modified, Moved, Renamed, Unchanged }
public enum ChangeKind { File, Namespace, Class, Method, Property, Field, Statement, Line }
```

### 3. Output Formatters

#### JsonFormatter
- AI-optimized structure
- Includes semantic information
- Complete context for LLM understanding

#### HtmlFormatter
- Side-by-side view
- Syntax highlighting (highlight.js or Prism)
- Collapsible sections
- File navigation
- Summary statistics

#### TerminalFormatter
**Plain Text (default)**
- Simple text diff output (similar to standard diff)
- Works in any terminal/environment
- Easy to pipe/redirect

**Spectre.Console (opt-in via `--rich` or `--color`)**
- Color-coded diff output
- Tree view for structural changes
- Tables for statistics
- Progress indicators for large diffs

---

## CLI Commands

### Root Command
```bash
roslyn-diff [options]
```

### Subcommands

#### File Diff
```bash
roslyn-diff file <old-file> <new-file> [options]

Options:
  -o, --output <format>    Output format: terminal|json|html (default: terminal)
  -m, --mode <mode>        Diff mode: auto|roslyn|line (default: auto)
  --out-file <path>        Write output to file
  --rich                   Use Spectre.Console for rich terminal output (colors, tables)
  --ignore-whitespace      Ignore whitespace changes
  --ignore-comments        Ignore comment changes
  --context <lines>        Lines of context (default: 3)
```

#### Class Diff
```bash
roslyn-diff class <source1> <source2> [options]

Arguments:
  source1                  File path or file:ClassName
  source2                  File path or file:ClassName

Options:
  -o, --output <format>    Output format: terminal|json|html
  --match-by <strategy>    Matching: name|interface|content (default: name)
  --interface <name>       Match classes implementing this interface
  --similarity <percent>   Content similarity threshold (default: 70)
  --out-file <path>        Write output to file
  --rich                   Use Spectre.Console for rich terminal output
```

### Examples
```bash
# Compare two files
roslyn-diff file old/Service.cs new/Service.cs

# Compare specific classes
roslyn-diff class old/Service.cs:UserService new/Service.cs:UserService

# Compare implementations of same interface
roslyn-diff class proj1/Handler.cs proj2/Handler.cs --match-by interface --interface IRequestHandler

# Compare with content similarity (for renamed classes)
roslyn-diff class old/Foo.cs:OldName new/Bar.cs:NewName --match-by content

# Output JSON for AI consumption
roslyn-diff file old.cs new.cs -o json --out-file diff.json

# Generate HTML report
roslyn-diff file old.cs new.cs -o html --out-file report.html

# Rich terminal output with colors and tables
roslyn-diff file old.cs new.cs --rich

# Force line-by-line mode
roslyn-diff file config.json other-config.json --mode line
```

---

## JSON Output Schema (AI-Friendly)

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
  "changes": [
    {
      "id": "change-001",
      "type": "Modified",
      "kind": "Method",
      "name": "ProcessOrder",
      "fullyQualifiedName": "MyApp.Services.OrderService.ProcessOrder",
      "oldLocation": {
        "file": "old/Service.cs",
        "startLine": 42,
        "endLine": 58,
        "startColumn": 5
      },
      "newLocation": {
        "file": "new/Service.cs",
        "startLine": 45,
        "endLine": 65,
        "startColumn": 5
      },
      "signatureChange": {
        "changed": true,
        "oldSignature": "public void ProcessOrder(int orderId)",
        "newSignature": "public async Task ProcessOrder(int orderId, CancellationToken ct)"
      },
      "bodyChange": {
        "changed": true,
        "childChanges": [
          {
            "type": "Added",
            "kind": "Statement",
            "description": "Added null check",
            "newContent": "ArgumentNullException.ThrowIfNull(orderId);",
            "newLocation": { "startLine": 47 }
          }
        ]
      }
    }
  ],
  "context": {
    "oldFileContent": "// Full old file for reference...",
    "newFileContent": "// Full new file for reference..."
  }
}
```

---

## Test Coverage Plan

### Unit Tests

#### RoslynDiffer Tests
- [ ] Empty files comparison
- [ ] Identical files (no changes)
- [ ] Single class added
- [ ] Single class removed
- [ ] Class renamed
- [ ] Method added to class
- [ ] Method removed from class
- [ ] Method signature changed
- [ ] Method body changed
- [ ] Property added/removed/modified
- [ ] Field added/removed/modified
- [ ] Namespace changed
- [ ] Using statements changed
- [ ] Multiple classes in single file
- [ ] Partial classes
- [ ] Nested classes
- [ ] Generic classes/methods
- [ ] Async methods
- [ ] Expression-bodied members
- [ ] Records and record structs
- [ ] Primary constructors
- [ ] Attributes changed
- [ ] XML documentation changed
- [ ] Code moved within file
- [ ] Large files (performance)

#### LineDiffer Tests
- [ ] Empty files
- [ ] Identical files
- [ ] Single line added
- [ ] Single line removed
- [ ] Single line modified
- [ ] Multiple scattered changes
- [ ] Whitespace-only changes
- [ ] Large files (performance)
- [ ] Binary file handling (should fail gracefully)

#### ClassMatcher Tests
- [ ] Exact name match
- [ ] Name mismatch with same interface
- [ ] Content similarity matching
- [ ] Multiple candidate matches
- [ ] No matches found
- [ ] Partial class matching

#### Output Formatter Tests
- [ ] JSON schema compliance
- [ ] HTML valid markup
- [ ] Terminal output rendering
- [ ] Special characters escaped
- [ ] Large output handling

### Integration Tests
- [ ] End-to-end CLI file diff
- [ ] End-to-end CLI class diff
- [ ] Output file generation
- [ ] Fallback to line diff
- [ ] Error handling (missing files, invalid syntax)

### Edge Cases
- [ ] File with syntax errors (partial parse)
- [ ] Mixed encoding (UTF-8, UTF-16)
- [ ] Very long lines
- [ ] Files with only whitespace changes
- [ ] Circular/recursive structures
- [ ] Files with preprocessor directives (#if, #region)
- [ ] Files with string interpolation
- [ ] Files with raw string literals

---

## Dependencies

### Core NuGet Packages
```xml
<!-- RoslynDiff.Core -->
<PackageReference Include="Microsoft.CodeAnalysis.CSharp" Version="4.12.0" />
<PackageReference Include="Microsoft.CodeAnalysis.CSharp.Workspaces" Version="4.12.0" />
<PackageReference Include="Microsoft.CodeAnalysis.VisualBasic" Version="4.12.0" />
<PackageReference Include="Microsoft.CodeAnalysis.VisualBasic.Workspaces" Version="4.12.0" />
<PackageReference Include="DiffPlex" Version="1.7.2" />  <!-- Line diff fallback -->

<!-- RoslynDiff.Cli -->
<PackageReference Include="System.CommandLine" Version="2.0.0-beta4.22272.1" />
<PackageReference Include="Spectre.Console" Version="0.49.1" />
<PackageReference Include="Spectre.Console.Cli" Version="0.49.1" />

<!-- RoslynDiff.Output -->
<PackageReference Include="System.Text.Json" Version="9.0.0" />

<!-- Tests -->
<PackageReference Include="xunit" Version="2.9.0" />
<PackageReference Include="FluentAssertions" Version="7.0.0" />
<PackageReference Include="NSubstitute" Version="5.3.0" />
<PackageReference Include="Verify.Xunit" Version="28.0.0" />  <!-- Snapshot testing -->
```

---

## Implementation Milestones

### Milestone 1: Project Setup (Foundation)
- [ ] Create solution and project structure
- [ ] Configure Directory.Build.props
- [ ] Set up .gitignore
- [ ] Add NuGet package references
- [ ] Create basic CLI skeleton with System.CommandLine
- [ ] Set up test projects

### Milestone 2: Line Diff Implementation
- [ ] Implement LineDiffer using DiffPlex
- [ ] Create DiffResult models
- [ ] Basic terminal output
- [ ] Unit tests for LineDiffer

### Milestone 3: Roslyn Syntax Diff
- [ ] Implement basic RoslynDiffer (syntax level)
- [ ] Parse and compare syntax trees
- [ ] Detect class/method/property changes
- [ ] Unit tests for syntax comparison

### Milestone 4: Roslyn Semantic Diff
- [ ] Add semantic model analysis
- [ ] Detect renames and moves
- [ ] Implement ClassMatcher
- [ ] Unit tests for semantic comparison

### Milestone 5: Output Formatters
- [ ] Implement JsonFormatter with schema
- [ ] Implement HtmlFormatter with template
- [ ] Implement TerminalFormatter with Spectre.Console
  - **REMINDER: Review user's existing Spectre.Console project**
- [ ] Unit tests for formatters

### Milestone 6: CLI Polish
- [ ] Complete file command
- [ ] Complete class command
- [ ] Error handling and validation
- [ ] Help text and examples
- [ ] Integration tests

### Milestone 7: Testing & Documentation
- [ ] Complete all unit tests
- [ ] Edge case tests
- [ ] Performance tests
- [ ] Write usage documentation
- [ ] Create sample files

### Milestone 8: Release v1.0
- [ ] Final testing
- [ ] README completion
- [ ] NuGet package (optional)
- [ ] GitHub release

---

## Future Enhancements (Post v1.0)

### v1.x - MCP Server
- Expose diff tools via MCP protocol
- JSON responses optimized for AI
- Streaming for large diffs

### v2.0 - Folder/Project/Git
- Folder comparison
- .csproj awareness
- Git integration
  - Compare commits
  - Compare branches
  - Compare staged changes
  - PR diff summary

### v3.0 - Solution Level
- Full solution comparison
- Cross-project refactoring detection
- Dependency analysis

### Future - Additional Language Support
- **F#** via FSharp.Compiler.Service
- **TypeScript/JavaScript** via tree-sitter or TypeScript compiler API
- **Other languages** via tree-sitter bindings

---

## Decisions Made

1. **NuGet Package Strategy**
   - Single NuGet package containing all assemblies (RoslynDiff.Core, RoslynDiff.Output, RoslynDiff.Cli)
   - Package name: `RoslynDiff`

2. **CLI Framework**
   - **System.CommandLine** for command parsing
   - **Spectre.Console** for optional formatted terminal output
   - **Plain text as default** terminal output (Spectre.Console opt-in for rich formatting)

3. **HTML Generation**
   - Raw HTML string building for v1.0
   - Can evaluate template engines (Scriban, Razor) later if needed

4. **Line Diff Library**
   - DiffPlex (well-tested, maintained)

5. **Snapshot Testing**
   - Verify.Xunit for output format testing
   - Stores expected outputs as files, easy to review changes

---

## Notes

- Version numbers for NuGet packages should be verified at project start (latest stable for .NET 10)
- Single `RoslynDiff` NuGet package contains all assemblies (Core, Output, Cli)
- MCP server implementation should follow latest MCP SDK patterns (Phase 2)
