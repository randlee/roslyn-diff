namespace RoslynDiff.Core.Comparison;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using RoslynDiff.Core.Models;

// Alias to avoid ambiguity with Microsoft.CodeAnalysis.Location
using SourceLocation = RoslynDiff.Core.Models.Location;

/// <summary>
/// Matches syntax nodes between old and new syntax trees for comparison.
/// </summary>
/// <remarks>
/// Uses name-based matching for named declarations (classes, methods, properties, fields)
/// and position-based matching as a fallback for unnamed nodes.
/// </remarks>
public sealed class NodeMatcher
{
    /// <summary>
    /// Represents the result of matching nodes between two syntax trees.
    /// </summary>
    /// <param name="MatchedPairs">Pairs of nodes that were matched between old and new trees.</param>
    /// <param name="UnmatchedOld">Nodes from the old tree that have no match in the new tree (removals).</param>
    /// <param name="UnmatchedNew">Nodes from the new tree that have no match in the old tree (additions).</param>
    public record MatchResult(
        IReadOnlyList<(SyntaxNode Old, SyntaxNode New)> MatchedPairs,
        IReadOnlyList<SyntaxNode> UnmatchedOld,
        IReadOnlyList<SyntaxNode> UnmatchedNew);

    /// <summary>
    /// Represents a structural node extracted from a syntax tree with metadata.
    /// </summary>
    /// <param name="Node">The syntax node.</param>
    /// <param name="Name">The name of the declaration, if applicable.</param>
    /// <param name="Kind">The kind of code element this node represents.</param>
    /// <param name="Signature">An optional signature for methods/constructors to distinguish overloads.</param>
    public record NodeInfo(SyntaxNode Node, string? Name, ChangeKind Kind, string? Signature = null);

    /// <summary>
    /// Matches nodes between old and new syntax trees.
    /// </summary>
    /// <param name="oldNodes">The structural nodes from the old syntax tree.</param>
    /// <param name="newNodes">The structural nodes from the new syntax tree.</param>
    /// <returns>A <see cref="MatchResult"/> containing matched pairs and unmatched nodes.</returns>
    public MatchResult MatchNodes(IReadOnlyList<NodeInfo> oldNodes, IReadOnlyList<NodeInfo> newNodes)
    {
        var matchedPairs = new List<(SyntaxNode, SyntaxNode)>();
        var unmatchedOld = new List<SyntaxNode>();
        var matchedNewIndices = new HashSet<int>();

        // First pass: exact name + kind matching
        foreach (var oldNode in oldNodes)
        {
            var matchIndex = FindBestMatch(oldNode, newNodes, matchedNewIndices);

            if (matchIndex >= 0)
            {
                matchedPairs.Add((oldNode.Node, newNodes[matchIndex].Node));
                matchedNewIndices.Add(matchIndex);
            }
            else
            {
                unmatchedOld.Add(oldNode.Node);
            }
        }

        // Collect unmatched new nodes
        var unmatchedNew = new List<SyntaxNode>();
        for (var i = 0; i < newNodes.Count; i++)
        {
            if (!matchedNewIndices.Contains(i))
            {
                unmatchedNew.Add(newNodes[i].Node);
            }
        }

        return new MatchResult(matchedPairs, unmatchedOld, unmatchedNew);
    }

    /// <summary>
    /// Extracts structural nodes from a syntax tree for comparison.
    /// </summary>
    /// <param name="root">The root syntax node of the tree.</param>
    /// <returns>A list of structural nodes with their metadata.</returns>
    /// <remarks>
    /// This method only extracts top-level structural nodes directly under the root.
    /// Nested nodes (e.g., methods within classes) are handled by CompareChildren in SyntaxComparer.
    /// This prevents duplicate reporting where the same node appears both as a child and at the top level.
    /// </remarks>
    public IReadOnlyList<NodeInfo> ExtractStructuralNodes(SyntaxNode root)
    {
        var nodes = new List<NodeInfo>();

        // Only extract immediate structural children of root (top-level declarations)
        // This prevents duplicate reporting of nested nodes
        foreach (var child in root.ChildNodes())
        {
            if (IsStructuralNode(child))
            {
                var name = GetNodeName(child);
                var kind = GetChangeKind(child);
                var signature = GetSignature(child);
                nodes.Add(new NodeInfo(child, name, kind, signature));
            }
        }

        return nodes;
    }

    /// <summary>
    /// Gets the name path of a node including its parent context (e.g., "Namespace.Class.Method").
    /// </summary>
    /// <param name="node">The syntax node to get the path for.</param>
    /// <returns>The fully qualified name path of the node.</returns>
    public static string GetNodePath(SyntaxNode node)
    {
        var parts = new List<string>();
        var current = node;

        while (current is not null)
        {
            var name = GetNodeName(current);
            if (name is not null)
            {
                parts.Insert(0, name);
            }
            current = current.Parent;
        }

        return string.Join(".", parts);
    }

