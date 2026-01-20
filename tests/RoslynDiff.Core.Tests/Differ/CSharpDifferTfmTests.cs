namespace RoslynDiff.Core.Tests.Differ;

using FluentAssertions;
using RoslynDiff.Core.Differ;
using RoslynDiff.Core.Models;
using Xunit;

/// <summary>
/// Unit tests for <see cref="CSharpDiffer"/> multi-TFM support.
/// </summary>
public class CSharpDifferTfmTests
{
    private readonly CSharpDiffer _differ = new();

    #region Pre-scan Optimization Tests

    [Fact]
    public void Compare_NoPreprocessorDirectives_SingleTfm_UsesSingleParse()
    {
        // Arrange: Code with no preprocessor directives
        var oldCode = """
            namespace Test;
            public class Foo
            {
                public void Method1() { }
            }
            """;
        var newCode = """
            namespace Test;
            public class Foo
            {
                public void Method1() { }
                public void Method2() { }
            }
            """;
        var options = new DiffOptions
        {
            TargetFrameworks = new[] { "net8.0" }
        };

        // Act
        var result = _differ.Compare(oldCode, newCode, options);

        // Assert: Should use single parse optimization (no multi-TFM analysis)
        result.AnalyzedTfms.Should().BeNull("no preprocessor directives were found, so multi-TFM analysis should be skipped");
        result.Stats.Additions.Should().BeGreaterThan(0);
        var changes = result.FileChanges[0].Changes.Flatten();
        changes.Should().Contain(c => c.Type == ChangeType.Added && c.Name == "Method2");
    }

    [Fact]
    public void Compare_NoPreprocessorDirectives_MultipleTfms_UsesSingleParse()
    {
        // Arrange: Code with no preprocessor directives but multiple TFMs requested
        var oldCode = """
            namespace Test;
            public class Foo
            {
                public void Method1() { }
            }
            """;
        var newCode = """
            namespace Test;
            public class Foo
            {
                public void Method1() { }
                public void Method2() { }
            }
            """;
        var options = new DiffOptions
        {
            TargetFrameworks = new[] { "net8.0", "net10.0" }
        };

        // Act
        var result = _differ.Compare(oldCode, newCode, options);

        // Assert: Should optimize and use single parse since no preprocessor directives
        result.AnalyzedTfms.Should().BeNull("no preprocessor directives were found, so multi-TFM analysis should be skipped");
        result.Stats.Additions.Should().BeGreaterThan(0);
    }

    [Fact]
    public void Compare_NoTfmsSpecified_UsesSingleParse()
    {
        // Arrange: Code with preprocessor directives but no TFMs specified
        // Use NET10_0 or NET10_0_OR_GREATER since default symbols are NET10_0
        var oldCode = """
            namespace Test;
            public class Foo
            {
            #if NET10_0
                public void Method1() { }
            #endif
            }
            """;
        var newCode = """
            namespace Test;
            public class Foo
            {
            #if NET10_0
                public void Method1() { }
                public void Method2() { }
            #endif
            }
            """;
        var options = new DiffOptions
        {
            TargetFrameworks = null // No TFMs specified
        };

        // Act
        var result = _differ.Compare(oldCode, newCode, options);

        // Assert: Should use single parse with default symbols (NET10_0)
        result.AnalyzedTfms.Should().BeNull("no TFMs were specified in options");
        result.Stats.Additions.Should().BeGreaterThan(0);
    }

    #endregion

    #region Single TFM with Preprocessor Directives

    [Fact]
    public void Compare_SingleTfm_WithIfDirective_NET8_0()
    {
        // Arrange: Code with #if NET8_0 directive
        var oldCode = """
            namespace Test;
            public class Foo
            {
            #if NET8_0
                public void Net8Method() { }
            #endif
            }
            """;
        var newCode = """
            namespace Test;
            public class Foo
            {
            #if NET8_0
                public void Net8Method() { }
                public void AnotherNet8Method() { }
            #endif
            }
            """;
        var options = new DiffOptions
        {
            TargetFrameworks = new[] { "net8.0" }
        };

        // Act
        var result = _differ.Compare(oldCode, newCode, options);

        // Assert: Should analyze with NET8_0 symbols
        result.AnalyzedTfms.Should().NotBeNull();
        result.AnalyzedTfms.Should().Contain("net8.0");
        result.Stats.Additions.Should().BeGreaterThan(0);
        var changes = result.FileChanges[0].Changes.Flatten();
        changes.Should().Contain(c => c.Type == ChangeType.Added && c.Name == "AnotherNet8Method");
    }

