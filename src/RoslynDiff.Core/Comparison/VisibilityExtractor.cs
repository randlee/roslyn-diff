using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using RoslynDiff.Core.Models;

namespace RoslynDiff.Core.Comparison;

/// <summary>
/// Extracts visibility/accessibility information from Roslyn syntax nodes.
/// </summary>
public static class VisibilityExtractor
{
    /// <summary>
    /// Extracts the visibility from a syntax node.
    /// </summary>
    public static Visibility Extract(SyntaxNode node)
    {
        return node switch
        {
            MemberDeclarationSyntax member => ExtractFromMember(member),
            ParameterSyntax => Visibility.Local,
            LocalDeclarationStatementSyntax => Visibility.Local,
            VariableDeclaratorSyntax => Visibility.Local,
            _ => Visibility.Private // Default for unknown nodes
        };
    }

    /// <summary>
    /// Extracts visibility from a member declaration.
    /// </summary>
    private static Visibility ExtractFromMember(MemberDeclarationSyntax member)
    {
        var modifiers = member.Modifiers;

        bool hasPublic = modifiers.Any(SyntaxKind.PublicKeyword);
        bool hasPrivate = modifiers.Any(SyntaxKind.PrivateKeyword);
        bool hasProtected = modifiers.Any(SyntaxKind.ProtectedKeyword);
        bool hasInternal = modifiers.Any(SyntaxKind.InternalKeyword);

        // Check combinations first
        if (hasProtected && hasInternal)
            return Visibility.ProtectedInternal;
        if (hasPrivate && hasProtected)
            return Visibility.PrivateProtected;

        // Single modifiers
        if (hasPublic)
            return Visibility.Public;
        if (hasProtected)
            return Visibility.Protected;
        if (hasInternal)
            return Visibility.Internal;
        if (hasPrivate)
            return Visibility.Private;

        // Default visibility depends on containing context
        return GetDefaultVisibility(member);
    }

    /// <summary>
    /// Gets the default visibility when no explicit modifier is specified.
    /// </summary>
    private static Visibility GetDefaultVisibility(MemberDeclarationSyntax member)
    {
        // Top-level types default to internal
        if (member is TypeDeclarationSyntax && member.Parent is CompilationUnitSyntax)
            return Visibility.Internal;

        // Interface members default to public (check BEFORE TypeDeclarationSyntax since interface IS a type)
        if (member.Parent is InterfaceDeclarationSyntax)
            return Visibility.Public;

        // Nested types and members default to private
        if (member.Parent is TypeDeclarationSyntax)
            return Visibility.Private;

        // Enum members are public
        if (member is EnumMemberDeclarationSyntax)
            return Visibility.Public;

        return Visibility.Private;
    }

    /// <summary>
    /// Determines if a visibility level is considered public API.
    /// </summary>
    public static bool IsPublicApi(Visibility visibility)
    {
        return visibility is Visibility.Public or Visibility.Protected or Visibility.ProtectedInternal;
    }

    /// <summary>
    /// Determines if a visibility level is considered internal API.
    /// </summary>
    public static bool IsInternalApi(Visibility visibility)
    {
        return visibility is Visibility.Internal or Visibility.PrivateProtected;
    }
}
