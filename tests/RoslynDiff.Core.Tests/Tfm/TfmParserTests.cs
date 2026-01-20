namespace RoslynDiff.Core.Tests.Tfm;

using FluentAssertions;
using RoslynDiff.Core.Tfm;
using Xunit;

/// <summary>
/// Unit tests for <see cref="TfmParser"/>.
/// </summary>
public class TfmParserTests
{
    #region ParseSingle - Valid TFMs

    [Theory]
    [InlineData("net8.0", "net8.0")]
    [InlineData("net6.0", "net6.0")]
    [InlineData("net5.0", "net5.0")]
    [InlineData("NET8.0", "net8.0")]
    [InlineData("Net6.0", "net6.0")]
    public void ParseSingle_ModernNet_ReturnsNormalized(string input, string expected)
    {
        var result = TfmParser.ParseSingle(input);
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("net462", "net462")]
    [InlineData("net48", "net48")]
    [InlineData("net472", "net472")]
    [InlineData("net461", "net461")]
    [InlineData("NET462", "net462")]
    [InlineData("Net48", "net48")]
    public void ParseSingle_NetFramework_ReturnsNormalized(string input, string expected)
    {
        var result = TfmParser.ParseSingle(input);
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("netcoreapp3.1", "netcoreapp3.1")]
    [InlineData("netcoreapp2.1", "netcoreapp2.1")]
    [InlineData("netcoreapp3.0", "netcoreapp3.0")]
    [InlineData("netcoreapp2.0", "netcoreapp2.0")]
    [InlineData("NETCOREAPP3.1", "netcoreapp3.1")]
    [InlineData("NetCoreApp2.1", "netcoreapp2.1")]
    public void ParseSingle_NetCoreApp_ReturnsNormalized(string input, string expected)
    {
        var result = TfmParser.ParseSingle(input);
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("netstandard2.1", "netstandard2.1")]
    [InlineData("netstandard2.0", "netstandard2.0")]
    [InlineData("netstandard1.6", "netstandard1.6")]
    [InlineData("NETSTANDARD2.1", "netstandard2.1")]
    [InlineData("NetStandard2.0", "netstandard2.0")]
    public void ParseSingle_NetStandard_ReturnsNormalized(string input, string expected)
    {
        var result = TfmParser.ParseSingle(input);
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("  net8.0  ", "net8.0")]
    [InlineData("\tnet6.0\t", "net6.0")]
    [InlineData(" netcoreapp3.1 ", "netcoreapp3.1")]
    [InlineData("  netstandard2.0  ", "netstandard2.0")]
    public void ParseSingle_WithWhitespace_TrimsAndNormalizes(string input, string expected)
    {
        var result = TfmParser.ParseSingle(input);
        result.Should().Be(expected);
    }

    #endregion

    #region ParseSingle - Invalid TFMs

