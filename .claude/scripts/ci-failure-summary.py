#!/usr/bin/env python3
"""
CI Failure Summary Script
Extracts concise failure information from GitHub Actions logs.
Usage: python ci-failure-summary.py <PR_NUMBER|RUN_ID> [--verbose]
"""

import json
import re
import subprocess
import sys
from dataclasses import dataclass


# ANSI colors
RED = "\033[0;31m"
YELLOW = "\033[1;33m"
GREEN = "\033[0;32m"
CYAN = "\033[0;36m"
BOLD = "\033[1m"
NC = "\033[0m"


@dataclass
class FailureInfo:
    test_failures: list[str]
    assertion_details: list[str]
    build_errors: list[str]
    timeouts: list[str]
    exceptions: list[str]


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
    except subprocess.CalledProcessError as e:
        return e.stdout or ""
    except FileNotFoundError:
        print("Error: gh CLI not found", file=sys.stderr)
        sys.exit(1)


def parse_failures(log: str) -> FailureInfo:
    """Parse failure log and extract relevant information."""
    lines = log.split("\n")

    # Test failures
    test_pattern = re.compile(r"Failed\s+\S+|✗.*\[FAIL\]|\[xUnit.*\].*Failed", re.IGNORECASE)
    test_failures = [
        clean_line(line) for line in lines
        if test_pattern.search(line)
    ][:20]

    # Assertion details
    assertion_pattern = re.compile(
        r"Expected.*to be|Expected.*but found|expected.*got|should be.*but|difference of",
        re.IGNORECASE
    )
    assertion_details = [
        clean_line(line) for line in lines
        if assertion_pattern.search(line) and "Passed" not in line
    ][:15]

    # Build errors
    build_pattern = re.compile(r"error CS\d+:|error NU\d+:|error MSB\d+:")
    build_errors = [
        clean_line(line) for line in lines
        if build_pattern.search(line)
    ][:10]

    # Timeouts
    timeout_pattern = re.compile(r"timeout|timed out|exceeded|too slow", re.IGNORECASE)
    fail_pattern = re.compile(r"fail|error|exceed", re.IGNORECASE)
    timeouts = [
        clean_line(line) for line in lines
        if timeout_pattern.search(line) and fail_pattern.search(line) and "Passed" not in line
    ][:10]

    # Exceptions
    exception_pattern = re.compile(r"Exception:|Error:|System\.\w+Exception")
    exceptions = [
        clean_line(line) for line in lines
        if exception_pattern.search(line) and not line.strip().startswith("at ")
    ][:10]

    return FailureInfo(
        test_failures=test_failures,
        assertion_details=assertion_details,
        build_errors=build_errors,
        timeouts=timeouts,
        exceptions=exceptions
    )


def clean_line(line: str) -> str:
    """Remove timestamps and job prefixes from log line."""
    # Remove tab-prefixed content
    if "\t" in line:
        line = line.split("\t")[-1]
    # Remove ISO timestamps
    line = re.sub(r"^\d{4}-\d{2}-\d{2}T[\d:.]+Z\s*", "", line)
    return line.strip()


def print_section(title: str, items: list[str], color: str = YELLOW, item_color: str = RED):
    """Print a section with items."""
    print(f"\n{color}── {title} ──{NC}")
    if items:
        for item in items:
            print(f"  {item_color}✗{NC} {item}")
    else:
        print(f"  {GREEN}No {title.lower()} found{NC}")


def main():
    if len(sys.argv) < 2:
        print(f"{RED}Usage: {sys.argv[0]} <PR_NUMBER|RUN_ID> [--verbose]{NC}")
        print("  PR_NUMBER: e.g., 29")
        print("  RUN_ID: e.g., 12345678")
        sys.exit(1)

    input_arg = sys.argv[1]
    verbose = "--verbose" in sys.argv

    # Determine if input is PR number or run ID
    run_id = None
    if input_arg.isdigit() and len(input_arg) < 6:
        # Likely a PR number
        pr_num = input_arg
        print(f"{CYAN}=== CI Failure Summary for PR #{pr_num} ==={NC}\n")

        # Get PR info
        pr_json = run_gh(["pr", "view", pr_num, "--json", "title,headRefName,baseRefName,state"])
        try:
            pr_info = json.loads(pr_json)
            print(f"{BOLD}PR:{NC} {pr_info.get('title', 'Unknown')}")
            print(f"{BOLD}Branch:{NC} {pr_info.get('headRefName', '?')} → {pr_info.get('baseRefName', '?')}\n")

            # Get check status
            print(f"{BOLD}Check Status:{NC}")
            checks_output = run_gh(["pr", "checks", pr_num])
            for line in checks_output.strip().split("\n"):
                if "fail" in line.lower() or "X" in line:
                    print(f"  {RED}✗{NC} {line}")
                elif "pass" in line.lower() or "✓" in line:
                    print(f"  {GREEN}✓{NC} {line}")
                else:
                    print(f"  {line}")
            print()

            # Get the failed run ID
            head_branch = pr_info.get("headRefName", "")
            runs_json = run_gh(["run", "list", "--branch", head_branch, "--limit", "1", "--json", "databaseId,conclusion"])
            runs = json.loads(runs_json)
            if runs:
                run_id = str(runs[0].get("databaseId"))
        except json.JSONDecodeError:
            print(f"{YELLOW}Could not parse PR info{NC}")
    else:
        run_id = input_arg
        print(f"{CYAN}=== CI Failure Summary for Run #{run_id} ==={NC}\n")

    if not run_id:
        print(f"{YELLOW}No recent runs found{NC}")
        return

    # Get run info
    run_json = run_gh(["run", "view", run_id, "--json", "conclusion,status,jobs"])
    try:
        run_info = json.loads(run_json)
        conclusion = run_info.get("conclusion") or "in_progress"
        status = run_info.get("status", "unknown")
        print(f"{BOLD}Run #{run_id}:{NC} {status} ({conclusion})\n")

        # List failed jobs
        print(f"{BOLD}Failed Jobs:{NC}")
        jobs = run_info.get("jobs", [])
        failed_jobs = [j.get("name") for j in jobs if j.get("conclusion") == "failure"]
        if failed_jobs:
            for job in failed_jobs:
                print(f"  {RED}✗{NC} {job}")
        else:
            print(f"  {GREEN}No failed jobs{NC}")
        print()
    except json.JSONDecodeError:
        pass

    # Get failed logs
    print(f"{BOLD}Failure Details:{NC}")
    failed_log = run_gh(["run", "view", run_id, "--log-failed"])

    if not failed_log.strip():
        print(f"  {GREEN}No failure logs available{NC}")
        return

    if verbose:
        log_path = "/tmp/ci-failure-raw.log"
        with open(log_path, "w") as f:
            f.write(failed_log)
        print(f"  {CYAN}Raw log saved to {log_path}{NC}")

    # Parse and display failures
    failures = parse_failures(failed_log)

    print_section("Test Failures", failures.test_failures)
    print_section("Assertion Details", failures.assertion_details, item_color=NC)
    print_section("Build Errors", failures.build_errors)
    print_section("Timeouts/Performance", failures.timeouts, item_color=YELLOW)
    print_section("Exceptions", failures.exceptions)

    print(f"\n{CYAN}=== End Summary ==={NC}")


if __name__ == "__main__":
    main()
