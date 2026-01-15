namespace RoslynDiff.Core.Models;

/// <summary>
/// Specifies the kind of code element affected by a change.
/// </summary>
public enum ChangeKind
{
    /// <summary>
    /// The change affects the entire file.
    /// </summary>
    File,

    /// <summary>
    /// The change affects a namespace declaration.
    /// </summary>
    Namespace,

    /// <summary>
    /// The change affects a class, struct, record, or interface declaration.
    /// </summary>
    Class,

    /// <summary>
    /// The change affects a method or function.
    /// </summary>
    Method,

    /// <summary>
    /// The change affects a property.
    /// </summary>
    Property,

    /// <summary>
    /// The change affects a field.
    /// </summary>
    Field,

    /// <summary>
    /// The change affects an individual statement within a method body.
    /// </summary>
    Statement,

    /// <summary>
    /// The change affects a single line (used for line-based diffs).
    /// </summary>
    Line
}
