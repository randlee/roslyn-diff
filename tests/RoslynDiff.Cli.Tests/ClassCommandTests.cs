namespace RoslynDiff.Cli.Tests;

using FluentAssertions;
using RoslynDiff.Cli.Commands;
using Spectre.Console.Cli;
using Xunit;

/// <summary>
/// Unit tests for <see cref="ClassCommand"/>.
/// </summary>
public class ClassCommandTests
{
    #region Settings Tests - Default Values

    [Fact]
    public void Settings_DefaultMatchBy_ShouldBeAuto()
    {
        // Arrange & Act
        var settings = new ClassCommand.Settings();

        // Assert
        settings.MatchBy.Should().Be("auto");
    }

    [Fact]
    public void Settings_DefaultSimilarity_ShouldBePointEight()
    {
        // Arrange & Act
        var settings = new ClassCommand.Settings();

        // Assert
        settings.Similarity.Should().Be(0.8);
    }

    [Fact]
    public void Settings_DefaultInterfaceName_ShouldBeNull()
    {
        // Arrange & Act
        var settings = new ClassCommand.Settings();

        // Assert
        settings.InterfaceName.Should().BeNull();
    }

    [Fact]
    public void Settings_DefaultHtmlOutput_ShouldBeNull()
    {
        // Arrange & Act
        var settings = new ClassCommand.Settings();

        // Assert
        settings.HtmlOutput.Should().BeNull();
    }

    [Fact]
    public void Settings_DefaultJsonOutput_ShouldBeNull()
    {
        // Arrange & Act
        var settings = new ClassCommand.Settings();

        // Assert
        settings.JsonOutput.Should().BeNull();
    }

    [Fact]
    public void Settings_DefaultTextOutput_ShouldBeNull()
    {
        // Arrange & Act
        var settings = new ClassCommand.Settings();

        // Assert
        settings.TextOutput.Should().BeNull();
    }

    [Fact]
    public void Settings_DefaultGitOutput_ShouldBeNull()
    {
        // Arrange & Act
        var settings = new ClassCommand.Settings();

        // Assert
        settings.GitOutput.Should().BeNull();
    }

    [Fact]
    public void Settings_DefaultQuiet_ShouldBeFalse()
    {
        // Arrange & Act
        var settings = new ClassCommand.Settings();

        // Assert
        settings.Quiet.Should().BeFalse();
    }

    [Fact]
    public void Settings_DefaultNoColor_ShouldBeFalse()
    {
        // Arrange & Act
        var settings = new ClassCommand.Settings();

        // Assert
        settings.NoColor.Should().BeFalse();
    }

    [Fact]
    public void Settings_DefaultOpenInBrowser_ShouldBeFalse()
    {
        // Arrange & Act
        var settings = new ClassCommand.Settings();

        // Assert
        settings.OpenInBrowser.Should().BeFalse();
    }

    #endregion

    #region Settings Tests - Output Options

    [Fact]
    public void Settings_HtmlOutput_WhenSetToPath_ShouldReturnThatPath()
    {
        // Arrange
        const string expectedPath = "/path/to/output.html";

        // Act
        var settings = new ClassCommand.Settings { HtmlOutput = expectedPath };

        // Assert
        settings.HtmlOutput.Should().Be(expectedPath);
    }

    [Theory]
    [InlineData("output.html")]
    [InlineData("./results/diff.html")]
    [InlineData("/absolute/path/output.html")]
    public void Settings_HtmlOutput_WhenSetToVariousPaths_ShouldReturnThatPath(string path)
    {
        // Arrange & Act
        var settings = new ClassCommand.Settings { HtmlOutput = path };

        // Assert
        settings.HtmlOutput.Should().Be(path);
    }

    #endregion

    #region Settings Tests - Control Options

    [Fact]
    public void Settings_Quiet_WhenSetToTrue_ShouldBeTrue()
    {
        // Arrange & Act
        var settings = new ClassCommand.Settings { Quiet = true };

        // Assert
        settings.Quiet.Should().BeTrue();
    }

    [Fact]
    public void Settings_NoColor_WhenSetToTrue_ShouldBeTrue()
    {
        // Arrange & Act
        var settings = new ClassCommand.Settings { NoColor = true };

        // Assert
        settings.NoColor.Should().BeTrue();
    }

    [Fact]
    public void Settings_OpenInBrowser_WhenSetToTrue_ShouldBeTrue()
    {
        // Arrange & Act
        var settings = new ClassCommand.Settings { OpenInBrowser = true };

        // Assert
        settings.OpenInBrowser.Should().BeTrue();
    }

    #endregion

    #region Validation Tests

    [Fact]
    public void Validate_WithOpenInBrowserWithoutHtmlOutput_ShouldReturnError()
    {
        // Arrange
        var settings = new ClassCommand.Settings
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
        var settings = new ClassCommand.Settings
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
        var settings = new ClassCommand.Settings
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
    public void Validate_WithDefaultSettings_ShouldSucceed()
    {
        // Arrange
        var settings = new ClassCommand.Settings();

        // Act
        var result = settings.Validate();

        // Assert
        result.Successful.Should().BeTrue();
    }

    #endregion
}

/// <summary>
/// Unit tests for <see cref="ClassSpecParser"/>.
/// </summary>
public class ClassSpecParserTests
{
    #region Parse Tests - Basic Format

    [Fact]
    public void Parse_WithClassNameSpec_ShouldReturnFilePathAndClassName()
    {
        // Arrange
        var spec = "file.cs:Foo";

        // Act
        var (filePath, className) = ClassSpecParser.Parse(spec);

        // Assert
        filePath.Should().Be("file.cs");
        className.Should().Be("Foo");
    }

