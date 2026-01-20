namespace RoslynDiff.Core.Tests.Tfm;

using FluentAssertions;
using RoslynDiff.Core.Tfm;
using Xunit;

/// <summary>
/// Unit tests for <see cref="PreprocessorDirectiveDetector"/>.
/// </summary>
public class PreprocessorDirectiveDetectorTests
{
    #region Basic Directive Detection Tests

    [Fact]
    public void HasPreprocessorDirectives_If_ReturnsTrue()
    {
        var content = """
            #if DEBUG
            Console.WriteLine("Debug");
            #endif
            """;

        var result = PreprocessorDirectiveDetector.HasPreprocessorDirectives(content);

        result.Should().BeTrue();
    }

    [Fact]
    public void HasPreprocessorDirectives_Elif_ReturnsTrue()
    {
        var content = """
            #if NET6_0
            // .NET 6.0 code
            #elif NET7_0
            // .NET 7.0 code
            #endif
            """;

        var result = PreprocessorDirectiveDetector.HasPreprocessorDirectives(content);

        result.Should().BeTrue();
    }

    [Fact]
    public void HasPreprocessorDirectives_Else_ReturnsTrue()
    {
        var content = """
            #if DEBUG
            Console.WriteLine("Debug");
            #else
            Console.WriteLine("Release");
            #endif
            """;

        var result = PreprocessorDirectiveDetector.HasPreprocessorDirectives(content);

        result.Should().BeTrue();
    }

    [Fact]
    public void HasPreprocessorDirectives_Endif_ReturnsTrue()
    {
        var content = """
            #if DEBUG
            // Debug code
            #endif
            """;

        var result = PreprocessorDirectiveDetector.HasPreprocessorDirectives(content);

        result.Should().BeTrue();
    }

    [Fact]
    public void HasPreprocessorDirectives_Define_ReturnsTrue()
    {
        var content = """
            #define DEBUG
            namespace Test;
            """;

        var result = PreprocessorDirectiveDetector.HasPreprocessorDirectives(content);

        result.Should().BeTrue();
    }

    [Fact]
    public void HasPreprocessorDirectives_Undef_ReturnsTrue()
    {
        var content = """
            #undef TRACE
            namespace Test;
            """;

        var result = PreprocessorDirectiveDetector.HasPreprocessorDirectives(content);

        result.Should().BeTrue();
    }

    #endregion

    #region No Directive Tests

    [Fact]
    public void HasPreprocessorDirectives_NoDirectives_ReturnsFalse()
    {
        var content = """
            namespace Test;

            public class Calculator
            {
                public int Add(int a, int b) => a + b;
            }
            """;

        var result = PreprocessorDirectiveDetector.HasPreprocessorDirectives(content);

        result.Should().BeFalse();
    }

    [Fact]
    public void HasPreprocessorDirectives_EmptyString_ReturnsFalse()
    {
        var content = "";

        var result = PreprocessorDirectiveDetector.HasPreprocessorDirectives(content);

        result.Should().BeFalse();
    }

    [Fact]
    public void HasPreprocessorDirectives_WhitespaceOnly_ReturnsFalse()
    {
        var content = "   \t\n  \r\n  ";

        var result = PreprocessorDirectiveDetector.HasPreprocessorDirectives(content);

        result.Should().BeFalse();
    }

    [Fact]
    public void HasPreprocessorDirectives_NoHashSymbol_ReturnsFalse()
    {
        var content = """
            namespace Test;
            public class Foo
            {
                private string name = "test";
            }
            """;

        var result = PreprocessorDirectiveDetector.HasPreprocessorDirectives(content);

        result.Should().BeFalse();
    }

    #endregion

    #region Edge Cases - Comments

    [Theory]
    [InlineData("// #if DEBUG\nnamespace Test;")]
    [InlineData("/* #if DEBUG */\nnamespace Test;")]
    [InlineData("/// <summary>#if</summary>\nclass Foo { }")]
    public void HasPreprocessorDirectives_DirectiveInComment_ReturnsTrueConservatively(string content)
    {
        // Conservative detection: we accept false positives for directives in comments
        // This maintains simplicity and performance
        var result = PreprocessorDirectiveDetector.HasPreprocessorDirectives(content);

        result.Should().BeTrue("conservative detection allows directives in comments");
    }

    #endregion

    #region Edge Cases - Strings

    [Theory]
    [InlineData("var x = \"#if DEBUG\";\nnamespace Test;")]
    [InlineData("var y = @\"#define TEST\";\nclass Foo { }")]
    [InlineData("const string code = \"\"\"#endif\"\"\";")]
    public void HasPreprocessorDirectives_DirectiveInString_ReturnsTrueConservatively(string content)
    {
        // Conservative detection: we accept false positives for directives in strings
        // This maintains simplicity and performance
        var result = PreprocessorDirectiveDetector.HasPreprocessorDirectives(content);

        result.Should().BeTrue("conservative detection allows directives in strings");
    }

