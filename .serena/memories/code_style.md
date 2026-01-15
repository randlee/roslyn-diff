# Code Style and Conventions

## Namespace Style
- File-scoped namespaces (no braces): `namespace RoslynDiff.Output;`

## Documentation
- XML documentation comments for public APIs
- Use `/// <inheritdoc/>` for interface implementations
- Summary sections for classes and methods

## Code Patterns
- Records for immutable data models
- Collection expressions `[]` for initializing collections
- Pattern matching with `is` and `switch` expressions
- Async/await with Task-based async pattern

## Testing Style
- xUnit with `[Fact]` attributes
- FluentAssertions for assertions
- Arrange/Act/Assert pattern in comments
- Test class naming: `{ClassName}Tests`
- Test method naming: `{MethodName}_{Condition}_{ExpectedResult}`

## Null Handling
- Nullable reference types enabled
- Use `??=` for null-coalescing assignment
- Use `?.` for null-conditional access