    /// <summary>
    /// Gets the name of a syntax node if it is a named declaration.
    /// </summary>
    /// <param name="node">The syntax node to get the name of.</param>
    /// <returns>The name of the declaration, or <c>null</c> if the node is not a named declaration.</returns>
    public static string? GetNodeName(SyntaxNode node)
    {
        return node switch
        {
            BaseNamespaceDeclarationSyntax ns => ns.Name.ToString(),
            TypeDeclarationSyntax type => type.Identifier.Text,
            MethodDeclarationSyntax method => method.Identifier.Text,
            PropertyDeclarationSyntax property => property.Identifier.Text,
            FieldDeclarationSyntax field => GetFieldName(field),
            ConstructorDeclarationSyntax ctor => ctor.Identifier.Text,
            EnumDeclarationSyntax enumDecl => enumDecl.Identifier.Text,
            EnumMemberDeclarationSyntax enumMember => enumMember.Identifier.Text,
            EventDeclarationSyntax eventDecl => eventDecl.Identifier.Text,
            DelegateDeclarationSyntax delegateDecl => delegateDecl.Identifier.Text,
            _ => null
        };
    }

    /// <summary>
    /// Gets the signature of a method or constructor for distinguishing overloads.
    /// </summary>
    /// <param name="node">The syntax node to get the signature of.</param>
    /// <returns>The parameter signature, or <c>null</c> if not applicable.</returns>
    public static string? GetSignature(SyntaxNode node)
    {
        return node switch
        {
            MethodDeclarationSyntax method => GetParameterSignature(method.ParameterList),
            ConstructorDeclarationSyntax ctor => GetParameterSignature(ctor.ParameterList),
            DelegateDeclarationSyntax delegateDecl => GetParameterSignature(delegateDecl.ParameterList),
            _ => null
        };
    }

    /// <summary>
    /// Maps a syntax node kind to a <see cref="ChangeKind"/>.
    /// </summary>
    /// <param name="node">The syntax node to map.</param>
    /// <returns>The corresponding <see cref="ChangeKind"/>.</returns>
    public static ChangeKind GetChangeKind(SyntaxNode node)
    {
        return node switch
        {
            BaseNamespaceDeclarationSyntax => ChangeKind.Namespace,
            ClassDeclarationSyntax => ChangeKind.Class,
            StructDeclarationSyntax => ChangeKind.Class,
            RecordDeclarationSyntax => ChangeKind.Class,
            InterfaceDeclarationSyntax => ChangeKind.Class,
            EnumDeclarationSyntax => ChangeKind.Class,
            MethodDeclarationSyntax => ChangeKind.Method,
            ConstructorDeclarationSyntax => ChangeKind.Method,
            PropertyDeclarationSyntax => ChangeKind.Property,
            FieldDeclarationSyntax => ChangeKind.Field,
            _ => ChangeKind.Statement
        };
    }

    /// <summary>
    /// Creates a <see cref="SourceLocation"/> from a Roslyn <see cref="SyntaxNode"/>.
    /// </summary>
    /// <param name="node">The syntax node to get the location from.</param>
    /// <param name="filePath">The optional file path to include in the location.</param>
    /// <returns>A <see cref="SourceLocation"/> representing the span of the node.</returns>
    public static SourceLocation CreateLocation(SyntaxNode node, string? filePath = null)
    {
        var lineSpan = node.GetLocation().GetLineSpan();

        // Roslyn uses 0-based line numbers, our Location uses 1-based
        return new SourceLocation
        {
            File = filePath,
            StartLine = lineSpan.StartLinePosition.Line + 1,
            EndLine = lineSpan.EndLinePosition.Line + 1,
            StartColumn = lineSpan.StartLinePosition.Character + 1,
            EndColumn = lineSpan.EndLinePosition.Character + 1
        };
    }

    private static string? GetFieldName(FieldDeclarationSyntax field)
    {
        // A field declaration can declare multiple variables, take the first one
        var firstVariable = field.Declaration.Variables.FirstOrDefault();
        return firstVariable?.Identifier.Text;
    }

    private static string GetParameterSignature(ParameterListSyntax? parameterList)
    {
        if (parameterList is null || parameterList.Parameters.Count == 0)
        {
            return "()";
        }

        var parameters = parameterList.Parameters
            .Select(p => p.Type?.ToString() ?? "var")
            .ToList();

        return $"({string.Join(", ", parameters)})";
    }

    private int FindBestMatch(NodeInfo oldNode, IReadOnlyList<NodeInfo> newNodes, HashSet<int> alreadyMatched)
    {
        // Strategy 1: Exact name + kind + signature match
        for (var i = 0; i < newNodes.Count; i++)
        {
            if (alreadyMatched.Contains(i))
                continue;

            var newNode = newNodes[i];

            // Must be same kind
            if (oldNode.Kind != newNode.Kind)
                continue;

            // Must have same name (if both have names)
            if (oldNode.Name is not null && newNode.Name is not null)
            {
                if (oldNode.Name != newNode.Name)
                    continue;

                // For methods/constructors, also check signature to handle overloads
                if (oldNode.Signature is not null || newNode.Signature is not null)
                {
                    if (oldNode.Signature != newNode.Signature)
                        continue;
                }

                return i;
            }
        }

        // Strategy 2: For unnamed nodes, use position-based matching (not implemented in basic version)
        // This could be extended for statement-level diffing

        return -1;
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
}
