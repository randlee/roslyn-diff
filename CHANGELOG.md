# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Planned
- MCP Server integration for AI tooling
- Folder/project comparison
- Git integration (compare commits, branches)
- F# support via FSharp.Compiler.Service

## [0.5.0] - 2026-01-15

### Added

#### Core Features
- **C# Semantic Diff** - Full semantic analysis of C# source files using Roslyn
- **VB.NET Semantic Diff** - Full semantic analysis of Visual Basic source files
- **Line-by-Line Diff** - Fallback diff for non-.NET files using DiffPlex
- **Rename Detection** - Detects when symbols are renamed (not just added/removed)
- **Move Detection** - Identifies code that has been moved within a file

#### Change Detection
- Class/struct/record/interface changes
- Method/function changes
- Property changes
- Field changes
- Namespace changes
- Statement-level changes (in Roslyn mode)

#### CLI Commands
- `diff` - Compare two files
  - `--mode` option: `auto`, `roslyn`, `line`
  - `--ignore-whitespace` / `-w` - Ignore whitespace differences
  - `--ignore-comments` / `-c` - Ignore comment differences
  - `--context` / `-C` - Control context lines
  - `--output` / `-o` - Select output format
  - `--out-file` - Write to file
  - `--rich` / `-r` - Rich terminal output

- `class` - Compare specific classes between files
  - `--match-by` / `-m` - Match strategy: `exact`, `interface`, `similarity`, `auto`
  - `--interface` / `-i` - Interface name for interface matching
  - `--similarity` / `-s` - Similarity threshold
  - `--output` / `-o` - Select output format
  - `--out-file` / `-f` - Write to file

#### Output Formats
- **JSON** - Machine-readable format for AI/tooling
- **HTML** - Interactive report with syntax highlighting
- **Text** - Unified diff format (similar to git diff)
- **Plain** - Simple text without ANSI codes
- **Terminal** - Rich console output with Spectre.Console

#### Class Matching Strategies
- Exact name matching
- Interface implementation matching
- Content similarity matching (for renamed classes)
- Auto mode (exact first, then similarity)

#### API
- `DifferFactory` for creating appropriate differs
- `IDiffer` interface for custom differ implementations
- `OutputFormatterFactory` for creating formatters
- `IOutputFormatter` interface for custom formatters
- `ClassMatcher` for flexible class matching

### Dependencies
- .NET 10.0
- Microsoft.CodeAnalysis.CSharp 4.12.0
- Microsoft.CodeAnalysis.VisualBasic 4.12.0
- DiffPlex 1.7.2
- Spectre.Console 0.49.x
- System.CommandLine 2.0.0-beta4

---

## Version History

### Versioning Strategy

This project follows [Semantic Versioning](https://semver.org/):

- **MAJOR** version for incompatible API changes
- **MINOR** version for backward-compatible functionality additions
- **PATCH** version for backward-compatible bug fixes

### Release Phases

| Phase | Version | Features |
|-------|---------|----------|
| Phase 1 | v0.5.0 | Core CLI (current) |
| Phase 2 | v1.x | MCP Server integration |
| Phase 3 | v2.0 | Folder/project/git support |
| Phase 4 | v3.0 | Solution-level comparison |

---

## Migration Guide

### From 0.x to 1.0

If upgrading from pre-release versions:

1. **CLI Changes**
   - `--format` renamed to `--output` / `-o`
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

---

## Links

- [GitHub Repository](https://github.com/randlee/roslyn-diff)
- [Issue Tracker](https://github.com/randlee/roslyn-diff/issues)
- [Documentation](./docs/)
