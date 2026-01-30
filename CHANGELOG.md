# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

## [0.10.0] - 2026-01-28

### Added

#### Multi-File Comparison
- **Git Branch Comparison** - New `--git-compare` option compares files between git refs
  - Ref range syntax: `main..feature`, `v1.0..v2.0`, `abc123..def456`
  - Compares HEAD states of two branches, tags, or commits
  - Only analyzes changed files for efficiency
  - Works with LibGit2Sharp for robust git integration
- **Folder-to-Folder Comparison** - Auto-detects directories and compares entire folder structures
  - Matches files by relative path
  - Detects modified, added, and removed files
  - Supports recursive and non-recursive modes
  - Parallel processing for optimal performance
- **File Filtering with Glob Patterns** - Precise control over which files to compare
  - `--include <pattern>` - Include files matching glob pattern (repeatable)
  - `--exclude <pattern>` - Exclude files matching glob pattern (repeatable)
  - Simplified glob syntax: `*` (any chars), `**` (recursive), `?` (single char)
  - Common patterns: `*.cs`, `src/**/*.cs`, `**/*.g.cs`, `bin/**`
  - Exclude takes precedence over include
  - Case-insensitive matching
- **Recursive Directory Traversal** - `--recursive` or `-r` flag
  - Default: Non-recursive (top-level files only)
  - When enabled: Traverses all subdirectories
  - Matches files at any depth
- **JSON Schema v3** - Enhanced schema for multi-file comparison
  - Backward compatible with single-file mode
  - `comparisonMode`: `"git"` or `"folder"`
  - `files` array with per-file results
  - File status: `modified`, `added`, `removed`, `renamed`
  - Aggregated summary with file counts
  - Per-file `DiffResult` embedded in `files[].result`
- **Parallel Processing** - Multi-file comparison uses parallel processing by default
  - Analyzes files concurrently on multiple CPU cores
  - Automatic thread pool management
  - Early termination for unchanged files
  - Optimized for large repositories

#### CLI Enhancements
- `--git-compare <ref-range>` - Git comparison mode (e.g., `main..feature`)
- `--recursive` / `-r` - Recursively traverse subdirectories
- `--include <pattern>` - Include files matching glob pattern (repeatable)
- `--exclude <pattern>` - Exclude files matching glob pattern (repeatable)
- Auto-detection of folder comparison when both arguments are directories
- Clear error messages for invalid ref ranges and patterns

#### Documentation
- **Multi-File Comparison Guide** (`docs/multi-file-comparison.md`)
  - Complete feature documentation
  - Git and folder comparison examples
  - Glob pattern reference
  - Performance tips
  - Integration patterns for CI/CD
- **Glob Patterns Reference** (`docs/GLOB_PATTERNS.md`)
  - Detailed glob syntax documentation
  - Pattern matching rules
  - Common filter examples
- **Multi-File Samples** (`samples/multi-file/`)
  - Folder comparison examples
  - Git comparison examples
  - Filtering examples
  - Integration patterns
  - Sample JSON outputs (schema v3)

### Changed
- Enhanced `DiffCommand` with folder and git comparison support
- Enhanced `DiffResult` to support both single-file and multi-file modes
- JSON formatter updated to generate schema v3
- Metadata structure enhanced for multi-file context

### Use Cases Enabled
- **Pull Request Reviews** - Analyze entire changesets before merging
- **Release Comparisons** - Compare releases or tagged versions
- **Migration Analysis** - Assess impact of large-scale refactorings
- **Codebase Audits** - Review changes across an entire project
- **CI/CD Integration** - Automated change analysis in build pipelines
- **Pre-Push Validation** - Catch breaking changes before pushing
- **Release Notes Automation** - Extract breaking changes for documentation

### Performance
- Parallel processing for multi-file comparison (leverages all CPU cores)
- Efficient git object access via LibGit2Sharp
- Early termination for unchanged files
- Memory-efficient streaming for large files
- Typical performance: 100 files in ~5 seconds

### Dependencies
- Added `LibGit2Sharp` v0.30.0 for git integration

### Known Limitations
- HTML output for multi-file is not yet implemented (JSON only)
- No cross-file rename detection (per-file only)
- Binary files reported but not diffed
- No directory structure comparison (files only)

