# LineDiff Golden Snapshots

This directory contains test fixtures for comparing roslyn-diff line-diff output against standard diff tools.

## Directory Structure

For each test scenario, there are four files:

- `{Scenario}_old.txt` - The original file content (before changes)
- `{Scenario}_new.txt` - The modified file content (after changes)
- `{Scenario}_diff_u.txt` - Output from `diff -u old.txt new.txt`
- `{Scenario}_git_diff.txt` - Output from `git diff --no-index old.txt new.txt`

## Test Scenarios

### SimpleAddition
Add lines to a file. Tests the basic case of appending new content at the end.

### SimpleDeletion
Remove lines from a file. Tests detecting and reporting removed lines from the middle of a file.

### SimpleModification
Change lines in a file. Tests detecting when lines are modified (replaced) in place.

### MultipleHunks
Changes in different parts of a file (non-contiguous). Tests that separate hunks are correctly identified when changes are far apart in the file.

### WhitespaceChanges
Whitespace-only modifications. Tests detection of:
- Tab vs. spaces changes
- Multiple spaces collapsed to single space
- Trailing whitespace changes

### EmptyToContent
Empty file to file with content. Tests the edge case of going from zero lines to multiple lines.

### ContentToEmpty
File with content to empty file. Tests the edge case of removing all content from a file.

### LargeContext
Changes surrounded by many unchanged lines. Tests that context lines are properly included/excluded around a small change in a larger file.

### AdjacentChanges
Multiple changes close together. Tests that consecutive changed lines are grouped together in the same hunk.

### MixedChanges
Additions, deletions, and modifications in one file. Tests the complex case where all three types of changes occur together:
- Lines deleted at the top
- Lines modified in multiple places
- Lines inserted in the middle
- Lines appended at the end

## Generating Golden Snapshots

To regenerate the golden snapshots after modifying input files:

```bash
cd tests/RoslynDiff.Integration.Tests/GoldenSnapshots/LineDiff

# For each scenario:
diff -u {Scenario}_old.txt {Scenario}_new.txt > {Scenario}_diff_u.txt || true
git diff --no-index {Scenario}_old.txt {Scenario}_new.txt > {Scenario}_git_diff.txt || true
```

Note: `|| true` is needed because diff tools return exit code 1 when files differ.

## Usage in Tests

Tests should:
1. Load the `_old.txt` and `_new.txt` files
2. Run roslyn-diff line comparison
3. Compare output against the golden snapshot files to verify correctness

The golden snapshots provide a baseline for expected diff output format and content.
