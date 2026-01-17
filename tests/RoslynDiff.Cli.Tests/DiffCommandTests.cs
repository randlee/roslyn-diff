namespace RoslynDiff.Cli.Tests;

using FluentAssertions;
using RoslynDiff.Cli.Commands;
using Spectre.Console.Cli;
using Xunit;

/// <summary>
/// Unit tests for <see cref="DiffCommand"/>.
/// </summary>
public class DiffCommandTests
{
    #region Default Values

    [Fact]
    public void Settings_DefaultMode_ShouldBeAuto()
    {
        // Arrange & Act
        var settings = new DiffCommand.Settings();

        // Assert
        settings.Mode.Should().Be("auto");
    }

    [Fact]
    public void Settings_DefaultContextLines_ShouldBeThree()
    {
        // Arrange & Act
        var settings = new DiffCommand.Settings();

        // Assert
        settings.ContextLines.Should().Be(3);
    }

    [Fact]
    public void Settings_DefaultIgnoreWhitespace_ShouldBeFalse()
    {
        // Arrange & Act
        var settings = new DiffCommand.Settings();

        // Assert
        settings.IgnoreWhitespace.Should().BeFalse();
    }

    [Fact]
    public void Settings_DefaultQuiet_ShouldBeFalse()
    {
        // Arrange & Act
        var settings = new DiffCommand.Settings();

        // Assert
        settings.Quiet.Should().BeFalse();
    }

    [Fact]
    public void Settings_DefaultNoColor_ShouldBeFalse()
    {
        // Arrange & Act
        var settings = new DiffCommand.Settings();

        // Assert
        settings.NoColor.Should().BeFalse();
    }

    [Fact]
    public void Settings_DefaultOpenInBrowser_ShouldBeFalse()
    {
        // Arrange & Act
        var settings = new DiffCommand.Settings();

        // Assert
        settings.OpenInBrowser.Should().BeFalse();
    }

    [Fact]
    public void Settings_DefaultHtmlOutput_ShouldBeNull()
    {
        // Arrange & Act
        var settings = new DiffCommand.Settings();

        // Assert
        settings.HtmlOutput.Should().BeNull();
    }

    [Fact]
    public void Settings_DefaultJsonOutput_ShouldBeNull()
    {
        // Arrange & Act
        var settings = new DiffCommand.Settings();

        // Assert
        settings.JsonOutput.Should().BeNull();
    }

    [Fact]
    public void Settings_DefaultTextOutput_ShouldBeNull()
    {
        // Arrange & Act
        var settings = new DiffCommand.Settings();

        // Assert
        settings.TextOutput.Should().BeNull();
    }

    [Fact]
    public void Settings_DefaultGitOutput_ShouldBeNull()
    {
        // Arrange & Act
        var settings = new DiffCommand.Settings();

        // Assert
        settings.GitOutput.Should().BeNull();
    }

    [Fact]
    public void Settings_DefaultPaths_ShouldBeEmpty()
    {
        // Arrange & Act
        var settings = new DiffCommand.Settings();

        // Assert
        settings.OldPath.Should().BeEmpty();
        settings.NewPath.Should().BeEmpty();
    }

    #endregion

    #region IgnoreWhitespace Option

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

    #endregion

    #region Context Option

    [Fact]
    public void Settings_ContextLines_WhenSetToZero_ShouldBeZero()
    {
        // Arrange & Act
        var settings = new DiffCommand.Settings { ContextLines = 0 };

        // Assert
        settings.ContextLines.Should().Be(0);
    }

    [Fact]
    public void Settings_ContextLines_WhenSetToTen_ShouldBeTen()
    {
        // Arrange & Act
        var settings = new DiffCommand.Settings { ContextLines = 10 };

        // Assert
        settings.ContextLines.Should().Be(10);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(5)]
    [InlineData(100)]
    public void Settings_ContextLines_WhenSetToPositiveValue_ShouldReturnThatValue(int lines)
    {
        // Arrange & Act
        var settings = new DiffCommand.Settings { ContextLines = lines };

        // Assert
        settings.ContextLines.Should().Be(lines);
    }

    #endregion

    #region Mode Option

    [Theory]
    [InlineData("auto")]
    [InlineData("roslyn")]
    [InlineData("line")]
    public void Settings_Mode_WhenSetToValidValue_ShouldReturnThatValue(string mode)
    {
        // Arrange & Act
        var settings = new DiffCommand.Settings { Mode = mode };

        // Assert
        settings.Mode.Should().Be(mode);
    }

