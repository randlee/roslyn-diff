# Design: Multi-File and Repository Diff Support

**Document ID:** DESIGN-007
**Date:** 2026-01-28
**Status:** PLANNING
**Related Issue:** #46 (split from HTML Fragment Mode request)
**Target Version:** v0.10.0 or v1.0.0

---

## 1. Overview

Add support for comparing multiple files, directories, and git branches in a single operation. This enables developers to analyze entire changesets at once, particularly useful for PR reviews, release comparisons, and migration analysis.

### Goals

1. **Folder-to-folder comparison** - Compare all files in two directories
2. **Git branch comparison** - Compare HEAD states of two branches
3. **File matching strategies** - Smart pairing of old/new files
4. **Aggregated output** - Unified reports with per-file sections
5. **Flexible filtering** - Include/exclude patterns for file selection
6. **Performance** - Parallel processing for large changesets

### Non-Goals (for initial release)

- Git commit range comparison (e.g., `main..HEAD~5`)
- Rename detection across files (within-file only)
- Binary file comparison
- Directory structure comparison (focus on file content)

---

## 2. Problem Statement

### Current Limitations

**Current roslyn-diff** only compares two files at a time:
```bash
roslyn-diff diff old.cs new.cs
```

**For multi-file scenarios**, users must:
1. Write shell scripts to loop through files
2. Manually aggregate results
3. Handle file matching logic themselves
4. Combine output formats manually

### User Pain Points

