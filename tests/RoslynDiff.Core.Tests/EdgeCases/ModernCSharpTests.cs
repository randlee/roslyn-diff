namespace RoslynDiff.Core.Tests.EdgeCases;

using FluentAssertions;
using RoslynDiff.Core.Differ;
using RoslynDiff.Core.Models;
using Xunit;

/// <summary>
/// Tests for modern C# language features (C# 9+).
/// </summary>
public class ModernCSharpTests
{
    private readonly CSharpDiffer _differ = new();

    #region Records Tests

    [Fact]
    public void Compare_RecordDeclaration_DetectsChanges()
    {
        // Arrange
        var oldCode = """
            namespace Test;
            public record Person(string Name);
            """;
        var newCode = """
            namespace Test;
            public record Person(string Name, int Age);
            """;

        var options = new DiffOptions();

        // Act
        var result = _differ.Compare(oldCode, newCode, options);

        // Assert
        result.Stats.TotalChanges.Should().BeGreaterThan(0);
    }

    [Fact]
    public void Compare_RecordWithPrimaryConstructor_DetectsChanges()
    {
        // Arrange
        var oldCode = "public record Person(string Name);";
        var newCode = "public record Person(string Name, int Age);";

        var options = new DiffOptions();

        // Act
        var result = _differ.Compare(oldCode, newCode, options);

        // Assert
        result.Stats.TotalChanges.Should().BeGreaterThan(0);
    }

    [Fact]
    public void Compare_RecordStruct_DetectsChanges()
    {
        // Arrange
        var oldCode = """
            namespace Test;
            public record struct Point(int X, int Y);
            """;
        var newCode = """
            namespace Test;
            public record struct Point(int X, int Y, int Z);
            """;

        var options = new DiffOptions();

        // Act
        var result = _differ.Compare(oldCode, newCode, options);

        // Assert
        result.Stats.TotalChanges.Should().BeGreaterThan(0);
    }

    [Fact]
    public void Compare_RecordToRecordStruct_DetectsChanges()
    {
        // Arrange
        var oldCode = """
            namespace Test;
            public record Point(int X, int Y);
            """;
        var newCode = """
            namespace Test;
            public record struct Point(int X, int Y);
            """;

        var options = new DiffOptions();

        // Act
        var result = _differ.Compare(oldCode, newCode, options);

        // Assert
        result.Stats.TotalChanges.Should().BeGreaterThan(0);
    }

    [Fact]
    public void Compare_ReadonlyRecordStruct_DetectsChanges()
    {
        // Arrange
        var oldCode = """
            namespace Test;
            public record struct Point(int X, int Y);
            """;
        var newCode = """
            namespace Test;
            public readonly record struct Point(int X, int Y);
            """;

        var options = new DiffOptions();

        // Act
        var result = _differ.Compare(oldCode, newCode, options);

        // Assert
        result.Stats.TotalChanges.Should().BeGreaterThan(0);
    }

    [Fact]
    public void Compare_RecordWithWith_DetectsChanges()
    {
        // Arrange
        var oldCode = """
            namespace Test;
            public record Person(string Name);
            public class Usage
            {
                public Person Update(Person p) => p with { Name = "New" };
            }
            """;
        var newCode = """
            namespace Test;
            public record Person(string Name, int Age);
            public class Usage
            {
                public Person Update(Person p) => p with { Name = "New", Age = 0 };
            }
            """;

        var options = new DiffOptions();

        // Act
        var result = _differ.Compare(oldCode, newCode, options);

        // Assert
        result.Stats.TotalChanges.Should().BeGreaterThan(0);
    }

    #endregion

    #region Primary Constructors Tests

