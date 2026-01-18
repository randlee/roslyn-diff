namespace RoslynDiff.Core.Tests;

using FluentAssertions;
using RoslynDiff.Core.Differ;
using RoslynDiff.Core.Models;
using Xunit;

/// <summary>
/// Unit tests for <see cref="VisualBasicDiffer"/>.
/// </summary>
public class VisualBasicDifferTests
{
    private readonly VisualBasicDiffer _differ = new();

    #region CanHandle Tests

    [Fact]
    public void CanHandle_VbFile_ReturnsTrue()
    {
        var options = new DiffOptions();
        var result = _differ.CanHandle("test.vb", options);
        result.Should().BeTrue();
    }

    [Fact]
    public void CanHandle_VbFileUpperCase_ReturnsTrue()
    {
        var options = new DiffOptions();
        var result = _differ.CanHandle("test.VB", options);
        result.Should().BeTrue();
    }

    [Fact]
    public void CanHandle_VbFileWithLineMode_ReturnsFalse()
    {
        var options = new DiffOptions { Mode = DiffMode.Line };
        var result = _differ.CanHandle("test.vb", options);
        result.Should().BeFalse();
    }

    [Fact]
    public void CanHandle_VbFileWithRoslynMode_ReturnsTrue()
    {
        var options = new DiffOptions { Mode = DiffMode.Roslyn };
        var result = _differ.CanHandle("test.vb", options);
        result.Should().BeTrue();
    }

    [Fact]
    public void CanHandle_NonVbFile_ReturnsFalse()
    {
        var options = new DiffOptions();
        var result = _differ.CanHandle("test.txt", options);
        result.Should().BeFalse();
    }

    [Fact]
    public void CanHandle_CsFile_ReturnsFalse()
    {
        var options = new DiffOptions();
        var result = _differ.CanHandle("test.cs", options);
        result.Should().BeFalse();
    }

    [Fact]
    public void CanHandle_NullFilePath_ThrowsArgumentNullException()
    {
        var options = new DiffOptions();
        var act = () => _differ.CanHandle(null!, options);
        act.Should().Throw<ArgumentNullException>().WithParameterName("filePath");
    }

    [Fact]
    public void CanHandle_NullOptions_ThrowsArgumentNullException()
    {
        var act = () => _differ.CanHandle("test.vb", null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("options");
    }

    #endregion

    #region Compare Tests - Basic

    [Fact]
    public void Compare_IdenticalCode_ReturnsNoChanges()
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
        result.Stats.TotalChanges.Should().Be(0);
    }

