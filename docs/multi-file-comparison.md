# Multi-File Comparison

**Version:** 0.10.0
**Status:** Implemented (Phase 2 Complete - HTML output pending)

---

## Table of Contents

- [Overview](#overview)
- [Quick Start](#quick-start)
- [Git Comparison](#git-comparison)
- [Folder Comparison](#folder-comparison)
- [File Filtering](#file-filtering)
- [Output Formats](#output-formats)
- [Performance Tips](#performance-tips)
- [Integration Patterns](#integration-patterns)
- [Limitations](#limitations)

---

## Overview

Multi-file comparison enables you to analyze changes across multiple files in a single operation. This is essential for:

- **Pull Request Reviews** - Analyze entire changesets before merging
- **Release Comparisons** - Compare releases or tagged versions
- **Migration Analysis** - Assess impact of large-scale refactorings
- **Codebase Audits** - Review changes across an entire project

### What is Multi-File Comparison?

Instead of comparing two individual files, multi-file comparison analyzes:
- **Git branches** - Compare the state of all files between two refs
- **Directory trees** - Compare all files in two folder structures
- **Filtered subsets** - Use glob patterns to include/exclude specific files

### Current Implementation Status

**Implemented (v0.10.0)**:
- Git branch comparison (`--git-compare`)
- Folder-to-folder comparison (auto-detect directories)
- File filtering with glob patterns (`--include`, `--exclude`)
- Recursive directory traversal (`--recursive`)
- JSON output (schema v3)
- Parallel processing for performance

**Planned (future release)**:
- HTML document mode (single HTML with file navigation)
- HTML fragment mode (directory of fragments)
- Text/Plain output formats
- Cross-file rename detection
- Progress reporting for large repositories

---

## Quick Start

### Compare Two Directories

```bash
# Basic folder comparison
roslyn-diff diff old/ new/ --json output.json

# With file filtering
roslyn-diff diff old/ new/ --include "*.cs" --json output.json

# Recursive comparison
roslyn-diff diff old/ new/ --recursive --json output.json
```

### Compare Git Branches

```bash
# Compare branches
roslyn-diff diff --git-compare main..feature --json changes.json

# Compare with specific files
roslyn-diff diff --git-compare main..feature --include "src/**/*.cs" --json changes.json

# Compare commits
roslyn-diff diff --git-compare abc123..def456 --json changes.json
```

### View Summary

```bash
# Console output shows summary even without explicit output file
roslyn-diff diff old/ new/
# Output:
# Folder comparison complete
# Files changed: 4
#   Modified: 2
#   Added: 1
#   Removed: 1
# Total changes: 10
```

---

## Git Comparison

Git comparison analyzes changes between two git references (branches, tags, or commits).

### Syntax

```bash
roslyn-diff diff --git-compare <ref-range> [options]
```

**Ref Range Format**: `<old-ref>..<new-ref>`

Examples:
- `main..feature` - Compare main branch to feature branch
- `v1.0.0..v2.0.0` - Compare two tags
- `abc123..def456` - Compare specific commits
- `HEAD~5..HEAD` - Compare current commit to 5 commits ago

### How It Works

1. **Reference Resolution** - Resolves both refs to commit SHAs
2. **File Collection** - Gets list of changed files between commits
3. **Content Extraction** - Retrieves file content at each ref
4. **Semantic Analysis** - Runs roslyn-diff on each file pair
5. **Aggregation** - Combines results into unified report

### Examples

**Compare Feature Branch to Main**:
```bash
roslyn-diff diff --git-compare main..feature-branch --json pr-review.json
```

**Filter to Source Code Only**:
```bash
roslyn-diff diff --git-compare main..feature \
  --include "src/**/*.cs" \
  --exclude "**/*.g.cs" \
  --exclude "**/*.Designer.cs" \
  --json changes.json
```

**Multiple Target Frameworks**:
```bash
roslyn-diff diff --git-compare main..feature \
  -t net8.0 -t net10.0 \
  --json changes.json
```

**Impact Filtering**:
```bash
# Only breaking changes
roslyn-diff diff --git-compare main..feature \
  --impact-level breaking-public \
  --json breaking-changes.json
```

### Git Requirements

- Must be run from within a git repository
- Both refs must exist in the repository
- Requires read access to git objects
- Works with local branches, remote branches, tags, and commits

### Performance

Git comparison is optimized for large repositories:
- Only changed files are analyzed (not the entire tree)
- Files are processed in parallel
- Early termination for unchanged files
- Efficient git object access via LibGit2Sharp

**Benchmarks**:
- 100 files: ~5 seconds
- 500 files: ~20 seconds
- 1000+ files: Shows progress and allows override

---

## Folder Comparison

Folder comparison analyzes differences between two directory trees.

### Syntax

```bash
roslyn-diff diff <old-folder> <new-folder> [options]
```

**Auto-Detection**: roslyn-diff automatically detects when both arguments are directories and switches to folder comparison mode.

### How It Works

1. **File Discovery** - Scans both directories for files
2. **Path Matching** - Matches files by relative path
3. **Change Detection** - Identifies added, removed, and modified files
4. **Semantic Analysis** - Runs roslyn-diff on each file pair
5. **Aggregation** - Combines results into unified report

### File Matching Strategy

Files are matched by **relative path**:
- `old/Calculator.cs` matches `new/Calculator.cs`
- `old/Utils/Helper.cs` matches `new/Utils/Helper.cs`
- Files with no match are marked as added or removed

### Examples

**Basic Folder Comparison**:
```bash
roslyn-diff diff before/ after/ --json changes.json
```

**Recursive Comparison**:
```bash
# Include all subdirectories
roslyn-diff diff before/ after/ --recursive --json changes.json
```

**Non-Recursive (Top-Level Only)**:
```bash
# Default behavior: only files in the specified directories
roslyn-diff diff before/ after/ --json changes.json
```

**With Filtering**:
```bash
# C# files only, exclude generated
roslyn-diff diff before/ after/ \
  --recursive \
  --include "*.cs" \
  --exclude "*.g.cs" \
  --exclude "*.Designer.cs" \
  --json changes.json
```

### Recursive vs Non-Recursive

**Non-Recursive (Default)**:
- Only compares files in the specified directories
- Does not traverse subdirectories
- Faster for large hierarchies
- Use when you only care about top-level changes

**Recursive (`--recursive` or `-r`)**:
- Traverses all subdirectories
- Compares entire directory trees
- Matches files at any depth
- Use for comprehensive analysis

Example directory structure:
```
before/
  Calculator.cs
  Utils/
    Helper.cs
    Logger.cs
```

Non-recursive: Compares only `Calculator.cs`
Recursive: Compares all three files

### Performance

Folder comparison is optimized with:
- Parallel file processing
- Early termination for identical files
- Efficient file system access
- Memory-conscious streaming

---

## File Filtering

Glob patterns allow precise control over which files to compare.

### Glob Pattern Syntax

roslyn-diff uses a simplified glob syntax:

| Pattern | Matches | Example |
|---------|---------|---------|
| `*` | Any characters (except `/`) | `*.cs` matches `File.cs` |
| `**` | Any directories | `src/**/*.cs` matches `src/foo/bar.cs` |
| `?` | Single character | `Test?.cs` matches `Test1.cs` |
| Literal | Exact match | `Program.cs` |

### Pattern Matching Rules

1. **Case Insensitive** - Patterns match regardless of case
2. **OR Logic for Includes** - File matches if ANY include pattern matches
3. **Exclude Takes Precedence** - Excluded files are never included
4. **Relative Paths** - Patterns match against relative paths from root

### CLI Options

**Include Patterns** (`--include`):
```bash
# Single pattern
roslyn-diff diff old/ new/ --include "*.cs"

# Multiple patterns (OR logic)
roslyn-diff diff old/ new/ --include "*.cs" --include "*.vb"
```

**Exclude Patterns** (`--exclude`):
```bash
# Exclude generated files
roslyn-diff diff old/ new/ --exclude "*.g.cs"

# Multiple exclusions
roslyn-diff diff old/ new/ \
  --exclude "*.g.cs" \
  --exclude "*.Designer.cs" \
  --exclude "bin/**" \
  --exclude "obj/**"
```

**Combined Include and Exclude**:
```bash
# All C# files except generated and in bin/obj
roslyn-diff diff old/ new/ \
  --recursive \
  --include "*.cs" \
  --exclude "*.g.cs" \
  --exclude "*.Designer.cs" \
  --exclude "bin/**" \
  --exclude "obj/**"
```

### Common Patterns

**Source Code Only**:
```bash
--include "src/**/*.cs" --include "src/**/*.vb"
```

**Exclude Build Artifacts**:
```bash
--exclude "bin/**" --exclude "obj/**" --exclude "packages/**"
```

**Exclude Generated Code**:
```bash
--exclude "**/*.g.cs" \
--exclude "**/*.Designer.cs" \
--exclude "**/*.generated.cs"
```

**Exclude Tests**:
```bash
--exclude "**/*Test.cs" \
--exclude "**/*Tests.cs" \
--exclude "tests/**"
```

**Include Specific Directories**:
```bash
--include "src/Core/**/*.cs" \
--include "src/Services/**/*.cs"
```

### Filter Precedence

Filters are applied in this order:

1. **Recursive Option** - Determines if subdirectories are traversed
2. **Include Patterns** - Only files matching at least one include pattern are considered
3. **Exclude Patterns** - Matching files are excluded, even if included
4. **File Exists** - Only existing files are compared

**If no include patterns**: All files are candidates (unless excluded)
**If include patterns exist**: Only matching files are candidates

### Examples

**Scenario 1: Include All C# Files**
```bash
--include "*.cs"
# Matches: Calculator.cs, Program.cs
# Skips: Readme.txt, image.png
```

**Scenario 2: Exclude Generated Files**
```bash
--include "*.cs" --exclude "*.g.cs"
# Matches: Calculator.cs
# Skips: Generated.g.cs
```

**Scenario 3: Specific Directories**
```bash
--include "src/**/*.cs" --exclude "src/Tests/**"
# Matches: src/Core/Calculator.cs, src/Services/Logger.cs
# Skips: src/Tests/CalculatorTests.cs
```

---

## Output Formats

### JSON Schema v3

Multi-file comparison introduces JSON schema v3 with enhanced structure for multiple files.

**Key Changes from v2**:
- New `comparisonMode` field: `"folder"` or `"git"`
- `files` array with per-file results
- File status: `modified`, `added`, `removed`, `renamed`
- Aggregated summary with file counts
- Each file has embedded `DiffResult`

### Schema Structure

```json
{
  "$schema": "roslyn-diff-output-v3",
  "metadata": {
    "version": "0.10.0",
    "timestamp": "2026-01-28T10:00:00Z",
    "mode": "multi-file",
    "comparisonMode": "git",
    "gitRefRange": "main..feature",
    "oldRoot": null,
    "newRoot": null,
    "options": {
      "includeContent": true,
      "contextLines": 3,
      "includeNonImpactful": false
    }
  },
  "summary": {
    "totalFiles": 15,
    "modifiedFiles": 8,
    "addedFiles": 5,
    "removedFiles": 2,
    "renamedFiles": 0,
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
        "summary": {
          "totalChanges": 3,
          "additions": 1,
          "deletions": 0,
          "modifications": 2
        },
        "changes": [
          /* Standard DiffResult.changes array */
        ]
      }
    }
  ]
}
```

### Metadata Fields

**Git Comparison**:
- `comparisonMode`: `"git"`
- `gitRefRange`: Ref range (e.g., `"main..feature"`)
- `oldRoot`: `null`
- `newRoot`: `null`

**Folder Comparison**:
- `comparisonMode`: `"folder"`
- `gitRefRange`: `null`
- `oldRoot`: Path to old directory
- `newRoot`: Path to new directory

### File Status Values

| Status | Description |
|--------|-------------|
| `modified` | File exists in both, content changed |
| `added` | File only exists in new version |
| `removed` | File only exists in old version |
| `renamed` | File moved (future feature) |
| `unchanged` | File exists but no semantic changes (skipped by default) |

### Summary Aggregation

The `summary` object aggregates across all files:

**File Counts**:
- `totalFiles` - Total files analyzed
- `modifiedFiles` - Files with changes
- `addedFiles` - Newly added files
- `removedFiles` - Deleted files
- `renamedFiles` - Moved files (future)

**Change Counts**:
- `totalChanges` - Sum of all changes across files
- `impactBreakdown` - Aggregated impact classification

### Per-File Results

Each file entry contains:
- `oldPath` - Path in old version (null for added files)
- `newPath` - Path in new version (null for removed files)
- `status` - File change status
- `result` - Full `DiffResult` object for the file

The `result` object is the standard single-file diff result with:
- `summary` - File-specific change summary
- `changes` - Hierarchical change tree

### Example Use Cases

**Extract Breaking Changes Only**:
```javascript
const breaking = data.files.flatMap(f =>
  f.result.changes.filter(c =>
    c.impact === 'breakingPublicApi'
  )
);
```

**Find Added Files**:
```javascript
const newFiles = data.files.filter(f => f.status === 'added');
```

**Calculate Total Impact**:
```javascript
const totalBreaking =
  data.summary.impactBreakdown.breakingPublicApi +
  data.summary.impactBreakdown.breakingInternalApi;
```

### HTML Output (Planned)

HTML output for multi-file is planned for a future release.

**Document Mode** (Planned):
- Single HTML file with all diffs
- File navigation sidebar
- Collapsible file sections
- Overall summary panel
- Per-file diff display

**Fragment Mode** (Planned):
- Directory of HTML fragments
- One fragment per file
- Shared CSS file
- Data attributes for metadata
- Embeddable in dashboards

**Example** (Planned):
```bash
# Document mode (single file)
roslyn-diff diff --git-compare main..feature --html report.html

# Fragment mode (directory)
roslyn-diff diff --git-compare main..feature \
  --html-mode fragment \
  --html fragments/
```

---

## Performance Tips

### Parallel Processing

Multi-file comparison uses parallel processing by default for optimal performance.

**Automatic Parallelization**:
- Files are analyzed concurrently
- Number of threads based on CPU cores
- Sequential fallback for small file counts

**Best Practices**:
- Larger file counts benefit more from parallelism
- SSD storage improves parallel I/O
- Don't run multiple instances concurrently

### Filtering for Performance

Use filtering to reduce work:

**Exclude Build Artifacts**:
```bash
--exclude "bin/**" --exclude "obj/**"
```

**Include Only Relevant Files**:
```bash
--include "src/**/*.cs"
```

**Non-Recursive When Possible**:
```bash
# Faster: top-level only
roslyn-diff diff old/ new/

# Slower: full tree
roslyn-diff diff old/ new/ --recursive
```

### Large Repository Handling

For repositories with 1000+ files:

1. **Use Filtering** - Narrow scope to relevant files
2. **Split by Directory** - Compare subsections separately
3. **Impact Filtering** - Focus on breaking changes first
4. **Incremental Analysis** - Compare recent commits only

**Example for Large Repo**:
```bash
# Compare only src/ directory with public API changes
roslyn-diff diff --git-compare main..feature \
  --include "src/**/*.cs" \
  --exclude "src/Tests/**" \
  --impact-level breaking-public \
  --json high-priority-changes.json
```

### Memory Considerations

Multi-file comparison is memory-efficient:
- Files are streamed, not fully loaded
- Results are written incrementally to JSON
- Garbage collection is optimized

**Typical Memory Usage**:
- 100 files: ~200 MB
- 500 files: ~500 MB
- 1000 files: ~1 GB

---

## Integration Patterns

### CI/CD Pipeline Integration

**GitHub Actions Example**:
```yaml
- name: Analyze PR Changes
  run: |
    dotnet tool install -g roslyn-diff
    roslyn-diff diff --git-compare main..HEAD \
      --include "src/**/*.cs" \
      --impact-level breaking-public \
      --json pr-analysis.json

- name: Upload Results
  uses: actions/upload-artifact@v3
  with:
    name: diff-results
    path: pr-analysis.json
```

**GitLab CI Example**:
```yaml
analyze-changes:
  script:
    - dotnet tool install -g roslyn-diff
    - roslyn-diff diff --git-compare main..$CI_COMMIT_SHA \
        --json changes.json
  artifacts:
    paths:
      - changes.json
```

### Pre-Commit Hooks

**Git Hook Example**:
```bash
#!/bin/bash
# .git/hooks/pre-push

roslyn-diff diff --git-compare origin/main..HEAD \
  --impact-level breaking-public \
  --json /tmp/changes.json

if [ $? -ne 0 ]; then
  echo "Breaking public API changes detected!"
  echo "Review /tmp/changes.json before pushing."
  exit 1
fi
```

### Code Review Automation

**Generate PR Summary**:
```bash
#!/bin/bash
PR_BRANCH=$1

roslyn-diff diff --git-compare main..$PR_BRANCH \
  --include "src/**/*.cs" \
  --json pr-diff.json

# Extract summary
jq '.summary' pr-diff.json > pr-summary.json

# Post to PR as comment
gh pr comment --body-file pr-summary.json
```

### Release Notes Generation

**Extract Breaking Changes**:
```bash
#!/bin/bash
OLD_VERSION=$1
NEW_VERSION=$2

roslyn-diff diff --git-compare $OLD_VERSION..$NEW_VERSION \
  --impact-level breaking-public \
  --json breaking-changes.json

# Format for release notes
jq -r '.files[].result.changes[] |
  select(.impact == "breakingPublicApi") |
  "- \(.kind): \(.name)"' \
  breaking-changes.json > BREAKING_CHANGES.md
```

### Dashboard Integration

**JSON to HTML Summary**:
```javascript
// Parse multi-file JSON
const data = JSON.parse(fs.readFileSync('changes.json'));

// Generate summary card
const summary = `
<div class="diff-summary">
  <h3>${data.summary.totalFiles} files changed</h3>
  <ul>
    <li>Modified: ${data.summary.modifiedFiles}</li>
    <li>Added: ${data.summary.addedFiles}</li>
    <li>Removed: ${data.summary.removedFiles}</li>
  </ul>
  <div class="impact">
    <span class="breaking">${data.summary.impactBreakdown.breakingPublicApi} breaking</span>
    <span class="total">${data.summary.totalChanges} total changes</span>
  </div>
</div>
`;
```

### Combining with Fragment Mode

When HTML fragment mode is implemented:

```bash
# Generate fragments for dashboard
roslyn-diff diff --git-compare main..feature \
  --html-mode fragment \
  --html dashboard/diffs/

# Fragments are then embedded in your web UI
```

### Combining with Inline View

When inline view is supported for multi-file:

```bash
# Multi-file with inline view
roslyn-diff diff --git-compare main..feature \
  --inline=5 \
  --html pr-review.html
```

---

## Limitations

### Current Limitations (v0.10.0)

**HTML Output Not Yet Implemented**:
- Multi-file HTML document mode is planned but not yet available
- Multi-file HTML fragment mode is planned but not yet available
- Only JSON output is currently supported
- Workaround: Generate single-file HTML for each changed file separately

**No Cross-File Analysis**:
- Rename detection is per-file only
- Moving a class between files is detected as delete + add
- No cross-file impact analysis
- No namespace reorganization detection

**No Binary File Support**:
- Binary files are reported as changed but not diffed
- No content comparison for images, DLLs, etc.
- Workaround: Use `--exclude` to skip binary files

**No Directory Structure Comparison**:
- Folder creation/deletion is not reported
- Only file changes are analyzed
- Empty directories are ignored

**Performance Limits**:
- 1000+ files: May require filtering
- Very large files (>10,000 lines): May be slow
- Parallel processing bound by CPU cores

### Known Issues

**Git Submodules**:
- Submodule changes are not analyzed
- Only files in the main repository are compared
- Workaround: Run roslyn-diff in submodule directories separately

**Symbolic Links**:
- Symlinks are followed but may cause issues
- Circular symlinks may hang the process
- Workaround: Use `--exclude` for symlink directories

**Case-Sensitive File Systems**:
- File matching is case-insensitive
- May cause issues on case-sensitive file systems
- Files differing only in case are treated as the same

### Workarounds

**Need HTML Output?**
Generate per-file HTML manually:
```bash
#!/bin/bash
for file in $(git diff --name-only main..feature); do
  roslyn-diff diff \
    <(git show main:$file) \
    <(git show feature:$file) \
    --html output/$(basename $file).html
done
```

**Need Cross-File Rename Detection?**
Post-process JSON to correlate deletes with adds:
```javascript
const deleted = data.files.filter(f => f.status === 'removed');
const added = data.files.filter(f => f.status === 'added');

// Compare content similarity to detect renames
```

**Need Directory Structure Comparison?**
Use external tools:
```bash
diff -qr old/ new/ > structure-diff.txt
```

---

## Related Documentation

- [Output Formats](./output-formats.md) - JSON schema v3 details
- [Usage Guide](./usage.md) - General CLI usage
- [Performance](./performance.md) - Performance optimization
- [Impact Classification](./impact-classification.md) - Understanding impact levels
- [Glob Patterns](./GLOB_PATTERNS.md) - Detailed glob pattern syntax

---

## Feedback and Contributions

Multi-file comparison is actively being developed. We welcome feedback on:
- Use cases and workflows
- Performance characteristics
- Integration patterns
- Feature requests

Please file issues at: [GitHub Issues](https://github.com/randlee/roslyn-diff/issues)

---

**Document Version**: 1.0
**Last Updated**: 2026-01-28
**Feature Version**: 0.10.0 (Phase 2 Complete)
