# roslyn-diff - Sprint Plan

## Progress Summary (Updated: 2026-01-15)

| Sprint | Status | Tests | Key Deliverables |
|--------|--------|-------|------------------|
| Sprint 1 | ✅ Complete | 52 | LineDiffer, DifferFactory, Basic CLI |
| Sprint 2 | ✅ Complete | 65 | CSharpDiffer, SyntaxComparer, NodeMatcher |
| Sprint 3 | ✅ Complete | 242 | VisualBasicDiffer, SemanticComparer, SymbolMatcher |
| Sprint 4 | ✅ Complete | 242 | JsonFormatter, HtmlFormatter, PlainText, SpectreConsole |
| Sprint 5 | ✅ Complete | 361 | ClassMatcher, `class` command, CLI options |
| Sprint 6 | ✅ Complete | 650 | Edge case tests, Integration tests, Documentation, Benchmarks |
| Sprint 7 | ✅ Complete | 650 | NuGet package config, Release artifacts, Final testing |

### Current State
- **Branch**: release/v0.5.0
- **Open PR**: #4 (release/v0.5.0 → main) - Contains Sprint 7
- **Total Tests**: 650 passing (321 Core + 130 Output + 84 CLI + 115 Integration)
- **Documentation**: Complete (README, docs/, samples/, CHANGELOG, RELEASE_NOTES)
- **Package**: Configured and tested (ready for NuGet publish)

---

## Sprint Overview

| Sprint | Focus | Duration | Dependencies |
|--------|-------|----------|--------------|
| Sprint 1 | Foundation & Line Diff | 1 sprint | None |
| Sprint 2 | C# Roslyn Diff | 1 sprint | Sprint 1 |
| Sprint 3 | VB.NET + Semantic Diff | 1 sprint | Sprint 2 |
| Sprint 4 | Output Formatters | 1 sprint | Sprint 2 (can parallel with Sprint 3) |
| Sprint 5 | CLI Polish & Class Diff | 1 sprint | Sprints 3, 4 |
| Sprint 6 | Testing & Documentation | 1 sprint | Sprint 5 |
| Sprint 7 | Release v1.0 | 1 sprint | Sprint 6 |

---

## Sprint 1: Foundation & Line Diff

### Goals
- Project scaffolding complete
- Line-by-line diff fully working
- Basic CLI operational

### Tasks

#### Parallel Work Stream A: Project Setup
- [ ] Create solution structure
- [ ] Configure Directory.Build.props (.NET 10, common settings)
- [ ] Set up .gitignore
- [ ] Create all project files (.csproj)
- [ ] Add NuGet package references
- [ ] Create README.md skeleton

#### Parallel Work Stream B: Core Models
- [ ] Define `IDiffer` interface
- [ ] Create `DiffResult` model
- [ ] Create `Change` model
- [ ] Create `ChangeType` enum
- [ ] Create `ChangeKind` enum
- [ ] Create `DiffOptions` model
- [ ] Create `Location` model

#### Sequential (after A & B):
- [ ] Implement `LineDiffer` using DiffPlex
- [ ] Implement `DifferFactory` (line diff only for now)
- [ ] Basic CLI with `file` command (plain text output only)
- [ ] Unit tests for LineDiffer
- [ ] Unit tests for models

### Deliverables
- Working `roslyn-diff file old.txt new.txt` with line diff
- Test coverage for LineDiffer

### Definition of Done
- [ ] `dotnet build` succeeds
- [ ] `dotnet test` passes
- [ ] CLI can diff two text files
- [ ] Plain text output works

---

## Sprint 2: C# Roslyn Diff (Syntax Level)

### Goals
- C# syntax tree comparison working
- Detect structural changes (classes, methods, properties)

### Tasks

#### Parallel Work Stream A: Roslyn Infrastructure
- [ ] Create `RoslynDifferBase` abstract class
- [ ] Create `CSharpDiffer` class
- [ ] Implement C# syntax tree parsing
- [ ] Implement syntax node traversal

#### Parallel Work Stream B: Syntax Comparison
- [ ] Create `SyntaxComparer` class
- [ ] Implement node matching algorithm
- [ ] Detect added nodes
- [ ] Detect removed nodes
- [ ] Detect modified nodes

#### Parallel Work Stream C: Test Fixtures
- [ ] Create test fixture: empty files
- [ ] Create test fixture: identical files
- [ ] Create test fixture: class added/removed
- [ ] Create test fixture: method added/removed
- [ ] Create test fixture: property changes
- [ ] Create test fixture: nested classes

#### Sequential (after A, B, C):
- [ ] Integrate CSharpDiffer into DifferFactory
- [ ] Update CLI to auto-detect .cs files
- [ ] Unit tests for CSharpDiffer
- [ ] Unit tests for SyntaxComparer
- [ ] Integration tests

