namespace RoslynDiff.Core.Models;

/// <summary>
/// Represents the visibility/accessibility of a symbol.
/// </summary>
public enum Visibility
{
    Public,
    Protected,
    Internal,
    ProtectedInternal,
    PrivateProtected,
    Private,
    Local  // For local variables, parameters
}
