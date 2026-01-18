namespace RoslynDiff.Core.Tests;

using FluentAssertions;
using RoslynDiff.Core.Comparison;
using RoslynDiff.Core.Models;
using Xunit;

/// <summary>
/// Unit tests for <see cref="SemanticComparer"/>.
/// </summary>
public class SemanticComparerTests
{
    private readonly SyntaxComparer _syntaxComparer = new();
    private readonly SemanticComparer _semanticComparer = new();

    /// <summary>
    /// Flattens a hierarchical change list into a flat list of all changes including children.
    /// </summary>
    private static List<Change> FlattenAllChanges(IReadOnlyList<Change> changes)
    {
        var result = new List<Change>();
        FlattenChangesRecursive(changes, result);
        return result;
    }

    private static void FlattenChangesRecursive(IReadOnlyList<Change>? changes, List<Change> result)
    {
        if (changes is null)
            return;

        foreach (var change in changes)
        {
            result.Add(change);
            FlattenChangesRecursive(change.Children, result);
        }
    }

    #region Rename Detection Tests - Classes

    [Fact]
    public void EnhanceWithSemantics_ClassRenamed_DetectsRename()
    {
        var oldCode = """
            namespace Test;
            public class OldClassName
            {
                public void DoSomething()
                {
                    Console.WriteLine("Hello");
                }
            }
            """;
        var newCode = """
            namespace Test;
            public class NewClassName
            {
                public void DoSomething()
                {
                    Console.WriteLine("Hello");
                }
            }
            """;
        var options = new DiffOptions();

        // First get syntax changes
        var syntaxChanges = _syntaxComparer.CompareSource(oldCode, newCode, options);

        // Enhance with semantic analysis
        var result = _semanticComparer.EnhanceWithSemantics(syntaxChanges, oldCode, newCode, options);

        // Should detect a rename instead of add+remove - flatten to find nested changes
        var flatResult = FlattenAllChanges(result);
        flatResult.Should().ContainSingle(c => c.Type == ChangeType.Renamed);
        var renamed = flatResult.Single(c => c.Type == ChangeType.Renamed);
        renamed.Kind.Should().Be(ChangeKind.Class);
        renamed.Name.Should().Be("NewClassName");
        renamed.OldName.Should().Be("OldClassName");
    }

    [Fact]
    public void EnhanceWithSemantics_ClassRenamedWithSameMembers_DetectsRename()
    {
        var oldCode = """
            namespace Test;
            public class OriginalService
            {
                private readonly string _name;

                public OriginalService(string name)
                {
                    _name = name;
                }

                public string GetName() => _name;
            }
            """;
        var newCode = """
            namespace Test;
            public class RenamedService
            {
                private readonly string _name;

                public RenamedService(string name)
                {
                    _name = name;
                }

                public string GetName() => _name;
            }
            """;
        var options = new DiffOptions();

        var syntaxChanges = _syntaxComparer.CompareSource(oldCode, newCode, options);
        var result = _semanticComparer.EnhanceWithSemantics(syntaxChanges, oldCode, newCode, options);

        // Should detect class rename - flatten to find nested changes
        var flatResult = FlattenAllChanges(result);
        flatResult.Should().Contain(c => c.Type == ChangeType.Renamed && c.Kind == ChangeKind.Class);
        var renamed = flatResult.First(c => c.Type == ChangeType.Renamed && c.Kind == ChangeKind.Class);
        renamed.Name.Should().Be("RenamedService");
        renamed.OldName.Should().Be("OriginalService");
    }

    #endregion

    #region Rename Detection Tests - Methods

    [Fact]
    public void EnhanceWithSemantics_MethodRenamed_DetectsRename()
    {
        var oldCode = """
            namespace Test;
            public class Calculator
            {
                public int OldMethodName(int a, int b)
                {
                    return a + b;
                }
            }
            """;
        var newCode = """
            namespace Test;
            public class Calculator
            {
                public int NewMethodName(int a, int b)
                {
                    return a + b;
                }
            }
            """;
        var options = new DiffOptions();

        var syntaxChanges = _syntaxComparer.CompareSource(oldCode, newCode, options);
        var result = _semanticComparer.EnhanceWithSemantics(syntaxChanges, oldCode, newCode, options);

        // Flatten all changes to find nested ones
        var allChanges = FlattenAllChanges(result);

        // Should detect a rename
        allChanges.Should().Contain(c => c.Type == ChangeType.Renamed && c.Kind == ChangeKind.Method);
        var renamed = allChanges.First(c => c.Type == ChangeType.Renamed && c.Kind == ChangeKind.Method);
        renamed.Name.Should().Be("NewMethodName");
        renamed.OldName.Should().Be("OldMethodName");
    }