### Migration from v0.9.0
- JSON schema updated to v3 (breaking change for multi-file mode)
- Single-file JSON output remains backward compatible with v2
- No CLI changes for single-file comparison
- New `--git-compare` option for git mode
- New `--include` / `--exclude` options for filtering
- New `--recursive` option for folder mode

## [0.10.0-beta] - 2026-01-28 (Previously Released)

### Added

#### Inline Diff View
- **Inline View Mode for HTML Output** - New `--inline` option displays diffs line-by-line with +/- markers
  - Similar to traditional git diff output while maintaining semantic intelligence
  - Two modes: full file view or contextual view with N lines around changes
  - Works with both document and fragment HTML modes
  - Compatible with all impact classification and filtering options
- **Full File Mode** - Show entire file content with inline diff markers (`--inline`)
  - Changed lines marked with `+` (additions) or `-` (deletions)
  - Unchanged lines shown without markers for complete context
  - Best for smaller files or comprehensive reviews
- **Context Mode** - Show only N lines around changes (`--inline=N`)
  - Similar to `git diff -U N` for compact output
  - Reduces scrolling in large files with isolated changes
  - Configurable context lines (e.g., `--inline=3`, `--inline=5`, `--inline=10`)

#### CLI Enhancements
- `--inline [context-lines]` - Enable inline view mode for HTML output
  - Without value: shows full file with inline markers
  - With number: shows N lines of context around changes
  - Examples: `--inline`, `--inline=3`, `--inline=10`

#### HTML Output Features
- **Line Markers** - Clear `+` and `-` prefixes with color-coded backgrounds
- **Syntax Highlighting** - Full C# and VB.NET syntax highlighting in inline view
- **Impact Indicators** - Impact badges and warnings integrated with inline view
- **Change Sections** - Changes grouped by location with collapsible sections
- **Familiar Workflow** - Traditional diff format for developers accustomed to git diff

#### Documentation
- **Inline View Section** in `docs/output-formats.md`
  - Tree view vs. inline view comparison table
  - Full file vs. context mode explanation
  - CLI usage examples and options
  - Use case recommendations
  - Integration with fragment mode examples
- **README.md Examples** - Added inline view examples to Quick Start
- **Samples** - Sample inline view HTML outputs (planned)

### Use Cases Enabled
- **Code Reviews** - Line-by-line review workflow familiar to developers
- **Patch Documentation** - Generate HTML patches for documentation or archival
- **Detailed Analysis** - Examine exact line-level modifications and whitespace
- **Traditional Git Workflow** - Maintain familiar diff format in HTML reports
- **Integration Scenarios** - Embed inline diffs in dashboards and documentation

### Changed
- Enhanced `ViewMode` enum with `Inline` option
- Enhanced `OutputOptions` with `InlineContext` property (null = full file, N = context lines)
- Enhanced `HtmlFormatter` to support both tree and inline view rendering
- CLI help text updated with inline view examples

## [0.9.0] - 2026-01-28

### Added

#### HTML Fragment Mode
- **Embeddable HTML Output** - New `--html-mode fragment` option generates embeddable HTML fragments
  - Outputs HTML fragment without document wrapper (`<html>`, `<head>`, `<body>` tags)
  - Generates external CSS file instead of embedding styles
  - Perfect for integration into existing web applications, documentation sites, and dashboards
- **External CSS Extraction** - `--extract-css` option to specify CSS filename (default: `roslyn-diff.css`)
  - CSS scoped to `.roslyn-diff-fragment` container
  - Uses CSS custom properties (variables) for easy theming
  - No external dependencies, self-contained stylesheet
- **Data Attributes for Metadata** - Fragment root element includes data attributes for JavaScript integration
  - File names: `data-old-file`, `data-new-file`
  - Change statistics: `data-changes-total`, `data-changes-added`, `data-changes-removed`, `data-changes-modified`
  - Impact statistics: `data-impact-breaking-public`, `data-impact-breaking-internal`, `data-impact-non-breaking`, `data-impact-formatting`
  - Diff mode: `data-mode` (roslyn/line)
- **Fragment Mode Examples** - New `samples/fragment-mode/` directory with integration examples
  - `fragment.html` - Example generated fragment
  - `roslyn-diff.css` - Example extracted CSS
  - `parent.html` - Complete example showing fragment embedding in dashboard
  - `README.md` - Comprehensive guide to fragment mode integration patterns

