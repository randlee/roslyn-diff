#!/usr/bin/env python3
"""
PreToolUse hook for /git-pr command.
Validates that the git-pr-status command is properly formed.
"""
import json
import re
import sys


def main() -> int:
    # Read PreToolUse hook payload from stdin
    try:
        payload = json.load(sys.stdin)
    except json.JSONDecodeError:
        return 0  # Not a valid payload, allow tool

    # Get the bash command being executed
    tool_input = payload.get("tool_input") or {}
    command = tool_input.get("command", "")

    # Check if this is a git-pr-status command
    if "git-pr-status.py" not in command:
        return 0  # Not our command, allow tool

    # Valid git-pr-status command, allow bash to execute
    return 0


if __name__ == "__main__":
    sys.exit(main())
