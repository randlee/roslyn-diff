using FluentAssertions;
using RoslynDiff.Cli.Commands;
using Xunit;

namespace RoslynDiff.Cli.Tests.Commands;

/// <summary>
/// Tests for the DiffCommand's TFM (Target Framework Moniker) options.
/// </summary>
public class DiffCommandTfmTests
{
    #region Default Value Tests

    [Fact]
    public void Settings_DefaultTargetFramework_ShouldBeNull()
    {
        // Arrange & Act
        var settings = new DiffCommand.Settings();

        // Assert
        settings.TargetFramework.Should().BeNull();
    }

    [Fact]
    public void Settings_DefaultTargetFrameworks_ShouldBeNull()
    {
        // Arrange & Act
        var settings = new DiffCommand.Settings();

        // Assert
        settings.TargetFrameworks.Should().BeNull();
    }

    #endregion

    #region Single TFM Flag Tests (-t)

    [Fact]
    public void Settings_SingleTargetFramework_ShouldBeStored()
    {
        // Arrange & Act
        var settings = new DiffCommand.Settings
        {
            TargetFramework = new[] { "net8.0" }
        };

        // Assert
        settings.TargetFramework.Should().NotBeNull();
        settings.TargetFramework.Should().HaveCount(1);
        settings.TargetFramework![0].Should().Be("net8.0");
    }

    [Theory]
    [InlineData("net8.0")]
    [InlineData("net10.0")]
    [InlineData("net462")]
    [InlineData("net48")]
    [InlineData("netcoreapp3.1")]
    [InlineData("netstandard2.0")]
    public void Validate_SingleTargetFramework_WhenValid_ShouldSucceed(string tfm)
    {
        // Arrange
        var settings = new DiffCommand.Settings
        {
            TargetFramework = new[] { tfm }
        };

        // Act
        var result = settings.Validate();

        // Assert
        result.Successful.Should().BeTrue();
    }

    [Theory]
    [InlineData("NET8.0")]
    [InlineData("Net10.0")]
    [InlineData("NETCOREAPP3.1")]
    public void Validate_SingleTargetFramework_WhenUpperCase_ShouldSucceed(string tfm)
    {
        // Arrange
        var settings = new DiffCommand.Settings
        {
            TargetFramework = new[] { tfm }
        };

        // Act
        var result = settings.Validate();

        // Assert
        result.Successful.Should().BeTrue();
    }

    [Theory]
    [InlineData("invalid")]
    [InlineData("net")]
    [InlineData("net8")]
    [InlineData("net10")]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_SingleTargetFramework_WhenInvalid_ShouldReturnError(string tfm)
    {
        // Arrange
        var settings = new DiffCommand.Settings
        {
            TargetFramework = new[] { tfm }
        };

        // Act
        var result = settings.Validate();

        // Assert
        result.Successful.Should().BeFalse();
        result.Message.Should().Contain("Invalid TFM");
    }

    #endregion

    #region Multiple TFM Flags Tests (repeatable -t)

    [Fact]
    public void Settings_MultipleTargetFrameworks_ShouldBeStored()
    {
        // Arrange & Act
        var settings = new DiffCommand.Settings
        {
            TargetFramework = new[] { "net8.0", "net10.0" }
        };

        // Assert
        settings.TargetFramework.Should().NotBeNull();
        settings.TargetFramework.Should().HaveCount(2);
        settings.TargetFramework![0].Should().Be("net8.0");
        settings.TargetFramework[1].Should().Be("net10.0");
    }

    [Fact]
    public void Validate_MultipleTargetFrameworks_WhenAllValid_ShouldSucceed()
    {
        // Arrange
        var settings = new DiffCommand.Settings
        {
            TargetFramework = new[] { "net8.0", "net10.0", "netcoreapp3.1" }
        };

        // Act
        var result = settings.Validate();

        // Assert
        result.Successful.Should().BeTrue();
    }