    [Fact]
    public void EnhanceWithSemantics_MethodRenamedWithComplexBody_DetectsRename()
    {
        var oldCode = """
            namespace Test;
            public class DataProcessor
            {
                public void ProcessOld(string[] items)
                {
                    foreach (var item in items)
                    {
                        if (!string.IsNullOrEmpty(item))
                        {
                            Console.WriteLine(item.ToUpper());
                        }
                    }
                }
            }
            """;
        var newCode = """
            namespace Test;
            public class DataProcessor
            {
                public void ProcessNew(string[] items)
                {
                    foreach (var item in items)
                    {
                        if (!string.IsNullOrEmpty(item))
                        {
                            Console.WriteLine(item.ToUpper());
                        }
                    }
                }
            }
            """;
        var options = new DiffOptions();

        var syntaxChanges = _syntaxComparer.CompareSource(oldCode, newCode, options);
        var result = _semanticComparer.EnhanceWithSemantics(syntaxChanges, oldCode, newCode, options);

        // Flatten all changes to find nested ones
        var allChanges = FlattenAllChanges(result);

        allChanges.Should().Contain(c => c.Type == ChangeType.Renamed && c.Kind == ChangeKind.Method);
        var renamed = allChanges.First(c => c.Type == ChangeType.Renamed && c.Kind == ChangeKind.Method);
        renamed.Name.Should().Be("ProcessNew");
        renamed.OldName.Should().Be("ProcessOld");
    }

    #endregion

    #region Move Detection Tests

    [Fact]
    public void EnhanceWithSemantics_MethodMovedBetweenClasses_DetectsMove()
    {
        var oldCode = """
            namespace Test;
            public class SourceClass
            {
                public void MovableMethod()
                {
                    Console.WriteLine("I will be moved");
                }
            }
            public class TargetClass
            {
            }
            """;
        var newCode = """
            namespace Test;
            public class SourceClass
            {
            }
            public class TargetClass
            {
                public void MovableMethod()
                {
                    Console.WriteLine("I will be moved");
                }
            }
            """;
        var options = new DiffOptions();

        var syntaxChanges = _syntaxComparer.CompareSource(oldCode, newCode, options);
        var result = _semanticComparer.EnhanceWithSemantics(syntaxChanges, oldCode, newCode, options);

        // Flatten all changes to find nested ones
        var allChanges = FlattenAllChanges(result);
        allChanges.Should().Contain(c => c.Type == ChangeType.Moved && c.Kind == ChangeKind.Method);
        var moved = allChanges.First(c => c.Type == ChangeType.Moved && c.Kind == ChangeKind.Method);
        moved.Name.Should().Be("MovableMethod");
    }

    [Fact]
    public void EnhanceWithSemantics_PropertyMovedBetweenClasses_DetectsMove()
    {
        var oldCode = """
            namespace Test;
            public class ClassA
            {
                public string SharedProperty { get; set; }
            }
            public class ClassB
            {
            }
            """;
        var newCode = """
            namespace Test;
            public class ClassA
            {
            }
            public class ClassB
            {
                public string SharedProperty { get; set; }
            }
            """;
        var options = new DiffOptions();

        var syntaxChanges = _syntaxComparer.CompareSource(oldCode, newCode, options);
        var result = _semanticComparer.EnhanceWithSemantics(syntaxChanges, oldCode, newCode, options);

        // Flatten all changes to find nested ones
        var allChanges = FlattenAllChanges(result);
        allChanges.Should().Contain(c => c.Type == ChangeType.Moved && c.Kind == ChangeKind.Property);
        var moved = allChanges.First(c => c.Type == ChangeType.Moved && c.Kind == ChangeKind.Property);
        moved.Name.Should().Be("SharedProperty");
    }

    #endregion

    #region Unrelated Add/Remove Tests