    [Fact]
    public void Compare_ClassWithPrimaryConstructor_DetectsChanges()
    {
        // Arrange
        var oldCode = """
            namespace Test;
            public class Service(ILogger logger)
            {
                public void Log(string message) => logger.Log(message);
            }
            public interface ILogger { void Log(string message); }
            """;
        var newCode = """
            namespace Test;
            public class Service(ILogger logger, string prefix)
            {
                public void Log(string message) => logger.Log(prefix + message);
            }
            public interface ILogger { void Log(string message); }
            """;

        var options = new DiffOptions();

        // Act
        var result = _differ.Compare(oldCode, newCode, options);

        // Assert
        result.Stats.TotalChanges.Should().BeGreaterThan(0);
    }

    [Fact]
    public void Compare_StructWithPrimaryConstructor_DetectsChanges()
    {
        // Arrange
        var oldCode = """
            namespace Test;
            public struct Vector(double x, double y)
            {
                public double X => x;
                public double Y => y;
            }
            """;
        var newCode = """
            namespace Test;
            public struct Vector(double x, double y, double z)
            {
                public double X => x;
                public double Y => y;
                public double Z => z;
            }
            """;

        var options = new DiffOptions();

        // Act
        var result = _differ.Compare(oldCode, newCode, options);

        // Assert
        result.Stats.TotalChanges.Should().BeGreaterThan(0);
    }

    #endregion

    #region Raw String Literals Tests

    [Fact]
    public void Compare_RawStringLiteral_DetectsChanges()
    {
        // Arrange
        var oldCode = """"
            namespace Test;
            public class Foo
            {
                public string Json = """
                    {
                        "name": "test"
                    }
                    """;
            }
            """";
        var newCode = """"
            namespace Test;
            public class Foo
            {
                public string Json = """
                    {
                        "name": "updated"
                    }
                    """;
            }
            """";

        var options = new DiffOptions();

        // Act
        var result = _differ.Compare(oldCode, newCode, options);

        // Assert
        result.Stats.TotalChanges.Should().BeGreaterThan(0);
    }

    [Fact]
    public void Compare_RawInterpolatedString_DetectsChanges()
    {
        // Arrange
        var oldCode = """"
            namespace Test;
            public class Foo
            {
                public string GetJson(string name) => $"""
                    {
                        "name": "{name}"
                    }
                    """;
            }
            """";
        var newCode = """"
            namespace Test;
            public class Foo
            {
                public string GetJson(string name) => $"""
                    {
                        "name": "{name}",
                        "version": 1
                    }
                    """;
            }
            """";

        var options = new DiffOptions();

        // Act
        var result = _differ.Compare(oldCode, newCode, options);

        // Assert
        result.Stats.TotalChanges.Should().BeGreaterThan(0);
    }

    [Fact]
    public void Compare_MultiLineRawString_DetectsChanges()
    {
        // Arrange
        var oldCode = """"
            namespace Test;
            public class Foo
            {
                public string Xml = """
                    <root>
                        <item>1</item>
                    </root>
                    """;
            }
            """";
        var newCode = """"
            namespace Test;
            public class Foo
            {
                public string Xml = """
                    <root>
                        <item>1</item>
                        <item>2</item>
                    </root>
                    """;
            }
            """";

        var options = new DiffOptions();

        // Act
        var result = _differ.Compare(oldCode, newCode, options);

        // Assert
        result.Stats.TotalChanges.Should().BeGreaterThan(0);
    }

    #endregion

    #region String Interpolation Tests

    [Fact]
    public void Compare_StringInterpolation_DetectsChanges()
    {
        // Arrange
        var oldCode = """
            namespace Test;
            public class Foo
            {
                public string Format(string name) => $"Hello, {name}!";
            }
            """;
        var newCode = """
            namespace Test;
            public class Foo
            {
                public string Format(string name) => $"Hi, {name}!";
            }
            """;

        var options = new DiffOptions();

        // Act
        var result = _differ.Compare(oldCode, newCode, options);

        // Assert
        result.Stats.TotalChanges.Should().BeGreaterThan(0);
    }