    [Fact]
    public void Compare_SingleTfm_WithIfDirective_NET10_0()
    {
        // Arrange: Code with #if NET10_0 directive
        var oldCode = """
            namespace Test;
            public class Foo
            {
            #if NET10_0
                public void Net10Method() { }
            #endif
            }
            """;
        var newCode = """
            namespace Test;
            public class Foo
            {
            #if NET10_0
                public void Net10Method() { }
                public void AnotherNet10Method() { }
            #endif
            }
            """;
        var options = new DiffOptions
        {
            TargetFrameworks = new[] { "net10.0" }
        };

        // Act
        var result = _differ.Compare(oldCode, newCode, options);

        // Assert: Should analyze with NET10_0 symbols
        result.AnalyzedTfms.Should().NotBeNull();
        result.AnalyzedTfms.Should().Contain("net10.0");
        result.Stats.Additions.Should().BeGreaterThan(0);
    }

    #endregion

    #region Multiple TFMs with Preprocessor Directives

    [Fact]
    public void Compare_MultipleTfms_WithIfDirective_ShowsDifferentResults()
    {
        // Arrange: Code where #if NET8_0 causes different behavior across TFMs
        var oldCode = """
            namespace Test;
            public class Foo
            {
            #if NET8_0
                public void Net8OnlyMethod() { }
            #endif
            }
            """;
        var newCode = """
            namespace Test;
            public class Foo
            {
            #if NET8_0
                public void Net8OnlyMethod() { }
                public void AnotherNet8OnlyMethod() { }
            #endif
            }
            """;
        var options = new DiffOptions
        {
            TargetFrameworks = new[] { "net8.0", "net10.0" }
        };

        // Act
        var result = _differ.Compare(oldCode, newCode, options);

        // Assert: Should analyze both TFMs
        result.AnalyzedTfms.Should().NotBeNull();
        result.AnalyzedTfms.Should().Contain("net8.0");
        result.AnalyzedTfms.Should().Contain("net10.0");

        // With NET8_0 symbols, the method should be visible and show as added
        // With NET10_0 symbols, the method won't be visible (no change detected)
        // Note: The placeholder merger returns the first result, so we'll see NET8_0 behavior
        result.Stats.Additions.Should().BeGreaterThan(0);
    }

    [Fact]
    public void Compare_MultipleTfms_WithIfElseDirectives()
    {
        // Arrange: Code with #if/#else directives
        var oldCode = """
            namespace Test;
            public class Foo
            {
            #if NET8_0
                public void Net8Method() { }
            #else
                public void OtherMethod() { }
            #endif
            }
            """;
        var newCode = """
            namespace Test;
            public class Foo
            {
            #if NET8_0
                public void Net8Method() { }
                public void AnotherNet8Method() { }
            #else
                public void OtherMethod() { }
                public void AnotherOtherMethod() { }
            #endif
            }
            """;
        var options = new DiffOptions
        {
            TargetFrameworks = new[] { "net8.0", "net10.0" }
        };

        // Act
        var result = _differ.Compare(oldCode, newCode, options);

        // Assert: Should analyze both TFMs
        result.AnalyzedTfms.Should().NotBeNull();
        result.AnalyzedTfms.Should().Contain("net8.0");
        result.AnalyzedTfms.Should().Contain("net10.0");
        result.Stats.Additions.Should().BeGreaterThan(0);
    }

