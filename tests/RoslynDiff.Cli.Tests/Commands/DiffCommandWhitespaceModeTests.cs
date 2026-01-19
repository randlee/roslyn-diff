using FluentAssertions;
using RoslynDiff.Cli.Commands;
using Xunit;

namespace RoslynDiff.Cli.Tests.Commands;

/// <summary>
/// Tests for the DiffCommand's --whitespace-mode option.
/// </summary>
public class DiffCommandWhitespaceModeTests
{
    #region Default Value Tests

    [Fact]
    public void Settings_DefaultWhitespaceMode_ShouldBeExact()
    {
        // Arrange & Act
        var settings = new DiffCommand.Settings();

        // Assert
        settings.WhitespaceMode.Should().Be("exact");
    }

    [Fact]
    public void Settings_DefaultIgnoreWhitespace_ShouldBeFalse()
    {
        // Arrange & Act
        var settings = new DiffCommand.Settings();

        // Assert
        settings.IgnoreWhitespace.Should().BeFalse();
    }

    #endregion

    #region WhitespaceMode Parsing Tests

    [Theory]
    [InlineData("exact")]
    [InlineData("ignore-leading-trailing")]
    [InlineData("ignore-all")]
    [InlineData("language-aware")]
    public void Settings_WhitespaceMode_WhenSetToValidValue_ShouldReturnThatValue(string mode)
    {
        // Arrange & Act
        var settings = new DiffCommand.Settings { WhitespaceMode = mode };

        // Assert
        settings.WhitespaceMode.Should().Be(mode);
    }

    [Fact]
    public void Settings_WhitespaceMode_Exact_ShouldBeStored()
    {
        // Arrange & Act
        var settings = new DiffCommand.Settings { WhitespaceMode = "exact" };

        // Assert
        settings.WhitespaceMode.Should().Be("exact");
    }

    [Fact]
    public void Settings_WhitespaceMode_IgnoreLeadingTrailing_ShouldBeStored()
    {
        // Arrange & Act
        var settings = new DiffCommand.Settings { WhitespaceMode = "ignore-leading-trailing" };

        // Assert
        settings.WhitespaceMode.Should().Be("ignore-leading-trailing");
    }

    [Fact]
    public void Settings_WhitespaceMode_IgnoreAll_ShouldBeStored()
    {
        // Arrange & Act
        var settings = new DiffCommand.Settings { WhitespaceMode = "ignore-all" };

        // Assert
        settings.WhitespaceMode.Should().Be("ignore-all");
    }

    [Fact]
    public void Settings_WhitespaceMode_LanguageAware_ShouldBeStored()
    {
        // Arrange & Act
        var settings = new DiffCommand.Settings { WhitespaceMode = "language-aware" };

        // Assert
        settings.WhitespaceMode.Should().Be("language-aware");
    }

    [Theory]
    [InlineData("EXACT")]
    [InlineData("Exact")]
    [InlineData("IGNORE-LEADING-TRAILING")]
    [InlineData("IGNORE-ALL")]
    [InlineData("LANGUAGE-AWARE")]
    public void Validate_WhitespaceMode_WhenUpperCase_ShouldSucceed(string mode)
    {
        // Arrange
        var settings = new DiffCommand.Settings { WhitespaceMode = mode };

        // Act
        var result = settings.Validate();

        // Assert
        result.Successful.Should().BeTrue();
    }

    [Theory]
    [InlineData("invalid")]
    [InlineData("none")]
    [InlineData("all")]
    [InlineData("ignore")]
    [InlineData("preserve")]
    [InlineData("")]
    public void Validate_WhitespaceMode_WhenInvalid_ShouldReturnError(string mode)
    {
        // Arrange
        var settings = new DiffCommand.Settings { WhitespaceMode = mode };

        // Act
        var result = settings.Validate();

        // Assert
        result.Successful.Should().BeFalse();
        result.Message.Should().Contain("Invalid whitespace mode");
    }

    [Fact]
    public void Validate_WhitespaceMode_WhenInvalid_ShouldListValidModes()
    {
        // Arrange
        var settings = new DiffCommand.Settings { WhitespaceMode = "invalid-mode" };

        // Act
        var result = settings.Validate();

        // Assert
        result.Successful.Should().BeFalse();
        result.Message.Should().Contain("exact");
        result.Message.Should().Contain("ignore-leading-trailing");
        result.Message.Should().Contain("ignore-all");
        result.Message.Should().Contain("language-aware");
    }

    #endregion

    #region Backward Compatibility Tests

    [Fact]
    public void Settings_IgnoreWhitespace_WhenSetToTrue_ShouldBeTrue()
    {
        // Arrange & Act
        var settings = new DiffCommand.Settings { IgnoreWhitespace = true };

        // Assert
        settings.IgnoreWhitespace.Should().BeTrue();
    }

    [Fact]
    public void Settings_IgnoreWhitespace_WhenSetToFalse_ShouldBeFalse()
    {
        // Arrange & Act
        var settings = new DiffCommand.Settings { IgnoreWhitespace = false };

        // Assert
        settings.IgnoreWhitespace.Should().BeFalse();
    }

    [Fact]
    public void Settings_IgnoreWhitespaceWithWhitespaceMode_BothCanBeSet()
    {
        // Arrange & Act
        // Both options can be set; the behavior (backward compat override) is handled in ExecuteAsync
        var settings = new DiffCommand.Settings
        {
            IgnoreWhitespace = true,
            WhitespaceMode = "exact"
        };

        // Assert
        settings.IgnoreWhitespace.Should().BeTrue();
        settings.WhitespaceMode.Should().Be("exact");
    }