    [Fact]
    public void EnhanceWithSemantics_DifferentMethods_StaysAddedRemoved()
    {
        var oldCode = """
            namespace Test;
            public class Service
            {
                public void OldMethod()
                {
                    Console.WriteLine("Old implementation");
                }
            }
            """;
        var newCode = """
            namespace Test;
            public class Service
            {
                public void NewMethod()
                {
                    // Completely different implementation
                    for (int i = 0; i < 10; i++)
                    {
                        DoSomethingElse(i);
                    }
                }
            }
            """;
        var options = new DiffOptions();

        var syntaxChanges = _syntaxComparer.CompareSource(oldCode, newCode, options);
        var result = _semanticComparer.EnhanceWithSemantics(syntaxChanges, oldCode, newCode, options);

        // Flatten all changes to find nested ones
        var allChanges = FlattenAllChanges(result);

        // Should remain as separate add/remove since bodies are too different
        allChanges.Should().NotContain(c => c.Type == ChangeType.Renamed && c.Kind == ChangeKind.Method);
        allChanges.Should().Contain(c => c.Type == ChangeType.Removed && c.Name == "OldMethod");
        allChanges.Should().Contain(c => c.Type == ChangeType.Added && c.Name == "NewMethod");
    }

    [Fact]
    public void EnhanceWithSemantics_DifferentClasses_StaysAddedRemoved()
    {
        var oldCode = """
            namespace Test;
            public class OldClass
            {
                public string PropertyA { get; set; }
            }
            """;
        var newCode = """
            namespace Test;
            public class NewClass
            {
                public int CompletelyDifferentProperty { get; set; }
                public void SomeMethod() { }
            }
            """;
        var options = new DiffOptions();

        var syntaxChanges = _syntaxComparer.CompareSource(oldCode, newCode, options);
        var result = _semanticComparer.EnhanceWithSemantics(syntaxChanges, oldCode, newCode, options);

        // Flatten all changes to find nested ones
        var allChanges = FlattenAllChanges(result);

        // Should remain as separate add/remove since class contents are too different
        allChanges.Should().NotContain(c => c.Type == ChangeType.Renamed && c.Kind == ChangeKind.Class);
        allChanges.Should().Contain(c => c.Type == ChangeType.Removed && c.Name == "OldClass");
        allChanges.Should().Contain(c => c.Type == ChangeType.Added && c.Name == "NewClass");
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void EnhanceWithSemantics_NoChanges_ReturnsEmptyList()
    {
        var code = """
            namespace Test;
            public class Unchanged { }
            """;
        var options = new DiffOptions();

        var syntaxChanges = _syntaxComparer.CompareSource(code, code, options);
        var result = _semanticComparer.EnhanceWithSemantics(syntaxChanges, code, code, options);

        result.Should().BeEmpty();
    }

    [Fact]
    public void EnhanceWithSemantics_OnlyAdditions_ReturnsAdditions()
    {
        var oldCode = """
            namespace Test;
            public class Base { }
            """;
        var newCode = """
            namespace Test;
            public class Base { }
            public class NewClass { }
            """;
        var options = new DiffOptions();

        var syntaxChanges = _syntaxComparer.CompareSource(oldCode, newCode, options);
        var result = _semanticComparer.EnhanceWithSemantics(syntaxChanges, oldCode, newCode, options);

        // Use Flatten() because changes may be hierarchical (nested under namespace)
        var flatResult = result.Flatten().ToList();
        flatResult.Should().ContainSingle(c => c.Type == ChangeType.Added && c.Name == "NewClass");
        flatResult.Should().NotContain(c => c.Type == ChangeType.Renamed);
        flatResult.Should().NotContain(c => c.Type == ChangeType.Moved);
    }

    [Fact]
    public void EnhanceWithSemantics_OnlyRemovals_ReturnsRemovals()
    {
        var oldCode = """
            namespace Test;
            public class Base { }
            public class ToRemove { }
            """;
        var newCode = """
            namespace Test;
            public class Base { }
            """;
        var options = new DiffOptions();

        var syntaxChanges = _syntaxComparer.CompareSource(oldCode, newCode, options);
        var result = _semanticComparer.EnhanceWithSemantics(syntaxChanges, oldCode, newCode, options);

        // Use Flatten() because changes may be hierarchical (nested under namespace)
        var flatResult = result.Flatten().ToList();
        flatResult.Should().ContainSingle(c => c.Type == ChangeType.Removed && c.Name == "ToRemove");
        flatResult.Should().NotContain(c => c.Type == ChangeType.Renamed);
        flatResult.Should().NotContain(c => c.Type == ChangeType.Moved);
    }

    [Fact]
    public void EnhanceWithSemantics_NullArguments_ThrowsArgumentNullException()
    {
        var options = new DiffOptions();
        var changes = new List<Change>();
        var code = "namespace Test;";

        var act1 = () => _semanticComparer.EnhanceWithSemantics(null!, code, code, options);
        var act2 = () => _semanticComparer.EnhanceWithSemantics(changes, null!, code, options);
        var act3 = () => _semanticComparer.EnhanceWithSemantics(changes, code, null!, options);
        var act4 = () => _semanticComparer.EnhanceWithSemantics(changes, code, code, null!);

        act1.Should().Throw<ArgumentNullException>();
        act2.Should().Throw<ArgumentNullException>();
        act3.Should().Throw<ArgumentNullException>();
        act4.Should().Throw<ArgumentNullException>();
    }

    #endregion

    #region Multiple Renames

    [Fact]
    public void EnhanceWithSemantics_MultipleMethodRenames_DetectsAllRenames()
    {
        var oldCode = """
            namespace Test;
            public class Calculator
            {
                public int Add(int a, int b) => a + b;
                public int Subtract(int a, int b) => a - b;
            }
            """;
        var newCode = """
            namespace Test;
            public class Calculator
            {
                public int Sum(int a, int b) => a + b;
                public int Minus(int a, int b) => a - b;
            }
            """;
        var options = new DiffOptions();

        var syntaxChanges = _syntaxComparer.CompareSource(oldCode, newCode, options);
        var result = _semanticComparer.EnhanceWithSemantics(syntaxChanges, oldCode, newCode, options);

        // Flatten all changes to find nested ones (distinct by reference to avoid duplicates)
        var allChanges = FlattenAllChanges(result).Distinct().ToList();

        var renames = allChanges.Where(c => c.Type == ChangeType.Renamed).ToList();
        renames.Should().HaveCount(2);
        renames.Should().Contain(r => r.OldName == "Add" && r.Name == "Sum");
        renames.Should().Contain(r => r.OldName == "Subtract" && r.Name == "Minus");
    }

    #endregion

    #region Mixed Changes

    [Fact]
    public void EnhanceWithSemantics_MixedChanges_HandlesCorrectly()
    {
        var oldCode = """
            namespace Test;
            public class Service
            {
                public void RenamedMethod() => Console.WriteLine("A");
                public void RemovedMethod()
                {
                    // This method has completely different logic
                    var x = 42;
                    for (int i = 0; i < x; i++)
                    {
                        DoSomething(i);
                    }
                }
                public void UnchangedMethod() => Console.WriteLine("C");
            }
            """;
        var newCode = """
            namespace Test;
            public class Service
            {
                public void RenamedMethodNew() => Console.WriteLine("A");
                public void AddedMethod()
                {
                    // Totally different implementation
                    var list = new List<string>();
                    foreach (var item in list)
                    {
                        Process(item);
                    }
                }
                public void UnchangedMethod() => Console.WriteLine("C");
            }
            """;
        var options = new DiffOptions();

        var syntaxChanges = _syntaxComparer.CompareSource(oldCode, newCode, options);
        var result = _semanticComparer.EnhanceWithSemantics(syntaxChanges, oldCode, newCode, options);

        // Flatten all changes to find nested ones
        var allChanges = FlattenAllChanges(result);

        // Should have a rename (RenamedMethod -> RenamedMethodNew)
        allChanges.Should().Contain(c =>
            c.Type == ChangeType.Renamed &&
            c.OldName == "RenamedMethod" &&
            c.Name == "RenamedMethodNew");

        // Should have a removal (RemovedMethod) - not matched with AddedMethod due to different bodies
        allChanges.Should().Contain(c => c.Type == ChangeType.Removed && c.Name == "RemovedMethod");

        // Should have an addition (AddedMethod) - not matched with RemovedMethod due to different bodies
        allChanges.Should().Contain(c => c.Type == ChangeType.Added && c.Name == "AddedMethod");
    }

    #endregion
}
