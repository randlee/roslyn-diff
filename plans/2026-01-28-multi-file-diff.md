# Design: Multi-File and Repository Diff Support

**Document ID:** DESIGN-007
**Date:** 2026-01-28
**Status:** PLANNING
**Related Issue:** #46 (split from HTML Fragment Mode request)
**Target Version:** v0.10.0

---

## 1. Overview

Add support for comparing multiple files, directories, and git branches in a single operation. Enables developers to analyze entire changesets at once for PR reviews, release comparisons, and migration analysis.

### Goals

1. **Git branch comparison** - Compare HEAD states of two branches (Phase 1 priority)
2. **Folder-to-folder comparison** - Compare all files in two directories (Phase 2)
3. **File filtering** - Include/exclude patterns for file selection
4. **Aggregated output** - Unified reports with per-file sections
5. **Fragment integration** - Works with fragment mode for embeddable multi-file reports

### Non-Goals (for initial release)

- Git commit range comparison (e.g., `main..HEAD~5`)
- Cross-file rename detection
- Binary file diffing
- Directory structure comparison

---

## 2. CLI Interface

### New Options

```bash
--git <ref-range>          # Git comparison (e.g., main..feature, abc123..def456)
--include <pattern>        # Include files matching pattern (repeatable)
--exclude <pattern>        # Exclude files matching pattern (repeatable)
--recursive                # Recurse subdirectories (default: false)
```

### Intelligent --html Reuse

```bash
# Document mode: Single HTML file (even for multi-file diffs)
roslyn-diff diff --git main..feature --html report.html

# Fragment mode: Directory of fragment files
roslyn-diff diff --git main..feature --html-mode fragment --html fragments/
```

**Detection logic:**
- Document mode → always single file output
- Fragment mode → directory output (one fragment per file)
- Path with `.html` extension → file
- Path without extension or ending in `/` → directory

### Usage Examples

```bash
# Git branch comparison
roslyn-diff diff --git main..feature-branch

# Git with HTML report
roslyn-diff diff --git main..feature --html report.html

# Git with fragments
roslyn-diff diff --git main..feature --html-mode fragment --html fragments/

# Folder comparison (auto-detect)
roslyn-diff diff old/ new/

# Folder with filtering
roslyn-diff diff old/ new/ --include "*.cs" --exclude "*.g.cs"

# Folder recursive
roslyn-diff diff old/ new/ --recursive

# Combined: Git + fragments + filtering
roslyn-diff diff --git main..feature \
  --include "*.cs" \
  --exclude "*.g.cs" \
  --html-mode fragment \
  --html fragments/
```

---

## 3. Data Model

### MultiFileDiffResult

```csharp
public record MultiFileDiffResult
{
    public IReadOnlyList<FileDiffResult> Files { get; init; }
    public MultiFileSummary Summary { get; init; }
    public MultiFileMetadata Metadata { get; init; }
}

public record FileDiffResult
{
    public DiffResult Result { get; init; }
    public FileChangeStatus Status { get; init; }
    public string? OldPath { get; init; }
    public string? NewPath { get; init; }
}

public enum FileChangeStatus
{
    Modified,
    Added,
    Removed,
    Renamed,
    Unchanged
}

public record MultiFileSummary
{
    public int TotalFiles { get; init; }
    public int ModifiedFiles { get; init; }
    public int AddedFiles { get; init; }
    public int RemovedFiles { get; init; }
    public int RenamedFiles { get; init; }

    public int TotalChanges { get; init; }
    public ImpactBreakdown ImpactBreakdown { get; init; }
}

public record MultiFileMetadata
{
    public string Mode { get; init; } // "git" or "folder"
    public string? GitRefRange { get; init; }
    public string? OldRoot { get; init; }
    public string? NewRoot { get; init; }
    public DateTime Timestamp { get; init; }
}
```

---

## 4. Implementation Plan

### Phase 1: Git Branch Comparison (Priority)
**Estimated effort**: 1-2 weeks