    #endregion

    #region Edge Cases - Partial Matches

    [Fact]
    public void HasPreprocessorDirectives_Ifdef_ReturnsTrue()
    {
        // #ifdef is not a valid C# directive (it's C/C++), but #endif is valid C#
        // Conservative detection returns true because there's a valid #endif directive
        var content = """
            #ifdef DEBUG
            // This is not C#
            #endif
            """;

        var result = PreprocessorDirectiveDetector.HasPreprocessorDirectives(content);

        result.Should().BeTrue("#endif is a valid C# directive");
    }

    [Fact]
    public void HasPreprocessorDirectives_Ifndef_ReturnsTrue()
    {
        // #ifndef is not a valid C# directive (it's C/C++), but #endif is valid C#
        // Conservative detection returns true because there's a valid #endif directive
        var content = """
            #ifndef RELEASE
            // This is not C#
            #endif
            """;

        var result = PreprocessorDirectiveDetector.HasPreprocessorDirectives(content);

        result.Should().BeTrue("#endif is a valid C# directive");
    }

    [Fact]
    public void HasPreprocessorDirectives_IfdefAsPartOfIdentifier_ReturnsFalse()
    {
        var content = """
            namespace Test;
            // Some comment about #ifdefined being different
            public class Foo { }
            """;

        var result = PreprocessorDirectiveDetector.HasPreprocessorDirectives(content);

        result.Should().BeFalse("partial matches should not be detected");
    }

    #endregion

    #region Edge Cases - Whitespace and Line Position

    [Fact]
    public void HasPreprocessorDirectives_DirectiveWithLeadingWhitespace_ReturnsTrue()
    {
        var content = """
            namespace Test;

                #if DEBUG
                Console.WriteLine("Debug");
                #endif
            """;

        var result = PreprocessorDirectiveDetector.HasPreprocessorDirectives(content);

        result.Should().BeTrue("directives can have leading whitespace");
    }

    [Fact]
    public void HasPreprocessorDirectives_DirectiveWithTabIndentation_ReturnsTrue()
    {
        var content = "\t#if DEBUG\n\tConsole.WriteLine(\"Debug\");\n\t#endif";

        var result = PreprocessorDirectiveDetector.HasPreprocessorDirectives(content);

        result.Should().BeTrue("directives can be indented with tabs");
    }

    [Fact]
    public void HasPreprocessorDirectives_HashInMiddleOfString_ReturnsTrue()
    {
        // Conservative detection: even #if in the middle of a string value is detected
        // This is acceptable as a false positive to maintain simplicity
        var content = """
            namespace Test;
            public class Foo
            {
                private string hash = "value#if";
            }
            """;

        var result = PreprocessorDirectiveDetector.HasPreprocessorDirectives(content);

        result.Should().BeTrue("conservative detection allows false positives");
    }

    [Fact]
    public void HasPreprocessorDirectives_DirectiveWithSpaceAfterHash_ReturnsTrue()
    {
        var content = """
            #  if DEBUG
            Console.WriteLine("Debug");
            #  endif
            """;

        var result = PreprocessorDirectiveDetector.HasPreprocessorDirectives(content);

        result.Should().BeTrue("whitespace between # and directive keyword is allowed");
    }

    #endregion

    #region Multiple Directives

    [Fact]
    public void HasPreprocessorDirectives_MultipleDirectiveTypes_ReturnsTrue()
    {
        var content = """
            #define CUSTOM_SYMBOL
            #undef OLD_SYMBOL

            namespace Test;

            #if NET6_0_OR_GREATER
                public class ModernFeature { }
            #elif NET5_0
                public class LegacyFeature { }
            #else
                public class FallbackFeature { }
            #endif
            """;

        var result = PreprocessorDirectiveDetector.HasPreprocessorDirectives(content);

        result.Should().BeTrue();
    }

    #endregion

    #region Complex Conditions

    [Fact]
    public void HasPreprocessorDirectives_ComplexCondition_ReturnsTrue()
    {
        var content = """
            #if (DEBUG && NET6_0) || TRACE
            namespace Test;
            public class DebugHelper { }
            #endif
            """;

        var result = PreprocessorDirectiveDetector.HasPreprocessorDirectives(content);

        result.Should().BeTrue("complex conditions should be detected");
    }

    #endregion

    #region Null Input Tests

    [Fact]
    public void HasPreprocessorDirectives_NullContent_ThrowsArgumentNullException()
    {
        var act = () => PreprocessorDirectiveDetector.HasPreprocessorDirectives(null!);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("content");
    }

