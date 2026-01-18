# TempTestCases - Ad-Hoc Test Files

## Purpose

This folder is designed for temporary, ad-hoc testing of sample file validation. Files placed here:

- Are automatically gitignored (not committed to the repository)
- Can be used for quick validation testing during development
- Allow testing of proprietary or sensitive code samples
- Enable rapid iteration without cluttering the main test fixtures

## Usage

### Adding Test Files

1. Create old/new file pairs following the naming convention:
   ```
   {TestName}_Old.{extension}
   {TestName}_New.{extension}
   ```

2. Place both files in this directory

3. Run validation tests as normal

### Naming Convention Examples

Good naming patterns:

```
Calculator_Old.cs
Calculator_New.cs

DatabaseMigration_Old.sql
DatabaseMigration_New.sql

ApiController_Old.vb
ApiController_New.vb

config_old.json
config_new.json
```

Alternative patterns (also supported):

```
TestCase1.old.cs
TestCase1.new.cs

feature.before.cs
feature.after.cs
```

### Running Tests on TempTestCases

#### Using SampleDataValidator Directly

```csharp
using RoslynDiff.TestUtilities.Validators;

var oldFile = "tests/RoslynDiff.Integration.Tests/TempTestCases/MyTest_Old.cs";
var newFile = "tests/RoslynDiff.Integration.Tests/TempTestCases/MyTest_New.cs";

var results = SampleDataValidator.ValidateAll(oldFile, newFile);

foreach (var result in results)
{
    Console.WriteLine(result);
}
```

#### Using xUnit Tests

```csharp
[Fact]
public void ValidateMyAdHocTest()
{
    var basePath = "tests/RoslynDiff.Integration.Tests/TempTestCases";
    var oldFile = Path.Combine(basePath, "MyTest_Old.cs");
    var newFile = Path.Combine(basePath, "MyTest_New.cs");

    var results = SampleDataValidator.ValidateAll(oldFile, newFile);

    Assert.All(results, r => Assert.True(r.Passed, r.ToString()));
}
```

#### Command Line Testing

```bash
# Navigate to worktree
cd /Users/randlee/Documents/github/roslyn-diff-worktrees/feature/sample-data-validation-tests

# Run tests that might include TempTestCases
dotnet test tests/RoslynDiff.Integration.Tests/

# Or test files directly with roslyn-diff CLI
dotnet run --project src/RoslynDiff.Cli/ -- file \
    tests/RoslynDiff.Integration.Tests/TempTestCases/MyTest_Old.cs \
    tests/RoslynDiff.Integration.Tests/TempTestCases/MyTest_New.cs \
    --output json
```

## File Types Supported

Any text-based file can be used:

### Code Files
- C#: `.cs`
- Visual Basic: `.vb`
- F#: `.fs`
- TypeScript: `.ts`
- JavaScript: `.js`
- Python: `.py`
- Java: `.java`
- etc.

### Configuration Files
- JSON: `.json`
- XML: `.xml`
- YAML: `.yml`, `.yaml`
- TOML: `.toml`

### Documentation
- Markdown: `.md`
- Text: `.txt`
- HTML: `.html`

### Diff Modes

The validator will automatically select the appropriate diff mode:

- **Roslyn Semantic Diff:** For `.cs` and `.vb` files (unless forced to line mode)
- **Line-by-Line Diff:** For all other file types and when explicitly specified

You can override with options:

```csharp
var options = new SampleDataValidatorOptions
{
    DiffMode = DiffMode.Line  // Force line-by-line mode
};

var results = SampleDataValidator.ValidateAll(oldFile, newFile, options);
```

## Best Practices

### 1. Use Descriptive Names

```
❌ test1_old.cs / test1_new.cs
✅ RefactoredAuthLogic_Old.cs / RefactoredAuthLogic_New.cs
```

### 2. Keep Files Small

For quick validation testing, use minimal examples:

```csharp
// Good: Focused test case
public class Calculator
{
    public int Add(int a, int b) => a + b;
    public int Subtract(int a, int b) => a - b;
}
```

### 3. Document Expected Behavior

Add a comment at the top of your test files:

