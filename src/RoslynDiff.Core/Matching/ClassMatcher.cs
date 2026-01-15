namespace RoslynDiff.Core.Matching;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

/// <summary>
/// Matches classes between old and new syntax trees using various strategies.
/// Supports classes, records, structs, and interfaces.
/// </summary>
public sealed class ClassMatcher
{
    /// <summary>
    /// Finds a matching class in the new tree for the given old class.
    /// </summary>
    /// <param name="oldClass">The type declaration from the old syntax tree.</param>
    /// <param name="newTree">The new syntax tree to search for a match.</param>
    /// <param name="options">Options controlling the matching strategy.</param>
    /// <returns>A match result if found, or null if no match exists.</returns>
    public ClassMatchResult? FindMatch(
        TypeDeclarationSyntax oldClass,
        SyntaxTree newTree,
        ClassMatchOptions options)
    {
        ArgumentNullException.ThrowIfNull(oldClass);
        ArgumentNullException.ThrowIfNull(newTree);
        ArgumentNullException.ThrowIfNull(options);

        var newClasses = GetClasses(newTree, options.IncludeNestedClasses);
        var oldClassPath = GetClassPath(oldClass);

        return options.Strategy switch
        {
            ClassMatchStrategy.ExactName => FindByExactName(oldClass, oldClassPath, newClasses),
            ClassMatchStrategy.Interface => FindByInterface(oldClass, oldClassPath, newClasses, options.InterfaceName),
            ClassMatchStrategy.Similarity => FindBySimilarity(oldClass, oldClassPath, newClasses, options.SimilarityThreshold),
            ClassMatchStrategy.Auto => FindByAuto(oldClass, oldClassPath, newClasses, options),
            _ => throw new ArgumentOutOfRangeException(nameof(options), "Unknown match strategy")
        };
    }

    /// <summary>
    /// Gets all type declarations from a syntax tree.
    /// Includes classes, records, record structs, structs, and interfaces.
    /// </summary>
    /// <param name="tree">The syntax tree to extract declarations from.</param>
    /// <param name="includeNested">Whether to include nested type declarations.</param>
    /// <returns>A list of type declarations found in the tree.</returns>
    public IReadOnlyList<TypeDeclarationSyntax> GetClasses(SyntaxTree tree, bool includeNested = true)
    {
        ArgumentNullException.ThrowIfNull(tree);

        var root = tree.GetRoot();
        return GetClassesFromNode(root, includeNested);
    }

    /// <summary>
    /// Gets all type declarations from a syntax node.
    /// </summary>
    private IReadOnlyList<TypeDeclarationSyntax> GetClassesFromNode(SyntaxNode node, bool includeNested)
    {
        var result = new List<TypeDeclarationSyntax>();
        CollectTypeDeclarations(node, result, includeNested, isTopLevel: true);
        return result;
    }

    private void CollectTypeDeclarations(SyntaxNode node, List<TypeDeclarationSyntax> result, bool includeNested, bool isTopLevel)
    {
        foreach (var child in node.ChildNodes())
        {
            if (child is TypeDeclarationSyntax typeDecl)
            {
                result.Add(typeDecl);

                // Only recurse into nested types if requested
                if (includeNested)
                {
                    CollectTypeDeclarations(typeDecl, result, includeNested, isTopLevel: false);
                }
            }
            else if (child is BaseNamespaceDeclarationSyntax)
            {
                // Always recurse into namespaces
                CollectTypeDeclarations(child, result, includeNested, isTopLevel);
            }
        }
    }

    /// <summary>
    /// Gets the full path for a type declaration (e.g., "OuterClass.InnerClass").
    /// </summary>
    private static string GetClassPath(TypeDeclarationSyntax typeDecl)
    {
        var parts = new List<string>();
        SyntaxNode? current = typeDecl;

        while (current is not null)
        {
            if (current is TypeDeclarationSyntax type)
            {
                parts.Insert(0, GetNameWithoutGenerics(type));
            }
            current = current.Parent;
        }

        return string.Join(".", parts);
    }

    /// <summary>
    /// Gets the class name without generic type parameters.
    /// </summary>
    private static string GetNameWithoutGenerics(TypeDeclarationSyntax typeDecl)
    {
        return typeDecl.Identifier.Text;
    }

    /// <summary>
    /// Gets the generic type parameters as a string (e.g., "&lt;T, TResult&gt;").
    /// </summary>
    private static string GetGenericParameters(TypeDeclarationSyntax typeDecl)
    {
        if (typeDecl.TypeParameterList is null || typeDecl.TypeParameterList.Parameters.Count == 0)
            return string.Empty;

        return typeDecl.TypeParameterList.ToString();
    }

    #region ExactName Strategy