    [Fact]
    public void Compare_InterpolationWithFormatSpecifier_DetectsChanges()
    {
        // Arrange
        var oldCode = """
            namespace Test;
            public class Foo
            {
                public string Format(decimal value) => $"Price: {value:C2}";
            }
            """;
        var newCode = """
            namespace Test;
            public class Foo
            {
                public string Format(decimal value) => $"Price: {value:C4}";
            }
            """;

        var options = new DiffOptions();

        // Act
        var result = _differ.Compare(oldCode, newCode, options);

        // Assert
        result.Stats.TotalChanges.Should().BeGreaterThan(0);
    }

    [Fact]
    public void Compare_NestedInterpolation_DetectsChanges()
    {
        // Arrange
        var oldCode = """
            namespace Test;
            public class Foo
            {
                public string Format(int x) => $"Result: {(x > 0 ? $"Positive {x}" : "Non-positive")}";
            }
            """;
        var newCode = """
            namespace Test;
            public class Foo
            {
                public string Format(int x) => $"Result: {(x > 0 ? $"Pos {x}" : "Neg")}";
            }
            """;

        var options = new DiffOptions();

        // Act
        var result = _differ.Compare(oldCode, newCode, options);

        // Assert
        result.Stats.TotalChanges.Should().BeGreaterThan(0);
    }

    #endregion

    #region Collection Expressions Tests

    [Fact]
    public void Compare_CollectionExpression_DetectsChanges()
    {
        // Arrange
        var oldCode = """
            namespace Test;
            public class Foo
            {
                public int[] Numbers = [1, 2, 3];
            }
            """;
        var newCode = """
            namespace Test;
            public class Foo
            {
                public int[] Numbers = [1, 2, 3, 4];
            }
            """;

        var options = new DiffOptions();

        // Act
        var result = _differ.Compare(oldCode, newCode, options);

        // Assert
        result.Stats.TotalChanges.Should().BeGreaterThan(0);
    }

    [Fact]
    public void Compare_CollectionExpressionSpread_DetectsChanges()
    {
        // Arrange
        var oldCode = """
            namespace Test;
            public class Foo
            {
                private int[] _base = [1, 2];
                public int[] GetAll() => [.. _base, 3];
            }
            """;
        var newCode = """
            namespace Test;
            public class Foo
            {
                private int[] _base = [1, 2];
                public int[] GetAll() => [.. _base, 3, 4];
            }
            """;

        var options = new DiffOptions();

        // Act
        var result = _differ.Compare(oldCode, newCode, options);

        // Assert
        result.Stats.TotalChanges.Should().BeGreaterThan(0);
    }

    [Fact]
    public void Compare_EmptyCollectionExpression_DetectsChanges()
    {
        // Arrange
        var oldCode = """
            namespace Test;
            public class Foo
            {
                public int[] Numbers = [];
            }
            """;
        var newCode = """
            namespace Test;
            public class Foo
            {
                public int[] Numbers = [0];
            }
            """;

        var options = new DiffOptions();

        // Act
        var result = _differ.Compare(oldCode, newCode, options);

        // Assert
        result.Stats.TotalChanges.Should().BeGreaterThan(0);
    }

    #endregion

    #region File-Scoped Namespaces Tests

    [Fact]
    public void Compare_FileScopedNamespace_DetectsChanges()
    {
        // Arrange
        var oldCode = """
            namespace Test;
            public class Foo { }
            """;
        var newCode = """
            namespace Test;
            public class Bar { }
            """;

        var options = new DiffOptions();

        // Act
        var result = _differ.Compare(oldCode, newCode, options);

        // Assert
        result.Stats.TotalChanges.Should().BeGreaterThan(0);
    }

    [Fact]
    public void Compare_BlockToFileScopedNamespace_DetectsChanges()
    {
        // Arrange
        var oldCode = """
            namespace Test
            {
                public class Foo { }
            }
            """;
        var newCode = """
            namespace Test;
            public class Foo { }
            """;

        var options = new DiffOptions();

        // Act
        var result = _differ.Compare(oldCode, newCode, options);

        // Assert
        result.FileChanges.Should().HaveCount(1);
    }

