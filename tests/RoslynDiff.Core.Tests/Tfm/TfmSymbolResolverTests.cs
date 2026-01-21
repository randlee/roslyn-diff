namespace RoslynDiff.Core.Tests.Tfm;

using FluentAssertions;
using RoslynDiff.Core.Tfm;
using Xunit;

public class TfmSymbolResolverTests
{
    #region .NET Framework Tests

    [Theory]
    [InlineData("net20", new[] { "NET20", "NETFRAMEWORK" })]
    [InlineData("net35", new[] { "NET35", "NETFRAMEWORK" })]
    [InlineData("net40", new[] { "NET40", "NETFRAMEWORK" })]
    [InlineData("net45", new[] { "NET45", "NETFRAMEWORK" })]
    [InlineData("net451", new[] { "NET451", "NETFRAMEWORK" })]
    [InlineData("net452", new[] { "NET452", "NETFRAMEWORK" })]
    [InlineData("net46", new[] { "NET46", "NETFRAMEWORK" })]
    [InlineData("net461", new[] { "NET461", "NETFRAMEWORK" })]
    [InlineData("net462", new[] { "NET462", "NETFRAMEWORK" })]
    [InlineData("net47", new[] { "NET47", "NETFRAMEWORK" })]
    [InlineData("net471", new[] { "NET471", "NETFRAMEWORK" })]
    [InlineData("net472", new[] { "NET472", "NETFRAMEWORK" })]
    [InlineData("net48", new[] { "NET48", "NETFRAMEWORK" })]
    public void GetPreprocessorSymbols_NetFramework_ReturnsCorrectSymbols(string tfm, string[] expectedSymbols)
    {
        var symbols = TfmSymbolResolver.GetPreprocessorSymbols(tfm);

        symbols.Should().BeEquivalentTo(expectedSymbols);
    }

    [Fact]
    public void GetPreprocessorSymbols_NetFramework48_IncludesFrameworkSymbol()
    {
        var symbols = TfmSymbolResolver.GetPreprocessorSymbols("net48");

        symbols.Should().Contain("NETFRAMEWORK");
        symbols.Should().Contain("NET48");
    }

    #endregion

    #region .NET Core Tests

    [Theory]
    [InlineData("netcoreapp1.0", new[] { "NETCOREAPP1_0", "NETCOREAPP" })]
    [InlineData("netcoreapp1.1", new[] { "NETCOREAPP1_1", "NETCOREAPP" })]
    [InlineData("netcoreapp2.0", new[] { "NETCOREAPP2_0", "NETCOREAPP" })]
    [InlineData("netcoreapp2.1", new[] { "NETCOREAPP2_1", "NETCOREAPP" })]
    [InlineData("netcoreapp2.2", new[] { "NETCOREAPP2_2", "NETCOREAPP" })]
    [InlineData("netcoreapp3.0", new[] { "NETCOREAPP3_0", "NETCOREAPP" })]
    [InlineData("netcoreapp3.1", new[] { "NETCOREAPP3_1", "NETCOREAPP" })]
    public void GetPreprocessorSymbols_NetCore_ReturnsCorrectSymbols(string tfm, string[] expectedSymbols)
    {
        var symbols = TfmSymbolResolver.GetPreprocessorSymbols(tfm);

        symbols.Should().BeEquivalentTo(expectedSymbols);
    }

    [Fact]
    public void GetPreprocessorSymbols_NetCore31_IncludesCoreAppSymbol()
    {
        var symbols = TfmSymbolResolver.GetPreprocessorSymbols("netcoreapp3.1");

        symbols.Should().Contain("NETCOREAPP");
        symbols.Should().Contain("NETCOREAPP3_1");
    }

    #endregion

    #region .NET 5+ Tests

    [Fact]
    public void GetPreprocessorSymbols_Net50_IncludesOrGreaterSymbols()
    {
        var symbols = TfmSymbolResolver.GetPreprocessorSymbols("net5.0");

        symbols.Should().Contain("NET5_0");
        symbols.Should().Contain("NET5_0_OR_GREATER");
        symbols.Should().HaveCount(2);
    }