    [Fact]
    public void Validate_MultipleTargetFrameworks_WhenOneInvalid_ShouldReturnError()
    {
        // Arrange
        var settings = new DiffCommand.Settings
        {
            TargetFramework = new[] { "net8.0", "invalid", "net10.0" }
        };

        // Act
        var result = settings.Validate();

        // Assert
        result.Successful.Should().BeFalse();
        result.Message.Should().Contain("Invalid TFM");
    }

    [Fact]
    public void Validate_MultipleTargetFrameworks_WithDuplicates_ShouldSucceed()
    {
        // Arrange
        // Duplicates are allowed at validation time; they're deduplicated during execution
        var settings = new DiffCommand.Settings
        {
            TargetFramework = new[] { "net8.0", "net8.0" }
        };

        // Act
        var result = settings.Validate();

        // Assert
        result.Successful.Should().BeTrue();
    }

    #endregion

    #region Semicolon-Separated TFMs Tests (-T)

    [Fact]
    public void Settings_SemicolonSeparatedTargetFrameworks_ShouldBeStored()
    {
        // Arrange & Act
        var settings = new DiffCommand.Settings
        {
            TargetFrameworks = "net8.0;net10.0"
        };

        // Assert
        settings.TargetFrameworks.Should().NotBeNull();
        settings.TargetFrameworks.Should().Be("net8.0;net10.0");
    }

    [Theory]
    [InlineData("net8.0;net10.0")]
    [InlineData("net8.0;net10.0;netcoreapp3.1")]
    [InlineData("net462;net48")]
    [InlineData("netstandard2.0;netstandard2.1")]
    public void Validate_SemicolonSeparatedTargetFrameworks_WhenValid_ShouldSucceed(string tfms)
    {
        // Arrange
        var settings = new DiffCommand.Settings
        {
            TargetFrameworks = tfms
        };

        // Act
        var result = settings.Validate();

        // Assert
        result.Successful.Should().BeTrue();
    }

    [Theory]
    [InlineData("net8.0;invalid")]
    [InlineData("invalid;net10.0")]
    [InlineData(";")]
    [InlineData(";;;")]
    public void Validate_SemicolonSeparatedTargetFrameworks_WhenInvalid_ShouldReturnError(string tfms)
    {
        // Arrange
        var settings = new DiffCommand.Settings
        {
            TargetFrameworks = tfms
        };

        // Act
        var result = settings.Validate();

        // Assert
        result.Successful.Should().BeFalse();
        result.Message.Should().Contain("Invalid TFM");
    }

    [Theory]
    [InlineData("net8.0;;net10.0")]
    public void Validate_SemicolonSeparatedTargetFrameworks_WithEmptyEntries_ShouldSucceed(string tfms)
    {
        // Arrange
        // Empty entries between semicolons are automatically removed by TfmParser
        var settings = new DiffCommand.Settings
        {
            TargetFrameworks = tfms
        };

        // Act
        var result = settings.Validate();

        // Assert
        // For "net8.0;;net10.0" - empty entry is removed, both TFMs are valid
        result.Successful.Should().BeTrue();
    }

    [Fact]
    public void Validate_SemicolonSeparatedTargetFrameworks_WithWhitespace_ShouldSucceed()
    {
        // Arrange
        var settings = new DiffCommand.Settings
        {
            TargetFrameworks = " net8.0 ; net10.0 "
        };

        // Act
        var result = settings.Validate();

        // Assert
        result.Successful.Should().BeTrue();
    }

    [Fact]
    public void Validate_SemicolonSeparatedTargetFrameworks_SingleTfm_ShouldSucceed()
    {
        // Arrange
        var settings = new DiffCommand.Settings
        {
            TargetFrameworks = "net8.0"
        };

        // Act
        var result = settings.Validate();

        // Assert
        result.Successful.Should().BeTrue();
    }

    #endregion

    #region Combined Flags Tests (-t and -T together)

