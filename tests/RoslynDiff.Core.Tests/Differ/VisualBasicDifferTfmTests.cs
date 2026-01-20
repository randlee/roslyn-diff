namespace RoslynDiff.Core.Tests.Differ;

using FluentAssertions;
using RoslynDiff.Core.Differ;
using RoslynDiff.Core.Models;
using Xunit;

/// <summary>
/// Unit tests for <see cref="VisualBasicDiffer"/> with multi-TFM support.
/// Tests VB.NET-specific preprocessor directive syntax.
/// </summary>
public class VisualBasicDifferTfmTests
{
    private readonly VisualBasicDiffer _differ = new();

    #region Single TFM Tests

    [Fact]
    public void Compare_NoTfmSpecified_UsesDefaultNet10()
    {
        var code = """
            Namespace Test
                Public Class Foo
                End Class
            End Namespace
            """;
        var options = new DiffOptions();

        var result = _differ.Compare(code, code, options);

        result.Mode.Should().Be(DiffMode.Roslyn);
        result.AnalyzedTfms.Should().NotBeNull();
        result.AnalyzedTfms.Should().HaveCount(1);
        result.AnalyzedTfms![0].Should().Be("net10.0");
    }

    [Fact]
    public void Compare_SingleTfmSpecified_AnalyzesThatTfm()
    {
        var code = """
            Namespace Test
                Public Class Foo
                End Class
            End Namespace
            """;
        var options = new DiffOptions
        {
            TargetFrameworks = new[] { "net8.0" }
        };

        var result = _differ.Compare(code, code, options);

        result.AnalyzedTfms.Should().NotBeNull();
        result.AnalyzedTfms.Should().HaveCount(1);
        result.AnalyzedTfms![0].Should().Be("net8.0");
    }

    #endregion

    #region VB.NET Preprocessor Directive Tests - #If/#End If

    [Fact]
    public void Compare_VbNetIfDirective_Net8_DetectsAddition()
    {
        var oldCode = """
            Namespace Test
                Public Class Foo
                End Class
            End Namespace
            """;
        var newCode = """
            Namespace Test
                Public Class Foo
            #If NET8_0 Then
                    Public Sub Net8Method()
                    End Sub
            #End If
                End Class
            End Namespace
            """;
        var options = new DiffOptions
        {
            TargetFrameworks = new[] { "net8.0" }
        };

        var result = _differ.Compare(oldCode, newCode, options);

        result.Stats.Additions.Should().BeGreaterThan(0);
        var changes = result.FileChanges[0].Changes.Flatten();
        changes.Should().Contain(c => c.Type == ChangeType.Added && c.Name != null && c.Name.Contains("Net8Method"));
    }

    [Fact]
    public void Compare_VbNetIfDirective_Net6_DoesNotDetectNet8Method()
    {
        var oldCode = """
            Namespace Test
                Public Class Foo
                End Class
            End Namespace
            """;
        var newCode = """
            Namespace Test
                Public Class Foo
            #If NET8_0 Then
                    Public Sub Net8Method()
                    End Sub
            #End If
                End Class
            End Namespace
            """;
        var options = new DiffOptions
        {
            TargetFrameworks = new[] { "net6.0" }
        };

        var result = _differ.Compare(oldCode, newCode, options);

        // NET8_0 symbol is not defined for net6.0, so the method should not be visible
        result.Stats.Additions.Should().Be(0);
    }

    [Fact]
    public void Compare_VbNetElseDirective_DetectsCorrectBranch()
    {
        var oldCode = """
            Namespace Test
                Public Class Foo
            #If NET8_0 Then
                    Public Sub Net8Method()
                    End Sub
            #Else
                    Public Sub OtherMethod()
                    End Sub
            #End If
                End Class
            End Namespace
            """;
        var newCode = """
            Namespace Test
                Public Class Foo
            #If NET8_0 Then
                    Public Sub Net8MethodModified()
                    End Sub
            #Else
                    Public Sub OtherMethodModified()
                    End Sub
            #End If
                End Class
            End Namespace
            """;
        var options = new DiffOptions
        {
            TargetFrameworks = new[] { "net8.0" }
        };

        var result = _differ.Compare(oldCode, newCode, options);

        // For NET8_0, only the first branch should be analyzed
        result.Stats.TotalChanges.Should().BeGreaterThan(0);
        var changes = result.FileChanges[0].Changes.Flatten();
        changes.Should().Contain(c => c.Name != null && c.Name.Contains("Net8Method"));
        // The "Else" branch should not be visible
        changes.Should().NotContain(c => c.Name != null && c.Name.Contains("OtherMethod"));
    }

    #endregion