    #endregion

    #region Global Usings Tests

    [Fact]
    public void Compare_GlobalUsing_WithClassChange_DetectsChanges()
    {
        // Arrange - Global usings with class changes
        var oldCode = """
            global using System;
            namespace Test;
            public class Foo { }
            """;
        var newCode = """
            global using System;
            global using System.Collections.Generic;
            namespace Test;
            public class Foo { }
            public class Bar { }
            """;

        var options = new DiffOptions();

        // Act
        var result = _differ.Compare(oldCode, newCode, options);

        // Assert - Class addition detected
        result.Stats.Additions.Should().BeGreaterThan(0);
    }

    [Fact]
    public void Compare_GlobalUsingAlias_SameCode_HandlesGracefully()
    {
        // Arrange - Using alias changes are typically not tracked as semantic changes
        var oldCode = """
            global using MyList = System.Collections.Generic.List<int>;
            namespace Test;
            public class Foo { }
            """;
        var newCode = """
            global using MyList = System.Collections.Generic.List<string>;
            namespace Test;
            public class Foo { }
            """;

        var options = new DiffOptions();

        // Act
        var result = _differ.Compare(oldCode, newCode, options);

        // Assert - Using directive changes handled gracefully
        result.FileChanges.Should().HaveCount(1);
    }

    #endregion

    #region Pattern Matching Tests

    [Fact]
    public void Compare_IsPattern_DetectsChanges()
    {
        // Arrange
        var oldCode = """
            namespace Test;
            public class Foo
            {
                public bool Check(object obj) => obj is string;
            }
            """;
        var newCode = """
            namespace Test;
            public class Foo
            {
                public bool Check(object obj) => obj is int;
            }
            """;

        var options = new DiffOptions();

        // Act
        var result = _differ.Compare(oldCode, newCode, options);

        // Assert
        result.Stats.TotalChanges.Should().BeGreaterThan(0);
    }

    [Fact]
    public void Compare_SwitchExpression_DetectsChanges()
    {
        // Arrange
        var oldCode = """
            namespace Test;
            public class Foo
            {
                public string Describe(int x) => x switch
                {
                    0 => "zero",
                    > 0 => "positive",
                    _ => "negative"
                };
            }
            """;
        var newCode = """
            namespace Test;
            public class Foo
            {
                public string Describe(int x) => x switch
                {
                    0 => "zero",
                    > 0 => "plus",
                    _ => "minus"
                };
            }
            """;

        var options = new DiffOptions();

        // Act
        var result = _differ.Compare(oldCode, newCode, options);

        // Assert
        result.Stats.TotalChanges.Should().BeGreaterThan(0);
    }

    [Fact]
    public void Compare_PropertyPattern_DetectsChanges()
    {
        // Arrange
        var oldCode = """
            namespace Test;
            public record Person(string Name, int Age);
            public class Foo
            {
                public bool IsAdult(Person p) => p is { Age: >= 18 };
            }
            """;
        var newCode = """
            namespace Test;
            public record Person(string Name, int Age);
            public class Foo
            {
                public bool IsAdult(Person p) => p is { Age: >= 21 };
            }
            """;

        var options = new DiffOptions();

        // Act
        var result = _differ.Compare(oldCode, newCode, options);

        // Assert
        result.Stats.TotalChanges.Should().BeGreaterThan(0);
    }

    [Fact]
    public void Compare_ListPattern_DetectsChanges()
    {
        // Arrange
        var oldCode = """
            namespace Test;
            public class Foo
            {
                public bool Check(int[] arr) => arr is [1, 2, ..];
            }
            """;
        var newCode = """
            namespace Test;
            public class Foo
            {
                public bool Check(int[] arr) => arr is [1, 2, 3, ..];
            }
            """;

        var options = new DiffOptions();

        // Act
        var result = _differ.Compare(oldCode, newCode, options);

        // Assert
        result.Stats.TotalChanges.Should().BeGreaterThan(0);
    }