    #endregion

    #region Real-World Examples

    [Fact]
    public void HasPreprocessorDirectives_RealWorldMultiTargeting_ReturnsTrue()
    {
        var content = """
            namespace MyLibrary;

            public class PlatformHelper
            {
            #if NET6_0_OR_GREATER
                public static string Framework => "Modern .NET";
            #elif NETSTANDARD2_0
                public static string Framework => ".NET Standard 2.0";
            #else
                public static string Framework => "Legacy .NET";
            #endif

                public void DoWork()
                {
                    Console.WriteLine(Framework);
                }
            }
            """;

        var result = PreprocessorDirectiveDetector.HasPreprocessorDirectives(content);

        result.Should().BeTrue();
    }

    [Fact]
    public void HasPreprocessorDirectives_RealWorldNoDirectives_ReturnsFalse()
    {
        var content = """
            namespace MyLibrary;

            /// <summary>
            /// A simple calculator class.
            /// </summary>
            public class Calculator
            {
                /// <summary>
                /// Adds two numbers.
                /// </summary>
                public int Add(int a, int b)
                {
                    return a + b;
                }

                /// <summary>
                /// Multiplies two numbers.
                /// </summary>
                public int Multiply(int a, int b)
                {
                    return a * b;
                }
            }
            """;

        var result = PreprocessorDirectiveDetector.HasPreprocessorDirectives(content);

        result.Should().BeFalse();
    }

    #endregion

    #region Performance Characteristics

    [Fact]
    public void HasPreprocessorDirectives_LargeFileWithoutHash_FastPath()
    {
        // Generate a large file with no # symbols to test fast path
        var lines = new List<string>();
        for (var i = 0; i < 10000; i++)
        {
            lines.Add($"public class Class{i} {{ }}");
        }
        var content = string.Join("\n", lines);

        var result = PreprocessorDirectiveDetector.HasPreprocessorDirectives(content);

        result.Should().BeFalse("fast path should handle large files efficiently");
    }

    [Fact]
    public void HasPreprocessorDirectives_DirectiveAtVeryEnd_ReturnsTrue()
    {
        var lines = new List<string>();
        for (var i = 0; i < 1000; i++)
        {
            lines.Add($"public class Class{i} {{ }}");
        }
        lines.Add("#if DEBUG");
        lines.Add("#endif");
        var content = string.Join("\n", lines);

        var result = PreprocessorDirectiveDetector.HasPreprocessorDirectives(content);

        result.Should().BeTrue("should detect directives even at end of large file");
    }

    #endregion

    #region Edge Cases - Line Endings

    [Theory]
    [InlineData("#if DEBUG\ncode\n#endif")]           // Unix (LF)
    [InlineData("#if DEBUG\r\ncode\r\n#endif")]       // Windows (CRLF)
    [InlineData("#if DEBUG\rcode\r#endif")]           // Old Mac (CR)
    public void HasPreprocessorDirectives_VariousLineEndings_ReturnsTrue(string content)
    {
        var result = PreprocessorDirectiveDetector.HasPreprocessorDirectives(content);

        result.Should().BeTrue("should handle all line ending styles");
    }

    #endregion

    #region Edge Cases - Boundary Conditions

    [Fact]
    public void HasPreprocessorDirectives_OnlyDirective_ReturnsTrue()
    {
        var content = "#if";

        var result = PreprocessorDirectiveDetector.HasPreprocessorDirectives(content);

        result.Should().BeTrue("minimal valid directive");
    }

    [Fact]
    public void HasPreprocessorDirectives_DirectiveAtStartOfFile_ReturnsTrue()
    {
        var content = "#define DEBUG\nnamespace Test;";

        var result = PreprocessorDirectiveDetector.HasPreprocessorDirectives(content);

        result.Should().BeTrue("directive at start of file");
    }

    [Fact]
    public void HasPreprocessorDirectives_DirectiveAtEndOfFile_ReturnsTrue()
    {
        var content = "namespace Test;\n#endif";

        var result = PreprocessorDirectiveDetector.HasPreprocessorDirectives(content);

        result.Should().BeTrue("directive at end of file");
    }

    [Fact]
    public void HasPreprocessorDirectives_OnlyHashSymbol_ReturnsFalse()
    {
        var content = "#";

        var result = PreprocessorDirectiveDetector.HasPreprocessorDirectives(content);

        result.Should().BeFalse("lone # is not a directive");
    }

    [Fact]
    public void HasPreprocessorDirectives_HashWithInvalidDirective_ReturnsFalse()
    {
        var content = "#invalid";

        var result = PreprocessorDirectiveDetector.HasPreprocessorDirectives(content);

        result.Should().BeFalse("invalid directive names should not match");
    }

    #endregion
}
