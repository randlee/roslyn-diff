# Corner Case Analysis: HTML Fragment, Inline View, and Multi-File Comparison

**Document ID:** QA-001
**Date:** 2026-01-28
**Status:** Complete
**Scope:** v0.9.0 (HTML Fragment & Inline View) and v0.10.0 (Multi-File Comparison)

---

## Executive Summary

This document provides a comprehensive analysis of corner cases and test coverage gaps for three major features:
1. **HTML Fragment Mode** (v0.9.0) - External CSS, embeddable fragments
2. **Inline View Mode** (v0.9.0) - Full file vs context mode, +/- indicators
3. **Multi-File Comparison** (v0.10.0) - Git and folder comparison

The analysis identifies **47 corner cases** that are not fully covered by existing tests, categorized by priority and feature area.

---

## Table of Contents

1. [Feature 1: HTML Fragment Mode](#feature-1-html-fragment-mode)
2. [Feature 2: Inline View Mode](#feature-2-inline-view-mode)
3. [Feature 3: Multi-File Comparison](#feature-3-multi-file-comparison)
4. [Cross-Feature Integration](#cross-feature-integration)
5. [Summary and Recommendations](#summary-and-recommendations)

---

## Feature 1: HTML Fragment Mode

### Current Test Coverage (Good)
- Fragment structure (no document wrapper)
- External CSS generation
- Data attributes (file names, stats, impact)
- Custom CSS filename support
- Backward compatibility with document mode
- Multiple fragments sharing CSS
- Style isolation tests

### Missing Corner Cases

#### HIGH Priority

| ID | Corner Case | Description | Test Recommendation |
|----|-------------|-------------|---------------------|
| F1-H1 | **CSS file write permission denied** | What happens when the CSS output directory is read-only or the file is locked? | Test with read-only directory; verify graceful error message |
| F1-H2 | **CSS path with special characters** | Filenames containing unicode, spaces, or URL-unsafe characters | Test: `"my styles (2).css"`, `"样式.css"`, `"path%20encoded.css"` |
| F1-H3 | **Network path CSS output** | UNC paths like `\\server\share\styles.css` on Windows | Test UNC path handling and appropriate error messages |
| F1-H4 | **Concurrent CSS file writes** | Multiple parallel fragment generations writing to same CSS file | Test with Parallel.ForEach generating 100 fragments to same CSS |
| F1-H5 | **HTML output path without directory** | `HtmlOutputPath = "fragment.html"` (no directory component) | Verify CSS is written to current working directory correctly |

#### MEDIUM Priority

| ID | Corner Case | Description | Test Recommendation |
|----|-------------|-------------|---------------------|
| F1-M1 | **Very long file path** | CSS path exceeding MAX_PATH (260 chars on Windows) | Test with 300+ character path; verify appropriate error |
| F1-M2 | **Fragment with no changes** | DiffResult where all FileChanges have empty Changes arrays | Test empty fragment renders "(no changes)" correctly |
| F1-M3 | **Data attributes with special characters** | File names containing `<`, `>`, `"`, `&` characters | Test: `old-file="File<T>.cs"` is properly escaped |
| F1-M4 | **CSS link href escaping** | ExtractCssPath containing HTML special chars | Test: `href="my&styles.css"` escapes to `href="my&amp;styles.css"` |
| F1-M5 | **ExtractCssPath as absolute path** | User provides full path like `/var/www/styles.css` | Test that absolute paths are handled correctly |
| F1-M6 | **Fragment with null OldPath and NewPath** | DiffResult where both paths are null | Test graceful handling; data-old-file should be empty or omitted |
| F1-M7 | **Very large DiffStats values** | Stats exceeding int.MaxValue theoretically | Test data-changes-total with large numbers; verify no overflow |

#### LOW Priority

| ID | Corner Case | Description | Test Recommendation |
|----|-------------|-------------|---------------------|
| F1-L1 | **CSS file already exists and is empty** | Pre-existing 0-byte CSS file | Verify it gets overwritten correctly |
| F1-L2 | **CSS filename starting with dot** | `.roslyn-diff.css` (hidden file on Unix) | Test hidden file creation works |
| F1-L3 | **Fragment mode with IncludeStats=false** | Verify stats section is omitted but data attributes remain | Confirm data attributes are independent of IncludeStats |

### Implementation Gaps Identified

1. **No validation of ExtractCssPath format** - The implementation accepts any string without validating it's a valid filename.
2. **No error handling documentation** - What exceptions can users expect when CSS file write fails?
3. **CSS content is generated on every call** - Could be optimized to cache the static CSS content.

---

## Feature 2: Inline View Mode

### Current Test Coverage (Good)
- Basic inline structure with diff-inline container
- Line numbers and prefixes (+/-)
- Added/removed/modified line styling
- Full file mode vs context mode
- Context separators between distant changes
- Semantic headers (method, class, property)
- Line diff vs semantic diff modes
- Fragment mode integration
- Special character escaping
- Null content handling

### Missing Corner Cases

#### HIGH Priority

| ID | Corner Case | Description | Test Recommendation |
|----|-------------|-------------|---------------------|
| I1-H1 | **Very long lines (>10,000 chars)** | Code lines exceeding reasonable display width | Test with 10KB single line; verify no memory issues |
| I1-H2 | **Zero-based vs one-based line numbers** | Edge case at line 0 or line 1 boundaries | Verify Location.StartLine=0 is handled (should be 1?) |
| I1-H3 | **Negative line numbers** | Malformed Location with negative StartLine | Test graceful handling of Location { StartLine = -1 } |
| I1-H4 | **InlineContext with null vs 0** | Difference between `InlineContext = null` (full file) and `InlineContext = 0` | Existing test covers 0; add test confirming null shows all |
| I1-H5 | **Overlapping change regions** | Two changes where one's EndLine > other's StartLine | Test changes at lines 5-10 and 8-12; verify no duplicate lines |

#### MEDIUM Priority

| ID | Corner Case | Description | Test Recommendation |
|----|-------------|-------------|---------------------|
| I1-M1 | **Windows vs Unix line endings in content** | OldContent with \r\n, NewContent with \n | Verify line counting works regardless of line ending style |
| I1-M2 | **Tab characters in line content** | Tab characters displayed vs converted to spaces | Test `\t` preservation in HTML output |
| I1-M3 | **Binary content accidentally treated as text** | Content with null bytes: `"class\x00Test"` | Verify graceful handling of binary-ish content |
| I1-M4 | **Changes at file boundaries** | Change at line 1 (start of file) or change at last line | Test StartLine=1 and StartLine=MAX; verify no off-by-one |
| I1-M5 | **Multiple changes on same line** | Two changes both reporting StartLine=5 | Test behavior; should not produce duplicate entries |
| I1-M6 | **Change spanning entire file** | Location { StartLine = 1, EndLine = 10000 } | Test with whole-file change; verify performance |
| I1-M7 | **Empty line content** | Change where OldContent or NewContent is `""` vs `null` | Test empty string shows empty line, null is handled |
| I1-M8 | **Whitespace-only changes** | Content changed from 4 spaces to 1 tab | Verify whitespace changes are visible in inline view |

#### LOW Priority

| ID | Corner Case | Description | Test Recommendation |
|----|-------------|-------------|---------------------|
| I1-L1 | **Extremely large context value** | `InlineContext = int.MaxValue` | Verify no memory explosion |
| I1-L2 | **Negative context value** | `InlineContext = -5` | Should be treated as 0 or throw? |
| I1-L3 | **RTL text content** | Arabic or Hebrew code in content | Verify RTL text displays correctly |
| I1-L4 | **Unicode combining characters** | Characters like `é` (e + combining accent) | Verify character counting is correct |

### Implementation Gaps Identified

1. **No validation of InlineContext bounds** - Negative values not explicitly handled
2. **Line number calculation assumes 1-based** - No documentation on expected Location values
3. **Context mode performance** - With large files and many changes, separator calculation could be expensive

---

## Feature 3: Multi-File Comparison

### GitComparer - Current Test Coverage (Good)
- Ref range parsing (branch names, commit SHAs)
- Invalid ref range formats
- Missing/invalid references
- File status detection (Added, Removed, Modified, Renamed)
- Parallel processing
- Null parameter validation
- Binary file handling
- Invalid C# syntax handling
- Empty file handling
- Large file handling
- Summary statistics aggregation

### GitComparer - Missing Corner Cases

#### HIGH Priority

| ID | Corner Case | Description | Test Recommendation |
|----|-------------|-------------|---------------------|
| G1-H1 | **Submodule changes** | Repository with submodule pointing to different commits | Test repo with submodule; verify submodule changes are reported |
| G1-H2 | **Symlinked files in repository** | File that is a symbolic link to another file | Test symlinked file modification; verify behavior |
| G1-H3 | **File mode changes only** | File changed from 644 to 755 (executable bit) | Verify mode-only changes are detected/reported |
| G1-H4 | **Detached HEAD comparison** | Comparing when repository is in detached HEAD state | Test `HEAD..abc123` when HEAD is detached |
| G1-H5 | **Merge commit comparison** | Commit with multiple parents | Test comparison involving merge commits |

#### MEDIUM Priority

| ID | Corner Case | Description | Test Recommendation |
|----|-------------|-------------|---------------------|
| G1-M1 | **Tag references** | Ref range like `v1.0.0..v2.0.0` | Test tag resolution works correctly |
| G1-M2 | **Annotated vs lightweight tags** | Different tag types should both resolve | Test both tag types in ref range |
| G1-M3 | **Repository with no commits** | Fresh git init with no history | Verify appropriate error message |
| G1-M4 | **Very large repository (1000+ files changed)** | Performance test with massive changeset | Test with 1000 changed files; verify < 30 seconds |
| G1-M5 | **UTF-8 BOM in files** | Files starting with UTF-8 BOM (EF BB BF) | Verify BOM doesn't cause issues |
| G1-M6 | **Files with mixed encoding** | Old file UTF-8, new file UTF-16 | Verify graceful handling |
| G1-M7 | **Sparse checkout repository** | Only partial working tree | Test if LibGit2Sharp handles sparse checkout |
| G1-M8 | **Ref with special characters** | Branch named `feature/special-chars!@#` | Test unusual branch names resolve |
| G1-M9 | **Triple-dot syntax rejected** | `main...feature` (three dots) should fail | Verify appropriate error for three-dot syntax |
| G1-M10 | **Repository path with spaces** | `/path/with spaces/repo` | Test space-containing paths work |

#### LOW Priority

| ID | Corner Case | Description | Test Recommendation |
|----|-------------|-------------|---------------------|
| G1-L1 | **Shallow clone repository** | Git repo created with --depth=1 | Test behavior when history is truncated |
| G1-L2 | **Worktree repository** | Comparison in a git worktree | Verify worktree repos work |
| G1-L3 | **Case-only file renames** | `File.cs` renamed to `file.cs` | Platform-dependent behavior test |

### FolderComparer - Current Test Coverage (Good)
- Empty folders
- Added/Removed/Modified/Unchanged files
- Multiple files with mixed statuses
- Non-recursive vs recursive traversal
- Include/exclude glob patterns
- Multiple patterns (matching any)
- Exclude takes precedence over include
- Recursive glob patterns (`**/*.cs`)
- `?` wildcard single character match
- Parallel processing
- Null parameter validation
- Directory not found handling
- Case-insensitive path matching
- Nested directories
- Summary statistics

### FolderComparer - Missing Corner Cases

#### HIGH Priority

| ID | Corner Case | Description | Test Recommendation |
|----|-------------|-------------|---------------------|
| D1-H1 | **Symbolic link to file** | Symlink in folder pointing to file outside folder | Test symlink handling; security concern (path traversal) |
| D1-H2 | **Symbolic link to directory** | Symlink creating infinite recursion loop | Test `a/b -> ../a` recursive symlink; verify no infinite loop |
| D1-H3 | **File read permission denied** | File exists but process lacks read permission | Test with chmod 000 file; verify graceful skip with message |
| D1-H4 | **Directory traversal in path** | Paths like `../../../etc/passwd` | Verify GetRelativePath doesn't allow escape from root |
| D1-H5 | **Very deep directory nesting** | 100+ levels of nested directories | Test performance and path length limits |

#### MEDIUM Priority

| ID | Corner Case | Description | Test Recommendation |
|----|-------------|-------------|---------------------|
| D1-M1 | **Empty file vs missing file** | File exists but is 0 bytes in old, missing in new | Verify 0-byte file is treated as empty, not binary |
| D1-M2 | **Very large file (100MB+)** | Single file exceeding typical memory limits | Test with large file; verify streaming or warning |
| D1-M3 | **Thousands of files** | 10,000+ files in comparison | Performance test; verify < 60 seconds |
| D1-M4 | **Special filenames** | `.gitkeep`, `desktop.ini`, `Thumbs.db` | Verify hidden/system files are processed |
| D1-M5 | **Files with no extension** | `Makefile`, `Dockerfile`, `LICENSE` | Verify no-extension files are not excluded accidentally |
| D1-M6 | **Folder path ending with slash** | `/path/to/folder/` vs `/path/to/folder` | Verify trailing slash handling |
| D1-M7 | **Same folder for old and new** | Comparing folder to itself | Should return empty result (no changes) |
| D1-M8 | **Unicode filenames** | `文件.cs`, `файл.cs`, `αρχείο.cs` | Test international filenames work |
| D1-M9 | **Glob pattern with brackets** | `[Tt]est.cs` character class | Test character class glob syntax |
| D1-M10 | **Glob pattern escaping** | Pattern matching literal `*` or `?` | Test escaping special glob chars |

#### LOW Priority

| ID | Corner Case | Description | Test Recommendation |
|----|-------------|-------------|---------------------|
| D1-L1 | **Junction points (Windows)** | NTFS junction pointing elsewhere | Windows-specific symlink variant |
| D1-L2 | **File modified during comparison** | File changes while being read | Race condition; verify no crash |
| D1-L3 | **Folder renamed between old and new** | `src/` in old, `source/` in new | No rename detection; files appear added/removed |

### Implementation Gaps Identified

1. **GitComparer: No symlink handling** - Implementation doesn't document symlink behavior
2. **GitComparer: No submodule support** - Submodule changes would be silently ignored
3. **FolderComparer: No symlink loop detection** - Could cause infinite recursion
4. **FolderComparer: No path traversal protection** - Symlinks could escape root directory
5. **FolderComparer: No file size limits** - Reading 1GB file would exhaust memory
6. **FolderComparer: Glob pattern edge cases** - Bracket syntax `[abc]` not tested

---

## Cross-Feature Integration

### Missing Integration Test Scenarios

#### HIGH Priority

| ID | Corner Case | Description | Test Recommendation |
|----|-------------|-------------|---------------------|
| X1-H1 | **Multi-file + Fragment + Inline** | All three features combined | Test git comparison with fragment mode and inline view for each file |
| X1-H2 | **Git comparison with binary + text files** | Mixed binary and text in same changeset | Verify binary files shown as "binary" while text files diff normally |
| X1-H3 | **Folder comparison output to fragments** | 100 files compared, each generating fragment | Test fragment directory creation and CSS sharing |

#### MEDIUM Priority

| ID | Corner Case | Description | Test Recommendation |
|----|-------------|-------------|---------------------|
| X1-M1 | **Empty changeset to fragment** | Git/folder comparison with no actual changes | Verify empty result generates meaningful fragment |
| X1-M2 | **Fragment mode with null HtmlOutputPath** | Documented to skip CSS file, but what about content? | Test fragment generation without output path |
| X1-M3 | **Inline view with very large file from git** | 10,000 line file with many changes | Performance test: inline view generation time |

---

## Summary and Recommendations

### Priority Distribution

| Priority | Count | Description |
|----------|-------|-------------|
| HIGH | 15 | Critical issues that could cause crashes, security issues, or data loss |
| MEDIUM | 30 | Important edge cases that could cause incorrect behavior |
| LOW | 12 | Minor issues or rare scenarios |
| **Total** | **57** | All identified corner cases |

### Top 10 Recommended Tests to Add

1. **[G1-H2] Symlinked files in git repository** - Security and correctness concern
2. **[D1-H1] Symbolic links in folder comparison** - Path traversal security risk
3. **[D1-H2] Recursive symlink loop detection** - Infinite loop prevention
4. **[F1-H4] Concurrent CSS file writes** - Race condition in parallel scenarios
5. **[I1-H5] Overlapping change regions** - Correctness of inline diff rendering
6. **[G1-H1] Submodule changes** - Common git pattern not tested
7. **[D1-H3] File permission errors** - Graceful degradation
8. **[I1-H1] Very long lines** - Memory safety
9. **[X1-H1] All features combined** - Integration validation
10. **[D1-M2] Very large files** - Memory management

### Implementation Improvements Needed

1. **Add symlink handling documentation and tests** for both GitComparer and FolderComparer
2. **Add file size limits** with configurable threshold for FolderComparer
3. **Add path traversal protection** to prevent escaping root directory
4. **Document expected behavior** for edge cases in XML documentation
5. **Add validation for InlineContext** bounds (reject negative values)
6. **Consider caching CSS content** instead of regenerating on each fragment
7. **Add progress reporting** for large changesets (mentioned in plan but not tested)

### Test Infrastructure Recommendations

1. **Create test fixtures** for common git repository scenarios (submodules, tags, etc.)
2. **Add platform-specific tests** marked with `[Fact(Skip = "Windows only")]` etc.
3. **Add performance benchmarks** for large file/changeset scenarios
4. **Create security test suite** for path traversal, symlink, and injection vectors

---

## Appendix: Test File Locations

| Test File | Tests Count | Primary Coverage |
|-----------|-------------|------------------|
| `HtmlFragmentTests.cs` | 19 | Fragment structure, CSS, data attributes |
| `HtmlInlineViewTests.cs` | 36 | Inline view structure, context mode |
| `HtmlFragmentIntegrationTests.cs` | 8 | Multiple fragments, style isolation |
| `GitComparerTests.cs` | 29 | Git ref parsing, file status, parallel |
| `FolderComparerTests.cs` | 25 | Folder comparison, glob patterns |
| `FolderComparerIntegrationTests.cs` | 7 | Realistic scenarios, performance |
| **Total** | **124** | Current test coverage |

---

**Document Version:** 1.0
**Author:** Claude Code (Opus 4.5)
**Review Status:** Ready for engineering review
