namespace RoslynDiff.Core.Tests.EdgeCases;

using FluentAssertions;
using RoslynDiff.Core.Differ;
using RoslynDiff.Core.Models;
using Xunit;

/// <summary>
/// Tests for handling whitespace-related edge cases.
/// </summary>
public class WhitespaceTests
{
    private readonly CSharpDiffer _differ = new();

    #region Whitespace-Only Changes Tests

    [Fact]
    public void Compare_WhitespaceOnlyChange_WithIgnoreWhitespace_NoChanges()
    {
        // Arrange
        var oldCode = "namespace Test;public class Foo{}";
        var newCode = "namespace Test; public class Foo { }";

        var options = new DiffOptions { IgnoreWhitespace = true };

        // Act
        var result = _differ.Compare(oldCode, newCode, options);

        // Assert
        result.Stats.TotalChanges.Should().Be(0);
    }

    [Fact]
    public void Compare_WhitespaceOnlyChange_WithoutIgnoreWhitespace_DetectsChanges()
    {
        // Arrange
        var oldCode = "namespace Test;public class Foo{}";
        var newCode = "namespace Test; public class Foo { }";

        var options = new DiffOptions { IgnoreWhitespace = false };

        // Act
        var result = _differ.Compare(oldCode, newCode, options);

        // Assert - Whitespace changes should be detected
        result.FileChanges.Should().HaveCount(1);
    }

    [Fact]
    public void Compare_ExtraBlankLines_WithIgnoreWhitespace_NoChanges()
    {
        // Arrange
        var oldCode = """
            namespace Test;
            public class Foo { }
            """;
        var newCode = """
            namespace Test;


            public class Foo { }
            """;

        var options = new DiffOptions { IgnoreWhitespace = true };

        // Act
        var result = _differ.Compare(oldCode, newCode, options);

        // Assert
        result.Stats.TotalChanges.Should().Be(0);
    }

    [Fact]
    public void Compare_LeadingWhitespaceChange_WithIgnoreWhitespace_NoChanges()
    {
        // Arrange
        var oldCode = "namespace Test; public class Foo { }";
        var newCode = "    namespace Test; public class Foo { }";

        var options = new DiffOptions { IgnoreWhitespace = true };

        // Act
        var result = _differ.Compare(oldCode, newCode, options);

        // Assert
        result.Stats.TotalChanges.Should().Be(0);
    }

    #endregion

    #region Tab vs Space Tests

    [Fact]
    public void Compare_TabsToSpaces_WithIgnoreWhitespace_NoChanges()
    {
        // Arrange
        var oldCode = "namespace Test;\n\tpublic class Foo\n\t{\n\t\tpublic void Method() { }\n\t}";
        var newCode = "namespace Test;\n    public class Foo\n    {\n        public void Method() { }\n    }";

        var options = new DiffOptions { IgnoreWhitespace = true };

        // Act
        var result = _differ.Compare(oldCode, newCode, options);

        // Assert
        result.Stats.TotalChanges.Should().Be(0);
    }

    [Fact]
    public void Compare_SpacesToTabs_WithIgnoreWhitespace_NoChanges()
    {
        // Arrange
        var oldCode = "namespace Test;\n    public class Foo { }";
        var newCode = "namespace Test;\n\tpublic class Foo { }";

        var options = new DiffOptions { IgnoreWhitespace = true };

        // Act
        var result = _differ.Compare(oldCode, newCode, options);

        // Assert
        result.Stats.TotalChanges.Should().Be(0);
    }

    [Fact]
    public void Compare_MixedTabsAndSpaces_WithIgnoreWhitespace_NoChanges()
    {
        // Arrange
        var oldCode = "namespace Test;\n  \tpublic class Foo { }";
        var newCode = "namespace Test;\n\t  public class Foo { }";

        var options = new DiffOptions { IgnoreWhitespace = true };

        // Act
        var result = _differ.Compare(oldCode, newCode, options);

        // Assert
        result.Stats.TotalChanges.Should().Be(0);
    }

    #endregion

    #region Line Ending Tests

    [Fact]
    public void Compare_LfToCrlf_WithIgnoreWhitespace_NoChanges()
    {
        // Arrange
        var oldCode = "namespace Test;\npublic class Foo\n{\n}";
        var newCode = "namespace Test;\r\npublic class Foo\r\n{\r\n}";

        var options = new DiffOptions { IgnoreWhitespace = true };

        // Act
        var result = _differ.Compare(oldCode, newCode, options);

        // Assert
        result.Stats.TotalChanges.Should().Be(0);
    }

    [Fact]
    public void Compare_CrlfToLf_WithIgnoreWhitespace_NoChanges()
    {
        // Arrange
        var oldCode = "namespace Test;\r\npublic class Foo { }";
        var newCode = "namespace Test;\npublic class Foo { }";

        var options = new DiffOptions { IgnoreWhitespace = true };

        // Act
        var result = _differ.Compare(oldCode, newCode, options);

        // Assert
        result.Stats.TotalChanges.Should().Be(0);
    }

