# Suggested Commands for roslyn-diff

## Building
```bash
dotnet build              # Build the entire solution
dotnet build src/RoslynDiff.Output/RoslynDiff.Output.csproj  # Build specific project
```

## Testing
```bash
dotnet test               # Run all tests
dotnet test --no-build    # Run tests without rebuilding
dotnet test --filter "FullyQualifiedName~PlainTextFormatterTests"  # Run specific tests
```

## Cleaning
```bash
dotnet clean              # Clean build artifacts
```

## Running the CLI
```bash
dotnet run --project src/RoslynDiff.Cli -- <args>
```

## Git
```bash
git status                # Check modified files
git diff                  # See changes
```
