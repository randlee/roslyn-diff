namespace RoslynDiff.Core.Comparison;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using RoslynDiff.Core.Models;

using SourceLocation = RoslynDiff.Core.Models.Location;

/// <summary>
/// Provides symbol matching capabilities for semantic comparison.
/// Matches symbols by content similarity, signature, and location context.
/// </summary>
public sealed class SymbolMatcher
{
    /// <summary>
    /// Represents a potential match between a removed and added symbol.
    /// </summary>
    /// <param name="RemovedChange">The removed change (from old tree).</param>
    /// <param name="AddedChange">The added change (from new tree).</param>
    /// <param name="Similarity">The body content similarity score (0.0 to 1.0).</param>
    /// <param name="IsSignatureMatch">Whether the signatures match (for methods).</param>
    /// <param name="IsSameParent">Whether both symbols have the same parent context.</param>
    public record MatchCandidate(
        Change RemovedChange,
        Change AddedChange,
        double Similarity,
        bool IsSignatureMatch,
        bool IsSameParent);

    /// <summary>
    /// The default minimum similarity threshold for considering two bodies as a rename.
    /// </summary>
    public const double DefaultSimilarityThreshold = 0.80;

    /// <summary>
    /// Finds potential rename matches between removed and added changes.
    /// </summary>
    /// <param name="removedChanges">Changes of type Removed.</param>
    /// <param name="addedChanges">Changes of type Added.</param>
    /// <param name="oldTree">The old syntax tree for context analysis.</param>
    /// <param name="newTree">The new syntax tree for context analysis.</param>
    /// <returns>A list of match candidates sorted by similarity (highest first).</returns>
    public IReadOnlyList<MatchCandidate> FindRenameMatches(
        IReadOnlyList<Change> removedChanges,
        IReadOnlyList<Change> addedChanges,
        SyntaxTree oldTree,
        SyntaxTree newTree)
    {
        var candidates = new List<MatchCandidate>();

        foreach (var removed in removedChanges)
        {
            foreach (var added in addedChanges)
            {
                // Must be same kind (class-to-class, method-to-method, etc.)
                if (removed.Kind != added.Kind)
                    continue;

                // Must have different names (otherwise it wouldn't be add/remove)
                if (removed.Name == added.Name)
                    continue;

                var similarity = CalculateBodySimilarity(removed, added);
                var isSignatureMatch = AreSignaturesCompatible(removed, added, oldTree, newTree);
                var isSameParent = HaveSameParentContext(removed, added, oldTree, newTree);

                // Only consider as rename if similarity is above threshold
                if (similarity >= DefaultSimilarityThreshold)
                {
                    candidates.Add(new MatchCandidate(removed, added, similarity, isSignatureMatch, isSameParent));
                }
            }
        }

        // Sort by similarity (highest first), then by whether same parent
        return candidates
            .OrderByDescending(c => c.Similarity)
            .ThenByDescending(c => c.IsSameParent)
            .ToList();
    }

    /// <summary>
    /// Finds potential move matches between removed and added changes.
    /// A move is when the same code (same name and body) appears in different locations.
    /// </summary>
    /// <param name="removedChanges">Changes of type Removed.</param>
    /// <param name="addedChanges">Changes of type Added.</param>
    /// <param name="oldTree">The old syntax tree for context analysis.</param>
    /// <param name="newTree">The new syntax tree for context analysis.</param>
    /// <returns>A list of match candidates for moves.</returns>
    public IReadOnlyList<MatchCandidate> FindMoveMatches(
        IReadOnlyList<Change> removedChanges,
        IReadOnlyList<Change> addedChanges,
        SyntaxTree oldTree,
        SyntaxTree newTree)
    {
        var candidates = new List<MatchCandidate>();

        foreach (var removed in removedChanges)
        {
            foreach (var added in addedChanges)
            {
                // Must be same kind
                if (removed.Kind != added.Kind)
                    continue;

                // Must have same name (for moves, the name stays the same)
                if (removed.Name != added.Name || removed.Name is null)
                    continue;

                var similarity = CalculateBodySimilarity(removed, added);
                var isSignatureMatch = AreSignaturesCompatible(removed, added, oldTree, newTree);
                var isSameParent = HaveSameParentContext(removed, added, oldTree, newTree);

                // For moves, we need high similarity and different parent
                if (similarity >= 0.95 && !isSameParent)
                {
                    candidates.Add(new MatchCandidate(removed, added, similarity, isSignatureMatch, isSameParent));
                }
            }
        }

        return candidates
            .OrderByDescending(c => c.Similarity)
            .ToList();
    }