    private ClassMatchResult? FindByExactName(
        TypeDeclarationSyntax oldClass,
        string oldClassPath,
        IReadOnlyList<TypeDeclarationSyntax> newClasses)
    {
        // First try exact path match (handles nested classes)
        var exactPathMatch = newClasses.FirstOrDefault(nc => GetClassPath(nc) == oldClassPath);
        if (exactPathMatch is not null)
        {
            return CreateResult(oldClass, exactPathMatch, oldClassPath, ClassMatchStrategy.ExactName, 1.0);
        }

        // Fall back to name-only match (for non-nested or differently nested classes)
        var oldName = GetNameWithoutGenerics(oldClass);
        var nameMatch = newClasses.FirstOrDefault(nc => GetNameWithoutGenerics(nc) == oldName);
        if (nameMatch is not null)
        {
            return CreateResult(oldClass, nameMatch, oldClassPath, ClassMatchStrategy.ExactName, 1.0);
        }

        return null;
    }

    #endregion

    #region Interface Strategy

    private ClassMatchResult? FindByInterface(
        TypeDeclarationSyntax oldClass,
        string oldClassPath,
        IReadOnlyList<TypeDeclarationSyntax> newClasses,
        string? interfaceName)
    {
        if (string.IsNullOrWhiteSpace(interfaceName))
            return null;

        // Check if old class implements the interface
        if (!ImplementsInterface(oldClass, interfaceName))
            return null;

        // Find new classes that implement the same interface
        var matchingClasses = newClasses
            .Where(nc => ImplementsInterface(nc, interfaceName))
            .ToList();

        if (matchingClasses.Count == 0)
            return null;

        // If exactly one match, return it
        if (matchingClasses.Count == 1)
        {
            return CreateResult(oldClass, matchingClasses[0], oldClassPath, ClassMatchStrategy.Interface, 1.0);
        }

        // Multiple matches - prefer exact name match
        var exactNameMatch = matchingClasses.FirstOrDefault(nc => GetNameWithoutGenerics(nc) == GetNameWithoutGenerics(oldClass));
        if (exactNameMatch is not null)
        {
            return CreateResult(oldClass, exactNameMatch, oldClassPath, ClassMatchStrategy.Interface, 1.0);
        }

        // Otherwise return the most similar one
        return FindMostSimilar(oldClass, oldClassPath, matchingClasses, ClassMatchStrategy.Interface);
    }

    private static bool ImplementsInterface(TypeDeclarationSyntax typeDecl, string interfaceName)
    {
        if (typeDecl.BaseList is null)
            return false;

        return typeDecl.BaseList.Types.Any(baseType =>
        {
            var typeName = baseType.Type.ToString();
            // Handle both simple names and generic interfaces
            return typeName == interfaceName ||
                   typeName.StartsWith($"{interfaceName}<", StringComparison.Ordinal) ||
                   typeName.EndsWith($".{interfaceName}", StringComparison.Ordinal) ||
                   typeName.Contains($".{interfaceName}<", StringComparison.Ordinal);
        });
    }

    #endregion

    #region Similarity Strategy

    private ClassMatchResult? FindBySimilarity(
        TypeDeclarationSyntax oldClass,
        string oldClassPath,
        IReadOnlyList<TypeDeclarationSyntax> newClasses,
        double threshold)
    {
        ClassMatchResult? bestMatch = null;
        var bestSimilarity = 0.0;

        foreach (var newClass in newClasses)
        {
            var similarity = CalculateSimilarity(oldClass, newClass);
            if (similarity >= threshold && similarity > bestSimilarity)
            {
                bestSimilarity = similarity;
                bestMatch = CreateResult(oldClass, newClass, oldClassPath, ClassMatchStrategy.Similarity, similarity);
            }
        }

        return bestMatch;
    }

    private ClassMatchResult? FindMostSimilar(
        TypeDeclarationSyntax oldClass,
        string oldClassPath,
        IReadOnlyList<TypeDeclarationSyntax> candidates,
        ClassMatchStrategy matchedBy)
    {
        ClassMatchResult? bestMatch = null;
        var bestSimilarity = 0.0;

        foreach (var candidate in candidates)
        {
            var similarity = CalculateSimilarity(oldClass, candidate);
            if (similarity > bestSimilarity)
            {
                bestSimilarity = similarity;
                bestMatch = CreateResult(oldClass, candidate, oldClassPath, matchedBy, similarity);
            }
        }

        return bestMatch;
    }

    /// <summary>
    /// Calculates similarity between two type declarations using normalized Levenshtein distance.
    /// </summary>
    private double CalculateSimilarity(TypeDeclarationSyntax oldClass, TypeDeclarationSyntax newClass)
    {
        // Compare normalized content (without declaration line to avoid name bias)
        var oldContent = NormalizeContent(ExtractMemberContent(oldClass));
        var newContent = NormalizeContent(ExtractMemberContent(newClass));

        if (string.IsNullOrWhiteSpace(oldContent) && string.IsNullOrWhiteSpace(newContent))
            return 1.0;

        if (string.IsNullOrWhiteSpace(oldContent) || string.IsNullOrWhiteSpace(newContent))
            return 0.0;

        if (oldContent == newContent)
            return 1.0;

        return CalculateLevenshteinSimilarity(oldContent, newContent);
    }

    /// <summary>
    /// Extracts the member content of a type declaration (body without the declaration line).
    /// </summary>
    private static string ExtractMemberContent(TypeDeclarationSyntax typeDecl)
    {
        if (typeDecl.Members.Count == 0)
            return string.Empty;

        return string.Join("\n", typeDecl.Members.Select(m => m.ToFullString()));
    }