    [Fact]
    public void Settings_BothTargetFrameworkOptions_ShouldBeStored()
    {
        // Arrange & Act
        var settings = new DiffCommand.Settings
        {
            TargetFramework = new[] { "net8.0" },
            TargetFrameworks = "net10.0;netcoreapp3.1"
        };

        // Assert
        settings.TargetFramework.Should().NotBeNull();
        settings.TargetFramework.Should().HaveCount(1);
        settings.TargetFrameworks.Should().NotBeNull();
    }

    [Fact]
    public void Validate_BothTargetFrameworkOptions_WhenAllValid_ShouldSucceed()
    {
        // Arrange
        var settings = new DiffCommand.Settings
        {
            TargetFramework = new[] { "net8.0" },
            TargetFrameworks = "net10.0;netcoreapp3.1"
        };

        // Act
        var result = settings.Validate();

        // Assert
        result.Successful.Should().BeTrue();
    }

    [Fact]
    public void Validate_BothTargetFrameworkOptions_WhenInvalidInArray_ShouldReturnError()
    {
        // Arrange
        var settings = new DiffCommand.Settings
        {
            TargetFramework = new[] { "invalid" },
            TargetFrameworks = "net10.0"
        };

        // Act
        var result = settings.Validate();

        // Assert
        result.Successful.Should().BeFalse();
        result.Message.Should().Contain("Invalid TFM");
    }

    [Fact]
    public void Validate_BothTargetFrameworkOptions_WhenInvalidInString_ShouldReturnError()
    {
        // Arrange
        var settings = new DiffCommand.Settings
        {
            TargetFramework = new[] { "net8.0" },
            TargetFrameworks = "invalid;net10.0"
        };

        // Act
        var result = settings.Validate();

        // Assert
        result.Successful.Should().BeFalse();
        result.Message.Should().Contain("Invalid TFM");
    }

    [Fact]
    public void Validate_BothTargetFrameworkOptions_WithDuplicates_ShouldSucceed()
    {
        // Arrange
        // Duplicates across both options are allowed; they're deduplicated during execution
        var settings = new DiffCommand.Settings
        {
            TargetFramework = new[] { "net8.0" },
            TargetFrameworks = "net8.0;net10.0"
        };

        // Act
        var result = settings.Validate();

        // Assert
        result.Successful.Should().BeTrue();
    }

    #endregion

    #region Error Message Quality Tests

    [Fact]
    public void Validate_InvalidTfm_ShouldProvideHelpfulErrorMessage()
    {
        // Arrange
        var settings = new DiffCommand.Settings
        {
            TargetFramework = new[] { "net8" }
        };

        // Act
        var result = settings.Validate();

        // Assert
        result.Successful.Should().BeFalse();
        result.Message.Should().Contain("Invalid TFM");
        result.Message.Should().Contain("format");
    }

    [Fact]
    public void Validate_EmptyTfm_ShouldProvideHelpfulErrorMessage()
    {
        // Arrange
        var settings = new DiffCommand.Settings
        {
            TargetFramework = new[] { "" }
        };

        // Act
        var result = settings.Validate();

        // Assert
        result.Successful.Should().BeFalse();
        result.Message.Should().Contain("Invalid TFM");
    }

    [Fact]
    public void Validate_WhitespaceTfm_ShouldProvideHelpfulErrorMessage()
    {
        // Arrange
        var settings = new DiffCommand.Settings
        {
            TargetFramework = new[] { "   " }
        };

        // Act
        var result = settings.Validate();

        // Assert
        result.Successful.Should().BeFalse();
        result.Message.Should().Contain("Invalid TFM");
    }

    #endregion

    #region Integration with Other Options Tests

    [Fact]
    public void Settings_TfmWithOtherOptions_ShouldHaveCorrectValues()
    {
        // Arrange & Act
        var settings = new DiffCommand.Settings
        {
            OldPath = "old.cs",
            NewPath = "new.cs",
            TargetFramework = new[] { "net8.0", "net10.0" },
            Mode = "roslyn",
            HtmlOutput = "output.html"
        };

        // Assert
        settings.OldPath.Should().Be("old.cs");
        settings.NewPath.Should().Be("new.cs");
        settings.TargetFramework.Should().HaveCount(2);
        settings.Mode.Should().Be("roslyn");
        settings.HtmlOutput.Should().Be("output.html");
    }

