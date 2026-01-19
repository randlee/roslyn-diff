# SampleDataValidator Architecture

## Component Overview

```
┌─────────────────────────────────────────────────────────────────┐
│                    SampleDataValidator                          │
│                   (Workstream F - Core)                         │
│                                                                 │
│  ┌─────────────────────────────────────────────────────────┐   │
│  │ ValidateAll(oldFile, newFile, options)                  │   │
│  │   • Main entry point                                    │   │
│  │   • Orchestrates all validations                        │   │
│  │   • Returns IEnumerable<TestResult>                     │   │
│  └─────────────────────────────────────────────────────────┘   │
│                             │                                   │
│         ┌───────────────────┼───────────────────┐              │
│         │                   │                   │              │
│         ▼                   ▼                   ▼              │
│  ┌──────────────┐  ┌──────────────┐  ┌─────────────────┐     │
│  │   Validate   │  │   Validate   │  │    Validate     │     │
│  │ LineNumber   │  │    JSON      │  │      HTML       │     │
│  │  Integrity   │  │  Consistency │  │   Consistency   │     │
│  └──────────────┘  └──────────────┘  └─────────────────┘     │
│         │                   │                   │              │
│         └───────────────────┼───────────────────┘              │
│                             ▼                                  │
│                 ┌─────────────────────┐                        │
│                 │ValidateCrossFormat  │                        │
│                 │   Consistency       │                        │
│                 └─────────────────────┘                        │
└─────────────────────────────────────────────────────────────────┘
                             │
        ┌────────────────────┼────────────────────┐
        │                    │                    │
        ▼                    ▼                    ▼
┌──────────────┐    ┌──────────────┐    ┌──────────────┐
│ Workstream D │    │ Workstream E │    │  roslyn-diff │
│   Parsers    │    │  Validators  │    │     CLI      │
└──────────────┘    └──────────────┘    └──────────────┘
        │                    │                    │
        │                    │                    │
        ▼                    ▼                    ▼
┌──────────────┐    ┌──────────────┐    ┌──────────────┐
│JsonParser    │    │JsonValidator │    │ JSON Output  │
│HtmlParser    │    │HtmlValidator │    │ HTML Output  │
│TextParser    │    │LineValidator │    │ Text Output  │
│UnifiedParser │    │GitValidator  │    │ Git Output   │
└──────────────┘    └──────────────┘    └──────────────┘
```

## Data Flow

```
┌─────────────┐
│  Old File   │
│  New File   │
└──────┬──────┘
       │
       ▼
┌─────────────────────────────────────┐
│ SampleDataValidator.ValidateAll()  │
└──────┬──────────────────────────────┘
       │
       ├─► GenerateOutput(format: json)  ──► Parse ──► Validate ──┐
       │                                                           │
       ├─► GenerateOutput(format: html)  ──► Parse ──► Validate ──┤
       │                                                           │
       ├─► GenerateOutput(format: text)  ──► Parse ──► Validate ──┤
       │                                                           │
       └─► GenerateOutput(format: git)   ──► Parse ──► Validate ──┘
                                                              │
                                                              ▼
                                            ┌────────────────────────────┐
                                            │ Aggregate All TestResults  │
                                            └────────────────────────────┘
                                                              │
                                                              ▼
                                            ┌────────────────────────────┐
                                            │  Return Results to Caller  │
                                            └────────────────────────────┘
```

## Method Dependency Graph

```
ValidateAll()
    │
    ├─► ValidateLineNumberIntegrity()
    │       ├─► GenerateOutput("json")
    │       │       └─► FindRoslynDiffCli()
    │       │       └─► Process.Start()
    │       ├─► JsonOutputParser.ExtractLineRanges()
    │       ├─► LineNumberValidator.ValidateRanges()
    │       └─► CleanupTempFiles()
    │
    ├─► ValidateJsonConsistency()
    │       ├─► GenerateOutput("json")
    │       ├─► JsonOutputParser.CanParse()
    │       ├─► JsonOutputParser.ExtractLineNumbers()
    │       ├─► JsonValidator.ValidateLineNumberIntegrity()
    │       └─► LineNumberValidator methods
    │
    ├─► ValidateHtmlConsistency()
    │       ├─► GenerateOutput("html")
    │       ├─► HtmlOutputParser.CanParse()
    │       ├─► HtmlOutputParser.ExtractLineRanges()
    │       ├─► HtmlValidator.ValidateAll()
    │       └─► LineNumberValidator methods
    │
    └─► ValidateCrossFormatConsistency()
            ├─► GenerateOutput("json")
            ├─► GenerateOutput("html")
            ├─► GenerateOutput("text")
            ├─► GenerateOutput("git") [conditional]
            ├─► Parse all formats
            ├─► Compare line ranges across formats
            └─► Compare line number sets
```

## Class Relationships

```
┌────────────────────────────────────┐
│ SampleDataValidatorOptions         │
├────────────────────────────────────┤
│ + IgnoreTimestamps: bool           │
│ + DiffMode: DiffMode               │
│ + IncludeExternalTools: bool       │
│ + TempOutputDirectory: string?     │
│ + PreserveTempFiles: bool          │
│ + CliTimeoutMs: int                │
│ + RoslynDiffCliPath: string?       │
└────────────────────────────────────┘
               │
               │ used by
               ▼
┌────────────────────────────────────┐
│ SampleDataValidator (static)       │
├────────────────────────────────────┤
│ + ValidateAll()                    │
│ + ValidateLineNumberIntegrity()    │
│ + ValidateJsonConsistency()        │
│ + ValidateHtmlConsistency()        │
│ + ValidateCrossFormatConsistency() │
│ + AggregateResults()               │
│ - GenerateOutput()                 │
│ - FindRoslynDiffCli()              │
│ - CleanupTempFiles()               │
│ - ShouldGenerateUnifiedDiff()      │
└────────────────────────────────────┘
               │
               │ returns
               ▼
┌────────────────────────────────────┐
│ TestResult (record)                │
├────────────────────────────────────┤
│ + Passed: bool                     │
│ + Context: string                  │
│ + Message: string                  │
│ + Issues: IReadOnlyList<string>    │
│ + Pass(): TestResult               │
│ + Fail(): TestResult               │
└────────────────────────────────────┘
```