**Tasks:**
- [ ] Add `MultiFileDiffResult` models
- [ ] Add `GitComparer` using LibGit2Sharp
- [ ] Add `--git` CLI option with ref range parsing
- [ ] Implement parallel file comparison
- [ ] Add LibGit2Sharp dependency
- [ ] Update JSON output for multi-file (schema v3)
- [ ] Update HTML output with file navigation
- [ ] Add unit tests for git comparison

**Key files:**
- `src/RoslynDiff.Core/Models/MultiFileDiffResult.cs` (new)
- `src/RoslynDiff.Core/MultiFile/GitComparer.cs` (new)
- `src/RoslynDiff.Cli/Commands/DiffCommand.cs` (modify)
- `src/RoslynDiff.Output/Formatters/` (modify all formatters)

**Deliverable**: Working `--git main..feature` with HTML/JSON output

### Phase 2: Folder Comparison
**Estimated effort**: 3-5 days

**Tasks:**
- [ ] Add `FolderComparer` for directory comparison
- [ ] Implement file matching by path
- [ ] Add `--include` and `--exclude` filtering
- [ ] Add `--recursive` flag
- [ ] Auto-detect folder arguments (no flag needed)
- [ ] Add unit tests for folder comparison

**Key files:**
- `src/RoslynDiff.Core/MultiFile/FolderComparer.cs` (new)
- `src/RoslynDiff.Core/MultiFile/FileMatchingStrategy.cs` (new)

**Deliverable**: Working folder-to-folder comparison with filtering

### Phase 3: Polish & Documentation
**Estimated effort**: 3-4 days

**Tasks:**
- [ ] Performance optimization (parallel processing)
- [ ] Progress reporting for large changesets
- [ ] Update README.md with multi-file examples
- [ ] Create `docs/multi-file-comparison.md`
- [ ] Add samples for multi-file scenarios
- [ ] Update CHANGELOG.md
- [ ] Integration tests for multi-file scenarios

**Deliverable**: Complete, documented feature ready for release

---

## 5. Output Formats

### JSON Schema v3

```json
{
  "$schema": "roslyn-diff-output-v3",
  "metadata": {
    "version": "0.10.0",
    "mode": "multi-file",
    "comparisonMode": "git",
    "gitRefRange": "main..feature-branch"
  },
  "summary": {
    "totalFiles": 15,
    "modifiedFiles": 8,
    "addedFiles": 5,
    "removedFiles": 2,
    "totalChanges": 47,
    "impactBreakdown": {
      "breakingPublicApi": 5,
      "breakingInternalApi": 8,
      "nonBreaking": 32,
      "formattingOnly": 2
    }
  },
  "files": [
    {
      "oldPath": "src/Calculator.cs",
      "newPath": "src/Calculator.cs",
      "status": "modified",
      "result": {
        "summary": { /* single-file DiffResult */ },
        "changes": [ /* ... */ ]
      }
    }
  ]
}
```

### HTML Document Mode

Single HTML file with all diffs:

```html
<!DOCTYPE html>
<html>
<head>
  <title>Multi-File Diff: main..feature-branch</title>
  <style>/* styles */</style>
</head>
<body>
  <div class="multi-file-diff">
    <!-- Overall summary -->
    <div class="summary-panel">
      <h1>15 files changed</h1>
      <div class="stats">47 changes, 5 breaking public</div>
    </div>

    <!-- File list navigation -->
    <nav class="file-list">
      <ul>
        <li><a href="#file-0">src/Calculator.cs</a> (8 changes)</li>
        <li><a href="#file-1">src/Service.cs</a> (12 changes)</li>
      </ul>
    </nav>

    <!-- Individual file diffs -->
    <section id="file-0">
      <h2>src/Calculator.cs</h2>
      <!-- Single-file diff HTML -->
    </section>
  </div>
</body>
</html>
```

### HTML Fragment Mode

Directory of fragments:

```bash
fragments/
  ├── src-Calculator.cs.html
  ├── src-Service.cs.html
  ├── src-Utils.cs.html
  └── roslyn-diff.css
```

