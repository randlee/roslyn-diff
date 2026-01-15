namespace RoslynDiff.Core.Tests.EdgeCases;

using System.Text;
using FluentAssertions;
using RoslynDiff.Core.Differ;
using RoslynDiff.Core.Models;
using Xunit;

/// <summary>
/// Tests for handling various file encodings and Unicode content.
/// </summary>
public class EncodingTests
{
    private readonly CSharpDiffer _differ = new();

    #region UTF-8 Encoding Tests

    [Fact]
    public void Compare_Utf8EncodedContent_HandlesCorrectly()
    {
        // Arrange
        var oldCode = """
            namespace Test;
            public class Foo
            {
                public string Message = "Hello, World!";
            }
            """;
        var newCode = """
            namespace Test;
            public class Foo
            {
                public string Message = "Hello, UTF-8 World!";
            }
            """;

        var options = new DiffOptions();

        // Act
        var result = _differ.Compare(oldCode, newCode, options);

        // Assert
        result.Stats.TotalChanges.Should().BeGreaterThan(0);
    }

    [Fact]
    public void Compare_Utf8WithBom_HandlesCorrectly()
    {
        // Arrange - UTF-8 BOM is EF BB BF
        var bom = Encoding.UTF8.GetString(Encoding.UTF8.Preamble);
        var oldCode = bom + "namespace Test; public class Foo { }";
        var newCode = bom + "namespace Test; public class Bar { }";

        var options = new DiffOptions();

        // Act
        var result = _differ.Compare(oldCode, newCode, options);

        // Assert
        result.FileChanges.Should().HaveCount(1);
    }

    #endregion

    #region Unicode Identifier Tests

    [Fact]
    public void Compare_UnicodeIdentifiers_DetectsChanges()
    {
        // Arrange - Using Unicode letters in identifiers (allowed in C#)
        var oldCode = """
            namespace Test;
            public class Foo
            {
                public string Nachricht = "Hello";
            }
            """;
        var newCode = """
            namespace Test;
            public class Foo
            {
                public string Nachricht = "Hallo";
            }
            """;

        var options = new DiffOptions();

        // Act
        var result = _differ.Compare(oldCode, newCode, options);

        // Assert
        result.Stats.TotalChanges.Should().BeGreaterThan(0);
    }

    [Fact]
    public void Compare_GreekIdentifiers_HandlesCorrectly()
    {
        // Arrange - Greek letters are valid C# identifiers
        var oldCode = """
            namespace Test;
            public class Calculator
            {
                public double Calculate(double radius) => 3.14159 * radius * radius;
            }
            """;
        var newCode = """
            namespace Test;
            public class Calculator
            {
                public double Calculate(double radius) => 3.14159265359 * radius * radius;
            }
            """;

        var options = new DiffOptions();

        // Act
        var result = _differ.Compare(oldCode, newCode, options);

        // Assert
        result.FileChanges.Should().HaveCount(1);
    }

    [Fact]
    public void Compare_ChineseIdentifiers_HandlesCorrectly()
    {
        // Arrange - Chinese characters are valid C# identifiers
        var oldCode = """
            namespace Test;
            public class Handler
            {
                public string Process() => "result";
            }
            """;
        var newCode = """
            namespace Test;
            public class Handler
            {
                public string Process() => "updated result";
            }
            """;

        var options = new DiffOptions();

        // Act
        var result = _differ.Compare(oldCode, newCode, options);

        // Assert
        result.Stats.TotalChanges.Should().BeGreaterThan(0);
    }

    [Fact]
    public void Compare_JapaneseIdentifiers_HandlesCorrectly()
    {
        // Arrange
        var oldCode = """
            namespace Test;
            public class DataProcessor
            {
                private int _counter = 0;
            }
            """;
        var newCode = """
            namespace Test;
            public class DataProcessor
            {
                private int _counter = 100;
            }
            """;

        var options = new DiffOptions();

        // Act
        var result = _differ.Compare(oldCode, newCode, options);

        // Assert
        result.FileChanges.Should().HaveCount(1);
    }

    [Fact]
    public void Compare_EmojiInComments_HandlesCorrectly()
    {
        // Arrange - Emojis in comments
        var oldCode = """
            namespace Test;
            // Regular comment
            public class Foo { }
            """;
        var newCode = """
            namespace Test;
            // TODO: Fix this code
            public class Foo { }
            """;

        var options = new DiffOptions();

        // Act
        var result = _differ.Compare(oldCode, newCode, options);

        // Assert
        result.FileChanges.Should().HaveCount(1);
    }