### Deliverables
- Working `roslyn-diff file old.cs new.cs` with syntax-aware diff
- Fallback to LineDiffer on parse failure

### Definition of Done
- [ ] C# files show structural changes
- [ ] Non-C# files still work (line diff)
- [ ] Parse failures gracefully fallback
- [ ] All tests pass

---

## Sprint 3: VB.NET + Semantic Analysis

### Goals
- VB.NET support complete
- Semantic analysis (renames, moves) working

### Tasks

#### Parallel Work Stream A: VB.NET Differ
- [ ] Create `VisualBasicDiffer` class
- [ ] Implement VB syntax tree parsing
- [ ] Handle VB-specific syntax (Sub, Function, etc.)
- [ ] VB.NET test fixtures

#### Parallel Work Stream B: Semantic Comparer
- [ ] Create `SemanticComparer` class
- [ ] Build semantic models for both versions
- [ ] Symbol matching by identity
- [ ] Detect renames
- [ ] Detect moves (same code, different location)

#### Parallel Work Stream C: Semantic Test Fixtures
- [ ] Create test fixture: renamed class
- [ ] Create test fixture: renamed method
- [ ] Create test fixture: moved method
- [ ] Create test fixture: signature changes
- [ ] Create test fixture: cross-file scenarios

#### Sequential:
- [ ] Integrate VisualBasicDiffer into DifferFactory
- [ ] Update CLI to auto-detect .vb files
- [ ] Unit tests for VisualBasicDiffer
- [ ] Unit tests for SemanticComparer
- [ ] Integration tests

### Deliverables
- Working VB.NET diff
- Rename/move detection for both languages

### Definition of Done
- [ ] VB.NET files show structural changes
- [ ] Renames detected (not shown as delete+add)
- [ ] Moves detected
- [ ] All tests pass

---

## Sprint 4: Output Formatters

**Note: Can run in parallel with Sprint 3**

### Goals
- All output formats working (JSON, HTML, Terminal)
- Plain text default, Spectre.Console opt-in

### Tasks

#### Parallel Work Stream A: JSON Formatter
- [ ] Create `IOutputFormatter` interface
- [ ] Implement `JsonFormatter`
- [ ] Define JSON schema
- [ ] AI-friendly structure with metadata
- [ ] Include context (file contents optional)
- [ ] Unit tests with Verify.Xunit snapshots

#### Parallel Work Stream B: HTML Formatter
- [ ] Implement `HtmlFormatter`
- [ ] Create HTML template (raw string)
- [ ] Side-by-side diff view
- [ ] Syntax highlighting (inline CSS)
- [ ] Summary statistics section
- [ ] Unit tests

#### Parallel Work Stream C: Terminal Formatter
- [ ] Implement `PlainTextFormatter` (default)
- [ ] Implement `SpectreConsoleFormatter` (--rich)
- [ ] Color-coded output
- [ ] Tree view for structural changes
- [ ] Tables for statistics
- [ ] **REMINDER: Review user's Spectre.Console project**
- [ ] Unit tests

#### Sequential:
- [ ] Create `OutputFormatterFactory`
- [ ] Integrate with CLI (--output flag)
- [ ] Add --out-file support
- [ ] Add --rich flag support
- [ ] Integration tests

### Deliverables
- All output formats working
- `--output json|html|terminal` flag
- `--out-file` flag
- `--rich` flag

### Definition of Done
- [ ] JSON output validates against schema
- [ ] HTML renders correctly in browser
- [ ] Plain text output is clean and readable
- [ ] Rich output uses Spectre.Console features
- [ ] All tests pass

---

## Sprint 5: CLI Polish & Class Diff

### Goals
- Class-to-class comparison working
- Full CLI feature set

### Tasks

#### Parallel Work Stream A: Class Matcher
- [ ] Create `ClassMatcher` class
- [ ] Implement exact name matching
- [ ] Implement interface matching
- [ ] Implement content similarity matching
- [ ] Handle partial classes
- [ ] Handle nested classes
- [ ] Unit tests

#### Parallel Work Stream B: Class Command
- [ ] Implement `class` subcommand
- [ ] Parse `file:ClassName` syntax
- [ ] Support `--match-by` option
- [ ] Support `--interface` option
- [ ] Support `--similarity` option
- [ ] Integration tests

#### Parallel Work Stream C: CLI Options
- [ ] Add `--ignore-whitespace` option
- [ ] Add `--ignore-comments` option
- [ ] Add `--context` option (lines of context)
- [ ] Add `--mode` option (auto/roslyn/line)
- [ ] Comprehensive help text
- [ ] Usage examples in help

#### Sequential:
- [ ] End-to-end testing of all commands
- [ ] Error handling and validation
- [ ] User-friendly error messages
- [ ] Integration tests