Each fragment is embeddable independently.

### Text Output

```
Multi-File Diff: main..feature-branch
Files: 15 (8 modified, 5 added, 2 removed)
Changes: 47 (+23, -12, ~12)
Impact: 5 breaking public, 8 breaking internal

Files Changed:
  [M] src/Calculator.cs (8 changes)
  [M] src/Service.cs (12 changes)
  [A] src/NewService.cs (added)
  [R] src/OldHelper.cs (removed)

──────────────────────────────────────────
File: src/Calculator.cs
──────────────────────────────────────────
  [+] Method: Multiply (line 15-18)
  [~] Method: Divide (line 20-25)
```

---

## 6. Dependencies

### LibGit2Sharp

**Package**: `LibGit2Sharp` v0.30.0

**Why**: Robust git integration without shelling out to CLI

**Alternatives considered**:
- Git CLI (less reliable, harder to parse)
- Manual git object reading (too complex)

**Decision**: Use LibGit2Sharp for git operations

---

## 7. Integration with Other Features

### Fragment Mode Integration

```bash
# Generate one fragment per file
roslyn-diff diff --git main..feature \
  --html-mode fragment \
  --html fragments/

# Creates:
# fragments/src-Calculator.cs.html
# fragments/src-Service.cs.html
# fragments/roslyn-diff.css (shared)
```

### Inline View Integration (future)

```bash
# Multi-file with inline view
roslyn-diff diff --git main..feature \
  --inline \
  --html report.html
```

Each file shown with inline code diff instead of tree view.

---

## 8. Testing Strategy

### Unit Tests

- Git comparison with test repository
- Folder comparison with test directories
- File matching strategies
- Include/exclude filtering
- Output format generation

### Integration Tests

- Real git repository comparison
- Large folder hierarchies
- Fragment mode with multiple files
- Performance benchmarks (100+ files)

---

## 9. Performance Considerations

**Parallel Processing**: Use `Parallel.ForEach` for file comparisons

**Memory Management**: Stream file content, don't load all at once

**Progress Reporting**: Show progress for large changesets (>50 files)

**Limits**: Warn for >1000 files, allow override with flag

---

## 10. Documentation Updates

### README.md

Add multi-file section:
- Git comparison examples
- Folder comparison examples
- Fragment mode integration
- Use cases

### New Documentation

Create `docs/multi-file-comparison.md`:
- Complete CLI reference
- Output format details
- Integration patterns
- Performance tips

---

## 11. Breaking Changes

### JSON Schema v3

Changes from v2:
- `files` array structure changed (added `oldPath`, `newPath`, `status`)
- Added `summary.totalFiles` and file counts
- Wrapped single-file result in `result` property

**Mitigation**: Document migration in CHANGELOG, consider bumping to v1.0.0

---

## 12. Open Questions

### Q1: Should we support commit range comparison?

**Example**: `main..HEAD~5` (multiple commits)

**Decision**: No for v1. HEAD-to-HEAD only. Future enhancement if needed.

### Q2: How to handle very large repositories?

**Options**:
- Default limit of 1000 files
- Show warning and prompt
- Add `--no-limit` flag

**Decision**: Warn at 1000 files, allow override with `--no-limit`

### Q3: Binary file handling?

**Decision**: Report "binary file changed" but don't diff content

---

## 13. Success Metrics

**Acceptance Criteria:**
- [ ] Git branch comparison works with ref range syntax
- [ ] Folder comparison with glob filtering works
- [ ] Multi-file JSON output follows schema v3
- [ ] Multi-file HTML output has file navigation
- [ ] Fragment mode generates one fragment per file
- [ ] Performance: 100 files in <5 seconds
- [ ] All tests passing

---

## 14. Related Features

- **Fragment Mode**: Generates embeddable multi-file reports
- **Inline View** (future): Shows code inline for multi-file diffs
- **Impact Classification**: Works across all files in changeset

---

**Status**: Ready for implementation
**Next Steps**:
1. Create worktree
2. Implement Phase 1 (git comparison)
3. Test and iterate