    [Fact]
    public void GetPreprocessorSymbols_Net60_IncludesCorrectOrGreaterSymbols()
    {
        var symbols = TfmSymbolResolver.GetPreprocessorSymbols("net6.0");

        symbols.Should().Contain("NET6_0");
        symbols.Should().Contain("NET5_0_OR_GREATER");
        symbols.Should().Contain("NET6_0_OR_GREATER");
        symbols.Should().HaveCount(3);
    }

    [Fact]
    public void GetPreprocessorSymbols_Net70_IncludesAllLowerOrGreaterSymbols()
    {
        var symbols = TfmSymbolResolver.GetPreprocessorSymbols("net7.0");

        symbols.Should().Contain("NET7_0");
        symbols.Should().Contain("NET5_0_OR_GREATER");
        symbols.Should().Contain("NET6_0_OR_GREATER");
        symbols.Should().Contain("NET7_0_OR_GREATER");
        symbols.Should().HaveCount(4);
    }

    [Fact]
    public void GetPreprocessorSymbols_Net80_IncludesAllOrGreaterSymbols()
    {
        var symbols = TfmSymbolResolver.GetPreprocessorSymbols("net8.0");

        symbols.Should().Contain("NET8_0");
        symbols.Should().Contain("NET5_0_OR_GREATER");
        symbols.Should().Contain("NET6_0_OR_GREATER");
        symbols.Should().Contain("NET7_0_OR_GREATER");
        symbols.Should().Contain("NET8_0_OR_GREATER");
        symbols.Should().HaveCount(5);
    }

    [Fact]
    public void GetPreprocessorSymbols_Net90_IncludesAllOrGreaterSymbolsThroughNet9()
    {
        var symbols = TfmSymbolResolver.GetPreprocessorSymbols("net9.0");

        symbols.Should().Contain("NET9_0");
        symbols.Should().Contain("NET5_0_OR_GREATER");
        symbols.Should().Contain("NET6_0_OR_GREATER");
        symbols.Should().Contain("NET7_0_OR_GREATER");
        symbols.Should().Contain("NET8_0_OR_GREATER");
        symbols.Should().Contain("NET9_0_OR_GREATER");
        symbols.Should().HaveCount(6);
    }

    [Fact]
    public void GetPreprocessorSymbols_Net100_IncludesAllOrGreaterSymbolsThroughNet10()
    {
        var symbols = TfmSymbolResolver.GetPreprocessorSymbols("net10.0");

        symbols.Should().Contain("NET10_0");
        symbols.Should().Contain("NET5_0_OR_GREATER");
        symbols.Should().Contain("NET6_0_OR_GREATER");
        symbols.Should().Contain("NET7_0_OR_GREATER");
        symbols.Should().Contain("NET8_0_OR_GREATER");
        symbols.Should().Contain("NET9_0_OR_GREATER");
        symbols.Should().Contain("NET10_0_OR_GREATER");
        symbols.Should().HaveCount(7);
    }

    #endregion

    #region .NET Standard Tests

    [Theory]
    [InlineData("netstandard1.0", new[] { "NETSTANDARD1_0", "NETSTANDARD" })]
    [InlineData("netstandard1.1", new[] { "NETSTANDARD1_1", "NETSTANDARD" })]
    [InlineData("netstandard1.2", new[] { "NETSTANDARD1_2", "NETSTANDARD" })]
    [InlineData("netstandard1.3", new[] { "NETSTANDARD1_3", "NETSTANDARD" })]
    [InlineData("netstandard1.4", new[] { "NETSTANDARD1_4", "NETSTANDARD" })]
    [InlineData("netstandard1.5", new[] { "NETSTANDARD1_5", "NETSTANDARD" })]
    [InlineData("netstandard1.6", new[] { "NETSTANDARD1_6", "NETSTANDARD" })]
    [InlineData("netstandard2.0", new[] { "NETSTANDARD2_0", "NETSTANDARD" })]
    [InlineData("netstandard2.1", new[] { "NETSTANDARD2_1", "NETSTANDARD" })]
    public void GetPreprocessorSymbols_NetStandard_ReturnsCorrectSymbols(string tfm, string[] expectedSymbols)
    {
        var symbols = TfmSymbolResolver.GetPreprocessorSymbols(tfm);

        symbols.Should().BeEquivalentTo(expectedSymbols);
    }