    [Fact]
    public void Settings_Mode_WhenSetToAuto_ShouldBeAuto()
    {
        // Arrange & Act
        var settings = new DiffCommand.Settings { Mode = "auto" };

        // Assert
        settings.Mode.Should().Be("auto");
    }

    [Fact]
    public void Settings_Mode_WhenSetToRoslyn_ShouldBeRoslyn()
    {
        // Arrange & Act
        var settings = new DiffCommand.Settings { Mode = "roslyn" };

        // Assert
        settings.Mode.Should().Be("roslyn");
    }

    [Fact]
    public void Settings_Mode_WhenSetToLine_ShouldBeLine()
    {
        // Arrange & Act
        var settings = new DiffCommand.Settings { Mode = "line" };

        // Assert
        settings.Mode.Should().Be("line");
    }

    #endregion

    #region Output Format Options

    [Fact]
    public void Settings_HtmlOutput_WhenSetToPath_ShouldReturnThatPath()
    {
        // Arrange
        const string expectedPath = "/path/to/output.html";

        // Act
        var settings = new DiffCommand.Settings { HtmlOutput = expectedPath };

        // Assert
        settings.HtmlOutput.Should().Be(expectedPath);
    }

    [Fact]
    public void Settings_HtmlOutput_WhenSetToNull_ShouldBeNull()
    {
        // Arrange & Act
        var settings = new DiffCommand.Settings { HtmlOutput = null };

        // Assert
        settings.HtmlOutput.Should().BeNull();
    }

    [Theory]
    [InlineData("output.html")]
    [InlineData("./results/diff.html")]
    [InlineData("/absolute/path/output.html")]
    public void Settings_HtmlOutput_WhenSetToVariousPaths_ShouldReturnThatPath(string path)
    {
        // Arrange & Act
        var settings = new DiffCommand.Settings { HtmlOutput = path };

        // Assert
        settings.HtmlOutput.Should().Be(path);
    }

    #endregion

    #region Output Control Options

    [Fact]
    public void Settings_Quiet_WhenSetToTrue_ShouldBeTrue()
    {
        // Arrange & Act
        var settings = new DiffCommand.Settings { Quiet = true };

        // Assert
        settings.Quiet.Should().BeTrue();
    }

    [Fact]
    public void Settings_Quiet_WhenSetToFalse_ShouldBeFalse()
    {
        // Arrange & Act
        var settings = new DiffCommand.Settings { Quiet = false };

        // Assert
        settings.Quiet.Should().BeFalse();
    }

    [Fact]
    public void Settings_NoColor_WhenSetToTrue_ShouldBeTrue()
    {
        // Arrange & Act
        var settings = new DiffCommand.Settings { NoColor = true };

        // Assert
        settings.NoColor.Should().BeTrue();
    }

    [Fact]
    public void Settings_NoColor_WhenSetToFalse_ShouldBeFalse()
    {
        // Arrange & Act
        var settings = new DiffCommand.Settings { NoColor = false };

        // Assert
        settings.NoColor.Should().BeFalse();
    }

    [Fact]
    public void Settings_OpenInBrowser_WhenSetToTrue_ShouldBeTrue()
    {
        // Arrange & Act
        var settings = new DiffCommand.Settings { OpenInBrowser = true };

        // Assert
        settings.OpenInBrowser.Should().BeTrue();
    }

    [Fact]
    public void Settings_OpenInBrowser_WhenSetToFalse_ShouldBeFalse()
    {
        // Arrange & Act
        var settings = new DiffCommand.Settings { OpenInBrowser = false };

        // Assert
        settings.OpenInBrowser.Should().BeFalse();
    }

    #endregion

    #region Option Combinations

    [Fact]
    public void Settings_WithHtmlOutputAndOpenInBrowser_ShouldHaveCorrectValues()
    {
        // Arrange & Act
        var settings = new DiffCommand.Settings
        {
            HtmlOutput = "report.html",
            OpenInBrowser = true
        };

        // Assert
        settings.HtmlOutput.Should().Be("report.html");
        settings.OpenInBrowser.Should().BeTrue();
    }

    [Fact]
    public void Settings_WithQuietAndNoColor_ShouldHaveCorrectValues()
    {
        // Arrange & Act
        var settings = new DiffCommand.Settings
        {
            Quiet = true,
            NoColor = true
        };

        // Assert
        settings.Quiet.Should().BeTrue();
        settings.NoColor.Should().BeTrue();
    }

