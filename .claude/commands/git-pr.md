---
name: git-pr
version: 0.1.0
description: Show outstanding PRs and their CI status in a formatted table.
---

# /git-pr command

Display GitHub PR status for this repository.

## Usage
```
/git-pr [--open|--all|--merged]
```

## Flags
- `--open` (default): Show open PRs only
- `--all`: Show all PRs (open, merged, closed)
- `--merged`: Show recently merged PRs

## Output

!python3 .claude/scripts/git-pr-status.py

## Actions

After reviewing the PR table, you can:
1. **Check a specific PR**: `/git-pr 29` - Show details for PR #29
2. **Check CI failures**: Run `.claude/scripts/ci-failure-summary.sh <PR#>` for detailed failure analysis
3. **Merge a PR**: `gh pr merge <PR#> --squash`