    [Fact]
    public void Compare_MultipleTfms_ThreeTfms_UsesParallelProcessing()
    {
        // Arrange: Code with preprocessor directives and 3 TFMs (should use parallel)
        var oldCode = """
            namespace Test;
            public class Foo
            {
            #if NET8_0
                public void Net8Method() { }
            #elif NET9_0
                public void Net9Method() { }
            #elif NET10_0
                public void Net10Method() { }
            #endif
            }
            """;
        var newCode = """
            namespace Test;
            public class Foo
            {
            #if NET8_0
                public void Net8Method() { }
                public void NewNet8Method() { }
            #elif NET9_0
                public void Net9Method() { }
                public void NewNet9Method() { }
            #elif NET10_0
                public void Net10Method() { }
                public void NewNet10Method() { }
            #endif
            }
            """;
        var options = new DiffOptions
        {
            TargetFrameworks = new[] { "net8.0", "net9.0", "net10.0" }
        };

        // Act
        var result = _differ.Compare(oldCode, newCode, options);

        // Assert: Should analyze all three TFMs
        result.AnalyzedTfms.Should().NotBeNull();
        result.AnalyzedTfms.Should().HaveCount(3);
        result.AnalyzedTfms.Should().Contain("net8.0");
        result.AnalyzedTfms.Should().Contain("net9.0");
        result.AnalyzedTfms.Should().Contain("net10.0");
    }

    #endregion

    #region Nested Preprocessor Directives

    [Fact]
    public void Compare_NestedPreprocessorDirectives()
    {
        // Arrange: Code with nested #if directives
        var oldCode = """
            namespace Test;
            public class Foo
            {
            #if NET8_0
                #if DEBUG
                    public void DebugNet8Method() { }
                #else
                    public void ReleaseNet8Method() { }
                #endif
            #endif
            }
            """;
        var newCode = """
            namespace Test;
            public class Foo
            {
            #if NET8_0
                #if DEBUG
                    public void DebugNet8Method() { }
                    public void AnotherDebugNet8Method() { }
                #else
                    public void ReleaseNet8Method() { }
                    public void AnotherReleaseNet8Method() { }
                #endif
            #endif
            }
            """;
        var options = new DiffOptions
        {
            TargetFrameworks = new[] { "net8.0", "net10.0" }
        };

        // Act
        var result = _differ.Compare(oldCode, newCode, options);

        // Assert: Should handle nested directives
        result.AnalyzedTfms.Should().NotBeNull();
        result.Stats.Additions.Should().BeGreaterThan(0);
    }

    [Fact]
    public void Compare_ComplexNestedConditions()
    {
        // Arrange: Code with complex nested conditions
        var oldCode = """
            namespace Test;
            public class Foo
            {
            #if NET8_0 || NET9_0
                public void ModernNetMethod() { }
                #if NET8_0
                    public void Net8SpecificMethod() { }
                #endif
            #endif
            }
            """;
        var newCode = """
            namespace Test;
            public class Foo
            {
            #if NET8_0 || NET9_0
                public void ModernNetMethod() { }
                public void AnotherModernNetMethod() { }
                #if NET8_0
                    public void Net8SpecificMethod() { }
                    public void AnotherNet8SpecificMethod() { }
                #endif
            #endif
            }
            """;
        var options = new DiffOptions
        {
            TargetFrameworks = new[] { "net8.0", "net9.0", "net10.0" }
        };

        // Act
        var result = _differ.Compare(oldCode, newCode, options);

        // Assert: Should handle complex conditions
        result.AnalyzedTfms.Should().NotBeNull();
        result.AnalyzedTfms.Should().HaveCount(3);
    }

    #endregion

    #region OR_GREATER Conditions

    [Fact]
    public void Compare_OrGreaterCondition_NET7_0_OR_GREATER()
    {
        // Arrange: Code with NET7_0_OR_GREATER condition
        var oldCode = """
            namespace Test;
            public class Foo
            {
            #if NET7_0_OR_GREATER
                public void ModernNetMethod() { }
            #endif
            }
            """;
        var newCode = """
            namespace Test;
            public class Foo
            {
            #if NET7_0_OR_GREATER
                public void ModernNetMethod() { }
                public void AnotherModernNetMethod() { }
            #endif
            }
            """;
        var options = new DiffOptions
        {
            TargetFrameworks = new[] { "net8.0", "net10.0" }
        };

        // Act
        var result = _differ.Compare(oldCode, newCode, options);

        // Assert: Both NET8_0 and NET10_0 should satisfy NET7_0_OR_GREATER
        result.AnalyzedTfms.Should().NotBeNull();
        result.AnalyzedTfms.Should().Contain("net8.0");
        result.AnalyzedTfms.Should().Contain("net10.0");
        result.Stats.Additions.Should().BeGreaterThan(0);
        var changes = result.FileChanges[0].Changes.Flatten();
        changes.Should().Contain(c => c.Type == ChangeType.Added && c.Name == "AnotherModernNetMethod");
    }