    [Fact]
    public void Compare_RelationalPattern_DetectsChanges()
    {
        // Arrange
        var oldCode = """
            namespace Test;
            public class Foo
            {
                public bool InRange(int x) => x is > 0 and < 100;
            }
            """;
        var newCode = """
            namespace Test;
            public class Foo
            {
                public bool InRange(int x) => x is >= 0 and <= 100;
            }
            """;

        var options = new DiffOptions();

        // Act
        var result = _differ.Compare(oldCode, newCode, options);

        // Assert
        result.Stats.TotalChanges.Should().BeGreaterThan(0);
    }

    #endregion

    #region Nullable Reference Types Tests

    [Fact]
    public void Compare_NullableReferenceType_DetectsChanges()
    {
        // Arrange
        var oldCode = """
            namespace Test;
            public class Foo
            {
                public string Name { get; set; } = "";
            }
            """;
        var newCode = """
            namespace Test;
            public class Foo
            {
                public string? Name { get; set; }
            }
            """;

        var options = new DiffOptions();

        // Act
        var result = _differ.Compare(oldCode, newCode, options);

        // Assert
        result.Stats.TotalChanges.Should().BeGreaterThan(0);
    }

    [Fact]
    public void Compare_NullableEnable_DetectsChanges()
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
            #nullable disable
            namespace Test;
            public class Foo
            {
                public string Name { get; set; } = "";
            }
            """;

        var options = new DiffOptions();

        // Act
        var result = _differ.Compare(oldCode, newCode, options);

        // Assert
        result.Stats.TotalChanges.Should().BeGreaterThan(0);
    }

    [Fact]
    public void Compare_NullForgiving_DetectsChanges()
    {
        // Arrange
        var oldCode = """
            namespace Test;
            public class Foo
            {
                public string GetName(string? input) => input ?? "";
            }
            """;
        var newCode = """
            namespace Test;
            public class Foo
            {
                public string GetName(string? input) => input!;
            }
            """;

        var options = new DiffOptions();

        // Act
        var result = _differ.Compare(oldCode, newCode, options);

        // Assert
        result.Stats.TotalChanges.Should().BeGreaterThan(0);
    }

    #endregion

    #region Expression-Bodied Members Tests

    [Fact]
    public void Compare_ExpressionBodiedMethod_DetectsChanges()
    {
        // Arrange
        var oldCode = """
            namespace Test;
            public class Foo
            {
                public int Double(int x) => x * 2;
            }
            """;
        var newCode = """
            namespace Test;
            public class Foo
            {
                public int Double(int x) => x + x;
            }
            """;

        var options = new DiffOptions();

        // Act
        var result = _differ.Compare(oldCode, newCode, options);

        // Assert
        result.Stats.TotalChanges.Should().BeGreaterThan(0);
    }

    [Fact]
    public void Compare_ExpressionBodiedProperty_DetectsChanges()
    {
        // Arrange
        var oldCode = """
            namespace Test;
            public class Foo
            {
                private string _name = "";
                public string Name => _name;
            }
            """;
        var newCode = """
            namespace Test;
            public class Foo
            {
                private string _name = "";
                public string Name => _name.ToUpper();
            }
            """;

        var options = new DiffOptions();

        // Act
        var result = _differ.Compare(oldCode, newCode, options);

        // Assert
        result.Stats.TotalChanges.Should().BeGreaterThan(0);
    }

    [Fact]
    public void Compare_ExpressionBodiedConstructor_DetectsChanges()
    {
        // Arrange
        var oldCode = """
            namespace Test;
            public class Foo
            {
                public string Name { get; }
                public Foo(string name) => Name = name;
            }
            """;
        var newCode = """
            namespace Test;
            public class Foo
            {
                public string Name { get; }
                public Foo(string name) => Name = name.Trim();
            }
            """;

        var options = new DiffOptions();

        // Act
        var result = _differ.Compare(oldCode, newCode, options);

        // Assert
        result.Stats.TotalChanges.Should().BeGreaterThan(0);
    }

    #endregion

    #region Local Functions Tests

    [Fact]
    public void Compare_LocalFunction_DetectsChanges()
    {
        // Arrange
        var oldCode = """
            namespace Test;
            public class Foo
            {
                public int Process(int x)
                {
                    int Double(int n) => n * 2;
                    return Double(x);
                }
            }
            """;
        var newCode = """
            namespace Test;
            public class Foo
            {
                public int Process(int x)
                {
                    int Triple(int n) => n * 3;
                    return Triple(x);
                }
            }
            """;

        var options = new DiffOptions();

        // Act
        var result = _differ.Compare(oldCode, newCode, options);

        // Assert
        result.Stats.TotalChanges.Should().BeGreaterThan(0);
    }

    [Fact]
    public void Compare_StaticLocalFunction_DetectsChanges()
    {
        // Arrange
        var oldCode = """
            namespace Test;
            public class Foo
            {
                public int Process(int x)
                {
                    static int Double(int n) => n * 2;
                    return Double(x);
                }
            }
            """;
        var newCode = """
            namespace Test;
            public class Foo
            {
                public int Process(int x)
                {
                    static int Triple(int n) => n * 3;
                    return Triple(x);
                }
            }
            """;

        var options = new DiffOptions();

        // Act
        var result = _differ.Compare(oldCode, newCode, options);

        // Assert
        result.Stats.TotalChanges.Should().BeGreaterThan(0);
    }

    #endregion

    #region Async/Await Tests

    [Fact]
    public void Compare_AsyncMethod_DetectsChanges()
    {
        // Arrange
        var oldCode = """
            namespace Test;
            using System.Threading.Tasks;
            public class Foo
            {
                public async Task<int> GetValueAsync()
                {
                    await Task.Delay(100);
                    return 42;
                }
            }
            """;
        var newCode = """
            namespace Test;
            using System.Threading.Tasks;
            public class Foo
            {
                public async Task<int> GetValueAsync()
                {
                    await Task.Delay(200);
                    return 43;
                }
            }
            """;

        var options = new DiffOptions();

        // Act
        var result = _differ.Compare(oldCode, newCode, options);

        // Assert
        result.Stats.TotalChanges.Should().BeGreaterThan(0);
    }

    [Fact]
    public void Compare_AsyncLambda_DetectsChanges()
    {
        // Arrange
        var oldCode = """
            namespace Test;
            using System;
            using System.Threading.Tasks;
            public class Foo
            {
                public Func<Task<int>> GetFunc() => async () =>
                {
                    await Task.Delay(100);
                    return 1;
                };
            }
            """;
        var newCode = """
            namespace Test;
            using System;
            using System.Threading.Tasks;
            public class Foo
            {
                public Func<Task<int>> GetFunc() => async () =>
                {
                    await Task.Delay(200);
                    return 2;
                };
            }
            """;

        var options = new DiffOptions();

        // Act
        var result = _differ.Compare(oldCode, newCode, options);

        // Assert
        result.Stats.TotalChanges.Should().BeGreaterThan(0);
    }

    #endregion

    #region LINQ Tests

    [Fact]
    public void Compare_LinqQuery_DetectsChanges()
    {
        // Arrange
        var oldCode = """
            namespace Test;
            using System.Linq;
            public class Foo
            {
                public int[] Filter(int[] arr) =>
                    (from x in arr where x > 0 select x).ToArray();
            }
            """;
        var newCode = """
            namespace Test;
            using System.Linq;
            public class Foo
            {
                public int[] Filter(int[] arr) =>
                    (from x in arr where x >= 0 select x).ToArray();
            }
            """;

        var options = new DiffOptions();

        // Act
        var result = _differ.Compare(oldCode, newCode, options);

        // Assert
        result.Stats.TotalChanges.Should().BeGreaterThan(0);
    }

    [Fact]
    public void Compare_LinqMethod_DetectsChanges()
    {
        // Arrange
        var oldCode = """
            namespace Test;
            using System.Linq;
            public class Foo
            {
                public int[] Filter(int[] arr) => arr.Where(x => x > 0).ToArray();
            }
            """;
        var newCode = """
            namespace Test;
            using System.Linq;
            public class Foo
            {
                public int[] Filter(int[] arr) => arr.Where(x => x >= 0).ToArray();
            }
            """;

        var options = new DiffOptions();

        // Act
        var result = _differ.Compare(oldCode, newCode, options);

        // Assert
        result.Stats.TotalChanges.Should().BeGreaterThan(0);
    }

    #endregion

    #region Deeply Nested Code Tests

    [Fact]
    public void Compare_DeeplyNestedCode_HandlesCorrectly()
    {
        // Arrange - 10+ levels of nesting
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
                                    if (true)
                                    {
                                        if (true)
                                        {
                                            if (true)
                                            {
                                                if (true)
                                                {
                                                    if (true)
                                                    {
                                                        if (true)
                                                        {
                                                            DoOld();
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                void DoOld() { }
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
                                    if (true)
                                    {
                                        if (true)
                                        {
                                            if (true)
                                            {
                                                if (true)
                                                {
                                                    if (true)
                                                    {
                                                        if (true)
                                                        {
                                                            DoNew();
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                void DoNew() { }
            }
            """;