## Integration Points

### With Workstream D (Parsers)
```
SampleDataValidator
        │
        ├─► JsonOutputParser
        │       ├─ FormatName: string
        │       ├─ CanParse(content): bool
        │       ├─ ExtractLineNumbers(content): IEnumerable<int>
        │       └─ ExtractLineRanges(content): IEnumerable<LineRange>
        │
        ├─► HtmlOutputParser
        │       ├─ FormatName: string
        │       ├─ CanParse(content): bool
        │       ├─ ExtractLineNumbers(content): IEnumerable<int>
        │       └─ ExtractLineRanges(content): IEnumerable<LineRange>
        │
        ├─► TextOutputParser
        │       ├─ FormatName: string
        │       ├─ CanParse(content): bool
        │       ├─ ExtractLineNumbers(content): IEnumerable<int>
        │       └─ ExtractLineRanges(content): IEnumerable<LineRange>
        │
        └─► UnifiedDiffParser
                ├─ FormatName: string
                ├─ CanParse(content): bool
                ├─ ExtractLineNumbers(content): IEnumerable<int>
                ├─ ExtractLineRanges(content): IEnumerable<LineRange>
                └─ ExtractHunkHeaders(content): List<HunkHeader>
```

### With Workstream E (Validators)
```
SampleDataValidator
        │
        ├─► JsonValidator
        │       └─ ValidateLineNumberIntegrity(content): TestResult
        │
        ├─► HtmlValidator
        │       └─ ValidateAll(content): IEnumerable<TestResult>
        │
        └─► LineNumberValidator (Sprint 1)
                ├─ ValidateNoOverlaps(ranges, context): TestResult
                ├─ ValidateNoDuplicates(lines, context): TestResult
                ├─ ValidateRangesSequential(ranges, context): TestResult
                └─ ValidateRanges(ranges, context, sequential): TestResult
```

## Process Flow Example

```
User Code:
    var results = SampleDataValidator.ValidateAll("old.cs", "new.cs");

SampleDataValidator:
    1. Check files exist
    2. Create options (or use provided)
    3. Run ValidateLineNumberIntegrity()
       ├─ Generate JSON: roslyn-diff file old.cs new.cs --output json
       ├─ Generate HTML: roslyn-diff file old.cs new.cs --output html
       ├─ Generate Text: roslyn-diff file old.cs new.cs --output text
       ├─ Parse each output
       ├─ Validate line numbers in each
       └─ Return results
    4. Run ValidateJsonConsistency()
       ├─ Generate JSON output
       ├─ Verify parseable
       ├─ Call JsonValidator
       └─ Return results
    5. Run ValidateHtmlConsistency()
       ├─ Generate HTML output
       ├─ Verify parseable
       ├─ Call HtmlValidator
       └─ Return results
    6. Run ValidateCrossFormatConsistency()
       ├─ Generate all formats
       ├─ Parse all formats
       ├─ Compare line ranges
       ├─ Compare line number sets
       └─ Return results
    7. Aggregate and return all results

User Code:
    foreach (var result in results)
        Console.WriteLine(result);
```

## Error Handling Strategy

```
┌─────────────────────────┐
│  Validation Method      │
└───────────┬─────────────┘
            │
            ├─► Try to generate outputs
            │       ├─ CLI not found → Return Fail result
            │       ├─ Timeout → Return Fail result
            │       └─ Exit code ≠ 0 → Return Fail result
            │
            ├─► Try to parse outputs
            │       ├─ Invalid format → Return Fail result
            │       └─ Parse error → Return Fail result
            │
            ├─► Run validations
            │       └─ Any validation fails → Include in results
            │
            └─► Always cleanup temp files (in finally block)
```

## Extension Points

```
New Validation Method:
    public static IEnumerable<TestResult> ValidateNewAspect(
        string oldFile, 
        string newFile, 
        SampleDataValidatorOptions? options = null)
    {
        // 1. Generate outputs
        // 2. Parse outputs
        // 3. Validate
        // 4. Return results
        // 5. Cleanup in finally
    }

New Parser (Workstream D):
    • Implement ILineNumberParser
    • Add to parser instantiation in validation methods

New Validator (Workstream E):
    • Create validator class
    • Call from appropriate validation method
```

## Performance Considerations

```
Current:
    Sequential execution
    ├─ Generate all formats (one by one)
    ├─ Parse each format
    ├─ Validate each format
    └─ Total time: Sum of all operations

Future Optimization:
    Parallel execution
    ├─ Generate all formats (parallel)
    ├─ Parse each format (parallel)
    ├─ Validate each format (parallel)
    └─ Total time: Max of all operations
```

## Testing Strategy

```
Unit Tests (SampleDataValidatorTests):
    ├─ Test all public methods
    ├─ Test with valid files
    ├─ Test with null parameters
    ├─ Test with missing files
    ├─ Test result aggregation
    └─ Test default options

Integration Tests:
    ├─ Test with real sample files
    ├─ Test CLI invocation
    ├─ Test all output formats
    ├─ Test cross-workstream integration
    └─ Test error scenarios

Examples (SampleDataValidatorUsageExample):
    ├─ Basic usage
    ├─ Custom options
    ├─ Individual validations
    ├─ Batch processing
    └─ Error handling
```
