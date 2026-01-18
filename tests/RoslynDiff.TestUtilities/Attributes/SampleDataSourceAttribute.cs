namespace RoslynDiff.TestUtilities.Attributes;

/// <summary>
/// Specifies a sample data source for test case discovery.
/// This attribute can be applied multiple times to a test class to specify multiple sample data sources.
/// </summary>
/// <remarks>
/// Use this attribute to mark test classes that should discover and validate sample data files.
/// The path can be relative to the test project or an absolute path.
/// </remarks>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
public sealed class SampleDataSourceAttribute : Attribute
{
    /// <summary>
    /// Gets the path to the sample data source.
    /// </summary>
    /// <remarks>
    /// The path can be relative to the test project directory or an absolute path.
    /// </remarks>
    public string Path { get; }

    /// <summary>
    /// Gets a value indicating whether the sample data source is optional.
    /// </summary>
    /// <remarks>
    /// When set to <c>true</c>, the test framework will not fail if the sample data source is not found.
    /// Default value is <c>false</c>.
    /// </remarks>
    public bool Optional { get; init; }

    /// <summary>
    /// Initializes a new instance of the <see cref="SampleDataSourceAttribute"/> class.
    /// </summary>
    /// <param name="path">The path to the sample data source.</param>
    public SampleDataSourceAttribute(string path)
    {
        Path = path ?? throw new ArgumentNullException(nameof(path));
    }
}
