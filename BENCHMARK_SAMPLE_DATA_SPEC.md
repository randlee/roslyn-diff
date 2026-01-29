# Benchmark Sample Data Specification

**File:** `ComprehensiveDiffModeBenchmarks.cs`
**Purpose:** Document the exact structure of test data used in comprehensive benchmarking

---

## Overview

The comprehensive benchmark suite tests 16 scenarios across 4 file sizes:

```
File Sizes:     50 lines   |  500 lines  |  2000 lines |  5000 lines
Change Types:   Small (0.2-2%) changes vs Large (30%) changes with rearrangement
Diff Modes:     Text Diff vs Semantic Diff
Total Tests:    4 sizes × 2 change types × 2 modes = 16 benchmarks
```

---

## File Size Templates

### Small File (50 lines)

**Structure:**
```
namespace BenchmarkTest;

public class TestClass
{
    private int _counter;
    private List<string> _cache = new();

    public long Method0(long input) { [5 lines] }
    public long Method1(long input) { [5 lines] }
    public long Method2(long input) { [5 lines] }
    public long Method3(long input) { [5 lines] }
    public long Method4(long input) { [5 lines] }
}
```

**Metadata:**
- Total lines: ~50
- Method count: 5
- Lines per method: 5
- Method body: Simple arithmetic operations
- Namespace: Single (BenchmarkTest)
- Class count: 1

**Generated sample method:**
```csharp
public long Method0(long input)
{
    var result = input;
    result = result + 0 + (input >> 0);
    result = result + 1 + (input >> 1);
    result = result + 2 + (input >> 2);
    result = result + 3 + (input >> 3);
    result = result + 4 + (input >> 4);
    return result;
}
```

---

### Medium File (500 lines)

**Structure:**
```
namespace BenchmarkTest;

public class TestClass
{
    [initialization]
    public long Method0(long input) { [5 lines] }
    public long Method1(long input) { [5 lines] }
    ...
    public long Method49(long input) { [5 lines] }
}
```

**Metadata:**
- Total lines: ~500
- Method count: 50
- Lines per method: 5
- Spacing: Each method occupies ~8 lines (declaration + body + blank)
- Total method lines: 50 methods × 8 = 400 lines
- Other (namespace, class, fields): ~50 lines

---

### Large File (2000 lines)

**Structure:**
```
namespace BenchmarkTest;

public class TestClass
{
    [initialization]
    public long Method0(long input) { [15 lines] }
    public long Method1(long input) { [15 lines] }
    ...
    public long Method99(long input) { [15 lines] }
}
```

**Metadata:**
- Total lines: ~2000
- Method count: 100
- Lines per method: 15
- Spacing: Each method occupies ~17 lines
- Total method lines: 100 × 17 = 1700 lines
- Other: ~300 lines

---

### Extra Large File (5000 lines)

**Structure:**
```
namespace BenchmarkTest;

public class TestClass
{
    [initialization]
    public long Method0(long input) { [15 lines] }
    public long Method1(long input) { [15 lines] }
    ...
    public long Method249(long input) { [15 lines] }
}
```

**Metadata:**
- Total lines: ~5000
- Method count: 250
- Lines per method: 15
- Total method lines: 250 × 17 ≈ 4250 lines
- Other: ~750 lines

---

## Change Scenarios

### Scenario Type 1: SMALL CHANGES (0.2-2%)

**Definition:** Minimal lines modified, no structural changes

#### 50-line file (1 line changed):
```
Original:  line 12: "        result = result + 0 + (input >> 0);"
Modified:  line 12: "        result = result + 0 + (input >> 0); // MODIFIED"

Change: 1 line
Percentage: ~2% of 50 lines
Detection: Easy (line diff) | Easy (semantic - comment only)
```

#### 500-line file (10 lines changed):
```
Modified: 10 random lines in method bodies
Changes distributed across multiple methods
No method signature changes
No rearrangement
Percentage: ~2% of 500 lines
```

**Implementation:**
```csharp
private static string ModifyNLines(string original, int linesToModify)
{
    var lines = original.Split('\n').ToList();
    var random = new Random(42); // Deterministic

    for (var i = 0; i < linesToModify && lines.Count > 0; i++)
    {
        var randomLine = random.Next(lines.Count);
        lines[randomLine] = lines[randomLine] + " // MODIFIED";
    }

    return string.Join('\n', lines);
}
```

