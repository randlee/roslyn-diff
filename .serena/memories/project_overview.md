# roslyn-diff Project Overview

## Purpose
roslyn-diff is a semantic diff tool for .NET using Roslyn. It provides semantic-aware diffing capabilities for C# code, understanding code structure rather than just line-by-line text comparison.

## Tech Stack
- .NET 10.0
- C#
- Roslyn (Microsoft.CodeAnalysis) for semantic code analysis
- Spectre.Console for rich terminal output
- xUnit for testing with FluentAssertions and NSubstitute

## Project Structure
- `src/RoslynDiff.Core/` - Core diff engine and models
- `src/RoslynDiff.Output/` - Output formatters (JSON, HTML, Plain Text, Terminal/Spectre)
- `src/RoslynDiff.Cli/` - Command-line interface
- `tests/` - Unit tests for each project

## Key Models (in RoslynDiff.Core.Models)
- `DiffResult` - Main result containing file changes and stats
- `Change` - Individual change with type, kind, location, content
- `ChangeType` - Added, Removed, Modified, Moved, Renamed, Unchanged
- `ChangeKind` - File, Namespace, Class, Method, Property, Field, Statement, Line
- `DiffStats` - Statistics about changes

## Output Formatters
Implement `IOutputFormatter` interface with:
- `Format` property (format identifier like "json", "text", "terminal")
- `ContentType` property (MIME type)
- `FormatResult()` method
- `FormatResultAsync()` method

Available formatters:
- JsonOutputFormatter - JSON output
- PlainTextFormatter - Simple text (no ANSI codes, good for pipes)
- SpectreConsoleFormatter - Rich terminal output with colors
- HtmlFormatter - HTML output
- UnifiedFormatter - Unified diff format