#### CLI Enhancements
- `--html-mode <mode>` - Select HTML generation mode: `document` (default) or `fragment`
- `--extract-css <filename>` - Specify CSS filename for fragment mode (default: `roslyn-diff.css`)

#### Documentation
- **Fragment Mode Documentation** - Comprehensive section in `docs/output-formats.md`
  - How fragment mode works
  - Data attribute reference
  - Embedding examples (PHP, Node.js, Ruby, Python, JavaScript)
  - Multiple fragments example
  - Styling and theming guide
  - Use case recommendations
  - Document vs. Fragment comparison table
- **Fragment Mode Section in README** - Quick start guide and use cases
- **Sample Integration Patterns** - Examples for all major integration scenarios
  - Server-side includes (PHP, Node.js, Ruby, Python)
  - Client-side loading (JavaScript, jQuery, React)
  - Static site generators (Jekyll, Hugo, Gatsby)
  - Code review dashboards
  - CI/CD reports

### Changed
- Enhanced `OutputOptions` with `HtmlMode` and `ExtractCssPath` properties
- Enhanced `HtmlFormatter` to support both document and fragment modes
- Fragment mode uses external CSS instead of embedded styles for better caching and reusability

### Use Cases Enabled
- **Documentation Sites** - Embed semantic diffs in changelog pages and upgrade guides
- **Code Review Tools** - Build custom review interfaces with multiple embedded diffs
- **CI/CD Dashboards** - Display diff reports in build pipeline status pages
- **Static Site Generators** - Include diffs in Jekyll, Hugo, Gatsby, or Docusaurus sites
- **Content Management Systems** - Embed diffs in CMS-based documentation platforms

## [Unreleased] - Future Features

### Added

#### Performance Analysis & Benchmarking
- **ComprehensiveDiffModeBenchmarks** - Complete benchmark suite comparing text diff vs semantic diff modes
  - 16 benchmark scenarios across 4 file sizes (50, 500, 2000, 5000 lines)
  - Tests both small changes (0.2-2% modified) and large changes (30% + rearrangement)
  - Measures time, memory allocation, and GC pressure for both modes
  - Realistic code generation with method signatures, bodies, and rearrangement patterns
- **ExtendedScaleBenchmarks** - Large file validation (3000-5000 lines)
  - Validates DESIGN-003 performance projections
  - Tests identical file comparison (early termination)
  - Confirms scaling characteristics
- **Comprehensive Performance Documentation**
  - Performance comparison guide in docs/performance.md
  - Benchmark results with actual data
  - Use case recommendations (when to use text vs semantic diff)
  - Hybrid strategy guidance for optimal performance
- **Performance Analysis Reports** (in project root, pending integration)
  - COMPREHENSIVE_DIFF_COMPARISON.md - Detailed 16-scenario analysis
  - BENCHMARK_SAMPLE_DATA_SPEC.md - Test data specification
  - PERFORMANCE_ANALYSIS.md - Extended scale analysis
  - PERFORMANCE_CHARTS.md - Visual performance charts
  - BENCHMARK_ANALYSIS_INDEX.md - Master navigation guide

### Performance Findings
- **Text Diff**: 2-80× faster than semantic diff depending on change density
- **Semantic Diff**: 18× more memory but perfect structural accuracy
- **GC Pressure**: Semantic diff triggers 13-18× more Gen0 collections
- **Scaling**: Text diff scales linearly (O(n)), semantic diff scales with file size × change complexity
- **Early Termination**: Both modes handle identical files in nanoseconds

### Planned
- MCP Server integration for AI tooling
- Folder/project comparison
- Git integration (compare commits, branches)
- F# support via FSharp.Compiler.Service

## [0.8.0] - 2026-01-20

### Added

#### Impact Classification
- **Automatic Impact Classification** - Every change is categorized by its potential impact on consumers
  - Breaking Public API - Changes that break external consumers (method signature changes, removed public members)
  - Breaking Internal API - Changes that break internal consumers (internal method renames, parameter changes)
  - Non-Breaking - Safe changes with no execution impact (private field renames, code reordering)
  - Formatting Only - Pure whitespace/comment changes
- **Visibility Tracking** - Each change includes visibility information (public, internal, private, protected)
- **Caveat Warnings** - Warnings for edge cases (e.g., "parameter rename may break named arguments")
- **Impact Filtering** - Filter changes by impact level in JSON output

