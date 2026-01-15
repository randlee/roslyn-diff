namespace RoslynDiff.Core.Comparison;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.VisualBasic;
using Microsoft.CodeAnalysis.VisualBasic.Syntax;
using RoslynDiff.Core.Models;

// Alias to avoid ambiguity with Microsoft.CodeAnalysis.Location
using SourceLocation = RoslynDiff.Core.Models.Location;

/// <summary>
/// Matches VB.NET syntax nodes between old and new syntax trees for comparison.
/// </summary>
/// <remarks>
/// Uses name-based matching for named declarations (Classes, Modules, Subs, Functions, Properties, Fields)
/// and position-based matching as a fallback for unnamed nodes.
/// </remarks>
public sealed class VisualBasicNodeMatcher
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

        // First pass: exact name + kind + signature match (case-insensitive for VB)
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
    public IReadOnlyList<NodeInfo> ExtractStructuralNodes(SyntaxNode root)
    {
        var nodes = new List<NodeInfo>();
        ExtractNodesRecursive(root, nodes);
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
            NamespaceBlockSyntax ns => ns.NamespaceStatement.Name.ToString(),
            NamespaceStatementSyntax ns => ns.Name.ToString(),
            ModuleBlockSyntax module => module.ModuleStatement.Identifier.Text,
            ClassBlockSyntax cls => cls.ClassStatement.Identifier.Text,
            StructureBlockSyntax structure => structure.StructureStatement.Identifier.Text,
            InterfaceBlockSyntax iface => iface.InterfaceStatement.Identifier.Text,
            EnumBlockSyntax enumBlock => enumBlock.EnumStatement.Identifier.Text,
            MethodBlockSyntax method => GetMethodName(method),
            SubNewStatementSyntax ctor => "New",
            ConstructorBlockSyntax ctor => "New",
            PropertyBlockSyntax prop => prop.PropertyStatement.Identifier.Text,
            PropertyStatementSyntax prop => prop.Identifier.Text,
            FieldDeclarationSyntax field => GetFieldName(field),
            EnumMemberDeclarationSyntax enumMember => enumMember.Identifier.Text,
            EventBlockSyntax eventBlock => eventBlock.EventStatement.Identifier.Text,
            EventStatementSyntax eventStmt => eventStmt.Identifier.Text,
            DelegateStatementSyntax delegateStmt => delegateStmt.Identifier.Text,
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
            MethodBlockSyntax method => GetMethodSignature(method),
            ConstructorBlockSyntax ctor => GetParameterSignature(ctor.SubNewStatement.ParameterList),
            DelegateStatementSyntax delegateStmt => GetParameterSignature(delegateStmt.ParameterList),
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
            NamespaceBlockSyntax => ChangeKind.Namespace,
            NamespaceStatementSyntax => ChangeKind.Namespace,
            ModuleBlockSyntax => ChangeKind.Class, // Module treated as class-like construct
            ClassBlockSyntax => ChangeKind.Class,
            StructureBlockSyntax => ChangeKind.Class,
            InterfaceBlockSyntax => ChangeKind.Class,
            EnumBlockSyntax => ChangeKind.Class,
            MethodBlockSyntax => ChangeKind.Method,
            ConstructorBlockSyntax => ChangeKind.Method,
            PropertyBlockSyntax => ChangeKind.Property,
            PropertyStatementSyntax => ChangeKind.Property,
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

    private static string? GetMethodName(MethodBlockSyntax method)
    {
        return method.SubOrFunctionStatement?.Identifier.Text;
    }

    private static string? GetMethodSignature(MethodBlockSyntax method)
    {
        return GetParameterSignature(method.SubOrFunctionStatement?.ParameterList);
    }

    private static string? GetFieldName(FieldDeclarationSyntax field)
    {
        // A field declaration can declare multiple variables, take the first one
        var firstVariable = field.Declarators.FirstOrDefault()?.Names.FirstOrDefault();
        return firstVariable?.Identifier.Text;
    }

    private static string GetParameterSignature(ParameterListSyntax? parameterList)
    {
        if (parameterList is null || parameterList.Parameters.Count == 0)
        {
            return "()";
        }

        var parameters = parameterList.Parameters
            .Select(p => p.AsClause?.Type?.ToString() ?? "Object")
            .ToList();

        return $"({string.Join(", ", parameters)})";
    }

    private int FindBestMatch(NodeInfo oldNode, IReadOnlyList<NodeInfo> newNodes, HashSet<int> alreadyMatched)
    {
        // Strategy 1: Exact name + kind + signature match (case-insensitive for VB)
        for (var i = 0; i < newNodes.Count; i++)
        {
            if (alreadyMatched.Contains(i))
                continue;

            var newNode = newNodes[i];

            // Must be same kind
            if (oldNode.Kind != newNode.Kind)
                continue;

            // Must have same name (if both have names) - VB is case-insensitive
            if (oldNode.Name is not null && newNode.Name is not null)
            {
                if (!string.Equals(oldNode.Name, newNode.Name, StringComparison.OrdinalIgnoreCase))
                    continue;

                // For methods/constructors, also check signature to handle overloads
                if (oldNode.Signature is not null || newNode.Signature is not null)
                {
                    // Signature comparison is also case-insensitive for VB
                    if (!string.Equals(oldNode.Signature, newNode.Signature, StringComparison.OrdinalIgnoreCase))
                        continue;
                }

                return i;
            }
        }

        // Strategy 2: For unnamed nodes, use position-based matching (not implemented in basic version)
        // This could be extended for statement-level diffing

        return -1;
    }

    private void ExtractNodesRecursive(SyntaxNode node, List<NodeInfo> nodes)
    {
        // Check if this node is a structural node we want to track
        if (IsStructuralNode(node))
        {
            var name = GetNodeName(node);
            var kind = GetChangeKind(node);
            var signature = GetSignature(node);
            nodes.Add(new NodeInfo(node, name, kind, signature));
        }

        // Recursively process children
        foreach (var child in node.ChildNodes())
        {
            ExtractNodesRecursive(child, nodes);
        }
    }

    private static bool IsStructuralNode(SyntaxNode node)
    {
        // Note: PropertyStatementSyntax is used for auto-properties (e.g., Public Property Name As String)
        // PropertyBlockSyntax is used for properties with explicit Get/Set blocks
        // We need to check that PropertyStatementSyntax is not inside a PropertyBlockSyntax (to avoid duplicates)
        if (node is PropertyStatementSyntax propStmt)
        {
            // Only treat as structural if it's an auto-property (parent is not PropertyBlockSyntax)
            return propStmt.Parent is not PropertyBlockSyntax;
        }

        return node is NamespaceBlockSyntax
            or ModuleBlockSyntax
            or ClassBlockSyntax
            or StructureBlockSyntax
            or InterfaceBlockSyntax
            or EnumBlockSyntax
            or MethodBlockSyntax
            or ConstructorBlockSyntax
            or PropertyBlockSyntax
            or FieldDeclarationSyntax;
    }
}
