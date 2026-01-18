---
allowed-tools: Bash(python3 .claude/scripts/git-pr-status.py*)
name: git-pr
version: 0.6.0
description: Show outstanding PRs and their CI status in a formatted table.
---

# /git-pr command

Display GitHub PR status for this repository.

## Usage
```
/git-pr [--open|--all|--merged] [--fix [instructions]]
```

## Context

- PR status: !`python3 .claude/scripts/git-pr-status.py $ARGUMENTS`

## Flags
- `--open` (default): Show open PRs only
- `--all`: Show all PRs (open, merged, closed)
- `--merged`: Show recently merged PRs
- `--fix`: If PR is failing, automatically attempt to fix (can include additional instructions)
- Any other text after the flags is ignored by the script but remains visible to Claude for context.

## Instructions

1. Use the PR status table from the Context section. Only run `python3 .claude/scripts/git-pr-status.py [flags]` if the Context is missing.
2. Process the output according to the Response Handling rules below

## Response Handling

After receiving the PR table, follow these rules:

### All PRs Passing
If all open PRs show passing CI (✅):
1. Output: `All PRs passing.`
2. If you know which PR is currently active (from conversation context, current branch, or recent work), include the full GitHub URL: `https://github.com/{owner}/{repo}/pull/{number}`

### PR Failing (no --fix flag)
If any PR shows failing CI (❌) and no `--fix` flag was provided:
1. Output: `PR #{number} failing. Investigating...`
2. If you know which PR is active/relevant, launch a **background** `ci-root-cause-agent` to analyze the failure
3. If you do NOT know which PR is active, ask the user which PR to investigate

### PR Failing (with --fix flag)
If any PR shows failing CI (❌) and `--fix` flag was provided:
1. Output: `PR #{number} failing. Attempting fix...`
2. If you know which PR is active/relevant, launch a **background** `ci-fix-agent` to fix the issue
3. Include any user-provided instructions after `--fix` in the agent prompt
4. If you do NOT know which PR is active, ask the user which PR to fix

### Unknown Active PR
If you cannot determine which PR the user is working on:
- Do NOT launch background agents automatically
- Ask the user: "Which PR would you like me to investigate/fix?"

## Manual Actions

You can also:
1. **Check a specific PR**: `/git-pr 29` - Show details for PR #29
2. **Check CI failures**: Run `python3 .claude/scripts/ci-failure-summary.py <PR#>` for detailed failure analysis
3. **Merge a PR**: `gh pr merge <PR#> --squash`