    [Theory]
    [InlineData(null)]
    public void ParseSingle_Null_ThrowsArgumentNullException(string? input)
    {
        var act = () => TfmParser.ParseSingle(input!);
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("tfm")
            .WithMessage("*TFM cannot be null*");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t")]
    public void ParseSingle_EmptyOrWhitespace_ThrowsArgumentException(string input)
    {
        var act = () => TfmParser.ParseSingle(input);
        act.Should().Throw<ArgumentException>()
            .WithParameterName("tfm")
            .WithMessage("*TFM cannot be empty or whitespace*");
    }

    [Theory]
    [InlineData("net8", "Invalid TFM format*Expected format*")]
    [InlineData("net", "Invalid TFM format*Expected format*")]
    [InlineData("netcore3.1", "Invalid TFM format*Expected format*")]
    [InlineData("dotnet8.0", "Invalid TFM format*Expected format*")]
    [InlineData("framework4.8", "Invalid TFM format*Expected format*")]
    [InlineData("net8.0.0", "Invalid TFM format*Expected format*")]
    [InlineData("net.8.0", "Invalid TFM format*Expected format*")]
    [InlineData("net-8.0", "Invalid TFM format*Expected format*")]
    [InlineData("8.0", "Invalid TFM format*Expected format*")]
    [InlineData("netstandard", "Invalid TFM format*Expected format*")]
    [InlineData("netcoreapp", "Invalid TFM format*Expected format*")]
    public void ParseSingle_InvalidFormat_ThrowsArgumentException(string input, string messagePattern)
    {
        var act = () => TfmParser.ParseSingle(input);
        act.Should().Throw<ArgumentException>()
            .WithParameterName("tfm")
            .WithMessage(messagePattern);
    }

    [Theory]
    [InlineData("net5", "Invalid TFM format*")]  // Modern .NET requires minor version
    [InlineData("net6", "Invalid TFM format*")]
    [InlineData("net7", "Invalid TFM format*")]
    [InlineData("net8", "Invalid TFM format*")]
    [InlineData("net10", "Invalid TFM format*")]
    public void ParseSingle_ModernNetWithoutMinorVersion_ThrowsArgumentException(string input, string messagePattern)
    {
        var act = () => TfmParser.ParseSingle(input);
        act.Should().Throw<ArgumentException>()
            .WithParameterName("tfm")
            .WithMessage(messagePattern);
    }

    [Theory]
    [InlineData("netcoreapp3", "Invalid TFM format*")]
    [InlineData("netcoreapp2", "Invalid TFM format*")]
    [InlineData("netstandard2", "Invalid TFM format*")]
    [InlineData("netstandard1", "Invalid TFM format*")]
    public void ParseSingle_NetCoreAppAndStandardWithoutMinorVersion_ThrowsArgumentException(string input, string messagePattern)
    {
        var act = () => TfmParser.ParseSingle(input);
        act.Should().Throw<ArgumentException>()
            .WithParameterName("tfm")
            .WithMessage(messagePattern);
    }

    #endregion

    #region ParseMultiple - Valid Lists

    [Fact]
    public void ParseMultiple_SingleTfm_ReturnsArray()
    {
        var result = TfmParser.ParseMultiple("net8.0");
        result.Should().BeEquivalentTo(new[] { "net8.0" });
    }

    [Fact]
    public void ParseMultiple_MultipleTfms_ReturnsNormalizedArray()
    {
        var result = TfmParser.ParseMultiple("net8.0;net6.0");
        result.Should().BeEquivalentTo(new[] { "net8.0", "net6.0" });
    }

    [Fact]
    public void ParseMultiple_MixedFrameworks_ReturnsNormalizedArray()
    {
        var result = TfmParser.ParseMultiple("net8.0;net462;netcoreapp3.1;netstandard2.0");
        result.Should().BeEquivalentTo(new[] { "net8.0", "net462", "netcoreapp3.1", "netstandard2.0" });
    }

    [Fact]
    public void ParseMultiple_WithWhitespace_TrimsAndNormalizes()
    {
        var result = TfmParser.ParseMultiple("  net8.0  ;  net6.0  ");
        result.Should().BeEquivalentTo(new[] { "net8.0", "net6.0" });
    }

    [Fact]
    public void ParseMultiple_WithExtraSpaces_TrimsAndNormalizes()
    {
        var result = TfmParser.ParseMultiple("net8.0 ; net6.0 ; net462");
        result.Should().BeEquivalentTo(new[] { "net8.0", "net6.0", "net462" });
    }

    [Fact]
    public void ParseMultiple_WithMixedCase_NormalizesToLowercase()
    {
        var result = TfmParser.ParseMultiple("NET8.0;Net6.0;NETSTANDARD2.1");
        result.Should().BeEquivalentTo(new[] { "net8.0", "net6.0", "netstandard2.1" });
    }

    [Fact]
    public void ParseMultiple_WithDuplicates_RemovesDuplicates()
    {
        var result = TfmParser.ParseMultiple("net8.0;net6.0;net8.0;net6.0");
        result.Should().BeEquivalentTo(new[] { "net8.0", "net6.0" });
    }

    [Fact]
    public void ParseMultiple_WithDuplicatesDifferentCase_RemovesDuplicatesCaseInsensitive()
    {
        var result = TfmParser.ParseMultiple("net8.0;NET8.0;Net8.0");
        result.Should().BeEquivalentTo(new[] { "net8.0" });
    }

    [Fact]
    public void ParseMultiple_WithTrailingSemicolon_IgnoresEmptyEntries()
    {
        var result = TfmParser.ParseMultiple("net8.0;net6.0;");
        result.Should().BeEquivalentTo(new[] { "net8.0", "net6.0" });
    }

    [Fact]
    public void ParseMultiple_WithMultipleSemicolons_IgnoresEmptyEntries()
    {
        var result = TfmParser.ParseMultiple("net8.0;;net6.0");
        result.Should().BeEquivalentTo(new[] { "net8.0", "net6.0" });
    }

    #endregion

    #region ParseMultiple - Invalid Lists

    [Theory]
    [InlineData(null)]
    public void ParseMultiple_Null_ThrowsArgumentNullException(string? input)
    {
        var act = () => TfmParser.ParseMultiple(input!);
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("tfms")
            .WithMessage("*TFM list cannot be null*");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t")]
    public void ParseMultiple_EmptyOrWhitespace_ThrowsArgumentException(string input)
    {
        var act = () => TfmParser.ParseMultiple(input);
        act.Should().Throw<ArgumentException>()
            .WithParameterName("tfms")
            .WithMessage("*TFM list cannot be empty or whitespace*");
    }

    [Theory]
    [InlineData(";")]
    [InlineData(";;")]
    [InlineData("  ;  ;  ")]
    public void ParseMultiple_OnlySemicolons_ThrowsArgumentException(string input)
    {
        var act = () => TfmParser.ParseMultiple(input);
        act.Should().Throw<ArgumentException>()
            .WithParameterName("tfms")
            .WithMessage("*TFM list cannot be empty after parsing*");
    }

    [Fact]
    public void ParseMultiple_WithOneInvalidTfm_ThrowsArgumentException()
    {
        var act = () => TfmParser.ParseMultiple("net8.0;invalid;net6.0");
        act.Should().Throw<ArgumentException>()
            .WithParameterName("tfms")
            .WithMessage("*Invalid TFM(s) in list*invalid*");
    }

    [Fact]
    public void ParseMultiple_WithMultipleInvalidTfms_ThrowsArgumentExceptionWithAllErrors()
    {
        var act = () => TfmParser.ParseMultiple("net8.0;invalid1;net6.0;invalid2");
        act.Should().Throw<ArgumentException>()
            .WithParameterName("tfms")
            .WithMessage("*Invalid TFM(s) in list*invalid1*invalid2*");
    }

    [Fact]
    public void ParseMultiple_AllInvalidTfms_ThrowsArgumentException()
    {
        var act = () => TfmParser.ParseMultiple("invalid1;invalid2;invalid3");
        act.Should().Throw<ArgumentException>()
            .WithParameterName("tfms")
            .WithMessage("*Invalid TFM(s) in list*");
    }

    #endregion

    #region Validate - Valid TFMs

    [Theory]
    [InlineData("net8.0")]
    [InlineData("net6.0")]
    [InlineData("net5.0")]
    [InlineData("net10.0")]
    public void Validate_ModernNet_ReturnsTrue(string tfm)
    {
        TfmParser.Validate(tfm).Should().BeTrue();
    }

    [Theory]
    [InlineData("net462")]
    [InlineData("net48")]
    [InlineData("net472")]
    [InlineData("net461")]
    [InlineData("net46")]
    [InlineData("net40")]
    [InlineData("net35")]
    public void Validate_NetFramework_ReturnsTrue(string tfm)
    {
        TfmParser.Validate(tfm).Should().BeTrue();
    }

    [Theory]
    [InlineData("netcoreapp3.1")]
    [InlineData("netcoreapp2.1")]
    [InlineData("netcoreapp3.0")]
    [InlineData("netcoreapp2.0")]
    public void Validate_NetCoreApp_ReturnsTrue(string tfm)
    {
        TfmParser.Validate(tfm).Should().BeTrue();
    }

    [Theory]
    [InlineData("netstandard2.1")]
    [InlineData("netstandard2.0")]
    [InlineData("netstandard1.6")]
    [InlineData("netstandard1.0")]
    public void Validate_NetStandard_ReturnsTrue(string tfm)
    {
        TfmParser.Validate(tfm).Should().BeTrue();
    }

    #endregion

    #region Validate - Invalid TFMs

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t")]
    public void Validate_NullOrWhitespace_ReturnsFalse(string? tfm)
    {
        TfmParser.Validate(tfm!).Should().BeFalse();
    }

    [Theory]
    [InlineData("net8")]      // Modern .NET requires minor version
    [InlineData("net6")]
    [InlineData("net5")]
    [InlineData("net10")]
    public void Validate_ModernNetWithoutMinorVersion_ReturnsFalse(string tfm)
    {
        TfmParser.Validate(tfm).Should().BeFalse();
    }

    [Theory]
    [InlineData("netcoreapp3")]  // netcoreapp requires minor version
    [InlineData("netcoreapp2")]
    [InlineData("netstandard2")] // netstandard requires minor version
    [InlineData("netstandard1")]
    public void Validate_NetCoreAppAndStandardWithoutMinorVersion_ReturnsFalse(string tfm)
    {
        TfmParser.Validate(tfm).Should().BeFalse();
    }

    [Theory]
    [InlineData("invalid")]
    [InlineData("dotnet8.0")]
    [InlineData("framework4.8")]
    [InlineData("netcore3.1")]
    [InlineData("net")]
    [InlineData("8.0")]
    [InlineData("net8.0.0")]
    [InlineData("net.8.0")]
    [InlineData("net-8.0")]
    public void Validate_InvalidFormat_ReturnsFalse(string tfm)
    {
        TfmParser.Validate(tfm).Should().BeFalse();
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void ParseSingle_PreservesOrderInNormalization()
    {
        var inputs = new[] { "NET8.0", "net6.0", "Net5.0" };
        var results = inputs.Select(TfmParser.ParseSingle).ToArray();
        results.Should().BeEquivalentTo(new[] { "net8.0", "net6.0", "net5.0" });
    }

    [Fact]
    public void ParseMultiple_PreservesOrder()
    {
        var result = TfmParser.ParseMultiple("net8.0;net6.0;net462;netcoreapp3.1");
        result.Should().Equal("net8.0", "net6.0", "net462", "netcoreapp3.1");
    }

    [Fact]
    public void ParseMultiple_RemovesDuplicatesPreservesFirstOccurrence()
    {
        var result = TfmParser.ParseMultiple("net8.0;net6.0;net8.0;net462");
        result.Should().Equal("net8.0", "net6.0", "net462");
    }

    [Fact]
    public void Validate_CaseInsensitive_AcceptsAnyCase()
    {
        // Validate is case-insensitive (though ParseSingle normalizes to lowercase)
        TfmParser.Validate("NET8.0").Should().BeTrue();
        TfmParser.Validate("net8.0").Should().BeTrue();
        TfmParser.Validate("Net8.0").Should().BeTrue();
    }

    [Theory]
    [InlineData("net4.8")]   // .NET Framework with dotted notation
    [InlineData("net4.6.2")] // .NET Framework with full version
    [InlineData("net4.5.2")]
    public void Validate_NetFrameworkDottedNotation_ReturnsTrue(string tfm)
    {
        TfmParser.Validate(tfm).Should().BeTrue();
    }

    #endregion
}