#### 2000-line file (10 lines changed):
```
Modified: 10 random lines (same approach as 500-line)
Percentage: ~0.5% of 2000 lines
Pattern: Very sparse changes
```

#### 5000-line file (10 lines changed):
```
Modified: 10 random lines
Percentage: ~0.2% of 5000 lines
Pattern: Extremely sparse (typical maintenance)
```

**Characteristics of Small Change Scenario:**
- ✅ Minimal file modifications
- ✅ No structural changes
- ✅ Text diff shows clear advantage (pure line changes)
- ✅ Semantic diff overhead not justified
- ✅ Real-world analogy: One-line bug fix, comment update

---

### Scenario Type 2: LARGE CHANGES (30% body changed + rearrangement)

**Definition:** Significant refactoring with method rearrangement

#### 50-line file (40% changed + rearranged):
```
Original methods:  [Method0, Method1, Method2, Method3, Method4]
Rearranged:        [Method3, Method4, Method0, Method1, Method2]
                   (moved first 2 to end)

Changes:           Modify Method3, Method4 (new 30% = 2/5 methods)
Modifications:
  - Method3: signature changed from (long) to (long, bool = true)
  - Method4: same signature change
  - Method0,1,2: moved but unchanged
```

#### 500-line file (30% changed + rearranged):
```
Original methods:  [Method0, Method1, ..., Method49]
Rearranged:        [Method15-49] + [Method0-14]
                   (moved first 15 to end)

Changes:           Modify first 15 methods
Percentage:        15/50 = 30% of methods
Modifications:     Each modified method:
  - Signature: add "bool optimized = true" parameter
  - Body: change operations from + to *, shifts from >> to <<
  - Logic: different computation
```

**Modification Pattern:**
```csharp
// Original
public long Method0(long input)
{
    var result = input;
    result = result + 0 + (input >> 0);  // ADD, RIGHT SHIFT
    result = result + 1 + (input >> 1);
    ...
    return result;
}

// Modified
public long Method0(long input, bool optimized = true)
{
    var result = input * 2;              // DIFFERENT START
    result = result * 2 + (input << 0);  // MULTIPLY, LEFT SHIFT
    result = result * 3 + (input << 1);  // DIFFERENT OPS
    ...
    return result;
}
```

#### 2000-line file (30% changed + rearranged):
```
Original methods:  100 methods
Rearranged:        First 30 methods moved to end
Changes:           30 methods modified (30%)
Per-method mods:   Signature + body changes
Result:            Significant refactoring scenario
```

#### 5000-line file (30% changed + rearranged):
```
Original methods:  250 methods
Rearranged:        First 75 methods moved to end
Changes:           75 methods modified (30%)
Result:            Large-scale refactoring
Memory impact:     High (more AST nodes analyzed)
Time impact:       Significant (182 ms for semantic diff)
```

**Implementation:**
```csharp
private static string GenerateClassWithRearrangement(
    int methodCount,
    int linesPerMethod,
    int methodsToModify,
    bool rearrange)
{
    var methodIndices = Enumerable.Range(0, methodCount).ToList();

    // Rearrange: move first 30% to end
    if (rearrange)
    {
        var rearrangeCount = (int)(methodCount * 0.3);
        var rearranged = methodIndices.Skip(rearrangeCount).ToList();
        rearranged.AddRange(methodIndices.Take(rearrangeCount));
        methodIndices = rearranged;
    }

    // Generate in new order, modify first N
    for (var i = 0; i < methodCount; i++)
    {
        var actualIndex = methodIndices[i];
        if (i < methodsToModify)
            GenerateModifiedMethod(sb, actualIndex, linesPerMethod);
        else
            GenerateMethod(sb, actualIndex, linesPerMethod);
    }
}
```

**Characteristics of Large Change Scenario:**
- ✅ Significant modifications (30%)
- ✅ Structural rearrangement (moves detection)
- ✅ Text diff struggles with move detection
- ✅ Semantic diff shines with structural understanding
- ✅ Real-world analogy: Major refactoring, method reorganization

---

## Data Generation Timeline

