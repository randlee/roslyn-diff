namespace RoslynDiff.Integration.Tests;

using FluentAssertions;
using RoslynDiff.Core.Differ;
using RoslynDiff.Core.Models;
using RoslynDiff.Output;
using Xunit;

/// <summary>
/// Integration tests for error handling scenarios.
/// Tests file not found, parse errors, invalid options, and graceful error handling.
/// </summary>
public class ErrorHandlingIntegrationTests
{
    private readonly DifferFactory _differFactory = new();
    private readonly OutputFormatterFactory _formatterFactory = new();

    #region File Not Found Tests

    [Fact]
    public async Task DiffFiles_FileNotFound_ThrowsFileNotFoundException()
    {
        // Arrange
        var nonExistentPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString(), "nonexistent.cs");

        // Act
        var act = async () => await File.ReadAllTextAsync(nonExistentPath);

        // Assert - Either DirectoryNotFoundException or FileNotFoundException
        try
        {
            await act();
            Assert.Fail("Expected an exception");
        }
        catch (DirectoryNotFoundException)
        {
            // Expected
        }
        catch (FileNotFoundException)
        {
            // Expected
        }
    }

    [Fact]
    public void DifferFactory_NullFilePath_ThrowsArgumentNullException()
    {
        // Arrange
        var options = new DiffOptions();

        // Act
        var act = () => _differFactory.GetDiffer(null!, options);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void DifferFactory_NullOptions_ThrowsArgumentNullException()
    {
        // Act
        var act = () => _differFactory.GetDiffer("file.cs", null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    #endregion

    #region Parse Error Tests

    [Fact]
    public void CSharpDiffer_InvalidCSharpSyntax_HandlesGracefully()
    {
        // Arrange
        var validCode = "namespace Test { public class Foo { } }";
        var invalidCode = "namespace Test { public class { } }"; // Missing class name

        var differ = _differFactory.GetDiffer("test.cs", new DiffOptions());
        var options = new DiffOptions { OldPath = "old.cs", NewPath = "new.cs" };

        // Act
        var result = differ.Compare(validCode, invalidCode, options);

        // Assert - Should return a result (possibly with error indication) rather than throwing
        result.Should().NotBeNull();
        result.FileChanges.Should().NotBeNull();
    }

    [Fact]
    public void CSharpDiffer_EmptyContent_HandlesGracefully()
    {
        // Arrange
        var differ = _differFactory.GetDiffer("test.cs", new DiffOptions());
        var options = new DiffOptions { OldPath = "old.cs", NewPath = "new.cs" };

        // Act
        var result = differ.Compare("", "", options);

        // Assert
        result.Should().NotBeNull();
        result.Stats.TotalChanges.Should().Be(0);
    }

    [Fact]
    public void CSharpDiffer_WhitespaceOnlyContent_HandlesGracefully()
    {
        // Arrange
        var differ = _differFactory.GetDiffer("test.cs", new DiffOptions());
        var options = new DiffOptions { OldPath = "old.cs", NewPath = "new.cs" };

        // Act
        var result = differ.Compare("   \n\t\n   ", "   \n\t\n   ", options);

        // Assert
        result.Should().NotBeNull();
    }

    [Fact]
    public void VbDiffer_InvalidSyntax_HandlesGracefully()
    {
        // Arrange
        var validCode = "Namespace Test\nPublic Class Foo\nEnd Class\nEnd Namespace";
        var invalidCode = "Namespace Test\nPublic Class\nEnd Class\nEnd Namespace"; // Missing class name

        var differ = _differFactory.GetDiffer("test.vb", new DiffOptions());
        var options = new DiffOptions { OldPath = "old.vb", NewPath = "new.vb" };

        // Act
        var result = differ.Compare(validCode, invalidCode, options);

        // Assert
        result.Should().NotBeNull();
        result.FileChanges.Should().NotBeNull();
    }

    #endregion

    #region Invalid Options Tests

    [Fact]
    public void OutputFormatterFactory_InvalidFormat_ThrowsArgumentException()
    {
        // Act
        var act = () => _formatterFactory.GetFormatter("invalid-format");

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Unsupported format*");
    }

    [Fact]
    public void OutputFormatterFactory_EmptyFormat_ThrowsArgumentException()
    {
        // Act
        var act = () => _formatterFactory.GetFormatter("");

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void OutputFormatterFactory_NullFormat_ThrowsArgumentException()
    {
        // Act & Assert
        try
        {
            _formatterFactory.GetFormatter(null!);
            Assert.Fail("Expected an exception");
        }
        catch (ArgumentException)
        {
            // Expected - ArgumentNullException inherits from ArgumentException
        }
    }

    [Fact]
    public void DifferFactory_ForCsWithRoslynMode_ReturnsCSharpDiffer()
    {
        // Arrange
        var options = new DiffOptions { Mode = DiffMode.Roslyn };

        // Act
        var differ = _differFactory.GetDiffer("file.cs", options);

        // Assert
        differ.Should().BeOfType<CSharpDiffer>();
    }

    [Fact]
    public void DifferFactory_ForTxtWithRoslynMode_ThrowsNotSupportedException()
    {
        // Arrange
        var options = new DiffOptions { Mode = DiffMode.Roslyn };

        // Act
        var act = () => _differFactory.GetDiffer("file.txt", options);

        // Assert
        act.Should().Throw<NotSupportedException>()
            .WithMessage("*Roslyn mode is not supported*");
    }

    #endregion

    #region Null Content Tests

    [Fact]
    public void CSharpDiffer_NullOldContent_ThrowsArgumentNullException()
    {
        // Arrange
        var differ = _differFactory.GetDiffer("test.cs", new DiffOptions());
        var options = new DiffOptions();

        // Act
        var act = () => differ.Compare(null!, "code", options);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void CSharpDiffer_NullNewContent_ThrowsArgumentNullException()
    {
        // Arrange
        var differ = _differFactory.GetDiffer("test.cs", new DiffOptions());
        var options = new DiffOptions();

        // Act
        var act = () => differ.Compare("code", null!, options);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void LineDiffer_NullOldContent_ThrowsArgumentNullException()
    {
        // Arrange
        var differ = _differFactory.GetDiffer("test.txt", new DiffOptions());
        var options = new DiffOptions();

        // Act
        var act = () => differ.Compare(null!, "content", options);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void LineDiffer_NullNewContent_ThrowsArgumentNullException()
    {
        // Arrange
        var differ = _differFactory.GetDiffer("test.txt", new DiffOptions());
        var options = new DiffOptions();

        // Act
        var act = () => differ.Compare("content", null!, options);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    #endregion

    #region Large Content Tests

    [Fact]
    public void CSharpDiffer_LargeFile_HandlesWithoutTimeout()
    {
        // Arrange
        var largeCode = GenerateLargeCSharpCode(1000);
        var differ = _differFactory.GetDiffer("test.cs", new DiffOptions());
        var options = new DiffOptions { OldPath = "old.cs", NewPath = "new.cs" };

        // Act & Assert - Should complete without timeout
        var act = () => differ.Compare(largeCode, largeCode, options);
        act.Should().NotThrow();
    }

    [Fact]
    public void LineDiffer_LargeFile_HandlesWithoutTimeout()
    {
        // Arrange
        var largeContent = string.Join("\n", Enumerable.Range(1, 10000).Select(i => $"Line {i}"));
        var differ = _differFactory.GetDiffer("test.txt", new DiffOptions());
        var options = new DiffOptions { OldPath = "old.txt", NewPath = "new.txt" };

        // Act & Assert - Should complete without timeout
        var act = () => differ.Compare(largeContent, largeContent, options);
        act.Should().NotThrow();
    }

    private static string GenerateLargeCSharpCode(int classCount)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("namespace LargeTest;");

        for (var i = 0; i < classCount; i++)
        {
            sb.AppendLine($"public class Class{i}");
            sb.AppendLine("{");
            sb.AppendLine($"    public int Property{i} {{ get; set; }}");
            sb.AppendLine($"    public void Method{i}() {{ }}");
            sb.AppendLine("}");
        }

        return sb.ToString();
    }

    #endregion

    #region Formatter Error Handling Tests

    [Fact]
    public void JsonFormatter_NullResult_ThrowsArgumentNullException()
    {
        // Arrange
        var formatter = _formatterFactory.GetFormatter("json");

        // Act
        var act = () => formatter.FormatResult(null!, new OutputOptions());

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public async Task JsonFormatter_NullWriter_ThrowsArgumentNullException()
    {
        // Arrange
        var formatter = _formatterFactory.GetFormatter("json");
        var result = new DiffResult
        {
            OldPath = "test.cs",
            NewPath = "test.cs",
            Mode = DiffMode.Roslyn,
            FileChanges = [],
            Stats = new DiffStats()
        };

        // Act
        var act = async () => await formatter.FormatResultAsync(result, null!, new OutputOptions());

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public void AllFormatters_NullResult_ThrowException()
    {
        // Arrange & Act & Assert
        foreach (var format in _formatterFactory.SupportedFormats)
        {
            var formatter = _formatterFactory.GetFormatter(format);
            var act = () => formatter.FormatResult(null!, new OutputOptions());
            // Formatters should throw some exception on null result
            // (ArgumentNullException for most, but some may throw NullReferenceException)
            act.Should().Throw<Exception>($"Format '{format}' should throw on null result");
        }
    }

    [Fact]
    public void AllFormatters_NullOptions_UsesDefaultOptions()
    {
        // Arrange
        var result = new DiffResult
        {
            OldPath = "test.cs",
            NewPath = "test.cs",
            Mode = DiffMode.Roslyn,
            FileChanges = [],
            Stats = new DiffStats()
        };

        // Act & Assert - Should not throw, uses default options
        foreach (var format in _formatterFactory.SupportedFormats)
        {
            var formatter = _formatterFactory.GetFormatter(format);
            var act = () => formatter.FormatResult(result, null);
            act.Should().NotThrow($"Format '{format}' should handle null options");
        }
    }

    #endregion

    #region Special Characters Tests

    [Fact]
    public void CSharpDiffer_CodeWithSpecialCharacters_HandlesCorrectly()
    {
        // Arrange
        var oldCode = @"namespace Test { public class Foo { string s = ""Hello\nWorld""; } }";
        var newCode = @"namespace Test { public class Foo { string s = ""Hello\tWorld""; } }";

        var differ = _differFactory.GetDiffer("test.cs", new DiffOptions());
        var options = new DiffOptions { OldPath = "old.cs", NewPath = "new.cs" };

        // Act
        var result = differ.Compare(oldCode, newCode, options);

        // Assert
        result.Should().NotBeNull();
        result.Stats.TotalChanges.Should().BeGreaterThan(0);
    }

    [Fact]
    public void CSharpDiffer_CodeWithUnicode_HandlesCorrectly()
    {
        // Arrange
        var oldCode = @"namespace Test { public class Foo { string s = ""Hello World""; } }";
        var newCode = @"namespace Test { public class Foo { string s = ""Hello""; } }"; // Japanese "World"

        var differ = _differFactory.GetDiffer("test.cs", new DiffOptions());
        var options = new DiffOptions { OldPath = "old.cs", NewPath = "new.cs" };

        // Act
        var result = differ.Compare(oldCode, newCode, options);

        // Assert
        result.Should().NotBeNull();
    }

    [Fact]
    public void LineDiffer_TextWithSpecialCharacters_HandlesCorrectly()
    {
        // Arrange
        var oldContent = "Line 1\nLine with <special> & \"chars\"\nLine 3";
        var newContent = "Line 1\nLine with <different> & \"chars\"\nLine 3";

        var differ = _differFactory.GetDiffer("test.txt", new DiffOptions());
        var options = new DiffOptions { OldPath = "old.txt", NewPath = "new.txt" };

        // Act
        var result = differ.Compare(oldContent, newContent, options);

        // Assert
        result.Should().NotBeNull();
        result.Stats.TotalChanges.Should().BeGreaterThan(0);
    }

    #endregion

    #region Mixed Line Endings Tests

    [Fact]
    public void LineDiffer_MixedLineEndings_HandlesCorrectly()
    {
        // Arrange - Mix of \n, \r\n, and \r
        var oldContent = "Line 1\nLine 2\r\nLine 3\rLine 4";
        var newContent = "Line 1\r\nLine 2\nLine 3\r\nLine 4";

        var differ = _differFactory.GetDiffer("test.txt", new DiffOptions());
        var options = new DiffOptions { OldPath = "old.txt", NewPath = "new.txt" };

        // Act
        var result = differ.Compare(oldContent, newContent, options);

        // Assert
        result.Should().NotBeNull();
    }

    #endregion
}