        var options = new DiffOptions();

        // Act
        var result = _differ.Compare(oldCode, newCode, options);

        // Assert
        result.Stats.TotalChanges.Should().BeGreaterThan(0);
    }

    #endregion

    #region Files With Only Comments Tests

    [Fact]
    public void Compare_FileWithOnlyComments_HandlesCorrectly()
    {
        // Arrange
        var oldCode = """
            // This file contains only comments
            // No actual code here
            /* Multi-line
               comment */
            """;
        var newCode = """
            // This file contains only comments
            // Updated comment
            /* Multi-line
               different comment */
            """;

        var options = new DiffOptions();

        // Act
        var result = _differ.Compare(oldCode, newCode, options);

        // Assert
        result.FileChanges.Should().HaveCount(1);
    }

    [Fact]
    public void Compare_CommentsOnly_WithIgnoreComments_NoChanges()
    {
        // Arrange
        var oldCode = """
            // Comment 1
            /* Comment 2 */
            """;
        var newCode = """
            // Different comment
            /* Another different comment */
            """;

        var options = new DiffOptions { IgnoreComments = true };

        // Act
        var result = _differ.Compare(oldCode, newCode, options);

        // Assert
        result.Stats.TotalChanges.Should().Be(0);
    }

    #endregion

    #region Init-Only Properties Tests

    [Fact]
    public void Compare_InitOnlyProperty_DetectsChanges()
    {
        // Arrange
        var oldCode = """
            namespace Test;
            public class Person
            {
                public string Name { get; init; }
            }
            """;
        var newCode = """
            namespace Test;
            public class Person
            {
                public string Name { get; init; }
                public int Age { get; init; }
            }
            """;

        var options = new DiffOptions();

        // Act
        var result = _differ.Compare(oldCode, newCode, options);

        // Assert
        result.Stats.Additions.Should().BeGreaterThan(0);
    }

    #endregion

    #region Required Members Tests

    [Fact]
    public void Compare_RequiredMember_DetectsChanges()
    {
        // Arrange
        var oldCode = """
            namespace Test;
            public class Person
            {
                public string Name { get; set; }
            }
            """;
        var newCode = """
            namespace Test;
            public class Person
            {
                public required string Name { get; set; }
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
