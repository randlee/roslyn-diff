# Impact Classification Guide

**Version:** 0.7.0+
**Last Updated:** January 2026

roslyn-diff includes a powerful impact classification system that automatically categorizes code changes by their potential impact on consumers. This allows developers and AI agents to quickly identify which changes require careful review and which are purely cosmetic.

## Table of Contents

- [Overview](#overview)
- [Impact Levels](#impact-levels)
- [How Impact Classification Works](#how-impact-classification-works)
- [CLI Options](#cli-options)
- [Smart Defaults](#smart-defaults)
- [Caveat Warning System](#caveat-warning-system)
- [JSON Schema v2](#json-schema-v2)
- [Use Cases](#use-cases)
- [Examples](#examples)

---

## Overview

Impact classification answers a critical question: **"Does this change actually matter?"**

Not all code changes are created equal. A public API method signature change can break thousands of downstream consumers, while renaming a private variable is purely internal. roslyn-diff automatically classifies every change into one of four impact levels, helping you:

- **Prioritize code review efforts** - Focus on breaking changes first
- **Reduce AI token consumption** - Filter out non-impactful changes from LLM context
- **Improve CI/CD workflows** - Automatically flag PRs with breaking changes
- **Document API evolution** - Track which releases contain breaking changes

### Key Features

- **Always-on detection**: Impact classification runs automatically for every diff
- **Visibility-aware**: Tracks whether changed symbols are public, internal, or private
- **Caveat warnings**: Flags changes that may break in specific scenarios (e.g., reflection, named arguments)
- **Format-specific defaults**: HTML shows all changes, JSON filters non-impactful by default
- **Comprehensive breakdown**: Summary statistics show impact distribution at a glance

---

## Impact Levels

roslyn-diff classifies every change into one of four impact levels:

### 1. BreakingPublicApi

**Definition**: Changes to public API surface that could break external consumers.

**Examples**:
- Public method signature changed: `public void Process(int x)` → `public void Process(int x, bool validate)`
- Public class renamed: `class Calculator` → `class MathCalculator`
- Public member removed: Deleted `public string Name { get; set; }`
- Public member visibility reduced: `public` → `internal`
- Public parameter renamed: `public void Save(string fileName)` → `public void Save(string path)`
- Public method moved to different namespace or class
- Public property type changed: `public int Count` → `public long Count`

**Why it matters**: These changes will break compilation for consumers who reference your library. Requires major or minor version bump in semantic versioning.

**Typical actions**:
- Document in release notes
- Consider deprecation path
- Update migration guides
- Notify consumers before release

### 2. BreakingInternalApi

**Definition**: Changes to internal API surface that could break internal consumers within the same assembly or via `InternalsVisibleTo`.

**Examples**:
- Internal method signature changed
- Protected member modified in non-sealed class (affects subclasses)
- Internal class renamed or removed
- Protected internal member visibility changed
- PrivateProtected member modified

**Why it matters**: These changes can break internal consumers or subclasses. May require coordination across team or between assemblies using `InternalsVisibleTo`.

**Typical actions**:
- Verify no internal consumers are affected
- Update tests and internal documentation
- Check for InternalsVisibleTo usage
- May warrant minor version bump

### 3. NonBreaking

**Definition**: Changes that don't affect code execution or API contracts. These changes are detectable but typically safe.

**Examples**:
- Private member renamed: `private int _count` → `private int _counter`
- Local variable renamed: `var x = 1` → `var result = 1`
- Parameter name changed (with caveat): `void Foo(int x)` → `void Foo(int y)`
- Code reordered within same scope
- Private method body implementation changed
- Local function added or removed

**Why it matters**: Generally safe, but may have edge cases (see Caveats below). Can be safely hidden from AI code review to reduce token usage.

**Typical actions**:
- Review for correctness but don't require approval
- Can be auto-merged in many workflows
- Document if behavior meaningfully changed

### 4. FormattingOnly

**Definition**: Pure whitespace or comment-only changes with no semantic impact.

**Examples**:
- Whitespace changes: `int x=1;` → `int x = 1;`
- Indentation adjustments
- Line ending normalization (CRLF ↔ LF)
- Comment additions, removals, or modifications
- Blank lines added or removed

**Why it matters**: Zero semantic impact. Purely cosmetic. Can be completely ignored for functional review.

**Typical actions**:
- Auto-approve in CI/CD
- Exclude from AI review entirely
- May indicate auto-formatter was applied

---

## How Impact Classification Works

### Visibility Tracking

Every symbol in C# has a visibility/accessibility level. roslyn-diff extracts this information during semantic analysis:

```
Visibility Levels (highest to lowest scope):
├─ Public              - Accessible everywhere
├─ Protected           - Accessible in derived classes
├─ Internal            - Accessible within assembly
├─ ProtectedInternal   - Protected OR Internal
├─ PrivateProtected    - Protected AND Internal
├─ Private             - Accessible only within type
└─ Local               - Parameters, local variables
```

### Classification Rules

The classification algorithm considers both the **change type** and the **symbol visibility**:

| Change Type | Symbol Visibility | Impact | Reasoning |
|------------|-------------------|--------|-----------|
| Renamed | Public/Protected | **BreakingPublicApi** | Consumers using the old name will break |
| Renamed | Internal/ProtectedInternal | **BreakingInternalApi** | Internal consumers will break |
| Renamed | Private | **NonBreaking** | Only internal implementation affected |
| Renamed | Local/Parameter | **NonBreaking** | No external reference possible |
| Added | Public/Protected | **BreakingPublicApi** | New member may cause ambiguity |
| Added | Internal | **BreakingInternalApi** | Internal API surface changed |
| Added | Private/Local | **NonBreaking** | No external visibility |
| Removed | Public/Protected | **BreakingPublicApi** | Consumers referencing it will break |
| Removed | Internal | **BreakingInternalApi** | Internal consumers may break |
| Removed | Private/Local | **NonBreaking** | Not externally visible |
| Modified (signature) | Public/Protected | **BreakingPublicApi** | Signature must match for consumers |
| Modified (signature) | Internal | **BreakingInternalApi** | Internal signature must match |
| Modified (body only) | Any | **NonBreaking** | Implementation detail |
| Moved (same scope) | Any | **NonBreaking** | Logical reordering only |
| Moved (different scope) | Public | **BreakingPublicApi** | Fully qualified name changed |

### Detection Process

Impact classification happens automatically during diff analysis:

1. **Extract syntax**: Parse old and new code into syntax trees
2. **Identify changes**: Compare trees to find additions, deletions, modifications
3. **Determine visibility**: For each changed symbol, extract its accessibility
4. **Classify impact**: Apply classification rules based on change type + visibility
5. **Detect caveats**: Flag special cases that may break despite low impact level
6. **Aggregate statistics**: Count changes by impact level for summary

This process is **always enabled** and cannot be disabled. You control only which impact levels to include in the output.

---

## CLI Options

roslyn-diff provides flexible filtering to show only the changes you care about:

### `--impact-level <level>`

Filter changes to show only those at or above the specified impact level.

**Values**:
- `breaking-public` - Show only public API breaking changes
- `breaking-internal` - Show public + internal breaking changes (default for JSON)
- `non-breaking` - Show all except formatting-only changes
- `all` - Show everything including formatting changes (default for HTML)

**Examples**:
```bash
# Library maintainer preparing release notes (only breaking changes)
roslyn-diff diff v1.0.0/lib.cs v2.0.0/lib.cs --impact-level breaking-public

# Internal code review (exclude formatting noise)
roslyn-diff diff old.cs new.cs --impact-level non-breaking

# Comprehensive audit (include everything)
roslyn-diff diff old.cs new.cs --impact-level all
```

### `--include-non-impactful`

Explicitly include non-breaking changes in JSON output. Only applies to JSON format.

**Default**: `false` for JSON, always `true` for HTML

**Examples**:
```bash
# AI code review with comprehensive context
roslyn-diff diff old.cs new.cs -o json --include-non-impactful

# Minimal JSON for breaking change detection only
roslyn-diff diff old.cs new.cs -o json  # Excludes non-breaking by default
```

### `--include-formatting`

Include formatting-only changes in output. Applies to all formats.

**Default**: `false` for JSON, `true` for HTML

**Examples**:
```bash
# Check if changes are pure formatting
roslyn-diff diff old.cs new.cs --impact-level all --include-formatting

# Verify formatter didn't change logic (should be formatting-only)
roslyn-diff diff before.cs after.cs -o json --include-formatting | \
  jq '.summary.impactBreakdown | select(.formattingOnly == .totalChanges)'
```

### Option Precedence

When multiple options conflict:
1. `--impact-level` takes priority (sets minimum threshold)
2. `--include-formatting` is applied after impact filtering
3. `--include-non-impactful` only affects JSON format

---

## Smart Defaults

roslyn-diff uses different defaults based on output format, optimized for common use cases:

### HTML Format (Human Review)

**Philosophy**: Show everything for comprehensive visual review.

```bash
roslyn-diff diff old.cs new.cs -o html
```

**Defaults**:
- `--impact-level all` - Show all impact levels
- `--include-formatting true` - Show formatting changes
- `--include-non-impactful true` - Show non-breaking changes

**Rationale**: Humans reviewing HTML reports benefit from seeing the complete picture. Visual styling (grayed-out text, badges) helps distinguish impact levels at a glance without requiring filtering.

### JSON Format (Machine Consumption)

**Philosophy**: Optimize for signal-to-noise ratio to conserve AI tokens.

```bash
roslyn-diff diff old.cs new.cs -o json
```

**Defaults**:
- `--impact-level breaking-internal` - Show breaking changes only
- `--include-formatting false` - Exclude formatting changes
- `--include-non-impactful false` - Exclude non-breaking changes

**Rationale**: AI models and automated tools should focus on changes that actually matter. Non-breaking and formatting changes waste tokens and distract from important changes.

### Override Defaults

Both formats respect explicit CLI options:

```bash
# Force JSON to show everything (like HTML)
roslyn-diff diff old.cs new.cs -o json --impact-level all --include-non-impactful

# Force HTML to show only breaking changes (like JSON)
roslyn-diff diff old.cs new.cs -o html --impact-level breaking-internal
```

---

## Caveat Warning System

Some changes classified as `NonBreaking` may actually break code in specific scenarios. roslyn-diff automatically detects these edge cases and attaches **caveat warnings** to help you assess risk.

### Caveat Types

#### 1. Parameter Renames

**Scenario**: Public or internal method parameter renamed

**Example**:
```csharp
// Before
public void Save(string fileName) { }

// After
public void Save(string path) { }
```

**Caveat**: `"Parameter rename may break callers using named arguments"`

**Why it matters**: Consumers calling `Save(fileName: "data.txt")` will break because the parameter name changed. The method signature (from a binary perspective) remains compatible, but named argument calls will fail.

**Mitigation**: Search codebase for named argument usage before renaming.

#### 2. Private Member Renames

**Scenario**: Private field, property, or method renamed

**Example**:
```csharp
// Before
private int _count;

// After
private int _counter;
```

**Caveat**: `"Private member rename may break reflection or serialization"`

**Why it matters**: Code using reflection to access `_count` will break:
```csharp
typeof(MyClass).GetField("_count", BindingFlags.NonPublic | BindingFlags.Instance)
```

Also affects serializers that use field names (JSON.NET, XML serialization).

**Mitigation**: Search for reflection usage or serialization attributes. Use `[JsonProperty("count")]` to decouple serialized name from field name.

#### 3. Code Reordering

**Scenario**: Members moved within same class/scope

**Example**:
```csharp
// Before
class C { void A() {} void B() {} }

// After
class C { void B() {} void A() {} }
```

**Caveat**: `"Code reordering within same scope"`

**Why it matters**: Generally safe, but may affect:
- Initialization order dependencies (rare)
- Reflection code that iterates members assuming order
- Debugging workflows that rely on line numbers

**Mitigation**: Usually safe to ignore. Verify no order-dependent logic exists.

### Viewing Caveats

Caveats appear in all output formats:

**JSON**:
```json
{
  "type": "renamed",
  "name": "path",
  "oldName": "fileName",
  "impact": "nonBreaking",
  "visibility": "local",
  "caveats": [
    "Parameter rename may break callers using named arguments"
  ]
}
```

**HTML**: Displayed as yellow warning boxes below the change.

**Terminal**: Shown as yellow warning text with ⚠ icon.

---

## JSON Schema v2

Version 0.7.0 introduced JSON schema v2 with impact classification support.

### Schema Identifier

```json
{
  "$schema": "roslyn-diff-output-v2",
  "metadata": {
    "version": "2.0.0",
    ...
  }
}
```

### Metadata Section

```json
"metadata": {
  "tool": "roslyn-diff",
  "version": "0.7.0",
  "timestamp": "2026-01-19T10:30:00Z",
  "mode": "roslyn",
  "options": {
    "includeContent": true,
    "contextLines": 3,
    "includeNonImpactful": false,
    "includeFormatting": false,
    "impactLevel": "breaking-internal"
  }
}
```

### Summary with Impact Breakdown

The summary now includes `impactBreakdown` showing the distribution of changes by impact level:

```json
"summary": {
  "totalChanges": 15,
  "impactBreakdown": {
    "breakingPublicApi": 3,
    "breakingInternalApi": 2,
    "nonBreaking": 8,
    "formattingOnly": 2
  },
  "typeBreakdown": {
    "additions": 5,
    "deletions": 2,
    "modifications": 6,
    "renames": 2,
    "moves": 0
  }
}
```

**Key Fields**:
- `totalChanges`: Total number of changes included in output (respects filters)
- `impactBreakdown`: Count of changes by impact level
- `typeBreakdown`: Count of changes by type (added/removed/modified/etc)

**Use cases**:
```bash
# Check if PR has breaking changes
jq '.summary.impactBreakdown.breakingPublicApi > 0' diff.json

# Count high-priority review items
jq '.summary.impactBreakdown.breakingPublicApi + .summary.impactBreakdown.breakingInternalApi' diff.json

# Verify changes are formatting-only
jq '.summary.totalChanges == .summary.impactBreakdown.formattingOnly' diff.json
```

### Change Object Schema

Each change now includes impact classification fields:

```json
{
  "type": "modified",
  "kind": "method",
  "name": "ProcessOrder",
  "oldName": null,
  "impact": "breakingPublicApi",
  "visibility": "public",
  "caveats": [],
  "oldLocation": {
    "file": "old/Service.cs",
    "startLine": 42,
    "endLine": 58,
    "startColumn": 5,
    "endColumn": 6
  },
  "newLocation": {
    "file": "new/Service.cs",
    "startLine": 45,
    "endLine": 62,
    "startColumn": 5,
    "endColumn": 6
  },
  "oldContent": "public void ProcessOrder(int id) { ... }",
  "newContent": "public async Task ProcessOrder(int id, CancellationToken ct) { ... }",
  "children": []
}
```

**New Fields**:
- `impact`: One of `breakingPublicApi`, `breakingInternalApi`, `nonBreaking`, `formattingOnly`
- `visibility`: One of `public`, `protected`, `internal`, `protectedInternal`, `privateProtected`, `private`, `local`, or `null`
- `caveats`: Array of warning strings (empty if none)

### Backward Compatibility

Schema v2 is backward compatible:
- All v1 fields remain unchanged
- New fields are purely additive
- Old consumers can safely ignore new fields
- Schema version clearly indicates capability

---

## Use Cases

### 1. Library Maintainer: Preparing Release Notes

**Scenario**: You're releasing v2.0 and need to document breaking changes.

```bash
# Extract only public API breaking changes
roslyn-diff diff v1.0.0/src/ v2.0.0/src/ \
  -o json \
  --impact-level breaking-public \
  > breaking-changes.json

# Generate release notes from JSON
jq -r '.fileChanges[].changes[] |
  "- \(.kind | ascii_upcase) \(.name): \(.type)"' \
  breaking-changes.json > BREAKING.md
```

**Result**: Focused list of changes that require consumer updates.

### 2. AI Code Review: Optimizing Token Usage

**Scenario**: Using Claude or GPT to review PRs, but hitting context limits.

```bash
# Generate minimal JSON for AI (default excludes non-impactful)
roslyn-diff diff base.cs pr.cs -o json > changes.json

# Let AI review only meaningful changes
cat changes.json | claude-code "Review these changes for correctness and security"
```

**Token savings**: Typically 60-80% reduction compared to full diff.

### 3. CI/CD Pipeline: Automated Checks

**Scenario**: Prevent accidental breaking changes in patch releases.

```yaml
# .github/workflows/check-breaking-changes.yml
- name: Check for breaking changes
  run: |
    roslyn-diff diff origin/main HEAD \
      -o json \
      --impact-level breaking-public \
      > changes.json

    BREAKING_COUNT=$(jq '.summary.impactBreakdown.breakingPublicApi' changes.json)

    if [ "$BREAKING_COUNT" -gt 0 ] && [[ "$VERSION" == *".0" ]]; then
      echo "❌ Breaking changes detected in patch release!"
      jq '.fileChanges[].changes[] | select(.impact=="breakingPublicApi")' changes.json
      exit 1
    fi
```

**Result**: Automated enforcement of semantic versioning rules.

### 4. Code Quality: Whitespace Cleanup Verification

**Scenario**: Applied auto-formatter and want to verify no logic changed.

```bash
# Run diff including formatting changes
roslyn-diff diff before.cs after.cs \
  -o json \
  --impact-level all \
  --include-formatting \
  > format-check.json

# Verify all changes are formatting-only
TOTAL=$(jq '.summary.totalChanges' format-check.json)
FORMATTING=$(jq '.summary.impactBreakdown.formattingOnly' format-check.json)

if [ "$TOTAL" -eq "$FORMATTING" ]; then
  echo "✅ All changes are formatting-only"
else
  echo "⚠️ Non-formatting changes detected!"
  jq '.fileChanges[].changes[] | select(.impact != "formattingOnly")' format-check.json
fi
```

**Result**: Confidence that formatting didn't introduce bugs.

### 5. Refactoring Safety: Impact Assessment

**Scenario**: Large refactoring; need to assess blast radius.

```bash
# Generate comprehensive report with impact levels
roslyn-diff diff before-refactor/ after-refactor/ \
  -o html \
  --out-file refactor-report.html

# Also get JSON for metrics
roslyn-diff diff before-refactor/ after-refactor/ \
  -o json \
  --include-non-impactful \
  > refactor-metrics.json

# Check impact distribution
jq '.summary.impactBreakdown' refactor-metrics.json
```

**Result**: Clear understanding of refactoring scope and risk level.

---

## Examples

### Example 1: Public Method Signature Change

**Code Change**:
```csharp
// Before (v1.0)
public class OrderService
{
    public void ProcessOrder(int orderId)
    {
        // Process order
    }
}

// After (v2.0)
public class OrderService
{
    public void ProcessOrder(int orderId, bool validateInventory)
    {
        // Process order with validation
    }
}
```

**Classification**:
- **Impact**: `BreakingPublicApi`
- **Visibility**: `public`
- **Caveats**: None
- **Reasoning**: Public method signature changed; consumers passing only 1 argument will break

**JSON Output**:
```json
{
  "type": "modified",
  "kind": "method",
  "name": "ProcessOrder",
  "impact": "breakingPublicApi",
  "visibility": "public",
  "caveats": []
}
```

### Example 2: Private Field Rename

**Code Change**:
```csharp
// Before
public class Customer
{
    private string _name;

    public string Name => _name;
}

// After
public class Customer
{
    private string _fullName;

    public string Name => _fullName;
}
```

**Classification**:
- **Impact**: `NonBreaking`
- **Visibility**: `private`
- **Caveats**: `["Private member rename may break reflection or serialization"]`
- **Reasoning**: Private field not directly accessible, but reflection/serialization may depend on name

**JSON Output**:
```json
{
  "type": "renamed",
  "kind": "field",
  "name": "_fullName",
  "oldName": "_name",
  "impact": "nonBreaking",
  "visibility": "private",
  "caveats": [
    "Private member rename may break reflection or serialization"
  ]
}
```

### Example 3: Parameter Rename with Named Arguments Risk

**Code Change**:
```csharp
// Before
public void SaveFile(string fileName, string directory)
{
    // Save logic
}

// After
public void SaveFile(string path, string folder)
{
    // Save logic
}
```

**Classification**:
- **Impact**: `BreakingPublicApi`
- **Visibility**: `public` (method) / `local` (parameters)
- **Caveats**: `["Parameter rename may break callers using named arguments"]`
- **Reasoning**: Binary compatible, but named argument calls like `SaveFile(fileName: "data.txt", directory: "/tmp")` will break

**JSON Output**:
```json
{
  "type": "modified",
  "kind": "method",
  "name": "SaveFile",
  "impact": "breakingPublicApi",
  "visibility": "public",
  "caveats": [
    "Parameter rename may break callers using named arguments"
  ]
}
```

### Example 4: Filtering by Impact Level

**Scenario**: Large PR with mixed changes

```bash
# Generate full diff
roslyn-diff diff old/ new/ -o json --include-non-impactful > full.json

# Check statistics
jq '.summary.impactBreakdown' full.json
```

**Output**:
```json
{
  "breakingPublicApi": 2,
  "breakingInternalApi": 5,
  "nonBreaking": 37,
  "formattingOnly": 156
}
```

**Filter to breaking changes only**:
```bash
# Re-run with filtering
roslyn-diff diff old/ new/ -o json --impact-level breaking-public > breaking.json

jq '.summary.totalChanges' breaking.json
# Output: 2

# View the breaking changes
jq '.fileChanges[].changes[]' breaking.json
```

**Result**: Focus review on 2 critical changes instead of 200 total changes.

### Example 5: Comprehensive Change with Multiple Issues

**Code Change**:
```csharp
// Before
public class DataProcessor
{
    private int _count;

    public void Process(string data)
    {
        _count++;
        // Process data
    }
}

// After
public class DataProcessor
{
    private int _itemsProcessed;

    public async Task<int> Process(string inputData, CancellationToken cancellationToken)
    {
        _itemsProcessed++;
        // Process data
        return _itemsProcessed;
    }
}
```

**Classifications**:

1. **Method signature change**:
   - Impact: `BreakingPublicApi`
   - Visibility: `public`
   - Reasoning: Return type changed, parameter added, made async

2. **Parameter rename** (`data` → `inputData`):
   - Impact: `BreakingPublicApi`
   - Visibility: `local`
   - Caveat: Parameter rename may break named arguments

3. **Field rename** (`_count` → `_itemsProcessed`):
   - Impact: `NonBreaking`
   - Visibility: `private`
   - Caveat: Private member rename may break reflection

**Summary Statistics**:
```json
{
  "totalChanges": 3,
  "impactBreakdown": {
    "breakingPublicApi": 2,
    "breakingInternalApi": 0,
    "nonBreaking": 1,
    "formattingOnly": 0
  }
}
```

---

## Best Practices

### For Library Authors

1. **Always check breaking changes before release**:
   ```bash
   roslyn-diff diff v1.0.0/ HEAD --impact-level breaking-public
   ```

2. **Document caveats in release notes**: Even non-breaking changes with caveats deserve mention.

3. **Use impact levels for semantic versioning decisions**:
   - Breaking public API → Major version bump
   - Breaking internal API → Minor version bump (if API is stable)
   - Non-breaking changes → Patch version bump

### For Code Reviewers

1. **Start with breaking changes**: Review high-impact changes first.

2. **Trust the classification, but verify caveats**: Automated classification is accurate, but caveats need human judgment.

3. **Use HTML for visual review**: Color coding and badges make impact levels obvious.

### For AI Integration

1. **Exclude non-impactful by default**: Use default JSON mode to conserve tokens.

2. **Include full context for complex refactorings**: Use `--include-non-impactful` when AI needs complete picture.

3. **Parse impact breakdown for routing**: Route PRs to different AI prompts based on impact level.

### For CI/CD Pipelines

1. **Fail builds on unexpected breaking changes**: Enforce versioning rules automatically.

2. **Generate impact reports**: Attach impact summaries to PR comments.

3. **Track breaking change frequency**: Monitor `impactBreakdown` over time for quality metrics.

---

## Limitations

### What Impact Classification Does NOT Detect

- **Binary compatibility beyond signatures**: Does not perform deep IL analysis
- **Behavioral breaking changes**: Method that changes return values but keeps signature
- **Performance regressions**: Algorithmic changes that maintain correctness but hurt performance
- **Thread safety changes**: Adding/removing locks or changing concurrency patterns
- **Exception changes**: Throwing new exception types
- **Semantic changes**: `Add(x)` method that now multiplies instead

These require human review and testing.

### Edge Cases

- **Overloaded methods**: Parameter rename on one overload may affect overload resolution
- **Extension methods**: Static class changes affect extension method availability
- **Generic constraints**: Constraint changes are detected but impact may be subtle
- **Implicit conversions**: User-defined conversion operators may change behavior

Caveats help flag some of these, but not all.

---

## Further Reading

- [Output Formats](output-formats.md) - Details on JSON schema and HTML rendering
- [Whitespace Handling](whitespace-handling.md) - Language-aware whitespace detection
- [API Documentation](api.md) - Programmatic usage of impact classification
- [Testing Strategy](testing.md) - How impact classification is tested

---

**Need help?** Open an issue at https://github.com/randlee/roslyn-diff/issues