```csharp
// Test: Validates method parameter rename detection
// Expected: One modification (parameter rename)
public class Calculator
{
    // Old: Add(int x, int y)
    public int Add(int a, int b) => a + b;
}
```

### 4. Clean Up Regularly

Since files are gitignored, remember to:
- Delete files after testing
- Don't rely on them for permanent test cases
- Move valuable test cases to TestFixtures if needed

### 5. Use for Debugging

When tests fail in TestFixtures, copy to TempTestCases to:
- Isolate the issue
- Test fixes quickly
- Iterate without affecting committed tests

## Integration with Validation Tests

Tests can optionally discover and validate files in TempTestCases:

```csharp
[Theory]
[TempTestCasesData]  // Discovers files in TempTestCases folder
public void ValidateTempTestCase(string oldFile, string newFile)
{
    var results = SampleDataValidator.ValidateAll(oldFile, newFile);

    Assert.All(results, r =>
        Assert.True(r.Passed,
            $"Validation failed for {Path.GetFileName(oldFile)}: {r}"));
}
```

## Troubleshooting

### Files Not Discovered

**Problem:** Test discovery doesn't find your files.

**Solutions:**
- Ensure files follow naming convention
- Check file extensions are supported
- Verify both old and new files exist
- Check files are in correct directory

### Validation Failures

**Problem:** Validation tests fail on your temp files.

**Solutions:**
1. Check roslyn-diff CLI is installed and working
2. Verify file syntax is valid
3. Review validation output for specific issues
4. Use `PreserveTempFiles = true` to inspect generated outputs
5. Test files directly with CLI to isolate issues

### Permission Issues

**Problem:** Cannot write or read files.

**Solutions:**
- Check file permissions
- Ensure directory is writable
- Run tests with appropriate permissions

## .gitignore Configuration

This folder contains a `.gitkeep` file to ensure it exists in the repository, but all other files are gitignored:

```gitignore
# In tests/RoslynDiff.Integration.Tests/TempTestCases/.gitignore
*
!.gitkeep
!README.md
```

This means:
- The folder structure is committed
- Documentation (this file) is committed
- All test files you add are automatically ignored
- You can safely add any files without worrying about committing them

## Examples

### Example 1: Quick Method Rename Test

**File: MethodRename_Old.cs**
```csharp
public class Calculator
{
    public int AddNumbers(int a, int b) => a + b;
}
```

**File: MethodRename_New.cs**
```csharp
public class Calculator
{
    public int Add(int a, int b) => a + b;
}
```

**Test:**
```csharp
var results = SampleDataValidator.ValidateAll(
    "TempTestCases/MethodRename_Old.cs",
    "TempTestCases/MethodRename_New.cs"
);

// Should detect one modification (method rename)
```

### Example 2: JSON Configuration Change

**File: AppConfig_Old.json**
```json
{
  "api": {
    "endpoint": "http://localhost:5000",
    "timeout": 30
  }
}
```

**File: AppConfig_New.json**
```json
{
  "api": {
    "endpoint": "https://api.example.com",
    "timeout": 60
  }
}
```

**Test:**
```csharp
var options = new SampleDataValidatorOptions { DiffMode = DiffMode.Line };
var results = SampleDataValidator.ValidateAll(
    "TempTestCases/AppConfig_Old.json",
    "TempTestCases/AppConfig_New.json",
    options
);

// Line-by-line diff will show changed values
```

### Example 3: Visual Basic Class Change

**File: Customer_Old.vb**
```vb
Public Class Customer
    Public Property Name As String
    Public Property Email As String
End Class
```

**File: Customer_New.vb**
```vb
Public Class Customer
    Public Property Name As String
    Public Property Email As String
    Public Property Phone As String
End Class
```

**Test:**
```csharp
var results = SampleDataValidator.ValidateAll(
    "TempTestCases/Customer_Old.vb",
    "TempTestCases/Customer_New.vb"
);

// Roslyn semantic diff will detect property addition
```

## Summary

The TempTestCases folder provides a convenient way to:
- Test new scenarios quickly
- Debug validation issues
- Experiment with different file types
- Keep temporary test data out of version control
- Validate proprietary code samples privately

For permanent test cases that should be part of the test suite, use the `TestFixtures/` directories instead.
