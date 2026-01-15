namespace RoslynDiff.Core.Tests.EdgeCases;

using FluentAssertions;
using RoslynDiff.Core.Differ;
using RoslynDiff.Core.Models;
using Xunit;

/// <summary>
/// Tests for handling files with syntax errors and partial parse scenarios.
/// </summary>
public class SyntaxErrorTests
{
    private readonly CSharpDiffer _differ = new();

    #region Syntax Error Recovery Tests

    [Fact]
    public void Compare_MissingSemicolon_HandlesGracefully()
    {
        // Arrange
        var oldCode = "namespace Test; public class Foo { }";
        var newCode = "namespace Test public class Foo { }"; // Missing semicolon

        var options = new DiffOptions();

        // Act
        var result = _differ.Compare(oldCode, newCode, options);

        // Assert - should handle syntax errors without throwing
        result.FileChanges.Should().HaveCount(1);
    }

    [Fact]
    public void Compare_MissingClosingBrace_HandlesGracefully()
    {
        // Arrange
        var oldCode = "namespace Test; public class Foo { }";
        var newCode = "namespace Test; public class Foo { public void Method() { }"; // Missing closing brace

        var options = new DiffOptions();

        // Act
        var result = _differ.Compare(oldCode, newCode, options);

        // Assert
        result.FileChanges.Should().HaveCount(1);
    }

    [Fact]
    public void Compare_UnterminatedString_HandlesGracefully()
    {
        // Arrange
        var oldCode = """
            namespace Test;
            public class Foo
            {
                public string Value = "complete";
            }
            """;
        var newCode = """
            namespace Test;
            public class Foo
            {
                public string Value = "incomplete
            }
            """;

        var options = new DiffOptions();

        // Act
        var result = _differ.Compare(oldCode, newCode, options);

        // Assert
        result.FileChanges.Should().HaveCount(1);
    }

    [Fact]
    public void Compare_BothFilesHaveSyntaxErrors_HandlesGracefully()
    {
        // Arrange
        var oldCode = "namespace Test public class Foo { }"; // Missing semicolon
        var newCode = "namespace Test public class Bar { }"; // Missing semicolon

        var options = new DiffOptions();

        // Act
        var result = _differ.Compare(oldCode, newCode, options);

        // Assert
        result.FileChanges.Should().HaveCount(1);
    }

    [Fact]
    public void Compare_InvalidKeyword_HandlesGracefully()
    {
        // Arrange
        var oldCode = "namespace Test; public class Foo { }";
        var newCode = "namespace Test; publick class Foo { }"; // Typo in keyword

        var options = new DiffOptions();

        // Act
        var result = _differ.Compare(oldCode, newCode, options);

        // Assert
        result.FileChanges.Should().HaveCount(1);
    }

    #endregion

    #region Mixed Valid/Invalid Code Tests

    [Fact]
    public void Compare_MixedValidInvalidCode_DetectsValidParts()
    {
        // Arrange
        var oldCode = """
            namespace Test;
            public class ValidClass { }
            public class AnotherValid { }
            """;
        var newCode = """
            namespace Test;
            public class ValidClass { }
            invalid syntax here
            public class AnotherValid { }
            """;

        var options = new DiffOptions();

        // Act
        var result = _differ.Compare(oldCode, newCode, options);

        // Assert
        result.FileChanges.Should().HaveCount(1);
    }

    [Fact]
    public void Compare_PartialClassDeclaration_HandlesGracefully()
    {
        // Arrange
        var oldCode = "namespace Test; public class Foo { public void Method() { } }";
        var newCode = "namespace Test; public partial class"; // Incomplete declaration

        var options = new DiffOptions();

        // Act
        var result = _differ.Compare(oldCode, newCode, options);

        // Assert
        result.FileChanges.Should().HaveCount(1);
    }

    [Fact]
    public void Compare_IncompleteGenericType_HandlesGracefully()
    {
        // Arrange
        var oldCode = "namespace Test; public class Foo<T> { }";
        var newCode = "namespace Test; public class Foo< { }"; // Incomplete generic

        var options = new DiffOptions();

        // Act
        var result = _differ.Compare(oldCode, newCode, options);

        // Assert
        result.FileChanges.Should().HaveCount(1);
    }

    #endregion

    #region Recovery from Parse Errors Tests

    [Fact]
    public void Compare_RecoverFromSyntaxError_ContinuesProcessing()
    {
        // Arrange
        var oldCode = """
            namespace Test;
            public class Foo
            {
                public void Method1() { }
                public void Method2() { }
            }
            """;
        var newCode = """
            namespace Test;
            public class Foo
            {
                public void Method1() { }
                public void invalid syntax
                public void Method2() { }
            }
            """;

        var options = new DiffOptions();

        // Act
        var result = _differ.Compare(oldCode, newCode, options);

        // Assert - Should still process what it can
        result.FileChanges.Should().HaveCount(1);
    }