    [Fact]
    public void Compare_MixedLineEndings_WithIgnoreWhitespace_NoChanges()
    {
        // Arrange
        var oldCode = "namespace Test;\npublic class Foo\r\n{\n}";
        var newCode = "namespace Test;\r\npublic class Foo\n{\r\n}";

        var options = new DiffOptions { IgnoreWhitespace = true };

        // Act
        var result = _differ.Compare(oldCode, newCode, options);

        // Assert
        result.Stats.TotalChanges.Should().Be(0);
    }

    [Fact]
    public void Compare_CrOnlyLineEndings_HandlesCorrectly()
    {
        // Arrange - Classic Mac line endings (CR only)
        var oldCode = "namespace Test;\rpublic class Foo { }";
        var newCode = "namespace Test;\rpublic class Bar { }";

        var options = new DiffOptions();

        // Act
        var result = _differ.Compare(oldCode, newCode, options);

        // Assert
        result.FileChanges.Should().HaveCount(1);
    }

    #endregion

    #region Trailing Whitespace Tests

    [Fact]
    public void Compare_TrailingWhitespace_WithIgnoreWhitespace_NoChanges()
    {
        // Arrange
        var oldCode = "namespace Test;   \npublic class Foo { }   ";
        var newCode = "namespace Test;\npublic class Foo { }";

        var options = new DiffOptions { IgnoreWhitespace = true };

        // Act
        var result = _differ.Compare(oldCode, newCode, options);

        // Assert
        result.Stats.TotalChanges.Should().Be(0);
    }

    [Fact]
    public void Compare_TrailingTabs_WithIgnoreWhitespace_NoChanges()
    {
        // Arrange
        var oldCode = "namespace Test;\t\t\npublic class Foo { }\t";
        var newCode = "namespace Test;\npublic class Foo { }";

        var options = new DiffOptions { IgnoreWhitespace = true };

        // Act
        var result = _differ.Compare(oldCode, newCode, options);

        // Assert
        result.Stats.TotalChanges.Should().Be(0);
    }

    [Fact]
    public void Compare_TrailingNewlines_WithIgnoreWhitespace_NoChanges()
    {
        // Arrange
        var oldCode = "namespace Test;\npublic class Foo { }\n\n\n";
        var newCode = "namespace Test;\npublic class Foo { }";

        var options = new DiffOptions { IgnoreWhitespace = true };

        // Act
        var result = _differ.Compare(oldCode, newCode, options);

        // Assert
        result.Stats.TotalChanges.Should().Be(0);
    }

    #endregion

    #region Indentation Tests

    [Fact]
    public void Compare_IndentationChange_WithIgnoreWhitespace_NoChanges()
    {
        // Arrange
        var oldCode = """
            namespace Test;
            public class Foo
            {
            public void Method() { }
            }
            """;
        var newCode = """
            namespace Test;
            public class Foo
            {
                public void Method() { }
            }
            """;

        var options = new DiffOptions { IgnoreWhitespace = true };

        // Act
        var result = _differ.Compare(oldCode, newCode, options);

        // Assert
        result.Stats.TotalChanges.Should().Be(0);
    }

    [Fact]
    public void Compare_DeeplyNestedIndentation_HandlesCorrectly()
    {
        // Arrange
        var oldCode = """
            namespace Test;
            public class Foo
            {
                public void Method()
                {
                    if (true)
                    {
                        if (true)
                        {
                            if (true)
                            {
                                if (true)
                                {
                                    DoSomething();
                                }
                            }
                        }
                    }
                }
                private void DoSomething() { }
            }
            """;
        var newCode = """
            namespace Test;
            public class Foo
            {
                public void Method()
                {
                    if (true)
                    {
                        if (true)
                        {
                            if (true)
                            {
                                if (true)
                                {
                                    DoSomethingElse();
                                }
                            }
                        }
                    }
                }
                private void DoSomethingElse() { }
            }
            """;

        var options = new DiffOptions();

        // Act
        var result = _differ.Compare(oldCode, newCode, options);

        // Assert
        result.Stats.TotalChanges.Should().BeGreaterThan(0);
    }

    #endregion

    #region Empty and Whitespace-Only Files Tests

    [Fact]
    public void Compare_EmptyFiles_NoChanges()
    {
        // Arrange
        var oldCode = "";
        var newCode = "";

        var options = new DiffOptions();

        // Act
        var result = _differ.Compare(oldCode, newCode, options);

        // Assert
        result.Stats.TotalChanges.Should().Be(0);
    }

