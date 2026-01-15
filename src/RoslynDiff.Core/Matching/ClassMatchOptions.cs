namespace RoslynDiff.Core.Matching;

/// <summary>
/// Strategy for matching classes between syntax trees.
/// </summary>
public enum ClassMatchStrategy
{
    /// <summary>
    /// Match by exact class name (case-sensitive for C#).
    /// </summary>
    ExactName,

    /// <summary>
    /// Match classes that implement a specific interface.
    /// </summary>
    Interface,

    /// <summary>
    /// Match by content similarity using Levenshtein distance.
    /// </summary>
    Similarity,

    /// <summary>
    /// Try all strategies in order: ExactName first, then Interface (if specified), then Similarity.
    /// </summary>
    Auto
}

/// <summary>
/// Options for configuring class matching behavior.
/// </summary>
public sealed class ClassMatchOptions
{
    /// <summary>
    /// Gets or sets the matching strategy to use.
    /// </summary>
    public ClassMatchStrategy Strategy { get; set; } = ClassMatchStrategy.ExactName;

    /// <summary>
    /// Gets or sets the interface name to match by (for Interface strategy).
    /// </summary>
    public string? InterfaceName { get; set; }

    /// <summary>
    /// Gets or sets the minimum similarity threshold (0.0-1.0) for Similarity strategy.
    /// Default is 0.8 (80% similarity required).
    /// </summary>
    public double SimilarityThreshold { get; set; } = 0.8;

    /// <summary>
    /// Gets or sets whether to include nested classes in matching.
    /// Default is true.
    /// </summary>
    public bool IncludeNestedClasses { get; set; } = true;

    /// <summary>
    /// Creates default options with ExactName strategy.
    /// </summary>
    public static ClassMatchOptions Default => new();

    /// <summary>
    /// Creates options for similarity-based matching with the specified threshold.
    /// </summary>
    /// <param name="threshold">The minimum similarity threshold (0.0-1.0).</param>
    public static ClassMatchOptions ForSimilarity(double threshold = 0.8) => new()
    {
        Strategy = ClassMatchStrategy.Similarity,
        SimilarityThreshold = threshold
    };

    /// <summary>
    /// Creates options for interface-based matching.
    /// </summary>
    /// <param name="interfaceName">The interface name to match by.</param>
    public static ClassMatchOptions ForInterface(string interfaceName) => new()
    {
        Strategy = ClassMatchStrategy.Interface,
        InterfaceName = interfaceName
    };

    /// <summary>
    /// Creates options for auto matching with fallback strategies.
    /// </summary>
    /// <param name="interfaceName">Optional interface name for Interface strategy fallback.</param>
    /// <param name="similarityThreshold">The minimum similarity threshold for Similarity fallback.</param>
    public static ClassMatchOptions ForAuto(string? interfaceName = null, double similarityThreshold = 0.8) => new()
    {
        Strategy = ClassMatchStrategy.Auto,
        InterfaceName = interfaceName,
        SimilarityThreshold = similarityThreshold
    };
}