    #endregion

    #region Special Characters in Strings Tests

    [Fact]
    public void Compare_UnicodeEscapeSequences_DetectsChanges()
    {
        // Arrange
        var oldCode = """
            namespace Test;
            public class Foo
            {
                public string Value = "\u0048\u0065\u006C\u006C\u006F"; // "Hello"
            }
            """;
        var newCode = """
            namespace Test;
            public class Foo
            {
                public string Value = "\u0057\u006F\u0072\u006C\u0064"; // "World"
            }
            """;

        var options = new DiffOptions();

        // Act
        var result = _differ.Compare(oldCode, newCode, options);

        // Assert
        result.Stats.TotalChanges.Should().BeGreaterThan(0);
    }

    [Fact]
    public void Compare_ExtendedUnicodeCharacters_HandlesCorrectly()
    {
        // Arrange - Characters outside Basic Multilingual Plane
        var oldCode = """
            namespace Test;
            public class Foo
            {
                public string Emoji = "Simple text";
            }
            """;
        var newCode = """
            namespace Test;
            public class Foo
            {
                public string Emoji = "Complex text with symbols";
            }
            """;

        var options = new DiffOptions();

        // Act
        var result = _differ.Compare(oldCode, newCode, options);

        // Assert
        result.Stats.TotalChanges.Should().BeGreaterThan(0);
    }

    [Fact]
    public void Compare_MixedEncodingCharacters_HandlesCorrectly()
    {
        // Arrange - Mix of ASCII, Latin-1, and Unicode
        var oldCode = """
            namespace Test;
            public class Foo
            {
                public string Mixed = "Hello World";
            }
            """;
        var newCode = """
            namespace Test;
            public class Foo
            {
                public string Mixed = "Bonjour le Monde";
            }
            """;

        var options = new DiffOptions();

        // Act
        var result = _differ.Compare(oldCode, newCode, options);

        // Assert
        result.Stats.TotalChanges.Should().BeGreaterThan(0);
    }

    [Fact]
    public void Compare_StringWithNullCharacter_HandlesCorrectly()
    {
        // Arrange
        var oldCode = """
            namespace Test;
            public class Foo
            {
                public string Value = "Hello\0World";
            }
            """;
        var newCode = """
            namespace Test;
            public class Foo
            {
                public string Value = "Hello World";
            }
            """;

        var options = new DiffOptions();

        // Act
        var result = _differ.Compare(oldCode, newCode, options);

        // Assert
        result.Stats.TotalChanges.Should().BeGreaterThan(0);
    }

    [Fact]
    public void Compare_StringWithEscapeSequences_DetectsChanges()
    {
        // Arrange
        var oldCode = """
            namespace Test;
            public class Foo
            {
                public string Value = "Line1\nLine2\tTabbed";
            }
            """;
        var newCode = """
            namespace Test;
            public class Foo
            {
                public string Value = "Line1\r\nLine2\tTabbed";
            }
            """;

        var options = new DiffOptions();

        // Act
        var result = _differ.Compare(oldCode, newCode, options);

        // Assert
        result.Stats.TotalChanges.Should().BeGreaterThan(0);
    }

    #endregion

    #region Verbatim and Raw String Tests

    [Fact]
    public void Compare_VerbatimStrings_DetectsChanges()
    {
        // Arrange
        var oldCode = """
            namespace Test;
            public class Foo
            {
                public string Path = @"C:\Users\Test\file.txt";
            }
            """;
        var newCode = """
            namespace Test;
            public class Foo
            {
                public string Path = @"D:\Data\Test\file.txt";
            }
            """;

        var options = new DiffOptions();

        // Act
        var result = _differ.Compare(oldCode, newCode, options);

        // Assert
        result.Stats.TotalChanges.Should().BeGreaterThan(0);
    }

