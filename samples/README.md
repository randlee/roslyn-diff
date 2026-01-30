# RoslynDiff Samples

This directory contains sample files demonstrating the capabilities of RoslynDiff, particularly its semantic analysis and impact classification features.

## Samples Overview

### multi-file

**Location:** `samples/multi-file/`

**Purpose:** Demonstrates Multi-File Comparison (v0.10.0) for analyzing changes across multiple files, directories, and git branches.

**Files:**
- `test-folders/before/` - Sample directory with 3 C# files (Calculator, StringHelper, Validator)
- `test-folders/after/` - Modified directory with changes, additions, and deletions
- `folder-comparison.json` - JSON output of folder comparison (schema v3)
- `folder-comparison-filtered.json` - Folder comparison with `--include` filter
- `git-comparison.json` - Real git comparison between roslyn-diff commits
- `README.md` - Comprehensive guide to multi-file comparison

**Features Demonstrated:**
- Git branch comparison (`--git-compare`)
- Folder-to-folder comparison (auto-detected)
- File filtering with glob patterns (`--include`, `--exclude`)
- Recursive directory traversal (`--recursive`)
- JSON schema v3 structure
- File status detection (modified, added, removed)
- Parallel processing for performance
- Integration patterns (CI/CD, pre-push hooks, dashboards)

**Use Cases:**
- Pull request analysis
- Release comparisons
- Migration planning
- Codebase audits
- CI/CD integration
- Pre-push validation

**How to Generate:**
```bash
cd src/RoslynDiff.Cli

# Folder comparison
dotnet run -- diff ../../samples/multi-file/test-folders/before/ \
  ../../samples/multi-file/test-folders/after/ \
  --json ../../samples/multi-file/folder-comparison.json

# Git comparison
dotnet run -- diff --git-compare main..feature \
  --include "src/**/*.cs" \
  --json ../../samples/multi-file/git-comparison.json

# With filtering
dotnet run -- diff ../../samples/multi-file/test-folders/before/ \
  ../../samples/multi-file/test-folders/after/ \
  --recursive \
  --include "*.cs" \
  --exclude "*.g.cs" \
  --json ../../samples/multi-file/folder-filtered.json
```

**View Examples:**
See `samples/multi-file/README.md` for detailed examples and integration patterns.

**Note**: HTML output for multi-file is not yet implemented in v0.10.0 (JSON only).

### inline-view

**Location:** `samples/inline-view/`

**Purpose:** Demonstrates Inline View Mode (v0.10.0) for line-by-line diff display with +/- markers, similar to git diff.

**Files:**
- `calculator-inline-full.html` - Full file view with all lines shown
- `calculator-inline-context3.html` - Context mode with 3 lines around changes
- `calculator-inline-context5.html` - Context mode with 5 lines around changes
- `impact-demo-inline.html` - Impact classification in inline view
- `calculator-inline-fragment.html` - Fragment mode with inline view
- `roslyn-diff.css` - External CSS for fragment mode
- `README.md` - Comprehensive guide to inline view features

**Features Demonstrated:**
- Line-by-line diff with +/- markers (like git diff)
- Full file mode vs. context mode (N lines around changes)
- Syntax highlighting in inline view
- Impact classification badges in inline format
- Fragment mode combined with inline view
- Comparison between tree view and inline view

**Use Cases:**
- Traditional diff workflow for developers familiar with git
- Line-by-line code reviews
- Patch documentation and archival
- Detailed whitespace and formatting analysis

**How to Generate:**
```bash
cd src/RoslynDiff.Cli

# Full file inline view
dotnet run -- diff ../../samples/before/Calculator.cs ../../samples/after/Calculator.cs \
  --html ../../samples/inline-view/calculator-inline-full.html \
  --inline

# Context mode (5 lines)
dotnet run -- diff ../../samples/before/Calculator.cs ../../samples/after/Calculator.cs \
  --html ../../samples/inline-view/calculator-inline-context5.html \
  --inline=5

# Fragment mode with inline view
dotnet run -- diff ../../samples/before/Calculator.cs ../../samples/after/Calculator.cs \
  --html ../../samples/inline-view/calculator-inline-fragment.html \
  --html-mode fragment \
  --inline=5
```

**View Examples:**
Open any `.html` file in `samples/inline-view/` to see inline diff view in action.

### fragment-mode

**Location:** `samples/fragment-mode/`

