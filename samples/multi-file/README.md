# Multi-File Comparison Samples

This directory contains sample files demonstrating multi-file comparison capabilities in roslyn-diff v0.10.0.

---

## Overview

Multi-file comparison enables analyzing changes across multiple files in a single operation. This is essential for:
- Pull request reviews
- Release comparisons
- Migration analysis
- Codebase audits

**Current Status (v0.10.0)**:
- ✅ Folder comparison (directory to directory)
- ✅ Git comparison (branch to branch, commit to commit)
- ✅ File filtering with glob patterns
- ✅ JSON output (schema v3)
- ⏳ HTML output (planned for future release)

---

## Table of Contents

- [Sample Files](#sample-files)
- [Folder Comparison Examples](#folder-comparison-examples)
- [Git Comparison Examples](#git-comparison-examples)
- [File Filtering Examples](#file-filtering-examples)
- [Integration Patterns](#integration-patterns)
- [Understanding the Output](#understanding-the-output)

---

## Sample Files

### Test Folders

**Location**: `test-folders/before/` and `test-folders/after/`

**Purpose**: Demonstrate folder-to-folder comparison with various change types.

**Files**:

**Before directory**:
- `Calculator.cs` - Basic calculator with Add and Subtract methods
- `StringHelper.cs` - String utility methods (ToUpperCase, ToLowerCase)
- `Validator.cs` - Validation methods (IsPositive, IsNegative)

**After directory**:
- `Calculator.cs` - Enhanced calculator with documentation and Multiply method (MODIFIED)
- `StringHelper.cs` - Enhanced with null checks and Reverse method (MODIFIED)
- `Logger.cs` - New logging utility class (ADDED)
- `Validator.cs` - Removed (REMOVED)

**Change Summary**:
- 2 modified files
- 1 added file
- 1 removed file
- 10 total changes

### Generated Outputs

**`folder-comparison.json`**:
- Full JSON output of folder comparison
- Demonstrates schema v3 structure
- Shows modified, added, and removed files
- Includes per-file change details

**`folder-comparison-filtered.json`**:
- Same comparison with `--include "*.cs"` filter
- Demonstrates glob pattern filtering
- Identical to unfiltered in this case (all files are .cs)

**`git-comparison.json`**:
- Real git comparison between commits in roslyn-diff repository
- Demonstrates large-scale multi-file analysis
- Shows 19 changed files with 13,714 changes
- Includes filter for `src/**/*.cs` files only

---

## Folder Comparison Examples

### Basic Folder Comparison

Compare two directories and output JSON:

```bash
roslyn-diff diff test-folders/before/ test-folders/after/ \
  --json folder-comparison.json
```

**Output**:
```
JSON output written to: folder-comparison.json
Folder comparison complete
Files changed: 4
  Modified: 2
  Added: 1
  Removed: 1
Total changes: 10
```

### Recursive Folder Comparison

Compare entire directory trees including subdirectories:

```bash
roslyn-diff diff old-project/ new-project/ \
  --recursive \
  --json full-comparison.json
```

**When to use recursive**:
- Comparing entire project structures
- Analyzing nested directory hierarchies
- Full codebase audits

**When NOT to use recursive**:
- Only need top-level changes
- Large directory trees (use filtering)
- Performance is critical

### Non-Recursive (Default)

Compare only files in the specified directories (not subdirectories):

```bash
roslyn-diff diff src/ src-modified/ \
  --json top-level-changes.json
```

**Use case**: Fast comparison of files in a single directory without traversing subdirectories.

---

## Git Comparison Examples

### Compare Branches

Compare all changes between two branches:

```bash
roslyn-diff diff --git-compare main..feature-branch \
  --json pr-review.json
```

**Use cases**:
- Pull request analysis
- Pre-merge review
- Release preparation

### Compare Specific Commits

Compare two specific commits:

```bash
roslyn-diff diff --git-compare abc123..def456 \
  --json commit-diff.json
```

**Use cases**:
- Analyzing specific changesets
- Investigating regressions
- Release comparison

### Compare Tags

Compare two release tags:

```bash
roslyn-diff diff --git-compare v1.0.0..v2.0.0 \
  --json release-changes.json
```

**Use cases**:
- Release notes generation
- Breaking change analysis
- Migration planning

### Compare to Previous Commit

Compare current HEAD to 5 commits ago:

```bash
roslyn-diff diff --git-compare HEAD~5..HEAD \
  --json recent-changes.json
```

**Use cases**:
- Recent history review
- Development progress tracking
- Quick change verification

---

## File Filtering Examples

### Include Specific File Types

Compare only C# files:

```bash
roslyn-diff diff old/ new/ \
  --include "*.cs" \
  --json csharp-only.json
```

### Exclude Generated Files

Exclude auto-generated code:

```bash
roslyn-diff diff old/ new/ \
  --recursive \
  --include "*.cs" \
  --exclude "*.g.cs" \
  --exclude "*.Designer.cs" \
  --json no-generated.json
```

### Exclude Build Artifacts

Skip bin and obj directories:

```bash
roslyn-diff diff old/ new/ \
  --recursive \
  --exclude "bin/**" \
  --exclude "obj/**" \
  --exclude "packages/**" \
  --json source-only.json
```

### Include Specific Directories

Compare only specific directories:

```bash
roslyn-diff diff --git-compare main..feature \
  --include "src/Core/**/*.cs" \
  --include "src/Services/**/*.cs" \
  --json core-services-changes.json
```

### Complex Filtering

Combine multiple filters for precise control:

```bash
roslyn-diff diff --git-compare main..feature \
  --recursive \
  --include "src/**/*.cs" \
  --include "src/**/*.vb" \
  --exclude "**/*.g.cs" \
  --exclude "**/*.Designer.cs" \
  --exclude "src/Tests/**" \
  --exclude "bin/**" \
  --exclude "obj/**" \
  --json filtered-changes.json
```

**This will**:
- ✅ Include all C# and VB.NET files in src/ and subdirectories
- ❌ Exclude generated files (*.g.cs, *.Designer.cs)
- ❌ Exclude test files (src/Tests/**)
- ❌ Exclude build artifacts (bin/**, obj/**)

---

## Integration Patterns

### CI/CD Pipeline

**GitHub Actions Example**:

```yaml
name: PR Analysis

on:
  pull_request:
    branches: [main]

jobs:
  analyze-changes:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3
        with:
          fetch-depth: 0  # Full history for git comparison

      - name: Setup .NET
        uses: actions/setup-dotnet@v3
        with:
          dotnet-version: '8.0.x'

      - name: Install roslyn-diff
        run: dotnet tool install -g roslyn-diff

      - name: Analyze PR Changes
        run: |
          roslyn-diff diff --git-compare origin/main..HEAD \
            --include "src/**/*.cs" \
            --exclude "**/*.g.cs" \
            --impact-level breaking-public \
            --json pr-analysis.json

      - name: Upload Results
        uses: actions/upload-artifact@v3
        with:
          name: diff-analysis
          path: pr-analysis.json

      - name: Check for Breaking Changes
        run: |
          breaking=$(jq '.summary.impactBreakdown.breakingPublicApi' pr-analysis.json)
          if [ "$breaking" -gt 0 ]; then
            echo "::warning::Found $breaking breaking public API changes"
          fi
```

### Pre-Push Hook

Prevent pushing breaking changes:

```bash
#!/bin/bash
# .git/hooks/pre-push

echo "Analyzing changes before push..."

roslyn-diff diff --git-compare origin/main..HEAD \
  --include "src/**/*.cs" \
  --impact-level breaking-public \
  --json /tmp/pre-push-analysis.json

breaking=$(jq '.summary.impactBreakdown.breakingPublicApi' /tmp/pre-push-analysis.json)

if [ "$breaking" -gt 0 ]; then
  echo ""
  echo "❌ PUSH BLOCKED: Found $breaking breaking public API changes"
  echo "Review /tmp/pre-push-analysis.json before pushing."
  echo ""
  exit 1
fi

echo "✅ No breaking changes detected"
```

### Release Notes Automation

Extract breaking changes for release notes:

```bash
#!/bin/bash
# generate-release-notes.sh

OLD_VERSION=$1
NEW_VERSION=$2

echo "Generating release notes for $OLD_VERSION -> $NEW_VERSION"

# Analyze all changes
roslyn-diff diff --git-compare $OLD_VERSION..$NEW_VERSION \
  --include "src/**/*.cs" \
  --json release-analysis.json

# Extract breaking changes
jq -r '.files[].result.changes[] |
  select(.impact == "breakingPublicApi") |
  "### Breaking: \(.kind) `\(.name)`\n\(.content)\n"' \
  release-analysis.json > BREAKING.md

# Extract new features (additions)
jq -r '.files[].result.changes[] |
  select(.type == "added" and .impact != "formattingOnly") |
  "### New: \(.kind) `\(.name)`"' \
  release-analysis.json > FEATURES.md

echo "Release notes generated:"
echo "  - BREAKING.md (breaking changes)"
echo "  - FEATURES.md (new features)"
```

### Dashboard Integration

Parse JSON for web dashboard:

```javascript
// parse-diff-results.js
const fs = require('fs');

const data = JSON.parse(fs.readFileSync('pr-analysis.json'));

// Generate summary HTML
const summaryHtml = `
<div class="diff-summary">
  <h2>Code Review Summary</h2>

  <div class="file-stats">
    <div class="stat">
      <span class="number">${data.summary.totalFiles}</span>
      <span class="label">Files Changed</span>
    </div>
    <div class="stat modified">
      <span class="number">${data.summary.modifiedFiles}</span>
      <span class="label">Modified</span>
    </div>
    <div class="stat added">
      <span class="number">${data.summary.addedFiles}</span>
      <span class="label">Added</span>
    </div>
    <div class="stat removed">
      <span class="number">${data.summary.removedFiles}</span>
      <span class="label">Removed</span>
    </div>
  </div>

  <div class="impact-stats">
    <div class="impact breaking">
      <span class="number">${data.summary.impactBreakdown.breakingPublicApi}</span>
      <span class="label">Breaking Public API</span>
    </div>
    <div class="impact internal">
      <span class="number">${data.summary.impactBreakdown.breakingInternalApi}</span>
      <span class="label">Breaking Internal</span>
    </div>
    <div class="impact safe">
      <span class="number">${data.summary.impactBreakdown.nonBreaking}</span>
      <span class="label">Non-Breaking</span>
    </div>
  </div>

  <h3>Modified Files</h3>
  <ul class="file-list">
    ${data.files
      .filter(f => f.status === 'modified')
      .map(f => `
        <li>
          <span class="path">${f.newPath}</span>
          <span class="changes">${f.result.summary.totalChanges} changes</span>
        </li>
      `)
      .join('')}
  </ul>
</div>
`;

fs.writeFileSync('dashboard.html', summaryHtml);
console.log('Dashboard HTML generated: dashboard.html');
```

---

## Understanding the Output

### JSON Schema v3 Structure

Multi-file comparison uses JSON schema v3:

```json
{
  "$schema": "roslyn-diff-output-v3",
  "metadata": {
    "version": "0.10.0",
    "mode": "multi-file",
    "comparisonMode": "folder",  // or "git"
    "oldRoot": "/path/to/old/",
    "newRoot": "/path/to/new/"
  },
  "summary": {
    "totalFiles": 4,
    "modifiedFiles": 2,
    "addedFiles": 1,
    "removedFiles": 1,
    "totalChanges": 10,
    "impactBreakdown": {
      "breakingPublicApi": 10,
      "breakingInternalApi": 0,
      "nonBreaking": 0,
      "formattingOnly": 0
    }
  },
  "files": [
    {
      "oldPath": "/path/to/old/Calculator.cs",
      "newPath": "/path/to/new/Calculator.cs",
      "status": "modified",
      "result": {
        /* Standard single-file DiffResult */
      }
    }
  ]
}
```

### File Status Values

| Status | Description | oldPath | newPath |
|--------|-------------|---------|---------|
| `modified` | File exists in both versions with changes | Present | Present |
| `added` | File only in new version | `null` | Present |
| `removed` | File only in old version | Present | `null` |
| `renamed` | File moved (future) | Old path | New path |

### Summary Fields

**File Counts**:
- `totalFiles` - Total number of changed files
- `modifiedFiles` - Files with content changes
- `addedFiles` - Newly added files
- `removedFiles` - Deleted files
- `renamedFiles` - Moved files (0 in current version)

**Change Counts**:
- `totalChanges` - Sum of all changes across all files
- `impactBreakdown` - Aggregated impact classification

### Per-File Results

Each file in the `files` array contains:
- `oldPath` - Full path in old version
- `newPath` - Full path in new version
- `status` - Change status
- `result` - Complete `DiffResult` for the file

The `result` object is identical to single-file output:
- `summary` - File-specific change summary
- `changes` - Hierarchical change tree
- Impact classification per change
- Location information

### Querying the Output

**Extract specific file types**:
```bash
jq '.files[] | select(.newPath | endswith(".cs"))' folder-comparison.json
```

**Find breaking changes**:
```bash
jq '.files[].result.changes[] | select(.impact == "breakingPublicApi")' folder-comparison.json
```

**Count changes by impact**:
```bash
jq '.summary.impactBreakdown' folder-comparison.json
```

**List added files**:
```bash
jq -r '.files[] | select(.status == "added") | .newPath' folder-comparison.json
```

**Find files with most changes**:
```bash
jq -r '.files[] | "\(.result.summary.totalChanges)\t\(.newPath)"' folder-comparison.json | sort -rn
```

---

## Generating These Samples

To regenerate the sample outputs:

### Folder Comparison

```bash
cd samples/multi-file

# Basic folder comparison
roslyn-diff diff test-folders/before/ test-folders/after/ \
  --json folder-comparison.json

# With filtering (same result in this case)
roslyn-diff diff test-folders/before/ test-folders/after/ \
  --include "*.cs" \
  --json folder-comparison-filtered.json
```

### Git Comparison

```bash
cd samples/multi-file

# Real git comparison from roslyn-diff repository
roslyn-diff diff --git-compare f8c0e87..69ce4cb \
  --include "src/**/*.cs" \
  --exclude "**/bin/**" \
  --exclude "**/obj/**" \
  --json git-comparison.json
```

---

## Future Enhancements

### HTML Output (Planned)

When HTML output is implemented:

**Document Mode**:
```bash
# Single HTML file with file navigation
roslyn-diff diff old/ new/ --html report.html
```

**Fragment Mode**:
```bash
# Directory of embeddable fragments
roslyn-diff diff old/ new/ \
  --html-mode fragment \
  --html fragments/
```

### Inline View Integration (Planned)

```bash
# Multi-file with inline diff view
roslyn-diff diff --git-compare main..feature \
  --inline=5 \
  --html pr-review.html
```

### Progress Reporting (Planned)

For large repositories:
```bash
# Progress bar for 1000+ files
roslyn-diff diff --git-compare v1.0..v2.0 \
  --show-progress \
  --json release-diff.json
```

---

## Tips and Best Practices

### Performance

1. **Use Filtering** - Exclude unnecessary files for faster analysis
2. **Non-Recursive** - Use default (non-recursive) when possible
3. **Impact Filtering** - Focus on breaking changes first
4. **Parallel Processing** - Automatic, leverages multiple CPU cores

### Accuracy

1. **Full Git History** - Use `fetch-depth: 0` in CI/CD for complete history
2. **Target Frameworks** - Specify `-t` flags for multi-TFM projects
3. **Include Patterns** - Be explicit about which files to analyze

### Maintainability

1. **Script Your Filters** - Create shell scripts for common filter combinations
2. **Save Configurations** - Document filter patterns in README
3. **Automate Analysis** - Integrate into CI/CD for consistent checks

---

## Related Documentation

- [Multi-File Comparison Guide](../../docs/multi-file-comparison.md) - Complete feature documentation
- [Output Formats](../../docs/output-formats.md) - JSON schema v3 details
- [Usage Guide](../../docs/usage.md) - General CLI usage
- [Glob Patterns](../../docs/GLOB_PATTERNS.md) - Detailed glob syntax

---

## Questions and Feedback

For questions or feedback about multi-file comparison:
- File an issue: [GitHub Issues](https://github.com/randlee/roslyn-diff/issues)
- Read the docs: [docs/multi-file-comparison.md](../../docs/multi-file-comparison.md)

---

**Samples Version**: 1.0
**Last Updated**: 2026-01-28
**Feature Version**: 0.10.0 (Phase 2 Complete)
