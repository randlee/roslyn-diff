namespace RoslynDiff.Core.Differ;

using RoslynDiff.Core.Models;

/// <summary>
/// Factory for creating the appropriate <see cref="IDiffer"/> based on file type and options.
/// </summary>
public sealed class DifferFactory
{
    private readonly LineDiffer _lineDiffer;
    private readonly CSharpDiffer _csharpDiffer;
    private readonly VisualBasicDiffer _vbDiffer;

    /// <summary>
    /// Initializes a new instance of the <see cref="DifferFactory"/> class.
    /// </summary>
    public DifferFactory()
    {
        _lineDiffer = new LineDiffer();
        _csharpDiffer = new CSharpDiffer();
        _vbDiffer = new VisualBasicDiffer();
    }

    /// <summary>
    /// Gets the appropriate differ for the given file path and options.
    /// </summary>
    /// <param name="filePath">The path to the file being diffed.</param>
    /// <param name="options">Options controlling the diff behavior.</param>
    /// <returns>An <see cref="IDiffer"/> suitable for the file type.</returns>
    public IDiffer GetDiffer(string filePath, DiffOptions options)
    {
        ArgumentNullException.ThrowIfNull(filePath);
        ArgumentNullException.ThrowIfNull(options);

        // If explicit line mode is requested, use LineDiffer
        if (options.Mode == DiffMode.Line)
        {
            return _lineDiffer;
        }

        // Get file extension to determine differ
        var extension = Path.GetExtension(filePath)?.ToLowerInvariant();

        // If explicit Roslyn mode is requested
        if (options.Mode == DiffMode.Roslyn)
        {
            return extension switch
            {
                ".cs" => _csharpDiffer,
                ".vb" => _vbDiffer,
                _ => throw new NotSupportedException($"Roslyn mode is not supported for '{extension}' files. Only .cs and .vb files are supported.")
            };
        }

        // Auto mode: select based on file extension
        return extension switch
        {
            ".cs" => _csharpDiffer,
            ".vb" => _vbDiffer,
            _ => _lineDiffer
        };
    }

    /// <summary>
    /// Gets the differ for comparing two files, using the new file's extension to determine the differ type.
    /// </summary>
    /// <param name="oldPath">The path to the old (original) file.</param>
    /// <param name="newPath">The path to the new file.</param>
    /// <param name="options">Options controlling the diff behavior.</param>
    /// <returns>An <see cref="IDiffer"/> suitable for the file type.</returns>
    public IDiffer GetDiffer(string oldPath, string newPath, DiffOptions options)
    {
        // Prefer new path for determining file type, fall back to old path
        var primaryPath = !string.IsNullOrEmpty(newPath) ? newPath : oldPath;
        return GetDiffer(primaryPath, options);
    }

    /// <summary>
    /// Determines if the given file can be diffed semantically (with Roslyn).
    /// </summary>
    /// <param name="filePath">The path to the file.</param>
    /// <returns><c>true</c> if the file can be diffed semantically; otherwise, <c>false</c>.</returns>
    public static bool SupportsSemantic(string filePath)
    {
        var extension = Path.GetExtension(filePath)?.ToLowerInvariant();
        return extension is ".cs" or ".vb";
    }

    /// <summary>
    /// Gets all supported file extensions for semantic diff.
    /// </summary>
    public static IReadOnlyList<string> SemanticExtensions => [".cs", ".vb"];

    /// <summary>
    /// Gets a list of all registered differs.
    /// </summary>
    public IReadOnlyList<IDiffer> RegisteredDiffers =>
    [
        _csharpDiffer,
        _vbDiffer,
        _lineDiffer
    ];
}
