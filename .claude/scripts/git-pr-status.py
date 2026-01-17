#!/usr/bin/env python3
"""
Git PR Status Script
Outputs a markdown table of open PRs with their CI status.
Usage: python git-pr-status.py [--open|--all|--merged]
"""

import json
import subprocess
import sys
from datetime import datetime


def run_gh(args: list[str]) -> str:
    """Run a gh CLI command and return output."""
    try:
        result = subprocess.run(
            ["gh"] + args,
            capture_output=True,
            text=True,
            check=True
        )
        return result.stdout
    except subprocess.CalledProcessError:
        return "[]"
    except FileNotFoundError:
        print("Error: gh CLI not found. Install from https://cli.github.com/", file=sys.stderr)
        sys.exit(1)


def get_check_status(checks: list | None) -> str:
    """Parse check rollup and return status string."""
    if not checks:
        return "—"

    total = len(checks)
    passed = sum(1 for c in checks if c.get("conclusion", "").upper() in ("SUCCESS",))
    failed = sum(1 for c in checks if c.get("conclusion", "").upper() in ("FAILURE",))
    pending = sum(1 for c in checks if not c.get("conclusion") or c.get("status", "").upper() in ("IN_PROGRESS", "QUEUED"))

    if failed > 0:
        return f"❌ {failed} fail"
    elif pending > 0:
        return f"⏳ {pending} pending"
    elif passed > 0:
        return f"✅ {passed} pass"
    return "—"


def get_state_display(state: str, is_draft: bool) -> tuple[str, str]:
    """Return (icon, text) for PR state."""
    if is_draft:
        return "📝", "Draft"
    elif state == "MERGED":
        return "✅", "Merged"
    elif state == "CLOSED":
        return "❌", "Closed"
    return "🔓", "Open"


def truncate(text: str, max_len: int) -> str:
    """Truncate text with ellipsis if too long."""
    if len(text) > max_len:
        return text[:max_len-3] + "..."
    return text


def main():
    # Parse arguments
    filter_arg = sys.argv[1] if len(sys.argv) > 1 else "--open"

    state_map = {
        "--all": ("all", "All PRs"),
        "--merged": ("merged", "Merged PRs"),
        "--open": ("open", "Open PRs"),
    }

    state_filter, title = state_map.get(filter_arg, ("open", "Open PRs"))

    # Fetch PR data
    pr_json = run_gh([
        "pr", "list",
        "--state", state_filter,
        "--json", "number,title,headRefName,baseRefName,state,isDraft,statusCheckRollup,updatedAt",
        "--limit", "20"
    ])

    try:
        prs = json.loads(pr_json)
    except json.JSONDecodeError:
        prs = []

    # Output header
    print(f"## {title}")
    print()

    if not prs:
        print("_No PRs found_")
        return

    print("| # | Title | Branch | Status | Checks | Updated |")
    print("|---|-------|--------|--------|--------|---------|")

    # Output each PR
    for pr in prs:
        num = pr.get("number", "?")
        title_text = truncate(pr.get("title", ""), 40)
        head = truncate(pr.get("headRefName", ""), 20)
        base = pr.get("baseRefName", "")
        state = pr.get("state", "OPEN")
        is_draft = pr.get("isDraft", False)
        updated = pr.get("updatedAt", "")[:10]

        state_icon, state_text = get_state_display(state, is_draft)
        check_status = get_check_status(pr.get("statusCheckRollup"))

        print(f"| [#{num}](../../pull/{num}) | {title_text} | `{head}` → `{base}` | {state_icon} {state_text} | {check_status} | {updated} |")

    print()
    print(f"_Generated: {datetime.now().strftime('%Y-%m-%d %H:%M:%S')}_")


if __name__ == "__main__":
    main()