```
BenchmarkDotNet GlobalSetup:
│
├─ Small file (50 lines)
│  ├─ Generate original: ~5 methods
│  ├─ Small change version: 1 line modification
│  └─ Large change version: rearrange + 2 modified
│
├─ Medium file (500 lines)
│  ├─ Generate original: 50 methods
│  ├─ Small change version: 10 lines scattered
│  └─ Large change version: rearrange first 15 + modify them
│
├─ Large file (2000 lines)
│  ├─ Generate original: 100 methods
│  ├─ Small change version: 10 lines scattered
│  └─ Large change version: rearrange first 30 + modify them
│
└─ Extra-large file (5000 lines)
   ├─ Generate original: 250 methods
   ├─ Small change version: 10 lines scattered
   └─ Large change version: rearrange first 75 + modify them
```

---

## Benchmark Execution

Each benchmark runs the diff on one scenario:

```
TextDiff_50_SmallChange()
  ├─ Input: _small50LineOld vs _small50LineNewSmallChanges
  ├─ Calls: _lineDiffer.Compare(old, new, options)
  └─ Measures: Time and memory for text-based diff

SemanticDiff_50_SmallChange()
  ├─ Input: same files
  ├─ Calls: _differ.Compare(old, new, options)  [CSharpDiffer]
  └─ Measures: Time and memory for semantic diff
```

---

## Sample Generated Code

### Example Method (small file):
```csharp
public long Method0(long input)
{
    var result = input;
    result = result + 0 + (input >> 0);
    result = result + 1 + (input >> 1);
    result = result + 2 + (input >> 2);
    result = result + 3 + (input >> 3);
    result = result + 4 + (input >> 4);
    return result;
}
```

### Example Modified Method (large changes):
```csharp
public long Method0(long input, bool optimized = true)
{
    var result = input * 2;
    result = result * 2 + (input << 0);
    result = result * 3 + (input << 1);
    result = result * 4 + (input << 2);
    result = result * 5 + (input << 3);
    result = result * 6 + (input << 4);
    return result;
}
```

**Differences:**
- Line 1: Parameter added (bool optimized = true)
- Line 3: `input` → `input * 2`
- Line 4-8: `+` → `*`, `>>` → `<<`, constants increased

---

## Key Properties of Test Data

### Determinism
```csharp
var random = new Random(42);  // Seed = 42 for reproducibility
```
- Same seed ensures identical data each run
- Allows comparison across iterations

### Realism
- Methods are actual code-like structures
- No trivial one-liners
- 5-15 lines per method = real methods
- Single class = typical production scenario

### Scalability
- Sizes increase by 10×: 50 → 500 → 2000 → 5000
- Same structure at all scales
- Proportional method counts

### Change Distribution
- Small changes: Random positions (realistic diffs)
- Large changes: Sequential modification + rearrangement
- Rearrangement: First 30% moved to end (common refactoring)

---

## Running the Benchmarks

```bash
# Run all comprehensive benchmarks
dotnet run -c Release -f net10.0 --project tests/RoslynDiff.Benchmarks -- \
  --filter "*ComprehensiveDiffModeBenchmarks*"

# Run specific scenario
dotnet run -c Release -f net10.0 --project tests/RoslynDiff.Benchmarks -- \
  --filter "*ComprehensiveDiffModeBenchmarks*TextDiff*5000*"

# With more iterations for statistical significance
... --iterationCount 5
```

---

## Data Validation

The benchmarks automatically validate:
- ✅ Files parse correctly (Roslyn syntax checking)
- ✅ Expected line counts
- ✅ Method count matches
- ✅ Changes are applied
- ✅ Memory allocation is tracked
- ✅ GC collections are recorded

---

## Conclusion

The comprehensive benchmark suite tests:
1. **Real-world file sizes:** 50 to 5000 lines
2. **Real-world change patterns:** Sparse edits to major refactoring
3. **Real-world scenarios:** Both text and semantic modes
4. **Both algorithms:** Text diff vs Roslyn semantic diff

This provides a complete performance picture for production decision-making.

---

**Reference:** ARCH-DIFF Analysis
**File:** `ComprehensiveDiffModeBenchmarks.cs`
**Location:** `/tests/RoslynDiff.Benchmarks/`
