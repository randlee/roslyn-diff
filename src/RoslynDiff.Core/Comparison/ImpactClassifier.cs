using RoslynDiff.Core.Models;

namespace RoslynDiff.Core.Comparison;

/// <summary>
/// Classifies the impact level of code changes based on visibility and change type.
/// </summary>
public static class ImpactClassifier
{
    /// <summary>
    /// Classifies the impact of a change.
    /// </summary>
    /// <param name="changeType">The type of change (Added, Removed, Modified, Renamed, Moved).</param>
    /// <param name="symbolKind">The kind of symbol being changed.</param>
    /// <param name="visibility">The visibility of the symbol.</param>
    /// <param name="isSignatureChange">Whether this is a signature change vs body-only change.</param>
    /// <param name="isSameScopeMove">For moves, whether it's within the same containing type.</param>
    /// <returns>A tuple of (impact level, list of caveats).</returns>
    public static (ChangeImpact Impact, List<string> Caveats) Classify(
        ChangeType changeType,
        SymbolKind symbolKind,
        Visibility visibility,
        bool isSignatureChange = true,
        bool isSameScopeMove = false)
    {
        var caveats = new List<string>();

        // Renamed symbols
        if (changeType == ChangeType.Renamed)
        {
            return ClassifyRename(symbolKind, visibility, caveats);
        }

        // Moved symbols
        if (changeType == ChangeType.Moved)
        {
            return ClassifyMove(visibility, isSameScopeMove, caveats);
        }

        // Added/Removed/Modified
        return ClassifyModification(changeType, visibility, isSignatureChange, caveats);
    }

    private static (ChangeImpact, List<string>) ClassifyRename(
        SymbolKind symbolKind,
        Visibility visibility,
        List<string> caveats)
    {
        // Public API renames are breaking
        if (VisibilityExtractor.IsPublicApi(visibility))
        {
            return (ChangeImpact.BreakingPublicApi, caveats);
        }

        // Internal API renames are breaking for internal consumers
        if (VisibilityExtractor.IsInternalApi(visibility))
        {
            return (ChangeImpact.BreakingInternalApi, caveats);
        }

        // Parameter renames have a caveat about named arguments
        if (symbolKind == SymbolKind.Parameter)
        {
            caveats.Add("Parameter rename may break callers using named arguments");
        }

        // Private member renames have a caveat about reflection/serialization
        if (visibility == Visibility.Private && symbolKind is SymbolKind.Field or SymbolKind.Property or SymbolKind.Method)
        {
            caveats.Add("Private member rename may break reflection or serialization");
        }

        return (ChangeImpact.NonBreaking, caveats);
    }

    private static (ChangeImpact, List<string>) ClassifyMove(
        Visibility visibility,
        bool isSameScopeMove,
        List<string> caveats)
    {
        // Same-scope moves are non-breaking (just reordering)
        if (isSameScopeMove)
        {
            caveats.Add("Code reordering within same scope");
            return (ChangeImpact.NonBreaking, caveats);
        }

        // Cross-scope moves follow visibility rules
        if (VisibilityExtractor.IsPublicApi(visibility))
        {
            return (ChangeImpact.BreakingPublicApi, caveats);
        }

        if (VisibilityExtractor.IsInternalApi(visibility))
        {
            return (ChangeImpact.BreakingInternalApi, caveats);
        }

        return (ChangeImpact.NonBreaking, caveats);
    }

    private static (ChangeImpact, List<string>) ClassifyModification(
        ChangeType changeType,
        Visibility visibility,
        bool isSignatureChange,
        List<string> caveats)
    {
        // Body-only modifications are non-breaking
        if (changeType == ChangeType.Modified && !isSignatureChange)
        {
            return (ChangeImpact.NonBreaking, caveats);
        }

        // Signature changes and additions/removals follow visibility rules
        if (VisibilityExtractor.IsPublicApi(visibility))
        {
            return (ChangeImpact.BreakingPublicApi, caveats);
        }

        if (VisibilityExtractor.IsInternalApi(visibility))
        {
            return (ChangeImpact.BreakingInternalApi, caveats);
        }

        // Private changes are non-breaking
        return (ChangeImpact.NonBreaking, caveats);
    }

    /// <summary>
    /// Determines if a change should be considered formatting-only.
    /// </summary>
    public static bool IsFormattingOnly(string? oldContent, string? newContent)
    {
        if (oldContent == null || newContent == null)
            return false;

        // Normalize whitespace and compare
        var normalizedOld = NormalizeWhitespace(oldContent);
        var normalizedNew = NormalizeWhitespace(newContent);

        return normalizedOld == normalizedNew;
    }

    private static string NormalizeWhitespace(string content)
    {
        // Remove all whitespace for comparison
        return string.Concat(content.Where(c => !char.IsWhiteSpace(c)));
    }
}