    [Fact]
    public void Compare_MissingReturnType_HandlesGracefully()
    {
        // Arrange
        var oldCode = "namespace Test; public class Foo { public void Method() { } }";
        var newCode = "namespace Test; public class Foo { public Method() { } }"; // Missing return type

        var options = new DiffOptions();

        // Act
        var result = _differ.Compare(oldCode, newCode, options);

        // Assert
        result.FileChanges.Should().HaveCount(1);
    }

    [Fact]
    public void Compare_DuplicateModifiers_HandlesGracefully()
    {
        // Arrange
        var oldCode = "namespace Test; public class Foo { }";
        var newCode = "namespace Test; public public class Foo { }"; // Duplicate modifier

        var options = new DiffOptions();

        // Act
        var result = _differ.Compare(oldCode, newCode, options);

        // Assert
        result.FileChanges.Should().HaveCount(1);
    }

    #endregion

    #region Extreme Syntax Errors Tests

    [Fact]
    public void Compare_CompletelyInvalidSyntax_HandlesGracefully()
    {
        // Arrange
        var oldCode = "namespace Test; public class Foo { }";
        var newCode = "this is not valid C# code at all!@#$%^&*()";

        var options = new DiffOptions();

        // Act
        var result = _differ.Compare(oldCode, newCode, options);

        // Assert
        result.FileChanges.Should().HaveCount(1);
    }

    [Fact]
    public void Compare_RandomCharacters_HandlesGracefully()
    {
        // Arrange
        var oldCode = "namespace Test; public class Foo { }";
        var newCode = "@#$%^&*()!~`<>?/\\|";

        var options = new DiffOptions();

        // Act
        var result = _differ.Compare(oldCode, newCode, options);

        // Assert
        result.FileChanges.Should().HaveCount(1);
    }

    [Fact]
    public void Compare_NestedUnbalancedBraces_HandlesGracefully()
    {
        // Arrange
        var oldCode = "namespace Test; public class Foo { { { { } } } }";
        var newCode = "namespace Test; public class Foo { { { { } }"; // Unbalanced

        var options = new DiffOptions();

        // Act
        var result = _differ.Compare(oldCode, newCode, options);

        // Assert
        result.FileChanges.Should().HaveCount(1);
    }

    #endregion

    #region Preprocessor and Conditional Syntax Error Tests

    [Fact]
    public void Compare_UnterminatedPreprocessorDirective_HandlesGracefully()
    {
        // Arrange
        var oldCode = """
            namespace Test;
            #if DEBUG
            public class Foo { }
            #endif
            """;
        var newCode = """
            namespace Test;
            #if DEBUG
            public class Foo { }
            """; // Missing #endif

        var options = new DiffOptions();

        // Act
        var result = _differ.Compare(oldCode, newCode, options);

        // Assert
        result.FileChanges.Should().HaveCount(1);
    }

    [Fact]
    public void Compare_MalformedPreprocessorExpression_HandlesGracefully()
    {
        // Arrange
        var oldCode = """
            namespace Test;
            #if DEBUG
            public class Foo { }
            #endif
            """;
        var newCode = """
            namespace Test;
            #if DEBUG &&
            public class Foo { }
            #endif
            """; // Incomplete expression

        var options = new DiffOptions();

        // Act
        var result = _differ.Compare(oldCode, newCode, options);

        // Assert
        result.FileChanges.Should().HaveCount(1);
    }

    #endregion

    #region Attribute Syntax Error Tests

    [Fact]
    public void Compare_IncompleteAttribute_HandlesGracefully()
    {
        // Arrange
        var oldCode = """
            namespace Test;
            [Serializable]
            public class Foo { }
            """;
        var newCode = """
            namespace Test;
            [Serializable
            public class Foo { }
            """; // Missing closing bracket

        var options = new DiffOptions();

        // Act
        var result = _differ.Compare(oldCode, newCode, options);

        // Assert
        result.FileChanges.Should().HaveCount(1);
    }

    [Fact]
    public void Compare_MalformedAttributeArguments_HandlesGracefully()
    {
        // Arrange
        var oldCode = """
            namespace Test;
            [Obsolete("message")]
            public class Foo { }
            """;
        var newCode = """
            namespace Test;
            [Obsolete("message", ]
            public class Foo { }
            """; // Incomplete arguments

        var options = new DiffOptions();

        // Act
        var result = _differ.Compare(oldCode, newCode, options);

        // Assert
        result.FileChanges.Should().HaveCount(1);
    }

    #endregion
}