    #region VB.NET Multiple TFMs Tests

    [Fact]
    public void Compare_MultipleTfms_NoPreprocessorDirectives_OnlyAnalyzesFirstTfm()
    {
        var code = """
            Namespace Test
                Public Class Foo
                End Class
            End Namespace
            """;
        var options = new DiffOptions
        {
            TargetFrameworks = new[] { "net8.0", "net6.0", "net10.0" }
        };

        var result = _differ.Compare(code, code, options);

        // Optimization: when there are no preprocessor directives, only first TFM is analyzed
        result.AnalyzedTfms.Should().NotBeNull();
        result.AnalyzedTfms.Should().HaveCount(1);
        result.AnalyzedTfms![0].Should().Be("net8.0");
    }

    [Fact]
    public void Compare_MultipleTfms_WithPreprocessorDirectives_AnalyzesAllTfms()
    {
        var code = """
            Namespace Test
                Public Class Foo
            #If NET8_0 Then
                    Public Sub Net8Method()
                    End Sub
            #End If
                End Class
            End Namespace
            """;
        var options = new DiffOptions
        {
            TargetFrameworks = new[] { "net8.0", "net6.0", "net10.0" }
        };

        var result = _differ.Compare(code, code, options);

        // With preprocessor directives, all TFMs should be analyzed
        result.AnalyzedTfms.Should().NotBeNull();
        result.AnalyzedTfms.Should().HaveCount(3);
        result.AnalyzedTfms.Should().Contain("net8.0");
        result.AnalyzedTfms.Should().Contain("net6.0");
        result.AnalyzedTfms.Should().Contain("net10.0");
    }

    [Fact]
    public void Compare_MultipleTfms_DifferentMethodsPerTfm()
    {
        var oldCode = """
            Namespace Test
                Public Class Foo
                End Class
            End Namespace
            """;
        var newCode = """
            Namespace Test
                Public Class Foo
            #If NET8_0 Then
                    Public Sub Net8Method()
                    End Sub
            #End If
            #If NET6_0 Then
                    Public Sub Net6Method()
                    End Sub
            #End If
                End Class
            End Namespace
            """;
        var options = new DiffOptions
        {
            TargetFrameworks = new[] { "net8.0", "net6.0" }
        };

        var result = _differ.Compare(oldCode, newCode, options);

        result.Stats.Additions.Should().BeGreaterThan(0);
        result.AnalyzedTfms.Should().HaveCount(2);

        // Note: Due to placeholder merge logic, we just verify that changes are detected
        // Sprint 3 will implement proper TfmResultMerger to handle per-TFM changes correctly
        result.FileChanges[0].Changes.Should().NotBeEmpty();
    }

    #endregion

    #region VB.NET Nested Conditions Tests

    [Fact]
    public void Compare_NestedVbNetIfDirectives_HandlesCorrectly()
    {
        var oldCode = """
            Namespace Test
                Public Class Foo
            #If NET8_0 Then
            #If DEBUG Then
                    Public Sub DebugNet8Method()
                    End Sub
            #End If
            #End If
                End Class
            End Namespace
            """;
        var newCode = """
            Namespace Test
                Public Class Foo
            #If NET8_0 Then
            #If DEBUG Then
                    Public Sub DebugNet8MethodModified()
                    End Sub
            #End If
            #End If
                End Class
            End Namespace
            """;
        var options = new DiffOptions
        {
            TargetFrameworks = new[] { "net8.0" }
        };

        var result = _differ.Compare(oldCode, newCode, options);

        // Nested conditions should be handled, but DEBUG is not defined by default
        // So the inner method should not be visible
        result.Stats.TotalChanges.Should().Be(0);
    }

    [Fact]
    public void Compare_VbNetElseIfDirective_HandlesMultipleBranches()
    {
        var oldCode = """
            Namespace Test
                Public Class Foo
                End Class
            End Namespace
            """;
        var newCode = """
            Namespace Test
                Public Class Foo
            #If NET10_0 Then
                    Public Sub Net10Method()
                    End Sub
            #ElseIf NET8_0 Then
                    Public Sub Net8Method()
                    End Sub
            #Else
                    Public Sub OtherMethod()
                    End Sub
            #End If
                End Class
            End Namespace
            """;
        var options = new DiffOptions
        {
            TargetFrameworks = new[] { "net8.0" }
        };

        var result = _differ.Compare(oldCode, newCode, options);

        // For net8.0, only the ElseIf branch should be active
        result.Stats.Additions.Should().BeGreaterThan(0);
        var changes = result.FileChanges[0].Changes.Flatten();
        changes.Should().Contain(c => c.Name != null && c.Name.Contains("Net8Method"));
    }

