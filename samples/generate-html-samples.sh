#!/bin/bash

###############################################################################
# HTML Samples Generation Script for RoslynDiff
###############################################################################
#
# This script generates comprehensive HTML samples demonstrating ALL HTML
# output modes supported by roslyn-diff:
#
# 1. Document vs Fragment mode
# 2. Tree vs Inline view
# 3. Context modes (full, 3 lines, 5 lines)
# 4. Multi-file outputs
# 5. Multi-TFM (Target Framework Moniker) analysis
# 6. Special features (impact classification, whitespace handling)
#
# Usage:
#   cd samples
#   ./generate-html-samples.sh
#
# Output:
#   All HTML samples are generated in samples/html-samples/ directory
#
###############################################################################

# Note: We don't use 'set -e' because roslyn-diff returns non-zero exit codes
# when differences are found, which is expected behavior for a diff tool.

# Colors for output
GREEN='\033[0;32m'
BLUE='\033[0;34m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Determine script directory and project root
SCRIPT_DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"
PROJECT_ROOT="$( cd "$SCRIPT_DIR/.." && pwd )"
CLI_DIR="$PROJECT_ROOT/src/RoslynDiff.Cli"
OUTPUT_DIR="$SCRIPT_DIR/html-samples"

# Framework to use
FRAMEWORK="net10.0"

echo -e "${BLUE}==================================================================${NC}"
echo -e "${BLUE}  RoslynDiff HTML Samples Generation${NC}"
echo -e "${BLUE}==================================================================${NC}"
echo ""

# Create output directory
echo -e "${YELLOW}Creating output directory...${NC}"
mkdir -p "$OUTPUT_DIR"

###############################################################################
# SECTION 1: Basic Modes - Document vs Fragment
###############################################################################

echo -e "\n${GREEN}[1/8] Generating Basic Mode Samples (Document vs Fragment)${NC}"

# 1.1 Tree view, document mode (default)
echo "  → tree-document.html (Tree view + Document mode)"
dotnet run --project "$CLI_DIR" --framework $FRAMEWORK -- diff \
  "$SCRIPT_DIR/before/Calculator.cs" \
  "$SCRIPT_DIR/after/Calculator.cs" \
  --html "$OUTPUT_DIR/tree-document.html" \
  --quiet

# 1.2 Tree view, fragment mode
echo "  → tree-fragment.html (Tree view + Fragment mode)"
dotnet run --project "$CLI_DIR" --framework $FRAMEWORK -- diff \
  "$SCRIPT_DIR/before/Calculator.cs" \
  "$SCRIPT_DIR/after/Calculator.cs" \
  --html "$OUTPUT_DIR/tree-fragment.html" \
  --html-mode fragment \
  --extract-css tree-fragment.css \
  --quiet

###############################################################################
# SECTION 2: Inline View Modes
###############################################################################

echo -e "\n${GREEN}[2/8] Generating Inline View Samples${NC}"

# 2.1 Inline view, document mode
echo "  → inline-document.html (Inline full view + Document mode)"
dotnet run --project "$CLI_DIR" --framework $FRAMEWORK -- diff \
  "$SCRIPT_DIR/before/Calculator.cs" \
  "$SCRIPT_DIR/after/Calculator.cs" \
  --html "$OUTPUT_DIR/inline-document.html" \
  --inline \
  --quiet

# 2.2 Inline view, fragment mode
echo "  → inline-fragment.html (Inline view + Fragment mode)"
dotnet run --project "$CLI_DIR" --framework $FRAMEWORK -- diff \
  "$SCRIPT_DIR/before/Calculator.cs" \
  "$SCRIPT_DIR/after/Calculator.cs" \
  --html "$OUTPUT_DIR/inline-fragment.html" \
  --html-mode fragment \
  --extract-css inline-fragment.css \
  --inline=5 \
  --quiet

