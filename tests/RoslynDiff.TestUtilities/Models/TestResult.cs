namespace RoslynDiff.TestUtilities.Models;

/// <summary>
/// Represents the result of a validation or test operation.
/// </summary>
public record TestResult
{
    /// <summary>
    /// Gets a value indicating whether the test passed.
    /// </summary>
    public bool Passed { get; init; }

    /// <summary>
    /// Gets the context or description of what was being tested.
    /// </summary>
    public string Context { get; init; } = string.Empty;

    /// <summary>
    /// Gets the detailed message describing the test result.
    /// </summary>
    public string Message { get; init; } = string.Empty;

    /// <summary>
    /// Gets the collection of specific issues or failures found during validation.
    /// </summary>
    public IReadOnlyList<string> Issues { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Creates a passing test result.
    /// </summary>
    /// <param name="context">The context or description of what was tested.</param>
    /// <param name="message">Optional success message.</param>
    /// <returns>A <see cref="TestResult"/> indicating success.</returns>
    public static TestResult Pass(string context, string? message = null)
    {
        return new TestResult
        {
            Passed = true,
            Context = context,
            Message = message ?? "Validation passed"
        };
    }

    /// <summary>
    /// Creates a failing test result.
    /// </summary>
    /// <param name="context">The context or description of what was tested.</param>
    /// <param name="message">The failure message.</param>
    /// <param name="issues">Optional collection of specific issues found.</param>
    /// <returns>A <see cref="TestResult"/> indicating failure.</returns>
    public static TestResult Fail(string context, string message, IEnumerable<string>? issues = null)
    {
        return new TestResult
        {
            Passed = false,
            Context = context,
            Message = message,
            Issues = issues?.ToList() as IReadOnlyList<string> ?? Array.Empty<string>()
        };
    }

    /// <summary>
    /// Returns a string representation of this test result.
    /// </summary>
    public override string ToString()
    {
        var status = Passed ? "PASS" : "FAIL";
        var result = $"[{status}] {Context}: {Message}";
        
        if (Issues.Any())
        {
            result += Environment.NewLine + "  Issues:" + Environment.NewLine;
            result += string.Join(Environment.NewLine, Issues.Select(i => $"    - {i}"));
        }
        
        return result;
    }

    // Workstream B required properties (for compatibility)
    public string? TestName => Context;
    public string? Details => Message;
    public string? SourceFile { get; init; }
    public int? LineNumber { get; init; }
}
