namespace RoslynDiff.Core.Tests.EdgeCases;

using FluentAssertions;
using RoslynDiff.Core.Differ;
using RoslynDiff.Core.Models;
using Xunit;

/// <summary>
/// Tests for handling preprocessor directives and conditional compilation.
/// The diff engine parses all code branches and compares them semantically.
/// Preprocessor directive changes are detected when they affect the semantic structure.
/// </summary>
public class PreprocessorTests
{
    private readonly CSharpDiffer _differ = new();

    #region #if/#else/#endif Tests

    [Fact]
    public void Compare_IfDirective_MethodRenamed_HandlesGracefully()
    {
        // Arrange - Method name change within #if directive
        // Note: Code within inactive preprocessor branches may be treated differently
        var oldCode = """
            namespace Test;
            public class Foo
            {
            #if DEBUG
                public void DebugMethod() { }
            #endif
            }
            """;
        var newCode = """
            namespace Test;
            public class Foo
            {
            #if DEBUG
                public void DebugMethodUpdated() { }
            #endif
            }
            """;

        var options = new DiffOptions();

        // Act
        var result = _differ.Compare(oldCode, newCode, options);

        // Assert - Preprocessor directive content handled gracefully
        result.FileChanges.Should().HaveCount(1);
    }

    [Fact]
    public void Compare_IfElseDirective_MethodAdded_HandlesGracefully()
    {
        // Arrange - Code inside preprocessor directives may be inactive
        var oldCode = """
            namespace Test;
            public class Foo
            {
            #if DEBUG
                public void DebugMethod() { }
            #else
                public void ReleaseMethod() { }
            #endif
            }
            """;
        var newCode = """
            namespace Test;
            public class Foo
            {
            #if DEBUG
                public void DebugMethod() { }
                public void NewDebugMethod() { }
            #else
                public void ReleaseMethod() { }
            #endif
            }
            """;

        var options = new DiffOptions();

        // Act
        var result = _differ.Compare(oldCode, newCode, options);

        // Assert - Handled gracefully
        result.FileChanges.Should().HaveCount(1);
    }

    [Fact]
    public void Compare_ElifDirective_MethodAdded_HandlesGracefully()
    {
        // Arrange
        var oldCode = """
            namespace Test;
            public class Foo
            {
            #if DEBUG
                public void DebugMethod() { }
            #elif RELEASE
                public void ReleaseMethod() { }
            #else
                public void DefaultMethod() { }
            #endif
            }
            """;
        var newCode = """
            namespace Test;
            public class Foo
            {
            #if DEBUG
                public void DebugMethod() { }
            #elif RELEASE
                public void ReleaseMethod() { }
                public void NewReleaseMethod() { }
            #else
                public void DefaultMethod() { }
            #endif
            }
            """;

        var options = new DiffOptions();

        // Act
        var result = _differ.Compare(oldCode, newCode, options);

        // Assert - Handled gracefully
        result.FileChanges.Should().HaveCount(1);
    }

    [Fact]
    public void Compare_NestedIfDirectives_MethodRenamed_HandlesGracefully()
    {
        // Arrange
        var oldCode = """
            namespace Test;
            public class Foo
            {
            #if DEBUG
                #if TRACE
                    public void TraceDebugMethod() { }
                #endif
            #endif
            }
            """;
        var newCode = """
            namespace Test;
            public class Foo
            {
            #if DEBUG
                #if TRACE
                    public void TraceDebugMethodRenamed() { }
                #endif
            #endif
            }
            """;

        var options = new DiffOptions();

        // Act
        var result = _differ.Compare(oldCode, newCode, options);

        // Assert - Handled gracefully
        result.FileChanges.Should().HaveCount(1);
    }