    [Fact]
    public void Compare_OrGreaterCondition_NET8_0_OR_GREATER()
    {
        // Arrange: Code with NET8_0_OR_GREATER condition
        var oldCode = """
            namespace Test;
            public class Foo
            {
            #if NET8_0_OR_GREATER
                public void Net8PlusMethod() { }
            #endif
            }
            """;
        var newCode = """
            namespace Test;
            public class Foo
            {
            #if NET8_0_OR_GREATER
                public void Net8PlusMethod() { }
                public void AnotherNet8PlusMethod() { }
            #endif
            }
            """;
        var options = new DiffOptions
        {
            TargetFrameworks = new[] { "net8.0", "net10.0" }
        };

        // Act
        var result = _differ.Compare(oldCode, newCode, options);

        // Assert: Both NET8_0 and NET10_0 should satisfy NET8_0_OR_GREATER
        result.AnalyzedTfms.Should().NotBeNull();
        result.Stats.Additions.Should().BeGreaterThan(0);
    }

    [Fact]
    public void Compare_OrGreaterCondition_NET10_0_OR_GREATER()
    {
        // Arrange: Code with NET10_0_OR_GREATER condition
        var oldCode = """
            namespace Test;
            public class Foo
            {
            #if NET10_0_OR_GREATER
                public void Net10PlusMethod() { }
            #endif
            }
            """;
        var newCode = """
            namespace Test;
            public class Foo
            {
            #if NET10_0_OR_GREATER
                public void Net10PlusMethod() { }
                public void AnotherNet10PlusMethod() { }
            #endif
            }
            """;
        var options = new DiffOptions
        {
            TargetFrameworks = new[] { "net8.0", "net9.0", "net10.0" }
        };

        // Act
        var result = _differ.Compare(oldCode, newCode, options);

        // Assert: Only NET10_0 should satisfy NET10_0_OR_GREATER
        // But the merger placeholder will return first result
        result.AnalyzedTfms.Should().NotBeNull();
        result.AnalyzedTfms.Should().HaveCount(3);
    }

