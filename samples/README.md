# RoslynDiff Samples

This directory contains sample files demonstrating the capabilities of RoslynDiff, particularly its semantic analysis and impact classification features.

## Samples Overview

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

# HTML report
dotnet run -- diff old.cs new.cs --html report.html

# Both formats
dotnet run -- diff old.cs new.cs --json output.json --html report.html

# Console output with color coding
dotnet run -- diff old.cs new.cs
```

## Output Schema

The JSON output follows the `roslyn-diff-output-v2` schema with these key sections:

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

- v0.8.0: Added impact classification system with filtering and whitespace modes
- v0.7.0: Added HTML report generation
- v0.6.0: Enhanced semantic analysis
- v0.5.0: Initial sample collection