    /// <summary>
    /// Calculates the similarity between two code bodies using normalized content comparison.
    /// </summary>
    /// <param name="removed">The removed change.</param>
    /// <param name="added">The added change.</param>
    /// <returns>A similarity score from 0.0 (completely different) to 1.0 (identical).</returns>
    public double CalculateBodySimilarity(Change removed, Change added)
    {
        var oldContent = removed.OldContent;
        var newContent = added.NewContent;

        if (string.IsNullOrEmpty(oldContent) || string.IsNullOrEmpty(newContent))
            return 0.0;

        // Extract bodies without the declaration line (which would contain the name)
        var oldBody = ExtractBodyWithoutDeclaration(oldContent, removed.Kind);
        var newBody = ExtractBodyWithoutDeclaration(newContent, added.Kind);

        if (string.IsNullOrWhiteSpace(oldBody) && string.IsNullOrWhiteSpace(newBody))
            return 1.0; // Both empty bodies are considered identical

        if (string.IsNullOrWhiteSpace(oldBody) || string.IsNullOrWhiteSpace(newBody))
            return 0.0;

        // Normalize whitespace for comparison
        var normalizedOld = NormalizeForComparison(oldBody);
        var normalizedNew = NormalizeForComparison(newBody);

        if (normalizedOld == normalizedNew)
            return 1.0;

        // Calculate Levenshtein-based similarity
        return CalculateLevenshteinSimilarity(normalizedOld, normalizedNew);
    }

    /// <summary>
    /// Checks if two symbols have compatible signatures (for methods).
    /// </summary>
    private bool AreSignaturesCompatible(Change removed, Change added, SyntaxTree oldTree, SyntaxTree newTree)
    {
        if (removed.Kind != ChangeKind.Method)
            return true; // Non-methods don't need signature matching

        var oldNode = FindNodeAtLocation(oldTree, removed.OldLocation);
        var newNode = FindNodeAtLocation(newTree, added.NewLocation);

        if (oldNode is null || newNode is null)
            return false;

        var oldSignature = GetMethodSignature(oldNode);
        var newSignature = GetMethodSignature(newNode);

        // For renames, we expect same parameter types
        return oldSignature == newSignature;
    }

    /// <summary>
    /// Checks if two symbols have the same parent context (same containing class/namespace).
    /// </summary>
    private bool HaveSameParentContext(Change removed, Change added, SyntaxTree oldTree, SyntaxTree newTree)
    {
        var oldNode = FindNodeAtLocation(oldTree, removed.OldLocation);
        var newNode = FindNodeAtLocation(newTree, added.NewLocation);

        if (oldNode is null || newNode is null)
            return false;

        var oldParentName = GetParentName(oldNode);
        var newParentName = GetParentName(newNode);

        return oldParentName == newParentName;
    }

    /// <summary>
    /// Gets the parent type/namespace name for a node.
    /// </summary>
    private static string? GetParentName(SyntaxNode node)
    {
        var parent = node.Parent;
        while (parent is not null)
        {
            switch (parent)
            {
                case TypeDeclarationSyntax typeDecl:
                    return typeDecl.Identifier.Text;
                case BaseNamespaceDeclarationSyntax ns:
                    return ns.Name.ToString();
            }
            parent = parent.Parent;
        }
        return null;
    }

    /// <summary>
    /// Gets the method signature (parameter types) from a node.
    /// </summary>
    private static string? GetMethodSignature(SyntaxNode node)
    {
        return node switch
        {
            MethodDeclarationSyntax method => GetParameterTypesSignature(method.ParameterList),
            ConstructorDeclarationSyntax ctor => GetParameterTypesSignature(ctor.ParameterList),
            _ => null
        };
    }

    private static string GetParameterTypesSignature(ParameterListSyntax? parameterList)
    {
        if (parameterList is null || parameterList.Parameters.Count == 0)
            return "()";

        var types = parameterList.Parameters
            .Select(p => p.Type?.ToString() ?? "var")
            .ToList();

        return $"({string.Join(", ", types)})";
    }

    /// <summary>
    /// Finds a syntax node at a given location in a tree.
    /// </summary>
    internal static SyntaxNode? FindNodeAtLocation(SyntaxTree tree, SourceLocation? location)
    {
        if (location is null)
            return null;

        var root = tree.GetRoot();
        // Convert 1-based line to 0-based
        var linePosition = new Microsoft.CodeAnalysis.Text.LinePosition(location.StartLine - 1, location.StartColumn - 1);
        var text = tree.GetText();
        var position = text.Lines.GetPosition(linePosition);

        var node = root.FindNode(new Microsoft.CodeAnalysis.Text.TextSpan(position, 0));

        // Walk up to find a structural node
        while (node is not null && !IsStructuralNode(node))
        {
            node = node.Parent;
        }

        return node;
    }