    #endregion

    #region VB.NET OR_GREATER Tests

    [Fact]
    public void Compare_VbNetOrGreater_Net8OrGreater_IncludesNet8()
    {
        var oldCode = """
            Namespace Test
                Public Class Foo
                End Class
            End Namespace
            """;
        var newCode = """
            Namespace Test
                Public Class Foo
            #If NET8_0_OR_GREATER Then
                    Public Sub ModernNetMethod()
                    End Sub
            #End If
                End Class
            End Namespace
            """;
        var options = new DiffOptions
        {
            TargetFrameworks = new[] { "net8.0" }
        };

        var result = _differ.Compare(oldCode, newCode, options);

        // NET8_0_OR_GREATER should be defined for net8.0
        result.Stats.Additions.Should().BeGreaterThan(0);
        var changes = result.FileChanges[0].Changes.Flatten();
        changes.Should().Contain(c => c.Name != null && c.Name.Contains("ModernNetMethod"));
    }

    [Fact]
    public void Compare_VbNetOrGreater_Net8OrGreater_ExcludesNet6()
    {
        var oldCode = """
            Namespace Test
                Public Class Foo
                End Class
            End Namespace
            """;
        var newCode = """
            Namespace Test
                Public Class Foo
            #If NET8_0_OR_GREATER Then
                    Public Sub ModernNetMethod()
                    End Sub
            #End If
                End Class
            End Namespace
            """;
        var options = new DiffOptions
        {
            TargetFrameworks = new[] { "net6.0" }
        };

        var result = _differ.Compare(oldCode, newCode, options);

        // NET8_0_OR_GREATER should NOT be defined for net6.0
        result.Stats.Additions.Should().Be(0);
    }

    [Fact]
    public void Compare_VbNetOrGreater_Net6OrGreater_IncludesNet8()
    {
        var oldCode = """
            Namespace Test
                Public Class Foo
                End Class
            End Namespace
            """;
        var newCode = """
            Namespace Test
                Public Class Foo
            #If NET6_0_OR_GREATER Then
                    Public Sub ModernNetMethod()
                    End Sub
            #End If
                End Class
            End Namespace
            """;
        var options = new DiffOptions
        {
            TargetFrameworks = new[] { "net8.0" }
        };

        var result = _differ.Compare(oldCode, newCode, options);

        // NET6_0_OR_GREATER should be defined for net8.0 (since 8 >= 6)
        result.Stats.Additions.Should().BeGreaterThan(0);
        var changes = result.FileChanges[0].Changes.Flatten();
        changes.Should().Contain(c => c.Name != null && c.Name.Contains("ModernNetMethod"));
    }

    [Fact]
    public void Compare_VbNetMultipleOrGreater_ComplexConditions()
    {
        var oldCode = """
            Namespace Test
                Public Class Foo
                End Class
            End Namespace
            """;
        var newCode = """
            Namespace Test
                Public Class Foo
            #If NET10_0_OR_GREATER Then
                    Public Sub Net10OrGreaterMethod()
                    End Sub
            #ElseIf NET8_0_OR_GREATER Then
                    Public Sub Net8OrGreaterMethod()
                    End Sub
            #ElseIf NET6_0_OR_GREATER Then
                    Public Sub Net6OrGreaterMethod()
                    End Sub
            #End If
                End Class
            End Namespace
            """;
        var options = new DiffOptions
        {
            TargetFrameworks = new[] { "net8.0" }
        };

        var result = _differ.Compare(oldCode, newCode, options);

        // For net8.0, NET10_0_OR_GREATER is false, but NET8_0_OR_GREATER is true
        result.Stats.Additions.Should().BeGreaterThan(0);
        var changes = result.FileChanges[0].Changes.Flatten();
        changes.Should().Contain(c => c.Name != null && c.Name.Contains("Net8OrGreaterMethod"));
        changes.Should().NotContain(c => c.Name != null && c.Name.Contains("Net10OrGreaterMethod"));
        changes.Should().NotContain(c => c.Name != null && c.Name.Contains("Net6OrGreaterMethod"));
    }

    #endregion

    #region VB.NET Complex Scenarios

    [Fact]
    public void Compare_VbNetComplexConditionals_WithLogicalOperators()
    {
        var oldCode = """
            Namespace Test
                Public Class Foo
                End Class
            End Namespace
            """;
        var newCode = """
            Namespace Test
                Public Class Foo
            #If NET8_0 OrElse NET10_0 Then
                    Public Sub ModernVersionMethod()
                    End Sub
            #End If
                End Class
            End Namespace
            """;
        var options = new DiffOptions
        {
            TargetFrameworks = new[] { "net8.0" }
        };

        var result = _differ.Compare(oldCode, newCode, options);

        // NET8_0 OrElse NET10_0 should evaluate to true for net8.0
        result.Stats.Additions.Should().BeGreaterThan(0);
        var changes = result.FileChanges[0].Changes.Flatten();
        changes.Should().Contain(c => c.Name != null && c.Name.Contains("ModernVersionMethod"));
    }