    [Fact]
    public void Validate_TfmWithValidOptions_ShouldSucceed()
    {
        // Arrange
        var settings = new DiffCommand.Settings
        {
            TargetFramework = new[] { "net8.0" },
            HtmlOutput = "report.html",
            OpenInBrowser = true
        };

        // Act
        var result = settings.Validate();

        // Assert
        result.Successful.Should().BeTrue();
    }

    [Fact]
    public void Validate_TfmWithInvalidWhitespaceMode_ShouldReturnWhitespaceModeError()
    {
        // Arrange
        // Whitespace mode validation happens before TFM validation
        var settings = new DiffCommand.Settings
        {
            TargetFramework = new[] { "net8.0" },
            WhitespaceMode = "invalid"
        };

        // Act
        var result = settings.Validate();

        // Assert
        result.Successful.Should().BeFalse();
        result.Message.Should().Contain("whitespace mode");
    }

    #endregion

    #region Modern .NET TFM Format Tests

    [Theory]
    [InlineData("net5.0")]
    [InlineData("net6.0")]
    [InlineData("net7.0")]
    [InlineData("net8.0")]
    [InlineData("net9.0")]
    [InlineData("net10.0")]
    public void Validate_ModernNetTfm_WithDot_ShouldSucceed(string tfm)
    {
        // Arrange
        var settings = new DiffCommand.Settings
        {
            TargetFramework = new[] { tfm }
        };

        // Act
        var result = settings.Validate();

        // Assert
        result.Successful.Should().BeTrue();
    }

    [Theory]
    [InlineData("net5")]
    [InlineData("net6")]
    [InlineData("net8")]
    [InlineData("net10")]
    public void Validate_ModernNetTfm_WithoutDot_ShouldFail(string tfm)
    {
        // Arrange
        var settings = new DiffCommand.Settings
        {
            TargetFramework = new[] { tfm }
        };

        // Act
        var result = settings.Validate();

        // Assert
        result.Successful.Should().BeFalse();
        result.Message.Should().Contain("Invalid TFM");
    }

    #endregion

    #region .NET Framework TFM Format Tests

    [Theory]
    [InlineData("net35")]
    [InlineData("net40")]
    [InlineData("net45")]
    [InlineData("net46")]
    [InlineData("net461")]
    [InlineData("net462")]
    [InlineData("net47")]
    [InlineData("net471")]
    [InlineData("net472")]
    [InlineData("net48")]
    [InlineData("net481")]
    public void Validate_NetFrameworkTfm_ShortFormat_ShouldSucceed(string tfm)
    {
        // Arrange
        var settings = new DiffCommand.Settings
        {
            TargetFramework = new[] { tfm }
        };

        // Act
        var result = settings.Validate();

        // Assert
        result.Successful.Should().BeTrue();
    }

    [Theory]
    [InlineData("net4.0")]
    [InlineData("net4.5")]
    [InlineData("net4.6")]
    [InlineData("net4.6.1")]
    [InlineData("net4.6.2")]
    [InlineData("net4.7")]
    [InlineData("net4.7.1")]
    [InlineData("net4.7.2")]
    [InlineData("net4.8")]
    [InlineData("net4.8.1")]
    public void Validate_NetFrameworkTfm_DottedFormat_ShouldSucceed(string tfm)
    {
        // Arrange
        var settings = new DiffCommand.Settings
        {
            TargetFramework = new[] { tfm }
        };

        // Act
        var result = settings.Validate();

        // Assert
        result.Successful.Should().BeTrue();
    }

    #endregion

    #region .NET Core and .NET Standard TFM Tests

    [Theory]
    [InlineData("netcoreapp2.1")]
    [InlineData("netcoreapp3.0")]
    [InlineData("netcoreapp3.1")]
    public void Validate_NetCoreAppTfm_ShouldSucceed(string tfm)
    {
        // Arrange
        var settings = new DiffCommand.Settings
        {
            TargetFramework = new[] { tfm }
        };

        // Act
        var result = settings.Validate();

        // Assert
        result.Successful.Should().BeTrue();
    }