    [Theory]
    [InlineData("exact")]
    [InlineData("ignore-all")]
    [InlineData("language-aware")]
    public void Validate_IgnoreWhitespaceWithAnyWhitespaceMode_ShouldSucceed(string mode)
    {
        // Arrange
        // -w flag combined with any --whitespace-mode should pass validation
        // The backward compatibility behavior (override) is handled at execution time
        var settings = new DiffCommand.Settings
        {
            IgnoreWhitespace = true,
            WhitespaceMode = mode
        };

        // Act
        var result = settings.Validate();

        // Assert
        result.Successful.Should().BeTrue();
    }

    #endregion

    #region Option Combinations Tests

    [Fact]
    public void Settings_WithWhitespaceModeAndContext_ShouldHaveCorrectValues()
    {
        // Arrange & Act
        var settings = new DiffCommand.Settings
        {
            WhitespaceMode = "ignore-all",
            ContextLines = 5
        };

        // Assert
        settings.WhitespaceMode.Should().Be("ignore-all");
        settings.ContextLines.Should().Be(5);
    }

    [Fact]
    public void Settings_WithWhitespaceModeAndLineMode_ShouldHaveCorrectValues()
    {
        // Arrange & Act
        var settings = new DiffCommand.Settings
        {
            WhitespaceMode = "ignore-leading-trailing",
            Mode = "line"
        };

        // Assert
        settings.WhitespaceMode.Should().Be("ignore-leading-trailing");
        settings.Mode.Should().Be("line");
    }

    [Fact]
    public void Settings_WithLanguageAwareAndRoslynMode_ShouldHaveCorrectValues()
    {
        // Arrange & Act
        var settings = new DiffCommand.Settings
        {
            WhitespaceMode = "language-aware",
            Mode = "roslyn"
        };

        // Assert
        settings.WhitespaceMode.Should().Be("language-aware");
        settings.Mode.Should().Be("roslyn");
    }

    [Fact]
    public void Settings_WithAllOptionsSet_ShouldHaveCorrectValues()
    {
        // Arrange & Act
        var settings = new DiffCommand.Settings
        {
            OldPath = "old.cs",
            NewPath = "new.cs",
            Mode = "line",
            WhitespaceMode = "ignore-all",
            IgnoreWhitespace = false,
            ContextLines = 10,
            Quiet = true,
            NoColor = true
        };

        // Assert
        settings.OldPath.Should().Be("old.cs");
        settings.NewPath.Should().Be("new.cs");
        settings.Mode.Should().Be("line");
        settings.WhitespaceMode.Should().Be("ignore-all");
        settings.IgnoreWhitespace.Should().BeFalse();
        settings.ContextLines.Should().Be(10);
        settings.Quiet.Should().BeTrue();
        settings.NoColor.Should().BeTrue();
    }

    #endregion

    #region Validation Combination Tests

    [Fact]
    public void Validate_WithValidWhitespaceModeAndOtherValidOptions_ShouldSucceed()
    {
        // Arrange
        var settings = new DiffCommand.Settings
        {
            WhitespaceMode = "ignore-leading-trailing",
            HtmlOutput = "output.html",
            OpenInBrowser = true
        };

        // Act
        var result = settings.Validate();

        // Assert
        result.Successful.Should().BeTrue();
    }

    [Fact]
    public void Validate_WithInvalidWhitespaceModeAndOtherValidOptions_ShouldReturnWhitespaceModeError()
    {
        // Arrange
        var settings = new DiffCommand.Settings
        {
            WhitespaceMode = "invalid",
            HtmlOutput = "output.html"
        };

        // Act
        var result = settings.Validate();

        // Assert
        result.Successful.Should().BeFalse();
        result.Message.Should().Contain("Invalid whitespace mode");
    }

    [Fact]
    public void Validate_WithValidWhitespaceModeAndOpenWithoutHtml_ShouldReturnOpenError()
    {
        // Arrange
        var settings = new DiffCommand.Settings
        {
            WhitespaceMode = "exact",
            OpenInBrowser = true,
            HtmlOutput = null
        };

        // Act
        var result = settings.Validate();

        // Assert
        result.Successful.Should().BeFalse();
        result.Message.Should().Contain("--open requires --html");
    }

    [Fact]
    public void Validate_WithDefaultSettings_ShouldSucceed()
    {
        // Arrange
        var settings = new DiffCommand.Settings();

        // Act
        var result = settings.Validate();

        // Assert
        result.Successful.Should().BeTrue();
    }

    #endregion

    #region Enum Mapping Documentation Tests

    /// <summary>
    /// Documents the expected mapping from CLI string values to WhitespaceMode enum values.
    /// These tests serve as documentation for the expected behavior.
    /// </summary>
    [Theory]
    [InlineData("exact", "Exact")]
    [InlineData("ignore-leading-trailing", "IgnoreLeadingTrailing")]
    [InlineData("ignore-all", "IgnoreAll")]
    [InlineData("language-aware", "LanguageAware")]
    public void WhitespaceMode_StringToEnumMapping_ShouldBeDocumented(string cliValue, string expectedEnumName)
    {
        // This test documents the expected mapping between CLI string values
        // and the WhitespaceMode enum members. The actual parsing is done in ExecuteAsync.

        // Arrange
        var settings = new DiffCommand.Settings { WhitespaceMode = cliValue };

        // Act
        var result = settings.Validate();

        // Assert
        result.Successful.Should().BeTrue($"'{cliValue}' should map to WhitespaceMode.{expectedEnumName}");
    }

    #endregion
}
