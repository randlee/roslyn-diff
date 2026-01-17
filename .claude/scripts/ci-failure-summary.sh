#!/bin/bash
# CI Failure Summary Script
# Extracts concise failure information from GitHub Actions logs
# Usage: ./ci-failure-summary.sh <PR_NUMBER|RUN_ID> [--verbose]

set -e

# Colors for output
RED='\033[0;31m'
YELLOW='\033[1;33m'
GREEN='\033[0;32m'
CYAN='\033[0;36m'
BOLD='\033[1m'
NC='\033[0m' # No Color

VERBOSE=false
INPUT="$1"

if [[ "$2" == "--verbose" ]]; then
    VERBOSE=true
fi

if [[ -z "$INPUT" ]]; then
    echo -e "${RED}Usage: $0 <PR_NUMBER|RUN_ID> [--verbose]${NC}"
    echo "  PR_NUMBER: e.g., 29"
    echo "  RUN_ID: e.g., 12345678"
    exit 1
fi

# Determine if input is PR number or run ID
if [[ "$INPUT" =~ ^[0-9]+$ ]] && [[ ${#INPUT} -lt 6 ]]; then
    # Likely a PR number (< 6 digits)
    PR_NUM="$INPUT"
    echo -e "${CYAN}=== CI Failure Summary for PR #${PR_NUM} ===${NC}\n"

    # Get PR info
    PR_INFO=$(gh pr view "$PR_NUM" --json title,headRefName,baseRefName,state 2>/dev/null || echo "{}")
    if [[ "$PR_INFO" != "{}" ]]; then
        TITLE=$(echo "$PR_INFO" | jq -r '.title')
        HEAD=$(echo "$PR_INFO" | jq -r '.headRefName')
        BASE=$(echo "$PR_INFO" | jq -r '.baseRefName')
        echo -e "${BOLD}PR:${NC} $TITLE"
        echo -e "${BOLD}Branch:${NC} $HEAD → $BASE\n"
    fi

    # Get check status
    echo -e "${BOLD}Check Status:${NC}"
    gh pr checks "$PR_NUM" 2>/dev/null | while read -r line; do
        if echo "$line" | grep -q "fail\|X"; then
            echo -e "  ${RED}✗${NC} $line"
        elif echo "$line" | grep -q "pass\|✓"; then
            echo -e "  ${GREEN}✓${NC} $line"
        else
            echo "  $line"
        fi
    done
    echo ""

    # Get the failed run ID
    RUN_ID=$(gh run list --branch "$(echo "$PR_INFO" | jq -r '.headRefName')" --limit 1 --json databaseId,conclusion --jq '.[0].databaseId' 2>/dev/null)
else
    # Assume it's a run ID
    RUN_ID="$INPUT"
    echo -e "${CYAN}=== CI Failure Summary for Run #${RUN_ID} ===${NC}\n"
fi

if [[ -z "$RUN_ID" ]]; then
    echo -e "${YELLOW}No recent runs found${NC}"
    exit 0
fi

# Get run status
RUN_INFO=$(gh run view "$RUN_ID" --json conclusion,status,jobs 2>/dev/null || echo "{}")
CONCLUSION=$(echo "$RUN_INFO" | jq -r '.conclusion // "in_progress"')
STATUS=$(echo "$RUN_INFO" | jq -r '.status')

echo -e "${BOLD}Run #${RUN_ID}:${NC} $STATUS ($CONCLUSION)\n"

# List failed jobs
echo -e "${BOLD}Failed Jobs:${NC}"
FAILED_JOBS=$(echo "$RUN_INFO" | jq -r '.jobs[] | select(.conclusion == "failure") | .name' 2>/dev/null)
if [[ -z "$FAILED_JOBS" ]]; then
    echo -e "  ${GREEN}No failed jobs${NC}\n"
else
    echo "$FAILED_JOBS" | while read -r job; do
        echo -e "  ${RED}✗${NC} $job"
    done
    echo ""
fi

# Get failed logs and parse them
echo -e "${BOLD}Failure Details:${NC}"
FAILED_LOG=$(gh run view "$RUN_ID" --log-failed 2>/dev/null || echo "")

if [[ -z "$FAILED_LOG" ]]; then
    echo -e "  ${GREEN}No failure logs available${NC}"
    exit 0
fi

# Save raw log for verbose mode
if [[ "$VERBOSE" == true ]]; then
    echo "$FAILED_LOG" > /tmp/ci-failure-raw.log
    echo -e "  ${CYAN}Raw log saved to /tmp/ci-failure-raw.log${NC}\n"
fi

# Extract .NET test failures
echo -e "\n${YELLOW}── Test Failures ──${NC}"
TEST_FAILURES=$(echo "$FAILED_LOG" | grep -E "Failed\s+\w+|✗.*\[FAIL\]|\[xUnit.*\].*Failed" | head -20 || true)
if [[ -n "$TEST_FAILURES" ]]; then
    echo "$TEST_FAILURES" | while read -r line; do
        # Clean up the line - remove timestamps and job prefixes
        CLEAN=$(echo "$line" | sed 's/^.*\t//' | sed 's/^[0-9T:.-]*Z //')
        echo -e "  ${RED}✗${NC} $CLEAN"
    done
else
    echo -e "  ${GREEN}No test failures found${NC}"
fi

# Extract assertion errors (expected vs actual)
echo -e "\n${YELLOW}── Assertion Details ──${NC}"
ASSERTIONS=$(echo "$FAILED_LOG" | grep -iE "Expected.*to be|Expected.*but found|Actual:|expected.*got|should be.*but|difference of" | grep -v "Passed" | head -15 || true)
if [[ -n "$ASSERTIONS" ]]; then
    echo "$ASSERTIONS" | while read -r line; do
        CLEAN=$(echo "$line" | sed 's/^.*\t//' | sed 's/^[0-9T:.-]*Z //')
        echo "  $CLEAN"
    done
else
    echo -e "  ${GREEN}No assertion details found${NC}"
fi

# Extract build errors
echo -e "\n${YELLOW}── Build Errors ──${NC}"
BUILD_ERRORS=$(echo "$FAILED_LOG" | grep -E "error CS[0-9]+:|error NU[0-9]+:|error MSB[0-9]+:" | head -10 || true)
if [[ -n "$BUILD_ERRORS" ]]; then
    echo "$BUILD_ERRORS" | while read -r line; do
        CLEAN=$(echo "$line" | sed 's/^.*\t//' | sed 's/^[0-9T:.-]*Z //')
        echo -e "  ${RED}$CLEAN${NC}"
    done
else
    echo -e "  ${GREEN}No build errors found${NC}"
fi

# Extract timeout/performance issues
echo -e "\n${YELLOW}── Timeouts/Performance ──${NC}"
TIMEOUTS=$(echo "$FAILED_LOG" | grep -iE "timeout|timed out|exceeded|too slow|milliseconds|elapsed" | grep -iE "fail|error|exceed" | grep -v "Passed" | head -10 || true)
if [[ -n "$TIMEOUTS" ]]; then
    echo "$TIMEOUTS" | while read -r line; do
        CLEAN=$(echo "$line" | sed 's/^.*\t//' | sed 's/^[0-9T:.-]*Z //')
        echo -e "  ${YELLOW}$CLEAN${NC}"
    done
else
    echo -e "  ${GREEN}No timeout issues found${NC}"
fi

# Extract exception stack traces (just the exception line, not full stack)
echo -e "\n${YELLOW}── Exceptions ──${NC}"
EXCEPTIONS=$(echo "$FAILED_LOG" | grep -E "Exception:|Error:|System\.\w+Exception" | grep -v "^[[:space:]]*at " | head -10 || true)
if [[ -n "$EXCEPTIONS" ]]; then
    echo "$EXCEPTIONS" | while read -r line; do
        CLEAN=$(echo "$line" | sed 's/^.*\t//' | sed 's/^[0-9T:.-]*Z //')
        echo -e "  ${RED}$CLEAN${NC}"
    done
else
    echo -e "  ${GREEN}No exceptions found${NC}"
fi

echo -e "\n${CYAN}=== End Summary ===${NC}"