    [Fact]
    public void GetPreprocessorSymbols_NetStandard21_IncludesStandardSymbol()
    {
        var symbols = TfmSymbolResolver.GetPreprocessorSymbols("netstandard2.1");

        symbols.Should().Contain("NETSTANDARD");
        symbols.Should().Contain("NETSTANDARD2_1");
    }

    #endregion

    #region Case Insensitivity Tests

    [Theory]
    [InlineData("NET8.0")]
    [InlineData("Net8.0")]
    [InlineData("net8.0")]
    [InlineData("nEt8.0")]
    public void GetPreprocessorSymbols_CaseInsensitive_ReturnsCorrectSymbols(string tfm)
    {
        var symbols = TfmSymbolResolver.GetPreprocessorSymbols(tfm);

        symbols.Should().Contain("NET8_0");
        symbols.Should().Contain("NET5_0_OR_GREATER");
        symbols.Should().Contain("NET6_0_OR_GREATER");
        symbols.Should().Contain("NET7_0_OR_GREATER");
        symbols.Should().Contain("NET8_0_OR_GREATER");
    }

    [Theory]
    [InlineData("NETSTANDARD2.0")]
    [InlineData("NetStandard2.0")]
    [InlineData("netstandard2.0")]
    [InlineData("nEtStAnDaRd2.0")]
    public void GetPreprocessorSymbols_NetStandard_CaseInsensitive_ReturnsCorrectSymbols(string tfm)
    {
        var symbols = TfmSymbolResolver.GetPreprocessorSymbols(tfm);

        symbols.Should().Contain("NETSTANDARD2_0");
        symbols.Should().Contain("NETSTANDARD");
    }

    [Theory]
    [InlineData("NET472")]
    [InlineData("Net472")]
    [InlineData("net472")]
    public void GetPreprocessorSymbols_NetFramework_CaseInsensitive_ReturnsCorrectSymbols(string tfm)
    {
        var symbols = TfmSymbolResolver.GetPreprocessorSymbols(tfm);

        symbols.Should().Contain("NET472");
        symbols.Should().Contain("NETFRAMEWORK");
    }

    #endregion

    #region Invalid TFM Tests

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void GetPreprocessorSymbols_NullOrEmpty_ThrowsArgumentException(string? tfm)
    {
        var act = () => TfmSymbolResolver.GetPreprocessorSymbols(tfm!);

        act.Should().Throw<ArgumentException>()
            .WithMessage("*TFM cannot be null or empty*")
            .And.ParamName.Should().Be("tfm");
    }

    [Theory]
    [InlineData("net4.5")]
    [InlineData("netcore3.1")]
    [InlineData("dotnet8.0")]
    [InlineData("net11.0")]
    [InlineData("netstandard3.0")]
    [InlineData("invalid")]
    [InlineData("net")]
    [InlineData("netstandard")]
    public void GetPreprocessorSymbols_UnrecognizedTfm_ThrowsArgumentException(string tfm)
    {
        var act = () => TfmSymbolResolver.GetPreprocessorSymbols(tfm);

        act.Should().Throw<ArgumentException>()
            .WithMessage($"*Unrecognized TFM: '{tfm}'*")
            .And.ParamName.Should().Be("tfm");
    }

    [Fact]
    public void GetPreprocessorSymbols_InvalidTfm_IncludesHelpfulMessage()
    {
        var act = () => TfmSymbolResolver.GetPreprocessorSymbols("invalid-tfm");

        act.Should().Throw<ArgumentException>()
            .WithMessage("*Supported TFMs include: .NET Framework*")
            .WithMessage("*net20-net48*")
            .WithMessage("*.NET Core*")
            .WithMessage("*netcoreapp1.0-netcoreapp3.1*")
            .WithMessage("*.NET 5+*")
            .WithMessage("*net5.0-net10.0*")
            .WithMessage("*.NET Standard*")
            .WithMessage("*netstandard1.0-netstandard2.1*");
    }

