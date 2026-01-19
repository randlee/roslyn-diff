namespace RoslynDiff.Core.Models;

/// <summary>
/// Categorizes the impact level of a code change.
/// </summary>
public enum ChangeImpact
{
    /// <summary>
    /// Changes to public API surface that could break external consumers.
    /// </summary>
    BreakingPublicApi,

    /// <summary>
    /// Changes to internal API surface that could break internal consumers.
    /// </summary>
    BreakingInternalApi,

    /// <summary>
    /// Changes that don't affect code execution or API contracts.
    /// </summary>
    NonBreaking,

    /// <summary>
    /// Whitespace-only or comment-only changes.
    /// </summary>
    FormattingOnly
}
