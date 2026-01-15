namespace RoslynDiff.Core.Tests;

using FluentAssertions;
using RoslynDiff.Core.Differ;
using RoslynDiff.Core.Models;
using Xunit;

/// <summary>
/// Unit tests for <see cref="CSharpDiffer"/>.
/// </summary>
public class CSharpDifferTests
{
    private readonly CSharpDiffer _differ = new();

    #region CanHandle Tests

    [Fact]
    public void CanHandle_CsFile_ReturnsTrue()
    {
        var options = new DiffOptions();
        var result = _differ.CanHandle("test.cs", options);
        result.Should().BeTrue();
    }

    [Fact]
    public void CanHandle_CsFileWithLineMode_ReturnsFalse()
    {
        var options = new DiffOptions { Mode = DiffMode.Line };
        var result = _differ.CanHandle("test.cs", options);
        result.Should().BeFalse();
    }

    [Fact]
    public void CanHandle_NonCsFile_ReturnsFalse()
    {
        var options = new DiffOptions();
        var result = _differ.CanHandle("test.txt", options);
        result.Should().BeFalse();
    }

    #endregion

    #region Compare Tests - Basic

    [Fact]
    public void Compare_IdenticalCode_ReturnsNoChanges()
    {
        var code = """
            namespace Test;
            public class Foo { }
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
            namespace Test;
            public class Foo { }
            """;
        var newCode = """
            namespace Test;
            public class Foo { }
            public class Bar { }
            """;
        var options = new DiffOptions();

        var result = _differ.Compare(oldCode, newCode, options);

        result.Stats.Additions.Should().BeGreaterThan(0);
        result.FileChanges.Should().HaveCount(1);
        var changes = result.FileChanges[0].Changes;
        changes.Should().Contain(c => c.Type == ChangeType.Added && c.Kind == ChangeKind.Class && c.Name == "Bar");
    }

    [Fact]
    public void Compare_ClassRemoved_DetectsRemoval()
    {
        var oldCode = """
            namespace Test;
            public class Foo { }
            public class Bar { }
            """;
        var newCode = """
            namespace Test;
            public class Foo { }
            """;
        var options = new DiffOptions();

        var result = _differ.Compare(oldCode, newCode, options);

        result.Stats.Deletions.Should().BeGreaterThan(0);
        var changes = result.FileChanges[0].Changes;
        changes.Should().Contain(c => c.Type == ChangeType.Removed && c.Kind == ChangeKind.Class && c.Name == "Bar");
    }

    [Fact]
    public void Compare_MethodAdded_DetectsMethodAddition()
    {
        var oldCode = """
            namespace Test;
            public class Foo
            {
                public void Existing() { }
            }
            """;
        var newCode = """
            namespace Test;
            public class Foo
            {
                public void Existing() { }
                public void NewMethod() { }
            }
            """;
        var options = new DiffOptions();

        var result = _differ.Compare(oldCode, newCode, options);

        result.Stats.Additions.Should().BeGreaterThan(0);
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
    public void Compare_ParseError_ReturnsErrorResult()
    {
        var validCode = "namespace Test; public class Foo { }";
        var invalidCode = "namespace Test public class { }"; // Missing semicolon and class name
        var options = new DiffOptions();

        var result = _differ.Compare(validCode, invalidCode, options);

        result.FileChanges.Should().HaveCount(1);
        var changes = result.FileChanges[0].Changes;
        changes.Should().Contain(c => c.Name == "Parse Error");
    }

    #endregion

    #region Compare Tests - Locations

    [Fact]
    public void Compare_ChangesHaveLocations()
    {
        var oldCode = """
            namespace Test;
            public class Foo { }
            """;
        var newCode = """
            namespace Test;
            public class Foo { }
            public class Bar { }
            """;
        var options = new DiffOptions
        {
            OldPath = "old.cs",
            NewPath = "new.cs"
        };

        var result = _differ.Compare(oldCode, newCode, options);

        var addedClass = result.FileChanges[0].Changes
            .FirstOrDefault(c => c.Type == ChangeType.Added && c.Name == "Bar");

        addedClass.Should().NotBeNull();
        addedClass!.NewLocation.Should().NotBeNull();
        addedClass.NewLocation!.StartLine.Should().BeGreaterThan(0);
    }

    #endregion

    #region DifferFactory Integration

    [Fact]
    public void DifferFactory_ForCsFile_ReturnsCSharpDiffer()
    {
        var factory = new DifferFactory();
        var options = new DiffOptions();

        var differ = factory.GetDiffer("file.cs", options);

        differ.Should().BeOfType<CSharpDiffer>();
    }

    [Fact]
    public void DifferFactory_ForCsFileWithLineMode_ReturnsLineDiffer()
    {
        var factory = new DifferFactory();
        var options = new DiffOptions { Mode = DiffMode.Line };

        var differ = factory.GetDiffer("file.cs", options);

        differ.Should().BeOfType<LineDiffer>();
    }

    #endregion
}