    [Fact]
    public void Compare_EmptyToWhitespaceOnly_WithIgnoreWhitespace_NoChanges()
    {
        // Arrange
        var oldCode = "";
        var newCode = "   \n\t\n   ";

        var options = new DiffOptions { IgnoreWhitespace = true };

        // Act
        var result = _differ.Compare(oldCode, newCode, options);

        // Assert
        result.Stats.TotalChanges.Should().Be(0);
    }

    [Fact]
    public void Compare_WhitespaceOnlyFiles_WithIgnoreWhitespace_NoChanges()
    {
        // Arrange
        var oldCode = "   \n\n\t\t  ";
        var newCode = "\t\t\t\n\n   ";

        var options = new DiffOptions { IgnoreWhitespace = true };

        // Act
        var result = _differ.Compare(oldCode, newCode, options);

        // Assert
        result.Stats.TotalChanges.Should().Be(0);
    }

    [Fact]
    public void Compare_EmptyToContent_DetectsAddition()
    {
        // Arrange
        var oldCode = "";
        var newCode = "namespace Test; public class Foo { }";

        var options = new DiffOptions();

        // Act
        var result = _differ.Compare(oldCode, newCode, options);

        // Assert
        result.Stats.Additions.Should().BeGreaterThan(0);
    }

    [Fact]
    public void Compare_ContentToEmpty_DetectsDeletion()
    {
        // Arrange
        var oldCode = "namespace Test; public class Foo { }";
        var newCode = "";

        var options = new DiffOptions();

        // Act
        var result = _differ.Compare(oldCode, newCode, options);

        // Assert
        result.Stats.Deletions.Should().BeGreaterThan(0);
    }

    #endregion

    #region Whitespace Inside Strings Tests

    [Fact]
    public void Compare_WhitespaceInStrings_DetectsChanges()
    {
        // Arrange - Whitespace inside strings should always be detected
        var oldCode = """
            namespace Test;
            public class Foo
            {
                public string Value = "Hello World";
            }
            """;
        var newCode = """
            namespace Test;
            public class Foo
            {
                public string Value = "Hello  World";
            }
            """;

        var options = new DiffOptions { IgnoreWhitespace = true };

        // Act
        var result = _differ.Compare(oldCode, newCode, options);

        // Assert - String content changes should be detected even with IgnoreWhitespace
        result.Stats.TotalChanges.Should().BeGreaterThan(0);
    }

    [Fact]
    public void Compare_TabsInStrings_DetectsChanges()
    {
        // Arrange
        var oldCode = """
            namespace Test;
            public class Foo
            {
                public string Value = "Hello\tWorld";
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

    #endregion

    #region Long Lines Tests

    [Fact]
    public void Compare_VeryLongLine_HandlesCorrectly()
    {
        // Arrange - Line with >1000 characters
        var longString = new string('x', 1000);
        var oldCode = $"namespace Test; public class Foo {{ public string Value = \"{longString}\"; }}";
        var newCode = $"namespace Test; public class Foo {{ public string Value = \"{longString}y\"; }}";

        var options = new DiffOptions();

        // Act
        var result = _differ.Compare(oldCode, newCode, options);

        // Assert
        result.Stats.TotalChanges.Should().BeGreaterThan(0);
    }

    [Fact]
    public void Compare_ExtremelyLongLine_HandlesCorrectly()
    {
        // Arrange - Line with >10000 characters
        var longString = new string('a', 10000);
        var oldCode = $"namespace Test; public class Foo {{ public string Value = \"{longString}\"; }}";
        var newCode = $"namespace Test; public class Foo {{ public string Value = \"{longString}b\"; }}";

        var options = new DiffOptions();

        // Act
        var result = _differ.Compare(oldCode, newCode, options);

        // Assert
        result.Stats.TotalChanges.Should().BeGreaterThan(0);
    }

    #endregion

    #region Semantic Whitespace Tests

    [Fact]
    public void Compare_WhitespaceChangeInSameStatement_HandlesCorrectly()
    {
        // Arrange - Whitespace change that doesn't affect semantics
        var oldCode = "namespace Test; public class Foo { public int X=1+2; }";
        var newCode = "namespace Test; public class Foo { public int X = 1 + 2; }";

        var options = new DiffOptions { IgnoreWhitespace = true };

        // Act
        var result = _differ.Compare(oldCode, newCode, options);

        // Assert
        result.Stats.TotalChanges.Should().Be(0);
    }

    [Fact]
    public void Compare_WhitespaceAffectingTokenization_HandlesCorrectly()
    {
        // Arrange - Whitespace that could affect how code is parsed
        var oldCode = "namespace Test; public class Foo { public int X = 1 + + 2; }"; // ++2 would be different
        var newCode = "namespace Test; public class Foo { public int X = 1 + +2; }";

        var options = new DiffOptions();

        // Act
        var result = _differ.Compare(oldCode, newCode, options);

        // Assert
        result.FileChanges.Should().HaveCount(1);
    }

    #endregion
}