    [Fact]
    public void Compare_ClassAdded_DetectsAddition()
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
                End Class
                Public Class Bar
                End Class
            End Namespace
            """;
        var options = new DiffOptions();

        var result = _differ.Compare(oldCode, newCode, options);

        result.Stats.Additions.Should().BeGreaterThan(0);
        result.FileChanges.Should().HaveCount(1);
        var changes = result.FileChanges[0].Changes.Flatten();
        changes.Should().Contain(c => c.Type == ChangeType.Added && c.Kind == ChangeKind.Class && c.Name == "Bar");
    }

    [Fact]
    public void Compare_ClassRemoved_DetectsRemoval()
    {
        var oldCode = """
            Namespace Test
                Public Class Foo
                End Class
                Public Class Bar
                End Class
            End Namespace
            """;
        var newCode = """
            Namespace Test
                Public Class Foo
                End Class
            End Namespace
            """;
        var options = new DiffOptions();

        var result = _differ.Compare(oldCode, newCode, options);

        result.Stats.Deletions.Should().BeGreaterThan(0);
        var changes = result.FileChanges[0].Changes.Flatten();
        changes.Should().Contain(c => c.Type == ChangeType.Removed && c.Kind == ChangeKind.Class && c.Name == "Bar");
    }

    [Fact]
    public void Compare_ModuleAdded_DetectsAddition()
    {
        var oldCode = """
            Namespace Test
                Public Module Foo
                End Module
            End Namespace
            """;
        var newCode = """
            Namespace Test
                Public Module Foo
                End Module
                Public Module Bar
                End Module
            End Namespace
            """;
        var options = new DiffOptions();

        var result = _differ.Compare(oldCode, newCode, options);

        result.Stats.Additions.Should().BeGreaterThan(0);
        var changes = result.FileChanges[0].Changes.Flatten();
        changes.Should().Contain(c => c.Type == ChangeType.Added && c.Kind == ChangeKind.Class && c.Name == "Bar");
    }

    #endregion

    #region Compare Tests - Methods (Sub and Function)

    [Fact]
    public void Compare_SubAdded_DetectsMethodAddition()
    {
        var oldCode = """
            Namespace Test
                Public Class Foo
                    Public Sub Existing()
                    End Sub
                End Class
            End Namespace
            """;
        var newCode = """
            Namespace Test
                Public Class Foo
                    Public Sub Existing()
                    End Sub
                    Public Sub NewMethod()
                    End Sub
                End Class
            End Namespace
            """;
        var options = new DiffOptions();

        var result = _differ.Compare(oldCode, newCode, options);

        result.Stats.Additions.Should().BeGreaterThan(0);
    }

    [Fact]
    public void Compare_FunctionAdded_DetectsMethodAddition()
    {
        var oldCode = """
            Namespace Test
                Public Class Foo
                    Public Function Existing() As Integer
                        Return 0
                    End Function
                End Class
            End Namespace
            """;
        var newCode = """
            Namespace Test
                Public Class Foo
                    Public Function Existing() As Integer
                        Return 0
                    End Function
                    Public Function NewFunction() As String
                        Return ""
                    End Function
                End Class
            End Namespace
            """;
        var options = new DiffOptions();

        var result = _differ.Compare(oldCode, newCode, options);

        result.Stats.Additions.Should().BeGreaterThan(0);
    }

    [Fact]
    public void Compare_MethodRemoved_DetectsMethodRemoval()
    {
        var oldCode = """
            Namespace Test
                Public Class Foo
                    Public Sub ToRemove()
                    End Sub
                End Class
            End Namespace
            """;
        var newCode = """
            Namespace Test
                Public Class Foo
                End Class
            End Namespace
            """;
        var options = new DiffOptions();

        var result = _differ.Compare(oldCode, newCode, options);

        result.Stats.Deletions.Should().BeGreaterThan(0);
    }

    [Fact]
    public void Compare_MethodModified_DetectsModification()
    {
        var oldCode = """
            Namespace Test
                Public Class Foo
                    Public Sub MyMethod()
                        Console.WriteLine("Old")
                    End Sub
                End Class
            End Namespace
            """;
        var newCode = """
            Namespace Test
                Public Class Foo
                    Public Sub MyMethod()
                        Console.WriteLine("New")
                    End Sub
                End Class
            End Namespace
            """;
        var options = new DiffOptions();

        var result = _differ.Compare(oldCode, newCode, options);

        result.Stats.Modifications.Should().BeGreaterThan(0);
    }

    #endregion

    #region Compare Tests - Properties

    [Fact]
    public void Compare_PropertyAdded_DetectsAddition()
    {
        var oldCode = """
            Namespace Test
                Public Class Foo
                    Public Property ExistingProperty As String
                End Class
            End Namespace
            """;
        var newCode = """
            Namespace Test
                Public Class Foo
                    Public Property ExistingProperty As String
                    Public Property NewProperty As Integer
                End Class
            End Namespace
            """;
        var options = new DiffOptions();

        var result = _differ.Compare(oldCode, newCode, options);

        result.Stats.Additions.Should().BeGreaterThan(0);
    }

    [Fact]
    public void Compare_PropertyWithGetSet_DetectsAddition()
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
                    Private _value As Integer
                    Public Property Value As Integer
                        Get
                            Return _value
                        End Get
                        Set(value As Integer)
                            _value = value
                        End Set
                    End Property
                End Class
            End Namespace
            """;
        var options = new DiffOptions();

        var result = _differ.Compare(oldCode, newCode, options);