**Purpose:** Demonstrates HTML Fragment Mode for embedding roslyn-diff reports into existing web applications.

**Files:**
- `fragment.html` - Example HTML fragment with embedded diff (no document wrapper)
- `roslyn-diff.css` - External CSS stylesheet with all necessary styles
- `parent.html` - Example parent page showing how to embed fragments in a dashboard
- `README.md` - Comprehensive guide to fragment mode integration patterns

**Features Demonstrated:**
- Embeddable HTML fragments without document wrapper
- External CSS for shared styling across multiple fragments
- Data attributes for JavaScript integration
- Custom parent page with dashboard UI
- Metadata extraction and JavaScript interaction
- Multiple fragment embedding patterns

**Use Cases:**
- Code review dashboards
- Documentation sites (changelog, upgrade guides)
- CI/CD pipeline reports
- Static site generators (Jekyll, Hugo, Gatsby)
- Content management systems

**How to Generate:**
```bash
cd src/RoslynDiff.Cli

# Generate HTML fragment with external CSS
dotnet run -- diff ../../samples/before/Calculator.cs ../../samples/after/Calculator.cs \
  --html ../../samples/fragment-mode/fragment.html \
  --html-mode fragment

# Custom CSS filename
dotnet run -- diff ../../samples/before/Calculator.cs ../../samples/after/Calculator.cs \
  --html ../../samples/fragment-mode/fragment.html \
  --html-mode fragment \
  --extract-css custom.css
```

**View Example:**
Open `samples/fragment-mode/parent.html` in your browser to see the fragment embedded in a complete dashboard.

### impact-demo

**Location:** `samples/impact-demo/`

**Purpose:** Demonstrates the impact classification system with changes across all impact levels.

**Files:**
- `old.cs` - Original PaymentService implementation with public API, internal helpers, and private members
- `new.cs` - Modified version with various types of changes
- `output.json` - Full JSON output with impact classification for all changes
- `output.html` - HTML report with visual impact indicators
- `output-filtered.json` - Filtered JSON showing only breaking public API changes

**Changes Demonstrated:**
1. **Breaking Public API Changes:**
   - Added parameter to public method (`ProcessPayment` now accepts optional `description`)

2. **Breaking Internal API Changes:**
   - Renamed internal method (`UpdateMerchantSettings` to `ConfigureMerchantSettings`)
   - Renamed internal static method in InternalHelper class

3. **Non-Breaking Changes:**
   - Renamed private field (`_merchantId` to `_merchantIdentifier`)
   - Renamed private method parameter (`amount` to `paymentAmount`)
   - Changed private method signatures

4. **Formatting Only Changes:**
   - Added XML documentation comments
   - Added inline code comments
   - Added blank lines for readability

**Caveats Demonstrated:**
- Parameter renames may break code using named arguments
- Private member renames may break code using reflection

**How to Regenerate:**
```bash
cd src/RoslynDiff.Cli

# Generate full JSON output with impact classification
dotnet run -- diff ../../samples/impact-demo/old.cs ../../samples/impact-demo/new.cs --json ../../samples/impact-demo/output.json

# Generate HTML report
dotnet run -- diff ../../samples/impact-demo/old.cs ../../samples/impact-demo/new.cs --html ../../samples/impact-demo/output.html

# Generate filtered JSON (only breaking public API changes)
dotnet run -- diff ../../samples/impact-demo/old.cs ../../samples/impact-demo/new.cs --impact-level breaking-public --json ../../samples/impact-demo/output-filtered.json
```

### Calculator

**Location:** `samples/before/Calculator.cs` and `samples/after/Calculator.cs`

**Purpose:** Demonstrates semantic diff of a simple calculator class with method additions and documentation improvements.

**Changes:**
- Added XML documentation parameters and return tags
- Added `Multiply` method
- Added `Divide` method with exception handling

**How to Regenerate:**
```bash
cd src/RoslynDiff.Cli

# Generate JSON output
dotnet run -- diff ../../samples/before/Calculator.cs ../../samples/after/Calculator.cs --json ../../samples/output-example.json

# Generate HTML report
dotnet run -- diff ../../samples/before/Calculator.cs ../../samples/after/Calculator.cs --html ../../samples/output-example.html
```

### UserService

**Location:** `samples/before/UserService.cs` and `samples/after/UserService.cs`

**Purpose:** Example service class with repository pattern for user management operations.