**PR Review Workflow** (from issue #46):
```bash
# Current: Manual loop and aggregation
for file in changed_files; do
  roslyn-diff diff target/$file source/$file --html fragments/$file.html --html-mode fragment
done
cat fragments/*.html > report.html
```

**Desired workflow**:
```bash
# Automatic multi-file comparison
roslyn-diff diff --git main..feature-branch --html report.html
```

**Other use cases**:
- **Release comparison**: Compare v1.0.0 vs v2.0.0 codebase
- **Migration analysis**: Before/after refactoring across many files
- **Bulk validation**: Ensure formatting changes don't affect semantics
- **CI/CD integration**: Automated PR analysis

---

## 3. Current Implementation Analysis

### CLI Structure

**File**: `src/RoslynDiff.Cli/Commands/DiffCommand.cs`

Current signature:
```csharp
public class DiffCommand : Command
{
    private static Argument<string> OldPathArgument = new(
        "old-path",
        "Path to the original file"
    );

    private static Argument<string> NewPathArgument = new(
        "new-path",
        "Path to the modified file"
    );
}
```

**Current flow**:
1. Parse CLI arguments (two file paths)
2. Read both files
3. Create differ based on file extension
4. Compare and output results

### DifferFactory

**File**: `src/RoslynDiff.Core/Differ/DifferFactory.cs`

```csharp
public interface IDiffer
{
    DiffResult Compare(string oldContent, string newContent, DiffOptions options);
}

public class DifferFactory
{
    public IDiffer GetDiffer(string filePath, DiffOptions options)
    {
        var extension = Path.GetExtension(filePath).ToLowerInvariant();
        return extension switch
        {
            ".cs" => new RoslynDiffer(),
            ".vb" => new RoslynDiffer(),
            _ => new LineDiffer()
        };
    }
}
```

**Good news**: Current architecture already returns per-file `DiffResult` objects. Multi-file support needs to:
1. Generate multiple `DiffResult` instances
2. Aggregate them into a collection
3. Update formatters to handle collections

---

## 4. Proposed Solution

### 4.1 Multi-File Modes

Three distinct comparison modes:

#### Mode 1: Folder-to-Folder
```bash
roslyn-diff diff src/old/ src/new/

# With filtering
roslyn-diff diff src/old/ src/new/ --include "*.cs" --exclude "*.g.cs"

# Recursive (default) vs shallow
roslyn-diff diff src/old/ src/new/ --no-recursive
```

**Behavior**:
- Match files by relative path
- Report unmatched files (added/removed)
- Support glob patterns for filtering

#### Mode 2: Git Branch Comparison
```bash
# Compare HEAD of two branches
roslyn-diff diff --git main..feature-branch

# Compare specific commits
roslyn-diff diff --git abc123..def456

# Compare working tree vs branch
roslyn-diff diff --git main..HEAD
```

**Behavior**:
- Use libgit2sharp or git CLI to extract file contents
- Detect renamed files (git's rename detection)
- Only compare text files (skip binaries)
- Support gitignore patterns

#### Mode 3: File List
```bash
# Explicit file pairs
roslyn-diff diff --file-pairs pairs.txt

# Format of pairs.txt:
# old/Calculator.cs,new/Calculator.cs
# old/Service.cs,new/Service.cs
```

**Behavior**:
- Read file pairs from text file
- Useful for custom matching logic
- Each line: `old_path,new_path`

### 4.2 CLI Design

**Updated argument structure**:
```bash
roslyn-diff diff <source> [target] [options]

# Examples:
roslyn-diff diff old.cs new.cs                    # Single file (current)
roslyn-diff diff old/ new/                        # Folder-to-folder
roslyn-diff diff --git main..feature              # Git comparison
roslyn-diff diff --file-pairs pairs.txt           # Explicit pairs
```

**New options**:
```bash
--git <ref-range>           Git comparison mode (e.g., main..feature)
--file-pairs <path>         File containing list of file pairs
--include <pattern>         Include files matching pattern (repeatable)
--exclude <pattern>         Exclude files matching pattern (repeatable)
--recursive                 Recurse into subdirectories (default: true)
--no-recursive              Don't recurse into subdirectories
--match-by <strategy>       File matching strategy: path, name, content
--max-files <n>             Limit number of files to compare
--parallel <n>              Number of parallel workers (default: CPU count)
```

### 4.3 File Matching Strategies

**Strategy 1: Path Matching** (default for folders)
```bash
roslyn-diff diff old/ new/ --match-by path
```
- Match by relative path: `old/src/Foo.cs` → `new/src/Foo.cs`
- Exact path match required
- Fast and predictable

**Strategy 2: Name Matching** (useful for reorganization)
```bash
roslyn-diff diff old/ new/ --match-by name
```
- Match by filename only: `old/a/Foo.cs` → `new/b/Foo.cs`
- Useful when files moved between directories
- May have ambiguity (multiple matches)

**Strategy 3: Content Similarity** (expensive but powerful)
```bash
roslyn-diff diff old/ new/ --match-by content --similarity-threshold 0.8
```
- Use fuzzy matching based on file content
- Detect renames even if some content changed
- Computationally expensive (O(n²))

**Strategy 4: Git Rename Detection** (for git mode)
```bash
roslyn-diff diff --git main..feature --match-by git-renames
```
- Use git's built-in rename detection
- Most accurate for git repositories
- Handles complex rename scenarios

### 4.4 Data Model Changes

#### New: MultiFileDiffResult

**File**: `src/RoslynDiff.Core/Models/MultiFileDiffResult.cs`

```csharp
public record MultiFileDiffResult
{
    /// <summary>
    /// Individual file diff results.
    /// </summary>
    public IReadOnlyList<FileDiffResult> Files { get; init; } = Array.Empty<FileDiffResult>();

    /// <summary>
    /// Overall summary across all files.
    /// </summary>
    public MultiFileSummary Summary { get; init; } = new();

    /// <summary>
    /// Metadata about the comparison operation.
    /// </summary>
    public MultiFileMetadata Metadata { get; init; } = new();
}

public record FileDiffResult
{
    /// <summary>
    /// The single-file diff result.
    /// </summary>
    public DiffResult Result { get; init; } = null!;

    /// <summary>
    /// Status of this file in the changeset.
    /// </summary>
    public FileChangeStatus Status { get; init; }

    /// <summary>
    /// Old file path (relative to comparison root).
    /// </summary>
    public string? OldPath { get; init; }

    /// <summary>
    /// New file path (relative to comparison root).
    /// </summary>
    public string? NewPath { get; init; }
}

public enum FileChangeStatus
{
    Modified,    // File exists in both old and new
    Added,       // File only exists in new
    Removed,     // File only exists in old
    Renamed,     // File was renamed (with or without modifications)
    Unchanged    // File exists in both but no changes detected
}

public record MultiFileSummary
{
    public int TotalFiles { get; init; }
    public int ModifiedFiles { get; init; }
    public int AddedFiles { get; init; }
    public int RemovedFiles { get; init; }
    public int RenamedFiles { get; init; }
    public int UnchangedFiles { get; init; }

    public int TotalChanges { get; init; }
    public int TotalAdditions { get; init; }
    public int TotalDeletions { get; init; }
    public int TotalModifications { get; init; }

    // Impact breakdown across all files
    public ImpactBreakdown ImpactBreakdown { get; init; } = new();
}

public record MultiFileMetadata
{
    public string Mode { get; init; } = "multi-file";  // "folder", "git", "file-pairs"
    public string? OldRoot { get; init; }
    public string? NewRoot { get; init; }
    public string? GitRefRange { get; init; }
    public DateTime Timestamp { get; init; }
    public string Version { get; init; } = "";
    public ComparisonOptions Options { get; init; } = new();
}
```

### 4.5 Folder Comparison Implementation

**File**: `src/RoslynDiff.Core/MultiFile/FolderComparer.cs`

```csharp
public class FolderComparer
{
    private readonly DifferFactory _differFactory;
    private readonly IFileMatchingStrategy _matchingStrategy;

    public MultiFileDiffResult Compare(
        string oldPath,
        string newPath,
        MultiFileOptions options)
    {
        // 1. Discover files in both directories
        var oldFiles = DiscoverFiles(oldPath, options);
        var newFiles = DiscoverFiles(newPath, options);

        // 2. Match files using strategy
        var pairs = _matchingStrategy.MatchFiles(oldFiles, newFiles, options);

        // 3. Compare each pair in parallel
        var results = new ConcurrentBag<FileDiffResult>();

        Parallel.ForEach(pairs, new ParallelOptions
        {
            MaxDegreeOfParallelism = options.ParallelWorkers
        },
        pair =>
        {
            var result = ComparePair(pair, options);
            results.Add(result);
        });

        // 4. Aggregate results
        return AggregateResults(results, oldPath, newPath, options);
    }

    private IEnumerable<string> DiscoverFiles(string root, MultiFileOptions options)
    {
        var searchOption = options.Recursive
            ? SearchOption.AllDirectories
            : SearchOption.TopDirectoryOnly;

        var files = Directory.GetFiles(root, "*.*", searchOption);

        // Apply include/exclude filters
        files = ApplyFilters(files, options.IncludePatterns, options.ExcludePatterns);

        return files;
    }

    private FileDiffResult ComparePair(
        FilePair pair,
        MultiFileOptions options)
    {
        var differ = _differFactory.GetDiffer(pair.OldPath ?? pair.NewPath, options.DiffOptions);

        string oldContent = pair.OldPath != null ? File.ReadAllText(pair.OldPath) : "";
        string newContent = pair.NewPath != null ? File.ReadAllText(pair.NewPath) : "";

        var result = differ.Compare(oldContent, newContent, options.DiffOptions);

        return new FileDiffResult
        {
            Result = result,
            OldPath = pair.OldPath != null ? Path.GetRelativePath(options.OldRoot, pair.OldPath) : null,
            NewPath = pair.NewPath != null ? Path.GetRelativePath(options.NewRoot, pair.NewPath) : null,
            Status = DetermineStatus(pair, result)
        };
    }
}
```

### 4.6 Git Comparison Implementation

**File**: `src/RoslynDiff.Core/MultiFile/GitComparer.cs`

**Approach A: Use LibGit2Sharp** (Recommended)
```csharp
using LibGit2Sharp;

public class GitComparer
{
    public MultiFileDiffResult Compare(
        string repoPath,
        string refRange,  // e.g., "main..feature-branch"
        MultiFileOptions options)
    {
        using var repo = new Repository(repoPath);

        // Parse ref range
        var parts = refRange.Split("..");
        var oldRef = parts[0];
        var newRef = parts.Length > 1 ? parts[1] : "HEAD";

        var oldCommit = repo.Lookup<Commit>(oldRef);
        var newCommit = repo.Lookup<Commit>(newRef);

        // Get tree diff
        var diff = repo.Diff.Compare<TreeChanges>(oldCommit.Tree, newCommit.Tree);

        var results = new List<FileDiffResult>();

        foreach (var change in diff)
        {
            // Skip binary files
            if (change.IsBinaryComparison) continue;

            // Get file content at each commit
            var oldContent = GetBlobContent(repo, oldCommit, change.OldPath);
            var newContent = GetBlobContent(repo, newCommit, change.Path);

            // Compare using appropriate differ
            var differ = _differFactory.GetDiffer(change.Path, options.DiffOptions);
            var result = differ.Compare(oldContent, newContent, options.DiffOptions);

            results.Add(new FileDiffResult
            {
                Result = result,
                OldPath = change.OldPath,
                NewPath = change.Path,
                Status = MapGitStatus(change.Status)
            });
        }

        return AggregateResults(results, oldRef, newRef, options);
    }

    private string GetBlobContent(Repository repo, Commit commit, string path)
    {
        if (string.IsNullOrEmpty(path)) return "";

        var treeEntry = commit[path];
        if (treeEntry == null) return "";

        var blob = (Blob)treeEntry.Target;
        return blob.GetContentText();
    }
}
```

**Approach B: Shell out to git CLI** (Fallback)
```csharp
public class GitCliComparer
{
    public MultiFileDiffResult Compare(
        string repoPath,
        string refRange,
        MultiFileOptions options)
    {
        // Get list of changed files
        var changedFiles = GetChangedFiles(repoPath, refRange);

        var results = new List<FileDiffResult>();

        foreach (var file in changedFiles)
        {
            // Get file content at each ref
            var oldContent = GetFileContentAtRef(repoPath, file.OldPath, file.OldRef);
            var newContent = GetFileContentAtRef(repoPath, file.NewPath, file.NewRef);

            // Compare
            var differ = _differFactory.GetDiffer(file.NewPath, options.DiffOptions);
            var result = differ.Compare(oldContent, newContent, options.DiffOptions);

            results.Add(new FileDiffResult
            {
                Result = result,
                OldPath = file.OldPath,
                NewPath = file.NewPath,
                Status = file.Status
            });
        }

        return AggregateResults(results, refRange, options);
    }

    private List<GitFileChange> GetChangedFiles(string repoPath, string refRange)
    {
        // git diff --name-status main..feature
        var output = RunGit(repoPath, $"diff --name-status {refRange}");

        return ParseGitDiffOutput(output);
    }

    private string GetFileContentAtRef(string repoPath, string path, string gitRef)
    {
        if (string.IsNullOrEmpty(path)) return "";

        // git show ref:path
        return RunGit(repoPath, $"show {gitRef}:{path}");
    }
}
```

**Decision**: Use **LibGit2Sharp** (Approach A) for robustness. Add git CLI fallback if LibGit2Sharp fails.

### 4.7 Output Format Changes

#### JSON Output

**Updated schema** (v3):
```json
{
  "$schema": "roslyn-diff-output-v3",
  "metadata": {
    "version": "0.10.0",
    "timestamp": "2026-01-28T10:00:00Z",
    "mode": "multi-file",
    "comparisonMode": "git",
    "gitRefRange": "main..feature-branch",
    "options": {
      "includeNonImpactful": false,
      "targetFrameworks": ["net8.0"]
    }
  },
  "summary": {
    "totalFiles": 15,
    "modifiedFiles": 8,
    "addedFiles": 5,
    "removedFiles": 2,
    "renamedFiles": 3,
    "unchangedFiles": 0,
    "totalChanges": 47,
    "additions": 23,
    "deletions": 12,
    "modifications": 12,
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
    },
    {
      "oldPath": null,
      "newPath": "src/NewService.cs",
      "status": "added",
      "result": {
        "summary": { /* all additions */ },
        "changes": [ /* ... */ ]
      }
    }
  ]
}
```

#### HTML Output

**Multi-file HTML structure**:
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
      <h1>Comparison: main..feature-branch</h1>
      <div class="stats">
        <span class="stat">15 files</span>
        <span class="stat">47 changes</span>
        <span class="stat breaking">5 breaking public</span>
      </div>
    </div>

    <!-- File list navigation -->
    <nav class="file-list">
      <h2>Changed Files</h2>
      <ul>
        <li><a href="#file-0">src/Calculator.cs</a> <span class="badge">8 changes</span></li>
        <li><a href="#file-1">src/Service.cs</a> <span class="badge">12 changes</span></li>
        <!-- ... -->
      </ul>
    </nav>

    <!-- Individual file diffs -->
    <div class="file-diffs">
      <section id="file-0" class="file-diff">
        <h2>src/Calculator.cs</h2>
        <!-- Existing single-file diff HTML -->
      </section>

      <section id="file-1" class="file-diff">
        <h2>src/Service.cs</h2>
        <!-- Existing single-file diff HTML -->
      </section>
    </div>
  </div>
</body>
</html>
```

**With fragment mode**:
```bash
roslyn-diff diff --git main..feature \
  --html-mode fragment \
  --html-output fragments/

# Generates:
# fragments/src-Calculator.cs.html
# fragments/src-Service.cs.html
# fragments/index.html (optional master index)
```

#### Text Output

```
Multi-File Diff: main..feature-branch
Mode: Git comparison
Files: 15 (8 modified, 5 added, 2 removed)
Changes: 47 (+23, -12, ~12)
Impact: 5 breaking public, 8 breaking internal, 32 non-breaking

Files Changed:
  [M] src/Calculator.cs (8 changes, 2 breaking public)
  [M] src/Service.cs (12 changes, 3 breaking internal)
  [A] src/NewService.cs (5 additions)
  [R] src/OldHelper.cs (removed)

──────────────────────────────────────────
File: src/Calculator.cs
──────────────────────────────────────────
  [+] Method: Multiply (line 15-18) [Breaking Public API]
  [~] Method: Divide (line 20-25) [Breaking Public API]

──────────────────────────────────────────
File: src/Service.cs
──────────────────────────────────────────
  [+] Method: ProcessAsync (line 30-45) [Breaking Public API]
  [~] Property: ConnectionString (line 12) [Non-Breaking]
```

---

## 5. Implementation Plan

### Phase 1: Core Multi-File Infrastructure (v0.10.0-alpha.1)
**Estimated effort**: 1 week

**Tasks**:
1. ✅ Create `MultiFileDiffResult` and related models
2. ✅ Create `MultiFileOptions` for configuration
3. ✅ Implement `FolderComparer` for directory comparison
4. ✅ Implement path-based file matching strategy
5. ✅ Add basic parallel processing support
6. ✅ Update CLI to detect folder arguments
7. ✅ Add unit tests for folder comparison

**Output**: Working `roslyn-diff diff old/ new/`

### Phase 2: File Filtering & Matching (v0.10.0-alpha.2)
**Estimated effort**: 3-4 days

**Tasks**:
1. ✅ Implement include/exclude pattern support (glob patterns)
2. ✅ Add name-based matching strategy
3. ✅ Add content-similarity matching strategy
4. ✅ Implement `--match-by` CLI option
5. ✅ Add tests for filtering and matching

**Output**: Flexible file matching with filtering

### Phase 3: Git Integration (v0.10.0-beta.1)
**Estimated effort**: 1 week

**Tasks**:
1. ✅ Add LibGit2Sharp dependency
2. ✅ Implement `GitComparer` using LibGit2Sharp
3. ✅ Add `--git` CLI option
4. ✅ Support ref range parsing (main..feature, commit..commit)
5. ✅ Handle git rename detection
6. ✅ Add git CLI fallback for error cases
7. ✅ Add integration tests with test git repositories

**Output**: Working `roslyn-diff diff --git main..feature`

### Phase 4: Output Format Updates (v0.10.0-beta.2)
**Estimated effort**: 1 week

**Tasks**:
1. ✅ Update JSON formatter for multi-file output (schema v3)
2. ✅ Update HTML formatter with file navigation
3. ✅ Update text formatter for multi-file summary
4. ✅ Integrate with fragment mode (generate one fragment per file)
5. ✅ Add `--html-output-dir` for directory of fragments
6. ✅ Generate index.html for fragment collections

**Output**: Complete multi-file output support

### Phase 5: Performance Optimization (v0.10.0-rc.1)
**Estimated effort**: 3-4 days

**Tasks**:
1. ✅ Optimize parallel processing (tuning worker count)
2. ✅ Add progress reporting for large changesets
3. ✅ Implement early termination for identical files
4. ✅ Add file size limits and warnings
5. ✅ Memory profiling and optimization
6. ✅ Performance benchmarks for multi-file scenarios

**Output**: Performant multi-file comparison

### Phase 6: Documentation & Release (v0.10.0)
**Estimated effort**: 3-4 days

**Tasks**:
1. ✅ Update README.md with multi-file examples
2. ✅ Create `docs/multi-file-comparison.md` comprehensive guide
3. ✅ Update `docs/usage.md` with CLI reference
4. ✅ Add `samples/multi-file/` with examples
5. ✅ Update CHANGELOG.md
6. ✅ Create migration guide from single-file workflows
7. ✅ Final testing with real-world repositories

**Output**: Documented, tested v0.10.0 release

---

## 6. Testing Strategy

### 6.1 Unit Tests

**Folder comparison**:
```csharp
[Fact]
public void FolderComparer_MatchesByPath()
{
    var comparer = new FolderComparer();
    var result = comparer.Compare("testdata/old", "testdata/new", options);

    Assert.Equal(5, result.Summary.TotalFiles);
    Assert.Equal(3, result.Summary.ModifiedFiles);
    Assert.Equal(1, result.Summary.AddedFiles);
    Assert.Equal(1, result.Summary.RemovedFiles);
}

[Fact]
public void FolderComparer_RespectsIncludePattern()
{
    var options = new MultiFileOptions
    {
        IncludePatterns = new[] { "*.cs" }
    };

    var result = comparer.Compare("testdata/old", "testdata/new", options);

    Assert.All(result.Files, file =>
        Assert.EndsWith(".cs", file.NewPath ?? file.OldPath));
}
```

**Git comparison**:
```csharp
[Fact]
public void GitComparer_ComparesGitRefs()
{
    // Setup: Create test repo with two branches
    var repoPath = CreateTestGitRepo();

    var comparer = new GitComparer();
    var result = comparer.Compare(repoPath, "main..feature", options);

    Assert.Equal(3, result.Summary.ModifiedFiles);
    Assert.Equal("main..feature", result.Metadata.GitRefRange);
}
```

### 6.2 Integration Tests

**End-to-end CLI test**:
```csharp
[Fact]
public void Cli_FolderComparison_GeneratesMultiFileJson()
{
    var exitCode = RunCli("diff testdata/old testdata/new --json output.json");

    Assert.Equal(1, exitCode); // Differences found
    Assert.True(File.Exists("output.json"));

    var json = File.ReadAllText("output.json");
    var result = JsonSerializer.Deserialize<MultiFileDiffResult>(json);

    Assert.Equal("multi-file", result.Metadata.Mode);
    Assert.True(result.Summary.TotalFiles > 0);
}

[Fact]
public void Cli_GitComparison_GeneratesHtmlReport()
{
    var repoPath = CreateTestGitRepo();
    var exitCode = RunCli($"diff --git main..feature --html report.html", repoPath);

    Assert.Equal(1, exitCode);
    Assert.True(File.Exists("report.html"));

    var html = File.ReadAllText("report.html");
    Assert.Contains("Multi-File Diff: main..feature", html);
}
```

### 6.3 Performance Tests

**Benchmark large repositories**:
```csharp
[Benchmark]
public void CompareLargeRepository()
{
    // Repository with 1000 files
    var result = comparer.Compare(largRepoOld, largeRepoNew, options);
}

[Benchmark]
public void CompareWithParallelism()
{
    var options = new MultiFileOptions
    {
        ParallelWorkers = 8
    };
    var result = comparer.Compare(repoOld, repoNew, options);
}
```

**Acceptance criteria**:
- 100 files: < 5 seconds
- 1000 files: < 30 seconds
- 10000 files: < 5 minutes

---

## 7. Dependencies

### 7.1 New NuGet Packages

**LibGit2Sharp** (required for git integration)
```xml
<PackageReference Include="LibGit2Sharp" Version="0.30.0" />
```

**Alternative**: Shell out to git CLI (no dependency but less robust)

### 7.2 Optional Packages

**Glob patterns** (for include/exclude):
```xml
<PackageReference Include="DotNet.Glob" Version="3.1.3" />
```

Or use built-in `Regex` for simple patterns.

---

## 8. Documentation Updates

### 8.1 New Documentation Files

**docs/multi-file-comparison.md** - Comprehensive guide:
- Overview of multi-file comparison
- Folder-to-folder comparison
- Git branch comparison
- File matching strategies
- Output formats for multi-file results
- Performance considerations
- Best practices

**docs/git-integration.md** - Git-specific guide:
- Git ref range syntax
- Rename detection
- Working with large repositories
- Troubleshooting git issues

### 8.2 README.md Updates

Add section:
```markdown
## Multi-File Comparison

Compare entire directories or git branches in one operation:

```bash
# Folder-to-folder comparison
roslyn-diff diff src/old/ src/new/

# Git branch comparison
roslyn-diff diff --git main..feature-branch

# With filtering
roslyn-diff diff src/old/ src/new/ --include "*.cs" --exclude "*.g.cs"

# Generate unified HTML report
roslyn-diff diff --git main..feature --html report.html
```

See [Multi-File Comparison Guide](docs/multi-file-comparison.md) for details.
```

---

## 9. Open Questions

### Q1: Should we support commit range comparison (not just two refs)?

**Example**: Compare all commits between two points
```bash
roslyn-diff diff --git main..HEAD~5
```

**Options**:
1. **No** - Only support HEAD of two refs (simpler)
2. **Yes** - Show evolution across multiple commits (complex)

**Decision**: **No** for v1. Support HEAD-to-HEAD comparison only. Can add commit-by-commit analysis later if needed.

### Q2: How to handle very large repositories (10k+ files)?

**Options**:
1. **Limit** - Default max 1000 files, require `--no-limit` flag
2. **Warn** - Show warning but proceed
3. **Progress** - Show progress bar and allow cancellation

**Decision**: Combination of 2 and 3 - show warning for >1000 files, display progress bar, allow Ctrl+C cancellation.

### Q3: Should we detect moved code across files?

**Example**: Method moved from Calculator.cs to MathUtils.cs

**Options**:
1. **No** - Only detect moves within same file (current)
2. **Yes** - Cross-file move detection (very complex)

**Decision**: **No** for v1. Cross-file move detection is complex and may have false positives. Revisit in v2.0 if users request it.

### Q4: Binary file handling?

**Options**:
1. **Skip** - Ignore binary files entirely
2. **Detect** - Show "binary file changed" in output
3. **Hash** - Compare binary files by hash only

**Decision**: **Option 2** - detect and report binary file changes but don't attempt to diff content.

---

## 10. Risks & Mitigations

| Risk | Likelihood | Impact | Mitigation |
|------|------------|--------|------------|
| LibGit2Sharp compatibility issues | Medium | High | Implement git CLI fallback |
| Performance with large repos | High | Medium | Parallel processing, progress reporting, limits |
| File matching ambiguity (multiple matches) | Medium | Medium | Clear error messages, require explicit disambiguation |
| Memory usage with many files | Medium | High | Stream processing, dispose resources, add limits |
| Git rename detection false positives | Low | Low | Use git's conservative thresholds, allow override |

---

## 11. Success Metrics

### Acceptance Criteria

- [ ] Folder-to-folder comparison works with glob filters
- [ ] Git branch comparison works with ref range syntax
- [ ] Parallel processing improves performance on multi-core systems
- [ ] Multi-file JSON output matches schema v3
- [ ] Multi-file HTML output has navigation between files
- [ ] Fragment mode generates one fragment per file
- [ ] Git rename detection works correctly
- [ ] Performance: 100 files in < 5 seconds

### User Validation

Test with real-world scenarios:
1. Compare two releases of roslyn-diff (self-hosting)
2. Compare feature branch to main (PR review)
3. Compare two versions of large OSS project (e.g., Roslyn itself)
4. Generate HTML report for PR with 20+ changed files
5. Verify fragment mode works with multi-file output

---

## 12. Future Enhancements (Post-v1.0)

### v1.1: Advanced Git Features
- Commit range comparison (not just HEAD-to-HEAD)
- Compare working directory to git ref
- Stash comparison
- Submodule support

### v1.2: Cross-File Analysis
- Detect moved code across files
- Detect refactored code (extract method, inline, etc.)
- Track symbol references across files

### v1.3: Cloud Integration
- GitHub PR integration (fetch via API)
- Azure DevOps integration
- GitLab integration

---

## 13. Alternatives Considered

### Alternative 1: Wrapper Script

**Description**: Provide shell/PowerShell scripts that loop through files

**Pros**:
- No code changes needed
- Users have full control

**Cons**:
- Poor user experience
- No unified output format
- Doesn't solve aggregation problem
- Not cross-platform

**Decision**: **Rejected** - Native support provides better UX.

### Alternative 2: External Tool Integration

**Description**: Integrate with existing tools like `diff`, `git diff`

**Pros**:
- Leverage existing tooling
- No reinvention

**Cons**:
- Loses semantic analysis for multi-file view
- No impact classification across files
- Doesn't match roslyn-diff's value proposition

**Decision**: **Rejected** - Native support aligns with roslyn-diff goals.

---

## 14. Related Issues

- **#46**: HTML Fragment Mode (split from this)
- **#48**: (this issue, to be created)

---

## 15. References

- [LibGit2Sharp Documentation](https://github.com/libgit2/libgit2sharp)
- [Git Diff Documentation](https://git-scm.com/docs/git-diff)
- [DiffPlex Multi-File Examples](https://github.com/mmanela/diffplex/wiki)

---

**Status**: Ready for review and refinement
**Next Steps**:
1. Create GitHub issue #48 for tracking
2. Get stakeholder feedback on design
3. Prioritize against other features (may be v1.0 instead of v0.10)
4. Create feature branch when approved