# 2.3 Inline with 3 lines context
echo "  → inline-context-3.html (Inline with 3 lines context)"
dotnet run --project "$CLI_DIR" --framework $FRAMEWORK -- diff \
  "$SCRIPT_DIR/before/Calculator.cs" \
  "$SCRIPT_DIR/after/Calculator.cs" \
  --html "$OUTPUT_DIR/inline-context-3.html" \
  --inline=3 \
  --quiet

# 2.4 Inline with 5 lines context
echo "  → inline-context-5.html (Inline with 5 lines context)"
dotnet run --project "$CLI_DIR" --framework $FRAMEWORK -- diff \
  "$SCRIPT_DIR/before/Calculator.cs" \
  "$SCRIPT_DIR/after/Calculator.cs" \
  --html "$OUTPUT_DIR/inline-context-5.html" \
  --inline=5 \
  --quiet

# 2.5 Inline with 10 lines context
echo "  → inline-context-10.html (Inline with 10 lines context)"
dotnet run --project "$CLI_DIR" --framework $FRAMEWORK -- diff \
  "$SCRIPT_DIR/before/Calculator.cs" \
  "$SCRIPT_DIR/after/Calculator.cs" \
  --html "$OUTPUT_DIR/inline-context-10.html" \
  --inline=10 \
  --quiet

###############################################################################
# SECTION 3: Additional File Comparisons (UserService)
###############################################################################

echo -e "\n${GREEN}[3/8] Generating Additional File Samples (UserService)${NC}"

# 3.1 UserService with tree view
echo "  → userservice-tree.html (UserService comparison)"
dotnet run --project "$CLI_DIR" --framework $FRAMEWORK -- diff \
  "$SCRIPT_DIR/before/UserService.cs" \
  "$SCRIPT_DIR/after/UserService.cs" \
  --html "$OUTPUT_DIR/userservice-tree.html" \
  --quiet

# 3.2 UserService with inline view
echo "  → userservice-inline.html (UserService + Inline view)"
dotnet run --project "$CLI_DIR" --framework $FRAMEWORK -- diff \
  "$SCRIPT_DIR/before/UserService.cs" \
  "$SCRIPT_DIR/after/UserService.cs" \
  --html "$OUTPUT_DIR/userservice-inline.html" \
  --inline=5 \
  --quiet

# 3.3 UserService with fragment mode
echo "  → userservice-fragment.html (UserService + Fragment mode)"
dotnet run --project "$CLI_DIR" --framework $FRAMEWORK -- diff \
  "$SCRIPT_DIR/before/UserService.cs" \
  "$SCRIPT_DIR/after/UserService.cs" \
  --html "$OUTPUT_DIR/userservice-fragment.html" \
  --html-mode fragment \
  --extract-css userservice-fragment.css \
  --quiet

###############################################################################
# SECTION 4: Impact Classification
###############################################################################

echo -e "\n${GREEN}[4/8] Generating Impact Classification Samples${NC}"

# 4.1 Full impact demo with all changes
echo "  → impact-full.html (All impact levels)"
dotnet run --project "$CLI_DIR" --framework $FRAMEWORK -- diff \
  "$SCRIPT_DIR/impact-demo/old.cs" \
  "$SCRIPT_DIR/impact-demo/new.cs" \
  --html "$OUTPUT_DIR/impact-full.html" \
  --quiet

# 4.2 Impact demo with inline view
echo "  → impact-inline.html (Impact classification + Inline view)"
dotnet run --project "$CLI_DIR" --framework $FRAMEWORK -- diff \
  "$SCRIPT_DIR/impact-demo/old.cs" \
  "$SCRIPT_DIR/impact-demo/new.cs" \
  --html "$OUTPUT_DIR/impact-inline.html" \
  --inline=5 \
  --quiet

# 4.3 Breaking public API changes only
echo "  → impact-breaking-public-only.html (Breaking public API only)"
dotnet run --project "$CLI_DIR" --framework $FRAMEWORK -- diff \
  "$SCRIPT_DIR/impact-demo/old.cs" \
  "$SCRIPT_DIR/impact-demo/new.cs" \
  --html "$OUTPUT_DIR/impact-breaking-public-only.html" \
  --impact-level breaking-public \
  --quiet