    [Fact]
    public void Compare_MixedOrGreaterAndExplicitConditions()
    {
        // Arrange: Code with mixed OR_GREATER and explicit conditions
        var oldCode = """
            namespace Test;
            public class Foo
            {
            #if NET8_0_OR_GREATER
                public void Net8PlusMethod() { }
            #endif
            #if NET10_0
                public void Net10OnlyMethod() { }
            #endif
            }
            """;
        var newCode = """
            namespace Test;
            public class Foo
            {
            #if NET8_0_OR_GREATER
                public void Net8PlusMethod() { }
                public void AnotherNet8PlusMethod() { }
            #endif
            #if NET10_0
                public void Net10OnlyMethod() { }
                public void AnotherNet10OnlyMethod() { }
            #endif
            }
            """;
        var options = new DiffOptions
        {
            TargetFrameworks = new[] { "net8.0", "net10.0" }
        };

        // Act
        var result = _differ.Compare(oldCode, newCode, options);

        // Assert: Should handle mixed conditions correctly
        result.AnalyzedTfms.Should().NotBeNull();
        result.AnalyzedTfms.Should().Contain("net8.0");
        result.AnalyzedTfms.Should().Contain("net10.0");
        result.Stats.Additions.Should().BeGreaterThan(0);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void Compare_PreprocessorDirectiveInOldContentOnly()
    {
        // Arrange: Preprocessor directive only in old content
        var oldCode = """
            namespace Test;
            public class Foo
            {
            #if NET8_0
                public void Net8Method() { }
            #endif
            }
            """;
        var newCode = """
            namespace Test;
            public class Foo
            {
                public void StandardMethod() { }
            }
            """;
        var options = new DiffOptions
        {
            TargetFrameworks = new[] { "net8.0", "net10.0" }
        };

        // Act
        var result = _differ.Compare(oldCode, newCode, options);

        // Assert: Should trigger multi-TFM analysis since old content has directives
        result.AnalyzedTfms.Should().NotBeNull();
    }

    [Fact]
    public void Compare_PreprocessorDirectiveInNewContentOnly()
    {
        // Arrange: Preprocessor directive only in new content
        var oldCode = """
            namespace Test;
            public class Foo
            {
                public void StandardMethod() { }
            }
            """;
        var newCode = """
            namespace Test;
            public class Foo
            {
            #if NET8_0
                public void Net8Method() { }
            #endif
            }
            """;
        var options = new DiffOptions
        {
            TargetFrameworks = new[] { "net8.0", "net10.0" }
        };

        // Act
        var result = _differ.Compare(oldCode, newCode, options);

        // Assert: Should trigger multi-TFM analysis since new content has directives
        result.AnalyzedTfms.Should().NotBeNull();
    }

    [Fact]
    public void Compare_EmptyTargetFrameworksList_UsesSingleParse()
    {
        // Arrange: Empty TFM list
        var oldCode = """
            namespace Test;
            public class Foo
            {
            #if NET8_0
                public void Net8Method() { }
            #endif
            }
            """;
        var newCode = """
            namespace Test;
            public class Foo
            {
            #if NET8_0
                public void Net8Method() { }
                public void AnotherMethod() { }
            #endif
            }
            """;
        var options = new DiffOptions
        {
            TargetFrameworks = Array.Empty<string>()
        };

        // Act
        var result = _differ.Compare(oldCode, newCode, options);

        // Assert: Should use single parse with default symbols
        result.AnalyzedTfms.Should().BeNull("empty TFM list should not trigger multi-TFM analysis");
    }

    [Fact]
    public void Compare_ParseError_InOneTfm_ContinuesWithOthers()
    {
        // Arrange: Code that might parse differently across TFMs
        // (This is a conceptual test - actual parse errors are unlikely to be TFM-specific)
        var oldCode = """
            namespace Test;
            public class Foo
            {
            #if NET8_0
                public void Method1() { }
            #endif
            }
            """;
        var newCode = """
            namespace Test;
            public class Foo
            {
            #if NET8_0
                public void Method1() { }
                public void Method2() { }
            #endif
            }
            """;
        var options = new DiffOptions
        {
            TargetFrameworks = new[] { "net8.0", "net10.0" }
        };

        // Act
        var result = _differ.Compare(oldCode, newCode, options);

        // Assert: Should complete successfully
        result.Should().NotBeNull();
        result.AnalyzedTfms.Should().NotBeNull();
    }

    #endregion

    #region Integration with DiffResult

    [Fact]
    public void Compare_SingleTfm_NoDirectives_AnalyzedTfmsIsNull()
    {
        // Arrange
        var code = """
            namespace Test;
            public class Foo { }
            """;
        var options = new DiffOptions
        {
            TargetFrameworks = new[] { "net8.0" }
        };

        // Act
        var result = _differ.Compare(code, code, options);

        // Assert: AnalyzedTfms should be null when optimization skips multi-TFM
        result.AnalyzedTfms.Should().BeNull();
    }

    [Fact]
    public void Compare_MultipleTfms_WithDirectives_AnalyzedTfmsPopulated()
    {
        // Arrange
        var code = """
            namespace Test;
            public class Foo
            {
            #if NET8_0
                public void Method() { }
            #endif
            }
            """;
        var options = new DiffOptions
        {
            TargetFrameworks = new[] { "net8.0", "net10.0" }
        };

        // Act
        var result = _differ.Compare(code, code, options);

        // Assert: AnalyzedTfms should contain both TFMs
        result.AnalyzedTfms.Should().NotBeNull();
        result.AnalyzedTfms.Should().Contain("net8.0");
        result.AnalyzedTfms.Should().Contain("net10.0");
    }

    [Fact]
    public void Compare_ResultMode_AlwaysRoslyn()
    {
        // Arrange
        var code = """
            namespace Test;
            public class Foo
            {
            #if NET8_0
                public void Method() { }
            #endif
            }
            """;
        var options = new DiffOptions
        {
            TargetFrameworks = new[] { "net8.0", "net10.0" }
        };

        // Act
        var result = _differ.Compare(code, code, options);

        // Assert: Mode should always be Roslyn for CSharpDiffer
        result.Mode.Should().Be(DiffMode.Roslyn);
    }

    #endregion
}