    /// <summary>
    /// Normalizes content for comparison by standardizing whitespace.
    /// </summary>
    private static string NormalizeContent(string content)
    {
        if (string.IsNullOrEmpty(content))
            return string.Empty;

        // Parse and normalize using Roslyn
        var tree = CSharpSyntaxTree.ParseText(content);
        var root = tree.GetRoot();
        var normalized = root.NormalizeWhitespace().ToFullString();

        // Collapse multiple whitespace
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

        // Use a single row to save memory for large strings
        var previousRow = new int[lengthB + 1];
        var currentRow = new int[lengthB + 1];

        for (var j = 0; j <= lengthB; j++)
            previousRow[j] = j;

        for (var i = 1; i <= lengthA; i++)
        {
            currentRow[0] = i;

            for (var j = 1; j <= lengthB; j++)
            {
                var cost = (a[i - 1] == b[j - 1]) ? 0 : 1;

                currentRow[j] = Math.Min(
                    Math.Min(
                        previousRow[j] + 1,      // deletion
                        currentRow[j - 1] + 1),  // insertion
                    previousRow[j - 1] + cost);  // substitution
            }

            // Swap rows
            (previousRow, currentRow) = (currentRow, previousRow);
        }

        return previousRow[lengthB];
    }

    #endregion

    #region Auto Strategy

    private ClassMatchResult? FindByAuto(
        TypeDeclarationSyntax oldClass,
        string oldClassPath,
        IReadOnlyList<TypeDeclarationSyntax> newClasses,
        ClassMatchOptions options)
    {
        // 1. Try exact name match first
        var exactMatch = FindByExactName(oldClass, oldClassPath, newClasses);
        if (exactMatch is not null)
            return exactMatch;

        // 2. Try interface match if interface name is specified
        if (!string.IsNullOrWhiteSpace(options.InterfaceName))
        {
            var interfaceMatch = FindByInterface(oldClass, oldClassPath, newClasses, options.InterfaceName);
            if (interfaceMatch is not null)
                return interfaceMatch;
        }

        // 3. Fall back to similarity match
        return FindBySimilarity(oldClass, oldClassPath, newClasses, options.SimilarityThreshold);
    }

    #endregion

    #region Result Creation

    private static ClassMatchResult CreateResult(
        TypeDeclarationSyntax oldClass,
        TypeDeclarationSyntax newClass,
        string oldClassPath,
        ClassMatchStrategy matchedBy,
        double similarity)
    {
        return new ClassMatchResult
        {
            OldClass = oldClass,
            NewClass = newClass,
            MatchedBy = matchedBy,
            Similarity = similarity,
            OldClassPath = oldClassPath,
            NewClassPath = GetClassPath(newClass)
        };
    }

    #endregion

    #region Partial Class Support

    /// <summary>
    /// Finds all partial class declarations that match the given class name.
    /// </summary>
    /// <param name="className">The class name to find partial declarations for.</param>
    /// <param name="tree">The syntax tree to search.</param>
    /// <returns>All partial declarations of the class.</returns>
    public IReadOnlyList<TypeDeclarationSyntax> FindPartialDeclarations(string className, SyntaxTree tree)
    {
        ArgumentNullException.ThrowIfNull(className);
        ArgumentNullException.ThrowIfNull(tree);

        var allClasses = GetClasses(tree, includeNested: true);

        return allClasses
            .Where(c => GetNameWithoutGenerics(c) == className && c.Modifiers.Any(SyntaxKind.PartialKeyword))
            .ToList();
    }

    /// <summary>
    /// Determines if a type declaration is a partial class.
    /// </summary>
    public static bool IsPartialClass(TypeDeclarationSyntax typeDecl)
    {
        ArgumentNullException.ThrowIfNull(typeDecl);
        return typeDecl.Modifiers.Any(SyntaxKind.PartialKeyword);
    }

    #endregion

    #region Type Information

    /// <summary>
    /// Gets a description of the type kind (class, record, struct, interface, etc.).
    /// </summary>
    public static string GetTypeKind(TypeDeclarationSyntax typeDecl)
    {
        ArgumentNullException.ThrowIfNull(typeDecl);

        return typeDecl switch
        {
            RecordDeclarationSyntax record when record.ClassOrStructKeyword.IsKind(SyntaxKind.StructKeyword) => "record struct",
            RecordDeclarationSyntax => "record",
            ClassDeclarationSyntax => "class",
            StructDeclarationSyntax => "struct",
            InterfaceDeclarationSyntax => "interface",
            _ => "type"
        };
    }

    /// <summary>
    /// Gets the full name including generic parameters (e.g., "MyClass&lt;T, TResult&gt;").
    /// </summary>
    public static string GetFullTypeName(TypeDeclarationSyntax typeDecl)
    {
        ArgumentNullException.ThrowIfNull(typeDecl);

        var name = GetNameWithoutGenerics(typeDecl);
        var generics = GetGenericParameters(typeDecl);

        return string.IsNullOrEmpty(generics) ? name : $"{name}{generics}";
    }

    #endregion
}
