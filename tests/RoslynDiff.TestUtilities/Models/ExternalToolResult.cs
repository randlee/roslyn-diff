using RoslynDiff.TestUtilities.Parsers;

namespace RoslynDiff.TestUtilities.Models;

/// <summary>
/// Represents the result of executing an external diff tool.
/// Contains exit codes, output, errors, and optional parsed results.
/// </summary>
public record ExternalToolResult
{
    /// <summary>
    /// Gets the exit code returned by the tool.
    /// </summary>
    /// <remarks>
    /// For diff tools:
    /// - 0: No differences found
    /// - 1: Differences found
    /// - 2+: Error occurred
    /// </remarks>
    public int ExitCode { get; init; }

    /// <summary>
    /// Gets the standard output from the tool (typically the diff content).
    /// </summary>
    public string Output { get; init; } = string.Empty;

    /// <summary>
    /// Gets the standard error output from the tool.
    /// </summary>
    public string Error { get; init; } = string.Empty;

    /// <summary>
    /// Gets a value indicating whether the tool executed successfully.
    /// </summary>
    /// <remarks>
    /// For diff tools, exit code 1 (differences found) is considered success.
    /// </remarks>
    public bool Success => ExitCode <= 1;

    /// <summary>
    /// Gets the name of the tool that was executed.
    /// </summary>
    public string ToolName { get; init; } = string.Empty;

    /// <summary>
    /// Gets the execution time in milliseconds.
    /// </summary>
    public long ExecutionTimeMs { get; init; }

    /// <summary>
    /// Gets the parsed result from the diff output, if parsing was performed.
    /// </summary>
    public ParsedDiffResult? ParsedResult { get; init; }

    /// <summary>
    /// Gets any exception that occurred during execution.
    /// </summary>
    public Exception? Exception { get; init; }

    /// <summary>
    /// Gets additional metadata about the execution.
    /// </summary>
    public Dictionary<string, string> Metadata { get; init; } = new();

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    /// <param name="toolName">The name of the tool.</param>
    /// <param name="exitCode">The exit code.</param>
    /// <param name="output">The standard output.</param>
    /// <param name="executionTimeMs">The execution time in milliseconds.</param>
    /// <returns>An <see cref="ExternalToolResult"/> indicating success.</returns>
    public static ExternalToolResult CreateSuccess(string toolName, int exitCode, string output, long executionTimeMs)
    {
        return new ExternalToolResult
        {
            ToolName = toolName,
            ExitCode = exitCode,
            Output = output,
            ExecutionTimeMs = executionTimeMs
        };
    }

    /// <summary>
    /// Creates a failure result.
    /// </summary>
    /// <param name="toolName">The name of the tool.</param>
    /// <param name="exitCode">The exit code.</param>
    /// <param name="error">The error message.</param>
    /// <param name="executionTimeMs">The execution time in milliseconds.</param>
    /// <param name="exception">Optional exception that occurred.</param>
    /// <returns>An <see cref="ExternalToolResult"/> indicating failure.</returns>
    public static ExternalToolResult CreateFailure(string toolName, int exitCode, string error, long executionTimeMs, Exception? exception = null)
    {
        return new ExternalToolResult
        {
            ToolName = toolName,
            ExitCode = exitCode,
            Error = error,
            ExecutionTimeMs = executionTimeMs,
            Exception = exception
        };
    }

    /// <summary>
    /// Creates a result from an exception.
    /// </summary>
    /// <param name="toolName">The name of the tool.</param>
    /// <param name="exception">The exception that occurred.</param>
    /// <returns>An <see cref="ExternalToolResult"/> representing the exception.</returns>
    public static ExternalToolResult FromException(string toolName, Exception exception)
    {
        return new ExternalToolResult
        {
            ToolName = toolName,
            ExitCode = -1,
            Error = exception.Message,
            Exception = exception,
            ExecutionTimeMs = 0
        };
    }

    /// <summary>
    /// Creates a result with parsed output.
    /// </summary>
    /// <param name="result">The base result.</param>
    /// <param name="parsedResult">The parsed diff result.</param>
    /// <returns>A new <see cref="ExternalToolResult"/> with parsed data.</returns>
    public static ExternalToolResult WithParsedResult(ExternalToolResult result, ParsedDiffResult parsedResult)
    {
        return result with { ParsedResult = parsedResult };
    }

    /// <summary>
    /// Returns a string representation of this result.
    /// </summary>
    public override string ToString()
    {
        var status = Success ? "SUCCESS" : "FAILURE";
        var result = $"[{status}] {ToolName} (ExitCode: {ExitCode}, Duration: {ExecutionTimeMs}ms)";

        if (!string.IsNullOrEmpty(Error))
        {
            result += $"\n  Error: {Error}";
        }

        if (ParsedResult != null)
        {
            result += $"\n  Parsed: {ParsedResult.Changes.Count} changes";
        }

        return result;
    }
}