**Features:**
- Dependency injection pattern
- Interface definitions
- CRUD operations
- XML documentation

**How to Compare:**
```bash
cd src/RoslynDiff.Cli

# Compare UserService files
dotnet run -- diff ../../samples/before/UserService.cs ../../samples/after/UserService.cs
```

## Understanding Impact Levels

RoslynDiff classifies changes into four impact levels:

### Breaking Public API
Changes that break the public API surface and require consumers to update their code:
- Public method signature changes (add/remove/rename parameters, change return type)
- Public method/property/field removal
- Public type removal or visibility reduction

### Breaking Internal API
Changes that break internal APIs, affecting code in the same assembly:
- Internal method/property/field signature changes
- Internal member removal
- Changes to internal visibility

### Non-Breaking
Changes that don't break APIs but may have subtle effects:
- Private member changes (may break reflection-based code)
- Parameter renames (may break named argument usage)
- Default value changes
- Method body changes

### Formatting Only
Pure formatting changes with no functional impact:
- Whitespace changes
- Comment additions/modifications
- Code reorganization without semantic changes

## CLI Options

### Impact Filtering

Filter changes by impact level:
```bash
# Show only breaking public API changes
dotnet run -- diff old.cs new.cs --impact-level breaking-public

# Show breaking public and breaking internal changes
dotnet run -- diff old.cs new.cs --impact-level breaking-internal

# Show all changes except formatting-only
dotnet run -- diff old.cs new.cs --impact-level non-breaking

# Show all changes including formatting
dotnet run -- diff old.cs new.cs --impact-level formatting-only
```

### Whitespace Handling

Control how whitespace is treated:
```bash
# Ignore all whitespace differences
dotnet run -- diff old.cs new.cs --whitespace-mode ignore

# Treat significant whitespace as non-breaking
dotnet run -- diff old.cs new.cs --whitespace-mode significant

# Treat all whitespace as formatting-only (default)
dotnet run -- diff old.cs new.cs --whitespace-mode all
```

### Output Formats

Generate different output formats:
```bash
# JSON output
dotnet run -- diff old.cs new.cs --json output.json

# HTML report (standalone document)
dotnet run -- diff old.cs new.cs --html report.html

# HTML fragment (embeddable with external CSS)
dotnet run -- diff old.cs new.cs --html fragment.html --html-mode fragment

# Both formats
dotnet run -- diff old.cs new.cs --json output.json --html report.html

# Console output with color coding
dotnet run -- diff old.cs new.cs
```

## Output Schema

The JSON output follows the `roslyn-diff-output-v2` schema for single-file comparisons and `roslyn-diff-output-v3` schema for multi-file comparisons with these key sections:

### Metadata
- Version information
- Timestamp
- Analysis mode (roslyn/line)
- Options used

### Summary
- Change counts by type (additions, deletions, modifications)
- Impact breakdown (counts per impact level)

### Changes
- Hierarchical structure (namespace → class → members)
- Each change includes:
  - Type (added/removed/modified)
  - Kind (namespace/class/method/property/field)
  - Impact level
  - Location information
  - Content (old and new)

## HTML Reports

HTML reports provide:
- Visual impact indicators with color coding
- Side-by-side before/after comparison
- Collapsible sections for easy navigation
- Syntax highlighting
- Summary statistics
- Filter capabilities

## Tips

1. **Use Impact Filtering:** When reviewing large changesets, start with `--impact-level breaking-public` to see the most critical changes first.

2. **Combine with Whitespace Modes:** Use `--whitespace-mode ignore` with impact filtering to focus purely on functional changes.

3. **HTML for Reviews:** Generate HTML reports for code reviews - they're easier to navigate and share than JSON.

4. **JSON for Automation:** Use JSON output for CI/CD pipelines and automated analysis.

5. **Understand Caveats:** Pay attention to caveat warnings for non-breaking changes that might have subtle effects.

## Contributing

To add new samples:
1. Create a new directory under `samples/`
2. Add before/after files demonstrating specific features
3. Generate sample outputs
4. Update this README with documentation

## Version History

- v0.10.0: Added Multi-File Comparison (git and folder) with glob filtering, and Inline Diff View
- v0.9.0: Added HTML Fragment Mode for embedding in existing applications
- v0.8.0: Added impact classification system with filtering and whitespace modes
- v0.7.0: Added HTML report generation
- v0.6.0: Enhanced semantic analysis
- v0.5.0: Initial sample collection