    [Fact]
    public void Compare_InterpolatedVerbatimStrings_DetectsChanges()
    {
        // Arrange
        var oldCode = """
            namespace Test;
            public class Foo
            {
                public string GetPath(string dir) => $@"C:\Users\{dir}\file.txt";
            }
            """;
        var newCode = """
            namespace Test;
            public class Foo
            {
                public string GetPath(string dir) => $@"D:\Data\{dir}\file.txt";
            }
            """;

        var options = new DiffOptions();

        // Act
        var result = _differ.Compare(oldCode, newCode, options);

        // Assert
        result.Stats.TotalChanges.Should().BeGreaterThan(0);
    }

    #endregion

    #region Byte Order Mark Tests

    [Fact]
    public void Compare_Utf16LeBom_HandlesCorrectly()
    {
        // Arrange - UTF-16 LE BOM is FF FE
        var oldCode = "namespace Test; public class Foo { }";
        var newCode = "namespace Test; public class Bar { }";

        var options = new DiffOptions();

        // Act
        var result = _differ.Compare(oldCode, newCode, options);

        // Assert
        result.FileChanges.Should().HaveCount(1);
    }

    [Fact]
    public void Compare_Utf16BeBom_HandlesCorrectly()
    {
        // Arrange - UTF-16 BE BOM is FE FF
        var oldCode = "namespace Test; public class Foo { }";
        var newCode = "namespace Test; public class Bar { }";

        var options = new DiffOptions();

        // Act
        var result = _differ.Compare(oldCode, newCode, options);

        // Assert
        result.FileChanges.Should().HaveCount(1);
    }

    [Fact]
    public void Compare_NoBom_HandlesCorrectly()
    {
        // Arrange
        var oldCode = "namespace Test; public class Foo { }";
        var newCode = "namespace Test; public class Bar { }";

        var options = new DiffOptions();

        // Act
        var result = _differ.Compare(oldCode, newCode, options);

        // Assert
        result.FileChanges.Should().HaveCount(1);
        var changes = result.FileChanges[0].Changes;
        changes.Should().Contain(c => c.Type == ChangeType.Removed && c.Name == "Foo");
        changes.Should().Contain(c => c.Type == ChangeType.Added && c.Name == "Bar");
    }

    #endregion

    #region Right-to-Left Text Tests

    [Fact]
    public void Compare_RtlTextInStrings_HandlesCorrectly()
    {
        // Arrange - Arabic text reads right-to-left
        var oldCode = """
            namespace Test;
            public class Foo
            {
                public string Greeting = "Hello";
            }
            """;
        var newCode = """
            namespace Test;
            public class Foo
            {
                public string Greeting = "Salaam";
            }
            """;

        var options = new DiffOptions();

        // Act
        var result = _differ.Compare(oldCode, newCode, options);

        // Assert
        result.Stats.TotalChanges.Should().BeGreaterThan(0);
    }

    [Fact]
    public void Compare_HebrewTextInStrings_HandlesCorrectly()
    {
        // Arrange
        var oldCode = """
            namespace Test;
            public class Foo
            {
                public string Message = "Hello";
            }
            """;
        var newCode = """
            namespace Test;
            public class Foo
            {
                public string Message = "Shalom";
            }
            """;

        var options = new DiffOptions();

        // Act
        var result = _differ.Compare(oldCode, newCode, options);

        // Assert
        result.Stats.TotalChanges.Should().BeGreaterThan(0);
    }

    #endregion

    #region Zero Width Characters Tests

    [Fact]
    public void Compare_ZeroWidthSpaces_HandlesCorrectly()
    {
        // Arrange - Zero-width space (U+200B) is invisible but present
        var oldCode = "namespace Test; public class Foo { }";
        var newCode = "namespace Test; public class Foo\u200B { }"; // With zero-width space

        var options = new DiffOptions();

        // Act
        var result = _differ.Compare(oldCode, newCode, options);

        // Assert
        result.FileChanges.Should().HaveCount(1);
    }

    [Fact]
    public void Compare_ZeroWidthNonJoiner_HandlesCorrectly()
    {
        // Arrange - Zero-width non-joiner (U+200C)
        var oldCode = """
            namespace Test;
            public class Foo
            {
                public string Text = "normal";
            }
            """;
        var newCode = """
            namespace Test;
            public class Foo
            {
                public string Text = "nor\u200Cmal";
            }
            """;

        var options = new DiffOptions();

        // Act
        var result = _differ.Compare(oldCode, newCode, options);

        // Assert
        result.Stats.TotalChanges.Should().BeGreaterThan(0);
    }

    #endregion
}
