namespace RoslynDiff.Core.Models;

/// <summary>
/// Specifies the kind of symbol being analyzed for impact classification.
/// </summary>
public enum SymbolKind
{
    /// <summary>
    /// A namespace declaration.
    /// </summary>
    Namespace,

    /// <summary>
    /// A type declaration (class, struct, record, interface, enum).
    /// </summary>
    Type,

    /// <summary>
    /// A method or function declaration.
    /// </summary>
    Method,

    /// <summary>
    /// A property declaration.
    /// </summary>
    Property,

    /// <summary>
    /// A field declaration.
    /// </summary>
    Field,

    /// <summary>
    /// An event declaration.
    /// </summary>
    Event,

    /// <summary>
    /// A parameter in a method or constructor.
    /// </summary>
    Parameter,

    /// <summary>
    /// A local variable.
    /// </summary>
    Local,

    /// <summary>
    /// A constructor.
    /// </summary>
    Constructor,

    /// <summary>
    /// An indexer.
    /// </summary>
    Indexer,

    /// <summary>
    /// An operator overload.
    /// </summary>
    Operator,

    /// <summary>
    /// A delegate declaration.
    /// </summary>
    Delegate,

    /// <summary>
    /// An enum member.
    /// </summary>
    EnumMember,

    /// <summary>
    /// Unknown or unclassified symbol.
    /// </summary>
    Unknown
}
