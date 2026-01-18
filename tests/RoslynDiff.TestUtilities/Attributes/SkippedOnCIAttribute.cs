namespace RoslynDiff.TestUtilities.Attributes;

/// <summary>
/// Marks a test to be skipped when running in a CI environment.
/// </summary>
/// <remarks>
/// This attribute is useful for tests that depend on external tools or resources
/// that may not be available in CI environments (e.g., diff, git).
/// </remarks>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
public sealed class SkippedOnCIAttribute : Attribute
{
    /// <summary>
    /// Gets the reason why the test should be skipped in CI.
    /// </summary>
    public string Reason { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="SkippedOnCIAttribute"/> class.
    /// </summary>
    /// <param name="reason">The reason for skipping in CI. Default is "Requires external tools not available in CI".</param>
    public SkippedOnCIAttribute(string? reason = null)
    {
        Reason = reason ?? "Requires external tools not available in CI";
    }

    /// <summary>
    /// Checks if the current environment is a CI environment.
    /// </summary>
    /// <returns>True if running in CI, false otherwise.</returns>
    public static bool IsRunningInCI()
    {
        return !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("CI")) ||
               !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("GITHUB_ACTIONS")) ||
               !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("BUILD_ID")) ||
               !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("JENKINS_URL"));
    }
}
