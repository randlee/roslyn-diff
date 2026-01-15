# roslyn-diff v1.0.0 Release Notes

**Release Date:** January 15, 2026

## Release Highlights

This is the first stable release of roslyn-diff, a semantic diff tool for .NET source code using the Roslyn compiler platform. Unlike traditional line-by-line diff tools, roslyn-diff understands code structure, enabling it to detect and report changes at the semantic level.

### Key Capabilities

- **Semantic Code Understanding** - Uses Roslyn to parse and analyze code structure
- **Multi-Language Support** - Full semantic diff for C# and VB.NET; line-based diff for other files
- **Intelligent Change Detection** - Detects additions, deletions, modifications, renames, and moves
- **Flexible Output** - JSON for AI/tooling, HTML reports, and terminal output
- **Class Comparison** - Compare specific classes with multiple matching strategies

## Features

### Semantic Diff Engine

- **C# Support** - Full semantic analysis of C# source files (.cs)
- **VB.NET Support** - Full semantic analysis of Visual Basic source files (.vb)
- **Line-by-Line Fallback** - Traditional diff for non-.NET files using DiffPlex
- **Rename Detection** - Identifies when symbols are renamed rather than added/removed
- **Move Detection** - Detects code that has been relocated within a file

### Change Detection

Detects changes to:
- Classes, structs, records, and interfaces
- Methods and functions
- Properties and fields
- Namespaces
- Statement-level changes (in Roslyn mode)

### CLI Commands

#### `diff` Command
Compare two files with semantic understanding:
```bash
roslyn-diff diff old.cs new.cs
roslyn-diff diff old.cs new.cs --output json
roslyn-diff diff old.cs new.cs --output html --out-file report.html
```

**Options:**
- `--mode <auto|roslyn|line>` - Select diff algorithm
- `--ignore-whitespace` / `-w` - Ignore whitespace differences
- `--ignore-comments` / `-c` - Ignore comment differences
- `--context` / `-C` - Control context lines
- `--output` / `-o` - Select output format (json, html, text, plain, terminal)
- `--rich` / `-r` - Rich terminal output with colors

#### `class` Command
Compare specific classes between files:
```bash
roslyn-diff class old.cs:Calculator new.cs:Calculator
roslyn-diff class old.cs new.cs --match-by interface --interface IRepository
```

**Options:**
- `--match-by <exact|interface|similarity|auto>` - Matching strategy
- `--interface` / `-i` - Interface name for interface matching
- `--similarity` / `-s` - Similarity threshold (0.0-1.0)

### Output Formats

| Format | Description | Best For |
|--------|-------------|----------|
| `json` | Machine-readable structured output | AI tools, CI/CD, automation |
| `html` | Interactive report with syntax highlighting | Code reviews, documentation |
| `text` | Unified diff format (like git diff) | Quick review, command line |
| `plain` | Simple text without ANSI codes | Piping, scripting |
| `terminal` | Rich console output with colors | Interactive terminal use |

### Programmatic API

Use roslyn-diff as a library in your .NET applications:

```csharp
using RoslynDiff.Core.Differ;
using RoslynDiff.Core.Models;

var factory = new DifferFactory();
var differ = factory.GetDiffer("file.cs", new DiffOptions());
var result = differ.Compare(oldContent, newContent, options);
```

## Installation

### As a .NET Tool (Recommended)

```bash
dotnet tool install -g roslyn-diff
```

### From Source

```bash
git clone https://github.com/randlee/roslyn-diff.git
cd roslyn-diff
dotnet build
```

### Run Without Installing

```bash
cd src/RoslynDiff.Cli
dotnet run -- diff old.cs new.cs
```

## Requirements

- .NET 10.0 or later

## Dependencies

- Microsoft.CodeAnalysis.CSharp 4.12.0
- Microsoft.CodeAnalysis.VisualBasic 4.12.0
- DiffPlex 1.7.2
- Spectre.Console 0.49.x
- System.CommandLine 2.0.0-beta4

## Breaking Changes

None - this is the initial stable release.

## Known Issues

- Large files (1000+ methods) may experience longer processing times (3-10 seconds)
- Roslyn mode requires .cs or .vb files; other extensions fall back to line diff
- System.CommandLine is in beta; some edge cases in argument parsing may exist

## Migration from Pre-release Versions

If upgrading from 0.x pre-release versions:

1. **CLI Changes**
   - `--format` has been renamed to `--output` / `-o`
   - Default output format is now `text` (was `terminal`)
   - Use `--rich` / `-r` for colored terminal output

2. **API Changes**
   - `DiffResult` is now a record type (immutable)
   - `Change.Location` split into `OldLocation` and `NewLocation`
   - `OutputFormatter` interface renamed to `IOutputFormatter`

3. **JSON Output Schema**
   - Schema version updated to `roslyn-diff-output-v1`
   - `summary` object structure changed
   - Location objects now include `startColumn` and `endColumn`

## Documentation

- [README](./README.md) - Getting started guide
- [Usage Guide](./docs/usage.md) - Detailed CLI usage
- [Output Formats](./docs/output-formats.md) - Format specifications
- [API Reference](./docs/api.md) - Programmatic API documentation
- [Architecture](./docs/architecture.md) - Project design
- [Performance Guide](./docs/performance.md) - Benchmarks and optimization

## Future Roadmap

Planned for future releases:
- MCP Server integration for AI tooling (v1.x)
- Folder/project comparison (v2.0)
- Git integration (compare commits, branches) (v2.0)
- F# support via FSharp.Compiler.Service (v2.0)
- Solution-level comparison (v3.0)

## Acknowledgments

- [Roslyn](https://github.com/dotnet/roslyn) - The .NET Compiler Platform
- [DiffPlex](https://github.com/mmanela/diffplex) - .NET Diff library
- [Spectre.Console](https://github.com/spectreconsole/spectre.console) - Beautiful console applications

## License

MIT License - see [LICENSE](./LICENSE) for details.

---

Full changelog available at [CHANGELOG.md](./CHANGELOG.md)
