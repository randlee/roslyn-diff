namespace RoslynDiff.Cli.Tests;

using FluentAssertions;
using RoslynDiff.Cli.Commands;
using Xunit;

/// <summary>
/// Unit tests for <see cref="ClassCommand"/>.
/// </summary>
public class ClassCommandTests
{
    #region Settings Tests

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
    public void Settings_DefaultOutput_ShouldBeText()
    {
        // Arrange & Act
        var settings = new ClassCommand.Settings();

        // Assert
        settings.Output.Should().Be("text");
    }

    [Fact]
    public void Settings_DefaultOutFile_ShouldBeNull()
    {
        // Arrange & Act
        var settings = new ClassCommand.Settings();

        // Assert
        settings.OutFile.Should().BeNull();
    }

    [Fact]
    public void Settings_DefaultInterfaceName_ShouldBeNull()
    {
        // Arrange & Act
        var settings = new ClassCommand.Settings();

        // Assert
        settings.InterfaceName.Should().BeNull();
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