### Deliverables
- Working `roslyn-diff class` command
- All CLI options functional
- Polished user experience

### Definition of Done
- [ ] Class diff works with all matching strategies
- [ ] All CLI options work correctly
- [ ] Error messages are helpful
- [ ] Help text is complete
- [ ] All tests pass

---

## Sprint 6: Testing & Documentation

### Goals
- Comprehensive test coverage
- Complete documentation

### Tasks

#### Parallel Work Stream A: Edge Case Tests
- [ ] File with syntax errors (partial parse)
- [ ] Mixed encoding (UTF-8, UTF-16)
- [ ] Very long lines
- [ ] Whitespace-only changes
- [ ] Files with preprocessor directives
- [ ] Files with string interpolation
- [ ] Files with raw string literals
- [ ] Generic classes/methods
- [ ] Records and record structs
- [ ] Primary constructors
- [ ] Expression-bodied members

#### Parallel Work Stream B: Integration Tests
- [ ] End-to-end file diff (C#)
- [ ] End-to-end file diff (VB.NET)
- [ ] End-to-end file diff (non-.NET)
- [ ] End-to-end class diff
- [ ] All output format combinations
- [ ] Error scenarios

#### Parallel Work Stream C: Documentation
- [ ] Complete README.md
- [ ] Usage documentation (docs/usage.md)
- [ ] Output format documentation (docs/output-formats.md)
- [ ] JSON schema documentation
- [ ] Example files in samples/

#### Parallel Work Stream D: Performance Tests
- [ ] Large file diff performance
- [ ] Many changes performance
- [ ] Memory usage tests
- [ ] Establish baseline metrics

### Deliverables
- >90% test coverage
- Complete documentation
- Performance benchmarks

### Definition of Done
- [ ] All edge cases have tests
- [ ] All integration tests pass
- [ ] Documentation is complete
- [ ] Performance is acceptable
- [ ] No known bugs

---

## Sprint 7: Release v1.0

### Goals
- Production-ready release
- NuGet package published

### Tasks

#### Parallel Work Stream A: Package Preparation
- [ ] Configure NuGet package metadata
- [ ] Set version to 1.0.0
- [ ] Create package icon
- [ ] Write package description
- [ ] Configure package dependencies
- [ ] Test package locally

#### Parallel Work Stream B: Final Testing
- [ ] Full regression test
- [ ] Manual testing on different OS (if applicable)
- [ ] Test NuGet package installation
- [ ] Test as global tool

#### Parallel Work Stream C: Release Artifacts
- [ ] Create CHANGELOG.md
- [ ] Tag release in git
- [ ] Create GitHub release (if using GitHub)
- [ ] Publish to NuGet.org

#### Sequential:
- [ ] Final review
- [ ] Publish package
- [ ] Announce release

### Deliverables
- NuGet package: `RoslynDiff` v1.0.0
- GitHub release with release notes

### Definition of Done
- [ ] Package installs correctly
- [ ] All features work as documented
- [ ] No critical bugs
- [ ] Documentation matches functionality

---

## Parallel Execution Summary

```
Sprint 1: [A: Setup]──────┐
          [B: Models]─────┼──► [LineDiffer + CLI]
                          │
Sprint 2: [A: Roslyn]─────┐
          [B: Comparer]───┼──► [Integration]
          [C: Fixtures]───┘
                          │
Sprint 3: [A: VB.NET]─────┐    Sprint 4: [A: JSON]────┐
          [B: Semantic]───┼──►           [B: HTML]────┼──► [Integration]
          [C: Fixtures]───┘              [C: Terminal]┘
                          │                    │
                          └────────────────────┘
                                    │
Sprint 5: [A: Matcher]────┐        ▼
          [B: Class Cmd]──┼──► [Integration]
          [C: CLI Opts]───┘
                          │
Sprint 6: [A: Edge Cases]─┐
          [B: Integration]┼──► [Final Review]
          [C: Docs]───────┤
          [D: Performance]┘
                          │
Sprint 7: [A: Package]────┐
          [B: Testing]────┼──► [Release]
          [C: Artifacts]──┘
```

---

## Risk Mitigation

| Risk | Mitigation |
|------|------------|
| Roslyn API complexity | Start with syntax-only, add semantic later |
| VB.NET edge cases | Defer VB-specific features if needed |
| Performance issues | Profile early, optimize in Sprint 6 |
| Output format changes | Use snapshot testing to catch regressions |
| Scope creep | Strict adherence to sprint goals |

---

## Post-v1.0 Roadmap

| Version | Focus |
|---------|-------|
| v1.1 | MCP Server |
| v1.2 | Bug fixes, user feedback |
| v2.0 | Folder/Project comparison, Git integration |
| v3.0 | Solution-level comparison |
| Future | F#, TypeScript, tree-sitter support |
