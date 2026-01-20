namespace RoslynDiff.Core.Tfm;

/// <summary>
/// Resolves Target Framework Monikers (TFMs) to their corresponding preprocessor symbols.
/// </summary>
/// <remarks>
/// <para>
/// This class provides the mapping between .NET target framework monikers and the preprocessor
/// symbols that are automatically defined by the compiler for each framework. These symbols
/// are used in conditional compilation directives to create framework-specific code.
/// </para>
/// <para>
/// Supported TFM families:
/// </para>
/// <list type="bullet">
/// <item>
/// <description>.NET Framework: net20, net35, net40, net45-net48</description>
/// </item>
/// <item>
/// <description>.NET Core: netcoreapp1.0-netcoreapp3.1</description>
/// </item>
/// <item>
/// <description>.NET 5+: net5.0, net6.0, net7.0, net8.0, net9.0, net10.0</description>
/// </item>
/// <item>
/// <description>.NET Standard: netstandard1.0-netstandard2.1</description>
/// </item>
/// </list>
/// <para>
/// For .NET 5 and later, the resolver automatically includes OR_GREATER symbols, which match
/// the compiler's behavior of defining symbols for the current version and all lower versions
/// (e.g., net8.0 defines NET8_0, NET8_0_OR_GREATER, NET7_0_OR_GREATER, ..., NET5_0_OR_GREATER).
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Get symbols for .NET 8.0
/// var symbols = TfmSymbolResolver.GetPreprocessorSymbols("net8.0");
/// // Returns: ["NET8_0", "NET5_0_OR_GREATER", "NET6_0_OR_GREATER",
/// //           "NET7_0_OR_GREATER", "NET8_0_OR_GREATER"]
///
/// // Get symbols for .NET Framework 4.8
/// var symbols = TfmSymbolResolver.GetPreprocessorSymbols("net48");
/// // Returns: ["NET48", "NETFRAMEWORK"]
///
/// // Get default symbols (NET10.0)
/// var defaultSymbols = TfmSymbolResolver.GetDefaultSymbols();
/// // Returns: ["NET10_0", "NET5_0_OR_GREATER", "NET6_0_OR_GREATER",
/// //           "NET7_0_OR_GREATER", "NET8_0_OR_GREATER", "NET9_0_OR_GREATER",
/// //           "NET10_0_OR_GREATER"]
/// </code>
/// </example>
public static class TfmSymbolResolver
{
    private static readonly Dictionary<string, string[]> TfmSymbolMap = new(StringComparer.OrdinalIgnoreCase)
    {
        // .NET Framework
        ["net20"] = new[] { "NET20", "NETFRAMEWORK" },
        ["net35"] = new[] { "NET35", "NETFRAMEWORK" },
        ["net40"] = new[] { "NET40", "NETFRAMEWORK" },
        ["net45"] = new[] { "NET45", "NETFRAMEWORK" },
        ["net451"] = new[] { "NET451", "NETFRAMEWORK" },
        ["net452"] = new[] { "NET452", "NETFRAMEWORK" },
        ["net46"] = new[] { "NET46", "NETFRAMEWORK" },
        ["net461"] = new[] { "NET461", "NETFRAMEWORK" },
        ["net462"] = new[] { "NET462", "NETFRAMEWORK" },
        ["net47"] = new[] { "NET47", "NETFRAMEWORK" },
        ["net471"] = new[] { "NET471", "NETFRAMEWORK" },
        ["net472"] = new[] { "NET472", "NETFRAMEWORK" },
        ["net48"] = new[] { "NET48", "NETFRAMEWORK" },

        // .NET Core
        ["netcoreapp1.0"] = new[] { "NETCOREAPP1_0", "NETCOREAPP" },
        ["netcoreapp1.1"] = new[] { "NETCOREAPP1_1", "NETCOREAPP" },
        ["netcoreapp2.0"] = new[] { "NETCOREAPP2_0", "NETCOREAPP" },
        ["netcoreapp2.1"] = new[] { "NETCOREAPP2_1", "NETCOREAPP" },
        ["netcoreapp2.2"] = new[] { "NETCOREAPP2_2", "NETCOREAPP" },
        ["netcoreapp3.0"] = new[] { "NETCOREAPP3_0", "NETCOREAPP" },
        ["netcoreapp3.1"] = new[] { "NETCOREAPP3_1", "NETCOREAPP" },

        // .NET 5+
        ["net5.0"] = new[] { "NET5_0" },
        ["net6.0"] = new[] { "NET6_0" },
        ["net7.0"] = new[] { "NET7_0" },
        ["net8.0"] = new[] { "NET8_0" },
        ["net9.0"] = new[] { "NET9_0" },
        ["net10.0"] = new[] { "NET10_0" },

        // .NET Standard
        ["netstandard1.0"] = new[] { "NETSTANDARD1_0", "NETSTANDARD" },
        ["netstandard1.1"] = new[] { "NETSTANDARD1_1", "NETSTANDARD" },
        ["netstandard1.2"] = new[] { "NETSTANDARD1_2", "NETSTANDARD" },
        ["netstandard1.3"] = new[] { "NETSTANDARD1_3", "NETSTANDARD" },
        ["netstandard1.4"] = new[] { "NETSTANDARD1_4", "NETSTANDARD" },
        ["netstandard1.5"] = new[] { "NETSTANDARD1_5", "NETSTANDARD" },
        ["netstandard1.6"] = new[] { "NETSTANDARD1_6", "NETSTANDARD" },
        ["netstandard2.0"] = new[] { "NETSTANDARD2_0", "NETSTANDARD" },
        ["netstandard2.1"] = new[] { "NETSTANDARD2_1", "NETSTANDARD" },
    };

