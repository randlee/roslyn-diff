namespace RoslynDiff.Core.Matching;

using Microsoft.CodeAnalysis.CSharp.Syntax;

/// <summary>
/// Represents the result of matching a class between two syntax trees.
/// </summary>
public sealed class ClassMatchResult
{
    /// <summary>
    /// Gets the type declaration from the old syntax tree that was matched.
    /// </summary>
    public required TypeDeclarationSyntax OldClass { get; init; }

    /// <summary>
    /// Gets the type declaration from the new syntax tree that matches the old class.
    /// </summary>
    public required TypeDeclarationSyntax NewClass { get; init; }

    /// <summary>
    /// Gets the strategy that was used to match the classes.
    /// </summary>
    public required ClassMatchStrategy MatchedBy { get; init; }

    /// <summary>
    /// Gets the similarity score (0.0-1.0) between the matched classes.
    /// For ExactName and Interface strategies, this will be 1.0 if names match exactly.
    /// For Similarity strategy, this is the calculated content similarity.
    /// </summary>
    public double Similarity { get; init; } = 1.0;

    /// <summary>
    /// Gets the full path of the old class (e.g., "OuterClass.InnerClass" for nested classes).
    /// </summary>
    public string OldClassPath { get; init; } = string.Empty;

    /// <summary>
    /// Gets the full path of the new class (e.g., "OuterClass.InnerClass" for nested classes).
    /// </summary>
    public string NewClassPath { get; init; } = string.Empty;

    /// <summary>
    /// Gets the old class name without generic type parameters.
    /// </summary>
    public string OldClassName => GetNameWithoutGenerics(OldClass);

    /// <summary>
    /// Gets the new class name without generic type parameters.
    /// </summary>
    public string NewClassName => GetNameWithoutGenerics(NewClass);

    /// <summary>
    /// Gets whether the match was exact (same name) or approximate (similarity-based).
    /// </summary>
    public bool IsExactMatch => MatchedBy == ClassMatchStrategy.ExactName || Similarity >= 1.0;

    private static string GetNameWithoutGenerics(TypeDeclarationSyntax typeDecl)
    {
        return typeDecl.Identifier.Text;
    }
}