    private static bool IsStructuralNode(SyntaxNode node)
    {
        return node is BaseNamespaceDeclarationSyntax
            or ClassDeclarationSyntax
            or StructDeclarationSyntax
            or RecordDeclarationSyntax
            or InterfaceDeclarationSyntax
            or EnumDeclarationSyntax
            or MethodDeclarationSyntax
            or ConstructorDeclarationSyntax
            or PropertyDeclarationSyntax
            or FieldDeclarationSyntax;
    }

    /// <summary>
    /// Extracts the body portion of a declaration, excluding the declaration line itself.
    /// This allows comparing method bodies without being affected by name changes.
    /// </summary>
    private static string ExtractBodyWithoutDeclaration(string content, ChangeKind kind)
    {
        var tree = CSharpSyntaxTree.ParseText(content);
        var root = tree.GetRoot();

        // Find the first structural node
        var node = root.DescendantNodes().FirstOrDefault(IsStructuralNode);
        if (node is null)
            return content;

        return kind switch
        {
            ChangeKind.Method => ExtractMethodBody(node),
            ChangeKind.Property => ExtractPropertyBody(node),
            ChangeKind.Class => ExtractTypeBody(node),
            _ => content
        };
    }

    private static string ExtractMethodBody(SyntaxNode node)
    {
        if (node is MethodDeclarationSyntax method)
        {
            if (method.Body is not null)
                return method.Body.ToFullString();
            if (method.ExpressionBody is not null)
                return method.ExpressionBody.ToFullString();
        }
        else if (node is ConstructorDeclarationSyntax ctor)
        {
            if (ctor.Body is not null)
                return ctor.Body.ToFullString();
            if (ctor.ExpressionBody is not null)
                return ctor.ExpressionBody.ToFullString();
        }
        return string.Empty;
    }

    private static string ExtractPropertyBody(SyntaxNode node)
    {
        if (node is PropertyDeclarationSyntax property)
        {
            if (property.AccessorList is not null)
                return property.AccessorList.ToFullString();
            if (property.ExpressionBody is not null)
                return property.ExpressionBody.ToFullString();
        }
        return string.Empty;
    }

    private static string ExtractTypeBody(SyntaxNode node)
    {
        if (node is TypeDeclarationSyntax typeDecl)
        {
            // Extract members without the type declaration header
            var members = typeDecl.Members;
            return string.Join("\n", members.Select(m => m.ToFullString()));
        }
        return string.Empty;
    }

    /// <summary>
    /// Normalizes content for comparison by removing extra whitespace and standardizing format.
    /// </summary>
    private static string NormalizeForComparison(string content)
    {
        if (string.IsNullOrEmpty(content))
            return string.Empty;

        // Parse and normalize using Roslyn
        var tree = CSharpSyntaxTree.ParseText(content);
        var root = tree.GetRoot();

        // Get normalized text (standardized whitespace and formatting)
        var normalized = root.NormalizeWhitespace().ToFullString();

        // Remove leading/trailing whitespace from each line and collapse multiple spaces
        var lines = normalized.Split('\n')
            .Select(line => string.Join(" ", line.Split([' ', '\t'], StringSplitOptions.RemoveEmptyEntries)))
            .Where(line => !string.IsNullOrWhiteSpace(line));

        return string.Join("\n", lines);
    }

    /// <summary>
    /// Calculates similarity using Levenshtein distance algorithm.
    /// </summary>
    private static double CalculateLevenshteinSimilarity(string a, string b)
    {
        if (string.IsNullOrEmpty(a) && string.IsNullOrEmpty(b))
            return 1.0;

        if (string.IsNullOrEmpty(a) || string.IsNullOrEmpty(b))
            return 0.0;

        var distance = LevenshteinDistance(a, b);
        var maxLength = Math.Max(a.Length, b.Length);

        return 1.0 - ((double)distance / maxLength);
    }

    /// <summary>
    /// Calculates the Levenshtein edit distance between two strings.
    /// </summary>
    private static int LevenshteinDistance(string a, string b)
    {
        var lengthA = a.Length;
        var lengthB = b.Length;
        var distances = new int[lengthA + 1, lengthB + 1];

        for (var i = 0; i <= lengthA; i++)
            distances[i, 0] = i;

        for (var j = 0; j <= lengthB; j++)
            distances[0, j] = j;

        for (var i = 1; i <= lengthA; i++)
        {
            for (var j = 1; j <= lengthB; j++)
            {
                var cost = (a[i - 1] == b[j - 1]) ? 0 : 1;

                distances[i, j] = Math.Min(
                    Math.Min(
                        distances[i - 1, j] + 1,      // deletion
                        distances[i, j - 1] + 1),     // insertion
                    distances[i - 1, j - 1] + cost);  // substitution
            }
        }

        return distances[lengthA, lengthB];
    }
}