    private static readonly string[] DefaultSymbols = GetNet10Symbols();

    /// <summary>
    /// Gets the preprocessor symbols for a given Target Framework Moniker.
    /// </summary>
    /// <param name="tfm">The Target Framework Moniker (e.g., "net8.0", "netstandard2.0").</param>
    /// <returns>An array of preprocessor symbols for the specified TFM.</returns>
    /// <exception cref="ArgumentException">Thrown when the TFM is null, empty, or not recognized.</exception>
    /// <remarks>
    /// <para>
    /// This method returns all preprocessor symbols that the .NET compiler automatically defines
    /// for the specified target framework. These symbols can be used in #if, #elif, and other
    /// conditional compilation directives.
    /// </para>
    /// <para>
    /// The TFM parameter is case-insensitive and whitespace-tolerant. For .NET 5 and later versions,
    /// the method automatically includes all applicable OR_GREATER symbols (e.g., NET5_0_OR_GREATER,
    /// NET6_0_OR_GREATER, etc., up to the specified version).
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Get symbols for .NET 8.0 - includes version-specific and OR_GREATER symbols
    /// var net8Symbols = TfmSymbolResolver.GetPreprocessorSymbols("net8.0");
    /// // Returns: ["NET8_0", "NET5_0_OR_GREATER", "NET6_0_OR_GREATER",
    /// //           "NET7_0_OR_GREATER", "NET8_0_OR_GREATER"]
    ///
    /// // Get symbols for .NET Standard 2.0
    /// var netstandardSymbols = TfmSymbolResolver.GetPreprocessorSymbols("netstandard2.0");
    /// // Returns: ["NETSTANDARD2_0", "NETSTANDARD"]
    ///
    /// // Case-insensitive and whitespace-tolerant
    /// var symbols = TfmSymbolResolver.GetPreprocessorSymbols("  NET8.0  ");
    /// // Works correctly
    ///
    /// // Invalid TFM throws ArgumentException
    /// try
    /// {
    ///     var invalid = TfmSymbolResolver.GetPreprocessorSymbols("invalid-tfm");
    /// }
    /// catch (ArgumentException ex)
    /// {
    ///     Console.WriteLine(ex.Message);
    ///     // "Unrecognized TFM: 'invalid-tfm'. Supported TFMs include..."
    /// }
    /// </code>
    /// </example>
    public static string[] GetPreprocessorSymbols(string tfm)
    {
        if (string.IsNullOrWhiteSpace(tfm))
        {
            throw new ArgumentException("TFM cannot be null or empty.", nameof(tfm));
        }

        // Normalize TFM to lowercase and remove any whitespace
        var normalizedTfm = tfm.Trim().ToLowerInvariant();

        // Check if it's a recognized TFM
        if (!TfmSymbolMap.TryGetValue(normalizedTfm, out var baseSymbols))
        {
            throw new ArgumentException(
                $"Unrecognized TFM: '{tfm}'. Supported TFMs include: .NET Framework (net20-net48), " +
                $".NET Core (netcoreapp1.0-netcoreapp3.1), .NET 5+ (net5.0-net10.0), " +
                $"and .NET Standard (netstandard1.0-netstandard2.1).",
                nameof(tfm));
        }

        // For .NET 5+, add OR_GREATER symbols
        if (IsNet5OrLater(normalizedTfm))
        {
            var orGreaterSymbols = GetOrGreaterSymbols(normalizedTfm);
            return baseSymbols.Concat(orGreaterSymbols).ToArray();
        }

        return baseSymbols;
    }