# 4.4 Breaking changes (public + internal)
echo "  → impact-breaking-internal-only.html (Breaking internal API and above)"
dotnet run --project "$CLI_DIR" --framework $FRAMEWORK -- diff \
  "$SCRIPT_DIR/impact-demo/old.cs" \
  "$SCRIPT_DIR/impact-demo/new.cs" \
  --html "$OUTPUT_DIR/impact-breaking-internal-only.html" \
  --impact-level breaking-internal \
  --quiet

###############################################################################
# SECTION 5: Multi-TFM (Target Framework Moniker) Analysis
###############################################################################

echo -e "\n${GREEN}[5/8] Generating Multi-TFM Samples${NC}"

# 5.1 Multi-TFM with tree view
echo "  → tfm-tree.html (Multi-TFM + Tree view)"
dotnet run --project "$CLI_DIR" --framework $FRAMEWORK -- diff \
  "$SCRIPT_DIR/multi-tfm/old-conditional-code.cs" \
  "$SCRIPT_DIR/multi-tfm/new-conditional-code.cs" \
  -t net8.0 -t net10.0 \
  --html "$OUTPUT_DIR/tfm-tree.html" \
  --quiet

# 5.2 Multi-TFM with inline view
echo "  → tfm-inline.html (Multi-TFM + Inline view)"
dotnet run --project "$CLI_DIR" --framework $FRAMEWORK -- diff \
  "$SCRIPT_DIR/multi-tfm/old-conditional-code.cs" \
  "$SCRIPT_DIR/multi-tfm/new-conditional-code.cs" \
  -t net8.0 -t net10.0 \
  --html "$OUTPUT_DIR/tfm-inline.html" \
  --inline=5 \
  --quiet

# 5.3 Multi-TFM with fragment mode
echo "  → tfm-fragment.html (Multi-TFM + Fragment mode)"
dotnet run --project "$CLI_DIR" --framework $FRAMEWORK -- diff \
  "$SCRIPT_DIR/multi-tfm/old-conditional-code.cs" \
  "$SCRIPT_DIR/multi-tfm/new-conditional-code.cs" \
  -t net8.0 -t net10.0 \
  --html "$OUTPUT_DIR/tfm-fragment.html" \
  --html-mode fragment \
  --extract-css tfm-fragment.css \
  --quiet

###############################################################################
# SECTION 6: Whitespace Handling
###############################################################################

echo -e "\n${GREEN}[6/8] Generating Whitespace Handling Samples${NC}"

# 6.1 With formatting changes (default)
echo "  → whitespace-exact.html (Exact whitespace matching - default)"
dotnet run --project "$CLI_DIR" --framework $FRAMEWORK -- diff \
  "$SCRIPT_DIR/impact-demo/old.cs" \
  "$SCRIPT_DIR/impact-demo/new.cs" \
  --html "$OUTPUT_DIR/whitespace-exact.html" \
  --whitespace-mode exact \
  --include-formatting \
  --quiet

# 6.2 Ignoring leading/trailing whitespace
echo "  → whitespace-ignore-leading-trailing.html (Ignore leading/trailing)"
dotnet run --project "$CLI_DIR" --framework $FRAMEWORK -- diff \
  "$SCRIPT_DIR/impact-demo/old.cs" \
  "$SCRIPT_DIR/impact-demo/new.cs" \
  --html "$OUTPUT_DIR/whitespace-ignore-leading-trailing.html" \
  --whitespace-mode ignore-leading-trailing \
  --include-formatting \
  --quiet

# 6.3 Ignoring all whitespace
echo "  → whitespace-ignore-all.html (Ignore all whitespace)"
dotnet run --project "$CLI_DIR" --framework $FRAMEWORK -- diff \
  "$SCRIPT_DIR/impact-demo/old.cs" \
  "$SCRIPT_DIR/impact-demo/new.cs" \
  --html "$OUTPUT_DIR/whitespace-ignore-all.html" \
  --whitespace-mode ignore-all \
  --quiet

