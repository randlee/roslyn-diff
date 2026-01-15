namespace RoslynDiff.Output;

/// <summary>
/// Factory for creating output formatters based on format name.
/// </summary>
public class OutputFormatterFactory
{
    private readonly Dictionary<string, Func<IOutputFormatter>> _formatters;

    /// <summary>
    /// Initializes a new instance of the <see cref="OutputFormatterFactory"/> class
    /// with the default set of formatters.
    /// </summary>
    public OutputFormatterFactory()
    {
        _formatters = new Dictionary<string, Func<IOutputFormatter>>(StringComparer.OrdinalIgnoreCase)
        {
            ["json"] = () => new JsonFormatter(),
            ["text"] = () => new UnifiedFormatter(),
            ["html"] = () => new HtmlFormatter(),
            ["plain"] = () => new PlainTextFormatter(),
            ["terminal"] = () => new SpectreConsoleFormatter()
        };
    }

    /// <summary>
    /// Gets the list of supported format names.
    /// </summary>
    public IReadOnlyList<string> SupportedFormats => _formatters.Keys.ToList().AsReadOnly();

    /// <summary>
    /// Gets a formatter for the specified format.
    /// </summary>
    /// <param name="format">The format name (e.g., "json", "html", "text").</param>
    /// <returns>An <see cref="IOutputFormatter"/> for the specified format.</returns>
    /// <exception cref="ArgumentException">Thrown when the format is not supported.</exception>
    public IOutputFormatter GetFormatter(string format)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(format);

        if (_formatters.TryGetValue(format, out var factory))
        {
            return factory();
        }

        var supported = string.Join(", ", _formatters.Keys);
        throw new ArgumentException($"Unsupported format '{format}'. Supported formats: {supported}", nameof(format));
    }

    /// <summary>
    /// Registers a custom formatter factory for the specified format.
    /// </summary>
    /// <param name="format">The format name.</param>
    /// <param name="factory">A factory function that creates the formatter.</param>
    /// <exception cref="ArgumentException">Thrown when the format name is null or whitespace.</exception>
    /// <exception cref="ArgumentNullException">Thrown when the factory is null.</exception>
    public void RegisterFormatter(string format, Func<IOutputFormatter> factory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(format);
        ArgumentNullException.ThrowIfNull(factory);

        _formatters[format] = factory;
    }

    /// <summary>
    /// Checks if a formatter is available for the specified format.
    /// </summary>
    /// <param name="format">The format name to check.</param>
    /// <returns><c>true</c> if the format is supported; otherwise, <c>false</c>.</returns>
    public bool IsFormatSupported(string format)
    {
        if (string.IsNullOrWhiteSpace(format))
        {
            return false;
        }

        return _formatters.ContainsKey(format);
    }
}