    [Fact]
    public void Parse_WithFileOnlySpec_ShouldReturnFilePathAndNullClassName()
    {
        // Arrange
        var spec = "file.cs";

        // Act
        var (filePath, className) = ClassSpecParser.Parse(spec);

        // Assert
        filePath.Should().Be("file.cs");
        className.Should().BeNull();
    }

    [Fact]
    public void Parse_WithPathContainingSpaces_ShouldReturnCorrectFilePath()
    {
        // Arrange
        var spec = "/path/to/my file.cs:MyClass";

        // Act
        var (filePath, className) = ClassSpecParser.Parse(spec);

        // Assert
        filePath.Should().Be("/path/to/my file.cs");
        className.Should().Be("MyClass");
    }

    [Fact]
    public void Parse_WithPathContainingDots_ShouldReturnCorrectFilePath()
    {
        // Arrange
        var spec = "MyProject.Core.Services/file.cs:MyClass";

        // Act
        var (filePath, className) = ClassSpecParser.Parse(spec);

        // Assert
        filePath.Should().Be("MyProject.Core.Services/file.cs");
        className.Should().Be("MyClass");
    }

    #endregion

    #region Parse Tests - Windows Paths

    [Fact]
    public void Parse_WithWindowsPathAndClassName_ShouldReturnCorrectFilePath()
    {
        // Arrange
        var spec = @"C:\Users\dev\file.cs:MyClass";

        // Act
        var (filePath, className) = ClassSpecParser.Parse(spec);

        // Assert
        filePath.Should().Be(@"C:\Users\dev\file.cs");
        className.Should().Be("MyClass");
    }

    [Fact]
    public void Parse_WithWindowsPathOnly_ShouldReturnCorrectFilePath()
    {
        // Arrange
        var spec = @"C:\Users\dev\file.cs";

        // Act
        var (filePath, className) = ClassSpecParser.Parse(spec);

        // Assert
        filePath.Should().Be(@"C:\Users\dev\file.cs");
        className.Should().BeNull();
    }

    [Fact]
    public void Parse_WithWindowsUncPath_ShouldReturnCorrectFilePath()
    {
        // Arrange
        var spec = @"\\server\share\file.cs:MyClass";

        // Act
        var (filePath, className) = ClassSpecParser.Parse(spec);

        // Assert
        filePath.Should().Be(@"\\server\share\file.cs");
        className.Should().Be("MyClass");
    }

    #endregion

    #region Parse Tests - Edge Cases

    [Fact]
    public void Parse_WithUnderscoreInClassName_ShouldReturnCorrectClassName()
    {
        // Arrange
        var spec = "file.cs:My_Class_Name";

        // Act
        var (filePath, className) = ClassSpecParser.Parse(spec);

        // Assert
        filePath.Should().Be("file.cs");
        className.Should().Be("My_Class_Name");
    }

    [Fact]
    public void Parse_WithNumericSuffixInClassName_ShouldReturnCorrectClassName()
    {
        // Arrange
        var spec = "file.cs:MyClass123";

        // Act
        var (filePath, className) = ClassSpecParser.Parse(spec);

        // Assert
        filePath.Should().Be("file.cs");
        className.Should().Be("MyClass123");
    }

    [Fact]
    public void Parse_WithWhitespaceAroundValues_ShouldTrimCorrectly()
    {
        // Arrange
        var spec = " file.cs : MyClass ";

        // Act
        var (filePath, className) = ClassSpecParser.Parse(spec);

        // Assert
        filePath.Should().Be("file.cs");
        className.Should().Be("MyClass");
    }

    #endregion

    #region Parse Tests - Error Cases

    [Fact]
    public void Parse_WithNullSpec_ShouldThrowArgumentException()
    {
        // Arrange & Act
        var act = () => ClassSpecParser.Parse(null!);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Parse_WithEmptySpec_ShouldThrowArgumentException()
    {
        // Arrange & Act
        var act = () => ClassSpecParser.Parse(string.Empty);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Parse_WithWhitespaceOnlySpec_ShouldThrowArgumentException()
    {
        // Arrange & Act
        var act = () => ClassSpecParser.Parse("   ");

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Parse_WithEmptyClassName_ShouldThrowArgumentException()
    {
        // Arrange & Act
        var act = () => ClassSpecParser.Parse("file.cs:");

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Class name cannot be empty*");
    }

    [Fact]
    public void Parse_WithInvalidClassNameStartingWithDigit_ShouldThrowArgumentException()
    {
        // Arrange & Act
        var act = () => ClassSpecParser.Parse("file.cs:123Invalid");

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Invalid class name*");
    }

    [Fact]
    public void Parse_WithInvalidClassNameWithSpecialChars_ShouldThrowArgumentException()
    {
        // Arrange & Act
        var act = () => ClassSpecParser.Parse("file.cs:My-Class");

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Invalid class name*");
    }

    #endregion

    #region ParseToClassSpec Tests

    [Fact]
    public void ParseToClassSpec_ReturnsClassSpecRecord()
    {
        // Arrange
        var spec = "file.cs:MyClass";

        // Act
        var result = ClassSpecParser.ParseToClassSpec(spec);

        // Assert
        result.Should().NotBeNull();
        result.FilePath.Should().Be("file.cs");
        result.ClassName.Should().Be("MyClass");
    }

    [Fact]
    public void ParseToClassSpec_WithFileOnly_ReturnsNullClassName()
    {
        // Arrange
        var spec = "file.cs";

        // Act
        var result = ClassSpecParser.ParseToClassSpec(spec);

        // Assert
        result.Should().NotBeNull();
        result.FilePath.Should().Be("file.cs");
        result.ClassName.Should().BeNull();
    }

    #endregion
}