    /// <summary>
    /// Gets the default preprocessor symbols (NET10_0 with all applicable OR_GREATER symbols).
    /// </summary>
    /// <returns>An array of default preprocessor symbols.</returns>
    /// <remarks>
    /// <para>
    /// This method returns the preprocessor symbols for .NET 10.0, which is the latest version
    /// supported by this resolver. This is used as the default when no specific TFM is provided.
    /// </para>
    /// <para>
    /// The returned symbols include NET10_0 and all OR_GREATER symbols from .NET 5.0 through .NET 10.0.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var defaultSymbols = TfmSymbolResolver.GetDefaultSymbols();
    /// // Returns: ["NET10_0", "NET5_0_OR_GREATER", "NET6_0_OR_GREATER",
    /// //           "NET7_0_OR_GREATER", "NET8_0_OR_GREATER", "NET9_0_OR_GREATER",
    /// //           "NET10_0_OR_GREATER"]
    ///
    /// // Use default symbols when TFM is not specified
    /// var symbols = options.TargetFrameworks?.Any() == true
    ///     ? TfmSymbolResolver.GetPreprocessorSymbols(options.TargetFrameworks[0])
    ///     : TfmSymbolResolver.GetDefaultSymbols();
    /// </code>
    /// </example>
    public static string[] GetDefaultSymbols()
    {
        return DefaultSymbols;
    }

    /// <summary>
    /// Determines if the TFM represents .NET 5 or later.
    /// </summary>
    /// <param name="normalizedTfm">The normalized TFM string.</param>
    /// <returns>True if the TFM is .NET 5 or later; otherwise, false.</returns>
    private static bool IsNet5OrLater(string normalizedTfm)
    {
        // .NET 5+ TFMs follow the pattern "netX.0" where X is a single or double digit >= 5
        // Examples: net5.0, net6.0, net7.0, net8.0, net9.0, net10.0
        // This excludes:
        // - .NET Framework: net20, net35, net40, net45, net451, net452, net46, net461, net462, net47, net471, net472, net48
        // - .NET Core: netcoreapp1.0, netcoreapp1.1, netcoreapp2.0, netcoreapp2.1, netcoreapp2.2, netcoreapp3.0, netcoreapp3.1
        // - .NET Standard: netstandard1.0 through netstandard2.1

        if (!normalizedTfm.StartsWith("net"))
            return false;

        if (normalizedTfm.StartsWith("netcoreapp") || normalizedTfm.StartsWith("netstandard"))
            return false;

        // Must contain a dot (e.g., net5.0, net6.0)
        // .NET Framework TFMs don't have dots (net48, net472, etc.)
        return normalizedTfm.Contains('.');
    }

    /// <summary>
    /// Gets the OR_GREATER symbols for a .NET 5+ TFM.
    /// </summary>
    /// <param name="normalizedTfm">The normalized TFM string.</param>
    /// <returns>An array of OR_GREATER symbols.</returns>
    private static string[] GetOrGreaterSymbols(string normalizedTfm)
    {
        // Extract the version number (e.g., "net8.0" -> 8)
        var versionStr = normalizedTfm.Replace("net", "").Replace(".0", "");
        if (!int.TryParse(versionStr, out var version))
        {
            return Array.Empty<string>();
        }

        // Generate OR_GREATER symbols for all versions from 5 to current
        var symbols = new List<string>();
        for (var v = 5; v <= version; v++)
        {
            symbols.Add($"NET{v}_0_OR_GREATER");
        }

        return symbols.ToArray();
    }

    /// <summary>
    /// Generates the NET10.0 symbols with all applicable OR_GREATER symbols.
    /// </summary>
    /// <returns>An array of NET10.0 symbols.</returns>
    private static string[] GetNet10Symbols()
    {
        var baseSymbols = new[] { "NET10_0" };
        var orGreaterSymbols = new[]
        {
            "NET5_0_OR_GREATER",
            "NET6_0_OR_GREATER",
            "NET7_0_OR_GREATER",
            "NET8_0_OR_GREATER",
            "NET9_0_OR_GREATER",
            "NET10_0_OR_GREATER"
        };
        return baseSymbols.Concat(orGreaterSymbols).ToArray();
    }
}