        result.Stats.Additions.Should().BeGreaterThan(0);
    }

    #endregion

    #region Compare Tests - VB-Specific Constructs

    [Fact]
    public void Compare_StructureAdded_DetectsAddition()
    {
        var oldCode = """
            Namespace Test
            End Namespace
            """;
        var newCode = """
            Namespace Test
                Public Structure Point
                    Public X As Integer
                    Public Y As Integer
                End Structure
            End Namespace
            """;
        var options = new DiffOptions();

        var result = _differ.Compare(oldCode, newCode, options);

        result.Stats.Additions.Should().BeGreaterThan(0);
        var changes = result.FileChanges[0].Changes.Flatten();
        changes.Should().Contain(c => c.Type == ChangeType.Added && c.Kind == ChangeKind.Class && c.Name == "Point");
    }

    [Fact]
    public void Compare_InterfaceAdded_DetectsAddition()
    {
        var oldCode = """
            Namespace Test
            End Namespace
            """;
        var newCode = """
            Namespace Test
                Public Interface IFoo
                    Sub DoSomething()
                End Interface
            End Namespace
            """;
        var options = new DiffOptions();

        var result = _differ.Compare(oldCode, newCode, options);

        result.Stats.Additions.Should().BeGreaterThan(0);
        var changes = result.FileChanges[0].Changes.Flatten();
        changes.Should().Contain(c => c.Type == ChangeType.Added && c.Kind == ChangeKind.Class && c.Name == "IFoo");
    }

    [Fact]
    public void Compare_EnumAdded_DetectsAddition()
    {
        var oldCode = """
            Namespace Test
            End Namespace
            """;
        var newCode = """
            Namespace Test
                Public Enum Color
                    Red
                    Green
                    Blue
                End Enum
            End Namespace
            """;
        var options = new DiffOptions();

        var result = _differ.Compare(oldCode, newCode, options);

        result.Stats.Additions.Should().BeGreaterThan(0);
        var changes = result.FileChanges[0].Changes.Flatten();
        changes.Should().Contain(c => c.Type == ChangeType.Added && c.Kind == ChangeKind.Class && c.Name == "Color");
    }

    #endregion

    #region Compare Tests - Case Insensitivity

    [Fact]
    public void Compare_CaseInsensitiveMatching_MatchesDifferentCase()
    {
        // VB is case-insensitive, so MyMethod and MYMETHOD should be treated as the same
        var oldCode = """
            Namespace Test
                Public Class Foo
                    Public Sub MyMethod()
                        Console.WriteLine("Old")
                    End Sub
                End Class
            End Namespace
            """;
        var newCode = """
            Namespace Test
                Public Class Foo
                    Public Sub MYMETHOD()
                        Console.WriteLine("New")
                    End Sub
                End Class
            End Namespace
            """;
        var options = new DiffOptions();

        var result = _differ.Compare(oldCode, newCode, options);

        // Should detect a modification, not an addition and removal
        result.Stats.Modifications.Should().BeGreaterThan(0);
        // Should NOT have both an addition and removal of the same method
        var changes = result.FileChanges[0].Changes.Flatten();
        changes.Should().NotContain(c => c.Type == ChangeType.Added && c.Name != null && c.Name.ToLower() == "mymethod");
        changes.Should().NotContain(c => c.Type == ChangeType.Removed && c.Name != null && c.Name.ToLower() == "mymethod");
    }

    #endregion

    #region Compare Tests - Error Handling

    [Fact]
    public void Compare_NullOldContent_ThrowsArgumentNullException()
    {
        var options = new DiffOptions();
        var act = () => _differ.Compare(null!, "code", options);
        act.Should().Throw<ArgumentNullException>().WithParameterName("oldContent");
    }

    [Fact]
    public void Compare_NullNewContent_ThrowsArgumentNullException()
    {
        var options = new DiffOptions();
        var act = () => _differ.Compare("code", null!, options);
        act.Should().Throw<ArgumentNullException>().WithParameterName("newContent");
    }

    [Fact]
    public void Compare_NullOptions_ThrowsArgumentNullException()
    {
        var act = () => _differ.Compare("code", "code", null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("options");
    }

    [Fact]
    public void Compare_ParseError_ReturnsErrorResult()
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
        var options = new DiffOptions();

        var result = _differ.Compare(validCode, invalidCode, options);

        result.FileChanges.Should().HaveCount(1);
        var changes = result.FileChanges[0].Changes.Flatten();
        changes.Should().Contain(c => c.Name == "Parse Error");
    }

    #endregion

    #region Compare Tests - Locations

    [Fact]
    public void Compare_ChangesHaveLocations()
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
                End Class
                Public Class Bar
                End Class
            End Namespace
            """;
        var options = new DiffOptions
        {
            OldPath = "old.vb",
            NewPath = "new.vb"
        };

        var result = _differ.Compare(oldCode, newCode, options);

        var addedClass = result.FileChanges[0].Changes.Flatten()
            .FirstOrDefault(c => c.Type == ChangeType.Added && c.Name == "Bar");

        addedClass.Should().NotBeNull();
        addedClass!.NewLocation.Should().NotBeNull();
        addedClass.NewLocation!.StartLine.Should().BeGreaterThan(0);
    }

    #endregion

    #region DifferFactory Integration

    [Fact]
    public void DifferFactory_ForVbFile_ReturnsVisualBasicDiffer()
    {
        var factory = new DifferFactory();
        var options = new DiffOptions();

        var differ = factory.GetDiffer("file.vb", options);

        differ.Should().BeOfType<VisualBasicDiffer>();
    }

    [Fact]
    public void DifferFactory_ForVbFileWithLineMode_ReturnsLineDiffer()
    {
        var factory = new DifferFactory();
        var options = new DiffOptions { Mode = DiffMode.Line };

        var differ = factory.GetDiffer("file.vb", options);

        differ.Should().BeOfType<LineDiffer>();
    }

    [Fact]
    public void DifferFactory_ForVbFileWithRoslynMode_ReturnsVisualBasicDiffer()
    {
        var factory = new DifferFactory();
        var options = new DiffOptions { Mode = DiffMode.Roslyn };

        var differ = factory.GetDiffer("file.vb", options);

        differ.Should().BeOfType<VisualBasicDiffer>();
    }

    [Fact]
    public void DifferFactory_SupportsSemantic_ReturnsTrueForVb()
    {
        var result = DifferFactory.SupportsSemantic("test.vb");
        result.Should().BeTrue();
    }

    #endregion

    #region Compare Tests - Empty Files

    [Fact]
    public void Compare_BothEmpty_ReturnsNoChanges()
    {
        var code = "";
        var options = new DiffOptions();

        var result = _differ.Compare(code, code, options);

        result.Stats.TotalChanges.Should().Be(0);
    }

    [Fact]
    public void Compare_EmptyToContent_DetectsAdditions()
    {
        var oldCode = "";
        var newCode = """
            Namespace Test
                Public Class Foo
                End Class
            End Namespace
            """;
        var options = new DiffOptions();

        var result = _differ.Compare(oldCode, newCode, options);

        result.Stats.Additions.Should().BeGreaterThan(0);
    }

    [Fact]
    public void Compare_ContentToEmpty_DetectsRemovals()
    {
        var oldCode = """
            Namespace Test
                Public Class Foo
                End Class
            End Namespace
            """;
        var newCode = "";
        var options = new DiffOptions();

        var result = _differ.Compare(oldCode, newCode, options);

        result.Stats.Deletions.Should().BeGreaterThan(0);
    }

    #endregion

    #region Compare Tests - Constructor

    [Fact]
    public void Compare_ConstructorAdded_DetectsAddition()
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
                    Public Sub New()
                    End Sub
                End Class
            End Namespace
            """;
        var options = new DiffOptions();

        var result = _differ.Compare(oldCode, newCode, options);

        result.Stats.Additions.Should().BeGreaterThan(0);
    }

    [Fact]
    public void Compare_ConstructorWithParameters_DetectsAddition()
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
                    Public Sub New(value As Integer)
                        Console.WriteLine(value)
                    End Sub
                End Class
            End Namespace
            """;
        var options = new DiffOptions();

        var result = _differ.Compare(oldCode, newCode, options);

        result.Stats.Additions.Should().BeGreaterThan(0);
    }

    #endregion

    #region Compare Tests - Fields

    [Fact]
    public void Compare_FieldAdded_DetectsAddition()
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
                    Private _value As Integer
                End Class
            End Namespace
            """;
        var options = new DiffOptions();

        var result = _differ.Compare(oldCode, newCode, options);

        result.Stats.Additions.Should().BeGreaterThan(0);
    }

    #endregion
}