    [Theory]
    [InlineData("netstandard1.0")]
    [InlineData("netstandard1.6")]
    [InlineData("netstandard2.0")]
    [InlineData("netstandard2.1")]
    public void Validate_NetStandardTfm_ShouldSucceed(string tfm)
    {
        // Arrange
        var settings = new DiffCommand.Settings
        {
            TargetFramework = new[] { tfm }
        };

        // Act
        var result = settings.Validate();

        // Assert
        result.Successful.Should().BeTrue();
    }

    [Theory]
    [InlineData("netcoreapp2")]
    [InlineData("netcoreapp31")]
    [InlineData("netstandard2")]
    [InlineData("netstandard21")]
    public void Validate_NetCoreAppAndStandard_WithoutDot_ShouldFail(string tfm)
    {
        // Arrange
        var settings = new DiffCommand.Settings
        {
            TargetFramework = new[] { tfm }
        };

        // Act
        var result = settings.Validate();

        // Assert
        result.Successful.Should().BeFalse();
        result.Message.Should().Contain("Invalid TFM");
    }

    #endregion

    #region Validation Order Tests

    [Fact]
    public void Validate_AllValidationRules_ShouldBeAppliedInOrder()
    {
        // Arrange
        // Test that validation rules are applied in the expected order
        var settings = new DiffCommand.Settings
        {
            OpenInBrowser = true,
            HtmlOutput = null,
            TargetFramework = new[] { "net8.0" }
        };

        // Act
        var result = settings.Validate();

        // Assert
        // Should fail on --open validation before TFM validation
        result.Successful.Should().BeFalse();
        result.Message.Should().Contain("--open requires --html");
    }

    [Fact]
    public void Validate_OnlyTfmInvalid_ShouldReturnTfmError()
    {
        // Arrange
        var settings = new DiffCommand.Settings
        {
            TargetFramework = new[] { "invalid" }
        };

        // Act
        var result = settings.Validate();

        // Assert
        result.Successful.Should().BeFalse();
        result.Message.Should().Contain("Invalid TFM");
    }

    #endregion

    #region Null and Empty Handling Tests

    [Fact]
    public void Validate_NullTargetFramework_ShouldSucceed()
    {
        // Arrange
        var settings = new DiffCommand.Settings
        {
            TargetFramework = null
        };

        // Act
        var result = settings.Validate();

        // Assert
        result.Successful.Should().BeTrue();
    }

    [Fact]
    public void Validate_NullTargetFrameworks_ShouldSucceed()
    {
        // Arrange
        var settings = new DiffCommand.Settings
        {
            TargetFrameworks = null
        };

        // Act
        var result = settings.Validate();

        // Assert
        result.Successful.Should().BeTrue();
    }

    [Fact]
    public void Validate_EmptyArrayTargetFramework_ShouldSucceed()
    {
        // Arrange
        var settings = new DiffCommand.Settings
        {
            TargetFramework = Array.Empty<string>()
        };

        // Act
        var result = settings.Validate();

        // Assert
        result.Successful.Should().BeTrue();
    }

    [Fact]
    public void Validate_EmptyStringTargetFrameworks_ShouldSucceed()
    {
        // Arrange
        // Empty string is treated as null/not provided
        var settings = new DiffCommand.Settings
        {
            TargetFrameworks = ""
        };

        // Act
        var result = settings.Validate();

        // Assert
        result.Successful.Should().BeTrue();
    }

    [Fact]
    public void Validate_WhitespaceOnlyTargetFrameworks_ShouldSucceed()
    {
        // Arrange
        // Whitespace-only string is treated as null/not provided
        var settings = new DiffCommand.Settings
        {
            TargetFrameworks = "   "
        };

        // Act
        var result = settings.Validate();

        // Assert
        result.Successful.Should().BeTrue();
    }

    #endregion
}