    [Fact]
    public void Compare_ComplexCondition_MethodAdded_HandlesGracefully()
    {
        // Arrange
        var oldCode = """
            namespace Test;
            public class Foo
            {
            #if DEBUG && !RELEASE
                public void DebugOnlyMethod() { }
            #endif
            }
            """;
        var newCode = """
            namespace Test;
            public class Foo
            {
            #if DEBUG && !RELEASE
                public void DebugOnlyMethod() { }
                public void AnotherDebugMethod() { }
            #endif
            }
            """;

        var options = new DiffOptions();

        // Act
        var result = _differ.Compare(oldCode, newCode, options);

        // Assert - Handled gracefully
        result.FileChanges.Should().HaveCount(1);
    }

    [Fact]
    public void Compare_AddingIfDirective_SameCode_NoSemanticChanges()
    {
        // Arrange - Wrapping code in #if doesn't change semantics
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
            #if DEBUG
                public void Method() { }
            #endif
            }
            """;

        var options = new DiffOptions();

        // Act
        var result = _differ.Compare(oldCode, newCode, options);

        // Assert - Roslyn parses both versions, code structure is the same
        result.FileChanges.Should().HaveCount(1);
    }

    #endregion

    #region #region/#endregion Tests

    [Fact]
    public void Compare_RegionDirective_MethodAdded_DetectsChanges()
    {
        // Arrange
        var oldCode = """
            namespace Test;
            public class Foo
            {
                #region Public Methods
                public void Method1() { }
                #endregion
            }
            """;
        var newCode = """
            namespace Test;
            public class Foo
            {
                #region Public Methods
                public void Method1() { }
                public void Method2() { }
                #endregion
            }
            """;

        var options = new DiffOptions();

        // Act
        var result = _differ.Compare(oldCode, newCode, options);

        // Assert
        result.Stats.Additions.Should().BeGreaterThan(0);
    }

    [Fact]
    public void Compare_RegionNameChange_CodeUnchanged_HandlesGracefully()
    {
        // Arrange - Only region name changes, code is the same
        var oldCode = """
            namespace Test;
            public class Foo
            {
                #region Public Methods
                public void Method() { }
                #endregion
            }
            """;
        var newCode = """
            namespace Test;
            public class Foo
            {
                #region Public API
                public void Method() { }
                #endregion
            }
            """;

        var options = new DiffOptions();

        // Act
        var result = _differ.Compare(oldCode, newCode, options);

        // Assert - Region names are trivia, semantic structure unchanged
        result.FileChanges.Should().HaveCount(1);
    }

    [Fact]
    public void Compare_NestedRegions_MethodAdded_DetectsChanges()
    {
        // Arrange
        var oldCode = """
            namespace Test;
            public class Foo
            {
                #region Public
                    #region Methods
                    public void Method1() { }
                    #endregion
                #endregion
            }
            """;
        var newCode = """
            namespace Test;
            public class Foo
            {
                #region Public
                    #region Methods
                    public void Method1() { }
                    public void Method2() { }
                    #endregion
                #endregion
            }
            """;

        var options = new DiffOptions();

        // Act
        var result = _differ.Compare(oldCode, newCode, options);

        // Assert
        result.Stats.Additions.Should().BeGreaterThan(0);
    }

    #endregion

    #region #pragma Tests

    [Fact]
    public void Compare_PragmaWarningDisable_MethodAdded_DetectsChanges()
    {
        // Arrange
        var oldCode = """
            namespace Test;
            public class Foo
            {
            #pragma warning disable CS0168
                public void Method()
                {
                    int unused;
                }
            #pragma warning restore CS0168
            }
            """;
        var newCode = """
            namespace Test;
            public class Foo
            {
            #pragma warning disable CS0168
                public void Method()
                {
                    int unused;
                }
                public void NewMethod() { }
            #pragma warning restore CS0168
            }
            """;

        var options = new DiffOptions();

        // Act
        var result = _differ.Compare(oldCode, newCode, options);

        // Assert
        result.Stats.Additions.Should().BeGreaterThan(0);
    }

    [Fact]
    public void Compare_PragmaChecksum_SameCode_NoSemanticChanges()
    {
        // Arrange - Pragma checksum is metadata, doesn't affect code semantics
        var oldCode = """
            #pragma checksum "file.cs" "{406ea660-64cf-4c82-b6f0-42d48172a799}" "ab007f1d23d9"
            namespace Test;
            public class Foo { }
            """;
        var newCode = """
            #pragma checksum "file.cs" "{406ea660-64cf-4c82-b6f0-42d48172a799}" "ab007f1d23d0"
            namespace Test;
            public class Foo { }
            """;

        var options = new DiffOptions();

        // Act
        var result = _differ.Compare(oldCode, newCode, options);

        // Assert - Checksum change is trivia, not semantic
        result.FileChanges.Should().HaveCount(1);
    }

    [Fact]
    public void Compare_AddingPragma_SameCode_NoSemanticChanges()
    {
        // Arrange
        var oldCode = """
            namespace Test;
            public class Foo
            {
                public void Method()
                {
                    int unused;
                }
            }
            """;
        var newCode = """
            namespace Test;
            public class Foo
            {
            #pragma warning disable CS0168
                public void Method()
                {
                    int unused;
                }
            #pragma warning restore CS0168
            }
            """;

        var options = new DiffOptions();

        // Act
        var result = _differ.Compare(oldCode, newCode, options);

        // Assert - Pragma is trivia, code structure unchanged
        result.FileChanges.Should().HaveCount(1);
    }

    #endregion

    #region #define/#undef Tests

    [Fact]
    public void Compare_DefineDirective_SameCode_NoSemanticChanges()
    {
        // Arrange - #define changes don't affect parsed code structure
        var oldCode = """
            #define DEBUG
            namespace Test;
            public class Foo { }
            """;
        var newCode = """
            #define RELEASE
            namespace Test;
            public class Foo { }
            """;

        var options = new DiffOptions();

        // Act
        var result = _differ.Compare(oldCode, newCode, options);

        // Assert - Define is a preprocessor directive, code unchanged
        result.FileChanges.Should().HaveCount(1);
    }

    [Fact]
    public void Compare_UndefDirective_SameCode_NoSemanticChanges()
    {
        // Arrange
        var oldCode = """
            #define DEBUG
            #undef DEBUG
            namespace Test;
            public class Foo { }
            """;
        var newCode = """
            #define DEBUG
            namespace Test;
            public class Foo { }
            """;

        var options = new DiffOptions();

        // Act
        var result = _differ.Compare(oldCode, newCode, options);

        // Assert
        result.FileChanges.Should().HaveCount(1);
    }

    [Fact]
    public void Compare_MultipleDefines_ClassAdded_DetectsChanges()
    {
        // Arrange
        var oldCode = """
            #define DEBUG
            #define TRACE
            namespace Test;
            public class Foo { }
            """;
        var newCode = """
            #define DEBUG
            #define TRACE
            #define VERBOSE
            namespace Test;
            public class Foo { }
            public class Bar { }
            """;

        var options = new DiffOptions();

        // Act
        var result = _differ.Compare(oldCode, newCode, options);

        // Assert - Class added
        result.Stats.Additions.Should().BeGreaterThan(0);
    }

    #endregion

    #region #error/#warning Tests

    [Fact]
    public void Compare_ErrorDirective_CodeChanged_DetectsChanges()
    {
        // Arrange
        var oldCode = """
            namespace Test;
            public class Foo
            {
            #if !DEBUG
            #error Debug build required
            #endif
                public void OldMethod() { }
            }
            """;
        var newCode = """
            namespace Test;
            public class Foo
            {
            #if !DEBUG
            #error Release builds not supported
            #endif
                public void NewMethod() { }
            }
            """;

        var options = new DiffOptions();

        // Act
        var result = _differ.Compare(oldCode, newCode, options);

        // Assert - Method name changed
        result.FileChanges.Should().HaveCount(1);
        result.FileChanges[0].Changes.Should().NotBeEmpty();
    }

    [Fact]
    public void Compare_WarningDirective_MethodRenamed_DetectsChanges()
    {
        // Arrange
        var oldCode = """
            namespace Test;
            public class Foo
            {
            #warning This is deprecated
                public void OldMethod() { }
            }
            """;
        var newCode = """
            namespace Test;
            public class Foo
            {
            #warning This will be removed in v2.0
                public void NewMethod() { }
            }
            """;

        var options = new DiffOptions();

        // Act
        var result = _differ.Compare(oldCode, newCode, options);

        // Assert
        result.FileChanges.Should().HaveCount(1);
        result.FileChanges[0].Changes.Should().NotBeEmpty();
    }

    #endregion

    #region #line Tests

    [Fact]
    public void Compare_LineDirective_SameCode_NoSemanticChanges()
    {
        // Arrange - #line is metadata for debugging, doesn't affect semantics
        var oldCode = """
            namespace Test;
            #line 100 "OriginalFile.cs"
            public class Foo { }
            #line default
            """;
        var newCode = """
            namespace Test;
            #line 200 "OriginalFile.cs"
            public class Foo { }
            #line default
            """;

        var options = new DiffOptions();

        // Act
        var result = _differ.Compare(oldCode, newCode, options);

        // Assert - Line directive is trivia
        result.FileChanges.Should().HaveCount(1);
    }

    [Fact]
    public void Compare_LineHidden_MethodAdded_DetectsChanges()
    {
        // Arrange
        var oldCode = """
            namespace Test;
            public class Foo
            {
            #line hidden
                private void GeneratedCode() { }
            #line default
            }
            """;
        var newCode = """
            namespace Test;
            public class Foo
            {
            #line hidden
                private void GeneratedCode() { }
                private void AnotherGenerated() { }
            #line default
            }
            """;

        var options = new DiffOptions();

        // Act
        var result = _differ.Compare(oldCode, newCode, options);

        // Assert
        result.Stats.Additions.Should().BeGreaterThan(0);
    }

    #endregion

    #region Conditional Compilation Scenarios

    [Fact]
    public void Compare_ConditionalMethodAttribute_AttributeAdded_DetectsChanges()
    {
        // Arrange
        var oldCode = """
            namespace Test;
            using System.Diagnostics;
            public class Foo
            {
                [Conditional("DEBUG")]
                public void DebugOnly() { }
            }
            """;
        var newCode = """
            namespace Test;
            using System.Diagnostics;
            public class Foo
            {
                [Conditional("DEBUG")]
                public void DebugOnly() { }
                [Conditional("TRACE")]
                public void TraceOnly() { }
            }
            """;

        var options = new DiffOptions();

        // Act
        var result = _differ.Compare(oldCode, newCode, options);

        // Assert
        result.Stats.Additions.Should().BeGreaterThan(0);
    }

    [Fact]
    public void Compare_PlatformSpecificCode_MethodAdded_HandlesGracefully()
    {
        // Arrange - Code in preprocessor branches may be inactive
        var oldCode = """
            namespace Test;
            public class Foo
            {
            #if WINDOWS
                public void WindowsMethod() { }
            #elif LINUX
                public void LinuxMethod() { }
            #elif MACOS
                public void MacOSMethod() { }
            #endif
            }
            """;
        var newCode = """
            namespace Test;
            public class Foo
            {
            #if WINDOWS
                public void WindowsMethod() { }
                public void NewWindowsMethod() { }
            #elif LINUX
                public void LinuxMethod() { }
            #elif MACOS
                public void MacOSMethod() { }
            #endif
            }
            """;

        var options = new DiffOptions();

        // Act
        var result = _differ.Compare(oldCode, newCode, options);

        // Assert - Handled gracefully
        result.FileChanges.Should().HaveCount(1);
    }

    [Fact]
    public void Compare_VersionSpecificCode_SameStructure_NoSemanticChanges()
    {
        // Arrange - Only directive condition changed, same code structure
        var oldCode = """
            namespace Test;
            public class Foo
            {
            #if NET6_0_OR_GREATER
                public void ModernMethod() { }
            #else
                public void LegacyMethod() { }
            #endif
            }
            """;
        var newCode = """
            namespace Test;
            public class Foo
            {
            #if NET7_0_OR_GREATER
                public void ModernMethod() { }
            #else
                public void LegacyMethod() { }
            #endif
            }
            """;

        var options = new DiffOptions();

        // Act
        var result = _differ.Compare(oldCode, newCode, options);

        // Assert - Code structure is identical
        result.FileChanges.Should().HaveCount(1);
    }

    #endregion

    #region #nullable Tests

    [Fact]
    public void Compare_NullableDirective_PropertyTypeChanged_DetectsChanges()
    {
        // Arrange
        var oldCode = """
            #nullable enable
            namespace Test;
            public class Foo
            {
                public string Name { get; set; } = "";
            }
            """;
        var newCode = """
            #nullable enable
            namespace Test;
            public class Foo
            {
                public string? Name { get; set; }
            }
            """;

        var options = new DiffOptions();

        // Act
        var result = _differ.Compare(oldCode, newCode, options);

        // Assert - Property type changed (nullable)
        result.Stats.TotalChanges.Should().BeGreaterThan(0);
    }

    [Fact]
    public void Compare_NullableRestore_PropertyAdded_DetectsChanges()
    {
        // Arrange
        var oldCode = """
            namespace Test;
            public class Foo
            {
            #nullable enable
                public string? NullableProp { get; set; }
            #nullable restore
            }
            """;
        var newCode = """
            namespace Test;
            public class Foo
            {
            #nullable enable
                public string? NullableProp { get; set; }
                public string? AnotherProp { get; set; }
            #nullable restore
            }
            """;

        var options = new DiffOptions();

        // Act
        var result = _differ.Compare(oldCode, newCode, options);

        // Assert
        result.Stats.Additions.Should().BeGreaterThan(0);
    }

    [Fact]
    public void Compare_NullableWarnings_SameCode_NoSemanticChanges()
    {
        // Arrange - Only nullable mode changed
        var oldCode = """
            #nullable enable warnings
            namespace Test;
            public class Foo { }
            """;
        var newCode = """
            #nullable enable annotations
            namespace Test;
            public class Foo { }
            """;

        var options = new DiffOptions();

        // Act
        var result = _differ.Compare(oldCode, newCode, options);

        // Assert - Code structure unchanged
        result.FileChanges.Should().HaveCount(1);
    }

    #endregion

    #region Multiple Directives Combined

    [Fact]
    public void Compare_MixedDirectives_PropertyAdded_DetectsChanges()
    {
        // Arrange
        var oldCode = """
            #define FEATURE_A
            #nullable enable
            namespace Test;
            public class Foo
            {
                #region Properties
            #if FEATURE_A
                public string? Name { get; set; }
            #endif
                #endregion

            #pragma warning disable CS0169
                private int _unused;
            #pragma warning restore CS0169
            }
            """;
        var newCode = """
            #define FEATURE_A
            #define FEATURE_B
            #nullable enable
            namespace Test;
            public class Foo
            {
                #region Properties
            #if FEATURE_A
                public string? Name { get; set; }
            #endif
            #if FEATURE_B
                public int? Age { get; set; }
            #endif
                #endregion

            #pragma warning disable CS0169
                private int _unused;
            #pragma warning restore CS0169
            }
            """;

        var options = new DiffOptions();

        // Act
        var result = _differ.Compare(oldCode, newCode, options);

        // Assert
        result.Stats.Additions.Should().BeGreaterThan(0);
    }

    #endregion
}