    [Fact]
    public void Settings_WithAllOptionsSet_ShouldHaveCorrectValues()
    {
        // Arrange & Act
        var settings = new DiffCommand.Settings
        {
            OldPath = "old.cs",
            NewPath = "new.cs",
            Mode = "roslyn",
            IgnoreWhitespace = true,
            ContextLines = 5,
            HtmlOutput = "diff-result.html",
            Quiet = false,
            NoColor = true,
            OpenInBrowser = true
        };

        // Assert
        settings.OldPath.Should().Be("old.cs");
        settings.NewPath.Should().Be("new.cs");
        settings.Mode.Should().Be("roslyn");
        settings.IgnoreWhitespace.Should().BeTrue();
        settings.ContextLines.Should().Be(5);
        settings.HtmlOutput.Should().Be("diff-result.html");
        settings.Quiet.Should().BeFalse();
        settings.NoColor.Should().BeTrue();
        settings.OpenInBrowser.Should().BeTrue();
    }

    [Fact]
    public void Settings_WithLineModeAndIgnoreWhitespace_ShouldHaveCorrectValues()
    {
        // Arrange & Act
        var settings = new DiffCommand.Settings
        {
            Mode = "line",
            IgnoreWhitespace = true,
            ContextLines = 0
        };

        // Assert
        settings.Mode.Should().Be("line");
        settings.IgnoreWhitespace.Should().BeTrue();
        settings.ContextLines.Should().Be(0);
    }

    #endregion

    #region File Paths

    [Theory]
    [InlineData("file.cs")]
    [InlineData("./relative/path/file.cs")]
    [InlineData("/absolute/path/file.cs")]
    [InlineData("file with spaces.cs")]
    public void Settings_OldPath_WhenSetToVariousPaths_ShouldReturnThatPath(string path)
    {
        // Arrange & Act
        var settings = new DiffCommand.Settings { OldPath = path };

        // Assert
        settings.OldPath.Should().Be(path);
    }

    [Theory]
    [InlineData("file.cs")]
    [InlineData("./relative/path/file.cs")]
    [InlineData("/absolute/path/file.cs")]
    [InlineData("file with spaces.cs")]
    public void Settings_NewPath_WhenSetToVariousPaths_ShouldReturnThatPath(string path)
    {
        // Arrange & Act
        var settings = new DiffCommand.Settings { NewPath = path };

        // Assert
        settings.NewPath.Should().Be(path);
    }

    #endregion

    #region Invalid Option Values (Settings Level)

    [Theory]
    [InlineData("invalid")]
    [InlineData("ROSLYN")]
    [InlineData("LINE")]
    [InlineData("AUTO")]
    public void Settings_Mode_WhenSetToArbitraryString_ShouldStoreIt(string mode)
    {
        // Arrange & Act
        // Note: Settings allows any string value; validation happens at execution time
        var settings = new DiffCommand.Settings { Mode = mode };

        // Assert
        settings.Mode.Should().Be(mode);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(-100)]
    public void Settings_ContextLines_WhenSetToNegativeValue_ShouldStoreIt(int lines)
    {
        // Arrange & Act
        // Note: Settings allows any int value; validation could happen at execution time
        var settings = new DiffCommand.Settings { ContextLines = lines };

        // Assert
        settings.ContextLines.Should().Be(lines);
    }

    #endregion

    #region Validation Tests

    [Fact]
    public void Validate_WithOpenInBrowserWithoutHtmlOutput_ShouldReturnError()
    {
        // Arrange
        var settings = new DiffCommand.Settings
        {
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
    public void Validate_WithOpenInBrowserAndHtmlOutput_ShouldSucceed()
    {
        // Arrange
        var settings = new DiffCommand.Settings
        {
            OpenInBrowser = true,
            HtmlOutput = "output.html"
        };

        // Act
        var result = settings.Validate();

        // Assert
        result.Successful.Should().BeTrue();
    }

    [Fact]
    public void Validate_WithHtmlOutputEmptyString_ShouldReturnError()
    {
        // Arrange
        var settings = new DiffCommand.Settings
        {
            HtmlOutput = "   " // whitespace only
        };

        // Act
        var result = settings.Validate();

        // Assert
        result.Successful.Should().BeFalse();
        result.Message.Should().Contain("--html requires a file path");
    }

    [Fact]
    public void Validate_WithValidSettings_ShouldSucceed()
    {
        // Arrange
        var settings = new DiffCommand.Settings
        {
            OldPath = "old.cs",
            NewPath = "new.cs",
            HtmlOutput = "output.html"
        };

        // Act
        var result = settings.Validate();

        // Assert
        result.Successful.Should().BeTrue();
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
}