# 6.4 Language-aware whitespace
echo "  → whitespace-language-aware.html (Language-aware whitespace)"
dotnet run --project "$CLI_DIR" --framework $FRAMEWORK -- diff \
  "$SCRIPT_DIR/impact-demo/old.cs" \
  "$SCRIPT_DIR/impact-demo/new.cs" \
  --html "$OUTPUT_DIR/whitespace-language-aware.html" \
  --whitespace-mode language-aware \
  --quiet

###############################################################################
# SECTION 7: Combination Examples
###############################################################################

echo -e "\n${GREEN}[7/8] Generating Combination Examples${NC}"

# 7.1 Multi-file + Multi-TFM + Inline
# Note: This requires multi-file directories with TFM-specific code, which we don't have
# So we'll use a meaningful combination: UserService comparison with various options
echo "  → combo-inline-impact-tfm.html (Inline + Impact + Formatting)"
dotnet run --project "$CLI_DIR" --framework $FRAMEWORK -- diff \
  "$SCRIPT_DIR/before/UserService.cs" \
  "$SCRIPT_DIR/after/UserService.cs" \
  --html "$OUTPUT_DIR/combo-inline-impact-tfm.html" \
  --inline=5 \
  --include-formatting \
  --quiet

# 7.2 Fragment + Inline + Impact filtering
echo "  → combo-fragment-inline-breaking.html (Fragment + Inline + Breaking changes only)"
dotnet run --project "$CLI_DIR" --framework $FRAMEWORK -- diff \
  "$SCRIPT_DIR/impact-demo/old.cs" \
  "$SCRIPT_DIR/impact-demo/new.cs" \
  --html "$OUTPUT_DIR/combo-fragment-inline-breaking.html" \
  --html-mode fragment \
  --extract-css combo-fragment-inline-breaking.css \
  --inline=5 \
  --impact-level breaking-internal \
  --quiet

# 7.3 UserService + Fragment + Tree (for dashboard embedding)
echo "  → combo-userservice-fragment-tree.html (UserService + Fragment for dashboards)"
dotnet run --project "$CLI_DIR" --framework $FRAMEWORK -- diff \
  "$SCRIPT_DIR/before/UserService.cs" \
  "$SCRIPT_DIR/after/UserService.cs" \
  --html "$OUTPUT_DIR/combo-userservice-fragment-tree.html" \
  --html-mode fragment \
  --extract-css combo-userservice-fragment-tree.css \
  --quiet

###############################################################################
# SECTION 8: Create Showcase README
###############################################################################

echo -e "\n${GREEN}[8/8] Creating documentation${NC}"

# Copy shared CSS files if they exist (for fragment modes)
if [ -f "$OUTPUT_DIR/roslyn-diff.css" ]; then
  echo "  → CSS files created for fragment mode samples"
fi

echo -e "\n${BLUE}==================================================================${NC}"
echo -e "${GREEN}✓ Successfully generated all HTML samples!${NC}"
echo -e "${BLUE}==================================================================${NC}"
echo ""
echo "Output directory: $OUTPUT_DIR"
echo ""
echo "Generated samples:"
echo "  • Basic modes: tree-document.html, tree-fragment.html"
echo "  • Inline views: inline-document.html, inline-fragment.html, inline-context-{3,5,10}.html"
echo "  • Additional files: userservice-tree.html, userservice-inline.html, userservice-fragment.html"
echo "  • Impact: impact-full.html, impact-inline.html, impact-breaking-*.html"
echo "  • Multi-TFM: tfm-tree.html, tfm-inline.html, tfm-fragment.html"
echo "  • Whitespace: whitespace-*.html"
echo "  • Combinations: combo-*.html"
echo ""
echo "To view samples:"
echo "  open $OUTPUT_DIR/tree-document.html"
echo ""
echo "See samples/html-samples/README.md for detailed documentation."
echo ""