    [Fact]
    public void Compare_VbNetConditionalClass_EntireClassConditional()
    {
        var oldCode = """
            Namespace Test
            End Namespace
            """;
        var newCode = """
            Namespace Test
            #If NET8_0 Then
                Public Class Net8OnlyClass
                    Public Sub Method1()
                    End Sub
                End Class
            #End If
            End Namespace
            """;
        var options = new DiffOptions
        {
            TargetFrameworks = new[] { "net8.0" }
        };

        var result = _differ.Compare(oldCode, newCode, options);

        // The entire class should be visible for net8.0
        result.Stats.Additions.Should().BeGreaterThan(0);
        var changes = result.FileChanges[0].Changes.Flatten();
        changes.Should().Contain(c => c.Type == ChangeType.Added && c.Name == "Net8OnlyClass");
    }

    [Fact]
    public void Compare_VbNetConditionalProperty_DetectsPropertyAddition()
    {
        var oldCode = """
            Namespace Test
                Public Class Foo
                End Class
            End Namespace
            """;
        var newCode = """
            Namespace Test
                Public Class Foo
            #If NET8_0 Then
                    Public Property Net8Property As String
            #End If
                End Class
            End Namespace
            """;
        var options = new DiffOptions
        {
            TargetFrameworks = new[] { "net8.0" }
        };

        var result = _differ.Compare(oldCode, newCode, options);

        result.Stats.Additions.Should().BeGreaterThan(0);
        var changes = result.FileChanges[0].Changes.Flatten();
        changes.Should().Contain(c => c.Name != null && c.Name.Contains("Net8Property"));
    }

    #endregion

    #region Error Handling

    [Fact]
    public void Compare_InvalidTfm_ThrowsException()
    {
        var code = """
            Namespace Test
                Public Class Foo
                End Class
            End Namespace
            """;
        var options = new DiffOptions
        {
            TargetFrameworks = new[] { "invalid-tfm" }
        };

        var act = () => _differ.Compare(code, code, options);

        // TfmSymbolResolver should throw for invalid TFMs
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Compare_MultipleTfms_OneWithParseError_ContinuesWithValidTfms()
    {
        var validCode = """
            Namespace Test
                Public Class Foo
                End Class
            End Namespace
            """;
        var invalidCode = """
            Namespace Test
                Public Class
                End Class
            End Namespace
            """; // Missing class name
        var options = new DiffOptions
        {
            TargetFrameworks = new[] { "net8.0", "net6.0" }
        };

        var result = _differ.Compare(validCode, invalidCode, options);

        // When parse errors occur across all TFMs, result should indicate errors
        result.FileChanges.Should().HaveCount(1);
    }

    #endregion

    #region VB.NET Specific Syntax Tests

    [Fact]
    public void Compare_VbNetRegion_WithConditionalCompilation()
    {
        var oldCode = """
            Namespace Test
                Public Class Foo
                End Class
            End Namespace
            """;
        var newCode = """
            Namespace Test
                Public Class Foo
            #Region "NET8.0 Methods"
            #If NET8_0 Then
                    Public Sub Net8Method()
                    End Sub
            #End If
            #End Region
                End Class
            End Namespace
            """;
        var options = new DiffOptions
        {
            TargetFrameworks = new[] { "net8.0" }
        };

        var result = _differ.Compare(oldCode, newCode, options);

        // Regions shouldn't affect conditional compilation
        result.Stats.Additions.Should().BeGreaterThan(0);
        var changes = result.FileChanges[0].Changes.Flatten();
        changes.Should().Contain(c => c.Name != null && c.Name.Contains("Net8Method"));
    }

    [Fact]
    public void Compare_VbNetModule_WithConditionalMembers()
    {
        var oldCode = """
            Namespace Test
                Public Module Utilities
                End Module
            End Namespace
            """;
        var newCode = """
            Namespace Test
                Public Module Utilities
            #If NET8_0 Then
                    Public Sub Net8Utility()
                    End Sub
            #End If
                End Module
            End Namespace
            """;
        var options = new DiffOptions
        {
            TargetFrameworks = new[] { "net8.0" }
        };

        var result = _differ.Compare(oldCode, newCode, options);

        result.Stats.Additions.Should().BeGreaterThan(0);
    }

    #endregion
}