    #endregion

    #region Default Behavior Tests

    [Fact]
    public void GetDefaultSymbols_ReturnsNet10WithOrGreaterSymbols()
    {
        var symbols = TfmSymbolResolver.GetDefaultSymbols();

        symbols.Should().Contain("NET10_0");
        symbols.Should().Contain("NET5_0_OR_GREATER");
        symbols.Should().Contain("NET6_0_OR_GREATER");
        symbols.Should().Contain("NET7_0_OR_GREATER");
        symbols.Should().Contain("NET8_0_OR_GREATER");
        symbols.Should().Contain("NET9_0_OR_GREATER");
        symbols.Should().Contain("NET10_0_OR_GREATER");
        symbols.Should().HaveCount(7);
    }

    [Fact]
    public void GetDefaultSymbols_MatchesNet10Symbols()
    {
        var defaultSymbols = TfmSymbolResolver.GetDefaultSymbols();
        var net10Symbols = TfmSymbolResolver.GetPreprocessorSymbols("net10.0");

        defaultSymbols.Should().BeEquivalentTo(net10Symbols);
    }

    #endregion

    #region Whitespace Handling Tests

    [Theory]
    [InlineData(" net8.0 ")]
    [InlineData("  net8.0")]
    [InlineData("net8.0  ")]
    [InlineData("\tnet8.0\t")]
    public void GetPreprocessorSymbols_WithWhitespace_TrimsAndReturnsCorrectSymbols(string tfm)
    {
        var symbols = TfmSymbolResolver.GetPreprocessorSymbols(tfm);

        symbols.Should().Contain("NET8_0");
        symbols.Should().Contain("NET5_0_OR_GREATER");
        symbols.Should().Contain("NET6_0_OR_GREATER");
        symbols.Should().Contain("NET7_0_OR_GREATER");
        symbols.Should().Contain("NET8_0_OR_GREATER");
    }

    #endregion

    #region Symbol Consistency Tests

    [Fact]
    public void GetPreprocessorSymbols_Net5Symbols_DoNotIncludeNetCoreAppOrFramework()
    {
        var symbols = TfmSymbolResolver.GetPreprocessorSymbols("net5.0");

        symbols.Should().NotContain("NETCOREAPP");
        symbols.Should().NotContain("NETFRAMEWORK");
        symbols.Should().NotContain("NETSTANDARD");
    }

    [Fact]
    public void GetPreprocessorSymbols_NetCoreApp_DoesNotIncludeOrGreaterSymbols()
    {
        var symbols = TfmSymbolResolver.GetPreprocessorSymbols("netcoreapp3.1");

        symbols.Should().NotContain(s => s.Contains("OR_GREATER"));
    }

    [Fact]
    public void GetPreprocessorSymbols_NetFramework_DoesNotIncludeOrGreaterSymbols()
    {
        var symbols = TfmSymbolResolver.GetPreprocessorSymbols("net48");

        symbols.Should().NotContain(s => s.Contains("OR_GREATER"));
    }

    [Fact]
    public void GetPreprocessorSymbols_NetStandard_DoesNotIncludeOrGreaterSymbols()
    {
        var symbols = TfmSymbolResolver.GetPreprocessorSymbols("netstandard2.1");

        symbols.Should().NotContain(s => s.Contains("OR_GREATER"));
    }

    #endregion

    #region OR_GREATER Symbol Ordering Tests

    [Fact]
    public void GetPreprocessorSymbols_Net80_OrGreaterSymbolsInCorrectOrder()
    {
        var symbols = TfmSymbolResolver.GetPreprocessorSymbols("net8.0");

        // Find the OR_GREATER symbols in order
        var orGreaterSymbols = symbols.Where(s => s.Contains("OR_GREATER")).ToList();

        orGreaterSymbols.Should().HaveCount(4);
        orGreaterSymbols.Should().ContainInOrder(
            "NET5_0_OR_GREATER",
            "NET6_0_OR_GREATER",
            "NET7_0_OR_GREATER",
            "NET8_0_OR_GREATER");
    }

    #endregion
}
