# RoslynDiff.TestUtilities

Test utilities library for validating line number integrity in roslyn-diff outputs.

## Overview

This library provides infrastructure for validating that line numbers in diff results do not overlap or contain duplicates, ensuring the correctness of change detection and application.

## Components

### LineRange Record

Represents a range of line numbers within a source file.

```csharp
public record LineRange(int Start, int End, string? Source = null)
{
    public int LineCount { get; }
    public bool ContainsLine(int line);
    public bool OverlapsWith(LineRange other);
}
```

### LineRangeComparer

Static class providing detection methods for line range validation.

**Methods:**
- `DetectOverlaps(IEnumerable<LineRange> ranges)` - Finds all overlapping range pairs
- `DetectDuplicates(IEnumerable<int> lineNumbers)` - Finds duplicate line numbers
- `AreSequential(IEnumerable<LineRange> ranges)` - Checks if ranges are in order

### LineNumberValidator

Static class providing validation methods that return TestResult objects.

**Methods:**
- `ValidateNoOverlaps(IEnumerable<LineRange> ranges, string context)` - Validates no overlaps exist
- `ValidateNoDuplicates(IEnumerable<int> lineNumbers, string context)` - Validates no duplicates exist
- `ValidateRangesSequential(IEnumerable<LineRange> ranges, string context)` - Validates sequential ordering
- `ValidateRanges(IEnumerable<LineRange> ranges, string context, bool requireSequential)` - Comprehensive validation

### TestResult Model

Represents the result of a validation operation.

```csharp
public record TestResult
{
    public bool Passed { get; }
    public string Context { get; }
    public string Message { get; }
    public IReadOnlyList<string> Issues { get; }

    public static TestResult Pass(string context, string? message = null);
    public static TestResult Fail(string context, string message, IEnumerable<string>? issues = null);
}
```

## Usage Examples

### Basic Overlap Detection

```csharp
var ranges = new[]
{
    new LineRange(1, 5, "Method1"),
    new LineRange(3, 7, "Method2")  // Overlaps with Method1
};

var result = LineNumberValidator.ValidateNoOverlaps(ranges, "Old file changes");

if (!result.Passed)
{
    Console.WriteLine(result.ToString());
    // Output:
    // [FAIL] Old file changes: Found 1 overlapping line range(s)
    //   Issues:
    //     - Lines 1-5 (Method1) overlaps with Lines 3-7 (Method2)
}
```

### Validating Diff Results

```csharp
var changes = GetDiffResults(); // Returns IEnumerable<Change>

// Extract line ranges from old locations
var oldRanges = changes
    .Where(c => c.OldLocation != null)
    .Select(c => new LineRange(
        c.OldLocation!.StartLine,
        c.OldLocation.EndLine,
        $"{c.Kind} {c.Name}"))
    .ToList();

// Validate with sequential requirement
var result = LineNumberValidator.ValidateRanges(
    oldRanges,
    "Old file changes",
    requireSequential: true);

Assert.True(result.Passed);
```

### Duplicate Line Number Detection

```csharp
var lineNumbers = new[] { 1, 2, 3, 2, 4 };

var result = LineNumberValidator.ValidateNoDuplicates(
    lineNumbers,
    "Change start lines");

if (!result.Passed)
{
    // result.Issues will contain: ["Line 2 appears multiple times"]
}
```

## Test Coverage

The library includes comprehensive unit tests covering:
- LineRange functionality (construction, containment, overlap detection)
- Overlap detection with various scenarios
- Duplicate detection
- Sequential validation
- Edge cases (empty collections, single items, adjacent ranges)
- Null parameter validation
- Integration examples with RoslynDiff.Core models

**Total Tests:** 93 tests, all passing

## Dependencies

- RoslynDiff.Core - For Change and Location models

## Implementation Details

- **Time Complexity:** Overlap detection is O(n²) for comparing all pairs
- **Adjacent Ranges:** Ranges like [1-5] and [6-10] are NOT considered overlapping
- **Touching Ranges:** Ranges like [1-5] and [5-10] ARE considered overlapping (share line 5)
- **Sequential Order:** Ranges must satisfy `range[i].End < range[i+1].Start` for all consecutive pairs

## Sprint 1, Workstream C Deliverables

This implementation completes all requirements for Sprint 1, Workstream C:

1. ✅ LineRange record with Start, End, and optional Source
2. ✅ DetectOverlaps method with comprehensive overlap detection
3. ✅ DetectDuplicates method with duplicate line number detection
4. ✅ AreSequential helper method for sequential validation
5. ✅ LineNumberValidator with three validation methods returning TestResult objects
6. ✅ TestResult model with Pass/Fail factory methods
7. ✅ 93 comprehensive unit tests covering all scenarios
8. ✅ XML documentation on all public APIs
9. ✅ Integration examples demonstrating real-world usage with diff results