#### Whitespace Intelligence
- **4 Whitespace Modes** - Fine-grained whitespace handling
  - Exact - Preserve all whitespace (default)
  - IgnoreLeadingTrailing - Ignore leading/trailing whitespace
  - IgnoreAll - Ignore all whitespace differences
  - LanguageAware - Language-specific handling (exact for Python/YAML, normalized for C#/Java)
- **Automatic Issue Detection** - Detects indentation changes, mixed tabs/spaces, trailing whitespace, line ending changes
- **Whitespace Warnings** - Visual indicators in HTML output for whitespace issues
- **CLI Options** - `--whitespace-mode` and `-w` shortcut for `--whitespace-mode ignore-all`

#### Multi-Target Framework (Multi-TFM) Support
- **Target Framework Analysis** - Analyze code differences across multiple .NET target frameworks simultaneously
- **TFM-Specific Change Detection** - Identify which changes apply to specific target frameworks vs. all frameworks
- **Conditional Compilation Awareness** - Properly handles `#if` directives and framework-specific code
- **Per-TFM Semantic Analysis** - Runs full Roslyn analysis for each target framework with correct compilation symbols
- **Parallel TFM Processing** - Optimized multi-TFM analysis with parallel processing

#### CLI Enhancements
- `--impact-level` - Filter by impact: `breaking-public`, `breaking-internal`, `non-breaking`, `all`
- `--include-non-impactful` - Include non-breaking and formatting changes in JSON output
- `--include-formatting` - Include formatting-only changes
- `--whitespace-mode` - Whitespace handling mode
- `--target-framework` / `-t` - Specify one or more target frameworks for analysis (can be repeated)
- `-T` / `--target-frameworks` - Specify semicolon-separated list of target frameworks (e.g., "net8.0;net10.0")
- TFM validation with helpful error messages for invalid framework identifiers
- Support for common TFM formats: `net8.0`, `net10.0`, `netcoreapp3.1`, `netstandard2.0`, `net462`, etc.

#### Output Format Enhancements
- **Impact Indicators** - Color-coded impact badges in HTML, impact properties in JSON
- **Whitespace Warnings** - Visual indicators for whitespace issues in HTML output
- **TFM Annotations** - Target framework information in all output formats
  - JSON: `targetFrameworks` array in metadata and `applicableToTfms` per change
  - HTML: TFM badges and framework-specific indicators
  - Text/Plain: TFM annotations like `[.NET 8.0]` for framework-specific changes

#### Architecture Improvements
- `ImpactClassifier` - Analyzes and classifies change impact
- `WhitespaceAnalyzer` - Detects whitespace issues
- `TfmSymbolResolver` - Maps TFMs to preprocessor symbols
- `TfmParser` - Validates and parses TFM strings
- `PreprocessorDirectiveDetector` - Pre-scan optimization for conditional compilation
- `TfmResultMerger` - Intelligent merging of per-TFM analysis results
- Enhanced `DiffResult` model with impact breakdown and TFM metadata
- Enhanced `Change` model with `Impact`, `Visibility`, `Caveats`, and `ApplicableToTfms` properties

#### Documentation
- **Impact Classification Guide** (`docs/impact-classification.md`) - Complete guide to impact levels, filtering, and use cases
- **Whitespace Handling Guide** (`docs/whitespace-handling.md`) - Whitespace modes and best practices
- **Multi-TFM Support Guide** (`docs/tfm-support.md`) - Multi-target framework analysis and conditional compilation
- Sample files demonstrating TFM scenarios and whitespace handling
- Updated API documentation with programmatic examples

### Changed
- Enhanced `DiffOptions` with `ImpactLevel`, `IncludeNonImpactful`, `IncludeFormatting`, `WhitespaceMode`, and `TargetFrameworks` properties
- Updated all output formatters to display impact, whitespace warnings, and TFM metadata
- JSON output defaults to showing only breaking changes (`IncludeNonImpactful: false`)
- HTML output defaults to showing all changes for review

### Performance
- Multi-TFM overhead is minimal (~2-2.5x for 3 frameworks vs. single framework)
- Pre-scan optimization skips multi-TFM analysis when no `#if` directives exist
- Parallel TFM processing where possible
- Efficient change correlation algorithms

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
